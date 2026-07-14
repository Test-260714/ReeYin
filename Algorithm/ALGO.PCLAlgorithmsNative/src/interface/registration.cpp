#include "pch.h"

#include "registration.h"

#include <pcl/kdtree/kdtree_flann.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <vector>

namespace
{
// native 侧的受限 ICP 只优化 X/Y/Z 平移和绕 Z 轴的 Yaw。
constexpr double Pi = 3.141592653589793238462643383279502884;
constexpr int OptimizeXMask = 1;
constexpr int OptimizeYMask = 2;
constexpr int OptimizeZMask = 4;
constexpr int OptimizeYawMask = 8;
constexpr int OptimizeAllMask = OptimizeXMask | OptimizeYMask | OptimizeZMask | OptimizeYawMask;
constexpr int MaxNearestCandidates = 8;

// ICP 迭代中累计的 4DoF 位姿；Roll、Pitch 和 Scale 不参与优化。
struct RigidState
{
    double x = 0.0;
    double y = 0.0;
    double z = 0.0;
    double yawRad = 0.0;
};

struct TargetBounds
{
    double minX = std::numeric_limits<double>::infinity();
    double minY = std::numeric_limits<double>::infinity();
    double minZ = std::numeric_limits<double>::infinity();
    double maxX = -std::numeric_limits<double>::infinity();
    double maxY = -std::numeric_limits<double>::infinity();
    double maxZ = -std::numeric_limits<double>::infinity();
};

// source 点按当前位姿变换后，与 target 点云建立的一组对应关系。
struct Correspondence
{
    pcl::PointXYZ source;
    pcl::PointXYZ target;
};

// 保存一次对应点收集结果，同时记录有效 source 点数和匹配 RMSE。
struct CorrespondenceSet
{
    std::vector<Correspondence> pairs;
    int validSourceCount = 0;
    double rmse = std::numeric_limits<double>::infinity();
};

bool IsFinite(double value)
{
    return std::isfinite(value);
}

bool IsFinitePoint(const pcl::PointXYZ& point)
{
    return std::isfinite(point.x) && std::isfinite(point.y) && std::isfinite(point.z);
}

void IncludePointInBounds(const pcl::PointXYZ& point, TargetBounds& bounds)
{
    bounds.minX = std::min(bounds.minX, static_cast<double>(point.x));
    bounds.minY = std::min(bounds.minY, static_cast<double>(point.y));
    bounds.minZ = std::min(bounds.minZ, static_cast<double>(point.z));
    bounds.maxX = std::max(bounds.maxX, static_cast<double>(point.x));
    bounds.maxY = std::max(bounds.maxY, static_cast<double>(point.y));
    bounds.maxZ = std::max(bounds.maxZ, static_cast<double>(point.z));
}

double SquaredDistanceToBounds(const pcl::PointXYZ& point, const TargetBounds& bounds)
{
    const auto axisDistance = [](double value, double minValue, double maxValue) {
        if (value < minValue) {
            return minValue - value;
        }
        if (value > maxValue) {
            return value - maxValue;
        }
        return 0.0;
    };

    const double dx = axisDistance(static_cast<double>(point.x), bounds.minX, bounds.maxX);
    const double dy = axisDistance(static_cast<double>(point.y), bounds.minY, bounds.maxY);
    const double dz = axisDistance(static_cast<double>(point.z), bounds.minZ, bounds.maxZ);
    return dx * dx + dy * dy + dz * dz;
}

bool IsDimensionEnabled(int optimizationMask, int dimensionMask)
{
    return (optimizationMask & dimensionMask) != 0;
}

// 按当前 4DoF 位姿变换 source 点：Yaw 只作用于 XY 平面，Z 只做平移。
pcl::PointXYZ TransformPoint(const pcl::PointXYZ& point, const RigidState& state, double cosYaw, double sinYaw)
{
    return pcl::PointXYZ(
        static_cast<float>(state.x + cosYaw * point.x - sinYaw * point.y),
        static_cast<float>(state.y + sinYaw * point.x + cosYaw * point.y),
        static_cast<float>(state.z + point.z));
}

bool IsFiniteState(const RigidState& state)
{
    return IsFinite(state.x) && IsFinite(state.y) && IsFinite(state.z) && IsFinite(state.yawRad);
}

double NormalizeYawDeg(double yawDeg)
{
    double normalized = std::fmod(yawDeg, 360.0);
    if (normalized > 180.0) {
        normalized -= 360.0;
    }
    if (normalized <= -180.0) {
        normalized += 360.0;
    }

    return normalized;
}

// 过滤 target 点云中的 NaN/Inf，并转成 unorganized cloud 供 KD-tree 建索引。
bool BuildFiniteTargetCloud(
    const pcl::PointCloud<pcl::PointXYZ>* targetCloud,
    pcl::PointCloud<pcl::PointXYZ>::Ptr& finiteTargetCloud,
    TargetBounds& targetBounds)
{
    if (targetCloud == nullptr || targetCloud->empty()) {
        return false;
    }

    finiteTargetCloud.reset(new pcl::PointCloud<pcl::PointXYZ>());
    finiteTargetCloud->reserve(targetCloud->size());
    targetBounds = TargetBounds{};

    for (const auto& point : targetCloud->points) {
        if (IsFinitePoint(point)) {
            finiteTargetCloud->push_back(point);
            IncludePointInBounds(point, targetBounds);
        }
    }

    if (finiteTargetCloud->empty()) {
        return false;
    }

    finiteTargetCloud->width = static_cast<std::uint32_t>(finiteTargetCloud->size());
    finiteTargetCloud->height = 1;
    finiteTargetCloud->is_dense = true;
    return true;
}

// 将 source 按当前 state 变换后，在 target KD-tree 中搜索半径内的对应点。
// usedTargets 保证一个 target 点只被匹配一次，避免多个 source 点抢同一个邻近点。
CorrespondenceSet CollectCorrespondences(
    const pcl::PointCloud<pcl::PointXYZ>* sourceCloud,
    const pcl::KdTreeFLANN<pcl::PointXYZ>& targetTree,
    const pcl::PointCloud<pcl::PointXYZ>& finiteTargetCloud,
    const TargetBounds& targetBounds,
    const RigidState& state,
    double maxCorrespondenceDistance)
{
    CorrespondenceSet result;
    if (sourceCloud == nullptr || sourceCloud->empty() || finiteTargetCloud.empty()) {
        return result;
    }

    const float maxDistanceSquared = static_cast<float>(maxCorrespondenceDistance * maxCorrespondenceDistance);
    const double cosYaw = std::cos(state.yawRad);
    const double sinYaw = std::sin(state.yawRad);
    const int nearestCandidateLimit = finiteTargetCloud.size() < static_cast<std::size_t>(MaxNearestCandidates)
        ? static_cast<int>(finiteTargetCloud.size())
        : MaxNearestCandidates;

    double squaredSum = 0.0;
    std::vector<char> usedTargets(finiteTargetCloud.size(), 0);
    std::vector<int> indices;
    std::vector<float> squaredDistances;
    indices.reserve(MaxNearestCandidates);
    squaredDistances.reserve(MaxNearestCandidates);

    for (const auto& point : sourceCloud->points) {
        if (!IsFinitePoint(point)) {
            continue;
        }

        ++result.validSourceCount;
        const pcl::PointXYZ transformed = TransformPoint(point, state, cosYaw, sinYaw);
        if (!IsFinitePoint(transformed)) {
            continue;
        }

        if (SquaredDistanceToBounds(transformed, targetBounds) > maxDistanceSquared) {
            continue;
        }

        indices.clear();
        squaredDistances.clear();

        const int neighborCount = targetTree.radiusSearch(
            transformed,
            maxCorrespondenceDistance,
            indices,
            squaredDistances,
            nearestCandidateLimit);

        if (neighborCount <= 0) {
            continue;
        }

        int selectedIndex = -1;
        float selectedSquaredDistance = 0.0f;
        for (std::size_t candidate = 0; candidate < indices.size(); ++candidate) {
            const float candidateSquaredDistance = squaredDistances[candidate];
            if (candidateSquaredDistance > maxDistanceSquared) {
                break;
            }

            const int targetIndex = indices[candidate];
            if (targetIndex < 0 || static_cast<std::size_t>(targetIndex) >= usedTargets.size()) {
                continue;
            }

            if (usedTargets[static_cast<std::size_t>(targetIndex)] != 0) {
                continue;
            }

            selectedIndex = targetIndex;
            selectedSquaredDistance = candidateSquaredDistance;
            break;
        }

        if (selectedIndex < 0) {
            continue;
        }

        usedTargets[static_cast<std::size_t>(selectedIndex)] = 1;
        const pcl::PointXYZ& target = finiteTargetCloud[static_cast<std::size_t>(selectedIndex)];
        result.pairs.push_back(Correspondence{ transformed, target });
        squaredSum += selectedSquaredDistance;
    }

    if (!result.pairs.empty()) {
        result.rmse = std::sqrt(squaredSum / static_cast<double>(result.pairs.size()));
    }

    return result;
}

// 根据对应点估计一轮修正：XY 用质心和 atan2 估计 Yaw，Z 用平均高度差。
bool EstimateCorrection(const std::vector<Correspondence>& pairs, int optimizationMask, RigidState& correction)
{
    if (pairs.size() < 3) {
        return false;
    }

    double sourceCentroidX = 0.0;
    double sourceCentroidY = 0.0;
    double targetCentroidX = 0.0;
    double targetCentroidY = 0.0;
    double deltaZSum = 0.0;

    for (const auto& pair : pairs) {
        sourceCentroidX += pair.source.x;
        sourceCentroidY += pair.source.y;
        targetCentroidX += pair.target.x;
        targetCentroidY += pair.target.y;
        deltaZSum += static_cast<double>(pair.target.z) - pair.source.z;
    }

    const double count = static_cast<double>(pairs.size());
    sourceCentroidX /= count;
    sourceCentroidY /= count;
    targetCentroidX /= count;
    targetCentroidY /= count;

    double sumCross = 0.0;
    double sumDot = 0.0;
    for (const auto& pair : pairs) {
        const double sourceX = pair.source.x - sourceCentroidX;
        const double sourceY = pair.source.y - sourceCentroidY;
        const double targetX = pair.target.x - targetCentroidX;
        const double targetY = pair.target.y - targetCentroidY;

        sumCross += sourceX * targetY - sourceY * targetX;
        sumDot += sourceX * targetX + sourceY * targetY;
    }

    correction.yawRad = IsDimensionEnabled(optimizationMask, OptimizeYawMask)
        ? std::atan2(sumCross, sumDot)
        : 0.0;
    const double cosYaw = std::cos(correction.yawRad);
    const double sinYaw = std::sin(correction.yawRad);
    correction.x = targetCentroidX - (cosYaw * sourceCentroidX - sinYaw * sourceCentroidY);
    correction.y = targetCentroidY - (sinYaw * sourceCentroidX + cosYaw * sourceCentroidY);
    correction.z = deltaZSum / count;

    if (!IsDimensionEnabled(optimizationMask, OptimizeXMask)) {
        correction.x = 0.0;
    }
    if (!IsDimensionEnabled(optimizationMask, OptimizeYMask)) {
        correction.y = 0.0;
    }
    if (!IsDimensionEnabled(optimizationMask, OptimizeZMask)) {
        correction.z = 0.0;
    }

    return IsFiniteState(correction);
}

// 将本轮 correction 组合到累计 state，使下一轮基于新的整体位姿继续匹配。
void ApplyCorrection(RigidState& state, const RigidState& correction, int optimizationMask)
{
    const RigidState previous = state;
    const double cosYaw = std::cos(correction.yawRad);
    const double sinYaw = std::sin(correction.yawRad);
    const double nextX = cosYaw * state.x - sinYaw * state.y + correction.x;
    const double nextY = sinYaw * state.x + cosYaw * state.y + correction.y;

    state.x = IsDimensionEnabled(optimizationMask, OptimizeXMask) ? nextX : previous.x;
    state.y = IsDimensionEnabled(optimizationMask, OptimizeYMask) ? nextY : previous.y;
    state.z = IsDimensionEnabled(optimizationMask, OptimizeZMask) ? previous.z + correction.z : previous.z;
    state.yawRad = IsDimensionEnabled(optimizationMask, OptimizeYawMask)
        ? previous.yawRad + correction.yawRad
        : previous.yawRad;
}

// native 导出函数的参数校验，防止空指针、无效数值或非正迭代参数进入 ICP。
bool ValidateInputs(
    PointCloudNativeHandle* source,
    PointCloudNativeHandle* target,
    double initialX,
    double initialY,
    double initialZ,
    double initialYawDeg,
    int maxIterations,
    double maxCorrespondenceDistance,
    double transformationEpsilon,
    double fitnessEpsilon,
    int optimizationMask,
    double* outTransform,
    double* outMetrics)
{
    return source != nullptr
        && target != nullptr
        && outTransform != nullptr
        && outMetrics != nullptr
        && maxIterations > 0
        && IsFinite(initialX)
        && IsFinite(initialY)
        && IsFinite(initialZ)
        && IsFinite(initialYawDeg)
        && IsFinite(maxCorrespondenceDistance)
        && maxCorrespondenceDistance > 0.0
        && IsFinite(transformationEpsilon)
        && transformationEpsilon > 0.0
        && IsFinite(fitnessEpsilon)
        && fitnessEpsilon > 0.0
        && optimizationMask >= 0
        && (optimizationMask & ~OptimizeAllMask) == 0;
}

// 失败路径也写入确定的零输出，避免调用方读到复用缓冲区中的旧值。
void ClearOutputs(double* outTransform, double* outMetrics)
{
    if (outTransform != nullptr) {
        for (int i = 0; i < 4; ++i) {
            outTransform[i] = 0.0;
        }
    }

    if (outMetrics != nullptr) {
        for (int i = 0; i < 5; ++i) {
            outMetrics[i] = 0.0;
        }
    }
}
}

// C API 导出入口：执行 source 到 target 的受限 ICP，成功返回 1，失败返回 0。
HEAD int CallingConvention constrainedIcpRegistration(
    PointCloudNativeHandle* source,
    PointCloudNativeHandle* target,
    double initial_x,
    double initial_y,
    double initial_z,
    double initial_yaw_deg,
    int max_iterations,
    double max_correspondence_distance,
    double transformation_epsilon,
    double fitness_epsilon,
    int optimization_mask,
    double* out_transform,
    double* out_metrics)
{
    ClearOutputs(out_transform, out_metrics);

    if (!ValidateInputs(
        source,
        target,
        initial_x,
        initial_y,
        initial_z,
        initial_yaw_deg,
        max_iterations,
        max_correspondence_distance,
        transformation_epsilon,
        fitness_epsilon,
        optimization_mask,
        out_transform,
        out_metrics)) {
        return 0;
    }

    const auto* sourceCloud = PointCloudData(source);
    const auto* targetCloud = PointCloudData(target);
    if (sourceCloud == nullptr || targetCloud == nullptr || sourceCloud->empty() || targetCloud->empty()) {
        return 0;
    }

    pcl::PointCloud<pcl::PointXYZ>::Ptr finiteTargetCloud;
    TargetBounds targetBounds;
    if (!BuildFiniteTargetCloud(targetCloud, finiteTargetCloud, targetBounds)) {
        return 0;
    }

    // ICP 前只对 target 建一次 KD-tree；source 每轮按 state 变换后再查询 target。
    pcl::KdTreeFLANN<pcl::PointXYZ> targetTree;
    targetTree.setInputCloud(finiteTargetCloud);

    RigidState state{
        initial_x,
        initial_y,
        initial_z,
        initial_yaw_deg * Pi / 180.0
    };

    if (!IsFiniteState(state)) {
        return 0;
    }

    bool converged = false;
    int iterations = 0;
    double previousRmse = std::numeric_limits<double>::infinity();
    RigidState bestState = state;
    double bestRmse = std::numeric_limits<double>::infinity();
    bool hasBestState = false;
    constexpr double DivergenceRatio = 2.0;

    // 每轮只收集一次对应点；RMSE 收敛判断延后到下一轮开头，避免重复 KD-tree 查询。
    for (int iteration = 0; iteration < max_iterations; ++iteration) {
        CorrespondenceSet correspondences = CollectCorrespondences(
            sourceCloud,
            targetTree,
            *finiteTargetCloud,
            targetBounds,
            state,
            max_correspondence_distance);

        if (correspondences.pairs.size() < 3 || !IsFinite(correspondences.rmse)) {
            if (hasBestState) {
                state = bestState;
                break;
            }

            return 0;
        }

        if (correspondences.rmse < bestRmse) {
            bestRmse = correspondences.rmse;
            bestState = state;
            hasBestState = true;
        }

        if (iteration > 0 && IsFinite(previousRmse)) {
            if (hasBestState && correspondences.rmse > bestRmse * DivergenceRatio) {
                state = bestState;
                break;
            }

            if (std::abs(previousRmse - correspondences.rmse) < fitness_epsilon) {
                converged = true;
                break;
            }
        }

        RigidState correction;
        if (!EstimateCorrection(correspondences.pairs, optimization_mask, correction)) {
            if (hasBestState) {
                state = bestState;
                break;
            }

            return 0;
        }

        ApplyCorrection(state, correction, optimization_mask);
        if (!IsFiniteState(state)) {
            if (hasBestState) {
                state = bestState;
                break;
            }

            return 0;
        }

        ++iterations;
        const double change = std::abs(correction.x)
            + std::abs(correction.y)
            + std::abs(correction.z)
            + std::abs(correction.yawRad);

        if (change < transformation_epsilon) {
            converged = true;
            break;
        }

        previousRmse = correspondences.rmse;
    }

    // 用最终 state 重新收集对应点，确保输出 metrics 对应最终位姿。
    CorrespondenceSet finalCorrespondences = CollectCorrespondences(
        sourceCloud,
        targetTree,
        *finiteTargetCloud,
        targetBounds,
        state,
        max_correspondence_distance);

    const bool finalInvalid = finalCorrespondences.pairs.size() < 3
        || finalCorrespondences.validSourceCount <= 0
        || !IsFinite(finalCorrespondences.rmse)
        || !IsFiniteState(state);
    const bool finalDiverged = hasBestState
        && IsFinite(finalCorrespondences.rmse)
        && finalCorrespondences.rmse > bestRmse * DivergenceRatio;

    if (hasBestState && (finalInvalid || finalDiverged)) {
        state = bestState;
        converged = false;
        finalCorrespondences = CollectCorrespondences(
            sourceCloud,
            targetTree,
            *finiteTargetCloud,
            targetBounds,
            state,
            max_correspondence_distance);
    }

    if (finalCorrespondences.pairs.size() < 3
        || finalCorrespondences.validSourceCount <= 0
        || !IsFinite(finalCorrespondences.rmse)
        || !IsFiniteState(state)) {
        return 0;
    }

    // out_transform: [x, y, z, yawDeg]
    // out_metrics: [rmse, inlierRatio, matchedPairs, iterations, converged]
    out_transform[0] = state.x;
    out_transform[1] = state.y;
    out_transform[2] = state.z;
    out_transform[3] = NormalizeYawDeg(state.yawRad * 180.0 / Pi);

    out_metrics[0] = finalCorrespondences.rmse;
    out_metrics[1] = static_cast<double>(finalCorrespondences.pairs.size()) / finalCorrespondences.validSourceCount;
    out_metrics[2] = static_cast<double>(finalCorrespondences.pairs.size());
    out_metrics[3] = static_cast<double>(iterations);
    out_metrics[4] = converged ? 1.0 : 0.0;

    return IsFinite(out_transform[0])
        && IsFinite(out_transform[1])
        && IsFinite(out_transform[2])
        && IsFinite(out_transform[3])
        && IsFinite(out_metrics[0])
        && IsFinite(out_metrics[1])
        ? 1
        : 0;
}

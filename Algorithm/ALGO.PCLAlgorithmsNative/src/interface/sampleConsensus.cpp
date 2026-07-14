#include "pch.h"

#include "sampleConsensus.h"

// ============================================================================
// 工具函数：计算两个向量的夹角
// ============================================================================
inline float calculateAngle(const double* vec1, const double* vec2) {
    // 计算点积
    double dot_product = vec1[0] * vec2[0] + vec1[1] * vec2[1] + vec1[2] * vec2[2];

    // 计算模长
    double norm1 = std::sqrt(vec1[0] * vec1[0] + vec1[1] * vec1[1] + vec1[2] * vec1[2]);
    double norm2 = std::sqrt(vec2[0] * vec2[0] + vec2[1] * vec2[1] + vec2[2] * vec2[2]);

    // 防止除零和数值误差
    if (norm1 < 1e-10 || norm2 < 1e-10) {
        return 0.0f;
    }

    // 计算cos值，限制在[-1, 1]范围内避免acos域错误
    double cos_angle = dot_product / (norm1 * norm2);
    cos_angle = std::max(-1.0, std::min(1.0, cos_angle));

    // 转换为角度（取锐角）
    double angle_rad = std::acos(std::abs(cos_angle));
    return static_cast<float>(angle_rad * 180.0 / 3.14159265358979323846);
}
//拟合平面
HEAD float CallingConvention fitPlane(
    PointCloudNativeHandle* in_pc,
    float distance_thresh,
    int max_itera,
    float* normal)
{
    auto* inputCloud = PointCloudData(in_pc);

    // ========================================
    // 1. 输入验证
    // ========================================
    if (!inputCloud || !normal) {
        return -1.0f;  // 空指针错误
    }

    if (inputCloud->points.empty()) {
        return -1.0f;  // 空点云
    }

    if (inputCloud->points.size() < 3) {
        return -1.0f;  // 点数不足3个，无法拟合平面
    }

    if (distance_thresh <= 0 || max_itera <= 0) {
        return -1.0f;  // 参数非法
    }

    // ========================================
    // 2. RANSAC平面拟合
    // ========================================
    pcl::ModelCoefficients::Ptr modelCoeff(new pcl::ModelCoefficients());
    pcl::PointIndices::Ptr pointIndices(new pcl::PointIndices());

    pcl::SACSegmentation<pcl::PointXYZ> seg;
    seg.setModelType(pcl::SACMODEL_PLANE);
    seg.setMethodType(pcl::SAC_RANSAC);
    seg.setInputCloud(inputCloud->makeShared());
    seg.setDistanceThreshold(distance_thresh);
    seg.setMaxIterations(max_itera);
    seg.setOptimizeCoefficients(true);  // 启用系数优化

    // 可选：设置概率阈值，提高拟合质量
    seg.setProbability(0.99);  // 99%置信度

    seg.segment(*pointIndices, *modelCoeff);

    // ========================================
    // 3. 检查拟合结果
    // ========================================
    if (pointIndices->indices.empty() || modelCoeff->values.size() != 4) {
        return -1.0f;  // 拟合失败
    }

    // ========================================
    // 4. 归一化法向量
    // ========================================
    double a = modelCoeff->values[0];
    double b = modelCoeff->values[1];
    double c = modelCoeff->values[2];
    double d = modelCoeff->values[3];

    // 计算法向量的模长
    double norm = std::sqrt(a * a + b * b + c * c);

    if (norm < 1e-10) {
        return -1.0f;  // 法向量接近零，拟合失败
    }

    // 归一化（确保法向量是单位向量）
    a /= norm;
    b /= norm;
    c /= norm;
    d /= norm;

    // 可选：确保法向量指向Z轴正方向（统一方向）
    if (c < 0) {
        a = -a;
        b = -b;
        c = -c;
        d = -d;
    }

    // ========================================
    // 5. 输出结果
    // ========================================
    normal[0] = static_cast<float>(a);
    normal[1] = static_cast<float>(b);
    normal[2] = static_cast<float>(c);
    normal[3] = static_cast<float>(d);

    // ========================================
    // 6. 计算与Z轴夹角
    // ========================================
    double reference_normal[3] = { 0.0, 0.0, 1.0 };  // Z轴方向
    double fitted_normal[3] = { a, b, c };

    float angle = calculateAngle(reference_normal, fitted_normal);

    return angle;
}

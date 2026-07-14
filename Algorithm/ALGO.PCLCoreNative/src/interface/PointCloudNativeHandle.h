#pragma once

#include <pcl/common/common.h>
#include <pcl/point_cloud.h>
#include <pcl/point_types.h>

#include <algorithm>
#include <cmath>
#include <cstdint>

struct PointCloudNativeHandle
{
    pcl::PointCloud<pcl::PointXYZ> cloud;
    bool boundsValid = false;
    pcl::PointXYZ minPoint = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
    pcl::PointXYZ maxPoint = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
    std::uint64_t version = 0;
};

inline pcl::PointCloud<pcl::PointXYZ>* PointCloudData(PointCloudNativeHandle* handle)
{
    return handle == nullptr ? nullptr : &handle->cloud;
}

inline const pcl::PointCloud<pcl::PointXYZ>* PointCloudData(const PointCloudNativeHandle* handle)
{
    return handle == nullptr ? nullptr : &handle->cloud;
}

inline void MarkPointCloudModified(PointCloudNativeHandle* handle)
{
    if (handle != nullptr) {
        ++handle->version;
    }
}

inline void SetPointCloudBounds(
    PointCloudNativeHandle* handle,
    const pcl::PointXYZ& minPoint,
    const pcl::PointXYZ& maxPoint)
{
    if (handle == nullptr) {
        return;
    }

    handle->minPoint = minPoint;
    handle->maxPoint = maxPoint;
    handle->boundsValid = true;
}

inline void SetPointCloudBoundsToZero(PointCloudNativeHandle* handle)
{
    SetPointCloudBounds(
        handle,
        pcl::PointXYZ(0.0f, 0.0f, 0.0f),
        pcl::PointXYZ(0.0f, 0.0f, 0.0f));
}

inline void InvalidatePointCloudBounds(PointCloudNativeHandle* handle)
{
    if (handle == nullptr) {
        return;
    }

    handle->boundsValid = false;
    handle->minPoint = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
    handle->maxPoint = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
    MarkPointCloudModified(handle);
}

class PointCloudBoundsAccumulator
{
public:
    void Add(const pcl::PointXYZ& point)
    {
        if (!std::isfinite(point.x) || !std::isfinite(point.y) || !std::isfinite(point.z)) {
            return;
        }

        if (!hasBounds_) {
            minPoint_ = point;
            maxPoint_ = point;
            hasBounds_ = true;
            return;
        }

        minPoint_.x = std::min(minPoint_.x, point.x);
        minPoint_.y = std::min(minPoint_.y, point.y);
        minPoint_.z = std::min(minPoint_.z, point.z);
        maxPoint_.x = std::max(maxPoint_.x, point.x);
        maxPoint_.y = std::max(maxPoint_.y, point.y);
        maxPoint_.z = std::max(maxPoint_.z, point.z);
    }

    bool HasBounds() const
    {
        return hasBounds_;
    }

    const pcl::PointXYZ& MinPoint() const
    {
        return minPoint_;
    }

    const pcl::PointXYZ& MaxPoint() const
    {
        return maxPoint_;
    }

    void ApplyTo(PointCloudNativeHandle* handle) const
    {
        if (handle == nullptr) {
            return;
        }

        if (hasBounds_) {
            SetPointCloudBounds(handle, minPoint_, maxPoint_);
        } else {
            SetPointCloudBoundsToZero(handle);
        }
        MarkPointCloudModified(handle);
    }

private:
    bool hasBounds_ = false;
    pcl::PointXYZ minPoint_ = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
    pcl::PointXYZ maxPoint_ = pcl::PointXYZ(0.0f, 0.0f, 0.0f);
};

inline bool EnsurePointCloudBounds(PointCloudNativeHandle* handle)
{
    if (handle == nullptr) {
        return false;
    }

    if (handle->boundsValid) {
        return true;
    }

    if (handle->cloud.empty()) {
        SetPointCloudBoundsToZero(handle);
        return true;
    }

    pcl::PointXYZ minPoint(0.0f, 0.0f, 0.0f);
    pcl::PointXYZ maxPoint(0.0f, 0.0f, 0.0f);
    pcl::getMinMax3D(handle->cloud, minPoint, maxPoint);
    SetPointCloudBounds(handle, minPoint, maxPoint);
    return true;
}

inline void CopyPointCloudBoundsIfValid(
    const PointCloudNativeHandle* source,
    PointCloudNativeHandle* destination)
{
    if (destination == nullptr) {
        return;
    }

    if (source != nullptr && source->boundsValid) {
        SetPointCloudBounds(destination, source->minPoint, source->maxPoint);
        MarkPointCloudModified(destination);
        return;
    }

    InvalidatePointCloudBounds(destination);
}

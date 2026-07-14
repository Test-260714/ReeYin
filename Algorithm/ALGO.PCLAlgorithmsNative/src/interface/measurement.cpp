#include "pch.h"

#include "measurement.h"
#include <pcl/filters/crop_box.h>

/*====================================================================================
 *                           辅助函数实现
 *====================================================================================*/

 // 归一化平面系数
HEAD void CallingConvention normalizePlaneCoefficients(float* plane)
{
    // 计算法向量的模
    float norm = sqrt(plane[0] * plane[0] + plane[1] * plane[1] + plane[2] * plane[2]);

    if (norm > 1e-6) // 避免除零
    {
        plane[0] /= norm;
        plane[1] /= norm;
        plane[2] /= norm;
        plane[3] /= norm;
    }

    // 调试输出
    cout << "[DEBUG] Normalized plane: "
        << "a=" << plane[0] << ", b=" << plane[1]
        << ", c=" << plane[2] << ", d=" << plane[3] << endl;
}

// 计算点到平面的距离
HEAD double CallingConvention pointToPlaneDistance(double* point, float* plane)
{
    // 公式：distance = (ax + by + cz + d) / sqrt(a² + b² + c²)
    // 如果平面系数已归一化，则分母为1
    double numerator = plane[0] * point[0] + plane[1] * point[1] + plane[2] * point[2] + plane[3];
    double denominator = sqrt(plane[0] * plane[0] + plane[1] * plane[1] + plane[2] * plane[2]);

    if (denominator < 1e-6)
    {
        cout << "[ERROR] Invalid plane coefficients in pointToPlaneDistance!" << endl;
        return 0.0;
    }

    return numerator / denominator;
}


/*====================================================================================
 *                           点到平面距离计算
 *====================================================================================*/

HEAD void CallingConvention calculatePointsToPlaneDistance(
    PointCloudNativeHandle* in_handle,
    float* plane_coeffs,
    double* out_distances,
    double* out_statistics)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || plane_coeffs == nullptr || out_distances == nullptr || out_statistics == nullptr)
    {
        return;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return;
    }

    cout << "[INFO] Calculating distances for " << in_pc->points.size() << " points..." << endl;

    // 归一化平面系数
    float normalized_plane[4];
    for (int i = 0; i < 4; i++)
        normalized_plane[i] = plane_coeffs[i];
    normalizePlaneCoefficients(normalized_plane);

    // 计算每个点到平面的距离
    double min_dist = DBL_MAX;
    double max_dist = -DBL_MAX;
    double sum_dist = 0.0;
    double sum_squared_dist = 0.0;

    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double point[3] = {
            in_pc->points[i].x,
            in_pc->points[i].y,
            in_pc->points[i].z
        };

        double distance = pointToPlaneDistance(point, normalized_plane);
        out_distances[i] = distance;

        // 更新统计信息
        if (distance < min_dist) min_dist = distance;
        if (distance > max_dist) max_dist = distance;
        sum_dist += distance;
        sum_squared_dist += distance * distance;
    }

    // 计算统计量
    size_t n = in_pc->points.size();
    double mean = sum_dist / n;
    double variance = (sum_squared_dist / n) - (mean * mean);
    double stddev = sqrt(variance);
    double range = max_dist - min_dist;

    // 输出统计信息
    out_statistics[0] = min_dist;
    out_statistics[1] = max_dist;
    out_statistics[2] = mean;
    out_statistics[3] = stddev;
    out_statistics[4] = range;

    cout << "[INFO] Distance Statistics:" << endl;
    cout << "  Min: " << min_dist << " mm" << endl;
    cout << "  Max: " << max_dist << " mm" << endl;
    cout << "  Mean: " << mean << " mm" << endl;
    cout << "  StdDev: " << stddev << " mm" << endl;
    cout << "  Range: " << range << " mm" << endl;
}


/*====================================================================================
 *                           高度测量功能
 *====================================================================================*/

 /*
 功能：测量从基准平面到点云顶部的高度（使用顶部平均值）
 改进：使用顶部2%点的平均值，替代单个最高点
 param[in] in_pc 输入点云对象指针
 param[in] base_plane 基准平面系数[a, b, c, d]
 param[out] max_point_index 最高点的索引号（可选，传入NULL则不输出）
 return 返回从基准面到顶部的平均高度值
 */
HEAD double CallingConvention measureHeightFromBasePlane(
    PointCloudNativeHandle* in_handle,
    float* base_plane,
    int* max_point_index)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || base_plane == nullptr)
    {
        return 0.0;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return 0.0;
    }

    cout << "[INFO] 测量高度（顶部平均法）..." << endl;

    // 1. 归一化平面系数
    float normalized_plane[4];
    for (int i = 0; i < 4; i++)
        normalized_plane[i] = base_plane[i];
    normalizePlaneCoefficients(normalized_plane);

    // 2. 计算所有点到基准面的距离
    vector<double> distances;
    distances.reserve(in_pc->points.size());

    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double point[3] = {
            in_pc->points[i].x,
            in_pc->points[i].y,
            in_pc->points[i].z
        };

        double distance = pointToPlaneDistance(point, normalized_plane);

        // 只保留正向距离（高于基准面的点）
        if (distance > 0)
        {
            distances.push_back(distance);
        }
    }

    if (distances.empty())
    {
        cout << "[ERROR] 没有找到高于基准面的点!" << endl;
        return 0.0;
    }

    // 3. 按距离排序（从大到小）
    sort(distances.begin(), distances.end(), greater<double>());

    // 4. 取顶部2%的点
    int top_count = max(10, (int)(distances.size() * 0.02));  // 至少10个点
    if (top_count > 500) top_count = 500;  // 最多500个点

    cout << "[INFO] 总点数: " << distances.size() << endl;
    cout << "[INFO] 顶部点数: " << top_count << endl;

    // 5. 计算顶部点的平均高度
    double sum = 0.0;
    for (int i = 0; i < top_count; i++)
    {
        sum += distances[i];
    }
    double avg_height = sum / top_count;

    // 6. 可选：简单的离群值剔除
    // 计算标准差
    double variance = 0.0;
    for (int i = 0; i < top_count; i++)
    {
        variance += (distances[i] - avg_height) * (distances[i] - avg_height);
    }
    double std_dev = sqrt(variance / top_count);

    // 剔除超过3σ的点，重新计算平均值
    double refined_sum = 0.0;
    int refined_count = 0;
    for (int i = 0; i < top_count; i++)
    {
        if (fabs(distances[i] - avg_height) <= 3.0 * std_dev)
        {
            refined_sum += distances[i];
            refined_count++;
        }
    }

    if (refined_count > 0)
    {
        avg_height = refined_sum / refined_count;
        cout << "[INFO] 离群值剔除: " << (top_count - refined_count) << " 个点" << endl;
        cout << "[INFO] 有效点数: " << refined_count << endl;
    }

    // 7. 输出统计信息
    cout << "[INFO] 平均高度: " << avg_height << " mm" << endl;
    cout << "[INFO] 标准差: " << std_dev << " mm" << endl;
    cout << "[INFO] 最高点: " << distances[0] << " mm" << endl;

    // 8. 如果需要，输出最高点索引（与旧版本兼容）
    if (max_point_index != NULL)
    {
        // 找到最高点的实际索引
        double max_distance = distances[0];
        for (size_t i = 0; i < in_pc->points.size(); i++)
        {
            double point[3] = {
                in_pc->points[i].x,
                in_pc->points[i].y,
                in_pc->points[i].z
            };
            double distance = pointToPlaneDistance(point, normalized_plane);

            if (fabs(distance - max_distance) < 0.0001)
            {
                *max_point_index = i;
                break;
            }
        }
    }

    return avg_height;
}



HEAD void CallingConvention separateBaseAndProtrusion(
    PointCloudNativeHandle* in_handle,
    float height_threshold,
    float* base_plane,
    PointCloudNativeHandle* out_base_handle,
    PointCloudNativeHandle* out_protrusion_handle)
{
    auto* in_pc = PointCloudData(in_handle);
    auto* out_base = PointCloudData(out_base_handle);
    auto* out_protrusion = PointCloudData(out_protrusion_handle);
    if (in_pc == nullptr || out_base == nullptr || out_protrusion == nullptr || base_plane == nullptr)
    {
        return;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return;
    }

    cout << "[INFO] Separating base and protrusion with threshold: " << height_threshold << " mm" << endl;

    // 归一化平面系数
    float normalized_plane[4];
    for (int i = 0; i < 4; i++)
        normalized_plane[i] = base_plane[i];
    normalizePlaneCoefficients(normalized_plane);

    // 清空输出点云
    out_base->clear();
    out_protrusion->clear();

    // 根据高度阈值分类
    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double point[3] = {
            in_pc->points[i].x,
            in_pc->points[i].y,
            in_pc->points[i].z
        };

        double distance = fabs(pointToPlaneDistance(point, normalized_plane));

        if (distance <= height_threshold)
        {
            out_base->push_back(in_pc->points[i]);
        }
        else
        {
            out_protrusion->push_back(in_pc->points[i]);
        }
    }

    cout << "[INFO] Separation complete:" << endl;
    cout << "  Base points: " << out_base->points.size() << endl;
    cout << "  Protrusion points: " << out_protrusion->points.size() << endl;

    if (out_protrusion->points.empty())
    {
        cout << "[WARNING] No protrusion detected! Consider adjusting height_threshold." << endl;
    }
    InvalidatePointCloudBounds(out_base_handle);
    InvalidatePointCloudBounds(out_protrusion_handle);
}


/*====================================================================================
 *                           【新增】ROI区域过滤功能
 *====================================================================================*/

HEAD void CallingConvention filterPointsByROI(
    PointCloudNativeHandle* in_handle,
    double roi_center_x,
    double roi_center_y,
    double roi_width,
    double roi_height,
    PointCloudNativeHandle* out_handle)
{
    auto* in_pc = PointCloudData(in_handle);
    auto* out_pc = PointCloudData(out_handle);
    if (in_pc == nullptr || out_pc == nullptr)
    {
        return;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return;
    }

    cout << "[INFO] Filtering points by ROI..." << endl;
    cout << "[INFO] ROI center: (" << roi_center_x << ", " << roi_center_y << ")" << endl;
    cout << "[INFO] ROI size: " << roi_width << " x " << roi_height << endl;

    // 计算ROI边界
    double x_min = roi_center_x - roi_width / 2.0;
    double x_max = roi_center_x + roi_width / 2.0;
    double y_min = roi_center_y - roi_height / 2.0;
    double y_max = roi_center_y + roi_height / 2.0;

    cout << "[INFO] ROI bounds: X[" << x_min << ", " << x_max
        << "], Y[" << y_min << ", " << y_max << "]" << endl;

    // 使用PCL的CropBox过滤器
    pcl::CropBox<pcl::PointXYZ> crop_filter;
    crop_filter.setInputCloud(in_pc->makeShared());

    // 设置裁剪盒的边界 (X,Y,Z的最小和最大值)
    // Z方向不限制,取整个点云的Z范围
    pcl::PointXYZ min_pt, max_pt;
    if (EnsurePointCloudBounds(in_handle)) {
        min_pt = in_handle->minPoint;
        max_pt = in_handle->maxPoint;
    }

    Eigen::Vector4f min_point(x_min, y_min, min_pt.z - 100, 1.0);
    Eigen::Vector4f max_point(x_max, y_max, max_pt.z + 100, 1.0);

    crop_filter.setMin(min_point);
    crop_filter.setMax(max_point);

    crop_filter.filter(*out_pc);

    cout << "[INFO] ROI filtering complete:" << endl;
    cout << "  Input points: " << in_pc->points.size() << endl;
    cout << "  Output points: " << out_pc->points.size() << endl;

    if (out_pc->points.empty())
    {
        cout << "[WARNING] No points in ROI! Check ROI parameters." << endl;
    }
    InvalidatePointCloudBounds(out_handle);
}


HEAD int CallingConvention suggestROIForProtrusion(
    PointCloudNativeHandle* in_handle,
    float* base_plane,
    double height_threshold,
    double* out_roi_x,
    double* out_roi_y,
    double* out_roi_width,
    double* out_roi_height)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || base_plane == nullptr)
    {
        return 0;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return 0;
    }

    cout << "[INFO] Detecting protrusion and suggesting ROI..." << endl;

    // 归一化平面
    float normalized_plane[4];
    for (int i = 0; i < 4; i++)
        normalized_plane[i] = base_plane[i];
    normalizePlaneCoefficients(normalized_plane);

    // 找出所有高于阈值的点
    std::vector<pcl::PointXYZ> protrusion_points;

    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double point[3] = {
            in_pc->points[i].x,
            in_pc->points[i].y,
            in_pc->points[i].z
        };

        double distance = pointToPlaneDistance(point, normalized_plane);

        if (distance > height_threshold)
        {
            protrusion_points.push_back(in_pc->points[i]);
        }
    }

    if (protrusion_points.empty())
    {
        cout << "[WARNING] No protrusion detected above threshold "
            << height_threshold << " mm" << endl;
        return 0;
    }

    cout << "[INFO] Found " << protrusion_points.size()
        << " points in protrusion" << endl;

    // 计算凸起点的XY边界
    double x_min = DBL_MAX, x_max = -DBL_MAX;
    double y_min = DBL_MAX, y_max = -DBL_MAX;

    for (const auto& pt : protrusion_points)
    {
        if (pt.x < x_min) x_min = pt.x;
        if (pt.x > x_max) x_max = pt.x;
        if (pt.y < y_min) y_min = pt.y;
        if (pt.y > y_max) y_max = pt.y;
    }

    // 计算ROI参数（加上一定余量）
    double margin = 2.0; // 2mm余量
    *out_roi_x = (x_min + x_max) / 2.0;
    *out_roi_y = (y_min + y_max) / 2.0;
    *out_roi_width = (x_max - x_min) + 2 * margin;
    *out_roi_height = (y_max - y_min) + 2 * margin;

    cout << "[INFO] Suggested ROI parameters:" << endl;
    cout << "  Center: (" << *out_roi_x << ", " << *out_roi_y << ")" << endl;
    cout << "  Size: " << *out_roi_width << " x " << *out_roi_height << endl;

    return (int)protrusion_points.size();
}


HEAD double CallingConvention measureHeightWithROI(
    PointCloudNativeHandle* in_handle,
    float* base_plane,
    bool use_auto_roi,
    double roi_x,
    double roi_y,
    double roi_width,
    double roi_height,
    int* out_max_point_index,
    double* out_used_roi_x,
    double* out_used_roi_y,
    double* out_used_roi_width,
    double* out_used_roi_height)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || base_plane == nullptr)
    {
        return 0.0;
    }

    cout << "[INFO] ===== Height Measurement with ROI =====" << endl;

    // 步骤1: 确定ROI参数
    double final_roi_x, final_roi_y, final_roi_width, final_roi_height;

    if (use_auto_roi)
    {
        cout << "[INFO] Using automatic ROI detection..." << endl;
        double height_threshold = 3.0; // 假设3mm以上是凸起

        int protrusion_count = suggestROIForProtrusion(
            in_handle,
            base_plane,
            height_threshold,
            &final_roi_x,
            &final_roi_y,
            &final_roi_width,
            &final_roi_height
        );

        if (protrusion_count == 0)
        {
            cout << "[ERROR] No protrusion detected for auto ROI!" << endl;
            return 0.0;
        }
    }
    else
    {
        cout << "[INFO] Using manual ROI parameters..." << endl;
        final_roi_x = roi_x;
        final_roi_y = roi_y;
        final_roi_width = roi_width;
        final_roi_height = roi_height;
    }

    // 输出使用的ROI参数
    if (out_used_roi_x) *out_used_roi_x = final_roi_x;
    if (out_used_roi_y) *out_used_roi_y = final_roi_y;
    if (out_used_roi_width) *out_used_roi_width = final_roi_width;
    if (out_used_roi_height) *out_used_roi_height = final_roi_height;

    // 步骤2: 根据ROI过滤点云
    PointCloudNativeHandle roi_handle;

    filterPointsByROI(
        in_handle,
        final_roi_x,
        final_roi_y,
        final_roi_width,
        final_roi_height,
        &roi_handle
    );

    if (roi_handle.cloud.points.empty())
    {
        cout << "[ERROR] No points in ROI! Cannot measure height." << endl;
        return 0.0;
    }

    // 步骤3: 对ROI内的点云测高
    int max_idx_in_roi = 0;
    double height = measureHeightFromBasePlane(
        &roi_handle,
        base_plane,
        &max_idx_in_roi
    );

    if (out_max_point_index)
    {
        *out_max_point_index = max_idx_in_roi;
    }

    cout << "[INFO] ===== Measurement Complete =====" << endl;
    cout << "[INFO] Final height: " << height << " mm" << endl;

    return height;
}


HEAD void CallingConvention createMaxPointMarker(
    PointCloudNativeHandle* in_handle,
    int max_point_index,
    double marker_radius,
    PointCloudNativeHandle* out_marker_handle)
{
    auto* in_pc = PointCloudData(in_handle);
    auto* out_marker_pc = PointCloudData(out_marker_handle);
    if (in_pc == nullptr || out_marker_pc == nullptr)
    {
        return;
    }

    if (in_pc->points.empty() || max_point_index < 0 ||
        max_point_index >= (int)in_pc->points.size())
    {
        cout << "[ERROR] Invalid input for marker creation!" << endl;
        return;
    }

    cout << "[INFO] Creating marker at point index " << max_point_index << endl;

    pcl::PointXYZ center = in_pc->points[max_point_index];
    out_marker_pc->clear();

    // 生成球形标记点（简化版：只生成一个小立方体的点）
    int points_per_side = 10;
    double step = marker_radius * 2.0 / points_per_side;

    for (int i = 0; i < points_per_side; i++)
    {
        for (int j = 0; j < points_per_side; j++)
        {
            for (int k = 0; k < points_per_side; k++)
            {
                double dx = -marker_radius + i * step;
                double dy = -marker_radius + j * step;
                double dz = -marker_radius + k * step;

                // 只保留在球内的点
                double dist = sqrt(dx * dx + dy * dy + dz * dz);
                if (dist <= marker_radius)
                {
                    pcl::PointXYZ marker_point;
                    marker_point.x = center.x + dx;
                    marker_point.y = center.y + dy;
                    marker_point.z = center.z + dz;
                    out_marker_pc->push_back(marker_point);
                }
            }
        }
    }

    cout << "[INFO] Marker created with " << out_marker_pc->points.size()
        << " points" << endl;
    InvalidatePointCloudBounds(out_marker_handle);
}


/*====================================================================================
 *                           平整度计算功能
 *====================================================================================*/

HEAD double CallingConvention calculateFlatness(
    PointCloudNativeHandle* in_handle,
    int flatness_type,
    float* out_plane)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || out_plane == nullptr)
    {
        return 0.0;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return 0.0;
    }

    cout << "[INFO] Calculating flatness (type=" << flatness_type << ") for "
        << in_pc->points.size() << " points..." << endl;

    // 第一步: 使用RANSAC拟合平面
    pcl::ModelCoefficients::Ptr coefficients(new pcl::ModelCoefficients);
    pcl::PointIndices::Ptr inliers(new pcl::PointIndices);

    pcl::SACSegmentation<pcl::PointXYZ> seg;
    seg.setOptimizeCoefficients(true);
    seg.setModelType(pcl::SACMODEL_PLANE);
    seg.setMethodType(pcl::SAC_RANSAC);
    seg.setDistanceThreshold(0.5); // 0.5mm阈值
    seg.setInputCloud(in_pc->makeShared());
    seg.segment(*inliers, *coefficients);

    if (inliers->indices.size() == 0)
    {
        cout << "[ERROR] Could not estimate a planar model!" << endl;
        return 0.0;
    }

    // 复制平面系数
    out_plane[0] = coefficients->values[0];
    out_plane[1] = coefficients->values[1];
    out_plane[2] = coefficients->values[2];
    out_plane[3] = coefficients->values[3];

    cout << "[INFO] Fitted plane: " << out_plane[0] << "x + "
        << out_plane[1] << "y + " << out_plane[2] << "z + "
        << out_plane[3] << " = 0" << endl;

    // 归一化平面系数
    normalizePlaneCoefficients(out_plane);

    // 第二步: 计算所有点到平面的距离
    std::vector<double> distances;
    distances.reserve(in_pc->points.size());

    double sum_squared = 0.0;

    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double point[3] = {
            in_pc->points[i].x,
            in_pc->points[i].y,
            in_pc->points[i].z
        };

        double dist = fabs(pointToPlaneDistance(point, out_plane));
        distances.push_back(dist);
        sum_squared += dist * dist;
    }

    // 第三步: 根据不同方法计算平整度
    double flatness = 0.0;

    switch (flatness_type)
    {
    case 0: // 峰谷值法
    {
        double min_dist = *std::min_element(distances.begin(), distances.end());
        double max_dist = *std::max_element(distances.begin(), distances.end());
        flatness = max_dist - min_dist;
        cout << "[INFO] Flatness (Peak-to-Valley): " << flatness << " mm" << endl;
        cout << "  Min distance: " << min_dist << " mm" << endl;
        cout << "  Max distance: " << max_dist << " mm" << endl;
        break;
    }

    case 1: // RMS法
        flatness = sqrt(sum_squared / distances.size());
        cout << "[INFO] Flatness (RMS): " << flatness << " mm" << endl;
        break;

    case 2: // 标准差法
    {
        double mean = 0.0;
        for (size_t i = 0; i < distances.size(); i++)
        {
            mean += distances[i];
        }
        mean /= distances.size();

        double variance = 0.0;
        for (size_t i = 0; i < distances.size(); i++)
        {
            variance += (distances[i] - mean) * (distances[i] - mean);
        }
        variance /= distances.size();

        flatness = sqrt(variance);
        cout << "[INFO] Flatness (Std Deviation): " << flatness << " mm" << endl;
        cout << "  Mean deviation: " << mean << " mm" << endl;
        break;
    }

    default:
        cout << "[ERROR] Invalid flatness_type: " << flatness_type << endl;
        flatness = 0.0;
    }

    return flatness;
}


HEAD double CallingConvention calculateRegionalFlatness(
    PointCloudNativeHandle* in_handle,
    int grid_size,
    double* out_flatness_map,
    int* out_grid_point_counts)
{
    auto* in_pc = PointCloudData(in_handle);
    if (in_pc == nullptr || out_flatness_map == nullptr || grid_size <= 0)
    {
        return 0.0;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return 0.0;
    }

    cout << "[INFO] Calculating regional flatness with " << grid_size << "x" << grid_size << " grid..." << endl;

    // 先拟合整体平面
    float plane_coeffs[4];
    calculateFlatness(in_handle, 0, plane_coeffs); // 用峰谷值法拟合平面

    // 找到点云的X-Y边界
    pcl::PointXYZ min_pt, max_pt;
    if (EnsurePointCloudBounds(in_handle)) {
        min_pt = in_handle->minPoint;
        max_pt = in_handle->maxPoint;
    }

    double x_range = max_pt.x - min_pt.x;
    double y_range = max_pt.y - min_pt.y;
    double cell_size_x = x_range / grid_size;
    double cell_size_y = y_range / grid_size;

    cout << "[INFO] Grid info:" << endl;
    cout << "  X range: [" << min_pt.x << ", " << max_pt.x << "], cell size: " << cell_size_x << endl;
    cout << "  Y range: [" << min_pt.y << ", " << max_pt.y << "], cell size: " << cell_size_y << endl;

    // 初始化网格
    vector<vector<pcl::PointCloud<pcl::PointXYZ>>> grid_cells(grid_size);
    for (int i = 0; i < grid_size; i++)
    {
        grid_cells[i].resize(grid_size);
    }

    // 将点分配到网格
    for (size_t i = 0; i < in_pc->points.size(); i++)
    {
        double x = in_pc->points[i].x;
        double y = in_pc->points[i].y;

        int grid_x = (int)((x - min_pt.x) / cell_size_x);
        int grid_y = (int)((y - min_pt.y) / cell_size_y);

        // 边界处理
        if (grid_x >= grid_size) grid_x = grid_size - 1;
        if (grid_y >= grid_size) grid_y = grid_size - 1;
        if (grid_x < 0) grid_x = 0;
        if (grid_y < 0) grid_y = 0;

        grid_cells[grid_x][grid_y].push_back(in_pc->points[i]);
    }

    // 计算每个网格的平整度
    double max_flatness = 0.0;
    int empty_cells = 0;

    for (int i = 0; i < grid_size; i++)
    {
        for (int j = 0; j < grid_size; j++)
        {
            int idx = i * grid_size + j;
            int point_count = (int)grid_cells[i][j].points.size();

            if (out_grid_point_counts != NULL)
            {
                out_grid_point_counts[idx] = point_count;
            }

            if (point_count < 10) // 点数太少，无法计算
            {
                out_flatness_map[idx] = 0.0;
                empty_cells++;
                continue;
            }

            // 计算该网格内的平整度（使用峰谷值法）
            double min_z = DBL_MAX;
            double max_z = -DBL_MAX;

            for (size_t k = 0; k < grid_cells[i][j].points.size(); k++)
            {
                double point[3] = {
                    grid_cells[i][j].points[k].x,
                    grid_cells[i][j].points[k].y,
                    grid_cells[i][j].points[k].z
                };

                double dist = fabs(pointToPlaneDistance(point, plane_coeffs));

                if (dist < min_z) min_z = dist;
                if (dist > max_z) max_z = dist;
            }

            double cell_flatness = max_z - min_z;
            out_flatness_map[idx] = cell_flatness;

            if (cell_flatness > max_flatness)
            {
                max_flatness = cell_flatness;
            }
        }
    }

    cout << "[INFO] Regional flatness analysis complete:" << endl;
    cout << "  Total cells: " << grid_size * grid_size << endl;
    cout << "  Empty cells: " << empty_cells << endl;
    cout << "  Maximum regional flatness: " << max_flatness << " mm" << endl;

    // 输出前5个最不平整的区域
    cout << "[INFO] Top 5 roughest regions:" << endl;
    vector<pair<double, int>> flatness_indices;
    for (int i = 0; i < grid_size * grid_size; i++)
    {
        if (out_flatness_map[i] > 0)
        {
            flatness_indices.push_back(make_pair(out_flatness_map[i], i));
        }
    }
    sort(flatness_indices.begin(), flatness_indices.end(), greater<pair<double, int>>());

    for (int i = 0; i < min(5, (int)flatness_indices.size()); i++)
    {
        int idx = flatness_indices[i].second;
        int grid_x = idx / grid_size;
        int grid_y = idx % grid_size;
        cout << "  Grid[" << grid_x << "][" << grid_y << "]: "
            << flatness_indices[i].first << " mm" << endl;
    }

    return max_flatness;
}


/*====================================================================================
 *                           点云投影功能
 *====================================================================================*/

HEAD void CallingConvention projectPointsToPlane(
    PointCloudNativeHandle* in_handle,
    float* plane,
    PointCloudNativeHandle* out_handle)
{
    auto* in_pc = PointCloudData(in_handle);
    auto* out_pc = PointCloudData(out_handle);
    if (in_pc == nullptr || out_pc == nullptr || plane == nullptr)
    {
        return;
    }

    if (in_pc->points.empty())
    {
        cout << "[ERROR] Input point cloud is empty!" << endl;
        return;
    }

    cout << "[INFO] Projecting " << in_pc->points.size() << " points to plane..." << endl;

    // 使用PCL的投影滤波器
    pcl::ModelCoefficients::Ptr coefficients(new pcl::ModelCoefficients());
    coefficients->values.resize(4);
    coefficients->values[0] = plane[0];
    coefficients->values[1] = plane[1];
    coefficients->values[2] = plane[2];
    coefficients->values[3] = plane[3];

    pcl::ProjectInliers<pcl::PointXYZ> proj;
    proj.setModelType(pcl::SACMODEL_PLANE);
    proj.setInputCloud(in_pc->makeShared());
    proj.setModelCoefficients(coefficients);
    proj.filter(*out_pc);

    cout << "[INFO] Projection complete. Output " << out_pc->points.size() << " points" << endl;
    InvalidatePointCloudBounds(out_handle);
}


/*====================================================================================
 *                           多区域高度测量（进阶功能）
 *====================================================================================*/

HEAD double CallingConvention measureMultipleHeights(
    PointCloudNativeHandle* in_pc,
    float* base_plane,
    double clustering_distance,
    double* out_heights,
    int* out_num_protrusions,
    int max_protrusions)
{
    cout << "[INFO] Measuring multiple protrusions..." << endl;
    cout << "[WARNING] This is an advanced feature that requires Euclidean clustering." << endl;
    cout << "[TODO] Implementation requires euclideanCluster from segmentation module." << endl;

    // 这个功能需要结合欧式聚类算法，将凸起部分分成多个簇
    // 然后对每个簇分别计算高度
    // 具体实现需要调用 euclideanCluster 函数

    *out_num_protrusions = 0;
    return 0.0;
}


/*====================================================================================
 *                           热图生成（可视化辅助）
 *====================================================================================*/

HEAD void CallingConvention generateFlatnessHeatmap(
    PointCloudNativeHandle* in_pc,
    int grid_size,
    float* plane,
    PointCloudNativeHandle* out_heatmap_points)
{
    cout << "[INFO] Generating flatness heatmap..." << endl;
    cout << "[WARNING] This function is for visualization purposes." << endl;
    cout << "[TODO] For color mapping, consider using PointCloudXYZRGB type." << endl;

    // 生成热图点云（高度映射到颜色）
    // 这个功能更适合用PointXYZRGB类型实现
    // 目前只返回投影后的点云
    projectPointsToPlane(in_pc, plane, out_heatmap_points);
}

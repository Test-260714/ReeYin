#pragma once

#include "pcl_util.h"
#include <pcl/sample_consensus/model_types.h>
#include <pcl/sample_consensus/method_types.h>
#include <pcl/segmentation/sac_segmentation.h>
#include <pcl/ModelCoefficients.h>
#include <pcl/common/transforms.h>
#include <pcl/common/common.h>
#include <pcl/common/common_headers.h>
#include <pcl/filters/project_inliers.h>
#include <pcl/filters/extract_indices.h>
#include <vector>
#include <algorithm>
#include <cmath>
#include "interface/PointCloudNativeHandle.h"

using namespace std;

// 定义导出方式：以C语言的方式导出，因为C语言方式函数名保持不变
// Export declarations are kept in simple ASCII-only macros to avoid encoding-related parse issues.
#ifndef EXTERNC
#define EXTERNC extern "C"
#endif
#ifndef HEAD
#define HEAD EXTERNC __declspec(dllexport)
#endif
#ifndef CallingConvention
#define CallingConvention __stdcall
#endif


/*====================================================================================
 *                           测高功能模块
 *====================================================================================*/

 /*
 功能：计算点云中每个点到指定平面的有符号距离
 param[in] in_pc 输入点云对象指针
 param[in] plane_coeffs 平面方程系数数组[a, b, c, d]，平面方程为 ax+by+cz+d=0
 param[out] out_distances 输出距离数组，长度应与点云大小相同。正值表示点在平面法向量方向，负值表示相反方向
 param[out] out_statistics 输出统计信息数组[min, max, mean, stddev, range]，长度为5
 说明：该函数用于分析点云相对于基准面的高度分布情况
 调试信息：检查平面系数是否归一化，检查输出数组长度是否正确
 */
HEAD void CallingConvention calculatePointsToPlaneDistance(
    PointCloudNativeHandle* in_pc,
    float* plane_coeffs,
    double* out_distances,
    double* out_statistics
);
/*
功能：测量从基准平面到点云最高点的距离（绝对高度测量）
【已修复】修正了初始化和比较逻辑的bug
param[in] in_pc 输入点云对象指针
param[in] base_plane 基准平面系数[a, b, c, d]，代表底部基准面
param[out] max_point_index 最高点的索引号（可选，传入NULL则不输出）
return 返回从基准面到最高点的距离值（正值）
说明：用于测量如"底部+螺柱高度"的场景。自动找到距离基准面最远的点
应用场景：箱体底部基准面到螺柱顶端的高度测量
调试信息：输出找到的最高点坐标和索引，便于验证
*/
HEAD double CallingConvention measureHeightFromBasePlane(
    PointCloudNativeHandle* in_pc,
    float* base_plane,
    int* max_point_index
);

/*
功能：自动分离底面和凸起部分（如螺柱）
param[in] in_pc 输入完整点云（包含底面+凸起）
param[in] height_threshold 高度阈值，超过此高度的点被认为是凸起部分
param[in] base_plane 底面平面方程系数[a,b,c,d]
param[out] out_base 输出底面点云
param[out] out_protrusion 输出凸起部分点云
说明：基于到平面的距离进行分割。需要先用fitPlane拟合底面
应用场景：将扫描数据分为箱体底面和螺柱两部分，便于分别处理
调试信息：输出分离后的两部分点数，检查分离效果
*/
HEAD void CallingConvention separateBaseAndProtrusion(
    PointCloudNativeHandle* in_pc,
    float height_threshold,
    float* base_plane,
    PointCloudNativeHandle* out_base,
    PointCloudNativeHandle* out_protrusion
);


/*====================================================================================
 *                           【新增】ROI区域选择功能（模拟基恩士）
 *====================================================================================*/

 /*
 功能：根据XY平面的矩形ROI框过滤点云
 param[in] in_pc 输入完整点云
 param[in] roi_center_x ROI中心X坐标
 param[in] roi_center_y ROI中心Y坐标
 param[in] roi_width ROI宽度(X方向)
 param[in] roi_height ROI高度(Y方向)
 param[out] out_pc 输出ROI内的点云
 说明：模拟基恩士系统的ROI选择功能,只保留指定矩形区域内的点
 应用场景：在测高前先框选螺柱区域,排除底面和其他干扰
 示例：filterPointsByROI(cloud, 30.0, 40.0, 20.0, 20.0, roi_cloud);
      // 选择中心在(30,40)、大小为20x20的矩形区域
 */
HEAD void CallingConvention filterPointsByROI(
    PointCloudNativeHandle* in_pc,
    double roi_center_x,
    double roi_center_y,
    double roi_width,
    double roi_height,
    PointCloudNativeHandle* out_pc
);

/*
功能：自动检测凸起区域并建议ROI参数
param[in] in_pc 输入点云
param[in] base_plane 基准平面
param[in] height_threshold 高度阈值,超过此高度认为是凸起
param[out] out_roi_x 输出ROI中心X坐标
param[out] out_roi_y 输出ROI中心Y坐标
param[out] out_roi_width 输出ROI宽度
param[out] out_roi_height 输出ROI高度
return 返回检测到的凸起点数
说明：自动分析点云,找到凸起部分的边界,建议合适的ROI框
应用场景：不知道ROI参数时,自动检测螺柱位置
示例：int count = suggestROIForProtrusion(cloud, plane, 3.0, &x, &y, &w, &h);
*/
HEAD int CallingConvention suggestROIForProtrusion(
    PointCloudNativeHandle* in_pc,
    float* base_plane,
    double height_threshold,
    double* out_roi_x,
    double* out_roi_y,
    double* out_roi_width,
    double* out_roi_height
);

/*
功能：带ROI的完整测高流程（推荐使用）
param[in] in_pc 输入完整点云
param[in] base_plane 基准平面
param[in] use_auto_roi 是否自动检测ROI (true=自动, false=使用手动参数)
param[in] roi_x ROI中心X (use_auto_roi=false时使用)
param[in] roi_y ROI中心Y (use_auto_roi=false时使用)
param[in] roi_width ROI宽度 (use_auto_roi=false时使用)
param[in] roi_height ROI高度 (use_auto_roi=false时使用)
param[out] out_max_point_index 输出最高点索引
param[out] out_used_roi_x 输出实际使用的ROI中心X
param[out] out_used_roi_y 输出实际使用的ROI中心Y
param[out] out_used_roi_width 输出实际使用的ROI宽度
param[out] out_used_roi_height 输出实际使用的ROI高度
return 测量得到的高度值
说明：完整的测高流程,包括ROI选择、过滤、测量，模拟基恩士工作流程
应用场景：标准测高操作,推荐用此函数代替直接调用measureHeightFromBasePlane
示例1（自动ROI）：
  double height = measureHeightWithROI(cloud, plane, true, 0, 0, 0, 0,
                                       &idx, &x, &y, &w, &h);
示例2（手动ROI）：
  double height = measureHeightWithROI(cloud, plane, false, 30, 40, 20, 20,
                                       &idx, &x, &y, &w, &h);
*/
HEAD double CallingConvention measureHeightWithROI(
    PointCloudNativeHandle* in_pc,
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
    double* out_used_roi_height
);


/*====================================================================================
 *                           【新增】可视化标记功能
 *====================================================================================*/

 /*
 功能：创建最高点标记点云（用于在界面上显示）
 param[in] in_pc 输入点云
 param[in] max_point_index 最高点的索引
 param[in] marker_radius 标记点的半径（单位：mm）
 param[out] out_marker_pc 输出标记点云（球形点集）
 说明：在最高点周围生成一个小球形点云,用于可视化标记
 应用场景：在3D界面上高亮显示测量的最高点，类似基恩士的ROI框
 示例：createMaxPointMarker(cloud, max_idx, 2.0, marker_cloud);
      // 在最高点处创建半径2mm的标记球
 */
HEAD void CallingConvention createMaxPointMarker(
    PointCloudNativeHandle* in_pc,
    int max_point_index,
    double marker_radius,
    PointCloudNativeHandle* out_marker_pc
);


/*====================================================================================
 *                           平整度测量模块
 *====================================================================================*/

 /*
 功能：计算平面的平整度（多种算法）
 param[in] in_pc 输入点云（应为近似平面的点云）
 param[in] flatness_type 平整度计算类型：
                         0 = 峰谷值法 (Peak-to-Valley): max_distance - min_distance
                         1 = RMS法 (均方根): sqrt(Σ(di²)/n)
                         2 = 标准差法 (Std Deviation): σ
 param[out] out_plane 输出拟合的平面方程系数[a,b,c,d]
 return 返回计算的平整度值
 说明：
   - 峰谷值法：反映最大偏差，适合检测局部缺陷
   - RMS法：反映整体偏差水平，更稳定
   - 标准差法：统计学方法，剔除异常值的影响
 应用场景：平面扫描后评估表面平整度
 推荐参数：flatness_type=0 用于质量检测，flatness_type=1 用于整体评估
 调试信息：输出拟合平面的点数、最大/最小偏差值
 */
HEAD double CallingConvention calculateFlatness(
    PointCloudNativeHandle* in_pc,
    int flatness_type,
    float* out_plane
);

/*
功能：分区域计算平整度（网格化分析）
param[in] in_pc 输入点云
param[in] grid_size 将平面分割成 grid_size × grid_size 个网格区域
param[out] out_flatness_map 输出每个网格区域的平整度值，数组长度为 grid_size*grid_size
param[out] out_grid_point_counts 输出每个网格的点数统计，数组长度为 grid_size*grid_size（可选，传NULL则不输出）
return 返回整体平整度（所有区域的最大值）
说明：用于检测平面的局部不平整区域，如凹坑、凸起等
应用场景：大平面的质量检测，定位问题区域
推荐参数：grid_size=5~10，太大会导致每个格子点数过少
调试信息：输出各网格的点数、平整度分布，标记异常网格
*/
HEAD double CallingConvention calculateRegionalFlatness(
    PointCloudNativeHandle* in_pc,
    int grid_size,
    double* out_flatness_map,
    int* out_grid_point_counts
);


/*====================================================================================
 *                           辅助工具函数
 *====================================================================================*/

 /*
 功能：计算单个点到平面的有符号距离
 param[in] point 点坐标[x, y, z]
 param[in] plane 平面系数[a, b, c, d]，平面方程 ax+by+cz+d=0
 return 返回有符号距离。正值表示点在法向量方向，负值表示相反方向
 说明：距离公式 distance = (ax+by+cz+d) / sqrt(a²+b²+c²)
 注意：此函数假设平面系数已归一化，如未归一化会影响结果
 */
HEAD double CallingConvention pointToPlaneDistance(
    double* point,
    float* plane
);

/*
功能：将点云投影到指定平面
param[in] in_pc 输入点云
param[in] plane 目标平面系数[a,b,c,d]
param[out] out_pc 输出投影后的点云
说明：所有点沿法向量方向投影到平面上，常用于平整度分析的可视化
应用场景：可视化点云相对平面的偏差分布
*/
HEAD void CallingConvention projectPointsToPlane(
    PointCloudNativeHandle* in_pc,
    float* plane,
    PointCloudNativeHandle* out_pc
);

/*
功能：归一化平面方程系数
param[in/out] plane 平面系数[a,b,c,d]，会被直接修改为归一化后的值
说明：将法向量部分(a,b,c)归一化为单位向量，使 sqrt(a²+b²+c²)=1
      这样点到平面距离公式可简化为 distance = ax+by+cz+d
*/
HEAD void CallingConvention normalizePlaneCoefficients(float* plane);


/*====================================================================================
 *                           高级测量功能
 *====================================================================================*/

 /*
 功能：多区域高度测量（用于测量多个螺柱）
 param[in] in_pc 输入点云
 param[in] base_plane 基准平面
 param[in] clustering_distance 聚类距离阈值，用于分离不同的凸起
 param[out] out_heights 输出每个凸起的高度，数组需预分配足够空间
 param[out] out_num_protrusions 输出检测到的凸起数量
 param[in] max_protrusions 最大凸起数量限制
 return 返回最高的凸起高度
 说明：自动检测并测量所有凸起部分的高度
 应用场景：一次扫描多个螺柱的场景
 调试信息：输出各个凸起的点数、位置和高度
 */
HEAD double CallingConvention measureMultipleHeights(
    PointCloudNativeHandle* in_pc,
    float* base_plane,
    double clustering_distance,
    double* out_heights,
    int* out_num_protrusions,
    int max_protrusions
);

/*
功能：平整度热图生成（用于可视化）
param[in] in_pc 输入点云
param[in] grid_size 网格大小
param[in] plane 参考平面
param[out] out_heatmap_points 输出热图点云（带高度颜色映射）
说明：生成用于可视化的高度偏差热图，方便直观查看不平整区域
注意：这是一个辅助可视化函数，需要配合PointCloudXYZRGB使用
*/
HEAD void CallingConvention generateFlatnessHeatmap(
    PointCloudNativeHandle* in_pc,
    int grid_size,
    float* plane,
    PointCloudNativeHandle* out_heatmap_points
);

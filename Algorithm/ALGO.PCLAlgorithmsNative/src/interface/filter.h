#pragma once
#include <iostream>
#include <pcl/filters/uniform_sampling.h>
#include <pcl/filters/radius_outlier_removal.h>
#include <pcl/filters/extract_indices.h>
#include <pcl/filters/uniform_sampling.h>
#include <pcl/filters/passthrough.h>
#include <pcl/filters/voxel_grid.h>
#include <pcl/filters/approximate_voxel_grid.h>
#include <pcl/filters/statistical_outlier_removal.h>
#include <pcl/common/common.h>
#include <pcl/common/common_headers.h>
#include "interface/PointCloudNativeHandle.h"

using namespace std;
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

/*
功能：对点云进行体素下采样
param[in] in_pc 目标点云对象的指针
param[in] leaf_size 体素下采样叶子尺寸，该值越大，则采样后点云越稀疏
param[out] out_pc 结果点云对象的指针
*/
HEAD void CallingConvention voxelDownSample(PointCloudNativeHandle* in_pc, double leaf_size,
	                                        PointCloudNativeHandle* out_pc);

/*
功能：对点云进行近似体素下采样
param[in] in_pc 目标点云对象的指针
param[in] leaf_size 体素下采样叶子尺寸，该值越大，则采样后点云越稀疏
param[out] out_pc 结果点云对象的指针
*/
HEAD void CallingConvention approximateVoxelDownSample(PointCloudNativeHandle* in_pc, double leaf_size,
	                                                   PointCloudNativeHandle* out_pc);

/*
功能：对点云进行均匀下采样
param[in] in_pc 目标点云对象的指针
param[in] radius 均匀下采样分辨率参数，该值越大，则采样后点云越稀疏
param[out] out_pc 结果点云对象的指针
*/
HEAD void CallingConvention uniformDownSample(PointCloudNativeHandle* in_pc, double radius,
	                                          PointCloudNativeHandle* out_pc);

/*
功能：对点云进行直通滤波
param[in] in_pc 目标点云对象的指针
param[in] axis_name 选哪个轴进行过滤
param[in] limit_min 过滤区间的最小值
param[in] limit_max 过滤区间的最大值
param[in] negative 输出是否反向，若为true，则输出[limit_min,limit_max]之外的点，一般为false
param[out] out_pc 结果点云对象的指针
*/
HEAD void CallingConvention passThroughFilter(PointCloudNativeHandle* in_pc, char * axis_name,
	                                          float limit_min, float limit_max, int negative,
	                                          PointCloudNativeHandle* out_pc);

/*
功能：使用统计滤波对点云去噪，将离群点去除
param[in] in_x 待采样点云x坐标
param[in] in_y 待采样点云y坐标
param[in] in_z 待采样点云z坐标
param[in] neighbor_num 近邻数，根据点的数目选取合适的邻居数,默认50
param[in] thresh 点的平均距离在[μ-α×σ,μ＋α×σ]之外的点被剔除，thresh即是α
param[out] out_x 采样后所有点的X坐标
param[out] out_y 采样后所有点的Y坐标
param[out] out_z 采样后所有点的Z坐标
*/
HEAD void CallingConvention staFilter(PointCloudNativeHandle* in_pc,
	                                  int neighbor_num, float thresh,
	                                  PointCloudNativeHandle* out_pc);

/*
功能：半径滤波，将离群点去除
*/
HEAD void CallingConvention radiusFilter(PointCloudNativeHandle* in_pc,
	                                     double radius, int num_thresh,
	                                     PointCloudNativeHandle* out_pc);

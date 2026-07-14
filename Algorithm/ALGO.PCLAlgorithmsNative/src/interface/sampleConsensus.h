#pragma once

#include "pcl_util.h"
#include <pcl/sample_consensus/model_types.h>
#include <pcl/sample_consensus/method_types.h>
#include <pcl/segmentation/sac_segmentation.h>
#include <pcl/segmentation/extract_clusters.h>//欧式聚类
#include <pcl/ModelCoefficients.h>
#include <pcl/common/transforms.h>
#include <pcl/common/common.h>
#include <pcl/common/common_headers.h>
#include "interface/PointCloudNativeHandle.h"
#include <set>

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
功能：使用Ransac算法拟合分割后点云的平面，并返回平面倾斜角度
param[in] in_pc 目标点云对象指针
param[in] distance_thresh Ransac算法距离阈值
param[in] max_itera Ransac算法最大迭代次数
param[out] normal 拟合平面的方程系数，依次包含a、b、c、d四个值。方程形式为ax+by+cz+d=0
*/
HEAD float CallingConvention fitPlane(PointCloudNativeHandle* in_pc,
	                                  float distance_thresh, int max_itera, float * normal);

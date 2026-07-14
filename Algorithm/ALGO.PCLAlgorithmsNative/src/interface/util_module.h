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
功能：将倾斜的点云平面校正，也就是将点云平面变换至和水平面平行
param[in] in_pc 目标点云对象指针
param[in] normal 输入点云的法向量
param[out] out_pc 结果点云对象的指针
*/
HEAD void CallingConvention correctPlane(PointCloudNativeHandle* in_pc, float * normal,
	PointCloudNativeHandle* out_pc);


/*
功能：sigam法则剔除异常值
param[in] in_pc 目标点云对象指针
param[in] sigam_thresh   sigam法则阈值，一般为3
param[out] out_pc 结果点云对象指针
*/
HEAD void CallingConvention sigamFilter(PointCloudNativeHandle* in_pc, int sigam_thresh,
	PointCloudNativeHandle* out_pc);

/*
功能：将点云按照列方向存储，每一列代表一个测量圆周，并输出用户指定的n列测量圆周点云
param[in] in_pc 目标点云对象指针
param[in] num   指定输出多少列测量圆周点云，一般来说，n越大，越逼近端面全跳动
当n为整个点云的列数目，输出的就是整个点云，测量的结果实际上是端面全跳动。n一般默认为20
param[out] out_pc 结果点云对象指针
*/
HEAD void CallingConvention getRunoutPoints(PointCloudNativeHandle* in_pc, int num,
	PointCloudNativeHandle* out_pc);

/*
功能：用于自己测试，直接返回端跳结果，不对外提供接口
将点云按照列方向存储，每一列代表一个测量圆周，并输出用户指定的n列测量圆周点云
而且还会用西格玛法则剔除异常值，并返回最终的端面跳动
param[in] in_pc 目标点云对象指针
param[in] num   指定输出多少列测量圆周点云，一般来说，n越大，越逼近端面全跳动
当n为整个点云的列数目，输出的就是整个点云，测量的结果实际上是端面全跳动。n一般默认为20
param[out] out_pc 结果点云对象指针
*/
HEAD float CallingConvention getRunoutPointsWithResult(PointCloudNativeHandle* in_pc, int num,
	PointCloudNativeHandle* out_pc);

/*
功能：计算端面跳动,并返回该点云中最小最大点的索引
param[in] in_pc 目标点云对象指针
*/
HEAD double CallingConvention calculateRunout(PointCloudNativeHandle* in_pc, int * indices);

/*
功能：根据点云索引，从一个点云对象中拷贝出来对应的点
*/
HEAD void CallingConvention copyPcBaseOnIndice(PointCloudNativeHandle* in_pc,PointCloudNativeHandle* out_pc);
HEAD void CallingConvention extractClusterPointCloud(
	PointCloudNativeHandle* in_pc,
	vector<pcl::PointIndices> * in_indices,
	int cluster_index,
	PointCloudNativeHandle* out_pc);

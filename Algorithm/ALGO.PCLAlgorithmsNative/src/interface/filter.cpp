#include "pch.h"

#include "filter.h"



//对点云进行体素下采样
HEAD void CallingConvention voxelDownSample(PointCloudNativeHandle* in_pc, double leaf_size,
	                                        PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::VoxelGrid<pcl::PointXYZ> voxel;
	voxel.setInputCloud(inputCloud->makeShared());
	voxel.setLeafSize(leaf_size, leaf_size, leaf_size);
	voxel.filter(*outputCloud);
	InvalidatePointCloudBounds(out_pc);

}
//对点云进行近似体素下采样
HEAD void CallingConvention approximateVoxelDownSample(PointCloudNativeHandle* in_pc, double leaf_size,
	                                                   PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::ApproximateVoxelGrid<pcl::PointXYZ> avoxel;
	avoxel.setInputCloud(inputCloud->makeShared());
	avoxel.setLeafSize(leaf_size, leaf_size, leaf_size);
	avoxel.filter(*outputCloud);
	InvalidatePointCloudBounds(out_pc);
}

//均匀下采样
HEAD void CallingConvention uniformDownSample(PointCloudNativeHandle* in_pc, double radius,
	PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::UniformSampling<pcl::PointXYZ> uniform;
	uniform.setInputCloud(inputCloud->makeShared());
	uniform.setRadiusSearch(radius);
	uniform.filter(*outputCloud);
	InvalidatePointCloudBounds(out_pc);

}

//直通滤波
HEAD void CallingConvention passThroughFilter(PointCloudNativeHandle* in_pc, char * axis_name,
	float limit_min, float limit_max, int negative,
	PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::PassThrough<pcl::PointXYZ> pass;
	pass.setInputCloud(inputCloud->makeShared());
	if (negative <= 0)
	{
		pass.setNegative(false);
		pass.setFilterFieldName(axis_name);
		pass.setFilterLimits(limit_min, limit_max);
		pass.filter(*outputCloud);
	}
	else
	{
		pass.setNegative(true);
		pass.setFilterFieldName(axis_name);
		pass.setFilterLimits(limit_min, limit_max);
		pass.filter(*outputCloud);
	}
	InvalidatePointCloudBounds(out_pc);
}
//统计滤波
HEAD void CallingConvention staFilter(PointCloudNativeHandle* in_pc,
	int neighbor_num, float thresh,
	PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::StatisticalOutlierRemoval<pcl::PointXYZ> sta;
	sta.setInputCloud(inputCloud->makeShared());
	sta.setMeanK(neighbor_num);
	sta.setStddevMulThresh(thresh);
	sta.filter(*outputCloud);
	InvalidatePointCloudBounds(out_pc);

}

/*
功能：半径滤波，将离群点去除
*/
HEAD void CallingConvention radiusFilter(PointCloudNativeHandle* in_pc,
	double radius, int num_thresh,
	PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr) {
		return;
	}

	pcl::RadiusOutlierRemoval<pcl::PointXYZ> ror;
	ror.setInputCloud(inputCloud->makeShared());
	ror.setRadiusSearch(radius);
	ror.setMinNeighborsInRadius(num_thresh);
	ror.filter(*outputCloud);
	InvalidatePointCloudBounds(out_pc);
}

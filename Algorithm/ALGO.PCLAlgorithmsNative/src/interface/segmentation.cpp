#include "pch.h"

#include "segmentation.h"

//原始的区域生长
HEAD void CallingConvention oriGrowRegion(PointCloudNativeHandle* in_pc,
	int neighbor_num, float smooth_thresh, float curva_thresh,
	int MinClusterSize, int MaxClusterSize,
	vector<pcl::PointIndices> * out_indice)
{
	auto* inputCloud = PointCloudData(in_pc);
	if (inputCloud == nullptr || out_indice == nullptr)
	{
		return;
	}

	out_indice->clear();

	//计算点云的法向量
	pcl::PointCloud<pcl::Normal>::Ptr normals(new pcl::PointCloud<pcl::Normal>());
	pcl::search::KdTree<pcl::PointXYZ>::Ptr tree(new pcl::search::KdTree<pcl::PointXYZ>());

	pcl::NormalEstimation<pcl::PointXYZ, pcl::Normal> ne;
	ne.setInputCloud(inputCloud->makeShared());
	ne.setSearchMethod(tree);
	ne.setKSearch(neighbor_num);
	ne.compute(*normals);

	//区域生长
	pcl::RegionGrowing<pcl::PointXYZ, pcl::Normal> rg;
	rg.setSearchMethod(tree);
	rg.setInputCloud(inputCloud->makeShared());
	rg.setInputNormals(normals);
	rg.setMinClusterSize(MinClusterSize);
	rg.setMaxClusterSize(MaxClusterSize);
	rg.setCurvatureThreshold(curva_thresh);
	rg.setNumberOfNeighbours(neighbor_num);
	rg.setSmoothnessThreshold(smooth_thresh / 180.0 * 3.14159265358979323846);//要求是弧度，输入的是角度，所以转换一下

	//提取聚类后的点簇
	//vector<pcl::PointIndices> cluster;
	rg.extract(*out_indice);

}

//欧式聚类
HEAD void CallingConvention euclideanCluster(PointCloudNativeHandle* in_pc,
	double distance_thresh, int MinClusterSize, int MaxClusterSize,
	vector<pcl::PointIndices> * out_indice)
{
	auto* inputCloud = PointCloudData(in_pc);
	if (inputCloud == nullptr || out_indice == nullptr)
	{
		return;
	}

	out_indice->clear();

	pcl::search::KdTree<pcl::PointXYZ>::Ptr tree(new pcl::search::KdTree<pcl::PointXYZ>());
	tree->setInputCloud(inputCloud->makeShared());

	pcl::EuclideanClusterExtraction<pcl::PointXYZ> ec;
	ec.setClusterTolerance(distance_thresh);
	ec.setMinClusterSize(MinClusterSize);
	ec.setMaxClusterSize(MaxClusterSize);
	ec.setSearchMethod(tree);
	ec.setInputCloud(inputCloud->makeShared());
	ec.extract(*out_indice);

}

//封装后的区域生长，直接返回点数的最多的平面
HEAD void CallingConvention modifiedGrowRegion(PointCloudNativeHandle* in_pc,
	int neighbor_num, float smooth_thresh, float curva_thresh,
	int MinClusterSize, int MaxClusterSize,
	PointCloudNativeHandle* out_pc)
{
	auto* inputCloud = PointCloudData(in_pc);
	auto* outputCloud = PointCloudData(out_pc);
	if (inputCloud == nullptr || outputCloud == nullptr)
	{
		return;
	}

	outputCloud->clear();
	InvalidatePointCloudBounds(out_pc);

	//计算点云的法向量
	pcl::PointCloud<pcl::Normal>::Ptr normals(new pcl::PointCloud<pcl::Normal>());
	pcl::search::KdTree<pcl::PointXYZ>::Ptr tree(new pcl::search::KdTree<pcl::PointXYZ>());

	pcl::NormalEstimation<pcl::PointXYZ, pcl::Normal> ne;
	ne.setInputCloud(inputCloud->makeShared());
	ne.setSearchMethod(tree);
	ne.setKSearch(neighbor_num);
	ne.compute(*normals);
	
	//区域生长
	pcl::RegionGrowing<pcl::PointXYZ, pcl::Normal> rg;
	rg.setSearchMethod(tree);
	rg.setInputCloud(inputCloud->makeShared());
	rg.setInputNormals(normals);
	rg.setMinClusterSize(MinClusterSize);
	rg.setMaxClusterSize(MaxClusterSize);
	rg.setCurvatureThreshold(curva_thresh);
	rg.setNumberOfNeighbours(neighbor_num);
	rg.setSmoothnessThreshold(smooth_thresh / 180.0 * 3.14159265358979323846);//要求是弧度，输入的是角度，所以转换一下

	//提取聚类后的点簇
	vector<pcl::PointIndices> cluster;
	rg.extract(cluster);
	if (cluster.empty())
	{
		return;
	}

	//找到点数最多的平面
	int pos = getMaxPointCluster(cluster);
	if (pos < 0 || pos >= static_cast<int>(cluster.size()))
	{
		return;
	}

	pcl::copyPointCloud(*inputCloud, cluster[pos], *outputCloud);
	InvalidatePointCloudBounds(out_pc);


}

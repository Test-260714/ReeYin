using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudSegmentation
{
    public static PointCloudHandle ModifiedGrowRegion(
        PointCloudHandle input,
        int neighborNum,
        float smoothThreshold,
        float curvaThreshold,
        int minClusterSize,
        int maxClusterSize)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.ModifiedGrowRegion(
            input,
            neighborNum,
            smoothThreshold,
            curvaThreshold,
            minClusterSize,
            maxClusterSize,
            output);
        return output;
    }

    public static PointIndicesHandle GrowRegion(
        PointCloudHandle input,
        int neighborNum,
        float smoothThreshold,
        float curvaThreshold,
        int minClusterSize,
        int maxClusterSize)
    {
        var output = PointIndicesCollection.Create();
        PclAlgorithmsNative.OriginalGrowRegion(
            input,
            neighborNum,
            smoothThreshold,
            curvaThreshold,
            minClusterSize,
            maxClusterSize,
            output);
        return output;
    }

    public static PointIndicesHandle EuclideanCluster(
        PointCloudHandle input,
        double distanceThreshold,
        int minClusterSize,
        int maxClusterSize)
    {
        var output = PointIndicesCollection.Create();
        PclAlgorithmsNative.EuclideanCluster(
            input,
            distanceThreshold,
            minClusterSize,
            maxClusterSize,
            output);
        return output;
    }

    public static PointCloudHandle ExtractCluster(PointCloudHandle input, PointIndicesHandle indices, int clusterIndex)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.ExtractClusterPointCloud(input, indices, clusterIndex, output);
        return output;
    }
}

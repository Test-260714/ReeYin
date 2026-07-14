using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudFilters
{
    public static PointCloudHandle VoxelDownSample(PointCloudHandle input, double leafSize)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.VoxelDownSample(input, leafSize, output);
        return output;
    }

    public static PointCloudHandle ApproximateVoxelDownSample(PointCloudHandle input, double leafSize)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.ApproximateVoxelDownSample(input, leafSize, output);
        return output;
    }

    public static PointCloudHandle UniformDownSample(PointCloudHandle input, double radius)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.UniformDownSample(input, radius, output);
        return output;
    }

    public static PointCloudHandle PassThrough(
        PointCloudHandle input,
        string axisName,
        float limitMin,
        float limitMax,
        bool negative = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(axisName);

        var axis = axisName.Trim().ToLowerInvariant();
        if (axis is not ("x" or "y" or "z"))
        {
            throw new ArgumentOutOfRangeException(nameof(axisName), "axisName 只能是 x/y/z。");
        }

        var output = PointClouds.Create();
        PclAlgorithmsNative.PassThroughFilter(input, axis, limitMin, limitMax, negative ? 1 : 0, output);
        return output;
    }

    public static PointCloudHandle StatisticalOutlierRemoval(PointCloudHandle input, int neighborNum, float threshold)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.StatisticalFilter(input, neighborNum, threshold, output);
        return output;
    }

    public static PointCloudHandle RadiusOutlierRemoval(PointCloudHandle input, double radius, int minNeighbors)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.RadiusFilter(input, radius, minNeighbors, output);
        return output;
    }
}

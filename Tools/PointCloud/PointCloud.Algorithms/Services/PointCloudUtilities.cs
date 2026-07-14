using PointCloud.Algorithms.Dtos;
using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudUtilities
{
    public static PointCloudHandle SigmaFilter(PointCloudHandle input, int sigmaThreshold)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.SigmaFilter(input, sigmaThreshold, output);
        return output;
    }

    public static PointCloudHandle GetRunoutPoints(PointCloudHandle input, int count)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.GetRunoutPoints(input, count, output);
        return output;
    }

    public static (PointCloudHandle PointCloud, double Runout) GetRunoutPointsWithResult(PointCloudHandle input, int count)
    {
        var output = PointClouds.Create();
        var runout = PclAlgorithmsNative.GetRunoutPointsWithResult(input, count, output);
        return (output, runout);
    }

    public static RunoutResult CalculateRunout(PointCloudHandle input)
    {
        var indices = new int[2];
        var value = PclAlgorithmsNative.CalculateRunout(input, indices);

        return new RunoutResult
        {
            Value = value,
            MinPointIndex = indices[0],
            MaxPointIndex = indices[1],
        };
    }

    public static PointCloudHandle Copy(PointCloudHandle input)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.CopyPointCloud(input, output);
        return output;
    }

    public static PointCloudHandle Scale(PointCloudHandle input, double scaleX, double scaleY, double scaleZ, Point3d pivot)
    {
        ArgumentNullException.ThrowIfNull(input);

        var output = PointClouds.Create();
        float[] inputBuffer = PointClouds.ToInterleavedArray(input);
        float[] outputBuffer = PointCloudScaleMath.TransformInterleaved(
            inputBuffer,
            scaleX,
            scaleY,
            scaleZ,
            pivot.X,
            pivot.Y,
            pivot.Z);

        PointClouds.SetFromInterleaved(output, outputBuffer);
        return output;
    }

    public static PointCloudHandle ExtractClusterPointCloud(PointCloudHandle input, PointIndicesHandle indices, int clusterIndex)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.ExtractClusterPointCloud(input, indices, clusterIndex, output);
        return output;
    }
}

using PointCloud.Algorithms.Dtos;
using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudFitting
{
    public static PlaneModel FitPlane(PointCloudHandle input, float distanceThreshold, int maxIterations)
    {
        var coefficients = new float[4];
        var angle = PclAlgorithmsNative.FitPlane(input, distanceThreshold, maxIterations, coefficients);
        if (angle < 0)
        {
            throw new PointCloudInteropException("平面拟合失败。");
        }

        return new PlaneModel(
            coefficients[0],
            coefficients[1],
            coefficients[2],
            coefficients[3],
            angle);
    }

    public static PointCloudHandle CorrectPlane(PointCloudHandle input, PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        var output = PointClouds.Create();
        PclAlgorithmsNative.CorrectPlane(input, plane.ToArray(), output);
        return output;
    }

    public static PlaneModel Normalize(PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        var values = plane.ToArray();
        PclAlgorithmsNative.NormalizePlaneCoefficients(values);

        return new PlaneModel(values[0], values[1], values[2], values[3], plane.TiltAngleDegrees);
    }
}

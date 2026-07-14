using PointCloud.Algorithms.Dtos;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudTransformations
{
    public static PointCloudHandle Transform(PointCloudHandle input, PointCloudRigidTransform2D transform)
    {
        ArgumentNullException.ThrowIfNull(input);
        ThrowIfNonFinite(transform.X, nameof(transform.X));
        ThrowIfNonFinite(transform.Y, nameof(transform.Y));
        ThrowIfNonFinite(transform.Z, nameof(transform.Z));
        ThrowIfNonFinite(transform.YawDeg, nameof(transform.YawDeg));

        float[] source = PointClouds.ToInterleavedArray(input);
        float[] target = new float[source.Length];
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        for (int i = 0; i < source.Length; i += 3)
        {
            double x = source[i];
            double y = source[i + 1];
            double z = source[i + 2];
            target[i] = (float)(transform.X + cos * x - sin * y);
            target[i + 1] = (float)(transform.Y + sin * x + cos * y);
            target[i + 2] = (float)(transform.Z + z);
        }

        return CreateFromInterleaved(target);
    }

    public static PointCloudHandle Merge(IReadOnlyList<PointCloudHandle> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        int totalFloats = 0;
        foreach (PointCloudHandle input in inputs)
        {
            ArgumentNullException.ThrowIfNull(input);
            totalFloats += PointClouds.Count(input) * 3;
        }

        float[] merged = new float[totalFloats];
        int offset = 0;

        foreach (PointCloudHandle input in inputs)
        {
            float[] data = PointClouds.ToInterleavedArray(input);
            Array.Copy(data, 0, merged, offset, data.Length);
            offset += data.Length;
        }

        return CreateFromInterleaved(merged);
    }

    private static PointCloudHandle CreateFromInterleaved(float[] data)
    {
        PointCloudHandle output = PointClouds.Create();
        try
        {
            PointClouds.SetFromInterleaved(output, data);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static void ThrowIfNonFinite(double value, string paramName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Transform values must be finite.");
        }
    }
}

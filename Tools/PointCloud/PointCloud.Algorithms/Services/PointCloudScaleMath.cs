namespace PointCloud.Algorithms.Services;

public static class PointCloudScaleMath
{
    public static float[] TransformInterleaved(
        float[] xyzInterleaved,
        double scaleX,
        double scaleY,
        double scaleZ,
        double pivotX,
        double pivotY,
        double pivotZ)
    {
        ArgumentNullException.ThrowIfNull(xyzInterleaved);

        if (xyzInterleaved.Length % 3 != 0)
        {
            throw new ArgumentException("Interleaved XYZ buffer length must be a multiple of 3.", nameof(xyzInterleaved));
        }

        float[] output = new float[xyzInterleaved.Length];
        for (int i = 0; i < xyzInterleaved.Length; i += 3)
        {
            double x = xyzInterleaved[i];
            double y = xyzInterleaved[i + 1];
            double z = xyzInterleaved[i + 2];

            output[i] = (float)((x - pivotX) * scaleX + pivotX);
            output[i + 1] = (float)((y - pivotY) * scaleY + pivotY);
            output[i + 2] = (float)((z - pivotZ) * scaleZ + pivotZ);
        }

        return output;
    }
}

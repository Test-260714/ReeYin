using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using HalconDotNet;
using PointCloud.Interop;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

internal static class PointCloudFrameBuilder
{
    public static PointCloudHandle Build(ImageFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        FrameDescriptor descriptor = frame.Descriptor;
        Validation.PositiveFinite(descriptor.IntervalX, nameof(descriptor.IntervalX), descriptor.HeightImagePath);
        Validation.PositiveFinite(descriptor.IntervalY, nameof(descriptor.IntervalY), descriptor.HeightImagePath);
        Validation.PositiveFinite(descriptor.IntervalZ, nameof(descriptor.IntervalZ), descriptor.HeightImagePath);

        HTuple? rows = null;
        HTuple? cols = null;
        HTuple? heights = null;

        try
        {
            HOperatorSet.GetRegionPoints(frame.ValidMask, out rows, out cols);
            int pointCount = rows.Length;
            if (pointCount < 3)
                throw new InvalidOperationException("Point cloud registration requires at least 3 valid points.");

            HOperatorSet.GetGrayval(frame.HeightImage, rows, cols, out heights);

            double[] x = new double[pointCount];
            double[] y = new double[pointCount];
            double[] z = new double[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                x[i] = cols[i].D * descriptor.IntervalX;
                y[i] = rows[i].D * descriptor.IntervalY;
                z[i] = heights[i].D * descriptor.IntervalZ;
            }

            PointCloudHandle cloud = PointClouds.Create();
            try
            {
                PointClouds.SetFromSplitArrays(cloud, x, y, z);
                return cloud;
            }
            catch
            {
                cloud.Dispose();
                throw;
            }
        }
        finally
        {
            heights?.Dispose();
            cols?.Dispose();
            rows?.Dispose();
        }
    }
}

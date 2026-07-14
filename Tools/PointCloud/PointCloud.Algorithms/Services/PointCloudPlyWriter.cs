using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using PointCloud.Algorithms.Dtos;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudPlyWriter
{
    public static long SaveTransformed(
        string path,
        IReadOnlyList<PointCloudHandle> pointClouds,
        IReadOnlyList<PointCloudRigidTransform2D> transforms,
        double coordinateScale = 1.0,
        bool centerCoordinates = false,
        string? comment = null,
        int pointStride = 1,
        bool colorizeByZ = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(pointClouds);
        ArgumentNullException.ThrowIfNull(transforms);
        if (pointClouds.Count != transforms.Count)
            throw new ArgumentException("Point cloud and transform counts must match.", nameof(transforms));
        if (!double.IsFinite(coordinateScale) || coordinateScale <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(coordinateScale), coordinateScale, "PLY coordinate scale must be a finite positive value.");
        if (pointStride <= 0)
            throw new ArgumentOutOfRangeException(nameof(pointStride), pointStride, "PLY point stride must be positive.");

        long outputPointCount = CountOutputPoints(pointClouds, pointStride);
        if (outputPointCount <= 0)
            throw new InvalidOperationException("Cannot save an empty point cloud set to PLY.");

        double centerX = 0.0;
        double centerY = 0.0;
        double centerZ = 0.0;
        double minZ = 0.0;
        double maxZ = 0.0;

        if (centerCoordinates || colorizeByZ)
        {
            CalculateTransformedBounds(
                pointClouds,
                transforms,
                out double minX,
                out double minY,
                out minZ,
                out double maxX,
                out double maxY,
                out maxZ);

            if (centerCoordinates)
            {
                centerX = (minX + maxX) * 0.5;
                centerY = (minY + maxY) * 0.5;
                centerZ = (minZ + maxZ) * 0.5;
            }
        }

        using FileStream stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 1024 * 1024);
        using BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII);

        WritePlyHeader(writer, outputPointCount, comment, colorizeByZ);

        for (int i = 0; i < pointClouds.Count; i++)
        {
            WriteTransformedCloud(
                writer,
                pointClouds[i],
                transforms[i],
                coordinateScale,
                centerX,
                centerY,
                centerZ,
                pointStride,
                colorizeByZ,
                minZ,
                maxZ);
        }

        return outputPointCount;
    }

    public static long SaveTransformedFromFactory(
        string path,
        IReadOnlyList<long> pointCounts,
        IReadOnlyList<PointCloudRigidTransform2D> transforms,
        Func<int, PointCloudHandle> pointCloudFactory,
        double coordinateScale = 1.0,
        bool centerCoordinates = false,
        string? comment = null,
        int pointStride = 1,
        bool colorizeByZ = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(pointCounts);
        ArgumentNullException.ThrowIfNull(transforms);
        ArgumentNullException.ThrowIfNull(pointCloudFactory);
        if (pointCounts.Count != transforms.Count)
            throw new ArgumentException("Point count and transform counts must match.", nameof(transforms));
        if (!double.IsFinite(coordinateScale) || coordinateScale <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(coordinateScale), coordinateScale, "PLY coordinate scale must be a finite positive value.");
        if (pointStride <= 0)
            throw new ArgumentOutOfRangeException(nameof(pointStride), pointStride, "PLY point stride must be positive.");

        long outputPointCount = CountFactoryOutputPoints(pointCounts, pointStride);
        if (outputPointCount <= 0)
            throw new InvalidOperationException("Cannot save an empty point cloud set to PLY.");

        double centerX = 0.0;
        double centerY = 0.0;
        double centerZ = 0.0;
        double minZ = 0.0;
        double maxZ = 0.0;

        if (centerCoordinates || colorizeByZ)
        {
            CalculateTransformedBoundsFromFactory(
                pointCounts,
                transforms,
                pointCloudFactory,
                out double minX,
                out double minY,
                out minZ,
                out double maxX,
                out double maxY,
                out maxZ);

            if (centerCoordinates)
            {
                centerX = (minX + maxX) * 0.5;
                centerY = (minY + maxY) * 0.5;
                centerZ = (minZ + maxZ) * 0.5;
            }
        }

        using FileStream stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 1024 * 1024);
        using BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII);

        WritePlyHeader(writer, outputPointCount, comment, colorizeByZ);

        for (int i = 0; i < pointCounts.Count; i++)
        {
            using PointCloudHandle pointCloud = pointCloudFactory(i);
            ValidateFactoryPointCloudCount(pointCloud, pointCounts[i], i);
            WriteTransformedCloud(
                writer,
                pointCloud,
                transforms[i],
                coordinateScale,
                centerX,
                centerY,
                centerZ,
                pointStride,
                colorizeByZ,
                minZ,
                maxZ);
        }

        return outputPointCount;
    }

    private static long CountOutputPoints(IReadOnlyList<PointCloudHandle> pointClouds, int pointStride)
    {
        long outputPointCount = 0;
        foreach (PointCloudHandle pointCloud in pointClouds)
        {
            ArgumentNullException.ThrowIfNull(pointCloud);
            PointCloudBufferInfo bufferInfo = PointClouds.GetInterleavedBufferInfo(pointCloud);
            if (bufferInfo.Count == 0)
                continue;

            ValidateBufferInfo(bufferInfo);
            outputPointCount += GetStridedPointCount(bufferInfo.Count, pointStride);
        }

        return outputPointCount;
    }

    private static long CountFactoryOutputPoints(IReadOnlyList<long> pointCounts, int pointStride)
    {
        return pointCounts.Sum(count =>
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(pointCounts), count, "PLY point counts cannot be negative.");
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(pointCounts), count, "PLY point counts cannot exceed Int32.MaxValue because native point-cloud handles expose int counts.");

            return GetStridedPointCount(checked((int)count), pointStride);
        });
    }

    private static void CalculateTransformedBounds(
        IReadOnlyList<PointCloudHandle> pointClouds,
        IReadOnlyList<PointCloudRigidTransform2D> transforms,
        out double minX,
        out double minY,
        out double minZ,
        out double maxX,
        out double maxY,
        out double maxZ)
    {
        InitializeBounds(out minX, out minY, out minZ, out maxX, out maxY, out maxZ);

        for (int i = 0; i < pointClouds.Count; i++)
        {
            AccumulateTransformedBounds(
                pointClouds[i],
                transforms[i],
                ref minX,
                ref minY,
                ref minZ,
                ref maxX,
                ref maxY,
                ref maxZ);
        }

        ValidateTransformedBounds(minX, minY, minZ, maxX, maxY, maxZ);
    }

    private static void CalculateTransformedBoundsFromFactory(
        IReadOnlyList<long> pointCounts,
        IReadOnlyList<PointCloudRigidTransform2D> transforms,
        Func<int, PointCloudHandle> pointCloudFactory,
        out double minX,
        out double minY,
        out double minZ,
        out double maxX,
        out double maxY,
        out double maxZ)
    {
        InitializeBounds(out minX, out minY, out minZ, out maxX, out maxY, out maxZ);

        for (int i = 0; i < transforms.Count; i++)
        {
            using PointCloudHandle pointCloud = pointCloudFactory(i);
            ValidateFactoryPointCloudCount(pointCloud, pointCounts[i], i);
            AccumulateTransformedBounds(
                pointCloud,
                transforms[i],
                ref minX,
                ref minY,
                ref minZ,
                ref maxX,
                ref maxY,
                ref maxZ);
        }

        ValidateTransformedBounds(minX, minY, minZ, maxX, maxY, maxZ);
    }

    private static void ValidateFactoryPointCloudCount(PointCloudHandle pointCloud, long expectedPointCount, int index)
    {
        ArgumentNullException.ThrowIfNull(pointCloud);
        int expectedNativeCount = checked((int)expectedPointCount);
        int actualCount = PointClouds.Count(pointCloud);
        if (actualCount != expectedNativeCount)
        {
            throw new InvalidOperationException(
                $"Factory point cloud {index.ToString(CultureInfo.InvariantCulture)} contained {actualCount.ToString(CultureInfo.InvariantCulture)} points, but {expectedNativeCount.ToString(CultureInfo.InvariantCulture)} were declared.");
        }
    }

    private static void InitializeBounds(
        out double minX,
        out double minY,
        out double minZ,
        out double maxX,
        out double maxY,
        out double maxZ)
    {
        minX = double.PositiveInfinity;
        minY = double.PositiveInfinity;
        minZ = double.PositiveInfinity;
        maxX = double.NegativeInfinity;
        maxY = double.NegativeInfinity;
        maxZ = double.NegativeInfinity;
    }

    private static void AccumulateTransformedBounds(
        PointCloudHandle pointCloud,
        PointCloudRigidTransform2D transform,
        ref double minX,
        ref double minY,
        ref double minZ,
        ref double maxX,
        ref double maxY,
        ref double maxZ)
    {
        ArgumentNullException.ThrowIfNull(pointCloud);
        PointCloudBufferInfo bufferInfo = PointClouds.GetInterleavedBufferInfo(pointCloud);
        if (bufferInfo.Count == 0)
            return;

        ValidateBufferInfo(bufferInfo);
        int strideFloats = bufferInfo.StrideBytes / sizeof(float);
        const int chunkPointCount = 65536;
        float[] rawChunk = new float[chunkPointCount * strideFloats];
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        for (int start = 0; start < bufferInfo.Count; start += chunkPointCount)
        {
            int chunkCount = Math.Min(chunkPointCount, bufferInfo.Count - start);
            int floatCount = chunkCount * strideFloats;
            IntPtr sourcePtr = new IntPtr(bufferInfo.BufferPointer.ToInt64() + (long)start * bufferInfo.StrideBytes);
            Marshal.Copy(sourcePtr, rawChunk, 0, floatCount);

            for (int pointOffset = 0; pointOffset < chunkCount; pointOffset++)
            {
                int offset = pointOffset * strideFloats;
                TransformPoint(
                    rawChunk[offset],
                    rawChunk[offset + 1],
                    rawChunk[offset + 2],
                    transform,
                    cos,
                    sin,
                    out double x,
                    out double y,
                    out double z);

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                minZ = Math.Min(minZ, z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                maxZ = Math.Max(maxZ, z);
            }
        }
    }

    private static void ValidateTransformedBounds(
        double minX,
        double minY,
        double minZ,
        double maxX,
        double maxY,
        double maxZ)
    {
        if (!double.IsFinite(minX) || !double.IsFinite(minY) || !double.IsFinite(minZ) ||
            !double.IsFinite(maxX) || !double.IsFinite(maxY) || !double.IsFinite(maxZ))
        {
            throw new InvalidOperationException("Cannot calculate transformed PLY bounds.");
        }
    }

    private static void WriteTransformedCloud(
        BinaryWriter writer,
        PointCloudHandle pointCloud,
        PointCloudRigidTransform2D transform,
        double coordinateScale,
        double centerX,
        double centerY,
        double centerZ,
        int pointStride,
        bool colorizeByZ,
        double minZ,
        double maxZ)
    {
        PointCloudBufferInfo bufferInfo = PointClouds.GetInterleavedBufferInfo(pointCloud);
        if (bufferInfo.Count == 0)
            return;

        ValidateBufferInfo(bufferInfo);
        int strideFloats = bufferInfo.StrideBytes / sizeof(float);
        const int chunkPointCount = 65536;
        float[] rawChunk = new float[chunkPointCount * strideFloats];
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        for (int start = 0; start < bufferInfo.Count; start += chunkPointCount)
        {
            int chunkCount = Math.Min(chunkPointCount, bufferInfo.Count - start);
            int floatCount = chunkCount * strideFloats;
            IntPtr sourcePtr = new IntPtr(bufferInfo.BufferPointer.ToInt64() + (long)start * bufferInfo.StrideBytes);
            Marshal.Copy(sourcePtr, rawChunk, 0, floatCount);

            for (int i = 0; i < chunkCount; i++)
            {
                int pointIndex = start + i;
                if (pointIndex % pointStride != 0)
                    continue;

                int offset = i * strideFloats;
                TransformPoint(
                    rawChunk[offset],
                    rawChunk[offset + 1],
                    rawChunk[offset + 2],
                    transform,
                    cos,
                    sin,
                    out double x,
                    out double y,
                    out double z);

                writer.Write((float)((x - centerX) * coordinateScale));
                writer.Write((float)((y - centerY) * coordinateScale));
                writer.Write((float)((z - centerZ) * coordinateScale));
                if (colorizeByZ)
                    WriteHeightColor(writer, z, minZ, maxZ);
            }
        }
    }

    private static void WritePlyHeader(BinaryWriter writer, long outputPointCount, string? comment, bool colorizeByZ)
    {
        string safeComment = string.IsNullOrWhiteSpace(comment)
            ? "Generated by ElectroStaticChuck without PCL camera element"
            : comment.Replace('\r', ' ').Replace('\n', ' ');
        string header =
            "ply\n" +
            "format binary_little_endian 1.0\n" +
            $"comment {safeComment}\n" +
            $"element vertex {outputPointCount.ToString(CultureInfo.InvariantCulture)}\n" +
            "property float x\n" +
            "property float y\n" +
            "property float z\n" +
            (colorizeByZ
                ? "property uchar red\nproperty uchar green\nproperty uchar blue\n"
                : string.Empty) +
            "end_header\n";
        writer.Write(Encoding.ASCII.GetBytes(header));
    }

    private static void TransformPoint(
        float sourceX,
        float sourceY,
        float sourceZ,
        PointCloudRigidTransform2D transform,
        double cos,
        double sin,
        out double x,
        out double y,
        out double z)
    {
        if (!float.IsFinite(sourceX) || !float.IsFinite(sourceY) || !float.IsFinite(sourceZ))
            throw new InvalidOperationException("Cannot save PLY because the point cloud contains non-finite coordinates.");

        x = transform.X + cos * sourceX - sin * sourceY;
        y = transform.Y + sin * sourceX + cos * sourceY;
        z = transform.Z + sourceZ;

        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
            throw new InvalidOperationException("Cannot save PLY because a transformed coordinate is non-finite.");
    }

    private static void ValidateBufferInfo(PointCloudBufferInfo bufferInfo)
    {
        if (bufferInfo.BufferPointer == IntPtr.Zero)
            throw new InvalidOperationException("Point cloud buffer pointer is null.");
        if (bufferInfo.StrideBytes < sizeof(float) * 3 || bufferInfo.StrideBytes % sizeof(float) != 0)
            throw new InvalidOperationException($"Point cloud stride is invalid: {bufferInfo.StrideBytes}.");
    }

    private static long GetStridedPointCount(int pointCount, int pointStride)
    {
        return (pointCount + (long)pointStride - 1) / pointStride;
    }

    private static void WriteHeightColor(BinaryWriter writer, double z, double minZ, double maxZ)
    {
        double range = maxZ - minZ;
        double t = range > 1e-9 ? (z - minZ) / range : 0.5;
        t = Math.Clamp(t, 0.0, 1.0);

        byte red;
        byte green;
        byte blue;
        if (t < 0.5)
        {
            double local = t * 2.0;
            red = 0;
            green = (byte)Math.Round(255.0 * local);
            blue = (byte)Math.Round(255.0 * (1.0 - local));
        }
        else
        {
            double local = (t - 0.5) * 2.0;
            red = (byte)Math.Round(255.0 * local);
            green = (byte)Math.Round(255.0 * (1.0 - local));
            blue = 0;
        }

        writer.Write(red);
        writer.Write(green);
        writer.Write(blue);
    }
}

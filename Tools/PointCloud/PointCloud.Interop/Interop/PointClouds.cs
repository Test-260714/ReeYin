using System.Runtime.InteropServices;

namespace PointCloud.Interop;

public static class PointClouds
{
    public static PointCloudHandle Create()
    {
        var handle = Native.PclCoreNative.CreatePointCloud();
        EnsureValid(handle, nameof(handle));
        return handle;
    }

    public static PointCloudHandle Load(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.LoadPointCloudFile);
    }

    public static PointCloudHandle LoadDepthTiff(string path, DepthTiffLoadOptions options)
    {
        options.Validate();

        return LoadInternal(
            path,
            (sourcePath, pointCloud) => Native.PclCoreNative.LoadDepthTiffFile(
                sourcePath,
                pointCloud,
                options.SpacingX,
                options.SpacingY,
                options.SpacingZ,
                options.InvalidValue,
                options.UseInvalidValue ? 1 : 0));
    }

    public static PointCloudHandle LoadPly(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.LoadPlyFile);
    }

    public static PointCloudHandle LoadPcd(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.LoadPcdFile);
    }

    public static PointCloudHandle LoadObj(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.LoadObjFile);
    }

    public static PointCloudHandle LoadTxt(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.LoadTxtFile);
    }

    public static PointCloudHandle LoadStl(string path)
    {
        return LoadInternal(path, Native.PclCoreNative.StlToPointCloud);
    }

    public static void SavePcd(string path, PointCloudHandle pointCloud, bool binaryMode = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        EnsureValid(pointCloud, nameof(pointCloud));
        Native.PclCoreNative.SavePcdFile(path, pointCloud, binaryMode ? 1 : 0);
    }

    public static void SavePly(string path, PointCloudHandle pointCloud, bool binaryMode = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        EnsureValid(pointCloud, nameof(pointCloud));
        Native.PclCoreNative.SavePlyFile(path, pointCloud, binaryMode ? 1 : 0);
    }

    public static void SaveObj(string path, PointCloudHandle pointCloud)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        EnsureValid(pointCloud, nameof(pointCloud));
        Native.PclCoreNative.SaveObjFile(path, pointCloud);
    }

    public static PointCloudHandle Clone(PointCloudHandle source)
    {
        EnsureValid(source, nameof(source));

        var clone = Create();
        var data = ToInterleavedArray(source);
        SetFromInterleaved(clone, data);
        return clone;
    }

    public static int Count(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        return Native.PclCoreNative.CountPointCloud(pointCloud);
    }

    public static int Width(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        return Native.PclCoreNative.GetPointCloudWidth(pointCloud);
    }

    public static int Height(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        return Native.PclCoreNative.GetPointCloudHeight(pointCloud);
    }

    public static PointCloudBounds GetBounds(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));

        var result = new double[6];
        Native.PclCoreNative.GetMinMaxXYZ(pointCloud, result);
        return new PointCloudBounds(
            result[0],
            result[1],
            result[2],
            result[3],
            result[4],
            result[5]);
    }

    public static Point3d GetPoint(PointCloudHandle pointCloud, int index)
    {
        EnsureIndex(pointCloud, index);

        return new Point3d(
            Native.PclCoreNative.GetX(pointCloud, index),
            Native.PclCoreNative.GetY(pointCloud, index),
            Native.PclCoreNative.GetZ(pointCloud, index));
    }

    public static void SetPoint(PointCloudHandle pointCloud, int index, Point3d point)
    {
        EnsureIndex(pointCloud, index);

        Native.PclCoreNative.SetX(pointCloud, index, point.X);
        Native.PclCoreNative.SetY(pointCloud, index, point.Y);
        Native.PclCoreNative.SetZ(pointCloud, index, point.Z);
    }

    public static void Resize(PointCloudHandle pointCloud, int size)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        Native.PclCoreNative.Resize(pointCloud, size);
    }

    public static void Push(PointCloudHandle pointCloud, Point3d point)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        Native.PclCoreNative.Push(pointCloud, point.X, point.Y, point.Z);
    }

    public static void Pop(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        if (Count(pointCloud) == 0)
        {
            return;
        }

        Native.PclCoreNative.Pop(pointCloud);
    }

    public static void Clear(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        Native.PclCoreNative.Clear(pointCloud);
    }

    public static PointCloudBufferInfo GetInterleavedBufferInfo(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));

        return new PointCloudBufferInfo(
            Count(pointCloud),
            Native.PclCoreNative.GetPointCloudInterleavedStrideBytes(),
            Native.PclCoreNative.GetPointCloudInterleavedF32Ptr(pointCloud));
    }

    public static IntPtr GetInterleavedBufferPointer(PointCloudHandle pointCloud)
    {
        return GetInterleavedBufferInfo(pointCloud).BufferPointer;
    }

    public static int GetInterleavedStrideBytes()
    {
        return Native.PclCoreNative.GetPointCloudInterleavedStrideBytes();
    }

    public static (double[] X, double[] Y, double[] Z) CopyToSplitArrays(PointCloudHandle pointCloud)
    {
        EnsureValid(pointCloud, nameof(pointCloud));

        var count = Count(pointCloud);
        var x = new double[count];
        var y = new double[count];
        var z = new double[count];
        Native.PclCoreNative.CopyPointCloudToSplitF64(pointCloud, x, y, z, count);
        return (x, y, z);
    }

    public static Point3d[] CopyToPointArray(PointCloudHandle pointCloud)
    {
        var (x, y, z) = CopyToSplitArrays(pointCloud);
        var points = new Point3d[x.Length];
        for (var i = 0; i < x.Length; i++)
        {
            points[i] = new Point3d(x[i], y[i], z[i]);
        }

        return points;
    }

    public static float[] ToInterleavedArray(PointCloudHandle pointCloud)
    {
        var info = GetInterleavedBufferInfo(pointCloud);
        if (info.Count == 0 || info.BufferPointer == IntPtr.Zero)
        {
            return Array.Empty<float>();
        }

        var strideFloats = Math.Max(info.StrideBytes / sizeof(float), 3);
        var raw = new float[info.Count * strideFloats];
        Marshal.Copy(info.BufferPointer, raw, 0, raw.Length);

        if (strideFloats == 3)
        {
            return raw;
        }

        var packed = new float[info.Count * 3];
        for (var i = 0; i < info.Count; i++)
        {
            var rawOffset = i * strideFloats;
            var packedOffset = i * 3;
            packed[packedOffset] = raw[rawOffset];
            packed[packedOffset + 1] = raw[rawOffset + 1];
            packed[packedOffset + 2] = raw[rawOffset + 2];
        }

        return packed;
    }

    public static void SetFromSplitArrays(
        PointCloudHandle pointCloud,
        double[] x,
        double[] y,
        double[] z)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        ArgumentNullException.ThrowIfNull(z);

        if (x.Length != y.Length || y.Length != z.Length)
        {
            throw new ArgumentException("Split buffer X/Y/Z lengths must match.");
        }

        Native.PclCoreNative.SetPointCloudFromSplitF64(pointCloud, x, y, z, x.Length);
    }

    public static void SetFromInterleaved(
        PointCloudHandle pointCloud,
        float[] xyzInterleaved,
        int strideBytes = 0)
    {
        EnsureValid(pointCloud, nameof(pointCloud));
        ArgumentNullException.ThrowIfNull(xyzInterleaved);

        if (strideBytes <= 0)
        {
            strideBytes = sizeof(float) * 3;
        }

        if (strideBytes % sizeof(float) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(strideBytes), "strideBytes must be a multiple of 4.");
        }

        var strideFloats = strideBytes / sizeof(float);
        if (strideFloats < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(strideBytes), "strideBytes must be at least 12 bytes.");
        }

        if (xyzInterleaved.Length % strideFloats != 0)
        {
            throw new ArgumentException("Interleaved buffer length does not match strideBytes.", nameof(xyzInterleaved));
        }

        var count = xyzInterleaved.Length / strideFloats;
        Native.PclCoreNative.SetPointCloudFromInterleavedF32(pointCloud, xyzInterleaved, count, strideBytes);
    }

    public static PointCloudResourceRef RegisterResource(PointCloudHandle pointCloud, string? name = null)
    {
        return PointCloudResourceRegistry.RegisterCloud(pointCloud, name);
    }

    public static PointCloudResourceRef RegisterResourceClone(PointCloudHandle pointCloud, string? name = null)
    {
        return PointCloudResourceRegistry.RegisterCloudClone(pointCloud, name);
    }

    private static void EnsureIndex(PointCloudHandle pointCloud, int index)
    {
        EnsureValid(pointCloud, nameof(pointCloud));

        var count = Count(pointCloud);
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside point cloud range [0, {count - 1}].");
        }
    }

    private static void EnsureValid(PointCloudHandle? handle, string paramName, string? message = null)
    {
        if (handle is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (handle.IsClosed || handle.IsInvalid)
        {
            throw new PointCloudInteropException(message ?? $"Handle '{paramName}' is invalid or already released.");
        }
    }

    private static PointCloudHandle LoadInternal(string path, Func<string, PointCloudHandle, int> loader)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var handle = Create();
        try
        {
            if (loader(path, handle) == 0)
            {
                throw new PointCloudInteropException($"Unable to load point cloud file: {path}");
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static PointCloudHandle LoadInternal(string path, Action<string, PointCloudHandle> loader)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var handle = Create();
        try
        {
            loader(path, handle);
            if (Count(handle) <= 0)
            {
                throw new PointCloudInteropException($"Unable to load point cloud file: {path}");
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }
}

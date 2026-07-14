namespace PointCloud.Interop;

public static class PointIndicesCollection
{
    public static PointIndicesHandle Create()
    {
        var handle = Native.PclCoreNative.CreatePointIndices();
        EnsureValid(handle, nameof(handle));
        return handle;
    }

    public static int Count(PointIndicesHandle pointIndices)
    {
        EnsureValid(pointIndices, nameof(pointIndices));
        return Native.PclCoreNative.CountPointIndices(pointIndices);
    }

    public static int GetClusterSize(PointIndicesHandle pointIndices, int clusterIndex)
    {
        EnsureValid(pointIndices, nameof(pointIndices));

        var clusterCount = Count(pointIndices);
        if (clusterIndex < 0 || clusterIndex >= clusterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(clusterIndex), $"索引 {clusterIndex} 超出聚类范围 [0, {clusterCount - 1}]。");
        }

        return Native.PclCoreNative.GetSizeOfIndice(pointIndices, clusterIndex);
    }

    public static PointIndicesResourceRef RegisterResource(PointIndicesHandle pointIndices, string? name = null)
    {
        return PointCloudResourceRegistry.RegisterIndices(pointIndices, name);
    }

    private static void EnsureValid(PointIndicesHandle? handle, string paramName)
    {
        if (handle is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (handle.IsClosed || handle.IsInvalid)
        {
            throw new PointCloudInteropException($"句柄 {paramName} 无效或已经释放。");
        }
    }
}

using System.Collections.Concurrent;

namespace PointCloud.Interop;

public static class PointCloudResourceRegistry
{
    private sealed class RegisteredCloud
    {
        public RegisteredCloud(PointCloudResourceRef reference, PointCloudHandle handle)
        {
            Reference = reference;
            Handle = handle;
        }

        public PointCloudResourceRef Reference { get; }

        public PointCloudHandle Handle { get; }
    }

    private sealed class RegisteredIndices
    {
        public RegisteredIndices(PointIndicesResourceRef reference, PointIndicesHandle handle)
        {
            Reference = reference;
            Handle = handle;
        }

        public PointIndicesResourceRef Reference { get; }

        public PointIndicesHandle Handle { get; }
    }

    private static readonly ConcurrentDictionary<string, RegisteredCloud> Clouds = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, RegisteredIndices> Indices = new(StringComparer.Ordinal);

    public static PointCloudResourceRef RegisterCloud(PointCloudHandle handle, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handle);

        var reference = new PointCloudResourceRef
        {
            ResourceId = Guid.NewGuid().ToString("N"),
            Name = name,
            PointCount = PointClouds.Count(handle),
        };

        if (!Clouds.TryAdd(reference.ResourceId, new RegisteredCloud(reference, handle)))
        {
            throw new PointCloudInteropException("注册点云资源失败。");
        }

        return reference;
    }

    public static PointCloudResourceRef RegisterCloudClone(PointCloudHandle handle, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return RegisterCloud(PointClouds.Clone(handle), name);
    }

    public static PointIndicesResourceRef RegisterIndices(PointIndicesHandle handle, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(handle);

        var reference = new PointIndicesResourceRef
        {
            ResourceId = Guid.NewGuid().ToString("N"),
            Name = name,
            ClusterCount = PointIndicesCollection.Count(handle),
        };

        if (!Indices.TryAdd(reference.ResourceId, new RegisteredIndices(reference, handle)))
        {
            throw new PointCloudInteropException("注册索引资源失败。");
        }

        return reference;
    }

    public static PointCloudHandle GetCloud(PointCloudResourceRef reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!TryGetCloud(reference, out var handle))
        {
            throw new PointCloudInteropException($"找不到点云资源: {reference.ResourceId}");
        }

        return handle;
    }

    public static PointIndicesHandle GetIndices(PointIndicesResourceRef reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!TryGetIndices(reference, out var handle))
        {
            throw new PointCloudInteropException($"找不到索引资源: {reference.ResourceId}");
        }

        return handle;
    }

    public static bool TryGetCloud(PointCloudResourceRef? reference, out PointCloudHandle handle)
    {
        if (reference is not null &&
            !string.IsNullOrWhiteSpace(reference.ResourceId) &&
            Clouds.TryGetValue(reference.ResourceId, out var entry))
        {
            handle = entry.Handle;
            return !handle.IsClosed && !handle.IsInvalid;
        }

        handle = null!;
        return false;
    }

    public static bool TryGetIndices(PointIndicesResourceRef? reference, out PointIndicesHandle handle)
    {
        if (reference is not null &&
            !string.IsNullOrWhiteSpace(reference.ResourceId) &&
            Indices.TryGetValue(reference.ResourceId, out var entry))
        {
            handle = entry.Handle;
            return !handle.IsClosed && !handle.IsInvalid;
        }

        handle = null!;
        return false;
    }

    public static bool RemoveCloud(PointCloudResourceRef? reference)
    {
        if (reference is null || string.IsNullOrWhiteSpace(reference.ResourceId))
        {
            return false;
        }

        if (!Clouds.TryRemove(reference.ResourceId, out var entry))
        {
            return false;
        }

        entry.Handle.Dispose();
        return true;
    }

    public static bool RemoveIndices(PointIndicesResourceRef? reference)
    {
        if (reference is null || string.IsNullOrWhiteSpace(reference.ResourceId))
        {
            return false;
        }

        if (!Indices.TryRemove(reference.ResourceId, out var entry))
        {
            return false;
        }

        entry.Handle.Dispose();
        return true;
    }

    public static void ClearAll()
    {
        foreach (var key in Clouds.Keys.ToArray())
        {
            if (Clouds.TryRemove(key, out var cloud))
            {
                cloud.Handle.Dispose();
            }
        }

        foreach (var key in Indices.Keys.ToArray())
        {
            if (Indices.TryRemove(key, out var indices))
            {
                indices.Handle.Dispose();
            }
        }
    }
}

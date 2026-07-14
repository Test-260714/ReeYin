namespace PointCloud.Interop;

public sealed class PointIndicesResourceRef
{
    public string ResourceId { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int ClusterCount { get; set; }
}

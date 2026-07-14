namespace PointCloud.Interop;

public sealed class PointCloudResourceRef
{
    public string ResourceId { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int PointCount { get; set; }
}

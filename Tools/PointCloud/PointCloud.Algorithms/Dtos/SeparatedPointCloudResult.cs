using PointCloud.Interop;

namespace PointCloud.Algorithms.Dtos;

public sealed class SeparatedPointCloudResult
{
    public PointCloudHandle BasePointCloud { get; set; } = null!;

    public PointCloudHandle ProtrusionPointCloud { get; set; } = null!;
}

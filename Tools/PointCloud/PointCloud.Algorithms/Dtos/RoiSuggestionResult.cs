namespace PointCloud.Algorithms.Dtos;

public sealed class RoiSuggestionResult
{
    public int ProtrusionPointCount { get; set; }

    public RoiRectangle Roi { get; set; } = new();
}

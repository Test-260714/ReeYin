namespace PointCloud.Algorithms.Dtos;

public sealed class MultipleHeightMeasurementResult
{
    public double MaxHeight { get; set; }

    public int ProtrusionCount { get; set; }

    public double[] Heights { get; set; } = Array.Empty<double>();
}

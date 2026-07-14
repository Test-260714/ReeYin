namespace PointCloud.Algorithms.Dtos;

public sealed class PointDistanceAnalysisResult
{
    public double[] Distances { get; set; } = Array.Empty<double>();

    public PointDistanceStatistics Statistics { get; set; } = new();
}

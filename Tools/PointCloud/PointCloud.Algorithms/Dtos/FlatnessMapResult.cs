namespace PointCloud.Algorithms.Dtos;

public sealed class FlatnessMapResult
{
    public int GridSize { get; set; }

    public double OverallFlatness { get; set; }

    public double[] FlatnessMap { get; set; } = Array.Empty<double>();

    public int[] PointCounts { get; set; } = Array.Empty<int>();
}

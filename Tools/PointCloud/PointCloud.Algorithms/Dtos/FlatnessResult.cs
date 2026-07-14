namespace PointCloud.Algorithms.Dtos;

public sealed class FlatnessResult
{
    public double Value { get; set; }

    public int FlatnessType { get; set; }

    public PlaneModel Plane { get; set; } = new();
}

namespace PointCloud.Algorithms.Dtos;

public sealed class PointDistanceStatistics
{
    public double Min { get; set; }

    public double Max { get; set; }

    public double Mean { get; set; }

    public double StandardDeviation { get; set; }

    public double Range { get; set; }

    internal static PointDistanceStatistics FromArray(double[] values)
    {
        return new PointDistanceStatistics
        {
            Min = values[0],
            Max = values[1],
            Mean = values[2],
            StandardDeviation = values[3],
            Range = values[4],
        };
    }
}

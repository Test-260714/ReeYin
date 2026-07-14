namespace PointCloud.Algorithms.Dtos;

public sealed class HeightMeasurementResult
{
    public double Height { get; set; }

    public int MaxPointIndex { get; set; }

    public RoiRectangle? UsedRoi { get; set; }
}

using PointCloud.Interop;

namespace PointCloud.Algorithms.Dtos;

public sealed class PointCloudWorkflowData
{
    public string? Stage { get; set; }

    public PointCloudResourceRef? PrimaryPointCloud { get; set; }

    public Dictionary<string, PointCloudResourceRef> PointClouds { get; set; } = new();

    public Dictionary<string, PointIndicesResourceRef> IndexSets { get; set; } = new();

    public Dictionary<string, PlaneModel> Planes { get; set; } = new();

    public Dictionary<string, RoiRectangle> RegionsOfInterest { get; set; } = new();

    public Dictionary<string, HeightMeasurementResult> Measurements { get; set; } = new();

    public Dictionary<string, double> Scalars { get; set; } = new();

    public Dictionary<string, string> Tags { get; set; } = new();
}

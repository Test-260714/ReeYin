namespace PointCloud.Algorithms.Dtos;

public sealed class PointCloudRegistrationOptions
{
    public int MaxIterations { get; set; } = 50;

    public double MaxCorrespondenceDistance { get; set; } = 1.0;

    public double TransformationEpsilon { get; set; } = 1e-6;

    public double FitnessEpsilon { get; set; } = 1e-6;

    public double MinInlierRatio { get; set; } = 0.5;

    public bool OptimizeX { get; set; } = true;

    public bool OptimizeY { get; set; } = true;

    public bool OptimizeZ { get; set; } = true;

    public bool OptimizeYawDeg { get; set; } = true;
}

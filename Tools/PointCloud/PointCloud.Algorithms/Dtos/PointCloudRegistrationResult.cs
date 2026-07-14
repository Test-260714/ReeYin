namespace PointCloud.Algorithms.Dtos;

public sealed class PointCloudRegistrationResult
{
    public bool Converged { get; init; }

    public PointCloudRigidTransform2D Transform { get; init; }

    public double Rmse { get; init; }

    public double InlierRatio { get; init; }

    public int InlierCount { get; init; }

    public int Iterations { get; init; }

    public string Message { get; init; } = string.Empty;
}

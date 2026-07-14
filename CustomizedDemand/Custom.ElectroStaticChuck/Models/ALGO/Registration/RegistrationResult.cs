using PointCloud.Algorithms.Dtos;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

public sealed class RegistrationResult
{
    private RegistrationResult(IReadOnlyList<PointCloudRigidTransform2D> transforms)
    {
        Transforms = transforms;
    }

    public IReadOnlyList<PointCloudRigidTransform2D> Transforms { get; }

    public static RegistrationResult FromTransforms(IReadOnlyList<PointCloudRigidTransform2D> transforms)
    {
        ArgumentNullException.ThrowIfNull(transforms);
        return new RegistrationResult(Array.AsReadOnly(transforms.ToArray()));
    }
}

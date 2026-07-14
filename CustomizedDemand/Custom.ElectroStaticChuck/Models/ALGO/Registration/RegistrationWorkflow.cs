using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using PointCloud.Algorithms.Dtos;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

public sealed class RegistrationWorkflow
{
    public RegistrationResult Register(CorrectedFrameSet corrected, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(context);
        FineRegistrationMode mode = context.Parameters.Registration.Fine.Mode;

        IReadOnlyList<PointCloudRigidTransform2D> transforms = mode switch
        {
            FineRegistrationMode.NoRegistration => new NoRegistrationTransformBuilder().Build(corrected, context),
            FineRegistrationMode.CoarseOnly => new SequentialRegistrationTransformBuilder().Build(corrected, context),
            FineRegistrationMode.CoarseAndFine => new SequentialRegistrationTransformBuilder().Build(corrected, context),
            _ => throw new ArgumentOutOfRangeException(
                nameof(FineRegistrationParameters.Mode),
                mode,
                $"Unsupported or unknown point cloud fine registration mode: {mode}.")
        };

        return RegistrationResult.FromTransforms(transforms);
    }
}

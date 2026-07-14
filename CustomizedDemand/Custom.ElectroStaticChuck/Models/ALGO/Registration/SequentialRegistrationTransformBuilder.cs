using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using PointCloud.Algorithms.Dtos;
using PointCloud.Algorithms.Services;
using PointCloud.Interop;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

internal sealed class SequentialRegistrationTransformBuilder
{
    public IReadOnlyList<PointCloudRigidTransform2D> Build(CorrectedFrameSet corrected, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Parameters);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration.Coarse);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration.Fine);
        ArgumentNullException.ThrowIfNull(context.Parameters.Sensor);

        IReadOnlyList<FrameDescriptor> descriptors = corrected.Frames.Frames;
        if (descriptors.Count == 0)
            throw new InvalidOperationException("Sequential registration requires at least one frame descriptor.");

        FineRegistrationParameters fineRegistration = context.Parameters.Registration.Fine;
        FineRegistrationMode fineMode = ValidateFineRegistrationMode(fineRegistration.Mode);
        bool useFineRegistration = fineMode == FineRegistrationMode.CoarseAndFine;

        FrameDescriptor baseDescriptor = descriptors[0];
        double baseX = GetNominalX(baseDescriptor);
        double baseY = GetNominalY(baseDescriptor);
        double spacing = Math.Max(baseDescriptor.IntervalX, baseDescriptor.IntervalY);
        ValidateStrictPositiveFinite(spacing, nameof(spacing));

        double icpLeafSize = 0.0;
        PointCloudRegistrationOptions? fineOptions = null;
        if (useFineRegistration)
        {
            icpLeafSize = CreateIcpDownSampleLeafSize(spacing, fineRegistration);
            fineOptions = CreateFineRegistrationOptions(spacing, fineRegistration);
        }

        CoarseImageSearchOptions coarseSearchOptions = CoarseImageRegistrationService.CreateCoarseImageSearchOptions(
            baseDescriptor.IntervalX,
            baseDescriptor.IntervalY,
            context.Parameters.Registration.Coarse);

        var transforms = new List<PointCloudRigidTransform2D>(descriptors.Count)
        {
            new(0.0, 0.0, 0.0, 0.0)
        };

        var coarseRegistration = new CoarseImageRegistrationService();
        PointCloudHandle? registrationTargetIcpCloud = null;
        ImageFrame? previousFrame = null;

        try
        {
            context.ReportProgress(new AlgoProgressEvent(
                ElectroStaticChuckStage.Registration,
                "preparing registration target",
                FrameIndex: 0,
                FrameCount: descriptors.Count));
            previousFrame = corrected.LoadFrame(0, context.Parameters.Sensor);

            if (useFineRegistration)
            {
                using PointCloudHandle rawTargetCloud = PointCloudFrameBuilder.Build(previousFrame);
                registrationTargetIcpCloud = PointCloudFilters.VoxelDownSample(rawTargetCloud, icpLeafSize);
                EnsureRegistrationCloudHasEnoughPoints(registrationTargetIcpCloud, "target", 0);
            }

            for (int i = 1; i < descriptors.Count; i++)
            {
                ImageFrame? currentFrame = null;
                try
                {
                    FrameDescriptor currentDescriptor = descriptors[i];
                    FrameDescriptor targetDescriptor = descriptors[i - 1];
                    context.ReportProgress(new AlgoProgressEvent(
                        ElectroStaticChuckStage.Registration,
                        "coarse registration",
                        FrameIndex: i,
                        FrameCount: descriptors.Count));
                    currentFrame = corrected.LoadFrame(i, context.Parameters.Sensor);

                    PointCloudRigidTransform2D initialTransform = new(
                        GetNominalX(currentDescriptor) - baseX,
                        GetNominalY(currentDescriptor) - baseY,
                        0.0,
                        0.0);

                    PointCloudRigidTransform2D targetInitialTransform = new(
                        GetNominalX(targetDescriptor) - baseX,
                        GetNominalY(targetDescriptor) - baseY,
                        0.0,
                        0.0);

                    PointCloudRigidTransform2D targetTransform = transforms[i - 1];
                    CoarseImageRegistrationResult coarseResult = coarseRegistration.EstimateCoarseTransformByHalconImageSearch(
                        currentFrame,
                        previousFrame!,
                        initialTransform,
                        targetInitialTransform,
                        targetTransform,
                        coarseSearchOptions,
                        context.Parameters.Sensor.InvalidValue);

                    if (!useFineRegistration)
                    {
                        transforms.Add(coarseResult.Transform);
                        previousFrame.Dispose();
                        previousFrame = currentFrame;
                        currentFrame = null;
                        continue;
                    }

                    PointCloudRegistrationResult registrationResult;
                    PointCloudHandle? sourceIcpCloud = null;
                    PointCloudHandle? nextRegistrationTargetIcpCloud = null;
                    try
                    {
                        context.ReportProgress(new AlgoProgressEvent(
                            ElectroStaticChuckStage.Registration,
                            "fine registration",
                            FrameIndex: i,
                            FrameCount: descriptors.Count));
                        using (PointCloudHandle sourceRawCloud = PointCloudFrameBuilder.Build(currentFrame))
                        {
                            sourceIcpCloud = PointCloudFilters.VoxelDownSample(sourceRawCloud, icpLeafSize);
                        }

                        EnsureRegistrationCloudHasEnoughPoints(sourceIcpCloud, "source", i);
                        EnsureRegistrationCloudHasEnoughPoints(registrationTargetIcpCloud!, "target", i - 1);

                        registrationResult = PointCloudRegistration.RegisterConstrainedIcp(
                            sourceIcpCloud,
                            registrationTargetIcpCloud!,
                            coarseResult.Transform,
                            fineOptions!);

                        nextRegistrationTargetIcpCloud = PointCloudTransformations.Transform(
                            sourceIcpCloud,
                            registrationResult.Transform);
                        registrationTargetIcpCloud!.Dispose();
                        registrationTargetIcpCloud = nextRegistrationTargetIcpCloud;
                        nextRegistrationTargetIcpCloud = null;
                    }
                    finally
                    {
                        nextRegistrationTargetIcpCloud?.Dispose();
                        sourceIcpCloud?.Dispose();
                    }

                    transforms.Add(registrationResult.Transform);
                    previousFrame.Dispose();
                    previousFrame = currentFrame;
                    currentFrame = null;
                }
                finally
                {
                    currentFrame?.Dispose();
                }
            }

            if (transforms.Count != descriptors.Count)
            {
                throw new InvalidOperationException(
                    $"Sequential registration transform count mismatch: Frames={descriptors.Count}, Transforms={transforms.Count}.");
            }

            return transforms;
        }
        finally
        {
            registrationTargetIcpCloud?.Dispose();
            previousFrame?.Dispose();
        }
    }

    private static FineRegistrationMode ValidateFineRegistrationMode(FineRegistrationMode mode)
    {
        return mode switch
        {
            FineRegistrationMode.CoarseOnly => mode,
            FineRegistrationMode.CoarseAndFine => mode,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported or unknown point cloud fine registration mode.")
        };
    }

    private static double CreateIcpDownSampleLeafSize(double spacing, FineRegistrationParameters fineParam)
    {
        ArgumentNullException.ThrowIfNull(fineParam);
        ValidateStrictPositiveFinite(spacing, nameof(spacing));
        ValidateStrictPositiveFinite(fineParam.IcpLeafSizeMultiplier, nameof(fineParam.IcpLeafSizeMultiplier));

        double leafSize = spacing * fineParam.IcpLeafSizeMultiplier;
        ValidateStrictPositiveFinite(leafSize, nameof(fineParam.IcpLeafSizeMultiplier));
        return leafSize;
    }

    private static PointCloudRegistrationOptions CreateFineRegistrationOptions(
        double spacing,
        FineRegistrationParameters fineParam)
    {
        ArgumentNullException.ThrowIfNull(fineParam);
        ValidateStrictPositiveFinite(spacing, nameof(spacing));
        if (fineParam.MaxIterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(fineParam.MaxIterations), fineParam.MaxIterations, "MaxIterations must be positive.");
        ValidateStrictPositiveFinite(fineParam.MaxCorrespondenceDistanceMultiplier, nameof(fineParam.MaxCorrespondenceDistanceMultiplier));
        ValidateStrictPositiveFinite(fineParam.MinMaxCorrespondenceDistance, nameof(fineParam.MinMaxCorrespondenceDistance));
        ValidateStrictPositiveFinite(fineParam.TransformationEpsilon, nameof(fineParam.TransformationEpsilon));
        ValidateStrictPositiveFinite(fineParam.FitnessEpsilon, nameof(fineParam.FitnessEpsilon));
        if (!double.IsFinite(fineParam.MinInlierRatio) || fineParam.MinInlierRatio <= 0.0 || fineParam.MinInlierRatio > 1.0)
            throw new ArgumentOutOfRangeException(nameof(fineParam.MinInlierRatio), fineParam.MinInlierRatio, "MinInlierRatio must be finite and inside (0, 1].");

        return new PointCloudRegistrationOptions
        {
            MaxIterations = fineParam.MaxIterations,
            MaxCorrespondenceDistance = Math.Max(
                spacing * fineParam.MaxCorrespondenceDistanceMultiplier,
                fineParam.MinMaxCorrespondenceDistance),
            TransformationEpsilon = fineParam.TransformationEpsilon,
            FitnessEpsilon = fineParam.FitnessEpsilon,
            MinInlierRatio = fineParam.MinInlierRatio,
            OptimizeX = fineParam.OptimizeX,
            OptimizeY = fineParam.OptimizeY,
            OptimizeZ = fineParam.OptimizeZ,
            OptimizeYawDeg = fineParam.OptimizeYawDeg
        };
    }

    private static void EnsureRegistrationCloudHasEnoughPoints(PointCloudHandle? pointCloud, string role, int index)
    {
        ArgumentNullException.ThrowIfNull(pointCloud);
        int count = PointClouds.Count(pointCloud);
        if (count < 3)
            throw new InvalidOperationException($"ICP {role} point cloud {index} has only {count} points after downsampling.");
    }

    private static double GetNominalX(FrameDescriptor descriptor)
    {
        return (descriptor.OffsetX + descriptor.CompensationX) * descriptor.IntervalX;
    }

    private static double GetNominalY(FrameDescriptor descriptor)
    {
        return (descriptor.OffsetY + descriptor.CompensationY) * descriptor.IntervalY;
    }

    private static void ValidateStrictPositiveFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite and positive.");
    }
}

using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using HalconDotNet;
using PointCloud.Algorithms.Dtos;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

public sealed class NoRegistrationTransformBuilder
{
    public IReadOnlyList<PointCloudRigidTransform2D> Build(CorrectedFrameSet corrected, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Parameters);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration.Fine);

        IReadOnlyList<FrameDescriptor> descriptors = corrected.Frames.Frames;
        PointCloudRigidTransform2D[] transforms = BuildNominalTransforms(descriptors, context);

        var fineParam = context.Parameters.Registration.Fine;
        if (!fineParam.NoRegistrationUseZCorrection)
            return transforms;

        ValidateZCorrectionParameters(fineParam);
        Validation.Finite(context.Parameters.Sensor.InvalidValue, nameof(context.Parameters.Sensor.InvalidValue), "NoRegistration");

        transforms[0] = new PointCloudRigidTransform2D(0.0, 0.0, 0.0, 0.0);
        ImageFrame? previousFrame = null;
        ImageFrame? currentFrame = null;
        try
        {
            context.ReportProgress(new AlgoProgressEvent(
                ElectroStaticChuckStage.Registration,
                "loading z-correction target",
                FrameIndex: 0,
                FrameCount: descriptors.Count));
            previousFrame = corrected.LoadFrame(0, context.Parameters.Sensor);

            for (int i = 1; i < descriptors.Count; i++)
            {
                context.ReportProgress(new AlgoProgressEvent(
                    ElectroStaticChuckStage.Registration,
                    "estimating z correction",
                    FrameIndex: i,
                    FrameCount: descriptors.Count));
                currentFrame = corrected.LoadFrame(i, context.Parameters.Sensor);
                double z = EstimateZCorrection(
                    currentFrame,
                    previousFrame,
                    transforms[i],
                    transforms[i - 1],
                    fineParam,
                    context.Parameters.Sensor.InvalidValue);

                transforms[i] = transforms[i] with { Z = z };
                previousFrame.Dispose();
                previousFrame = currentFrame;
                currentFrame = null;
            }
        }
        finally
        {
            currentFrame?.Dispose();
            previousFrame?.Dispose();
        }

        return transforms;
    }

    private static PointCloudRigidTransform2D[] BuildNominalTransforms(
        IReadOnlyList<FrameDescriptor> descriptors,
        ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Parameters);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration);
        ArgumentNullException.ThrowIfNull(context.Parameters.Registration.Fine);

        if (descriptors.Count == 0)
            throw new InvalidOperationException("NoRegistration requires at least one frame descriptor.");

        FrameDescriptor baseDescriptor = descriptors[0];
        double baseNominalX = GetNominalX(baseDescriptor);
        double baseNominalY = GetNominalY(baseDescriptor);
        var transforms = new PointCloudRigidTransform2D[descriptors.Count];

        for (int i = 0; i < descriptors.Count; i++)
        {
            FrameDescriptor descriptor = descriptors[i];
            context.ReportProgress(new AlgoProgressEvent(
                ElectroStaticChuckStage.Registration,
                "using nominal transform",
                FrameIndex: i,
                FrameCount: descriptors.Count));
            transforms[i] = new PointCloudRigidTransform2D(
                GetNominalX(descriptor) - baseNominalX,
                GetNominalY(descriptor) - baseNominalY,
                0.0,
                0.0);
        }

        return transforms;
    }

    private static void ValidateZCorrectionParameters(FineRegistrationParameters fineParam)
    {
        if (fineParam.NoRegistrationZSampleStepPixel <= 0)
            throw new ArgumentOutOfRangeException(nameof(fineParam.NoRegistrationZSampleStepPixel), fineParam.NoRegistrationZSampleStepPixel, "NoRegistrationZSampleStepPixel must be positive.");
        if (fineParam.NoRegistrationMinZSampleCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(fineParam.NoRegistrationMinZSampleCount), fineParam.NoRegistrationMinZSampleCount, "NoRegistrationMinZSampleCount must be positive.");
    }

    private static double EstimateZCorrection(
        ImageFrame sourceFrame,
        ImageFrame targetFrame,
        PointCloudRigidTransform2D sourceTransform,
        PointCloudRigidTransform2D targetTransform,
        FineRegistrationParameters fineParam,
        double invalidDepthValue)
    {
        FrameDescriptor sourceDescriptor = sourceFrame.Descriptor;
        FrameDescriptor targetDescriptor = targetFrame.Descriptor;
        ValidatePositiveFinite(sourceDescriptor.IntervalX, nameof(sourceDescriptor.IntervalX), sourceDescriptor.GrayImagePath);
        ValidatePositiveFinite(sourceDescriptor.IntervalY, nameof(sourceDescriptor.IntervalY), sourceDescriptor.GrayImagePath);
        ValidatePositiveFinite(targetDescriptor.IntervalX, nameof(targetDescriptor.IntervalX), targetDescriptor.GrayImagePath);
        ValidatePositiveFinite(targetDescriptor.IntervalY, nameof(targetDescriptor.IntervalY), targetDescriptor.GrayImagePath);
        ValidatePositiveFinite(sourceDescriptor.IntervalZ, nameof(sourceDescriptor.IntervalZ), sourceDescriptor.GrayImagePath);
        ValidatePositiveFinite(targetDescriptor.IntervalZ, nameof(targetDescriptor.IntervalZ), targetDescriptor.GrayImagePath);

        if (Math.Abs(sourceDescriptor.IntervalX - targetDescriptor.IntervalX) > 1e-6 ||
            Math.Abs(sourceDescriptor.IntervalY - targetDescriptor.IntervalY) > 1e-6)
        {
            throw new InvalidOperationException("NoRegistration Z correction requires adjacent frames to have matching X/Y pixel intervals.");
        }

        int offsetCol = (int)Math.Round((sourceTransform.X - targetTransform.X) / sourceDescriptor.IntervalX);
        int offsetRow = (int)Math.Round((sourceTransform.Y - targetTransform.Y) / sourceDescriptor.IntervalY);

        int targetColStart = Math.Max(0, offsetCol);
        int targetRowStart = Math.Max(0, offsetRow);
        int targetColEnd = Math.Min(targetFrame.Width - 1, offsetCol + sourceFrame.Width - 1);
        int targetRowEnd = Math.Min(targetFrame.Height - 1, offsetRow + sourceFrame.Height - 1);
        int overlapWidth = targetColEnd - targetColStart + 1;
        int overlapHeight = targetRowEnd - targetRowStart + 1;
        if (overlapWidth <= 0 || overlapHeight <= 0)
            throw new InvalidOperationException("NoRegistration Z correction failed: known Offset produces no adjacent-frame overlap.");

        int sourceColStart = targetColStart - offsetCol;
        int sourceRowStart = targetRowStart - offsetRow;
        var diffs = new List<double>();

        for (int row = 0; row < overlapHeight; row += fineParam.NoRegistrationZSampleStepPixel)
        {
            int targetRow = targetRowStart + row;
            int sourceRow = sourceRowStart + row;
            for (int col = 0; col < overlapWidth; col += fineParam.NoRegistrationZSampleStepPixel)
            {
                int targetCol = targetColStart + col;
                int sourceCol = sourceColStart + col;
                if (!AreBothPixelsValid(targetFrame, targetRow, targetCol, sourceFrame, sourceRow, sourceCol))
                    continue;

                double targetRaw = GetGrayValue(targetFrame.HeightImage, targetRow, targetCol);
                double sourceRaw = GetGrayValue(sourceFrame.HeightImage, sourceRow, sourceCol);
                if (IsInvalidDepth(targetRaw, invalidDepthValue) || IsInvalidDepth(sourceRaw, invalidDepthValue))
                    continue;

                diffs.Add(targetRaw * targetDescriptor.IntervalZ + targetTransform.Z - sourceRaw * sourceDescriptor.IntervalZ);
            }
        }

        if (diffs.Count < fineParam.NoRegistrationMinZSampleCount)
        {
            throw new InvalidOperationException(
                $"NoRegistration Z correction failed: Samples={diffs.Count}, Required={fineParam.NoRegistrationMinZSampleCount}.");
        }

        diffs.Sort();
        int middle = diffs.Count / 2;
        return diffs.Count % 2 == 1
            ? diffs[middle]
            : (diffs[middle - 1] + diffs[middle]) * 0.5;
    }

    private static bool AreBothPixelsValid(
        ImageFrame targetFrame,
        int targetRow,
        int targetCol,
        ImageFrame sourceFrame,
        int sourceRow,
        int sourceCol)
    {
        HOperatorSet.TestRegionPoint(targetFrame.ValidMask, targetRow, targetCol, out HTuple targetInside);
        HOperatorSet.TestRegionPoint(sourceFrame.ValidMask, sourceRow, sourceCol, out HTuple sourceInside);
        try
        {
            return targetInside.I != 0 && sourceInside.I != 0;
        }
        finally
        {
            targetInside.Dispose();
            sourceInside.Dispose();
        }
    }

    private static double GetGrayValue(HObject image, int row, int col)
    {
        HOperatorSet.GetGrayval(image, row, col, out HTuple value);
        try
        {
            return value.D;
        }
        finally
        {
            value.Dispose();
        }
    }

    private static bool IsInvalidDepth(double value, double invalidValue)
    {
        return !double.IsFinite(value) || Math.Abs(value - invalidValue) < 1e-6;
    }

    private static void ValidatePositiveFinite(double value, string name, string source)
    {
        Validation.PositiveFinite(value, name, source);
    }

    private static double GetNominalX(FrameDescriptor descriptor)
    {
        return (descriptor.OffsetX + descriptor.CompensationX) * descriptor.IntervalX;
    }

    private static double GetNominalY(FrameDescriptor descriptor)
    {
        return (descriptor.OffsetY + descriptor.CompensationY) * descriptor.IntervalY;
    }
}

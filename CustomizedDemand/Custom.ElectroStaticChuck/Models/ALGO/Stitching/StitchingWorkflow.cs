using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Registration;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public sealed class StitchingWorkflow
{
    public StitchingResult Build(CorrectedFrameSet corrected, RegistrationResult registration, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(context);

        ImageFrame? stitchedFrame = null;
        try
        {
            if (ShouldMaterialize(corrected, registration, context))
            {
                int frameCount = corrected.Frames.Frames.Count;
                stitchedFrame = StitchingRasterizer.Rasterize(
                    corrected,
                    registration,
                    context.Parameters.Sensor,
                    (frameIndex, _) => context.ReportProgress(new AlgoProgressEvent(
                        ElectroStaticChuckStage.Stitching,
                        "rasterizing",
                        FrameIndex: frameIndex,
                        FrameCount: frameCount)));
            }

            var result = new StitchingResult(corrected, registration, stitchedFrame);
            stitchedFrame = null;
            return result;
        }
        finally
        {
            stitchedFrame?.Dispose();
        }
    }

    private static bool ShouldMaterialize(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        ElectroStaticChuckContext context)
    {
        if (!context.Options.EnableStitching)
            return false;

        if (context.Options.MeasurementMode == ElectroStaticChuckMeasurementMode.StreamingTiles)
            return false;

        return registration.Transforms.Count == corrected.Frames.Frames.Count;
    }
}

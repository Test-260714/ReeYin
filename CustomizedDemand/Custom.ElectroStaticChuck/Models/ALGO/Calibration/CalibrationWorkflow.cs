using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class CalibrationWorkflow
{
    private readonly PitchCalibrationService _pitchCalibration = new();

    public CalibrationState Calibrate(FrameSet frames, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(context);
        if (context.Parameters.Calibration.UseYawCorrection)
            throw CreateYawNotSupportedException();

        PitchCalibrationState pitch = context.Parameters.Calibration.UsePitchCorrection
            ? _pitchCalibration.Calibrate(frames, context.Parameters, context.ProgressReporter)
            : new PitchCalibrationState(false, 0.0, Array.Empty<float>());

        YawCalibrationState yaw = new(false, 0.0);

        return new CalibrationState(
            pitch,
            yaw);
    }

    internal static NotSupportedException CreateYawNotSupportedException()
    {
        return new NotSupportedException("Yaw calibration/correction is not implemented in ALGO yet.");
    }
}

public sealed class CorrectionWorkflow
{
    public CorrectedFrameSet Apply(FrameSet frames, CalibrationState calibration, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(context);
        if (context.Parameters.Calibration.UseYawCorrection)
            throw CalibrationWorkflow.CreateYawNotSupportedException();

        context.ReportProgress(new AlgoProgressEvent(
            ElectroStaticChuckStage.Correction,
            context.Parameters.Calibration.UsePitchCorrection
                ? "preparing on-demand pitch correction"
                : "using source frames without pitch correction"));

        return new CorrectedFrameSet(
            frames,
            calibration,
            context.Parameters.Calibration.UsePitchCorrection);
    }
}

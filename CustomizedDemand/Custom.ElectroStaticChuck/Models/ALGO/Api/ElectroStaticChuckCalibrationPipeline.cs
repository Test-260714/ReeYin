using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 独立标定 pipeline。
/// </summary>
public sealed class ElectroStaticChuckCalibrationPipeline
{
    private ElectroStaticChuckCalibrationPipeline()
    {
    }

    public static ElectroStaticChuckCalibrationPipeline CreateDefault()
    {
        return new ElectroStaticChuckCalibrationPipeline();
    }

    public FrameWorkflow Frames { get; } = new();

    public CalibrationWorkflow Calibration { get; } = new();

    public ElectroStaticChuckContext CreateContext(
        ElectroStaticChuckParameters parameters,
        ElectroStaticChuckRunOptions? options = null,
        string? outputDirectory = null,
        ElectroStaticChuckInput? input = null)
    {
        return ElectroStaticChuckContext.Create(parameters, options, outputDirectory, input);
    }

    public ElectroStaticChuckCalibrationResult Run(ElectroStaticChuckCalibrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Input == null)
        {
            throw new ElectroStaticChuckException(
                ElectroStaticChuckStage.LoadFrames,
                "Calibration request input is required.");
        }

        ElectroStaticChuckContext context = CreateContext(
            request.Parameters,
            request.Options,
            request.OutputDirectory,
            request.Input);

        FrameSet frameSet = RunStage(
            ElectroStaticChuckStage.LoadFrames,
            () => Frames.Load(request.Input, context));

        CalibrationState calibration = RunStage(
            ElectroStaticChuckStage.Calibration,
            () => Calibration.Calibrate(frameSet, context));

        return new ElectroStaticChuckCalibrationResult
        {
            FrameSet = frameSet,
            Calibration = calibration
        };
    }

    public bool TryRun(
        ElectroStaticChuckCalibrationRequest request,
        out ElectroStaticChuckCalibrationResult? result,
        out ElectroStaticChuckError? error)
    {
        try
        {
            result = Run(request);
            error = null;
            return true;
        }
        catch (ElectroStaticChuckException ex)
        {
            result = null;
            error = ElectroStaticChuckError.FromException(ex);
            return false;
        }
        catch (Exception ex)
        {
            result = null;
            error = ElectroStaticChuckError.FromException(ex);
            return false;
        }
    }

    private static T RunStage<T>(ElectroStaticChuckStage stage, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (ElectroStaticChuckException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ElectroStaticChuckException(stage, ex.Message, innerException: ex);
        }
    }
}

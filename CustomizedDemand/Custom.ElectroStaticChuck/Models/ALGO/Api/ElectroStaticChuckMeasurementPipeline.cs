using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Export;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Measurement;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using Custom.ElectroStaticChuckMeasure.ALGO.Registration;
using Custom.ElectroStaticChuckMeasure.ALGO.Rendering;
using Custom.ElectroStaticChuckMeasure.ALGO.Stitching;
using OpenCvSharp;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 生产测量 pipeline，仅消费显式传入的 CalibrationState，不执行 one-shot 标定。
/// </summary>
public sealed class ElectroStaticChuckMeasurementPipeline
{
    private ElectroStaticChuckMeasurementPipeline()
    {
    }

    public static ElectroStaticChuckMeasurementPipeline CreateDefault()
    {
        return new ElectroStaticChuckMeasurementPipeline();
    }

    public FrameWorkflow Frames { get; } = new();

    public CorrectionWorkflow Correction { get; } = new();

    public RegistrationWorkflow Registration { get; } = new();

    public StitchingWorkflow Stitching { get; } = new();

    public MeasurementWorkflow Measurement { get; } = new();

    public RenderingWorkflow Rendering { get; } = new();

    public ExportWorkflow Export { get; } = new();

    public ElectroStaticChuckContext CreateContext(
        ElectroStaticChuckParameters parameters,
        ElectroStaticChuckRunOptions? options = null,
        string? outputDirectory = null,
        ElectroStaticChuckInput? input = null)
    {
        return ElectroStaticChuckContext.Create(parameters, options, outputDirectory, input);
    }

    public ElectroStaticChuckRunResult Run(ElectroStaticChuckMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Input == null)
        {
            throw new ElectroStaticChuckException(
                ElectroStaticChuckStage.LoadFrames,
                "Measurement request input is required.");
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
            ElectroStaticChuckStage.Correction,
            () => ResolveMeasurementCalibration(request));

        CorrectedFrameSet corrected = RunStage(
            ElectroStaticChuckStage.Correction,
            () => Correction.Apply(frameSet, calibration, context));

        RegistrationResult registration = RunStage(
            ElectroStaticChuckStage.Registration,
            () => Registration.Register(corrected, context));

        StitchingResult stitching = RunStage(
            ElectroStaticChuckStage.Stitching,
            () => Stitching.Build(corrected, registration, context));

        MeasurementResult? measurement = null;
        RenderResult? temporaryRender = null;
        RenderResult? returnedRender = null;
        try
        {
            measurement = RunStage(
                ElectroStaticChuckStage.Measurement,
                () => Measurement.Measure(stitching, context));

            temporaryRender = RunStage(
                ElectroStaticChuckStage.Rendering,
                () => Rendering.Render(measurement, context));

            returnedRender = RunStage(
                ElectroStaticChuckStage.Rendering,
                () => CloneRenderResult(temporaryRender));

            ExportResult? export = request.Options.ExportResults
                ? RunStage(
                    ElectroStaticChuckStage.Export,
                    () => Export.Export(temporaryRender, measurement, stitching, context))
                : null;

            var result = new ElectroStaticChuckRunResult
            {
                FrameSet = frameSet,
                Calibration = calibration,
                CorrectedFrameSet = corrected,
                Registration = registration,
                Stitching = stitching,
                Measurement = measurement,
                Render = returnedRender,
                Export = export
            };

            measurement = null;
            returnedRender = null;
            return result;
        }
        catch
        {
            measurement?.Dispose();
            returnedRender?.Dispose();
            stitching.Dispose();
            throw;
        }
        finally
        {
            temporaryRender?.Dispose();
        }
    }

    public bool TryRun(
        ElectroStaticChuckMeasurementRequest request,
        out ElectroStaticChuckRunResult? result,
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

    private static CalibrationState ResolveMeasurementCalibration(ElectroStaticChuckMeasurementRequest request)
    {
        if (request.Parameters.Calibration.UsePitchCorrection &&
            request.Calibration?.Pitch.IsCalibrated != true)
        {
            throw new InvalidOperationException("Pitch correction requires a calibrated PitchCalibrationState.");
        }

        if (request.Parameters.Calibration.UseYawCorrection)
            throw CalibrationWorkflow.CreateYawNotSupportedException();

        return request.Calibration ?? new CalibrationState();
    }

    private static RenderResult? CloneRenderResult(RenderResult? render)
    {
        if (render?.Overlay == null)
            return render == null ? null : new RenderResult(null);

        Mat clone = render.Overlay.Clone();
        return new RenderResult(clone);
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

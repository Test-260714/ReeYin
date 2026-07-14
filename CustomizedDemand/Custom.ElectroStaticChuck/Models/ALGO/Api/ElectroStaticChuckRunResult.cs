using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Export;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Measurement;
using Custom.ElectroStaticChuckMeasure.ALGO.Registration;
using Custom.ElectroStaticChuckMeasure.ALGO.Rendering;
using Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

public sealed class ElectroStaticChuckRunResult : IDisposable
{
    private bool _disposed;

    public FrameSet? FrameSet { get; init; }

    public CalibrationState? Calibration { get; init; }

    public CorrectedFrameSet? CorrectedFrameSet { get; init; }

    public RegistrationResult? Registration { get; init; }

    public StitchingResult? Stitching { get; init; }

    public MeasurementResult? Measurement { get; init; }

    public RenderResult? Render { get; init; }

    public ExportResult? Export { get; init; }

    /// <summary>
    /// 调用方拥有返回结果中的可释放资源；使用完成后应释放运行结果。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Measurement?.Dispose();
        Render?.Dispose();
        Stitching?.Dispose();
        _disposed = true;
    }
}

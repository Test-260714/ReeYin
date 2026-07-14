using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Registration;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public sealed class StitchingResult : IDisposable
{
    private bool _disposed;

    public StitchingResult(CorrectedFrameSet corrected, RegistrationResult registration, ImageFrame? stitchedFrame = null)
    {
        Corrected = corrected;
        Registration = registration;
        StitchedFrame = stitchedFrame;
    }

    public CorrectedFrameSet Corrected { get; }

    public RegistrationResult Registration { get; }

    /// <summary>由 StitchingResult 持有，调用方不要单独释放。</summary>
    public ImageFrame? StitchedFrame { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        StitchedFrame?.Dispose();
        StitchedFrame = null;
        _disposed = true;
    }
}

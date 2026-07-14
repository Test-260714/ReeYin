using OpenCvSharp;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Rendering;

public sealed class RenderResult : IDisposable
{
    public RenderResult(Mat? overlay)
    {
        Overlay = overlay;
    }

    public Mat? Overlay { get; }

    public void Dispose()
    {
        Overlay?.Dispose();
    }
}

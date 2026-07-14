using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public sealed class ImageFrame : IDisposable
{
    private bool _disposed;

    public ImageFrame(
        FrameDescriptor descriptor,
        HObject grayImage,
        HObject heightImage,
        HObject validMask,
        int width,
        int height,
        long validPointCount)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        GrayImage = grayImage ?? throw new ArgumentNullException(nameof(grayImage));
        HeightImage = heightImage ?? throw new ArgumentNullException(nameof(heightImage));
        ValidMask = validMask ?? throw new ArgumentNullException(nameof(validMask));
        Width = width;
        Height = height;
        ValidPointCount = validPointCount;
    }

    public FrameDescriptor Descriptor { get; }

    /// <summary>Owned by this frame; callers must not dispose it separately.</summary>
    public HObject GrayImage { get; private set; }

    /// <summary>Owned by this frame; callers must not dispose it separately.</summary>
    public HObject HeightImage { get; private set; }

    /// <summary>Owned by this frame; callers must not dispose it separately.</summary>
    public HObject ValidMask { get; private set; }

    public int Width { get; }

    public int Height { get; }

    public long ValidPointCount { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        GrayImage.Dispose();
        HeightImage.Dispose();
        ValidMask.Dispose();
        _disposed = true;
    }
}

namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public sealed class FrameSet
{
    public FrameSet(IReadOnlyList<FrameDescriptor> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
            throw new ArgumentException("At least one frame descriptor is required.", nameof(frames));

        Frames = frames.ToArray();
    }

    public IReadOnlyList<FrameDescriptor> Frames { get; }
}

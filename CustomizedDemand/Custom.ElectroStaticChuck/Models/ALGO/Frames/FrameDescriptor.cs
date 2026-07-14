namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public sealed record FrameDescriptor(
    int Index,
    string FolderPath,
    string GrayImagePath,
    string HeightImagePath,
    double IntervalX,
    double IntervalY,
    double IntervalZ,
    double MinDepth,
    double MaxDepth,
    double OffsetX,
    double OffsetY,
    double CompensationX,
    double CompensationY,
    bool IsFlip,
    int Width,
    int Height,
    long RawValidPointCount);

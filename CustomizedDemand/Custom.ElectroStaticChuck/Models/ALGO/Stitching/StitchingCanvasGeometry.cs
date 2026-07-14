namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public sealed record StitchingCanvasGeometry(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY,
    int Width,
    int Height,
    double IntervalX,
    double IntervalY,
    double IntervalZ)
{
    public double OriginX => MinX;

    public double OriginY => MinY;
}

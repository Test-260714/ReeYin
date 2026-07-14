namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class MeasurementParameters
{
    public double ConvexStandardDiameter { get; init; } = 820;

    public double ConvexStandardHeight { get; init; } = 30;

    public int StreamingTileCoreWidthPixel { get; init; } = 2048;

    public int StreamingTileCoreHeightPixel { get; init; } = 2048;

    public int StreamingTileHaloPixel { get; init; }
}

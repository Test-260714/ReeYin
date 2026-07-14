namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class SensorParameters
{
    public double IntervalX { get; init; } = 2.9;

    public double IntervalY { get; init; } = 5.0;

    public double IntervalZ { get; init; } = 1.0;

    public double MinDepth { get; init; } = -5000;

    public double MaxDepth { get; init; } = 5000;

    public double InvalidValue { get; init; } = 888888;

    public bool IsFlip { get; init; }
}

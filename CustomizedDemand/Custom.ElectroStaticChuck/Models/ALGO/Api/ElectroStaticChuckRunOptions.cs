namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

public enum ElectroStaticChuckMeasurementMode
{
    FullImage = 0,
    StreamingTiles = 1
}

public sealed class ElectroStaticChuckRunOptions
{
    public bool EnableStitching { get; init; } = true;

    public ElectroStaticChuckMeasurementMode MeasurementMode { get; init; } = ElectroStaticChuckMeasurementMode.StreamingTiles;

    public bool SavePreviewImages { get; init; } = true;

    public bool SavePointCloud { get; init; } = true;

    public bool RenderOverlay { get; init; } = true;

    public bool ExportResults { get; init; } = true;

    public IAlgoProgressReporter? ProgressReporter { get; init; }
}

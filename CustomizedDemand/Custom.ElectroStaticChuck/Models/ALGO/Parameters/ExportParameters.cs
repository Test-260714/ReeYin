namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class ExportParameters
{
    public int ImageDownsampleFactor { get; init; } = 1;

    public int StitchingPointCloudStride { get; init; } = 1;
}

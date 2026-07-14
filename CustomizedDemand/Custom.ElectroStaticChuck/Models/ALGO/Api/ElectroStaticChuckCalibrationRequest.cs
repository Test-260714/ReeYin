using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 独立标定请求。
/// </summary>
public sealed class ElectroStaticChuckCalibrationRequest
{
    public ElectroStaticChuckInput Input { get; init; } = null!;

    public ElectroStaticChuckParameters Parameters { get; init; } = new();

    public ElectroStaticChuckRunOptions Options { get; init; } = new();

    public string? OutputDirectory { get; init; }
}

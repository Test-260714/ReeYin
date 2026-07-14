using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 独立标定结果。
/// </summary>
public sealed class ElectroStaticChuckCalibrationResult
{
    public FrameSet FrameSet { get; init; } = null!;

    public CalibrationState Calibration { get; init; } = new();
}

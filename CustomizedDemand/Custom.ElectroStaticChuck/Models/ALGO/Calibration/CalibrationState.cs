namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class CalibrationState
{
    public CalibrationState()
        : this(new PitchCalibrationState(false, 0.0, Array.Empty<float>()), new YawCalibrationState(false, 0.0))
    {
    }

    public CalibrationState(PitchCalibrationState pitch, YawCalibrationState yaw)
    {
        ArgumentNullException.ThrowIfNull(pitch);
        ArgumentNullException.ThrowIfNull(yaw);

        Pitch = pitch;
        Yaw = yaw;
    }

    public PitchCalibrationState Pitch { get; }
    public YawCalibrationState Yaw { get; }
}

public sealed class PitchCalibrationState
{
    public PitchCalibrationState(bool isCalibrated, double slope, IReadOnlyList<float> depthBase)
    {
        ArgumentNullException.ThrowIfNull(depthBase);

        IsCalibrated = isCalibrated;
        Slope = slope;
        DepthBase = Array.AsReadOnly(depthBase.ToArray());
    }

    public bool IsCalibrated { get; }

    public double Slope { get; }

    /// <summary>
    /// 每列标定基准高度。该值作为标定结果输出保留；当前俯仰校正保持旧算法行为，只使用 Slope。
    /// </summary>
    public IReadOnlyList<float> DepthBase { get; }
}

public sealed class YawCalibrationState
{
    public YawCalibrationState(bool isCalibrated, double yawDeg)
    {
        IsCalibrated = isCalibrated;
        YawDeg = yawDeg;
    }

    public bool IsCalibrated { get; }
    public double YawDeg { get; }
}

using Custom.ElectroStaticChuckMeasure.ALGO.Frames;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class YawCorrectionService
{
    /// <summary>
    /// 偏航校正算法尚未实现；调用方不能把它当作 no-op 校正使用。
    /// </summary>
    public ImageFrame Correct(ImageFrame frame, YawCalibrationState state)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(state);
        throw CalibrationWorkflow.CreateYawNotSupportedException();
    }
}

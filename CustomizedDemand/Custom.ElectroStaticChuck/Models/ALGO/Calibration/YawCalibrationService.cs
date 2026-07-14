namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class YawCalibrationService
{
    /// <summary>
    /// 偏航标定算法尚未实现；legacy 中该接口也是预留入口。
    /// </summary>
    public YawCalibrationState Calibrate()
    {
        throw CalibrationWorkflow.CreateYawNotSupportedException();
    }
}

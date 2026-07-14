namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    public class Truelight3DApiStub : ITruelight3DApi
    {
        public bool IsInitialized { get; private set; }

        public bool IsConnected { get; private set; }

        public uint ImageWidth { get; private set; }

        public uint ImageHeight { get; private set; }

        public Truelight3DApiResult Initialize()
        {
            IsInitialized = true;
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "当前为降级占位实现，未加载正式 AMSDK。");
        }

        public Truelight3DApiResult Connect()
        {
            IsConnected = true;
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位连接已建立，等待正式 AMSDK 依赖补齐。");
        }

        public Truelight3DApiResult Disconnect()
        {
            IsConnected = false;
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位连接已断开。");
        }

        public Truelight3DApiResult Shutdown()
        {
            IsInitialized = false;
            IsConnected = false;
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位 SDK 已关闭。");
        }

        public Truelight3DApiResult<Truelight3DFrame> ReadImage(uint timeoutMs = 500)
        {
            return Truelight3DApiResult<Truelight3DFrame>.FailureResult(
                Truelight3DStatus.STATUS_NOT_IMPLEMENTATION,
                "占位实现不支持单帧读取。");
        }

        public Truelight3DApiResult ConfigureScan(Truelight3DScanConfiguration configuration)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位实现已接收扫描参数，但未下发到设备。");
        }

        public Truelight3DApiResult StartScan()
        {
            return IsConnected
                ? Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位扫描已开始。")
                : Truelight3DApiResult.FailureResult(Truelight3DStatus.STATUS_NOT_CONNECTED, "设备未连接，无法开始占位扫描。");
        }

        public Truelight3DApiResult StopScan()
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位扫描已停止。");
        }

        public Truelight3DApiResult<Truelight3DScanResult> ReadScanResult(bool includePointCloud = false)
        {
            return Truelight3DApiResult<Truelight3DScanResult>.FailureResult(
                Truelight3DStatus.STATUS_NOT_IMPLEMENTATION,
                "占位实现不支持读取扫描结果。");
        }

        public Truelight3DApiResult SetObjectiveMagnification(Truelight3DObjectiveMagnification magnification)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"Objective magnification updated: {magnification}");
        }

        public Truelight3DApiResult SetExposureTime(uint exposureTimeUs)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"Exposure updated: {exposureTimeUs} us");
        }

        public Truelight3DApiResult SetLightRgb(byte red, byte green, byte blue)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"占位 RGB 光源已更新：R={red}, G={green}, B={blue}。");
        }

        public Truelight3DApiResult SetCircleLight(uint value)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"占位环形光已更新：{value}。");
        }

        public Truelight3DApiResult MoveZ(Truelight3DMotionDirection direction)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"Z motion direction: {direction}");
        }

        public Truelight3DApiResult MoveZRelative(float positionMm)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"占位 Z 轴相对运动：{positionMm} mm。");
        }

        public Truelight3DApiResult MoveZAbsolute(float positionMm)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"占位 Z 轴绝对运动：{positionMm} mm。");
        }

        public Truelight3DApiResult MoveZHome(bool isWaiting = false)
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, $"占位 Z 轴回零，阻塞模式：{isWaiting}。");
        }

        public Truelight3DApiResult StopZ()
        {
            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "占位 Z 轴已停止。");
        }

        public Truelight3DApiResult<float> GetZPosition()
        {
            return Truelight3DApiResult<float>.SuccessResult(0f, Truelight3DStatus.STATUS_OK, "占位 Z 轴当前位置为 0。");
        }

        public Truelight3DApiResult<float> GetZSpeed()
        {
            return Truelight3DApiResult<float>.SuccessResult(2f, Truelight3DStatus.STATUS_OK, "占位 Z 轴当前速度为 2 mm/s。");
        }

        public Truelight3DApiResult SetParameter(string key, object? value)
        {
            return Truelight3DApiResult.SuccessResult(
                Truelight3DStatus.STATUS_OK,
                $"占位参数已接收：{key}={value ?? "null"}。");
        }

        public string GetStatusSummary()
        {
            if (!IsInitialized)
            {
                return "占位实现未初始化。";
            }

            return IsConnected ? "占位实现已连接。" : "占位实现已初始化但未连接。";
        }
    }
}

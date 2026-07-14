namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    public interface ITruelight3DApi
    {
        bool IsInitialized { get; }

        bool IsConnected { get; }

        uint ImageWidth { get; }

        uint ImageHeight { get; }

        Truelight3DApiResult Initialize();

        Truelight3DApiResult Connect();

        Truelight3DApiResult Disconnect();

        Truelight3DApiResult Shutdown();

        Truelight3DApiResult<Truelight3DFrame> ReadImage(uint timeoutMs = 500);

        Truelight3DApiResult ConfigureScan(Truelight3DScanConfiguration configuration);

        Truelight3DApiResult StartScan();

        Truelight3DApiResult StopScan();

        Truelight3DApiResult<Truelight3DScanResult> ReadScanResult(bool includePointCloud = false);

        Truelight3DApiResult SetObjectiveMagnification(Truelight3DObjectiveMagnification magnification);

        Truelight3DApiResult SetExposureTime(uint exposureTimeUs);

        Truelight3DApiResult SetLightRgb(byte red, byte green, byte blue);

        Truelight3DApiResult SetCircleLight(uint value);

        Truelight3DApiResult MoveZ(Truelight3DMotionDirection direction);

        Truelight3DApiResult MoveZRelative(float positionMm);

        Truelight3DApiResult MoveZAbsolute(float positionMm);

        Truelight3DApiResult MoveZHome(bool isWaiting = false);

        Truelight3DApiResult StopZ();

        Truelight3DApiResult<float> GetZPosition();

        Truelight3DApiResult<float> GetZSpeed();

        Truelight3DApiResult SetParameter(string key, object? value);

        string GetStatusSummary();
    }
}

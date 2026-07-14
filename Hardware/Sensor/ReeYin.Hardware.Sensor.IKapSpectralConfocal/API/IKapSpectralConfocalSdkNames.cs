namespace ReeYin.Hardware.Sensor.IKapSpectralConfocal.API
{
    /// <summary>
    /// IKap 线光谱共焦 SDK 常量，集中管理 DLL、Feature 名称和示例默认值。
    /// </summary>
    internal static class IKapSpectralConfocalSdkNames
    {
        public const string ManagedDll = "IKapCDotNet2.dll";
        public const string NativeDll = "IKapC.dll";
        public const string BoardManagedDll = "IKapBoardDotNet2.dll";
        public const string BoardNativeDll = "IKapBoard.dll";

        public const string Width = "Width";
        public const string Height = "Height";
        public const string PixelFormat = "PixelFormat";
        public const string LayerSelector = "LayerSelector";
        public const string LayerNumber = "LayerNumber";
        public const string ProfileXUnit = "ProfileXUnit";
        public const string ProfileZUnit = "ProfileZUnit";
        public const string XStep = "XStep";
        public const string YSpacing = "YSpacing";
        public const string AcquisitionLineRateEnable = "AcquisitionLineRateEnable";
        public const string AcquisitionLineRate = "AcquisitionLineRate";
        public const string AcquisitionLineCount = "AcquisitionLineCount";
        public const string ExposureTime = "ExposureTime";
        public const string TriggerSelector = "TriggerSelector";
        public const string TriggerMode = "TriggerMode";

        public const string PixelFormatCoord3DAC32 = "Calibrated_Coord3D_AC32";
        public const string PixelFormatCoord3DC32 = "Calibrated_Coord3D_C32";
        public const string LayerSelectorManual = "Manual";
        public const string LayerSelectorBrightestAll = "BrightestAll";
        public const string TriggerSelectorFrameStart = "FrameStart";
        public const string TriggerSelectorLineStart = "LineStart";
        public const string TriggerModeOff = "Off";
        public const string TriggerModeOn = "On";

        public const uint DefaultDeviceIndex = 0U;
        public const uint DefaultBufferCount = 5U;
        public const double DefaultYSpacing = -1.0;
        public const double DefaultAcquisitionLineRate = 1000.0;
        public const uint DefaultAcquisitionLineCount = 300U;
        public const double DefaultExposureTime = 3000.0;
        public const long DefaultGrabTimeoutMs = 5000L;
        public const long DefaultLayerNumber = 2L;
    }
}
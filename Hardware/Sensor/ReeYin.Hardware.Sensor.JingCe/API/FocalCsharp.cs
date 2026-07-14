using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ReeYin.Hardware.Sensor.JingCe.API
{
    #region 错误码枚举
    /// <summary>
    /// SDK错误码
    /// </summary>
    public enum ErrorCode
    {
        OK = 0,                                    // 成功
        InvalidId,                                // 无效的相机ID
        Disconnect,                               // 相机没有连接
        ConnectOvertime,                          // 连接相机超时
        GetNetworkCardFailed,                     // 获取网卡信息失败
        GetDeviceIpFailed,                        // 获取设备IP失败
        CommandInitFailed,                        // 初始化命令失败
        CommandFpgaVersionFailed,                 // 获取FPGA版本号失败
        CommandServerVersionFailed,               // 获取Server版本号失败
        CommandDeviceIdFailed,                    // 获取设备ID失败
        CommandStartFailed,                       // 开始命令失败
        CommandStopFailed,                        // 停止命令失败
        CommandStartRawFailed,                    // 采集Raw图命令失败
        CommandStartProfileFailed,                // 采集Profile图命令失败
        CommandStartPeakFailed,                   // 采集Peak图命令失败
        CommandSetPwmFailed,                      // 设置脉冲宽度命令失败
        CommandGetPwmFailed,                      // 获取脉冲宽度命令失败
        CommandSetFrequencyFailed,                // 设置频率命令失败
        CommandGetFrequencyFailed,                // 获取频率命令失败
        CommandSetExposureFailed,                 // 设置曝光命令失败
        CommandGetExposureFailed,                 // 获取曝光命令失败
        CommandSetWindowRangeFailed,              // 设置开窗命令失败
        CommandGetWindowRangeFailed,              // 获取开窗命令失败
        CommandGetSensorWidthHeightFailed,        // 获取sensor宽高失败
        CommandGetSensorBinningFailed,            // 获取sensor width/height binning失败
        CommandSetMaterialTypeFailed,             // 设置样品材质失败
        CommandGetMaterialTypeFailed,             // 获取样品材质失败
        CommandSetSensitivityFailed,              // 设置Sensitivity失败
        CommandGetSensitivityFailed,              // 获取Sensitivity失败
        CommandSetMinThicknessFailed,             // 设置MinThickness失败
        CommandGetMinThicknessFailed,             // 获取MinThickness失败
        CommandSetBiasFailed,                     // 设置Bias失败
        CommandGetBiasFailed,                     // 获取Bias失败
        CommandSetLeastGrayFailed,                // 设置LeastGray失败
        CommandGetLeastGrayFailed,                // 获取LeastGray失败
        CommandSetLeastNumberFailed,              // 设置LeastNumber失败
        CommandGetLeastNumberFailed,              // 获取LeastNumber失败
        UpgradeCpsnFailed,                        // 烧录boot.bin和image.ub至SD卡失败
        UpgradeCpfnFailed,                        // 烧录boot.bin和image.ub至Flash失败
        CommandOpenTriggerFailed,                 // 打开外触发失败
        CommandCloseTriggerFailed,                // 关闭外触发失败
        CommandSetGpioFailed,                     // 设置GPIO失败
        CommandGetPointDataFormatFailed,          // 获取数据点格式失败
        CommandSetPointDataFormatFailed,          // 设置数据点格式失败
        CommandGetInnerTriggerFreqFailed,         // 获取内触发频率失败
        CommandSetInnerTriggerFreqFailed,         // 设置内触发频率失败
        CommandSetPeakSensorBinningFailed,        // 设置Binning失败
        CommandGetPeakSensorBinningFailed,        // 获取Binning失败
        CommandSetOuterTriggerTypeFailed,         // 设置外触发类型失败
        CommandGetOuterTriggerTypeFailed,         // 获取外触发类型失败
        CommandSetOuterTriggerFilterFailed,       // 设置外触发过滤失败
        CommandGetOuterTriggerFilterFailed,       // 获取外触发过滤失败
        CommandSetOuterTriggerHighlevelSwitchFailed, // 设置外触发高电平开关失败
        CommandGetOuterTriggerHighlevelSwitchFailed, // 获取外触发高电平开关失败
        ParasIniFileNotExist,                     // 参数文件不存在
        ParasIniFileIllegal,                      // 参数文件格式不对
        CommandGetXRatioFailed,                   // 获取X间距失败
        CommandGetFrameRateFailed,                // 获取实时帧率失败
        CommandSetZRangeOutOfRange,               // Z范围参数超出范围
        CommandGetValidZRangeFailed,              // 获取Z有效范围失败
        CommandGetDeviceModelTypeFailed,          // 获取设备ModelType失败
        CommandGetLicenseEmpty,                   // 获取License为空
        CommandSetLicenseFailed,                  // 设置License失败
        CommandSetProfileInvalidValueFailed,      // 设置Profile默认无效值失败
        CommandGetProfileInvalidValueFailed,      // 获取Profile默认无效值失败
        CommandStartSurfaceFailed,                // 开始采集Surface数据失败
        CommandTimeout,                           // 命令超时
        InvalidParams,                            // 无效的参数
        CommandNotSupport                         // 不支持的命令
    }

    /// <summary>
    /// 相机工作状态
    /// </summary>
    public enum CameraWorkStatus
    {
        Disconnected = 0,                          // 断开
        Connected                                  // 连接
    }

    /// <summary>
    /// 采集类型
    /// </summary>
    public enum CameraCaptureType
    {
        CaptureRaw = 0,                            // Raw图像采集
        CaptureProfile,                            // Profile轮廓采集
        CapturePeaks                               // Peak峰值采集
    }

    #endregion

    #region 原生结构体

    /// <summary>
    /// 平面点结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Prf_Struct
    {
        public short x;
        public double cali_x;
        public double z;
        public short intensity;
    }

    /// <summary>
    /// 曲面请求结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Surface_Request_Struct
    {
        public double yResolution;
        public uint yFrameCount;
    }

    /// <summary>
    /// 曲面数据结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Surface_Struct
    {
        public uint layerID;
        public double xResolution;
        public double yResolution;
        public double invalidZValue;
        public uint width;
        public uint height;
        public IntPtr zValues;
        public IntPtr intensities;
    }

    /// <summary>
    /// 编码器数据结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Encoder_Data_Struct
    {
        public short frameIndex;
        public int frameLength;
        public IntPtr data;
    }

    #endregion

    #region 托管数据结构

    /// <summary>
    /// 轮廓点数据（托管结构）
    /// </summary>
    public struct ProfilePoint
    {
        public short X;
        public double CalibratedX;
        public double Z;
        public short Intensity;

        public ProfilePoint(Prf_Struct native)
        {
            X = native.x;
            CalibratedX = native.cali_x;
            Z = native.z;
            Intensity = native.intensity;
        }
    }

    /// <summary>
    /// 曲面数据（托管类）
    /// </summary>
    /// <summary>
    /// 曲面数据（托管类）。ZValues/Intensities 采用懒拷贝：构造时不复制数据，
    /// 首次访问属性时才从原生指针拷贝。注意：必须在回调函数内访问，回调返回后原生内存即释放。
    /// </summary>
    public class SurfaceData
    {
        private IntPtr _zValuesPtr;
        private IntPtr _intensitiesPtr;
        private int _totalPoints;

        private double[] _zValues;
        private byte[] _intensities;

        public uint LayerID { get; set; }
        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public double InvalidZValue { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }

        public double[] ZValues
        {
            get
            {
                if (_zValues == null && _zValuesPtr != IntPtr.Zero && _totalPoints > 0)
                {
                    _zValues = new double[_totalPoints];
                    Marshal.Copy(_zValuesPtr, _zValues, 0, _totalPoints);
                }
                return _zValues;
            }
            set { _zValues = value; }
        }

        public byte[] Intensities
        {
            get
            {
                if (_intensities == null && _intensitiesPtr != IntPtr.Zero && _totalPoints > 0)
                {
                    _intensities = new byte[_totalPoints];
                    Marshal.Copy(_intensitiesPtr, _intensities, 0, _totalPoints);
                }
                return _intensities;
            }
            set { _intensities = value; }
        }

        public SurfaceData() { }

        public SurfaceData(Surface_Struct native)
        {
            LayerID = native.layerID;
            XResolution = native.xResolution;
            YResolution = native.yResolution;
            InvalidZValue = native.invalidZValue;
            Width = native.width;
            Height = native.height;

            _totalPoints = (int)(Width * Height);
            _zValuesPtr = native.zValues;
            _intensitiesPtr = native.intensities;
        }
    }

    /// <summary>
    /// 编码器数据（托管类）
    /// </summary>
    public class EncoderData
    {
        public short FrameIndex { get; set; }
        public byte[] Data { get; set; }

        public EncoderData() { }

        public EncoderData(Encoder_Data_Struct native)
        {
            FrameIndex = native.frameIndex;
            if (native.frameLength > 0 && native.data != IntPtr.Zero)
            {
                Data = new byte[native.frameLength];
                Marshal.Copy(native.data, Data, 0, native.frameLength);
            }
        }
    }

    #endregion

    #region 原生回调委托

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DealWithRawImage(string deviceID, ref int iWinBegin, ref int iWinEnd, IntPtr image);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DealWithProfile(string deviceID, IntPtr points, ref int iCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DealWithPeaks(string deviceID, IntPtr points, ref int iCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DealWithSurfaces(string deviceID, IntPtr surfacesData, ref int surfacesCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReportGpioIn1(string deviceID, int status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReportGpioIn2(string deviceID, int status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReportCameraStatus(string deviceID, CameraWorkStatus status);

    #endregion

    #region P/Invoke 声明

    /// <summary>
    /// Focal SDK 底层 P/Invoke 封装
    /// </summary>
    internal static class FocalSDKNative
    {
        private const string DllName = "FocalSDK2.0.dll";

        // SDK 初始化
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int InitSDK();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeInitSDK();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetParasFromIni")]
        private static extern int SetParasFromIniInternal(string deviceID, IntPtr fileName);

        public static int SetParasFromIni(string deviceID, string fileName)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(fileName + "\0");
            IntPtr ptr = Marshal.AllocHGlobal(utf8Bytes.Length);
            try
            {
                Marshal.Copy(utf8Bytes, 0, ptr, utf8Bytes.Length);
                return SetParasFromIniInternal(deviceID, ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // 连接相关
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNetworkCardList(out IntPtr list, out int iNum);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FindDevices(string hostIp, out IntPtr deviceIDs, int secondsTimeout);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FindDevicesMultiNIC(IntPtr hostIps, int hostIpCount, out IntPtr deviceIDs, int secondsTimeout);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FindAllDevices(out IntPtr deviceIDs, int secondsTimeout);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDeviceSourceNIC(string deviceID, IntPtr nicIP);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Connect(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Disconnect(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Close(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsDeviceReboot(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsDeviceOnline(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDeviceIP(string deviceID, IntPtr deviceIP);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ConnectByIP(string deviceIP, IntPtr deviceID);

        // 版本号
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetFPGAVersion(string deviceID, IntPtr fpgaVersion);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetServerVersion(string deviceID, IntPtr serverVersion);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDeviceModelType(string deviceID, IntPtr modelType);

        // 参数设置
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLEDPulseWidth(string deviceID, int iLedDuration);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetFrequency(string deviceID, int iFrequency);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetExposure(string deviceID, int iExposure);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetWindowRange(string deviceID, int iBegin, int iEnd);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetZRange(string deviceID, double zRange);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetBias(string deviceID, int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLeastGray(string deviceID, int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLeastNumber(string deviceID, int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetPointDataFormat(string deviceID, int dataFormat);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetInnerTriggerFreq(string deviceID, int iFreq);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetOuterTriggerType(string deviceID, int iTriggerType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetSensorBinning(string deviceID, int binning);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetOuterTriggerFilter(string deviceID, int iTriggerFilter);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetOuterTriggerHighlevelSwitch(string deviceID, int iTriggerHighlevelSwitch);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetRealTimeCaptureMode(string deviceID, [MarshalAs(UnmanagedType.I1)] bool isRealTime);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetExposureAndUpdatePwm(string deviceID, int iExposure);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetProfileInvalidValue(string deviceID, double invalidValue);

        // 参数获取
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLEDPulseWidth(string deviceID, out int iLedDuration);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetFrequency(string deviceID, out int iFrequency);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetExposure(string deviceID, out int iExposure);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetWindowRange(string deviceID, out int iBegin, out int iEnd);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetZRange(string deviceID, out double zRange);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetZValidRange(string deviceID, out double zRange);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSensorWidthHeight(string deviceID, out int width, out int height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSensorBinning(string deviceID, out int width, out int height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetFrameRate(string deviceID, out int iRawFps, out int iPeaksFps);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetBias(string deviceID, out int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLeastGray(string deviceID, out int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLeastNumber(string deviceID, out int iValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetXRatio(string deviceID, out double dXRatio);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetInnerTriggerFreq(string deviceID, out int iFreq);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOuterTriggerType(string deviceID, out int iTriggerType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetPeakSensorBinning(string deviceID, out int binning);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOuterTriggerFilter(string deviceID, out int iTriggerFilter);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOuterTriggerHighlevelSwitch(string deviceID, out int iTriggerHighlevelSwitch);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetProfileInvalidValue(string deviceID, out double invalidValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetPointDataFormat(string deviceID, out int dataFormat);

        // 温度
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetFpgaTemp(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetSensorTemp(string deviceID);

        // 数据采集
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Stop(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabRawImage(string deviceID, DealWithRawImage data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabProfile(string deviceID, DealWithProfile data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabPeaks(string deviceID, DealWithPeaks data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabSurfaces(string deviceID, ref Surface_Request_Struct request, DealWithSurfaces callBack);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabSingleRawImage(string deviceID, DealWithRawImage data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabSingleProfile(string deviceID, DealWithProfile data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GrabSinglePeaks(string deviceID, DealWithPeaks data);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetFrameCountAfterTriggerClose(string deviceID, out int frameCount);

        // 文件操作
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyFileToSD(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyFileToFlash(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CopyCalFileToFlash(string deviceID);

        // 触发
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenTrigger(string deviceID);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseTrigger(string deviceID);

        // 校准
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CalibrateZ(string deviceID, int idx, double zlamda, IntPtr ptrZ);

        // GPIO
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetReportGpioIn1CallBack(string deviceID, ReportGpioIn1 reportIn1);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetReportGpioIn2CallBack(string deviceID, ReportGpioIn2 reportIn2);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WriteGpio3(string deviceID, int status);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WriteGpio4(string deviceID, int status);

        // 相机状态回调
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetReportCameraStatusCallBack(ReportCameraStatus reportStatus);

        // 编码器
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetEncoderMode(string deviceID, int mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetEncoderMode(string deviceID, out int mode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetEncoderData(string deviceID, out Encoder_Data_Struct encoderData);

        // License
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLicenseContent(string deviceID, IntPtr license);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLicenseContent(string deviceID, string license, int len);
    }

    #endregion

    #region 高级封装

    /// <summary>
    /// Focal SDK 高级封装类
    /// 提供易用的 C# 接口，自动管理内存分配和释放
    /// </summary>
    public static class FocalSDKWrapper
    {
        #region 数据转换辅助方法

        /// <summary>
        /// 将 IntPtr 数组转换为字符串数组
        /// </summary>
        public static string[] PtrArrayToStringArray(IntPtr ptrArray, int count)
        {
            if (count <= 0 || ptrArray == IntPtr.Zero)
                return new string[0];

            string[] result = new string[count];
            IntPtr[] ptrs = new IntPtr[count];
            Marshal.Copy(ptrArray, ptrs, 0, count);

            for (int i = 0; i < count; i++)
            {
                result[i] = Marshal.PtrToStringAnsi(ptrs[i]);
            }

            return result;
        }

        /// <summary>
        /// 将 IntPtr 转换为 ProfilePoint 数组
        /// </summary>
        public static ProfilePoint[] ConvertToProfilePoints(IntPtr pointsPtr, int count)
        {
            if (count <= 0 || pointsPtr == IntPtr.Zero)
                return new ProfilePoint[0];

            ProfilePoint[] result = new ProfilePoint[count];
            int structSize = Marshal.SizeOf(typeof(Prf_Struct));

            for (int i = 0; i < count; i++)
            {
                IntPtr currentPtr = new IntPtr(pointsPtr.ToInt64() + i * structSize);
                Prf_Struct native = (Prf_Struct)Marshal.PtrToStructure(currentPtr, typeof(Prf_Struct));
                result[i] = new ProfilePoint(native);
            }

            return result;
        }

        /// <summary>
        /// 将 IntPtr 转换为 SurfaceData 数组
        /// </summary>
        public static SurfaceData[] ConvertToSurfaceData(IntPtr surfacesPtr, int count)
        {
            if (count <= 0 || surfacesPtr == IntPtr.Zero)
                return new SurfaceData[0];

            SurfaceData[] result = new SurfaceData[count];
            int structSize = Marshal.SizeOf(typeof(Surface_Struct));

            for (int i = 0; i < count; i++)
            {
                IntPtr currentPtr = new IntPtr(surfacesPtr.ToInt64() + i * structSize);
                Surface_Struct native = (Surface_Struct)Marshal.PtrToStructure(currentPtr, typeof(Surface_Struct));
                result[i] = new SurfaceData(native);
            }

            return result;
        }

        #endregion

        #region SDK 初始化

        /// <summary>
        /// 初始化SDK
        /// </summary>
        public static int InitSDK() => FocalSDKNative.InitSDK();

        /// <summary>
        /// 反初始化SDK
        /// </summary>
        public static void DeInitSDK() => FocalSDKNative.DeInitSDK();

        #endregion

        #region 设备发现与连接

        /// <summary>
        /// 获取所有网卡IP列表
        /// </summary>
        public static string[] GetNetworkCardList()
        {
            int ret = FocalSDKNative.GetNetworkCardList(out IntPtr listPtr, out int count);
            if (ret != 0 || count <= 0)
                return new string[0];
            return PtrArrayToStringArray(listPtr, count);
        }

        /// <summary>
        /// 在指定网卡上搜索设备
        /// </summary>
        public static string[] FindDevices(string hostIp, int timeoutSeconds = 3)
        {
            int count = FocalSDKNative.FindDevices(hostIp, out IntPtr deviceIDsPtr, timeoutSeconds);
            if (count <= 0)
                return new string[0];
            return PtrArrayToStringArray(deviceIDsPtr, count);
        }

        /// <summary>
        /// 在多个网卡上搜索设备
        /// </summary>
        public static string[] FindDevicesMultiNIC(string[] hostIps, int timeoutSeconds = 3)
        {
            if (hostIps == null || hostIps.Length == 0)
                return new string[0];

            IntPtr[] ipPtrs = new IntPtr[hostIps.Length];
            IntPtr hostIpsPtr = Marshal.AllocHGlobal(IntPtr.Size * hostIps.Length);

            try
            {
                for (int i = 0; i < hostIps.Length; i++)
                {
                    ipPtrs[i] = Marshal.StringToHGlobalAnsi(hostIps[i]);
                }
                Marshal.Copy(ipPtrs, 0, hostIpsPtr, hostIps.Length);

                int count = FocalSDKNative.FindDevicesMultiNIC(hostIpsPtr, hostIps.Length, out IntPtr deviceIDsPtr, timeoutSeconds);
                if (count <= 0)
                    return new string[0];
                return PtrArrayToStringArray(deviceIDsPtr, count);
            }
            finally
            {
                for (int i = 0; i < ipPtrs.Length; i++)
                {
                    if (ipPtrs[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(ipPtrs[i]);
                }
                Marshal.FreeHGlobal(hostIpsPtr);
            }
        }

        /// <summary>
        /// 在所有网卡上搜索设备
        /// </summary>
        public static string[] FindAllDevices(int timeoutSeconds = 3)
        {
            int count = FocalSDKNative.FindAllDevices(out IntPtr deviceIDsPtr, timeoutSeconds);
            if (count <= 0)
                return new string[0];
            return PtrArrayToStringArray(deviceIDsPtr, count);
        }

        /// <summary>
        /// 获取设备所属的网卡IP
        /// </summary>
        public static string GetDeviceSourceNIC(string deviceId)
        {
            IntPtr nicIPPtr = Marshal.AllocHGlobal(64);
            try
            {
                int ret = FocalSDKNative.GetDeviceSourceNIC(deviceId, nicIPPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(nicIPPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(nicIPPtr);
            }
        }

        /// <summary>
        /// 通过设备ID连接设备
        /// </summary>
        public static int Connect(string deviceId) => FocalSDKNative.Connect(deviceId);

        /// <summary>
        /// 通过IP地址连接设备，返回设备ID
        /// </summary>
        public static string ConnectByIP(string deviceIp)
        {
            IntPtr deviceIdPtr = Marshal.AllocHGlobal(64);
            try
            {
                int ret = FocalSDKNative.ConnectByIP(deviceIp, deviceIdPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(deviceIdPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(deviceIdPtr);
            }
        }

        /// <summary>
        /// 通过IP地址连接设备（带错误码）
        /// </summary>
        public static int ConnectByIP(string deviceIp, out string deviceId)
        {
            IntPtr deviceIdPtr = Marshal.AllocHGlobal(64);
            try
            {
                int ret = FocalSDKNative.ConnectByIP(deviceIp, deviceIdPtr);
                deviceId = ret == 0 ? Marshal.PtrToStringAnsi(deviceIdPtr) : null;
                return ret;
            }
            finally
            {
                Marshal.FreeHGlobal(deviceIdPtr);
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        public static void Disconnect(string deviceId) => FocalSDKNative.Disconnect(deviceId);

        /// <summary>
        /// 关闭设备
        /// </summary>
        public static void Close(string deviceId) => FocalSDKNative.Close(deviceId);

        /// <summary>
        /// 检查设备是否重启
        /// </summary>
        public static bool IsDeviceReboot(string deviceId) => FocalSDKNative.IsDeviceReboot(deviceId);

        /// <summary>
        /// 检查设备是否在线
        /// </summary>
        public static bool IsDeviceOnline(string deviceId) => FocalSDKNative.IsDeviceOnline(deviceId);

        /// <summary>
        /// 获取设备IP地址
        /// </summary>
        public static string GetDeviceIP(string deviceId)
        {
            IntPtr ipPtr = Marshal.AllocHGlobal(64);
            try
            {
                int ret = FocalSDKNative.GetDeviceIP(deviceId, ipPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(ipPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(ipPtr);
            }
        }

        #endregion

        #region 版本信息

        /// <summary>
        /// 获取FPGA版本
        /// </summary>
        public static string GetFPGAVersion(string deviceId)
        {
            IntPtr versionPtr = Marshal.AllocHGlobal(128);
            try
            {
                int ret = FocalSDKNative.GetFPGAVersion(deviceId, versionPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(versionPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(versionPtr);
            }
        }

        /// <summary>
        /// 获取服务器版本
        /// </summary>
        public static string GetServerVersion(string deviceId)
        {
            IntPtr versionPtr = Marshal.AllocHGlobal(128);
            try
            {
                int ret = FocalSDKNative.GetServerVersion(deviceId, versionPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(versionPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(versionPtr);
            }
        }

        /// <summary>
        /// 获取设备型号
        /// </summary>
        public static string GetDeviceModelType(string deviceId)
        {
            IntPtr modelPtr = Marshal.AllocHGlobal(128);
            try
            {
                int ret = FocalSDKNative.GetDeviceModelType(deviceId, modelPtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(modelPtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(modelPtr);
            }
        }

        #endregion

        #region 参数设置

        /// <summary>
        /// 从INI文件加载参数（支持中文路径）
        /// </summary>
        public static int SetParasFromIni(string deviceId, string iniFilePath)
            => FocalSDKNative.SetParasFromIni(deviceId, iniFilePath);

        /// <summary>
        /// 设置LED脉冲宽度。
        /// </summary>
        public static int SetLEDPulseWidth(string deviceId, int duration)
            => FocalSDKNative.SetLEDPulseWidth(deviceId, duration);

        /// <summary>
        /// 设置采集频率。
        /// </summary>
        public static int SetFrequency(string deviceId, int frequency)
            => FocalSDKNative.SetFrequency(deviceId, frequency);

        /// <summary>
        /// 设置曝光时间。
        /// </summary>
        public static int SetExposure(string deviceId, int exposure)
            => FocalSDKNative.SetExposure(deviceId, exposure);

        /// <summary>
        /// 设置曝光时间并同步更新PWM。
        /// </summary>
        public static int SetExposureAndUpdatePwm(string deviceId, int exposure)
            => FocalSDKNative.SetExposureAndUpdatePwm(deviceId, exposure);

        /// <summary>
        /// 设置传感器开窗范围。
        /// </summary>
        public static int SetWindowRange(string deviceId, int begin, int end)
            => FocalSDKNative.SetWindowRange(deviceId, begin, end);

        /// <summary>
        /// 设置Z轴测量范围。
        /// </summary>
        public static int SetZRange(string deviceId, double zRange)
            => FocalSDKNative.SetZRange(deviceId, zRange);

        /// <summary>
        /// 设置本底阈值。
        /// </summary>
        public static int SetBias(string deviceId, int value)
            => FocalSDKNative.SetBias(deviceId, value);

        /// <summary>
        /// 设置最低灰度阈值。
        /// </summary>
        public static int SetLeastGray(string deviceId, int value)
            => FocalSDKNative.SetLeastGray(deviceId, value);

        /// <summary>
        /// 设置最低数量阈值。
        /// </summary>
        public static int SetLeastNumber(string deviceId, int value)
            => FocalSDKNative.SetLeastNumber(deviceId, value);

        /// <summary>
        /// 设置数据点格式。
        /// </summary>
        public static int SetPointDataFormat(string deviceId, int format)
            => FocalSDKNative.SetPointDataFormat(deviceId, format);

        /// <summary>
        /// 设置内触发频率。
        /// </summary>
        public static int SetInnerTriggerFreq(string deviceId, int freq)
            => FocalSDKNative.SetInnerTriggerFreq(deviceId, freq);

        /// <summary>
        /// 设置外触发类型。
        /// </summary>
        public static int SetOuterTriggerType(string deviceId, int triggerType)
            => FocalSDKNative.SetOuterTriggerType(deviceId, triggerType);

        /// <summary>
        /// 设置传感器Binning模式。
        /// </summary>
        public static int SetSensorBinning(string deviceId, int binning)
            => FocalSDKNative.SetSensorBinning(deviceId, binning);

        /// <summary>
        /// 设置外触发过滤值。
        /// </summary>
        public static int SetOuterTriggerFilter(string deviceId, int filter)
            => FocalSDKNative.SetOuterTriggerFilter(deviceId, filter);

        /// <summary>
        /// 设置外触发高电平开关。
        /// </summary>
        public static int SetOuterTriggerHighlevelSwitch(string deviceId, int value)
            => FocalSDKNative.SetOuterTriggerHighlevelSwitch(deviceId, value);

        /// <summary>
        /// 设置实时采集模式。
        /// </summary>
        public static void SetRealTimeCaptureMode(string deviceId, bool isRealTime)
            => FocalSDKNative.SetRealTimeCaptureMode(deviceId, isRealTime);

        /// <summary>
        /// 设置Profile默认无效值。
        /// </summary>
        public static int SetProfileInvalidValue(string deviceId, double invalidValue)
            => FocalSDKNative.SetProfileInvalidValue(deviceId, invalidValue);

        #endregion

        #region 参数获取

        /// <summary>
        /// 获取LED脉冲宽度。
        /// </summary>
        public static int GetLEDPulseWidth(string deviceId, out int duration)
            => FocalSDKNative.GetLEDPulseWidth(deviceId, out duration);

        /// <summary>
        /// 获取采集频率。
        /// </summary>
        public static int GetFrequency(string deviceId, out int frequency)
            => FocalSDKNative.GetFrequency(deviceId, out frequency);

        /// <summary>
        /// 获取曝光时间。
        /// </summary>
        public static int GetExposure(string deviceId, out int exposure)
            => FocalSDKNative.GetExposure(deviceId, out exposure);

        /// <summary>
        /// 获取传感器开窗范围。
        /// </summary>
        public static int GetWindowRange(string deviceId, out int begin, out int end)
            => FocalSDKNative.GetWindowRange(deviceId, out begin, out end);

        /// <summary>
        /// 获取Z轴测量范围。
        /// </summary>
        public static int GetZRange(string deviceId, out double zRange)
            => FocalSDKNative.GetZRange(deviceId, out zRange);

        /// <summary>
        /// 获取Z轴有效范围。
        /// </summary>
        public static int GetZValidRange(string deviceId, out double zRange)
            => FocalSDKNative.GetZValidRange(deviceId, out zRange);

        /// <summary>
        /// 获取传感器宽高。
        /// </summary>
        public static int GetSensorWidthHeight(string deviceId, out int width, out int height)
            => FocalSDKNative.GetSensorWidthHeight(deviceId, out width, out height);

        /// <summary>
        /// 获取传感器Binning后的宽高。
        /// </summary>
        public static int GetSensorBinning(string deviceId, out int width, out int height)
            => FocalSDKNative.GetSensorBinning(deviceId, out width, out height);

        /// <summary>
        /// 获取Raw和Peaks实时帧率。
        /// </summary>
        public static int GetFrameRate(string deviceId, out int rawFps, out int peaksFps)
            => FocalSDKNative.GetFrameRate(deviceId, out rawFps, out peaksFps);

        /// <summary>
        /// 获取本底阈值。
        /// </summary>
        public static int GetBias(string deviceId, out int value)
            => FocalSDKNative.GetBias(deviceId, out value);

        /// <summary>
        /// 获取最低灰度阈值。
        /// </summary>
        public static int GetLeastGray(string deviceId, out int value)
            => FocalSDKNative.GetLeastGray(deviceId, out value);

        /// <summary>
        /// 获取最低数量阈值。
        /// </summary>
        public static int GetLeastNumber(string deviceId, out int value)
            => FocalSDKNative.GetLeastNumber(deviceId, out value);

        /// <summary>
        /// 获取X方向间距。
        /// </summary>
        public static int GetXRatio(string deviceId, out double ratio)
            => FocalSDKNative.GetXRatio(deviceId, out ratio);

        /// <summary>
        /// 获取内触发频率。
        /// </summary>
        public static int GetInnerTriggerFreq(string deviceId, out int freq)
            => FocalSDKNative.GetInnerTriggerFreq(deviceId, out freq);

        /// <summary>
        /// 获取外触发类型。
        /// </summary>
        public static int GetOuterTriggerType(string deviceId, out int triggerType)
            => FocalSDKNative.GetOuterTriggerType(deviceId, out triggerType);

        /// <summary>
        /// 获取Peak传感器Binning模式。
        /// </summary>
        public static int GetPeakSensorBinning(string deviceId, out int binning)
            => FocalSDKNative.GetPeakSensorBinning(deviceId, out binning);

        /// <summary>
        /// 获取外触发过滤值。
        /// </summary>
        public static int GetOuterTriggerFilter(string deviceId, out int filter)
            => FocalSDKNative.GetOuterTriggerFilter(deviceId, out filter);

        /// <summary>
        /// 获取外触发高电平开关。
        /// </summary>
        public static int GetOuterTriggerHighlevelSwitch(string deviceId, out int value)
            => FocalSDKNative.GetOuterTriggerHighlevelSwitch(deviceId, out value);

        /// <summary>
        /// 获取Profile默认无效值。
        /// </summary>
        public static int GetProfileInvalidValue(string deviceId, out double invalidValue)
            => FocalSDKNative.GetProfileInvalidValue(deviceId, out invalidValue);

        /// <summary>
        /// 获取数据点格式。
        /// </summary>
        public static int GetPointDataFormat(string deviceId, out int format)
            => FocalSDKNative.GetPointDataFormat(deviceId, out format);

        #endregion

        #region 温度

        /// <summary>
        /// 获取FPGA温度。
        /// </summary>
        public static float GetFpgaTemp(string deviceId) => FocalSDKNative.GetFpgaTemp(deviceId);
        public static float GetSensorTemp(string deviceId) => FocalSDKNative.GetSensorTemp(deviceId);

        #endregion

        #region 数据采集

        /// <summary>
        /// 停止当前设备采集。
        /// </summary>
        public static int Stop(string deviceId) => FocalSDKNative.Stop(deviceId);

        public static int GrabRawImage(string deviceId, DealWithRawImage callback)
            => FocalSDKNative.GrabRawImage(deviceId, callback);

        /// <summary>
        /// 开始连续采集Profile轮廓数据。
        /// </summary>
        public static int GrabProfile(string deviceId, DealWithProfile callback)
            => FocalSDKNative.GrabProfile(deviceId, callback);

        /// <summary>
        /// 开始连续采集Peak峰值数据。
        /// </summary>
        public static int GrabPeaks(string deviceId, DealWithPeaks callback)
            => FocalSDKNative.GrabPeaks(deviceId, callback);

        /// <summary>
        /// 开始采集Surface面数据。
        /// </summary>
        public static int GrabSurfaces(string deviceId, double yResolution, uint yFrameCount, DealWithSurfaces callback)
        {
            var request = new Surface_Request_Struct
            {
                yResolution = yResolution,
                yFrameCount = yFrameCount
            };
            return FocalSDKNative.GrabSurfaces(deviceId, ref request, callback);
        }

        /// <summary>
        /// 采集单张Raw图像。
        /// </summary>
        public static int GrabSingleRawImage(string deviceId, DealWithRawImage callback)
            => FocalSDKNative.GrabSingleRawImage(deviceId, callback);

        /// <summary>
        /// 采集单条Profile轮廓数据。
        /// </summary>
        public static int GrabSingleProfile(string deviceId, DealWithProfile callback)
            => FocalSDKNative.GrabSingleProfile(deviceId, callback);

        /// <summary>
        /// 采集单次Peak峰值数据。
        /// </summary>
        public static int GrabSinglePeaks(string deviceId, DealWithPeaks callback)
            => FocalSDKNative.GrabSinglePeaks(deviceId, callback);

        /// <summary>
        /// 获取关闭触发后的帧数。
        /// </summary>
        public static int GetFrameCountAfterTriggerClose(string deviceId, out int frameCount)
            => FocalSDKNative.GetFrameCountAfterTriggerClose(deviceId, out frameCount);

        #endregion

        #region 触发控制

        /// <summary>
        /// 打开外触发。
        /// </summary>
        public static int OpenTrigger(string deviceId) => FocalSDKNative.OpenTrigger(deviceId);
        public static int CloseTrigger(string deviceId) => FocalSDKNative.CloseTrigger(deviceId);

        #endregion

        #region 文件操作

        /// <summary>
        /// 将升级文件烧录到SD卡。
        /// </summary>
        public static int CopyFileToSD(string deviceId) => FocalSDKNative.CopyFileToSD(deviceId);
        public static int CopyFileToFlash(string deviceId) => FocalSDKNative.CopyFileToFlash(deviceId);
        public static int CopyCalFileToFlash(string deviceId) => FocalSDKNative.CopyCalFileToFlash(deviceId);

        #endregion

        #region GPIO

        /// <summary>
        /// 设置GPIO输入1状态上报回调。
        /// </summary>
        public static int SetReportGpioIn1CallBack(string deviceId, ReportGpioIn1 callback)
            => FocalSDKNative.SetReportGpioIn1CallBack(deviceId, callback);

        /// <summary>
        /// 设置GPIO输入2状态上报回调。
        /// </summary>
        public static int SetReportGpioIn2CallBack(string deviceId, ReportGpioIn2 callback)
            => FocalSDKNative.SetReportGpioIn2CallBack(deviceId, callback);

        /// <summary>
        /// 写入GPIO3输出状态。
        /// </summary>
        public static int WriteGpio3(string deviceId, int status)
            => FocalSDKNative.WriteGpio3(deviceId, status);

        /// <summary>
        /// 写入GPIO4输出状态。
        /// </summary>
        public static int WriteGpio4(string deviceId, int status)
            => FocalSDKNative.WriteGpio4(deviceId, status);

        #endregion

        #region 相机状态回调

        /// <summary>
        /// 设置相机状态上报回调。
        /// </summary>
        public static int SetCameraStatusCallback(ReportCameraStatus callback)
            => FocalSDKNative.SetReportCameraStatusCallBack(callback);

        #endregion

        #region 编码器

        /// <summary>
        /// 设置编码器模式。
        /// </summary>
        public static int SetEncoderMode(string deviceId, int mode)
            => FocalSDKNative.SetEncoderMode(deviceId, mode);

        /// <summary>
        /// 获取编码器模式。
        /// </summary>
        public static int GetEncoderMode(string deviceId, out int mode)
            => FocalSDKNative.GetEncoderMode(deviceId, out mode);

        /// <summary>
        /// 获取编码器数据
        /// </summary>
        public static EncoderData GetEncoderData(string deviceId)
        {
            int ret = FocalSDKNative.GetEncoderData(deviceId, out Encoder_Data_Struct native);
            return ret == 0 ? new EncoderData(native) : null;
        }

        #endregion

        #region License

        /// <summary>
        /// 获取License内容
        /// </summary>
        public static string GetLicenseContent(string deviceId)
        {
            IntPtr licensePtr = Marshal.AllocHGlobal(1024);
            try
            {
                int ret = FocalSDKNative.GetLicenseContent(deviceId, licensePtr);
                return ret == 0 ? Marshal.PtrToStringAnsi(licensePtr) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(licensePtr);
            }
        }

        /// <summary>
        /// 设置License内容
        /// </summary>
        public static int SetLicenseContent(string deviceId, string license)
            => FocalSDKNative.SetLicenseContent(deviceId, license, license?.Length ?? 0);

        #endregion

        #region Z校准

        /// <summary>
        /// Z轴校准
        /// </summary>
        public static void CalibrateZ(string deviceId, int idx, double zLambda, double[] zValues)
        {
            if (zValues == null || zValues.Length == 0)
                return;

            IntPtr zPtr = Marshal.AllocHGlobal(zValues.Length * sizeof(double));
            try
            {
                Marshal.Copy(zValues, 0, zPtr, zValues.Length);
                FocalSDKNative.CalibrateZ(deviceId, idx, zLambda, zPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(zPtr);
            }
        }

        #endregion
    }

    #endregion

    #region 设备对象封装

    /// <summary>
    /// Focal 设备对象封装，提供面向对象的设备操作方式
    /// </summary>
    public class FocalDevice : IDisposable
    {
        private string _deviceId;
        private bool _disposed = false;
        private bool _connected = false;

        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId => _deviceId;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _connected && FocalSDKWrapper.IsDeviceOnline(_deviceId);

        private FocalDevice(string deviceId)
        {
            _deviceId = deviceId;
        }

        /// <summary>
        /// 通过设备ID创建并连接设备
        /// </summary>
        public static FocalDevice Connect(string deviceId)
        {
            int ret = FocalSDKWrapper.Connect(deviceId);
            if (ret == 0)
            {
                return new FocalDevice(deviceId) { _connected = true };
            }
            return null;
        }

        /// <summary>
        /// 通过IP地址创建并连接设备
        /// </summary>
        public static FocalDevice ConnectByIP(string deviceIp)
        {
            string deviceId = FocalSDKWrapper.ConnectByIP(deviceIp);
            if (deviceId != null)
            {
                return new FocalDevice(deviceId) { _connected = true };
            }
            return null;
        }

        #region 版本信息

        /// <summary>
        /// 获取当前设备FPGA版本号。
        /// </summary>
        public string GetFPGAVersion() => FocalSDKWrapper.GetFPGAVersion(_deviceId);
        public string GetServerVersion() => FocalSDKWrapper.GetServerVersion(_deviceId);
        public string GetDeviceModelType() => FocalSDKWrapper.GetDeviceModelType(_deviceId);
        /// <summary>
        /// 获取当前设备IP地址。
        /// </summary>
        public string GetDeviceIP() => FocalSDKWrapper.GetDeviceIP(_deviceId);

        #endregion

        #region 参数设置

        /// <summary>
        /// 从ini文件读取并下发当前设备参数。
        /// </summary>
        public int SetParasFromIni(string iniFilePath) => FocalSDKWrapper.SetParasFromIni(_deviceId, iniFilePath);
        public int SetLEDPulseWidth(int duration) => FocalSDKWrapper.SetLEDPulseWidth(_deviceId, duration);
        public int SetFrequency(int frequency) => FocalSDKWrapper.SetFrequency(_deviceId, frequency);
        /// <summary>
        /// 设置当前设备曝光时间。
        /// </summary>
        public int SetExposure(int exposure) => FocalSDKWrapper.SetExposure(_deviceId, exposure);
        public int SetExposureAndUpdatePwm(int exposure) => FocalSDKWrapper.SetExposureAndUpdatePwm(_deviceId, exposure);
        public int SetWindowRange(int begin, int end) => FocalSDKWrapper.SetWindowRange(_deviceId, begin, end);
        /// <summary>
        /// 设置当前设备Z轴测量范围。
        /// </summary>
        public int SetZRange(double zRange) => FocalSDKWrapper.SetZRange(_deviceId, zRange);
        public int SetBias(int value) => FocalSDKWrapper.SetBias(_deviceId, value);
        public int SetLeastGray(int value) => FocalSDKWrapper.SetLeastGray(_deviceId, value);
        /// <summary>
        /// 设置当前设备最低数量阈值。
        /// </summary>
        public int SetLeastNumber(int value) => FocalSDKWrapper.SetLeastNumber(_deviceId, value);
        public int SetPointDataFormat(int format) => FocalSDKWrapper.SetPointDataFormat(_deviceId, format);
        public int SetInnerTriggerFreq(int freq) => FocalSDKWrapper.SetInnerTriggerFreq(_deviceId, freq);
        /// <summary>
        /// 设置当前设备外触发类型。
        /// </summary>
        public int SetOuterTriggerType(int triggerType) => FocalSDKWrapper.SetOuterTriggerType(_deviceId, triggerType);
        public int SetSensorBinning(int binning) => FocalSDKWrapper.SetSensorBinning(_deviceId, binning);
        public int SetOuterTriggerFilter(int filter) => FocalSDKWrapper.SetOuterTriggerFilter(_deviceId, filter);
        /// <summary>
        /// 设置当前设备外触发高电平开关。
        /// </summary>
        public int SetOuterTriggerHighlevelSwitch(int value) => FocalSDKWrapper.SetOuterTriggerHighlevelSwitch(_deviceId, value);
        public void SetRealTimeCaptureMode(bool isRealTime) => FocalSDKWrapper.SetRealTimeCaptureMode(_deviceId, isRealTime);
        public int SetProfileInvalidValue(double invalidValue) => FocalSDKWrapper.SetProfileInvalidValue(_deviceId, invalidValue);

        #endregion

        #region 参数获取

        /// <summary>
        /// 获取当前设备LED脉冲宽度。
        /// </summary>
        public int GetLEDPulseWidth(out int duration) => FocalSDKWrapper.GetLEDPulseWidth(_deviceId, out duration);
        public int GetFrequency(out int frequency) => FocalSDKWrapper.GetFrequency(_deviceId, out frequency);
        public int GetExposure(out int exposure) => FocalSDKWrapper.GetExposure(_deviceId, out exposure);
        /// <summary>
        /// 获取当前设备开窗范围。
        /// </summary>
        public int GetWindowRange(out int begin, out int end) => FocalSDKWrapper.GetWindowRange(_deviceId, out begin, out end);
        public int GetZRange(out double zRange) => FocalSDKWrapper.GetZRange(_deviceId, out zRange);
        public int GetZValidRange(out double zRange) => FocalSDKWrapper.GetZValidRange(_deviceId, out zRange);
        /// <summary>
        /// 获取当前设备传感器宽高。
        /// </summary>
        public int GetSensorWidthHeight(out int width, out int height) => FocalSDKWrapper.GetSensorWidthHeight(_deviceId, out width, out height);
        public int GetSensorBinning(out int width, out int height) => FocalSDKWrapper.GetSensorBinning(_deviceId, out width, out height);
        public int GetFrameRate(out int rawFps, out int peaksFps) => FocalSDKWrapper.GetFrameRate(_deviceId, out rawFps, out peaksFps);
        /// <summary>
        /// 获取当前设备本底阈值。
        /// </summary>
        public int GetBias(out int value) => FocalSDKWrapper.GetBias(_deviceId, out value);
        public int GetLeastGray(out int value) => FocalSDKWrapper.GetLeastGray(_deviceId, out value);
        public int GetLeastNumber(out int value) => FocalSDKWrapper.GetLeastNumber(_deviceId, out value);
        /// <summary>
        /// 获取当前设备X方向间距。
        /// </summary>
        public int GetXRatio(out double ratio) => FocalSDKWrapper.GetXRatio(_deviceId, out ratio);
        public int GetInnerTriggerFreq(out int freq) => FocalSDKWrapper.GetInnerTriggerFreq(_deviceId, out freq);
        public int GetOuterTriggerType(out int triggerType) => FocalSDKWrapper.GetOuterTriggerType(_deviceId, out triggerType);
        /// <summary>
        /// 获取当前设备Peak传感器Binning模式。
        /// </summary>
        public int GetPeakSensorBinning(out int binning) => FocalSDKWrapper.GetPeakSensorBinning(_deviceId, out binning);
        public int GetOuterTriggerFilter(out int filter) => FocalSDKWrapper.GetOuterTriggerFilter(_deviceId, out filter);
        public int GetOuterTriggerHighlevelSwitch(out int value) => FocalSDKWrapper.GetOuterTriggerHighlevelSwitch(_deviceId, out value);
        /// <summary>
        /// 获取当前设备Profile默认无效值。
        /// </summary>
        public int GetProfileInvalidValue(out double invalidValue) => FocalSDKWrapper.GetProfileInvalidValue(_deviceId, out invalidValue);
        public int GetPointDataFormat(out int format) => FocalSDKWrapper.GetPointDataFormat(_deviceId, out format);
        public float GetFpgaTemp() => FocalSDKWrapper.GetFpgaTemp(_deviceId);
        /// <summary>
        /// 获取当前设备传感器温度。
        /// </summary>
        public float GetSensorTemp() => FocalSDKWrapper.GetSensorTemp(_deviceId);

        #endregion

        #region 数据采集

        /// <summary>
        /// 停止当前设备采集。
        /// </summary>
        public int Stop() => FocalSDKWrapper.Stop(_deviceId);
        public int GrabRawImage(DealWithRawImage callback) => FocalSDKWrapper.GrabRawImage(_deviceId, callback);
        public int GrabProfile(DealWithProfile callback) => FocalSDKWrapper.GrabProfile(_deviceId, callback);
        /// <summary>
        /// 开始连续采集当前设备Peak峰值数据。
        /// </summary>
        public int GrabPeaks(DealWithPeaks callback) => FocalSDKWrapper.GrabPeaks(_deviceId, callback);
        public int GrabSurfaces(double yResolution, uint yFrameCount, DealWithSurfaces callback)
            => FocalSDKWrapper.GrabSurfaces(_deviceId, yResolution, yFrameCount, callback);
        /// <summary>
        /// 采集当前设备单张Raw图像。
        /// </summary>
        public int GrabSingleRawImage(DealWithRawImage callback) => FocalSDKWrapper.GrabSingleRawImage(_deviceId, callback);
        public int GrabSingleProfile(DealWithProfile callback) => FocalSDKWrapper.GrabSingleProfile(_deviceId, callback);
        public int GrabSinglePeaks(DealWithPeaks callback) => FocalSDKWrapper.GrabSinglePeaks(_deviceId, callback);
        /// <summary>
        /// 获取当前设备关闭触发后的帧数。
        /// </summary>
        public int GetFrameCountAfterTriggerClose(out int frameCount) => FocalSDKWrapper.GetFrameCountAfterTriggerClose(_deviceId, out frameCount);

        #endregion

        #region 触发控制

        /// <summary>
        /// 打开当前设备外触发。
        /// </summary>
        public int OpenTrigger() => FocalSDKWrapper.OpenTrigger(_deviceId);
        public int CloseTrigger() => FocalSDKWrapper.CloseTrigger(_deviceId);

        #endregion

        #region 文件操作

        /// <summary>
        /// 将当前设备升级文件烧录到SD卡。
        /// </summary>
        public int CopyFileToSD() => FocalSDKWrapper.CopyFileToSD(_deviceId);
        public int CopyFileToFlash() => FocalSDKWrapper.CopyFileToFlash(_deviceId);
        public int CopyCalFileToFlash() => FocalSDKWrapper.CopyCalFileToFlash(_deviceId);

        #endregion

        #region GPIO

        /// <summary>
        /// 设置当前设备GPIO输入1状态上报回调。
        /// </summary>
        public int SetReportGpioIn1CallBack(ReportGpioIn1 callback) => FocalSDKWrapper.SetReportGpioIn1CallBack(_deviceId, callback);
        public int SetReportGpioIn2CallBack(ReportGpioIn2 callback) => FocalSDKWrapper.SetReportGpioIn2CallBack(_deviceId, callback);
        public int WriteGpio3(int status) => FocalSDKWrapper.WriteGpio3(_deviceId, status);
        /// <summary>
        /// 写入当前设备GPIO4输出状态。
        /// </summary>
        public int WriteGpio4(int status) => FocalSDKWrapper.WriteGpio4(_deviceId, status);

        #endregion

        #region 编码器

        /// <summary>
        /// 设置当前设备编码器模式。
        /// </summary>
        public int SetEncoderMode(int mode) => FocalSDKWrapper.SetEncoderMode(_deviceId, mode);
        public int GetEncoderMode(out int mode) => FocalSDKWrapper.GetEncoderMode(_deviceId, out mode);
        public EncoderData GetEncoderData() => FocalSDKWrapper.GetEncoderData(_deviceId);

        #endregion

        #region License

        /// <summary>
        /// 获取当前设备License内容。
        /// </summary>
        public string GetLicenseContent() => FocalSDKWrapper.GetLicenseContent(_deviceId);
        public int SetLicenseContent(string license) => FocalSDKWrapper.SetLicenseContent(_deviceId, license);

        #endregion

        #region Z校准

        /// <summary>
        /// 执行当前设备Z轴校准。
        /// </summary>
        public void CalibrateZ(int idx, double zLambda, double[] zValues)
            => FocalSDKWrapper.CalibrateZ(_deviceId, idx, zLambda, zValues);

        #endregion

        #region IDisposable

        /// <summary>
        /// 断开当前设备连接。
        /// </summary>
        public void Disconnect()
        {
            if (_connected)
            {
                FocalSDKWrapper.Disconnect(_deviceId);
                _connected = false;
            }
        }

        /// <summary>
        /// 释放当前设备对象并断开连接。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
        }

        ~FocalDevice()
        {
            Dispose(false);
        }

        #endregion
    }

    #endregion
}

using System;
using System.Runtime.InteropServices;

namespace ReeYin.Hardware.Sensor.JueXin.API
{
    /// <summary>
    /// 觉芯白光干涉传感器SDK C# API完整封装
    /// 基于 WliSdkC.h v2.13.0
    /// </summary>
    public static partial class WliSdkCAPI
    {
        private const string DLL_NAME = "WliSdkC.dll";

        #region 错误码枚举
        /// <summary>
        /// 错误代码枚举
        /// </summary>
        public enum WliErrorCode
        {
            WLI_ERR_NONE = 0,                       // 成功
            WLI_ERR_TIMEOUT = 1,                    // 超时
            WLI_ERR_NETWORK_SEND_FAILURE = 2,       // 网络发送失败
            WLI_ERR_NETWORK_RECV_FAILURE = 3,       // 网络接收失败
            WLI_ERR_READ_FAIL = 4,                  // 读取失败
            WLI_ERR_FIRMWARE_SEND_SUCCESS = 5,      // 固件发送成功
            WLI_ERR_FIRMWARE_SEND_FAILURE = 6,      // 固件发送失败
            WLI_ERR_INVALID_PARAMETER = 7,          // 无效参数
            WLI_ERR_UNKNOWN = 0xFF                  // 未知错误
        }
        #endregion

        #region 事件类型枚举
        /// <summary>
        /// 事件类型枚举
        /// </summary>
        public enum WliEventType
        {
            WLI_ERROR_EVENT_TYPE = 0,               // 错误码事件
            WLI_SPECTRUM_EVENT_TYPE = 1,            // 光谱数据事件
            WLI_FFT_EVENT_TYPE = 2,                 // 频谱数据事件
            WLI_SPECTRUM_FFT_EVENT_TYPE = 3,        // 光谱频谱同时传输数据事件
            WLI_THICKNESS_EVENT_TYPE = 4,           // 厚度数据事件
            WLI_THICK_ENCODER_EVENT_TYPE = 5,       // 编码器位移数据+厚度数据事件
            WLI_CAPTURE_DARK_SIGNAL_EVENT_TYPE = 6  // 暗信号采集事件
        }
        #endregion

        #region 触发源枚举
        /// <summary>
        /// 触发源枚举
        /// </summary>
        public enum WliTrigSource
        {
            WLI_TRIG_SOURCE_TRIGGER = 0,
            WLI_TRIG_SOURCE_ENCODER1 = 1,
            WLI_TRIG_SOURCE_ENCODER2 = 2,
            WLI_TRIG_SOURCE_ENCODER3 = 3
        }
        #endregion

        #region 触发模式枚举
        /// <summary>
        /// 触发模式枚举
        /// </summary>
        public enum WliTrigMode
        {
            WLI_TRIG_MODE_EDGE = 0,
            WLI_TRIG_MODE_LEVEL = 1,
            WLI_TRIG_MODE_COUNT = 2,
            WLI_TRIG_MODE_LOCATION = 3
        }
        #endregion

        #region 触发电平枚举
        /// <summary>
        /// 触发电平枚举
        /// </summary>
        public enum WliTrigLevel
        {
            WLI_TRIG_LEVEL_RISE_EDGE = 0,
            WLI_TRIG_LEVEL_FALL_EDGE = 1,
            WLI_TRIG_LEVEL_HIGH_LEVEL = 2,
            WLI_TRIG_LEVEL_LOW_LEVEL = 3
        }
        #endregion

        #region 编码器输入模式枚举
        /// <summary>
        /// 编码器输入模式枚举
        /// </summary>
        public enum WliEncoderInputMode
        {
            WLI_ENCODER_INPUT_MODE_SINGLE_PHASE = 0,
            WLI_ENCODER_INPUT_MODE_TWO_PHASE = 1
        }
        #endregion

        #region 触发方向枚举
        /// <summary>
        /// 触发方向枚举
        /// </summary>
        public enum WliTrigDirection
        {
            WLI_TRIG_DIRECTION_POSITIVE = 0,
            WLI_TRIG_DIRECTION_NEGATIVE = 1,
            WLI_TRIG_DIRECTION_BIDIRECTION = 2
        }
        #endregion

        #region 解码模式枚举
        /// <summary>
        /// 解码模式枚举
        /// </summary>
        public enum WliDecodeMode
        {
            WLI_DECODE_MODE_X1 = 0,
            WLI_DECODE_MODE_X2 = 1,
            WLI_DECODE_MODE_X4 = 2
        }
        #endregion

        #region 事件结构体
        /// <summary>
        /// 事件基础结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliEventC
        {
            public int type;
        }

        /// <summary>
        /// 错误事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliErrorEventC
        {
            public WliEventC baseEvent;
            public WliErrorCode code;
        }

        /// <summary>
        /// 光谱事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliSpectrumEventC
        {
            public WliEventC baseEvent;
            public uint state;
            public uint peak;
            public uint count;
            public IntPtr spectrum; // uint16_t*
        }

        /// <summary>
        /// FFT事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliFFTEventC
        {
            public WliEventC baseEvent;
            public uint state;
            public float thickness;
            public float peak;
            public float thickCoeff;
            public uint count;
            public IntPtr fft; // float*
        }

        /// <summary>
        /// 光谱FFT事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliSpectrumFFTEventC
        {
            public WliEventC baseEvent;
            public uint state;
            public uint spectrumPeak;
            public float thickness;
            public float thickCoeff;
            public float fftPeak;
            public uint spectrumCount;
            public uint fftCount;
            public IntPtr spectrum; // uint16_t*
            public IntPtr fft;      // float*
        }

        /// <summary>
        /// 厚度事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliThicknessEventC
        {
            public WliEventC baseEvent;
            public uint state;
            public uint count;
            public IntPtr thicknesses; // float*
        }

        /// <summary>
        /// 厚度/编码器事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliThickEncoderEventC
        {
            public WliEventC baseEvent;
            public uint state;
            public uint count;
            public IntPtr thicknesses; // float*
            public IntPtr encVals1;    // float*
            public IntPtr encVals2;    // float*
            public IntPtr encVals3;    // float*
        }

        /// <summary>
        /// 捕获暗信号事件结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliCaptureDarkSignalEventC
        {
            public WliEventC baseEvent;
        }
        #endregion

        #region 配置结构体
        /// <summary>
        /// 触发配置结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliTrigConfigC
        {
            public WliTrigSource trigSource;
            public WliTrigMode trigMode;
            public WliTrigLevel trigLevel;
            public int trigNumber;
            public float delayTime;
        }

        /// <summary>
        /// 编码器配置结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliEncoderConfigC
        {
            public WliEncoderInputMode inputMode;
            public WliTrigDirection trigDirection;
            public WliDecodeMode decodeMode;
            public int trigCount;
            public float lenPulseRatio;
            public int zPhaseEnable;
            public float zPhaseResetPosition;
        }

        /// <summary>
        /// 错误输出配置结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WliErrorOutputC
        {
            public int condition;
            public float upper;
            public float upperHyst;
            public float lower;
            public float lowerHyst;
        }

        /// <summary>
        /// 完整配置结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WliConfigureC
        {
            // id
            public int valid;
            public int id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string name;

            // basic
            public int sampleInterval;
            public int integrationTime;
            public int exposureMode;
            public int targetValue;

            // spectrum
            public int spectrumType;
            public int specWeakThresh;
            public int specSatThesh;

            // thickness
            public int filterType;
            public int filterWidth;
            public int errorHandleSwitch;
            public int errorHandleCount;
            public float filterCoeff;

            // fft
            public float fftWeakThresh;
            public float fftRangeLow;
            public float fftRangeHigh;
            public int fftProcOption;
            public int fftAvrWin;
            public int fftAccWin;

            // dac
            public int aoSwitch;
            public int aoVolRange;
            public int aoExVolSign;
            public float aoThickLow;
            public float aoThickHigh;

            // light source
            public int ldSwitch;
            public float ldVoltage;

            // trigger
            public int trigEnabled;
            public WliTrigConfigC trigConfig;

            // sync
            public int syncEnabled;
            public int syncMode;
            public int syncRes;

            // encoder
            public int encoder1Enabled;
            public WliEncoderConfigC encoder1Config;
            public int encoder2Enabled;
            public WliEncoderConfigC encoder2Config;
            public int encoder3Enabled;
            public WliEncoderConfigC encoder3Config;

            // error output
            public int error1Enabled;
            public WliErrorOutputC error1Output;
            public int error2Enabled;
            public WliErrorOutputC error2Output;
        }
        #endregion

        #region 回调委托
        /// <summary>
        /// 事件回调函数委托
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void WliEventCallbackC(IntPtr eventPtr);
        #endregion

        #region SDK基本操作
        /// <summary>
        /// 创建SDK实例
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WliSdk_Create();

        /// <summary>
        /// 销毁SDK实例
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void WliSdk_Destroy(IntPtr sdk);

        /// <summary>
        /// 设置事件回调函数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void WliSdk_SetEventCallback(IntPtr sdk, WliEventCallbackC callback);

        /// <summary>
        /// 设置以太网连接参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void WliSdk_SetEthernet(IntPtr sdk, string ip, int port);

        /// <summary>
        /// 设置超时时间
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetTimeout(IntPtr sdk, int ms);

        /// <summary>
        /// 打开设备
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_OpenDevice(IntPtr sdk);

        /// <summary>
        /// 关闭设备
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_CloseDevice(IntPtr sdk);
        #endregion

        #region 属性操作
        /// <summary>
        /// 设置UInt32类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_SetAttributeUInt32(IntPtr sdk, string name, uint val);

        /// <summary>
        /// 获取UInt32类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_GetAttributeUInt32(IntPtr sdk, string name, out uint val);

        /// <summary>
        /// 设置Float32类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_SetAttributeFloat32(IntPtr sdk, string name, float val);

        /// <summary>
        /// 获取Float32类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_GetAttributeFloat32(IntPtr sdk, string name, out float val);

        /// <summary>
        /// 设置字符串类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_SetString(IntPtr sdk, string name, string val);

        /// <summary>
        /// 获取字符串类型属性
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_GetString(IntPtr sdk, string name, IntPtr val, int maxLen);
        #endregion

        #region FFT操作
        /// <summary>
        /// 设置FFT范围
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetFFTRange(IntPtr sdk, float low, float high);

        /// <summary>
        /// 获取FFT范围
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetFFTRange(IntPtr sdk, out float low, out float high);
        #endregion

        #region 厚度滤波器
        /// <summary>
        /// 设置厚度滤波器
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetThicknessFilter(IntPtr sdk, int filterType, int filterWidth);

        /// <summary>
        /// 获取厚度滤波器
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetThicknessFilter(IntPtr sdk, out int filterType, out int filterWidth);
        #endregion

        #region 错误数据处理
        /// <summary>
        /// 设置错误数据处理
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetErrorData(IntPtr sdk, int on, int count);

        /// <summary>
        /// 获取错误数据处理
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetErrorData(IntPtr sdk, out int on, out int count);
        #endregion

        #region 折射率
        /// <summary>
        /// 设置折射率表
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_SetRefractiveIndexTable(IntPtr sdk, string nameBuf, int m,
            [In] float[] lambdas, [In] float[] ns, [In] float[] ks);

        /// <summary>
        /// 获取折射率表
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_GetRefractiveIndexTable(IntPtr sdk, IntPtr nameBuf, int bufLen,
            out int m, out IntPtr lambdas, out IntPtr ns, out IntPtr ks);
        #endregion

        #region 信号获取
        /// <summary>
        /// 获取暗信号
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetDarkSignals(IntPtr sdk, out int n, out IntPtr vals);

        /// <summary>
        /// 获取DC偏置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetDcBias(IntPtr sdk, out int n, out IntPtr vals);

        /// <summary>
        /// 获取光源信号
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetLightSource(IntPtr sdk, out int n, out IntPtr vals);
        #endregion

        #region 模拟输出
        /// <summary>
        /// 设置模拟输出
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetAnalogOutput(IntPtr sdk, int on, int volRange, int exVolSign,
            float low, float high);

        /// <summary>
        /// 获取模拟输出
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetAnalogOutput(IntPtr sdk, out int on, out int volRange,
            out int exVolSign, out float low, out float high);
        #endregion

        #region 配置管理
        /// <summary>
        /// 设置配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_SetConfigure(IntPtr sdk, int id, int valid, string nameBuf);

        /// <summary>
        /// 获取配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetConfigure(IntPtr sdk, int id, ref WliConfigureC config);

        /// <summary>
        /// 应用配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_ApplyConfigure(IntPtr sdk, int id);
        #endregion

        #region 触发配置
        /// <summary>
        /// 设置触发使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetTrigEnabled(IntPtr sdk, int on);

        /// <summary>
        /// 获取触发使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetTrigEnabled(IntPtr sdk, out int on);

        /// <summary>
        /// 设置触发配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetTrigConfig(IntPtr sdk, ref WliTrigConfigC config);

        /// <summary>
        /// 获取触发配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetTrigConfig(IntPtr sdk, ref WliTrigConfigC config);
        #endregion

        #region 同步配置
        /// <summary>
        /// 设置同步使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetSyncEnabled(IntPtr sdk, int on);

        /// <summary>
        /// 获取同步使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetSyncEnabled(IntPtr sdk, out int on);

        /// <summary>
        /// 设置同步配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetSyncConfig(IntPtr sdk, int slave, int res);

        /// <summary>
        /// 获取同步配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetSyncConfig(IntPtr sdk, out int slave, out int res);
        #endregion

        #region 编码器配置
        /// <summary>
        /// 设置编码器使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetEncoderEnabled(IntPtr sdk, int encoder, int on);

        /// <summary>
        /// 获取编码器使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetEncoderEnabled(IntPtr sdk, int encoder, out int on);

        /// <summary>
        /// 设置编码器配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetEncoderConfig(IntPtr sdk, int encoder, ref WliEncoderConfigC config);

        /// <summary>
        /// 获取编码器配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetEncoderConfig(IntPtr sdk, int encoder, ref WliEncoderConfigC config);

        /// <summary>
        /// 设置编码器位置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetEncoderPosition(IntPtr sdk, int encoder, float position);
        #endregion

        #region 错误输出配置
        /// <summary>
        /// 设置错误输出使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetErrorEnabled(IntPtr sdk, int id, int on);

        /// <summary>
        /// 获取错误输出使能
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetErrorEnabled(IntPtr sdk, int id, out int on);

        /// <summary>
        /// 设置错误输出配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_SetErrorOutput(IntPtr sdk, int id, ref WliErrorOutputC eo);

        /// <summary>
        /// 获取错误输出配置
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetErrorOutput(IntPtr sdk, int id, ref WliErrorOutputC eo);
        #endregion

        #region 命令执行
        /// <summary>
        /// 执行命令
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WliSdk_Execute(IntPtr sdk, string command);
        #endregion

        #region 数据传输控制
        /// <summary>
        /// 开始光谱传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartSpectrumTransfer(IntPtr sdk);

        /// <summary>
        /// 停止光谱传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopSpectrumTransfer(IntPtr sdk);

        /// <summary>
        /// 开始FFT传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartFFTTransfer(IntPtr sdk);

        /// <summary>
        /// 停止FFT传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopFFTTransfer(IntPtr sdk);

        /// <summary>
        /// 开始光谱FFT传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartSpectrumFFTTransfer(IntPtr sdk);

        /// <summary>
        /// 停止光谱FFT传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopSpectrumFFTTransfer(IntPtr sdk);

        /// <summary>
        /// 开始厚度传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartThicknessTransfer(IntPtr sdk);

        /// <summary>
        /// 停止厚度传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopThicknessTransfer(IntPtr sdk);

        /// <summary>
        /// 开始厚度编码器传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartThickEncoderTransfer(IntPtr sdk);

        /// <summary>
        /// 停止厚度编码器传输
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopThickEncoderTransfer(IntPtr sdk);
        #endregion

        #region 固件更新
        /// <summary>
        /// 开始固件更新
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StartUpdateFirmware(IntPtr sdk, [In] byte[] bits, int len);

        /// <summary>
        /// 停止固件更新
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_StopUpdateFirmware(IntPtr sdk);

        /// <summary>
        /// 获取固件更新进度
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_GetUpdateFirmwareProgress(IntPtr sdk, out int progress);

        /// <summary>
        /// 更新固件
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_UpdateFirmware(IntPtr sdk);

        /// <summary>
        /// 清除固件
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WliSdk_ClearFirmware(IntPtr sdk);
        #endregion
    }
}

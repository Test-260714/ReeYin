using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Hyperson.API
{
    public enum LCF_StatusTypeDef
    {
        LCF_Status_Succeed = 0,
        LCF_Status_NoCamera = 1,
        LCF_Status_NoGrabberCard = 2,
        LCF_Status_CamApiCallFailed = 3,
        LCF_Status_ResMulErr = 4,
        LCF_Status_ResOutOfRange = 5,
        LCF_Status_OldCamFwVer = 6,
        LCF_Status_ParamOverFlash = 7,
        LCF_Status_CtrlErrCheckSum = 8,
        LCF_Status_CtrlPathNotExist = 9,
        LCF_Status_CalFileNotExist = 10,
        LCF_Status_CalFileFormatErr = 11,
        LCF_Status_NoCalPara = 12,
        LCF_Status_NoGrayCalPara = 13,
        LCF_Status_GrayCalFileFormatErr = 14,
        LCF_Status_NoRefCalFile = 15,
        LCF_Status_RefFileFormatErr = 16,
        LCF_Status_RefFileModelNotMatch = 17,
        LCF_Status_SysProcPendingTimeout = 18,
        LCF_Status_CacheNoData = 19,
        LCF_Status_NoSampleData = 20,
        LCF_Status_ParaOutOfRange = 21,
        LCF_Status_DeviceNotConnected = 22,
        LCF_Status_InvalidPara = 23,
        LCF_Status_CmosIndexError = 24,
        LCF_Status_NotInSoftwareTriggerMode = 25,
        LCF_Status_CaptureNotStart = 26,
        LCF_Status_CaptureTimeOut = 27,
        LCF_Status_NoFactoryPara = 28,
        LCF_Status_NoXCalFile = 29,
        LCF_Status_CtrlIOErr = 30,
        LCF_Status_NoMacthFramerate = 31,
        LCF_Status_XPointsUnderThre = 32,
        LCF_Status_ParaSizeNotMatch = 33,
        LCF_Status_NoLicense = 34,
        LCF_Status_ReadOnlyPara = 35,
        LCF_Status_CommunicateTimeout = 36,
        LCF_Status_CtrlFileNotFound = 37,
        LCF_Status_CtrlOpenFileFailed = 38,
        LCF_Status_PacketLost = 39,
        LCF_Status_LockSyncTimeout = 40,
        LCF_Status_FileFormatErr = 41,
        LCF_Status_TooFewValidePoints = 42,
        LCF_Status_InvalidPoints = 43,
        LCF_Status_TooFewSamplePoints = 44,
        LCF_Status_FileNameErr = 45,
        LCF_Status_XCalFormatErr = 46,
        LCF_Status_NoParaMatch = 47,
        LCF_Status_LicenseFeatureNotFound = 48,
        LCF_Status_LicenseExpired = 49,
        LCF_Status_LoadLibFailed = 50,
        LCF_Status_LicenseLoginFailed = 51,
        LCF_Status_SDKVerNotMatch = 52,
        LCF_Status_UDPSendFailed = 53,
        LCF_Status_CmdBufferSizeNotEnough = 54,
        LCF_Status_InvalidIpPara = 55,
        LCF_Status_TransmitTimeout = 56,
        LCF_Status_AllocateMemFaild = 57,
        LCF_Status_ControlerFileNotFound = 58,
        LCF_Status_NoMatchCmd = 59,
        LCF_Status_ConnectServerFailed = 60,
        LCF_Status_DeviceAlreadyOpen = 61,
        LCF_Status_DeviceNumberExceedLimit = 62,
        LCF_Status_Undefine = 63,
        LCF_Status_NotConnected = 64,
        LCF_Status_CtrlLockSyncTimeout = 65,
        LCF_Status_IOBoardCommTimeout = 66,
        LCF_Status_FileNotFound = 67,
        LCF_Status_OpenFileFailed = 68,
        LCF_Status_ErrCheckSum = 69,
        LCF_Status_TriggerModeErrDyExp = 70,
        LCF_Status_CamFWUpdatinge = 71,
        LCF_Status_OpticalFWUpdating = 72,
        LCF_Status_CamConneting = 73,
        LCF_Status_NoCamUpdateTool = 74,
        LCF_Status_NoOpticalUpdateTool = 75,
        LCF_Status_CtrlFileFormatErr = 76,
        LCF_Status_CamNoFirmware = 77,
        LCF_Status_SensorInitTimeout = 78,
        LCF_Status_ClientNumberExceedLimit = 79,
        LCF_Status_IOBoardInitFailed = 80,
        LCF_Status_IOBoardReturnDataLenErr = 81,
        LCF_Status_IOBoardReturnDataErr = 82,
        LCF_Status_SDKCacheNotConfig = 83,
        LCF_Status_SDKCacheSizeBelowThre = 84,

        LCF_Status_Others = 255,
    }

    //返回过程数据的RID
    public enum LCF_DataRid_t
    {
        LCF_RID_RESULT = 0,                     //测量结果
        LCF_RID_API_CALL_EXCEPTION,             //API调用错误 
        LCF_RID_API_CALL_WARNING,               //API调用警告
        LCF_RID_IO_ASYNC_EVENT,                 //控制器IO中断,IN0、IN1、OUT0、OUT1对应事件触发时对应的RID
        LCF_RID_CACHE_REACH_THRE,               //控制器缓存轮廓数大于等于用户设定阈值
        LCF_RID_VERSION_NOT_MATCH,              //SDK版本和控制器固件版本不匹配
        LCF_RID_NETWORK_EXCEPTION,              //网络异常

    }

    //异步通知事件类型
    public enum LCF_EventTypeDef
    {
        LCF_EventType_DataRecv = 0,             //事件类型:接收数据
        LCF_EventType_Disconnect,				//事件类型:设备断开连接
    }

    //曝光模式
    public enum LCF_ExposureMode_t
    {
        LCF_Normal_Exposure = 0,                //正常曝光模式，CMOS曝光时间等于用户设定的曝光时间
        LCF_Dynamic_Exposure,                   //动态曝光模式，曝光时间等于外部触发间隔
    }

    //增益
    public enum LCF_CameraGain_t
    {
        //2K resolution sensor
        LCF_2K_Gain_1 = 1,
        LCF_2K_Gain_2 = 2,
        LCF_2K_Gain_4 = 4,
    }

    //触发模式
    public enum LCF_TriggerMode_t
    {
        LCF_InternalTrigger = 0,                //内部连续触发
        LCF_EncoderTrigger = 1,                 //编码器触发
        LCF_SoftwareTrigger = 2,                //软件单次触发
        LCF_ExternalTrigger = 3,                //外部触发
    }

    //外部触发模式
    public enum LCF_ExternalTriggerMode_t
    {
        LCF_ExternalFalling = 0,        //下降沿触发
        LCF_ExternalRasing,             //上升沿触发
        LCF_ExternalRasingFalling,      //双边沿触发
        LCF_ExternalLowLevel,           //低电平触发
        LCF_ExternalHighLevel,          //高电平触发
    }

    //编码器分频模式
    public enum LCF_EncoderDivisionMode_t
    {
        LCF_Fixed_Division = 0,         //固定分频
        LCF_Dynamic_Division,           //动态分频
    }

    //binning模式
    public enum LCF_BinningMode_t
    {
        LCF_BinMode_No = 0x0,
        LCF_BinMode_2X1 = 0x01,         //X轴Binning，扩大X轴点间隔，提高帧率
        LCF_BinMode_1X2 = 0x02,         //Z轴Binning，降低Z轴分辨率，提高帧率
        LCF_BinMode_2X2 = 0x03,         //X轴、Z轴Binning，扩大X轴点间隔、降低Z轴分辨率，提高帧率
        LCF_BinMode_Ext = 0x04,         //Binning扩展模式，提高帧率
    }

    //X量程
    public enum LCF_X_Range_t
    {
        LCF_X_Range_Full = 0x0,         //X轴满量程
        LCF_X_Range_3_4,                //X轴3/4量程
        LCF_X_Range_2_4,                //X轴1/2量程
        LCF_X_Range_1_4,                //X轴1/4量程
        LCF_X_Range_1_8,                //X轴1/8量程
    }

    //X量程缩小方向
    public enum LCF_X_Reduce_dir_t
    {
        LCF_X_Reduce_Tail = 0,          //裁剪掉X轴尾部
        LCF_X_Reduce_Head,              //裁剪掉X轴头部
        LCF_X_Reduce_Edge               //裁剪掉X轴两端
    }

    //X轴降采样
    public enum LCF_X_Subsample_t
    {
        LCF_X_Subsample_No = 0x0,
        LCF_X_Subsample_X2 = 0x01,      //X轴2倍降采样，X点数量变为1/2
        LCF_X_Subsample_X4 = 0x02,      //X轴4倍降采样，X点数量变为1/4
        LCF_X_Subsample_X8 = 0x03,      //X轴8倍降采样，X点数量变为1/8
        LCF_X_Subsample_X16 = 0x04,     //X轴16倍降采样，X点数量变为1/16
    }

    //Z轴降采样
    public enum LCF_Z_Subsample_t
    {
        LCF_Z_Subsample_No = 0x0,
        LCF_Z_Subsample_X2 = 0x01,      //Z轴2倍降采样
    }

    //HDR多帧合成模式,使用多帧不同光强/曝光的测量数据合成一帧
    public enum LCF_HDRMode_t
    {
        LCF_HDRMode_OFF = 1,        //关闭HDR模式
        LCF_HDRMode_2,              //使用2帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_3,              //使用3帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_4,              //使用4帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_5,              //使用5帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_6,              //使用6帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_7,              //使用7帧不同光强/曝光的测量数据合成一帧
        LCF_HDRMode_8,              //使用8帧不同光强/曝光的测量数据合成一帧
    }

    //HDR模式下选择灰度图类型
    public enum LCF_HDR_Intensity_t
    {
        LCF_HDR_I_Avg = 0,          //多帧HDR模式下，使用多帧平均灰度作为最终灰度图
        LCF_HDR_I_Max,              //多帧HDR模式下，使用信号最强的一帧灰度图作为最终灰度图
        LCF_HDR_I_Min               //多帧HDR模式下，使用信号最弱的一帧灰度图作为最终灰度图
    };

    //近端、远端、多距离模式下信号检测灵敏度
    public enum LCF_SignalDetectSensitivity_t
    {
        LCF_LowSensitivity = 0,     //低灵敏度
        LCF_NormalSensitivity,      //正常灵敏度
        LCF_HighSensitivity,        //高灵敏度
        LCF_CustomizeSensitivity,   //自定义灵敏度
    }

    //测量模式
    public enum LCF_MeasureModet_t
    {
        LCF_Mode_SinglePeak = 0,        //单层模式,测量强度最强的信号
        LCF_Mode_MultiPeak,             //多层模式，测量所有检测导的信号
        LCF_Mode_NearEnd,               //近端模式，测量靠近传感头的信号
        LCF_Mode_FarEnd,                //远端模式，测量远离传感头的信号
        LCF_Mode_Thickness,             //厚度模式，测量厚度
    }

    //ROI自适应参数
    public enum LCF_AdjustRoiFpsPara_t
    {
        LCF_SetRoiForFps_NoBinning,     //适应ROI到用户指定帧率，点间距优先，压缩Z向量程
        LCF_SetRoiForFps_X_Binning,     //适应ROI到用户指定帧率，Z向量程优先，扩大X向点间距，X向有效点数变为最大点数的一半,X向量程不变
        LCF_SetRoiForFps_Z_Binning,     //适应ROI到用户指定帧率，Z向量程优先，减低Z向分辨率
        LCF_SetRoiForFps_X_Z_Binning,   //适应ROI到用户指定帧率，速度和Z向量程优先，扩大X向点间距、减低Z向分辨率
    }

    //编码器输入模式
    public enum LCF_EncoderInputMode_t
    {
        LCF_ENCODER_MUT_2_INC_1 = 1,    //2相1倍频
        LCF_ENCODER_MUT_2_INC_2 = 2,    //2相2倍频
        LCF_ENCODER_MUT_2_INC_4 = 3,    //2相4倍频
    }

    //控制器IO端口,这些端口不是实时端口只用于绑定特定的触发功能，不用于实时触发传感器采集
    public enum LCF_IO_Port_t
    {
        LCF_IO_IN0 = 0,     //IN0
        LCF_IO_IN1,         //IN1
        LCF_IO_OUT0,        //OUT0
        LCF_IO_OUT1         //OUT1
    }

    //控制器IO事件触发类型
    public enum LCF_IO_TriggerMode_t
    {
        LCF_IO_TriggerRasing = 0,           //上升沿触发
        LCF_IO_TriggerFalling,              //下降沿触发
        LCF_IO_TriggerRasingFalling,        //双边沿触发
    }

    //控制器IO引脚功能
    public enum LCF_IO_Func_t
    {
        LCF_IO_Func_Idle = 0,           //IO端口空闲,不绑定任何功能
        LCF_IO_Func_StartSample,        //IO引脚触发传感器启动采集数据
        LCF_IO_Func_StopSample,         //IO引脚触发传感器停止采集数据
        LCF_IO_Func_ClearCache,         //IO引脚触发传感器情况缓存数据
        LCF_IO_Func_AsyncNotice,        //IO引脚中断通知，将该中断事件通过回调函数通知用户，SDK内部不做任何处理
    }

    //数据Cache选择，只对LCF_ExportCacheData方式导出数据有效
    public enum LCF_DataCacheType_t
    {
        LCF_Cache_Controler = 0,        //扫描数据缓存在控制器中
        LCF_Cache_SDK = 1,              //扫描数据缓存在SDK中，需要先通过参数PARAM_SDK_CACHE_SIZE
    }

    //控制器状态
    public enum LCF_ControlerStatus_t
    {
        LCF_CS_CameraReady = 0,         //传感器已经就绪，SDK可以连接
        LCF_CS_NoCamera = 1,            //检测不到传感头
        LCF_CS_OldFirmware = 2,         //控制器固件为旧版本固件
        LCF_CS_ClientsReachMax = 3,     //控制器连接的TCP客户端个数已经达到上限
        LCF_CS_NetSegmentNotMatch = 4,  //控制器和PC不在同一网段
        LCF_CS_NotGigabitNetwork = 5,   //PC网卡速率达不到千兆网速率
        LCF_CS_JumboFrameError = 6,     //PC网卡没有打开巨型帧
        LCF_CS_IPConflict = 7,          //控制IP和PC端IP冲突，需要修改控制器IP
        LCF_CS_FirewallEnalble = 8,     //防火墙未关闭，扫描信息可能被拦截
    }



    //控制器以太网参数
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_DeviceEthPara_t
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ip_addr;
        UInt32 port;
    }

    // 控制器详细信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_DeviceControlInfo_t
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string controlerIP;                                  //控制器IP
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string controlerGateway;                             //控制器网关
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string controlerNetmask;                             //控制子网掩码
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string controlerMAC;                                 //控制器MAC地址
        public int controlerPort;                                   //控制器端口号
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string localIP;                                      //连接控制器的本地网卡IP
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string localMAC;                                     //连接控制器的本地网卡MAC地址
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string deviceUserID;                                 //控制器用户ID名
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string controlerSN;                                  //控制器序列号
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string controlerFWVersion;                           //控制器固件版本
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string controlerModelName;                           //控制器型号
        public int status;                                          //当前控制器的状态,值等于 LCF_ControlerStatus_t
        public int timeStamp_H;                                     //控制器时间戳高32位
        public int timeStamp_L;                                     //控制器时间戳低32位
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string firmwareType;                                 //固件类型
        public int controlerRunTime_Min;                            //控制器运行时间，单位分钟
        public float freeDiskSpace_GB;                              //控制器剩余磁盘空间
        public float freeMem_GB;                                    //控制器剩余内存
        public int grabberCardTemp;                                 //控制器光模块温度
        public int cpuTemp;                                         //控制器CPU温度
        public int gpuTemp;                                         //控制器GPU温度

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 251)]
        public int[] reserve;
    }

    //传感头出厂参数
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_SensorHeadFactoryPara_t
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string model;                                            // 该传感头型号
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string serialNumber;                                     // 该传感头序列号
        public int measureRangeMin;                                     //量程近端距离，等于真实值*10，单位mm
        public int measureRangeMax;                                     //量程远端距离，等于真实值*10，单位mm
        public int scanLength;                                          //X轴扫描长度，等于真实值*10，单位mm
        public uint calibrationTimestamp;                               //校正时间戳
        public int calibrationTemp;                                     //记录校正时温度的温度

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 510)]
        public int[] reserve;
    }

    //用户配置参数
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct LCF_DeviceSetting_t
    {
        public int exposureTime;                //曝光时间，us
        public float lightIntensity;            //照明强度(0.0~100.0)
        public float scanLength;                //X轴扫描长度，单位mm
        public int max_resolution_X;            //X轴最大分辨率
        public int max_resolution_Y;            //Y轴(对应测量深度图的Z轴)最大分辨率
        public int pixel_depth;                 //像素深度
        public int resolution_x_raw;            //ROI0_dim_X;   //当前X轴分辨率,该值为没有降采样的分辨率
        public int resolution_y_raw;            //ROI0_dim_Y;   //当前Y轴分辨率,该值为没有降采样的分辨率
        public int pos_x_raw;                   //ROI0_pos_X;   //X轴偏移
        public int pos_y_raw;                   //ROI0_pos_Y;   //Y轴偏移
        public int countourLineThreshold;       //回调阈值，控制器内部轮廓大于该值才通知用户
        public int frameRateControl;            //帧率控制
        public int frameRate;                   //帧率
        public int medianFilterDepth;           //时间域滑动中值滤波深度
        public int movingAvgFilterDepth;        //时间域滑动平均深度
        public int spaceMedianFilterDepth;      //空间域域滑动中值滤波深度
        public int spaceMovingAvgFilterDepth;   //空间域滑动平均深度
        public int encoderDivision;             //编码器分频系数
        public int invalidSignalThrehold;       //无效信号阈值(0~255)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_HDR_NUMBER * LCFDevice.MAX_HDR_NUMBER)]
        public float[] HDR_LightIntensity;      //多光强HDR模式下，每个HDR模式下对应的光强（0.0~100.0）
        public float calculateThrehold;         //信号计算阈值(0~1)
        public int cameraGain;                  //传感器增益
        public int HDR_Num;                     //HDR 次数
        public int triggerMode;                 //触发模式
        public int binningMode;                 //binning模式
        public int measureMode;                 //测量模式
        public int removeInvalidSignal;         //HDR去除饱和\无信号数据
        public int reverseHightValue;           //输出高度图翻转
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string controlerFWVersion;       //控制器固件版本号
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string sensorFWVersion;          //传感头固件版本号
        public float x_point_interval;          //X轴点间隔，单位um
        public int encoderInputMode;            //编码器输入模式
        public int maxInterpolationPoint;       //空洞插值最大的空洞长度
        public int signalNumberLimit;           //信号数量限制
        public int calPointsThreshold;          //最小计算点数阈值
        public int intensity_fft2_w;            //灰度数据fft2宽度
        public int intensity_fft2_h;            //灰度数据fft2高度
        public int height_fft2_w;               //高度数据fft2宽度
        public int height_fft2_h;               //高度数据fft2高度
        public int medianFilterDepth_2D;        //2维平面中值滤波
        public int avgFilterDepth_2D;           //2维平面平均滤波
        public int morphClose_2D;               //2维平面形态学闭运算
        public int morphOpen_2D;                //2维平面形态学开运算
        public int surfaceSorting;              //多层距离排序方式
        public int refractiveCal;               //多距离、远端模式否使能折射率校正
        public int externalTriggerMode;         //外部触发模式
        public int x_subsample;                 //X轴降采样比例
        public int intensity_sharpen;           //2D灰度数据锐化
        public int cmos_hdr_para_magic;         //CMOS HDR参数magic
        public int cmos_hdr_mode;               //CMOS HDR模式
        public int triggerSyncOutEn;            //外部IO同步输出使能
        public int heartBeatTimeout;            //心跳包超时时间，单位秒
        public int signalSmooth;                //信号平滑长度
        public int measureRange_near;           //近端量程，单位um
        public int measureRange_far;            //远端量程，单位um
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] controlerIOTriggerMode;    //IN0、IN1、OUT0、OUT1 IO口电平触发方式
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] controlerIOFunc;           //IN0、IN1、OUT0、OUT1 IO口绑定的功能
        public int intensityResample;           //使能灰度图重采样
        public int HDR_SaturationRemoveLen;     //HDR模式下信号饱和区域的滤除范围
        public int IO_DebounceTime;             //IO输入消抖延迟时间，单位ms
        public int firstLayerMinDist;           //多层距离下，第一层和第二层最小距离（um）
        public int firstLayerMinLen;            //多层距离下，第一层最小点数
        public int intensityLayerSelect;        //选择对应层的灰度图
        public int cacheCountourThreshold;      //Cache缓存轮廓阈值，达到阈值后异步通知用户
        public int z_subsample;                 //Z轴降采样比例
        public int enFilterOffset;              //使能偏移滤波
        public int interpolationMaxThickness;   //插值的最大Z方向间隙(um)
        public int clusterFilterMinLen;         //集群滤波最小长度
        public int clusterFilterDistanceZ;      //集群滤波最小Z方向间隙(um)
        public int signalDetectSensitivity;     //信号检测灵敏度
        public int lightOverpower;              //光源超功率模式
        public float avgFilterSigma_s_2D;       //2D平滑滤波中心距离标准差
        public float avgFilterSigma_diff_2D;    //2D平滑滤波中心点差值标准差
        public int morphClose_sigma_2D;        //2维平面形态学闭运算Z方向标准差
        public int x_range;                    //X轴量程
        public int exposureMode;                //CMOS曝光模式
        public int spaceMovingAvgFilter_Z_Thre; //一维空间平滑滤波Z方向阈值
        public int encoderDivisionMode;         //编码器分频模式
        public int dyncDivGroupNumber;          //动态分频组数
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DYNAMIC_DIV_GROUP)]
        public int[] dyncDivEncoderPos;         //动态分频对应的电机脉冲位置
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DYNAMIC_DIV_GROUP)]
        public int[] dyncDivision;              //动态分频下对应每个区域的分频值
        public int trim_edges_filter;           //滤除伪边沿
        public int morphOpen_Z_Thre_2D;         //开运行Z阈值，和周围点差值大于该值认为是离群点
        public int hdr_intensity_select;        //HDR模式下灰度图选择
        public int enFilterOffset_2D;           //2D偏移滤波
        public int x_reduce_dir;                //X轴量程缩小方向
        public int HDR_MultiExpMode;            //HDR多曝光模式，支持每帧数据不同的曝光时间
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_HDR_NUMBER * LCFDevice.MAX_HDR_NUMBER)]
        public int[] HDR_Exposure;              //多曝光HDR模式下，每个HDR模式下对应的曝光时间
        public int intensityCal;                //灰度补偿
        public int resolution_x;                //X方向实际分辨率
        public int warnUpLight;                 //暖机亮度值
        public int energySavingMode;            //光源使用寿命延长
        public int customizeSensitivity;        //自定义信号检测灵敏度
        public int graySharpnessLevel;                 //灰度清晰化
        public int grayBinEn;                          //灰度二值化使能
        public int grayBinReverse;                     //灰度二值化反向
        public int grayBinAutoThre;                    //灰度二值化自动查找阈值
        public int grayBinThre;                        //灰度二值化阈值
        public int dataCacheSelet;                     //数据缓存选择
        public int sdkCacheSize;                       //SDK Cache缓存大小，单位：轮廓数
        public int expWaitFifoEmpty;                   //使用SDK Cache导出数据时，等待

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 499)]
        public int[] reserve;                          //保留区域
    }

    //异步事件参数
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_EventCallbackArgs_t
    {
        public LCF_EventTypeDef eventType;      //事件类型
        public LCF_DataRid_t rid;               //数据rid
        public IntPtr data;                     //数据
        public int dataLen;                     //数据个数
    };

    //轮廓特征数据
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_StatisticInfo_t
    {
        /****************多层高度统计信息，若是单层模式，则只有第一个数组成员有效*****************/
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] maxHeightValue;            //高度最大值，数值 /10.0 = 实际的高度，单位um

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] minHeightValue;            //高度最小值，数值 /10.0 = 实际的高度，单位um

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] maxHeightValue_X;          //高度最大值X坐标

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] maxHeightValue_Y;          //高度最大值Y坐标

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] minHeightValue_X;          //高度最小值X坐标

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DETECT_LAYER_NUMBER)]
        public int[] minHeightValue_Y;          //高度最小值Y坐标

        /***************灰度统计信息*****************/
        public int maxIntensityValue;           //灰度最大值
        public int minIntensityValue;           //灰度最小值
        public int maxIntensityValue_X;         //灰度最大值X坐标
        public int maxIntensityValue_Y;         //灰度最大值Y坐标
        public int minIntensityValue_X;         //灰度最小值X坐标
        public int minIntensityValue_Y;         //灰度最小值Y坐标
    }

    //记录不同分频下的y位置
    //例:第0~ y_pos[0]轮廓为encoderDiv[0]分频;第y_pos[0]~ y_pos[1]轮廓为encoderDiv[1]分频;第y_pos[1]~ y_pos[2]轮廓为encoderDiv[2]分频
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_DynamicDivisionInof_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DYNAMIC_DIV_GROUP)]
        public int[] y_pos;             //分频 x 电机一个脉冲对应的距离值等于Y方向扫描间隔

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LCFDevice.MAX_DYNAMIC_DIV_GROUP)]
        public int[] encoderDiv;        //分频数值
    };

    //轮廓数据
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_Result_t
    {
        public UInt32 x_count;          //轮廓长度
        public UInt32 y_count;          //轮廓个数
        public UInt32 layer_number;     //层数，若当前为单层模式，值该值为1
        public float x_interval;        //轮廓X轴点间隔，单位um
        /*
        * 存放多个轮廓数据, 点数等于 layer_number* x_count * y_count, 数值 /10.0 = 实际的高度,单位um；例 height[0] = 2000-> 对应高度为 2000/10.0 = 200um
        * 第一层(靠近传感器)数据地址为:height。第二层数据地址为:height + x_count * y_count,其他层以此类推。
        */
        public IntPtr height;
        public IntPtr intensity;                    //存放轮廓的灰度图，数据类型unsigned shot，数据个数等于 x_count * y_count；如果没有使能灰度图输出,则里面的数据无效
        public IntPtr triggerCount;                 //存放每个轮廓的触发计数，个数等于y_count
        public LCF_StatisticInfo_t statisticInfo;   //返回统计信息
        public LCF_DynamicDivisionInof_t dynamicDiv;//返回动态编码器分频模式下，编码器分频信息，只对LCF_ExportCacheData导出的测量结果有效，回调函数返回的测量结果该字段无效
    };

    //控制统计信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LCF_DeviceStatisticInfo_t
    {
        public int cpu_temp;                //CPU温度
        public int gpu_temp;                //GPU温度
        public int camera_temp;             //相机温度
        public int grabber_card_temp;       //采集卡温度
        public int calculateFrameRate;      //计算帧率
        public int cameraFrameRate;         //相机帧率
        public int transmitFrameRate;       //传输帧率
        public int occupancyRate;           //资源占用率
        public int lightTemp;               //光源温度
        public int cmosLostFrame;           //相机丢帧信息
        public int cmosErrorFrame;          //相机错误帧信息
        public int cameraMaxFrameRateHistory;      //相机CMOS历史最大帧率
        public float freeDiskSpace_GB;             //控制器剩余磁盘空间
        public float freeMem_GB;                   //控制器剩余内存

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 121)]
        public int[] reserve;               //保留区域
    };



    // 事件委托类型
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LCF_UserEventCallbackHandleDelegate(int handle, ref LCF_EventCallbackArgs_t arg, IntPtr userPara);


    public interface ILCFDevice { }
    public partial class LCFDevice : ILCFDevice
    {
        public const int MINI_ROI_X_RES = 64;             //X轴最小分辨率
        public const int MINI_ROI_Y_RES = 56;             //Y轴最小分辨率
        public const int MAX_ROI_Y_RES = 1080;
        public const float INVALID_VALUE = 888888;        //信号无效值
        public const int MAX_HDR_NUMBER = 8;              //最大HDR次数
        public const int MAX_DETECT_LAYER_NUMBER = 4;     //最大检测层数
        public const int MAX_DYNAMIC_DIV_GROUP = 3;       //动态分频最大组数

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ScanDeviceList(ref IntPtr deviceList, ref UInt32 deviceNumber);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ScanDeviceList_Detail(ref IntPtr deviceList, ref UInt32 deviceNumber);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ConnectDevice(IntPtr cotrollerIP, UInt16 controllerPort, ref int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static void LCFAPI_CloseDevice(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_IsConnect(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_IsStart(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_StartCapture(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_StopCapture(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_RegisterEventCallback(LCF_UserEventCallbackHandleDelegate eventHandle, IntPtr userPara);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_UnregisterEventCallback();

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SingleShot(int handle, ref LCF_Result_t result);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static IntPtr LCFAPI_GetErrorInfo(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_Reboot(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_Shutdown(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetIntParameter(int handle, IntPtr paramName, int value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetFloatParameter(int handle, IntPtr paramName, float value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetDoubleParameter(int handle, IntPtr paramName, double value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetStringParameter(int handle, IntPtr paramName, string value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetIntParameter(int handle, IntPtr paramName, ref int value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetFloatParameter(int handle, IntPtr paramName, ref float value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDoubleParameter(int handle, IntPtr paramName, ref double value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetStringParameter(int handle, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDeviceSetting(int handle, ref LCF_DeviceSetting_t setting);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SaveSetting(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_RestoreFactorySetting(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ExportCacheData(int handle, ref LCF_Result_t result);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ClearCacheData(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetHDRIntensityGroup(int handle, int HDR_Mode, float[] intenSity);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetHDRExposureGroup(int handle, int HDR_Mode, int[] exposure);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDeviceStatisticInfo(int handle, ref LCF_DeviceStatisticInfo_t info);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ReadSensorHeadFactoryPara(int handle, ref LCF_SensorHeadFactoryPara_t para);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ClearEncoderTriggerCount(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ExportDeviceSetting(int handle, IntPtr path);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ImportDeviceSetting(int handle, IntPtr fileName);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_AdjustRoiForFps(int handle, int mode, int frameRate, ref float Z_Rangeo);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetMeasureRange(int handle, int farRange_um, int nearRange_um);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetIOPortFunc(int handle, int io_port, int triggerMode, int io_func);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_FindGoodLight(int hahandlendle, int enHDR);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetEncoderDyncDivValue(int hahandlendle, int[] encoderPos, int[] division, int[] real_div, int group_member);



        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetControllerParam(IntPtr controlerSN, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetControllerParam(IntPtr controlerSN, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ForceIP(IntPtr controlerSN, IntPtr ip, IntPtr gateway, IntPtr netmask);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetEthParam(IntPtr controlerSN, IntPtr ip, IntPtr gateway, IntPtr netmask);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_WriteUserFlashData(int handle, IntPtr data, int len);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ReadUserFlashData(int handle, IntPtr data, ref int len);



        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetMaxROIFrameRate(int handle, UInt32 x_res, UInt32 y_res, ref UInt32 maxFrameRate);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetSignalOutput(int handle, int en, int signalIndex);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetLastSignal(int handle, IntPtr signal, ref UInt32 signalLen);



        public static double[] getHeightValue(IntPtr ptr, Int32 count, double precision = 10.0)
        {


            double[] ret = new double[count];
            int[] temp = new int[count];

            Marshal.Copy(ptr, temp, 0, count);

            for (int i = 0; i < count; i++)
                ret[i] = (float)(temp[i] / precision);

            temp = null;
            return ret;
        }

        public static short[] getIntensityValue(IntPtr ptr, Int32 count)
        {
            short[] ret = new short[count];
            Marshal.Copy(ptr, ret, 0, count);
            return ret;
        }

        public static LCF_Result_t getLCFResult(IntPtr ptr)
        {

            return (LCF_Result_t)Marshal.PtrToStructure(ptr, typeof(LCF_Result_t));

        }



        /**
        * @brief LCF_ScanDeviceList 扫描传感器，返回传感器IP和端口号; 注:使用该接口扫描设备需要关闭PC端防火墙，反则可能会扫描不到设备
        * @param deviceList    返回扫描到的设备列表，存放IP和端口信息
        * @param deviceNumber  扫描到的设备个数
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_ScanDeviceList(out LCF_DeviceEthPara_t[] deviceList, out UInt32 deviceNumber)
        {

            deviceList = new LCF_DeviceEthPara_t[0];
            deviceNumber = 0;
            IntPtr devListPt = new IntPtr();
            int ret = LCFAPI_ScanDeviceList(ref devListPt, ref deviceNumber);
            if ((LCF_StatusTypeDef)ret == LCF_StatusTypeDef.LCF_Status_Succeed && deviceNumber > 0)
            {
                deviceList = new LCF_DeviceEthPara_t[deviceNumber];
                byte[] deviceListByteArry = new byte[deviceNumber * Marshal.SizeOf<LCF_DeviceEthPara_t>()];
                Marshal.Copy(devListPt, deviceListByteArry, 0, deviceListByteArry.Length);
                //将byte 数组转换成结构体
                for (int i = 0; i < deviceNumber; i++)
                {
                    deviceList[i] = (LCF_DeviceEthPara_t)LCF_Util.BytesToStuct(deviceListByteArry, i * Marshal.SizeOf<LCF_DeviceEthPara_t>(), typeof(LCF_DeviceEthPara_t));
                }
            }

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ScanDeviceList_Detail:扫描传感器，返回控制器详细信息; 注:使用该接口扫描设备需要关闭PC端防火墙，反则可能会扫描不到设备
          * @param infoList   返回扫描到的设备列表，存放控制器相关的详细信息
          * @param listNum    扫描到的设备个数
          * @return           返回错误码
          */
        public static LCF_StatusTypeDef LCF_ScanDeviceList_Detail(out LCF_DeviceControlInfo_t[] deviceList, out UInt32 deviceNumber)
        {
            deviceList = new LCF_DeviceControlInfo_t[0];
            deviceNumber = 0;
            IntPtr devListPt = new IntPtr();
            int ret = LCFAPI_ScanDeviceList_Detail(ref devListPt, ref deviceNumber);
            if ((LCF_StatusTypeDef)ret == LCF_StatusTypeDef.LCF_Status_Succeed && deviceNumber > 0)
            {
                deviceList = new LCF_DeviceControlInfo_t[deviceNumber];
                byte[] deviceListByteArry = new byte[deviceNumber * Marshal.SizeOf<LCF_DeviceControlInfo_t>()];
                Marshal.Copy(devListPt, deviceListByteArry, 0, deviceListByteArry.Length);
                //将byte 数组转换成结构体
                for (int i = 0; i < deviceNumber; i++)
                {
                    deviceList[i] = (LCF_DeviceControlInfo_t)LCF_Util.BytesToStuct(deviceListByteArry, i * Marshal.SizeOf<LCF_DeviceControlInfo_t>(), typeof(LCF_DeviceControlInfo_t));
                }
            }

            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_ConnectDevice:连接控制器
         * @param controllerIp:  控制器IP，输入NULL则使用默认的通讯参数连接
         * @param controllerPort 控制器端口号，输入0或 IP输入NULL则使用默认的通讯参数连接
         * @param deviceHandler  返回设备句柄
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ConnectDevice(string controllerIp, UInt16 controllerPort, out int deviceHandler)
        {

            deviceHandler = -1;
            IntPtr ptr = Marshal.StringToHGlobalAnsi(controllerIp);
            LCF_StatusTypeDef ret = (LCF_StatusTypeDef)LCFAPI_ConnectDevice(ptr, controllerPort, ref deviceHandler);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        /**
         * @brief LCF_CloseDevice 断开设备连接
         * @param handle          设备句柄
         */
        public static void LCF_CloseDevice(int handle)
        {
            LCFAPI_CloseDevice(handle);
        }

        /**
         * @brief LCF_IsConnect 获取传感器的连接状态
         * @param handle        设备句柄
         * @return 返回连接状态
         */
        public static bool LCF_IsConnect(int handle)
        {
            return LCFAPI_IsConnect(handle) != 0;
        }

        /**
        * @brief LCF_IsStart 获取传感器的采集状态
        * @param handle      设备句柄
        * @return            返回采集状态
        */
        public static bool LCF_IsStart(int handle)
        {
            return LCFAPI_IsStart(handle) != 0;
        }

        /**
		 * @brief LCF_StartCapture 启动传感器测量
		 * @param handle           设备句柄
		 * @return  返回错误码
		 */
        public static LCF_StatusTypeDef LCF_StartCapture(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_StartCapture(handle);
        }

        /**
		 * @brief LCF_StopCapture 停止传感器测量
		 * @param handle          设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_StopCapture(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_StopCapture(handle);
        }

        /**
		 * @brief LCF_RegisterEventCallback 注册事件回调函数
		 * @param eventHandle               回调函数
		 * @param userPara                  用户参数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_RegisterEventCallback(LCF_UserEventCallbackHandleDelegate eventHandle, IntPtr userPara)
        {
            return (LCF_StatusTypeDef)LCFAPI_RegisterEventCallback(eventHandle, userPara);
        }

        /**
		 * @brief LCF_UnregisterEventCallback 注销回调函数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_UnregisterEventCallback()
        {
            return (LCF_StatusTypeDef)LCFAPI_UnregisterEventCallback();
        }

        /**
		 * @brief LCF_SingleShot 单次采集一帧轮廓数据，需要先将传感器设置为软件触发模式并启动采样
		 * @param handle        设备句柄
		 * @param result        返回轮廓数据
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SingleShot(int handle, out LCF_Result_t result)
        {
            result = new LCF_Result_t();
            return (LCF_StatusTypeDef)LCFAPI_SingleShot(handle, ref result);
        }

        /**
		 * @brief LCF_GetErrorInfo 获取传感器错误描述信息
		 * @param handle           设备句柄
		 * @return 返回错误描述字符串
		 */
        public static string LCF_GetErrorInfo(int handle)
        {
            string err = "";
            IntPtr ptr = LCFAPI_GetErrorInfo(handle);
            err = Marshal.PtrToStringAnsi(ptr);
            return err;
        }

        /**
         * @brief LCF_Reboot    重启控制器
         * @param handle        设备句柄
         * @return              返回错误描述字符串
         */
        public static LCF_StatusTypeDef LCF_Reboot(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_Reboot(handle);
        }

        /**
         * @brief LCF_Shutdown  关闭控制器
         * @param handle        设备句柄
         * @return              返回错误码
         */
        public static LCF_StatusTypeDef LCF_Shutdown(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_Shutdown(handle);
        }

        /**
         * @brief LCF_SetIntParameter 设置Int类型参数接口，
         * @param handle              设备句柄
         * @param paramName           参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value               设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetIntParameter(int handle, string paramName, int value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetIntParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetStringParameter 设置字符串类型参数接口
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetStringParameter(int handle, string paramName, string value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetStringParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetFloatParameter 设置Float类型参数接口，
         * @param handle                设备句柄
         * @param paramName             参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                 设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetFloatParameter(int handle, string paramName, float value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetFloatParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetDoubleParameter 设置Double类型参数接口，
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetDoubleParameter(int handle, string paramName, double value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetDoubleParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetIntParameter 获取Int类型参数值的接口
         * @param handle              设备句柄
         * @param paramName           参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value               返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetIntParameter(int handle, string paramName, out int value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetIntParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetFloatParameter 获取Float类型参数值的接口
         * @param handle             设备句柄
         * @param paramName          参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value              返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetFloatParameter(int handle, string paramName, out float value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetFloatParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetDoubleParameter 获取Double类型参数值的接口
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetDoubleParameter(int handle, string paramName, out double value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetDoubleParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
        * @brief LCF_GetStringParameter 获取字符串类型参数值的接口
        * @param handle                 设备句柄
        * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
        * @param value                  返回值
        * @return
        */
        public static LCF_StatusTypeDef LCF_GetStringParameter(int handle, string paramName, out string value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            int len = 256;
            value = "";
            IntPtr mem = Marshal.AllocHGlobal(len);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetStringParameter(handle, name, mem);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
                value = Marshal.PtrToStringAnsi(mem);

            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(name);
            Marshal.FreeHGlobal(mem);
            return (LCF_StatusTypeDef)ret;
        }

        /**
		 * @brief LCF_GetDeviceSetting 获取传感器所有设置
		 * @param handle        设备句柄
		 * @param setting       返回传感器所有设置信息
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_GetDeviceSetting(int handle, out LCF_DeviceSetting_t setting)
        {
            setting = new LCF_DeviceSetting_t();
            return (LCF_StatusTypeDef)LCFAPI_GetDeviceSetting(handle, ref setting);
        }

        /**
		 * @brief LCF_SaveSetting 保存当前传感器设置
		 * @param handle          设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SaveSetting(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_SaveSetting(handle);
        }

        /**
		 * @brief LCF_RestoreFactorySetting  恢复出厂设置
		 * @param handle        设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_RestoreFactorySetting(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_RestoreFactorySetting(handle);
        }

        /**
		 * @brief LCF_ExportCacheData 导出控制器缓存的所有轮廓数据
		 * @param handle            设备句柄
		 * @param result            深度数据
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ExportCacheData(int handle, out LCF_Result_t result)
        {

            result = new LCF_Result_t();
            return (LCF_StatusTypeDef)LCFAPI_ExportCacheData(handle, ref result);
        }

        /**
		 * @brief LCF_ClearCacheData 清空缓存数据
		 * @param handle             设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ClearCacheData(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_ClearCacheData(handle);
        }

        /**
		 * @brief LCF_SetHDRIntensityGroup 设置某个HDR模式下，每一帧的光强值
		 * @param handle        设备句柄
		 * @param HDR_Mode      HDR模式
		 * @param intenSity     每一帧的光强值，例:如果HDR_Mode为LCF_HDRMode_4，则该数组指定4帧数据的光强值
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SetHDRIntensityGroup(int handle, LCF_HDRMode_t HDR_Mode, float[] intenSity)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetHDRIntensityGroup(handle, (int)HDR_Mode, intenSity);
        }

        /**
        * @brief LCF_SetHDRExposureGroup 设置某个HDR模式下，每一帧的曝光时间
        * @param handle        设备句柄
        * @param HDR_Mode      HDR模式
        * @param exposure      每一帧的曝光时间，例:如果HDR_Mode为LCF_HDRMode_4，则该数组指定4帧数据的曝光时间
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_SetHDRExposureGroup(int handle, LCF_HDRMode_t HDR_Mode, int[] exposure)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetHDRExposureGroup(handle, (int)HDR_Mode, exposure);
        }

        /**
        * @brief LCF_GetDeviceStatisticInfo  获取设备统计信息
        * @param handle                      设备句柄
        * @param info                        返回设备统计信息
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_GetDeviceStatisticInfo(int handle, out LCF_DeviceStatisticInfo_t info)
        {
            info = new LCF_DeviceStatisticInfo_t();
            return (LCF_StatusTypeDef)LCFAPI_GetDeviceStatisticInfo(handle, ref info);
        }

        /**
		 * @brief LCF_ReadSensorHeadFactoryPara 读取传感器出厂参数
		 * @param handle        设备句柄
		 * @param para          返回传感器出参数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ReadSensorHeadFactoryPara(int handle, out LCF_SensorHeadFactoryPara_t para)
        {
            para = new LCF_SensorHeadFactoryPara_t();
            return (LCF_StatusTypeDef)LCFAPI_ReadSensorHeadFactoryPara(handle, ref para);
        }

        /**
         * @brief LCF_ClearEncoderTriggerCount   清除编码器计数值，控制器编码器计数从0开始，计数到用户设定的分频值后触发传感器采集
         * @param handle                         设备句柄
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ClearEncoderTriggerCount(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_ClearEncoderTriggerCount(handle);
        }

        /**
         * @brief LCF_ExportDeviceSetting 导出设备配置信息到文件中
         * @param handle                  设备句柄
         * @param path                    导出路径
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ExportDeviceSetting(int handle, string path)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr path_ptr = Marshal.StringToHGlobalAnsi(path);
            do
            {
                ret = LCFAPI_ExportDeviceSetting(handle, path_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(path_ptr);
            return (LCF_StatusTypeDef)ret;

        }

        /**
         * @brief LCF_ImportDeviceSetting 导入设备配置文件
         * @param handle                  设备句柄
         * @param fileName                文件路径名
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ImportDeviceSetting(int handle, string fileName)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr fileName_ptr = Marshal.StringToHGlobalAnsi(fileName);
            do
            {
                ret = LCFAPI_ImportDeviceSetting(handle, fileName_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(fileName_ptr);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_AdjustRoiForFps 如果当前设置无法满足用户指定帧率，则自动适配合适的ROI分辨率，适应指定的帧率
         * @param handle              设备句柄
         * @param mode                适配模式
         * @param frameRate           指定帧率
         * @param Z_Range             返回适配成功后的Z轴理论量程，单位um
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_AdjustRoiForFps(int handle, LCF_AdjustRoiFpsPara_t mode, int frameRate, out float Z_Range)
        {
            Z_Range = 0;
            return (LCF_StatusTypeDef)LCFAPI_AdjustRoiForFps(handle, (int)mode, frameRate, ref Z_Range);
        }

        /**
         * @brief LCF_SetMeasureRange 设置传感器量程，例：LCF1000传感器量程为 +-1600um，可以设置的远端范围最小为-1600，近端最大范围为1600
         * @param handle              设备句柄
         * @param farRange_um         远端测量范围,单位um
         * @param nearRange_um        近端测量范围,单位um
         * @return  返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetMeasureRange(int handle, int farRange_um, int nearRange_um)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetMeasureRange(handle, farRange_um, nearRange_um);
        }

        /**
        * @brief LCF_SetIOPortFunc  设置控制器IO引脚(IN0、IN1、OUT0、OUT1)的功能
        * @param handle             设备句柄
        * @param io_port            IO引脚
        * @param triggerMode        IO引脚的触发方式
        * @param io_func            IO引脚绑定的功能
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_SetIOPortFunc(int handle, LCF_IO_Port_t io_port, LCF_IO_TriggerMode_t triggerMode, LCF_IO_Func_t io_func)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetIOPortFunc(handle, (int)io_port, (int)triggerMode, (int)io_func);
        }

        /**
         * @brief LCF_FindGoodLight 根据当前测量位置，查找合适的光强
         * @param handle            设备句柄
         * @param enHDR             是否在HDR模式下查找，0:不使用HDR 1:使用HDR
         * @return                  返回错误码
         */
        public static LCF_StatusTypeDef LCF_FindGoodLight(int handle, int enHDR)
        {
            return (LCF_StatusTypeDef)LCFAPI_FindGoodLight(handle, enHDR);
        }

        /**
         * @brief LCF_SetEncoderDyncDivValue 设置动态编码器分频模式下，各个区域的分频值；用户可以根据编码器脉冲个数设定几个不同的分频值。例:在0~5000个编码器脉冲区间使用一个分频值，在5000~10000个脉冲区间使用另外一个分频值
         * @param handle            设备句柄
         * @param encoderPos        不同区域编码器脉冲个数，例:encoderPos[0]=1000,division[0]=16,则在编码器0~1000个脉冲区间使用16分频，encoderPos[1]=2000,division[0]=4,则在编码1000~2000个脉冲区间使用4分频；
         * @param division          不同区域编码器分频值
         * @param group_member      分频组数
         * @return                  返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetEncoderDyncDivValue(int handle, int[] encoderPos, int[] division, int[] real_div, int group_member)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetEncoderDyncDivValue(handle, encoderPos, division, real_div, group_member);
        }





        /**
          * @brief LCF_SetControllerParam 通过广播命令设置控制器参数，该接口不受SDK连接状态影响，在SDK未连接控制器前依旧可以调用;
          *                               为了使控制器能够接收到广播命令，需要将本地防火墙关闭。
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param paramName     参数名，包含的参数可以参考LCF_ParamterDefine.cs文件PARAM_CTRL_XXXX类型的控制参数
          * @param value         设置的数据
          * @return  返回错误码
          */
        public static LCF_StatusTypeDef LCF_SetControllerParam(string controlerSN, string paramName, IntPtr value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);

            do
            {
                ret = LCFAPI_SetControllerParam(sn, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            } while (false);


            //释放内存
            Marshal.FreeHGlobal(sn);
            Marshal.FreeHGlobal(name);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_SetControllerParam 通过广播命令获取控制器参数，该接口不受SDK连接状态影响，在SDK未连接控制器前依旧可以调用;
          *                               为了使控制器能够接收到广播命令，需要将本地防火墙关闭。
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param paramName     参数名，包含的参数可以参考LCF_ParamterDefine.cs文件PARAM_CTRL_XXXX类型的控制参数
          * @param value         设置的数据,用户自己根据参数类型将value进行转换
          * @return  返回错误码
          */
        public static LCF_StatusTypeDef LCF_GetControllerParam(string controlerSN, string paramName, out IntPtr value)
        {
            int len = 256;
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            value = Marshal.AllocHGlobal(len);

            do
            {
                ret = LCFAPI_GetControllerParam(sn, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    Marshal.FreeHGlobal(value);
                    value = IntPtr.Zero;
                    break;
                }
            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(sn);
            Marshal.FreeHGlobal(name);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ForceIP  临时修改控制器IP参数,用户可以根据本地网口的网段，将控制器IP设置在同一网段下;控制器重新启动后IP恢复为初始值;需要永久修改控制器IP可以使用LCF_SetEthParam接口或连接上控制器后,使用PARAM_IP_ADDR参数进行修改
          * @param controlerSN  通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param ip            控制器IP
          * @param gateway       控制器网关
          * @param netmask       控制器子网掩码
          * @return
          */
        public static LCF_StatusTypeDef LCF_ForceIP(string controlerSN, string ip, string gateway, string netmask)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn_ptr = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr ip_ptr = Marshal.StringToHGlobalAnsi(ip);
            IntPtr gateway_ptr = Marshal.StringToHGlobalAnsi(gateway);
            IntPtr netmask_ptr = Marshal.StringToHGlobalAnsi(netmask);

            do
            {
                ret = LCFAPI_ForceIP(sn_ptr, ip_ptr, gateway_ptr, netmask_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            }
            while (false);

            Marshal.FreeHGlobal(sn_ptr);
            Marshal.FreeHGlobal(ip_ptr);
            Marshal.FreeHGlobal(gateway_ptr);
            Marshal.FreeHGlobal(netmask_ptr);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ForceIP   永久修改控制器IP参数，修改完成后重启控制器后生效
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param ip            控制器IP
          * @param gateway       控制器网关
          * @param netmask       控制器子网掩码
          * @return
          */
        public static LCF_StatusTypeDef LCF_SetEthParam(string controlerSN, string ip, string gateway, string netmask)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn_ptr = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr ip_ptr = Marshal.StringToHGlobalAnsi(ip);
            IntPtr gateway_ptr = Marshal.StringToHGlobalAnsi(gateway);
            IntPtr netmask_ptr = Marshal.StringToHGlobalAnsi(netmask);

            do
            {
                ret = LCFAPI_SetEthParam(sn_ptr, ip_ptr, gateway_ptr, netmask_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            }
            while (false);

            Marshal.FreeHGlobal(sn_ptr);
            Marshal.FreeHGlobal(ip_ptr);
            Marshal.FreeHGlobal(gateway_ptr);
            Marshal.FreeHGlobal(netmask_ptr);

            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_WriteUserFlashData 用户自定义数据写入传感头内部Flash
         * @param handle        设备句柄
         * @param data          写入数据
         * @param len           数据长度,单位字节
         * @return
         */
        public static LCF_StatusTypeDef LCF_WriteUserFlashData(int handle, IntPtr data, int len)
        {
            return (LCF_StatusTypeDef)LCFAPI_WriteUserFlashData(handle, data, len);
        }

        /**
         * @brief LCF_ReadUserFlashData 读取传感头内部Flash用户写入的数据
         * @param handle         设备句柄
         * @param data           返回用户数据
         * @param len           返回数据长度,单位字节。如果Flash没被写入过数据则返回数据长度为0
         * @return
         */
        public static LCF_StatusTypeDef LCF_ReadUserFlashData(int handle, IntPtr data, out int len)
        {
            len = 0;
            return (LCF_StatusTypeDef)LCFAPI_ReadUserFlashData(handle, data, ref len);
        }


        /**
         * @brief LCF_GetMaxROIFrameRate 获取用户指定的分辨率下CMOS支持的最大帧率
         * @param handle             设备句柄
         * @param x_res              X分辨率
         * @param y_res              Y分辨率
         * @param maxFrameRate       返回最大帧率
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetMaxROIFrameRate(int handle, UInt32 x_res, UInt32 y_res, out UInt32 maxFrameRate)
        {
            maxFrameRate = 0;
            return (LCF_StatusTypeDef)LCFAPI_GetMaxROIFrameRate(handle, x_res, y_res, ref maxFrameRate);
        }

        /**
         * @brief LCF_SetSignalOutput 设置单点信号数据输出
         * @param handle             设备句柄
         * @param en                 true:使能传输单点信号数据  false:关闭单点信号数据传输
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetSignalOutput(int handle, bool en, int signalIndex)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetSignalOutput(handle, en ? 1 : 0, signalIndex);
        }

        /**
         * @brief LCF_SetSignalOutput 获取单个信号数据
         * @param handle             设备句柄
         * @param signal             传入数组，将非托管内存中的数据拷贝到其中
         * @param signalLen          输出信号的整体长度
         * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_GetLastSignal(int handle, short[] signal, out UInt32 signalLen)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr mem = IntPtr.Zero;
            mem = Marshal.AllocHGlobal(MAX_ROI_Y_RES * 2);
            signalLen = 0;
            do
            {
                ret = LCFAPI_GetLastSignal(handle, mem, ref signalLen);

                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

                Marshal.Copy(mem, signal, 0, (int)signalLen);
            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(mem);
            return (LCF_StatusTypeDef)ret;
        }
    }
    partial class LCFDevice2 : ILCFDevice
    {
        public const int MINI_ROI_X_RES = 64;             //X轴最小分辨率
        public const int MINI_ROI_Y_RES = 56;             //Y轴最小分辨率
        public const int MAX_ROI_Y_RES = 1080;
        public const float INVALID_VALUE = 888888;        //信号无效值
        public const int MAX_HDR_NUMBER = 8;              //最大HDR次数
        public const int MAX_DETECT_LAYER_NUMBER = 4;     //最大检测层数
        public const int MAX_DYNAMIC_DIV_GROUP = 3;       //动态分频最大组数

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ScanDeviceList(ref IntPtr deviceList, ref UInt32 deviceNumber);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ScanDeviceList_Detail(ref IntPtr deviceList, ref UInt32 deviceNumber);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ConnectDevice(IntPtr cotrollerIP, UInt16 controllerPort, ref int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static void LCFAPI_CloseDevice(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_IsConnect(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_IsStart(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_StartCapture(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_StopCapture(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_RegisterEventCallback(LCF_UserEventCallbackHandleDelegate eventHandle, IntPtr userPara);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_UnregisterEventCallback();

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SingleShot(int handle, ref LCF_Result_t result);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static IntPtr LCFAPI_GetErrorInfo(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_Reboot(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_Shutdown(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetIntParameter(int handle, IntPtr paramName, int value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetFloatParameter(int handle, IntPtr paramName, float value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetDoubleParameter(int handle, IntPtr paramName, double value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetStringParameter(int handle, IntPtr paramName, string value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetIntParameter(int handle, IntPtr paramName, ref int value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetFloatParameter(int handle, IntPtr paramName, ref float value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDoubleParameter(int handle, IntPtr paramName, ref double value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetStringParameter(int handle, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDeviceSetting(int handle, ref LCF_DeviceSetting_t setting);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SaveSetting(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_RestoreFactorySetting(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ExportCacheData(int handle, ref LCF_Result_t result);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ClearCacheData(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetHDRIntensityGroup(int handle, int HDR_Mode, float[] intenSity);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetHDRExposureGroup(int handle, int HDR_Mode, int[] exposure);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetDeviceStatisticInfo(int handle, ref LCF_DeviceStatisticInfo_t info);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ReadSensorHeadFactoryPara(int handle, ref LCF_SensorHeadFactoryPara_t para);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ClearEncoderTriggerCount(int handle);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ExportDeviceSetting(int handle, IntPtr path);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ImportDeviceSetting(int handle, IntPtr fileName);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_AdjustRoiForFps(int handle, int mode, int frameRate, ref float Z_Rangeo);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetMeasureRange(int handle, int farRange_um, int nearRange_um);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetIOPortFunc(int handle, int io_port, int triggerMode, int io_func);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_FindGoodLight(int hahandlendle, int enHDR);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetEncoderDyncDivValue(int hahandlendle, int[] encoderPos, int[] division, int[] real_div, int group_member);



        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetControllerParam(IntPtr controlerSN, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetControllerParam(IntPtr controlerSN, IntPtr paramName, IntPtr value);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ForceIP(IntPtr controlerSN, IntPtr ip, IntPtr gateway, IntPtr netmask);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetEthParam(IntPtr controlerSN, IntPtr ip, IntPtr gateway, IntPtr netmask);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_WriteUserFlashData(int handle, IntPtr data, int len);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_ReadUserFlashData(int handle, IntPtr data, ref int len);



        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetMaxROIFrameRate(int handle, UInt32 x_res, UInt32 y_res, ref UInt32 maxFrameRate);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_SetSignalOutput(int handle, int en, int signalIndex);

        [DllImport("hps_lcf_sdk.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        extern static int LCFAPI_GetLastSignal(int handle, IntPtr signal, ref UInt32 signalLen);



        public static double[] getHeightValue(IntPtr ptr, Int32 count, double precision = 10.0)
        {


            double[] ret = new double[count];
            int[] temp = new int[count];

            Marshal.Copy(ptr, temp, 0, count);

            for (int i = 0; i < count; i++)
                ret[i] = (float)(temp[i] / precision);

            temp = null;
            return ret;
        }

        public static short[] getIntensityValue(IntPtr ptr, Int32 count)
        {
            short[] ret = new short[count];
            Marshal.Copy(ptr, ret, 0, count);
            return ret;
        }

        public static LCF_Result_t getLCFResult(IntPtr ptr)
        {

            return (LCF_Result_t)Marshal.PtrToStructure(ptr, typeof(LCF_Result_t));

        }



        /**
        * @brief LCF_ScanDeviceList 扫描传感器，返回传感器IP和端口号; 注:使用该接口扫描设备需要关闭PC端防火墙，反则可能会扫描不到设备
        * @param deviceList    返回扫描到的设备列表，存放IP和端口信息
        * @param deviceNumber  扫描到的设备个数
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_ScanDeviceList(out LCF_DeviceEthPara_t[] deviceList, out UInt32 deviceNumber)
        {

            deviceList = new LCF_DeviceEthPara_t[0];
            deviceNumber = 0;
            IntPtr devListPt = new IntPtr();
            int ret = LCFAPI_ScanDeviceList(ref devListPt, ref deviceNumber);
            if ((LCF_StatusTypeDef)ret == LCF_StatusTypeDef.LCF_Status_Succeed && deviceNumber > 0)
            {
                deviceList = new LCF_DeviceEthPara_t[deviceNumber];
                byte[] deviceListByteArry = new byte[deviceNumber * Marshal.SizeOf<LCF_DeviceEthPara_t>()];
                Marshal.Copy(devListPt, deviceListByteArry, 0, deviceListByteArry.Length);
                //将byte 数组转换成结构体
                for (int i = 0; i < deviceNumber; i++)
                {
                    deviceList[i] = (LCF_DeviceEthPara_t)LCF_Util.BytesToStuct(deviceListByteArry, i * Marshal.SizeOf<LCF_DeviceEthPara_t>(), typeof(LCF_DeviceEthPara_t));
                }
            }

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ScanDeviceList_Detail:扫描传感器，返回控制器详细信息; 注:使用该接口扫描设备需要关闭PC端防火墙，反则可能会扫描不到设备
          * @param infoList   返回扫描到的设备列表，存放控制器相关的详细信息
          * @param listNum    扫描到的设备个数
          * @return           返回错误码
          */
        public static LCF_StatusTypeDef LCF_ScanDeviceList_Detail(out LCF_DeviceControlInfo_t[] deviceList, out UInt32 deviceNumber)
        {
            deviceList = new LCF_DeviceControlInfo_t[0];
            deviceNumber = 0;
            IntPtr devListPt = new IntPtr();
            int ret = LCFAPI_ScanDeviceList_Detail(ref devListPt, ref deviceNumber);
            if ((LCF_StatusTypeDef)ret == LCF_StatusTypeDef.LCF_Status_Succeed && deviceNumber > 0)
            {
                deviceList = new LCF_DeviceControlInfo_t[deviceNumber];
                byte[] deviceListByteArry = new byte[deviceNumber * Marshal.SizeOf<LCF_DeviceControlInfo_t>()];
                Marshal.Copy(devListPt, deviceListByteArry, 0, deviceListByteArry.Length);
                //将byte 数组转换成结构体
                for (int i = 0; i < deviceNumber; i++)
                {
                    deviceList[i] = (LCF_DeviceControlInfo_t)LCF_Util.BytesToStuct(deviceListByteArry, i * Marshal.SizeOf<LCF_DeviceControlInfo_t>(), typeof(LCF_DeviceControlInfo_t));
                }
            }

            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_ConnectDevice:连接控制器
         * @param controllerIp:  控制器IP，输入NULL则使用默认的通讯参数连接
         * @param controllerPort 控制器端口号，输入0或 IP输入NULL则使用默认的通讯参数连接
         * @param deviceHandler  返回设备句柄
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ConnectDevice(string controllerIp, UInt16 controllerPort, out int deviceHandler)
        {

            deviceHandler = -1;
            IntPtr ptr = Marshal.StringToHGlobalAnsi(controllerIp);
            LCF_StatusTypeDef ret = (LCF_StatusTypeDef)LCFAPI_ConnectDevice(ptr, controllerPort, ref deviceHandler);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        /**
         * @brief LCF_CloseDevice 断开设备连接
         * @param handle          设备句柄
         */
        public static void LCF_CloseDevice(int handle)
        {
            LCFAPI_CloseDevice(handle);
        }

        /**
         * @brief LCF_IsConnect 获取传感器的连接状态
         * @param handle        设备句柄
         * @return 返回连接状态
         */
        public static bool LCF_IsConnect(int handle)
        {
            return LCFAPI_IsConnect(handle) != 0;
        }

        /**
        * @brief LCF_IsStart 获取传感器的采集状态
        * @param handle      设备句柄
        * @return            返回采集状态
        */
        public static bool LCF_IsStart(int handle)
        {
            return LCFAPI_IsStart(handle) != 0;
        }

        /**
		 * @brief LCF_StartCapture 启动传感器测量
		 * @param handle           设备句柄
		 * @return  返回错误码
		 */
        public static LCF_StatusTypeDef LCF_StartCapture(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_StartCapture(handle);
        }

        /**
		 * @brief LCF_StopCapture 停止传感器测量
		 * @param handle          设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_StopCapture(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_StopCapture(handle);
        }

        /**
		 * @brief LCF_RegisterEventCallback 注册事件回调函数
		 * @param eventHandle               回调函数
		 * @param userPara                  用户参数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_RegisterEventCallback(LCF_UserEventCallbackHandleDelegate eventHandle, IntPtr userPara)
        {
            return (LCF_StatusTypeDef)LCFAPI_RegisterEventCallback(eventHandle, userPara);
        }

        /**
		 * @brief LCF_UnregisterEventCallback 注销回调函数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_UnregisterEventCallback()
        {
            return (LCF_StatusTypeDef)LCFAPI_UnregisterEventCallback();
        }

        /**
		 * @brief LCF_SingleShot 单次采集一帧轮廓数据，需要先将传感器设置为软件触发模式并启动采样
		 * @param handle        设备句柄
		 * @param result        返回轮廓数据
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SingleShot(int handle, out LCF_Result_t result)
        {
            result = new LCF_Result_t();
            return (LCF_StatusTypeDef)LCFAPI_SingleShot(handle, ref result);
        }

        /**
		 * @brief LCF_GetErrorInfo 获取传感器错误描述信息
		 * @param handle           设备句柄
		 * @return 返回错误描述字符串
		 */
        public static string LCF_GetErrorInfo(int handle)
        {
            string err = "";
            IntPtr ptr = LCFAPI_GetErrorInfo(handle);
            err = Marshal.PtrToStringAnsi(ptr);
            return err;
        }

        /**
         * @brief LCF_Reboot    重启控制器
         * @param handle        设备句柄
         * @return              返回错误描述字符串
         */
        public static LCF_StatusTypeDef LCF_Reboot(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_Reboot(handle);
        }

        /**
         * @brief LCF_Shutdown  关闭控制器
         * @param handle        设备句柄
         * @return              返回错误码
         */
        public static LCF_StatusTypeDef LCF_Shutdown(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_Shutdown(handle);
        }

        /**
         * @brief LCF_SetIntParameter 设置Int类型参数接口，
         * @param handle              设备句柄
         * @param paramName           参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value               设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetIntParameter(int handle, string paramName, int value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetIntParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetStringParameter 设置字符串类型参数接口
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetStringParameter(int handle, string paramName, string value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetStringParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetFloatParameter 设置Float类型参数接口，
         * @param handle                设备句柄
         * @param paramName             参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                 设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetFloatParameter(int handle, string paramName, float value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetFloatParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_SetDoubleParameter 设置Double类型参数接口，
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  设置的值
         * @return
         */
        public static LCF_StatusTypeDef LCF_SetDoubleParameter(int handle, string paramName, double value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_SetDoubleParameter(handle, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetIntParameter 获取Int类型参数值的接口
         * @param handle              设备句柄
         * @param paramName           参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value               返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetIntParameter(int handle, string paramName, out int value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetIntParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetFloatParameter 获取Float类型参数值的接口
         * @param handle             设备句柄
         * @param paramName          参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value              返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetFloatParameter(int handle, string paramName, out float value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetFloatParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_GetDoubleParameter 获取Double类型参数值的接口
         * @param handle                 设备句柄
         * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
         * @param value                  返回值
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetDoubleParameter(int handle, string paramName, out double value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            value = 0;
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetDoubleParameter(handle, name, ref value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(name);
            return (LCF_StatusTypeDef)ret;
        }

        /**
        * @brief LCF_GetStringParameter 获取字符串类型参数值的接口
        * @param handle                 设备句柄
        * @param paramName              参数名，包含的参数可以参考LCF_ParamterDefine.h文件
        * @param value                  返回值
        * @return
        */
        public static LCF_StatusTypeDef LCF_GetStringParameter(int handle, string paramName, out string value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            int len = 256;
            value = "";
            IntPtr mem = Marshal.AllocHGlobal(len);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            do
            {
                ret = LCFAPI_GetStringParameter(handle, name, mem);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
                value = Marshal.PtrToStringAnsi(mem);

            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(name);
            Marshal.FreeHGlobal(mem);
            return (LCF_StatusTypeDef)ret;
        }

        /**
		 * @brief LCF_GetDeviceSetting 获取传感器所有设置
		 * @param handle        设备句柄
		 * @param setting       返回传感器所有设置信息
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_GetDeviceSetting(int handle, out LCF_DeviceSetting_t setting)
        {
            setting = new LCF_DeviceSetting_t();
            return (LCF_StatusTypeDef)LCFAPI_GetDeviceSetting(handle, ref setting);
        }

        /**
		 * @brief LCF_SaveSetting 保存当前传感器设置
		 * @param handle          设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SaveSetting(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_SaveSetting(handle);
        }

        /**
		 * @brief LCF_RestoreFactorySetting  恢复出厂设置
		 * @param handle        设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_RestoreFactorySetting(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_RestoreFactorySetting(handle);
        }

        /**
		 * @brief LCF_ExportCacheData 导出控制器缓存的所有轮廓数据
		 * @param handle            设备句柄
		 * @param result            深度数据
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ExportCacheData(int handle, out LCF_Result_t result)
        {

            result = new LCF_Result_t();
            return (LCF_StatusTypeDef)LCFAPI_ExportCacheData(handle, ref result);
        }

        /**
		 * @brief LCF_ClearCacheData 清空缓存数据
		 * @param handle             设备句柄
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ClearCacheData(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_ClearCacheData(handle);
        }

        /**
		 * @brief LCF_SetHDRIntensityGroup 设置某个HDR模式下，每一帧的光强值
		 * @param handle        设备句柄
		 * @param HDR_Mode      HDR模式
		 * @param intenSity     每一帧的光强值，例:如果HDR_Mode为LCF_HDRMode_4，则该数组指定4帧数据的光强值
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_SetHDRIntensityGroup(int handle, LCF_HDRMode_t HDR_Mode, float[] intenSity)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetHDRIntensityGroup(handle, (int)HDR_Mode, intenSity);
        }

        /**
        * @brief LCF_SetHDRExposureGroup 设置某个HDR模式下，每一帧的曝光时间
        * @param handle        设备句柄
        * @param HDR_Mode      HDR模式
        * @param exposure      每一帧的曝光时间，例:如果HDR_Mode为LCF_HDRMode_4，则该数组指定4帧数据的曝光时间
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_SetHDRExposureGroup(int handle, LCF_HDRMode_t HDR_Mode, int[] exposure)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetHDRExposureGroup(handle, (int)HDR_Mode, exposure);
        }

        /**
        * @brief LCF_GetDeviceStatisticInfo  获取设备统计信息
        * @param handle                      设备句柄
        * @param info                        返回设备统计信息
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_GetDeviceStatisticInfo(int handle, out LCF_DeviceStatisticInfo_t info)
        {
            info = new LCF_DeviceStatisticInfo_t();
            return (LCF_StatusTypeDef)LCFAPI_GetDeviceStatisticInfo(handle, ref info);
        }

        /**
		 * @brief LCF_ReadSensorHeadFactoryPara 读取传感器出厂参数
		 * @param handle        设备句柄
		 * @param para          返回传感器出参数
		 * @return 返回错误码
		 */
        public static LCF_StatusTypeDef LCF_ReadSensorHeadFactoryPara(int handle, out LCF_SensorHeadFactoryPara_t para)
        {
            para = new LCF_SensorHeadFactoryPara_t();
            return (LCF_StatusTypeDef)LCFAPI_ReadSensorHeadFactoryPara(handle, ref para);
        }

        /**
         * @brief LCF_ClearEncoderTriggerCount   清除编码器计数值，控制器编码器计数从0开始，计数到用户设定的分频值后触发传感器采集
         * @param handle                         设备句柄
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ClearEncoderTriggerCount(int handle)
        {
            return (LCF_StatusTypeDef)LCFAPI_ClearEncoderTriggerCount(handle);
        }

        /**
         * @brief LCF_ExportDeviceSetting 导出设备配置信息到文件中
         * @param handle                  设备句柄
         * @param path                    导出路径
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ExportDeviceSetting(int handle, string path)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr path_ptr = Marshal.StringToHGlobalAnsi(path);
            do
            {
                ret = LCFAPI_ExportDeviceSetting(handle, path_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(path_ptr);
            return (LCF_StatusTypeDef)ret;

        }

        /**
         * @brief LCF_ImportDeviceSetting 导入设备配置文件
         * @param handle                  设备句柄
         * @param fileName                文件路径名
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_ImportDeviceSetting(int handle, string fileName)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr fileName_ptr = Marshal.StringToHGlobalAnsi(fileName);
            do
            {
                ret = LCFAPI_ImportDeviceSetting(handle, fileName_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

            }
            while (false);

            Marshal.FreeHGlobal(fileName_ptr);
            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_AdjustRoiForFps 如果当前设置无法满足用户指定帧率，则自动适配合适的ROI分辨率，适应指定的帧率
         * @param handle              设备句柄
         * @param mode                适配模式
         * @param frameRate           指定帧率
         * @param Z_Range             返回适配成功后的Z轴理论量程，单位um
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_AdjustRoiForFps(int handle, LCF_AdjustRoiFpsPara_t mode, int frameRate, out float Z_Range)
        {
            Z_Range = 0;
            return (LCF_StatusTypeDef)LCFAPI_AdjustRoiForFps(handle, (int)mode, frameRate, ref Z_Range);
        }

        /**
         * @brief LCF_SetMeasureRange 设置传感器量程，例：LCF1000传感器量程为 +-1600um，可以设置的远端范围最小为-1600，近端最大范围为1600
         * @param handle              设备句柄
         * @param farRange_um         远端测量范围,单位um
         * @param nearRange_um        近端测量范围,单位um
         * @return  返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetMeasureRange(int handle, int farRange_um, int nearRange_um)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetMeasureRange(handle, farRange_um, nearRange_um);
        }

        /**
        * @brief LCF_SetIOPortFunc  设置控制器IO引脚(IN0、IN1、OUT0、OUT1)的功能
        * @param handle             设备句柄
        * @param io_port            IO引脚
        * @param triggerMode        IO引脚的触发方式
        * @param io_func            IO引脚绑定的功能
        * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_SetIOPortFunc(int handle, LCF_IO_Port_t io_port, LCF_IO_TriggerMode_t triggerMode, LCF_IO_Func_t io_func)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetIOPortFunc(handle, (int)io_port, (int)triggerMode, (int)io_func);
        }

        /**
         * @brief LCF_FindGoodLight 根据当前测量位置，查找合适的光强
         * @param handle            设备句柄
         * @param enHDR             是否在HDR模式下查找，0:不使用HDR 1:使用HDR
         * @return                  返回错误码
         */
        public static LCF_StatusTypeDef LCF_FindGoodLight(int handle, int enHDR)
        {
            return (LCF_StatusTypeDef)LCFAPI_FindGoodLight(handle, enHDR);
        }

        /**
         * @brief LCF_SetEncoderDyncDivValue 设置动态编码器分频模式下，各个区域的分频值；用户可以根据编码器脉冲个数设定几个不同的分频值。例:在0~5000个编码器脉冲区间使用一个分频值，在5000~10000个脉冲区间使用另外一个分频值
         * @param handle            设备句柄
         * @param encoderPos        不同区域编码器脉冲个数，例:encoderPos[0]=1000,division[0]=16,则在编码器0~1000个脉冲区间使用16分频，encoderPos[1]=2000,division[0]=4,则在编码1000~2000个脉冲区间使用4分频；
         * @param division          不同区域编码器分频值
         * @param group_member      分频组数
         * @return                  返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetEncoderDyncDivValue(int handle, int[] encoderPos, int[] division, int[] real_div, int group_member)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetEncoderDyncDivValue(handle, encoderPos, division, real_div, group_member);
        }





        /**
          * @brief LCF_SetControllerParam 通过广播命令设置控制器参数，该接口不受SDK连接状态影响，在SDK未连接控制器前依旧可以调用;
          *                               为了使控制器能够接收到广播命令，需要将本地防火墙关闭。
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param paramName     参数名，包含的参数可以参考LCF_ParamterDefine.cs文件PARAM_CTRL_XXXX类型的控制参数
          * @param value         设置的数据
          * @return  返回错误码
          */
        public static LCF_StatusTypeDef LCF_SetControllerParam(string controlerSN, string paramName, IntPtr value)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);

            do
            {
                ret = LCFAPI_SetControllerParam(sn, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            } while (false);


            //释放内存
            Marshal.FreeHGlobal(sn);
            Marshal.FreeHGlobal(name);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_SetControllerParam 通过广播命令获取控制器参数，该接口不受SDK连接状态影响，在SDK未连接控制器前依旧可以调用;
          *                               为了使控制器能够接收到广播命令，需要将本地防火墙关闭。
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param paramName     参数名，包含的参数可以参考LCF_ParamterDefine.cs文件PARAM_CTRL_XXXX类型的控制参数
          * @param value         设置的数据,用户自己根据参数类型将value进行转换
          * @return  返回错误码
          */
        public static LCF_StatusTypeDef LCF_GetControllerParam(string controlerSN, string paramName, out IntPtr value)
        {
            int len = 256;
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr name = Marshal.StringToHGlobalAnsi(paramName);
            value = Marshal.AllocHGlobal(len);

            do
            {
                ret = LCFAPI_GetControllerParam(sn, name, value);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    Marshal.FreeHGlobal(value);
                    value = IntPtr.Zero;
                    break;
                }
            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(sn);
            Marshal.FreeHGlobal(name);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ForceIP  临时修改控制器IP参数,用户可以根据本地网口的网段，将控制器IP设置在同一网段下;控制器重新启动后IP恢复为初始值;需要永久修改控制器IP可以使用LCF_SetEthParam接口或连接上控制器后,使用PARAM_IP_ADDR参数进行修改
          * @param controlerSN  通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param ip            控制器IP
          * @param gateway       控制器网关
          * @param netmask       控制器子网掩码
          * @return
          */
        public static LCF_StatusTypeDef LCF_ForceIP(string controlerSN, string ip, string gateway, string netmask)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn_ptr = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr ip_ptr = Marshal.StringToHGlobalAnsi(ip);
            IntPtr gateway_ptr = Marshal.StringToHGlobalAnsi(gateway);
            IntPtr netmask_ptr = Marshal.StringToHGlobalAnsi(netmask);

            do
            {
                ret = LCFAPI_ForceIP(sn_ptr, ip_ptr, gateway_ptr, netmask_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            }
            while (false);

            Marshal.FreeHGlobal(sn_ptr);
            Marshal.FreeHGlobal(ip_ptr);
            Marshal.FreeHGlobal(gateway_ptr);
            Marshal.FreeHGlobal(netmask_ptr);

            return (LCF_StatusTypeDef)ret;
        }

        /**
          * @brief LCF_ForceIP   永久修改控制器IP参数，修改完成后重启控制器后生效
          * @param controlerSN   通过LCF_ScanDeviceList_Detail 扫描到的控制器序列号
          * @param ip            控制器IP
          * @param gateway       控制器网关
          * @param netmask       控制器子网掩码
          * @return
          */
        public static LCF_StatusTypeDef LCF_SetEthParam(string controlerSN, string ip, string gateway, string netmask)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;
            IntPtr sn_ptr = Marshal.StringToHGlobalAnsi(controlerSN);
            IntPtr ip_ptr = Marshal.StringToHGlobalAnsi(ip);
            IntPtr gateway_ptr = Marshal.StringToHGlobalAnsi(gateway);
            IntPtr netmask_ptr = Marshal.StringToHGlobalAnsi(netmask);

            do
            {
                ret = LCFAPI_SetEthParam(sn_ptr, ip_ptr, gateway_ptr, netmask_ptr);
                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }
            }
            while (false);

            Marshal.FreeHGlobal(sn_ptr);
            Marshal.FreeHGlobal(ip_ptr);
            Marshal.FreeHGlobal(gateway_ptr);
            Marshal.FreeHGlobal(netmask_ptr);

            return (LCF_StatusTypeDef)ret;
        }

        /**
         * @brief LCF_WriteUserFlashData 用户自定义数据写入传感头内部Flash
         * @param handle        设备句柄
         * @param data          写入数据
         * @param len           数据长度,单位字节
         * @return
         */
        public static LCF_StatusTypeDef LCF_WriteUserFlashData(int handle, IntPtr data, int len)
        {
            return (LCF_StatusTypeDef)LCFAPI_WriteUserFlashData(handle, data, len);
        }

        /**
         * @brief LCF_ReadUserFlashData 读取传感头内部Flash用户写入的数据
         * @param handle         设备句柄
         * @param data           返回用户数据
         * @param len           返回数据长度,单位字节。如果Flash没被写入过数据则返回数据长度为0
         * @return
         */
        public static LCF_StatusTypeDef LCF_ReadUserFlashData(int handle, IntPtr data, out int len)
        {
            len = 0;
            return (LCF_StatusTypeDef)LCFAPI_ReadUserFlashData(handle, data, ref len);
        }


        /**
         * @brief LCF_GetMaxROIFrameRate 获取用户指定的分辨率下CMOS支持的最大帧率
         * @param handle             设备句柄
         * @param x_res              X分辨率
         * @param y_res              Y分辨率
         * @param maxFrameRate       返回最大帧率
         * @return
         */
        public static LCF_StatusTypeDef LCF_GetMaxROIFrameRate(int handle, UInt32 x_res, UInt32 y_res, out UInt32 maxFrameRate)
        {
            maxFrameRate = 0;
            return (LCF_StatusTypeDef)LCFAPI_GetMaxROIFrameRate(handle, x_res, y_res, ref maxFrameRate);
        }

        /**
         * @brief LCF_SetSignalOutput 设置单点信号数据输出
         * @param handle             设备句柄
         * @param en                 true:使能传输单点信号数据  false:关闭单点信号数据传输
         * @return 返回错误码
         */
        public static LCF_StatusTypeDef LCF_SetSignalOutput(int handle, bool en, int signalIndex)
        {
            return (LCF_StatusTypeDef)LCFAPI_SetSignalOutput(handle, en ? 1 : 0, signalIndex);
        }

        /**
         * @brief LCF_SetSignalOutput 获取单个信号数据
         * @param handle             设备句柄
         * @param signal             传入数组，将非托管内存中的数据拷贝到其中
         * @param signalLen          输出信号的整体长度
         * @return 返回错误码
        */
        public static LCF_StatusTypeDef LCF_GetLastSignal(int handle, short[] signal, out UInt32 signalLen)
        {
            int ret = (int)LCF_StatusTypeDef.LCF_Status_Succeed;

            IntPtr mem = IntPtr.Zero;
            mem = Marshal.AllocHGlobal(MAX_ROI_Y_RES * 2);
            signalLen = 0;
            do
            {
                ret = LCFAPI_GetLastSignal(handle, mem, ref signalLen);

                if (ret != (int)LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    break;
                }

                Marshal.Copy(mem, signal, 0, (int)signalLen);
            }
            while (false);

            //释放内存
            Marshal.FreeHGlobal(mem);
            return (LCF_StatusTypeDef)ret;
        }
    }
}

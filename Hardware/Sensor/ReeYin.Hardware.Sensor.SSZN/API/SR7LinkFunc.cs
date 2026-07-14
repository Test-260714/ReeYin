/**************************************************************************
 * File Name:    SR7LinkFunc.cs
 * Created Date: 2025-04-01
 * Version:      1.0.0
 * Description:  将SR7lnik sdk库转成C#可用接口
 *               Convert the SR7lnik SDK library into a C# usable interface
 * 
 * Change Log:
 * Date         Author      Description
 * ----------   ---------   ----------------------------------------------
 * 2025-04-01   GK          Add
 **************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SR7Link
{
    #region Structure
    /// <summary>
    /// IP 结构体 / IP structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SR7IF_ETHERNET_CONFIG
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] abyIpAddress;
    }

    /// <summary>
    /// 目标设置结构体 / Target setting structure
    /// </summary>
    public struct SR_TARGET_SETTING
    {
        public byte byType;           // 类型 / Type
        public byte byCategory;       // 类别 / Category
        public byte byItem;           // 项目 / Item
        public byte reserve;          // 保留 / Reserved
        public byte byTarget1;        // 目标1 / Target 1
        public byte byTarget2;        // 目标2 / Target 2
        public byte byTarget3;        // 目标3 / Target 3
        public byte byTarget4;        // 目标4 / Target 4
    };

    /// <summary>
    /// 回调信息结构体 / Callback information structure
    /// </summary>
    public struct SR7IF_STR_CALLBACK_INFO
    {
        public int xPoints;           // x方向数据数量 / Number of data points in x direction
        public int BatchPoints;       // 批处理数量 / Batch quantity
        public int BatchTimes;        // 批处理次数 / Number of batches
        public double xPixth;         // x方向点间距 / Point spacing in x direction
        public int startEncoder;      // 批处理开始编码器值 / Batch start encoder value
        public int HeadNumber;        // 相机头数量 / Number of camera heads
        public int returnStatus;      // 0:正常批处理 / 0: Normal batch processing
    }

    /// <summary>
    /// 异步回调结构体 / Asynchronous callback structure
    /// </summary>
    public struct AsyncCallBackInfo
    {
        public int dev;               // 本次回调控制器编号 / The controller number of this callback
        public int profileIndex;      // 本次回调起始行号 / The starting line number of this callback
        public int profileNum;        // 本次回调帧数量 / The number of frames in this callback
        public int profileWidth;      // 本次回调宽度 / The width of this callback
        public uint BatchTimes;       // 本次回调批处理序号 / The batch number of this callback
        public int bGray;             // 本次回调是否开启灰度 / Whether this callback is grayscale enabled
        public int bCamB;             // 本次回调相机b是否在线 / Whether camera B is online in this callback
        public int ProfileBits;       // 本次回调数据类型0:32bit;1:16bit / The data type of this callback is 0:32bit; 1:16bit

        public IntPtr pProfileDataA;       // 相机a数据地址 / Camera A data address
        public IntPtr pProfileDataB;       // 相机b数据地址 / Camera B data address
        public IntPtr pProfileDataA16Bit;  // 相机A16Bit数据地址 / Camera A 16Bit data address
        public IntPtr pProfileDataB16Bit;  // 相机B16Bit数据地址 / Camera B 16Bit data address
        public IntPtr pGrayDataA;          // 相机A灰度图 / Camera A grayscale image
        public IntPtr pGrayDataB;          // 相机B灰度图 / Camera B grayscale image
        public IntPtr pEncoderDataA;       // 相机A编码器数据 / Camera A encoder data
        public IntPtr pEncoderDataB;       // 相机B编码器数据 / Camera B encoder data
    };
    #endregion

    #region Method
    /// <summary>
    /// 回调函数-高速数据通信的回调函数接口 / Callback function - callback function interface for high-speed data communication
    /// </summary>
    /// <param name="buffer">指向储存概要数据的缓冲区的指针 / Pointer to a buffer to store summary data</param>
    /// <param name="size">每个单元(行)的字节数量 / The number of bytes per cell (row)</param>
    /// <param name="count">存储在pBuffer中的内存的单元数量 / The number of cells of memory stored in pBuffer</param>
    /// <param name="notify">中断或批量结束等中断的通知 / Notification of interruptions such as interruption or batch end</param>
    /// <param name="user">用户自定义信息 / User-defined information</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void HighSpeedDataCallBack(IntPtr buffer, uint size, uint count, uint notify, uint user);

    /// <summary>
    /// 异步回调回调函数 / Asynchronous callback function
    /// </summary>
    /// <param name="info">保留 / Reserved</param>
    /// <param name="SR7IF_Data">回调相关信息 / Callback related information</param>
    /// <param name="pUserData">返回用户注册传入的指针 / Returns the pointer passed by user registration</param>
    /// <param name="state">本次回调状态 / Current callback status
    ///     (0x01 << 0) : 批处理批处理正常结束 / Batch processing ended normally
    ///     (0x01 << 1) : 批处理软件溢出 / Batch software overflow
    ///     (0x01 << 2) : 批处理硬件溢出 / Batch hardware overflow
    ///     (0x01 << 3) : 网络异常，接收断开 / Network abnormality, reception disconnected
    ///     (0x01 << 4) : 其他错误 / Other errors
    /// </param>
    /// <param name="dwDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
    /// <returns>
    ///     &lt;0: 失败 / Fail
    ///     =0: 成功 / Success
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_AsyncCALLBACK(IntPtr SR7IF_Data, IntPtr pUserData, uint state, int dwDeviceId);

    /// <summary>
    /// 异步错误回调 / Asynchronous error callback
    /// </summary>
    /// <param name="DeviciId">设备ID / Device ID</param>
    /// <param name="ErrCode">错误码 / Error code</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_AsyncErrCallBack(int DeviciId, int ErrCode);

    /// <summary>
    /// 回调函数-无限循环回调函数接口 / Callback function - infinite loop callback function interface
    /// </summary>
    /// <param name="pProfileBuffer">返回轮廓数据 / Return contour data</param>
    /// <param name="pIntensityBuffer">返回灰度数据 / Return grayscale data</param>
    /// <param name="pEncoder">返回编码器数据 / Return encoder data</param>
    /// <param name="dwSize">数据宽度 / Data width</param>
    /// <param name="dwCount">批处理点数 / Batch points</param>
    /// <param name="dwRet">返回值，待使用 / Return value, to be used</param>
    /// <param name="dwDeviceId">相应的控制器(0-63) / Corresponding controller (0-63)</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_RollDataCallback(IntPtr pProfileBuffer, IntPtr pIntensityBuffer, IntPtr pEncoder, int dwSize, int dwCount, int dwRet, int dwDeviceId);

    /// <summary>
    /// 高速数据通信的回调函数接口 / Callback function interface for high-speed data communication
    /// </summary>
    /// <param name="data">指向储存概要数据的缓冲区的指针 / Pointer to the buffer where the summary data is stored</param>
    /// <param name="ProfileCompressType">Ethernet 压缩类型0:32bit,1:16bit / Ethernet compression type 0:32bit, 1:16bit</param>
    /// <param name="xPoints">一行轮廓的点数 / The number of points in a line of contours</param>
    /// <param name="dwCount">当前回调返回行数 / The current callback returns the number of rows</param>
    /// <param name="dwNotify">回调状态0：回调中，1：回调结束 / Callback status 0: callback in progress, 1: callback completed</param>
    /// <param name="dwDeviceId">被调用函数DeviceID号 / DeviceID number of the called function</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_ProfileCALLBACK(IntPtr data, byte ProfileCompressType, int xPoints, int dwCount, int dwNotify, int dwDeviceId);

    /// <summary>
    /// 批处理一次性回调 / Batch one-time callback
    /// </summary>
    /// <param name="info">信息 / Information</param>
    /// <param name="DataObj">数据对象 / Data object</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_BatchOneTimeCallBack(IntPtr info, IntPtr DataObj);

    /// <summary>
    /// 相机异常回调 / Camera exception callback
    /// </summary>
    /// <param name="dwDeviceId">设备ID / Device ID</param>
    /// <param name="cmd">命令 / Command</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ErrConnectCallBack(int dwDeviceId, int cmd);

    /// <summary>
    /// Function definitions 接口函数定义 / Interface function definition
    /// </summary>
    internal class SR7LinkFunc
    {
        internal static UInt32 ProgramSettingSize
        {
            get { return 10932; }
        }

        /// <summary>
        /// 连接相机 / Connect camera
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="pEthernetConfig">（网口）通信设定 / (Network port) Communication settings</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_EthernetOpen(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig);

        /// <summary>
        /// 连接相机ext / Connect camera ext
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="pEthernetConfig">（网口）通信设定 / (Network port) Communication settings</param>
        /// <param name="Timeout">超时时间，默认为2000ms / Timeout, default is 2000ms</param>
        /// <param name="fun">TCP连接函数 / TCP connection function</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_EthernetOpenExt(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig, int Timeout = 2000, ErrConnectCallBack fun = null);

        /// <summary>
        /// 断开相机连接 / Disconnect the camera
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_CommClose(int lDeviceId);

        /// <summary>
        /// 开始批处理，立即执行批处理程序 / Start batch processing and execute the batch program immediately
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="Timeout">非循环获取时，超时时间(单位ms); 循环模式该参数可设置为-1 / Timeout in milliseconds for non-loop mode; set to -1 for loop mode</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartMeasure(int lDeviceId, int Timeout);

        /// <summary>
        /// 开始批处理，硬件IO触发开始批处理，具体查看硬件手册 / Start batch processing, triggered by hardware IO. Refer to the hardware manual for details.
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="Timeout">非循环获取时，超时时间(单位ms); 循环模式该参数可设置为-1 / Timeout in milliseconds for non-loop mode; set to -1 for loop mode</param>
        /// <param name="restart">预留，设为0 / Reserved, set to 0</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartIOTriggerMeasure(int lDeviceId, int Timeout, int restart);

        /// <summary>
        /// 停止批处理---停止扫描 / Stop batch processing - stop scanning
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StopMeasure(int lDeviceId);

        /// <summary>
        /// 阻塞方式获取数据---等待数据接收完成 / Get data in blocking mode - wait until data reception is complete
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="DataObj">返回数据指针 / Pointer to the returned data</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ReceiveData(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取当前编码器值 / Get the current encoder value
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="DataObj">返回数据指针 / Pointer to the returned data</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetCurrentEncoder(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取当前批处理设定行数 / Get the number of preset lines for batch processing
        /// </summary>
        /// <param name="lDeviceId">设备号0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="DataObj">预留设置为null / Reserved, set to null</param>
        /// <returns>
        ///     返回实际批处理行数 / Returns the actual number of batch processing lines
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfilePointSetCount(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取批处理实际获取行数 / Get the actual number of lines obtained in batch processing
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <returns>
        ///     返回实际批处理行数 / Returns the actual number of batch processing lines
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfilePointCount(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取数据宽度 / Get the data width
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID, ranging from 0 to 63</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <returns>
        ///     返回数据宽度(单位像素) / Returns the data width (in pixels)
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfileDataWidth(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 本次上电后控制器运行的时间 / The running time of the controller since the last power-on
        /// </summary>
        /// <param name="lDeviceId">设备ID号,范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="DataObj">预留,设置为NULL / Reserved, set to NULL</param>
        /// <param name="TimeStamp">返回运行的时间(单位秒) / Returns the running time (in seconds)</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetTimeStamp(int lDeviceId, IntPtr DataObj, int TimeStamp);

        /// <summary>
        /// 批处理状态查询 / Batch processing status query
        /// </summary>
        /// <param name="lDeviceId">设备ID号,范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="Status">批处理状态 1：正在批处理 0：批处理结束 / Batch status 1: batch processing in progress 0: batch processing ended</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchStatus(int lDeviceId, int Status);

        /// <summary>
        /// 设置控制器网络参数 / Set controller network parameters
        /// </summary>
        /// <param name="lDeviceId">设备ID号,范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="ip">IP地址 / IP address</param>
        /// <param name="netmask">子网掩码 / Subnet mask</param>
        /// <param name="gateway">默认网关 / Default gateway</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetNetworkParam(int lDeviceId, IntPtr ip, IntPtr netmask, IntPtr gateway);

        /// <summary>
        /// 续传功能使能 / Resume function enable
        /// 设置相机在一次批处理过程中，可以通过IO控制来暂停或继续批处理 / Set the camera to pause or resume batch processing through IO control during a batch process
        /// IO控制使用控制器11脚和14脚配合使用，其中11脚开启电平控制模式，14脚控制暂停和继续批处理 / IO control uses pin 11 and pin 14 of the controller, where pin 11 enables level control mode and pin 14 controls pause and resume of batch processing
        /// </summary>
        /// <param name="lDeviceId">设备ID号,范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="DataObj">预留,设置为NULL / Reserved, set to NULL</param>
        /// <param name="Enable">功能使能0：关闭 1:使能 / Function enable 0: disable 1: enable</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchCtrlByIO(int lDeviceId, IntPtr DataObj, int Enable);

        /// <summary>
        /// 获取z向测量范围上下限 / Get the upper and lower limits of the z-direction measurement range
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="up">上限（mm） / Upper limit (mm)</param>
        /// <param name="down">下限（mm） / Lower limit (mm)</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetMeasuringRangeZ(int lDeviceId, double up, double down);

        /// <summary>
        /// 获取编码器值64bit / Get 64-bit encoder value
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Encoder">返回数据指针 / Return data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchEncoder64Bit(int lDeviceId, IntPtr DataObj, IntPtr Encoder);

        /// <summary>
        /// 获取激光器宽度数据(仅支持SRI设备) / Get laser width data (only supports SRI devices)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="widthData">返回数据指针,双相机为A/B行交替数据 / Return data pointer, dual cameras are A/B row alternate data</param>
        /// <param name="GetCnt">获取数据长度 / Get data length</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetLaserWidthData(int lDeviceId, IntPtr DataObj, IntPtr widthData, int GetCnt = 0);

        /// <summary>
        /// 设置有效数据范围（获取数据模式:批处理一次回调一次） / Set valid data range (data acquisition mode: one callback per batch)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="Left">左侧裁剪数量 / Number of left crops</param>
        /// <param name="Right">右侧裁剪数量 / Number of right crops</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchOneTimeDataValidRange(int lDeviceId, int Left, int Right);

        /// <summary>
        /// 获取数据x方向间距 / Get data x-direction spacing
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <returns>
        ///     返回数据x方向间距(mm) / Returns the x-direction spacing of the data (mm)
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern double SR7IF_ProfileData_XPitch(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取编码器值 / Get encoder value
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Encoder">返回数据指针 / Return data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetEncoder(int lDeviceId, IntPtr DataObj, IntPtr Encoder);

        /// <summary>
        /// 非阻塞方式获取编码器值 / Get encoder value in non-blocking mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Encoder">返回数据指针 / Return data pointer</param>
        /// <param name="GetCnt">获取数据长度 / Get data length</param>
        /// <returns>
        ///     >=0: 实际返回的数据长度 小于0: 获取失败 / >=0: actual returned data length less than 0: failed to get
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetEncoderContiune(int lDeviceId, IntPtr DataObj, IntPtr Encoder, int GetCnt);

        /// <summary>
        /// 阻塞方式获取轮廓数据 / Get profile data in blocking mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Profile">返回数据指针 / Return data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfileData(int lDeviceId, IntPtr DataObj, IntPtr Profile);

        /// <summary>
        /// 非阻塞方式获取轮廓数据 / Get profile data in non-blocking mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Profile">返回数据指针 / Return data pointer</param>
        /// <param name="GetCnt">获取数据长度 / Get data length</param>
        /// <returns>
        ///     >=0: 实际返回的数据长度 小于0: 获取失败 / >=0: actual returned data length less than 0: failed to get
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfileContiuneData(int lDeviceId, IntPtr DataObj, IntPtr Profile, int GetCnt);

        /// <summary>
        /// 无终止循环获取数据 / Get data in infinite loop mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Profile">返回轮廓数据指针 / Return profile data pointer</param>
        /// <param name="Intensity">返回亮度数据指针 / Return intensity data pointer</param>
        /// <param name="Encoder">返回编码器数据指针 / Return encoder data pointer</param>
        /// <param name="FrameId">返回帧编号数据指针 / Return frame ID data pointer</param>
        /// <param name="FrameLoss">返回批处理过快掉帧数量数据指针 / Return the number of frames lost due to too fast batch processing</param>
        /// <param name="GetCnt">获取数据长度 / Get data length</param>
        /// <returns>
        ///     >=0: 实际返回的数据长度 小于0: 获取失败 / >=0: actual returned data length less than 0: failed to get
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchRollData(int lDeviceId, IntPtr DataObj, IntPtr Profile, IntPtr Intensity, IntPtr Encoder, IntPtr FrameId, IntPtr FrameLoss, int GetCnt);

        /// <summary>
        /// 无终止循环设定终止行数（该设置断电不保存） / Set the termination line number for infinite loop (this setting is not saved after power off)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="points">设定行数，范围（0：无终止循环。≥15000：设定终止行数，其他无效） / Set the number of lines, range (0: infinite loop. ≥15000: set termination line number, others invalid)</param>
        /// <returns>
        ///     =0: 设置成功 小于0: 设置失败 / =0: setting successful less than 0: setting failed
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchRollProfilePoint(int lDeviceId, IntPtr DataObj, uint points);

        /// <summary>
        /// 无终止循环回调方式 / Infinite loop callback mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="pCallBack">回调函数 / Callback function</param>
        /// <param name="dwProfileCnt">单次回调行数：范围0-15000 / Number of lines per callback: range 0-15000</param>
        /// <param name="Timeout">获取档次回调行数超时时间，单位ms <0:关闭超时 / Timeout for getting callback lines, in ms <0: disable timeout</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_RollDataCallbackInitalize(int lDeviceId, SR7IF_RollDataCallback pCallBack, int dwProfileCnt, int Timeout);

        /// <summary>
        /// 阻塞方式获取亮度数据 / Get intensity data in blocking mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Intensity">返回数据指针 / Return data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetIntensityData(int lDeviceId, IntPtr DataObj, IntPtr Intensity);

        /// <summary>
        /// 非阻塞获取亮度数据 / Get intensity data in non-blocking mode
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="DataObj">预留，设置为NULL / Reserved, set to NULL</param>
        /// <param name="Intensity">返回数据指针 / Return data pointer</param>
        /// <param name="GetCnt">获取数据长度 / Get data length</param>
        /// <returns>
        ///     >=0: 实际返回的数据长度 小于0: 获取失败 / >=0: actual returned data length less than 0: failed to get
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetIntensityContiuneData(int lDeviceId, IntPtr DataObj, IntPtr Intensity, int GetCnt);

        /// <summary>
        /// 无终止循环获取数据异常计算值 / Get abnormal calculation value of infinite loop data
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="EthErrCnt">返回网络传输导致错误的数量 / Returns the number of errors caused by network transmission</param>
        /// <param name="UserErrCnt">返回用户获取导致错误的数量 / Returns the number of errors caused by user acquisition</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchRollError(int lDeviceId, IntPtr EthErrCnt, IntPtr UserErrCnt);

        /// <summary>
        /// 获取系统错误信息 / Get system error information
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="EthErrCnt">返回错误码数量 / Returns the number of error codes</param>
        /// <param name="UserErrCnt">返回错误码指针 / Returns the error code pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetError(int lDeviceId, IntPtr pbyErrCnt, IntPtr pwErrCode);

        /// <summary>
        /// 初始化以太网高速数据通信 / Initialize Ethernet high-speed data communication
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="pEthernetConfig">Ethernet 通信设定 / Ethernet communication settings</param>
        /// <param name="wHighSpeedPortNo">Ethernet 通信端口设定 / Ethernet communication port settings</param>
        /// <param name="pCallBack">高速通信中数据接收的回调函数 / Callback function for data reception in high-speed communication</param>
        /// <param name="dwProfileCnt">回调函数被调用的频率. 范围1-256 / Frequency at which the callback function is called. Range 1-256</param>
        /// <param name="dwThreadId">线程号 / Thread number</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_HighSpeedDataEthernetCommunicationInitalize(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig, int wHighSpeedPortNo,
            HighSpeedDataCallBack pCallBack, uint dwProfileCnt, uint dwThreadId);

        /// <summary>
        /// 初始化以太网高速数据通信 / Initialize Ethernet high-speed data communication
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="pEthernetConfig">Ethernet 通信设定 / Ethernet communication settings</param>
        /// <param name="pCallBack">回调函数 / Callback function</param>
        /// <param name="dwProfileCnt">范围1-15000 / Range 1-15000</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_HighSpeedDataCallBackInitalize(int lDeviceId, [MarshalAs(UnmanagedType.LPStr)] string pEthernetConfig, SR7IF_ProfileCALLBACK pCallBack, int dwProfileCnt);

        /// <summary>
        /// 高速回调开始批处理 / High-speed callback start batch processing
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="ImmediateBatch">0:立即开始批处理 非0:等待外部开始批处理 / 0: start batch immediately non-zero: wait for external start batch</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartMeasureWithHighSpeedCallback(int lDeviceId, int ImmediateBatch);

        /// <summary>
        /// 高速回调停止批处理 / High-speed callback stop batch processing
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="instantStop">0:数据传输完成 1：立即停止，抛弃剩余未传完数据 / 0: data transmission completed 1: stop immediately, discard remaining untransmitted data</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StopMeasureWithHighSpeedCallback(int lDeviceId, int instantStop);

        /// <summary>
        /// 高速回调获取32位高度数据 / High-speed callback get 32-bit height data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <param name="Profile">接收数据指针,单点数据大小为4Bytes / Receive data pointer, single point data size is 4Bytes</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfilePointData32bit(IntPtr DataObj, int head, int[] Profile);

        /// <summary>
        /// 高速回调获取16位高度数据 / High-speed callback get 16-bit height data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <param name="Profile">接收数据指针,单点数据大小为2Bytes / Receive data pointer, single point data size is 2Bytes</param>
        /// <param name="Scale">压缩比例 / Compression ratio</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfilePointData16bit(IntPtr DataObj, int head, IntPtr Profile, double Scale);

        /// <summary>
        /// 高速回调获取灰度数据 / High-speed callback get grayscale data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <param name="Profile">接收数据指针 / Receive data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetHighSpeedIntensityData(IntPtr DataObj, int head, byte[] Intensity);

        /// <summary>
        /// 高速回调获取16位灰度图 / High-speed callback get 16-bit grayscale image
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <param name="Profile">接收数据指针,单点数据大小为2Bytes / Receive data pointer, single point data size is 2Bytes</param>
        /// <param name="Scale">压缩比例 / Compression ratio</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetGrayData16Bit(IntPtr DataObj, int head, IntPtr Profile, double Scale);

        /// <summary>
        /// 高速回调获取编码器数据 / High-speed callback get encoder data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="pEncoder">接收数据指针 / Receive data pointer</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetHighSpeedEncoderContiune(IntPtr DataObj, int[] pEncoder);

        /// <summary>
        /// 获取库版本号 / Get library version number
        /// </summary>
        /// <returns>
        ///     返回版本信息 / Returns version information
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetVersion();

        /// <summary>
        /// 获取相机型号 / Get camera model
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetModels(int lDeviceId);

        /// <summary>
        /// 获取相机头序列号 / Get camera head serial number
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Head">0:相机头A 1:相机头B / 0: camera head A 1: camera head B</param>
        /// <returns>
        ///     成功:返回相机头序列号,失败：相机头不存在或参数错误 / Success: returns camera head serial number, failure: camera head does not exist or parameter error
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetHeaderSerial(int lDeviceId, int Head);

        /// <summary>
        /// 获取传感头B是否在线 / Get whether sensor head B is online
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <returns>
        ///     0：在线; 小于0：不在线 / 0: online; less than 0: offline
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetOnlineCameraB(int lDeviceId);

        /// <summary>
        /// 切换相机配置的参数 / Switch camera configuration parameters
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="No">任务参数列表编号 0 - 63 / Task parameter list number 0 - 63</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SwitchProgram(int lDeviceId, int No);

        /// <summary>
        /// 设置输出端口电平 / Set output port level
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Port">输出端口号，范围为0-7 / Output port number, ranging from 0 to 7</param>
        /// <param name="Level">输出电平值 / Output level value</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetOutputPortLevel(uint lDeviceId, uint Port, bool Level);

        /// <summary>
        /// 读取输入端口电平 / Read input port level
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Port">输入端口号，范围为0-7 / Input port number, ranging from 0 to 7</param>
        /// <param name="Level">读取输入电平 / Read input level</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetInputPortLevel(uint lDeviceId, uint Port, IntPtr Level);

        /// <summary>
        /// 参数设定 / Parameter setting
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Depth">设置的值的级别 / Level of the value to be set</param>
        /// <param name="Type">设置类型 / Setting type</param>
        /// <param name="Category">设置种类 / Setting category</param>
        /// <param name="Item">设置项目 / Setting item</param>
        /// <param name="Target">根据发送/接收的设定，可能需要进行相应的指定。无需设定时，指定为0 / Depending on the send/receive settings, it may need to be specified accordingly. When no setting is needed, specify as 0</param>
        /// <param name="pData">设置数据 / Setting data</param>
        /// <param name="DataSize">设置数据的长度 / Length of setting data</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetSetting(uint lDeviceId, int Depth, int Type, int Category, int Item, int[] Target, IntPtr pData, int DataSize);

        /// <summary>
        /// 参数获取 / Parameter acquisition
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Type">设置类型 / Setting type</param>
        /// <param name="Category">设置种类 / Setting category</param>
        /// <param name="Item">设置项目 / Setting item</param>
        /// <param name="Target">根据发送/接收的设定，可能需要进行相应的指定。无需设定时，指定为0 / Depending on the send/receive settings, it may need to be specified accordingly. When no setting is needed, specify as 0</param>
        /// <param name="pData">设置数据 / Setting data</param>
        /// <param name="DataSize">设置数据的长度 / Length of setting data</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetSetting(uint lDeviceId, int Type, int Category, int Item, int[] Target, IntPtr pData, int DataSize);

        /// <summary>
        /// 获取当前一条轮廓（非批处理下） / Get current single profile (not in batch mode)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="pProfileData">返回轮廓的指针 / Pointer to return profile</param>
        /// <param name="pEncoder">返回编码器的指针 / Pointer to return encoder</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetSingleProfile(uint lDeviceId, [Out] int[] pProfileData, [Out] uint[] pEncoder);
		///
		/// \brief SR7IF_GetSingleProfile3DMode     获取一条轮廓数据（非批处理下 3D模式）
		/// \param lDeviceId                        设备ID号，范围为0-63.
		/// \param pProfileData                     返回轮廓的指针.
		/// \return
		///     <0:                                 失败.
		///     =0:                                 成功.
		///
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetSingleProfile3DMode(uint lDeviceId, [Out] int[] pProfileData);

        /// <summary>
        /// 将导出的参数导入到系统中 / Import exported parameters into the system
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="pSettingdata">导入参数表指针 / Pointer to import parameter table</param>
        /// <param name="size">导入参数表的大小 / Size of import parameter table</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_LoadParameters(uint lDeviceId, IntPtr pSettingdata, UInt32 size);

        /// <summary>
        /// 返回产品剩余天数 / Return product remaining days
        /// </summary>
        /// <param name="lDeviceId">设备ID号 / Device ID number</param>
        /// <param name="RemainDay">返回剩余天数 / Returns remaining days</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetLicenseKey(uint lDeviceId, IntPtr RemainDay);

        /// <summary>
        /// 将系统参数导出，注意只导出当前任务的参数 / Export system parameters, note that only the parameters of the current task are exported
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="size">返回参数表的大小 / Returns the size of the parameter table</param>
        /// <returns>
        ///     NULL:失败. 其他:成功 / NULL: failure. Others: success
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_ExportParameters(int lDeviceId, IntPtr size);

        /// <summary>
        /// 3D显示 / 3D display
        /// </summary>
        /// <param name="_BatchData">批处理数据 / Batch data</param>
        /// <param name="x_true_step">x方向间矩/mm / x-direction spacing/mm</param>
        /// <param name="y_true_step">y方向间距/mm / y-direction spacing/mm</param>
        /// <param name="x_Point_num">x方向数据个数 / Number of data points in x direction</param>
        /// <param name="y_batchPoint_num">批处理行数 / Number of batch lines</param>
        /// <param name="z_scale">z方向缩放系数 / z-direction scaling factor</param>
        /// <param name="Ho">z方向最大值 / Maximum value in z direction</param>
        /// <param name="Lo">z方向最小值 / Minimum value in z direction</param>
        [DllImport("SR3dexe.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SR_3D_EXE_Show(IntPtr _BatchData,
                                double x_true_step,
                                double y_true_step,
                                int x_Point_num,
                                int y_batchPoint_num,
                                double z_scale,
                                double Ho,
                                double Lo
                                );

        /// <summary>
        /// 设置回调函数,建议获取数据后另外开启线程进行处理(批处理一次回调一次） / Set callback function, it is recommended to open another thread for processing after obtaining data (one callback per batch)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="size">回调函数 / Callback function</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchOneTimeDataHandler(int lDeviceId, SR7IF_BatchOneTimeCallBack CallFunc);

        /// <summary>
        /// 开始批处理 （批处理一次回调一次) / Start batch processing (one callback per batch)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="ImmediateBatch">0:立即开始批处理 1:等待外部开始批处理 / 0: start batch immediately 1: wait for external start batch</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartMeasureWithCallback(int lDeviceId, int ImmediateBatch);

        /// <summary>
        /// 批处理软件触发开始（批处理一次回调一次） / Batch software trigger start (one callback per batch)
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <returns>
        ///     0：成功; 小于0：失败 / 0: success; less than 0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_TriggerOneBatch(int lDeviceId);

        /// <summary>
        /// 批处理轮廓获取（批处理一次回调一次） / Batch profile acquisition (one callback per batch)
        /// </summary>
        /// <param name="DataObj">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Head">0：相机头A 1：相机头B / 0: camera head A 1: camera head B</param>
        /// <returns>
        ///     返回数据指针 null:无数据或相应头不存在 / Returns data pointer null: no data or corresponding head does not exist
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchProfilePoint(IntPtr DataObj, int Head);

        /// <summary>
        /// 批处理亮度获取（批处理一次回调一次） / Batch intensity acquisition (one callback per batch)
        /// </summary>
        /// <param name="DataObj">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Head">0：相机头A 1：相机头B / 0: camera head A 1: camera head B</param>
        /// <returns>
        ///     返回数据指针 null:无数据或相应头不存在 / Returns data pointer null: no data or corresponding head does not exist
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchIntensityPoint(IntPtr DataObj, int Head);

        /// <summary>
        /// 批处理编码器获取（批处理一次回调一次） / Batch encoder acquisition (one callback per batch)
        /// </summary>
        /// <param name="DataObj">设备ID号，范围为0-3 / Device ID number, ranging from 0 to 3</param>
        /// <param name="Head">0：相机头A 1：相机头B / 0: camera head A 1: camera head B</param>
        /// <returns>
        ///     返回数据指针 null:无数据或相应头不存在 / Returns data pointer null: no data or corresponding head does not exist
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchEncoderPoint(IntPtr DataObj, int Head);

        /// <summary>
        /// 查找在线设备 / Search for online devices
        /// </summary>
        /// <param name="ReadNum">搜索到的设备个数 / Number of devices found</param>
        /// <param name="timeOut">搜索超时时间 / Search timeout</param>
        /// <returns>
        ///     返回已搜索到的设备的 IP 地址指针 / Returns the IP address pointer of the found devices
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_SearchOnline(IntPtr ReadNum, int timeOut);

        /// <summary>
        /// 异步回调连接函数 / Asynchronous callback connection function
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="pEthernetConfig">Ethernet 通信设定 / Ethernet communication settings</param>
        /// <param name="timeOut">搜索时间(ms),最小值100 / Search time (ms), minimum 100</param>
        /// <param name="fun">掉线回调函数 / Disconnection callback function</param>
        /// <returns>
        ///     &lt;0: 失败 / Fail
        ///     =0: 成功 / Success
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_AsyncEthernetOpen(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig, int timeOut = 2000, SR7IF_AsyncErrCallBack errCallBackFunc = null);

        /// <summary>
        /// 初始化异步回调数据通信 / Initialize asynchronous callback data communication
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="pCallBack">回调函数 / Callback function</param>
        /// <param name="pUserData">用户注册传入指针 / Pointer passed by user registration</param>
        /// <param name="ProfileBits">0：返回32位高度数据。1：返回16位高度数据 / 0: return 32-bit height data. 1: return 16-bit height data</param>
        /// <param name="mMaxLine">指定行数返回数据 / Specify the number of lines to return data</param>
        /// <returns>
        ///     &lt;0: 失败 / Fail
        ///     =0: 成功 / Success
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_AsyncCallBackInitalize(int lDeviceId, SR7IF_AsyncCALLBACK pCallBack, IntPtr pUserData, uint ProfileBits = 0, uint mMaxLine = 0xFFFFFFFF);

        /// <summary>
        /// 异步回调软触发 / Asynchronous callback soft trigger
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <returns>
        ///     &lt;0: 失败 / Fail
        ///     =0: 成功 / Success
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_AsyncSoftStartBatch(int lDeviceId);

        /// <summary>
        /// 异步回调停止批处理 / Asynchronous callback stop batch processing
        /// </summary>
        /// <param name="lDeviceId">设备ID号，范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <returns>
        ///     &lt;0: 失败 / Fail
        ///     =0: 成功 / Success
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_AsyncSoftStopBatch(int lDeviceId);

        /// <summary>
        /// 异步回调获取16位高度数据 / Asynchronous callback get 16-bit height data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <returns>
        ///     数据指针 / Data pointer
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetAsyncProfilePointData16Bit(IntPtr DataObj, int Head);

        /// <summary>
        /// 异步回调获取32位高度数据 / Asynchronous callback get 32-bit height data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <returns>
        ///     数据指针 / Data pointer
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetAsyncProfilePointData(IntPtr DataObj, int Head);

        /// <summary>
        /// 异步回调获取灰度数据 / Asynchronous callback get grayscale data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <returns>
        ///     数据指针 / Data pointer
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetAsyncIntensityContiuneData(IntPtr DataObj, int Head);

        /// <summary>
        /// 异步回调获取编码器数据 / Asynchronous callback get encoder data
        /// </summary>
        /// <param name="DataObj">回调数据指针 / Callback data pointer</param>
        /// <param name="head">0:相机A 1：相机B / 0: camera A 1: camera B</param>
        /// <returns>
        ///     数据指针 / Data pointer
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetAsyncEncoderContiune(IntPtr DataObj, int Head);

        /// <summary>
        /// 获取16bit高度数据物理单位 / Get 16bit height data physical unit
        /// </summary>
        /// <param name="lDeviceId">设备ID号,范围为0-63 / Device ID number, ranging from 0 to 63</param>
        /// <param name="Scale">单位(mm) / Unit (mm)</param>
        /// <returns>
        ///     0:成功,&lt;0: 失败 / 0: success, &lt;0: failure
        /// </returns>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_Get16BitScale(int lDeviceId, IntPtr Scale);

        internal static int SR7IF_CommClose(uint controllerID)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

}
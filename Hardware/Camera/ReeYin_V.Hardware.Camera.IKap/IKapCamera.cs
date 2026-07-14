using HalconDotNet;
using IKapBoardDotNet;
using IKapCDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.Memory;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Image;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ReeYin_V.Hardware.Camera.IKap
{
    /// <summary>
    /// 图片基类
    /// </summary>
    public abstract class ImageBase
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public int nWidth {  get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public int nHeight { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        public int nType { get; set; }


    }

    public partial class IKapCamera : CameraBase
    {
        [DllImport("kernel32.dll")]
        public static extern void OutputDebugString(string lpOutputString);
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        delegate void IKapCCallBackDelegate(uint eventType, IntPtr pContext);
        private IKapCCallBackDelegate IKapC_OnGrabStartDelegate;
        private IKapCCallBackDelegate IKapC_OnFrameLostDelegate;
        private IKapCCallBackDelegate IKapC_OnTimeoutDelegate;
        private IKapCCallBackDelegate IKapC_OnFrameReadyDelegate;
        private IKapCCallBackDelegate IKapC_OnGrabStopDelegate;

        delegate void IKapBoardCallBackDelegate(IntPtr pParam);
        private IKapBoardCallBackDelegate IKapB_OnGrabStartDelegate;
        private IKapBoardCallBackDelegate IKapB_OnFrameLostDelegate;
        private IKapBoardCallBackDelegate IKapB_OnTimeoutDelegate;
        private IKapBoardCallBackDelegate IKapB_OnFrameReadyDelegate;
        private IKapBoardCallBackDelegate IKapB_OnGrabStopDelegate;

        // 相机句柄
        [NonSerialized]
        [JsonIgnore]
        public ITKDEVICE m_hDev = new ITKDEVICE();
        // 采集流句柄
        [NonSerialized]
        [JsonIgnore]
        public ITKSTREAM m_hStream = new ITKSTREAM();
        // 缓冲区列表
        [NonSerialized]
        [JsonIgnore]
        public List<ITKBUFFER> m_listBuffer = new List<ITKBUFFER>();
        // 采集卡句柄
        [NonSerialized]
        [JsonIgnore]
        public IntPtr m_pBoard = new IntPtr(-1);
        // 相机缓冲区个数
        [NonSerialized]
        [JsonIgnore]
        public uint m_nFrameCount = 20;
        // 用户缓冲区，用于图像数据转换
        [NonSerialized]
        [JsonIgnore]
        public IntPtr m_pUserBuffer = new IntPtr(-1);
        // 是否正在采集
        [NonSerialized]
        [JsonIgnore]
        public volatile bool m_bGrabingImage = false;
        // 是否已更新用户缓冲区
        [NonSerialized]
        [JsonIgnore]
        public volatile bool m_bUpdateImage = false;
        // 相机类型，0为GV/XGV相机+网卡，1为CL相机，2为CXP相机，3为GV/XGV相机+采集卡,4为U3V设备
        [NonSerialized]
        [JsonIgnore]
        public int m_nType = -1;
        // 图像宽度
        [NonSerialized]
        [JsonIgnore]
        public int m_nWidth = -1;
        // 图像高度
        [NonSerialized]
        [JsonIgnore]
        public int m_nHeight = -1;
        // 通道位深
        [NonSerialized]
        [JsonIgnore]
        public int m_nChannelDepth = 8;
        // 图像通道数
        [NonSerialized]
        [JsonIgnore]
        public uint m_nChannels = 1;
        // 像素深度
        [NonSerialized]
        [JsonIgnore]
        public int m_nPixelDepth = 8;
        // BayerPattern: 0:非bayer格式; 1:BGGR; 2:RGGB; 3:GBRG; 4:GRBG
        [NonSerialized]
        [JsonIgnore]
        public int m_nBayerPattern = 0;
        // 相机名
        [NonSerialized]
        [JsonIgnore]
        public string m_devName;
        // 相机索引
        [NonSerialized]
        [JsonIgnore]
        public int m_nDevIndex = -1;
        // 采集卡索引
        [NonSerialized]
        [JsonIgnore]
        public int m_nBoardIndex = -1;
        // 当前帧索引
        [NonSerialized]
        [JsonIgnore]
        public uint m_nCurFrameIndex = 0;
        // 相机缓冲区大小
        [NonSerialized]
        [JsonIgnore]
        public int m_nBufferSize = 0;
        // 用户缓冲区锁
        [NonSerialized]
        [JsonIgnore]
        public readonly object m_mutexImage = new object();
        // 相机像素格式
        [NonSerialized]
        [JsonIgnore]
        public uint m_uCameraPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_RGB888;
        //设备是否打开
        [NonSerialized]
        [JsonIgnore]
        public bool m_bIsOpened = false;
        ///<summary>设备类型：面阵、线阵</summary>
        ///<remarks>TODO:目前Demo只考虑面阵相机处理</remarks>
        [NonSerialized]
        [JsonIgnore]
        public bool m_bIsAreaType = true;

        /// \~chinese 相机设备信息						\~english Camera device info
        [NonSerialized]
        [JsonIgnore]
        public ITKDEV_INFO g_devInfo;

        /// \~chinese 是否开启软触发						\~english Whether enable softTrigger or not
        [NonSerialized]
        [JsonIgnore]
        public bool g_bSoftTriggerUsed = false;

        /// \~chinese 是否加载采集卡配置文件				\~english Whether load grabber configure file or not
        [NonSerialized]
        [JsonIgnore]
        public bool g_bLoadGrabberConfig = false;

        /// \~chinese 图像缓冲区申请的帧数				\~english The number of frames requested by buffer
        [NonSerialized]
        [JsonIgnore]
        public uint g_bufferCount = 5;

        /// \~chinese 要打开相机的序列号					\~english SerialNumber of camera to open
        [NonSerialized]
        [JsonIgnore]
        public string g_SerialNumber;

        /// \~chinese 相机序号						\~english Index of Camera
        [NonSerialized]
        [JsonIgnore]
        public int g_index = 0;

        private void ResetRuntimeState()
        {
            m_hDev = new ITKDEVICE();
            m_hStream = new ITKSTREAM();
            m_listBuffer = new List<ITKBUFFER>();
            m_pBoard = new IntPtr(-1);
            m_nFrameCount = 20;
            m_pUserBuffer = new IntPtr(-1);
            m_bGrabingImage = false;
            m_bUpdateImage = false;
            m_nType = -1;
            m_nWidth = -1;
            m_nHeight = -1;
            m_nChannelDepth = 8;
            m_nChannels = 1;
            m_nPixelDepth = 8;
            m_nBayerPattern = 0;
            m_devName = null;
            m_nDevIndex = -1;
            m_nBoardIndex = -1;
            m_nCurFrameIndex = 0;
            m_nBufferSize = 0;
            m_uCameraPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_RGB888;
            m_bIsOpened = false;
            m_bIsAreaType = true;
            g_devInfo = null;
            g_bSoftTriggerUsed = false;
            g_bLoadGrabberConfig = false;
            g_bufferCount = 5;
            g_SerialNumber = null;
            g_index = 0;
        }

        /*
         *@brief:根据索引开启设备
         *@param [in] nDevIndex:相机索引
         *@param [in] nBoardIndex:采集卡索引
         *@return:是否开启成功
         */
        public virtual bool openDevice(int nDevIndex, int nBoardIndex = -1)
        {
            if (isOpen())
            {
                DebugLog("Device already opened!");
                return true;
            }

            //获取设备信息
            g_devInfo = new ITKDEV_INFO();
            uint res = IKapC.ItkManGetDeviceInfo((uint)nDevIndex, g_devInfo);
            if (!CheckIKapC(res))
            {
                DebugLog("Get device info failed!");
                return false;
            }
            m_devName = g_devInfo.FullName;

            if (nBoardIndex != -1) //设备：采集卡+相机
            {
                //打开相机-215292768
                res = IKapC.ItkDevOpen((uint)nDevIndex, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref m_hDev);
                if (!CheckIKapC(res))
                {
                    DebugLog("Camera error:Open camera failed");
                    return false;
                }
                //打开采集卡
                m_pBoard = IKapBoard.IKapOpen((uint)IKapBoard.IKBoardALL, (uint)nBoardIndex);
                if (m_pBoard == new IntPtr(-1))
                    return false;

                if (g_devInfo.DeviceClass == "CameraLink")
                {
                    //CL采集卡关闭后，无法保存设置参数，所以必须需要通过配置文件方式配置参数。其他设备可选
                    // 导入配置文件
                    string configFileName = GetOption();
                    if (configFileName == null)
                    {
                        MessageBox.Show("Fail to get frameGrabber configuration file, using default setting!", "Tool Hint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        int ret = IKapBoard.IKapLoadConfigurationFromFile(m_pBoard, configFileName);
                        CheckIKapBoard(ret);
                    }
                }

                m_nBoardIndex = nBoardIndex;
            }
            else
            { //设备：相机
                //打开相机
                res = IKapC.ItkDevOpen((uint)nDevIndex, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref m_hDev);
                if (!CheckIKapC(res))
                {
                    DebugLog("Camera error:Open camera failed");
                    return false;
                }

            }
            m_nDevIndex = nDevIndex;
            m_bIsOpened = true;

            //获取设备信息
            if (m_pBoard != new IntPtr(-1)) //设备：采集卡+相机 
            {
                int ret = IKapBoard.IK_RTN_OK;
                int nImageType = 0; //图像类型
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_WIDTH, ref m_nWidth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_HEIGHT, ref m_nHeight);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_TYPE, ref nImageType);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_DATA_FORMAT, ref m_nChannelDepth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_BOARD_BIT, ref m_nPixelDepth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_BAYER_PATTERN, ref m_nBayerPattern);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_FRAME_SIZE, ref m_nBufferSize);
                if (!CheckIKapBoard(ret))
                    return false;
                switch (nImageType)
                {
                    case 0: //IKP_IMAGE_TYPE_VAL_MONOCHROME、Bayer format
                        m_nChannels = 1;
                        break;
                    case 1: //IKP_IMAGE_TYPE_VAL_COLORFUL、IKP_IMAGE_TYPE_VAL_RGB
                    case 3: //IKP_IMAGE_TYPE_VAL_BGR
                        m_nChannels = 3;
                        break;
                    case 2: //IKP_IMAGE_TYPE_VAL_RGBC
                    case 4: //IKP_IMAGE_TYPE_VAL_BGRC
                        m_nChannels = 4;
                        break;
                    case 5: //IKP_IMAGE_TYPE_VAL_YUV422、IKP_IMAGE_TYPE_VAL_YUV422_YUYV
                        DebugLog("YUV image type!");
                        m_nChannels = 2;
                        break;
                    default:
                        DebugLog("Not supported image type!");
                        return false;
                }
            }
            else
            {
                // 获取图像宽度。
                long m_nWidth_long = 0;
                res = IKapC.ItkDevGetInt64(m_hDev, "Width", ref m_nWidth_long);
                if (!CheckIKapC(res)) return false;
                m_nWidth = (int)m_nWidth_long;
                // 获取图像高度。
                long m_nHeight_long = 0;
                res = IKapC.ItkDevGetInt64(m_hDev, "Height", ref m_nHeight_long);
                if (!CheckIKapC(res)) return false;
                m_nHeight = (int)m_nHeight_long;
                // 获取像素格式。
                uint pixelFormatSize = 128;
                StringBuilder pixelFormat = new StringBuilder((int)pixelFormatSize);
                res = IKapC.ItkDevToString(m_hDev, "PixelFormat", pixelFormat, ref pixelFormatSize);
                if (!CheckIKapC(res)) return false;
                uint nFormat = IKapC.ITKBUFFER_VAL_FORMAT_MONO8;
                res = IKapC.ItkDevGetPixelFormatVal(m_hDev, pixelFormat.ToString(), ref nFormat);
                if (!CheckIKapC(res)) return false;
                //根据像素格式获取通道深度、像素深度、通道数、bayer格式信息
                m_nChannelDepth = (int)IKapCLib.ITKBUFFER_FORMAT_CHANNEL_BITS(nFormat);
                m_nPixelDepth = (int)IKapCLib.ITKBUFFER_FORMAT_PIXEL_BITS(nFormat);
                m_nChannels = IKapCLib.ITKBUFFER_FORMAT_IMAGE_CHANNELS(nFormat);
                if (pixelFormat.ToString().Contains("Bayer"))
                {
                    //BayerPattern: 0:非bayer格式; 1:BGGR; 2:RGGB; 3:GBRG; 4:GRBG
                    if (pixelFormat.ToString().Contains("BayerBG"))
                    {
                        m_nBayerPattern = 1;
                    }
                    else if (pixelFormat.ToString().Contains("BayerRG"))
                    {
                        m_nBayerPattern = 2;
                    }
                    else if (pixelFormat.ToString().Contains("BayerGB"))
                    {
                        m_nBayerPattern = 3;
                    }
                    else if (pixelFormat.ToString().Contains("BayerGR"))
                    {
                        m_nBayerPattern = 4;
                    }
                    else
                    {
                        DebugLog("The Bayer format is not supported!");
                    }
                }
                // 获取图像大小
                long payloadSize = 0;
                res = IKapC.ItkDevGetInt64(m_hDev, "PayloadSize", ref payloadSize);
                if (!CheckIKapC(res)) return false;
                m_nBufferSize = (int)payloadSize;
            }

            _frameBuffer = new UnmanagedArray2D<byte>(m_nWidth, m_nHeight);
            return true;
        }

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <returns></returns>
        public virtual bool GetDeviceInfo()
        {
            if (m_pBoard != new IntPtr(-1)) //设备：采集卡+相机 
            {
                int ret = IKapBoard.IK_RTN_OK;
                int nImageType = 0; //图像类型
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_WIDTH, ref m_nWidth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_HEIGHT, ref m_nHeight);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_IMAGE_TYPE, ref nImageType);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_DATA_FORMAT, ref m_nChannelDepth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_BOARD_BIT, ref m_nPixelDepth);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_BAYER_PATTERN, ref m_nBayerPattern);
                if (!CheckIKapBoard(ret))
                    return false;
                ret = IKapBoard.IKapGetInfo(m_pBoard, (uint)IKapBoard.IKP_FRAME_SIZE, ref m_nBufferSize);
                if (!CheckIKapBoard(ret))
                    return false;
                switch (nImageType)
                {
                    case 0: //IKP_IMAGE_TYPE_VAL_MONOCHROME、Bayer format
                        m_nChannels = 1;
                        break;
                    case 1: //IKP_IMAGE_TYPE_VAL_COLORFUL、IKP_IMAGE_TYPE_VAL_RGB
                    case 3: //IKP_IMAGE_TYPE_VAL_BGR
                        m_nChannels = 3;
                        break;
                    case 2: //IKP_IMAGE_TYPE_VAL_RGBC
                    case 4: //IKP_IMAGE_TYPE_VAL_BGRC
                        m_nChannels = 4;
                        break;
                    case 5: //IKP_IMAGE_TYPE_VAL_YUV422、IKP_IMAGE_TYPE_VAL_YUV422_YUYV
                        DebugLog("YUV image type!");
                        m_nChannels = 2;
                        break;
                    default:
                        DebugLog("Not supported image type!");
                        return false;
                }
            }
            else
            {
                // 获取图像宽度。
                long m_nWidth_long = 0;
                var res = IKapC.ItkDevGetInt64(m_hDev, "Width", ref m_nWidth_long);
                if (!CheckIKapC(res)) return false;
                m_nWidth = (int)m_nWidth_long;
                // 获取图像高度。
                long m_nHeight_long = 0;
                res = IKapC.ItkDevGetInt64(m_hDev, "Height", ref m_nHeight_long);
                if (!CheckIKapC(res)) return false;
                m_nHeight = (int)m_nHeight_long;
                // 获取像素格式。
                uint pixelFormatSize = 128;
                StringBuilder pixelFormat = new StringBuilder((int)pixelFormatSize);
                res = IKapC.ItkDevToString(m_hDev, "PixelFormat", pixelFormat, ref pixelFormatSize);
                if (!CheckIKapC(res)) return false;
                uint nFormat = IKapC.ITKBUFFER_VAL_FORMAT_MONO8;
                res = IKapC.ItkDevGetPixelFormatVal(m_hDev, pixelFormat.ToString(), ref nFormat);
                if (!CheckIKapC(res)) return false;
                //根据像素格式获取通道深度、像素深度、通道数、bayer格式信息
                m_nChannelDepth = (int)IKapCLib.ITKBUFFER_FORMAT_CHANNEL_BITS(nFormat);
                m_nPixelDepth = (int)IKapCLib.ITKBUFFER_FORMAT_PIXEL_BITS(nFormat);
                m_nChannels = IKapCLib.ITKBUFFER_FORMAT_IMAGE_CHANNELS(nFormat);
                if (pixelFormat.ToString().Contains("Bayer"))
                {
                    //BayerPattern: 0:非bayer格式; 1:BGGR; 2:RGGB; 3:GBRG; 4:GRBG
                    if (pixelFormat.ToString().Contains("BayerBG"))
                    {
                        m_nBayerPattern = 1;
                    }
                    else if (pixelFormat.ToString().Contains("BayerRG"))
                    {
                        m_nBayerPattern = 2;
                    }
                    else if (pixelFormat.ToString().Contains("BayerGB"))
                    {
                        m_nBayerPattern = 3;
                    }
                    else if (pixelFormat.ToString().Contains("BayerGR"))
                    {
                        m_nBayerPattern = 4;
                    }
                    else
                    {
                        DebugLog("The Bayer format is not supported!");
                    }
                }
                // 获取图像大小
                long payloadSize = 0;
                res = IKapC.ItkDevGetInt64(m_hDev, "PayloadSize", ref payloadSize);
                if (!CheckIKapC(res)) return false;
                m_nBufferSize = (int)payloadSize;
            }

            return true;
        }
        ///*
        // *@brief:关闭相机
        // *@param [in]:
        // *@return:是否关闭成功
        // */
        //public virtual bool closeDevice()
        //{
        //    if (isOpen())
        //    {
        //        if (m_pBoard != new IntPtr(-1))
        //        {
        //            IKapBoard.IKapClose(m_pBoard);
        //        }
        //        IKapC.ItkDevClose(m_hDev);
        //    }

        //    m_bIsOpened = false;
        //    return true;
        //}

        /*
         *@brief:查询设备连接状态
         *@param [in]:
         *@return:是否连接
         */
        public virtual bool isOpen()
        {
            return m_bIsOpened;
        }

        ///*
        // *@brief:加载采集卡配置文件
        // *@param [in] sFilePath:配置文件路径
        // *@return:是否加载成功
        // */
        //public virtual bool loadConfiguration(string sFilePath)
        //{
        //    int ret = IKapBoard.IKapLoadConfigurationFromFile(m_pBoard, sFilePath);
        //    return ret == IKapBoard.IK_RTN_OK;
        //}

        /*
         * @brief:申请缓冲区资源
         * @return:是否申请成功
         */
        public virtual bool createStream()
        {
            if (m_pBoard != new IntPtr(-1))
            {
                // 设置帧超时时间
                int timeout = -1;
                int ret = IKapBoard.IKapSetInfo(m_pBoard, (uint)IKapBoard.IKP_TIME_OUT, timeout);
                if (!CheckIKapBoard(ret))
                    return false;

                // 设置抓取模式，IKP_GRAB_NON_BLOCK为非阻塞模式
                int grab_mode = IKapBoard.IKP_GRAB_NON_BLOCK;
                ret = IKapBoard.IKapSetInfo(m_pBoard, (uint)IKapBoard.IKP_GRAB_MODE, grab_mode);
                if (!CheckIKapBoard(ret))
                    return false;

                // 设置帧传输模式，IKP_FRAME_TRANSFER_SYNCHRONOUS_NEXT_EMPTY_WITH_PROTECT为同步保存模式
                int transfer_mode = IKapBoard.IKP_FRAME_TRANSFER_SYNCHRONOUS_NEXT_EMPTY_WITH_PROTECT;
                ret = IKapBoard.IKapSetInfo(m_pBoard, (uint)IKapBoard.IKP_FRAME_TRANSFER_MODE, transfer_mode);
                if (!CheckIKapBoard(ret))
                    return false;

                // 设置缓冲区格式
                ret = IKapBoard.IKapSetInfo(m_pBoard, (uint)IKapBoard.IKP_FRAME_COUNT, (int)m_nFrameCount);
                if (!CheckIKapBoard(ret))
                    return false;

                //注册采集开始回调
                IKapB_OnGrabStartDelegate = new IKapBoardCallBackDelegate(IKapB_OnGrabStartFunc);
                ret = IKapBoard.IKapRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_GrabStart, Marshal.GetFunctionPointerForDelegate(IKapB_OnGrabStartDelegate), IntPtr.Zero);
                if (!CheckIKapBoard(ret))
                    return false;
                //注册帧结束回调
                IKapB_OnFrameReadyDelegate = new IKapBoardCallBackDelegate(IKapB_OnFrameReadyFunc);
                ret = IKapBoard.IKapRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_FrameReady, Marshal.GetFunctionPointerForDelegate(IKapB_OnFrameReadyDelegate), IntPtr.Zero);
                if (!CheckIKapBoard(ret))
                    return false;
                //注册采集丢帧回调
                IKapB_OnFrameLostDelegate = new IKapBoardCallBackDelegate(IKapB_OnFrameLostFunc);
                ret = IKapBoard.IKapRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_FrameLost, Marshal.GetFunctionPointerForDelegate(IKapB_OnFrameLostDelegate), IntPtr.Zero);
                if (!CheckIKapBoard(ret))
                    return false;
                //注册采集超时回调
                IKapB_OnTimeoutDelegate = new IKapBoardCallBackDelegate(IKapB_OnTimeoutFunc);
                ret = IKapBoard.IKapRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_TimeOut, Marshal.GetFunctionPointerForDelegate(IKapB_OnTimeoutDelegate), IntPtr.Zero);
                if (!CheckIKapBoard(ret))
                    return false;
                //注册帧结束回调
                IKapB_OnGrabStopDelegate = new IKapBoardCallBackDelegate(IKapB_OnGrabStopFunc);
                ret = IKapBoard.IKapRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_GrabStop, Marshal.GetFunctionPointerForDelegate(IKapB_OnGrabStopDelegate), IntPtr.Zero);
                if (!CheckIKapBoard(ret))
                    return false;
            }
            else
            {
                /*创建流*/
                Console.WriteLine($"正在创建采集流: m_hDev={m_hDev}, m_nFrameCount={m_nFrameCount}");
                uint res = IKapC.ItkDevAllocStreamEx(m_hDev, 0, m_nFrameCount, ref m_hStream);
                if (!CheckIKapC(res))
                {
                    Console.WriteLine($"ItkDevAllocStreamEx 失败: 0x{res:X8}");
                    return false;
                }
                Console.WriteLine($"采集流创建成功: m_hStream={m_hStream}");

                //注册采集开始回调
                IKapC_OnGrabStartDelegate = new IKapCCallBackDelegate(IKapC_OnStreamStartFunc);
                res = IKapC.ItkStreamRegisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_START_OF_STREAM, Marshal.GetFunctionPointerForDelegate(IKapC_OnGrabStartDelegate), IntPtr.Zero);
                if (!CheckIKapC(res))
                {
                    DebugLog("Configure stream error:Register callback failed");
                    return false;
                }
                //注册采集超时回调
                IKapC_OnTimeoutDelegate = new IKapCCallBackDelegate(IKapC_OnTimeOutFunc);
                res = IKapC.ItkStreamRegisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_TIME_OUT, Marshal.GetFunctionPointerForDelegate(IKapC_OnTimeoutDelegate), IntPtr.Zero);
                if (!CheckIKapC(res))
                {
                    DebugLog("Configure stream error:Register callback failed");
                    return false;
                }
                //注册采集丢帧回调
                IKapC_OnFrameLostDelegate = new IKapCCallBackDelegate(IKapC_OnFrameLostFunc);
                res = IKapC.ItkStreamRegisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_FRAME_LOST, Marshal.GetFunctionPointerForDelegate(IKapC_OnFrameLostDelegate), IntPtr.Zero);
                if (!CheckIKapC(res))
                {
                    DebugLog("Configure stream error:Register callback failed");
                    return false;
                }
                //注册采集结束回调
                IKapC_OnGrabStopDelegate = new IKapCCallBackDelegate(IKapC_OnStreamEndFunc);
                res = IKapC.ItkStreamRegisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_STREAM, Marshal.GetFunctionPointerForDelegate(IKapC_OnGrabStopDelegate), IntPtr.Zero);
                if (!CheckIKapC(res))
                {
                    DebugLog("Configure stream error:Register callback failed");
                    return false;
                }
                //注册帧结束回调
                IKapC_OnFrameReadyDelegate = new IKapCCallBackDelegate(IKapC_OnFrameReadyFunc);
                res = IKapC.ItkStreamRegisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_FRAME, Marshal.GetFunctionPointerForDelegate(IKapC_OnFrameReadyDelegate), IntPtr.Zero);
                if (!CheckIKapC(res))
                {
                    DebugLog("Configure stream error:Register callback failed");
                    return false;
                }

            }

            //申请用户缓冲区
            m_pUserBuffer = Marshal.AllocHGlobal(m_nBufferSize);

            return true;
        }

        /*
         * @brief:清除已申请缓冲区资源
         * @return:
         */
        public virtual void releaseStream()
        {
            if (m_pBoard == new IntPtr(-1)) //设备：相机
            {
                //注销回调函数
                IKapC.ItkStreamUnregisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_START_OF_STREAM);
                IKapC.ItkStreamUnregisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_STREAM);
                IKapC.ItkStreamUnregisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_FRAME_LOST);
                IKapC.ItkStreamUnregisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_TIME_OUT);
                IKapC.ItkStreamUnregisterCallback(m_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_FRAME);

                IKapC.ItkDevFreeStream(m_hStream); //释放流
            }
            else
            {
                //设备：相机+采集卡

                //注销回调函数
                IKapBoard.IKapUnRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_GrabStart);
                IKapBoard.IKapUnRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_FrameReady);
                IKapBoard.IKapUnRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_FrameLost);
                IKapBoard.IKapUnRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_TimeOut);
                IKapBoard.IKapUnRegisterCallback(m_pBoard, (uint)IKapBoard.IKEvent_GrabStop);
            }
            //释放用户缓冲区
            //if (m_pUserBuffer != new IntPtr(-1))
            //{
            //    Marshal.FreeHGlobal(m_pUserBuffer);
            //    m_pUserBuffer = new IntPtr(-1);
            //}
        }

        /*
         *@brief:开始采集
         *@param [in] nCount:采集帧数
         *@return:是否开始采集
         */
        public virtual bool startGrab(int nCount)
        {
            //开始采集
            if (m_pBoard == new IntPtr(-1))
            { //设备类型：相机            
                uint nGrabCount = (uint)nCount;
                if (nCount <= 0)
                    nGrabCount = IKapCLib.ITKSTREAM_CONTINUOUS;
                uint res = IKapC.ItkStreamStart(m_hStream, nGrabCount);
                if (!CheckIKapC(res))
                {
                    DebugLog("Start grab error:Start stream failed");
                    return false;
                }
            }
            else
            {

                var temp = IKapC.ItkDevExecuteCommand(m_hDev, "AcquisitionStop");

                int ret = IKapBoard.IKapStartGrab(m_pBoard, nCount);
                if (!CheckIKapBoard(ret))
                    return false;
                //设备类型：采集卡+相机
                temp = IKapC.ItkDevExecuteCommand(m_hDev, "AcquisitionStart");

                ret = IKapBoard.IKapStartGrab(m_pBoard, nCount);
                if (!CheckIKapBoard(ret))
                    return false;

            }

            m_bUpdateImage = false;
            m_nCurFrameIndex = 0;
            m_bGrabingImage = true;
            return true;
        }

        /*
         *@brief:等待序列采集结束
         *@param [in] nCount:采集帧数
         *@return:是否开始采集
         */
        public virtual bool waitGrab()
        {
            if (m_pBoard == new IntPtr(-1))
            { //设备类型：相机            
                uint res = IKapC.ItkStreamWait(m_hStream);
                if (!CheckIKapC(res))
                {
                    DebugLog($"Waiting stream operation failed, retured {res}.");
                    return false;
                }
            }
            else
            { //设备类型：采集卡+相机
                int ret = IKapBoard.IKapWaitGrab(m_pBoard);
                if (!CheckIKapBoard(ret))
                {
                    DebugLog($"Waiting stream operation failed!");
                    return false;
                }
            }

            return true;
        }

        /*
         *@brief:停止采集
         *@return:是否停止采集
         */
        public virtual bool stopGrab()
        {
            if (m_pBoard == new IntPtr(-1))
            { //设备类型：相机  

                uint res = IKapC.ItkStreamStop(m_hStream);
                if (!CheckIKapC(res))
                {
                    DebugLog("Stop grab error:Stop stream failed");
                    return false;
                }

                m_bGrabingImage = false;
                return true;
            }
            else
            { //设备类型：采集卡+相机

                IKapBoard.IKapStopGrab(m_pBoard);

                m_bGrabingImage = false;
                return true;
            }
        }

        /*
         * @brief:设置相机特征值
         * @param [in] featureName:特征名
         * @param [in] featureValue:特征值
         * @return: 是否设置成功
         */
        public bool setFeatureValue(string featureName, string featureValue)
        {
            ITKFEATURE itkFeature = new ITKFEATURE();
            uint nType = 0;
            uint res = IKapC.ItkDevAllocFeature(m_hDev, featureName, ref itkFeature);
            if (!CheckIKapC(res))
            {
                DebugLog("Camera error:Allocate feature failed");
                return false;
            }
            res = IKapC.ItkFeatureGetType(itkFeature, ref nType);
            if (!CheckIKapC(res))
            {
                DebugLog("Camera error:Get feature type failed");
                return false;
            }
            if (nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_INT32)
            {
                res = IKapC.ItkFeatureSetInt32(itkFeature, Convert.ToInt32(featureValue));
            }
            else if (nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_INT32)
            {
                res = IKapC.ItkFeatureSetInt64(itkFeature, Convert.ToInt64(featureValue));
            }
            else if (nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_FLOAT || nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_DOUBLE)
            {
                res = IKapC.ItkFeatureSetDouble(itkFeature, Convert.ToDouble(featureValue));
            }
            else if (nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_ENUM || nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_STRING)
            {
                res = IKapC.ItkFeatureFromString(itkFeature, featureValue);
            }
            else if (nType == (uint)ITKFEATURE_VAL_TYPE_LIST.ITKFEATURE_VAL_TYPE_COMMAND)
            {
                res = IKapC.ItkFeatureExecuteCommand(itkFeature);
            }
            else
            {
                DebugLog("Not supported feature type: " + nType);
            }

            if (!CheckIKapC(res))
            {
                DebugLog("Camera error:Set feature failed, res: " + res.ToString("X"));
                return false;
            }
            return true;
        }


        /*
        *@brief:获取相机图片格式
        *@param [in]:
        *@return:相机图片格式
        */
        public virtual uint getPixelFormat()
        {
            uint nPixelFormat = 0;
            uint nFormatLen = 64;
            StringBuilder sPixelFormat = new StringBuilder((int)nFormatLen);
            uint res = IKapC.ItkDevToString(m_hDev, "PixelFormat", sPixelFormat, ref nFormatLen);
            if (!CheckIKapC(res))
            {
                DebugLog("Pixel format error:Get pixel format failed");
                return nPixelFormat;
            }

            if (m_nType == 0 || m_nType == 4)
            {
                //GV、U3V设备
                res = IKapC.ItkDevGetPixelFormatVal(m_hDev, sPixelFormat.ToString(), ref nPixelFormat);
                if (!CheckIKapC(res))
                {
                    DebugLog("ItkDevGetPixelFormatVal: Excute failed!");
                }
            }
            else
            {
                if (sPixelFormat.ToString() == "Mono8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_MONO8;
                }
                else if (sPixelFormat.ToString() == "Mono10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_MONO10;
                }
                else if (sPixelFormat.ToString() == "Mono12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_MONO12;
                }
                else if (sPixelFormat.ToString() == "BayerGR8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GR8;
                }
                else if (sPixelFormat.ToString() == "BayerRG8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_RG8;
                }
                else if (sPixelFormat.ToString() == "BayerGB8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GB8;
                }
                else if (sPixelFormat.ToString() == "BayerBG8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_BG8;
                }
                else if (sPixelFormat.ToString() == "BayerGR10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GR10;
                }
                else if (sPixelFormat.ToString() == "BayerRG10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_RG10;
                }
                else if (sPixelFormat.ToString() == "BayerGB10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GB10;
                }
                else if (sPixelFormat.ToString() == "BayerBG10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_BG10;
                }
                else if (sPixelFormat.ToString() == "BayerGR12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GR12;
                }
                else if (sPixelFormat.ToString() == "BayerRG12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_RG12;
                }
                else if (sPixelFormat.ToString() == "BayerGB12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GB12;
                }
                else if (sPixelFormat.ToString() == "BayerBG12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_BG12;
                }
                else if (sPixelFormat.ToString() == "RGB8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_RGB888;
                }
                else if (sPixelFormat.ToString() == "RGB10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_RGB101010;
                }
                else if (sPixelFormat.ToString() == "RGB12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_RGB121212;
                }
                else if (sPixelFormat.ToString() == "BGR8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BGR888;
                }
                else if (sPixelFormat.ToString() == "BGR10")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BGR101010;
                }
                else if (sPixelFormat.ToString() == "BGR12")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BGR121212;
                }
                else if (sPixelFormat.ToString() == "YUV422_8")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_YUV422_8_UYUV;
                }
                //采集卡会对相机传输的图像数据解packed模式，所以最终图像像素格式为非packed类型
                else if (sPixelFormat.ToString() == "BayerRG12Packed")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_RG12;
                }
                else if (sPixelFormat.ToString() == "BayerGB12Packed")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GB12;
                }
                else if (sPixelFormat.ToString() == "BayerBG12Packed")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_BG12;
                }
                else if (sPixelFormat.ToString() == "BayerGR12Packed")
                {
                    nPixelFormat = IKapC.ITKBUFFER_VAL_FORMAT_BAYER_GR12;
                }
                else
                {
                    DebugLog("Pixel format error:Undefined format type");
                    MessageBox.Show("Pixel format error:Undefined format type", "Error", MessageBoxButtons.OK);
                }
            }

            return nPixelFormat;
        }

        /*
         *@brief:Bayer image transform to RGB image
         *@param [in]：pDataSrc: the buffer of Bayer image 
         *@param [in]：nDataSrcFormat: the pixelformat of Bayer image defiend in IKapC 
         *@param [out]：pDataDst: the buffer of RGB image
         *@param [in]：nDataSrcFormat: the pixelformat of RGB image defiend in IKapC 
         *@param [in]：nWidth: the width of image
         *@param [in]：nHeight: the height of image
         *@param [in]：nBayerEncodeType: the encoding type of Bayer image，optional valid params: 1[BGGR]; 2[RGGB]; 3[GBRG]; 4[GRBG]
         *@return:是否错误
         */
        public unsafe static bool Bayer2RGB(IntPtr pDataSrc, uint nDataSrcFormat, ref IntPtr pDataDst, uint nDataDstFormat, int nWidth, int nHeight, int nBayerEncodeType)
        {
            DebugLog("Bayer2RGB Begin...");

            uint res = IKapC.ITKSTATUS_OK;
            ITKBUFFER itkBufferBayer = new ITKBUFFER();
            ITKBUFFER itkBufferRGB = new ITKBUFFER();

            res = IKapC.ItkBufferNewWithPtr(nWidth, nHeight, nDataSrcFormat, pDataSrc, ref itkBufferBayer);
            if (!CheckIKapC(res))
            {
                return false;
            }
            res = IKapC.ItkBufferNewWithPtr(nWidth, nHeight, nDataDstFormat, pDataDst, ref itkBufferRGB);
            if (!CheckIKapC(res))
            {
                return false;
            }
            ///res = IKapC.ItkBufferSave(itkBufferBayer,"./bayer.tif",IKapC.NET.ItkBufferSaveType.ITKBUFFER_VAL_TIFF); 

            DebugLog("ItkBufferBayerConvert Begin...");
            res = IKapC.ItkBufferBayerConvert(itkBufferBayer, itkBufferRGB, IKapC.ITKBUFFER_VAL_BAYER_METHOD_BILINEAR | IKapCLib.IKAPC_BAYER_PATTERN_VAL_FROM_IKAP(nBayerEncodeType)); //ITKBUFFER_VAL_BAYER_METHOD_BILINEAR为最快算法
            DebugLog("ItkBufferBayerConvert End!");
            if (!CheckIKapC(res))
            {
                return false;
            }
            ///res = IKapC.ItkBufferSave(itkBufferRGB, "./rgb.tif", IKapC.NET.ItkBufferSaveType.ITKBUFFER_VAL_TIFF);

            //释放资源，否则会造成资源泄露
            res = IKapC.ItkBufferFree(itkBufferBayer);
            if (!CheckIKapC(res))
            {
                return false;
            }
            res = IKapC.ItkBufferFree(itkBufferRGB);
            if (!CheckIKapC(res))
            {
                return false;
            }

            DebugLog("Bayer2RGB end!");
            return true;
        }
        public unsafe static bool Bayer2BGR(IntPtr pDataSrc, uint nDataSrcFormat, ref IntPtr pDataDst, uint nDataDstFormat, int nWidth, int nHeight, int nBayerEncodeType)
        {
            DebugLog("Bayer2RGB Begin...");

            uint res = IKapC.ITKSTATUS_OK;
            ITKBUFFER itkBufferBayer = new ITKBUFFER();
            ITKBUFFER itkBufferBGR = new ITKBUFFER();

            res = IKapC.ItkBufferNewWithPtr(nWidth, nHeight, nDataSrcFormat, pDataSrc, ref itkBufferBayer);
            if (!CheckIKapC(res))
            {
                return false;
            }
            res = IKapC.ItkBufferNewWithPtr(nWidth, nHeight, nDataDstFormat, pDataDst, ref itkBufferBGR);
            if (!CheckIKapC(res))
            {
                return false;
            }
            ///res = IKapC.ItkBufferSave(itkBufferBayer,"./bayer.tif",IKapC.NET.ItkBufferSaveType.ITKBUFFER_VAL_TIFF); 

            DebugLog("ItkBufferBayerConvert Begin...");
            res = IKapC.ItkBufferBayerConvert(itkBufferBayer, itkBufferBGR, IKapC.ITKBUFFER_VAL_BAYER_METHOD_BILINEAR | IKapCLib.IKAPC_BAYER_PATTERN_VAL_FROM_IKAP(nBayerEncodeType)); //ITKBUFFER_VAL_BAYER_METHOD_BILINEAR为最快算法
            DebugLog("ItkBufferBayerConvert End!");
            if (!CheckIKapC(res))
            {
                return false;
            }
            ///res = IKapC.ItkBufferSave(itkBufferBGR, "./rgb.tif", IKapC.NET.ItkBufferSaveType.ITKBUFFER_VAL_TIFF);

            //释放资源，否则会造成资源泄露
            res = IKapC.ItkBufferFree(itkBufferBayer);
            if (!CheckIKapC(res))
            {
                return false;
            }
            res = IKapC.ItkBufferFree(itkBufferBGR);
            if (!CheckIKapC(res))
            {
                return false;
            }

            DebugLog("Bayer2BGR end!");
            return true;
        }

        public string GetOption()
        {
            string vlcfFileName = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "vlcf文件(*.vlcf)|*.vlcf|所有文件(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "选择打开文件";
            if (ofd.ShowDialog() == DialogResult.OK)
                vlcfFileName = ofd.FileName;
            return vlcfFileName;
        }

        internal static void DebugLog(string message)
        {
#if DEBUG
            OutputDebugString($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
#endif
        }

        /*
         *@brief:检查IKapCDotNet接口返回
         *@param [in] err:错误码
         *@return:是否错误
         */
        public static bool CheckIKapC(uint err)
        {
            if (err != IKapC.ITKSTATUS_OK)
            {
                DebugLog("Error code: " + err.ToString("x8"));
                return false;
            }
            return true;
        }

        /*
         *@brief:检查IKapBoardDotNet接口返回
         *@param [in] err:错误码
         *@return:是否错误
         */
        public static bool CheckIKapBoard(int ret)
        {
            if (ret != IKapBoard.IK_RTN_OK)
            {
                IKAPERRORINFO tIKei = new IKAPERRORINFO();
                string sErrMsg = "";

                // 获取错误码信息。
                //
                // Get error code message.
                IKapBoard.IKapGetLastError(tIKei, true);

                // 打印错误信息。
                //
                // Print error message.
                sErrMsg = string.Concat("Error",
                                        sErrMsg,
                                        "Board Type\t = 0x", tIKei.uBoardType.ToString("X4"), "\n",
                                        "Board Index\t = 0x", tIKei.uBoardIndex.ToString("X4"), "\n",
                                        "Error Code\t = 0x", tIKei.uErrorCode.ToString("X4"), "\n"
                                        );
                DebugLog(sErrMsg);
                return false;
            }
            return true;

        }


        #region Callback
        // 开始抓帧回调
        public void IKapB_OnGrabStartFunc(IntPtr pParam)
        {
            Console.WriteLine("Start grabbing image");
        }

        // 丢帧回调
        public void IKapB_OnFrameLostFunc(IntPtr pParam)
        {
            Console.WriteLine("Frame lost");
        }

        // 帧超时回调
        public void IKapB_OnTimeoutFunc(IntPtr pParam)
        {
            Console.WriteLine("Grab image timeout");
        }

        // 停止抓取图像回调
        public void IKapB_OnGrabStopFunc(IntPtr pParam)
        {
            Console.WriteLine("Stop grabbing image");
        }

        public void IKapC_OnStreamStartFunc(uint eventType, IntPtr pContext)
        {
            DebugLog("Stream start");
        }
        public void IKapC_OnTimeOutFunc(uint eventType, IntPtr pContext)
        {
            DebugLog("Timeout");
        }
        public void IKapC_OnFrameLostFunc(uint eventType, IntPtr pContext)
        {
            DebugLog("Frame lost");
        }
        public void IKapC_OnStreamEndFunc(uint eventType, IntPtr pContext)
        {
            DebugLog("Stream end");
        }
        public void IKapC_OnFrameReadyFunc(uint eventType, IntPtr pContext)
        {
            try
            {
                m_nCurFrameIndex++;
                
                if (!m_bGrabingImage)
                    return;
                    
                ITKBUFFER hCurrBuffer = new ITKBUFFER();
                uint res = IKapC.ItkStreamGetCurrentBuffer(m_hStream, ref hCurrBuffer);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} GetCurrentBuffer失败: 0x{res:X8}");
                    return;
                }
                
                ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                res = IKapC.ItkBufferGetInfo(hCurrBuffer, bufferInfo);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} GetBufferInfo失败: 0x{res:X8}");
                    return;
                }
                
                uint bufferStatus = (uint)bufferInfo.State;
                ulong nImageSize = bufferInfo.ValidImageSize;
                
                // 使用 Debug.WriteLine 避免阻塞回调线程
                Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex}, 状态={bufferStatus}, 大小={nImageSize}");
                
                if (bufferStatus == IKapC.ITKBUFFER_VAL_STATE_FULL || bufferStatus == IKapC.ITKBUFFER_VAL_STATE_UNCOMPLETED)
                {
                    if (m_pUserBuffer == IntPtr.Zero || m_pUserBuffer == new IntPtr(-1))
                    {
                        Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} 用户缓冲区无效");
                        return;
                    }
                    if (nImageSize <= 0 || nImageSize > (ulong)m_nBufferSize)
                    {
                        Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} 图像大小无效: {nImageSize}");
                        return;
                    }
                    
                    res = IKapC.ItkBufferRead(hCurrBuffer, 0, m_pUserBuffer, (uint)nImageSize);
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} BufferRead失败: 0x{res:X8}");
                        return;
                    }
                   
                    lock (_frameLock)
                    {
                        try
                        {
                            // 复制图像数据，确保 Image 持有独立副本
                            using (var tempImage = new HImage("byte", m_nWidth, m_nHeight, m_pUserBuffer))
                            {
                                Image = tempImage.CopyImage();
                            }
                            m_bUpdateImage = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[IKap回调] 帧#{m_nCurFrameIndex} HImage创建失败: {ex.Message}");
                        }
                    }
                    
                    EventWait?.Set();
                     
                    // 触发图像回调事件
                    ImageGrab?.Invoke(Image);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IKap回调] 异常: {ex.Message}");
            }
        }
        #endregion Callback

    }

    struct IKDeviceInfo
    {
        public IKDeviceType nType;
        public int nDevIndex;
        public int nBoardIndex;
        public string sDevName;
    }

    // 设备类型枚举
    public enum IKDeviceType
    {
        DEVICE_NIL = 0,
        DEVICE_CML,
        DEVICE_CXP,
        DEVICE_U3V,
        DEVICE_GIGEVISION,
        DEVICE_GIGEVISIONBOARD
    }
}

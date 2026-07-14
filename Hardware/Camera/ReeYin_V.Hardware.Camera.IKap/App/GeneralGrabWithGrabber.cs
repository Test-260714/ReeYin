using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IKapCDotNet;
using IKapBoardDotNet;

namespace ReeYin_V.Hardware.Camera.IKap
{
    /// <summary>
    /// IKap 采集卡相机辅助类
    /// 参考官方示例 GeneralGrabWithGrabber
    /// </summary>
    public static class GeneralGrabWithGrabber
    {
        /// <summary>
        /// 配置相机设备
        /// </summary>
        public static void ConfigureCamera(IKapCamera cam)
        {
            uint res = IKapC.ITKSTATUS_OK;
            uint numCameras = 0;

            res = IKapC.ItkManGetDeviceCount(ref numCameras);
            if (!CheckIKapC(res) || numCameras == 0)
            {
                Console.WriteLine("No camera found.");
                return;
            }

            // 根据序列号查找相机
            for (uint i = 0; i < numCameras; i++)
            {
                ITKDEV_INFO di = new ITKDEV_INFO();
                res = IKapC.ItkManGetDeviceInfo(i, di);

                if (!string.IsNullOrEmpty(cam.g_SerialNumber))
                {
                    if (string.Compare(di.SerialNumber, cam.g_SerialNumber) == 0)
                    {
                        Console.WriteLine($"Using camera: serial: {di.SerialNumber}, name: {di.FullName}, interface: {di.DeviceClass}");
                        res = IKapC.ItkDevOpen(i, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref cam.m_hDev);
                        CheckIKapC(res);
                        cam.g_devInfo = di;
                        break;
                    }
                }
            }

            if (cam.m_hDev == null)
            {
                Console.WriteLine("Cannot find proper camera.");
                return;
            }

            // 设置相机超时时间
            IntPtr timeOutPtr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(timeOutPtr, 20000);
            res = IKapC.ItkDevSetPrm(cam.m_hDev, IKapC.ITKDEV_PRM_HEARTBEAT_TIMEOUT, timeOutPtr);
            CheckIKapC(res);
            Marshal.FreeHGlobal(timeOutPtr);

            // 打开采集卡
            OpenFrameGrabber(cam);
        }

        /// <summary>
        /// 打开采集卡设备
        /// </summary>
        private static void OpenFrameGrabber(IKapCamera cam)
        {
            uint res = IKapC.ITKSTATUS_OK;
            IntPtr info = IntPtr.Zero;

            // 普通 GigE 或 USB3 相机不需要采集卡
            if (string.Compare(cam.g_devInfo.DeviceClass, "GigEVision") == 0 ||
                string.Compare(cam.g_devInfo.DeviceClass, "USB3Vision") == 0)
            {
                // 不需要打开采集卡，直接返回
                cam.m_pBoard = IntPtr.Zero;
                return;
            }

            if (string.Compare(cam.g_devInfo.DeviceClass, "CoaXPress") == 0)
            {
                ITK_CXP_DEV_INFO cxp_cam_info = new ITK_CXP_DEV_INFO();
                uint numCameras = 0;
                IKapC.ItkManGetDeviceCount(ref numCameras);
                for (uint i = 0; i < numCameras; i++)
                {
                    ITKDEV_INFO di = new ITKDEV_INFO();
                    IKapC.ItkManGetDeviceInfo(i, di);
                    if (string.Compare(di.SerialNumber, cam.g_SerialNumber) == 0)
                    {
                        res = IKapC.ItkManGetCXPDeviceInfo(i, cxp_cam_info);
                        CheckIKapC(res);
                        info = IKapC.get_itk_cxp_dev_info_IntPtr(cxp_cam_info);
                        break;
                    }
                }
            }
            else if (string.Compare(cam.g_devInfo.DeviceClass, "GigEVisionBoard") == 0)
            {
                ITK_GVB_DEV_INFO gvb_cam_info = new ITK_GVB_DEV_INFO();
                uint numCameras = 0;
                IKapC.ItkManGetDeviceCount(ref numCameras);
                for (uint i = 0; i < numCameras; i++)
                {
                    ITKDEV_INFO di = new ITKDEV_INFO();
                    IKapC.ItkManGetDeviceInfo(i, di);
                    if (string.Compare(di.SerialNumber, cam.g_SerialNumber) == 0)
                    {
                        res = IKapC.ItkManGetGVBDeviceInfo(i, gvb_cam_info);
                        CheckIKapC(res);
                        info = IKapC.get_itk_gvb_dev_info_IntPtr(gvb_cam_info);
                        break;
                    }
                }
            }
            else if (string.Compare(cam.g_devInfo.DeviceClass, "CameraLink") == 0)
            {
                ITK_CL_DEV_INFO cl_cam_info = new ITK_CL_DEV_INFO();
                uint numCameras = 0;
                IKapC.ItkManGetDeviceCount(ref numCameras);
                for (uint i = 0; i < numCameras; i++)
                {
                    ITKDEV_INFO di = new ITKDEV_INFO();
                    IKapC.ItkManGetDeviceInfo(i, di);
                    if (string.Compare(di.SerialNumber, cam.g_SerialNumber) == 0)
                    {
                        res = IKapC.ItkManGetCLDeviceInfo(i, cl_cam_info);
                        CheckIKapC(res);
                        info = IKapC.get_itk_cl_dev_info_IntPtr(cl_cam_info);
                        break;
                    }
                }
            }

            if (info != IntPtr.Zero)
            {
                cam.m_pBoard = IKapBoard.IKapOpenWithSpecificInfo(info);
            }
        }

        /// <summary>
        /// 配置采集卡设备
        /// </summary>
        public static void ConfigureFrameGrabber(IKapCamera cam)
        {
            int ret = IKapBoard.IK_RTN_OK;

            if (cam.m_pBoard == IntPtr.Zero)
                return;

            // 可选：加载采集卡配置文件
            if (cam.g_bLoadGrabberConfig)
            {
                string configFileName = GetConfigFile();
                if (!string.IsNullOrEmpty(configFileName))
                {
                    ret = IKapBoard.IKapLoadConfigurationFromFile(cam.m_pBoard, configFileName);
                    CheckIKapBoard(ret);
                }
            }

            // 设置缓冲区帧数
            ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_FRAME_COUNT, (int)cam.m_nFrameCount);
            CheckIKapBoard(ret);

            // 设置超时时间 (-1 表示无限等待)
            ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_TIME_OUT, -1);
            CheckIKapBoard(ret);

            // 设置非阻塞采集模式
            ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_GRAB_MODE, IKapBoard.IKP_GRAB_NON_BLOCK);
            CheckIKapBoard(ret);

            // 设置帧传输模式
            ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_FRAME_TRANSFER_MODE, 
                IKapBoard.IKP_FRAME_TRANSFER_SYNCHRONOUS_NEXT_EMPTY_WITH_PROTECT);
            CheckIKapBoard(ret);
        }

        /// <summary>
        /// 设置软触发模式
        /// </summary>
        public static void SetSoftTriggerWithGrabber(IKapCamera cam)
        {
            uint res = IKapC.ITKSTATUS_OK;
            int ret = IKapBoard.IK_RTN_OK;

            if (cam.g_bSoftTriggerUsed)
            {
                if (string.Compare(cam.g_devInfo.DeviceClass, "CoaXPress") == 0)
                {
                    res = IKapC.ItkDevSetDouble(cam.m_hDev, "ExposureTime", 80);
                    res = IKapC.ItkDevFromString(cam.m_hDev, "TriggerMode", "On");
                    res = IKapC.ItkDevFromString(cam.m_hDev, "ExposureMode", "TriggerPulse");
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_IMAGE_HEIGHT, 1000);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_CXP_TRIGGER_OUTPUT_SELECTOR, 1);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_INTEGRATION_TRIGGER_SOURCE, 9);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_INTEGRATION_METHOD, 4);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_INTEGRATION_PARAM2, 5000);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_CXP_FRAME_BURST_COUNT, 1050);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_CXP_FRAME_BURST_PERIOD, 100000);
                    CheckIKapBoard(ret);

                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_SOFTWARE_TRIGGER_WIDTH, 5000);
                    CheckIKapBoard(ret);
                }
                else if (string.Compare(cam.g_devInfo.DeviceClass, "CameraLink") == 0)
                {
                    res = IKapC.ItkDevFromString(cam.m_hDev, "SynchronizationMode", "ExternalPulse");
                    res = IKapC.ItkDevFromString(cam.m_hDev, "InputLineTriggerSource", "CC1");
                    res = IKapC.ItkDevSetDouble(cam.m_hDev, "LinePeriodTime", 50);
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_IMAGE_HEIGHT, 2000);
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_CC1_SOURCE, 5);
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_SOFTWARE_TRIGGER_SYNC_MODE, 0);
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_SOFTWARE_TRIGGER_PERIOD, 80);
                    ret = IKapBoard.IKapSetInfo(cam.m_pBoard, (uint)IKapBoard.IKP_SOFTWARE_TRIGGER_COUNT, 2050);
                }
                else
                {
                    // GigE 线扫相机可能不支持 TriggerSelector/TriggerMode/TriggerSource，忽略错误
                    IKapC.ItkDevFromString(cam.m_hDev, "TriggerSelector", "FrameStart");
                    IKapC.ItkDevFromString(cam.m_hDev, "TriggerMode", "On");
                    IKapC.ItkDevFromString(cam.m_hDev, "TriggerSource", "Software");
                }
            }
            else
            {
                if (string.Compare(cam.g_devInfo.DeviceClass, "CoaXPress") == 0)
                {
                    res = IKapC.ItkDevFromString(cam.m_hDev, "ExposureMode", "Timed");
                }
                else if (string.Compare(cam.g_devInfo.DeviceClass, "CameraLink") == 0)
                {
                    res = IKapC.ItkDevFromString(cam.m_hDev, "SynchronizationMode", "InternalFreeRun");
                }
                else
                {
                    // GigE 线扫相机可能不支持 TriggerSelector/TriggerMode，忽略错误
                    IKapC.ItkDevFromString(cam.m_hDev, "TriggerSelector", "FrameStart");
                    IKapC.ItkDevFromString(cam.m_hDev, "TriggerMode", "Off");
                }
            }
        }

        /// <summary>
        /// 开始采集图像
        /// </summary>
        public static bool StartGrabImage(IKapCamera cam)
        {
            uint res = IKapC.ITKSTATUS_OK;
            int ret = IKapBoard.IK_RTN_OK;

            // GigE/USB3 相机不需要采集卡，使用流方式采集
            // m_pBoard == IntPtr.Zero 或 m_pBoard == new IntPtr(-1) 都表示不使用采集卡
            bool useFrameGrabber = cam.m_pBoard != IntPtr.Zero && cam.m_pBoard != new IntPtr(-1);
            
            if (!useFrameGrabber)
            {
                // 如果已经在采集中，直接返回成功
                if (cam.m_bGrabingImage)
                {
                    return true;
                }
                
                // 设置自由运行模式（关闭触发，让相机持续输出）
                SetFreeRunMode(cam);
                
                // 启动流采集
                uint nGrabCount = IKapCLib.ITKSTREAM_CONTINUOUS;
                res = IKapC.ItkStreamStart(cam.m_hStream, nGrabCount);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    Console.WriteLine($"ItkStreamStart 失败: 0x{res:X8}");
                    return false;
                }
                
                // 开始采集
                res = IKapC.ItkDevExecuteCommand(cam.m_hDev, "AcquisitionStart");
                if (res != IKapC.ITKSTATUS_OK)
                {
                    Console.WriteLine($"AcquisitionStart 失败: 0x{res:X8}");
                }
                
                cam.m_bGrabingImage = true;
                Console.WriteLine("连续采集已启动");
                return true;
            }

            // 非 CameraLink 相机需要先停止再启动
            if (string.Compare(cam.g_devInfo.DeviceClass, "CameraLink") != 0)
            {
                res = IKapC.ItkDevExecuteCommand(cam.m_hDev, "AcquisitionStop");
            }

            ret = IKapBoard.IKapStartGrab(cam.m_pBoard, 0);
            if (!CheckIKapBoard(ret))
                return false;

            if (string.Compare(cam.g_devInfo.DeviceClass, "CameraLink") != 0)
            {
                res = IKapC.ItkDevExecuteCommand(cam.m_hDev, "AcquisitionStart");
            }

            cam.m_bGrabingImage = true;
            return true;
        }

        /// <summary>
        /// 设置自由运行模式（关闭触发，让相机持续输出）
        /// </summary>
        private static void SetFreeRunMode(IKapCamera cam)
        {
            try
            {
                uint res;
                
                // 1. 尝试关闭 LineStart 触发（线扫相机）
                res = IKapC.ItkDevFromString(cam.m_hDev, "TriggerSelector", "LineStart");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkDevFromString(cam.m_hDev, "TriggerMode", "Off");
                    Console.WriteLine($"LineStart触发模式: {(res == IKapC.ITKSTATUS_OK ? "已关闭" : $"设置失败0x{res:X8}")}");
                }
                
                // 2. 尝试关闭 FrameStart 触发
                res = IKapC.ItkDevFromString(cam.m_hDev, "TriggerSelector", "FrameStart");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkDevFromString(cam.m_hDev, "TriggerMode", "Off");
                    Console.WriteLine($"FrameStart触发模式: {(res == IKapC.ITKSTATUS_OK ? "已关闭" : $"设置失败0x{res:X8}")}");
                }
                
                // 3. 尝试设置内部行频率（线扫相机）
                double lineRate = 0;
                res = IKapC.ItkDevGetDouble(cam.m_hDev, "AcquisitionLineRate", ref lineRate);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    Console.WriteLine($"当前行频率: {lineRate} Hz");
                    if (lineRate < 100)
                    {
                        res = IKapC.ItkDevSetDouble(cam.m_hDev, "AcquisitionLineRate", 10000);
                        Console.WriteLine($"设置行频率10kHz: {(res == IKapC.ITKSTATUS_OK ? "成功" : $"失败0x{res:X8}")}");
                    }
                }
                
                Console.WriteLine("已设置自由运行模式");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置自由运行模式异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 选择配置文件
        /// </summary>
        private static string GetConfigFile()
        {
            string vlcfFileName = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "vlcf文件(*.vlcf)|*.vlcf|所有文件(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "选择采集卡配置文件";
            if (ofd.ShowDialog() == DialogResult.OK)
                vlcfFileName = ofd.FileName;
            return vlcfFileName;
        }

        /// <summary>
        /// 检查 IKapC 返回值
        /// </summary>
        private static bool CheckIKapC(uint res)
        {
            if (res != IKapC.ITKSTATUS_OK)
            {
                Console.WriteLine($"IKapC Error: {res}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查 IKapBoard 返回值
        /// </summary>
        private static bool CheckIKapBoard(int ret)
        {
            if (ret != IKapBoard.IK_RTN_OK)
            {
                Console.WriteLine($"IKapBoard Error: {ret}");
                return false;
            }
            return true;
        }
    }
}

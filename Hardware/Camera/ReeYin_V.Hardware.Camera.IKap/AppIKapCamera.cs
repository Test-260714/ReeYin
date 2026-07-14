using HalconDotNet;
using IKapBoardDotNet;
using IKapCDotNet;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper.Memory;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Image;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using static OpenCvSharp.Stitcher;

namespace ReeYin_V.Hardware.Camera.IKap
{
    public enum CamHardType
    {
        /// <summary>
        /// 普通2D
        /// </summary>
        General = 0,
        /// <summary>
        /// 采集卡
        /// </summary>
        Card = 1,
        /// <summary>
        /// 线扫
        /// </summary>
        LineScan = 2,

    }

    [Category("相机")]
    [DisplayName("埃克相机")]
    [Serializable]
    public partial class IKapCamera: CameraBase
    {
        #region Fields

        #endregion

        #region Serialized
        //[OnSerializing()] 序列化之前
        //[OnSerialized()] 序列化之后
        //[OnDeserializing()] 反序列化之前
        [OnDeserialized()] //反序列化之后
        internal void OnDeserializedMethod(StreamingContext context)
        {
            ResetRuntimeState();

            uint res = IKapC.ItkManInitialize();
            if (res != IKapC.ITKSTATUS_OK)
            {
                Console.WriteLine("相机初始化失败");
                return;
            }

            ConnectDev();
            if (Connected)
            {
                GetAllParam();
            }
        }
        #endregion

        #region Overriden Methods
        /// <summary>搜索相机</summary>
        public override List<CameraInfoModel> SearchCameras()
        {
            List<CameraInfoModel> mCamInfoList = new List<CameraInfoModel>();

            uint nDevCount = 0;
            uint res = IKapC.ItkManInitialize();
            res = IKapC.ItkManGetDeviceCount(ref nDevCount);
            if (res != IKapC.ITKSTATUS_OK)
                return null;

            g_devInfo = new ITKDEV_INFO();
            ITK_CL_DEV_INFO pClDevInfo = new ITK_CL_DEV_INFO();
            ITK_CXP_DEV_INFO pCxpDevInfo = new ITK_CXP_DEV_INFO();
            ITKGIGEDEV_INFO pGvDevInfo = new ITKGIGEDEV_INFO();
            ITK_GVB_DEV_INFO pGvbDevInfo = new ITK_GVB_DEV_INFO();
            ITK_U3V_DEV_INFO pU3VDevInfo = new ITK_U3V_DEV_INFO();
            for (uint i = 0; i < nDevCount; ++i)
            {
                IKDeviceInfo pInfo = new IKDeviceInfo();
                IKapC.ItkManGetDeviceInfo(i, g_devInfo);
                if (g_devInfo.DeviceClass.CompareTo("GigEVision") == 0)
                {
                    res = IKapC.ItkManGetGigEDeviceInfo(i, pGvDevInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return null;
                    pInfo.nType = IKDeviceType.DEVICE_GIGEVISION;
                    pInfo.nDevIndex = (int)i;
                    pInfo.nBoardIndex = -1;
                    pInfo.sDevName = g_devInfo.FullName;
                }
                else if (g_devInfo.DeviceClass.CompareTo("USB3Vision") == 0)
                {
                    res = IKapC.ItkManGetU3VDeviceInfo(i, pU3VDevInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return null;
                    pInfo.nType = IKDeviceType.DEVICE_U3V;
                    pInfo.nDevIndex = (int)i;
                    pInfo.nBoardIndex = -1;
                    pInfo.sDevName = g_devInfo.FullName;
                }
                else if (g_devInfo.DeviceClass.CompareTo("GigEVisionBoard") == 0)
                {
                    res = IKapC.ItkManGetGVBDeviceInfo(i, pGvbDevInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return null;
                    pInfo.nType = IKDeviceType.DEVICE_GIGEVISIONBOARD;
                    pInfo.nDevIndex = (int)i;
                    pInfo.nBoardIndex = (int)pGvbDevInfo.BoardIndex;
                    pInfo.sDevName = g_devInfo.FullName;
                }
                else if (g_devInfo.DeviceClass.CompareTo("CoaXPress") == 0)
                {
                    res = IKapC.ItkManGetCXPDeviceInfo(i, pCxpDevInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return null;
                    pInfo.nType = IKDeviceType.DEVICE_CXP;
                    pInfo.nDevIndex = (int)i;
                    pInfo.nBoardIndex = (int)pCxpDevInfo.BoardIndex;
                    pInfo.sDevName = g_devInfo.FullName;
                }
                else
                {
                    res = IKapC.ItkManGetCLDeviceInfo(i, pClDevInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return null;
                    pInfo.nType = IKDeviceType.DEVICE_CML;
                    pInfo.nDevIndex = (int)i;
                    pInfo.nBoardIndex = (int)pClDevInfo.BoardIndex;
                    pInfo.sDevName = g_devInfo.FullName;
                }
                mCamInfoList.Add(new CameraInfoModel
                {
                    CamName = "IKap" + pInfo.sDevName,
                    SerialNO = g_devInfo.SerialNumber,
                    MaskName = pInfo.nBoardIndex.ToString(),
                    ExtInfo = pInfo.nBoardIndex,
                });
                ExtInfo = pInfo.nBoardIndex;
            }
            return mCamInfoList;
        }

        /// <summary>连接相机</summary>
        public override void ConnectDev()
        {
            try
            {
                // 如果已连接，先断开
                if (Connected || m_bIsOpened)
                {
                    DisConnectDev();
                }
                 
                // 初始化 SDK
                uint res = IKapC.ItkManInitialize();
                if (res != IKapC.ITKSTATUS_OK)
                {
                    Console.WriteLine($"IKap SDK 初始化失败: 0x{res:X8}");
                    Connected = false;
                    return;
                }

                // 获取设备数量
                uint nDevCount = 0;
                res = IKapC.ItkManGetDeviceCount(ref nDevCount);
                if (res != IKapC.ITKSTATUS_OK || nDevCount == 0)
                {
                    Console.WriteLine("未找到 IKap 设备");
                    Connected = false;
                    return;
                }

                // 根据序列号查找并打开相机
                g_SerialNumber = SerialNo;
                bool found = false;
                
                for (uint i = 0; i < nDevCount; i++)
                {
                    ITKDEV_INFO di = new ITKDEV_INFO();
                    res = IKapC.ItkManGetDeviceInfo(i, di);
                    if (res != IKapC.ITKSTATUS_OK)
                        continue;

                    if (string.Compare(di.SerialNumber, g_SerialNumber) == 0)
                    {
                        Console.WriteLine($"找到相机: {di.FullName}, 序列号: {di.SerialNumber}, 类型: {di.DeviceClass}");
                        
                        // 打开相机
                        res = IKapC.ItkDevOpen(i, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref m_hDev);
                        if (res != IKapC.ITKSTATUS_OK)
                        {
                            Console.WriteLine($"打开相机失败: 0x{res:X8}");
                            Connected = false;
                            return;
                        }
                        
                        g_devInfo = di;
                        m_nDevIndex = (int)i;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Console.WriteLine($"未找到序列号为 {g_SerialNumber} 的相机");
                    Connected = false;
                    return;
                }

                if (IsFrameGrabberDevice(g_devInfo.DeviceClass))
                {
                    if (!OpenFrameGrabber((uint)m_nDevIndex, g_devInfo))
                    {
                        Console.WriteLine("打开 IKap 采集卡失败");
                        IKapC.ItkDevClose(m_hDev);
                        Connected = false;
                        return;
                    }
                }
                else
                {
                    // GigE/USB3 相机不需要采集卡，统一使用 -1 作为无采集卡哨兵值。
                    m_pBoard = new IntPtr(-1);
                    m_nType = string.Compare(g_devInfo.DeviceClass, "USB3Vision", StringComparison.Ordinal) == 0 ? 4 : 0;
                }

                // 获取设备信息
                if (!GetDeviceInfo())
                {
                    Console.WriteLine("获取 IKap 设备信息失败");
                    if (m_pBoard != new IntPtr(-1))
                    {
                        IKapBoard.IKapClose(m_pBoard);
                        m_pBoard = new IntPtr(-1);
                    }
                    IKapC.ItkDevClose(m_hDev);
                    Connected = false;
                    return;
                }

                // 创建流并注册回调
                if (!createStream())
                {
                    Console.WriteLine("创建采集流失败");
                    IKapC.ItkDevClose(m_hDev);
                    Connected = false;
                    return;
                }

                // 初始化帧缓冲区
                if (m_nWidth > 0 && m_nHeight > 0)
                {
                    _frameBuffer = new UnmanagedArray2D<byte>(m_nWidth, m_nHeight);
                }

                m_bIsOpened = true;
                Connected = true;
                Console.WriteLine("相机连接成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConnectDev 异常: {ex.Message}");
                Connected = false;
            }
        }

        private static bool IsFrameGrabberDevice(string deviceClass)
        {
            return string.Compare(deviceClass, "CoaXPress", StringComparison.Ordinal) == 0 ||
                   string.Compare(deviceClass, "GigEVisionBoard", StringComparison.Ordinal) == 0 ||
                   string.Compare(deviceClass, "CameraLink", StringComparison.Ordinal) == 0;
        }

        private bool OpenFrameGrabber(uint deviceIndex, ITKDEV_INFO deviceInfo)
        {
            uint res;
            IntPtr boardInfo = IntPtr.Zero;

            if (string.Compare(deviceInfo.DeviceClass, "CoaXPress", StringComparison.Ordinal) == 0)
            {
                var cxpInfo = new ITK_CXP_DEV_INFO();
                res = IKapC.ItkManGetCXPDeviceInfo(deviceIndex, cxpInfo);
                if (!CheckIKapC(res))
                    return false;

                boardInfo = IKapC.get_itk_cxp_dev_info_IntPtr(cxpInfo);
                m_nBoardIndex = (int)cxpInfo.BoardIndex;
                m_nType = 2;
            }
            else if (string.Compare(deviceInfo.DeviceClass, "GigEVisionBoard", StringComparison.Ordinal) == 0)
            {
                var gvbInfo = new ITK_GVB_DEV_INFO();
                res = IKapC.ItkManGetGVBDeviceInfo(deviceIndex, gvbInfo);
                if (!CheckIKapC(res))
                    return false;

                boardInfo = IKapC.get_itk_gvb_dev_info_IntPtr(gvbInfo);
                m_nBoardIndex = (int)gvbInfo.BoardIndex;
                m_nType = 3;
            }
            else if (string.Compare(deviceInfo.DeviceClass, "CameraLink", StringComparison.Ordinal) == 0)
            {
                var clInfo = new ITK_CL_DEV_INFO();
                res = IKapC.ItkManGetCLDeviceInfo(deviceIndex, clInfo);
                if (!CheckIKapC(res))
                    return false;

                boardInfo = IKapC.get_itk_cl_dev_info_IntPtr(clInfo);
                m_nBoardIndex = (int)clInfo.BoardIndex;
                m_nType = 1;
            }
            else
            {
                Console.WriteLine($"不支持的采集卡相机类型: {deviceInfo.DeviceClass}");
                return false;
            }

            if (boardInfo == IntPtr.Zero)
                return false;

            m_pBoard = IKapBoard.IKapOpenWithSpecificInfo(boardInfo);
            if (m_pBoard == IntPtr.Zero || m_pBoard == new IntPtr(-1))
            {
                m_pBoard = new IntPtr(-1);
                return false;
            }

            if (string.Compare(deviceInfo.DeviceClass, "CameraLink", StringComparison.Ordinal) == 0)
            {
                string configFileName = GetOption();
                if (!string.IsNullOrWhiteSpace(configFileName))
                {
                    int ret = IKapBoard.IKapLoadConfigurationFromFile(m_pBoard, configFileName);
                    CheckIKapBoard(ret);
                }
            }

            return true;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public override void DisConnectDev()
        {
            try
            {
                // 停止采集
                if (m_bGrabingImage)
                {
                    stopGrab();
                }

                // 清除回调释放资源
                releaseStream();

                // 关闭采集卡
                if (m_pBoard != new IntPtr(-1))
                {
                    IKapBoard.IKapClose(m_pBoard);
                    m_pBoard = new IntPtr(-1);
                }

                // 关闭相机设备
                IKapC.ItkDevClose(m_hDev);
                m_hDev = new ITKDEVICE();

                m_bIsOpened = false;
                Connected = false;
                 
                Console.WriteLine("相机已断开连接");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DisConnectDev 异常: {ex.Message}");
            }
            
            base.DisConnectDev();
        }

        /// <summary>
        /// 设置指定参数值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool SetSpecifiedParam(string type, string key, object value)
        {
            try
            {
                switch (type)
                {
                    case "String":
                        {
                            var res = IKapC.ItkDevFromString(m_hDev, key, (string)value);
                            if (!CheckIKapC(res)) return false;
                        }
                        break;
                    case "Long":
                        {
                            var res = IKapC.ItkDevSetInt64(m_hDev, key, (long)value);
                            if (!CheckIKapC(res)) return false;
                        }
                        break;
                    case "Int":
                        {
                            var res = IKapC.ItkDevSetInt32(m_hDev, key, (int)value);
                            if (!CheckIKapC(res)) return false;
                        }
                        break;
                    case "Double":
                        {
                            var res = IKapC.ItkDevSetDouble(m_hDev, key, (double)value);
                            if (!CheckIKapC(res)) return false;
                        }
                        break;
                    case "Bool":
                        {
                            var res = IKapC.ItkDevSetBool(m_hDev, key, (bool)value);
                            if (!CheckIKapC(res)) return false;
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("SetSpecifiedParam()_未知类型");
                        }
                        break;

                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        /// <summary>
        /// 获取指定参数值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool GetSpecifiedParam(string type, string key,ref object value)
        {
            try
            {
                switch (type)
                {
                    case "String":
                        {
                            StringBuilder strValue = new StringBuilder();
                            uint stringSize = 128;
                            var res = IKapC.ItkDevToString(m_hDev, key, strValue,ref stringSize);
                            if (!CheckIKapC(res)) return false;
                            value = strValue.ToString();
                        }
                        break;
                    case "Long":
                        {
                            long temp = 0;
                            var res = IKapC.ItkDevGetInt64(m_hDev, key, ref temp);
                            if (!CheckIKapC(res)) return false;
                            value = temp;
                        }
                        break;
                    case "Int":
                        {
                            int temp = 0;
                            var res = IKapC.ItkDevGetInt32(m_hDev, key,ref temp);
                            if (!CheckIKapC(res)) return false;
                            value = temp;
                        }
                        break;
                    case "Double":
                        {
                            double temp = 0.0;
                            var res = IKapC.ItkDevGetDouble(m_hDev, key, ref temp);
                            if (!CheckIKapC(res)) return false;
                            value = temp;
                        }
                        break;
                    case "bool":
                        {
                            bool temp = false;
                            var res = IKapC.ItkDevGetBool(m_hDev, key,ref temp);
                            if (!CheckIKapC(res)) return false;
                            value = temp;
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("SetSpecifiedParam()_未知类型");
                        }
                        break;

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        /// <summary>采集图像,是否手动采图</summary>
        public override bool CaptureImage(bool byHand)
        {
            try
            {
                if (byHand)
                {
                    //使用命令
                    uint res = IKapC.ItkDevExecuteCommand(m_hDev, "TriggerSoftware");
                    if (!CheckIKapC(res))
                    {
                        Console.WriteLine("Execure Command TriggerSoftware failed");
                    }
                }
                else
                {
                    // 连续采集模式：主动从流中获取最新缓冲区
                    if (!m_bGrabingImage || m_hStream == null)
                        return false;
                    
                    ITKBUFFER hCurrBuffer = new ITKBUFFER();
                    uint res = IKapC.ItkStreamGetCurrentBuffer(m_hStream, ref hCurrBuffer);
                    if (res != IKapC.ITKSTATUS_OK)
                        return false;
                    
                    ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                    res = IKapC.ItkBufferGetInfo(hCurrBuffer, bufferInfo);
                    if (res != IKapC.ITKSTATUS_OK)
                        return false;
                    
                    uint bufferStatus = (uint)bufferInfo.State;
                    ulong nImageSize = bufferInfo.ValidImageSize;
                    
                    if ((bufferStatus == IKapC.ITKBUFFER_VAL_STATE_FULL || bufferStatus == IKapC.ITKBUFFER_VAL_STATE_UNCOMPLETED)
                        && nImageSize > 0 && nImageSize <= (ulong)m_nBufferSize
                        && m_pUserBuffer != IntPtr.Zero && m_pUserBuffer != new IntPtr(-1))
                    {
                        res = IKapC.ItkBufferRead(hCurrBuffer, 0, m_pUserBuffer, (uint)nImageSize);
                        if (res == IKapC.ITKSTATUS_OK)
                        {
                            lock (_frameLock)
                            {
                                using (var tempImage = new HImage("byte", m_nWidth, m_nHeight, m_pUserBuffer))
                                {
                                    Image = tempImage.CopyImage();
                                }
                                m_bUpdateImage = true;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CaptureImage()_错误：{ex.StackTrace}");
                return false;
            }
        }

        public override bool SetOutPut(int lineIndex, int time)
        {

            return true;
        }

        /// <summary> 相机设置</summary>
        public override void SetSetting()
        {
            //int nRet = 0;
            ////设置采集模式
            //SetTriggerMode(TrigMode);
            ////设置曝光时间
            //SetExposureTime(Config.ExposeTime);
            ////nRet = MyCamera.MV_CC_SetFloatValue_NET("ExposureTime", ExposeTime);
            //SetGain((long)Config.Gain);

            ////设置ip
            ////apiReturn = myApi.Gige_Camera_setIPAddress(m_Handle, uint.Parse(_UniqueLabel), uint.Parse(_DevDirExt));

        }

        public override void CameraChanged(ChangType changTyp)
        {
            try
            {

            }
            catch (Exception ex)
            {
                //Logger.AddLog("HikVision:" + ex.Message, eMsgType.Error);
            }
        }

        /// <summary>设置触发</summary>
        public override bool SetTriggerMode(eTrigMode mode)
        {
            if (!Connected)
            {
                Console.WriteLine("设置触发源失败,相机未连接");
                return false;
            }
            
            // GigE 线扫相机可能不支持 TriggerMode/TriggerSource 参数
            // 尝试设置，但忽略错误
            try
            {
                var res = IKapC.ItkDevFromString(m_hDev, "TriggerMode", "On");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkDevFromString(m_hDev, "TriggerSource", "Software");
                }
                return true; // 即使设置失败也返回 true，因为某些相机不支持这些参数
            }
            catch
            {
                return true;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 从相机中获取所有参数
        /// </summary>
        /// <returns></returns>
        public bool GetAllParam()
        {
            try
            {
                object temp = null ;
                //曝光
                if (GetSpecifiedParam("Double", "ExposureTime", ref temp))
                    Config.ExposeTime = (float)temp;
                //增益
                if (GetSpecifiedParam("Double", "Gain", ref temp))
                    Config.Gain = (float)temp;


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        /// <summary>
        /// 修改触发方式
        /// </summary>
        /// <returns></returns>
        public bool ModifyTriggerMode(string Mode)
        {
            try
            {
                switch(Mode)
                {
                    case "SoftTrigger":
                        {
                            g_bSoftTriggerUsed = true;
                            GeneralGrabWithGrabber.SetSoftTriggerWithGrabber(this);


                            return true;
                        }
                    case "Hard":
                        {
                            g_bSoftTriggerUsed = false;
                            GeneralGrabWithGrabber.SetSoftTriggerWithGrabber(this);


                            return true;
                        }
                    case "":
                        {


                            return true;
                        }


                    default:
                        {
                            Logs.LogWarning("修改触发方式失败，此条件未定义");
                            return false;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        /// <returns></returns>
        public override bool StartCollect()
        {
            try
            {
                // 检查相机是否已连接
                if (!Connected || !m_bIsOpened)
                {
                    Console.WriteLine("相机未连接，无法开始采集");
                    return false;
                }
                
                if (!GeneralGrabWithGrabber.StartGrabImage(this))
                {
                    Console.WriteLine("开始采集相机失败！");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <returns></returns>
        public override bool EndCollect()
        {
            try
            {
                // GigE/USB3 相机不使用采集卡
                bool useFrameGrabber = m_pBoard != IntPtr.Zero && m_pBoard != new IntPtr(-1);
                
                if (!useFrameGrabber)
                {
                    // 使用流方式停止
                    uint res = IKapC.ItkStreamStop(m_hStream);
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        Console.WriteLine($"ItkStreamStop 失败: 0x{res:X8}");
                        return false;
                    }
                    m_bGrabingImage = false;
                    return true;
                }
                 
                var ret = IKapBoard.IKapStopGrab(m_pBoard);
                if (!CheckIKapBoard(ret))
                {
                    Console.WriteLine("结束相机采集失败！");
                    return false;
                }
                m_bGrabingImage = false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        public override bool SoftTriggerOnce()
        {
            try
            {
                ModifyTriggerMode("SoftTrigger");
                uint res = IKapC.ItkDevExecuteCommand(m_hDev, "TriggerSoftware");
                if (!CheckIKapC(res))
                {
                    Console.WriteLine("Execure Command TriggerSoftware failed");
                    return false;
                }
                ModifyTriggerMode("Hard");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region Callback
        // 一帧图像完成回调
        public void IKapB_OnFrameReadyFunc(IntPtr pParam)
        {
            Console.WriteLine("进入回调");
            IntPtr hPtr = new IntPtr(-1);
            HImage grabbedImage = null;
            // 获取当前帧状态
            IKAPBUFFERSTATUS status = new IKAPBUFFERSTATUS();
            IKapBoard.IKapGetBufferStatus(m_pBoard, (int)m_nCurFrameIndex, status);
            if (status.uFull == 1)
            {
                IKapBoard.IKapGetBufferAddress(m_pBoard, (int)m_nCurFrameIndex, ref hPtr);

                if (Monitor.TryEnter(m_mutexImage))
                {
                    try
                    {
                        //DebugLog("m_mutexImage Monitor.TryEnter successed!");
                        lock (_frameLock)
                        {
                            MemoryHelper.CopyMemory(_frameBuffer.Header, hPtr, _frameBuffer.Length);

                            // 创建临时 HImage 引用缓冲区，然后立即复制数据
                            // 这样可以确保 Image 持有独立的数据副本，不会被下一帧覆盖
                            using (var tempImage = new HImage("byte", m_nWidth, m_nHeight, hPtr))
                            {
                                Image = tempImage.CopyImage();
                                grabbedImage = Image;
                            }
                        }
                        //DebugLog("CL OnFrameReadyFunc: finish buffer data copy!");
                        m_bUpdateImage = true;
                        Console.WriteLine($"[IKapBoard回调] 帧#{m_nCurFrameIndex} 已更新");
                    }
                    finally
                    {
                        Monitor.Exit(m_mutexImage);
                    }

                    //DebugLog("m_mutexImage Monitor.Exit!");
                }
            }

            EventWait.Set();
            if (grabbedImage != null)
            {
                ImageGrab?.Invoke(grabbedImage);
            }

            m_nCurFrameIndex++;
            m_nCurFrameIndex = m_nCurFrameIndex % m_nFrameCount;
        }
        #endregion
    }
}

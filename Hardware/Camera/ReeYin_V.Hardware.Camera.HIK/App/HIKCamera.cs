using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using MvCamCtrl.NET;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Models;
using System.IO;
using System.Runtime.Serialization;
using HalconDotNet;
using System.Diagnostics;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.UI;

namespace ReeYin_V.Hardware.Camera.HIK
{
    [Category("相机")]
    [DisplayName("海康相机")]
    [Serializable]
    public partial class HIKCamera : CameraBase
    {
        #region Fields
        [NonSerialized]
        private MyCamera MyCamera = new MyCamera();
        [NonSerialized]
        public MyCamera.MV_CC_DEVICE_INFO CurDevice;
        [NonSerialized]
        private MyCamera.cbOutputExdelegate ImageCallback;
        //public object ExtInfo
        //{
        //    set { CurDevice = (MyCamera.MV_CC_DEVICE_INFO)value; }
        //    get { return CurDevice; }
        //}
        // ch:用于保存图像的缓存 | en:Buffer for saving image
        private UInt32 m_nBufSizeForSaveImage = 5120 * 5120 * 3 + 2048;
        [NonSerialized]
        private byte[] m_pBufForSaveImage = new byte[5120 * 5120 * 3 + 2048];

        // 线扫相关字段
        [NonSerialized]
        private MyCamera.cbOutputExdelegate LineScanCallback;
        /// <summary>线扫行数据缓冲区，持续积累每次回调的像素数据</summary>
        [NonSerialized]
        private List<byte[]> _lineScanRowBuffer = new List<byte[]>();
        [NonSerialized]
        private object _lineScanLock = new object();
        [NonSerialized]
        private int _lineScanFrameWidth = 0;
        [NonSerialized]
        private int _lineScanBytesPerPixel = 1;
        [NonSerialized]
        private bool _lineScanIsMono = true;
        /// <summary>当前已累积的总行数</summary>
        [NonSerialized]
        private int _lineScanAccumulatedLines = 0;
        #endregion

        #region Constructor
        public HIKCamera() : base() { }
        #endregion

        #region Methods
        public void GetSetting()
        {
            int nRet = 0;
            MyCamera.MVCC_ENUMVALUE stTriggerMode = new MyCamera.MVCC_ENUMVALUE();
            MyCamera.MVCC_ENUMVALUE stTriggerSource = new MyCamera.MVCC_ENUMVALUE();
            MyCamera.MVCC_ENUMVALUE stTriggerActivation = new MyCamera.MVCC_ENUMVALUE();
            nRet = MyCamera.MV_CC_GetTriggerMode_NET(ref stTriggerMode);
            nRet = MyCamera.MV_CC_GetTriggerSource_NET(ref stTriggerSource);
            if (stTriggerMode.nCurValue == (uint)eTrigMode.内触发)
                TrigMode = eTrigMode.内触发;
            else if (stTriggerSource.nCurValue == 7)//软触发
                TrigMode = eTrigMode.软触发;
            else if (stTriggerSource.nCurValue == 0) //Line0 触发
            {
                nRet += MyCamera.MV_CC_GetEnumValue_NET("TriggerActivation", ref stTriggerActivation);
                if (stTriggerActivation.nCurValue == 0)
                    TrigMode = eTrigMode.上升沿;
                else
                    TrigMode = eTrigMode.下降沿;
            }

            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            nRet = MyCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            Config.ExposeTime = stParam.fCurValue;
            MyCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
            Framerate = stParam.fCurValue.ToString();
            //base.GetSetting();
        }

        /// <summary>设置宽度</summary>
        public void SetWidth()
        {
            if (Width > 100 & Width <= WidthMax)
            {

            }
        }
        /// <summary>设置高度</summary>
        public void SetHeight()
        {
            if (Height > 100 & Height <= HeightMax)
            {

            }
        }

        public void FindCBySN(string Ctemp)
        {
            MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int nRet;
            // ch:创建设备列表 en:Create Device List
            System.GC.Collect();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (0 != nRet)
            {
                MessageView.Ins.MessageBoxShow("没有找到任何设备,请确认相机是否连接好!", eMsgType.Warn);
                return;
            }

            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (Ctemp == gigeInfo.chSerialNumber)//判断是否等于指定相机序号
                    {
                        CurDevice = device;
                        ExtInfo = device;
                        return;
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (Ctemp == usbInfo.chSerialNumber)//判断是否等于指定相机序号
                        if (Ctemp == usbInfo.chSerialNumber)//判断是否等于指定相机序号
                        {
                            CurDevice = device;
                            ExtInfo = device;
                            return;
                        }

                }
            }
            MessageView.Ins.MessageBoxShow("没有找当前到设备,请确认相机是否连接好!", eMsgType.Warn);
            return;
        }

        //[OnSerializing()] 序列化之前
        //[OnSerialized()] 序列化之后
        //[OnDeserializing()] 反序列化之前
        [OnDeserialized()] //反序列化之后
        internal void OnDeserializedMethod(StreamingContext context)
        {
            MyCamera = new MyCamera();
            _lineScanRowBuffer = new List<byte[]>();
            _lineScanLock = new object();
            _lineScanAccumulatedLines = 0;
            if (SerialNo == null || SerialNo == "")
            {
                return;
            }
            m_pBufForSaveImage = new byte[5120 * 5120 * 3 + 2048];
            FindCBySN(SerialNo);
            ConnectDev();
        }
        /// <summary>
        /// 通过查询相机 DeviceScanType 参数自动检测是否为线扫相机。
        /// 检测结果写入 IsLineScan（0=面阵，1=线扫）。
        /// </summary>
        private void DetectIsLineScan()
        {
            MyCamera.MVCC_ENUMVALUE stScanType = new MyCamera.MVCC_ENUMVALUE();
            int nRet = MyCamera.MV_CC_GetEnumValue_NET("DeviceScanType", ref stScanType);
            if (nRet == MyCamera.MV_OK)
            {
                // 0 = Areascan（面阵），1 = Linescan（线扫）
                IsLineScan = stScanType.nCurValue == 1;
            }
        }

        /// <summary>
        /// 获取线扫图像。调用时将当前 Buffer 中累积的所有行数据拼合为一张完整图片返回，并清空 Buffer。
        /// </summary>
        /// <returns>由已累积行数据拼合而成的 HImage；Buffer 为空时返回空 HImage。</returns>
        public HImage GrabLineScanImage()
        {
            // 换桶：锁内只交换引用，耗时的内存分配和数据拷贝全部移到锁外
            List<byte[]> snapshot;
            int width;
            int bytesPerPixel;
            bool isMono;

            lock (_lineScanLock)
            {
                if (_lineScanRowBuffer.Count == 0 || _lineScanFrameWidth == 0)
                    return new HImage();

                snapshot = _lineScanRowBuffer;
                _lineScanRowBuffer = new List<byte[]>();

                width = _lineScanFrameWidth;
                bytesPerPixel = _lineScanBytesPerPixel;
                isMono = _lineScanIsMono;
                _lineScanFrameWidth = 0;
            }
            // 锁已释放，回调可以继续追加新行——以下耗时操作不再阻塞采集

            int totalLines = 0;
            foreach (var chunk in snapshot)
                totalLines += chunk.Length / (bytesPerPixel * width);

            byte[] imageData = new byte[width * totalLines * bytesPerPixel];
            int offset = 0;
            foreach (var chunk in snapshot)
            {
                Buffer.BlockCopy(chunk, 0, imageData, offset, chunk.Length);
                offset += chunk.Length;
            }

            GCHandle handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
            try
            {
                IntPtr pImage = handle.AddrOfPinnedObject();
                HImage hImage = new HImage();
                if (isMono)
                    hImage = new HImage("byte", width, totalLines, pImage);
                else
                    hImage.GenImageInterleaved(pImage, "rgb", width, totalLines, -1, "byte", 0, 0, 0, 0, -1, 0);
                return hImage;
            }
            finally
            {
                handle.Free();
            }
        }

        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }
            //Logger.AddLog("HikVision:" + errorMsg, eMsgType.Error);
        }
        #endregion

        #region Event
        /// <summary>采集回调</summary>
        private void ImageCallbackFunc(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            try
            {
                MyCamera.MvGvspPixelType enDstPixelType;
                if (IsMonoData(pFrameInfo.enPixelType))
                {
                    enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                }
                else if (IsColorData(pFrameInfo.enPixelType))
                {
                    enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                }
                else
                {
                    Trace.TraceError("{0} GrabImage Fail!ex: No such pixel type!");
                    return;
                }
                if (m_pBufForSaveImage == null)
                {
                    m_pBufForSaveImage = new byte[5120 * 5120 * 3 + 2048];
                }

                IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);

                MyCamera.MV_PIXEL_CONVERT_PARAM stConverPixelParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();
                stConverPixelParam.nWidth = pFrameInfo.nWidth;
                stConverPixelParam.nHeight = pFrameInfo.nHeight;
                stConverPixelParam.pSrcData = pData;
                stConverPixelParam.nSrcDataLen = pFrameInfo.nFrameLen;
                stConverPixelParam.enSrcPixelType = pFrameInfo.enPixelType;
                stConverPixelParam.enDstPixelType = enDstPixelType;
                stConverPixelParam.pDstBuffer = pImage;
                stConverPixelParam.nDstBufferSize = m_nBufSizeForSaveImage;
                int nRet = MyCamera.MV_CC_ConvertPixelType_NET(ref stConverPixelParam);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }


                HImage hImage = new HImage();
                if (enDstPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                {
                    //************************Mono8 转 HImage*******************************
                    try
                    {
                        hImage = new HImage("byte", pFrameInfo.nWidth, pFrameInfo.nHeight, pImage);
                    }
                    catch (Exception ex)
                    {

                    }


                }
                else
                {
                    //*********************RGB8 转 Bitmap**************************
                    try
                    {

                        hImage.GenImageInterleaved(pImage, "rgb", pFrameInfo.nWidth, pFrameInfo.nHeight, -1, "byte", 0, 0, 0, 0, -1, 0);

                    }

                    catch (Exception ex)
                    {
                        Trace.TraceError("{0} GrabImage Fail!ex:{1}", ex);
                    }

                }

                Image = hImage;
                EventWait.Set();
                ImageGrab?.Invoke(Image);
            }
            catch (Exception ex)
            {
                EventWait.Set();
            }
        }
        private Boolean IsMonoData(MyCamera.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;

                default:
                    return false;
            }
        }

        private Boolean IsColorData(MyCamera.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YCBCR411_8_CBYYCRYY:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 线扫采集回调。每次触发将一帧（一行或多行）像素数据转换后追加到 _lineScanRowBuffer。
        /// </summary>
        private void LineScanCallbackFunc(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            try
            {
                MyCamera.MvGvspPixelType enDstPixelType;
                bool isMono;
                if (IsMonoData(pFrameInfo.enPixelType))
                {
                    enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                    isMono = true;
                }
                else if (IsColorData(pFrameInfo.enPixelType))
                {
                    enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                    isMono = false;
                }
                else
                {
                    return;
                }

                int bytesPerPixel = isMono ? 1 : 3;
                int chunkSize = pFrameInfo.nWidth * pFrameInfo.nHeight * bytesPerPixel;
                byte[] chunkData = new byte[chunkSize];

                GCHandle handle = GCHandle.Alloc(chunkData, GCHandleType.Pinned);
                try
                {
                    MyCamera.MV_PIXEL_CONVERT_PARAM stConvert = new MyCamera.MV_PIXEL_CONVERT_PARAM();
                    stConvert.nWidth = pFrameInfo.nWidth;
                    stConvert.nHeight = pFrameInfo.nHeight;
                    stConvert.pSrcData = pData;
                    stConvert.nSrcDataLen = pFrameInfo.nFrameLen;
                    stConvert.enSrcPixelType = pFrameInfo.enPixelType;
                    stConvert.enDstPixelType = enDstPixelType;
                    stConvert.pDstBuffer = handle.AddrOfPinnedObject();
                    stConvert.nDstBufferSize = (uint)chunkSize;
                    if (MyCamera.MV_CC_ConvertPixelType_NET(ref stConvert) != MyCamera.MV_OK)
                        return;
                }
                finally
                {
                    handle.Free();
                }

                bool shouldAssemble = false;
                lock (_lineScanLock)
                {
                    if (_lineScanFrameWidth == 0)
                    {
                        _lineScanFrameWidth = pFrameInfo.nWidth;
                        _lineScanBytesPerPixel = bytesPerPixel;
                        _lineScanIsMono = isMono;
                    }
                    _lineScanRowBuffer.Add(chunkData);
                    _lineScanAccumulatedLines += pFrameInfo.nHeight;

                    // 读取拼帧行数阈值（从 Config 或默认 6000）
                    int frameHeight = Config?.LineScanFrameHeight ?? 4000;
                    if (_lineScanAccumulatedLines >= frameHeight)
                    {
                        shouldAssemble = true;
                    }
                }

                // 在锁外执行耗时的拼帧和回调，不阻塞采集
                if (shouldAssemble)
                {
                    try
                    {
                        HImage hImage = GrabLineScanImage();
                        // GrabLineScanImage 内部已清空 buffer 并重置 _lineScanFrameWidth
                        lock (_lineScanLock)
                        {
                            _lineScanAccumulatedLines = 0;
                        }

                        if (hImage != null && hImage.IsInitialized())
                        {
                            Image = hImage;
                            EventWait.Set();
                            ImageGrab?.Invoke(Image);
                        }
                    }
                    catch (Exception assembleEx)
                    {
                        Trace.TraceError("LineScan assemble error: {0}", assembleEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("LineScanCallbackFunc error: {0}", ex.Message);
            }
        }

        /// <summary>断开连接时触发</summary>
        private void OnConnectionLost(object sender, EventArgs e)
        {
            // Close the MyCamera object.
            DisConnectDev();
        }
        #endregion


        #region Assist
        //public HImage ConverterImage()
        //{
        //    HImage hImage = new HImage();
        //    if (enDstPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
        //    {
        //        //************************Mono8 转 HImage*******************************
        //        try
        //        {
        //            hImage = new HImage("byte", pFrameInfo.nWidth, pFrameInfo.nHeight, pImage);
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //    else
        //    {
        //        //*********************RGB8 转 Bitmap**************************
        //        try
        //        {
        //            hImage.GenImageInterleaved(pImage, "rgb", pFrameInfo.nWidth, pFrameInfo.nHeight, -1, "byte", 0, 0, 0, 0, -1, 0);
        //        }
        //        catch (Exception ex)
        //        {
        //            Trace.TraceError("{0} GrabImage Fail!ex:{1}", ex);
        //        }
        //    }
        //}
        #endregion

    }
}

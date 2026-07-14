using MvCamCtrl.NET;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Models;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.Camera.HIK
{
    public partial class HIKCamera : CameraBase
    {
        #region Overriden Methods
        /// <summary>搜索相机</summary>
        public override List<CameraInfoModel> SearchCameras()
        {
            List<CameraInfoModel> mCamInfoList = new List<CameraInfoModel>();
            MyCamera.MV_CC_DEVICE_INFO_LIST mDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            if (MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref mDeviceList) != 0)
            {
                MessageView.Ins.MessageBoxShow("查找设备失败", eMsgType.Warn);
                return mCamInfoList;
            }
            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < mDeviceList.nDeviceNum; i++)
            {
                CameraInfoModel _camInfo = new CameraInfoModel();
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(mDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        _camInfo.CamName = "HikVision: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")";
                    }
                    else
                    {
                        _camInfo.CamName = "HikVision: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")";
                    }
                    _camInfo.SerialNO = gigeInfo.chSerialNumber;
                    _camInfo.MaskName = gigeInfo.chSerialNumber;
                    _camInfo.ExtInfo = device;
                    ExtInfo = device;
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        _camInfo.CamName = "HikVision: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")";
                    }
                    else
                    {
                        _camInfo.CamName = ("HikVision: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }

                    _camInfo.SerialNO = usbInfo.chSerialNumber;
                    _camInfo.MaskName = usbInfo.chSerialNumber;
                    _camInfo.ExtInfo = device;
                    ExtInfo = device;
                }

                mCamInfoList.Add(_camInfo);
            }
            return mCamInfoList;
        }

        /// <summary>连接相机</summary>
        public override void ConnectDev()
        {
            try
            {
                base.ConnectDev();
                // 如果设备已经连接先断开
                DisConnectDev();
                int nRet = -1;
                // ch:打开设备 | en:Open device
                if (MyCamera == null)
                {
                    MyCamera = new MyCamera();
                    if (null == MyCamera)
                    {
                        return;
                    }
                }
                if (null == ExtInfo) { return; }

                var curDevice = (MyCamera.MV_CC_DEVICE_INFO)ExtInfo;
                nRet = MyCamera.MV_CC_CreateDevice_NET(ref curDevice);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Device open fail!", nRet);
                    return;
                }
                nRet = MyCamera.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    MyCamera.MV_CC_DestroyDevice_NET();
                    ShowErrorMsg("Device open fail!", nRet);
                    //Logger.AddLog("Device open fail!" + nRet.ToString(), eMsgType.Error);
                    return;
                }
                SetSetting();
                // 检测是面阵相机还是线扫相机，并注册对应回调
                DetectIsLineScan();
                // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
                //MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);// ch:工作在连续模式 | en:Acquisition On Continuous Mode
                // ch:注册回调函数 | en:Register image callback
                if (IsLineScan)
                {
                    LineScanCallback = new MyCamera.cbOutputExdelegate(LineScanCallbackFunc);
                    nRet = MyCamera.MV_CC_RegisterImageCallBackEx_NET(LineScanCallback, IntPtr.Zero);
                }
                else
                {
                    ImageCallback = new MyCamera.cbOutputExdelegate(ImageCallbackFunc);
                    nRet = MyCamera.MV_CC_RegisterImageCallBackEx_NET(ImageCallback, IntPtr.Zero);
                }
                if (MyCamera.MV_OK != nRet)
                {
                    Console.WriteLine("Register image callback failed!");
                }
                // ch:开启抓图 || en: start grab image
                nRet = MyCamera.MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    Console.WriteLine("Start grabbing failed:{0:x8}", nRet);
                }
                Connected = true;
            }
            catch (Exception ex)
            {
                Connected = false;
            }
            base.ConnectDev();
        }
        /// <summary>断开相机</summary>
        public override void DisConnectDev()
        {
            if (Connected)
            {
                int nRet = MyCamera.MV_CC_CloseDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }
                nRet = MyCamera.MV_CC_DestroyDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }
                Connected = false;
            }
            base.DisConnectDev();
        }
        /// <summary>采集图像,是否手动采图</summary>
        public override bool CaptureImage(bool byHand)
        {
            try
            {
                int nRet = 0;
                if (byHand)
                {
                    //获取触发模式和触发源
                    //MyCamera.MVCC_ENUMVALUE stTriggerMode = new MyCamera.MVCC_ENUMVALUE();
                    //MyCamera.MVCC_ENUMVALUE stTriggerSource = new MyCamera.MVCC_ENUMVALUE();
                    //nRet = MyCamera.MV_CC_GetTriggerMode_NET(ref stTriggerMode);
                    //nRet = MyCamera.MV_CC_GetTriggerSource_NET(ref stTriggerSource);
                    eTrigMode temp = TrigMode;
                    //设置内触发
                    SetTriggerMode(eTrigMode.软触发);
                    nRet = MyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
                    //恢复旧模式
                    SetTriggerMode(temp);

                }
                else
                {
                    if (TrigMode == eTrigMode.软触发)
                    {
                        //SignalWait.Reset();
                        //SignalWait.WaitOne();
                        nRet = MyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
                    }
                }

                if (nRet != MyCamera.MV_OK)
                {
                    ShowErrorMsg("Set CaptureImage Time Fail!", nRet);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                //Logger.GetExceptionMsg(ex);
                return false;
            }
        }
        public override bool SetOutPut(int lineIndex, int time)
        {
            ////Strobe输出
            //nRet = MV_CC_SetEnumValue(handle, "LineSelector", 1);
            ////0:Line0 1:Line1 2:Line2 
            //nRet = MV_CC_SetEnumValue(handle, "LineMode", 8);//仅LineSelector为line2时需要特意设置，其他输出不需要
            //                                                 //0:Input 1:Output 8:Strobe 
            //int DurationValue = 0, DelayValue = 0, PreDelayValue = 0;//us
            //nRet = MV_CC_SetIntValue(handle, "StrobeLineDuration", DurationValue);
            ////strobe持续时间，设置为0，持续时间就是曝光时间，设置其他值，就是其他值时间
            //nRet = MV_CC_SetIntValue(handle, "StrobeLineDelay", DelayValue);//strobe延时，从曝光开始，延时多久输出
            //nRet = MV_CC_SetIntValue(handle, "StrobeLinePreDelay", PreDelayValue);//strobe提前输出，曝光延后开始
            //                                                                      //--------------------------------------------------------------------------------------------------
            //nRet = MV_CC_SetBoolValue(handle, "StrobeEnable", TRUE);/
            int nRet = 0;
            if (lineIndex != 1 && lineIndex != 2)
                return false;
            nRet += MyCamera.MV_CC_SetEnumValue_NET("LineSelector", (uint)lineIndex);//1:Line1 2:Line2 
            //if (lineIndex == 2)
            //    nRet += MyCamera.MV_CC_SetEnumValue_NET("LineMode", 8);//0:Input 1:Output 8:Strobe //仅LineSelector为line2时需要特意设置，其他输出不需要
            nRet += MyCamera.MV_CC_SetIntValue_NET("StrobeLineDuration", (uint)time); //strobe持续时间，设置为0，持续时间就是曝光时间，设置其他值，就是其他值时间
            //nRet += MyCamera.MV_CC_SetBoolValue_NET("StrobeEnable", true); //Strobe使能
            nRet += MyCamera.MV_CC_SetCommandValue_NET("LineTriggerSoftware");//触发输出
            if (nRet != MyCamera.MV_OK)
            {
                //ShowErrorMsg("Set CaptureImage Time Fail!", nRet);
                return false;
            }
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
            ////置帧率
            //nRet = MyCamera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(Framerate));
            //设置ip
            //apiReturn = myApi.Gige_Camera_setIPAddress(m_Handle, uint.Parse(_UniqueLabel), uint.Parse(_DevDirExt));

        }

        public override void CameraChanged(ChangType changTyp)
        {
            try
            {
                switch (changTyp)
                {
                    //case ChangType.增益:
                    //    SetGain((long)Config.Gain);
                    //    break;
                    //case ChangType.曝光:
                    //    SetExposureTime((long)Config.ExposeTime);
                    //    break;
                    case ChangType.宽度:
                        SetWidth();
                        break;
                    case ChangType.高度:
                        SetHeight();
                        break;
                    case ChangType.触发:
                        SetTriggerMode(TrigMode);
                        break;
                }
            }
            catch (Exception ex)
            {
                //Logger.AddLog("HikVision:" + ex.Message, eMsgType.Error);
            }
        }


        public override bool SetSpecifiedParam(string type,string key, object value)
        {
            try
            {
                switch (type)
                {
                    case "Int":
                        {

                        }
                        break;
                    case "Enum":
                        {

                        }
                        break;
                    case "Float":
                        {
                            int nRet = MyCamera.MV_CC_SetFloatValue_NET(key, (float)value);
                            if (nRet != MyCamera.MV_OK)
                            {
                                return false;
                            }
                        }
                        break;

                    default:
                        {


                        }break;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
                return false;
            }
        }

        public override bool GetSpecifiedParam(string type, string key, ref object value)
        {
            try
            {
                switch (type)
                {
                    case "Float":
                        {
                            MyCamera.MVCC_FLOATVALUE stFloatValue = new MyCamera.MVCC_FLOATVALUE();
                            int nRet = MyCamera.MV_CC_GetFloatValue_NET(key, ref stFloatValue);
                            if (nRet != MyCamera.MV_OK) return false;
                            value = stFloatValue.fCurValue;
                        }
                        break;
                    case "Int":
                        {
                            MyCamera.MVCC_INTVALUE_EX stIntValue = new MyCamera.MVCC_INTVALUE_EX();
                            int nRet = MyCamera.MV_CC_GetIntValueEx_NET(key, ref stIntValue);
                            if (nRet != MyCamera.MV_OK) return false;
                            value = stIntValue.nCurValue;
                        }
                        break;
                    default:
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

       
        /// <summary>设置触发</summary>
        public override bool SetTriggerMode(eTrigMode mode)
        {
            int nRet = 0;
            if (mode == eTrigMode.内触发)
                nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 1); //之前是这个设置，不知道內触发是什么玩意，暂且设置为软触发nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0);
            else
                nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 1);
            // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
            //           1 - Line1;
            //           2 - Line2;
            //           3 - Line3;
            //           4 - Counter;
            //           7 - Software;
            switch (mode)
            {
                case eTrigMode.内触发:   // no acquisition                    
                    break;
                case eTrigMode.软触发:   // freerunning
                    {
                        nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 7);
                        break;
                    }
                case eTrigMode.上升沿:   // Software trigger
                    {
                        nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 0);
                        nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerActivation", 0);
                        break;
                    }
                case eTrigMode.下降沿:   // Software trigger
                    {
                        nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 0);
                        nRet += MyCamera.MV_CC_SetEnumValue_NET("TriggerActivation", 1);
                        break;
                    }
            }
            if (nRet != MyCamera.MV_OK)
                return false;
            else
                return true;
        }
        #endregion

    }
}

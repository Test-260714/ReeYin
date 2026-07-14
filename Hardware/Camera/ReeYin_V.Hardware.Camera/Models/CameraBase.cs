using HalconDotNet;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Image;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ReeYin_V.Hardware.Camera.Models
{
    public delegate void ImageGrabcallback(HImage img);
    [Serializable]
    public class CameraBase : BindableBase,ICamera
    {
        #region 属性
        /// <summary>
        /// 回调事件
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public ImageGrabcallback ImageGrab = null;
        
        /// <summary>
        /// 采集图像
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public HImage Image = new HImage();

        /// <summary>
        /// 回调数据
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public UnmanagedArray2D<byte> _frameBuffer;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public readonly object _frameLock = new object();

        /// <summary>
        /// 采集信号
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public AutoResetEvent EventWait = new AutoResetEvent(false);
        
        /// <summary>
        /// 软触发时收到图像信号-同步
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public AutoResetEvent SignalWait = new AutoResetEvent(false);
        
        /// <summary>
        /// 软触发时收到图像信号-异步
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public AutoResetEvent GetSignalWait = new AutoResetEvent(false);
       
        /// <summary>
        /// 扩展信息
        /// </summary>
        [NonSerialized]
        [JsonIgnore]
        public object ExtInfo;
        
        /// <summary>
        /// 触发模式
        /// </summary>
        [NonSerialized]
        private eTrigMode _TrigMode = eTrigMode.软触发;

        public eTrigMode TrigMode
        {
            get { return _TrigMode; }
            set { SetProperty(ref _TrigMode, value); }
        }
        [JsonIgnore]
        public Array TrigModes { set; get; } = Enum.GetValues(typeof(eTrigMode));

        /// <summary>
        /// 最新编号
        /// </summary>
        [NonSerialized]
        public static int LastNo = 0;

        [NonSerialized]
        private string _CameraNo = "未选中";
        /// <summary>
        /// 设备自己编号
        /// </summary>
        public string CameraNo
        {
            get { return _CameraNo; }
            set { SetProperty(ref _CameraNo, value); }
        }

        /// <summary>
        /// 设备内部编号
        /// </summary>
        public string SerialNo { set; get; }

        /// <summary>
        /// 相机类型
        /// </summary>
        public string CameraType { set; get; }

        /// <summary>
        /// 是否为线扫相机
        /// </summary>
        public bool IsLineScan { get; set; } = false;

        /// <summary>
        /// 设备内部IP
        /// </summary>
        public string CameraIP { set; get; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }


        [NonSerialized]
        private bool _Connected = false;

        /// <summary>
        /// 初始连接状态
        /// </summary>
        [JsonIgnore]
        public bool Connected
        {
            get { return _Connected; }
            set { SetProperty(ref _Connected, value, new Action(() => PrismProvider.EventAggregator.GetEvent<HardwareChangedEvent>().Publish())); }
        }

        /// <summary>
        /// 最大宽度
        /// </summary>
        public int WidthMax { set; get; } = 0;

        /// <summary>
        /// 最大高度
        /// </summary>
        public int HeightMax { set; get; } = 0;

        [NonSerialized]
        private ConfigModel _config;

        public ConfigModel Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new ConfigModel();
                }
                return _config;
            }
            set { _config = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width { set; get; } = 0;

        /// <summary>
        /// 高度
        /// </summary>
        public int Height { set; get; } = 0;

        /// <summary>
        /// 帧率
        /// </summary>
        public string Framerate { set; get; } = "0";
        #endregion

        #region 构造函数
        /// <summary>
        /// 创建相机实体
        /// </summary>
        public CameraBase()
        {
            PrismProvider.EventAggregator.GetEvent<SoftwareExitEvent>().Subscribe(DisConnectDev);
        }
        public CameraBase(string _SerialNo)
        {
            LastNo++;
            CameraNo = "Dev" + LastNo;
        }
        #endregion

        #region 虚函数
        /// <summary>
        /// 搜索相机
        /// </summary>
        /// <returns></returns>
        public virtual List<CameraInfoModel> SearchCameras() { return null; }

        /// <summary>
        /// 建立连接
        /// </summary>
        public virtual void ConnectDev()
        {
            PrismProvider.EventAggregator.GetEvent<HardwareChangedEvent>().Publish();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public virtual void DisConnectDev()
        {
            PrismProvider.EventAggregator.GetEvent<HardwareChangedEvent>().Publish();

        }

        /// <summary>
        /// 设置指定参数值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool SetSpecifiedParam(string type, string key, object value)
        {
            return false;
        }

        /// <summary>
        /// 获取指定参数值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool GetSpecifiedParam(string type, string key,ref object value)
        {
            return false;
        }

        /// <summary>
        /// 抓捕图像
        /// <param name="byHand">是否手动采图</param>
        /// <returns></returns>
        public virtual bool CaptureImage(bool byHand) { return true; }


        public virtual bool SetOutPut(int index, int time) { return true; }

        /// <summary>
        /// 相机设置
        /// </summary>
        public virtual void SetSetting() { }
        /// <summary>
        /// 设置触发模式
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public virtual bool SetTriggerMode(eTrigMode mode)
        {
            return true;
        }
        /// <summary>
        /// 参数设置
        /// </summary>
        /// <param name="changTyp"></param>
        public virtual void CameraChanged(ChangType changTyp) { }

        /// <summary>
        /// 软触发一次
        /// </summary>
        /// <returns></returns>
        public virtual bool StartCollect()
        {
            return false; 
        }

        /// <summary>
        /// 结束采集
        /// </summary>
        /// <returns></returns>
        public virtual bool EndCollect()
        {
            return false; 
        }

        /// <summary>
        /// 软触发一次
        /// </summary>
        /// <returns></returns>
        public virtual bool SoftTriggerOnce()
        {
            return false;
        }

        /// <summary>
        /// 设置线扫相机行频
        /// </summary>
        /// <param name="lineRate">行频（Hz）</param>
        /// <returns></returns>
        public virtual bool SetLineRate(float lineRate)
        {
            return SetSpecifiedParam("Float", "AcquisitionLineRate", lineRate);
        }

        /// <summary>
        /// 获取线扫相机行频
        /// </summary>
        /// <returns>行频（Hz），失败返回0</returns>
        public virtual float GetLineRate()
        {
            object value = 0f;
            if (GetSpecifiedParam("Float", "AcquisitionLineRate", ref value))
                return Convert.ToSingle(value);
            return 0f;
        }
        #endregion
        [OnDeserializing()]
        internal void OnDeSerializingMethod(StreamingContext context)
        {
            Image = new HImage();
            PrismProvider.EventAggregator.GetEvent<SoftwareExitEvent>().Subscribe(DisConnectDev);
            SignalWait = new AutoResetEvent(false);//采集信号
            GetSignalWait = new AutoResetEvent(false);//软触收到图像信号
            EventWait = new AutoResetEvent(false);
        }
    }
}

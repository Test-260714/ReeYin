using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController.Models
{
    /// <summary>
    /// 光源控制器基类
    /// </summary>
    public class LightControllerBase : BindableBase, ILightController
    {
        #region Properties
        public bool IsEnabled { get; set; }

        [JsonIgnore]
        private string _ip = "192.168.1.100";
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP
        {
            get { return _ip; }
            set { _ip = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _port = 5000;
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _comPort = 1;
        /// <summary>
        /// 串口号
        /// </summary>
        public int ComPort
        {
            get { return _comPort; }
            set { _comPort = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isConnected;
        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _nickName = "";
        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName
        {
            get { return _nickName; }
            set { _nickName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _venderName = "";
        /// <summary>
        /// 厂家标识
        /// </summary>
        public string VenderName
        {
            get { return _venderName; }
            set { _venderName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _venderType = "";
        /// <summary>
        /// 厂家类型
        /// </summary>
        public string VenderType
        {
            get { return _venderType; }
            set { _venderType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _channelCount = 4;
        /// <summary>
        /// 通道数量
        /// </summary>
        public int ChannelCount
        {
            get { return _channelCount; }
            set { _channelCount = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _connectionType = 0;
        /// <summary>
        /// 连接类型 0:网口 1:串口
        /// </summary>
        public int ConnectionType
        {
            get { return _connectionType; }
            set { _connectionType = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Methods
        public virtual bool Init()
        {
            return false;
        }

        public virtual void Close()
        {
            return;
        }

        public virtual bool SetBrightness(int channelIndex, int value)
        {
            throw new NotImplementedException();
        }

        public virtual int GetBrightness(int channelIndex)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetMultiBrightness(Dictionary<int, int> channelValues)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetChannelOnOff(int channelIndex, bool isOn)
        {
            throw new NotImplementedException();
        }

        public virtual bool GetChannelOnOff(int channelIndex)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetStrobeTime(int channelIndex, int strobeTime)
        {
            throw new NotImplementedException();
        }

        public virtual int GetStrobeTime(int channelIndex)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetTriggerMode(int mode)
        {
            throw new NotImplementedException();
        }

        public virtual int GetTriggerMode()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

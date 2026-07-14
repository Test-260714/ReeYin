using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ReeYin.Hardware.Sensor.Models
{
    public class SensorBase : BindableBase, ISensor
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]

        public bool IsEnabled { get ; set; }

        [JsonIgnore]
        private string _ip = "192.168.1.188";
        /// <summary>
        /// 网口
        /// </summary>
        public string IP
        {
            get { return _ip; }
            set { _ip = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort _port = 58080;
        /// <summary>
        /// 端口
        /// </summary>
        public ushort Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isConnected;
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
        /// 厂家
        /// </summary>
        public string VenderName
        {
            get { return _venderName; }
            set { _venderName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _venderType = "";
        /// <summary>
        /// 厂家定义类型
        /// </summary>
        public string VenderType
        {
            get { return _venderType; }
            set { _venderType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HardwareState _state;
        [JsonIgnore]
        public HardwareState State
        {
            get { return _state; }
            set 
            { 
                _state = value; RaisePropertyChanged();
                PrismProvider.EventAggregator.GetEvent<HardwareStatusChangedEvent>().Publish(new HardwareStatus
                {
                    Name = _nickName,
                    Status = _state,
                    IsConnect = _isConnected,
                    Describe = "",
                    SourceType = HardwareAlarmSources.Sensor,
                    Location = string.IsNullOrWhiteSpace(IP) ? NickName : IP,
                    ExtraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["IsConnect"] = _isConnected,
                        ["NickName"] = NickName,
                        ["IP"] = IP
                    },
                    Timestamp = DateTime.Now
                });
            }
        }
        #endregion

        #region Methods
        public virtual bool Init()
        {
            State = HardwareState.Initializing;
            return false;
        }

        public virtual void Close()
        {
            State = HardwareState.Closed;
            return;
        }

        public virtual void StartCollect()
        {
            State = HardwareState.Running;

        }

        public virtual void StopCollect()
        {
            State = HardwareState.Complete;

        }

        public virtual List<MeasureData> ReceiveSensorData()
        {

            return new List<MeasureData>();
        }

        /// <summary>
        /// 设定参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool SettingParam(string key, object value)
        {
            return false;
        }

        public virtual bool SaveConfig()
        {
            return false;
        }
        #endregion


    }
}

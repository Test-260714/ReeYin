using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// PLC点位配置
    /// </summary>
    [Serializable]
    public class PLCPointConfig : BindableBase
    {
        private string _address = "";
        public string Address
        {
            get { return _address; }
            set { _address = value; RaisePropertyChanged(); }
        }

        private EnumParaInfoModelParaType _dataType = EnumParaInfoModelParaType.Bool;
        public EnumParaInfoModelParaType DataType
        {
            get { return _dataType; }
            set { _dataType = value; RaisePropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 带触发策略的命令点位
    /// </summary>
    [Serializable]
    public class PLCCommandPointConfig : PLCPointConfig
    {
        private string _activeValue = "True";
        public string ActiveValue
        {
            get { return _activeValue; }
            set { _activeValue = value; RaisePropertyChanged(); }
        }

        private string _clearValue = "False";
        public string ClearValue
        {
            get { return _clearValue; }
            set { _clearValue = value; RaisePropertyChanged(); }
        }

        private bool _autoClear = true;
        public bool AutoClear
        {
            get { return _autoClear; }
            set { _autoClear = value; RaisePropertyChanged(); }
        }

        private int _pulseMs = 150;
        public int PulseMs
        {
            get { return _pulseMs; }
            set
            {
                _pulseMs = Math.Clamp(value, 10, 60000);
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// 速度档配置
    /// </summary>
    [Serializable]
    public class PLCSpeedProfile : BindableBase
    {
        private string _name = "低速";
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private double _runSpeed = 10;
        public double RunSpeed
        {
            get { return _runSpeed; }
            set { _runSpeed = value; RaisePropertyChanged(); }
        }

        private double _acc = 10;
        public double Acc
        {
            get { return _acc; }
            set { _acc = value; RaisePropertyChanged(); }
        }

        private double _dec = 10;
        public double Dec
        {
            get { return _dec; }
            set { _dec = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 轴运动点位配置
    /// </summary>
    [Serializable]
    public class PLCAxisMotionConfig : BindableBase
    {
        private PLCCommandPointConfig _enableShieldWrite = new PLCCommandPointConfig { Description = "使能屏蔽" };
        public PLCCommandPointConfig EnableShieldWrite
        {
            get { return _enableShieldWrite; }
            set { _enableShieldWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _setPositionWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "设定位置" };
        public PLCPointConfig SetPositionWrite
        {
            get { return _setPositionWrite; }
            set { _setPositionWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _runPositionWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "跑位位置" };
        public PLCPointConfig RunPositionWrite
        {
            get { return _runPositionWrite; }
            set { _runPositionWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _currentPosRead = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "当前位置" };
        public PLCPointConfig CurrentPosRead
        {
            get { return _currentPosRead; }
            set { _currentPosRead = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _runTriggerWrite = new PLCCommandPointConfig { Description = "跑位触发" };
        public PLCCommandPointConfig RunTriggerWrite
        {
            get { return _runTriggerWrite; }
            set { _runTriggerWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _setPositionTriggerWrite = new PLCCommandPointConfig { Description = "设定位置触发" };
        public PLCCommandPointConfig SetPositionTriggerWrite
        {
            get { return _setPositionTriggerWrite; }
            set { _setPositionTriggerWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _stopTriggerWrite = new PLCCommandPointConfig { Description = "停止触发" };
        public PLCCommandPointConfig StopTriggerWrite
        {
            get { return _stopTriggerWrite; }
            set { _stopTriggerWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _homeTriggerWrite = new PLCCommandPointConfig { Description = "回原触发" };
        public PLCCommandPointConfig HomeTriggerWrite
        {
            get { return _homeTriggerWrite; }
            set { _homeTriggerWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _moveDoneRead = new PLCPointConfig { Description = "移动到位" };
        public PLCPointConfig MoveDoneRead
        {
            get { return _moveDoneRead; }
            set { _moveDoneRead = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _moveDoneResetWrite = new PLCCommandPointConfig { Description = "到位复位" };
        public PLCCommandPointConfig MoveDoneResetWrite
        {
            get { return _moveDoneResetWrite; }
            set { _moveDoneResetWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _jogPositiveWrite = new PLCCommandPointConfig { Description = "正向点动" };
        public PLCCommandPointConfig JogPositiveWrite
        {
            get { return _jogPositiveWrite; }
            set { _jogPositiveWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _jogNegativeWrite = new PLCCommandPointConfig { Description = "负向点动" };
        public PLCCommandPointConfig JogNegativeWrite
        {
            get { return _jogNegativeWrite; }
            set { _jogNegativeWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _jogStepWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "点动量" };
        public PLCPointConfig JogStepWrite
        {
            get { return _jogStepWrite; }
            set { _jogStepWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _manualSpeedWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "手动速度" };
        public PLCPointConfig ManualSpeedWrite
        {
            get { return _manualSpeedWrite; }
            set { _manualSpeedWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _accWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "加速度" };
        public PLCPointConfig AccWrite
        {
            get { return _accWrite; }
            set { _accWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _decWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "减速度" };
        public PLCPointConfig DecWrite
        {
            get { return _decWrite; }
            set { _decWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _runSpeedWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "运行速度" };
        public PLCPointConfig RunSpeedWrite
        {
            get { return _runSpeedWrite; }
            set { _runSpeedWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _maxLimitWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "最大限位" };
        public PLCPointConfig MaxLimitWrite
        {
            get { return _maxLimitWrite; }
            set { _maxLimitWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _minLimitWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Float, Description = "最小限位" };
        public PLCPointConfig MinLimitWrite
        {
            get { return _minLimitWrite; }
            set { _minLimitWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _alarmRead = new PLCPointConfig { Description = "轴报警" };
        public PLCPointConfig AlarmRead
        {
            get { return _alarmRead; }
            set { _alarmRead = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _homeDoneRead = new PLCPointConfig { Description = "回原完成" };
        public PLCPointConfig HomeDoneRead
        {
            get { return _homeDoneRead; }
            set { _homeDoneRead = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _busyRead = new PLCPointConfig { Description = "运行中" };
        public PLCPointConfig BusyRead
        {
            get { return _busyRead; }
            set { _busyRead = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _enabledRead = new PLCPointConfig { Description = "使能状态" };
        public PLCPointConfig EnabledRead
        {
            get { return _enabledRead; }
            set { _enabledRead = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 设备级运动配置
    /// </summary>
    [Serializable]
    public class PLCDeviceMotionConfig : BindableBase
    {
        private PLCCommandPointConfig _resetWrite = new PLCCommandPointConfig { Description = "复位" };
        public PLCCommandPointConfig ResetWrite
        {
            get { return _resetWrite; }
            set { _resetWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _resetDoneRead = new PLCPointConfig { Description = "复位完成" };
        public PLCPointConfig ResetDoneRead
        {
            get { return _resetDoneRead; }
            set { _resetDoneRead = value; RaisePropertyChanged(); }
        }

        private bool _startupResetEnabled;
        /// <summary>
        /// 软件启动后是否自动触发PLC复位。
        /// </summary>
        public bool StartupResetEnabled
        {
            get { return _startupResetEnabled; }
            set { _startupResetEnabled = value; RaisePropertyChanged(); }
        }

        private int _startupResetTimeoutMs = 30000;
        /// <summary>
        /// 软件启动复位等待完成超时时间。
        /// </summary>
        public int StartupResetTimeoutMs
        {
            get { return _startupResetTimeoutMs; }
            set { _startupResetTimeoutMs = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _plcStateRead = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Int, Description = "PLC状态" };
        public PLCPointConfig PLCStateRead
        {
            get { return _plcStateRead; }
            set { _plcStateRead = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _hostStateWrite = new PLCPointConfig { DataType = EnumParaInfoModelParaType.Int, Description = "上位机状态" };
        public PLCPointConfig HostStateWrite
        {
            get { return _hostStateWrite; }
            set { _hostStateWrite = value; RaisePropertyChanged(); }
        }

        private PLCCommandPointConfig _servoAlarmClearWrite = new PLCCommandPointConfig { Description = "伺服报警清除" };
        public PLCCommandPointConfig ServoAlarmClearWrite
        {
            get { return _servoAlarmClearWrite; }
            set { _servoAlarmClearWrite = value; RaisePropertyChanged(); }
        }

        private PLCPointConfig _alarmRead = new PLCPointConfig { Description = "设备报警" };
        public PLCPointConfig AlarmRead
        {
            get { return _alarmRead; }
            set { _alarmRead = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<PLCStateMapItem> _plcStateMaps = new ObservableCollection<PLCStateMapItem>();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<PLCStateMapItem> PLCStateMaps
        {
            get { return _plcStateMaps; }
            set { _plcStateMaps = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<PLCStateMapItem> _hostStateMaps = new ObservableCollection<PLCStateMapItem>();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<PLCStateMapItem> HostStateMaps
        {
            get { return _hostStateMaps; }
            set { _hostStateMaps = value; RaisePropertyChanged(); }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            EnsureStateMaps();
        }

        public PLCDeviceMotionConfig()
        {
            EnsureStateMaps();
        }

        private void EnsureStateMaps()
        {
            if (PLCStateMaps == null || PLCStateMaps.Count == 0)
            {
                PLCStateMaps = new ObservableCollection<PLCStateMapItem>
                {
                    new PLCStateMapItem(1, "Ready"),
                    new PLCStateMapItem(2, "Stop"),
                    new PLCStateMapItem(3, "Warning"),
                };
            }

            if (HostStateMaps == null || HostStateMaps.Count == 0)
            {
                HostStateMaps = new ObservableCollection<PLCStateMapItem>
                {
                    new PLCStateMapItem(0, "Idle"),
                    new PLCStateMapItem(1, "Ready"),
                    new PLCStateMapItem(2, "Moving"),
                    new PLCStateMapItem(3, "Resetting"),
                    new PLCStateMapItem(4, "Alarm"),
                };
            }
        }
    }

    /// <summary>
    /// 状态映射
    /// </summary>
    [Serializable]
    public class PLCStateMapItem : BindableBase
    {
        public PLCStateMapItem()
        {
        }

        public PLCStateMapItem(int value, string text)
        {
            Value = value;
            Text = text;
        }

        private int _value;
        public int Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        private string _text = "";
        public string Text
        {
            get { return _text; }
            set { _text = value; RaisePropertyChanged(); }
        }
    }
}

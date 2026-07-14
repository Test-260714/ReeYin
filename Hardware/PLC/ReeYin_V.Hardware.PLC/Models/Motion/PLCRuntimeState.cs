using Newtonsoft.Json;
using System;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// 单轴运行状态
    /// </summary>
    [Serializable]
    public class PLCAxisRuntimeState : BindableBase
    {
        private double _targetPos;
        public double TargetPos
        {
            get { return _targetPos; }
            set { _targetPos = value; RaisePropertyChanged(); }
        }

        private double _inputTargetPos;
        public double InputTargetPos
        {
            get { return _inputTargetPos; }
            set { _inputTargetPos = value; RaisePropertyChanged(); }
        }

        private double _inputRunSpeed = 10;
        public double InputRunSpeed
        {
            get { return _inputRunSpeed; }
            set { _inputRunSpeed = value; RaisePropertyChanged(); }
        }

        private double _inputJogSpeed = 10;
        public double InputJogSpeed
        {
            get { return _inputJogSpeed; }
            set { _inputJogSpeed = value; RaisePropertyChanged(); }
        }

        private double _inputJogStep = 1;
        public double InputJogStep
        {
            get { return _inputJogStep; }
            set { _inputJogStep = value; RaisePropertyChanged(); }
        }

        private double _inputAcc = 10;
        public double InputAcc
        {
            get { return _inputAcc; }
            set { _inputAcc = value; RaisePropertyChanged(); }
        }

        private double _inputDec = 10;
        public double InputDec
        {
            get { return _inputDec; }
            set { _inputDec = value; RaisePropertyChanged(); }
        }

        private double _currentPos;
        [JsonIgnore]
        public double CurrentPos
        {
            get { return _currentPos; }
            set { _currentPos = value; RaisePropertyChanged(); }
        }

        private bool _isMoving;
        [JsonIgnore]
        public bool IsMoving
        {
            get { return _isMoving; }
            set { _isMoving = value; RaisePropertyChanged(); }
        }

        private bool _isInPosition;
        [JsonIgnore]
        public bool IsInPosition
        {
            get { return _isInPosition; }
            set { _isInPosition = value; RaisePropertyChanged(); }
        }

        private bool _isAlarm;
        [JsonIgnore]
        public bool IsAlarm
        {
            get { return _isAlarm; }
            set { _isAlarm = value; RaisePropertyChanged(); }
        }

        private bool _isHomeDone;
        [JsonIgnore]
        public bool IsHomeDone
        {
            get { return _isHomeDone; }
            set { _isHomeDone = value; RaisePropertyChanged(); }
        }

        private bool _isBusy;
        [JsonIgnore]
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(); }
        }

        private bool _isEnabled;
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private string _lastUpdateTime = "--";
        [JsonIgnore]
        public string LastUpdateTime
        {
            get { return _lastUpdateTime; }
            set { _lastUpdateTime = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 设备运行状态
    /// </summary>
    [Serializable]
    public class PLCDeviceRuntimeState : BindableBase
    {
        private int _plcStateValue;
        public int PLCStateValue
        {
            get { return _plcStateValue; }
            set { _plcStateValue = value; RaisePropertyChanged(); }
        }

        private string _plcStateText = "--";
        public string PLCStateText
        {
            get { return _plcStateText; }
            set { _plcStateText = value; RaisePropertyChanged(); }
        }

        private int _hostStateValue;
        public int HostStateValue
        {
            get { return _hostStateValue; }
            set { _hostStateValue = value; RaisePropertyChanged(); }
        }

        private string _hostStateText = "--";
        public string HostStateText
        {
            get { return _hostStateText; }
            set { _hostStateText = value; RaisePropertyChanged(); }
        }

        private bool _resetCompleted;
        public bool ResetCompleted
        {
            get { return _resetCompleted; }
            set { _resetCompleted = value; RaisePropertyChanged(); }
        }

        private bool _hasAlarm;
        public bool HasAlarm
        {
            get { return _hasAlarm; }
            set { _hasAlarm = value; RaisePropertyChanged(); }
        }

        private string _alarmText = "";
        public string AlarmText
        {
            get { return _alarmText; }
            set { _alarmText = value; RaisePropertyChanged(); }
        }

        private string _lastUpdateTime = "--";
        public string LastUpdateTime
        {
            get { return _lastUpdateTime; }
            set { _lastUpdateTime = value; RaisePropertyChanged(); }
        }
    }
}

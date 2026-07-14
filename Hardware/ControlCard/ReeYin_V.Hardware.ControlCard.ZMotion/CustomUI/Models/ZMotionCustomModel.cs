using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Models
{
    [Serializable]
    public class ZMotionCustomModel : BindableBase
    {
        public ZMotionCustomModel()
        {
            for (int i = 0; i < 32; i++)
            {
                InputPoints.Add(new ZMotionIoPoint { Port = i, Name = $"IN{i}" });
                OutputPoints.Add(new ZMotionIoPoint { Port = i, Name = $"OUT{i}" });
            }
        }

        private string _ipAddress = "192.168.0.11";
        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; RaisePropertyChanged(); }
        }

        private int _connectionType = 1;
        /// <summary>
        /// 连接类型 0-串口 1-以太网
        /// </summary>
        public int ConnectionType
        {
            get { return _connectionType; }
            set { _connectionType = value; RaisePropertyChanged(); }
        }

        private uint _comPort = 1;
        /// <summary>
        /// 串口号
        /// </summary>
        public uint ComPort
        {
            get { return _comPort; }
            set { _comPort = value; RaisePropertyChanged(); }
        }

        private float _homeSpeed = 50f;
        /// <summary>
        /// 回零速度
        /// </summary>
        public float HomeSpeed
        {
            get { return _homeSpeed; }
            set { _homeSpeed = value; RaisePropertyChanged(); }
        }

        private float _moveSpeed = 100f;
        /// <summary>
        /// 运动速度
        /// </summary>
        public float MoveSpeed
        {
            get { return _moveSpeed; }
            set { _moveSpeed = value; RaisePropertyChanged(); }
        }

        private float _targetPosition = 0f;
        /// <summary>
        /// 目标位置
        /// </summary>
        public float TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; RaisePropertyChanged(); }
        }

        private float _currentPosition = 0f;
        /// <summary>
        /// 当前位置
        /// </summary>
        public float CurrentPosition
        {
            get { return _currentPosition; }
            set { _currentPosition = value; RaisePropertyChanged(); }
        }

        private float _currentSpeed = 0f;
        /// <summary>
        /// 当前速度
        /// </summary>
        public float CurrentSpeed
        {
            get { return _currentSpeed; }
            set { _currentSpeed = value; RaisePropertyChanged(); }
        }

        private float _encoderSpeed = 0f;
        /// <summary>
        /// 编码器速度，单位：脉冲/秒
        /// </summary>
        public float EncoderSpeed
        {
            get { return _encoderSpeed; }
            set { _encoderSpeed = value; RaisePropertyChanged(); }
        }

        private string _runState = "停止中";
        /// <summary>
        /// 运行状态
        /// </summary>
        public string RunState
        {
            get { return _runState; }
            set 
            { 
                _runState = value; 
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsRunning));
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _runState == "运行中";

        private ObservableCollection<string> _logMessages = new ObservableCollection<string>();
        /// <summary>
        /// 日志消息
        /// </summary>
        public ObservableCollection<string> LogMessages
        {
            get { return _logMessages; }
            set { _logMessages = value; RaisePropertyChanged(); }
        }

        public void AddLog(string message)
        {
            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(LogMessages.Count - 1);
            }
        }

        private ObservableCollection<ZMotionIoPoint> _inputPoints = new ObservableCollection<ZMotionIoPoint>();
        public ObservableCollection<ZMotionIoPoint> InputPoints
        {
            get { return _inputPoints; }
            set { _inputPoints = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ZMotionIoPoint> _outputPoints = new ObservableCollection<ZMotionIoPoint>();
        public ObservableCollection<ZMotionIoPoint> OutputPoints
        {
            get { return _outputPoints; }
            set { _outputPoints = value; RaisePropertyChanged(); }
        }
    }

    [Serializable]
    public class ZMotionIoPoint : BindableBase
    {
        private int _port;
        public int Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private bool _state;
        public bool State
        {
            get { return _state; }
            set { _state = value; RaisePropertyChanged(); }
        }
    }
}

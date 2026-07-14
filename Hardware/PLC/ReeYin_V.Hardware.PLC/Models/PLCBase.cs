using HalconDotNet;
using HslCommunication.Core;
using HslCommunication.Profinet.Inovance;
using HslCommunication.Profinet.XINJE;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.PLC.Interface;
using ReeYin_V.Hardware.PLC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// 使用Hsl的类库实现，暂不考虑抽基类
    /// </summary>
    public class PLCBase : BindableBase,IPLCService
    {
        #region Fields
        [JsonIgnore]
        public HslClient hslClient = new HslClient();

        #endregion

        #region Properties
        [JsonIgnore]
        private PlcConfigModel _config = new PlcConfigModel();

        public PlcConfigModel Config
        {
            get { return _config; }
            set { _config = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<PLCAxisGroup> _axisGroups = new ObservableCollection<PLCAxisGroup>();
        /// <summary>
        /// 轴组列表
        /// </summary>
        public ObservableCollection<PLCAxisGroup> AxisGroups
        {
            get { return _axisGroups; }
            set { _axisGroups = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCDeviceMotionConfig _deviceMotionConfig = new PLCDeviceMotionConfig();
        /// <summary>
        /// 设备级运动协议配置
        /// </summary>
        public PLCDeviceMotionConfig DeviceMotionConfig
        {
            get { return _deviceMotionConfig; }
            set { _deviceMotionConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCDeviceRuntimeState _deviceRuntimeState = new PLCDeviceRuntimeState();
        /// <summary>
        /// 设备运行时状态
        /// </summary>
        [JsonIgnore]
        public PLCDeviceRuntimeState DeviceRuntimeState
        {
            get { return _deviceRuntimeState; }
            set { _deviceRuntimeState = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HeartbeatConfig _heartbeat = new HeartbeatConfig();
        /// <summary>
        /// 心跳监控配置
        /// </summary>
        public HeartbeatConfig Heartbeat
        {
            get { return _heartbeat; }
            set { _heartbeat = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHeartbeatAlive;
        /// <summary>
        /// 心跳是否存活
        /// </summary>
        [JsonIgnore]
        public bool IsHeartbeatAlive
        {
            get { return _isHeartbeatAlive; }
            set { _isHeartbeatAlive = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _lastHeartbeatValue = "";
        /// <summary>
        /// 最近一次心跳读取值
        /// </summary>
        [JsonIgnore]
        public string LastHeartbeatValue
        {
            get { return _lastHeartbeatValue; }
            set { _lastHeartbeatValue = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _lastHeartbeatTime = "--";
        /// <summary>
        /// 最近一次心跳成功时间
        /// </summary>
        [JsonIgnore]
        public string LastHeartbeatTime
        {
            get { return _lastHeartbeatTime; }
            set { _lastHeartbeatTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Timer _heartbeatTimer;

        [JsonIgnore]
        private PLCMotionService _motionService;
        [JsonIgnore]
        public PLCMotionService MotionService => _motionService ??= new PLCMotionService(this);

        [JsonIgnore]
        private PLCMonitorService _monitorService;
        [JsonIgnore]
        public PLCMonitorService MonitorService => _monitorService ??= new PLCMonitorService(this);

        [JsonIgnore]
        private HardwareState _state;
        [JsonIgnore]
        public HardwareState State
        {
            get { return _state; }
            set 
            {
                _state = value;
                PrismProvider.EventAggregator.GetEvent<HardwareStatusChangedEvent>().Publish(new HardwareStatus
                {
                    Name = "PLC",
                    Status = _state,
                    IsConnect = Config.IsConnected,
                    Describe = ""
                });
            }
        }

        #endregion

        #region Constructor
        public PLCBase()
        {

        }
        #endregion

        #region Commands

        #endregion

        #region Methods
        public bool Init()
        {
            State = HardwareState.Initializing;

            //传递参数
            hslClient.Init(Config);

            hslClient.Connect();

            if (hslClient.IsConnected())
            {
                Console.WriteLine("PLC连接成功");
                State = HardwareState.Connected;
                Config.IsConnected = true;
                MotionService.ClearWriteCache();

                if (Heartbeat.IsEnabled)
                    StartHeartbeat();

                StartMotionMonitor();

                return true;
            }
            else
            {
                Console.WriteLine("PLC连接失败");
                Config.IsConnected = false;
                State = HardwareState.NotConnected;
                return false;
            }
        }

        public bool Close()
        {
            StopHeartbeat();
            StopMotionMonitor();
            MotionService.ClearWriteCache();
            State = HardwareState.Closed;
            bool result = hslClient.Dispose();
            Config.IsConnected = false;
            return result;
        }

        public bool WritePLCPara(AddressMappingItem amim)
        {
            return hslClient.WritePLCPara(amim);
        }

        public bool ReadPLCPara(AddressMappingItem amim)
        {
            return hslClient.ReadPLCPara(amim);
        }

        public bool WritePLCPara(PLCParaInfoModel amim)
        {
            return hslClient.WritePLCPara(amim);
        }

        public bool ReadPLCPara(PLCParaInfoModel amim)
        {
            return hslClient.ReadPLCPara(amim);
        }

        public bool TryReadPointValue(PLCPointConfig point, out object value)
        {
            value = null;
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            var param = new PLCParaInfoModel
            {
                PLCAddress = point.Address,
                ParaType = point.DataType,
            };

            bool success = ReadPLCPara(param);
            if (success)
            {
                value = param.ParaValue;
            }

            return success;
        }

        public bool WritePoint(PLCPointConfig point, object value)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            return WritePLCPara(new PLCParaInfoModel
            {
                PLCAddress = point.Address,
                ParaType = point.DataType,
                ParaValue = value,
            });
        }

        public bool ExecuteCommandPoint(PLCCommandPointConfig point)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            var activeValue = ConvertTextValue(point.ActiveValue, point.DataType);
            if (!WritePoint(point, activeValue))
            {
                return false;
            }

            if (point.AutoClear)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(point.PulseMs);
                    WriteCommandClearValue(point);
                });
            }

            return true;
        }

        public bool WriteCommandClearValue(PLCCommandPointConfig point)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            return WritePoint(point, ConvertTextValue(point.ClearValue, point.DataType));
        }

        /// <summary>
        /// 启动心跳监控
        /// </summary>
        public void StartHeartbeat()
        {
            StopHeartbeat();

            if (string.IsNullOrWhiteSpace(Heartbeat.Address))
                return;

            int interval = Heartbeat.ScanInterval > 0 ? Heartbeat.ScanInterval : 1000;
            _heartbeatTimer = new Timer(interval);
            _heartbeatTimer.Elapsed += async (sender, e) => await Task.Run(() =>
            {
                try
                {
                    var param = new PLCParaInfoModel
                    {
                        PLCAddress = Heartbeat.Address,
                        ParaType = Heartbeat.DataType,
                    };

                    bool success = ReadPLCPara(param);
                    if (success)
                    {
                        IsHeartbeatAlive = true;
                        LastHeartbeatValue = param.ParaValue?.ToString() ?? "";
                        LastHeartbeatTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        IsHeartbeatAlive = false;
                    }
                }
                catch
                {
                    IsHeartbeatAlive = false;
                }
            });
            _heartbeatTimer.Enabled = true;
        }

        /// <summary>
        /// 停止心跳监控
        /// </summary>
        public void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Enabled = false;
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }
            IsHeartbeatAlive = false;
            LastHeartbeatValue = "";
            LastHeartbeatTime = "--";
        }

        /// <summary>
        /// 启动运动状态监听
        /// </summary>
        public void StartMotionMonitor()
        {
            MonitorService.Start();
        }

        /// <summary>
        /// 停止运动状态监听
        /// </summary>
        public void StopMotionMonitor()
        {
            MonitorService.Stop();
        }

        private static object ConvertTextValue(string value, EnumParaInfoModelParaType dataType)
        {
            string text = value ?? string.Empty;
            switch (dataType)
            {
                case EnumParaInfoModelParaType.Bool:
                    if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return bool.TryParse(text, out bool boolValue) && boolValue;
                case EnumParaInfoModelParaType.Short:
                    return short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out short shortValue) ? shortValue : (short)0;
                case EnumParaInfoModelParaType.Ushort:
                    return ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ushort ushortValue) ? ushortValue : (ushort)0;
                case EnumParaInfoModelParaType.Int:
                    return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue) ? intValue : 0;
                case EnumParaInfoModelParaType.Uint:
                    return uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out uint uintValue) ? uintValue : 0u;
                case EnumParaInfoModelParaType.Long:
                    return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out long longValue) ? longValue : 0L;
                case EnumParaInfoModelParaType.Ulong:
                    return ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ulong ulongValue) ? ulongValue : 0UL;
                case EnumParaInfoModelParaType.Double:
                    return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue) ? doubleValue : 0d;
                case EnumParaInfoModelParaType.Float:
                default:
                    return float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue) ? floatValue : 0f;
            }
        }
        #endregion
    }


    /// <summary>
    /// 操作类型
    /// </summary>
    public enum OperationType
    {
        读单个地址,
        写单个地址,
        轴操作,
        延时操作,
        触发事件,
    }

    /// <summary>
    /// 下发给PLC的指令
    /// </summary>
    [Serializable]
    public class PLCOrder : BindableBase
    {
        [JsonIgnore]
        private bool isUsing;
        [Browsable(false)]
        public bool IsUsing
        {
            get { return isUsing; }
            set { isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private OperationType _operationType;
        [Browsable(false)]
        public OperationType OperationType
        {
            get { return _operationType; }
            set { _operationType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTab = 0;
        /// <summary>
        /// 用来区分不同的Tab页
        /// </summary>
        [Browsable(false)]
        public int SltTab
        {
            get { return _sltTab; }
            set { _sltTab = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EnumParaInfoModelParaType _paramType;
        /// <summary>
        /// 发给PLC的参数类型
        /// </summary>
        public EnumParaInfoModelParaType ParamType
        {
            get { return _paramType; }
            set { _paramType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _addr;
        /// <summary>
        /// 地址
        /// </summary>
        public string Addr
        {
            get { return _addr; }
            set { _addr = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _delay;
        /// <summary>
        /// 延时操作
        /// </summary>
        public int Delay
        {
            get { return _delay; }
            set
            {
                // 限制值不超过60000，同时确保不小于0（可选，根据业务需求）
                int newValue = Math.Clamp(value, 0, 60);
                if (_delay != newValue)
                {
                    _delay = newValue;
                    RaisePropertyChanged();
                }
            }
        }

        private int _waitDelay;
        /// <summary>
        /// 等待延时
        /// </summary>
        public int WaitDelay
        {
            get { return _waitDelay; }
            set { _waitDelay = value; RaisePropertyChanged(); }
        }

        private bool _isUsingJedge;
        /// <summary>
        /// 启用判定
        /// </summary>
        public bool IsUsingJedge
        {
            get { return _isUsingJedge; }
            set { _isUsingJedge = value; RaisePropertyChanged(); }
        }

        private bool _isUsingPublish;
        /// <summary>
        /// 启用推送
        /// </summary>
        public bool IsUsingPublish
        {
            get { return _isUsingPublish; }
            set { _isUsingPublish = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _judgeValue;
        /// <summary>
        /// 判定值
        /// </summary>
        public string JudgeValue
        {
            get { return _judgeValue; }
            set { _judgeValue = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _value;
        /// <summary>
        /// 操作的值
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        [Browsable(false)]
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _linkValue;
        /// <summary>
        /// 链接值
        /// </summary>
        [Browsable(false)]
        public TransmitParam LinkValue
        {
            get { return _linkValue; }
            set { _linkValue = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 选中的事件名称
        /// </summary>
        [JsonIgnore]
        private string _sltEventName;
        public string SltEventName
        {
            get { return _sltEventName; }
            set { _sltEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _targetPlcId = "";
        /// <summary>
        /// 轴操作目标PLC
        /// </summary>
        public string TargetPlcId
        {
            get { return _targetPlcId; }
            set { _targetPlcId = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _axisGroupName = "";
        /// <summary>
        /// 轴操作目标轴组
        /// </summary>
        public string AxisGroupName
        {
            get { return _axisGroupName; }
            set { _axisGroupName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<PLCOrderAxisMoveItem> _axisMoveItems = new ObservableCollection<PLCOrderAxisMoveItem>();
        /// <summary>
        /// 轴操作目标位置
        /// </summary>
        public ObservableCollection<PLCOrderAxisMoveItem> AxisMoveItems
        {
            get
            {
                if (_axisMoveItems == null)
                    _axisMoveItems = new ObservableCollection<PLCOrderAxisMoveItem>();
                return _axisMoveItems;
            }
            set { _axisMoveItems = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _waitMoveDone = false;
        /// <summary>
        /// 是否等待移动到位
        /// </summary>
        public bool WaitMoveDone
        {
            get { return _waitMoveDone; }
            set { _waitMoveDone = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _moveTimeoutMs = 60000;
        /// <summary>
        /// 移动到位超时时间
        /// </summary>
        public int MoveTimeoutMs
        {
            get { return _moveTimeoutMs; }
            set { _moveTimeoutMs = Math.Clamp(value, 100, 600000); RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _publishLineScanSegment;
        /// <summary>
        /// 是否发布线扫段坐标
        /// </summary>
        public bool PublishLineScanSegment
        {
            get { return _publishLineScanSegment; }
            set { _publishLineScanSegment = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCLineScanSegmentInfo? _lineScanSegmentInfo;
        /// <summary>
        /// 线扫段坐标信息
        /// </summary>
        public PLCLineScanSegmentInfo? LineScanSegmentInfo
        {
            get { return _lineScanSegmentInfo; }
            set { _lineScanSegmentInfo = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// PLC指令中的单轴走位参数
    /// </summary>
    [Serializable]
    public class PLCOrderAxisMoveItem : BindableBase
    {
        [JsonIgnore]
        private bool _isUsing;
        /// <summary>
        /// 是否启用该轴
        /// </summary>
        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _axisName = "";
        /// <summary>
        /// 轴名称
        /// </summary>
        public string AxisName
        {
            get { return _axisName; }
            set { _axisName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EnumAxisType _axisType = EnumAxisType.Undefined;
        /// <summary>
        /// 轴类型
        /// </summary>
        public EnumAxisType AxisType
        {
            get { return _axisType; }
            set { _axisType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _targetPosition;
        /// <summary>
        /// 目标位置
        /// </summary>
        public double TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _runSpeed;
        /// <summary>
        /// 运行速度
        /// </summary>
        public double RunSpeed
        {
            get { return _runSpeed; }
            set { _runSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _acc;
        /// <summary>
        /// 加速度
        /// </summary>
        public double Acc
        {
            get { return _acc; }
            set { _acc = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _dec;
        /// <summary>
        /// 减速度
        /// </summary>
        public double Dec
        {
            get { return _dec; }
            set { _dec = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 心跳监控配置
    /// </summary>
    [Serializable]
    public class HeartbeatConfig : BindableBase
    {
        [JsonIgnore]
        private string _address = "";
        /// <summary>
        /// 心跳PLC地址
        /// </summary>
        public string Address
        {
            get { return _address; }
            set { _address = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _scanInterval = 1000;
        /// <summary>
        /// 扫描周期（ms）
        /// </summary>
        public int ScanInterval
        {
            get { return _scanInterval; }
            set
            {
                _scanInterval = Math.Clamp(value, 100, 60000);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private EnumParaInfoModelParaType _dataType = EnumParaInfoModelParaType.Bool;
        /// <summary>
        /// 数据类型
        /// </summary>
        public EnumParaInfoModelParaType DataType
        {
            get { return _dataType; }
            set { _dataType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isEnabled = false;
        /// <summary>
        /// 是否启用心跳监控
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe = "";
        /// <summary>
        /// 描述
        /// </summary>
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }
    }
}

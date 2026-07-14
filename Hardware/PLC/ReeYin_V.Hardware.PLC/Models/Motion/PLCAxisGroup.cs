using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// PLC轴组 - 一组逻辑上相关的轴
    /// </summary>
    [Serializable]
    public class PLCAxisGroup : BindableBase
    {
        [JsonIgnore]
        private string _groupName = "新轴组";
        /// <summary>
        /// 轴组名称
        /// </summary>
        public string GroupName
        {
            get { return _groupName; }
            set { _groupName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _description = "";
        /// <summary>
        /// 轴组描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing = true;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<PLCAxisItem> _axisItems = new ObservableCollection<PLCAxisItem>();
        /// <summary>
        /// 轴组中的所有轴
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<PLCAxisItem> AxisItems
        {
            get { return _axisItems; }
            set { _axisItems = value; RaisePropertyChanged(); }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            AxisItems ??= new ObservableCollection<PLCAxisItem>();
        }
    }

    /// <summary>
    /// PLC轴项 - 单个轴的配置（基于PLC地址）
    /// </summary>
    [Serializable]
    public class PLCAxisItem : BindableBase
    {
        [JsonIgnore]
        private string _axisName = "新轴";
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
        private short _axisNo = 1;
        /// <summary>
        /// 轴号
        /// </summary>
        public short AxisNo
        {
            get { return _axisNo; }
            set { _axisNo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EnumMotionType _motionType = EnumMotionType.None;
        /// <summary>
        /// 运动类型
        /// </summary>
        public EnumMotionType MotionType
        {
            get { return _motionType; }
            set { _motionType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _address = "";
        /// <summary>
        /// PLC地址
        /// </summary>
        public string Address
        {
            get { return _address; }
            set { _address = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EnumParaInfoModelParaType _dataType = EnumParaInfoModelParaType.Float;
        /// <summary>
        /// 数据类型
        /// </summary>
        public EnumParaInfoModelParaType DataType
        {
            get { return _dataType; }
            set { _dataType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _value = "";
        /// <summary>
        /// 值
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _description = "";
        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing = true;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _defaultJogStep = 1;
        /// <summary>
        /// 默认点动量
        /// </summary>
        public double DefaultJogStep
        {
            get { return _defaultJogStep; }
            set { _defaultJogStep = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _defaultRunSpeed = 10;
        /// <summary>
        /// 默认运行速度
        /// </summary>
        public double DefaultRunSpeed
        {
            get { return _defaultRunSpeed; }
            set { _defaultRunSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _defaultAcc = 10;
        /// <summary>
        /// 默认加速度
        /// </summary>
        public double DefaultAcc
        {
            get { return _defaultAcc; }
            set { _defaultAcc = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _defaultDec = 10;
        /// <summary>
        /// 默认减速度
        /// </summary>
        public double DefaultDec
        {
            get { return _defaultDec; }
            set { _defaultDec = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _maxLimit = 100;
        /// <summary>
        /// 最大限位
        /// </summary>
        public double MaxLimit
        {
            get { return _maxLimit; }
            set { _maxLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _minLimit = -100;
        /// <summary>
        /// 最小限位
        /// </summary>
        public double MinLimit
        {
            get { return _minLimit; }
            set { _minLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCAxisMotionConfig _motionConfig = new PLCAxisMotionConfig();
        /// <summary>
        /// 轴协议配置
        /// </summary>
        public PLCAxisMotionConfig MotionConfig
        {
            get { return _motionConfig; }
            set { _motionConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<PLCSpeedProfile> _speedProfiles = new ObservableCollection<PLCSpeedProfile>();
        /// <summary>
        /// 速度档
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<PLCSpeedProfile> SpeedProfiles
        {
            get { return _speedProfiles; }
            set { _speedProfiles = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCAxisRuntimeState _runtimeState = new PLCAxisRuntimeState();
        /// <summary>
        /// 轴运行时状态
        /// </summary>
        public PLCAxisRuntimeState RuntimeState
        {
            get { return _runtimeState; }
            set { _runtimeState = value; RaisePropertyChanged(); }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            MotionConfig ??= new PLCAxisMotionConfig();
            RuntimeState ??= new PLCAxisRuntimeState();
            EnsureSpeedProfiles();
        }

        public PLCAxisItem()
        {
            EnsureSpeedProfiles();
        }

        private void EnsureSpeedProfiles()
        {
            if (SpeedProfiles != null && SpeedProfiles.Count > 0)
            {
                return;
            }

            SpeedProfiles = new ObservableCollection<PLCSpeedProfile>
            {
                new PLCSpeedProfile { Name = "低速", RunSpeed = 10, Acc = 10, Dec = 10 },
                new PLCSpeedProfile { Name = "中速", RunSpeed = 30, Acc = 20, Dec = 20 },
                new PLCSpeedProfile { Name = "高速", RunSpeed = 60, Acc = 30, Dec = 30 },
                new PLCSpeedProfile { Name = "工作", RunSpeed = 20, Acc = 15, Dec = 15 },
                new PLCSpeedProfile { Name = "复位", RunSpeed = 15, Acc = 10, Dec = 10 },
            };
        }
    }
}

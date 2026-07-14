using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    /// <summary>
    /// 优先级
    /// </summary>
    public enum En_Priority
    {
        Top = 0,
        High = 1,
        Normal = 2,
        Low = 3,
        Bottom = 4,
    }

    /// <summary>
    /// 定义轴状态位枚举（按位枚举）
    /// </summary>
    [Flags]
    public enum AxisStatusFlags
    {
        // 0位：保留
        Reserved0 = 0 << 0,

        // 1位：驱动器报警标志
        DriverAlarm = 1 << 1,

        // 2位：保留
        Reserved2 = 1 << 2,

        // 3位：保留
        Reserved3 = 1 << 3,

        // 4位：跟随误差越限标志
        FollowErrorOverLimit = 1 << 4,

        // 5位：正限位触发标志
        PositiveLimitTriggered = 1 << 5,

        // 6位：负限位触发标志
        NegativeLimitTriggered = 1 << 6,

        // 7位：IO平滑停止触发标志
        IOSmoothStopTriggered = 1 << 7,

        // 8位：IO急停触发标志
        IOEmergencyStopTriggered = 1 << 8,

        // 9位：电机使能标志
        MotorEnabled = 1 << 9,

        // 10位：规划运动标志
        PlanningInMotion = 1 << 10,

        // 11位：电机到位标志
        MotorInPosition = 1 << 11,

        // 12~31位：保留（可以不单独定义）
    }

    /// <summary>
    /// 单个轴参数
    /// </summary>
    [Serializable]
    public class SingleAxisParam : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        #region SomeStatus
        [JsonIgnore]
        private int _axisStatus;
        /// <summary>
        /// 轴状态
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public int AxisStatus
        {
            get { return _axisStatus; }
            set { _axisStatus = value; }
        }

        [JsonIgnore]
        private bool _isEnable = false;
        /// <summary>
        /// 是否使能
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsEnable
        {
            get { return _isEnable; }
            set { _isEnable = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isOrigin = false;
        /// <summary>
        /// 是否在原点
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsOrigin
        {
            get { return _isOrigin; }
            set { _isOrigin = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isPositiveLimit = false;
        /// <summary>
        /// 是否正限位
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsPositiveLimit
        {
            get { return _isPositiveLimit; }
            set { _isPositiveLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isNegativeLimit = false;
        /// <summary>
        /// 是否负限位
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsNegativeLimit
        {
            get { return _isNegativeLimit; }
            set { _isNegativeLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isAlarm = false;
        /// <summary>
        /// 是否报警
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsAlarm
        {
            get { return _isAlarm; }
            set { _isAlarm = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isEStop = false;
        /// <summary>
        /// 是否急停
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsEStop
        {
            get { return _isEStop; }
            set { _isEStop = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isResetCompleted = false;
        /// <summary>
        /// 是否回零完成
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsResetCompleted
        {
            get { return _isResetCompleted; }
            set { _isResetCompleted = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isMoving = false;
        /// <summary>
        /// 是否在运动
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsMoving
        {
            get { return _isMoving; }
            set { _isMoving = value; RaisePropertyChanged(); }
        }
        #endregion

        /// <summary>
        /// 当前位置
        /// </summary>
        [JsonIgnore]
        private double _curPos;
        /// <summary>
        /// 当前位置
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public double CurPos
        {
            get { return _curPos; }
            set { _curPos = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前实际速度
        /// </summary>
        [JsonIgnore]
        private double _curSpeed;
        /// <summary>
        /// 当前实际速度
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public double CurSpeed
        {
            get { return _curSpeed; }
            set { _curSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private En_AxisNum _axisNum = En_AxisNum.X;
        [Browsable(false)]
        public En_AxisNum AxisNum
        {
            get { return _axisNum; }
            set { _axisNum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing = false;
        /// <summary>
        /// 启用
        /// </summary>
        [Browsable(false)]
        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _nickName = "Default";
        /// <summary>
        /// 昵称
        /// </summary>
        [Browsable(false)]
        public string NickName
        {
            get { return _nickName; }
            set { _nickName = value; RaisePropertyChanged(); }
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

        /// <summary>
        /// 轴号从1 开始
        /// </summary>
        [JsonIgnore]
        private short _axisNo = 1;
        [Browsable(false)]
        public short AxisNo
        {
            get { return _axisNo; }
            set { _axisNo = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 移动方向取反
        /// </summary>
        [Category("1.轴设置"), DisplayName("移动方向取反")]
        public bool MovingDirNe { get; set; } = false;

        /// <summary>
        /// 原点偏移
        /// </summary>
        [Category("1.轴设置"), DisplayName("原点偏移")]
        public double OriginOffset { get; set; } = 0;

        /// <summary>
        /// 误差带mm
        /// </summary>
        [Category("1.轴设置"), DisplayName("误差带(mm)")]
        public double AxisBend { get; set; } = 0;

        /// <summary>
        /// 脉冲当量/mm
        /// </summary>
        [Category("1.轴设置"), DisplayName("脉冲当量/mm")]
        public double PulseEquivalent { get; set; } = 10000;

        /// <summary>
        /// 脉冲当量/mm
        /// </summary>
        [Category("1.轴设置"), DisplayName("回原方式")]
        public short GoHomeMode { get; set; } = 29;

        /// <summary>
        /// 软限位正
        /// </summary>
        [Category("1.轴设置"), DisplayName("软限位正")]
        public double SoftLimitPositive { get; set; } = 50;

        /// <summary>
        /// 软限位负
        /// </summary>
        [Category("1.轴设置"), DisplayName("软限位负")]
        public double SoftLimitNegative { get; set; } = -50;

        /// <summary>
        /// 安全距离（针对Z轴，不限于Z轴）
        /// </summary>
        [Category("1.轴设置"), DisplayName("安全距离（针对Z轴，不限于Z轴）")]
        public double SafetyDis { get; set; } = 10;

        /// <summary>
        /// 减速距离（针对Z轴，不限于Z轴）
        /// </summary>
        [Category("1.轴设置"), DisplayName("减速距离（针对Z轴，不限于Z轴）")]
        public double DecelerateDis { get; set; } = 10;

        [JsonIgnore]
        private En_Priority _priority = En_Priority.Normal;
        [Category("1.轴设置"), DisplayName("轴的优先级")]
        public En_Priority Priority
        {
            get { return _priority; }
            set { _priority = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<SpeedSetting> _speedDict1;
        //    = new ObservableCollection<SpeedSetting>
        //{
        //    new SpeedSetting{ SpeedType = EN_SpeedType.Low, SpeedDescribe = "低速",StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
        //    new SpeedSetting{ SpeedType = EN_SpeedType.Mid,SpeedDescribe = "中速",StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
        //    new SpeedSetting { SpeedType = EN_SpeedType.High, SpeedDescribe = "高速", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 } ,
        //    new SpeedSetting { SpeedType = EN_SpeedType.Work, SpeedDescribe = "工作速度", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
        //    new SpeedSetting { SpeedType = EN_SpeedType.Reset, SpeedDescribe = "复位速度", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
        //};

        /// <summary>
        /// 速度字典
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<SpeedSetting> SpeedDict1
        {
            get { return _speedDict1; }
            set { _speedDict1 = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SingleAxisParam()
        {

        }
        #endregion

        #region Methods

        public static ObservableCollection<SpeedSetting> CreateDefaultSpeedSettings()
        {
            return new ObservableCollection<SpeedSetting>
            {
                new SpeedSetting{ SpeedType = EN_SpeedType.Low, SpeedDescribe = "低速", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
                new SpeedSetting{ SpeedType = EN_SpeedType.Mid, SpeedDescribe = "中速", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
                new SpeedSetting { SpeedType = EN_SpeedType.High, SpeedDescribe = "高速", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
                new SpeedSetting { SpeedType = EN_SpeedType.Work, SpeedDescribe = "工作速度", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
                new SpeedSetting { SpeedType = EN_SpeedType.Reset, SpeedDescribe = "复位速度", StartSpeed = 10, MaxSpeed = 50, AccSpeed = 200 },
                CreateCustomSpeedSetting()
            };
        }

        public static SpeedSetting CreateCustomSpeedSetting()
        {
            return new SpeedSetting
            {
                SpeedType = EN_SpeedType.Custom,
                SpeedDescribe = "自定义速度",
                StartSpeed = 10,
                MaxSpeed = 50,
                AccSpeed = 200
            };
        }

        public void EnsureDefaultSpeedSettings()
        {
            if (SpeedDict1 == null || SpeedDict1.Count == 0)
            {
                SpeedDict1 = CreateDefaultSpeedSettings();
                return;
            }

            if (!SpeedDict1.Any(speed => speed != null && speed.SpeedType == EN_SpeedType.Custom))
            {
                SpeedDict1.Add(CreateCustomSpeedSetting());
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            EnsureDefaultSpeedSettings();
        }

        // 解析轴状态整数并返回各个标志的状态
        public Dictionary<string, bool> ParseAxisStatus(int statusValue)
        {
            var statusDict = new Dictionary<string, bool>
            {
                // 1位：驱动器报警标志
                { "驱动器报警标志", (statusValue & (int)AxisStatusFlags.DriverAlarm) != 0 },
            
                // 4位：跟随误差越限标志
                { "跟随误差越限标志", (statusValue & (int)AxisStatusFlags.FollowErrorOverLimit) != 0 },
            
                // 5位：正限位触发标志
                { "正限位触发标志", (statusValue & (int)AxisStatusFlags.PositiveLimitTriggered) != 0 },
            
                // 6位：负限位触发标志
                { "负限位触发标志", (statusValue & (int)AxisStatusFlags.NegativeLimitTriggered) != 0 },
            
                // 7位：IO平滑停止触发标志
                { "IO平滑停止触发标志", (statusValue & (int)AxisStatusFlags.IOSmoothStopTriggered) != 0 },
            
                // 8位：IO急停触发标志
                { "IO急停触发标志", (statusValue & (int)AxisStatusFlags.IOEmergencyStopTriggered) != 0 },
            
                // 9位：电机使能标志
                { "电机使能标志", (statusValue & (int)AxisStatusFlags.MotorEnabled) != 0 },
            
                // 10位：规划运动标志
                { "规划运动标志", (statusValue & (int)AxisStatusFlags.PlanningInMotion) != 0 },
            
                // 11位：电机到位标志
                { "电机到位标志", (statusValue & (int)AxisStatusFlags.MotorInPosition) != 0 }
            };

            return statusDict;
        }

        // 更详细的解析，包含描述信息
        public List<string> GetStatusDescriptions(int statusValue)
        {
            var descriptions = new List<string>();

            if ((statusValue & (int)AxisStatusFlags.DriverAlarm) != 0)
            {
                IsAlarm = false;
                descriptions.Add("驱动器报警（控制轴连接的驱动器报警）");
            }
            else
            {
                IsAlarm = true;
            }

            if ((statusValue & (int)AxisStatusFlags.FollowErrorOverLimit) != 0)
                descriptions.Add("跟随误差越限（控制轴规划位置和实际位置的误差大于设定极限）");

            if ((statusValue & (int)AxisStatusFlags.PositiveLimitTriggered) != 0)
            {
                IsPositiveLimit = false;
                descriptions.Add("正限位触发（正限位开关触发或规划位置大于正向软限位）");
            }
            else
            {
                IsPositiveLimit = true;

            }

            if ((statusValue & (int)AxisStatusFlags.NegativeLimitTriggered) != 0)
            {
                IsNegativeLimit = false;
                descriptions.Add("负限位触发（负限位开关触发或规划位置小于负向软限位）");
            }
            else
            {
                IsNegativeLimit = true;
            }

            if ((statusValue & (int)AxisStatusFlags.IOSmoothStopTriggered) != 0)
                descriptions.Add("IO平滑停止触发（平滑停止IO输入为触发电平）");

            if ((statusValue & (int)AxisStatusFlags.IOEmergencyStopTriggered) != 0)
                descriptions.Add("IO急停触发（急停IO输入为触发电平）");

            if ((statusValue & (int)AxisStatusFlags.MotorEnabled) != 0)
            {
                IsEnable = true; descriptions.Add("电机已使能");
            }
            else
            {
                IsEnable = false; descriptions.Add("电机未使能");
            }

            if ((statusValue & (int)AxisStatusFlags.PlanningInMotion) != 0)
            descriptions.Add("规划器正在运动");

            if ((statusValue & (int)AxisStatusFlags.MotorInPosition) != 0)
                descriptions.Add("电机已到位");

            return descriptions;
        }
        #endregion

    }

    /// <summary>
    /// 速度设置
    /// </summary>
    public class SpeedSetting : BindableBase
    {
        public EN_SpeedType SpeedType { get; set; }

        private string _speedDescribe;
        /// <summary>
        /// 速度描述
        /// </summary>
        public string SpeedDescribe
        {
            get { return _speedDescribe; }
            set { _speedDescribe = value; RaisePropertyChanged(); }
        }

        private double _startSpeed;

        public double StartSpeed
        {
            get { return _startSpeed; }
            set { _startSpeed = value; RaisePropertyChanged(); }
        }

        private double _maxSpeed;

        public double MaxSpeed
        {
            get { return _maxSpeed; }
            set { _maxSpeed = value; RaisePropertyChanged(); }
        }

        private double _accSpeed;

        public double AccSpeed
        {
            get { return _accSpeed; }
            set
            {
                _accSpeed = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DecSpeed));
                RaisePropertyChanged(nameof(KillDecSpeed));
                RaisePropertyChanged(nameof(Jerk));
            }
        }

        private double _decSpeed;

        public double DecSpeed
        {
            get { return _decSpeed > 0d ? _decSpeed : AccSpeed; }
            set { _decSpeed = value; RaisePropertyChanged(); }
        }

        public double GetConfiguredDecSpeed()
        {
            return _decSpeed;
        }

        private double _killDecSpeed;

        public double KillDecSpeed
        {
            get { return _killDecSpeed > 0d ? _killDecSpeed : AccSpeed * 10d; }
            set { _killDecSpeed = value; RaisePropertyChanged(); }
        }

        public double GetConfiguredKillDecSpeed()
        {
            return _killDecSpeed;
        }

        private double _jerk;

        public double Jerk
        {
            get { return _jerk > 0d ? _jerk : AccSpeed * 10d; }
            set { _jerk = value; RaisePropertyChanged(); }
        }

        public double GetConfiguredJerk()
        {
            return _jerk;
        }

    }
}

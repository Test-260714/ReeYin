using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    /// <summary>
    /// 旋转方向
    /// </summary>
    public enum DirOfRotation
    {
        顺时针,
        逆时针
    }

    /// <summary>
    /// 绘制圆弧方式
    /// (理论上起点和终点肯定都是知道的)
    /// </summary>
    public enum DrawArc
    {
        Center,
        Radius,
        //知道角度，圆心和半径都可以计算出来
        Angle,
    }

    [Serializable]
    public class AxisModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private bool _isInching;
        /// <summary>
        /// 是点动
        /// </summary>
        public bool IsInching
        {
            get { return _isInching; }
            set { _isInching = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private EN_SpeedType _curSpeedType;
        /// <summary>
        /// 当前速度类型
        /// </summary>
        public EN_SpeedType CurSpeedType
        {
            get { return _curSpeedType; }
            set { _curSpeedType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CoordinatePos _moveTargetPos;
        /// <summary>
        /// 移动目标位置
        /// </summary>
        public CoordinatePos MoveTargetPos
        {
            get { return _moveTargetPos; }
            set { _moveTargetPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _stepLenght = 1.1111;
        /// <summary>
        /// 步长
        /// </summary>
        public double StepLenght
        {
            get { return _stepLenght; }
            set { _stepLenght = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double[] _curPosInfos;
        /// <summary>
        /// 当前位置信息
        /// </summary>
        [JsonIgnore]
        public double[] CurPosInfos
        {
            get { return _curPosInfos; }
            set { _curPosInfos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double[] _curSpeedInfos;
        /// <summary>
        /// 当前实际速度信息
        /// </summary>
        [JsonIgnore]
        public double[] CurSpeedInfos
        {
            get { return _curSpeedInfos; }
            set { _curSpeedInfos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double[] _curCore1PosInfos;
        /// <summary>
        /// 当前核1位置信息
        /// </summary>
        [JsonIgnore]
        public double[] CurCore1PosInfos
        {
            get { return _curCore1PosInfos; }
            set { _curCore1PosInfos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PosComparisonOutputParam _posComparisonParam = new PosComparisonOutputParam();
        /// <summary>
        /// 位置比较参数
        /// </summary>
        public PosComparisonOutputParam PosComparisonParam
        {
            get { return _posComparisonParam; }
            set { _posComparisonParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public AxisModel()
        {
            
        }
        #endregion

        #region Methods

        #endregion

    }

    /// <summary>
    /// 插补坐标系参数
    /// </summary>
    [Serializable]
    public class InterPoCoordinateSystem : BindableBase
    {
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
        private int _axisCount = 2;
        /// <summary>
        /// 参与插补的轴数量，最少2轴，最多5轴
        /// </summary>
        public int AxisCount
        {
            get { return _axisCount; }
            set
            {
                _axisCount = Math.Max(2, Math.Min(5, value));
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
                RaisePropertyChanged(nameof(IsAxis3Enabled));
                RaisePropertyChanged(nameof(IsAxis4Enabled));
                RaisePropertyChanged(nameof(IsAxis5Enabled));
            }
        }

        [JsonIgnore]
        private En_AxisNum _axis1 = En_AxisNum.X;
        /// <summary>
        /// 参与插补的第一轴
        /// </summary>
        public En_AxisNum Axis1
        {
            get { return _axis1; }
            set
            {
                _axis1 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
            }
        }

        [JsonIgnore]
        private En_AxisNum _axis2 = En_AxisNum.Y;
        /// <summary>
        /// 参与插补的第二轴
        /// </summary>
        public En_AxisNum Axis2
        {
            get { return _axis2; }
            set
            {
                _axis2 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
            }
        }

        [JsonIgnore]
        private En_AxisNum _axis3 = En_AxisNum.Z;
        /// <summary>
        /// 参与插补的第三轴
        /// </summary>
        public En_AxisNum Axis3
        {
            get { return _axis3; }
            set
            {
                _axis3 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
            }
        }

        [JsonIgnore]
        private En_AxisNum _axis4 = En_AxisNum.R;
        /// <summary>
        /// 参与插补的第四轴
        /// </summary>
        public En_AxisNum Axis4
        {
            get { return _axis4; }
            set
            {
                _axis4 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
            }
        }

        [JsonIgnore]
        private En_AxisNum _axis5 = En_AxisNum.X1;
        /// <summary>
        /// 参与插补的第五轴
        /// </summary>
        public En_AxisNum Axis5
        {
            get { return _axis5; }
            set
            {
                _axis5 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(InterPoAxiss));
                RaisePropertyChanged(nameof(InterPoAxisDescription));
            }
        }

        [JsonIgnore]
        private double _startSpeed = 10;
        /// <summary>
        /// 合成起始速度
        /// </summary>
        public double StartSpeed
        {
            get { return _startSpeed; }
            set { _startSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _maxSpeed = 50;
        /// <summary>
        /// 合成最大速度
        /// </summary>
        public double MaxSpeed
        {
            get { return _maxSpeed; }
            set { _maxSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _accSpeed = 200;
        /// <summary>
        /// 合成加速度
        /// </summary>
        public double AccSpeed
        {
            get { return _accSpeed; }
            set { _accSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _endSpeed = 0;
        /// <summary>
        /// 终点速度
        /// </summary>
        public double EndSpeed
        {
            get { return _endSpeed; }
            set { _endSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _pulseEquivalent = 10000;
        /// <summary>
        /// 脉冲当量/mm（理论上建立的坐标系，对应的轴的脉冲当量需要一致）
        /// </summary>
        public double PulseEquivalent
        {
            get { return _pulseEquivalent; }
            set { _pulseEquivalent = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 插补坐标系名称
        /// </summary>
        [JsonIgnore]
        public string Name => string.Concat(GetSelectedAxes().Select(axis => axis.ToString()));

        /// <summary>
        /// 参与插补的轴组
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<En_AxisNum> InterPoAxiss => new ObservableCollection<En_AxisNum>(GetSelectedAxes());

        /// <summary>
        /// 参与轴描述
        /// </summary>
        [JsonIgnore]
        public string InterPoAxisDescription => string.Join(", ", GetSelectedAxes());

        /// <summary>
        /// 轴数量可选项
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<int> AxisCountOptions => new ObservableCollection<int> { 2, 3, 4, 5 };

        /// <summary>
        /// 第三轴是否参与
        /// </summary>
        [JsonIgnore]
        public bool IsAxis3Enabled => AxisCount >= 3;

        /// <summary>
        /// 第四轴是否参与
        /// </summary>
        [JsonIgnore]
        public bool IsAxis4Enabled => AxisCount >= 4;

        /// <summary>
        /// 第五轴是否参与
        /// </summary>
        [JsonIgnore]
        public bool IsAxis5Enabled => AxisCount >= 5;

        public bool TryValidate(out string message)
        {
            message = string.Empty;

            if (AxisCount < 2 || AxisCount > 5)
            {
                message = "插补坐标系轴数量必须在2到5之间。";
                return false;
            }

            var selectedAxes = GetSelectedAxes().ToList();
            if (selectedAxes.Distinct().Count() != selectedAxes.Count)
            {
                message = "同一插补坐标系内的轴不能重复。";
                return false;
            }

            return true;
        }

        public InterPoCoordinateSystem Clone()
        {
            return new InterPoCoordinateSystem
            {
                IsUsing = IsUsing,
                AxisCount = AxisCount,
                Axis1 = Axis1,
                Axis2 = Axis2,
                Axis3 = Axis3,
                Axis4 = Axis4,
                Axis5 = Axis5,
                StartSpeed = StartSpeed,
                MaxSpeed = MaxSpeed,
                AccSpeed = AccSpeed,
                EndSpeed = EndSpeed,
                PulseEquivalent = PulseEquivalent
            };
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            AxisCount = _axisCount;
        }

        private IEnumerable<En_AxisNum> GetSelectedAxes()
        {
            yield return Axis1;
            yield return Axis2;

            if (AxisCount >= 3)
            {
                yield return Axis3;
            }

            if (AxisCount >= 4)
            {
                yield return Axis4;
            }

            if (AxisCount >= 5)
            {
                yield return Axis5;
            }
        }
    }

    /// <summary>
    /// 圆弧插补参数
    /// PS：圆弧插补时，只能选择终点位置和半径或圆心
    /// 目前只考虑XY的
    /// </summary>
    public class ArcInterPoParam
    {
        /// <summary>
        /// 绘制圆弧的方法
        /// </summary>
        public DrawArc DrawArcMethod { get; set; } = DrawArc.Center;

        /// <summary>
        /// 是否安全移动(默认开启)
        /// 安全移动时优先控制优先级较高的轴回到安全位置
        /// </summary>
        public bool IsSafetyMoving { get; set; } = true;

        /// <summary>
        /// 插补坐标系轴组
        /// </summary>
        public List<En_AxisNum> InterPoAxiss { get; set; } = new List<En_AxisNum>();

        /// <summary>
        /// 默认速度
        /// </summary>
        public EN_SpeedType DefaultSpeed { get; set; } = EN_SpeedType.Mid;

        /// <summary>
        /// 起点
        /// </summary>
        public Point Origin { get; set; }

        /// <summary>
        /// 圆心
        /// </summary>
        public Point Center { get; set; }

        /// <summary>
        /// 终点
        /// </summary>
        public Point Destination { get; set; }

        /// <summary>
        /// 旋转方向
        /// </summary>
        public DirOfRotation Dir { get; set; } = DirOfRotation.顺时针;

        /// <summary>
        /// 半径
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// 角度
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// 终点位置字典
        /// </summary>
        public Dictionary<En_AxisNum,double> FinalPosDic { get; set; }

        /// <summary>
        /// 等待停止
        /// </summary>
        public bool waitforend { get; set; } = true;

        /// <summary>
        /// 可选 Program Buffer 编号，支持 ACS 等需要 Buffer 执行插补的板卡。
        /// </summary>
        public int? BufferNo { get; set; }

        /// <summary>
        /// 可选 Program Buffer 等待超时时间。
        /// </summary>
        public int? Timeout { get; set; }

    }

    /// <summary>
    /// 直线插补脉冲输出参数
    /// </summary>
    public class LineInterpolationPulseOutputParam
    {
        /// <summary>
        /// 是否启用脉冲输出
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 脉冲宽度，单位 ms
        /// </summary>
        public double PulseWidth { get; set; } = 0.01d;

        /// <summary>
        /// 固定距离间隔，单位为插补用户单位
        /// </summary>
        public double Interval { get; set; } = 1d;

        /// <summary>
        /// 脉冲起始距离
        /// </summary>
        public double StartDistance { get; set; }

        /// <summary>
        /// 脉冲结束距离，小于等于起始距离时由 ACS 实现按线段长度计算
        /// </summary>
        public double EndDistance { get; set; }

        /// <summary>
        /// 是否将 LCI 通道路由到 ConfigOut
        /// </summary>
        public bool RouteConfigOutput { get; set; } = true;

        /// <summary>
        /// ConfigOut 输出序号
        /// </summary>
        public int ConfigOutputIndex { get; set; }

        /// <summary>
        /// ConfigOut 输出功能码
        /// </summary>
        public int ConfigOutputCode { get; set; } = 7;
    }

    /// <summary>
    /// 直线插补参数
    /// </summary>
    public class LineInterPoParam
    {
        /// <summary>
        /// 是否安全移动(默认开启)
        /// 安全移动时优先控制优先级较高的轴回到安全位置
        /// </summary>
        public bool IsSafetyMoving { get; set; } = true;

        /// <summary>
        /// 插补坐标系轴组
        /// </summary>
        public List<En_AxisNum> InterPoAxiss { get; set; } = new List<En_AxisNum>();

        /// <summary>
        /// 默认速度
        /// </summary>
        public EN_SpeedType DefaultSpeed { get; set; } = EN_SpeedType.Mid;

        /// <summary>
        /// 目标位置
        /// </summary>
        public double[] TargetPos { get; set; }

        /// <summary>
        /// 目标位置字典
        /// </summary>
        public Dictionary<En_AxisNum,double> TargetPosDic { get; set; }

        /// <summary>
        /// 下降速度
        /// </summary>
        public double[] decZSpeed { get; set; }

        /// <summary>
        /// 上抬速度
        /// </summary>
        public double[] upZSpeed { get; set; }

        /// <summary>
        /// 等待停止
        /// </summary>
        public bool waitforend { get; set; } = true;

        /// <summary>
        /// 可选 Program Buffer 编号，支持 ACS 等需要 Buffer 执行插补的板卡。
        /// </summary>
        public int? BufferNo { get; set; }

        /// <summary>
        /// 可选 Program Buffer 等待超时时间。
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// 可选脉冲输出参数，启用后由支持的控制卡在插补运动中同步输出脉冲。
        /// </summary>
        public LineInterpolationPulseOutputParam? PulseOutput { get; set; }

    }

    /// <summary>
    /// 自定义插补参数
    /// </summary>
    public class CustomInterPoParam
    {
        /// <summary>
        /// 是否安全移动(默认开启)
        /// 安全移动时优先控制优先级较高的轴回到安全位置
        /// </summary>
        public bool IsSafetyMoving { get; set; } = true;

        /// <summary>
        /// 插补坐标系轴组
        /// </summary>
        public List<En_AxisNum> InterPoAxiss { get; set; } = new List<En_AxisNum>();

        /// <summary>
        /// 默认速度
        /// </summary>
        public EN_SpeedType DefaultSpeed { get; set; } = EN_SpeedType.Mid;

        /// <summary>
        /// 目标位置
        /// </summary>
        public double[] TargetPos { get; set; }

        /// <summary>
        /// 目标位置字典(最终停止的位置)
        /// </summary>
        public Dictionary<En_AxisNum, double> TargetPosDic { get; set; }

        /// <summary>
        /// 最大运行速度字典
        /// </summary>
        public Dictionary<En_AxisNum, double> MaxSpeedDic { get; set; }
        /// <summary>
        /// 等待停止
        /// </summary>
        public bool waitforend { get; set; } = true;
    }

}

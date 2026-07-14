using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion.Models
{
    [Serializable]
    public class GoogolCustomModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<AxisStatus> _allAxisStatus = new ObservableCollection<AxisStatus>();
        /// <summary>
        /// 所有轴状态
        /// </summary>
        public ObservableCollection<AxisStatus> AllAxisStatus
        {
            get { return _allAxisStatus; }
            set { _allAxisStatus = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private AxisStatus _sltAxisStatus;
        /// <summary>
        /// 选中的轴的状态
        /// </summary>
        public AxisStatus SltAxisStatus
        {
            get { return _sltAxisStatus; }
            set { _sltAxisStatus = value; RaisePropertyChanged(); }
        }


        #region (PSO)位置比较
        [JsonIgnore]
        private ObservableCollection<string> _psoOutputStatus = new ObservableCollection<string>();
        /// <summary>
        /// 输出状态
        /// </summary>
        public ObservableCollection<string> PSOOutputStatus
        {
            get { return _psoOutputStatus; }
            set { _psoOutputStatus = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PosComparisonParam _posComparisonParam = new PosComparisonParam();
        /// <summary>
        /// 位置比较输出参数
        /// </summary>
        public PosComparisonParam PosComparisonParam
        {
            get { return _posComparisonParam; }
            set { _posComparisonParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlPower _hso0 = ControlPower.激光开关;
        /// <summary>
        /// HSO0控制权(默认是激光控制)
        /// </summary>
        public ControlPower HSO0
        {
            get { return _hso0; }
            set { _hso0 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlPower _hso1 = ControlPower.激光PWM;
        /// <summary>
        /// HSO1控制权(默认是激光控制)
        /// </summary>
        public ControlPower HSO1
        {
            get { return _hso1; }
            set { _hso1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlPower _hso2 = ControlPower.第一路位置比较;
        /// <summary>
        /// HSO2控制权(默认是第一路位置比较)
        /// </summary>
        public ControlPower HSO2
        {
            get { return _hso2; }
            set { _hso2 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlPower _hso3 = ControlPower.第二路位置比较;
        /// <summary>
        /// HSO3控制权(默认是第二路位置比较)
        /// </summary>
        public ControlPower HSO3
        {
            get { return _hso3; }
            set { _hso3 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<string> _readHsoControlPower = new ObservableCollection<string>()
        {
            "0","1","2","3"
        };
        /// <summary>
        /// 读取控制权[0-3]
        /// </summary>
        public ObservableCollection<string> ReadHSOControlPower
        {
            get { return _readHsoControlPower; }
            set { _readHsoControlPower = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short _oncehardChannel = 0;
        /// <summary>
        /// 硬件通道
        /// </summary>
        public short OnceHardChannel
        {
            get { return _oncehardChannel; }
            set { _oncehardChannel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short _oncePSOIndex = 0;
        /// <summary>
        /// 单次位置比较索引
        /// </summary>
        public short OncePSOIndex
        {
            get { return _oncePSOIndex; }
            set { _oncePSOIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort _oncePulseWidth;
        /// <summary>
        /// 单次位置比较脉宽
        /// </summary>
        public ushort OncePulseWidth
        {
            get { return _oncePulseWidth; }
            set { _oncePulseWidth = value; RaisePropertyChanged(); }
        }



        #endregion

        #endregion

        #region Constructor
        public GoogolCustomModel()
        {
            AllAxisStatus.AddRange(new ObservableCollection<AxisStatus>
            {
                new AxisStatus
                {
                    Name = "轴1",
                    PrfPos = 0,
                    RealPos = 0,
                    PrfSpeed = 0,
                    RealSpeed = 0,
                    Enabled = true,
                    Alarm = false,
                    PLimit = false,
                    NLimit = false,
                    IsPrf = false,
                    IsArrival = false,
                    IsSmoothStop = false,
                    IsAbruptStop = false,
                    IsFollowError = false,
                    PrfMode = 0,
                },
                new AxisStatus
                {
                    Name = "轴2",
                    PrfPos = 0,
                    RealPos = 0,
                    PrfSpeed = 0,
                    RealSpeed = 0,
                    Enabled = true,
                    Alarm = false,
                    PLimit = false,
                    NLimit = false,
                    IsPrf = false,
                    IsArrival = false,
                    IsSmoothStop = false,
                    IsAbruptStop = false,
                    IsFollowError = false,
                    PrfMode = 0,
                },
                new AxisStatus
                {
                    Name = "轴3",
                    PrfPos = 0,
                    RealPos = 0,
                    PrfSpeed = 0,
                    RealSpeed = 0,
                    Enabled = true,
                    Alarm = false,
                    PLimit = false,
                    NLimit = false,
                    IsPrf = false,
                    IsArrival = false,
                    IsSmoothStop = false,
                    IsAbruptStop = false,
                    IsFollowError = false,
                    PrfMode = 0,
                },
                new AxisStatus
                {
                    Name = "轴4",
                    PrfPos = 0,
                    RealPos = 0,
                    PrfSpeed = 0,
                    RealSpeed = 0,
                    Enabled = true,
                    Alarm = false,
                    PLimit = false,
                    NLimit = false,
                    IsPrf = false,
                    IsArrival = false,
                    IsSmoothStop = false,
                    IsAbruptStop = false,
                    IsFollowError = false,
                    PrfMode = 0,
                },
            });
        }
        #endregion

        #region Methods

        #endregion

        #region Commands

        #endregion

    }

    /// <summary>
    /// 控制权
    /// </summary>
    public enum ControlPower
    {
        第一路位置比较,
        第二路位置比较,
        激光开关,
        激光PWM,
    }

    /// <summary>
    /// 位置比较参数
    /// </summary>
    public class PosComparisonParam : BindableBase
    {
        public short fifo_alive = 0;
        public int S;

        [JsonIgnore]
        private short psoIndex = 1;
        /// <summary>
        /// 位置比较索引[1,8]
        /// </summary>
        public short PSOIndex
        {
            get { return psoIndex; }
            set { psoIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short compareMode = 0;
        /// <summary>
        /// 位置比较输出模式选择
        /// 0:FIFO模式/1:Linear模式/2:PSO立即模式/3:PSO等待到位模式
        /// </summary>
        public short CompareMode
        {
            get { return compareMode; }
            set { compareMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short compareDimension = 2;
        /// <summary>
        /// 维数
        /// 1:一维模式/2:二维模式
        /// </summary>
        public short CompareDimension
        {
            get { return compareDimension; }
            set { compareDimension = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short compare_X = 1;
        /// <summary>
        /// 位置比较输出轴号X
        /// </summary>
        public short Compare_X
        {
            get { return compare_X; }
            set { compare_X = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short compare_Y = 2;
        /// <summary>
        /// 位置比较输出轴号Y
        /// </summary>
        public short Compare_Y
        {
            get { return compare_Y; }
            set { compare_Y = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort comparePulseWidth = 1000;
        /// <summary>
        /// 位置比较输出脉冲宽度，单位us
        /// </summary>
        public ushort ComparePulseWidth
        {
            get { return comparePulseWidth; }
            set { comparePulseWidth = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short _compareOutputMode = 0;
        /// <summary>
        /// 输出信号模式
        /// 0:脉冲/1:电平
        /// </summary>
        public short CompareOutputMode
        {
            get { return _compareOutputMode; }
            set { _compareOutputMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private short _sourceMode = 1;
        /// <summary>
        /// 比较源
        /// </summary>
        public short SourceMode
        {
            get { return _sourceMode; }
            set { _sourceMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort _compareErrBand = 1;
        /// <summary>
        /// 误差带设置
        /// </summary>
        public ushort CompareErrBand
        {
            get { return _compareErrBand; }
            set { _compareErrBand = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort _space = 1;
        /// <summary>
        /// 空间
        /// </summary>
        public ushort Space
        {
            get { return _space; }
            set { _space = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _syncPos = 100;
        /// <summary>
        /// 等间距触发间隔，XY轴合成间隔
        /// </summary>
        public int SyncPos
        {
            get { return _syncPos; }
            set { _syncPos = value; }
        }


    }

    /// <summary>
    /// 轴状态
    /// </summary>
    public class AxisStatus : BindableBase
    {
        [JsonIgnore]
        private string _name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _prfPos;
        /// <summary>
        /// 规划器位置
        /// </summary>
        public double PrfPos
        {
            get { return _prfPos; }
            set { _prfPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _realPos;
        /// <summary>
        /// 实际位置
        /// </summary>
        public double RealPos
        {
            get { return _realPos; }
            set { _realPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _prfSpeed;
        /// <summary>
        /// 规划器速度
        /// </summary>
        public double PrfSpeed
        {
            get { return _prfSpeed; }
            set { _prfSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _realSpeed;
        /// <summary>
        /// 实际速度
        /// </summary>
        public double RealSpeed
        {
            get { return _realSpeed; }
            set { _realSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _enabled;
        /// <summary>
        /// 使能
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _alarm;
        /// <summary>
        /// 报警
        /// </summary>
        public bool Alarm
        {
            get { return _alarm; }
            set { _alarm = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _pLimit;
        /// <summary>
        /// 正限位
        /// </summary>
        public bool PLimit
        {
            get { return _pLimit; }
            set { _pLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _nLimit;
        /// <summary>
        /// 负限位
        /// </summary>
        public bool NLimit
        {
            get { return _nLimit; }
            set { _nLimit = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isPrf;
        /// <summary>
        /// 规划器
        /// </summary>
        public bool IsPrf
        {
            get { return _isPrf; }
            set { _isPrf = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isArrival;
        /// <summary>
        /// 到位
        /// </summary>
        public bool IsArrival
        {
            get { return _isArrival; }
            set { _isArrival = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSmoothStop;
        /// <summary>
        /// 平滑停止
        /// </summary>
        public bool IsSmoothStop
        {
            get { return _isSmoothStop; }
            set { _isSmoothStop = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isAbruptStop;
        /// <summary>
        /// 紧急停止
        /// </summary>
        public bool IsAbruptStop
        {
            get { return _isAbruptStop; }
            set { _isAbruptStop = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isFollowError;
        /// <summary>
        /// 跟随误差超限
        /// </summary>
        public bool IsFollowError
        {
            get { return _isFollowError; }
            set { _isFollowError = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _prfMode;
        /// <summary>
        /// 运动模式
        /// </summary>
        public int PrfMode
        {
            get { return _prfMode; }
            set { _prfMode = value; RaisePropertyChanged(); }
        }

    }
}

using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
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
    [Serializable]
    public class ControlCardConfigModel : BindableBase, IHardwareModule, IStartupResetHardwareModule
    {
        #region Fields
        [NonSerialized]
        private InterPoCoordinateSystem _observedInterPoCoordinateSystem;
        #endregion

        #region Properties

        [JsonIgnore]
        private ObservableCollection<ControlCardBase> _cardModels = new ObservableCollection<ControlCardBase>();
        /// <summary>
        /// 所有控制卡集合
        /// </summary>
        public ObservableCollection<ControlCardBase> CardModels
        {
            get { return _cardModels; }
            set { _cardModels = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlCardBase _curSltCard;
        /// <summary>
        /// 当前选中的控制卡
        /// </summary>
        public ControlCardBase CurSltCard
        {
            get 
            {
                if(_curSltCard != null)
                    IsCurSltCardIsNotNull = true;
                else
                    IsCurSltCardIsNotNull = false;

                return _curSltCard; 
            }
            set
            {
                _curSltCard = value;
                IsCurSltCardIsNotNull = _curSltCard != null;
                IsCurSltCardAcs = IsAcsCard(_curSltCard);
                _curSltCard?.Config?.EnsureInterpolationCoordinateSystems();
                SltInterPoCoordinateSystem = _curSltCard?.Config?.GetMatchedInterpolationCoordinateSystem(_curSltCard?.Config?.DefaultInterpCS)
                    ?? _curSltCard?.Config?.InterpolationCoordinateSystems?.FirstOrDefault();
                RaisePropertyChanged();
            }
        }
        [JsonIgnore]
        private bool _isCurSltCardIsNotNull;
        /// <summary>
        /// 选中的控制卡为空时，不允许操作
        /// </summary>
        [JsonIgnore]
        public bool IsCurSltCardIsNotNull
        {
            get { return _isCurSltCardIsNotNull; }
            set { _isCurSltCardIsNotNull = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isCurSltCardAcs;

        [JsonIgnore]
        public bool IsCurSltCardAcs
        {
            get => _isCurSltCardAcs;
            private set { _isCurSltCardAcs = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SingleAxisParam _sltAxis = new SingleAxisParam();
        /// <summary>
        /// 当前选中的轴
        /// </summary>
        public SingleAxisParam SltAxis
        {
            get { return _sltAxis; }
            set { _sltAxis = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _SltCardName = "";
        /// <summary>
        /// 选择的卡类型
        /// </summary>
        public string SltCardName
        {
            get { return _SltCardName; }
            set { _SltCardName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _SltVendorType;
        /// <summary>
        /// 选择的厂家类型
        /// </summary>
        public string SltVendorType
        {
            get { return _SltVendorType; }
            set { _SltVendorType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<LimitPos> _allLimitPos = new ObservableCollection<LimitPos>();
        /// <summary>
        /// 所有位置限制
        /// </summary>
        public ObservableCollection<LimitPos> AllLimitPos
        {
            get { return _allLimitPos; }
            set { _allLimitPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isAutoResetOnStartup;
        /// <summary>
        /// 软件启动并完成控制卡初始化后是否自动复位。
        /// </summary>
        public bool IsAutoResetOnStartup
        {
            get { return _isAutoResetOnStartup; }
            set { _isAutoResetOnStartup = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _startupAutoResetTimeoutSeconds = 60;
        /// <summary>
        /// 启动自动复位的页面提示超时时间，单位秒。
        /// </summary>
        public int StartupAutoResetTimeoutSeconds
        {
            get { return _startupAutoResetTimeoutSeconds; }
            set
            {
                _startupAutoResetTimeoutSeconds = value <= 0 ? 60 : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private bool _isAxisViewIoJogEnabled;
        /// <summary>
        /// 是否启用 AxisView 面板的输入 IO 方向控制。
        /// </summary>
        public bool IsAxisViewIoJogEnabled
        {
            get { return _isAxisViewIoJogEnabled; }
            set { _isAxisViewIoJogEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _axisViewIoJogUpInputPort = 3;
        /// <summary>
        /// AxisView 面板向上移动使用的输入 IO 端口。
        /// </summary>
        [DefaultValue(3)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int AxisViewIoJogUpInputPort
        {
            get { return _axisViewIoJogUpInputPort; }
            set { _axisViewIoJogUpInputPort = value < 0 ? 0 : value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _axisViewIoJogDownInputPort = 4;
        /// <summary>
        /// AxisView 面板向下移动使用的输入 IO 端口。
        /// </summary>
        [DefaultValue(4)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int AxisViewIoJogDownInputPort
        {
            get { return _axisViewIoJogDownInputPort; }
            set { _axisViewIoJogDownInputPort = value < 0 ? 0 : value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _axisViewIoJogLeftInputPort = 5;
        /// <summary>
        /// AxisView 面板向左移动使用的输入 IO 端口。
        /// </summary>
        [DefaultValue(5)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int AxisViewIoJogLeftInputPort
        {
            get { return _axisViewIoJogLeftInputPort; }
            set { _axisViewIoJogLeftInputPort = value < 0 ? 0 : value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _axisViewIoJogRightInputPort = 6;
        /// <summary>
        /// AxisView 面板向右移动使用的输入 IO 端口。
        /// </summary>
        [DefaultValue(6)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int AxisViewIoJogRightInputPort
        {
            get { return _axisViewIoJogRightInputPort; }
            set { _axisViewIoJogRightInputPort = value < 0 ? 0 : value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LimitPos _sltLimitPos;
        /// <summary>
        /// 选中的限制
        /// </summary>
        public LimitPos SltLimitPos
        {
            get { return _sltLimitPos; }
            set { _sltLimitPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private InterPoCoordinateSystem _sltInterPoCoordinateSystem;
        /// <summary>
        /// 选中的插补坐标系
        /// </summary>
        public InterPoCoordinateSystem SltInterPoCoordinateSystem
        {
            get { return _sltInterPoCoordinateSystem; }
            set
            {
                if (_observedInterPoCoordinateSystem != null)
                {
                    _observedInterPoCoordinateSystem.PropertyChanged -= OnSelectedInterpolationCoordinateSystemPropertyChanged;
                }

                _sltInterPoCoordinateSystem = value;
                _observedInterPoCoordinateSystem = value;

                if (_observedInterPoCoordinateSystem != null)
                {
                    _observedInterPoCoordinateSystem.PropertyChanged += OnSelectedInterpolationCoordinateSystemPropertyChanged;
                }

                SyncDefaultInterpolationCoordinateSystem();
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Constructor
        public ControlCardConfigModel()
        {

        }
        #endregion

        #region Methods
        public InitResult Init()
        {
            Dictionary<string, bool> CardStatus = new Dictionary<string, bool>();

            for (var index = 0; index < CardModels.Count; index++)
            {
                var card = CardModels[index];
                if (card == null)
                {
                    continue;
                }

                var cardName = string.IsNullOrWhiteSpace(card.NickName) ? $"ControlCard{index + 1}" : $"{card.NickName}{index + 1}";
                try
                {
                    CardStatus[cardName] = card.Initialized || card.Init();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"控制卡初始化异常：{ex.Message}");
                    CardStatus[cardName] = false;
                }
            }

            InitResult result = new InitResult();

            if (CardStatus.Values.Any(value => value == false))
            {
                result = new InitResult
                {
                    Message = "连接失败！",
                    Success = false,
                };
            }
            else
            {
                result = new InitResult
                {
                    Message = "连接成功！",
                    Success = true,
                };
            }

            return result;
        }

        public async Task<InitResult> ExecuteStartupResetAsync(Action<string> updateMessage)
        {
            if (!IsAutoResetOnStartup)
            {
                return new InitResult { Success = true, Message = "控制卡启动自动复位未启用。" };
            }

            var card = ResolveStartupResetCard();
            if (card == null)
            {
                return new InitResult { Success = false, Message = "未找到已初始化且已连接的控制卡，启动自动复位失败。" };
            }

            try
            {
                var cardName = string.IsNullOrWhiteSpace(card.NickName) ? "控制卡" : card.NickName;
                updateMessage?.Invoke($"{cardName}启动复位中...");
                var resetResult = await Task.Run(() =>
                {
                    var success = card.GoHome(out var message);
                    return new InitResult { Success = success, Message = message };
                });

                if (!resetResult.Success)
                {
                    return new InitResult { Success = false, Message = $"{cardName}启动自动复位失败：{resetResult.Message}" };
                }

                updateMessage?.Invoke($"{cardName}启动复位完成。");
                return new InitResult { Success = true, Message = $"{cardName}启动复位完成。" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"控制卡启动自动复位异常：{ex.Message}");
                return new InitResult { Success = false, Message = $"控制卡启动自动复位异常：{ex.Message}" };
            }
        }

        private ControlCardBase? ResolveStartupResetCard()
        {
            var card = IsStartupResetCardReady(CurSltCard)
                ? CurSltCard
                : CardModels.FirstOrDefault(IsStartupResetCardReady);

            if (card != null && !ReferenceEquals(CurSltCard, card))
            {
                CurSltCard = card;
            }

            return card;
        }

        private static bool IsStartupResetCardReady(ControlCardBase? card)
        {
            return card?.Initialized == true && card.IsConnected;
        }

        public void Shutdown()
        {
            foreach (var Card in CardModels)
            {
                Card.Close();
            }
        }

        public void RefreshStatus()
        {
            foreach (var Card in CardModels)
            {
                Card.State = Card.State;
            }
        }

        private static bool IsAcsCard(ControlCardBase? card)
        {
            return card != null &&
                   (string.Equals(card.VenderName, "ACS", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(card.GetType().FullName, "ReeYin_V.Hardware.ControlCard.ACS.App.AcsControlCard", StringComparison.Ordinal));
        }

        private void OnSelectedInterpolationCoordinateSystemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SyncDefaultInterpolationCoordinateSystem();
        }

        private void SyncDefaultInterpolationCoordinateSystem()
        {
            _curSltCard?.Config?.SyncDefaultInterpolationCoordinateSystem(_sltInterPoCoordinateSystem);
        }


        #endregion

    }

    public enum Condition
    {
        大于,
        小于

    }

    public enum LimitTriggerCondition
    {
        大于,
        大于等于,
        等于,
        小于等于,
        小于
    }

    /// <summary>
    /// 限位配置
    /// </summary>
    [Serializable]
    public class LimitPos : BindableBase
    {
        [JsonIgnore]
        private En_AxisNum _limitAxisNum = En_AxisNum.X;
        /// <summary>
        /// 被限制轴号
        /// </summary>
        public En_AxisNum LimitAxisNum
        {
            get { return _limitAxisNum; }
            set { _limitAxisNum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private En_AxisNum _byLimitAxisNum = En_AxisNum.Z;
        /// <summary>
        /// 限制轴号
        /// </summary>
        public En_AxisNum ByLimitAxisNum
        {
            get { return _byLimitAxisNum; }
            set { _byLimitAxisNum = value; RaisePropertyChanged(); }
        }

        private LimitTriggerCondition _triggerCondition = LimitTriggerCondition.大于;
        /// <summary>
        /// 条件轴触发方式
        /// </summary>
        public LimitTriggerCondition TriggerCondition
        {
            get { return _triggerCondition; }
            set { _triggerCondition = value; RaisePropertyChanged(); }
        }

        private Condition _condition = Condition.大于;
        /// <summary>
        /// 旧版单边限位的比较条件，保留用于兼容历史配置
        /// </summary>
        public Condition Condition
        {
            get { return _condition; }
            set { _condition = value; RaisePropertyChanged(); }
        }

        private double _limitValue = 0.0;
        /// <summary>
        /// 条件轴阈值
        /// </summary>
        public double LimitValue
        {
            get { return _limitValue; }
            set { _limitValue = value; RaisePropertyChanged(); }
        }

        private double? _byLimitValue;
        /// <summary>
        /// 旧版单边限位阈值，保留用于兼容历史配置
        /// </summary>
        public double? ByLimitValue
        {
            get { return _byLimitValue; }
            set { _byLimitValue = value; RaisePropertyChanged(); }
        }

        private double? _minLimitValue;
        /// <summary>
        /// 受限轴最小允许值
        /// </summary>
        public double? MinLimitValue
        {
            get { return _minLimitValue; }
            set { _minLimitValue = value; RaisePropertyChanged(); }
        }

        private double? _maxLimitValue;
        /// <summary>
        /// 受限轴最大允许值
        /// </summary>
        public double? MaxLimitValue
        {
            get { return _maxLimitValue; }
            set { _maxLimitValue = value; RaisePropertyChanged(); }
        }

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
        public bool IsRangeLimitConfigured => MinLimitValue.HasValue || MaxLimitValue.HasValue;

        [JsonIgnore]
        public string LimitRangeDescription
        {
            get
            {
                var minText = MinLimitValue.HasValue ? $"{MinLimitValue.Value:F3}" : "-inf";
                var maxText = MaxLimitValue.HasValue ? $"{MaxLimitValue.Value:F3}" : "+inf";
                return $"{LimitAxisNum}轴[{minText}, {maxText}]";
            }
        }

        public bool TryValidate(out string message)
        {
            if (!IsUsing)
            {
                message = string.Empty;
                return true;
            }

            if (LimitAxisNum == ByLimitAxisNum)
            {
                message = "受限轴和条件轴不能设置为同一轴。";
                return false;
            }

            if (IsRangeLimitConfigured)
            {
                if (!MinLimitValue.HasValue && !MaxLimitValue.HasValue)
                {
                    message = "请至少配置一个运动区域边界。";
                    return false;
                }

                if (MinLimitValue.HasValue && MaxLimitValue.HasValue &&
                    MinLimitValue.Value > MaxLimitValue.Value)
                {
                    message = "最小值不能大于最大值。";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (!ByLimitValue.HasValue)
            {
                message = "旧版单边限位缺少限制值。";
                return false;
            }

            message = string.Empty;
            return true;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            // 历史数据仍然保留 ByLimitValue / Condition 语义，新数据优先走范围限位。
            if (IsRangeLimitConfigured)
            {
                return;
            }

            if (!ByLimitValue.HasValue)
            {
                return;
            }
        }

    }
}

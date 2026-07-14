using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Condition = ReeYin_V.Hardware.ControlCard.Models.Condition;
using LimitTriggerCondition = ReeYin_V.Hardware.ControlCard.Models.LimitTriggerCondition;

namespace ReeYin_V.Hardware.ControlCard
{
    public enum EN_SysState
    {
        None = 0,
        IsServoAlarm = 1,
        IsNeedReset = 2,
        IsReseting = 3,


        IsOtherError = 99,
    }

    /// <summary>
    /// 控制卡的抽象父类
    /// </summary>
    [Serializable]
    public abstract class ControlCardBase : BindableBase,IControlCard
    {
        #region Fields
        [JsonIgnore]
        public bool IsReady => Initialized && (!RequiresHomeBeforeMove || IsAxisHomed);

        [JsonIgnore]
        protected virtual int MotionTimeoutMs => 60000;

        [JsonIgnore]
        protected virtual bool RequiresHomeBeforeMove => true;

        //私服报警
        [JsonIgnore]
        public bool IsServoAlarm = false;

        //其他错误
        [JsonIgnore]
        public bool IsOtherError = false;

        //需要复位
        [JsonIgnore]
        public bool IsNeedReset = true;

        //正在复位
        [JsonIgnore]
        public bool IsReseting = false;

        #endregion

        #region Properties
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
        private string _cardType = "";
        /// <summary>
        /// 厂家定义类型
        /// </summary>
        public string CardType
        {
            get { return _cardType; }
            set { _cardType = value; RaisePropertyChanged(); }
        }

        public float Height_Content_Z { get; set; } = 0f;

        public float Height_Z { get; set; } = 0f;

        [JsonIgnore]
        private bool _isConnected = false;

        /// <summary>
        /// 初始连接状态
        /// </summary>
        [JsonIgnore]
        public bool IsConnected
        {
            get { return _isConnected; }
            set { SetProperty(ref _isConnected, value, new Action(() => PrismProvider.EventAggregator?.GetEvent<HardwareChangedEvent>().Publish())); }
        }

        /// <summary>
        /// 是否初始化完成
        /// </summary>
        [JsonIgnore]
        public bool Initialized { get; private set; }

        /// <summary>
        /// 当前所有轴的位置信息位置
        /// </summary>
        [JsonIgnore]
        public double[] CurPos { get; set; }

        /// <summary>
        /// 当前所有轴的脉冲位置
        /// </summary>
        [JsonIgnore]
        public int[] CurPulse { get; set; }

        /// <summary>
        /// 当前所有轴的实际速度
        /// </summary>
        [JsonIgnore]
        public double[] CurSpeed { get; set; }

        /// <summary>
        /// 运动轴是否回零
        /// </summary>
        [JsonIgnore]
        public bool IsAxisHomed { get; set; }

        /// <summary>
        /// 运动轴正在回零中
        /// </summary>
        [JsonIgnore]
        public bool IsAxisHoming { get; set; }

        /// <summary>
        /// 轴运动速度模式
        /// </summary>
        [JsonIgnore]
        protected EN_SpeedType SpeedMode { get; private set; }

        [JsonIgnore]
        private ControlCardConfig _config = new ControlCardConfig();
        /// <summary>
        /// 控制卡的配置参数
        /// </summary>
        public ControlCardConfig Config
        {
            get { return _config; }
            set
            {
                _config = value ?? new ControlCardConfig();
                OnConfigChanged();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private HardwareState _state;
        [JsonIgnore]
        public HardwareState State
        {
            get { return _state; }
            set { _state = value;

                PrismProvider.EventAggregator?.GetEvent<HardwareStatusChangedEvent>().Publish(new HardwareStatus
                {
                    Name = _nickName,
                    Status = _state,
                    IsConnect = _isConnected,
                    Describe = "",
                    SourceType = HardwareAlarmSources.MotionCard,
                    Location = BuildMotionCardLocation(),
                    ExtraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["IsConnect"] = _isConnected
                    },
                    Timestamp = DateTime.Now
                });
            }
        }

        #endregion

        #region Constructor
        protected ControlCardBase()
        {
            //订阅关闭硬件的事件通知
            PrismProvider.EventAggregator?.GetEvent<CloseAllHardwareEvent>().Subscribe(Close);
        }
        #endregion

        /// <summary>
        /// 获取AxisType枚举的元素集合
        /// </summary>
        protected IEnumerable<En_AxisNum> AxisTypes { get; private set; } = Enum.GetValues(typeof(En_AxisNum)).Cast<En_AxisNum>();

        public void Close()
        {
            if (Initialized)
            {
                DoStop(null, AxisStopMode.减速停止);
                DoClose();
                Initialized = false;
            }
        }

        protected virtual void OnConfigChanged()
        {
        }

        public IControlCardConfigProvider? Provider { get; private set; }

        public bool Init(/*IControlCardConfigProvider provider*/)
        {
            //Provider = provider;
            //Config = provider.ControlCardConfig;

            if (Initialized)
            {
                throw new Exception("重复初始化控制卡");
            }

            if (DoInit())
            {
                Initialized = true;
                return true;
            }

            return false;
        }


        public virtual bool Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message)
        {
            if (!IsReady)
            {
                message = "运动轴未准备好";
                return false;
            }

            if (double.IsNaN(um) || double.IsInfinity(um) || Math.Abs(um) <= double.Epsilon)
            {
                message = "移动距离必须大于 0";
                return false;
            }

            return MoveAxis(axisType, NormalizeRelativeDistance(moveDirection, um), out message);
        }

        /// <summary>
        /// 移动轴功能
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="um"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool MoveAxis(En_AxisNum axisType, double um, out string v)
        {
            //判断当前轴是否开启使能
            if (!DoGetAxisEnable(axisType))
            {
                v = "当前轴未开启使能";
                return false;
            }
            //判断当前轴是否正在运动
            if (!DoGetAxisStopped(axisType))
            {
                v = "当前轴正在运动中";
                return false;
            }

            if (!DoMoveAxis(axisType, um))
            {
                v = "当前轴运动启动失败";
                return false;
            }

            if (!WaitUntilAxisStopped(axisType, MotionTimeoutMs))
            {
                v = $"当前轴等待停止超时，超时时间 {MotionTimeoutMs}ms";
                return false;
            }

            v = "当前轴运动完成";
            return true;
        }

        /// <summary>
        /// 轴的连续运动
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="moveDirection"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Move(En_AxisNum axisType, MoveDirection moveDirection)
        {
            if (!IsReady)
            {
                return false;
            }

            //判断当前轴是否开启使能
            if (!DoGetAxisEnable(axisType))
            {
                return false;
            }
            //判断当前轴是否正在运动
            if (!DoGetAxisStopped(axisType))
            {
                return false;
            }

            return DoMoveContinue(axisType, moveDirection);
        }

        public void SetSpeedMode(EN_SpeedType mode)
        {
            SpeedMode = mode;
        }

        public void Stop(En_AxisNum? axisType)
        {
            DoStop(axisType, AxisStopMode.减速停止);
        }

        /// <summary>
        /// 轴回零
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool GoHome(out string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("hh-mm-ss.fff")}触发回零！");
            message = string.Empty;
            var overlayOperationId = Guid.NewGuid();
            var overlayTimeoutSeconds = ResolveResetOverlayTimeoutSeconds();
            PublishControlCardResetOverlay(true, overlayOperationId, overlayTimeoutSeconds, $"复位中，不允许操作。超时时间：{overlayTimeoutSeconds}秒");

            try
            {
                if (!Initialized)
                {
                    message = "控制卡未初始化";
                    Console.WriteLine(message);
                    return false;
                }

                DoStop(null, AxisStopMode.立即停止);

                Thread.Sleep(100);

                IsAxisHoming = true;
                IsAxisHomed = false;
                IsAxisHomed = DoGoHome(out message);
                return IsAxisHomed;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                IsAxisHomed = false;
                return false;
            }
            finally
            {
                IsAxisHoming = false;
                PublishControlCardResetOverlay(false, overlayOperationId, overlayTimeoutSeconds, message);
            }
        }

        private int ResolveResetOverlayTimeoutSeconds()
        {
            try
            {
                PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.ControlCard, out var moduleConfig);
                if (moduleConfig is ControlCardConfigModel controlCardConfig &&
                    controlCardConfig.StartupAutoResetTimeoutSeconds > 0)
                {
                    return controlCardConfig.StartupAutoResetTimeoutSeconds;
                }
            }
            catch
            {
                // Keep reset usable even if the global config is not ready yet.
            }

            return 60;
        }

        private void PublishControlCardResetOverlay(bool isRunning, Guid operationId, int timeoutSeconds, string message)
        {
            try
            {
                PrismProvider.EventAggregator?.GetEvent<ControlCardResetOverlayEvent>().Publish(new ControlCardResetOverlayPayload
                {
                    IsRunning = isRunning,
                    OperationId = operationId,
                    TimeoutSeconds = timeoutSeconds,
                    Message = message ?? string.Empty
                });
            }
            catch
            {
                // Overlay notification must not break the hardware reset flow.
            }
        }

        protected bool TryGetAxisConfig(En_AxisNum axisNum, out SingleAxisParam axis)
        {
            var matchedAxis = Config?.AllAxis?.FirstOrDefault(item => item != null && item.AxisNum == axisNum);
            if (matchedAxis == null)
            {
                axis = null!;
                return false;
            }

            axis = matchedAxis;
            return true;
        }

        protected IReadOnlyList<SingleAxisParam> GetConfiguredAxes(bool onlyUsing = true)
        {
            var axes = Config?.AllAxis?.Where(item => item != null).ToList() ?? new List<SingleAxisParam>();
            return onlyUsing ? axes.Where(item => item.IsUsing).ToList() : axes;
        }

        protected static short GetOneBasedAxisNo(SingleAxisParam axis)
        {
            return axis == null ? (short)1 : (short)Math.Max(1, (int)axis.AxisNo);
        }

        protected static short GetZeroBasedAxisNo(SingleAxisParam axis)
        {
            return axis == null ? (short)0 : (short)Math.Max(0, (int)axis.AxisNo - 1);
        }

        protected void EnsurePositionBuffers(int requiredLength = 0)
        {
            var count = Math.Max(requiredLength, Config?.AllAxis?.Count ?? 0);
            count = Math.Max(1, count);

            if (CurPos == null || CurPos.Length < count)
            {
                CurPos = new double[count];
            }

            if (CurPulse == null || CurPulse.Length < count)
            {
                CurPulse = new int[count];
            }
        }

        protected void EnsureSpeedBuffers(int requiredLength = 0)
        {
            var maxAxisNo = Config?.AllAxis?
                .Where(axis => axis != null)
                .Select(axis => Math.Max(1, (int)axis.AxisNo))
                .DefaultIfEmpty(0)
                .Max() ?? 0;
            var count = Math.Max(requiredLength, Math.Max(Config?.AllAxis?.Count ?? 0, maxAxisNo));
            count = Math.Max(1, count);

            if (CurSpeed == null || CurSpeed.Length < count)
            {
                CurSpeed = new double[count];
            }
        }

        protected SpeedSetting? ResolveSpeedSetting(En_AxisNum axisNum, EN_SpeedType? speedType = null)
        {
            if (!TryGetAxisConfig(axisNum, out var axis) || axis.SpeedDict1 == null || axis.SpeedDict1.Count == 0)
            {
                return null;
            }

            var requestedSpeedType = speedType ?? SpeedMode;
            return axis.SpeedDict1.FirstOrDefault(item => item.SpeedType == requestedSpeedType)
                   ?? axis.SpeedDict1.FirstOrDefault(item => item.SpeedType == EN_SpeedType.Work)
                   ?? axis.SpeedDict1.FirstOrDefault();
        }

        protected double GetAxisDefaultVelocity(En_AxisNum axisNum, EN_SpeedType? speedType = null)
        {
            return Math.Abs(ResolveSpeedSetting(axisNum, speedType)?.MaxSpeed ?? 100d);
        }

        protected static double NormalizeRelativeDistance(MoveDirection direction, double distance)
        {
            return direction == MoveDirection.反向 ? -Math.Abs(distance) : Math.Abs(distance);
        }

        protected bool WaitUntilAxisStopped(En_AxisNum axisNum, int timeoutMs, int pollingIntervalMs = 20)
        {
            var timeout = Math.Max(0, timeoutMs);
            var interval = Math.Max(1, pollingIntervalMs);
            var elapsed = 0;

            while (elapsed <= timeout)
            {
                if (DoGetAxisStopped(axisNum))
                {
                    return true;
                }

                if (elapsed == timeout)
                {
                    break;
                }

                var sleep = Math.Min(interval, timeout - elapsed);
                Thread.Sleep(sleep);
                elapsed += sleep;
            }

            return false;
        }

        protected bool WaitUntilAllAxesStopped(IEnumerable<En_AxisNum> axes, int timeoutMs, int pollingIntervalMs = 20)
        {
            var axisArray = axes?.Distinct().ToArray() ?? Array.Empty<En_AxisNum>();
            if (axisArray.Length == 0)
            {
                return true;
            }

            var timeout = Math.Max(0, timeoutMs);
            var interval = Math.Max(1, pollingIntervalMs);
            var elapsed = 0;

            while (elapsed <= timeout)
            {
                if (axisArray.All(DoGetAxisStopped))
                {
                    return true;
                }

                if (elapsed == timeout)
                {
                    break;
                }

                var sleep = Math.Min(interval, timeout - elapsed);
                Thread.Sleep(sleep);
                elapsed += sleep;
            }

            return false;
        }

        protected bool TryGetCurrentAxisPosition(En_AxisNum axisNum, out double position)
        {
            position = default;
            if (!TryGetAxisConfig(axisNum, out var axis))
            {
                return false;
            }

            if (CurPos != null && axis.AxisNo > 0 && axis.AxisNo <= CurPos.Length)
            {
                position = CurPos[axis.AxisNo - 1];
                return true;
            }

            position = axis.CurPos;
            return true;
        }

        protected bool TryBuildCoordinatedTargets(
            IReadOnlyList<En_AxisNum>? requestedAxes,
            IReadOnlyDictionary<En_AxisNum, double>? targetPositionMap,
            IReadOnlyList<double>? targetPositions,
            out En_AxisNum[] axisIds,
            out double[] targets,
            out Dictionary<En_AxisNum, double> targetForLimitValidation,
            out string message)
        {
            axisIds = Array.Empty<En_AxisNum>();
            targets = Array.Empty<double>();
            targetForLimitValidation = new Dictionary<En_AxisNum, double>();
            message = string.Empty;

            if (requestedAxes == null || requestedAxes.Count == 0)
            {
                message = "插补轴不能为空";
                return false;
            }

            axisIds = requestedAxes.ToArray();
            if (axisIds.Distinct().Count() != axisIds.Length)
            {
                message = "插补轴不能重复";
                return false;
            }

            targets = new double[axisIds.Length];
            for (var index = 0; index < axisIds.Length; index++)
            {
                var axisId = axisIds[index];
                if (targetPositionMap != null && targetPositionMap.TryGetValue(axisId, out var mappedPosition))
                {
                    targets[index] = mappedPosition;
                }
                else if (targetPositions != null && index < targetPositions.Count)
                {
                    targets[index] = targetPositions[index];
                }
                else
                {
                    message = $"缺少{axisId}轴目标位置";
                    return false;
                }

                if (double.IsNaN(targets[index]) || double.IsInfinity(targets[index]))
                {
                    message = $"{axisId}轴目标位置不是有效数值";
                    return false;
                }

                targetForLimitValidation[axisId] = targets[index];
            }

            return true;
        }

        /// <summary>
        /// 校验单个轴的目标坐标是否满足联动限位配置。
        /// 未传入的其他轴坐标默认取当前坐标参与判断。
        /// </summary>
        /// <param name="axisNum">目标轴号</param>
        /// <param name="targetPos">目标坐标</param>
        /// <param name="message">不满足限制时的提示信息</param>
        /// <param name="tolerance">坐标比较容差</param>
        /// <returns>满足配置返回 true，否则 false</returns>
        public virtual bool ValidateLimitPosition(En_AxisNum axisNum, double targetPos, out string message, double tolerance = 0.001d)
        {
            return ValidateLimitPosition(new Dictionary<En_AxisNum, double>
            {
                [axisNum] = targetPos
            }, out message, tolerance);
        }

        /// <summary>
        /// 校验输入的目标坐标是否满足联动限位配置。
        /// 规则语义：当“条件轴”满足触发条件时，“受限轴”必须处于允许运动范围内。
        /// 同时兼容历史单边限位规则。
        /// </summary>
        /// <param name="targetAxisPositions">待校验的目标坐标，可只传部分轴</param>
        /// <param name="message">不满足限制时的提示信息</param>
        /// <param name="tolerance">坐标比较容差</param>
        /// <returns>满足配置返回 true，否则 false</returns>
        public virtual bool ValidateLimitPosition(IDictionary<En_AxisNum, double> targetAxisPositions, out string message, double tolerance = 0.001d)
        {
            message = string.Empty;

            if (targetAxisPositions == null)
            {
                message = "待校验的轴坐标不能为空";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionSafetyError,
                    nameof(ValidateLimitPosition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["Reason"] = "TargetAxisPositionsNull"
                    });
                return false;
            }

            PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.ControlCard, out var moduleConfig);
            var module = moduleConfig as ControlCardConfigModel;
            var limitPositions = module?.AllLimitPos?
                .Where(item => item != null && item.IsUsing)
                .ToList();

            if (limitPositions == null || limitPositions.Count == 0)
            {
                return true;
            }

            var axisPositions = BuildAxisPositionSnapshot(targetAxisPositions);

            foreach (var limit in limitPositions)
            {
                if (!limit.TryValidate(out var validateMessage))
                {
                    message = $"限位配置无效：{validateMessage}";
                    ReportMotionAlarm(
                        HardwareAlarmCodes.MotionSafetyError,
                        nameof(ValidateLimitPosition),
                        message,
                        new Dictionary<string, object?>
                        {
                            ["Reason"] = "LimitConfigInvalid",
                            ["LimitAxisNum"] = limit.LimitAxisNum,
                            ["ByLimitAxisNum"] = limit.ByLimitAxisNum
                        });
                    return false;
                }

                if (!TryGetAxisPosition(axisPositions, limit.ByLimitAxisNum, out var triggerAxisPos) ||
                    !TryGetAxisPosition(axisPositions, limit.LimitAxisNum, out var restrictedAxisPos))
                {
                    continue;
                }

                if (!IsTriggerMatched(triggerAxisPos, limit.TriggerCondition, limit.LimitValue, tolerance))
                {
                    continue;
                }

                if (limit.IsRangeLimitConfigured)
                {
                    var hitMin = limit.MinLimitValue.HasValue && restrictedAxisPos < limit.MinLimitValue.Value - tolerance;
                    var hitMax = limit.MaxLimitValue.HasValue && restrictedAxisPos > limit.MaxLimitValue.Value + tolerance;
                    if (!hitMin && !hitMax)
                    {
                        continue;
                    }

                    message = $"坐标限制不满足：当{limit.ByLimitAxisNum}轴{GetTriggerConditionText(limit.TriggerCondition)}{limit.LimitValue:F3}时，" +
                              $"{limit.LimitAxisNum}轴必须在{FormatAxisRange(limit.MinLimitValue, limit.MaxLimitValue)}内。" +
                              $"当前输入后{limit.LimitAxisNum}轴坐标为{restrictedAxisPos:F3}。";
                    ReportMotionAlarm(
                        HardwareAlarmCodes.MotionLimitTriggered,
                        nameof(ValidateLimitPosition),
                        message,
                        new Dictionary<string, object?>
                        {
                            ["LimitAxisNum"] = limit.LimitAxisNum,
                            ["ByLimitAxisNum"] = limit.ByLimitAxisNum,
                            ["TriggerCondition"] = limit.TriggerCondition,
                            ["LimitValue"] = limit.LimitValue,
                            ["RestrictedAxisPos"] = restrictedAxisPos,
                            ["TriggerAxisPos"] = triggerAxisPos,
                            ["Tolerance"] = tolerance
                        });
                    return false;
                }

                if (!limit.ByLimitValue.HasValue)
                {
                    continue;
                }

                if (!IsLegacyRestrictedPositionViolated(restrictedAxisPos, limit.Condition, limit.ByLimitValue.Value, tolerance))
                {
                    continue;
                }

                message = $"坐标限制不满足：当{limit.ByLimitAxisNum}轴{GetTriggerConditionText(limit.TriggerCondition)}{limit.LimitValue:F3}时，" +
                          $"{limit.LimitAxisNum}轴不能{GetLegacyConditionText(limit.Condition)}{limit.ByLimitValue.Value:F3}。" +
                          $"当前输入后{limit.LimitAxisNum}轴坐标为{restrictedAxisPos:F3}。";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionLimitTriggered,
                    nameof(ValidateLimitPosition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["LimitAxisNum"] = limit.LimitAxisNum,
                        ["ByLimitAxisNum"] = limit.ByLimitAxisNum,
                        ["TriggerCondition"] = limit.TriggerCondition,
                        ["LimitValue"] = limit.LimitValue,
                        ["RestrictedAxisPos"] = restrictedAxisPos,
                        ["TriggerAxisPos"] = triggerAxisPos,
                        ["Tolerance"] = tolerance
                    });
                return false;
            }

            return true;
        }

        /// <summary>
        /// 校验轴在当前点动方向上是否已经触发限位。
        /// 仅用于示教/Jog 按下前的限位拦截，额外结合当前位姿下的联动限位配置。
        /// </summary>
        /// <param name="axisNum">轴号</param>
        /// <param name="direction">点动方向</param>
        /// <param name="message">不满足时的提示信息</param>
        /// <param name="tolerance">坐标比较容差</param>
        /// <returns>允许点动返回 true，否则 false</returns>
        public virtual bool ValidateJogLimitCondition(En_AxisNum axisNum, MoveDirection direction, out string message, double tolerance = 0.001d)
        {
            message = string.Empty;

            var axisConfig = Config?.AllAxis?.FirstOrDefault(item => item.AxisNum == axisNum);
            if (axisConfig == null)
            {
                message = $"未找到{axisNum}轴配置。";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionSafetyError,
                    nameof(ValidateJogLimitCondition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["AxisNum"] = axisNum,
                        ["Direction"] = direction
                    });
                return false;
            }

            GetAllPosInfos();

            double currentPos;
            if (CurPos != null && axisConfig.AxisNo > 0 && axisConfig.AxisNo <= CurPos.Length)
            {
                currentPos = CurPos[axisConfig.AxisNo - 1];
            }
            else
            {
                currentPos = axisConfig.CurPos;
            }

            // 先校验当前位姿是否已经触发 AllLimitPos 联动限制。
            // 例如移动 X 轴时，如果当前 Z 轴已超出配置允许范围，则直接禁止继续点动。
            if (!ValidateLimitPosition(axisNum, currentPos, out message, tolerance))
            {
                return false;
            }

            var moveToPositiveLimit = (direction == MoveDirection.正向) ^ axisConfig.MovingDirNe;
            var expectedLimitFlag = moveToPositiveLimit
                ? AxisStatusFlags.PositiveLimitTriggered
                : AxisStatusFlags.NegativeLimitTriggered;

            if ((axisConfig.AxisStatus & (int)expectedLimitFlag) != 0)
            {
                message = moveToPositiveLimit
                    ? $"{axisNum}轴正限位已触发，无法继续正向点动。"
                    : $"{axisNum}轴负限位已触发，无法继续反向点动。";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionLimitTriggered,
                    nameof(ValidateJogLimitCondition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["AxisNum"] = axisNum,
                        ["Direction"] = direction,
                        ["CurrentPos"] = currentPos,
                        ["AxisStatus"] = axisConfig.AxisStatus,
                        ["LimitFlag"] = expectedLimitFlag
                    });
                return false;
            }

            var softLimitConfigured = axisConfig.SoftLimitPositive > axisConfig.SoftLimitNegative;
            if (!softLimitConfigured)
            {
                return true;
            }

            if (moveToPositiveLimit && currentPos >= axisConfig.SoftLimitPositive - tolerance)
            {
                message = $"{axisNum}轴正向软限位已到达，当前坐标{currentPos:F3}，正限位{axisConfig.SoftLimitPositive:F3}。";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionLimitTriggered,
                    nameof(ValidateJogLimitCondition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["AxisNum"] = axisNum,
                        ["Direction"] = direction,
                        ["CurrentPos"] = currentPos,
                        ["SoftLimitPositive"] = axisConfig.SoftLimitPositive,
                        ["Tolerance"] = tolerance
                    });
                return false;
            }

            if (!moveToPositiveLimit && currentPos <= axisConfig.SoftLimitNegative + tolerance)
            {
                message = $"{axisNum}轴负向软限位已到达，当前坐标{currentPos:F3}，负限位{axisConfig.SoftLimitNegative:F3}。";
                ReportMotionAlarm(
                    HardwareAlarmCodes.MotionLimitTriggered,
                    nameof(ValidateJogLimitCondition),
                    message,
                    new Dictionary<string, object?>
                    {
                        ["AxisNum"] = axisNum,
                        ["Direction"] = direction,
                        ["CurrentPos"] = currentPos,
                        ["SoftLimitNegative"] = axisConfig.SoftLimitNegative,
                        ["Tolerance"] = tolerance
                    });
                return false;
            }

            return true;
        }

        private string BuildMotionCardLocation()
        {
            return string.IsNullOrWhiteSpace(NickName) ? "MotionCard" : NickName.Trim();
        }

        private string BuildMotionCardSource()
        {
            return BuildMotionCardLocation();
        }

        private void ReportMotionAlarm(string code, string operation, string message, IDictionary<string, object?>? extraData = null)
        {
            try
            {
                var reporter = PrismProvider.HardwareAlarmReporter;
                if (reporter == null)
                {
                    return;
                }

                reporter.Report(new AlarmReportRequest
                {
                    Code = code,
                    Source = BuildMotionCardSource(),
                    SourceType = HardwareAlarmSources.MotionCard,
                    Location = BuildMotionCardLocation(),
                    Operation = operation,
                    Message = string.IsNullOrWhiteSpace(message) ? code : message,
                    ExtraData = extraData == null
                        ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, object?>(extraData, StringComparer.OrdinalIgnoreCase)
                });
            }
            catch
            {
                // Alarm reporting must never change motion validation behavior.
            }
        }

        #region virtuals

        #endregion

        #region abstracts
        //抽象方法成员
        /// <summary>
        /// 初始化控制卡
        /// </summary>
        /// <returns></returns>
        protected abstract bool DoInit();

        /// <summary>
        /// 配置控制卡
        /// </summary>
        protected abstract void DoConfigure();

        /// <summary>
        /// 停止所有轴
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="stopMode"></param>
        protected abstract void DoStop(En_AxisNum? axisType, AxisStopMode stopMode);

        /// <summary>
        /// 关闭
        /// </summary>
        protected abstract void DoClose();

        /// <summary>
        /// 读某个轴的使能状态
        /// </summary>
        /// <param name="axisType"></param>
        /// <returns></returns>
        protected abstract bool DoGetAxisEnable(En_AxisNum axisType);

        /// <summary>
        /// 写某个轴的使能状态
        /// </summary>
        /// <param name="axisType"></param>
        /// <returns></returns>
        protected abstract bool DoSetAxisEnable(En_AxisNum axisType, bool v);

        /// <summary>
        /// 读某个轴的运动状态
        /// </summary>
        /// <param name="axisType"></param>
        /// <returns></returns>
        protected abstract bool DoGetAxisStopped(En_AxisNum axisType);

        /// <summary>
        /// 移动某个轴到指定的距离
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="um"></param>
        /// <returns></returns>
        protected abstract bool DoMoveAxis(En_AxisNum axisType, double um);

        /// <summary>
        /// 某个轴的连续运动
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="moveDirection"></param>
        protected abstract bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection);

        /// <summary>
        /// 执行回零操作
        /// </summary>
        /// <param name="message"></param>
        protected abstract bool DoGoHome(out string message);



        public virtual bool JogAxis(En_AxisNum axisId, MoveDirection dir, float step)
        {
            return false;
        }

        public virtual bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)
        {
            return false;
        }

        public virtual bool LineInterpoMoving(LineInterPoParam param)
        {
            return false;
        }

        public virtual bool GetAllInput(out bool[] Status)
        {
            Status = new bool[16];
            return true;
        }

        public virtual bool GetAllOutput(out bool[] Status)
        {
            Status = new bool[16];
            return true;
        }

        public virtual bool SetSpecifiedIO(int Part, bool OnOrOff)
        {
            return true;
        }

        public virtual bool GetSpecifiedIO(bool InOrOut, int Part, out bool OnOrOff)
        {
            OnOrOff = false;
            return true;
        }

        public virtual bool SetAxisEnabled(short axisId, bool isEnabled)
        {
            return false;
        }

        public virtual bool ControlPosComparison(bool On_Off, PosComparisonOutputParam param)
        {
            return false;
        }

        public virtual bool BufIO(ushort doMask, ushort doValue)
        {
            return false;
        }

        public virtual bool BufDelay(ushort time)
        {
            return false;
        }

        public virtual int QuerySpace(short crd)
        {
            return -1;
        }

        public virtual bool PushOrder(Func<string> ConstomOrder)
        {
            return false;
        }

        public virtual void InsertPosCompareData(double[] pos, PosCompareData posCompareData)
        {

        }

        /// <summary>
        /// 获取实际的位置比较
        /// </summary>
        /// <param name="posCompareIndex"></param>
        /// <param name="ActualX"></param>
        /// <param name="ActualY"></param>
        /// <returns></returns>
        public virtual bool GetActualComparePos(short posCompareIndex, ref int[] ActualX, ref int[] ActualY)
        {
            return false;
        }

        /// <summary>
        /// 自定义插补运动
        /// </summary>
        /// <param name="param"></param>
        /// <param name="ConstomOrder"></param>
        /// <returns></returns>
        public virtual bool CustomInterpolationMoving(CustomInterPoParam param, Func<string> ConstomOrder, bool waitend = false)
        {
            return false;
        }

        /// <summary>
        /// 单轴绝对运动
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="fpos"></param>
        /// <param name="waitforend"></param>
        /// <returns></returns>
        public virtual bool MoveAbsoluteAxis(En_AxisNum axisId, double fpos, bool waitforend = false)
        {
            return false;
        }

        /// <summary>
        /// 圆弧插补运动
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual bool ArcInterpoMoving(ArcInterPoParam param)
        {
            return false;
        }

        public virtual void InsertPosCompareData(PosCompareData posCompareData)
        {

        }

        public virtual bool GetAllPosInfos(short core = 2)
        {
            return false;
        }
        public virtual bool GetAllPosInfos(ref double[] allPosInfos, short core = 2)
        {
            return false;
        }

        public virtual bool GetAllSpeedInfos(short core = 2)
        {
            return false;
        }

        public virtual bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
        {
            return false;
        }

        /// <summary>
        /// 根据圆心、半径和当前位置，计算距离当前位置最近的圆上坐标，
        /// 并输出圆上点到圆心的 XY 偏差值。
        /// 偏差值定义为：圆心坐标 - 圆上坐标。
        /// 当当前位置与圆心重合时，默认取圆心正 X 方向上的圆上点。
        /// </summary>
        /// <param name="center">圆心坐标</param>
        /// <param name="radius">圆半径</param>
        /// <param name="currentPoint">当前位置</param>
        /// <param name="pointOnCircle">按最短距离计算得到的圆上坐标</param>
        /// <param name="offsetFromPointOnCircleToCenter">圆上坐标到圆心的 XY 偏差值</param>
        /// <param name="tolerance">容差</param>
        /// <returns>计算成功返回 true，否则返回 false</returns>
        public static bool TryGetNearestPointOnCircle(
            Point center,
            double radius,
            Point currentPoint,
            out Point pointOnCircle,
            out Point offsetFromPointOnCircleToCenter,
            double tolerance = 1E-9d)
        {
            pointOnCircle = default;
            offsetFromPointOnCircleToCenter = default;

            if (!IsFinitePoint(center) || !IsFinitePoint(currentPoint))
            {
                return false;
            }

            var validRadius = Math.Abs(radius);
            if (!IsFinite(validRadius) || validRadius <= tolerance)
            {
                return false;
            }

            var deltaX = currentPoint.X - center.X;
            var deltaY = currentPoint.Y - center.Y;
            var distanceToCenter = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distanceToCenter <= tolerance)
            {
                pointOnCircle = new Point(center.X + validRadius, center.Y);
            }
            else
            {
                var scale = validRadius / distanceToCenter;
                pointOnCircle = new Point(
                    center.X + (deltaX * scale),
                    center.Y + (deltaY * scale));
            }

            offsetFromPointOnCircleToCenter = new Point(
                center.X - pointOnCircle.X,
                center.Y - pointOnCircle.Y);

            return true;
        }

        /// <summary>
        /// 根据圆心、半径和当前位置，计算距离当前位置最近的圆上坐标，
        /// 并输出圆上点到圆心的 XY 偏差值。
        /// </summary>
        /// <param name="centerX">圆心 X</param>
        /// <param name="centerY">圆心 Y</param>
        /// <param name="radius">圆半径</param>
        /// <param name="currentX">当前位置 X</param>
        /// <param name="currentY">当前位置 Y</param>
        /// <param name="pointOnCircle">按最短距离计算得到的圆上坐标</param>
        /// <param name="offsetFromPointOnCircleToCenter">圆上坐标到圆心的 XY 偏差值</param>
        /// <param name="tolerance">容差</param>
        /// <returns>计算成功返回 true，否则返回 false</returns>
        public static bool TryGetNearestPointOnCircle(
            double centerX,
            double centerY,
            double radius,
            double currentX,
            double currentY,
            out Point pointOnCircle,
            out Point offsetFromPointOnCircleToCenter,
            double tolerance = 1E-9d)
        {
            return TryGetNearestPointOnCircle(
                new Point(centerX, centerY),
                radius,
                new Point(currentX, currentY),
                out pointOnCircle,
                out offsetFromPointOnCircleToCenter,
                tolerance);
        }

        /// <summary>
        /// 根据起点、终点和合成速度计算XY分量速度。
        /// 默认返回各轴速度的绝对值，适合直接用于运动参数配置；
        /// 如需保留方向，请将 keepDirection 设为 true。
        /// </summary>
        /// <param name="origin">起点坐标</param>
        /// <param name="destination">终点坐标</param>
        /// <param name="synthesisSpeed">合成速度</param>
        /// <param name="keepDirection">是否保留方向符号</param>
        /// <param name="tolerance">零长度路径判定容差</param>
        /// <returns>X/Y分量速度</returns>
        public static (double XSpeed, double YSpeed) GetXYSpeedComponents(
            Point origin,
            Point destination,
            double synthesisSpeed,
            bool keepDirection = false,
            double tolerance = 1E-9d)
        {
            return GetXYSpeedComponents(
                origin.X,
                origin.Y,
                destination.X,
                destination.Y,
                synthesisSpeed,
                keepDirection,
                tolerance);
        }

        /// <summary>
        /// 根据起点、终点和合成速度计算XY分量速度。
        /// 默认返回各轴速度的绝对值，适合直接用于运动参数配置；
        /// 如需保留方向，请将 keepDirection 设为 true。
        /// </summary>
        /// <param name="startX">起点X</param>
        /// <param name="startY">起点Y</param>
        /// <param name="endX">终点X</param>
        /// <param name="endY">终点Y</param>
        /// <param name="synthesisSpeed">合成速度</param>
        /// <param name="keepDirection">是否保留方向符号</param>
        /// <param name="tolerance">零长度路径判定容差</param>
        /// <returns>X/Y分量速度</returns>
        public static (double XSpeed, double YSpeed) GetXYSpeedComponents(
            double startX,
            double startY,
            double endX,
            double endY,
            double synthesisSpeed,
            bool keepDirection = false,
            double tolerance = 1E-9d)
        {
            var speedMagnitude = Math.Abs(synthesisSpeed);
            var deltaX = endX - startX;
            var deltaY = endY - startY;
            var totalDistance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (totalDistance <= tolerance || speedMagnitude <= tolerance)
            {
                return (0d, 0d);
            }

            var xSpeed = speedMagnitude * deltaX / totalDistance;
            var ySpeed = speedMagnitude * deltaY / totalDistance;

            if (!keepDirection)
            {
                xSpeed = Math.Abs(xSpeed);
                ySpeed = Math.Abs(ySpeed);
            }

            return (xSpeed, ySpeed);
        }

        private Dictionary<En_AxisNum, double> BuildAxisPositionSnapshot(IDictionary<En_AxisNum, double> targetAxisPositions)
        {
            var axisPositions = new Dictionary<En_AxisNum, double>();
            GetAllPosInfos();
            if (Config?.AllAxis != null)
            {
                foreach (var axis in Config.AllAxis)
                {
                    if (CurPos != null && axis.AxisNo > 0 && axis.AxisNo <= CurPos.Length)
                    {
                        axisPositions[axis.AxisNum] = CurPos[axis.AxisNo - 1];
                        continue;
                    }

                    axisPositions[axis.AxisNum] = axis.CurPos;
                }
            }

            foreach (var axisPosition in targetAxisPositions)
            {
                axisPositions[axisPosition.Key] = axisPosition.Value;
            }

            return axisPositions;
        }

        private bool TryGetAxisPosition(IReadOnlyDictionary<En_AxisNum, double> axisPositions, En_AxisNum axisNum, out double axisPos)
        {
            if (axisPositions.TryGetValue(axisNum, out axisPos))
            {
                return true;
            }

            var axisConfig = Config?.AllAxis?.FirstOrDefault(item => item.AxisNum == axisNum);
            if (axisConfig != null)
            {
                if (CurPos != null && axisConfig.AxisNo > 0 && axisConfig.AxisNo <= CurPos.Length)
                {
                    axisPos = CurPos[axisConfig.AxisNo - 1];
                    return true;
                }

                axisPos = axisConfig.CurPos;
                return true;
            }

            axisPos = default;
            return false;
        }

        private static bool AreCoordinatesEqual(double source, double target, double tolerance)
        {
            return Math.Abs(source - target) <= tolerance;
        }

        private static bool IsFinitePoint(Point point)
        {
            return IsFinite(point.X) && IsFinite(point.Y);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsTriggerMatched(double axisPos, LimitTriggerCondition condition, double triggerValue, double tolerance)
        {
            return condition switch
            {
                LimitTriggerCondition.大于 => axisPos > triggerValue + tolerance,
                LimitTriggerCondition.大于等于 => axisPos >= triggerValue - tolerance,
                LimitTriggerCondition.等于 => AreCoordinatesEqual(axisPos, triggerValue, tolerance),
                LimitTriggerCondition.小于等于 => axisPos <= triggerValue + tolerance,
                LimitTriggerCondition.小于 => axisPos < triggerValue - tolerance,
                _ => false
            };
        }

        private static bool IsLegacyRestrictedPositionViolated(double axisPos, Condition condition, double limitValue, double tolerance)
        {
            return condition switch
            {
                Condition.大于 => axisPos > limitValue + tolerance,
                Condition.小于 => axisPos < limitValue - tolerance,
                _ => false
            };
        }

        private static string GetTriggerConditionText(LimitTriggerCondition condition)
        {
            return condition switch
            {
                LimitTriggerCondition.大于 => "大于",
                LimitTriggerCondition.大于等于 => "大于等于",
                LimitTriggerCondition.等于 => "等于",
                LimitTriggerCondition.小于等于 => "小于等于",
                LimitTriggerCondition.小于 => "小于",
                _ => condition.ToString()
            };
        }

        private static string GetLegacyConditionText(Condition condition)
        {
            return condition switch
            {
                Condition.大于 => "大于",
                Condition.小于 => "小于",
                _ => condition.ToString()
            };
        }

        private static string FormatAxisRange(double? minValue, double? maxValue)
        {
            var minText = minValue.HasValue ? minValue.Value.ToString("F3") : "-∞";
            var maxText = maxValue.HasValue ? maxValue.Value.ToString("F3") : "+∞";
            return $"[{minText}, {maxText}]";
        }

        #endregion

    }
}

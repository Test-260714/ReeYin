using DryIoc;
using DryIoc.ImTools;
using HandyControl.Controls;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ReeYin_V.Hardware.ControlCard.ViewModels
{
    public class AxisViewModel : DialogViewModelBase
    {
        #region Fields
        private DispatcherTimer _timer;

        private readonly ControlCardConfigModel _controlCardConfig;
        private readonly AxisViewIoJogController _ioJogController;

        private Task _localTask = Task.CompletedTask;
        private (En_AxisNum Axis, MoveDirection Direction, EN_SpeedType SpeedType)? _lastAxisViewIoJogRequest;
        private (En_AxisNum Axis, MoveDirection Direction, EN_SpeedType SpeedType)? _blockedAxisViewIoJogStartRequest;
        private (En_AxisNum Axis, MoveDirection Direction, string Message)? _lastAxisViewIoJogLimitFailure;
        private bool _isManualJogActive;

        private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];

        private DelegateCommand<string> _generalCommand;
        private DelegateCommand<object> _movingCommand;
        #endregion

        #region Properties
        private ObservableCollection<bool> _allAxisStatus = new ObservableCollection<bool>();
        /// <summary>
        /// 所有轴使能状态
        /// </summary>
        public ObservableCollection<bool> AllAxisStatus
        {
            get { return _allAxisStatus; }
            set { _allAxisStatus = value; RaisePropertyChanged(); }
        }

        private IConfigManager ConfigManager { get; }

        public bool IsMoving { get; set; }

        private ControlCardBase ControlCard { get; }

        public bool IsXAxisConfigured => IsAxisConfigured(En_AxisNum.X);

        public bool IsYAxisConfigured => IsAxisConfigured(En_AxisNum.Y);

        public bool IsZAxisConfigured => IsAxisConfigured(En_AxisNum.Z);

        public bool IsZ1AxisConfigured => IsAxisConfigured(En_AxisNum.Z1);

        public bool IsZ2AxisConfigured => IsAxisConfigured(En_AxisNum.Z2);

        private AxisModel _param;
        /// <summary>
        /// 参数
        /// </summary>
        public AxisModel ModelParam
        {
            get { return _param; }
            set { _param = value; RaisePropertyChanged(); }
        }

        private bool _editPosCompare = false;
        /// <summary>
        /// 编辑位置比较
        /// </summary>
        public bool EditPosCompare
        {
            get { return _editPosCompare; }
            set { _editPosCompare = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public AxisViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;

            ModelParam = ConfigManager.Read<AxisModel>(ConfigKey.AxisModel) ?? new AxisModel();

            var controlCardModules = PrismProvider.HardwareModuleManager?.Modules;
            IHardwareModule? controlCardModule = null;
            controlCardModules?.TryGetValue(ConfigKey.ControlCard, out controlCardModule);
            _controlCardConfig = controlCardModule as ControlCardConfigModel ?? new ControlCardConfigModel();
            //先直接获取到第一个
            ControlCard = _controlCardConfig.CardModels.FirstOrDefault()
                ?? throw new InvalidOperationException("未找到控制卡配置，无法打开轴操作面板。");
            _ioJogController = new AxisViewIoJogController(ControlCard, _controlCardConfig);

            RefreshDisplayedAxisStatus();
            RefreshCurrentPositionSnapshots();
            RefreshCurrentSpeedSnapshots();
            RefreshAxisConfiguredProperties();

            InitTimer();
            _timer?.Start();
        }

        #endregion

        #region Methods
        /// <summary>
        /// 实时获取当前位置
        /// </summary>
        private void InitTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Tick += (s, e) =>
            {
                if (!ControlCard.GetAllPosInfos())
                {
                    StopAxisViewIoJogAndClearState();
                    return;
                }
                ModelParam.CurPosInfos = AxisViewAxisMatcher.BuildPositionSnapshot(ControlCard.Config.AllAxis);
                RefreshDisplayedAxisStatus();
                if (!RefreshCurrentSpeedSnapshotFromCard())
                {
                    return;
                }
                UpdateAxisViewIoJog();

                if (!ControlCard.GetAllPosInfos(1))
                {
                    Console.WriteLine($"获取核1位置数据失败!!!");
                    return;
                }

                ModelParam.CurCore1PosInfos = AxisViewAxisMatcher.BuildPositionSnapshot(ControlCard.Config.AllAxis);
            };
        }

        private bool IsAxisConfigured(En_AxisNum axisType)
        {
            return AxisViewAxisMatcher.ContainsAxis(ControlCard?.Config?.AllAxis, axisType);
        }

        private void RefreshAxisConfiguredProperties()
        {
            RaisePropertyChanged(nameof(IsXAxisConfigured));
            RaisePropertyChanged(nameof(IsYAxisConfigured));
            RaisePropertyChanged(nameof(IsZAxisConfigured));
            RaisePropertyChanged(nameof(IsZ1AxisConfigured));
            RaisePropertyChanged(nameof(IsZ2AxisConfigured));
        }

        private void RefreshDisplayedAxisStatus()
        {
            var axisStatus = AxisViewAxisMatcher.BuildEnableSnapshot(ControlCard?.Config?.AllAxis);
            while (AllAxisStatus.Count < axisStatus.Length)
            {
                AllAxisStatus.Add(false);
            }

            for (var index = 0; index < axisStatus.Length; index++)
            {
                AllAxisStatus[index] = axisStatus[index];
            }
        }

        private void RefreshCurrentPositionSnapshots()
        {
            var positions = AxisViewAxisMatcher.BuildPositionSnapshot(ControlCard?.Config?.AllAxis);
            ModelParam.CurPosInfos = positions;
            ModelParam.CurCore1PosInfos = positions.ToArray();
        }

        private void RefreshCurrentSpeedSnapshots()
        {
            ModelParam.CurSpeedInfos = AxisViewAxisMatcher.BuildSpeedSnapshot(ControlCard?.Config?.AllAxis);
        }

        private void UpdateAxisViewIoJog()
        {
            if (!_controlCardConfig.IsAxisViewIoJogEnabled)
            {
                StopAxisViewIoJogAndClearState();
                return;
            }

            if (!(_localTask == null || _localTask.IsCompleted))
            {
                StopAxisViewIoJogAndClearState();
                return;
            }

            if (_isManualJogActive)
            {
                StopAxisViewIoJogAndClearState();
                return;
            }

            if (ControlCard.IsReseting)
            {
                StopAxisViewIoJogAndClearState();
                return;
            }

            if (!ControlCard.GetAllInput(out var inputStatus))
            {
                Console.WriteLine("AxisView IO Jog获取所有输入IO失败");
                StopAxisViewIoJogAndClearState();
                return;
            }

            if (!TryResolveAxisViewIoJogRequest(inputStatus, out var axis, out _)
                || !IsAxisConfigured(axis))
            {
                ClearAxisViewIoJogState();
            }

            _ioJogController.Update(
                inputStatus,
                ModelParam.CurSpeedType,
                IsAxisConfigured,
                CanStartOrContinueAxisViewIoJog);
        }

        private bool CanStartOrContinueAxisViewIoJog(En_AxisNum axis, MoveDirection direction)
        {
            if (!ControlCard.ValidateJogLimitCondition(axis, direction, out var message))
            {
                ClearAxisViewIoJogRequest();
                LogAxisViewIoJogLimitFailure(axis, direction, message);
                return false;
            }

            ClearAxisViewIoJogLimitFailure();

            if (IsSameAxisViewIoJogRequest(axis, direction))
            {
                return true;
            }

            _lastAxisViewIoJogRequest = null;
            if (IsSameBlockedAxisViewIoJogRequest(axis, direction))
            {
                return false;
            }

            if (NeedLiftZAxesBeforeJog(axis) && !TryMoveAllZAxesToSafePosition())
            {
                _blockedAxisViewIoJogStartRequest = (axis, direction, ModelParam.CurSpeedType);
                return false;
            }

            _blockedAxisViewIoJogStartRequest = null;
            _lastAxisViewIoJogRequest = (axis, direction, ModelParam.CurSpeedType);
            return true;
        }

        private bool IsSameAxisViewIoJogRequest(En_AxisNum axis, MoveDirection direction)
        {
            return _lastAxisViewIoJogRequest.HasValue
                && _lastAxisViewIoJogRequest.Value.Axis == axis
                && _lastAxisViewIoJogRequest.Value.Direction == direction
                && _lastAxisViewIoJogRequest.Value.SpeedType == ModelParam.CurSpeedType;
        }

        private bool IsSameBlockedAxisViewIoJogRequest(En_AxisNum axis, MoveDirection direction)
        {
            return _blockedAxisViewIoJogStartRequest.HasValue
                && _blockedAxisViewIoJogStartRequest.Value.Axis == axis
                && _blockedAxisViewIoJogStartRequest.Value.Direction == direction
                && _blockedAxisViewIoJogStartRequest.Value.SpeedType == ModelParam.CurSpeedType;
        }

        private static bool TryResolveAxisViewIoJogRequest(
            bool[] inputStatus,
            int port,
            AxisViewIoJogDirection direction,
            ref int triggeredCount,
            ref En_AxisNum axis,
            ref MoveDirection moveDirection)
        {
            if (port < 0 || port >= inputStatus.Length || !inputStatus[port])
            {
                return false;
            }

            triggeredCount++;
            (axis, moveDirection) = AxisViewIoJogController.ResolveMovement(direction);
            return true;
        }

        private bool TryResolveAxisViewIoJogRequest(
            bool[] inputStatus,
            out En_AxisNum axis,
            out MoveDirection direction)
        {
            axis = default;
            direction = default;

            if (inputStatus == null)
            {
                return false;
            }

            var triggeredCount = 0;
            TryResolveAxisViewIoJogRequest(inputStatus, _controlCardConfig.AxisViewIoJogUpInputPort, AxisViewIoJogDirection.Up, ref triggeredCount, ref axis, ref direction);
            TryResolveAxisViewIoJogRequest(inputStatus, _controlCardConfig.AxisViewIoJogDownInputPort, AxisViewIoJogDirection.Down, ref triggeredCount, ref axis, ref direction);
            TryResolveAxisViewIoJogRequest(inputStatus, _controlCardConfig.AxisViewIoJogLeftInputPort, AxisViewIoJogDirection.Left, ref triggeredCount, ref axis, ref direction);
            TryResolveAxisViewIoJogRequest(inputStatus, _controlCardConfig.AxisViewIoJogRightInputPort, AxisViewIoJogDirection.Right, ref triggeredCount, ref axis, ref direction);

            return triggeredCount == 1;
        }

        private void StopAxisViewIoJogAndClearState()
        {
            if (!_ioJogController.StopActiveJog())
            {
                ControlCard.Stop(null);
                _ioJogController.ResetActiveState();
            }

            ClearAxisViewIoJogState();
        }

        private void ClearAxisViewIoJogState()
        {
            ClearAxisViewIoJogRequest();
            ClearAxisViewIoJogLimitFailure();
        }

        private void ClearAxisViewIoJogRequest()
        {
            _lastAxisViewIoJogRequest = null;
            _blockedAxisViewIoJogStartRequest = null;
        }

        private void LogAxisViewIoJogLimitFailure(En_AxisNum axis, MoveDirection direction, string message)
        {
            message ??= string.Empty;
            if (_lastAxisViewIoJogLimitFailure.HasValue
                && _lastAxisViewIoJogLimitFailure.Value.Axis == axis
                && _lastAxisViewIoJogLimitFailure.Value.Direction == direction
                && _lastAxisViewIoJogLimitFailure.Value.Message == message)
            {
                return;
            }

            _lastAxisViewIoJogLimitFailure = (axis, direction, message);
            Console.WriteLine($"AxisView IO Jog限位检查失败：{message}");
        }

        private void ClearAxisViewIoJogLimitFailure()
        {
            _lastAxisViewIoJogLimitFailure = null;
        }

        private bool RefreshCurrentSpeedSnapshotFromCard()
        {
            if (!ControlCard.GetAllSpeedInfos())
            {
                Console.WriteLine("获取轴速度数据失败!!!");
                StopAxisViewIoJogAndClearState();
                return false;
            }

            RefreshCurrentSpeedSnapshots();
            return true;
        }

        private void CleanupAxisViewIoJogAndTimer()
        {
            if (!_ioJogController.StopActiveJog())
            {
                ControlCard.Stop(null);
                _ioJogController.ResetActiveState();
            }

            ClearAxisViewIoJogState();
            _isManualJogActive = false;
            _timer?.Stop();
        }

        private bool TrySetAxisEnabled(En_AxisNum axisType, bool isEnabled)
        {
            var axisNo = AxisViewAxisMatcher.GetAxisNo(ControlCard?.Config?.AllAxis, axisType);
            if (!axisNo.HasValue)
            {
                return false;
            }

            if (!ControlCard.SetAxisEnabled(axisNo.Value, isEnabled))
            {
                SetDisplayedAxisStatus(axisType, false);
                return false;
            }

            if (AxisViewAxisMatcher.TryGetAxis(ControlCard.Config.AllAxis, axisType, out var axis))
            {
                axis.IsEnable = isEnabled;
            }

            SetDisplayedAxisStatus(axisType, isEnabled);
            return true;
        }

        private void SetDisplayedAxisStatus(En_AxisNum axisType, bool isEnabled)
        {
            var index = AxisViewAxisMatcher.GetDisplayIndex(axisType);
            if (index < 0)
            {
                return;
            }

            while (AllAxisStatus.Count <= index)
            {
                AllAxisStatus.Add(false);
            }

            AllAxisStatus[index] = isEnabled;
        }

        private bool TryGetTargetPosition(En_AxisNum axisType, out double targetPosition)
        {
            targetPosition = 0d;
            if (!IsAxisConfigured(axisType))
            {
                return false;
            }

            var index = AxisViewAxisMatcher.GetDisplayIndex(axisType);
            var targetPositions = ModelParam?.MoveTargetPos?.TargetPos;
            if (index < 0 || targetPositions == null || targetPositions.Count <= index)
            {
                return false;
            }

            targetPosition = targetPositions[index];
            return true;
        }

        private Dictionary<En_AxisNum, double> BuildConfiguredTargetPositions()
        {
            var targetPositions = new Dictionary<En_AxisNum, double>();
            foreach (var axisType in AxisViewAxisMatcher.DisplayAxes)
            {
                if (IsAxisConfigured(axisType) && TryGetTargetPosition(axisType, out var targetPosition))
                {
                    targetPositions[axisType] = targetPosition;
                }
            }

            return targetPositions;
        }

        private bool TryMoveAllZAxesToSafePosition()
        {
            const double tolerance = 0.001d;

            if (!ControlCard.GetAllPosInfos())
            {
                MessageBox.Show("获取当前位置失败，无法执行安全抬刀。");
                return false;
            }

            var zAxes = ControlCard.Config.AllAxis
                .Where(axis => axis.AxisNum == En_AxisNum.Z || axis.AxisNum == En_AxisNum.Z1 || axis.AxisNum == En_AxisNum.Z2)
                .ToList();

            foreach (var axis in zAxes)
            {
                var safePos = axis.SafetyDis;
                if (axis.CurPos <= safePos + tolerance)
                {
                    continue;
                }

                if (!ControlCard.MoveAbsoluteAxis(axis.AxisNum, safePos, true))
                {
                    MessageBox.Show($"{axis.AxisNum}轴移动到安全位置失败，目标位置：{safePos:F3}");
                    return false;
                }
            }

            return true;
        }

        private static LineInterPoParam CreateLineInterpolationParam(
            Dictionary<En_AxisNum, double> targetPositions,
            double[] targetPosArray)
        {
            return new LineInterPoParam
            {
                InterPoAxiss = PlanarMoveAxes.ToList(),
                TargetPos = targetPosArray.ToArray(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(targetPositions),
                decZSpeed = [5, 10, 50],
                upZSpeed = [5, 10, 50],
                waitforend = true,
            };
        }

        private static CustomInterPoParam CreateCustomInterpolationParam(
            Dictionary<En_AxisNum, double> targetPositions,
            double[] targetPosArray)
        {
            return new CustomInterPoParam
            {
                InterPoAxiss = PlanarMoveAxes.ToList(),
                TargetPos = targetPosArray.ToArray(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(targetPositions),
                waitforend = true,
            };
        }

        private bool TryMovePlanarAxesToTarget(
            ControlCardBase controlCard,
            Dictionary<En_AxisNum, double> targetPositions,
            double[] targetPosArray)
        {
            var lineParam = CreateLineInterpolationParam(targetPositions, targetPosArray);
            if (controlCard is ICoordinatedMotionCard coordinatedMotionCard &&
                coordinatedMotionCard.SupportsCoordinatedMotion)
            {
                var request = new CoordinatedMotionRequest
                {
                    Kind = CoordinatedMotionKind.Line,
                    Axes = PlanarMoveAxes.ToList(),
                    TargetPositions = new Dictionary<En_AxisNum, double>(targetPositions),
                    WaitForEnd = true,
                    LineParam = lineParam,
                };

                if (!coordinatedMotionCard.MoveCoordinated(request, out var message))
                {
                    Console.WriteLine($"AxisView coordinated move failed: {message}");
                    return false;
                }

                return true;
            }

            if (!controlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions, targetPosArray),
                () => controlCard.LineInterpoMoving(lineParam) ? "OK" : "NG",
                true))
            {
                Console.WriteLine("AxisView custom interpolation move failed.");
                return false;
            }

            return true;
        }

        private void MoveAdditionalAxes(IReadOnlyDictionary<En_AxisNum, double> targetPositions)
        {
            if (targetPositions.TryGetValue(En_AxisNum.Z, out var zTargetPosition))
            {
                Task.Run(() =>
                {
                    if (!ControlCard.MoveAbsoluteAxis(En_AxisNum.Z, zTargetPosition))
                    {
                        Console.WriteLine("执行Z轴移动失败！！！");
                    }
                });
            }

            if (targetPositions.TryGetValue(En_AxisNum.Z1, out var z1TargetPosition))
            {
                Task.Run(() =>
                {
                    ControlCard.GetAllPosInfos();
                    if (!AxisViewAxisMatcher.TryGetAxis(ControlCard.Config.AllAxis, En_AxisNum.Y, out var yAxis) || yAxis.CurPos < 150)
                    {
                        MessageBox.Show("不在安全位置，无法移动！");
                        return;
                    }

                    if (!ControlCard.MoveAbsoluteAxis(En_AxisNum.Z1, z1TargetPosition))
                    {
                        Console.WriteLine("执行Z1轴移动失败！！！");
                    }
                });
            }
        }

        private static bool NeedLiftZAxesBeforeJog(En_AxisNum axisType)
        {
            return axisType == En_AxisNum.X || axisType == En_AxisNum.Y;
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => _generalCommand ??= new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CleanupAxisViewIoJogAndTimer();
                    CloseDialog(ButtonResult.No);

                    break;

                case "移动至指定位置":
                    {
                        if (!IsXAxisConfigured || !IsYAxisConfigured)
                        {
                            MessageBox.Show("X/Y轴未配置，无法移动至指定位置。");
                            return;
                        }

                        if (ModelParam.MoveTargetPos?.TargetPos == null || ModelParam.MoveTargetPos.TargetPos.Count < 2)
                        {
                            MessageBox.Show("目标位置无效，无法移动。");
                            return;
                        }

                        MessageBoxResult result = MessageBox.Show("确定要移动至目标位置吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        if (_localTask == null || _localTask.IsCompleted)
                        {
                            StopAxisViewIoJogAndClearState();
                            _isManualJogActive = false;

                            var targetPositions = BuildConfiguredTargetPositions();
                            var targetPosArray = ModelParam.MoveTargetPos.TargetPos.ToArray();
                            var controlCard = ControlCard;

                            _localTask = Task.Factory.StartNew(() =>
                            {
                                if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))
                                {
                                    return;
                                }

                                MoveAdditionalAxes(targetPositions);
                            });
                        }
                    }
                    break;

                case "Z轴使能":
                    {
                        if (!TrySetAxisEnabled(En_AxisNum.Z, AllAxisStatus[2]))
                        {
                            AllAxisStatus[2] =false;
                        }
                    }
                    break;
                case "Z1轴使能":
                    {
                        if (!TrySetAxisEnabled(En_AxisNum.Z1, AllAxisStatus[3]))
                        {
                            AllAxisStatus[3] =false;
                        }
                    }
                    break;
                case "Z2轴使能":
                    {
                        if (!TrySetAxisEnabled(En_AxisNum.Z2, AllAxisStatus[4]))
                        {
                            AllAxisStatus[4] =false;
                        }
                    }
                    break;

                case "停止":
                    {
                        StopAxisViewIoJogAndClearState();
                        _isManualJogActive = false;
                        ControlCard.Stop(null);
                    }
                    break;
                case "暂停":
                    {
                        StopAxisViewIoJogAndClearState();
                        _isManualJogActive = false;
                        ControlCard.Stop(null);
                    }
                    break;
                case "复位":
                    {
                        if (_localTask == null || _localTask.IsCompleted)
                        {
                            StopAxisViewIoJogAndClearState();
                            _isManualJogActive = false;

                            _localTask = Task.Factory.StartNew(() =>
                            {
                                var ErrInfo = "";
                                if (!ControlCard.GoHome(out ErrInfo))
                                {
                                    MessageBox.Show($"复位异常信息：{ErrInfo}");
                                }
                            });
                        }
                    }
                    break;
                case "切换速度":
                    {
                        EN_SpeedType newSpeed;
                        var values = Enum.GetValues(typeof(EN_SpeedType));
                        if (ModelParam.CurSpeedType == (EN_SpeedType)values.GetValue(values.Length - 1))
                            newSpeed = (EN_SpeedType)values.GetValue(0);
                        else
                            newSpeed = ModelParam.CurSpeedType + 1;

                        ModelParam.CurSpeedType = newSpeed;
                        ConfigManager.Write(ConfigKey.AxisModel, ModelParam);
                    }
                    break;
                case "关闭":
                    {
                        //存一下参数
                        ConfigManager.Write(ConfigKey.AxisModel, ModelParam);

                        _ioJogController.StopActiveJog();
                        CleanupAxisViewIoJogAndTimer();
                    }
                    break;

                default:
                    break;
            }

        });

        /// <summary>
        /// 移动事件
        /// </summary>
        public DelegateCommand<object> MovingCommand => _movingCommand ??= new DelegateCommand<object>((obj) =>
        {

            if (!_localTask.IsCompleted)
            {
                return;
            }

            var param = obj as AxisMoveParameter;
            if (param == null)
            {
                return;
            }

            //复位中不允许操作
            if (ControlCard.IsReseting == true) 
                return;

            if (!IsAxisConfigured(param.AxisType))
            {
                return;
            }

            switch (param.MouseOrder)
            {
                case "Down":
                    {
                        StopAxisViewIoJogAndClearState();

                        if (NeedLiftZAxesBeforeJog(param.AxisType) && !TryMoveAllZAxesToSafePosition())
                        {
                            return;
                        }

                        if (!ControlCard.ValidateJogLimitCondition(param.AxisType, param.Direction, out string mas))
                        {
                            MessageBox.Show($"限位了：{mas}");
                            return;
                        }

                        if (ModelParam.IsInching)
                        {
                            ControlCard.JogAxis(param.AxisType, param.Direction, (float)ModelParam.StepLenght);
                        }
                        else if (ControlCard.JogAxis(param.AxisType, param.Direction, ModelParam.CurSpeedType, true))
                        {
                            _isManualJogActive = true;
                        }
                    }
                    break;
                case "Up":
                    {
                        if (!ModelParam.IsInching)
                        {
                            ControlCard.JogAxis(param.AxisType, param.Direction, ModelParam.CurSpeedType, false);
                        }

                        _isManualJogActive = false;
                    }
                    break;
                case "Leave":
                    {
                        if (!ModelParam.IsInching)
                        {
                            ControlCard.JogAxis(param.AxisType, param.Direction, ModelParam.CurSpeedType, false);
                        }

                        _isManualJogActive = false;
                    }
                    break;

            }
        });

        #endregion

    }
}

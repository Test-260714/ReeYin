using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.ViewModels
{
    public class CoordinateCacheViewModel : DialogViewModelBase
    {
        #region Fields
        private IControlCard? ControlCard { get; }

        private IConfigManager ConfigManager { get; }

        private Task _localTask = Task.CompletedTask;

        private DelegateCommand? _loadCommand;

        private DelegateCommand<string>? _generalCommand;

        private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];
        #endregion

        #region Properties
        private CoordinateCacheModel _modelParam = new CoordinateCacheModel();

        public new CoordinateCacheModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public CoordinateCacheViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;

            ModelParam = ConfigManager.Read<CoordinateCacheModel>(ConfigKey.CoordinateCacheModel) ?? new CoordinateCacheModel();

            ControlCard = ResolveControlCard();
        }
        #endregion

        #region Methods
        public void Init()
        {

        }

        public override void InitParam()
        {

        }

        public override void OnDialogClosed()
        {
            ConfigManager.Write(ConfigKey.CoordinateCacheModel, ModelParam);
        }

        private static IControlCard? ResolveControlCard()
        {
            if (PrismProvider.HardwareModuleManager?.Modules?.TryGetValue(ConfigKey.ControlCard, out var module) != true ||
                module is not ControlCardConfigModel controlCardConfig)
            {
                return null;
            }

            if (IsUsableControlCard(controlCardConfig.CurSltCard))
            {
                return controlCardConfig.CurSltCard;
            }

            return controlCardConfig.CardModels?.FirstOrDefault(IsUsableControlCard);
        }

        private static bool IsUsableControlCard(ControlCardBase? card)
        {
            // CurSltCard only means the selected config item; require a live runtime card.
            return card?.Initialized == true && card.IsConnected;
        }

        private IReadOnlyList<SingleAxisParam> GetCoordinateAxes()
        {
            return ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .ToList()
                ?? new List<SingleAxisParam>();
        }

        private void NormalizeAllCoordinatePositions()
        {
            var axes = GetCoordinateAxes();
            if (axes.Count == 0)
            {
                return;
            }

            foreach (var position in ModelParam.AllPosInfo.Where(item => item != null))
            {
                NormalizeCoordinatePosition(position, axes);
            }
        }

        private void NormalizeCoordinatePosition(CoordinatePos position)
        {
            NormalizeCoordinatePosition(position, GetCoordinateAxes());
        }

        private static void NormalizeCoordinatePosition(CoordinatePos position, IReadOnlyList<SingleAxisParam> axes)
        {
            if (position == null || axes.Count == 0)
            {
                return;
            }

            position.TargetPos = ResizeValues(position.TargetPos, axes.Count, _ => 0d);
            position.PLimitPos = axes.Select(axis => axis.SoftLimitPositive).ToList();
            position.NLimitPos = axes.Select(axis => axis.SoftLimitNegative).ToList();
        }

        private static List<double> ResizeValues(IReadOnlyList<double>? values, int count, Func<int, double> defaultValueFactory)
        {
            var normalized = values?.Take(count).ToList() ?? new List<double>();
            while (normalized.Count < count)
            {
                normalized.Add(defaultValueFactory(normalized.Count));
            }

            return normalized;
        }

        private bool EnsureSelectedPosition(out CoordinatePos position, bool showMessage = true)
        {
            position = ModelParam?.SltPosInfo;
            if (position != null)
            {
                NormalizeCoordinatePosition(position);
                return true;
            }

            if (showMessage)
            {
                MessageBox.Show("请先选择一个位置项。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            position = null!;
            return false;
        }

        private bool TryGetCoordinateAxes(out IReadOnlyList<SingleAxisParam> axes, bool showMessage = true)
        {
            axes = GetCoordinateAxes();
            if (ControlCard != null && axes.Count > 0)
            {
                return true;
            }

            if (showMessage)
            {
                MessageBox.Show("未找到可用控制卡或运动轴配置。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }

        private CoordinatePos CreateCoordinatePosition(IReadOnlyList<SingleAxisParam> axes)
        {
            return new CoordinatePos
            {
                Name = CreateDefaultPositionName(),
                TargetPos = axes.Select(_ => 0d).ToList(),
                PLimitPos = axes.Select(axis => axis.SoftLimitPositive).ToList(),
                NLimitPos = axes.Select(axis => axis.SoftLimitNegative).ToList(),
            };
        }

        private string CreateDefaultPositionName()
        {
            var index = (ModelParam?.AllPosInfo?.Count ?? 0) + 1;
            var name = $"位置{index}";
            while (ModelParam?.AllPosInfo?.Any(item => string.Equals(item?.Name, name, StringComparison.Ordinal)) == true)
            {
                index++;
                name = $"位置{index}";
            }

            return name;
        }

        private static bool HasAxis(IReadOnlyList<SingleAxisParam> axes, En_AxisNum axisNum)
        {
            return axes.Any(axis => axis.AxisNum == axisNum);
        }

        private static Dictionary<En_AxisNum, double> BuildTargetPositionDictionary(
            CoordinatePos position,
            IReadOnlyList<SingleAxisParam> axes)
        {
            var targetPositions = new Dictionary<En_AxisNum, double>();
            for (var index = 0; index < axes.Count && index < position.TargetPos.Count; index++)
            {
                targetPositions[axes[index].AxisNum] = position.TargetPos[index];
            }

            return targetPositions;
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
            IControlCard controlCard,
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
                    Console.WriteLine($"CoordinateCache coordinated move failed: {message}");
                    return false;
                }

                return true;
            }

            if (!controlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions, targetPosArray),
                () => controlCard.LineInterpoMoving(lineParam) ? "OK" : "NG",
                true))
            {
                Console.WriteLine("CoordinateCache custom interpolation move failed.");
                return false;
            }

            return true;
        }

        private void MoveAdditionalAxes(IReadOnlyList<double> targetPositions, IReadOnlyList<SingleAxisParam> axes)
        {
            var controlCard = ControlCard;
            if (controlCard == null)
            {
                return;
            }

            for (var index = 0; index < axes.Count && index < targetPositions.Count; index++)
            {
                var axis = axes[index];
                if (axis.AxisNum == En_AxisNum.X || axis.AxisNum == En_AxisNum.Y)
                {
                    continue;
                }

                var axisNum = axis.AxisNum;
                var targetPos = targetPositions[index];
                Task.Run(() =>
                {
                    if (!controlCard.MoveAbsoluteAxis(axisNum, targetPos))
                    {
                        Console.WriteLine($"执行{axisNum}轴移动失败！！！");
                    }
                });
            }
        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand => _loadCommand ??= new DelegateCommand(NormalizeAllCoordinatePositions);

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => _generalCommand ??= new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "DoubleClickCachePos":
                    {
                        if (!EnsureSelectedPosition(out var position, false)) return;

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", position },
                        });
                    }
                    break;
                case "获取当前位置":
                    {
                        if (!EnsureSelectedPosition(out var position)) return;
                        if (!TryGetCoordinateAxes(out var axes)) return;

                        MessageBoxResult result = MessageBox.Show("确定要获取当前位置作为此项参数吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        if (ControlCard == null || !ControlCard.GetAllPosInfos())
                        {
                            MessageBox.Show("获取当前位置失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        position.TargetPos = axes.Select(axis => axis.CurPos).ToList();
                    }
                    break;
                case "移动至此位置":
                    {
                        if (!EnsureSelectedPosition(out var position)) return;
                        if (!TryGetCoordinateAxes(out var axes)) return;
                        if (!HasAxis(axes, En_AxisNum.X) || !HasAxis(axes, En_AxisNum.Y))
                        {
                            MessageBox.Show("X/Y轴未配置，无法移动到目标位置。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        MessageBoxResult result = MessageBox.Show("确定要移动到目标位置吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        if (_localTask != null && !_localTask.IsCompleted)
                        {
                            MessageBox.Show("当前已有位置移动任务在执行。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var controlCard = ControlCard;
                        var targetPositions = BuildTargetPositionDictionary(position, axes);
                        var targetPosArray = position.TargetPos.ToArray();
                        _localTask = Task.Run(() =>
                        {
                            if (controlCard == null)
                            {
                                return;
                            }

                            if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))
                            {
                                return;
                            }

                            MoveAdditionalAxes(targetPosArray, axes);
                        });

                    }
                    break;
                case "添加新项":
                    {
                        if (!TryGetCoordinateAxes(out var axes)) return;

                        var position = CreateCoordinatePosition(axes);
                        ModelParam.AllPosInfo.Add(position);
                        ModelParam.SltPosInfo = position;
                    }
                    break;
                case "删除选中项":
                    {
                        if (!EnsureSelectedPosition(out var position)) return;

                        ModelParam.AllPosInfo.Remove(position);
                        ModelParam.SltPosInfo = null;
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    {
                        //存一下参数


                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            //{ "Param", ModelParam },
                        });
                    }
                    break;
                default:
                    break;
            }

        });


        #endregion


    }
}

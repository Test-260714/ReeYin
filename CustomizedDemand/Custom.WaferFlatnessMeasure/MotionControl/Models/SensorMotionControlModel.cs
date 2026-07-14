using Custom.WaferFlatnessMeasure.Models;
using HslCommunication.Core.Net;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LineSegment = ReeYin_V.Core.MovingRelated.LineSegment;

namespace Custom.WaferFlatnessMeasure
{
    public partial class SensorMotionControlModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        private ControlCardBase? ControlCard;

        [JsonIgnore]
        public int LocusCount => AllLocusInfo?.Count ?? 0;

        //未被压完的剩余指令
        [JsonIgnore]
        Queue<LocusInfo> residualOrder = new Queue<LocusInfo>();

        [JsonIgnore]
        private readonly WaferTrajectoryTrackingPublisher _trajectoryTrackingPublisher = new WaferTrajectoryTrackingPublisher();

        [JsonIgnore]
        private readonly List<LineSegmentStartPositionInfo> _lineSegmentStartPositions = new List<LineSegmentStartPositionInfo>();

        private const int PointTemperatureCapturePollIntervalMs = 20;

        private const int PointTemperatureCaptureTimeoutMs = 60000;

        private const double PointTemperatureCapturePositionTolerance = 0.05;

        [JsonIgnore]
        private CancellationTokenSource? _pointTemperatureCaptureCts;

        [JsonIgnore]
        private Task? _pointTemperatureCaptureTask;

        [JsonIgnore]
        private List<string> _pointTemperatureAddresses =
            PointTemperatureStorageService.GetActivePointTemperatureAddresses();
        #endregion

        #region Properties
        [JsonIgnore]
        private CreateTrajectoryModel _createTrajectoryModel = new CreateTrajectoryModel();

        public CreateTrajectoryModel CreateTrajectoryModel
        {
            get { return _createTrajectoryModel; }
            set { _createTrajectoryModel = value;RaisePropertyChanged(); }
        }

        private DesignedTrajectoryPlan _designedTrajectoryPlan = new DesignedTrajectoryPlan();

        [RecipeParam("自定义设计轨迹", "Skia轨迹设计器生成的轨迹方案")]
        public DesignedTrajectoryPlan DesignedTrajectoryPlan
        {
            get => _designedTrajectoryPlan;
            set
            {
                _designedTrajectoryPlan = value ?? new DesignedTrajectoryPlan();
                RefreshDesignedTrajectoryRunSteps();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DesignedTrajectorySummaryText));
            }
        }

        [JsonIgnore]
        private ObservableCollection<DesignedTrajectoryRunStep> _designedTrajectoryRunSteps =
            new ObservableCollection<DesignedTrajectoryRunStep>();

        [JsonIgnore]
        public ObservableCollection<DesignedTrajectoryRunStep> DesignedTrajectoryRunSteps
        {
            get => _designedTrajectoryRunSteps;
            private set
            {
                _designedTrajectoryRunSteps = value ?? new ObservableCollection<DesignedTrajectoryRunStep>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public string DesignedTrajectorySummaryText
        {
            get
            {
                var plan = DesignedTrajectoryPlan ?? new DesignedTrajectoryPlan();
                int shapeCount = plan.Shapes?.Count ?? 0;
                int enabledShapeCount = plan.Shapes?.Count(item => item?.IsEnabled == true) ?? 0;
                int pointCount = plan.RunSteps?.Count(item => item.Kind == DesignedTrajectoryRunStepKind.Point) ?? 0;
                int lineCount = plan.RunSteps?.Count(item => item.Kind == DesignedTrajectoryRunStepKind.Line) ?? 0;

                if (shapeCount == 0 && pointCount == 0 && lineCount == 0)
                {
                    return "尚未设计轨迹，请点击“编辑轨迹”创建。";
                }

                return $"图形 {shapeCount} 个，启用 {enabledShapeCount} 个，执行步骤 {pointCount + lineCount} 条（点 {pointCount}，线段 {lineCount}），模式 {plan.ExecutionMode}。";
            }
        }

        [JsonIgnore]
        private ObservableCollection<LocusInfo> _allLocusInfo = new ObservableCollection<LocusInfo>();

        [OutputParam("AllLocusInfo", "当前轨迹列表")]
        public ObservableCollection<LocusInfo> AllLocusInfo
        {
            get { return _allLocusInfo; }
            set { _allLocusInfo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<CalibrationWaferPosition> _calibrationWaferPositions = new ObservableCollection<CalibrationWaferPosition>();

        public ObservableCollection<CalibrationWaferPosition> CalibrationWaferPositions
        {
            get => _calibrationWaferPositions;
            set
            {
                _calibrationWaferPositions = value ?? new ObservableCollection<CalibrationWaferPosition>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        [InputParam(nameof(CircleCenterActualXYLinkParam), "输入图像参数")]
        public TransmitParam CircleCenterActualXYLinkParam
        {
            get => CreateTrajectoryModel.CircleCenterActualXYLinkParam;
            set
            {
                CreateTrajectoryModel.CircleCenterActualXYLinkParam = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _zHight = 100;
        public double ZHight
        {
            get => _zHight;
            set
            {
                _zHight = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _z1Hight = 100;
        public double Z1Hight
        {
            get => _z1Hight;
            set
            {
                _z1Hight = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private PosComparisonOutputParam _posComparisonParam = new PosComparisonOutputParam();

        public PosComparisonOutputParam PosComparisonParam
        {
            get => _posComparisonParam;
            set
            {
                _posComparisonParam = value ?? new PosComparisonOutputParam();
                RaisePropertyChanged();
            }
        }

        private AcsLciFixedDistancePulseConfig _acsLciPulseParam = new AcsLciFixedDistancePulseConfig();

        public AcsLciFixedDistancePulseConfig AcsLciPulseParam
        {
            get => _acsLciPulseParam;
            set
            {
                _acsLciPulseParam = value ?? new AcsLciFixedDistancePulseConfig();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private ObservableCollection<LocusInfo> _orderedLocusInfo = new ObservableCollection<LocusInfo>();

        [JsonIgnore]
        [OutputParam("outOrderedLocusInfos", "按最短路径排序后的轨迹")]
        public ObservableCollection<LocusInfo> OrderedLocusInfo
        {
            get => _orderedLocusInfo;
            set
            {
                _orderedLocusInfo = value ?? new ObservableCollection<LocusInfo>();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(OrderedLocusCount));
            }
        }

        private ushort _pointOperationIoMaskHex = 0x20;
        public ushort PointOperationIoMaskHex
        {
            get => _pointOperationIoMaskHex;
            set => SetProperty(ref _pointOperationIoMaskHex, value);
        }

        private ushort _pointOperationCloseDelayMs = 1000;
        public ushort PointOperationCloseDelayMs
        {
            get => _pointOperationCloseDelayMs;
            set => SetProperty(ref _pointOperationCloseDelayMs, value);
        }

        private ushort _pointReadyDelayMs = 500;
        public ushort PointReadyDelayMs
        {
            get => _pointReadyDelayMs;
            set => SetProperty(ref _pointReadyDelayMs, value);
        }

        private int _expectedCollectionDataCount;
        public int ExpectedCollectionDataCount
        {
            get => _expectedCollectionDataCount;
            set => SetProperty(ref _expectedCollectionDataCount, Math.Max(0, value));
        }

        private int _trajectoryTrackingPollIntervalMs = 100;
        public int TrajectoryTrackingPollIntervalMs
        {
            get => _trajectoryTrackingPollIntervalMs;
            set => SetProperty(ref _trajectoryTrackingPollIntervalMs, Math.Max(10, value));
        }



        #endregion


        [JsonIgnore]
        public int OrderedLocusCount => OrderedLocusInfo?.Count ?? 0;

        [JsonIgnore]
        private double _lastFlatnessValue = double.NaN;
        [JsonIgnore]
        [OutputParam("outFlatnessValue", "最近一次计算得到的平面度")]
        public double LastFlatnessValue
        {
            get => _lastFlatnessValue;
            set
            {
                if (SetProperty(ref _lastFlatnessValue, value))
                {
                    RaisePropertyChanged(nameof(LastFlatnessText));
                }
            }
        }

        [JsonIgnore]
        public string LastFlatnessText => double.IsFinite(LastFlatnessValue) ? LastFlatnessValue.ToString("F6") : "待计算";


        #region Constructor
        public SensorMotionControlModel()
        {
            Name = "晶圆检测轨迹设定";
        }
        #endregion

        #region Override
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam())
                {
                    return false;
                }

                ModuleName = Serial.ToString("D3");
                CreateTrajectoryModel.SyncCircleCenterFromInput(param => GetTransmitParam(InputParams, param, false));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                //_createTrajectoryModel = new CreateTrajectoryModel();

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != PointTemperatureStorageService.PointTemperatureAddressConfigUpdatedEventName)
                    {
                        return;
                    }

                    if (obj.Item2 is IEnumerable<PointTemperatureAddressModel> addressModels)
                    {
                        _pointTemperatureAddresses =
                            PointTemperatureStorageService.GetActivePointTemperatureAddresses(addressModels);
                    }
                    else if (obj.Item2 is IEnumerable<string> addresses)
                    {
                        _pointTemperatureAddresses =
                            PointTemperatureStorageService.GetActivePointTemperatureAddresses(addresses);
                    }
                }, ThreadOption.BackgroundThread);

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    Console.WriteLine($"开始加载参数：{DateTime.Now:HH:mm:ss.fff}");
                    LoadKeyParam();
                    Console.WriteLine($"结束加载参数：{DateTime.Now:HH:mm:ss.fff}");

                    ControlCard = ResolveCurrentControlCard();
                    if (ControlCard == null)
                    {
                        return NodeStatus.Error;
                    }

                    bool hasDesignedTrajectoryPlan = HasDesignedTrajectoryPlan();
                    if (hasDesignedTrajectoryPlan)
                    {
                        ApplyDesignedTrajectoryPlan();
                    }

                    ValidateExecution();

                    LastFlatnessValue = double.NaN;

                    if (hasDesignedTrajectoryPlan &&
                        DesignedTrajectoryPlan.ExecutionMode == DesignedTrajectoryExecutionMode.Points)
                    {
                        ExecutePointMoving(ControlCard);
                    }
                    else if (hasDesignedTrajectoryPlan &&
                        (DesignedTrajectoryPlan.ExecutionMode == DesignedTrajectoryExecutionMode.Lines ||
                         DesignedTrajectoryPlan.ExecutionMode == DesignedTrajectoryExecutionMode.Mixed))
                    {
                        ExecuteMoving(ControlCard);
                    }
                    else if (CreateTrajectoryModel.IsPointGenerationMode)
                    {
                        ExecutePointMoving(ControlCard);
                    }
                    else
                    {
                        ExecuteMoving(ControlCard);
                    }

                }
                catch (Exception ex)
                {
                    Logs.LogError(ex);
                    return NodeStatus.Error;
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：找圆模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }
        #endregion

        public void ApplyDesignedTrajectoryPlan(DesignedTrajectoryPlan? plan = null)
        {
            bool forceSync = plan != null;
            if (plan != null)
            {
                _designedTrajectoryPlan = plan.Clone();
            }

            if (_designedTrajectoryPlan == null)
            {
                _designedTrajectoryPlan = new DesignedTrajectoryPlan();
            }

            if (HasDesignedTrajectoryPlan())
            {
                _designedTrajectoryPlan = DesignedTrajectoryBuilder.BuildPlan(
                    DesignedTrajectoryBuilder.ToEditableShapes(_designedTrajectoryPlan),
                    _designedTrajectoryPlan.CoordinateStartX,
                    _designedTrajectoryPlan.CoordinateStartY,
                    _designedTrajectoryPlan.CoordinateEndX,
                    _designedTrajectoryPlan.CoordinateEndY,
                    _designedTrajectoryPlan.DefaultCenterX,
                    _designedTrajectoryPlan.DefaultCenterY,
                    GetSamplingSpacing());
            }

            RefreshDesignedTrajectoryRunSteps();
            RaisePropertyChanged(nameof(DesignedTrajectoryPlan));
            RaisePropertyChanged(nameof(DesignedTrajectorySummaryText));

            if (forceSync || HasDesignedTrajectoryPlan())
            {
                AllLocusInfo = new ObservableCollection<LocusInfo>(DesignedTrajectoryBuilder.ToLocusInfos(_designedTrajectoryPlan));
            }
        }

        private bool HasDesignedTrajectoryPlan()
        {
            return (DesignedTrajectoryPlan?.Shapes?.Count ?? 0) > 0 ||
                   (DesignedTrajectoryPlan?.RunSteps?.Count ?? 0) > 0;
        }

        private void RefreshDesignedTrajectoryRunSteps()
        {
            DesignedTrajectoryRunSteps = new ObservableCollection<DesignedTrajectoryRunStep>(
                DesignedTrajectoryPlan?.RunSteps ?? new List<DesignedTrajectoryRunStep>());
            RaisePropertyChanged(nameof(DesignedTrajectorySummaryText));
        }

        private bool HasTrajectoryTargetOutOfSoftLimitWithWarning(
            ControlCardBase controlCard,
            IEnumerable<Dictionary<En_AxisNum, double>> targets)
        {
            if (!WaferTrajectoryMotionHelper.TryGetTrajectoryTargetSoftLimitViolation(
                controlCard,
                targets,
                out string violationMessage))
            {
                return false;
            }

            Logs.LogWarning($"Trajectory soft-limit violation: {violationMessage}");
            ShowTrajectorySoftLimitWarning(violationMessage);
            return true;
        }

        private static void ShowTrajectorySoftLimitWarning(string violationMessage)
        {
            void ShowWarning()
            {
                MessageBox.Show(
                    $"轨迹超出控制卡软限位，已取消本次运动。\n{violationMessage}",
                    "轨迹超限",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            try
            {
                var dispatcher = PrismProvider.Dispatcher;
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                {
                    return;
                }

                if (dispatcher.CheckAccess())
                {
                    ShowWarning();
                }
                else
                {
                    dispatcher.Invoke(ShowWarning);
                }
            }
            catch (Exception ex)
            {
                Logs.LogWarning($"Unable to display trajectory soft-limit warning: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行直线轨迹
        /// </summary>
        /// <param name="controlCard"></param>
        public void ExecuteMoving(ControlCardBase? controlCard)
        {
            if (controlCard == null)
            {
                Logs.LogWarning("运动控制卡为空，无法执行轨迹");

                return;
            }

            var orderedSegments = WaferTrajectoryMotionHelper.BuildOrderedLineSegments(
                AllLocusInfo,
                CreateTrajectoryModel.IsOptimalPathEnabled);
            if (orderedSegments.Count == 0)
            {
                Logs.LogWarning("未配置有效轨迹，跳过执行运动");

                return;
            }

            if (HasTrajectoryTargetOutOfSoftLimitWithWarning(
                controlCard,
                WaferTrajectoryMotionHelper.BuildLineTrajectoryTargets(orderedSegments, ZHight, Z1Hight)))
            {
                return;
            }

            string trackingRunId = _trajectoryTrackingPublisher.BeginLineTracking(
                Serial,
                orderedSegments,
                TrajectoryTrackingPollIntervalMs);
            bool trackingCompleted = false;

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((
                LineSegmentCsvSessionInfo.EventName,
                new LineSegmentCsvSessionInfo { ExpectedSegmentCount = orderedSegments.Count }));
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("IsPoint", false));
            _lineSegmentStartPositions.Clear();

            try
            {
                for (int trajectoryIndex = 0; trajectoryIndex < orderedSegments.Count; trajectoryIndex++)
                {
                    var item = orderedSegments[trajectoryIndex];
                    var collectPoints = WaferTrajectoryMotionHelper.GenerateLineSamplePoints(
                        item.Start.X,
                        item.Start.Y,
                        item.End.X,
                        item.End.Y,
                        GetSamplingSpacing());

                    _trajectoryTrackingPublisher.PublishProgress(trackingRunId, trajectoryIndex, trajectoryIndex - 1);
                    _trajectoryTrackingPublisher.PublishTarget(trackingRunId, trajectoryIndex, item.Start.X, item.Start.Y);

                    if (!controlCard.CustomInterpolationMoving(new CustomInterPoParam
                    {
                        InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                        TargetPosDic = new Dictionary<En_AxisNum, double>
                        {
                            { En_AxisNum.X, item.End.X },
                            { En_AxisNum.Y, item.End.Y },
                            { En_AxisNum.Z, ZHight },
                            { En_AxisNum.Z1, Z1Hight },
                            { En_AxisNum.Z2, 60 },
                        },
                    }, () =>
                    {
                        if (!controlCard.LineInterpoMoving(new LineInterPoParam
                        {
                            InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                            TargetPosDic = new Dictionary<En_AxisNum, double>
                                {
                                    { En_AxisNum.X, item.Start.X },
                                    { En_AxisNum.Y, item.Start.Y },
                                    { En_AxisNum.Z, ZHight },
                                    { En_AxisNum.Z1, Z1Hight },
                                    { En_AxisNum.Z2, 60 },
                                },
                            decZSpeed = [5, 10, 50],
                            upZSpeed = [5, 10, 50],
                        }))
                        {
                            Logs.LogWarning("运动前定位到起点失败");
                        }

                        return "OK";
                    }))
                    {
                        Logs.LogWarning("执行自定义插补运动到起点失败");
                    }

                    RecordLineSegmentStartPosition(controlCard, trajectoryIndex + 1, item.Start.X, item.Start.Y);
                    if (AcsLciPulseParam?.IsEnabled == true)
                    {
                        if (!ExecuteAcsLciFixedDistancePulseSegment(
                            controlCard,
                            item,
                            collectPoints,
                            trackingRunId,
                            trajectoryIndex))
                        {
                            Logs.LogWarning($"ACS LCI固定距离脉冲执行失败，已跳过第 {trajectoryIndex + 1} 段轨迹。");
                        }

                        continue;
                    }

                    SwitchPosCompare(controlCard, true);
                    Task.Delay(5).Wait();

                    PublishStartCollectEvent(trajectoryIndex + 1, false);
                    Task.Delay(100).Wait();
                    _trajectoryTrackingPublisher.PublishProgress(trackingRunId, trajectoryIndex, trajectoryIndex - 1);
                    _trajectoryTrackingPublisher.PublishTarget(trackingRunId, trajectoryIndex, item.End.X, item.End.Y);
                    if (!controlCard.CustomInterpolationMoving(new CustomInterPoParam
                    {
                        InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                        TargetPosDic = new Dictionary<En_AxisNum, double>
                                {
                                    { En_AxisNum.X, item.End.X },
                                    { En_AxisNum.Y, item.End.Y },
                                                         { En_AxisNum.Z, ZHight },
                                    { En_AxisNum.Z1, Z1Hight },
                                    { En_AxisNum.Z2, 60 },
                                },
                    }, () =>
                    {
                        if (!controlCard.LineInterpoMoving(new LineInterPoParam
                        {
                            InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                            TargetPosDic = new Dictionary<En_AxisNum, double>
                                {
                                    { En_AxisNum.X, item.End.X },
                                    { En_AxisNum.Y, item.End.Y },
                                                         { En_AxisNum.Z, ZHight },
                                    { En_AxisNum.Z1, Z1Hight },
                                    { En_AxisNum.Z2, 60 },
                                },
                            decZSpeed = [5, 10, 50],
                            upZSpeed = [5, 10, 50],
                        }))
                        {
                            Logs.LogWarning("轨迹移动到终点失败");
                        }

                        return "OK";
                    },true))
                    {
                        Logs.LogWarning("执行自定义插补运动到终点失败");
                    }

                    RecordLineSegmentEndPosition(controlCard, trajectoryIndex + 1, item.End.X, item.End.Y);
                    PublishLineCollectPoints(collectPoints);
                    SwitchPosCompare(controlCard, false);

                    PublishStopCollectEvent(trajectoryIndex + 1, false);
                }

                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("RunALGO");
                trackingCompleted = true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);

            }
            finally
            {
                if (!trackingCompleted)
                {
                    CancelPointTemperatureCapture();
                }

                _trajectoryTrackingPublisher.PublishStop(
                    trackingRunId,
                    trackingCompleted,
                    trackingCompleted ? "Line trajectory finished." : "Line trajectory stopped with error.");
            }
        }

        private void RecordLineSegmentStartPosition(
            ControlCardBase controlCard,
            int segmentIndex,
            double fallbackStartX,
            double fallbackStartY)
        {
            if (!TryReadCurrentAxisPosition(controlCard, out double startX, out double startY))
            {
                startX = fallbackStartX;
                startY = fallbackStartY;
                Logs.LogWarning($"读取线段{segmentIndex}起点实际坐标失败，使用规划起点坐标。");
            }

            var positionInfo = new LineSegmentStartPositionInfo
            {
                SegmentIndex = segmentIndex,
                StartX = Math.Round(startX, 5),
                StartY = Math.Round(startY, 5)
            };

            _lineSegmentStartPositions.Add(positionInfo);
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((
                LineSegmentStartPositionInfo.EventName,
                positionInfo.Clone()));
        }

        private void RecordLineSegmentEndPosition(
            ControlCardBase controlCard,
            int segmentIndex,
            double fallbackEndX,
            double fallbackEndY)
        {
            if (!TryReadCurrentAxisPosition(controlCard, out double endX, out double endY))
            {
                endX = fallbackEndX;
                endY = fallbackEndY;
                Logs.LogWarning($"读取线段{segmentIndex}终点实际坐标失败，使用规划终点坐标。");
            }

            LineSegmentStartPositionInfo? positionInfo =
                _lineSegmentStartPositions.LastOrDefault(item => item.SegmentIndex == segmentIndex);
            if (positionInfo == null)
            {
                positionInfo = new LineSegmentStartPositionInfo
                {
                    SegmentIndex = segmentIndex,
                    StartX = Math.Round(fallbackEndX, 5),
                    StartY = Math.Round(fallbackEndY, 5)
                };
                _lineSegmentStartPositions.Add(positionInfo);
            }

            positionInfo.EndX = Math.Round(endX, 5);
            positionInfo.EndY = Math.Round(endY, 5);
            positionInfo.HasEndPosition = true;

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((
                LineSegmentStartPositionInfo.EventName,
                positionInfo.Clone()));
        }

        private static bool TryReadCurrentAxisPosition(
            ControlCardBase controlCard,
            out double xPosition,
            out double yPosition)
        {
            xPosition = double.NaN;
            yPosition = double.NaN;

            try
            {
                var axisConfigs = controlCard.Config?.AllAxis;
                if (axisConfigs == null || axisConfigs.Count == 0)
                {
                    return false;
                }

                if (!controlCard.GetAllPosInfos())
                {
                    return false;
                }

                return TryGetAxisPositionFromControlCard(controlCard, En_AxisNum.X, out xPosition) &&
                       TryGetAxisPositionFromControlCard(controlCard, En_AxisNum.Y, out yPosition);
            }
            catch (Exception ex)
            {
                Logs.LogWarning($"读取控制卡XY坐标失败：{ex.Message}");
                return false;
            }
        }

        private static bool TryGetAxisPositionFromControlCard(
            ControlCardBase controlCard,
            En_AxisNum axisNum,
            out double position)
        {
            position = double.NaN;

            var axisConfig = controlCard.Config?.AllAxis?.FirstOrDefault(axis => axis.AxisNum == axisNum);
            if (axisConfig == null)
            {
                return false;
            }

            position = axisConfig.CurPos;
            if (double.IsFinite(position))
            {
                return true;
            }

            if (controlCard.CurPos != null &&
                axisConfig.AxisNo > 0 &&
                axisConfig.AxisNo <= controlCard.CurPos.Length)
            {
                position = controlCard.CurPos[axisConfig.AxisNo - 1];
            }

            return double.IsFinite(position);
        }

        /// <summary>
        /// 执行打点运动
        /// </summary>
        /// <param name="controlCard"></param>
        public List<LocusInfo> BuildCalibrationWaferLocusInfos()
        {
            return (CalibrationWaferPositions ?? new ObservableCollection<CalibrationWaferPosition>())
                .Where(IsValidCalibrationWaferPosition)
                .Select(position => new LocusInfo
                {
                    Type = LocusInfo.PointType,
                    OriginX = position.X,
                    OriginY = position.Y,
                    TargetX = position.X,
                    TargetY = position.Y
                })
                .ToList();
        }

        public void ExecuteCalibrationPointMoving(ControlCardBase? controlCard)
        {
            var calibrationLocusInfos = BuildCalibrationWaferLocusInfos();
            if (calibrationLocusInfos.Count == 0)
            {
                Logs.LogWarning("未配置有效标定片位置，跳过执行标定。");
                return;
            }

            ExecutePointMoving(controlCard, true, calibrationLocusInfos);
        }

        public void ExecutePointMoving(ControlCardBase? controlCard,bool Calib = false, IEnumerable<LocusInfo>? sourceLocusInfos = null)
        {
            if (controlCard == null)
            {
                Logs.LogWarning("运动控制卡为空，无法执行轨迹");

                return;
            }

            string trackingRunId = string.Empty;
            string pointTemperatureCaptureSessionId = string.Empty;
            bool trackingCompleted = false;
            var triggerCapability = ResolvePointTriggerCapability(controlCard);
            var useSynchronizedPointTrigger = triggerCapability != SynchronizedTriggerCapabilities.None;

            try
            {
                PointExecutionPlan pointPlan = new PointExecutionPlan();
                List<LocusInfo> orderedLocusInfos = new List<LocusInfo>();
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    pointPlan = BuildPointExecutionPlan(Calib, sourceLocusInfos, useSynchronizedPointTrigger);
                    orderedLocusInfos = pointPlan.ExecutionLocusInfos;
                    OrderedLocusInfo = new ObservableCollection<LocusInfo>(orderedLocusInfos);
                    if (sourceLocusInfos == null &&
                        CreateTrajectoryModel.IsOptimalPathEnabled &&
                        !CreateTrajectoryModel.IsCalibrationWaferMeasurementActive &&
                        orderedLocusInfos.Count > 0)
                    {
                        WaferTrajectoryMotionHelper.ReplaceLocusOrder(AllLocusInfo, orderedLocusInfos);
                    }
                });

                if (orderedLocusInfos.Count == 0)
                {
                    Logs.LogWarning("未配置有效打点轨迹，跳过执行运动");
                    return;
                }

                if (HasTrajectoryTargetOutOfSoftLimitWithWarning(
                    controlCard,
                    WaferTrajectoryMotionHelper.BuildPointTrajectoryTargets(orderedLocusInfos, ZHight, Z1Hight)))
                {
                    return;
                }

                trackingRunId = _trajectoryTrackingPublisher.BeginPointTracking(
                    Serial,
                    orderedLocusInfos,
                    TrajectoryTrackingPollIntervalMs);

                ushort ioMask = WaferTrajectoryMotionHelper.ConvertIoMaskToUInt16(PointOperationIoMaskHex);
                //const ushort pointReadyDelayMs = 200;

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("IsPoint", true));
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((PointCollectionStepInfo.EventName, pointPlan.CollectionSteps));
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("CollectPoints", pointPlan.CollectPoints));
                pointTemperatureCaptureSessionId = Guid.NewGuid().ToString("N");
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((
                    PointTemperatureStorageService.PointTemperatureCaptureStartedEventName,
                    pointTemperatureCaptureSessionId));
                //映射IO
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("GetAllDatas");
                StartPointTemperatureCapture(
                    controlCard,
                    pointPlan.CollectionSteps,
                    pointTemperatureCaptureSessionId,
                    Calib);

                _trajectoryTrackingPublisher.PublishProgress(trackingRunId, 0, -1);
                _trajectoryTrackingPublisher.PublishTarget(trackingRunId, 0, orderedLocusInfos[0].TargetX, orderedLocusInfos[0].TargetY);


                //未被压完的剩余指令
                //List<LocusInfo> residualOrder = new List<LocusInfo>();
                if (useSynchronizedPointTrigger)
                {
                    trackingCompleted = ExecuteSynchronizedPointTrigger(
                        controlCard,
                        orderedLocusInfos,
                        pointPlan,
                        triggerCapability,
                        ioMask,
                        trackingRunId);

                    if (!Calib)
                    {
                        WaitPointTemperatureCaptureComplete(pointPlan.CollectPoints.Count);
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
                    }
                    else
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Calib");
                    }

                    return;
                }

                residualOrder.Clear();
                if (!controlCard.CustomInterpolationMoving(new CustomInterPoParam
                {
                    InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                    TargetPosDic = new Dictionary<En_AxisNum, double>
                            {
                                { En_AxisNum.X, 0 },
                                { En_AxisNum.Y, 0 },
                                { En_AxisNum.Z, ZHight },
                                { En_AxisNum.Z1, Z1Hight },
                                { En_AxisNum.Z2, 70 },
                            },
                }, () =>
                {
                    for (int trajectoryIndex = 0; trajectoryIndex < orderedLocusInfos.Count; trajectoryIndex++)
                    {
                        var locusInfo = orderedLocusInfos[trajectoryIndex];

                        //压入之前先查询剩余空间
                        var residualSpace = controlCard.QuerySpace(1);
                        Console.WriteLine($"Buff剩余空间为：{residualSpace}");
                        if (residualSpace < 1000)
                        {
                            residualOrder.Enqueue(locusInfo);
                            continue;
                        }

                        //移动至目标点
                        if (!controlCard.LineInterpoMoving(new LineInterPoParam
                        {
                            InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                            TargetPosDic = new Dictionary<En_AxisNum, double>
                            {
                                { En_AxisNum.X, locusInfo.TargetX },
                                { En_AxisNum.Y, locusInfo.TargetY },
                                { En_AxisNum.Z, ZHight },
                                { En_AxisNum.Z1, Z1Hight },
                                { En_AxisNum.Z2, 70 },
                            },
                            decZSpeed = [5, 10, 50],
                            upZSpeed = [5, 10, 50],
                        }))
                        {
                            Logs.LogWarning("运动前定位到起点失败");
                        }

                        //延时一下确保到位
                        controlCard.BufDelay(PointReadyDelayMs);

                        //操作指定IO开启
                        controlCard.BufIO(ioMask, 0x00);

                        //延时关闭
                        controlCard.BufDelay(PointOperationCloseDelayMs);

                        //操作指定IO关闭
                        controlCard.BufIO(ioMask, 0xff);
                        controlCard.BufDelay(PointReadyDelayMs);
                        //controlCard.BufDelay(10);
                    }

                    //开个线程，将之前没压完的是数据压进去
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            Task.Delay(10).Wait();
                            var residualSpace = controlCard.QuerySpace(1);
                            //压入之前先查询剩余空间

                            Console.WriteLine($"压入剩余空间为：{residualSpace}");
                            if (residualSpace < 2000)
                            {
                                continue;
                            }
                            LocusInfo temp = new LocusInfo();
                            if (residualOrder.Count > 0)
                            {
                                temp = residualOrder.Dequeue();

                                //移动至目标点
                                if (!controlCard.LineInterpoMoving(new LineInterPoParam
                                {
                                    InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                                    TargetPosDic = new Dictionary<En_AxisNum, double>
                            {
                                { En_AxisNum.X, temp.TargetX },
                                { En_AxisNum.Y, temp.TargetY },
                                                              { En_AxisNum.Z, ZHight },
                                { En_AxisNum.Z1, Z1Hight },
                                { En_AxisNum.Z2, 70 },
                            },
                                    decZSpeed = [5, 10, 50],
                                    upZSpeed = [5, 10, 50],
                                }))
                                {
                                    Logs.LogWarning("运动前定位到起点失败");
                                }

                                //延时一下确保到位
                                controlCard.BufDelay(PointReadyDelayMs);

                                //操作指定IO开启
                                controlCard.BufIO(ioMask, 0x00);

                                //延时关闭
                                controlCard.BufDelay(PointOperationCloseDelayMs);

                                //操作指定IO关闭
                                controlCard.BufIO(ioMask, 0xff);

                                controlCard.PushOrder(null);
                            }


                            if(residualOrder.Count== 0)return;
                        }
                    });

                    return "OK";
                }, true))
                {
                    Logs.LogWarning("执行自定义插补运动到起点失败");
                }

                if (!Calib)
                {
                    WaitPointTemperatureCaptureComplete(pointPlan.CollectPoints.Count);
                    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
                }
                else
                {
                    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Calib");
                }

                trackingCompleted = true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
            finally
            {
                _trajectoryTrackingPublisher.PublishStop(
                    trackingRunId,
                    trackingCompleted,
                    trackingCompleted ? "Point trajectory finished." : "Point trajectory stopped with error.");
            }
        }

        private void StartPointTemperatureCapture(
            ControlCardBase controlCard,
            IReadOnlyList<PointCollectionStepInfo> collectionSteps,
            string captureSessionId,
            bool isCalibrationRun)
        {
            CancelPointTemperatureCapture();

            if (isCalibrationRun || string.IsNullOrWhiteSpace(captureSessionId))
            {
                return;
            }

            List<PointCollectionStepInfo> captureSteps = (collectionSteps ?? Array.Empty<PointCollectionStepInfo>())
                .Where(step => step != null && !step.IsCalibrationReference && step.NormalPointIndex >= 0)
                .Select(step => step.Clone())
                .ToList();
            if (captureSteps.Count == 0)
            {
                return;
            }

            PLCBase? plc = ResolvePointTemperaturePlc();
            if (plc == null)
            {
                Logs.LogWarning("未找到可用 PLC，点到位温度记录将为空。");
                return;
            }

            _pointTemperatureCaptureCts = new CancellationTokenSource();
            CancellationToken token = _pointTemperatureCaptureCts.Token;
            _pointTemperatureCaptureTask = Task.Run(() =>
            {
                CapturePointTemperaturesAtArrivals(
                    controlCard,
                    captureSteps,
                    plc,
                    captureSessionId,
                    token);
            }, token);
        }

        private void CapturePointTemperaturesAtArrivals(
            ControlCardBase controlCard,
            IReadOnlyList<PointCollectionStepInfo> captureSteps,
            PLCBase plc,
            string captureSessionId,
            CancellationToken token)
        {
            try
            {
                foreach (PointCollectionStepInfo step in captureSteps)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!WaitUntilPointReached(controlCard, step.X, step.Y, token))
                    {
                        Logs.LogWarning(
                            $"等待打点第 {step.NormalPointIndex + 1} 点到位超时，坐标 X={step.X:F3}, Y={step.Y:F3}，本点温度未记录。");
                        continue;
                    }

                    PointTemperatureRecord record = ReadPointTemperatureRecordAtArrival(
                        plc,
                        captureSessionId,
                        step.NormalPointIndex + 1,
                        step.X,
                        step.Y);
                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((
                        PointTemperatureStorageService.PointTemperatureRecordCapturedEventName,
                        record));
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when a point run is aborted.
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
        }

        private bool WaitUntilPointReached(
            ControlCardBase controlCard,
            double targetX,
            double targetY,
            CancellationToken token)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!token.IsCancellationRequested &&
                   stopwatch.ElapsedMilliseconds < PointTemperatureCaptureTimeoutMs)
            {
                if (TryGetCurrentXyPosition(controlCard, out double currentX, out double currentY) &&
                    Math.Abs(currentX - targetX) <= PointTemperatureCapturePositionTolerance &&
                    Math.Abs(currentY - targetY) <= PointTemperatureCapturePositionTolerance)
                {
                    return true;
                }

                if (token.WaitHandle.WaitOne(PointTemperatureCapturePollIntervalMs))
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryGetCurrentXyPosition(
            ControlCardBase controlCard,
            out double currentX,
            out double currentY)
        {
            currentX = 0;
            currentY = 0;

            try
            {
                if (controlCard == null || !controlCard.GetAllPosInfos())
                {
                    return false;
                }

                return TryGetAxisPosition(controlCard, En_AxisNum.X, out currentX) &&
                       TryGetAxisPosition(controlCard, En_AxisNum.Y, out currentY);
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
        }

        private static bool TryGetAxisPosition(
            ControlCardBase controlCard,
            En_AxisNum axisNum,
            out double position)
        {
            position = 0;
            var axis = controlCard.Config?.AllAxis?.FirstOrDefault(item => item.AxisNum == axisNum);
            if (axis == null)
            {
                return false;
            }

            if (controlCard.CurPos != null &&
                axis.AxisNo > 0 &&
                axis.AxisNo <= controlCard.CurPos.Length)
            {
                position = controlCard.CurPos[axis.AxisNo - 1];
                return true;
            }

            position = axis.CurPos;
            return true;
        }

        private PointTemperatureRecord ReadPointTemperatureRecordAtArrival(
            PLCBase plc,
            string captureSessionId,
            int pointIndex,
            double x,
            double y)
        {
            var temperatures = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
            foreach (string address in GetActivePointTemperatureAddresses())
            {
                temperatures[address] = ReadPointTemperature(plc, address);
            }

            return new PointTemperatureRecord
            {
                CaptureSessionId = captureSessionId,
                PointIndex = pointIndex,
                X = x,
                Y = y,
                Temperatures = temperatures
            };
        }

        private IReadOnlyList<string> GetActivePointTemperatureAddresses()
        {
            List<string> addresses = PointTemperatureStorageService.GetActivePointTemperatureAddresses(_pointTemperatureAddresses);
            return addresses.Count > 0
                ? addresses
                : PointTemperatureStorageService.GetActivePointTemperatureAddresses();
        }

        private double? ReadPointTemperature(PLCBase plc, string plcAddress)
        {
            try
            {
                var param = new PLCParaInfoModel
                {
                    PLCAddress = plcAddress,
                    ParaType = EnumParaInfoModelParaType.Float
                };

                if (!plc.ReadPLCPara(param) || param.ParaValue == null)
                {
                    Logs.LogWarning($"读取打点温度失败，地址：{plcAddress}");
                    return null;
                }

                return Convert.ToDouble(param.ParaValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return null;
            }
        }

        private PLCBase? ResolvePointTemperaturePlc()
        {
            if (PrismProvider.HardwareModuleManager?.Modules == null ||
                !PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.PLCConfig, out var module) ||
                module is not PLCSetModel models ||
                models.Models == null ||
                models.Models.Count == 0)
            {
                return null;
            }

            return models.Models[0];
        }

        private void WaitPointTemperatureCaptureComplete(int expectedPointCount)
        {
            Task? captureTask = _pointTemperatureCaptureTask;
            if (captureTask == null || captureTask.IsCompleted)
            {
                return;
            }

            int waitMilliseconds = Math.Max(
                1000,
                Math.Min(10000, Math.Max(1, expectedPointCount) * PointTemperatureCapturePollIntervalMs * 10));
            try
            {
                if (!captureTask.Wait(waitMilliseconds))
                {
                    Logs.LogWarning("等待点到位温度记录完成超时，未完成点位温度将留空。");
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    if (innerException is not OperationCanceledException)
                    {
                        Logs.LogError(innerException);
                    }
                }
            }
        }

        private void CancelPointTemperatureCapture()
        {
            try
            {
                _pointTemperatureCaptureCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _pointTemperatureCaptureCts = null;
                _pointTemperatureCaptureTask = null;
            }
        }

        public bool MoveToLocus(ControlCardBase? controlCard, LocusInfo? locusInfo)
        {
            if (controlCard == null)
            {
                Logs.LogWarning("运动控制卡为空，无法移动到选中点");
                return false;
            }

            if (!WaferTrajectoryMotionHelper.TryGetMoveTarget(locusInfo, out double targetX, out double targetY))
            {
                Logs.LogWarning("选中的轨迹点无效，无法执行移动");
                return false;
            }

            var moveTarget = new Dictionary<En_AxisNum, double>
            {
                { En_AxisNum.X, targetX },
                { En_AxisNum.Y, targetY },
                { En_AxisNum.Z, ZHight },
                { En_AxisNum.Z1, 70 },
                { En_AxisNum.Z2, 70 },
            };

            if (HasTrajectoryTargetOutOfSoftLimitWithWarning(controlCard, new[] { moveTarget }))
            {
                return false;
            }

            bool moveResult = controlCard.CustomInterpolationMoving(new CustomInterPoParam
            {
                InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                TargetPosDic = moveTarget,
            }, () =>
            {
                if (!controlCard.LineInterpoMoving(new LineInterPoParam
                {
                    InterPoAxiss = [En_AxisNum.X, En_AxisNum.Y],
                    TargetPosDic = new Dictionary<En_AxisNum, double>(moveTarget),
                    decZSpeed = [5, 10, 50],
                    upZSpeed = [5, 10, 50],
                }))
                {
                    Logs.LogWarning("移动到选中点失败");
                    return "NG";
                }

                return "OK";
            }, true);

            if (!moveResult)
            {
                Logs.LogWarning("执行移动到选中点的插补运动失败");
            }

            return moveResult;
        }

        private PointExecutionPlan BuildPointExecutionPlan(bool calib, IEnumerable<LocusInfo>? sourceLocusInfos, bool preserveInputOrder = false)
        {
            var executionLocusInfos = (sourceLocusInfos ?? AllLocusInfo ?? Enumerable.Empty<LocusInfo>())
                .Where(WaferTrajectoryMotionHelper.IsValidLocus)
                .ToList();

            if (executionLocusInfos.Count == 0)
            {
                return new PointExecutionPlan();
            }

            bool shouldInsertCalibration = ShouldInsertCalibrationWaferMeasurements(calib, sourceLocusInfos);
            if (shouldInsertCalibration)
            {
                LocusInfo? calibrationLocus = BuildCalibrationWaferLocusInfos().FirstOrDefault();
                if (calibrationLocus != null)
                {
                    return BuildCalibrationInsertedPointExecutionPlan(
                        executionLocusInfos,
                        calibrationLocus,
                        CreateTrajectoryModel.CalibrationWaferMeasurementMode);
                }

                Logs.LogWarning("已启用标准片测量，但未配置有效标定片位置，已按普通打点轨迹执行。");
            }

            var orderedLocusInfos = CreateTrajectoryModel.IsOptimalPathEnabled && !shouldInsertCalibration && !preserveInputOrder
                ? WaferTrajectoryMotionHelper.SortPointLocusInfosByShortestPath(executionLocusInfos)
                : executionLocusInfos;

            return PointExecutionPlan.Create(orderedLocusInfos, orderedLocusInfos, false);
        }

        private static SynchronizedTriggerCapabilities ResolvePointTriggerCapability(ControlCardBase controlCard)
        {
            if (controlCard is not ISynchronizedTriggerCard synchronizedTriggerCard)
            {
                return SynchronizedTriggerCapabilities.None;
            }

            var capabilities = synchronizedTriggerCard.TriggerCapabilities;
            if ((capabilities & SynchronizedTriggerCapabilities.CoordinateArrayPulse) == SynchronizedTriggerCapabilities.CoordinateArrayPulse)
            {
                return SynchronizedTriggerCapabilities.CoordinateArrayPulse;
            }

            if ((capabilities & SynchronizedTriggerCapabilities.PositionCompare) == SynchronizedTriggerCapabilities.PositionCompare)
            {
                return SynchronizedTriggerCapabilities.PositionCompare;
            }

            return SynchronizedTriggerCapabilities.None;
        }

        private bool ExecuteSynchronizedPointTrigger(
            ControlCardBase controlCard,
            IReadOnlyList<LocusInfo> orderedLocusInfos,
            PointExecutionPlan pointPlan,
            SynchronizedTriggerCapabilities triggerCapability,
            ushort ioMask,
            string trackingRunId)
        {
            if (controlCard is not ISynchronizedTriggerCard synchronizedTriggerCard)
            {
                Logs.LogWarning("当前控制卡不支持同步触发打点。");
                return false;
            }

            var request = BuildPointTriggerRequest(
                orderedLocusInfos,
                pointPlan,
                triggerCapability,
                ioMask);

            if (!synchronizedTriggerCard.RunSynchronizedTrigger(request, out var result, out var message) || !result.Success)
            {
                Logs.LogWarning($"同步触发打点失败：{message}");
                return false;
            }

            _trajectoryTrackingPublisher.PublishProgress(
                trackingRunId,
                orderedLocusInfos.Count,
                Math.Max(0, orderedLocusInfos.Count - 1));
            Logs.LogInfo($"同步触发打点完成：{result.PointCount} 点，脉冲数 {result.PulseCount}。");
            return true;
        }

        private SynchronizedTriggerRequest BuildPointTriggerRequest(
            IReadOnlyList<LocusInfo> orderedLocusInfos,
            PointExecutionPlan pointPlan,
            SynchronizedTriggerCapabilities triggerCapability,
            ushort ioMask)
        {
            var config = AcsLciPulseParam ?? new AcsLciFixedDistancePulseConfig();
            var mode = triggerCapability == SynchronizedTriggerCapabilities.CoordinateArrayPulse
                ? SynchronizedTriggerMode.CoordinateArrayPulse
                : SynchronizedTriggerMode.PositionCompare;
            var interval = config.Interval;

            return new SynchronizedTriggerRequest
            {
                Mode = mode,
                Axes = [En_AxisNum.X, En_AxisNum.Y],
                Points = orderedLocusInfos
                    .Select(locus => new Point(locus.TargetX, locus.TargetY))
                    .ToList(),
                BufferNo = config.BufferNo,
                PulseWidth = config.PulseWidth,
                Interval = interval,
                StartDistance = config.StartDistance,
                EndDistance = config.EndDistance,
                TriggerWindow = config.CoordinateArrayMultiAxWinSize,
                Velocity = config.CoordinateArrayVelocity,
                RouteConfigOutput = config.RouteConfigOutput,
                ConfigOutputIndex = config.ConfigOutputIndex,
                ConfigOutputCode = config.ConfigOutputCode,
                DoMask = ioMask,
                DoValue = 0x00,
                DelayMilliseconds = PointOperationCloseDelayMs,
                WaitForEnd = true,
                Timeout = config.Timeout,
                PositionCompareParam = PosComparisonParam ?? new PosComparisonOutputParam()
            };
        }

        private bool ShouldInsertCalibrationWaferMeasurements(bool calib, IEnumerable<LocusInfo>? sourceLocusInfos)
        {
            return !calib &&
                   sourceLocusInfos == null &&
                   CreateTrajectoryModel?.IsCalibrationWaferMeasurementActive == true;
        }

        private static PointExecutionPlan BuildCalibrationInsertedPointExecutionPlan(
            IReadOnlyList<LocusInfo> normalLocusInfos,
            LocusInfo calibrationLocus,
            CalibrationWaferMeasurementMode measurementMode)
        {
            var executionLocusInfos = new List<LocusInfo>();
            const double rowTolerance = 1e-6;

            if (measurementMode == CalibrationWaferMeasurementMode.每行开始前)
            {
                double? currentRowY = null;
                foreach (LocusInfo normalLocus in normalLocusInfos)
                {
                    if (!currentRowY.HasValue || Math.Abs(normalLocus.TargetY - currentRowY.Value) > rowTolerance)
                    {
                        executionLocusInfos.Add(CreateCalibrationReferenceLocus(calibrationLocus));
                        currentRowY = normalLocus.TargetY;
                    }

                    executionLocusInfos.Add(normalLocus);
                }
            }
            else if (measurementMode == CalibrationWaferMeasurementMode.每点测量前)
            {
                foreach (LocusInfo normalLocus in normalLocusInfos)
                {
                    executionLocusInfos.Add(CreateCalibrationReferenceLocus(calibrationLocus));
                    executionLocusInfos.Add(normalLocus);
                }
            }
            else
            {
                executionLocusInfos.AddRange(normalLocusInfos);
            }

            return PointExecutionPlan.Create(executionLocusInfos, normalLocusInfos, true);
        }

        private static LocusInfo CreateCalibrationReferenceLocus(LocusInfo calibrationLocus)
        {
            return new LocusInfo
            {
                Type = LocusInfo.PointType,
                OriginX = calibrationLocus.TargetX,
                OriginY = calibrationLocus.TargetY,
                TargetX = calibrationLocus.TargetX,
                TargetY = calibrationLocus.TargetY,
                IsCalibrationReference = true
            };
        }

        private sealed class PointExecutionPlan
        {
            public List<LocusInfo> ExecutionLocusInfos { get; private set; } = new List<LocusInfo>();

            public List<(double, double)> CollectPoints { get; private set; } = new List<(double, double)>();

            public List<PointCollectionStepInfo> CollectionSteps { get; private set; } = new List<PointCollectionStepInfo>();

            public bool IsCalibrationInserted { get; private set; }

            public static PointExecutionPlan Create(
                IReadOnlyList<LocusInfo> executionLocusInfos,
                IReadOnlyList<LocusInfo> normalLocusInfos,
                bool isCalibrationInserted)
            {
                var normalIndexes = new Dictionary<LocusInfo, Queue<int>>();
                for (int i = 0; i < normalLocusInfos.Count; i++)
                {
                    if (!normalIndexes.TryGetValue(normalLocusInfos[i], out Queue<int>? indexes))
                    {
                        indexes = new Queue<int>();
                        normalIndexes[normalLocusInfos[i]] = indexes;
                    }

                    indexes.Enqueue(i);
                }

                var plan = new PointExecutionPlan
                {
                    ExecutionLocusInfos = executionLocusInfos.ToList(),
                    CollectPoints = normalLocusInfos
                        .Select(locus => (locus.TargetX, locus.TargetY))
                        .ToList(),
                    IsCalibrationInserted = isCalibrationInserted
                };

                foreach (LocusInfo locus in executionLocusInfos)
                {
                    bool isCalibrationReference = locus.IsCalibrationReference;
                    int normalPointIndex = -1;
                    if (!isCalibrationReference &&
                        normalIndexes.TryGetValue(locus, out Queue<int>? indexes) &&
                        indexes.Count > 0)
                    {
                        normalPointIndex = indexes.Dequeue();
                    }

                    plan.CollectionSteps.Add(new PointCollectionStepInfo
                    {
                        X = locus.TargetX,
                        Y = locus.TargetY,
                        IsCalibrationReference = isCalibrationReference,
                        NormalPointIndex = normalPointIndex
                    });
                }

                return plan;
            }
        }

        private void PublishStartCollectEvent(int segmentIndex, bool isAcsLci)
        {
            Logs.LogInfo($"第 {segmentIndex} 段{FormatCollectModeName(isAcsLci)}轨迹触发传感器开始采集事件：TrrigerStartCollect");
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");
        }

        private void PublishStopCollectEvent(int segmentIndex, bool isAcsLci)
        {
            Logs.LogInfo($"第 {segmentIndex} 段{FormatCollectModeName(isAcsLci)}轨迹触发传感器停止采集事件：TrrigerStopCollect");
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
        }

        private static string FormatCollectModeName(bool isAcsLci)
        {
            return isAcsLci ? "ACS LCI" : "普通";
        }

        private bool ExecuteAcsLciFixedDistancePulseSegment(
            ControlCardBase controlCard,
            LineSegment segment,
            List<(double X, double Y)> collectPoints,
            string trackingRunId,
            int trajectoryIndex)
        {
            _trajectoryTrackingPublisher.PublishProgress(trackingRunId, trajectoryIndex, trajectoryIndex - 1);
            _trajectoryTrackingPublisher.PublishTarget(trackingRunId, trajectoryIndex, segment.End.X, segment.End.Y);

            var collectionStarted = false;
            try
            {
                PublishStartCollectEvent(trajectoryIndex + 1, true);
                collectionStarted = true;
                Task.Delay(800).Wait();

                if (!TryRunLciFixedDistancePulseXseg(controlCard, segment, out var message))
                {
                    Logs.LogWarning($"ACS LCI固定距离脉冲执行失败：{message}");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Logs.LogInfo(message);
                }

                PublishLineCollectPoints(collectPoints);
                return true;
            }
            finally
            {
                if (collectionStarted)
                {
                    RecordLineSegmentEndPosition(controlCard, trajectoryIndex + 1, segment.End.X, segment.End.Y);
                    PublishStopCollectEvent(trajectoryIndex + 1, true);
                }
            }
        }

        private bool TryRunLciFixedDistancePulseXseg(
            ControlCardBase controlCard,
            LineSegment segment,
            out string message)
        {
            message = string.Empty;
            if (!TryGetAcsLciFixedDistancePulseMethod(controlCard, out var method, out var parameterType, out message))
            {
                return false;
            }

            object parameter;
            try
            {
                parameter = BuildAcsLciFixedDistancePulseParam(parameterType, segment);
            }
            catch (Exception ex)
            {
                message = $"构建ACS LCI固定距离脉冲参数失败：{ex.Message}";
                return false;
            }

            try
            {
                var args = new object?[] { parameter, null, null };
                var invokeResult = method.Invoke(controlCard, args);
                message = args.Length > 2 ? args[2]?.ToString() ?? string.Empty : string.Empty;
                return invokeResult is bool success && success;
            }
            catch (Exception ex)
            {
                message = $"调用ACS LCI固定距离脉冲失败：{ex.InnerException?.Message ?? ex.Message}";
                return false;
            }
        }

        private object BuildAcsLciFixedDistancePulseParam(Type parameterType, LineSegment segment)
        {
            var config = AcsLciPulseParam ?? new AcsLciFixedDistancePulseConfig();
            var parameter = Activator.CreateInstance(parameterType)
                ?? throw new InvalidOperationException($"无法创建ACS LCI参数类型：{parameterType.FullName}");

            var segmentLength = GetLineSegmentLength(segment);
            var endDistance = config.EndDistance > config.StartDistance
                ? config.EndDistance
                : segmentLength;
            var interval = config.Interval;

            SetAcsLciProperty(parameter, nameof(config.BufferNo), config.BufferNo);
            SetAcsLciProperty(parameter, nameof(config.AxisX), config.AxisX);
            SetAcsLciProperty(parameter, nameof(config.AxisY), config.AxisY);
            SetAcsLciProperty(parameter, nameof(config.PulseWidth), config.PulseWidth);
            SetAcsLciProperty(parameter, nameof(config.Interval), interval);
            SetAcsLciProperty(parameter, nameof(config.StartDistance), config.StartDistance);
            SetAcsLciProperty(parameter, nameof(config.EndDistance), Math.Max(config.StartDistance, endDistance));
            SetAcsLciProperty(parameter, nameof(config.RouteConfigOutput), config.RouteConfigOutput);
            SetAcsLciProperty(parameter, nameof(config.ConfigOutputIndex), config.ConfigOutputIndex);
            SetAcsLciProperty(parameter, nameof(config.ConfigOutputCode), config.ConfigOutputCode);
            SetAcsLciProperty(parameter, nameof(config.Timeout), config.Timeout);
            SetAcsLciPoints(parameter, segment);

            return parameter;
        }

        private static bool TryGetAcsLciFixedDistancePulseMethod(
            ControlCardBase controlCard,
            out MethodInfo method,
            out Type parameterType,
            out string message)
        {
            method = null!;
            parameterType = null!;
            if (controlCard == null)
            {
                message = "控制卡为空。";
                return false;
            }

            method = controlCard.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(item =>
                    item.Name == "TryRunLciFixedDistancePulseXseg" &&
                    item.GetParameters().Length == 3)!;
            if (method == null)
            {
                message = $"当前控制卡 {controlCard.GetType().Name} 不支持ACS LCI固定距离脉冲。";
                return false;
            }

            var parameters = method.GetParameters();
            parameterType = parameters[0].ParameterType;
            message = string.Empty;
            return true;
        }

        private static void SetAcsLciProperty(object parameter, string propertyName, object value)
        {
            var property = parameter.GetType().GetProperty(propertyName)
                ?? throw new InvalidOperationException($"ACS LCI参数缺少属性：{propertyName}");
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            property.SetValue(parameter, Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
        }

        private static void SetAcsLciPoints(object parameter, LineSegment segment)
        {
            var pointsProperty = parameter.GetType().GetProperty("Points")
                ?? throw new InvalidOperationException("ACS LCI参数缺少Points属性。");
            var pointType = pointsProperty.PropertyType.GetGenericArguments().FirstOrDefault()
                ?? throw new InvalidOperationException("ACS LCI Points属性不是泛型集合。");
            var listType = typeof(List<>).MakeGenericType(pointType);
            var points = (IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("无法创建ACS LCI点位集合。"));

            points.Add(CreateAcsPoint2D(pointType, segment.Start.X, segment.Start.Y));
            points.Add(CreateAcsPoint2D(pointType, segment.End.X, segment.End.Y));
            pointsProperty.SetValue(parameter, points);
        }

        private static object CreateAcsPoint2D(Type pointType, double x, double y)
        {
            return Activator.CreateInstance(pointType, x, y)
                ?? throw new InvalidOperationException("无法创建ACS LCI点位。");
        }

        private static double GetLineSegmentLength(LineSegment segment)
        {
            var dx = segment.End.X - segment.Start.X;
            var dy = segment.End.Y - segment.Start.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void PublishLineCollectPoints(List<(double X, double Y)> collectPoints)
        {
            if (collectPoints.Count > 2)
            {
                collectPoints = collectPoints
                    .Skip(1)
                    .Take(collectPoints.Count - 2)
                    .ToList();
            }
            else
            {
                Logs.LogWarning("轨迹点数量不足，已跳过首尾点剔除。");
            }

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("CollectPoints", collectPoints));
        }

        private bool SwitchPosCompare(ControlCardBase controlCard, bool change)
        {
            if (controlCard == null)
                return false;

            var posComparisonParam = PosComparisonParam ?? new PosComparisonOutputParam();
            var success = controlCard.ControlPosComparison(change, new PosComparisonOutputParam
            {
                psoIndex = posComparisonParam.psoIndex,
                compareMode = posComparisonParam.compareMode,
                compareDimension = posComparisonParam.compareDimension,
                compare_X = posComparisonParam.compare_X,
                compare_Y = posComparisonParam.compare_Y,
                comparePulseWidth = posComparisonParam.comparePulseWidth,
                compareOutputMode = posComparisonParam.compareOutputMode,
                sourceMode = posComparisonParam.sourceMode,
                compareErrBand = posComparisonParam.compareErrBand,
                syncPos = posComparisonParam.syncPos,
            });

            if (!success)
            {
                Logs.LogWarning(change ? "启用位置比较异常!" : "关闭位置比较异常!");
            }

            return success;
        }

        public static double ConvertGoogolPulseIntervalToDistance(int pulseInterval, double pulseEquivalent)
        {
            if (pulseInterval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseInterval), "固高等间距触发间隔必须大于 0。");
            }

            if (!double.IsFinite(pulseEquivalent) || pulseEquivalent <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseEquivalent), "固高插补坐标系的脉冲当量必须大于 0。");
            }

            return pulseInterval / pulseEquivalent;
        }

        public double GetSamplingSpacing()
        {
            ControlCardBase? controlCard = ResolveCurrentControlCard();
            if (IsAcsControlCard(controlCard))
            {
                double interval = AcsLciPulseParam?.Interval ?? 0;
                if (!double.IsFinite(interval) || interval <= 0)
                {
                    throw new InvalidOperationException("ACS 脉冲间距必须大于 0。");
                }

                return interval;
            }

            if (controlCard == null)
            {
                throw new InvalidOperationException("无法读取固高控制卡配置，不能将脉冲间距转换为实际位置。");
            }

            double pulseEquivalent = controlCard.Config?.DefaultInterpCS?.PulseEquivalent ?? 0;
            return ConvertGoogolPulseIntervalToDistance(PosComparisonParam.syncPos, pulseEquivalent);
        }

        private static bool IsAcsControlCard(ControlCardBase? controlCard)
        {
            return string.Equals(controlCard?.VenderName, "ACS", StringComparison.OrdinalIgnoreCase)
                || (controlCard?.GetType().FullName?.Contains(".ACS.", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private ControlCardBase? ResolveCurrentControlCard()
        {
            if (ControlCard != null)
            {
                return ControlCard;
            }

            try
            {
                return (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel)
                    ?.CardModels?
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidCalibrationWaferPosition(CalibrationWaferPosition? position)
        {
            return position != null &&
                   double.IsFinite(position.X) &&
                   double.IsFinite(position.Y);
        }

        private void ValidateExecution()
        {
            _ = GetSamplingSpacing();

            if (WaferTrajectoryMotionHelper.BuildOrderedLineSegments(AllLocusInfo).Count == 0)
            {
                throw new InvalidOperationException("请先配置至少一条有效轨迹。");
            }

            if (CreateTrajectoryModel.IsPointGenerationMode)
            {
                _ = WaferTrajectoryMotionHelper.ConvertIoMaskToUInt16(PointOperationIoMaskHex);
            }
        }

    }
}

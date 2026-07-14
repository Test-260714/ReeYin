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
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
        #endregion

        #region Properties
        [JsonIgnore]
        private CreateTrajectoryModel _createTrajectoryModel = new CreateTrajectoryModel();

        public CreateTrajectoryModel CreateTrajectoryModel
        {
            get { return _createTrajectoryModel; }
            set { _createTrajectoryModel = value;RaisePropertyChanged(); }
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
        private double _sensorInterval = 1;
        //[ReassignParam("SensorInterval", "采样间距")]
        public double SensorInterval
        {
            get => _sensorInterval;
            set
            {
                _sensorInterval = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _zHight = 100;
        //[ReassignParam("SensorInterval", "采样间距")]
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
        //[ReassignParam("SensorInterval", "采样间距")]
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

                    ValidateExecution();
                    ControlCard = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel).CardModels[0];
                    if (ControlCard == null)
                    {
                        return NodeStatus.Error;
                    }

                    LastFlatnessValue = double.NaN;

                    if (CreateTrajectoryModel.IsPointGenerationMode)
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

            if (WaferTrajectoryMotionHelper.HasAnyTrajectoryTargetOutOfSoftLimit(
                controlCard,
                WaferTrajectoryMotionHelper.BuildLineTrajectoryTargets(orderedSegments)))
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
                        NormalizeStep());

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

                    SwitchPosCompare(controlCard, true);
                    Task.Delay(5).Wait();

                    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");
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
                    SwitchPosCompare(controlCard, false);

                    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
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
                _trajectoryTrackingPublisher.PublishStop(
                    trackingRunId,
                    trackingCompleted,
                    trackingCompleted ? "Line trajectory finished." : "Line trajectory stopped with error.");
            }
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
            bool trackingCompleted = false;

            try
            {
                PointExecutionPlan pointPlan = new PointExecutionPlan();
                List<LocusInfo> orderedLocusInfos = new List<LocusInfo>();
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    pointPlan = BuildPointExecutionPlan(Calib, sourceLocusInfos);
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

                if (WaferTrajectoryMotionHelper.HasAnyTrajectoryTargetOutOfSoftLimit(
                    controlCard,
                    WaferTrajectoryMotionHelper.BuildPointTrajectoryTargets(orderedLocusInfos)))
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
                //映射IO
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("GetAllDatas");

                _trajectoryTrackingPublisher.PublishProgress(trackingRunId, 0, -1);
                _trajectoryTrackingPublisher.PublishTarget(trackingRunId, 0, orderedLocusInfos[0].TargetX, orderedLocusInfos[0].TargetY);


                //未被压完的剩余指令
                //List<LocusInfo> residualOrder = new List<LocusInfo>();
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

            if (WaferTrajectoryMotionHelper.HasAnyTrajectoryTargetOutOfSoftLimit(controlCard, new[] { moveTarget }))
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

        private PointExecutionPlan BuildPointExecutionPlan(bool calib, IEnumerable<LocusInfo>? sourceLocusInfos)
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

            var orderedLocusInfos = CreateTrajectoryModel.IsOptimalPathEnabled && !shouldInsertCalibration
                ? WaferTrajectoryMotionHelper.SortPointLocusInfosByShortestPath(executionLocusInfos)
                : executionLocusInfos;

            return PointExecutionPlan.Create(orderedLocusInfos, orderedLocusInfos, false);
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

        private double NormalizeStep()
        {
            return SensorInterval > 0 ? SensorInterval : 1;
        }

        private static bool IsValidCalibrationWaferPosition(CalibrationWaferPosition? position)
        {
            return position != null &&
                   double.IsFinite(position.X) &&
                   double.IsFinite(position.Y);
        }

        private void ValidateExecution()
        {
            if (SensorInterval <= 0)
            {
                throw new InvalidOperationException("采样间距必须大于 0。");
            }

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

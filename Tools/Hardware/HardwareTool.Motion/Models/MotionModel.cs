using Newtonsoft.Json;
using OpenCvSharp.ML;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Converters;

namespace HardwareTool.Motion.Models
{
    [Serializable]
    public class MotionModel : ModelParamBase
    {
        #region Fields
        private const double ArcGeometryTolerance = 0.001d;
        public string SltModelName;

        private sealed class ArcExecutionPlan
        {
            public Point CurrentPoint { get; set; }

            public Point ArcStartPoint { get; set; }

            public Point DestinationPoint { get; set; }

            public Point CenterPoint { get; set; }

            public Point CenterOffsetFromStart { get; set; }

            public double Radius { get; set; }

            public DrawArc DrawArcMethod { get; set; }

            public DirOfRotation Direction { get; set; }

            public bool RequiresMoveToCircle { get; set; }
        }
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<ControlCardBase> _models = new ObservableCollection<ControlCardBase>();
        [JsonIgnore]
        public ObservableCollection<ControlCardBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlCardBase _controlCard;
        [JsonIgnore]
        public ControlCardBase ControlCard
        {
            get { return _controlCard; }
            set { _controlCard = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private int _sltMovingMode = 0;
        /// <summary>
        /// 选中的运动方式
        /// </summary>
        public int SltMovingMode
        {
            get { return _sltMovingMode; }
            set { _sltMovingMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private WaferScanning _waferScanning;
        /// <summary>
        /// 晶圆扫描方式
        /// </summary>
        public WaferScanning WaferScanning
        {
            get { return _waferScanning; }
            set { _waferScanning = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sensorOutputIO = 0;
        /// <summary>
        /// 输出的IO
        /// </summary>
        public int SensorOutputIO
        {
            get { return _sensorOutputIO; }
            set { _sensorOutputIO = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _outputIODelay;
        /// <summary>
        /// 输出IO延时
        /// 指定位置延时单位：ms
        /// 位置比较（输出脉冲宽度）延时单位：us
        /// </summary>
        public int OutputIODelay
        {
            get { return _outputIODelay; }
            set { _outputIODelay = value; RaisePropertyChanged(); }
        }

        #region 指定点
        [JsonIgnore]
        private CoordinatePos _assignPosInfo = new CoordinatePos();
        /// <summary>
        /// 指定的目标位置
        /// </summary>
        public CoordinatePos AssignPosInfo
        {
            get { return _assignPosInfo; }
            set { _assignPosInfo = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsingOutputIO;
        /// <summary>
        /// 启用运动时输出指定IO
        /// </summary>
        public bool IsUsingOutputIO
        {
            get { return _isUsingOutputIO; }
            set { _isUsingOutputIO = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 晶圆扫描
        [JsonIgnore]
        private ObservableCollection<WaferScanning> scanningTracks = new ObservableCollection<WaferScanning>();
        /// <summary>
        /// 扫描轨迹
        /// </summary>
        public ObservableCollection<WaferScanning> ScanningTracks
        {
            get { return scanningTracks; }
            set { scanningTracks = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private WaferScanning _sltScanningTracks = new WaferScanning();
        /// <summary>
        /// 选中的扫描轨迹
        /// </summary>
        [JsonIgnore]
        public WaferScanning SltScanningTracks
        {
            get { return _sltScanningTracks; }
            set { _sltScanningTracks = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 自定义轨迹
        [JsonIgnore]
        private ObservableCollection<MovementLocus> _movementLocuss = new ObservableCollection<MovementLocus>();
        /// <summary>
        /// 所有轨迹
        /// </summary>
        public ObservableCollection<MovementLocus> MovementLocuss
        {
            get { return _movementLocuss; }
            set { _movementLocuss = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private MovementLocus _sltMovementLocus;
        /// <summary>
        /// 选中的轨迹
        /// </summary>
        public MovementLocus SltMovementLocus
        {
            get { return _sltMovementLocus; }
            set { _sltMovementLocus = value; RaisePropertyChanged(); }
        }

        #endregion

        #endregion

        #region Constructor
        public MotionModel()
        {
            RefreshControlCardContext();

            TriggerModuleRun += ExecuteModuleSynchronously;
        }
        #endregion

        #region Methods
        /// <summary>
        /// 刷新控制卡运行时上下文。
        /// MotionModel 可能早于硬件模块初始化完成被创建，所以这里需要延迟绑定。
        /// </summary>
        public void RefreshControlCardContext()
        {
            var hardwareModuleManager = PrismProvider.HardwareModuleManager;
            if (hardwareModuleManager?.Modules == null)
            {
                return;
            }

            if (!hardwareModuleManager.Modules.TryGetValue(ConfigKey.ControlCard, out var module))
            {
                return;
            }

            if (module is not ControlCardConfigModel controlCardConfig || controlCardConfig.CardModels == null)
            {
                return;
            }

            if (!ReferenceEquals(Models, controlCardConfig.CardModels))
            {
                Models = controlCardConfig.CardModels;
            }

            if (ControlCard == null && !string.IsNullOrWhiteSpace(SltModelName))
            {
                ControlCard = Models.FirstOrDefault(c => c.NickName == SltModelName);
            }
        }

        private List<En_AxisNum> GetInterpolationAxes()
        {
            var configuredAxisList = ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .Where(axis => axis.AxisNo > 0)
                .OrderBy(axis => axis.AxisNo)
                .Select(axis => axis.AxisNum)
                .Distinct()
                .ToList() ?? new List<En_AxisNum>();

            var configuredAxes = configuredAxisList.ToHashSet();
            var defaultAxes = ControlCard?.Config?.DefaultInterpCS?.InterPoAxiss?
                .Distinct()
                .ToList();

            if (defaultAxes != null &&
                defaultAxes.Count >= 2 &&
                defaultAxes.Count <= 5 &&
                defaultAxes.All(configuredAxes.Contains))
            {
                return defaultAxes;
            }

            if (configuredAxisList.Count >= 2 && configuredAxisList.Count <= 5)
            {
                return configuredAxisList;
            }

            return new List<En_AxisNum>();
        }

        private List<En_AxisNum> GetPlanarInterpolationAxes()
        {
            var interpolationAxes = GetInterpolationAxes();
            if (interpolationAxes.Count >= 2)
            {
                return interpolationAxes.Take(2).ToList();
            }

            return new List<En_AxisNum>();
        }

        private bool TryBuildTargetPositionMap(
            CoordinatePos coordinatePos,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            targetPositions = new Dictionary<En_AxisNum, double>();
            errorMessage = string.Empty;

            if (ControlCard?.Config?.AllAxis == null)
            {
                errorMessage = "控制卡未配置运动轴。";
                return false;
            }

            var axes = ControlCard.Config.AllAxis
                .Where(axis => axis != null)
                .ToList();

            if (axes.Count == 0)
            {
                errorMessage = "控制卡未配置运动轴。";
                return false;
            }

            if (coordinatePos?.TargetPos == null)
            {
                errorMessage = "目标坐标不能为空。";
                return false;
            }

            // CoordinatePos.TargetPos follows the configured axis list order, not the physical AxisNo.
            for (var coordinateIndex = 0; coordinateIndex < axes.Count; coordinateIndex++)
            {
                var axis = axes[coordinateIndex];
                if (axis.AxisNo <= 0)
                {
                    errorMessage = $"{axis.AxisNum}轴物理轴号无效。";
                    return false;
                }

                if (coordinateIndex >= coordinatePos.TargetPos.Count)
                {
                    errorMessage = $"{axis.AxisNum}轴缺少目标坐标。";
                    return false;
                }

                var targetPosition = coordinatePos.TargetPos[coordinateIndex];
                if (!IsFinite(targetPosition))
                {
                    errorMessage = $"{axis.AxisNum}轴目标坐标存在非法数值。";
                    return false;
                }

                targetPositions[axis.AxisNum] = targetPosition;
            }

            if (targetPositions.Count == 0)
            {
                errorMessage = "目标坐标映射为空。";
                return false;
            }

            return true;
        }

        private bool TryBuildTargetPositionMap(
            MovementLocus movementLocus,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            if (movementLocus == null)
            {
                targetPositions = new Dictionary<En_AxisNum, double>();
                errorMessage = "运动轨迹不能为空。";
                return false;
            }

            return TryBuildTargetPositionMap(movementLocus.AssignPosInfo, out targetPositions, out errorMessage);
        }

        private bool TryBuildPlanarTargetPositionMap(
            MovementLocus movementLocus,
            double firstAxisPosition,
            double secondAxisPosition,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            if (!TryBuildTargetPositionMap(movementLocus, out targetPositions, out errorMessage))
            {
                return false;
            }

            return TryApplyPlanarTargetPositions(targetPositions, firstAxisPosition, secondAxisPosition, out errorMessage);
        }

        private bool TryApplyPlanarTargetPositions(
            Dictionary<En_AxisNum, double> targetPositions,
            double firstAxisPosition,
            double secondAxisPosition,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (targetPositions == null)
            {
                errorMessage = "目标坐标映射不能为空。";
                return false;
            }

            var interpolationAxes = GetPlanarInterpolationAxes();
            if (interpolationAxes.Count < 2)
            {
                errorMessage = "控制卡至少需要配置两个插补轴。";
                return false;
            }

            if (!IsFinite(firstAxisPosition) || !IsFinite(secondAxisPosition))
            {
                errorMessage = "插补目标坐标存在非法数值。";
                return false;
            }

            targetPositions[interpolationAxes[0]] = firstAxisPosition;
            targetPositions[interpolationAxes[1]] = secondAxisPosition;
            return true;
        }

        private double[] BuildTargetPositionArray(IReadOnlyDictionary<En_AxisNum, double> targetPositions)
        {
            var axes = ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .Where(axis => axis.AxisNo > 0)
                .OrderBy(axis => axis.AxisNo)
                .ToList();

            if (axes == null || axes.Count == 0)
            {
                return Array.Empty<double>();
            }

            var maxAxisNo = axes.Max(axis => (int)axis.AxisNo);
            var positions = new double[maxAxisNo];
            if (ControlCard?.CurPos != null)
            {
                Array.Copy(ControlCard.CurPos, positions, Math.Min(ControlCard.CurPos.Length, positions.Length));
            }

            foreach (var axis in axes)
            {
                var targetIndex = axis.AxisNo - 1;
                if (targetIndex < 0 || targetIndex >= positions.Length)
                {
                    continue;
                }

                if (targetPositions != null && targetPositions.TryGetValue(axis.AxisNum, out var position))
                {
                    positions[targetIndex] = position;
                }
                else if (targetIndex >= (ControlCard?.CurPos?.Length ?? 0))
                {
                    positions[targetIndex] = axis.CurPos;
                }
            }

            return positions;
        }

        private CustomInterPoParam CreateCustomInterpolationParam(
            Dictionary<En_AxisNum, double> finalPositions,
            bool waitForEnd = true)
        {
            return new CustomInterPoParam
            {
                InterPoAxiss = GetInterpolationAxes(),
                TargetPos = BuildTargetPositionArray(finalPositions),
                TargetPosDic = finalPositions,
                waitforend = waitForEnd
            };
        }

        private LineInterPoParam CreateLineInterpolationParam(
            Dictionary<En_AxisNum, double> targetPositions,
            bool waitForEnd = true)
        {
            return new LineInterPoParam
            {
                InterPoAxiss = GetInterpolationAxes(),
                TargetPos = BuildTargetPositionArray(targetPositions),
                TargetPosDic = targetPositions,
                decZSpeed = [5, 10, 50],
                upZSpeed = [5, 10, 50],
                waitforend = waitForEnd
            };
        }

        /// <summary>
        /// 判断当前圆弧轨迹参数是否满足几何条件。
        /// </summary>
        public bool TryValidateArcMovement(MovementLocus movementLocus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!TryValidateArcDefinition(movementLocus, out errorMessage))
            {
                return false;
            }

            if (!TryBuildArcExecutionPlan(movementLocus, out _, out errorMessage))
            {
                return false;
            }

            return true;
        }

        private static bool TryValidateArcDefinition(MovementLocus movementLocus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (movementLocus == null)
            {
                errorMessage = "圆弧轨迹不能为空。";
                return false;
            }

            if (movementLocus.IsSimulateFullCircle && movementLocus.SltDrawingMethod != 0)
            {
                errorMessage = "整圆仅支持以圆心方式绘制。";
                return false;
            }

            if (!movementLocus.IsSimulateFullCircle)
            {
                Point destination = new Point(movementLocus.DestinationX, movementLocus.DestinationY);
                if (!IsFinitePoint(destination))
                {
                    errorMessage = "圆弧终点存在非法数值。";
                    return false;
                }
            }

            return movementLocus.SltDrawingMethod switch
            {
                0 => TryValidateArcByCenterDefinition(
                    new Point(movementLocus.CenterX, movementLocus.CenterY),
                    movementLocus.IsSimulateFullCircle ? null : new Point(movementLocus.DestinationX, movementLocus.DestinationY),
                    movementLocus.IsSimulateFullCircle,
                    out errorMessage),
                1 => TryValidateArcByRadiusDefinition(movementLocus.Radius, out errorMessage),
                _ => FailArcValidation("圆弧绘制方式无效。", out errorMessage)
            };
        }

        private static bool TryValidateArcByCenterDefinition(Point center, Point? destination, bool isFullCircle, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!IsFinitePoint(center))
            {
                errorMessage = "圆心坐标存在非法数值。";
                return false;
            }

            if (isFullCircle)
            {
                return true;
            }

            if (!destination.HasValue || !IsFinitePoint(destination.Value))
            {
                errorMessage = "圆弧终点存在非法数值。";
                return false;
            }

            if (IsSamePoint(center, destination.Value))
            {
                errorMessage = "以圆心绘制时，圆心不能与终点重合。";
                return false;
            }

            return true;
        }

        private static bool TryValidateArcByRadiusDefinition(double radius, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!IsFinite(radius))
            {
                errorMessage = "半径存在非法数值。";
                return false;
            }

            if (Math.Abs(radius) <= ArcGeometryTolerance)
            {
                errorMessage = "圆弧半径必须大于 0。";
                return false;
            }

            return true;
        }

        private bool TryBuildArcExecutionPlan(MovementLocus movementLocus, out ArcExecutionPlan plan, out string errorMessage)
        {
            plan = null!;
            errorMessage = string.Empty;

            if (!TryValidateArcDefinition(movementLocus, out errorMessage))
            {
                return false;
            }

            if (!TryGetCurrentArcPoint(movementLocus, out Point currentPoint))
            {
                errorMessage = "无法获取当前 XY 坐标，无法准备圆弧运动。";
                return false;
            }

            Point destination = movementLocus.IsSimulateFullCircle
                ? currentPoint
                : new Point(movementLocus.DestinationX, movementLocus.DestinationY);
            DirOfRotation direction = movementLocus.Rotation ? DirOfRotation.顺时针 : DirOfRotation.逆时针;

            return movementLocus.SltDrawingMethod switch
            {
                0 => TryBuildArcByCenterExecutionPlan(movementLocus, currentPoint, destination, direction, out plan, out errorMessage),
                1 => TryBuildArcByRadiusExecutionPlan(currentPoint, destination, movementLocus.Radius, direction, out plan, out errorMessage),
                _ => FailArcExecutionPlan("圆弧绘制方式无效。", out plan, out errorMessage)
            };
        }

        private static bool TryBuildArcByCenterExecutionPlan(
            MovementLocus movementLocus,
            Point currentPoint,
            Point destination,
            DirOfRotation direction,
            out ArcExecutionPlan plan,
            out string errorMessage)
        {
            plan = null!;
            errorMessage = string.Empty;

            Point center = new Point(movementLocus.CenterX, movementLocus.CenterY);
            if (!IsFinitePoint(center))
            {
                errorMessage = "圆心坐标存在非法数值。";
                return false;
            }

            Point radiusReferencePoint = movementLocus.IsSimulateFullCircle ? currentPoint : destination;
            double radius = GetDistance(radiusReferencePoint, center);
            if (radius <= ArcGeometryTolerance)
            {
                errorMessage = movementLocus.IsSimulateFullCircle
                    ? "整圆执行时，当前 XY 不能与圆心重合。"
                    : "以圆心绘制时，圆心不能与终点重合。";
                return false;
            }

            if (!ControlCardBase.TryGetNearestPointOnCircle(
                center,
                radius,
                currentPoint,
                out Point pointOnCircle,
                out Point centerOffsetFromStart,
                ArcGeometryTolerance))
            {
                errorMessage = "根据当前坐标计算圆上起点失败。";
                return false;
            }

            Point finalDestination = movementLocus.IsSimulateFullCircle ? pointOnCircle : destination;
            if (!movementLocus.IsSimulateFullCircle && IsSamePoint(pointOnCircle, finalDestination))
            {
                errorMessage = "当前位置修正到圆上后与终点重合，普通圆弧将退化；请调整当前位置或启用整圆模式。";
                return false;
            }

            plan = new ArcExecutionPlan
            {
                CurrentPoint = currentPoint,
                ArcStartPoint = pointOnCircle,
                DestinationPoint = finalDestination,
                CenterPoint = new Point(
                    pointOnCircle.X + centerOffsetFromStart.X,
                    pointOnCircle.Y + centerOffsetFromStart.Y),
                CenterOffsetFromStart = centerOffsetFromStart,
                Radius = radius,
                DrawArcMethod = DrawArc.Center,
                Direction = direction,
                RequiresMoveToCircle = !IsSamePoint(currentPoint, pointOnCircle)
            };

            return true;
        }

        private static bool TryBuildArcByRadiusExecutionPlan(
            Point currentPoint,
            Point destination,
            double radius,
            DirOfRotation direction,
            out ArcExecutionPlan plan,
            out string errorMessage)
        {
            plan = null!;
            errorMessage = string.Empty;

            double validRadius = Math.Abs(radius);
            if (!IsFinite(validRadius) || validRadius <= ArcGeometryTolerance)
            {
                errorMessage = "圆弧半径必须大于 0。";
                return false;
            }

            if (IsSamePoint(currentPoint, destination))
            {
                errorMessage = "普通圆弧的起点与终点不能相同；整圆请启用整圆模式。";
                return false;
            }

            double chordLength = GetDistance(currentPoint, destination);
            if (chordLength > (validRadius * 2d) + ArcGeometryTolerance)
            {
                errorMessage = "圆弧半径过小，无法连接当前起点和终点。";
                return false;
            }

            plan = new ArcExecutionPlan
            {
                CurrentPoint = currentPoint,
                ArcStartPoint = currentPoint,
                DestinationPoint = destination,
                CenterPoint = default,
                CenterOffsetFromStart = default,
                Radius = validRadius,
                DrawArcMethod = DrawArc.Radius,
                Direction = direction,
                RequiresMoveToCircle = false
            };

            return true;
        }

        private static bool FailArcValidation(string message, out string errorMessage)
        {
            errorMessage = message;
            return false;
        }

        private static bool FailArcExecutionPlan(string message, out ArcExecutionPlan plan, out string errorMessage)
        {
            plan = null!;
            errorMessage = message;
            return false;
        }

        private static bool IsFinitePoint(Point point)
        {
            return IsFinite(point.X) && IsFinite(point.Y);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsSamePoint(Point left, Point right)
        {
            return GetDistance(left, right) <= ArcGeometryTolerance;
        }

        private static double GetDistance(Point start, Point end)
        {
            double deltaX = start.X - end.X;
            double deltaY = start.Y - end.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        private bool ExecuteArcInterpolationSequence(MovementLocus movementLocus)
        {
            if (ControlCard == null)
            {
                Logs.LogWarning("执行圆弧失败：未找到可用的控制卡。");
                return false;
            }

            if (!TryBuildArcExecutionPlan(movementLocus, out var executionPlan, out string errorMessage))
            {
                Logs.LogWarning($"圆弧参数非法，不符合要求：{errorMessage}");
                return false;
            }

            if (!TryBuildPlanarTargetPositionMap(
                    movementLocus,
                    executionPlan.DestinationPoint.X,
                    executionPlan.DestinationPoint.Y,
                    out var finalPositions,
                    out errorMessage))
            {
                Logs.LogWarning($"圆弧目标坐标非法：{errorMessage}");
                return false;
            }

            if (executionPlan.RequiresMoveToCircle &&
                (!TryCreateMoveToCircleParam(executionPlan, finalPositions, out var moveToCircleParam, out errorMessage) ||
                !ControlCard.LineInterpoMoving(moveToCircleParam)))
            {
                Logs.LogWarning($"当前位置未在目标圆上，移动到最近圆上点失败：{errorMessage}");
                return false;
            }

            return ControlCard.ArcInterpoMoving(CreateArcInterPoParam(executionPlan, finalPositions));
        }

        private ArcInterPoParam CreateArcInterPoParam(
            ArcExecutionPlan executionPlan,
            Dictionary<En_AxisNum, double> finalPositions)
        {
            return new ArcInterPoParam
            {
                DrawArcMethod = executionPlan.DrawArcMethod,
                InterPoAxiss = GetInterpolationAxes(),
                Origin = executionPlan.ArcStartPoint,
                Destination = executionPlan.DestinationPoint,
                Center = executionPlan.CenterPoint,
                Radius = executionPlan.Radius,
                Dir = executionPlan.Direction,
                FinalPosDic = finalPositions,
                waitforend = true,
            };
        }

        private bool TryCreateMoveToCircleParam(
            ArcExecutionPlan executionPlan,
            Dictionary<En_AxisNum, double> finalPositions,
            out LineInterPoParam moveToCircleParam,
            out string errorMessage)
        {
            moveToCircleParam = null!;
            var moveToCirclePositions = new Dictionary<En_AxisNum, double>(finalPositions);
            if (!TryApplyPlanarTargetPositions(
                    moveToCirclePositions,
                    executionPlan.ArcStartPoint.X,
                    executionPlan.ArcStartPoint.Y,
                    out errorMessage))
            {
                return false;
            }

            moveToCircleParam = CreateLineInterpolationParam(moveToCirclePositions, true);
            return true;
        }

        private bool TryGetCurrentArcPoint(MovementLocus movementLocus, out Point currentPoint)
        {
            if (TryGetCurrentXYPoint(out currentPoint))
            {
                return true;
            }

            currentPoint = new Point(movementLocus.OriginX, movementLocus.OriginY);
            return IsFinitePoint(currentPoint);
        }

        private bool TryGetCurrentXYPoint(out Point currentPoint)
        {
            currentPoint = default;

            if (ControlCard == null)
            {
                return false;
            }

            try
            {
                ControlCard.GetAllPosInfos();
            }
            catch
            {
                return false;
            }

            if (!TryGetAxisCurrentPosition(En_AxisNum.X, out double xPos) ||
                !TryGetAxisCurrentPosition(En_AxisNum.Y, out double yPos))
            {
                return false;
            }

            currentPoint = new Point(xPos, yPos);
            return IsFinitePoint(currentPoint);
        }

        private bool TryGetAxisCurrentPosition(En_AxisNum axisNum, out double axisPosition)
        {
            axisPosition = default;

            var axisConfig = ControlCard?.Config?.AllAxis?.FirstOrDefault(item => item.AxisNum == axisNum);
            if (axisConfig == null)
            {
                return false;
            }

            if (ControlCard!.CurPos != null &&
                axisConfig.AxisNo > 0 &&
                axisConfig.AxisNo <= ControlCard.CurPos.Length)
            {
                axisPosition = ControlCard.CurPos[axisConfig.AxisNo - 1];
                return IsFinite(axisPosition);
            }

            axisPosition = axisConfig.CurPos;
            return IsFinite(axisPosition);
        }

        private static string FormatPoint(Point point)
        {
            return $"({point.X:F3}, {point.Y:F3})";
        }

        private bool ExecuteMovementLocus(MovementLocus movementLocus)
        {
            if (movementLocus == null)
            {
                return false;
            }

            return movementLocus.MovingMode switch
            {
                CardOperaion.点 => ExecutePointMovement(movementLocus),
                CardOperaion.IO => ExecuteIoOperation(movementLocus),
                CardOperaion.直线线段 => ExecuteLineSegmentMovement(movementLocus),
                CardOperaion.圆弧线段 => ExecuteArcSegmentMovement(movementLocus),
                CardOperaion.位置比较 => true,
                CardOperaion.延时 => ExecuteDelayOperation(movementLocus),
                CardOperaion.触发事件 => ExecuteTriggerEventOperation(movementLocus),
                CardOperaion.自定义 => true,
                _ => true
            };
        }

        private bool ExecutePointMovement(MovementLocus movementLocus)
        {
            if (!TryBuildTargetPositionMap(movementLocus, out var targetPositions, out string errorMessage))
            {
                Logs.LogWarning($"点位运动目标坐标非法：{errorMessage}");
                return false;
            }

            return ExecuteLineWithCustomInterpolation(targetPositions, true);
        }

        private bool ExecuteLineSegmentMovement(MovementLocus movementLocus)
        {
            if (!TryBuildPlanarTargetPositionMap(
                    movementLocus,
                    movementLocus.OriginX,
                    movementLocus.OriginY,
                    out var originPositions,
                    out string errorMessage))
            {
                Logs.LogWarning($"直线起点坐标非法：{errorMessage}");
                return false;
            }

            if (!ExecuteLineWithCustomInterpolation(originPositions, true))
            {
                Logs.LogWarning("移动至直线起点失败。");
                return false;
            }

            Task.Delay(5).Wait();
            var posCompareEnabled = false;
            try
            {
                if (movementLocus.IsUsingValid)
                {
                    movementLocus.Switch = true;
                    if (!SwitchPosCompare(movementLocus))
                    {
                        return false;
                    }

                    posCompareEnabled = true;
                }

                if (!TryBuildPlanarTargetPositionMap(
                        movementLocus,
                        movementLocus.DestinationX,
                        movementLocus.DestinationY,
                        out var destinationPositions,
                        out errorMessage))
                {
                    Logs.LogWarning($"直线终点坐标非法：{errorMessage}");
                    return false;
                }

                return ExecuteLineWithCustomInterpolation(destinationPositions, true);
            }
            finally
            {
                if (posCompareEnabled)
                {
                    movementLocus.Switch = false;
                    SwitchPosCompare(movementLocus);
                }
            }
        }

        private bool ExecuteArcSegmentMovement(MovementLocus movementLocus)
        {
            if (!TryValidateArcMovement(movementLocus, out string errorMessage))
            {
                Logs.LogWarning($"输入的圆弧参数非法，不符合要求：{errorMessage}");
                return false;
            }

            if (!TryBuildTargetPositionMap(movementLocus, out var targetPositions, out errorMessage))
            {
                Logs.LogWarning($"圆弧目标坐标非法：{errorMessage}");
                return false;
            }

            if (ControlCard == null)
            {
                Logs.LogWarning("执行圆弧失败：未找到可用的控制卡。");
                return false;
            }

            return ControlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions),
                () => ExecuteArcInterpolationSequence(movementLocus) ? "OK" : "NG",
                true);
        }

        private bool ExecuteLineWithCustomInterpolation(
            Dictionary<En_AxisNum, double> targetPositions,
            bool waitForEnd)
        {
            if (ControlCard == null)
            {
                Logs.LogWarning("执行直线插补失败：未找到可用的控制卡。");
                return false;
            }

            return ControlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions, waitForEnd),
                () => ControlCard.LineInterpoMoving(CreateLineInterpolationParam(targetPositions, waitForEnd)) ? "OK" : "NG",
                waitForEnd);
        }

        private bool ExecuteIoOperation(MovementLocus movementLocus)
        {
            if (ControlCard == null)
            {
                Logs.LogWarning("执行IO操作失败：未找到可用的控制卡。");
                return false;
            }

            if (movementLocus.OutputIODelay == 0)
            {
                return ControlCard.SetSpecifiedIO(movementLocus.OutputIO, movementLocus.OutputIOStatus);
            }

            Task.Run(() =>
            {
                Task.Delay(movementLocus.OutputIODelay).Wait();
                if (!ControlCard.SetSpecifiedIO(movementLocus.OutputIO, !movementLocus.OutputIOStatus))
                {
                    Logs.LogWarning($"延时设置IO{movementLocus.OutputIO}失败。");
                }
            });

            return true;
        }

        private static bool ExecuteDelayOperation(MovementLocus movementLocus)
        {
            Task.Delay(movementLocus.OutputIODelay * 1000).Wait();
            return true;
        }

        private static bool ExecuteTriggerEventOperation(MovementLocus movementLocus)
        {
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish(movementLocus.EventName);
            return true;
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
                    RefreshControlCardContext();

                    if (ControlCard == null && SltModelName != null && SltModelName != "")
                    {
                        ControlCard = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
                    }

                    if (ControlCard == null)
                    {
                        Logs.LogWarning("运动模块执行失败：未找到可用的控制卡。");
                        return NodeStatus.Error;
                    }

                    switch (SltMovingMode)
                    {
                        //自定义轨迹
                        case 0:
                            {
                                foreach (var MovementLocus in MovementLocuss)
                                {
                                    if (MovementLocus.IsUsing)
                                    {
                                        if (!ExecuteMovementLocus(MovementLocus))
                                        {
                                            return NodeStatus.Error;
                                        }
                                    }
                                }

                                Console.WriteLine("执行结束");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                    return NodeStatus.Error;
                }

                Console.WriteLine($"开始输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    Console.WriteLine(JsonHelper.Serialize(item.Value));
                }

                #region 输出
                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                Console.WriteLine($"结束输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                #endregion

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：运动模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };

        }

        private ExecuteModuleOutput ExecuteModuleSynchronously()
        {
            return ExecuteModule().Result;
        }

        /// <summary>
        /// 切换位置比较
        /// </summary>
        /// <returns></returns>
        public bool SwitchPosCompare(MovementLocus MovementLocus)
        {
            RefreshControlCardContext();

            if (ControlCard == null)
            {
                MessageBox.Show("请先选择控制卡", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (MovementLocus.Switch)
            {
                if (!ControlCard.ControlPosComparison(true, new PosComparisonOutputParam
                {
                    psoIndex = MovementLocus.PosComparisonParam.psoIndex,
                    compareMode = MovementLocus.PosComparisonParam.compareMode,
                    compareDimension = MovementLocus.PosComparisonParam.compareDimension,
                    compare_X = MovementLocus.PosComparisonParam.compare_X,
                    compare_Y = MovementLocus.PosComparisonParam.compare_Y,
                    comparePulseWidth = (ushort)MovementLocus.PosComparisonParam.comparePulseWidth,
                    compareOutputMode = MovementLocus.PosComparisonParam.compareOutputMode,
                    sourceMode = MovementLocus.PosComparisonParam.sourceMode,
                    compareErrBand = MovementLocus.PosComparisonParam.compareErrBand,
                    syncPos = MovementLocus.PosComparisonParam.syncPos,
                    //posCompareDatas = new Queue<PosCompareData>(MovementLocus.PosComparisonParam.PosCompareDatas)
                }))
                {
                    Logs.LogWarning("启用位置比较异常!");
                    return false;
                }
            }
            else
            {
                if (!ControlCard.ControlPosComparison(false, new PosComparisonOutputParam
                {
                    psoIndex = MovementLocus.PosComparisonParam.psoIndex,
                    compareMode = MovementLocus.PosComparisonParam.compareMode,
                    compareDimension = MovementLocus.PosComparisonParam.compareDimension,
                    compare_X = MovementLocus.PosComparisonParam.compare_X,
                    compare_Y = MovementLocus.PosComparisonParam.compare_Y,
                    comparePulseWidth = (ushort)MovementLocus.PosComparisonParam.comparePulseWidth,
                    compareOutputMode = MovementLocus.PosComparisonParam.compareOutputMode,
                    sourceMode = MovementLocus.PosComparisonParam.sourceMode,
                    compareErrBand = MovementLocus.PosComparisonParam.compareErrBand,
                    syncPos = MovementLocus.PosComparisonParam.syncPos,
                    //posCompareDatas = new Queue<PosCompareData>(MovementLocus.PosComparisonParam.PosCompareDatas)
                }))
                {
                    Logs.LogWarning("关闭位置比较异常!");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 自定义运动
        /// </summary>
        /// <param name="movementLocus"></param>
        public void CustomMoving(MovementLocus movementLocus)
        {
            if (movementLocus == null) return;

            RefreshControlCardContext();

            if (ControlCard == null)
            {
                MessageBox.Show("请先选择控制卡", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show("确定要移动至目标位置吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (!ExecuteMovementLocus(movementLocus))
            {
                MessageBox.Show("运动执行失败，请检查控制卡状态和运动参数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion
    }
}

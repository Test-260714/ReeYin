using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DelegateCommand = Prism.Commands.DelegateCommand;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// 轨迹监控控件的状态协调层，负责接收外部轨迹/点位并刷新画布层和图表层绑定数据。
    /// </summary>
    public sealed class AxisMotionTrajectoryMonitorViewModel : BindableBase
    {
        private const string DefaultMonitorTitle = "XY轴组运动监控";
        private const double PositionTolerance = 0.001d;
        private const double MarkerSize = 24d;
        private const int SimulationCircleSegmentCount = 180;

        private readonly AxisMotionTrajectoryMonitorModel _model = new();
        // 外部轨迹集合和单条轨迹都可能变化，需分别记录订阅对象以便重新绑定或释放。
        private readonly HashSet<AxisTrajectoryItem> _subscribedTrajectories = new();
        private readonly List<AxisTrajectoryItem> _trackedTrajectories = new();
        // 已执行轨迹保存世界坐标点；刷新画布和图表时再转换为对应坐标系。
        private readonly List<Point> _motionTraceWorldPoints = new();

        private IEnumerable<AxisTrajectoryItem>? _trajectorySource;
        private INotifyCollectionChanged? _notifyingTrajectorySource;
        private AxisMonitorBounds _bounds = new(
            AxisMotionTrajectoryMonitorModel.DefaultXMinimum,
            AxisMotionTrajectoryMonitorModel.DefaultXMaximum,
            AxisMotionTrajectoryMonitorModel.DefaultYMinimum,
            AxisMotionTrajectoryMonitorModel.DefaultYMaximum);
        private Point _currentPosition = new(double.NaN, double.NaN);
        private Point _targetPosition = new(double.NaN, double.NaN);
        private string _monitorTitle = DefaultMonitorTitle;
        private string _statusText = "尚未接收到 XY 轴当前位置。";
        private string _positionStateText = "等待反馈";
        private string _currentPositionText = "--";
        private string _targetPositionText = "--";
        private string _currentXText = "--";
        private string _currentYText = "--";
        private string _targetXText = "--";
        private string _targetYText = "--";
        private string _remainingDistanceText = "--";
        private string _executedDistanceText = "--";
        private string _tracePointCountText = "暂无轨迹采样";
        private string _trajectoryCountText = "未加载";
        private string _trajectorySummaryText = "当前未加载规划轨迹层。";
        private string _plannedPathLengthText = "--";
        private string _axisRangeText = "X 0.000 ~ 800.000    Y 0.000 ~ 800.000";
        private int _trajectoryCount;
        private double _plannedPathLength;
        private double _executedPathLength;
        private IReadOnlyList<AxisMonitorLineVisual> _gridLines = Array.Empty<AxisMonitorLineVisual>();
        private IReadOnlyList<AxisMonitorLabelVisual> _xAxisLabels = Array.Empty<AxisMonitorLabelVisual>();
        private IReadOnlyList<AxisMonitorLabelVisual> _yAxisLabels = Array.Empty<AxisMonitorLabelVisual>();
        private IReadOnlyList<AxisMonitorPathVisual> _plannedTrajectories = Array.Empty<AxisMonitorPathVisual>();
        private AxisXCollection _chartXAxes = new();
        private AxisYCollection _chartYAxes = new();
        private FreeformPointLineSeriesCollection _chartSeries = new();
        private PointCollection _motionTracePoints = new();
        private Visibility _currentMarkerVisibility = Visibility.Collapsed;
        private Visibility _targetMarkerVisibility = Visibility.Collapsed;
        private Visibility _motionTraceVisibility = Visibility.Collapsed;
        private double _currentPointX = AxisMotionTrajectoryMonitorModel.PlotLeft;
        private double _currentPointY = AxisMotionTrajectoryMonitorModel.PlotBottom;
        private double _currentMarkerLeft = AxisMotionTrajectoryMonitorModel.PlotLeft - (MarkerSize / 2d);
        private double _currentMarkerTop = AxisMotionTrajectoryMonitorModel.PlotBottom - (MarkerSize / 2d);
        private double _targetPointX = AxisMotionTrajectoryMonitorModel.PlotLeft;
        private double _targetPointY = AxisMotionTrajectoryMonitorModel.PlotBottom;
        private double _targetMarkerLeft = AxisMotionTrajectoryMonitorModel.PlotLeft - (MarkerSize / 2d);
        private double _targetMarkerTop = AxisMotionTrajectoryMonitorModel.PlotBottom - (MarkerSize / 2d);
        private IReadOnlyList<AxisMonitorPathVisual> _simulationTrajectories = Array.Empty<AxisMonitorPathVisual>();
        private double _circleCenterX = (AxisMotionTrajectoryMonitorModel.DefaultXMinimum + AxisMotionTrajectoryMonitorModel.DefaultXMaximum) / 2d;
        private double _circleCenterY = (AxisMotionTrajectoryMonitorModel.DefaultYMinimum + AxisMotionTrajectoryMonitorModel.DefaultYMaximum) / 2d;
        private double _circleRadius = 120d;
        private bool _showTrajectoryLines = true;
        private string _circleSimulationStatusText = "输入圆心和半径后，点击预览圆轨迹。";
        private bool _hasCircleSimulationPreview;
        private AxisMotionTrajectoryMonitorAppearance _appearance = AxisMotionTrajectoryMonitorAppearance.Default;

        public AxisMotionTrajectoryMonitorViewModel()
        {
            RefreshAxisFrame();
            RefreshPlannedTrajectoryLayer();
            RefreshCircleSimulationLayer();
            RefreshMotionTraceLayer();
            RefreshMarkerLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        public double CanvasSize => AxisMotionTrajectoryMonitorModel.CanvasSize;

        public double PlotLeft => AxisMotionTrajectoryMonitorModel.PlotLeft;

        public double PlotTop => AxisMotionTrajectoryMonitorModel.PlotTop;

        public double PlotWidth => AxisMotionTrajectoryMonitorModel.PlotWidth;

        public double PlotHeight => AxisMotionTrajectoryMonitorModel.PlotHeight;

        public double PlotRight => AxisMotionTrajectoryMonitorModel.PlotRight;

        public double PlotBottom => AxisMotionTrajectoryMonitorModel.PlotBottom;

        public IReadOnlyList<AxisMonitorLineVisual> GridLines
        {
            get => _gridLines;
            private set => SetProperty(ref _gridLines, value);
        }

        public IReadOnlyList<AxisMonitorLabelVisual> XAxisLabels
        {
            get => _xAxisLabels;
            private set => SetProperty(ref _xAxisLabels, value);
        }

        public IReadOnlyList<AxisMonitorLabelVisual> YAxisLabels
        {
            get => _yAxisLabels;
            private set => SetProperty(ref _yAxisLabels, value);
        }

        public IReadOnlyList<AxisMonitorPathVisual> PlannedTrajectories
        {
            get => _plannedTrajectories;
            private set => SetProperty(ref _plannedTrajectories, value);
        }

        public AxisXCollection ChartXAxes
        {
            get => _chartXAxes;
            private set => SetProperty(ref _chartXAxes, value);
        }

        public AxisYCollection ChartYAxes
        {
            get => _chartYAxes;
            private set => SetProperty(ref _chartYAxes, value);
        }

        public FreeformPointLineSeriesCollection ChartSeries
        {
            get => _chartSeries;
            private set => SetProperty(ref _chartSeries, value);
        }

        public IReadOnlyList<AxisMonitorPathVisual> SimulationTrajectories
        {
            get => _simulationTrajectories;
            private set => SetProperty(ref _simulationTrajectories, value);
        }

        public PointCollection MotionTracePoints
        {
            get => _motionTracePoints;
            private set => SetProperty(ref _motionTracePoints, value);
        }

        public string MonitorTitle
        {
            get => _monitorTitle;
            private set => SetProperty(ref _monitorTitle, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string PositionStateText
        {
            get => _positionStateText;
            private set => SetProperty(ref _positionStateText, value);
        }

        public string CurrentPositionText
        {
            get => _currentPositionText;
            private set => SetProperty(ref _currentPositionText, value);
        }

        public string TargetPositionText
        {
            get => _targetPositionText;
            private set => SetProperty(ref _targetPositionText, value);
        }

        public string CurrentXText
        {
            get => _currentXText;
            private set => SetProperty(ref _currentXText, value);
        }

        public string CurrentYText
        {
            get => _currentYText;
            private set => SetProperty(ref _currentYText, value);
        }

        public string TargetXText
        {
            get => _targetXText;
            private set => SetProperty(ref _targetXText, value);
        }

        public string TargetYText
        {
            get => _targetYText;
            private set => SetProperty(ref _targetYText, value);
        }

        public string RemainingDistanceText
        {
            get => _remainingDistanceText;
            private set => SetProperty(ref _remainingDistanceText, value);
        }

        public string ExecutedDistanceText
        {
            get => _executedDistanceText;
            private set => SetProperty(ref _executedDistanceText, value);
        }

        public string TracePointCountText
        {
            get => _tracePointCountText;
            private set => SetProperty(ref _tracePointCountText, value);
        }

        public string TrajectoryCountText
        {
            get => _trajectoryCountText;
            private set => SetProperty(ref _trajectoryCountText, value);
        }

        public string TrajectorySummaryText
        {
            get => _trajectorySummaryText;
            private set => SetProperty(ref _trajectorySummaryText, value);
        }

        public string PlannedPathLengthText
        {
            get => _plannedPathLengthText;
            private set => SetProperty(ref _plannedPathLengthText, value);
        }

        public string AxisRangeText
        {
            get => _axisRangeText;
            private set => SetProperty(ref _axisRangeText, value);
        }

        public Visibility CurrentMarkerVisibility
        {
            get => _currentMarkerVisibility;
            private set => SetProperty(ref _currentMarkerVisibility, value);
        }

        public Visibility TargetMarkerVisibility
        {
            get => _targetMarkerVisibility;
            private set => SetProperty(ref _targetMarkerVisibility, value);
        }

        public Visibility MotionTraceVisibility
        {
            get => _motionTraceVisibility;
            private set => SetProperty(ref _motionTraceVisibility, value);
        }

        public double CurrentPointX
        {
            get => _currentPointX;
            private set => SetProperty(ref _currentPointX, value);
        }

        public double CurrentPointY
        {
            get => _currentPointY;
            private set => SetProperty(ref _currentPointY, value);
        }

        public double CurrentMarkerLeft
        {
            get => _currentMarkerLeft;
            private set => SetProperty(ref _currentMarkerLeft, value);
        }

        public double CurrentMarkerTop
        {
            get => _currentMarkerTop;
            private set => SetProperty(ref _currentMarkerTop, value);
        }

        public double TargetPointX
        {
            get => _targetPointX;
            private set => SetProperty(ref _targetPointX, value);
        }

        public double TargetPointY
        {
            get => _targetPointY;
            private set => SetProperty(ref _targetPointY, value);
        }

        public double TargetMarkerLeft
        {
            get => _targetMarkerLeft;
            private set => SetProperty(ref _targetMarkerLeft, value);
        }

        public double TargetMarkerTop
        {
            get => _targetMarkerTop;
            private set => SetProperty(ref _targetMarkerTop, value);
        }

        public double CircleCenterX
        {
            get => _circleCenterX;
            set => SetProperty(ref _circleCenterX, value);
        }

        public double CircleCenterY
        {
            get => _circleCenterY;
            set => SetProperty(ref _circleCenterY, value);
        }

        public double CircleRadius
        {
            get => _circleRadius;
            set => SetProperty(ref _circleRadius, value);
        }

        public string CircleSimulationStatusText
        {
            get => _circleSimulationStatusText;
            private set => SetProperty(ref _circleSimulationStatusText, value);
        }

        public DelegateCommand PreviewCircleCommand => new DelegateCommand(PreviewCircle);

        public DelegateCommand ClearCirclePreviewCommand => new DelegateCommand(ClearCirclePreview);

        public void SetAppearance(AxisMotionTrajectoryMonitorAppearance? appearance)
        {
            _appearance = appearance ?? AxisMotionTrajectoryMonitorAppearance.Default;
            RefreshAxisFrame();
            RefreshPlannedTrajectoryLayer();
            RefreshCircleSimulationLayer();
            RefreshMotionTraceLayer();
            RefreshMarkerLayer();
            RefreshChartSeries();
        }

        public void SetShowTrajectoryLines(bool showTrajectoryLines)
        {
            if (_showTrajectoryLines == showTrajectoryLines)
            {
                return;
            }

            _showTrajectoryLines = showTrajectoryLines;
            RefreshPlannedTrajectoryLayer();
            RefreshChartSeries();
        }

        public void SetMonitorTitle(string? monitorTitle)
        {
            MonitorTitle = string.IsNullOrWhiteSpace(monitorTitle)
                ? DefaultMonitorTitle
                : monitorTitle.Trim();
        }

        public void SetAxisRange(double xMinimum, double xMaximum, double yMinimum, double yMaximum)
        {
            // 坐标范围变化会影响所有投影结果，因此需要刷新坐标轴、轨迹层、标记层和摘要。
            _bounds = _model.CreateBounds(xMinimum, xMaximum, yMinimum, yMaximum);
            RefreshAxisFrame();
            RefreshPlannedTrajectoryLayer();
            RefreshCircleSimulationLayer();
            RefreshMotionTraceLayer();
            RefreshMarkerLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        public void SetTrajectories(IEnumerable<AxisTrajectoryItem>? trajectories)
        {
            // 轨迹源可能来自 ObservableCollection，也可能是普通枚举；统一重建跟踪列表。
            DetachTrajectorySource();

            _trajectorySource = trajectories;
            _notifyingTrajectorySource = trajectories as INotifyCollectionChanged;
            if (_notifyingTrajectorySource != null)
            {
                _notifyingTrajectorySource.CollectionChanged += OnTrajectoryCollectionChanged;
            }

            RefreshTrackedTrajectories();
            RefreshPlannedTrajectoryLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        public void SetCurrentPosition(Point currentPosition)
        {
            // 无效当前位置表示外部反馈中断，需清空已执行轨迹并回到等待状态。
            if (!IsFinitePoint(currentPosition))
            {
                ResetExecutionState();
                RefreshMotionTraceLayer();
                RefreshMarkerLayer();
                RefreshChartSeries();
                UpdateSummary();
                return;
            }

            bool hadPreviousPoint = IsFinitePoint(_currentPosition);
            if (!hadPreviousPoint)
            {
                // 第一个有效点只作为运动痕迹起点，不计入已执行距离。
                _motionTraceWorldPoints.Clear();
                _motionTraceWorldPoints.Add(currentPosition);
            }
            else
            {
                double segmentLength = Distance(_currentPosition, currentPosition);
                if (segmentLength > 1e-6)
                {
                    // 忽略极小抖动，避免点位噪声造成痕迹和里程数持续膨胀。
                    _motionTraceWorldPoints.Add(currentPosition);
                    _executedPathLength += segmentLength;
                }
            }

            _currentPosition = currentPosition;
            RefreshMotionTraceLayer();
            RefreshMarkerLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        public void SetTargetPosition(Point targetPosition)
        {
            _targetPosition = IsFinitePoint(targetPosition)
                ? targetPosition
                : new Point(double.NaN, double.NaN);

            RefreshMarkerLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        private void OnTrajectoryCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 集合增删会影响订阅关系和统计信息，直接重建轨迹层最稳妥。
            RefreshTrackedTrajectories();
            RefreshPlannedTrajectoryLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        private void OnTrajectoryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AxisTrajectoryItem)
            {
                return;
            }

            // 轨迹点、状态或可见性变化都会影响展示和摘要，按完整轨迹层刷新处理。
            RefreshPlannedTrajectoryLayer();
            RefreshChartSeries();
            UpdateSummary();
        }

        private void RefreshTrackedTrajectories()
        {
            // 重新计算可见轨迹列表，并为每条轨迹维护属性变化订阅。
            foreach (AxisTrajectoryItem trajectoryItem in _subscribedTrajectories)
            {
                trajectoryItem.PropertyChanged -= OnTrajectoryItemPropertyChanged;
            }

            _subscribedTrajectories.Clear();
            _trackedTrajectories.Clear();

            if (_trajectorySource == null)
            {
                return;
            }

            foreach (AxisTrajectoryItem trajectoryItem in _trajectorySource)
            {
                _trackedTrajectories.Add(trajectoryItem);
                trajectoryItem.PropertyChanged += OnTrajectoryItemPropertyChanged;
                _subscribedTrajectories.Add(trajectoryItem);
            }
        }

        private void RefreshAxisFrame()
        {
            // 坐标框架同时服务 Canvas 网格和 LightningChart 坐标轴，保持两层显示一致。
            GridLines = _model.CreateGridLines(_bounds, _appearance);
            XAxisLabels = _model.CreateXLabels(_bounds);
            YAxisLabels = _model.CreateYLabels(_bounds);
            ChartXAxes = _model.CreateChartXAxes(_bounds, _appearance);
            ChartYAxes = _model.CreateChartYAxes(_bounds, _appearance);
            AxisRangeText = $"X {_bounds.XMinimum:F2} ~ {_bounds.XMaximum:F2}    Y {_bounds.YMinimum:F2} ~ {_bounds.YMaximum:F2}";
        }

        private void RefreshPlannedTrajectoryLayer()
        {
            // 只绘制有点且可见的轨迹，状态样式由模型层统一转换。
            List<AxisTrajectoryItem> visibleTrajectories = _trackedTrajectories
                .Where(item => item.IsVisible && item.HasPoints)
                .ToList();

            PlannedTrajectories = visibleTrajectories
                .Select(item => _model.CreateTrajectoryVisual(item, _bounds, _appearance))
                .Where(item => item.Points.Count > 1)
                .ToArray();

            _trajectoryCount = visibleTrajectories.Count;
            _plannedPathLength = visibleTrajectories.Sum(item => item.PathLength);

            int pendingCount = visibleTrajectories.Count(item => item.State == AxisTrajectoryState.待执行);
            int runningCount = visibleTrajectories.Count(item => item.State == AxisTrajectoryState.执行中);
            int completedCount = visibleTrajectories.Count(item => item.State == AxisTrajectoryState.已完成);

            TrajectoryCountText = _trajectoryCount == 0 ? "未加载" : $"{_trajectoryCount} 条";
            PlannedPathLengthText = _trajectoryCount == 0 ? "--" : $"{_plannedPathLength:F2}";
            TrajectorySummaryText = _trajectoryCount == 0
                ? "当前未加载规划轨迹层。"
                : $"待执行 {pendingCount} 条，执行中 {runningCount} 条，已完成 {completedCount} 条。";
        }

        private void RefreshCircleSimulationLayer()
        {
            if (!_hasCircleSimulationPreview)
            {
                SimulationTrajectories = Array.Empty<AxisMonitorPathVisual>();
                return;
            }

            // 圆轨迹预览不改变外部轨迹数据，仅作为临时辅助层显示。
            Point center = new(CircleCenterX, CircleCenterY);
            AxisMonitorPathVisual visual = _model.CreateSimulationCircleVisual(
                center,
                CircleRadius,
                _bounds,
                SimulationCircleSegmentCount,
                _appearance);
            SimulationTrajectories = visual.Points.Count > 1
                ? new[] { visual }
                : Array.Empty<AxisMonitorPathVisual>();
        }

        private void RefreshMotionTraceLayer()
        {
            // 运动痕迹以世界坐标存储，刷新时投影为 Canvas 路径点。
            MotionTracePoints = _model.CreateProjectedPath(_motionTraceWorldPoints, _bounds);
            MotionTraceVisibility = _showTrajectoryLines && MotionTracePoints.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            ExecutedDistanceText = _motionTraceWorldPoints.Count > 1 ? $"{_executedPathLength:F2}" : "--";
            TracePointCountText = _motionTraceWorldPoints.Count > 0
                ? $"{_motionTraceWorldPoints.Count} 个采样点"
                : "暂无轨迹采样";
        }

        private void RefreshMarkerLayer()
        {
            // 标记点超出量程时仍按边界钳制显示，摘要文本会提示超出范围。
            if (IsFinitePoint(_currentPosition))
            {
                Point currentPoint = _model.Project(_currentPosition, _bounds);
                CurrentPointX = currentPoint.X;
                CurrentPointY = currentPoint.Y;
                CurrentMarkerLeft = currentPoint.X - (MarkerSize / 2d);
                CurrentMarkerTop = currentPoint.Y - (MarkerSize / 2d);
                CurrentMarkerVisibility = Visibility.Visible;
            }
            else
            {
                CurrentMarkerVisibility = Visibility.Collapsed;
            }

            if (IsFinitePoint(_targetPosition))
            {
                Point targetPoint = _model.Project(_targetPosition, _bounds);
                TargetPointX = targetPoint.X;
                TargetPointY = targetPoint.Y;
                TargetMarkerLeft = targetPoint.X - (MarkerSize / 2d);
                TargetMarkerTop = targetPoint.Y - (MarkerSize / 2d);
                TargetMarkerVisibility = Visibility.Visible;
            }
            else
            {
                TargetMarkerVisibility = Visibility.Collapsed;
            }
        }

        private void RefreshChartSeries()
        {
            // 图表层按“计划轨迹、圆预览、运动痕迹、目标/当前标记”的顺序叠加。
            FreeformPointLineSeriesCollection chartSeries = new();

            foreach (AxisTrajectoryItem trajectoryItem in _trackedTrajectories.Where(item => item.IsVisible && item.HasPoints))
            {
                FreeformPointLineSeries series = _model.CreateTrajectoryChartSeries(trajectoryItem, _bounds, _appearance);
                if (series.Points != null && series.Points.Length > 0)
                {
                    chartSeries.Add(series);
                }
            }

            if (_hasCircleSimulationPreview)
            {
                FreeformPointLineSeries simulationCircleSeries = _model.CreateSimulationCircleChartSeries(
                    new Point(CircleCenterX, CircleCenterY),
                    CircleRadius,
                    _bounds,
                    SimulationCircleSegmentCount,
                    _appearance);

                if (simulationCircleSeries.Points != null && simulationCircleSeries.Points.Length > 1)
                {
                    chartSeries.Add(simulationCircleSeries);
                }
            }

            FreeformPointLineSeries motionTraceSeries = _model.CreateMotionTraceChartSeries(_motionTraceWorldPoints, _bounds, _appearance);
            if (_showTrajectoryLines &&
                motionTraceSeries.Points != null &&
                motionTraceSeries.Points.Length > 1)
            {
                chartSeries.Add(motionTraceSeries);
            }

            FreeformPointLineSeries? targetMarkerSeries = _model.CreateTargetMarkerChartSeries(_targetPosition, _bounds, _appearance);
            if (targetMarkerSeries != null)
            {
                chartSeries.Add(targetMarkerSeries);
            }

            FreeformPointLineSeries? currentMarkerSeries = _model.CreateCurrentMarkerChartSeries(_currentPosition, _bounds, _appearance);
            if (currentMarkerSeries != null)
            {
                chartSeries.Add(currentMarkerSeries);
            }

            ChartSeries = chartSeries;
        }

        private void UpdateSummary()
        {
            // 摘要区集中展示点位、剩余距离、路径长度和量程状态，避免 XAML 中写复杂判断。
            CurrentPositionText = FormatPoint(_currentPosition);
            TargetPositionText = FormatPoint(_targetPosition);
            CurrentXText = FormatCoordinate(_currentPosition.X);
            CurrentYText = FormatCoordinate(_currentPosition.Y);
            TargetXText = FormatCoordinate(_targetPosition.X);
            TargetYText = FormatCoordinate(_targetPosition.Y);

            bool hasCurrent = IsFinitePoint(_currentPosition);
            bool hasTarget = IsFinitePoint(_targetPosition);
            bool currentInRange = !hasCurrent || _model.IsInRange(_currentPosition, _bounds);
            bool targetInRange = !hasTarget || _model.IsInRange(_targetPosition, _bounds);

            if (hasCurrent && hasTarget)
            {
                double remainingDistance = Distance(_currentPosition, _targetPosition);
                RemainingDistanceText = $"{remainingDistance:F2}";

                if (!currentInRange || !targetInRange)
                {
                    PositionStateText = "超出量程";
                    StatusText = "当前点或目标点超出配置坐标范围，视图中已按边界进行钳制显示。";
                    return;
                }

                if (remainingDistance <= PositionTolerance)
                {
                    PositionStateText = "已到位";
                    StatusText = "当前点与目标点已重合，可以继续下发下一次移动命令。";
                    return;
                }

                PositionStateText = "运动中";
                StatusText = $"当前点正在向目标点移动，剩余距离 {remainingDistance:F2}。";
                return;
            }

            RemainingDistanceText = "--";

            if (!hasCurrent && !hasTarget)
            {
                PositionStateText = "等待反馈";
                StatusText = "尚未接收到 XY 轴当前位置，也还没有下发目标点。";
                return;
            }

            if (!hasCurrent)
            {
                PositionStateText = "待启动";
                StatusText = "目标点已经设置，等待当前位置反馈后即可开始模拟移动。";
                return;
            }

            if (!currentInRange)
            {
                PositionStateText = "超出量程";
                StatusText = "当前点超出配置坐标范围，视图中已按边界进行钳制显示。";
                return;
            }

            PositionStateText = "待命";
            StatusText = "当前位置已就绪，可以直接下发目标点位命令。";
        }

        private void DetachTrajectorySource()
        {
            if (_notifyingTrajectorySource != null)
            {
                _notifyingTrajectorySource.CollectionChanged -= OnTrajectoryCollectionChanged;
                _notifyingTrajectorySource = null;
            }

            foreach (AxisTrajectoryItem trajectoryItem in _subscribedTrajectories)
            {
                trajectoryItem.PropertyChanged -= OnTrajectoryItemPropertyChanged;
            }

            _subscribedTrajectories.Clear();
            _trackedTrajectories.Clear();
            _trajectorySource = null;
        }

        private void ResetExecutionState()
        {
            _currentPosition = new Point(double.NaN, double.NaN);
            _motionTraceWorldPoints.Clear();
            _executedPathLength = 0d;
        }

        private void PreviewCircle()
        {
            Point center = new(CircleCenterX, CircleCenterY);
            if (!IsFinitePoint(center))
            {
                _hasCircleSimulationPreview = false;
                SimulationTrajectories = Array.Empty<AxisMonitorPathVisual>();
                RefreshChartSeries();
                CircleSimulationStatusText = "圆心坐标无效，请输入有效数值。";
                return;
            }

            if (!double.IsFinite(CircleRadius) || CircleRadius <= 0d)
            {
                _hasCircleSimulationPreview = false;
                SimulationTrajectories = Array.Empty<AxisMonitorPathVisual>();
                RefreshChartSeries();
                CircleSimulationStatusText = "半径必须大于 0。";
                return;
            }

            _hasCircleSimulationPreview = true;
            RefreshCircleSimulationLayer();
            RefreshChartSeries();
            CircleSimulationStatusText = $"已预览圆心 ({CircleCenterX:F2}, {CircleCenterY:F2})、半径 {CircleRadius:F2} 的圆轨迹。";
        }

        private void ClearCirclePreview()
        {
            _hasCircleSimulationPreview = false;
            SimulationTrajectories = Array.Empty<AxisMonitorPathVisual>();
            RefreshChartSeries();
            CircleSimulationStatusText = "已清除圆轨迹预览。";
        }

        private static bool IsFinitePoint(Point point)
        {
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }

        private static string FormatPoint(Point point)
        {
            return IsFinitePoint(point)
                ? $"X = {point.X:F2}    Y = {point.Y:F2}"
                : "--";
        }

        private static string FormatCoordinate(double coordinate)
        {
            return double.IsFinite(coordinate) ? coordinate.ToString("F2") : "--";
        }

        private static double Distance(Point startPoint, Point endPoint)
        {
            double deltaX = endPoint.X - startPoint.X;
            double deltaY = endPoint.Y - startPoint.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}

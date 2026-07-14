using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using Custom.WaferFlatnessMeasure;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    /// <summary>
    /// XY 轨迹监控窗口的桥接层：接收执行模型发布的轨迹事件，并轮询控制卡实际位置。
    /// </summary>
    public sealed class WaferTrajectoryMonitorDemoViewModel : DialogViewModelBase, IViewModuleParam, INavigationAware
    {
        private static readonly AxisTrajectoryState PendingTrajectoryState = (AxisTrajectoryState)0;
        private static readonly AxisTrajectoryState RunningTrajectoryState = (AxisTrajectoryState)1;
        private static readonly AxisTrajectoryState CompletedTrajectoryState = (AxisTrajectoryState)2;
        private const double TrajectoryViewportPaddingRatio = 0.18d;
        private const double MinimumTrajectoryViewportSpan = 10d;

        private CancellationTokenSource? _actualPositionMonitorCts;
        private SubscriptionToken? _trajectoryTrackingToken;
        private string _activeExternalTrackingRunId = string.Empty;
        private int _navigationSerial = -1;
        private bool _hasInitialized;
        private bool _hasExternalTrackingData;
        private Point _currentAxisPosition = new(double.NaN, double.NaN);
        private Point _previewTargetPosition = new(double.NaN, double.NaN);
        private string _monitorStatus = "等待外部轨迹数据或实际位置反馈。";
        private bool _isActualPositionMonitoring;
        private int _actualPositionPollIntervalMs = 100;
        private double _xMinimum = 0d;
        private double _xMaximum = 800d;
        private double _yMinimum = 0d;
        private double _yMaximum = 800d;
        private DateTime _lastActualPositionStatusTime = DateTime.MinValue;

        public WaferTrajectoryMonitorDemoViewModel()
        {
            Title = "XY 轴组轨迹监控";
            Icon = "\ue81c";

            PreviewTrajectories = new ObservableCollection<AxisTrajectoryItem>();
            PreviewTrajectories.CollectionChanged += OnPreviewTrajectoriesCollectionChanged;

            LoadCommand = new DelegateCommand(OnLoaded);
            StartActualPositionMonitorCommand = new DelegateCommand(async () => await StartActualPositionMonitorAsync());
            RefreshActualPositionCommand = new DelegateCommand(RefreshActualAxisPosition);
            StopMotionCommand = new DelegateCommand(StopMotion);

            _trajectoryTrackingToken = PrismProvider.EventAggregator
                .GetEvent<OutputResultEvent>()
                .Subscribe(OnTrajectoryTrackingEvent, ThreadOption.UIThread);
        }

        public ObservableCollection<AxisTrajectoryItem> PreviewTrajectories { get; }

        public DelegateCommand LoadCommand { get; }

        public DelegateCommand StartActualPositionMonitorCommand { get; }

        public DelegateCommand RefreshActualPositionCommand { get; }

        public DelegateCommand StopMotionCommand { get; }

        public Point CurrentAxisPosition
        {
            get => _currentAxisPosition;
            set
            {
                if (SetProperty(ref _currentAxisPosition, value))
                {
                    RaisePropertyChanged(nameof(CurrentAxisSummary));
                    RaisePropertyChanged(nameof(ActualTrajectoryAccuracySummary));
                }
            }
        }

        public Point PreviewTargetPosition
        {
            get => _previewTargetPosition;
            set
            {
                Point normalizedValue = IsFinitePoint(value)
                    ? value
                    : new Point(double.NaN, double.NaN);

                if (SetProperty(ref _previewTargetPosition, normalizedValue))
                {
                    RaisePropertyChanged(nameof(TargetAxisSummary));
                    RaisePropertyChanged(nameof(ActualTrajectoryAccuracySummary));
                }
            }
        }

        public double XMinimum
        {
            get => _xMinimum;
            set
            {
                if (SetProperty(ref _xMinimum, value))
                {
                    EnsureAxisRangeConsistency(nameof(XMinimum));
                }
            }
        }

        public double XMaximum
        {
            get => _xMaximum;
            set
            {
                if (SetProperty(ref _xMaximum, value))
                {
                    EnsureAxisRangeConsistency(nameof(XMaximum));
                }
            }
        }

        public double YMinimum
        {
            get => _yMinimum;
            set
            {
                if (SetProperty(ref _yMinimum, value))
                {
                    EnsureAxisRangeConsistency(nameof(YMinimum));
                }
            }
        }

        public double YMaximum
        {
            get => _yMaximum;
            set
            {
                if (SetProperty(ref _yMaximum, value))
                {
                    EnsureAxisRangeConsistency(nameof(YMaximum));
                }
            }
        }

        public string MonitorStatus
        {
            get => _monitorStatus;
            set => SetProperty(ref _monitorStatus, value ?? string.Empty);
        }

        public bool IsActualPositionMonitoring
        {
            get => _isActualPositionMonitoring;
            private set
            {
                if (SetProperty(ref _isActualPositionMonitoring, value))
                {
                    RaisePropertyChanged(nameof(ActualPositionMonitorSummary));
                }
            }
        }

        public int ActualPositionPollIntervalMs
        {
            get => _actualPositionPollIntervalMs;
            set
            {
                if (SetProperty(ref _actualPositionPollIntervalMs, Math.Max(value, 10)))
                {
                    RaisePropertyChanged(nameof(ActualPositionMonitorSummary));
                }
            }
        }

        public string CurrentAxisSummary =>
            IsFinitePoint(CurrentAxisPosition)
                ? $"当前位置：X = {CurrentAxisPosition.X:F2}    Y = {CurrentAxisPosition.Y:F2}"
                : "当前位置：尚未读取实际位置。";

        public string TargetAxisSummary =>
            IsFinitePoint(PreviewTargetPosition)
                ? $"监控目标：X = {PreviewTargetPosition.X:F2}    Y = {PreviewTargetPosition.Y:F2}"
                : "监控目标：等待外部轨迹目标。";

        public string ActualPositionMonitorSummary =>
            IsActualPositionMonitoring
                ? $"实际位置监控：运行中，读取间隔 {ActualPositionPollIntervalMs} ms"
                : $"实际位置监控：未启动，读取间隔 {ActualPositionPollIntervalMs} ms";

        public string ActualTrajectoryAccuracySummary
        {
            get
            {
                if (!IsFinitePoint(CurrentAxisPosition))
                {
                    return "实际轨迹偏差：尚未读取实际位置。";
                }

                if (PreviewTrajectories.Count == 0)
                {
                    return "实际轨迹偏差：尚未加载规划轨迹。";
                }

                double nearestDistance = CalculateNearestTrajectoryDistance(CurrentAxisPosition);
                string targetDistanceText = IsFinitePoint(PreviewTargetPosition)
                    ? $"，距当前目标 {Distance(CurrentAxisPosition, PreviewTargetPosition):F2}"
                    : string.Empty;

                return double.IsFinite(nearestDistance)
                    ? $"实际轨迹偏差：距规划轨迹最近 {nearestDistance:F2}{targetDistanceText}。"
                    : "实际轨迹偏差：规划轨迹中没有有效点位。";
            }
        }

        public string RangeSummary =>
            $"工作范围：X {XMinimum:F2} ~ {XMaximum:F2}    Y {YMinimum:F2} ~ {YMaximum:F2}";

        public string TrajectoryGeometrySummary
        {
            get
            {
                if (PreviewTrajectories.Count == 0)
                {
                    return "规划轨迹：尚未加载。";
                }

                int pointCount = PreviewTrajectories.Count(item => item.PointCount <= 1);
                int lineCount = PreviewTrajectories.Count - pointCount;
                double totalLength = PreviewTrajectories.Sum(item => item.PathLength);
                return $"规划轨迹：共 {PreviewTrajectories.Count} 条，线段 {lineCount} 条，点位 {pointCount} 条，总长度 {totalLength:F2}。";
            }
        }

        public string TrajectoryProgressSummary
        {
            get
            {
                if (PreviewTrajectories.Count == 0)
                {
                    return "执行进度：暂无轨迹。";
                }

                int pendingCount = PreviewTrajectories.Count(item => item.State == PendingTrajectoryState);
                int runningCount = PreviewTrajectories.Count(item => item.State == RunningTrajectoryState);
                int completedCount = PreviewTrajectories.Count(item => item.State == CompletedTrajectoryState);
                return $"执行进度：待执行 {pendingCount} 条，执行中 {runningCount} 条，已完成 {completedCount} 条。";
            }
        }

        public override void OnDialogClosed()
        {
            if (_trajectoryTrackingToken != null)
            {
                PrismProvider.EventAggregator
                    .GetEvent<OutputResultEvent>()
                    .Unsubscribe(_trajectoryTrackingToken);
                _trajectoryTrackingToken = null;
            }

            _actualPositionMonitorCts?.Cancel();
            _actualPositionMonitorCts?.Dispose();
            _actualPositionMonitorCts = null;

            PreviewTrajectories.CollectionChanged -= OnPreviewTrajectoriesCollectionChanged;
            foreach (AxisTrajectoryItem trajectoryItem in PreviewTrajectories)
            {
                trajectoryItem.PropertyChanged -= OnPreviewTrajectoryItemPropertyChanged;
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.TryGetValue<int>("Serial", out int serial))
            {
                _navigationSerial = serial;
                Serial = serial;
            }

            TryRestoreLatestTrajectorySnapshot();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void OnLoaded()
        {
            if (_hasInitialized)
            {
                return;
            }

            _hasInitialized = true;
            if (_hasExternalTrackingData)
            {
                return;
            }

            if (TryRestoreLatestTrajectorySnapshot())
            {
                return;
            }

            LoadDefaults();
        }

        private void LoadDefaults()
        {
            XMinimum = 0d;
            XMaximum = 800d;
            YMinimum = 0d;
            YMaximum = 800d;
            ActualPositionPollIntervalMs = 100;

            CurrentAxisPosition = new Point(double.NaN, double.NaN);
            PreviewTargetPosition = new Point(double.NaN, double.NaN);
            PreviewTrajectories.Clear();

            MonitorStatus = "轨迹监控已加载，等待外部轨迹事件。";
        }

        private async Task StartActualPositionMonitorAsync()
        {
            if (IsActualPositionMonitoring)
            {
                MonitorStatus = "实际位置监控已经在运行。";
                return;
            }

            PreviewTargetPosition = GetInitialTrajectoryTarget();
            _actualPositionMonitorCts?.Dispose();
            _actualPositionMonitorCts = new CancellationTokenSource();
            IsActualPositionMonitoring = true;
            _lastActualPositionStatusTime = DateTime.MinValue;
            MonitorStatus = $"开始按 {ActualPositionPollIntervalMs} ms 间隔读取控制卡 XY 实际位置。";

            try
            {
                while (true)
                {
                    CancellationToken token = _actualPositionMonitorCts.Token;
                    token.ThrowIfCancellationRequested();

                    var result = await Task.Run(ReadActualAxisPosition, token);
                    token.ThrowIfCancellationRequested();

                    if (result.Success)
                    {
                        CurrentAxisPosition = result.Position;
                        UpdateActualPositionStatus(result.Position);
                    }
                    else
                    {
                        MonitorStatus = $"读取实际位置失败：{result.Message}";
                    }

                    await Task.Delay(ActualPositionPollIntervalMs, token);
                }
            }
            catch (OperationCanceledException)
            {
                MonitorStatus = "已停止实际位置监控。";
            }
            catch (Exception ex)
            {
                MonitorStatus = $"实际位置监控异常：{ex.Message}";
            }
            finally
            {
                IsActualPositionMonitoring = false;
                _actualPositionMonitorCts?.Dispose();
                _actualPositionMonitorCts = null;
            }
        }

        private void RefreshActualAxisPosition()
        {
            var result = ReadActualAxisPosition();
            if (!result.Success)
            {
                MonitorStatus = $"读取实际位置失败：{result.Message}";
                return;
            }

            CurrentAxisPosition = result.Position;
            if (!IsFinitePoint(PreviewTargetPosition))
            {
                PreviewTargetPosition = GetInitialTrajectoryTarget();
            }

            MonitorStatus = $"已读取实际位置：X = {result.Position.X:F2}，Y = {result.Position.Y:F2}。";
        }

        private void StopMotion()
        {
            if (IsActualPositionMonitoring && _actualPositionMonitorCts != null)
            {
                _actualPositionMonitorCts.Cancel();
                MonitorStatus = "正在停止实际位置监控...";
                return;
            }

            MonitorStatus = "当前没有正在运行的实际位置监控。";
        }

        private void OnTrajectoryTrackingEvent((string, object) message)
        {
            switch (message.Item1)
            {
                case WaferTrajectoryTrackingEventNames.Start:
                    if (message.Item2 is WaferTrajectoryTrackingStartPayload startPayload)
                    {
                        ApplyExternalTrajectoryTrackingStart(startPayload);
                    }
                    break;

                case WaferTrajectoryTrackingEventNames.Target:
                    if (message.Item2 is WaferTrajectoryTrackingTargetPayload targetPayload)
                    {
                        ApplyExternalTrajectoryTrackingTarget(targetPayload);
                    }
                    break;

                case WaferTrajectoryTrackingEventNames.Progress:
                    if (message.Item2 is WaferTrajectoryTrackingProgressPayload progressPayload)
                    {
                        ApplyExternalTrajectoryTrackingProgress(progressPayload);
                    }
                    break;

                case WaferTrajectoryTrackingEventNames.Stop:
                    if (message.Item2 is WaferTrajectoryTrackingStopPayload stopPayload)
                    {
                        ApplyExternalTrajectoryTrackingStop(stopPayload);
                    }
                    break;
            }
        }

        private bool TryRestoreLatestTrajectorySnapshot()
        {
            int sourceSerial = _navigationSerial >= 0 ? _navigationSerial : Serial;
            if (!WaferTrajectoryTrackingPublisher.TryGetLatestSnapshot(sourceSerial, out WaferTrajectoryTrackingSnapshot? snapshot) ||
                snapshot == null)
            {
                return false;
            }

            ApplyTrajectoryTrackingSnapshot(snapshot);
            return true;
        }

        private void ApplyTrajectoryTrackingSnapshot(WaferTrajectoryTrackingSnapshot snapshot)
        {
            WaferTrajectoryTrackingStartPayload startPayload =
                WaferTrajectoryTrackingSnapshot.CloneStartPayload(snapshot.StartPayload);
            startPayload.StartActualPositionMonitor = startPayload.StartActualPositionMonitor && snapshot.IsActive;

            ApplyExternalTrajectoryTrackingStart(startPayload);

            if (snapshot.CurrentTarget.HasValue)
            {
                PreviewTargetPosition = ToPoint(snapshot.CurrentTarget.Value);
            }

            ApplyTrajectoryStates(snapshot.RunningIndex, snapshot.CompletedIndex, snapshot.IsFinished);

            if (snapshot.IsStopped)
            {
                _activeExternalTrackingRunId = string.Empty;
                if (IsActualPositionMonitoring && _actualPositionMonitorCts != null)
                {
                    _actualPositionMonitorCts.Cancel();
                }
            }

            MonitorStatus = string.IsNullOrWhiteSpace(snapshot.StatusMessage)
                ? "Restored trajectory tracking state."
                : snapshot.StatusMessage;
        }

        private void ApplyExternalTrajectoryTrackingStart(WaferTrajectoryTrackingStartPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            _hasExternalTrackingData = true;
            _activeExternalTrackingRunId = payload.RunId ?? string.Empty;

            ActualPositionPollIntervalMs = payload.ActualPositionPollIntervalMs;
            PreviewTrajectories.Clear();

            foreach (WaferTrajectoryTrackingItem trackingItem in payload.Trajectories ?? Array.Empty<WaferTrajectoryTrackingItem>())
            {
                PreviewTrajectories.Add(new AxisTrajectoryItem
                {
                    Id = string.IsNullOrWhiteSpace(trackingItem.Id) ? Guid.NewGuid().ToString("N") : trackingItem.Id,
                    DisplayName = string.IsNullOrWhiteSpace(trackingItem.DisplayName) ? "Trajectory" : trackingItem.DisplayName,
                    Points = CreateMonitorPoints(trackingItem.Points),
                    State = PendingTrajectoryState,
                });
            }

            ApplyAxisRangeFromTrackingPayload(payload);
            CurrentAxisPosition = new Point(double.NaN, double.NaN);
            PreviewTargetPosition = payload.InitialTarget.HasValue
                ? ToPoint(payload.InitialTarget.Value)
                : GetInitialTrajectoryTarget();
            RaiseTrajectorySummaryChanged();

            string modeText = payload.IsPointTrajectory ? "点位" : "线段";
            MonitorStatus = $"已加载外部{modeText}轨迹：{PreviewTrajectories.Count} 条。";

            if (payload.StartActualPositionMonitor && !IsActualPositionMonitoring)
            {
                _ = StartActualPositionMonitorAsync();
            }
        }

        private void ApplyExternalTrajectoryTrackingTarget(WaferTrajectoryTrackingTargetPayload payload)
        {
            if (!IsActiveExternalTrackingRun(payload.RunId))
            {
                return;
            }

            PreviewTargetPosition = ToPoint(payload.Target);
            ApplyTrajectoryStates(payload.TrajectoryIndex, payload.TrajectoryIndex - 1, false);
        }

        private void ApplyExternalTrajectoryTrackingProgress(WaferTrajectoryTrackingProgressPayload payload)
        {
            if (!IsActiveExternalTrackingRun(payload.RunId))
            {
                return;
            }

            ApplyTrajectoryStates(payload.RunningIndex, payload.CompletedIndex, payload.IsFinished);
        }

        private void ApplyExternalTrajectoryTrackingStop(WaferTrajectoryTrackingStopPayload payload)
        {
            if (!IsActiveExternalTrackingRun(payload.RunId))
            {
                return;
            }

            if (payload.IsCompleted)
            {
                ApplyTrajectoryStates(-1, PreviewTrajectories.Count - 1, true);
            }

            var finalPosition = ReadActualAxisPosition();
            if (finalPosition.Success)
            {
                CurrentAxisPosition = finalPosition.Position;
            }

            if (IsActualPositionMonitoring && _actualPositionMonitorCts != null)
            {
                _actualPositionMonitorCts.Cancel();
            }

            MonitorStatus = string.IsNullOrWhiteSpace(payload.Message)
                ? (payload.IsCompleted ? "外部轨迹跟踪已完成。" : "外部轨迹跟踪已停止。")
                : payload.Message;
            _activeExternalTrackingRunId = string.Empty;
        }

        private void ApplyTrajectoryStates(int runningIndex, int completedIndex, bool isFinished)
        {
            for (int index = 0; index < PreviewTrajectories.Count; index++)
            {
                if (isFinished || index <= completedIndex)
                {
                    PreviewTrajectories[index].State = CompletedTrajectoryState;
                }
                else if (index == runningIndex)
                {
                    PreviewTrajectories[index].State = RunningTrajectoryState;
                }
                else
                {
                    PreviewTrajectories[index].State = PendingTrajectoryState;
                }
            }

            RaiseTrajectorySummaryChanged();
        }

        private void ApplyAxisRangeFromTrackingPayload(WaferTrajectoryTrackingStartPayload payload)
        {
            var allPoints = (payload.Trajectories ?? Array.Empty<WaferTrajectoryTrackingItem>())
                .SelectMany(item => item.Points ?? Array.Empty<WaferTrajectoryTrackingPoint>())
                .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
                .ToArray();

            if (allPoints.Length == 0)
            {
                return;
            }

            double minX = allPoints.Min(point => point.X);
            double maxX = allPoints.Max(point => point.X);
            double minY = allPoints.Min(point => point.Y);
            double maxY = allPoints.Max(point => point.Y);
            double centerX = (minX + maxX) / 2d;
            double centerY = (minY + maxY) / 2d;
            double spanX = Math.Max(maxX - minX, MinimumTrajectoryViewportSpan);
            double spanY = Math.Max(maxY - minY, MinimumTrajectoryViewportSpan);
            double viewportSpan = Math.Max(spanX, spanY) * (1d + TrajectoryViewportPaddingRatio);

            XMinimum = centerX - (viewportSpan / 2d);
            XMaximum = centerX + (viewportSpan / 2d);
            YMinimum = centerY - (viewportSpan / 2d);
            YMaximum = centerY + (viewportSpan / 2d);
        }

        private bool IsActiveExternalTrackingRun(string runId)
        {
            return !string.IsNullOrWhiteSpace(runId) &&
                   string.Equals(_activeExternalTrackingRunId, runId, StringComparison.Ordinal);
        }

        private static IReadOnlyList<Point> CreateMonitorPoints(IEnumerable<WaferTrajectoryTrackingPoint>? points)
        {
            return points?
                .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Select(ToPoint)
                .ToArray() ?? Array.Empty<Point>();
        }

        private static Point ToPoint(WaferTrajectoryTrackingPoint point)
        {
            return new Point(point.X, point.Y);
        }

        private Point GetInitialTrajectoryTarget()
        {
            AxisTrajectoryItem? firstTrajectory = PreviewTrajectories.FirstOrDefault();
            return firstTrajectory == null
                ? new Point(double.NaN, double.NaN)
                : firstTrajectory.EndPoint;
        }

        private void EnsureAxisRangeConsistency(string changedPropertyName)
        {
            bool rangeAdjusted = false;

            if (_xMaximum <= _xMinimum)
            {
                rangeAdjusted = true;
                if (changedPropertyName == nameof(XMinimum))
                {
                    _xMaximum = _xMinimum + 1d;
                    RaisePropertyChanged(nameof(XMaximum));
                }
                else
                {
                    _xMinimum = _xMaximum - 1d;
                    RaisePropertyChanged(nameof(XMinimum));
                }
            }

            if (_yMaximum <= _yMinimum)
            {
                rangeAdjusted = true;
                if (changedPropertyName == nameof(YMinimum))
                {
                    _yMaximum = _yMinimum + 1d;
                    RaisePropertyChanged(nameof(YMaximum));
                }
                else
                {
                    _yMinimum = _yMaximum - 1d;
                    RaisePropertyChanged(nameof(YMinimum));
                }
            }

            if (rangeAdjusted)
            {
                MonitorStatus = "坐标范围已自动调整，确保最大值始终大于最小值。";
            }

            RaisePropertyChanged(nameof(RangeSummary));
        }

        private void OnPreviewTrajectoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (AxisTrajectoryItem trajectoryItem in e.OldItems.OfType<AxisTrajectoryItem>())
                {
                    trajectoryItem.PropertyChanged -= OnPreviewTrajectoryItemPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (AxisTrajectoryItem trajectoryItem in e.NewItems.OfType<AxisTrajectoryItem>())
                {
                    trajectoryItem.PropertyChanged += OnPreviewTrajectoryItemPropertyChanged;
                }
            }

            RaiseTrajectorySummaryChanged();
        }

        private void OnPreviewTrajectoryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AxisTrajectoryItem.State) ||
                e.PropertyName == nameof(AxisTrajectoryItem.Points) ||
                string.IsNullOrEmpty(e.PropertyName))
            {
                RaiseTrajectorySummaryChanged();
            }
        }

        private void RaiseTrajectorySummaryChanged()
        {
            RaisePropertyChanged(nameof(TrajectoryGeometrySummary));
            RaisePropertyChanged(nameof(TrajectoryProgressSummary));
            RaisePropertyChanged(nameof(ActualTrajectoryAccuracySummary));
        }

        private void UpdateActualPositionStatus(Point position)
        {
            DateTime now = DateTime.Now;
            if ((now - _lastActualPositionStatusTime).TotalMilliseconds < 1000)
            {
                return;
            }

            _lastActualPositionStatusTime = now;
            MonitorStatus = $"实际位置已更新：X = {position.X:F2}，Y = {position.Y:F2}。";
        }

        private static (bool Success, Point Position, string Message) ReadActualAxisPosition()
        {
            try
            {
                if (PrismProvider.HardwareModuleManager?.Modules == null)
                {
                    return (false, new Point(double.NaN, double.NaN), "硬件模块管理器未初始化。");
                }

                if (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] is not ControlCardConfigModel controlCardConfig)
                {
                    return (false, new Point(double.NaN, double.NaN), "未找到控制卡配置模块。");
                }

                ControlCardBase? controlCard = controlCardConfig.CardModels?.FirstOrDefault();
                if (controlCard == null)
                {
                    return (false, new Point(double.NaN, double.NaN), "控制卡实例为空。");
                }

                if (!controlCard.GetAllPosInfos())
                {
                    return (false, new Point(double.NaN, double.NaN), "控制卡 GetAllPosInfos 返回失败。");
                }

                if (!TryGetAxisPosition(controlCard, En_AxisNum.X, out double xPosition) ||
                    !TryGetAxisPosition(controlCard, En_AxisNum.Y, out double yPosition))
                {
                    return (false, new Point(double.NaN, double.NaN), "未能从控制卡配置中读取 X/Y 轴当前位置。");
                }

                return (true, new Point(xPosition, yPosition), string.Empty);
            }
            catch (Exception ex)
            {
                return (false, new Point(double.NaN, double.NaN), ex.Message);
            }
        }

        private static bool TryGetAxisPosition(ControlCardBase controlCard, En_AxisNum axisNum, out double position)
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

        private double CalculateNearestTrajectoryDistance(Point point)
        {
            double nearestDistance = double.PositiveInfinity;

            foreach (AxisTrajectoryItem trajectoryItem in PreviewTrajectories)
            {
                double currentDistance = DistanceToTrajectory(point, trajectoryItem.Points);
                if (double.IsFinite(currentDistance) && currentDistance < nearestDistance)
                {
                    nearestDistance = currentDistance;
                }
            }

            return nearestDistance;
        }

        private static double DistanceToTrajectory(Point point, IReadOnlyList<Point>? points)
        {
            if (points == null || points.Count == 0)
            {
                return double.NaN;
            }

            Point[] finitePoints = points.Where(IsFinitePoint).ToArray();
            if (finitePoints.Length == 0)
            {
                return double.NaN;
            }

            if (finitePoints.Length == 1)
            {
                return Distance(point, finitePoints[0]);
            }

            double nearestDistance = double.PositiveInfinity;
            for (int index = 1; index < finitePoints.Length; index++)
            {
                double segmentDistance = DistanceToSegment(point, finitePoints[index - 1], finitePoints[index]);
                if (segmentDistance < nearestDistance)
                {
                    nearestDistance = segmentDistance;
                }
            }

            return nearestDistance;
        }

        private static double DistanceToSegment(Point point, Point segmentStart, Point segmentEnd)
        {
            double deltaX = segmentEnd.X - segmentStart.X;
            double deltaY = segmentEnd.Y - segmentStart.Y;
            double lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

            if (lengthSquared <= 1E-12d)
            {
                return Distance(point, segmentStart);
            }

            double projectedRatio =
                (((point.X - segmentStart.X) * deltaX) + ((point.Y - segmentStart.Y) * deltaY)) / lengthSquared;
            projectedRatio = Math.Clamp(projectedRatio, 0d, 1d);

            Point projectedPoint = new(
                segmentStart.X + (deltaX * projectedRatio),
                segmentStart.Y + (deltaY * projectedRatio));

            return Distance(point, projectedPoint);
        }

        private static bool IsFinitePoint(Point point)
        {
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }

        private static double Distance(Point left, Point right)
        {
            double deltaX = right.X - left.X;
            double deltaY = right.Y - left.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}

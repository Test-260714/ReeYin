using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Prism.Commands;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Custom.DefectOverview.ViewModels
{
    public class BandMapViewModel : BindableBase
    {
        private const int LiveTimeoutMs = 1200;
        private const int StaleTimeoutMs = 3500;
        private const int WatchdogIntervalMs = 1000;
        private const int SnapshotApplySlowLogMs = 80;
        private const int SnapshotCoalescedLogThreshold = 3;
        private const int BrjAutoSyncIntervalSeconds = 5;
        private const int BrjSyncTimeoutMs = 3000;
        private const string LineLineDistanceGlobalName = "Distance[线线距离]";
        private const string LineLineDistanceParamName = "Distance";
        private const string LineLineDistanceDescription = "线线距离";
        private const string DistanceLlResourcePathMarker = "ALGO.DistanceLL.DistanceLLModel.Distance";
        private static readonly int SnapshotApplyThrottleMs = DefectOverviewRuntimeOptions.UiRefreshIntervalMs;

        private readonly IBandMapStateService _stateService;
        private readonly DispatcherTimer _watchdogTimer;
        private readonly DispatcherTimer _viewportCommitTimer;
        private readonly DispatcherTimer _brjAutoSyncTimer;
        private readonly object _snapshotSync = new();

        private bool _isLoaded;
        private bool _isDetectionRunning;
        private bool _isApplyingSnapshot;
        private BandMapStateSnapshot _pendingSnapshot;
        private bool _snapshotApplyQueued;
        private int _coalescedSnapshotCount;
        private DateTime _lastSnapshotApplyUtc = DateTime.MinValue;
        private double? _pendingViewportStartMeters;
        private string _streamState = "Idle";
        private string _streamStatusText = "等待首帧";
        private string _lastFrameIdText = "-";
        private string _path1Header = "左路";
        private string _path2Header = "右路";
        private string _path1Result = "-";
        private string _path2Result = "-";
        private int _path1DefectCount;
        private int _path2DefectCount;
        private bool _isSlittingEnabled;
        private double _knifeSpacingMillimeters = 200;
        private double _firstCutOffsetMillimeters;
        private double _stripWidthMillimeters = 1000;
        private int _slitCount = 4;
        private string _slittingSummaryText = "切刀分切未启用";
        private bool _showPathStatusBadges;
        private double _cumulativeMeters;
        private string _batchNumberText = BuildFallbackBatchNumberText();
        private int _recentNgFrameCount;
        private int _recentNgWindowSize = 20;
        private int _totalFrames;
        private int _okFrames;
        private int _ngFrames;
        private double _currentSpeedMetersPerMinute;
        private string _currentStatusCode = "Idle";
        private string _currentStatusText = "未运行";
        private string _reportSuggestedFileName = "DefectOverview_Report.csv";
        private IReadOnlyList<BandMapReportRow> _reportRows = Array.Empty<BandMapReportRow>();
        private double _frameSpanMillimeters = 120;
        private double _windowMeters = 12;
        private double _viewportStartMeters;
        private double _viewportMaxStartMeters;
        private DateTime _lastFrameUtc = DateTime.MinValue;
        private string _selectedMapPointPopupDefectKey;
        private bool _isSelectedMapPointPopupOpen;
        private string _selectedMapPointPopupTitle = string.Empty;
        private string _selectedMapPointPopupDetail = string.Empty;
        private double _selectedMapPointPopupOffsetX;
        private double _selectedMapPointPopupOffsetY;
        private IReadOnlyList<string> _selectedMapPointPopupGroupDefectKeys = Array.Empty<string>();
        private bool _isBrjAutoSyncEnabled = true;
        private bool _isBrjDirty;
        private bool _isBrjSyncing;
        private long _brjSnapshotVersion;
        private long _lastBrjSyncedVersion;
        private long _lastRollCompletedSyncedVersion;
        private string _lastBrjSnapshotFingerprint = string.Empty;
        private string _brjSyncStatusText = "BRJ等待数据";
        private DateTime? _lastBrjSyncTime;
        private string _rangeSummary = "当前累计 0.00 m | 显示窗口 12.0 m";
        private BandMapPointDisplayMode _pointDisplayMode = BandMapPointDisplayMode.Precise;
        private double? _realtimeWidthMillimeters;
        private double? _lineLineDistanceMillimeters;

        public BandMapViewModel(IBandMapStateService stateService)
        {
            _stateService = stateService;
            _watchdogTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(WatchdogIntervalMs)
            };
            _watchdogTimer.Tick += OnWatchdogTick;
            _viewportCommitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(85)
            };
            _viewportCommitTimer.Tick += OnViewportCommitTick;
            _brjAutoSyncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(BrjAutoSyncIntervalSeconds)
            };
            _brjAutoSyncTimer.Tick += OnBrjAutoSyncTick;
            SelectPointCommand = new DelegateCommand<BandMapPointItem>(OnSelectPoint);
            SelectRecentDefectCommand = new DelegateCommand<BandMapRecentDefectItem>(OnSelectRecentDefect);
            ToggleLegendCommand = new DelegateCommand<BandMapLegendItem>(OnToggleLegend);
            SyncBrjReportCommand = new DelegateCommand(async () => await SyncBrjReportAsync(manual: true, force: true));
            ApplySnapshot(_stateService.GetSnapshot());
        }

        public ObservableCollection<BandMapLegendItem> LegendItems { get; } = new BulkObservableCollection<BandMapLegendItem>();
        public ObservableCollection<BandMapGuideLineItem> GuideLines { get; } = new BulkObservableCollection<BandMapGuideLineItem>();
        public ObservableCollection<BandMapPointItem> DefectPoints { get; } = new BulkObservableCollection<BandMapPointItem>();
        public ObservableCollection<BandMapAxisTickItem> XAxisTicks { get; } = new BulkObservableCollection<BandMapAxisTickItem>();
        public ObservableCollection<BandMapAxisTickItem> YAxisTicks { get; } = new BulkObservableCollection<BandMapAxisTickItem>();
        public ObservableCollection<BandMapRecentDefectItem> RecentDefects { get; } = new BulkObservableCollection<BandMapRecentDefectItem>();

        public string StreamState
        {
            get => _streamState;
            set => SetProperty(ref _streamState, value);
        }

        public string StreamStatusText
        {
            get => _streamStatusText;
            set => SetProperty(ref _streamStatusText, value);
        }

        public string LastFrameIdText
        {
            get => _lastFrameIdText;
            set => SetProperty(ref _lastFrameIdText, value);
        }

        public string Path1Header
        {
            get => _path1Header;
            set => SetProperty(ref _path1Header, value);
        }

        public string Path2Header
        {
            get => _path2Header;
            set => SetProperty(ref _path2Header, value);
        }

        public string Path1Result
        {
            get => _path1Result;
            set => SetProperty(ref _path1Result, value);
        }

        public string Path2Result
        {
            get => _path2Result;
            set => SetProperty(ref _path2Result, value);
        }

        public int Path1DefectCount
        {
            get => _path1DefectCount;
            set => SetProperty(ref _path1DefectCount, value);
        }

        public int Path2DefectCount
        {
            get => _path2DefectCount;
            set => SetProperty(ref _path2DefectCount, value);
        }

        public bool ShowPathStatusBadges
        {
            get => _showPathStatusBadges;
            set => SetProperty(ref _showPathStatusBadges, value);
        }

        public bool IsSlittingEnabled
        {
            get => _isSlittingEnabled;
            set
            {
                if (!SetProperty(ref _isSlittingEnabled, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSlittingSettings(isEnabled: value);
            }
        }

        public double KnifeSpacingMillimeters
        {
            get => _knifeSpacingMillimeters;
            set
            {
                if (!SetProperty(ref _knifeSpacingMillimeters, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSlittingSettings(knifeSpacingMillimeters: value);
            }
        }

        public double FirstCutOffsetMillimeters
        {
            get => _firstCutOffsetMillimeters;
            set
            {
                if (!SetProperty(ref _firstCutOffsetMillimeters, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSlittingSettings(firstCutOffsetMillimeters: value);
            }
        }

        public double StripWidthMillimeters
        {
            get => _stripWidthMillimeters;
            set
            {
                if (!SetProperty(ref _stripWidthMillimeters, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSlittingSettings(stripWidthMillimeters: value);
            }
        }

        public int SlitCount
        {
            get => _slitCount;
            set
            {
                if (!SetProperty(ref _slitCount, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSlittingSettings(slitCount: value);
            }
        }

        public string SlittingSummaryText
        {
            get => _slittingSummaryText;
            set => SetProperty(ref _slittingSummaryText, value);
        }

        public double CumulativeMeters
        {
            get => _cumulativeMeters;
            set => SetProperty(ref _cumulativeMeters, value);
        }

        public string BatchNumberText
        {
            get => _batchNumberText;
            set => SetProperty(ref _batchNumberText, value);
        }

        public int RecentNgFrameCount
        {
            get => _recentNgFrameCount;
            set
            {
                if (SetProperty(ref _recentNgFrameCount, value))
                    RaisePropertyChanged(nameof(RecentNgSummaryText));
            }
        }

        public int RecentNgWindowSize
        {
            get => _recentNgWindowSize;
            set
            {
                if (SetProperty(ref _recentNgWindowSize, value))
                    RaisePropertyChanged(nameof(RecentNgSummaryText));
            }
        }

        public int TotalFrames
        {
            get => _totalFrames;
            set
            {
                if (SetProperty(ref _totalFrames, value))
                {
                    RaisePropertyChanged(nameof(CurrentSpeedHintText));
                    RaisePropertyChanged(nameof(CurrentStatusHintText));
                }
            }
        }

        public int OkFrames
        {
            get => _okFrames;
            set
            {
                if (SetProperty(ref _okFrames, value))
                    RaisePropertyChanged(nameof(CurrentStatusHintText));
            }
        }

        public int NgFrames
        {
            get => _ngFrames;
            set
            {
                if (SetProperty(ref _ngFrames, value))
                    RaisePropertyChanged(nameof(CurrentStatusHintText));
            }
        }

        public double CurrentSpeedMetersPerMinute
        {
            get => _currentSpeedMetersPerMinute;
            set
            {
                if (SetProperty(ref _currentSpeedMetersPerMinute, value))
                {
                    RaisePropertyChanged(nameof(CurrentSpeedDisplayText));
                    RaisePropertyChanged(nameof(CurrentSpeedHintText));
                }
            }
        }

        public string CurrentStatusCode
        {
            get => _currentStatusCode;
            set => SetProperty(ref _currentStatusCode, value);
        }

        public string CurrentStatusText
        {
            get => _currentStatusText;
            set => SetProperty(ref _currentStatusText, value);
        }

        public bool IsDetectionRunning
        {
            get => _isDetectionRunning;
            private set
            {
                if (!SetProperty(ref _isDetectionRunning, value))
                    return;

                RaisePropertyChanged(nameof(DataPrimaryText));
                RaisePropertyChanged(nameof(DataSummaryText));
                RaisePropertyChanged(nameof(CurrentStatusHintText));
            }
        }

        public string ReportSuggestedFileName
        {
            get => _reportSuggestedFileName;
            set => SetProperty(ref _reportSuggestedFileName, value);
        }

        public IReadOnlyList<BandMapReportRow> ReportRows
        {
            get => _reportRows;
            set => SetProperty(ref _reportRows, value ?? Array.Empty<BandMapReportRow>());
        }

        public double FrameSpanMillimeters
        {
            get => _frameSpanMillimeters;
            set
            {
                if (!SetProperty(ref _frameSpanMillimeters, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSettings(frameSpanMillimeters: value);
            }
        }

        public double WindowMeters
        {
            get => _windowMeters;
            set
            {
                if (!SetProperty(ref _windowMeters, value))
                    return;

                if (_isApplyingSnapshot)
                    return;

                _stateService.UpdateSettings(windowMeters: value);
            }
        }

        public string RangeSummary
        {
            get => _rangeSummary;
            set => SetProperty(ref _rangeSummary, value);
        }

        public BandMapPointDisplayMode PointDisplayMode
        {
            get => _pointDisplayMode;
            set
            {
                if (!SetProperty(ref _pointDisplayMode, value))
                    return;

                RaisePropertyChanged(nameof(PointDisplayModeHintText));
            }
        }

        public string PointDisplayModeHintText => PointDisplayMode == BandMapPointDisplayMode.Aggregate
            ? "同位置点合并为 xN，点击查看聚合明细"
            : "按真实坐标绘制，重叠点不做偏移";

        public double? RealtimeWidthMillimeters
        {
            get => _realtimeWidthMillimeters;
            set
            {
                if (!SetProperty(ref _realtimeWidthMillimeters, value))
                    return;

                RaisePropertyChanged(nameof(RealtimeWidthDisplayText));
                RaisePropertyChanged(nameof(LineDistanceSummaryText));
            }
        }

        public double? LineLineDistanceMillimeters
        {
            get => _lineLineDistanceMillimeters;
            set
            {
                if (!SetProperty(ref _lineLineDistanceMillimeters, value))
                    return;

                RaisePropertyChanged(nameof(RealtimeWidthDisplayText));
                RaisePropertyChanged(nameof(LineDistanceSummaryText));
            }
        }

        private double? EffectiveRealtimeWidthMillimeters => RealtimeWidthMillimeters ?? LineLineDistanceMillimeters;

        public string RealtimeWidthDisplayText => EffectiveRealtimeWidthMillimeters.HasValue
            ? Math.Max(0.0, EffectiveRealtimeWidthMillimeters.Value).ToString("F1", CultureInfo.InvariantCulture)
            : "--";

        public string LineDistanceSummaryText => LineLineDistanceMillimeters.HasValue
            ? $"线线距离 {Math.Max(0.0, LineLineDistanceMillimeters.Value):F1} mm"
            : "线线距离 -- mm";

        public string CurrentSpeedDisplayText => _lastFrameUtc == DateTime.MinValue
            ? "--"
            : Math.Max(0, CurrentSpeedMetersPerMinute).ToString("F1");

        public string CurrentSpeedHintText => _lastFrameUtc == DateTime.MinValue
            ? "m/min · 等待首帧"
            : $"m/min · 累计 {CumulativeMeters:F2} m";

        public string CurrentStatusHintText => TotalFrames <= 0
            ? ResolveStreamStateHint()
            : $"{ResolveStreamStateHint()} · OK {OkFrames} / NG {NgFrames}";

        public string RecentNgSummaryText => $"近 {Math.Max(1, RecentNgWindowSize)} 帧内 NG 帧数";

        public string ProductionSummaryText => _lastFrameUtc == DateTime.MinValue
            ? $"卷 {BatchNumberText} · 等待首帧"
            : $"累计 {CumulativeMeters:F2} m · 卷 {BatchNumberText}";

        public string QualitySummaryText => TotalFrames <= 0
            ? "OK/总帧 -- · 等待判定"
            : $"OK {OkFrames}/{TotalFrames} · NG {NgFrames}";

        public string QualityRiskText =>
            $"近{Math.Max(1, RecentNgWindowSize)}帧 NG {RecentNgFrameCount} · NG率 {FormatRate(NgFrames, TotalFrames)}";

        public string DataPrimaryText => StreamState switch
        {
            "Live" => "实时中",
            "Warn" => IsDetectionRunning && _lastFrameUtc == DateTime.MinValue ? "检测中" : "等新帧",
            "Stale" => "已滞后",
            _ => "待首帧"
        };

        public string DataSummaryText => string.IsNullOrWhiteSpace(LastFrameIdText) || LastFrameIdText == "-"
            ? StreamStatusText
            : $"{StreamStatusText} · 最后帧 {LastFrameIdText}";

        public string RouteSummaryText => ShowPathStatusBadges
            ? $"{Path1Header} {Path1Result} · {Path2Header} {Path2Result}"
            : "未启用双路状态";

        public string RollTraceSummaryText => $"卷 {BatchNumberText} · 总帧 {TotalFrames}";

        public bool HasLegendItems => LegendItems.Count > 0;

        public bool IsBrjAutoSyncEnabled
        {
            get => _isBrjAutoSyncEnabled;
            set
            {
                if (!SetProperty(ref _isBrjAutoSyncEnabled, value))
                    return;

                if (!_isLoaded)
                    return;

                if (value)
                    _brjAutoSyncTimer.Start();
                else
                    _brjAutoSyncTimer.Stop();
            }
        }

        public bool IsBrjDirty
        {
            get => _isBrjDirty;
            private set => SetProperty(ref _isBrjDirty, value);
        }

        public bool IsBrjSyncing
        {
            get => _isBrjSyncing;
            private set
            {
                if (SetProperty(ref _isBrjSyncing, value))
                    RaisePropertyChanged(nameof(BrjSyncButtonText));
            }
        }

        public string BrjSyncStatusText
        {
            get => _brjSyncStatusText;
            private set => SetProperty(ref _brjSyncStatusText, value ?? string.Empty);
        }

        public DateTime? LastBrjSyncTime
        {
            get => _lastBrjSyncTime;
            private set => SetProperty(ref _lastBrjSyncTime, value);
        }

        public string BrjSyncButtonText => IsBrjSyncing ? "同步中..." : "同步BRJ";

        public double ViewportStartMeters
        {
            get => _viewportStartMeters;
            set
            {
                if (!SetProperty(ref _viewportStartMeters, value))
                    return;

                RaisePropertyChanged(nameof(ViewportScrollOffset));

                if (_isApplyingSnapshot)
                    return;

                QueueViewportStartCommit(value);
            }
        }

        public double ViewportMaxStartMeters
        {
            get => _viewportMaxStartMeters;
            set
            {
                if (!SetProperty(ref _viewportMaxStartMeters, value))
                    return;

                RaiseViewportScrollProperties();
            }
        }

        public bool CanScrollViewport => ViewportMaxStartMeters > 0.001;

        public double ViewportScrollOffset
        {
            get => Math.Max(0, ViewportMaxStartMeters - ViewportStartMeters);
            set
            {
                double safeOffset = Math.Clamp(value, 0, Math.Max(0, ViewportMaxStartMeters));
                double targetStartMeters = Math.Max(0, ViewportMaxStartMeters - safeOffset);
                ViewportStartMeters = targetStartMeters;
            }
        }

        public double ViewportScrollViewportSize => Math.Max(1.0, WindowMeters);

        public double ViewportScrollSmallChange => Math.Max(0.2, Math.Round(WindowMeters / 8.0, 2));

        public double ViewportScrollLargeChange => Math.Max(0.5, Math.Round(WindowMeters * 0.75, 2));

        public double CanvasWidth { get; private set; }
        public double CanvasHeight { get; private set; }
        public double PlotLeft { get; private set; }
        public double PlotTop { get; private set; }
        public double PlotWidth { get; private set; }
        public double PlotHeight { get; private set; }
        public double PlotBottom => PlotTop + PlotHeight;

        public bool IsSelectedMapPointPopupOpen
        {
            get => _isSelectedMapPointPopupOpen;
            set => SetProperty(ref _isSelectedMapPointPopupOpen, value);
        }

        public string SelectedMapPointPopupTitle
        {
            get => _selectedMapPointPopupTitle;
            set => SetProperty(ref _selectedMapPointPopupTitle, value);
        }

        public string SelectedMapPointPopupDetail
        {
            get => _selectedMapPointPopupDetail;
            set => SetProperty(ref _selectedMapPointPopupDetail, value);
        }

        public double SelectedMapPointPopupOffsetX
        {
            get => _selectedMapPointPopupOffsetX;
            set => SetProperty(ref _selectedMapPointPopupOffsetX, value);
        }

        public double SelectedMapPointPopupOffsetY
        {
            get => _selectedMapPointPopupOffsetY;
            set => SetProperty(ref _selectedMapPointPopupOffsetY, value);
        }

        public DelegateCommand LoadCommand => new DelegateCommand(OnLoad);
        public DelegateCommand UnLoadedCommand => new DelegateCommand(OnUnload);
        public DelegateCommand StartDetectionCommand => new DelegateCommand(OnStartDetection);
        public DelegateCommand EndDetectionCommand => new DelegateCommand(OnEndDetection);
        public DelegateCommand ResetCommand => new DelegateCommand(() => _stateService.Reset());
        public DelegateCommand ChangeBatchCommand => new DelegateCommand(OnChangeBatch);
        public DelegateCommand SyncBrjReportCommand { get; }
        public DelegateCommand<BandMapLegendItem> ToggleLegendCommand { get; }
        public DelegateCommand<BandMapPointItem> SelectPointCommand { get; }
        public DelegateCommand<BandMapRecentDefectItem> SelectRecentDefectCommand { get; }

        private void OnLoad()
        {
            if (_isLoaded)
                return;
            _isLoaded = true;
            _stateService.SnapshotChanged += OnSnapshotChanged;
            ApplySnapshot(_stateService.GetSnapshot());
            RefreshLineLineDistanceFromGlobalParams();
            _watchdogTimer.Start();
            if (IsBrjAutoSyncEnabled)
                _brjAutoSyncTimer.Start();
        }

        private void OnUnload()
        {
            if (!_isLoaded)
                return;

            _isLoaded = false;
            _stateService.SnapshotChanged -= OnSnapshotChanged;
            _watchdogTimer.Stop();
            _viewportCommitTimer.Stop();
            _brjAutoSyncTimer.Stop();
            lock (_snapshotSync)
            {
                _pendingSnapshot = null;
                _snapshotApplyQueued = false;
                _coalescedSnapshotCount = 0;
                _lastSnapshotApplyUtc = DateTime.MinValue;
            }
        }

        private void OnSnapshotChanged(BandMapStateSnapshot snapshot)
        {
            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            TimeSpan delay;
            lock (_snapshotSync)
            {
                _pendingSnapshot = snapshot;
                if (_snapshotApplyQueued)
                {
                    _coalescedSnapshotCount++;
                    return;
                }

                _snapshotApplyQueued = true;
                delay = GetSnapshotApplyDelayLocked();
            }

            QueueSnapshotApply(dispatcher, delay);
        }

        private TimeSpan GetSnapshotApplyDelayLocked()
        {
            if (_lastSnapshotApplyUtc == DateTime.MinValue)
                return TimeSpan.Zero;

            TimeSpan interval = TimeSpan.FromMilliseconds(SnapshotApplyThrottleMs);
            TimeSpan elapsed = DateTime.UtcNow - _lastSnapshotApplyUtc;
            TimeSpan remaining = interval - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void QueueSnapshotApply(Dispatcher dispatcher, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                dispatcher.BeginInvoke(new Action(ApplyPendingSnapshot), DispatcherPriority.Background);
                return;
            }

            var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = delay
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ApplyPendingSnapshot();
            };
            timer.Start();
        }

        private void ApplyPendingSnapshot()
        {
            BandMapStateSnapshot snapshot;
            int coalescedCount;
            lock (_snapshotSync)
            {
                snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
                coalescedCount = _coalescedSnapshotCount;
                _coalescedSnapshotCount = 0;
            }

            if (!_isLoaded)
            {
                lock (_snapshotSync)
                {
                    _snapshotApplyQueued = false;
                }
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                ApplySnapshot(snapshot);
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMap] ApplySnapshot failed: {ex.Message}");
            }
            finally
            {
                if (coalescedCount >= SnapshotCoalescedLogThreshold || stopwatch.ElapsedMilliseconds >= SnapshotApplySlowLogMs)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[BandMap] ApplySnapshot frame={snapshot?.LastFrameIdText ?? string.Empty}, coalesced={coalescedCount}, elapsed={stopwatch.ElapsedMilliseconds}ms");
                }
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;
            TimeSpan delay = TimeSpan.Zero;
            bool shouldQueueAgain;
            lock (_snapshotSync)
            {
                _lastSnapshotApplyUtc = DateTime.UtcNow;
                shouldQueueAgain = _pendingSnapshot != null && _isLoaded;
                if (shouldQueueAgain)
                {
                    delay = GetSnapshotApplyDelayLocked();
                }
                else
                {
                    _snapshotApplyQueued = false;
                }
            }

            if (shouldQueueAgain && dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                QueueSnapshotApply(dispatcher, delay);
        }

        private void ApplySnapshot(BandMapStateSnapshot snapshot)
        {
            snapshot ??= _stateService.GetSnapshot();
            _isApplyingSnapshot = true;
            try
            {
                CanvasWidth = snapshot.CanvasWidth;
                CanvasHeight = snapshot.CanvasHeight;
                PlotLeft = snapshot.PlotLeft;
                PlotTop = snapshot.PlotTop;
                PlotWidth = snapshot.PlotWidth;
                PlotHeight = snapshot.PlotHeight;
                RaisePropertyChanged(nameof(CanvasWidth));
                RaisePropertyChanged(nameof(CanvasHeight));
                RaisePropertyChanged(nameof(PlotLeft));
                RaisePropertyChanged(nameof(PlotTop));
                RaisePropertyChanged(nameof(PlotWidth));
                RaisePropertyChanged(nameof(PlotHeight));
                RaisePropertyChanged(nameof(PlotBottom));

                CumulativeMeters = snapshot.CumulativeMeters;
                BatchNumberText = string.IsNullOrWhiteSpace(snapshot.BatchNumberText)
                    ? BuildFallbackBatchNumberText()
                    : snapshot.BatchNumberText;
                TotalFrames = snapshot.TotalFrames;
                OkFrames = snapshot.OkFrames;
                NgFrames = snapshot.NgFrames;
                RecentNgFrameCount = snapshot.RecentNgFrameCount;
                RecentNgWindowSize = snapshot.RecentNgWindowSize;
                CurrentSpeedMetersPerMinute = snapshot.CurrentSpeedMetersPerMinute;
                string nextStatusCode = snapshot.CurrentStatusCode ?? "Idle";
                string nextStatusText = string.IsNullOrWhiteSpace(snapshot.CurrentStatusText) ? "未运行" : snapshot.CurrentStatusText;
                CurrentStatusCode = nextStatusCode;
                CurrentStatusText = IsDetectionRunning
                    && nextStatusCode == "Idle"
                    && snapshot.LastFrameUtc == DateTime.MinValue
                    ? "检测中"
                    : nextStatusText;
                ReportSuggestedFileName = snapshot.ReportSuggestedFileName ?? "DefectOverview_Report.csv";
                ReportRows = snapshot.ReportRows ?? Array.Empty<BandMapReportRow>();
                FrameSpanMillimeters = snapshot.FrameSpanMillimeters;
                WindowMeters = snapshot.WindowMeters;
                ViewportMaxStartMeters = snapshot.ViewportMaxStartMeters;
                double nextViewportStartMeters = _pendingViewportStartMeters.HasValue
                    ? Math.Clamp(_pendingViewportStartMeters.Value, 0, Math.Max(0, ViewportMaxStartMeters))
                    : snapshot.ViewportStartMeters;
                ViewportStartMeters = nextViewportStartMeters;
                LastFrameIdText = snapshot.LastFrameIdText ?? "-";
                ShowPathStatusBadges = snapshot.ShowPathStatusBadges;
                Path1Header = snapshot.Path1Header ?? "左路";
                Path2Header = snapshot.Path2Header ?? "右路";
                Path1Result = snapshot.Path1Result ?? "-";
                Path2Result = snapshot.Path2Result ?? "-";
                Path1DefectCount = snapshot.Path1DefectCount;
                Path2DefectCount = snapshot.Path2DefectCount;
                IsSlittingEnabled = snapshot.IsSlittingEnabled;
                KnifeSpacingMillimeters = snapshot.KnifeSpacingMillimeters;
                FirstCutOffsetMillimeters = snapshot.FirstCutOffsetMillimeters;
                StripWidthMillimeters = snapshot.StripWidthMillimeters ?? StripWidthMillimeters;
                SlitCount = snapshot.SlitCount > 0 ? snapshot.SlitCount : SlitCount;
                SlittingSummaryText = snapshot.SlittingSummaryText ?? "切刀分切未启用";
                RangeSummary = snapshot.RangeSummary ?? "当前累计 0.00 m | 显示窗口 12.0 m";
                _lastFrameUtc = snapshot.LastFrameUtc;
                ReplaceCollection(LegendItems, snapshot.LegendItems);
                ReplaceCollection(GuideLines, snapshot.GuideLines);
                ReplaceCollection(DefectPoints, snapshot.DefectPoints);
                ReplaceCollection(XAxisTicks, snapshot.XAxisTicks);
                ReplaceCollection(YAxisTicks, snapshot.YAxisTicks);
                ReplaceCollection(RecentDefects, snapshot.RecentDefects);
                UpdateBrjDirtyState(snapshot);
                RefreshSelectedMapPointPopup(snapshot);
                RaisePropertyChanged(nameof(HasLegendItems));
                RaiseViewportScrollProperties();
                RaiseHeaderComputedProperties();
            }
            finally
            {
                _isApplyingSnapshot = false;
                UpdateStreamStatus();
            }
        }

        private void OnWatchdogTick(object sender, EventArgs e)
        {
            RefreshLineLineDistanceFromGlobalParams();
            UpdateStreamStatus();
        }

        private void RefreshLineLineDistanceFromGlobalParams()
        {
            if (!TryFindLineLineDistanceGlobalParam(out TransmitParam param))
                return;

            LineLineDistanceMillimeters = TryConvertToFiniteDouble(param?.Value, out double distance)
                ? distance
                : null;
        }

        private static bool TryFindLineLineDistanceGlobalParam(out TransmitParam bestParam)
        {
            bestParam = null;
            var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution == null)
                return true;

            int bestScore = 0;
            if (!TrySnapshotLineLineDistanceGlobalParams(solution.GlobalParams, out TransmitParam[] globalParams))
                return false;

            if (!TrySnapshotLineLineDistanceGlobalParams(solution.CustomGlobalParams, out TransmitParam[] customGlobalParams))
                return false;

            foreach (TransmitParam param in globalParams)
            {
                int score = GetLineLineDistanceParamScore(param);
                if (score > bestScore)
                {
                    bestParam = param;
                    bestScore = score;
                }
            }

            foreach (TransmitParam param in customGlobalParams)
            {
                int score = GetLineLineDistanceParamScore(param) - 5;
                if (score > bestScore)
                {
                    bestParam = param;
                    bestScore = score;
                }
            }

            if (bestScore <= 0)
                bestParam = null;

            return true;
        }

        private static bool TrySnapshotLineLineDistanceGlobalParams(IEnumerable<TransmitParam> source, out TransmitParam[] snapshot)
        {
            snapshot = Array.Empty<TransmitParam>();
            if (source == null)
                return true;

            try
            {
                snapshot = source.Where(item => item != null).ToArray();
                return true;
            }
            catch (InvalidOperationException)
            {
                // GlobalParams may be updated by node output synchronization while the UI timer reads it.
                return false;
            }
        }

        private static int GetLineLineDistanceParamScore(TransmitParam param)
        {
            if (param == null)
                return 0;

            int score = 0;
            if (string.Equals(param.Name, LineLineDistanceGlobalName, StringComparison.Ordinal))
                score += 100;
            if (string.Equals(param.ParamName, LineLineDistanceParamName, StringComparison.OrdinalIgnoreCase))
                score += 40;
            if (string.Equals(param.Describe, LineLineDistanceDescription, StringComparison.Ordinal))
                score += 30;
            if (ContainsOrdinal(param.Name, LineLineDistanceDescription))
                score += 20;
            if (ContainsOrdinalIgnoreCase(param.ResourcePath, DistanceLlResourcePathMarker))
                score += 60;
            else if (ContainsOrdinalIgnoreCase(param.ResourcePath, "ALGO.DistanceLL"))
                score += 20;
            if (TryConvertToFiniteDouble(param.Value, out _))
                score += 10;

            return score;
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source)
                && !string.IsNullOrWhiteSpace(value)
                && source.IndexOf(value, StringComparison.Ordinal) >= 0;
        }

        private static bool ContainsOrdinalIgnoreCase(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source)
                && !string.IsNullOrWhiteSpace(value)
                && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryConvertToFiniteDouble(object value, out double result)
        {
            result = 0;
            if (value == null)
                return false;

            switch (value)
            {
                case double doubleValue:
                    result = doubleValue;
                    return IsFinite(result);
                case float floatValue:
                    result = floatValue;
                    return IsFinite(result);
                case decimal decimalValue:
                    result = (double)decimalValue;
                    return IsFinite(result);
                case byte byteValue:
                    result = byteValue;
                    return true;
                case sbyte sbyteValue:
                    result = sbyteValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case ushort ushortValue:
                    result = ushortValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return IsFinite(result);
                case ulong ulongValue:
                    result = ulongValue;
                    return IsFinite(result);
                case string text:
                    return TryParseFiniteDouble(text, out result);
            }

            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return IsFinite(result);
            }
            catch (Exception)
            {
                return TryParseFiniteDouble(value.ToString(), out result);
            }
        }

        private static bool TryParseFiniteDouble(string text, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            return (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                    || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                && IsFinite(result);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void OnViewportCommitTick(object sender, EventArgs e)
        {
            _viewportCommitTimer.Stop();
            CommitPendingViewportStart(runAsync: true);
        }

        private void OnBrjAutoSyncTick(object sender, EventArgs e)
        {
            if (!IsBrjAutoSyncEnabled || !IsBrjDirty || IsBrjSyncing)
                return;

            _ = SyncBrjReportAsync(manual: false);
        }

        private int GetCurrentCachedDefectCount()
        {
            return LegendItems.Sum(item => Math.Max(0, item?.DefectCount ?? 0));
        }

        private async Task<bool> SyncBrjReportAsync(bool manual, bool isRollCompleted = false, bool force = false)
        {
            if (IsBrjSyncing)
            {
                if (manual)
                    BrjSyncStatusText = "BRJ正在同步中";

                return false;
            }

            bool hasData = TotalFrames > 0 || (ReportRows?.Count ?? 0) > 0 || NgFrames > 0;
            if (!hasData)
            {
                if (manual)
                    BrjSyncStatusText = "BRJ暂无可同步数据";

                return true;
            }

            if (!force && !IsBrjDirty)
            {
                if (manual)
                    BrjSyncStatusText = "BRJ已是最新快照";

                return true;
            }

            int cachedDefectCount = GetCurrentCachedDefectCount();
            if (!manual
                && !isRollCompleted
                && !force
                && cachedDefectCount > DefectOverviewRuntimeOptions.MaxAutoBrjSyncDefects)
            {
                BrjSyncStatusText = $"BRJ自动同步跳过：缺陷数 {cachedDefectCount}，等待换卷收尾同步";
                return true;
            }

            IsBrjSyncing = true;
            long requestVersion = _brjSnapshotVersion;
            try
            {
                if (PrismProvider.EventAggregator == null)
                {
                    BrjSyncStatusText = "BRJ同步失败：事件总线未初始化";
                    return false;
                }

                var completion = new TaskCompletionSource<DefectBatchReportSyncResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                DefectBatchReportSyncRequest request = _stateService.CreateBrjReportSyncRequest(
                    ResolveBrjSN(),
                    isRollCompleted,
                    requestVersion,
                    completion);

                BrjSyncStatusText = isRollCompleted ? "BRJ换卷收尾同步中..." : "BRJ同步中...";
                PrismProvider.EventAggregator.GetEvent<DefectBatchReportSyncEvent>().Publish(request);

                Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(BrjSyncTimeoutMs));
                if (completedTask != completion.Task)
                {
                    BrjSyncStatusText = "BRJ同步超时：请确认BRJ报表模块已加载";
                    return false;
                }

                DefectBatchReportSyncResult result = await completion.Task;
                if (!result.Success)
                {
                    BrjSyncStatusText = result.Message;
                    return false;
                }

                LastBrjSyncTime = DateTime.Now;
                _lastBrjSyncedVersion = Math.Max(_lastBrjSyncedVersion, request.SnapshotVersion);
                if (isRollCompleted)
                    _lastRollCompletedSyncedVersion = Math.Max(_lastRollCompletedSyncedVersion, request.SnapshotVersion);

                if (_brjSnapshotVersion == request.SnapshotVersion)
                    IsBrjDirty = false;

                BrjSyncStatusText = isRollCompleted
                    ? $"BRJ换卷快照已同步 {LastBrjSyncTime:HH:mm:ss}"
                    : $"BRJ快照已同步 {LastBrjSyncTime:HH:mm:ss}";
                return true;
            }
            catch (Exception ex)
            {
                BrjSyncStatusText = $"BRJ同步异常：{ex.Message}";
                return false;
            }
            finally
            {
                IsBrjSyncing = false;
            }
        }

        private void UpdateBrjDirtyState(BandMapStateSnapshot snapshot)
        {
            bool hasData = snapshot != null
                && (snapshot.TotalFrames > 0 || (snapshot.ReportRows?.Count ?? 0) > 0 || snapshot.NgFrames > 0);
            if (!hasData)
            {
                _lastBrjSnapshotFingerprint = string.Empty;
                IsBrjDirty = false;
                if (!IsBrjSyncing)
                    BrjSyncStatusText = "BRJ等待数据";
                return;
            }

            string fingerprint = BuildBrjSnapshotFingerprint(snapshot);
            if (string.Equals(_lastBrjSnapshotFingerprint, fingerprint, StringComparison.Ordinal))
                return;

            _lastBrjSnapshotFingerprint = fingerprint;
            _brjSnapshotVersion++;
            IsBrjDirty = true;
            if (!IsBrjSyncing)
                BrjSyncStatusText = $"BRJ待同步 v{_brjSnapshotVersion}";
        }

        private static string BuildBrjSnapshotFingerprint(BandMapStateSnapshot snapshot)
        {
            return string.Join("|",
                snapshot.BatchNumberText ?? string.Empty,
                snapshot.TotalFrames.ToString(CultureInfo.InvariantCulture),
                snapshot.OkFrames.ToString(CultureInfo.InvariantCulture),
                snapshot.NgFrames.ToString(CultureInfo.InvariantCulture),
                (snapshot.ReportRows?.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                snapshot.LastFrameIdText ?? string.Empty,
                Math.Round(snapshot.CumulativeMeters, 3).ToString(CultureInfo.InvariantCulture),
                snapshot.SlittingSummaryText ?? string.Empty);
        }

        private string ResolveBrjSN()
        {
            return string.IsNullOrWhiteSpace(BatchNumberText) ? BuildFallbackBatchNumberText() : BatchNumberText.Trim();
        }

        private static string BuildFallbackBatchNumberText()
        {
            return $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-001";
        }

        private void UpdateStreamStatus()
        {
            if (_lastFrameUtc == DateTime.MinValue)
            {
                StreamState = IsDetectionRunning ? "Warn" : "Idle";
                StreamStatusText = IsDetectionRunning ? "检测中，等待首帧" : "等待首帧";
                RaiseHeaderComputedProperties();
                return;
            }

            var ageMs = (DateTime.UtcNow - _lastFrameUtc).TotalMilliseconds;
            if (ageMs <= LiveTimeoutMs)
            {
                StreamState = "Live";
                StreamStatusText = "流状态正常";
            }
            else if (ageMs <= StaleTimeoutMs)
            {
                StreamState = "Warn";
                StreamStatusText = $"等待新帧 ({ageMs / 1000:F1}s)";
            }
            else
            {
                StreamState = "Stale";
                StreamStatusText = $"数据滞后 ({ageMs / 1000:F1}s)";
            }

            RaiseHeaderComputedProperties();
        }

        private void OnSelectPoint(BandMapPointItem point)
        {
            if (string.IsNullOrWhiteSpace(point?.DefectKey))
                return;

            _stateService.SelectDefect(point.DefectKey);
        }

        private void OnSelectRecentDefect(BandMapRecentDefectItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.DefectKey))
                return;

            ClearSelectedMapPointPopup();
            _stateService.SelectDefect(item.DefectKey);
        }

        private void OnToggleLegend(BandMapLegendItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.LegendKey))
                return;

            _stateService.SetLegendFilter(item.LegendKey, !item.IsChecked);
        }

        private async void OnChangeBatch()
        {
            bool hasData = TotalFrames > 0 || (ReportRows?.Count ?? 0) > 0 || NgFrames > 0;
            bool needsRollCompletedSync = hasData && _lastRollCompletedSyncedVersion < _brjSnapshotVersion;
            if (needsRollCompletedSync)
            {
                bool synced = await SyncBrjReportAsync(manual: false, isRollCompleted: true, force: true);
                if (!synced)
                {
                    MessageBox.Show("BRJ报表收尾同步失败，已取消换卷。请确认BRJ报表模块已加载后重试。", "BRJ报表同步", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _stateService.ChangeBatch();
        }

        private void OnStartDetection()
        {
            IsDetectionRunning = true;
            if (_lastFrameUtc == DateTime.MinValue)
            {
                CurrentStatusCode = "Idle";
                CurrentStatusText = "检测中";
            }

            UpdateStreamStatus();

            try
            {
                if (PrismProvider.EventAggregator == null)
                {
                    MessageBox.Show("事件总线未初始化，无法触发开始检测。", "开始检测", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PrismProvider.EventAggregator
                    .GetEvent<SwitchWorkStatusEvent>()
                    .Publish((eRunStatus.Running, -1));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开始检测触发失败：{ex.Message}", "开始检测", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnEndDetection()
        {
            IsDetectionRunning = false;
            PrismProvider.WorkStatusManager?.SwitchWorkStatus(WorkStatus.Stopped);
            UpdateStreamStatus();

            bool hasData = TotalFrames > 0 || (ReportRows?.Count ?? 0) > 0 || NgFrames > 0;
            if (!hasData)
            {
                CurrentStatusCode = "Idle";
                CurrentStatusText = "已结束";
                return;
            }

            bool synced = await SyncBrjReportAsync(manual: false, isRollCompleted: true, force: true);
            CurrentStatusCode = "Idle";
            CurrentStatusText = "已结束";

            if (!synced)
            {
                MessageBox.Show("检测已结束，但 BRJ 报表收尾同步失败。请确认 BRJ 报表模块已加载后手动同步。", "结束检测", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void QueueViewportStartCommit(double targetMeters)
        {
            _pendingViewportStartMeters = targetMeters;
            _viewportCommitTimer.Stop();
            _viewportCommitTimer.Start();
        }

        public void CommitViewportStartNow()
        {
            _viewportCommitTimer.Stop();
            CommitPendingViewportStart(runAsync: false);
        }

        private void CommitPendingViewportStart(bool runAsync)
        {
            if (!_pendingViewportStartMeters.HasValue)
                return;

            double targetMeters = _pendingViewportStartMeters.Value;
            _pendingViewportStartMeters = null;

            if (runAsync)
                _ = Task.Run(() => _stateService.SetViewportStart(targetMeters));
            else
                _stateService.SetViewportStart(targetMeters);
        }

        public void NudgeViewportScroll(double offsetDelta)
        {
            if (!CanScrollViewport || !double.IsFinite(offsetDelta) || Math.Abs(offsetDelta) < double.Epsilon)
                return;

            ViewportScrollOffset += offsetDelta;
        }

        public void UpdateViewportSize(double width, double height)
        {
            _stateService.UpdateViewportSize(width, height);
        }

        public void ShowSelectedMapPointPopup(BandMapPointItem point, double offsetX, double offsetY)
        {
            ShowSelectedMapPointPopup(point, point == null ? Array.Empty<BandMapPointItem>() : new[] { point }, offsetX, offsetY);
        }

        public void ShowSelectedMapPointPopup(
            BandMapPointItem point,
            IReadOnlyList<BandMapPointItem> groupedPoints,
            double offsetX,
            double offsetY)
        {
            if (string.IsNullOrWhiteSpace(point?.DefectKey))
                return;

            _selectedMapPointPopupDefectKey = point.DefectKey;
            _selectedMapPointPopupGroupDefectKeys = groupedPoints?
                .Where(item => !string.IsNullOrWhiteSpace(item?.DefectKey))
                .Select(item => item.DefectKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();
            SelectedMapPointPopupOffsetX = offsetX;
            SelectedMapPointPopupOffsetY = offsetY;
            if (_selectedMapPointPopupGroupDefectKeys.Count > 1)
                UpdateAggregateMapPointPopupContent(groupedPoints);
            else
                UpdateSelectedMapPointPopupContent(point);
            IsSelectedMapPointPopupOpen = true;
        }

        private void RaiseViewportScrollProperties()
        {
            RaisePropertyChanged(nameof(CanScrollViewport));
            RaisePropertyChanged(nameof(ViewportScrollOffset));
            RaisePropertyChanged(nameof(ViewportScrollViewportSize));
            RaisePropertyChanged(nameof(ViewportScrollSmallChange));
            RaisePropertyChanged(nameof(ViewportScrollLargeChange));
        }

        private void RaiseHeaderComputedProperties()
        {
            RaisePropertyChanged(nameof(CurrentSpeedDisplayText));
            RaisePropertyChanged(nameof(CurrentSpeedHintText));
            RaisePropertyChanged(nameof(CurrentStatusHintText));
            RaisePropertyChanged(nameof(RecentNgSummaryText));
            RaisePropertyChanged(nameof(ProductionSummaryText));
            RaisePropertyChanged(nameof(QualitySummaryText));
            RaisePropertyChanged(nameof(QualityRiskText));
            RaisePropertyChanged(nameof(DataPrimaryText));
            RaisePropertyChanged(nameof(DataSummaryText));
            RaisePropertyChanged(nameof(RealtimeWidthDisplayText));
            RaisePropertyChanged(nameof(LineDistanceSummaryText));
            RaisePropertyChanged(nameof(RouteSummaryText));
            RaisePropertyChanged(nameof(RollTraceSummaryText));
        }

        private static string FormatRate(int value, int total)
        {
            if (total <= 0)
                return "--";

            return $"{Math.Max(0, value) * 100.0 / total:F1}%";
        }

        private string ResolveStreamStateHint()
        {
            return StreamState switch
            {
                "Live" => "实时判定",
                "Warn" => "等待新帧",
                "Stale" => "数据滞后",
                _ => "等待首帧"
            };
        }

        private void RefreshSelectedMapPointPopup(BandMapStateSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(_selectedMapPointPopupDefectKey)
                || string.IsNullOrWhiteSpace(snapshot?.SelectedDefectKey)
                || !IsSelectedPopupKeyStillActive(snapshot.SelectedDefectKey))
            {
                ClearSelectedMapPointPopup();
                return;
            }

            if (_selectedMapPointPopupGroupDefectKeys.Count > 1)
            {
                HashSet<string> keySet = new(_selectedMapPointPopupGroupDefectKeys, StringComparer.Ordinal);
                var groupedPoints = snapshot.DefectPoints?
                    .Where(item => item != null && keySet.Contains(item.DefectKey))
                    .ToList() ?? new List<BandMapPointItem>();
                if (groupedPoints.Count > 1)
                {
                    UpdateAggregateMapPointPopupContent(groupedPoints);
                    IsSelectedMapPointPopupOpen = true;
                    return;
                }
            }

            var selectedPoint = snapshot.DefectPoints?.FirstOrDefault(item => item.DefectKey == snapshot.SelectedDefectKey);
            if (selectedPoint == null)
            {
                IsSelectedMapPointPopupOpen = false;
                return;
            }

            UpdateSelectedMapPointPopupContent(selectedPoint);
            IsSelectedMapPointPopupOpen = true;
        }

        private bool IsSelectedPopupKeyStillActive(string selectedDefectKey)
        {
            if (string.IsNullOrWhiteSpace(selectedDefectKey))
                return false;

            if (_selectedMapPointPopupGroupDefectKeys.Count > 1)
                return _selectedMapPointPopupGroupDefectKeys.Contains(selectedDefectKey, StringComparer.Ordinal);

            return string.Equals(_selectedMapPointPopupDefectKey, selectedDefectKey, StringComparison.Ordinal);
        }

        private void UpdateSelectedMapPointPopupContent(BandMapPointItem point)
        {
            SelectedMapPointPopupTitle = point?.ClassName ?? "-";

            var detailParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(point?.PositionText))
                detailParts.Add($"位置 {point.PositionText}");
            if (!string.IsNullOrWhiteSpace(point?.MeterText))
                detailParts.Add($"累计 {point.MeterText}");
            if (!string.IsNullOrWhiteSpace(point?.PathText))
                detailParts.Add(point.PathText);

            SelectedMapPointPopupDetail = detailParts.Count > 0
                ? string.Join(" | ", detailParts)
                : string.Empty;
        }

        private void UpdateAggregateMapPointPopupContent(IReadOnlyList<BandMapPointItem> points)
        {
            var validPoints = points?
                .Where(item => item != null)
                .ToList() ?? new List<BandMapPointItem>();
            if (validPoints.Count == 0)
            {
                SelectedMapPointPopupTitle = "-";
                SelectedMapPointPopupDetail = string.Empty;
                return;
            }

            BandMapPointItem first = validPoints[0];
            SelectedMapPointPopupTitle = $"聚合缺陷 x{validPoints.Count}";
            var detailParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(first.PositionText))
                detailParts.Add($"位置 {first.PositionText}");
            if (!string.IsNullOrWhiteSpace(first.MeterText))
                detailParts.Add($"累计 {first.MeterText}");

            detailParts.AddRange(validPoints
                .Take(6)
                .Select((item, index) => FormatAggregatePointLine(item, index)));

            if (validPoints.Count > 6)
                detailParts.Add($"还有 {validPoints.Count - 6} 条...");

            SelectedMapPointPopupDetail = string.Join(Environment.NewLine, detailParts);
        }

        private static string FormatAggregatePointLine(BandMapPointItem point, int index)
        {
            string className = string.IsNullOrWhiteSpace(point?.ClassName) ? "-" : point.ClassName;
            string pathText = string.IsNullOrWhiteSpace(point?.PathText) ? "-" : point.PathText;
            string frameText = string.IsNullOrWhiteSpace(point?.FrameIdText) ? "-" : point.FrameIdText;
            return $"{index + 1}. {className} | {pathText} | 帧 {frameText} | #{point?.ResultIndex ?? 0}";
        }

        private void ClearSelectedMapPointPopup()
        {
            _selectedMapPointPopupDefectKey = null;
            _selectedMapPointPopupGroupDefectKeys = Array.Empty<string>();
            SelectedMapPointPopupTitle = string.Empty;
            SelectedMapPointPopupDetail = string.Empty;
            IsSelectedMapPointPopupOpen = false;
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            if (target is BulkObservableCollection<T> bulkCollection)
            {
                bulkCollection.ReplaceWith(source);
                return;
            }

            target.Clear();
            if (source == null)
                return;

            foreach (var item in source)
                target.Add(item);
        }
    }

    internal sealed class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotifications;

        public void ReplaceWith(IEnumerable<T> source)
        {
            _suppressNotifications = true;
            try
            {
                Items.Clear();
                if (source != null)
                {
                    foreach (var item in source)
                        Items.Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
            }

            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnPropertyChanged(e);
        }
    }
}

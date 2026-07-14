using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Custom.DefectOverview.ViewModels
{
    public class SharedDefectWallViewModel : BindableBase
    {
        private const int SnapshotApplySlowLogMs = 80;
        private const int SnapshotCoalescedLogThreshold = 3;
        private static readonly int SnapshotApplyThrottleMs = DefectOverviewRuntimeOptions.WallRefreshIntervalMs;

        private readonly IBandMapStateService _stateService;
        private readonly object _snapshotSync = new();

        private bool _isLoaded;
        private BandMapStateSnapshot _snapshot = new();
        private BandMapStateSnapshot _pendingSnapshot;
        private bool _snapshotApplyQueued;
        private int _coalescedSnapshotCount;
        private DateTime _lastSnapshotApplyUtc = DateTime.MinValue;

        public SharedDefectWallViewModel(IBandMapStateService stateService)
        {
            _stateService = stateService;
            SelectSharedDefectWallItemCommand = new DelegateCommand<BandMapWallItem>(OnSelectSharedDefectWallItem);
            FirstSharedDefectWallPageCommand = new DelegateCommand(OnFirstSharedDefectWallPage);
            PreviousSharedDefectWallPageCommand = new DelegateCommand(OnPreviousSharedDefectWallPage);
            NextSharedDefectWallPageCommand = new DelegateCommand(OnNextSharedDefectWallPage);
            LastSharedDefectWallPageCommand = new DelegateCommand(OnLastSharedDefectWallPage);
            KeepLatestSharedDefectWallPageCommand = new DelegateCommand(OnKeepLatestSharedDefectWallPage);
            OpenSharedDefectDetailCommand = new DelegateCommand(OnOpenSharedDefectDetail);
            ApplySnapshot(_stateService.GetSnapshot());
        }

        public DelegateCommand LoadCommand => new DelegateCommand(OnLoad);
        public DelegateCommand UnLoadedCommand => new DelegateCommand(OnUnload);
        public DelegateCommand<BandMapWallItem> SelectSharedDefectWallItemCommand { get; }
        public DelegateCommand FirstSharedDefectWallPageCommand { get; }
        public DelegateCommand PreviousSharedDefectWallPageCommand { get; }
        public DelegateCommand NextSharedDefectWallPageCommand { get; }
        public DelegateCommand LastSharedDefectWallPageCommand { get; }
        public DelegateCommand KeepLatestSharedDefectWallPageCommand { get; }
        public DelegateCommand OpenSharedDefectDetailCommand { get; }

        public IReadOnlyList<BandMapWallItem> SharedDefectWallItems =>
            _snapshot?.WallItems ?? Array.Empty<BandMapWallItem>();

        public BandMapWallItem SelectedSharedDefectWallItem => _snapshot?.SelectedWallItem;

        public bool HasSharedDefectWallItems => SharedDefectWallItems.Count > 0;

        public bool HasSharedSelectedDefect => SelectedSharedDefectWallItem != null;

        public ImageSource SharedSelectedDefectPreviewImage =>
            SelectedSharedDefectWallItem?.PreviewImage ?? SelectedSharedDefectWallItem?.ThumbnailImage;

        public Brush SharedSelectedDefectAccentBrush =>
            SelectedSharedDefectWallItem?.AccentBrush ?? Brushes.Transparent;

        public string SharedSelectedDefectTitle =>
            SelectedSharedDefectWallItem?.ClassName ?? "未选择缺陷";

        public string SharedSelectedDefectSubtitle =>
            SelectedSharedDefectWallItem?.SummaryText ?? "从缺陷地图点位或缺陷墙缩略卡中选择一个缺陷。";

        public string SharedSelectedDefectDetailLine =>
            SelectedSharedDefectWallItem?.DetailText ?? "当前没有可查看的缺陷详情。";

        public string SharedSelectedDefectFrameLine => SelectedSharedDefectWallItem == null
            ? $"最近帧 {_snapshot?.LastFrameIdText ?? "-"}"
            : string.IsNullOrWhiteSpace(SelectedSharedDefectWallItem.PathText)
                ? SelectedSharedDefectWallItem.FrameIdText
                : $"{SelectedSharedDefectWallItem.FrameIdText} | {SelectedSharedDefectWallItem.PathText}";

        public string SharedDefectWallSummaryText =>
            _snapshot?.WallSummaryText ?? "最近缺陷 0 条";

        public int SharedDefectWallCurrentPage => _snapshot?.WallCurrentPage ?? 1;

        public int SharedDefectWallTotalPages => Math.Max(1, _snapshot?.WallTotalPages ?? 1);

        public string SharedDefectWallPageText => $"{SharedDefectWallCurrentPage}/{SharedDefectWallTotalPages}";

        public bool IsKeepingLatestSharedDefectWallPage => _snapshot?.IsWallPinnedToLatestPage ?? false;

        public bool CanFirstSharedDefectWallPage => SharedDefectWallCurrentPage > 1;

        public bool CanPreviousSharedDefectWallPage => SharedDefectWallCurrentPage > 1;

        public bool CanNextSharedDefectWallPage => SharedDefectWallCurrentPage < SharedDefectWallTotalPages;

        public bool CanLastSharedDefectWallPage => SharedDefectWallCurrentPage < SharedDefectWallTotalPages;

        public bool CanKeepLatestSharedDefectWallPage => !IsKeepingLatestSharedDefectWallPage;

        public string SharedHistorySummary =>
            $"当前帧 {_snapshot?.LastFrameIdText ?? "-"} | 累计 {_snapshot?.CumulativeMeters ?? 0.0:F2} m | {SharedDefectWallSummaryText}";

        private void OnLoad()
        {
            if (_isLoaded)
                return;

            _isLoaded = true;
            _stateService.SnapshotChanged += OnSnapshotChanged;
            ApplySnapshot(_stateService.GetSnapshot());
        }

        private void OnUnload()
        {
            if (!_isLoaded)
                return;

            _isLoaded = false;
            _stateService.SnapshotChanged -= OnSnapshotChanged;
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
                dispatcher.BeginInvoke(new Action(ApplyPendingSnapshot), DispatcherPriority.DataBind);
                return;
            }

            DispatcherTimer timer = new(DispatcherPriority.DataBind, dispatcher)
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

            if (snapshot == null || !_isLoaded)
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
                    $"[SharedDefectWall] ApplySnapshot failed: {ex.Message}");
            }
            finally
            {
                if (coalescedCount >= SnapshotCoalescedLogThreshold || stopwatch.ElapsedMilliseconds >= SnapshotApplySlowLogMs)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[SharedDefectWall] ApplySnapshot frame={snapshot?.LastFrameIdText ?? string.Empty}, coalesced={coalescedCount}, elapsed={stopwatch.ElapsedMilliseconds}ms");
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
            _snapshot = snapshot ?? _stateService.GetSnapshot() ?? new BandMapStateSnapshot();
            RaiseAllSnapshotProperties();
        }

        private void ApplyCommandSnapshotImmediately()
        {
            BandMapStateSnapshot snapshot = _stateService.GetSnapshot();
            lock (_snapshotSync)
            {
                _pendingSnapshot = null;
                _snapshotApplyQueued = false;
                _coalescedSnapshotCount = 0;
                _lastSnapshotApplyUtc = DateTime.UtcNow;
            }

            ApplySnapshot(snapshot);
        }

        private void RaiseAllSnapshotProperties()
        {
            RaisePropertyChanged(nameof(SharedDefectWallItems));
            RaisePropertyChanged(nameof(SelectedSharedDefectWallItem));
            RaisePropertyChanged(nameof(HasSharedDefectWallItems));
            RaisePropertyChanged(nameof(HasSharedSelectedDefect));
            RaisePropertyChanged(nameof(SharedSelectedDefectPreviewImage));
            RaisePropertyChanged(nameof(SharedSelectedDefectAccentBrush));
            RaisePropertyChanged(nameof(SharedSelectedDefectTitle));
            RaisePropertyChanged(nameof(SharedSelectedDefectSubtitle));
            RaisePropertyChanged(nameof(SharedSelectedDefectDetailLine));
            RaisePropertyChanged(nameof(SharedSelectedDefectFrameLine));
            RaisePropertyChanged(nameof(SharedDefectWallSummaryText));
            RaisePropertyChanged(nameof(SharedDefectWallCurrentPage));
            RaisePropertyChanged(nameof(SharedDefectWallTotalPages));
            RaisePropertyChanged(nameof(SharedDefectWallPageText));
            RaisePropertyChanged(nameof(IsKeepingLatestSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanFirstSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanPreviousSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanNextSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanLastSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanKeepLatestSharedDefectWallPage));
            RaisePropertyChanged(nameof(SharedHistorySummary));
        }

        private void OnSelectSharedDefectWallItem(BandMapWallItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.DefectKey))
                return;

            _stateService.SelectDefect(item.DefectKey);
            ApplyCommandSnapshotImmediately();
        }

        private void OnFirstSharedDefectWallPage()
        {
            _stateService.MoveWallToFirstPage();
            ApplyCommandSnapshotImmediately();
        }

        private void OnPreviousSharedDefectWallPage()
        {
            _stateService.MoveWallPage(-1);
            ApplyCommandSnapshotImmediately();
        }

        private void OnNextSharedDefectWallPage()
        {
            _stateService.MoveWallPage(1);
            ApplyCommandSnapshotImmediately();
        }

        private void OnLastSharedDefectWallPage()
        {
            _stateService.MoveWallToLastPage();
            ApplyCommandSnapshotImmediately();
        }

        private void OnKeepLatestSharedDefectWallPage()
        {
            _stateService.PinWallToLatestPage();
            ApplyCommandSnapshotImmediately();
        }

        private void OnOpenSharedDefectDetail()
        {
            if (SelectedSharedDefectWallItem == null)
                return;

            PrismProvider.DialogService.ShowDialog(nameof(DefectDetailView), new DialogParameters
            {
                { "Title", "缺陷详情" },
                { "Icon", "\ue7ba" },
                { "Param", SelectedSharedDefectWallItem }
            }, _ => { });
        }
    }
}

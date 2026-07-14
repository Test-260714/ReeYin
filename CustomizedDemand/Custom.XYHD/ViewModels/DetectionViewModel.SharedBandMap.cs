using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Custom.XYHD.Models;
using Custom.XYHD.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text;

namespace Custom.XYHD.ViewModels
{
    public partial class DetectionViewModel
    {
        private const int SharedBandMapUiThrottleMilliseconds = 150;

        [NonSerialized]
        private object _sharedBandMapSnapshotSync = new();
        [NonSerialized]
        private BandMapStateSnapshot _pendingSharedBandMapSnapshot;
        [NonSerialized]
        private bool _sharedBandMapApplyQueued;
        [NonSerialized]
        private DateTime _lastSharedBandMapApplyUtc = DateTime.MinValue;

        private object SharedBandMapSnapshotSync => _sharedBandMapSnapshotSync ??= new object();

        public IReadOnlyList<BandMapWallItem> SharedDefectWallItems =>
            _bandMapSnapshot?.WallItems ?? Array.Empty<BandMapWallItem>();

        public IReadOnlyList<BandMapRecentDefectItem> SharedRecentDefects =>
            _bandMapSnapshot?.RecentDefects ?? Array.Empty<BandMapRecentDefectItem>();

        public BandMapWallItem SelectedSharedDefectWallItem => _bandMapSnapshot?.SelectedWallItem;

        public bool HasSharedDefectWallItems => SharedDefectWallItems.Count > 0;
        public bool HasSharedSelectedDefect => SelectedSharedDefectWallItem != null;
        public bool HasSharedRecentDefects => SharedRecentDefects.Count > 0;

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
            ? $"最近帧 {_bandMapSnapshot?.LastFrameIdText ?? "-"}"
            : $"{SelectedSharedDefectWallItem.FrameIdText} | {SelectedSharedDefectWallItem.PathText}";

        public string SharedDefectWallSummaryText =>
            _bandMapSnapshot?.WallSummaryText ?? "最近缺陷 0 条";

        public int SharedDefectWallCurrentPage => _bandMapSnapshot?.WallCurrentPage ?? 1;
        public int SharedDefectWallTotalPages => Math.Max(1, _bandMapSnapshot?.WallTotalPages ?? 1);
        public string SharedDefectWallPageText => $"{SharedDefectWallCurrentPage}/{SharedDefectWallTotalPages}";
        public bool CanPreviousSharedDefectWallPage => SharedDefectWallCurrentPage > 1;
        public bool CanNextSharedDefectWallPage => SharedDefectWallCurrentPage < SharedDefectWallTotalPages;
        public string SharedRangeSummary => _bandMapSnapshot?.RangeSummary ?? "当前累计 0.00 m | 显示窗口 12.0 m | 可视点位 0";
        public double SharedCumulativeMeters => _bandMapSnapshot?.CumulativeMeters ?? 0.0;
        public string SharedLastFrameIdText => _bandMapSnapshot?.LastFrameIdText ?? "-";
        public string SharedPath1Header => _bandMapSnapshot?.Path1Header ?? "路1";
        public string SharedPath2Header => _bandMapSnapshot?.Path2Header ?? "路2";
        public string SharedPath1Result => _bandMapSnapshot?.Path1Result ?? "-";
        public string SharedPath2Result => _bandMapSnapshot?.Path2Result ?? "-";
        public int SharedPath1DefectCount => _bandMapSnapshot?.Path1DefectCount ?? 0;
        public int SharedPath2DefectCount => _bandMapSnapshot?.Path2DefectCount ?? 0;
        public string SharedHistorySummary =>
            $"当前帧 {_bandMapSnapshot?.LastFrameIdText ?? "-"} | 累计 {_bandMapSnapshot?.CumulativeMeters ?? 0.0:F2} m | {SharedDefectWallSummaryText}";

        private IBandMapStateService ResolveBandMapStateService()
        {
            if (_bandMapStateService != null)
                return _bandMapStateService;

            try
            {
                _bandMapStateService = PrismProvider.Container.Resolve(typeof(IBandMapStateService)) as IBandMapStateService;
            }
            catch
            {
                _bandMapStateService = null;
            }

            return _bandMapStateService;
        }

        private void SubscribeBandMapState()
        {
            var stateService = ResolveBandMapStateService();
            if (stateService == null || _bandMapStateSubscribed)
                return;

            _bandMapStateSubscribed = true;
            stateService.SnapshotChanged += OnBandMapSnapshotChanged;
            ResetSharedBandMapSnapshotQueue();
            ApplyBandMapSnapshot(stateService.GetSnapshot());
        }

        private void UnsubscribeBandMapState()
        {
            if (!_bandMapStateSubscribed || _bandMapStateService == null)
                return;

            _bandMapStateService.SnapshotChanged -= OnBandMapSnapshotChanged;
            _bandMapStateSubscribed = false;
            ResetSharedBandMapSnapshotQueue();
        }

        private void OnBandMapSnapshotChanged(BandMapStateSnapshot snapshot)
        {
            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            TimeSpan delay;
            lock (SharedBandMapSnapshotSync)
            {
                _pendingSharedBandMapSnapshot = snapshot;
                if (_sharedBandMapApplyQueued)
                    return;

                _sharedBandMapApplyQueued = true;
                delay = GetSharedBandMapApplyDelayLocked();
            }

            QueueSharedBandMapSnapshotApply(dispatcher, delay);
        }

        private TimeSpan GetSharedBandMapApplyDelayLocked()
        {
            if (_lastSharedBandMapApplyUtc == DateTime.MinValue)
                return TimeSpan.Zero;

            TimeSpan interval = TimeSpan.FromMilliseconds(SharedBandMapUiThrottleMilliseconds);
            TimeSpan elapsed = DateTime.UtcNow - _lastSharedBandMapApplyUtc;
            TimeSpan remaining = interval - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void QueueSharedBandMapSnapshotApply(Dispatcher dispatcher, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                dispatcher.BeginInvoke(new Action(ApplyPendingSharedBandMapSnapshot), DispatcherPriority.Background);
                return;
            }

            var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = delay
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ApplyPendingSharedBandMapSnapshot();
            };
            timer.Start();
        }

        private void ApplyPendingSharedBandMapSnapshot()
        {
            BandMapStateSnapshot snapshot;
            lock (SharedBandMapSnapshotSync)
            {
                snapshot = _pendingSharedBandMapSnapshot;
                _pendingSharedBandMapSnapshot = null;
                _sharedBandMapApplyQueued = false;
                _lastSharedBandMapApplyUtc = DateTime.UtcNow;
            }

            if (!_bandMapStateSubscribed)
                return;

            ApplyBandMapSnapshot(snapshot);
        }

        private void ResetSharedBandMapSnapshotQueue()
        {
            lock (SharedBandMapSnapshotSync)
            {
                _pendingSharedBandMapSnapshot = null;
                _sharedBandMapApplyQueued = false;
                _lastSharedBandMapApplyUtc = DateTime.MinValue;
            }
        }

        private void ApplyBandMapSnapshot(BandMapStateSnapshot snapshot)
        {
            var previousSelectionVersion = _lastBandMapSelectionVersion;
            _bandMapSnapshot = snapshot ?? new BandMapStateSnapshot();
            _lastBandMapSelectionVersion = _bandMapSnapshot.SelectionVersion;

            if (!string.IsNullOrWhiteSpace(_bandMapSnapshot.SelectedDefectKey)
                && _bandMapSnapshot.SelectionVersion > previousSelectionVersion)
            {
                SelectedMainTabIndex = 0;
            }

            RaisePropertyChanged(nameof(SharedDefectWallItems));
            RaisePropertyChanged(nameof(SharedRecentDefects));
            RaisePropertyChanged(nameof(SelectedSharedDefectWallItem));
            RaisePropertyChanged(nameof(HasSharedDefectWallItems));
            RaisePropertyChanged(nameof(HasSharedSelectedDefect));
            RaisePropertyChanged(nameof(HasSharedRecentDefects));
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
            RaisePropertyChanged(nameof(CanPreviousSharedDefectWallPage));
            RaisePropertyChanged(nameof(CanNextSharedDefectWallPage));
            RaisePropertyChanged(nameof(SharedRangeSummary));
            RaisePropertyChanged(nameof(SharedCumulativeMeters));
            RaisePropertyChanged(nameof(SharedLastFrameIdText));
            RaisePropertyChanged(nameof(SharedPath1Header));
            RaisePropertyChanged(nameof(SharedPath2Header));
            RaisePropertyChanged(nameof(SharedPath1Result));
            RaisePropertyChanged(nameof(SharedPath2Result));
            RaisePropertyChanged(nameof(SharedPath1DefectCount));
            RaisePropertyChanged(nameof(SharedPath2DefectCount));
            RaisePropertyChanged(nameof(SharedHistorySummary));
        }

        private void OnSelectSharedDefectWallItem(BandMapWallItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.DefectKey))
                return;

            ResolveBandMapStateService()?.SelectDefect(item.DefectKey);
        }

        private void OnSelectSharedRecentDefect(BandMapRecentDefectItem item)
        {
            if (string.IsNullOrWhiteSpace(item?.DefectKey))
                return;

            ResolveBandMapStateService()?.SelectDefect(item.DefectKey);
        }

        private void OnPreviousSharedDefectWallPage()
        {
            if (!CanPreviousSharedDefectWallPage)
                return;

            ResolveBandMapStateService()?.MoveWallPage(-1);
        }

        private void OnNextSharedDefectWallPage()
        {
            if (!CanNextSharedDefectWallPage)
                return;

            ResolveBandMapStateService()?.MoveWallPage(1);
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

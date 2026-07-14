using Custom.DefectOverview.Models;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Events;
using ALGO.DefectPostProcess.Models;
using HalconDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ReeYin_V.Core.IOC;

namespace Custom.DefectOverview.Services
{
    public interface IBandMapStateService
    {
        event Action<BandMapStateSnapshot> SnapshotChanged;

        BandMapStateSnapshot GetSnapshot();

        void AppendFrame(BandMapFrameInput frame);

        void UpdateSettings(double? frameSpanMillimeters = null, double? windowMeters = null);

        void UpdateSlittingSettings(
            bool? isEnabled = null,
            double? knifeSpacingMillimeters = null,
            double? firstCutOffsetMillimeters = null,
            double? stripWidthMillimeters = null,
            int? slitCount = null);

        void SelectDefect(string defectKey);

        void SetLegendFilter(string legendKey, bool isEnabled);

        void SetViewportStart(double startMeters);

        void UpdateViewportSize(double width, double height);

        void MoveWallPage(int delta);

        void MoveWallToFirstPage();

        void MoveWallToLastPage();

        void PinWallToLatestPage();

        void ChangeBatch();

        DefectBatchReportSyncRequest CreateBrjReportSyncRequest(
            string sn,
            bool isRollCompleted,
            long snapshotVersion,
            TaskCompletionSource<DefectBatchReportSyncResult> completion = null);

        void Reset();
    }

    public sealed partial class BandMapStateService : IBandMapStateService
    {
        private sealed class LegendStyleAssignment
        {
            public string LegendKey { get; init; }
            public string ClassName { get; set; }
            public string MarkerKind { get; init; }
            public Brush Fill { get; init; }
            public Brush Stroke { get; init; }
            public int DisplayOrder { get; init; }
        }

        private sealed class HistoryItem
        {
            public string DefectKey { get; init; }
            public string LegendKey { get; init; }
            public long FrameSequence { get; init; }
            public double XRatio { get; init; }
            public bool OccupiesFullWidth { get; init; }
            public string PathName { get; init; }
            public int ResultIndex { get; init; }
            public int ClassId { get; init; }
            public string ClassName { get; init; }
            public float Confidence { get; init; }
            public string FrameIdText { get; init; }
            public double CenterX { get; init; }
            public double CenterY { get; init; }
            public double Width { get; init; }
            public double Height { get; init; }
            public double Angle { get; init; }
            public int SourceWidth { get; init; }
            public int SourceHeight { get; init; }
            public double PixelEquivalentX { get; init; }
            public double PixelEquivalentY { get; init; }
            public double EdgeCalibrationX { get; init; }
            public bool HasSegmentation { get; init; }
            public string ModelTypeText { get; init; }
            public double? PhysicalXMillimeters { get; init; }
            public double? PhysicalWidthMillimeters { get; init; }
            public double? LaneWidthMillimeters { get; init; }
            public string CoordinateSource { get; init; }
            public ImageSource ThumbnailImage { get; init; }
            public ImageSource PreviewImage { get; init; }
            public string LocalImagePath { get; init; }
        }

        private sealed class RollArchive
        {
            public int BatchNumber { get; init; }
            public string BatchNumberText { get; init; }
            public DateTime BatchStartedLocalTime { get; init; }
            public DateTime? BatchEndedLocalTime { get; init; }
            public long FrameSequence { get; init; }
            public int TotalFrames { get; init; }
            public int OkFrames { get; init; }
            public int NgFrames { get; init; }
            public double FrameSpanMillimeters { get; init; }
            public double CumulativeMeters { get; init; }
            public double CurrentSpeedMetersPerMinute { get; init; }
            public DateTime LastFrameUtc { get; init; }
            public string LastFrameIdText { get; init; }
            public string Path1Header { get; init; }
            public string Path2Header { get; init; }
            public string Path1Result { get; init; }
            public string Path2Result { get; init; }
            public int Path1DefectCount { get; init; }
            public int Path2DefectCount { get; init; }
            public bool ShowPathStatusBadges { get; init; }
            public bool IsCurrent { get; init; }
            public BandMapSlittingSettings SlittingSettings { get; init; } = new();
            public List<HistoryItem> History { get; init; } = [];
        }

        private sealed class SlitEvaluation
        {
            public string PercentText { get; init; }
            public string PhysicalPositionText { get; init; }
            public string SlitText { get; init; }
            public string CombinedPositionText { get; init; }
            public bool IsCrossSlit { get; init; }
        }

        private sealed class PhysicalMetrics
        {
            public double? PhysicalXMillimeters { get; init; }
            public double? PhysicalWidthMillimeters { get; init; }
            public double? LaneWidthMillimeters { get; init; }
            public string CoordinateSource { get; init; }
        }

        private sealed class ResultMapGeometry
        {
            public double CenterX { get; init; }
            public double Width { get; init; }
        }

        private sealed class RenderMetrics
        {
            public double CanvasWidth { get; init; }
            public double CanvasHeight { get; init; }
            public double PlotLeft { get; init; }
            public double PlotTop { get; init; }
            public double PlotWidth { get; init; }
            public double PlotHeight { get; init; }
        }

        private sealed class LocalDefectImageSaveItem
        {
            public BitmapSource Bitmap { get; init; }
            public string FilePath { get; init; }
        }

        private const double DefaultCanvasWidthValue = 1440;
        private const double DefaultCanvasHeightValue = 1120;
        private const double PlotLeftMarginValue = 44;
        private const double PlotTopMarginValue = 8;
        private const double PlotRightMarginValue = 30;
        private const double PlotBottomMarginValue = 78;
        private const int WallPageSize = 16;
        private static readonly int MaxWallPages = DefectOverviewRuntimeOptions.MaxWallPages;
        private static readonly int MaxWallItems = MaxWallPages * WallPageSize;
        private static readonly int MaxHistoryItems = DefectOverviewRuntimeOptions.MaxHistoryItems;
        private static readonly int MaxHistoryImageItems = DefectOverviewRuntimeOptions.MaxHistoryImageItems;
        private const int MaxRecentItems = 24;
        private static readonly int MaxVisibleMapPoints = DefectOverviewRuntimeOptions.MaxVisibleMapPoints;
        private const int RecentNgFrameWindowSize = 20;
        private const int MaxProcessedFrameKeys = 4096;
        private static readonly int AppendFrameSnapshotThrottleMs = DefectOverviewRuntimeOptions.RefreshIntervalMs;
        private static readonly int MaxArchivedRolls = DefectOverviewRuntimeOptions.MaxArchivedRolls;
        private static readonly BlockingCollection<LocalDefectImageSaveItem> LocalDefectImageSaveQueue =
            new(new ConcurrentQueue<LocalDefectImageSaveItem>(), DefectOverviewRuntimeOptions.MaxLocalImageSaveQueue);
        private static int _localDefectImageSaveWorkerStarted;
        private static int _localDefectImageSaveDroppedCount;
        private const string LeftPathDisplayName = "左路";
        private const string RightPathDisplayName = "右路";
        private const string GenericPathDisplayName = "通道";
        private const double DefaultKnifeSpacingMillimeters = 200d;
        private const double DefaultStripWidthMillimeters = 1000d;
        private const int DefaultSlitCount = 4;
        private const int MaxSlitCount = 200;
        private static readonly (string MarkerKind, string FillColor)[] LegendStyleTemplates =
        [
            ("Circle", "#DC2626"),
            ("Triangle", "#EAB308"),
            ("Cross", "#111111"),
            ("Cross", "#FFFFFF")
        ];

        private readonly object _sync = new();
        private readonly List<HistoryItem> _history = [];
        private readonly List<RollArchive> _archivedRolls = [];
        private readonly Queue<bool> _recentFrameResults = new();
        private readonly Queue<string> _processedFrameKeyOrder = new();
        private readonly HashSet<string> _processedFrameKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<string, LegendStyleAssignment> _legendStyles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _legendCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _legendFilters = new(StringComparer.OrdinalIgnoreCase);

        private int _batchNumber = 1;
        private DateTime _batchStartedLocalTime = DateTime.Now;
        private long _frameSequence;
        private int _totalFrames;
        private int _okFrames;
        private int _ngFrames;
        private double _frameSpanMillimeters = 120;
        private double _windowMeters = 12;
        private bool _isSlittingEnabled;
        private double _knifeSpacingMillimeters = DefaultKnifeSpacingMillimeters;
        private double _firstCutOffsetMillimeters;
        private double _stripWidthMillimeters = DefaultStripWidthMillimeters;
        private int _slitCount = DefaultSlitCount;
        private double _currentSpeedMetersPerMinute;
        private DateTime _lastFrameUtc = DateTime.MinValue;
        private string _lastFrameIdText = "-";
        private string _path1Header = LeftPathDisplayName;
        private string _path2Header = RightPathDisplayName;
        private string _path1Result = "-";
        private string _path2Result = "-";
        private int _path1DefectCount;
        private int _path2DefectCount;
        private double? _path1LaneWidthMillimeters;
        private double? _path2LaneWidthMillimeters;
        private bool _showPathStatusBadges;
        private string _selectedDefectKey;
        private long _selectionVersion;
        private int _wallCurrentPage = 1;
        private double _viewportStartMeters;
        private double _viewportCanvasWidth = DefaultCanvasWidthValue;
        private double _viewportCanvasHeight = DefaultCanvasHeightValue;
        private bool _isViewportPinnedToLatest = true;
        private bool _isWallPinnedToLatestPage = true;
        private DateTime _lastAppendSnapshotUtc = DateTime.MinValue;

        public event Action<BandMapStateSnapshot> SnapshotChanged;

        public BandMapStateService()
        {
            LoadBatchState();
            SaveBatchStateLocked();
        }

        private static string BuildBatchNumberText(DateTime batchStartedLocalTime, int batchNumber)
        {
            DateTime safeStartedTime = batchStartedLocalTime == default ? DateTime.Now : batchStartedLocalTime;
            int safeBatchNumber = Math.Max(1, batchNumber);
            return $"{safeStartedTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{safeBatchNumber:D3}";
        }

        private static string BuildLocalDefectImagePath(string batchNumberText, string frameIdText, int defectIndex)
        {
            string basePath = string.IsNullOrWhiteSpace(PrismProvider.AppBasePath)
                ? AppContext.BaseDirectory
                : PrismProvider.AppBasePath;
            string safeBatch = SanitizePathSegment(batchNumberText, "Batch");
            string safeFrame = SanitizePathSegment(frameIdText, "Frame");
            string fileName = $"{safeBatch}_{safeFrame}_{Math.Max(1, defectIndex):D4}.jpg";
            return Path.Combine(basePath, "DefectImages", safeBatch, fileName);
        }

        private static string SanitizePathSegment(string value, string fallback)
        {
            string text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return text.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        }

        private static void QueueLocalDefectImageSave(ImageSource imageSource, string filePath)
        {
            if (imageSource is not BitmapSource bitmap || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                if (!bitmap.IsFrozen && bitmap.CanFreeze)
                    bitmap.Freeze();
            }
            catch
            {
                // SaveLocalDefectImage has its own error log; keep enqueue path non-blocking.
            }

            EnsureLocalDefectImageSaveWorker();
            if (LocalDefectImageSaveQueue.TryAdd(new LocalDefectImageSaveItem { Bitmap = bitmap, FilePath = filePath }))
            {
                return;
            }

            int dropped = Interlocked.Increment(ref _localDefectImageSaveDroppedCount);
            if (dropped == 1 || dropped % 100 == 0)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMapState] Local defect image save queue full, dropped={dropped}, capacity={DefectOverviewRuntimeOptions.MaxLocalImageSaveQueue}");
            }
        }

        private static void EnsureLocalDefectImageSaveWorker()
        {
            if (Interlocked.Exchange(ref _localDefectImageSaveWorkerStarted, 1) == 1)
                return;

            _ = Task.Factory.StartNew(
                ProcessLocalDefectImageSaveQueue,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static void ProcessLocalDefectImageSaveQueue()
        {
            foreach (LocalDefectImageSaveItem item in LocalDefectImageSaveQueue.GetConsumingEnumerable())
            {
                if (item == null)
                    continue;

                SaveLocalDefectImage(item.Bitmap, item.FilePath);
            }
        }

        private static void SaveLocalDefectImage(BitmapSource bitmap, string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using FileStream stream = File.Create(filePath);
                BitmapSource jpegSource = EnsureJpegCompatibleBitmap(bitmap);
                var encoder = new JpegBitmapEncoder
                {
                    QualityLevel = DefectOverviewRuntimeOptions.DefectImageJpegQuality
                };
                encoder.Frames.Add(BitmapFrame.Create(jpegSource));
                encoder.Save(stream);
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMapState] Local defect image save failed: {filePath}, error={ex.Message}");
            }
        }

        private static BitmapSource EnsureJpegCompatibleBitmap(BitmapSource bitmap)
        {
            if (bitmap == null)
                return null;

            if (bitmap.Format == PixelFormats.Bgr24)
                return bitmap;

            FormatConvertedBitmap converted = new(bitmap, PixelFormats.Bgr24, null, 0);
            converted.Freeze();
            return converted;
        }

        public BandMapStateSnapshot GetSnapshot()
        {
            lock (_sync)
            {
                return BuildSnapshotLocked();
            }
        }

        public void AppendFrame(BandMapFrameInput frame)
        {
            if (frame == null)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMapState] AppendFrame start frame={frame.FrameIdText ?? string.Empty}, left={frame.Left?.DefectCount ?? 0}, right={frame.Right?.DefectCount ?? 0}");
            }
            BandMapStateSnapshot snapshot = null;
            bool shouldRaiseSnapshot = false;
            lock (_sync)
            {
                if (!TryRememberFrameKeyLocked(frame.FrameIdText))
                {
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                            $"[BandMapState] AppendFrame skipped duplicate frame={frame.FrameIdText ?? string.Empty}, elapsed={stopwatch.ElapsedMilliseconds}ms");
                    }

                    return;
                }

                DateTime nowUtc = DateTime.UtcNow;
                _frameSequence++;
                _lastFrameIdText = string.IsNullOrWhiteSpace(frame.FrameIdText)
                    ? $"Frame-{_frameSequence:D6}"
                    : frame.FrameIdText;

                bool hasDualPathFrame = frame.Left != null && frame.Right != null;
                _path1Header = ResolvePathHeaderDisplayName(frame.Left, isLeft: true, hasDualPathFrame);
                _path2Header = ResolvePathHeaderDisplayName(frame.Right, isLeft: false, hasDualPathFrame);
                _path1Result = frame.Left?.ResultText ?? "-";
                _path2Result = frame.Right?.ResultText ?? "-";
                _path1DefectCount = frame.Left?.DefectCount ?? 0;
                _path2DefectCount = frame.Right?.DefectCount ?? 0;
                _path1LaneWidthMillimeters = ResolveLaneWidthMillimeters(frame.Left) ?? _path1LaneWidthMillimeters;
                _path2LaneWidthMillimeters = ResolveLaneWidthMillimeters(frame.Right) ?? _path2LaneWidthMillimeters;
                _showPathStatusBadges = hasDualPathFrame;
                UpdateFrameMetricsLocked(nowUtc);
                RecordFrameResultLocked(IsNgFrame(frame));
                _lastFrameUtc = nowUtc;

                AppendPathHistoryLocked(frame.Left, true, hasDualPathFrame, _frameSequence, _lastFrameIdText);
                AppendPathHistoryLocked(frame.Right, false, hasDualPathFrame, _frameSequence, _lastFrameIdText);
                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[BandMapState] AppendFrame history frame={_lastFrameIdText}, seq={_frameSequence}, history={_history.Count}, elapsed={stopwatch.ElapsedMilliseconds}ms");
                }

                if (_history.Count > MaxHistoryItems)
                {
                    RemoveHistoryRangeLocked(0, _history.Count - MaxHistoryItems);
                }

                TrimHistoryImagesLocked();
                if (ShouldBuildAppendSnapshotLocked(nowUtc))
                {
                    snapshot = BuildSnapshotLocked();
                    shouldRaiseSnapshot = true;
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                            $"[BandMapState] AppendFrame snapshot frame={_lastFrameIdText}, history={_history.Count}, elapsed={stopwatch.ElapsedMilliseconds}ms");
                    }
                }
            }

            if (shouldRaiseSnapshot)
                RaiseSnapshotChanged(snapshot, nameof(AppendFrame), stopwatch);
        }

        private bool ShouldBuildAppendSnapshotLocked(DateTime nowUtc)
        {
            if (SnapshotChanged == null)
                return false;

            if (_lastAppendSnapshotUtc == DateTime.MinValue
                || (nowUtc - _lastAppendSnapshotUtc).TotalMilliseconds >= AppendFrameSnapshotThrottleMs)
            {
                _lastAppendSnapshotUtc = nowUtc;
                return true;
            }

            return false;
        }

        private bool TryRememberFrameKeyLocked(string frameIdText)
        {
            if (string.IsNullOrWhiteSpace(frameIdText))
            {
                return true;
            }

            string frameKey = frameIdText.Trim();
            if (!_processedFrameKeys.Add(frameKey))
            {
                return false;
            }

            _processedFrameKeyOrder.Enqueue(frameKey);
            while (_processedFrameKeyOrder.Count > MaxProcessedFrameKeys)
            {
                string oldFrameKey = _processedFrameKeyOrder.Dequeue();
                _processedFrameKeys.Remove(oldFrameKey);
            }

            return true;
        }

        private void TrimHistoryImagesLocked()
        {
            int firstImageIndex = Math.Max(0, _history.Count - MaxHistoryImageItems);
            for (int i = 0; i < firstImageIndex; i++)
            {
                HistoryItem item = _history[i];
                if (item == null || (item.ThumbnailImage == null && item.PreviewImage == null))
                {
                    continue;
                }

                _history[i] = CopyHistoryItemWithoutImages(item);
            }
        }

        private static HistoryItem CopyHistoryItemWithoutImages(HistoryItem item)
        {
            if (item == null)
            {
                return null;
            }

            return new HistoryItem
            {
                DefectKey = item.DefectKey,
                LegendKey = item.LegendKey,
                FrameSequence = item.FrameSequence,
                XRatio = item.XRatio,
                OccupiesFullWidth = item.OccupiesFullWidth,
                PathName = item.PathName,
                ResultIndex = item.ResultIndex,
                ClassId = item.ClassId,
                ClassName = item.ClassName,
                Confidence = item.Confidence,
                FrameIdText = item.FrameIdText,
                CenterX = item.CenterX,
                CenterY = item.CenterY,
                Width = item.Width,
                Height = item.Height,
                Angle = item.Angle,
                SourceWidth = item.SourceWidth,
                SourceHeight = item.SourceHeight,
                PixelEquivalentX = item.PixelEquivalentX,
                PixelEquivalentY = item.PixelEquivalentY,
                EdgeCalibrationX = item.EdgeCalibrationX,
                HasSegmentation = item.HasSegmentation,
                ModelTypeText = item.ModelTypeText,
                PhysicalXMillimeters = item.PhysicalXMillimeters,
                PhysicalWidthMillimeters = item.PhysicalWidthMillimeters,
                LaneWidthMillimeters = item.LaneWidthMillimeters,
                CoordinateSource = item.CoordinateSource,
                LocalImagePath = item.LocalImagePath
            };
        }

        private void RemoveHistoryRangeLocked(int index, int count)
        {
            if (count <= 0)
                return;

            for (int i = index; i < index + count && i < _history.Count; i++)
            {
                DecrementLegendCount(_history[i]?.LegendKey);
            }

            _history.RemoveRange(index, count);
        }

        private void IncrementLegendCount(string legendKey)
        {
            legendKey ??= string.Empty;
            _legendCounts.TryGetValue(legendKey, out int count);
            _legendCounts[legendKey] = count + 1;
        }

        private void DecrementLegendCount(string legendKey)
        {
            legendKey ??= string.Empty;
            if (!_legendCounts.TryGetValue(legendKey, out int count))
                return;

            if (count <= 1)
            {
                _legendCounts.Remove(legendKey);
                return;
            }

            _legendCounts[legendKey] = count - 1;
        }

        private void RaiseSnapshotChanged(BandMapStateSnapshot snapshot, string source, Stopwatch stopwatch)
        {
            Action<BandMapStateSnapshot> handlers = SnapshotChanged;
            if (handlers == null)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMapState] SnapshotChanged skipped source={source}, subscribers=0, elapsed={stopwatch?.ElapsedMilliseconds ?? 0}ms");
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<BandMapStateSnapshot>)handler)(snapshot);
                }
                catch (Exception ex)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[BandMapState] SnapshotChanged subscriber failed source={source}, handler={handler.Method?.DeclaringType?.FullName}.{handler.Method?.Name}, error={ex.Message}");
                }
            }

            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[BandMapState] SnapshotChanged done source={source}, subscribers={handlers.GetInvocationList().Length}, elapsed={stopwatch?.ElapsedMilliseconds ?? 0}ms");
            }
        }

        public void UpdateSettings(double? frameSpanMillimeters = null, double? windowMeters = null)
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                if (frameSpanMillimeters.HasValue)
                {
                    var safeFrameSpan = double.IsFinite(frameSpanMillimeters.Value) && frameSpanMillimeters.Value > 0
                        ? frameSpanMillimeters.Value
                        : 120;
                    _frameSpanMillimeters = safeFrameSpan;
                }

                if (windowMeters.HasValue)
                {
                    var safeWindow = double.IsFinite(windowMeters.Value) && windowMeters.Value >= 1
                        ? windowMeters.Value
                        : 12;
                    _windowMeters = safeWindow;
                }

                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void UpdateSlittingSettings(
            bool? isEnabled = null,
            double? knifeSpacingMillimeters = null,
            double? firstCutOffsetMillimeters = null,
            double? stripWidthMillimeters = null,
            int? slitCount = null)
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                if (isEnabled.HasValue)
                    _isSlittingEnabled = isEnabled.Value;

                if (knifeSpacingMillimeters.HasValue)
                {
                    _knifeSpacingMillimeters = double.IsFinite(knifeSpacingMillimeters.Value) && knifeSpacingMillimeters.Value > 0
                        ? knifeSpacingMillimeters.Value
                        : DefaultKnifeSpacingMillimeters;
                }

                if (firstCutOffsetMillimeters.HasValue)
                {
                    _firstCutOffsetMillimeters = double.IsFinite(firstCutOffsetMillimeters.Value) && firstCutOffsetMillimeters.Value >= 0
                        ? firstCutOffsetMillimeters.Value
                        : 0d;
                }

                if (stripWidthMillimeters.HasValue)
                {
                    _stripWidthMillimeters = double.IsFinite(stripWidthMillimeters.Value) && stripWidthMillimeters.Value > 1
                        ? stripWidthMillimeters.Value
                        : DefaultStripWidthMillimeters;
                }

                if (slitCount.HasValue)
                {
                    _slitCount = Math.Clamp(slitCount.Value, 1, MaxSlitCount);
                }

                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void SelectDefect(string defectKey)
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                _selectedDefectKey = string.IsNullOrWhiteSpace(defectKey) ? null : defectKey;
                if (!string.IsNullOrWhiteSpace(_selectedDefectKey))
                    _isWallPinnedToLatestPage = false;
                _selectionVersion++;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void SetLegendFilter(string legendKey, bool isEnabled)
        {
            if (string.IsNullOrWhiteSpace(legendKey))
                return;

            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                _legendFilters[legendKey] = isEnabled;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void SetViewportStart(double startMeters)
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                double safeFrameSpan = Math.Max(1.0, _frameSpanMillimeters);
                double safeWindow = Math.Max(1.0, _windowMeters);
                double currentMeters = _frameSequence * safeFrameSpan / 1000.0;
                double maxStart = Math.Max(0, currentMeters - safeWindow);
                double safeStart = double.IsFinite(startMeters)
                    ? Math.Clamp(startMeters, 0, maxStart)
                    : maxStart;

                if (Math.Abs(safeStart - _viewportStartMeters) < 0.03
                    && _isViewportPinnedToLatest == (Math.Abs(safeStart - maxStart) < 0.0001))
                {
                    return;
                }

                _viewportStartMeters = safeStart;
                _isViewportPinnedToLatest = Math.Abs(safeStart - maxStart) < 0.0001;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void UpdateViewportSize(double width, double height)
        {
            if (!double.IsFinite(width) || !double.IsFinite(height) || width < 1 || height < 1)
                return;

            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                double safeWidth = Math.Round(width);
                double safeHeight = Math.Round(height);

                if (Math.Abs(safeWidth - _viewportCanvasWidth) < 0.5
                    && Math.Abs(safeHeight - _viewportCanvasHeight) < 0.5)
                {
                    return;
                }

                _viewportCanvasWidth = safeWidth;
                _viewportCanvasHeight = safeHeight;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void MoveWallPage(int delta)
        {
            if (delta == 0)
                return;

            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                int totalPages = GetWallTotalPagesLocked();
                _wallCurrentPage = Math.Clamp(_wallCurrentPage + delta, 1, totalPages);
                _isWallPinnedToLatestPage = false;
                _selectedDefectKey = null;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void MoveWallToFirstPage()
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                _wallCurrentPage = 1;
                _isWallPinnedToLatestPage = false;
                _selectedDefectKey = null;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void MoveWallToLastPage()
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                int totalPages = GetWallTotalPagesLocked();
                _wallCurrentPage = totalPages;
                _isWallPinnedToLatestPage = false;
                _selectedDefectKey = null;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void PinWallToLatestPage()
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                int totalPages = GetWallTotalPagesLocked();
                _wallCurrentPage = totalPages;
                _isWallPinnedToLatestPage = true;
                _selectedDefectKey = null;
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        private int GetWallTotalPagesLocked()
        {
            int wallItemCount = Math.Min(_history.Count, MaxWallItems);
            return Math.Max(1, (int)Math.Ceiling(wallItemCount / (double)WallPageSize));
        }

        public void ChangeBatch()
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                ArchiveCurrentRollLocked(DateTime.Now);
                _batchNumber++;
                ResetRuntimeLocked();
                SaveBatchStateLocked();
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

        public void Reset()
        {
            BandMapStateSnapshot snapshot;
            lock (_sync)
            {
                _archivedRolls.Clear();
                ResetRuntimeLocked();
                SaveBatchStateLocked();
                snapshot = BuildSnapshotLocked();
            }

            SnapshotChanged?.Invoke(snapshot);
        }

    }
}


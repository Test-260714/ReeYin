using Custom.DefectOverview.Models;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.DefectOverview.Services
{
    public interface IDefectOverviewIngestService
    {
        void PublishPath(DefectOverviewPathPacket packet);

        void Reset();
    }

    public sealed class DefectOverviewIngestService : IDefectOverviewIngestService
    {
        private sealed class PendingFrame
        {
            public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

            public string FrameIdText { get; init; } = string.Empty;

            public BandMapPathInput Left { get; set; }

            public BandMapPathInput Right { get; set; }

            public bool IsComplete => Left != null && Right != null;
        }

        private const int PendingFrameTimeoutMs = 5000;
        private const int MaxPendingFrames = 8;
        private const int MaxQueuedPackets = 128;
        private const int PublishPathSlowWarningMs = 80;
        private const int QueueOverflowWarningIntervalMs = 5000;
        private const int XyhdDefaultSourceSuppressMs = 10000;
        private static readonly int ThumbnailMaxDimension = DefectOverviewRuntimeOptions.ThumbnailMaxDimension;
        private const string DefaultPublishSourceName = "DefectOverview";
        private const string XyhdDetectionSourceName = "XYHD_Detection";

        private readonly IBandMapStateService _stateService;
        private readonly IDefectOverviewPostProcessService _postProcessService;
        private readonly object _sync = new();
        private readonly object _queueSync = new();
        private readonly Queue<DefectOverviewPathPacket> _packetQueue = new();
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly Dictionary<string, PendingFrame> _pendingFrames = new();
        private DateTime _lastQueueOverflowWarningUtc = DateTime.MinValue;
        private DateTime _lastXyhdPacketUtc = DateTime.MinValue;

        public DefectOverviewIngestService(
            IBandMapStateService stateService,
            IDefectOverviewPostProcessService postProcessService)
        {
            _stateService = stateService;
            _postProcessService = postProcessService;
            Task.Factory.StartNew(
                ProcessQueuedPackets,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void PublishPath(DefectOverviewPathPacket packet)
        {
            if (packet == null)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            DefectOverviewPathPacket queuedPacket = ClonePacketForQueue(packet);
            if (queuedPacket == null)
                return;

            EnqueuePacket(queuedPacket);
            if (stopwatch.ElapsedMilliseconds >= PublishPathSlowWarningMs)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] PublishPath enqueue slow frame={ResolveFrameIdText(packet)}, elapsed={stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void ProcessQueuedPackets()
        {
            while (true)
            {
                _queueSignal.Wait();

                DefectOverviewPathPacket packet = null;
                lock (_queueSync)
                {
                    if (_packetQueue.Count > 0)
                        packet = _packetQueue.Dequeue();
                }

                if (packet == null)
                    continue;

                try
                {
                    ProcessPathPacket(packet);
                }
                catch (Exception ex)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectOverviewIngest] Process packet failed: {ex.Message}");
                }
                finally
                {
                    DisposePacketImages(packet);
                }
            }
        }

        private void ProcessPathPacket(DefectOverviewPathPacket packet)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string frameIdText = ResolveFrameIdText(packet);
            string sourceName = NormalizeSourceName(packet.SourceName);
            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] PublishPath start frame={frameIdText}, layout={packet.FrameLayout}, role={packet.PathRole}, path={packet.PathName ?? string.Empty}");
            }
            if (ShouldSuppressDefaultPublishSource(sourceName, DateTime.UtcNow))
            {
                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectOverviewIngest] PublishPath skipped frame={frameIdText}, source={sourceName}, role={packet.PathRole}, reason=xyhd-active-default-source, elapsed={stopwatch.ElapsedMilliseconds}ms");
                }
                return;
            }

            BandMapPathInput pathInput = CreatePathInput(packet, frameIdText);
            if (pathInput == null)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] PublishPath skipped frame={frameIdText}, role={packet.PathRole}, reason=pathInput-null, elapsed={stopwatch.ElapsedMilliseconds}ms");
                return;
            }

            if (packet.FrameLayout == DefectOverviewFrameLayout.SinglePath)
            {
                AppendFrameWithTrace(new BandMapFrameInput
                {
                    FrameIdText = frameIdText,
                    Left = pathInput
                }, "single-path", string.Empty, stopwatch);
                return;
            }

            string pendingKey = BuildPendingFrameKey(packet, frameIdText);
            if (string.IsNullOrWhiteSpace(pendingKey))
            {
                AppendFrameWithTrace(new BandMapFrameInput
                {
                    FrameIdText = frameIdText,
                    Left = pathInput
                }, "empty-pending-key", string.Empty, stopwatch);
                return;
            }

            PendingFrame completedFrame = null;
            int pendingCount;
            lock (_sync)
            {
                CleanupExpiredFramesLocked(DateTime.UtcNow);

                if (!_pendingFrames.TryGetValue(pendingKey, out PendingFrame frame))
                {
                    frame = new PendingFrame
                    {
                        FrameIdText = frameIdText
                    };
                    _pendingFrames[pendingKey] = frame;
                }

                AssignPath(frame, packet, pathInput);
                if (frame.IsComplete)
                {
                    completedFrame = frame;
                    _pendingFrames.Remove(pendingKey);
                }
                else
                {
                    TrimPendingFramesLocked();
                }

                pendingCount = _pendingFrames.Count;
            }

            if (completedFrame == null)
            {
            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] PublishPath pending frame={frameIdText}, key={pendingKey}, role={packet.PathRole}, pending={pendingCount}, elapsed={stopwatch.ElapsedMilliseconds}ms");
            }
            return;
        }

            AppendFrameWithTrace(new BandMapFrameInput
            {
                FrameIdText = completedFrame.FrameIdText,
                Left = completedFrame.Left,
                Right = completedFrame.Right
            }, "dual-complete", pendingKey, stopwatch);
        }

        private void EnqueuePacket(DefectOverviewPathPacket packet)
        {
            int dropped = 0;
            lock (_queueSync)
            {
                while (_packetQueue.Count >= MaxQueuedPackets)
                {
                    DefectOverviewPathPacket droppedPacket = _packetQueue.Dequeue();
                    DisposePacketImages(droppedPacket);
                    dropped++;
                }

                _packetQueue.Enqueue(packet);
            }

            if (dropped > 0)
                WarnQueueOverflow(dropped);

            _queueSignal.Release();
        }

        private void WarnQueueOverflow(int dropped)
        {
            DateTime nowUtc = DateTime.UtcNow;
            if ((nowUtc - _lastQueueOverflowWarningUtc).TotalMilliseconds < QueueOverflowWarningIntervalMs)
                return;

            _lastQueueOverflowWarningUtc = nowUtc;
            Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                $"[DefectOverviewIngest] queue overflow, dropped={dropped}, max={MaxQueuedPackets}");
        }

        public void Reset()
        {
            ClearQueuedPackets();
            lock (_sync)
            {
                _pendingFrames.Clear();
                _lastXyhdPacketUtc = DateTime.MinValue;
            }

            _stateService.Reset();
        }

        private void ClearQueuedPackets()
        {
            List<DefectOverviewPathPacket> packets = new();
            lock (_queueSync)
            {
                while (_packetQueue.Count > 0)
                    packets.Add(_packetQueue.Dequeue());
            }

            foreach (DefectOverviewPathPacket packet in packets)
                DisposePacketImages(packet);
        }

        private bool ShouldSuppressDefaultPublishSource(string sourceName, DateTime nowUtc)
        {
            lock (_sync)
            {
                if (string.Equals(sourceName, XyhdDetectionSourceName, StringComparison.Ordinal))
                {
                    _lastXyhdPacketUtc = nowUtc;
                    return false;
                }

                if (!string.Equals(sourceName, DefaultPublishSourceName, StringComparison.Ordinal))
                    return false;

                return _lastXyhdPacketUtc != DateTime.MinValue
                    && (nowUtc - _lastXyhdPacketUtc).TotalMilliseconds <= XyhdDefaultSourceSuppressMs;
            }
        }

        private BandMapPathInput CreatePathInput(DefectOverviewPathPacket packet, string frameIdText)
        {
            bool allowOriginalFallback = !string.Equals(NormalizeSourceName(packet.SourceName), XyhdDetectionSourceName, StringComparison.Ordinal);
            HImage pathImage = packet.PathImage;
            HImage originalImage = allowOriginalFallback ? packet.OriginalImage : null;
                DefectOverviewPathPacket workingPacket = new()
                {
                    SourceName = packet.SourceName,
                    FrameKey = packet.FrameKey,
                    FrameIdText = packet.FrameIdText,
                    CreatedUtc = packet.CreatedUtc,
                    FrameLayout = packet.FrameLayout,
                    PathRole = packet.PathRole,
                    PathName = packet.PathName,
                    PathImage = pathImage,
                    OriginalImage = originalImage,
                    ApplyPostProcess = packet.ApplyPostProcess,
                    SaveLocalDefectImages = packet.SaveLocalDefectImages,
                    IsNg = packet.IsNg,
                    Results = packet.Results,
                    LaneWidth = packet.LaneWidth,
                    PixelEquivalentX = packet.PixelEquivalentX,
                    PixelEquivalentY = packet.PixelEquivalentY,
                    EdgeCalibrationX = packet.EdgeCalibrationX,
                    SchemeFilePath = packet.SchemeFilePath
                };
                List<Result> filteredResults = ResolvePublishedResults(workingPacket);

                (int sourceWidth, int sourceHeight) = ResolveImageSize(pathImage);
                (int originalWidth, int originalHeight) = allowOriginalFallback
                    ? ResolveImageSize(originalImage)
                    : default;
                string pathName = ResolvePathName(packet);
                string resultText = filteredResults.Count > 0 ? "NG" : "OK";
                double laneWidth = ResolveLaneWidth(workingPacket, filteredResults, sourceWidth);
                BitmapSource pathBitmap = null;
                BitmapSource originalBitmap = null;

                if (filteredResults.Count > 0)
                {
                    pathBitmap = DefectPreviewFactory.CreateBitmapFromHImage(pathImage);

                    if ((pathBitmap == null || sourceWidth <= 0 || sourceHeight <= 0) && originalImage != null)
                    {
                        originalBitmap = DefectPreviewFactory.CreateBitmapFromHImage(originalImage);
                        if (originalBitmap != null)
                        {
                            pathBitmap = originalBitmap;
                            sourceWidth = originalWidth > 0 ? originalWidth : originalBitmap.PixelWidth;
                            sourceHeight = originalHeight > 0 ? originalHeight : originalBitmap.PixelHeight;
                        }
                    }
                }

                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectOverviewIngest] Frame={frameIdText}, Path={pathName}, Results={filteredResults.Count}, PathImage={DescribeHImage(pathImage)}, OriginalImage={DescribeHImage(originalImage)}, PathBitmap={DescribeBitmap(pathBitmap)}, OriginalBitmap={DescribeBitmap(originalBitmap)}, LaneWidth={laneWidth:F2}");
                }

                return new BandMapPathInput
                {
                    PathName = pathName,
                    ResultText = resultText,
                    DefectCount = filteredResults.Count,
                    LaneWidth = laneWidth,
                    PixelEquivalentX = packet.PixelEquivalentX ?? 0d,
                    PixelEquivalentY = packet.PixelEquivalentY ?? 0d,
                    EdgeCalibrationX = packet.EdgeCalibrationX ?? 0d,
                    OccupiesFullWidth = packet.FrameLayout == DefectOverviewFrameLayout.SinglePath,
                    SaveLocalDefectImages = packet.SaveLocalDefectImages,
                    Results = filteredResults,
                    Defects = filteredResults.Count > 0
                        ? CreateDefectSeeds(
                            pathName,
                            frameIdText,
                            pathImage,
                            pathBitmap,
                            sourceWidth,
                            sourceHeight,
                            originalImage,
                            originalBitmap,
                            originalWidth,
                            originalHeight,
                            filteredResults)
                        : Array.Empty<BandMapDefectSeed>()
                };
        }

        private List<Result> ResolvePublishedResults(DefectOverviewPathPacket packet)
        {
            if (packet == null)
                return new List<Result>();

            if (packet.ApplyPostProcess)
            {
                return _postProcessService.FilterResults(packet)?
                    .Where(item => item != null)
                    .ToList() ?? new List<Result>();
            }

            return packet.Results?
                .Where(item => item != null)
                .ToList() ?? new List<Result>();
        }

        private void AssignPath(PendingFrame frame, DefectOverviewPathPacket packet, BandMapPathInput pathInput)
        {
            if (IsLeftPath(packet))
            {
                frame.Left = pathInput;
                return;
            }

            if (IsRightPath(packet))
            {
                frame.Right = pathInput;
                return;
            }

            if (frame.Left == null)
            {
                frame.Left = pathInput;
                return;
            }

            frame.Right = pathInput;
        }

        private void AppendFrameWithTrace(BandMapFrameInput frame, string reason, string pendingKey, Stopwatch stopwatch)
        {
            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] AppendFrame start reason={reason}, key={pendingKey}, frame={frame?.FrameIdText ?? string.Empty}, left={frame?.Left?.DefectCount ?? 0}, right={frame?.Right?.DefectCount ?? 0}, elapsed={stopwatch.ElapsedMilliseconds}ms");
            }

            _stateService.AppendFrame(frame);

            if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] AppendFrame done reason={reason}, key={pendingKey}, frame={frame?.FrameIdText ?? string.Empty}, total={stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void CleanupExpiredFramesLocked(DateTime nowUtc)
        {
            List<string> expiredKeys = _pendingFrames
                .Where(item => (nowUtc - item.Value.CreatedUtc).TotalMilliseconds > PendingFrameTimeoutMs)
                .Select(item => item.Key)
                .ToList();

            foreach (string key in expiredKeys)
                _pendingFrames.Remove(key);
        }

        private void TrimPendingFramesLocked()
        {
            int overflow = _pendingFrames.Count - MaxPendingFrames;
            if (overflow <= 0)
                return;

            List<string> overflowKeys = _pendingFrames
                .OrderBy(item => item.Value.CreatedUtc)
                .Take(overflow)
                .Select(item => item.Key)
                .ToList();

            foreach (string key in overflowKeys)
                _pendingFrames.Remove(key);
        }

        private static string BuildPendingFrameKey(DefectOverviewPathPacket packet, string frameIdText)
        {
            string frameKey = packet.FrameKey;
            if (string.IsNullOrWhiteSpace(frameKey))
                frameKey = frameIdText;

            if (string.IsNullOrWhiteSpace(frameKey))
                return string.Empty;

            string sourceName = NormalizeSourceName(packet.SourceName);
            return $"{sourceName}|{frameKey}";
        }

        private static string NormalizeSourceName(string sourceName)
        {
            return string.IsNullOrWhiteSpace(sourceName) ? DefaultPublishSourceName : sourceName;
        }

        private static string ResolveFrameIdText(DefectOverviewPathPacket packet)
        {
            if (!string.IsNullOrWhiteSpace(packet.FrameIdText))
                return packet.FrameIdText;

            if (!string.IsNullOrWhiteSpace(packet.FrameKey))
                return packet.FrameKey;

            DateTime createdUtc = packet.CreatedUtc == default ? DateTime.UtcNow : packet.CreatedUtc;
            return createdUtc.ToLocalTime().ToString("yyyyMMdd-HHmmssfff");
        }

        private static string ResolvePathName(DefectOverviewPathPacket packet)
        {
            if (!string.IsNullOrWhiteSpace(packet.PathName))
                return packet.PathName;

            if (packet.FrameLayout != DefectOverviewFrameLayout.DualPath)
                return string.Empty;

            return packet.PathRole switch
            {
                DefectOverviewPathRole.Left => "左路",
                DefectOverviewPathRole.Right => "右路",
                _ => "通道"
            };
        }

        private static bool IsLeftPath(DefectOverviewPathPacket packet)
        {
            if (packet.PathRole == DefectOverviewPathRole.Left)
                return true;

            string pathName = packet.PathName ?? string.Empty;
            return pathName.Contains("左", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("left", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("path1", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("lane1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRightPath(DefectOverviewPathPacket packet)
        {
            if (packet.PathRole == DefectOverviewPathRole.Right)
                return true;

            string pathName = packet.PathName ?? string.Empty;
            return pathName.Contains("右", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("right", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("path2", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("lane2", StringComparison.OrdinalIgnoreCase);
        }

        private static (int width, int height) ResolveImageSize(HalconDotNet.HImage image)
        {
            try
            {
                if (image != null && image.IsInitialized())
                {
                    image.GetImageSize(out int width, out int height);
                    return (width, height);
                }
            }
            catch
            {
            }

            return (0, 0);
        }

        private static HImage CopyImageSafe(HImage image)
        {
            try
            {
                return image != null && image.IsInitialized()
                    ? image.CopyImage()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static DefectOverviewPathPacket ClonePacketForQueue(DefectOverviewPathPacket packet)
        {
            if (packet == null)
                return null;

            List<Result> results = packet.Results?
                .Where(item => item != null)
                .ToList() ?? new List<Result>();
            bool retainImages = results.Count > 0;
            HImage pathImage = retainImages ? CopyImageSafe(packet.PathImage) : null;
            HImage originalImage = retainImages && !ReferenceEquals(packet.OriginalImage, packet.PathImage)
                ? CopyImageSafe(packet.OriginalImage)
                : null;

            return new DefectOverviewPathPacket
            {
                SourceName = packet.SourceName,
                FrameKey = packet.FrameKey,
                FrameIdText = packet.FrameIdText,
                CreatedUtc = packet.CreatedUtc,
                FrameLayout = packet.FrameLayout,
                PathRole = packet.PathRole,
                PathName = packet.PathName,
                PathImage = pathImage,
                OriginalImage = originalImage,
                ApplyPostProcess = packet.ApplyPostProcess,
                SaveLocalDefectImages = packet.SaveLocalDefectImages,
                IsNg = packet.IsNg,
                Results = results,
                LaneWidth = packet.LaneWidth,
                PixelEquivalentX = packet.PixelEquivalentX,
                PixelEquivalentY = packet.PixelEquivalentY,
                EdgeCalibrationX = packet.EdgeCalibrationX,
                SchemeFilePath = packet.SchemeFilePath
            };
        }

        private static void DisposePacketImages(DefectOverviewPathPacket packet)
        {
            if (packet == null)
                return;

            try
            {
                packet.PathImage?.Dispose();
            }
            catch
            {
            }

            try
            {
                packet.OriginalImage?.Dispose();
            }
            catch
            {
            }
        }

        private static double ResolveLaneWidth(DefectOverviewPathPacket packet, List<Result> results, int sourceWidth)
        {
            if (packet.LaneWidth.HasValue && packet.LaneWidth.Value > 1)
                return packet.LaneWidth.Value;

            if (sourceWidth > 1)
                return sourceWidth;

            double fallback = results.Count == 0
                ? 1.0
                : results.Max(item => item.Cx + Math.Max(1.0, item.Width / 2.0));

            return Math.Max(1.0, fallback);
        }

        private static List<BandMapDefectSeed> CreateDefectSeeds(
            string pathName,
            string frameIdText,
            HImage pathImage,
            BitmapSource pathBitmap,
            int sourceWidth,
            int sourceHeight,
            HImage originalImage,
            BitmapSource originalBitmap,
            int originalWidth,
            int originalHeight,
            IReadOnlyList<Result> results)
        {
            if (results == null || results.Count == 0)
                return [];

            List<BandMapDefectSeed> seeds = new(results.Count);
            Dictionary<BitmapSource, DefectPreviewFactory.PreviewPixelCache> pixelCaches = new(ReferenceEqualityComparer.Instance);
            for (int index = 0; index < results.Count; index++)
            {
                Result result = results[index];
                if (result == null)
                    continue;

                Result previewResult = CreatePreviewResult(result);
                BitmapSource defectImage = TryCreateFixedPreview(pathImage, sourceWidth, sourceHeight, previewResult);
                string imageSource = defectImage != null ? "path-himage" : "none";

                if (defectImage == null && originalImage != null && !ReferenceEquals(originalImage, pathImage))
                {
                    defectImage = TryCreateFixedPreview(originalImage, originalWidth, originalHeight, previewResult);
                    imageSource = defectImage != null ? "original-himage" : imageSource;
                }

                if (defectImage == null)
                {
                    defectImage = TryCreatePreview(pathBitmap, sourceWidth, sourceHeight, previewResult, 2.4, 1.0, pixelCaches);
                    imageSource = defectImage != null ? "path-bitmap-fallback" : imageSource;
                }

                if (defectImage == null && originalBitmap != null && !ReferenceEquals(originalBitmap, pathBitmap))
                {
                    defectImage = TryCreatePreview(originalBitmap, originalWidth, originalHeight, previewResult, 2.4, 1.0, pixelCaches);
                    imageSource = defectImage != null ? "original-bitmap-fallback" : imageSource;
                }
                string defectKey = BuildDefectKey(frameIdText, pathName, result, index);

                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectOverviewIngest] Seed[{index}] key={defectKey}, class={DescribeResult(result)}, pathBitmap={DescribeBitmap(pathBitmap)}, originalBitmap={DescribeBitmap(originalBitmap)}, defectImage={DescribeBitmap(defectImage)}, imageSource={imageSource}, geom={DescribePreviewGeometry(previewResult)}");
                }

                seeds.Add(new BandMapDefectSeed
                {
                    DefectKey = defectKey,
                    ResultIndex = index,
                    SourceWidth = sourceWidth,
                    SourceHeight = sourceHeight,
                    CenterX = result.Cx,
                    CenterY = result.Cy,
                    Width = result.Width,
                    Height = result.Height,
                    Angle = result.Angle,
                    HasSegmentation = HasInitializedSegmentation(result),
                    ModelTypeText = result.ModelType.ToString(),
                    ThumbnailImage = defectImage,
                    PreviewImage = defectImage
                });
            }

            return seeds;
        }

        private static BitmapSource TryCreateFixedPreview(
            HImage image,
            int sourceWidth,
            int sourceHeight,
            Result previewResult)
        {
            if (image == null || previewResult == null)
                return null;

            return DefectPreviewFactory.CreateFixedDefectPreviewBitmapFromHImage(
                image,
                sourceWidth,
                sourceHeight,
                previewResult);
        }

        private static BitmapSource TryCreatePreview(
            BitmapSource bitmap,
            int sourceWidth,
            int sourceHeight,
            Result previewResult,
            double paddingScale,
            double targetAspectRatio,
            Dictionary<BitmapSource, DefectPreviewFactory.PreviewPixelCache> pixelCaches)
        {
            if (previewResult == null)
                return null;

            int safeWidth = sourceWidth > 0
                ? sourceWidth
                : bitmap?.PixelWidth ?? 0;
            int safeHeight = sourceHeight > 0
                ? sourceHeight
                : bitmap?.PixelHeight ?? 0;
            return DefectPreviewFactory.CreateDefectPreviewBitmapCached(
                bitmap,
                safeWidth,
                safeHeight,
                previewResult,
                paddingScale,
                targetAspectRatio,
                item => ResolvePreviewPixelCache(item, pixelCaches));
        }

        private static DefectPreviewFactory.PreviewPixelCache ResolvePreviewPixelCache(
            BitmapSource bitmap,
            Dictionary<BitmapSource, DefectPreviewFactory.PreviewPixelCache> pixelCaches)
        {
            if (bitmap == null || pixelCaches == null)
                return null;

            if (pixelCaches.TryGetValue(bitmap, out DefectPreviewFactory.PreviewPixelCache cache))
                return cache;

            cache = DefectPreviewFactory.CreatePixelCache(bitmap);
            if (cache != null)
                pixelCaches[bitmap] = cache;

            return cache;
        }

        private static Result CreatePreviewResult(Result displayResult)
        {
            if (displayResult == null)
                return null;

            Result previewResult = new()
            {
                Cx = displayResult.Cx,
                Cy = displayResult.Cy,
                Width = displayResult.Width,
                Height = displayResult.Height,
                Angle = displayResult.Angle,
                Confidence = displayResult.Confidence,
                ClassId = displayResult.ClassId,
                ClassName = displayResult.ClassName,
                Kpt = displayResult.Kpt ?? new Keypoints(),
                Seg = DefectOverviewRuntimeOptions.UseSegmentationGeometry ? displayResult.Seg : null,
                ModelType = displayResult.ModelType,
                Others = new Dictionary<string, object>()
            };

            CopyMetadata(previewResult.Others, displayResult.Others);
            return previewResult;
        }

        private static void CopyMetadata(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            if (target == null || source == null)
                return;

            foreach (KeyValuePair<string, object> pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                target[pair.Key] = pair.Value;
            }
        }

        private static BitmapSource CreateThumbnailBitmap(BitmapSource source)
        {
            if (source == null)
                return null;

            try
            {
                BitmapSource thumbnailSource = source;
                double maxDimension = Math.Max(thumbnailSource.PixelWidth, thumbnailSource.PixelHeight);
                if (maxDimension <= 0)
                    return null;

                double scale = Math.Min(1.0, (double)ThumbnailMaxDimension / maxDimension);
                if (!thumbnailSource.Format.Equals(PixelFormats.Bgra32))
                {
                    FormatConvertedBitmap converted = new(thumbnailSource, PixelFormats.Bgra32, null, 0);
                    converted.Freeze();
                    thumbnailSource = converted;
                }

                if (scale < 1.0)
                {
                    TransformedBitmap scaled = new(thumbnailSource, new ScaleTransform(scale, scale));
                    scaled.Freeze();
                    thumbnailSource = scaled;
                }

                int width = Math.Max(1, thumbnailSource.PixelWidth);
                int height = Math.Max(1, thumbnailSource.PixelHeight);
                int stride = width * 4;
                byte[] pixels = new byte[stride * height];
                thumbnailSource.CopyPixels(pixels, stride, 0);

                BitmapSource thumbnail = BitmapSource.Create(width, height, 96.0, 96.0, PixelFormats.Bgra32, null, pixels, stride);
                thumbnail.Freeze();
                return thumbnail;
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewIngest] Thumbnail exception source={DescribeBitmap(source)}: {ex.Message}");
                return null;
            }
        }

        private static bool HasInitializedSegmentation(Result result)
        {
            try
            {
                return result?.Seg != null && result.Seg.IsInitialized();
            }
            catch
            {
                return false;
            }
        }

        private static string DescribeBitmap(BitmapSource bitmap)
        {
            if (bitmap == null)
                return "null";

            return $"{bitmap.PixelWidth}x{bitmap.PixelHeight}, format={bitmap.Format}";
        }

        private static string DescribeHImage(HalconDotNet.HImage image)
        {
            try
            {
                if (image == null)
                    return "null";

                if (!image.IsInitialized())
                    return "not-initialized";

                image.GetImageSize(out int width, out int height);
                return $"{width}x{height}, ch={image.CountChannels()}, obj={image.CountObj()}";
            }
            catch (Exception ex)
            {
                return $"invalid:{ex.GetType().Name}";
            }
        }

        private static string DescribeResult(Result result)
        {
            if (result == null)
                return "null";

            string className = string.IsNullOrWhiteSpace(result.ClassName) ? "-" : result.ClassName;
            return $"{className}#{result.ClassId}";
        }

        private static string DescribePreviewGeometry(Result result)
        {
            if (result == null)
                return "null";

            string rawGeometry = $"raw=(cx={result.Cx:F1}, cy={result.Cy:F1}, w={result.Width:F1}, h={result.Height:F1}, angle={result.Angle:F4})";
            if (!TryResolveDisplayPreviewGeometry(result, out double displayCx, out double displayCy, out double displayWidth, out double displayHeight, out double displayAngle))
                return $"{rawGeometry}, modelType={(int)result.ModelType}";

            return $"{rawGeometry}, display=(cx={displayCx:F1}, cy={displayCy:F1}, w={displayWidth:F1}, h={displayHeight:F1}, angle={displayAngle:F4}), modelType={(int)result.ModelType}";
        }

        private static bool TryResolveDisplayPreviewGeometry(
            Result result,
            out double centerX,
            out double centerY,
            out double width,
            out double height,
            out double angle)
        {
            centerX = 0d;
            centerY = 0d;
            width = 0d;
            height = 0d;
            angle = 0d;

            if (result?.Others == null)
                return false;

            bool hasCenterX = TryResolveMetadataDouble(result, DefectPreviewFactory.DisplayCenterXKey, out centerX);
            bool hasCenterY = TryResolveMetadataDouble(result, DefectPreviewFactory.DisplayCenterYKey, out centerY);
            bool hasWidth = TryResolveMetadataDouble(result, DefectPreviewFactory.DisplayPixelWidthKey, out width) && width > 0d;
            bool hasHeight = TryResolveMetadataDouble(result, DefectPreviewFactory.DisplayPixelHeightKey, out height) && height > 0d;
            TryResolveMetadataDouble(result, DefectPreviewFactory.DisplayAngleKey, out angle);
            return hasCenterX && hasCenterY && hasWidth && hasHeight;
        }

        private static bool TryResolveMetadataDouble(Result result, string key, out double value)
        {
            value = 0d;
            if (result?.Others == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            try
            {
                value = Convert.ToDouble(rawValue, System.Globalization.CultureInfo.InvariantCulture);
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
            catch
            {
                value = 0d;
                return false;
            }
        }

        private static string BuildDefectKey(string frameIdText, string pathName, Result result, int index)
        {
            if (result == null)
                return $"{frameIdText}|{pathName}|{index}";

            return $"{frameIdText}|{pathName}|{index}|{result.ClassId}|{result.Cx:F1}|{result.Cy:F1}|{result.Width:F1}|{result.Height:F1}";
        }
    }
}

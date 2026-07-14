using Custom.DefectOverview.Models;
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
        private const int PathImagePreviewThrottleMilliseconds = 250;

        [NonSerialized]
        private object _pathImagePreviewThrottleSync = new();
        [NonSerialized]
        private DateTime _lastLeftPathImagePreviewUtc = DateTime.MinValue;
        [NonSerialized]
        private DateTime _lastRightPathImagePreviewUtc = DateTime.MinValue;

        private object PathImagePreviewThrottleSync => _pathImagePreviewThrottleSync ??= new object();

        private void SubscribeEvents()
        {
            if (_resultToken != null)
                return;

            _resultToken = PrismProvider.EventAggregator
                .GetEvent<OutputResultEvent>()
                .Subscribe(OnResultReceived, ThreadOption.PublisherThread);
            Model.AddLog("已订阅 XYHD_Detection 结果事件", "INFO");
        }

        private void UnsubscribeEvents()
        {
            if (_resultToken == null)
                return;

            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Unsubscribe(_resultToken);
            _resultToken = null;
            ClearPendingFrames();
            ClearFrameIdTextCache();
        }

        private void OnResultReceived((string source, object data) result)
        {
            if (result.source != "XYHD_Detection")
                return;

            HImage pathImageCopy = null;
            bool pathImageStoredInPending = false;
            PendingFrameUpdate frameToCommit = null;

            try
            {
                var sw = Stopwatch.StartNew();
                long tParse = 0;
                long tMainImage = 0;
                long tPathDetails = 0;
                long tPathImage = 0;
                long tFrameCommit = 0;

                if (result.data is not XYHDInputPacket inputPacket)
                {
                    Model.AddLog($"收到事件但数据类型不匹配: {result.data?.GetType()?.Name ?? "null"}", "WARN");
                    return;
                }

                var frameId = inputPacket.FrameId > 0 ? inputPacket.FrameId : DateTime.UtcNow.Ticks;
                var frameIdText = ResolveFrameIdText(inputPacket, frameId);
                var frameKey = BuildFrameKey(frameId, frameIdText);

                // 详细的接收日志
                Model.AddLog($"收到XYHD数据包: PathName={inputPacket.PathName ?? "null"}, " +
                    $"OriginalImage={(inputPacket.OriginalImage != null ? "Provided" : "Null")}, " +
                    $"PathImage={(inputPacket.PathImage != null ? "Provided" : "Null")}, " +
                    $"Serial={inputPacket.SourceSerial}, FrameId={frameIdText}", "DEBUG");

                var parseStart = sw.ElapsedMilliseconds;
                if (!TryParseInputPacket(inputPacket, out _, out var isNG, out var results))
                {
                    Model.AddLog("TryParseInputPacket 返回 false", "WARN");
                    return;
                }
                tParse = sw.ElapsedMilliseconds - parseStart;

                Model.AddLog($"解析结果: isNG={isNG}, 缺陷数={results.Count}, " +
                    $"image={(inputPacket.PathImage != null && inputPacket.PathImage.IsInitialized() ? "OK" : "Null")}", "DEBUG");

                int sourceSerial = inputPacket.SourceSerial;
                string originalPathName = inputPacket.PathName;
                var orientation = XYHDFieldOrientationMapper.Resolve(
                    originalPathName,
                    XYHDFieldOrientationSettings.FromModel(Model));
                string pathName = orientation.FieldPathName;
                double laneWidth = ResolvePathLaneWidth(inputPacket.PathImage, results);
                var pathResults = XYHDFieldOrientationMapper.ApplyToResults(results, laneWidth, orientation);
                bool shouldRefreshPathImage = ShouldRefreshPathImagePreview(orientation.FieldRole);
                if (shouldRefreshPathImage)
                    pathImageCopy = CopyImageSafe(inputPacket.PathImage);
                var pieceStats = ResolveAlgorithmPieceStats(inputPacket, pathResults, isNG);
                bool hasIsOkList = TryExtractIsOkList(inputPacket.IsOks, out var isOkList);
                int estimatedNgByImageIndex = EstimateNgPieceCountByResultImageIndex(pathResults);
                string pieceStatSource = hasIsOkList && isOkList.Count > 0
                    ? "IsOks"
                    : pathResults.Count > 0
                        ? "DefectsOnlyNoPieceGroup"
                        : "Empty";
                Model.AddLog(
                    $"计片诊断: Frame={frameIdText}, Path={originalPathName ?? "-"}->{pathName ?? "-"}, Serial={sourceSerial}, " +
                    $"IsOksType={inputPacket.IsOks?.GetType().FullName ?? "null"}, IsOksCount={(hasIsOkList ? isOkList.Count : -1)}, " +
                    $"IsOks=[{FormatBoolListForLog(isOkList)}], Defects={pathResults.Count}, EstimateNg={estimatedNgByImageIndex}, " +
                    $"Source={pieceStatSource}, Total={pieceStats.total}, Ng={pieceStats.ng}, PathIsNg={isNG}",
                    "DEBUG");
                List<string> droppedPendingFrameKeys = null;
                lock (_pendingFrameLock)
                {
                    if (!_pendingFrames.TryGetValue(frameKey, out var pending))
                    {
                        pending = new PendingFrameUpdate
                        {
                            FrameId = frameId,
                            FrameIdText = frameIdText
                        };
                        _pendingFrames[frameKey] = pending;
                    }

                    var pathUpdate = new PendingPathUpdate
                    {
                        PathName = pathName,
                        Serial = sourceSerial,
                        LaneWidth = laneWidth,
                        PathImage = null,
                        OriginalImage = null,
                        IsNG = isNG,
                        PieceCount = pieceStats.total,
                        NgPieceCount = pieceStats.ng,
                        Results = pathResults
                    };

                    if (orientation.FieldRole == DefectOverviewPathRole.Left)
                    {
                        DisposePendingPathImages(pending.Left);
                        pending.Left = pathUpdate;
                    }
                    else if (orientation.FieldRole == DefectOverviewPathRole.Right)
                    {
                        DisposePendingPathImages(pending.Right);
                        pending.Right = pathUpdate;
                    }

                    if (orientation.FieldRole != DefectOverviewPathRole.Left
                        && orientation.FieldRole != DefectOverviewPathRole.Right)
                    {
                        DisposePendingPathImages(pathUpdate);
                        pathImageCopy = null;
                    }

                    if (!pending.MainImageUpdated && pathImageCopy != null && pathImageCopy.IsInitialized())
                        pending.MainImageUpdated = true;

                    if (!pending.Committed && pending.IsComplete)
                    {
                        pending.Committed = true;
                        frameToCommit = pending;
                        _pendingFrames.Remove(frameKey);
                    }
                    else
                    {
                        droppedPendingFrameKeys = TrimPendingFramesLocked();
                    }
                }

                // 主图一帧只刷新一次，避免左右包重复刷新整个页面
                var mainImageStart = sw.ElapsedMilliseconds;
                if (droppedPendingFrameKeys != null)
                {
                    foreach (string key in droppedPendingFrameKeys)
                        Model.AddLog($"Pending frame limit reached, dropped oldest incomplete frame: {key}", "WARN");
                }

                tMainImage = sw.ElapsedMilliseconds - mainImageStart;

                // 左右路独立更新，只影响各自区域
                var pathDetailStart = sw.ElapsedMilliseconds;
                UpdatePathDetails(pathName, sourceSerial, isNG, pieceStats.total, pathResults);
                if (shouldRefreshPathImage)
                    UpdatePathImage(pathName, pathImageCopy, isNG, pieceStats.total, pathResults);
                tPathDetails = sw.ElapsedMilliseconds - pathDetailStart;

                tPathImage = 0;

                // 左右两路都到齐后，再提交整帧统计/批次，避免一帧重复计数
                var frameCommitStart = sw.ElapsedMilliseconds;
                if (frameToCommit != null)
                {
                    try
                    {
                        CommitCompletedFrame(frameToCommit);
                    }
                    finally
                    {
                        DisposePendingFrameImages(frameToCommit);
                        frameToCommit = null;
                    }
                }
                tFrameCommit = sw.ElapsedMilliseconds - frameCommitStart;

                Model.AddLog(
                    $"XYHD收包耗时: Path={originalPathName ?? "-"}->{pathName ?? "-"}, Serial={sourceSerial}, FrameId={frameIdText}, " +
                    $"计片={pieceStats.total}, NG片={pieceStats.ng}, 解析={tParse}ms 主图={tMainImage}ms 明细={tPathDetails}ms 路图={tPathImage}ms 帧提交={tFrameCommit}ms 总计={sw.ElapsedMilliseconds}ms",
                    "DEBUG");
            }
            catch (Exception ex)
            {
                Model.AddLog($"处理异常: {ex.Message}\n{ex.StackTrace}", "ERROR");
            }
            finally
            {
                if (frameToCommit != null)
                    DisposePendingFrameImages(frameToCommit);
                else if (!pathImageStoredInPending)
                    DisposeImageSafe(pathImageCopy);
            }
        }

        private bool ShouldRefreshPathImagePreview(DefectOverviewPathRole role)
        {
            DateTime nowUtc = DateTime.UtcNow;
            lock (PathImagePreviewThrottleSync)
            {
                if (role == DefectOverviewPathRole.Right)
                {
                    if (_lastRightPathImagePreviewUtc != DateTime.MinValue
                        && (nowUtc - _lastRightPathImagePreviewUtc).TotalMilliseconds < PathImagePreviewThrottleMilliseconds)
                    {
                        return false;
                    }

                    _lastRightPathImagePreviewUtc = nowUtc;
                    return true;
                }

                if (_lastLeftPathImagePreviewUtc != DateTime.MinValue
                    && (nowUtc - _lastLeftPathImagePreviewUtc).TotalMilliseconds < PathImagePreviewThrottleMilliseconds)
                {
                    return false;
                }

                _lastLeftPathImagePreviewUtc = nowUtc;
                return true;
            }
        }

        private void ResetPathImagePreviewThrottle()
        {
            lock (PathImagePreviewThrottleSync)
            {
                _lastLeftPathImagePreviewUtc = DateTime.MinValue;
                _lastRightPathImagePreviewUtc = DateTime.MinValue;
            }
        }

        private string ResolveFrameIdText(XYHDInputPacket inputPacket, long frameId)
        {
            if (!string.IsNullOrWhiteSpace(inputPacket.FrameIdText))
                return inputPacket.FrameIdText;

            if (frameId > 0)
                return GetOrCreateFrameIdText(frameId);

            return GenerateFrameIdText();
        }

        private static string BuildFrameKey(long frameId, string frameIdText)
        {
            if (frameId > 0)
                return $"ID:{frameId}";

            return $"TXT:{frameIdText}";
        }

        private static double ResolvePathLaneWidth(
            HImage pathImage,
            IReadOnlyList<ReeYin_V.Core.DeepLearning.Result> results)
        {
            try
            {
                if (pathImage != null && pathImage.IsInitialized())
                {
                    pathImage.GetImageSize(out int width, out _);
                    if (width > 1)
                        return width;
                }
            }
            catch
            {
            }

            double fallback = results == null || results.Count == 0
                ? 1.0
                : results.Where(item => item != null)
                    .Select(item => item.Cx + Math.Max(1.0, item.Width / 2.0))
                    .DefaultIfEmpty(1.0)
                    .Max();

            return Math.Max(1.0, fallback);
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

        private static void DisposePendingFrameImages(PendingFrameUpdate frame)
        {
            if (frame == null)
                return;

            DisposePendingPathImages(frame.Left);
            DisposePendingPathImages(frame.Right);
            frame.Left = null;
            frame.Right = null;
        }

        private static void DisposePendingPathImages(PendingPathUpdate path)
        {
            if (path == null)
                return;

            DisposeImageSafe(path.PathImage);
            DisposeImageSafe(path.OriginalImage);
        }

        private static void DisposeImageSafe(HImage image)
        {
            try
            {
                image?.Dispose();
            }
            catch
            {
            }
        }

        private void UpdatePathPieceTotals(PendingFrameUpdate frame, int totalPieces, int ngPieces)
        {
            if (frame == null)
                return;

            int leftPieces = Math.Max(0, frame.Left?.PieceCount ?? 0);
            int leftNgPieces = Math.Min(Math.Max(0, frame.Left?.NgPieceCount ?? 0), leftPieces);
            int rightPieces = Math.Max(0, frame.Right?.PieceCount ?? 0);
            int rightNgPieces = Math.Min(Math.Max(0, frame.Right?.NgPieceCount ?? 0), rightPieces);
            int safeTotalPieces = Math.Max(0, totalPieces);
            int safeNgPieces = Math.Min(Math.Max(0, ngPieces), safeTotalPieces);

            RunOnUiThread(() =>
            {
                Path1PieceCount += leftPieces;
                Path1NgPieceCount += leftNgPieces;
                Path2PieceCount += rightPieces;
                Path2NgPieceCount += rightNgPieces;
                LastFramePieceCount = safeTotalPieces;
                LastFrameNgPieceCount = safeNgPieces;
            });
        }

        private void CommitCompletedFrame(PendingFrameUpdate frame)
        {
            try
            {
                var mergedResults = frame.GetMergedResults();
                var isNG = frame.FrameIsNg;
                int totalPieces = Math.Max(0, frame.TotalPieceCount);
                int ngPieces = Math.Min(Math.Max(0, frame.NgPieceCount), totalPieces);
                Model.SetNgOutputs(frame.Left?.IsNG ?? false, frame.Right?.IsNG ?? false);
                OnFrameArrived(frame.FrameId, frame.FrameIdText);
                UpdateStatistics(isNG, totalPieces, ngPieces);
                UpdateDefectDetails(mergedResults);
                UpdatePathPieceTotals(frame, totalPieces, ngPieces);
                SafeRecordFrame(frame.FrameIdText, isNG, mergedResults, frame.GetPathSummary());

                Model.AddLog(
                    $"整帧提交完成: FrameId={frame.FrameIdText}, Left={(frame.Left != null ? "Y" : "N")}, Right={(frame.Right != null ? "Y" : "N")}, " +
                    $"左计片={frame.Left?.PieceCount ?? 0}, 左NG片={frame.Left?.NgPieceCount ?? 0}, 右计片={frame.Right?.PieceCount ?? 0}, 右NG片={frame.Right?.NgPieceCount ?? 0}, " +
                    $"总计片={totalPieces}, 总NG片={ngPieces}, 缺陷数={mergedResults.Count}, IsNG={isNG}",
                    "DEBUG");

                RemoveFrameIdText(frame.FrameId);
            }
            catch (Exception ex)
            {
                Model.AddLog($"整帧提交异常: {ex.Message}", "ERROR");
            }
        }

        private void OnFrameArrived(long frameId, string frameIdText = null)
        {
            var nowUtc = DateTime.UtcNow;
            if (frameId <= 0)
                frameId = nowUtc.Ticks;

            RunOnUiThread(() =>
            {
                if (frameId == LastFrameId)
                    return;

                FrameIntervalMs = _lastFrameUtc == DateTime.MinValue
                    ? -1
                    : (nowUtc - _lastFrameUtc).TotalMilliseconds;

                LastFrameId = frameId;
                if (!string.IsNullOrEmpty(frameIdText))
                    LastFrameIdText = frameIdText;
                FrameCount++;
                LastFrameTimeText = DateTime.Now.ToString("HH:mm:ss.fff");
                ShowNewFrameBadge = true;
                _newFrameBadgeUntilUtc = nowUtc.AddMilliseconds(NewFrameBadgeDurationMs);
                _lastFrameUtc = nowUtc;
                _lastDisplayedStreamAgeBucket = 0;
                StreamState = "Live";
                StreamStatusText = "流正常";
            });
        }

        private static bool TryParseInputPacket(
            XYHDInputPacket packet,
            out HImage image,
            out bool isNG,
            out List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            image = null;
            isNG = false;
            results = new List<ReeYin_V.Core.DeepLearning.Result>();

            if (packet == null)
                return false;

            results = ExtractResults(packet.DefectResults);
            isNG = ResolveIsNg(packet.IsOks, results);
            return true;
        }

        private static List<ReeYin_V.Core.DeepLearning.Result> ExtractResults(object rawResults)
        {
            if (rawResults is List<ReeYin_V.Core.DeepLearning.Result> single)
                return single;

            if (rawResults is List<List<ReeYin_V.Core.DeepLearning.Result>> multi)
            {
                return multi
                    .Where(x => x != null)
                    .SelectMany(x => x)
                    .ToList();
            }

            return new List<ReeYin_V.Core.DeepLearning.Result>();
        }

        private static bool ResolveIsNg(object isOks, List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            if ((results?.Count ?? 0) > 0)
                return true;

            if (isOks is bool singleBool)
                return !singleBool;

            if (isOks is IEnumerable<bool> boolEnumerable)
            {
                var list = boolEnumerable.ToList();
                if (list.Count > 0)
                    return list.Any(b => !b);
            }

            if (isOks is IEnumerable genericEnumerable && isOks is not string)
            {
                var bools = new List<bool>();
                foreach (var item in genericEnumerable)
                {
                    if (item is bool b)
                        bools.Add(b);
                }

                if (bools.Count > 0)
                    return bools.Any(b => !b);
            }

            return false;
        }

        private static (int total, int ng) ResolveAlgorithmPieceStats(
            XYHDInputPacket packet,
            List<ReeYin_V.Core.DeepLearning.Result> results,
            bool pathIsNg)
        {
            if (TryExtractIsOkList(packet?.IsOks, out var isOkList))
            {
                if (isOkList.Count > 0)
                {
                    int ngCount = isOkList.Count(item => !item);
                    if (ngCount > 0 || (results?.Count ?? 0) == 0)
                        return (isOkList.Count, ngCount);
                }
            }

            if ((results?.Count ?? 0) > 0)
            {
                int estimatedNg = Math.Max(1, EstimateNgPieceCountByResultImageIndex(results));
                return (estimatedNg, estimatedNg);
            }

            return pathIsNg ? (1, 1) : (0, 0);
        }

        private static bool TryExtractIsOkList(object isOks, out List<bool> list)
        {
            list = new List<bool>();
            if (isOks == null || isOks is string)
                return false;

            if (isOks is bool singleBool)
            {
                list.Add(singleBool);
                return true;
            }

            if (isOks is IEnumerable<bool> boolEnumerable)
            {
                list.AddRange(boolEnumerable);
                return true;
            }

            if (isOks is IEnumerable genericEnumerable)
            {
                bool hasNonBoolItem = false;
                foreach (var item in genericEnumerable)
                {
                    if (item is bool b)
                    {
                        list.Add(b);
                    }
                    else if (item != null)
                    {
                        hasNonBoolItem = true;
                    }
                }

                return list.Count > 0 || !hasNonBoolItem;
            }

            return false;
        }

        private static string FormatBoolListForLog(IEnumerable<bool> values)
        {
            if (values == null)
                return "null";

            return string.Join(",", values.Select(v => v ? "1" : "0"));
        }

        private static int EstimateNgPieceCountByResultImageIndex(List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            if (results == null || results.Count == 0)
                return 0;

            var imageIndexes = new HashSet<int>();
            foreach (var result in results)
            {
                if (TryGetResultImageIndex(result, out int imageIndex))
                    imageIndexes.Add(imageIndex);
            }

            return imageIndexes.Count;
        }

        private static bool TryGetResultImageIndex(ReeYin_V.Core.DeepLearning.Result result, out int imageIndex)
        {
            imageIndex = 0;
            if (result?.Others == null)
                return false;

            foreach (string key in new[] { "DefectPostProcess.ImageIndex", "XYHD_SourceImageIndex", "ImageIndex", "TargetIndex" })
            {
                if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                    continue;

                try
                {
                    imageIndex = Convert.ToInt32(rawValue);
                    return imageIndex >= 0;
                }
                catch
                {
                }
            }

            return false;
        }

        /// <summary>
        /// 更新主图显示。主图按帧刷新，不再被左右路重复触发。
        /// </summary>
    }
}

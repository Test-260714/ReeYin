using Custom.DefectOverview.Models.Common;
using Custom.DefectOverview.Models.GroupedDualCamera;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.Services.GroupedDualCamera;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
        private const string GroupedDualCameraDefaultSourceName = "DefectOverview.GroupedDualCamera";
        private const string GroupedDualCameraDisplayPrefix = "多相机";
        private const int GroupedCameraSnapshotCacheIntervalMs = 1000;

        public void EnsureDefaultGroupedDualCameraBindings()
        {
            GroupedDualCameraBindings ??= new ObservableCollection<GroupedDualCameraBinding>();
            if (GroupedDualCameraBindings.Count == 0)
            {
                foreach (GroupedDualCameraBinding binding in CreateDefaultGroupedDualCameraBindings())
                    GroupedDualCameraBindings.Add(binding);

                return;
            }

            if (!HasSourceDrivenGroupedDualCameraBindings()
                && ShouldNormalizeDefaultGroupedDualCameraBindings())
            {
                NormalizeDefaultGroupedDualCameraBindings();
            }
        }

        public void NormalizeDefaultGroupedDualCameraBindings()
        {
            List<GroupedDualCameraBinding> current = GroupedDualCameraBindings?
                .Where(binding => binding != null)
                .ToList() ?? new List<GroupedDualCameraBinding>();

            ObservableCollection<GroupedDualCameraBinding> normalized = new();
            int defaultIndex = 0;
            foreach (GroupedDualCameraBinding defaultBinding in CreateDefaultGroupedDualCameraBindings())
            {
                List<(GroupedDualCameraBinding Binding, int Index)> matches = current
                    .Select((binding, index) => (binding, index))
                    .Where(item => IsSameGroupedDualCameraChannel(item.binding, defaultBinding))
                    .ToList();

                GroupedDualCameraBinding selected = matches
                    .OrderByDescending(item => GetGroupedDualCameraSelectionScore(item.Binding))
                    .ThenByDescending(item => item.Index)
                    .Select(item => item.Binding)
                    .FirstOrDefault();

                normalized.Add(MergeGroupedDualCameraBinding(defaultBinding, selected, ++defaultIndex));
            }

            GroupedDualCameraBindings = normalized;
        }

        public void ResetDefaultGroupedDualCameraBindings()
        {
            GroupedDualCameraBindings = new ObservableCollection<GroupedDualCameraBinding>(CreateDefaultGroupedDualCameraBindings());
        }

        private static IEnumerable<GroupedDualCameraBinding> CreateDefaultGroupedDualCameraBindings()
        {
            int sortIndex = 0;
            for (int groupIndex = 1; groupIndex <= 3; groupIndex++)
            {
                string groupKey = $"{groupIndex:D2}";
                string groupName = $"第{groupIndex}组";
                yield return CreateDefaultGroupedDualCameraBinding(++sortIndex, groupKey, groupName, WidthSide.Left);
                yield return CreateDefaultGroupedDualCameraBinding(++sortIndex, groupKey, groupName, WidthSide.Right);
            }
        }

        private static GroupedDualCameraBinding CreateDefaultGroupedDualCameraBinding(
            int sortIndex,
            string groupKey,
            string groupName,
            WidthSide side)
        {
            string sideText = side == WidthSide.Left ? "L" : "R";
            return new GroupedDualCameraBinding
            {
                SortIndex = sortIndex,
                GroupKey = groupKey,
                GroupName = groupName,
                Side = side,
                DisplayName = $"{groupKey}-{sideText}",
                ResultInput = new TransmitParam(),
                ImageInput = new TransmitParam(),
                IsRequired = true
            };
        }

        private bool ShouldNormalizeDefaultGroupedDualCameraBindings()
        {
            List<GroupedDualCameraBinding> bindings = GroupedDualCameraBindings?
                .Where(binding => binding != null)
                .ToList() ?? new List<GroupedDualCameraBinding>();
            List<GroupedDualCameraBinding> defaults = CreateDefaultGroupedDualCameraBindings().ToList();
            if (bindings.Count != defaults.Count)
                return true;

            HashSet<string> channelKeys = new(StringComparer.OrdinalIgnoreCase);
            foreach (GroupedDualCameraBinding binding in bindings)
            {
                if (binding.Side == WidthSide.Unknown)
                    return true;

                string channelKey = BuildGroupedDualCameraChannelKey(binding);
                if (string.IsNullOrWhiteSpace(channelKey) || !channelKeys.Add(channelKey))
                    return true;
            }

            return defaults.Any(defaultBinding =>
                !bindings.Any(binding => IsSameGroupedDualCameraChannel(binding, defaultBinding)));
        }

        private static GroupedDualCameraBinding MergeGroupedDualCameraBinding(
            GroupedDualCameraBinding defaultBinding,
            GroupedDualCameraBinding source,
            int sortIndex)
        {
            if (source == null)
            {
                defaultBinding.SortIndex = sortIndex;
                return defaultBinding;
            }

            return new GroupedDualCameraBinding
            {
                SortIndex = sortIndex,
                SourceSerial = source.SourceSerial,
                SourceCameraName = source.SourceCameraName,
                SourceOutputName = source.SourceOutputName,
                GroupKey = defaultBinding.GroupKey,
                GroupName = string.IsNullOrWhiteSpace(source.GroupName) ? defaultBinding.GroupName : source.GroupName,
                Side = defaultBinding.Side,
                DisplayName = defaultBinding.DisplayName,
                ResultInput = source.ResultInput ?? new TransmitParam(),
                ImageInput = source.ImageInput ?? new TransmitParam(),
                IsRequired = source.IsRequired
            };
        }

        private bool HasSourceDrivenGroupedDualCameraBindings()
        {
            return GroupedDualCameraBindings?
                .Any(binding => binding != null
                    && (binding.SourceSerial >= 0
                        || !string.IsNullOrWhiteSpace(binding.SourceCameraName)
                        || !string.IsNullOrWhiteSpace(binding.SourceOutputName))) == true;
        }

        private static bool IsSameGroupedDualCameraChannel(
            GroupedDualCameraBinding left,
            GroupedDualCameraBinding right)
        {
            return string.Equals(
                    NormalizeGroupedDualCameraGroupKey(left?.GroupKey),
                    NormalizeGroupedDualCameraGroupKey(right?.GroupKey),
                    StringComparison.OrdinalIgnoreCase)
                && left?.Side == right?.Side;
        }

        private static string BuildGroupedDualCameraChannelKey(GroupedDualCameraBinding binding)
        {
            if (binding == null)
                return string.Empty;

            string groupKey = NormalizeGroupedDualCameraGroupKey(binding.GroupKey);
            return string.IsNullOrWhiteSpace(groupKey) || binding.Side == WidthSide.Unknown
                ? string.Empty
                : $"{groupKey}:{binding.Side}";
        }

        private static int GetGroupedDualCameraSelectionScore(GroupedDualCameraBinding binding)
        {
            if (binding == null)
                return 0;

            int score = 0;
            if (HasConfiguredInputSelection(binding.ResultInput))
                score += 100;
            if (HasConfiguredInputSelection(binding.ImageInput))
                score += 50;
            if (!string.IsNullOrWhiteSpace(binding.DisplayName))
                score += 5;
            if (!string.IsNullOrWhiteSpace(binding.GroupName))
                score += 3;
            if (binding.IsRequired)
                score += 1;

            return score;
        }

        private bool ShouldUseGroupedDualCameraInputs()
        {
            if (!UseGroupedDualCameraInputs)
                return false;

            EnsureDefaultGroupedDualCameraBindings();
            return true;
        }

        private NodeStatus PublishGroupedDualCameraInputs()
        {
            EnsureDefaultGroupedDualCameraBindings();
            List<GroupedDualCameraBinding> bindings = GetActiveGroupedDualCameraBindings();
            if (!TryValidateGroupedDualCameraBindings(bindings, out string validateMessage))
            {
                PublishStatusText = validateMessage;
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] Error: {PublishStatusText}");
                return NodeStatus.Error;
            }

            string frameKey = ResolveString(ResolveSelectedInputValue(InputFrameKey), string.Empty);
            string frameIdText = ResolveString(ResolveSelectedInputValue(InputFrameIdText), frameKey);
            double laneWidth = ResolveDouble(ResolveSelectedInputValue(InputLaneWidth), LaneWidth);
            double pixelEquivalentX = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentX), PixelEquivalentX);
            double pixelEquivalentY = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentY), PixelEquivalentY);
            double edgeCalibrationX = ResolveDouble(ResolveSelectedInputValue(InputEdgeCalibrationX), EdgeCalibrationX);
            string schemeFilePath = ResolveString(ResolveSelectedInputValue(InputSchemeFilePath), SchemeFilePath);

            if (string.IsNullOrWhiteSpace(frameKey))
                frameKey = BuildDefaultFrameKey();

            if (string.IsNullOrWhiteSpace(frameIdText))
                frameIdText = frameKey;

            long cycleId = FlowCycleContext.CurrentCycleId;
            Dictionary<GroupedDualCameraBinding, IReadOnlyList<Result>> channelResults = new();
            HImage leftImage = null;
            HImage rightImage = null;
            DateTime snapshotCacheUtc = DateTime.UtcNow;
            bool shouldCacheCameraSnapshots = ShouldCacheGroupedCameraSnapshots(snapshotCacheUtc);
            DateTime snapshotCacheLocalTime = DateTime.Now;
            List<BandMapCameraSnapshotItem> cameraSnapshots = shouldCacheCameraSnapshots
                ? new List<BandMapCameraSnapshotItem>()
                : null;
            int cameraSnapshotIndex = 0;

            try
            {
                foreach (GroupedDualCameraBinding binding in bindings)
                {
                    cameraSnapshotIndex++;
                    DefectOverviewPathRole pathRole = ResolveGroupedDualCameraPathRole(binding.Side);
                    string pathName = ResolveGroupedDualCameraDisplayName(binding);
                    TransmitParam resultInput = ResolveGroupedDualCameraResultInput(binding);
                    object rawResults = ResolveSelectedInputValue(resultInput, false, out string resultsSource);
                    List<Result> inputResults = ExtractResults(rawResults);
                    (HImage Image, string Source) resolvedImage = ResolveSelectedImageInput(binding.ImageInput, resultInput);
                    using HImage image = resolvedImage.Image;

                    List<Result> publishedResults = BuildPublishedResults(
                        image,
                        inputResults,
                        frameKey,
                        frameIdText,
                        laneWidth,
                        pixelEquivalentX,
                        pixelEquivalentY,
                        edgeCalibrationX,
                        schemeFilePath,
                        DefectOverviewFrameLayout.DualPath,
                        pathRole,
                        pathName);
                    AttachPreviewMetadata(image, publishedResults);

                    channelResults[binding] = publishedResults;
                    CaptureGroupedDualCameraSideImage(binding.Side, image, ref leftImage, ref rightImage);
                    TryAddGroupedCameraSnapshot(
                        cameraSnapshots,
                        binding,
                        image,
                        cameraSnapshotIndex,
                        snapshotCacheLocalTime,
                        publishedResults.Count);

                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                            $"[DefectOverviewPublish] GroupedDualCamera channel={pathName}, image={DescribeTransmitParam(binding.ImageInput)}, imageSource={resolvedImage.Source ?? "none"}, results={DescribeTransmitParam(resultInput)}, resultsSource={resultsSource ?? "none"}, rawType={rawResults?.GetType().FullName ?? "null"}, input={inputResults.Count}, published={publishedResults.Count}");
                    }

                    Custom.DefectOverview.DefectOverviewConsole.WriteFrameTrace(
                        $"[FrameTrace][PublishChannel] cycle={cycleId}, frameKey={frameKey}, channel={pathName}, input={inputResults.Count}, published={publishedResults.Count}, results={DescribeResultsForFrameTrace(publishedResults)}");
                }

                GroupedDualCameraFrame frame = new GroupedDualCameraOverviewBuilder().Build(
                    frameKey,
                    frameIdText,
                    bindings,
                    binding => channelResults.TryGetValue(binding, out IReadOnlyList<Result> results) ? results : Array.Empty<Result>());

                TryPublishGroupedCameraSnapshots(cameraSnapshots);

                List<Result> leftResults = frame.Channels
                    .Where(channel => channel.Side == WidthSide.Left)
                    .SelectMany(channel => channel.Results ?? Array.Empty<Result>())
                    .Where(result => result != null)
                    .ToList();
                List<Result> rightResults = frame.Channels
                    .Where(channel => channel.Side == WidthSide.Right)
                    .SelectMany(channel => channel.Results ?? Array.Empty<Result>())
                    .Where(result => result != null)
                    .ToList();

                string sourceName = ResolveGroupedDualCameraSourceName();
                PublishGroupedDualCameraSide(
                    WidthSide.Left,
                    sourceName,
                    frameKey,
                    frameIdText,
                    cycleId,
                    leftImage,
                    leftResults,
                    laneWidth,
                    pixelEquivalentX,
                    pixelEquivalentY,
                    edgeCalibrationX,
                    schemeFilePath);
                PublishGroupedDualCameraSide(
                    WidthSide.Right,
                    sourceName,
                    frameKey,
                    frameIdText,
                    cycleId,
                    rightImage,
                    rightResults,
                    laneWidth,
                    pixelEquivalentX,
                    pixelEquivalentY,
                    edgeCalibrationX,
                    schemeFilePath);

                List<Result> allResults = frame.Channels
                    .SelectMany(channel => channel.Results ?? Array.Empty<Result>())
                    .Where(result => result != null)
                    .ToList();
                PublishedResults = allResults;
                PublishedCount = allResults.Count;
                PublishedFrameKey = frameKey;
                PublishStatusText = BuildGroupedDualCameraStatusText(frame, leftResults.Count, rightResults.Count);
                LastPublishTime = DateTime.Now;

                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectOverviewPublish] GroupedDualCamera published frameKey={frameKey}, channels={frame.Channels.Count}, left={leftResults.Count}, right={rightResults.Count}, total={allResults.Count}");
                }
                Custom.DefectOverview.DefectOverviewConsole.WriteFrameTrace(
                    $"[FrameTrace][PublishFrame] cycle={cycleId}, frameKey={frameKey}, channels={frame.Channels.Count}, left={leftResults.Count}, right={rightResults.Count}, total={allResults.Count}");
                return NodeStatus.Success;
            }
            finally
            {
                leftImage?.Dispose();
                rightImage?.Dispose();
            }
        }

        private List<GroupedDualCameraBinding> GetActiveGroupedDualCameraBindings()
        {
            return GroupedDualCameraBindings?
                .Where(binding => binding != null)
                .Where(binding => binding.IsRequired || HasConfiguredInputSelection(ResolveGroupedDualCameraResultInput(binding)))
                .OrderBy(binding => binding.SortIndex)
                .ToList() ?? new List<GroupedDualCameraBinding>();
        }

        private bool TryValidateGroupedDualCameraBindings(
            IReadOnlyCollection<GroupedDualCameraBinding> bindings,
            out string message)
        {
            if (bindings == null || bindings.Count == 0)
            {
                message = "多相机至少需要配置一组相机结果绑定。";
                return false;
            }

            foreach (GroupedDualCameraBinding binding in bindings)
            {
                string displayName = ResolveGroupedDualCameraDisplayName(binding);
                if (binding.Side == WidthSide.Unknown)
                {
                    message = $"多相机通道 {displayName} 未设置左/右侧别。";
                    return false;
                }

                if (binding.IsRequired && !HasConfiguredInputSelection(ResolveGroupedDualCameraResultInput(binding)))
                {
                    message = $"多相机通道 {displayName} 未选择后处理结果源。";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private void PublishGroupedDualCameraSide(
            WidthSide side,
            string sourceName,
            string frameKey,
            string frameIdText,
            long cycleId,
            HImage image,
            IReadOnlyList<Result> results,
            double laneWidth,
            double pixelEquivalentX,
            double pixelEquivalentY,
            double edgeCalibrationX,
            string schemeFilePath)
        {
            List<Result> publishedResults = results?.Where(result => result != null).ToList() ?? new List<Result>();
            DefectOverviewPathRole pathRole = ResolveGroupedDualCameraPathRole(side);
            string pathName = ResolveGroupedDualCameraSidePathName(side);

            ResolveIngestService().PublishPath(new DefectOverviewPathPacket
            {
                SourceName = sourceName,
                FrameKey = frameKey,
                FrameIdText = frameIdText,
                CycleId = cycleId,
                CreatedUtc = DateTime.UtcNow,
                FrameLayout = DefectOverviewFrameLayout.DualPath,
                PathRole = pathRole,
                PathName = pathName,
                PathImage = image,
                OriginalImage = image,
                ApplyPostProcess = false,
                SaveLocalDefectImages = SaveLocalDefectImages,
                IsNg = publishedResults.Count > 0,
                Results = publishedResults,
                LaneWidth = laneWidth > 0 ? laneWidth : null,
                PixelEquivalentX = pixelEquivalentX > 0 ? pixelEquivalentX : null,
                PixelEquivalentY = pixelEquivalentY > 0 ? pixelEquivalentY : null,
                EdgeCalibrationX = edgeCalibrationX,
                SchemeFilePath = schemeFilePath ?? string.Empty
            });
        }

        private static string DescribeResultsForFrameTrace(IReadOnlyList<Result> results)
        {
            if (results == null || results.Count == 0)
                return "none";

            const int maxItems = 5;
            IEnumerable<string> items = results
                .Where(result => result != null)
                .Take(maxItems)
                .Select((result, index) =>
                    $"#{index}:cls={result.ClassId},cx={result.Cx:F1},cy={result.Cy:F1},w={result.Width:F1},h={result.Height:F1},conf={result.Confidence:F3}");
            string suffix = results.Count > maxItems ? $",more={results.Count - maxItems}" : string.Empty;
            return string.Join(";", items) + suffix;
        }

        private bool ShouldCacheGroupedCameraSnapshots(DateTime nowUtc)
        {
            if (_lastGroupedCameraSnapshotCacheUtc != DateTime.MinValue
                && (nowUtc - _lastGroupedCameraSnapshotCacheUtc).TotalMilliseconds < GroupedCameraSnapshotCacheIntervalMs)
            {
                return false;
            }

            _lastGroupedCameraSnapshotCacheUtc = nowUtc;
            return true;
        }

        private static void TryAddGroupedCameraSnapshot(
            List<BandMapCameraSnapshotItem> snapshots,
            GroupedDualCameraBinding binding,
            HImage image,
            int sortIndex,
            DateTime refreshLocalTime,
            int defectCount)
        {
            if (snapshots == null)
                return;

            var bitmap = DefectPreviewFactory.CreateBitmapFromHImage(image);
            bool hasImage = bitmap != null;
            string cameraName = BuildGroupedCameraSnapshotName(binding, sortIndex);
            string statusState = hasImage
                ? defectCount > 0 ? "NG" : "OK"
                : "ConnectionError";
            snapshots.Add(new BandMapCameraSnapshotItem
            {
                SortIndex = sortIndex,
                CameraKey = BuildGroupedCameraSnapshotKey(binding, sortIndex),
                CameraName = cameraName,
                StatusText = hasImage
                    ? defectCount > 0 ? $"NG {defectCount}" : "OK"
                    : "连接异常",
                StatusState = statusState,
                LastRefreshText = hasImage
                    ? $"最后缓存 {refreshLocalTime:HH:mm:ss}"
                    : $"缓存 {refreshLocalTime:HH:mm:ss} 无图像",
                SnapshotImage = bitmap
            });
        }

        private void TryPublishGroupedCameraSnapshots(IReadOnlyList<BandMapCameraSnapshotItem> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
                return;

            try
            {
                ResolveBandMapStateService().UpdateCameraSnapshots(snapshots);
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectOverviewPublish] Camera snapshot cache skipped: {ex.Message}");
            }
        }

        private static string BuildGroupedCameraSnapshotName(GroupedDualCameraBinding binding, int sortIndex)
        {
            if (!string.IsNullOrWhiteSpace(binding?.SourceCameraName))
                return binding.SourceCameraName;

            return $"相机 {Math.Clamp(sortIndex, 1, 99):D2}";
        }

        private static string BuildGroupedCameraSnapshotKey(GroupedDualCameraBinding binding, int sortIndex)
        {
            if (binding != null && binding.SourceSerial >= 0 && !string.IsNullOrWhiteSpace(binding.SourceOutputName))
                return $"{binding.SourceSerial}:{binding.SourceOutputName}";

            string displayName = ResolveGroupedDualCameraDisplayName(binding);
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Camera-{Math.Clamp(sortIndex, 1, 99):D2}"
                : displayName;
        }

        private static DefectOverviewPathRole ResolveGroupedDualCameraPathRole(WidthSide side)
        {
            return side switch
            {
                WidthSide.Left => DefectOverviewPathRole.Left,
                WidthSide.Right => DefectOverviewPathRole.Right,
                _ => DefectOverviewPathRole.Unknown
            };
        }

        private static void CaptureGroupedDualCameraSideImage(
            WidthSide side,
            HImage image,
            ref HImage leftImage,
            ref HImage rightImage)
        {
            if (image == null)
                return;

            if (side == WidthSide.Left && leftImage == null)
                leftImage = CopyGroupedDualCameraImage(image);
            else if (side == WidthSide.Right && rightImage == null)
                rightImage = CopyGroupedDualCameraImage(image);
        }

        private static HImage CopyGroupedDualCameraImage(HImage image)
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

        private string ResolveGroupedDualCameraSourceName()
        {
            string sourceName = string.IsNullOrWhiteSpace(SourceName) ? "DefectOverview" : SourceName;
            return string.Equals(sourceName, "DefectOverview", StringComparison.Ordinal)
                ? GroupedDualCameraDefaultSourceName
                : sourceName;
        }

        private string ResolveGroupedDualCameraSidePathName(WidthSide side)
        {
            string prefix = string.IsNullOrWhiteSpace(PathName) ? GroupedDualCameraDisplayPrefix : PathName;
            return side switch
            {
                WidthSide.Left => $"{prefix}-L",
                WidthSide.Right => $"{prefix}-R",
                _ => prefix
            };
        }

        private static string ResolveGroupedDualCameraDisplayName(GroupedDualCameraBinding binding)
        {
            if (!string.IsNullOrWhiteSpace(binding?.DisplayName))
                return binding.DisplayName;

            string groupKey = string.IsNullOrWhiteSpace(binding?.GroupKey) ? "G??" : binding.GroupKey;
            groupKey = FormatGroupedDualCameraGroupKeyForDisplay(groupKey);
            string sideText = binding?.Side == WidthSide.Left
                ? "L"
                : binding?.Side == WidthSide.Right ? "R" : "?";
            return $"{groupKey}-{sideText}";
        }

        private static string FormatGroupedDualCameraGroupKeyForDisplay(string groupKey)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                return "??";

            string text = groupKey.Trim();
            if (text.Length >= 2
                && (text[0] == 'G' || text[0] == 'g')
                && int.TryParse(text[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gIndex))
            {
                return gIndex.ToString("D2", CultureInfo.InvariantCulture);
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                ? index.ToString("D2", CultureInfo.InvariantCulture)
                : text;
        }

        private static string BuildGroupedDualCameraStatusText(
            GroupedDualCameraFrame frame,
            int leftCount,
            int rightCount)
        {
            string channelSummary = string.Join("，", frame.Channels.Select(channel =>
                $"{channel.DisplayName}:{channel.DefectCount}"));
            return $"多相机已发布：{frame.Channels.Count}路，左侧 {leftCount} 条，右侧 {rightCount} 条，总计 {frame.TotalDefectCount} 条 | {channelSummary}";
        }
    }
}

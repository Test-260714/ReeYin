using Custom.DefectOverview.Models;
using ReeYin_V.Core.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Custom.DefectOverview.Services
{
    public sealed partial class BandMapStateService : IBandMapStateService
    {
        public DefectBatchReportSyncRequest CreateBrjReportSyncRequest(
            string sn,
            bool isRollCompleted,
            long snapshotVersion,
            TaskCompletionSource<DefectBatchReportSyncResult> completion = null)
        {
            lock (_sync)
            {
                DateTime syncTime = DateTime.Now;
                string batchName = BuildBatchNumberText(_batchStartedLocalTime, _batchNumber);
                string safeSn = string.IsNullOrWhiteSpace(sn) ? batchName : sn.Trim();
                double safeFrameSpan = Math.Max(1.0, _frameSpanMillimeters);
                double detectMeters = _frameSequence * safeFrameSpan / 1000.0;
                BandMapSlittingSettings slittingSettings = BuildSlittingSettings(
                    _history,
                    _isSlittingEnabled,
                    _knifeSpacingMillimeters,
                    _firstCutOffsetMillimeters,
                    _stripWidthMillimeters,
                    _slitCount,
                    _path1LaneWidthMillimeters,
                    _path2LaneWidthMillimeters);
                BuildSlitCoordinateTexts(slittingSettings, out string slitLeftCoordinates, out string slitRightCoordinates);

                var defects = new List<DefectBatchReportItem>(_history.Count);
                for (int i = 0; i < _history.Count; i++)
                {
                    defects.Add(CreateBrjReportItem(_history[i], i, safeFrameSpan, slittingSettings, syncTime));
                }

                return new DefectBatchReportSyncRequest
                {
                    SN = safeSn,
                    BatchName = batchName,
                    DetectMeters = detectMeters,
                    IsRollCompleted = isRollCompleted,
                    BatchStartedTime = _batchStartedLocalTime,
                    BatchEndedTime = isRollCompleted ? syncTime : null,
                    SyncTime = syncTime,
                    SnapshotVersion = snapshotVersion,
                    TotalFrames = _totalFrames,
                    OkFrames = _okFrames,
                    NgFrames = _ngFrames,
                    ProductWidthMm = NormalizeReportNumber(slittingSettings?.StripWidthMillimeters ?? 0d),
                    CameraCount = ResolveReportCameraCount(_history, _showPathStatusBadges, _totalFrames),
                    ResolutionX = BuildDistinctNumberText(_history, item => item.PixelEquivalentX),
                    ResolutionY = BuildDistinctNumberText(_history, item => item.PixelEquivalentY),
                    ImageWidth = BuildDistinctIntegerText(_history, item => item.SourceWidth),
                    ImageHeight = BuildDistinctIntegerText(_history, item => item.SourceHeight),
                    SlitLeftCoordinates = slitLeftCoordinates,
                    SlitRightCoordinates = slitRightCoordinates,
                    Defects = defects,
                    Completion = completion,
                };
            }
        }

        private static DefectBatchReportItem CreateBrjReportItem(
            HistoryItem item,
            int index,
            double frameSpanMillimeters,
            BandMapSlittingSettings slittingSettings,
            DateTime syncTime)
        {
            double widthMillimeters = ResolvePositiveNumber(item?.PhysicalWidthMillimeters) ?? ResolveNumber(item?.Width, 0d);
            double heightMillimeters = ResolveHeightMillimeters(item);
            double diameterMillimeters = Math.Max(widthMillimeters, heightMillimeters);
            double positionXMm = ResolveAbsoluteCenterMillimeters(item, slittingSettings) ?? ResolveNumber(item?.CenterX, 0d);
            double positionYM = ResolveNumber(item?.FrameSequence, 0L) * Math.Max(1.0, frameSpanMillimeters) / 1000.0;
            SlitEvaluation slitEvaluation = EvaluateSlit(item, slittingSettings);

            return new DefectBatchReportItem
            {
                FrameKey = item?.FrameIdText ?? string.Empty,
                DefectIndex = index + 1,
                CameraIndex = ResolveCameraIndex(item),
                CameraName = ResolveCameraName(item),
                SegmentIndex = ToSafeInt(item?.FrameSequence ?? 0L),
                SlitIndex = ResolveSlitIndex(slitEvaluation?.SlitText),
                DefectType = string.IsNullOrWhiteSpace(item?.ClassName)
                    ? (item?.ClassId ?? 0).ToString(CultureInfo.InvariantCulture)
                    : item.ClassName,
                AreaMm2 = NormalizeReportNumber(widthMillimeters * heightMillimeters),
                DiameterMm = NormalizeReportNumber(diameterMillimeters),
                PositionXMm = NormalizeReportNumber(positionXMm),
                PositionYM = NormalizeReportNumber(positionYM),
                DefectImagePath = string.Empty,
                CreateTime = syncTime,
            };
        }

        private static double ResolveHeightMillimeters(HistoryItem item)
        {
            if (item == null)
            {
                return 0d;
            }

            if (double.IsFinite(item.Height) && item.Height > 0d && double.IsFinite(item.PixelEquivalentY) && item.PixelEquivalentY > 0d)
            {
                return item.Height / item.PixelEquivalentY;
            }

            return ResolveNumber(item.Height, 0d);
        }

        private static int ResolveCameraIndex(HistoryItem item)
        {
            int cameraNumber = ExtractFirstNumber(item?.PathName);
            return cameraNumber > 0 ? cameraNumber - 1 : 0;
        }

        private static string ResolveCameraName(HistoryItem item)
        {
            return string.IsNullOrWhiteSpace(item?.PathName) ? ResolveCameraIndex(item).ToString(CultureInfo.InvariantCulture) : item.PathName.Trim();
        }

        private static int ResolveSlitIndex(string slitText)
        {
            int slitNumber = ExtractFirstNumber(slitText);
            return Math.Max(0, slitNumber);
        }

        private static int ExtractFirstNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            int start = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
            {
                return 0;
            }

            int end = start;
            while (end < text.Length && char.IsDigit(text[end]))
            {
                end++;
            }

            return int.TryParse(text[start..end], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }

        private static double? ResolvePositiveNumber(double? value)
        {
            return value.HasValue && double.IsFinite(value.Value) && value.Value > 0d ? value.Value : null;
        }

        private static double? ResolvePositiveOrFiniteNumber(double? value)
        {
            return value.HasValue && double.IsFinite(value.Value) ? value.Value : null;
        }

        private static double ResolveNumber(double? value, double fallback)
        {
            return value.HasValue && double.IsFinite(value.Value) ? value.Value : fallback;
        }

        private static double ResolveNumber(long? value, long fallback)
        {
            return value.HasValue ? value.Value : fallback;
        }

        private static double NormalizeReportNumber(double value)
        {
            return double.IsFinite(value) ? value : 0d;
        }

        private static int ResolveReportCameraCount(IReadOnlyCollection<HistoryItem> history, bool showPathStatusBadges, int totalFrames)
        {
            int maxCameraIndex = -1;
            var pathNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (history != null)
            {
                foreach (HistoryItem item in history)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    maxCameraIndex = Math.Max(maxCameraIndex, ResolveCameraIndex(item));
                    if (!string.IsNullOrWhiteSpace(item.PathName))
                    {
                        pathNames.Add(item.PathName.Trim());
                    }
                }
            }

            int historyCameraCount = Math.Max(pathNames.Count, maxCameraIndex + 1);
            if (showPathStatusBadges)
            {
                return Math.Max(2, historyCameraCount);
            }

            return historyCameraCount > 0 ? historyCameraCount : (totalFrames > 0 ? 1 : 0);
        }

        private static string BuildDistinctNumberText(IEnumerable<HistoryItem> history, Func<HistoryItem, double> selector)
        {
            var values = new List<double>();
            if (history == null || selector == null)
            {
                return string.Empty;
            }

            foreach (HistoryItem item in history)
            {
                if (item == null)
                {
                    continue;
                }

                double value = selector(item);
                if (!double.IsFinite(value) || value <= 0d || ContainsNumber(values, value))
                {
                    continue;
                }

                values.Add(value);
            }

            return string.Join(",", values.ConvertAll(FormatReportNumber));
        }

        private static string BuildDistinctIntegerText(IEnumerable<HistoryItem> history, Func<HistoryItem, int> selector)
        {
            var values = new List<int>();
            if (history == null || selector == null)
            {
                return string.Empty;
            }

            foreach (HistoryItem item in history)
            {
                if (item == null)
                {
                    continue;
                }

                int value = selector(item);
                if (value <= 0 || values.Contains(value))
                {
                    continue;
                }

                values.Add(value);
            }

            return string.Join(",", values);
        }

        private static void BuildSlitCoordinateTexts(
            BandMapSlittingSettings slittingSettings,
            out string leftCoordinates,
            out string rightCoordinates)
        {
            leftCoordinates = string.Empty;
            rightCoordinates = string.Empty;

            if (slittingSettings == null
                || !slittingSettings.IsEnabled
                || !slittingSettings.StripWidthMillimeters.HasValue
                || slittingSettings.StripWidthMillimeters.Value <= 1d
                || slittingSettings.SlitCount <= 0)
            {
                return;
            }

            double stripWidth = slittingSettings.StripWidthMillimeters.Value;
            double spacing = double.IsFinite(slittingSettings.KnifeSpacingMillimeters) && slittingSettings.KnifeSpacingMillimeters > 0d
                ? slittingSettings.KnifeSpacingMillimeters
                : 1d;
            double offset = double.IsFinite(slittingSettings.FirstCutOffsetMillimeters) && slittingSettings.FirstCutOffsetMillimeters >= 0d
                ? slittingSettings.FirstCutOffsetMillimeters
                : 0d;
            var lefts = new List<string>();
            var rights = new List<string>();

            for (int i = 0; i < slittingSettings.SlitCount; i++)
            {
                double left = offset + i * spacing;
                double right = offset + (i + 1) * spacing;
                if (!double.IsFinite(left) || left > stripWidth + 0.0001d)
                {
                    break;
                }

                lefts.Add(FormatReportNumber(Math.Clamp(left, 0d, stripWidth)));
                rights.Add(FormatReportNumber(Math.Clamp(right, 0d, stripWidth)));
            }

            leftCoordinates = string.Join(",", lefts);
            rightCoordinates = string.Join(",", rights);
        }

        private static bool ContainsNumber(List<double> values, double value)
        {
            foreach (double existing in values)
            {
                if (Math.Abs(existing - value) < 0.0001d)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatReportNumber(double value)
        {
            return NormalizeReportNumber(value).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static int ToSafeInt(long value)
        {
            if (value <= 0L)
            {
                return 0;
            }

            return value > int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}

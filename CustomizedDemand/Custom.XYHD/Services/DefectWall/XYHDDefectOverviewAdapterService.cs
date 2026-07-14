using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.XYHD.Models;
using HalconDotNet;
using Prism.Events;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Custom.XYHD.Services
{
    public sealed class XYHDDefectOverviewAdapterService : IDisposable
    {
        private readonly IDefectOverviewIngestService _ingestService;
        private SubscriptionToken _resultToken;
        private bool _disposed;

        public XYHDDefectOverviewAdapterService(IDefectOverviewIngestService ingestService)
        {
            _ingestService = ingestService;
            Start();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_resultToken != null)
            {
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Unsubscribe(_resultToken);
                _resultToken = null;
            }
        }

        private void Start()
        {
            if (_resultToken != null)
                return;

            _resultToken = PrismProvider.EventAggregator
                .GetEvent<OutputResultEvent>()
                .Subscribe(OnResultReceived, ThreadOption.PublisherThread);
        }

        private void OnResultReceived((string source, object data) message)
        {
            if (_disposed || message.source != "XYHD_Detection")
                return;

            if (message.data is not XYHDInputPacket packet)
                return;

            string frameKey = BuildFrameKey(packet);
            if (string.IsNullOrWhiteSpace(frameKey))
                return;

            List<Result> results = ExtractResults(packet.DefectResults);
            double laneWidth = results.Count > 0
                ? ResolveLaneWidth(packet.PathImage, results)
                : 0d;
            XYHDFieldOrientationSettings settings = ResolveFieldOrientationSettings(packet);
            XYHDFieldOrientationMap orientation = XYHDFieldOrientationMapper.Resolve(packet.PathName, settings);
            List<Result> publishedResults = XYHDFieldOrientationMapper.ApplyToResults(results, laneWidth, orientation);

            HImage fallbackPathImage = null;
            HImage pathImage = packet.PathImage;
            try
            {
                if (pathImage == null || !pathImage.IsInitialized())
                {
                    fallbackPathImage = TryResolveConfiguredPathImage(packet, orientation, publishedResults, out string fallbackSource);
                    if (fallbackPathImage != null && fallbackPathImage.IsInitialized())
                    {
                        pathImage = fallbackPathImage;
                        laneWidth = ResolveLaneWidth(pathImage, results);
                        Logs.LogTrace($"[XYHD] [DefectOverviewAdapter] PathImage fallback hit: Path={orientation.FieldPathName ?? packet.PathName ?? "-"}, Source={fallbackSource ?? "-"}");
                    }
                }

                _ingestService.PublishPath(new DefectOverviewPathPacket
                {
                    SourceName = "XYHD_Detection",
                    FrameKey = frameKey,
                    FrameIdText = ResolveFrameIdText(packet),
                    CreatedUtc = packet.ReceiveTime == default ? DateTime.UtcNow : packet.ReceiveTime.ToUniversalTime(),
                    FrameLayout = DefectOverviewFrameLayout.DualPath,
                    PathRole = orientation.FieldRole,
                    PathName = orientation.FieldPathName,
                    PathImage = pathImage,
                    OriginalImage = null,
                    IsNg = ResolveIsNg(packet.IsOks, results),
                    Results = publishedResults,
                    LaneWidth = laneWidth,
                    SchemeFilePath = string.Empty
                });
            }
            finally
            {
                fallbackPathImage?.Dispose();
            }
        }

        private static string BuildFrameKey(XYHDInputPacket packet)
        {
            if (packet == null)
                return null;

            if (packet.FrameId > 0)
                return $"ID:{packet.FrameId}";

            if (!string.IsNullOrWhiteSpace(packet.FrameIdText))
                return $"TXT:{packet.FrameIdText}";

            return null;
        }

        private static string ResolveFrameIdText(XYHDInputPacket packet)
        {
            if (!string.IsNullOrWhiteSpace(packet?.FrameIdText))
                return packet.FrameIdText;

            if (packet?.FrameId > 0)
                return $"Frame-{packet.FrameId}";

            return $"Frame-{DateTime.Now:HHmmssfff}";
        }

        private static XYHDFieldOrientationSettings ResolveFieldOrientationSettings(XYHDInputPacket packet)
        {
            if (packet?.HasFieldOrientationSettings == true)
            {
                return new XYHDFieldOrientationSettings
                {
                    IsConfigured = true,
                    SwapLeftRightPaths = packet.SwapLeftRightPaths,
                    LeftPathXMirror = packet.LeftPathXMirror,
                    RightPathXMirror = packet.RightPathXMirror
                };
            }

            var runtimeSettings = XYHDFieldOrientationRuntimeState.GetSnapshot();
            if (runtimeSettings.IsConfigured)
            {
                return new XYHDFieldOrientationSettings
                {
                    IsConfigured = true,
                    SwapLeftRightPaths = runtimeSettings.SwapLeftRightPaths,
                    LeftPathXMirror = runtimeSettings.LeftPathXMirror,
                    RightPathXMirror = runtimeSettings.RightPathXMirror
                };
            }

            try
            {
                var caches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
                if (caches == null || caches.Count == 0)
                    return default;

                var models = caches.Values
                    .OfType<DetectionModel>()
                    .OrderBy(item => item.Serial < 0 ? int.MaxValue : item.Serial)
                    .ToList();

                DetectionModel model = ResolveDetectionModelFromCache(models, packet);

                if (model == null)
                    return default;

                return XYHDFieldOrientationSettings.FromModel(model);
            }
            catch
            {
                return default;
            }
        }

        private static DetectionModel ResolveDetectionModelFromCache(
            IReadOnlyList<DetectionModel> models,
            XYHDInputPacket packet)
        {
            if (models == null || models.Count == 0)
                return null;

            if (packet?.OwnerSerial >= 0)
            {
                var owner = models.FirstOrDefault(item => item.Serial == packet.OwnerSerial);
                if (owner != null)
                    return owner;
            }

            if (models.Count == 1)
                return models[0];

            var configured = models.FirstOrDefault(item =>
                item.SwapLeftRightPaths
                || item.LeftPathXMirror
                || item.RightPathXMirror);
            if (configured != null)
                return configured;

            return models[0];
        }

        private static List<Result> ExtractResults(object rawResults)
        {
            if (rawResults is List<Result> single)
                return single;

            if (rawResults is List<List<Result>> multi)
            {
                return multi
                    .Where(item => item != null)
                    .SelectMany(item => item)
                    .ToList();
            }

            if (rawResults is IEnumerable enumerable && rawResults is not string)
            {
                List<Result> flattened = new();
                foreach (object item in enumerable)
                {
                    switch (item)
                    {
                        case Result result:
                            flattened.Add(result);
                            break;
                        case IEnumerable<Result> nested:
                            flattened.AddRange(nested.Where(result => result != null));
                            break;
                    }
                }

                return flattened;
            }

            return [];
        }

        private static bool ResolveIsNg(object isOks, List<Result> results)
        {
            if ((results?.Count ?? 0) > 0)
                return true;

            if (isOks is bool singleBool)
                return !singleBool;

            if (isOks is IEnumerable<bool> boolEnumerable)
            {
                List<bool> values = boolEnumerable.ToList();
                if (values.Count > 0)
                    return values.Any(value => !value);
            }

            if (isOks is IEnumerable enumerable && isOks is not string)
            {
                List<bool> flags = new();
                foreach (object item in enumerable)
                {
                    if (item is bool value)
                        flags.Add(value);
                }

                if (flags.Count > 0)
                    return flags.Any(value => !value);
            }

            return false;
        }

        private static HImage TryResolveConfiguredPathImage(
            XYHDInputPacket packet,
            XYHDFieldOrientationMap orientation,
            IReadOnlyList<Result> results,
            out string source)
        {
            source = null;
            DetectionModel model = ResolveDetectionModel(packet);
            if (model == null)
                return null;

            bool isRight = orientation.FieldRole == DefectOverviewPathRole.Right
                || IsRightPathText(orientation.FieldPathName)
                || IsRightPathText(packet?.PathName);
            TransmitParam imageInput = isRight ? model.RightInputImage : model.LeftInputImage;
            string configuredName = isRight ? model.RightInputImageName : model.LeftInputImageName;
            string defaultName = isRight ? "输入原图右" : "输入原图左";

            object imageValue = ResolveModelTransmitParamValue(model, imageInput);
            if (imageValue == null)
            {
                TransmitParam namedInput = FindInputByName(model.InputParams, configuredName)
                    ?? FindInputByName(model.InputParams, defaultName);
                imageValue = ResolveModelTransmitParamValue(model, namedInput);
                if (namedInput != null)
                    source = $"{defaultName}:{namedInput.Name ?? namedInput.ParamName ?? namedInput.Serial.ToString()}";
            }
            else
            {
                source = $"{defaultName}:{imageInput?.Name ?? imageInput?.ParamName ?? imageInput?.Serial.ToString() ?? "Selected"}";
            }

            int imageIndex = ResolveFirstResultImageIndex(results, 0);
            HImage image = TryExtractHImageAt(imageValue, imageIndex);
            if (image != null && image.IsInitialized())
            {
                source ??= $"{defaultName}[{imageIndex}]";
                return image;
            }

            image?.Dispose();
            return null;
        }

        private static DetectionModel ResolveDetectionModel(XYHDInputPacket packet)
        {
            try
            {
                var caches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
                if (caches == null || caches.Count == 0)
                    return null;

                List<DetectionModel> models = caches.Values
                    .OfType<DetectionModel>()
                    .OrderBy(item => item.Serial < 0 ? int.MaxValue : item.Serial)
                    .ToList();

                return ResolveDetectionModelFromCache(models, packet);
            }
            catch
            {
                return null;
            }
        }

        private static object ResolveModelTransmitParamValue(ModelParamBase model, TransmitParam transmitParam)
        {
            if (model == null || transmitParam == null)
                return null;

            if (transmitParam.Value != null)
                return transmitParam.Value;

            try
            {
                return model.GetTransmitParam(model.InputParams, transmitParam, false);
            }
            catch
            {
                return null;
            }
        }

        private static TransmitParam FindInputByName(IEnumerable<TransmitParam> inputs, string expectedName)
        {
            if (inputs == null || string.IsNullOrWhiteSpace(expectedName))
                return null;

            return inputs.FirstOrDefault(input => NameMatches(input?.Name, expectedName)
                || NameMatches(input?.ParamName, expectedName));
        }

        private static bool NameMatches(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
                return false;

            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
                || actual.Contains(expected, StringComparison.OrdinalIgnoreCase)
                || expected.Contains(actual, StringComparison.OrdinalIgnoreCase);
        }

        private static HImage TryExtractHImageAt(object input, int imageIndex)
        {
            if (input == null || input is string)
                return null;

            if (input is HImage hImage && hImage.IsInitialized())
                return hImage.CopyImage();

            if (input is HObject hObject && hObject.IsInitialized())
                return TryExtractHImageFromHObjectAt(hObject, imageIndex);

            if (input is Array array)
            {
                if (array.Length == 0)
                    return null;

                int safeIndex = Math.Clamp(imageIndex, 0, array.Length - 1);
                return TryExtractHImageAt(array.GetValue(safeIndex), 0);
            }

            if (input is IList list)
            {
                if (list.Count == 0)
                    return null;

                int safeIndex = Math.Clamp(imageIndex, 0, list.Count - 1);
                return TryExtractHImageAt(list[safeIndex], 0);
            }

            if (input is IEnumerable enumerable)
            {
                int safeIndex = Math.Max(0, imageIndex);
                int index = 0;
                object last = null;
                foreach (object item in enumerable)
                {
                    last = item;
                    if (index == safeIndex)
                        return TryExtractHImageAt(item, 0);
                    index++;
                }

                return last == null ? null : TryExtractHImageAt(last, 0);
            }

            return null;
        }

        private static HImage TryExtractHImageFromHObjectAt(HObject hObject, int imageIndex)
        {
            HObject selectedObject = null;
            HImage selectedImage = null;

            try
            {
                HOperatorSet.CountObj(hObject, out HTuple countTuple);
                int count = countTuple.TupleLength() == 0 ? 0 : countTuple[0].I;
                if (count <= 0)
                    return null;

                int selectIndex = Math.Clamp(imageIndex + 1, 1, count);
                HOperatorSet.SelectObj(hObject, out selectedObject, selectIndex);
                if (selectedObject == null || !selectedObject.IsInitialized())
                    return null;

                selectedImage = new HImage(selectedObject);
                return selectedImage != null && selectedImage.IsInitialized()
                    ? selectedImage.CopyImage()
                    : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                selectedImage?.Dispose();
                selectedObject?.Dispose();
            }
        }

        private static int ResolveFirstResultImageIndex(IEnumerable<Result> results, int fallback)
        {
            if (results == null)
                return fallback;

            foreach (Result result in results)
            {
                int index = ResolveResultImageIndex(result, fallback);
                if (index >= 0)
                    return index;
            }

            return fallback;
        }

        private static int ResolveResultImageIndex(Result result, int fallback)
        {
            if (result?.Others == null)
                return fallback;

            foreach (string key in new[] { "DefectPostProcess.ImageIndex", "XYHD_SourceImageIndex", "ImageIndex", "TargetIndex" })
            {
                if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                    continue;

                try
                {
                    int index = Convert.ToInt32(rawValue);
                    if (index >= 0)
                        return index;
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static bool IsRightPathText(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && (text.Contains("右", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("right", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("path2", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("lane2", StringComparison.OrdinalIgnoreCase));
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

        private static double ResolveLaneWidth(HImage pathImage, List<Result> results)
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

            double fallback = results.Count == 0
                ? 1.0
                : results.Max(item => item.Cx + Math.Max(1.0, item.Width / 2.0));

            return Math.Max(1.0, fallback);
        }
    }
}

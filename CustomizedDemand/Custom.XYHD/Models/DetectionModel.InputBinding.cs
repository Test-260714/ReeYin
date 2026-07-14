using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {
        private string _inputOriginalImageName = DefaultOriginalImageInputName;
        /// <summary>
        /// 输入口：原图
        /// </summary>
        public string InputOriginalImageName
        {
            get => EffectiveName(_inputOriginalImageName, DefaultOriginalImageInputName);
            set => SetProperty(ref _inputOriginalImageName, EffectiveName(value, DefaultOriginalImageInputName));
        }

        [JsonIgnore]
        private TransmitParam _inputOriginalImage = new TransmitParam();
        /// <summary>
        /// 输入口链接：原图（可在界面中选择变量）
        /// </summary>
        [InputParam("原图", "整帧原图", false)]
        public TransmitParam InputOriginalImage
        {
            get => _inputOriginalImage;
            set
            {
                if (SetProperty(ref _inputOriginalImage, value ?? new TransmitParam()))
                    SyncInputNameFromLink(ref _inputOriginalImageName, _inputOriginalImage, nameof(InputOriginalImageName));
            }
        }

        private string _leftInputImageName = DefaultLeftImageInputName;
        public string LeftInputImageName
        {
            get => EffectiveName(_leftInputImageName, DefaultLeftImageInputName);
            set => SetProperty(ref _leftInputImageName, EffectiveName(value, DefaultLeftImageInputName));
        }

        [JsonIgnore]
        private TransmitParam _leftInputImage = new TransmitParam();
        [InputParam("左路图像", "左路缺陷墙裁图图像", false)]
        public TransmitParam LeftInputImage
        {
            get => _leftInputImage;
            set
            {
                if (SetProperty(ref _leftInputImage, value ?? new TransmitParam()))
                    SyncInputNameFromLink(ref _leftInputImageName, _leftInputImage, nameof(LeftInputImageName));
            }
        }

        private string _leftInputResultsName = DefaultLeftResultsInputDisplayName;
        public string LeftInputResultsName
        {
            get => EffectiveName(_leftInputResultsName, DefaultLeftResultsInputDisplayName);
            set => SetProperty(ref _leftInputResultsName, EffectiveName(value, DefaultLeftResultsInputDisplayName));
        }

        [JsonIgnore]
        private TransmitParam _leftInputResults = new TransmitParam();
        [InputParam("左路结果", "左路缺陷结果", false)]
        public TransmitParam LeftInputResults
        {
            get => _leftInputResults;
            set
            {
                if (SetProperty(ref _leftInputResults, value ?? new TransmitParam()))
                    SyncInputNameFromLink(ref _leftInputResultsName, _leftInputResults, nameof(LeftInputResultsName));
            }
        }

        private string _rightInputImageName = DefaultRightImageInputName;
        public string RightInputImageName
        {
            get => EffectiveName(_rightInputImageName, DefaultRightImageInputName);
            set => SetProperty(ref _rightInputImageName, EffectiveName(value, DefaultRightImageInputName));
        }

        [JsonIgnore]
        private TransmitParam _rightInputImage = new TransmitParam();
        [InputParam("右路图像", "右路缺陷墙裁图图像", false)]
        public TransmitParam RightInputImage
        {
            get => _rightInputImage;
            set
            {
                if (SetProperty(ref _rightInputImage, value ?? new TransmitParam()))
                    SyncInputNameFromLink(ref _rightInputImageName, _rightInputImage, nameof(RightInputImageName));
            }
        }

        private string _rightInputResultsName = DefaultRightResultsInputDisplayName;
        public string RightInputResultsName
        {
            get => EffectiveName(_rightInputResultsName, DefaultRightResultsInputDisplayName);
            set => SetProperty(ref _rightInputResultsName, EffectiveName(value, DefaultRightResultsInputDisplayName));
        }

        [JsonIgnore]
        private TransmitParam _rightInputResults = new TransmitParam();
        [InputParam("右路结果", "右路缺陷结果", false)]
        public TransmitParam RightInputResults
        {
            get => _rightInputResults;
            set
            {
                if (SetProperty(ref _rightInputResults, value ?? new TransmitParam()))
                    SyncInputNameFromLink(ref _rightInputResultsName, _rightInputResults, nameof(RightInputResultsName));
            }
        }

        public void TryRebindInputLinks()
        {
            InputOriginalImage = RebindSingleInputByFixedName(InputOriginalImage, InputOriginalImageName, DefaultOriginalImageInputName, useFixedFallback: false);
            LeftInputImage = RebindSingleInputByFixedName(LeftInputImage, LeftInputImageName, DefaultLeftImageInputName);
            LeftInputResults = RebindSingleInputByFixedSerial(LeftInputResults, LeftInputResultsName, DefaultLeftResultsSerial, DefaultResultsByImageInputName, DefaultLeftResultsInputDisplayName);
            RightInputImage = RebindSingleInputByFixedName(RightInputImage, RightInputImageName, DefaultRightImageInputName);
            RightInputResults = RebindSingleInputByFixedSerial(RightInputResults, RightInputResultsName, DefaultRightResultsSerial, DefaultResultsByImageInputName, DefaultRightResultsInputDisplayName);
        }

        private TransmitParam RebindSingleInputByFixedName(TransmitParam selected, string fixedName)
        {
            return RebindSingleInputByFixedName(selected, fixedName, fixedName);
        }

        private TransmitParam RebindSingleInputByFixedName(
            TransmitParam selected,
            string configuredName,
            string fixedName,
            bool useFixedFallback = true)
        {
            selected ??= new TransmitParam();
            if (HasConfiguredSelection(selected))
                return RebindSingleInput(selected, configuredName);

            var configuredInput = FindInputByConfiguredName(configuredName, fixedName);
            if (configuredInput != null)
                return configuredInput;

            if (!useFixedFallback || HasUserConfiguredName(configuredName, fixedName))
                return RebindSingleInput(selected, configuredName);

            var fixedInput = FindInputByName(fixedName);
            return fixedInput ?? RebindSingleInput(selected, null);
        }

        private TransmitParam RebindSingleInputByFixedSerial(TransmitParam selected, int fixedSerial, string preferredParamName)
        {
            return RebindSingleInputByFixedSerial(selected, null, fixedSerial, preferredParamName, null);
        }

        private TransmitParam RebindSingleInputByFixedSerial(
            TransmitParam selected,
            string configuredName,
            int fixedSerial,
            string preferredParamName,
            string defaultDisplayName)
        {
            selected ??= new TransmitParam();
            if (HasConfiguredSelection(selected))
                return RebindSingleInput(selected, configuredName);

            var configuredInput = FindInputByConfiguredName(configuredName, preferredParamName);
            if (configuredInput != null)
                return configuredInput;

            if (HasUserConfiguredName(configuredName, defaultDisplayName)
                && !NameMatch(configuredName, preferredParamName))
            {
                return RebindSingleInput(selected, configuredName);
            }

            var fixedInput = FindInputBySerial(fixedSerial, preferredParamName);
            return fixedInput ?? RebindSingleInput(selected, null);
        }

        private TransmitParam RebindSingleInput(TransmitParam selected, string fallbackName)
        {
            selected ??= new TransmitParam();
            if (InputParams == null || InputParams.Count == 0)
                return selected;

            if (selected.Guid != Guid.Empty)
            {
                var byGuid = InputParams.FirstOrDefault(p => p.Guid == selected.Guid);
                if (byGuid != null)
                    return byGuid;
            }

            string selectedName = EffectiveName(selected.Name, selected.ParamName);
            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                var bySelectedName = InputParams.FirstOrDefault(p =>
                    ParamNameMatch(p, selectedName)
                    && (selected.Serial < 0 || p.Serial == selected.Serial));
                bySelectedName ??= selected.Serial < 0
                    ? InputParams.FirstOrDefault(p => ParamNameMatch(p, selectedName))
                    : null;
                if (bySelectedName != null)
                    return bySelectedName;
            }

            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                var byFallbackName = InputParams.FirstOrDefault(p => ParamNameMatch(p, fallbackName));
                if (byFallbackName != null)
                    return byFallbackName;
            }

            return selected;
        }

        public void SyncInputNamesFromLinks()
        {
            InputOriginalImageName = ResolveInputNameForSync(InputOriginalImage, _inputOriginalImageName, DefaultOriginalImageInputName);
            LeftInputImageName = ResolveInputNameForSync(LeftInputImage, _leftInputImageName, DefaultLeftImageInputName);
            LeftInputResultsName = ResolveInputNameForSync(LeftInputResults, _leftInputResultsName, DefaultLeftResultsInputDisplayName);
            RightInputImageName = ResolveInputNameForSync(RightInputImage, _rightInputImageName, DefaultRightImageInputName);
            RightInputResultsName = ResolveInputNameForSync(RightInputResults, _rightInputResultsName, DefaultRightResultsInputDisplayName);
        }

        private static string ResolveInputNameForSync(TransmitParam input, string currentName, string defaultName)
        {
            string linkName = BuildInputNameForStorage(input);
            return string.IsNullOrWhiteSpace(linkName)
                ? EffectiveName(currentName, defaultName)
                : linkName;
        }

        private void SyncInputNameFromLink(ref string targetName, TransmitParam input, string propertyName)
        {
            string name = BuildInputNameForStorage(input);
            if (string.IsNullOrWhiteSpace(name) || string.Equals(targetName, name, StringComparison.Ordinal))
                return;

            targetName = name;
            RaisePropertyChanged(propertyName);
        }

        private static string BuildInputNameForStorage(TransmitParam input)
        {
            if (input == null)
                return null;

            if (!string.IsNullOrWhiteSpace(input.Name)
                && !NameMatch(input.Name, input.ParamName))
            {
                return input.Name;
            }

            if (!string.IsNullOrWhiteSpace(input.ParamName))
            {
                return input.Serial > 0
                    ? $"{input.ParamName}@{input.Serial}"
                    : input.ParamName;
            }

            return input.Name;
        }

        private object GetSelectedInputValue(TransmitParam selectedParam, string fallbackName)
        {
            return GetSelectedInputValue(selectedParam, fallbackName, -1, out _, out _);
        }

        private object GetSelectedInputValue(
            TransmitParam selectedParam,
            string fallbackName,
            int fallbackSerial,
            out string source,
            out TransmitParam matchedParam)
        {
            source = null;
            matchedParam = null;

            bool selectedConfigured = selectedParam != null && HasConfiguredSelection(selectedParam);
            if (selectedConfigured)
            {
                var liveValue = TryResolveSelectedInputValue(selectedParam, out source, out matchedParam);
                if (liveValue != null)
                    return liveValue;

                if (selectedParam.Value != null)
                {
                    source = $"SelectedLocal:{DescribeInput(selectedParam, fallbackName)}";
                    matchedParam = selectedParam;
                    return selectedParam.Value;
                }
            }

            if (selectedConfigured && !SelectionMatchesFallback(selectedParam, fallbackName, fallbackSerial))
                return null;

            if (fallbackSerial >= 0 || !string.IsNullOrWhiteSpace(fallbackName))
            {
                var fallbackValue = TryResolveRuntimeValue(fallbackSerial, fallbackName, out source, out matchedParam);
                if (fallbackValue != null)
                    return fallbackValue;
            }

            var byName = GetInputValueByName(fallbackName);
            if (byName != null)
            {
                source = $"CurrentInput:{fallbackName}";
                return byName;
            }

            return null;
        }

        private object GetInputValueByFixedName(string fixedName)
        {
            return GetInputValueByFixedName(fixedName, out _);
        }

        private object GetInputValueByFixedName(string fixedName, out string source)
        {
            source = null;
            if (string.IsNullOrWhiteSpace(fixedName))
                return null;

            var fixedInput = FindInputByName(fixedName);
            if (fixedInput != null)
            {
                var fixedValue = GetInputValue(fixedInput);
                if (fixedValue != null)
                {
                    source = $"当前输入:{DescribeInput(fixedInput, fixedName)}";
                    return fixedValue;
                }
            }

            var namedValue = GetInputValueByName(fixedName);
            if (namedValue != null)
            {
                source = $"当前输入:{fixedName}";
                return namedValue;
            }

            var ancestorValue = TryResolveAncestorGraphValue(-1, fixedName, out source, out _);
            if (ancestorValue != null)
                return ancestorValue;

            return TryFindRuntimeCacheValueByName(fixedName, out source);
        }

        private object GetInputValueByFixedSerial(int fixedSerial, string preferredParamName)
        {
            var fixedInput = FindInputBySerial(fixedSerial, preferredParamName);
            if (fixedInput != null)
            {
                var fixedValue = GetInputValue(fixedInput);
                if (fixedValue != null)
                    return fixedValue;
            }

            return TryResolveRuntimeValue(fixedSerial, preferredParamName, out _, out _);
        }

        private object GetInputValue(TransmitParam input)
        {
            if (input == null)
                return null;

            var liveValue = GetTransmitParam(InputParams, input);
            return liveValue ?? input.Value;
        }

        private object TryResolveSelectedInputValue(
            TransmitParam selectedParam,
            out string source,
            out TransmitParam matchedParam)
        {
            source = null;
            matchedParam = null;
            if (selectedParam == null)
                return null;

            var liveValue = GetTransmitParam(InputParams, selectedParam);
            if (liveValue != null)
            {
                source = $"CurrentInput:{DescribeInput(selectedParam)}";
                matchedParam = selectedParam;
                return liveValue;
            }

            if (TryFindValueInTransmitParams(
                    EnumerateInputCandidates(),
                    selectedParam,
                    selectedParam.Serial >= 0,
                    out object currentValue,
                    out matchedParam)
                && currentValue != null)
            {
                source = $"CurrentInput:{DescribeInput(matchedParam)}";
                return currentValue;
            }

            string expectedName = EffectiveName(selectedParam.Name, selectedParam.ParamName);
            return TryResolveRuntimeValue(selectedParam.Serial, expectedName, out source, out matchedParam);
        }

        private object TryResolveRuntimeValue(
            int serial,
            string expectedName,
            out string source,
            out TransmitParam matchedParam)
        {
            source = null;
            matchedParam = null;

            object ancestorValue = TryResolveAncestorGraphValue(serial, expectedName, out source, out matchedParam);
            if (ancestorValue != null)
                return ancestorValue;

            object cacheValue = TryFindRuntimeCacheValue(serial, expectedName, out source, out matchedParam);
            if (cacheValue != null)
                return cacheValue;

            return null;
        }

        private object GetInputValueByName(string expectedName)
        {
            if (string.IsNullOrWhiteSpace(expectedName))
                return null;

            var linkedInputs = moduleInputParam?.TransmitParams?.Values?.OfType<TransmitParam>() ?? Enumerable.Empty<TransmitParam>();
            var linkedParam = linkedInputs.FirstOrDefault(p => ParamNameMatch(p, expectedName));
            if (linkedParam != null)
                return linkedParam.Value;

            var cachedParam = InputParams?.FirstOrDefault(p => ParamNameMatch(p, expectedName));
            return cachedParam?.Value;
        }

        private object TryFindRuntimeCacheValueByName(string expectedName, out string source)
        {
            source = null;
            if (string.IsNullOrWhiteSpace(expectedName))
                return null;

            try
            {
                var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solutionItem == null)
                    return null;

                foreach (var cache in EnumerateRuntimeCacheEntries(solutionItem.NodesOutputCache))
                {
                    var value = TryFindValueInTransmitParams(cache.Value, expectedName, out var matchedName);
                    if (value != null)
                    {
                        source = $"NodesOutputCache[{cache.Key}]:{matchedName}";
                        return value;
                    }
                }

                foreach (var cache in EnumerateRuntimeCacheEntries(solutionItem.NodeParamCaches))
                {
                    if (cache.Value is not ModelParamBase model)
                        continue;

                    var value = TryFindValueInTransmitParams(model.OutputParams, expectedName, out var matchedName);
                    if (value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].Output:{matchedName}";
                        return value;
                    }

                    value = TryFindValueInModuleParam(model.moduleOutputParam, expectedName, out matchedName);
                    if (value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].ModuleOutput:{matchedName}";
                        return value;
                    }

                    value = TryFindValueInTransmitParams(model.InputParams, expectedName, out matchedName);
                    if (value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].Input:{matchedName}";
                        return value;
                    }

                    value = TryFindValueInModuleParam(model.moduleInputParam, expectedName, out matchedName);
                    if (value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].ModuleInput:{matchedName}";
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"[固定输入] 从运行时缓存读取 {expectedName} 异常: {ex.Message}", "WARN");
            }

            return null;
        }

        private object TryFindRuntimeCacheValue(
            int serial,
            string expectedName,
            out string source,
            out TransmitParam matchedParam)
        {
            source = null;
            matchedParam = null;
            if (serial < 0 && string.IsNullOrWhiteSpace(expectedName))
                return null;

            try
            {
                var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solutionItem == null)
                    return null;

                foreach (var cache in EnumerateRuntimeCacheEntries(solutionItem.NodesOutputCache, serial))
                {
                    if (TryFindValueInTransmitParams(
                            cache.Value,
                            serial,
                            expectedName,
                            serial >= 0,
                            out object value,
                            out matchedParam)
                        && value != null)
                    {
                        source = $"NodesOutputCache[{cache.Key}]:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }
                }

                foreach (var cache in EnumerateRuntimeCacheEntries(solutionItem.NodeParamCaches, serial))
                {
                    if (cache.Value is not ModelParamBase model)
                        continue;

                    if (TryFindValueInTransmitParams(model.OutputParams, serial, expectedName, serial >= 0, out object value, out matchedParam)
                        && value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].Output:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }

                    if (TryFindValueInModuleParam(model.moduleOutputParam, serial, expectedName, serial >= 0, out value, out matchedParam)
                        && value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].ModuleOutput:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }

                    if (TryFindValueInTransmitParams(model.InputParams, serial, expectedName, serial >= 0, out value, out matchedParam)
                        && value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].Input:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }

                    if (TryFindValueInModuleParam(model.moduleInputParam, serial, expectedName, serial >= 0, out value, out matchedParam)
                        && value != null)
                    {
                        source = $"NodeParamCaches[{cache.Key}].ModuleInput:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ExplicitInput] cache lookup failed. Serial={serial}, Name={expectedName}, Error={ex.Message}", "WARN");
            }

            return null;
        }

        private object TryResolveAncestorGraphValue(
            int serial,
            string expectedName,
            out string source,
            out TransmitParam matchedParam)
        {
            source = null;
            matchedParam = null;
            if (serial < 0 && string.IsNullOrWhiteSpace(expectedName))
                return null;

            try
            {
                var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solution?.NodeCaches is not IEnumerable nodeCaches || solution.NodeCaches is string)
                    return null;

                List<object> nodes = nodeCaches.Cast<object>().Where(item => item != null).ToList();
                object currentNode = nodes.FirstOrDefault(node => GetNodeSerial(node) == Serial);
                if (currentNode == null)
                    return null;

                foreach (object node in EnumerateAncestorNodes(currentNode))
                {
                    int nodeSerial = GetNodeSerial(node);
                    if (serial >= 0 && nodeSerial >= 0 && nodeSerial != serial)
                        continue;

                    if (TryFindValueInTransmitParams(
                            GetNodeOutputParams(node),
                            serial,
                            expectedName,
                            serial >= 0,
                            out object value,
                            out matchedParam)
                        && value != null)
                    {
                        source = $"AncestorLive[{nodeSerial:D3}]:{DescribeInput(matchedParam, expectedName)}";
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ExplicitInput] ancestor lookup failed. Serial={serial}, Name={expectedName}, Error={ex.Message}", "WARN");
            }

            return null;
        }

        private IEnumerable<KeyValuePair<string, T>> EnumerateRuntimeCacheEntries<T>(IDictionary<string, T> caches)
        {
            if (caches == null || caches.Count == 0)
                yield break;

            foreach (var item in caches
                .Where(item => !IsCurrentCacheKey(item.Key))
                .OrderBy(item => ResolveCachePriority(item.Key)))
            {
                yield return item;
            }
        }

        private IEnumerable<KeyValuePair<string, T>> EnumerateRuntimeCacheEntries<T>(IDictionary<string, T> caches, int serial)
        {
            if (caches == null || caches.Count == 0)
                yield break;

            if (serial >= 0)
            {
                foreach (string cacheKey in EnumerateSourceCacheKeys(serial))
                {
                    if (caches.TryGetValue(cacheKey, out T value) && value != null && !IsCurrentCacheKey(cacheKey))
                        yield return new KeyValuePair<string, T>(cacheKey, value);
                }

                yield break;
            }

            foreach (var item in EnumerateRuntimeCacheEntries(caches))
                yield return item;
        }

        private bool IsCurrentCacheKey(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                return false;

            if (string.Equals(cacheKey, Serial.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(cacheKey, Serial.ToString("D3", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                return true;

            return int.TryParse(cacheKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cacheSerial)
                && cacheSerial == Serial;
        }

        private static int ResolveCachePriority(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                return int.MaxValue;

            return int.TryParse(cacheKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int serial)
                ? serial
                : int.MaxValue;
        }

        private static IEnumerable<string> EnumerateSourceCacheKeys(int sourceSerial)
        {
            if (sourceSerial < 0)
                yield break;

            HashSet<string> yieldedKeys = new(StringComparer.OrdinalIgnoreCase);
            string rawKey = sourceSerial.ToString(CultureInfo.InvariantCulture);
            if (yieldedKeys.Add(rawKey))
                yield return rawKey;

            string paddedKey = sourceSerial.ToString("D3", CultureInfo.InvariantCulture);
            if (yieldedKeys.Add(paddedKey))
                yield return paddedKey;
        }

        private static IEnumerable<object> EnumerateAncestorNodes(object currentNode)
        {
            Stack<object> stack = new();
            HashSet<int> visitedSerials = new();

            foreach (object parentNode in GetNodeCollectionProperty(currentNode, "LastNodes"))
                stack.Push(parentNode);

            while (stack.Count > 0)
            {
                object node = stack.Pop();
                if (node == null)
                    continue;

                int nodeSerial = GetNodeSerial(node);
                if (nodeSerial >= 0 && !visitedSerials.Add(nodeSerial))
                    continue;

                yield return node;

                foreach (object parentNode in GetNodeCollectionProperty(node, "LastNodes"))
                    stack.Push(parentNode);
            }
        }

        private static IEnumerable<object> GetNodeCollectionProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is not IEnumerable enumerable || value is string)
                yield break;

            foreach (object item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        private static IEnumerable<TransmitParam> GetNodeOutputParams(object node)
        {
            object moduleParamObject = node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
            if (moduleParamObject is ModelParamBase model)
            {
                foreach (TransmitParam param in model.OutputParams ?? Enumerable.Empty<TransmitParam>())
                {
                    if (param != null)
                        yield return param;
                }

                foreach (TransmitParam param in EnumerateModuleTransmitParams(model.moduleOutputParam))
                {
                    if (param != null)
                        yield return param;
                }

                yield break;
            }

            object outputParamsObject = moduleParamObject?.GetType().GetProperty("OutputParams")?.GetValue(moduleParamObject);
            if (outputParamsObject is not IEnumerable enumerable || outputParamsObject is string)
                yield break;

            foreach (object item in enumerable)
            {
                if (item is TransmitParam param)
                    yield return param;
            }
        }

        private static object GetNodeModuleParamObject(object node)
        {
            return node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
        }

        private static IEnumerable<TransmitParam> EnumerateModuleTransmitParams(ModuleParam moduleParam)
        {
            if (moduleParam?.TransmitParams == null)
                yield break;

            foreach (object value in moduleParam.TransmitParams.Values)
            {
                if (value is TransmitParam param)
                    yield return param;
            }
        }

        private static int GetNodeSerial(object node)
        {
            object moduleParamObject = node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
            object serialValue = moduleParamObject?.GetType().GetProperty("Serial")?.GetValue(moduleParamObject);
            if (serialValue is int moduleSerial)
                return moduleSerial;

            object menuInfoObject = node?.GetType().GetProperty("MenuInfo")?.GetValue(node);
            serialValue = menuInfoObject?.GetType().GetProperty("Serial")?.GetValue(menuInfoObject);
            return serialValue is int menuSerial
                ? menuSerial
                : -1;
        }

        private static object TryFindValueInModuleParam(ModuleParam moduleParam, string expectedName, out string matchedName)
        {
            matchedName = null;
            return TryFindValueInTransmitParams(
                moduleParam?.TransmitParams?.Values?.OfType<TransmitParam>(),
                expectedName,
                out matchedName);
        }

        private static bool TryFindValueInModuleParam(
            ModuleParam moduleParam,
            int serial,
            string expectedName,
            bool requireSerialMatch,
            out object value,
            out TransmitParam matchedParam)
        {
            return TryFindValueInTransmitParams(
                moduleParam?.TransmitParams?.Values?.OfType<TransmitParam>(),
                serial,
                expectedName,
                requireSerialMatch,
                out value,
                out matchedParam);
        }

        private static object TryFindValueInTransmitParams(IEnumerable<TransmitParam> transmitParams, string expectedName, out string matchedName)
        {
            matchedName = null;
            if (transmitParams == null || string.IsNullOrWhiteSpace(expectedName))
                return null;

            var matched = transmitParams.FirstOrDefault(p => ParamNameMatch(p, expectedName));
            if (matched?.Value == null)
                return null;

            matchedName = DescribeInput(matched, expectedName);
            return matched.Value;
        }

        private static bool TryFindValueInTransmitParams(
            IEnumerable<TransmitParam> transmitParams,
            TransmitParam selectedParam,
            bool requireSerialMatch,
            out object value,
            out TransmitParam matchedParam)
        {
            value = null;
            matchedParam = null;
            if (selectedParam == null)
                return false;

            List<TransmitParam> paramsList = transmitParams?
                .Where(item => item != null)
                .ToList();
            if (paramsList == null || paramsList.Count == 0)
                return false;

            matchedParam = paramsList.FirstOrDefault(item =>
                selectedParam.Guid != Guid.Empty
                && item.Guid == selectedParam.Guid);

            string expectedName = EffectiveName(selectedParam.Name, selectedParam.ParamName);
            if (matchedParam == null)
            {
                bool strictSerial = requireSerialMatch && selectedParam.Serial >= 0;
                matchedParam = paramsList.FirstOrDefault(item =>
                    ParamMatches(item, selectedParam.Serial, expectedName, strictSerial));
            }

            if (matchedParam?.Value == null)
                return false;

            value = matchedParam.Value;
            return true;
        }

        private static bool TryFindValueInTransmitParams(
            IEnumerable<TransmitParam> transmitParams,
            int serial,
            string expectedName,
            bool requireSerialMatch,
            out object value,
            out TransmitParam matchedParam)
        {
            value = null;
            matchedParam = null;

            List<TransmitParam> paramsList = transmitParams?
                .Where(item => item != null)
                .ToList();
            if (paramsList == null || paramsList.Count == 0)
                return false;

            matchedParam = paramsList.FirstOrDefault(item =>
                ParamMatches(item, serial, expectedName, requireSerialMatch));

            if (matchedParam?.Value == null)
                return false;

            value = matchedParam.Value;
            return true;
        }

        private static bool ParamMatches(TransmitParam param, int serial, string expectedName, bool requireSerialMatch)
        {
            if (param == null)
                return false;

            if (requireSerialMatch && serial >= 0 && param.Serial != serial)
                return false;

            if (string.IsNullOrWhiteSpace(expectedName))
                return serial >= 0 && param.Serial == serial;

            return ParamNameMatch(param, expectedName);
        }

        private TransmitParam FindInputByName(string expectedName)
        {
            if (string.IsNullOrWhiteSpace(expectedName))
                return null;

            return EnumerateInputCandidates()
                .FirstOrDefault(p => ParamNameMatch(p, expectedName));
        }

        private TransmitParam FindInputByConfiguredName(string configuredName, string preferredParamName)
        {
            if (string.IsNullOrWhiteSpace(configuredName))
                return null;

            var byName = FindInputByName(configuredName);
            if (byName != null)
                return byName;

            if (TryParseSerialFromDisplayName(configuredName, preferredParamName, out int serial))
                return FindInputBySerial(serial, preferredParamName);

            return null;
        }

        private TransmitParam FindInputBySerial(int serial, string preferredParamName)
        {
            if (serial <= 0)
                return null;

            var candidates = EnumerateInputCandidates()
                .Where(p => p.Serial == serial)
                .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates.FirstOrDefault(p => ParamNameMatch(p, preferredParamName))
                ?? candidates.FirstOrDefault(p => ParamNameContains(p, preferredParamName))
                ?? candidates.FirstOrDefault();
        }

        private IEnumerable<TransmitParam> EnumerateInputCandidates()
        {
            if (moduleInputParam?.TransmitParams != null)
            {
                foreach (var input in moduleInputParam.TransmitParams.Values.OfType<TransmitParam>())
                {
                    if (input != null)
                        yield return input;
                }
            }

            if (InputParams != null)
            {
                foreach (var input in InputParams)
                {
                    if (input != null)
                        yield return input;
                }
            }
        }

        private static bool HasUserConfiguredName(string configuredName, string defaultName)
        {
            if (string.IsNullOrWhiteSpace(configuredName))
                return false;

            if (string.IsNullOrWhiteSpace(defaultName))
                return true;

            return !NameMatch(configuredName, defaultName);
        }

        private static bool TryParseSerialFromDisplayName(string configuredName, string preferredParamName, out int serial)
        {
            serial = -1;
            if (string.IsNullOrWhiteSpace(configuredName))
                return false;

            int atIndex = configuredName.LastIndexOf('@');
            if (atIndex < 0 || atIndex >= configuredName.Length - 1)
                return false;

            string namePart = configuredName[..atIndex];
            if (!string.IsNullOrWhiteSpace(preferredParamName)
                && !NameMatch(namePart, preferredParamName))
            {
                return false;
            }

            return int.TryParse(configuredName[(atIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out serial)
                && serial > 0;
        }

        private static string DescribeInput(TransmitParam input, string fallbackName = null)
        {
            if (!string.IsNullOrWhiteSpace(input?.Name))
                return input.Name;

            if (!string.IsNullOrWhiteSpace(input?.ParamName))
                return input.ParamName;

            if (!string.IsNullOrWhiteSpace(fallbackName))
                return fallbackName;

            return "未选择";
        }

        private static string DescribeInputBindingForLog(TransmitParam input, string fallbackName = null)
        {
            if (input == null)
            {
                return $"{fallbackName ?? "-"}:null";
            }

            return $"{DescribeInput(input, fallbackName)}(Serial={input.Serial:D3}, ParamName={input.ParamName ?? "-"}, " +
                   $"Resource={input.Resourece}, Link={input.IsLink}, Value={input.Value?.GetType().Name ?? "null"})";
        }

        private string DescribeNodeOutputCacheKeysForLog()
        {
            try
            {
                var cache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
                if (cache == null)
                    return "null";

                return string.Join(",", cache.Keys.Take(30));
            }
            catch
            {
                return "读取失败";
            }
        }

        private static string DescribePathResultForLog(PathResult path)
        {
            return $"{path.pathName ?? "-"}(Serial={path.sourceSerial:D3}, IsOks={path.isOks?.Count ?? 0}, " +
                   $"Explicit={path.isOksExplicit}, Defects={path.results?.Count ?? 0}, " +
                   $"Image={(path.pathImage != null && path.pathImage.IsInitialized() ? "OK" : "Null")}, " +
                   $"Source={path.pathImageSource ?? "-"})";
        }

        private static bool HasConfiguredSelection(TransmitParam input)
        {
            if (input == null)
                return false;

            return input.IsLink
                || !string.IsNullOrWhiteSpace(input.Name)
                || !string.IsNullOrWhiteSpace(input.ParamName)
                || input.Value != null;
        }

        private static bool SelectionMatchesFallback(TransmitParam input, string fallbackName, int fallbackSerial)
        {
            if (input == null)
                return true;

            bool serialMatches = fallbackSerial < 0 || input.Serial < 0 || input.Serial == fallbackSerial;
            if (!serialMatches)
                return false;

            if (string.IsNullOrWhiteSpace(fallbackName))
                return true;

            string inputName = EffectiveName(input.Name, input.ParamName);
            return string.IsNullOrWhiteSpace(inputName)
                || NameMatch(inputName, fallbackName);
        }

        private static string DescribeFixedSerialInput(
            TransmitParam fixedInput,
            int fixedSerial,
            string preferredParamName,
            TransmitParam selectedInput = null)
        {
            if (selectedInput != null && HasConfiguredSelection(selectedInput))
                return $"{DescribeInput(selectedInput, preferredParamName)}(Serial={selectedInput.Serial:000})";

            if (fixedInput != null)
                return $"{DescribeInput(fixedInput, preferredParamName)}(Serial={fixedInput.Serial:000})";

            if (fixedSerial > 0)
                return $"{preferredParamName ?? "结果"}(Serial={fixedSerial:000})";

            return DescribeInput(selectedInput, preferredParamName);
        }

        private static bool NameMatch(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParamNameMatch(TransmitParam param, string expectedName)
        {
            return param != null
                && (NameMatch(param.Name, expectedName)
                    || NameMatch(param.ParamName, expectedName));
        }

        private static bool ParamNameContains(TransmitParam param, string expectedName)
        {
            return param != null
                && (NameContains(param.Name, expectedName)
                    || NameContains(param.ParamName, expectedName));
        }

        private static bool NameContains(string value, string expectedName)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(expectedName))
                return false;

            return value.IndexOf(expectedName.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string EffectiveName(string preferredName, string fallbackName)
        {
            return string.IsNullOrWhiteSpace(preferredName)
                ? fallbackName
                : preferredName;
        }
    }
}

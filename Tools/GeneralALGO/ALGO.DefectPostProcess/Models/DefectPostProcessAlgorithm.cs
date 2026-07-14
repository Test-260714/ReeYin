using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace ALGO.DefectPostProcess.Models
{
    public partial class DefectPostProcessModel
    {
        #region 算法处理

        #region 规则阈值与缺陷筛选
        private const string RelationAnd = "与";
        private const string RelationOr = "或";
        private const string PixelLengthUnit = "px";
        private const string ActualLengthUnit = "mm";
        private const string PixelAreaUnit = "px^2";
        private const string ActualAreaUnit = "mm^2";
        private const string DefaultMinimumValue = "0";
        private const string DefaultPixelLengthMaximum = "999999";
        private const string DefaultActualLengthMaximum = "20";
        private const string DefaultPixelAreaMaximum = "999999999";
        private const string DefaultActualAreaMaximum = "400";
        [JsonIgnore]
        private bool _isLoadingCurrentRule;

        [JsonIgnore]
        private bool _isRefreshingAreaThresholds;

        public List<DefectRuleConfig> DefectRuleConfigs { get; set; } = new List<DefectRuleConfig>();

        [JsonIgnore]
        public bool HasCurrentDefect
        {
            get { return CurrentDefect != null; }
        }

        private ObservableCollection<FeatureThresholdItem> _featureThresholds = CreateDefaultFeatureThresholds(PixelLengthUnit, PixelAreaUnit);
        public ObservableCollection<FeatureThresholdItem> FeatureThresholds
        {
            get { return _featureThresholds; }
            set
            {
                if (ReferenceEquals(_featureThresholds, value))
                {
                    return;
                }

                DetachFeatureThresholdEvents(_featureThresholds);
                _featureThresholds = value ?? CreateDefaultFeatureThresholds();
                AttachFeatureThresholdEvents(_featureThresholds);
                RaisePropertyChanged();

                if (!_isLoadingCurrentRule && !_isRefreshingAreaThresholds)
                {
                    SyncCurrentRuleConfigFromEditingState();
                    MarkSchemeDirty();
                    RequestEditingStateRefresh();
                }
            }
        }

        private int _previewResultCount;
        public int PreviewResultCount
        {
            get { return _previewResultCount; }
            set
            {
                if (SetProperty(ref _previewResultCount, value))
                {
                    RaisePropertyChanged(nameof(PreviewSummary));
                }
            }
        }

        [JsonIgnore]
        public string PreviewSummary
        {
            get
            {
                if (CurrentDefect == null)
                {
                    return $"当前预览数量: {PreviewResultCount}";
                }

                return $"当前预览数量: {PreviewResultCount}/{CurrentDefect.Count}";
            }
        }

        /// <summary>
        /// 加载当前选中缺陷对应的规则配置。
        /// </summary>
        private void LoadCurrentDefectRule()
        {
            _isLoadingCurrentRule = true;

            try
            {
                if (CurrentDefect == null)
                {
                    FeatureThresholds = CreateDefaultFeatureThresholds();
                    MinimumConfidence = 0;
                    IsNmsEnabled = true;
                    NmsIoUThreshold = 0.5d;
                    return;
                }

                DefectRuleConfig currentRuleConfig = GetOrCreateRuleConfig(CurrentDefect);
                FeatureThresholds = currentRuleConfig.FeatureThresholds;
                MinimumConfidence = currentRuleConfig.MinimumConfidence;
                IsNmsEnabled = currentRuleConfig.IsNmsEnabled;
                NmsIoUThreshold = currentRuleConfig.NmsIoUThreshold;
            }
            finally
            {
                _isLoadingCurrentRule = false;
            }
        }

        /// <summary>
        /// 获取当前预览需要显示的缺陷结果。
        /// </summary>
        private List<Result> GetPreviewResults()
        {
            return GetPreviewResultsCore(preferExecutionResults: false);
        }

        /// <summary>
        /// 获取当前预览需要显示的缺陷结果；执行后刷新优先复用已生成结果，避免重复 NMS。
        /// </summary>
        private List<Result> GetPreviewResultsCore(bool preferExecutionResults)
        {
            if (preferExecutionResults && Results != null)
            {
                IEnumerable<Result> executionResults = Results.Where(IsResultOnCurrentPreviewImage);
                if (CurrentDefect != null)
                {
                    string currentRuleKey = GetDefectRuleKey(CurrentDefect);
                    executionResults = executionResults.Where(item =>
                        string.Equals(GetResultRuleKey(item), currentRuleKey, StringComparison.Ordinal));
                }

                return executionResults
                    .Where(IsPreviewDrawableResult)
                    .ToList();
            }

            if (CurrentDefect != null)
            {
                DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
                IEnumerable<Result> currentResults = (CurrentDefect.ResultItems ?? Enumerable.Empty<Result>())
                    .Where(IsResultOnCurrentPreviewImage);

                return BuildMatchedAndMergedResults(currentResults, currentRuleConfig)
                    .Where(IsPreviewDrawableResult)
                    .ToList();
            }

            IEnumerable<Result> sourceResults = (SourceResults ?? new List<Result>())
                .Where(IsResultOnCurrentPreviewImage);

            return BuildMatchedAndMergedResults(sourceResults)
                .Where(IsPreviewDrawableResult)
                .ToList();
        }

        /// 按当前规则构建筛选后的缺陷结果列表。
        /// </summary>
        private List<Result> BuildFilteredResults()
        {
            return BuildMatchedAndMergedResults(SourceResults);
        }

        /// <summary>
        /// 对多类缺陷按规则筛选并按类别分别执行 NMS 合并。
        /// </summary>
        private List<Result> BuildMatchedAndMergedResults(IEnumerable<Result> source)
        {
            List<Result> matchedResults = new List<Result>();
            Dictionary<string, List<Result>> groupedResults = new Dictionary<string, List<Result>>();
            Dictionary<string, DefectRuleConfig> ruleConfigMap = new Dictionary<string, DefectRuleConfig>();
            Dictionary<string, DefectRuleConfig> configuredRuleMap = CreateRuleConfigMap();
            List<string> ruleOrder = new List<string>();

            foreach (Result result in GetProcessableResults(source))
            {
                DefectRuleConfig ruleConfig = GetRuleConfigForResult(result, configuredRuleMap);
                string ruleKey = GetResultRuleKey(result);
                if (!groupedResults.TryGetValue(ruleKey, out List<Result> resultGroup))
                {
                    resultGroup = new List<Result>();
                    groupedResults.Add(ruleKey, resultGroup);
                    ruleConfigMap[ruleKey] = ruleConfig;
                    ruleOrder.Add(ruleKey);
                }

                resultGroup.Add(result);
            }

            foreach (string ruleKey in ruleOrder)
            {
                foreach (IGrouping<int, Result> imageGroup in GroupResultsByImageIndex(groupedResults[ruleKey]))
                {
                    List<Result> mergedResults = BuildMergedResultsByNms(imageGroup, ruleConfigMap[ruleKey]);
                    matchedResults.AddRange(FilterMatchedResults(mergedResults, ruleConfigMap[ruleKey]));
                }
            }

            return matchedResults;
        }

        /// <summary>
        /// 对单类缺陷按规则筛选并执行 NMS 合并。
        /// </summary>
        private List<Result> BuildMatchedAndMergedResults(IEnumerable<Result> source, DefectRuleConfig ruleConfig)
        {
            List<Result> matchedResults = new List<Result>();
            foreach (IGrouping<int, Result> imageGroup in GroupResultsByImageIndex(source))
            {
                List<Result> mergedResults = BuildMergedResultsByNms(imageGroup, ruleConfig);
                matchedResults.AddRange(FilterMatchedResults(mergedResults, ruleConfig));
            }

            return matchedResults;
        }

        private IEnumerable<IGrouping<int, Result>> GroupResultsByImageIndex(IEnumerable<Result> results)
        {
            return GetProcessableResults(results).GroupBy(GetResultImageIndex);
        }

        /// <summary>
        /// 从结果集合中保留满足规则的结果，并释放未命中的临时结果。
        /// </summary>
        private List<Result> FilterMatchedResults(IEnumerable<Result> source, DefectRuleConfig ruleConfig)
        {
            List<Result> matchedResults = new List<Result>();
            List<FeatureThresholdItem> enabledRules = GetEnabledFeatureRules(ruleConfig);
            foreach (Result result in source ?? Enumerable.Empty<Result>())
            {
                if (IsResultMatched(result, ruleConfig, enabledRules))
                {
                    matchedResults.Add(result);
                    continue;
                }

                DisposeResult(result);
            }

            return matchedResults;
        }

        /// <summary>
        /// 对同类缺陷结果执行 NMS 合并，返回新的结果集合。
        /// </summary>
        private List<Result> BuildMergedResultsByNms(IEnumerable<Result> source, DefectRuleConfig ruleConfig)
        {
            List<Result> clonedResults = GetProcessableResults(source)
                .Select(CloneResult)
                .ToList();

            bool isNmsEnabled = ruleConfig?.IsNmsEnabled ?? true;
            if (clonedResults.Count <= 1 || !isNmsEnabled)
            {
                return clonedResults;
            }

            double iouThreshold = System.Math.Clamp(ruleConfig?.NmsIoUThreshold ?? 0.5d, 0d, 1d);
            List<Result> pendingResults = clonedResults
                .OrderByDescending(item => item?.Confidence ?? 0f)
                .ToList();
            List<Result> mergedResults = new List<Result>();
            using NmsRegionCache regionCache = new NmsRegionCache();

            while (pendingResults.Count > 0)
            {
                Result seedResult = pendingResults[0];
                pendingResults.RemoveAt(0);

                List<Result> clusterResults = new List<Result> { seedResult };
                bool hasClusterExpanded;
                do
                {
                    hasClusterExpanded = false;
                    for (int i = pendingResults.Count - 1; i >= 0; i--)
                    {
                        if (ShouldMergeIntoCluster(clusterResults, pendingResults[i], iouThreshold, regionCache))
                        {
                            clusterResults.Add(pendingResults[i]);
                            pendingResults.RemoveAt(i);
                            hasClusterExpanded = true;
                        }
                    }
                }
                while (hasClusterExpanded);

                if (clusterResults.Count == 1)
                {
                    mergedResults.Add(clusterResults[0]);
                    continue;
                }

                Result mergedResult = MergeResultCluster(clusterResults);
                if (mergedResult != null)
                {
                    mergedResults.Add(mergedResult);
                }

                foreach (Result result in clusterResults)
                {
                    DisposeResult(result);
                }
            }

            return mergedResults;
        }

        /// <summary>
        /// 判断候选结果是否应并入当前聚类。
        /// </summary>
        private bool ShouldMergeIntoCluster(
            IEnumerable<Result> clusterResults,
            Result candidateResult,
            double iouThreshold,
            NmsRegionCache regionCache)
        {
            if (candidateResult == null)
            {
                return false;
            }

            foreach (Result clusterResult in clusterResults)
            {
                if (TryGetSegmentationIoU(clusterResult, candidateResult, regionCache, out double iou) && iou > iouThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算两个分割结果之间的 IoU。
        /// </summary>
        private static bool TryGetSegmentationIoU(
            Result leftResult,
            Result rightResult,
            NmsRegionCache regionCache,
            out double iou)
        {
            iou = 0d;
            if (!IsInitializedSafely(leftResult?.Seg) || !IsInitializedSafely(rightResult?.Seg))
            {
                return false;
            }

            HObject intersectionRegion = null;

            try
            {
                if (regionCache != null
                    && regionCache.TryRejectByBounds(leftResult, rightResult))
                {
                    return true;
                }

                HObject leftRegion = regionCache?.GetUnionRegion(leftResult);
                HObject rightRegion = regionCache?.GetUnionRegion(rightResult);
                if (!IsInitializedSafely(leftRegion) || !IsInitializedSafely(rightRegion))
                {
                    return false;
                }

                HOperatorSet.Intersection(leftRegion, rightRegion, out intersectionRegion);
                double intersectionArea = GetRegionArea(intersectionRegion);
                double unionArea = (regionCache?.GetArea(leftResult) ?? GetRegionArea(leftRegion))
                    + (regionCache?.GetArea(rightResult) ?? GetRegionArea(rightRegion))
                    - intersectionArea;
                if (unionArea <= 0)
                {
                    return false;
                }

                iou = intersectionArea / unionArea;
                return !double.IsNaN(iou) && !double.IsInfinity(iou);
            }
            catch
            {
                return false;
            }
            finally
            {
                SafeDisposeHObject(intersectionRegion);
            }
        }

        /// <summary>
        /// 缓存 NMS 中重复使用的区域并集、面积和外接框。
        /// </summary>
        private sealed class NmsRegionCache : IDisposable
        {
            private readonly Dictionary<Result, HObject> _unionRegions = new Dictionary<Result, HObject>();
            private readonly Dictionary<Result, double> _areas = new Dictionary<Result, double>();
            private readonly Dictionary<Result, NmsBounds> _bounds = new Dictionary<Result, NmsBounds>();

            public HObject GetUnionRegion(Result result)
            {
                if (result == null)
                {
                    return null;
                }

                if (_unionRegions.TryGetValue(result, out HObject region))
                {
                    return region;
                }

                try
                {
                    HOperatorSet.Union1(result.Seg, out region);
                    _unionRegions[result] = region;
                    return region;
                }
                catch
                {
                    SafeDisposeHObject(region);
                    _unionRegions[result] = null;
                    return null;
                }
            }

            public double GetArea(Result result)
            {
                if (result == null)
                {
                    return 0d;
                }

                if (_areas.TryGetValue(result, out double area))
                {
                    return area;
                }

                area = GetRegionArea(GetUnionRegion(result));
                _areas[result] = area;
                return area;
            }

            public bool TryRejectByBounds(Result left, Result right)
            {
                if (!TryGetBounds(left, out NmsBounds leftBounds)
                    || !TryGetBounds(right, out NmsBounds rightBounds))
                {
                    return false;
                }

                return leftBounds.Right < rightBounds.Left
                    || rightBounds.Right < leftBounds.Left
                    || leftBounds.Bottom < rightBounds.Top
                    || rightBounds.Bottom < leftBounds.Top;
            }

            private bool TryGetBounds(Result result, out NmsBounds bounds)
            {
                if (result == null)
                {
                    bounds = default;
                    return false;
                }

                if (_bounds.TryGetValue(result, out bounds))
                {
                    return bounds.IsValid;
                }

                bounds = default;
                HObject region = GetUnionRegion(result);
                if (!IsInitializedSafely(region))
                {
                    _bounds[result] = bounds;
                    return false;
                }

                try
                {
                    HOperatorSet.SmallestRectangle1(region, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                    bounds = new NmsBounds(row1.D, col1.D, row2.D, col2.D);
                    _bounds[result] = bounds;
                    return bounds.IsValid;
                }
                catch
                {
                    _bounds[result] = bounds;
                    return false;
                }
            }

            public void Dispose()
            {
                foreach (HObject region in _unionRegions.Values)
                {
                    SafeDisposeHObject(region);
                }

                _unionRegions.Clear();
                _areas.Clear();
                _bounds.Clear();
            }
        }

        private readonly struct NmsBounds
        {
            public NmsBounds(double top, double left, double bottom, double right)
            {
                Top = top;
                Left = left;
                Bottom = bottom;
                Right = right;
                IsValid = !double.IsNaN(top)
                    && !double.IsNaN(left)
                    && !double.IsNaN(bottom)
                    && !double.IsNaN(right);
            }

            public double Top { get; }

            public double Left { get; }

            public double Bottom { get; }

            public double Right { get; }

            public bool IsValid { get; }
        }

        /// <summary>
        /// 合并同一聚类内的多个缺陷结果。
        /// </summary>
        private Result MergeResultCluster(IReadOnlyList<Result> clusterResults)
        {
            if (clusterResults == null || clusterResults.Count == 0)
            {
                return null;
            }

            Result primaryResult = clusterResults
                .OrderByDescending(item => item?.Confidence ?? 0f)
                .FirstOrDefault(item => item != null);
            if (primaryResult == null)
            {
                return null;
            }

            Result mergedResult = CloneResult(primaryResult);
            HObject mergedRegion = null;

            try
            {
                mergedRegion = BuildUnionRegion(clusterResults.Select(item => item?.Seg));
                if (IsInitializedSafely(mergedRegion))
                {
                    SafeDisposeHObject(mergedResult.Seg);
                    mergedResult.Seg = mergedRegion;
                    mergedRegion = null;
                    UpdateMergedResultGeometry(mergedResult);
                }

                mergedResult.Confidence = clusterResults.Max(item => item?.Confidence ?? 0f);
                return mergedResult;
            }
            catch
            {
                DisposeResult(mergedResult);
                return CloneResult(primaryResult);
            }
            finally
            {
                SafeDisposeHObject(mergedRegion);
            }
        }

        /// <summary>
        /// 构建多个区域的并集结果。
        /// </summary>
        private static HObject BuildUnionRegion(IEnumerable<HObject> regions)
        {
            List<HObject> validRegions = regions?
                .Where(IsInitializedSafely)
                .ToList() ?? new List<HObject>();
            if (validRegions.Count == 0)
            {
                return null;
            }

            if (validRegions.Count == 1)
            {
                return SafeCloneHObject(validRegions[0], "DefectPostProcess 克隆 NMS 并集区域失败");
            }

            HObject concatenatedRegion = SafeCloneHObject(validRegions[0], "DefectPostProcess 克隆 NMS 初始区域失败");
            HObject unionRegion = null;
            try
            {
                for (int i = 1; i < validRegions.Count; i++)
                {
                    HOperatorSet.ConcatObj(concatenatedRegion, validRegions[i], out HObject combinedRegion);
                    SafeDisposeHObject(concatenatedRegion);
                    concatenatedRegion = combinedRegion;
                }

                HOperatorSet.Union1(concatenatedRegion, out unionRegion);
                SafeDisposeHObject(concatenatedRegion);
                concatenatedRegion = null;
                return unionRegion;
            }
            catch
            {
                SafeDisposeHObject(unionRegion);
                return SafeCloneHObject(validRegions[0], "DefectPostProcess 回退克隆 NMS 区域失败");
            }
            finally
            {
                SafeDisposeHObject(concatenatedRegion);
            }
        }

        /// <summary>
        /// 根据合并后的区域更新结果几何参数。
        /// </summary>
        private static void UpdateMergedResultGeometry(Result result)
        {
            if (result == null || !IsInitializedSafely(result.Seg))
            {
                return;
            }

            try
            {
                HOperatorSet.SmallestRectangle2(result.Seg, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                result.Cx = (float)column.D;
                result.Cy = (float)row.D;
                result.Width = (float)(length1.D * 2d);
                result.Height = (float)(length2.D * 2d);
                result.Angle = (float)phi.D;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 获取区域面积。
        /// </summary>
        private static double GetRegionArea(HObject region)
        {
            if (!IsInitializedSafely(region))
            {
                return 0d;
            }

            try
            {
                HOperatorSet.AreaCenter(region, out HTuple areas, out _, out _);
                return areas.TupleSum().D;
            }
            catch
            {
                return 0d;
            }
        }

        /// <summary>
        /// 获取结果所属的规则键。
        /// </summary>
        private string GetResultRuleKey(Result result)
        {
            return result == null
                ? string.Empty
                : GetDefectRuleKey(result.ClassId, ResolveClassName(result.ClassId, result.ClassName));
        }

        /// <summary>
        /// 获取指定缺陷结果对应的规则配置。
        /// </summary>
        private DefectRuleConfig GetRuleConfigForResult(Result result)
        {
            return GetRuleConfigForResult(result, CreateRuleConfigMap());
        }

        private DefectRuleConfig GetRuleConfigForResult(Result result, IReadOnlyDictionary<string, DefectRuleConfig> ruleConfigMap)
        {
            if (result == null)
            {
                return null;
            }

            string ruleKey = GetDefectRuleKey(result.ClassId, ResolveClassName(result.ClassId, result.ClassName));
            return ruleConfigMap != null && ruleConfigMap.TryGetValue(ruleKey, out DefectRuleConfig ruleConfig)
                ? ruleConfig
                : null;
        }

        private Dictionary<string, DefectRuleConfig> CreateRuleConfigMap()
        {
            return DefectRuleConfigs?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.RuleKey))
                .GroupBy(item => item.RuleKey)
                .ToDictionary(item => item.Key, item => item.First()) ?? new Dictionary<string, DefectRuleConfig>();
        }

        /// <summary>
        /// 判断缺陷结果是否满足当前规则。
        /// </summary>
        private bool IsResultMatched(Result result, DefectRuleConfig ruleConfig)
        {
            return IsResultMatched(result, ruleConfig, GetEnabledFeatureRules(ruleConfig));
        }

        private bool IsResultMatched(Result result, DefectRuleConfig ruleConfig, IReadOnlyList<FeatureThresholdItem> enabledRules)
        {
            if (!IsConfidenceMatched(result, ruleConfig?.MinimumConfidence ?? 0))
            {
                return false;
            }

            return IsFeatureRulesMatched(result, enabledRules);
        }

        /// <summary>
        /// 判断特征阈值规则是否全部满足。
        /// </summary>
        private bool IsFeatureRulesMatched(Result result, IReadOnlyList<FeatureThresholdItem> enabledRules)
        {
            if (enabledRules == null || enabledRules.Count == 0)
            {
                return true;
            }

            bool matched = IsFeatureMatched(result, enabledRules[0]);
            for (int i = 1; i < enabledRules.Count; i++)
            {
                bool currentMatched = IsFeatureMatched(result, enabledRules[i]);
                matched = enabledRules[i].RelationOperator == RelationOr
                    ? matched || currentMatched
                    : matched && currentMatched;
            }

            return matched;
        }

        /// <summary>
        /// 获取缺陷结果的匹配说明文本。
        /// </summary>
        private string GetMatchText(Result result, DefectRuleConfig ruleConfig)
        {
            List<FeatureThresholdItem> enabledRules = GetEnabledFeatureRules(ruleConfig);
            bool hasConfidenceRule = (ruleConfig?.MinimumConfidence ?? 0) > 0;
            bool hasFeatureRule = enabledRules.Count > 0;

            if (!hasConfidenceRule && !hasFeatureRule)
            {
                return "未启用规则";
            }

            if (!IsConfidenceMatched(result, ruleConfig?.MinimumConfidence ?? 0))
            {
                return "置信度过滤";
            }

            if (!IsFeatureRulesMatched(result, enabledRules))
            {
                return "特征过滤";
            }

            return "通过";
        }

        /// <summary>
        /// 判断单项特征是否满足阈值条件。
        /// </summary>
        private bool IsFeatureMatched(Result result, FeatureThresholdItem featureRule)
        {
            double featureValue = GetFeatureValue(result, featureRule.FeatureKey, featureRule?.Unit);

            if (TryParseThreshold(featureRule.MinimumValue, out double minimumValue) && featureValue < minimumValue)
            {
                return false;
            }

            if (TryParseThreshold(featureRule.MaximumValue, out double maximumValue) && featureValue > maximumValue)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断置信度是否满足阈值要求。
        /// </summary>
        private static bool IsConfidenceMatched(Result result, double minimumConfidence)
        {
            return (result?.Confidence ?? 0) >= minimumConfidence;
        }

        /// <summary>
        /// 获取已启用的特征阈值规则。
        /// </summary>
        private static List<FeatureThresholdItem> GetEnabledFeatureRules(DefectRuleConfig ruleConfig)
        {
            return ruleConfig?.FeatureThresholds?
                .Where(item => item != null && item.IsEnabled)
                .ToList() ?? new List<FeatureThresholdItem>();
        }

        /// <summary>
        /// 获取指定特征在当前单位下的数值。
        /// </summary>
        internal double GetFeatureValue(Result result, string featureKey, string unit = null)
        {
            switch (featureKey)
            {
                case DefectFeatureKeys.Length:
                    return ShouldUseActualLengthUnit(unit)
                        ? GetActualLengthFeatureValue(result, true)
                        : GetPixelLengthFeatureValue(result, true);
                case DefectFeatureKeys.Width:
                    return ShouldUseActualLengthUnit(unit)
                        ? GetActualLengthFeatureValue(result, false)
                        : GetPixelLengthFeatureValue(result, false);
                case DefectFeatureKeys.Area:
                    return ShouldUseActualArea(unit)
                        ? ConvertPixelAreaToActualArea(GetMaskArea(result))
                        : GetMaskArea(result);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取像素单位下的长度特征值。
        /// </summary>
        private static double GetPixelLengthFeatureValue(Result result, bool useLongSide)
        {
            double width = result?.Width ?? 0;
            double height = result?.Height ?? 0;
            return useLongSide
                ? System.Math.Max(width, height)
                : System.Math.Min(width, height);
        }

        /// <summary>
        /// 获取实际单位下的长度特征值。
        /// </summary>
        private double GetActualLengthFeatureValue(Result result, bool useLongSide)
        {
            if (!HasValidPixelEquivalent)
            {
                return GetPixelLengthFeatureValue(result, useLongSide);
            }

            double side1 = GetActualRectangleSideLength(result?.Width ?? 0, result?.Angle ?? 0);
            double side2 = GetActualRectangleSideLength(result?.Height ?? 0, (result?.Angle ?? 0) + (System.Math.PI / 2d));
            return useLongSide
                ? System.Math.Max(side1, side2)
                : System.Math.Min(side1, side2);
        }

        /// <summary>
        /// 计算矩形边在实际单位下的长度。
        /// </summary>
        private double GetActualRectangleSideLength(double pixelLength, double angle)
        {
            if (pixelLength <= 0)
            {
                return 0;
            }

            if (!HasValidPixelEquivalent)
            {
                return pixelLength;
            }

            double cosValue = System.Math.Cos(angle);
            double sinValue = System.Math.Sin(angle);
            double mmPerPixel = System.Math.Sqrt(
                (cosValue * cosValue) * PixelEquivalentX * PixelEquivalentX +
                (sinValue * sinValue) * PixelEquivalentY * PixelEquivalentY);

            return pixelLength * mmPerPixel;
        }

        /// <summary>
        /// 获取分割区域的像素面积。
        /// </summary>
        private static double GetMaskArea(Result result)
        {
            if (result?.Seg == null || !result.Seg.IsInitialized())
            {
                return 0;
            }

            try
            {
                HOperatorSet.AreaCenter(result.Seg, out HTuple areas, out HTuple row, out HTuple column);
                return areas.TupleSum().D;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 判断当前是否应使用实际面积单位。
        /// </summary>
        private bool ShouldUseActualArea(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return HasValidPixelEquivalent;
            }

            return IsActualAreaUnit(unit);
        }

        /// <summary>
        /// 判断当前是否应使用实际长度单位。
        /// </summary>
        private bool ShouldUseActualLengthUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return HasValidPixelEquivalent;
            }

            return IsActualLengthUnit(unit);
        }

        /// <summary>
        /// 将像素长度换算为实际长度。
        /// </summary>
        private double ConvertPixelLengthToActualLength(double pixelLength)
        {
            double scaleFactor = GetLengthScaleFactor();
            if (scaleFactor <= 0)
            {
                return pixelLength;
            }

            return pixelLength * scaleFactor;
        }

        /// <summary>
        /// 将像素面积换算为实际面积。
        /// </summary>
        private double ConvertPixelAreaToActualArea(double pixelArea)
        {
            double scaleFactor = GetAreaScaleFactor();
            if (scaleFactor <= 0)
            {
                return pixelArea;
            }

            return pixelArea * scaleFactor;
        }

        /// <summary>
        /// 获取长度单位换算系数。
        /// </summary>
        private double GetLengthScaleFactor()
        {
            double areaScaleFactor = GetAreaScaleFactor();
            if (areaScaleFactor <= 0)
            {
                return 0;
            }

            return System.Math.Sqrt(areaScaleFactor);
        }

        /// <summary>
        /// 获取面积单位换算系数。
        /// </summary>
        private double GetAreaScaleFactor()
        {
            if (!HasValidPixelEquivalent)
            {
                return 0;
            }

            return PixelEquivalentX * PixelEquivalentY;
        }

        private bool HasValidPixelEquivalent
        {
            get { return PixelEquivalentX > 0 && PixelEquivalentY > 0; }
        }

        /// <summary>
        /// 获取默认显示的长度单位。
        /// </summary>
        private string GetDefaultLengthUnit()
        {
            return HasValidPixelEquivalent ? ActualLengthUnit : PixelLengthUnit;
        }

        /// <summary>
        /// 获取默认显示的面积单位。
        /// </summary>
        private string GetDefaultAreaUnit()
        {
            return HasValidPixelEquivalent ? ActualAreaUnit : PixelAreaUnit;
        }

        /// <summary>
        /// 获取特征在界面上显示时使用的单位。
        /// </summary>
        internal string GetFeatureDisplayUnit(string featureKey)
        {
            return featureKey switch
            {
                DefectFeatureKeys.Length => GetDefaultLengthUnit(),
                DefectFeatureKeys.Width => GetDefaultLengthUnit(),
                DefectFeatureKeys.Area => GetDefaultAreaUnit(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 尝试解析阈值字符串。
        /// </summary>
        private static bool TryParseThreshold(string value, out double threshold)
        {
            threshold = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out threshold)
                || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out threshold);
        }

        /// <summary>
        /// 尝试将对象转换为双精度数值。
        /// </summary>
        internal static bool TryConvertToDouble(object value, out double result)
        {
            result = 0;
            if (value == null)
            {
                return false;
            }

            if (value is double doubleValue)
            {
                result = doubleValue;
                return true;
            }

            if (value is float floatValue)
            {
                result = floatValue;
                return true;
            }

            if (value is decimal decimalValue)
            {
                result = (double)decimalValue;
                return true;
            }

            if (value is byte or sbyte or short or ushort or int or uint or long or ulong)
            {
                result = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }

            if (value is string textValue)
            {
                return TryParseThreshold(textValue, out result);
            }

            try
            {
                result = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取当前选中缺陷的规则配置。
        /// </summary>
        internal DefectRuleConfig GetCurrentRuleConfig()
        {
            if (CurrentDefect == null)
            {
                return null;
            }

            return GetOrCreateRuleConfig(CurrentDefect);
        }

        /// <summary>
        /// 将当前编辑区里的规则参数写回当前缺陷规则配置，避免执行或保存前丢失未提交状态。
        /// </summary>
        private void SyncCurrentRuleConfigFromEditingState()
        {
            DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
            if (currentRuleConfig == null)
            {
                return;
            }

            currentRuleConfig.MinimumConfidence = System.Math.Clamp(MinimumConfidence, 0d, 1d);
            currentRuleConfig.IsNmsEnabled = IsNmsEnabled;
            currentRuleConfig.NmsIoUThreshold = System.Math.Clamp(NmsIoUThreshold, 0d, 1d);
            currentRuleConfig.FeatureThresholds = FeatureThresholds ?? CreateDefaultFeatureThresholds();
        }

        /// <summary>
        /// 获取规则配置；不存在时自动创建。
        /// </summary>
        private DefectRuleConfig GetOrCreateRuleConfig(DefectItem defectItem)
        {
            string ruleKey = GetDefectRuleKey(defectItem);
            DefectRuleConfig defectRuleConfig = DefectRuleConfigs.FirstOrDefault(item => item.RuleKey == ruleKey);
            if (defectRuleConfig == null)
            {
                defectRuleConfig = DefectRuleConfigs.FirstOrDefault(item => item.ClassId == defectItem.ClassId);
            }

            if (defectRuleConfig == null)
            {
                defectRuleConfig = new DefectRuleConfig
                {
                    RuleKey = ruleKey,
                    ClassId = defectItem.ClassId,
                    ClassName = defectItem.ClassName,
                    MinimumConfidence = 0,
                    IsNmsEnabled = true,
                    NmsIoUThreshold = 0.5d,
                    FeatureThresholds = CreateDefaultFeatureThresholds()
                };

                DefectRuleConfigs.Add(defectRuleConfig);
            }
            else
            {
                defectRuleConfig.RuleKey = ruleKey;
                defectRuleConfig.ClassId = defectItem.ClassId;
                defectRuleConfig.ClassName = defectItem.ClassName;
                defectRuleConfig.NmsIoUThreshold = System.Math.Clamp(defectRuleConfig.NmsIoUThreshold, 0d, 1d);
                defectRuleConfig.FeatureThresholds = NormalizeFeatureThresholds(defectRuleConfig.FeatureThresholds);
            }

            return defectRuleConfig;
        }

        /// <summary>
        /// 绑定特征阈值事件。
        /// </summary>
        private void AttachFeatureThresholdEvents(IEnumerable<FeatureThresholdItem> featureThresholds)
        {
            if (featureThresholds == null)
            {
                return;
            }

            foreach (FeatureThresholdItem item in featureThresholds)
            {
                item.PropertyChanged += OnFeatureThresholdChanged;
            }
        }

        /// <summary>
        /// 解绑特征阈值事件。
        /// </summary>
        private void DetachFeatureThresholdEvents(IEnumerable<FeatureThresholdItem> featureThresholds)
        {
            if (featureThresholds == null)
            {
                return;
            }

            foreach (FeatureThresholdItem item in featureThresholds)
            {
                item.PropertyChanged -= OnFeatureThresholdChanged;
            }
        }

        /// <summary>
        /// 处理特征阈值项变更后的联动刷新。
        /// </summary>
        private void OnFeatureThresholdChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isLoadingCurrentRule)
            {
                return;
            }

            if (_isRefreshingAreaThresholds)
            {
                return;
            }

            SyncCurrentRuleConfigFromEditingState();
            MarkSchemeDirty();
            RequestEditingStateRefresh();
        }

        /// <summary>
        /// 创建默认特征阈值集合。
        /// </summary>
        private ObservableCollection<FeatureThresholdItem> CreateDefaultFeatureThresholds()
        {
            return CreateDefaultFeatureThresholds(GetDefaultLengthUnit(), GetDefaultAreaUnit());
        }

        /// <summary>
        /// 按指定单位创建默认特征阈值集合。
        /// </summary>
        private static ObservableCollection<FeatureThresholdItem> CreateDefaultFeatureThresholds(string lengthUnit, string areaUnit)
        {
            return new ObservableCollection<FeatureThresholdItem>
            {
                new FeatureThresholdItem(DefectFeatureKeys.Length, "长度", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Length, lengthUnit), lengthUnit, RelationAnd, true),
                new FeatureThresholdItem(DefectFeatureKeys.Width, "宽度", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Width, lengthUnit), lengthUnit, RelationAnd, true),
                new FeatureThresholdItem(DefectFeatureKeys.Area, "面积", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Area, areaUnit), areaUnit, RelationAnd, true)
            };
        }

        /// <summary>
        /// 规范化特征阈值集合。
        /// </summary>
        private ObservableCollection<FeatureThresholdItem> NormalizeFeatureThresholds(IEnumerable<FeatureThresholdItem> featureThresholds)
        {
            Dictionary<string, FeatureThresholdItem> featureMap = featureThresholds?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.FeatureKey))
                .GroupBy(item => item.FeatureKey)
                .ToDictionary(item => item.Key, item => item.First())
                ?? new Dictionary<string, FeatureThresholdItem>();

            return new ObservableCollection<FeatureThresholdItem>
            {
                CreateFeatureThreshold(DefectFeatureKeys.Length, "长度", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Length, GetDefaultLengthUnit()), RelationAnd, true, featureMap),
                CreateFeatureThreshold(DefectFeatureKeys.Width, "宽度", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Width, GetDefaultLengthUnit()), RelationAnd, true, featureMap),
                CreateFeatureThreshold(DefectFeatureKeys.Area, "面积", DefaultMinimumValue, GetDefaultMaximumValue(DefectFeatureKeys.Area, GetDefaultAreaUnit()), RelationAnd, true, featureMap)
            };
        }

        /// <summary>
        /// 创建单个特征阈值项。
        /// </summary>
        private FeatureThresholdItem CreateFeatureThreshold(
            string featureKey,
            string featureName,
            string minimumValue,
            string maximumValue,
            string relationOperator,
            bool canEditRelation,
            IReadOnlyDictionary<string, FeatureThresholdItem> featureMap)
        {
            if (!featureMap.TryGetValue(featureKey, out FeatureThresholdItem source))
            {
                return new FeatureThresholdItem(featureKey, featureName, minimumValue, maximumValue, GetFeatureDisplayUnit(featureKey), relationOperator, canEditRelation);
            }

            string normalizedUnit = NormalizeFeatureUnit(featureKey, source.Unit);
            string targetUnit = GetFeatureDisplayUnit(featureKey);
            bool shouldConvertToActualValue = ShouldConvertFeatureThresholdToActual(featureKey, normalizedUnit, targetUnit);
            bool shouldConvertToPixelValue = ShouldConvertFeatureThresholdToPixel(featureKey, normalizedUnit, targetUnit);

            return new FeatureThresholdItem(
                featureKey,
                featureName,
                NormalizeFeatureThresholdValue(featureKey, source.MinimumValue, normalizedUnit, shouldConvertToActualValue, shouldConvertToPixelValue),
                NormalizeFeatureThresholdValue(featureKey, source.MaximumValue, normalizedUnit, shouldConvertToActualValue, shouldConvertToPixelValue),
                targetUnit,
                source.RelationOperator,
                canEditRelation)
            {
                IsEnabled = source.IsEnabled
            };
        }

        /// <summary>
        /// 判断是否需要将阈值从像素单位换算为实际单位。
        /// </summary>
        private bool ShouldConvertFeatureThresholdToActual(string featureKey, string normalizedUnit, string targetUnit)
        {
            return HasValidPixelEquivalent
                && IsPixelFeatureUnit(featureKey, normalizedUnit)
                && IsActualFeatureUnit(featureKey, targetUnit);
        }

        /// <summary>
        /// 判断是否需要将阈值从实际单位换算为像素单位。
        /// </summary>
        private bool ShouldConvertFeatureThresholdToPixel(string featureKey, string normalizedUnit, string targetUnit)
        {
            return HasCalibrationRuleScale()
                && IsActualFeatureUnit(featureKey, normalizedUnit)
                && IsPixelFeatureUnit(featureKey, targetUnit);
        }

        /// <summary>
        /// 规范化特征阈值数值。
        /// </summary>
        private string NormalizeFeatureThresholdValue(
            string featureKey,
            string value,
            string sourceUnit,
            bool shouldConvertToActualValue,
            bool shouldConvertToPixelValue)
        {
            if ((!shouldConvertToActualValue && !shouldConvertToPixelValue)
                || !TryParseThreshold(value, out double threshold))
            {
                return value;
            }

            if (IsDefaultFeatureThresholdValue(featureKey, value, sourceUnit, isMinimumValue: true))
            {
                return DefaultMinimumValue;
            }

            if (IsDefaultFeatureThresholdValue(featureKey, value, sourceUnit, isMinimumValue: false))
            {
                return GetDefaultMaximumValue(
                    featureKey,
                    shouldConvertToActualValue ? GetActualFeatureUnit(featureKey) : GetPixelFeatureUnit(featureKey));
            }

            double normalizedValue;
            if (featureKey == DefectFeatureKeys.Area)
            {
                normalizedValue = shouldConvertToActualValue
                    ? ConvertPixelAreaToActualArea(threshold)
                    : ConvertActualAreaToPixelArea(threshold);
            }
            else
            {
                normalizedValue = shouldConvertToActualValue
                    ? ConvertPixelLengthToActualLength(threshold)
                    : ConvertActualLengthToPixelLength(threshold);
            }

            return normalizedValue.ToString("0.############", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 获取指定特征和单位的默认上限值。
        /// </summary>
        private static string GetDefaultMaximumValue(string featureKey, string unit)
        {
            bool isAreaFeature = featureKey == DefectFeatureKeys.Area;
            bool isActualUnit = IsActualFeatureUnit(featureKey, unit);

            if (isAreaFeature)
            {
                return isActualUnit ? DefaultActualAreaMaximum : DefaultPixelAreaMaximum;
            }

            return isActualUnit ? DefaultActualLengthMaximum : DefaultPixelLengthMaximum;
        }

        /// <summary>
        /// 判断当前阈值是否为默认值。
        /// </summary>
        private static bool IsDefaultFeatureThresholdValue(string featureKey, string value, string unit, bool isMinimumValue)
        {
            if (!TryParseThreshold(value, out double actualValue))
            {
                return false;
            }

            string defaultValue = isMinimumValue
                ? DefaultMinimumValue
                : GetDefaultMaximumValue(featureKey, unit);

            return TryParseThreshold(defaultValue, out double expectedValue)
                && System.Math.Abs(actualValue - expectedValue) < 1e-9;
        }

        /// <summary>
        /// 规范化特征单位字符串。
        /// </summary>
        private static string NormalizeFeatureUnit(string featureKey, string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return string.Empty;
            }

            string normalized = unit.Trim().Replace(" ", string.Empty).ToLowerInvariant();
            if (featureKey == DefectFeatureKeys.Area)
            {
                return normalized switch
                {
                    "px2" => PixelAreaUnit,
                    "px^2" => PixelAreaUnit,
                    "mm2" => ActualAreaUnit,
                    "mm^2" => ActualAreaUnit,
                    _ => unit.Trim()
                };
            }

            return normalized switch
            {
                "px" => PixelLengthUnit,
                "mm" => ActualLengthUnit,
                _ => unit.Trim()
            };
        }

        /// <summary>
        /// 获取像素单位下的特征单位。
        /// </summary>
        private static string GetPixelFeatureUnit(string featureKey)
        {
            return featureKey == DefectFeatureKeys.Area ? PixelAreaUnit : PixelLengthUnit;
        }

        /// <summary>
        /// 获取实际单位下的特征单位。
        /// </summary>
        private static string GetActualFeatureUnit(string featureKey)
        {
            return featureKey == DefectFeatureKeys.Area ? ActualAreaUnit : ActualLengthUnit;
        }

        /// <summary>
        /// 判断当前是否具备标定规则换算系数。
        /// </summary>
        private bool HasCalibrationRuleScale()
        {
            return GetCalibrationRuleAreaScaleFactor() > 0;
        }

        /// <summary>
        /// 将实际长度换算为像素长度。
        /// </summary>
        private double ConvertActualLengthToPixelLength(double actualLength)
        {
            double scaleFactor = GetReverseLengthScaleFactor();
            if (scaleFactor <= 0)
            {
                return actualLength;
            }

            return actualLength / scaleFactor;
        }

        /// <summary>
        /// 将实际面积换算为像素面积。
        /// </summary>
        private double ConvertActualAreaToPixelArea(double actualArea)
        {
            double scaleFactor = GetReverseAreaScaleFactor();
            if (scaleFactor <= 0)
            {
                return actualArea;
            }

            return actualArea / scaleFactor;
        }

        /// <summary>
        /// 获取实际单位换算回像素单位时使用的长度换算系数。
        /// </summary>
        private double GetReverseLengthScaleFactor()
        {
            double areaScaleFactor = GetReverseAreaScaleFactor();
            if (areaScaleFactor <= 0)
            {
                return 0;
            }

            return System.Math.Sqrt(areaScaleFactor);
        }

        /// <summary>
        /// 获取实际单位换算回像素单位时使用的面积换算系数。
        /// </summary>
        private double GetReverseAreaScaleFactor()
        {
            double directAreaScaleFactor = GetAreaScaleFactor();
            return directAreaScaleFactor > 0
                ? directAreaScaleFactor
                : GetCalibrationRuleAreaScaleFactor();
        }

        /// <summary>
        /// 获取标定规则使用的面积换算系数。
        /// </summary>
        private double GetCalibrationRuleAreaScaleFactor()
        {
            double calibrationPixelEquivalentX = _calibrationIntervalX > 0 ? _calibrationIntervalX : 0;
            double calibrationPixelEquivalentY = _calibrationIntervalY > 0 ? _calibrationIntervalY : 0;
            if (calibrationPixelEquivalentX <= 0 || calibrationPixelEquivalentY <= 0)
            {
                return 0;
            }

            return calibrationPixelEquivalentX * calibrationPixelEquivalentY;
        }

        /// <summary>
        /// 判断是否为像素单位特征。
        /// </summary>
        private static bool IsPixelFeatureUnit(string featureKey, string unit)
        {
            return string.Equals(
                NormalizeFeatureUnit(featureKey, unit),
                GetPixelFeatureUnit(featureKey),
                System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否为实际单位特征。
        /// </summary>
        private static bool IsActualFeatureUnit(string featureKey, string unit)
        {
            return string.Equals(
                NormalizeFeatureUnit(featureKey, unit),
                GetActualFeatureUnit(featureKey),
                System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否为实际面积单位。
        /// </summary>
        private static bool IsActualAreaUnit(string unit)
        {
            return IsActualFeatureUnit(DefectFeatureKeys.Area, unit);
        }

        /// <summary>
        /// 判断是否为实际长度单位。
        /// </summary>
        private static bool IsActualLengthUnit(string unit)
        {
            return IsActualFeatureUnit(DefectFeatureKeys.Length, unit);
        }

        /// <summary>
        /// 刷新特征阈值显示单位。
        /// </summary>
        private void RefreshFeatureThresholdUnits()
        {
            if (_isLoadingCurrentRule || _isRefreshingAreaThresholds)
            {
                return;
            }

            _isRefreshingAreaThresholds = true;
            try
            {
                if (DefectRuleConfigs != null)
                {
                    foreach (DefectRuleConfig ruleConfig in DefectRuleConfigs.Where(item => item != null))
                    {
                        ruleConfig.FeatureThresholds = NormalizeFeatureThresholds(ruleConfig.FeatureThresholds);
                    }
                }

                if (CurrentDefect == null)
                {
                    FeatureThresholds = NormalizeFeatureThresholds(FeatureThresholds);
                    return;
                }

                DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
                if (currentRuleConfig == null)
                {
                    FeatureThresholds = NormalizeFeatureThresholds(FeatureThresholds);
                    return;
                }

                if (!ReferenceEquals(FeatureThresholds, currentRuleConfig.FeatureThresholds))
                {
                    FeatureThresholds = currentRuleConfig.FeatureThresholds;
                    return;
                }

                ObservableCollection<FeatureThresholdItem> normalizedThresholds = NormalizeFeatureThresholds(currentRuleConfig.FeatureThresholds);
                currentRuleConfig.FeatureThresholds = normalizedThresholds;
                FeatureThresholds = normalizedThresholds;
            }
            finally
            {
                _isRefreshingAreaThresholds = false;
            }
        }

        /// <summary>
        /// 根据缺陷项生成规则键。
        /// </summary>
        internal static string GetDefectRuleKey(DefectItem defectItem)
        {
            if (defectItem == null)
            {
                return string.Empty;
            }

            return GetDefectRuleKey(defectItem.ClassId, defectItem.ClassName);
        }

        /// <summary>
        /// 根据类别信息生成规则键。
        /// </summary>
        internal static string GetDefectRuleKey(int classId, string className)
        {
            return $"{classId}__{NormalizeClassName(className, classId)}";
        }

        /// <summary>
        /// 构建用于查看实例特征值的弹窗数据。
        /// </summary>
        public DefectFeatureValueDialogData CreateFeatureValueDialogData()
        {
            DefectFeatureValueDialogData dialogData = new DefectFeatureValueDialogData
            {
                SourceModel = this,
                CurrentRuleKey = GetDefectRuleKey(CurrentDefect),
                LengthUnit = GetFeatureDisplayUnit(DefectFeatureKeys.Length),
                WidthUnit = GetFeatureDisplayUnit(DefectFeatureKeys.Width),
                AreaUnit = GetFeatureDisplayUnit(DefectFeatureKeys.Area)
            };

            Dictionary<string, int> instanceIndexMap = new Dictionary<string, int>();
            Dictionary<string, DefectRuleConfig> ruleConfigMap = CreateRuleConfigMap();
            List<Result> mergedResults = BuildMergedResultsByRuleForDialog(SourceResults);
            foreach (Result result in mergedResults)
            {
                string className = ResolveClassName(result.ClassId, result.ClassName);
                string ruleKey = GetDefectRuleKey(result.ClassId, className);

                if (!instanceIndexMap.ContainsKey(ruleKey))
                {
                    instanceIndexMap[ruleKey] = 0;
                }

                instanceIndexMap[ruleKey]++;

                ruleConfigMap.TryGetValue(ruleKey, out DefectRuleConfig ruleConfig);
                bool isMatched = IsResultMatched(result, ruleConfig);
                double? physicalX = null;
                double? physicalY = null;
                if (TryConvertResultCenterToWorld(result, out double worldX, out double worldY, out _, out _))
                {
                    physicalX = worldX;
                    physicalY = worldY;
                }

                dialogData.Items.Add(new DefectFeatureValueItem
                {
                    RuleKey = ruleKey,
                    DefectName = className,
                    ClassId = result.ClassId,
                    InstanceIndex = instanceIndexMap[ruleKey],
                    LengthValue = GetFeatureValue(result, DefectFeatureKeys.Length, dialogData.LengthUnit),
                    WidthValue = GetFeatureValue(result, DefectFeatureKeys.Width, dialogData.WidthUnit),
                    AreaValue = GetFeatureValue(result, DefectFeatureKeys.Area, dialogData.AreaUnit),
                    PhysicalXValue = physicalX,
                    PhysicalYValue = physicalY,
                    Confidence = result.Confidence,
                    IsMatched = isMatched,
                    MatchText = GetMatchText(result, ruleConfig)
                });
            }
            return dialogData;
        }

        /// <summary>
        /// 为实例特征窗口构建执行 NMS 后的结果集合。
        /// </summary>
        private List<Result> BuildMergedResultsByRuleForDialog(IEnumerable<Result> source)
        {
            List<Result> mergedResults = new List<Result>();
            Dictionary<string, List<Result>> groupedResults = new Dictionary<string, List<Result>>();
            Dictionary<string, DefectRuleConfig> ruleConfigMap = new Dictionary<string, DefectRuleConfig>();
            Dictionary<string, DefectRuleConfig> configuredRuleMap = CreateRuleConfigMap();
            List<string> ruleOrder = new List<string>();

            foreach (Result result in GetProcessableResults(source))
            {
                string ruleKey = GetResultRuleKey(result);
                if (!groupedResults.TryGetValue(ruleKey, out List<Result> resultGroup))
                {
                    resultGroup = new List<Result>();
                    groupedResults.Add(ruleKey, resultGroup);
                    ruleConfigMap[ruleKey] = GetRuleConfigForResult(result, configuredRuleMap);
                    ruleOrder.Add(ruleKey);
                }

                resultGroup.Add(result);
            }

            foreach (string ruleKey in ruleOrder)
            {
                foreach (IGrouping<int, Result> imageGroup in GroupResultsByImageIndex(groupedResults[ruleKey]))
                {
                    mergedResults.AddRange(BuildMergedResultsByNms(imageGroup, ruleConfigMap[ruleKey]));
                }
            }

            return mergedResults;
        }

        #endregion

        #region 定制算法处理
        private void ApplyCustomAlgorithmStage()
        {
            ClearSheetSizeJudgeResultCaches();
            ApplyDefectJudgeToJudgeResultsByImage();
            ApplySheetSizeJudgeToJudgeResultsByImage();
            ApplyFinalJudgeToJudgeResultsByImage();
        }

        private List<Dictionary<string, object>> BuildJudgeResultsByImageSkeleton()
        {
            int imageCount = Math.Max(ResultsByImage?.Count ?? 0, PreviewImageCount);
            return Enumerable.Range(0, imageCount)
                .Select(_ => new Dictionary<string, object>
                {
                    [DefectPostProcessResultKeys.DefectJudgeIsOk] = null,
                    [DefectPostProcessResultKeys.SheetSizeJudgeIsOk] = null,
                    [DefectPostProcessResultKeys.FinalJudgeIsOk] = null
                })
                .ToList();
        }

        private void ApplyDefectJudgeToJudgeResultsByImage()
        {
            if (JudgeResultsByImage == null)
            {
                return;
            }

            List<List<Result>> judgeGroups = ResultsByImage;
            if (!HasEnabledFeatureJudgeRules())
            {
                return;
            }

            if (judgeGroups == null)
            {
                return;
            }

            EnsureJudgeResultsByImageCount(judgeGroups.Count);
            for (int imageIndex = 0; imageIndex < judgeGroups.Count; imageIndex++)
            {
                JudgeResultsByImage[imageIndex][DefectPostProcessResultKeys.DefectJudgeIsOk] =
                    judgeGroups[imageIndex] == null || judgeGroups[imageIndex].Count == 0;
            }
        }

        private bool HasEnabledFeatureJudgeRules()
        {
            bool hasCurrentEnabledRule = FeatureThresholds?.Any(item => item?.IsEnabled == true) == true;
            bool hasConfiguredEnabledRule = DefectRuleConfigs?
                .SelectMany(item => item?.FeatureThresholds ?? Enumerable.Empty<FeatureThresholdItem>())
                .Any(item => item?.IsEnabled == true) == true;
            return hasCurrentEnabledRule || hasConfiguredEnabledRule;
        }

        private void EnsureJudgeResultsByImageCount(int imageCount)
        {
            if (JudgeResultsByImage == null)
            {
                JudgeResultsByImage = new List<Dictionary<string, object>>();
            }

            while (JudgeResultsByImage.Count < imageCount)
            {
                JudgeResultsByImage.Add(new Dictionary<string, object>
                {
                    [DefectPostProcessResultKeys.DefectJudgeIsOk] = null,
                    [DefectPostProcessResultKeys.SheetSizeJudgeIsOk] = null,
                    [DefectPostProcessResultKeys.FinalJudgeIsOk] = null
                });
            }
        }

        private void ApplyFinalJudgeToJudgeResultsByImage()
        {
            if (JudgeResultsByImage == null)
            {
                return;
            }

            List<List<Result>> defectJudgeGroups = ResultsByImage;

            for (int imageIndex = 0; imageIndex < JudgeResultsByImage.Count; imageIndex++)
            {
                Dictionary<string, object> judgeResult = JudgeResultsByImage[imageIndex];
                if (judgeResult == null)
                {
                    continue;
                }

                bool hasDefectJudge = TryGetJudgeBoolean(judgeResult, DefectPostProcessResultKeys.DefectJudgeIsOk, out bool defectJudgeIsOk);
                if (!hasDefectJudge
                    && defectJudgeGroups != null
                    && imageIndex >= 0
                    && imageIndex < defectJudgeGroups.Count)
                {
                    defectJudgeIsOk = defectJudgeGroups[imageIndex] == null || defectJudgeGroups[imageIndex].Count == 0;
                    hasDefectJudge = true;
                    judgeResult[DefectPostProcessResultKeys.DefectJudgeIsOk] = defectJudgeIsOk;
                }

                bool hasSheetJudge = TryGetJudgeBoolean(judgeResult, DefectPostProcessResultKeys.SheetSizeJudgeIsOk, out bool sheetJudgeIsOk);

                if (hasSheetJudge && !sheetJudgeIsOk)
                {
                    judgeResult[DefectPostProcessResultKeys.FinalJudgeIsOk] = false;
                }
                else if (hasDefectJudge)
                {
                    judgeResult[DefectPostProcessResultKeys.FinalJudgeIsOk] = defectJudgeIsOk;
                }
                else
                {
                    judgeResult[DefectPostProcessResultKeys.FinalJudgeIsOk] = false;
                }
            }
        }

        private static bool TryGetJudgeBoolean(Dictionary<string, object> judgeResult, string key, out bool value)
        {
            value = false;
            if (judgeResult == null
                || !judgeResult.TryGetValue(key, out object rawValue)
                || rawValue is not bool boolValue)
            {
                return false;
            }

            value = boolValue;
            return true;
        }

        private void ClearSheetSizeJudgeResultCaches()
        {
            if (ResultsByImage != null)
            {
                foreach (List<Result> imageResults in ResultsByImage)
                {
                    ClearSheetSizeJudgeResultCaches(imageResults);
                }
            }

            ClearSheetSizeJudgeResultCaches(Results);
        }

        private static void ClearSheetSizeJudgeResultCaches(IEnumerable<Result> results)
        {
            if (results == null)
            {
                return;
            }

            foreach (Result result in results)
            {
                if (result?.Others == null)
                {
                    continue;
                }

                result.Others.Remove(DefectPostProcessResultKeys.SheetSizeJudgeIsOk);
            }
        }

        private void ApplySheetSizeJudgeToJudgeResultsByImage()
        {
            if (SheetSizeJudge == null
                || !SheetSizeJudge.IsEnabled
                || _inputImages == null)
            {
                return;
            }

            int imageCount = _inputImages.Count;
            EnsureJudgeResultsByImageCount(imageCount);
            for (int imageIndex = 0; imageIndex < imageCount; imageIndex++)
            {
                if (!TryJudgeSheetSize(_inputImages[imageIndex], out SheetSizeJudgeResult judgeResult))
                {
                    continue;
                }

                JudgeResultsByImage[imageIndex][DefectPostProcessResultKeys.SheetSizeJudgeIsOk] = judgeResult.IsOk;
            }
        }

        public void ExecuteSheetSizeJudgePreview()
        {
            HImage previewImage = GetCurrentSheetSizeJudgePreviewImage();
            if (previewImage == null || !previewImage.IsInitialized())
            {
                SetSheetSizeJudgePreviewResult(false, null, "无可用图像");
                ClearSheetSizeJudgePreviewRegions();
                return;
            }

            if (!TryJudgeSheetSize(previewImage, out SheetSizeJudgeResult judgeResult, capturePreviewRegions: true))
            {
                SetSheetSizeJudgePreviewResult(false, null, "未启用或无法执行");
                ClearSheetSizeJudgePreviewRegions();
                return;
            }

            SetSheetSizeJudgePreviewResult(true, judgeResult, judgeResult.IsOk ? "OK" : "NG");
            UpdateSheetSizeJudgePreviewRegions(judgeResult);
            DisposeSheetSizeJudgeResultPreviewRegions(judgeResult);
        }

        private HImage GetCurrentSheetSizeJudgePreviewImage()
        {
            if (_inputImages == null || _inputImages.Count == 0)
            {
                RefreshInputImagePreview();
            }

            if (_inputImages == null || _inputImages.Count == 0)
            {
                return null;
            }

            int imageIndex = Math.Clamp(CurrentPreviewImageIndex, 0, _inputImages.Count - 1);
            HImage image = _inputImages[imageIndex];
            return image != null && image.IsInitialized() ? image : null;
        }

        private void SetSheetSizeJudgePreviewResult(bool hasResult, SheetSizeJudgeResult result, string statusText)
        {
            HasSheetSizeJudgePreviewResult = hasResult;
            SheetSizeJudgePreviewStatusText = statusText;
            SheetSizeJudgePreviewRectangularityText = result == null ? "--" : result.Rectangularity.ToString("F4");
            SheetSizeJudgePreviewLengthText = result == null ? "--" : result.Length.ToString("F4");
            SheetSizeJudgePreviewWidthText = result == null ? "--" : result.Width.ToString("F4");
        }

        private sealed class SheetSizeJudgeResult
        {
            public bool IsOk { get; set; }

            public double Rectangularity { get; set; }

            public double Length { get; set; }

            public double Width { get; set; }

            public HObject BeforeRectangle2Region { get; set; }

            public HObject AfterRectangle2Region { get; set; }
        }

        private bool TryJudgeSheetSize(HImage image, out SheetSizeJudgeResult result, bool capturePreviewRegions = false)
        {
            result = null;
            if (image == null
                || !image.IsInitialized()
                || SheetSizeJudge == null
                || !SheetSizeJudge.IsEnabled)
            {
                return false;
            }

            try
            {
                return TryJudgeSheetSizeCore(image, out result, capturePreviewRegions);
            }
            catch (Exception ex)
            {
                LogWarning($"尺寸判定失败: {ex.Message}");
                result = CreateNgSheetSizeJudgeResult();
                return true;
            }
        }

        private bool TryJudgeSheetSizeCore(HImage image, out SheetSizeJudgeResult result, bool capturePreviewRegions)
        {
            result = CreateNgSheetSizeJudgeResult();

            HOperatorSet.Threshold(image, out HObject thresholdRegion, SheetSizeJudge.Threshold, 255d);
            using (thresholdRegion)
            {
                double openingRadius = SheetSizeJudge.OpeningRadius > 0d ? SheetSizeJudge.OpeningRadius : 3.5d;
                HOperatorSet.OpeningCircle(thresholdRegion, out HObject openedRegion, openingRadius);
                using (openedRegion)
                {
                    HOperatorSet.Connection(openedRegion, out HObject connectedRegions);
                    using (connectedRegions)
                    {
                        if (!TrySelectLargestConnectedComponent(connectedRegions, out HObject largestRegion))
                        {
                            return true;
                        }

                        using (largestRegion)
                        {
                            if (!TryGetRegionRectangularity(largestRegion, out double rectangularity))
                            {
                                return true;
                            }

                            if (!TryGetRectangle2(largestRegion, out double row, out double column, out double phi, out double length1, out double length2))
                            {
                                return true;
                            }

                            HOperatorSet.GenRectangle2(out HObject rectangleRegion, row, column, phi, length1, length2);
                            using (rectangleRegion)
                            {
                                double side1 = GetSheetSizeActualSideLength(length1 * 2d, phi);
                                double side2 = GetSheetSizeActualSideLength(length2 * 2d, phi + (Math.PI / 2d));
                                double longSide = Math.Max(side1, side2);
                                double shortSide = Math.Min(side1, side2);

                                result = new SheetSizeJudgeResult
                                {
                                    Rectangularity = rectangularity,
                                    Length = longSide,
                                    Width = shortSide,
                                    BeforeRectangle2Region = capturePreviewRegions
                                        ? CloneSheetSizeJudgePreviewRegion(largestRegion, "DefectPostProcess clone sheet region before rectangle2 failed")
                                        : null,
                                    AfterRectangle2Region = capturePreviewRegions
                                        ? CloneSheetSizeJudgePreviewRegion(rectangleRegion, "DefectPostProcess clone region after rectangle2 failed")
                                        : null,
                                    IsOk = rectangularity >= SheetSizeJudge.RectangularityMinimum
                                        && IsSheetSizeDimensionOk(longSide, SheetSizeJudge.StandardLength, SheetSizeJudge.LengthTolerance)
                                        && IsSheetSizeDimensionOk(shortSide, SheetSizeJudge.StandardWidth, SheetSizeJudge.WidthTolerance)
                                };

                                return true;
                            }
                        }
                    }
                }
            }
        }

        private static SheetSizeJudgeResult CreateNgSheetSizeJudgeResult()
        {
            return new SheetSizeJudgeResult { IsOk = false };
        }

        private void UpdateSheetSizeJudgePreviewRegions(SheetSizeJudgeResult result)
        {
            ClearSheetSizeJudgePreviewRegions();

            if (IsFastModeEnabled || result == null)
            {
                ClearSheetSizeJudgePreviewRois();
                return;
            }

            _sheetSizeJudgeBeforeRectangle2PreviewRegion = CloneSheetSizeJudgePreviewRegion(
                result.BeforeRectangle2Region,
                "DefectPostProcess cache sheet region before rectangle2 failed");
            _sheetSizeJudgeAfterRectangle2PreviewRegion = CloneSheetSizeJudgePreviewRegion(
                result.AfterRectangle2Region,
                "DefectPostProcess cache region after rectangle2 failed");

            ClearSheetSizeJudgePreviewRois();
            AddSheetSizeJudgePreviewRois();
            ShowHRoi();
        }

        private void AddSheetSizeJudgePreviewRois()
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            HObject beforeRectangle2Region = CloneSheetSizeJudgePreviewRegion(
                _sheetSizeJudgeBeforeRectangle2PreviewRegion,
                "DefectPostProcess clone sheet region before rectangle2 ROI failed");
            if (IsInitializedSafely(beforeRectangle2Region))
            {
                ShowHRoi(new HRoi(
                    Serial,
                    ModuleName,
                    "SheetSizeJudgeBeforeRectangle2",
                    HRoiType.输入区域,
                    "cyan",
                    beforeRectangle2Region,
                    _for: true));
            }

            HObject afterRectangle2Region = CloneSheetSizeJudgePreviewRegion(
                _sheetSizeJudgeAfterRectangle2PreviewRegion,
                "DefectPostProcess clone region after rectangle2 ROI failed");
            if (IsInitializedSafely(afterRectangle2Region))
            {
                ShowHRoi(new HRoi(
                    Serial,
                    ModuleName,
                    "SheetSizeJudgeAfterRectangle2",
                    HRoiType.检测范围,
                    "yellow",
                    afterRectangle2Region,
                    _for: true));
            }
        }

        private void ClearSheetSizeJudgePreviewRois()
        {
            if (mHRoi == null)
            {
                return;
            }

            foreach (HRoi roi in mHRoi
                .Where(item =>
                    string.Equals(item?.Remarks, "SheetSizeJudgeBeforeRectangle2", StringComparison.Ordinal)
                    || string.Equals(item?.Remarks, "SheetSizeJudgeAfterRectangle2", StringComparison.Ordinal))
                .ToList())
            {
                DisposeRoiObject(roi);
                mHRoi.Remove(roi);
            }
        }

        private void ClearSheetSizeJudgePreviewRegions()
        {
            ClearSheetSizeJudgePreviewRois();
            DisposeHObjectSafely(_sheetSizeJudgeBeforeRectangle2PreviewRegion);
            DisposeHObjectSafely(_sheetSizeJudgeAfterRectangle2PreviewRegion);
            _sheetSizeJudgeBeforeRectangle2PreviewRegion = null;
            _sheetSizeJudgeAfterRectangle2PreviewRegion = null;
        }

        private static void DisposeHObjectSafely(HObject hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }

        private static void DisposeSheetSizeJudgeResultPreviewRegions(SheetSizeJudgeResult result)
        {
            if (result == null)
            {
                return;
            }

            DisposeHObjectSafely(result.BeforeRectangle2Region);
            DisposeHObjectSafely(result.AfterRectangle2Region);
            result.BeforeRectangle2Region = null;
            result.AfterRectangle2Region = null;
        }

        private static HObject CloneSheetSizeJudgePreviewRegion(HObject hObject, string logPrefix)
        {
            return SafeCloneHObject(hObject, logPrefix);
        }

        private static bool TrySelectLargestConnectedComponent(HObject connectedRegions, out HObject largestRegion)
        {
            largestRegion = null;
            HOperatorSet.AreaCenter(connectedRegions, out HTuple areas, out _, out _);

            int areaCount = areas.TupleLength();
            if (areaCount <= 0)
            {
                return false;
            }

            double maxArea = 0;
            int maxAreaIndex = -1;
            for (int index = 0; index < areaCount; index++)
            {
                double area = areas[index].D;
                if (area > maxArea)
                {
                    maxArea = area;
                    maxAreaIndex = index;
                }
            }

            if (maxAreaIndex < 0 || maxArea <= 0)
            {
                return false;
            }

            HOperatorSet.SelectObj(connectedRegions, out largestRegion, maxAreaIndex + 1);
            return largestRegion != null && largestRegion.IsInitialized();
        }

        private static bool TryGetRectangle2(
            HObject region,
            out double row,
            out double column,
            out double phi,
            out double length1,
            out double length2)
        {
            row = 0;
            column = 0;
            phi = 0;
            length1 = 0;
            length2 = 0;

            HOperatorSet.SmallestRectangle2(region, out HTuple rowTuple, out HTuple columnTuple, out HTuple phiTuple, out HTuple length1Tuple, out HTuple length2Tuple);
            row = rowTuple.D;
            column = columnTuple.D;
            phi = phiTuple.D;
            length1 = length1Tuple.D;
            length2 = length2Tuple.D;

            return length1 > 0 && length2 > 0;
        }

        private static bool TryGetRegionRectangularity(HObject region, out double rectangularity)
        {
            rectangularity = 0d;
            if (region == null)
            {
                return false;
            }

            try
            {
                HOperatorSet.Rectangularity(region, out HTuple rectangularityTuple);
                rectangularity = rectangularityTuple.D;
                return rectangularity > 0d;
            }
            catch (Exception ex)
            {
                LogStaticWarning($"计算片材矩形度失败: {ex.Message}");
                return false;
            }
        }

        private double GetSheetSizeActualSideLength(double pixelLength, double angle)
        {
            if (pixelLength <= 0)
            {
                return 0;
            }

            if (!TryGetSheetSizePixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY))
            {
                return pixelLength;
            }

            double cosValue = Math.Cos(angle);
            double sinValue = Math.Sin(angle);
            double equivalent = Math.Sqrt(
                (cosValue * cosValue) * pixelEquivalentX * pixelEquivalentX +
                (sinValue * sinValue) * pixelEquivalentY * pixelEquivalentY);

            return pixelLength * equivalent;
        }

        private bool TryGetSheetSizePixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY)
        {
            return TryGetCalibrationIntervalPixelEquivalent(out pixelEquivalentX, out pixelEquivalentY);
        }

        private static bool IsSheetSizeDimensionOk(double actualValue, double standardValue, double tolerance)
        {
            if (standardValue <= 0)
            {
                return true;
            }

            return Math.Abs(actualValue - standardValue) <= Math.Abs(tolerance);
        }

        #endregion

        #endregion
    }
}

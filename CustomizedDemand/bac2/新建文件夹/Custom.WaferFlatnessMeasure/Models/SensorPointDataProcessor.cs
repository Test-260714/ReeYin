using Custom.WaferFlatnessMeasure;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Logger;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.Models
{
    internal sealed class DataAnalysisSourceFilter
    {
        public DataAnalysisSourceFilter(
            DataAnalysisDataSourceOption source,
            string sourceName,
            double minValue,
            double maxValue)
        {
            Source = source;
            SourceName = sourceName;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public DataAnalysisDataSourceOption Source { get; }

        public string SourceName { get; }

        public double MinValue { get; }

        public double MaxValue { get; }

        public int RemovedCount { get; set; }
    }

    internal sealed class SensorPointDataProcessingOptions
    {
        public List<MeasureData>? SensorDatas { get; set; }

        public IReadOnlyList<PreprocessDatasetModel>? PreDatas { get; set; }

        public IReadOnlyList<PointCollectionStepInfo>? PointCollectionSteps { get; set; }

        public IReadOnlyList<DataAnalysisSourceFilter>? RawDataFilters { get; set; }

        public IReadOnlyList<DataAnalysisDataSourceOption>? DataAnalysisDataSources { get; set; }

        public string UpSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;

        public string DownSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;

        public int PointCollectionTrimCountPerSide { get; set; } = 2;
    }

    internal sealed class SensorPointDataProcessingResult
    {
        public List<MeasureData> DataCollect { get; set; } = new List<MeasureData>();

        public List<MeasureData> CalibrationWaferDatas { get; set; } = new List<MeasureData>();

        public bool HasCalibrationWaferReference { get; set; }
    }

    internal static class SensorPointDataProcessor
    {
        public static List<PreprocessDatasetModel> BuildPreprocessDatas(
            IEnumerable<MeasureData>? dataCollect,
            string upSurfaceOriginalDataValueName,
            string downSurfaceOriginalDataValueName)
        {
            return PreprocessDatasetModel.CreateFromMeasureDatas(
                dataCollect,
                upSurfaceOriginalDataValueName,
                downSurfaceOriginalDataValueName);
        }

        public static List<PreprocessDatasetModel> FilterFinalSurfacePreDatas(
            IEnumerable<PreprocessDatasetModel>? dataCollect,
            IReadOnlyList<DataAnalysisSourceFilter>? activeFilters,
            Func<PreprocessDatasetModel, DataAnalysisDataSourceOption, double> getDataAnalysisSourceValue)
        {
            dataCollect ??= Enumerable.Empty<PreprocessDatasetModel>();
            activeFilters ??= Array.Empty<DataAnalysisSourceFilter>();

            if (getDataAnalysisSourceValue == null)
            {
                throw new ArgumentNullException(nameof(getDataAnalysisSourceValue));
            }

            int removedCount = 0;
            int addedCount = 0;
            List<PreprocessDatasetModel> filteredDatas = new List<PreprocessDatasetModel>();

            foreach (PreprocessDatasetModel data in dataCollect)
            {
                if (activeFilters.Count > 0)
                {
                    List<string> invalidReasons = new List<string>();
                    foreach (DataAnalysisSourceFilter filter in activeFilters)
                    {
                        double sourceValue = getDataAnalysisSourceValue(data, filter.Source);
                        if (!IsSurfaceValueInRange(sourceValue, filter.MinValue, filter.MaxValue))
                        {
                            filter.RemovedCount++;
                            invalidReasons.Add(
                                $"{filter.SourceName}={sourceValue:F3} 不在设定范围 [{filter.MinValue:F3}, {filter.MaxValue:F3}] 内");
                        }
                    }

                    if (invalidReasons.Count > 0)
                    {
                        removedCount++;
                        Logs.LogWarning(
                            $"采集点 X={data.PosX:F3}, Y={data.PosY:F3} 的 {string.Join("；", invalidReasons)}，已从本次最终 UpSurface/DownSurface 有效结果中剔除。");
                        continue;
                    }
                }

                filteredDatas.Add(data);
                addedCount++;
            }

            if (activeFilters.Count > 0)
            {
                string filterSummary = string.Join(
                    "，",
                    activeFilters.Select(filter => $"{filter.SourceName} 超范围 {filter.RemovedCount} 条"));
                Logs.LogInfo(
                    $"最终 UpSurface/DownSurface 范围过滤完成，保留 {addedCount} 条，移除 {removedCount} 条，其中 {filterSummary}。");
            }

            return filteredDatas;
        }

        public static SensorPointDataProcessingResult ProcessCollectedData(SensorPointDataProcessingOptions options)
        {
            options ??= new SensorPointDataProcessingOptions();
            List<MeasureData> sensorDatas = options.SensorDatas ?? new List<MeasureData>();
            IReadOnlyList<PreprocessDatasetModel> preDatas = options.PreDatas ?? Array.Empty<PreprocessDatasetModel>();
            IReadOnlyList<DataAnalysisSourceFilter> rawDataFilters = options.RawDataFilters ?? Array.Empty<DataAnalysisSourceFilter>();

            if (sensorDatas.Count == 0)
            {
                Logs.LogWarning("未接收到传感器数据。");
                return new SensorPointDataProcessingResult { DataCollect = sensorDatas };
            }

            if (preDatas.Count == 0)
            {
                Logs.LogWarning("轨迹坐标为空，返回原始采集数据。");
                return new SensorPointDataProcessingResult { DataCollect = sensorDatas };
            }

            if (TryGetActivePointCollectionSteps(options.PointCollectionSteps, preDatas, out List<PointCollectionStepInfo> pointCollectionSteps))
            {
                return ProcessCollectedDataByPointCollectionSteps(sensorDatas, preDatas, pointCollectionSteps, rawDataFilters, options);
            }

            if (sensorDatas.Count <= preDatas.Count && rawDataFilters.Count == 0)
            {
                return new SensorPointDataProcessingResult
                {
                    DataCollect = AlignCollectedDataWithPositions(sensorDatas, preDatas)
                };
            }

            var groupedDatas = SplitSensorDataByPointCount(sensorDatas, preDatas.Count);
            if (rawDataFilters.Count > 0)
            {
                groupedDatas = FilterRawMeasureDataGroups(groupedDatas, rawDataFilters, options);
            }

            Dictionary<string, int> trimCountsByOriginalDataValueName = BuildOriginalDataTrimCountMap(options.DataAnalysisDataSources);
            var aggregatedDatas = new List<MeasureData>(groupedDatas.Count);

            for (int groupIndex = 0; groupIndex < groupedDatas.Count; groupIndex++)
            {
                List<MeasureData> group = groupedDatas[groupIndex] ?? new List<MeasureData>();
                if (group.Count == 0)
                {
                    Logs.LogWarning($"第 {groupIndex + 1} 个采集点没有有效数据，已从本次预处理结果中删除。");
                    continue;
                }

                MeasureData aggregatedData = AggregateMeasureDataGroup(
                    group,
                    options.PointCollectionTrimCountPerSide,
                    trimCountsByOriginalDataValueName);
                AssignCollectedDataPosition(aggregatedData, groupIndex, preDatas);
                aggregatedDatas.Add(aggregatedData);
            }

            return new SensorPointDataProcessingResult { DataCollect = aggregatedDatas };
        }

        public static List<MeasureData> AlignCollectedDataWithPositions(
            List<MeasureData>? dataCollect,
            IReadOnlyList<PreprocessDatasetModel>? preDatas)
        {
            dataCollect ??= new List<MeasureData>();
            preDatas ??= Array.Empty<PreprocessDatasetModel>();

            if (preDatas.Count == 0)
            {
                if (dataCollect.Count > 0)
                {
                    Logs.LogWarning("轨迹坐标为空，已跳过采集数据坐标回填。");
                }

                return dataCollect;
            }

            if (dataCollect.Count > preDatas.Count)
            {
                Logs.LogWarning($"采集数据数量 {dataCollect.Count} 多于轨迹点数量 {preDatas.Count}，已截断多余数据。");
                dataCollect = dataCollect.Take(preDatas.Count).ToList();
            }
            else if (dataCollect.Count < preDatas.Count)
            {
                Logs.LogWarning($"采集数据数量 {dataCollect.Count} 少于轨迹点数量 {preDatas.Count}，缺失点已从本次预处理结果中删除。");
            }

            for (int i = 0; i < dataCollect.Count; i++)
            {
                AssignCollectedDataPosition(dataCollect[i], i, preDatas);
            }

            return dataCollect;
        }

        private static bool TryGetActivePointCollectionSteps(
            IReadOnlyList<PointCollectionStepInfo>? sourceSteps,
            IReadOnlyList<PreprocessDatasetModel> preDatas,
            out List<PointCollectionStepInfo> pointCollectionSteps)
        {
            pointCollectionSteps = (sourceSteps ?? Array.Empty<PointCollectionStepInfo>())
                .Where(step => step != null)
                .Select(step => step.Clone())
                .ToList();

            if (pointCollectionSteps.Count == 0 ||
                !pointCollectionSteps.Any(step => step.IsCalibrationReference) ||
                preDatas.Count == 0)
            {
                return false;
            }

            int normalStepCount = pointCollectionSteps.Count(step => !step.IsCalibrationReference);
            if (normalStepCount != preDatas.Count)
            {
                Logs.LogWarning($"标准片测量序列中的晶圆点数量 {normalStepCount} 与轨迹点数量 {preDatas.Count} 不一致，已按普通点位数据处理。");
                return false;
            }

            if (!DoPointCollectionStepsMatchPreDatas(pointCollectionSteps, preDatas))
            {
                Logs.LogWarning("标准片测量序列与当前轨迹坐标不一致，已按普通点位数据处理。");
                return false;
            }

            return true;
        }

        private static bool DoPointCollectionStepsMatchPreDatas(
            IReadOnlyList<PointCollectionStepInfo> pointCollectionSteps,
            IReadOnlyList<PreprocessDatasetModel> preDatas)
        {
            const double positionTolerance = 1e-6;
            int normalPointIndex = 0;

            foreach (PointCollectionStepInfo step in pointCollectionSteps)
            {
                if (step.IsCalibrationReference)
                {
                    continue;
                }

                if (normalPointIndex >= preDatas.Count)
                {
                    return false;
                }

                PreprocessDatasetModel preData = preDatas[normalPointIndex];
                if (Math.Abs(preData.PosX - step.X) > positionTolerance ||
                    Math.Abs(preData.PosY - step.Y) > positionTolerance)
                {
                    return false;
                }

                normalPointIndex++;
            }

            return normalPointIndex == preDatas.Count;
        }

        private static SensorPointDataProcessingResult ProcessCollectedDataByPointCollectionSteps(
            List<MeasureData> sensorDatas,
            IReadOnlyList<PreprocessDatasetModel> preDatas,
            IReadOnlyList<PointCollectionStepInfo> pointCollectionSteps,
            IReadOnlyList<DataAnalysisSourceFilter> rawDataFilters,
            SensorPointDataProcessingOptions options)
        {
            var groupedDatas = SplitSensorDataByPointCount(sensorDatas, pointCollectionSteps.Count);
            Dictionary<string, int> trimCountsByOriginalDataValueName = BuildOriginalDataTrimCountMap(options.DataAnalysisDataSources);
            List<MeasureData> calibrationWaferDatas = BuildCalibrationWaferDatas(
                groupedDatas,
                pointCollectionSteps,
                options.PointCollectionTrimCountPerSide,
                trimCountsByOriginalDataValueName);

            if (rawDataFilters.Count > 0)
            {
                groupedDatas = FilterRawMeasureDataGroups(groupedDatas, rawDataFilters, options);
            }

            var aggregatedDatas = new List<MeasureData>(preDatas.Count);
            int normalPointIndex = 0;
            int referencePointCount = 0;
            int referenceSampleCount = 0;

            for (int groupIndex = 0; groupIndex < pointCollectionSteps.Count; groupIndex++)
            {
                PointCollectionStepInfo step = pointCollectionSteps[groupIndex];
                List<MeasureData> group = groupedDatas[groupIndex] ?? new List<MeasureData>();

                if (step.IsCalibrationReference)
                {
                    referencePointCount++;
                    referenceSampleCount += group.Count;
                    continue;
                }

                if (group.Count == 0)
                {
                    Logs.LogWarning($"第 {normalPointIndex + 1} 个采集点没有有效数据，已从本次预处理结果中删除。");
                    normalPointIndex++;
                    continue;
                }

                MeasureData aggregatedData = AggregateMeasureDataGroup(
                    group,
                    options.PointCollectionTrimCountPerSide,
                    trimCountsByOriginalDataValueName);
                AssignCollectedDataPosition(aggregatedData, normalPointIndex, preDatas);
                aggregatedDatas.Add(aggregatedData);
                normalPointIndex++;
            }

            Logs.LogInfo($"标准片测量数据已从平面度计算输入中排除：标准片点 {referencePointCount} 个，原始数据 {referenceSampleCount} 行。");
            return new SensorPointDataProcessingResult
            {
                DataCollect = aggregatedDatas,
                CalibrationWaferDatas = calibrationWaferDatas,
                HasCalibrationWaferReference = true
            };
        }

        private static List<MeasureData> BuildCalibrationWaferDatas(
            IReadOnlyList<List<MeasureData>> groupedDatas,
            IReadOnlyList<PointCollectionStepInfo> pointCollectionSteps,
            int trimCountPerSide,
            IReadOnlyDictionary<string, int> trimCountsByOriginalDataValueName)
        {
            var calibrationDatas = new List<MeasureData>();

            for (int groupIndex = 0; groupIndex < pointCollectionSteps.Count; groupIndex++)
            {
                PointCollectionStepInfo step = pointCollectionSteps[groupIndex];
                if (!step.IsCalibrationReference)
                {
                    continue;
                }

                List<MeasureData> group = groupIndex < groupedDatas.Count
                    ? groupedDatas[groupIndex] ?? new List<MeasureData>()
                    : new List<MeasureData>();

                if (group.Count == 0)
                {
                    Logs.LogWarning($"第 {groupIndex + 1} 个标准片采集点没有有效数据，本次未写入标准片 CSV。");
                    continue;
                }

                MeasureData aggregatedData = AggregateMeasureDataGroup(
                    group,
                    trimCountPerSide,
                    trimCountsByOriginalDataValueName);
                aggregatedData.X = step.X;
                aggregatedData.Y = step.Y;
                calibrationDatas.Add(aggregatedData);
            }

            return calibrationDatas;
        }

        private static List<List<MeasureData>> FilterRawMeasureDataGroups(
            List<List<MeasureData>> groupedDatas,
            IReadOnlyList<DataAnalysisSourceFilter> activeFilters,
            SensorPointDataProcessingOptions options)
        {
            if (groupedDatas == null || groupedDatas.Count == 0 || activeFilters == null || activeFilters.Count == 0)
            {
                return groupedDatas ?? new List<List<MeasureData>>();
            }

            int removedCount = 0;
            int keptCount = 0;
            var filteredGroups = new List<List<MeasureData>>(groupedDatas.Count);

            for (int groupIndex = 0; groupIndex < groupedDatas.Count; groupIndex++)
            {
                List<MeasureData> group = groupedDatas[groupIndex] ?? new List<MeasureData>();
                var filteredGroup = new List<MeasureData>(group.Count);

                foreach (MeasureData data in group)
                {
                    bool isValid = true;
                    foreach (DataAnalysisSourceFilter filter in activeFilters)
                    {
                        double sourceValue = GetRawDataAnalysisSourceValue(data, filter.Source, options);
                        if (!IsSurfaceValueInRange(sourceValue, filter.MinValue, filter.MaxValue))
                        {
                            filter.RemovedCount++;
                            isValid = false;
                        }
                    }

                    if (isValid)
                    {
                        filteredGroup.Add(data);
                        keptCount++;
                    }
                    else
                    {
                        removedCount++;
                    }
                }

                if (group.Count > 0 && filteredGroup.Count == 0)
                {
                    Logs.LogWarning($"第 {groupIndex + 1} 个采集点的原始数据经过范围过滤后无有效行。");
                }

                filteredGroups.Add(filteredGroup);
            }

            string filterSummary = string.Join(
                "，",
                activeFilters.Select(filter => $"{filter.SourceName} 超范围 {filter.RemovedCount} 行"));
            Logs.LogInfo(
                $"原始采样范围过滤完成，保留 {keptCount} 行，移除 {removedCount} 行，其中 {filterSummary}。");

            return filteredGroups;
        }

        private static double GetRawDataAnalysisSourceValue(
            MeasureData? data,
            DataAnalysisDataSourceOption source,
            SensorPointDataProcessingOptions options)
        {
            if (data == null || source == null)
            {
                return double.NaN;
            }

            if (PreprocessDatasetModel.TryGetMeasureDataOriginalDataValue(
                    data,
                    source.OriginalDataValueName,
                    out double sourceValue))
            {
                return sourceValue;
            }

            if (string.Equals(source.Name, SensorDataCollectionModel.UpSurfaceResultOption, StringComparison.OrdinalIgnoreCase))
            {
                return PreprocessDatasetModel.GetMeasureDataOriginalDataValue(
                    data,
                    options.UpSurfaceOriginalDataValueName,
                    PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName);
            }

            if (string.Equals(source.Name, SensorDataCollectionModel.DownSurfaceResultOption, StringComparison.OrdinalIgnoreCase))
            {
                return PreprocessDatasetModel.GetMeasureDataOriginalDataValue(
                    data,
                    options.DownSurfaceOriginalDataValueName,
                    PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName);
            }

            return double.NaN;
        }

        private static bool IsSurfaceValueInRange(double surfaceValue, double minValue, double maxValue)
        {
            return !double.IsNaN(surfaceValue) &&
                   !double.IsInfinity(surfaceValue) &&
                   surfaceValue >= minValue &&
                   surfaceValue <= maxValue;
        }

        private static void AssignCollectedDataPosition(
            MeasureData data,
            int positionIndex,
            IReadOnlyList<PreprocessDatasetModel> preDatas)
        {
            if (data == null || positionIndex < 0 || positionIndex >= preDatas.Count)
            {
                return;
            }

            data.X = preDatas[positionIndex].PosX;
            data.Y = preDatas[positionIndex].PosY;
        }

        private static List<List<MeasureData>> SplitSensorDataByPointCount(List<MeasureData> sensorDatas, int pointCount)
        {
            var groupedDatas = new List<List<MeasureData>>(pointCount);
            int baseGroupSize = sensorDatas.Count / pointCount;
            int remainder = sensorDatas.Count % pointCount;
            int startIndex = 0;

            for (int i = 0; i < pointCount; i++)
            {
                int currentGroupSize = baseGroupSize + (i < remainder ? 1 : 0);
                groupedDatas.Add(currentGroupSize > 0
                    ? sensorDatas.GetRange(startIndex, currentGroupSize)
                    : new List<MeasureData>());
                startIndex += currentGroupSize;
            }

            return groupedDatas;
        }

        private static Dictionary<string, int> BuildOriginalDataTrimCountMap(
            IEnumerable<DataAnalysisDataSourceOption>? dataSources)
        {
            var trimCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (dataSources == null)
            {
                return trimCounts;
            }

            foreach (DataAnalysisDataSourceOption source in dataSources.Where(source => source != null))
            {
                string normalizedValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(source.OriginalDataValueName);
                if (string.IsNullOrWhiteSpace(normalizedValueName) || trimCounts.ContainsKey(normalizedValueName))
                {
                    continue;
                }

                trimCounts[normalizedValueName] = Math.Max(0, source.PointCollectionTrimCountPerSide);
            }

            return trimCounts;
        }

        private static MeasureData AggregateMeasureDataGroup(
            List<MeasureData>? measureDatas,
            int trimCountPerSide,
            IReadOnlyDictionary<string, int>? trimCountsByOriginalDataValueName)
        {
            measureDatas ??= new List<MeasureData>();
            if (measureDatas.Count == 0)
            {
                return new MeasureData();
            }

            int normalizedTrimCount = Math.Max(0, trimCountPerSide);
            var trimmedDatas = TrimEdgeSamples(measureDatas, normalizedTrimCount);
            var effectiveDatas = trimmedDatas.Count > 0 ? trimmedDatas : measureDatas;
            var representativeData = effectiveDatas[effectiveDatas.Count / 2];

            return new MeasureData
            {
                OriginalDatas = AverageOriginalDatas(
                    measureDatas,
                    effectiveDatas,
                    trimCountsByOriginalDataValueName),
                AreaData = representativeData.AreaData,
                RTime = representativeData.RTime,
                Z = effectiveDatas.Average(data => data.Z),
                IsValid = effectiveDatas.Any(data => data.IsValid),
            };
        }

        private static List<MeasureData> TrimEdgeSamples(List<MeasureData> measureDatas, int trimCountPerSide)
        {
            if (measureDatas.Count <= trimCountPerSide * 2)
            {
                return measureDatas;
            }

            return measureDatas
                .Skip(trimCountPerSide)
                .Take(measureDatas.Count - trimCountPerSide * 2)
                .ToList();
        }

        private static Dictionary<string, Dictionary<string, object>> AverageOriginalDatas(
            IReadOnlyList<MeasureData> allMeasureDatas,
            IEnumerable<MeasureData> fallbackMeasureDatas,
            IReadOnlyDictionary<string, int>? trimCountsByOriginalDataValueName)
        {
            Dictionary<string, Dictionary<string, List<double>>> collectedValues = new Dictionary<string, Dictionary<string, List<double>>>(StringComparer.OrdinalIgnoreCase);

            HashSet<string> sourceSpecificValueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (trimCountsByOriginalDataValueName != null && trimCountsByOriginalDataValueName.Count > 0)
            {
                foreach (KeyValuePair<string, int> trimPair in trimCountsByOriginalDataValueName)
                {
                    AddOriginalDataValues(
                        collectedValues,
                        TrimEdgeSamples(allMeasureDatas.ToList(), Math.Max(0, trimPair.Value)),
                        trimPair.Key);
                    sourceSpecificValueNames.Add(trimPair.Key);
                }
            }

            AddOriginalDataValues(collectedValues, fallbackMeasureDatas, null, sourceSpecificValueNames);

            Dictionary<string, Dictionary<string, object>> averagedDatas = new Dictionary<string, Dictionary<string, object>>();
            foreach (KeyValuePair<string, Dictionary<string, List<double>>> channelPair in collectedValues)
            {
                Dictionary<string, object> averagedTypeValues = channelPair.Value
                    .Where(typePair => typePair.Value.Count > 0)
                    .ToDictionary(
                        typePair => typePair.Key,
                        typePair => (object)typePair.Value.Average());

                if (averagedTypeValues.Count > 0)
                {
                    averagedDatas[channelPair.Key] = averagedTypeValues;
                }
            }

            return averagedDatas;
        }

        private static void AddOriginalDataValues(
            Dictionary<string, Dictionary<string, List<double>>> collectedValues,
            IEnumerable<MeasureData> measureDatas,
            string? onlyOriginalDataValueName = null,
            ISet<string>? excludedOriginalDataValueNames = null)
        {
            string normalizedOnlyValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(onlyOriginalDataValueName);

            foreach (MeasureData measureData in measureDatas)
            {
                if (measureData.OriginalDatas == null || measureData.OriginalDatas.Count == 0)
                {
                    continue;
                }

                foreach (KeyValuePair<string, Dictionary<string, object>> channelPair in measureData.OriginalDatas)
                {
                    if (string.IsNullOrWhiteSpace(channelPair.Key) || channelPair.Value == null)
                    {
                        continue;
                    }

                    if (!collectedValues.TryGetValue(channelPair.Key, out Dictionary<string, List<double>>? typeValues))
                    {
                        typeValues = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
                        collectedValues[channelPair.Key] = typeValues;
                    }

                    foreach (KeyValuePair<string, object> typePair in channelPair.Value)
                    {
                        string originalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                            $"{channelPair.Key}.{typePair.Key}");
                        if (!string.IsNullOrWhiteSpace(normalizedOnlyValueName) &&
                            !string.Equals(originalDataValueName, normalizedOnlyValueName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (excludedOriginalDataValueNames != null &&
                            excludedOriginalDataValueNames.Contains(originalDataValueName))
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(typePair.Key) ||
                            !PreprocessDatasetModel.TryConvertOriginalDataValue(typePair.Value, out double value))
                        {
                            continue;
                        }

                        if (!typeValues.TryGetValue(typePair.Key, out List<double>? values))
                        {
                            values = new List<double>();
                            typeValues[typePair.Key] = values;
                        }

                        values.Add(value);
                    }
                }
            }
        }
    }
}

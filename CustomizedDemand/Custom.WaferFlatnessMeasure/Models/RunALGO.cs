using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Custom.WaferFlatnessMeasure.Models
{
    public partial class SensorDataCollectionModel : ModelParamBase
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public void RunALGO()
        {
            if (!IsPoint)
            {
                return;
            }

            if (PreDatas == null || PreDatas.Count == 0)
            {
                Logs.LogWarning("PreDatas 为空，无法执行平面度算法。");
                ResetMeasurementResults();
                RefreshOutputParamValues();
                return;
            }

            ALGO ??= new Flatness_Algorithm(MeasureParam);
            EnsureDataAnalysisConfiguration();

            List<PreprocessDatasetModel> algoPreDatas = PreprocessDatasetModel.Clone(PreDatas);
            List<DataAnalysisDataSourceOption> enabledSources = GetEnabledDataAnalysisSources();
            if (enabledSources.Count == 0)
            {
                Logs.LogWarning("未启用任何数据分析数据源，已跳过算法计算。");
                ResetMeasurementResults();
                RefreshOutputParamValues();
                return;
            }

            if (IsUsingCalib && enabledSources.Count > 0)
            {
                List<double[]> calibrationSourcePoints = BuildDataSourcePointCloud(algoPreDatas, enabledSources[0]);
                var calibResult = _calibALGO.CompensatePoints(calibrationSourcePoints, FlatCalibCompensationMode.Diagnostic);
                if (calibResult == null || !calibResult.Success || calibResult.CompensatedPoints == null)
                {
                    MessageBox.Show("标定结果无效，无法进行计算！");
                    Logs.LogWarning("标定结果无效，已取消本次算法计算。");
                    ResetMeasurementResults();
                    RefreshOutputParamValues();
                    return;
                }

                ApplyDataSourceCompensation(algoPreDatas, enabledSources[0], calibResult.CompensatedPoints);
            }

            algoPreDatas = FilterFinalSurfacePreDatas(algoPreDatas);
            if (algoPreDatas.Count == 0)
            {
                Logs.LogWarning("最终 UpSurface/DownSurface 过滤后无有效点，已跳过算法计算。");
                ResetMeasurementResults();
                ValidCollect = new List<double[]>();
                DownValidCollect = new List<double[]>();
                RefreshOutputParamValues();
                return;
            }

            Dictionary<string, List<double[]>> runAlgoPointClouds = new Dictionary<string, List<double[]>>();
            List<DisplayPointCloudCandidate> displayPointCloudCandidates = new List<DisplayPointCloudCandidate>();
            Dictionary<DataAnalysisDataSourceOption, List<double[]>> sourcePointClouds = enabledSources
                .ToDictionary(source => source, source => BuildDataSourcePointCloud(algoPreDatas, source));

            ValidCollect = sourcePointClouds.TryGetValue(enabledSources[0], out List<double[]> firstSourcePoints)
                ? firstSourcePoints
                : new List<double[]>();
            DownValidCollect = enabledSources.Count > 1 && sourcePointClouds.TryGetValue(enabledSources[1], out List<double[]> secondSourcePoints)
                ? secondSourcePoints
                : new List<double[]>();

            ResetMeasurementResults();
            List<DataAnalysisAlgorithmOption> enabledAlgorithms = DataAnalysisAlgorithms
                .Where(algorithm => algorithm != null && algorithm.IsEnabled)
                .ToList();

            if (enabledAlgorithms.Count == 0)
            {
                Logs.LogWarning("未勾选任何数据分析算法，已跳过算法计算。");
            }

            foreach (DataAnalysisAlgorithmOption algorithm in enabledAlgorithms)
            {
                if (DataAnalysisAlgorithmOption.RequiresPairSources(algorithm.Algorithm))
                {
                    RunPairDataAnalysisAlgorithm(
                        algorithm,
                        enabledSources,
                        algoPreDatas,
                        runAlgoPointClouds,
                        displayPointCloudCandidates);
                }
                else
                {
                    RunSingleDataAnalysisAlgorithm(
                        algorithm.Algorithm,
                        enabledSources,
                        sourcePointClouds,
                        runAlgoPointClouds,
                        displayPointCloudCandidates);
                }
            }

            ApplyDisplayPointCloud(displayPointCloudCandidates);

            SaveRunAlgoPointClouds(runAlgoPointClouds);
            SaveRunAlgoPointCloudImages(runAlgoPointClouds);
            AppendDataAnalysisResultsToLastCsv();

            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("DisposeImage");
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("DisposeDatas", ResultPointCloud));
            ExecuteOnUiThreadSync(() =>
            {
                RaisePropertyChanged(nameof(DataAnalysisResultCount));
                RaisePropertyChanged(nameof(LastMeasurementSummary));
                RaiseSelectedSurfaceResultChanged();
            });
            RefreshOutputParamValues();
        }

        private void RunSingleDataAnalysisAlgorithm(
            DataAnalysisAlgorithmKind algorithm,
            IReadOnlyList<DataAnalysisDataSourceOption> enabledSources,
            IReadOnlyDictionary<DataAnalysisDataSourceOption, List<double[]>> sourcePointClouds,
            IDictionary<string, List<double[]>> runAlgoPointClouds,
            ICollection<DisplayPointCloudCandidate> displayPointCloudCandidates)
        {
            foreach (DataAnalysisDataSourceOption source in enabledSources)
            {
                List<double[]> points = sourcePointClouds.TryGetValue(source, out List<double[]> sourcePoints)
                    ? ALGO.ToPoint3D(sourcePoints, false)
                    : new List<double[]>();

                switch (algorithm)
                {
                    case DataAnalysisAlgorithmKind.Flatness:
                        RunFlatnessAlgorithm(source, enabledSources, points, runAlgoPointClouds, displayPointCloudCandidates);
                        break;
                    case DataAnalysisAlgorithmKind.SurfaceStatistics:
                        RunSurfaceStatisticsAlgorithm(source, enabledSources, points);
                        break;
                }
            }
        }

        private void RunFlatnessAlgorithm(
            DataAnalysisDataSourceOption source,
            IReadOnlyList<DataAnalysisDataSourceOption> enabledSources,
            List<double[]> points,
            IDictionary<string, List<double[]>> runAlgoPointClouds,
            ICollection<DisplayPointCloudCandidate> displayPointCloudCandidates)
        {
            string sourceName = GetDataSourceDisplayName(source);
            if (points.Count < 3)
            {
                AddDataAnalysisResult(DataAnalysisAlgorithmKind.Flatness, sourceName, double.NaN, double.NaN, double.NaN, points.Count, "点数不足");
                Logs.LogWarning($"{sourceName} 有效点数量不足，当前仅 {points.Count} 条，已跳过平面度计算。");
                return;
            }

            int status = ALGO.Flatness(points, out double flatness, out List<double[]> flatnessPCD);
            AddDataAnalysisResult(DataAnalysisAlgorithmKind.Flatness, sourceName, flatness, double.NaN, double.NaN, points.Count, status == 0 ? "成功" : "失败");
            runAlgoPointClouds[$"Flatness_{sourceName}"] = flatnessPCD;
            displayPointCloudCandidates.Add(new DisplayPointCloudCandidate(50, $"{sourceName}平面度点云", flatnessPCD));

            int sourceIndex = GetDataSourceIndex(enabledSources, source);
            if (sourceIndex == 0)
            {
                LastUpSurfaceFlatnessValue = flatness;
            }
            else if (sourceIndex == 1)
            {
                LastDownSurfaceFlatnessValue = flatness;
            }
        }

        private void RunSurfaceStatisticsAlgorithm(
            DataAnalysisDataSourceOption source,
            IReadOnlyList<DataAnalysisDataSourceOption> enabledSources,
            List<double[]> points)
        {
            string sourceName = GetDataSourceDisplayName(source);
            if (!TryCalculateSurfaceStatistics(points, out double ttv, out double minValue, out double maxValue))
            {
                AddDataAnalysisResult(DataAnalysisAlgorithmKind.SurfaceStatistics, sourceName, double.NaN, double.NaN, double.NaN, points.Count, "点数不足");
                return;
            }

            AddDataAnalysisResult(DataAnalysisAlgorithmKind.SurfaceStatistics, sourceName, ttv, minValue, maxValue, points.Count, "成功");
            int sourceIndex = GetDataSourceIndex(enabledSources, source);
            if (sourceIndex == 0)
            {
                LastUpSurfaceTtvValue = ttv;
                LastUpSurfaceMinValue = minValue;
                LastUpSurfaceMaxValue = maxValue;
            }
            else if (sourceIndex == 1)
            {
                LastDownSurfaceTtvValue = ttv;
                LastDownSurfaceMinValue = minValue;
                LastDownSurfaceMaxValue = maxValue;
            }
        }

        private void RunPairDataAnalysisAlgorithm(
            DataAnalysisAlgorithmOption algorithmOption,
            IReadOnlyList<DataAnalysisDataSourceOption> enabledSources,
            IReadOnlyList<PreprocessDatasetModel> algoPreDatas,
            IDictionary<string, List<double[]>> runAlgoPointClouds,
            ICollection<DisplayPointCloudCandidate> displayPointCloudCandidates)
        {
            DataAnalysisAlgorithmKind algorithm = algorithmOption.Algorithm;
            if (enabledSources.Count < 2)
            {
                AddDataAnalysisResult(algorithm, "双数据源", double.NaN, double.NaN, double.NaN, 0, "数据源不足");
                return;
            }

            DataAnalysisDataSourceOption? sourceA = FindDataSourceById(enabledSources, algorithmOption.SourceADataSourceId);
            DataAnalysisDataSourceOption? sourceB = FindDataSourceById(enabledSources, algorithmOption.SourceBDataSourceId);
            if (sourceA == null || sourceB == null)
            {
                AddDataAnalysisResult(algorithm, "双数据源", double.NaN, double.NaN, double.NaN, 0, "数据源未选择");
                Logs.LogWarning($"{DataAnalysisAlgorithmOption.GetDisplayName(algorithm)} 未选择有效的双数据源，已跳过计算。");
                return;
            }

            if (ReferenceEquals(sourceA, sourceB))
            {
                AddDataAnalysisResult(algorithm, GetDataSourceDisplayName(sourceA), double.NaN, double.NaN, double.NaN, 0, "数据源重复");
                Logs.LogWarning($"{DataAnalysisAlgorithmOption.GetDisplayName(algorithm)} 的数据源A和数据源B相同，已跳过计算。");
                return;
            }

            BuildPairedPointClouds(algoPreDatas, sourceA, sourceB, out List<double[]> pointsA, out List<double[]> pointsB);

            string pairName = $"{GetDataSourceDisplayName(sourceA)} - {GetDataSourceDisplayName(sourceB)}";
            if (pointsA.Count < 3 || pointsB.Count < 3 || pointsA.Count != pointsB.Count)
            {
                AddDataAnalysisResult(algorithm, pairName, double.NaN, double.NaN, double.NaN, Math.Min(pointsA.Count, pointsB.Count), "点数不足");
                Logs.LogWarning($"{pairName} 有效点数量不足或不匹配，已跳过 {DataAnalysisAlgorithmOption.GetDisplayName(algorithm)} 计算。");
                return;
            }

            List<double[]> point3DA = ALGO.ToPoint3D(pointsA, false);
            List<double[]> point3DB = ALGO.ToPoint3D(pointsB, false);
            RunPairDataAnalysisAlgorithmCore(
                algorithm,
                pairName,
                GetDataSourceIndex(enabledSources, sourceA),
                GetDataSourceIndex(enabledSources, sourceB),
                point3DA,
                point3DB,
                runAlgoPointClouds,
                displayPointCloudCandidates);
        }

        private void RunPairDataAnalysisAlgorithmCore(
            DataAnalysisAlgorithmKind algorithm,
            string pairName,
            int sourceAIndex,
            int sourceBIndex,
            List<double[]> pointsA,
            List<double[]> pointsB,
            IDictionary<string, List<double[]>> runAlgoPointClouds,
            ICollection<DisplayPointCloudCandidate> displayPointCloudCandidates)
        {
            int status;
            double value;
            double minValue = double.NaN;
            double maxValue = double.NaN;
            List<double[]> pointCloud;
            int priority;

            switch (algorithm)
            {
                case DataAnalysisAlgorithmKind.Parallelism:
                    status = ALGO.Parallelism(pointsA, pointsB, out value, out pointCloud);
                    priority = 30;
                    LastParallelismValue = value;
                    break;
                case DataAnalysisAlgorithmKind.TTV:
                    status = ALGO.TTV(pointsA, pointsB, out value, out pointCloud);
                    priority = 20;
                    LastTtvValue = value;
                    break;
                case DataAnalysisAlgorithmKind.THK:
                    status = ALGO.THK(pointsA, pointsB, out minValue, out maxValue, out pointCloud);
                    value = double.IsFinite(minValue) && double.IsFinite(maxValue)
                        ? maxValue - minValue
                        : double.NaN;
                    priority = 10;
                    LastThicknessValue = value;
                    LastThicknessMinValue = minValue;
                    LastThicknessMaxValue = maxValue;
                    LastThicknessRawPointCloud = pointCloud;
                    LastThicknessPointCloud = pointCloud;
                    LastThicknessNormalPointCloud = pointCloud;
                    break;
                case DataAnalysisAlgorithmKind.TIR:
                    status = ALGO.TIR(pointsA, pointsB, out value, out pointCloud);
                    priority = 25;
                    LastTirValue = value;
                    break;
                case DataAnalysisAlgorithmKind.Warp1:
                    status = ALGO.Warp1(pointsA, pointsB, out value, out pointCloud);
                    priority = 25;
                    LastWarp1Value = value;
                    break;
                case DataAnalysisAlgorithmKind.Warp2:
                    status = ALGO.Warp2(pointsA, pointsB, out value, out pointCloud);
                    priority = 25;
                    LastWarp2Value = value;
                    break;
                default:
                    return;
            }

            AddDataAnalysisResult(algorithm, pairName, value, minValue, maxValue, pointsA.Count, status == 0 ? "成功" : "失败");
            runAlgoPointClouds[$"{algorithm}_{pairName}"] = pointCloud;
            displayPointCloudCandidates.Add(new DisplayPointCloudCandidate(
                priority,
                $"{DataAnalysisAlgorithmOption.GetDisplayName(algorithm)}点云 {pairName}",
                pointCloud));
        }

        private static bool TryCalculateSurfaceStatistics(
            IEnumerable<double[]>? surfacePointCloud,
            out double ttv,
            out double minValue,
            out double maxValue)
        {
            List<double> surfaceValues = surfacePointCloud?
                .Where(point => point != null &&
                                point.Length > 2 &&
                                double.IsFinite(point[2]))
                .Select(point => point[2])
                .ToList() ?? new List<double>();

            if (surfaceValues.Count == 0)
            {
                ttv = double.NaN;
                minValue = double.NaN;
                maxValue = double.NaN;
                return false;
            }

            minValue = surfaceValues.Min();
            maxValue = surfaceValues.Max();
            ttv = maxValue - minValue;
            return true;
        }

        private List<double[]> BuildDataSourcePointCloud(
            IEnumerable<PreprocessDatasetModel>? preDatas,
            DataAnalysisDataSourceOption source)
        {
            if (preDatas == null || source == null)
            {
                return new List<double[]>();
            }

            return PreprocessDatasetModel.ToPointCloud(
                preDatas,
                data => GetDataAnalysisSourceValue(data, source));
        }

        private static double GetDataAnalysisSourceValue(
            PreprocessDatasetModel data,
            DataAnalysisDataSourceOption source)
        {
            if (data == null || source == null)
            {
                return double.NaN;
            }

            if (data.TryGetOriginalDataValue(source.OriginalDataValueName, out double originalDataValue))
            {
                return originalDataValue;
            }

            if (string.Equals(source.Name, UpSurfaceResultOption, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.OriginalDataValueName, PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName, StringComparison.OrdinalIgnoreCase))
            {
                return data.UpSurface;
            }

            if (string.Equals(source.Name, DownSurfaceResultOption, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.OriginalDataValueName, PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName, StringComparison.OrdinalIgnoreCase))
            {
                return data.DownSurface;
            }

            return double.NaN;
        }

        private static void ApplyDataSourceCompensation(
            IReadOnlyList<PreprocessDatasetModel> preDatas,
            DataAnalysisDataSourceOption source,
            IReadOnlyList<double[]> compensatedPoints)
        {
            if (preDatas == null || source == null || compensatedPoints == null || preDatas.Count != compensatedPoints.Count)
            {
                return;
            }

            for (int i = 0; i < preDatas.Count; i++)
            {
                if (compensatedPoints[i] == null ||
                    compensatedPoints[i].Length < 3 ||
                    !double.IsFinite(compensatedPoints[i][2]))
                {
                    continue;
                }

                double compensatedValue = compensatedPoints[i][2];
                string sourceName = GetDataSourceDisplayName(source);
                string normalizedOriginalDataName = PreprocessDatasetModel.NormalizeOriginalDataValueName(source.OriginalDataValueName);
                preDatas[i].OriginalDataValues[normalizedOriginalDataName] = compensatedValue;

                if (string.Equals(sourceName, UpSurfaceResultOption, StringComparison.OrdinalIgnoreCase))
                {
                    preDatas[i].UpSurface = compensatedValue;
                }
                else if (string.Equals(sourceName, DownSurfaceResultOption, StringComparison.OrdinalIgnoreCase))
                {
                    preDatas[i].DownSurface = compensatedValue;
                }
            }
        }

        private static void BuildPairedPointClouds(
            IEnumerable<PreprocessDatasetModel> preDatas,
            DataAnalysisDataSourceOption sourceA,
            DataAnalysisDataSourceOption sourceB,
            out List<double[]> pointsA,
            out List<double[]> pointsB)
        {
            pointsA = new List<double[]>();
            pointsB = new List<double[]>();

            if (preDatas == null)
            {
                return;
            }

            foreach (PreprocessDatasetModel data in preDatas)
            {
                if (data == null ||
                    !double.IsFinite(data.PosX) ||
                    !double.IsFinite(data.PosY))
                {
                    continue;
                }

                double valueA = GetDataAnalysisSourceValue(data, sourceA);
                double valueB = GetDataAnalysisSourceValue(data, sourceB);
                if (!double.IsFinite(valueA) || !double.IsFinite(valueB))
                {
                    continue;
                }

                pointsA.Add(new[] { data.PosX, data.PosY, valueA });
                pointsB.Add(new[] { data.PosX, data.PosY, valueB });
            }
        }

        private void AddDataAnalysisResult(
            DataAnalysisAlgorithmKind algorithm,
            string dataSourceName,
            double value,
            double minValue,
            double maxValue,
            int pointCount,
            string status)
        {
            DataAnalysisResult result = new DataAnalysisResult
            {
                AlgorithmName = DataAnalysisAlgorithmOption.GetDisplayName(algorithm),
                DataSourceName = dataSourceName,
                Value = value,
                MinValue = minValue,
                MaxValue = maxValue,
                PointCount = pointCount,
                Status = status
            };

            ExecuteOnUiThreadSync(() =>
            {
                DataAnalysisResults.Add(result);
                RaisePropertyChanged(nameof(DataAnalysisResultCount));
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            });
        }

        private void AppendDataAnalysisResultsToLastCsv()
        {
            string csvPath = LastPreDatasCsvPath;
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                return;
            }

            if (!File.Exists(csvPath))
            {
                Logs.LogWarning($"数据分析结果未追加保存，CSV文件不存在：{csvPath}");
                return;
            }

            List<DataAnalysisResult> results = SnapshotDataAnalysisResults();
            if (results.Count == 0)
            {
                Logs.LogInfo("当前没有数据分析结果，本次未追加保存到 CSV。");
                return;
            }

            try
            {
                using StreamWriter writer = new StreamWriter(csvPath, append: true, encoding: Encoding.UTF8);
                for (int i = 0; i < 3; i++)
                {
                    writer.WriteLine();
                }

                writer.WriteLine(EscapeCsvValue("数据分析结果"));
                writer.WriteLine(string.Join(",", new[]
                {
                    "算法",
                    "数据源",
                    "结果值",
                    "最小值",
                    "最大值",
                    "点数",
                    "状态"
                }.Select(EscapeCsvValue)));

                foreach (DataAnalysisResult result in results)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        result.AlgorithmName,
                        result.DataSourceName,
                        FormatAnalysisResultValue(result.Value),
                        FormatAnalysisResultValue(result.MinValue),
                        FormatAnalysisResultValue(result.MaxValue),
                        result.PointCount.ToString(CultureInfo.InvariantCulture),
                        result.Status
                    }.Select(EscapeCsvValue)));
                }

                Logs.LogInfo($"数据分析结果已追加保存到 CSV：{csvPath}");
            }
            catch (Exception ex)
            {
                Logs.LogError($"数据分析结果追加保存到 CSV 失败：{ex.Message}{Environment.NewLine}{ex}");
            }
        }

        private List<DataAnalysisResult> SnapshotDataAnalysisResults()
        {
            List<DataAnalysisResult> results = new List<DataAnalysisResult>();
            ExecuteOnUiThreadSync(() =>
            {
                results = DataAnalysisResults?
                    .Where(result => result != null)
                    .Select(result => new DataAnalysisResult
                    {
                        AlgorithmName = result.AlgorithmName,
                        DataSourceName = result.DataSourceName,
                        Value = result.Value,
                        MinValue = result.MinValue,
                        MaxValue = result.MaxValue,
                        PointCount = result.PointCount,
                        Status = result.Status
                    })
                    .ToList() ?? new List<DataAnalysisResult>();
            });

            return results;
        }

        private static string FormatAnalysisResultValue(double value)
        {
            return double.IsFinite(value)
                ? value.ToString("F8", CultureInfo.InvariantCulture)
                : "NaN";
        }

        private void ApplyDisplayPointCloud(IEnumerable<DisplayPointCloudCandidate> candidates)
        {
            DisplayPointCloudCandidate? selectedCandidate = candidates?
                .Where(candidate => candidate.PointCloud != null && candidate.PointCloud.Count > 0)
                .OrderBy(candidate => candidate.Priority)
                .FirstOrDefault();

            if (selectedCandidate == null)
            {
                ResultPointCloud = new List<double[]>();
                ResultPointCloudTitle = "暂无点云";
                return;
            }

            ResultPointCloud = selectedCandidate.PointCloud;
            ResultPointCloudTitle = selectedCandidate.Title;
        }

        private static int GetDataSourceIndex(
            IReadOnlyList<DataAnalysisDataSourceOption> sources,
            DataAnalysisDataSourceOption source)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (ReferenceEquals(sources[i], source))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetDataSourceDisplayName(DataAnalysisDataSourceOption source)
        {
            return string.IsNullOrWhiteSpace(source?.Name)
                ? "DataSource"
                : source.Name.Trim();
        }

        private static string FormatResultValue(double value)
        {
            return double.IsFinite(value)
                ? value.ToString("F8", CultureInfo.InvariantCulture)
                : "NaN";
        }

        private sealed class DisplayPointCloudCandidate
        {
            public DisplayPointCloudCandidate(int priority, string title, List<double[]> pointCloud)
            {
                Priority = priority;
                Title = title;
                PointCloud = pointCloud ?? new List<double[]>();
            }

            public int Priority { get; }

            public string Title { get; }

            public List<double[]> PointCloud { get; }
        }

        private void SaveRunAlgoPointClouds(IReadOnlyDictionary<string, List<double[]>> pointClouds)
        {
            if (pointClouds == null || pointClouds.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(PointCloudOutputDirectory))
            {
                Logs.LogInfo("未设置 RunALGO 点云导出目录，本次跳过点云导出。");
                return;
            }

            try
            {
                Directory.CreateDirectory(PointCloudOutputDirectory);

                int exportedCount = 0;
                foreach (var pointCloud in pointClouds)
                {
                    if (pointCloud.Value == null || pointCloud.Value.Count == 0)
                    {
                        continue;
                    }

                    string fileName = SanitizePointCloudFileName(pointCloud.Key);
                    string filePath = Path.Combine(PointCloudOutputDirectory, $"{fileName}.ply");
                    WritePlyAscii(filePath, pointCloud.Value);
                    exportedCount++;
                }

                if (exportedCount > 0)
                {
                    Logs.LogInfo($"RunALGO 点云已导出到目录：{PointCloudOutputDirectory}，共 {exportedCount} 个文件。");
                }
                else
                {
                    Logs.LogWarning("RunALGO 没有可导出的点云数据。");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"RunALGO 点云导出失败：{ex.Message}{Environment.NewLine}{ex}");
            }
        }

        private static void WritePlyAscii(string path, List<double[]> points)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var sw = new StreamWriter(path, false, Encoding.ASCII);
            sw.WriteLine("ply");
            sw.WriteLine("format ascii 1.0");
            sw.WriteLine($"element vertex {points.Count}");
            sw.WriteLine("property float x");
            sw.WriteLine("property float y");
            sw.WriteLine("property float z");
            sw.WriteLine("end_header");

            foreach (var p in points)
            {
                sw.WriteLine(
                    $"{p[0].ToString("G17", Invariant)} {p[1].ToString("G17", Invariant)} {p[2].ToString("G17", Invariant)}");
            }
        }
    }
}

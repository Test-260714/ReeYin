using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Logger;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Custom.WaferFlatnessMeasure.Models
{
    internal static class SensorDataStorageService
    {
        public static string ResolvePreDatasCsvDirectory(string? configuredDirectory)
        {
            return configuredDirectory?.Trim() ?? string.Empty;
        }

        public static string GetPointRawCsvPath(string? configuredDirectory, DateTime? createdAt = null)
        {
            string csvDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
            return string.IsNullOrWhiteSpace(csvDirectory)
                ? string.Empty
                : Path.Combine(csvDirectory, $"{(createdAt ?? DateTime.Now):yyMMddHHmmss}_Raw.csv");
        }

        public static string SavePreDatasToCsvIfNeeded(
            bool isSavePreDatasToCsv,
            List<PreprocessDatasetModel>? preDatas,
            string? configuredDirectory,
            double channelCalibrationC)
        {
            if (!isSavePreDatasToCsv)
            {
                return string.Empty;
            }

            if (preDatas == null || preDatas.Count == 0)
            {
                Logs.LogWarning("PreDatas 为空，本次未导出 CSV。");
                return string.Empty;
            }

            string csvDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
            if (string.IsNullOrWhiteSpace(csvDirectory))
            {
                Logs.LogWarning("PreDatas CSV directory is not configured; export skipped.");
                return string.Empty;
            }

            string csvPath = Path.Combine(csvDirectory, $"{DateTime.Now:yyMMddHHmmss}.csv");
            PreprocessDatasetModel.UpdateThicknesses(preDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(preDatas, csvPath);
            Logs.LogInfo($"预处理数据已导出到 CSV：{csvPath}");
            return csvPath;
        }

        public static string SaveFilteredPreDatasToCsvIfNeeded(
            bool isSavePreDatasToCsv,
            List<PreprocessDatasetModel>? preDatas,
            string? configuredDirectory,
            double channelCalibrationC)
        {
            if (!isSavePreDatasToCsv || preDatas == null || preDatas.Count == 0)
            {
                return string.Empty;
            }

            string csvDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
            if (string.IsNullOrWhiteSpace(csvDirectory))
            {
                Logs.LogWarning("PreDatas CSV directory is not configured; filtered export skipped.");
                return string.Empty;
            }

            string csvPath = Path.Combine(csvDirectory, $"{DateTime.Now:yyMMddHHmmss}_Filtered.csv");
            PreprocessDatasetModel.UpdateThicknesses(preDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(preDatas, csvPath);
            Logs.LogInfo($"Filtered PreDatas were exported to CSV: {csvPath}");
            return csvPath;
        }

        public static string CreateLineSegmentCsvSessionDirectory(string? configuredDirectory)
        {
            return CreateLineSegmentCsvSessionDirectory(configuredDirectory, DateTime.Now);
        }

        public static string CreateLineSegmentCsvSessionDirectory(string? configuredDirectory, DateTime createdAt)
        {
            string baseDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                Logs.LogWarning("PreDatas CSV directory is not configured; line segment CSV session skipped.");
                return string.Empty;
            }

            Directory.CreateDirectory(baseDirectory);

            string directoryName = $"LineSegments_{createdAt:yyyyMMddHHmmss}";
            string sessionDirectory = Path.Combine(baseDirectory, directoryName);
            int suffix = 1;
            while (Directory.Exists(sessionDirectory))
            {
                sessionDirectory = Path.Combine(baseDirectory, $"{directoryName}_{suffix:D3}");
                suffix++;
            }

            Directory.CreateDirectory(sessionDirectory);
            return sessionDirectory;
        }

        public static string GetLineSegmentRawCsvPath(string sessionDirectory, int segmentIndex)
        {
            return GetLineSegmentCsvPath(sessionDirectory, segmentIndex, "Raw");
        }

        public static string GetLineSegmentPreDatasCsvPath(string sessionDirectory, int segmentIndex)
        {
            return GetLineSegmentCsvPath(sessionDirectory, segmentIndex, "PreDatas");
        }

        public static string GetLineSegmentFilteredPreDatasCsvPath(string sessionDirectory, int segmentIndex)
        {
            return GetLineSegmentCsvPath(sessionDirectory, segmentIndex, "PreDatas_Filtered");
        }

        public static string SaveLineSegmentPreDatasCsv(
            string sessionDirectory,
            int segmentIndex,
            List<PreprocessDatasetModel>? preDatas,
            double channelCalibrationC)
        {
            if (preDatas == null || preDatas.Count == 0)
            {
                Logs.LogWarning("Line segment PreDatas is empty; CSV export skipped.");
                return string.Empty;
            }

            string csvPath = GetLineSegmentPreDatasCsvPath(sessionDirectory, segmentIndex);
            PreprocessDatasetModel.UpdateThicknesses(preDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(preDatas, csvPath);
            Logs.LogInfo($"Line segment PreDatas were exported to CSV: {csvPath}");
            return csvPath;
        }

        public static string SaveFilteredLineSegmentPreDatasCsv(
            string sessionDirectory,
            int segmentIndex,
            List<PreprocessDatasetModel>? preDatas,
            double channelCalibrationC)
        {
            if (preDatas == null || preDatas.Count == 0)
            {
                return string.Empty;
            }

            string csvPath = GetLineSegmentFilteredPreDatasCsvPath(sessionDirectory, segmentIndex);
            PreprocessDatasetModel.UpdateThicknesses(preDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(preDatas, csvPath);
            Logs.LogInfo($"Filtered line segment PreDatas were exported to CSV: {csvPath}");
            return csvPath;
        }

        public static string SaveLineSegmentSummaryCsv(
            string sessionDirectory,
            IEnumerable<PreprocessDatasetModel>? preDatas,
            double channelCalibrationC)
        {
            List<PreprocessDatasetModel> exportDatas = PreprocessDatasetModel.Clone(preDatas);
            if (exportDatas.Count == 0)
            {
                Logs.LogWarning("Line segment summary PreDatas is empty; CSV export skipped.");
                return string.Empty;
            }

            string csvPath = Path.Combine(ResolveLineSegmentSessionDirectory(sessionDirectory), "LineSegments_All_PreDatas.csv");
            PreprocessDatasetModel.UpdateThicknesses(exportDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(exportDatas, csvPath);
            Logs.LogInfo($"Line segment summary PreDatas were exported to CSV: {csvPath}");
            return csvPath;
        }

        public static string SaveFilteredLineSegmentSummaryCsv(
            string sessionDirectory,
            IEnumerable<PreprocessDatasetModel>? preDatas,
            double channelCalibrationC)
        {
            List<PreprocessDatasetModel> exportDatas = PreprocessDatasetModel.Clone(preDatas);
            if (exportDatas.Count == 0)
            {
                Logs.LogWarning("Filtered line segment summary PreDatas is empty; CSV export skipped.");
                return string.Empty;
            }

            string csvPath = Path.Combine(
                ResolveLineSegmentSessionDirectory(sessionDirectory),
                "LineSegments_All_PreDatas_Filtered.csv");
            PreprocessDatasetModel.UpdateThicknesses(exportDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(exportDatas, csvPath);
            Logs.LogInfo($"Filtered line segment summary PreDatas were exported to CSV: {csvPath}");
            return csvPath;
        }

        public static string SaveLineSegmentSummaryPly(
            string sessionDirectory,
            IEnumerable<PreprocessDatasetModel>? preDatas,
            double channelCalibrationC)
        {
            List<PreprocessDatasetModel> exportDatas = PreprocessDatasetModel.Clone(preDatas);
            PreprocessDatasetModel.UpdateThicknesses(exportDatas, channelCalibrationC);

            List<double[]> pointCloud = BuildThicknessPointCloud(exportDatas);
            if (pointCloud.Count == 0)
            {
                Logs.LogWarning("Line segment summary thickness point cloud is empty; PLY export skipped.");
                return string.Empty;
            }

            string plyPath = Path.Combine(ResolveLineSegmentSessionDirectory(sessionDirectory), "LineSegments_All_PreDatas.ply");
            WritePlyAscii(plyPath, pointCloud);
            Logs.LogInfo($"Line segment summary thickness point cloud was exported to PLY: {plyPath}");
            return plyPath;
        }

        public static string SaveCalibrationWaferDataCsv(
            IEnumerable<MeasureData>? calibrationDatas,
            string? configuredDirectory)
        {
            List<MeasureData> exportDatas = calibrationDatas?
                .Where(data => data != null)
                .ToList() ?? new List<MeasureData>();

            if (exportDatas.Count == 0)
            {
                Logs.LogWarning("标准片测量数据为空，本次未导出标准片 CSV。");
                return string.Empty;
            }

            try
            {
                string csvDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
                if (string.IsNullOrWhiteSpace(csvDirectory))
                {
                    Logs.LogWarning("PreDatas CSV directory is not configured; calibration wafer CSV export skipped.");
                    return string.Empty;
                }

                string csvPath = Path.Combine(
                    csvDirectory,
                    $"{DateTime.Now:yyMMddHHmmss}_CalibrationWafer.csv");

                ExportOriginalDatasToCsv(exportDatas, csvPath);
                Logs.LogInfo($"标准片测量数据已导出到 CSV：{csvPath}");
                return csvPath;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return string.Empty;
            }
        }

        public static void ExportOriginalDatasToCsv(List<MeasureData> measureDataList, string filePath)
        {
            if (measureDataList == null)
            {
                throw new ArgumentNullException(nameof(measureDataList), "输入的 MeasureData 列表不能为 null。");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空或仅包含空白字符。", nameof(filePath));
            }

            List<string> originalDataColumns = GetOriginalDataCsvColumns(measureDataList);
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
            writer.WriteLine(string.Join(",", new[] { "X", "Y" }
                .Concat(originalDataColumns)
                .Select(EscapeCsvValue)));

            foreach (var data in measureDataList)
            {
                Dictionary<string, object> originalDataValues = FlattenOriginalDatas(data);
                IEnumerable<string> rowValues = new[]
                {
                    FormatCsvNumber(data.X),
                    FormatCsvNumber(data.Y)
                }.Concat(originalDataColumns.Select(column =>
                    originalDataValues.TryGetValue(column, out object? value)
                        ? FormatCsvValue(value)
                        : string.Empty));

                writer.WriteLine(string.Join(",", rowValues.Select(EscapeCsvValue)));
            }
        }

        private static string GetLineSegmentCsvPath(string sessionDirectory, int segmentIndex, string dataKind)
        {
            if (segmentIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentIndex), "Segment index must be greater than zero.");
            }

            return Path.Combine(
                ResolveLineSegmentSessionDirectory(sessionDirectory),
                $"Line_{segmentIndex:D3}_{dataKind}.csv");
        }

        private static string ResolveLineSegmentSessionDirectory(string sessionDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionDirectory);
            Directory.CreateDirectory(sessionDirectory);
            return sessionDirectory;
        }

        private static List<double[]> BuildThicknessPointCloud(IEnumerable<PreprocessDatasetModel>? preDatas)
        {
            return preDatas?
                .Where(data =>
                    data != null &&
                    double.IsFinite(data.PosX) &&
                    double.IsFinite(data.PosY) &&
                    double.IsFinite(data.Thickness))
                .Select(data => new[] { data.PosX, data.PosY, data.Thickness })
                .ToList() ?? new List<double[]>();
        }

        private static void WritePlyAscii(string path, IReadOnlyList<double[]> points)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(path, append: false, encoding: Encoding.ASCII);
            writer.WriteLine("ply");
            writer.WriteLine("format ascii 1.0");
            writer.WriteLine($"element vertex {points.Count}");
            writer.WriteLine("property float x");
            writer.WriteLine("property float y");
            writer.WriteLine("property float z");
            writer.WriteLine("end_header");

            foreach (double[] point in points)
            {
                writer.WriteLine(
                    $"{FormatPlyNumber(point[0])} {FormatPlyNumber(point[1])} {FormatPlyNumber(point[2])}");
            }
        }

        private static string FormatPlyNumber(double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        private static List<string> GetOriginalDataCsvColumns(IEnumerable<MeasureData> measureDataList)
        {
            return measureDataList
                .SelectMany(data => FlattenOriginalDatas(data).Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetOriginalDataCsvColumnSortKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(column => column, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Dictionary<string, object> FlattenOriginalDatas(MeasureData? measureData)
        {
            Dictionary<string, object> flattenedDatas = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (measureData?.OriginalDatas == null || measureData.OriginalDatas.Count == 0)
            {
                return flattenedDatas;
            }

            foreach (KeyValuePair<string, Dictionary<string, object>> channelPair in measureData.OriginalDatas)
            {
                if (string.IsNullOrWhiteSpace(channelPair.Key) || channelPair.Value == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> typePair in channelPair.Value)
                {
                    if (string.IsNullOrWhiteSpace(typePair.Key))
                    {
                        continue;
                    }

                    flattenedDatas[$"{channelPair.Key}.{typePair.Key}"] = typePair.Value;
                }
            }

            return flattenedDatas;
        }

        private static string GetOriginalDataCsvColumnSortKey(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return string.Empty;
            }

            string[] tokens = columnName.Split('.', 2);
            string channelName = tokens[0];
            string typeName = tokens.Length > 1 ? tokens[1] : string.Empty;
            if (string.Equals(channelName, "Controller", StringComparison.OrdinalIgnoreCase))
            {
                return $"0000.{typeName}";
            }

            if (channelName.StartsWith("Channel", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(channelName.Substring("Channel".Length), out int channelIndex))
            {
                return $"{channelIndex:D4}.{typeName}";
            }

            return columnName;
        }

        private static string FormatCsvValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return PreprocessDatasetModel.TryConvertOriginalDataValue(value, out double doubleValue)
                ? FormatCsvNumber(doubleValue)
                : value.ToString() ?? string.Empty;
        }

        private static string FormatCsvNumber(double value)
        {
            return double.IsFinite(value)
                ? value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty;
        }

        public static string EscapeCsvValue(string? value)
        {
            value ??= string.Empty;
            return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}

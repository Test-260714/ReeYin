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
        private const string DefaultPreDatasCsvDirectory = "C:\\Users\\admin\\Desktop\\ReceiveDatas";
        private const float PointCloudImageInvalidValue = 8888880f;
        private const int DefaultLineSegmentTiffResolution = 600;
        private const int MinLineSegmentTiffResolution = 2;
        private const int MaxLineSegmentTiffResolution = 5480;

        public static string ResolvePreDatasCsvDirectory(string? configuredDirectory)
        {
            return string.IsNullOrWhiteSpace(configuredDirectory)
                ? DefaultPreDatasCsvDirectory
                : configuredDirectory;
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
            string csvPath = Path.Combine(csvDirectory, $"{DateTime.Now:yyMMddHHmmss}.csv");
            PreprocessDatasetModel.UpdateThicknesses(preDatas, channelCalibrationC);
            PreprocessDatasetModel.ExportToCsv(preDatas, csvPath);
            Logs.LogInfo($"预处理数据已导出到 CSV：{csvPath}");
            return csvPath;
        }

        public static string CreateLineSegmentCsvSessionDirectory(string? configuredDirectory)
        {
            return CreateLineSegmentCsvSessionDirectory(configuredDirectory, DateTime.Now);
        }

        public static string CreateLineSegmentCsvSessionDirectory(string? configuredDirectory, DateTime createdAt)
        {
            string baseDirectory = ResolvePreDatasCsvDirectory(configuredDirectory);
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
            SaveThicknessPointCloudTiff(
                Path.ChangeExtension(plyPath, ".tif"),
                pointCloud,
                DefaultLineSegmentTiffResolution);
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
                string csvPath = Path.Combine(
                    ResolvePreDatasCsvDirectory(configuredDirectory),
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

        private static string SaveThicknessPointCloudTiff(
            string path,
            IReadOnlyCollection<double[]> pointCloud,
            int maxResolution)
        {
            if (!TryRasterizePointCloudToHeightImage(
                    pointCloud,
                    maxResolution,
                    out float[] imageData,
                    out int width,
                    out int height))
            {
                Logs.LogWarning("Line segment summary thickness point cloud could not be converted to TIFF.");
                return string.Empty;
            }

            WriteFloatTiff(path, imageData, width, height);
            Logs.LogInfo($"Line segment summary thickness image was exported to TIFF: {path}");
            return path;
        }

        private static bool TryRasterizePointCloudToHeightImage(
            IReadOnlyCollection<double[]> pointCloud,
            int maxResolution,
            out float[] imageData,
            out int width,
            out int height)
        {
            imageData = Array.Empty<float>();
            width = 0;
            height = 0;

            List<double[]> validPoints = pointCloud
                .Where(point =>
                    point != null &&
                    point.Length > 2 &&
                    double.IsFinite(point[0]) &&
                    double.IsFinite(point[1]) &&
                    double.IsFinite(point[2]))
                .ToList();

            if (validPoints.Count == 0)
            {
                return false;
            }

            double minX = validPoints.Min(point => point[0]);
            double maxX = validPoints.Max(point => point[0]);
            double minY = validPoints.Min(point => point[1]);
            double maxY = validPoints.Max(point => point[1]);
            double rangeX = maxX - minX;
            double rangeY = maxY - minY;

            if (rangeX <= 0 && rangeY <= 0)
            {
                width = 2;
                height = 2;
                imageData = Enumerable.Repeat((float)validPoints[0][2], width * height).ToArray();
                return true;
            }

            rangeX = rangeX <= 0 ? rangeY : rangeX;
            rangeY = rangeY <= 0 ? rangeX : rangeY;
            maxX = minX + rangeX;
            maxY = minY + rangeY;

            int resolution = Math.Clamp(maxResolution, MinLineSegmentTiffResolution, MaxLineSegmentTiffResolution);
            if (rangeX >= rangeY)
            {
                width = resolution;
                height = Math.Max(2, (int)Math.Round(resolution * rangeY / rangeX));
            }
            else
            {
                height = resolution;
                width = Math.Max(2, (int)Math.Round(resolution * rangeX / rangeY));
            }

            width = Math.Clamp(width, MinLineSegmentTiffResolution, MaxLineSegmentTiffResolution);
            height = Math.Clamp(height, MinLineSegmentTiffResolution, MaxLineSegmentTiffResolution);

            imageData = new float[width * height];
            Array.Fill(imageData, PointCloudImageInvalidValue);

            double[] valueSums = new double[imageData.Length];
            int[] valueCounts = new int[imageData.Length];
            foreach (double[] point in validPoints)
            {
                int col = width <= 1 ? 0 : (int)Math.Round((point[0] - minX) / rangeX * (width - 1));
                int row = height <= 1 ? 0 : (int)Math.Round((maxY - point[1]) / rangeY * (height - 1));
                col = Math.Clamp(col, 0, width - 1);
                row = Math.Clamp(row, 0, height - 1);

                int index = row * width + col;
                valueSums[index] += point[2];
                valueCounts[index]++;
            }

            int directFilledCount = 0;
            for (int index = 0; index < imageData.Length; index++)
            {
                if (valueCounts[index] <= 0)
                {
                    continue;
                }

                imageData[index] = (float)(valueSums[index] / valueCounts[index]);
                directFilledCount++;
            }

            if (directFilledCount == 0)
            {
                return false;
            }

            FillHeightImageHolesWithNearestValue(imageData, width, height);
            return true;
        }

        private static void FillHeightImageHolesWithNearestValue(float[] imageData, int width, int height)
        {
            int[] queue = new int[imageData.Length];
            bool[] visited = new bool[imageData.Length];
            int head = 0;
            int tail = 0;

            for (int index = 0; index < imageData.Length; index++)
            {
                if (!IsValidPointCloudImageValue(imageData[index]))
                {
                    continue;
                }

                visited[index] = true;
                queue[tail++] = index;
            }

            int[] rowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] colOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

            while (head < tail)
            {
                int current = queue[head++];
                int row = current / width;
                int col = current - row * width;

                for (int i = 0; i < rowOffsets.Length; i++)
                {
                    int nextRow = row + rowOffsets[i];
                    int nextCol = col + colOffsets[i];
                    if (nextRow < 0 || nextRow >= height || nextCol < 0 || nextCol >= width)
                    {
                        continue;
                    }

                    int nextIndex = nextRow * width + nextCol;
                    if (visited[nextIndex])
                    {
                        continue;
                    }

                    imageData[nextIndex] = imageData[current];
                    visited[nextIndex] = true;
                    queue[tail++] = nextIndex;
                }
            }
        }

        private static bool IsValidPointCloudImageValue(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   Math.Abs(value - PointCloudImageInvalidValue) > 1e-3f;
        }

        private static void WriteFloatTiff(string path, IReadOnlyList<float> imageData, int width, int height)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            const ushort entryCount = 10;
            const uint ifdOffset = 8;
            uint pixelDataOffset = ifdOffset + 2u + entryCount * 12u + 4u;
            uint pixelByteCount = checked((uint)(imageData.Count * sizeof(float)));

            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII);

            writer.Write((byte)'I');
            writer.Write((byte)'I');
            writer.Write((ushort)42);
            writer.Write(ifdOffset);

            writer.Write(entryCount);
            WriteTiffShortEntry(writer, 256, checked((uint)width));
            WriteTiffShortEntry(writer, 257, checked((uint)height));
            WriteTiffShortEntry(writer, 258, 32);
            WriteTiffShortEntry(writer, 259, 1);
            WriteTiffShortEntry(writer, 262, 1);
            WriteTiffLongEntry(writer, 273, pixelDataOffset);
            WriteTiffShortEntry(writer, 277, 1);
            WriteTiffLongEntry(writer, 278, checked((uint)height));
            WriteTiffLongEntry(writer, 279, pixelByteCount);
            WriteTiffShortEntry(writer, 339, 3);
            writer.Write(0u);

            foreach (float value in imageData)
            {
                writer.Write(value);
            }
        }

        private static void WriteTiffShortEntry(BinaryWriter writer, ushort tag, uint value)
        {
            writer.Write(tag);
            writer.Write((ushort)3);
            writer.Write(1u);
            writer.Write((ushort)value);
            writer.Write((ushort)0);
        }

        private static void WriteTiffLongEntry(BinaryWriter writer, ushort tag, uint value)
        {
            writer.Write(tag);
            writer.Write((ushort)4);
            writer.Write(1u);
            writer.Write(value);
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

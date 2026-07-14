using Custom.WaferFlatnessMeasure;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Custom.WaferFlatnessMeasure.Models
{
    public sealed record LineSpectrumTiffExportPlanItem(
        int BatchIndex,
        IReadOnlyList<float[]> HeightRows,
        IReadOnlyList<float[]> GrayRows,
        double YOffset,
        double XOffset,
        string BatchDirectory,
        string DepthFilePath,
        string ImageFilePath);

    public static class LineSpectrumTiffExportService
    {
        public const string DefaultFileNamePrefix = "2.9_5.0_0.1_-50000.0_50000.0";

        public static IReadOnlyList<LineSpectrumTiffExportPlanItem> BuildExportPlan(
            string outputDirectory,
            LineSpectrumMeasureRows rows,
            IReadOnlyDictionary<int, LineSegmentStartPositionInfo>? linePositions,
            int startBatchIndex)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            List<float[]> heightRows = rows.HeightRows ?? new List<float[]>();
            List<float[]> grayRows = rows.GrayRows ?? new List<float[]>();
            if (heightRows.Count == 0 && grayRows.Count == 0)
            {
                return Array.Empty<LineSpectrumTiffExportPlanItem>();
            }

            int batchIndex = Math.Max(0, startBatchIndex);
            int segmentIndex = batchIndex + 1;
            var (yOffset, xOffset) = CalculateCoordinateOffset(segmentIndex, linePositions);
            string fileName = BuildFileName(yOffset, xOffset);
            string batchDirectory = Path.Combine(outputDirectory, batchIndex.ToString(CultureInfo.InvariantCulture));
            string depthFilePath = Path.Combine(batchDirectory, "depth", fileName);
            string imageFilePath = Path.Combine(batchDirectory, "image", fileName);

            return new[]
            {
                new LineSpectrumTiffExportPlanItem(
                    batchIndex,
                    heightRows,
                    grayRows,
                    yOffset,
                    xOffset,
                    batchDirectory,
                    depthFilePath,
                    imageFilePath)
            };
        }

        public static (double YOffset, double XOffset) CalculateCoordinateOffset(
            int segmentIndex,
            IReadOnlyDictionary<int, LineSegmentStartPositionInfo>? linePositions)
        {
            if (segmentIndex <= 1 || linePositions == null)
            {
                return (0d, 0d);
            }

            if (!linePositions.TryGetValue(segmentIndex, out LineSegmentStartPositionInfo? current) ||
                current == null ||
                !current.HasEndPosition ||
                !linePositions.TryGetValue(segmentIndex - 1, out LineSegmentStartPositionInfo? previous) ||
                previous == null)
            {
                return (0d, 0d);
            }

            return (current.EndY - previous.StartY, current.EndX - previous.StartX);
        }

        public static string BuildFileName(double yOffset, double xOffset)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{DefaultFileNamePrefix}_{FormatFileNumber(yOffset)}_{FormatFileNumber(xOffset)}.tif");
        }

        public static int Export(IReadOnlyList<LineSpectrumTiffExportPlanItem> items)
        {
            int exportedCount = 0;

            foreach (LineSpectrumTiffExportPlanItem item in items)
            {
                if (WriteRealTiff(item.DepthFilePath, item.HeightRows))
                {
                    exportedCount++;
                }

                if (WriteByteTiff(item.ImageFilePath, item.GrayRows))
                {
                    exportedCount++;
                }
            }

            return exportedCount;
        }

        private static string FormatFileNumber(double value)
        {
            if (!double.IsFinite(value) || Math.Abs(value) < 1e-9)
            {
                return "0";
            }

            return value.ToString("0.###############", CultureInfo.InvariantCulture);
        }

        private static bool WriteRealTiff(string path, IReadOnlyList<float[]> rows)
        {
            if (!TryBuildRealImageData(rows, out float[] imageData, out int width, out int height))
            {
                return false;
            }

            WriteImage(path, "real", imageData, width, height);
            return true;
        }

        private static bool WriteByteTiff(string path, IReadOnlyList<float[]> rows)
        {
            if (!TryBuildByteImageData(rows, out byte[] imageData, out int width, out int height))
            {
                return false;
            }

            WriteImage(path, "byte", imageData, width, height);
            return true;
        }

        private static bool TryBuildRealImageData(
            IReadOnlyList<float[]> rows,
            out float[] imageData,
            out int width,
            out int height)
        {
            width = GetFirstValidWidth(rows);
            if (width <= 0)
            {
                imageData = Array.Empty<float>();
                height = 0;
                return false;
            }

            List<float[]> validRows = GetValidRows(rows);
            height = validRows.Count;
            imageData = new float[width * height];

            for (int rowIndex = 0; rowIndex < validRows.Count; rowIndex++)
            {
                float[] row = validRows[rowIndex];
                Array.Copy(row, 0, imageData, rowIndex * width, Math.Min(width, row.Length));
            }

            return height > 0;
        }

        private static bool TryBuildByteImageData(
            IReadOnlyList<float[]> rows,
            out byte[] imageData,
            out int width,
            out int height)
        {
            width = GetFirstValidWidth(rows);
            if (width <= 0)
            {
                imageData = Array.Empty<byte>();
                height = 0;
                return false;
            }

            List<float[]> validRows = GetValidRows(rows);
            height = validRows.Count;
            imageData = new byte[width * height];

            for (int rowIndex = 0; rowIndex < validRows.Count; rowIndex++)
            {
                float[] row = validRows[rowIndex];
                int offset = rowIndex * width;
                int count = Math.Min(width, row.Length);
                for (int column = 0; column < count; column++)
                {
                    imageData[offset + column] = ToByte(row[column]);
                }
            }

            return height > 0;
        }

        private static int GetFirstValidWidth(IReadOnlyList<float[]> rows)
        {
            foreach (float[] row in rows)
            {
                if (row != null && row.Length > 0)
                {
                    return row.Length;
                }
            }

            return 0;
        }

        private static List<float[]> GetValidRows(IReadOnlyList<float[]> rows)
        {
            var validRows = new List<float[]>();
            foreach (float[] row in rows)
            {
                if (row != null && row.Length > 0)
                {
                    validRows.Add(row);
                }
            }

            return validRows;
        }

        private static byte ToByte(float value)
        {
            if (!float.IsFinite(value))
            {
                return 0;
            }

            return (byte)Math.Clamp(value, 0f, 255f);
        }

        private static void WriteImage<T>(string path, string imageType, T[] imageData, int width, int height)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GCHandle handle = default;
            HObject image = null!;

            try
            {
                handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                HOperatorSet.GenImage1(out image, imageType, width, height, handle.AddrOfPinnedObject());
                HOperatorSet.WriteImage(image, "tiff", 0, path);
            }
            finally
            {
                image?.Dispose();
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
    }
}

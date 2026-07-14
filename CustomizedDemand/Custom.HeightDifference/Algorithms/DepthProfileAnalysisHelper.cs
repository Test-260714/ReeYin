using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReeYin.Customized.Algo.Algorithms
{
    /// <summary>
    /// 保存深度图像素类型、尺寸和原始高度数组。
    /// </summary>
    internal sealed record DepthImageData(string PixelType, int Width, int Height, float[] RawValues);

    /// <summary>
    /// 保存画线剖面中的单个采样点位置、距离和高度值。
    /// </summary>
    internal sealed record DepthProfilePoint(
        int PixelX,
        int PixelY,
        double Distance,
        double ActualX,
        double ActualY,
        double HeightValue,
        bool IsValid);

    /// <summary>
    /// 保存高度曲线选中区间的均值、采样数和端点索引。
    /// </summary>
    internal sealed record DepthProfileSegmentStats(
        int StartIndex,
        int EndIndex,
        int TotalSamples,
        int ValidSamples,
        double MeanHeight,
        double StartDistance,
        double EndDistance);

    /// <summary>
    /// 保存矩形区域统计出的平均高度和有效采样数。
    /// </summary>
    internal sealed record DepthProfileRegionStats(
        int TotalSamples,
        int ValidSamples,
        double MeanHeight);

    internal static class DepthProfileAnalysisHelper
    {
        /// <summary>
        /// 从 HALCON 高度图读取像素类型、尺寸和深度数组。
        /// </summary>
        public static DepthImageData LoadDepthImage(HObject imageObject)
        {
            if (imageObject == null || !imageObject.IsInitialized())
            {
                throw new ArgumentException("输入图像不能为空，且必须是已初始化的 HObject。", nameof(imageObject));
            }

            using HImage image = new(imageObject);
            IntPtr pointer = image.GetImagePointer1(out string pixelType, out int width, out int height);
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("图像尺寸无效。");
            }

            return new DepthImageData(pixelType, width, height, CopyDepthValues(pointer, width, height, pixelType));
        }

        /// <summary>
        /// 沿用户绘制的直线按像素采样，生成高度曲线数据。
        /// </summary>
        public static IReadOnlyList<DepthProfilePoint> ExtractLineProfile(
            DepthImageData imageData,
            int startX,
            int startY,
            int endX,
            int endY,
            double spacingX,
            double spacingY,
            double intervalZ,
            double invalidGrayCenter,
            double invalidGrayTolerance)
        {
            if (imageData.Width <= 0 || imageData.Height <= 0 || imageData.RawValues.Length == 0)
            {
                return Array.Empty<DepthProfilePoint>();
            }

            if (spacingX <= 0 || spacingY <= 0 || intervalZ <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spacingX), "采样间距和高度转换系数必须大于 0。");
            }

            int x0 = Math.Clamp(startX, 0, imageData.Width - 1);
            int y0 = Math.Clamp(startY, 0, imageData.Height - 1);
            int x1 = Math.Clamp(endX, 0, imageData.Width - 1);
            int y1 = Math.Clamp(endY, 0, imageData.Height - 1);

            int dx = x1 - x0;
            int dy = y1 - y0;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            int lastX = int.MinValue;
            int lastY = int.MinValue;
            double distance = 0d;
            List<DepthProfilePoint> points = new(Math.Max(steps + 1, 1));

            for (int i = 0; i <= steps; i++)
            {
                double t = steps == 0 ? 0d : i / (double)steps;
                int x = Math.Clamp((int)Math.Round(x0 + dx * t), 0, imageData.Width - 1);
                int y = Math.Clamp((int)Math.Round(y0 + dy * t), 0, imageData.Height - 1);

                if (x == lastX && y == lastY)
                {
                    continue;
                }

                if (lastX != int.MinValue)
                {
                    double deltaX = (x - lastX) * spacingX;
                    double deltaY = (y - lastY) * spacingY;
                    distance += Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                }

                float rawValue = imageData.RawValues[y * imageData.Width + x];
                bool isValid = IsDepthValid(rawValue, invalidGrayCenter, invalidGrayTolerance);

                points.Add(new DepthProfilePoint(
                    x,
                    y,
                    distance,
                    x * spacingX,
                    y * spacingY,
                    isValid ? rawValue * intervalZ : double.NaN,
                    isValid));

                lastX = x;
                lastY = y;
            }

            return points;
        }

        /// <summary>
        /// 统计高度曲线指定索引范围内的有效高度均值。
        /// </summary>
        public static DepthProfileSegmentStats EvaluateSegment(
            IReadOnlyList<DepthProfilePoint> profilePoints,
            int startIndex,
            int endIndex,
            double trimRatio)
        {
            if (profilePoints == null || profilePoints.Count == 0)
            {
                throw new InvalidOperationException("当前没有可分析的曲线数据。");
            }

            int normalizedStart = Math.Clamp(Math.Min(startIndex, endIndex), 0, profilePoints.Count - 1);
            int normalizedEnd = Math.Clamp(Math.Max(startIndex, endIndex), 0, profilePoints.Count - 1);

            List<double> validValues = new();
            for (int i = normalizedStart; i <= normalizedEnd; i++)
            {
                if (profilePoints[i].IsValid)
                {
                    validValues.Add(profilePoints[i].HeightValue);
                }
            }

            double meanHeight = CalculateTrimmedMean(validValues, trimRatio);
            return new DepthProfileSegmentStats(
                normalizedStart,
                normalizedEnd,
                normalizedEnd - normalizedStart + 1,
                validValues.Count,
                meanHeight,
                profilePoints[normalizedStart].Distance,
                profilePoints[normalizedEnd].Distance);
        }

        /// <summary>
        /// 统计水平矩形区域内的有效高度均值。
        /// </summary>
        public static DepthProfileRegionStats EvaluateRectangle(
            DepthImageData imageData,
            int x1,
            int y1,
            int x2,
            int y2,
            double intervalZ,
            double invalidGrayCenter,
            double invalidGrayTolerance,
            double trimRatio)
        {
            if (imageData.Width <= 0 || imageData.Height <= 0 || imageData.RawValues.Length == 0)
            {
                return new DepthProfileRegionStats(0, 0, double.NaN);
            }

            int left = Math.Clamp(Math.Min(x1, x2), 0, imageData.Width - 1);
            int right = Math.Clamp(Math.Max(x1, x2), 0, imageData.Width - 1);
            int top = Math.Clamp(Math.Min(y1, y2), 0, imageData.Height - 1);
            int bottom = Math.Clamp(Math.Max(y1, y2), 0, imageData.Height - 1);

            List<double> validValues = new();
            int totalSamples = 0;
            for (int row = top; row <= bottom; row++)
            {
                int rowOffset = row * imageData.Width;
                for (int column = left; column <= right; column++)
                {
                    totalSamples++;
                    float rawValue = imageData.RawValues[rowOffset + column];
                    if (IsDepthValid(rawValue, invalidGrayCenter, invalidGrayTolerance))
                    {
                        validValues.Add(rawValue * intervalZ);
                    }
                }
            }

            return new DepthProfileRegionStats(
                totalSamples,
                validValues.Count,
                CalculateTrimmedMean(validValues, trimRatio));
        }

        /// <summary>
        /// 统计旋转矩形区域内的有效高度均值。
        /// </summary>
        public static DepthProfileRegionStats EvaluateRectangle2(
            DepthImageData imageData,
            double centerX,
            double centerY,
            double phi,
            double length1,
            double length2,
            double intervalZ,
            double invalidGrayCenter,
            double invalidGrayTolerance,
            double trimRatio)
        {
            if (imageData.Width <= 0 || imageData.Height <= 0 || imageData.RawValues.Length == 0)
            {
                return new DepthProfileRegionStats(0, 0, double.NaN);
            }

            HObject? region = null;
            try
            {
                HOperatorSet.GenRectangle2(out region, centerY, centerX, phi, length1, length2);
                HOperatorSet.GetRegionPoints(region, out HTuple rows, out HTuple columns);

                List<double> validValues = new();
                int totalSamples = 0;
                int count = Math.Min(rows.Length, columns.Length);
                for (int i = 0; i < count; i++)
                {
                    int row = rows[i].I;
                    int column = columns[i].I;
                    if (row < 0 || row >= imageData.Height || column < 0 || column >= imageData.Width)
                    {
                        continue;
                    }

                    totalSamples++;
                    float rawValue = imageData.RawValues[row * imageData.Width + column];
                    if (IsDepthValid(rawValue, invalidGrayCenter, invalidGrayTolerance))
                    {
                        validValues.Add(rawValue * intervalZ);
                    }
                }

                return new DepthProfileRegionStats(
                    totalSamples,
                    validValues.Count,
                    CalculateTrimmedMean(validValues, trimRatio));
            }
            finally
            {
                region?.Dispose();
            }
        }

        /// <summary>
        /// 判断高度值是否避开无效灰度中心及容差范围。
        /// </summary>
        private static bool IsDepthValid(float value, double invalidGrayCenter, double invalidGrayTolerance)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return false;
            }

            return Math.Abs(value - invalidGrayCenter) > invalidGrayTolerance;
        }

        /// <summary>
        /// 对有效高度值排序并裁剪两端异常值后计算均值。
        /// </summary>
        private static double CalculateTrimmedMean(List<double> values, double trimRatio)
        {
            if (values.Count == 0)
            {
                return double.NaN;
            }

            values.Sort();
            int trimCount = (int)Math.Floor(values.Count * Math.Clamp(trimRatio, 0d, 0.49d));
            int start = trimCount;
            int end = values.Count - trimCount - 1;
            if (end < start)
            {
                start = 0;
                end = values.Count - 1;
            }

            double sum = 0d;
            int count = 0;
            for (int i = start; i <= end; i++)
            {
                sum += values[i];
                count++;
            }

            return count > 0 ? sum / count : double.NaN;
        }

        /// <summary>
        /// 从 HALCON 图像内存复制 real 或 uint2 深度数据。
        /// </summary>
        private static float[] CopyDepthValues(IntPtr pointer, int width, int height, string pixelType)
        {
            int count = checked(width * height);
            float[] values = new float[count];

            switch (pixelType)
            {
                case "byte":
                    byte[] sourceByte = new byte[count];
                    Marshal.Copy(pointer, sourceByte, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceByte[i];
                    }

                    return values;

                case "int2":
                    short[] sourceInt16 = new short[count];
                    Marshal.Copy(pointer, sourceInt16, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceInt16[i];
                    }

                    return values;

                case "uint2":
                    byte[] sourceBytes = new byte[count * sizeof(ushort)];
                    Marshal.Copy(pointer, sourceBytes, 0, sourceBytes.Length);
                    ushort[] sourceUInt16 = new ushort[count];
                    Buffer.BlockCopy(sourceBytes, 0, sourceUInt16, 0, sourceBytes.Length);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceUInt16[i];
                    }

                    return values;

                case "int4":
                    int[] sourceInt32 = new int[count];
                    Marshal.Copy(pointer, sourceInt32, 0, count);
                    for (int i = 0; i < count; i++)
                    {
                        values[i] = sourceInt32[i];
                    }

                    return values;

                case "real":
                    Marshal.Copy(pointer, values, 0, count);
                    return values;

                default:
                    throw new NotSupportedException($"当前不支持 HALCON 像素类型 {pixelType}。");
            }
        }
    }
}

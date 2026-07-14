using Custom.PhysicalScale.Models;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Custom.PhysicalScale.Helpers
{
    internal sealed class PhysicalScaleMicroHoleRegion
    {
        public List<int> Pixels { get; } = new List<int>();

        public int Area => Pixels.Count;

        public double CenterX { get; set; }

        public double CenterY { get; set; }

        public double MaxDistance { get; set; }
    }

    internal sealed class PhysicalScaleMicroHoleAnalysisResult
    {
        public BitmapSource CropBitmap { get; set; }

        public Int32Rect CropRect { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public byte[] GrayPixels { get; set; }

        public bool[] RoiMask { get; set; }

        public bool[] OpenMask { get; set; }

        public PhysicalScaleMicroHoleRegion MainOpenRegion { get; set; }

        public List<PhysicalScaleMicroHoleRegion> DefectRegions { get; set; } = new List<PhysicalScaleMicroHoleRegion>();

        public double NominalCenterX { get; set; }

        public double NominalCenterY { get; set; }

        public double NominalRadius { get; set; }

        public double FittedCenterX { get; set; }

        public double FittedCenterY { get; set; }

        public double FittedRadius { get; set; }

        public double DefectEvalRadius { get; set; }

        public double OpeningRatio { get; set; }

        public int OpenArea { get; set; }
    }

    internal static class PhysicalScaleMicroHoleAnalyzer
    {
        public static bool TryAnalyze(
            BitmapSource source,
            PhysicalScaleMeasurement holeRoi,
            PhysicalScaleHolePolarity polarity,
            int threshold,
            double minDefectAreaPx,
            out PhysicalScaleMicroHoleAnalysisResult result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;

            try
            {
                Int32Rect cropRect = ResolveCropBounds(holeRoi, source.PixelWidth, source.PixelHeight);
                if (cropRect.Width < 8 || cropRect.Height < 8)
                {
                    errorMessage = "单孔圆框范围过小，无法执行判定";
                    return false;
                }

                BitmapSource cropBitmap = new CroppedBitmap(source, cropRect);
                cropBitmap.Freeze();

                byte[] grayPixels = ExtractGrayPixels(cropBitmap, out int width, out int height);
                double nominalCenterX = holeRoi.StartX - cropRect.X;
                double nominalCenterY = holeRoi.StartY - cropRect.Y;
                double nominalRadius = Math.Max(holeRoi.RadiusPx, 4.0);

                bool[] roiMask = BuildCircleMask(width, height, nominalCenterX, nominalCenterY, nominalRadius);
                bool[] openCandidate = new bool[width * height];
                for (int i = 0; i < grayPixels.Length; i++)
                {
                    if (!roiMask[i])
                        continue;

                    bool isOpen = polarity == PhysicalScaleHolePolarity.DarkHoleOnBrightBackground
                        ? grayPixels[i] <= threshold
                        : grayPixels[i] >= threshold;
                    openCandidate[i] = isOpen;
                }

                List<PhysicalScaleMicroHoleRegion> openRegions = ExtractRegions(openCandidate, width, height);
                PhysicalScaleMicroHoleRegion mainOpenRegion = openRegions
                    .OrderByDescending(item => item.Area)
                    .ThenBy(item => DistanceSquared(item.CenterX, item.CenterY, nominalCenterX, nominalCenterY))
                    .FirstOrDefault();

                bool[] mainOpenMask = new bool[width * height];
                double fittedCenterX = nominalCenterX;
                double fittedCenterY = nominalCenterY;
                double fittedRadius = Math.Max(3.0, nominalRadius * 0.7);
                int openArea = 0;

                if (mainOpenRegion != null)
                {
                    foreach (int pixelIndex in mainOpenRegion.Pixels)
                    {
                        mainOpenMask[pixelIndex] = true;
                    }

                    openArea = mainOpenRegion.Area;
                    fittedCenterX = mainOpenRegion.CenterX;
                    fittedCenterY = mainOpenRegion.CenterY;
                    double equivalentRadius = Math.Sqrt(mainOpenRegion.Area / Math.PI);
                    fittedRadius = Math.Clamp(Math.Max(equivalentRadius, mainOpenRegion.MaxDistance), 3.0, nominalRadius);
                }

                double defectEvalRadius = Math.Max(3.0, Math.Min(nominalRadius - 1.0, fittedRadius * 0.88));
                bool[] defectMask = new bool[width * height];
                if (openArea > 0)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * width + x;
                            if (!IsInsideCircle(x, y, fittedCenterX, fittedCenterY, defectEvalRadius))
                                continue;

                            if (!mainOpenMask[index])
                            {
                                defectMask[index] = true;
                            }
                        }
                    }
                }

                List<PhysicalScaleMicroHoleRegion> defectRegions = ExtractRegions(defectMask, width, height)
                    .Where(item => item.Area >= Math.Max(1.0, minDefectAreaPx))
                    .OrderByDescending(item => item.Area)
                    .ToList();

                double roiArea = roiMask.Count(item => item);
                double openingRatio = roiArea > 0 ? openArea / roiArea : 0;

                result = new PhysicalScaleMicroHoleAnalysisResult
                {
                    CropBitmap = cropBitmap,
                    CropRect = cropRect,
                    Width = width,
                    Height = height,
                    GrayPixels = grayPixels,
                    RoiMask = roiMask,
                    OpenMask = mainOpenMask,
                    MainOpenRegion = mainOpenRegion,
                    DefectRegions = defectRegions,
                    NominalCenterX = nominalCenterX,
                    NominalCenterY = nominalCenterY,
                    NominalRadius = nominalRadius,
                    FittedCenterX = fittedCenterX,
                    FittedCenterY = fittedCenterY,
                    FittedRadius = fittedRadius,
                    DefectEvalRadius = defectEvalRadius,
                    OpeningRatio = openingRatio,
                    OpenArea = openArea
                };

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"微孔判定失败：{ex.Message}";
                return false;
            }
        }

        private static Int32Rect ResolveCropBounds(PhysicalScaleMeasurement measurement, int imageWidth, int imageHeight)
        {
            const double baseMargin = 24.0;
            double radius = Math.Max(measurement.RadiusPx, 4.0) + baseMargin;
            double left = measurement.StartX - radius;
            double top = measurement.StartY - radius;
            double right = measurement.StartX + radius;
            double bottom = measurement.StartY + radius;

            int x = Math.Clamp((int)Math.Floor(left), 0, Math.Max(0, imageWidth - 1));
            int y = Math.Clamp((int)Math.Floor(top), 0, Math.Max(0, imageHeight - 1));
            int maxRight = Math.Clamp((int)Math.Ceiling(right), 0, imageWidth);
            int maxBottom = Math.Clamp((int)Math.Ceiling(bottom), 0, imageHeight);
            return new Int32Rect(x, y, Math.Max(0, maxRight - x), Math.Max(0, maxBottom - y));
        }

        private static byte[] ExtractGrayPixels(BitmapSource source, out int width, out int height)
        {
            width = source.PixelWidth;
            height = source.PixelHeight;
            var grayBitmap = new FormatConvertedBitmap();
            grayBitmap.BeginInit();
            grayBitmap.Source = source;
            grayBitmap.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            grayBitmap.EndInit();
            grayBitmap.Freeze();

            byte[] pixels = new byte[width * height];
            grayBitmap.CopyPixels(pixels, width, 0);
            return pixels;
        }

        private static bool[] BuildCircleMask(int width, int height, double centerX, double centerY, double radius)
        {
            bool[] mask = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    mask[index] = IsInsideCircle(x, y, centerX, centerY, radius);
                }
            }

            return mask;
        }

        private static List<PhysicalScaleMicroHoleRegion> ExtractRegions(bool[] mask, int width, int height)
        {
            var regions = new List<PhysicalScaleMicroHoleRegion>();
            bool[] visited = new bool[mask.Length];

            for (int index = 0; index < mask.Length; index++)
            {
                if (!mask[index] || visited[index])
                    continue;

                var queue = new Queue<int>();
                var region = new PhysicalScaleMicroHoleRegion();
                queue.Enqueue(index);
                visited[index] = true;

                double sumX = 0;
                double sumY = 0;

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    region.Pixels.Add(current);
                    int x = current % width;
                    int y = current / width;
                    sumX += x;
                    sumY += y;

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                                continue;

                            int nextX = x + offsetX;
                            int nextY = y + offsetY;
                            if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                                continue;

                            int nextIndex = nextY * width + nextX;
                            if (!mask[nextIndex] || visited[nextIndex])
                                continue;

                            visited[nextIndex] = true;
                            queue.Enqueue(nextIndex);
                        }
                    }
                }

                region.CenterX = sumX / Math.Max(1, region.Pixels.Count);
                region.CenterY = sumY / Math.Max(1, region.Pixels.Count);
                region.MaxDistance = region.Pixels.Max(pixelIndex =>
                {
                    int px = pixelIndex % width;
                    int py = pixelIndex / width;
                    return Math.Sqrt(DistanceSquared(px, py, region.CenterX, region.CenterY));
                });
                regions.Add(region);
            }

            return regions;
        }

        internal static double DistanceSquared(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        internal static bool IsInsideCircle(double x, double y, double centerX, double centerY, double radius)
        {
            double dx = x - centerX;
            double dy = y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}

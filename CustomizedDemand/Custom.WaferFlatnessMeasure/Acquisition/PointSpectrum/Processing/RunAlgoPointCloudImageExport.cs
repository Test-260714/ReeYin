using HalconDotNet;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Custom.WaferFlatnessMeasure.Models
{
    public partial class SensorDataCollectionModel
    {
        private const float PointCloudImageInvalidValue = 8888880f;
        private const int MinPointCloudImageResolution = 16;
        private const int MaxPointCloudImageResolution = 5480;

        private void SaveRunAlgoPointCloudImages(IReadOnlyDictionary<string, List<double[]>> pointClouds)
        {
            LastPointCloudImageDirectory = string.Empty;

            if (!IsSaveRunAlgoPointCloudImages)
            {
                Logs.LogInfo("RunALGO point cloud height image export is disabled.");
                return;
            }

            if (pointClouds == null || pointClouds.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(PointCloudOutputDirectory))
            {
                Logs.LogInfo("RunALGO point cloud output directory is empty; GrayAndHeightChart height image export is skipped.");
                return;
            }

            try
            {
                string imageRootDirectory = Path.Combine(PointCloudOutputDirectory, "GrayAndHeightChart");
                string depthDirectory = Path.Combine(imageRootDirectory, "depth");
                Directory.CreateDirectory(depthDirectory);

                int exportedCount = 0;
                int resolution = ResolvePointCloudImageResolution();

                foreach (var pointCloud in pointClouds)
                {
                    if (pointCloud.Value == null || pointCloud.Value.Count == 0)
                    {
                        continue;
                    }

                    string fileName = SanitizePointCloudFileName(pointCloud.Key);
                    string filePath = Path.Combine(depthDirectory, $"{fileName}.tif");

                    if (!TryInterpolatePointCloudToHeightImage(
                            pointCloud.Value,
                            resolution,
                            out float[] imageData,
                            out int width,
                            out int height,
                            out string message))
                    {
                        Logs.LogWarning($"RunALGO point cloud {fileName} height image export failed: {message}");
                        continue;
                    }

                    WriteRealTiff(filePath, imageData, width, height);
                    exportedCount++;
                }

                if (exportedCount > 0)
                {
                    LastPointCloudImageDirectory = depthDirectory;
                    Logs.LogInfo(
                        $"RunALGO point cloud height images were exported to {depthDirectory}. TIFF count: {exportedCount}.");
                }
                else
                {
                    Logs.LogWarning("RunALGO has no point cloud data available for height image export.");
                }
            }
            catch (Exception ex)
            {
                LastPointCloudImageDirectory = string.Empty;
                Logs.LogError($"RunALGO point cloud height image export failed: {ex.Message}{Environment.NewLine}{ex}");
            }
        }

        private int ResolvePointCloudImageResolution()
        {
            return Math.Clamp(
                RunAlgoPointCloudImageResolution,
                MinPointCloudImageResolution,
                MaxPointCloudImageResolution);
        }

        private bool TryInterpolatePointCloudToHeightImage(
            IReadOnlyCollection<double[]> pointCloud,
            int maxResolution,
            out float[] imageData,
            out int width,
            out int height,
            out string message)
        {
            imageData = Array.Empty<float>();
            width = 0;
            height = 0;

            List<double[]> validPoints = pointCloud
                .Where(point => point != null &&
                                point.Length > 2 &&
                                double.IsFinite(point[0]) &&
                                double.IsFinite(point[1]) &&
                                double.IsFinite(point[2]))
                .ToList();

            if (validPoints.Count < 3)
            {
                message = $"Not enough valid points. Count: {validPoints.Count}.";
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
                message = "Invalid point cloud X/Y range.";
                return false;
            }

            rangeX = rangeX <= 0 ? rangeY : rangeX;
            rangeY = rangeY <= 0 ? rangeX : rangeY;
            maxX = minX + rangeX;
            maxY = minY + rangeY;

            if (rangeX >= rangeY)
            {
                width = maxResolution;
                height = Math.Max(2, (int)Math.Round(maxResolution * rangeY / rangeX));
            }
            else
            {
                height = maxResolution;
                width = Math.Max(2, (int)Math.Round(maxResolution * rangeX / rangeY));
            }

            width = Math.Clamp(width, 2, MaxPointCloudImageResolution);
            height = Math.Clamp(height, 2, MaxPointCloudImageResolution);

            imageData = new float[width * height];
            Array.Fill(imageData, PointCloudImageInvalidValue);

            bool[] roiMask = BuildPointCloudImageRoiMask(validPoints, minX, maxX, minY, maxY, width, height);
            double[] valueSums = new double[imageData.Length];
            int[] valueCounts = new int[imageData.Length];

            foreach (double[] point in validPoints)
            {
                int col = width <= 1 ? 0 : (int)Math.Round((point[0] - minX) / rangeX * (width - 1));
                int row = height <= 1 ? 0 : (int)Math.Round((maxY - point[1]) / rangeY * (height - 1));
                col = Math.Clamp(col, 0, width - 1);
                row = Math.Clamp(row, 0, height - 1);

                int index = row * width + col;
                roiMask[index] = true;
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
                message = "No valid pixel was mapped from the point cloud.";
                return false;
            }

            int interpolatedCount = FillHeightImageHolesWithNearestValue(imageData, roiMask, width, height);
            message = $"Direct pixels: {directFilledCount}; interpolated pixels: {interpolatedCount}.";
            return true;
        }

        private bool[] BuildPointCloudImageRoiMask(
            IReadOnlyList<double[]> validPoints,
            double minX,
            double maxX,
            double minY,
            double maxY,
            int width,
            int height)
        {
            bool[] roiMask = new bool[width * height];
            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double outerRadius = ResolveOuterRadius(validPoints, centerX, centerY);
            double innerRadius = ResolveInnerRadius();
            double xStep = width <= 1 ? 0.0 : (maxX - minX) / (width - 1);
            double yStep = height <= 1 ? 0.0 : (maxY - minY) / (height - 1);
            double pixelTolerance = Math.Max(xStep, yStep);
            double outerLimit = outerRadius + pixelTolerance;
            double innerLimit = Math.Max(0.0, innerRadius - pixelTolerance);
            double outerLimitSq = outerLimit * outerLimit;
            double innerLimitSq = innerLimit * innerLimit;

            for (int row = 0; row < height; row++)
            {
                double y = maxY - row * yStep;
                for (int col = 0; col < width; col++)
                {
                    double x = minX + col * xStep;
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distanceSq = dx * dx + dy * dy;

                    if (distanceSq <= outerLimitSq && distanceSq >= innerLimitSq)
                    {
                        roiMask[row * width + col] = true;
                    }
                }
            }

            return roiMask;
        }

        private double ResolveOuterRadius(IReadOnlyList<double[]> validPoints, double centerX, double centerY)
        {
            double? configuredOuterRadius = MeasureParam?.OuterRadius;
            if (configuredOuterRadius.HasValue &&
                double.IsFinite(configuredOuterRadius.Value) &&
                configuredOuterRadius.Value > 0)
            {
                return configuredOuterRadius.Value;
            }

            double maxDistance = 0.0;
            foreach (double[] point in validPoints)
            {
                double dx = point[0] - centerX;
                double dy = point[1] - centerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            return Math.Max(maxDistance, 1e-6);
        }

        private double ResolveInnerRadius()
        {
            double? configuredInnerRadius = MeasureParam?.InnerRadius;
            if (configuredInnerRadius.HasValue &&
                double.IsFinite(configuredInnerRadius.Value) &&
                configuredInnerRadius.Value > 0)
            {
                return configuredInnerRadius.Value;
            }

            return 0.0;
        }

        private static int FillHeightImageHolesWithNearestValue(
            float[] imageData,
            bool[] roiMask,
            int width,
            int height)
        {
            int[] queue = new int[imageData.Length];
            bool[] visited = new bool[imageData.Length];
            int head = 0;
            int tail = 0;

            for (int index = 0; index < imageData.Length; index++)
            {
                if (!roiMask[index] || !IsValidPointCloudImageValue(imageData[index]))
                {
                    continue;
                }

                visited[index] = true;
                queue[tail++] = index;
            }

            int filledCount = 0;
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
                    if (!roiMask[nextIndex] || visited[nextIndex])
                    {
                        continue;
                    }

                    imageData[nextIndex] = imageData[current];
                    visited[nextIndex] = true;
                    queue[tail++] = nextIndex;
                    filledCount++;
                }
            }

            return filledCount;
        }

        private static bool IsValidPointCloudImageValue(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   Math.Abs(value - PointCloudImageInvalidValue) > 1e-3f;
        }

        private static void WriteRealTiff(string path, float[] imageData, int width, int height)
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
                HOperatorSet.GenImage1(out image, "real", width, height, handle.AddrOfPinnedObject());
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

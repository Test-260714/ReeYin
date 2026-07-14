using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Series3D;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin_V.Core.ResultsDisplay;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.GrayAndHeightChart.Models
{
    internal sealed class GrayAndHeightChartDepthFilterResult
    {
        public float[][] FilteredData { get; init; } = Array.Empty<float[]>();

        public int TotalPoints { get; init; }

        public int ValidPoints { get; init; }

        public int FilteredPoints { get; init; }

        public int OriginalInvalidPoints { get; init; }

        public float OriginalMin { get; init; } = float.NaN;

        public float OriginalMax { get; init; } = float.NaN;
    }

    internal sealed class GrayAndHeightChartModel
    {
        private const int UltraFastSampleSize = 128;
        internal const int FastSampleSize = 256;
        private const int BalancedSampleSize = 512;
        private const int HighQualitySampleSize = 1024;
        private const int UltraHighQualitySampleSize = 2048;
        private const int UltimaHighQualitySampleSize = 5480;

        public GrayAndHeightChartView.QualityMode CurrentQualityMode { get; set; } =
            GrayAndHeightChartView.QualityMode.UltraHighQuality;

        public GrayAndHeightChartView.ColorPaletteType CurrentPaletteType { get; set; } =
            GrayAndHeightChartView.ColorPaletteType.Classic;

        public bool UseCustomColorRange { get; private set; }

        public float CustomMinValue { get; private set; } = -1.0f;

        public float CustomMaxValue { get; private set; } = 1.0f;

        public float DataMinValue { get; private set; } = float.MaxValue;

        public float DataMaxValue { get; private set; } = float.MinValue;

        public FeatureConfig FeatureConfig { get; private set; } = new();

        /// <summary>
        /// 读取缺陷参数配置；如果文件缺失或损坏，则回退到默认配置。
        /// </summary>
        public FeatureConfig LoadConfiguration(string configFilePath)
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Debug.WriteLine($"配置文件不存在: {configFilePath}");
                    return CreateDefaultConfiguration();
                }

                string jsonContent = File.ReadAllText(configFilePath, System.Text.Encoding.UTF8);
                FeatureConfig = JsonConvert.DeserializeObject<FeatureConfig>(jsonContent) ?? CreateDefaultConfiguration();
                Debug.WriteLine("GrayAndHeightChart 配置文件加载成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载 GrayAndHeightChart 配置失败: {ex.Message}");
                CreateDefaultConfiguration();
            }

            return FeatureConfig;
        }

        /// <summary>
        /// 将当前配置写回 JSON 文件，供下次启动复用。
        /// </summary>
        public void SaveConfiguration(string configFilePath, FeatureConfig? featureConfig = null)
        {
            FeatureConfig = featureConfig ?? FeatureConfig;

            string? directory = Path.GetDirectoryName(configFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            JsonSerializerSettings jsonSettings = new()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include
            };

            string jsonContent = JsonConvert.SerializeObject(FeatureConfig, jsonSettings);
            File.WriteAllText(configFilePath, jsonContent, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 提供一份最小可用的默认配置，避免控件因配置缺失无法工作。
        /// </summary>
        public FeatureConfig CreateDefaultConfiguration()
        {
            FeatureConfig = new FeatureConfig
            {
                DefectList =
                [
                    new Defect
                    {
                        Id = 0,
                        Name = "翘钉",
                        AlgName = "GetWarpFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "height_select", Describe = "NG阈值", Value = 75 }
                        ]
                    },
                    new Defect
                    {
                        Id = 1,
                        Name = "轨迹偏移",
                        AlgName = "GetOrbitFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "offset_select", Describe = "NG阈值", Value = 200 }
                        ]
                    },
                    new Defect
                    {
                        Id = 2,
                        Name = "裂纹",
                        AlgName = "GetCrackFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "area_select", Describe = "面积NG阈值", Value = 0 },
                            new Parameter { Name = "length_select", Describe = "长度NG阈值", Value = 0 },
                            new Parameter { Name = "width_select", Describe = "宽度NG阈值", Value = 0 },
                            new Parameter { Name = "depth_select", Describe = "深度NG阈值", Value = 0 }
                        ]
                    },
                    new Defect
                    {
                        Id = 3,
                        Name = "失真",
                        AlgName = "GetDiameterFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "image_type", Describe = "使用灰度图:0; 使用高度图:1", Value = 1 },
                            new Parameter { Name = "select", Describe = "NG 阈值", Value = 0 },
                            new Parameter { Name = "area_lower_limit", Describe = "面积下限", Value = 1000 },
                            new Parameter { Name = "area_upper_limit", Describe = "面积上限", Value = 1000000000 }
                        ]
                    },
                    new Defect
                    {
                        Id = 4,
                        Name = "焊瘤",
                        AlgName = "GetDiameterFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "image_type", Describe = "使用灰度图:0; 使用高度图:1", Value = 1 },
                            new Parameter { Name = "select", Describe = "NG 阈值", Value = 0 },
                            new Parameter { Name = "area_lower_limit", Describe = "面积下限", Value = 0 }
                        ]
                    }
                ]
            };

            return FeatureConfig;
        }

        public void SetCustomColorRange(float minValue, float maxValue)
        {
            UseCustomColorRange = true;
            CustomMinValue = minValue;
            CustomMaxValue = maxValue;
        }

        public void DisableCustomColorRange()
        {
            UseCustomColorRange = false;
        }

        public bool TryGetDataRange(out float minValue, out float maxValue)
        {
            minValue = DataMinValue;
            maxValue = DataMaxValue;
            return DataMinValue != float.MaxValue && DataMaxValue != float.MinValue;
        }

        /// <summary>
        /// 清空当前缓存的数据范围。
        /// 当宿主将结果置空时，页面可以恢复到无数据状态。
        /// </summary>
        public void ClearDataRange()
        {
            DataMinValue = float.MaxValue;
            DataMaxValue = float.MinValue;
        }

        /// <summary>
        /// 从本地文件读取深度图，并统一整理为单通道 Mat，便于后续转成高度矩阵。
        /// </summary>
        public Mat LoadDepthImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("深度图路径不能为空。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("未找到指定的深度图文件。", filePath);
            }

            Mat source = Cv2.ImRead(filePath, ImreadModes.Unchanged);
            if (source.Empty())
            {
                source.Dispose();
                throw new InvalidOperationException("图片读取失败，可能是不支持的深度图格式。");
            }

            if (source.Channels() == 1)
            {
                return source;
            }

            Mat[] channels = Cv2.Split(source);
            Mat depthImage = channels[0].Clone();

            foreach (Mat channel in channels)
            {
                channel.Dispose();
            }

            source.Dispose();
            return depthImage;
        }

        /// <summary>
        /// 根据质量档位给出采样目标尺寸，控制大图显示性能。
        /// </summary>
        public int GetSampleSizeForQuality(GrayAndHeightChartView.QualityMode mode)
        {
            return mode switch
            {
                GrayAndHeightChartView.QualityMode.UltraFast => UltraFastSampleSize,
                GrayAndHeightChartView.QualityMode.Fast => FastSampleSize,
                GrayAndHeightChartView.QualityMode.Balanced => BalancedSampleSize,
                GrayAndHeightChartView.QualityMode.HighQuality => HighQualitySampleSize,
                GrayAndHeightChartView.QualityMode.UltraHighQuality => UltraHighQualitySampleSize,
                GrayAndHeightChartView.QualityMode.UltimaHighQuality => UltimaHighQualitySampleSize,
                _ => BalancedSampleSize
            };
        }

        /// <summary>
        /// 按目标尺寸对高度图进行采样，优先保留轮廓细节。
        /// </summary>
        public float[][] SmartSampleFloatArray(float[][] originalData, int targetSize, bool useAdvancedSampling = true)
        {
            if (originalData == null || originalData.Length == 0 || originalData[0].Length == 0)
            {
                return originalData;
            }

            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            if (originalRows <= targetSize && originalCols <= targetSize)
            {
                return originalData;
            }

            return useAdvancedSampling
                ? BilinearSample(originalData, targetSize)
                : NearestNeighborSample(originalData, targetSize);
        }

        /// <summary>
        /// 过滤无效深度值，并保留筛选统计结果给界面层决定如何展示。
        /// </summary>
        public GrayAndHeightChartDepthFilterResult FilterDepthData(float[][] heightData, float minDepth, float maxDepth)
        {
            if (heightData == null || heightData.Length == 0)
            {
                return new GrayAndHeightChartDepthFilterResult { FilteredData = heightData };
            }

            int rows = heightData.Length;
            int cols = heightData[0].Length;
            int totalPoints = rows * cols;
            int validPoints = 0;
            int filteredPoints = 0;
            int originalInvalidPoints = 0;
            float originalMin = float.MaxValue;
            float originalMax = float.MinValue;

            float[][] filteredData = new float[rows][];

            for (int i = 0; i < rows; i++)
            {
                filteredData[i] = new float[cols];

                for (int j = 0; j < cols; j++)
                {
                    float value = heightData[i][j];

                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        filteredData[i][j] = float.NaN;
                        originalInvalidPoints++;
                        continue;
                    }

                    originalMin = Math.Min(originalMin, value);
                    originalMax = Math.Max(originalMax, value);

                    if (value < minDepth || value > maxDepth)
                    {
                        filteredData[i][j] = float.NaN;
                        filteredPoints++;
                        continue;
                    }

                    filteredData[i][j] = value;
                    validPoints++;
                }
            }

            Debug.WriteLine("深度数据筛选完成");
            Debug.WriteLine($"  总数据点: {totalPoints:N0}");
            Debug.WriteLine($"  原始无效点: {originalInvalidPoints:N0}");
            Debug.WriteLine($"  超出筛选范围点: {filteredPoints:N0}");
            Debug.WriteLine($"  最终有效点: {validPoints:N0}");

            return new GrayAndHeightChartDepthFilterResult
            {
                FilteredData = filteredData,
                TotalPoints = totalPoints,
                ValidPoints = validPoints,
                FilteredPoints = filteredPoints,
                OriginalInvalidPoints = originalInvalidPoints,
                OriginalMin = originalMin == float.MaxValue ? float.NaN : originalMin,
                OriginalMax = originalMax == float.MinValue ? float.NaN : originalMax
            };
        }

        /// <summary>
        /// 将 OpenCV 的单通道 Mat 转成 float 二维数组，便于后续采样和绘图。
        /// </summary>
        public static float[][] MatToFloat2DArray(Mat mat)
        {
            if (mat.Type() != MatType.CV_32FC1)
            {
                mat.ConvertTo(mat, MatType.CV_32FC1);
            }

            int rows = mat.Rows;
            int cols = mat.Cols;
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            float[][] result = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                result[i] = new float[cols];
                Array.Copy(buffer, i * cols, result[i], 0, cols);
            }

            return result;
        }

        /// <summary>
        /// 根据当前调色板类型生成 LightningChart 需要的色带对象。
        /// </summary>
        public ValueRangePalette CreatePalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            return CurrentPaletteType == GrayAndHeightChartView.ColorPaletteType.Classic
                ? CreateGradientPalette(ownerSeries, heightDatas)
                : CreateHeatmapPalette(ownerSeries, heightDatas);
        }

        /// <summary>
        /// 计算当前高度数据的有效最小值和最大值。
        /// </summary>
        public (float MinValue, float MaxValue) CalculateDataRange(float[][] heightDatas)
        {
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;
            int validCount = 0;

            foreach (float[] row in heightDatas)
            {
                foreach (float value in row)
                {
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        continue;
                    }

                    validCount++;
                    minVal = Math.Min(minVal, value);
                    maxVal = Math.Max(maxVal, value);
                }
            }

            if (validCount == 0 || minVal == float.MaxValue || maxVal == float.MinValue)
            {
                Debug.WriteLine("未找到有效高度数据，使用默认色域 [-1.0, 1.0]");
                return (-1.0f, 1.0f);
            }

            return (minVal, maxVal);
        }

        private float[][] BilinearSample(float[][] originalData, int targetSize)
        {
            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            double scale = originalRows >= originalCols
                ? (double)targetSize / originalRows
                : (double)targetSize / originalCols;

            int newRows = Math.Max(1, (int)Math.Round(originalRows * scale));
            int newCols = Math.Max(1, (int)Math.Round(originalCols * scale));

            float[][] resizedData = new float[newRows][];
            for (int i = 0; i < newRows; i++)
            {
                resizedData[i] = new float[newCols];
            }

            if (newRows == 1 || newCols == 1)
            {
                return NearestNeighborSample(originalData, targetSize);
            }

            double rowScale = (double)(originalRows - 1) / (newRows - 1);
            double colScale = (double)(originalCols - 1) / (newCols - 1);

            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    double srcRow = i * rowScale;
                    double srcCol = j * colScale;

                    int row1 = Math.Min((int)srcRow, originalRows - 2);
                    int col1 = Math.Min((int)srcCol, originalCols - 2);
                    int row2 = row1 + 1;
                    int col2 = col1 + 1;

                    double rowWeight = srcRow - row1;
                    double colWeight = srcCol - col1;

                    float val11 = originalData[row1][col1];
                    float val12 = originalData[row1][col2];
                    float val21 = originalData[row2][col1];
                    float val22 = originalData[row2][col2];

                    resizedData[i][j] = (float)(
                        val11 * (1 - rowWeight) * (1 - colWeight) +
                        val12 * (1 - rowWeight) * colWeight +
                        val21 * rowWeight * (1 - colWeight) +
                        val22 * rowWeight * colWeight);
                }
            }

            return resizedData;
        }

        private float[][] NearestNeighborSample(float[][] originalData, int targetSize)
        {
            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            double scale = originalRows >= originalCols
                ? (double)targetSize / originalRows
                : (double)targetSize / originalCols;

            int newRows = Math.Max(1, (int)Math.Round(originalRows * scale));
            int newCols = Math.Max(1, (int)Math.Round(originalCols * scale));

            float[][] resizedData = new float[newRows][];
            for (int i = 0; i < newRows; i++)
            {
                resizedData[i] = new float[newCols];
            }

            double rowStep = (double)originalRows / newRows;
            double colStep = (double)originalCols / newCols;

            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    int srcRow = Math.Min((int)Math.Round(i * rowStep), originalRows - 1);
                    int srcCol = Math.Min((int)Math.Round(j * colStep), originalCols - 1);
                    resizedData[i][j] = originalData[srcRow][srcCol];
                }
            }

            return resizedData;
        }

        private ValueRangePalette CreateGradientPalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            ValueRangePalette palette = new(ownerSeries)
            {
                Type = PaletteType.Gradient
            };

            palette.Steps.Clear();

            (float minVal, float maxVal) = ResolvePaletteRange(heightDatas);
            float range = maxVal - minVal;
            palette.MinValue = minVal;

            float step1 = minVal + range * 0.0f;
            float step2 = minVal + range * 0.125f;
            float step3 = minVal + range * 0.25f;
            float step4 = minVal + range * 0.375f;
            float step5 = minVal + range * 0.5f;
            float step6 = minVal + range * 0.625f;
            float step7 = minVal + range * 0.75f;
            float step8 = minVal + range * 0.875f;
            float step9 = maxVal;

            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 139), step1));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 255), step2));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 255), step3));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 128), step4));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 0), step5));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(128, 255, 0), step6));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 0), step7));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 165, 0), step8));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 0, 0), step9));

            return palette;
        }

        private ValueRangePalette CreateHeatmapPalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            ValueRangePalette palette = new(ownerSeries)
            {
                Type = PaletteType.Gradient
            };

            palette.Steps.Clear();

            (float minVal, float maxVal) = ResolvePaletteRange(heightDatas);
            float range = maxVal - minVal;
            palette.MinValue = minVal;

            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 0), minVal));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(128, 0, 128), minVal + range * 0.15f));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 255), minVal + range * 0.3f));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 0), minVal + range * 0.5f));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 0), minVal + range * 0.7f));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 0, 0), minVal + range * 0.85f));
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 255), maxVal));

            return palette;
        }

        private (float MinValue, float MaxValue) ResolvePaletteRange(float[][] heightDatas)
        {
            if (UseCustomColorRange)
            {
                return (CustomMinValue, CustomMaxValue);
            }

            (float dataMin, float dataMax) = CalculateDataRange(heightDatas);
            SetDataRange(dataMin, dataMax);

            if (Math.Abs(dataMax - dataMin) < 1e-6f)
            {
                return (-1.0f, 1.0f);
            }

            return (dataMin, dataMax);
        }

        private void SetDataRange(float minValue, float maxValue)
        {
            DataMinValue = minValue;
            DataMaxValue = maxValue;
        }
    }
}

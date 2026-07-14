using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.ResultsDisplay;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ImageCommonAlgorithm = ReeYin_V.Core.Helper.ImageOP.Common_Algorithm;
using ImageConvertHelper = ReeYin_V.Core.Helper.ImageOP.ImageHelper;

namespace Custom.MFDJC.Models
{
    public sealed class ElectroStaticChuckImageSet
    {
        public ElectroStaticChuckImageSet(string grayImagePath, string heightImagePath, string sourceFolder)
        {
            GrayImagePath = grayImagePath;
            HeightImagePath = heightImagePath;
            SourceFolder = sourceFolder;
        }

        public string GrayImagePath { get; }

        public string HeightImagePath { get; }

        public string SourceFolder { get; }
    }

    public sealed class ElectroStaticChuckAlgorithmRunResult : IDisposable
    {
        public ElectroStaticChuckAlgorithmRunResult(
            ElectroStaticChuck_MeasureResult measureResult,
            ElectroStaticChuck_MeasureParam measureParam,
            Mat heightDisplayImage,
            Mat planeDisplayImage,
            HObject? planeDisplayHObject)
        {
            MeasureResult = measureResult;
            MeasureParam = measureParam;
            HeightDisplayImage = heightDisplayImage;
            PlaneDisplayImage = planeDisplayImage;
            PlaneDisplayHObject = planeDisplayHObject;
        }

        public ElectroStaticChuck_MeasureResult MeasureResult { get; }

        public ElectroStaticChuck_MeasureParam MeasureParam { get; }

        public Mat HeightDisplayImage { get; }

        public Mat PlaneDisplayImage { get; }

        public HObject? PlaneDisplayHObject { get; }

        public ImageResultsDisplay CreateImageResultsDisplay()
        {
            return new ImageResultsDisplay
            {
                GrayImage = PlaneDisplayImage.Clone(),
                HeightImage = HeightDisplayImage.Clone()
            };
        }

        public void Dispose()
        {
            HeightDisplayImage.Dispose();
            PlaneDisplayImage.Dispose();
            PlaneDisplayHObject?.Dispose();
        }
    }

    public static class ElectroStaticChuckImagePipeline
    {
        public const string DefaultImageFolder = @"C:\Users\19765\Desktop\项目\静电卡盘";

        private static readonly string[] SupportedImageExtensions =
        {
            ".tif", ".tiff", ".png", ".bmp", ".jpg", ".jpeg", ".exr"
        };

        private static readonly string[] HeightKeywords =
        {
            "depth", "height", "z", "深度", "高度"
        };

        private static readonly string[] GrayKeywords =
        {
            "gray", "grey", "plane", "intensity", "2d", "平面", "灰度"
        };

        public static (string DepthImagePath, string PlaneImagePath) ResolveImageFiles(string folder)
        {
            ElectroStaticChuckImageSet imageSet = ResolveImageSets(folder).First();
            return (imageSet.HeightImagePath, imageSet.GrayImagePath);
        }

        public static List<ElectroStaticChuckImageSet> ResolveImageSets(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new InvalidOperationException("图片文件夹为空。");
            }

            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"图片文件夹不存在：{folder}");
            }

            ElectroStaticChuckImageSet? directSet = TryResolveImageSet(folder);
            if (directSet != null)
            {
                return new List<ElectroStaticChuckImageSet> { directSet };
            }

            List<ElectroStaticChuckImageSet> imageSets = Directory.GetDirectories(folder)
                .Select(TryResolveImageSet)
                .Where(imageSet => imageSet != null)
                .Cast<ElectroStaticChuckImageSet>()
                .OrderBy(imageSet => imageSet.SourceFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (imageSets.Count == 0)
            {
                throw new InvalidOperationException("请在文件夹中放入平面图和深度图，或使用包含 gray/depth 子目录的采集文件夹。");
            }

            return imageSets;
        }

        public static ElectroStaticChuckAlgorithmRunResult RunAlgorithm(
            ElectroStaticChuck_Algorithm algorithm,
            ElectroStaticChuckImageSet imageSet,
            ElectroStaticChuck_MeasureParam? baseParam = null)
        {
            return RunAlgorithm(algorithm, imageSet.GrayImagePath, imageSet.HeightImagePath, baseParam);
        }

        public static ElectroStaticChuckAlgorithmRunResult RunAlgorithm(
            ElectroStaticChuck_Algorithm algorithm,
            string grayImagePath,
            string heightImagePath,
            ElectroStaticChuck_MeasureParam? baseParam = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            using Mat grayImage = LoadGrayImage(grayImagePath);
            using Mat heightImage = LoadHeightImage(heightImagePath);

            List<float[]> grayData = ImageCommonAlgorithm.ConvertMatToList(grayImage);
            List<float[]> heightData = ImageCommonAlgorithm.ConvertMatToList(heightImage);
            ElectroStaticChuck_MeasureParam measureParam = CreateMeasureParamFromFileName(grayImagePath, baseParam);

            measureParam.IntervalX = 23;
            measureParam.IntervalY = 23;
            measureParam.IntervalZ = 1;
            measureParam.MinDepth = -5000;
            measureParam.MaxDepth = 5000;
            measureParam.InvalidValue = 888888;
            measureParam.IsFlip = false;
            measureParam.ConvexStandardDiameter = 1300;
            measureParam.ConvexStandardHeight = 30;


            ElectroStaticChuck_MeasureResult measureResult = algorithm.Process(grayData, heightData, measureParam);

            Mat heightDisplayImage = HObjectToMat(measureResult.HeightImage);
            if (heightDisplayImage.Empty())
            {
                heightDisplayImage.Dispose();
                heightDisplayImage = heightImage.Clone();
            }

            using Mat drawResult = algorithm.CvDrawResult(measureResult);
            Mat planeDisplayImage = drawResult.Empty() ? grayImage.Clone() : drawResult.Clone();
            HObject? planeDisplayHObject = ImageConvertHelper.ConvertMatToHObject(planeDisplayImage);

            return new ElectroStaticChuckAlgorithmRunResult(
                measureResult,
                measureParam,
                heightDisplayImage,
                planeDisplayImage,
                planeDisplayHObject);
        }

        public static ElectroStaticChuck_MeasureParam CreateMeasureParamFromFileName(
            string imagePath,
            ElectroStaticChuck_MeasureParam? baseParam = null)
        {
            ElectroStaticChuck_MeasureParam measureParam = CopyMeasureParam(baseParam);
            string imageName = Path.GetFileNameWithoutExtension(imagePath);
            double[] values = imageName
                .Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TryParseNumber)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToArray();

            if (values.Length >= 7)
            {
                measureParam.IntervalX = values[0];
                measureParam.IntervalY = values[1];
                measureParam.IntervalZ = values[2];
                measureParam.MinDepth = values[3];
                measureParam.MaxDepth = values[4];
                measureParam.IsFlip = false;
                measureParam.IsScanEnd = false;
                measureParam.OffsetX = values[5];
                measureParam.OffsetY = values[6];
            }
            else if (values.Length >= 6)
            {
                measureParam.IntervalX = values[0];
                measureParam.IntervalY = values[1];
                measureParam.IntervalZ = 0.1;
                measureParam.MinDepth = values[2];
                measureParam.MaxDepth = values[3];
                measureParam.IsFlip = false;
                measureParam.IsScanEnd = false;
                measureParam.OffsetX = values[4];
                measureParam.OffsetY = values[5];
            }

            return measureParam;
        }

        public static Mat HObjectToMat(HObject image)
        {
            if (image == null || !image.IsInitialized())
            {
                return new Mat();
            }

            HOperatorSet.CountChannels(image, out HTuple channels);
            if (channels.Length == 0)
            {
                return new Mat();
            }

            int channelCount = channels[0].I;
            if (channelCount == 1)
            {
                HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
                return Mat.FromPixelData(height.I, width.I, GetSingleChannelMatType(type.S), (IntPtr)pointer).Clone();
            }

            if (channelCount == 3)
            {
                HOperatorSet.GetImagePointer3(
                    image,
                    out HTuple redPointer,
                    out HTuple greenPointer,
                    out HTuple bluePointer,
                    out HTuple type,
                    out HTuple width,
                    out HTuple height);

                MatType matType = GetSingleChannelMatType(type.S);
                using Mat red = Mat.FromPixelData(height.I, width.I, matType, (IntPtr)redPointer);
                using Mat green = Mat.FromPixelData(height.I, width.I, matType, (IntPtr)greenPointer);
                using Mat blue = Mat.FromPixelData(height.I, width.I, matType, (IntPtr)bluePointer);
                using Mat bgr = new();
                Cv2.Merge(new[] { blue, green, red }, bgr);

                Mat gray = new();
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                return gray;
            }

            return new Mat();
        }

        private static ElectroStaticChuckImageSet? TryResolveImageSet(string folder)
        {
            string grayFolder = Path.Combine(folder, "gray");
            string heightFolder = Path.Combine(folder, "depth");
            if (Directory.Exists(grayFolder) && Directory.Exists(heightFolder))
            {
                string? grayPath = FindImageFiles(grayFolder, SearchOption.TopDirectoryOnly).FirstOrDefault();
                string? heightPath = FindImageFiles(heightFolder, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (grayPath != null && heightPath != null)
                {
                    return new ElectroStaticChuckImageSet(grayPath, heightPath, folder);
                }
            }

            List<string> files = FindImageFiles(folder, SearchOption.TopDirectoryOnly);
            if (files.Count < 2)
            {
                return null;
            }

            string? heightImagePath = files.FirstOrDefault(file => ContainsKeyword(file, HeightKeywords));
            string? grayImagePath = files.FirstOrDefault(file =>
                !IsSamePath(file, heightImagePath) && ContainsKeyword(file, GrayKeywords));

            heightImagePath ??= files.FirstOrDefault(file => !IsSamePath(file, grayImagePath));
            grayImagePath ??= files.FirstOrDefault(file => !IsSamePath(file, heightImagePath));

            if (grayImagePath == null || heightImagePath == null)
            {
                grayImagePath = files[0];
                heightImagePath = files[1];
            }

            return new ElectroStaticChuckImageSet(grayImagePath, heightImagePath, folder);
        }

        private static List<string> FindImageFiles(string folder, SearchOption searchOption)
        {
            if (!Directory.Exists(folder))
            {
                return new List<string>();
            }

            return Directory.EnumerateFiles(folder, "*.*", searchOption)
                .Where(file => SupportedImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool ContainsKeyword(string filePath, IEnumerable<string> keywords)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
            string? parentName = Path.GetFileName(Path.GetDirectoryName(filePath));
            string searchableText = $"{fileName} {parentName}";
            return keywords.Any(keyword => searchableText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSamePath(string filePath, string? otherFilePath)
        {
            return !string.IsNullOrEmpty(otherFilePath)
                   && string.Equals(filePath, otherFilePath, StringComparison.OrdinalIgnoreCase);
        }

        private static Mat LoadGrayImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("平面图不存在。", imagePath);
            }

            Mat image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
            if (image.Empty())
            {
                image.Dispose();
                throw new InvalidOperationException($"平面图读取失败：{imagePath}");
            }

            if (image.Type() == MatType.CV_8UC1 && image.IsContinuous())
            {
                return image;
            }

            Mat normalized = new();
            image.ConvertTo(normalized, MatType.CV_8UC1);
            image.Dispose();
            return normalized;
        }

        private static Mat LoadHeightImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("深度图不存在。", imagePath);
            }

            using Mat source = Cv2.ImRead(imagePath, ImreadModes.Unchanged);
            if (source.Empty())
            {
                throw new InvalidOperationException($"深度图读取失败：{imagePath}");
            }

            Mat singleChannel = source;
            Mat? convertedChannel = null;
            if (source.Channels() > 1)
            {
                convertedChannel = new Mat();
                Cv2.CvtColor(source, convertedChannel, ColorConversionCodes.BGR2GRAY);
                singleChannel = convertedChannel;
            }

            Mat normalized = new();
            if (singleChannel.Type() == MatType.CV_8UC1
                || singleChannel.Type() == MatType.CV_16SC1
                || singleChannel.Type() == MatType.CV_32SC1
                || singleChannel.Type() == MatType.CV_32FC1)
            {
                normalized = singleChannel.Clone();
            }
            else
            {
                singleChannel.ConvertTo(normalized, MatType.CV_32FC1);
            }

            convertedChannel?.Dispose();
            return normalized;
        }

        private static ElectroStaticChuck_MeasureParam CopyMeasureParam(ElectroStaticChuck_MeasureParam? source)
        {
            if (source == null)
            {
                return new ElectroStaticChuck_MeasureParam();
            }

            return new ElectroStaticChuck_MeasureParam
            {
                IntervalX = source.IntervalX,
                IntervalY = source.IntervalY,
                IntervalZ = source.IntervalZ,
                MinDepth = source.MinDepth,
                MaxDepth = source.MaxDepth,
                InvalidValue = source.InvalidValue,
                IsFlip = source.IsFlip,
                IsScanEnd = source.IsScanEnd,
                OffsetX = source.OffsetX,
                OffsetY = source.OffsetY,
                ConvexStandardDiameter = source.ConvexStandardDiameter,
                ConvexStandardHeight = source.ConvexStandardHeight
            };
        }

        private static double? TryParseNumber(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariantValue))
            {
                return invariantValue;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out double currentValue))
            {
                return currentValue;
            }

            return null;
        }

        private static MatType GetSingleChannelMatType(string halconType)
        {
            return halconType switch
            {
                "byte" => MatType.CV_8UC1,
                "int1" => MatType.CV_8SC1,
                "uint2" => MatType.CV_16UC1,
                "int2" => MatType.CV_16SC1,
                "int4" => MatType.CV_32SC1,
                "real" => MatType.CV_32FC1,
                _ => throw new NotSupportedException($"不支持的HALCON图像类型：{halconType}")
            };
        }
    }
}

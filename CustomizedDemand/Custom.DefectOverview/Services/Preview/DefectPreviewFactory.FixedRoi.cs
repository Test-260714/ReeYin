using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.DefectOverview.Services
{
    public static partial class DefectPreviewFactory
    {
        private const double FixedPreviewMinCropSize = 16.0;

        public static BitmapSource CreateFixedDefectPreviewBitmapFromHImage(
            HImage image,
            int sourceWidth,
            int sourceHeight,
            Result defect)
        {
            HObject croppedObject = null;
            HObject zoomedObject = null;
            HImage zoomedImage = null;

            try
            {
                if (image == null || !image.IsInitialized() || defect == null)
                    return null;

                if (sourceWidth <= 0 || sourceHeight <= 0)
                    image.GetImageSize(out sourceWidth, out sourceHeight);

                if (sourceWidth <= 0 || sourceHeight <= 0)
                    return null;

                ResolvePreviewGeometry(
                    defect,
                    sourceWidth,
                    sourceHeight,
                    sourceWidth,
                    sourceHeight,
                    out double centerX,
                    out double centerY,
                    out double scaledWidth,
                    out double scaledHeight,
                    out double scaledAngle,
                    out bool usedFallbackGeometry,
                    out string geometrySource);

                ResolveRotatedBounds(
                    scaledWidth,
                    scaledHeight,
                    scaledAngle,
                    out double boundsWidth,
                    out double boundsHeight);

                int outputSize = Math.Max(1, DefectOverviewRuntimeOptions.DefectImageSize);
                int boxSize = Math.Clamp(
                    DefectOverviewRuntimeOptions.DefectImageBoxSize,
                    1,
                    Math.Max(1, outputSize - 4));

                double cropByWidth = boundsWidth * outputSize / boxSize;
                double cropByHeight = boundsHeight * outputSize / boxSize;
                double cropSize = Math.Max(Math.Max(cropByWidth, cropByHeight), FixedPreviewMinCropSize);

                int cropWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(sourceWidth, cropSize)));
                int cropHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(sourceHeight, cropSize)));
                double cropX = centerX - cropWidth / 2.0;
                double cropY = centerY - cropHeight / 2.0;
                ClampCropOrigin(ref cropX, ref cropY, cropWidth, cropHeight, sourceWidth, sourceHeight);

                int left = Math.Clamp((int)Math.Round(cropX), 0, Math.Max(0, sourceWidth - cropWidth));
                int top = Math.Clamp((int)Math.Round(cropY), 0, Math.Max(0, sourceHeight - cropHeight));
                int right = Math.Min(sourceWidth - 1, left + cropWidth - 1);
                int bottom = Math.Min(sourceHeight - 1, top + cropHeight - 1);

                HOperatorSet.CropRectangle1(image, out croppedObject, top, left, bottom, right);
                HOperatorSet.ZoomImageSize(croppedObject, out zoomedObject, outputSize, outputSize, "constant");
                zoomedImage = new HImage(zoomedObject);

                BitmapSource previewBitmap = CreateBitmapFromHImage(zoomedImage);
                double locatorCenterX = (centerX - left) * outputSize / Math.Max(1, cropWidth);
                double locatorCenterY = (centerY - top) * outputSize / Math.Max(1, cropHeight);
                BitmapSource fixedPreview = DrawFixedLocatorBorder(previewBitmap, outputSize, boxSize, locatorCenterX, locatorCenterY);

                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectPreviewFactory] FixedPreview {DescribeDefect(defect)} geomSource={geometrySource}, fallbackGeom={usedFallbackGeometry}, source={sourceWidth}x{sourceHeight}, geom=({centerX:F1},{centerY:F1},{scaledWidth:F1},{scaledHeight:F1},{scaledAngle:F4}), crop=({left},{top},{cropWidth},{cropHeight}), output={DescribeBitmap(fixedPreview)}, box={boxSize}");
                }

                return fixedPreview;
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                    $"[DefectPreviewFactory] FixedPreview exception {DescribeDefect(defect)}: {ex.Message}");
                return null;
            }
            finally
            {
                zoomedImage?.Dispose();
                zoomedObject?.Dispose();
                croppedObject?.Dispose();
            }
        }

        private static BitmapSource DrawFixedLocatorBorder(
            BitmapSource bitmap,
            int outputSize,
            int boxSize,
            double locatorCenterX,
            double locatorCenterY)
        {
            if (bitmap == null || outputSize <= 0 || boxSize <= 0)
                return bitmap;

            BitmapSource source = bitmap;
            if (source.PixelWidth != outputSize || source.PixelHeight != outputSize)
            {
                TransformedBitmap scaled = new(source, new ScaleTransform(
                    (double)outputSize / Math.Max(1, source.PixelWidth),
                    (double)outputSize / Math.Max(1, source.PixelHeight)));
                scaled.Freeze();
                source = scaled;
            }

            byte[] pixels = ExtractBgra32Pixels(source);
            if (pixels == null)
                return source;

            if (!IsFinite(locatorCenterX))
                locatorCenterX = outputSize / 2.0;
            if (!IsFinite(locatorCenterY))
                locatorCenterY = outputSize / 2.0;

            double left = Math.Clamp(locatorCenterX - boxSize / 2.0, 0.0, Math.Max(0.0, outputSize - boxSize));
            double top = Math.Clamp(locatorCenterY - boxSize / 2.0, 0.0, Math.Max(0.0, outputSize - boxSize));
            DrawAxisAlignedBorder(pixels, outputSize, outputSize, left, top, boxSize, boxSize);

            BitmapSource result = BitmapSource.Create(
                outputSize,
                outputSize,
                96.0,
                96.0,
                PixelFormats.Bgra32,
                null,
                pixels,
                outputSize * 4);
            result.Freeze();
            return result;
        }
    }
}

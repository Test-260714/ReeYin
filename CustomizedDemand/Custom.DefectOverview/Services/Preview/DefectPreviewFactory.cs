using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.DefectOverview.Services
{
    public static partial class DefectPreviewFactory
    {
        private static readonly int MaxDisplayWidth = DefectOverviewRuntimeOptions.BaseBitmapMaxDimension;
        private static readonly int MaxDisplayHeight = DefectOverviewRuntimeOptions.BaseBitmapMaxDimension;
        private static readonly int MaxPreviewCacheDimension = DefectOverviewRuntimeOptions.PreviewMaxDimension;
        private const double MinPreviewCropSize = 96.0;
        private const int PreviewBorderThickness = 1;

        public const string DisplayTargetBitmapKey = "DefectOverview_DisplayTargetBitmap";
        public const string DisplayTargetSourceWidthKey = "DefectOverview_DisplayTargetSourceWidth";
        public const string DisplayTargetSourceHeightKey = "DefectOverview_DisplayTargetSourceHeight";
        public const string DisplayCenterXKey = "DefectOverview_DisplayCenterX";
        public const string DisplayCenterYKey = "DefectOverview_DisplayCenterY";
        public const string DisplayPixelWidthKey = "DefectOverview_DisplayPixelWidth";
        public const string DisplayPixelHeightKey = "DefectOverview_DisplayPixelHeight";
        public const string DisplayAngleKey = "DefectOverview_DisplayAngle";
        public const string MapCenterXKey = "DefectOverview_MapCenterX";
        public const string MapPixelWidthKey = "DefectOverview_MapPixelWidth";
        public const string MapXMirrorKey = "DefectOverview_MapXMirror";

        private const string LegacyDisplayTargetBitmapKey = "XYHD_DisplayTargetBitmap";
        private const string LegacyDisplayTargetSourceWidthKey = "XYHD_DisplayTargetSourceWidth";
        private const string LegacyDisplayTargetSourceHeightKey = "XYHD_DisplayTargetSourceHeight";
        private const string LegacyDisplayCenterXKey = "XYHD_DisplayCenterX";
        private const string LegacyDisplayCenterYKey = "XYHD_DisplayCenterY";
        private const string LegacyDisplayPixelWidthKey = "XYHD_DisplayPixelWidth";
        private const string LegacyDisplayPixelHeightKey = "XYHD_DisplayPixelHeight";

        public static BitmapSource CreateBitmapFromHImage(HImage hImage)
        {
            try
            {
                if (hImage == null || !hImage.IsInitialized())
                    return null;

                bool disposeNormalizedImage = false;
                HImage normalizedImage = NormalizeDisplayImage(hImage, out disposeNormalizedImage);
                if (normalizedImage == null || !normalizedImage.IsInitialized())
                    return null;

                try
                {
                    normalizedImage.GetImageSize(out int width, out int height);
                    int channels = normalizedImage.CountChannels();

                    return channels == 3
                        ? CreateBitmapFromRgb(normalizedImage, width, height)
                        : CreateBitmapFromGray(normalizedImage, width, height);
                }
                finally
                {
                    if (disposeNormalizedImage)
                        normalizedImage.Dispose();
                }
            }
            catch
            {
                return null;
            }
        }

        public static BitmapSource CreateDefectPreviewBitmap(
            BitmapSource baseBitmap,
            int sourceWidth,
            int sourceHeight,
            Result defect,
            double paddingScale,
            double targetAspectRatio = 1.0)
        {
            return CreateDefectPreviewBitmapCore(
                baseBitmap,
                sourceWidth,
                sourceHeight,
                defect,
                paddingScale,
                targetAspectRatio,
                null);
        }

        internal static BitmapSource CreateDefectPreviewBitmapCached(
            BitmapSource baseBitmap,
            int sourceWidth,
            int sourceHeight,
            Result defect,
            double paddingScale,
            double targetAspectRatio,
            Func<BitmapSource, PreviewPixelCache> pixelCacheResolver)
        {
            return CreateDefectPreviewBitmapCore(
                baseBitmap,
                sourceWidth,
                sourceHeight,
                defect,
                paddingScale,
                targetAspectRatio,
                pixelCacheResolver);
        }

        private static BitmapSource CreateDefectPreviewBitmapCore(
            BitmapSource baseBitmap,
            int sourceWidth,
            int sourceHeight,
            Result defect,
            double paddingScale,
            double targetAspectRatio,
            Func<BitmapSource, PreviewPixelCache> pixelCacheResolver)
        {
            try
            {
                if (defect == null)
                {
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine("[DefectPreviewFactory] Skip: defect is null");
                    }
                    return null;
                }

                bool usedMetadataBitmap = TryGetMetadataBitmap(defect, out BitmapSource metadataBitmap);
                if (usedMetadataBitmap)
                    baseBitmap = metadataBitmap;

                if (baseBitmap == null)
                {
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectPreviewFactory] Skip {DescribeDefect(defect)}: baseBitmap is null, metadataBitmap={usedMetadataBitmap}");
                    }
                    return null;
                }

                sourceWidth = ResolveMetadataInt(defect, sourceWidth > 0 ? sourceWidth : baseBitmap.PixelWidth, DisplayTargetSourceWidthKey, LegacyDisplayTargetSourceWidthKey);
                sourceHeight = ResolveMetadataInt(defect, sourceHeight > 0 ? sourceHeight : baseBitmap.PixelHeight, DisplayTargetSourceHeightKey, LegacyDisplayTargetSourceHeightKey);
                double scaleX = ResolveScale(baseBitmap.PixelWidth, sourceWidth);
                double scaleY = ResolveScale(baseBitmap.PixelHeight, sourceHeight);
                ResolvePreviewGeometry(
                    defect,
                    baseBitmap.PixelWidth,
                    baseBitmap.PixelHeight,
                    sourceWidth,
                    sourceHeight,
                    out double scaledCx,
                    out double scaledCy,
                    out double scaledWidth,
                    out double scaledHeight,
                    out double scaledAngle,
                    out bool usedFallbackGeometry,
                    out string geometrySource);
                bool hasDisplayGeometry = string.Equals(geometrySource, "display-metadata", StringComparison.Ordinal);

                ResolveRotatedBounds(
                    scaledWidth,
                    scaledHeight,
                    scaledAngle,
                    out double cropReferenceWidth,
                    out double cropReferenceHeight);

                double targetCropWidth = Math.Max(Math.Max(cropReferenceWidth * paddingScale, cropReferenceWidth + 40), MinPreviewCropSize);
                double targetCropHeight = Math.Max(Math.Max(cropReferenceHeight * paddingScale, cropReferenceHeight + 40), MinPreviewCropSize);
                targetAspectRatio = targetAspectRatio <= 0 ? 1.0 : targetAspectRatio;

                double actualAspect = targetCropHeight <= 0 ? targetAspectRatio : targetCropWidth / targetCropHeight;
                if (actualAspect < targetAspectRatio)
                    targetCropWidth = Math.Max(targetCropWidth, targetCropHeight * targetAspectRatio);
                else
                    targetCropHeight = Math.Max(targetCropHeight, targetCropWidth / targetAspectRatio);

                int cropW = (int)Math.Ceiling(Math.Min(Math.Max(1, baseBitmap.PixelWidth), targetCropWidth));
                int cropH = (int)Math.Ceiling(Math.Min(Math.Max(1, baseBitmap.PixelHeight), targetCropHeight));
                if (cropW <= 0 || cropH <= 0)
                {
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectPreviewFactory] Skip {DescribeDefect(defect)}: invalid crop size crop={cropW}x{cropH}, base={DescribeBitmap(baseBitmap)}");
                    }
                    return null;
                }

                double cropX = scaledCx - cropW / 2.0;
                double cropY = scaledCy - cropH / 2.0;
                bool geometryWithinBounds = IsGeometryWithinSourceBounds(
                    scaledCx,
                    scaledCy,
                    scaledWidth,
                    scaledHeight,
                    baseBitmap.PixelWidth,
                    baseBitmap.PixelHeight,
                    baseBitmap.PixelWidth,
                    baseBitmap.PixelHeight);

                PreviewPixelCache pixelCache = pixelCacheResolver?.Invoke(baseBitmap);
                BitmapSource previewBitmap = CreatePreviewBitmapFromPixels(
                    baseBitmap,
                    pixelCache,
                    cropW,
                    cropH,
                    cropX,
                    cropY,
                    scaledCx,
                    scaledCy,
                    scaledWidth,
                    scaledHeight,
                    scaledAngle,
                    defect,
                    scaleX,
                    scaleY,
                    sourceWidth,
                    sourceHeight,
                    hasDisplayGeometry,
                    out bool usedSegmentationOutline);
                if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                        $"[DefectPreviewFactory] Preview {DescribeDefect(defect)} padding={paddingScale:F2}, aspect={targetAspectRatio:F2}, metadataBitmap={usedMetadataBitmap}, geomSource={geometrySource}, fallbackGeom={usedFallbackGeometry}, inBounds={geometryWithinBounds}, overlay={(usedSegmentationOutline ? "seg" : (Math.Abs(scaledAngle) > 0.001 ? "rotated" : "axis"))}, base={DescribeBitmap(baseBitmap)}, source={sourceWidth}x{sourceHeight}, scaledGeom=({scaledCx:F1},{scaledCy:F1},{scaledWidth:F1},{scaledHeight:F1},{scaledAngle:F4}), crop=({cropX:F1},{cropY:F1},{cropW},{cropH}), result={DescribeBitmap(previewBitmap)}");
                }
                return LimitBitmapSize(previewBitmap, MaxPreviewCacheDimension);
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectPreviewFactory] Preview exception {DescribeDefect(defect)}: {ex.Message}");
                return null;
            }
        }

    }
}


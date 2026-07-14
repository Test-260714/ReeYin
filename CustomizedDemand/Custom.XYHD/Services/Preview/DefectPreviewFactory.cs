using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Services
{
    internal static partial class DefectPreviewFactory
    {
        private const int MaxDisplayWidth = 1024;
        private const int MaxDisplayHeight = 1024;
        private const int MaxPreviewCacheDimension = 360;
        private const double MinPreviewCropSize = 96.0;
        internal const string DisplayTargetBitmapKey = "XYHD_DisplayTargetBitmap";
        internal const string DisplayTargetSourceWidthKey = "XYHD_DisplayTargetSourceWidth";
        internal const string DisplayTargetSourceHeightKey = "XYHD_DisplayTargetSourceHeight";
        internal const string DisplayCenterXKey = "XYHD_DisplayCenterX";
        internal const string DisplayCenterYKey = "XYHD_DisplayCenterY";
        internal const string DisplayPixelWidthKey = "XYHD_DisplayPixelWidth";
        internal const string DisplayPixelHeightKey = "XYHD_DisplayPixelHeight";
        private const string OverviewDisplayTargetBitmapKey = "DefectOverview_DisplayTargetBitmap";
        private const string OverviewDisplayTargetSourceWidthKey = "DefectOverview_DisplayTargetSourceWidth";
        private const string OverviewDisplayTargetSourceHeightKey = "DefectOverview_DisplayTargetSourceHeight";
        private const string OverviewDisplayCenterXKey = "DefectOverview_DisplayCenterX";
        private const string OverviewDisplayCenterYKey = "DefectOverview_DisplayCenterY";
        private const string OverviewDisplayPixelWidthKey = "DefectOverview_DisplayPixelWidth";
        private const string OverviewDisplayPixelHeightKey = "DefectOverview_DisplayPixelHeight";

        public static BitmapSource CreateBitmapFromHImage(HImage hImage)
        {
            try
            {
                if (hImage == null || !hImage.IsInitialized())
                    return null;

                using HImage imageCopy = hImage.CopyImage();
                if (imageCopy == null || !imageCopy.IsInitialized())
                    return null;

                imageCopy.GetImageSize(out int width, out int height);
                int channels = imageCopy.CountChannels();

                return channels == 3
                    ? CreateBitmapFromRgb(imageCopy, width, height)
                    : CreateBitmapFromGray(imageCopy, width, height);
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
            try
            {
                if (defect == null)
                    return null;

                if (TryGetMetadataBitmap(defect, out var metadataBitmap))
                    baseBitmap = metadataBitmap;

                if (baseBitmap == null)
                    return null;

                sourceWidth = ResolveMetadataInt(
                    defect,
                    sourceWidth > 0 ? sourceWidth : baseBitmap.PixelWidth,
                    DisplayTargetSourceWidthKey,
                    OverviewDisplayTargetSourceWidthKey);
                sourceHeight = ResolveMetadataInt(
                    defect,
                    sourceHeight > 0 ? sourceHeight : baseBitmap.PixelHeight,
                    DisplayTargetSourceHeightKey,
                    OverviewDisplayTargetSourceHeightKey);
                var scaleX = ResolveScale(baseBitmap.PixelWidth, sourceWidth);
                var scaleY = ResolveScale(baseBitmap.PixelHeight, sourceHeight);
                ResolvePreviewGeometry(
                    defect,
                    baseBitmap.PixelWidth,
                    baseBitmap.PixelHeight,
                    sourceWidth,
                    sourceHeight,
                    out var scaledCx,
                    out var scaledCy,
                    out var scaledWidth,
                    out var scaledHeight);
                bool hasDisplayGeometry = TryResolveDisplayGeometryMetadata(defect, out _, out _, out _, out _);

                double targetCropWidth = Math.Max(Math.Max(scaledWidth * paddingScale, scaledWidth + 40), MinPreviewCropSize);
                double targetCropHeight = Math.Max(Math.Max(scaledHeight * paddingScale, scaledHeight + 40), MinPreviewCropSize);
                targetAspectRatio = targetAspectRatio <= 0 ? 1.0 : targetAspectRatio;

                double actualAspect = targetCropHeight <= 0 ? targetAspectRatio : targetCropWidth / targetCropHeight;
                if (actualAspect < targetAspectRatio)
                {
                    targetCropWidth = Math.Max(targetCropWidth, targetCropHeight * targetAspectRatio);
                }
                else
                {
                    targetCropHeight = Math.Max(targetCropHeight, targetCropWidth / targetAspectRatio);
                }

                int cropW = (int)Math.Ceiling(Math.Min(Math.Max(1, baseBitmap.PixelWidth), targetCropWidth));
                int cropH = (int)Math.Ceiling(Math.Min(Math.Max(1, baseBitmap.PixelHeight), targetCropHeight));
                if (cropW <= 0 || cropH <= 0)
                    return null;

                double cropX = scaledCx - cropW / 2.0;
                double cropY = scaledCy - cropH / 2.0;
                ClampCropOrigin(
                    ref cropX,
                    ref cropY,
                    cropW,
                    cropH,
                    baseBitmap.PixelWidth,
                    baseBitmap.PixelHeight);

                return LimitBitmapSize(CreatePreviewBitmapFromPixels(
                    baseBitmap,
                    cropW,
                    cropH,
                    cropX,
                    cropY,
                    scaledCx,
                    scaledCy,
                    scaledWidth,
                    scaledHeight,
                    defect,
                    scaleX,
                    scaleY,
                    sourceWidth,
                    sourceHeight,
                    hasDisplayGeometry), MaxPreviewCacheDimension);
            }
            catch
            {
                return null;
            }
        }
    }
}

using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Services
{
    internal static partial class DefectPreviewFactory
    {
        private static void ResolvePreviewGeometry(
            Result defect,
            int displayWidth,
            int displayHeight,
            int sourceWidth,
            int sourceHeight,
            out double scaledCx,
            out double scaledCy,
            out double scaledWidth,
            out double scaledHeight)
        {
            var scaleX = ResolveScale(displayWidth, sourceWidth);
            var scaleY = ResolveScale(displayHeight, sourceHeight);

            double cx;
            double cy;
            double width;
            double height;

            // Crop follows the bitmap shown by the wall. Seg may be in another coordinate basis,
            // so prefer explicit display geometry and use Seg as a fallback.
            int geometryBoundWidth = sourceWidth > 0 ? sourceWidth : displayWidth;
            int geometryBoundHeight = sourceHeight > 0 ? sourceHeight : displayHeight;

            if (!TryResolveDisplayGeometryMetadata(defect, out cx, out cy, out width, out height))
            {
                if (TryResolveGeometryFromSeg(defect, geometryBoundWidth, geometryBoundHeight, out var segCx, out var segCy, out var segWidth, out var segHeight))
                {
                    cx = segCx;
                    cy = segCy;
                    width = segWidth;
                    height = segHeight;
                }
                else
                {
                    cx = ResolveMetadataDouble(defect, defect?.Cx ?? 0.0, DisplayCenterXKey, OverviewDisplayCenterXKey);
                    cy = ResolveMetadataDouble(defect, defect?.Cy ?? 0.0, DisplayCenterYKey, OverviewDisplayCenterYKey);
                    width = ResolveMetadataDouble(defect, defect?.Width ?? 0.0, DisplayPixelWidthKey, OverviewDisplayPixelWidthKey);
                    height = ResolveMetadataDouble(defect, defect?.Height ?? 0.0, DisplayPixelHeightKey, OverviewDisplayPixelHeightKey);
                }
            }

            if (!HasUsableGeometry(cx, cy, width, height))
            {
                cx = geometryBoundWidth > 0 ? geometryBoundWidth / 2.0 : 0.0;
                cy = geometryBoundHeight > 0 ? geometryBoundHeight / 2.0 : 0.0;
                width = geometryBoundWidth > 0 ? Math.Min(geometryBoundWidth, MinPreviewCropSize) : MinPreviewCropSize;
                height = geometryBoundHeight > 0 ? Math.Min(geometryBoundHeight, MinPreviewCropSize) : MinPreviewCropSize;
            }
            else
            {
                if (geometryBoundWidth > 0)
                {
                    cx = Clamp(cx, 0.0, geometryBoundWidth);
                    width = Math.Min(width, geometryBoundWidth);
                }

                if (geometryBoundHeight > 0)
                {
                    cy = Clamp(cy, 0.0, geometryBoundHeight);
                    height = Math.Min(height, geometryBoundHeight);
                }
            }

            scaledCx = cx * scaleX;
            scaledCy = cy * scaleY;
            scaledWidth = Math.Max(1.0, width * scaleX);
            scaledHeight = Math.Max(1.0, height * scaleY);
        }

        private static bool TryResolveDisplayGeometryMetadata(
            Result defect,
            out double cx,
            out double cy,
            out double width,
            out double height)
        {
            cx = 0.0;
            cy = 0.0;
            width = 0.0;
            height = 0.0;

            if (!TryResolveMetadataDouble(defect, out cx, DisplayCenterXKey, OverviewDisplayCenterXKey)
                || !TryResolveMetadataDouble(defect, out cy, DisplayCenterYKey, OverviewDisplayCenterYKey))
            {
                return false;
            }

            if (!TryResolveMetadataDouble(defect, out width, DisplayPixelWidthKey, OverviewDisplayPixelWidthKey))
                width = defect?.Width ?? 0.0;

            if (!TryResolveMetadataDouble(defect, out height, DisplayPixelHeightKey, OverviewDisplayPixelHeightKey))
                height = defect?.Height ?? 0.0;

            return HasUsableGeometry(cx, cy, width, height);
        }

        private static bool TryResolveGeometryFromSeg(
            Result defect,
            int sourceCoordinateWidth,
            int sourceCoordinateHeight,
            out double cx,
            out double cy,
            out double width,
            out double height)
        {
            cx = 0;
            cy = 0;
            width = 0;
            height = 0;

            try
            {
                if (defect?.Seg == null || !defect.Seg.IsInitialized())
                    return false;

                using HObject unionRegion = UnionSegRegion(defect.Seg);
                HObject geometryRegion = unionRegion != null && unionRegion.IsInitialized()
                    ? unionRegion
                    : defect.Seg;
                ResolveSegCoordinateOffset(
                    geometryRegion,
                    sourceCoordinateWidth,
                    sourceCoordinateHeight,
                    out double coordinateOffsetX,
                    out double coordinateOffsetY);

                HOperatorSet.SmallestRectangle1(geometryRegion, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                if (row1.TupleLength() == 0 || col1.TupleLength() == 0 || row2.TupleLength() == 0 || col2.TupleLength() == 0)
                    return false;

                double left = col1[0].D - coordinateOffsetX;
                double top = row1[0].D - coordinateOffsetY;
                double right = col2[0].D - coordinateOffsetX;
                double bottom = row2[0].D - coordinateOffsetY;

                if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(right) || double.IsNaN(bottom))
                    return false;

                width = Math.Max(1.0, right - left + 1.0);
                height = Math.Max(1.0, bottom - top + 1.0);
                cx = left + width / 2.0;
                cy = top + height / 2.0;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetMetadataBitmap(Result defect, out BitmapSource bitmap)
        {
            bitmap = null;
            if (defect?.Others == null)
                return false;

            if (!defect.Others.TryGetValue(DisplayTargetBitmapKey, out var rawBitmap)
                && !defect.Others.TryGetValue(OverviewDisplayTargetBitmapKey, out rawBitmap))
                return false;

            bitmap = rawBitmap as BitmapSource;
            return bitmap != null;
        }

        private static int ResolveMetadataInt(Result defect, string key, int fallback)
            => ResolveMetadataInt(defect, fallback, key);

        private static int ResolveMetadataInt(Result defect, int fallback, params string[] keys)
        {
            if (defect?.Others == null || keys == null || keys.Length == 0)
                return fallback;

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || !defect.Others.TryGetValue(key, out var rawValue)
                    || rawValue == null)
                    continue;

                try
                {
                    return Convert.ToInt32(rawValue);
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static double ResolveMetadataDouble(Result defect, string key, double fallback)
            => ResolveMetadataDouble(defect, fallback, key);

        private static double ResolveMetadataDouble(Result defect, double fallback, params string[] keys)
        {
            if (defect?.Others == null || keys == null || keys.Length == 0)
                return fallback;

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || !defect.Others.TryGetValue(key, out var rawValue)
                    || rawValue == null)
                    continue;

                try
                {
                    double value = Convert.ToDouble(rawValue);
                    if (double.IsFinite(value))
                        return value;
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static bool TryResolveMetadataDouble(Result defect, out double value, params string[] keys)
        {
            value = 0.0;
            if (defect?.Others == null || keys == null || keys.Length == 0)
                return false;

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || !defect.Others.TryGetValue(key, out var rawValue)
                    || rawValue == null)
                    continue;

                try
                {
                    value = Convert.ToDouble(rawValue);
                    return double.IsFinite(value);
                }
                catch
                {
                    value = 0.0;
                }
            }

            return false;
        }

        private static bool HasMetadataValue(Result defect, params string[] keys)
        {
            if (defect?.Others == null || keys == null || keys.Length == 0)
                return false;

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || !defect.Others.TryGetValue(key, out var rawValue)
                    || rawValue == null)
                    continue;

                try
                {
                    double value = Convert.ToDouble(rawValue);
                    if (double.IsFinite(value))
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool HasUsableGeometry(double cx, double cy, double width, double height)
        {
            return double.IsFinite(cx)
                && double.IsFinite(cy)
                && double.IsFinite(width)
                && double.IsFinite(height)
                && width > 0.0
                && height > 0.0;
        }

        private static double ResolveScale(int displaySize, int sourceSize)
        {
            if (displaySize <= 0 || sourceSize <= 0)
                return 1.0;

            return (double)displaySize / sourceSize;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private static void ClampCropOrigin(
            ref double cropX,
            ref double cropY,
            int cropWidth,
            int cropHeight,
            int sourceWidth,
            int sourceHeight)
        {
            if (sourceWidth > 0 && cropWidth > 0 && cropWidth <= sourceWidth)
                cropX = Math.Clamp(cropX, 0.0, sourceWidth - cropWidth);

            if (sourceHeight > 0 && cropHeight > 0 && cropHeight <= sourceHeight)
                cropY = Math.Clamp(cropY, 0.0, sourceHeight - cropHeight);
        }
    }
}

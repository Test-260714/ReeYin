using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;

namespace Custom.XYHD.Services
{
    internal static partial class DefectPreviewFactory
    {
        private static bool TryDrawSegmentationBorder(
            byte[] pixels,
            int width,
            int height,
            Result defect,
            double scaleX,
            double scaleY,
            int sourceCoordinateWidth,
            int sourceCoordinateHeight,
            int cropLeft,
            int cropTop)
        {
            if (pixels == null
                || width <= 0
                || height <= 0
                || defect?.Seg == null
                || !defect.Seg.IsInitialized()
                || !double.IsFinite(scaleX)
                || !double.IsFinite(scaleY)
                || scaleX <= 0.0
                || scaleY <= 0.0)
                return false;

            HObject unionRegion = null;
            HObject contours = null;
            try
            {
                unionRegion = UnionSegRegion(defect.Seg);
                HObject geometryRegion = unionRegion != null && unionRegion.IsInitialized()
                    ? unionRegion
                    : defect.Seg;
                ResolveSegCoordinateOffset(
                    geometryRegion,
                    sourceCoordinateWidth,
                    sourceCoordinateHeight,
                    out double coordinateOffsetX,
                    out double coordinateOffsetY);

                HOperatorSet.GenContourRegionXld(geometryRegion, out contours, "border");
                if (contours == null || !contours.IsInitialized())
                    return false;

                HOperatorSet.CountObj(contours, out HTuple contourCountTuple);
                int contourCount = contourCountTuple.TupleLength() == 0
                    ? 0
                    : contourCountTuple[0].I;
                if (contourCount <= 0)
                    return false;

                int borderThickness = Math.Max(2, Math.Min(width, height) / 80);
                bool drewVisibleContour = false;
                for (int contourIndex = 1; contourIndex <= contourCount; contourIndex++)
                {
                    using HObject contour = contours.SelectObj(contourIndex);
                    if (contour == null || !contour.IsInitialized())
                        continue;

                    HOperatorSet.GetContourXld(contour, out HTuple rows, out HTuple cols);
                    int pointCount = Math.Min(rows.TupleLength(), cols.TupleLength());
                    if (pointCount < 2)
                        continue;

                    double firstX = (cols[0].D - coordinateOffsetX) * scaleX - cropLeft;
                    double firstY = (rows[0].D - coordinateOffsetY) * scaleY - cropTop;
                    double previousX = firstX;
                    double previousY = firstY;
                    for (int pointIndex = 1; pointIndex < pointCount; pointIndex++)
                    {
                        double currentX = (cols[pointIndex].D - coordinateOffsetX) * scaleX - cropLeft;
                        double currentY = (rows[pointIndex].D - coordinateOffsetY) * scaleY - cropTop;
                        if (SegmentMayTouchViewport(previousX, previousY, currentX, currentY, width, height))
                        {
                            DrawLine(pixels, width, height, previousX, previousY, currentX, currentY, borderThickness);
                            drewVisibleContour = true;
                        }

                        previousX = currentX;
                        previousY = currentY;
                    }

                    if (SegmentMayTouchViewport(previousX, previousY, firstX, firstY, width, height))
                    {
                        DrawLine(pixels, width, height, previousX, previousY, firstX, firstY, borderThickness);
                        drewVisibleContour = true;
                    }
                }

                return drewVisibleContour;
            }
            catch
            {
                return false;
            }
            finally
            {
                contours?.Dispose();
                unionRegion?.Dispose();
            }
        }

        private static HObject UnionSegRegion(HObject seg)
        {
            try
            {
                if (seg == null || !seg.IsInitialized())
                    return null;

                HOperatorSet.CountObj(seg, out HTuple count);
                if (count.TupleLength() == 0 || count[0].I <= 1)
                    return null;

                HOperatorSet.Union1(seg, out HObject unionRegion);
                return unionRegion;
            }
            catch
            {
                return null;
            }
        }

        private static bool SegmentMayTouchViewport(
            double x1,
            double y1,
            double x2,
            double y2,
            int width,
            int height)
        {
            if (width <= 0 || height <= 0)
                return false;

            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);
            double minY = Math.Min(y1, y2);
            double maxY = Math.Max(y1, y2);

            return maxX >= 0.0
                && maxY >= 0.0
                && minX < width
                && minY < height;
        }

        private static void ResolveSegCoordinateOffset(
            HObject geometryRegion,
            int sourceCoordinateWidth,
            int sourceCoordinateHeight,
            out double coordinateOffsetX,
            out double coordinateOffsetY)
        {
            coordinateOffsetX = 0.0;
            coordinateOffsetY = 0.0;

            if (geometryRegion == null || !geometryRegion.IsInitialized())
                return;

            try
            {
                HOperatorSet.SmallestRectangle1(geometryRegion, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                if (row1.TupleLength() == 0
                    || col1.TupleLength() == 0
                    || row2.TupleLength() == 0
                    || col2.TupleLength() == 0)
                    return;

                double top = row1[0].D;
                double left = col1[0].D;
                double bottom = row2[0].D;
                double right = col2[0].D;

                if (sourceCoordinateWidth > 1)
                {
                    double centerX = (left + right + 1.0) * 0.5;
                    if (double.IsFinite(centerX)
                        && centerX > sourceCoordinateWidth
                        && centerX <= sourceCoordinateWidth * 2.0 + 1.0)
                    {
                        coordinateOffsetX = sourceCoordinateWidth;
                    }
                }

                if (sourceCoordinateHeight > 1)
                {
                    double centerY = (top + bottom + 1.0) * 0.5;
                    if (double.IsFinite(centerY)
                        && centerY > sourceCoordinateHeight
                        && centerY <= sourceCoordinateHeight * 2.0 + 1.0)
                    {
                        coordinateOffsetY = sourceCoordinateHeight;
                    }
                }
            }
            catch
            {
                coordinateOffsetX = 0.0;
                coordinateOffsetY = 0.0;
            }
        }

        private static void DrawBorder(
            byte[] pixels,
            int width,
            int height,
            double left,
            double top,
            double rectWidth,
            double rectHeight)
        {
            if (pixels == null || width <= 0 || height <= 0 || rectWidth <= 0 || rectHeight <= 0)
                return;

            int borderThickness = Math.Max(2, Math.Min(width, height) / 80);
            int x1 = (int)Math.Floor(left);
            int y1 = (int)Math.Floor(top);
            int x2 = (int)Math.Ceiling(left + rectWidth) - 1;
            int y2 = (int)Math.Ceiling(top + rectHeight) - 1;

            if (x2 < 0 || y2 < 0 || x1 >= width || y1 >= height)
                return;

            x1 = Math.Max(0, x1);
            y1 = Math.Max(0, y1);
            x2 = Math.Min(width - 1, x2);
            y2 = Math.Min(height - 1, y2);

            for (int offset = 0; offset < borderThickness; offset++)
            {
                DrawHorizontalLine(pixels, width, height, x1, x2, y1 + offset);
                DrawHorizontalLine(pixels, width, height, x1, x2, y2 - offset);
                DrawVerticalLine(pixels, width, height, x1 + offset, y1, y2);
                DrawVerticalLine(pixels, width, height, x2 - offset, y1, y2);
            }
        }

        private static void DrawHorizontalLine(byte[] pixels, int width, int height, int x1, int x2, int y)
        {
            if (y < 0 || y >= height)
                return;

            int startX = Math.Max(0, Math.Min(x1, x2));
            int endX = Math.Min(width - 1, Math.Max(x1, x2));
            for (int x = startX; x <= endX; x++)
                SetRedPixel(pixels, width, x, y);
        }

        private static void DrawVerticalLine(byte[] pixels, int width, int height, int x, int y1, int y2)
        {
            if (x < 0 || x >= width)
                return;

            int startY = Math.Max(0, Math.Min(y1, y2));
            int endY = Math.Min(height - 1, Math.Max(y1, y2));
            for (int y = startY; y <= endY; y++)
                SetRedPixel(pixels, width, x, y);
        }

        private static void DrawLine(
            byte[] pixels,
            int width,
            int height,
            double x1,
            double y1,
            double x2,
            double y2,
            int thickness)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1))));
            for (int index = 0; index <= steps; index++)
            {
                double t = (double)index / steps;
                int x = (int)Math.Round(x1 + (x2 - x1) * t);
                int y = (int)Math.Round(y1 + (y2 - y1) * t);
                PaintRedPoint(pixels, width, height, x, y, thickness);
            }
        }

        private static void PaintRedPoint(byte[] pixels, int width, int height, int x, int y, int thickness)
        {
            int radius = Math.Max(0, thickness / 2);
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                int targetY = y + offsetY;
                if (targetY < 0 || targetY >= height)
                    continue;

                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    int targetX = x + offsetX;
                    if (targetX < 0 || targetX >= width)
                        continue;

                    SetRedPixel(pixels, width, targetX, targetY);
                }
            }
        }

        private static void SetRedPixel(byte[] pixels, int width, int x, int y)
        {
            int index = (y * width + x) * 4;
            if (index < 0 || index + 3 >= pixels.Length)
                return;

            pixels[index] = 0;
            pixels[index + 1] = 0;
            pixels[index + 2] = 255;
            pixels[index + 3] = 255;
        }
    }
}

using System;
using System.Collections.Generic;

namespace ALGO.ImagePerProcessing
{
    public enum ImagePerProcessingRoiPreviewHandle
    {
        None,
        Body,
        Center,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
        Rotate,
        Radius
    }

    public readonly record struct ImagePerProcessingRoiPreviewPoint(double X, double Y);

    public readonly record struct ImagePerProcessingRoiPreview(
        eCreateRoiType Mode,
        double CenterX,
        double CenterY,
        double Length1,
        double Length2,
        double Angle,
        double Radius);

    public static class ImagePerProcessingRoiPreviewGeometry
    {
        private const double MinSize = 1.0;

        public static ImagePerProcessingRoiPreview CreateDefault(eCreateRoiType mode, double imageWidth, double imageHeight)
        {
            double width = Math.Max(1.0, imageWidth);
            double height = Math.Max(1.0, imageHeight);
            double halfWidth = Math.Max(MinSize, width * 0.2);
            double halfHeight = Math.Max(MinSize, height * 0.2);
            double radius = Math.Max(MinSize, Math.Min(width, height) * 0.2);

            return Normalize(new ImagePerProcessingRoiPreview(
                mode,
                (width - 1.0) / 2.0,
                (height - 1.0) / 2.0,
                halfWidth,
                halfHeight,
                0.0,
                radius));
        }

        public static ImagePerProcessingRoiPreview Normalize(ImagePerProcessingRoiPreview roi)
        {
            return roi with
            {
                Length1 = Math.Max(MinSize, Math.Abs(roi.Length1)),
                Length2 = Math.Max(MinSize, Math.Abs(roi.Length2)),
                Radius = Math.Max(MinSize, Math.Abs(roi.Radius))
            };
        }

        public static ImagePerProcessingRoiPreview Move(ImagePerProcessingRoiPreview roi, double deltaX, double deltaY)
        {
            return Normalize(roi with
            {
                CenterX = roi.CenterX + deltaX,
                CenterY = roi.CenterY + deltaY
            });
        }

        public static ImagePerProcessingRoiPreview ResizeFromHandle(
            ImagePerProcessingRoiPreview roi,
            ImagePerProcessingRoiPreviewHandle handle,
            double pointerX,
            double pointerY)
        {
            if (roi.Mode == eCreateRoiType.圆形)
            {
                double radius = Distance(new ImagePerProcessingRoiPreviewPoint(roi.CenterX, roi.CenterY), pointerX, pointerY);
                return Normalize(roi with { Radius = radius });
            }

            ToLocal(roi, pointerX, pointerY, out double localX, out double localY);
            double length1 = roi.Length1;
            double length2 = roi.Length2;

            if (handle is ImagePerProcessingRoiPreviewHandle.Left
                or ImagePerProcessingRoiPreviewHandle.Right
                or ImagePerProcessingRoiPreviewHandle.TopLeft
                or ImagePerProcessingRoiPreviewHandle.TopRight
                or ImagePerProcessingRoiPreviewHandle.BottomRight
                or ImagePerProcessingRoiPreviewHandle.BottomLeft)
            {
                length1 = Math.Max(MinSize, Math.Abs(localX));
            }

            if (handle is ImagePerProcessingRoiPreviewHandle.Top
                or ImagePerProcessingRoiPreviewHandle.Bottom
                or ImagePerProcessingRoiPreviewHandle.TopLeft
                or ImagePerProcessingRoiPreviewHandle.TopRight
                or ImagePerProcessingRoiPreviewHandle.BottomRight
                or ImagePerProcessingRoiPreviewHandle.BottomLeft)
            {
                length2 = Math.Max(MinSize, Math.Abs(localY));
            }

            return Normalize(roi with
            {
                Length1 = length1,
                Length2 = length2
            });
        }

        public static ImagePerProcessingRoiPreview RotateToPoint(
            ImagePerProcessingRoiPreview roi,
            double pointerX,
            double pointerY)
        {
            if (roi.Mode != eCreateRoiType.矩形)
            {
                return Normalize(roi);
            }

            return Normalize(roi with
            {
                Angle = -Math.Atan2(pointerY - roi.CenterY, pointerX - roi.CenterX)
            });
        }

        public static ImagePerProcessingRoiPreviewHandle HitTest(
            ImagePerProcessingRoiPreview roi,
            double pointerX,
            double pointerY,
            double tolerance)
        {
            roi = Normalize(roi);
            foreach (var pair in GetHandlePoints(roi))
            {
                if (Distance(pair.Value, pointerX, pointerY) <= tolerance)
                {
                    return pair.Key;
                }
            }

            if (roi.Mode == eCreateRoiType.圆形)
            {
                return Distance(new ImagePerProcessingRoiPreviewPoint(roi.CenterX, roi.CenterY), pointerX, pointerY) <= roi.Radius
                    ? ImagePerProcessingRoiPreviewHandle.Body
                    : ImagePerProcessingRoiPreviewHandle.None;
            }

            ToLocal(roi, pointerX, pointerY, out double localX, out double localY);
            if (Math.Abs(localX) <= roi.Length1 && Math.Abs(localY) <= roi.Length2)
            {
                return ImagePerProcessingRoiPreviewHandle.Body;
            }

            return ImagePerProcessingRoiPreviewHandle.None;
        }

        public static IReadOnlyDictionary<ImagePerProcessingRoiPreviewHandle, ImagePerProcessingRoiPreviewPoint> GetHandlePoints(
            ImagePerProcessingRoiPreview roi)
        {
            roi = Normalize(roi);
            if (roi.Mode == eCreateRoiType.圆形)
            {
                return new Dictionary<ImagePerProcessingRoiPreviewHandle, ImagePerProcessingRoiPreviewPoint>
                {
                    [ImagePerProcessingRoiPreviewHandle.Center] = new(roi.CenterX, roi.CenterY),
                    [ImagePerProcessingRoiPreviewHandle.Radius] = new(roi.CenterX + roi.Radius, roi.CenterY)
                };
            }

            var corners = GetCorners(roi);
            var handles = new Dictionary<ImagePerProcessingRoiPreviewHandle, ImagePerProcessingRoiPreviewPoint>
            {
                [ImagePerProcessingRoiPreviewHandle.Center] = new(roi.CenterX, roi.CenterY),
                [ImagePerProcessingRoiPreviewHandle.TopLeft] = corners[0],
                [ImagePerProcessingRoiPreviewHandle.TopRight] = corners[1],
                [ImagePerProcessingRoiPreviewHandle.BottomRight] = corners[2],
                [ImagePerProcessingRoiPreviewHandle.BottomLeft] = corners[3],
                [ImagePerProcessingRoiPreviewHandle.Left] = new((corners[0].X + corners[3].X) / 2.0, (corners[0].Y + corners[3].Y) / 2.0),
                [ImagePerProcessingRoiPreviewHandle.Right] = new((corners[1].X + corners[2].X) / 2.0, (corners[1].Y + corners[2].Y) / 2.0),
                [ImagePerProcessingRoiPreviewHandle.Top] = new((corners[0].X + corners[1].X) / 2.0, (corners[0].Y + corners[1].Y) / 2.0),
                [ImagePerProcessingRoiPreviewHandle.Bottom] = new((corners[2].X + corners[3].X) / 2.0, (corners[2].Y + corners[3].Y) / 2.0)
            };

            var axes = GetAxes(roi);
            double rotateOffset = Math.Max(20.0, Math.Min(roi.Length1, roi.Length2) * 0.35);
            handles[ImagePerProcessingRoiPreviewHandle.Rotate] = new(
                roi.CenterX + axes.Axis1X + Math.Cos(roi.Angle) * rotateOffset,
                roi.CenterY + axes.Axis1Y - Math.Sin(roi.Angle) * rotateOffset);

            return handles;
        }

        public static ImagePerProcessingRoiPreviewPoint[] GetCorners(ImagePerProcessingRoiPreview roi)
        {
            roi = Normalize(roi);
            var axes = GetAxes(roi);
            return new[]
            {
                new ImagePerProcessingRoiPreviewPoint(roi.CenterX - axes.Axis1X - axes.Axis2X, roi.CenterY - axes.Axis1Y - axes.Axis2Y),
                new ImagePerProcessingRoiPreviewPoint(roi.CenterX + axes.Axis1X - axes.Axis2X, roi.CenterY + axes.Axis1Y - axes.Axis2Y),
                new ImagePerProcessingRoiPreviewPoint(roi.CenterX + axes.Axis1X + axes.Axis2X, roi.CenterY + axes.Axis1Y + axes.Axis2Y),
                new ImagePerProcessingRoiPreviewPoint(roi.CenterX - axes.Axis1X + axes.Axis2X, roi.CenterY - axes.Axis1Y + axes.Axis2Y)
            };
        }

        private static (double Axis1X, double Axis1Y, double Axis2X, double Axis2Y) GetAxes(ImagePerProcessingRoiPreview roi)
        {
            double cos = Math.Cos(roi.Angle);
            double sin = Math.Sin(roi.Angle);
            return (
                cos * roi.Length1,
                -sin * roi.Length1,
                sin * roi.Length2,
                cos * roi.Length2);
        }

        private static void ToLocal(
            ImagePerProcessingRoiPreview roi,
            double pointerX,
            double pointerY,
            out double localX,
            out double localY)
        {
            double dx = pointerX - roi.CenterX;
            double dy = pointerY - roi.CenterY;
            double cos = Math.Cos(roi.Angle);
            double sin = Math.Sin(roi.Angle);
            localX = dx * cos - dy * sin;
            localY = dx * sin + dy * cos;
        }

        private static double Distance(ImagePerProcessingRoiPreviewPoint point, double x, double y)
        {
            double dx = point.X - x;
            double dy = point.Y - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    internal static class ImagePerProcessingRoiPreviewStyle
    {
        public const double HitToleranceScreenPixels = 22.0;
        public const double HandleScreenSize = 11.0;
        public const double CenterHandleScreenSize = 14.0;
        public const double DirectionArrowHeadScreenSize = 11.0;
        public const string EditableRoiColor = "blue";
    }
}

using System;
using System.Collections.Generic;

namespace ALGO.MeasureRect
{
    #region 预览几何类型
    /// <summary>
    /// 矩形预览中可拖拽的命中目标。
    /// </summary>
    public enum MeasureRectPreviewHandle
    {
        /// <summary>未命中任何可编辑目标。</summary>
        None,

        /// <summary>命中矩形主体，可整体移动。</summary>
        Body,

        /// <summary>命中中心点手柄。</summary>
        Center,

        /// <summary>命中左侧边手柄。</summary>
        Left,

        /// <summary>命中右侧边手柄。</summary>
        Right,

        /// <summary>命中上侧边手柄。</summary>
        Top,

        /// <summary>命中下侧边手柄。</summary>
        Bottom,

        /// <summary>命中左上角手柄。</summary>
        TopLeft,

        /// <summary>命中右上角手柄。</summary>
        TopRight,

        /// <summary>命中右下角手柄。</summary>
        BottomRight,

        /// <summary>命中左下角手柄。</summary>
        BottomLeft,

        /// <summary>命中旋转手柄。</summary>
        Rotate
    }

    /// <summary>
    /// 矩形预览使用的二维点坐标。
    /// </summary>
    public readonly record struct MeasureRectPreviewPoint(double X, double Y);

    /// <summary>
    /// 矩形预览使用的中心点、半长宽和 HALCON 角度。
    /// </summary>
    public readonly record struct MeasureRectPreviewRect(
        double CenterX,
        double CenterY,
        double Length1,
        double Length2,
        double Angle);
    #endregion

    #region 命中测试与几何变换
    /// <summary>
    /// 提供矩形预览 ROI 的几何变换、命中测试和缩放辅助计算。
    /// </summary>
    public static class MeasureRectPreviewGeometry
    {
        private const double MinHalfLength = 1.0;

        /// <summary>
        /// 计算矩形四个角点的预览坐标。
        /// </summary>
        public static MeasureRectPreviewPoint[] GetCorners(MeasureRectPreviewRect rect)
        {
            var axes = GetAxes(rect);
            return new[]
            {
                new MeasureRectPreviewPoint(rect.CenterX - axes.Axis1X - axes.Axis2X, rect.CenterY - axes.Axis1Y - axes.Axis2Y),
                new MeasureRectPreviewPoint(rect.CenterX + axes.Axis1X - axes.Axis2X, rect.CenterY + axes.Axis1Y - axes.Axis2Y),
                new MeasureRectPreviewPoint(rect.CenterX + axes.Axis1X + axes.Axis2X, rect.CenterY + axes.Axis1Y + axes.Axis2Y),
                new MeasureRectPreviewPoint(rect.CenterX - axes.Axis1X + axes.Axis2X, rect.CenterY - axes.Axis1Y + axes.Axis2Y)
            };
        }

        /// <summary>
        /// 按指定偏移整体移动矩形。
        /// </summary>
        public static MeasureRectPreviewRect Move(MeasureRectPreviewRect rect, double deltaX, double deltaY)
        {
            return rect with
            {
                CenterX = rect.CenterX + deltaX,
                CenterY = rect.CenterY + deltaY
            };
        }

        /// <summary>
        /// 根据被拖拽的手柄和鼠标坐标调整矩形半长或半宽。
        /// </summary>
        public static MeasureRectPreviewRect ResizeFromHandle(
            MeasureRectPreviewRect rect,
            MeasureRectPreviewHandle handle,
            double pointerX,
            double pointerY)
        {
            ToLocal(rect, pointerX, pointerY, out double localX, out double localY);

            double length1 = rect.Length1;
            double length2 = rect.Length2;

            if (handle is MeasureRectPreviewHandle.Left
                or MeasureRectPreviewHandle.Right
                or MeasureRectPreviewHandle.TopLeft
                or MeasureRectPreviewHandle.TopRight
                or MeasureRectPreviewHandle.BottomRight
                or MeasureRectPreviewHandle.BottomLeft)
            {
                length1 = Math.Max(MinHalfLength, Math.Abs(localX));
            }

            if (handle is MeasureRectPreviewHandle.Top
                or MeasureRectPreviewHandle.Bottom
                or MeasureRectPreviewHandle.TopLeft
                or MeasureRectPreviewHandle.TopRight
                or MeasureRectPreviewHandle.BottomRight
                or MeasureRectPreviewHandle.BottomLeft)
            {
                length2 = Math.Max(MinHalfLength, Math.Abs(localY));
            }

            return rect with
            {
                Length1 = length1,
                Length2 = length2
            };
        }

        /// <summary>
        /// 根据鼠标位置计算 HALCON 角度，保证箭头、轮廓和卡尺同向旋转。
        /// </summary>
        public static MeasureRectPreviewRect RotateToPoint(MeasureRectPreviewRect rect, double pointerX, double pointerY)
        {
            return rect with
            {
                Angle = -Math.Atan2(pointerY - rect.CenterY, pointerX - rect.CenterX)
            };
        }

        /// <summary>
        /// 将预览缩放比例限制在允许范围内。
        /// </summary>
        public static double ClampZoom(double zoom)
        {
            return Math.Clamp(zoom, MeasureRectPreviewStyle.MinZoom, MeasureRectPreviewStyle.MaxZoom);
        }

        /// <summary>
        /// 根据鼠标滚轮增量计算单次缩放倍率。
        /// </summary>
        public static double GetWheelZoomFactor(int wheelDelta)
        {
            if (wheelDelta == 0)
            {
                return 1.0;
            }

            double notches = wheelDelta / 120.0;
            return Math.Pow(MeasureRectPreviewStyle.WheelZoomStep, notches);
        }

        /// <summary>
        /// 根据鼠标坐标和容差判断命中的矩形预览目标。
        /// </summary>
        public static MeasureRectPreviewHandle HitTest(
            MeasureRectPreviewRect rect,
            double pointerX,
            double pointerY,
            double tolerance)
        {
            var handles = GetHandlePoints(rect);
            foreach (var pair in handles)
            {
                if (Distance(pair.Value, pointerX, pointerY) <= tolerance)
                {
                    return pair.Key;
                }
            }

            ToLocal(rect, pointerX, pointerY, out double localX, out double localY);
            if (Math.Abs(localX) <= rect.Length1 && Math.Abs(localY) <= rect.Length2)
            {
                return MeasureRectPreviewHandle.Body;
            }

            return MeasureRectPreviewHandle.None;
        }

        /// <summary>
        /// 计算矩形各个编辑手柄的预览坐标。
        /// </summary>
        public static IReadOnlyDictionary<MeasureRectPreviewHandle, MeasureRectPreviewPoint> GetHandlePoints(
            MeasureRectPreviewRect rect)
        {
            var corners = GetCorners(rect);
            var axes = GetAxes(rect);
            var rotateOffset = Math.Max(20.0, Math.Min(rect.Length1, rect.Length2) * 0.35);
            var rotateX = rect.CenterX + axes.Axis1X + Math.Cos(rect.Angle) * rotateOffset;
            var rotateY = rect.CenterY + axes.Axis1Y - Math.Sin(rect.Angle) * rotateOffset;

            return new Dictionary<MeasureRectPreviewHandle, MeasureRectPreviewPoint>
            {
                [MeasureRectPreviewHandle.Center] = new(rect.CenterX, rect.CenterY),
                [MeasureRectPreviewHandle.TopLeft] = corners[0],
                [MeasureRectPreviewHandle.TopRight] = corners[1],
                [MeasureRectPreviewHandle.BottomRight] = corners[2],
                [MeasureRectPreviewHandle.BottomLeft] = corners[3],
                [MeasureRectPreviewHandle.Left] = new((corners[0].X + corners[3].X) / 2.0, (corners[0].Y + corners[3].Y) / 2.0),
                [MeasureRectPreviewHandle.Right] = new((corners[1].X + corners[2].X) / 2.0, (corners[1].Y + corners[2].Y) / 2.0),
                [MeasureRectPreviewHandle.Top] = new((corners[0].X + corners[1].X) / 2.0, (corners[0].Y + corners[1].Y) / 2.0),
                [MeasureRectPreviewHandle.Bottom] = new((corners[2].X + corners[3].X) / 2.0, (corners[2].Y + corners[3].Y) / 2.0),
                [MeasureRectPreviewHandle.Rotate] = new(rotateX, rotateY)
            };
        }

        /// <summary>
        /// 将 HALCON 角度投影到 Row 向下的图像显示坐标。
        /// </summary>
        private static (double Axis1X, double Axis1Y, double Axis2X, double Axis2Y) GetAxes(MeasureRectPreviewRect rect)
        {
            double cos = Math.Cos(rect.Angle);
            double sin = Math.Sin(rect.Angle);
            return (
                cos * rect.Length1,
                -sin * rect.Length1,
                sin * rect.Length2,
                cos * rect.Length2);
        }

        /// <summary>
        /// 按 HALCON 角度将全局预览坐标转换为矩形局部坐标。
        /// </summary>
        private static void ToLocal(MeasureRectPreviewRect rect, double pointerX, double pointerY, out double localX, out double localY)
        {
            double dx = pointerX - rect.CenterX;
            double dy = pointerY - rect.CenterY;
            double cos = Math.Cos(rect.Angle);
            double sin = Math.Sin(rect.Angle);
            localX = dx * cos - dy * sin;
            localY = dx * sin + dy * cos;
        }

        /// <summary>
        /// 计算预览点到指定坐标的欧氏距离。
        /// </summary>
        private static double Distance(MeasureRectPreviewPoint point, double x, double y)
        {
            double dx = point.X - x;
            double dy = point.Y - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
    #endregion

    #region 标定矩形计算
    /// <summary>
    /// 提供矩形四角世界坐标到半长、半宽和角度的换算方法。
    /// </summary>
    public static class RectangleCalibrationMath
    {
        /// <summary>
        /// 根据四个世界坐标角点计算矩形的半长、半宽和角度。
        /// </summary>
        public static bool TryCalculateWorldRectangle(
            IReadOnlyList<(double X, double Y)> worldCorners,
            out double length1,
            out double length2,
            out double angle)
        {
            length1 = 0d;
            length2 = 0d;
            angle = 0d;

            if (worldCorners == null || worldCorners.Count != 4)
            {
                return false;
            }

            double side01 = Distance(worldCorners[0], worldCorners[1]);
            double side12 = Distance(worldCorners[1], worldCorners[2]);
            double side23 = Distance(worldCorners[2], worldCorners[3]);
            double side30 = Distance(worldCorners[3], worldCorners[0]);

            if (!IsValidLength(side01) || !IsValidLength(side12) ||
                !IsValidLength(side23) || !IsValidLength(side30))
            {
                return false;
            }

            double firstAxis = (side01 + side23) / 2d;
            double secondAxis = (side12 + side30) / 2d;

            if (firstAxis >= secondAxis)
            {
                length1 = firstAxis / 2d;
                length2 = secondAxis / 2d;
                angle = Math.Atan2(worldCorners[1].Y - worldCorners[0].Y, worldCorners[1].X - worldCorners[0].X);
            }
            else
            {
                length1 = secondAxis / 2d;
                length2 = firstAxis / 2d;
                angle = Math.Atan2(worldCorners[2].Y - worldCorners[1].Y, worldCorners[2].X - worldCorners[1].X);
            }

            return IsValidLength(length1) && IsValidLength(length2);
        }

        /// <summary>
        /// 计算两个世界坐标点之间的欧氏距离。
        /// </summary>
        private static double Distance((double X, double Y) a, (double X, double Y) b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2d) + Math.Pow(a.Y - b.Y, 2d));
        }

        /// <summary>
        /// 判断长度是否为有限且大于零的有效值。
        /// </summary>
        private static bool IsValidLength(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;
        }
    }
    #endregion

    #region 预览样式常量
    /// <summary>
    /// 矩形测量 WPF 预览使用的显示和交互样式常量。
    /// </summary>
    internal static class MeasureRectPreviewStyle
    {
        /// <summary>命中测试的屏幕像素容差。</summary>
        public const double HitToleranceScreenPixels = 22.0;

        /// <summary>边角手柄的屏幕像素尺寸。</summary>
        public const double HandleScreenSize = 11.0;

        /// <summary>中心手柄的屏幕像素尺寸。</summary>
        public const double CenterHandleScreenSize = 14.0;

        /// <summary>可编辑矩形卡尺的屏幕像素线宽。</summary>
        public const double CaliperLineScreenThickness = 3.0;

        /// <summary>方向箭头主体的最大屏幕像素长度。</summary>
        public const double DirectionArrowScreenLength = 48.0;

        /// <summary>方向箭头头部的屏幕像素尺寸。</summary>
        public const double DirectionArrowHeadScreenSize = 11.0;

        /// <summary>旋转辅助线的屏幕像素线宽。</summary>
        public const double RotateLineScreenThickness = 2.0;

        /// <summary>手柄描边的屏幕像素线宽。</summary>
        public const double HandleStrokeScreenThickness = 1.25;

        /// <summary>预览最小缩放比例。</summary>
        public const double MinZoom = 0.1;

        /// <summary>预览最大缩放比例。</summary>
        public const double MaxZoom = 12.0;

        /// <summary>鼠标滚轮单档缩放倍率。</summary>
        public const double WheelZoomStep = 1.2;
    }
    #endregion
}

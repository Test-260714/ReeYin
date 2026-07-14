using System;
using System.Collections.Generic;

namespace ALGO.ShapeMatching
{
    #region ROI 预览几何类型

    /// <summary>
    /// 形状匹配 ROI 预览中可拖拽的命中目标。
    /// </summary>
    public enum ShapeMatchingRoiPreviewHandle
    {
        /// <summary>未命中任何可编辑目标。</summary>
        None,

        /// <summary>命中 ROI 主体，可整体移动。</summary>
        Body,

        /// <summary>命中 ROI 中心点。</summary>
        Center,

        /// <summary>命中左侧缩放手柄。</summary>
        Left,

        /// <summary>命中右侧缩放手柄。</summary>
        Right,

        /// <summary>命中上侧缩放手柄。</summary>
        Top,

        /// <summary>命中下侧缩放手柄。</summary>
        Bottom,

        /// <summary>命中左上角缩放手柄。</summary>
        TopLeft,

        /// <summary>命中右上角缩放手柄。</summary>
        TopRight,

        /// <summary>命中右下角缩放手柄。</summary>
        BottomRight,

        /// <summary>命中左下角缩放手柄。</summary>
        BottomLeft,

        /// <summary>命中旋转矩形的旋转手柄。</summary>
        Rotate,

        /// <summary>命中圆形 ROI 的半径手柄。</summary>
        Radius
    }

    /// <summary>
    /// 形状匹配 ROI 预览使用的图像坐标点。
    /// </summary>
    public readonly record struct ShapeMatchingRoiPreviewPoint(double X, double Y);

    /// <summary>
    /// 形状匹配 ROI 预览状态，坐标均为 HALCON 图像坐标。
    /// </summary>
    public readonly record struct ShapeMatchingRoiPreview(
        RegionCreatMode Mode,
        double CenterX,
        double CenterY,
        double Length1,
        double Length2,
        double Angle,
        double Radius);

    #endregion

    #region ROI 命中测试与几何变换

    /// <summary>
    /// 提供形状匹配可编辑 ROI 的默认值、命中测试和拖拽几何计算。
    /// </summary>
    public static class ShapeMatchingRoiPreviewGeometry
    {
        /// <summary>ROI 半长、半宽和半径允许的最小图像像素尺寸。</summary>
        private const double MinSize = 1.0;

        /// <summary>
        /// 根据当前图像尺寸创建默认 ROI，覆盖绘制矩形、绘制旋转矩形和绘制圆形三种模式。
        /// </summary>
        /// <param name="mode">界面选择的 ROI 获取方式。</param>
        /// <param name="imageWidth">输入图像宽度，单位为像素。</param>
        /// <param name="imageHeight">输入图像高度，单位为像素。</param>
        /// <returns>位于图像中心的默认 ROI 预览状态。</returns>
        public static ShapeMatchingRoiPreview CreateDefault(RegionCreatMode mode, double imageWidth, double imageHeight)
        {
            mode = mode switch
            {
                RegionCreatMode.绘制矩形 => RegionCreatMode.绘制矩形,
                RegionCreatMode.绘制旋转矩形 => RegionCreatMode.绘制旋转矩形,
                RegionCreatMode.绘制圆形 => RegionCreatMode.绘制圆形,
                _ => RegionCreatMode.绘制矩形
            };

            double width = Math.Max(1.0, imageWidth);
            double height = Math.Max(1.0, imageHeight);
            double halfWidth = Math.Max(MinSize, width * 0.2);
            double halfHeight = Math.Max(MinSize, height * 0.2);
            double radius = Math.Max(MinSize, Math.Min(width, height) * 0.2);

            return Normalize(new ShapeMatchingRoiPreview(
                mode,
                (width - 1.0) / 2.0,
                (height - 1.0) / 2.0,
                halfWidth,
                halfHeight,
                0.0,
                radius));
        }

        /// <summary>
        /// 修正 ROI 尺寸和角度，避免拖拽后出现无效区域。
        /// </summary>
        /// <param name="roi">需要修正的 ROI 预览状态。</param>
        /// <returns>修正后的 ROI 预览状态。</returns>
        public static ShapeMatchingRoiPreview Normalize(ShapeMatchingRoiPreview roi)
        {
            return roi with
            {
                Length1 = Math.Max(MinSize, Math.Abs(roi.Length1)),
                Length2 = Math.Max(MinSize, Math.Abs(roi.Length2)),
                Radius = Math.Max(MinSize, Math.Abs(roi.Radius)),
                Angle = roi.Mode == RegionCreatMode.绘制旋转矩形 ? roi.Angle : 0.0
            };
        }

        /// <summary>
        /// 按指定偏移整体移动 ROI。
        /// </summary>
        /// <param name="roi">拖拽开始时的 ROI 状态。</param>
        /// <param name="deltaX">图像列方向偏移。</param>
        /// <param name="deltaY">图像行方向偏移。</param>
        /// <returns>移动后的 ROI 状态。</returns>
        public static ShapeMatchingRoiPreview Move(ShapeMatchingRoiPreview roi, double deltaX, double deltaY)
        {
            return Normalize(roi with
            {
                CenterX = roi.CenterX + deltaX,
                CenterY = roi.CenterY + deltaY
            });
        }

        /// <summary>
        /// 根据被拖拽手柄和鼠标图像坐标调整 ROI 尺寸。
        /// </summary>
        /// <param name="roi">拖拽开始时的 ROI 状态。</param>
        /// <param name="handle">当前拖拽的命中目标。</param>
        /// <param name="pointerX">鼠标列坐标。</param>
        /// <param name="pointerY">鼠标行坐标。</param>
        /// <returns>缩放后的 ROI 状态。</returns>
        public static ShapeMatchingRoiPreview ResizeFromHandle(
            ShapeMatchingRoiPreview roi,
            ShapeMatchingRoiPreviewHandle handle,
            double pointerX,
            double pointerY)
        {
            if (roi.Mode == RegionCreatMode.绘制圆形)
            {
                double radius = Distance(new ShapeMatchingRoiPreviewPoint(roi.CenterX, roi.CenterY), pointerX, pointerY);
                return Normalize(roi with { Radius = radius });
            }

            ToLocal(roi, pointerX, pointerY, out double localX, out double localY);
            double length1 = roi.Length1;
            double length2 = roi.Length2;

            if (handle is ShapeMatchingRoiPreviewHandle.Left
                or ShapeMatchingRoiPreviewHandle.Right
                or ShapeMatchingRoiPreviewHandle.TopLeft
                or ShapeMatchingRoiPreviewHandle.TopRight
                or ShapeMatchingRoiPreviewHandle.BottomRight
                or ShapeMatchingRoiPreviewHandle.BottomLeft)
            {
                length1 = Math.Max(MinSize, Math.Abs(localX));
            }

            if (handle is ShapeMatchingRoiPreviewHandle.Top
                or ShapeMatchingRoiPreviewHandle.Bottom
                or ShapeMatchingRoiPreviewHandle.TopLeft
                or ShapeMatchingRoiPreviewHandle.TopRight
                or ShapeMatchingRoiPreviewHandle.BottomRight
                or ShapeMatchingRoiPreviewHandle.BottomLeft)
            {
                length2 = Math.Max(MinSize, Math.Abs(localY));
            }

            return Normalize(roi with
            {
                Length1 = length1,
                Length2 = length2
            });
        }

        /// <summary>
        /// 根据鼠标图像坐标调整旋转矩形角度。
        /// </summary>
        /// <param name="roi">拖拽开始时的 ROI 状态。</param>
        /// <param name="pointerX">鼠标列坐标。</param>
        /// <param name="pointerY">鼠标行坐标。</param>
        /// <returns>旋转后的 ROI 状态。</returns>
        public static ShapeMatchingRoiPreview RotateToPoint(
            ShapeMatchingRoiPreview roi,
            double pointerX,
            double pointerY)
        {
            if (roi.Mode != RegionCreatMode.绘制旋转矩形)
            {
                return Normalize(roi);
            }

            return Normalize(roi with
            {
                Angle = -Math.Atan2(pointerY - roi.CenterY, pointerX - roi.CenterX)
            });
        }

        /// <summary>
        /// 根据鼠标图像坐标和命中容差判断当前拖拽目标。
        /// </summary>
        /// <param name="roi">当前 ROI 预览状态。</param>
        /// <param name="pointerX">鼠标列坐标。</param>
        /// <param name="pointerY">鼠标行坐标。</param>
        /// <param name="tolerance">命中容差，单位为图像像素。</param>
        /// <returns>命中的 ROI 手柄或主体。</returns>
        public static ShapeMatchingRoiPreviewHandle HitTest(
            ShapeMatchingRoiPreview roi,
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

            if (roi.Mode == RegionCreatMode.绘制圆形)
            {
                return Distance(new ShapeMatchingRoiPreviewPoint(roi.CenterX, roi.CenterY), pointerX, pointerY) <= roi.Radius
                    ? ShapeMatchingRoiPreviewHandle.Body
                    : ShapeMatchingRoiPreviewHandle.None;
            }

            ToLocal(roi, pointerX, pointerY, out double localX, out double localY);
            if (Math.Abs(localX) <= roi.Length1 && Math.Abs(localY) <= roi.Length2)
            {
                return ShapeMatchingRoiPreviewHandle.Body;
            }

            return ShapeMatchingRoiPreviewHandle.None;
        }

        /// <summary>
        /// 获取当前 ROI 的可拖拽手柄位置。
        /// </summary>
        /// <param name="roi">当前 ROI 预览状态。</param>
        /// <returns>手柄类型到图像坐标点的映射。</returns>
        public static IReadOnlyDictionary<ShapeMatchingRoiPreviewHandle, ShapeMatchingRoiPreviewPoint> GetHandlePoints(
            ShapeMatchingRoiPreview roi)
        {
            roi = Normalize(roi);
            if (roi.Mode == RegionCreatMode.绘制圆形)
            {
                return new Dictionary<ShapeMatchingRoiPreviewHandle, ShapeMatchingRoiPreviewPoint>
                {
                    [ShapeMatchingRoiPreviewHandle.Center] = new(roi.CenterX, roi.CenterY),
                    [ShapeMatchingRoiPreviewHandle.Radius] = new(roi.CenterX + roi.Radius, roi.CenterY)
                };
            }

            var corners = GetCorners(roi);
            var handles = new Dictionary<ShapeMatchingRoiPreviewHandle, ShapeMatchingRoiPreviewPoint>
            {
                [ShapeMatchingRoiPreviewHandle.Center] = new(roi.CenterX, roi.CenterY),
                [ShapeMatchingRoiPreviewHandle.TopLeft] = corners[0],
                [ShapeMatchingRoiPreviewHandle.TopRight] = corners[1],
                [ShapeMatchingRoiPreviewHandle.BottomRight] = corners[2],
                [ShapeMatchingRoiPreviewHandle.BottomLeft] = corners[3],
                [ShapeMatchingRoiPreviewHandle.Left] = new((corners[0].X + corners[3].X) / 2.0, (corners[0].Y + corners[3].Y) / 2.0),
                [ShapeMatchingRoiPreviewHandle.Right] = new((corners[1].X + corners[2].X) / 2.0, (corners[1].Y + corners[2].Y) / 2.0),
                [ShapeMatchingRoiPreviewHandle.Top] = new((corners[0].X + corners[1].X) / 2.0, (corners[0].Y + corners[1].Y) / 2.0),
                [ShapeMatchingRoiPreviewHandle.Bottom] = new((corners[2].X + corners[3].X) / 2.0, (corners[2].Y + corners[3].Y) / 2.0)
            };

            if (roi.Mode == RegionCreatMode.绘制旋转矩形)
            {
                var axes = GetAxes(roi);
                double rotateOffset = Math.Max(20.0, Math.Min(roi.Length1, roi.Length2) * 0.35);
                handles[ShapeMatchingRoiPreviewHandle.Rotate] = new(
                    roi.CenterX + axes.Axis1X + Math.Cos(roi.Angle) * rotateOffset,
                    roi.CenterY + axes.Axis1Y - Math.Sin(roi.Angle) * rotateOffset);
            }

            return handles;
        }

        /// <summary>
        /// 计算矩形 ROI 的四个角点图像坐标。
        /// </summary>
        /// <param name="roi">矩形或旋转矩形 ROI。</param>
        /// <returns>从左上角开始顺时针排列的四个角点。</returns>
        public static ShapeMatchingRoiPreviewPoint[] GetCorners(ShapeMatchingRoiPreview roi)
        {
            roi = Normalize(roi);
            var axes = GetAxes(roi);
            return new[]
            {
                new ShapeMatchingRoiPreviewPoint(roi.CenterX - axes.Axis1X - axes.Axis2X, roi.CenterY - axes.Axis1Y - axes.Axis2Y),
                new ShapeMatchingRoiPreviewPoint(roi.CenterX + axes.Axis1X - axes.Axis2X, roi.CenterY + axes.Axis1Y - axes.Axis2Y),
                new ShapeMatchingRoiPreviewPoint(roi.CenterX + axes.Axis1X + axes.Axis2X, roi.CenterY + axes.Axis1Y + axes.Axis2Y),
                new ShapeMatchingRoiPreviewPoint(roi.CenterX - axes.Axis1X + axes.Axis2X, roi.CenterY - axes.Axis1Y + axes.Axis2Y)
            };
        }

        /// <summary>
        /// 将 ROI 的 HALCON 角度投影到 Row 向下的图像显示坐标。
        /// </summary>
        /// <param name="roi">矩形或旋转矩形 ROI。</param>
        /// <returns>长轴和短轴在图像显示坐标中的向量。</returns>
        private static (double Axis1X, double Axis1Y, double Axis2X, double Axis2Y) GetAxes(ShapeMatchingRoiPreview roi)
        {
            double cos = Math.Cos(roi.Angle);
            double sin = Math.Sin(roi.Angle);
            return (
                cos * roi.Length1,
                -sin * roi.Length1,
                sin * roi.Length2,
                cos * roi.Length2);
        }

        /// <summary>
        /// 按 ROI 角度将全局图像坐标转换为 ROI 局部坐标。
        /// </summary>
        /// <param name="roi">矩形或旋转矩形 ROI。</param>
        /// <param name="pointerX">鼠标列坐标。</param>
        /// <param name="pointerY">鼠标行坐标。</param>
        /// <param name="localX">输出的局部长轴方向坐标。</param>
        /// <param name="localY">输出的局部短轴方向坐标。</param>
        private static void ToLocal(
            ShapeMatchingRoiPreview roi,
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

        /// <summary>
        /// 计算图像点到指定坐标的欧氏距离。
        /// </summary>
        /// <param name="point">图像点。</param>
        /// <param name="x">目标列坐标。</param>
        /// <param name="y">目标行坐标。</param>
        /// <returns>两点距离。</returns>
        private static double Distance(ShapeMatchingRoiPreviewPoint point, double x, double y)
        {
            double dx = point.X - x;
            double dy = point.Y - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    #endregion

    #region ROI 预览样式

    /// <summary>
    /// 形状匹配 ROI 预览使用的显示和交互样式常量。
    /// </summary>
    internal static class ShapeMatchingRoiPreviewStyle
    {
        /// <summary>命中测试的屏幕像素容差。</summary>
        public const double HitToleranceScreenPixels = 22.0;

        /// <summary>普通缩放手柄的屏幕像素尺寸。</summary>
        public const double HandleScreenSize = 11.0;

        /// <summary>中心手柄的屏幕像素尺寸。</summary>
        public const double CenterHandleScreenSize = 14.0;

        /// <summary>方向箭头头部的屏幕像素尺寸。</summary>
        public const double DirectionArrowHeadScreenSize = 11.0;

        /// <summary>可编辑 ROI 覆盖层颜色。</summary>
        public const string EditableRoiColor = "blue";
    }

    #endregion
}

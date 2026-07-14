using System;

namespace ALGO.MeasureLine
{
    #region 预览几何类型
    /// <summary>直线预览中可拖拽的命中目标。</summary>
    public enum MeasureLinePreviewHandle
    {
        None,
        Body,
        Start,
        End
    }

    /// <summary>直线预览使用的二维点坐标。</summary>
    public readonly record struct MeasureLinePreviewPoint(double X, double Y);

    /// <summary>直线预览使用的起点和终点坐标。</summary>
    public readonly record struct MeasureLinePreviewLine(double StartX, double StartY, double EndX, double EndY);
    #endregion

    #region 命中测试与几何变换
    public static class MeasureLinePreviewGeometry
    {
        public static MeasureLinePreviewLine Move(MeasureLinePreviewLine line, double deltaX, double deltaY)
        {
            return line with
            {
                StartX = line.StartX + deltaX,
                StartY = line.StartY + deltaY,
                EndX = line.EndX + deltaX,
                EndY = line.EndY + deltaY
            };
        }

        public static MeasureLinePreviewLine MoveStart(MeasureLinePreviewLine line, double x, double y)
        {
            return line with { StartX = x, StartY = y };
        }

        public static MeasureLinePreviewLine MoveEnd(MeasureLinePreviewLine line, double x, double y)
        {
            return line with { EndX = x, EndY = y };
        }

        public static MeasureLinePreviewHandle HitTest(MeasureLinePreviewLine line, double pointerX, double pointerY, double tolerance)
        {
            if (Distance(line.StartX, line.StartY, pointerX, pointerY) <= tolerance)
                return MeasureLinePreviewHandle.Start;

            if (Distance(line.EndX, line.EndY, pointerX, pointerY) <= tolerance)
                return MeasureLinePreviewHandle.End;

            if (DistanceToSegment(line, pointerX, pointerY) <= tolerance)
                return MeasureLinePreviewHandle.Body;

            return MeasureLinePreviewHandle.None;
        }

        private static double DistanceToSegment(MeasureLinePreviewLine line, double x, double y)
        {
            double dx = line.EndX - line.StartX;
            double dy = line.EndY - line.StartY;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= double.Epsilon)
                return Distance(line.StartX, line.StartY, x, y);

            double t = ((x - line.StartX) * dx + (y - line.StartY) * dy) / lengthSquared;
            t = Math.Clamp(t, 0.0, 1.0);
            return Distance(line.StartX + t * dx, line.StartY + t * dy, x, y);
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
    #endregion

    #region 预览样式常量
    internal static class MeasureLinePreviewStyle
    {
        public const double HitToleranceScreenPixels = 10.0;
        public const double HandleScreenSize = 9.0;
    }
    #endregion
}

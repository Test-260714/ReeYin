using System;
using System.Collections.Generic;
using System.Linq;

namespace ALGO.DistanceLL
{
    #region 坐标线段模型
    /// <summary>线线距离使用的计算线段，X 对应列坐标，Y 对应行坐标或公共坐标。</summary>
    public readonly record struct DistanceLLSourceLine(double StartX, double StartY, double EndX, double EndY)
    {
        public bool IsValid()
        {
            return IsFinite(StartX)
                && IsFinite(StartY)
                && IsFinite(EndX)
                && IsFinite(EndY)
                && Distance(StartX, StartY, EndX, EndY) > DistanceLLGeometry.Epsilon;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>坐标画布中已经归一化后的显示线段。</summary>
    public sealed class DistanceLLPreviewLine
    {
        public DistanceLLPreviewLine(string name, double startX, double startY, double endX, double endY, string color)
        {
            Name = name;
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            Color = color;
        }

        public string Name { get; }
        public double StartX { get; }
        public double StartY { get; }
        public double EndX { get; }
        public double EndY { get; }
        public string Color { get; }
    }
    #endregion

    #region 距离计算
    public static class DistanceLLGeometry
    {
        public const double Epsilon = 0.0000001d;

        public static bool TryCalculateSegmentDistance(DistanceLLSourceLine lineA, DistanceLLSourceLine lineB, out double distance)
        {
            distance = -1d;
            if (!lineA.IsValid() || !lineB.IsValid())
                return false;

            if (SegmentsIntersect(lineA, lineB))
            {
                distance = 0d;
                return true;
            }

            distance = new[]
            {
                DistancePointToSegment(lineA.StartX, lineA.StartY, lineB),
                DistancePointToSegment(lineA.EndX, lineA.EndY, lineB),
                DistancePointToSegment(lineB.StartX, lineB.StartY, lineA),
                DistancePointToSegment(lineB.EndX, lineB.EndY, lineA),
            }.Min();
            return true;
        }

        private static bool SegmentsIntersect(DistanceLLSourceLine a, DistanceLLSourceLine b)
        {
            double d1 = Direction(a.StartX, a.StartY, a.EndX, a.EndY, b.StartX, b.StartY);
            double d2 = Direction(a.StartX, a.StartY, a.EndX, a.EndY, b.EndX, b.EndY);
            double d3 = Direction(b.StartX, b.StartY, b.EndX, b.EndY, a.StartX, a.StartY);
            double d4 = Direction(b.StartX, b.StartY, b.EndX, b.EndY, a.EndX, a.EndY);

            if (((d1 > Epsilon && d2 < -Epsilon) || (d1 < -Epsilon && d2 > Epsilon))
                && ((d3 > Epsilon && d4 < -Epsilon) || (d3 < -Epsilon && d4 > Epsilon)))
                return true;

            return Math.Abs(d1) <= Epsilon && IsPointOnSegment(b.StartX, b.StartY, a)
                || Math.Abs(d2) <= Epsilon && IsPointOnSegment(b.EndX, b.EndY, a)
                || Math.Abs(d3) <= Epsilon && IsPointOnSegment(a.StartX, a.StartY, b)
                || Math.Abs(d4) <= Epsilon && IsPointOnSegment(a.EndX, a.EndY, b);
        }

        private static double Direction(double startX, double startY, double endX, double endY, double pointX, double pointY)
        {
            return (pointX - startX) * (endY - startY) - (pointY - startY) * (endX - startX);
        }

        private static bool IsPointOnSegment(double x, double y, DistanceLLSourceLine line)
        {
            return x >= Math.Min(line.StartX, line.EndX) - Epsilon
                && x <= Math.Max(line.StartX, line.EndX) + Epsilon
                && y >= Math.Min(line.StartY, line.EndY) - Epsilon
                && y <= Math.Max(line.StartY, line.EndY) + Epsilon;
        }

        private static double DistancePointToSegment(double x, double y, DistanceLLSourceLine line)
        {
            double dx = line.EndX - line.StartX;
            double dy = line.EndY - line.StartY;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= Epsilon)
                return Distance(x, y, line.StartX, line.StartY);

            double t = ((x - line.StartX) * dx + (y - line.StartY) * dy) / lengthSquared;
            t = Math.Clamp(t, 0d, 1d);
            return Distance(x, y, line.StartX + t * dx, line.StartY + t * dy);
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
    #endregion

    #region 预览画布
    public static class DistanceLLPreviewGeometry
    {
        public static bool TryCreateCanvas(DistanceLLSourceLine? line1, DistanceLLSourceLine? line2, out int canvasWidth, out int canvasHeight, out List<DistanceLLPreviewLine> canvasLines)
        {
            canvasWidth = (int)DistanceLLPreviewStyle.MinCanvasWidth;
            canvasHeight = (int)DistanceLLPreviewStyle.MinCanvasHeight;
            canvasLines = new List<DistanceLLPreviewLine>();

            var sourceLines = new List<(string Name, DistanceLLSourceLine Line, string Color)>();
            if (line1.HasValue && line1.Value.IsValid())
                sourceLines.Add(("线段1", line1.Value, "green"));

            if (line2.HasValue && line2.Value.IsValid())
                sourceLines.Add(("线段2", line2.Value, "cyan"));

            if (sourceLines.Count == 0)
                return false;

            double minX = sourceLines.Min(item => Math.Min(item.Line.StartX, item.Line.EndX));
            double minY = sourceLines.Min(item => Math.Min(item.Line.StartY, item.Line.EndY));
            double maxX = sourceLines.Max(item => Math.Max(item.Line.StartX, item.Line.EndX));
            double maxY = sourceLines.Max(item => Math.Max(item.Line.StartY, item.Line.EndY));
            double width = Math.Max(1d, maxX - minX);
            double height = Math.Max(1d, maxY - minY);

            canvasWidth = (int)Math.Ceiling(Math.Max(DistanceLLPreviewStyle.MinCanvasWidth, width + DistanceLLPreviewStyle.CanvasPadding * 2d));
            canvasHeight = (int)Math.Ceiling(Math.Max(DistanceLLPreviewStyle.MinCanvasHeight, height + DistanceLLPreviewStyle.CanvasPadding * 2d));

            foreach (var item in sourceLines)
            {
                canvasLines.Add(new DistanceLLPreviewLine(
                    item.Name,
                    Normalize(item.Line.StartX, minX),
                    Normalize(item.Line.StartY, minY),
                    Normalize(item.Line.EndX, minX),
                    Normalize(item.Line.EndY, minY),
                    item.Color));
            }

            return true;
        }

        private static double Normalize(double source, double min)
        {
            return source - min + DistanceLLPreviewStyle.CanvasPadding;
        }
    }

    public static class DistanceLLPreviewStyle
    {
        public const double CanvasPadding = 28d;
        public const double MinCanvasWidth = 420d;
        public const double MinCanvasHeight = 300d;
        public const double HandleRadius = 4d;
    }
    #endregion
}

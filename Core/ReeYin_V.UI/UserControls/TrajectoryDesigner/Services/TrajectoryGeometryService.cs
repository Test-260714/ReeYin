using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Services
{
    public static class TrajectoryGeometryService
    {
        public static IEnumerable<(double X, double Y)> GenerateLocalInnerPoints(
            TrajectoryShapeGeometry shape)
        {
            if (shape.InnerPattern != TrajectoryInnerPattern.EquidistantPoints)
            {
                yield break;
            }

            if (shape.Kind == TrajectoryShapeKind.Circle)
            {
                double radius = shape.ScaledRadius;
                int limit = Math.Max((int)Math.Floor(radius / shape.InnerSpacing), 0);
                for (int gridY = -limit; gridY <= limit; gridY++)
                {
                    for (int gridX = -limit; gridX <= limit; gridX++)
                    {
                        double x = gridX * shape.InnerSpacing;
                        double y = gridY * shape.InnerSpacing;
                        if ((x * x) + (y * y) <= (radius * radius) + 1e-9d)
                        {
                            yield return (x, y);
                        }
                    }
                }

                yield break;
            }

            if (shape.Kind != TrajectoryShapeKind.Rectangle)
            {
                yield break;
            }

            double halfWidth = shape.ScaledWidth / 2d;
            double halfHeight = shape.ScaledHeight / 2d;
            foreach (double y in GenerateCenteredPositions(halfHeight, shape.InnerSpacing))
            {
                foreach (double x in GenerateCenteredPositions(halfWidth, shape.InnerSpacing))
                {
                    yield return (x, y);
                }
            }
        }

        public static IEnumerable<(double StartX, double StartY, double EndX, double EndY)> GenerateLocalInnerLines(
            TrajectoryShapeGeometry shape)
        {
            if (shape.InnerPattern is TrajectoryInnerPattern.None or TrajectoryInnerPattern.EquidistantPoints)
            {
                yield break;
            }

            IEnumerable<(double StartX, double StartY, double EndX, double EndY)> lines = shape.Kind switch
            {
                TrajectoryShapeKind.Circle => GenerateCircleInnerLines(shape),
                TrajectoryShapeKind.Rectangle => GenerateRectangleInnerLines(shape),
                _ => Array.Empty<(double StartX, double StartY, double EndX, double EndY)>()
            };

            foreach (var line in lines)
            {
                yield return line;
            }
        }

        public static IEnumerable<(double X, double Y)> GenerateInnerPoints(TrajectoryShapeGeometry shape)
        {
            foreach (var point in GenerateLocalInnerPoints(shape))
            {
                yield return ToWorldPoint(shape, point.X, point.Y);
            }
        }

        public static IEnumerable<(double StartX, double StartY, double EndX, double EndY)> GenerateInnerLines(
            TrajectoryShapeGeometry shape)
        {
            foreach (var line in GenerateLocalInnerLines(shape))
            {
                var start = ToWorldPoint(shape, line.StartX, line.StartY);
                var end = ToWorldPoint(shape, line.EndX, line.EndY);
                yield return (start.X, start.Y, end.X, end.Y);
            }
        }

        public static (double StartX, double StartY, double EndX, double EndY) GetLineEndpointCoordinates(
            TrajectoryShapeGeometry shape)
        {
            double halfLength = shape.ScaledWidth / 2d;
            double angle = DegreesToRadians(shape.RotationAngle);
            double offsetX = halfLength * Math.Cos(angle);
            double offsetY = halfLength * Math.Sin(angle);
            return (
                shape.X - offsetX,
                shape.Y - offsetY,
                shape.X + offsetX,
                shape.Y + offsetY);
        }

        public static (double X, double Y) ToWorldPoint(
            TrajectoryShapeGeometry shape,
            double localX,
            double localY)
        {
            double angle = DegreesToRadians(shape.RotationAngle);
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return (
                shape.X + (localX * cos) - (localY * sin),
                shape.Y + (localX * sin) + (localY * cos));
        }

        public static (double X, double Y) ToLocalPoint(
            TrajectoryShapeGeometry shape,
            double x,
            double y)
        {
            double deltaX = x - shape.X;
            double deltaY = y - shape.Y;
            double angle = DegreesToRadians(-shape.RotationAngle);
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return (
                (deltaX * cos) - (deltaY * sin),
                (deltaX * sin) + (deltaY * cos));
        }

        public static bool ContainsLocalPoint(
            TrajectoryShapeGeometry shape,
            double localX,
            double localY)
        {
            return shape.Kind switch
            {
                TrajectoryShapeKind.Point => Distance(0d, 0d, localX, localY) <= 8d,
                TrajectoryShapeKind.Line => ContainsLine(shape, localX, localY),
                TrajectoryShapeKind.Rectangle => ContainsRectangle(shape, localX, localY),
                TrajectoryShapeKind.Arc => ContainsArc(shape, localX, localY),
                _ => ContainsCircle(shape, localX, localY)
            };
        }

        public static bool ContainsPoint(
            double centerX,
            double centerY,
            double pointX,
            double pointY,
            double tolerance)
        {
            return Distance(centerX, centerY, pointX, pointY) <= NormalizeTolerance(tolerance);
        }

        public static bool ContainsLineSegmentPoint(
            double startX,
            double startY,
            double endX,
            double endY,
            double pointX,
            double pointY,
            double tolerance)
        {
            return ContainsLineSegmentPoint(
                startX,
                startY,
                endX,
                endY,
                pointX,
                pointY,
                tolerance,
                tolerance);
        }

        public static bool ContainsLineSegmentPoint(
            double startX,
            double startY,
            double endX,
            double endY,
            double pointX,
            double pointY,
            double tolerance,
            double degenerateTolerance)
        {
            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
            if (lengthSquared < 1e-9d)
            {
                return ContainsPoint(startX, startY, pointX, pointY, degenerateTolerance);
            }

            double position = (((pointX - startX) * deltaX) + ((pointY - startY) * deltaY)) / lengthSquared;
            position = Math.Clamp(position, 0d, 1d);
            return ContainsPoint(
                startX + (deltaX * position),
                startY + (deltaY * position),
                pointX,
                pointY,
                tolerance);
        }

        private static IEnumerable<(double StartX, double StartY, double EndX, double EndY)> GenerateCircleInnerLines(
            TrajectoryShapeGeometry shape)
        {
            double radius = shape.ScaledRadius;
            switch (shape.InnerPattern)
            {
                case TrajectoryInnerPattern.HorizontalLines:
                    foreach (double offset in GenerateCircleLineOffsets(shape.InnerLineCount, radius))
                    {
                        double halfSpan = Math.Sqrt(Math.Max((radius * radius) - (offset * offset), 0d));
                        yield return (-halfSpan, offset, halfSpan, offset);
                    }
                    break;

                case TrajectoryInnerPattern.VerticalLines:
                    foreach (double offset in GenerateCircleLineOffsets(shape.InnerLineCount, radius))
                    {
                        double halfSpan = Math.Sqrt(Math.Max((radius * radius) - (offset * offset), 0d));
                        yield return (offset, -halfSpan, offset, halfSpan);
                    }
                    break;

                case TrajectoryInnerPattern.CrossLines:
                    double angleStep = Math.PI / shape.InnerLineCount;
                    for (int index = 0; index < shape.InnerLineCount; index++)
                    {
                        double angle = angleStep * index;
                        double x = radius * Math.Cos(angle);
                        double y = radius * Math.Sin(angle);
                        yield return (-x, -y, x, y);
                    }
                    break;
            }
        }

        private static IEnumerable<(double StartX, double StartY, double EndX, double EndY)> GenerateRectangleInnerLines(
            TrajectoryShapeGeometry shape)
        {
            double halfWidth = shape.ScaledWidth / 2d;
            double halfHeight = shape.ScaledHeight / 2d;
            switch (shape.InnerPattern)
            {
                case TrajectoryInnerPattern.HorizontalLines:
                    foreach (double y in GenerateEdgeInclusivePositions(-halfHeight, halfHeight, shape.InnerSpacing))
                    {
                        yield return (-halfWidth, y, halfWidth, y);
                    }
                    break;

                case TrajectoryInnerPattern.VerticalLines:
                    foreach (double x in GenerateEdgeInclusivePositions(-halfWidth, halfWidth, shape.InnerSpacing))
                    {
                        yield return (x, -halfHeight, x, halfHeight);
                    }
                    break;

                case TrajectoryInnerPattern.CrossLines:
                    yield return (-halfWidth, -halfHeight, halfWidth, halfHeight);
                    yield return (-halfWidth, halfHeight, halfWidth, -halfHeight);

                    int extraLineCount = Math.Max(shape.InnerLineCount - 2, 0);
                    if (extraLineCount == 0)
                    {
                        yield break;
                    }

                    double angleStep = Math.PI / extraLineCount;
                    for (int index = 0; index < extraLineCount; index++)
                    {
                        yield return ClipCenterLineToRectangle(halfWidth, halfHeight, angleStep * index);
                    }
                    break;
            }
        }

        private static bool ContainsLine(TrajectoryShapeGeometry shape, double localX, double localY)
        {
            double halfLength = shape.ScaledWidth / 2d;
            return localX >= -halfLength - 4d
                && localX <= halfLength + 4d
                && Math.Abs(localY) <= 6d;
        }

        private static bool ContainsCircle(TrajectoryShapeGeometry shape, double localX, double localY)
        {
            return Distance(0d, 0d, localX, localY) <= shape.ScaledRadius + 6d;
        }

        private static bool ContainsRectangle(TrajectoryShapeGeometry shape, double localX, double localY)
        {
            double halfWidth = shape.ScaledWidth / 2d;
            double halfHeight = shape.ScaledHeight / 2d;
            return localX >= -halfWidth - 4d
                && localX <= halfWidth + 4d
                && localY >= -halfHeight - 4d
                && localY <= halfHeight + 4d;
        }

        private static bool ContainsArc(TrajectoryShapeGeometry shape, double localX, double localY)
        {
            double distance = Distance(0d, 0d, localX, localY);
            if (Math.Abs(distance - shape.ScaledRadius) > 8d)
            {
                return false;
            }

            double angle = NormalizeAngle(Math.Atan2(localY, localX) * 180d / Math.PI);
            double delta = NormalizeAngle(angle - NormalizeAngle(shape.StartAngle));
            return delta <= NormalizeSweep(shape.SweepAngle);
        }

        private static IEnumerable<double> GenerateCircleLineOffsets(int lineCount, double radius)
        {
            if (lineCount <= 1)
            {
                yield return 0d;
                yield break;
            }

            double step = radius * 2d / lineCount;
            for (int index = 0; index < lineCount; index++)
            {
                yield return -radius + (step * (index + 0.5d));
            }
        }

        private static IEnumerable<double> GenerateCenteredPositions(double halfSpan, double spacing)
        {
            int limit = Math.Max((int)Math.Floor(halfSpan / spacing), 0);
            for (int index = -limit; index <= limit; index++)
            {
                yield return index * spacing;
            }
        }

        private static IEnumerable<double> GenerateEdgeInclusivePositions(double min, double max, double spacing)
        {
            for (double position = min; position < max - 1e-9d; position += spacing)
            {
                yield return position;
            }

            yield return max;
        }

        private static (double StartX, double StartY, double EndX, double EndY) ClipCenterLineToRectangle(
            double halfWidth,
            double halfHeight,
            double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double maxX = Math.Abs(cos) < 1e-9d ? double.PositiveInfinity : halfWidth / Math.Abs(cos);
            double maxY = Math.Abs(sin) < 1e-9d ? double.PositiveInfinity : halfHeight / Math.Abs(sin);
            double length = Math.Min(maxX, maxY);
            double x = length * cos;
            double y = length * sin;
            return (-x, -y, x, y);
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % 360d;
            return normalized < 0d ? normalized + 360d : normalized;
        }

        private static double NormalizeSweep(double sweepAngle)
        {
            if (!double.IsFinite(sweepAngle) || Math.Abs(sweepAngle) < 1e-6d)
            {
                return 180d;
            }

            return Math.Clamp(Math.Abs(sweepAngle), 1d, 360d);
        }

        private static double DegreesToRadians(double angle)
        {
            return angle * Math.PI / 180d;
        }

        private static double NormalizeTolerance(double tolerance)
        {
            return double.IsFinite(tolerance) ? Math.Max(tolerance, 0d) : 0d;
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double deltaX = x2 - x1;
            double deltaY = y2 - y1;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}

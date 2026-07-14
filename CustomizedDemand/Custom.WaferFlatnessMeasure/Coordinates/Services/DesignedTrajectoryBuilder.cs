using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using ReeYin_V.UI.UserControls.TrajectoryDesigner.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Custom.WaferFlatnessMeasure
{
    public static class DesignedTrajectoryBuilder
    {
        private const double DefaultSampleInterval = 1d;
        private const double CoordinateRoundScale = 1000000d;

        public static DesignedTrajectoryPlan BuildPlan(
            IEnumerable<EditableTrajectoryShape>? editableShapes,
            double coordinateStartX,
            double coordinateStartY,
            double coordinateEndX,
            double coordinateEndY,
            double defaultCenterX,
            double defaultCenterY,
            double sampleInterval)
        {
            var plan = new DesignedTrajectoryPlan
            {
                CoordinateStartX = coordinateStartX,
                CoordinateStartY = coordinateStartY,
                CoordinateEndX = coordinateEndX,
                CoordinateEndY = coordinateEndY,
                DefaultCenterX = defaultCenterX,
                DefaultCenterY = defaultCenterY,
                UpdatedAt = DateTime.Now,
                Shapes = (editableShapes ?? Enumerable.Empty<EditableTrajectoryShape>())
                    .Select(ToDesignedShape)
                    .ToList()
            };

            plan.RunSteps = BuildRunSteps(plan.Shapes, sampleInterval);
            plan.ExecutionMode = ResolveExecutionMode(plan.RunSteps);
            ReindexRunSteps(plan.RunSteps);
            return plan;
        }

        public static ObservableCollection<EditableTrajectoryShape> ToEditableShapes(DesignedTrajectoryPlan? plan)
        {
            var shapes = new ObservableCollection<EditableTrajectoryShape>();
            foreach (DesignedTrajectoryShape shape in plan?.Shapes ?? Enumerable.Empty<DesignedTrajectoryShape>())
            {
                shapes.Add(ToEditableShape(shape));
            }

            return shapes;
        }

        public static List<LocusInfo> ToLocusInfos(DesignedTrajectoryPlan? plan)
        {
            return ToLocusInfos(plan?.RunSteps);
        }

        public static List<LocusInfo> ToLocusInfos(IEnumerable<DesignedTrajectoryRunStep>? runSteps)
        {
            return (runSteps ?? Enumerable.Empty<DesignedTrajectoryRunStep>())
                .Where(IsValidRunStep)
                .Select(ToLocusInfo)
                .ToList();
        }

        public static List<DesignedTrajectoryRunStep> BuildRunSteps(
            IEnumerable<DesignedTrajectoryShape>? shapes,
            double sampleInterval)
        {
            double normalizedSampleInterval = NormalizePositive(sampleInterval, DefaultSampleInterval);
            var steps = new List<DesignedTrajectoryRunStep>();

            foreach (DesignedTrajectoryShape shape in shapes ?? Enumerable.Empty<DesignedTrajectoryShape>())
            {
                if (shape == null || !shape.IsEnabled)
                {
                    continue;
                }

                steps.AddRange(BuildShapeRunSteps(shape, normalizedSampleInterval));
            }

            ReindexRunSteps(steps);
            return steps;
        }

        public static DesignedTrajectoryShape ToDesignedShape(EditableTrajectoryShape shape)
        {
            return new DesignedTrajectoryShape
            {
                Id = shape.Id,
                IsEnabled = shape.IsEnabled,
                Kind = shape.Kind,
                X = RoundCoordinate(shape.X),
                Y = RoundCoordinate(shape.Y),
                Width = NormalizePositive(shape.Width, 80d),
                Height = NormalizePositive(shape.Height, 60d),
                Radius = NormalizePositive(shape.Radius, 40d),
                StartAngle = shape.StartAngle,
                SweepAngle = NormalizeSweep(shape.SweepAngle),
                RotationAngle = NormalizeAngle(shape.RotationAngle),
                Scale = NormalizePositive(shape.Scale, 1d),
                InnerPattern = shape.InnerPattern,
                InnerSpacing = NormalizePositive(shape.InnerSpacing, 20d),
                InnerLineCount = Math.Max(shape.InnerLineCount, 1),
                PolylinePoints = ClonePolylinePoints(shape.PolylinePoints)
            };
        }

        public static EditableTrajectoryShape ToEditableShape(DesignedTrajectoryShape shape)
        {
            return new EditableTrajectoryShape
            {
                Id = shape.Id == Guid.Empty ? Guid.NewGuid() : shape.Id,
                IsEnabled = shape.IsEnabled,
                Kind = shape.Kind,
                X = shape.X,
                Y = shape.Y,
                Width = NormalizePositive(shape.Width, 80d),
                Height = NormalizePositive(shape.Height, 60d),
                Radius = NormalizePositive(shape.Radius, 40d),
                StartAngle = shape.StartAngle,
                SweepAngle = NormalizeSweep(shape.SweepAngle),
                RotationAngle = NormalizeAngle(shape.RotationAngle),
                Scale = NormalizePositive(shape.Scale, 1d),
                InnerPattern = shape.InnerPattern,
                InnerSpacing = NormalizePositive(shape.InnerSpacing, 20d),
                InnerLineCount = Math.Max(shape.InnerLineCount, 1),
                PolylinePoints = ClonePolylinePoints(shape.PolylinePoints)
            };
        }

        private static IEnumerable<DesignedTrajectoryRunStep> BuildShapeRunSteps(
            DesignedTrajectoryShape shape,
            double sampleInterval)
        {
            return shape.Kind switch
            {
                TrajectoryShapeKind.Point => new[] { CreatePointStep(shape, shape.X, shape.Y, "点") },
                TrajectoryShapeKind.Line => GenerateLineShapeStep(shape),
                TrajectoryShapeKind.Polyline => GeneratePolylineSteps(shape),
                TrajectoryShapeKind.Rectangle => GenerateRegionRunSteps(shape),
                TrajectoryShapeKind.Arc => GenerateArcSteps(shape, sampleInterval),
                _ => GenerateRegionRunSteps(shape)
            };
        }

        private static bool IsRegionShape(DesignedTrajectoryShape shape)
        {
            return shape.Category == TrajectoryShapeCategory.Region;
        }

        private static IEnumerable<DesignedTrajectoryRunStep> GenerateLineShapeStep(DesignedTrajectoryShape shape)
        {
            var endpoints = TrajectoryGeometryService.GetLineEndpointCoordinates(
                ToGeometry(shape));
            yield return CreateLineStep(
                shape,
                endpoints.StartX,
                endpoints.StartY,
                endpoints.EndX,
                endpoints.EndY,
                "线段");
        }

        private static IEnumerable<DesignedTrajectoryRunStep> GeneratePolylineSteps(DesignedTrajectoryShape shape)
        {
            List<TrajectoryPoint> points = ClonePolylinePoints(shape.PolylinePoints);
            for (int index = 1; index < points.Count; index++)
            {
                TrajectoryPoint start = points[index - 1];
                TrajectoryPoint end = points[index];
                yield return CreateLineStep(shape, start.X, start.Y, end.X, end.Y, "Polyline");
            }
        }

        private static IEnumerable<DesignedTrajectoryRunStep> GenerateRegionRunSteps(DesignedTrajectoryShape shape)
        {
            if (!IsRegionShape(shape))
            {
                yield break;
            }

            TrajectoryShapeGeometry geometry = ToGeometry(shape);

            foreach (var point in TrajectoryGeometryService.GenerateInnerPoints(geometry))
            {
                yield return CreatePointStep(shape, point.X, point.Y, "?????");
            }

            foreach (var line in TrajectoryGeometryService.GenerateInnerLines(geometry))
            {
                yield return CreateLineStep(shape, line.StartX, line.StartY, line.EndX, line.EndY, "?????");
            }
        }

        private static IEnumerable<DesignedTrajectoryRunStep> GenerateArcSteps(
            DesignedTrajectoryShape shape,
            double sampleInterval)
        {
            double radius = GetScaledRadius(shape);
            double sweep = NormalizeSweep(shape.SweepAngle);
            double arcLength = Math.Abs(sweep) * Math.PI / 180d * radius;
            int segmentCount = Math.Max(2, (int)Math.Ceiling(arcLength / sampleInterval));
            return GenerateArcPolylineSteps(shape, shape.StartAngle, sweep, segmentCount, "圆弧");
        }

        private static IEnumerable<DesignedTrajectoryRunStep> GenerateArcPolylineSteps(
            DesignedTrajectoryShape shape,
            double startAngle,
            double sweepAngle,
            int segmentCount,
            string sourceName)
        {
            (double X, double Y)? previous = null;
            for (int index = 0; index <= segmentCount; index++)
            {
                double angle = startAngle + (sweepAngle * index / segmentCount);
                var current = ToWorldPoint(shape, GetScaledRadius(shape), 0d, angle);
                if (previous.HasValue)
                {
                    yield return CreateLineStep(
                        shape,
                        previous.Value.X,
                        previous.Value.Y,
                        current.X,
                        current.Y,
                        sourceName);
                }

                previous = current;
            }
        }

        private static DesignedTrajectoryRunStep CreatePointStep(
            DesignedTrajectoryShape shape,
            double x,
            double y,
            string sourceName)
        {
            double roundedX = RoundCoordinate(x);
            double roundedY = RoundCoordinate(y);
            return new DesignedTrajectoryRunStep
            {
                SourceShapeId = shape.Id,
                Kind = DesignedTrajectoryRunStepKind.Point,
                StartX = roundedX,
                StartY = roundedY,
                EndX = roundedX,
                EndY = roundedY,
                SourceName = sourceName
            };
        }

        private static DesignedTrajectoryRunStep CreateLineStep(
            DesignedTrajectoryShape shape,
            double startX,
            double startY,
            double endX,
            double endY,
            string sourceName)
        {
            return new DesignedTrajectoryRunStep
            {
                SourceShapeId = shape.Id,
                Kind = DesignedTrajectoryRunStepKind.Line,
                StartX = RoundCoordinate(startX),
                StartY = RoundCoordinate(startY),
                EndX = RoundCoordinate(endX),
                EndY = RoundCoordinate(endY),
                SourceName = sourceName
            };
        }

        private static LocusInfo ToLocusInfo(DesignedTrajectoryRunStep step)
        {
            bool isPoint = step.Kind == DesignedTrajectoryRunStepKind.Point;
            return new LocusInfo
            {
                Type = isPoint ? LocusInfo.PointType : LocusInfo.LineType,
                OriginX = step.StartX,
                OriginY = step.StartY,
                TargetX = isPoint ? step.StartX : step.EndX,
                TargetY = isPoint ? step.StartY : step.EndY
            };
        }

        private static DesignedTrajectoryExecutionMode ResolveExecutionMode(
            IReadOnlyCollection<DesignedTrajectoryRunStep>? steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return DesignedTrajectoryExecutionMode.None;
            }

            bool hasPoint = steps.Any(item => item.Kind == DesignedTrajectoryRunStepKind.Point);
            bool hasLine = steps.Any(item => item.Kind == DesignedTrajectoryRunStepKind.Line);
            if (hasPoint && hasLine)
            {
                return DesignedTrajectoryExecutionMode.Mixed;
            }

            return hasPoint ? DesignedTrajectoryExecutionMode.Points : DesignedTrajectoryExecutionMode.Lines;
        }

        private static void ReindexRunSteps(IList<DesignedTrajectoryRunStep> steps)
        {
            for (int index = 0; index < steps.Count; index++)
            {
                steps[index].Index = index + 1;
            }
        }

        private static bool IsValidRunStep(DesignedTrajectoryRunStep? step)
        {
            return step != null &&
                   double.IsFinite(step.StartX) &&
                   double.IsFinite(step.StartY) &&
                   double.IsFinite(step.EndX) &&
                   double.IsFinite(step.EndY);
        }

        private static (double X, double Y) ToWorldPoint(
            DesignedTrajectoryShape shape,
            double localX,
            double localY,
            double localAngle)
        {
            double radiusX = localX * Math.Cos(localAngle * Math.PI / 180d);
            double radiusY = localX * Math.Sin(localAngle * Math.PI / 180d);
            return ToWorldPoint(shape, radiusX, radiusY);
        }

        private static (double X, double Y) ToWorldPoint(
            DesignedTrajectoryShape shape,
            double localX,
            double localY)
        {
            var point = TrajectoryGeometryService.ToWorldPoint(
                ToGeometry(shape),
                localX,
                localY);
            return (
                RoundCoordinate(point.X),
                RoundCoordinate(point.Y));
        }

        private static TrajectoryShapeGeometry ToGeometry(DesignedTrajectoryShape shape)
        {
            return TrajectoryShapeGeometry.FromEditable(ToEditableShape(shape));
        }

        private static double GetScaledRadius(DesignedTrajectoryShape shape)
        {
            return NormalizePositive(shape.Radius, 40d) * GetScale(shape);
        }

        private static double GetScale(DesignedTrajectoryShape shape)
        {
            return NormalizePositive(shape.Scale, 1d);
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return double.IsFinite(value) && value > 0d ? value : fallback;
        }

        private static double NormalizeSweep(double value)
        {
            if (!double.IsFinite(value) || Math.Abs(value) < 1e-6d)
            {
                return 180d;
            }

            return Math.Clamp(Math.Abs(value), 1d, 360d);
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % 360d;
            return normalized < 0d ? normalized + 360d : normalized;
        }

        private static double RoundCoordinate(double value)
        {
            if (!double.IsFinite(value))
            {
                return 0d;
            }

            return Math.Round(value * CoordinateRoundScale) / CoordinateRoundScale;
        }

        private static List<TrajectoryPoint> ClonePolylinePoints(IEnumerable<TrajectoryPoint>? points)
        {
            return (points ?? Enumerable.Empty<TrajectoryPoint>())
                .Where(point => point != null && double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Select(point => new TrajectoryPoint
                {
                    X = RoundCoordinate(point.X),
                    Y = RoundCoordinate(point.Y)
                })
                .ToList();
        }
    }
}

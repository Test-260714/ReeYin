using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public sealed class DesignedTrajectoryPlan
    {
        public int Version { get; set; } = 1;

        public double CoordinateStartX { get; set; }

        public double CoordinateStartY { get; set; }

        public double CoordinateEndX { get; set; } = 640d;

        public double CoordinateEndY { get; set; } = 480d;

        public double DefaultCenterX { get; set; }

        public double DefaultCenterY { get; set; }

        public DesignedTrajectoryExecutionMode ExecutionMode { get; set; } = DesignedTrajectoryExecutionMode.None;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<DesignedTrajectoryShape> Shapes { get; set; } = new List<DesignedTrajectoryShape>();

        public List<DesignedTrajectoryRunStep> RunSteps { get; set; } = new List<DesignedTrajectoryRunStep>();

        public bool HasEnabledShapes => Shapes?.Any(item => item?.IsEnabled == true) == true;

        public bool HasRunSteps => RunSteps?.Count > 0;

        public DesignedTrajectoryPlan Clone()
        {
            return new DesignedTrajectoryPlan
            {
                Version = Version,
                CoordinateStartX = CoordinateStartX,
                CoordinateStartY = CoordinateStartY,
                CoordinateEndX = CoordinateEndX,
                CoordinateEndY = CoordinateEndY,
                DefaultCenterX = DefaultCenterX,
                DefaultCenterY = DefaultCenterY,
                ExecutionMode = ExecutionMode,
                UpdatedAt = UpdatedAt,
                Shapes = Shapes?.Select(item => item.Clone()).ToList() ?? new List<DesignedTrajectoryShape>(),
                RunSteps = RunSteps?.Select(item => item.Clone()).ToList() ?? new List<DesignedTrajectoryRunStep>()
            };
        }
    }

    [Serializable]
    public sealed class DesignedTrajectoryShape
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool IsEnabled { get; set; } = true;

        public TrajectoryShapeKind Kind { get; set; }

        public TrajectoryShapeCategory Category => TrajectoryShapeCategoryResolver.Resolve(Kind);

        public string CategoryText => TrajectoryShapeCategoryResolver.ToDisplayText(Category);

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; } = 80d;

        public double Height { get; set; } = 60d;

        public double Radius { get; set; } = 40d;

        public double StartAngle { get; set; }

        public double SweepAngle { get; set; } = 180d;

        public double RotationAngle { get; set; }

        public double Scale { get; set; } = 1d;

        public TrajectoryInnerPattern InnerPattern { get; set; } = TrajectoryInnerPattern.None;

        public double InnerSpacing { get; set; } = 20d;

        public int InnerLineCount { get; set; } = 5;

        public List<TrajectoryPoint> PolylinePoints { get; set; } = new List<TrajectoryPoint>();

        public DesignedTrajectoryShape Clone()
        {
            return new DesignedTrajectoryShape
            {
                Id = Id,
                IsEnabled = IsEnabled,
                Kind = Kind,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                Radius = Radius,
                StartAngle = StartAngle,
                SweepAngle = SweepAngle,
                RotationAngle = RotationAngle,
                Scale = Scale,
                InnerPattern = InnerPattern,
                InnerSpacing = InnerSpacing,
                InnerLineCount = InnerLineCount,
                PolylinePoints = PolylinePoints?.Where(point => point != null).Select(point => point.Clone()).ToList()
                    ?? new List<TrajectoryPoint>()
            };
        }
    }

    [Serializable]
    public sealed class DesignedTrajectoryRunStep
    {
        public int Index { get; set; }

        public Guid SourceShapeId { get; set; }

        public DesignedTrajectoryRunStepKind Kind { get; set; }

        public double StartX { get; set; }

        public double StartY { get; set; }

        public double EndX { get; set; }

        public double EndY { get; set; }

        public string SourceName { get; set; } = string.Empty;

        public string KindText => Kind == DesignedTrajectoryRunStepKind.Point ? "点" : "线段";

        public DesignedTrajectoryRunStep Clone()
        {
            return new DesignedTrajectoryRunStep
            {
                Index = Index,
                SourceShapeId = SourceShapeId,
                Kind = Kind,
                StartX = StartX,
                StartY = StartY,
                EndX = EndX,
                EndY = EndY,
                SourceName = SourceName
            };
        }
    }

    [Serializable]
    public enum DesignedTrajectoryExecutionMode
    {
        None,
        Points,
        Lines,
        Mixed
    }

    [Serializable]
    public enum DesignedTrajectoryRunStepKind
    {
        Point,
        Line
    }
}

using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using System;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Services
{
    public readonly struct TrajectoryShapeGeometry
    {
        private TrajectoryShapeGeometry(
            Guid id,
            TrajectoryShapeKind kind,
            double x,
            double y,
            double width,
            double height,
            double radius,
            double startAngle,
            double sweepAngle,
            double rotationAngle,
            double scale,
            TrajectoryInnerPattern innerPattern,
            double innerSpacing,
            int innerLineCount)
        {
            Id = id;
            Kind = kind;
            X = NormalizeFinite(x, 0d);
            Y = NormalizeFinite(y, 0d);
            Width = NormalizePositive(width, 80d);
            Height = NormalizePositive(height, 60d);
            Radius = NormalizePositive(radius, 40d);
            StartAngle = NormalizeFinite(startAngle, 0d);
            SweepAngle = NormalizeFinite(sweepAngle, 180d);
            RotationAngle = NormalizeFinite(rotationAngle, 0d);
            Scale = NormalizePositive(scale, 1d);
            InnerPattern = innerPattern;
            InnerSpacing = NormalizePositive(innerSpacing, 20d);
            InnerLineCount = Math.Max(innerLineCount, 1);
        }

        public Guid Id { get; }

        public TrajectoryShapeKind Kind { get; }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public double Radius { get; }

        public double StartAngle { get; }

        public double SweepAngle { get; }

        public double RotationAngle { get; }

        public double Scale { get; }

        public TrajectoryInnerPattern InnerPattern { get; }

        public double InnerSpacing { get; }

        public int InnerLineCount { get; }

        public double ScaledWidth => Width * Scale;

        public double ScaledHeight => Height * Scale;

        public double ScaledRadius => Radius * Scale;

        public static TrajectoryShapeGeometry FromEditable(EditableTrajectoryShape shape)
        {
            ArgumentNullException.ThrowIfNull(shape);
            return new TrajectoryShapeGeometry(
                shape.Id,
                shape.Kind,
                shape.X,
                shape.Y,
                shape.Width,
                shape.Height,
                shape.Radius,
                shape.StartAngle,
                shape.SweepAngle,
                shape.RotationAngle,
                shape.Scale,
                shape.InnerPattern,
                shape.InnerSpacing,
                shape.InnerLineCount);
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return double.IsFinite(value) && value > 0d ? value : fallback;
        }

        private static double NormalizeFinite(double value, double fallback)
        {
            return double.IsFinite(value) ? value : fallback;
        }
    }
}

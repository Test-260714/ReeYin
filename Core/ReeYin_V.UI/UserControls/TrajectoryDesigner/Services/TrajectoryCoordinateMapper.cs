using System;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Services
{
    public readonly struct CoordinateScreenTransform
    {
        public CoordinateScreenTransform(
            double plotLeft,
            double plotTop,
            double plotWidth,
            double plotHeight,
            double uniformScale,
            double coordinateStartX,
            double coordinateStartY,
            double coordinateEndX,
            double coordinateEndY)
        {
            PlotLeft = plotLeft;
            PlotTop = plotTop;
            PlotWidth = plotWidth;
            PlotHeight = plotHeight;
            UniformScale = uniformScale;
            CoordinateStartX = coordinateStartX;
            CoordinateStartY = coordinateStartY;
            CoordinateEndX = coordinateEndX;
            CoordinateEndY = coordinateEndY;
        }

        public double PlotLeft { get; }

        public double PlotTop { get; }

        public double PlotWidth { get; }

        public double PlotHeight { get; }

        public double UniformScale { get; }

        public double CoordinateStartX { get; }

        public double CoordinateStartY { get; }

        public double CoordinateEndX { get; }

        public double CoordinateEndY { get; }
    }

    public static class TrajectoryCoordinateMapper
    {
        private const double MinimumRange = 1e-9d;

        public static CoordinateScreenTransform GetTransform(
            double viewportWidth,
            double viewportHeight,
            double coordinateStartX,
            double coordinateStartY,
            double coordinateEndX,
            double coordinateEndY)
        {
            viewportWidth = NormalizePositive(viewportWidth, 1d);
            viewportHeight = NormalizePositive(viewportHeight, 1d);
            coordinateStartX = NormalizeFinite(coordinateStartX, 0d);
            coordinateStartY = NormalizeFinite(coordinateStartY, 0d);
            coordinateEndX = NormalizeFinite(coordinateEndX, coordinateStartX + 1d);
            coordinateEndY = NormalizeFinite(coordinateEndY, coordinateStartY + 1d);

            if (Math.Abs(coordinateEndX - coordinateStartX) < MinimumRange)
            {
                coordinateEndX = coordinateStartX + 1d;
            }

            if (Math.Abs(coordinateEndY - coordinateStartY) < MinimumRange)
            {
                coordinateEndY = coordinateStartY + 1d;
            }

            double rangeX = Math.Abs(coordinateEndX - coordinateStartX);
            double rangeY = Math.Abs(coordinateEndY - coordinateStartY);
            double uniformScale = Math.Max(
                Math.Min(viewportWidth / rangeX, viewportHeight / rangeY),
                MinimumRange);
            double plotWidth = rangeX * uniformScale;
            double plotHeight = rangeY * uniformScale;

            return new CoordinateScreenTransform(
                (viewportWidth - plotWidth) / 2d,
                (viewportHeight - plotHeight) / 2d,
                plotWidth,
                plotHeight,
                uniformScale,
                coordinateStartX,
                coordinateStartY,
                coordinateEndX,
                coordinateEndY);
        }

        public static (double X, double Y) ScreenToCoordinate(
            CoordinateScreenTransform transform,
            double screenX,
            double screenY)
        {
            double boundedScreenX = Math.Clamp(
                NormalizeFinite(screenX, transform.PlotLeft),
                transform.PlotLeft,
                transform.PlotLeft + transform.PlotWidth);
            double boundedScreenY = Math.Clamp(
                NormalizeFinite(screenY, transform.PlotTop),
                transform.PlotTop,
                transform.PlotTop + transform.PlotHeight);
            double xRatio = (boundedScreenX - transform.PlotLeft) / transform.PlotWidth;
            double yRatio = (transform.PlotHeight - (boundedScreenY - transform.PlotTop)) / transform.PlotHeight;

            return (
                transform.CoordinateStartX + (xRatio * (transform.CoordinateEndX - transform.CoordinateStartX)),
                transform.CoordinateStartY + (yRatio * (transform.CoordinateEndY - transform.CoordinateStartY)));
        }

        public static (double X, double Y) CoordinateToScreen(
            CoordinateScreenTransform transform,
            double x,
            double y)
        {
            double rangeX = transform.CoordinateEndX - transform.CoordinateStartX;
            double rangeY = transform.CoordinateEndY - transform.CoordinateStartY;
            double normalizedX = (NormalizeFinite(x, transform.CoordinateStartX) - transform.CoordinateStartX) / rangeX;
            double normalizedY = (NormalizeFinite(y, transform.CoordinateStartY) - transform.CoordinateStartY) / rangeY;

            return (
                transform.PlotLeft + (normalizedX * transform.PlotWidth),
                transform.PlotTop + transform.PlotHeight - (normalizedY * transform.PlotHeight));
        }

        public static double LengthToScreen(CoordinateScreenTransform transform, double coordinateLength)
        {
            return Math.Abs(NormalizeFinite(coordinateLength, 0d)) * transform.UniformScale;
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

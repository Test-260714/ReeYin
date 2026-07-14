using System;
using System.Collections.Generic;
using System.Linq;

namespace ALGO.MeasureCircle
{
    public static class CircleCalibrationMath
    {
        public static bool TryCalculateWorldRadius(
            double centerX,
            double centerY,
            IReadOnlyCollection<(double X, double Y)> worldPoints,
            out double radius)
        {
            radius = 0d;

            if (worldPoints == null || worldPoints.Count == 0)
            {
                return false;
            }

            var distances = worldPoints
                .Select(point => Math.Sqrt(
                    Math.Pow(point.X - centerX, 2d) +
                    Math.Pow(point.Y - centerY, 2d)))
                .Where(distance => !double.IsNaN(distance) && !double.IsInfinity(distance) && distance > 0d)
                .OrderBy(distance => distance)
                .ToArray();

            if (distances.Length == 0)
            {
                return false;
            }

            int middle = distances.Length / 2;
            radius = distances.Length % 2 == 1
                ? distances[middle]
                : (distances[middle - 1] + distances[middle]) / 2d;

            return radius > 0d;
        }
    }
}

using System;

namespace ReeYin_V.Share.Models.Trajectory
{
    [Serializable]
    public readonly struct TrajectoryPoint : IEquatable<TrajectoryPoint>
    {
        public TrajectoryPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

        public static TrajectoryPoint Empty { get; } = new TrajectoryPoint(double.NaN, double.NaN);

        public double DistanceTo(TrajectoryPoint other)
        {
            double deltaX = other.X - X;
            double deltaY = other.Y - Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        public bool Equals(TrajectoryPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is TrajectoryPoint point && Equals(point);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(TrajectoryPoint left, TrajectoryPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TrajectoryPoint left, TrajectoryPoint right)
        {
            return !left.Equals(right);
        }
    }
}

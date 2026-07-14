using OpenCvSharp;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Measurement;

internal sealed class Plane
{
    public Plane(double a, double b, double c, double d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public double A { get; }

    public double B { get; }

    public double C { get; }

    public double D { get; }

    public double DistanceTo(Point3d point)
    {
        return (A * point.X + B * point.Y + C * point.Z + D) /
               (Math.Sqrt(A * A + B * B + C * C) + 1e-12);
    }
}

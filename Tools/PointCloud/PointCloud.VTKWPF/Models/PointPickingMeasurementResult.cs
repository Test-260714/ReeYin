using System.Globalization;
using PointCloud.Interop;

namespace PointCloud.VTKWPF.Models;

public sealed class PointPickingMeasurementResult
{
    public static PointPickingMeasurementResult Empty { get; } = new(
        title: string.Empty,
        body: string.Empty);

    private PointPickingMeasurementResult(
        string title,
        string body,
        double distance = 0.0,
        double deltaX = 0.0,
        double deltaY = 0.0,
        double deltaZ = 0.0,
        double deltaXY = 0.0,
        double deltaXZ = 0.0,
        double deltaZY = 0.0,
        double area = 0.0,
        Point3d normal = default,
        double edgeAB = 0.0,
        double edgeBC = 0.0,
        double edgeCA = 0.0,
        double angleA = 0.0,
        double angleB = 0.0,
        double angleC = 0.0)
    {
        Title = title;
        Body = body;
        Distance = distance;
        DeltaX = deltaX;
        DeltaY = deltaY;
        DeltaZ = deltaZ;
        DeltaXY = deltaXY;
        DeltaXZ = deltaXZ;
        DeltaZY = deltaZY;
        Area = area;
        Normal = normal;
        EdgeAB = edgeAB;
        EdgeBC = edgeBC;
        EdgeCA = edgeCA;
        AngleA = angleA;
        AngleB = angleB;
        AngleC = angleC;
    }

    public string Title { get; }

    public string Body { get; }

    public double Distance { get; }

    public double DeltaX { get; }

    public double DeltaY { get; }

    public double DeltaZ { get; }

    public double DeltaXY { get; }

    public double DeltaXZ { get; }

    public double DeltaZY { get; }

    public double Area { get; }

    public Point3d Normal { get; }

    public double EdgeAB { get; }

    public double EdgeBC { get; }

    public double EdgeCA { get; }

    public double AngleA { get; }

    public double AngleB { get; }

    public double AngleC { get; }

    public static PointPickingMeasurementResult From(
        PointPickingMeasurementMode mode,
        IReadOnlyList<Point3d> points)
    {
        if (points.Count == 0)
        {
            return Empty;
        }

        return mode switch
        {
            PointPickingMeasurementMode.PointInfo => FromPoint(points[^1]),
            PointPickingMeasurementMode.Distance => FromDistance(points),
            PointPickingMeasurementMode.Angle => FromAngle(points),
            _ => Empty,
        };
    }

    private static PointPickingMeasurementResult FromPoint(Point3d point)
    {
        string body = string.Join(
            Environment.NewLine,
            "Point",
            $"X: {Format(point.X)}",
            $"Y: {Format(point.Y)}",
            $"Z: {Format(point.Z)}");

        return new PointPickingMeasurementResult("Point", body);
    }

    private static PointPickingMeasurementResult FromDistance(IReadOnlyList<Point3d> points)
    {
        if (points.Count < 2)
        {
            return FromPoint(points[^1]);
        }

        Point3d a = points[0];
        Point3d b = points[1];
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double dz = b.Z - a.Z;
        double distance = Length(dx, dy, dz);
        double deltaXY = Length(dx, dy, 0.0);
        double deltaXZ = Length(dx, 0.0, dz);
        double deltaZY = Length(0.0, dy, dz);

        string body = string.Join(
            Environment.NewLine,
            "Distance",
            $"A: {FormatPoint(a)}",
            $"B: {FormatPoint(b)}",
            $"Distance: {Format(distance)}",
            $"dX: {Format(dx)}",
            $"dY: {Format(dy)}",
            $"dZ: {Format(dz)}",
            $"dXY: {Format(deltaXY)}",
            $"dXZ: {Format(deltaXZ)}",
            $"dZY: {Format(deltaZY)}");

        return new PointPickingMeasurementResult(
            "Distance",
            body,
            distance: distance,
            deltaX: dx,
            deltaY: dy,
            deltaZ: dz,
            deltaXY: deltaXY,
            deltaXZ: deltaXZ,
            deltaZY: deltaZY);
    }

    private static PointPickingMeasurementResult FromAngle(IReadOnlyList<Point3d> points)
    {
        if (points.Count < 3)
        {
            return FromDistance(points);
        }

        Point3d a = points[0];
        Point3d b = points[1];
        Point3d c = points[2];
        double ab = DistanceBetween(a, b);
        double bc = DistanceBetween(b, c);
        double ca = DistanceBetween(c, a);

        (double normalX, double normalY, double normalZ) = Cross(
            b.X - a.X,
            b.Y - a.Y,
            b.Z - a.Z,
            c.X - a.X,
            c.Y - a.Y,
            c.Z - a.Z);
        double crossLength = Length(normalX, normalY, normalZ);
        double area = crossLength * 0.5;
        Point3d normal = crossLength > 0.0
            ? new Point3d(normalX / crossLength, normalY / crossLength, normalZ / crossLength)
            : default;

        double angleA = AngleBetween(
            b.X - a.X,
            b.Y - a.Y,
            b.Z - a.Z,
            c.X - a.X,
            c.Y - a.Y,
            c.Z - a.Z);
        double angleB = AngleBetween(
            a.X - b.X,
            a.Y - b.Y,
            a.Z - b.Z,
            c.X - b.X,
            c.Y - b.Y,
            c.Z - b.Z);
        double angleC = AngleBetween(
            a.X - c.X,
            a.Y - c.Y,
            a.Z - c.Z,
            b.X - c.X,
            b.Y - c.Y,
            b.Z - c.Z);

        string body = string.Join(
            Environment.NewLine,
            "Angle",
            $"A: {FormatPoint(a)}",
            $"B: {FormatPoint(b)}",
            $"C: {FormatPoint(c)}",
            $"Area: {Format(area)}",
            $"Normal: {FormatPoint(normal)}",
            $"AB: {Format(ab)}",
            $"BC: {Format(bc)}",
            $"CA: {Format(ca)}",
            $"Angle A: {Format(angleA)} deg",
            $"Angle B: {Format(angleB)} deg",
            $"Angle C: {Format(angleC)} deg");

        return new PointPickingMeasurementResult(
            "Angle / Area",
            body,
            area: area,
            normal: normal,
            edgeAB: ab,
            edgeBC: bc,
            edgeCA: ca,
            angleA: angleA,
            angleB: angleB,
            angleC: angleC);
    }

    private static double DistanceBetween(Point3d a, Point3d b)
    {
        return Length(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
    }

    private static double AngleBetween(
        double ax,
        double ay,
        double az,
        double bx,
        double by,
        double bz)
    {
        double lengthA = Length(ax, ay, az);
        double lengthB = Length(bx, by, bz);
        if (lengthA <= 0.0 || lengthB <= 0.0)
        {
            return 0.0;
        }

        double cosine = ((ax * bx) + (ay * by) + (az * bz)) / (lengthA * lengthB);
        cosine = Math.Clamp(cosine, -1.0, 1.0);
        return Math.Acos(cosine) * 180.0 / Math.PI;
    }

    private static (double X, double Y, double Z) Cross(
        double ax,
        double ay,
        double az,
        double bx,
        double by,
        double bz)
    {
        return (
            (ay * bz) - (az * by),
            (az * bx) - (ax * bz),
            (ax * by) - (ay * bx));
    }

    private static double Length(double x, double y, double z)
    {
        return Math.Sqrt((x * x) + (y * y) + (z * z));
    }

    private static string FormatPoint(Point3d point)
    {
        return $"({Format(point.X)}, {Format(point.Y)}, {Format(point.Z)})";
    }

    private static string Format(double value)
    {
        return value.ToString("G9", CultureInfo.InvariantCulture);
    }
}

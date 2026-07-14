using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public static class AcsPegPathSampler
{
    private const double Tolerance = 1e-9;

    // Keep geometry deterministic and hardware-free so PEG point generation can be tested without a controller.
    public static IReadOnlyList<AcsPoint2D> SampleLine(AcsPoint2D start, AcsPoint2D end, double interval)
    {
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));
        ValidateInterval(interval);

        var length = Distance(start, end);
        if (length <= Tolerance)
        {
            return new[] { start };
        }

        return BuildDistances(length, interval)
            .Select(distance =>
            {
                if (Math.Abs(distance - length) <= Tolerance)
                {
                    return end;
                }

                var ratio = distance / length;
                return Lerp(start, end, ratio);
            })
            .ToList();
    }

    public static IReadOnlyList<AcsPoint2D> SampleArcCenter(
        AcsPoint2D start,
        AcsPoint2D end,
        AcsPoint2D center,
        DirOfRotation direction,
        double interval)
    {
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));
        ValidatePoint(center, nameof(center));
        ValidateInterval(interval);

        var radius = Distance(center, start);
        var endRadius = Distance(center, end);
        if (radius <= Tolerance)
        {
            throw new ArgumentException("Arc radius must be greater than zero.", nameof(start));
        }

        if (Math.Abs(radius - endRadius) > Math.Max(0.001, radius * 1e-6))
        {
            throw new ArgumentException("Arc start and end points must use the same radius.");
        }

        if (Distance(start, end) <= Tolerance)
        {
            return new[] { start };
        }

        var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        var clockwise = (int)direction == 0;
        var sweep = clockwise
            ? NormalizePositive(startAngle - endAngle)
            : NormalizePositive(endAngle - startAngle);
        var length = sweep * radius;

        return BuildDistances(length, interval)
            .Select(distance =>
            {
                if (Math.Abs(distance - length) <= Tolerance)
                {
                    return end;
                }

                var delta = distance / radius;
                var angle = clockwise ? startAngle - delta : startAngle + delta;
                return new AcsPoint2D(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle));
            })
            .ToList();
    }

    public static IReadOnlyList<AcsPoint2D> SamplePolyline(IReadOnlyList<AcsPoint2D> points, double interval)
    {
        if (points == null || points.Count < 2)
        {
            throw new ArgumentException("Polyline must contain at least two points.", nameof(points));
        }

        ValidateInterval(interval);
        foreach (var point in points)
        {
            ValidatePoint(point, nameof(points));
        }

        // Zero-length segments are ignored so duplicate vertices do not disturb equal path spacing.
        var segments = new List<(AcsPoint2D Start, AcsPoint2D End, double Length)>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var length = Distance(points[i], points[i + 1]);
            if (length > Tolerance)
            {
                segments.Add((points[i], points[i + 1], length));
            }
        }

        if (segments.Count == 0)
        {
            return new[] { points[0] };
        }

        var totalLength = segments.Sum(segment => segment.Length);
        var sampled = new List<AcsPoint2D>();
        foreach (var distance in BuildDistances(totalLength, interval))
        {
            if (Math.Abs(distance - totalLength) <= Tolerance)
            {
                sampled.Add(segments[^1].End);
                continue;
            }

            // Walk cumulative segment length to preserve spacing across polyline corners.
            var remaining = distance;
            foreach (var segment in segments)
            {
                if (remaining <= segment.Length + Tolerance)
                {
                    sampled.Add(Lerp(segment.Start, segment.End, Math.Clamp(remaining / segment.Length, 0d, 1d)));
                    break;
                }

                remaining -= segment.Length;
            }
        }

        return sampled;
    }

    public static bool TrySamplePath(AcsPeg2DPathRequest request, out IReadOnlyList<AcsPoint2D> points, out string message)
    {
        points = Array.Empty<AcsPoint2D>();
        message = string.Empty;

        if (request == null)
        {
            message = "PEG 2D path request is null.";
            return false;
        }

        try
        {
            points = request.PathKind switch
            {
                AcsPeg2DPathKind.Line => SampleLine(request.Start, request.End, request.Interval),
                AcsPeg2DPathKind.ArcCenter => SampleArcCenter(
                    request.Start,
                    request.End,
                    request.Center,
                    request.ArcDirection,
                    request.Interval),
                AcsPeg2DPathKind.Polyline => SamplePolyline(request.Points?.ToList() ?? new List<AcsPoint2D>(), request.Interval),
                _ => throw new ArgumentOutOfRangeException(nameof(request.PathKind), request.PathKind, "Unsupported PEG path kind.")
            };

            message = $"Sampled {points.Count} PEG path points.";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    public static bool TryProjectReferencePositions(
        IReadOnlyList<AcsPoint2D> points,
        AcsPegReferenceAxis referenceAxis,
        out double[] positions,
        out string message)
    {
        positions = Array.Empty<double>();
        message = string.Empty;

        if (points == null || points.Count == 0)
        {
            message = "PEG points are empty.";
            return false;
        }

        if (referenceAxis == AcsPegReferenceAxis.Auto)
        {
            if (TryProjectReferencePositions(points, AcsPegReferenceAxis.X, out positions, out message))
            {
                return true;
            }

            if (TryProjectReferencePositions(points, AcsPegReferenceAxis.Y, out positions, out message))
            {
                return true;
            }

            message = "PEG path is not monotonic on X or Y; split the path or use a controller-side buffer.";
            return false;
        }

        // A single ACS PEG engine tracks one reference axis, so the projected point table must be monotonic.
        positions = points
            .Select(point => referenceAxis == AcsPegReferenceAxis.X ? point.X : point.Y)
            .ToArray();

        if (!IsStrictlyMonotonic(positions))
        {
            message = $"PEG reference positions on {referenceAxis} must be strictly monotonic.";
            positions = Array.Empty<double>();
            return false;
        }

        message = $"Projected {positions.Length} positions on {referenceAxis}.";
        return true;
    }

    private static IReadOnlyList<double> BuildDistances(double length, double interval)
    {
        // Include both endpoints; skip an extra near-end interval point to avoid duplicate controller triggers.
        var distances = new List<double> { 0d };
        for (var distance = interval; distance < length - Tolerance; distance += interval)
        {
            distances.Add(distance);
        }

        if (length > Tolerance)
        {
            distances.Add(length);
        }

        return distances;
    }

    private static bool IsStrictlyMonotonic(IReadOnlyList<double> values)
    {
        if (values.Count <= 1)
        {
            return true;
        }

        var direction = 0;
        for (var i = 1; i < values.Count; i++)
        {
            var delta = values[i] - values[i - 1];
            if (Math.Abs(delta) <= Tolerance)
            {
                return false;
            }

            var currentDirection = delta > 0d ? 1 : -1;
            if (direction == 0)
            {
                direction = currentDirection;
                continue;
            }

            if (direction != currentDirection)
            {
                return false;
            }
        }

        return true;
    }

    private static AcsPoint2D Lerp(AcsPoint2D start, AcsPoint2D end, double ratio)
    {
        return new AcsPoint2D(
            start.X + (end.X - start.X) * ratio,
            start.Y + (end.Y - start.Y) * ratio);
    }

    private static double Distance(AcsPoint2D a, AcsPoint2D b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static double NormalizePositive(double angle)
    {
        var fullCircle = Math.PI * 2d;
        angle %= fullCircle;
        return angle <= Tolerance ? angle + fullCircle : angle;
    }

    private static void ValidateInterval(double interval)
    {
        if (!IsFinite(interval) || interval <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "PEG sample interval must be greater than zero.");
        }
    }

    private static void ValidatePoint(AcsPoint2D point, string name)
    {
        if (!IsFinite(point.X) || !IsFinite(point.Y))
        {
            throw new ArgumentException("PEG point coordinates must be finite.", name);
        }
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

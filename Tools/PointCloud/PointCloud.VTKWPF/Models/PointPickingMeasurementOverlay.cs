using PointCloud.Interop;

namespace PointCloud.VTKWPF.Models;

public sealed class PointPickingMeasurementOverlay
{
    public PointPickingMeasurementOverlay(IEnumerable<Point3d> points)
    {
        Points = points?.Where(IsFinite).Take(3).ToArray() ?? Array.Empty<Point3d>();
    }

    public IReadOnlyList<Point3d> Points { get; }

    public bool HasAny => Points.Count > 0;

    private static bool IsFinite(Point3d point)
    {
        return double.IsFinite(point.X)
            && double.IsFinite(point.Y)
            && double.IsFinite(point.Z);
    }
}

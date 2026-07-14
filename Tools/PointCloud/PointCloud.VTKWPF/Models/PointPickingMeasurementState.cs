using PointCloud.Interop;

namespace PointCloud.VTKWPF.Models;

public sealed class PointPickingMeasurementState
{
    private readonly List<Point3d> _points = new();

    public PointPickingMeasurementMode Mode { get; private set; }

    public IReadOnlyList<Point3d> Points => _points;

    public PointPickingMeasurementResult Result { get; private set; } = PointPickingMeasurementResult.Empty;

    public void SetMode(PointPickingMeasurementMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        Clear();
    }

    public void Clear()
    {
        _points.Clear();
        Result = PointPickingMeasurementResult.Empty;
    }

    public bool AddPickedPoint(Point3d point)
    {
        if (Mode == PointPickingMeasurementMode.None || !IsFinite(point))
        {
            return false;
        }

        int maxCount = Mode switch
        {
            PointPickingMeasurementMode.PointInfo => 1,
            PointPickingMeasurementMode.Distance => 2,
            PointPickingMeasurementMode.Angle => 3,
            _ => 0,
        };

        if (maxCount <= 0)
        {
            return false;
        }

        if (_points.Count >= maxCount)
        {
            _points.Clear();
        }

        if (Mode == PointPickingMeasurementMode.PointInfo)
        {
            _points.Clear();
        }

        _points.Add(point);
        Result = PointPickingMeasurementResult.From(Mode, _points);
        return true;
    }

    private static bool IsFinite(Point3d point)
    {
        return double.IsFinite(point.X)
            && double.IsFinite(point.Y)
            && double.IsFinite(point.Z);
    }
}

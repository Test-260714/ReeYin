using System.Windows;
using PointCloud.Interop;

namespace PointCloud.VTKWPF.Models;

public sealed class PointPickedEventArgs : EventArgs
{
    public PointPickedEventArgs(Point3d point, Point screenPosition)
    {
        Point = point;
        ScreenPosition = screenPosition;
    }

    public Point3d Point { get; }

    public Point ScreenPosition { get; }
}

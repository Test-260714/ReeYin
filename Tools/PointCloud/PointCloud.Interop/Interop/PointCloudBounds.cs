namespace PointCloud.Interop;

public readonly record struct PointCloudBounds(
    double MinX,
    double MaxX,
    double MinY,
    double MaxY,
    double MinZ,
    double MaxZ)
{
    public double SizeX => MaxX - MinX;

    public double SizeY => MaxY - MinY;

    public double SizeZ => MaxZ - MinZ;
}

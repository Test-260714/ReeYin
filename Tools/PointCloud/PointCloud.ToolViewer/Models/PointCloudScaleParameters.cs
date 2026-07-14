namespace PointCloud.ToolViewer.Models;

public sealed class PointCloudScaleParameters
{
    public double ScaleX { get; set; } = 1.0;

    public double ScaleY { get; set; } = 1.0;

    public double ScaleZ { get; set; } = 1.0;

    public bool SameScaleForAllDimensions { get; set; } = true;

    public bool KeepEntityInPlace { get; set; }

    public bool ResetRequested { get; private set; }

    public void SyncLinkedAxes()
    {
        if (!SameScaleForAllDimensions)
        {
            return;
        }

        ScaleY = ScaleX;
        ScaleZ = ScaleX;
    }

    public static PointCloudScaleParameters CreateResetResult()
    {
        return new PointCloudScaleParameters
        {
            ResetRequested = true,
        };
    }
}

using PointCloud.Interop;

namespace PointCloud.ToolViewer.Models;

public sealed class DepthImageImportParameters
{
    public double SpacingX { get; set; } = 1.0;

    public double SpacingY { get; set; } = 1.0;

    public double SpacingZ { get; set; } = 1.0;

    public double InvalidValue { get; set; } = 0.0;

    public bool UseInvalidValue { get; set; } = true;

    public DepthImageImportParameters Clone()
    {
        return new DepthImageImportParameters
        {
            SpacingX = SpacingX,
            SpacingY = SpacingY,
            SpacingZ = SpacingZ,
            InvalidValue = InvalidValue,
            UseInvalidValue = UseInvalidValue,
        };
    }

    public DepthTiffLoadOptions ToLoadOptions()
    {
        return new DepthTiffLoadOptions(SpacingX, SpacingY, SpacingZ, InvalidValue, UseInvalidValue);
    }
}

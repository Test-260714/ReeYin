namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public sealed record StitchingTile(
    int Index,
    int CoreCol,
    int CoreRow,
    int CoreWidth,
    int CoreHeight,
    int HaloCol,
    int HaloRow,
    int HaloWidth,
    int HaloHeight,
    double OriginX,
    double OriginY);

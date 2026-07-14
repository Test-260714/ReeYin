namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public static class StitchingTilePlanner
{
    public static IReadOnlyList<StitchingTile> Plan(
        StitchingCanvasGeometry canvas,
        int coreWidth,
        int coreHeight,
        int halo)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (canvas.Width <= 0 || canvas.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(canvas), "Canvas dimensions must be positive.");
        if (coreWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(coreWidth), coreWidth, "Tile core width must be positive.");
        if (coreHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(coreHeight), coreHeight, "Tile core height must be positive.");
        if (halo < 0)
            throw new ArgumentOutOfRangeException(nameof(halo), halo, "Tile halo must be non-negative.");

        long tileColumns = DivideCeiling(canvas.Width, coreWidth);
        long tileRows = DivideCeiling(canvas.Height, coreHeight);
        long tileCount = checked(tileColumns * tileRows);
        if (tileCount > int.MaxValue)
            throw new OverflowException($"Stitching tile count {tileCount} exceeds supported tile index range.");

        var tiles = new List<StitchingTile>();
        int index = 0;
        long canvasWidth = canvas.Width;
        long canvasHeight = canvas.Height;
        long haloPixels = halo;
        for (long coreRow = 0; coreRow < canvasHeight; coreRow += coreHeight)
        {
            long clippedCoreHeight = Math.Min(coreHeight, canvasHeight - coreRow);
            for (long coreCol = 0; coreCol < canvasWidth; coreCol += coreWidth)
            {
                long clippedCoreWidth = Math.Min(coreWidth, canvasWidth - coreCol);
                long haloCol = Math.Max(0, coreCol - haloPixels);
                long haloRow = Math.Max(0, coreRow - haloPixels);
                long haloRight = Math.Min(canvasWidth, coreCol + clippedCoreWidth + haloPixels);
                long haloBottom = Math.Min(canvasHeight, coreRow + clippedCoreHeight + haloPixels);

                tiles.Add(new StitchingTile(
                    index++,
                    ToInt(coreCol, nameof(StitchingTile.CoreCol)),
                    ToInt(coreRow, nameof(StitchingTile.CoreRow)),
                    ToInt(clippedCoreWidth, nameof(StitchingTile.CoreWidth)),
                    ToInt(clippedCoreHeight, nameof(StitchingTile.CoreHeight)),
                    ToInt(haloCol, nameof(StitchingTile.HaloCol)),
                    ToInt(haloRow, nameof(StitchingTile.HaloRow)),
                    ToInt(haloRight - haloCol, nameof(StitchingTile.HaloWidth)),
                    ToInt(haloBottom - haloRow, nameof(StitchingTile.HaloHeight)),
                    canvas.MinX + haloCol * canvas.IntervalX,
                    canvas.MinY + haloRow * canvas.IntervalY));
            }
        }

        return tiles;
    }

    private static long DivideCeiling(long dividend, long divisor)
    {
        return (dividend + divisor - 1) / divisor;
    }

    private static int ToInt(long value, string fieldName)
    {
        if (value < 0 || value > int.MaxValue)
            throw new OverflowException($"{fieldName} value {value} is outside Int32 range.");

        return (int)value;
    }
}

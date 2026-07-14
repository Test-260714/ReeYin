using System.Runtime.InteropServices;
using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using Custom.ElectroStaticChuckMeasure.ALGO.Registration;
using HalconDotNet;
using PointCloud.Algorithms.Dtos;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

public static class StitchingRasterizer
{
    private const double IntervalTolerance = 1e-9;

    public static StitchingCanvasGeometry PlanCanvas(CorrectedFrameSet corrected, RegistrationResult registration)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(registration);
        IReadOnlyList<FrameDescriptor> frames = corrected.Frames.Frames;
        if (frames.Count == 0)
            throw new InvalidOperationException("Stitching requires at least one corrected frame.");
        if (registration.Transforms.Count != frames.Count)
        {
            throw new InvalidOperationException(
                $"Stitching transform count mismatch: Frames={frames.Count}, Transforms={registration.Transforms.Count}.");
        }

        FrameDescriptor first = frames[0];
        ValidateFrameIntervals(frames);

        GetTransformedCanvasBounds(frames, registration.Transforms, out double minX, out double minY, out double maxX, out double maxY);
        int width = ToCanvasSize(maxX - minX, first.IntervalX, "Width");
        int height = ToCanvasSize(maxY - minY, first.IntervalY, "Height");
        return new StitchingCanvasGeometry(minX, minY, maxX, maxY, width, height, first.IntervalX, first.IntervalY, first.IntervalZ);
    }

    public static ImageFrame Rasterize(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        SensorParameters sensor,
        Action<int, FrameDescriptor>? frameProgress = null)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        StitchingCanvasGeometry canvas = PlanCanvas(corrected, registration);
        return Rasterize(corrected, registration, sensor, canvas, frameProgress);
    }

    internal static ImageFrame RasterizeUncorrected(CorrectedFrameSet corrected, RegistrationResult registration, SensorParameters sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        StitchingCanvasGeometry canvas = PlanCanvas(corrected, registration);
        return Rasterize(corrected, registration, sensor, canvas, applyPointIntervalCorrection: false);
    }

    internal static ImageFrame RasterizePreview(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        SensorParameters sensor,
        int downsampleFactor,
        Action<int, FrameDescriptor>? frameProgress = null)
    {
        if (downsampleFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(downsampleFactor), downsampleFactor, "Preview downsample factor must be positive.");

        StitchingCanvasGeometry canvas = PlanCanvas(corrected, registration);
        StitchingCanvasGeometry previewCanvas = downsampleFactor == 1
            ? canvas
            : CreateDownsampledCanvas(canvas, downsampleFactor);
        return Rasterize(corrected, registration, sensor, previewCanvas, applyPointIntervalCorrection: true, frameProgress);
    }

    public static ImageFrame Rasterize(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        SensorParameters sensor,
        StitchingCanvasGeometry canvas,
        Action<int, FrameDescriptor>? frameProgress = null)
    {
        return Rasterize(corrected, registration, sensor, canvas, applyPointIntervalCorrection: true, frameProgress);
    }

    private static StitchingCanvasGeometry CreateDownsampledCanvas(StitchingCanvasGeometry canvas, int downsampleFactor)
    {
        int width = Math.Max(1, (canvas.Width + downsampleFactor - 1) / downsampleFactor);
        int height = Math.Max(1, (canvas.Height + downsampleFactor - 1) / downsampleFactor);
        return canvas with
        {
            Width = width,
            Height = height,
            IntervalX = canvas.IntervalX * downsampleFactor,
            IntervalY = canvas.IntervalY * downsampleFactor
        };
    }

    private static ImageFrame Rasterize(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        SensorParameters sensor,
        StitchingCanvasGeometry canvas,
        bool applyPointIntervalCorrection,
        Action<int, FrameDescriptor>? frameProgress = null)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(sensor);
        ArgumentNullException.ThrowIfNull(canvas);

        IReadOnlyList<FrameDescriptor> frames = corrected.Frames.Frames;
        ValidateFrameIntervals(frames);
        if (registration.Transforms.Count != frames.Count)
        {
            throw new InvalidOperationException(
                $"Stitching transform count mismatch: Frames={frames.Count}, Transforms={registration.Transforms.Count}.");
        }

        long pixelCountLong = checked((long)canvas.Width * canvas.Height);
        if (pixelCountLong > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Stitching canvas is too large: Width={canvas.Width}, Height={canvas.Height}, Pixels={pixelCountLong}.");
        }

        int pixelCount = (int)pixelCountLong;
        double[] heightSums = new double[pixelCount];
        double[] grayWeightedSums = new double[pixelCount];
        double[] grayWeightSums = new double[pixelCount];
        int[] counts = new int[pixelCount];

        for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
        {
            frameProgress?.Invoke(frameIndex, frames[frameIndex]);
            using ImageFrame frame = corrected.LoadFrame(frameIndex, sensor);
            RasterizeFrame(frame, registration.Transforms[frameIndex], canvas, sensor.InvalidValue, heightSums, grayWeightedSums, grayWeightSums, counts);
        }

        return CreateFrame(frames[0], canvas, sensor.InvalidValue, heightSums, grayWeightedSums, grayWeightSums, counts, applyPointIntervalCorrection);
    }

    public static ImageFrame RasterizeTile(
        CorrectedFrameSet corrected,
        RegistrationResult registration,
        SensorParameters sensor,
        StitchingCanvasGeometry canvas,
        StitchingTile tile,
        Action<int, FrameDescriptor>? frameProgress = null)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(sensor);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(tile);

        IReadOnlyList<FrameDescriptor> frames = corrected.Frames.Frames;
        ValidateFrameIntervals(frames);
        if (registration.Transforms.Count != frames.Count)
        {
            throw new InvalidOperationException(
                $"Stitching transform count mismatch: Frames={frames.Count}, Transforms={registration.Transforms.Count}.");
        }

        long pixelCountLong = checked((long)tile.HaloWidth * tile.HaloHeight);
        if (pixelCountLong > int.MaxValue)
            throw new InvalidOperationException($"Stitching tile is too large: Width={tile.HaloWidth}, Height={tile.HaloHeight}.");

        int pixelCount = (int)pixelCountLong;
        double[] heightSums = new double[pixelCount];
        double[] graySums = new double[pixelCount];
        int[] counts = new int[pixelCount];

        for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
        {
            if (!FrameMayIntersectTile(frames[frameIndex], registration.Transforms[frameIndex], canvas, tile))
                continue;

            frameProgress?.Invoke(frameIndex, frames[frameIndex]);
            using ImageFrame frame = corrected.LoadFrame(frameIndex, sensor);
            RasterizeFrameToTile(frame, registration.Transforms[frameIndex], canvas, tile, sensor.InvalidValue, heightSums, graySums, counts);
        }

        return CreateTileFrame(frames[0], canvas, tile, sensor.InvalidValue, heightSums, graySums, counts);
    }

    private static void RasterizeFrame(
        ImageFrame frame,
        PointCloudRigidTransform2D transform,
        StitchingCanvasGeometry canvas,
        double invalidDepthValue,
        double[] heightSums,
        double[] grayWeightedSums,
        double[] grayWeightSums,
        int[] counts)
    {
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        FrameDescriptor descriptor = frame.Descriptor;

        HOperatorSet.GetRegionPoints(frame.ValidMask, out HTuple rows, out HTuple cols);
        try
        {
            int validPixels = rows.Length;
            if (validPixels == 0)
                return;

            HOperatorSet.GetGrayval(frame.HeightImage, rows, cols, out HTuple heights);
            try
            {
                HOperatorSet.GetGrayval(frame.GrayImage, rows, cols, out HTuple grays);
                try
                {
                    for (int pointIndex = 0; pointIndex < validPixels; pointIndex++)
                    {
                        double rawHeight = heights[pointIndex].D;
                        if (IsInvalidDepth(rawHeight, invalidDepthValue))
                            continue;

                        int sourceCol = cols[pointIndex].I;
                        int sourceRow = rows[pointIndex].I;
                        double x = sourceCol * descriptor.IntervalX;
                        double y = sourceRow * descriptor.IntervalY;
                        double transformedX = transform.X + cos * x - sin * y;
                        double transformedY = transform.Y + sin * x + cos * y;

                        int dstCol = (int)Math.Round((transformedX - canvas.MinX) / canvas.IntervalX);
                        int dstRow = (int)Math.Round((transformedY - canvas.MinY) / canvas.IntervalY);
                        if (dstCol < 0 || dstCol >= canvas.Width || dstRow < 0 || dstRow >= canvas.Height)
                            continue;

                        int dstIndex = dstRow * canvas.Width + dstCol;
                        heightSums[dstIndex] += (rawHeight * descriptor.IntervalZ + transform.Z) / canvas.IntervalZ;
                        double grayWeight = ComputeRectangleGrayBlendWeight(sourceCol, sourceRow, frame.Width, frame.Height);
                        grayWeightedSums[dstIndex] += grays[pointIndex].D * grayWeight;
                        grayWeightSums[dstIndex] += grayWeight;
                        counts[dstIndex]++;
                    }
                }
                finally
                {
                    grays.Dispose();
                }
            }
            finally
            {
                heights.Dispose();
            }
        }
        finally
        {
            rows.Dispose();
            cols.Dispose();
        }
    }

    private static void RasterizeFrameToTile(
        ImageFrame frame,
        PointCloudRigidTransform2D transform,
        StitchingCanvasGeometry canvas,
        StitchingTile tile,
        double invalidDepthValue,
        double[] heightSums,
        double[] graySums,
        int[] counts)
    {
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        FrameDescriptor descriptor = frame.Descriptor;

        HObject? tileSourceMask = null;
        HTuple? rows = null;
        HTuple? cols = null;
        try
        {
            if (!TryCreateSourceTileMask(frame, transform, canvas, tile, out tileSourceMask))
                return;

            HOperatorSet.GetRegionPoints(tileSourceMask, out rows, out cols);
            int validPixels = rows.Length;
            if (validPixels == 0)
                return;

            HOperatorSet.GetGrayval(frame.HeightImage, rows, cols, out HTuple heights);
            try
            {
                HOperatorSet.GetGrayval(frame.GrayImage, rows, cols, out HTuple grays);
                try
                {
                    for (int pointIndex = 0; pointIndex < validPixels; pointIndex++)
                    {
                        double rawHeight = heights[pointIndex].D;
                        if (IsInvalidDepth(rawHeight, invalidDepthValue))
                            continue;

                        int sourceCol = cols[pointIndex].I;
                        int sourceRow = rows[pointIndex].I;
                        double x = sourceCol * descriptor.IntervalX;
                        double y = sourceRow * descriptor.IntervalY;
                        double transformedX = transform.X + cos * x - sin * y;
                        double transformedY = transform.Y + sin * x + cos * y;

                        int globalCol = (int)Math.Round((transformedX - canvas.MinX) / canvas.IntervalX);
                        int globalRow = (int)Math.Round((transformedY - canvas.MinY) / canvas.IntervalY);
                        int localCol = globalCol - tile.HaloCol;
                        int localRow = globalRow - tile.HaloRow;
                        if (localCol < 0 || localCol >= tile.HaloWidth || localRow < 0 || localRow >= tile.HaloHeight)
                            continue;

                        int localIndex = localRow * tile.HaloWidth + localCol;
                        heightSums[localIndex] += (rawHeight * descriptor.IntervalZ + transform.Z) / canvas.IntervalZ;
                        graySums[localIndex] += grays[pointIndex].D;
                        counts[localIndex]++;
                    }
                }
                finally
                {
                    grays.Dispose();
                }
            }
            finally
            {
                heights.Dispose();
            }
        }
        finally
        {
            rows?.Dispose();
            cols?.Dispose();
            tileSourceMask?.Dispose();
        }
    }

    private static bool TryCreateSourceTileMask(
        ImageFrame frame,
        PointCloudRigidTransform2D transform,
        StitchingCanvasGeometry canvas,
        StitchingTile tile,
        out HObject? sourceTileMask)
    {
        sourceTileMask = null;
        FrameDescriptor descriptor = frame.Descriptor;
        double tileMinX = canvas.MinX + tile.HaloCol * canvas.IntervalX - canvas.IntervalX * 0.5;
        double tileMinY = canvas.MinY + tile.HaloRow * canvas.IntervalY - canvas.IntervalY * 0.5;
        double tileMaxX = canvas.MinX + (tile.HaloCol + tile.HaloWidth - 1) * canvas.IntervalX + canvas.IntervalX * 0.5;
        double tileMaxY = canvas.MinY + (tile.HaloRow + tile.HaloHeight - 1) * canvas.IntervalY + canvas.IntervalY * 0.5;

        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        double minCol = double.PositiveInfinity;
        double minRow = double.PositiveInfinity;
        double maxCol = double.NegativeInfinity;
        double maxRow = double.NegativeInfinity;
        UpdateInverseTransformedTileBounds(tileMinX, tileMinY, transform, cos, sin, descriptor, ref minCol, ref minRow, ref maxCol, ref maxRow);
        UpdateInverseTransformedTileBounds(tileMaxX, tileMinY, transform, cos, sin, descriptor, ref minCol, ref minRow, ref maxCol, ref maxRow);
        UpdateInverseTransformedTileBounds(tileMinX, tileMaxY, transform, cos, sin, descriptor, ref minCol, ref minRow, ref maxCol, ref maxRow);
        UpdateInverseTransformedTileBounds(tileMaxX, tileMaxY, transform, cos, sin, descriptor, ref minCol, ref minRow, ref maxCol, ref maxRow);

        if (!double.IsFinite(minCol) || !double.IsFinite(minRow) || !double.IsFinite(maxCol) || !double.IsFinite(maxRow))
            return false;

        int roiCol1 = Math.Max(0, (int)Math.Floor(minCol) - 1);
        int roiRow1 = Math.Max(0, (int)Math.Floor(minRow) - 1);
        int roiCol2 = Math.Min(frame.Width - 1, (int)Math.Ceiling(maxCol) + 1);
        int roiRow2 = Math.Min(frame.Height - 1, (int)Math.Ceiling(maxRow) + 1);
        if (roiCol1 > roiCol2 || roiRow1 > roiRow2)
            return false;

        HObject? roi = null;
        HObject? intersection = null;
        try
        {
            HOperatorSet.GenRectangle1(out roi, roiRow1, roiCol1, roiRow2, roiCol2);
            HOperatorSet.Intersection(frame.ValidMask, roi, out intersection);
            sourceTileMask = intersection;
            intersection = null;
            return true;
        }
        finally
        {
            HObjectUtils.DisposeAll(roi, intersection);
        }
    }

    private static void UpdateInverseTransformedTileBounds(
        double worldX,
        double worldY,
        PointCloudRigidTransform2D transform,
        double cos,
        double sin,
        FrameDescriptor descriptor,
        ref double minCol,
        ref double minRow,
        ref double maxCol,
        ref double maxRow)
    {
        double dx = worldX - transform.X;
        double dy = worldY - transform.Y;
        double sourceX = cos * dx + sin * dy;
        double sourceY = -sin * dx + cos * dy;
        double col = sourceX / descriptor.IntervalX;
        double row = sourceY / descriptor.IntervalY;

        minCol = Math.Min(minCol, col);
        minRow = Math.Min(minRow, row);
        maxCol = Math.Max(maxCol, col);
        maxRow = Math.Max(maxRow, row);
    }

    private static ImageFrame CreateFrame(
        FrameDescriptor source,
        StitchingCanvasGeometry canvas,
        double invalidDepthValue,
        double[] heightSums,
        double[] grayWeightedSums,
        double[] grayWeightSums,
        int[] counts,
        bool applyPointIntervalCorrection)
    {
        byte[] grayData = new byte[counts.Length];
        float[] heightData = new float[counts.Length];
        var validRows = new List<int>();
        var validCols = new List<int>();

        for (int row = 0; row < canvas.Height; row++)
        {
            for (int col = 0; col < canvas.Width; col++)
            {
                int index = row * canvas.Width + col;
                if (counts[index] > 0)
                {
                    heightData[index] = (float)(heightSums[index] / counts[index]);
                    double gray = grayWeightSums[index] > 0.0
                        ? grayWeightedSums[index] / grayWeightSums[index]
                        : 0.0;
                    grayData[index] = (byte)Math.Clamp((int)Math.Round(gray), 0, 255);
                    validRows.Add(row);
                    validCols.Add(col);
                }
                else
                {
                    heightData[index] = (float)invalidDepthValue;
                    grayData[index] = 0;
                }
            }
        }

        HObject? grayImage = null;
        HObject? heightImage = null;
        HObject? validMask = null;
        try
        {
            grayImage = CreateByteImage(grayData, canvas.Width, canvas.Height);
            heightImage = CreateRealImage(heightData, canvas.Width, canvas.Height);
            validMask = CreateValidMask(validRows, validCols);
            int correctedWidth = canvas.Width;
            int correctedHeight = canvas.Height;
            double correctedIntervalX = canvas.IntervalX;
            double correctedIntervalY = canvas.IntervalY;
            if (applyPointIntervalCorrection)
            {
                ApplyPointIntervalCorrection(
                    ref grayImage,
                    ref heightImage,
                    ref validMask,
                    invalidDepthValue,
                    ref correctedWidth,
                    ref correctedHeight,
                    ref correctedIntervalX,
                    ref correctedIntervalY);
            }

            long correctedValidPointCount = CountRegionPoints(validMask);

            var descriptor = source with
            {
                FolderPath = string.Empty,
                GrayImagePath = string.Empty,
                HeightImagePath = string.Empty,
                IntervalX = correctedIntervalX,
                IntervalY = correctedIntervalY,
                IntervalZ = canvas.IntervalZ,
                MinDepth = source.MinDepth,
                MaxDepth = source.MaxDepth,
                OffsetX = canvas.OriginX / correctedIntervalX,
                OffsetY = canvas.OriginY / correctedIntervalY,
                CompensationX = 0,
                CompensationY = 0,
                IsFlip = false,
                Width = correctedWidth,
                Height = correctedHeight,
                RawValidPointCount = correctedValidPointCount
            };

            var frame = new ImageFrame(descriptor, grayImage!, heightImage!, validMask!, correctedWidth, correctedHeight, correctedValidPointCount);
            grayImage = null;
            heightImage = null;
            validMask = null;
            return frame;
        }
        finally
        {
            HObjectUtils.DisposeAll(grayImage, heightImage, validMask);
        }
    }

    private static void ApplyPointIntervalCorrection(
        ref HObject? grayImage,
        ref HObject? heightImage,
        ref HObject? validMask,
        double invalidDepthValue,
        ref int width,
        ref int height,
        ref double intervalX,
        ref double intervalY)
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        if (intervalX < intervalY)
            scaleY = intervalY / intervalX;
        else if (intervalX > intervalY)
            scaleX = intervalX / intervalY;

        if (Math.Abs(scaleX - 1.0) <= 1e-9 && Math.Abs(scaleY - 1.0) <= 1e-9)
            return;

        HObject? scaledGray = null;
        HObject? scaledHeight = null;
        HObject? correctedMask = null;
        try
        {
            HOperatorSet.ZoomImageFactor(grayImage, out scaledGray, scaleX, scaleY, "bilinear");
            HOperatorSet.ZoomImageFactor(heightImage, out scaledHeight, scaleX, scaleY, "nearest_neighbor");
            (int correctedWidth, int correctedHeight) = GetImageSize(scaledHeight);
            correctedMask = CreateValidMaskFromHeight(scaledHeight, correctedWidth, correctedHeight, invalidDepthValue);

            ReplaceNullable(ref grayImage, ref scaledGray);
            ReplaceNullable(ref heightImage, ref scaledHeight);
            ReplaceNullable(ref validMask, ref correctedMask);

            width = correctedWidth;
            height = correctedHeight;
            intervalX /= scaleX;
            intervalY /= scaleY;
        }
        finally
        {
            HObjectUtils.DisposeAll(scaledGray, scaledHeight, correctedMask);
        }
    }

    private static HObject CreateValidMaskFromHeight(HObject heightImage, int width, int height, double invalidDepthValue)
    {
        HObject? fullRegion = null;
        HObject? invalidRegion = null;
        HObject? validRegion = null;
        try
        {
            HOperatorSet.GenRectangle1(out fullRegion, 0, 0, height - 1, width - 1);
            HOperatorSet.Threshold(heightImage, out invalidRegion, invalidDepthValue, invalidDepthValue);
            HOperatorSet.Difference(fullRegion, invalidRegion, out validRegion);
            HObject result = validRegion;
            validRegion = null;
            return result;
        }
        finally
        {
            HObjectUtils.DisposeAll(fullRegion, invalidRegion, validRegion);
        }
    }

    private static void ReplaceNullable(ref HObject? target, ref HObject? source)
    {
        HObject? previous = target;
        if (!ReferenceEquals(previous, source))
            previous?.Dispose();

        target = source;
        source = null;
    }

    private static long CountRegionPoints(HObject? region)
    {
        ArgumentNullException.ThrowIfNull(region);
        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple row, out HTuple col);
        try
        {
            return (long)Math.Round(area.D);
        }
        finally
        {
            col.Dispose();
            row.Dispose();
            area.Dispose();
        }
    }

    private static (int Width, int Height) GetImageSize(HObject? image)
    {
        ArgumentNullException.ThrowIfNull(image);
        HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
        try
        {
            return (width.I, height.I);
        }
        finally
        {
            height.Dispose();
            width.Dispose();
        }
    }

    private static ImageFrame CreateTileFrame(
        FrameDescriptor source,
        StitchingCanvasGeometry canvas,
        StitchingTile tile,
        double invalidDepthValue,
        double[] heightSums,
        double[] graySums,
        int[] counts)
    {
        byte[] grayData = new byte[counts.Length];
        float[] heightData = new float[counts.Length];
        var validRows = new List<int>();
        var validCols = new List<int>();

        for (int row = 0; row < tile.HaloHeight; row++)
        {
            for (int col = 0; col < tile.HaloWidth; col++)
            {
                int index = row * tile.HaloWidth + col;
                if (counts[index] > 0)
                {
                    heightData[index] = (float)(heightSums[index] / counts[index]);
                    grayData[index] = (byte)Math.Clamp((int)Math.Round(graySums[index] / counts[index]), 0, 255);
                    validRows.Add(row);
                    validCols.Add(col);
                }
                else
                {
                    heightData[index] = (float)invalidDepthValue;
                    grayData[index] = 0;
                }
            }
        }

        HObject? grayImage = null;
        HObject? heightImage = null;
        HObject? validMask = null;
        try
        {
            grayImage = CreateByteImage(grayData, tile.HaloWidth, tile.HaloHeight);
            heightImage = CreateRealImage(heightData, tile.HaloWidth, tile.HaloHeight);
            validMask = CreateValidMask(validRows, validCols);

            var descriptor = source with
            {
                FolderPath = string.Empty,
                GrayImagePath = string.Empty,
                HeightImagePath = string.Empty,
                IntervalX = canvas.IntervalX,
                IntervalY = canvas.IntervalY,
                IntervalZ = canvas.IntervalZ,
                OffsetX = tile.OriginX / canvas.IntervalX,
                OffsetY = tile.OriginY / canvas.IntervalY,
                CompensationX = 0,
                CompensationY = 0,
                IsFlip = false,
                Width = tile.HaloWidth,
                Height = tile.HaloHeight,
                RawValidPointCount = validRows.Count
            };

            var frame = new ImageFrame(descriptor, grayImage, heightImage, validMask, tile.HaloWidth, tile.HaloHeight, validRows.Count);
            grayImage = null;
            heightImage = null;
            validMask = null;
            return frame;
        }
        finally
        {
            HObjectUtils.DisposeAll(grayImage, heightImage, validMask);
        }
    }

    private static bool FrameMayIntersectTile(
        FrameDescriptor descriptor,
        PointCloudRigidTransform2D transform,
        StitchingCanvasGeometry canvas,
        StitchingTile tile)
    {
        double tileMinX = canvas.MinX + tile.HaloCol * canvas.IntervalX - canvas.IntervalX * 0.5;
        double tileMinY = canvas.MinY + tile.HaloRow * canvas.IntervalY - canvas.IntervalY * 0.5;
        double tileMaxX = canvas.MinX + (tile.HaloCol + tile.HaloWidth - 1) * canvas.IntervalX + canvas.IntervalX * 0.5;
        double tileMaxY = canvas.MinY + (tile.HaloRow + tile.HaloHeight - 1) * canvas.IntervalY + canvas.IntervalY * 0.5;

        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        double width = Math.Max(1, descriptor.Width) - 1;
        double height = Math.Max(1, descriptor.Height) - 1;

        double frameMinX = double.PositiveInfinity;
        double frameMinY = double.PositiveInfinity;
        double frameMaxX = double.NegativeInfinity;
        double frameMaxY = double.NegativeInfinity;

        UpdateFrameCornerBounds(descriptor, transform, cos, sin, 0.0, 0.0, ref frameMinX, ref frameMinY, ref frameMaxX, ref frameMaxY);
        UpdateFrameCornerBounds(descriptor, transform, cos, sin, width, 0.0, ref frameMinX, ref frameMinY, ref frameMaxX, ref frameMaxY);
        UpdateFrameCornerBounds(descriptor, transform, cos, sin, 0.0, height, ref frameMinX, ref frameMinY, ref frameMaxX, ref frameMaxY);
        UpdateFrameCornerBounds(descriptor, transform, cos, sin, width, height, ref frameMinX, ref frameMinY, ref frameMaxX, ref frameMaxY);

        return frameMaxX >= tileMinX && frameMinX <= tileMaxX && frameMaxY >= tileMinY && frameMinY <= tileMaxY;
    }

    private static void UpdateFrameCornerBounds(
        FrameDescriptor descriptor,
        PointCloudRigidTransform2D transform,
        double cos,
        double sin,
        double col,
        double row,
        ref double minX,
        ref double minY,
        ref double maxX,
        ref double maxY)
    {
        double x = col * descriptor.IntervalX;
        double y = row * descriptor.IntervalY;
        double transformedX = transform.X + cos * x - sin * y;
        double transformedY = transform.Y + sin * x + cos * y;

        minX = Math.Min(minX, transformedX);
        minY = Math.Min(minY, transformedY);
        maxX = Math.Max(maxX, transformedX);
        maxY = Math.Max(maxY, transformedY);
    }

    private static HObject CreateByteImage(byte[] data, int width, int height)
    {
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            HOperatorSet.GenImage1(out HObject image, "byte", width, height, handle.AddrOfPinnedObject());
            return image;
        }
        finally
        {
            handle.Free();
        }
    }

    private static HObject CreateRealImage(float[] data, int width, int height)
    {
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            HOperatorSet.GenImage1(out HObject image, "real", width, height, handle.AddrOfPinnedObject());
            return image;
        }
        finally
        {
            handle.Free();
        }
    }

    private static HObject CreateValidMask(IReadOnlyList<int> rows, IReadOnlyList<int> cols)
    {
        if (rows.Count == 0)
        {
            HOperatorSet.GenEmptyRegion(out HObject empty);
            return empty;
        }

        using var rowTuple = new HTuple(rows.ToArray());
        using var colTuple = new HTuple(cols.ToArray());
        HOperatorSet.GenRegionPoints(out HObject region, rowTuple, colTuple);
        return region;
    }

    private static void GetTransformedCanvasBounds(
        IReadOnlyList<FrameDescriptor> frames,
        IReadOnlyList<PointCloudRigidTransform2D> transforms,
        out double minX,
        out double minY,
        out double maxX,
        out double maxY)
    {
        minX = double.PositiveInfinity;
        minY = double.PositiveInfinity;
        maxX = double.NegativeInfinity;
        maxY = double.NegativeInfinity;

        for (int i = 0; i < frames.Count; i++)
        {
            FrameDescriptor descriptor = frames[i];
            PointCloudRigidTransform2D transform = transforms[i];
            double width = Math.Max(1, descriptor.Width) - 1;
            double height = Math.Max(1, descriptor.Height) - 1;

            UpdateBounds(descriptor, 0, 0, transform, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(descriptor, width, 0, transform, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(descriptor, 0, height, transform, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(descriptor, width, height, transform, ref minX, ref minY, ref maxX, ref maxY);
        }

        if (!double.IsFinite(minX) || !double.IsFinite(minY) || !double.IsFinite(maxX) || !double.IsFinite(maxY))
            throw new InvalidOperationException("Unable to compute stitching canvas bounds.");
    }

    private static void UpdateBounds(
        FrameDescriptor descriptor,
        double col,
        double row,
        PointCloudRigidTransform2D transform,
        ref double minX,
        ref double minY,
        ref double maxX,
        ref double maxY)
    {
        double theta = transform.YawDeg * Math.PI / 180.0;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        double x = col * descriptor.IntervalX;
        double y = row * descriptor.IntervalY;
        double transformedX = transform.X + cos * x - sin * y;
        double transformedY = transform.Y + sin * x + cos * y;

        minX = Math.Min(minX, transformedX);
        minY = Math.Min(minY, transformedY);
        maxX = Math.Max(maxX, transformedX);
        maxY = Math.Max(maxY, transformedY);
    }

    private static int ToCanvasSize(double span, double interval, string name)
    {
        double estimate = Math.Max(1.0, Math.Ceiling(span / interval) + 1.0);
        if (!double.IsFinite(estimate) || estimate > int.MaxValue)
            throw new InvalidOperationException($"Stitching canvas {name} is invalid: {estimate:F0}.");

        return (int)estimate;
    }

    private static double ComputeRectangleGrayBlendWeight(int col, int row, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 1.0;

        col = Math.Clamp(col, 0, width - 1);
        row = Math.Clamp(row, 0, height - 1);
        int edgeDistance = Math.Min(
            Math.Min(col, row),
            Math.Min(width - 1 - col, height - 1 - row));
        int featherPixels = Math.Max(1, (Math.Min(width, height) - 1) / 2);
        return Math.Min(1.0, (edgeDistance + 1.0) / (featherPixels + 1.0));
    }

    private static bool IsInvalidDepth(double value, double invalidValue)
    {
        return !double.IsFinite(value) || Math.Abs(value - invalidValue) < 1e-6;
    }

    private static void ValidatePositiveFinite(double value, string name, string source)
    {
        Validation.PositiveFinite(value, name, source);
    }

    private static void ValidateFrameIntervals(IReadOnlyList<FrameDescriptor> frames)
    {
        if (frames.Count == 0)
            throw new InvalidOperationException("Stitching requires at least one corrected frame.");

        FrameDescriptor first = frames[0];
        ValidatePositiveFinite(first.IntervalX, nameof(first.IntervalX), first.GrayImagePath);
        ValidatePositiveFinite(first.IntervalY, nameof(first.IntervalY), first.GrayImagePath);
        ValidatePositiveFinite(first.IntervalZ, nameof(first.IntervalZ), first.GrayImagePath);

        for (int i = 1; i < frames.Count; i++)
        {
            FrameDescriptor current = frames[i];
            ValidatePositiveFinite(current.IntervalX, nameof(current.IntervalX), current.GrayImagePath);
            ValidatePositiveFinite(current.IntervalY, nameof(current.IntervalY), current.GrayImagePath);
            ValidatePositiveFinite(current.IntervalZ, nameof(current.IntervalZ), current.GrayImagePath);
            ValidateIntervalMatches(first.IntervalX, current.IntervalX, nameof(current.IntervalX), first, current);
            ValidateIntervalMatches(first.IntervalY, current.IntervalY, nameof(current.IntervalY), first, current);
            ValidateIntervalMatches(first.IntervalZ, current.IntervalZ, nameof(current.IntervalZ), first, current);
        }
    }

    private static void ValidateIntervalMatches(
        double expected,
        double actual,
        string name,
        FrameDescriptor expectedFrame,
        FrameDescriptor actualFrame)
    {
        if (Math.Abs(expected - actual) <= IntervalTolerance)
            return;

        throw new InvalidOperationException(
            $"Stitching frame interval mismatch: {name} differs between frames. " +
            $"Frame{expectedFrame.Index}={expected:R}, Frame{actualFrame.Index}={actual:R}, " +
            $"Tolerance={IntervalTolerance:R}.");
    }
}

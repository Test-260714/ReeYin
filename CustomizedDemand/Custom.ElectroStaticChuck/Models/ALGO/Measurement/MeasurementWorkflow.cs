using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using Custom.ElectroStaticChuckMeasure.ALGO.Stitching;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Measurement;

public sealed class MeasurementWorkflow
{
    public MeasurementResult Measure(StitchingResult stitching, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(stitching);
        ArgumentNullException.ThrowIfNull(context);
        int frameCount = stitching.Corrected.Frames.Frames.Count;
        if (context.Options.MeasurementMode == ElectroStaticChuckMeasurementMode.StreamingTiles)
        {
            return RunStage(
                ElectroStaticChuckStage.Measurement,
                () => MeasureStreamingTiles(stitching, context));
        }

        if (stitching.StitchedFrame != null)
        {
            return RunStage(
                ElectroStaticChuckStage.Measurement,
                () =>
                {
                    ImageFrame stitchedFrame = stitching.StitchedFrame;
                    context.ReportProgress(new AlgoProgressEvent(
                        ElectroStaticChuckStage.Measurement,
                        "measuring stitched frame"));
                    SensorParameters sensor = CreateFrameSensor(stitchedFrame.Descriptor, context.Parameters.Sensor);
                    var engine = new ConvexMeasurementEngine();
                    MeasurementResult result = engine.Measure(new ConvexMeasurementInput(stitchedFrame, context.Parameters.Measurement, sensor));
                    MapStitchedMeasurementOrigin(result, stitchedFrame.Descriptor);
                    return result;
                });
        }

        if (frameCount != 1)
        {
            throw new InvalidOperationException(
                $"Measurement workflow requires exactly one corrected frame or a materialized stitched frame. FrameCount={frameCount}.");
        }

        return RunStage(
            ElectroStaticChuckStage.Measurement,
            () =>
            {
                FrameDescriptor descriptor = stitching.Corrected.Frames.Frames[0];
                SensorParameters sensor = CreateFrameSensor(descriptor, context.Parameters.Sensor);
                context.ReportProgress(new AlgoProgressEvent(
                    ElectroStaticChuckStage.Measurement,
                    "measuring",
                    FrameIndex: 0,
                    FrameCount: 1));
                using ImageFrame frame = stitching.Corrected.LoadFrame(0, sensor);
                var engine = new ConvexMeasurementEngine();
                return engine.Measure(new ConvexMeasurementInput(frame, context.Parameters.Measurement, sensor));
            });
    }

    private static MeasurementResult MeasureStreamingTiles(StitchingResult stitching, ElectroStaticChuckContext context)
    {
        if (stitching.Corrected.Frames.Frames.Count == 0)
            throw new InvalidOperationException("Streaming tile measurement requires at least one corrected frame.");

        StitchingCanvasGeometry canvas = StitchingRasterizer.PlanCanvas(stitching.Corrected, stitching.Registration);
        int halo = ResolveStreamingTileHalo(canvas, context.Parameters.Measurement);
        ValidateStreamingTileSize(canvas, context.Parameters.Measurement, halo);
        IReadOnlyList<StitchingTile> tiles = StitchingTilePlanner.Plan(
            canvas,
            context.Parameters.Measurement.StreamingTileCoreWidthPixel,
            context.Parameters.Measurement.StreamingTileCoreHeightPixel,
            halo);

        DisplayGeometry display = CreateDisplayGeometry(canvas, stitching.StitchedFrame);
        SensorParameters tileSensor = CreateCanvasSensor(canvas, stitching.Corrected.Frames.Frames[0], context.Parameters.Sensor);
        var allResults = new List<ConvexFeature>();
        foreach (StitchingTile tile in tiles)
        {
            int frameCount = stitching.Corrected.Frames.Frames.Count;
            using ImageFrame tileFrame = StitchingRasterizer.RasterizeTile(
                stitching.Corrected,
                stitching.Registration,
                tileSensor,
                canvas,
                tile,
                (frameIndex, _) => context.ReportProgress(new AlgoProgressEvent(
                    ElectroStaticChuckStage.Measurement,
                    "rasterizing",
                    FrameIndex: frameIndex,
                    FrameCount: frameCount,
                    TileIndex: tile.Index,
                    TileCount: tiles.Count)));

            context.ReportProgress(new AlgoProgressEvent(
                ElectroStaticChuckStage.Measurement,
                "measuring",
                TileIndex: tile.Index,
                TileCount: tiles.Count));
            var engine = new ConvexMeasurementEngine();
            using MeasurementResult tileResult = engine.Measure(new ConvexMeasurementInput(tileFrame, context.Parameters.Measurement, tileSensor));
            allResults.AddRange(MapTileResultsToGlobalCanvas(canvas, display, tile, tileResult));
        }

        MeasurementResult result = CreateEmptyStreamingResult(display);
        result.ConvexResults.AddRange(DeduplicateStreamingTileResults(allResults, context.Parameters.Measurement));
        ComputeGlobalConvexFlatness(result);
        if (stitching.StitchedFrame != null)
            result.DisplayGrayImage = stitching.StitchedFrame.GrayImage.Clone();

        return result;
    }

    private static SensorParameters CreateCanvasSensor(
        StitchingCanvasGeometry canvas,
        FrameDescriptor sourceDescriptor,
        SensorParameters contextSensor)
    {
        return new SensorParameters
        {
            IntervalX = canvas.IntervalX,
            IntervalY = canvas.IntervalY,
            IntervalZ = canvas.IntervalZ,
            MinDepth = sourceDescriptor.MinDepth,
            MaxDepth = sourceDescriptor.MaxDepth,
            InvalidValue = contextSensor.InvalidValue,
            IsFlip = false
        };
    }

    private static DisplayGeometry CreateDisplayGeometry(StitchingCanvasGeometry canvas, ImageFrame? stitchedFrame)
    {
        if (stitchedFrame == null)
        {
            return new DisplayGeometry(
                canvas.MinX,
                canvas.MinY,
                canvas.IntervalX,
                canvas.IntervalY,
                canvas.IntervalZ);
        }

        FrameDescriptor descriptor = stitchedFrame.Descriptor;
        return new DisplayGeometry(
            (descriptor.OffsetX + descriptor.CompensationX) * descriptor.IntervalX,
            (descriptor.OffsetY + descriptor.CompensationY) * descriptor.IntervalY,
            descriptor.IntervalX,
            descriptor.IntervalY,
            descriptor.IntervalZ);
    }

    private static MeasurementResult CreateEmptyStreamingResult(DisplayGeometry display)
    {
        return new MeasurementResult
        {
            IntervalX = display.IntervalX,
            IntervalY = display.IntervalY,
            IntervalZ = display.IntervalZ,
            IsSuccess = true
        };
    }

    private static int ResolveStreamingTileHalo(StitchingCanvasGeometry canvas, MeasurementParameters measurement)
    {
        if (measurement.StreamingTileHaloPixel > 0)
            return measurement.StreamingTileHaloPixel;
        if (!RequiresMultipleStreamingTiles(canvas, measurement))
            return 0;

        return Math.Max(64, ResolveStreamingTileMeasurementMargin(canvas, measurement));
    }

    private static bool RequiresMultipleStreamingTiles(StitchingCanvasGeometry canvas, MeasurementParameters measurement)
    {
        return canvas.Width > measurement.StreamingTileCoreWidthPixel ||
            canvas.Height > measurement.StreamingTileCoreHeightPixel;
    }

    private static int ResolveStreamingTileMeasurementMargin(StitchingCanvasGeometry canvas, MeasurementParameters measurement)
    {
        double diameterPixel = measurement.ConvexStandardDiameter / Math.Min(canvas.IntervalX, canvas.IntervalY);
        return Math.Max(0, (int)Math.Ceiling(diameterPixel * 3.0));
    }

    private static void ValidateStreamingTileSize(StitchingCanvasGeometry canvas, MeasurementParameters measurement, int halo)
    {
        if (measurement.StreamingTileCoreWidthPixel < halo)
            throw new InvalidOperationException("StreamingTileCoreWidthPixel must be greater than or equal to the resolved halo.");
        if (measurement.StreamingTileCoreHeightPixel < halo)
            throw new InvalidOperationException("StreamingTileCoreHeightPixel must be greater than or equal to the resolved halo.");

        int requiredMeasurementMargin = ResolveStreamingTileMeasurementMargin(canvas, measurement);
        if (measurement.StreamingTileHaloPixel > 0 &&
            RequiresMultipleStreamingTiles(canvas, measurement) &&
            halo < requiredMeasurementMargin)
        {
            throw new InvalidOperationException(
                $"StreamingTileHaloPixel={halo} is too small for convex measurement on a multi-tile canvas. " +
                $"RequiredMin={requiredMeasurementMargin}. Increase StreamingTileHaloPixel to at least {requiredMeasurementMargin} or set StreamingTileHaloPixel to 0 to use automatic halo.");
        }

        double diameterPixel = measurement.ConvexStandardDiameter / Math.Min(canvas.IntervalX, canvas.IntervalY);
        int minCore = Math.Max(1, (int)Math.Ceiling(diameterPixel * 4.0));
        if (measurement.StreamingTileCoreWidthPixel < minCore || measurement.StreamingTileCoreHeightPixel < minCore)
        {
            throw new InvalidOperationException(
                $"Streaming tile core is too small for convex measurement. Core={measurement.StreamingTileCoreWidthPixel}x{measurement.StreamingTileCoreHeightPixel}, RequiredMin={minCore}.");
        }

        _ = checked(((long)measurement.StreamingTileCoreWidthPixel + (long)halo * 2) *
            ((long)measurement.StreamingTileCoreHeightPixel + (long)halo * 2));
    }

    private static List<ConvexFeature> MapTileResultsToGlobalCanvas(
        StitchingCanvasGeometry canvas,
        DisplayGeometry display,
        StitchingTile tile,
        MeasurementResult tileResult)
    {
        var accepted = new List<ConvexFeature>();
        foreach (ConvexFeature local in tileResult.ConvexResults)
        {
            double globalX = tile.OriginX + local.PixelX * tileResult.IntervalX;
            double globalY = tile.OriginY + local.PixelY * tileResult.IntervalY;
            double globalCanvasCol = (globalX - canvas.MinX) / canvas.IntervalX;
            double globalCanvasRow = (globalY - canvas.MinY) / canvas.IntervalY;
            if (!IsInsideTileCore(tile, globalCanvasCol, globalCanvasRow))
                continue;

            double displayCol = (globalX - display.OriginX) / display.IntervalX;
            double displayRow = (globalY - display.OriginY) / display.IntervalY;
            accepted.Add(new ConvexFeature
            {
                Height = local.Height,
                Roundness = local.Roundness,
                Diameter = local.Diameter,
                Flatness = local.Flatness,
                PixelX = displayCol,
                PixelY = displayRow,
                X = globalX,
                Y = globalY,
                Z = local.Z,
                ResidualZ = 0
            });
        }

        return accepted;
    }

    private static bool IsInsideTileCore(StitchingTile tile, double globalCanvasCol, double globalCanvasRow)
    {
        return globalCanvasCol >= tile.CoreCol &&
               globalCanvasCol < tile.CoreCol + tile.CoreWidth &&
               globalCanvasRow >= tile.CoreRow &&
               globalCanvasRow < tile.CoreRow + tile.CoreHeight;
    }

    private static List<ConvexFeature> DeduplicateStreamingTileResults(List<ConvexFeature> results, MeasurementParameters measurement)
    {
        double dedupeDistance = Math.Max(1.0, measurement.ConvexStandardDiameter * 0.35);
        var kept = new List<ConvexFeature>();
        foreach (ConvexFeature candidate in results.OrderBy(item => item.Y).ThenBy(item => item.X))
        {
            int duplicateIndex = kept.FindIndex(item =>
            {
                double dx = item.X - candidate.X;
                double dy = item.Y - candidate.Y;
                return Math.Sqrt(dx * dx + dy * dy) <= dedupeDistance;
            });

            if (duplicateIndex < 0)
            {
                kept.Add(candidate);
                continue;
            }

            ConvexFeature current = kept[duplicateIndex];
            if (IsBetterDuplicate(candidate, current))
                kept[duplicateIndex] = candidate;
        }

        return kept.OrderBy(item => item.Y).ThenBy(item => item.X).ToList();
    }

    private static bool IsBetterDuplicate(ConvexFeature candidate, ConvexFeature current)
    {
        bool candidateValidHeight = double.IsFinite(candidate.Height) && candidate.Height > 0;
        bool currentValidHeight = double.IsFinite(current.Height) && current.Height > 0;
        if (candidateValidHeight != currentValidHeight)
            return candidateValidHeight;

        double candidateRoundnessError = Math.Abs(candidate.Roundness - 1.0);
        double currentRoundnessError = Math.Abs(current.Roundness - 1.0);
        if (Math.Abs(candidateRoundnessError - currentRoundnessError) > 1e-9)
            return candidateRoundnessError < currentRoundnessError;

        return candidate.Height > current.Height;
    }

    private static void ComputeGlobalConvexFlatness(MeasurementResult result)
    {
        double[] x = result.ConvexResults.Select(item => item.X).ToArray();
        double[] y = result.ConvexResults.Select(item => item.Y).ToArray();
        double[] z = result.ConvexResults.Select(item => item.Z).ToArray();
        result.ConvexsFlatness = ConvexMeasurementEngine.GetFlatnessV2(x, y, z, out double[] residualZ);
        for (int i = 0; i < result.ConvexResults.Count && i < residualZ.Length; i++)
            result.ConvexResults[i].ResidualZ = residualZ[i];
    }

    private static SensorParameters CreateFrameSensor(FrameDescriptor descriptor, SensorParameters contextSensor)
    {
        return new SensorParameters
        {
            IntervalX = descriptor.IntervalX,
            IntervalY = descriptor.IntervalY,
            IntervalZ = descriptor.IntervalZ,
            MinDepth = descriptor.MinDepth,
            MaxDepth = descriptor.MaxDepth,
            InvalidValue = contextSensor.InvalidValue,
            IsFlip = descriptor.IsFlip
        };
    }

    private static void MapStitchedMeasurementOrigin(MeasurementResult result, FrameDescriptor descriptor)
    {
        double originX = (descriptor.OffsetX + descriptor.CompensationX) * descriptor.IntervalX;
        double originY = (descriptor.OffsetY + descriptor.CompensationY) * descriptor.IntervalY;
        if (Math.Abs(originX) <= 1e-12 && Math.Abs(originY) <= 1e-12)
            return;

        foreach (ConvexFeature feature in result.ConvexResults)
        {
            feature.X = originX + feature.PixelX * result.IntervalX;
            feature.Y = originY + feature.PixelY * result.IntervalY;
        }
    }

    private static T RunStage<T>(ElectroStaticChuckStage stage, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (ElectroStaticChuckException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ElectroStaticChuckException(stage, ex.Message, innerException: ex);
        }
    }

    private static void RunStage(ElectroStaticChuckStage stage, Action action)
    {
        RunStage(
            stage,
            () =>
            {
                action();
                return true;
            });
    }

    private readonly record struct DisplayGeometry(
        double OriginX,
        double OriginY,
        double IntervalX,
        double IntervalY,
        double IntervalZ);
}

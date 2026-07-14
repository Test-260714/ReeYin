using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Measurement;
using Custom.ElectroStaticChuckMeasure.ALGO.Rendering;
using Custom.ElectroStaticChuckMeasure.ALGO.Stitching;
using System.IO;
using HalconDotNet;
using OpenCvSharp;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Export;

public sealed class ExportWorkflow
{
    private const int PlyVertexCountFieldWidth = 20;

    private readonly Func<string, Mat, bool> _writeImage;

    public ExportWorkflow()
        : this(static (path, image) => Cv2.ImWrite(path, image))
    {
    }

    internal ExportWorkflow(Func<string, Mat, bool> writeImage)
    {
        _writeImage = writeImage ?? throw new ArgumentNullException(nameof(writeImage));
    }

    public ExportResult Export(RenderResult? render, ElectroStaticChuckContext context)
    {
        return Export(render, measurement: null, context);
    }

    public ExportResult Export(RenderResult? render, MeasurementResult? measurement, ElectroStaticChuckContext context)
    {
        return Export(render, measurement, stitching: null, context);
    }

    public ExportResult Export(
        RenderResult? render,
        MeasurementResult? measurement,
        StitchingResult? stitching,
        ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureOutputDirectoryForExportableOutput(render, measurement, stitching, context);

        var outputPaths = new List<string>();
        using PreviewExportResult? preview = TryCreatePreviewExport(stitching, measurement, context);
        string? overlayPath = TryWriteOverlay(render, measurement, preview?.Frame, context);
        if (overlayPath != null)
            outputPaths.Add(overlayPath);

        string? csvPath = TryWriteConvexResultsCsv(measurement, context.OutputDirectory);
        if (csvPath != null)
            outputPaths.Add(csvPath);

        string? flatnessPlyPath = TryWriteFlatnessPointCloudPly(measurement, context.OutputDirectory);
        if (flatnessPlyPath != null)
            outputPaths.Add(flatnessPlyPath);

        if (preview != null)
            outputPaths.AddRange(preview.OutputPaths);

        string? stitchingPlyPath = TryWriteStitchingPointCloudPly(stitching, context);
        if (stitchingPlyPath != null)
            outputPaths.Add(stitchingPlyPath);

        return new ExportResult { OutputPaths = outputPaths };
    }

    private static void EnsureOutputDirectoryForExportableOutput(
        RenderResult? render,
        MeasurementResult? measurement,
        StitchingResult? stitching,
        ElectroStaticChuckContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.OutputDirectory))
            return;

        bool hasOverlay = render?.Overlay != null && !render.Overlay.Empty();
        bool hasMeasurementExport = measurement != null;
        bool hasPreviewExport = context.Options.SavePreviewImages && stitching != null;
        bool hasStitchingPlyExport = context.Options.SavePointCloud && stitching != null;
        if (hasOverlay || hasMeasurementExport || hasPreviewExport || hasStitchingPlyExport)
            throw new InvalidOperationException("OutputDirectory is required when exporting ElectroStaticChuck results.");
    }

    private string? TryWriteOverlay(
        RenderResult? render,
        MeasurementResult? measurement,
        ImageFrame? previewFrame,
        ElectroStaticChuckContext context)
    {
        string? outputDirectory = context.OutputDirectory;
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return null;

        if (context.Options.RenderOverlay && measurement != null && previewFrame != null)
        {
            using MeasurementResult previewMeasurement = CreatePreviewMeasurementResult(measurement, previewFrame);
            using Mat previewOverlay = new ResultOverlayRenderer().Render(previewMeasurement);
            return WriteOverlayImage(previewOverlay, outputDirectory, applyDownsample: false, context);
        }

        if (render?.Overlay == null || render.Overlay.Empty())
            return null;

        return WriteOverlayImage(render.Overlay, outputDirectory, applyDownsample: true, context);
    }

    private string WriteOverlayImage(Mat overlay, string outputDirectory, bool applyDownsample, ElectroStaticChuckContext context)
    {
        int downsampleFactor = ValidatePositive(context.Parameters.Export.ImageDownsampleFactor, "ImageDownsampleFactor");
        Directory.CreateDirectory(outputDirectory);
        string overlayPath = Path.Combine(outputDirectory, "MeasureResult.png");
        Mat? resized = null;
        Mat imageToWrite = overlay;
        try
        {
            if (applyDownsample && downsampleFactor > 1)
            {
                int width = Math.Max(1, overlay.Width / downsampleFactor);
                int height = Math.Max(1, overlay.Height / downsampleFactor);
                resized = new Mat();
                Cv2.Resize(overlay, resized, new Size(width, height), 0, 0, InterpolationFlags.Area);
                imageToWrite = resized;
            }

            if (!_writeImage(overlayPath, imageToWrite))
                throw new IOException($"Failed to write overlay image: {overlayPath}");
        }
        finally
        {
            resized?.Dispose();
        }

        if (!File.Exists(overlayPath))
            throw new IOException($"Overlay image was not created: {overlayPath}");

        return overlayPath;
    }

    private static string? TryWriteConvexResultsCsv(MeasurementResult? measurement, string? outputDirectory)
    {
        if (measurement == null || string.IsNullOrWhiteSpace(outputDirectory))
            return null;

        Directory.CreateDirectory(outputDirectory);
        string csvPath = Path.Combine(outputDirectory, "ConvexResults.csv");
        using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
        writer.WriteLine("Index,X,Y,Z,Height,Diameter,Roundness,Flatness,ResidualZ");
        for (int i = 0; i < measurement.ConvexResults.Count; i++)
        {
            ConvexFeature item = measurement.ConvexResults[i];
            writer.WriteLine(string.Join(
                ",",
                i.ToString(CultureInfo.InvariantCulture),
                item.X.ToString("R", CultureInfo.InvariantCulture),
                item.Y.ToString("R", CultureInfo.InvariantCulture),
                item.Z.ToString("R", CultureInfo.InvariantCulture),
                item.Height.ToString("R", CultureInfo.InvariantCulture),
                item.Diameter.ToString("R", CultureInfo.InvariantCulture),
                item.Roundness.ToString("R", CultureInfo.InvariantCulture),
                item.Flatness.ToString("R", CultureInfo.InvariantCulture),
                item.ResidualZ.ToString("R", CultureInfo.InvariantCulture)));
        }

        return csvPath;
    }

    private static string? TryWriteFlatnessPointCloudPly(MeasurementResult? measurement, string? outputDirectory)
    {
        if (measurement == null || string.IsNullOrWhiteSpace(outputDirectory))
            return null;

        List<ConvexFeature> points = measurement.ConvexResults
            .Where(IsValidFlatnessPoint)
            .ToList();

        Directory.CreateDirectory(outputDirectory);
        string plyPath = Path.Combine(outputDirectory, "FlatnessPointCloud.ply");
        using var writer = new StreamWriter(plyPath);
        writer.WriteLine("ply");
        writer.WriteLine("format ascii 1.0");
        writer.WriteLine($"element vertex {points.Count}");
        writer.WriteLine("property float x");
        writer.WriteLine("property float y");
        writer.WriteLine("property float z");
        writer.WriteLine("end_header");

        var culture = CultureInfo.InvariantCulture;
        foreach (ConvexFeature point in points)
            writer.WriteLine($"{point.X.ToString(culture)} {point.Y.ToString(culture)} {point.ResidualZ.ToString(culture)}");

        return plyPath;
    }

    private static bool IsValidFlatnessPoint(ConvexFeature? point)
    {
        return point != null &&
               double.IsFinite(point.X) &&
               double.IsFinite(point.Y) &&
               double.IsFinite(point.ResidualZ);
    }

    private static PreviewExportResult? TryCreatePreviewExport(
        StitchingResult? stitching,
        MeasurementResult? measurement,
        ElectroStaticChuckContext context)
    {
        bool needsPreviewImages = context.Options.SavePreviewImages;
        bool needsOverlayPreview = context.Options.RenderOverlay && measurement != null;
        if ((!needsPreviewImages && !needsOverlayPreview) || stitching == null)
            return null;

        int downsampleFactor = ValidatePositive(context.Parameters.Export.ImageDownsampleFactor, "ImageDownsampleFactor");
        ImageFrame previewFrame = CreatePreviewFrame(stitching, context, downsampleFactor);
        var outputPaths = new List<string>();
        try
        {
            if (needsPreviewImages && !string.IsNullOrWhiteSpace(context.OutputDirectory))
            {
                Directory.CreateDirectory(context.OutputDirectory);
                string grayPath = Path.Combine(context.OutputDirectory, "GrayImage.tiff");
                string heightPath = Path.Combine(context.OutputDirectory, "HeightImage.tiff");
                HOperatorSet.WriteImage(previewFrame.GrayImage, "tiff", 0, grayPath);
                HOperatorSet.WriteImage(previewFrame.HeightImage, "tiff", 0, heightPath);
                outputPaths.Add(grayPath);
                outputPaths.Add(heightPath);
            }

            var result = new PreviewExportResult(previewFrame, outputPaths);
            previewFrame = null!;
            return result;
        }
        finally
        {
            previewFrame?.Dispose();
        }
    }

    private static ImageFrame CreatePreviewFrame(
        StitchingResult stitching,
        ElectroStaticChuckContext context,
        int downsampleFactor)
    {
        if (CanRasterizeUncorrected(stitching))
        {
            int frameCount = stitching.Corrected.Frames.Frames.Count;
            return StitchingRasterizer.RasterizePreview(
                stitching.Corrected,
                stitching.Registration,
                context.Parameters.Sensor,
                downsampleFactor,
                (frameIndex, _) => context.ReportProgress(new AlgoProgressEvent(
                    ElectroStaticChuckStage.Export,
                    "rasterizing preview",
                    FrameIndex: frameIndex,
                    FrameCount: frameCount)));
        }

        if (stitching.StitchedFrame == null)
            throw new InvalidOperationException("Preview image export requires readable source frames or a materialized stitched frame.");

        return CreatePreviewFrameFromStitchedFrame(stitching.StitchedFrame, downsampleFactor, context.Parameters.Sensor.InvalidValue);
    }

    private static ImageFrame CreatePreviewFrameFromStitchedFrame(
        ImageFrame stitchedFrame,
        int downsampleFactor,
        double invalidDepthValue)
    {
        HObject? grayImage = null;
        HObject? heightImage = null;
        HObject? validMask = null;
        try
        {
            if (downsampleFactor == 1)
            {
                grayImage = stitchedFrame.GrayImage.Clone();
                heightImage = stitchedFrame.HeightImage.Clone();
            }
            else
            {
                double scale = 1.0 / downsampleFactor;
                HOperatorSet.ZoomImageFactor(stitchedFrame.GrayImage, out grayImage, scale, scale, "bilinear");
                HOperatorSet.ZoomImageFactor(stitchedFrame.HeightImage, out heightImage, scale, scale, "nearest_neighbor");
            }

            (int width, int height) = GetImageSize(heightImage);
            validMask = CreateValidMaskFromHeight(heightImage, width, height, invalidDepthValue);
            FrameDescriptor source = stitchedFrame.Descriptor;
            double originX = (source.OffsetX + source.CompensationX) * source.IntervalX;
            double originY = (source.OffsetY + source.CompensationY) * source.IntervalY;
            double intervalX = source.IntervalX * downsampleFactor;
            double intervalY = source.IntervalY * downsampleFactor;
            long validPointCount = CountRegionPoints(validMask);
            var descriptor = source with
            {
                IntervalX = intervalX,
                IntervalY = intervalY,
                OffsetX = originX / intervalX,
                OffsetY = originY / intervalY,
                CompensationX = 0,
                CompensationY = 0,
                Width = width,
                Height = height,
                RawValidPointCount = validPointCount
            };

            var frame = new ImageFrame(descriptor, grayImage, heightImage, validMask, width, height, validPointCount);
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

    private static MeasurementResult CreatePreviewMeasurementResult(MeasurementResult measurement, ImageFrame previewFrame)
    {
        FrameDescriptor descriptor = previewFrame.Descriptor;
        double originX = (descriptor.OffsetX + descriptor.CompensationX) * descriptor.IntervalX;
        double originY = (descriptor.OffsetY + descriptor.CompensationY) * descriptor.IntervalY;
        var result = new MeasurementResult
        {
            DisplayGrayImage = previewFrame.GrayImage.Clone(),
            IntervalX = descriptor.IntervalX,
            IntervalY = descriptor.IntervalY,
            IntervalZ = measurement.IntervalZ,
            ConvexsFlatness = measurement.ConvexsFlatness,
            OverallFlatness = measurement.OverallFlatness,
            IsSuccess = measurement.IsSuccess,
            ErrorMessage = measurement.ErrorMessage
        };

        foreach (ConvexFeature feature in measurement.ConvexResults)
            result.ConvexResults.Add(CloneFeatureForPreview(feature, originX, originY, descriptor.IntervalX, descriptor.IntervalY));

        return result;
    }

    private static ConvexFeature CloneFeatureForPreview(
        ConvexFeature feature,
        double originX,
        double originY,
        double intervalX,
        double intervalY)
    {
        return new ConvexFeature
        {
            Height = feature.Height,
            Roundness = feature.Roundness,
            Diameter = feature.Diameter,
            Flatness = feature.Flatness,
            PixelX = double.IsFinite(feature.X) ? (feature.X - originX) / intervalX : feature.PixelX,
            PixelY = double.IsFinite(feature.Y) ? (feature.Y - originY) / intervalY : feature.PixelY,
            X = feature.X,
            Y = feature.Y,
            Z = feature.Z,
            ResidualZ = feature.ResidualZ
        };
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

    private static (int Width, int Height) GetImageSize(HObject image)
    {
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

    private static long CountRegionPoints(HObject region)
    {
        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple row, out HTuple col);
        try
        {
            return Math.Max(0L, (long)Math.Round(area.D));
        }
        finally
        {
            col.Dispose();
            row.Dispose();
            area.Dispose();
        }
    }

    private static string? TryWriteStitchingPointCloudPly(StitchingResult? stitching, ElectroStaticChuckContext context)
    {
        if (!context.Options.SavePointCloud)
            return null;

        if (stitching == null || string.IsNullOrWhiteSpace(context.OutputDirectory))
            return null;

        Directory.CreateDirectory(context.OutputDirectory);
        string plyPath = Path.Combine(context.OutputDirectory, "PointCloudStitching.ply");
        int stride = ValidatePositive(context.Parameters.Export.StitchingPointCloudStride, "StitchingPointCloudStride");
        if (context.Options.MeasurementMode == ElectroStaticChuckMeasurementMode.StreamingTiles &&
            CanRasterizeUncorrected(stitching))
        {
            WriteStreamingStitchingPointCloudPly(plyPath, stitching, context, stride);
            return plyPath;
        }

        if (stitching.StitchedFrame == null)
            throw new InvalidOperationException("PointCloudStitching.ply export requires readable source frames or a materialized stitched frame.");

        ImageFrame? uncorrectedFrame = null;
        try
        {
            ImageFrame frame = stitching.StitchedFrame;
            if (CanRasterizeUncorrected(stitching))
            {
                uncorrectedFrame = StitchingRasterizer.RasterizeUncorrected(stitching.Corrected, stitching.Registration, context.Parameters.Sensor);
                frame = uncorrectedFrame;
            }

            WriteStitchingPointCloudPly(plyPath, frame, context.Parameters.Sensor.InvalidValue, stride);
        }
        finally
        {
            uncorrectedFrame?.Dispose();
        }

        return plyPath;
    }

    private static bool CanRasterizeUncorrected(StitchingResult stitching)
    {
        IReadOnlyList<FrameDescriptor> frames = stitching.Corrected.Frames.Frames;
        if (frames.Count != stitching.Registration.Transforms.Count)
            return false;

        return frames.All(HasReadableFrameImages);
    }

    private static bool HasReadableFrameImages(FrameDescriptor frame)
    {
        return !string.IsNullOrWhiteSpace(frame.GrayImagePath) &&
               !string.IsNullOrWhiteSpace(frame.HeightImagePath) &&
               File.Exists(frame.GrayImagePath) &&
               File.Exists(frame.HeightImagePath);
    }

    private static void WriteStitchingPointCloudPly(string plyPath, ImageFrame frame, double invalidValue, int stride)
    {
        float[] heightData = ReadRealImage(frame.HeightImage, frame.Width, frame.Height);
        int pointCount = CountValidDepthPoints(heightData, frame.Width, frame.Height, invalidValue, stride);

        using var stream = new FileStream(plyPath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteBinaryPlyHeader(stream, pointCount.ToString(CultureInfo.InvariantCulture));

        var descriptor = frame.Descriptor;
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
        for (int row = 0; row < frame.Height; row += stride)
        {
            for (int col = 0; col < frame.Width; col += stride)
            {
                float rawHeight = heightData[row * frame.Width + col];
                if (!IsValidDepth(rawHeight, invalidValue))
                    continue;

                double x = (descriptor.OffsetX + descriptor.CompensationX + col) * descriptor.IntervalX;
                double y = (descriptor.OffsetY + descriptor.CompensationY + row) * descriptor.IntervalY;
                double z = rawHeight * descriptor.IntervalZ;
                writer.Write((float)x);
                writer.Write((float)y);
                writer.Write((float)z);
            }
        }
    }

    private static void WriteStreamingStitchingPointCloudPly(
        string plyPath,
        StitchingResult stitching,
        ElectroStaticChuckContext context,
        int stride)
    {
        string tempPath = plyPath + ".tmp";
        if (File.Exists(tempPath))
            File.Delete(tempPath);

        try
        {
            long vertexCount;
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                long vertexCountOffset = WriteBinaryPlyHeader(stream, new string('0', PlyVertexCountFieldWidth));
                using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

                vertexCount = 0;
                StitchingCanvasGeometry canvas = StitchingRasterizer.PlanCanvas(stitching.Corrected, stitching.Registration);
                int coreWidth = ValidatePositive(context.Parameters.Measurement.StreamingTileCoreWidthPixel, "StreamingTileCoreWidthPixel");
                int coreHeight = ValidatePositive(context.Parameters.Measurement.StreamingTileCoreHeightPixel, "StreamingTileCoreHeightPixel");
                int halo = context.Parameters.Measurement.StreamingTileHaloPixel;
                IReadOnlyList<StitchingTile> tiles = StitchingTilePlanner.Plan(canvas, coreWidth, coreHeight, halo);
                int frameCount = stitching.Corrected.Frames.Frames.Count;
                foreach (StitchingTile tile in tiles)
                {
                    using ImageFrame tileFrame = StitchingRasterizer.RasterizeTile(
                        stitching.Corrected,
                        stitching.Registration,
                        context.Parameters.Sensor,
                        canvas,
                        tile,
                        (frameIndex, _) => context.ReportProgress(new AlgoProgressEvent(
                            ElectroStaticChuckStage.Export,
                            "rasterizing",
                            FrameIndex: frameIndex,
                            FrameCount: frameCount,
                            TileIndex: tile.Index,
                            TileCount: tiles.Count,
                            Operation: "PointCloudStitching")));

                    vertexCount += WriteTileCoreDepthPoints(
                        writer,
                        tileFrame,
                        tile,
                        context.Parameters.Sensor.InvalidValue,
                        stride);
                }

                writer.Flush();
                WriteFixedWidthPlyVertexCount(stream, vertexCountOffset, vertexCount);
            }

            File.Move(tempPath, plyPath, overwrite: true);
        }
        catch
        {
            TryDeleteFile(tempPath);
            throw;
        }
    }

    private static long WriteTileCoreDepthPoints(
        BinaryWriter writer,
        ImageFrame frame,
        StitchingTile tile,
        double invalidValue,
        int stride)
    {
        float[] heightData = ReadRealImage(frame.HeightImage, frame.Width, frame.Height);
        FrameDescriptor descriptor = frame.Descriptor;
        int localCoreCol = tile.CoreCol - tile.HaloCol;
        int localCoreRow = tile.CoreRow - tile.HaloRow;
        int localCoreRight = localCoreCol + tile.CoreWidth;
        int localCoreBottom = localCoreRow + tile.CoreHeight;
        long count = 0;

        for (int row = localCoreRow; row < localCoreBottom; row++)
        {
            int globalRow = tile.HaloRow + row;
            if (globalRow % stride != 0)
                continue;

            for (int col = localCoreCol; col < localCoreRight; col++)
            {
                int globalCol = tile.HaloCol + col;
                if (globalCol % stride != 0)
                    continue;

                float rawHeight = heightData[row * frame.Width + col];
                if (!IsValidDepth(rawHeight, invalidValue))
                    continue;

                double x = (descriptor.OffsetX + descriptor.CompensationX + col) * descriptor.IntervalX;
                double y = (descriptor.OffsetY + descriptor.CompensationY + row) * descriptor.IntervalY;
                double z = rawHeight * descriptor.IntervalZ;
                writer.Write((float)x);
                writer.Write((float)y);
                writer.Write((float)z);
                count++;
            }
        }

        return count;
    }

    private static long WriteBinaryPlyHeader(Stream stream, string vertexCountText)
    {
        byte[] prefixBytes = Encoding.ASCII.GetBytes(
            "ply\n" +
            "format binary_little_endian 1.0\n" +
            "element vertex ");
        byte[] countBytes = Encoding.ASCII.GetBytes(vertexCountText);
        byte[] suffixBytes = Encoding.ASCII.GetBytes(
            "\n" +
            "property float x\n" +
            "property float y\n" +
            "property float z\n" +
            "end_header\n");

        stream.Write(prefixBytes, 0, prefixBytes.Length);
        long vertexCountOffset = stream.Position;
        stream.Write(countBytes, 0, countBytes.Length);
        stream.Write(suffixBytes, 0, suffixBytes.Length);
        return vertexCountOffset;
    }

    private static void WriteFixedWidthPlyVertexCount(Stream stream, long vertexCountOffset, long vertexCount)
    {
        if (vertexCount < 0)
            throw new InvalidOperationException($"PLY vertex count is invalid: {vertexCount}.");

        string text = vertexCount.ToString($"D{PlyVertexCountFieldWidth}", CultureInfo.InvariantCulture);
        if (text.Length != PlyVertexCountFieldWidth)
        {
            throw new InvalidOperationException(
                $"PLY vertex count {vertexCount} exceeds the reserved {PlyVertexCountFieldWidth}-digit header width.");
        }

        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Seek(vertexCountOffset, SeekOrigin.Begin);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup; keep the original export failure intact.
        }
    }

    private static float[] ReadRealImage(HObject image, int width, int height)
    {
        HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple imageWidth, out HTuple imageHeight);
        try
        {
            if (!string.Equals(type.S, "real", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Stitching point cloud export requires a real height image, got {type.S}.");
            if (imageWidth.I != width || imageHeight.I != height)
            {
                throw new InvalidOperationException(
                    $"Stitching point cloud export image size mismatch: Frame={width}x{height}, Image={imageWidth.I}x{imageHeight.I}.");
            }

            float[] data = new float[checked(width * height)];
            Marshal.Copy(pointer.IP, data, 0, data.Length);
            return data;
        }
        finally
        {
            pointer.Dispose();
            type.Dispose();
            imageWidth.Dispose();
            imageHeight.Dispose();
        }
    }

    private static int CountValidDepthPoints(float[] heightData, int width, int height, double invalidValue, int stride)
    {
        int count = 0;
        for (int row = 0; row < height; row += stride)
        {
            for (int col = 0; col < width; col += stride)
            {
                float value = heightData[row * width + col];
                if (IsValidDepth(value, invalidValue))
                    count++;
            }
        }

        return count;
    }

    private static int ValidatePositive(int value, string name)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than or equal to 1.");

        return value;
    }

    private static bool IsValidDepth(float value, double invalidValue)
    {
        return float.IsFinite(value) && Math.Abs(value - invalidValue) >= 1e-6;
    }

    private sealed class PreviewExportResult : IDisposable
    {
        public PreviewExportResult(ImageFrame frame, IReadOnlyList<string> outputPaths)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            OutputPaths = outputPaths ?? throw new ArgumentNullException(nameof(outputPaths));
        }

        public ImageFrame Frame { get; }

        public IReadOnlyList<string> OutputPaths { get; }

        public void Dispose()
        {
            Frame.Dispose();
        }
    }
}

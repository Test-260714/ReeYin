using System.IO;
using System.Globalization;
using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public static class FrameCatalog
{
    public static FrameSet FromFrameDirectory(
        string baseDirectory,
        SensorParameters sensor,
        IAlgoProgressReporter? progressReporter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        if (!Directory.Exists(baseDirectory))
            throw new DirectoryNotFoundException(baseDirectory);

        return FromFrameFolders(GetOrderedFrameFolders(baseDirectory), sensor, progressReporter);
    }

    public static FrameSet FromFrameFolders(
        IReadOnlyList<string> frameFolders,
        SensorParameters sensor,
        IAlgoProgressReporter? progressReporter = null)
    {
        ArgumentNullException.ThrowIfNull(frameFolders);
        ArgumentNullException.ThrowIfNull(sensor);
        if (frameFolders.Count == 0)
            throw new ArgumentException("At least one streaming frame folder is required.", nameof(frameFolders));

        var descriptors = new List<FrameDescriptor>(frameFolders.Count);
        for (int i = 0; i < frameFolders.Count; i++)
        {
            progressReporter?.Report(new AlgoProgressEvent(
                ElectroStaticChuckStage.LoadFrames,
                "reading frame metadata",
                FrameIndex: i,
                FrameCount: frameFolders.Count));
            descriptors.Add(CreateFrameDescriptor(i, frameFolders[i], sensor));
        }

        FrameDescriptor metadataReference = descriptors[0];
        for (int i = 1; i < descriptors.Count; i++)
        {
            FrameDescriptor current = descriptors[i];
            ValidateStreamingFrameMetadataMatches(metadataReference.IntervalX, current.IntervalX, "IntervalX", metadataReference, current);
            ValidateStreamingFrameMetadataMatches(metadataReference.IntervalY, current.IntervalY, "IntervalY", metadataReference, current);
            ValidateStreamingFrameMetadataMatches(metadataReference.IntervalZ, current.IntervalZ, "IntervalZ", metadataReference, current);
            ValidateStreamingFrameMetadataMatches(metadataReference.MinDepth, current.MinDepth, "MinDepth", metadataReference, current);
            ValidateStreamingFrameMetadataMatches(metadataReference.MaxDepth, current.MaxDepth, "MaxDepth", metadataReference, current);
        }

        return new FrameSet(descriptors);
    }

    private static FrameDescriptor CreateFrameDescriptor(int index, string frameFolder, SensorParameters sensor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(frameFolder);
        string grayPath = GetSingleImagePath(Path.Combine(frameFolder, "image"));
        string heightPath = GetSingleImagePath(Path.Combine(frameFolder, "depth"));
        double[] values = ParseFrameValuesFromFileName(grayPath);

        Validation.PositiveFinite(values[0], "IntervalX", grayPath);
        Validation.PositiveFinite(values[1], "IntervalY", grayPath);
        Validation.PositiveFinite(values[2], "IntervalZ", grayPath);
        Validation.Finite(values[3], "MinDepth", grayPath);
        Validation.Finite(values[4], "MaxDepth", grayPath);
        if (values[3] > values[4])
            throw new InvalidOperationException($"MinDepth must be less than or equal to MaxDepth in frame filename: {grayPath}");
        Validation.Finite(values[5], "OffsetX", grayPath);
        Validation.Finite(values[6], "OffsetY", grayPath);
        Validation.Finite(values[7], "CompensationX", grayPath);
        Validation.Finite(values[8], "CompensationY", grayPath);

        double intervalX = values[0];
        double intervalY = values[1];
        var provisional = new FrameDescriptor(
            index,
            frameFolder,
            grayPath,
            heightPath,
            intervalX,
            intervalY,
            values[2],
            values[3],
            values[4],
            values[5] / intervalX,
            values[6] / intervalY,
            values[7] / intervalX,
            values[8] / intervalY,
            sensor.IsFlip,
            0,
            0,
            0);

        using ImageFrame loaded = FrameLoader.Load(provisional, sensor);
        return provisional with
        {
            Width = loaded.Width,
            Height = loaded.Height,
            RawValidPointCount = loaded.ValidPointCount
        };
    }

    private static string[] GetOrderedFrameFolders(string baseDirectory)
    {
        return Directory.GetDirectories(baseDirectory)
            .Select(path =>
            {
                string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) ?? string.Empty;
                bool isNumeric = long.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number);
                return new
                {
                    FolderPath = path,
                    FolderName = name,
                    IsNumeric = isNumeric,
                    Number = number
                };
            })
            .OrderBy(item => item.IsNumeric ? 0 : 1)
            .ThenBy(item => item.Number)
            .ThenBy(item => item.FolderName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.FolderPath)
            .ToArray();
    }

    private static string GetSingleImagePath(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        string[] files = Directory.GetFiles(directory)
            .Where(IsSupportedImageFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
            throw new FileNotFoundException($"Directory contains no image files: {directory}");

        if (files.Length > 1)
            throw new InvalidOperationException($"Directory contains multiple supported image files; expected exactly one: {directory}");

        return files[0];
    }

    private static bool IsSupportedImageFile(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".tif" or ".tiff" or ".png" or ".bmp" or ".jpg" or ".jpeg";
    }

    private static double[] ParseFrameValuesFromFileName(string imagePath)
    {
        string imageName = Path.GetFileNameWithoutExtension(imagePath);
        string[] parts = imageName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 9 && parts.Length != 7)
            throw new InvalidOperationException($"Image filename has too few parameters; at 9 or 7 segments are required: {imagePath}");

        double[] values = new double[9];
        for (int i = 0; i < values.Length; i++)
        {
            if (i >= parts.Length)
            {
                values[i] = 0;
                continue;
            }

            try
            {
                values[i] = double.Parse(parts[i], CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new InvalidOperationException(
                    $"Image filename parameter is not a valid number. SegmentIndex={i}, Value={parts[i]}, Path={imagePath}",
                    ex);
            }
        }

        return values;
    }

    private static void ValidateStreamingFrameMetadataMatches(
        double expected,
        double actual,
        string name,
        FrameDescriptor expectedFrame,
        FrameDescriptor actualFrame)
    {
        if (Math.Abs(expected - actual) <= 1e-9)
            return;

        throw new InvalidOperationException(
            $"Streaming frame filename metadata mismatch: {name} differs between frames. " +
            $"Frame{expectedFrame.Index}={expected:R}, Frame{actualFrame.Index}={actual:R}, " +
            $"Frame{expectedFrame.Index}Path={expectedFrame.GrayImagePath}, Frame{actualFrame.Index}Path={actualFrame.GrayImagePath}");
    }
}

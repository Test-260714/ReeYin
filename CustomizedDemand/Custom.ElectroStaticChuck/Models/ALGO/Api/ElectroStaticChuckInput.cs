namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

public enum ElectroStaticChuckInputKind
{
    FrameDirectory = 0,
    FrameFolders = 1,
    CachedImages = 2
}

public sealed class ElectroStaticChuckInput
{
    private ElectroStaticChuckInput()
    {
    }

    public ElectroStaticChuckInputKind Kind { get; init; }

    public string FrameDirectory { get; init; } = string.Empty;

    public IReadOnlyList<string> FrameFolders { get; init; } = Array.AsReadOnly(Array.Empty<string>());

    public string GrayImagePath { get; init; } = string.Empty;

    public string HeightImagePath { get; init; } = string.Empty;

    public static ElectroStaticChuckInput FromFrameDirectory(string frameDirectory)
    {
        if (string.IsNullOrWhiteSpace(frameDirectory))
            throw new ArgumentException("Frame directory must not be empty.", nameof(frameDirectory));

        return new ElectroStaticChuckInput
        {
            Kind = ElectroStaticChuckInputKind.FrameDirectory,
            FrameDirectory = frameDirectory
        };
    }

    public static ElectroStaticChuckInput FromFrameFolders(IEnumerable<string> frameFolders)
    {
        ArgumentNullException.ThrowIfNull(frameFolders);

        string[] folders = frameFolders.ToArray();
        if (folders.Length == 0)
            throw new ArgumentException("At least one frame folder is required.", nameof(frameFolders));
        if (folders.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Frame folders must not contain empty paths.", nameof(frameFolders));

        return new ElectroStaticChuckInput
        {
            Kind = ElectroStaticChuckInputKind.FrameFolders,
            FrameFolders = Array.AsReadOnly(folders)
        };
    }

    public static ElectroStaticChuckInput FromCachedImages(string grayImagePath, string heightImagePath)
    {
        if (string.IsNullOrWhiteSpace(grayImagePath))
            throw new ArgumentException("Gray image path must not be empty.", nameof(grayImagePath));
        if (string.IsNullOrWhiteSpace(heightImagePath))
            throw new ArgumentException("Height image path must not be empty.", nameof(heightImagePath));

        return new ElectroStaticChuckInput
        {
            Kind = ElectroStaticChuckInputKind.CachedImages,
            GrayImagePath = grayImagePath,
            HeightImagePath = heightImagePath
        };
    }
}

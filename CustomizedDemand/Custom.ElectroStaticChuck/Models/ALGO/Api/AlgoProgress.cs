using System.Text;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;

using ReeYin_V.Logger;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

public interface IAlgoProgressReporter
{
    void Report(AlgoProgressEvent progress);
}

public sealed record AlgoProgressEvent(
    ElectroStaticChuckStage Stage,
    string Action,
    int? FrameIndex = null,
    int? FrameCount = null,
    int? TileIndex = null,
    int? TileCount = null,
    string? Operation = null);

public sealed class ConsoleAlgoProgressReporter : IAlgoProgressReporter
{
    public void Report(AlgoProgressEvent progress)
    {
        Console.WriteLine(Format(progress));
    }

    internal static string Format(AlgoProgressEvent progress)
    {
        var builder = new StringBuilder();
        builder.Append("[ALGO][");
        builder.Append(progress.Stage);
        builder.Append(']');
        if (!string.IsNullOrWhiteSpace(progress.Operation))
        {
            builder.Append('[');
            builder.Append(progress.Operation);
            builder.Append(']');
        }

        bool hasTile = progress.TileIndex.HasValue && progress.TileCount.HasValue;
        bool hasFrame = progress.FrameIndex.HasValue && progress.FrameCount.HasValue;
        if (hasTile)
        {
            builder.Append(" tile ");
            builder.Append(progress.TileIndex!.Value + 1);
            builder.Append('/');
            builder.Append(progress.TileCount!.Value);
        }

        if (hasFrame)
        {
            builder.Append(hasTile ? ", frame " : " frame ");
            builder.Append(progress.FrameIndex!.Value + 1);
            builder.Append('/');
            builder.Append(progress.FrameCount!.Value);
        }

        builder.Append(": ");
        builder.Append(progress.Action);
        return builder.ToString();
    }
}

internal sealed class NullAlgoProgressReporter : IAlgoProgressReporter
{
    public static NullAlgoProgressReporter Instance { get; } = new();

    private NullAlgoProgressReporter()
    {
    }

    public void Report(AlgoProgressEvent progress)
    {
    }
}

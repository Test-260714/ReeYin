using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using System.IO;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public sealed class FrameWorkflow
{
    public FrameSet Load(ElectroStaticChuckInput input, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        return input.Kind switch
        {
            ElectroStaticChuckInputKind.FrameDirectory => FrameCatalog.FromFrameDirectory(input.FrameDirectory, context.Parameters.Sensor, context.ProgressReporter),
            ElectroStaticChuckInputKind.FrameFolders => FrameCatalog.FromFrameFolders(input.FrameFolders, context.Parameters.Sensor, context.ProgressReporter),
            ElectroStaticChuckInputKind.CachedImages => CreateCachedFrameSet(input, context),
            _ => throw new InvalidOperationException($"Unsupported input kind: {input.Kind}")
        };
    }

    private static FrameSet CreateCachedFrameSet(ElectroStaticChuckInput input, ElectroStaticChuckContext context)
    {
        context.ReportProgress(new AlgoProgressEvent(
            ElectroStaticChuckStage.LoadFrames,
            "loading cached image pair",
            FrameIndex: 0,
            FrameCount: 1));

        return new FrameSet(new[]
        {
            new FrameDescriptor(
                0,
                Path.GetDirectoryName(input.GrayImagePath) ?? string.Empty,
                input.GrayImagePath,
                input.HeightImagePath,
                context.Parameters.Sensor.IntervalX,
                context.Parameters.Sensor.IntervalY,
                context.Parameters.Sensor.IntervalZ,
                context.Parameters.Sensor.MinDepth,
                context.Parameters.Sensor.MaxDepth,
                0,
                0,
                0,
                0,
                context.Parameters.Sensor.IsFlip,
                0,
                0,
                0)
        });
    }
}

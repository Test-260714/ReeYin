namespace Custom.ElectroStaticChuckMeasure.ALGO.Common;

public sealed class ElectroStaticChuckError
{
    public ElectroStaticChuckStage Stage { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? FrameId { get; init; }

    public Exception Exception { get; init; } = new InvalidOperationException("Unknown ElectroStaticChuck error.");

    public static ElectroStaticChuckError FromException(ElectroStaticChuckException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ElectroStaticChuckError
        {
            Stage = exception.Stage,
            Message = exception.Message,
            FrameId = exception.FrameId,
            Exception = exception
        };
    }

    public static ElectroStaticChuckError FromException(Exception exception)
    {
        return FromException(exception, ElectroStaticChuckStage.Measurement);
    }

    public static ElectroStaticChuckError FromException(Exception exception, ElectroStaticChuckStage stage)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ElectroStaticChuckError
        {
            Stage = stage,
            Message = exception.Message,
            Exception = exception
        };
    }
}

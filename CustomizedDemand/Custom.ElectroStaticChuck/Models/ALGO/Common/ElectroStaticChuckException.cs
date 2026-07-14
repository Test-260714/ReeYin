namespace Custom.ElectroStaticChuckMeasure.ALGO.Common;

public sealed class ElectroStaticChuckException : Exception
{
    public ElectroStaticChuckException(
        ElectroStaticChuckStage stage,
        string message,
        string? frameId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Stage = stage;
        FrameId = frameId;
    }

    public ElectroStaticChuckStage Stage { get; }

    public string? FrameId { get; }
}

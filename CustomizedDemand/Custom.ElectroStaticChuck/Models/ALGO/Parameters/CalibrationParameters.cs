namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class CalibrationParameters
{
    public bool UsePitchCorrection { get; init; }

    public bool UseYawCorrection { get; init; }
}

namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class ElectroStaticChuckParameters
{
    public SensorParameters Sensor { get; init; } = new();

    public CalibrationParameters Calibration { get; init; } = new();

    public RegistrationParameters Registration { get; init; } = new();

    public MeasurementParameters Measurement { get; init; } = new();

    public RenderingParameters Rendering { get; init; } = new();

    public ExportParameters Export { get; init; } = new();
}

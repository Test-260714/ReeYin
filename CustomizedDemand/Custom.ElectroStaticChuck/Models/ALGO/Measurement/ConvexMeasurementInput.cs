using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Measurement;

public sealed class ConvexMeasurementInput
{
    public ConvexMeasurementInput(ImageFrame frame, MeasurementParameters measurement, SensorParameters sensor)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(sensor);

        Frame = frame;
        Measurement = measurement;
        Sensor = sensor;
    }

    public ImageFrame Frame { get; }

    public MeasurementParameters Measurement { get; }

    public SensorParameters Sensor { get; }
}

namespace Custom.ElectroStaticChuckMeasure.ALGO.Common;

public static class Validation
{
    public static void PositiveFinite(double value, string name, string source)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be finite and positive. Source: {source}");
    }

    public static void Finite(double value, string name, string source)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"{name} must be finite. Value={value}, Source: {source}");
    }
}

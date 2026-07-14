namespace PointCloud.Interop;

public readonly record struct DepthTiffLoadOptions(
    double SpacingX,
    double SpacingY,
    double SpacingZ,
    double InvalidValue,
    bool UseInvalidValue = true)
{
    public void Validate()
    {
        ValidatePositiveFinite(SpacingX, nameof(SpacingX));
        ValidatePositiveFinite(SpacingY, nameof(SpacingY));
        ValidatePositiveFinite(SpacingZ, nameof(SpacingZ));

        if (UseInvalidValue && !double.IsFinite(InvalidValue))
        {
            throw new ArgumentOutOfRangeException(
                nameof(InvalidValue),
                "InvalidValue must be finite when UseInvalidValue is true.");
        }
    }

    private static void ValidatePositiveFinite(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be a finite positive value.");
        }
    }
}

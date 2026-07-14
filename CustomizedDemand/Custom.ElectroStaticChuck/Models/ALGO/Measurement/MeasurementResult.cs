using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Measurement;

public sealed class MeasurementResult : IDisposable
{
    private bool _disposed;
    private HObject _displayGrayImage = new();

    /// <summary>
    /// 拟合得到的凸点区域；由 MeasurementResult 拥有，使用完成后需要 Dispose。
    /// </summary>
    public HObject FitConvexRegion { get; set; } = new();

    /// <summary>
    /// 用于结果绘制的灰度图；由 MeasurementResult 持有，内容来自校正和点间隔归一化之后的测量图。
    /// </summary>
    public HObject DisplayGrayImage
    {
        get => _displayGrayImage;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_displayGrayImage, value))
                return;

            _displayGrayImage.Dispose();
            _displayGrayImage = value;
        }
    }

    public double IntervalX { get; set; } = 1.0;

    public double IntervalY { get; set; } = 1.0;

    public double IntervalZ { get; set; } = 1.0;

    public double ConvexsFlatness { get; set; } = -1.0;

    public double OverallFlatness { get; set; } = -1.0;

    public List<ConvexFeature> ConvexResults { get; } = new();

    public bool IsSuccess { get; set; } = true;

    public string ErrorMessage { get; set; } = string.Empty;

    public void Dispose()
    {
        if (_disposed)
            return;

        FitConvexRegion?.Dispose();
        FitConvexRegion = new HObject();
        _displayGrayImage.Dispose();
        _displayGrayImage = new HObject();
        _disposed = true;
    }
}

public sealed class ConvexFeature
{
    public double Height { get; set; } = -1.0;

    public double Roundness { get; set; } = -1.0;

    public double Diameter { get; set; } = -1.0;

    public double Flatness { get; set; } = -1.0;

    public double PixelX { get; set; }

    public double PixelY { get; set; }

    public double X { get; set; } = double.NegativeInfinity;

    public double Y { get; set; } = double.NegativeInfinity;

    public double Z { get; set; } = double.NegativeInfinity;

    public double ResidualZ { get; set; }
}

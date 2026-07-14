using System.ComponentModel;

namespace PointCloud.VTKWPF.Models;

public sealed class ScalarFieldRenderParameters : INotifyPropertyChanged
{
    private const double Epsilon = 1e-12;

    private double _dataMin;
    private double _dataMax;
    private double _displayMin;
    private double _displayMax;
    private double _saturationMin;
    private double _saturationMax;
    private bool _showOutOfRangeInGray = true;
    private bool _alwaysShowZero;
    private bool _symmetricalScale;
    private bool _logScale;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double DataMin
    {
        get => _dataMin;
        set => SetField(ref _dataMin, value, nameof(DataMin));
    }

    public double DataMax
    {
        get => _dataMax;
        set => SetField(ref _dataMax, value, nameof(DataMax));
    }

    public double DisplayMin
    {
        get => _displayMin;
        set => SetField(ref _displayMin, value, nameof(DisplayMin));
    }

    public double DisplayMax
    {
        get => _displayMax;
        set => SetField(ref _displayMax, value, nameof(DisplayMax));
    }

    public double SaturationMin
    {
        get => _saturationMin;
        set => SetField(ref _saturationMin, value, nameof(SaturationMin));
    }

    public double SaturationMax
    {
        get => _saturationMax;
        set => SetField(ref _saturationMax, value, nameof(SaturationMax));
    }

    public bool ShowOutOfRangeInGray
    {
        get => _showOutOfRangeInGray;
        set => SetField(ref _showOutOfRangeInGray, value, nameof(ShowOutOfRangeInGray));
    }

    public bool AlwaysShowZero
    {
        get => _alwaysShowZero;
        set => SetField(ref _alwaysShowZero, value, nameof(AlwaysShowZero));
    }

    public bool SymmetricalScale
    {
        get => _symmetricalScale;
        set => SetField(ref _symmetricalScale, value, nameof(SymmetricalScale));
    }

    public bool LogScale
    {
        get => _logScale;
        set => SetField(ref _logScale, value, nameof(LogScale));
    }

    public bool IsFlat => Math.Abs(DataMax - DataMin) <= Epsilon;

    public static ScalarFieldRenderParameters CreateDefault(double dataMin, double dataMax)
    {
        var parameters = new ScalarFieldRenderParameters
        {
            DataMin = dataMin,
            DataMax = dataMax,
            ShowOutOfRangeInGray = true,
        };

        parameters.ResetRangesToDefaults();
        return parameters;
    }

    public ScalarFieldRenderParameters Clone()
    {
        return new ScalarFieldRenderParameters
        {
            DataMin = DataMin,
            DataMax = DataMax,
            DisplayMin = DisplayMin,
            DisplayMax = DisplayMax,
            SaturationMin = SaturationMin,
            SaturationMax = SaturationMax,
            ShowOutOfRangeInGray = ShowOutOfRangeInGray,
            AlwaysShowZero = AlwaysShowZero,
            SymmetricalScale = SymmetricalScale,
            LogScale = LogScale,
        };
    }

    public void UpdateDataBounds(double dataMin, double dataMax, bool resetRanges)
    {
        if (!double.IsFinite(dataMin))
        {
            dataMin = 0.0;
        }

        if (!double.IsFinite(dataMax))
        {
            dataMax = 1.0;
        }

        if (dataMax < dataMin)
        {
            (dataMin, dataMax) = (dataMax, dataMin);
        }

        DataMin = dataMin;
        DataMax = dataMax;

        if (resetRanges)
        {
            ResetRangesToDefaults();
        }
        else
        {
            Clamp();
        }
    }

    public void ResetRangesToDefaults()
    {
        DisplayMin = DataMin;
        DisplayMax = DataMax;
        ResetSaturationRangeToDefaults();
    }

    public void ResetSaturationRangeToDefaults()
    {
        (double min, double max) = GetAllowedSaturationBounds();
        SaturationMin = min;
        SaturationMax = max;
    }

    public (double Min, double Max) GetAllowedSaturationBounds()
    {
        if (LogScale)
        {
            double minAbs = GetMinimumAbsoluteValue();
            double maxAbs = GetMaximumAbsoluteValue();
            return (Math.Log10(Math.Max(minAbs, Epsilon)), Math.Log10(Math.Max(maxAbs, Epsilon)));
        }

        if (SymmetricalScale)
        {
            return (GetMinimumAbsoluteValue(), GetMaximumAbsoluteValue());
        }

        return (DataMin, DataMax);
    }

    public void Clamp()
    {
        double dataMin = double.IsFinite(DataMin) ? DataMin : 0.0;
        double dataMax = double.IsFinite(DataMax) ? DataMax : 1.0;
        if (dataMax < dataMin)
        {
            (dataMin, dataMax) = (dataMax, dataMin);
        }

        DataMin = dataMin;
        DataMax = dataMax;

        if (!double.IsFinite(DisplayMin))
        {
            DisplayMin = dataMin;
        }

        if (!double.IsFinite(DisplayMax))
        {
            DisplayMax = dataMax;
        }

        DisplayMin = Math.Clamp(DisplayMin, dataMin, dataMax);
        DisplayMax = Math.Clamp(DisplayMax, dataMin, dataMax);

        if (DisplayMax < DisplayMin)
        {
            DisplayMax = DisplayMin;
        }

        (double saturationBoundMin, double saturationBoundMax) = GetAllowedSaturationBounds();

        if (!double.IsFinite(SaturationMin))
        {
            SaturationMin = saturationBoundMin;
        }

        if (!double.IsFinite(SaturationMax))
        {
            SaturationMax = saturationBoundMax;
        }

        SaturationMin = Math.Clamp(SaturationMin, saturationBoundMin, saturationBoundMax);
        SaturationMax = Math.Clamp(SaturationMax, saturationBoundMin, saturationBoundMax);

        if (SaturationMax < SaturationMin)
        {
            SaturationMax = SaturationMin;
        }
    }

    private double GetMinimumAbsoluteValue()
    {
        if (DataMax < 0)
        {
            return Math.Min(-DataMax, -DataMin);
        }

        return Math.Max(DataMin, 0.0);
    }

    private double GetMaximumAbsoluteValue()
    {
        return Math.Max(Math.Abs(DataMin), Math.Abs(DataMax));
    }

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

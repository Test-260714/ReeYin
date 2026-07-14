using PointCloud.ToolViewer.Models;
using PointCloud.VTKWPF.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointCloud.ToolViewer.Dialogs;

public partial class ScalarFieldDisplayParamsDialog : Window
{
    private readonly ScalarColorAxis _axis;
    private readonly ScalarFieldHistogramData _histogram;
    private bool _updatingInternals;

    public ScalarFieldDisplayParamsDialog(
        ScalarColorAxis axis,
        ScalarFieldRenderParameters initialParameters,
        ScalarFieldHistogramData histogram)
    {
        InitializeComponent();

        _axis = axis;
        _histogram = histogram;
        Parameters = initialParameters.Clone();
        Parameters.PropertyChanged += OnParametersChanged;

        DataContext = this;
        Loaded += OnLoaded;
        HistogramCanvas.SizeChanged += (_, _) => RedrawPreview();
    }

    public ScalarFieldRenderParameters Parameters { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Title = $"SF display params - {GetAxisTitle(_axis)}";
        UpdateRangeEditors();
        UpdateParameterUi();
        RedrawPreview();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Parameters.Clamp();
        DialogResult = true;
    }

    private void OnParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_updatingInternals)
        {
            return;
        }

        if (e.PropertyName is nameof(ScalarFieldRenderParameters.LogScale))
        {
            HandleLogScaleChanged();
            return;
        }

        if (e.PropertyName is nameof(ScalarFieldRenderParameters.SymmetricalScale))
        {
            HandleSymmetricalScaleChanged();
            return;
        }

        Parameters.Clamp();
        UpdateRangeEditors();
        UpdateParameterUi();
        RedrawPreview();
    }

    private void HandleLogScaleChanged()
    {
        _updatingInternals = true;

        if (Parameters.LogScale && Parameters.SymmetricalScale)
        {
            Parameters.SymmetricalScale = false;
        }

        Parameters.ResetSaturationRangeToDefaults();
        Parameters.Clamp();

        _updatingInternals = false;
        UpdateRangeEditors();
        UpdateParameterUi();
        RedrawPreview();
    }

    private void HandleSymmetricalScaleChanged()
    {
        if (Parameters.LogScale)
        {
            return;
        }

        _updatingInternals = true;
        Parameters.ResetSaturationRangeToDefaults();
        Parameters.Clamp();
        _updatingInternals = false;

        UpdateRangeEditors();
        UpdateParameterUi();
        RedrawPreview();
    }

    private void UpdateRangeEditors()
    {
        Parameters.Clamp();

        double displayStep = GetSuggestedStep(Parameters.DataMin, Parameters.DataMax);
        DisplayMinNumeric.Minimum = Parameters.DataMin;
        DisplayMinNumeric.Maximum = Parameters.DisplayMax;
        DisplayMinNumeric.Increment = displayStep;

        DisplayMaxNumeric.Minimum = Parameters.DisplayMin;
        DisplayMaxNumeric.Maximum = Parameters.DataMax;
        DisplayMaxNumeric.Increment = displayStep;

        (double saturationMin, double saturationMax) = Parameters.GetAllowedSaturationBounds();
        double saturationStep = GetSuggestedStep(saturationMin, saturationMax);
        SaturationMinNumeric.Minimum = saturationMin;
        SaturationMinNumeric.Maximum = Parameters.SaturationMax;
        SaturationMinNumeric.Increment = saturationStep;

        SaturationMaxNumeric.Minimum = Parameters.SaturationMin;
        SaturationMaxNumeric.Maximum = saturationMax;
        SaturationMaxNumeric.Increment = saturationStep;
    }

    private void UpdateParameterUi()
    {
        AlwaysShowZeroCheckBox.IsEnabled = !Parameters.LogScale;
        SymmetricalScaleCheckBox.IsEnabled = !Parameters.LogScale;
        SaturationLabel.Text = Parameters.LogScale
            ? "log sat."
            : Parameters.SymmetricalScale
                ? "abs. sat."
                : "saturation";

        string mode = Parameters.LogScale
            ? "log"
            : Parameters.SymmetricalScale
                ? "symmetrical"
                : "linear";
        string outOfRange = Parameters.ShowOutOfRangeInGray ? "grey" : "hidden";

        SummaryTextBlock.Text =
            $"Axis: {GetAxisTitle(_axis)}\n" +
            $"Mode: {mode}\n" +
            $"Displayed: {Parameters.DisplayMin:F6} .. {Parameters.DisplayMax:F6}\n" +
            $"Saturation: {Parameters.SaturationMin:F6} .. {Parameters.SaturationMax:F6}\n" +
            $"Out of range: {outOfRange}";
    }

    private void RedrawPreview()
    {
        HistogramCanvas.Children.Clear();

        double width = HistogramCanvas.ActualWidth;
        double height = HistogramCanvas.ActualHeight;
        if (width < 40 || height < 40)
        {
            return;
        }

        const double left = 16;
        const double right = 16;
        const double top = 18;
        const double bottom = 22;

        double plotWidth = Math.Max(1.0, width - left - right);
        double plotHeight = Math.Max(1.0, height - top - bottom);
        double plotBottom = top + plotHeight;

        DrawGrid(left, top, plotWidth, plotHeight);
        DrawHistogram(left, top, plotWidth, plotHeight);
        DrawDisplayMarkers(left, top, plotWidth, plotHeight);
        DrawSaturationMarkers(left, plotBottom, plotWidth);

        if (Parameters.AlwaysShowZero && Parameters.DataMin <= 0 && Parameters.DataMax >= 0)
        {
            double x = MapValueToX(0.0, left, plotWidth);
            var zeroLine = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = top,
                Y2 = plotBottom,
                Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 3 },
            };
            HistogramCanvas.Children.Add(zeroLine);
        }
    }

    private void DrawGrid(double left, double top, double plotWidth, double plotHeight)
    {
        var outline = new Rectangle
        {
            Width = plotWidth,
            Height = plotHeight,
            Stroke = new SolidColorBrush(Color.FromRgb(188, 188, 188)),
            StrokeThickness = 1.0,
            Fill = Brushes.Transparent,
        };
        Canvas.SetLeft(outline, left);
        Canvas.SetTop(outline, top);
        HistogramCanvas.Children.Add(outline);

        for (int i = 1; i < 6; i++)
        {
            double x = left + plotWidth * i / 6.0;
            var line = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = top,
                Y2 = top + plotHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 4 },
            };
            HistogramCanvas.Children.Add(line);
        }

        for (int i = 1; i < 3; i++)
        {
            double y = top + plotHeight * i / 3.0;
            var line = new Line
            {
                X1 = left,
                X2 = left + plotWidth,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 2, 4 },
            };
            HistogramCanvas.Children.Add(line);
        }
    }

    private void DrawHistogram(double left, double top, double plotWidth, double plotHeight)
    {
        if (_histogram.Bins.Count == 0 || _histogram.MaxBinValue <= 0)
        {
            return;
        }

        double barWidth = plotWidth / _histogram.Bins.Count;
        var brush = new SolidColorBrush(Color.FromRgb(255, 106, 0));

        for (int i = 0; i < _histogram.Bins.Count; i++)
        {
            double normalized = (double)_histogram.Bins[i] / _histogram.MaxBinValue;
            double barHeight = normalized * (plotHeight - 6);
            var rect = new Rectangle
            {
                Width = Math.Max(1.0, barWidth - 1.0),
                Height = Math.Max(1.0, barHeight),
                Fill = brush,
            };

            Canvas.SetLeft(rect, left + i * barWidth);
            Canvas.SetTop(rect, top + plotHeight - barHeight);
            HistogramCanvas.Children.Add(rect);
        }
    }

    private void DrawDisplayMarkers(double left, double top, double plotWidth, double plotHeight)
    {
        DrawMarker(
            MapValueToX(Parameters.DisplayMin, left, plotWidth),
            top,
            Color.FromRgb(62, 92, 230),
            true,
            plotHeight);
        DrawMarker(
            MapValueToX(Parameters.DisplayMax, left, plotWidth),
            top,
            Color.FromRgb(184, 165, 0),
            true,
            plotHeight);
    }

    private void DrawSaturationMarkers(double left, double plotBottom, double plotWidth)
    {
        foreach ((double value, Color color) in GetPreviewSaturationMarkers())
        {
            DrawMarker(
                MapValueToX(value, left, plotWidth),
                plotBottom,
                color,
                false,
                plotBottom - 18);
        }
    }

    private IEnumerable<(double Value, Color Color)> GetPreviewSaturationMarkers()
    {
        if (Parameters.LogScale)
        {
            double start = Math.Pow(10.0, Parameters.SaturationMin);
            double stop = Math.Pow(10.0, Parameters.SaturationMax);

            if (Parameters.DataMin < 0)
            {
                yield return (-stop, Color.FromRgb(196, 82, 0));
                if (Parameters.DataMax > 0)
                {
                    yield return (-start, Color.FromRgb(222, 142, 34));
                }
            }

            if (Parameters.DataMax > 0)
            {
                yield return (start, Color.FromRgb(76, 149, 25));
                yield return (stop, Color.FromRgb(50, 176, 34));
            }

            yield break;
        }

        if (Parameters.SymmetricalScale)
        {
            yield return (-Parameters.SaturationMax, Color.FromRgb(196, 82, 0));
            if (Parameters.SaturationMin > 0)
            {
                yield return (-Parameters.SaturationMin, Color.FromRgb(222, 142, 34));
                yield return (Parameters.SaturationMin, Color.FromRgb(131, 180, 24));
            }
            yield return (Parameters.SaturationMax, Color.FromRgb(50, 176, 34));
            yield break;
        }

        yield return (Parameters.SaturationMin, Color.FromRgb(196, 82, 0));
        yield return (Parameters.SaturationMax, Color.FromRgb(50, 176, 34));
    }

    private void DrawMarker(double x, double yAnchor, Color color, bool fromTop, double lineLength)
    {
        const double markerHalfWidth = 7;
        const double markerHeight = 12;

        var triangle = new Polygon
        {
            Fill = new SolidColorBrush(color),
            Stroke = Brushes.White,
            StrokeThickness = 0.8,
            Points = fromTop
                ? new PointCollection
                {
                    new Point(x - markerHalfWidth, yAnchor),
                    new Point(x + markerHalfWidth, yAnchor),
                    new Point(x, yAnchor + markerHeight),
                }
                : new PointCollection
                {
                    new Point(x - markerHalfWidth, yAnchor),
                    new Point(x + markerHalfWidth, yAnchor),
                    new Point(x, yAnchor - markerHeight),
                },
        };
        HistogramCanvas.Children.Add(triangle);

        var line = new Line
        {
            X1 = x,
            X2 = x,
            Y1 = fromTop ? yAnchor + markerHeight : yAnchor - markerHeight,
            Y2 = fromTop ? yAnchor + lineLength : lineLength,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 1.3,
        };
        HistogramCanvas.Children.Add(line);
    }

    private double MapValueToX(double value, double left, double plotWidth)
    {
        double min = _histogram.Minimum;
        double max = _histogram.Maximum;
        if (max <= min)
        {
            return left + plotWidth * 0.5;
        }

        double clamped = Math.Clamp(value, min, max);
        return left + (clamped - min) / (max - min) * plotWidth;
    }

    private static double GetSuggestedStep(double min, double max)
    {
        double range = Math.Abs(max - min);
        if (range <= 1e-12)
        {
            return 0.001;
        }

        return Math.Max(range / 1000.0, 1e-6);
    }

    private static string GetAxisTitle(ScalarColorAxis axis)
    {
        return axis switch
        {
            ScalarColorAxis.X => "X",
            ScalarColorAxis.Y => "Y",
            ScalarColorAxis.Z => "Z",
            _ => "None",
        };
    }
}

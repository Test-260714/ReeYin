using Arction.Wpf.Charting;
using Arction.Wpf.Charting.SeriesXY;
using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.DefectOverview.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Custom.DefectOverview.Views
{
    public partial class LightningBandMapView : UserControl
    {
        private const double HitTestThresholdDip = 14.0;
        private const double NormalPointSizeDip = 8.0;
        private const double AggregateBucketDip = 8.0;
        private const double AggregateBadgeOffsetX = 7.0;
        private const double AggregateBadgeOffsetY = -22.0;
        private static readonly TimeSpan ChartUpdateMinInterval =
            TimeSpan.FromMilliseconds(DefectOverviewRuntimeOptions.UiRefreshIntervalMs);

        private LightningChart _chart;
        private BandMapViewModel _viewModel;
        private bool _isLoaded;
        private bool _updateQueued;
        private readonly DispatcherTimer _chartUpdateTimer;
        private IReadOnlyDictionary<BandMapPointItem, DisplayPoint> _displayPointsByPoint = new Dictionary<BandMapPointItem, DisplayPoint>();
        private IReadOnlyList<DisplayPoint> _visibleDisplayPoints = Array.Empty<DisplayPoint>();
        private DateTime _lastChartUpdateUtc = DateTime.MinValue;

        public event EventHandler<BandMapPointSelectedEventArgs> PointSelected;

        public LightningBandMapView()
        {
            InitializeComponent();

            _chartUpdateTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
            _chartUpdateTimer.Tick += OnChartUpdateTimerTick;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
            ChartHost.PreviewMouseLeftButtonDown += OnChartMouseLeftButtonDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            EnsureChart();
            AttachViewModel(DataContext as BandMapViewModel);
            QueueChartUpdate();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            _chartUpdateTimer.Stop();
            _updateQueued = false;
            AttachViewModel(null);
            BadgeOverlay.Children.Clear();

            if (_chart == null)
                return;

            ChartHost.Children.Clear();
            _chart.Dispose();
            _chart = null;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as BandMapViewModel);
            QueueChartUpdate();
        }

        private void AttachViewModel(BandMapViewModel viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
                return;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.DefectPoints.CollectionChanged -= OnMapCollectionChanged;
                _viewModel.GuideLines.CollectionChanged -= OnMapCollectionChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.DefectPoints.CollectionChanged += OnMapCollectionChanged;
                _viewModel.GuideLines.CollectionChanged += OnMapCollectionChanged;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(BandMapViewModel.ViewportStartMeters)
                || e.PropertyName == nameof(BandMapViewModel.WindowMeters)
                || e.PropertyName == nameof(BandMapViewModel.PlotLeft)
                || e.PropertyName == nameof(BandMapViewModel.PlotWidth)
                || e.PropertyName == nameof(BandMapViewModel.CumulativeMeters)
                || e.PropertyName == nameof(BandMapViewModel.PointDisplayMode))
            {
                QueueChartUpdate();
            }
        }

        private void OnMapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueChartUpdate();
        }

        private void EnsureChart()
        {
            if (_chart != null)
                return;

            try
            {
                _chart = new LightningChart
                {
                    ActiveView = ActiveView.ViewXY,
                    ChartName = "DefectOverviewBandMapChart",
                    Background = Brushes.Transparent
                };

                _chart.Title.Visible = false;
                _chart.ChartBackground.Color = Color.FromRgb(168, 175, 183);
                _chart.ViewXY.GraphBackground = new Fill
                {
                    Style = RectFillStyle.ColorOnly,
                    Color = Color.FromRgb(168, 175, 183)
                };
                _chart.ViewXY.Margins = new Thickness(42, 8, 8, 78);

                if (_chart.ViewXY.LegendBoxes.Count > 0)
                    _chart.ViewXY.LegendBoxes[0].Visible = false;

                DisableBuiltInZoomPan();
                ConfigureAxes(0.0, 1.0);

                ChartHost.Children.Add(_chart);
            }
            catch (Exception ex)
            {
                EmptyChartHint.Text = $"LightningChart 初始化失败：{ex.Message}";
                EmptyChartHint.Visibility = Visibility.Visible;
            }
        }

        private void DisableBuiltInZoomPan()
        {
            var options = _chart.ViewXY.ZoomPanOptions;
            options.WheelZooming = WheelZooming.Off;
            options.AxisWheelAction = AxisWheelAction.None;
            options.RightToLeftZoomAction = RightToLeftZoomActionXY.Off;
            options.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
            options.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction.None;
            options.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction.None;
            options.MultiTouchPanEnabled = false;
            options.MultiTouchZoomEnabled = false;
        }

        private void QueueChartUpdate()
        {
            if (!_isLoaded || _updateQueued)
                return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(QueueChartUpdate), DispatcherPriority.Background);
                return;
            }

            _updateQueued = true;
            TimeSpan delay = GetChartUpdateDelay();
            if (delay <= TimeSpan.Zero)
            {
                Dispatcher.BeginInvoke(new Action(RunQueuedChartUpdate), DispatcherPriority.Background);
                return;
            }

            _chartUpdateTimer.Interval = delay;
            _chartUpdateTimer.Start();
        }

        private TimeSpan GetChartUpdateDelay()
        {
            if (_lastChartUpdateUtc == DateTime.MinValue)
                return TimeSpan.Zero;

            TimeSpan elapsed = DateTime.UtcNow - _lastChartUpdateUtc;
            TimeSpan remaining = ChartUpdateMinInterval - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void OnChartUpdateTimerTick(object sender, EventArgs e)
        {
            _chartUpdateTimer.Stop();
            RunQueuedChartUpdate();
        }

        private void RunQueuedChartUpdate()
        {
            _chartUpdateTimer.Stop();
            _updateQueued = false;
            if (!_isLoaded)
                return;

            UpdateChart();
            _lastChartUpdateUtc = DateTime.UtcNow;
        }

        private void UpdateChart()
        {
            EnsureChart();

            if (_chart == null || _viewModel == null)
                return;

            bool updateStarted = false;
            try
            {
                double startMeters = Math.Max(0.0, _viewModel.ViewportStartMeters);
                double windowMeters = Math.Max(1.0, _viewModel.WindowMeters);
                double endMeters = startMeters + windowMeters;
                var points = _viewModel.DefectPoints
                    .Where(item => item != null
                        && double.IsFinite(item.XPercent)
                        && double.IsFinite(item.MeterValue))
                    .ToArray();
                DisplayPointLayout displayLayout = BuildDisplayPointLayout(points, startMeters, windowMeters);

                _chart.BeginUpdate();
                updateStarted = true;
                _displayPointsByPoint = displayLayout.PointsBySource;
                _visibleDisplayPoints = displayLayout.VisiblePoints;

                ConfigureAxes(startMeters, endMeters);
                _chart.ViewXY.FreeformPointLineSeries.Clear();

                AddGuideLineSeries(startMeters, endMeters);
                AddDefectPointSeries(displayLayout.VisiblePoints);
                AddSelectedPointSeries(points.FirstOrDefault(item => item.IsSelected), displayLayout.PointsBySource);
                RefreshAggregateBadges(displayLayout.VisiblePoints);

                EmptyChartHint.Text = "当前窗口暂无缺陷点";
                EmptyChartHint.Visibility = points.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                BadgeOverlay.Children.Clear();
                EmptyChartHint.Text = $"地图刷新失败：{ex.Message}";
                EmptyChartHint.Visibility = Visibility.Visible;
            }
            finally
            {
                if (updateStarted)
                    _chart.EndUpdate();
            }
        }

        private DisplayPointLayout BuildDisplayPointLayout(
            BandMapPointItem[] points,
            double startMeters,
            double windowMeters)
        {
            var layout = new DisplayPointLayout();
            if (points == null || points.Length == 0)
                return layout;

            double plotLeft = _viewModel?.PlotLeft ?? 0.0;
            double plotTop = _viewModel?.PlotTop ?? 0.0;
            double plotWidth = _viewModel?.PlotWidth ?? 0.0;
            double plotHeight = _viewModel?.PlotHeight ?? 0.0;
            bool canAggregate = _viewModel?.PointDisplayMode == BandMapPointDisplayMode.Aggregate
                && plotWidth > 1.0
                && plotHeight > 1.0
                && windowMeters > 0.0;
            List<DisplayPoint> rawPoints = new(points.Length);

            foreach (BandMapPointItem point in points)
            {
                double xPercent = Math.Clamp(point.XPercent, 0.0, 100.0);
                double meterValue = point.MeterValue;
                double centerX = plotWidth > 1.0
                    ? plotLeft + xPercent / 100.0 * plotWidth
                    : point.X + point.Size / 2.0;
                double centerY = plotHeight > 1.0 && windowMeters > 0.0
                    ? plotTop + (1.0 - (meterValue - startMeters) / windowMeters) * plotHeight
                    : point.Y + point.Size / 2.0;
                var displayPoint = new DisplayPoint
                {
                    Point = point,
                    SourcePoints = new[] { point },
                    XPercent = xPercent,
                    MeterValue = meterValue,
                    CenterX = centerX,
                    CenterY = centerY
                };
                rawPoints.Add(displayPoint);
            }

            if (!canAggregate)
            {
                foreach (DisplayPoint point in rawPoints)
                {
                    layout.VisiblePoints.Add(point);
                    layout.PointsBySource[point.Point] = point;
                }

                return layout;
            }

            foreach (var group in rawPoints.GroupBy(item => BuildAggregateBucketKey(item.CenterX, item.CenterY)))
            {
                DisplayPoint[] groupedPoints = group.ToArray();
                DisplayPoint representative = groupedPoints[0];
                BandMapPointItem[] sourcePoints = groupedPoints
                    .Select(item => item.Point)
                    .Where(item => item != null)
                    .ToArray();
                var aggregatePoint = new DisplayPoint
                {
                    Point = representative.Point,
                    SourcePoints = sourcePoints,
                    XPercent = representative.XPercent,
                    MeterValue = representative.MeterValue,
                    CenterX = representative.CenterX,
                    CenterY = representative.CenterY
                };
                layout.VisiblePoints.Add(aggregatePoint);
                foreach (BandMapPointItem sourcePoint in sourcePoints)
                {
                    layout.PointsBySource[sourcePoint] = aggregatePoint;
                }
            }

            return layout;
        }

        private static string BuildAggregateBucketKey(double x, double y)
        {
            return $"{Math.Round(x / AggregateBucketDip)}|{Math.Round(y / AggregateBucketDip)}";
        }

        private void ConfigureAxes(double startMeters, double endMeters)
        {
            var view = _chart.ViewXY;
            var xAxis = view.XAxes[0];
            var yAxis = view.YAxes[0];
            ApplyChartMargins(view);
            Color axisLabelAccentColor = Color.FromRgb(248, 250, 252);
            Color axisLineAccentColor = Color.FromRgb(198, 146, 0);

            xAxis.SetRange(0.0, 100.0);
            xAxis.Title.Text = "幅宽 (%)";
            xAxis.Title.Color = axisLabelAccentColor;
            xAxis.AxisColor = axisLineAccentColor;
            xAxis.AxisThickness = 1.6;
            xAxis.LabelsColor = axisLabelAccentColor;
            xAxis.LabelsFont.Family = new FontFamily("Bahnschrift");
            xAxis.LabelsFont.Size = 13.0;
            xAxis.LabelsFont.Bold = false;
            xAxis.LabelsNumberFormat = "0";
            xAxis.MajorDiv = 10.0;
            xAxis.MajorDivTickStyle.Color = axisLineAccentColor;
            xAxis.MajorDivTickStyle.LineLength = 6;
            xAxis.MajorGrid.Visible = false;

            yAxis.SetRange(startMeters, Math.Max(startMeters + 0.001, endMeters));
            yAxis.Title.Text = string.Empty;
            yAxis.AxisColor = axisLineAccentColor;
            yAxis.AxisThickness = 1.6;
            yAxis.LabelsColor = axisLabelAccentColor;
            yAxis.LabelsFont.Family = new FontFamily("Bahnschrift");
            yAxis.LabelsFont.Size = 13.0;
            yAxis.LabelsFont.Bold = false;
            yAxis.LabelsNumberFormat = "0.0";
            yAxis.MajorDivTickStyle.Color = axisLineAccentColor;
            yAxis.MajorDivTickStyle.LineLength = 6;
            yAxis.MajorGrid.Visible = true;
            yAxis.MajorGrid.Color = Color.FromRgb(95, 102, 112);
        }

        private void AddGuideLineSeries(double startMeters, double endMeters)
        {
            if (_viewModel.PlotWidth <= 0)
                return;

            foreach (var guideLine in _viewModel.GuideLines)
            {
                double xPercent = (guideLine.X1 - _viewModel.PlotLeft) / _viewModel.PlotWidth * 100.0;
                if (!double.IsFinite(xPercent))
                    continue;

                xPercent = Math.Clamp(xPercent, 0.0, 100.0);
                var series = CreateBaseSeries();
                series.LineVisible = true;
                series.PointsVisible = false;
                series.LineStyle.Color = Color.FromRgb(91, 99, 112);
                series.LineStyle.Width = Math.Max(1.0, guideLine.StrokeThickness);
                series.Points = new[]
                {
                    new SeriesPoint(xPercent, startMeters),
                    new SeriesPoint(xPercent, endMeters)
                };
                _chart.ViewXY.FreeformPointLineSeries.Add(series);
            }
        }

        private void AddDefectPointSeries(IReadOnlyList<DisplayPoint> points)
        {
            if (points == null || points.Count == 0)
                return;

            foreach (var group in points.GroupBy(item => BuildSeriesKey(item.Point)))
            {
                BandMapPointItem first = group.First().Point;
                var series = CreateBaseSeries();
                series.LineVisible = false;
                series.PointsVisible = true;
                series.PointsType = PointsType.Points;
                series.PointStyle.Shape = ResolveShape(first.MarkerKind);
                series.PointStyle.Width = ResolveNormalPointSize(first.MarkerKind);
                series.PointStyle.Height = ResolveNormalPointSize(first.MarkerKind);
                series.PointStyle.Color1 = ResolveColor(first.Fill, Color.FromRgb(239, 68, 68));
                series.PointStyle.GradientFill = GradientFillPoint.Solid;
                series.Title.Text = string.IsNullOrWhiteSpace(first.ClassName) ? first.LegendKey : first.ClassName;
                series.Points = group
                    .Select(item =>
                    {
                        return new SeriesPoint(
                            item.XPercent,
                            item.MeterValue,
                            item.Point,
                            ResolveColor(item.Point.Fill, Color.FromRgb(239, 68, 68)));
                    })
                    .ToArray();

                _chart.ViewXY.FreeformPointLineSeries.Add(series);
            }
        }

        private void AddSelectedPointSeries(
            BandMapPointItem selectedPoint,
            IReadOnlyDictionary<BandMapPointItem, DisplayPoint> displayPoints)
        {
            if (selectedPoint == null)
                return;

            DisplayPoint displayPoint = ResolveDisplayPoint(selectedPoint, displayPoints);
            var series = CreateBaseSeries();
            series.LineVisible = false;
            series.PointsVisible = true;
            series.PointsType = PointsType.Points;
            series.PointStyle.Shape = ResolveShape(selectedPoint.MarkerKind);
            series.PointStyle.Width = Math.Max(12.0, selectedPoint.Size * 0.55);
            series.PointStyle.Height = Math.Max(12.0, selectedPoint.Size * 0.55);
            series.PointStyle.Color1 = ResolveColor(selectedPoint.Stroke, Color.FromRgb(255, 214, 10));
            series.PointStyle.GradientFill = GradientFillPoint.Solid;
            series.Points = new[]
            {
                new SeriesPoint(
                    displayPoint.XPercent,
                    displayPoint.MeterValue,
                    selectedPoint,
                    ResolveColor(selectedPoint.Stroke, Color.FromRgb(255, 214, 10)))
            };

            _chart.ViewXY.FreeformPointLineSeries.Add(series);
        }

        private void RefreshAggregateBadges(IReadOnlyList<DisplayPoint> displayPoints)
        {
            BadgeOverlay.Children.Clear();
            if (_viewModel?.PointDisplayMode != BandMapPointDisplayMode.Aggregate || displayPoints == null)
                return;

            foreach (DisplayPoint displayPoint in displayPoints.Where(item => item?.SourcePoints?.Count > 1))
            {
                var badge = new System.Windows.Controls.Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(5, 1, 5, 2),
                    Child = new TextBlock
                    {
                        Text = $"x{displayPoint.SourcePoints.Count}",
                        Foreground = Brushes.White,
                        FontSize = 10.5,
                        FontWeight = FontWeights.SemiBold
                    }
                };

                Point chartPoint = ResolveChartPoint(displayPoint);
                Canvas.SetLeft(badge, Math.Max(0.0, chartPoint.X + AggregateBadgeOffsetX));
                Canvas.SetTop(badge, Math.Max(0.0, chartPoint.Y + AggregateBadgeOffsetY));
                BadgeOverlay.Children.Add(badge);
            }
        }

        private FreeformPointLineSeries CreateBaseSeries()
        {
            var view = _chart.ViewXY;
            var series = new FreeformPointLineSeries(view, view.XAxes[0], view.YAxes[0])
            {
                IncludeInAutoFit = false,
                ShowInLegendBox = false,
                CursorTrackEnabled = false,
                Highlight = Highlight.None
            };
            series.Title.Visible = false;
            return series;
        }

        private void OnChartMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_chart == null)
                return;

            var position = e.GetPosition(ChartHost);
            var hitPoint = ResolveNearestPoint(position);
            if (hitPoint == null)
                return;

            PointSelected?.Invoke(this, new BandMapPointSelectedEventArgs(hitPoint.Point, position, hitPoint.SourcePoints));
            e.Handled = true;
        }

        private DisplayPoint ResolveNearestPoint(Point position)
        {
            DisplayPoint bestPoint = null;
            double bestDistance = double.MaxValue;

            if (_viewModel == null)
                return null;

            foreach (DisplayPoint point in _visibleDisplayPoints)
            {
                if (point == null || !double.IsFinite(point.CenterX) || !double.IsFinite(point.CenterY))
                    continue;

                Point chartPoint = ResolveChartPoint(point);
                double centerX = chartPoint.X;
                double centerY = chartPoint.Y;
                double distance = Math.Sqrt(Math.Pow(centerX - position.X, 2.0) + Math.Pow(centerY - position.Y, 2.0));

                if (!double.IsFinite(distance) || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestPoint = point;
            }

            return bestDistance <= HitTestThresholdDip ? bestPoint : null;
        }

        private Point ResolveChartPoint(DisplayPoint point)
        {
            if (_chart == null || point == null)
                return new Point(point?.CenterX ?? 0.0, point?.CenterY ?? 0.0);

            var view = _chart.ViewXY;
            double x = view.XAxes[0].ValueToCoordD(point.XPercent, true);
            double y = view.YAxes[0].ValueToCoord(point.MeterValue, true);

            if (!double.IsFinite(x) || !double.IsFinite(y))
                return new Point(point.CenterX, point.CenterY);

            return new Point(x, y);
        }

        private static DisplayPoint ResolveDisplayPoint(
            BandMapPointItem point,
            IReadOnlyDictionary<BandMapPointItem, DisplayPoint> displayPoints)
        {
            if (point != null && displayPoints != null && displayPoints.TryGetValue(point, out DisplayPoint displayPoint))
                return displayPoint;

            return new DisplayPoint
            {
                Point = point,
                SourcePoints = point == null ? Array.Empty<BandMapPointItem>() : new[] { point },
                XPercent = Math.Clamp(point?.XPercent ?? 0.0, 0.0, 100.0),
                MeterValue = point?.MeterValue ?? 0.0,
                CenterX = (point?.X ?? 0.0) + (point?.Size ?? 0.0) / 2.0,
                CenterY = (point?.Y ?? 0.0) + (point?.Size ?? 0.0) / 2.0
            };
        }

        private void ApplyChartMargins(Arction.Wpf.Charting.Views.ViewXY.ViewXY view)
        {
            if (_viewModel == null
                || _viewModel.CanvasWidth <= 1
                || _viewModel.CanvasHeight <= 1
                || _viewModel.PlotWidth <= 1
                || _viewModel.PlotHeight <= 1)
            {
                return;
            }

            double right = Math.Max(0.0, _viewModel.CanvasWidth - _viewModel.PlotLeft - _viewModel.PlotWidth);
            double bottom = Math.Max(0.0, _viewModel.CanvasHeight - _viewModel.PlotTop - _viewModel.PlotHeight);
            view.Margins = new Thickness(
                Math.Max(0.0, _viewModel.PlotLeft),
                Math.Max(0.0, _viewModel.PlotTop),
                right,
                bottom);
        }

        private static string BuildSeriesKey(BandMapPointItem item)
        {
            return string.Join("|",
                item.LegendKey ?? string.Empty,
                item.MarkerKind ?? string.Empty,
                ResolveColor(item.Fill, Colors.Transparent).ToString());
        }

        private static Shape ResolveShape(string markerKind)
        {
            return string.Equals(markerKind, "Cross", StringComparison.OrdinalIgnoreCase)
                ? Shape.Cross
                : string.Equals(markerKind, "Triangle", StringComparison.OrdinalIgnoreCase)
                    ? Shape.Triangle
                    : Shape.Circle;
        }

        private static double ResolveNormalPointSize(string markerKind)
        {
            return string.Equals(markerKind, "Cross", StringComparison.OrdinalIgnoreCase)
                ? NormalPointSizeDip + 2.0
                : NormalPointSizeDip;
        }

        private static Color ResolveColor(Brush brush, Color fallback)
        {
            if (brush is SolidColorBrush solidColorBrush)
                return solidColorBrush.Color;

            return fallback;
        }
    }

    internal sealed class DisplayPoint
    {
        public BandMapPointItem Point { get; init; }

        public IReadOnlyList<BandMapPointItem> SourcePoints { get; init; } = Array.Empty<BandMapPointItem>();

        public double XPercent { get; init; }

        public double MeterValue { get; init; }

        public double CenterX { get; init; }

        public double CenterY { get; init; }
    }

    internal sealed class DisplayPointLayout
    {
        public Dictionary<BandMapPointItem, DisplayPoint> PointsBySource { get; } = new();

        public List<DisplayPoint> VisiblePoints { get; } = new();
    }

    public sealed class BandMapPointSelectedEventArgs : EventArgs
    {
        public BandMapPointSelectedEventArgs(
            BandMapPointItem point,
            Point position,
            IReadOnlyList<BandMapPointItem> groupedPoints = null)
        {
            Point = point;
            Position = position;
            GroupedPoints = groupedPoints ?? (point == null ? Array.Empty<BandMapPointItem>() : new[] { point });
        }

        public BandMapPointItem Point { get; }

        public Point Position { get; }

        public IReadOnlyList<BandMapPointItem> GroupedPoints { get; }
    }
}

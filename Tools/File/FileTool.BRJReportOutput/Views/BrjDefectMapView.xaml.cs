using Arction.Wpf.Charting;
using Arction.Wpf.Charting.SeriesXY;
using FileTool.BRJReportOutput.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FileTool.BRJReportOutput.Views
{
    public partial class BrjDefectMapView : UserControl
    {
        private const double HitTestThresholdDip = 14.0;
        private const double NormalPointSizeDip = 8.0;
        private const int OverviewSegmentCount = 5;
        private const double NormalHitSizeDip = 20.0;
        private const double SelectedHitSizeDip = 28.0;
        private static readonly Thickness ChartMargins = new(44, 8, 30, 56);

        private LightningChart? _chart;
        private INotifyCollectionChanged? _pointsNotifyCollection;
        private bool _isLoaded;
        private bool _updateQueued;
        private bool _isSyncingViewportScrollBar;
        private double _viewportStartMeters;
        private double _viewportLengthMeters = double.PositiveInfinity;
        private double _currentLengthM = 1.0;

        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register(nameof(Points), typeof(IEnumerable<BrjDefectMapPoint>), typeof(BrjDefectMapView), new PropertyMetadata(null, OnPointsChanged));

        public static readonly DependencyProperty GuideLinePercentsProperty =
            DependencyProperty.Register(nameof(GuideLinePercents), typeof(IEnumerable<double>), typeof(BrjDefectMapView), new PropertyMetadata(null, OnGuideLinePercentsChanged));

        public static readonly DependencyProperty SelectedPointProperty =
            DependencyProperty.Register(nameof(SelectedPoint), typeof(BrjDefectMapPoint), typeof(BrjDefectMapView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPointChanged));

        public static readonly DependencyProperty LengthMProperty =
            DependencyProperty.Register(nameof(LengthM), typeof(double), typeof(BrjDefectMapView), new PropertyMetadata(1.0, OnMapRangeChanged));

        public IEnumerable<BrjDefectMapPoint>? Points
        {
            get { return (IEnumerable<BrjDefectMapPoint>?)GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        public IEnumerable<double>? GuideLinePercents
        {
            get { return (IEnumerable<double>?)GetValue(GuideLinePercentsProperty); }
            set { SetValue(GuideLinePercentsProperty, value); }
        }

        public BrjDefectMapPoint? SelectedPoint
        {
            get { return (BrjDefectMapPoint?)GetValue(SelectedPointProperty); }
            set { SetValue(SelectedPointProperty, value); }
        }

        public double LengthM
        {
            get { return (double)GetValue(LengthMProperty); }
            set { SetValue(LengthMProperty, value); }
        }

        public BrjDefectMapView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            ChartHost.PreviewMouseLeftButtonDown += OnChartMouseLeftButtonDown;
            PreviewMouseWheel += OnMapMouseWheel;
        }

        private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (BrjDefectMapView)d;
            view.AttachPoints(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged);
            view.ResetViewport();
            view.QueueChartUpdate();
        }

        private static void OnGuideLinePercentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BrjDefectMapView)d).QueueChartUpdate();
        }

        private static void OnSelectedPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (BrjDefectMapView)d;
            view.EnsurePointVisible(e.NewValue as BrjDefectMapPoint);
            view.QueueChartUpdate();
        }

        private static void OnMapRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (BrjDefectMapView)d;
            view.ResetViewport();
            view.QueueChartUpdate();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            EnsureChart();
            AttachPoints(null, Points as INotifyCollectionChanged);
            QueueChartUpdate();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            AttachPoints(_pointsNotifyCollection, null);

            if (_chart == null)
            {
                return;
            }

            ChartHost.Children.Clear();
            _chart.Dispose();
            _chart = null;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueChartUpdate();
        }

        private void AttachPoints(INotifyCollectionChanged? oldCollection, INotifyCollectionChanged? newCollection)
        {
            if (ReferenceEquals(_pointsNotifyCollection, newCollection))
            {
                return;
            }

            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= OnPointsCollectionChanged;
            }

            _pointsNotifyCollection = newCollection;

            if (_pointsNotifyCollection != null)
            {
                _pointsNotifyCollection.CollectionChanged += OnPointsCollectionChanged;
            }
        }

        private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            QueueChartUpdate();
        }

        private void EnsureChart()
        {
            if (_chart != null)
            {
                return;
            }

            try
            {
                _chart = new LightningChart
                {
                    ActiveView = ActiveView.ViewXY,
                    ChartName = "BrjDefectMapChart",
                    Background = Brushes.Transparent,
                };

                _chart.Title.Visible = false;
                _chart.ChartBackground.Color = Color.FromRgb(168, 175, 183);
                _chart.ViewXY.GraphBackground = new Fill
                {
                    Style = RectFillStyle.ColorOnly,
                    Color = Color.FromRgb(168, 175, 183),
                };
                _chart.ViewXY.Margins = ChartMargins;

                if (_chart.ViewXY.LegendBoxes.Count > 0)
                {
                    _chart.ViewXY.LegendBoxes[0].Visible = false;
                }

                DisableBuiltInZoomPan();
                ConfigureAxes(1.0);
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
            var chart = _chart;
            if (chart == null)
            {
                return;
            }

            var options = chart.ViewXY.ZoomPanOptions;
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
            {
                return;
            }

            _updateQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _updateQueued = false;
                if (_isLoaded)
                {
                    UpdateChart();
                }
            }), DispatcherPriority.Render);
        }

        private void UpdateChart()
        {
            EnsureChart();

            if (_chart == null)
            {
                return;
            }

            bool updateStarted = false;
            try
            {
                var points = ResolvePoints();
                double lengthM = ResolveLength(points);
                _currentLengthM = lengthM;
                ClampViewport();
                var visiblePoints = ResolveVisiblePoints(points);

                _chart.BeginUpdate();
                updateStarted = true;

                ConfigureAxes(lengthM);
                SyncViewportScrollBar();
                _chart.ViewXY.FreeformPointLineSeries.Clear();

                AddGuideLineSeries(lengthM);
                AddDefectPointSeries(visiblePoints);
                AddSelectedPointSeries(visiblePoints.FirstOrDefault(IsSelectedPoint));

                EmptyChartHint.Text = "当前批次暂无缺陷点";
                EmptyChartHint.Visibility = visiblePoints.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                EmptyChartHint.Text = $"地图刷新失败：{ex.Message}";
                EmptyChartHint.Visibility = Visibility.Visible;
            }
            finally
            {
                if (updateStarted)
                {
                    _chart.EndUpdate();
                }
            }

        }

        private List<BrjDefectMapPoint> ResolvePoints()
        {
            return Points == null
                ? new List<BrjDefectMapPoint>()
                : Points.Where(item => item != null && double.IsFinite(item.XPercent) && double.IsFinite(item.MeterValue)).ToList();
        }

        private List<BrjDefectMapPoint> ResolveVisiblePoints(IEnumerable<BrjDefectMapPoint> points)
        {
            double endMeters = _viewportStartMeters + _viewportLengthMeters;
            return points
                .Where(item => item.MeterValue >= _viewportStartMeters && item.MeterValue <= endMeters)
                .ToList();
        }

        private double ResolveLength(IEnumerable<BrjDefectMapPoint> points)
        {
            double maxPointMeter = points.Select(item => item.MeterValue).DefaultIfEmpty(0.0).Max();
            return Math.Max(1.0, Math.Max(LengthM, maxPointMeter));
        }

        private static double ResolveOverviewX(BrjDefectMapPoint item)
        {
            return Math.Clamp(item.XPercent, 0.0, 100.0);
        }

        private double ResolveOverviewY(BrjDefectMapPoint item)
        {
            return Math.Clamp(Math.Max(0.0, item.MeterValue), 0.0, _currentLengthM);
        }

        private void ResetViewport()
        {
            _viewportStartMeters = 0.0;
            _viewportLengthMeters = double.PositiveInfinity;
        }

        private void ClampViewport()
        {
            if (!double.IsFinite(_viewportLengthMeters))
            {
                _viewportLengthMeters = _currentLengthM;
            }

            _viewportLengthMeters = Math.Clamp(_viewportLengthMeters, ResolveMinViewportLength(), _currentLengthM);
            _viewportStartMeters = Math.Clamp(_viewportStartMeters, 0.0, Math.Max(0.0, _currentLengthM - _viewportLengthMeters));
        }

        private double ResolveMinViewportLength()
        {
            return Math.Min(_currentLengthM, Math.Max(0.001, _currentLengthM / 20.0));
        }

        private void ConfigureAxes(double lengthM)
        {
            var chart = _chart;
            if (chart == null)
            {
                return;
            }

            var view = chart.ViewXY;
            var xAxis = view.XAxes[0];
            var yAxis = view.YAxes[0];
            view.Margins = ChartMargins;

            xAxis.SetRange(0.0, 100.0);
            xAxis.Title.Text = "幅宽 (%)";
            xAxis.Title.Color = Color.FromRgb(71, 85, 105);
            xAxis.AxisColor = Color.FromRgb(91, 99, 112);
            xAxis.LabelsColor = Color.FromRgb(71, 85, 105);
            xAxis.LabelsNumberFormat = "0";
            xAxis.MajorDiv = 10.0;
            xAxis.MajorGrid.Visible = false;

            yAxis.SetRange(_viewportStartMeters, Math.Max(_viewportStartMeters + 0.001, _viewportStartMeters + _viewportLengthMeters));
            yAxis.Title.Text = string.Empty;
            yAxis.AxisColor = Color.FromRgb(91, 99, 112);
            yAxis.LabelsColor = Color.FromRgb(71, 85, 105);
            yAxis.LabelsNumberFormat = "0.0";
            yAxis.MajorDiv = Math.Max(0.001, _viewportLengthMeters / OverviewSegmentCount);
            yAxis.MajorGrid.Visible = true;
            yAxis.MajorGrid.Color = Color.FromRgb(95, 102, 112);
        }

        private void AddGuideLineSeries(double lengthM)
        {
            var chart = _chart;
            if (chart == null)
            {
                return;
            }

            foreach (double xPercent in ResolveGuideLinePercents())
            {
                var series = CreateBaseSeries();
                series.LineVisible = true;
                series.PointsVisible = false;
                series.LineStyle.Color = Color.FromRgb(91, 99, 112);
                series.LineStyle.Width = 1.0;
                series.Points = new[]
                {
                    new SeriesPoint(xPercent, _viewportStartMeters),
                    new SeriesPoint(xPercent, _viewportStartMeters + _viewportLengthMeters),
                };
                chart.ViewXY.FreeformPointLineSeries.Add(series);
            }
        }

        private void AddDefectPointSeries(List<BrjDefectMapPoint> points)
        {
            var chart = _chart;
            if (chart == null)
            {
                return;
            }

            foreach (var group in points.GroupBy(BuildSeriesKey))
            {
                var first = group.First();
                var series = CreateBaseSeries();
                series.LineVisible = false;
                series.PointsVisible = true;
                series.PointsType = PointsType.Points;
                series.PointStyle.Shape = ResolveShape(first.MarkerKind);
                series.PointStyle.Width = ResolveNormalPointSize(first.MarkerKind);
                series.PointStyle.Height = ResolveNormalPointSize(first.MarkerKind);
                series.PointStyle.Color1 = ResolveColor(first.Fill, Color.FromRgb(239, 68, 68));
                series.PointStyle.GradientFill = GradientFillPoint.Solid;
                series.Title.Text = first.DefectType;
                series.Points = group
                    .Select(item => new SeriesPoint(
                        ResolveOverviewX(item),
                        ResolveOverviewY(item),
                        item,
                        ResolveColor(item.Fill, Color.FromRgb(239, 68, 68))))
                    .ToArray();

                chart.ViewXY.FreeformPointLineSeries.Add(series);
            }
        }

        private void AddSelectedPointSeries(BrjDefectMapPoint? selectedPoint)
        {
            var chart = _chart;
            if (selectedPoint == null || chart == null)
            {
                return;
            }

            var series = CreateBaseSeries();
            series.LineVisible = false;
            series.PointsVisible = true;
            series.PointsType = PointsType.Points;
            series.PointStyle.Shape = ResolveShape(selectedPoint.MarkerKind);
            series.PointStyle.Width = Math.Max(12.0, NormalPointSizeDip * 1.75);
            series.PointStyle.Height = Math.Max(12.0, NormalPointSizeDip * 1.75);
            series.PointStyle.Color1 = ResolveColor(selectedPoint.Stroke, Color.FromRgb(255, 214, 10));
            series.PointStyle.GradientFill = GradientFillPoint.Solid;
            series.Points = new[]
            {
                new SeriesPoint(
                    ResolveOverviewX(selectedPoint),
                    ResolveOverviewY(selectedPoint),
                    selectedPoint,
                    ResolveColor(selectedPoint.Stroke, Color.FromRgb(255, 214, 10))),
            };

            chart.ViewXY.FreeformPointLineSeries.Add(series);
        }

        private FreeformPointLineSeries CreateBaseSeries()
        {
            var chart = _chart ?? throw new InvalidOperationException("LightningChart is not initialized.");
            var view = chart.ViewXY;
            var series = new FreeformPointLineSeries(view, view.XAxes[0], view.YAxes[0])
            {
                IncludeInAutoFit = false,
                ShowInLegendBox = false,
                CursorTrackEnabled = false,
                Highlight = Highlight.None,
            };
            series.Title.Visible = false;
            return series;
        }

        private void OnChartMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = ResolveNearestPoint(e.GetPosition(ChartHost));
            if (point == null)
            {
                return;
            }

            SelectedPoint = point;
            e.Handled = true;
        }

        private void OnMapMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_currentLengthM <= 1.0)
            {
                return;
            }

            ClampViewport();
            double oldLength = _viewportLengthMeters;
            double zoomFactor = e.Delta > 0 ? 0.75 : 1.25;
            double newLength = Math.Clamp(oldLength * zoomFactor, ResolveMinViewportLength(), _currentLengthM);
            if (Math.Abs(newLength - oldLength) < 0.001)
            {
                return;
            }

            double ratio = ResolveMouseYRatio(e.GetPosition(ChartHost));
            double anchorMeters = _viewportStartMeters + oldLength * ratio;
            _viewportLengthMeters = newLength;
            _viewportStartMeters = anchorMeters - newLength * ratio;
            ClampViewport();
            QueueChartUpdate();
            e.Handled = true;
        }

        private void OnViewportScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSyncingViewportScrollBar)
            {
                return;
            }

            double maxStart = Math.Max(0.0, _currentLengthM - _viewportLengthMeters);
            _viewportStartMeters = Math.Clamp(maxStart - Math.Clamp(e.NewValue, 0.0, maxStart), 0.0, maxStart);
            QueueChartUpdate();
        }

        private void SyncViewportScrollBar()
        {
            double maxStart = Math.Max(0.0, _currentLengthM - _viewportLengthMeters);
            _isSyncingViewportScrollBar = true;
            try
            {
                ViewportScrollBar.Maximum = maxStart;
                ViewportScrollBar.ViewportSize = Math.Max(1.0, _viewportLengthMeters);
                ViewportScrollBar.SmallChange = Math.Max(1.0, _viewportLengthMeters / 10.0);
                ViewportScrollBar.LargeChange = Math.Max(1.0, _viewportLengthMeters * 0.8);
                ViewportScrollBar.IsEnabled = maxStart > 0.001;
                ViewportScrollBar.Value = Math.Clamp(maxStart - _viewportStartMeters, 0.0, maxStart);
            }
            finally
            {
                _isSyncingViewportScrollBar = false;
            }
        }

        private double ResolveMouseYRatio(Point position)
        {
            double height = Math.Max(1.0, ChartHost.ActualHeight - ChartMargins.Top - ChartMargins.Bottom);
            double y = Math.Clamp(position.Y - ChartMargins.Top, 0.0, height);
            return Math.Clamp(1.0 - y / height, 0.0, 1.0);
        }

        private void EnsurePointVisible(BrjDefectMapPoint? point)
        {
            if (point == null || !double.IsFinite(point.MeterValue))
            {
                return;
            }

            double meter = Math.Clamp(Math.Max(0.0, point.MeterValue), 0.0, _currentLengthM);
            if (meter >= _viewportStartMeters && meter <= _viewportStartMeters + _viewportLengthMeters)
            {
                return;
            }

            _viewportStartMeters = meter - _viewportLengthMeters / 2.0;
            ClampViewport();
        }

        private BrjDefectMapPoint? ResolveNearestPoint(Point position)
        {
            var points = ResolveVisiblePoints(ResolvePoints());
            if (points.Count == 0)
            {
                return null;
            }

            BrjDefectMapPoint? bestPoint = null;
            double bestDistance = double.MaxValue;

            foreach (var point in points)
            {
                if (!TryResolvePointVisualBounds(point, out double x, out double y, out double size))
                {
                    continue;
                }

                double centerX = x + size / 2.0;
                double centerY = y + size / 2.0;
                double distance = Math.Sqrt(Math.Pow(centerX - position.X, 2.0) + Math.Pow(centerY - position.Y, 2.0));

                if (!double.IsFinite(distance) || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestPoint = point;
            }

            return bestDistance <= HitTestThresholdDip ? bestPoint : null;
        }

        private bool TryResolvePointVisualBounds(BrjDefectMapPoint point, out double x, out double y, out double size)
        {
            x = 0.0;
            y = 0.0;
            size = 0.0;

            var chart = _chart;
            if (chart == null)
            {
                return false;
            }

            var view = chart.ViewXY;
            double centerX = view.XAxes[0].ValueToCoordD(ResolveOverviewX(point), true);
            double centerY = view.YAxes[0].ValueToCoord(ResolveOverviewY(point), true);
            size = IsSelectedPoint(point) ? SelectedHitSizeDip : NormalHitSizeDip;
            x = SnapVisualCoordinate(centerX - size / 2.0);
            y = SnapVisualCoordinate(centerY - size / 2.0);
            return true;
        }

        private static double SnapVisualCoordinate(double value)
        {
            return double.IsFinite(value) ? Math.Round(value, MidpointRounding.AwayFromZero) + 0.5 : 0.5;
        }

        private bool IsSelectedPoint(BrjDefectMapPoint? point)
        {
            return point != null
                && SelectedPoint != null
                && point.Source != null
                && SelectedPoint.Source != null
                && point.Source.Id == SelectedPoint.Source.Id;
        }

        private static string BuildSeriesKey(BrjDefectMapPoint item)
        {
            return string.Join("|",
                item.DefectType ?? string.Empty,
                item.MarkerKind ?? string.Empty,
                ResolveColor(item.Fill, Colors.Transparent).ToString());
        }

        private IEnumerable<double> ResolveGuideLinePercents()
        {
            return GuideLinePercents == null
                ? Enumerable.Empty<double>()
                : GuideLinePercents.Where(double.IsFinite).Select(item => Math.Clamp(item, 0.0, 100.0)).Distinct();
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
            return brush is SolidColorBrush solidColorBrush ? solidColorBrush.Color : fallback;
        }
    }
}

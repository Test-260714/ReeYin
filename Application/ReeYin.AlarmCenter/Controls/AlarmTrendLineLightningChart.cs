using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using ReeYin.AlarmCenter.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ChartActiveView = Arction.Wpf.ChartingMVVM.ActiveView;

namespace ReeYin.AlarmCenter.Controls
{
    public sealed class AlarmTrendLineLightningChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(AlarmTrendLineLightningChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

        private readonly Grid _root = new Grid();
        private readonly TextBlock _fallbackText;
        private LightningChart? _chart;
        private ViewXY? _view;
        private INotifyCollectionChanged? _collectionChanged;
        private bool _renderScheduled;
        private string? _lastRenderedSignature;
        private bool _isRenderDirty = true;

        public AlarmTrendLineLightningChart()
        {
            Focusable = false;
            IsHitTestVisible = false;
            Content = _root;

            _fallbackText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.SlateGray,
                Text = "图表准备中...",
                Visibility = Visibility.Collapsed
            };
            _root.Children.Add(_fallbackText);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AlarmTrendLineLightningChart chart)
            {
                return;
            }

            chart.DetachCollectionChanged();
            if (chart.IsLoaded)
            {
                chart.AttachCollectionChanged();
            }

            chart.MarkRenderDirty();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            MarkRenderDirty();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ScheduleRender();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Mouse.Capture(null);
            DetachCollectionChanged();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                ScheduleRender();
                return;
            }

            _isRenderDirty = true;
        }

        private void EnsureChart()
        {
            if (_chart != null && _view != null)
            {
                return;
            }

            if (_chart != null || _view != null)
            {
                _chart = null;
                _view = null;
                _lastRenderedSignature = null;
            }

            for (int index = _root.Children.Count - 1; index >= 0; index--)
            {
                if (_root.Children[index] is LightningChart)
                {
                    _root.Children.RemoveAt(index);
                    _lastRenderedSignature = null;
                }
            }

            LightningChart chart = new LightningChart
            {
                ActiveView = ChartActiveView.ViewXY,
                Focusable = false,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ChartBackground = CreateSolidFill(Colors.White)
            };
            ViewXY view = CreateView(chart);
            chart.ViewXY = view;
            _root.Children.Insert(0, chart);
            _chart = chart;
            _view = view;
        }

        private void MarkRenderDirty()
        {
            _isRenderDirty = true;
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (_renderScheduled)
            {
                return;
            }

            if (!IsLoaded)
            {
                _isRenderDirty = true;
                return;
            }

            if (!IsVisible)
            {
                _isRenderDirty = true;
                return;
            }

            _renderScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _renderScheduled = false;
                RenderSafely();
            }), DispatcherPriority.ContextIdle);
        }

        private void RenderSafely()
        {
            if (!IsLoaded)
            {
                _isRenderDirty = true;
                return;
            }

            if (!IsVisible)
            {
                _isRenderDirty = true;
                return;
            }

            try
            {
                AttachCollectionChanged();
                EnsureChart();
                List<AlarmTrendPoint> items = GetTrendItems();
                int maximum = GetMaximum(items);
                string currentSignature = BuildDataSignature(items, maximum);
                if (string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal))
                {
                    if (_isRenderDirty)
                    {
                        _isRenderDirty = false;
                    }

                    HideFallback();
                    return;
                }

                if (!RenderChart(items, maximum))
                {
                    _lastRenderedSignature = null;
                    _isRenderDirty = true;
                    ShowFallback("图表准备中...");
                    return;
                }

                _lastRenderedSignature = currentSignature;
                _isRenderDirty = false;
                HideFallback();
            }
            catch (Exception ex)
            {
                _lastRenderedSignature = null;
                _isRenderDirty = true;
                ShowFallback($"LightningChart 初始化失败：{ex.Message}");
            }
        }

        private bool RenderChart(IReadOnlyList<AlarmTrendPoint> items, int maximum)
        {
            LightningChart? chart = _chart;
            ViewXY? view = _view;
            if (chart == null || view == null)
            {
                return false;
            }

            AxisXCollection xAxes = CreateCategoryAxisCollection(view, items);
            AxisYCollection yAxes = CreateValueAxisCollection(view, maximum);

            PointLineSeries series = new PointLineSeries
            {
                AllowUserInteraction = false,
                AssignXAxisIndex = 0,
                AssignYAxisIndex = 0,
                CursorTrackEnabled = false,
                IncludeInAutoFit = true,
                LineStyle = new LineStyle
                {
                    Color = Color.FromRgb(37, 99, 235),
                    Pattern = LinePattern.Solid,
                    Width = 2.2
                },
                LineVisible = true,
                Points = items.Select((item, index) => new SeriesPoint(index, item.Count)).ToArray(),
                PointsOptimization = PointsRenderOptimization.None,
                PointsVisible = true,
                PointStyle = new PointShapeStyle
                {
                    BorderColor = Colors.White,
                    BorderWidth = 1,
                    Color1 = Color.FromRgb(37, 99, 235),
                    GradientFill = GradientFillPoint.Solid,
                    Height = 7,
                    Shape = Shape.Circle,
                    Width = 7
                },
                ShowInLegendBox = false
            };

            chart.BeginUpdate();
            try
            {
                view.XAxes = xAxes;
                view.YAxes = yAxes;
                view.PointLineSeries = new PointLineSeriesCollection { series };
            }
            finally
            {
                chart.EndUpdate();
            }

            return true;
        }

        private List<AlarmTrendPoint> GetTrendItems()
        {
            return ItemsSource?.OfType<AlarmTrendPoint>().ToList() ?? new List<AlarmTrendPoint>();
        }

        private static int GetMaximum(IReadOnlyCollection<AlarmTrendPoint> items)
        {
            return Math.Max(1, items.Count == 0 ? 1 : items.Max(item => item.Count));
        }

        private static string BuildDataSignature(IReadOnlyList<AlarmTrendPoint> items, int maximum)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(maximum.ToString(CultureInfo.InvariantCulture)).Append('|');
            foreach (AlarmTrendPoint item in items)
            {
                AppendSegment(builder, item.Label);
                builder.Append(item.Count.ToString(CultureInfo.InvariantCulture)).Append(';');
            }

            return builder.ToString();
        }

        private static void AppendSegment(StringBuilder builder, string? value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
        }

        private static ViewXY CreateView(LightningChart chart)
        {
            ViewXY view = new ViewXY(chart)
            {
                AutoSpaceLegendBoxes = false,
                GraphBackground = CreateSolidFill(Colors.White),
                LegendBoxes = new LegendBoxXYCollection(),
                Margins = new Thickness(8, 12, 12, 8),
                PointLineSeries = new PointLineSeriesCollection()
            };

            view.ZoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
            view.ZoomPanOptions.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction.None;
            view.ZoomPanOptions.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction.None;
            view.ZoomPanOptions.MultiTouchPanEnabled = false;
            view.ZoomPanOptions.MultiTouchZoomEnabled = false;
            view.ZoomPanOptions.WheelZooming = WheelZooming.Off;
            return view;
        }

        private static AxisXCollection CreateCategoryAxisCollection(ViewXY view, IReadOnlyList<AlarmTrendPoint> items)
        {
            AxisX axis = new AxisX(view)
            {
                AllowScaling = false,
                AllowScrolling = false,
                AllowSeriesDragDrop = false,
                AllowUserInteraction = false,
                AutoDivSpacing = false,
                AutoFormatLabels = false,
                AxisColor = Color.FromRgb(203, 213, 225),
                AxisThickness = 1,
                CustomTicks = new CustomAxisTickCollection(),
                CustomTicksEnabled = true,
                LabelsColor = Color.FromRgb(71, 85, 105),
                LabelsFont = new WpfFont("Microsoft YaHei UI", 10),
                MajorGrid = new GridOptions { Visible = false },
                MinorGrid = new GridOptions { Visible = false },
                PanningEnabled = false,
                ValueType = AxisValueType.Number,
                ZoomingEnabled = false
            };

            axis.SetRange(-0.6, Math.Max(0.6, items.Count - 0.4));
            foreach (int index in SelectCategoryAxisTickIndexes(items.Count))
            {
                axis.CustomTicks.Add(new CustomAxisTick(axis, index, items[index].Label));
            }

            return new AxisXCollection { axis };
        }

        private static AxisYCollection CreateValueAxisCollection(ViewXY view, int maximum)
        {
            double axisMaximum = Math.Max(5, Math.Ceiling(maximum * 1.18));
            AxisY axis = new AxisY(view)
            {
                AllowAutoYFit = false,
                AllowScaling = false,
                AllowScrolling = false,
                AllowSeriesDragDrop = false,
                AllowUserInteraction = false,
                AutoDivSpacing = true,
                AxisColor = Color.FromRgb(203, 213, 225),
                AxisThickness = 1,
                LabelsColor = Color.FromRgb(71, 85, 105),
                LabelsFont = new WpfFont("Microsoft YaHei UI", 10),
                LabelsNumberFormat = "0",
                MajorGrid = new GridOptions
                {
                    Color = Color.FromRgb(226, 232, 240),
                    Pattern = LinePattern.Solid,
                    Visible = true
                },
                MinorGrid = new GridOptions { Visible = false },
                PanningEnabled = false,
                ValueType = AxisValueType.Number,
                ZoomingEnabled = false
            };

            axis.SetRange(0, axisMaximum);
            return new AxisYCollection { axis };
        }

        private static IEnumerable<int> SelectCategoryAxisTickIndexes(int count)
        {
            if (count <= 0)
            {
                yield break;
            }

            int step = count <= 12 ? 1 : (int)Math.Ceiling(count / 12d);
            for (int index = 0; index < count; index += step)
            {
                yield return index;
            }

            int lastIndex = count - 1;
            if ((lastIndex % step) != 0)
            {
                yield return lastIndex;
            }
        }

        private static Fill CreateSolidFill(Color color)
        {
            return new Fill
            {
                Color = color,
                Style = RectFillStyle.ColorOnly
            };
        }

        private void AttachCollectionChanged()
        {
            if (_collectionChanged != null)
            {
                return;
            }

            _collectionChanged = ItemsSource as INotifyCollectionChanged;
            if (_collectionChanged != null)
            {
                _collectionChanged.CollectionChanged += OnCollectionChanged;
            }
        }

        private void DetachCollectionChanged()
        {
            if (_collectionChanged != null)
            {
                _collectionChanged.CollectionChanged -= OnCollectionChanged;
                _collectionChanged = null;
            }
        }

        private void HideFallback()
        {
            if (_chart != null)
            {
                _chart.Visibility = Visibility.Visible;
            }

            _fallbackText.Visibility = Visibility.Collapsed;
        }

        private void ShowFallback(string message)
        {
            if (_chart != null)
            {
                _chart.Visibility = Visibility.Collapsed;
            }

            _fallbackText.Text = message;
            _fallbackText.Visibility = Visibility.Visible;
        }
    }
}

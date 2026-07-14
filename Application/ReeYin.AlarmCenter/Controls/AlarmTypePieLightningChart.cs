using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Views.ViewPie3D;
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
using Pie3DView = Arction.Wpf.ChartingMVVM.Views.ViewPie3D.ViewPie3D;

namespace ReeYin.AlarmCenter.Controls
{
    public sealed class AlarmTypePieLightningChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(AlarmTypePieLightningChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

        private readonly Grid _root = new Grid();
        private readonly TextBlock _fallbackText;
        private LightningChart? _chart;
        private Pie3DView? _view;
        private INotifyCollectionChanged? _collectionChanged;
        private bool _renderScheduled;
        private string? _lastRenderedSignature;
        private bool _isRenderDirty = true;

        public AlarmTypePieLightningChart()
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
            if (d is not AlarmTypePieLightningChart chart)
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
                ActiveView = ChartActiveView.ViewPie3D,
                Focusable = false,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ChartBackground = CreateSolidFill(Colors.White)
            };
            Pie3DView view = CreateView(chart);
            chart.ViewPie3D = view;
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
                List<AlarmPieSliceItem> items = GetPieItems();
                string currentSignature = BuildDataSignature(items);
                if (string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal))
                {
                    if (_isRenderDirty)
                    {
                        _isRenderDirty = false;
                    }

                    HideFallback();
                    return;
                }

                if (!RenderChart(items))
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

        private bool RenderChart(IReadOnlyList<AlarmPieSliceItem> items)
        {
            LightningChart? chart = _chart;
            Pie3DView? view = _view;
            if (chart == null || view == null)
            {
                return false;
            }

            PieSliceCollection values = new PieSliceCollection();

            foreach (AlarmPieSliceItem item in items)
            {
                Color color = ResolveSliceColor(item);
                PieSlice slice = new PieSlice
                {
                    AllowUserInteraction = false,
                    BlinkOnOver = false,
                    Color = color,
                    ShowInLegendBox = false,
                    TitleAlignment = AlignmentPie3DTitle.Outside,
                    Value = item.Count
                };
                values.Add(slice);
            }

            chart.BeginUpdate();
            try
            {
                view.Values = values;
            }
            finally
            {
                chart.EndUpdate();
            }

            return true;
        }

        private List<AlarmPieSliceItem> GetPieItems()
        {
            return ItemsSource?.OfType<AlarmPieSliceItem>()
                .Where(item => item.Count > 0)
                .ToList() ?? new List<AlarmPieSliceItem>();
        }

        private static string BuildDataSignature(IReadOnlyList<AlarmPieSliceItem> items)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('|');
            foreach (AlarmPieSliceItem item in items)
            {
                Color color = ResolveSliceColor(item);
                AppendSegment(builder, item.Label);
                builder.Append(item.Count.ToString(CultureInfo.InvariantCulture)).Append(';');
                builder.Append(item.Percentage.ToString("R", CultureInfo.InvariantCulture)).Append(';');
                builder.Append(color.A.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(color.R.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(color.G.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(color.B.ToString(CultureInfo.InvariantCulture)).Append(';');
            }

            return builder.ToString();
        }

        private static Color ResolveSliceColor(AlarmPieSliceItem item)
        {
            return item.FillBrush is SolidColorBrush brush ? brush.Color : Colors.SteelBlue;
        }

        private static void AppendSegment(StringBuilder builder, string? value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
        }

        private static Pie3DView CreateView(LightningChart chart)
        {
            Pie3DView view = new Pie3DView(chart)
            {
                DonutInnerPercents = 44,
                ExplodePercents = 2,
                LegendBox3DPie = new LegendBoxPie3D
                {
                    AllowDragging = false,
                    AllowResize = false,
                    AllowUserInteraction = false,
                    ShowCheckboxes = false,
                    Visible = false
                },
                Margins = new Thickness(0),
                Rounding = 4,
                StartAngle = 270,
                Style = PieStyle3D.Donut,
                Thickness = 28,
                TitlesNumberFormat = "0.#%",
                TitlesStyle = PieTitleStyle.Percents,
                Values = new PieSliceCollection()
            };

            view.ZoomPanOptions.AllowWheelZoom = false;
            view.ZoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction3DPie.None;
            view.ZoomPanOptions.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction3DPie.None;
            view.ZoomPanOptions.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction3DPie.None;
            view.ZoomPanOptions.MultiTouchPanEnabled = false;
            view.ZoomPanOptions.MultiTouchZoomEnabled = false;
            return view;
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

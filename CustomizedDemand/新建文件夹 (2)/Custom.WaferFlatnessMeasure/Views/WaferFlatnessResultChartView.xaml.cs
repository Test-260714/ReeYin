using Arction.Wpf.Charting;
using Arction.Wpf.Charting.SeriesXY;
using Custom.WaferFlatnessMeasure.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Custom.WaferFlatnessMeasure.Views
{
    public partial class WaferFlatnessResultChartView : UserControl
    {
        private LightningChart? _chart;
        private PointLineSeries? _series;
        private WaferFlatnessResultChartViewModel? _viewModel;

        public WaferFlatnessResultChartView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureChart();
            AttachViewModel(DataContext as WaferFlatnessResultChartViewModel);

            if (_viewModel?.LoadCommand.CanExecute() == true)
            {
                _viewModel.LoadCommand.Execute();
            }

            UpdateChart();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(null);

            if (_chart != null)
            {
                ChartHost.Children.Clear();
                _chart.Dispose();
                _chart = null;
                _series = null;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as WaferFlatnessResultChartViewModel);
            UpdateChart();
        }

        private void AttachViewModel(WaferFlatnessResultChartViewModel? viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
            {
                return;
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(WaferFlatnessResultChartViewModel.ChartPoints) ||
                e.PropertyName == nameof(WaferFlatnessResultChartViewModel.ChartTitle) ||
                e.PropertyName == nameof(WaferFlatnessResultChartViewModel.SelectedChartDataSource))
            {
                if (Dispatcher.CheckAccess())
                {
                    UpdateChart();
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(UpdateChart));
                }
            }
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
                    ChartName = "WaferFlatnessResultChart",
                    Background = Brushes.Transparent
                };
                _chart.ChartBackground.Color = Colors.White;

                _chart.Title.Text = "结果曲线";
                _chart.Title.Color = Color.FromRgb(31, 41, 55);

                _chart.ViewXY.XAxes[0].Title.Text = "序号";
                _chart.ViewXY.XAxes[0].LabelsColor = Color.FromRgb(226, 232, 240);
                _chart.ViewXY.XAxes[0].AxisColor = Color.FromRgb(148, 163, 184);
                _chart.ViewXY.XAxes[0].SetRange(0, 10);

                _chart.ViewXY.YAxes[0].Title.Text = "数据值";
                _chart.ViewXY.YAxes[0].LabelsColor = Color.FromRgb(226, 232, 240);
                _chart.ViewXY.YAxes[0].AxisColor = Color.FromRgb(148, 163, 184);
                _chart.ViewXY.YAxes[0].SetRange(0, 1);

                _series = new PointLineSeries(
                    _chart.ViewXY,
                    _chart.ViewXY.XAxes[0],
                    _chart.ViewXY.YAxes[0])
                {
                    PointsVisible = false,
                    LineVisible = true,
                    CursorTrackEnabled = true,
                    ShowInLegendBox = true
                };
                _series.LineStyle.Color = Color.FromRgb(14, 165, 233);
                _series.LineStyle.Width = 2;
                _series.Title.Text = "结果";

                _chart.ViewXY.PointLineSeries.Add(_series);
                ChartHost.Children.Add(_chart);
            }
            catch (Exception ex)
            {
                EmptyChartHint.Text = $"LightningChart 初始化失败：{ex.Message}";
                EmptyChartHint.Visibility = Visibility.Visible;
            }
        }

        private void UpdateChart()
        {
            if (_chart == null || _series == null || _viewModel == null)
            {
                EmptyChartHint.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var points = _viewModel.ChartPoints ?? Array.Empty<WaferFlatnessResultChartPoint>();
                _chart.BeginUpdate();

                _chart.Title.Text = string.IsNullOrWhiteSpace(_viewModel.ChartTitle)
                    ? "结果曲线"
                    : _viewModel.ChartTitle;
                _series.Title.Text = _viewModel.SelectedChartDataSource?.DisplayName ?? "结果";
                _chart.ViewXY.YAxes[0].Title.Text = _viewModel.SelectedChartDataSource?.DisplayName ?? "数据值";

                if (points.Count == 0)
                {
                    _series.Clear();
                    _chart.ViewXY.XAxes[0].SetRange(0, 10);
                    _chart.ViewXY.YAxes[0].SetRange(0, 1);
                    EmptyChartHint.Visibility = Visibility.Visible;
                    return;
                }

                double[] xValues = points.Select(point => point.X).ToArray();
                double[] yValues = points.Select(point => point.Y).ToArray();
                _series.SetValues(xValues, yValues);

                double xMin = xValues.Min();
                double xMax = xValues.Max();
                double yMin = yValues.Min();
                double yMax = yValues.Max();

                if (Math.Abs(xMax - xMin) < 1E-9)
                {
                    xMin -= 1;
                    xMax += 1;
                }

                if (Math.Abs(yMax - yMin) < 1E-9)
                {
                    yMin -= 1;
                    yMax += 1;
                }

                double xPadding = Math.Max(1, (xMax - xMin) * 0.03d);
                double yPadding = Math.Max(Math.Abs(yMax - yMin) * 0.08d, 1E-6d);
                _chart.ViewXY.XAxes[0].SetRange(Math.Max(0, xMin - xPadding), xMax + xPadding);
                _chart.ViewXY.YAxes[0].SetRange(yMin - yPadding, yMax + yPadding);

                EmptyChartHint.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                EmptyChartHint.Text = $"曲线刷新失败：{ex.Message}";
                EmptyChartHint.Visibility = Visibility.Visible;
            }
            finally
            {
                _chart?.EndUpdate();
            }
        }
    }
}

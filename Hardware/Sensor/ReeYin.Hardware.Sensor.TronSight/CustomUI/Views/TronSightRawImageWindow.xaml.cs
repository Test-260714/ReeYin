using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.SeriesXY;
using Microsoft.Win32;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Models;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.ViewModels;
using ReeYin_V.Core.Enums;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.Views
{
    /// <summary>
    /// 原始图像独立窗口。
    /// </summary>
    public partial class TronSightRawImageWindow : UserControl
    {
        private const int DefaultFrameLength = 1024;
        private const int MaxPeakPositionCount = 10;
        private const int FallbackPeakCount = 2;

        private TronSightSensor _sensor = new TronSightSensor();
        private TronSightRawImageConfigModel _config = new TronSightRawImageConfigModel();
        private readonly DispatcherTimer _autoRefreshTimer;
        private readonly LightningChart _rawImageChart = new LightningChart();
        private double[] _frameData = Array.Empty<double>();
        private int[] _peakPositions = Array.Empty<int>();
        private TronSightRawImageWindowViewModel? _viewModel;
        private bool _isLoading;
        private bool _isUpdatingRefreshState;
        private bool _isInitialized;
        private bool _isInitializeRetryQueued;
        private bool _isDisposed;

        public TronSightRawImageWindow()
        {
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _autoRefreshTimer.Tick += (s, e) => RefreshImage(false);

            InitializeComponent();
            DataContextChanged += TronSightRawImageWindow_DataContextChanged;
            RawImageChartHost.Content = _rawImageChart;
            InitializeChart();
        }

        private void TronSightRawImageWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel();
        }

        private void AttachViewModel()
        {
            if (ReferenceEquals(_viewModel, DataContext))
            {
                return;
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = DataContext as TronSightRawImageWindowViewModel;
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            SyncViewModelParameters();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TronSightRawImageWindowViewModel.Sensor)
                || e.PropertyName == nameof(TronSightRawImageWindowViewModel.RawImageConfig))
            {
                SyncViewModelParameters();
            }
        }

        private void SyncViewModelParameters()
        {
            if (_viewModel == null)
            {
                return;
            }

            _sensor = _viewModel.Sensor ?? new TronSightSensor();
            _config = _viewModel.RawImageConfig;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                return;
            }

            AttachViewModel();
            if (_viewModel == null && !_isInitializeRetryQueued)
            {
                _isInitializeRetryQueued = true;
                Dispatcher.BeginInvoke(new Action(() => Window_Loaded(sender, e)), DispatcherPriority.Loaded);
                return;
            }

            _isInitialized = true;
            InitializeOptions();
            LoadCurrentConfig();
            if (_config.IsAutoRefreshEnabled)
            {
                StartRealtimeRefresh(false);
            }
            else
            {
                RefreshImage(false);
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            StopRealtimeRefresh(false);
            _rawImageChart.Dispose();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void InitializeChart()
        {
            _rawImageChart.BeginUpdate();
            try
            {
                _rawImageChart.ActiveView = ActiveView.ViewXY;
                _rawImageChart.ChartName = "TronSight Raw Image";
                _rawImageChart.Title.Text = string.Empty;
                _rawImageChart.ViewXY.LegendBoxes[0].Visible = false;
                _rawImageChart.ViewXY.XAxes[0].SetRange(0, DefaultFrameLength - 1);
                _rawImageChart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
                _rawImageChart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
                _rawImageChart.ViewXY.XAxes[0].Title.Text = "像素";
                _rawImageChart.ViewXY.YAxes[0].SetRange(0, 5);
                _rawImageChart.ViewXY.YAxes[0].Title.Text = "峰高";
            }
            finally
            {
                _rawImageChart.EndUpdate();
            }
        }

        private void InitializeOptions()
        {
            _isLoading = true;
            try
            {
                _viewModel?.RefreshSensorChannelOptions();
                ApplyModelToControls();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadCurrentConfig()
        {
            if (!IsSensorReady(false))
            {
                SetStatus("传感器未连接，窗口已以离线状态打开", true);
                return;
            }

            _isLoading = true;
            try
            {
                int sensorIndex = SelectedSensorIndex;

                FRAME_DATA_SRC frameDataSource = FRAME_DATA_SRC.ORIGIN;
                if (TSCMCAPICS.GetConfigFrameDataSource(_sensor.ControlerHandle, 0, ref frameDataSource) == ERRCODE.OK)
                {
                    _config.FrameDataSource = frameDataSource;
                }

                STATE lightState = STATE.OFF;
                if (TSCMCAPICS.GetConfigLightSource(_sensor.ControlerHandle, 0, sensorIndex, ref lightState) == ERRCODE.OK)
                {
                    _config.IsLightSourceEnabled = lightState == STATE.ON;
                }

                double lightIntensity = 0;
                if (TSCMCAPICS.GetConfigLightIntensity(_sensor.ControlerHandle, 0, sensorIndex, ref lightIntensity) == ERRCODE.OK)
                {
                    _config.LightIntensity = lightIntensity;
                }

                ExposureConfig exposureConfig = new ExposureConfig();
                if (TSCMCAPICS.GetConfigExposure(_sensor.ControlerHandle, 0, sensorIndex, ref exposureConfig) == ERRCODE.OK)
                {
                    _config.IsAutoExposureEnabled = exposureConfig.auto_control == STATE.ON;
                    _config.ManualExposure = exposureConfig.exposure_time;
                }

                ushort exposureTarget = 0;
                if (TSCMCAPICS.GetConfigAutoExposureTarget(_sensor.ControlerHandle, 0, sensorIndex, ref exposureTarget) == ERRCODE.OK)
                {
                    _config.TargetExposure = exposureTarget;
                }

                AutoExposureTimeSetting autoExposure = new AutoExposureTimeSetting();
                if (TSCMCAPICS.GetConfigAutoExposureTimeSetting(_sensor.ControlerHandle, 0, sensorIndex, ref autoExposure) == ERRCODE.OK)
                {
                    _config.MinExposure = autoExposure.min_exposure_time;
                    _config.MaxExposure = autoExposure.max_exposure_time;
                }

                PeakDetection peakDetection = new PeakDetection();
                if (TSCMCAPICS.GetConfigPeakDetection(_sensor.ControlerHandle, 0, sensorIndex, ref peakDetection) == ERRCODE.OK)
                {
                    _config.PeakHeightThreshold = peakDetection.threshold;
                    _config.PeakSharpnessThreshold = peakDetection.sharpness;
                    _config.PeakMinSpacing = peakDetection.minimum_spacing;
                }

                PeakSelection peakSelection = new PeakSelection();
                if (TSCMCAPICS.GetConfigPeakSelection(_sensor.ControlerHandle, 0, sensorIndex, ref peakSelection) == ERRCODE.OK)
                {
                    _config.PeakSelectionMode = peakSelection.mode;
                    _config.Peak1Index = Math.Max(1, peakSelection.peak1_idx);
                    _config.Peak1Start = Clamp(peakSelection.peak1_window_start, 0, DefaultFrameLength - 1);
                    _config.Peak1End = Clamp(peakSelection.peak1_window_end, 0, DefaultFrameLength - 1);
                    _config.Peak2Index = Math.Max(1, peakSelection.peak2_idx);
                    _config.Peak2Start = Clamp(peakSelection.peak2_window_start, 0, DefaultFrameLength - 1);
                    _config.Peak2End = Clamp(peakSelection.peak2_window_end, 0, DefaultFrameLength - 1);
                }

                PEAK_SORT_MODE peakSortMode = PEAK_SORT_MODE.LEFT_AND_RIGHT;
                if (TSCMCAPICS.GetConfigPeakSortMode(_sensor.ControlerHandle, 0, sensorIndex, ref peakSortMode) == ERRCODE.OK)
                {
                    _config.PeakSortMode = peakSortMode;
                }

                STATE autoFilter = STATE.ON;
                IMAGE_FILTER_WIDTH filterWidth = IMAGE_FILTER_WIDTH._1;
                if (TSCMCAPICS.GetSensorImageFilterWidth(_sensor.ControlerHandle, 0, sensorIndex, ref autoFilter, ref filterWidth) == ERRCODE.OK)
                {
                    _config.IsAutoImageFilterEnabled = autoFilter == STATE.ON;
                    _config.ImageFilterWidth = filterWidth;
                }

                UpdateExposureControlState();
                UpdateModelFromControls();
                SetStatus("参数读取完成", false);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void ApplyModelToControls()
        {
            int sensorCount = Math.Max(1, _viewModel?.SensorChannelOptions.Count ?? 1);
            _config.SensorChannelIndex = Clamp(_config.SensorChannelIndex, 0, sensorCount - 1);
            _config.LightIntensity = Clamp(_config.LightIntensity, 0, 100);
            _config.ManualExposure = Clamp(_config.ManualExposure, 1, 5000);
            _config.TargetExposure = Clamp(_config.TargetExposure, 0, ushort.MaxValue);
            _config.MinExposure = Clamp(_config.MinExposure, 1, 5000);
            _config.MaxExposure = Clamp(_config.MaxExposure, _config.MinExposure, 5000);
            _config.PeakHeightThreshold = Clamp(_config.PeakHeightThreshold, 0, ushort.MaxValue);
            _config.PeakSharpnessThreshold = Clamp(_config.PeakSharpnessThreshold, 0, ushort.MaxValue);
            _config.PeakMinSpacing = Clamp(_config.PeakMinSpacing, 0, DefaultFrameLength - 1);
            _config.Peak1Index = Clamp(_config.Peak1Index, 1, MaxPeakPositionCount);
            _config.Peak1Start = Clamp(_config.Peak1Start, 0, DefaultFrameLength - 1);
            _config.Peak1End = Clamp(_config.Peak1End, _config.Peak1Start, DefaultFrameLength - 1);
            _config.Peak2Index = Clamp(_config.Peak2Index, 1, MaxPeakPositionCount);
            _config.Peak2Start = Clamp(_config.Peak2Start, 0, DefaultFrameLength - 1);
            _config.Peak2End = Clamp(_config.Peak2End, _config.Peak2Start, DefaultFrameLength - 1);
        }

        private void UpdateModelFromControls()
        {
            ApplyModelToControls();
        }

        private void RefreshImage(bool showSuccess)
        {
            UpdateModelFromControls();
            if (!IsSensorReady(showSuccess))
            {
                _frameData = Array.Empty<double>();
                _config.MaxPeakValue = 0;
                _config.MaxPeakPosition = 0;
                DrawChart();
                return;
            }

            int pixelSize = 0;
            double[] dataBuffer = new double[DefaultFrameLength];
            ERRCODE rtn = TSCMCAPICS.GetDataFrameSingle(
                _sensor.ControlerHandle,
                0,
                5,
                ref dataBuffer[0],
                ref pixelSize,
                dataBuffer.Length);

            if (rtn != ERRCODE.OK || pixelSize <= 0)
            {
                SetStatus($"读取原始图像失败：{rtn}", true);
                return;
            }

            int validLength = Math.Min(pixelSize, dataBuffer.Length);
            _frameData = dataBuffer.Take(validLength).ToArray();
            RefreshPeakInfo();
            DrawChart();

            if (showSuccess)
            {
                SetStatus($"已刷新 {validLength} 个像素", false);
            }
        }

        private void RefreshPeakInfo()
        {
            if (_frameData.Length == 0)
            {
                _config.MaxPeakValue = 0;
                _config.MaxPeakPosition = 0;
                _peakPositions = Array.Empty<int>();
                return;
            }

            int warning = 0;
            if (TSCMCAPICS.GetExposureSatureWarning(_sensor.ControlerHandle, 0, SelectedSensorIndex, ref warning) == ERRCODE.OK)
            {
                ExposureStateEllipse.Fill = warning == 0 ? Brushes.ForestGreen : Brushes.OrangeRed;
            }

            int[] peakBuffer = new int[MaxPeakPositionCount];
            int peakCount = 0;
            ERRCODE peakRtn = TSCMCAPICS.GetExposurePeakPosition(
                _sensor.ControlerHandle,
                0,
                SelectedSensorIndex,
                ref peakBuffer[0],
                ref peakCount,
                peakBuffer.Length);

            _peakPositions = peakRtn == ERRCODE.OK && peakCount > 0
                ? NormalizePeakPositions(peakBuffer.Take(Math.Min(peakCount, peakBuffer.Length)))
                : FindLocalPeakPositions();

            UpdateMaxPeakDisplay();
        }

        private void UpdateMaxPeakDisplay()
        {
            if (_frameData.Length == 0)
            {
                _config.MaxPeakValue = 0;
                _config.MaxPeakPosition = 0;
                return;
            }

            double maxValue = _frameData.Max();
            int maxPosition = Array.IndexOf(_frameData, maxValue);
            _config.MaxPeakValue = maxValue;
            _config.MaxPeakPosition = maxPosition;
        }

        private int[] FindLocalPeakPositions()
        {
            if (_frameData.Length == 0)
            {
                return Array.Empty<int>();
            }

            int threshold = _config.PeakHeightThreshold;
            int minSpacing = Math.Max(1, _config.PeakMinSpacing);
            List<int> candidates = new List<int>();
            for (int index = 1; index < _frameData.Length - 1; index++)
            {
                double value = _frameData[index];
                if (value < threshold)
                {
                    continue;
                }

                if (value >= _frameData[index - 1] && value >= _frameData[index + 1])
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                double maxValue = _frameData.Max();
                return new[] { Array.IndexOf(_frameData, maxValue) };
            }

            List<int> selected = new List<int>();
            foreach (int candidate in candidates.OrderByDescending(index => _frameData[index]))
            {
                if (selected.All(index => Math.Abs(index - candidate) >= minSpacing))
                {
                    selected.Add(candidate);
                }

                if (selected.Count >= FallbackPeakCount)
                {
                    break;
                }
            }

            selected.Sort();
            return selected.ToArray();
        }

        private int[] NormalizePeakPositions(IEnumerable<int> positions)
        {
            int[] normalized = positions
                .Where(position => position >= 0 && position < _frameData.Length)
                .Distinct()
                .ToArray();

            return normalized.Length > 0 ? normalized : FindLocalPeakPositions();
        }

        private void DrawChart()
        {
            NoDataTextBlock.Visibility = _frameData.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            _rawImageChart.BeginUpdate();
            try
            {
                _rawImageChart.ViewXY.FreeformPointLineSeries.Clear();
                _rawImageChart.ViewXY.Annotations.Clear();

                double xMax = GetXAxisMax();
                double yMax = GetYAxisMax();
                _rawImageChart.ViewXY.XAxes[0].SetRange(0, xMax);
                _rawImageChart.ViewXY.YAxes[0].SetRange(0, yMax);

                if (_frameData.Length == 0)
                {
                    return;
                }

                _rawImageChart.ViewXY.FreeformPointLineSeries.Add(CreateLineSeries(
                    "图像曲线",
                    BuildFramePoints(),
                    Colors.SeaGreen,
                    1.6));

                if (_config.IsThresholdVisible)
                {
                    AddThresholdSeries(xMax, yMax);
                }

                if (_config.IsMeasureRangeVisible)
                {
                    AddMeasureRangeSeries(xMax, yMax);
                }

                AddPeakMarkerSeries(xMax, yMax);
            }
            finally
            {
                _rawImageChart.EndUpdate();
            }
        }

        private double GetXAxisMax()
        {
            if (_config.IsXAxisAuto && _frameData.Length > 1)
            {
                return Math.Max(1, _frameData.Length - 1);
            }

            return DefaultFrameLength - 1;
        }

        private double GetYAxisMax()
        {
            if (!_config.IsYAxisAuto)
            {
                return 5;
            }

            double maxValue = _frameData.Length == 0 ? 5 : _frameData.Max();
            maxValue = Math.Max(maxValue, _config.PeakHeightThreshold);
            if (maxValue <= 0)
            {
                return 5;
            }

            return maxValue * 1.15;
        }

        private SeriesPoint[] BuildFramePoints()
        {
            SeriesPoint[] points = new SeriesPoint[_frameData.Length];
            for (int i = 0; i < _frameData.Length; i++)
            {
                points[i].X = i;
                points[i].Y = _frameData[i];
            }

            return points;
        }

        private void AddThresholdSeries(double xMax, double yMax)
        {
            double threshold = _config.PeakHeightThreshold;
            if (threshold <= 0 || threshold > yMax)
            {
                return;
            }

            _rawImageChart.ViewXY.FreeformPointLineSeries.Add(CreateLineSeries(
                "峰高阈值",
                new[]
                {
                    new SeriesPoint(0, threshold),
                    new SeriesPoint(xMax, threshold)
                },
                Colors.OrangeRed,
                1));
        }

        private void AddMeasureRangeSeries(double xMax, double yMax)
        {
            if (!IsSensorReady(false))
            {
                return;
            }

            MeasureRangeNode range = new MeasureRangeNode();
            ERRCODE rtn = TSCMCAPICS.GetMeasureRangeThreshold(_sensor.ControlerHandle, 0, SelectedSensorIndex, ref range);
            if (rtn != ERRCODE.OK || range.end <= range.start)
            {
                return;
            }

            AddVerticalMarker("量程起点", range.start, xMax, yMax, Colors.SteelBlue, 1);
            AddVerticalMarker("量程终点", range.end, xMax, yMax, Colors.SteelBlue, 1);
        }

        private void AddPeakMarkerSeries(double xMax, double yMax)
        {
            for (int index = 0; index < _peakPositions.Length; index++)
            {
                int peakPosition = _peakPositions[index];
                AddVerticalMarker("峰位置", peakPosition, xMax, yMax, Colors.Orange, 1.2);
                AddPeakIndexAnnotation(index + 1, peakPosition, xMax, yMax);
            }
        }

        private void AddPeakIndexAnnotation(int peakIndex, int peakPosition, double xMax, double yMax)
        {
            if (peakPosition < 0 || peakPosition > xMax || peakPosition >= _frameData.Length)
            {
                return;
            }

            double peakValue = _frameData[peakPosition];
            double labelY = Math.Min(yMax * 0.96, peakValue + yMax * 0.055);
            AnnotationXY annotation = new AnnotationXY(
                _rawImageChart.ViewXY,
                _rawImageChart.ViewXY.XAxes[0],
                _rawImageChart.ViewXY.YAxes[0])
            {
                Text = peakIndex.ToString(CultureInfo.InvariantCulture),
                TargetCoordinateSystem = AnnotationTargetCoordinates.AxisValues,
                TargetAxisValues = new PointDoubleXY(peakPosition, peakValue),
                LocationAxisValues = new PointDoubleXY(peakPosition, labelY),
                Style = AnnotationStyle.Rectangle,
                Sizing = AnnotationXYSizing.Automatic,
                KeepVisible = true,
                ClipInsideGraph = true,
                BorderVisible = false,
                AllowUserInteraction = false,
                AllowDragging = false,
                AllowResize = false,
                AllowTargetMove = false
            };
            annotation.Fill.Style = RectFillStyle.ColorOnly;
            annotation.Fill.Color = Colors.Transparent;
            annotation.TextStyle.Color = Colors.SteelBlue;
            annotation.TextStyle.HorizAlign = AlignmentHorizontal.Center;
            annotation.TextStyle.VerticalAlign = AlignmentVertical.Bottom;

            _rawImageChart.ViewXY.Annotations.Add(annotation);
        }

        private void AddVerticalMarker(string title, double value, double xMax, double yMax, Color color, double width)
        {
            if (value < 0 || value > xMax)
            {
                return;
            }

            _rawImageChart.ViewXY.FreeformPointLineSeries.Add(CreateLineSeries(
                title,
                new[]
                {
                    new SeriesPoint(value, 0),
                    new SeriesPoint(value, yMax)
                },
                color,
                width));
        }

        private FreeformPointLineSeries CreateLineSeries(string title, SeriesPoint[] points, Color color, double width)
        {
            FreeformPointLineSeries series = new FreeformPointLineSeries(
                _rawImageChart.ViewXY,
                _rawImageChart.ViewXY.XAxes[0],
                _rawImageChart.ViewXY.YAxes[0])
            {
                LineVisible = true,
                PointsVisible = false,
                PointsType = PointsType.Points,
                Points = points
            };
            series.Title.Text = title;
            series.LineStyle.Color = color;
            series.LineStyle.Width = width;
            return series;
        }

        private void SensorChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;

            if (SensorChannelComboBox.SelectedValue is int selectedIndex)
            {
                _config.SensorChannelIndex = selectedIndex;
            }

            LoadCurrentConfig();
        }

        private void FrameDataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            UpdateModelFromControls();
            ERRCODE rtn = TSCMCAPICS.SetConfigFrameDataSource(_sensor.ControlerHandle, 0, SelectedFrameDataSource);
            ApplyResult(rtn, "设置图像类型");
            RefreshImage(false);
        }

        private void LightSourceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            UpdateModelFromControls();
            STATE state = _config.IsLightSourceEnabled ? STATE.ON : STATE.OFF;
            ApplyResult(TSCMCAPICS.SetConfigLightSource(_sensor.ControlerHandle, 0, SelectedSensorIndex, state), "设置光源开关");
        }

        private void LightIntensityBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            double intensity = Clamp(_config.LightIntensity, 0, 100);
            _config.LightIntensity = intensity;
            UpdateModelFromControls();
            ApplyResult(TSCMCAPICS.SetConfigLightIntensity(_sensor.ControlerHandle, 0, SelectedSensorIndex, intensity), "设置光源强度");
        }

        private void AutoExposureCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized) return;

            UpdateExposureControlState();
            ApplyExposureConfig();
        }

        private void ExposureBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyExposureConfig();
        }

        private void TargetExposureBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            ushort target = (ushort)Clamp(_config.TargetExposure, 0, ushort.MaxValue);
            _config.TargetExposure = target;
            UpdateModelFromControls();
            ApplyResult(TSCMCAPICS.SetConfigAutoExposureTarget(_sensor.ControlerHandle, 0, SelectedSensorIndex, target), "设置目标曝光值");
        }

        private void AutoExposureRangeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            int min = Clamp(_config.MinExposure, 1, 5000);
            int max = Clamp(_config.MaxExposure, min, 5000);
            _config.MinExposure = min;
            _config.MaxExposure = max;
            UpdateModelFromControls();

            AutoExposureTimeSetting setting = new AutoExposureTimeSetting
            {
                min_exposure_time = (ushort)min,
                max_exposure_time = (ushort)max
            };
            ApplyResult(TSCMCAPICS.SetConfigAutoExposureTimeSetting(_sensor.ControlerHandle, 0, SelectedSensorIndex, setting), "设置自动曝光范围");
        }

        private void PeakDetectionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyPeakDetectionConfig();
            DrawChart();
        }

        private void PeakSelectionModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyPeakSelectionConfig();
        }

        private void PeakSelectionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyPeakSelectionConfig();
        }

        private void PeakSortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !_isInitialized || !IsSensorReady(false)) return;

            UpdateModelFromControls();
            ApplyResult(TSCMCAPICS.SetConfigPeakSortMode(_sensor.ControlerHandle, 0, SelectedSensorIndex, SelectedPeakSortMode), "设置峰排序");
        }

        private void ImageFilter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyImageFilterConfig();
        }

        private void ImageFilterWidthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyImageFilterConfig();
        }

        private void DownloadDarkReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSensorReady(true)) return;

            DarkReferenceTable darkReferenceTable = CreateDarkReferenceTable();
            ERRCODE rtn = TSCMCAPICS.DownloadDarkReference(_sensor.ControlerHandle, 0, SelectedSensorIndex, ref darkReferenceTable);
            ShowOperationMessage(rtn, "下载暗校准表");
        }

        private void DarkCalibrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSensorReady(true)) return;

            DarkReferenceTable darkReferenceTable = CreateDarkReferenceTable();
            ERRCODE rtn = TSCMCAPICS.DarkCalibration(_sensor.ControlerHandle, 0, SelectedSensorIndex, ref darkReferenceTable);
            ShowOperationMessage(rtn, "暗校准");
        }

        private void RefreshImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_autoRefreshTimer.IsEnabled)
            {
                StopRealtimeRefresh(true);
                return;
            }

            StartRealtimeRefresh(true);
        }

        private void AutoRefreshCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || !_isInitialized)
            {
                return;
            }

            if (_isUpdatingRefreshState)
            {
                return;
            }

            if (_config.IsAutoRefreshEnabled)
            {
                StartRealtimeRefresh(false);
            }
            else
            {
                StopRealtimeRefresh(false);
            }
        }

        private void StartRealtimeRefresh(bool showMessage)
        {
            if (!IsSensorReady(showMessage))
            {
                StopRealtimeRefresh(false);
                return;
            }

            _autoRefreshTimer.Start();
            SetAutoRefreshChecked(true);
            SetStatus("图像实时刷新中", false);
            RefreshImage(false);
        }

        private void StopRealtimeRefresh(bool showMessage)
        {
            _autoRefreshTimer.Stop();
            SetAutoRefreshChecked(false);
            if (showMessage)
            {
                SetStatus("已停止实时刷新", false);
            }
        }

        private void SetAutoRefreshChecked(bool isChecked)
        {
            _isUpdatingRefreshState = true;
            try
            {
                _config.IsAutoRefreshEnabled = isChecked;
            }
            finally
            {
                _isUpdatingRefreshState = false;
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (RawImageChartHost.ActualWidth <= 1 || RawImageChartHost.ActualHeight <= 1)
            {
                MessageView.Ins.MessageBoxShow("当前没有可保存的图像", eMsgType.Warn);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PNG图像|*.png|CSV数据|*.csv",
                DefaultExt = ".png",
                FileName = $"TronSightRawImage_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (System.IO.Path.GetExtension(dialog.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    SaveFrameData(dialog.FileName);
                }
                else
                {
                    SaveChartImage(dialog.FileName);
                }

                MessageView.Ins.MessageBoxShow("图像已保存", eMsgType.Info);
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"保存失败：{ex.Message}", eMsgType.Error);
            }
        }

        private void ChartOption_Click(object sender, RoutedEventArgs e)
        {
            UpdateModelFromControls();
            DrawChart();
        }

        private void ApplyExposureConfig()
        {
            if (_isLoading || !_isInitialized) return;

            UpdateModelFromControls();
            if (!IsSensorReady(false)) return;

            ExposureConfig exposureConfig = new ExposureConfig
            {
                auto_control = _config.IsAutoExposureEnabled ? STATE.ON : STATE.OFF,
                exposure_time = (ushort)Clamp(_config.ManualExposure, 1, 5000)
            };
            ApplyResult(TSCMCAPICS.SetConfigExposure(_sensor.ControlerHandle, 0, SelectedSensorIndex, exposureConfig), "设置曝光参数");
        }

        private void ApplyPeakDetectionConfig()
        {
            if (_isLoading || !_isInitialized) return;

            UpdateModelFromControls();
            if (!IsSensorReady(false)) return;

            PeakDetection peakDetection = new PeakDetection
            {
                threshold = Clamp(_config.PeakHeightThreshold, 0, ushort.MaxValue),
                sharpness = Clamp(_config.PeakSharpnessThreshold, 0, ushort.MaxValue),
                minimum_spacing = Clamp(_config.PeakMinSpacing, 0, DefaultFrameLength - 1)
            };
            ApplyResult(TSCMCAPICS.SetConfigPeakDetection(_sensor.ControlerHandle, 0, SelectedSensorIndex, peakDetection), "设置峰值检测参数");
        }

        private void ApplyPeakSelectionConfig()
        {
            if (_isLoading || !_isInitialized) return;

            UpdateModelFromControls();
            if (!IsSensorReady(false)) return;

            PeakSelection peakSelection = new PeakSelection
            {
                mode = SelectedPeakSelectionMode,
                peak1_idx = Clamp(_config.Peak1Index, 1, MaxPeakPositionCount),
                peak1_window_start = Clamp(_config.Peak1Start, 0, DefaultFrameLength - 1),
                peak1_window_end = Clamp(_config.Peak1End, 0, DefaultFrameLength - 1),
                peak2_idx = Clamp(_config.Peak2Index, 1, MaxPeakPositionCount),
                peak2_window_start = Clamp(_config.Peak2Start, 0, DefaultFrameLength - 1),
                peak2_window_end = Clamp(_config.Peak2End, 0, DefaultFrameLength - 1)
            };

            if (peakSelection.peak1_window_end < peakSelection.peak1_window_start)
            {
                peakSelection.peak1_window_end = peakSelection.peak1_window_start;
                _config.Peak1End = peakSelection.peak1_window_end;
            }

            if (peakSelection.peak2_window_end < peakSelection.peak2_window_start)
            {
                peakSelection.peak2_window_end = peakSelection.peak2_window_start;
                _config.Peak2End = peakSelection.peak2_window_end;
            }

            UpdateModelFromControls();
            ApplyResult(TSCMCAPICS.SetConfigPeakSelection(_sensor.ControlerHandle, 0, SelectedSensorIndex, peakSelection), "设置峰值选择参数");
        }

        private void ApplyImageFilterConfig()
        {
            if (_isLoading || !_isInitialized) return;

            UpdateModelFromControls();
            if (!IsSensorReady(false)) return;

            STATE autoSelect = _config.IsAutoImageFilterEnabled ? STATE.ON : STATE.OFF;
            ApplyResult(TSCMCAPICS.SetSensorImageFilterWidth(_sensor.ControlerHandle, 0, SelectedSensorIndex, autoSelect, SelectedImageFilterWidth), "设置图像滤波");
        }

        private void UpdateExposureControlState()
        {
            // 控件启用状态由 ViewModel 的 IsManualExposureEnabled / IsAutoExposureSettingEnabled 绑定驱动。
        }

        private void SaveChartImage(string fileName)
        {
            _rawImageChart.SaveToFile(fileName);
        }

        private void SaveFrameData(string fileName)
        {
            IEnumerable<string> lines = _frameData.Select((value, index) => $"{index},{value.ToString(CultureInfo.InvariantCulture)}");
            File.WriteAllLines(fileName, new[] { "Pixel,Value" }.Concat(lines));
        }

        private bool IsSensorReady(bool showMessage)
        {
            bool ready = _sensor != null && _sensor.ControlerHandle != 0 && _sensor.IsConnected;
            if (!ready && showMessage)
            {
                MessageView.Ins.MessageBoxShow("传感器未连接，无法执行原始图像操作", eMsgType.Warn);
            }

            return ready;
        }

        private bool ApplyResult(ERRCODE rtn, string action)
        {
            if (rtn == ERRCODE.OK)
            {
                SetStatus($"{action}成功", false);
                return true;
            }

            SetStatus($"{action}失败：{rtn}", true);
            return false;
        }

        private void ShowOperationMessage(ERRCODE rtn, string action)
        {
            if (ApplyResult(rtn, action))
            {
                MessageView.Ins.MessageBoxShow($"{action}成功", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow($"{action}失败：{rtn}", eMsgType.Error);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? Brushes.OrangeRed : Brushes.ForestGreen;
        }

        private static DarkReferenceTable CreateDarkReferenceTable()
        {
            return new DarkReferenceTable
            {
                refr = new DarkRefCurve { data = new short[ConstDef.CALIB_TABLE_SIZE] },
                coeff = new DarkCoeffCurve { data = new ushort[ConstDef.CALIB_TABLE_SIZE] }
            };
        }

        private int SelectedSensorIndex => _config.SensorChannelIndex;
        private FRAME_DATA_SRC SelectedFrameDataSource => _config.FrameDataSource;
        private PEAK_SELECTION_MODE SelectedPeakSelectionMode => _config.PeakSelectionMode;
        private PEAK_SORT_MODE SelectedPeakSortMode => _config.PeakSortMode;
        private IMAGE_FILTER_WIDTH SelectedImageFilterWidth => _config.ImageFilterWidth;

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }
}

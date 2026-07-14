using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using Prism.Commands;
using ReeYin.Hardware.Sensor.JueXin;
using ReeYin.Hardware.Sensor.JueXin.API;
using ReeYin.Hardware.Sensor.JueXin.CustomUI.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.JueXin.CustomUI.ViewModels
{
    public class JueXinSensorViewModel : DialogViewModelBase
    {
        private DispatcherTimer? _chartRefreshTimer;
        private readonly HashSet<ViewXY> _initializedYAxisViews = new HashSet<ViewXY>();

        private Grid _singlePointChartGrid = new Grid();
        public Grid SinglePointChartGrid
        {
            get { return _singlePointChartGrid; }
            set { _singlePointChartGrid = value; RaisePropertyChanged(); }
        }

        private Grid _outlineChartGrid = new Grid();
        public Grid OutlineChartGrid
        {
            get { return _outlineChartGrid; }
            set { _outlineChartGrid = value; RaisePropertyChanged(); }
        }

        private JueXinSensorModel _modelParam = new JueXinSensorModel();
        public new JueXinSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public ViewXY SpectrumSignalChartView { get; } = CreateChartView(0, 2048, 9800, 10000);

        public ViewXY ThicknessSignalChartView { get; } = CreateChartView(-0.5, 0.5, -0.5, 0.5);

        public ViewXY SpectrumFftSpectrumChartView { get; } = CreateChartView(0, 2048, 9800, 10000);

        public ViewXY SpectrumFftChartView { get; } = CreateChartView(0, 500, 0, 100);

        public ViewXY SpectrumFftThicknessChartView { get; } = CreateChartView(-0.5, 0.5, -0.5, 0.5);

        public ViewXY EncoderThicknessChartView { get; } = CreateChartView(0, 1000, 0, 10);

        private int _selectedEncoderXAxisIndex;
        public int SelectedEncoderXAxisIndex
        {
            get { return _selectedEncoderXAxisIndex; }
            set { _selectedEncoderXAxisIndex = value; RaisePropertyChanged(); }
        }

        private const int SpectrumFftChartTabIndex = 2;
        private const string SpectrumFftTransferTip = "光谱频谱厚度同时传输，传输速率下降至 100Hz";

        private int _selectedChartTabIndex;
        public int SelectedChartTabIndex
        {
            get { return _selectedChartTabIndex; }
            set
            {
                if (_selectedChartTabIndex == value)
                {
                    return;
                }

                StopCollectIfRunning();
                _selectedChartTabIndex = value;
                RaisePropertyChanged();
                if (_selectedChartTabIndex == SpectrumFftChartTabIndex)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_selectedChartTabIndex == SpectrumFftChartTabIndex)
                        {
                            MessageView.Ins.MessageBoxShow(SpectrumFftTransferTip, eMsgType.Info);
                        }
                    }), DispatcherPriority.Background);
                }
            }
        }

        private int _spectrumStatisticFrameCount = 1;
        public int SpectrumStatisticFrameCount
        {
            get { return _spectrumStatisticFrameCount; }
            set { _spectrumStatisticFrameCount = Math.Max(1, value); RaisePropertyChanged(); }
        }

        private double _displayAverageSpectrumPeak;
        public double DisplayAverageSpectrumPeak
        {
            get { return _displayAverageSpectrumPeak; }
            set { _displayAverageSpectrumPeak = value; RaisePropertyChanged(); }
        }

        public string SpectrumCollectButtonText
        {
            get { return IsCollectModeRunning(JueXinCollectMode.Spectrum) ? "停止" : "开始"; }
        }

        public string ThicknessCollectButtonText
        {
            get { return IsCollectModeRunning(JueXinCollectMode.Thickness) ? "停止" : "开始"; }
        }

        public string SpectrumFftCollectButtonText
        {
            get { return IsCollectModeRunning(JueXinCollectMode.SpectrumFFT) ? "停止" : "开始"; }
        }

        public string ThickEncoderCollectButtonText
        {
            get { return IsCollectModeRunning(JueXinCollectMode.ThickEncoder) ? "停止" : "开始"; }
        }

        public IReadOnlyList<KeyValuePair<int, string>> ExposureModeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "手动曝光"),
            new KeyValuePair<int, string>(1, "自动曝光")
        };

        public IReadOnlyList<KeyValuePair<int, string>> SpectrumTypeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "原始光谱"),
            new KeyValuePair<int, string>(1, "校准光谱")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FftProcOptionItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭"),
            new KeyValuePair<int, string>(1, "频谱移动平均"),
            new KeyValuePair<int, string>(2, "频谱累加")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FftWindowItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "2"),
            new KeyValuePair<int, string>(1, "4")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FilterTypeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭滤波"),
            new KeyValuePair<int, string>(1, "中值滤波"),
            new KeyValuePair<int, string>(2, "平均滤波")
        };

        public IReadOnlyList<KeyValuePair<int, string>> SyncModeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "主机模式"),
            new KeyValuePair<int, string>(1, "从机模式")
        };

        public IReadOnlyList<KeyValuePair<int, string>> OnOffItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭"),
            new KeyValuePair<int, string>(1, "打开")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigSource, string>> TriggerSourceItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigSource, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_TRIGGER, "触发输入"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER1, "编码器1"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER2, "编码器2"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER3, "编码器3")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigMode, string>> TriggerModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_EDGE, "边沿触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_LEVEL, "电平触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_COUNT, "计数触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_LOCATION, "位置触发")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigLevel, string>> TriggerLevelItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigLevel, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_RISE_EDGE, "上升沿"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_FALL_EDGE, "下降沿"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_HIGH_LEVEL, "高电平"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_LOW_LEVEL, "低电平")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>> EncoderInputModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>(WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_SINGLE_PHASE, "单相"),
            new KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>(WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_TWO_PHASE, "双相")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigDirection, string>> EncoderDirectionItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigDirection, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_POSITIVE, "正向"),
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_NEGATIVE, "反向"),
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_BIDIRECTION, "双向")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliDecodeMode, string>> EncoderDecodeModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliDecodeMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X1, "x1"),
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X2, "x2"),
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X4, "x4")
        };

        public override void InitParam()
        {
            if (Param != null && (Param is JueXinSensor))
                ModelParam.JueXinSensor = Param as JueXinSensor ?? new JueXinSensor();
            else
                ModelParam.JueXinSensor = new JueXinSensor();

            StartChartRefreshTimer();
        }

        private static ViewXY CreateChartView(double xMin, double xMax, double yMin, double yMax)
        {
            return new ViewXY
            {
                XAxes = new AxisXCollection
                {
                    new AxisX
                    {
                        Minimum = xMin,
                        Maximum = xMax,
                        LabelsVisible = true
                    }
                },
                YAxes = new AxisYCollection
                {
                    new AxisY
                    {
                        Minimum = yMin,
                        Maximum = yMax,
                        LabelsVisible = true
                    }
                },
                FreeformPointLineSeries = new FreeformPointLineSeriesCollection
                {
                    new FreeformPointLineSeries
                    {
                        PointsVisible = false,
                        LineVisible = true,
                        Points = Array.Empty<SeriesPoint>()
                    }
                }
            };
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "Cancel":
                    _chartRefreshTimer?.Stop();
                    CloseDialog(ButtonResult.No);
                    break;

                case "Confirm":
                    _chartRefreshTimer?.Stop();
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.JueXinSensor },
                    });
                    break;

                default:
                    break;
            }
        });

        // 这里只处理已连接后的 SDK 参数和采集动作；连接/断开入口在 SensorSetView。
        public DelegateCommand<string> SensorCtrlCommand => new DelegateCommand<string>((order) =>
        {
            bool success = true;

            switch (order)
            {
                case "LoadParams":
                    success = ModelParam.JueXinSensor.LoadDeviceParams();
                    break;

                case "ApplyBasic":
                    success = ModelParam.JueXinSensor.ApplyBasicParams();
                    break;

                case "ApplySpectrum":
                    success = ModelParam.JueXinSensor.ApplySpectrumParams();
                    break;

                case "ApplyThickness":
                    success = ModelParam.JueXinSensor.ApplyThicknessParams();
                    break;

                case "ApplyTrigger":
                    success = ModelParam.JueXinSensor.ApplyTriggerConfig();
                    break;

                case "ApplySync":
                    success = ModelParam.JueXinSensor.ApplySyncConfig();
                    break;

                case "ApplyEncoder":
                    success = ModelParam.JueXinSensor.ApplyEncoder1Config();
                    break;

                case "ApplyEncoder1":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(1);
                    break;

                case "ApplyEncoder2":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(2);
                    break;

                case "ApplyEncoder3":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(3);
                    break;

                case "SetEncoderPosition":
                    success = ModelParam.JueXinSensor.SetEncoder1Position();
                    break;

                case "SetEncoderPosition1":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(1);
                    break;

                case "SetEncoderPosition2":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(2);
                    break;

                case "SetEncoderPosition3":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(3);
                    break;

                case "SampleDarkSignal":
                    success = ModelParam.JueXinSensor.ExecuteSdkCommand("SampleDarkSignal", "暗校准");
                    break;

                case "SampleLightSource":
                    success = ModelParam.JueXinSensor.ExecuteSdkCommand("SampleLightSource", "采集光源");
                    break;

                case "StartCollect":
                    ModelParam.JueXinSensor.StartCollect();
                    break;

                case "StartSpectrum":
                    StartCollect(JueXinCollectMode.Spectrum);
                    break;

                case "StartThickness":
                    StartCollect(JueXinCollectMode.Thickness);
                    break;

                case "StartSpectrumFFT":
                    StartCollect(JueXinCollectMode.SpectrumFFT);
                    break;

                case "StartThickEncoder":
                    StartCollect(JueXinCollectMode.ThickEncoder);
                    break;

                case "StopCollect":
                    ModelParam.JueXinSensor.StopCollect();
                    break;

                case "ClearCache":
                    ModelParam.JueXinSensor.ClearDataQueue();
                    break;

                default:
                    return;
            }

            if (!success)
            {
                MessageView.Ins.MessageBoxShow(ModelParam.JueXinSensor.LastSdkMessage, eMsgType.Warn);
            }
        });

        private void StartCollect(JueXinCollectMode mode)
        {
            if (IsCollectModeRunning(mode))
            {
                ModelParam.JueXinSensor.StopCollect();
                RaiseCollectButtonTextChanged();
                return;
            }

            if (ModelParam.JueXinSensor.State == HardwareState.Running)
            {
                ModelParam.JueXinSensor.StopCollect();
            }

            ModelParam.JueXinSensor.CollectMode = mode;
            ResetChartAxis(mode);
            ModelParam.JueXinSensor.StartCollect();
            RaiseCollectButtonTextChanged();
        }

        private void StartChartRefreshTimer()
        {
            if (_chartRefreshTimer != null)
            {
                _chartRefreshTimer.Start();
                return;
            }

            _chartRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _chartRefreshTimer.Tick += (s, e) => RefreshCharts();
            _chartRefreshTimer.Start();
        }

        private void RefreshCharts()
        {
            RaiseCollectButtonTextChanged();

            JueXinSensor sensor = ModelParam.JueXinSensor;
            ushort[] spectrum = sensor.GetLatestSpectrumSnapshot();
            float[] fft = sensor.GetLatestFftSnapshot();
            List<JueXinThicknessChartPoint> thicknessPoints = sensor.GetThicknessChartPointsSnapshot();
            List<JueXinEncoderThicknessChartPoint> encoderPoints = sensor.GetEncoderThicknessChartPointsSnapshot();
            DisplayAverageSpectrumPeak = sensor.GetAverageSpectrumPeak(SpectrumStatisticFrameCount);

            UpdateSpectrumChart(SpectrumSignalChartView, spectrum, spectrum.Length > 0 && ShouldUpdateYAxis(SpectrumSignalChartView));
            UpdateSpectrumChart(SpectrumFftSpectrumChartView, spectrum, spectrum.Length > 0 && ShouldUpdateYAxis(SpectrumFftSpectrumChartView));
            UpdateFftChart(SpectrumFftChartView, fft, fft.Length > 0 && ShouldUpdateYAxis(SpectrumFftChartView));
            UpdateThicknessChart(ThicknessSignalChartView, thicknessPoints, thicknessPoints.Count > 0 && ShouldUpdateYAxis(ThicknessSignalChartView));
            UpdateThicknessChart(SpectrumFftThicknessChartView, thicknessPoints, thicknessPoints.Count > 0 && ShouldUpdateYAxis(SpectrumFftThicknessChartView));
            UpdateEncoderThicknessChart(encoderPoints);
        }

        private static void UpdateSpectrumChart(ViewXY view, ushort[] values, bool updateYAxis)
        {
            if (values.Length == 0)
            {
                return;
            }

            SeriesPoint[] points = new SeriesPoint[values.Length];
            double min = values[0];
            double max = values[0];

            for (int i = 0; i < values.Length; i++)
            {
                points[i] = new SeriesPoint(i, values[i]);
                min = Math.Min(min, values[i]);
                max = Math.Max(max, values[i]);
            }

            ApplyRangePadding(ref min, ref max, 0.15, 1);
            UpdateChart(view, points, 0, values.Length - 1, min, max, updateYAxis);
        }

        private static void UpdateFftChart(ViewXY view, float[] values, bool updateYAxis)
        {
            if (values.Length == 0)
            {
                return;
            }

            SeriesPoint[] points = new SeriesPoint[values.Length];
            double min = values[0];
            double max = values[0];

            for (int i = 0; i < values.Length; i++)
            {
                points[i] = new SeriesPoint(i, values[i]);
                min = Math.Min(min, values[i]);
                max = Math.Max(max, values[i]);
            }

            ApplyRangePadding(ref min, ref max, 0.15, 1);
            UpdateChart(view, points, 0, values.Length - 1, min, max, updateYAxis);
        }

        private static void UpdateThicknessChart(ViewXY view, List<JueXinThicknessChartPoint> values, bool updateYAxis)
        {
            if (values.Count == 0)
            {
                return;
            }

            SeriesPoint[] points = new SeriesPoint[values.Count];
            double minX = values[0].X;
            double maxX = values[0].X;
            double minY = values[0].Thickness;
            double maxY = values[0].Thickness;

            for (int i = 0; i < values.Count; i++)
            {
                points[i] = new SeriesPoint(values[i].X, values[i].Thickness);
                minX = Math.Min(minX, values[i].X);
                maxX = Math.Max(maxX, values[i].X);
                minY = Math.Min(minY, values[i].Thickness);
                maxY = Math.Max(maxY, values[i].Thickness);
            }

            ApplyRangePadding(ref minY, ref maxY, 0.15, 0.001);
            UpdateChart(view, points, minX, maxX, minY, maxY, updateYAxis);
        }

        private void UpdateEncoderThicknessChart(List<JueXinEncoderThicknessChartPoint> values)
        {
            if (values.Count == 0)
            {
                return;
            }

            SeriesPoint[] points = new SeriesPoint[values.Count];
            double minX = GetEncoderXAxisValue(values[0]);
            double maxX = minX;
            double minY = values[0].Thickness;
            double maxY = values[0].Thickness;

            for (int i = 0; i < values.Count; i++)
            {
                double x = GetEncoderXAxisValue(values[i]);
                points[i] = new SeriesPoint(x, values[i].Thickness);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, values[i].Thickness);
                maxY = Math.Max(maxY, values[i].Thickness);
            }

            ApplyRangePadding(ref minY, ref maxY, 0.15, 0.001);
            UpdateChart(EncoderThicknessChartView, points, minX, maxX, minY, maxY, ShouldUpdateYAxis(EncoderThicknessChartView));
        }

        private double GetEncoderXAxisValue(JueXinEncoderThicknessChartPoint point)
        {
            return SelectedEncoderXAxisIndex switch
            {
                1 => point.Encoder1,
                2 => point.Encoder2,
                3 => point.Encoder3,
                _ => point.Index
            };
        }

        private static void UpdateChart(ViewXY view, SeriesPoint[] points, double xMin, double xMax, double yMin, double yMax, bool updateYAxis)
        {
            if (view.FreeformPointLineSeries.Count == 0)
            {
                return;
            }

            NormalizeRange(ref xMin, ref xMax);
            view.XAxes[0].Minimum = xMin;
            view.XAxes[0].Maximum = xMax;
            if (updateYAxis)
            {
                view.YAxes[0].Minimum = yMin;
                view.YAxes[0].Maximum = yMax;
            }
            view.FreeformPointLineSeries[0].Points = points;
        }

        private static void NormalizeRange(ref double min, ref double max)
        {
            if (Math.Abs(max - min) < 0.000001)
            {
                min -= 1;
                max += 1;
            }
        }

        private static void ApplyRangePadding(ref double min, ref double max, double paddingRatio, double minPadding)
        {
            NormalizeRange(ref min, ref max);
            double padding = Math.Max((max - min) * paddingRatio, minPadding);
            min -= padding;
            max += padding;
        }

        private bool IsCollectModeRunning(JueXinCollectMode mode)
        {
            return ModelParam.JueXinSensor.State == HardwareState.Running && ModelParam.JueXinSensor.CollectMode == mode;
        }

        private bool ShouldUpdateYAxis(ViewXY view)
        {
            if (_initializedYAxisViews.Contains(view))
            {
                return false;
            }

            _initializedYAxisViews.Add(view);
            return true;
        }

        private void ResetChartAxis(JueXinCollectMode mode)
        {
            switch (mode)
            {
                case JueXinCollectMode.Spectrum:
                    _initializedYAxisViews.Remove(SpectrumSignalChartView);
                    break;
                case JueXinCollectMode.Thickness:
                    _initializedYAxisViews.Remove(ThicknessSignalChartView);
                    break;
                case JueXinCollectMode.SpectrumFFT:
                    _initializedYAxisViews.Remove(SpectrumFftSpectrumChartView);
                    _initializedYAxisViews.Remove(SpectrumFftChartView);
                    _initializedYAxisViews.Remove(SpectrumFftThicknessChartView);
                    break;
                case JueXinCollectMode.ThickEncoder:
                    _initializedYAxisViews.Remove(EncoderThicknessChartView);
                    break;
            }
        }

        private void StopCollectIfRunning()
        {
            if (ModelParam.JueXinSensor.State == HardwareState.Running)
            {
                ModelParam.JueXinSensor.StopCollect();
                RaiseCollectButtonTextChanged();
            }
        }

        private void RaiseCollectButtonTextChanged()
        {
            RaisePropertyChanged(nameof(SpectrumCollectButtonText));
            RaisePropertyChanged(nameof(ThicknessCollectButtonText));
            RaisePropertyChanged(nameof(SpectrumFftCollectButtonText));
            RaisePropertyChanged(nameof(ThickEncoderCollectButtonText));
        }
    }
}

using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using HalconDotNet;
using Microsoft.Win32;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.JueXin_LineSpectrum.CustomUI.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace ReeYin.Hardware.Sensor.JueXin_LineSpectrum.CustomUI.ViewModels
{
    /// <summary>
    /// 觉芯线光谱传感器设置页面 ViewModel。
    /// </summary>
    public class JueXinLineSpectrumSensorViewModel : DialogViewModelBase
    {
        private const int SignalRefreshIntervalMs = 100;

        private readonly Common_Algorithm _commonAlgorithm;
        private readonly DispatcherTimer _signalRenderTimer;
        private readonly List<MeasureData> _collectedData = new List<MeasureData>();
        private JueXinLineSpectrumSensorModel _modelParam = new JueXinLineSpectrumSensorModel();
        private HObject? _disposeImage;
        private ImageResultsDisplay? _heightDisplayResult;
        private ViewXY _distanceChartView;
        private ViewXY _peakChartView;
        private bool _isSignalCollecting;
        private bool _distanceChartUserAdjusted;
        private bool _peakChartUserAdjusted;

        public JueXinLineSpectrumSensorViewModel()
        {
            _commonAlgorithm = PrismProvider.Container.Resolve(typeof(Common_Algorithm)) as Common_Algorithm ?? new Common_Algorithm();
            _distanceChartView = CreateSignalChartView(Array.Empty<float>(), "#2563EB", () => _distanceChartUserAdjusted = true);
            _peakChartView = CreateSignalChartView(Array.Empty<float>(), "#16A34A", () => _peakChartUserAdjusted = true);
            _signalRenderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SignalRefreshIntervalMs)
            };
            _signalRenderTimer.Tick += OnSignalRenderTimerTick;
        }

        public new JueXinLineSpectrumSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 右侧 2D 视图绑定的数据，当前使用采集结果中的第二组数组进行渲染。
        /// </summary>
        public HObject? DisposeImage
        {
            get { return _disposeImage; }
            set
            {
                if (ReferenceEquals(_disposeImage, value))
                {
                    return;
                }

                _disposeImage?.Dispose();
                _disposeImage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 右侧 3D 视图绑定的数据，使用位移数组构建高度图。
        /// </summary>
        public ImageResultsDisplay? HeightDisplayResult
        {
            get { return _heightDisplayResult; }
            set
            {
                if (ReferenceEquals(_heightDisplayResult, value))
                {
                    return;
                }

                _heightDisplayResult?.HeightImage?.Dispose();
                _heightDisplayResult = value;
                RaisePropertyChanged();
            }
        }

        public ViewXY DistanceChartView
        {
            get { return _distanceChartView; }
            private set { _distanceChartView = value; RaisePropertyChanged(); }
        }

        public ViewXY PeakChartView
        {
            get { return _peakChartView; }
            private set { _peakChartView = value; RaisePropertyChanged(); }
        }

        public string SignalCollectButtonText => _isSignalCollecting ? "停止" : "开始";

        public string CollectedDataCountText => $"采集条数：{_collectedData.Count}";

        public override void InitParam()
        {
            if (Param is JueXinLineSpectrumSensor sensor)
            {
                ModelParam.Sensor = sensor;
            }
            else
            {
                ModelParam.Sensor = new JueXinLineSpectrumSensor();
            }

            InitializeSensorParameters();
        }

        public override void OnDialogClosed()
        {
            StopCollection(false);
            DisposeImage = null;
            HeightDisplayResult = null;
            base.OnDialogClosed();
        }

        /// <summary>
        /// 页面打开时优先读取设备当前参数，未连接时则继续显示本地缓存。
        /// </summary>
        private void InitializeSensorParameters()
        {
            ModelParam.Sensor?.RefreshParametersFromDevice();
        }

        /// <summary>
        /// 将采集缓存中的高度数组和灰度数组分别转换为图表控件可识别的数据对象。
        /// </summary>
        private void RenderCollectedData()
        {
            DrainSensorData();
            List<MeasureData> measureData = new List<MeasureData>(_collectedData);
            if (measureData.Count == 0)
            {
                return;
            }

            List<float[]> grayRows = new List<float[]>(measureData.Count);
            List<float[]> heightRows = new List<float[]>(measureData.Count);
            foreach (MeasureData item in measureData)
            {
                if (item?.AreaData == null || item.AreaData.Count == 0)
                {
                    continue;
                }

                if (item.AreaData[0] != null && item.AreaData[0].Length > 0)
                {
                    heightRows.Add(item.AreaData[0]);
                }

                if (item.AreaData.Count > 1 && item.AreaData[1] != null && item.AreaData[1].Length > 0)
                {
                    grayRows.Add(item.AreaData[1]);
                }
            }

            if (grayRows.Count > 0 && _commonAlgorithm.ConvertListToHObject(grayRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Gray, out HObject grayImage) == 0)
            {
                DisposeImage = grayImage;
            }

            if (heightRows.Count > 0 && Common_Algorithm.ConvertListToMat(heightRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage) == 0)
            {
                HeightDisplayResult = new ImageResultsDisplay
                {
                    HeightImage = heightImage
                };
            }
        }

        /// <summary>
        /// 启动传感器采集，并开启位移信号实时刷新。
        /// </summary>
        private void StartCollection()
        {
            if (_isSignalCollecting)
            {
                return;
            }

            JueXinLineSpectrumSensor? sensor = ModelParam.Sensor;
            if (sensor == null)
            {
                return;
            }

            DisposeImage = null;
            HeightDisplayResult = null;
            _collectedData.Clear();
            RaisePropertyChanged(nameof(CollectedDataCountText));
            ResetSignalCharts();
            sensor.StartCollect();
            if (sensor.State != HardwareState.Running)
            {
                return;
            }

            _isSignalCollecting = true;
            RaisePropertyChanged(nameof(SignalCollectButtonText));
            _signalRenderTimer.Start();
        }

        /// <summary>
        /// 停止传感器采集，并按需渲染已缓存的 2D/3D 数据。
        /// </summary>
        private void StopCollection(bool renderCollectedData)
        {
            if (!_isSignalCollecting)
            {
                return;
            }

            _signalRenderTimer.Stop();
            ModelParam.Sensor?.StopCollect();
            _isSignalCollecting = false;
            RaisePropertyChanged(nameof(SignalCollectButtonText));

            if (renderCollectedData)
            {
                RenderCollectedData();
            }
        }

        /// <summary>
        /// 定时取最新一帧数据刷新位移和峰值曲线。
        /// </summary>
        private void OnSignalRenderTimerTick(object? sender, EventArgs e)
        {
            List<MeasureData> newData = DrainSensorData();
            MeasureData? latest = newData.LastOrDefault(item => item?.AreaData != null && item.AreaData.Count > 0);
            if (latest?.AreaData == null || latest.AreaData.Count == 0)
            {
                return;
            }

            float[] distances = latest.AreaData[0] ?? Array.Empty<float>();
            float[] peaks = latest.AreaData.Count > 1 ? latest.AreaData[1] ?? Array.Empty<float>() : Array.Empty<float>();
            UpdateSignalChart(DistanceChartView, distances, !_distanceChartUserAdjusted);
            UpdateSignalChart(PeakChartView, peaks, !_peakChartUserAdjusted);
        }

        /// <summary>
        /// 取出传感器队列数据，并保存到页面采集缓存。
        /// </summary>
        private List<MeasureData> DrainSensorData()
        {
            List<MeasureData> newData = ModelParam.Sensor?.ReceiveSensorData() ?? new List<MeasureData>();
            if (newData.Count > 0)
            {
                _collectedData.AddRange(newData);
                RaisePropertyChanged(nameof(CollectedDataCountText));
            }

            return newData;
        }

        /// <summary>
        /// 清空位移和峰值曲线显示。
        /// </summary>
        private void ResetSignalCharts()
        {
            _distanceChartUserAdjusted = false;
            _peakChartUserAdjusted = false;
            DistanceChartView = CreateSignalChartView(Array.Empty<float>(), "#2563EB", () => _distanceChartUserAdjusted = true);
            PeakChartView = CreateSignalChartView(Array.Empty<float>(), "#16A34A", () => _peakChartUserAdjusted = true);
        }

        /// <summary>
        /// 根据通道数据创建一张信号折线图。
        /// </summary>
        private static ViewXY CreateSignalChartView(float[] values, string lineColor, Action? chartAdjusted = null)
        {
            (double minimum, double maximum) = GetAxisRange(values);
            FreeformPointLineSeries series = new FreeformPointLineSeries
            {
                LineVisible = true,
                PointsVisible = false,
                IncludeInAutoFit = true,
                LineStyle = new LineStyle
                {
                    Color = ParseColor(lineColor),
                    Width = 2
                },
                Points = values.Length == 0
                    ? Array.Empty<SeriesPoint>()
                    : values.Select((value, index) => new SeriesPoint(index, value)).ToArray()
            };

            ViewXY view = new ViewXY
            {
                ZoomPanOptions = new ZoomPanOptions
                {
                    WheelZooming = WheelZooming.HorizontalAndVertical,
                    AxisWheelAction = AxisWheelAction.ZoomAll
                },
                GraphBackground = new Fill
                {
                    Style = RectFillStyle.ColorOnly,
                    Color = ParseColor("#FFFDFEFF")
                },
                Margins = new System.Windows.Thickness(10, 6, 10, 8),
                XAxes = new AxisXCollection
                {
                    new AxisX
                    {
                        Minimum = 0,
                        Maximum = values.Length > 1 ? values.Length - 1 : 1,
                        LabelsVisible = true
                    }
                },
                YAxes = new AxisYCollection
                {
                    new AxisY
                    {
                        Minimum = minimum,
                        Maximum = maximum,
                        LabelsVisible = true
                    }
                },
                FreeformPointLineSeries = new FreeformPointLineSeriesCollection
                {
                    series
                }
            };

            if (chartAdjusted != null)
            {
                view.Zoomed += (_, _) => chartAdjusted();
                view.Panned += (_, _) => chartAdjusted();
            }

            return view;
        }

        /// <summary>
        /// 更新曲线数据，避免实时刷新时重建图表导致缩放状态丢失。
        /// </summary>
        private static void UpdateSignalChart(ViewXY chartView, float[] values, bool fitAxes)
        {
            FreeformPointLineSeries? series = chartView.FreeformPointLineSeries.FirstOrDefault();
            if (series == null)
            {
                return;
            }

            series.Points = values.Length == 0
                ? Array.Empty<SeriesPoint>()
                : values.Select((value, index) => new SeriesPoint(index, value)).ToArray();

            if (!fitAxes)
            {
                return;
            }

            AxisX? xAxis = chartView.XAxes.FirstOrDefault();
            AxisY? yAxis = chartView.YAxes.FirstOrDefault();
            if (xAxis == null || yAxis == null)
            {
                return;
            }

            xAxis.Minimum = 0;
            xAxis.Maximum = values.Length > 1 ? values.Length - 1 : 1;
            (double minimum, double maximum) = GetAxisRange(values);
            yAxis.Minimum = minimum;
            yAxis.Maximum = maximum;
        }

        /// <summary>
        /// 计算 Y 轴范围，并为曲线留出显示边距。
        /// </summary>
        private static (double Minimum, double Maximum) GetAxisRange(float[] values)
        {
            if (values.Length == 0)
            {
                return (-1, 1);
            }

            double minimum = values.Min();
            double maximum = values.Max();
            if (Math.Abs(maximum - minimum) < 0.000001)
            {
                double padding = Math.Max(1, Math.Abs(maximum) * 0.1);
                return (minimum - padding, maximum + padding);
            }

            double range = maximum - minimum;
            return (minimum - range * 0.1, maximum + range * 0.1);
        }

        /// <summary>
        /// 将颜色字符串转换为 WPF 颜色。
        /// </summary>
        private static Color ParseColor(string hex)
        {
            object? value = System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return value is Color color ? color : Colors.SteelBlue;
        }

        /// <summary>
        /// 保存当前页面显示的灰度图。
        /// </summary>
        private void SaveGrayImage()
        {
            if (DisposeImage == null)
            {
                MessageBox.Show("当前没有灰度图，请先采集数据。", "保存失败");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "保存灰度图",
                Filter = "PNG图像|*.png|BMP图像|*.bmp|TIFF图像|*.tif",
                DefaultExt = ".png",
                FileName = $"gray_{DateTime.Now:yyyyMMdd_HHmmssfff}.png",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                HOperatorSet.WriteImage(DisposeImage, GetHalconImageFormat(dialog.FileName), 0, dialog.FileName);
                MessageBox.Show("灰度图保存成功。", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"灰度图保存失败：{ex.Message}", "保存失败");
            }
        }

        /// <summary>
        /// 保存当前页面显示的高度图。
        /// </summary>
        private void SaveHeightImage()
        {
            Mat? heightImage = HeightDisplayResult?.HeightImage;
            if (heightImage == null || heightImage.Empty())
            {
                MessageBox.Show("当前没有高度图，请先采集数据。", "保存失败");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "保存高度图",
                Filter = "TIFF图像|*.tif",
                DefaultExt = ".tif",
                FileName = $"height_{DateTime.Now:yyyyMMdd_HHmmssfff}.tif",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (Cv2.ImWrite(dialog.FileName, heightImage))
                {
                    MessageBox.Show("高度图保存成功。", "提示");
                }
                else
                {
                    MessageBox.Show("高度图保存失败。", "保存失败");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"高度图保存失败：{ex.Message}", "保存失败");
            }
        }

        /// <summary>
        /// 根据文件扩展名选择 Halcon 保存格式。
        /// </summary>
        private static string GetHalconImageFormat(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".bmp":
                    return "bmp";

                case ".tif":
                case ".tiff":
                    return "tiff";

                default:
                    return "png";
            }
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "StartCollect":
                    StartCollection();
                    break;

                case "StopCollect":
                    StopCollection(true);
                    break;

                case "ToggleSignalCollect":
                    if (_isSignalCollecting)
                    {
                        StopCollection(false);
                    }
                    else
                    {
                        StartCollection();
                    }
                    break;

                case "SaveGrayImage":
                    SaveGrayImage();
                    break;

                case "SaveHeightImage":
                    SaveHeightImage();
                    break;

                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;

                case "Confirm":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;
            }
        });
    }
}

using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Series3D;
using Microsoft.Win32;
using OpenCvSharp;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.UI.UserControls.GrayAndHeightChart.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using ProjectionType = Arction.Wpf.Charting.ProjectionType;

namespace ReeYin_V.UI.UserControls.GrayAndHeightChart
{
    public partial class GrayAndHeightChartView : IDisposable
    {
        public static readonly DependencyProperty DisplayResultProperty =
            DependencyProperty.Register(
                nameof(DisplayResult),
                typeof(ImageResultsDisplay),
                typeof(GrayAndHeightChartView),
                new PropertyMetadata(null, OnDisplayResultChanged));

        private const double DefaultDimensionWidth = 300.0;
        private const double DefaultDimensionHeight = 30.0;
        private const double DefaultDimensionDepth = 200.0;

        private SurfaceGridSeries3D? _surfaceGround;
        private readonly GrayAndHeightChartModel _chartModel = new();
        private readonly string _configFilePath;
        private bool _isProcessing;
        private bool _enableDataCursor;
        private int _maxDataPointsForCursor = 512 * 512;
        private CameraState _initialCameraState;
        private bool _cameraStateInitialized;
        private bool _isChartLayoutRefreshPending;
        private double _baseDimensionWidth = DefaultDimensionWidth;
        private double _baseDimensionDepth = DefaultDimensionDepth;

        public enum QualityMode
        {
            UltraFast,
            Fast,
            Balanced,
            HighQuality,
            UltraHighQuality,
            UltimaHighQuality
        }

        public enum ColorPaletteType
        {
            Classic,
            Heatmap
        }

        public struct CameraState
        {
            public double ViewDistance;
            public double RotationX;
            public double RotationY;
            public double RotationZ;
            public ProjectionType Projection;
        }

        public GrayAndHeightChartView()
        {
            InitializeComponent();

            _configFilePath = Path.Combine(PrismProvider.AppBasePath, "SealingNailsSDK", "ini", "FeatureConfig.json");

            InitializeSelectionsFromView();
            SetUpHeightChart();
            BindViewEvents();
            LoadConfiguration();
            UpdateDataRangeDisplay();
            UpdateDataCursorButtonStatus();
        }

        /// <summary>
        /// 宿主通过绑定传入的显示结果。
        /// 当前控件以 <see cref="ImageResultsDisplay.HeightImage"/> 作为 3D 曲面数据源。
        /// </summary>
        public ImageResultsDisplay? DisplayResult
        {
            get => (ImageResultsDisplay?)GetValue(DisplayResultProperty);
            set => SetValue(DisplayResultProperty, value);
        }

        private static async void OnDisplayResultChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is not GrayAndHeightChartView view)
            {
                return;
            }

            if (args.NewValue is ImageResultsDisplay result)
            {
                await view.ApplyDisplayResultAsync(result);
                return;
            }

            if (args.NewValue == null)
            {
                await view.ClearDisplayResultAsync();
            }
        }

        /// <summary>
        /// 从界面的默认选项初始化内部状态，避免首次刷新时出现配置不同步。
        /// </summary>
        private void InitializeSelectionsFromView()
        {
            UpdateQualityModeFromSelection();
            UpdatePaletteFromSelection();
        }

        /// <summary>
        /// 统一绑定页面级事件，便于把页面交互逻辑与数据处理逻辑分离。
        /// </summary>
        private void BindViewEvents()
        {
            cb_QualityMode.SelectionChanged += OnQualityModeChanged;
            cb_ColorPalette.SelectionChanged += OnColorPaletteChanged;
            slider_ViewDistance.ValueChanged += OnViewDistanceChanged;
            btn_ResetView.Click += OnResetViewClicked;
            btn_FitView.Click += OnFitViewClicked;

            ToggleButton? dataCursorButton = FindName("btn_ToggleDataCursor") as ToggleButton;
            if (dataCursorButton != null)
            {
                dataCursorButton.Checked += OnDataCursorToggled;
                dataCursorButton.Unchecked += OnDataCursorToggled;
            }

            Focusable = true;
            KeyDown += OnKeyDown;
            SizeChanged += OnViewSizeChanged;
        }

        /// <summary>
        /// 当控件宿主区域尺寸变化时，请求图表重新布局。
        /// </summary>
        private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
            {
                return;
            }

            RequestChartLayoutRefresh();
        }

        /// <summary>
        /// 合并连续的布局刷新请求，避免拖拽缩放时重复执行昂贵的图表重排。
        /// </summary>
        private void RequestChartLayoutRefresh()
        {
            if (_isChartLayoutRefreshPending || LightningChart == null)
            {
                return;
            }

            _isChartLayoutRefreshPending = true;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _isChartLayoutRefreshPending = false;

                if (LightningChart == null)
                {
                    return;
                }

                LightningChart.InvalidateMeasure();
                LightningChart.InvalidateArrange();
                LightningChart.BeginUpdate();
                LightningChart.EndUpdate();
            }));
        }

        /// <summary>
        /// 初始化 3D 图表的基础状态，包括相机、尺寸和渲染选项。
        /// </summary>
        private void SetUpHeightChart()
        {
            LightningChart.BeginUpdate();

            LightningChart.Title.Text = string.Empty;
            LightningChart.ActiveView = ActiveView.View3D;
            LightningChart.View3D.LegendBox.ShowCheckboxes = false;
            LightningChart.View3D.LegendBox.Visible = true;
            LightningChart.View3D.Camera.MinimumViewDistance = 10;
            LightningChart.View3D.Camera.ViewDistance = 180.0;
            LightningChart.View3D.Camera.Projection = ProjectionType.Orthographic;
            LightningChart.View3D.Camera.RotationX = 90;
            LightningChart.View3D.Camera.RotationY = 0;
            LightningChart.View3D.Camera.RotationZ = 90;
            LightningChart.View3D.ZoomPanOptions.DevicePrimaryButtonDoubleClickAction = DoubleClickAction3D.Off;
            LightningChart.View3D.XAxisPrimary3D.Reversed = false;
            LightningChart.View3D.YAxisPrimary3D.Reversed = false;
            LightningChart.View3D.ZAxisPrimary3D.Reversed = false;
            _baseDimensionWidth = DefaultDimensionWidth;
            _baseDimensionDepth = DefaultDimensionDepth;
            nud_3DWidthScale.Value = 1.0;
            nud_3DLengthScale.Value = 1.0;
            nud_3DHeight.Value = DefaultDimensionHeight;
            ApplyDimensionSettingsCore(LightningChart);

            foreach (var wall in LightningChart.View3D.GetWalls())
            {
                wall.Visible = false;
            }

            foreach (var axis in LightningChart.View3D.GetAxes())
            {
                axis.Visible = false;
            }

            OptimizeLightningChartForLargeData();
            ConfigureDataCursor();
            SaveInitialCameraState();

            LightningChart.EndUpdate();
        }

        /// <summary>
        /// 调整 LightningChart 的性能选项，让大尺寸深度图的交互更平滑。
        /// </summary>
        private void OptimizeLightningChartForLargeData()
        {
            LightningChart.ChartRenderOptions.AntiAliasLevel = 0;
            LightningChart.View3D.ZoomPanOptions.WheelZoomFactor = 1.2;
        }

        /// <summary>
        /// 保存初始镜头参数，供重置视图时恢复。
        /// </summary>
        private void SaveInitialCameraState()
        {
            _initialCameraState = new CameraState
            {
                ViewDistance = LightningChart.View3D.Camera.ViewDistance,
                RotationX = LightningChart.View3D.Camera.RotationX,
                RotationY = LightningChart.View3D.Camera.RotationY,
                RotationZ = LightningChart.View3D.Camera.RotationZ,
                Projection = LightningChart.View3D.Camera.Projection
            };

            _cameraStateInitialized = true;
            slider_ViewDistance.Value = _initialCameraState.ViewDistance;
            tb_ViewDistance.Text = ((int)_initialCameraState.ViewDistance).ToString();
        }

        /// <summary>
        /// 读取图表相关配置文件。
        /// 当前主要承载算法参数和默认显示配置。
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                _chartModel.LoadConfiguration(_configFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载 GrayAndHeightChart 配置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理宿主绑定进来的新结果，并在需要时切回 UI 线程刷新图表。
        /// </summary>
        private async Task ApplyDisplayResultAsync(ImageResultsDisplay result)
        {
            if (result?.HeightImage == null)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Task applyTask = await Dispatcher.InvokeAsync(
                    () => ApplyDisplayResultAsync(result),
                    DispatcherPriority.Send);
                await applyTask;
                return;
            }

            Mat? clonedHeightImage = null;

            try
            {
                clonedHeightImage = result.HeightImage.Clone();
                await UpdateMeasureDataAsync(
                    new ImageResultsDisplay
                    {
                        HeightImage = clonedHeightImage
                    });
                clonedHeightImage = null;
            }
            finally
            {
                clonedHeightImage?.Dispose();
            }
        }

        /// <summary>
        /// 当绑定值被清空时，移除当前曲面并重置数据范围显示。
        /// </summary>
        private async Task ClearDisplayResultAsync()
        {
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(ClearDisplayResultCore, DispatcherPriority.Send);
                return;
            }

            ClearDisplayResultCore();
        }

        private void ClearDisplayResultCore()
        {
            if (LightningChart == null)
            {
                return;
            }

            LightningChart.BeginUpdate();
            try
            {
                LightningChart.View3D.SurfaceGridSeries3D.Clear();
                _surfaceGround = null;
                _chartModel.ClearDataRange();
                UpdateDataRangeDisplay();
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        /// <summary>
        /// 从本地选择深度图文件并加载到当前 3D 图表。
        /// </summary>
        #if false
        /// <summary>
        /// 手动从本地选择深度图文件，并复用统一的图表刷新流程进行显示。
        /// </summary>
        private async void OnLoadDepthImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "选择深度图",
                Filter = "深度图|*.tif;*.tiff;*.png;*.bmp;*.jpg;*.jpeg;*.exr|TIFF|*.tif;*.tiff|EXR|*.exr|常见图片|*.png;*.bmp;*.jpg;*.jpeg|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await LoadDepthImageFromFileAsync(dialog.FileName);
        }

        #endif

        /// <summary>
        /// 从本地选择深度图文件并加载到当前 3D 图表。
        /// </summary>
        private async void OnLoadDepthImageClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "选择深度图",
                Filter = "深度图|*.tif;*.tiff;*.png;*.bmp;*.jpg;*.jpeg;*.exr|TIFF|*.tif;*.tiff|EXR|*.exr|常见图片|*.png;*.bmp;*.jpg;*.jpeg|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await LoadDepthImageFromFileAsync(dialog.FileName);
        }

        /// <summary>
        /// 从文件读取深度图，转换为结果对象后交给统一的刷新流程处理。
        /// </summary>
        private async Task LoadDepthImageFromFileAsync(string filePath, CancellationToken token = default)
        {
            Mat? depthImage = null;

            try
            {
                if (_isProcessing)
                {
                    ShowDepthLoadError("当前图表正在刷新，请稍后再试。");
                    return;
                }

                depthImage = _chartModel.LoadDepthImage(filePath);
                SetChartName(Path.GetFileName(filePath));

                ImageResultsDisplay result = new()
                {
                    HeightImage = depthImage
                };

                await UpdateMeasureDataAsync(result, token);
            }
            catch (Exception ex)
            {
                ShowDepthLoadError(ex.Message);
                return;
                #if false
                MessageBox.Show(
                    $"加载深度图失败：{ex.Message}",
                    "GrayAndHeightChart",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                #endif
            }
            finally
            {
                depthImage?.Dispose();
            }
        }

        /// <summary>
        /// 统一显示深度图加载失败提示。
        /// </summary>
        private void ShowDepthLoadError(string error)
        {
            MessageBox.Show(
                $"加载深度图失败：{error}",
                "GrayAndHeightChart",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// 将高度图转换为矩阵数据，并分阶段刷新 3D 曲面。
        /// </summary>
        public async Task UpdateMeasureDataAsync(ImageResultsDisplay result, CancellationToken token = default)
        {
            if (_isProcessing || result?.HeightImage == null)
            {
                return;
            }

            _isProcessing = true;
            Mat? rotatedHeightImage = null;

            try
            {
                rotatedHeightImage = new Mat();
                Cv2.Rotate(result.HeightImage, rotatedHeightImage, RotateFlags.Rotate90Clockwise);

                float[][] heightData = await Task.Run(
                    () => GrayAndHeightChartModel.MatToFloat2DArray(rotatedHeightImage),
                    token);

                GrayAndHeightChartDepthFilterResult filteredResult = await Task.Run(
                    () => _chartModel.FilterDepthData(heightData, -50000f, 50000f),
                    token);

                await ProgressiveLoadSurfaceData(filteredResult.FilteredData, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateMeasureDataAsync error: {ex.Message}");
            }
            finally
            {
                rotatedHeightImage?.Dispose();
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 先渲染快速预览，再按当前质量档位补绘最终曲面。
        /// </summary>
        private async Task ProgressiveLoadSurfaceData(float[][] heightData, CancellationToken token)
        {
            if (heightData == null || heightData.Length == 0 || heightData[0].Length == 0)
            {
                return;
            }

            float[][] previewData = await Task.Run(
                () => _chartModel.SmartSampleFloatArray(heightData, GrayAndHeightChartModel.FastSampleSize, false),
                token);

            await Application.Current.Dispatcher.InvokeAsync(
                () => FillSurfaceOptimized(LightningChart, ref _surfaceGround, previewData),
                DispatcherPriority.Send,
                token);

            int finalSampleSize = _chartModel.GetSampleSizeForQuality(_chartModel.CurrentQualityMode);
            if (finalSampleSize == GrayAndHeightChartModel.FastSampleSize)
            {
                return;
            }

            float[][] finalData = await Task.Run(
                () => _chartModel.SmartSampleFloatArray(heightData, finalSampleSize),
                token);

            await Application.Current.Dispatcher.InvokeAsync(
                () => FillSurfaceOptimized(LightningChart, ref _surfaceGround, finalData),
                DispatcherPriority.Normal,
                token);
        }

        /// <summary>
        /// 将高度矩阵写入 LightningChart 曲面，并同步更新尺寸、坐标范围和调色板。
        /// </summary>
        private void FillSurfaceOptimized(
            LightningChart chart,
            ref SurfaceGridSeries3D? surfaceGridSeries3D,
            float[][] heightDatas,
            bool reverseZ = false)
        {
            if (heightDatas == null || heightDatas.Length == 0 || heightDatas[0] == null || heightDatas[0].Length == 0)
            {
                return;
            }

            chart.BeginUpdate();

            try
            {
                int rows = heightDatas.Length;
                int cols = heightDatas[0].Length;
                SmartEnableDataCursor(rows * cols);
                AdjustChartDimensionsToData(chart, rows, cols);

                if (surfaceGridSeries3D == null)
                {
                    surfaceGridSeries3D = new SurfaceGridSeries3D(
                        chart.View3D,
                        Axis3DBinding.Primary,
                        Axis3DBinding.Primary,
                        Axis3DBinding.Primary);

                    chart.View3D.SurfaceGridSeries3D.Add(surfaceGridSeries3D);
                    surfaceGridSeries3D.Fill = SurfaceFillStyle.PalettedByY;
                    surfaceGridSeries3D.ContourLineType = ContourLineType3D.None;
                    surfaceGridSeries3D.WireframeType = SurfaceWireframeType3D.None;
                }

                surfaceGridSeries3D.SetSize(rows, cols);
                SurfacePoint[,] grid = surfaceGridSeries3D.Data;

                double xMin = chart.View3D.XAxisPrimary3D.Minimum;
                double xMax = chart.View3D.XAxisPrimary3D.Maximum;
                double zMin = chart.View3D.ZAxisPrimary3D.Minimum;
                double zMax = chart.View3D.ZAxisPrimary3D.Maximum;

                double[] xs = new double[rows];
                double[] zs = new double[cols];
                double xStep = rows <= 1 ? 0.0 : (xMax - xMin) / (rows - 1);
                double zStep = cols <= 1 ? 0.0 : (zMax - zMin) / (cols - 1);

                for (int i = 0; i < rows; i++)
                {
                    xs[i] = xMin + xStep * i;
                }

                for (int j = 0; j < cols; j++)
                {
                    zs[j] = zMin + zStep * j;
                }

                float minVal = float.MaxValue;
                float maxVal = float.MinValue;
                int validCount = 0;

                for (int i = 0; i < rows; i++)
                {
                    int srcRowIndex = reverseZ ? rows - 1 - i : i;
                    float[] srcRow = heightDatas[srcRowIndex];
                    double x = xs[i];

                    for (int j = 0; j < cols; j++)
                    {
                        float y = srcRow[j];
                        SurfacePoint point = grid[i, j];
                        point.X = x;
                        point.Y = y;
                        point.Z = zs[j];
                        point.Value = y;
                        grid[i, j] = point;

                        if (float.IsNaN(y) || float.IsInfinity(y))
                        {
                            continue;
                        }

                        validCount++;
                        minVal = Math.Min(minVal, y);
                        maxVal = Math.Max(maxVal, y);
                    }
                }

                if (validCount > 0 && maxVal > minVal)
                {
                    double range = maxVal - minVal;
                    double padding = range * 0.1;
                    chart.View3D.YAxisPrimary3D.SetRange(minVal - padding, maxVal + padding);
                }

                surfaceGridSeries3D.ContourPalette = _chartModel.CreatePalette(surfaceGridSeries3D, heightDatas);
                surfaceGridSeries3D.InvalidateData();
                UpdateDataRangeDisplay();
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        public void SetChartName(string chartName)
        {
            LightningChart.Title.Text = chartName ?? string.Empty;
        }

        public void ShowSettingView()
        {
        }

        /// <summary>
        /// 由外部设置质量档位，并同步更新界面下拉框。
        /// </summary>
        public void SetQualityMode(QualityMode mode)
        {
            _chartModel.CurrentQualityMode = mode;
            SelectComboItemByTag(cb_QualityMode, mode.ToString());
        }

        private void OnQualityModeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateQualityModeFromSelection();
        }

        private void UpdateQualityModeFromSelection()
        {
            if (cb_QualityMode.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string tag &&
                Enum.TryParse(tag, out QualityMode mode))
            {
                _chartModel.CurrentQualityMode = mode;
            }
        }

        private void OnColorPaletteChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePaletteFromSelection();

            if (_surfaceGround != null && !_isProcessing)
            {
                RefreshPalette();
            }
        }

        private void UpdatePaletteFromSelection()
        {
            if (cb_ColorPalette.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string tag &&
                Enum.TryParse(tag, out ColorPaletteType paletteType))
            {
                _chartModel.CurrentPaletteType = paletteType;
            }
        }

        /// <summary>
        /// 基于当前曲面数据重建调色板，供颜色方案和色域切换时复用。
        /// </summary>
        private void RefreshPalette()
        {
            if (_surfaceGround == null)
            {
                return;
            }

            SurfacePoint[,] surfaceData = _surfaceGround.Data;
            int rows = surfaceData.GetLength(0);
            int cols = surfaceData.GetLength(1);
            float[][] heightData = new float[rows][];

            for (int i = 0; i < rows; i++)
            {
                heightData[i] = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    heightData[i][j] = (float)surfaceData[i, j].Value;
                }
            }

            LightningChart.BeginUpdate();
            try
            {
                _surfaceGround.ContourPalette = _chartModel.CreatePalette(_surfaceGround, heightData);
                _surfaceGround.InvalidateData();
                UpdateDataRangeDisplay();
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        public void RefreshWithCurrentQuality()
        {
            if (_surfaceGround != null && !_isProcessing)
            {
                RefreshPalette();
            }
        }

        /// <summary>
        /// 按当前状态刷新数据光标显示。
        /// </summary>
        private void ConfigureDataCursor()
        {
            LightningChart.View3D.DataCursor.Visible = _enableDataCursor;
            LightningChart.View3D.DataCursor.ShowLabels = _enableDataCursor;

            if (_enableDataCursor)
            {
                LightningChart.View3D.DataCursor.LineStyle.Width = 1;
                LightningChart.View3D.DataCursor.LineStyle.Color = System.Windows.Media.Colors.Yellow;
            }
        }

        /// <summary>
        /// 根据数据点数量自动决定是否启用数据光标，避免大数据量下交互卡顿。
        /// </summary>
        private void SmartEnableDataCursor(int dataPoints)
        {
            bool shouldEnable = dataPoints <= _maxDataPointsForCursor;
            if (shouldEnable == _enableDataCursor)
            {
                return;
            }

            _enableDataCursor = shouldEnable;
            ConfigureDataCursor();
            Application.Current.Dispatcher.InvokeAsync(UpdateDataCursorButtonStatus);
        }

        public void ToggleDataCursor(bool enable)
        {
            _enableDataCursor = enable;
            ConfigureDataCursor();
            Application.Current.Dispatcher.InvokeAsync(UpdateDataCursorButtonStatus);
        }

        public bool IsDataCursorEnabled()
        {
            return _enableDataCursor;
        }

        public void SetDataCursorThreshold(int maxDataPoints)
        {
            _maxDataPointsForCursor = maxDataPoints;
        }

        private void UpdateDataCursorButtonStatus()
        {
            ToggleButton? dataCursorButton = FindName("btn_ToggleDataCursor") as ToggleButton;
            if (dataCursorButton == null)
            {
                return;
            }

            dataCursorButton.IsChecked = _enableDataCursor;
            ApplyDataCursorToolTip(dataCursorButton);
            return;
#if false
            string status = _enableDataCursor ? "已启用" : "已禁用";
            dataCursorButton.ToolTip =
                $"数据光标{status}\n{(_enableDataCursor ? "可查看鼠标位置的数据值" : "点击启用数据光标功能")}\n大数据集时会自动优化性能";
#endif
        }

        /// <summary>
        /// 统一生成数据光标按钮提示，避免多处拼接提示文案。
        /// </summary>
        private void UpdateDataCursorToolTip(ToggleButton dataCursorButton)
        {
            string status = _enableDataCursor ? "已启用" : "已禁用";
            dataCursorButton.ToolTip =
                $"数据光标{status}\n{(_enableDataCursor ? "可查看鼠标位置的数据值" : "点击启用数据光标功能")}\n大数据集时会自动优化性能";
        }

        /// <summary>
        /// 统一生成数据光标按钮提示文案。
        /// </summary>
        private void ApplyDataCursorToolTip(ToggleButton dataCursorButton)
        {
            string status = _enableDataCursor ? "已启用" : "已禁用";
            dataCursorButton.ToolTip =
                $"数据光标{status}\n{(_enableDataCursor ? "可查看鼠标位置的数据值" : "点击启用数据光标功能")}\n大数据集时会自动优化性能";
        }

        private void OnViewDistanceChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LightningChart?.View3D?.Camera == null)
            {
                return;
            }

            LightningChart.View3D.Camera.ViewDistance = e.NewValue;
            tb_ViewDistance.Text = ((int)e.NewValue).ToString();
        }

        /// <summary>
        /// 按当前宽度、长度和高度缩放参数刷新 3D 场景尺寸。
        /// </summary>
        private void Update3DDimensions()
        {
            if (LightningChart?.View3D == null)
            {
                return;
            }

            LightningChart.BeginUpdate();
            try
            {
                ApplyDimensionSettingsCore(LightningChart);
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        /// <summary>
        /// 在尺寸输入框按回车时立即应用缩放参数。
        /// </summary>
        private void OnDimensionEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            Update3DDimensions();
            TraversalRequest request = new(FocusNavigationDirection.Next);
            (sender as UIElement)?.MoveFocus(request);
        }

        /// <summary>
        /// 尺寸输入框失焦时，同步应用最新的缩放参数。
        /// </summary>
        private void OnDimensionEditorLostFocus(object sender, RoutedEventArgs e)
        {
            Update3DDimensions();
        }

        private void OnResetViewClicked(object sender, RoutedEventArgs e)
        {
            if (!_cameraStateInitialized || LightningChart?.View3D?.Camera == null)
            {
                return;
            }

            LightningChart.BeginUpdate();
            try
            {
                LightningChart.View3D.Camera.ViewDistance = _initialCameraState.ViewDistance;
                LightningChart.View3D.Camera.RotationX = _initialCameraState.RotationX;
                LightningChart.View3D.Camera.RotationY = _initialCameraState.RotationY;
                LightningChart.View3D.Camera.RotationZ = _initialCameraState.RotationZ;
                LightningChart.View3D.Camera.Projection = _initialCameraState.Projection;
                slider_ViewDistance.Value = _initialCameraState.ViewDistance;
                tb_ViewDistance.Text = ((int)_initialCameraState.ViewDistance).ToString();
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        public void SetViewDistance(double distance)
        {
            if (LightningChart?.View3D?.Camera == null)
            {
                return;
            }

            distance = Math.Max(slider_ViewDistance.Minimum, Math.Min(slider_ViewDistance.Maximum, distance));
            slider_ViewDistance.Value = distance;
            LightningChart.View3D.Camera.ViewDistance = distance;
            tb_ViewDistance.Text = ((int)distance).ToString();
        }

        public CameraState GetCurrentCameraState()
        {
            if (LightningChart?.View3D?.Camera == null)
            {
                return _initialCameraState;
            }

            return new CameraState
            {
                ViewDistance = LightningChart.View3D.Camera.ViewDistance,
                RotationX = LightningChart.View3D.Camera.RotationX,
                RotationY = LightningChart.View3D.Camera.RotationY,
                RotationZ = LightningChart.View3D.Camera.RotationZ,
                Projection = LightningChart.View3D.Camera.Projection
            };
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsVisible || LightningChart?.View3D?.Camera == null)
            {
                return;
            }

            const double stepSize = 10.0;
            switch (e.Key)
            {
                case Key.Add:
                    ZoomIn(stepSize);
                    e.Handled = true;
                    break;

                case Key.Subtract:
                    ZoomOut(stepSize);
                    e.Handled = true;
                    break;

                case Key.Home:
                    OnResetViewClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        public void ZoomIn(double step = 10.0)
        {
            SetViewDistance(slider_ViewDistance.Value - step);
        }

        public void ZoomOut(double step = 10.0)
        {
            SetViewDistance(slider_ViewDistance.Value + step);
        }

        public void FitToData()
        {
            if (_surfaceGround == null || LightningChart?.View3D?.Camera == null)
            {
                return;
            }

            double planeMaxSize = Math.Max(
                LightningChart.View3D.Dimensions.Width,
                LightningChart.View3D.Dimensions.Depth);
            double suggestedDistance = planeMaxSize * 1.2 + Math.Max(40.0, LightningChart.View3D.Dimensions.Height);
            SetViewDistance(suggestedDistance);
        }

        /// <summary>
        /// 根据数据宽高比重新计算 3D 平面的基准宽度和长度。
        /// </summary>
        private void AdjustChartDimensionsToData(LightningChart chart, int dataRows, int dataCols)
        {
            if (dataRows <= 0 || dataCols <= 0)
            {
                return;
            }

            double widthDepthRatio = (double)dataRows / dataCols;
            const double baseSize = 200.0;

            double width;
            double depth;
            if (widthDepthRatio >= 1.0)
            {
                width = baseSize;
                depth = baseSize / widthDepthRatio;
            }
            else
            {
                width = baseSize * widthDepthRatio;
                depth = baseSize;
            }

            _baseDimensionWidth = width;
            _baseDimensionDepth = depth;
            ApplyDimensionSettingsCore(chart);
            SetAxisRangesToData(chart, dataRows, dataCols);
        }

        /// <summary>
        /// 将基准尺寸与界面中的缩放参数叠加后应用到 3D 场景。
        /// </summary>
        private void ApplyDimensionSettingsCore(LightningChart chart)
        {
            double widthScale = Math.Max(0.1, nud_3DWidthScale.Value);
            double lengthScale = Math.Max(0.1, nud_3DLengthScale.Value);

            chart.View3D.Dimensions.Width = _baseDimensionWidth * widthScale;
            chart.View3D.Dimensions.Height = nud_3DHeight.Value;
            chart.View3D.Dimensions.Depth = _baseDimensionDepth * lengthScale;
        }

        private void SetAxisRangesToData(LightningChart chart, int dataRows, int dataCols)
        {
            chart.View3D.XAxisPrimary3D.SetRange(0, dataRows - 1);
            chart.View3D.ZAxisPrimary3D.SetRange(0, dataCols - 1);
        }

        private void OnFitViewClicked(object sender, RoutedEventArgs e)
        {
            FitToData();
        }

        private void OnDataCursorToggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                ToggleDataCursor(toggleButton.IsChecked ?? false);
            }
        }

        private void OnCustomColorRangeChecked(object sender, RoutedEventArgs e)
        {
            (float minValue, float maxValue) = NormalizeRangeFromMin();
            _chartModel.SetCustomColorRange(minValue, maxValue);
            RefreshPalette();
        }

        private void OnCustomColorRangeUnchecked(object sender, RoutedEventArgs e)
        {
            _chartModel.DisableCustomColorRange();
            RefreshPalette();
        }

        private void OnMinValueChanged(object sender, EventArgs e)
        {
            if (!_chartModel.UseCustomColorRange)
            {
                return;
            }

            (float minValue, float maxValue) = NormalizeRangeFromMin();
            _chartModel.SetCustomColorRange(minValue, maxValue);
            RefreshPalette();
        }

        private void OnMaxValueChanged(object sender, EventArgs e)
        {
            if (!_chartModel.UseCustomColorRange)
            {
                return;
            }

            (float minValue, float maxValue) = NormalizeRangeFromMax();
            _chartModel.SetCustomColorRange(minValue, maxValue);
            RefreshPalette();
        }

        private (float MinValue, float MaxValue) NormalizeRangeFromMin()
        {
            float minValue = (float)nud_MinValue.Value;
            float maxValue = (float)nud_MaxValue.Value;

            if (minValue >= maxValue)
            {
                maxValue = minValue + 0.1f;
                nud_MaxValue.Value = maxValue;
            }

            return (minValue, maxValue);
        }

        private (float MinValue, float MaxValue) NormalizeRangeFromMax()
        {
            float minValue = (float)nud_MinValue.Value;
            float maxValue = (float)nud_MaxValue.Value;

            if (minValue >= maxValue)
            {
                minValue = maxValue - 0.1f;
                nud_MinValue.Value = minValue;
            }

            return (minValue, maxValue);
        }

        /// <summary>
        /// 将当前数据范围同步到页面按钮提示中，便于一键回填色域。
        /// </summary>
        private void UpdateDataRangeDisplay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!_chartModel.TryGetDataRange(out float minValue, out float maxValue))
                {
                    btn_ApplyDataRange.ToolTip = "无数据范围可应用";
                    btn_ApplyDataRange.IsEnabled = false;
                    return;
                }

                btn_ApplyDataRange.ToolTip = $"应用当前数据范围: [{minValue:F3}, {maxValue:F3}] mm";
                btn_ApplyDataRange.IsEnabled = true;
            });
        }

        public void AutoSetColorRangeToDataRange()
        {
            if (!_chartModel.TryGetDataRange(out float minValue, out float maxValue))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                nud_MinValue.Value = minValue;
                nud_MaxValue.Value = maxValue;

                if (_chartModel.UseCustomColorRange)
                {
                    _chartModel.SetCustomColorRange(minValue, maxValue);
                    RefreshPalette();
                }
            });
        }

        private void OnApplyDataRangeClick(object sender, RoutedEventArgs e)
        {
            if (!_chartModel.TryGetDataRange(out float minValue, out float maxValue))
            {
                return;
            }

            nud_MinValue.Value = minValue;
            nud_MaxValue.Value = maxValue;

            if (_chartModel.UseCustomColorRange)
            {
                _chartModel.SetCustomColorRange(minValue, maxValue);
                RefreshPalette();
                return;
            }

            chk_CustomColorRange.IsChecked = true;
        }

        private static void SelectComboItemByTag(ComboBox comboBox, string tag)
        {
            foreach (object item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && string.Equals(comboBoxItem.Tag as string, tag, StringComparison.Ordinal))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        public void Dispose()
        {
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RequestChartLayoutRefresh();
        }
    }
}

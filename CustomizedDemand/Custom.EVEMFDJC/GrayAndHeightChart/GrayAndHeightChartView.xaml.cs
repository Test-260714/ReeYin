using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Series3D;
using HandyControl.Controls;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Custom.EVEMFDJC.Models.EVEMFDJC0_Algorithm;
using Application = System.Windows.Application;
using Border = System.Windows.Controls.Border;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
using DialogResult = System.Windows.Forms.DialogResult;
using GroupBox = System.Windows.Controls.GroupBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using ProjectionType = Arction.Wpf.Charting.ProjectionType;

namespace Custom.EVEMFDJC.GrayAndHeightChart
{
    public partial class GrayAndHeightChartView
    {
        //public MFDJC0_GrayAndHeightChartViewModel ViewModel { get; set; }

        private SurfaceGridSeries3D? _surfaceGround;
        private PointLineSeries3D? _pointCloudSeries;

        private WriteableBitmap _writeableBitmap;

        private FeatureConfig _featureConfig;
        private string _configFilePath;
        private Dictionary<string, FrameworkElement> _dynamicControls = new Dictionary<string, FrameworkElement>();


        // 性能优化配置 - 更智能的采样策略
        private const int ULTRA_FAST_SAMPLE_SIZE = 128;   // 超快速预览
        private const int FAST_SAMPLE_SIZE = 256;         // 快速模式
        private const int BALANCED_SAMPLE_SIZE = 512;     // 平衡模式  
        private const int HIGH_QUALITY_SAMPLE_SIZE = 1024; // 高质量模式
        private const int ULTRA_HIGH_QUALITY_SAMPLE_SIZE = 2048; // 超高质量模式
        private const int ULTIMA_HIGH_QUALITY_SAMPLE_SIZE = 5480; // 超高质量模式

        // 性能/质量设置
        public enum QualityMode
        {
            UltraFast,    // 128x128 - 极速预览
            Fast,         // 256x256 - 快速
            Balanced,     // 512x512 - 平衡（推荐）
            HighQuality,  // 1024x1024 - 高质量
            UltraHighQuality, // 2048x2048 - 超高质量
            UltimaHighQuality// 20480x20480 - 究极高质量
        }

        // 调色板类型
        public enum ColorPaletteType
        {
            Classic,  // 经典：蓝->青->绿->黄->红
            Heatmap   // 热图：黑->紫->蓝->绿->黄->红->白
        }

        public enum RenderMode
        {
            Surface,
            PointCloud
        }

        private QualityMode _currentQualityMode = QualityMode.HighQuality; // 默认超高模式
        private ColorPaletteType _currentPaletteType = ColorPaletteType.Classic; // 默认经典调色板
        private RenderMode _currentRenderMode = RenderMode.Surface;
        private bool _isProcessing = false; // 防止重复处理
        private float[][]? _lastRenderedHeightData;
        private int _lastDataRows;
        private int _lastDataCols;

        // 自定义色域范围相关
        private bool _useCustomColorRange = false;
        private float _customMinValue = -1.0f;
        private float _customMaxValue = 1.0f;
        private float _dataMinValue = float.MaxValue;
        private float _dataMaxValue = float.MinValue;

        // 镜头控制相关
        public struct CameraState
        {
            public double ViewDistance;
            public double RotationX;
            public double RotationY;
            public double RotationZ;
            public ProjectionType Projection;
        }

        private CameraState _initialCameraState; // 初始镜头状态
        private bool _cameraStateInitialized = false; // 是否已保存初始状态
        private double _baseDimensionWidth = 300.0;
        private double _baseDimensionDepth = 200.0;

        // 圆形绘制相关
        private bool _isDrawingCircle = false; // 是否正在绘制圆形
        private Point _circleCenter; // 圆心坐标
        private double _circleRadius; // 圆半径
        private Ellipse _currentCircle; // 当前绘制的圆形

        // 数据游标相关
        private bool _enableDataCursor = false; // 是否启用数据游标（默认关闭以提高性能）
        private int _maxDataPointsForCursor = 512 * 512; // 数据游标启用的最大数据点数阈值

        /// <summary>
        /// 复制非托管内存数据。
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void MoveMemory(IntPtr dest, IntPtr src, uint count);
        private IDataProcessorService _dataProcessorService;

        public GrayAndHeightChartView()
        {
            InitializeComponent();
            SetUpHeightChart();

            // 绑定事件
            cb_QualityMode.SelectionChanged += OnQualityModeChanged;
            cb_ColorPalette.SelectionChanged += OnColorPaletteChanged;
            cb_RenderMode.SelectionChanged += OnRenderModeChanged;

            // 绑定镜头控制事件
            slider_ViewDistance.ValueChanged += OnViewDistanceChanged;
            btn_ResetView.Click += OnResetViewClicked;
            btn_FitView.Click += OnFitViewClicked;

            // 绑定自定义色域范围事件
            chk_CustomColorRange.Checked += OnCustomColorRangeChecked;
            chk_CustomColorRange.Unchecked += OnCustomColorRangeUnchecked;

            // 绑定数据游标控制事件 - 使用FindName方法
            var dataCursorButton = this.FindName("btn_ToggleDataCursor") as ToggleButton;
            if (dataCursorButton != null)
            {
                dataCursorButton.Checked += OnDataCursorToggled;
                dataCursorButton.Unchecked += OnDataCursorToggled;
            }

            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe(Test, ThreadOption.BackgroundThread);
            // 绑定图像操作事件
            btn_SaveImage.Click += btn_SaveImage_Click;
            btn_OriginalScale.Click += btn_OriginalScale_Click;
            //btn_Ellipse.Click += btn_Ellipse_OnClick;
            btn_ClearTools.Click += btn_ClearTools_OnClick;

            // 绑定绘制工具事件
            drawCanvas.DrawToolEndedEvent += OnDrawToolEnded;

            // 启用键盘焦点和快捷键支持
            Focusable = true;
            KeyDown += OnKeyDown;

            // 设置配置文件路径
            _configFilePath = Path.Combine(PrismProvider.AppBasePath, "SealingNailsSDK", "ini", "FeatureConfig.json");

            // 加载配置文件
            LoadConfigurationAndGenerateUI();

            // 初始化数据范围显示
            UpdateDataRangeDisplay();

            // 初始化数据游标按钮状态
            UpdateDataCursorButtonStatus();
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// 加载配置并生成 UI。
        /// </summary>
        private void LoadConfigurationAndGenerateUI()
        {
            try
            {
                LoadConfigurationFromFile();
                GenerateDynamicUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置并生成UI时出错: {ex.Message}");
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// 根据配置动态生成参数 UI。
        /// </summary>
        private void GenerateDynamicUI()
        {
            if (_featureConfig?.DefectList == null) return;

            // 清空现有控件
            DynamicConfigContainer.Children.Clear();
            _dynamicControls.Clear();

            foreach (var defect in _featureConfig.DefectList)
            {
                var groupBox = CreateDefectGroupBox(defect);
                DynamicConfigContainer.Children.Add(groupBox);
            }

            System.Diagnostics.Debug.WriteLine($"动态生成了 {_featureConfig.DefectList.Count} 个缺陷配置组");
        }

        /// <summary>
        /// 创建缺陷配置GroupBox
        /// </summary>
        private GroupBox CreateDefectGroupBox(Defect defect)
        {
            var groupBox = new GroupBox
            {
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 12),
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            // 设置Header样式
            var headerBorder = new Border
            {
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4, 0, 0)
            };

            var headerText = new TextBlock
            {
                Text = defect.Name,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            headerBorder.Child = headerText;
            groupBox.Header = headerBorder;

            // 创建参数控件容器
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // 为每个参数创建控件
            foreach (var param in defect.AlgParam)
            {
                var parameterControl = CreateParameterControl(defect, param);
                if (parameterControl != null)
                {
                    parameterControl.Margin = new Thickness(0, 0, 0, 8);
                    stackPanel.Children.Add(parameterControl);
                }
            }

            groupBox.Content = stackPanel;
            return groupBox;
        }

        /// <summary>
        /// 根据参数类型创建对应的控件
        /// </summary>
        private FrameworkElement CreateParameterControl(Defect defect, Parameter param)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (!string.IsNullOrEmpty(param.Unit))
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            }

            // 创建标签
            var label = new TextBlock
            {
                Text = $"{param.Describe}:",
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Normal,
                Width = 300,
                FontSize = 11
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // 根据UI类型创建控件
            FrameworkElement inputControl = null;
            string controlKey = $"{defect.Name}_{param.Name}";

            switch (param.UiType?.ToLower() ?? "numeric")
            {
                case "numeric":
                    inputControl = CreateNumericUpDown(param);
                    break;
                case "combo":
                    inputControl = CreateComboBox(param);
                    break;
                case "checkbox":
                    inputControl = CreateCheckBox(param);
                    break;
                default:
                    inputControl = CreateNumericUpDown(param);
                    break;
            }

            if (inputControl != null)
            {
                Grid.SetColumn(inputControl, 1);
                grid.Children.Add(inputControl);
                _dynamicControls[controlKey] = inputControl;
            }

            // 添加单位标签
            if (!string.IsNullOrEmpty(param.Unit))
            {
                var unitLabel = new TextBlock
                {
                    Text = param.Unit,
                    Foreground = new SolidColorBrush(Colors.LightGray),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 10
                };
                Grid.SetColumn(unitLabel, 2);
                grid.Children.Add(unitLabel);
            }

            return grid;
        }


        /// <summary>
        /// 创建数值输入控件
        /// </summary>
        private FrameworkElement CreateNumericUpDown(Parameter param)
        {
            var numericUpDown = new HandyControl.Controls.NumericUpDown
            {
                Value = Convert.ToDouble(param.Value ?? 0),
                Minimum = param.MinValue ?? 0,
                Maximum = param.MaxValue ?? 10000,
                DecimalPlaces = param.DecimalPlaces ?? 4,
                Increment = param.Increment ?? 0.1,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Width = 120
            };

            return numericUpDown;
        }

        /// <summary>
        /// 创建下拉框控件
        /// </summary>
        private FrameworkElement CreateComboBox(Parameter param)
        {
            var comboBox = new HandyControl.Controls.ComboBox()
            {
                Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x00, 0x00, 0x00)),
                Foreground = new SolidColorBrush(Colors.White)
            };

            // 添加选项
            if (param.Options != null && param.Options.Count > 0)
            {
                foreach (var option in param.Options)
                {
                    comboBox.Items.Add(new ComboBoxItem { Content = option });
                }

                // 设置选中项
                if (param.Value != null)
                {
                    int selectedIndex = Convert.ToInt32(param.Value);
                    if (selectedIndex >= 0 && selectedIndex < comboBox.Items.Count)
                    {
                        comboBox.SelectedIndex = selectedIndex;
                    }
                }
            }

            return comboBox;
        }

        /// <summary>
        /// 创建复选框控件
        /// </summary>
        private FrameworkElement CreateCheckBox(Parameter param)
        {
            var checkBox = new CheckBox
            {
                Content = "启用",
                IsChecked = Convert.ToBoolean(param.Value ?? false),
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };

            return checkBox;
        }

        /// <summary>
        /// 从动态控件收集参数值
        /// </summary>
        private void UpdateConfigurationFromDynamicUI()
        {
            if (_featureConfig?.DefectList == null) return;

            try
            {
                foreach (var defect in _featureConfig.DefectList)
                {
                    foreach (var param in defect.AlgParam)
                    {
                        string controlKey = $"{defect.Name}_{param.Name}";
                        if (_dynamicControls.TryGetValue(controlKey, out var control))
                        {
                            // 根据控件类型获取值
                            object newValue = GetValueFromControl(control, param);

                            // 更新参数值
                            param.Value = newValue;

                            System.Diagnostics.Debug.WriteLine($"更新参数: {defect.Name}.{param.Name} = {newValue}");
                        }
                    }
                }

                //ReeYin.XRay.Algorithm.MFDJC0_Algorithm.UpdateParseFeatureConfig(_featureConfig);

                System.Diagnostics.Debug.WriteLine("配置已从动态UI更新完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从动态UI更新配置时出错: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// 初始化高度图的 3D 图表配置。
        /// </summary>
        private void SetUpHeightChart()
        {
            LightningChart.BeginUpdate();

            LightningChart.Title.Text = "";
            LightningChart.ActiveView = ActiveView.View3D;

            // 暂时设置默认尺寸，实际尺寸会在数据加载时动态调整
            LightningChart.View3D.Dimensions.Width = 300.0;
            nud_3DWidthScale.Value = 1.0;
            LightningChart.View3D.Dimensions.Height = 30.0;
            nud_3DHeight.Value = 30.0;
                //ConfigCenter.FrameConfig.GetPara("MFDJC0_HeightChartSetting_DimensionsHeight", 30.0);
            LightningChart.View3D.Dimensions.Depth = 200.0;
            nud_3DLengthScale.Value = 1.0;
            LightningChart.View3D.LegendBox.ShowCheckboxes = false;
            LightningChart.View3D.LegendBox.Visible = true;
            LightningChart.View3D.Camera.MinimumViewDistance = 10;
            LightningChart.View3D.Camera.ViewDistance = 180.0;
            LightningChart.View3D.Camera.Projection = ProjectionType.Orthographic;
            LightningChart.View3D.Camera.RotationX = 90;
            LightningChart.View3D.Camera.RotationY = 0;
            LightningChart.View3D.Camera.RotationZ = 90;
            LightningChart.View3D.ZoomPanOptions.DevicePrimaryButtonDoubleClickAction = DoubleClickAction3D.Off;
            LightningChart.View3D.ZAxisPrimary3D.Reversed = false;
            LightningChart.View3D.YAxisPrimary3D.Reversed = false;
            LightningChart.View3D.XAxisPrimary3D.Reversed = false;

            // 数据游标设置 - 默认关闭以提高性能
            ConfigureDataCursor();
            foreach (var wall in LightningChart.View3D.GetWalls())
            {
                wall.Visible = false;
            }

            foreach (var axis in LightningChart.View3D.GetAxes())
            {
                axis.Visible = false;
            }

            // 性能优化配置
            OptimizeLightningChartForLargeData();

            // 保存初始镜头状态
            SaveInitialCameraState();

            LightningChart.EndUpdate();
        }

        /// <summary>
        /// 保存初始镜头状态
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

            // 同步滑块值
            slider_ViewDistance.Value = _initialCameraState.ViewDistance;
            tb_ViewDistance.Text = ((int)_initialCameraState.ViewDistance).ToString();
        }

        /// <summary>
        /// 配置数据游标 - 性能优化版本
        /// </summary>
        private void ConfigureDataCursor()
        {
            // 默认关闭数据游标以提高性能
            LightningChart.View3D.DataCursor.Visible = _enableDataCursor;
            LightningChart.View3D.DataCursor.ShowLabels = _enableDataCursor;

            if (_enableDataCursor)
            {
                // 如果启用数据游标，进行性能优化配置
                LightningChart.View3D.DataCursor.LineStyle.Width = 1;
                LightningChart.View3D.DataCursor.LineStyle.Color = Colors.Yellow;

                System.Diagnostics.Debug.WriteLine("数据游标已启用（可能影响性能）");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("数据游标已禁用（性能优化）");
            }
        }

        /// <summary>
        /// 智能启用数据游标 - 根据数据量自动决定是否启用
        /// </summary>
        /// <param name="dataPoints">数据点数量</param>
        private void SmartEnableDataCursor(int dataPoints)
        {
            bool shouldEnable = dataPoints <= _maxDataPointsForCursor;

            if (shouldEnable != _enableDataCursor)
            {
                _enableDataCursor = shouldEnable;
                ConfigureDataCursor();

                // 同步更新界面按钮状态
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateDataCursorButtonStatus();
                });

                if (shouldEnable)
                {
                    System.Diagnostics.Debug.WriteLine($"数据量较小({dataPoints:N0}点)，自动启用数据游标");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"数据量较大({dataPoints:N0}点)，自动禁用数据游标以提高性能");
                }
            }
        }

        /// <summary>
        /// 手动切换数据游标状态
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void ToggleDataCursor(bool enable)
        {
            _enableDataCursor = enable;
            ConfigureDataCursor();

            // 同步更新界面按钮状态（如果不是从按钮触发的话）
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateDataCursorButtonStatus();
            });
        }

        /// <summary>
        /// 获取当前数据游标状态
        /// </summary>
        /// <returns>是否启用数据游标</returns>
        public bool IsDataCursorEnabled()
        {
            return _enableDataCursor;
        }

        /// <summary>
        /// 设置数据游标启用的阈值
        /// </summary>
        /// <param name="maxDataPoints">最大数据点数</param>
        public void SetDataCursorThreshold(int maxDataPoints)
        {
            _maxDataPointsForCursor = maxDataPoints;
            System.Diagnostics.Debug.WriteLine($"数据游标启用阈值已设置为: {maxDataPoints:N0} 个数据点");
        }

        /// <summary>
        /// 更新数据游标按钮状态
        /// </summary>
        private void UpdateDataCursorButtonStatus()
        {
            // 使用FindName方法查找控件
            var dataCursorButton = this.FindName("btn_ToggleDataCursor") as ToggleButton;
            if (dataCursorButton != null)
            {
                // 更新按钮状态
                dataCursorButton.IsChecked = _enableDataCursor;

                // 更新提示信息
                string status = _enableDataCursor ? "已启用" : "已禁用";
                string tooltip = $"数据光标{status}\n{(_enableDataCursor ? "可查看鼠标位置的数据值" : "点击启用数据光标功能")}\n大数据集时会自动优化性能";
                dataCursorButton.ToolTip = tooltip;
            }
        }

        /// <summary>
        /// 为大数据集优化 LightningChart 性能
        /// </summary>
        private void OptimizeLightningChartForLargeData()
        {
            LightningChart.ChartRenderOptions.AntiAliasLevel = 0; // 禁用抗锯齿

            // 优化鼠标交互性能
            LightningChart.View3D.ZoomPanOptions.WheelZoomFactor = 1.2;

            System.Diagnostics.Debug.WriteLine("LightningChart 性能优化配置已应用");
        }

        /// <summary>
        /// 设置质量模式
        /// </summary>
        /// <param name="mode">质量模式</param>
        public void SetQualityMode(QualityMode mode)
        {
            _currentQualityMode = mode;
        }

        /// <summary>
        /// 获取当前质量模式对应的采样大小
        /// </summary>
        private int GetSampleSizeForQuality(QualityMode mode)
        {
            return mode switch
            {
                QualityMode.UltraFast => ULTRA_FAST_SAMPLE_SIZE,
                QualityMode.Fast => FAST_SAMPLE_SIZE,
                QualityMode.Balanced => BALANCED_SAMPLE_SIZE,
                QualityMode.HighQuality => HIGH_QUALITY_SAMPLE_SIZE,
                QualityMode.UltraHighQuality => ULTRA_HIGH_QUALITY_SAMPLE_SIZE,
                QualityMode.UltimaHighQuality => ULTIMA_HIGH_QUALITY_SAMPLE_SIZE,
                _ => BALANCED_SAMPLE_SIZE
            };
        }

        #region 数据处理

        /// <summary>
        /// 智能采样 - 根据数据特征自适应选择采样策略
        /// </summary>
        /// <param name="originalData">原始数据 (float[][])</param>
        /// <param name="targetSize">目标大小</param>
        /// <param name="useAdvancedSampling">是否使用高级采样算法</param>
        /// <returns>采样后的数据</returns>
        private float[][] SmartSampleFloatArray(float[][] originalData, int targetSize, bool useAdvancedSampling = true)
        {
            if (originalData == null || originalData.Length == 0 || originalData[0].Length == 0)
                return originalData;

            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            // 如果原数据已经很小，直接返回
            if (originalRows <= targetSize && originalCols <= targetSize)
                return originalData;

            if (useAdvancedSampling)
            {
                // 使用双线性插值采样，保持更好的细节
                return BilinearSample(originalData, targetSize);
            }
            else
            {
                // 使用快速最近邻采样
                return NearestNeighborSample(originalData, targetSize);
            }
        }

        /// <summary>
        /// 双线性插值采样 - 更好的质量(保持图像宽高比，最长边为 targetSize)
        /// </summary>
        private float[][] BilinearSample(float[][] originalData, int targetSize)
        {
            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            // 计算缩放因子：以长边为基准
            double scale = (originalRows >= originalCols)
                ? (double)targetSize / originalRows
                : (double)targetSize / originalCols;

            int newRows = (int)Math.Round(originalRows * scale);
            int newCols = (int)Math.Round(originalCols * scale);

            float[][] resizedData = new float[newRows][];
            for (int i = 0; i < newRows; i++)
                resizedData[i] = new float[newCols];

            double rowScale = (double)(originalRows - 1) / (newRows - 1);
            double colScale = (double)(originalCols - 1) / (newCols - 1);

            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    double srcRow = i * rowScale;
                    double srcCol = j * colScale;

                    int row1 = Math.Min((int)srcRow, originalRows - 2);
                    int col1 = Math.Min((int)srcCol, originalCols - 2);
                    int row2 = row1 + 1;
                    int col2 = col1 + 1;

                    double rowWeight = srcRow - row1;
                    double colWeight = srcCol - col1;

                    float val11 = originalData[row1][col1];
                    float val12 = originalData[row1][col2];
                    float val21 = originalData[row2][col1];
                    float val22 = originalData[row2][col2];

                    float interpolatedValue = (float)(
                        val11 * (1 - rowWeight) * (1 - colWeight) +
                        val12 * (1 - rowWeight) * colWeight +
                        val21 * rowWeight * (1 - colWeight) +
                        val22 * rowWeight * colWeight
                    );

                    resizedData[i][j] = interpolatedValue;
                }
            }

            return resizedData;
        }


        /// <summary>
        /// 最近邻采样 - 更快的速度(保持宽高比，最长边为 targetSize)
        /// </summary>
        private float[][] NearestNeighborSample(float[][] originalData, int targetSize)
        {
            int originalRows = originalData.Length;
            int originalCols = originalData[0].Length;

            // 计算缩放因子（以长边为基准）
            double scale = (originalRows >= originalCols)
                ? (double)targetSize / originalRows
                : (double)targetSize / originalCols;

            int newRows = (int)Math.Round(originalRows * scale);
            int newCols = (int)Math.Round(originalCols * scale);

            var resizedData = new float[newRows][];
            for (int i = 0; i < newRows; i++)
                resizedData[i] = new float[newCols];

            double rowStep = (double)originalRows / newRows;
            double colStep = (double)originalCols / newCols;

            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    int srcRow = Math.Min((int)Math.Round(i * rowStep), originalRows - 1);
                    int srcCol = Math.Min((int)Math.Round(j * colStep), originalCols - 1);
                    resizedData[i][j] = originalData[srcRow][srcCol];
                }
            }

            return resizedData;
        }


        /// <summary>
        /// 智能采样 - 将大数据集缩减到合理大小
        /// </summary>
        /// <param name="originalData">原始数据 (float[][])</param>
        /// <param name="targetSize">目标大小</param>
        /// <returns>采样后的数据</returns>
        private float[][] AggressiveSampleFloatArray(float[][] originalData, int targetSize = BALANCED_SAMPLE_SIZE)
        {
            return SmartSampleFloatArray(originalData, targetSize, true);
        }

        /// <summary>
        /// 对二维数组执行快速降采样。
        /// </summary>
        private double[,] AggressiveSample(double[,] originalData)
        {
            // 建议采样到 512x512 或 256x256
            int targetSize = 512; // 或者更小，比如256

            int originalRows = 4148;
            int originalCols = 4096;

            var sampledData = new double[targetSize, targetSize];

            // 计算采样步长
            double rowStep = (double)originalRows / targetSize;
            double colStep = (double)originalCols / targetSize;

            for (int i = 0; i < targetSize; i++)
            {
                for (int j = 0; j < targetSize; j++)
                {
                    // 使用最近邻采样（最快）
                    int srcRow = Math.Min((int)(i * rowStep), originalRows - 1);
                    int srcCol = Math.Min((int)(j * colStep), originalCols - 1);
                    sampledData[i, j] = originalData[srcRow, srcCol];
                }
            }

            return sampledData;
        }

        /// <summary>
        /// 刷新 LightningChart 的基础显示参数。
        /// </summary>
        private void RefreshLightningChart()
        {
            LightningChart.View3D.Dimensions.Height = 10.0; /*ConfigCenter.FrameConfig.GetPara<double>("MFDJC0_HeightChartSetting_DimensionsHeight", 10.0);*/
        }

        #endregion


        /// <summary>
        /// 测试处理数据的更新流程。
        /// </summary>
        public void Test(ProcessedData pd)
        {
            Logs.LogInfo($"进入UpdateMeasureDataAsync(pd)");
            var taslk =  UpdateMeasureDataAsync(pd);
            Logs.LogInfo($"结束UpdateMeasureDataAsync(pd)");
            Logs.LogInfo($"开始等待taslk.Wait()");
            taslk.Wait();
            Logs.LogInfo($"taslk.Wait()执行结束");
            Logs.LogInfo($"开始GC.Collect()");
            GC.Collect();
            Logs.LogInfo($"结束GC.Collect()");
            Logs.LogInfo($"开始GC.WaitForPendingFinalizers()");
            GC.WaitForPendingFinalizers(); // 等待所有终结器执行完毕
            Logs.LogInfo($"结束GC.WaitForPendingFinalizers()");
        }


        /// <summary>
        /// 异步更新测量数据 - 渐进式加载版本
        /// </summary>
        public async Task UpdateMeasureDataAsync(ProcessedData pd, CancellationToken token = default)
        {
            Logs.LogInfo($"进入_isProcessing{_isProcessing}");
            // 防止重复处理
            if (_isProcessing)
                return;
            Logs.LogInfo($"结束_isProcessing{_isProcessing}");
            _isProcessing = true;
            Mat HeightImage = null;
            Mat GrayImage = null;
            try
            {
                Logs.LogInfo($"进入pd.GetMemoryPara");
                var result = pd.GetMemoryPara("MFDJC0_MeasureResult", new MFDJC0_MeasureResult());
                if(result == null)
                    Logs.LogInfo($"获取到的{result}结果为null");
                if(result?.HeightImage == null)
                    Logs.LogInfo($"获取到的HeightImage结果为null");
                if(result?.GrayImage == null)
                    Logs.LogInfo($"获取到的GrayImage结果为null");
                Logs.LogInfo($"结束pd.GetMemoryPara");
                Logs.LogInfo($"进入异步处理高度数据");
                HeightImage = result.HeightImage.Clone();
                GrayImage = result.GrayImage.Clone();
                // 异步处理高度数据
                var heightDataTask = Task.Run(() => MatToFloat2DArray(HeightImage), token);
                Logs.LogInfo($"结束异步处理高度数据");
                Logs.LogInfo($"进入异步处理灰度图像");
                // 异步处理灰度图像
                var grayImageTask = Task.Run(() => ConvertMatToBitmapFrame(GrayImage), token);
                Logs.LogInfo($"结束异步处理灰度图像");
                Logs.LogInfo($"进入等待高度数据处理完成");
                // 等待高度数据处理完成
                var heightData = await heightDataTask;
                Logs.LogInfo($"结束等待高度数据处理完成");
                Logs.LogInfo($"进入应用深度筛选，将范围外的值设置为 NaN");
                // 应用深度筛选，将范围外的值设置为 NaN
                var filteredHeightData = await Task.Run(() =>
                    FilterDepthData(heightData, (float)result.MinDepth, (float)result.MaxDepth), token);
                Logs.LogInfo($"结束应用深度筛选，将范围外的值设置为 NaN");
                Logs.LogInfo($"进入渐进式加载策略");
                // 渐进式加载策略
                await ProgressiveLoadSurfaceData(filteredHeightData, token);
                Logs.LogInfo($"结束渐进式加载策略");
                Logs.LogInfo($"进入等待灰度图像处理完成");
                // 等待灰度图像处理完成
                await grayImageTask;
                Logs.LogInfo($"结束等待灰度图像处理完成");
                Logs.LogInfo($"进入更新瑕疵信息显示");
                // 更新瑕疵信息显示
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateDefectInfoDisplay(result);
                }, DispatcherPriority.Normal, token);
                Logs.LogInfo($"结束更新瑕疵信息显示");
            }
            catch (OperationCanceledException)
            {
                // 处理取消操作
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Logs.LogError($"UpdateMeasureDataAsync error: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                HeightImage?.Dispose();
                GrayImage?.Dispose();
            }
        }

        /// <summary>
        /// 根据最小最大深度值筛选有效的深度数据
        /// </summary>
        /// <param name="heightData">原始高度数据</param>
        /// <param name="minDepth">最小有效深度</param>
        /// <param name="maxDepth">最大有效深度</param>
        /// <returns>筛选后的高度数据</returns>
        private float[][] FilterDepthData(float[][] heightData, float minDepth, float maxDepth)
        {
            if (heightData == null || heightData.Length == 0)
                return heightData;

            int rows = heightData.Length;
            int cols = heightData[0].Length;
            int totalPoints = rows * cols;
            int validPoints = 0;
            int filteredPoints = 0;
            int originalInvalidPoints = 0;

            var filteredData = new float[rows][];

            // 统计原始数据的有效值范围
            float originalMin = float.MaxValue;
            float originalMax = float.MinValue;

            for (int i = 0; i < rows; i++)
            {
                filteredData[i] = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    float value = heightData[i][j];

                    // 统计原始数据
                    if (!float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        originalMin = Math.Min(originalMin, value);
                        originalMax = Math.Max(originalMax, value);
                    }
                    else
                    {
                        originalInvalidPoints++;
                    }

                    // 检查值是否在有效范围内
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        filteredData[i][j] = float.NaN;
                        // 已在上面统计过了，不重复计算
                    }
                    else if (value < minDepth || value > maxDepth)
                    {
                        filteredData[i][j] = float.NaN;
                        filteredPoints++;
                    }
                    else
                    {
                        filteredData[i][j] = value;
                        validPoints++;
                    }
                }
            }

            // 输出筛选统计信息
            System.Diagnostics.Debug.WriteLine($"深度数据筛选结果:");
            System.Diagnostics.Debug.WriteLine($"  总数据点: {totalPoints:N0}");
            System.Diagnostics.Debug.WriteLine($"  原始无效点: {originalInvalidPoints:N0} ({100.0 * originalInvalidPoints / totalPoints:F1}%)");
            System.Diagnostics.Debug.WriteLine($"  筛选掉的点: {filteredPoints:N0} ({100.0 * filteredPoints / totalPoints:F1}%)");
            System.Diagnostics.Debug.WriteLine($"  最终有效点: {validPoints:N0} ({100.0 * validPoints / totalPoints:F1}%)");
            System.Diagnostics.Debug.WriteLine($"  原始数据范围: [{originalMin:F3}, {originalMax:F3}]");
            System.Diagnostics.Debug.WriteLine($"  筛选范围: [{minDepth:F3}, {maxDepth:F3}]");

            // 更新UI显示
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateDepthRangeDisplay(minDepth, maxDepth, validPoints, totalPoints);
            });

            return filteredData;
        }

        /// <summary>
        /// 更新深度范围显示
        /// </summary>
        /// <param name="minDepth">最小深度</param>
        /// <param name="maxDepth">最大深度</param>
        /// <param name="validPoints">有效点数</param>
        /// <param name="totalPoints">总点数</param>
        private void UpdateDepthRangeDisplay(float minDepth, float maxDepth, int validPoints, int totalPoints)
        {
            double percentage = 100.0 * validPoints / totalPoints;
            //tb_DepthRange.Text = $"深度: [{minDepth:F1}, {maxDepth:F1}] ({percentage:F1}%有效)";
        }

        /// <summary>
        /// 渐进式加载表面数据 - 先快速预览，再高质量显示
        /// </summary>
        private async Task ProgressiveLoadSurfaceData(float[][] heightData, CancellationToken token)
        {
            // 第一步：快速预览（使用快速模式）
            var previewData = await Task.Run(() =>
                SmartSampleFloatArray(heightData, FAST_SAMPLE_SIZE, false), token);

            // 立即显示预览
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                FillHeightData(LightningChart, previewData);
            }, DispatcherPriority.Send, token);

            // 第二步：根据设置的质量模式加载最终数据
            int finalSampleSize = GetSampleSizeForCurrentRenderMode();

            // 如果预览和最终质量相同，跳过
            if (finalSampleSize == FAST_SAMPLE_SIZE)
                return;

            var finalData = await Task.Run(() =>
                SmartSampleFloatArray(heightData, finalSampleSize, true), token);

            // 显示最终高质量结果
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                FillHeightData(LightningChart, finalData);
            }, DispatcherPriority.Normal, token);
        }

        /// <summary>
        /// 将 OpenCV Mat 转换为二维浮点数组。
        /// </summary>
        public static float[][] MatToFloat2DArray(Mat mat)
        {
            if (mat.Type() != MatType.CV_32FC1)
            {
                mat.ConvertTo(mat, MatType.CV_32FC1);
            }

            int rows = mat.Rows;
            int cols = mat.Cols;

            // 使用Marshal.Copy获取所有数据
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            // 转换为二维数组
            float[][] result = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                result[i] = new float[cols];
                Array.Copy(buffer, i * cols, result[i], 0, cols);
            }
            //mat.Dispose();
            return result;
        }



        /// <summary>
        /// 将 Mat 图像转换并刷新到位图显示。
        /// </summary>
        private void ConvertMatToBitmapFrame(Mat mat)
        {
            // 确保 Mat 是有效的
            if (mat.Empty())
                return;

            // 将 Mat 转换为 BGRA 格式 (每个通道占用 1 字节)
            Mat matBgra = new Mat();
            if (mat.Channels() == 1)
            {
                mat.ConvertTo(matBgra, MatType.CV_32F);
                Cv2.Normalize(matBgra, matBgra, 0, 255, NormTypes.MinMax, MatType.CV_8U);
                Cv2.CvtColor(matBgra, matBgra, ColorConversionCodes.GRAY2BGRA);
            }
            else if (mat.Channels() == 2)
            {
                // 如果是 GRAY 格式，转换为 BGRA 格式 (添加 Alpha 通道)
                Cv2.CvtColor(mat, matBgra, ColorConversionCodes.GRAY2BGRA);
            }
            else if (mat.Channels() == 3)
            {
                // 如果是 BGR 格式，转换为 BGRA 格式 (添加 Alpha 通道)
                Cv2.CvtColor(mat, matBgra, ColorConversionCodes.BGR2BGRA);
            }
            else if (mat.Channels() == 4)
            {
                // 如果本身就是 BGRA 格式，直接使用
                matBgra = mat;
            }
            else
            {
                throw new NotSupportedException("Only BGR and BGRA images are supported.");
            }

            // 获取 Mat 的数据
            int width = matBgra.Width;
            int height = matBgra.Height;
            int stride = width * 4; // 每个像素 4 字节 (BGRA)
            byte[] pixelData = new byte[stride * height];

            // 将 Mat 数据复制到 byte 数组
            Marshal.Copy(matBgra.Data, pixelData, 0, pixelData.Length);
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 创建 WriteableBitmap 并加载 Mat 数据
                if (_writeableBitmap == null || _writeableBitmap.PixelWidth != width || _writeableBitmap.PixelHeight != height)
                {
                    _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                    drawViewer.UpdateBackgroundImageAsync(_writeableBitmap);
                }
                CopyMemory(_writeableBitmap, pixelData, width, height);
            }, DispatcherPriority.Send);
        }

        /// <summary>
        /// 把像素数据复制到 WriteableBitmap。
        /// </summary>
        private unsafe void CopyMemory(WriteableBitmap bitmap, byte[] pixelData, int width, int height)
        {
            bitmap.Lock();
            fixed (byte* ptr = pixelData)
            {
                var p = new IntPtr(ptr);
                MoveMemory(bitmap.BackBuffer, new IntPtr(ptr), (uint)pixelData.Length);
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();
        }

        /// <summary>
        /// 创建渐变调色板，提供更好的色域辨别
        /// </summary>
        /// <param name="ownerSeries">拥有者序列</param>
        /// <param name="heightDatas">高度数据</param>
        /// <returns>渐变调色板</returns>
        private ValueRangePalette CreateGradientPalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            var palette = new ValueRangePalette(ownerSeries);
            palette.Type = PaletteType.Gradient;
            palette.Steps.Clear();

            // 获取色域范围
            float minVal, maxVal;
            if (_useCustomColorRange)
            {
                // 使用自定义色域范围
                minVal = _customMinValue;
                maxVal = _customMaxValue;
            }
            else
            {
                // 计算数据的最值
                var (dataMin, dataMax) = CalculateDataRange(heightDatas);
                minVal = dataMin;
                maxVal = dataMax;

                // 更新数据范围显示
                SetDataRange(dataMin, dataMax);

                // 如果数据范围太小，使用默认范围
                if (Math.Abs(maxVal - minVal) < 1e-6)
                {
                    minVal = -1.0f;
                    maxVal = 1.0f;
                }
            }

            var range = maxVal - minVal;

            // 创建经典的蓝->青->绿->黄->红渐变
            palette.MinValue = minVal;

            // 分为8个色阶，提供更好的色域辨别
            var step1 = minVal + range * 0.0f;  // 深蓝
            var step2 = minVal + range * 0.125f; // 蓝色
            var step3 = minVal + range * 0.25f;  // 青色
            var step4 = minVal + range * 0.375f; // 绿青色
            var step5 = minVal + range * 0.5f;   // 绿色
            var step6 = minVal + range * 0.625f; // 黄绿色
            var step7 = minVal + range * 0.75f;  // 黄色
            var step8 = minVal + range * 0.875f; // 橙色
            var step9 = maxVal;                  // 红色

            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 139), step1));     // 深蓝
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 255), step2));     // 蓝色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 255), step3));   // 青色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 128), step4));   // 绿青色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 0), step5));     // 绿色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(128, 255, 0), step6));   // 黄绿色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 0), step7));   // 黄色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 165, 0), step8));   // 橙色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 0, 0), step9));     // 红色

            return palette;
        }

        /// <summary>
        /// 计算数据范围 - 自动跳过 NaN 和无穷值
        /// </summary>
        /// <param name="heightDatas">高度数据</param>
        /// <returns>最小值和最大值</returns>
        private (float minVal, float maxVal) CalculateDataRange(float[][] heightDatas)
        {
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;
            int validCount = 0;

            foreach (var row in heightDatas)
            {
                foreach (var value in row)
                {
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        continue;

                    validCount++;
                    if (value < minVal) minVal = value;
                    if (value > maxVal) maxVal = value;
                }
            }

            // 如果没有有效数据，返回默认范围
            if (validCount == 0 || minVal == float.MaxValue || maxVal == float.MinValue)
            {
                System.Diagnostics.Debug.WriteLine("警告: 没有找到有效的深度数据，使用默认范围 [-1.0, 1.0]");
                return (-1.0f, 1.0f);
            }

            System.Diagnostics.Debug.WriteLine($"有效数据范围: [{minVal:F3}, {maxVal:F3}] (共 {validCount:N0} 个有效点)");
            return (minVal, maxVal);
        }

        /// <summary>
        /// 创建热图风格的调色板（另一种选择）
        /// </summary>
        /// <param name="ownerSeries">拥有者序列</param>
        /// <param name="heightDatas">高度数据</param>
        /// <returns>热图调色板</returns>
        private ValueRangePalette CreateHeatmapPalette(SeriesBase3D ownerSeries, float[][] heightDatas)
        {
            var palette = new ValueRangePalette(ownerSeries);
            palette.Type = PaletteType.Gradient;
            palette.Steps.Clear();

            // 获取色域范围
            float minVal, maxVal;
            if (_useCustomColorRange)
            {
                // 使用自定义色域范围
                minVal = _customMinValue;
                maxVal = _customMaxValue;
            }
            else
            {
                // 计算数据的最值
                var (dataMin, dataMax) = CalculateDataRange(heightDatas);
                minVal = dataMin;
                maxVal = dataMax;

                // 更新数据范围显示
                SetDataRange(dataMin, dataMax);

                // 如果数据范围太小，使用默认范围
                if (Math.Abs(maxVal - minVal) < 1e-6)
                {
                    minVal = -1.0f;
                    maxVal = 1.0f;
                }
            }

            var range = maxVal - minVal;
            palette.MinValue = minVal;

            // 热图风格：黑->紫->蓝->绿->黄->红->白
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 0), minVal));                    // 黑色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(128, 0, 128), minVal + range * 0.15f)); // 紫色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 0, 255), minVal + range * 0.3f));   // 蓝色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(0, 255, 0), minVal + range * 0.5f));   // 绿色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 0), minVal + range * 0.7f)); // 黄色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 0, 0), minVal + range * 0.85f));  // 红色
            palette.Steps.Add(new PaletteStep(palette, Color.FromRgb(255, 255, 255), maxVal));              // 白色

            return palette;
        }

        /// <summary>
        /// 将高度数据分发到当前选择的 3D 渲染器。
        /// </summary>
        private void FillHeightData(LightningChart chart, float[][] heightDatas)
        {
            _lastRenderedHeightData = heightDatas;
            _lastDataRows = heightDatas?.Length ?? 0;
            _lastDataCols = heightDatas != null && heightDatas.Length > 0 ? heightDatas[0].Length : 0;

            if (_currentRenderMode == RenderMode.PointCloud)
            {
                FillPointCloudOptimized(chart, ref _pointCloudSeries, heightDatas);
            }
            else
            {
                FillSurfaceOptimized(chart, ref _surfaceGround, heightDatas);
            }
        }

        /// <summary>
        /// 获取当前渲染模式下的采样大小。
        /// </summary>
        private int GetSampleSizeForCurrentRenderMode()
        {
            int sampleSize = GetSampleSizeForQuality(_currentQualityMode);
            return _currentRenderMode == RenderMode.PointCloud ? Math.Min(sampleSize, HIGH_QUALITY_SAMPLE_SIZE) : sampleSize;
        }

        private void FillSurfaceOptimized(LightningChart chart, ref SurfaceGridSeries3D? surfaceGridSeries3D,
            float[][] heightDatas, bool reverseZ = false)
        {
            if (heightDatas.Length == 0 || heightDatas[0].Length == 0) return;

            chart.BeginUpdate();

            try
            {
                //根据数据尺寸动态调整3D视图比例
                int dataRows = heightDatas[0].Length;
                int dataCols = heightDatas.Length;
                int totalDataPoints = dataRows * dataCols;

                //智能启用数据游标
                SmartEnableDataCursor(totalDataPoints);
                AdjustChartDimensionsToData(chart, dataRows, dataCols);
                //优化坐标映射
                OptimizeCoordinateMapping(chart, heightDatas);

                if (_pointCloudSeries != null)
                {
                    _pointCloudSeries.Visible = false;
                    _pointCloudSeries.ShowInLegendBox = false;
                }

                if (surfaceGridSeries3D == null)
                {
                    surfaceGridSeries3D = new SurfaceGridSeries3D(chart.View3D, Axis3DBinding.Primary,
                        Axis3DBinding.Primary,
                        Axis3DBinding.Primary);
                    chart.View3D.SurfaceGridSeries3D.Add(surfaceGridSeries3D);

                    surfaceGridSeries3D.Fill = SurfaceFillStyle.PalettedByY;
                    surfaceGridSeries3D.ContourPalette.Type = PaletteType.Gradient;
                    surfaceGridSeries3D.ContourLineType = ContourLineType3D.None;
                    surfaceGridSeries3D.WireframeType = SurfaceWireframeType3D.None;
                    surfaceGridSeries3D.Title.Text = "曲面";
                }

                surfaceGridSeries3D.Visible = true;
                surfaceGridSeries3D.ShowInLegendBox = true;
                surfaceGridSeries3D.SetSize(heightDatas.Length, heightDatas[0].Length);

                var surfacePointArray = surfaceGridSeries3D.Data;
                var xAxisRange = chart.View3D.XAxisPrimary3D.Maximum - chart.View3D.XAxisPrimary3D.Minimum;
                var zAxisRange = chart.View3D.ZAxisPrimary3D.Maximum - chart.View3D.ZAxisPrimary3D.Minimum;
                var xAxisMin = chart.View3D.XAxisPrimary3D.Minimum;
                var zAxisMin = chart.View3D.ZAxisPrimary3D.Minimum;

                var rowCount = heightDatas.Length;
                var colCount = heightDatas[0].Length;

                for (var i = 0; i < rowCount; i++)
                {
                    for (var j = 0; j < colCount; j++)
                    {
                        var x = xAxisMin + xAxisRange * i / (rowCount - 1);
                        var z = zAxisMin + zAxisRange * j / (colCount - 1);
                        var y = heightDatas[reverseZ ? rowCount - 1 - i : i][j];

                        surfacePointArray[i, j].X = x;
                        surfacePointArray[i, j].Y = y;
                        surfacePointArray[i, j].Z = z;
                        surfacePointArray[i, j].Value = heightDatas[i][j];
                    }
                }

                surfaceGridSeries3D.ContourPalette = _currentPaletteType == ColorPaletteType.Classic ? CreateGradientPalette(surfaceGridSeries3D, heightDatas) : CreateHeatmapPalette(surfaceGridSeries3D, heightDatas);
                surfaceGridSeries3D.InvalidateData();
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        /// <summary>
        /// 使用高度数据填充点云渲染。
        /// </summary>
        private void FillPointCloudOptimized(LightningChart chart, ref PointLineSeries3D? pointCloudSeries, float[][] heightDatas)
        {
            if (heightDatas.Length == 0 || heightDatas[0].Length == 0) return;

            chart.BeginUpdate();

            try
            {
                int dataRows = heightDatas[0].Length;
                int dataCols = heightDatas.Length;
                int totalDataPoints = dataRows * dataCols;

                SmartEnableDataCursor(totalDataPoints);
                AdjustChartDimensionsToData(chart, dataRows, dataCols);
                OptimizeCoordinateMapping(chart, heightDatas);

                if (_surfaceGround != null)
                {
                    _surfaceGround.Visible = false;
                    _surfaceGround.ShowInLegendBox = false;
                }

                if (pointCloudSeries == null)
                {
                    pointCloudSeries = new PointLineSeries3D(chart.View3D, Axis3DBinding.Primary,
                        Axis3DBinding.Primary,
                        Axis3DBinding.Primary);
                    pointCloudSeries.PointsType = PointsType3D.CompactPointsColor;
                    pointCloudSeries.LineVisible = false;
                    pointCloudSeries.PointsVisible = true;
                    pointCloudSeries.IndividualPointColors = true;
                    pointCloudSeries.PointsOptimization = PointsRenderOptimization3D.Pixels;
                    pointCloudSeries.PointStyle.Shape3D = PointShape3D.Box;
                    pointCloudSeries.PointStyle.Size3D.Width = 1;
                    pointCloudSeries.PointStyle.Size3D.Height = 1;
                    pointCloudSeries.PointStyle.Size3D.Depth = 1;
                    pointCloudSeries.Title.Text = "点云";
                    chart.View3D.PointLineSeries3D.Add(pointCloudSeries);
                }

                pointCloudSeries.Visible = true;
                pointCloudSeries.ShowInLegendBox = true;
                pointCloudSeries.PointsCompactColored = BuildPointCloudPoints(chart, heightDatas);
                pointCloudSeries.InvalidateData();
            }
            finally
            {
                chart.EndUpdate();
            }
        }

        /// <summary>
        /// 根据高度数据构建点云点集。
        /// </summary>
        private SeriesPointCompactColored3D[] BuildPointCloudPoints(LightningChart chart, float[][] heightDatas)
        {
            var (minVal, maxVal) = GetPointColorRange(heightDatas);
            var xAxisRange = chart.View3D.XAxisPrimary3D.Maximum - chart.View3D.XAxisPrimary3D.Minimum;
            var zAxisRange = chart.View3D.ZAxisPrimary3D.Maximum - chart.View3D.ZAxisPrimary3D.Minimum;
            var xAxisMin = chart.View3D.XAxisPrimary3D.Minimum;
            var zAxisMin = chart.View3D.ZAxisPrimary3D.Minimum;
            var rowCount = heightDatas.Length;
            var colCount = heightDatas[0].Length;
            var rowDivisor = Math.Max(1, rowCount - 1);
            var colDivisor = Math.Max(1, colCount - 1);
            var validCount = 0;

            for (var i = 0; i < rowCount; i++)
            {
                for (var j = 0; j < colCount; j++)
                {
                    var value = heightDatas[i][j];
                    if (!float.IsNaN(value) && !float.IsInfinity(value))
                    {
                        validCount++;
                    }
                }
            }

            var points = new SeriesPointCompactColored3D[validCount];
            var index = 0;

            for (var i = 0; i < rowCount; i++)
            {
                for (var j = 0; j < colCount; j++)
                {
                    var value = heightDatas[i][j];
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        continue;
                    }

                    var x = (float)(xAxisMin + xAxisRange * i / rowDivisor);
                    var z = (float)(zAxisMin + zAxisRange * j / colDivisor);
                    points[index++] = new SeriesPointCompactColored3D(x, value, z, ToLightningChartColor(GetPointColor(value, minVal, maxVal)));
                }
            }

            return points;
        }

        private (float minVal, float maxVal) GetPointColorRange(float[][] heightDatas)
        {
            if (_useCustomColorRange)
            {
                return (_customMinValue, _customMaxValue);
            }

            var range = CalculateDataRange(heightDatas);
            SetDataRange(range.minVal, range.maxVal);
            return Math.Abs(range.maxVal - range.minVal) < 1e-6 ? (-1.0f, 1.0f) : range;
        }

        /// <summary>
        /// 按高度值计算点云颜色。
        /// </summary>
        private Color GetPointColor(float value, float minVal, float maxVal)
        {
            var ratio = Math.Abs(maxVal - minVal) < 1e-6 ? 0.5 : (value - minVal) / (maxVal - minVal);
            ratio = Math.Max(0, Math.Min(1, ratio));

            return _currentPaletteType == ColorPaletteType.Classic
                ? InterpolatePalette(ratio, new[]
                {
                    Color.FromRgb(0, 0, 139),
                    Color.FromRgb(0, 0, 255),
                    Color.FromRgb(0, 255, 255),
                    Color.FromRgb(0, 255, 128),
                    Color.FromRgb(0, 255, 0),
                    Color.FromRgb(128, 255, 0),
                    Color.FromRgb(255, 255, 0),
                    Color.FromRgb(255, 165, 0),
                    Color.FromRgb(255, 0, 0)
                })
                : InterpolatePalette(ratio, new[]
                {
                    Color.FromRgb(0, 0, 0),
                    Color.FromRgb(128, 0, 128),
                    Color.FromRgb(0, 0, 255),
                    Color.FromRgb(0, 255, 0),
                    Color.FromRgb(255, 255, 0),
                    Color.FromRgb(255, 0, 0),
                    Color.FromRgb(255, 255, 255)
                });
        }

        /// <summary>
        /// 按比例在调色板颜色间插值。
        /// </summary>
        private static Color InterpolatePalette(double ratio, Color[] colors)
        {
            if (colors.Length == 0) return Colors.White;
            if (colors.Length == 1) return colors[0];

            var scaled = ratio * (colors.Length - 1);
            var index = Math.Min(colors.Length - 2, (int)Math.Floor(scaled));
            var localRatio = scaled - index;
            var start = colors[index];
            var end = colors[index + 1];

            return Color.FromRgb(
                (byte)(start.R + (end.R - start.R) * localRatio),
                (byte)(start.G + (end.G - start.G) * localRatio),
                (byte)(start.B + (end.B - start.B) * localRatio));
        }

        /// <summary>
        /// 将 WPF 颜色转换为 LightningChart 颜色值。
        /// </summary>
        private static int ToLightningChartColor(Color color)
        {
            return unchecked((int)((uint)color.A << 24 | (uint)color.R << 16 | (uint)color.G << 8 | color.B));
        }

        /// <summary>
        /// 清空图表数据和瑕疵信息。
        /// </summary>
        public void ClearData()
        {
            ClearDefectInfo();
            if (_pointCloudSeries != null)
            {
                _pointCloudSeries.PointsCompactColored = Array.Empty<SeriesPointCompactColored3D>();
                _pointCloudSeries.InvalidateData();
            }
        }

        /// <summary>
        /// 设置图表名称。
        /// </summary>
        public void SetChartName(string chartName)
        {
            //ViewModel.ChartName = chartName;
        }

        /// <summary>
        /// 显示图表设置视图。
        /// </summary>
        public void ShowSettingView()
        {

        }

        /// <summary>
        /// 处理质量模式切换。
        /// </summary>
        private void OnQualityModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_QualityMode.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string tag &&
                Enum.TryParse<QualityMode>(tag, out var mode))
            {
                SetQualityMode(mode);
                if (_lastRenderedHeightData != null && !_isProcessing)
                {
                    FillHeightData(LightningChart, _lastRenderedHeightData);
                }
            }
        }

        /// <summary>
        /// 处理颜色调色板切换。
        /// </summary>
        private void OnColorPaletteChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_ColorPalette.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string tag &&
                Enum.TryParse<ColorPaletteType>(tag, out var paletteType))
            {
                _currentPaletteType = paletteType;
                if ((_surfaceGround != null || _pointCloudSeries != null) && !_isProcessing)
                {
                    RefreshPalette();
                }
            }
        }

        /// <summary>
        /// 处理渲染模式切换。
        /// </summary>
        private void OnRenderModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_RenderMode.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string tag &&
                Enum.TryParse<RenderMode>(tag, out var mode))
            {
                _currentRenderMode = mode;
                if (_lastRenderedHeightData != null && !_isProcessing)
                {
                    FillHeightData(LightningChart, _lastRenderedHeightData);
                }
            }
        }

        /// <summary>
        /// 刷新当前渲染器的颜色。
        /// </summary>
        private void RefreshPalette()
        {
            if (_currentRenderMode == RenderMode.PointCloud)
            {
                if (_pointCloudSeries == null || _lastRenderedHeightData == null) return;

                try
                {
                    LightningChart.BeginUpdate();
                    _pointCloudSeries.PointsCompactColored = BuildPointCloudPoints(LightningChart, _lastRenderedHeightData);
                    _pointCloudSeries.InvalidateData();
                }
                finally
                {
                    LightningChart.EndUpdate();
                }

                return;
            }

            if (_surfaceGround?.Data == null) return;

            try
            {
                LightningChart.BeginUpdate();

                var surfaceData = _surfaceGround.Data;
                var rows = surfaceData.GetLength(0);
                var cols = surfaceData.GetLength(1);

                var heightData = new float[rows][];
                for (int i = 0; i < rows; i++)
                {
                    heightData[i] = new float[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        heightData[i][j] = (float)surfaceData[i, j].Value;
                    }
                }

                _surfaceGround.ContourPalette = _currentPaletteType == ColorPaletteType.Classic ? CreateGradientPalette(_surfaceGround, heightData) : CreateHeatmapPalette(_surfaceGround, heightData);
                _surfaceGround.InvalidateData();
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        /// <summary>
        /// 使用当前质量设置重新渲染。
        /// </summary>
        public void RefreshWithCurrentQuality()
        {
            if (_lastRenderedHeightData != null && !_isProcessing)
            {
                FillHeightData(LightningChart, _lastRenderedHeightData);
            }
        }

        /// <summary>
        /// 视距滑块值改变事件处理
        /// </summary>
        private void OnViewDistanceChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LightningChart?.View3D?.Camera == null) return;

            try
            {
                double newDistance = e.NewValue;

                // 更新镜头视距
                LightningChart.View3D.Camera.ViewDistance = newDistance;

                // 更新显示文本
                tb_ViewDistance.Text = ((int)newDistance).ToString();
            }
            catch (Exception ex)
            {
                // ignored
            }
        }

        /// <summary>
        /// 更新3D视图的高度缩放
        /// </summary>
        private void Update3DHeight()
        {
            if (LightningChart?.View3D == null || nud_3DHeight == null) return;

            LightningChart.BeginUpdate();
            // 获取当前值并更新图表
            var newHeight = nud_3DHeight.Value;
            LightningChart.View3D.Dimensions.Height = (float)newHeight;
            //ConfigCenter.FrameConfig.SetPara("MFDJC0_HeightChartSetting_DimensionsHeight", newHeight);
            LightningChart.EndUpdate();
        }

        /// <summary>
        /// 处理3D高度输入框的回车键事件
        /// </summary>
        private void Nud_3DHeight_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Update3DHeight();

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                (sender as UIElement)?.MoveFocus(request);
            }
        }

        /// <summary>
        /// 处理3D高度输入框失去焦点事件
        /// </summary>
        private void Nud_3DHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            Update3DHeight();
        }

        /// <summary>
        /// 应用 3D 宽度和长度缩放。
        /// </summary>
        private void Apply3DWidthAndDepthScale(LightningChart chart)
        {
            if (chart?.View3D == null || nud_3DWidthScale == null || nud_3DLengthScale == null) return;

            chart.View3D.Dimensions.Width = _baseDimensionWidth * Math.Max(0.1, nud_3DWidthScale.Value);
            chart.View3D.Dimensions.Depth = _baseDimensionDepth * Math.Max(0.1, nud_3DLengthScale.Value);
        }

        /// <summary>
        /// 更新 3D 宽度和长度缩放。
        /// </summary>
        private void Update3DWidthAndDepthScale()
        {
            if (LightningChart?.View3D == null || nud_3DWidthScale == null || nud_3DLengthScale == null) return;

            LightningChart.BeginUpdate();
            try
            {
                Apply3DWidthAndDepthScale(LightningChart);
            }
            finally
            {
                LightningChart.EndUpdate();
            }
        }

        /// <summary>
        /// 处理 3D 缩放输入框回车事件。
        /// </summary>
        private void Nud_3DScale_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Update3DWidthAndDepthScale();

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                (sender as UIElement)?.MoveFocus(request);
            }
        }

        /// <summary>
        /// 处理 3D 缩放输入框失焦事件。
        /// </summary>
        private void Nud_3DScale_LostFocus(object sender, RoutedEventArgs e)
        {
            Update3DWidthAndDepthScale();
        }

        /// <summary>
        /// 重置视图按钮点击事件处理
        /// </summary>
        private void OnResetViewClicked(object sender, RoutedEventArgs e)
        {
            if (!_cameraStateInitialized || LightningChart?.View3D?.Camera == null) return;

            try
            {
                LightningChart.BeginUpdate();

                // 还原到初始镜头状态
                LightningChart.View3D.Camera.ViewDistance = _initialCameraState.ViewDistance;
                LightningChart.View3D.Camera.RotationX = _initialCameraState.RotationX;
                LightningChart.View3D.Camera.RotationY = _initialCameraState.RotationY;
                LightningChart.View3D.Camera.RotationZ = _initialCameraState.RotationZ;
                LightningChart.View3D.Camera.Projection = _initialCameraState.Projection;

                // 同步滑块值
                slider_ViewDistance.Value = _initialCameraState.ViewDistance;
                tb_ViewDistance.Text = ((int)_initialCameraState.ViewDistance).ToString();

                LightningChart.EndUpdate();

                System.Diagnostics.Debug.WriteLine("视图已还原到初始状态");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"还原视图时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置镜头视距
        /// </summary>
        /// <param name="distance">视距值</param>
        public void SetViewDistance(double distance)
        {
            if (LightningChart?.View3D?.Camera == null) return;

            // 限制范围
            distance = Math.Max(slider_ViewDistance.Minimum, Math.Min(slider_ViewDistance.Maximum, distance));

            slider_ViewDistance.Value = distance;
            LightningChart.View3D.Camera.ViewDistance = distance;
            tb_ViewDistance.Text = ((int)distance).ToString();
        }

        /// <summary>
        /// 获取当前镜头状态
        /// </summary>
        /// <returns>当前镜头状态</returns>
        public CameraState GetCurrentCameraState()
        {
            if (LightningChart?.View3D?.Camera == null)
                return _initialCameraState;

            return new CameraState
            {
                ViewDistance = LightningChart.View3D.Camera.ViewDistance,
                RotationX = LightningChart.View3D.Camera.RotationX,
                RotationY = LightningChart.View3D.Camera.RotationY,
                RotationZ = LightningChart.View3D.Camera.RotationZ,
                Projection = LightningChart.View3D.Camera.Projection
            };
        }

        /// <summary>
        /// 键盘快捷键处理
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsVisible || LightningChart?.View3D?.Camera == null) return;

            const double stepSize = 10.0; // 视距调整步长

            switch (e.Key)
            {
                case Key.Add: // 数字键盘 +
                //case Key.OemPlus: // 主键盘 +
                    // 拉近视距
                    ZoomIn(stepSize);
                    e.Handled = true;
                    break;

                case Key.Subtract: // 数字键盘 -
                //case Key.OemMinus: // 主键盘 -
                    // 拉远视距
                    ZoomOut(stepSize);
                    e.Handled = true;
                    break;

                case Key.Home:
                    // 还原初始视图
                    OnResetViewClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 拉近视距
        /// </summary>
        /// <param name="step">步长</param>
        public void ZoomIn(double step = 10.0)
        {
            double currentDistance = slider_ViewDistance.Value;
            double newDistance = Math.Max(slider_ViewDistance.Minimum, currentDistance - step);
            SetViewDistance(newDistance);
        }

        /// <summary>
        /// 拉远视距
        /// </summary>
        /// <param name="step">步长</param>
        public void ZoomOut(double step = 10.0)
        {
            double currentDistance = slider_ViewDistance.Value;
            double newDistance = Math.Min(slider_ViewDistance.Maximum, currentDistance + step);
            SetViewDistance(newDistance);
        }

        /// <summary>
        /// 适配视图到数据范围
        /// </summary>
        public void FitToData()
        {
            if (LightningChart?.View3D?.Camera == null) return;

            try
            {
                if (_currentRenderMode == RenderMode.PointCloud)
                {
                    if (_lastDataRows <= 0 || _lastDataCols <= 0) return;
                }
                else
                {
                    if (_surfaceGround?.Data == null) return;
                }

                double suggestedDistance = Math.Max(LightningChart.View3D.Dimensions.Width,
                                                    LightningChart.View3D.Dimensions.Depth) * 1.2 +
                                           Math.Max(40, LightningChart.View3D.Dimensions.Height);
                suggestedDistance = Math.Max(slider_ViewDistance.Minimum,
                                           Math.Min(slider_ViewDistance.Maximum, suggestedDistance));

                SetViewDistance(suggestedDistance);

                System.Diagnostics.Debug.WriteLine($"Fit view distance: {suggestedDistance:F1}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fit view error: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据数据尺寸动态调整3D视图比例，确保与实际数据比例一致
        /// </summary>
        /// <param name="chart">图表对象</param>
        /// <param name="dataRows">数据行数</param>
        /// <param name="dataCols">数据列数</param>
        private void AdjustChartDimensionsToData(LightningChart chart, int dataRows, int dataCols)
        {
            // 计算数据的长宽比
            var dataAspectRatio = (double)dataCols / dataRows;

            // 基础尺寸 - 可以根据需要调整
            var baseSize = 200.0;


            // 根据长宽比调整Width和Depth
            double width, depth;
            width = baseSize * dataAspectRatio;
            depth = baseSize;


            // 高度设置 - 可以通过配置调整，但不要太小
            double height = nud_3DHeight != null ? nud_3DHeight.Value : 30.0;

            // 应用新的尺寸
            _baseDimensionWidth = width;
            _baseDimensionDepth = depth;
            Apply3DWidthAndDepthScale(chart);
            chart.View3D.Dimensions.Height = height;

            // 设置坐标轴范围以匹配数据尺寸
            SetAxisRangesToData(chart, dataRows, dataCols);

            System.Diagnostics.Debug.WriteLine($"调整3D视图尺寸: Width={chart.View3D.Dimensions.Width:F1}, Height={chart.View3D.Dimensions.Height:F1}, Depth={chart.View3D.Dimensions.Depth:F1}");
            System.Diagnostics.Debug.WriteLine($"数据尺寸: {dataRows}×{dataCols}, 长宽比: {dataAspectRatio:F2}");
        }

        /// <summary>
        /// 设置坐标轴范围以匹配数据尺寸
        /// </summary>
        /// <param name="chart">图表对象</param>
        /// <param name="dataRows">数据行数</param>
        /// <param name="dataCols">数据列数</param>
        private void SetAxisRangesToData(LightningChart chart, int dataRows, int dataCols)
        {
            // X轴对应行方向
            chart.View3D.XAxisPrimary3D.SetRange(0, dataRows - 1);

            // Z轴对应列方向  
            chart.View3D.ZAxisPrimary3D.SetRange(0, dataCols - 1);
        }

        /// <summary>
        /// 优化坐标映射，确保3D图与2D图的像素位置精确对应
        /// </summary>
        /// <param name="chart">图表对象</param>
        /// <param name="heightData">高度数据</param>
        private void OptimizeCoordinateMapping(LightningChart chart, float[][] heightData)
        {
            if (heightData == null || heightData.Length == 0) return;

            int rows = heightData.Length;
            int cols = heightData[0].Length;

            // 确保坐标轴的范围与数据索引完全匹配
            // 这样3D图的每个点都对应灰度图的一个像素
            chart.View3D.XAxisPrimary3D.SetRange(0, rows - 1);
            chart.View3D.ZAxisPrimary3D.SetRange(0, cols - 1);

            // 计算高度数据的范围
            var (minVal, maxVal) = CalculateDataRange(heightData);
            if (Math.Abs(maxVal - minVal) > 1e-6)
            {
                // 设置Y轴范围稍微大一点，以便更好地显示数据
                double range = maxVal - minVal;
                double padding = range * 0.1; // 10%的填充
                chart.View3D.YAxisPrimary3D.SetRange(minVal - padding, maxVal + padding);
            }

            System.Diagnostics.Debug.WriteLine($"坐标轴范围 - X: [0, {rows - 1}], Y: [{minVal:F3}, {maxVal:F3}], Z: [0, {cols - 1}]");
        }

        /// <summary>
        /// 适配视图按钮点击事件处理
        /// </summary>
        private void OnFitViewClicked(object sender, RoutedEventArgs e)
        {
            FitToData();
        }

        /// <summary>
        /// 数据游标切换事件处理
        /// </summary>
        private void OnDataCursorToggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                bool isEnabled = toggleButton.IsChecked ?? false;
                ToggleDataCursor(isEnabled);

                // 更新按钮提示信息
                string status = isEnabled ? "已启用" : "已禁用";
                string tooltip = $"数据光标{status}\n{(isEnabled ? "可查看鼠标位置的数据值" : "点击启用数据光标功能")}\n大数据集时会自动优化性能";
                toggleButton.ToolTip = tooltip;

                System.Diagnostics.Debug.WriteLine($"数据游标{status}");
            }
        }

        #region 工具栏方法





        /// <summary>
        /// 处理恢复原始缩放按钮点击。
        /// </summary>
        private void btn_OriginalScale_Click(object sender, RoutedEventArgs e)
        {
            drawViewer.Home();
        }

        /// <summary>
        /// 处理保存图像按钮点击。
        /// </summary>
        private void btn_SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var backgroundImage = drawViewer.BackgroundImage;
            if (backgroundImage == null)
            {
                MessageBox.Show("没有可保存的图像", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = @"Original tiff files (*.tif;*.tiff)|*.tif;*.tiff|Images files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                OverwritePrompt = true,
                RestoreDirectory = true,
                FileName = $"GrayImage_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var extension = Path.GetExtension(dlg.FileName).ToLower();

                    if (extension == ".tif" || extension == ".tiff")
                    {
                        // 保存为TIFF格式 - 保持原始数据精度
                        SaveAsOriginalTiff(dlg.FileName);
                    }
                    else
                    {
                        // 询问用户想要保存哪种图像
                        var result = MessageBox.Show(
                            "选择保存类型：\n" +
                            "是(Yes) - 保存纯灰度图\n" +
                            "否(No) - 保存带绘制内容的图像\n" +
                            "取消(Cancel) - 取消保存",
                            "选择保存类型",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        switch (result)
                        {
                            case DialogResult.Yes:
                                // 保存纯灰度图
                                SavePureGrayImage(dlg.FileName, backgroundImage);
                                break;
                            case DialogResult.No:
                                // 保存带绘制内容的图像
                                SaveCompositeImage(dlg.FileName, backgroundImage);
                                break;
                            default:
                                return; // 取消保存
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"图像已保存至：{dlg.FileName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 保存为原始TIFF格式
        /// </summary>
        private void SaveAsOriginalTiff(string fileName)
        {
            var para = new Dictionary<string, string>()
            {
                {"Path", fileName }
            };
            // 如果有原始数据保存服务，调用它
            //_dataProcessorService?.SendCommand("SaveRawImage", para);

            // 临时实现：如果没有原始数据服务，保存当前显示的灰度图
            if (drawViewer.BackgroundImage != null)
            {
                SavePureGrayImage(fileName, drawViewer.BackgroundImage);
            }
        }

        /// <summary>
        /// 保存纯灰度图（不包含绘制内容）
        /// </summary>
        private void SavePureGrayImage(string fileName, BitmapSource grayImage)
        {
            // 直接保存灰度图，不包含Canvas上的绘制内容
            var encoder = GetImageEncoder(fileName);
            if (encoder != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(grayImage));

                using (var fileStream = new FileStream(fileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }

        /// <summary>
        /// 保存复合图像（包含绘制内容）
        /// </summary>
        private void SaveCompositeImage(string fileName, BitmapSource backgroundImage)
        {
            // 生成包含绘制内容的复合图像
            var frame = drawCanvas.ToBitmapFrame(
                backgroundImage.PixelWidth,
                backgroundImage.PixelHeight,
                DpiHelper.GetDpiFromVisual(drawCanvas),
                backgroundImage);

            if (frame != null)
            {
                ImageHelper.Save(fileName, frame);
            }
        }

        /// <summary>
        /// 根据文件扩展名获取对应的图像编码器
        /// </summary>
        private BitmapEncoder GetImageEncoder(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                ".bmp" => new BmpBitmapEncoder(),
                ".tif" or ".tiff" => new TiffBitmapEncoder { Compression = TiffCompressOption.None },
                _ => new PngBitmapEncoder() // 默认使用PNG
            };
        }

        /// <summary>
        /// 椭圆按钮点击事件 - 启用椭圆绘制模式
        /// </summary>
        //private void btn_Ellipse_OnClick(object sender, RoutedEventArgs e)
        //{
        //    // 切换椭圆绘制模式
        //    if (drawCanvas.DrawingToolType == DrawToolType.Ellipse)
        //    {
        //        // 当前是椭圆模式，切换为编辑模式
        //        drawCanvas.DrawingToolType = DrawToolType.Edit;
        //        btn_Ellipse.Content = "&#xe637;"; // 恢复原始图标
        //        System.Diagnostics.Debug.WriteLine("椭圆绘制模式已关闭");
        //    }
        //    else
        //    {
        //        // 切换为椭圆绘制模式
        //        drawCanvas.DrawingToolType = DrawToolType.Ellipse;
        //        btn_Ellipse.Content = "⭕"; // 改变按钮图标表示激活状态
        //        System.Diagnostics.Debug.WriteLine("椭圆绘制模式已启用");
        //    }
        //}

        /// <summary>
        /// 清除工具按钮点击事件
        /// </summary>
        private void btn_ClearTools_OnClick(object sender, RoutedEventArgs e)
        {
            ClearAllDrawings();
        }

        #endregion



        /// <summary>
        /// 禁用圆形绘制模式
        /// </summary>
        private void DisableCircleDrawingMode()
        {
            // 解绑鼠标事件
            drawCanvas.MouseLeftButtonDown -= OnCanvasMouseDown;
            drawCanvas.MouseMove -= OnCanvasMouseMove;
            drawCanvas.MouseLeftButtonUp -= OnCanvasMouseUp;

            // 恢复默认光标
            drawCanvas.Cursor = Cursors.Arrow;

            _isDrawingCircle = false;
        }

        /// <summary>
        /// 鼠标按下事件 - 开始绘制圆形
        /// </summary>
        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawingCircle) return;

            _circleCenter = e.GetPosition(drawCanvas);

            // 创建新的圆形
            _currentCircle = new Ellipse
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            // 添加到Canvas
            drawCanvas.Children.Add(_currentCircle);

            // 设置初始位置和大小
            Canvas.SetLeft(_currentCircle, _circleCenter.X);
            Canvas.SetTop(_currentCircle, _circleCenter.Y);
            _currentCircle.Width = 0;
            _currentCircle.Height = 0;

            drawCanvas.CaptureMouse();

            System.Diagnostics.Debug.WriteLine($"开始绘制圆形，中心点: ({_circleCenter.X:F1}, {_circleCenter.Y:F1})");
        }

        /// <summary>
        /// 鼠标移动事件 - 实时更新圆形大小
        /// </summary>
        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawingCircle || _currentCircle == null || !drawCanvas.IsMouseCaptured) return;

            Point currentPos = e.GetPosition(drawCanvas);

            // 计算半径（取x和y方向距离的最大值，保证是圆形）
            double deltaX = Math.Abs(currentPos.X - _circleCenter.X);
            double deltaY = Math.Abs(currentPos.Y - _circleCenter.Y);
            _circleRadius = Math.Max(deltaX, deltaY);

            // 更新圆形的位置和大小
            double diameter = _circleRadius * 2;
            _currentCircle.Width = diameter;
            _currentCircle.Height = diameter;

            // 设置圆心位置
            Canvas.SetLeft(_currentCircle, _circleCenter.X - _circleRadius);
            Canvas.SetTop(_currentCircle, _circleCenter.Y - _circleRadius);

            // 实时显示圆形信息
            UpdateCircleInfo();
        }

        /// <summary>
        /// 鼠标抬起事件 - 完成圆形绘制
        /// </summary>
        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawingCircle || _currentCircle == null) return;

            drawCanvas.ReleaseMouseCapture();

            // 最终确认圆形信息
            UpdateCircleInfo();

            System.Diagnostics.Debug.WriteLine($"圆形绘制完成: 中心({_circleCenter.X:F1}, {_circleCenter.Y:F1}), 半径: {_circleRadius:F1}");
        }

        /// <summary>
        /// 更新圆形信息显示
        /// </summary>
        private void UpdateCircleInfo()
        {
            if (_currentCircle == null) return;

            // 转换为图像坐标系（如果需要的话）
            var imageCoords = ConvertCanvasToImageCoordinates(_circleCenter);
            var imageRadius = ConvertCanvasToImageRadius(_circleRadius);

            // 在UI上显示圆形信息
            string info = $"圆心: ({imageCoords.X:F0}, {imageCoords.Y:F0}), 半径: {imageRadius:F1}";

            // 更新UI显示
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                tb_CircleInfo.Text = info;
                tb_CircleInfo.Visibility = Visibility.Visible;
            });

            // 输出到调试窗口
            System.Diagnostics.Debug.WriteLine($"圆形信息: {info}");
        }

        /// <summary>
        /// 将Canvas坐标转换为图像坐标
        /// </summary>
        private Point ConvertCanvasToImageCoordinates(Point canvasPoint)
        {
            // 获取背景图像的实际尺寸
            if (drawViewer.BackgroundImage != null)
            {
                double scaleX = drawViewer.BackgroundImage.PixelWidth / drawCanvas.ActualWidth;
                double scaleY = drawViewer.BackgroundImage.PixelHeight / drawCanvas.ActualHeight;

                return new Point(
                    canvasPoint.X * scaleX,
                    canvasPoint.Y * scaleY
                );
            }

            return canvasPoint;
        }

        /// <summary>
        /// 将Canvas半径转换为图像半径
        /// </summary>
        private double ConvertCanvasToImageRadius(double canvasRadius)
        {
            // 获取背景图像的实际尺寸
            if (drawViewer.BackgroundImage != null)
            {
                double scaleX = drawViewer.BackgroundImage.PixelWidth / drawCanvas.ActualWidth;
                double scaleY = drawViewer.BackgroundImage.PixelHeight / drawCanvas.ActualHeight;

                // 使用平均缩放比例
                double averageScale = (scaleX + scaleY) / 2.0;
                return canvasRadius * averageScale;
            }

            return canvasRadius;
        }

        /// <summary>
        /// 清除所有绘制内容
        /// </summary>
        private void ClearAllDrawings()
        {
            drawCanvas.Clear();
            _currentCircle = null;
            _circleRadius = 0;
            _circleCenter = new Point(0, 0);

            // 隐藏圆形信息显示
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                tb_CircleInfo.Visibility = Visibility.Collapsed;
                tb_CircleInfo.Text = "圆形: 未绘制";
            });

            System.Diagnostics.Debug.WriteLine("已清除所有绘制内容");
        }

        /// <summary>
        /// 获取当前圆形信息
        /// </summary>
        /// <returns>包含中心点和半径的信息</returns>
        public (Point center, double radius, Point imageCenter, double imageRadius) GetCurrentCircleInfo()
        {
            if (_currentCircle == null)
                return (new Point(), 0, new Point(), 0);

            // 获取画布坐标信息
            Point canvasCenter = new Point(
                Canvas.GetLeft(_currentCircle) + _currentCircle.Width / 2,
                Canvas.GetTop(_currentCircle) + _currentCircle.Height / 2
            );
            double canvasRadius = _currentCircle.Width / 2;

            // 转换为图像坐标
            Point imageCenter = ConvertCanvasToImageCoordinates(canvasCenter);
            double imageRadius = ConvertCanvasToImageRadius(canvasRadius);

            return (canvasCenter, canvasRadius, imageCenter, imageRadius);
        }

        /// <summary>
        /// 更新瑕疵信息显示
        /// </summary>
        /// <param name="result">测量结果</param>
        private void UpdateDefectInfoDisplay(MFDJC0_MeasureResult result)
        {
            try
            {
                if (result?.Defects == null)
                {
                    UpdateInspectionResultDisplay(0, false);

                    // 显示无瑕疵数据
                    DefectInfoExpander.Header = "瑕疵(0个)";
                    sp_DefectList.Children.Clear();
                    sp_DefectList.Children.Add(new TextBlock
                    {
                        Text = "暂无瑕疵数据",
                        FontSize = 10,
                        Foreground = Brushes.LightGray,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 20) // 增加底部边距确保滚动时显示完整
                    });
                    return;
                }

                var defects = result.Defects;
                int defectCount = defects.Count;
                bool isNg = false;
                for (int i = 0; i < defectCount; i++)
                {
                    if (!defects[i].IsOk)
                    {
                        isNg = true;
                        break;
                    }
                }
                UpdateInspectionResultDisplay(defectCount, defectCount != 0 && isNg);

                // 更新瑕疵计数
                DefectInfoExpander.Header = $"瑕疵({defectCount}个)";

                // 清空现有列表
                sp_DefectList.Children.Clear();

                if (defectCount == 0)
                {
                    sp_DefectList.Children.Add(new TextBlock
                    {
                        Text = "未检测到瑕疵",
                        FontSize = 10,
                        Foreground = Brushes.LightGreen,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                    return;
                }

                // 遍历所有瑕疵并显示
                for (int i = 0; i < defectCount; i++)
                {
                    var defect = defects[i];
                    CreateDefectInfoCard(defect, i + 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDefectInfoDisplay error: {ex.Message}");

                // 显示错误信息
                DefectInfoExpander.Header = "瑕疵(错误)";
                SetInspectionResult("异常", "结果加载失败", Brushes.Orange, new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), Brushes.Orange);
                sp_DefectList.Children.Clear();
                sp_DefectList.Children.Add(new TextBlock
                {
                    Text = "数据加载失败",
                    FontSize = 10,
                    Foreground = Brushes.Orange,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }

        /// <summary>
        /// 更新检测结果显示。
        /// </summary>
        private void UpdateInspectionResultDisplay(int defectCount, bool isNg)
        {
            SetInspectionResult(
                isNg ? "NG" : "OK",
                $"瑕疵: {defectCount}",
                isNg ? Brushes.Red : Brushes.LightGreen,
                new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                isNg ? Brushes.Red : Brushes.LightGreen);
        }

        /// <summary>
        /// 设置检测结果文本和样式。
        /// </summary>
        private void SetInspectionResult(string result, string detail, Brush foreground, Brush background, Brush borderBrush)
        {
            tb_InspectionResult.Text = result;
            tb_InspectionResult.Foreground = foreground;
            tb_InspectionResultDetail.Text = detail;
            tb_InspectionResultDetail.Foreground = foreground;
            InspectionResultBorder.Background = background;
            InspectionResultBorder.BorderBrush = borderBrush;
        }

        /// <summary>
        /// 处理绘图工具结束事件。
        /// </summary>
        private void OnDrawToolEnded(object sender, DrawToolEventArgs e)
        {
            if (e.DrawingToolType == DrawToolType.Ellipse)
            {
                // 处理椭圆绘制完成，但强制转换为圆形
                var centerPoint = e.CenterPoint;
                var startPoint = e.StartPoint;
                var endPoint = e.EndPoint;

                // 计算半径（取较小的半径作为圆的半径，确保是正圆）
                double radiusX = Math.Abs(endPoint.X - startPoint.X) / 2.0;
                double radiusY = Math.Abs(endPoint.Y - startPoint.Y) / 2.0;
                double circleRadius = Math.Min(radiusX, radiusY); // 使用较小的半径作为圆的半径

                // 转换为图像坐标
                var imageCenter = ConvertCanvasToImageCoordinates(centerPoint);
                var imageRadius = ConvertCanvasToImageRadius(circleRadius);

                // 更新圆形信息显示
                string info = $"圆形: 中心({imageCenter.X:F0}, {imageCenter.Y:F0}), 半径: {imageRadius:F1}";

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    tb_CircleInfo.Text = info;
                    tb_CircleInfo.Visibility = Visibility.Visible;
                });

                System.Diagnostics.Debug.WriteLine($"圆形绘制完成: {info}");
                System.Diagnostics.Debug.WriteLine(
                    $"Canvas坐标 - 中心: ({centerPoint.X:F1}, {centerPoint.Y:F1}), 起点: ({startPoint.X:F1}, {startPoint.Y:F1}), 终点: ({endPoint.X:F1}, {endPoint.Y:F1})");
                System.Diagnostics.Debug.WriteLine(
                    $"原始半径 - X轴: {radiusX:F1}, Y轴: {radiusY:F1}, 圆形半径: {circleRadius:F1}");

                var templateParams = new Dictionary<string, object>
                {
                    { "CenterX", imageCenter.X }, // 圆心X坐标（图像坐标系）
                    { "CenterY", imageCenter.Y }, // 圆心Y坐标（图像坐标系）
                    { "Radius", imageRadius }, // 圆的半径（图像坐标系）
                    { "TemplateType", "Circle" }, // 模板类型改为Circle
                    { "TemplateName", $"CircleTemplate_{DateTime.Now:yyyyMMdd_HHmmss}" } // 模板名称
                };

                try
                {
                    var result = _dataProcessorService?.SendCommand("CreateHalconTemplate", templateParams);
                    if (result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Halcon圆形模板创建成功: {result}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Halcon圆形模板创建失败：返回结果为空");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Halcon圆形模板创建异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 创建瑕疵信息卡片
        /// </summary>
        /// <param name="defect">瑕疵数据</param>
        /// <param name="index">瑕疵序号</param>
        private void CreateDefectInfoCard(DefectResult defect, int index)
        {
            // 创建瑕疵卡片容器
            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderBrush = defect.IsOk ? Brushes.LightGreen : Brushes.Orange,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 2, 0, 8), // 增加底部边距以确保最后元素可见
                Padding = new Thickness(8, 6, 8, 8)
            };

            var cardStackPanel = new StackPanel();

            // 瑕疵标题
            var titlePanel = new DockPanel();

            var titleText = new TextBlock
            {
                Text = $"缺陷编号:{defect.InstanceId}，{defect.Categories[defect.ClassId]}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            DockPanel.SetDock(titleText, Dock.Left);
            titlePanel.Children.Add(titleText);

            var statusText = new TextBlock
            {
                Text = defect.IsOk ? "正常" : "异常",
                FontSize = 9,
                Foreground = defect.IsOk ? Brushes.LightGreen : Brushes.Orange,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            titlePanel.Children.Add(statusText);

            cardStackPanel.Children.Add(titlePanel);

            // 参数信息网格
            var paramsGrid = new Grid();
            paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 面积特征
            var areaPanel = CreateParameterPanel("面积", FormatFeatureValue(defect.AreaFeature), "mm²");
            Grid.SetRow(areaPanel, 0);
            Grid.SetColumn(areaPanel, 0);
            paramsGrid.Children.Add(areaPanel);

            // 长度特征
            var lengthPanel = CreateParameterPanel("长度", FormatFeatureValue(defect.LengthFeature), "mm");
            Grid.SetRow(lengthPanel, 0);
            Grid.SetColumn(lengthPanel, 1);
            paramsGrid.Children.Add(lengthPanel);

            // 添加第二行
            paramsGrid.RowDefinitions.Add(new RowDefinition());
            paramsGrid.RowDefinitions.Add(new RowDefinition());

            // 宽度特征
            var widthPanel = CreateParameterPanel("宽度", FormatFeatureValue(defect.WidthFeature), "mm");
            Grid.SetRow(widthPanel, 1);
            Grid.SetColumn(widthPanel, 0);
            paramsGrid.Children.Add(widthPanel);

            // 深度特征
            var depthPanel = CreateParameterPanel("深度", FormatFeatureValue(defect.DepthFeature), "mm");
            Grid.SetRow(depthPanel, 1);
            Grid.SetColumn(depthPanel, 1);
            paramsGrid.Children.Add(depthPanel);

            cardStackPanel.Children.Add(paramsGrid);

            // 位置信息（可选，如果需要显示）
            if (!double.IsNegativeInfinity(defect.CenterRowFeature) && !double.IsNegativeInfinity(defect.CenterColFeature))
            {
                var positionText = new TextBlock
                {
                    Text = $"位置: ({defect.CenterColFeature:F1}, {defect.CenterRowFeature:F1})",
                    FontSize = 9,
                    Foreground = Brushes.LightGray,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                cardStackPanel.Children.Add(positionText);
            }

            cardBorder.Child = cardStackPanel;
            sp_DefectList.Children.Add(cardBorder);
        }

        /// <summary>
        /// 创建参数显示面板
        /// </summary>
        /// <param name="label">参数标签</param>
        /// <param name="value">参数值</param>
        /// <param name="unit">单位</param>
        /// <returns>参数面板</returns>
        private StackPanel CreateParameterPanel(string label, string value, string unit)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(2)
            };

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 9,
                Foreground = Brushes.LightGray
            };
            panel.Children.Add(labelText);

            var valuePanel = new DockPanel();
            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            DockPanel.SetDock(valueText, Dock.Left);
            valuePanel.Children.Add(valueText);

            if (!string.IsNullOrEmpty(unit))
            {
                var unitText = new TextBlock
                {
                    Text = unit,
                    FontSize = 8,
                    Foreground = Brushes.LightGray,
                    Margin = new Thickness(2, 0, 0, 0),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };
                valuePanel.Children.Add(unitText);
            }

            panel.Children.Add(valuePanel);
            return panel;
        }

        /// <summary>
        /// 格式化特征值显示
        /// </summary>
        /// <param name="value">特征值</param>
        /// <returns>格式化后的字符串</returns>
        private string FormatFeatureValue(double value)
        {
            if (double.IsNegativeInfinity(value) || double.IsPositiveInfinity(value) || double.IsNaN(value))
            {
                return "无效";
            }

            return Math.Abs(value) switch
            {
                // 根据数值大小选择合适的精度
                < 0.001 => value.ToString("F6"),
                < 0.1 => value.ToString("F4"),
                < 10 => value.ToString("F3"),
                < 1000 => value.ToString("F2"),
                _ => value.ToString("F1")
            };
        }

        /// <summary>
        /// 清空瑕疵信息显示
        /// </summary>
        public void ClearDefectInfo()
        {
            SetInspectionResult("待检测", "瑕疵: 0", Brushes.LightGray, new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), Brushes.Gray);
            DefectInfoExpander.Header = "瑕疵(0个)";
            sp_DefectList.Children.Clear();
            sp_DefectList.Children.Add(new TextBlock
            {
                Text = "暂无瑕疵数据",
                FontSize = 10,
                Foreground = Brushes.LightGray,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
        }

        /// <summary>
        /// 从JSON文件加载配置
        /// </summary>
        private void LoadConfigurationFromFile()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"配置文件不存在: {_configFilePath}");
                    // 创建默认配置
                    CreateDefaultConfiguration();
                    return;
                }

                var jsonContent = File.ReadAllText(_configFilePath, System.Text.Encoding.UTF8);
                _featureConfig = JsonConvert.DeserializeObject<FeatureConfig>(jsonContent);

                if (_featureConfig != null)
                {
                    // 将配置数据应用到UI控件
                    System.Diagnostics.Debug.WriteLine("配置文件加载成功");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {ex.Message}");
                // 如果加载失败，创建默认配置
                CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private void CreateDefaultConfiguration()
        {
            _featureConfig = new FeatureConfig
            {
                Locator = new LocatorConfig
                {
                    Name = "密封钉定位",
                    AlgName = "GetNailCenterAndOrbitMaskV2",
                    AlgParam = new List<Parameter>()
                },

                DefectList =
                [
                    new Defect
                    {
                        Id = 0,
                        Name = "翘钉",
                        AlgName = "GetWarpFeature",
                        AlgParam = new List<Parameter>
                        {
                            new Parameter { Name = "height_select", Describe = "NG阈值", Value = 75 }
                        }
                    },

                    new Defect
                    {
                        Id = 1,
                        Name = "轨迹偏移",
                        AlgName = "GetOrbitFeature",
                        AlgParam = new List<Parameter>
                        {
                            new Parameter { Name = "offset_select", Describe = "NG阈值", Value = 200 }
                        }
                    },

                    new Defect
                    {
                        Id = 2,
                        Name = "裂纹",
                        AlgName = "GetCrackFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "area_select", Describe = "面积NG阈值", Value = 0 },
                            new Parameter { Name = "length_select", Describe = "长度NG阈值", Value = 0 },
                            new Parameter { Name = "width_select", Describe = "宽度NG阈值", Value = 0 },
                            new Parameter { Name = "depth_select", Describe = "深度NG阈值", Value = 0 }
                        ]
                    },

                    new Defect
                    {
                        Id = 3,
                        Name = "失真",
                        AlgName = "GetDiameterFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "image_type", Describe = "使用灰度图:0; 使用高度图:1", Value = 1 },
                            new Parameter { Name = "select", Describe = "NG 阈值", Value = 0 },
                            new Parameter { Name = "area_lower_limit", Describe = "面积下限", Value = 1000 },
                            new Parameter { Name = "area_upper_limit", Describe = "面积上限", Value = 1000000000 }
                        ]
                    },

                    new Defect
                    {
                        Id = 4,
                        Name = "焊瘤",
                        AlgName = "GetDiameterFeature",
                        AlgParam =
                        [
                            new Parameter { Name = "image_type", Describe = "使用灰度图:0; 使用高度图:1", Value = 1 },
                            new Parameter { Name = "select", Describe = "NG 阈值", Value = 0},
                            new Parameter { Name = "area_lower_limit", Describe = "面积下限", Value = 0 }
                        ]
                    }
                ]
            };

            // 应用默认配置到UI
            System.Diagnostics.Debug.WriteLine("已创建默认配置");
        }


        /// <summary>
        /// 重置参数按钮点击事件
        /// </summary>
        private void btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 重新加载原始配置
                LoadConfigurationFromFile();
                System.Diagnostics.Debug.WriteLine("参数已重置到文件中的值");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置参数时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用参数按钮点击事件
        /// </summary>
        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateConfigurationFromDynamicUI();

                System.Diagnostics.Debug.WriteLine("参数已应用到算法");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用参数时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存参数按钮点击事件
        /// </summary>
        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 显示保存中状态
                ShowSavingStatus(true);

                // 验证所有参数
                if (!ValidateAllParameters())
                {
                    ShowSavingStatus(false);
                    return;
                }

                // 从UI收集数据并更新配置对象
                UpdateConfigurationFromDynamicUI();

                // 保存到JSON文件
                SaveConfigurationToFile();

                System.Diagnostics.Debug.WriteLine("参数已保存并应用");

                // 关闭抽屉
                DrawerLeft.IsOpen = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存参数时出错: {ex.Message}");
                ShowSaveErrorMessage(ex.Message);
            }
            finally
            {
                ShowSavingStatus(false);
            }
        }


        #region 反写Json文件
        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        private void SaveConfigurationToFile()
        {
            try
            {
                // 先从UI更新配置对象
                UpdateConfigurationFromDynamicUI();

                // 确保目录存在
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化设置
                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include
                };

                // 序列化并保存
                string jsonContent = JsonConvert.SerializeObject(_featureConfig, jsonSettings);
                File.WriteAllText(_configFilePath, jsonContent, System.Text.Encoding.UTF8);

                System.Diagnostics.Debug.WriteLine($"配置已保存到: {_configFilePath}");

                // 可选：显示保存成功提示
                ShowSaveSuccessMessage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
                ShowSaveErrorMessage(ex.Message);
                throw;
            }
        }


        /// <summary>
        /// 从控件获取值
        /// </summary>
        private object GetValueFromControl(FrameworkElement control, Parameter param)
        {
            switch (control)
            {
                case HandyControl.Controls.NumericUpDown numericUpDown:
                    // 根据参数的原始类型返回相应的值类型
                    if (param.DecimalPlaces == 0)
                    {
                        // 整数类型
                        return Convert.ToInt64(numericUpDown.Value);
                    }
                    else
                    {
                        // 浮点数类型
                        return numericUpDown.Value;
                    }

                case HandyControl.Controls.ComboBox comboBox:
                    return comboBox.SelectedIndex;

                case CheckBox checkBox:
                    return checkBox.IsChecked ?? false;

                default:
                    return param.Value; // 返回原值
            }
        }


        /// <summary>
        /// 验证并转换参数值
        /// </summary>
        private object ValidateAndConvertValue(object value, Parameter param)
        {
            try
            {
                switch (param.UiType?.ToLower())
                {
                    case "numeric":
                        double numValue = Convert.ToDouble(value);

                        // 验证范围
                        if (param.MinValue.HasValue && numValue < param.MinValue.Value)
                        {
                            numValue = param.MinValue.Value;
                            System.Diagnostics.Debug.WriteLine($"参数 {param.Name} 值 {value} 小于最小值，已调整为 {numValue}");
                        }

                        if (param.MaxValue.HasValue && numValue > param.MaxValue.Value)
                        {
                            numValue = param.MaxValue.Value;
                            System.Diagnostics.Debug.WriteLine($"参数 {param.Name} 值 {value} 大于最大值，已调整为 {numValue}");
                        }

                        // 根据小数位数决定返回类型
                        if (param.DecimalPlaces == 0)
                        {
                            return Convert.ToInt64(numValue);
                        }
                        else
                        {
                            return Math.Round(numValue, param.DecimalPlaces ?? 2);
                        }

                    case "combo":
                        return Convert.ToInt32(value);

                    case "checkbox":
                        return Convert.ToBoolean(value);

                    default:
                        return value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"验证参数 {param.Name} 时出错: {ex.Message}，使用原值");
                return param.Value;
            }
        }

        /// <summary>
        /// 验证所有参数
        /// </summary>
        private bool ValidateAllParameters()
        {
            try
            {
                foreach (var defect in _featureConfig.DefectList)
                {
                    foreach (var param in defect.AlgParam)
                    {
                        string controlKey = $"{defect.Name}_{param.Name}";
                        if (_dynamicControls.TryGetValue(controlKey, out var control))
                        {
                            // 验证控件值
                            if (!ValidateControlValue(control, param))
                            {
                                System.Diagnostics.Debug.WriteLine($"参数验证失败: {defect.Name}.{param.Name}");
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"参数验证时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 验证单个控件值
        /// </summary>
        private bool ValidateControlValue(FrameworkElement control, Parameter param)
        {
            try
            {
                switch (control)
                {
                    case HandyControl.Controls.NumericUpDown numericUpDown:
                        var value = numericUpDown.Value;
                        if (param.MinValue.HasValue && value < param.MinValue.Value)
                        {
                            ShowValidationError($"{param.Describe} 不能小于 {param.MinValue.Value}");
                            return false;
                        }
                        if (param.MaxValue.HasValue && value > param.MaxValue.Value)
                        {
                            ShowValidationError($"{param.Describe} 不能大于 {param.MaxValue.Value}");
                            return false;
                        }
                        break;

                    case HandyControl.Controls.ComboBox comboBox:
                        if (comboBox.SelectedIndex < 0)
                        {
                            ShowValidationError($"请选择 {param.Describe}");
                            return false;
                        }
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"验证控件值时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 显示保存状态
        /// </summary>
        private void ShowSavingStatus(bool isSaving)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isSaving)
                {
                    // 可以添加加载动画或禁用按钮
                    btn_Save.IsEnabled = false;
                    btn_Save.Content = "保存中...";
                }
                else
                {
                    btn_Save.IsEnabled = true;
                    btn_Save.Content = "保存";
                }
            });
        }

        /// <summary>
        /// 显示保存成功消息
        /// </summary>
        private void ShowSaveSuccessMessage()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Success("参数保存成功！");
            });
        }

        /// <summary>
        /// 显示保存错误消息
        /// </summary>
        private void ShowSaveErrorMessage(string error)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Error($"保存失败: {error}");
            });
        }

        /// <summary>
        /// 显示验证错误消息
        /// </summary>
        private void ShowValidationError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Warning(message);
            });
        }

        #endregion

        #region 自定义色域范围事件处理

        /// <summary>
        /// 启用自定义色域范围
        /// </summary>
        private void OnCustomColorRangeChecked(object sender, RoutedEventArgs e)
        {
            _useCustomColorRange = true;
            _customMinValue = (float)nud_MinValue.Value;
            _customMaxValue = (float)nud_MaxValue.Value;

            // 验证范围有效性
            if (_customMinValue >= _customMaxValue)
            {
                _customMaxValue = _customMinValue + 0.1f;
                nud_MaxValue.Value = _customMaxValue;
            }

            // 应用新的色域范围
            RefreshPalette();

            System.Diagnostics.Debug.WriteLine($"启用自定义色域范围: [{_customMinValue:F3}, {_customMaxValue:F3}]");
        }

        /// <summary>
        /// 禁用自定义色域范围
        /// </summary>
        private void OnCustomColorRangeUnchecked(object sender, RoutedEventArgs e)
        {
            _useCustomColorRange = false;

            // 恢复到数据范围
            RefreshPalette();

            System.Diagnostics.Debug.WriteLine("禁用自定义色域范围，恢复到数据范围");
        }

        /// <summary>
        /// 最小值改变事件
        /// </summary>
        private void OnMinValueChanged(object sender, EventArgs e)
        {
            if (!_useCustomColorRange) return;

            var minValue = (float)nud_MinValue.Value;
            var maxValue = (float)nud_MaxValue.Value;

            // 验证范围有效性
            if (minValue >= maxValue)
            {
                maxValue = minValue + 0.1f;
                nud_MaxValue.Value = maxValue;
            }

            _customMinValue = minValue;
            _customMaxValue = maxValue;

            // 实时更新调色板
            RefreshPalette();

            System.Diagnostics.Debug.WriteLine($"最小值更新: [{_customMinValue:F3}, {_customMaxValue:F3}]");
        }

        /// <summary>
        /// 最大值改变事件
        /// </summary>
        private void OnMaxValueChanged(object sender, EventArgs e)
        {
            if (!_useCustomColorRange) return;

            var minValue = (float)nud_MinValue.Value;
            var maxValue = (float)nud_MaxValue.Value;

            // 验证范围有效性
            if (minValue >= maxValue)
            {
                minValue = maxValue - 0.1f;
                nud_MinValue.Value = minValue;
            }

            _customMinValue = minValue;
            _customMaxValue = maxValue;

            // 实时更新调色板
            RefreshPalette();

            System.Diagnostics.Debug.WriteLine($"最大值更新: [{_customMinValue:F3}, {_customMaxValue:F3}]");
        }

        /// <summary>
        /// 更新数据范围显示
        /// </summary>
        private void UpdateDataRangeDisplay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (btn_ApplyDataRange != null)
                {
                    if (_dataMinValue != float.MaxValue && _dataMaxValue != float.MinValue)
                    {
                        btn_ApplyDataRange.ToolTip = $"应用当前数据范围: [{_dataMinValue:F3}, {_dataMaxValue:F3}] mm";
                        btn_ApplyDataRange.IsEnabled = true;
                        System.Diagnostics.Debug.WriteLine($"数据范围: [{_dataMinValue:F3}, {_dataMaxValue:F3}] mm");
                    }
                    else
                    {
                        btn_ApplyDataRange.ToolTip = "无数据范围可应用";
                        btn_ApplyDataRange.IsEnabled = false;
                    }
                }
            });
        }

        /// <summary>
        /// 设置数据范围用于显示
        /// </summary>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        private void SetDataRange(float minValue, float maxValue)
        {
            _dataMinValue = minValue;
            _dataMaxValue = maxValue;
            UpdateDataRangeDisplay();
        }

        /// <summary>
        /// 自动设置色域范围为数据范围
        /// </summary>
        public void AutoSetColorRangeToDataRange()
        {
            if (_dataMinValue != float.MaxValue && _dataMaxValue != float.MinValue)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    nud_MinValue.Value = _dataMinValue;
                    nud_MaxValue.Value = _dataMaxValue;

                    if (_useCustomColorRange)
                    {
                        _customMinValue = _dataMinValue;
                        _customMaxValue = _dataMaxValue;
                        RefreshPalette();
                    }
                });
            }
        }

        /// <summary>
        /// 应用数据范围按钮点击事件
        /// </summary>
        private void OnApplyDataRangeClick(object sender, RoutedEventArgs e)
        {
            if (_dataMinValue != float.MaxValue && _dataMaxValue != float.MinValue)
            {
                nud_MinValue.Value = _dataMinValue;
                nud_MaxValue.Value = _dataMaxValue;

                if (_useCustomColorRange)
                {
                    _customMinValue = _dataMinValue;
                    _customMaxValue = _dataMaxValue;
                    RefreshPalette();
                }

                // 如果未启用自定义范围，自动启用
                if (!_useCustomColorRange)
                {
                    chk_CustomColorRange.IsChecked = true;
                }

                System.Diagnostics.Debug.WriteLine($"应用数据范围: [{_dataMinValue:F3}, {_dataMaxValue:F3}]");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("无有效数据范围可应用");
            }
        }

        #endregion

    }
}

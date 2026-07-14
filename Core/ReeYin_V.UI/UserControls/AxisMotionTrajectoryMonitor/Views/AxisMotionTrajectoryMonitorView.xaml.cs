using Arction.Wpf.ChartingMVVM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// XY 轴运动轨迹监控控件，提供外部绑定入口并承接设置面板交互。
    /// </summary>
    public partial class AxisMotionTrajectoryMonitorView : UserControl, IDisposable
    {
        // 对外公开的依赖属性用于在业务界面中绑定轨迹集合、当前位置、目标点和坐标范围。
        public static readonly DependencyProperty TrajectoriesProperty =
            DependencyProperty.Register(
                nameof(Trajectories),
                typeof(IEnumerable<AxisTrajectoryItem>),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(null, OnTrajectoriesChanged));

        public static readonly DependencyProperty CurrentPositionProperty =
            DependencyProperty.Register(
                nameof(CurrentPosition),
                typeof(Point),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(new Point(double.NaN, double.NaN), OnCurrentPositionChanged));

        public static readonly DependencyProperty TargetPositionProperty =
            DependencyProperty.Register(
                nameof(TargetPosition),
                typeof(Point),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(new Point(double.NaN, double.NaN), OnTargetPositionChanged));

        public static readonly DependencyProperty ShowTrajectoryLinesProperty =
            DependencyProperty.Register(
                nameof(ShowTrajectoryLines),
                typeof(bool),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(true, OnShowTrajectoryLinesChanged));

        public static readonly DependencyProperty MonitorTitleProperty =
            DependencyProperty.Register(
                nameof(MonitorTitle),
                typeof(string),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata("XY 轴组运动监控", OnMonitorTitleChanged));

        public static readonly DependencyProperty XMinimumProperty =
            DependencyProperty.Register(
                nameof(XMinimum),
                typeof(double),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(AxisMotionTrajectoryMonitorModel.DefaultXMinimum, OnAxisRangeChanged));

        public static readonly DependencyProperty XMaximumProperty =
            DependencyProperty.Register(
                nameof(XMaximum),
                typeof(double),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(AxisMotionTrajectoryMonitorModel.DefaultXMaximum, OnAxisRangeChanged));

        public static readonly DependencyProperty YMinimumProperty =
            DependencyProperty.Register(
                nameof(YMinimum),
                typeof(double),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(AxisMotionTrajectoryMonitorModel.DefaultYMinimum, OnAxisRangeChanged));

        public static readonly DependencyProperty YMaximumProperty =
            DependencyProperty.Register(
                nameof(YMaximum),
                typeof(double),
                typeof(AxisMotionTrajectoryMonitorView),
                new PropertyMetadata(AxisMotionTrajectoryMonitorModel.DefaultYMaximum, OnAxisRangeChanged));

        private readonly AxisMotionTrajectoryMonitorModel _monitorModel = new();
        private readonly AxisMotionTrajectoryMonitorViewModel _viewModel;
        private AxisMotionTrajectoryMonitorAppearance _appearance = AxisMotionTrajectoryMonitorAppearance.Default;
        private bool _disposed;
        // 防止坐标范围规范化后回写依赖属性时再次触发 OnAxisRangeChanged 造成重复刷新。
        private bool _isApplyingAxisRange;

        public AxisMotionTrajectoryMonitorView()
        {
            _viewModel = new AxisMotionTrajectoryMonitorViewModel();
            InitializeComponent();
            DisplaySettingsPopup.DataContext = this;
        }

        public IEnumerable<AxisTrajectoryItem>? Trajectories
        {
            get => (IEnumerable<AxisTrajectoryItem>?)GetValue(TrajectoriesProperty);
            set => SetValue(TrajectoriesProperty, value);
        }

        public Point CurrentPosition
        {
            get => (Point)GetValue(CurrentPositionProperty);
            set => SetValue(CurrentPositionProperty, value);
        }

        public Point TargetPosition
        {
            get => (Point)GetValue(TargetPositionProperty);
            set => SetValue(TargetPositionProperty, value);
        }

        public string MonitorTitle
        {
            get => (string)GetValue(MonitorTitleProperty);
            set => SetValue(MonitorTitleProperty, value);
        }

        public bool ShowTrajectoryLines
        {
            get => (bool)GetValue(ShowTrajectoryLinesProperty);
            set => SetValue(ShowTrajectoryLinesProperty, value);
        }

        public double XMinimum
        {
            get => (double)GetValue(XMinimumProperty);
            set => SetValue(XMinimumProperty, value);
        }

        public double XMaximum
        {
            get => (double)GetValue(XMaximumProperty);
            set => SetValue(XMaximumProperty, value);
        }

        public double YMinimum
        {
            get => (double)GetValue(YMinimumProperty);
            set => SetValue(YMinimumProperty, value);
        }

        public double YMaximum
        {
            get => (double)GetValue(YMaximumProperty);
            set => SetValue(YMaximumProperty, value);
        }

        public AxisMotionTrajectoryMonitorViewModel ViewModel => _viewModel;

        private static void OnTrajectoriesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view)
            {
                return;
            }

            view._viewModel.SetTrajectories(e.NewValue as IEnumerable<AxisTrajectoryItem>);
        }

        private static void OnCurrentPositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view || e.NewValue is not Point currentPosition)
            {
                return;
            }

            view._viewModel.SetCurrentPosition(currentPosition);
        }

        private static void OnTargetPositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view || e.NewValue is not Point targetPosition)
            {
                return;
            }

            view._viewModel.SetTargetPosition(targetPosition);
        }

        private static void OnMonitorTitleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view)
            {
                return;
            }

            view._viewModel.SetMonitorTitle(e.NewValue as string);
        }

        private static void OnAxisRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view)
            {
                return;
            }

            view.ApplyAxisRange();
        }

        private static void OnShowTrajectoryLinesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not AxisMotionTrajectoryMonitorView view || e.NewValue is not bool showTrajectoryLines)
            {
                return;
            }

            view._viewModel.SetShowTrajectoryLines(showTrajectoryLines);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _disposed = false;
            ConfigureChart();
            _viewModel.SetAppearance(_appearance);
            _viewModel.SetMonitorTitle(MonitorTitle);
            _viewModel.SetShowTrajectoryLines(ShowTrajectoryLines);
            ApplyAxisRange();
            _viewModel.SetTrajectories(Trajectories);
            _viewModel.SetTargetPosition(TargetPosition);
            _viewModel.SetCurrentPosition(CurrentPosition);
            SyncFloatingSettingsEditors();
            SetSettingsMessage("可在此面板中调整坐标范围和轨迹显示颜色。", false);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _viewModel.SetTrajectories(null);
            _viewModel.SetTargetPosition(new Point(double.NaN, double.NaN));
            _viewModel.SetCurrentPosition(new Point(double.NaN, double.NaN));
        }

        private void ApplyAxisRange()
        {
            if (_isApplyingAxisRange)
            {
                return;
            }

            // 依赖属性先回写为规范化范围，再通知 ViewModel 刷新所有绑定数据。
            AxisMonitorBounds normalizedBounds = _monitorModel.CreateBounds(XMinimum, XMaximum, YMinimum, YMaximum);

            _isApplyingAxisRange = true;
            try
            {
                SetAxisRangePropertyIfNeeded(XMinimumProperty, XMinimum, normalizedBounds.XMinimum);
                SetAxisRangePropertyIfNeeded(XMaximumProperty, XMaximum, normalizedBounds.XMaximum);
                SetAxisRangePropertyIfNeeded(YMinimumProperty, YMinimum, normalizedBounds.YMinimum);
                SetAxisRangePropertyIfNeeded(YMaximumProperty, YMaximum, normalizedBounds.YMaximum);
            }
            finally
            {
                _isApplyingAxisRange = false;
            }

            _viewModel.SetAxisRange(
                normalizedBounds.XMinimum,
                normalizedBounds.XMaximum,
                normalizedBounds.YMinimum,
                normalizedBounds.YMaximum);

            SyncAxisEditorValues();
        }

        private void SetAxisRangePropertyIfNeeded(DependencyProperty property, double currentValue, double normalizedValue)
        {
            if (!double.IsFinite(currentValue) || Math.Abs(currentValue - normalizedValue) > 1E-9d)
            {
                SetCurrentValue(property, normalizedValue);
            }
        }

        private void ConfigureChart()
        {
            if (PART_Chart?.ViewXY == null)
            {
                return;
            }

            // 该控件作为监控面板使用，关闭用户缩放/平移以避免显示范围和设置面板不一致。
            var zoomPanOptions = PART_Chart.ViewXY.ZoomPanOptions;
            zoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction.None;
            zoomPanOptions.WheelZooming = WheelZooming.Off;
            zoomPanOptions.AxisWheelAction = AxisWheelAction.None;
            zoomPanOptions.RightToLeftZoomAction = RightToLeftZoomActionXY.Off;
            zoomPanOptions.MultiTouchZoomEnabled = false;
            zoomPanOptions.MultiTouchPanEnabled = false;
            // 关闭 1:1 强制比例，避免控件尺寸变化时图表为保持比例自动扩展坐标范围。
            // X/Y 刻度按实际图表宽高比例拉伸，左下角始终保持为配置的起始坐标。
            zoomPanOptions.AspectRatioOptions.AspectRatio = ViewAspectRatio.Off;
            zoomPanOptions.AspectRatioOptions.XAxisIndex = 0;
            zoomPanOptions.AspectRatioOptions.YAxisIndex = 0;

            if (PART_Chart.ViewXY.LegendBoxes.Count > 0)
            {
                PART_Chart.ViewXY.LegendBoxes[0].Visible = false;
            }

            PART_Chart.ViewXY.DataCursor.Visible = false;
        }

        private void DisplaySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SyncFloatingSettingsEditors();
            DisplaySettingsPopup.IsOpen = true;
        }

        private void CloseFloatingSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            DisplaySettingsPopup.IsOpen = false;
        }

        private void ApplyFloatingSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 设置面板的坐标和颜色需要全部验证通过后再一次性应用，避免半更新状态。
            if (!TryParseEditorDouble(XMinimumEditor, "X 最小值", out double xMinimum, out string errorMessage) ||
                !TryParseEditorDouble(XMaximumEditor, "X 最大值", out double xMaximum, out errorMessage) ||
                !TryParseEditorDouble(YMinimumEditor, "Y 最小值", out double yMinimum, out errorMessage) ||
                !TryParseEditorDouble(YMaximumEditor, "Y 最大值", out double yMaximum, out errorMessage))
            {
                SetSettingsMessage(errorMessage, true);
                return;
            }

            if (!TryCreateAppearanceFromEditors(out AxisMotionTrajectoryMonitorAppearance appearance, out errorMessage))
            {
                SetSettingsMessage(errorMessage, true);
                return;
            }

            AxisMonitorBounds normalizedBounds = _monitorModel.CreateBounds(xMinimum, xMaximum, yMinimum, yMaximum);
            ApplyNormalizedBounds(normalizedBounds);
            ApplyAppearance(appearance);
            SetSettingsMessage(
                $"已应用显示设置：X {normalizedBounds.XMinimum:F0} ~ {normalizedBounds.XMaximum:F0}，Y {normalizedBounds.YMinimum:F0} ~ {normalizedBounds.YMaximum:F0}。",
                false);
        }

        private void ResetFloatingSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AxisMonitorBounds defaultBounds = _monitorModel.CreateBounds(
                AxisMotionTrajectoryMonitorModel.DefaultXMinimum,
                AxisMotionTrajectoryMonitorModel.DefaultXMaximum,
                AxisMotionTrajectoryMonitorModel.DefaultYMinimum,
                AxisMotionTrajectoryMonitorModel.DefaultYMaximum);

            ApplyNormalizedBounds(defaultBounds);
            ApplyAppearance(AxisMotionTrajectoryMonitorAppearance.Default);
            SetSettingsMessage("已恢复默认坐标范围和轨迹颜色。", false);
        }

        private void ApplyNormalizedBounds(AxisMonitorBounds bounds)
        {
            // SetCurrentValue 保留外部 Binding，不会像 SetValue 一样覆盖绑定表达式。
            _isApplyingAxisRange = true;
            try
            {
                SetCurrentValue(XMinimumProperty, bounds.XMinimum);
                SetCurrentValue(XMaximumProperty, bounds.XMaximum);
                SetCurrentValue(YMinimumProperty, bounds.YMinimum);
                SetCurrentValue(YMaximumProperty, bounds.YMaximum);
            }
            finally
            {
                _isApplyingAxisRange = false;
            }

            ApplyAxisRange();
        }

        private void ApplyAppearance(AxisMotionTrajectoryMonitorAppearance appearance)
        {
            _appearance = appearance;
            _viewModel.SetAppearance(_appearance);
            SyncAppearanceEditorValues();
        }

        private void SyncFloatingSettingsEditors()
        {
            SyncAxisEditorValues();
            SyncAppearanceEditorValues();
        }

        private void SyncAxisEditorValues()
        {
            if (XMinimumEditor == null || XMaximumEditor == null || YMinimumEditor == null || YMaximumEditor == null)
            {
                return;
            }

            // 设置面板统一使用 InvariantCulture，避免中英文系统小数点格式差异影响复制和解析。
            XMinimumEditor.Text = XMinimum.ToString("F2", CultureInfo.InvariantCulture);
            XMaximumEditor.Text = XMaximum.ToString("F2", CultureInfo.InvariantCulture);
            YMinimumEditor.Text = YMinimum.ToString("F2", CultureInfo.InvariantCulture);
            YMaximumEditor.Text = YMaximum.ToString("F2", CultureInfo.InvariantCulture);
        }

        private void SyncAppearanceEditorValues()
        {
            if (PendingTrajectoryColorEditor == null ||
                RunningTrajectoryColorEditor == null ||
                CompletedTrajectoryColorEditor == null ||
                MotionTraceColorEditor == null ||
                CurrentMarkerColorEditor == null ||
                TargetMarkerColorEditor == null)
            {
                return;
            }

            PendingTrajectoryColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.PendingTrajectoryColor);
            RunningTrajectoryColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.RunningTrajectoryColor);
            CompletedTrajectoryColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.CompletedTrajectoryColor);
            MotionTraceColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.MotionTraceColor);
            CurrentMarkerColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.CurrentMarkerColor);
            TargetMarkerColorEditor.Text = AxisMotionTrajectoryMonitorAppearance.ToHexRgb(_appearance.TargetMarkerColor);
        }

        private void SetSettingsMessage(string message, bool isError)
        {
            if (SettingsMessageTextBlock == null)
            {
                return;
            }

            SettingsMessageTextBlock.Text = message;
            SettingsMessageTextBlock.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(0xC2, 0x41, 0x0C))
                : new SolidColorBrush(Color.FromRgb(0x4F, 0x62, 0x74));
        }

        private static bool TryParseEditorDouble(TextBox? editor, string displayName, out double value, out string errorMessage)
        {
            value = 0d;
            errorMessage = string.Empty;

            if (editor == null)
            {
                errorMessage = $"{displayName}输入框未初始化。";
                return false;
            }

            string text = editor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                errorMessage = $"{displayName}不能为空。";
                return false;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                if (double.IsFinite(value))
                {
                    return true;
                }
            }

            errorMessage = $"{displayName}请输入有效数字。";
            return false;
        }

        private bool TryCreateAppearanceFromEditors(out AxisMotionTrajectoryMonitorAppearance appearance, out string errorMessage)
        {
            appearance = _appearance;
            errorMessage = string.Empty;

            if (!TryParseColorText(PendingTrajectoryColorEditor, "待执行轨迹颜色", out Color pendingColor, out errorMessage) ||
                !TryParseColorText(RunningTrajectoryColorEditor, "执行中轨迹颜色", out Color runningColor, out errorMessage) ||
                !TryParseColorText(CompletedTrajectoryColorEditor, "已完成轨迹颜色", out Color completedColor, out errorMessage) ||
                !TryParseColorText(MotionTraceColorEditor, "移动轨迹颜色", out Color motionTraceColor, out errorMessage) ||
                !TryParseColorText(CurrentMarkerColorEditor, "当前位置颜色", out Color currentMarkerColor, out errorMessage) ||
                !TryParseColorText(TargetMarkerColorEditor, "目标位置颜色", out Color targetMarkerColor, out errorMessage))
            {
                return false;
            }

            appearance = _appearance with
            {
                PendingTrajectoryColor = pendingColor,
                RunningTrajectoryColor = runningColor,
                CompletedTrajectoryColor = completedColor,
                MotionTraceColor = motionTraceColor,
                CurrentMarkerColor = currentMarkerColor,
                TargetMarkerColor = targetMarkerColor
            };

            return true;
        }

        private static bool TryParseColorText(TextBox? editor, string displayName, out Color color, out string errorMessage)
        {
            color = Colors.Transparent;
            errorMessage = string.Empty;

            if (editor == null)
            {
                errorMessage = $"{displayName}输入框未初始化。";
                return false;
            }

            string text = editor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                errorMessage = $"{displayName}不能为空。";
                return false;
            }

            try
            {
                object? converted = ColorConverter.ConvertFromString(text);
                if (converted is Color parsedColor)
                {
                    color = parsedColor;
                    return true;
                }
            }
            catch
            {
            }

            errorMessage = $"{displayName}格式无效，请使用 #RRGGBB 或 #AARRGGBB。";
            return false;
        }
    }
}

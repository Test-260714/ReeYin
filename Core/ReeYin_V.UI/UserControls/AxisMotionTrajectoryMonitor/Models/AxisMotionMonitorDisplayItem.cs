using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// 轨迹监控画布中的线段绘制数据，供 XAML 直接绑定使用。
    /// </summary>
    public sealed class AxisMonitorLineVisual
    {
        public double X1 { get; init; }

        public double Y1 { get; init; }

        public double X2 { get; init; }

        public double Y2 { get; init; }

        public Brush Stroke { get; init; } = Brushes.Transparent;

        public double StrokeThickness { get; init; } = 1d;

        public DoubleCollection? StrokeDashArray { get; init; }

        public double Opacity { get; init; } = 1d;
    }

    /// <summary>
    /// 坐标轴刻度文本的显示位置与内容。
    /// </summary>
    public sealed class AxisMonitorLabelVisual
    {
        public double Left { get; init; }

        public double Top { get; init; }

        public double Width { get; init; } = 64d;

        public string Text { get; init; } = string.Empty;

        public TextAlignment TextAlignment { get; init; } = TextAlignment.Center;
    }

    /// <summary>
    /// 轨迹、运动痕迹和模拟图形的折线路径显示数据。
    /// </summary>
    public sealed class AxisMonitorPathVisual
    {
        public PointCollection Points { get; init; } = new();

        public Brush Stroke { get; init; } = Brushes.Transparent;

        public double StrokeThickness { get; init; } = 1d;

        public DoubleCollection? StrokeDashArray { get; init; }

        public double Opacity { get; init; } = 1d;
    }

    /// <summary>
    /// 监控区域的世界坐标范围。
    /// </summary>
    public readonly record struct AxisMonitorBounds(double XMinimum, double XMaximum, double YMinimum, double YMaximum)
    {
        public double XSpan => XMaximum - XMinimum;

        public double YSpan => YMaximum - YMinimum;
    }
}

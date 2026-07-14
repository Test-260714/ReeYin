using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// 轨迹监控控件的颜色配置，集中管理画布层和图表层的视觉样式。
    /// </summary>
    public sealed record AxisMotionTrajectoryMonitorAppearance(
        Color PendingTrajectoryColor,
        Color RunningTrajectoryColor,
        Color CompletedTrajectoryColor,
        Color MotionTraceColor,
        Color CurrentMarkerColor,
        Color TargetMarkerColor,
        Color SimulatedCircleColor,
        Color AxisGridColor,
        Color AxisBoundaryColor,
        Color AxisLineColor,
        Color AxisLabelColor,
        Color AxisTitleColor)
    {
        /// <summary>
        /// 默认配色方案，兼顾浅色背景下的轨迹、网格和标记可读性。
        /// </summary>
        public static AxisMotionTrajectoryMonitorAppearance Default { get; } = new(
            Color.FromRgb(0x3B, 0x82, 0xF6),
            Color.FromRgb(0xF5, 0x9E, 0x0B),
            Color.FromRgb(0x10, 0xB9, 0x81),
            Color.FromRgb(0x06, 0xB6, 0xD4),
            Color.FromRgb(0xF9, 0x73, 0x16),
            Color.FromRgb(0xEA, 0xB3, 0x08),
            Color.FromRgb(0xD9, 0x46, 0xEF),
            Color.FromArgb(0x70, 0x3F, 0x52, 0x63),
            Color.FromArgb(0xCC, 0x2E, 0x40, 0x50),
            Color.FromRgb(0x2E, 0x40, 0x50),
            Color.FromRgb(0x2E, 0x40, 0x50),
            Color.FromRgb(0x2E, 0x40, 0x50));

        /// <summary>
        /// 将颜色转换为设置面板中使用的 #RRGGBB 文本。
        /// </summary>
        public static string ToHexRgb(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}

#nullable enable

using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed record DefectMapAppearance(
        Color MinorColor,
        Color WarningColor,
        Color CriticalColor,
        Color SelectedColor,
        Color AxisLineColor,
        Color AxisLabelColor,
        Color AxisGridColor,
        Color ChartBackgroundColor,
        double MinorSize,
        double WarningSize,
        double CriticalSize,
        double SelectedSize)
    {
        public static DefectMapAppearance Default { get; } = new(
            Color.FromRgb(0x22, 0xC5, 0x5E),
            Color.FromRgb(0xF5, 0x9E, 0x0B),
            Color.FromRgb(0xEF, 0x44, 0x44),
            Color.FromRgb(0x25, 0x63, 0xEB),
            Color.FromRgb(0x2E, 0x40, 0x50),
            Color.FromRgb(0x3F, 0x52, 0x63),
            Color.FromArgb(0x70, 0x8E, 0xA8, 0xBA),
            Color.FromRgb(0xF7, 0xFA, 0xFD),
            8d,
            11d,
            15d,
            22d);

        public Color GetSeverityColor(DefectMapSeverity severity)
        {
            return severity switch
            {
                DefectMapSeverity.Critical => CriticalColor,
                DefectMapSeverity.Warning => WarningColor,
                _ => MinorColor
            };
        }

        public double GetSeveritySize(DefectMapSeverity severity)
        {
            return severity switch
            {
                DefectMapSeverity.Critical => CriticalSize,
                DefectMapSeverity.Warning => WarningSize,
                _ => MinorSize
            };
        }
    }
}

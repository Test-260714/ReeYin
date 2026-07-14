using System.Windows.Media;

namespace FileTool.BRJReportOutput.Models
{
    public sealed class BrjDefectMapPoint
    {
        public BrjDefectRecord Source { get; init; } = new();

        public string DefectKey { get; init; } = string.Empty;

        public string DefectType { get; init; } = string.Empty;

        public string MarkerKind { get; init; } = "Circle";

        public double XPercent { get; init; }

        public double MeterValue { get; init; }

        public Brush Fill { get; init; } = Brushes.Red;

        public Brush Stroke { get; init; } = Brushes.Black;
    }
}

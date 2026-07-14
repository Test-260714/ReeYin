using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Custom.DefectOverview.Models
{
    public enum BandMapPointDisplayMode
    {
        Precise,
        Aggregate
    }

    public sealed class BandMapDefectSeed
    {
        public string DefectKey { get; init; }
        public int ResultIndex { get; init; }
        public int SourceWidth { get; init; }
        public int SourceHeight { get; init; }
        public double CenterX { get; init; }
        public double CenterY { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public double Angle { get; init; }
        public bool HasSegmentation { get; init; }
        public string ModelTypeText { get; init; }
        public ImageSource ThumbnailImage { get; init; }
        public ImageSource PreviewImage { get; init; }
    }

    public sealed class BandMapPathInput
    {
        public string PathName { get; init; }
        public string ResultText { get; init; }
        public int DefectCount { get; init; }
        public double LaneWidth { get; init; }
        public double PixelEquivalentX { get; init; }
        public double PixelEquivalentY { get; init; }
        public double EdgeCalibrationX { get; init; }
        public bool OccupiesFullWidth { get; init; }
        public bool SaveLocalDefectImages { get; init; }
        public IReadOnlyList<Result> Results { get; init; } = Array.Empty<Result>();
        public IReadOnlyList<BandMapDefectSeed> Defects { get; init; } = Array.Empty<BandMapDefectSeed>();
    }

    public sealed class BandMapFrameInput
    {
        public string FrameIdText { get; init; }
        public long CycleId { get; init; }
        public BandMapPathInput Left { get; init; }
        public BandMapPathInput Right { get; init; }
    }

    public sealed class BandMapPointItem
    {
        public string DefectKey { get; init; }
        public string LegendKey { get; init; }
        public string ClassName { get; init; }
        public string MeterText { get; init; }
        public string PositionText { get; init; }
        public string PercentText { get; init; }
        public string PhysicalPositionText { get; init; }
        public string SlitText { get; init; }
        public string PathText { get; init; }
        public string FrameIdText { get; init; }
        public int ResultIndex { get; init; }
        public double XPercent { get; init; }
        public double MeterValue { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public double Size { get; init; }
        public string MarkerKind { get; init; }
        public Brush Fill { get; init; }
        public Brush Stroke { get; init; }
        public string ToolTip { get; init; }
        public bool IsSelected { get; init; }
    }

    public sealed class BandMapAxisTickItem
    {
        public double X1 { get; init; }
        public double Y1 { get; init; }
        public double X2 { get; init; }
        public double Y2 { get; init; }
        public double LabelX { get; init; }
        public double LabelY { get; init; }
        public string Label { get; init; }
        public Brush Stroke { get; init; }
        public double StrokeThickness { get; init; }
    }

    public sealed class BandMapRecentDefectItem
    {
        public string DefectKey { get; init; }
        public string LegendKey { get; init; }
        public Brush AccentBrush { get; init; }
        public string FrameIdText { get; init; }
        public string MeterText { get; init; }
        public string PositionText { get; init; }
        public string PercentText { get; init; }
        public string PhysicalPositionText { get; init; }
        public string SlitText { get; init; }
        public string PathText { get; init; }
        public string ClassName { get; init; }
        public string ConfidenceText { get; init; }
        public bool IsCrossSlit { get; init; }
        public bool IsSelected { get; init; }
    }

    public sealed class BandMapWallItem
    {
        public string DefectKey { get; init; }
        public string LegendKey { get; init; }
        public ImageSource WallImage { get; init; }
        public ImageSource ThumbnailImage { get; init; }
        public ImageSource PreviewImage { get; init; }
        public Brush AccentBrush { get; init; }
        public Brush PathBrush { get; init; }
        public string FrameIdText { get; init; }
        public string MeterText { get; init; }
        public string PositionText { get; init; }
        public string PercentText { get; init; }
        public string PhysicalPositionText { get; init; }
        public string SlitText { get; init; }
        public string PathText { get; init; }
        public string PositionSummaryText { get; init; }
        public string PathShortText { get; init; }
        public string ClassName { get; init; }
        public string ConfidenceText { get; init; }
        public string SizeText { get; init; }
        public string GeometryText { get; init; }
        public string SourceImageText { get; init; }
        public string ModelTypeText { get; init; }
        public string CoordinateSourceText { get; init; }
        public string SummaryText { get; init; }
        public string DetailText { get; init; }
        public bool IsCrossSlit { get; init; }
        public bool IsSelected { get; init; }
    }

    public sealed class BandMapReportRow
    {
        public string BatchNumberText { get; init; }
        public string FrameIdText { get; init; }
        public long FrameSequence { get; init; }
        public string PathName { get; init; }
        public string ClassName { get; init; }
        public double MeterValue { get; init; }
        public string MeterText { get; init; }
        public string PositionText { get; init; }
        public string PercentText { get; init; }
        public string PhysicalPositionText { get; init; }
        public string SlitText { get; init; }
        public string ConfidenceText { get; init; }
        public string SizeText { get; init; }
        public string GeometryText { get; init; }
        public string SourceImageText { get; init; }
        public string ModelTypeText { get; init; }
        public string CoordinateSourceText { get; init; }
        public bool IsCrossSlit { get; init; }
    }

    public sealed class BandMapGuideLineItem
    {
        public double X1 { get; init; }
        public double Y1 { get; init; }
        public double X2 { get; init; }
        public double Y2 { get; init; }
        public Brush Stroke { get; init; }
        public double StrokeThickness { get; init; }
    }

    public sealed class BandMapSlittingSettings
    {
        public bool IsEnabled { get; init; }
        public double KnifeSpacingMillimeters { get; init; }
        public double FirstCutOffsetMillimeters { get; init; }
        public double? StripWidthMillimeters { get; init; }
        public int SlitCount { get; init; }
        public string SummaryText { get; init; } = "切刀分切未启用";
    }

    public sealed class BandMapCameraSnapshotItem
    {
        public int SortIndex { get; init; }
        public string CameraKey { get; init; }
        public string CameraName { get; init; }
        public string StatusText { get; init; } = "暂无图像";
        public string StatusState { get; init; } = "Offline";
        public string LastRefreshText { get; init; } = "暂无缓存";
        public ImageSource SnapshotImage { get; init; }
    }

    public sealed class BandMapStateSnapshot
    {
        public double CanvasWidth { get; init; }
        public double CanvasHeight { get; init; }
        public double PlotLeft { get; init; }
        public double PlotTop { get; init; }
        public double PlotWidth { get; init; }
        public double PlotHeight { get; init; }
        public double PlotBottom => PlotTop + PlotHeight;
        public double CumulativeMeters { get; init; }
        public double FrameSpanMillimeters { get; init; }
        public double WindowMeters { get; init; }
        public double ViewportStartMeters { get; init; }
        public double ViewportMaxStartMeters { get; init; }
        public int BatchNumber { get; init; }
        public string BatchNumberText { get; init; } = string.Empty;
        public DateTime BatchStartedLocalTime { get; init; }
        public int TotalFrames { get; init; }
        public int OkFrames { get; init; }
        public int NgFrames { get; init; }
        public int RecentNgFrameCount { get; init; }
        public int RecentNgWindowSize { get; init; } = 20;
        public double CurrentSpeedMetersPerMinute { get; init; }
        public string CurrentStatusCode { get; init; } = "Idle";
        public string CurrentStatusText { get; init; } = "未运行";
        public string ReportSuggestedFileName { get; init; } = "DefectOverview_Report.csv";
        public DateTime LastFrameUtc { get; init; }
        public string LastFrameIdText { get; init; }
        public string RangeSummary { get; init; }
        public bool IsSlittingEnabled { get; init; }
        public double KnifeSpacingMillimeters { get; init; }
        public double FirstCutOffsetMillimeters { get; init; }
        public double? StripWidthMillimeters { get; init; }
        public int SlitCount { get; init; }
        public string SlittingSummaryText { get; init; } = "切刀分切未启用";
        public bool ShowPathStatusBadges { get; init; }
        public string Path1Header { get; init; }
        public string Path2Header { get; init; }
        public string Path1Result { get; init; }
        public string Path2Result { get; init; }
        public int Path1DefectCount { get; init; }
        public int Path2DefectCount { get; init; }
        public int TotalDefectCount { get; init; }
        public string SelectedDefectKey { get; init; }
        public long SelectionVersion { get; init; }
        public IReadOnlyList<BandMapGuideLineItem> GuideLines { get; init; } = Array.Empty<BandMapGuideLineItem>();
        public IReadOnlyList<BandMapPointItem> DefectPoints { get; init; } = Array.Empty<BandMapPointItem>();
        public IReadOnlyList<BandMapAxisTickItem> XAxisTicks { get; init; } = Array.Empty<BandMapAxisTickItem>();
        public IReadOnlyList<BandMapAxisTickItem> YAxisTicks { get; init; } = Array.Empty<BandMapAxisTickItem>();
        public IReadOnlyList<BandMapRecentDefectItem> RecentDefects { get; init; } = Array.Empty<BandMapRecentDefectItem>();
        public IReadOnlyList<BandMapWallItem> WallItems { get; init; } = Array.Empty<BandMapWallItem>();
        public IReadOnlyList<BandMapReportRow> ReportRows { get; init; } = Array.Empty<BandMapReportRow>();
        public IReadOnlyList<BandMapCameraSnapshotItem> CameraSnapshots { get; init; } = Array.Empty<BandMapCameraSnapshotItem>();
        public BandMapWallItem SelectedWallItem { get; init; }
        public string WallSummaryText { get; init; } = "最近缺陷 0 条";
        public int WallCurrentPage { get; init; } = 1;
        public int WallTotalPages { get; init; } = 1;
        public bool IsWallPinnedToLatestPage { get; init; }
    }
}

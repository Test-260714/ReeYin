using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;

namespace Custom.DefectOverview.Models
{
    public enum DefectOverviewFrameLayout
    {
        SinglePath,
        DualPath
    }

    public enum DefectOverviewPathRole
    {
        Unknown,
        Left,
        Right
    }

    [Serializable]
    public sealed class DefectOverviewPathPacket
    {
        public string SourceName { get; init; } = string.Empty;

        public string FrameKey { get; init; } = string.Empty;

        public string FrameIdText { get; init; } = string.Empty;

        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        public DefectOverviewFrameLayout FrameLayout { get; init; } = DefectOverviewFrameLayout.DualPath;

        public DefectOverviewPathRole PathRole { get; init; } = DefectOverviewPathRole.Unknown;

        public string PathName { get; init; } = string.Empty;

        public HImage PathImage { get; init; }

        public HImage OriginalImage { get; init; }

        public bool ApplyPostProcess { get; init; } = true;

        public bool SaveLocalDefectImages { get; init; }

        public bool? IsNg { get; init; }

        public IReadOnlyList<Result> Results { get; init; } = Array.Empty<Result>();

        public double? LaneWidth { get; init; }

        public double? PixelEquivalentX { get; init; }

        public double? PixelEquivalentY { get; init; }

        public double? EdgeCalibrationX { get; init; }

        public string SchemeFilePath { get; init; } = string.Empty;
    }
}

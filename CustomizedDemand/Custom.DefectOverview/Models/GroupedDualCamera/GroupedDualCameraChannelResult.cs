using Custom.DefectOverview.Models.Common;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;

namespace Custom.DefectOverview.Models.GroupedDualCamera
{
    /// <summary>
    /// 多相机发布中的单通道结果，例如 01-L 或 02-R。
    /// </summary>
    public sealed class GroupedDualCameraChannelResult
    {
        public string GroupKey { get; init; } = string.Empty;

        public string GroupName { get; init; } = string.Empty;

        public WidthSide Side { get; init; } = WidthSide.Unknown;

        public string DisplayName { get; init; } = string.Empty;

        public string SourceName { get; init; } = string.Empty;

        public IReadOnlyList<Result> Results { get; init; } = Array.Empty<Result>();

        public int DefectCount => Results?.Count ?? 0;

        public bool IsNg => DefectCount > 0;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.DefectOverview.Models.GroupedDualCamera
{
    /// <summary>
    /// 一帧多组双相机总览结果，负责承载统一后的通道集合和总判定摘要。
    /// </summary>
    public sealed class GroupedDualCameraFrame
    {
        public string FrameKey { get; init; } = string.Empty;

        public string FrameIdText { get; init; } = string.Empty;

        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        public List<GroupedDualCameraChannelResult> Channels { get; } = new();

        public int TotalDefectCount => Channels.Sum(channel => channel.DefectCount);

        public bool IsNg => Channels.Any(channel => channel.IsNg);
    }
}
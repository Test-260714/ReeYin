#nullable enable

using Prism.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Events
{
    public class DefectBatchReportSyncEvent : PubSubEvent<DefectBatchReportSyncRequest>
    {
    }

    public sealed class DefectBatchReportSyncRequest
    {
        public string SN { get; init; } = string.Empty;

        public string BatchName { get; init; } = string.Empty;

        public double DetectMeters { get; init; }

        public bool IsRollCompleted { get; init; }

        public DateTime BatchStartedTime { get; init; }

        public DateTime? BatchEndedTime { get; init; }

        public DateTime SyncTime { get; init; } = DateTime.Now;

        public long SnapshotVersion { get; init; }

        public int TotalFrames { get; init; }

        public int OkFrames { get; init; }

        public int NgFrames { get; init; }

        public double ProductWidthMm { get; init; }

        public string OperatorName { get; init; } = string.Empty;

        public string ShiftName { get; init; } = string.Empty;

        public string ProductModel { get; init; } = string.Empty;

        public int CameraCount { get; init; }

        public string ResolutionX { get; init; } = string.Empty;

        public string ResolutionY { get; init; } = string.Empty;

        public string ImageWidth { get; init; } = string.Empty;

        public string ImageHeight { get; init; } = string.Empty;

        public string SlitLeftCoordinates { get; init; } = string.Empty;

        public string SlitRightCoordinates { get; init; } = string.Empty;

        public List<DefectBatchReportItem> Defects { get; init; } = new();

        public TaskCompletionSource<DefectBatchReportSyncResult>? Completion { get; init; }
    }

    public sealed class DefectBatchReportItem
    {
        public string FrameKey { get; init; } = string.Empty;

        public int DefectIndex { get; init; }

        public int CameraIndex { get; init; }

        public string CameraName { get; init; } = string.Empty;

        public int SegmentIndex { get; init; }

        public int SlitIndex { get; init; }

        public string DefectType { get; init; } = string.Empty;

        public double AreaMm2 { get; init; }

        public double DiameterMm { get; init; }

        public double PositionXMm { get; init; }

        public double PositionYM { get; init; }

        public string DefectImagePath { get; init; } = string.Empty;

        public DateTime CreateTime { get; init; } = DateTime.Now;
    }

    public sealed class DefectBatchReportSyncResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public string SN { get; init; } = string.Empty;

        public int DefectCount { get; init; }

        public long SnapshotVersion { get; init; }
    }
}

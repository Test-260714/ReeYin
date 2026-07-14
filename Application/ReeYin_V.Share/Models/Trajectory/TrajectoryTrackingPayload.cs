using System;
using System.Collections.Generic;

namespace ReeYin_V.Share.Models.Trajectory
{
    public static class TrajectoryTrackingEventNames
    {
        public const string Start = "WaferTrajectoryTrackingStart";
        public const string Target = "WaferTrajectoryTrackingTarget";
        public const string Progress = "WaferTrajectoryTrackingProgress";
        public const string Stop = "WaferTrajectoryTrackingStop";
    }

    [Serializable]
    public sealed class TrajectoryTrackingStartPayload
    {
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");

        public int SourceSerial { get; set; }

        public bool IsPointTrajectory { get; set; }

        public IReadOnlyList<TrajectoryItem> Trajectories { get; set; } = Array.Empty<TrajectoryItem>();

        public TrajectoryPoint? InitialTarget { get; set; }

        public int ActualPositionPollIntervalMs { get; set; } = 100;

        public bool StartActualPositionMonitor { get; set; } = true;
    }

    [Serializable]
    public sealed class TrajectoryTrackingTargetPayload
    {
        public string RunId { get; set; } = string.Empty;

        public int TrajectoryIndex { get; set; }

        public TrajectoryPoint Target { get; set; }
    }

    [Serializable]
    public sealed class TrajectoryTrackingProgressPayload
    {
        public string RunId { get; set; } = string.Empty;

        public int RunningIndex { get; set; } = -1;

        public int CompletedIndex { get; set; } = -1;

        public bool IsFinished { get; set; }
    }

    [Serializable]
    public sealed class TrajectoryTrackingStopPayload
    {
        public string RunId { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}

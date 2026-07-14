using System;
using System.Collections.Generic;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 轨迹执行过程对监控界面发布的事件名。
    /// </summary>
    public static class WaferTrajectoryTrackingEventNames
    {
        public const string Start = "WaferTrajectoryTrackingStart";
        public const string Target = "WaferTrajectoryTrackingTarget";
        public const string Progress = "WaferTrajectoryTrackingProgress";
        public const string Stop = "WaferTrajectoryTrackingStop";
    }

    /// <summary>
    /// 一次轨迹监控会话的初始快照，包含全部规划轨迹和首个目标点。
    /// </summary>
    public sealed class WaferTrajectoryTrackingStartPayload
    {
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");

        public int SourceSerial { get; set; }

        public bool IsPointTrajectory { get; set; }

        public IReadOnlyList<WaferTrajectoryTrackingItem> Trajectories { get; set; } =
            Array.Empty<WaferTrajectoryTrackingItem>();

        public WaferTrajectoryTrackingPoint? InitialTarget { get; set; }

        public int ActualPositionPollIntervalMs { get; set; } = 100;

        public bool StartActualPositionMonitor { get; set; } = true;
    }

    /// <summary>
    /// 监控控件绘制的单条轨迹，线段通常包含两个点，点位通常包含一个点。
    /// </summary>
    public sealed class WaferTrajectoryTrackingItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string DisplayName { get; set; } = "Trajectory";

        public IReadOnlyList<WaferTrajectoryTrackingPoint> Points { get; set; } =
            Array.Empty<WaferTrajectoryTrackingPoint>();
    }

    /// <summary>
    /// 当前运动目标更新事件，用于实时刷新监控界面的目标标记。
    /// </summary>
    public sealed class WaferTrajectoryTrackingTargetPayload
    {
        public string RunId { get; set; } = string.Empty;

        public int TrajectoryIndex { get; set; }

        public WaferTrajectoryTrackingPoint Target { get; set; }
    }

    /// <summary>
    /// 轨迹执行进度事件，用于切换待执行、执行中和已完成状态。
    /// </summary>
    public sealed class WaferTrajectoryTrackingProgressPayload
    {
        public string RunId { get; set; } = string.Empty;

        public int RunningIndex { get; set; } = -1;

        public int CompletedIndex { get; set; } = -1;

        public bool IsFinished { get; set; }
    }

    /// <summary>
    /// 轨迹监控会话结束事件。
    /// </summary>
    public sealed class WaferTrajectoryTrackingStopPayload
    {
        public string RunId { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 轨迹监控事件中的轻量 XY 坐标，避免依赖 WPF Point 参与业务事件传递。
    /// </summary>
    public readonly struct WaferTrajectoryTrackingPoint
    {
        public WaferTrajectoryTrackingPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }
}

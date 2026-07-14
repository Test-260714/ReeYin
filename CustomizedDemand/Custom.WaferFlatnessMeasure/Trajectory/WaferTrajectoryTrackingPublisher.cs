using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using LineSegment = ReeYin_V.Core.MovingRelated.LineSegment;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 负责向轨迹监控界面发布开始、目标、进度和结束事件，避免运动执行流程内重复组装 payload。
    /// </summary>
    internal sealed class WaferTrajectoryTrackingPublisher
    {
        private string _currentRunId = string.Empty;

        public string BeginLineTracking(
            int sourceSerial,
            IReadOnlyList<LineSegment>? segments,
            int actualPositionPollIntervalMs)
        {
            if (segments == null || segments.Count == 0)
            {
                return string.Empty;
            }

            string runId = Guid.NewGuid().ToString("N");
            _currentRunId = runId;

            var trackingItems = new List<WaferTrajectoryTrackingItem>(segments.Count);
            for (int index = 0; index < segments.Count; index++)
            {
                LineSegment segment = segments[index];
                trackingItems.Add(new WaferTrajectoryTrackingItem
                {
                    Id = $"line-{index + 1:D3}",
                    DisplayName = $"Line {index + 1:D3}",
                    Points = new[]
                    {
                        new WaferTrajectoryTrackingPoint(segment.Start.X, segment.Start.Y),
                        new WaferTrajectoryTrackingPoint(segment.End.X, segment.End.Y),
                    },
                });
            }

            PublishEvent(
                WaferTrajectoryTrackingEventNames.Start,
                new WaferTrajectoryTrackingStartPayload
                {
                    RunId = runId,
                    SourceSerial = sourceSerial,
                    IsPointTrajectory = false,
                    Trajectories = trackingItems,
                    InitialTarget = new WaferTrajectoryTrackingPoint(segments[0].Start.X, segments[0].Start.Y),
                    ActualPositionPollIntervalMs = actualPositionPollIntervalMs,
                    StartActualPositionMonitor = true,
                });

            return runId;
        }

        public string BeginPointTracking(
            int sourceSerial,
            IReadOnlyList<LocusInfo>? locusInfos,
            int actualPositionPollIntervalMs)
        {
            if (locusInfos == null || locusInfos.Count == 0)
            {
                return string.Empty;
            }

            string runId = Guid.NewGuid().ToString("N");
            _currentRunId = runId;

            var trackingItems = new List<WaferTrajectoryTrackingItem>(locusInfos.Count);
            for (int index = 0; index < locusInfos.Count; index++)
            {
                LocusInfo locusInfo = locusInfos[index];
                trackingItems.Add(new WaferTrajectoryTrackingItem
                {
                    Id = $"point-{index + 1:D3}",
                    DisplayName = $"Point {index + 1:D3}",
                    Points = new[]
                    {
                        new WaferTrajectoryTrackingPoint(locusInfo.TargetX, locusInfo.TargetY),
                    },
                });
            }

            PublishEvent(
                WaferTrajectoryTrackingEventNames.Start,
                new WaferTrajectoryTrackingStartPayload
                {
                    RunId = runId,
                    SourceSerial = sourceSerial,
                    IsPointTrajectory = true,
                    Trajectories = trackingItems,
                    InitialTarget = new WaferTrajectoryTrackingPoint(locusInfos[0].TargetX, locusInfos[0].TargetY),
                    ActualPositionPollIntervalMs = actualPositionPollIntervalMs,
                    StartActualPositionMonitor = true,
                });

            return runId;
        }

        public void PublishTarget(string runId, int trajectoryIndex, double targetX, double targetY)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            PublishEvent(
                WaferTrajectoryTrackingEventNames.Target,
                new WaferTrajectoryTrackingTargetPayload
                {
                    RunId = runId,
                    TrajectoryIndex = trajectoryIndex,
                    Target = new WaferTrajectoryTrackingPoint(targetX, targetY),
                });
        }

        public void PublishProgress(
            string runId,
            int runningIndex,
            int completedIndex,
            bool isFinished = false)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            PublishEvent(
                WaferTrajectoryTrackingEventNames.Progress,
                new WaferTrajectoryTrackingProgressPayload
                {
                    RunId = runId,
                    RunningIndex = runningIndex,
                    CompletedIndex = completedIndex,
                    IsFinished = isFinished,
                });
        }

        public void PublishStop(string runId, bool isCompleted, string message)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            PublishEvent(
                WaferTrajectoryTrackingEventNames.Stop,
                new WaferTrajectoryTrackingStopPayload
                {
                    RunId = runId,
                    IsCompleted = isCompleted,
                    Message = message ?? string.Empty,
                });

            if (string.Equals(_currentRunId, runId, StringComparison.Ordinal))
            {
                _currentRunId = string.Empty;
            }
        }

        private static void PublishEvent(string eventName, object payload)
        {
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((eventName, payload));
        }
    }
}

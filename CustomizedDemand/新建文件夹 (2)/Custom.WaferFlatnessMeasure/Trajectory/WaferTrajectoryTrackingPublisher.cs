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
        private static readonly object TrackingSnapshotLock = new object();
        private static readonly Dictionary<string, WaferTrajectoryTrackingSnapshot> TrackingSnapshotsByRunId =
            new Dictionary<string, WaferTrajectoryTrackingSnapshot>(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> LatestRunIdBySourceSerial = new Dictionary<int, string>();
        private static string _latestRunId = string.Empty;

        private string _currentRunId = string.Empty;

        public static bool TryGetLatestSnapshot(int sourceSerial, out WaferTrajectoryTrackingSnapshot? snapshot)
        {
            lock (TrackingSnapshotLock)
            {
                if (sourceSerial >= 0 &&
                    LatestRunIdBySourceSerial.TryGetValue(sourceSerial, out string? sourceRunId) &&
                    TryCloneSnapshotLocked(sourceRunId, out snapshot))
                {
                    return true;
                }

                if (TryCloneLatestActiveSnapshotLocked(out snapshot))
                {
                    return true;
                }

                return TryCloneSnapshotLocked(_latestRunId, out snapshot);
            }
        }

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

            var payload = new WaferTrajectoryTrackingStartPayload
            {
                RunId = runId,
                SourceSerial = sourceSerial,
                IsPointTrajectory = false,
                Trajectories = trackingItems,
                InitialTarget = new WaferTrajectoryTrackingPoint(segments[0].Start.X, segments[0].Start.Y),
                ActualPositionPollIntervalMs = actualPositionPollIntervalMs,
                StartActualPositionMonitor = true,
            };

            TrackStart(payload);
            PublishEvent(
                WaferTrajectoryTrackingEventNames.Start,
                payload);

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

            var payload = new WaferTrajectoryTrackingStartPayload
            {
                RunId = runId,
                SourceSerial = sourceSerial,
                IsPointTrajectory = true,
                Trajectories = trackingItems,
                InitialTarget = new WaferTrajectoryTrackingPoint(locusInfos[0].TargetX, locusInfos[0].TargetY),
                ActualPositionPollIntervalMs = actualPositionPollIntervalMs,
                StartActualPositionMonitor = true,
            };

            TrackStart(payload);
            PublishEvent(
                WaferTrajectoryTrackingEventNames.Start,
                payload);

            return runId;
        }

        public void PublishTarget(string runId, int trajectoryIndex, double targetX, double targetY)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            var payload = new WaferTrajectoryTrackingTargetPayload
            {
                RunId = runId,
                TrajectoryIndex = trajectoryIndex,
                Target = new WaferTrajectoryTrackingPoint(targetX, targetY),
            };

            TrackTarget(payload);
            PublishEvent(
                WaferTrajectoryTrackingEventNames.Target,
                payload);
        }

        public void PublishProgress(
            string? runId,
            int runningIndex,
            int completedIndex,
            bool isFinished = false)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            var payload = new WaferTrajectoryTrackingProgressPayload
            {
                RunId = runId,
                RunningIndex = runningIndex,
                CompletedIndex = completedIndex,
                IsFinished = isFinished,
            };

            TrackProgress(payload);
            PublishEvent(
                WaferTrajectoryTrackingEventNames.Progress,
                payload);
        }

        public void PublishStop(string runId, bool isCompleted, string message)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            var payload = new WaferTrajectoryTrackingStopPayload
            {
                RunId = runId,
                IsCompleted = isCompleted,
                Message = message ?? string.Empty,
            };

            TrackStop(payload);
            PublishEvent(
                WaferTrajectoryTrackingEventNames.Stop,
                payload);

            if (string.Equals(_currentRunId, runId, StringComparison.Ordinal))
            {
                _currentRunId = string.Empty;
            }
        }

        private static bool TryCloneSnapshotLocked(
            string runId,
            out WaferTrajectoryTrackingSnapshot? snapshot)
        {
            if (!string.IsNullOrWhiteSpace(runId) &&
                TrackingSnapshotsByRunId.TryGetValue(runId, out WaferTrajectoryTrackingSnapshot? storedSnapshot))
            {
                snapshot = storedSnapshot.Clone();
                return true;
            }

            snapshot = null;
            return false;
        }

        private static bool TryCloneLatestActiveSnapshotLocked(out WaferTrajectoryTrackingSnapshot? snapshot)
        {
            WaferTrajectoryTrackingSnapshot? activeSnapshot = null;
            foreach (WaferTrajectoryTrackingSnapshot candidate in TrackingSnapshotsByRunId.Values)
            {
                if (candidate.IsActive)
                {
                    activeSnapshot = candidate;
                }
            }

            if (activeSnapshot == null)
            {
                snapshot = null;
                return false;
            }

            snapshot = activeSnapshot.Clone();
            return true;
        }

        private static void TrackStart(WaferTrajectoryTrackingStartPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.RunId))
            {
                return;
            }

            lock (TrackingSnapshotLock)
            {
                TrackingSnapshotsByRunId[payload.RunId] = new WaferTrajectoryTrackingSnapshot
                {
                    StartPayload = WaferTrajectoryTrackingSnapshot.CloneStartPayload(payload),
                    CurrentTarget = payload.InitialTarget,
                    RunningIndex = -1,
                    CompletedIndex = -1,
                    IsFinished = false,
                    IsStopped = false,
                    StatusMessage = "Trajectory tracking started.",
                };
                LatestRunIdBySourceSerial[payload.SourceSerial] = payload.RunId;
                _latestRunId = payload.RunId;
            }
        }

        private static void TrackTarget(WaferTrajectoryTrackingTargetPayload payload)
        {
            lock (TrackingSnapshotLock)
            {
                if (!TrackingSnapshotsByRunId.TryGetValue(payload.RunId, out WaferTrajectoryTrackingSnapshot? snapshot))
                {
                    return;
                }

                snapshot.CurrentTarget = payload.Target;
                snapshot.RunningIndex = payload.TrajectoryIndex;
                snapshot.CompletedIndex = payload.TrajectoryIndex - 1;
                snapshot.IsFinished = false;
                snapshot.IsStopped = false;
                snapshot.StatusMessage = "Trajectory target updated.";
                _latestRunId = payload.RunId;
            }
        }

        private static void TrackProgress(WaferTrajectoryTrackingProgressPayload payload)
        {
            lock (TrackingSnapshotLock)
            {
                if (!TrackingSnapshotsByRunId.TryGetValue(payload.RunId, out WaferTrajectoryTrackingSnapshot? snapshot))
                {
                    return;
                }

                snapshot.RunningIndex = payload.RunningIndex;
                snapshot.CompletedIndex = payload.CompletedIndex;
                snapshot.IsFinished = payload.IsFinished;
                snapshot.IsStopped = false;
                snapshot.StatusMessage = payload.IsFinished
                    ? "Trajectory tracking finished."
                    : "Trajectory progress updated.";
                _latestRunId = payload.RunId;
            }
        }

        private static void TrackStop(WaferTrajectoryTrackingStopPayload payload)
        {
            lock (TrackingSnapshotLock)
            {
                if (!TrackingSnapshotsByRunId.TryGetValue(payload.RunId, out WaferTrajectoryTrackingSnapshot? snapshot))
                {
                    return;
                }

                snapshot.RunningIndex = -1;
                snapshot.CompletedIndex = payload.IsCompleted
                    ? Math.Max(snapshot.CompletedIndex, snapshot.StartPayload.Trajectories?.Count - 1 ?? -1)
                    : snapshot.CompletedIndex;
                snapshot.IsFinished = payload.IsCompleted;
                snapshot.IsStopped = true;
                snapshot.StatusMessage = payload.Message ?? string.Empty;
                _latestRunId = payload.RunId;
            }
        }

        private static void PublishEvent(string eventName, object payload)
        {
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish((eventName, payload));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Share.Models.Trajectory
{
    public static class TrajectoryTrackingPayloadFactory
    {
        public static TrajectoryTrackingStartPayload CreateStartPayload(
            IEnumerable<TrajectoryPrimitive> primitives,
            int sourceSerial = 0,
            int actualPositionPollIntervalMs = 100,
            bool startActualPositionMonitor = true,
            string runId = null)
        {
            TrajectoryPrimitive[] sourceItems = primitives?.Where(item => item != null).ToArray()
                ?? Array.Empty<TrajectoryPrimitive>();

            TrajectoryItem[] trajectories = sourceItems
                .Select((item, index) => CreateTrajectoryItem(item, index))
                .ToArray();

            return new TrajectoryTrackingStartPayload
            {
                RunId = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N") : runId,
                SourceSerial = sourceSerial,
                IsPointTrajectory = trajectories.All(item => item.Kind == TrajectoryGeometryType.Point),
                Trajectories = trajectories,
                InitialTarget = trajectories.Length == 0 ? (TrajectoryPoint?)null : trajectories[0].StartPoint,
                ActualPositionPollIntervalMs = Math.Max(10, actualPositionPollIntervalMs),
                StartActualPositionMonitor = startActualPositionMonitor
            };
        }

        public static TrajectoryTrackingTargetPayload CreateTargetPayload(string runId, int trajectoryIndex, TrajectoryPoint target)
        {
            return new TrajectoryTrackingTargetPayload
            {
                RunId = runId ?? string.Empty,
                TrajectoryIndex = trajectoryIndex,
                Target = target
            };
        }

        public static TrajectoryTrackingProgressPayload CreateProgressPayload(
            string runId,
            int runningIndex,
            int completedIndex,
            bool isFinished = false)
        {
            return new TrajectoryTrackingProgressPayload
            {
                RunId = runId ?? string.Empty,
                RunningIndex = runningIndex,
                CompletedIndex = completedIndex,
                IsFinished = isFinished
            };
        }

        public static TrajectoryTrackingStopPayload CreateStopPayload(string runId, bool isCompleted, string message = null)
        {
            return new TrajectoryTrackingStopPayload
            {
                RunId = runId ?? string.Empty,
                IsCompleted = isCompleted,
                Message = message ?? string.Empty
            };
        }

        private static TrajectoryItem CreateTrajectoryItem(TrajectoryPrimitive primitive, int index)
        {
            string prefix = primitive.Kind == TrajectoryGeometryType.Point ? "point" : "trajectory";
            string fallbackName = primitive.Kind == TrajectoryGeometryType.Point ? "Point" : "Trajectory";
            string displayName = string.IsNullOrWhiteSpace(primitive.DisplayName)
                ? $"{fallbackName} {index + 1:D3}"
                : primitive.DisplayName;

            return new TrajectoryItem
            {
                Id = string.IsNullOrWhiteSpace(primitive.Id) ? $"{prefix}-{index + 1:D3}" : primitive.Id,
                DisplayName = displayName,
                GeometryType = primitive.GeometryType,
                Points = primitive.Points,
                RunProfile = primitive.RunProfile,
                State = TrajectoryExecutionState.Pending
            };
        }
    }
}

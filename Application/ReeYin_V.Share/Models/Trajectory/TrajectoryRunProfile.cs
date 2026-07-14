using Prism.Mvvm;
using System;

namespace ReeYin_V.Share.Models.Trajectory
{
    public enum TrajectoryRunMode
    {
        None = 0,
        Positioning = 1,
        PointToPoint = 2,
        LineInterpolation = 3,
        CustomInterpolation = 4,
        ContinuousScan = 5,
        ExecutorDefined = 100
    }

    public enum TrajectoryStartMode
    {
        FromCurrentPosition = 0,
        MoveToStartFirst = 1
    }

    [Serializable]
    public sealed class TrajectoryRunProfile : BindableBase
    {
        private TrajectoryRunMode _runMode = TrajectoryRunMode.ExecutorDefined;
        private TrajectoryStartMode _startMode = TrajectoryStartMode.FromCurrentPosition;
        private bool _enableDataCollection;
        private bool _enablePositionComparison;
        private double _sampleInterval;
        private string _executorKey = string.Empty;

        public TrajectoryRunMode RunMode
        {
            get => _runMode;
            set => SetProperty(ref _runMode, value);
        }

        public TrajectoryStartMode StartMode
        {
            get => _startMode;
            set => SetProperty(ref _startMode, value);
        }

        public bool EnableDataCollection
        {
            get => _enableDataCollection;
            set => SetProperty(ref _enableDataCollection, value);
        }

        public bool EnablePositionComparison
        {
            get => _enablePositionComparison;
            set => SetProperty(ref _enablePositionComparison, value);
        }

        public double SampleInterval
        {
            get => _sampleInterval;
            set => SetProperty(ref _sampleInterval, double.IsFinite(value) ? Math.Max(0d, value) : 0d);
        }

        public string ExecutorKey
        {
            get => _executorKey;
            set => SetProperty(ref _executorKey, value ?? string.Empty);
        }

        public static TrajectoryRunProfile DefaultFor(TrajectoryGeometryType geometryType)
        {
            switch (geometryType)
            {
                case TrajectoryGeometryType.Point:
                    return new TrajectoryRunProfile
                    {
                        RunMode = TrajectoryRunMode.Positioning,
                        StartMode = TrajectoryStartMode.FromCurrentPosition
                    };

                case TrajectoryGeometryType.LineSegment:
                    return new TrajectoryRunProfile
                    {
                        RunMode = TrajectoryRunMode.LineInterpolation,
                        StartMode = TrajectoryStartMode.MoveToStartFirst
                    };

                case TrajectoryGeometryType.Polyline:
                    return new TrajectoryRunProfile
                    {
                        RunMode = TrajectoryRunMode.CustomInterpolation,
                        StartMode = TrajectoryStartMode.MoveToStartFirst
                    };

                default:
                    return new TrajectoryRunProfile();
            }
        }
    }
}

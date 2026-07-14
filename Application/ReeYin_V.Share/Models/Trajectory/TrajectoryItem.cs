using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Share.Models.Trajectory
{
    public enum TrajectoryExecutionState
    {
        Pending = 0,
        Running = 1,
        Completed = 2
    }

    [Serializable]
    public sealed class TrajectoryItem : BindableBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _displayName = "Trajectory";
        private TrajectoryGeometryType _geometryType = TrajectoryGeometryType.LineSegment;
        private IReadOnlyList<TrajectoryPoint> _points = Array.Empty<TrajectoryPoint>();
        private TrajectoryExecutionState _state = TrajectoryExecutionState.Pending;
        private TrajectoryRunProfile _runProfile = TrajectoryRunProfile.DefaultFor(TrajectoryGeometryType.LineSegment);
        private bool _isVisible = true;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value ?? string.Empty);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, string.IsNullOrWhiteSpace(value) ? "Trajectory" : value.Trim());
        }

        public TrajectoryGeometryType GeometryType
        {
            get => _geometryType;
            set
            {
                if (SetProperty(ref _geometryType, value))
                {
                    RunProfile = TrajectoryRunProfile.DefaultFor(value);
                    RaisePropertyChanged(nameof(Kind));
                }
            }
        }

        public IReadOnlyList<TrajectoryPoint> Points
        {
            get => _points;
            set
            {
                if (SetProperty(ref _points, value ?? Array.Empty<TrajectoryPoint>()))
                {
                    RaiseGeometryChanged();
                }
            }
        }

        public TrajectoryExecutionState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public TrajectoryRunProfile RunProfile
        {
            get => _runProfile;
            set => SetProperty(ref _runProfile, value ?? TrajectoryRunProfile.DefaultFor(GeometryType));
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public TrajectoryGeometryType Kind => GeometryType;

        public int PointCount => _points.Count;

        public bool HasPoints => _points.Count > 0;

        public TrajectoryPoint StartPoint => _points.Count > 0 ? _points[0] : TrajectoryPoint.Empty;

        public TrajectoryPoint EndPoint => _points.Count > 0 ? _points[_points.Count - 1] : TrajectoryPoint.Empty;

        public double PathLength
        {
            get
            {
                if (_points.Count < 2)
                {
                    return 0d;
                }

                double length = 0d;
                for (int index = 1; index < _points.Count; index++)
                {
                    length += _points[index - 1].DistanceTo(_points[index]);
                }

                return length;
            }
        }

        public static TrajectoryItem FromPoints(string displayName, IEnumerable<TrajectoryPoint> points)
        {
            return new TrajectoryItem
            {
                DisplayName = displayName,
                GeometryType = InferGeometryType(points),
                Points = points?.ToArray() ?? Array.Empty<TrajectoryPoint>()
            };
        }

        public static TrajectoryItem FromPrimitive(TrajectoryPrimitive primitive)
        {
            if (primitive == null)
            {
                return new TrajectoryItem();
            }

            return primitive.ToTrajectoryItem();
        }

        private void RaiseGeometryChanged()
        {
            RaisePropertyChanged(nameof(Kind));
            RaisePropertyChanged(nameof(GeometryType));
            RaisePropertyChanged(nameof(PointCount));
            RaisePropertyChanged(nameof(HasPoints));
            RaisePropertyChanged(nameof(StartPoint));
            RaisePropertyChanged(nameof(EndPoint));
            RaisePropertyChanged(nameof(PathLength));
        }

        private static TrajectoryGeometryType InferGeometryType(IEnumerable<TrajectoryPoint> points)
        {
            if (points == null)
            {
                return TrajectoryGeometryType.Custom;
            }

            int count = points.Count();
            if (count <= 1)
            {
                return TrajectoryGeometryType.Point;
            }

            return count == 2 ? TrajectoryGeometryType.LineSegment : TrajectoryGeometryType.Polyline;
        }
    }
}

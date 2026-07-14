using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Share.Models.Trajectory
{
    public enum TrajectoryGeometryType
    {
        Point = 0,
        LineSegment = 1,
        Polyline = 2,
        Arc = 3,
        Custom = 100
    }

    [Serializable]
    public sealed class TrajectoryPrimitive : BindableBase
    {
        public const string LineType = "IsLine";
        public const string PointType = "IsPoint";

        private string _id = Guid.NewGuid().ToString("N");
        private string _displayName = "Trajectory";
        private TrajectoryGeometryType _geometryType = TrajectoryGeometryType.LineSegment;
        private IReadOnlyList<TrajectoryPoint> _points = Array.Empty<TrajectoryPoint>();
        private TrajectoryRunProfile _runProfile = TrajectoryRunProfile.DefaultFor(TrajectoryGeometryType.LineSegment);

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
                    RaiseGeometryChanged();
                    RaisePropertyChanged(nameof(Type));
                }
            }
        }

        public TrajectoryGeometryType Kind
        {
            get => GeometryType;
            set => GeometryType = value;
        }

        public string Type
        {
            get => GeometryType == TrajectoryGeometryType.Point ? PointType : LineType;
            set => GeometryType = string.Equals(value, PointType, StringComparison.OrdinalIgnoreCase)
                ? TrajectoryGeometryType.Point
                : TrajectoryGeometryType.LineSegment;
        }

        public IReadOnlyList<TrajectoryPoint> Points
        {
            get => _points;
            set
            {
                if (SetProperty(ref _points, NormalizePoints(value)))
                {
                    RaiseGeometryChanged();
                }
            }
        }

        public TrajectoryRunProfile RunProfile
        {
            get => _runProfile;
            set => SetProperty(ref _runProfile, value ?? TrajectoryRunProfile.DefaultFor(GeometryType));
        }

        public double OriginX
        {
            get => StartPoint.X;
            set => SetPoint(0, value, OriginY);
        }

        public double OriginY
        {
            get => StartPoint.Y;
            set => SetPoint(0, OriginX, value);
        }

        public double TargetX
        {
            get => EndPoint.X;
            set => SetPoint(GetTargetPointIndex(), value, TargetY);
        }

        public double TargetY
        {
            get => EndPoint.Y;
            set => SetPoint(GetTargetPointIndex(), TargetX, value);
        }

        public TrajectoryPoint Origin => new TrajectoryPoint(OriginX, OriginY);

        public TrajectoryPoint Target => new TrajectoryPoint(TargetX, TargetY);

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

        public bool IsValidForRun()
        {
            switch (GeometryType)
            {
                case TrajectoryGeometryType.Point:
                    return _points.Count == 1 && _points[0].IsFinite;
                case TrajectoryGeometryType.LineSegment:
                    return _points.Count >= 2 && _points[0].IsFinite && _points[1].IsFinite;
                case TrajectoryGeometryType.Polyline:
                case TrajectoryGeometryType.Arc:
                case TrajectoryGeometryType.Custom:
                    return _points.Count >= 2 && AllPointsAreFinite();
                default:
                    return false;
            }
        }

        public TrajectoryItem ToTrajectoryItem()
        {
            return new TrajectoryItem
            {
                Id = Id,
                DisplayName = DisplayName,
                GeometryType = GeometryType,
                Points = Points,
                RunProfile = RunProfile
            };
        }

        private static IReadOnlyList<TrajectoryPoint> NormalizePoints(IEnumerable<TrajectoryPoint> points)
        {
            if (points == null)
            {
                return Array.Empty<TrajectoryPoint>();
            }

            return new List<TrajectoryPoint>(points);
        }

        private void SetPoint(int index, double x, double y)
        {
            int normalizedIndex = Math.Max(index, 0);
            var points = new List<TrajectoryPoint>(_points);
            while (points.Count <= normalizedIndex)
            {
                points.Add(TrajectoryPoint.Empty);
            }

            points[normalizedIndex] = new TrajectoryPoint(x, y);
            Points = points;
        }

        private int GetTargetPointIndex()
        {
            if (GeometryType == TrajectoryGeometryType.Point)
            {
                return 0;
            }

            return _points.Count > 1 ? _points.Count - 1 : 1;
        }

        private bool AllPointsAreFinite()
        {
            foreach (TrajectoryPoint point in _points)
            {
                if (!point.IsFinite)
                {
                    return false;
                }
            }

            return true;
        }

        private void RaiseGeometryChanged()
        {
            RaisePropertyChanged(nameof(GeometryType));
            RaisePropertyChanged(nameof(Kind));
            RaisePropertyChanged(nameof(Origin));
            RaisePropertyChanged(nameof(Target));
            RaisePropertyChanged(nameof(OriginX));
            RaisePropertyChanged(nameof(OriginY));
            RaisePropertyChanged(nameof(TargetX));
            RaisePropertyChanged(nameof(TargetY));
            RaisePropertyChanged(nameof(PointCount));
            RaisePropertyChanged(nameof(HasPoints));
            RaisePropertyChanged(nameof(StartPoint));
            RaisePropertyChanged(nameof(EndPoint));
            RaisePropertyChanged(nameof(PathLength));
        }
    }
}

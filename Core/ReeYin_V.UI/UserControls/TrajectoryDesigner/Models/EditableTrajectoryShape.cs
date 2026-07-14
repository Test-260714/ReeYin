using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Models
{
    [Serializable]
    public sealed class EditableTrajectoryShape : BindableBase
    {
        private Guid _id = Guid.NewGuid();
        private bool _isEnabled = true;
        private TrajectoryShapeKind _kind;
        private double _x;
        private double _y;
        private double _width = 80d;
        private double _height = 60d;
        private double _radius = 40d;
        private double _startAngle;
        private double _sweepAngle = 180d;
        private double _rotationAngle;
        private double _scale = 1d;
        private TrajectoryInnerPattern _innerPattern = TrajectoryInnerPattern.None;
        private double _innerSpacing = 20d;
        private int _innerLineCount = 5;
        private List<TrajectoryPoint> _polylinePoints = new List<TrajectoryPoint>();

        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public TrajectoryShapeKind Kind
        {
            get => _kind;
            set
            {
                if (SetProperty(ref _kind, value))
                {
                    RaisePropertyChanged(nameof(Category));
                    RaisePropertyChanged(nameof(CategoryText));
                }
            }
        }

        public TrajectoryShapeCategory Category => TrajectoryShapeCategoryResolver.Resolve(Kind);

        public string CategoryText => TrajectoryShapeCategoryResolver.ToDisplayText(Category);

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, Math.Max(1d, value));
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, Math.Max(1d, value));
        }

        public double Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, Math.Max(1d, value));
        }

        public double StartAngle
        {
            get => _startAngle;
            set => SetProperty(ref _startAngle, value);
        }

        public double SweepAngle
        {
            get => _sweepAngle;
            set => SetProperty(ref _sweepAngle, value);
        }

        public double RotationAngle
        {
            get => _rotationAngle;
            set => SetProperty(ref _rotationAngle, double.IsFinite(value) ? value : 0d);
        }

        public double Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, double.IsFinite(value) && value > 0d ? value : 1d);
        }

        public TrajectoryInnerPattern InnerPattern
        {
            get => _innerPattern;
            set => SetProperty(ref _innerPattern, value);
        }

        public double InnerSpacing
        {
            get => _innerSpacing;
            set => SetProperty(ref _innerSpacing, double.IsFinite(value) && value > 0d ? value : 1d);
        }

        public int InnerLineCount
        {
            get => _innerLineCount;
            set => SetProperty(ref _innerLineCount, Math.Max(1, value));
        }

        public List<TrajectoryPoint> PolylinePoints
        {
            get => _polylinePoints;
            set => SetProperty(
                ref _polylinePoints,
                value?.Where(point => point != null).Select(point => point.Clone()).ToList()
                    ?? new List<TrajectoryPoint>());
        }
    }
}

using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    public sealed class TrajectoryDesignerEditViewModel : BindableBase
    {
        private readonly double _sampleInterval;
        private EditableTrajectoryShape? _selectedShape;
        private TrajectoryDesignerTool _activeTool = TrajectoryDesignerTool.Select;
        private double _defaultCenterX;
        private double _defaultCenterY;
        private double _coordinateStartX;
        private double _coordinateStartY;
        private double _coordinateEndX = 640d;
        private double _coordinateEndY = 480d;
        private DesignedTrajectoryPlan _resultPlan = new DesignedTrajectoryPlan();
        private bool _isApplied;

        public TrajectoryDesignerEditViewModel(
            DesignedTrajectoryPlan? plan,
            double defaultCenterX,
            double defaultCenterY,
            double sampleInterval)
        {
            _sampleInterval = sampleInterval > 0 ? sampleInterval : 1d;
            Shapes = DesignedTrajectoryBuilder.ToEditableShapes(plan);
            Shapes.CollectionChanged += OnShapesCollectionChanged;
            foreach (EditableTrajectoryShape shape in Shapes)
            {
                shape.PropertyChanged += OnShapePropertyChanged;
            }

            SelectedShapes = new ObservableCollection<EditableTrajectoryShape>();
            SelectedShapes.CollectionChanged += OnSelectedShapesCollectionChanged;
            InnerPatternItems = new[]
            {
                new TrajectoryInnerPatternOption(TrajectoryInnerPattern.None, "无"),
                new TrajectoryInnerPatternOption(TrajectoryInnerPattern.EquidistantPoints, "等间距点"),
                new TrajectoryInnerPatternOption(TrajectoryInnerPattern.HorizontalLines, "水平线"),
                new TrajectoryInnerPatternOption(TrajectoryInnerPattern.VerticalLines, "垂直线"),
                new TrajectoryInnerPatternOption(TrajectoryInnerPattern.CrossLines, "交叉线")
            };

            if (plan != null)
            {
                CoordinateStartX = plan.CoordinateStartX;
                CoordinateStartY = plan.CoordinateStartY;
                CoordinateEndX = plan.CoordinateEndX;
                CoordinateEndY = plan.CoordinateEndY;
                DefaultCenterX = plan.DefaultCenterX;
                DefaultCenterY = plan.DefaultCenterY;
                ResultPlan = plan.Clone();
            }
            else
            {
                DefaultCenterX = defaultCenterX;
                DefaultCenterY = defaultCenterY;
                ResultPlan = BuildCurrentPlan();
            }

            RestoreSelectedShapeCenterCommand = new DelegateCommand(RestoreSelectedShapeCenter, () => SelectedShape != null)
                .ObservesProperty(() => SelectedShape);
            ClearShapesCommand = new DelegateCommand(ClearShapes, () => Shapes.Count > 0);
            ReturnCommand = new DelegateCommand(ReturnAndApply);
        }

        public event Action? RequestClose;

        public ObservableCollection<EditableTrajectoryShape> Shapes { get; }

        public ObservableCollection<EditableTrajectoryShape> SelectedShapes { get; }

        public IReadOnlyList<TrajectoryInnerPatternOption> InnerPatternItems { get; }

        public DelegateCommand RestoreSelectedShapeCenterCommand { get; }

        public DelegateCommand ClearShapesCommand { get; }

        public DelegateCommand ReturnCommand { get; }

        public EditableTrajectoryShape? SelectedShape
        {
            get => _selectedShape;
            set
            {
                if (SetProperty(ref _selectedShape, value))
                {
                    if (value != null && SelectedShapes.Count > 0)
                    {
                        SelectedShapes.Clear();
                    }

                    RaiseSelectionChanged();
                }
            }
        }

        public TrajectoryDesignerTool ActiveTool
        {
            get => _activeTool;
            set => SetProperty(ref _activeTool, value);
        }

        public double DefaultCenterX
        {
            get => _defaultCenterX;
            set => SetProperty(ref _defaultCenterX, value);
        }

        public double DefaultCenterY
        {
            get => _defaultCenterY;
            set => SetProperty(ref _defaultCenterY, value);
        }

        public double CoordinateStartX
        {
            get => _coordinateStartX;
            set
            {
                if (SetProperty(ref _coordinateStartX, value))
                {
                    RaisePlanInputChanged();
                }
            }
        }

        public double CoordinateStartY
        {
            get => _coordinateStartY;
            set
            {
                if (SetProperty(ref _coordinateStartY, value))
                {
                    RaisePlanInputChanged();
                }
            }
        }

        public double CoordinateEndX
        {
            get => _coordinateEndX;
            set
            {
                if (SetProperty(ref _coordinateEndX, value))
                {
                    RaisePlanInputChanged();
                }
            }
        }

        public double CoordinateEndY
        {
            get => _coordinateEndY;
            set
            {
                if (SetProperty(ref _coordinateEndY, value))
                {
                    RaisePlanInputChanged();
                }
            }
        }

        public DesignedTrajectoryPlan ResultPlan
        {
            get => _resultPlan;
            private set => SetProperty(ref _resultPlan, value ?? new DesignedTrajectoryPlan());
        }

        public bool IsApplied
        {
            get => _isApplied;
            private set => SetProperty(ref _isApplied, value);
        }

        public bool HasSelectedShape => SelectedShape != null && SelectedShapes.Count == 0;

        public string ShapeCountText => $"图形：{Shapes.Count}，启用：{Shapes.Count(item => item.IsEnabled)}";

        public string RunStepCountText
        {
            get
            {
                DesignedTrajectoryPlan plan = BuildCurrentPlan();
                int pointCount = plan.RunSteps.Count(item => item.Kind == DesignedTrajectoryRunStepKind.Point);
                int lineCount = plan.RunSteps.Count(item => item.Kind == DesignedTrajectoryRunStepKind.Line);
                return $"执行步骤：{plan.RunSteps.Count}，点：{pointCount}，线段：{lineCount}";
            }
        }

        public string SelectedShapeText =>
            SelectedShapes.Count > 0
                ? $"已框选 {SelectedShapes.Count} 个图形，可一起拖拽或删除。"
                : SelectedShape == null
                    ? "未选中图形。"
                    : $"已选中 {SelectedShape.Kind}：X={SelectedShape.X:F2}, Y={SelectedShape.Y:F2}, 角度={SelectedShape.RotationAngle:F1}";

        private void ReturnAndApply()
        {
            ResultPlan = BuildCurrentPlan();
            IsApplied = true;
            RequestClose?.Invoke();
        }

        private DesignedTrajectoryPlan BuildCurrentPlan()
        {
            return DesignedTrajectoryBuilder.BuildPlan(
                Shapes,
                CoordinateStartX,
                CoordinateStartY,
                CoordinateEndX,
                CoordinateEndY,
                DefaultCenterX,
                DefaultCenterY,
                _sampleInterval);
        }

        private void RestoreSelectedShapeCenter()
        {
            if (SelectedShape == null)
            {
                return;
            }

            (double x, double y) = ClampPointToCoordinateBounds(DefaultCenterX, DefaultCenterY);
            SelectedShape.X = x;
            SelectedShape.Y = y;
        }

        private void ClearShapes()
        {
            SelectedShape = null;
            SelectedShapes.Clear();
            Shapes.Clear();
        }

        private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (EditableTrajectoryShape shape in e.OldItems)
                {
                    shape.PropertyChanged -= OnShapePropertyChanged;
                    SelectedShapes.Remove(shape);
                }
            }

            if (e.NewItems != null)
            {
                foreach (EditableTrajectoryShape shape in e.NewItems)
                {
                    shape.PropertyChanged += OnShapePropertyChanged;
                }
            }

            PruneSelectionToExistingShapes();
            RaisePlanInputChanged();
            ClearShapesCommand.RaiseCanExecuteChanged();
        }

        private void OnSelectedShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedShapes.Count > 0 && SelectedShape != null)
            {
                SelectedShape = null;
            }

            RaiseSelectionChanged();
        }

        private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePlanInputChanged();
            if (ReferenceEquals(sender, SelectedShape))
            {
                RaiseSelectionChanged();
            }
        }

        private void PruneSelectionToExistingShapes()
        {
            if (SelectedShape != null && !Shapes.Contains(SelectedShape))
            {
                SelectedShape = null;
            }

            for (int index = SelectedShapes.Count - 1; index >= 0; index--)
            {
                if (!Shapes.Contains(SelectedShapes[index]))
                {
                    SelectedShapes.RemoveAt(index);
                }
            }
        }

        private void RaisePlanInputChanged()
        {
            RaisePropertyChanged(nameof(ShapeCountText));
            RaisePropertyChanged(nameof(RunStepCountText));
        }

        private void RaiseSelectionChanged()
        {
            RaisePropertyChanged(nameof(HasSelectedShape));
            RaisePropertyChanged(nameof(SelectedShapeText));
        }

        private (double X, double Y) ClampPointToCoordinateBounds(double x, double y)
        {
            double minX = Math.Min(CoordinateStartX, CoordinateEndX);
            double maxX = Math.Max(CoordinateStartX, CoordinateEndX);
            double minY = Math.Min(CoordinateStartY, CoordinateEndY);
            double maxY = Math.Max(CoordinateStartY, CoordinateEndY);
            return (Math.Clamp(x, minX, maxX), Math.Clamp(y, minY, maxY));
        }
    }
}

using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using Prism.Commands;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    [Serializable]
    public sealed class TrajectoryInnerPatternOption
    {
        public TrajectoryInnerPatternOption(TrajectoryInnerPattern value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public TrajectoryInnerPattern Value { get; }

        public string DisplayName { get; }
    }

    [Serializable]
    public sealed class TrajectoryDesignerTestViewModel : DialogViewModelBase, IViewModuleParam
    {
        private EditableTrajectoryShape? _selectedShape;
        private TrajectoryDesignerTool _activeTool = TrajectoryDesignerTool.Select;
        private double _defaultCenterX = 320d;
        private double _defaultCenterY = 240d;
        private double _coordinateStartX;
        private double _coordinateStartY;
        private double _coordinateEndX = 640d;
        private double _coordinateEndY = 480d;

        public TrajectoryDesignerTestViewModel()
        {
            Title = "轨迹设计测试";
            Icon = "\ue784";

            Shapes = new ObservableCollection<EditableTrajectoryShape>();
            Shapes.CollectionChanged += OnShapesCollectionChanged;
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

            AddSampleShapesCommand = new DelegateCommand(AddSampleShapes);
            DeleteSelectedShapeCommand = new DelegateCommand(DeleteSelectedShape, () => SelectedShape != null || SelectedShapes.Count > 0)
                .ObservesProperty(() => SelectedShape);
            RestoreSelectedShapeCenterCommand = new DelegateCommand(RestoreSelectedShapeCenter, () => SelectedShape != null)
                .ObservesProperty(() => SelectedShape);
            ClearShapesCommand = new DelegateCommand(ClearShapes, () => Shapes.Count > 0);
        }

        public ObservableCollection<EditableTrajectoryShape> Shapes { get; }

        public ObservableCollection<EditableTrajectoryShape> SelectedShapes { get; }

        public IReadOnlyList<TrajectoryInnerPatternOption> InnerPatternItems { get; }

        public DelegateCommand AddSampleShapesCommand { get; }

        public DelegateCommand DeleteSelectedShapeCommand { get; }

        public DelegateCommand RestoreSelectedShapeCenterCommand { get; }

        public DelegateCommand ClearShapesCommand { get; }

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

                    RaisePropertyChanged(nameof(HasSelectedShape));
                    RaisePropertyChanged(nameof(SelectedShapeText));
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
            set => SetProperty(ref _coordinateStartX, value);
        }

        public double CoordinateStartY
        {
            get => _coordinateStartY;
            set => SetProperty(ref _coordinateStartY, value);
        }

        public double CoordinateEndX
        {
            get => _coordinateEndX;
            set => SetProperty(ref _coordinateEndX, value);
        }

        public double CoordinateEndY
        {
            get => _coordinateEndY;
            set => SetProperty(ref _coordinateEndY, value);
        }

        public bool HasSelectedShape => SelectedShape != null;

        public string ShapeCountText => $"当前图形数量：{Shapes.Count}";

        public string SelectedShapeText =>
            SelectedShapes.Count > 0
                ? $"已选中：{SelectedShapes.Count} 个图形"
                : SelectedShape == null
                ? "当前未选中图形"
                : $"已选中：{SelectedShape.Kind}  X={SelectedShape.X:F1}, Y={SelectedShape.Y:F1}, 角度={SelectedShape.RotationAngle:F1}, 缩放={SelectedShape.Scale:F2}, 内部={SelectedShape.InnerPattern}";

        private void AddSampleShapes()
        {
            (double circleX, double circleY) = ClampPointToCoordinateBounds(DefaultCenterX, DefaultCenterY);
            (double rectangleX, double rectangleY) = ClampPointToCoordinateBounds(DefaultCenterX + 180d, DefaultCenterY);
            (double arcX, double arcY) = ClampPointToCoordinateBounds(DefaultCenterX + 360d, DefaultCenterY);

            Shapes.Add(new EditableTrajectoryShape
            {
                Kind = TrajectoryShapeKind.Circle,
                X = circleX,
                Y = circleY,
                Radius = 55d,
                RotationAngle = 0d,
                Scale = 1d,
                InnerPattern = TrajectoryInnerPattern.EquidistantPoints,
                InnerSpacing = 20d,
                InnerLineCount = 5
            });

            Shapes.Add(new EditableTrajectoryShape
            {
                Kind = TrajectoryShapeKind.Rectangle,
                X = rectangleX,
                Y = rectangleY,
                Width = 140d,
                Height = 90d,
                RotationAngle = 15d,
                Scale = 1d,
                InnerPattern = TrajectoryInnerPattern.HorizontalLines,
                InnerSpacing = 20d,
                InnerLineCount = 5
            });

            Shapes.Add(new EditableTrajectoryShape
            {
                Kind = TrajectoryShapeKind.Arc,
                X = arcX,
                Y = arcY,
                Radius = 70d,
                StartAngle = 20d,
                SweepAngle = 220d,
                RotationAngle = -20d,
                Scale = 1d
            });

            SelectedShapes.Clear();
            SelectedShape = Shapes[^1];
        }

        private void DeleteSelectedShape()
        {
            if (SelectedShapes.Count > 0)
            {
                foreach (EditableTrajectoryShape selectedShape in new List<EditableTrajectoryShape>(SelectedShapes))
                {
                    Shapes.Remove(selectedShape);
                }

                SelectedShapes.Clear();
                SelectedShape = null;
                return;
            }

            if (SelectedShape == null)
            {
                return;
            }

            EditableTrajectoryShape shape = SelectedShape;
            SelectedShape = null;
            Shapes.Remove(shape);
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
            RaiseShapeSummaryChanged();
            ClearShapesCommand.RaiseCanExecuteChanged();
        }

        private void OnSelectedShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(SelectedShapeText));
            DeleteSelectedShapeCommand.RaiseCanExecuteChanged();
        }

        private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, SelectedShape))
            {
                RaisePropertyChanged(nameof(SelectedShapeText));
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

        private (double X, double Y) ClampPointToCoordinateBounds(double x, double y)
        {
            double minX = Math.Min(CoordinateStartX, CoordinateEndX);
            double maxX = Math.Max(CoordinateStartX, CoordinateEndX);
            double minY = Math.Min(CoordinateStartY, CoordinateEndY);
            double maxY = Math.Max(CoordinateStartY, CoordinateEndY);
            return (Math.Clamp(x, minX, maxX), Math.Clamp(y, minY, maxY));
        }

        private void RaiseShapeSummaryChanged()
        {
            RaisePropertyChanged(nameof(ShapeCountText));
            RaisePropertyChanged(nameof(SelectedShapeText));
        }
    }
}

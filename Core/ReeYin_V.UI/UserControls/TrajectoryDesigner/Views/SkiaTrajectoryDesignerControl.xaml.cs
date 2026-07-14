using SkiaSharp;
using SkiaSharp.Views.Desktop;
using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using ReeYin_V.UI.UserControls.TrajectoryDesigner.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Views
{
    public partial class SkiaTrajectoryDesignerControl : UserControl
    {
        public static readonly DependencyProperty ShapesProperty =
            DependencyProperty.Register(
                nameof(Shapes),
                typeof(ObservableCollection<EditableTrajectoryShape>),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(null, OnShapesChanged));

        public static readonly DependencyProperty ActiveToolProperty =
            DependencyProperty.Register(
                nameof(ActiveTool),
                typeof(TrajectoryDesignerTool),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(TrajectoryDesignerTool.Select, OnActiveToolChanged));

        public static readonly DependencyProperty SelectedShapeProperty =
            DependencyProperty.Register(
                nameof(SelectedShape),
                typeof(EditableTrajectoryShape),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(null, OnSelectedShapeChanged));

        public static readonly DependencyProperty SelectedShapesProperty =
            DependencyProperty.Register(
                nameof(SelectedShapes),
                typeof(ObservableCollection<EditableTrajectoryShape>),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(null, OnSelectedShapesChanged));

        public static readonly DependencyProperty DefaultCircleRadiusProperty =
            DependencyProperty.Register(
                nameof(DefaultCircleRadius),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(40d));

        public static readonly DependencyProperty DefaultRectangleWidthProperty =
            DependencyProperty.Register(
                nameof(DefaultRectangleWidth),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(90d));

        public static readonly DependencyProperty DefaultRectangleHeightProperty =
            DependencyProperty.Register(
                nameof(DefaultRectangleHeight),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(60d));

        public static readonly DependencyProperty DefaultArcRadiusProperty =
            DependencyProperty.Register(
                nameof(DefaultArcRadius),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(48d));

        public static readonly DependencyProperty DefaultArcStartAngleProperty =
            DependencyProperty.Register(
                nameof(DefaultArcStartAngle),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(0d));

        public static readonly DependencyProperty DefaultArcSweepAngleProperty =
            DependencyProperty.Register(
                nameof(DefaultArcSweepAngle),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(180d));

        public static readonly DependencyProperty DefaultCenterXProperty =
            DependencyProperty.Register(
                nameof(DefaultCenterX),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(0d, OnDefaultCenterChanged));

        public static readonly DependencyProperty DefaultCenterYProperty =
            DependencyProperty.Register(
                nameof(DefaultCenterY),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(0d, OnDefaultCenterChanged));

        public static readonly DependencyProperty CoordinateStartXProperty =
            DependencyProperty.Register(
                nameof(CoordinateStartX),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(0d, OnCoordinateBoundsChanged));

        public static readonly DependencyProperty CoordinateStartYProperty =
            DependencyProperty.Register(
                nameof(CoordinateStartY),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(0d, OnCoordinateBoundsChanged));

        public static readonly DependencyProperty CoordinateEndXProperty =
            DependencyProperty.Register(
                nameof(CoordinateEndX),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(640d, OnCoordinateBoundsChanged));

        public static readonly DependencyProperty CoordinateEndYProperty =
            DependencyProperty.Register(
                nameof(CoordinateEndY),
                typeof(double),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(480d, OnCoordinateBoundsChanged));

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(
                nameof(StatusText),
                typeof(string),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata("选择工具后，在区域中移动鼠标预览，左键确认放置。"));

        public static readonly DependencyProperty IsPreviewOnlyProperty =
            DependencyProperty.Register(
                nameof(IsPreviewOnly),
                typeof(bool),
                typeof(SkiaTrajectoryDesignerControl),
                new PropertyMetadata(false, OnIsPreviewOnlyChanged));

        private EditableTrajectoryShape? _previewShape;
        private EditableTrajectoryShape? _dragShape;
        private EditableTrajectoryShape? _resizeShape;
        private (double X, double Y)? _lineStartCoordinate;
        private readonly List<TrajectoryPoint> _polylinePlacementPoints = new();
        private double? _viewStartX;
        private double? _viewStartY;
        private double? _viewEndX;
        private double? _viewEndY;
        private Point _viewportPanStartPoint;
        private (double StartX, double StartY, double EndX, double EndY) _viewportPanStartWindow;
        private readonly Dictionary<EditableTrajectoryShape, (double X, double Y)> _dragStartShapeCenters = new();
        private readonly Dictionary<EditableTrajectoryShape, List<TrajectoryPoint>> _dragStartPolylinePoints = new();
        private readonly Dictionary<EditableTrajectoryShape, (double MinX, double MinY, double MaxX, double MaxY)> _dragStartShapeBounds = new();
        private readonly HashSet<EditableTrajectoryShape> _constrainingShapes = new();
        private (double X, double Y) _dragStartCoordinate;
        private Point _selectionBoxStartPoint;
        private Point _selectionBoxCurrentPoint;
        private Point _rightButtonDownPoint;
        private EditableTrajectoryShape? _rightButtonDownShape;
        private bool _isDragging;
        private bool _isResizingShape;
        private bool _isPanningCoordinateViewport;
        private bool _isSelectingByBox;
        private bool _hasRightButtonPanCandidate;
        private bool _isRightButtonPanningCoordinateViewport;
        private TrajectoryResizeHandleKind _activeResizeHandle = TrajectoryResizeHandleKind.None;
        private const double TickIntervalMillimeters = 20d;
        private const double ResizeHandleHitRadius = 10d;
        private const double CoordinateZoomStep = 1.25d;
        private const double RightButtonPanThreshold = 3d;

        private enum TrajectoryResizeHandleKind
        {
            None,
            CircleRadius,
            CircleRotate,
            RectangleTopLeft,
            RectangleTopRight,
            RectangleBottomRight,
            RectangleBottomLeft,
            RectangleRotate,
            LineStart,
            LineEnd,
            ArcRadius,
            ArcStart,
            ArcEnd
        }

        public SkiaTrajectoryDesignerControl()
        {
            InitializeComponent();
            Shapes = new ObservableCollection<EditableTrajectoryShape>();
            SelectedShapes = new ObservableCollection<EditableTrajectoryShape>();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        public ObservableCollection<EditableTrajectoryShape> Shapes
        {
            get => (ObservableCollection<EditableTrajectoryShape>)GetValue(ShapesProperty);
            set => SetValue(ShapesProperty, value);
        }

        public TrajectoryDesignerTool ActiveTool
        {
            get => (TrajectoryDesignerTool)GetValue(ActiveToolProperty);
            set => SetValue(ActiveToolProperty, value);
        }

        public EditableTrajectoryShape? SelectedShape
        {
            get => (EditableTrajectoryShape?)GetValue(SelectedShapeProperty);
            set => SetValue(SelectedShapeProperty, value);
        }

        public ObservableCollection<EditableTrajectoryShape> SelectedShapes
        {
            get => EnsureSelectedShapes();
            set => SetValue(SelectedShapesProperty, value ?? new ObservableCollection<EditableTrajectoryShape>());
        }

        public double DefaultCircleRadius
        {
            get => (double)GetValue(DefaultCircleRadiusProperty);
            set => SetValue(DefaultCircleRadiusProperty, value);
        }

        public double DefaultRectangleWidth
        {
            get => (double)GetValue(DefaultRectangleWidthProperty);
            set => SetValue(DefaultRectangleWidthProperty, value);
        }

        public double DefaultRectangleHeight
        {
            get => (double)GetValue(DefaultRectangleHeightProperty);
            set => SetValue(DefaultRectangleHeightProperty, value);
        }

        public double DefaultArcRadius
        {
            get => (double)GetValue(DefaultArcRadiusProperty);
            set => SetValue(DefaultArcRadiusProperty, value);
        }

        public double DefaultArcStartAngle
        {
            get => (double)GetValue(DefaultArcStartAngleProperty);
            set => SetValue(DefaultArcStartAngleProperty, value);
        }

        public double DefaultArcSweepAngle
        {
            get => (double)GetValue(DefaultArcSweepAngleProperty);
            set => SetValue(DefaultArcSweepAngleProperty, value);
        }

        public double DefaultCenterX
        {
            get => (double)GetValue(DefaultCenterXProperty);
            set => SetValue(DefaultCenterXProperty, value);
        }

        public double DefaultCenterY
        {
            get => (double)GetValue(DefaultCenterYProperty);
            set => SetValue(DefaultCenterYProperty, value);
        }

        public double CoordinateStartX
        {
            get => (double)GetValue(CoordinateStartXProperty);
            set => SetValue(CoordinateStartXProperty, value);
        }

        public double CoordinateStartY
        {
            get => (double)GetValue(CoordinateStartYProperty);
            set => SetValue(CoordinateStartYProperty, value);
        }

        public double CoordinateEndX
        {
            get => (double)GetValue(CoordinateEndXProperty);
            set => SetValue(CoordinateEndXProperty, value);
        }

        public double CoordinateEndY
        {
            get => (double)GetValue(CoordinateEndYProperty);
            set => SetValue(CoordinateEndYProperty, value);
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public bool IsPreviewOnly
        {
            get => (bool)GetValue(IsPreviewOnlyProperty);
            set => SetValue(IsPreviewOnlyProperty, value);
        }

        private ObservableCollection<EditableTrajectoryShape> EnsureSelectedShapes()
        {
            if (GetValue(SelectedShapesProperty) is ObservableCollection<EditableTrajectoryShape> value)
            {
                return value;
            }

            value = new ObservableCollection<EditableTrajectoryShape>();
            SetCurrentValue(SelectedShapesProperty, value);
            return value;
        }

        private static void OnShapesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            control.DetachShapes(e.OldValue as ObservableCollection<EditableTrajectoryShape>);
            control.AttachShapes(e.NewValue as ObservableCollection<EditableTrajectoryShape>);
            control.ConstrainExistingShapeCenters();
            control.InvalidateSurface();
        }

        private static void OnActiveToolChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            control.EndInteraction();
            control._lineStartCoordinate = null;
            control._polylinePlacementPoints.Clear();
            control._previewShape = null;
            if (control.IsPreviewOnly)
            {
                if (control.ActiveTool != TrajectoryDesignerTool.Select)
                {
                    control.ActiveTool = TrajectoryDesignerTool.Select;
                    return;
                }

                control.StatusText = "轨迹预览：仅显示设计结果，可使用滚轮或按钮缩放查看。";
                control.SyncToolButtons();
                control.InvalidateSurface();
                return;
            }

            control.SyncToolButtons();
            control.StatusText = control.ActiveTool switch
            {
                TrajectoryDesignerTool.Point => "点工具：移动鼠标预览位置，左键确认放置。",
                TrajectoryDesignerTool.Line => "线段工具：左键确定起点，再左键确定终点。",
                TrajectoryDesignerTool.Polyline => "多线段工具：左键逐点添加，右键结束并保存。",
                TrajectoryDesignerTool.Circle => "圆工具：默认在固定中心创建，左键确认放置。",
                TrajectoryDesignerTool.Rectangle => "矩形工具：默认在固定中心创建，左键确认放置。",
                TrajectoryDesignerTool.Arc => "圆弧工具：移动鼠标预览位置，左键确认放置。",
                _ => "选择工具：左键选中并拖动，按住左键框选，右键图形可删除。"
            };
            control.InvalidateSurface();
        }

        private static void OnSelectedShapeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is SkiaTrajectoryDesignerControl control)
            {
                if (control.SelectedShape != null)
                {
                    control.ClearSelectedShapes();
                }

                control.InvalidateSurface();
            }
        }

        private static void OnSelectedShapesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<EditableTrajectoryShape> oldSelectedShapes)
            {
                oldSelectedShapes.CollectionChanged -= control.OnSelectedShapesCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<EditableTrajectoryShape> newSelectedShapes)
            {
                newSelectedShapes.CollectionChanged += control.OnSelectedShapesCollectionChanged;
            }
            else
            {
                control.EnsureSelectedShapes();
            }

            control.InvalidateSurface();
        }

        private static void OnDefaultCenterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            if (IsFixedCenterTool(control.ActiveTool))
            {
                control._previewShape = control.CreateShape(control.ActiveTool, control.DefaultCenterX, control.DefaultCenterY);
            }

            control.InvalidateSurface();
        }

        private static void OnCoordinateBoundsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            control.ResetCoordinateViewport(updateStatus: false);
            control.ConstrainExistingShapeCenters();
            if (IsFixedCenterTool(control.ActiveTool))
            {
                control._previewShape = control.CreateShape(control.ActiveTool, control.DefaultCenterX, control.DefaultCenterY);
            }

            control.InvalidateSurface();
        }

        private static void OnIsPreviewOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not SkiaTrajectoryDesignerControl control)
            {
                return;
            }

            control.SyncPreviewOnlyState();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DetachShapes(Shapes);
            AttachShapes(Shapes);
            ConstrainExistingShapeCenters();
            SelectedShapes.CollectionChanged -= OnSelectedShapesCollectionChanged;
            SelectedShapes.CollectionChanged += OnSelectedShapesCollectionChanged;
            SyncPreviewOnlyState();
            SyncToolButtons();
            Focus();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            EndInteraction();
            DetachShapes(Shapes);
            SelectedShapes.CollectionChanged -= OnSelectedShapesCollectionChanged;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsPreviewOnly)
            {
                return;
            }

            if (e.Key != Key.Delete)
            {
                return;
            }

            int removedCount = DeleteSelectedShapes();
            if (removedCount == 0)
            {
                return;
            }

            StatusText = "已删除选中的图形。";
            e.Handled = true;
        }

        private void ToolButton_Checked(object sender, RoutedEventArgs e)
        {
            if (IsPreviewOnly)
            {
                return;
            }

            if (sender is RadioButton { Tag: TrajectoryDesignerTool tool })
            {
                ActiveTool = tool;
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomCoordinateViewport(CoordinateZoomStep, anchorScreenPoint: null);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomCoordinateViewport(1d / CoordinateZoomStep, anchorScreenPoint: null);
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCoordinateViewport(updateStatus: true);
        }

        private void DrawingSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawingSurface.Focus();
            Focus();

            Point point = e.GetPosition(DrawingSurface);

            if (IsPreviewOnly)
            {
                e.Handled = true;
                return;
            }

            if (!IsInsideCoordinatePlot(point))
            {
                StatusText = "请在坐标系范围内操作。";
                e.Handled = true;
                return;
            }

            if (ActiveTool == TrajectoryDesignerTool.Line)
            {
                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                if (!_lineStartCoordinate.HasValue)
                {
                    TryBeginLinePlacement(coordinateX, coordinateY);
                    StatusText = "已确定线段起点，移动鼠标预览线段，左键确定终点。";
                }
                else
                {
                    EditableTrajectoryShape shape = CompleteLinePlacement(coordinateX, coordinateY);
                    StatusText = $"已放置{GetShapeDisplayName(shape.Kind)}，已切换为选择工具。";
                }

                InvalidateSurface();
                e.Handled = true;
                return;
            }

            if (IsShapeTool(ActiveTool))
            {
                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                EditableTrajectoryShape shape = CreateShape(ActiveTool, coordinateX, coordinateY);
                Shapes?.Add(shape);
                SelectSingleShape(shape);
                ActiveTool = TrajectoryDesignerTool.Select;
                _previewShape = null;
                StatusText = $"已放置{GetShapeDisplayName(shape.Kind)}，已切换为选择工具。";
                InvalidateSurface();
                e.Handled = true;
                return;
            }

            var lineEndpointResize = HitTestLineEndpointHandle(point);
            if (lineEndpointResize.Shape != null)
            {
                SelectSingleShape(lineEndpointResize.Shape);
                _previewShape = null;
                BeginLineEndpointResize(lineEndpointResize.Shape, lineEndpointResize.Handle);
                StatusText = "正在调整线段端点，拖动起点或终点可拉长或缩短。";
                e.Handled = true;
                return;
            }

            if (ActiveTool == TrajectoryDesignerTool.Polyline)
            {
                if (!IsInsideCoordinatePlot(point))
                {
                    StatusText = "多线段顶点必须位于坐标系范围内。";
                    e.Handled = true;
                    return;
                }

                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                AddPolylinePlacementPoint(coordinateX, coordinateY);
                StatusText = _polylinePlacementPoints.Count == 1
                    ? "已确定多线段起点，左键继续添加顶点，右键结束。"
                    : $"已添加第 {_polylinePlacementPoints.Count} 个顶点，左键继续，右键结束。";
                InvalidateSurface();
                e.Handled = true;
                return;
            }

            EditableTrajectoryShape? circleRotationShape = HitTestCircleRotationHandle(point);
            if (circleRotationShape != null)
            {
                SelectSingleShape(circleRotationShape);
                _previewShape = null;
                BeginCircleRotation(circleRotationShape);
                StatusText = "正在旋转圆形，拖动旋转抓手可调整角度。";
                e.Handled = true;
                return;
            }

            EditableTrajectoryShape? radiusShape = HitTestCircleRadiusHandle(point);
            if (radiusShape != null)
            {
                SelectSingleShape(radiusShape);
                _previewShape = null;
                BeginCircleRadiusResize(radiusShape);
                StatusText = "正在调整圆半径，拖动抓手可改变大小。";
                e.Handled = true;
                return;
            }

            EditableTrajectoryShape? rotationShape = HitTestRectangleRotationHandle(point);
            if (rotationShape != null)
            {
                SelectSingleShape(rotationShape);
                _previewShape = null;
                BeginRectangleRotation(rotationShape);
                StatusText = "正在旋转矩形，拖动旋转抓手可调整角度。";
                e.Handled = true;
                return;
            }

            var rectangleResize = HitTestRectangleResizeHandle(point);
            if (rectangleResize.Shape != null)
            {
                SelectSingleShape(rectangleResize.Shape);
                _previewShape = null;
                BeginRectangleResize(rectangleResize.Shape, rectangleResize.Handle);
                StatusText = "正在调整矩形尺寸，拖动角点可改变长宽。";
                e.Handled = true;
                return;
            }

            var arcResize = HitTestArcResizeHandle(point);
            if (arcResize.Shape != null)
            {
                SelectSingleShape(arcResize.Shape);
                _previewShape = null;
                BeginArcResize(arcResize.Shape, arcResize.Handle);
                StatusText = "正在调整圆弧，拖动抓手可改变半径或弧度。";
                e.Handled = true;
                return;
            }

            EditableTrajectoryShape? hitShape = HitTest(point.X, point.Y);
            _previewShape = null;

            if (hitShape != null)
            {
                if (!IsShapeSelected(hitShape))
                {
                    SelectSingleShape(hitShape);
                }

                BeginDragSelectedShapes(hitShape, point);
                StatusText = GetDraggingStatusText();
            }
            else
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    BeginCoordinateViewportPan(point);
                    StatusText = "正在平移坐标系，按住 Shift + 左键拖动可移动视图。";
                }
                else
                {
                    BeginSelectionBox(point);
                    StatusText = "正在框选图形，拖动矩形覆盖需要选中的图形。";
                }
            }

            e.Handled = true;
        }

        private void DrawingSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging && !_isResizingShape && !_isPanningCoordinateViewport && !_isSelectingByBox)
            {
                return;
            }

            if (_isSelectingByBox)
            {
                int selectedCount = CompleteSelectionBox();
                StatusText = selectedCount > 0
                    ? $"已框选 {selectedCount} 个图形，可一起拖拽或按 Delete/右键删除。"
                    : "框选区域内没有图形。";
                e.Handled = true;
                return;
            }

            bool wasPanningCoordinateViewport = _isPanningCoordinateViewport;
            EndInteraction();
            StatusText = wasPanningCoordinateViewport
                ? "已确认坐标系平移。"
                : "已确认图形调整。";
            e.Handled = true;
        }

        private void DrawingSurface_MouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(DrawingSurface);

            if (_hasRightButtonPanCandidate)
            {
                if (e.RightButton != MouseButtonState.Pressed)
                {
                    EndRightButtonCoordinateViewportPan();
                    return;
                }

                if (!_isRightButtonPanningCoordinateViewport
                    && Distance(point.X, point.Y, _rightButtonDownPoint.X, _rightButtonDownPoint.Y) >= RightButtonPanThreshold)
                {
                    BeginRightButtonCoordinateViewportPan(_rightButtonDownPoint);
                }

                if (_isRightButtonPanningCoordinateViewport)
                {
                    UpdateRightButtonCoordinateViewportPan(point);
                    e.Handled = true;
                    return;
                }
            }

            if (_isResizingShape && _resizeShape != null)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    EndResize();
                    return;
                }

                UpdateShapeResizeFromScreenPoint(_resizeShape, point);
                StatusText = GetResizeStatusText(_resizeShape);
                e.Handled = true;
                return;
            }

            if (_isDragging)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    EndDrag();
                    return;
                }

                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                MoveDraggedShapes(coordinateX - _dragStartCoordinate.X, coordinateY - _dragStartCoordinate.Y);
                StatusText = GetDraggingStatusText();
                e.Handled = true;
                return;
            }

            if (_isSelectingByBox)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    CompleteSelectionBox();
                    return;
                }

                UpdateSelectionBox(point);
                int previewCount = GetShapesInsideSelectionBox(GetSelectionBoxRect()).Count;
                StatusText = previewCount > 0
                    ? $"正在框选图形：已覆盖 {previewCount} 个。"
                    : "正在框选图形。";
                e.Handled = true;
                return;
            }

            if (_isPanningCoordinateViewport)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    EndCoordinateViewportPan();
                    return;
                }

                UpdateCoordinateViewportPan(point);
                e.Handled = true;
                return;
            }

            if (ActiveTool == TrajectoryDesignerTool.Line && _lineStartCoordinate.HasValue)
            {
                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                _previewShape = CreateLineShapeFromEndpoints(
                    _lineStartCoordinate.Value.X,
                    _lineStartCoordinate.Value.Y,
                    coordinateX,
                    coordinateY);
                InvalidateSurface();
                e.Handled = true;
                return;
            }

            if (ActiveTool == TrajectoryDesignerTool.Polyline && _polylinePlacementPoints.Count > 0)
            {
                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                _previewShape = CreatePolylinePreviewShape(coordinateX, coordinateY);
                InvalidateSurface();
                e.Handled = true;
                return;
            }

            if (IsShapeTool(ActiveTool))
            {
                (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
                _previewShape = CreateShape(ActiveTool, coordinateX, coordinateY);
                InvalidateSurface();
                e.Handled = true;
            }
        }

        private void DrawingSurface_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point point = e.GetPosition(DrawingSurface);
            if (!IsInsideCoordinatePlot(point))
            {
                return;
            }

            double zoomFactor = e.Delta > 0 ? CoordinateZoomStep : 1d / CoordinateZoomStep;
            ZoomCoordinateViewport(zoomFactor, point);
            e.Handled = true;
        }

        private void DrawingSurface_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging || _isResizingShape || _isPanningCoordinateViewport || _isSelectingByBox || _hasRightButtonPanCandidate)
            {
                return;
            }

            if (_previewShape != null)
            {
                _previewShape = null;
                InvalidateSurface();
            }
        }

        private void DrawingSurface_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawingSurface.Focus();
            Focus();

            if (IsPreviewOnly)
            {
                e.Handled = true;
                return;
            }

            if (ActiveTool == TrajectoryDesignerTool.Polyline)
            {
                if (_polylinePlacementPoints.Count >= 2)
                {
                    EditableTrajectoryShape shape = CompletePolylinePlacement();
                    StatusText = $"已放置{GetShapeDisplayName(shape.Kind)}，已切换为选择工具。";
                }
                else
                {
                    _polylinePlacementPoints.Clear();
                    _previewShape = null;
                    StatusText = "多线段至少需要两个顶点，已取消当前绘制。";
                }

                InvalidateSurface();
                e.Handled = true;
                return;
            }

            Point point = e.GetPosition(DrawingSurface);
            if (!IsInsideCoordinatePlot(point))
            {
                StatusText = "请在坐标系范围内操作。";
                e.Handled = true;
                return;
            }

            EndInteraction();
            _previewShape = null;
            _rightButtonDownPoint = point;
            _rightButtonDownShape = HitTest(point.X, point.Y);
            _hasRightButtonPanCandidate = true;
            DrawingSurface.CaptureMouse();
            StatusText = _rightButtonDownShape == null
                ? "按住右键拖动可平移坐标系。"
                : "右键拖动可平移坐标系，松开可打开删除菜单。";
            e.Handled = true;
        }

        private void DrawingSurface_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsPreviewOnly)
            {
                e.Handled = true;
                return;
            }

            if (!_hasRightButtonPanCandidate)
            {
                return;
            }

            if (_isRightButtonPanningCoordinateViewport)
            {
                EndRightButtonCoordinateViewportPan();
                StatusText = "已确认坐标系平移。";
                e.Handled = true;
                return;
            }

            if (_rightButtonDownShape != null)
            {
                if (!IsShapeSelected(_rightButtonDownShape))
                {
                    SelectSingleShape(_rightButtonDownShape);
                }

                ShowDeleteMenu(_rightButtonDownShape);
                StatusText = SelectedShapes.Count > 1
                    ? $"已选中 {SelectedShapes.Count} 个图形，可在右键菜单中一起删除。"
                    : "已选中图形，可在右键菜单中删除。";
            }
            else
            {
                StatusText = "右键拖动可平移坐标系，左键拖动空白处可框选图形。";
            }

            EndRightButtonCoordinateViewportPan();
            e.Handled = true;
        }

        private void DrawingSurface_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(new SKColor(247, 250, 253));

            double scaleX = e.Info.Width / Math.Max(DrawingSurface.ActualWidth, 1d);
            double scaleY = e.Info.Height / Math.Max(DrawingSurface.ActualHeight, 1d);
            canvas.Scale((float)scaleX, (float)scaleY);

            double viewportWidth = e.Info.Width / scaleX;
            double viewportHeight = e.Info.Height / scaleY;
            DrawGrid(canvas);
            DrawCoordinateTicks(canvas, viewportWidth, viewportHeight);

            (double centerX, double centerY) = ClampPointToCoordinateBounds(DefaultCenterX, DefaultCenterY);

            foreach (EditableTrajectoryShape shape in Shapes ?? Enumerable.Empty<EditableTrajectoryShape>())
            {
                DrawShapeScreen(canvas, shape, IsShapeSelected(shape));
            }

            if (_previewShape != null)
            {
                DrawShapeScreen(canvas, _previewShape, false, true);
            }

            Point centerScreen = CoordinateToScreen(centerX, centerY);
            DrawDefaultCenter(canvas, centerScreen.X, centerScreen.Y);

            foreach (EditableTrajectoryShape shape in GetSelectedCircleShapes())
            {
                DrawCircleRadiusHandle(canvas, shape);
                DrawCircleRotationHandle(canvas, shape);
            }

            foreach (EditableTrajectoryShape shape in GetSelectedRectangleShapes())
            {
                DrawRectangleResizeHandles(canvas, shape);
            }

            foreach (EditableTrajectoryShape shape in GetSelectedArcShapes())
            {
                DrawArcResizeHandles(canvas, shape);
            }

            foreach (EditableTrajectoryShape shape in GetSelectedLineShapes())
            {
                DrawLineEndpointHandles(canvas, shape);
            }

            foreach (EditableTrajectoryShape shape in Shapes ?? Enumerable.Empty<EditableTrajectoryShape>())
            {
                DrawShapeDimensionLabel(canvas, shape);
                DrawShapeCenterCoordinateLabel(canvas, shape);
            }

            if (_previewShape != null)
            {
                DrawShapeDimensionLabel(canvas, _previewShape);
                DrawShapeCenterCoordinateLabel(canvas, _previewShape);
            }

            DrawSelectionBox(canvas);
        }

        private void AttachShapes(ObservableCollection<EditableTrajectoryShape>? shapes)
        {
            if (shapes == null)
            {
                return;
            }

            shapes.CollectionChanged += OnShapesCollectionChanged;
            foreach (EditableTrajectoryShape shape in shapes)
            {
                shape.PropertyChanged += OnShapePropertyChanged;
            }
        }

        private void DetachShapes(ObservableCollection<EditableTrajectoryShape>? shapes)
        {
            if (shapes == null)
            {
                return;
            }

            shapes.CollectionChanged -= OnShapesCollectionChanged;
            foreach (EditableTrajectoryShape shape in shapes)
            {
                shape.PropertyChanged -= OnShapePropertyChanged;
            }
        }

        private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (EditableTrajectoryShape shape in e.OldItems.OfType<EditableTrajectoryShape>())
                {
                    shape.PropertyChanged -= OnShapePropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (EditableTrajectoryShape shape in e.NewItems.OfType<EditableTrajectoryShape>())
                {
                    shape.PropertyChanged += OnShapePropertyChanged;
                    ClampShapeToCoordinateBounds(shape);
                }
            }

            PruneSelectionToExistingShapes();
            InvalidateSurface();
        }

        private void OnSelectedShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateSurface();
        }

        private void OnShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is EditableTrajectoryShape shape)
            {
                ClampShapeToCoordinateBounds(shape);
            }

            InvalidateSurface();
        }

        private void SyncToolButtons()
        {
            if (!IsLoaded)
            {
                return;
            }

            SelectToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Select;
            PointToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Point;
            LineToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Line;
            PolylineToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Polyline;
            CircleToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Circle;
            RectangleToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Rectangle;
            ArcToolButton.IsChecked = ActiveTool == TrajectoryDesignerTool.Arc;
        }

        private void SyncPreviewOnlyState()
        {
            if (IsPreviewOnly)
            {
                EndInteraction();
                _lineStartCoordinate = null;
                _polylinePlacementPoints.Clear();
                _previewShape = null;
                SelectedShape = null;
                ClearSelectedShapes();
                ActiveTool = TrajectoryDesignerTool.Select;
                StatusText = "轨迹预览：仅显示设计结果，可使用滚轮或按钮缩放查看。";
            }

            if (IsLoaded)
            {
                ToolStackPanel.Visibility = IsPreviewOnly ? Visibility.Collapsed : Visibility.Visible;
                SyncToolButtons();
            }

            InvalidateSurface();
        }

        private EditableTrajectoryShape CreateShape(TrajectoryDesignerTool tool, double x, double y)
        {
            double shapeX = IsFixedCenterTool(tool) ? DefaultCenterX : x;
            double shapeY = IsFixedCenterTool(tool) ? DefaultCenterY : y;
            (shapeX, shapeY) = ClampPointToCoordinateBounds(shapeX, shapeY);

            EditableTrajectoryShape shape = tool switch
            {
                TrajectoryDesignerTool.Point => new EditableTrajectoryShape
                {
                    Kind = TrajectoryShapeKind.Point,
                    X = shapeX,
                    Y = shapeY,
                    Scale = 1d
                },
                TrajectoryDesignerTool.Line => CreateLineShapeFromEndpoints(shapeX, shapeY, shapeX + 40d, shapeY),
                TrajectoryDesignerTool.Rectangle => new EditableTrajectoryShape
                {
                    Kind = TrajectoryShapeKind.Rectangle,
                    X = shapeX,
                    Y = shapeY,
                    Width = NormalizePositive(DefaultRectangleWidth, 90d),
                    Height = NormalizePositive(DefaultRectangleHeight, 60d),
                    Scale = 1d
                },
                TrajectoryDesignerTool.Arc => new EditableTrajectoryShape
                {
                    Kind = TrajectoryShapeKind.Arc,
                    X = shapeX,
                    Y = shapeY,
                    Radius = NormalizePositive(DefaultArcRadius, 48d),
                    StartAngle = DefaultArcStartAngle,
                    SweepAngle = NormalizeSweep(DefaultArcSweepAngle),
                    Scale = 1d
                },
                _ => new EditableTrajectoryShape
                {
                    Kind = TrajectoryShapeKind.Circle,
                    X = shapeX,
                    Y = shapeY,
                    Radius = NormalizePositive(DefaultCircleRadius, 40d),
                    Scale = 1d
                }
            };

            ClampShapeToCoordinateBounds(shape);
            return shape;
        }

        private bool TryBeginLinePlacement(double x, double y)
        {
            _lineStartCoordinate = ClampPointToCoordinateBounds(x, y);
            _previewShape = null;
            return true;
        }

        private EditableTrajectoryShape CompleteLinePlacement(double x, double y)
        {
            (double startX, double startY) = _lineStartCoordinate ?? ClampPointToCoordinateBounds(x, y);
            (double endX, double endY) = ClampPointToCoordinateBounds(x, y);
            EditableTrajectoryShape shape = CreateLineShapeFromEndpoints(startX, startY, endX, endY);
            Shapes?.Add(shape);
            SelectSingleShape(shape);
            ActiveTool = TrajectoryDesignerTool.Select;
            _lineStartCoordinate = null;
            _previewShape = null;
            return shape;
        }

        private EditableTrajectoryShape CreateLineShapeFromEndpoints(double startX, double startY, double endX, double endY)
        {
            (startX, startY) = ClampPointToCoordinateBounds(startX, startY);
            (endX, endY) = ClampPointToCoordinateBounds(endX, endY);
            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double length = Math.Max(Distance(startX, startY, endX, endY), 1d);
            return new EditableTrajectoryShape
            {
                Kind = TrajectoryShapeKind.Line,
                X = (startX + endX) / 2d,
                Y = (startY + endY) / 2d,
                Width = length,
                Height = 1d,
                RotationAngle = NormalizeAngle(Math.Atan2(deltaY, deltaX) * 180d / Math.PI),
                Scale = 1d
            };
        }

        private void AddPolylinePlacementPoint(double x, double y)
        {
            (double boundedX, double boundedY) = ClampPointToCoordinateBounds(x, y);
            _polylinePlacementPoints.Add(new TrajectoryPoint { X = boundedX, Y = boundedY });
            _previewShape = CreatePolylineShape(_polylinePlacementPoints);
        }

        private EditableTrajectoryShape CreatePolylinePreviewShape(double x, double y)
        {
            var points = _polylinePlacementPoints.Select(point => point.Clone()).ToList();
            (double boundedX, double boundedY) = ClampPointToCoordinateBounds(x, y);
            if (points.Count == 0
                || !NearlyEqual(points[^1].X, boundedX)
                || !NearlyEqual(points[^1].Y, boundedY))
            {
                points.Add(new TrajectoryPoint { X = boundedX, Y = boundedY });
            }

            return CreatePolylineShape(points);
        }

        private EditableTrajectoryShape CompletePolylinePlacement()
        {
            EditableTrajectoryShape shape = CreatePolylineShape(_polylinePlacementPoints);
            Shapes?.Add(shape);
            SelectSingleShape(shape);
            _polylinePlacementPoints.Clear();
            _previewShape = null;
            ActiveTool = TrajectoryDesignerTool.Select;
            return shape;
        }

        private EditableTrajectoryShape CreatePolylineShape(IEnumerable<TrajectoryPoint> points)
        {
            List<TrajectoryPoint> boundedPoints = (points ?? Enumerable.Empty<TrajectoryPoint>())
                .Where(point => point != null && double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Select(point =>
                {
                    (double x, double y) = ClampPointToCoordinateBounds(point.X, point.Y);
                    return new TrajectoryPoint { X = x, Y = y };
                })
                .ToList();

            (double centerX, double centerY) = GetPolylineCenter(boundedPoints);
            return new EditableTrajectoryShape
            {
                Kind = TrajectoryShapeKind.Polyline,
                X = centerX,
                Y = centerY,
                PolylinePoints = boundedPoints,
                Scale = 1d
            };
        }

        private static (double X, double Y) GetPolylineCenter(IReadOnlyCollection<TrajectoryPoint> points)
        {
            if (points.Count == 0)
            {
                return (0d, 0d);
            }

            return (
                (points.Min(point => point.X) + points.Max(point => point.X)) / 2d,
                (points.Min(point => point.Y) + points.Max(point => point.Y)) / 2d);
        }

        private EditableTrajectoryShape? HitTest(double screenX, double screenY)
        {
            if (Shapes == null)
            {
                return null;
            }

            for (int index = Shapes.Count - 1; index >= 0; index--)
            {
                EditableTrajectoryShape shape = Shapes[index];
                if (ContainsScreenPoint(shape, screenX, screenY))
                {
                    return shape;
                }
            }

            return null;
        }

        private EditableTrajectoryShape? HitTestCircleRadiusHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedCircleShapes())
            {
                Point handlePoint = GetCircleRadiusHandleScreenPoint(shape);
                if (Distance(screenPoint.X, screenPoint.Y, handlePoint.X, handlePoint.Y) <= ResizeHandleHitRadius)
                {
                    return shape;
                }
            }

            return null;
        }

        private EditableTrajectoryShape? HitTestCircleRotationHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedCircleShapes())
            {
                Point handlePoint = GetCircleRotationHandlePoint(shape);
                if (Distance(screenPoint.X, screenPoint.Y, handlePoint.X, handlePoint.Y) <= ResizeHandleHitRadius)
                {
                    return shape;
                }
            }

            return null;
        }

        private EditableTrajectoryShape? HitTestRectangleRotationHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedRectangleShapes())
            {
                Point handlePoint = GetRectangleRotationHandlePoint(shape);
                if (Distance(screenPoint.X, screenPoint.Y, handlePoint.X, handlePoint.Y) <= ResizeHandleHitRadius)
                {
                    return shape;
                }
            }

            return null;
        }

        private (EditableTrajectoryShape? Shape, TrajectoryResizeHandleKind Handle) HitTestLineEndpointHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedLineShapes())
            {
                foreach (var handle in GetLineEndpointHandlePoints(shape))
                {
                    if (Distance(screenPoint.X, screenPoint.Y, handle.Point.X, handle.Point.Y) <= ResizeHandleHitRadius)
                    {
                        return (shape, handle.Kind);
                    }
                }
            }

            return (null, TrajectoryResizeHandleKind.None);
        }

        private (EditableTrajectoryShape? Shape, TrajectoryResizeHandleKind Handle) HitTestRectangleResizeHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedRectangleShapes())
            {
                foreach (var handle in GetRectangleResizeHandlePoints(shape))
                {
                    if (Distance(screenPoint.X, screenPoint.Y, handle.Point.X, handle.Point.Y) <= ResizeHandleHitRadius)
                    {
                        return (shape, handle.Kind);
                    }
                }
            }

            return (null, TrajectoryResizeHandleKind.None);
        }

        private (EditableTrajectoryShape? Shape, TrajectoryResizeHandleKind Handle) HitTestArcResizeHandle(Point screenPoint)
        {
            foreach (EditableTrajectoryShape shape in GetSelectedArcShapes())
            {
                foreach (var handle in GetArcResizeHandlePoints(shape))
                {
                    if (Distance(screenPoint.X, screenPoint.Y, handle.Point.X, handle.Point.Y) <= ResizeHandleHitRadius)
                    {
                        return (shape, handle.Kind);
                    }
                }
            }

            return (null, TrajectoryResizeHandleKind.None);
        }

        private void SelectSingleShape(EditableTrajectoryShape? shape)
        {
            ClearSelectedShapes();
            SelectedShape = shape;
        }

        private void SelectAllShapes()
        {
            ClearSelectedShapes();
            SelectedShape = null;

            if (Shapes == null)
            {
                return;
            }

            foreach (EditableTrajectoryShape shape in Shapes)
            {
                SelectedShapes.Add(shape);
            }

            InvalidateSurface();
        }

        private void ClearSelectedShapes()
        {
            if (SelectedShapes.Count > 0)
            {
                SelectedShapes.Clear();
            }
        }

        private void SetSelectionFromShapes(IEnumerable<EditableTrajectoryShape> shapes)
        {
            List<EditableTrajectoryShape> selected = shapes
                .Where(shape => Shapes?.Contains(shape) == true)
                .Distinct()
                .ToList();

            ClearSelectedShapes();

            if (selected.Count == 1)
            {
                SelectedShape = selected[0];
                return;
            }

            SelectedShape = null;
            foreach (EditableTrajectoryShape shape in selected)
            {
                SelectedShapes.Add(shape);
            }

            InvalidateSurface();
        }

        private void BeginSelectionBox(Point point)
        {
            _selectionBoxStartPoint = point;
            _selectionBoxCurrentPoint = point;
            _isSelectingByBox = true;
            DrawingSurface.CaptureMouse();
            InvalidateSurface();
        }

        private void UpdateSelectionBox(Point point)
        {
            _selectionBoxCurrentPoint = point;
            InvalidateSurface();
        }

        private int CompleteSelectionBox()
        {
            if (!_isSelectingByBox)
            {
                return 0;
            }

            Rect selectionRect = GetSelectionBoxRect();
            List<EditableTrajectoryShape> selected = selectionRect.Width < 4d && selectionRect.Height < 4d
                ? new List<EditableTrajectoryShape>()
                : GetShapesInsideSelectionBox(selectionRect);

            SetSelectionFromShapes(selected);
            EndSelectionBox();
            return selected.Count;
        }

        private void EndSelectionBox()
        {
            if (!_isSelectingByBox)
            {
                return;
            }

            _isSelectingByBox = false;
            _selectionBoxStartPoint = default;
            _selectionBoxCurrentPoint = default;
            DrawingSurface?.ReleaseMouseCapture();
            InvalidateSurface();
        }

        private Rect GetSelectionBoxRect()
        {
            return new Rect(_selectionBoxStartPoint, _selectionBoxCurrentPoint);
        }

        private List<EditableTrajectoryShape> GetShapesInsideSelectionBox(Rect selectionRect)
        {
            if (Shapes == null || selectionRect.IsEmpty)
            {
                return new List<EditableTrajectoryShape>();
            }

            return Shapes
                .Where(shape => selectionRect.IntersectsWith(GetShapeScreenBounds(shape)))
                .ToList();
        }

        private int DeleteSelectedShapes()
        {
            List<EditableTrajectoryShape> shapesToDelete = SelectedShapes.Count > 0
                ? SelectedShapes.ToList()
                : SelectedShape == null ? new List<EditableTrajectoryShape>() : new List<EditableTrajectoryShape> { SelectedShape };

            foreach (EditableTrajectoryShape shape in shapesToDelete)
            {
                if (Shapes?.Contains(shape) == true)
                {
                    Shapes.Remove(shape);
                }
            }

            SelectedShape = null;
            ClearSelectedShapes();
            return shapesToDelete.Count;
        }

        private void PruneSelectionToExistingShapes()
        {
            if (Shapes == null)
            {
                SelectedShape = null;
                ClearSelectedShapes();
                return;
            }

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

        private bool IsShapeSelected(EditableTrajectoryShape shape)
        {
            return ReferenceEquals(shape, SelectedShape) || SelectedShapes.Contains(shape);
        }

        private IEnumerable<EditableTrajectoryShape> GetSelectedCircleShapes()
        {
            if (SelectedShape?.Kind == TrajectoryShapeKind.Circle)
            {
                yield return SelectedShape;
            }

            foreach (EditableTrajectoryShape shape in EnsureSelectedShapes())
            {
                if (shape.Kind == TrajectoryShapeKind.Circle)
                {
                    yield return shape;
                }
            }
        }

        private IEnumerable<EditableTrajectoryShape> GetSelectedRectangleShapes()
        {
            if (SelectedShape?.Kind == TrajectoryShapeKind.Rectangle)
            {
                yield return SelectedShape;
            }

            foreach (EditableTrajectoryShape shape in EnsureSelectedShapes())
            {
                if (shape.Kind == TrajectoryShapeKind.Rectangle)
                {
                    yield return shape;
                }
            }
        }

        private IEnumerable<EditableTrajectoryShape> GetSelectedArcShapes()
        {
            if (SelectedShape?.Kind == TrajectoryShapeKind.Arc)
            {
                yield return SelectedShape;
            }

            foreach (EditableTrajectoryShape shape in EnsureSelectedShapes())
            {
                if (shape.Kind == TrajectoryShapeKind.Arc)
                {
                    yield return shape;
                }
            }
        }

        private IEnumerable<EditableTrajectoryShape> GetSelectedLineShapes()
        {
            if (SelectedShape?.Kind == TrajectoryShapeKind.Line)
            {
                yield return SelectedShape;
            }

            foreach (EditableTrajectoryShape shape in EnsureSelectedShapes())
            {
                if (shape.Kind == TrajectoryShapeKind.Line)
                {
                    yield return shape;
                }
            }
        }

        private void ConstrainExistingShapeCenters()
        {
            if (Shapes == null)
            {
                return;
            }

            foreach (EditableTrajectoryShape shape in Shapes)
            {
                ClampShapeToCoordinateBounds(shape);
            }
        }

        private void ClampShapeCenter(EditableTrajectoryShape shape)
        {
            ClampShapeToCoordinateBounds(shape);
        }

        private void ClampShapeToCoordinateBounds(EditableTrajectoryShape shape)
        {
            if (!_constrainingShapes.Add(shape))
            {
                return;
            }

            try
            {
                switch (shape.Kind)
                {
                    case TrajectoryShapeKind.Line:
                        ClampLineShape(shape);
                        break;
                    case TrajectoryShapeKind.Polyline:
                        ClampPolylineShape(shape);
                        break;
                    case TrajectoryShapeKind.Circle:
                    case TrajectoryShapeKind.Arc:
                        ClampCircularShape(shape);
                        break;
                    case TrajectoryShapeKind.Rectangle:
                        ClampRectangleShape(shape);
                        break;
                    default:
                        SetShapeCenter(shape, ClampPointToCoordinateBounds(shape.X, shape.Y));
                        break;
                }
            }
            finally
            {
                _constrainingShapes.Remove(shape);
            }
        }

        private void ClampLineShape(EditableTrajectoryShape shape)
        {
            var endpoints = GetLineEndpointCoordinates(shape);
            EditableTrajectoryShape bounded = CreateLineShapeFromEndpoints(
                endpoints.StartX,
                endpoints.StartY,
                endpoints.EndX,
                endpoints.EndY);
            shape.X = bounded.X;
            shape.Y = bounded.Y;
            shape.Width = bounded.Width;
            shape.Height = bounded.Height;
            shape.RotationAngle = bounded.RotationAngle;
        }

        private void ClampPolylineShape(EditableTrajectoryShape shape)
        {
            List<TrajectoryPoint> points = shape.PolylinePoints ?? new List<TrajectoryPoint>();
            foreach (TrajectoryPoint point in points.Where(point => point != null))
            {
                (point.X, point.Y) = ClampPointToCoordinateBounds(point.X, point.Y);
            }

            if (points.Count > 0)
            {
                SetShapeCenter(shape, GetPolylineCenter(points));
            }
            else
            {
                SetShapeCenter(shape, ClampPointToCoordinateBounds(shape.X, shape.Y));
            }
        }

        private void ClampCircularShape(EditableTrajectoryShape shape)
        {
            SetShapeCenter(shape, ClampPointToCoordinateBounds(shape.X, shape.Y));
            (double minX, double minY, double maxX, double maxY) = GetCoordinateBounds();
            double maximumScaledRadius = Math.Max(
                Math.Min(
                    Math.Min(shape.X - minX, maxX - shape.X),
                    Math.Min(shape.Y - minY, maxY - shape.Y)),
                0d);
            double boundedRadius = Math.Min(GetScaledRadius(shape), maximumScaledRadius) / GetScale(shape);
            shape.Radius = Math.Max(boundedRadius, 1d);
        }

        private void ClampRectangleShape(EditableTrajectoryShape shape)
        {
            (double minX, double minY, double maxX, double maxY) = GetCoordinateBounds();
            double rangeX = maxX - minX;
            double rangeY = maxY - minY;
            double angle = shape.RotationAngle * Math.PI / 180d;
            double cos = Math.Abs(Math.Cos(angle));
            double sin = Math.Abs(Math.Sin(angle));
            double scaledWidth = GetScaledWidth(shape);
            double scaledHeight = GetScaledHeight(shape);
            double projectedWidth = (scaledWidth * cos) + (scaledHeight * sin);
            double projectedHeight = (scaledWidth * sin) + (scaledHeight * cos);
            double fitFactor = Math.Min(
                projectedWidth > 0d ? rangeX / projectedWidth : 1d,
                projectedHeight > 0d ? rangeY / projectedHeight : 1d);

            if (fitFactor < 1d)
            {
                shape.Width = Math.Max(shape.Width * Math.Max(fitFactor, 0d), 1d);
                shape.Height = Math.Max(shape.Height * Math.Max(fitFactor, 0d), 1d);
                scaledWidth = GetScaledWidth(shape);
                scaledHeight = GetScaledHeight(shape);
            }

            double halfWidth = ((scaledWidth * cos) + (scaledHeight * sin)) / 2d;
            double halfHeight = ((scaledWidth * sin) + (scaledHeight * cos)) / 2d;
            SetShapeCenter(
                shape,
                (
                    ClampCenterToRange(shape.X, minX + halfWidth, maxX - halfWidth),
                    ClampCenterToRange(shape.Y, minY + halfHeight, maxY - halfHeight)));
        }

        private static double ClampCenterToRange(double value, double minimum, double maximum)
        {
            return minimum > maximum
                ? (minimum + maximum) / 2d
                : Math.Clamp(value, minimum, maximum);
        }

        private static void SetShapeCenter(EditableTrajectoryShape shape, (double X, double Y) center)
        {
            if (!NearlyEqual(shape.X, center.X))
            {
                shape.X = center.X;
            }

            if (!NearlyEqual(shape.Y, center.Y))
            {
                shape.Y = center.Y;
            }
        }

        private (double X, double Y) ClampPointToCoordinateBounds(double x, double y)
        {
            (double minX, double minY, double maxX, double maxY) = GetCoordinateBounds();
            return (Math.Clamp(x, minX, maxX), Math.Clamp(y, minY, maxY));
        }

        private (double MinX, double MinY, double MaxX, double MaxY) GetCoordinateBounds()
        {
            double startX = NormalizeCoordinateValue(CoordinateStartX, 0d);
            double startY = NormalizeCoordinateValue(CoordinateStartY, 0d);
            double endX = NormalizeCoordinateValue(CoordinateEndX, startX);
            double endY = NormalizeCoordinateValue(CoordinateEndY, startY);
            return (
                Math.Min(startX, endX),
                Math.Min(startY, endY),
                Math.Max(startX, endX),
                Math.Max(startY, endY));
        }

        private (double StartX, double StartY, double EndX, double EndY) GetFullCoordinateWindow()
        {
            double startX = NormalizeCoordinateValue(CoordinateStartX, 0d);
            double startY = NormalizeCoordinateValue(CoordinateStartY, 0d);
            double endX = NormalizeCoordinateValue(CoordinateEndX, startX + 1d);
            double endY = NormalizeCoordinateValue(CoordinateEndY, startY + 1d);

            if (NearlyEqual(startX, endX))
            {
                endX = startX + 1d;
            }

            if (NearlyEqual(startY, endY))
            {
                endY = startY + 1d;
            }

            return (startX, startY, endX, endY);
        }

        private (double StartX, double StartY, double EndX, double EndY) GetVisibleCoordinateWindow()
        {
            (double fullStartX, double fullStartY, double fullEndX, double fullEndY) = GetFullCoordinateWindow();
            double startX = NormalizeCoordinateValue(_viewStartX ?? fullStartX, fullStartX);
            double startY = NormalizeCoordinateValue(_viewStartY ?? fullStartY, fullStartY);
            double endX = NormalizeCoordinateValue(_viewEndX ?? fullEndX, fullEndX);
            double endY = NormalizeCoordinateValue(_viewEndY ?? fullEndY, fullEndY);

            if (NearlyEqual(startX, endX))
            {
                endX = startX + Math.Sign(fullEndX - fullStartX == 0d ? 1d : fullEndX - fullStartX);
            }

            if (NearlyEqual(startY, endY))
            {
                endY = startY + Math.Sign(fullEndY - fullStartY == 0d ? 1d : fullEndY - fullStartY);
            }

            return ClampViewportToCoordinateBounds(startX, startY, endX, endY);
        }

        private void ZoomCoordinateViewport(double zoomFactor, Point? anchorScreenPoint)
        {
            if (!double.IsFinite(zoomFactor) || zoomFactor <= 0d || NearlyEqual(zoomFactor, 1d))
            {
                return;
            }

            (double viewStartX, double viewStartY, double viewEndX, double viewEndY) = GetVisibleCoordinateWindow();
            double rangeX = viewEndX - viewStartX;
            double rangeY = viewEndY - viewStartY;
            if (Math.Abs(rangeX) < 1e-9d || Math.Abs(rangeY) < 1e-9d)
            {
                return;
            }

            (double anchorX, double anchorY) = anchorScreenPoint.HasValue
                ? ScreenToCoordinate(anchorScreenPoint.Value.X, anchorScreenPoint.Value.Y)
                : ((viewStartX + viewEndX) / 2d, (viewStartY + viewEndY) / 2d);

            double ratioX = (anchorX - viewStartX) / rangeX;
            double ratioY = (anchorY - viewStartY) / rangeY;
            (double fullStartX, double fullStartY, double fullEndX, double fullEndY) = GetFullCoordinateWindow();
            double newRangeX = ClampViewportRange(rangeX / zoomFactor, fullEndX - fullStartX);
            double newRangeY = ClampViewportRange(rangeY / zoomFactor, fullEndY - fullStartY);

            double newStartX = anchorX - (ratioX * newRangeX);
            double newStartY = anchorY - (ratioY * newRangeY);
            (double startX, double startY, double endX, double endY) = ClampViewportToCoordinateBounds(
                newStartX,
                newStartY,
                newStartX + newRangeX,
                newStartY + newRangeY);

            if (IsFullCoordinateViewport(startX, startY, endX, endY))
            {
                _viewStartX = null;
                _viewStartY = null;
                _viewEndX = null;
                _viewEndY = null;
            }
            else
            {
                _viewStartX = startX;
                _viewStartY = startY;
                _viewEndX = endX;
                _viewEndY = endY;
            }

            StatusText = $"坐标系缩放：{GetCoordinateViewportZoomFactor():0.##}x，点击还原可恢复完整范围。";
            InvalidateSurface();
        }

        private void ResetCoordinateViewport(bool updateStatus)
        {
            _viewStartX = null;
            _viewStartY = null;
            _viewEndX = null;
            _viewEndY = null;

            if (updateStatus)
            {
                StatusText = "已还原坐标系视图到完整范围。";
            }

            InvalidateSurface();
        }

        private void BeginCoordinateViewportPan(Point point)
        {
            _viewportPanStartPoint = point;
            _viewportPanStartWindow = GetVisibleCoordinateWindow();
            _isPanningCoordinateViewport = true;
            DrawingSurface.Cursor = Cursors.SizeAll;
            DrawingSurface.CaptureMouse();
        }

        private void BeginRightButtonCoordinateViewportPan(Point point)
        {
            _isRightButtonPanningCoordinateViewport = true;
            BeginCoordinateViewportPan(point);
        }

        private void UpdateCoordinateViewportPan(Point point)
        {
            CoordinateScreenTransform transform = GetCoordinateScreenTransform();
            double rangeX = _viewportPanStartWindow.EndX - _viewportPanStartWindow.StartX;
            double rangeY = _viewportPanStartWindow.EndY - _viewportPanStartWindow.StartY;
            double deltaX = -((point.X - _viewportPanStartPoint.X) / transform.PlotWidth) * rangeX;
            double deltaY = ((point.Y - _viewportPanStartPoint.Y) / transform.PlotHeight) * rangeY;

            PanCoordinateViewport(deltaX, deltaY);
        }

        private void UpdateRightButtonCoordinateViewportPan(Point point)
        {
            UpdateCoordinateViewportPan(point);
        }

        private void PanCoordinateViewport(double deltaX, double deltaY)
        {
            SetVisibleCoordinateWindow(
                _viewportPanStartWindow.StartX + deltaX,
                _viewportPanStartWindow.StartY + deltaY,
                _viewportPanStartWindow.EndX + deltaX,
                _viewportPanStartWindow.EndY + deltaY);

            var visibleWindow = GetVisibleCoordinateWindow();
            StatusText = $"正在平移坐标系：X={Math.Min(visibleWindow.StartX, visibleWindow.EndX):F1}~{Math.Max(visibleWindow.StartX, visibleWindow.EndX):F1}";
        }

        private void SetVisibleCoordinateWindow(double startX, double startY, double endX, double endY)
        {
            (startX, startY, endX, endY) = ClampViewportToCoordinateBounds(startX, startY, endX, endY);

            if (IsFullCoordinateViewport(startX, startY, endX, endY))
            {
                _viewStartX = null;
                _viewStartY = null;
                _viewEndX = null;
                _viewEndY = null;
            }
            else
            {
                _viewStartX = startX;
                _viewStartY = startY;
                _viewEndX = endX;
                _viewEndY = endY;
            }

            InvalidateSurface();
        }

        private (double StartX, double StartY, double EndX, double EndY) ClampViewportToCoordinateBounds(
            double startX,
            double startY,
            double endX,
            double endY)
        {
            (double fullStartX, double fullStartY, double fullEndX, double fullEndY) = GetFullCoordinateWindow();
            (double clampedStartX, double clampedEndX) = ClampViewportAxisToBounds(startX, endX, fullStartX, fullEndX);
            (double clampedStartY, double clampedEndY) = ClampViewportAxisToBounds(startY, endY, fullStartY, fullEndY);
            return (clampedStartX, clampedStartY, clampedEndX, clampedEndY);
        }

        private bool IsFullCoordinateViewport(double startX, double startY, double endX, double endY)
        {
            (double fullStartX, double fullStartY, double fullEndX, double fullEndY) = GetFullCoordinateWindow();
            return NearlyEqual(startX, fullStartX)
                && NearlyEqual(startY, fullStartY)
                && NearlyEqual(endX, fullEndX)
                && NearlyEqual(endY, fullEndY);
        }

        private double GetCoordinateViewportZoomFactor()
        {
            (double fullStartX, double fullStartY, double fullEndX, double fullEndY) = GetFullCoordinateWindow();
            (double viewStartX, double viewStartY, double viewEndX, double viewEndY) = GetVisibleCoordinateWindow();
            double zoomX = Math.Abs((fullEndX - fullStartX) / (viewEndX - viewStartX));
            double zoomY = Math.Abs((fullEndY - fullStartY) / (viewEndY - viewStartY));
            return Math.Max(Math.Min(zoomX, zoomY), 1d);
        }

        private CoordinateScreenTransform GetCoordinateScreenTransform()
        {
            var visibleWindow = GetVisibleCoordinateWindow();
            return TrajectoryCoordinateMapper.GetTransform(
                GetViewportWidth(),
                GetViewportHeight(),
                visibleWindow.StartX,
                visibleWindow.StartY,
                visibleWindow.EndX,
                visibleWindow.EndY);
        }

        private (double X, double Y) ScreenToCoordinate(double screenX, double screenY)
        {
            return TrajectoryCoordinateMapper.ScreenToCoordinate(
                GetCoordinateScreenTransform(),
                screenX,
                screenY);
        }

        private bool IsInsideCoordinatePlot(Point point)
        {
            CoordinateScreenTransform transform = GetCoordinateScreenTransform();
            return point.X >= transform.PlotLeft
                && point.X <= transform.PlotLeft + transform.PlotWidth
                && point.Y >= transform.PlotTop
                && point.Y <= transform.PlotTop + transform.PlotHeight;
        }

        private Point CoordinateToScreen(double x, double y)
        {
            var point = TrajectoryCoordinateMapper.CoordinateToScreen(GetCoordinateScreenTransform(), x, y);
            return new Point(point.X, point.Y);
        }

        private double CoordinateLengthToScreenRadius(double coordinateLength)
        {
            return TrajectoryCoordinateMapper.LengthToScreen(GetCoordinateScreenTransform(), coordinateLength);
        }

        private (double X, double Y) ToVisualLocalCoordinate(EditableTrajectoryShape shape, double screenX, double screenY)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double unitScale = Math.Max(CoordinateLengthToScreenRadius(1d), 1e-9d);
            double coordinateX = shape.X + ((screenX - center.X) / unitScale);
            double coordinateY = shape.Y - ((screenY - center.Y) / unitScale);
            return ToLocalPoint(shape, coordinateX, coordinateY);
        }

        private double GetViewportWidth()
        {
            return Math.Max(DrawingSurface?.ActualWidth ?? ActualWidth, 1d);
        }

        private double GetViewportHeight()
        {
            return Math.Max(DrawingSurface?.ActualHeight ?? ActualHeight, 1d);
        }

        private double GetCoordinateRangeX()
        {
            var visibleWindow = GetVisibleCoordinateWindow();
            double range = visibleWindow.EndX - visibleWindow.StartX;
            return Math.Abs(range) < 1e-9d ? 1d : range;
        }

        private double GetCoordinateRangeY()
        {
            var visibleWindow = GetVisibleCoordinateWindow();
            double range = visibleWindow.EndY - visibleWindow.StartY;
            return Math.Abs(range) < 1e-9d ? 1d : range;
        }

        private static bool ContainsPoint(EditableTrajectoryShape shape, double x, double y)
        {
            (double localX, double localY) = ToLocalPoint(shape, x, y);
            return TrajectoryGeometryService.ContainsLocalPoint(
                TrajectoryShapeGeometry.FromEditable(shape),
                localX,
                localY);
        }

        private bool ContainsScreenPoint(EditableTrajectoryShape shape, double screenX, double screenY)
        {
            if (shape.Kind == TrajectoryShapeKind.Point)
            {
                return ContainsPointShape(shape, screenX, screenY);
            }

            if (shape.Kind == TrajectoryShapeKind.Line)
            {
                return ContainsLineScreenPoint(shape, screenX, screenY);
            }

            if (shape.Kind == TrajectoryShapeKind.Polyline)
            {
                return ContainsPolylineScreenPoint(shape, screenX, screenY);
            }

            (double localX, double localY) = ToVisualLocalCoordinate(shape, screenX, screenY);
            return TrajectoryGeometryService.ContainsLocalPoint(
                TrajectoryShapeGeometry.FromEditable(shape),
                localX,
                localY);
        }

        private bool ContainsLineScreenPoint(EditableTrajectoryShape shape, double screenX, double screenY)
        {
            var (startPoint, endPoint) = GetLineEndpointScreenPoints(shape);
            return TrajectoryGeometryService.ContainsLineSegmentPoint(
                startPoint.X,
                startPoint.Y,
                endPoint.X,
                endPoint.Y,
                screenX,
                screenY,
                6d,
                8d);
        }

        private bool ContainsPolylineScreenPoint(EditableTrajectoryShape shape, double screenX, double screenY)
        {
            IReadOnlyList<Point> points = GetPolylineScreenPoints(shape);
            for (int index = 1; index < points.Count; index++)
            {
                Point start = points[index - 1];
                Point end = points[index];
                if (TrajectoryGeometryService.ContainsLineSegmentPoint(
                    start.X,
                    start.Y,
                    end.X,
                    end.Y,
                    screenX,
                    screenY,
                    6d,
                    8d))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsPointShape(EditableTrajectoryShape shape, double screenX, double screenY)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            return TrajectoryGeometryService.ContainsPoint(
                center.X,
                center.Y,
                screenX,
                screenY,
                8d);
        }

        private Rect GetShapeScreenBounds(EditableTrajectoryShape shape)
        {
            const double padding = 8d;
            Point center = CoordinateToScreen(shape.X, shape.Y);

            if (shape.Kind == TrajectoryShapeKind.Point)
            {
                return new Rect(
                    center.X - padding,
                    center.Y - padding,
                    padding * 2d,
                    padding * 2d);
            }

            if (shape.Kind == TrajectoryShapeKind.Line)
            {
                var linePoints = GetLineEndpointScreenPoints(shape);
                var endpoints = new[] { linePoints.StartPoint, linePoints.EndPoint };
                return CreateScreenBounds(endpoints, padding);
            }

            if (shape.Kind == TrajectoryShapeKind.Polyline)
            {
                return CreateScreenBounds(GetPolylineScreenPoints(shape), padding);
            }

            if (shape.Kind == TrajectoryShapeKind.Rectangle)
            {
                var corners = GetRectangleResizeHandlePoints(shape).Select(handle => handle.Point).ToList();
                return CreateScreenBounds(corners, padding);
            }

            double radius = CoordinateLengthToScreenRadius(GetScaledRadius(shape)) + padding;
            return new Rect(
                center.X - radius,
                center.Y - radius,
                radius * 2d,
                radius * 2d);
        }

        private static Rect CreateScreenBounds(IReadOnlyCollection<Point> points, double padding)
        {
            if (points.Count == 0)
            {
                return Rect.Empty;
            }

            double left = points.Min(point => point.X) - padding;
            double top = points.Min(point => point.Y) - padding;
            double right = points.Max(point => point.X) + padding;
            double bottom = points.Max(point => point.Y) + padding;
            return new Rect(left, top, Math.Max(right - left, 0d), Math.Max(bottom - top, 0d));
        }

        private (double MinX, double MinY, double MaxX, double MaxY) GetShapeCoordinateBounds(EditableTrajectoryShape shape)
        {
            if (shape.Kind == TrajectoryShapeKind.Line)
            {
                var endpoints = GetLineEndpointCoordinates(shape);
                return (
                    Math.Min(endpoints.StartX, endpoints.EndX),
                    Math.Min(endpoints.StartY, endpoints.EndY),
                    Math.Max(endpoints.StartX, endpoints.EndX),
                    Math.Max(endpoints.StartY, endpoints.EndY));
            }

            if (shape.Kind == TrajectoryShapeKind.Polyline)
            {
                List<TrajectoryPoint> points = shape.PolylinePoints
                    .Where(point => point != null && double.IsFinite(point.X) && double.IsFinite(point.Y))
                    .ToList();
                if (points.Count > 0)
                {
                    return (
                        points.Min(point => point.X),
                        points.Min(point => point.Y),
                        points.Max(point => point.X),
                        points.Max(point => point.Y));
                }
            }

            if (shape.Kind == TrajectoryShapeKind.Rectangle)
            {
                TrajectoryShapeGeometry geometry = TrajectoryShapeGeometry.FromEditable(shape);
                double halfWidth = geometry.ScaledWidth / 2d;
                double halfHeight = geometry.ScaledHeight / 2d;
                var corners = new[]
                {
                    TrajectoryGeometryService.ToWorldPoint(geometry, -halfWidth, -halfHeight),
                    TrajectoryGeometryService.ToWorldPoint(geometry, halfWidth, -halfHeight),
                    TrajectoryGeometryService.ToWorldPoint(geometry, halfWidth, halfHeight),
                    TrajectoryGeometryService.ToWorldPoint(geometry, -halfWidth, halfHeight)
                };
                return (
                    corners.Min(point => point.X),
                    corners.Min(point => point.Y),
                    corners.Max(point => point.X),
                    corners.Max(point => point.Y));
            }

            if (shape.Kind is TrajectoryShapeKind.Circle or TrajectoryShapeKind.Arc)
            {
                double radius = GetScaledRadius(shape);
                return (shape.X - radius, shape.Y - radius, shape.X + radius, shape.Y + radius);
            }

            return (shape.X, shape.Y, shape.X, shape.Y);
        }

        private void DrawGrid(SKCanvas canvas)
        {
            CoordinateScreenTransform transform = GetCoordinateScreenTransform();
            SKRect plotRect = SKRect.Create(
                (float)transform.PlotLeft,
                (float)transform.PlotTop,
                (float)transform.PlotWidth,
                (float)transform.PlotHeight);

            using SKPaint plotFillPaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint gridPaint = new()
            {
                Color = new SKColor(225, 232, 241),
                IsAntialias = true,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawRect(plotRect, plotFillPaint);
            canvas.Save();
            canvas.ClipRect(plotRect, SKClipOperation.Intersect, true);

            (double startX, double startY, double endX, double endY) = GetVisibleCoordinateWindow();
            foreach (double x in GenerateTickValues(Math.Min(startX, endX), Math.Max(startX, endX), TickIntervalMillimeters))
            {
                Point screen = CoordinateToScreen(x, startY);
                canvas.DrawLine((float)screen.X, plotRect.Top, (float)screen.X, plotRect.Bottom, gridPaint);
            }

            foreach (double y in GenerateTickValues(Math.Min(startY, endY), Math.Max(startY, endY), TickIntervalMillimeters))
            {
                Point screen = CoordinateToScreen(startX, y);
                canvas.DrawLine(plotRect.Left, (float)screen.Y, plotRect.Right, (float)screen.Y, gridPaint);
            }

            canvas.Restore();
        }

        private void DrawCoordinateTicks(SKCanvas canvas, double width, double height)
        {
            CoordinateScreenTransform transform = GetCoordinateScreenTransform();
            float plotLeft = (float)transform.PlotLeft;
            float plotTop = (float)transform.PlotTop;
            float plotRight = (float)(transform.PlotLeft + transform.PlotWidth);
            float plotBottom = (float)(transform.PlotTop + transform.PlotHeight);

            using SKPaint tickPaint = new()
            {
                Color = new SKColor(88, 109, 129, 210),
                IsAntialias = true,
                StrokeWidth = 1.1f,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint labelPaint = new()
            {
                Color = new SKColor(62, 78, 94),
                IsAntialias = true,
                TextSize = 10.5f
            };

            canvas.DrawLine(plotLeft, plotBottom, plotRight, plotBottom, tickPaint);
            canvas.DrawLine(plotLeft, plotTop, plotLeft, plotBottom, tickPaint);

            (double startX, double startY, double endX, double endY) = GetVisibleCoordinateWindow();
            foreach (double x in GenerateTickValues(Math.Min(startX, endX), Math.Max(startX, endX), TickIntervalMillimeters))
            {
                Point screen = CoordinateToScreen(x, startY);
                canvas.DrawLine((float)screen.X, plotBottom, (float)screen.X, plotBottom - 8f, tickPaint);
                DrawTickLabel(canvas, labelPaint, FormatTickLabel(x), (float)screen.X + 2f, plotBottom - 11f);
            }

            foreach (double y in GenerateTickValues(Math.Min(startY, endY), Math.Max(startY, endY), TickIntervalMillimeters))
            {
                Point screen = CoordinateToScreen(startX, y);
                canvas.DrawLine(plotLeft, (float)screen.Y, plotLeft + 8f, (float)screen.Y, tickPaint);
                DrawTickLabel(canvas, labelPaint, FormatTickLabel(y), plotLeft + 10f, (float)screen.Y - 2f);
            }

            DrawTickLabel(canvas, labelPaint, "X(mm)", Math.Max(plotRight - 36f, plotLeft), plotBottom - 11f);
            DrawTickLabel(canvas, labelPaint, "Y(mm)", plotLeft + 10f, plotTop + 14f);
        }

        private static void DrawTickLabel(SKCanvas canvas, SKPaint labelPaint, string text, float x, float y)
        {
            canvas.DrawText(text, x, y, labelPaint);
        }

        private static IEnumerable<double> GenerateTickValues(double min, double max, double step)
        {
            double first = Math.Ceiling(min / step) * step;
            for (double value = first; value <= max + 1e-9d; value += step)
            {
                yield return Math.Abs(value) < 1e-9d ? 0d : value;
            }
        }

        private static string FormatTickLabel(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void DrawCoordinateBounds(SKCanvas canvas)
        {
            (double minX, double minY, double maxX, double maxY) = GetCoordinateBounds();
            using SKPaint fillPaint = new()
            {
                Color = new SKColor(45, 150, 96, 18),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint strokePaint = new()
            {
                Color = new SKColor(45, 150, 96, 190),
                IsAntialias = true,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint labelPaint = new()
            {
                Color = new SKColor(45, 110, 86, 230),
                IsAntialias = true,
                TextSize = 12f
            };

            SKRect bounds = SKRect.Create(
                (float)minX,
                (float)minY,
                (float)Math.Max(maxX - minX, 0d),
                (float)Math.Max(maxY - minY, 0d));

            canvas.DrawRect(bounds, fillPaint);
            canvas.DrawRect(bounds, strokePaint);
            canvas.DrawCircle((float)CoordinateStartX, (float)CoordinateStartY, 4f, strokePaint);
            canvas.DrawCircle((float)CoordinateEndX, (float)CoordinateEndY, 4f, strokePaint);
            canvas.DrawText("Start", (float)CoordinateStartX + 6f, (float)CoordinateStartY - 6f, labelPaint);
            canvas.DrawText("End", (float)CoordinateEndX + 6f, (float)CoordinateEndY - 6f, labelPaint);
        }

        private static void DrawDefaultCenter(SKCanvas canvas, double x, double y)
        {
            using SKPaint centerPaint = new()
            {
                Color = new SKColor(45, 150, 96, 210),
                IsAntialias = true,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint labelPaint = new()
            {
                Color = new SKColor(45, 150, 96, 210),
                IsAntialias = true,
                TextSize = 12f
            };

            canvas.DrawLine((float)(x - 9d), (float)y, (float)(x + 9d), (float)y, centerPaint);
            canvas.DrawLine((float)x, (float)(y - 9d), (float)x, (float)(y + 9d), centerPaint);
            canvas.DrawCircle((float)x, (float)y, 5f, centerPaint);
            canvas.DrawText("Center", (float)(x + 8d), (float)(y - 8d), labelPaint);
        }

        private void DrawCircleRadiusHandle(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            Point handlePoint = GetCircleRadiusHandleScreenPoint(shape);
            Point centerPoint = CoordinateToScreen(shape.X, shape.Y);

            using SKPaint linePaint = new()
            {
                Color = new SKColor(232, 87, 39, 210),
                IsAntialias = true,
                StrokeWidth = 0.8f,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint handleFillPaint = new()
            {
                Color = new SKColor(255, 255, 255),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint handleStrokePaint = new()
            {
                Color = new SKColor(232, 87, 39),
                IsAntialias = true,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawLine((float)centerPoint.X, (float)centerPoint.Y, (float)handlePoint.X, (float)handlePoint.Y, linePaint);
            canvas.DrawCircle((float)handlePoint.X, (float)handlePoint.Y, 6f, handleFillPaint);
            canvas.DrawCircle((float)handlePoint.X, (float)handlePoint.Y, 6f, handleStrokePaint);
        }

        private void DrawCircleRotationHandle(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            Point radiusPoint = GetCircleRadiusHandleScreenPoint(shape);
            Point handlePoint = GetCircleRotationHandlePoint(shape);

            using SKPaint linePaint = new()
            {
                Color = new SKColor(45, 150, 96, 210),
                IsAntialias = true,
                StrokeWidth = 0.8f,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawLine((float)radiusPoint.X, (float)radiusPoint.Y, (float)handlePoint.X, (float)handlePoint.Y, linePaint);
            DrawResizeHandle(canvas, handlePoint, new SKColor(45, 150, 96));
        }

        private void DrawRectangleResizeHandles(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            foreach (var handle in GetRectangleResizeHandlePoints(shape))
            {
                DrawResizeHandle(canvas, handle.Point, new SKColor(232, 87, 39));
            }

            DrawRectangleRotationHandle(canvas, shape);
        }

        private void DrawRectangleRotationHandle(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            double halfHeight = CoordinateLengthToScreenRadius(GetScaledHeight(shape)) / 2d;
            Point topCenter = ToRotatedScreenPoint(shape, 0d, -halfHeight);
            Point handlePoint = GetRectangleRotationHandlePoint(shape);

            using SKPaint linePaint = new()
            {
                Color = new SKColor(45, 150, 96, 210),
                IsAntialias = true,
                StrokeWidth = 0.8f,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawLine((float)topCenter.X, (float)topCenter.Y, (float)handlePoint.X, (float)handlePoint.Y, linePaint);
            DrawResizeHandle(canvas, handlePoint, new SKColor(45, 150, 96));
        }

        private void DrawLineEndpointHandles(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            foreach (var handle in GetLineEndpointHandlePoints(shape))
            {
                SKColor color = handle.Kind == TrajectoryResizeHandleKind.LineStart
                    ? new SKColor(45, 150, 96)
                    : new SKColor(232, 87, 39);
                DrawResizeHandle(canvas, handle.Point, color);
            }
        }

        private void DrawArcResizeHandles(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            foreach (var handle in GetArcResizeHandlePoints(shape))
            {
                SKColor color = handle.Kind == TrajectoryResizeHandleKind.ArcRadius
                    ? new SKColor(232, 87, 39)
                    : new SKColor(45, 150, 96);
                DrawResizeHandle(canvas, handle.Point, color);
            }
        }

        private static void DrawResizeHandle(SKCanvas canvas, Point point, SKColor color)
        {
            using SKPaint fillPaint = new()
            {
                Color = new SKColor(255, 255, 255),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint strokePaint = new()
            {
                Color = color,
                IsAntialias = true,
                StrokeWidth = 1.6f,
                Style = SKPaintStyle.Stroke
            };

            SKRect rect = SKRect.Create((float)point.X - 5f, (float)point.Y - 5f, 10f, 10f);
            canvas.DrawRoundRect(rect, 2f, 2f, fillPaint);
            canvas.DrawRoundRect(rect, 2f, 2f, strokePaint);
        }

        private void DrawSelectionBox(SKCanvas canvas)
        {
            if (!_isSelectingByBox)
            {
                return;
            }

            Rect rect = GetSelectionBoxRect();
            SKRect skRect = new(
                (float)rect.Left,
                (float)rect.Top,
                (float)rect.Right,
                (float)rect.Bottom);

            using SKPaint fillPaint = new()
            {
                Color = new SKColor(54, 132, 196, 32),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint strokePaint = new()
            {
                Color = new SKColor(54, 132, 196, 210),
                IsAntialias = true,
                StrokeWidth = 1.2f,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0f)
            };

            canvas.DrawRect(skRect, fillPaint);
            canvas.DrawRect(skRect, strokePaint);
        }

        private void DrawShapeDimensionLabel(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            switch (shape.Kind)
            {
                case TrajectoryShapeKind.Circle:
                    DrawCircleRadiusDimensionLabel(canvas, shape);
                    break;
                case TrajectoryShapeKind.Rectangle:
                    DrawRectangleDimensionLabelsOnSegments(canvas, shape);
                    break;
                case TrajectoryShapeKind.Arc:
                    DrawArcSweepDimensionLabel(canvas, shape);
                    break;
                case TrajectoryShapeKind.Line:
                    DrawLineLengthDimensionLabel(canvas, shape);
                    break;
            }
        }

        private void DrawShapeCenterCoordinateLabel(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            Point anchor = shape.Kind == TrajectoryShapeKind.Point
                ? new Point(center.X + 10d, center.Y - 10d)
                : new Point(center.X + 8d, center.Y + 16d);
            DrawScreenLabel(canvas, FormatShapeCenterCoordinateText(shape), anchor, new SKColor(38, 92, 146));
        }

        private static string FormatShapeCenterCoordinateText(EditableTrajectoryShape shape)
        {
            return $"X={shape.X:F1}mm Y={shape.Y:F1}mm";
        }

        private void DrawCircleRadiusDimensionLabel(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            DrawSegmentLabel(canvas, center, GetCircleRadiusHandleScreenPoint(shape), FormatCircleDimensionText(shape));
        }

        private void DrawRectangleDimensionLabelsOnSegments(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            double halfWidth = CoordinateLengthToScreenRadius(GetScaledWidth(shape)) / 2d;
            double halfHeight = CoordinateLengthToScreenRadius(GetScaledHeight(shape)) / 2d;
            Point topLeft = ToRotatedScreenPoint(shape, -halfWidth, -halfHeight);
            Point topRight = ToRotatedScreenPoint(shape, halfWidth, -halfHeight);
            Point bottomRight = ToRotatedScreenPoint(shape, halfWidth, halfHeight);

            DrawSegmentLabel(canvas, topLeft, topRight, FormatRectangleWidthDimensionText(shape));
            DrawSegmentLabel(canvas, topRight, bottomRight, FormatRectangleHeightDimensionText(shape));
        }

        private void DrawArcSweepDimensionLabel(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            double midAngle = shape.StartAngle + shape.SweepAngle / 2d;
            Point tangentStart = GetArcEndpointScreenPoint(shape, midAngle - 1d);
            Point tangentEnd = GetArcEndpointScreenPoint(shape, midAngle + 1d);
            DrawSegmentLabel(canvas, tangentStart, tangentEnd, FormatArcDimensionText(shape));
        }

        private void DrawLineLengthDimensionLabel(SKCanvas canvas, EditableTrajectoryShape shape)
        {
            var handles = GetLineEndpointHandlePoints(shape).ToArray();
            if (handles.Length == 2)
            {
                DrawSegmentLabel(canvas, handles[0].Point, handles[1].Point, FormatLineDimensionText(shape));
            }
        }

        private static void DrawSegmentLabel(SKCanvas canvas, Point start, Point end, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            if (Distance(0d, 0d, deltaX, deltaY) < 1e-6d)
            {
                return;
            }

            double angle = Math.Atan2(deltaY, deltaX) * 180d / Math.PI;
            if (angle > 90d || angle < -90d)
            {
                angle += 180d;
            }

            canvas.Save();
            canvas.Translate((float)((start.X + end.X) / 2d), (float)((start.Y + end.Y) / 2d));
            canvas.RotateDegrees((float)angle);
            DrawCenteredLabel(canvas, text, new SKColor(36, 52, 68));
            canvas.Restore();
        }

        private static void DrawScreenLabel(SKCanvas canvas, string text, Point anchor, SKColor textColor)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            using SKPaint textPaint = CreateLabelTextPaint(textColor);
            using SKPaint backgroundPaint = CreateLabelBackgroundPaint();
            SKRect textBounds = new();
            textPaint.MeasureText(text, ref textBounds);
            SKRect background = SKRect.Create(
                (float)anchor.X - 3f,
                (float)anchor.Y + textBounds.Top - 3f,
                textBounds.Width + 6f,
                textBounds.Height + 6f);
            canvas.DrawRoundRect(background, 3f, 3f, backgroundPaint);
            canvas.DrawText(text, (float)anchor.X, (float)anchor.Y, textPaint);
        }

        private static void DrawCenteredLabel(SKCanvas canvas, string text, SKColor textColor)
        {
            using SKPaint textPaint = CreateLabelTextPaint(textColor);
            using SKPaint backgroundPaint = CreateLabelBackgroundPaint();
            SKRect textBounds = new();
            float textWidth = textPaint.MeasureText(text, ref textBounds);
            float baseline = -((textBounds.Top + textBounds.Bottom) / 2f);
            SKRect background = SKRect.Create(
                -textWidth / 2f - 4f,
                baseline + textBounds.Top - 3f,
                textWidth + 8f,
                textBounds.Height + 6f);
            canvas.DrawRoundRect(background, 3f, 3f, backgroundPaint);
            canvas.DrawText(text, -textWidth / 2f, baseline, textPaint);
        }

        private static SKPaint CreateLabelTextPaint(SKColor color)
        {
            return new SKPaint
            {
                Color = color,
                IsAntialias = true,
                TextSize = 11.5f
            };
        }

        private static SKPaint CreateLabelBackgroundPaint()
        {
            return new SKPaint
            {
                Color = new SKColor(255, 255, 255, 218),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
        }

        private static string FormatCircleDimensionText(EditableTrajectoryShape shape)
        {
            return $"R={shape.Radius:F1}mm";
        }

        private static string FormatRectangleWidthDimensionText(EditableTrajectoryShape shape)
        {
            return $"W={shape.Width:F1}mm";
        }

        private static string FormatRectangleHeightDimensionText(EditableTrajectoryShape shape)
        {
            return $"H={shape.Height:F1}mm";
        }

        private static string FormatRectangleDimensionText(EditableTrajectoryShape shape)
        {
            return $"{FormatRectangleWidthDimensionText(shape)} {FormatRectangleHeightDimensionText(shape)}";
        }

        private static string FormatArcDimensionText(EditableTrajectoryShape shape)
        {
            return $"弧度={shape.SweepAngle:F1}°";
        }

        private static string FormatLineDimensionText(EditableTrajectoryShape shape)
        {
            return $"L={shape.Width:F1}mm";
        }

        private void DrawShapeScreen(SKCanvas canvas, EditableTrajectoryShape shape, bool isSelected, bool isPreview = false)
        {
            using SKPaint strokePaint = new()
            {
                Color = isPreview
                    ? new SKColor(45, 150, 96, 170)
                    : isSelected ? new SKColor(232, 87, 39) : new SKColor(46, 85, 115),
                IsAntialias = true,
                StrokeWidth = isSelected ? 2f : 1.2f,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint fillPaint = new()
            {
                Color = isPreview ? new SKColor(45, 150, 96, 28) : new SKColor(54, 132, 196, 26),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            if (shape.Kind == TrajectoryShapeKind.Line)
            {
                var (startPoint, endPoint) = GetLineEndpointScreenPoints(shape);
                DrawLineScreenShape(canvas, shape, strokePaint, startPoint, endPoint);
                return;
            }

            if (shape.Kind == TrajectoryShapeKind.Polyline)
            {
                DrawPolylineScreenShape(canvas, shape, strokePaint);
                return;
            }

            Point center = CoordinateToScreen(shape.X, shape.Y);
            double unitScale = CoordinateLengthToScreenRadius(1d);
            canvas.Save();
            canvas.Translate((float)center.X, (float)center.Y);
            canvas.RotateDegrees((float)-shape.RotationAngle);

            switch (shape.Kind)
            {
                case TrajectoryShapeKind.Point:
                    DrawPointScreenShape(canvas, shape, strokePaint, fillPaint, unitScale);
                    break;
                case TrajectoryShapeKind.Rectangle:
                    DrawRectangleScreenShape(canvas, shape, strokePaint, fillPaint, unitScale);
                    break;
                case TrajectoryShapeKind.Arc:
                    DrawArcScreenShape(canvas, shape, strokePaint, unitScale);
                    break;
                default:
                    DrawCircleScreenShape(canvas, shape, strokePaint, fillPaint, unitScale);
                    break;
            }

            DrawInternalTrajectoryScreen(canvas, shape, isPreview, unitScale);
            canvas.Restore();
        }

        private static void DrawPointScreenShape(
            SKCanvas canvas,
            EditableTrajectoryShape shape,
            SKPaint strokePaint,
            SKPaint fillPaint,
            double unitScale)
        {
            const float pointRadius = 4f;
            canvas.DrawCircle(0f, 0f, pointRadius, fillPaint);
            canvas.DrawCircle(0f, 0f, pointRadius, strokePaint);
            canvas.DrawLine(-6f, 0f, 6f, 0f, strokePaint);
            canvas.DrawLine(0f, -6f, 0f, 6f, strokePaint);
        }

        private static void DrawLineScreenShape(
            SKCanvas canvas,
            EditableTrajectoryShape shape,
            SKPaint strokePaint,
            Point startPoint,
            Point endPoint)
        {
            canvas.DrawLine(
                (float)startPoint.X,
                (float)startPoint.Y,
                (float)endPoint.X,
                (float)endPoint.Y,
                strokePaint);
            DrawCenterScreen(
                canvas,
                new Point((startPoint.X + endPoint.X) / 2d, (startPoint.Y + endPoint.Y) / 2d),
                strokePaint.Color);
        }

        private void DrawPolylineScreenShape(
            SKCanvas canvas,
            EditableTrajectoryShape shape,
            SKPaint strokePaint)
        {
            IReadOnlyList<Point> points = GetPolylineScreenPoints(shape);
            for (int index = 1; index < points.Count; index++)
            {
                Point start = points[index - 1];
                Point end = points[index];
                canvas.DrawLine(
                    (float)start.X,
                    (float)start.Y,
                    (float)end.X,
                    (float)end.Y,
                    strokePaint);
            }

            DrawCenterScreen(canvas, CoordinateToScreen(shape.X, shape.Y), strokePaint.Color);
        }

        private static void DrawCircleScreenShape(
            SKCanvas canvas,
            EditableTrajectoryShape shape,
            SKPaint strokePaint,
            SKPaint fillPaint,
            double unitScale)
        {
            double radius = GetScaledRadius(shape) * unitScale;
            canvas.DrawCircle(0f, 0f, (float)radius, fillPaint);
            canvas.DrawCircle(0f, 0f, (float)radius, strokePaint);
            canvas.DrawLine(0f, 0f, (float)radius, 0f, strokePaint);
            DrawCenterScreen(canvas, strokePaint.Color);
        }

        private static void DrawRectangleScreenShape(
            SKCanvas canvas,
            EditableTrajectoryShape shape,
            SKPaint strokePaint,
            SKPaint fillPaint,
            double unitScale)
        {
            double width = GetScaledWidth(shape) * unitScale;
            double height = GetScaledHeight(shape) * unitScale;
            SKRect rect = SKRect.Create(
                (float)(-width / 2d),
                (float)(-height / 2d),
                (float)width,
                (float)height);

            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, strokePaint);
            DrawCenterScreen(canvas, strokePaint.Color);
        }

        private static void DrawArcScreenShape(SKCanvas canvas, EditableTrajectoryShape shape, SKPaint strokePaint, double unitScale)
        {
            double radius = GetScaledRadius(shape) * unitScale;
            SKRect rect = SKRect.Create(
                (float)-radius,
                (float)-radius,
                (float)(radius * 2d),
                (float)(radius * 2d));

            canvas.DrawArc(rect, (float)-shape.StartAngle, (float)-shape.SweepAngle, false, strokePaint);
            DrawCenterScreen(canvas, strokePaint.Color);
        }

        private static void DrawInternalTrajectoryScreen(SKCanvas canvas, EditableTrajectoryShape shape, bool isPreview, double unitScale)
        {
            if (shape.InnerPattern == TrajectoryInnerPattern.None
                || shape.Kind == TrajectoryShapeKind.Arc
                || shape.Kind == TrajectoryShapeKind.Line)
            {
                return;
            }

            TrajectoryShapeGeometry geometry = TrajectoryShapeGeometry.FromEditable(shape);

            SKColor color = isPreview
                ? new SKColor(45, 150, 96, 150)
                : new SKColor(232, 87, 39, 150);

            using SKPaint linePaint = new()
            {
                Color = color,
                IsAntialias = true,
                StrokeWidth = 0.8f,
                Style = SKPaintStyle.Stroke
            };

            using SKPaint pointPaint = new()
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            foreach (var point in TrajectoryGeometryService.GenerateLocalInnerPoints(geometry))
            {
                canvas.DrawCircle((float)(point.X * unitScale), (float)(-point.Y * unitScale), 2f, pointPaint);
            }

            foreach (var line in TrajectoryGeometryService.GenerateLocalInnerLines(geometry))
            {
                canvas.DrawLine(
                    (float)(line.StartX * unitScale),
                    (float)(-line.StartY * unitScale),
                    (float)(line.EndX * unitScale),
                    (float)(-line.EndY * unitScale),
                    linePaint);
            }
        }

        private static void DrawCenterScreen(SKCanvas canvas, SKColor color)
        {
            using SKPaint centerPaint = new()
            {
                Color = color,
                IsAntialias = true,
                StrokeWidth = 1f,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawLine(-4f, 0f, 4f, 0f, centerPaint);
            canvas.DrawLine(0f, -4f, 0f, 4f, centerPaint);
        }

        private static void DrawCenterScreen(SKCanvas canvas, Point center, SKColor color)
        {
            using SKPaint centerPaint = new()
            {
                Color = color,
                IsAntialias = true,
                StrokeWidth = 1f,
                Style = SKPaintStyle.Stroke
            };

            canvas.DrawLine((float)(center.X - 4d), (float)center.Y, (float)(center.X + 4d), (float)center.Y, centerPaint);
            canvas.DrawLine((float)center.X, (float)(center.Y - 4d), (float)center.X, (float)(center.Y + 4d), centerPaint);
        }

        private void BeginDragSelectedShapes(EditableTrajectoryShape shape, Point point)
        {
            (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
            _dragShape = shape;
            _dragStartCoordinate = (coordinateX, coordinateY);
            _dragStartShapeCenters.Clear();
            _dragStartPolylinePoints.Clear();
            _dragStartShapeBounds.Clear();

            IEnumerable<EditableTrajectoryShape> dragShapes = SelectedShapes.Count > 0 && SelectedShapes.Contains(shape)
                ? SelectedShapes
                : new[] { shape };

            foreach (EditableTrajectoryShape dragShape in dragShapes.Distinct())
            {
                _dragStartShapeCenters[dragShape] = (dragShape.X, dragShape.Y);
                _dragStartShapeBounds[dragShape] = GetShapeCoordinateBounds(dragShape);
                if (dragShape.Kind == TrajectoryShapeKind.Polyline)
                {
                    _dragStartPolylinePoints[dragShape] = dragShape.PolylinePoints
                        .Where(point => point != null)
                        .Select(point => point.Clone())
                        .ToList();
                }
            }

            _isDragging = true;
            DrawingSurface.CaptureMouse();
        }

        private void BeginCircleRadiusResize(EditableTrajectoryShape shape)
        {
            BeginResize(shape, TrajectoryResizeHandleKind.CircleRadius);
        }

        private void BeginCircleRotation(EditableTrajectoryShape shape)
        {
            BeginResize(shape, TrajectoryResizeHandleKind.CircleRotate);
        }

        private void BeginRectangleResize(EditableTrajectoryShape shape, TrajectoryResizeHandleKind handle)
        {
            BeginResize(shape, handle);
        }

        private void BeginRectangleRotation(EditableTrajectoryShape shape)
        {
            BeginResize(shape, TrajectoryResizeHandleKind.RectangleRotate);
        }

        private void BeginLineEndpointResize(EditableTrajectoryShape shape, TrajectoryResizeHandleKind handle)
        {
            BeginResize(shape, handle);
        }

        private void BeginArcResize(EditableTrajectoryShape shape, TrajectoryResizeHandleKind handle)
        {
            BeginResize(shape, handle);
        }

        private void BeginResize(EditableTrajectoryShape shape, TrajectoryResizeHandleKind handle)
        {
            _resizeShape = shape;
            _activeResizeHandle = handle;
            _isResizingShape = true;
            DrawingSurface.CaptureMouse();
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            _dragShape = null;
            _dragStartCoordinate = default;
            _dragStartShapeCenters.Clear();
            _dragStartPolylinePoints.Clear();
            _dragStartShapeBounds.Clear();
            DrawingSurface?.ReleaseMouseCapture();
        }

        private void EndResize()
        {
            if (!_isResizingShape)
            {
                return;
            }

            _isResizingShape = false;
            _resizeShape = null;
            _activeResizeHandle = TrajectoryResizeHandleKind.None;
            DrawingSurface?.ReleaseMouseCapture();
        }

        private void EndCoordinateViewportPan()
        {
            if (!_isPanningCoordinateViewport)
            {
                return;
            }

            _isPanningCoordinateViewport = false;
            _viewportPanStartPoint = default;
            _viewportPanStartWindow = default;
            if (DrawingSurface != null)
            {
                DrawingSurface.Cursor = null;
                DrawingSurface.ReleaseMouseCapture();
            }
        }

        private void EndRightButtonCoordinateViewportPan()
        {
            if (!_hasRightButtonPanCandidate && !_isRightButtonPanningCoordinateViewport)
            {
                return;
            }

            bool wasPanning = _isRightButtonPanningCoordinateViewport;
            _hasRightButtonPanCandidate = false;
            _isRightButtonPanningCoordinateViewport = false;
            _rightButtonDownPoint = default;
            _rightButtonDownShape = null;

            if (wasPanning)
            {
                EndCoordinateViewportPan();
                return;
            }

            if (DrawingSurface != null)
            {
                DrawingSurface.ReleaseMouseCapture();
            }
        }

        private void EndInteraction()
        {
            EndRightButtonCoordinateViewportPan();
            EndDrag();
            EndResize();
            EndCoordinateViewportPan();
            EndSelectionBox();
        }

        private void UpdateShapeResizeFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            switch (_activeResizeHandle)
            {
                case TrajectoryResizeHandleKind.CircleRadius:
                case TrajectoryResizeHandleKind.ArcRadius:
                    UpdateCircleRadiusFromScreenPoint(shape, point);
                    break;
                case TrajectoryResizeHandleKind.CircleRotate:
                    UpdateCircleRotationFromScreenPoint(shape, point);
                    break;
                case TrajectoryResizeHandleKind.RectangleTopLeft:
                case TrajectoryResizeHandleKind.RectangleTopRight:
                case TrajectoryResizeHandleKind.RectangleBottomRight:
                case TrajectoryResizeHandleKind.RectangleBottomLeft:
                    UpdateRectangleSizeFromScreenPoint(shape, point);
                    break;
                case TrajectoryResizeHandleKind.RectangleRotate:
                    UpdateRectangleRotationFromScreenPoint(shape, point);
                    break;
                case TrajectoryResizeHandleKind.LineStart:
                case TrajectoryResizeHandleKind.LineEnd:
                    UpdateLineEndpointFromScreenPoint(shape, point);
                    break;
                case TrajectoryResizeHandleKind.ArcStart:
                case TrajectoryResizeHandleKind.ArcEnd:
                    UpdateArcFromScreenPoint(shape, point);
                    break;
            }
        }

        private void UpdateCircleRadiusFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double unitScale = Math.Max(CoordinateLengthToScreenRadius(1d), 1e-9d);
            double radius = Distance(center.X, center.Y, point.X, point.Y) / unitScale / GetScale(shape);
            shape.Radius = NormalizePositive(radius, 1d);
            InvalidateSurface();
        }

        private void UpdateCircleRotationFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double screenAngle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180d / Math.PI;
            shape.RotationAngle = NormalizeAngle(-screenAngle);
            InvalidateSurface();
        }

        private void UpdateRectangleSizeFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            (double localX, double localY) = ToVisualLocalCoordinate(shape, point.X, point.Y);
            double scale = GetScale(shape);
            shape.Width = Math.Max(Math.Abs(localX) * 2d / scale, 1d);
            shape.Height = Math.Max(Math.Abs(localY) * 2d / scale, 1d);
            InvalidateSurface();
        }

        private void UpdateRectangleRotationFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double screenAngle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180d / Math.PI;
            shape.RotationAngle = NormalizeAngle(-90d - screenAngle);
            InvalidateSurface();
        }

        private void UpdateLineEndpointFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            (double coordinateX, double coordinateY) = ScreenToCoordinate(point.X, point.Y);
            (coordinateX, coordinateY) = ClampPointToCoordinateBounds(coordinateX, coordinateY);
            (double startX, double startY, double endX, double endY) = GetLineEndpointCoordinates(shape);

            EditableTrajectoryShape updated = _activeResizeHandle == TrajectoryResizeHandleKind.LineStart
                ? CreateLineShapeFromEndpoints(coordinateX, coordinateY, endX, endY)
                : CreateLineShapeFromEndpoints(startX, startY, coordinateX, coordinateY);

            shape.X = updated.X;
            shape.Y = updated.Y;
            shape.Width = updated.Width;
            shape.Height = updated.Height;
            shape.RotationAngle = updated.RotationAngle;
            InvalidateSurface();
        }

        private void UpdateArcFromScreenPoint(EditableTrajectoryShape shape, Point point)
        {
            (double localX, double localY) = ToVisualLocalCoordinate(shape, point.X, point.Y);
            double angle = NormalizeAngle(Math.Atan2(localY, localX) * 180d / Math.PI);
            if (_activeResizeHandle == TrajectoryResizeHandleKind.ArcStart)
            {
                double endAngle = NormalizeAngle(shape.StartAngle + shape.SweepAngle);
                shape.StartAngle = angle;
                shape.SweepAngle = NormalizeSweepDelta(endAngle - angle);
            }
            else if (_activeResizeHandle == TrajectoryResizeHandleKind.ArcEnd)
            {
                shape.SweepAngle = NormalizeSweepDelta(angle - shape.StartAngle);
            }

            InvalidateSurface();
        }

        private string GetResizeStatusText(EditableTrajectoryShape shape)
        {
            if (_activeResizeHandle == TrajectoryResizeHandleKind.CircleRotate)
            {
                return $"正在旋转圆形：角度={shape.RotationAngle:F1}";
            }

            if (_activeResizeHandle == TrajectoryResizeHandleKind.RectangleRotate)
            {
                return $"正在旋转矩形：角度={shape.RotationAngle:F1}";
            }

            return shape.Kind switch
            {
                TrajectoryShapeKind.Line => $"正在调整线段：L={shape.Width:F1}, 角度={shape.RotationAngle:F1}",
                TrajectoryShapeKind.Rectangle => $"正在调整矩形：W={shape.Width:F1}, H={shape.Height:F1}",
                TrajectoryShapeKind.Arc => $"正在调整圆弧：R={shape.Radius:F1}, 弧度={shape.SweepAngle:F1}",
                _ => $"正在调整圆半径：R={shape.Radius:F1}"
            };
        }

        private void MoveDraggedShapes(double deltaX, double deltaY)
        {
            if (_dragStartShapeCenters.Count == 0)
            {
                return;
            }

            (deltaX, deltaY) = ClampDragDeltaToCoordinateBounds(deltaX, deltaY);
            foreach (var item in _dragStartShapeCenters)
            {
                if (item.Key.Kind == TrajectoryShapeKind.Polyline
                    && _dragStartPolylinePoints.TryGetValue(item.Key, out List<TrajectoryPoint>? points))
                {
                    item.Key.PolylinePoints = points
                        .Select(point => new TrajectoryPoint
                        {
                            X = point.X + deltaX,
                            Y = point.Y + deltaY
                        })
                        .ToList();
                    SetShapeCenter(item.Key, GetPolylineCenter(item.Key.PolylinePoints));
                }
                else
                {
                    item.Key.X = item.Value.X + deltaX;
                    item.Key.Y = item.Value.Y + deltaY;
                }
            }

            InvalidateSurface();
        }

        private (double DeltaX, double DeltaY) ClampDragDeltaToCoordinateBounds(double deltaX, double deltaY)
        {
            (double minX, double minY, double maxX, double maxY) = GetCoordinateBounds();
            double minDeltaX = double.NegativeInfinity;
            double maxDeltaX = double.PositiveInfinity;
            double minDeltaY = double.NegativeInfinity;
            double maxDeltaY = double.PositiveInfinity;

            foreach (var bounds in _dragStartShapeBounds.Values)
            {
                minDeltaX = Math.Max(minDeltaX, minX - bounds.MinX);
                maxDeltaX = Math.Min(maxDeltaX, maxX - bounds.MaxX);
                minDeltaY = Math.Max(minDeltaY, minY - bounds.MinY);
                maxDeltaY = Math.Min(maxDeltaY, maxY - bounds.MaxY);
            }

            return (
                Math.Min(Math.Max(deltaX, minDeltaX), maxDeltaX),
                Math.Min(Math.Max(deltaY, minDeltaY), maxDeltaY));
        }

        private string GetDraggingStatusText()
        {
            if (_dragStartShapeCenters.Count > 1)
            {
                return $"正在移动 {_dragStartShapeCenters.Count} 个图形。";
            }

            EditableTrajectoryShape? shape = _dragShape;
            return shape == null
                ? "正在移动图形。"
                : $"正在移动图形：X={shape.X:F1}, Y={shape.Y:F1}";
        }

        private Point GetCircleRadiusHandleScreenPoint(EditableTrajectoryShape shape)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double radius = CoordinateLengthToScreenRadius(GetScaledRadius(shape));
            double angle = -shape.RotationAngle * Math.PI / 180d;
            return new Point(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle));
        }

        private Point GetCircleRotationHandlePoint(EditableTrajectoryShape shape)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double radius = CoordinateLengthToScreenRadius(GetScaledRadius(shape)) + 28d;
            double angle = -shape.RotationAngle * Math.PI / 180d;
            return new Point(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle));
        }

        private IEnumerable<(TrajectoryResizeHandleKind Kind, Point Point)> GetRectangleResizeHandlePoints(EditableTrajectoryShape shape)
        {
            double halfWidth = CoordinateLengthToScreenRadius(GetScaledWidth(shape)) / 2d;
            double halfHeight = CoordinateLengthToScreenRadius(GetScaledHeight(shape)) / 2d;
            yield return (TrajectoryResizeHandleKind.RectangleTopLeft, ToRotatedScreenPoint(shape, -halfWidth, -halfHeight));
            yield return (TrajectoryResizeHandleKind.RectangleTopRight, ToRotatedScreenPoint(shape, halfWidth, -halfHeight));
            yield return (TrajectoryResizeHandleKind.RectangleBottomRight, ToRotatedScreenPoint(shape, halfWidth, halfHeight));
            yield return (TrajectoryResizeHandleKind.RectangleBottomLeft, ToRotatedScreenPoint(shape, -halfWidth, halfHeight));
        }

        private Point GetRectangleRotationHandlePoint(EditableTrajectoryShape shape)
        {
            double halfHeight = CoordinateLengthToScreenRadius(GetScaledHeight(shape)) / 2d;
            return ToRotatedScreenPoint(shape, 0d, -halfHeight - 28d);
        }

        private IEnumerable<(TrajectoryResizeHandleKind Kind, Point Point)> GetLineEndpointHandlePoints(EditableTrajectoryShape shape)
        {
            var endpoints = GetLineEndpointScreenPoints(shape);
            yield return (TrajectoryResizeHandleKind.LineStart, endpoints.StartPoint);
            yield return (TrajectoryResizeHandleKind.LineEnd, endpoints.EndPoint);
        }

        private (Point StartPoint, Point EndPoint) GetLineEndpointScreenPoints(EditableTrajectoryShape shape)
        {
            var endpoints = GetLineEndpointCoordinates(shape);
            Point startPoint = CoordinateToScreen(endpoints.StartX, endpoints.StartY);
            Point endPoint = CoordinateToScreen(endpoints.EndX, endpoints.EndY);
            return (startPoint, endPoint);
        }

        private IReadOnlyList<Point> GetPolylineScreenPoints(EditableTrajectoryShape shape)
        {
            return (shape.PolylinePoints ?? new List<TrajectoryPoint>())
                .Where(point => point != null && double.IsFinite(point.X) && double.IsFinite(point.Y))
                .Select(point => CoordinateToScreen(point.X, point.Y))
                .ToList();
        }

        private static (double StartX, double StartY, double EndX, double EndY) GetLineEndpointCoordinates(EditableTrajectoryShape shape)
        {
            return TrajectoryGeometryService.GetLineEndpointCoordinates(
                TrajectoryShapeGeometry.FromEditable(shape));
        }

        private IEnumerable<(TrajectoryResizeHandleKind Kind, Point Point)> GetArcResizeHandlePoints(EditableTrajectoryShape shape)
        {
            yield return (TrajectoryResizeHandleKind.ArcRadius, GetArcEndpointScreenPoint(shape, shape.StartAngle + shape.SweepAngle / 2d));
            yield return (TrajectoryResizeHandleKind.ArcStart, GetArcEndpointScreenPoint(shape, shape.StartAngle));
            yield return (TrajectoryResizeHandleKind.ArcEnd, GetArcEndpointScreenPoint(shape, shape.StartAngle + shape.SweepAngle));
        }

        private Point GetArcEndpointScreenPoint(EditableTrajectoryShape shape, double angle)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double radius = CoordinateLengthToScreenRadius(GetScaledRadius(shape));
            double screenAngle = -(shape.RotationAngle + angle) * Math.PI / 180d;
            return new Point(
                center.X + radius * Math.Cos(screenAngle),
                center.Y + radius * Math.Sin(screenAngle));
        }

        private Point ToRotatedScreenPoint(EditableTrajectoryShape shape, double localScreenX, double localScreenY)
        {
            Point center = CoordinateToScreen(shape.X, shape.Y);
            double angle = -shape.RotationAngle * Math.PI / 180d;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Point(
                center.X + (localScreenX * cos) - (localScreenY * sin),
                center.Y + (localScreenX * sin) + (localScreenY * cos));
        }

        private void ShowDeleteMenu(EditableTrajectoryShape shape)
        {
            ContextMenu menu = new();
            MenuItem deleteItem = new()
            {
                Header = "删除"
            };

            deleteItem.Click += (_, _) =>
            {
                int removedCount = DeleteSelectedShapes();
                StatusText = removedCount > 1
                    ? $"已删除 {removedCount} 个选中的图形。"
                    : "已删除选中的图形。";
            };

            menu.Items.Add(deleteItem);
            menu.PlacementTarget = DrawingSurface;
            menu.IsOpen = true;
        }

        private void InvalidateSurface()
        {
            DrawingSurface?.InvalidateVisual();
        }

        private static bool IsShapeTool(TrajectoryDesignerTool tool)
        {
            return tool is TrajectoryDesignerTool.Point
                or TrajectoryDesignerTool.Line
                or TrajectoryDesignerTool.Circle
                or TrajectoryDesignerTool.Rectangle
                or TrajectoryDesignerTool.Arc;
        }

        private static bool IsFixedCenterTool(TrajectoryDesignerTool tool)
        {
            return tool is TrajectoryDesignerTool.Circle
                or TrajectoryDesignerTool.Rectangle;
        }

        private static string GetShapeDisplayName(TrajectoryShapeKind kind)
        {
            return kind switch
            {
                TrajectoryShapeKind.Point => "点",
                TrajectoryShapeKind.Line => "线段",
                TrajectoryShapeKind.Polyline => "多线段",
                TrajectoryShapeKind.Rectangle => "矩形",
                TrajectoryShapeKind.Arc => "圆弧",
                _ => "圆"
            };
        }

        private static (double X, double Y) ToLocalPoint(EditableTrajectoryShape shape, double x, double y)
        {
            return TrajectoryGeometryService.ToLocalPoint(
                TrajectoryShapeGeometry.FromEditable(shape),
                x,
                y);
        }

        private static double GetScaledRadius(EditableTrajectoryShape shape)
        {
            return NormalizePositive(shape.Radius, 40d) * GetScale(shape);
        }

        private static double GetScaledWidth(EditableTrajectoryShape shape)
        {
            return NormalizePositive(shape.Width, 90d) * GetScale(shape);
        }

        private static double GetScaledHeight(EditableTrajectoryShape shape)
        {
            return NormalizePositive(shape.Height, 60d) * GetScale(shape);
        }

        private static double GetScale(EditableTrajectoryShape shape)
        {
            return NormalizePositive(shape.Scale, 1d);
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return double.IsFinite(value) && value > 0d ? value : fallback;
        }

        private static double ClampViewportRange(double requestedRange, double fullRange)
        {
            double direction = Math.Sign(requestedRange);
            if (direction == 0d)
            {
                direction = Math.Sign(fullRange);
            }

            if (direction == 0d)
            {
                direction = 1d;
            }

            double fullLength = Math.Max(Math.Abs(fullRange), 1d);
            double minLength = Math.Min(Math.Max(fullLength / 1000d, 1d), fullLength);
            double length = Math.Clamp(Math.Abs(requestedRange), minLength, fullLength);
            return direction * length;
        }

        private static (double Start, double End) ClampViewportAxisToBounds(
            double start,
            double end,
            double fullStart,
            double fullEnd)
        {
            bool increasing = fullEnd >= fullStart;
            double fullMin = Math.Min(fullStart, fullEnd);
            double fullMax = Math.Max(fullStart, fullEnd);
            double fullLength = Math.Max(fullMax - fullMin, 1e-9d);
            double low = Math.Min(start, end);
            double high = Math.Max(start, end);
            double length = Math.Clamp(high - low, 1e-9d, fullLength);

            if (length >= fullLength - 1e-9d)
            {
                return (fullStart, fullEnd);
            }

            double center = (low + high) / 2d;
            low = center - (length / 2d);
            high = center + (length / 2d);

            if (low < fullMin)
            {
                high += fullMin - low;
                low = fullMin;
            }

            if (high > fullMax)
            {
                low -= high - fullMax;
                high = fullMax;
            }

            low = Math.Max(low, fullMin);
            high = Math.Min(high, fullMax);
            return increasing ? (low, high) : (high, low);
        }

        private static double NormalizeCoordinateValue(double value, double fallback)
        {
            return double.IsFinite(value) ? value : fallback;
        }

        private static bool NearlyEqual(double left, double right)
        {
            return Math.Abs(left - right) < 1e-6d;
        }

        private static double NormalizeSweep(double value)
        {
            if (!double.IsFinite(value) || Math.Abs(value) < 1e-6d)
            {
                return 180d;
            }

            return Math.Clamp(Math.Abs(value), 1d, 360d);
        }

        private static double NormalizeSweepDelta(double value)
        {
            double normalized = NormalizeAngle(value);
            if (normalized < 1e-6d)
            {
                normalized = 360d;
            }

            return Math.Clamp(normalized, 1d, 360d);
        }

        private static double NormalizeAngle(double angle)
        {
            double normalized = angle % 360d;
            return normalized < 0d ? normalized + 360d : normalized;
        }

        private static double Distance(double startX, double startY, double endX, double endY)
        {
            double deltaX = endX - startX;
            double deltaY = endY - startY;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }
    }
}


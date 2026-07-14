using Custom.PhysicalScale.Models;
using Custom.PhysicalScale.ViewModels;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Custom.PhysicalScale.Views
{
    /// <summary>
    /// PhysicalScaleView.xaml 的交互逻辑
    /// </summary>
    public partial class PhysicalScaleView : UserControl
    {
        private const double MinLineLengthPx = 0.2;
        private const double MinRectangleSizePx = 0.2;
        private const double MinCircleRadiusPx = 0.05;
        private const double HitToleranceDisplayPx = 4.0;

        private static readonly Brush PointBrush = CreateBrush("#F97316");
        private static readonly Brush LineBrush = CreateBrush("#0EA5E9");
        private static readonly Brush RectangleBrush = CreateBrush("#22C55E");
        private static readonly Brush CircleBrush = CreateBrush("#A855F7");
        private static readonly Brush SelectedBrush = CreateBrush("#FACC15");
        private static readonly Brush PreviewBrush = CreateBrush("#F8FAFC");
        private static readonly Brush LabelBackgroundBrush = CreateBrush("#CC0F172A");

        private PhysicalScaleViewModel _viewModel;
        private bool _isDrawing;
        private bool _isPanning;
        private Point _drawStartImagePoint;
        private PhysicalScaleMeasurement _previewMeasurement;
        private Point _panStartPoint;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;
        private MouseButton? _panMouseButton;

        public PhysicalScaleView()
        {
            InitializeComponent();
            Loaded += PhysicalScaleView_Loaded;
            Unloaded += PhysicalScaleView_Unloaded;
            DataContextChanged += PhysicalScaleView_DataContextChanged;
            ImageScrollViewer.SizeChanged += ImageScrollViewer_SizeChanged;
        }

        private void PhysicalScaleView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as PhysicalScaleViewModel);
            UpdateSurfaceSize();
            RedrawMeasurements();
        }

        private void PhysicalScaleView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel(_viewModel);
        }

        private void PhysicalScaleView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModel(e.OldValue as PhysicalScaleViewModel);
            AttachViewModel(e.NewValue as PhysicalScaleViewModel);
        }

        private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_viewModel?.Model.DisplayImageSource == null)
                return;

            UpdateSurfaceSize();
            RedrawMeasurements();
        }

        private void AttachViewModel(PhysicalScaleViewModel viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
                return;

            _viewModel = viewModel;
            if (_viewModel == null)
                return;

            _viewModel.Model.PropertyChanged += Model_PropertyChanged;
            _viewModel.Model.Measurements.CollectionChanged += Measurements_CollectionChanged;
            _viewModel.FitRequested += ViewModel_FitRequested;
            UpdateCursor();
        }

        private void DetachViewModel(PhysicalScaleViewModel viewModel)
        {
            if (viewModel == null)
                return;

            viewModel.Model.PropertyChanged -= Model_PropertyChanged;
            viewModel.Model.Measurements.CollectionChanged -= Measurements_CollectionChanged;
            viewModel.FitRequested -= ViewModel_FitRequested;

            if (ReferenceEquals(_viewModel, viewModel))
            {
                _viewModel = null;
            }
        }

        private void ViewModel_FitRequested()
        {
            FitImageToViewport();
        }

        private void Measurements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(RedrawMeasurements);
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(PhysicalScaleModel.Zoom):
                    case nameof(PhysicalScaleModel.LoadedImage):
                    case nameof(PhysicalScaleModel.DisplayImageSource):
                    case nameof(PhysicalScaleModel.DisplayImagePixelWidth):
                    case nameof(PhysicalScaleModel.DisplayImagePixelHeight):
                        UpdateSurfaceSize();
                        RedrawMeasurements();
                        break;
                    case nameof(PhysicalScaleModel.SelectedMeasurement):
                    case nameof(PhysicalScaleModel.HoleRoiMeasurement):
                    case nameof(PhysicalScaleModel.SelectedMode):
                    case nameof(PhysicalScaleModel.HolePreviewVisible):
                        RedrawMeasurements();
                        break;
                    case nameof(PhysicalScaleModel.SelectedTool):
                        UpdateCursor();
                        break;
                }
            });
        }

        private void UpdateSurfaceSize()
        {
            if (_viewModel == null)
                return;

            double width = _viewModel.Model.DisplayImagePixelWidth * _viewModel.Model.Zoom;
            double height = _viewModel.Model.DisplayImagePixelHeight * _viewModel.Model.Zoom;

            if (width <= 0 || height <= 0)
            {
                SourceImage.Width = 0;
                SourceImage.Height = 0;
                MeasurementCanvas.Width = 0;
                MeasurementCanvas.Height = 0;
                ImageSurface.Width = 0;
                ImageSurface.Height = 0;
                return;
            }

            SourceImage.Width = width;
            SourceImage.Height = height;
            MeasurementCanvas.Width = width;
            MeasurementCanvas.Height = height;
            ImageSurface.Width = width;
            ImageSurface.Height = height;
        }

        private void RedrawMeasurements()
        {
            MeasurementCanvas.Children.Clear();

            if (_viewModel == null || _viewModel.Model.DisplayImageSource == null)
                return;

            if (_viewModel.Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                if (_viewModel.Model.HolePreviewVisible)
                    return;

                if (_viewModel.Model.HoleRoiMeasurement != null)
                    DrawMeasurement(_viewModel.Model.HoleRoiMeasurement, true, false);

                if (_previewMeasurement != null)
                {
                    DrawMeasurement(_previewMeasurement, false, true);
                }

                return;
            }

            foreach (var measurement in _viewModel.Model.Measurements)
            {
                DrawMeasurement(measurement, ReferenceEquals(measurement, _viewModel.Model.SelectedMeasurement), false);
            }

            if (_previewMeasurement != null)
            {
                DrawMeasurement(_previewMeasurement, false, true);
            }
        }

        private void DrawMeasurement(PhysicalScaleMeasurement measurement, bool isSelected, bool isPreview)
        {
            Brush stroke = isPreview ? PreviewBrush : GetMeasurementBrush(measurement.Tool);
            if (isSelected)
            {
                stroke = SelectedBrush;
            }

            double strokeThickness = isSelected ? 2.0 : 1.2;

            switch (measurement.Tool)
            {
                case PhysicalScaleTool.Point:
                    DrawPoint(measurement, stroke, strokeThickness, isPreview);
                    break;
                case PhysicalScaleTool.Line:
                    DrawLine(measurement, stroke, strokeThickness, isPreview);
                    break;
                case PhysicalScaleTool.Rectangle:
                    DrawRectangle(measurement, stroke, strokeThickness, isPreview);
                    break;
                case PhysicalScaleTool.Circle:
                    DrawCircle(measurement, stroke, strokeThickness, isPreview);
                    break;
            }

            DrawLabel(measurement, isSelected, isPreview);
        }

        private void DrawPoint(PhysicalScaleMeasurement measurement, Brush stroke, double strokeThickness, bool isPreview)
        {
            double centerX = ToDisplayX(measurement.StartX);
            double centerY = ToDisplayY(measurement.StartY);
            double radius = 2.5;

            var point = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Fill = isPreview ? Brushes.Transparent : stroke
            };

            Canvas.SetLeft(point, centerX - radius);
            Canvas.SetTop(point, centerY - radius);
            Canvas.SetZIndex(point, 10);
            MeasurementCanvas.Children.Add(point);

            MeasurementCanvas.Children.Add(new Line
            {
                X1 = centerX - radius * 1.8,
                Y1 = centerY,
                X2 = centerX + radius * 1.8,
                Y2 = centerY,
                Stroke = stroke,
                StrokeThickness = strokeThickness
            });

            MeasurementCanvas.Children.Add(new Line
            {
                X1 = centerX,
                Y1 = centerY - radius * 1.8,
                X2 = centerX,
                Y2 = centerY + radius * 1.8,
                Stroke = stroke,
                StrokeThickness = strokeThickness
            });
        }

        private void DrawLine(PhysicalScaleMeasurement measurement, Brush stroke, double strokeThickness, bool isPreview)
        {
            var line = new Line
            {
                X1 = ToDisplayX(measurement.StartX),
                Y1 = ToDisplayY(measurement.StartY),
                X2 = ToDisplayX(measurement.EndX),
                Y2 = ToDisplayY(measurement.EndY),
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            if (isPreview)
            {
                line.StrokeDashArray = new DoubleCollection { 6, 4 };
            }

            MeasurementCanvas.Children.Add(line);
        }

        private void DrawRectangle(PhysicalScaleMeasurement measurement, Brush stroke, double strokeThickness, bool isPreview)
        {
            var rect = GetNormalizedRect(measurement);
            var rectangle = new Rectangle
            {
                Width = ToDisplayLength(rect.Width),
                Height = ToDisplayLength(rect.Height),
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Fill = Brushes.Transparent,
                RadiusX = 4,
                RadiusY = 4
            };

            if (isPreview)
            {
                rectangle.StrokeDashArray = new DoubleCollection { 6, 4 };
            }

            Canvas.SetLeft(rectangle, ToDisplayX(rect.X));
            Canvas.SetTop(rectangle, ToDisplayY(rect.Y));
            MeasurementCanvas.Children.Add(rectangle);
        }

        private void DrawCircle(PhysicalScaleMeasurement measurement, Brush stroke, double strokeThickness, bool isPreview)
        {
            double radius = ToDisplayLength(measurement.RadiusPx);
            double centerX = ToDisplayX(measurement.StartX);
            double centerY = ToDisplayY(measurement.StartY);

            var ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Fill = Brushes.Transparent
            };

            if (isPreview)
            {
                ellipse.StrokeDashArray = new DoubleCollection { 6, 4 };
            }

            Canvas.SetLeft(ellipse, centerX - radius);
            Canvas.SetTop(ellipse, centerY - radius);
            MeasurementCanvas.Children.Add(ellipse);

            var center = new Ellipse
            {
                Width = 3,
                Height = 3,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Fill = isPreview ? Brushes.Transparent : stroke
            };

            Canvas.SetLeft(center, centerX - 1.5);
            Canvas.SetTop(center, centerY - 1.5);
            Canvas.SetZIndex(center, 10);
            MeasurementCanvas.Children.Add(center);
        }

        private void DrawLabel(PhysicalScaleMeasurement measurement, bool isSelected, bool isPreview)
        {
            Point labelAnchor = GetLabelAnchor(measurement);
            string labelText = GetLabelText(measurement, isPreview);

            var label = new Border
            {
                Background = isSelected ? SelectedBrush : LabelBackgroundBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Child = new TextBlock
                {
                    Text = labelText,
                    Foreground = isSelected ? Brushes.Black : Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                }
            };

            Canvas.SetLeft(label, labelAnchor.X);
            Canvas.SetTop(label, labelAnchor.Y);
            Canvas.SetZIndex(label, 20);
            MeasurementCanvas.Children.Add(label);
        }

        private Point GetLabelAnchor(PhysicalScaleMeasurement measurement)
        {
            const double offset = 12.0;

            switch (measurement.Tool)
            {
                case PhysicalScaleTool.Point:
                    return new Point(ToDisplayX(measurement.StartX) + offset, ToDisplayY(measurement.StartY) - offset);
                case PhysicalScaleTool.Line:
                    return new Point((ToDisplayX(measurement.StartX) + ToDisplayX(measurement.EndX)) / 2 + offset,
                        (ToDisplayY(measurement.StartY) + ToDisplayY(measurement.EndY)) / 2 - offset);
                case PhysicalScaleTool.Rectangle:
                    var rect = GetNormalizedRect(measurement);
                    return new Point(ToDisplayX(rect.X) + offset, ToDisplayY(rect.Y) - offset);
                case PhysicalScaleTool.Circle:
                    return new Point(ToDisplayX(measurement.StartX + measurement.RadiusPx) + offset,
                        ToDisplayY(measurement.StartY - measurement.RadiusPx) - offset);
                default:
                    return new Point(offset, offset);
            }
        }

        private string GetLabelText(PhysicalScaleMeasurement measurement, bool isPreview)
        {
            if (isPreview)
            {
                return measurement.Tool switch
                {
                    PhysicalScaleTool.Point => "点",
                    PhysicalScaleTool.Line => "线",
                    PhysicalScaleTool.Rectangle => "矩形",
                    PhysicalScaleTool.Circle => "圆",
                    _ => "预览"
                };
            }

            string summary = string.IsNullOrWhiteSpace(measurement.PhysicalSummary)
                ? measurement.PixelSummary
                : measurement.PhysicalSummary;

            return string.IsNullOrWhiteSpace(summary)
                ? measurement.DisplayName
                : $"{measurement.DisplayName}  {summary}";
        }

        private void UpdateCursor()
        {
            if (_isPanning)
            {
                MeasurementCanvas.Cursor = Cursors.SizeAll;
                return;
            }

            MeasurementCanvas.Cursor = _viewModel?.Model.SelectedTool == PhysicalScaleTool.Select
                ? Cursors.Arrow
                : Cursors.Cross;
        }

        private void FitImageToViewport()
        {
            if (_viewModel == null || _viewModel.Model.DisplayImagePixelWidth <= 0 || _viewModel.Model.DisplayImagePixelHeight <= 0)
                return;

            double viewportWidth = ImageScrollViewer.ViewportWidth;
            double viewportHeight = ImageScrollViewer.ViewportHeight;

            if (viewportWidth <= 1 || viewportHeight <= 1)
            {
                Dispatcher.BeginInvoke(new Action(FitImageToViewport), DispatcherPriority.Background);
                return;
            }

            double availableWidth = Math.Max(80.0, viewportWidth - 32.0);
            double availableHeight = Math.Max(80.0, viewportHeight - 32.0);
            double zoomX = availableWidth / _viewModel.Model.DisplayImagePixelWidth;
            double zoomY = availableHeight / _viewModel.Model.DisplayImagePixelHeight;
            _viewModel.Model.Zoom = Math.Max(PhysicalScaleViewModel.MinZoomLevel, Math.Min(PhysicalScaleViewModel.MaxZoomLevel, Math.Min(zoomX, zoomY)));
        }

        private Point GetImagePoint(MouseEventArgs e)
        {
            double zoom = GetZoom();
            Point displayPoint = e.GetPosition(MeasurementCanvas);
            double maxX = Math.Max(_viewModel.Model.DisplayImagePixelWidth - 1, 0);
            double maxY = Math.Max(_viewModel.Model.DisplayImagePixelHeight - 1, 0);

            return new Point(
                Math.Clamp(displayPoint.X / zoom, 0, maxX),
                Math.Clamp(displayPoint.Y / zoom, 0, maxY));
        }

        private PhysicalScaleMeasurement CreateMeasurement(PhysicalScaleTool tool, Point startPoint, Point endPoint)
        {
            if (tool == PhysicalScaleTool.Point)
            {
                return new PhysicalScaleMeasurement
                {
                    Tool = PhysicalScaleTool.Point,
                    StartX = startPoint.X,
                    StartY = startPoint.Y,
                    EndX = startPoint.X,
                    EndY = startPoint.Y
                };
            }

            if (tool == PhysicalScaleTool.Circle)
            {
                return new PhysicalScaleMeasurement
                {
                    Tool = PhysicalScaleTool.Circle,
                    StartX = startPoint.X,
                    StartY = startPoint.Y,
                    EndX = endPoint.X,
                    EndY = endPoint.Y,
                    RadiusPx = (endPoint - startPoint).Length
                };
            }

            return new PhysicalScaleMeasurement
            {
                Tool = tool,
                StartX = startPoint.X,
                StartY = startPoint.Y,
                EndX = endPoint.X,
                EndY = endPoint.Y
            };
        }

        private bool IsValidMeasurement(PhysicalScaleMeasurement measurement)
        {
            switch (measurement.Tool)
            {
                case PhysicalScaleTool.Point:
                    return true;
                case PhysicalScaleTool.Line:
                    return (new Point(measurement.EndX, measurement.EndY) - new Point(measurement.StartX, measurement.StartY)).Length >= MinLineLengthPx;
                case PhysicalScaleTool.Rectangle:
                    var rect = GetNormalizedRect(measurement);
                    return rect.Width >= MinRectangleSizePx && rect.Height >= MinRectangleSizePx;
                case PhysicalScaleTool.Circle:
                    return measurement.RadiusPx >= MinCircleRadiusPx;
                default:
                    return false;
            }
        }

        private PhysicalScaleMeasurement FindMeasurementAtPoint(Point imagePoint)
        {
            if (_viewModel == null)
                return null;

            double tolerance = HitToleranceDisplayPx / GetZoom();

            if (_viewModel.Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                var holeMeasurement = _viewModel.Model.HoleRoiMeasurement;
                if (holeMeasurement == null)
                    return null;

                double distance = (new Point(holeMeasurement.StartX, holeMeasurement.StartY) - imagePoint).Length;
                if (distance <= holeMeasurement.RadiusPx || Math.Abs(distance - holeMeasurement.RadiusPx) <= tolerance)
                    return holeMeasurement;

                return null;
            }

            foreach (var measurement in _viewModel.Model.Measurements.Reverse())
            {
                switch (measurement.Tool)
                {
                    case PhysicalScaleTool.Point:
                        if ((new Point(measurement.StartX, measurement.StartY) - imagePoint).Length <= tolerance)
                            return measurement;
                        break;
                    case PhysicalScaleTool.Line:
                        if (DistancePointToSegment(imagePoint,
                            new Point(measurement.StartX, measurement.StartY),
                            new Point(measurement.EndX, measurement.EndY)) <= tolerance)
                            return measurement;
                        break;
                    case PhysicalScaleTool.Rectangle:
                        var rect = GetNormalizedRect(measurement);
                        if (rect.Contains(imagePoint) || IsNearRectangleBorder(rect, imagePoint, tolerance))
                            return measurement;
                        break;
                    case PhysicalScaleTool.Circle:
                        double distance = (new Point(measurement.StartX, measurement.StartY) - imagePoint).Length;
                        if (distance <= measurement.RadiusPx || Math.Abs(distance - measurement.RadiusPx) <= tolerance)
                            return measurement;
                        break;
                }
            }

            return null;
        }

        private static double DistancePointToSegment(Point point, Point lineStart, Point lineEnd)
        {
            Vector line = lineEnd - lineStart;
            if (line.LengthSquared < double.Epsilon)
                return (point - lineStart).Length;

            double t = Vector.Multiply(point - lineStart, line) / line.LengthSquared;
            t = Math.Clamp(t, 0, 1);
            Point projection = lineStart + line * t;
            return (point - projection).Length;
        }

        private static bool IsNearRectangleBorder(Rect rect, Point point, double tolerance)
        {
            if (!rect.Contains(point))
                return false;

            return Math.Abs(point.X - rect.Left) <= tolerance ||
                   Math.Abs(point.X - rect.Right) <= tolerance ||
                   Math.Abs(point.Y - rect.Top) <= tolerance ||
                   Math.Abs(point.Y - rect.Bottom) <= tolerance;
        }

        private static Brush GetMeasurementBrush(PhysicalScaleTool tool)
        {
            return tool switch
            {
                PhysicalScaleTool.Point => PointBrush,
                PhysicalScaleTool.Line => LineBrush,
                PhysicalScaleTool.Rectangle => RectangleBrush,
                PhysicalScaleTool.Circle => CircleBrush,
                _ => PreviewBrush
            };
        }

        private static SolidColorBrush CreateBrush(string colorText)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorText));
            brush.Freeze();
            return brush;
        }

        private static Rect GetNormalizedRect(PhysicalScaleMeasurement measurement)
        {
            double left = Math.Min(measurement.StartX, measurement.EndX);
            double top = Math.Min(measurement.StartY, measurement.EndY);
            double width = Math.Abs(measurement.EndX - measurement.StartX);
            double height = Math.Abs(measurement.EndY - measurement.StartY);
            return new Rect(left, top, width, height);
        }

        private double GetZoom()
        {
            return Math.Max(_viewModel?.Model.Zoom ?? 1.0, PhysicalScaleViewModel.MinZoomLevel);
        }

        private double ToDisplayX(double imageX)
        {
            return imageX * GetZoom();
        }

        private double ToDisplayY(double imageY)
        {
            return imageY * GetZoom();
        }

        private double ToDisplayLength(double imageLength)
        {
            return imageLength * GetZoom();
        }

        private bool EnsureImageLoaded()
        {
            if (_viewModel == null || _viewModel.Model.LoadedImage == null)
            {
                if (_viewModel != null)
                {
                    _viewModel.Model.StatusText = "请先加载连续保存的原始图像";
                }

                return false;
            }

            return true;
        }

        private void ResetPreview(string statusText)
        {
            _isDrawing = false;
            _previewMeasurement = null;

            if (MeasurementCanvas.IsMouseCaptured)
            {
                MeasurementCanvas.ReleaseMouseCapture();
            }

            if (_viewModel != null)
            {
                _viewModel.Model.StatusText = statusText;
            }
        }

        private void EndPan()
        {
            _isPanning = false;
            _panMouseButton = null;
            if (ImageScrollViewer.IsMouseCaptured)
            {
                ImageScrollViewer.ReleaseMouseCapture();
            }

            ImageScrollViewer.ClearValue(CursorProperty);
            UpdateCursor();
        }

        private void ImageCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
                return;

            if (!EnsureImageLoaded())
                return;

            Point imagePoint = GetImagePoint(e);
            PhysicalScaleTool tool = _viewModel.Model.SelectedTool;

            if (tool == PhysicalScaleTool.Select)
            {
                if (_viewModel.Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
                {
                    _viewModel.SelectHoleRoiAt(imagePoint);
                }
                else
                {
                    _viewModel.Model.SelectedMeasurement = FindMeasurementAtPoint(imagePoint);
                    _viewModel.Model.StatusText = _viewModel.Model.SelectedMeasurement == null
                        ? "当前位置没有量测图形"
                        : $"当前选中：{_viewModel.Model.SelectedMeasurement.DisplayName}";
                }
                RedrawMeasurements();
                e.Handled = true;
                return;
            }

            if (tool == PhysicalScaleTool.Point)
            {
                _viewModel.AddMeasurement(CreateMeasurement(PhysicalScaleTool.Point, imagePoint, imagePoint));
                RedrawMeasurements();
                e.Handled = true;
                return;
            }

            _isDrawing = true;
            _drawStartImagePoint = imagePoint;
            _previewMeasurement = CreateMeasurement(tool, imagePoint, imagePoint);
            _viewModel.Model.SelectedMeasurement = null;
            _viewModel.Model.StatusText = "拖动鼠标完成量测图形";
            MeasurementCanvas.CaptureMouse();
            RedrawMeasurements();
            e.Handled = true;
        }

        private void ImageCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel != null && _viewModel.Model.LoadedImage != null)
            {
                Point hoverPoint = GetImagePoint(e);
                _viewModel.UpdateCursorPosition(hoverPoint.X, hoverPoint.Y);
            }

            if (_isPanning)
                return;

            if (!_isDrawing || _viewModel == null || _previewMeasurement == null)
                return;

            Point currentPoint = GetImagePoint(e);
            _previewMeasurement = CreateMeasurement(_viewModel.Model.SelectedTool, _drawStartImagePoint, currentPoint);
            RedrawMeasurements();
            e.Handled = true;
        }

        private void ImageCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
                return;

            if (!_isDrawing || _viewModel == null)
                return;

            Point endPoint = GetImagePoint(e);
            var measurement = CreateMeasurement(_viewModel.Model.SelectedTool, _drawStartImagePoint, endPoint);
            ResetPreview("已完成量测图形");

            if (!IsValidMeasurement(measurement))
            {
                _viewModel.Model.StatusText = "图形尺寸过小，未生成量测结果";
                RedrawMeasurements();
                e.Handled = true;
                return;
            }

            if (_viewModel.Model.SelectedMode == PhysicalScaleMode.MicroHoleJudgement)
            {
                _viewModel.SetHoleRoiMeasurement(measurement);
            }
            else
            {
                _viewModel.AddMeasurement(measurement);
            }
            RedrawMeasurements();
            e.Handled = true;
        }

        private void ImageCanvas_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                EndPan();
                e.Handled = true;
                return;
            }

            if (_isDrawing)
            {
                ResetPreview("已取消当前绘制");
                RedrawMeasurements();
                e.Handled = true;
                return;
            }
        }

        private void ImageCanvas_OnMouseLeave(object sender, MouseEventArgs e)
        {
            _viewModel?.ClearCursorPosition();
        }

        private void ImageScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_viewModel == null || _viewModel.Model.DisplayImageSource == null)
                return;

            Point pointerInViewport = e.GetPosition(ImageScrollViewer);
            double oldZoom = GetZoom();
            double factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
            double newZoom = Math.Max(
                PhysicalScaleViewModel.MinZoomLevel,
                Math.Min(PhysicalScaleViewModel.MaxZoomLevel, oldZoom * factor));

            if (Math.Abs(newZoom - oldZoom) < 0.0001)
                return;

            double contentX = Math.Clamp(ImageScrollViewer.HorizontalOffset + pointerInViewport.X, 0, ImageSurface.ActualWidth);
            double contentY = Math.Clamp(ImageScrollViewer.VerticalOffset + pointerInViewport.Y, 0, ImageSurface.ActualHeight);
            double imageX = contentX / oldZoom;
            double imageY = contentY / oldZoom;

            _viewModel.Model.Zoom = newZoom;
            ImageScrollViewer.UpdateLayout();

            double targetHorizontalOffset = imageX * newZoom - pointerInViewport.X;
            double targetVerticalOffset = imageY * newZoom - pointerInViewport.Y;

            double maxHorizontalOffset = Math.Max(0, ImageSurface.ActualWidth - ImageScrollViewer.ViewportWidth);
            double maxVerticalOffset = Math.Max(0, ImageSurface.ActualHeight - ImageScrollViewer.ViewportHeight);

            ImageScrollViewer.ScrollToHorizontalOffset(Math.Clamp(targetHorizontalOffset, 0, maxHorizontalOffset));
            ImageScrollViewer.ScrollToVerticalOffset(Math.Clamp(targetVerticalOffset, 0, maxVerticalOffset));

            e.Handled = true;
        }

        private void ImageScrollViewer_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right && e.ChangedButton != MouseButton.Middle)
                return;

            if (_viewModel == null || _viewModel.Model.DisplayImageSource == null)
                return;

            if (_isDrawing)
                return;

            _isPanning = true;
            _panMouseButton = e.ChangedButton;
            _panStartPoint = e.GetPosition(ImageScrollViewer);
            _panStartHorizontalOffset = ImageScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = ImageScrollViewer.VerticalOffset;
            ImageScrollViewer.CaptureMouse();
            ImageScrollViewer.Cursor = Cursors.SizeAll;
            UpdateCursor();
            e.Handled = true;
        }

        private void ImageScrollViewer_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_panMouseButton == null || e.ChangedButton != _panMouseButton.Value)
                return;

            if (!_isPanning)
                return;

            EndPan();
            e.Handled = true;
        }

        private void ImageScrollViewer_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
                return;

            Point currentPoint = e.GetPosition(ImageScrollViewer);
            Vector delta = currentPoint - _panStartPoint;
            ImageScrollViewer.ScrollToHorizontalOffset(Math.Max(0, _panStartHorizontalOffset - delta.X));
            ImageScrollViewer.ScrollToVerticalOffset(Math.Max(0, _panStartVerticalOffset - delta.Y));
            e.Handled = true;
        }
    }
}

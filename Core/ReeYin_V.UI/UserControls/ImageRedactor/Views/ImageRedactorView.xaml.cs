using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReeYin_V.UI.UserControls.ImageRedactor
{
    public partial class ImageRedactorView : UserControl, IDisposable
    {
        public static readonly DependencyProperty ImageFilePathProperty =
            DependencyProperty.Register(
                nameof(ImageFilePath),
                typeof(string),
                typeof(ImageRedactorView),
                new PropertyMetadata(string.Empty, OnImageFilePathChanged));

        private readonly ImageRedactorViewModel _viewModel;
        private bool _isDrawing;
        private bool _disposed;

        public ImageRedactorView()
        {
            InitializeComponent();
            _viewModel = new ImageRedactorViewModel();
            DataContext = _viewModel;
        }

        public string ImageFilePath
        {
            get => (string)GetValue(ImageFilePathProperty);
            set => SetValue(ImageFilePathProperty, value);
        }

        public ImageRedactorViewModel ViewModel => _viewModel;

        public bool LoadImage(string filePath)
        {
            bool result = _viewModel.LoadBackgroundImage(filePath);
            if (result)
            {
                FitImageToViewport(resetToImageBounds: true);
            }

            return result;
        }

        public bool LoadAnnotationImage(string filePath)
        {
            return _viewModel.LoadAnnotationImage(filePath);
        }

        public void ClearOverlays()
        {
            _viewModel.ClearOverlays();
        }

        public void ResetToolMode()
        {
            _isDrawing = false;
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.None);
            UpdateMouseInteractionMode();
        }

        public void ZoomToFitChart()
        {
            ZoomToFit();
        }

        private static void OnImageFilePathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is not ImageRedactorView view)
            {
                return;
            }

            string? filePath = args.NewValue as string;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            view.LoadImage(filePath);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachChartEvents();
            UpdateMouseInteractionMode();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.HasBackgroundImage)
                {
                    FitImageToViewport(resetToImageBounds: false);
                }
            }), DispatcherPriority.Loaded);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void AttachChartEvents()
        {
            PART_Chart.PreviewMouseLeftButtonDown -= Chart_PreviewMouseLeftButtonDown;
            PART_Chart.PreviewMouseMove -= Chart_PreviewMouseMove;
            PART_Chart.PreviewMouseLeftButtonUp -= Chart_PreviewMouseLeftButtonUp;
            PART_Chart.SizeChanged -= Chart_SizeChanged;

            PART_Chart.PreviewMouseLeftButtonDown += Chart_PreviewMouseLeftButtonDown;
            PART_Chart.PreviewMouseMove += Chart_PreviewMouseMove;
            PART_Chart.PreviewMouseLeftButtonUp += Chart_PreviewMouseLeftButtonUp;
            PART_Chart.SizeChanged += Chart_SizeChanged;
        }

        private void DetachChartEvents()
        {
            PART_Chart.PreviewMouseLeftButtonDown -= Chart_PreviewMouseLeftButtonDown;
            PART_Chart.PreviewMouseMove -= Chart_PreviewMouseMove;
            PART_Chart.PreviewMouseLeftButtonUp -= Chart_PreviewMouseLeftButtonUp;
            PART_Chart.SizeChanged -= Chart_SizeChanged;
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Background Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ImageFilePath = dialog.FileName;
            }
        }

        private void ZoomToFitButton_Click(object sender, RoutedEventArgs e)
        {
            FitImageToViewport(resetToImageBounds: true);
        }

        private void AddTextAnnotationButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.AddTextAnnotation);
            UpdateMouseInteractionMode();
        }

        private void SelectAnnotationImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Annotation Bitmap",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.LoadAnnotationImage(dialog.FileName);
            }
        }

        private void AddBitmapAnnotationButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.AddBitmapAnnotation);
            UpdateMouseInteractionMode();
        }

        private void DrawRingPointButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.AddRingPoint);
            UpdateMouseInteractionMode();
        }

        private void PickColorButton_Click(object sender, RoutedEventArgs e)
        {
            ImageColorPickerDialog dialog = new(_viewModel.DrawColor)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.SetDrawColor(dialog.SelectedColor);
            }
        }

        private void LineWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            _viewModel.SetLineWidth(e.NewValue);
        }

        private void DrawPolygonButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.AddPolygon);
            UpdateMouseInteractionMode();
        }

        private void DrawPolylineButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelInProgressSketch();
            _viewModel.SetEditMode(ImageRedactorEditMode.AddPolyline);
            UpdateMouseInteractionMode();
        }

        private void ClearOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            _isDrawing = false;
            _viewModel.ClearOverlays();
        }

        private void ResetModeButton_Click(object sender, RoutedEventArgs e)
        {
            ResetToolMode();
        }

        private void Chart_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_viewModel.HasBackgroundImage)
            {
                return;
            }

            if (!TryGetAxisValues(e, out double xValue, out double yValue))
            {
                return;
            }

            switch (_viewModel.EditMode)
            {
                case ImageRedactorEditMode.AddTextAnnotation:
                    if (_viewModel.AddTextAnnotation(xValue, yValue))
                    {
                        e.Handled = true;
                    }
                    break;

                case ImageRedactorEditMode.AddBitmapAnnotation:
                    if (_viewModel.AddBitmapAnnotation(xValue, yValue))
                    {
                        e.Handled = true;
                    }
                    break;

                case ImageRedactorEditMode.AddRingPoint:
                    if (_viewModel.AddRingPoint(xValue, yValue))
                    {
                        e.Handled = true;
                    }
                    break;

                case ImageRedactorEditMode.AddPolygon:
                case ImageRedactorEditMode.AddPolyline:
                    _isDrawing = _viewModel.StartOrContinueSketch(xValue, yValue);
                    e.Handled = _isDrawing;
                    break;
            }
        }

        private void Chart_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (_viewModel.EditMode is not (ImageRedactorEditMode.AddPolygon or ImageRedactorEditMode.AddPolyline))
            {
                return;
            }

            if (!TryGetAxisValues(e, out double xValue, out double yValue))
            {
                return;
            }

            _viewModel.StartOrContinueSketch(xValue, yValue);
            e.Handled = true;
        }

        private void Chart_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing)
            {
                return;
            }

            _isDrawing = false;
            _viewModel.FinishSketch();
            e.Handled = true;
        }

        private bool TryGetAxisValues(MouseEventArgs e, out double xValue, out double yValue)
        {
            xValue = 0;
            yValue = 0;

            if (PART_Chart.ViewXY is null || _viewModel.XAxes.Count == 0 || _viewModel.YAxes.Count == 0)
            {
                return false;
            }

            Point position = e.GetPosition(PART_Chart);

            _viewModel.XAxes[0].CoordToValue((int)position.X, out xValue, false);
            _viewModel.YAxes[0].CoordToValue((int)position.Y, out yValue);

            xValue = Math.Max(0, Math.Min(_viewModel.ImageWidth, xValue));
            yValue = Math.Max(0, Math.Min(_viewModel.ImageHeight, yValue));
            return true;
        }

        private void UpdateMouseInteractionMode()
        {
            if (PART_Chart.ViewXY is null)
            {
                return;
            }

            PART_Chart.ViewXY.ZoomPanOptions.DevicePrimaryButtonAction =
                _viewModel.EditMode == ImageRedactorEditMode.None
                    ? UserInteractiveDeviceButtonAction.Zoom
                    : UserInteractiveDeviceButtonAction.None;
        }

        private void Chart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_viewModel.HasBackgroundImage || e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
            {
                return;
            }

            FitImageToViewport(resetToImageBounds: false);
        }

        private void ZoomToFit()
        {
            FitImageToViewport(resetToImageBounds: true);
        }

        private void FitImageToViewport(bool resetToImageBounds)
        {
            if (PART_Chart.ViewXY is null || !_viewModel.HasBackgroundImage)
            {
                return;
            }

            double chartWidth = PART_Chart.ActualWidth;
            double chartHeight = PART_Chart.ActualHeight;
            if (chartWidth <= 0 || chartHeight <= 0)
            {
                return;
            }

            double xMin;
            double xMax;
            double yMin;
            double yMax;

            if (resetToImageBounds)
            {
                xMin = 0;
                xMax = _viewModel.ImageWidth;
                yMin = 0;
                yMax = _viewModel.ImageHeight;
            }
            else
            {
                xMin = _viewModel.XAxes[0].Minimum;
                xMax = _viewModel.XAxes[0].Maximum;
                yMin = _viewModel.YAxes[0].Minimum;
                yMax = _viewModel.YAxes[0].Maximum;
            }

            double visibleWidth = Math.Abs(xMax - xMin);
            double visibleHeight = Math.Abs(yMax - yMin);

            if (visibleWidth <= 0 || visibleHeight <= 0)
            {
                return;
            }

            double viewportAspect = chartWidth / chartHeight;
            double visibleAspect = visibleWidth / visibleHeight;
            double centerX = (xMin + xMax) * 0.5;
            double centerY = (yMin + yMax) * 0.5;

            if (visibleAspect < viewportAspect)
            {
                visibleWidth = visibleHeight * viewportAspect;
            }
            else
            {
                visibleHeight = visibleWidth / viewportAspect;
            }

            _viewModel.XAxes[0].Minimum = centerX - visibleWidth * 0.5;
            _viewModel.XAxes[0].Maximum = centerX + visibleWidth * 0.5;
            _viewModel.YAxes[0].Minimum = centerY - visibleHeight * 0.5;
            _viewModel.YAxes[0].Maximum = centerY + visibleHeight * 0.5;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DetachChartEvents();
        }
    }
}

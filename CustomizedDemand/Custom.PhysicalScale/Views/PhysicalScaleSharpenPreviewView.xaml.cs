using Custom.PhysicalScale.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Custom.PhysicalScale.Views
{
    public partial class PhysicalScaleSharpenPreviewView : UserControl
    {
        private bool _isPanning;
        private MouseButton? _panMouseButton;
        private Point _panStartPoint;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;
        private PhysicalScaleSharpenPreviewViewModel _viewModel;
        private bool _pendingViewportReset;

        public PhysicalScaleSharpenPreviewView()
        {
            InitializeComponent();
            Loaded += PhysicalScaleSharpenPreviewView_Loaded;
            Unloaded += PhysicalScaleSharpenPreviewView_Unloaded;
            DataContextChanged += PhysicalScaleSharpenPreviewView_DataContextChanged;
            PreviewScrollViewer.ScrollChanged += PreviewScrollViewer_OnScrollChanged;
            PreviewScrollViewer.SizeChanged += PreviewScrollViewer_OnSizeChanged;
            PreviewSurface.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
            PreviewSurface.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
            PreviewImage.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
            PreviewImage.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
            PreviewGuideCanvas.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
            PreviewGuideCanvas.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(PreviewScrollViewer_OnPreviewMouseWheel), true);
        }

        private void PhysicalScaleSharpenPreviewView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as PhysicalScaleSharpenPreviewViewModel);
            UpdateSurfaceSize();
            Dispatcher.BeginInvoke(new Action(CenterPreviewViewport), DispatcherPriority.Loaded);
        }

        private void PhysicalScaleSharpenPreviewView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel(_viewModel);
            EndPan();
        }

        private void PhysicalScaleSharpenPreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModel(e.OldValue as PhysicalScaleSharpenPreviewViewModel);
            AttachViewModel(e.NewValue as PhysicalScaleSharpenPreviewViewModel);
            UpdateSurfaceSize();
            Dispatcher.BeginInvoke(new Action(CenterPreviewViewport), DispatcherPriority.Loaded);
        }

        private void AttachViewModel(PhysicalScaleSharpenPreviewViewModel viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
                return;

            _viewModel = viewModel;
            if (_viewModel == null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.PreviewViewportResetRequested += ViewModel_PreviewViewportResetRequested;
        }

        private void DetachViewModel(PhysicalScaleSharpenPreviewViewModel viewModel)
        {
            if (viewModel == null)
                return;

            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.PreviewViewportResetRequested -= ViewModel_PreviewViewportResetRequested;

            if (ReferenceEquals(_viewModel, viewModel))
            {
                _viewModel = null;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhysicalScaleSharpenPreviewViewModel.DisplayImage))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateSurfaceSize();
                    SyncViewportToViewModel();
                }), DispatcherPriority.Loaded);
            }
            else if (e.PropertyName == nameof(PhysicalScaleSharpenPreviewViewModel.PreviewZoom))
            {
                Dispatcher.BeginInvoke(new Action(UpdateSurfaceSize), DispatcherPriority.Loaded);
            }
        }

        private void ViewModel_PreviewViewportResetRequested()
        {
            _pendingViewportReset = true;
            Dispatcher.BeginInvoke(new Action(CenterPreviewViewport), DispatcherPriority.Loaded);
        }

        private void PreviewScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            SyncViewportToViewModel();
        }

        private void PreviewScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSurfaceSize();
                SyncViewportToViewModel();
            }), DispatcherPriority.Loaded);
        }

        private void PreviewScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not PhysicalScaleSharpenPreviewViewModel viewModel)
                return;

            if (PreviewImage.Source == null || PreviewImage.ActualWidth <= 0 || PreviewImage.ActualHeight <= 0)
                return;

            double oldZoom = Math.Max(viewModel.PreviewZoom, 0.1);
            double newZoom = viewModel.GetNextPreviewZoom(e.Delta > 0);

            if (Math.Abs(newZoom - oldZoom) < 0.0001)
                return;

            Point pointerInViewport = e.GetPosition(PreviewScrollViewer);
            Point pointerInContent = GetContentPoint(pointerInViewport);
            double imageX = Math.Clamp(pointerInContent.X / oldZoom, 0, PreviewImage.ActualWidth / oldZoom);
            double imageY = Math.Clamp(pointerInContent.Y / oldZoom, 0, PreviewImage.ActualHeight / oldZoom);

            viewModel.PreviewZoom = newZoom;
            UpdateSurfaceSize();
            PreviewScrollViewer.UpdateLayout();
            ScrollViewportToImagePoint(imageX, imageY, pointerInViewport.X, pointerInViewport.Y);
            SyncViewportToViewModel();
        }

        private void PreviewGuideCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PhysicalScaleSharpenPreviewViewModel viewModel)
                return;

            Point guidePoint = e.GetPosition(PreviewGuideCanvas);
            viewModel.UpdateSampleGuideByDisplayPoint(guidePoint.X, guidePoint.Y, updateVertical: true, updateHorizontal: true);
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                viewModel.ApplyCircleFitCenterFromDisplayPoint(guidePoint.X, guidePoint.Y);
            }

            e.Handled = true;
        }

        private void PreviewScrollViewer_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right && e.ChangedButton != MouseButton.Middle)
                return;

            if (DataContext is not PhysicalScaleSharpenPreviewViewModel)
                return;

            _isPanning = true;
            _panMouseButton = e.ChangedButton;
            _panStartPoint = e.GetPosition(PreviewScrollViewer);
            _panStartHorizontalOffset = PreviewScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = PreviewScrollViewer.VerticalOffset;
            PreviewScrollViewer.CaptureMouse();
            PreviewScrollViewer.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void PreviewScrollViewer_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_panMouseButton == null || e.ChangedButton != _panMouseButton.Value)
                return;

            if (!_isPanning)
                return;

            EndPan();
            e.Handled = true;
        }

        private void PreviewScrollViewer_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
                return;

            Point currentPoint = e.GetPosition(PreviewScrollViewer);
            Vector delta = currentPoint - _panStartPoint;
            double maxHorizontalOffset = Math.Max(0, PreviewScrollViewer.ExtentWidth - PreviewScrollViewer.ViewportWidth);
            double maxVerticalOffset = Math.Max(0, PreviewScrollViewer.ExtentHeight - PreviewScrollViewer.ViewportHeight);
            PreviewScrollViewer.ScrollToHorizontalOffset(Math.Clamp(_panStartHorizontalOffset - delta.X, 0, maxHorizontalOffset));
            PreviewScrollViewer.ScrollToVerticalOffset(Math.Clamp(_panStartVerticalOffset - delta.Y, 0, maxVerticalOffset));
            SyncViewportToViewModel();
            e.Handled = true;
        }

        private void EndPan()
        {
            _isPanning = false;
            _panMouseButton = null;
            if (PreviewScrollViewer.IsMouseCaptured)
            {
                PreviewScrollViewer.ReleaseMouseCapture();
            }

            PreviewScrollViewer.ClearValue(CursorProperty);
        }

        private void CenterPreviewViewport()
        {
            _pendingViewportReset = false;
            if (PreviewImage.Source == null)
            {
                SyncViewportToViewModel();
                return;
            }

            PreviewScrollViewer.UpdateLayout();

            double horizontalOffset = Math.Max(0, (PreviewScrollViewer.ExtentWidth - PreviewScrollViewer.ViewportWidth) * 0.5);
            double verticalOffset = Math.Max(0, (PreviewScrollViewer.ExtentHeight - PreviewScrollViewer.ViewportHeight) * 0.5);
            PreviewScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            PreviewScrollViewer.ScrollToVerticalOffset(verticalOffset);
            SyncViewportToViewModel();
        }

        private void UpdateSurfaceSize()
        {
            if (_viewModel == null)
                return;

            if (PreviewImage.Source is not System.Windows.Media.Imaging.BitmapSource bitmap)
            {
                PreviewImage.Width = 0;
                PreviewImage.Height = 0;
                PreviewGuideCanvas.Width = 0;
                PreviewGuideCanvas.Height = 0;
                PreviewSurface.Width = 0;
                PreviewSurface.Height = 0;
                return;
            }

            double zoom = Math.Max(_viewModel.PreviewZoom, 0.1);
            double contentWidth = bitmap.PixelWidth * zoom;
            double contentHeight = bitmap.PixelHeight * zoom;
            double surfaceWidth = Math.Max(contentWidth, PreviewScrollViewer.ViewportWidth);
            double surfaceHeight = Math.Max(contentHeight, PreviewScrollViewer.ViewportHeight);

            PreviewImage.Width = contentWidth;
            PreviewImage.Height = contentHeight;
            PreviewGuideCanvas.Width = contentWidth;
            PreviewGuideCanvas.Height = contentHeight;
            PreviewSurface.Width = surfaceWidth;
            PreviewSurface.Height = surfaceHeight;
        }

        private void SyncViewportToViewModel()
        {
            if (_viewModel == null)
                return;

            if (PreviewImage.Source is not System.Windows.Media.Imaging.BitmapSource bitmap)
            {
                _viewModel.UpdateViewport(0, 0, 0, 0);
                return;
            }

            double zoom = Math.Max(_viewModel.PreviewZoom, 0.1);
            double contentWidth = bitmap.PixelWidth * zoom;
            double contentHeight = bitmap.PixelHeight * zoom;
            double originX = Math.Max(0, (PreviewSurface.ActualWidth - contentWidth) * 0.5);
            double originY = Math.Max(0, (PreviewSurface.ActualHeight - contentHeight) * 0.5);
            double visibleLeft = Math.Max(originX, PreviewScrollViewer.HorizontalOffset);
            double visibleTop = Math.Max(originY, PreviewScrollViewer.VerticalOffset);
            double visibleRight = Math.Min(originX + contentWidth, PreviewScrollViewer.HorizontalOffset + PreviewScrollViewer.ViewportWidth);
            double visibleBottom = Math.Min(originY + contentHeight, PreviewScrollViewer.VerticalOffset + PreviewScrollViewer.ViewportHeight);

            double viewportX = Math.Clamp(Math.Floor((visibleLeft - originX) / zoom), 0, Math.Max(0, bitmap.PixelWidth - 1));
            double viewportY = Math.Clamp(Math.Floor((visibleTop - originY) / zoom), 0, Math.Max(0, bitmap.PixelHeight - 1));
            double viewportWidth = Math.Clamp(Math.Ceiling((visibleRight - visibleLeft) / zoom), 1, bitmap.PixelWidth - viewportX);
            double viewportHeight = Math.Clamp(Math.Ceiling((visibleBottom - visibleTop) / zoom), 1, bitmap.PixelHeight - viewportY);
            _viewModel.UpdateViewport(viewportX, viewportY, viewportWidth, viewportHeight);
        }

        private void ScrollViewportToImagePoint(double imageX, double imageY, double anchorViewportX, double anchorViewportY)
        {
            double zoom = Math.Max(_viewModel?.PreviewZoom ?? 1.0, 0.1);
            double originX = Math.Max(0, (PreviewSurface.ActualWidth - PreviewImage.Width) * 0.5);
            double originY = Math.Max(0, (PreviewSurface.ActualHeight - PreviewImage.Height) * 0.5);
            double targetHorizontalOffset = originX + imageX * zoom - anchorViewportX;
            double targetVerticalOffset = originY + imageY * zoom - anchorViewportY;
            double maxHorizontalOffset = Math.Max(0, PreviewScrollViewer.ExtentWidth - PreviewScrollViewer.ViewportWidth);
            double maxVerticalOffset = Math.Max(0, PreviewScrollViewer.ExtentHeight - PreviewScrollViewer.ViewportHeight);

            PreviewScrollViewer.ScrollToHorizontalOffset(Math.Clamp(targetHorizontalOffset, 0, maxHorizontalOffset));
            PreviewScrollViewer.ScrollToVerticalOffset(Math.Clamp(targetVerticalOffset, 0, maxVerticalOffset));
        }

        private Point GetContentPoint(Point viewportPoint)
        {
            double contentX = Math.Clamp(
                PreviewScrollViewer.HorizontalOffset + viewportPoint.X,
                0,
                Math.Max(0, PreviewScrollViewer.ExtentWidth));
            double contentY = Math.Clamp(
                PreviewScrollViewer.VerticalOffset + viewportPoint.Y,
                0,
                Math.Max(0, PreviewScrollViewer.ExtentHeight));
            return new Point(contentX, contentY);
        }

    }
}

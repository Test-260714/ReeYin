using Custom.CalibrationPlateMeasure.ViewModels;
using Prism.Dialogs;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfCursors = System.Windows.Input.Cursors;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace Custom.CalibrationPlateMeasure.Views
{
    // 灰度图交互视图：负责图像缩放平移、ROI 框选、XYZ 光标值和点云窗口切换。
    public partial class CalibrationPlateMeasureView : WpfUserControl
    {
        private const double DialogHostWidth = 1600;
        private const double DialogHostHeight = 920;

        private readonly CalibrationPlatePointCloudViewerView _pointCloudView;
        private CalibrationPlateMeasureViewModel? _viewModel;
        private bool _isGrayImageDragging;
        private bool _isRoiSelecting;
        private WpfPoint _lastGrayImageMousePosition;
        private WpfPoint _roiSelectionStartPosition;
        private int _pointCloudRenderRequestId;

        public CalibrationPlateMeasureView()
        {
            InitializeComponent();

            _pointCloudView = new CalibrationPlatePointCloudViewerView
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };

            DataContextChanged += CalibrationPlateMeasureView_DataContextChanged;
        }

        private CalibrationPlateMeasureViewModel? ViewModel => DataContext as CalibrationPlateMeasureViewModel;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachViewModelEvents(ViewModel);
            ApplyDialogHostSize();
            ApplyCenterDisplayMode();
        }

        private void ApplyDialogHostSize()
        {
            Window? hostWindow = Window.GetWindow(this);
            if (hostWindow is not IDialogWindow)
            {
                return;
            }

            // 设计模式算法工具通过 Prism 弹窗承载，显式固定尺寸以对齐运行模式视图体验。
            hostWindow.SizeToContent = SizeToContent.Manual;
            hostWindow.Width = DialogHostWidth;
            hostWindow.Height = DialogHostHeight;
            hostWindow.MinWidth = DialogHostWidth;
            hostWindow.MinHeight = DialogHostHeight;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModelEvents();
            DetachPointCloudViewFromHost();
        }

        private void CalibrationPlateMeasureView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModelEvents();
            AttachViewModelEvents(e.NewValue as CalibrationPlateMeasureViewModel);
            ApplyCenterDisplayMode();
        }

        private void AttachViewModelEvents(CalibrationPlateMeasureViewModel? nextViewModel)
        {
            if (ReferenceEquals(_viewModel, nextViewModel))
            {
                return;
            }

            DetachViewModelEvents();
            _viewModel = nextViewModel;
            if (_viewModel == null)
            {
                _pointCloudView.DataContext = null;
                return;
            }

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.GrayImageRefreshRequested += ViewModel_GrayImageRefreshRequested;
            _pointCloudView.Tag = _viewModel;
            _pointCloudView.DataContext = _viewModel.EmbeddedPointCloudViewModel;
        }

        private void DetachViewModelEvents()
        {
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.GrayImageRefreshRequested -= ViewModel_GrayImageRefreshRequested;
            _pointCloudView.DataContext = null;
            _pointCloudView.Tag = null;
            _viewModel = null;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalibrationPlateMeasureViewModel.ModelParam)
                || e.PropertyName == nameof(CalibrationPlateMeasureViewModel.CenterDisplayMode))
            {
                ApplyCenterDisplayMode();
            }
        }

        private void ViewModel_GrayImageRefreshRequested(object? sender, EventArgs e)
        {
            if (ViewModel?.CenterDisplayMode != CalibrationPlateCenterDisplayMode.GrayImage)
            {
                return;
            }

            ApplyCenterDisplayMode();
        }

        private void ApplyCenterDisplayMode()
        {
            if (ViewModel?.CenterDisplayMode == CalibrationPlateCenterDisplayMode.PointCloud)
            {
                AttachPointCloudViewToHost();
                SchedulePointCloudRender(includeFirstShowWarmup: true);
                return;
            }

            AttachPointCloudViewToHost();
            UpdateSelectedRoiOverlayFromModel();
        }

        private void AttachPointCloudViewToHost()
        {
            if (!ReferenceEquals(PointCloudHost.Content, _pointCloudView))
            {
                PointCloudHost.Content = _pointCloudView;
            }
        }

        private void DetachPointCloudViewFromHost()
        {
            if (PointCloudHost.Content != null)
            {
                PointCloudHost.Content = null;
            }
        }

        private void SchedulePointCloudRender(bool includeFirstShowWarmup)
        {
            // VTK 原生窗口随 WPF 切换显示时需要多阶段刷新，避免首次切换空白。
            int requestId = ++_pointCloudRenderRequestId;
            RunPointCloudRenderPass(requestId);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RunPointCloudRenderPass(requestId);
            }), DispatcherPriority.DataBind);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RunPointCloudRenderPass(requestId);
            }), DispatcherPriority.Loaded);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RunPointCloudRenderPass(requestId);
            }), DispatcherPriority.Render);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RunPointCloudRenderPass(requestId);
            }), DispatcherPriority.ContextIdle);

            if (!includeFirstShowWarmup)
            {
                return;
            }

            _ = Dispatcher.InvokeAsync(async () =>
            {
                foreach (int delayMs in new[] { 60, 140, 260, 420 })
                {
                    await Task.Delay(delayMs);
                    RunPointCloudRenderPass(requestId);
                }
            }, DispatcherPriority.Background);
        }

        private void RunPointCloudRenderPass(int requestId)
        {
            if (requestId != _pointCloudRenderRequestId ||
                ViewModel?.CenterDisplayMode != CalibrationPlateCenterDisplayMode.PointCloud)
            {
                return;
            }

            PointCloudHost.UpdateLayout();
            _pointCloudView.UpdateLayout();
            ForceNativeChildWindowRedraw(sendMouseMove: true);
            _pointCloudView.ForceRender();
        }

        private void ForceNativeChildWindowRedraw(bool sendMouseMove)
        {
            Window? window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            IntPtr rootHandle = new WindowInteropHelper(window).Handle;
            if (rootHandle == IntPtr.Zero)
            {
                return;
            }

            RedrawNativeWindow(rootHandle);
            EnumChildWindows(rootHandle, (childHandle, _) =>
            {
                RedrawNativeWindow(childHandle);
                if (sendMouseMove)
                {
                    PostMessage(childHandle, WindowMessages.MouseMove, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }, IntPtr.Zero);
        }

        private static void RedrawNativeWindow(IntPtr handle)
        {
            SetWindowPos(
                handle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NoMove
                | SetWindowPosFlags.NoSize
                | SetWindowPosFlags.NoZOrder
                | SetWindowPosFlags.NoActivate
                | SetWindowPosFlags.ShowWindow
                | SetWindowPosFlags.FrameChanged);

            RedrawWindow(
                handle,
                IntPtr.Zero,
                IntPtr.Zero,
                RedrawWindowFlags.Invalidate
                | RedrawWindowFlags.UpdateNow
                | RedrawWindowFlags.AllChildren);
            UpdateWindow(handle);
        }

        private void GrayImageInteractionLayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (GrayImageViewer.Source == null)
            {
                return;
            }

            WpfPoint mousePosition = e.GetPosition(GrayImageInteractionLayer);
            double oldScale = GrayImageScaleTransform.ScaleX;
            double scaleFactor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
            double newScale = Math.Clamp(oldScale * scaleFactor, 0.2, 20.0);
            if (Math.Abs(newScale - oldScale) < 0.000001)
            {
                return;
            }

            double localX = (mousePosition.X - GrayImageTranslateTransform.X) / oldScale;
            double localY = (mousePosition.Y - GrayImageTranslateTransform.Y) / oldScale;
            GrayImageScaleTransform.ScaleX = newScale;
            GrayImageScaleTransform.ScaleY = newScale;
            GrayImageTranslateTransform.X = mousePosition.X - localX * newScale;
            GrayImageTranslateTransform.Y = mousePosition.Y - localY * newScale;
            UpdateSelectedRoiOverlayFromModel();
            UpdateCursorCoordinate(mousePosition);
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GrayImageViewer.Source == null)
            {
                return;
            }

            _isRoiSelecting = true;
            _roiSelectionStartPosition = e.GetPosition(GrayImageInteractionLayer);
            GrayImageInteractionLayer.CaptureMouse();
            GrayImageInteractionLayer.Cursor = WpfCursors.Cross;
            UpdateRoiSelectionOverlay(_roiSelectionStartPosition, _roiSelectionStartPosition);
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isRoiSelecting)
            {
                WpfPoint endPosition = e.GetPosition(GrayImageInteractionLayer);
                _isRoiSelecting = false;
                GrayImageInteractionLayer.ReleaseMouseCapture();
                GrayImageInteractionLayer.Cursor = WpfCursors.Arrow;

                if (TryMapViewportToImage(_roiSelectionStartPosition, out double startColumn, out double startRow) &&
                    TryMapViewportToImage(endPosition, out double endColumn, out double endRow) &&
                    GrayImageViewer.Source is BitmapSource bitmap)
                {
                    CalibrationPlateImageRoi normalizedRoi = CalibrationPlateRoiViewportMapper.NormalizeRoi(
                        startRow,
                        startColumn,
                        endRow,
                        endColumn,
                        bitmap.PixelWidth,
                        bitmap.PixelHeight,
                        minSize: 2);

                    ViewModel?.ModelParam.SetSelectedRoi(
                        normalizedRoi.Row1,
                        normalizedRoi.Column1,
                        normalizedRoi.Row2,
                        normalizedRoi.Column2);
                    UpdateSelectedRoiOverlayFromModel();
                }

                e.Handled = true;
                return;
            }

            _isGrayImageDragging = false;
            GrayImageInteractionLayer.ReleaseMouseCapture();
            GrayImageInteractionLayer.Cursor = WpfCursors.Arrow;
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            BeginGrayImagePan(e.GetPosition(GrayImageInteractionLayer));
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndGrayImagePan();
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
            {
                return;
            }

            BeginGrayImagePan(e.GetPosition(GrayImageInteractionLayer));
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
            {
                return;
            }

            EndGrayImagePan();
            e.Handled = true;
        }

        private void GrayImageInteractionLayer_MouseMove(object sender, WpfMouseEventArgs e)
        {
            WpfPoint currentPosition = e.GetPosition(GrayImageInteractionLayer);
            if (_isRoiSelecting)
            {
                UpdateRoiSelectionOverlay(_roiSelectionStartPosition, currentPosition);
                e.Handled = true;
                return;
            }

            if (_isGrayImageDragging)
            {
                Vector offset = currentPosition - _lastGrayImageMousePosition;
                GrayImageTranslateTransform.X += offset.X;
                GrayImageTranslateTransform.Y += offset.Y;
                _lastGrayImageMousePosition = currentPosition;
                UpdateSelectedRoiOverlayFromModel();
                e.Handled = true;
                return;
            }

            UpdateCursorCoordinate(currentPosition);
        }

        private void GrayImageInteractionLayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSelectedRoiOverlayFromModel();
        }

        private void BeginGrayImagePan(WpfPoint position)
        {
            if (GrayImageViewer.Source == null)
            {
                return;
            }

            _isGrayImageDragging = true;
            _lastGrayImageMousePosition = position;
            GrayImageInteractionLayer.CaptureMouse();
            GrayImageInteractionLayer.Cursor = WpfCursors.SizeAll;
        }

        private void EndGrayImagePan()
        {
            _isGrayImageDragging = false;
            if (!_isRoiSelecting)
            {
                GrayImageInteractionLayer.ReleaseMouseCapture();
                GrayImageInteractionLayer.Cursor = WpfCursors.Arrow;
            }
        }

        private void UpdateRoiSelectionOverlay(WpfPoint startPosition, WpfPoint endPosition)
        {
            if (!TryMapViewportToImage(startPosition, out double startColumn, out double startRow) ||
                !TryMapViewportToImage(endPosition, out double endColumn, out double endRow) ||
                GrayImageViewer.Source is not BitmapSource bitmap)
            {
                SelectedRoiOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            CalibrationPlateImageRoi normalizedRoi = CalibrationPlateRoiViewportMapper.NormalizeRoi(
                startRow,
                startColumn,
                endRow,
                endColumn,
                bitmap.PixelWidth,
                bitmap.PixelHeight,
                minSize: 2);
            UpdateRoiOverlayByImageRoi(normalizedRoi);
        }

        private void UpdateCursorCoordinate(WpfPoint viewportPoint)
        {
            if (ViewModel?.CenterDisplayMode != CalibrationPlateCenterDisplayMode.GrayImage
                || GrayImageViewer.Source is not BitmapSource bitmap
                || GrayImageInteractionLayer.ActualWidth <= 0
                || GrayImageInteractionLayer.ActualHeight <= 0)
            {
                return;
            }

            if (TryMapViewportToImage(viewportPoint, out double imageColumn, out double imageRow))
            {
                ViewModel.ModelParam.UpdateCursorCoordinateText(imageColumn, imageRow);
            }
        }

        private bool TryMapViewportToImage(WpfPoint viewportPoint, out double imageColumn, out double imageRow)
        {
            // 鼠标点先从透明交互层转换到 Image 控件，再扣除 Uniform 留白和缩放。
            imageColumn = 0;
            imageRow = 0;

            if (GrayImageViewer.Source is not BitmapSource bitmap ||
                GrayImageViewer.ActualWidth <= 0 ||
                GrayImageViewer.ActualHeight <= 0)
            {
                return false;
            }

            WpfPoint imageControlPoint = GrayImageInteractionLayer.TranslatePoint(viewportPoint, GrayImageViewer);

            if (!TryGetImageContentLayout(bitmap, out double contentLeft, out double contentTop, out double contentScale))
            {
                return false;
            }

            double naturalColumn = (imageControlPoint.X - contentLeft) / contentScale;
            double naturalRow = (imageControlPoint.Y - contentTop) / contentScale;
            imageColumn = naturalColumn * bitmap.PixelWidth / bitmap.Width;
            imageRow = naturalRow * bitmap.PixelHeight / bitmap.Height;
            return double.IsFinite(imageColumn) && double.IsFinite(imageRow);
        }

        private bool TryMapImageToViewport(double imageColumn, double imageRow, out WpfPoint viewportPoint)
        {
            // ROI 回显走反向映射，确保缩放和平移后边框仍贴合图像像素。
            viewportPoint = default;
            if (GrayImageViewer.Source is not BitmapSource bitmap ||
                GrayImageViewer.ActualWidth <= 0 ||
                GrayImageViewer.ActualHeight <= 0)
            {
                return false;
            }

            if (!TryGetImageContentLayout(bitmap, out double contentLeft, out double contentTop, out double contentScale))
            {
                return false;
            }

            double naturalColumn = imageColumn * bitmap.Width / bitmap.PixelWidth;
            double naturalRow = imageRow * bitmap.Height / bitmap.PixelHeight;
            WpfPoint imageControlPoint = new(
                contentLeft + naturalColumn * contentScale,
                contentTop + naturalRow * contentScale);
            viewportPoint = GrayImageViewer.TranslatePoint(imageControlPoint, GrayImageInteractionLayer);

            return double.IsFinite(viewportPoint.X) && double.IsFinite(viewportPoint.Y);
        }

        private bool TryGetImageContentLayout(
            BitmapSource bitmap,
            out double contentLeft,
            out double contentTop,
            out double contentScale)
        {
            contentLeft = 0;
            contentTop = 0;
            contentScale = 0;

            if (bitmap.Width <= 0 ||
                bitmap.Height <= 0 ||
                bitmap.PixelWidth <= 0 ||
                bitmap.PixelHeight <= 0 ||
                GrayImageViewer.ActualWidth <= 0 ||
                GrayImageViewer.ActualHeight <= 0)
            {
                return false;
            }

            contentScale = Math.Min(
                GrayImageViewer.ActualWidth / bitmap.Width,
                GrayImageViewer.ActualHeight / bitmap.Height);
            if (!double.IsFinite(contentScale) || contentScale <= 0)
            {
                return false;
            }

            contentLeft = (GrayImageViewer.ActualWidth - bitmap.Width * contentScale) * 0.5;
            contentTop = (GrayImageViewer.ActualHeight - bitmap.Height * contentScale) * 0.5;
            return true;
        }

        private void UpdateSelectedRoiOverlayFromModel()
        {
            if (_isRoiSelecting || GrayImageViewer.Source is not BitmapSource bitmap)
            {
                return;
            }

            if (ViewModel?.ModelParam?.HasSelectedRoi != true)
            {
                SelectedRoiOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            CalibrationPlateImageRoi normalizedRoi = CalibrationPlateRoiViewportMapper.NormalizeRoi(
                ViewModel.ModelParam.SelectedRoiRow1,
                ViewModel.ModelParam.SelectedRoiColumn1,
                ViewModel.ModelParam.SelectedRoiRow2,
                ViewModel.ModelParam.SelectedRoiColumn2,
                bitmap.PixelWidth,
                bitmap.PixelHeight,
                minSize: 2);
            UpdateRoiOverlayByImageRoi(normalizedRoi);
        }

        private void UpdateRoiOverlayByImageRoi(CalibrationPlateImageRoi roi)
        {
            if (!TryMapImageToViewport(roi.Column1, roi.Row1, out WpfPoint topLeft) ||
                !TryMapImageToViewport(roi.Column2, roi.Row2, out WpfPoint bottomRight))
            {
                SelectedRoiOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            Rect viewportRect = new(topLeft, bottomRight);
            SelectedRoiOverlay.Margin = new Thickness(viewportRect.Left, viewportRect.Top, 0, 0);
            SelectedRoiOverlay.Width = Math.Max(viewportRect.Width, 1);
            SelectedRoiOverlay.Height = Math.Max(viewportRect.Height, 1);
            SelectedRoiOverlay.Visibility = Visibility.Visible;
        }

        private delegate bool EnumChildWindowsCallback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hwndParent,
            EnumChildWindowsCallback callback,
            IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(
            IntPtr hwnd,
            IntPtr lprcUpdate,
            IntPtr hrgnUpdate,
            RedrawWindowFlags flags);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hwnd,
            WindowMessages msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr hwndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            SetWindowPosFlags flags);

        [Flags]
        private enum RedrawWindowFlags : uint
        {
            Invalidate = 0x0001,
            UpdateNow = 0x0100,
            AllChildren = 0x0080
        }

        private enum WindowMessages : uint
        {
            MouseMove = 0x0200
        }

        [Flags]
        private enum SetWindowPosFlags : uint
        {
            NoSize = 0x0001,
            NoMove = 0x0002,
            NoZOrder = 0x0004,
            NoActivate = 0x0010,
            ShowWindow = 0x0040,
            FrameChanged = 0x0020
        }
    }
}

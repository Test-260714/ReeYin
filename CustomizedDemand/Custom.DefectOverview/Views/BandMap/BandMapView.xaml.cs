using Custom.DefectOverview.ViewModels;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Custom.DefectOverview.Views
{
    public partial class BandMapView : UserControl
    {
        private const string BrjReportViewName = "BrjReportOutputView";
        private const string BrjReportSubjection = "FileTool.BRJReportOutput";

        private bool _isMiddlePanning;
        private Point _lastPanPoint;

        public BandMapView()
        {
            InitializeComponent();
        }

        private void OnMapViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not BandMapViewModel viewModel || !viewModel.CanScrollViewport)
                return;

            double delta = e.Delta > 0
                ? -viewModel.ViewportScrollSmallChange
                : viewModel.ViewportScrollSmallChange;

            viewModel.NudgeViewportScroll(delta);
            viewModel.CommitViewportStartNow();
            e.Handled = true;
        }

        private void OnMapViewportPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle || sender is not FrameworkElement element)
                return;

            _isMiddlePanning = true;
            _lastPanPoint = e.GetPosition(element);
            element.CaptureMouse();
            element.Cursor = Cursors.SizeNS;
            e.Handled = true;
        }

        private void OnMapViewportPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMiddlePanning || sender is not FrameworkElement element || DataContext is not BandMapViewModel viewModel)
                return;

            var currentPoint = e.GetPosition(element);
            var deltaY = currentPoint.Y - _lastPanPoint.Y;
            _lastPanPoint = currentPoint;

            if (!viewModel.CanScrollViewport || element.ActualHeight <= 1 || double.IsNaN(deltaY) || double.IsInfinity(deltaY))
                return;

            var scrollDelta = deltaY / element.ActualHeight * Math.Max(1.0, viewModel.WindowMeters);
            viewModel.ViewportScrollOffset += scrollDelta;
            e.Handled = true;
        }

        private void OnMapCanvasHostLoaded(object sender, RoutedEventArgs e)
        {
            SyncMapViewportSize(sender as FrameworkElement);
        }

        private void OnMapCanvasHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncMapViewportSize(sender as FrameworkElement);
        }

        private void OnLightningMapPointSelected(object sender, BandMapPointSelectedEventArgs e)
        {
            if (e?.Point == null || DataContext is not BandMapViewModel viewModel)
            {
                return;
            }

            var host = MapCanvasHost;
            if (host == null || host.ActualWidth <= 1 || host.ActualHeight <= 1)
                return;

            var position = e.Position;
            double popupWidth = e.GroupedPoints?.Count > 1 ? 320 : 210;
            double popupHeight = e.GroupedPoints?.Count > 1 ? 168 : 58;
            const double gapX = 18;
            const double gapY = 14;
            const double edgePadding = 8;

            double offsetX = position.X + gapX;
            double offsetY = position.Y + gapY;

            if (offsetX + popupWidth > host.ActualWidth - edgePadding)
                offsetX = Math.Max(edgePadding, position.X - popupWidth - 10);

            if (offsetY + popupHeight > host.ActualHeight - edgePadding)
                offsetY = Math.Max(edgePadding, host.ActualHeight - popupHeight - edgePadding);

            viewModel.CommitViewportStartNow();
            viewModel.ShowSelectedMapPointPopup(e.Point, e.GroupedPoints, offsetX, offsetY);
            if (viewModel.SelectPointCommand.CanExecute(e.Point))
                viewModel.SelectPointCommand.Execute(e.Point);
        }

        private void OnMapViewportPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            if (DataContext is BandMapViewModel viewModel)
                viewModel.CommitViewportStartNow();

            EndMiddlePan(sender as FrameworkElement);
            e.Handled = true;
        }

        private void OnMapViewportLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isMiddlePanning && DataContext is BandMapViewModel viewModel)
                viewModel.CommitViewportStartNow();

            EndMiddlePan(sender as FrameworkElement);
        }

        private void OnSlittingPopupActionClick(object sender, RoutedEventArgs e)
        {
            if (SlittingPopupToggleButton != null)
            {
                SlittingPopupToggleButton.IsChecked = false;
            }
        }

        private void OnOpenBrjReportPageClick(object sender, RoutedEventArgs e)
        {
            if (!IsBrjReportDynamicViewRegistered())
            {
                MessageBox.Show("BRJ报表动态页面未注册，请确认 FileTool.BRJReportOutput 模块已加载。", "记录", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var request = new DynamicRegionViewFocusRequest
            {
                Type = DynamicViewType.Custom,
                ViewName = BrjReportViewName,
                DisplayName = "BRJ报表输出",
                Subjection = BrjReportSubjection,
                Completion = (success, message) =>
                {
                    if (success)
                        return;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(
                            string.IsNullOrWhiteSpace(message) ? "未能切换到BRJ报表动态页面。" : message,
                            "记录",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }));
                }
            };

            PrismProvider.EventAggregator?
                .GetEvent<DynamicRegionViewFocusEvent>()
                .Publish(request);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!request.IsCompleted)
                {
                    MessageBox.Show("动态窗口未响应页面切换请求，请确认主动态窗口已加载。", "记录", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }));
        }

        private void EndMiddlePan(FrameworkElement element)
        {
            _isMiddlePanning = false;
            if (element == null)
                return;

            element.ReleaseMouseCapture();
            element.ClearValue(CursorProperty);
        }

        private void SyncMapViewportSize(FrameworkElement element)
        {
            if (element == null || DataContext is not BandMapViewModel viewModel)
                return;

            if (element.ActualWidth <= 1 || element.ActualHeight <= 1)
                return;

            viewModel.UpdateViewportSize(element.ActualWidth, element.ActualHeight);
        }

        private static bool IsBrjReportDynamicViewRegistered()
        {
            return PrismProvider.DynamicViewManager?.DynamicViews?.Any(view =>
                view.Type == DynamicViewType.Custom
                && string.Equals(view.ViewName, BrjReportViewName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(view.Subjection ?? string.Empty, BrjReportSubjection, StringComparison.OrdinalIgnoreCase)) == true;
        }

    }
}

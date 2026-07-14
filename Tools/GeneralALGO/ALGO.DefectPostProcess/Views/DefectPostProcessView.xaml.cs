using ALGO.DefectPostProcess.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ALGO.DefectPostProcess.Views
{
    /// <summary>
    /// 实现缺陷后处理主界面视图。
    /// </summary>
    public partial class DefectPostProcessView : UserControl
    {
        private bool _isDraggingCustomAlgorithmPopup;
        private Point _customAlgorithmPopupDragStartPoint;
        private double _customAlgorithmPopupStartHorizontalOffset;
        private double _customAlgorithmPopupStartVerticalOffset;

        /// <summary>
        /// 初始化缺陷后处理主界面视图。
        /// </summary>
        public DefectPostProcessView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (DataContext is DefectPostProcessViewModel viewModel)
                {
                    viewModel.ModelParam.RefreshPreview();
                }
            }), DispatcherPriority.Loaded);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        // Popup 默认不支持拖动，通过偏移量实现定制算法窗口的自由移动。
        private void CustomAlgorithmPopupRoot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CustomAlgorithmPopup?.IsOpen != true
                || IsCustomAlgorithmPopupDragIgnored(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _isDraggingCustomAlgorithmPopup = true;
            _customAlgorithmPopupDragStartPoint = e.GetPosition(this);
            _customAlgorithmPopupStartHorizontalOffset = CustomAlgorithmPopup.HorizontalOffset;
            _customAlgorithmPopupStartVerticalOffset = CustomAlgorithmPopup.VerticalOffset;

            if (sender is UIElement element)
            {
                element.CaptureMouse();
            }

            e.Handled = true;
        }

        private void CustomAlgorithmPopupRoot_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingCustomAlgorithmPopup || CustomAlgorithmPopup?.IsOpen != true)
            {
                return;
            }

            Point currentPoint = e.GetPosition(this);
            CustomAlgorithmPopup.HorizontalOffset = _customAlgorithmPopupStartHorizontalOffset
                + currentPoint.X - _customAlgorithmPopupDragStartPoint.X;
            CustomAlgorithmPopup.VerticalOffset = _customAlgorithmPopupStartVerticalOffset
                + currentPoint.Y - _customAlgorithmPopupDragStartPoint.Y;
            e.Handled = true;
        }

        private void CustomAlgorithmPopupRoot_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopCustomAlgorithmPopupDrag(sender as UIElement);
        }

        private void CustomAlgorithmPopupRoot_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopCustomAlgorithmPopupDrag(sender as UIElement);
        }

        private void StopCustomAlgorithmPopupDrag(UIElement capturedElement)
        {
            if (!_isDraggingCustomAlgorithmPopup)
            {
                return;
            }

            _isDraggingCustomAlgorithmPopup = false;
            if (capturedElement?.IsMouseCaptured == true)
            {
                capturedElement.ReleaseMouseCapture();
            }
        }

        private bool IsCustomAlgorithmPopupDragIgnored(DependencyObject source)
        {
            while (source != null && !ReferenceEquals(source, CustomAlgorithmPopupRoot))
            {
                if (source is ButtonBase
                    || source is TextBoxBase
                    || source is Selector
                    || source is RangeBase
                    || source is ScrollBar
                    || source is ScrollViewer
                    || source is PasswordBox
                    || source is Hyperlink)
                {
                    return true;
                }

                source = GetDependencyParent(source);
            }

            return false;
        }

        private static DependencyObject GetDependencyParent(DependencyObject source)
        {
            return source is FrameworkContentElement contentElement
                ? contentElement.Parent
                : VisualTreeHelper.GetParent(source);
        }
    }
}

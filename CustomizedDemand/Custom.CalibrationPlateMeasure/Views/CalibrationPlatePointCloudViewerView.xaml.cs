using Custom.CalibrationPlateMeasure.ViewModels;
using PointCloud.VTKWPF.Models;
using System.Windows;
using System.Windows.Threading;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace Custom.CalibrationPlateMeasure.Views
{
    // 本项目专用点云视图包装层，隔离对原始 PointCloud 视图的布局和刷新定制。
    public partial class CalibrationPlatePointCloudViewerView : WpfUserControl
    {
        private CalibrationPlatePointCloudViewModel? _viewModel;
        private bool _hasAppliedScene;

        public CalibrationPlatePointCloudViewerView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
            Viewport.PointPicked += OnViewportPointPicked;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null && DataContext is CalibrationPlatePointCloudViewModel viewModel)
            {
                AttachToViewModel(viewModel);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachFromViewModel();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && _viewModel is { HasLoadedCloud: true })
            {
                ForceRender();
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.OldValue, e.NewValue))
            {
                return;
            }

            DetachFromViewModel();
            if (e.NewValue is CalibrationPlatePointCloudViewModel viewModel)
            {
                AttachToViewModel(viewModel);
            }
        }

        private void AttachToViewModel(CalibrationPlatePointCloudViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.SceneChanged += OnSceneChanged;
            _viewModel.RenderOptionsChanged += OnRenderOptionsChanged;
            _viewModel.ViewOrientationRequested += OnViewOrientationRequested;

            ApplyScene(resetCamera: false);
        }

        private void DetachFromViewModel()
        {
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.SceneChanged -= OnSceneChanged;
            _viewModel.RenderOptionsChanged -= OnRenderOptionsChanged;
            _viewModel.ViewOrientationRequested -= OnViewOrientationRequested;
            _viewModel = null;
        }

        private void OnSceneChanged(object? sender, CalibrationPlatePointCloudSceneChangedEventArgs e)
        {
            ApplyScene(e.ResetCamera);
        }

        private void OnRenderOptionsChanged(object? sender, EventArgs e)
        {
            if (_viewModel == null || !_viewModel.HasLoadedCloud)
            {
                return;
            }

            _viewModel.BeginRendering();
            Viewport.RenderOptions = _viewModel.BuildRenderOptions();
            Viewport.Render();
            CompleteRenderOnNextFrame();
        }

        private void OnViewOrientationRequested(object? sender, CalibrationPlatePointCloudViewOrientationRequestedEventArgs e)
        {
            _viewModel?.BeginRendering();
            Viewport.SetStandardView(e.Orientation);
            CompleteRenderOnNextFrame();
        }

        private void OnViewportPointPicked(object? sender, PointPickedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            if (_viewModel.IsPivotPickingEnabled)
            {
                Viewport.SetCameraFocalPoint(e.Point);
            }

            _viewModel.HandlePointPicked(e.Point);
        }

        private void ApplyScene(bool resetCamera)
        {
            // 只在已有点云数据时刷新 VTK 场景，避免切换灰度图时误清理缓存。
            if (_viewModel == null || !_viewModel.HasLoadedCloud || _viewModel.DisplayCloud == null)
            {
                Viewport.ClearScene();
                _hasAppliedScene = false;
                return;
            }

            _viewModel.BeginApplyingScene();
            Viewport.SetPointCloud(_viewModel.DisplayCloud, _viewModel.BuildRenderOptions());
            _hasAppliedScene = true;
            if (resetCamera)
            {
                Viewport.ResetCamera();
            }

            _viewModel.BeginRendering();
            Viewport.Render();
            CompleteRenderOnNextFrame();
        }

        public void ForceRender()
        {
            // VTK 子窗口第一次显示可能晚于 WPF 布局，此处强制补一帧。
            if (_viewModel == null || !_viewModel.HasLoadedCloud || _viewModel.DisplayCloud == null)
            {
                return;
            }

            if (!_hasAppliedScene)
            {
                ApplyScene(resetCamera: false);
                return;
            }

            UpdateLayout();
            Viewport.UpdateLayout();
            Viewport.InvalidateVisual();
            _viewModel.BeginRendering();
            Viewport.Render();
            CompleteRenderOnNextFrame();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel == null || !_viewModel.HasLoadedCloud || _viewModel.DisplayCloud == null)
                {
                    return;
                }

                UpdateLayout();
                Viewport.UpdateLayout();
                Viewport.InvalidateVisual();
                _viewModel.BeginRendering();
                Viewport.Render();
                CompleteRenderOnNextFrame();
            }), DispatcherPriority.Render);
        }

        private void CompleteRenderOnNextFrame()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel == null || !_viewModel.HasLoadedCloud || _viewModel.DisplayCloud == null)
                {
                    return;
                }

                _viewModel.CompleteRendering();
            }), DispatcherPriority.Render);
        }
    }
}

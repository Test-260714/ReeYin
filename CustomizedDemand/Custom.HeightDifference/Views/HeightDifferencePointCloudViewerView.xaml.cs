using ReeYin.Customized.Algo.ViewModels;
using PointCloud.VTKWPF.Models;
using System.Windows;
using System.Windows.Threading;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace ReeYin.Customized.Algo.Views
{
    // 本项目专用点云视图包装层，隔离对原始 PointCloud 视图的布局和刷新定制。
    public partial class HeightDifferencePointCloudViewerView : WpfUserControl
    {
        // 点云显示控件当前绑定的视图模型。
        private HeightDifferencePointCloudViewModel? _viewModel;
        // 记录点云场景是否已经提交到原生视口。
        private bool _hasAppliedScene;

        /// <summary>
        /// 初始化点云显示控件并注册视口事件。
        /// </summary>
        public HeightDifferencePointCloudViewerView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
            Viewport.PointPicked += OnViewportPointPicked;
        }

        /// <summary>
        /// 点云视图加载后绑定视图模型并提交当前场景。
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null && DataContext is HeightDifferencePointCloudViewModel viewModel)
            {
                AttachToViewModel(viewModel);
            }
        }

        /// <summary>
        /// 点云视图卸载时解绑视图模型并释放事件订阅。
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachFromViewModel();
        }

        /// <summary>
        /// 点云视图重新显示时强制刷新原生视口。
        /// </summary>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && _viewModel is { HasLoadedCloud: true })
            {
                ForceRender();
            }
        }

        /// <summary>
        /// 数据上下文变化时切换点云视图模型绑定。
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.OldValue, e.NewValue))
            {
                return;
            }

            DetachFromViewModel();
            if (e.NewValue is HeightDifferencePointCloudViewModel viewModel)
            {
                AttachToViewModel(viewModel);
            }
        }

        /// <summary>
        /// 订阅点云视图模型事件并应用当前场景。
        /// </summary>
        private void AttachToViewModel(HeightDifferencePointCloudViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.SceneChanged += OnSceneChanged;
            _viewModel.RenderOptionsChanged += OnRenderOptionsChanged;
            _viewModel.ResetCameraRequested += OnResetCameraRequested;
            _viewModel.ViewOrientationRequested += OnViewOrientationRequested;

            ApplyScene(resetCamera: false);
        }

        /// <summary>
        /// 取消点云视图模型事件订阅并清空引用。
        /// </summary>
        private void DetachFromViewModel()
        {
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.SceneChanged -= OnSceneChanged;
            _viewModel.RenderOptionsChanged -= OnRenderOptionsChanged;
            _viewModel.ResetCameraRequested -= OnResetCameraRequested;
            _viewModel.ViewOrientationRequested -= OnViewOrientationRequested;
            _viewModel = null;
        }

        /// <summary>
        /// 点云场景变化时把新场景提交到原生视口。
        /// </summary>
        private void OnSceneChanged(object? sender, HeightDifferencePointCloudSceneChangedEventArgs e)
        {
            ApplyScene(e.ResetCamera);
        }

        /// <summary>
        /// 点云渲染参数变化时刷新场景或命令状态。
        /// </summary>
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

        /// <summary>
        /// 根据视图模型请求切换点云标准视角。
        /// </summary>
        private void OnViewOrientationRequested(object? sender, HeightDifferencePointCloudViewOrientationRequestedEventArgs e)
        {
            _viewModel?.BeginRendering();
            Viewport.SetStandardView(e.Orientation);
            CompleteRenderOnNextFrame();
        }

        /// <summary>
        /// 根据视图模型请求复位点云相机。
        /// </summary>
        private void OnResetCameraRequested(object? sender, EventArgs e)
        {
            if (_viewModel == null || !_viewModel.HasLoadedCloud)
            {
                return;
            }

            _viewModel.BeginRendering();
            Viewport.ResetCamera();
            Viewport.Render();
            CompleteRenderOnNextFrame();
        }

        /// <summary>
        /// 把原生视口拾取的点坐标回传给视图模型。
        /// </summary>
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

        /// <summary>
        /// 把当前点云场景和渲染选项应用到原生视口。
        /// </summary>
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

        /// <summary>
        /// 请求点云原生视口立即重绘。
        /// </summary>
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

        /// <summary>
        /// 在下一帧渲染完成后通知视图模型结束忙碌状态。
        /// </summary>
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

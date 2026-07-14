using System.Windows;
using System.Windows.Controls;
using PointCloud.ToolViewer.ViewModels;
using PointCloud.VTKWPF.Models;

namespace PointCloud.ToolViewer.Views;

public partial class PointCloudViewerView : UserControl
{
    private PointCloudViewerViewModel? _viewModel;

    public PointCloudViewerView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Viewport.PointPicked += OnViewportPointPicked;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null && DataContext is PointCloudViewerViewModel viewModel)
        {
            AttachToViewModel(viewModel);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachFromViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.OldValue, e.NewValue))
        {
            return;
        }

        DetachFromViewModel();

        if (e.NewValue is PointCloudViewerViewModel viewModel)
        {
            AttachToViewModel(viewModel);
        }
    }

    private void AttachToViewModel(PointCloudViewerViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.SceneChanged += OnSceneChanged;
        _viewModel.RenderOptionsChanged += OnRenderOptionsChanged;
        _viewModel.ResetCameraRequested += OnResetCameraRequested;
        _viewModel.ViewOrientationRequested += OnViewOrientationRequested;
        _viewModel.ApplyMeasurementOverlayRequested += OnApplyMeasurementOverlayRequested;

        ApplyScene(resetCamera: false);
    }

    private void DetachFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.SceneChanged -= OnSceneChanged;
        _viewModel.RenderOptionsChanged -= OnRenderOptionsChanged;
        _viewModel.ResetCameraRequested -= OnResetCameraRequested;
        _viewModel.ViewOrientationRequested -= OnViewOrientationRequested;
        _viewModel.ApplyMeasurementOverlayRequested -= OnApplyMeasurementOverlayRequested;
        _viewModel = null;
    }

    private void OnSceneChanged(object? sender, PointCloudSceneChangedEventArgs e)
    {
        ApplyScene(e.ResetCamera);
    }

    private void OnRenderOptionsChanged(object? sender, EventArgs e)
    {
        if (_viewModel is null || !_viewModel.HasLoadedCloud)
        {
            return;
        }

        Viewport.RenderOptions = _viewModel.BuildRenderOptions();
        Viewport.Render();
    }

    private void OnResetCameraRequested(object? sender, EventArgs e)
    {
        Viewport.ResetCamera();
    }

    private void OnViewOrientationRequested(object? sender, PointCloudViewOrientationRequestedEventArgs e)
    {
        Viewport.SetStandardView(e.Orientation);
    }

    private void OnApplyMeasurementOverlayRequested(object? sender, PointPickingMeasurementOverlayEventArgs e)
    {
        if (e.Overlay.HasAny)
        {
            Viewport.SetMeasurementOverlay(e.Overlay);
            return;
        }

        Viewport.ClearMeasurementOverlay();
    }

    private void OnViewportPointPicked(object? sender, PointPickedEventArgs e)
    {
        if (_viewModel is null)
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
        if (_viewModel is null || !_viewModel.HasLoadedCloud || _viewModel.DisplayCloud is null)
        {
            Viewport.ClearMeasurementOverlay();
            Viewport.ClearScene();
            return;
        }

        Viewport.SetPointCloud(_viewModel.DisplayCloud, _viewModel.BuildRenderOptions());
        if (resetCamera)
        {
            Viewport.ResetCamera();
        }
    }
}

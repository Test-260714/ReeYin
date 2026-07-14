using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PointCloud.Interop;
using PointCloud.VTKWPF.Models;

namespace PointCloud.VTKWPF.Controls;

public sealed class PointCloudViewportControl : UserControl
{
    public static readonly DependencyProperty OverlayTextProperty =
        DependencyProperty.Register(
            nameof(OverlayText),
            typeof(string),
            typeof(PointCloudViewportControl),
            new PropertyMetadata(string.Empty, OnOverlayTextChanged));

    private readonly VtkRenderHost _renderHost;
    private readonly Popup _overlayPopup;
    private readonly TextBlock _overlayTextBlock;
    private PointCloudRenderOptions _renderOptions = new();

    public PointCloudViewportControl()
    {
        _renderHost = new VtkRenderHost();
        Content = _renderHost;

        _overlayTextBlock = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0),
        };

        _overlayPopup = new Popup
        {
            PlacementTarget = this,
            Placement = PlacementMode.RelativePoint,
            HorizontalOffset = 12,
            VerticalOffset = 12,
            AllowsTransparency = true,
            StaysOpen = true,
            IsHitTestVisible = false,
            Child = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(210, 11, 18, 32)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Child = _overlayTextBlock,
            },
        };

        Loaded += (_, _) => UpdateOverlayPopup();
        Unloaded += (_, _) => _overlayPopup.IsOpen = false;
    }

    public event EventHandler<PointPickedEventArgs>? PointPicked
    {
        add => _renderHost.PointPicked += value;
        remove => _renderHost.PointPicked -= value;
    }

    public string OverlayText
    {
        get => (string)GetValue(OverlayTextProperty);
        set => SetValue(OverlayTextProperty, value);
    }

    public PointCloudRenderOptions RenderOptions
    {
        get => _renderOptions;
        set
        {
            _renderOptions = value ?? new PointCloudRenderOptions();
            _renderHost.ApplyOptions(_renderOptions);
        }
    }

    public void SetPointCloud(PointCloudHandle pointCloud)
    {
        _renderHost.LoadPointCloud(pointCloud, RenderOptions);
    }

    public void SetPointCloud(PointCloudHandle pointCloud, PointCloudRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(pointCloud);

        _renderOptions = options ?? new PointCloudRenderOptions();
        _renderHost.LoadPointCloud(pointCloud, _renderOptions);
    }

    public void SetPointCloud(PointCloudResourceRef pointCloudRef)
    {
        ArgumentNullException.ThrowIfNull(pointCloudRef);
        SetPointCloud(PointCloudResourceRegistry.GetCloud(pointCloudRef));
    }

    public void SetPointCloud(PointCloudResourceRef pointCloudRef, PointCloudRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(pointCloudRef);
        SetPointCloud(PointCloudResourceRegistry.GetCloud(pointCloudRef), options);
    }

    public void ClearScene()
    {
        _renderHost.ClearPointCloud();
    }

    public void ResetCamera()
    {
        _renderHost.ResetCamera();
    }

    public void SetStandardView(PointCloudViewOrientation orientation)
    {
        _renderHost.SetStandardView(orientation);
    }

    public void SetCameraFocalPoint(Point3d point, bool render = true)
    {
        _renderHost.SetCameraFocalPoint(point, render);
    }

    public void SetMeasurementOverlay(PointPickingMeasurementOverlay overlay)
    {
        _renderHost.SetMeasurementOverlay(overlay);
    }

    public void ClearMeasurementOverlay()
    {
        _renderHost.ClearMeasurementOverlay();
    }

    public void Render()
    {
        _renderHost.Render();
    }

    private static void OnOverlayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PointCloudViewportControl)d).UpdateOverlayPopup();
    }

    private void UpdateOverlayPopup()
    {
        _overlayTextBlock.Text = OverlayText ?? string.Empty;
        _overlayPopup.IsOpen = IsLoaded && !string.IsNullOrWhiteSpace(_overlayTextBlock.Text);
    }
}

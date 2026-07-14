using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PointCloud.Interop;
using PointCloud.VTKWPF.Models;
using PointCloud.VTKWPF.Native;

namespace PointCloud.VTKWPF.Controls;

internal sealed class VtkRenderHost : HwndHost
{
    private const int PivotSegmentCount = 128;
    private const int PivotActorCount = 8;
    private const int PivotXRingIndex = 0;
    private const int PivotYRingIndex = 1;
    private const int PivotZRingIndex = 2;
    private const int PivotXAxisIndex = 3;
    private const int PivotYAxisIndex = 4;
    private const int PivotZAxisIndex = 5;
    private const int PivotCenterOutlineIndex = 6;
    private const int PivotCenterFillIndex = 7;
    private const double PivotRadiusScaleFactor = 0.55;
    private const double PivotMinimumRadius = 0.001;
    private const float PivotRingLineWidth = 1.4f;
    private const float PivotAxisLineWidth = 1.6f;
    private const double PivotCenterOutlineRadius = 0.024;
    private const double PivotCenterFillRadius = 0.016;
    private const int MeasurementMarkerCount = 3;
    private const float MeasurementLineWidth = 2.4f;
    private IntPtr _hwnd;
    private IntPtr _renderWindow;
    private IntPtr _renderer;
    private IntPtr _overlayRenderer;
    private IntPtr _hudRenderer;
    private IntPtr _interactor;
    private IntPtr _interactorStyle;
    private IntPtr _orientationWidget;
    private IntPtr _orientationAxesMarker;
    private IntPtr _renderStepsPass;
    private IntPtr _edlPass;

    private IntPtr _points;
    private IntPtr _polyData;
    private IntPtr _scalars;
    private IntPtr _glyphFilter;
    private IntPtr _lookupTable;
    private IntPtr _mapper;
    private IntPtr _actor;
    private IntPtr _secondaryPoints;
    private IntPtr _secondaryPolyData;
    private IntPtr _secondaryGlyphFilter;
    private IntPtr _secondaryMapper;
    private IntPtr _secondaryActor;
    private IntPtr _scalarBarActor;

    private PointCloudHandle? _pendingPointCloud;
    private PointCloudRenderOptions _options = new();
    private PointCloudBounds _sceneBounds;
    private bool _hasSceneBounds;
    private Point3d _defaultCameraPosition;
    private Point3d _defaultCameraFocalPoint;
    private Point3d _defaultCameraViewUp;
    private double _defaultCameraDistance;
    private bool _hasDefaultCameraState;
    private int _leftButtonDownX;
    private int _leftButtonDownY;
    private bool _isLeftButtonDown;
    private bool _leftButtonDragDetected;
    private readonly IntPtr[] _pivotPoints = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _pivotLines = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _pivotPolyData = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _pivotSphereSources = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _pivotMappers = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _pivotActors = new IntPtr[PivotActorCount];
    private readonly IntPtr[] _measurementMarkerPoints = new IntPtr[MeasurementMarkerCount];
    private readonly IntPtr[] _measurementMarkerCells = new IntPtr[MeasurementMarkerCount];
    private readonly IntPtr[] _measurementMarkerPolyData = new IntPtr[MeasurementMarkerCount];
    private readonly IntPtr[] _measurementMarkerMappers = new IntPtr[MeasurementMarkerCount];
    private readonly IntPtr[] _measurementMarkerActors = new IntPtr[MeasurementMarkerCount];
    private IntPtr _measurementLinePoints;
    private IntPtr _measurementLineCells;
    private IntPtr _measurementLinePolyData;
    private IntPtr _measurementLineMapper;
    private IntPtr _measurementLineActor;
    private IntPtr _measurementTrianglePoints;
    private IntPtr _measurementTriangleCells;
    private IntPtr _measurementTrianglePolyData;
    private IntPtr _measurementTriangleMapper;
    private IntPtr _measurementTriangleActor;
    private bool _isPivotVisible;
    private double _pivotBaseScale = 1.0;
    private double _pivotBaseCameraDistance;

    public event EventHandler<PointPickedEventArgs>? PointPicked;

    public void LoadPointCloud(PointCloudHandle pointCloud, PointCloudRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pointCloud);

        _pendingPointCloud = pointCloud;
        _options = options ?? _options;

        if (_renderWindow != IntPtr.Zero)
        {
            LoadPendingPointCloud(resetCamera: true, captureDefaultCameraState: true);
            RequestDeferredRender();
        }
    }

    public void ApplyOptions(PointCloudRenderOptions options)
    {
        PointCloudRenderOptions newOptions = options ?? new PointCloudRenderOptions();
        bool requiresPipelineRebuild = RequiresPointCloudPipelineRebuild(_options, newOptions);
        _options = newOptions;

        if (_renderWindow == IntPtr.Zero)
        {
            return;
        }

        if (requiresPipelineRebuild && _pendingPointCloud is not null)
        {
            RebuildPointCloudPipelinePreservingView();
            return;
        }

        ApplySceneOptions();
        Render();
    }

    public void ClearPointCloud()
    {
        _pendingPointCloud = null;
        DestroyPointCloudObjects();
        DestroyMeasurementOverlay();
        ClearDefaultCameraState();
        SetPivotVisibility(false);
        ClearPointCloudRenderLayers();

        Render();
    }

    public void ResetCamera()
    {
        if (_renderer == IntPtr.Zero)
        {
            return;
        }

        if (_hasDefaultCameraState)
        {
            VtkNativeMethods.Renderer_SetActiveCameraPosition(
                _renderer,
                _defaultCameraPosition.X,
                _defaultCameraPosition.Y,
                _defaultCameraPosition.Z);
            VtkNativeMethods.Renderer_SetActiveCameraFocalPoint(
                _renderer,
                _defaultCameraFocalPoint.X,
                _defaultCameraFocalPoint.Y,
                _defaultCameraFocalPoint.Z);
            VtkNativeMethods.Renderer_SetActiveCameraViewUp(
                _renderer,
                _defaultCameraViewUp.X,
                _defaultCameraViewUp.Y,
                _defaultCameraViewUp.Z);
        }

        VtkNativeMethods.Renderer_ResetCamera(_renderer);
        VtkNativeMethods.Renderer_ResetCameraClippingRange(_renderer);
        SyncPivotToCameraFocalPoint();
        RefreshPivotScaleForCamera();
        SetPivotVisibility(false);
        Render();
    }

    public void SetStandardView(PointCloudViewOrientation orientation)
    {
        if (_renderer == IntPtr.Zero || !_hasSceneBounds)
        {
            return;
        }

        Point3d focalPoint = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraFocalPoint);
        Point3d cameraPosition = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraPosition);
        double distance = GetDistance(cameraPosition, focalPoint);
        if (!IsValidCameraDistance(distance))
        {
            distance = GetFallbackCameraDistance();
        }

        (Point3d direction, Point3d viewUp) = orientation switch
        {
            PointCloudViewOrientation.Front => (new Point3d(0.0, 1.0, 0.0), new Point3d(0.0, 0.0, 1.0)),
            PointCloudViewOrientation.Back => (new Point3d(0.0, -1.0, 0.0), new Point3d(0.0, 0.0, 1.0)),
            PointCloudViewOrientation.Left => (new Point3d(-1.0, 0.0, 0.0), new Point3d(0.0, 0.0, 1.0)),
            PointCloudViewOrientation.Right => (new Point3d(1.0, 0.0, 0.0), new Point3d(0.0, 0.0, 1.0)),
            PointCloudViewOrientation.Top => (new Point3d(0.0, 0.0, 1.0), new Point3d(0.0, 1.0, 0.0)),
            PointCloudViewOrientation.Bottom => (new Point3d(0.0, 0.0, -1.0), new Point3d(0.0, 1.0, 0.0)),
            _ => (new Point3d(0.0, 1.0, 0.0), new Point3d(0.0, 0.0, 1.0)),
        };

        Point3d targetPosition = new(
            focalPoint.X + direction.X * distance,
            focalPoint.Y + direction.Y * distance,
            focalPoint.Z + direction.Z * distance);

        VtkNativeMethods.Renderer_SetActiveCameraFocalPoint(_renderer, focalPoint.X, focalPoint.Y, focalPoint.Z);
        VtkNativeMethods.Renderer_SetActiveCameraPosition(_renderer, targetPosition.X, targetPosition.Y, targetPosition.Z);
        VtkNativeMethods.Renderer_SetActiveCameraViewUp(_renderer, viewUp.X, viewUp.Y, viewUp.Z);
        VtkNativeMethods.Renderer_ResetCameraClippingRange(_renderer);
        SyncPivotToCameraFocalPoint();
        RefreshPivotScaleForCamera();
        SetPivotVisibility(false);
        Render();
    }

    public void SetCameraFocalPoint(Point3d point, bool render = true)
    {
        if (_renderer == IntPtr.Zero)
        {
            return;
        }

        VtkNativeMethods.Renderer_SetActiveCameraFocalPoint(_renderer, point.X, point.Y, point.Z);
        VtkNativeMethods.Renderer_ResetCameraClippingRange(_renderer);
        SyncPivotToCameraFocalPoint();
        RefreshPivotScaleForCamera();
        if (render)
        {
            Render();
        }
    }

    public void SetMeasurementOverlay(PointPickingMeasurementOverlay overlay)
    {
        DestroyMeasurementOverlay();

        if (_overlayRenderer == IntPtr.Zero || overlay is null || !overlay.HasAny)
        {
            Render();
            return;
        }

        Point3d[] points = overlay.Points.Take(MeasurementMarkerCount).ToArray();
        for (int i = 0; i < points.Length; i++)
        {
            CreateMeasurementMarkerActor(i, points[i]);
        }

        if (points.Length >= 2)
        {
            CreateMeasurementLineActor(points);
        }

        if (points.Length >= 3)
        {
            CreateMeasurementTriangleActor(points);
        }

        Render();
    }

    public void ClearMeasurementOverlay()
    {
        DestroyMeasurementOverlay();
        Render();
    }

    public void Render()
    {
        if (_renderWindow != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindow_Render(_renderWindow);
        }
    }

    private void RequestDeferredRender()
    {
        if (_renderWindow == IntPtr.Zero)
        {
            return;
        }

        _ = Dispatcher.InvokeAsync(
            () =>
            {
                if (_renderWindow != IntPtr.Zero)
                {
                    Render();
                }
            },
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var width = Math.Max(1, (int)Math.Ceiling(ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(ActualHeight));
        string className = User32Native.EnsureVtkHostWindowClass();

        _hwnd = User32Native.CreateWindowEx(
            0,
            className,
            string.Empty,
            User32Native.WS_CHILD | User32Native.WS_VISIBLE | User32Native.WS_CLIPSIBLINGS | User32Native.WS_CLIPCHILDREN,
            0,
            0,
            width,
            height,
            hwndParent.Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法创建 VTK 宿主窗口。");
        }

        CreateScene();
        LoadPendingPointCloud(resetCamera: true, captureDefaultCameraState: true);
        RequestDeferredRender();
        return new HandleRef(this, _hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyScene();

        if (hwnd.Handle != IntPtr.Zero)
        {
            User32Native.DestroyWindow(hwnd.Handle);
        }

        _hwnd = IntPtr.Zero;
    }

    protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case User32Native.WM_ERASEBKGND:
                handled = true;
                return new IntPtr(1);
            case User32Native.WM_PAINT:
                HandlePaint(hwnd);
                handled = true;
                break;
            case User32Native.WM_MOUSEMOVE:
                HandleMouseMove(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_LBUTTONDOWN:
                HandleLeftButtonDown(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_SIZE:
                HandleSize(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_LBUTTONUP:
                HandleLeftButtonUp(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_RBUTTONDOWN:
                HandleRightButtonDown(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_RBUTTONUP:
                HandleRightButtonUp(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_MBUTTONDOWN:
                HandleMiddleButtonDown(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_MBUTTONUP:
                HandleMiddleButtonUp(wParam, lParam);
                handled = true;
                break;
            case User32Native.WM_MOUSEWHEEL:
                HandleMouseWheel(wParam, lParam);
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private void HandlePaint(IntPtr hwnd)
    {
        if (_renderWindow != IntPtr.Zero)
        {
            Render();
        }

        User32Native.ValidateRect(hwnd, IntPtr.Zero);
    }

    private void CreateScene()
    {
        _renderWindow = VtkNativeMethods.RenderWindow_Create();
        _renderer = VtkNativeMethods.Renderer_Create();
        _overlayRenderer = VtkNativeMethods.Renderer_Create();
        _hudRenderer = VtkNativeMethods.Renderer_Create();
        _interactor = VtkNativeMethods.RenderWindowInteractor_Create();
        _interactorStyle = VtkNativeMethods.InteractorStyleTrackballCamera_Create();
        _orientationAxesMarker = VtkNativeMethods.OrientationAxesMarker_Create();
        _orientationWidget = VtkNativeMethods.OrientationMarkerWidget_Create();

        VtkNativeMethods.RenderWindow_SetWindowId(_renderWindow, _hwnd);
        VtkNativeMethods.RenderWindow_SetNumberOfLayers(_renderWindow, 3);
        ResizeRenderWindow();
        VtkNativeMethods.Renderer_SetLayer(_renderer, 0);
        VtkNativeMethods.Renderer_SetLayer(_overlayRenderer, 1);
        VtkNativeMethods.Renderer_SetLayer(_hudRenderer, 2);
        VtkNativeMethods.Renderer_InteractiveOff(_overlayRenderer);
        VtkNativeMethods.Renderer_InteractiveOff(_hudRenderer);
        VtkNativeMethods.Renderer_SetActiveCamera(_overlayRenderer, _renderer);
        VtkNativeMethods.Renderer_SetActiveCamera(_hudRenderer, _renderer);
        VtkNativeMethods.RenderWindow_AddRenderer(_renderWindow, _renderer);
        VtkNativeMethods.RenderWindow_AddRenderer(_renderWindow, _overlayRenderer);
        VtkNativeMethods.RenderWindow_AddRenderer(_renderWindow, _hudRenderer);

        VtkNativeMethods.RenderWindowInteractor_SetRenderWindow(_interactor, _renderWindow);
        VtkNativeMethods.RenderWindowInteractor_SetInteractorStyle(_interactor, _interactorStyle);
        VtkNativeMethods.RenderWindowInteractor_SetInstallMessageProc(_interactor, 0);

        VtkNativeMethods.OrientationMarkerWidget_SetOrientationMarker(_orientationWidget, _orientationAxesMarker);
        VtkNativeMethods.OrientationMarkerWidget_SetInteractor(_orientationWidget, _interactor);
        VtkNativeMethods.OrientationMarkerWidget_SetViewport(_orientationWidget, 0.0, 0.0, 0.18, 0.18);

        VtkNativeMethods.RenderWindowInteractor_Initialize(_interactor);
        VtkNativeMethods.RenderWindowInteractor_Enable(_interactor);

        CreatePivotVisual();
        ApplySceneOptions();
        Render();
    }

    private void DestroyScene()
    {
        DestroyPointCloudObjects();
        ClearDefaultCameraState();
        DestroyMeasurementOverlay();
        DestroyPivotVisual();

        if (_renderer != IntPtr.Zero)
        {
            VtkNativeMethods.Renderer_SetPass(_renderer, IntPtr.Zero);
        }

        DestroyIfCreated(ref _orientationWidget, VtkNativeMethods.OrientationMarkerWidget_Destroy);
        DestroyIfCreated(ref _orientationAxesMarker, VtkNativeMethods.OrientationAxesMarker_Destroy);
        DestroyIfCreated(ref _interactorStyle, VtkNativeMethods.InteractorStyle_Destroy);
        DestroyIfCreated(ref _interactor, VtkNativeMethods.RenderWindowInteractor_Destroy);
        DestroyIfCreated(ref _hudRenderer, VtkNativeMethods.Renderer_Destroy);
        DestroyIfCreated(ref _overlayRenderer, VtkNativeMethods.Renderer_Destroy);
        DestroyIfCreated(ref _renderer, VtkNativeMethods.Renderer_Destroy);
        DestroyIfCreated(ref _renderStepsPass, VtkNativeMethods.RenderStepsPass_Destroy);
        DestroyIfCreated(ref _edlPass, VtkNativeMethods.CloudCompareEdlPass_Destroy);

        if (_renderWindow != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindow_Finalize(_renderWindow);
            VtkNativeMethods.RenderWindow_Destroy(_renderWindow);
            _renderWindow = IntPtr.Zero;
        }
    }

    private void ApplySceneOptions()
    {
        if (_renderer == IntPtr.Zero)
        {
            return;
        }

        ApplyBackground();
        ApplyOrientationWidget();
        ApplyEdl();
        ApplyActorVisuals(
            _actor,
            setSolidColor: _options.ColorAxis == ScalarColorAxis.None,
            solidColor: _options.SolidPointColor);
        ApplyActorVisuals(
            _secondaryActor,
            setSolidColor: true,
            solidColor: Color.FromRgb(145, 145, 145));
        ApplyMeasurementMarkerVisuals();
    }

    private void ApplyBackground()
    {
        var top = _options.BackgroundTop;
        var bottom = _options.BackgroundBottom;

        VtkNativeMethods.Renderer_SetBackground(_renderer, bottom.ScR, bottom.ScG, bottom.ScB);
        VtkNativeMethods.Renderer_SetBackground2(_renderer, top.ScR, top.ScG, top.ScB);

        if (_options.UseGradientBackground)
        {
            VtkNativeMethods.Renderer_GradientBackgroundOn(_renderer);
        }
        else
        {
            VtkNativeMethods.Renderer_GradientBackgroundOff(_renderer);
        }
    }

    private void ApplyOrientationWidget()
    {
        if (_orientationWidget == IntPtr.Zero)
        {
            return;
        }

        if (_options.ShowOrientationAxes)
        {
            VtkNativeMethods.OrientationMarkerWidget_SetEnabled(_orientationWidget, 1);
            VtkNativeMethods.OrientationMarkerWidget_SetInteractive(_orientationWidget, 0);
        }
        else
        {
            VtkNativeMethods.OrientationMarkerWidget_SetEnabled(_orientationWidget, 0);
        }
    }

    private void ApplyEdl()
    {
        if (!_options.EnableEdl)
        {
            if (_renderer != IntPtr.Zero)
            {
                VtkNativeMethods.Renderer_SetPass(_renderer, IntPtr.Zero);
            }

            DestroyIfCreated(ref _edlPass, VtkNativeMethods.CloudCompareEdlPass_Destroy);
            DestroyIfCreated(ref _renderStepsPass, VtkNativeMethods.RenderStepsPass_Destroy);
            return;
        }

        if (_renderStepsPass == IntPtr.Zero)
        {
            _renderStepsPass = VtkNativeMethods.RenderStepsPass_Create();
        }

        if (_edlPass == IntPtr.Zero)
        {
            _edlPass = VtkNativeMethods.CloudCompareEdlPass_Create();
            VtkNativeMethods.CloudCompareEdlPass_SetDelegatePass(_edlPass, _renderStepsPass);
            VtkNativeMethods.CloudCompareEdlPass_SetStrength(_edlPass, 100.0f);
            VtkNativeMethods.CloudCompareEdlPass_SetRadiusScales(_edlPass, 3.0f, 1.2f);
            VtkNativeMethods.CloudCompareEdlPass_SetLightDirection(_edlPass, 0.0f, 0.0f, 1.0f);
        }

        VtkNativeMethods.Renderer_SetPass(_renderer, _edlPass);
    }

    private static bool RequiresPointCloudPipelineRebuild(
        PointCloudRenderOptions previousOptions,
        PointCloudRenderOptions nextOptions)
    {
        return previousOptions.ColorAxis != nextOptions.ColorAxis
            || previousOptions.ShowScalarBar != nextOptions.ShowScalarBar
            || !string.Equals(previousOptions.ScalarTitle, nextOptions.ScalarTitle, StringComparison.Ordinal)
            || !ScalarParametersEqual(previousOptions.ScalarParameters, nextOptions.ScalarParameters);
    }

    private void RebuildPointCloudPipelinePreservingView()
    {
        if (_pendingPointCloud is null || _renderWindow == IntPtr.Zero)
        {
            return;
        }

        DestroyPointCloudObjects();
        SetPivotVisibility(false);
        ClearPointCloudRenderLayers();
        BuildPointCloudPipeline(_pendingPointCloud, resetCamera: false, captureDefaultCameraState: false);
    }

    private void ClearPointCloudRenderLayers()
    {
        if (_renderer != IntPtr.Zero)
        {
            VtkNativeMethods.Renderer_RemoveAllViewProps(_renderer);
        }

        if (_hudRenderer != IntPtr.Zero)
        {
            VtkNativeMethods.Renderer_RemoveAllViewProps(_hudRenderer);
        }
    }

    private void LoadPendingPointCloud(bool resetCamera, bool captureDefaultCameraState)
    {
        if (_pendingPointCloud is null || _renderWindow == IntPtr.Zero)
        {
            return;
        }

        DestroyPointCloudObjects();
        if (captureDefaultCameraState)
        {
            ClearDefaultCameraState();
        }

        SetPivotVisibility(false);
        ClearPointCloudRenderLayers();
        BuildPointCloudPipeline(_pendingPointCloud, resetCamera, captureDefaultCameraState);
    }

    private void BuildPointCloudPipeline(
        PointCloudHandle pointCloud,
        bool resetCamera,
        bool captureDefaultCameraState)
    {
        PointCloudBounds bounds = PointClouds.GetBounds(pointCloud);
        _sceneBounds = bounds;
        _hasSceneBounds = true;
        var bufferInfo = PointClouds.GetInterleavedBufferInfo(pointCloud);
        if (bufferInfo.Count <= 0 || bufferInfo.BufferPointer == IntPtr.Zero)
        {
            SetPivotVisibility(false);
            ApplySceneOptions();
            Render();
            return;
        }

        if (_options.ColorAxis == ScalarColorAxis.None)
        {
            BuildSolidPointCloudPipeline(bufferInfo);
        }
        else
        {
            BuildScalarPointCloudPipeline(pointCloud);
        }

        if (_options.ShowScalarBar && _options.ColorAxis != ScalarColorAxis.None && _lookupTable != IntPtr.Zero)
        {
            _scalarBarActor = VtkNativeMethods.ScalarBarActor_Create();
            VtkNativeMethods.ScalarBarActor_SetLookupTable(_scalarBarActor, _lookupTable);
            VtkNativeMethods.ScalarBarActor_SetTitle(_scalarBarActor, _options.ScalarTitle);
            VtkNativeMethods.ScalarBarActor_SetNumberOfLabels(_scalarBarActor, 6);
            VtkNativeMethods.ScalarBarActor_SetPosition(_scalarBarActor, 0.86, 0.08);
            VtkNativeMethods.ScalarBarActor_SetMaximumWidthInPixels(_scalarBarActor, 110);
            VtkNativeMethods.ScalarBarActor_SetMaximumHeightInPixels(_scalarBarActor, 280);
            VtkNativeMethods.ScalarBarActor_SetUnconstrainedFontSize(_scalarBarActor, 1);
            VtkNativeMethods.Renderer_AddActor2D(_hudRenderer, _scalarBarActor);
        }

        ApplySceneOptions();

        if (resetCamera)
        {
            VtkNativeMethods.Renderer_ResetCamera(_renderer);
        }
        else
        {
            VtkNativeMethods.Renderer_ResetCameraClippingRange(_renderer);
        }

        if (captureDefaultCameraState)
        {
            CaptureDefaultCameraState();
        }

        UpdatePivotScale(bounds);
        SyncPivotToCameraFocalPoint();
        SetPivotVisibility(false);
        Render();
    }

    private void BuildSolidPointCloudPipeline(PointCloudBufferInfo bufferInfo)
    {
        _points = VtkNativeMethods.Points_Create();
        VtkNativeMethods.Points_SetDataInterleavedF32(_points, bufferInfo.Count, bufferInfo.BufferPointer, bufferInfo.StrideBytes);

        _polyData = VtkNativeMethods.PolyData_Create();
        VtkNativeMethods.PolyData_SetPoints(_polyData, _points);

        _glyphFilter = VtkNativeMethods.VertexGlyphFilter_Create();
        VtkNativeMethods.VertexGlyphFilter_SetInputData(_glyphFilter, _polyData);
        VtkNativeMethods.VertexGlyphFilter_Update(_glyphFilter);

        _mapper = VtkNativeMethods.PolyDataMapper_Create();
        VtkNativeMethods.PolyDataMapper_SetInputData(_mapper, VtkNativeMethods.VertexGlyphFilter_GetOutput(_glyphFilter));
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_mapper);

        _actor = VtkNativeMethods.Actor_Create();
        VtkNativeMethods.Actor_SetMapper(_actor, _mapper);
        VtkNativeMethods.Renderer_AddActor(_renderer, _actor);
    }

    private void BuildScalarPointCloudPipeline(PointCloudHandle pointCloud)
    {
        int axisIndex = (int)_options.ColorAxis;
        float[] interleaved = PointClouds.ToInterleavedArray(pointCloud);
        if (interleaved.Length < 3)
        {
            return;
        }

        ScalarFieldRenderParameters scalarParameters = GetEffectiveScalarParameters(interleaved, axisIndex);
        (float[] inRangePoints, float[] outOfRangePoints) = SplitPointCloudByDisplayRange(interleaved, axisIndex, scalarParameters);
        (double displayMin, double displayMax) = GetSafeDisplayRange(scalarParameters);
        _lookupTable = CreateLookupTable(scalarParameters, displayMin, displayMax);

        if (inRangePoints.Length > 0)
        {
            BuildManagedPointCloudPipeline(
                inRangePoints,
                useScalars: true,
                axisIndex,
                _options.ScalarTitle,
                out _points,
                out _polyData,
                out _scalars,
                out _glyphFilter,
                out _mapper,
                out _actor);

            VtkNativeMethods.PolyDataMapper_SetLookupTable(_mapper, _lookupTable);
            VtkNativeMethods.PolyDataMapper_SetScalarRange(_mapper, displayMin, displayMax);
            VtkNativeMethods.PolyDataMapper_ScalarVisibilityOn(_mapper);
            VtkNativeMethods.Renderer_AddActor(_renderer, _actor);
        }

        if (outOfRangePoints.Length > 0)
        {
            BuildManagedPointCloudPipeline(
                outOfRangePoints,
                useScalars: false,
                axisIndex,
                _options.ScalarTitle,
                out _secondaryPoints,
                out _secondaryPolyData,
                out _,
                out _secondaryGlyphFilter,
                out _secondaryMapper,
                out _secondaryActor);

            VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_secondaryMapper);
            VtkNativeMethods.Renderer_AddActor(_renderer, _secondaryActor);
        }
    }

    private void BuildManagedPointCloudPipeline(
        float[] interleaved,
        bool useScalars,
        int axisIndex,
        string scalarTitle,
        out IntPtr points,
        out IntPtr polyData,
        out IntPtr scalars,
        out IntPtr glyphFilter,
        out IntPtr mapper,
        out IntPtr actor)
    {
        points = VtkNativeMethods.Points_Create();
        SetPointsFromManagedBuffer(points, interleaved);

        polyData = VtkNativeMethods.PolyData_Create();
        VtkNativeMethods.PolyData_SetPoints(polyData, points);

        scalars = IntPtr.Zero;
        if (useScalars)
        {
            scalars = VtkNativeMethods.FloatArray_Create();
            VtkNativeMethods.FloatArray_SetName(scalars, scalarTitle);
            SetScalarsFromManagedBuffer(scalars, interleaved, axisIndex);

            IntPtr pointData = VtkNativeMethods.PolyData_GetPointData(polyData);
            VtkNativeMethods.PointData_SetScalars(pointData, scalars);
        }

        glyphFilter = VtkNativeMethods.VertexGlyphFilter_Create();
        VtkNativeMethods.VertexGlyphFilter_SetInputData(glyphFilter, polyData);
        VtkNativeMethods.VertexGlyphFilter_Update(glyphFilter);

        mapper = VtkNativeMethods.PolyDataMapper_Create();
        VtkNativeMethods.PolyDataMapper_SetInputData(mapper, VtkNativeMethods.VertexGlyphFilter_GetOutput(glyphFilter));

        actor = VtkNativeMethods.Actor_Create();
        VtkNativeMethods.Actor_SetMapper(actor, mapper);
    }

    private ScalarFieldRenderParameters GetEffectiveScalarParameters(float[] interleaved, int axisIndex)
    {
        bool hasFiniteValue = false;
        double minValue = 0.0;
        double maxValue = 1.0;

        for (int i = axisIndex; i < interleaved.Length; i += 3)
        {
            float value = interleaved[i];
            if (!float.IsFinite(value))
            {
                continue;
            }

            if (!hasFiniteValue)
            {
                minValue = value;
                maxValue = value;
                hasFiniteValue = true;
                continue;
            }

            minValue = Math.Min(minValue, value);
            maxValue = Math.Max(maxValue, value);
        }

        ScalarFieldRenderParameters parameters = _options.ScalarParameters?.Clone()
            ?? ScalarFieldRenderParameters.CreateDefault(minValue, maxValue);

        if (hasFiniteValue)
        {
            parameters.UpdateDataBounds(minValue, maxValue, resetRanges: false);
        }
        else
        {
            parameters.UpdateDataBounds(0.0, 1.0, resetRanges: true);
        }

        parameters.Clamp();
        return parameters;
    }

    private static (float[] InRange, float[] OutOfRange) SplitPointCloudByDisplayRange(
        float[] interleaved,
        int axisIndex,
        ScalarFieldRenderParameters parameters)
    {
        int inRangeCount = 0;
        int outOfRangeCount = 0;
        for (int i = 0; i < interleaved.Length; i += 3)
        {
            if (!IsFinitePoint(interleaved, i))
            {
                continue;
            }

            float scalar = interleaved[i + axisIndex];
            if (scalar < parameters.DisplayMin || scalar > parameters.DisplayMax)
            {
                if (parameters.ShowOutOfRangeInGray)
                {
                    outOfRangeCount++;
                }
            }
            else
            {
                inRangeCount++;
            }
        }

        float[] inRange = new float[inRangeCount * 3];
        float[] outOfRange = parameters.ShowOutOfRangeInGray
            ? new float[outOfRangeCount * 3]
            : Array.Empty<float>();

        int inOffset = 0;
        int outOffset = 0;
        for (int i = 0; i < interleaved.Length; i += 3)
        {
            if (!IsFinitePoint(interleaved, i))
            {
                continue;
            }

            float scalar = interleaved[i + axisIndex];
            if (scalar < parameters.DisplayMin || scalar > parameters.DisplayMax)
            {
                if (!parameters.ShowOutOfRangeInGray)
                {
                    continue;
                }

                outOfRange[outOffset++] = interleaved[i];
                outOfRange[outOffset++] = interleaved[i + 1];
                outOfRange[outOffset++] = interleaved[i + 2];
                continue;
            }

            inRange[inOffset++] = interleaved[i];
            inRange[inOffset++] = interleaved[i + 1];
            inRange[inOffset++] = interleaved[i + 2];
        }

        return (inRange, outOfRange);
    }

    private static bool IsFinitePoint(float[] interleaved, int pointOffset)
    {
        return float.IsFinite(interleaved[pointOffset])
            && float.IsFinite(interleaved[pointOffset + 1])
            && float.IsFinite(interleaved[pointOffset + 2]);
    }

    private static void SetPointsFromManagedBuffer(IntPtr points, float[] interleaved)
    {
        if (interleaved.Length == 0)
        {
            VtkNativeMethods.Points_SetNumberOfPoints(points, 0);
            return;
        }

        GCHandle handle = GCHandle.Alloc(interleaved, GCHandleType.Pinned);
        try
        {
            VtkNativeMethods.Points_SetDataInterleavedF32(
                points,
                interleaved.Length / 3,
                handle.AddrOfPinnedObject(),
                sizeof(float) * 3);
        }
        finally
        {
            handle.Free();
        }
    }

    private static void SetScalarsFromManagedBuffer(IntPtr scalars, float[] interleaved, int axisIndex)
    {
        if (interleaved.Length == 0)
        {
            return;
        }

        GCHandle handle = GCHandle.Alloc(interleaved, GCHandleType.Pinned);
        try
        {
            VtkNativeMethods.FloatArray_SetDataFromInterleavedF32Axis(
                scalars,
                interleaved.Length / 3,
                handle.AddrOfPinnedObject(),
                sizeof(float) * 3,
                axisIndex,
                out _,
                out _);
        }
        finally
        {
            handle.Free();
        }
    }

    private IntPtr CreateLookupTable(ScalarFieldRenderParameters parameters, double rangeMin, double rangeMax)
    {
        const int colorSteps = 256;

        IntPtr lookupTable = VtkNativeMethods.LookupTable_Create();
        VtkNativeMethods.LookupTable_SetNumberOfTableValues(lookupTable, colorSteps);
        VtkNativeMethods.LookupTable_SetRange(lookupTable, rangeMin, rangeMax);
        VtkNativeMethods.LookupTable_Build(lookupTable);

        double step = colorSteps > 1 ? (rangeMax - rangeMin) / (colorSteps - 1) : 0.0;
        for (int i = 0; i < colorSteps; i++)
        {
            double rawValue = rangeMin + step * i;
            double normalized = NormalizeScalar(rawValue, parameters);
            Color color = EvaluateScalarRamp(normalized);
            VtkNativeMethods.LookupTable_SetTableValue(lookupTable, i, color.ScR, color.ScG, color.ScB, 1.0);
        }

        return lookupTable;
    }

    private static (double Min, double Max) GetSafeDisplayRange(ScalarFieldRenderParameters parameters)
    {
        double min = parameters.DisplayMin;
        double max = parameters.DisplayMax;
        if (!(max > min))
        {
            double epsilon = Math.Max(1e-6, Math.Abs(min) * 1e-6);
            min -= epsilon;
            max += epsilon;
        }

        return (min, max);
    }

    private static double NormalizeScalar(double value, ScalarFieldRenderParameters parameters)
    {
        if (!double.IsFinite(value) || value < parameters.DisplayMin || value > parameters.DisplayMax)
        {
            return -1.0;
        }

        if (!parameters.LogScale)
        {
            double saturationStart = parameters.SaturationMin;
            double saturationStop = parameters.SaturationMax;
            double saturationRange = Math.Max(saturationStop - saturationStart, 1e-12);

            if (!parameters.SymmetricalScale)
            {
                if (value <= saturationStart)
                {
                    return 0.0;
                }

                if (value >= saturationStop)
                {
                    return 1.0;
                }

                return (value - saturationStart) / saturationRange;
            }

            if (Math.Abs(value) <= saturationStart)
            {
                return 0.5;
            }

            if (value >= 0)
            {
                if (value >= saturationStop)
                {
                    return 1.0;
                }

                return (1.0 + (value - saturationStart) / saturationRange) * 0.5;
            }

            if (value <= -saturationStop)
            {
                return 0.0;
            }

            return (1.0 + (value + saturationStart) / saturationRange) * 0.5;
        }

        double logValue = Math.Log10(Math.Max(Math.Abs(value), 1e-12));
        double logStart = parameters.SaturationMin;
        double logStop = parameters.SaturationMax;
        double logRange = Math.Max(logStop - logStart, 1e-12);

        if (logValue <= logStart)
        {
            return 0.0;
        }

        if (logValue >= logStop)
        {
            return 1.0;
        }

        return (logValue - logStart) / logRange;
    }

    private static Color EvaluateScalarRamp(double normalized)
    {
        double clamped = Math.Clamp(normalized, 0.0, 1.0);
        double hue = (2.0 / 3.0) * (1.0 - clamped);
        (double r, double g, double b) = HsvToRgb(hue, 1.0, 1.0);
        return Color.FromScRgb(1.0f, (float)r, (float)g, (float)b);
    }

    private static (double R, double G, double B) HsvToRgb(double hue, double saturation, double value)
    {
        if (saturation <= 0.0)
        {
            return (value, value, value);
        }

        double scaledHue = (hue % 1.0 + 1.0) % 1.0 * 6.0;
        int sector = (int)Math.Floor(scaledHue);
        double fraction = scaledHue - sector;
        double p = value * (1.0 - saturation);
        double q = value * (1.0 - saturation * fraction);
        double t = value * (1.0 - saturation * (1.0 - fraction));

        return sector switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q),
        };
    }

    private void CaptureDefaultCameraState()
    {
        if (_renderer == IntPtr.Zero)
        {
            return;
        }

        _defaultCameraPosition = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraPosition);
        _defaultCameraFocalPoint = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraFocalPoint);
        _defaultCameraViewUp = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraViewUp);
        _defaultCameraDistance = GetDistance(_defaultCameraPosition, _defaultCameraFocalPoint);
        _hasDefaultCameraState = true;
    }

    private void ClearDefaultCameraState()
    {
        _defaultCameraPosition = default;
        _defaultCameraFocalPoint = default;
        _defaultCameraViewUp = default;
        _defaultCameraDistance = 0.0;
        _hasDefaultCameraState = false;
        ClearPivotScaleState();
    }

    private Point3d GetCameraVector(Action<IntPtr, double[]> getter)
    {
        double[] xyz = new double[3];
        getter(_renderer, xyz);
        return new Point3d(xyz[0], xyz[1], xyz[2]);
    }

    private double GetFallbackCameraDistance()
    {
        if (IsValidCameraDistance(_defaultCameraDistance))
        {
            return _defaultCameraDistance;
        }

        if (_hasSceneBounds)
        {
            double diagonal = Math.Sqrt(
                _sceneBounds.SizeX * _sceneBounds.SizeX
                + _sceneBounds.SizeY * _sceneBounds.SizeY
                + _sceneBounds.SizeZ * _sceneBounds.SizeZ);

            if (IsValidCameraDistance(diagonal))
            {
                return diagonal * 1.2;
            }
        }

        return 1.0;
    }

    private static double GetDistance(Point3d first, Point3d second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        double dz = first.Z - second.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static bool IsValidCameraDistance(double distance)
    {
        return double.IsFinite(distance) && distance > 1e-6;
    }

    private void CreateMeasurementMarkerActor(int index, Point3d point)
    {
        if (index < 0 || index >= MeasurementMarkerCount || _overlayRenderer == IntPtr.Zero)
        {
            return;
        }

        _measurementMarkerPoints[index] = VtkNativeMethods.Points_Create();
        _measurementMarkerCells[index] = VtkNativeMethods.CellArray_Create();
        _measurementMarkerPolyData[index] = VtkNativeMethods.PolyData_Create();
        _measurementMarkerMappers[index] = VtkNativeMethods.PolyDataMapper_Create();
        _measurementMarkerActors[index] = VtkNativeMethods.Actor_Create();

        VtkNativeMethods.Points_SetNumberOfPoints(_measurementMarkerPoints[index], 1);
        VtkNativeMethods.Points_SetPoint(_measurementMarkerPoints[index], 0, point.X, point.Y, point.Z);
        VtkNativeMethods.CellArray_InsertNextCell(_measurementMarkerCells[index], 1);
        VtkNativeMethods.CellArray_InsertCellPoint(_measurementMarkerCells[index], 0);
        VtkNativeMethods.PolyData_SetPoints(_measurementMarkerPolyData[index], _measurementMarkerPoints[index]);
        VtkNativeMethods.PolyData_SetVerts(_measurementMarkerPolyData[index], _measurementMarkerCells[index]);
        VtkNativeMethods.PolyDataMapper_SetInputData(
            _measurementMarkerMappers[index],
            _measurementMarkerPolyData[index]);
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_measurementMarkerMappers[index]);
        VtkNativeMethods.Actor_SetMapper(_measurementMarkerActors[index], _measurementMarkerMappers[index]);
        ApplyMeasurementMarkerVisuals(index);
        VtkNativeMethods.Renderer_AddActor(_overlayRenderer, _measurementMarkerActors[index]);
    }

    private void ApplyMeasurementMarkerVisuals()
    {
        for (int index = 0; index < MeasurementMarkerCount; index++)
        {
            ApplyMeasurementMarkerVisuals(index);
        }
    }

    private void ApplyMeasurementMarkerVisuals(int index)
    {
        if (index < 0 || index >= MeasurementMarkerCount || _measurementMarkerActors[index] == IntPtr.Zero)
        {
            return;
        }

        VtkNativeMethods.Actor_SetPointSize(_measurementMarkerActors[index], (float)_options.PointSize);
        VtkNativeMethods.Actor_SetColor(_measurementMarkerActors[index], 1.0, 0.18, 0.12);
        VtkNativeMethods.Actor_SetOpacity(_measurementMarkerActors[index], 1.0);
    }

    private void CreateMeasurementLineActor(IReadOnlyList<Point3d> points)
    {
        if (_overlayRenderer == IntPtr.Zero || points.Count < 2)
        {
            return;
        }

        int pointCount = points.Count >= 3 ? 4 : 2;
        _measurementLinePoints = VtkNativeMethods.Points_Create();
        _measurementLineCells = VtkNativeMethods.CellArray_Create();
        _measurementLinePolyData = VtkNativeMethods.PolyData_Create();
        _measurementLineMapper = VtkNativeMethods.PolyDataMapper_Create();
        _measurementLineActor = VtkNativeMethods.Actor_Create();

        VtkNativeMethods.Points_SetNumberOfPoints(_measurementLinePoints, pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            Point3d point = i < points.Count ? points[i] : points[0];
            VtkNativeMethods.Points_SetPoint(_measurementLinePoints, i, point.X, point.Y, point.Z);
        }

        VtkNativeMethods.CellArray_InsertNextCell(_measurementLineCells, pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            VtkNativeMethods.CellArray_InsertCellPoint(_measurementLineCells, i);
        }

        VtkNativeMethods.PolyData_SetPoints(_measurementLinePolyData, _measurementLinePoints);
        VtkNativeMethods.PolyData_SetLines(_measurementLinePolyData, _measurementLineCells);
        VtkNativeMethods.PolyDataMapper_SetInputData(_measurementLineMapper, _measurementLinePolyData);
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_measurementLineMapper);
        VtkNativeMethods.Actor_SetMapper(_measurementLineActor, _measurementLineMapper);
        VtkNativeMethods.Actor_SetLineWidth(_measurementLineActor, MeasurementLineWidth);
        VtkNativeMethods.Actor_SetColor(_measurementLineActor, 1.0, 0.18, 0.12);
        VtkNativeMethods.Actor_SetOpacity(_measurementLineActor, 0.96);
        VtkNativeMethods.Renderer_AddActor(_overlayRenderer, _measurementLineActor);
    }

    private void CreateMeasurementTriangleActor(IReadOnlyList<Point3d> points)
    {
        if (_overlayRenderer == IntPtr.Zero || points.Count < 3)
        {
            return;
        }

        _measurementTrianglePoints = VtkNativeMethods.Points_Create();
        _measurementTriangleCells = VtkNativeMethods.CellArray_Create();
        _measurementTrianglePolyData = VtkNativeMethods.PolyData_Create();
        _measurementTriangleMapper = VtkNativeMethods.PolyDataMapper_Create();
        _measurementTriangleActor = VtkNativeMethods.Actor_Create();

        VtkNativeMethods.Points_SetNumberOfPoints(_measurementTrianglePoints, 3);
        for (int i = 0; i < 3; i++)
        {
            Point3d point = points[i];
            VtkNativeMethods.Points_SetPoint(_measurementTrianglePoints, i, point.X, point.Y, point.Z);
        }

        VtkNativeMethods.CellArray_InsertNextCell(_measurementTriangleCells, 3);
        VtkNativeMethods.CellArray_InsertCellPoint(_measurementTriangleCells, 0);
        VtkNativeMethods.CellArray_InsertCellPoint(_measurementTriangleCells, 1);
        VtkNativeMethods.CellArray_InsertCellPoint(_measurementTriangleCells, 2);

        VtkNativeMethods.PolyData_SetPoints(_measurementTrianglePolyData, _measurementTrianglePoints);
        VtkNativeMethods.PolyData_SetPolys(_measurementTrianglePolyData, _measurementTriangleCells);
        VtkNativeMethods.PolyDataMapper_SetInputData(_measurementTriangleMapper, _measurementTrianglePolyData);
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_measurementTriangleMapper);
        VtkNativeMethods.Actor_SetMapper(_measurementTriangleActor, _measurementTriangleMapper);
        VtkNativeMethods.Actor_SetColor(_measurementTriangleActor, 1.0, 0.78, 0.18);
        VtkNativeMethods.Actor_SetOpacity(_measurementTriangleActor, 0.28);
        VtkNativeMethods.Renderer_AddActor(_overlayRenderer, _measurementTriangleActor);
    }

    private void CreatePivotVisual()
    {
        if (_overlayRenderer == IntPtr.Zero)
        {
            return;
        }

        CreatePivotRingActor(PivotXRingIndex, Color.FromRgb(255, 84, 84));
        CreatePivotRingActor(PivotYRingIndex, Color.FromRgb(92, 255, 92));
        CreatePivotRingActor(PivotZRingIndex, Color.FromRgb(64, 192, 255));
        CreatePivotAxisActor(PivotXAxisIndex, new Point3d(1.0, 0.0, 0.0), Color.FromRgb(255, 84, 84));
        CreatePivotAxisActor(PivotYAxisIndex, new Point3d(0.0, 1.0, 0.0), Color.FromRgb(92, 255, 92));
        CreatePivotAxisActor(PivotZAxisIndex, new Point3d(0.0, 0.0, 1.0), Color.FromRgb(64, 192, 255));
        CreatePivotCenterActor(PivotCenterOutlineIndex, Color.FromRgb(255, 196, 32), PivotCenterOutlineRadius);
        CreatePivotCenterActor(PivotCenterFillIndex, Color.FromRgb(255, 236, 120), PivotCenterFillRadius);
        SetPivotVisibility(false);
    }

    private void CreatePivotRingActor(int index, Color color)
    {
        CreatePivotActorResources(index);

        int pointCount = PivotSegmentCount + 1;
        VtkNativeMethods.Points_SetNumberOfPoints(_pivotPoints[index], pointCount);
        VtkNativeMethods.CellArray_InsertNextCell(_pivotLines[index], pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            double angle = (Math.PI * 2.0 * i) / PivotSegmentCount;
            Point3d point = GetPivotRingPoint(index, angle);
            VtkNativeMethods.Points_SetPoint(_pivotPoints[index], i, point.X, point.Y, point.Z);
            VtkNativeMethods.CellArray_InsertCellPoint(_pivotLines[index], i);
        }

        ConfigurePivotLineActor(index, color, PivotRingLineWidth);
    }

    private void CreatePivotAxisActor(int index, Point3d direction, Color color)
    {
        CreatePivotActorResources(index);

        VtkNativeMethods.Points_SetNumberOfPoints(_pivotPoints[index], 2);
        VtkNativeMethods.Points_SetPoint(_pivotPoints[index], 0, -direction.X, -direction.Y, -direction.Z);
        VtkNativeMethods.Points_SetPoint(_pivotPoints[index], 1, direction.X, direction.Y, direction.Z);

        VtkNativeMethods.CellArray_InsertNextCell(_pivotLines[index], 2);
        VtkNativeMethods.CellArray_InsertCellPoint(_pivotLines[index], 0);
        VtkNativeMethods.CellArray_InsertCellPoint(_pivotLines[index], 1);

        ConfigurePivotLineActor(index, color, PivotAxisLineWidth);
    }

    private void CreatePivotCenterActor(int index, Color color, double radius)
    {
        CreatePivotSphereActorResources(index);
        VtkNativeMethods.SphereSource_SetRadius(_pivotSphereSources[index], radius);
        VtkNativeMethods.SphereSource_SetThetaResolution(_pivotSphereSources[index], 24);
        VtkNativeMethods.SphereSource_SetPhiResolution(_pivotSphereSources[index], 24);
        VtkNativeMethods.SphereSource_Update(_pivotSphereSources[index]);
        ConfigurePivotSphereActor(index, color);
    }

    private void CreatePivotActorResources(int index)
    {
        _pivotPoints[index] = VtkNativeMethods.Points_Create();
        _pivotLines[index] = VtkNativeMethods.CellArray_Create();
        _pivotPolyData[index] = VtkNativeMethods.PolyData_Create();
        _pivotMappers[index] = VtkNativeMethods.PolyDataMapper_Create();
        _pivotActors[index] = VtkNativeMethods.Actor_Create();
    }

    private void ConfigurePivotLineActor(int index, Color color, float lineWidth)
    {
        VtkNativeMethods.PolyData_SetPoints(_pivotPolyData[index], _pivotPoints[index]);
        VtkNativeMethods.PolyData_SetLines(_pivotPolyData[index], _pivotLines[index]);
        VtkNativeMethods.PolyDataMapper_SetInputData(_pivotMappers[index], _pivotPolyData[index]);
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_pivotMappers[index]);
        VtkNativeMethods.Actor_SetMapper(_pivotActors[index], _pivotMappers[index]);
        VtkNativeMethods.Actor_SetLineWidth(_pivotActors[index], lineWidth);
        VtkNativeMethods.Actor_SetColor(_pivotActors[index], color.ScR, color.ScG, color.ScB);
        VtkNativeMethods.Actor_SetOpacity(_pivotActors[index], 0.95);
        VtkNativeMethods.Actor_SetVisibility(_pivotActors[index], 0);
        VtkNativeMethods.Renderer_AddActor(_overlayRenderer, _pivotActors[index]);
    }

    private void CreatePivotSphereActorResources(int index)
    {
        _pivotSphereSources[index] = VtkNativeMethods.SphereSource_Create();
        _pivotMappers[index] = VtkNativeMethods.PolyDataMapper_Create();
        _pivotActors[index] = VtkNativeMethods.Actor_Create();
    }

    private void ConfigurePivotSphereActor(int index, Color color)
    {
        VtkNativeMethods.PolyDataMapper_SetInputData(_pivotMappers[index], VtkNativeMethods.SphereSource_GetOutput(_pivotSphereSources[index]));
        VtkNativeMethods.PolyDataMapper_ScalarVisibilityOff(_pivotMappers[index]);
        VtkNativeMethods.Actor_SetMapper(_pivotActors[index], _pivotMappers[index]);
        VtkNativeMethods.Actor_SetColor(_pivotActors[index], color.ScR, color.ScG, color.ScB);
        VtkNativeMethods.Actor_SetOpacity(_pivotActors[index], 1.0);
        VtkNativeMethods.Actor_SetVisibility(_pivotActors[index], 0);
        VtkNativeMethods.Renderer_AddActor(_overlayRenderer, _pivotActors[index]);
    }

    private static Point3d GetPivotRingPoint(int index, double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);

        return index switch
        {
            PivotXRingIndex => new Point3d(0.0, cos, sin),
            PivotYRingIndex => new Point3d(cos, 0.0, sin),
            PivotZRingIndex => new Point3d(cos, sin, 0.0),
            _ => default,
        };
    }

    private void UpdatePivotScale(PointCloudBounds bounds)
    {
        double diagonal = Math.Sqrt(
            bounds.SizeX * bounds.SizeX
            + bounds.SizeY * bounds.SizeY
            + bounds.SizeZ * bounds.SizeZ);

        double radius = diagonal * PivotRadiusScaleFactor;
        if (!double.IsFinite(radius) || radius < PivotMinimumRadius)
        {
            radius = 1.0;
        }

        _pivotBaseScale = radius;
        _pivotBaseCameraDistance = GetCurrentCameraDistance();
        ApplyPivotScale(radius);
    }

    private void RefreshPivotScaleForCamera()
    {
        if (_pivotActors[0] == IntPtr.Zero)
        {
            return;
        }

        double scale = PivotScaleCalculator.CalculateCameraStableScale(
            _pivotBaseScale,
            _pivotBaseCameraDistance,
            GetCurrentCameraDistance());
        ApplyPivotScale(scale);
    }

    private void ApplyPivotScale(double scale)
    {
        for (int i = 0; i < PivotActorCount; i++)
        {
            if (_pivotActors[i] != IntPtr.Zero)
            {
                VtkNativeMethods.Actor_SetScale(_pivotActors[i], scale, scale, scale);
            }
        }
    }

    private double GetCurrentCameraDistance()
    {
        if (_renderer == IntPtr.Zero)
        {
            return 0.0;
        }

        Point3d cameraPosition = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraPosition);
        Point3d focalPoint = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraFocalPoint);
        return GetDistance(cameraPosition, focalPoint);
    }

    private void ClearPivotScaleState()
    {
        _pivotBaseScale = 1.0;
        _pivotBaseCameraDistance = 0.0;
    }

    private void SyncPivotToCameraFocalPoint()
    {
        if (_renderer == IntPtr.Zero || _pivotActors[0] == IntPtr.Zero)
        {
            return;
        }

        Point3d focalPoint = GetCameraVector(VtkNativeMethods.Renderer_GetActiveCameraFocalPoint);
        for (int i = 0; i < PivotActorCount; i++)
        {
            if (_pivotActors[i] != IntPtr.Zero)
            {
                VtkNativeMethods.Actor_SetPosition(_pivotActors[i], focalPoint.X, focalPoint.Y, focalPoint.Z);
            }
        }
    }

    private void SetPivotVisibility(bool visible)
    {
        bool targetVisibility = visible && (_actor != IntPtr.Zero || _secondaryActor != IntPtr.Zero);
        if (_isPivotVisible == targetVisibility)
        {
            return;
        }

        for (int i = 0; i < PivotActorCount; i++)
        {
            if (_pivotActors[i] != IntPtr.Zero)
            {
                VtkNativeMethods.Actor_SetVisibility(_pivotActors[i], targetVisibility ? 1 : 0);
            }
        }

        _isPivotVisible = targetVisibility;
    }

    private void DestroyPivotVisual()
    {
        _isPivotVisible = false;

        for (int i = 0; i < PivotActorCount; i++)
        {
            DestroyIfCreated(ref _pivotActors[i], VtkNativeMethods.Actor_Destroy);
            DestroyIfCreated(ref _pivotMappers[i], VtkNativeMethods.PolyDataMapper_Destroy);
            DestroyIfCreated(ref _pivotSphereSources[i], VtkNativeMethods.SphereSource_Destroy);
            DestroyIfCreated(ref _pivotPolyData[i], VtkNativeMethods.PolyData_Destroy);
            DestroyIfCreated(ref _pivotLines[i], VtkNativeMethods.CellArray_Destroy);
            DestroyIfCreated(ref _pivotPoints[i], VtkNativeMethods.Points_Destroy);
        }
    }

    private void DestroyMeasurementOverlay()
    {
        for (int i = 0; i < MeasurementMarkerCount; i++)
        {
            RemoveOverlayActor(_measurementMarkerActors[i]);
            DestroyIfCreated(ref _measurementMarkerActors[i], VtkNativeMethods.Actor_Destroy);
            DestroyIfCreated(ref _measurementMarkerMappers[i], VtkNativeMethods.PolyDataMapper_Destroy);
            DestroyIfCreated(ref _measurementMarkerPolyData[i], VtkNativeMethods.PolyData_Destroy);
            DestroyIfCreated(ref _measurementMarkerCells[i], VtkNativeMethods.CellArray_Destroy);
            DestroyIfCreated(ref _measurementMarkerPoints[i], VtkNativeMethods.Points_Destroy);
        }

        RemoveOverlayActor(_measurementLineActor);
        DestroyIfCreated(ref _measurementLineActor, VtkNativeMethods.Actor_Destroy);
        DestroyIfCreated(ref _measurementLineMapper, VtkNativeMethods.PolyDataMapper_Destroy);
        DestroyIfCreated(ref _measurementLinePolyData, VtkNativeMethods.PolyData_Destroy);
        DestroyIfCreated(ref _measurementLineCells, VtkNativeMethods.CellArray_Destroy);
        DestroyIfCreated(ref _measurementLinePoints, VtkNativeMethods.Points_Destroy);

        RemoveOverlayActor(_measurementTriangleActor);
        DestroyIfCreated(ref _measurementTriangleActor, VtkNativeMethods.Actor_Destroy);
        DestroyIfCreated(ref _measurementTriangleMapper, VtkNativeMethods.PolyDataMapper_Destroy);
        DestroyIfCreated(ref _measurementTrianglePolyData, VtkNativeMethods.PolyData_Destroy);
        DestroyIfCreated(ref _measurementTriangleCells, VtkNativeMethods.CellArray_Destroy);
        DestroyIfCreated(ref _measurementTrianglePoints, VtkNativeMethods.Points_Destroy);
    }

    private void RemoveOverlayActor(IntPtr actor)
    {
        if (_overlayRenderer != IntPtr.Zero && actor != IntPtr.Zero)
        {
            VtkNativeMethods.Renderer_RemoveActor(_overlayRenderer, actor);
        }
    }

    private void DestroyPointCloudObjects()
    {
        _sceneBounds = default;
        _hasSceneBounds = false;
        DestroyIfCreated(ref _scalarBarActor, VtkNativeMethods.ScalarBarActor_Destroy);
        DestroyIfCreated(ref _secondaryActor, VtkNativeMethods.Actor_Destroy);
        DestroyIfCreated(ref _secondaryMapper, VtkNativeMethods.PolyDataMapper_Destroy);
        DestroyIfCreated(ref _secondaryGlyphFilter, VtkNativeMethods.VertexGlyphFilter_Destroy);
        DestroyIfCreated(ref _secondaryPolyData, VtkNativeMethods.PolyData_Destroy);
        DestroyIfCreated(ref _secondaryPoints, VtkNativeMethods.Points_Destroy);
        DestroyIfCreated(ref _actor, VtkNativeMethods.Actor_Destroy);
        DestroyIfCreated(ref _mapper, VtkNativeMethods.PolyDataMapper_Destroy);
        DestroyIfCreated(ref _lookupTable, VtkNativeMethods.LookupTable_Destroy);
        DestroyIfCreated(ref _glyphFilter, VtkNativeMethods.VertexGlyphFilter_Destroy);
        DestroyIfCreated(ref _scalars, VtkNativeMethods.FloatArray_Destroy);
        DestroyIfCreated(ref _polyData, VtkNativeMethods.PolyData_Destroy);
        DestroyIfCreated(ref _points, VtkNativeMethods.Points_Destroy);
    }

    private void ResizeRenderWindow()
    {
        ResizeRenderWindow((int)Math.Ceiling(ActualWidth), (int)Math.Ceiling(ActualHeight));
    }

    private void ResizeRenderWindow(int width, int height)
    {
        if (_renderWindow == IntPtr.Zero)
        {
            return;
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        VtkNativeMethods.RenderWindow_SetSize(_renderWindow, width, height);
    }

    private void HandleSize(IntPtr wParam, IntPtr lParam)
    {
        (int width, int height) = GetClientPoint(lParam);
        ResizeRenderWindow(width, height);

        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnSize(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), width, height);
        }

        if (width > 0 && height > 0)
        {
            Render();
        }
    }

    private void HandleMouseMove(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        bool becameDragging = false;

        if (_isLeftButtonDown
            && !_leftButtonDragDetected
            && (Math.Abs(x - _leftButtonDownX) > 2 || Math.Abs(y - _leftButtonDownY) > 2))
        {
            _leftButtonDragDetected = true;
            becameDragging = true;
        }

        if (_leftButtonDragDetected)
        {
            SyncPivotToCameraFocalPoint();
            if (becameDragging)
            {
                SetPivotVisibility(true);
            }
        }

        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnMouseMove(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y);
        }
    }

    private void HandleLeftButtonDown(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        _leftButtonDownX = x;
        _leftButtonDownY = y;
        _isLeftButtonDown = true;
        _leftButtonDragDetected = false;
        SetPivotVisibility(false);

        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnLButtonDown(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y, 0);
        }
    }

    private void HandleLeftButtonUp(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        if (_leftButtonDragDetected)
        {
            SetPivotVisibility(false);
        }

        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnLButtonUp(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y);
        }

        bool shouldPick = _isLeftButtonDown && !_leftButtonDragDetected;
        _isLeftButtonDown = false;
        _leftButtonDragDetected = false;

        if (shouldPick)
        {
            HandlePick(x, y);
        }
    }

    private void HandleMiddleButtonDown(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnMButtonDown(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y, 0);
        }
    }

    private void HandleMiddleButtonUp(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnMButtonUp(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y);
        }
    }

    private void HandleRightButtonDown(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnRButtonDown(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y, 0);
        }
    }

    private void HandleRightButtonUp(IntPtr wParam, IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        if (_interactor != IntPtr.Zero)
        {
            VtkNativeMethods.RenderWindowInteractor_OnRButtonUp(_interactor, _hwnd, unchecked((uint)wParam.ToInt64()), x, y);
        }
    }

    private void HandleMouseWheel(IntPtr wParam, IntPtr lParam)
    {
        if (_interactor == IntPtr.Zero)
        {
            return;
        }

        var point = GetScreenPoint(lParam);
        User32Native.ScreenToClient(_hwnd, ref point);

        short delta = unchecked((short)((wParam.ToInt64() >> 16) & 0xFFFF));
        uint flags = unchecked((uint)wParam.ToInt64());
        bool zoomed = false;
        if (delta > 0)
        {
            VtkNativeMethods.RenderWindowInteractor_OnMouseWheelForward(_interactor, _hwnd, flags, point.X, point.Y);
            zoomed = true;
        }
        else if (delta < 0)
        {
            VtkNativeMethods.RenderWindowInteractor_OnMouseWheelBackward(_interactor, _hwnd, flags, point.X, point.Y);
            zoomed = true;
        }

        if (zoomed)
        {
            SyncPivotToCameraFocalPoint();
            RefreshPivotScaleForCamera();
            Render();
        }
    }

    private static (int X, int Y) GetClientPoint(IntPtr lParam)
    {
        int raw = unchecked((int)lParam.ToInt64());
        int x = unchecked((short)(raw & 0xFFFF));
        int y = unchecked((short)((raw >> 16) & 0xFFFF));
        return (x, y);
    }

    private static User32Native.POINT GetScreenPoint(IntPtr lParam)
    {
        (int x, int y) = GetClientPoint(lParam);
        return new User32Native.POINT
        {
            X = x,
            Y = y,
        };
    }

    private void HandlePick(int x, int y)
    {
        if (_renderer == IntPtr.Zero)
        {
            return;
        }

        (int displayX, int displayY) = VtkDisplayCoordinateMapper.FromClientPoint(x, y, GetRenderWindowHeight());
        var xyz = new double[3];
        if (VtkNativeMethods.Renderer_PickPoint(_renderer, displayX, displayY, xyz) == 0)
        {
            return;
        }

        PointPicked?.Invoke(
            this,
            new PointPickedEventArgs(
                new Point3d(xyz[0], xyz[1], xyz[2]),
                new System.Windows.Point(x, y)));
    }

    private int GetRenderWindowHeight()
    {
        if (_renderWindow == IntPtr.Zero)
        {
            return Math.Max(1, (int)Math.Ceiling(ActualHeight));
        }

        int[] size = new int[2];
        VtkNativeMethods.RenderWindow_GetSize(_renderWindow, size);
        return size[1] > 0 ? size[1] : Math.Max(1, (int)Math.Ceiling(ActualHeight));
    }

    private static void DestroyIfCreated(ref IntPtr pointer, Action<IntPtr> destroyAction)
    {
        if (pointer == IntPtr.Zero)
        {
            return;
        }

        destroyAction(pointer);
        pointer = IntPtr.Zero;
    }

    private void ApplyActorVisuals(IntPtr actor, bool setSolidColor, Color solidColor)
    {
        if (actor == IntPtr.Zero)
        {
            return;
        }

        VtkNativeMethods.Actor_SetPointSize(actor, (float)_options.PointSize);
        VtkNativeMethods.Actor_SetOpacity(actor, _options.Opacity);

        if (setSolidColor)
        {
            VtkNativeMethods.Actor_SetColor(actor, solidColor.ScR, solidColor.ScG, solidColor.ScB);
        }
    }

    private static bool ScalarParametersEqual(ScalarFieldRenderParameters? left, ScalarFieldRenderParameters? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return NearlyEqual(left.DataMin, right.DataMin)
            && NearlyEqual(left.DataMax, right.DataMax)
            && NearlyEqual(left.DisplayMin, right.DisplayMin)
            && NearlyEqual(left.DisplayMax, right.DisplayMax)
            && NearlyEqual(left.SaturationMin, right.SaturationMin)
            && NearlyEqual(left.SaturationMax, right.SaturationMax)
            && left.ShowOutOfRangeInGray == right.ShowOutOfRangeInGray
            && left.AlwaysShowZero == right.AlwaysShowZero
            && left.SymmetricalScale == right.SymmetricalScale
            && left.LogScale == right.LogScale;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-12;
    }
}

internal static class VtkDisplayCoordinateMapper
{
    internal static (int X, int Y) FromClientPoint(int clientX, int clientY, int renderHeight)
    {
        if (renderHeight <= 0)
        {
            return (clientX, clientY);
        }

        return (clientX, renderHeight - 1 - clientY);
    }
}

internal static class PivotScaleCalculator
{
    internal static double CalculateCameraStableScale(
        double baseScale,
        double baseCameraDistance,
        double currentCameraDistance)
    {
        if (!IsValidScale(baseScale))
        {
            return 1.0;
        }

        if (!IsValidDistance(baseCameraDistance) || !IsValidDistance(currentCameraDistance))
        {
            return baseScale;
        }

        double scale = baseScale * (currentCameraDistance / baseCameraDistance);
        return IsValidScale(scale) ? scale : baseScale;
    }

    private static bool IsValidScale(double scale)
    {
        return double.IsFinite(scale) && scale > 0.0;
    }

    private static bool IsValidDistance(double distance)
    {
        return double.IsFinite(distance) && distance > 1e-6;
    }
}

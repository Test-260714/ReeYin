using Microsoft.Win32;
using PointCloud.Algorithms.Services;
using PointCloud.Interop;
using PointCloud.ToolViewer.Dialogs;
using PointCloud.ToolViewer.Models;
using PointCloud.VTKWPF.Models;
using Prism.Commands;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PointCloud.ToolViewer.ViewModels;

public sealed class PointCloudSceneChangedEventArgs : EventArgs
{
    public PointCloudSceneChangedEventArgs(bool resetCamera)
    {
        ResetCamera = resetCamera;
    }

    public bool ResetCamera { get; }
}

public sealed class PointCloudViewOrientationRequestedEventArgs : EventArgs
{
    public PointCloudViewOrientationRequestedEventArgs(PointCloudViewOrientation orientation)
    {
        Orientation = orientation;
    }

    public PointCloudViewOrientation Orientation { get; }
}

public sealed class PointPickingMeasurementOverlayEventArgs : EventArgs
{
    public PointPickingMeasurementOverlayEventArgs(PointPickingMeasurementOverlay overlay)
    {
        Overlay = overlay;
    }

    public PointPickingMeasurementOverlay Overlay { get; }
}

public sealed class PointCloudViewerViewModel : DialogViewModelBase, IViewModuleParam
{
    private const string SupportedFileExtensions = "*.ply;*.pcd;*.obj;*.txt;*.xyz;*.asc;*.csv;*.pts;*.stl;*.tif;*.tiff";
    private const string OpenFileFilter =
        "点云/深度图文件 (*.ply;*.pcd;*.obj;*.txt;*.xyz;*.asc;*.csv;*.pts;*.stl;*.tif;*.tiff)|*.ply;*.pcd;*.obj;*.txt;*.xyz;*.asc;*.csv;*.pts;*.stl;*.tif;*.tiff|所有文件 (*.*)|*.*";
    private const int MaxLogEntries = 200;
    private const string DefaultTitle = "Point Cloud Viewer";
    private const string DefaultMeasurementText = "尚未启用选点测量";
    private const string DefaultFileName = "未加载点云";
    private const string DefaultSelectedPointText = "尚未拾取点";
    private const string DefaultPipelineText = "PointCloud.Algorithms.Copy 会创建用于展示的副本，避免直接修改原始点云数据。";
    private const string DefaultStatusText = "就绪";
    private const string DefaultReadyText = "加载点云后即可开始浏览、切换 EDL 或设置旋转中心。";

    private readonly DelegateCommand _openPointCloudCommand;
    private readonly DelegateCommand _reloadCommand;
    private readonly DelegateCommand _clearCommand;
    private readonly DelegateCommand _setScalarFieldCommand;
    private readonly DelegateCommand _scalePointCloudCommand;
    private readonly DelegateCommand _editScalarFieldDisplayParamsCommand;
    private readonly DelegateCommand _openPointPickingMeasurementDialogCommand;
    private readonly DelegateCommand _resetCameraCommand;
    private readonly DelegateCommand<PointCloudViewOrientation?> _setViewOrientationCommand;
    private readonly DelegateCommand<PointPickingMeasurementMode?> _setPointPickingMeasurementModeCommand;
    private readonly DelegateCommand _clearPointPickingMeasurementCommand;
    private readonly PointPickingMeasurementState _pointPickingMeasurementState = new();

    private CancellationTokenSource? _loadCts;
    private PointCloudHandle? _sourceCloud;
    private PointCloudHandle? _displayCloud;
    private string _filePath = string.Empty;
    private string _fileName = DefaultFileName;
    private string _fileExtension = "--";
    private string _pointCountText = "0";
    private string _boundsMinText = "--";
    private string _boundsMaxText = "--";
    private string _boundsSizeText = "--";
    private string _sceneTitle = DefaultTitle;
    private string _statusText = DefaultStatusText;
    private string _overlayText = DefaultReadyText;
    private string _selectedPointText = DefaultSelectedPointText;
    private string _measurementText = DefaultMeasurementText;
    private string _pipelineText = DefaultPipelineText;
    private bool _isBusy;
    private bool _enableEdl;
    private bool _showOrientationAxes = true;
    private bool _useGradientBackground = true;
    private bool _isPivotPickingEnabled;
    private PointPickingMeasurementMode _currentMeasurementMode = PointPickingMeasurementMode.None;
    private ScalarColorAxis _scalarColorAxis = ScalarColorAxis.None;
    private ScalarFieldRenderParameters? _scalarFieldParameters;
    private DepthImageImportParameters _lastDepthImageImportParameters = new();
    private double _pointSize = 1.0;
    private double _opacity = 1.0;

    public PointCloudViewerViewModel()
    {
        Logs = new ObservableCollection<string>();

        _openPointCloudCommand = new DelegateCommand(async () => await OpenPointCloudAsync(), () => !IsBusy);
        _reloadCommand = new DelegateCommand(async () => await ReloadPointCloudAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(FilePath));
        _clearCommand = new DelegateCommand(ClearPointCloud, () => !IsBusy && HasLoadedCloud);
        _setScalarFieldCommand = new DelegateCommand(ExecuteSetScalarField, () => !IsBusy && HasLoadedCloud);
        _scalePointCloudCommand = new DelegateCommand(ExecuteScalePointCloud, () => !IsBusy && HasLoadedCloud);
        _editScalarFieldDisplayParamsCommand = new DelegateCommand(
            ExecuteEditScalarFieldDisplayParams,
            () => !IsBusy && HasLoadedCloud && _scalarColorAxis != ScalarColorAxis.None && _scalarFieldParameters is not null);
        _openPointPickingMeasurementDialogCommand = new DelegateCommand(
            ExecuteOpenPointPickingMeasurementDialog,
            () => !IsBusy);
        _resetCameraCommand = new DelegateCommand(() => ResetCameraRequested?.Invoke(this, EventArgs.Empty), () => HasLoadedCloud);
        _setViewOrientationCommand = new DelegateCommand<PointCloudViewOrientation?>(
            ExecuteSetViewOrientation,
            orientation => HasLoadedCloud && orientation.HasValue);
        _setPointPickingMeasurementModeCommand = new DelegateCommand<PointPickingMeasurementMode?>(
            ExecuteSetPointPickingMeasurementMode,
            mode => !IsBusy && mode.HasValue);
        _clearPointPickingMeasurementCommand = new DelegateCommand(
            ExecuteClearPointPickingMeasurement,
            () => !IsBusy);
    }

    public event EventHandler<PointCloudSceneChangedEventArgs>? SceneChanged;

    public event EventHandler? RenderOptionsChanged;

    public event EventHandler? ResetCameraRequested;

    public event EventHandler<PointCloudViewOrientationRequestedEventArgs>? ViewOrientationRequested;

    public event EventHandler<PointPickingMeasurementOverlayEventArgs>? ApplyMeasurementOverlayRequested;

    public ObservableCollection<string> Logs { get; }

    public DelegateCommand OpenPointCloudCommand => _openPointCloudCommand;

    public DelegateCommand ReloadCommand => _reloadCommand;

    public DelegateCommand ClearCommand => _clearCommand;

    public DelegateCommand SetScalarFieldCommand => _setScalarFieldCommand;

    public DelegateCommand ScalePointCloudCommand => _scalePointCloudCommand;

    public DelegateCommand EditScalarFieldDisplayParamsCommand => _editScalarFieldDisplayParamsCommand;

    public DelegateCommand OpenPointPickingMeasurementDialogCommand => _openPointPickingMeasurementDialogCommand;

    public DelegateCommand ResetCameraCommand => _resetCameraCommand;

    public DelegateCommand<PointCloudViewOrientation?> SetViewOrientationCommand => _setViewOrientationCommand;

    public DelegateCommand<PointPickingMeasurementMode?> SetPointPickingMeasurementModeCommand => _setPointPickingMeasurementModeCommand;

    public DelegateCommand ClearPointPickingMeasurementCommand => _clearPointPickingMeasurementCommand;

    public PointCloudHandle? DisplayCloud => _displayCloud;

    public string FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    public string FileExtension
    {
        get => _fileExtension;
        private set => SetProperty(ref _fileExtension, value);
    }

    public string PointCountText
    {
        get => _pointCountText;
        private set => SetProperty(ref _pointCountText, value);
    }

    public string BoundsMinText
    {
        get => _boundsMinText;
        private set => SetProperty(ref _boundsMinText, value);
    }

    public string BoundsMaxText
    {
        get => _boundsMaxText;
        private set => SetProperty(ref _boundsMaxText, value);
    }

    public string BoundsSizeText
    {
        get => _boundsSizeText;
        private set => SetProperty(ref _boundsSizeText, value);
    }

    public string SceneTitle
    {
        get => _sceneTitle;
        private set => SetProperty(ref _sceneTitle, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string OverlayText
    {
        get => _overlayText;
        private set => SetProperty(ref _overlayText, value);
    }

    public string SelectedPointText
    {
        get => _selectedPointText;
        private set => SetProperty(ref _selectedPointText, value);
    }

    public string MeasurementText
    {
        get => _measurementText;
        private set => SetProperty(ref _measurementText, value);
    }

    public string PipelineText
    {
        get => _pipelineText;
        private set => SetProperty(ref _pipelineText, value);
    }

    public string SupportedFileTypesText => SupportedFileExtensions;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool HasLoadedCloud => _displayCloud is not null && !_displayCloud.IsClosed && !_displayCloud.IsInvalid;

    public bool EnableEdl
    {
        get => _enableEdl;
        set
        {
            if (SetProperty(ref _enableEdl, value))
            {
                StatusText = value ? "EDL 已开启" : "EDL 已关闭";
                AppendLog(StatusText);
                OnRenderOptionsChanged();
            }
        }
    }

    public bool ShowOrientationAxes
    {
        get => _showOrientationAxes;
        set
        {
            if (SetProperty(ref _showOrientationAxes, value))
            {
                OnRenderOptionsChanged();
            }
        }
    }

    public bool UseGradientBackground
    {
        get => _useGradientBackground;
        set
        {
            if (SetProperty(ref _useGradientBackground, value))
            {
                OnRenderOptionsChanged();
            }
        }
    }

    public bool IsPivotPickingEnabled
    {
        get => _isPivotPickingEnabled;
        set => SetPivotPickingState(value, logChange: true);
    }

    public PointPickingMeasurementMode CurrentMeasurementMode
    {
        get => _currentMeasurementMode;
        private set
        {
            if (SetProperty(ref _currentMeasurementMode, value))
            {
                RaisePropertyChanged(nameof(IsPointInfoMeasurementMode));
                RaisePropertyChanged(nameof(IsDistanceMeasurementMode));
                RaisePropertyChanged(nameof(IsAngleMeasurementMode));
            }
        }
    }

    public bool IsPointInfoMeasurementMode => CurrentMeasurementMode == PointPickingMeasurementMode.PointInfo;

    public bool IsDistanceMeasurementMode => CurrentMeasurementMode == PointPickingMeasurementMode.Distance;

    public bool IsAngleMeasurementMode => CurrentMeasurementMode == PointPickingMeasurementMode.Angle;

    public string ScalarFieldDisplayText => GetScalarFieldDisplayText(_scalarColorAxis);

    public double PointSize
    {
        get => _pointSize;
        set => SetPointSize(value, notifyRender: true);
    }

    public string PointSizeText => $"{PointSize:F1}px";

    public double Opacity
    {
        get => _opacity;
        set
        {
            double clamped = Math.Clamp(value, 0.15, 1.0);
            if (SetProperty(ref _opacity, clamped))
            {
                RaisePropertyChanged(nameof(OpacityText));
                OnRenderOptionsChanged();
            }
        }
    }

    public string OpacityText => $"{Opacity:P0}";

    public override void InitParam()
    {
        base.InitParam();

        if (Param is string filePath && File.Exists(filePath))
        {
            _ = LoadPointCloudAsync(filePath, resetCamera: true);
        }
    }

    public PointCloudRenderOptions BuildRenderOptions()
    {
        return new PointCloudRenderOptions
        {
            BackgroundTop = Color.FromScRgb(1.0f, 0.8f, 0.8f, 0.8f),
            BackgroundBottom = Color.FromScRgb(1.0f, 0.2f, 0.3f, 0.3f),
            UseGradientBackground = UseGradientBackground,
            PointSize = PointSize,
            Opacity = Opacity,
            SolidPointColor = Colors.White,
            ColorAxis = _scalarColorAxis,
            ShowScalarBar = _scalarColorAxis != ScalarColorAxis.None,
            ScalarTitle = GetScalarTitle(_scalarColorAxis),
            ScalarParameters = _scalarFieldParameters?.Clone(),
            ShowOrientationAxes = ShowOrientationAxes,
            EnableEdl = EnableEdl,
        };
    }

    public void BeginSwitchingDisplay(string? sourceName)
    {
        string displayName = string.IsNullOrWhiteSpace(sourceName) ? "point cloud" : sourceName;
        StatusText = $"Switching point-cloud display: {displayName}";
        OverlayText = "Preparing point-cloud display...";
        AppendLog($"Switching point-cloud display: {displayName}.");
    }

    public void BeginApplyingScene()
    {
        if (!HasLoadedCloud)
        {
            return;
        }

        StatusText = "Applying point-cloud scene";
        OverlayText = "Applying point-cloud scene...";
    }

    public void BeginRendering()
    {
        if (!HasLoadedCloud)
        {
            return;
        }

        StatusText = "Rendering point cloud";
    }

    public void CompleteRendering()
    {
        if (!HasLoadedCloud)
        {
            return;
        }

        StatusText = $"Loaded {PointCountText} points";
        OverlayText = CurrentMeasurementMode == PointPickingMeasurementMode.None
            ? BuildReadyOverlay()
            : MeasurementText;
    }

    public void HandlePointPicked(Point3d point)
    {
        SelectedPointText = $"({point.X:F3}, {point.Y:F3}, {point.Z:F3})";

        if (!_isPivotPickingEnabled && CurrentMeasurementMode != PointPickingMeasurementMode.None)
        {
            HandlePointPickingMeasurement(point);
            return;
        }

        if (_isPivotPickingEnabled)
        {
            SetPivotPickingState(false, logChange: false);
            OverlayText = $"Pivot 已设置到 ({point.X:F3}, {point.Y:F3}, {point.Z:F3})";
            StatusText = "旋转中心已更新";
            AppendLog($"Pivot 已设置到 ({point.X:F3}, {point.Y:F3}, {point.Z:F3})。");
            return;
        }

        OverlayText = $"当前选中点：({point.X:F3}, {point.Y:F3}, {point.Z:F3})";
        StatusText = "已拾取点云中的一个点";
        AppendLog($"拾取点：({point.X:F3}, {point.Y:F3}, {point.Z:F3})。");
    }

    public override void OnDialogClosed()
    {
        CancelPendingLoad();
        DisposeClouds();
        base.OnDialogClosed();
    }

    private async Task OpenPointCloudAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择点云文件",
            Filter = OpenFileFilter,
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await LoadPointCloudAsync(dialog.FileName, resetCamera: true);
    }

    private async Task ReloadPointCloudAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            return;
        }

        await LoadPointCloudAsync(FilePath, resetCamera: true);
    }

    private void ExecuteSetScalarField()
    {
        if (!HasLoadedCloud)
        {
            return;
        }

        var dialog = new SetScalarFieldDialog(_scalarColorAxis)
        {
            Owner = GetActiveWindow(),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ApplyScalarField(dialog.SelectedAxis);
    }

    private void ExecuteOpenPointPickingMeasurementDialog()
    {
        var dialog = new PointPickingMeasurementDialog(CurrentMeasurementMode)
        {
            Owner = GetActiveWindow(),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (dialog.ClearRequested)
        {
            ExecuteClearPointPickingMeasurement();
            return;
        }

        ExecuteSetPointPickingMeasurementMode(dialog.SelectedMode);
    }

    private void ExecuteEditScalarFieldDisplayParams()
    {
        if (!HasLoadedCloud || _displayCloud is null || _scalarColorAxis == ScalarColorAxis.None)
        {
            return;
        }

        RefreshScalarFieldParameters(resetRanges: false);
        if (_scalarFieldParameters is null)
        {
            return;
        }

        var histogram = BuildScalarFieldHistogram(_displayCloud, _scalarColorAxis);
        var dialog = new ScalarFieldDisplayParamsDialog(_scalarColorAxis, _scalarFieldParameters, histogram)
        {
            Owner = GetActiveWindow(),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _scalarFieldParameters = dialog.Parameters.Clone();
        _scalarFieldParameters.Clamp();

        StatusText = "已更新 SF 显示参数";
        AppendLog($"SF 显示参数已更新：{GetScalarTitle(_scalarColorAxis)} 轴。");
        RaiseCommandStates();
        OnRenderOptionsChanged();
    }

    private void ExecuteScalePointCloud()
    {
        if (!HasLoadedCloud || _displayCloud is null || _sourceCloud is null)
        {
            return;
        }

        var dialog = new ScalePointCloudDialog
        {
            Owner = GetActiveWindow(),
        };

        bool? dialogResult = dialog.ShowDialog();
        if (dialogResult != true)
        {
            return;
        }

        if (dialog.Parameters.ResetRequested)
        {
            ResetDisplayCloudToSource();
            return;
        }

        ApplyPointCloudScale(dialog.Parameters);
    }

    private void ApplyScalarField(ScalarColorAxis axis)
    {
        if (_scalarColorAxis == axis && (axis == ScalarColorAxis.None || _scalarFieldParameters is not null))
        {
            return;
        }

        _scalarColorAxis = axis;
        RefreshScalarFieldParameters(resetRanges: true);
        RaisePropertyChanged(nameof(ScalarFieldDisplayText));
        RaiseCommandStates();

        if (axis == ScalarColorAxis.None)
        {
            StatusText = "已关闭标量场";
            AppendLog("已关闭标量场渲染。");
        }
        else
        {
            string title = GetScalarTitle(axis);
            StatusText = $"已应用标量场：{title}";
            AppendLog($"已应用标量场渲染：{title} 轴。");
        }

        OnRenderOptionsChanged();
    }

    private async Task LoadPointCloudAsync(string filePath, bool resetCamera)
    {
        bool isDepthTiff = IsDepthTiffFile(filePath);
        DepthTiffLoadOptions depthTiffLoadOptions = default;
        if (isDepthTiff && !TryGetDepthTiffLoadOptions(out depthTiffLoadOptions))
        {
            return;
        }

        CancelPendingLoad();

        var cts = new CancellationTokenSource();
        _loadCts = cts;

        IsBusy = true;
        StatusText = "正在加载点云...";
        OverlayText = "正在后台读取点云，请稍候。";
        AppendLog($"开始加载：{filePath}");

        PointCloudHandle? loadedSource = null;
        PointCloudHandle? loadedDisplay = null;

        try
        {
            loadedSource = await Task.Run(
                () => isDepthTiff
                    ? PointClouds.LoadDepthTiff(filePath, depthTiffLoadOptions)
                    : PointClouds.Load(filePath),
                cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            loadedDisplay = await Task.Run(() => PointCloudUtilities.Copy(loadedSource), cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            int pointCount = PointClouds.Count(loadedSource);
            PointCloudBounds bounds = PointClouds.GetBounds(loadedSource);

            ApplyLoadedCloud(filePath, loadedSource, loadedDisplay, pointCount, bounds, resetCamera);
            loadedSource = null;
            loadedDisplay = null;
        }
        catch (OperationCanceledException)
        {
            StatusText = "加载已取消";
            OverlayText = HasLoadedCloud ? BuildReadyOverlay() : "加载已取消，可以重新选择点云文件。";
            AppendLog("点云加载已取消。");
        }
        catch (Exception ex)
        {
            StatusText = "加载失败";
            OverlayText = HasLoadedCloud ? BuildReadyOverlay() : "加载失败，请重新选择文件。";
            AppendLog($"加载失败：{ex.Message}");
        }
        finally
        {
            loadedDisplay?.Dispose();
            loadedSource?.Dispose();

            if (ReferenceEquals(_loadCts, cts))
            {
                _loadCts.Dispose();
                _loadCts = null;
            }

            IsBusy = false;
        }
    }

    private bool TryGetDepthTiffLoadOptions(out DepthTiffLoadOptions options)
    {
        options = default;

        var dialog = new DepthImageImportDialog(_lastDepthImageImportParameters)
        {
            Owner = GetActiveWindow(),
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        _lastDepthImageImportParameters = dialog.Parameters.Clone();
        options = _lastDepthImageImportParameters.ToLoadOptions();
        AppendLog(
            $"Depth TIFF parameters: spacing=({_lastDepthImageImportParameters.SpacingX}, {_lastDepthImageImportParameters.SpacingY}, {_lastDepthImageImportParameters.SpacingZ}), invalid={_lastDepthImageImportParameters.InvalidValue}, useInvalid={_lastDepthImageImportParameters.UseInvalidValue}");
        return true;
    }

    private static bool IsDepthTiffFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyLoadedCloud(
        string filePath,
        PointCloudHandle sourceCloud,
        PointCloudHandle displayCloud,
        int pointCount,
        PointCloudBounds bounds,
        bool resetCamera)
    {
        DisposeClouds();

        _sourceCloud = sourceCloud;
        _displayCloud = displayCloud;

        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        string extension = Path.GetExtension(filePath);
        FileExtension = string.IsNullOrWhiteSpace(extension) ? "--" : extension.TrimStart('.').ToUpperInvariant();
        PointCountText = pointCount.ToString("N0");
        BoundsMinText = $"X {bounds.MinX:F3}  Y {bounds.MinY:F3}  Z {bounds.MinZ:F3}";
        BoundsMaxText = $"X {bounds.MaxX:F3}  Y {bounds.MaxY:F3}  Z {bounds.MaxZ:F3}";
        BoundsSizeText = $"dX {bounds.SizeX:F3}  dY {bounds.SizeY:F3}  dZ {bounds.SizeZ:F3}";
        SceneTitle = string.IsNullOrWhiteSpace(FileName) ? DefaultTitle : FileName;
        StatusText = $"已加载 {PointCountText} 个点";
        SelectedPointText = DefaultSelectedPointText;
        ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
        OverlayText = BuildReadyOverlay();

        SetPivotPickingState(false, logChange: false);
        RefreshScalarFieldParameters(resetRanges: true);
        RaisePropertyChanged(nameof(DisplayCloud));
        RaisePropertyChanged(nameof(HasLoadedCloud));
        RaiseCommandStates();

        SceneChanged?.Invoke(this, new PointCloudSceneChangedEventArgs(resetCamera));
        AppendLog($"加载完成：{FileName}，共 {PointCountText} 个点。");
    }

    private void ClearPointCloud()
    {
        CancelPendingLoad();
        DisposeClouds();
        SetPivotPickingState(false, logChange: false);
        _scalarFieldParameters = null;

        FilePath = string.Empty;
        FileName = DefaultFileName;
        FileExtension = "--";
        PointCountText = "0";
        BoundsMinText = "--";
        BoundsMaxText = "--";
        BoundsSizeText = "--";
        SetPointSize(1.0, notifyRender: false);
        SceneTitle = DefaultTitle;
        ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
        StatusText = "已清空";
        SelectedPointText = DefaultSelectedPointText;
        OverlayText = "点云已清空，可以重新加载文件。";

        RaisePropertyChanged(nameof(DisplayCloud));
        RaisePropertyChanged(nameof(HasLoadedCloud));
        RaiseCommandStates();

        SceneChanged?.Invoke(this, new PointCloudSceneChangedEventArgs(false));
        AppendLog("当前点云已清空。");
    }

    private void ApplyPointCloudScale(PointCloudScaleParameters parameters)
    {
        if (_displayCloud is null)
        {
            return;
        }

        parameters.SyncLinkedAxes();

        Point3d pivot = parameters.KeepEntityInPlace
            ? GetBoundsCenter(PointClouds.GetBounds(_displayCloud))
            : new Point3d(0.0, 0.0, 0.0);

        PointCloudHandle scaledCloud = PointCloudUtilities.Scale(
            _displayCloud,
            parameters.ScaleX,
            parameters.ScaleY,
            parameters.ScaleZ,
            pivot);

        UpdateDisplayCloud(
            scaledCloud,
            "Scale applied",
            $"Point-cloud scale applied: X={parameters.ScaleX:F6}, Y={parameters.ScaleY:F6}, Z={parameters.ScaleZ:F6}.");
    }

    private void ResetDisplayCloudToSource()
    {
        if (_sourceCloud is null)
        {
            return;
        }

        PointCloudHandle resetCloud = PointCloudUtilities.Copy(_sourceCloud);
        UpdateDisplayCloud(
            resetCloud,
            "Scale reset",
            "Point cloud restored to the original loaded state.");
    }

    private void UpdateDisplayCloud(PointCloudHandle newDisplayCloud, string statusText, string logText)
    {
        PointCloudHandle? previousDisplay = _displayCloud;
        _displayCloud = newDisplayCloud;

        try
        {
            PointCloudBounds bounds = PointClouds.GetBounds(_displayCloud);
            PointCountText = PointClouds.Count(_displayCloud).ToString("N0");
            BoundsMinText = $"X {bounds.MinX:F3}  Y {bounds.MinY:F3}  Z {bounds.MinZ:F3}";
            BoundsMaxText = $"X {bounds.MaxX:F3}  Y {bounds.MaxY:F3}  Z {bounds.MaxZ:F3}";
            BoundsSizeText = $"dX {bounds.SizeX:F3}  dY {bounds.SizeY:F3}  dZ {bounds.SizeZ:F3}";
            SelectedPointText = DefaultSelectedPointText;
            ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
            OverlayText = BuildReadyOverlay();
            StatusText = statusText;
            RefreshScalarFieldParameters(resetRanges: true);
            RaisePropertyChanged(nameof(DisplayCloud));
            RaisePropertyChanged(nameof(HasLoadedCloud));
            RaiseCommandStates();
            SceneChanged?.Invoke(this, new PointCloudSceneChangedEventArgs(false));
            AppendLog(logText);

            previousDisplay?.Dispose();
        }
        catch
        {
            _displayCloud = previousDisplay;
            newDisplayCloud.Dispose();
            throw;
        }
    }

    private void DisposeClouds()
    {
        _displayCloud?.Dispose();
        _displayCloud = null;

        _sourceCloud?.Dispose();
        _sourceCloud = null;
    }

    private void CancelPendingLoad()
    {
        if (_loadCts is null)
        {
            return;
        }

        _loadCts.Cancel();
        _loadCts.Dispose();
        _loadCts = null;
    }

    private void ExecuteSetPointPickingMeasurementMode(PointPickingMeasurementMode? mode)
    {
        if (!mode.HasValue)
        {
            return;
        }

        PointPickingMeasurementMode targetMode = mode.Value;
        if (targetMode == PointPickingMeasurementMode.None)
        {
            ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
            OverlayText = HasLoadedCloud ? BuildReadyOverlay() : DefaultReadyText;
            StatusText = "选点测量已关闭";
            AppendLog("选点测量已关闭。");
            return;
        }

        if (!HasLoadedCloud)
        {
            ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
            MeasurementText = "请先加载点云，再启用选点测量。";
            OverlayText = MeasurementText;
            StatusText = "尚未加载点云";
            AppendLog("选点测量需要先加载点云。");
            return;
        }

        SetPivotPickingState(false, logChange: false);
        CurrentMeasurementMode = targetMode;
        _pointPickingMeasurementState.SetMode(targetMode);
        MeasurementText = GetMeasurementPrompt(targetMode);
        OverlayText = MeasurementText;
        StatusText = $"选点测量: {GetMeasurementModeDisplayName(targetMode)}";
        RequestMeasurementOverlay();
        AppendLog($"选点测量模式切换为 {GetMeasurementModeDisplayName(targetMode)}。");
    }

    private void ExecuteClearPointPickingMeasurement()
    {
        _pointPickingMeasurementState.Clear();
        MeasurementText = CurrentMeasurementMode == PointPickingMeasurementMode.None
            ? DefaultMeasurementText
            : GetMeasurementPrompt(CurrentMeasurementMode);
        OverlayText = HasLoadedCloud ? MeasurementText : DefaultReadyText;
        StatusText = "选点测量结果已清除";
        RequestMeasurementOverlay();
        AppendLog("选点测量结果已清除。");
    }

    private void HandlePointPickingMeasurement(Point3d point)
    {
        if (!_pointPickingMeasurementState.AddPickedPoint(point))
        {
            StatusText = "选点测量未更新";
            AppendLog("选点测量忽略了无效点。");
            return;
        }

        MeasurementText = BuildMeasurementText();
        OverlayText = MeasurementText;
        StatusText = $"选点测量: {GetMeasurementModeDisplayName(CurrentMeasurementMode)}";
        RequestMeasurementOverlay();
        AppendLog($"选点测量拾取点: ({point.X:F3}, {point.Y:F3}, {point.Z:F3})。");
    }

    private void ResetPointPickingMeasurement(bool resetMode, bool requestOverlay)
    {
        _pointPickingMeasurementState.Clear();
        if (resetMode)
        {
            _pointPickingMeasurementState.SetMode(PointPickingMeasurementMode.None);
            CurrentMeasurementMode = PointPickingMeasurementMode.None;
        }
        else
        {
            _pointPickingMeasurementState.SetMode(CurrentMeasurementMode);
        }

        MeasurementText = resetMode ? DefaultMeasurementText : GetMeasurementPrompt(CurrentMeasurementMode);

        if (requestOverlay)
        {
            RequestMeasurementOverlay();
        }
    }

    private void RequestMeasurementOverlay()
    {
        ApplyMeasurementOverlayRequested?.Invoke(
            this,
            new PointPickingMeasurementOverlayEventArgs(
                new PointPickingMeasurementOverlay(_pointPickingMeasurementState.Points)));
    }

    private string BuildMeasurementText()
    {
        if (CurrentMeasurementMode == PointPickingMeasurementMode.None)
        {
            return DefaultMeasurementText;
        }

        string result = _pointPickingMeasurementState.Result.Body;
        int maxPointCount = GetMeasurementMaxPointCount(CurrentMeasurementMode);
        int pickedPointCount = _pointPickingMeasurementState.Points.Count;
        if (pickedPointCount <= 0 || string.IsNullOrWhiteSpace(result))
        {
            return GetMeasurementPrompt(CurrentMeasurementMode);
        }

        if (pickedPointCount < maxPointCount)
        {
            return string.Join(
                Environment.NewLine,
                result,
                $"继续选取第 {pickedPointCount + 1}/{maxPointCount} 个点。");
        }

        return result;
    }

    private static string GetMeasurementPrompt(PointPickingMeasurementMode mode)
    {
        return mode switch
        {
            PointPickingMeasurementMode.PointInfo => "单点测量：点击点云获取点坐标。",
            PointPickingMeasurementMode.Distance => "两点测量：依次点击两个点获取距离和分量差。",
            PointPickingMeasurementMode.Angle => "三点测量：依次点击三个点获取面积、边长和角度。",
            _ => DefaultMeasurementText,
        };
    }

    private static string GetMeasurementModeDisplayName(PointPickingMeasurementMode mode)
    {
        return mode switch
        {
            PointPickingMeasurementMode.PointInfo => "单点",
            PointPickingMeasurementMode.Distance => "两点",
            PointPickingMeasurementMode.Angle => "三点",
            _ => "关闭",
        };
    }

    private static int GetMeasurementMaxPointCount(PointPickingMeasurementMode mode)
    {
        return mode switch
        {
            PointPickingMeasurementMode.PointInfo => 1,
            PointPickingMeasurementMode.Distance => 2,
            PointPickingMeasurementMode.Angle => 3,
            _ => 0,
        };
    }

    private void SetPivotPickingState(bool value, bool logChange)
    {
        if (value && !HasLoadedCloud)
        {
            OverlayText = "请先加载点云，再使用 Set Pivot。";
            StatusText = "尚未加载点云";
            if (logChange)
            {
                AppendLog("Set Pivot 需要先加载点云。");
            }

            if (_isPivotPickingEnabled)
            {
                _isPivotPickingEnabled = false;
                RaisePropertyChanged(nameof(IsPivotPickingEnabled));
            }

            return;
        }

        if (_isPivotPickingEnabled == value)
        {
            return;
        }

        _isPivotPickingEnabled = value;
        RaisePropertyChanged(nameof(IsPivotPickingEnabled));

        if (value)
        {
            ResetPointPickingMeasurement(resetMode: true, requestOverlay: true);
            OverlayText = "Set Pivot 已开启，点击点云中的目标点来设置旋转中心。";
            StatusText = "等待拾取旋转中心";
            if (logChange)
            {
                AppendLog("Set Pivot 已开启，点击点云中的点以设置旋转中心。");
            }

            return;
        }

        if (HasLoadedCloud)
        {
            OverlayText = BuildReadyOverlay();
        }

        if (logChange)
        {
            StatusText = "Set Pivot 已关闭";
            AppendLog("Set Pivot 已关闭。");
        }
    }

    private void OnRenderOptionsChanged()
    {
        if (!HasLoadedCloud)
        {
            return;
        }

        RenderOptionsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetPointSize(double value, bool notifyRender)
    {
        double clamped = Math.Clamp(value, 1.0, 20.0);
        if (SetProperty(ref _pointSize, clamped))
        {
            RaisePropertyChanged(nameof(PointSizeText));
            if (notifyRender)
            {
                OnRenderOptionsChanged();
            }
        }
    }

    private void RaiseCommandStates()
    {
        _openPointCloudCommand.RaiseCanExecuteChanged();
        _reloadCommand.RaiseCanExecuteChanged();
        _clearCommand.RaiseCanExecuteChanged();
        _setScalarFieldCommand.RaiseCanExecuteChanged();
        _scalePointCloudCommand.RaiseCanExecuteChanged();
        _editScalarFieldDisplayParamsCommand.RaiseCanExecuteChanged();
        _openPointPickingMeasurementDialogCommand.RaiseCanExecuteChanged();
        _resetCameraCommand.RaiseCanExecuteChanged();
        _setViewOrientationCommand.RaiseCanExecuteChanged();
        _setPointPickingMeasurementModeCommand.RaiseCanExecuteChanged();
        _clearPointPickingMeasurementCommand.RaiseCanExecuteChanged();
    }

    private void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Add(line);

        while (Logs.Count > MaxLogEntries)
        {
            Logs.RemoveAt(0);
        }
    }

    private void ExecuteSetViewOrientation(PointCloudViewOrientation? orientation)
    {
        if (!orientation.HasValue)
        {
            return;
        }

        PointCloudViewOrientation viewOrientation = orientation.Value;
        string viewName = GetViewOrientationDisplayName(viewOrientation);
        StatusText = $"已切换到{viewName}";
        AppendLog($"视角切换到{viewName}。");
        ViewOrientationRequested?.Invoke(this, new PointCloudViewOrientationRequestedEventArgs(viewOrientation));
    }

    private void RefreshScalarFieldParameters(bool resetRanges)
    {
        if (!HasLoadedCloud || _displayCloud is null || _scalarColorAxis == ScalarColorAxis.None)
        {
            _scalarFieldParameters = null;
            RaiseCommandStates();
            return;
        }

        if (!TryGetScalarFieldStatistics(_displayCloud, _scalarColorAxis, out double minValue, out double maxValue))
        {
            _scalarFieldParameters = ScalarFieldRenderParameters.CreateDefault(0.0, 1.0);
            RaiseCommandStates();
            return;
        }

        if (_scalarFieldParameters is null || resetRanges)
        {
            _scalarFieldParameters = ScalarFieldRenderParameters.CreateDefault(minValue, maxValue);
        }
        else
        {
            _scalarFieldParameters.UpdateDataBounds(minValue, maxValue, resetRanges: false);
        }

        _scalarFieldParameters.Clamp();
        RaiseCommandStates();
    }

    private static bool TryGetScalarFieldStatistics(
        PointCloudHandle pointCloud,
        ScalarColorAxis axis,
        out double minValue,
        out double maxValue)
    {
        minValue = 0.0;
        maxValue = 1.0;

        int axisIndex = GetAxisIndex(axis);
        if (axisIndex < 0)
        {
            return false;
        }

        float[] interleaved = PointClouds.ToInterleavedArray(pointCloud);
        if (interleaved.Length < 3)
        {
            return false;
        }

        bool hasFiniteValue = false;
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

            if (value < minValue)
            {
                minValue = value;
            }

            if (value > maxValue)
            {
                maxValue = value;
            }
        }

        if (!hasFiniteValue)
        {
            return false;
        }

        return true;
    }

    private static ScalarFieldHistogramData BuildScalarFieldHistogram(PointCloudHandle pointCloud, ScalarColorAxis axis)
    {
        int axisIndex = GetAxisIndex(axis);
        if (axisIndex < 0)
        {
            return new ScalarFieldHistogramData(Array.Empty<int>(), 0.0, 1.0);
        }

        float[] interleaved = PointClouds.ToInterleavedArray(pointCloud);
        if (interleaved.Length < 3)
        {
            return new ScalarFieldHistogramData(Array.Empty<int>(), 0.0, 1.0);
        }

        var values = new List<float>(interleaved.Length / 3);
        for (int i = axisIndex; i < interleaved.Length; i += 3)
        {
            float value = interleaved[i];
            if (float.IsFinite(value))
            {
                values.Add(value);
            }
        }

        if (values.Count == 0)
        {
            return new ScalarFieldHistogramData(Array.Empty<int>(), 0.0, 1.0);
        }

        double minimum = values.Min();
        double maximum = values.Max();

        if (maximum <= minimum + 1e-12)
        {
            return new ScalarFieldHistogramData(new[] { values.Count }, minimum, maximum);
        }

        int binCount = Math.Clamp((int)Math.Ceiling(Math.Sqrt(values.Count)), 4, 512);
        int[] bins = new int[binCount];
        double scale = binCount / (maximum - minimum);

        foreach (float value in values)
        {
            int index = (int)((value - minimum) * scale);
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= binCount)
            {
                index = binCount - 1;
            }

            bins[index]++;
        }

        return new ScalarFieldHistogramData(bins, minimum, maximum);
    }

    private static string GetViewOrientationDisplayName(PointCloudViewOrientation orientation)
    {
        return orientation switch
        {
            PointCloudViewOrientation.Front => "前视图",
            PointCloudViewOrientation.Back => "后视图",
            PointCloudViewOrientation.Left => "左视图",
            PointCloudViewOrientation.Right => "右视图",
            PointCloudViewOrientation.Top => "俯视图",
            PointCloudViewOrientation.Bottom => "仰视图",
            _ => "标准视图",
        };
    }

    private static int GetAxisIndex(ScalarColorAxis axis)
    {
        return axis switch
        {
            ScalarColorAxis.X => 0,
            ScalarColorAxis.Y => 1,
            ScalarColorAxis.Z => 2,
            _ => -1,
        };
    }

    private static string GetScalarTitle(ScalarColorAxis axis)
    {
        return axis switch
        {
            ScalarColorAxis.X => "X",
            ScalarColorAxis.Y => "Y",
            ScalarColorAxis.Z => "Z",
            _ => string.Empty,
        };
    }

    private static string GetScalarFieldDisplayText(ScalarColorAxis axis)
    {
        return axis switch
        {
            ScalarColorAxis.X => "X 坐标",
            ScalarColorAxis.Y => "Y 坐标",
            ScalarColorAxis.Z => "Z 坐标",
            _ => "未设置",
        };
    }

    private static Window? GetActiveWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
            ?? Application.Current?.MainWindow;
    }

    private static Point3d GetBoundsCenter(PointCloudBounds bounds)
    {
        return new Point3d(
            (bounds.MinX + bounds.MaxX) * 0.5,
            (bounds.MinY + bounds.MaxY) * 0.5,
            (bounds.MinZ + bounds.MaxZ) * 0.5);
    }

    private string BuildReadyOverlay()
    {
        if (!HasLoadedCloud)
        {
            return DefaultReadyText;
        }

        return $"当前文件：{FileName}，点数：{PointCountText}。";
    }
}

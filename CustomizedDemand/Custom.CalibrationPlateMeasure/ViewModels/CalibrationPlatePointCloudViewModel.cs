using PointCloud.Algorithms.Services;
using PointCloud.Interop;
using PointCloud.VTKWPF.Models;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.IO;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Custom.CalibrationPlateMeasure.ViewModels
{
    public sealed class CalibrationPlatePointCloudSceneChangedEventArgs : EventArgs
    {
        public CalibrationPlatePointCloudSceneChangedEventArgs(bool resetCamera)
        {
            ResetCamera = resetCamera;
        }

        public bool ResetCamera { get; }
    }

    public sealed class CalibrationPlatePointCloudViewOrientationRequestedEventArgs : EventArgs
    {
        public CalibrationPlatePointCloudViewOrientationRequestedEventArgs(PointCloudViewOrientation orientation)
        {
            Orientation = orientation;
        }

        public PointCloudViewOrientation Orientation { get; }
    }

    public sealed class CalibrationPlatePointCloudViewModel : BindableBase
    {
        private readonly DelegateCommand _openPointCloudCommand;
        private readonly DelegateCommand _clearCommand;
        private readonly DelegateCommand _setScalarFieldCommand;
        private readonly DelegateCommand _scalePointCloudCommand;
        private readonly DelegateCommand _editScalarFieldDisplayParamsCommand;
        private readonly DelegateCommand<PointCloudViewOrientation?> _setViewOrientationCommand;

        private CancellationTokenSource? _loadCts;
        private PointCloudHandle? _sourceCloud;
        private PointCloudHandle? _displayCloud;
        private string _filePath = string.Empty;
        private string _selectedPointText = "尚未拾取点";
        private string _boundsMinText = "--";
        private string _boundsMaxText = "--";
        private string _boundsSizeText = "--";
        private string _statusText = "未加载";
        private bool _isBusy;
        private bool _enableEdl;
        private bool _showOrientationAxes = true;
        private bool _useGradientBackground = true;
        private bool _isPivotPickingEnabled;
        private ScalarColorAxis _scalarColorAxis = ScalarColorAxis.None;
        private double _pointSize = 1.0;
        private double _opacity = 1.0;

        public CalibrationPlatePointCloudViewModel()
        {
            Logs = [];
            _openPointCloudCommand = new DelegateCommand(async () => await OpenPointCloudAsync(), () => !IsBusy);
            _clearCommand = new DelegateCommand(ClearPointCloud, () => !IsBusy && HasLoadedCloud);
            _setScalarFieldCommand = new DelegateCommand(ToggleScalarField, () => !IsBusy && HasLoadedCloud);
            _scalePointCloudCommand = new DelegateCommand(() => { }, () => false);
            _editScalarFieldDisplayParamsCommand = new DelegateCommand(() => { }, () => false);
            _setViewOrientationCommand = new DelegateCommand<PointCloudViewOrientation?>(
                orientation =>
                {
                    if (orientation.HasValue)
                    {
                        ViewOrientationRequested?.Invoke(this, new CalibrationPlatePointCloudViewOrientationRequestedEventArgs(orientation.Value));
                    }
                },
                orientation => HasLoadedCloud && orientation.HasValue);
        }

        public event EventHandler<CalibrationPlatePointCloudSceneChangedEventArgs>? SceneChanged;

        public event EventHandler? RenderOptionsChanged;

        public event EventHandler<CalibrationPlatePointCloudViewOrientationRequestedEventArgs>? ViewOrientationRequested;

        public ObservableCollection<string> Logs { get; }

        public DelegateCommand OpenPointCloudCommand => _openPointCloudCommand;

        public DelegateCommand ClearCommand => _clearCommand;

        public DelegateCommand SetScalarFieldCommand => _setScalarFieldCommand;

        public DelegateCommand ScalePointCloudCommand => _scalePointCloudCommand;

        public DelegateCommand EditScalarFieldDisplayParamsCommand => _editScalarFieldDisplayParamsCommand;

        public DelegateCommand<PointCloudViewOrientation?> SetViewOrientationCommand => _setViewOrientationCommand;

        public PointCloudHandle? DisplayCloud => _displayCloud;

        public string FilePath
        {
            get => _filePath;
            private set => SetProperty(ref _filePath, value);
        }

        public string SelectedPointText
        {
            get => _selectedPointText;
            private set => SetProperty(ref _selectedPointText, value);
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

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

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
            set => SetProperty(ref _isPivotPickingEnabled, value);
        }

        public string ScalarFieldDisplayText => _scalarColorAxis switch
        {
            ScalarColorAxis.X => "X 坐标",
            ScalarColorAxis.Y => "Y 坐标",
            ScalarColorAxis.Z => "Z 坐标",
            _ => "未设置",
        };

        public double PointSize
        {
            get => _pointSize;
            set
            {
                double clamped = Math.Clamp(value, 1.0, 20.0);
                if (SetProperty(ref _pointSize, clamped))
                {
                    RaisePropertyChanged(nameof(PointSizeText));
                    OnRenderOptionsChanged();
                }
            }
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

        public async Task LoadDepthTiffAsync(string filePath, DepthTiffLoadOptions options, bool resetCamera)
        {
            await LoadPointCloudAsync(filePath, resetCamera, () => PointClouds.LoadDepthTiff(filePath, options));
        }

        public PointCloudRenderOptions BuildRenderOptions()
        {
            return new PointCloudRenderOptions
            {
                BackgroundTop = WpfColor.FromScRgb(1.0f, 0.8f, 0.8f, 0.8f),
                BackgroundBottom = WpfColor.FromScRgb(1.0f, 0.2f, 0.3f, 0.3f),
                UseGradientBackground = UseGradientBackground,
                PointSize = PointSize,
                Opacity = Opacity,
                SolidPointColor = WpfColors.White,
                ColorAxis = _scalarColorAxis,
                ShowScalarBar = _scalarColorAxis != ScalarColorAxis.None,
                ScalarTitle = _scalarColorAxis == ScalarColorAxis.None ? string.Empty : _scalarColorAxis.ToString(),
                ShowOrientationAxes = ShowOrientationAxes,
                EnableEdl = EnableEdl,
            };
        }

        public void BeginSwitchingDisplay(string? sourceName)
        {
            string displayName = string.IsNullOrWhiteSpace(sourceName) ? "point cloud" : sourceName;
            StatusText = $"加载中：{displayName}";
            AppendLog($"开始加载点云：{displayName}");
        }

        public void BeginApplyingScene()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        public void BeginRendering()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        public void CompleteRendering()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        public void HandlePointPicked(Point3d point)
        {
            SelectedPointText = $"({point.X:F3}, {point.Y:F3}, {point.Z:F3})";
            if (IsPivotPickingEnabled)
            {
                IsPivotPickingEnabled = false;
            }
        }

        public void ClearPointCloud()
        {
            CancelPendingLoad();
            DisposeClouds();
            FilePath = string.Empty;
            BoundsMinText = "--";
            BoundsMaxText = "--";
            BoundsSizeText = "--";
            SelectedPointText = "尚未拾取点";
            StatusText = "未加载";
            RaiseLoadedCloudChanged();
            RaiseCommandStates();
            SceneChanged?.Invoke(this, new CalibrationPlatePointCloudSceneChangedEventArgs(resetCamera: false));
        }

        public void OnDialogClosed()
        {
            CancelPendingLoad();
            DisposeClouds();
        }

        private async Task OpenPointCloudAsync()
        {
            var dialog = new WpfOpenFileDialog
            {
                Title = "选择点云文件",
                Filter = "点云/深度图文件|*.ply;*.pcd;*.obj;*.txt;*.xyz;*.asc;*.csv;*.pts;*.stl;*.tif;*.tiff|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = dialog.FileName;
            if (IsDepthTiffFile(filePath))
            {
                await LoadDepthTiffAsync(filePath, new DepthTiffLoadOptions(1.0, 1.0, 1.0, 0.0, false), resetCamera: true);
                return;
            }

            await LoadPointCloudAsync(filePath, resetCamera: true, () => PointClouds.Load(filePath));
        }

        private async Task LoadPointCloudAsync(string filePath, bool resetCamera, Func<PointCloudHandle> loader)
        {
            CancelPendingLoad();

            var cts = new CancellationTokenSource();
            _loadCts = cts;

            IsBusy = true;
            StatusText = "加载中";

            PointCloudHandle? loadedSource = null;
            PointCloudHandle? loadedDisplay = null;

            try
            {
                loadedSource = await Task.Run(loader, cts.Token);
                cts.Token.ThrowIfCancellationRequested();

                loadedDisplay = await Task.Run(() => PointCloudUtilities.Copy(loadedSource), cts.Token);
                cts.Token.ThrowIfCancellationRequested();

                int pointCount = PointClouds.Count(loadedSource);
                PointCloudBounds bounds = PointClouds.GetBounds(loadedSource);
                ApplyLoadedCloud(filePath, loadedSource, loadedDisplay, pointCount, bounds, resetCamera);
                loadedSource = null;
                loadedDisplay = null;
            }
            catch
            {
                StatusText = "加载失败";
                throw;
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
            BoundsMinText = $"X {bounds.MinX:F3}  Y {bounds.MinY:F3}  Z {bounds.MinZ:F3}";
            BoundsMaxText = $"X {bounds.MaxX:F3}  Y {bounds.MaxY:F3}  Z {bounds.MaxZ:F3}";
            BoundsSizeText = $"dX {bounds.SizeX:F3}  dY {bounds.SizeY:F3}  dZ {bounds.SizeZ:F3}";
            SelectedPointText = "尚未拾取点";
            StatusText = "加载完成";

            RaiseLoadedCloudChanged();
            RaiseCommandStates();
            SceneChanged?.Invoke(this, new CalibrationPlatePointCloudSceneChangedEventArgs(resetCamera));
            AppendLog($"加载完成：{Path.GetFileName(filePath)}，点数 {pointCount:N0}");
        }

        private void ToggleScalarField()
        {
            _scalarColorAxis = _scalarColorAxis == ScalarColorAxis.None ? ScalarColorAxis.Z : ScalarColorAxis.None;
            RaisePropertyChanged(nameof(ScalarFieldDisplayText));
            OnRenderOptionsChanged();
        }

        private void OnRenderOptionsChanged()
        {
            if (HasLoadedCloud)
            {
                RenderOptionsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RaiseLoadedCloudChanged()
        {
            RaisePropertyChanged(nameof(DisplayCloud));
            RaisePropertyChanged(nameof(HasLoadedCloud));
        }

        private void RaiseCommandStates()
        {
            _openPointCloudCommand.RaiseCanExecuteChanged();
            _clearCommand.RaiseCanExecuteChanged();
            _setScalarFieldCommand.RaiseCanExecuteChanged();
            _scalePointCloudCommand.RaiseCanExecuteChanged();
            _editScalarFieldDisplayParamsCommand.RaiseCanExecuteChanged();
            _setViewOrientationCommand.RaiseCanExecuteChanged();
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

        private void DisposeClouds()
        {
            _displayCloud?.Dispose();
            _displayCloud = null;
            _sourceCloud?.Dispose();
            _sourceCloud = null;
        }

        private void AppendLog(string message)
        {
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            while (Logs.Count > 200)
            {
                Logs.RemoveAt(0);
            }
        }

        private static bool IsDepthTiffFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
        }
    }
}

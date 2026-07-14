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

namespace ReeYin.Customized.Algo.ViewModels
{
    public sealed class HeightDifferencePointCloudSceneChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建点云场景变更事件参数，并携带是否复位相机的标记。
        /// </summary>
        public HeightDifferencePointCloudSceneChangedEventArgs(bool resetCamera)
        {
            ResetCamera = resetCamera;
        }

        /// <summary>
        /// 请求点云窗口复位相机视角的事件。
        /// </summary>
        public bool ResetCamera { get; }
    }

    public sealed class HeightDifferencePointCloudViewOrientationRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建点云标准视角切换事件参数。
        /// </summary>
        public HeightDifferencePointCloudViewOrientationRequestedEventArgs(PointCloudViewOrientation orientation)
        {
            Orientation = orientation;
        }

        /// <summary>
        /// 点云窗口请求切换到的标准观察方向。
        /// </summary>
        public PointCloudViewOrientation Orientation { get; }
    }

    public sealed class HeightDifferencePointCloudViewModel : BindableBase
    {
        // 打开点云文件命令的延迟创建实例。
        private readonly DelegateCommand _openPointCloudCommand;
        // 清空点云命令的延迟创建实例。
        private readonly DelegateCommand _clearCommand;
        // 切换标量字段显示命令的延迟创建实例。
        private readonly DelegateCommand _setScalarFieldCommand;
        // 点云缩放命令的延迟创建实例。
        private readonly DelegateCommand _scalePointCloudCommand;
        // 标量字段显示参数编辑命令的延迟创建实例。
        private readonly DelegateCommand _editScalarFieldDisplayParamsCommand;
        // 点云相机复位命令的延迟创建实例。
        private readonly DelegateCommand _resetCameraCommand;
        // 点云标准视角切换命令的延迟创建实例。
        private readonly DelegateCommand<PointCloudViewOrientation?> _setViewOrientationCommand;

        // 点云异步加载任务的取消令牌源。
        private CancellationTokenSource? _loadCts;
        // 从文件加载的原始点云句柄。
        private PointCloudHandle? _sourceCloud;
        // 应用缩放或显示参数后的点云句柄。
        private PointCloudHandle? _displayCloud;
        // 当前点云文件路径。
        private string _filePath = string.Empty;
        // 点云拾取点坐标的显示文本。
        private string _selectedPointText = "尚未拾取点";
        // 点云包围盒最小坐标的显示文本。
        private string _boundsMinText = "--";
        // 点云包围盒最大坐标的显示文本。
        private string _boundsMaxText = "--";
        // 点云包围盒尺寸的显示文本。
        private string _boundsSizeText = "--";
        // 点云视图当前状态栏文本。
        private string _statusText = "未加载";
        // 标记点云加载或渲染是否正在执行。
        private bool _isBusy;
        // 点云渲染是否启用 EDL 深度增强。
        private bool _enableEdl;
        // 点云视图是否显示方向坐标轴。
        private bool _showOrientationAxes = true;
        // 点云视图是否使用渐变背景。
        private bool _useGradientBackground = true;
        // 点云视图是否启用旋转中心拾取。
        private bool _isPivotPickingEnabled;
        // 点云标量字段颜色轴是否显示。
        private ScalarColorAxis _scalarColorAxis = ScalarColorAxis.None;
        // 点云渲染点大小。
        private double _pointSize = 1.0;
        // 点云渲染透明度。
        private double _opacity = 1.0;

        /// <summary>
        /// 初始化点云工具栏命令、默认渲染参数和日志集合。
        /// </summary>
        public HeightDifferencePointCloudViewModel()
        {
            Logs = [];
            _openPointCloudCommand = new DelegateCommand(async () => await OpenPointCloudAsync(), () => !IsBusy);
            _clearCommand = new DelegateCommand(ClearPointCloud, () => !IsBusy && HasLoadedCloud);
            _setScalarFieldCommand = new DelegateCommand(ToggleScalarField, () => !IsBusy && HasLoadedCloud);
            _scalePointCloudCommand = new DelegateCommand(
                () => ResetCameraRequested?.Invoke(this, EventArgs.Empty),
                () => HasLoadedCloud);
            _editScalarFieldDisplayParamsCommand = new DelegateCommand(() => { }, () => false);
            _resetCameraCommand = new DelegateCommand(
                () => ResetCameraRequested?.Invoke(this, EventArgs.Empty),
                () => HasLoadedCloud);
            _setViewOrientationCommand = new DelegateCommand<PointCloudViewOrientation?>(
                orientation =>
                {
                    if (orientation.HasValue)
                    {
                        ViewOrientationRequested?.Invoke(this, new HeightDifferencePointCloudViewOrientationRequestedEventArgs(orientation.Value));
                    }
                },
                orientation => HasLoadedCloud && orientation.HasValue);
        }

        public event EventHandler<HeightDifferencePointCloudSceneChangedEventArgs>? SceneChanged;

        public event EventHandler? RenderOptionsChanged;

        public event EventHandler? ResetCameraRequested;

        public event EventHandler<HeightDifferencePointCloudViewOrientationRequestedEventArgs>? ViewOrientationRequested;

        /// <summary>
        /// 点云加载、显示和交互过程的执行日志。
        /// </summary>
        public ObservableCollection<string> Logs { get; }

        /// <summary>
        /// 打开点云文件选择对话框并加载点云的命令。
        /// </summary>
        public DelegateCommand OpenPointCloudCommand => _openPointCloudCommand;

        /// <summary>
        /// 清空当前点云和显示状态的命令。
        /// </summary>
        public DelegateCommand ClearCommand => _clearCommand;

        /// <summary>
        /// 切换点云按标量字段着色显示的命令。
        /// </summary>
        public DelegateCommand SetScalarFieldCommand => _setScalarFieldCommand;

        /// <summary>
        /// 按当前标定间距缩放点云坐标的命令。
        /// </summary>
        public DelegateCommand ScalePointCloudCommand => _scalePointCloudCommand;

        /// <summary>
        /// 打开标量字段显示参数编辑窗口的命令。
        /// </summary>
        public DelegateCommand EditScalarFieldDisplayParamsCommand => _editScalarFieldDisplayParamsCommand;

        /// <summary>
        /// 触发点云视图相机复位的命令。
        /// </summary>
        public DelegateCommand ResetCameraCommand => _resetCameraCommand;

        /// <summary>
        /// 切换点云标准视角方向的命令。
        /// </summary>
        public DelegateCommand<PointCloudViewOrientation?> SetViewOrientationCommand => _setViewOrientationCommand;

        /// <summary>
        /// 当前用于渲染显示的点云句柄。
        /// </summary>
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

        /// <summary>
        /// 指示当前是否已有可显示的点云数据。
        /// </summary>
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

        /// <summary>
        /// 点云标量字段显示开关的界面文本。
        /// </summary>
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

        /// <summary>
        /// 点云渲染点大小的界面显示文本。
        /// </summary>
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

        /// <summary>
        /// 点云整体透明度的界面显示文本。
        /// </summary>
        public string OpacityText => $"{Opacity:P0}";

        /// <summary>
        /// 异步加载深度 TIFF 并转换为点云显示数据。
        /// </summary>
        public async Task LoadDepthTiffAsync(string filePath, DepthTiffLoadOptions options, bool resetCamera)
        {
            await LoadPointCloudAsync(filePath, resetCamera, () => PointClouds.LoadDepthTiff(filePath, options));
        }

        /// <summary>
        /// 异步加载指定点云文件并应用当前显示参数。
        /// </summary>
        public async Task LoadPointCloudFileAsync(string filePath, bool resetCamera)
        {
            await LoadPointCloudAsync(filePath, resetCamera, () => PointClouds.Load(filePath));
        }

        /// <summary>
        /// 根据界面参数构建点云渲染选项。
        /// </summary>
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

        /// <summary>
        /// 进入点云显示模式切换的忙碌状态。
        /// </summary>
        public void BeginSwitchingDisplay(string? sourceName)
        {
            string displayName = string.IsNullOrWhiteSpace(sourceName) ? "point cloud" : sourceName;
            StatusText = $"加载中：{displayName}";
            AppendLog($"开始加载点云：{displayName}");
        }

        /// <summary>
        /// 进入点云场景应用阶段并提示界面等待。
        /// </summary>
        public void BeginApplyingScene()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        /// <summary>
        /// 进入点云渲染阶段并更新状态文本。
        /// </summary>
        public void BeginRendering()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        /// <summary>
        /// 结束点云渲染忙碌状态并刷新命令可用性。
        /// </summary>
        public void CompleteRendering()
        {
            if (HasLoadedCloud)
            {
                StatusText = "加载完成";
            }
        }

        /// <summary>
        /// 接收点云拾取结果并显示选中点坐标。
        /// </summary>
        public void HandlePointPicked(Point3d point)
        {
            SelectedPointText = $"({point.X:F3}, {point.Y:F3}, {point.Z:F3})";
            if (IsPivotPickingEnabled)
            {
                IsPivotPickingEnabled = false;
            }
        }

        /// <summary>
        /// 清空当前点云句柄、文件路径和包围盒信息。
        /// </summary>
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
            SceneChanged?.Invoke(this, new HeightDifferencePointCloudSceneChangedEventArgs(resetCamera: false));
        }

        /// <summary>
        /// 对话框关闭时释放点云或测量视图相关资源。
        /// </summary>
        public void OnDialogClosed()
        {
            CancelPendingLoad();
            DisposeClouds();
        }

        /// <summary>
        /// 弹出文件选择窗口并加载用户选择的点云。
        /// </summary>
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

        /// <summary>
        /// 后台读取点云文件并切换到显示场景。
        /// </summary>
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

        /// <summary>
        /// 将加载完成的点云句柄应用到界面显示状态。
        /// </summary>
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
            SceneChanged?.Invoke(this, new HeightDifferencePointCloudSceneChangedEventArgs(resetCamera));
            AppendLog($"加载完成：{Path.GetFileName(filePath)}，点数 {pointCount:N0}");
        }

        /// <summary>
        /// 切换点云按标量字段着色或原色显示。
        /// </summary>
        private void ToggleScalarField()
        {
            _scalarColorAxis = _scalarColorAxis == ScalarColorAxis.None ? ScalarColorAxis.Z : ScalarColorAxis.None;
            RaisePropertyChanged(nameof(ScalarFieldDisplayText));
            OnRenderOptionsChanged();
        }

        /// <summary>
        /// 点云渲染参数变化时刷新场景或命令状态。
        /// </summary>
        private void OnRenderOptionsChanged()
        {
            if (HasLoadedCloud)
            {
                RenderOptionsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 通知界面刷新点云文件、包围盒和加载状态属性。
        /// </summary>
        private void RaiseLoadedCloudChanged()
        {
            RaisePropertyChanged(nameof(DisplayCloud));
            RaisePropertyChanged(nameof(HasLoadedCloud));
        }

        /// <summary>
        /// 通知界面刷新点云相关命令的可执行状态。
        /// </summary>
        private void RaiseCommandStates()
        {
            _openPointCloudCommand.RaiseCanExecuteChanged();
            _clearCommand.RaiseCanExecuteChanged();
            _setScalarFieldCommand.RaiseCanExecuteChanged();
            _scalePointCloudCommand.RaiseCanExecuteChanged();
            _editScalarFieldDisplayParamsCommand.RaiseCanExecuteChanged();
            _resetCameraCommand.RaiseCanExecuteChanged();
            _setViewOrientationCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 取消尚未完成的点云加载任务并释放取消令牌。
        /// </summary>
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

        /// <summary>
        /// 释放原始点云和显示点云句柄。
        /// </summary>
        private void DisposeClouds()
        {
            _displayCloud?.Dispose();
            _displayCloud = null;
            _sourceCloud?.Dispose();
            _sourceCloud = null;
        }

        /// <summary>
        /// 向点云日志追加一条带时间的运行信息。
        /// </summary>
        private void AppendLog(string message)
        {
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            while (Logs.Count > 200)
            {
                Logs.RemoveAt(0);
            }
        }

        /// <summary>
        /// 判断文件路径是否为可作为高度图使用的 TIFF。
        /// </summary>
        private static bool IsDepthTiffFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
        }
    }
}

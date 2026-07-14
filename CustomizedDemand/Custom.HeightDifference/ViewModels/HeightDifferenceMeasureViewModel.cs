using Microsoft.Win32;
using Prism.Commands;
using PointCloud.Interop;
using ReeYin.Customized.Algo.Algorithms;
using ReeYin.Customized.Algo.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace ReeYin.Customized.Algo.ViewModels
{
    /// <summary>
    /// 测量模式下拉项，保存模式枚举和界面显示名称。
    /// </summary>
    public sealed class HeightDifferenceMeasureModeOption
    {
        /// <summary>
        /// 创建测量模式下拉项，并保存模式枚举和显示名称。
        /// </summary>
        public HeightDifferenceMeasureModeOption(HeightDifferenceMeasureKind mode, string displayName)
        {
            Mode = mode;
            DisplayName = displayName;
        }

        /// <summary>
        /// 测量模式下拉项对应的枚举值。
        /// </summary>
        public HeightDifferenceMeasureKind Mode { get; }

        /// <summary>
        /// 测量模式下拉项在界面中显示的中文名称。
        /// </summary>
        public string DisplayName { get; }
    }

    /// <summary>
    /// 中间显示区当前承载的视图类型。
    /// </summary>
    public enum HeightDifferenceCenterViewMode
    {
        PointCloud,
        Heatmap
    }

    public class HeightDifferenceMeasureViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 字段与状态

        // 点云视图依赖的原生库，缺失时只禁用点云显示，不影响热力图测量。
        private static readonly string[] PointCloudViewerRequiredFiles =
        [
            "ALGO.VTKWrapperNative.dll",
            "ALGO.PCLCoreNative.dll",
            "ALGO.PCLAlgorithmsNative.dll",
        ];

        /// <summary>
        /// 热力图窗口允许的最小缩放倍率。
        /// </summary>
        public const double MinZoomLevel = 0.1;
        /// <summary>
        /// 热力图窗口允许的最大缩放倍率。
        /// </summary>
        public const double MaxZoomLevel = 64.0;

        private readonly HeightDifferenceMeasureModel _fallbackModel = new();
        private readonly HeightDifferencePointCloudViewModel _embeddedPointCloudViewModel = new();
        // 中间显示区当前选择的点云或热力图视图。
        private HeightDifferenceCenterViewMode _centerViewMode = HeightDifferenceCenterViewMode.Heatmap;
        // 已加载点云对应的原始输入路径，用于判断是否需要重新加载。
        private string _loadedPointCloudSourcePath = string.Empty;
        // 已加载点云使用的 X 方向像素间距。
        private double _loadedPointCloudIntervalX;
        // 已加载点云使用的 Y 方向像素间距。
        private double _loadedPointCloudIntervalY;
        // 已加载点云使用的原始 Z 值到毫米换算系数。
        private double _loadedPointCloudZValueToMillimeterFactor;
        // 点云加载状态在界面上的显示文本。
        private string _pointCloudLoadStateText = "未加载";
        // 点云加载状态文本对应的提示颜色。
        private WpfBrush _pointCloudLoadStateBrush = WpfBrushes.Gray;
        // 标记手动测量热力图是否正在刷新，避免重复触发。
        private bool _isRefreshingManualHeatmap;
        // 标记手动测量热力图在当前刷新后是否还需再次更新。
        private bool _manualHeatmapNeedsRefresh;
        // 当前下拉框选中的测量模式项。
        private HeightDifferenceMeasureModeOption _selectedMeasureModeOption;
        // 结果列表中当前选中的测量结果。
        private HeightDifferenceMeasureItem? _selectedMeasureItem;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化高度差测量对话框状态、测量模式列表和内嵌点云视图模型。
        /// </summary>
        public HeightDifferenceMeasureViewModel()
        {
            Title = "高度差测量";
            Icon = "\ue6a2";
            MeasureModeOptions =
            [
                new(HeightDifferenceMeasureKind.Automatic, "自动测量"),
                new(HeightDifferenceMeasureKind.LineProfile, "画线测量"),
                new(HeightDifferenceMeasureKind.Rectangle1, "矩形1测量"),
                new(HeightDifferenceMeasureKind.Rectangle2, "矩形2测量"),
            ];
            _selectedMeasureModeOption = MeasureModeOptions[0];
            _embeddedPointCloudViewModel.PropertyChanged += EmbeddedPointCloudViewModel_PropertyChanged;
        }

        #endregion

        #region 界面绑定属性

        public new HeightDifferenceMeasureModel ModelParam
        {
            get => base.ModelParam as HeightDifferenceMeasureModel ?? _fallbackModel;
            set
            {
                HeightDifferenceMeasureModel? currentModel = base.ModelParam as HeightDifferenceMeasureModel;
                if (ReferenceEquals(currentModel, value))
                {
                    if (currentModel != null)
                    {
                        currentModel.PropertyChanged -= ModelParam_PropertyChanged;
                        currentModel.PropertyChanged += ModelParam_PropertyChanged;
                    }

                    RaiseSummaryProperties();
                    return;
                }

                if (currentModel != null)
                {
                    currentModel.PropertyChanged -= ModelParam_PropertyChanged;
                }

                base.ModelParam = value;

                if (value != null)
                {
                    value.PropertyChanged += ModelParam_PropertyChanged;
                }

                RaisePropertyChanged();
                RaiseSummaryProperties();
            }
        }

        public string HeightDiffText => ModelParam == null || double.IsNaN(ModelParam.HeightDiff)
            ? "--"
            : ModelParam.FormatMeasurement(Math.Abs(ModelParam.HeightDiff));

        public string ProfileHeightDiffText => ModelParam == null || double.IsNaN(ModelParam.ProfileHeightDiff)
            ? "--"
            : ModelParam.FormatMeasurement(Math.Abs(ModelParam.ProfileHeightDiff));

        public string AreaHeightDiffText
        {
            get
            {
                if (ModelParam != null && !double.IsNaN(ModelParam.AreaHeightDiff))
                {
                    return ModelParam.FormatMeasurement(Math.Abs(ModelParam.AreaHeightDiff));
                }

                return "--";
            }
        }

        public string ProfileSegment1Text => ModelParam == null || double.IsNaN(ModelParam.ProfileSegment1Mean)
            ? "--"
            : ModelParam.FormatMeasurement(ModelParam.ProfileSegment1Mean);

        public string ProfileSegment2Text => ModelParam == null || double.IsNaN(ModelParam.ProfileSegment2Mean)
            ? "--"
            : ModelParam.FormatMeasurement(ModelParam.ProfileSegment2Mean);

        public string ProfileLengthText => ModelParam == null || double.IsNaN(ModelParam.ProfileLength)
            ? "--"
            : ModelParam.FormatPlainNumber(ModelParam.ProfileLength);

        /// <summary>
        /// 当前画线剖面有效采样数与总采样数的显示文本。
        /// </summary>
        public string ProfileSampleSummaryText => ModelParam == null || ModelParam.ProfileSampleCount <= 0
            ? "--"
            : $"{ModelParam.ProfileValidSampleCount}/{ModelParam.ProfileSampleCount}";

        /// <summary>
        /// 热力图高度范围的完整显示文本。
        /// </summary>
        public string HeatmapRangeText => ModelParam == null
            || double.IsNaN(ModelParam.HeatmapRangeMin)
            || double.IsNaN(ModelParam.HeatmapRangeMax)
            ? "--"
            : $"{ModelParam.FormatPlainNumber(ModelParam.HeatmapRangeMin)} ~ {ModelParam.FormatMeasurement(ModelParam.HeatmapRangeMax)}";

        public string HeatmapRangeMaxText => ModelParam == null || double.IsNaN(ModelParam.HeatmapRangeMax)
            ? "--"
            : ModelParam.FormatMeasurement(ModelParam.HeatmapRangeMax);

        /// <summary>
        /// 热力图色带中间刻度对应的高度文本。
        /// </summary>
        public string HeatmapRangeMidText => ModelParam == null
            || double.IsNaN(ModelParam.HeatmapRangeMin)
            || double.IsNaN(ModelParam.HeatmapRangeMax)
            ? "--"
            : ModelParam.FormatMeasurement((ModelParam.HeatmapRangeMin + ModelParam.HeatmapRangeMax) * 0.5);

        public string HeatmapRangeMinText => ModelParam == null || double.IsNaN(ModelParam.HeatmapRangeMin)
            ? "--"
            : ModelParam.FormatMeasurement(ModelParam.HeatmapRangeMin);

        /// <summary>
        /// 模块最近一次执行耗时的界面显示文本。
        /// </summary>
        public string RunTimeText => ModelParam?.Output == null
            ? "--"
            : $"{ModelParam.Output.RunTime:F0} ms";

        /// <summary>
        /// 模块最近一次执行状态的界面显示文本。
        /// </summary>
        public string RunStatusText => ModelParam?.Output == null
            ? "未执行"
            : GetRunStatusText(ModelParam.Output.RunStatus);

        public string InputFileName => string.IsNullOrWhiteSpace(ModelParam?.ManualImagePath)
            ? "未选择高度图或 PCD"
            : Path.GetFileName(ModelParam.ManualImagePath);

        /// <summary>
        /// 统一测量按钮可切换的测量模式列表。
        /// </summary>
        public IReadOnlyList<HeightDifferenceMeasureModeOption> MeasureModeOptions { get; }

        public HeightDifferenceMeasureModeOption SelectedMeasureModeOption
        {
            get => _selectedMeasureModeOption;
            set
            {
                if (value != null && SetProperty(ref _selectedMeasureModeOption, value))
                {
                    RaisePropertyChanged(nameof(MeasureActionText));
                    RaisePropertyChanged(nameof(ProfileInstructionText));
                    RaisePropertyChanged(nameof(AutomaticParameterVisibility));
                }
            }
        }

        public HeightDifferenceMeasureItem? SelectedMeasureItem
        {
            get => _selectedMeasureItem;
            set => SetProperty(ref _selectedMeasureItem, value);
        }

        /// <summary>
        /// 根据测量模式切换统一按钮文案。
        /// </summary>
        public string MeasureActionText => SelectedMeasureModeOption.Mode switch
        {
            HeightDifferenceMeasureKind.LineProfile => "开始画线测量",
            HeightDifferenceMeasureKind.Rectangle1 => "开始矩形1测量",
            HeightDifferenceMeasureKind.Rectangle2 => "开始矩形2测量",
            _ => "执行自动测量"
        };

        /// <summary>
        /// 根据当前测量模式提示高度曲线区域的交互含义。
        /// </summary>
        public string ProfileInstructionText => SelectedMeasureModeOption.Mode switch
        {
            HeightDifferenceMeasureKind.Rectangle1 => "矩形1测量不生成高度曲线；选中测量结果时会在热力图显示对应矩形。",
            HeightDifferenceMeasureKind.Rectangle2 => "矩形2测量不生成高度曲线；选中测量结果时会在热力图显示对应旋转矩形。",
            HeightDifferenceMeasureKind.Automatic => "自动测量直接输出结果；画线测量会在这里显示高度曲线。",
            _ => "在曲线区域拖动两次，分别选择第一段和第二段；选中测量结果时会显示对应线和区间。"
        };

        /// <summary>
        /// 自动测量专用参数区域的可见性。
        /// </summary>
        public System.Windows.Visibility AutomaticParameterVisibility => SelectedMeasureModeOption.Mode == HeightDifferenceMeasureKind.Automatic
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        /// <summary>
        /// 嵌入在高度差窗口中的点云预览视图模型。
        /// </summary>
        public HeightDifferencePointCloudViewModel EmbeddedPointCloudViewModel => _embeddedPointCloudViewModel;

        public string PointCloudLoadStateText
        {
            get => _pointCloudLoadStateText;
            private set => SetProperty(ref _pointCloudLoadStateText, value);
        }

        public WpfBrush PointCloudLoadStateBrush
        {
            get => _pointCloudLoadStateBrush;
            private set => SetProperty(ref _pointCloudLoadStateBrush, value);
        }

        public HeightDifferenceCenterViewMode CenterViewMode
        {
            get => _centerViewMode;
            private set
            {
                if (SetProperty(ref _centerViewMode, value))
                {
                    RaisePropertyChanged(nameof(IsPointCloudViewActive));
                    RaisePropertyChanged(nameof(IsHeatmapViewActive));
                    RaisePropertyChanged(nameof(PointCloudViewVisibility));
                    RaisePropertyChanged(nameof(HeatmapViewVisibility));
                    RaisePropertyChanged(nameof(CenterViewTitle));
                    RaisePropertyChanged(nameof(ToggleCenterViewText));
                }
            }
        }

        /// <summary>
        /// 指示中间区域当前是否显示点云视图。
        /// </summary>
        public bool IsPointCloudViewActive => CenterViewMode == HeightDifferenceCenterViewMode.PointCloud;

        /// <summary>
        /// 指示中间区域当前是否显示热力图与剖面视图。
        /// </summary>
        public bool IsHeatmapViewActive => CenterViewMode == HeightDifferenceCenterViewMode.Heatmap;

        /// <summary>
        /// 点云视图容器的可见性。
        /// </summary>
        public System.Windows.Visibility PointCloudViewVisibility => IsPointCloudViewActive
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        /// <summary>
        /// 热力图与剖面容器的可见性。
        /// </summary>
        public System.Windows.Visibility HeatmapViewVisibility => IsHeatmapViewActive
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        /// <summary>
        /// 中间显示区标题，随点云和热力图视图切换。
        /// </summary>
        public string CenterViewTitle => IsPointCloudViewActive
            ? "点云视图"
            : "热力图与剖面";

        /// <summary>
        /// 点云与热力图切换按钮的显示文案。
        /// </summary>
        public string ToggleCenterViewText => IsPointCloudViewActive
            ? "显示热力图"
            : "显示点云";

        #endregion

        #region 事件通知

        /// <summary>
        /// 请求视图重新适配当前热力图或重置点云相机。
        /// </summary>
        public event Action? FitRequested;

        /// <summary>
        /// 请求视图进入指定的手动测量交互模式。
        /// </summary>
        public event Action<HeightDifferenceMeasureKind>? ManualMeasurementRequested;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化模块参数，并补齐首次打开时需要显示的默认状态。
        /// </summary>
        public override void InitParam()
        {
            ModelParam = InitModelParam<HeightDifferenceMeasureModel>();
            ModelParam.Output ??= new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.NotRun,
                RunTime = 0
            };

            if (string.IsNullOrWhiteSpace(ModelParam.ResultMessage))
            {
                ModelParam.ResultMessage = "请选择高度图或 PCD 文件，系统会自动生成热力图。";
            }

            if (string.IsNullOrWhiteSpace(ModelParam.InputSourceText))
            {
                ModelParam.InputSourceText = "未选择高度图或 PCD 文件";
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = "高度差测量";
            }

            if (string.IsNullOrWhiteSpace(Icon))
            {
                Icon = "\ue6a2";
            }

            CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
            RaiseSummaryProperties();
        }

        /// <summary>
        /// 对话框关闭时释放点云或测量视图相关资源。
        /// </summary>
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.PropertyChanged -= ModelParam_PropertyChanged;
                ModelParam.IsDebug = false;
            }

            _embeddedPointCloudViewModel.PropertyChanged -= EmbeddedPointCloudViewModel_PropertyChanged;
            _embeddedPointCloudViewModel.OnDialogClosed();
        }

        #endregion

        #region 命令入口

        public DelegateCommand LoadCommand => new(() =>
        {
            if (ModelParam == null)
            {
                return;
            }

            ModelParam.Output ??= new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.NotRun,
                RunTime = 0
            };

            if (string.IsNullOrWhiteSpace(ModelParam.ResultMessage))
            {
                ModelParam.ResultMessage = "请选择高度图或 PCD 文件，系统会自动生成热力图。";
            }

            if (string.IsNullOrWhiteSpace(ModelParam.InputSourceText))
            {
                ModelParam.InputSourceText = "未选择高度图或 PCD 文件";
            }

            RaiseSummaryProperties();
        });

        public DelegateCommand BrowseImageCommand => new(() =>
        {
            WpfOpenFileDialog dialog = new()
            {
                Filter = "高度图或点云文件|*.pcd;*.tif;*.tiff;*.png;*.bmp;*.jpg;*.jpeg|点云文件|*.pcd|图像文件|*.tif;*.tiff;*.png;*.bmp;*.jpg;*.jpeg|所有文件|*.*",
                Title = "选择高度图或 PCD 文件"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ModelParam.ManualImagePath = dialog.FileName;
            ModelParam.ClearManualMeasureItems();
            ModelParam.ResetRuntimeState(PcdDepthInputHelper.IsPcdFile(dialog.FileName)
                ? "已选择 PCD 文件，正在生成热力图。"
                : "已选择高度图，正在生成热力图。");
            ModelParam.InputSourceText = dialog.FileName;
            ResetLoadedPointCloudState();
            CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
            ModelParam.PrepareManualMeasurement();
            _manualHeatmapNeedsRefresh = ModelParam.Output?.RunStatus != NodeStatus.Success;
            RaiseSummaryProperties();
            if (ModelParam.HeatmapPreviewImage != null)
            {
                FitRequested?.Invoke();
            }
        });

        /// <summary>
        /// 统一测量按钮：自动模式执行算法，手动模式切换到热力图交互。
        /// </summary>
        public DelegateCommand StartMeasureCommand => new(() =>
        {
            if (ModelParam == null)
            {
                return;
            }

            // 统一测量入口：自动测量直接执行算法，手动模式交给视图进入绘制交互。
            HeightDifferenceMeasureKind mode = SelectedMeasureModeOption.Mode;
            SelectedMeasureItem = null;
            if (mode == HeightDifferenceMeasureKind.Automatic)
            {
                ModelParam.ExecuteMeasurementWithCurrentParameters();
                _manualHeatmapNeedsRefresh = ModelParam.Output?.RunStatus != NodeStatus.Success;
                RaiseSummaryProperties();
                return;
            }

            if (!EnsureManualHeatmapFreshForMeasurement())
            {
                ModelParam.ResultMessage = "请先选择有效的高度图或 PCD 文件，并生成热力图后再开始手动测量。";
                RaiseSummaryProperties();
                return;
            }

            CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
            ModelParam.ResultMessage = mode switch
            {
                HeightDifferenceMeasureKind.LineProfile => "画线测量已启动，请在热力图上按住鼠标左键拖动画线。",
                HeightDifferenceMeasureKind.Rectangle2 => "矩形2测量已启动，请在热力图上依次绘制两个旋转矩形区域。",
                _ => "矩形1测量已启动，请在热力图上依次框选两个水平矩形区域。"
            };
            ManualMeasurementRequested?.Invoke(mode);
            RaiseSummaryProperties();
        });

        public DelegateCommand AutoMeasureCommand => new(() =>
        {
            if (ModelParam == null)
            {
                return;
            }

            ModelParam.ExecuteMeasurementWithCurrentParameters();
            _manualHeatmapNeedsRefresh = ModelParam.Output?.RunStatus != NodeStatus.Success;
            RaiseSummaryProperties();
        });

        public DelegateCommand ManualMeasureCommand => new(() =>
        {
            if (ModelParam == null)
            {
                return;
            }

            ModelParam.PrepareManualMeasurement();
            _manualHeatmapNeedsRefresh = ModelParam.Output?.RunStatus != NodeStatus.Success;
            RaiseSummaryProperties();
        });

        public DelegateCommand ToggleCenterViewCommand => new(async () =>
        {
            if (IsPointCloudViewActive)
            {
                CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
                if (ModelParam?.HeatmapPreviewImage != null)
                {
                    FitRequested?.Invoke();
                }

                return;
            }

            CenterViewMode = HeightDifferenceCenterViewMode.PointCloud;
            await TryLoadEmbeddedPointCloudAsync(updateStatusMessage: true);
        });

        public DelegateCommand ShowPointCloudCommand => new(async () =>
        {
            if (ModelParam == null)
            {
                return;
            }

            CenterViewMode = HeightDifferenceCenterViewMode.PointCloud;
            await TryLoadEmbeddedPointCloudAsync(updateStatusMessage: true);
        });

        public DelegateCommand ShowHeatmapCommand => new(() =>
        {
            CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;

            if (ModelParam?.HeatmapPreviewImage != null)
            {
                FitRequested?.Invoke();
            }
        });

        public DelegateCommand FitImageCommand => new(() =>
        {
            if (IsPointCloudViewActive)
            {
                System.Windows.Input.ICommand resetCameraCommand = _embeddedPointCloudViewModel.ResetCameraCommand;
                if (resetCameraCommand.CanExecute(null))
                {
                    resetCameraCommand.Execute(null);
                }

                return;
            }

            FitRequested?.Invoke();
        });

        public DelegateCommand ZoomInCommand => new(() =>
            ModelParam.Zoom = Math.Min(MaxZoomLevel, ModelParam.Zoom + GetZoomStep(ModelParam.Zoom)));

        public DelegateCommand ZoomOutCommand => new(() =>
            ModelParam.Zoom = Math.Max(MinZoomLevel, ModelParam.Zoom - GetZoomStep(ModelParam.Zoom)));

        #endregion

        #region 属性变化与辅助逻辑

        /// <summary>
        /// 监听模型输出变化，刷新界面汇总文本和图像适配状态。
        /// </summary>
        private void ModelParam_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HeightDifferenceMeasureModel.HeightDiff):
                case nameof(HeightDifferenceMeasureModel.AreaHeightDiff):
                case nameof(HeightDifferenceMeasureModel.ProfileHeightDiff):
                case nameof(HeightDifferenceMeasureModel.ProfileSegment1Mean):
                case nameof(HeightDifferenceMeasureModel.ProfileSegment2Mean):
                case nameof(HeightDifferenceMeasureModel.ProfileLength):
                case nameof(HeightDifferenceMeasureModel.ProfileSampleCount):
                case nameof(HeightDifferenceMeasureModel.ProfileValidSampleCount):
                case nameof(HeightDifferenceMeasureModel.HeatmapRangeMin):
                case nameof(HeightDifferenceMeasureModel.HeatmapRangeMax):
                case nameof(HeightDifferenceMeasureModel.HeatmapColorBarImage):
                case nameof(HeightDifferenceMeasureModel.MeasurementPrecision):
                case nameof(HeightDifferenceMeasureModel.Output):
                case nameof(HeightDifferenceMeasureModel.ManualImagePath):
                    RaiseSummaryProperties();
                    if (e.PropertyName == nameof(HeightDifferenceMeasureModel.ManualImagePath))
                    {
                        ModelParam.ClearManualMeasureItems();
                        _manualHeatmapNeedsRefresh = true;
                        ResetLoadedPointCloudState();
                        CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
                    }

                    break;
                case nameof(HeightDifferenceMeasureModel.IntervalX):
                case nameof(HeightDifferenceMeasureModel.IntervalY):
                case nameof(HeightDifferenceMeasureModel.ZValueScale):
                case nameof(HeightDifferenceMeasureModel.ZValueUnit):
                case nameof(HeightDifferenceMeasureModel.InvalidGrayCenter):
                case nameof(HeightDifferenceMeasureModel.InvalidGrayTolerance):
                    RaiseSummaryProperties();
                    ResetLoadedPointCloudState();
                    _manualHeatmapNeedsRefresh = true;
                    break;
                case nameof(HeightDifferenceMeasureModel.HeatmapPreviewImage):
                    RaiseSummaryProperties();
                    if (ModelParam.HeatmapPreviewImage != null
                        && IsHeatmapViewActive
                        && !ModelParam.IsReplacingHeatmapPreviewWithoutLayoutReset)
                    {
                        FitRequested?.Invoke();
                    }

                    break;
                case nameof(HeightDifferenceMeasureModel.Zoom):
                    RaisePropertyChanged(nameof(ModelParam));
                    break;
            }
        }

        /// <summary>
        /// 通知界面刷新测量结果、热力图范围和视图状态文本。
        /// </summary>
        private void RaiseSummaryProperties()
        {
            RaisePropertyChanged(nameof(HeightDiffText));
            RaisePropertyChanged(nameof(ProfileHeightDiffText));
            RaisePropertyChanged(nameof(AreaHeightDiffText));
            RaisePropertyChanged(nameof(ProfileSegment1Text));
            RaisePropertyChanged(nameof(ProfileSegment2Text));
            RaisePropertyChanged(nameof(ProfileLengthText));
            RaisePropertyChanged(nameof(ProfileSampleSummaryText));
            RaisePropertyChanged(nameof(HeatmapRangeText));
            RaisePropertyChanged(nameof(HeatmapRangeMaxText));
            RaisePropertyChanged(nameof(HeatmapRangeMidText));
            RaisePropertyChanged(nameof(HeatmapRangeMinText));
            RaisePropertyChanged(nameof(RunTimeText));
            RaisePropertyChanged(nameof(RunStatusText));
            RaisePropertyChanged(nameof(InputFileName));
        }

        /// <summary>
        /// 在进入手动测量前刷新热力图和深度缓存。
        /// </summary>
        public bool EnsureManualHeatmapFreshForMeasurement(bool forceRefresh = false)
        {
            if (_isRefreshingManualHeatmap
                || ModelParam == null
                || string.IsNullOrWhiteSpace(ModelParam.ManualImagePath)
                || !File.Exists(ModelParam.ManualImagePath))
            {
                return false;
            }

            if (!_manualHeatmapNeedsRefresh && !forceRefresh)
            {
                return true;
            }

            _isRefreshingManualHeatmap = true;
            try
            {
                CenterViewMode = HeightDifferenceCenterViewMode.Heatmap;
                ModelParam.PrepareManualMeasurement();
                _manualHeatmapNeedsRefresh = ModelParam.Output?.RunStatus != NodeStatus.Success;
                RaiseSummaryProperties();

                return !_manualHeatmapNeedsRefresh;
            }
            finally
            {
                _isRefreshingManualHeatmap = false;
            }
        }

        public DelegateCommand<HeightDifferenceMeasureItem?> DeleteMeasureItemCommand => new(item =>
        {
            if (ModelParam?.DeleteMeasureItem(item) == true)
            {
                if (ReferenceEquals(SelectedMeasureItem, item))
                {
                    SelectedMeasureItem = ModelParam.MeasureItems.LastOrDefault();
                }

                RaiseSummaryProperties();
            }
        });

        public DelegateCommand ClearMeasureItemsCommand => new(() =>
        {
            ModelParam?.ClearManualMeasureItems();
            SelectedMeasureItem = null;
            RaiseSummaryProperties();
        });

        /// <summary>
        /// 根据当前输入自动加载内嵌点云预览。
        /// </summary>
        private async Task<bool> TryLoadEmbeddedPointCloudAsync(bool updateStatusMessage)
        {
            if (ModelParam == null)
            {
                return false;
            }

            if (!TryResolvePointCloudDisplaySource(out string sourcePath, out bool isDepthTiff, out string sourceError))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.ResultMessage = sourceError;
                }

                return false;
            }

            if (!TryEnsurePointCloudViewerAvailable(out string dependencyMessage))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.ResultMessage = $"点云查看器依赖未就绪：{dependencyMessage}";
                }

                return false;
            }

            try
            {
                if (IsEmbeddedPointCloudReadyForCurrentInput(sourcePath))
                {
                    SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                }
                else
                {
                    SetPointCloudLoadState("加载中", WpfBrushes.DodgerBlue);
                    _embeddedPointCloudViewModel.BeginSwitchingDisplay(Path.GetFileName(sourcePath));

                    if (isDepthTiff)
                    {
                        await _embeddedPointCloudViewModel.LoadDepthTiffAsync(
                            sourcePath,
                            new DepthTiffLoadOptions(
                                ModelParam.IntervalX,
                                ModelParam.IntervalY,
                                ModelParam.ZValueToMillimeterFactor,
                                ModelParam.InvalidGrayCenter,
                                true),
                            resetCamera: true);
                    }
                    else
                    {
                        await _embeddedPointCloudViewModel.LoadPointCloudFileAsync(sourcePath, resetCamera: true);
                    }

                    UpdateLoadedPointCloudCache(sourcePath);
                    SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                }
            }
            catch (Exception ex)
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.ResultMessage = $"点云加载失败：{ex.Message}";
                }

                return false;
            }

            if (updateStatusMessage)
            {
                ModelParam.ResultMessage = $"当前显示点云：{sourcePath}";
            }

            return true;
        }

        /// <summary>
        /// 解析点云视图应该使用的 PCD 或深度 TIFF 来源。
        /// </summary>
        private bool TryResolvePointCloudDisplaySource(out string sourcePath, out bool isDepthTiff, out string errorMessage)
        {
            sourcePath = string.Empty;
            isDepthTiff = false;
            errorMessage = "请先选择高度图或点云文件。";

            string inputPath = ModelParam.ManualImagePath;
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return false;
            }

            if (!File.Exists(inputPath))
            {
                errorMessage = "输入文件不存在，无法显示点云。";
                return false;
            }

            if (IsDepthTiffFile(inputPath))
            {
                sourcePath = inputPath;
                isDepthTiff = true;
                return true;
            }

            string pointCloudPath = PcdDepthInputHelper.TryResolvePointCloudPath(inputPath);
            if (!string.IsNullOrWhiteSpace(pointCloudPath))
            {
                sourcePath = pointCloudPath;
                return true;
            }

            errorMessage = "未找到可显示的点云文件，请选择 PCD 文件、TIFF 高度图，或选择同名 PCD 对应的高度图。";
            return false;
        }

        /// <summary>
        /// 点云视图状态变化时同步加载状态到主窗口。
        /// </summary>
        private void EmbeddedPointCloudViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HeightDifferencePointCloudViewModel.HasLoadedCloud)
                && _embeddedPointCloudViewModel.HasLoadedCloud)
            {
                SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                return;
            }

            if (e.PropertyName == nameof(HeightDifferencePointCloudViewModel.StatusText)
                && IsPointCloudLoadFailureStatus(_embeddedPointCloudViewModel.StatusText))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
            }
        }

        /// <summary>
        /// 清空已加载点云路径和标定参数缓存。
        /// </summary>
        private void ResetLoadedPointCloudState()
        {
            _loadedPointCloudSourcePath = string.Empty;
            _loadedPointCloudIntervalX = 0;
            _loadedPointCloudIntervalY = 0;
            _loadedPointCloudZValueToMillimeterFactor = 0;
            SetPointCloudLoadState("未加载", WpfBrushes.Gray);
        }

        /// <summary>
        /// 记录当前已加载点云的来源路径和标定参数。
        /// </summary>
        private void UpdateLoadedPointCloudCache(string sourcePath)
        {
            _loadedPointCloudSourcePath = sourcePath;
            _loadedPointCloudIntervalX = ModelParam.IntervalX;
            _loadedPointCloudIntervalY = ModelParam.IntervalY;
            _loadedPointCloudZValueToMillimeterFactor = ModelParam.ZValueToMillimeterFactor;
        }

        /// <summary>
        /// 判断内嵌点云是否已经匹配当前输入和标定参数。
        /// </summary>
        private bool IsEmbeddedPointCloudReadyForCurrentInput(string sourcePath)
        {
            return !string.IsNullOrWhiteSpace(_embeddedPointCloudViewModel.FilePath)
                && File.Exists(_embeddedPointCloudViewModel.FilePath)
                && _embeddedPointCloudViewModel.HasLoadedCloud
                && string.Equals(_loadedPointCloudSourcePath, sourcePath, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(_loadedPointCloudIntervalX - ModelParam.IntervalX) < 0.000000001
                && Math.Abs(_loadedPointCloudIntervalY - ModelParam.IntervalY) < 0.000000001
                && Math.Abs(_loadedPointCloudZValueToMillimeterFactor - ModelParam.ZValueToMillimeterFactor) < 0.000000001;
        }

        /// <summary>
        /// 更新点云加载状态文本和提示颜色。
        /// </summary>
        private void SetPointCloudLoadState(string text, WpfBrush brush)
        {
            PointCloudLoadStateText = text;
            PointCloudLoadStateBrush = brush;
        }

        /// <summary>
        /// 判断点云加载状态是否属于失败或不可用。
        /// </summary>
        private static bool IsPointCloudLoadFailureStatus(string statusText)
        {
            return !string.IsNullOrWhiteSpace(statusText)
                && (statusText.Contains("加载失败", StringComparison.OrdinalIgnoreCase)
                    || statusText.Contains("Load failed", StringComparison.OrdinalIgnoreCase));
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

        /// <summary>
        /// 把节点运行状态枚举转换为界面状态文本。
        /// </summary>
        private static string GetRunStatusText(NodeStatus status)
        {
            return status switch
            {
                NodeStatus.Success => "成功",
                NodeStatus.Error => "失败",
                NodeStatus.NotRun => "未执行",
                NodeStatus.NoParam => "待执行",
                _ => status.ToString()
            };
        }

        /// <summary>
        /// 根据当前缩放倍率计算鼠标滚轮缩放步长。
        /// </summary>
        private static double GetZoomStep(double currentZoom)
        {
            if (currentZoom < 2.0)
            {
                return 0.1;
            }

            if (currentZoom < 8.0)
            {
                return 0.25;
            }

            if (currentZoom < 20.0)
            {
                return 0.5;
            }

            return 1.0;
        }

        /// <summary>
        /// 检查点云视图依赖库是否存在并给出日志提示。
        /// </summary>
        private static bool TryEnsurePointCloudViewerAvailable(out string message)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] missingFiles = PointCloudViewerRequiredFiles
                .Where(file => !File.Exists(Path.Combine(baseDirectory, file)))
                .ToArray();

            if (missingFiles.Length == 0)
            {
                message = string.Empty;
                return true;
            }

            message = $"缺少原生依赖 {string.Join(", ", missingFiles)}，当前运行目录：{baseDirectory}";
            return false;
        }

        #endregion
    }
}

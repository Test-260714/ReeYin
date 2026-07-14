using Custom.CalibrationPlateMeasure.Algorithms;
using Custom.CalibrationPlateMeasure.Models;
using PointCloud.Interop;
using Prism.Commands;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Custom.CalibrationPlateMeasure.ViewModels
{
    // 中间区域显示模式：灰度图用于框选和查看测量结果，点云用于三维确认。
    public enum CalibrationPlateCenterDisplayMode
    {
        GrayImage,
        PointCloud
    }

    // 测量类型下拉框数据项。
    public sealed class CalibrationPlateMeasurementModeOption
    {
        public required CalibrationPlateMeasurementMode Mode { get; init; }

        public required string Name { get; init; }
    }

    // 负责视图状态、命令、日志和嵌入点云窗口的调度。
    public sealed class CalibrationPlateMeasureViewModel : DialogViewModelBase, IViewModuleParam
    {
        private static readonly string[] PointCloudViewerRequiredFiles =
        [
            "ALGO.VTKWrapperNative.dll",
            "ALGO.PCLCoreNative.dll",
            "ALGO.PCLAlgorithmsNative.dll",
        ];

        private const int MaxOperationLogEntries = 300;

        private readonly CalibrationPlateMeasureModel _fallbackModel = new();
        private readonly CalibrationPlatePointCloudViewModel _embeddedPointCloudViewModel = new();
        private CalibrationPlateCenterDisplayMode _centerDisplayMode = CalibrationPlateCenterDisplayMode.GrayImage;
        private string _loadedPointCloudSourcePath = string.Empty;
        private double _loadedPointCloudIntervalX;
        private double _loadedPointCloudIntervalY;
        private double _loadedPointCloudIntervalZ;
        private string _pointCloudLoadStateText = "未加载";
        private WpfBrush _pointCloudLoadStateBrush = WpfBrushes.Gray;

        public CalibrationPlateMeasureViewModel()
        {
            Title = "标准片测量";
            Icon = "\ue6a2";
            _embeddedPointCloudViewModel.Logs.CollectionChanged += EmbeddedPointCloudLogs_CollectionChanged;
            _embeddedPointCloudViewModel.PropertyChanged += EmbeddedPointCloudViewModel_PropertyChanged;
            AppendOperationLog("\u65e5\u5fd7\u6a21\u5757\u5df2\u5c31\u7eea\u3002");
        }

        public event EventHandler? GrayImageRefreshRequested;

        public new CalibrationPlateMeasureModel ModelParam
        {
            get => base.ModelParam as CalibrationPlateMeasureModel ?? _fallbackModel;
            set
            {
                if (base.ModelParam is CalibrationPlateMeasureModel currentModel)
                {
                    currentModel.PropertyChanged -= ModelParam_PropertyChanged;
                }

                base.ModelParam = value;

                if (value != null)
                {
                    value.PropertyChanged += ModelParam_PropertyChanged;
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedMeasurementMode));
                RaiseParameterVisibilityChanged();
                RaiseSummaryProperties();
            }
        }

        public string TargetCountText => $"{ModelParam.MeasureItems.Count} 个目标";

        public string RunStatusText => ModelParam.Output == null
            ? "未执行"
            : ModelParam.Output.RunStatus switch
            {
                NodeStatus.Success => "成功",
                NodeStatus.Error => "失败",
                NodeStatus.NotRun => "未执行",
                NodeStatus.NoParam => "待执行",
                _ => ModelParam.Output.RunStatus.ToString()
            };

        public string RunTimeText => ModelParam.Output == null
            ? "--"
            : $"{ModelParam.Output.RunTime:F0} ms";

        public ObservableCollection<string> OperationLogs { get; } = [];

        public IReadOnlyList<CalibrationPlateMeasurementModeOption> MeasurementModeOptions { get; } =
        [
            new() { Mode = CalibrationPlateMeasurementMode.Groove, Name = "刻槽测量" },
            new() { Mode = CalibrationPlateMeasurementMode.Circle, Name = "圆测量" }
        ];

        public CalibrationPlatePointCloudViewModel EmbeddedPointCloudViewModel => _embeddedPointCloudViewModel;

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

        public CalibrationPlateMeasurementMode SelectedMeasurementMode
        {
            get => ModelParam.MeasurementMode;
            set
            {
                if (ModelParam.MeasurementMode == value)
                {
                    return;
                }

                ModelParam.MeasurementMode = value;
                RaisePropertyChanged();
                RaiseParameterVisibilityChanged();
            }
        }

        public CalibrationPlateCenterDisplayMode CenterDisplayMode
        {
            get => _centerDisplayMode;
            private set
            {
                if (SetProperty(ref _centerDisplayMode, value))
                {
                    RaisePropertyChanged(nameof(GrayImageVisibility));
                    RaisePropertyChanged(nameof(PointCloudVisibility));
                    RaisePropertyChanged(nameof(ToggleCenterDisplayText));
                }
            }
        }

        public Visibility GrayImageVisibility => CenterDisplayMode == CalibrationPlateCenterDisplayMode.GrayImage
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility PointCloudVisibility => CenterDisplayMode == CalibrationPlateCenterDisplayMode.PointCloud
            ? Visibility.Visible
            : Visibility.Collapsed;

        public string ToggleCenterDisplayText => CenterDisplayMode == CalibrationPlateCenterDisplayMode.GrayImage
            ? "显示点云"
            : "显示灰度图";

        public Visibility GrooveParameterVisibility => SelectedMeasurementMode == CalibrationPlateMeasurementMode.Groove
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility CircleParameterVisibility => SelectedMeasurementMode == CalibrationPlateMeasurementMode.Circle
            ? Visibility.Visible
            : Visibility.Collapsed;

        public override void InitParam()
        {
            ModelParam = InitModelParam<CalibrationPlateMeasureModel>();
            ModelParam.Output ??= new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.NotRun,
                RunTime = 0
            };

            if (string.IsNullOrWhiteSpace(ModelParam.StatusText))
            {
                ModelParam.StatusText = "请选择 TIFF 高度图后执行测量。";
            }

            AppendOperationLog("\u6a21\u5757\u53c2\u6570\u5df2\u521d\u59cb\u5316\u3002");
            ShowGrayImage();
            RaiseSummaryProperties();
        }

        public override void LoadSpecificConfig(ModelParamBase modelParam)
        {
            if (modelParam == null)
            {
                return;
            }

            modelParam.OutputParamResource.Clear();
            if (Serial >= 0)
            {
                modelParam.Serial = Serial;
            }
            else if (modelParam.Serial == -999)
            {
                modelParam.Serial = Serial;
            }

            if (modelParam is CalibrationPlateMeasureModel calibrationModel)
            {
                AppendOperationLog("\u8fd0\u884c\u6a21\u5f0f\u89c6\u56fe\u53c2\u6570\u5df2\u52a0\u8f7d\u3002");
                ShowGrayImage();
                calibrationModel.EnsureWindowControlInitialized();
                TryLoadGrayPreview();
                return;
            }

            base.LoadSpecificConfig(modelParam);
        }

        public override void OnDialogClosed()
        {
            if (base.ModelParam is CalibrationPlateMeasureModel model)
            {
                model.PropertyChanged -= ModelParam_PropertyChanged;
                model.IsDebug = false;
            }

            _embeddedPointCloudViewModel.Logs.CollectionChanged -= EmbeddedPointCloudLogs_CollectionChanged;
            _embeddedPointCloudViewModel.PropertyChanged -= EmbeddedPointCloudViewModel_PropertyChanged;
            _embeddedPointCloudViewModel.OnDialogClosed();
        }

        public DelegateCommand BrowseImageCommand => new(() =>
        {
            WpfOpenFileDialog dialog = new()
            {
                Filter = "TIFF 高度图|*.tif;*.tiff|图像文件|*.tif;*.tiff;*.png;*.bmp;*.jpg;*.jpeg|所有文件|*.*",
                Title = "选择标准片 TIFF 高度图"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ModelParam.ImagePath = dialog.FileName;
            ModelParam.ResetRuntimeState("已选择图像，请执行测量。");
            ModelParam.ClearSelectedRoi();
            TryLoadGrayPreview();
            ResetLoadedPointCloudState();
            ShowGrayImage();
            RaiseSummaryProperties();
            AppendOperationLog($"\u5df2\u9009\u62e9\u56fe\u50cf\uff1a{dialog.FileName}");
        });

        public DelegateCommand ExecuteMeasureCommand => new(() =>
        {
            AppendOperationLog($"开始执行{GetMeasurementModeName(ModelParam.MeasurementMode)}算法。");
            ShowGrayImage();
            ModelParam.EnsureWindowControlInitialized();
            ModelParam.ExecuteModule();
            ShowGrayImage();
            RaiseSummaryProperties();
            RequestGrayImageRefresh();
            AppendOperationLog($"\u7b97\u6cd5\u6267\u884c\u7ed3\u675f\uff1a{RunStatusText}\uff0c\u8017\u65f6 {RunTimeText}\uff0c\u76ee\u6807\u6570 {TargetCountText}\u3002");
        });

        public DelegateCommand ToggleCenterDisplayCommand => new(async () =>
        {
            if (CenterDisplayMode == CalibrationPlateCenterDisplayMode.PointCloud)
            {
                ShowGrayImage();
                AppendOperationLog("\u5207\u6362\u5230\u7070\u5ea6\u56fe\u663e\u793a\u3002");
                return;
            }

            AppendOperationLog("\u51c6\u5907\u5207\u6362\u5230\u70b9\u4e91\u663e\u793a\u3002");
            if (await TryLoadEmbeddedPointCloudAsync(updateStatusMessage: true))
            {
                CenterDisplayMode = CalibrationPlateCenterDisplayMode.PointCloud;
                AppendOperationLog("\u5df2\u5207\u6362\u5230\u70b9\u4e91\u663e\u793a\u3002");
            }
            else
            {
                ShowGrayImage();
                AppendOperationLog("\u70b9\u4e91\u663e\u793a\u5207\u6362\u5931\u8d25\uff0c\u5df2\u4fdd\u6301\u7070\u5ea6\u56fe\u3002");
            }
        });

        public DelegateCommand<CalibrationPlateMeasureItem?> DeleteMeasureItemCommand => new(item =>
        {
            if (!ModelParam.DeleteMeasureItem(item))
            {
                return;
            }

            RaiseSummaryProperties();
            RequestGrayImageRefresh();
            AppendOperationLog($"已删除测量结果，剩余目标数量：{TargetCountText}。");
        });

        private void ModelParam_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CalibrationPlateMeasureModel.ImagePath):
                case nameof(CalibrationPlateMeasureModel.Output):
                case nameof(CalibrationPlateMeasureModel.MeasurementMode):
                    RaiseSummaryProperties();
                    if (e.PropertyName == nameof(CalibrationPlateMeasureModel.ImagePath))
                    {
                        ResetLoadedPointCloudState();
                        ShowGrayImage();
                    }

                    if (e.PropertyName == nameof(CalibrationPlateMeasureModel.MeasurementMode))
                    {
                        RaisePropertyChanged(nameof(SelectedMeasurementMode));
                        RaiseParameterVisibilityChanged();
                        AppendOperationLog($"测量类型已切换为：{GetMeasurementModeName(ModelParam.MeasurementMode)}。");
                    }

                    break;

                case nameof(CalibrationPlateMeasureModel.StatusText):
                    if (!string.IsNullOrWhiteSpace(ModelParam.StatusText))
                    {
                        AppendOperationLog($"\u7b97\u6cd5\u72b6\u6001\uff1a{ModelParam.StatusText}");
                    }

                    break;
            }
        }

        private void RaiseSummaryProperties()
        {
            RaisePropertyChanged(nameof(TargetCountText));
            RaisePropertyChanged(nameof(RunStatusText));
            RaisePropertyChanged(nameof(RunTimeText));
        }

        private void RaiseParameterVisibilityChanged()
        {
            RaisePropertyChanged(nameof(GrooveParameterVisibility));
            RaisePropertyChanged(nameof(CircleParameterVisibility));
        }

        private void ShowGrayImage()
        {
            CenterDisplayMode = CalibrationPlateCenterDisplayMode.GrayImage;
            RequestGrayImageRefresh();
        }

        private void RequestGrayImageRefresh()
        {
            GrayImageRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TryLoadGrayPreview()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ModelParam.ImagePath) || !File.Exists(ModelParam.ImagePath))
                {
                    return;
                }

                ModelParam.LoadInputPreview();
                AppendOperationLog("\u5df2\u52a0\u8f7d\u7070\u5ea6\u9884\u89c8\u56fe\uff0c\u8bf7\u9f20\u6807\u6846\u9009\u76ee\u6807\u533a\u57df\u3002");
            }
            catch (Exception ex)
            {
                AppendOperationLog($"\u7070\u5ea6\u9884\u89c8\u56fe\u52a0\u8f7d\u5931\u8d25\uff1a{ex.Message}");
            }
        }

        private async Task<bool> TryLoadEmbeddedPointCloudAsync(bool updateStatusMessage)
        {
            if (!TryValidatePointCloudSource(updateStatusMessage))
            {
                AppendOperationLog("\u70b9\u4e91\u6e90\u56fe\u50cf\u6821\u9a8c\u5931\u8d25\u3002");
                return false;
            }

            if (!TryEnsurePointCloudViewerAvailable(out string dependencyMessage))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.StatusText = $"点云查看器依赖未就绪：{dependencyMessage}";
                }

                AppendOperationLog($"\u70b9\u4e91\u4f9d\u8d56\u68c0\u67e5\u5931\u8d25\uff1a{dependencyMessage}");
                return false;
            }

            try
            {
                if (IsEmbeddedPointCloudReadyForCurrentInput())
                {
                    SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                    AppendOperationLog("\u4f7f\u7528\u5df2\u7f13\u5b58\u7684\u70b9\u4e91\u6570\u636e\u3002");
                    return true;
                }

                string pointCloudPath = ModelParam.ImagePath;
                AppendOperationLog($"\u70b9\u4e91\u6587\u4ef6\u5df2\u51c6\u5907\uff1a{pointCloudPath}");
                if (!string.Equals(_embeddedPointCloudViewModel.FilePath, pointCloudPath, StringComparison.OrdinalIgnoreCase)
                    || !_embeddedPointCloudViewModel.HasLoadedCloud)
                {
                    SetPointCloudLoadState("加载中", WpfBrushes.DodgerBlue);
                    _embeddedPointCloudViewModel.BeginSwitchingDisplay(Path.GetFileName(ModelParam.ImagePath));
                    await _embeddedPointCloudViewModel.LoadDepthTiffAsync(
                        pointCloudPath,
                        new DepthTiffLoadOptions(ModelParam.IntervalX, ModelParam.IntervalY, ModelParam.IntervalZ, 0, false),
                        resetCamera: true);
                    UpdateLoadedPointCloudCache();
                }

                SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                return true;
            }
            catch (Exception ex)
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.StatusText = $"点云生成失败：{ex.Message}";
                }

                AppendOperationLog($"\u70b9\u4e91\u751f\u6210\u5931\u8d25\uff1a{ex.Message}");
                return false;
            }
        }

        private bool TryValidatePointCloudSource(bool updateStatusMessage)
        {
            if (string.IsNullOrWhiteSpace(ModelParam.ImagePath))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.StatusText = "请先选择 TIFF 高度图。";
                }

                return false;
            }

            if (!File.Exists(ModelParam.ImagePath))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
                if (updateStatusMessage)
                {
                    ModelParam.StatusText = "图像文件不存在，无法生成点云。";
                }

                return false;
            }

            return true;
        }

        private void UpdateLoadedPointCloudCache()
        {
            _loadedPointCloudSourcePath = ModelParam.ImagePath;
            _loadedPointCloudIntervalX = ModelParam.IntervalX;
            _loadedPointCloudIntervalY = ModelParam.IntervalY;
            _loadedPointCloudIntervalZ = ModelParam.IntervalZ;
        }

        private void EmbeddedPointCloudLogs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
            {
                return;
            }

            foreach (object? item in e.NewItems)
            {
                if (item is string line && !string.IsNullOrWhiteSpace(line))
                {
                    AppendOperationLog($"\u70b9\u4e91\uff1a{line}");
                }
            }
        }

        private void EmbeddedPointCloudViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalibrationPlatePointCloudViewModel.HasLoadedCloud)
                && _embeddedPointCloudViewModel.HasLoadedCloud)
            {
                SetPointCloudLoadState("加载完成", WpfBrushes.SeaGreen);
                return;
            }

            if (e.PropertyName == nameof(CalibrationPlatePointCloudViewModel.StatusText)
                && IsPointCloudLoadFailureStatus(_embeddedPointCloudViewModel.StatusText))
            {
                SetPointCloudLoadState("加载失败", WpfBrushes.IndianRed);
            }
        }

        private void AppendOperationLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            OperationLogs.Add(line);

            while (OperationLogs.Count > MaxOperationLogEntries)
            {
                OperationLogs.RemoveAt(0);
            }
        }

        private void ResetLoadedPointCloudState()
        {
            _loadedPointCloudSourcePath = string.Empty;
            _loadedPointCloudIntervalX = 0;
            _loadedPointCloudIntervalY = 0;
            _loadedPointCloudIntervalZ = 0;
            SetPointCloudLoadState("未加载", WpfBrushes.Gray);
        }

        private void SetPointCloudLoadState(string text, WpfBrush brush)
        {
            PointCloudLoadStateText = text;
            PointCloudLoadStateBrush = brush;
        }

        private static bool IsPointCloudLoadFailureStatus(string statusText)
        {
            return !string.IsNullOrWhiteSpace(statusText)
                && (statusText.Contains("加载失败", StringComparison.OrdinalIgnoreCase)
                    || statusText.Contains("Load failed", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsEmbeddedPointCloudReadyForCurrentInput()
        {
            return !string.IsNullOrWhiteSpace(_embeddedPointCloudViewModel.FilePath)
                && File.Exists(_embeddedPointCloudViewModel.FilePath)
                && _embeddedPointCloudViewModel.HasLoadedCloud
                && string.Equals(_loadedPointCloudSourcePath, ModelParam.ImagePath, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(_loadedPointCloudIntervalX - ModelParam.IntervalX) < 0.000000001
                && Math.Abs(_loadedPointCloudIntervalY - ModelParam.IntervalY) < 0.000000001
                && Math.Abs(_loadedPointCloudIntervalZ - ModelParam.IntervalZ) < 0.000000001;
        }

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

        private static string GetMeasurementModeName(CalibrationPlateMeasurementMode mode)
        {
            return mode == CalibrationPlateMeasurementMode.Circle
                ? "圆测量"
                : "刻槽测量";
        }
    }
}


using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using Newtonsoft.Json;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;

namespace ALGO.DefectPostProcess.Models
{
    /// <summary>
    /// 实现缺陷后处理模块的核心流程、输入同步与预览刷新逻辑。
    /// </summary>
    [Serializable]
    public partial class DefectPostProcessModel : ModelParamBase
    {
        #region 常量与内部类型

        private static readonly string[] PreviewColors =
        {
            "cyan",
            "magenta",
            "green",
            "yellow",
            "orange",
            "red"
        };
        private const double DefaultEdgeCalibrationX = 0d;
        private const string LogPrefix = "[缺陷后处理]";
        private const string MissingInputParametersMessage = "没有配置输入参数。";

        private const string CalibrationCoordinateSource = "CalibrationFile";

        private static readonly Lazy<Dictionary<string, OutputParamInfo>> OutputParamInfoMap =
            new Lazy<Dictionary<string, OutputParamInfo>>(
                () => OutputParamCollector.GetDataPoints(typeof(DefectPostProcessModel))
                    .GroupBy(item => item.Name ?? string.Empty)
                    .ToDictionary(item => item.Key, item => item.First()),
                LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 存储坐标换算所需的标定上下文数据。
        /// </summary>
        private sealed class CalibrationCoordinateContext
        {
            public string CameraId { get; set; } = string.Empty;
            public double IntervalX { get; set; }
            public double IntervalY { get; set; }
            public double[] Intrinsic { get; set; } = Array.Empty<double>();
            public double[] Distortion { get; set; } = Array.Empty<double>();
            public double[] Extrinsic { get; set; } = Array.Empty<double>();
            public double[] HomographyMatrix { get; set; } = Array.Empty<double>();
            public bool HomographyUsesPixelCoordinates { get; set; }
        }

        #endregion

        #region 字段
        [JsonIgnore]
        public override VMHWindowControl mWindowH { get; set; } = null!;

        [JsonIgnore]
        public new bool IsDebug { get; set; } = false;

        private bool _isFastModeEnabled;
        private ObservableCollection<DefectCustomAlgorithmItem> _customAlgorithmItems = CreateDefaultCustomAlgorithmItems();
        private string _selectedCustomAlgorithmKey = DefectCustomAlgorithmKeys.SheetSizeJudge;
        private SheetSizeJudgeConfig _sheetSizeJudge = new SheetSizeJudgeConfig();
        private bool _hasSheetSizeJudgePreviewResult;
        private string _sheetSizeJudgePreviewStatusText = "未执行";
        private string _sheetSizeJudgePreviewRectangularityText = "--";
        private string _sheetSizeJudgePreviewLengthText = "--";
        private string _sheetSizeJudgePreviewWidthText = "--";
        private HObject _sheetSizeJudgeBeforeRectangle2PreviewRegion;
        private HObject _sheetSizeJudgeAfterRectangle2PreviewRegion;
        private bool _isCustomAlgorithmPopupOpen;
        private bool _isSynchronizingCustomAlgorithmState;

        [JsonIgnore]
        private bool _isDeferringStateRefresh;

        [JsonIgnore]
        private bool _pendingPreviewRefresh;

        [JsonIgnore]
        private bool _pendingFeatureValueDialogRefresh;

        [JsonIgnore]
        [NonSerialized]
        private object _previewImageOwnershipLock;

        [JsonIgnore]
        private string _lastExecutionValidationPrompt = string.Empty;

        [JsonIgnore]
        private bool _ownsInputParamSnapshots;

        [JsonIgnore]
        private CameraCalibrationSdk _cameraCalibrationSdk;

        [JsonIgnore]
        private string _loadedCalibrationFilePath = string.Empty;

        [JsonIgnore]
        private string _calibrationCameraId = string.Empty;

        [JsonIgnore]
        private double _calibrationIntervalX;

        [JsonIgnore]
        private double _calibrationIntervalY;

        [JsonIgnore]
        private CalibrationCoordinateContext _calibrationCoordinateContext;

        [JsonIgnore]
        private List<DefectClassDefinition> _configuredDefectDefinitions = new List<DefectClassDefinition>();

        [JsonIgnore]
        private Dictionary<int, string> _configuredClassNameMap = new Dictionary<int, string>();

        [JsonIgnore]
        private string _manualDefectClassIdText = "0";

        [JsonIgnore]
        private string _manualDefectClassName = string.Empty;

        [JsonIgnore]
        private List<HImage> _inputImages = new List<HImage>();

        [JsonIgnore]
        private HObject _previewImageObject;

        [JsonIgnore]
        private double _previewImageWidth = 1.0d;

        [JsonIgnore]
        private double _previewImageHeight = 1.0d;

        [JsonIgnore]
        private long _previewUpdateVersion;

        [JsonIgnore]
        private int _currentPreviewImageIndex;

        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        #endregion

        #region 属性
        [JsonIgnore]
        public List<Result> Results { get; private set; } = new List<Result>();

        [JsonIgnore]
        [OutputParam("ResultsByImage", "缺陷结果分组，外层序号对应输入图像序号")]
        public List<List<Result>> ResultsByImage { get; private set; } = new List<List<Result>>();

        [JsonIgnore]
        [OutputParam("JudgeResultsByImage", "图像判定结果，外层序号对应输入图像序号")]
        public List<Dictionary<string, object>> JudgeResultsByImage { get; private set; } = new List<Dictionary<string, object>>();

        [JsonIgnore]
        public int CurrentPreviewImageIndex
        {
            get { return _currentPreviewImageIndex; }
            private set
            {
                int normalizedIndex = NormalizePreviewImageIndex(value);
                if (SetProperty(ref _currentPreviewImageIndex, normalizedIndex))
                {
                    RaisePreviewImageNavigationProperties();
                }
            }
        }

        [JsonIgnore]
        public int PreviewImageCount
        {
            get { return Math.Max(_inputImages?.Count ?? 0, SourceResultsByImage?.Count ?? 0); }
        }

        [JsonIgnore]
        public string CurrentPreviewImageDisplayText
        {
            get
            {
                int imageCount = PreviewImageCount;
                return imageCount <= 0
                    ? "0/0"
                    : $"{CurrentPreviewImageIndex + 1}/{imageCount}";
            }
        }

        [JsonIgnore]
        public bool CanMovePreviousPreviewImage
        {
            get { return CurrentPreviewImageIndex > 0; }
        }

        [JsonIgnore]
        public bool CanMoveNextPreviewImage
        {
            get { return CurrentPreviewImageIndex < PreviewImageCount - 1; }
        }

        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get { return _previewImageObject; }
            private set { SetProperty(ref _previewImageObject, value); }
        }

        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new ObservableCollection<HalconDrawingObject>();

        [JsonIgnore]
        public double PreviewImageWidth
        {
            get { return _previewImageWidth; }
            private set { SetProperty(ref _previewImageWidth, Math.Max(1.0d, value)); }
        }

        [JsonIgnore]
        public double PreviewImageHeight
        {
            get { return _previewImageHeight; }
            private set { SetProperty(ref _previewImageHeight, Math.Max(1.0d, value)); }
        }

        /// <summary>
        /// 部署时跳过运行时预览刷新，只保留后处理计算和输出。
        /// </summary>
        public bool IsFastModeEnabled
        {
            get { return _isFastModeEnabled; }
            set
            {
                if (SetProperty(ref _isFastModeEnabled, value))
                {
                    MarkSchemeDirty();
                    if (value)
                    {
                        ClearRuntimePreviewState();
                    }
                    else
                    {
                        RequestEditingStateRefresh();
                    }
                }
            }
        }

        public ObservableCollection<DefectCustomAlgorithmItem> CustomAlgorithmItems
        {
            get { return _customAlgorithmItems; }
            set
            {
                ObservableCollection<DefectCustomAlgorithmItem> newValue = NormalizeCustomAlgorithmItems(value);
                if (ReferenceEquals(_customAlgorithmItems, newValue))
                {
                    return;
                }

                DetachCustomAlgorithmEvents(_customAlgorithmItems);
                if (SetProperty(ref _customAlgorithmItems, newValue))
                {
                    AttachCustomAlgorithmEvents(_customAlgorithmItems);
                    SynchronizeSheetSizeJudgeItemFromConfig(markDirty: false);
                    OnCustomAlgorithmStateChanged();
                }
            }
        }

        public string SelectedCustomAlgorithmKey
        {
            get { return _selectedCustomAlgorithmKey; }
            set
            {
                if (SetProperty(ref _selectedCustomAlgorithmKey, value ?? DefectCustomAlgorithmKeys.SheetSizeJudge))
                {
                    RaisePropertyChanged(nameof(IsSheetSizeJudgeSelected));
                    OnCustomAlgorithmStateChanged();
                }
            }
        }

        public SheetSizeJudgeConfig SheetSizeJudge
        {
            get { return _sheetSizeJudge; }
            set
            {
                SheetSizeJudgeConfig newValue = value ?? new SheetSizeJudgeConfig();
                if (ReferenceEquals(_sheetSizeJudge, newValue))
                {
                    return;
                }

                DetachSheetSizeJudgeEvents(_sheetSizeJudge);
                if (SetProperty(ref _sheetSizeJudge, newValue))
                {
                    AttachSheetSizeJudgeEvents(_sheetSizeJudge);
                    SynchronizeSheetSizeJudgeItemFromConfig(markDirty: false);
                    OnCustomAlgorithmStateChanged();
                }
            }
        }

        [JsonIgnore]
        public bool IsSheetSizeJudgeSelected
        {
            get { return string.Equals(SelectedCustomAlgorithmKey, DefectCustomAlgorithmKeys.SheetSizeJudge, StringComparison.Ordinal); }
        }

        public bool IsCustomAlgorithmPopupOpen
        {
            get { return _isCustomAlgorithmPopupOpen; }
            set { SetProperty(ref _isCustomAlgorithmPopupOpen, value); }
        }

        public bool HasSheetSizeJudgePreviewResult
        {
            get { return _hasSheetSizeJudgePreviewResult; }
            private set { SetProperty(ref _hasSheetSizeJudgePreviewResult, value); }
        }

        public string SheetSizeJudgePreviewStatusText
        {
            get { return _sheetSizeJudgePreviewStatusText; }
            private set { SetProperty(ref _sheetSizeJudgePreviewStatusText, value ?? string.Empty); }
        }

        public string SheetSizeJudgePreviewRectangularityText
        {
            get { return _sheetSizeJudgePreviewRectangularityText; }
            private set { SetProperty(ref _sheetSizeJudgePreviewRectangularityText, value ?? string.Empty); }
        }

        public string SheetSizeJudgePreviewLengthText
        {
            get { return _sheetSizeJudgePreviewLengthText; }
            private set { SetProperty(ref _sheetSizeJudgePreviewLengthText, value ?? string.Empty); }
        }

        public string SheetSizeJudgePreviewWidthText
        {
            get { return _sheetSizeJudgePreviewWidthText; }
            private set { SetProperty(ref _sheetSizeJudgePreviewWidthText, value ?? string.Empty); }
        }

        [JsonIgnore]
        private long _featureValueDialogRefreshToken;
        [JsonIgnore]
        public long FeatureValueDialogRefreshToken
        {
            get { return _featureValueDialogRefreshToken; }
            private set { SetProperty(ref _featureValueDialogRefreshToken, value); }
        }

        private ObservableCollection<DefectItem> _defectItems = new ObservableCollection<DefectItem>();
        public ObservableCollection<DefectItem> DefectItems
        {
            get { return _defectItems; }
            set { SetProperty(ref _defectItems, value); }
        }

        private DefectItem _currentDefect;
        public DefectItem CurrentDefect
        {
            get { return _currentDefect; }
            set
            {
                if (SetProperty(ref _currentDefect, value))
                {
                    LoadCurrentDefectRule();
                    UpdateCurrentDefectSummary();
                    RaisePropertyChanged(nameof(HasCurrentDefect));
                    RaisePropertyChanged(nameof(PreviewSummary));
                    RequestPreviewRefresh();
                }
            }
        }

        /// <summary>
        /// 手动新增缺陷类别时输入的类别 ID，留空时自动取当前方案的下一个 ID。
        /// </summary>
        [JsonIgnore]
        public string ManualDefectClassIdText
        {
            get { return _manualDefectClassIdText; }
            set { SetProperty(ref _manualDefectClassIdText, value ?? string.Empty); }
        }

        /// <summary>
        /// 手动新增缺陷类别时输入的类别名称。
        /// </summary>
        [JsonIgnore]
        public string ManualDefectClassName
        {
            get { return _manualDefectClassName; }
            set { SetProperty(ref _manualDefectClassName, value ?? string.Empty); }
        }

        private string _ruleName = string.Empty;
        public string RuleName
        {
            get { return _ruleName; }
            set { SetProperty(ref _ruleName, value); }
        }

        private string _defectName = string.Empty;
        public string DefectName
        {
            get { return _defectName; }
            set { SetProperty(ref _defectName, value); }
        }

        private int _sourceClassId;
        public int SourceClassId
        {
            get { return _sourceClassId; }
            set { SetProperty(ref _sourceClassId, value); }
        }

        private int _outputClassId;
        public int OutputClassId
        {
            get { return _outputClassId; }
            set { SetProperty(ref _outputClassId, value); }
        }

        private string _sourceClassName = string.Empty;
        public string SourceClassName
        {
            get { return _sourceClassName; }
            set { SetProperty(ref _sourceClassName, value); }
        }

        private string _outputClassName = string.Empty;
        public string OutputClassName
        {
            get { return _outputClassName; }
            set { SetProperty(ref _outputClassName, value); }
        }

        private double _currentMinimumConfidence;
        public double CurrentMinimumConfidence
        {
            get { return _currentMinimumConfidence; }
            set { SetProperty(ref _currentMinimumConfidence, value); }
        }

        private double _minimumConfidence;
        public double MinimumConfidence
        {
            get { return _minimumConfidence; }
            set
            {
                double clamped = Math.Clamp(value, 0d, 1d);
                if (SetProperty(ref _minimumConfidence, clamped))
                {
                    if (_isLoadingCurrentRule)
                    {
                        return;
                    }

                    DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
                    if (currentRuleConfig != null)
                    {
                        currentRuleConfig.MinimumConfidence = clamped;
                    }

                    MarkSchemeDirty();
                    RequestEditingStateRefresh();
                }
            }
        }

        private bool _isNmsEnabled = true;
        public bool IsNmsEnabled
        {
            get { return _isNmsEnabled; }
            set
            {
                if (SetProperty(ref _isNmsEnabled, value))
                {
                    if (_isLoadingCurrentRule)
                    {
                        return;
                    }

                    DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
                    if (currentRuleConfig != null)
                    {
                        currentRuleConfig.IsNmsEnabled = value;
                    }

                    MarkSchemeDirty();
                    BuildDefectItems();
                    RequestEditingStateRefresh();
                }
            }
        }

        private double _nmsIoUThreshold = 0.5d;
        public double NmsIoUThreshold
        {
            get { return _nmsIoUThreshold; }
            set
            {
                double clamped = Math.Clamp(value, 0d, 1d);
                if (SetProperty(ref _nmsIoUThreshold, clamped))
                {
                    if (_isLoadingCurrentRule)
                    {
                        return;
                    }

                    DefectRuleConfig currentRuleConfig = GetCurrentRuleConfig();
                    if (currentRuleConfig != null)
                    {
                        currentRuleConfig.NmsIoUThreshold = clamped;
                    }

                    MarkSchemeDirty();
                    BuildDefectItems();
                    RequestEditingStateRefresh();
                }
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化缺陷后处理模型的默认输入、输出与预览状态。
        /// </summary>
        public DefectPostProcessModel()
        {
            AttachFeatureThresholdEvents(FeatureThresholds);
            AttachCustomAlgorithmEvents(CustomAlgorithmItems);
            AttachSheetSizeJudgeEvents(SheetSizeJudge);
            SynchronizeSheetSizeJudgeItemFromConfig(markDirty: false);

            if (TriggerModuleRun == null)
            {
                TriggerModuleRun = () =>
                {
                    return ExecuteModule().GetAwaiter().GetResult();
                };
            }
        }
        #endregion

        #region 生命周期
        public override bool OnceInit()
        {
            if (IsOnceInit)
            {
                return true;
            }

            if (!base.OnceInit())
            {
                return false;
            }

            TriggerModuleRun ??= () => ExecuteModule().GetAwaiter().GetResult();
            IsOnceInit = true;
            return true;
        }

        public override void Dispose()
        {
            DetachFeatureThresholdEvents(FeatureThresholds);
            DetachCustomAlgorithmEvents(CustomAlgorithmItems);
            DetachSheetSizeJudgeEvents(SheetSizeJudge);

            DisposeRuntimeImages();
            ClearSheetSizeJudgePreviewRegions();
            DisposeResultOutputs(Results, ResultsByImage);
            Results = new List<Result>();
            ResultsByImage = new List<List<Result>>();
            JudgeResultsByImage = new List<Dictionary<string, object>>();
            ClearInputParamSnapshot();

            _cameraCalibrationSdk?.Dispose();
            _cameraCalibrationSdk = null;
            mWindowH?.Dispose();

            base.Dispose();
        }

        private void DisposeRuntimeImages()
        {
            ClearPreviewDisplay();
            ClearCurrentPreviewImage();
            DisposeImageList(_inputImages);
            _inputImages = new List<HImage>();
            DisposeInputImageValueList();
        }

        private void DisposeInputImageValueList()
        {
            if (_inputImage?.Value is IEnumerable<HImage> images
                && !ReferenceEquals(images, _inputImages))
            {
                DisposeImageList(images);
            }

            if (_inputImage != null)
            {
                _inputImage.Value = null;
            }
        }

        private static void DisposeImageList(IEnumerable<HImage> images)
        {
            HalconImageOwnership.DisposeOwnedList(images);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 开始延迟页面态刷新，避免同一轮输入加载过程中重复重算和重绘。
        /// </summary>
        private void BeginDeferredStateRefresh()
        {
            _isDeferringStateRefresh = true;
            _pendingPreviewRefresh = false;
            _pendingFeatureValueDialogRefresh = false;
        }

        /// <summary>
        /// 结束延迟页面态刷新，并按需一次性补发预览与特征窗口刷新。
        /// </summary>
        private void EndDeferredStateRefresh(bool flushQueuedRefresh)
        {
            bool shouldRefreshPreview = flushQueuedRefresh && _pendingPreviewRefresh;
            bool shouldRefreshFeatureDialog = flushQueuedRefresh && _pendingFeatureValueDialogRefresh;

            _isDeferringStateRefresh = false;
            _pendingPreviewRefresh = false;
            _pendingFeatureValueDialogRefresh = false;

            if (shouldRefreshFeatureDialog && !IsFastModeEnabled)
            {
                NotifyFeatureValueDialogChanged();
            }

            if (shouldRefreshPreview && !IsFastModeEnabled)
            {
                RefreshPreview();
            }
        }

        /// <summary>
        /// 请求刷新预览；若当前处于批量加载阶段，则延后到结束时统一刷新。
        /// </summary>
        private void RequestPreviewRefresh()
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            if (_isDeferringStateRefresh)
            {
                _pendingPreviewRefresh = true;
                return;
            }

            RefreshPreview();
        }

        /// <summary>
        /// 请求刷新编辑态预览和特征值窗口，不提交正式输出。
        /// </summary>
        private void RequestEditingStateRefresh()
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            if (_isDeferringStateRefresh)
            {
                _pendingPreviewRefresh = true;
                _pendingFeatureValueDialogRefresh = true;
                return;
            }

            NotifyFeatureValueDialogChanged();
            RefreshPreview();
        }

        /// <summary>
        /// 应用当前规则并刷新测量输出。
        /// </summary>
        private void ApplyPostProcessResults()
        {
            List<Result> previousResults = Results;
            List<List<Result>> previousResultsByImage = ResultsByImage;

            Results = BuildFilteredResults();
            ResultsByImage = BuildResultGroupsByImageIndex(Results);
            JudgeResultsByImage = BuildJudgeResultsByImageSkeleton();
            ApplyCustomAlgorithmStage();
            RefreshMeasurementOutputsSafely();

            RaisePropertyChanged(nameof(Results));
            RaisePropertyChanged(nameof(ResultsByImage));
            RaisePropertyChanged(nameof(JudgeResultsByImage));

            if (!IsFastModeEnabled)
            {
                NotifyFeatureValueDialogChanged();
            }

            DisposeResultOutputs(previousResults, previousResultsByImage);
        }

        /// <summary>
        /// 清空已生成的后处理输出，避免校验失败时继续保留旧结果。
        /// </summary>
        private void ClearPostProcessOutputs()
        {
            List<Result> previousResults = Results;
            List<List<Result>> previousResultsByImage = ResultsByImage;

            Results = new List<Result>();
            ResultsByImage = new List<List<Result>>();
            JudgeResultsByImage = new List<Dictionary<string, object>>();
            RaisePropertyChanged(nameof(Results));
            RaisePropertyChanged(nameof(ResultsByImage));
            RaisePropertyChanged(nameof(JudgeResultsByImage));

            ClearMeasurementOutputs();

            if (!IsFastModeEnabled)
            {
                NotifyFeatureValueDialogChanged();
            }

            DisposeResultOutputs(previousResults, previousResultsByImage);
        }

        /// <summary>
        /// 应用当前后处理结果，并同步刷新预览图像。
        /// </summary>
        private void ApplyPostProcessResultsAndRefreshPreview()
        {
            ApplyPostProcessResults();
            if (!IsFastModeEnabled)
            {
                RefreshPreview(preferExecutionResults: true);
            }
        }

        private void ClearMeasurementOutputs()
        {
            RunOnDispatcher(() =>
            {
                DefectMeasurements.Clear();
                RaisePropertyChanged(nameof(DefectMeasurements));
            });
        }

        /// <summary>
        /// 执行后处理规则并刷新筛选输出。
        /// </summary>
        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            long runId = DateTime.UtcNow.Ticks;
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                CancellationTokenSource watchdog = null;
                try
                {
                    if (IsVerboseFlowLogEnabled())
                    {
                        LogInfo(
                            $"开始执行: RunId={runId}, InputImage={DescribeDefectTransmitParamForLog(_inputImage)}, " +
                            $"InputResults={DescribeDefectTransmitParamForLog(_inputResults)}, FastMode={IsFastModeEnabled}");
                    }

                    RuntimeEditingState editingState = CaptureRuntimeEditingState();
                    bool loadParamResult = LoadKeyParam(flushDeferredStateRefresh: false);
                    if (IsVerboseFlowLogEnabled())
                    {
                        LogInfo(
                            $"参数加载: RunId={runId}, Result={loadParamResult}, " +
                            $"InputParams={DescribeDefectTransmitParamsForLog(InputParams)}");
                    }

                    if (!loadParamResult)
                    {
                        RestoreRuntimeEditingState(editingState);
                        ClearPostProcessOutputs();
                        LogWarning($"执行校验: RunId={runId}, 参数加载失败");
                        return NodeStatus.Error;
                    }

                    RestoreRuntimeEditingState(editingState);
                    if (IsVerboseFlowLogEnabled())
                    {
                        LogInfo(
                            $"输入状态: RunId={runId}, InputImages={_inputImages?.Count ?? 0}, PreviewImageReady={HasCurrentPreviewImage()}, " +
                            $"SourceResults={SourceResults?.Count ?? 0}, ResultsValue={_inputResults?.Value?.GetType().Name ?? "null"}, " +
                            $"CustomAlgorithm={HasEnabledCustomAlgorithm()}, FastMode={IsFastModeEnabled}");
                    }

                    if (!ValidateExecutionRequirements())
                    {
                        ClearPostProcessOutputs();
                        NodeStatus validationStatus = ShouldReturnNotRunForValidationFailure()
                            ? NodeStatus.NotRun
                            : NodeStatus.None;
                        LogWarning($"执行校验: RunId={runId}, Result=False, Status={validationStatus}");
                        return validationStatus;
                    }

                    LogInfo($"执行校验: RunId={runId}, Result=True");
                    watchdog = StartPostProcessWatchdog(runId);
                    var applyStopwatch = Stopwatch.StartNew();
                    ApplyPostProcessResultsAndRefreshPreview();
                    watchdog.Cancel();
                    applyStopwatch.Stop();
                    if (IsVerboseFlowLogEnabled())
                    {
                        LogInfo(
                            $"后处理结果: RunId={runId}, Cost={applyStopwatch.Elapsed.TotalMilliseconds:F3}ms, Results={Results?.Count ?? 0}, " +
                            $"ResultsByImage={ResultsByImage?.Count ?? 0}, JudgeResultsByImage={JudgeResultsByImage?.Count ?? 0}");
                    }
                    ResetExecutionValidationPrompt();

                    if (!HasCurrentPreviewImage())
                    {
                        ClearPostProcessOutputs();
                        LogWarning($"执行返回 NotRun: RunId={runId}, DisposeImage 无效");
                        return NodeStatus.NotRun;
                    }

                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    watchdog?.Cancel();
                    LogError($"执行失败: RunId={runId}, Error={ex}");
                    return NodeStatus.Error;
                }
                finally
                {
                    watchdog?.Cancel();
                }
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };

            try
            {
                UpdateOutputParamValues();

                if (!UpdateParam())
                {
                    LogWarning($"输出刷新: RunId={runId}, 更新参数失败");
                }
                else
                {
                    if (IsVerboseFlowLogEnabled())
                    {
                        LogInfo(
                            $"输出刷新: RunId={runId}, OutputParams={DescribeDefectTransmitParamsForLog(OutputParams)}, " +
                            $"Status={result}, Time={time}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"输出刷新异常: RunId={runId}, Error={ex}");
                Output.RunStatus = NodeStatus.Error;
            }

            return Task.FromResult(Output);
        }

        private RuntimeEditingState CaptureRuntimeEditingState()
        {
            SyncCurrentRuleConfigFromEditingState();

            return new RuntimeEditingState
            {
                DefectRuleConfigs = CloneRuleConfigs(DefectRuleConfigs),
                SheetSizeJudge = CloneSheetSizeJudgeConfig(SheetSizeJudge),
                IsFastModeEnabled = IsFastModeEnabled,
                PreferredRuleKey = GetDefectRuleKey(CurrentDefect),
                IsSchemeDirty = IsSchemeDirty
            };
        }

        private void RestoreRuntimeEditingState(RuntimeEditingState state)
        {
            if (state == null)
            {
                return;
            }

            DefectRuleConfigs = CloneRuleConfigs(state.DefectRuleConfigs);
            SheetSizeJudge = CloneSheetSizeJudgeConfig(state.SheetSizeJudge);
            IsFastModeEnabled = state.IsFastModeEnabled;
            RefreshSchemeRuleBindings(state.PreferredRuleKey);
            IsSchemeDirty = state.IsSchemeDirty;
            NotifySchemeStateChanged();
        }

        /// <summary>
        /// 校验模块执行前必需的输入参数。
        /// </summary>
        private bool ValidateExecutionRequirements()
        {
            if (ShouldUseCustomAlgorithmOnlyRequirements())
            {
                return ValidateCustomAlgorithmOnlyRequirements();
            }

            if (!TryGetExecutionValidationMessage(out string message))
            {
                return true;
            }

            RunOnDispatcher(() =>
            {
                if (ShouldShowExecutionValidationPrompt(message))
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "提示",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            });

            return false;
        }

        private bool ShouldShowExecutionValidationPrompt(string message)
        {
            string normalizedMessage = message ?? string.Empty;
            if (string.Equals(_lastExecutionValidationPrompt, normalizedMessage, StringComparison.Ordinal))
            {
                return false;
            }

            _lastExecutionValidationPrompt = normalizedMessage;
            return true;
        }

        private void ResetExecutionValidationPrompt()
        {
            _lastExecutionValidationPrompt = string.Empty;
        }

        /// <summary>
        /// 生成执行前校验提示信息。
        /// </summary>
        private bool TryGetExecutionValidationMessage(out string message)
        {
            message = string.Empty;

            if (HasEmptyDefectResultsWithValidImageContext())
            {
                return false;
            }

            if (_inputResults?.Value == null)
            {
                message = MissingInputParametersMessage;
                return true;
            }

            bool hasCalibrationSupport = HasCalibrationMeasurementSupport();

            if (!hasCalibrationSupport)
            {
                message = BuildMeasurementValidationMessage();
                return true;
            }

            return false;
        }

        private bool ShouldReturnNotRunForValidationFailure()
        {
            if (!ShouldUseCustomAlgorithmOnlyRequirements())
            {
                return false;
            }

            return TryGetCustomAlgorithmOnlyValidationMessage(out string message)
                && IsCustomAlgorithmMissingInputImageMessage(message);
        }

        private bool ShouldUseCustomAlgorithmOnlyRequirements()
        {
            return HasEnabledCustomAlgorithm()
                && !HasAnyDefectResults();
        }

        private bool HasEnabledCustomAlgorithm()
        {
            return SheetSizeJudge?.IsEnabled == true
                || CustomAlgorithmItems?.Any(item => item?.IsEnabled == true) == true;
        }

        private bool HasAnyDefectResults()
        {
            return ExtractLinkedResultGroups(_inputResults?.Value)
                .Any(group => group != null && group.Any(IsProcessableResult));
        }

        private bool HasEmptyDefectResultsWithValidImageContext()
        {
            return !HasAnyDefectResults()
                && HasConfiguredDefectResultInput()
                && HasValidInputImages();
        }

        private bool HasConfiguredDefectResultInput()
        {
            return _inputResults?.Value != null
                || HasInputBindingMetadata(InputResultsBinding)
                || HasTransmitParamMetadata(_inputResults);
        }

        private static bool HasTransmitParamMetadata(TransmitParam param)
        {
            if (param == null)
            {
                return false;
            }

            return param.IsLink
                || param.LinkGuid != Guid.Empty
                || param.Serial >= 0
                || !string.IsNullOrWhiteSpace(param.ParentNode)
                || param.Guid != Guid.Empty
                || param.Resourece != ResoureceType.None
                || !string.IsNullOrWhiteSpace(param.Name)
                || !string.IsNullOrWhiteSpace(param.ParamName)
                || param.Type != DataType.None
                || !string.IsNullOrWhiteSpace(param.Describe)
                || param.IsGlobal
                || !string.IsNullOrWhiteSpace(param.ResourcePath);
        }

        private bool ValidateCustomAlgorithmOnlyRequirements()
        {
            if (!TryGetCustomAlgorithmOnlyValidationMessage(out string message))
            {
                return true;
            }

            if (IsCustomAlgorithmMissingInputImageMessage(message))
            {
                return false;
            }

            RunOnDispatcher(() =>
            {
                if (ShouldShowExecutionValidationPrompt(message))
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "提示",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            });

            return false;
        }

        private static bool IsCustomAlgorithmMissingInputImageMessage(string message)
        {
            return string.Equals(message, "启用定制算法时，请先输入图像。", StringComparison.Ordinal);
        }

        private bool TryGetCustomAlgorithmOnlyValidationMessage(out string message)
        {
            message = string.Empty;

            if (!HasValidInputImages())
            {
                message = "启用定制算法时，请先输入图像。";
                return true;
            }

            if (!TryGetSheetSizePixelEquivalent(out _, out _))
            {
                message = BuildMeasurementValidationMessage();
                return true;
            }

            return false;
        }

        private bool HasValidInputImages()
        {
            if (_inputImages?.Any(IsInitializedSafely) == true)
            {
                return true;
            }

            if (_inputImage?.Value is HObject hObject)
            {
                return IsInitializedSafely(hObject) && hObject.CountObj() > 0;
            }

            return _inputImage?.Value is IEnumerable<HImage> images
                && images.Any(IsInitializedSafely);
        }

        /// <summary>
        /// 构建测量基准缺失时的提示语。
        /// </summary>
        private string BuildMeasurementValidationMessage()
        {
            return MissingInputParametersMessage;
        }

        #endregion
    }
}

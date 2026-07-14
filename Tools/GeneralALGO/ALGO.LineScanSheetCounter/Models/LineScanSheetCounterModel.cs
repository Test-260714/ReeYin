using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.LineScanSheetCounter.Models;

/// <summary>
/// 线扫片材计数模块参数模型。
/// 负责参数加载、算法调用、结果缓存、图像预览和输出参数刷新。
/// </summary>
[Serializable]
public sealed class LineScanSheetCounterModel : ModelParamBase
{
    #region 字段：运行时状态
    private const string LogPrefix = "[线扫计数]";
    private const double MaxRuntimeRemainHeightFactor = 2.0;

    private readonly LineScanSheetCounterAlgorithm _algorithm = new();

    [JsonIgnore]
    private TransmitParam _inputImage = CreateInputImageParam();

    [JsonIgnore]
    private TransmitParam _maskImage = CreateMaskImageParam();

    [JsonIgnore]
    private HImage? _runtimeRemainImage;

    [JsonIgnore]
    private HImage? _runtimeRemainMaskImage;

    [JsonIgnore]
    private HObject? _runtimeCroppedImages;

    [JsonIgnore]
    private readonly List<HImage> _runtimeTargetImages = [];

    private double _scaleFactor = 0.3;
    private double _smoothSigma = 30.0;
    private double _edgeThreshold = 40.0;
    private double _measureCenterColumn;
    private double _measureRoiWidth;
    private double _cropRatio = 0.1;
    private double _previewImageWidth = 1000.0;
    private double _previewRoiWidthMaximum = 200.0;
    private HObject? _previewImageObject;
    private bool _isFastModeEnabled;
    private bool _isRefreshingPreview;
    private bool _runtimePreviewEnabled;
    private bool _ownsInputImageValue;
    private bool _ownsMaskImageValue;
    private long _previewUpdateVersion;
    private long _executionLogSequence;
    private readonly object _executionSyncRoot = new();

    #endregion

    #region 构造与基础属性
    public LineScanSheetCounterModel()
    {
    }

    [JsonIgnore]
    public HObject? PreviewImageObject
    {
        get => _previewImageObject;
        private set => SetProperty(ref _previewImageObject, value);
    }

    [JsonIgnore]
    public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = [];

    [JsonIgnore]
    public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

    [JsonIgnore]
    public new ExecuteModuleOutput Output { get; set; } = new()
    {
        RunStatus = NodeStatus.NotRun,
        RunTime = 0
    };

    #endregion

    #region 输入参数

    /// <summary>
    /// 输入线扫图像，支持从上游模块链接 HObject。
    /// </summary>
    public TransmitParam InputImage
    {
        get
        {
            _inputImage ??= CreateInputImageParam();
            EnsureInputImageParamMetadata(_inputImage);
            return _inputImage;
        }
        set
        {
            lock (_executionSyncRoot)
            {
                DisposeOwnedInputImageValue();
                _inputImage = value ?? CreateInputImageParam();
                EnsureInputImageParamMetadata(_inputImage);
                _ownsInputImageValue = false;
                ResolveAndApplyInputImage();
            }

            RaisePropertyChanged();
        }
    }

    public TransmitParam MaskImage
    {
        get
        {
            _maskImage ??= CreateMaskImageParam();
            EnsureMaskImageParamMetadata(_maskImage);
            return _maskImage;
        }
        set
        {
            lock (_executionSyncRoot)
            {
                DisposeOwnedMaskImageValue();
                _maskImage = value ?? CreateMaskImageParam();
                EnsureMaskImageParamMetadata(_maskImage);
                _ownsMaskImageValue = false;
                ResolveAndApplyMaskImage();
            }

            RaisePropertyChanged();
        }
    }

    #endregion

    #region 算法设置

    /// <summary>
    /// 测量前缩放比例，用于提升边缘检测速度。
    /// </summary>
    public double ScaleFactor
    {
        get => _scaleFactor;
        set => SetMeasurementParameter(ref _scaleFactor, Math.Clamp(value, 0.05, 1.0), nameof(ScaleFactor));
    }

    /// <summary>
    /// MeasurePos 平滑系数。
    /// </summary>
    public double SmoothSigma
    {
        get => _smoothSigma;
        set => SetMeasurementParameter(ref _smoothSigma, Math.Max(0.0, value), nameof(SmoothSigma));
    }

    /// <summary>
    /// MeasurePos 边缘阈值。
    /// </summary>
    public double EdgeThreshold
    {
        get => _edgeThreshold;
        set => SetMeasurementParameter(ref _edgeThreshold, Math.Max(1.0, value), nameof(EdgeThreshold));
    }

    /// <summary>
    /// 卡尺中心列坐标，滑动时实时刷新预览检测结果。
    /// </summary>
    public double MeasureCenterColumn
    {
        get => _measureCenterColumn;
        set => SetMeasurementParameter(ref _measureCenterColumn, Math.Max(0.0, value), nameof(MeasureCenterColumn));
    }

    /// <summary>
    /// 卡尺 ROI 宽度，滑动时实时刷新预览检测结果。
    /// </summary>
    public double MeasureRoiWidth
    {
        get => _measureRoiWidth;
        set => SetMeasurementParameter(ref _measureRoiWidth, Math.Max(1.0, value), nameof(MeasureRoiWidth));
    }

    /// <summary>
    /// 片材裁剪比例，滑动时实时刷新预览裁剪位置。
    /// </summary>
    public double CropRatio
    {
        get => _cropRatio;
        set => SetMeasurementParameter(ref _cropRatio, Math.Clamp(value, 0.0, 1.0), nameof(CropRatio));
    }

    /// <summary>
    /// 下次执行前是否清空累计计数和残留图像。
    /// </summary>
    public bool ResetBeforeNextRun { get; set; }

    /// <summary>
    /// 部署时跳过预览、绘制和保存，只保留算法计数与必要输出。
    /// </summary>
    public bool IsFastModeEnabled
    {
        get => _isFastModeEnabled;
        set => SetProperty(ref _isFastModeEnabled, value);
    }

    /// <summary>
    /// 是否保存本次裁剪出的单张片材图。
    /// </summary>
    public bool SaveSheetImages { get; set; }

    /// <summary>
    /// 是否保存本次用于检测的拼接图。
    /// </summary>
    public bool SaveConcatImage { get; set; }

    /// <summary>
    /// 是否保存本次留到下一帧继续拼接的残留图。
    /// </summary>
    public bool SaveRemainImage { get; set; }

    /// <summary>
    /// 图像保存目录。
    /// </summary>
    public string SaveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 当前预览图像宽度，用作卡尺列坐标滑动条上限。
    /// </summary>
    public double PreviewImageWidth
    {
        get => _previewImageWidth;
        private set
        {
            if (Math.Abs(_previewImageWidth - value) < 0.001)
            {
                return;
            }

            _previewImageWidth = value;
            RaisePropertyChanged(nameof(PreviewImageWidth));
        }
    }

    /// <summary>
    /// ROI 宽度滑动条上限。
    /// </summary>
    public double PreviewRoiWidthMaximum
    {
        get => _previewRoiWidthMaximum;
        private set
        {
            if (Math.Abs(_previewRoiWidthMaximum - value) < 0.001)
            {
                return;
            }

            _previewRoiWidthMaximum = value;
            RaisePropertyChanged(nameof(PreviewRoiWidthMaximum));
        }
    }

    #endregion

    #region 输出结果

    [OutputParam("IncrementCount", "本次新增数量")]
    public int IncrementCount { get; private set; }

    public int TotalCount { get; private set; }

    [JsonIgnore]
    public List<HImage> TargetImages { get; private set; } = [];

    [JsonIgnore]
    [OutputParam("CroppedImages", "本次裁剪片材图像集合")]
    public HObject? CroppedImages { get; private set; }

    [JsonIgnore]
    public HImage? RemainImage { get; private set; }

    public double[] EdgeRows { get; private set; } = [];

    [JsonIgnore]
    public HImage? LastConcatImage { get; private set; }

    public string StatusText { get; private set; } = "等待输入线扫图像。";

    #endregion

    #region 参数加载

    public override bool OnceInit()
    {
        try
        {
            if (IsOnceInit)
            {
                return true;
            }

            if (!base.OnceInit())
            {
                return false;
            }

            Task.Run(() =>
            {
                EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);

            });

            if (TriggerModuleRun == null)
            {
                TriggerModuleRun = () =>
                {
                    return ExecuteModule().Result;
                };
            }

            IsOnceInit = true;
            return true;
        }
        catch (Exception)
        {

            throw;
        }
    }

    /// <summary>
    /// 从上游链接加载输入图像，并同步到预览窗口。
    /// </summary>
    public override bool LoadKeyParam()
    {
        try
        {
            base.LoadKeyParam();
            ModuleName = Serial.ToString("D3");
            EnsureInputImageParamMetadata(_inputImage);
            EnsureMaskImageParamMetadata(_maskImage);

            ResolveAndApplyInputImage();
            ResolveAndApplyMaskImage();

            return true;
        }
        catch (Exception ex)
        {
            LogWarning($"加载参数失败: {ex.Message}");
            StatusText = $"加载输入图像失败：{ex.Message}";
            RaisePropertyChanged(nameof(StatusText));
            return false;
        }
    }

    #endregion

    #region 执行与重置
    /// <summary>
    /// 执行线扫片材计数。
    /// 只有用户显式重置或勾选下次执行前重置时，才清空计数和残留图像。
    /// </summary>
    public Task<ExecuteModuleOutput> ExecuteModule(bool resetBeforeRun = false, bool clearRemainBeforeRun = false)
    {
        lock (_executionSyncRoot)
        {
            long runId = System.Threading.Interlocked.Increment(ref _executionLogSequence);
            var (status, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    bool isSchemeFlowExecution = IsSchemeFlowExecution();
                    LogTrace(
                        $"开始执行: RunId={runId}, Serial={Serial}, SchemeFlow={isSchemeFlowExecution}, " +
                        $"FastMode={IsFastModeEnabled}, RuntimePreview={_runtimePreviewEnabled}, ResetBeforeNextRun={ResetBeforeNextRun}, " +
                        $"SaveSheet={SaveSheetImages}, SaveConcat={SaveConcatImage}, SaveRemain={SaveRemainImage}");

                    if (resetBeforeRun || ResetBeforeNextRun)
                    {
                        LogInfo($"执行前重置: RunId={runId}");
                        ResetCounter();
                    }
                    else if (clearRemainBeforeRun)
                    {
                        LogInfo($"手动执行前清空残留图像: RunId={runId}");
                        ClearRuntimeRemainImages();
                    }

                    if (!LoadKeyParam())
                    {
                        LogWarning($"输入刷新失败，跳过本次执行: RunId={runId}");
                        StatusText = "未读取到有效的线扫图像。";
                        RaisePropertyChanged(nameof(StatusText));
                        return NodeStatus.None;
                    }

                    LogTrace(
                        $"输入图像: RunId={runId}, Input={DescribeImage(_inputImage?.Value as HObject)}, " +
                        $"Mask={DescribeImage(_maskImage?.Value as HObject)}");
                    LogInputSourceDiagnostics(runId);
                    LogMemoryCheckpoint(runId, "before-input-clone");

                    if (!TryGetInitializedHObject(_inputImage?.Value, out HObject? inputObject))
                    {
                        LogWarning($"输入图像无效: RunId={runId}, InputParam={DescribeTransmitParam(_inputImage)}");
                        StatusText = "未链接有效的线扫图像。";
                        RaisePropertyChanged(nameof(StatusText));
                        return NodeStatus.None;
                    }

                    LineScanSheetCounterParams parameters = BuildParameters();
                    if (!TryResolveProcessImage(inputObject, out HImage? input, out bool ownsInput, out string inputCopyError))
                    {
                        LogWarning(
                            $"线扫图像复制失败: RunId={runId}, Input={DescribeImage(inputObject)}, " +
                            $"Error={inputCopyError}");
                        StatusText = $"线扫图像复制失败：{inputCopyError}";
                        RaisePropertyChanged(nameof(StatusText));
                        return NodeStatus.None;
                    }

                    if (input == null)
                    {
                        LogWarning($"线扫图像解析为空: RunId={runId}");
                        StatusText = "线扫图像解析为空。";
                        RaisePropertyChanged(nameof(StatusText));
                        return NodeStatus.None;
                    }

                    HImage? mask = null;
                    bool ownsMask = false;
                    try
                    {
                        bool shouldDrawRuntimePreview = !IsFastModeEnabled && (!isSchemeFlowExecution || _runtimePreviewEnabled);
                        bool shouldSaveRuntimeImages = !IsFastModeEnabled;
                        bool shouldCreatePreviewImages = shouldDrawRuntimePreview || (shouldSaveRuntimeImages && SaveConcatImage);
                        if (TryGetInitializedHObject(_maskImage?.Value, out HObject? maskObject))
                        {
                            if (!TryResolveProcessImage(maskObject, out mask, out ownsMask, out string maskCopyError))
                            {
                                LogWarning(
                                    $"掩膜图像复制失败: RunId={runId}, Mask={DescribeImage(maskObject)}, " +
                                    $"Error={maskCopyError}");
                                StatusText = $"掩膜图像复制失败：{maskCopyError}";
                                RaisePropertyChanged(nameof(StatusText));
                                return NodeStatus.None;
                            }
                        }

                        ClearRuntimeResultImages();
                        LogMemoryCheckpoint(runId, "after-clear-last-result");
                        ValidateRuntimeRemainImagesBeforeProcess(runId, mask != null, input);
                        LogTrace(
                            $"残留状态: RunId={runId}, Remain={DescribeImage(_runtimeRemainImage)}, " +
                            $"RemainMask={DescribeImage(_runtimeRemainMaskImage)}, HasMask={mask != null}, " +
                            $"DrawPreview={shouldDrawRuntimePreview}, SaveImages={shouldSaveRuntimeImages}, CreatePreviewImages={shouldCreatePreviewImages}");
                        LogMemoryCheckpoint(runId, "before-algorithm-process");

                        using LineScanSheetCounterResult result = _algorithm.Process(
                            _runtimeRemainImage,
                            input,
                            _runtimeRemainMaskImage,
                            mask,
                            parameters,
                            createPreviewImages: shouldCreatePreviewImages);

                        LogTrace(
                            $"算法结果: RunId={runId}, Increment={result.IncrementCount}, EdgeRows={result.EdgeRows.Length}, " +
                            $"CropRanges={result.CropRanges.Count}, Remain={DescribeImage(result.SourceRemainImage ?? result.RemainImage)}, " +
                            $"RemainMask={DescribeImage(result.RemainMaskImage)}, LastConcat={DescribeImage(result.LastConcatImage)}");
                        LogMemoryCheckpoint(runId, "after-algorithm-process");

                        if (shouldDrawRuntimePreview)
                        {
                            ApplyResult(result);
                        }
                        else
                        {
                            ApplyFastResult(result, shouldSaveRuntimeImages);
                        }
                        LogMemoryCheckpoint(runId, "after-apply-result");
                    }
                    finally
                    {
                        if (ownsMask)
                        {
                            HalconImageOwnership.DisposeOwned(mask);
                        }

                        if (ownsInput)
                        {
                            HalconImageOwnership.DisposeOwned(input);
                        }
                    }

                    StatusText = $"本次新增 {IncrementCount}，累计 {TotalCount}。";
                    RaisePropertyChanged(nameof(StatusText));
                    if (IsFastModeEnabled)
                    {
                        RefreshOutputParamsFast();
                    }
                    else
                    {
                        RefreshOutputParams();
                    }

                    if (IncrementCount <= 0)
                    {
                        StatusText = $"本次未形成完整片材，继续累计，累计 {TotalCount}。";
                        RaisePropertyChanged(nameof(StatusText));
                        LogTrace($"执行完成: RunId={runId}, Increment={IncrementCount}, Total={TotalCount}, Status=Waiting");
                        return NodeStatus.Waiting;
                    }

                    LogTrace($"执行完成: RunId={runId}, Increment={IncrementCount}, Total={TotalCount}, Status=Success");
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    LogError($"执行失败: RunId={runId}, Error={ex}");
                    StatusText = $"线扫片材计数失败：{ex.Message}";
                    RaisePropertyChanged(nameof(StatusText));
                    return NodeStatus.Error;
                }
            });


            Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = time
            };
            RaisePropertyChanged(nameof(Output));
            return Task.FromResult(Output);
        }
    }

    /// <summary>
    /// 清空计数、残留图像和输出缓存。
    /// </summary>
    public void ResetCounter()
    {
        LogInfo($"重置计数和残留图像: TotalBefore={TotalCount}, IncrementBefore={IncrementCount}, Remain={DescribeImage(_runtimeRemainImage)}, RemainMask={DescribeImage(_runtimeRemainMaskImage)}");
        IncrementCount = 0;
        TotalCount = 0;
        EdgeRows = [];
        StatusText = "计数已重置。";
        ClearRuntimeImages();
        RefreshOutputProperties();
        RefreshOutputParams();
    }

    public override void Dispose()
    {
        ClearRuntimeImages();
        ClearPreviewDisplay();
        DisposeOwnedInputImageValue();
        DisposeOwnedMaskImageValue();
        base.Dispose();
    }

    #endregion

    #region 参数构建

    /// <summary>
    /// 构建算法参数，并限制现场可配置值的有效范围。
    /// </summary>
    private LineScanSheetCounterParams BuildParameters()
    {
        return new LineScanSheetCounterParams
        {
            ScaleFactor = Math.Clamp(ScaleFactor, 0.05, 1.0),
            SmoothSigma = Math.Max(0.0, SmoothSigma),
            EdgeThreshold = Math.Max(1.0, EdgeThreshold),
            MeasureCenterColumn = Math.Max(0.0, MeasureCenterColumn),
            MeasureRoiWidth = Math.Max(0.0, MeasureRoiWidth),
            CropRatio = Math.Clamp(CropRatio, 0.0, 1.0)
        };
    }

    private HImage? CreateCurrentMaskImage()
    {
        return _maskImage?.Value is HObject maskObject && TryCloneInitializedImage(maskObject, out HImage? imageCopy)
            ? imageCopy
            : null;
    }

    private bool ResolveAndApplyInputImage()
    {
        object? imageValue = ResolveLinkedImageValue(_inputImage);
        if (!TryCloneInitializedImage(imageValue, out HImage? imageCopy))
        {
            DisposeOwnedInputImageValue();
            _inputImage.Value = null;
            return false;
        }

        if (imageCopy == null)
        {
            return false;
        }

        ReplaceOwnedInputImageValue(imageCopy);
        if (!IsFastModeEnabled)
        {
            UpdateMeasurementSliderRanges(imageCopy);
        }

        return true;
    }

    private bool ResolveAndApplyMaskImage()
    {
        object? imageValue = ResolveLinkedImageValue(_maskImage);
        if (TryCloneInitializedImage(imageValue, out HImage? imageCopy))
        {
            if (imageCopy == null)
            {
                return false;
            }

            ReplaceOwnedMaskImageValue(imageCopy);
            return true;
        }

        if (HasConfiguredMaskImage())
        {
            return false;
        }

        DisposeOwnedMaskImageValue();
        _maskImage.Value = null;
        return true;
    }

    private void ValidateRuntimeRemainImagesBeforeProcess(long runId, bool hasMask, HObject inputImage)
    {
        bool hasInputSize = TryGetImageSize(inputImage, out double inputWidth, out double inputHeight, out string inputSizeError);
        if (!hasInputSize)
        {
            LogWarning($"输入图像尺寸读取失败，跳过残留高度保护: RunId={runId}, Input={DescribeImage(inputImage)}, Error={inputSizeError}");
        }

        double remainWidth = 0.0;
        double remainHeight = 0.0;
        string remainError = string.Empty;
        bool hasRemainSize = _runtimeRemainImage != null
            && TryGetImageSize(_runtimeRemainImage, out remainWidth, out remainHeight, out remainError);
        if (_runtimeRemainImage != null && !hasRemainSize)
        {
            HImage? invalidRemainImage = _runtimeRemainImage;
            LogWarning(
                $"残留线扫图像已失效，执行前清空: RunId={runId}, Remain={DescribeImage(invalidRemainImage)}, Error={remainError}");
            _runtimeRemainImage = null;
            SafeDisposeRuntimeImage(invalidRemainImage, "_runtimeRemainImage-invalid-before-process");
        }
        else if (_runtimeRemainImage != null && hasInputSize && hasRemainSize)
        {
            double maxRemainHeight = Math.Max(1.0, inputHeight * MaxRuntimeRemainHeightFactor);
            if (Math.Abs(remainWidth - inputWidth) > 0.5 || remainHeight > maxRemainHeight)
            {
                HImage? oversizedRemainImage = _runtimeRemainImage;
                HImage? oversizedRemainMaskImage = _runtimeRemainMaskImage;
                LogWarning(
                    $"残留线扫图像尺寸超限，执行前清空: RunId={runId}, Remain={DescribeImage(oversizedRemainImage)}, " +
                    $"Input={inputWidth}x{inputHeight}, MaxRemainHeight={maxRemainHeight:0.###}");
                _runtimeRemainImage = null;
                _runtimeRemainMaskImage = null;
                SafeDisposeRuntimeImage(oversizedRemainImage, "_runtimeRemainImage-size-limit-before-process");
                SafeDisposeRuntimeImage(oversizedRemainMaskImage, "_runtimeRemainMaskImage-size-limit-before-process");
                return;
            }
        }

        double remainMaskWidth = 0.0;
        double remainMaskHeight = 0.0;
        string remainMaskError = string.Empty;
        bool hasRemainMaskSize = _runtimeRemainMaskImage != null
            && hasMask
            && _runtimeRemainImage != null
            && TryGetImageSize(_runtimeRemainMaskImage, out remainMaskWidth, out remainMaskHeight, out remainMaskError);
        if (_runtimeRemainMaskImage != null && !hasRemainMaskSize)
        {
            HImage? invalidRemainMaskImage = _runtimeRemainMaskImage;
            string error = !hasMask
                ? "mask input is not available"
                : _runtimeRemainImage == null
                    ? "source remain image is not available"
                    : remainMaskError;
            LogWarning(
                $"残留掩膜图像已失效，执行前清空: RunId={runId}, RemainMask={DescribeImage(invalidRemainMaskImage)}, Error={error}");
            _runtimeRemainMaskImage = null;
            SafeDisposeRuntimeImage(invalidRemainMaskImage, "_runtimeRemainMaskImage-invalid-before-process");
        }
        else if (_runtimeRemainImage != null && _runtimeRemainMaskImage != null)
        {
            if (Math.Abs(remainMaskWidth - remainWidth) > 0.5 || Math.Abs(remainMaskHeight - remainHeight) > 0.5)
            {
                HImage? unsyncedRemainImage = _runtimeRemainImage;
                HImage? unsyncedRemainMaskImage = _runtimeRemainMaskImage;
                LogWarning(
                    $"残留图像与残留掩膜尺寸不同步，执行前清空: RunId={runId}, " +
                    $"Remain={DescribeImage(unsyncedRemainImage)}, RemainMask={DescribeImage(unsyncedRemainMaskImage)}");
                _runtimeRemainImage = null;
                _runtimeRemainMaskImage = null;
                SafeDisposeRuntimeImage(unsyncedRemainImage, "_runtimeRemainImage-mask-unsynced-before-process");
                SafeDisposeRuntimeImage(unsyncedRemainMaskImage, "_runtimeRemainMaskImage-mask-unsynced-before-process");
            }
        }
    }

    private static bool TryCloneInitializedImage(object? imageValue, out HImage? imageCopy, out string error)
    {
        imageCopy = null;
        error = string.Empty;
        if (imageValue is not HObject image)
        {
            error = $"value is not HObject: {imageValue?.GetType().FullName ?? "null"}";
            return false;
        }

        if (!HalconImageOwnership.IsInitializedSafe(image))
        {
            error = "image is not initialized";
            return false;
        }

        try
        {
            HImage? copy = image is HImage hImage
                ? HalconImageOwnership.CopyBorrowedOrNull(hImage)
                : HalconImageOwnership.TryCopyBorrowed(image, 1, out HImage selectedImage)
                    ? selectedImage
                    : null;

            if (copy == null)
            {
                error = "copy image failed";
                return false;
            }

            try
            {
                if (!TryGetImageSize(copy, out _, out _, out string sizeError))
                {
                    error = $"get image size failed: {sizeError}";
                    HalconImageOwnership.DisposeOwned(copy);
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"get image size failed: {ex.Message}";
                HalconImageOwnership.DisposeOwned(copy);
                return false;
            }

            imageCopy = copy;
            return true;
        }
        catch (Exception ex)
        {
            error = $"copy image failed: {ex.Message}";
            HalconImageOwnership.DisposeOwned(imageCopy);
            imageCopy = null;
            return false;
        }
    }

    private static bool TryCloneInitializedImage(object? imageValue, out HImage? imageCopy)
    {
        return TryCloneInitializedImage(imageValue, out imageCopy, out _);
    }

    private static bool TryResolveProcessImage(HObject? imageObject, out HImage? image, out bool ownsImage, out string error)
    {
        image = null;
        ownsImage = false;
        error = string.Empty;

        if (imageObject == null)
        {
            error = "image is null";
            return false;
        }

        if (imageObject is HImage hImage)
        {
            image = hImage;
            return true;
        }

        if (!TryCloneInitializedImage(imageObject, out image, out error))
        {
            return false;
        }

        ownsImage = image != null;
        return image != null;
    }

    private static bool TryGetInitializedHObject(object? imageValue, out HObject? image)
    {
        image = null;
        if (imageValue is not HObject candidate)
        {
            return false;
        }

        try
        {
            if (!HalconImageOwnership.IsInitializedSafe(candidate))
            {
                return false;
            }

            if (!TryGetImageSize(candidate, out _, out _))
            {
                return false;
            }

            image = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool HasConfiguredMaskImage()
    {
        return _maskImage != null &&
            (!string.IsNullOrWhiteSpace(_maskImage.Name) ||
             !string.IsNullOrWhiteSpace(_maskImage.ParamName) ||
             _maskImage.Guid != Guid.Empty ||
             _maskImage.LinkGuid != Guid.Empty);
    }

    private void ReplaceOwnedInputImageValue(HObject image)
    {
        DisposeOwnedInputImageValue();
        _inputImage.Value = image;
        _ownsInputImageValue = true;
    }

    private void ReplaceOwnedMaskImageValue(HObject image)
    {
        DisposeOwnedMaskImageValue();
        _maskImage.Value = image;
        _ownsMaskImageValue = true;
    }

    private void DisposeOwnedInputImageValue()
    {
        if (_ownsInputImageValue && _inputImage?.Value is HObject image)
        {
            SafeDisposeOwnedImageValue(image, "InputImage.Value");
        }

        _ownsInputImageValue = false;
    }

    private void DisposeOwnedMaskImageValue()
    {
        if (_ownsMaskImageValue && _maskImage?.Value is HObject image)
        {
            SafeDisposeOwnedImageValue(image, "MaskImage.Value");
        }

        _ownsMaskImageValue = false;
    }

    private void SafeDisposeOwnedImageValue(HObject? image, string label)
    {
        if (image == null)
        {
            return;
        }

        try
        {
            image.Dispose();
        }
        catch (HOperatorException ex) when (IsDeletedObjectCleanup(ex))
        {
            LogWarning($"Ignore deleted owned input image cleanup: Label={label}, State={DescribeImage(image)}, Error={ex.Message}");
        }
        catch (Exception ex)
        {
            LogError($"Owned input image cleanup failed: Label={label}, State={DescribeImage(image)}, Error={ex}");
        }
    }

    private object? ResolveLinkedImageValue(TransmitParam param)
    {
        if (param == null)
        {
            return null;
        }

        object? value = GetTransmitParam(InputParams, param);
        if (value != null)
        {
            return value;
        }

        TransmitParam? cachedParam = FindCachedImageParam(param);
        if (TryGetImageValue(cachedParam?.Value, out object? cachedValue))
        {
            LogWarning(
                $"输入参数当前帧为空，回退上游输出缓存: Serial={param.Serial}, ParamName={param.ParamName}, " +
                $"Name={param.Name}, Cached={DescribeTransmitParam(cachedParam)}, CachedSize={DescribeImage(cachedValue as HObject)}");
            return cachedValue;
        }

        TransmitParam? globalParam = FindCurrentGlobalImageParam(param);
        if (TryGetImageValue(globalParam?.Value, out object? globalValue))
        {
            return globalValue;
        }

        if (TryGetImageValue(param.Value, out object? directValue))
        {
            return directValue;
        }

        TransmitParam? moduleInputParamValue = FindMatchingModuleTransmitParam(moduleInputParam, param);
        if (TryGetImageValue(moduleInputParamValue?.Value, out object? moduleInputValue))
        {
            return moduleInputValue;
        }

        if (TryResolveLinkedImageValueFromNodeCache(param, out object? nodeValue, out string source))
        {
            LogWarning(
                $"Input link value is empty, fallback node cache image: Serial={param.Serial}, ParamName={param.ParamName}, " +
                $"Name={param.Name}, Source={source}, Size={DescribeImage(nodeValue as HObject)}");
            return nodeValue;
        }

        return null;
    }

    private TransmitParam? FindCachedImageParam(TransmitParam? param)
    {
        if (param == null)
        {
            return null;
        }

        Dictionary<string, ObservableCollection<TransmitParam>>? outputCache =
            PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
        if (outputCache == null)
        {
            return null;
        }

        foreach ((_, ObservableCollection<TransmitParam>? outputParams) in EnumerateOutputCacheEntries(outputCache, param.Serial))
        {
            TransmitParam? matchedParam = FindMatchingTransmitParam(outputParams, param);
            if (matchedParam != null)
            {
                return matchedParam;
            }
        }

        return null;
    }

    private TransmitParam? FindCurrentGlobalImageParam(TransmitParam param)
    {
        ObservableCollection<TransmitParam>? globalParams = param.Resourece switch
        {
            ResoureceType.Global => PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams,
            ResoureceType.CustomGlobal => PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams,
            _ => null
        };

        return globalParams?.FirstOrDefault(item => IsSameTransmitParam(item, param));
    }

    private bool TryResolveLinkedImageValueFromNodeCache(TransmitParam param, out object? value, out string source)
    {
        value = null;
        source = string.Empty;
        Dictionary<string, object>? nodeParamCaches =
            PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
        if (nodeParamCaches == null)
        {
            return false;
        }

        foreach ((string cacheKey, ModelParamBase model) in EnumerateNodeParamCacheModels(nodeParamCaches, param.Serial))
        {
            TransmitParam? matchedParam = FindMatchingTransmitParam(model.OutputParams, param);
            if (TryGetImageValue(matchedParam?.Value, out value))
            {
                source = $"NodeParamCaches[{cacheKey}].OutputParams:{DescribeParamMatch(matchedParam)}";
                return true;
            }

            matchedParam = FindMatchingModuleTransmitParam(model.moduleOutputParam, param);
            if (TryGetImageValue(matchedParam?.Value, out value))
            {
                source = $"NodeParamCaches[{cacheKey}].moduleOutputParam:{DescribeParamMatch(matchedParam)}";
                return true;
            }

            matchedParam = FindMatchingTransmitParam(model.InputParams, param);
            if (TryGetImageValue(matchedParam?.Value, out value))
            {
                source = $"NodeParamCaches[{cacheKey}].InputParams:{DescribeParamMatch(matchedParam)}";
                return true;
            }

            matchedParam = FindMatchingModuleTransmitParam(model.moduleInputParam, param);
            if (TryGetImageValue(matchedParam?.Value, out value))
            {
                source = $"NodeParamCaches[{cacheKey}].moduleInputParam:{DescribeParamMatch(matchedParam)}";
                return true;
            }

            if (ShouldUseSourceInputImageFallback(param)
                && TryResolveSourceInputImageFromModel(model, out value, out string sourceMember))
            {
                source = $"NodeParamCaches[{cacheKey}].{sourceMember}";
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(string Key, ObservableCollection<TransmitParam>? Params)> EnumerateOutputCacheEntries(
        Dictionary<string, ObservableCollection<TransmitParam>>? outputCache,
        int serial)
    {
        if (outputCache == null)
        {
            yield break;
        }

        HashSet<string> visitedKeys = new(StringComparer.OrdinalIgnoreCase);
        foreach (string cacheKey in EnumerateSourceCacheKeys(serial))
        {
            if (outputCache.TryGetValue(cacheKey, out ObservableCollection<TransmitParam>? outputParams))
            {
                visitedKeys.Add(cacheKey);
                yield return (cacheKey, outputParams);
            }
        }

        foreach (KeyValuePair<string, ObservableCollection<TransmitParam>> item in outputCache)
        {
            if (visitedKeys.Contains(item.Key)
                || !TryParseCacheSerial(item.Key, out int cacheSerial)
                || cacheSerial != serial)
            {
                continue;
            }

            visitedKeys.Add(item.Key);
            yield return (item.Key, item.Value);
        }
    }

    private static IEnumerable<(string Key, ModelParamBase Model)> EnumerateNodeParamCacheModels(
        Dictionary<string, object>? nodeParamCaches,
        int serial)
    {
        if (nodeParamCaches == null)
        {
            yield break;
        }

        HashSet<string> visitedKeys = new(StringComparer.OrdinalIgnoreCase);
        foreach (string cacheKey in EnumerateSourceCacheKeys(serial))
        {
            if (nodeParamCaches.TryGetValue(cacheKey, out object? cacheValue)
                && cacheValue is ModelParamBase model)
            {
                visitedKeys.Add(cacheKey);
                yield return (cacheKey, model);
            }
        }

        foreach (KeyValuePair<string, object> item in nodeParamCaches)
        {
            if (visitedKeys.Contains(item.Key)
                || item.Value is not ModelParamBase model
                || model.Serial != serial)
            {
                continue;
            }

            visitedKeys.Add(item.Key);
            yield return (item.Key, model);
        }
    }

    private static IEnumerable<string> EnumerateSourceCacheKeys(int serial)
    {
        if (serial < 0)
        {
            yield break;
        }

        string rawKey = serial.ToString();
        yield return rawKey;

        string displayKey = serial.ToString("D3");
        if (!string.Equals(displayKey, rawKey, StringComparison.Ordinal))
        {
            yield return displayKey;
        }
    }

    private static bool TryParseCacheSerial(string? cacheKey, out int serial)
    {
        serial = -1;
        return !string.IsNullOrWhiteSpace(cacheKey)
            && int.TryParse(cacheKey, out serial);
    }

    private static TransmitParam? FindMatchingModuleTransmitParam(ModuleParam? moduleParam, TransmitParam target)
    {
        return FindMatchingTransmitParam(EnumerateModuleTransmitParams(moduleParam), target);
    }

    private static IEnumerable<TransmitParam> EnumerateModuleTransmitParams(ModuleParam? moduleParam)
    {
        if (moduleParam?.TransmitParams == null)
        {
            yield break;
        }

        foreach (object item in moduleParam.TransmitParams.Values)
        {
            if (item is TransmitParam transmitParam)
            {
                yield return transmitParam;
            }
        }
    }

    private static TransmitParam? FindMatchingTransmitParam(IEnumerable<TransmitParam>? candidates, TransmitParam target)
    {
        if (candidates == null || target == null)
        {
            return null;
        }

        return candidates.FirstOrDefault(item => IsSameTransmitParam(item, target));
    }

    private static bool IsSameTransmitParam(TransmitParam item, TransmitParam target)
    {
        return item.Guid == target.Guid
            || (item.Serial == target.Serial && !string.IsNullOrWhiteSpace(item.Name) && item.Name == target.Name)
            || (!string.IsNullOrWhiteSpace(item.ResourcePath) && item.ResourcePath == target.ResourcePath)
            || (!string.IsNullOrWhiteSpace(item.ParamName) && item.ParamName == target.ParamName);
    }

    private static bool ShouldUseSourceInputImageFallback(TransmitParam param)
    {
        if (!IsSourceInputImageRequest(param))
        {
            return false;
        }

        return param.IsLink
            || param.LinkGuid != Guid.Empty
            || !string.IsNullOrWhiteSpace(param.ResourcePath)
            || !string.IsNullOrWhiteSpace(param.ParentNode);
    }

    private static bool IsSourceInputImageRequest(TransmitParam param)
    {
        return string.Equals(param.ParamName, "SourceImage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param.ParamName, "InputImage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param.Name, "SourceImage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param.Name, "InputImage", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveSourceInputImageFromModel(ModelParamBase model, out object? value, out string sourceMember)
    {
        if (TryGetModelImageMemberValue(model, "SourceImage", out value))
        {
            sourceMember = "SourceImage";
            return true;
        }

        if (TryGetModelImageMemberValue(model, "_inputImage", out value))
        {
            sourceMember = "_inputImage.Value";
            return true;
        }

        if (TryFindFirstImageParam(model.InputParams, out TransmitParam? inputParam, out value))
        {
            sourceMember = $"InputParams.FirstImage:{DescribeParamMatch(inputParam)}";
            return true;
        }

        if (TryFindFirstImageParam(EnumerateModuleTransmitParams(model.moduleInputParam), out inputParam, out value))
        {
            sourceMember = $"moduleInputParam.FirstImage:{DescribeParamMatch(inputParam)}";
            return true;
        }

        if (TryGetModelImageMemberValue(model, "InputImage", out value))
        {
            sourceMember = "InputImage.Value";
            return true;
        }

        sourceMember = string.Empty;
        value = null;
        return false;
    }

    private static bool TryGetModelImageMemberValue(ModelParamBase model, string memberName, out object? value)
    {
        value = null;
        if (model == null || string.IsNullOrWhiteSpace(memberName))
        {
            return false;
        }

        try
        {
            Type? currentType = model.GetType();
            while (currentType != null)
            {
                FieldInfo? field = currentType.GetField(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return TryGetImageValue(field.GetValue(model), out value);
                }

                PropertyInfo? property = currentType.GetProperty(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return TryGetImageValue(property.GetValue(model), out value);
                }

                currentType = currentType.BaseType;
            }
        }
        catch
        {
            value = null;
        }

        return false;
    }

    private static bool TryFindFirstImageParam(
        IEnumerable<TransmitParam>? candidates,
        out TransmitParam? matchedParam,
        out object? value)
    {
        matchedParam = null;
        value = null;
        if (candidates == null)
        {
            return false;
        }

        foreach (TransmitParam candidate in candidates)
        {
            if (TryGetImageValue(candidate?.Value, out value))
            {
                matchedParam = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetImageValue(object? candidate, out object? value)
    {
        value = null;
        if (candidate is TransmitParam transmitParam)
        {
            candidate = transmitParam.Value;
        }

        if (!TryGetInitializedHObject(candidate, out HObject? image))
        {
            return false;
        }

        value = image;
        return true;
    }

    private static string DescribeParamMatch(TransmitParam? param)
    {
        return param == null
            ? "-"
            : $"{param.ParamName ?? "-"}|{param.Name ?? "-"}";
    }

    internal LineScanSheetCounterDialogState CaptureDialogState()
    {
        return new LineScanSheetCounterDialogState(
            CloneTransmitParamMetadata(_inputImage, includeValue: false),
            CloneTransmitParamMetadata(_maskImage, includeValue: false),
            OutputParams
                .Select(item => CloneTransmitParamMetadata(item, includeValue: true))
                .OfType<TransmitParam>()
                .ToList(),
            ScaleFactor,
            SmoothSigma,
            EdgeThreshold,
            MeasureCenterColumn,
            MeasureRoiWidth,
            CropRatio,
            ResetBeforeNextRun,
            IsFastModeEnabled,
            SaveSheetImages,
            SaveConcatImage,
            SaveRemainImage,
            SaveDirectory);
    }

    internal void RestoreDialogState(LineScanSheetCounterDialogState? state)
    {
        if (state == null)
        {
            return;
        }

        lock (_executionSyncRoot)
        {
            DisposeOwnedInputImageValue();
            _inputImage = CloneTransmitParamMetadata(state.InputImage, includeValue: false) ?? CreateInputImageParam();
            EnsureInputImageParamMetadata(_inputImage);
            _ownsInputImageValue = false;

            DisposeOwnedMaskImageValue();
            _maskImage = CloneTransmitParamMetadata(state.MaskImage, includeValue: false) ?? CreateMaskImageParam();
            EnsureMaskImageParamMetadata(_maskImage);
            _ownsMaskImageValue = false;

            OutputParams = new ObservableCollection<TransmitParam>(
                state.OutputParams
                    .Select(item => CloneTransmitParamMetadata(item, includeValue: true))
                    .OfType<TransmitParam>());

            _scaleFactor = state.ScaleFactor;
            _smoothSigma = state.SmoothSigma;
            _edgeThreshold = state.EdgeThreshold;
            _measureCenterColumn = state.MeasureCenterColumn;
            _measureRoiWidth = state.MeasureRoiWidth;
            _cropRatio = state.CropRatio;
            ResetBeforeNextRun = state.ResetBeforeNextRun;
            IsFastModeEnabled = state.IsFastModeEnabled;
            SaveSheetImages = state.SaveSheetImages;
            SaveConcatImage = state.SaveConcatImage;
            SaveRemainImage = state.SaveRemainImage;
            SaveDirectory = state.SaveDirectory;
            SyncModuleOutputParams();
        }

        RaisePropertyChanged(nameof(InputImage));
        RaisePropertyChanged(nameof(MaskImage));
        RaisePropertyChanged(nameof(OutputParams));
        RefreshOutputProperties();
    }

    internal void ApplyDialogInputLinks(TransmitParam? inputImage, TransmitParam? maskImage)
    {
        lock (_executionSyncRoot)
        {
            InputImage = CloneTransmitParamMetadata(inputImage, includeValue: false) ?? CreateInputImageParam();
            MaskImage = CloneTransmitParamMetadata(maskImage, includeValue: false) ?? CreateMaskImageParam();
        }
    }

    internal void SetRuntimePreviewEnabled(bool enabled)
    {
        _runtimePreviewEnabled = enabled;
        if (!enabled)
        {
            ClearPreviewDisplay();
        }
        else if (!IsFastModeEnabled)
        {
            RefreshRuntimePreviewFromLinkedInputs();
        }
    }

    internal void RefreshRuntimePreviewForFastModeChange()
    {
        if (!_runtimePreviewEnabled)
        {
            return;
        }

        if (IsFastModeEnabled)
        {
            ClearPreviewDisplay();
            return;
        }

        RefreshMeasurementPreview();
    }

    internal void RefreshRuntimePreviewFromLinkedInputs()
    {
        lock (_executionSyncRoot)
        {
            if (!ShouldDrawRuntimePreview())
            {
                return;
            }

            try
            {
                if (!ResolveAndApplyInputImage())
                {
                    return;
                }

                if (!ResolveAndApplyMaskImage())
                {
                    return;
                }

                RefreshMeasurementPreview();
            }
            catch (Exception ex)
            {
                LogPreviewSkip(ex);
            }
        }
    }

    private static TransmitParam? CloneTransmitParamMetadata(TransmitParam? source, bool includeValue)
    {
        if (source == null)
        {
            return null;
        }

        return new TransmitParam
        {
            IsLink = source.IsLink,
            LinkGuid = source.LinkGuid,
            Serial = source.Serial,
            ParentNode = source.ParentNode,
            Guid = source.Guid,
            Resourece = source.Resourece,
            Name = source.Name,
            ParamName = source.ParamName,
            Type = source.Type,
            Value = includeValue ? source.Value : null,
            Describe = source.Describe,
            IsGlobal = source.IsGlobal,
            ResourcePath = source.ResourcePath
        };
    }

    #endregion

    #region 结果处理

    /// <summary>
    /// 拷贝算法结果到模块状态，并刷新预览、保存和输出参数。
    /// </summary>
    private void ApplyResult(LineScanSheetCounterResult result)
    {
        ClearRuntimeImages();

        int previousTotalCount = TotalCount;
        IncrementCount = result.IncrementCount;
        TotalCount += IncrementCount;
        EdgeRows = result.EdgeRows.ToArray();

        _runtimeRemainImage = TakeRuntimeRemainImage(result);
        _runtimeRemainMaskImage = result.DetachRemainMaskImage();
        RemainImage = _runtimeRemainImage;
        LastConcatImage = result.DetachLastConcatImage();
        AdoptTargetImages(result.DetachTargetImages());

        CroppedImages = BuildCroppedImagesObject(_runtimeTargetImages);
        _runtimeCroppedImages = CroppedImages;
        SaveRuntimeImagesIfEnabled(previousTotalCount);

        UpdatePreviewDisplay(result);

        RefreshOutputProperties();
    }

    private void ApplyFastResult(LineScanSheetCounterResult result, bool shouldSaveRuntimeImages)
    {
        ClearRuntimeImages();
        ClearPreviewDisplay();

        int previousTotalCount = TotalCount;
        IncrementCount = result.IncrementCount;
        TotalCount += IncrementCount;
        EdgeRows = [];

        _runtimeRemainImage = TakeRuntimeRemainImage(result);
        _runtimeRemainMaskImage = result.DetachRemainMaskImage();
        if (shouldSaveRuntimeImages && SaveRemainImage)
        {
            RemainImage = _runtimeRemainImage;
        }

        if (shouldSaveRuntimeImages && SaveConcatImage)
        {
            LastConcatImage = result.DetachLastConcatImage();
        }
        else
        {
            SafeDisposeRuntimeImage(result.DetachLastConcatImage(), "LastConcatImage-unused");
        }

        AdoptTargetImages(result.DetachTargetImages());

        CroppedImages = BuildCroppedImagesObject(_runtimeTargetImages);
        _runtimeCroppedImages = CroppedImages;
        if (shouldSaveRuntimeImages)
        {
            SaveRuntimeImagesIfEnabled(previousTotalCount);
        }

        RefreshFastOutputProperties();
    }

    private static HImage? TakeRuntimeRemainImage(LineScanSheetCounterResult result)
    {
        return result.DetachSourceRemainImage() ?? result.DetachRemainImage();
    }

    private void AdoptTargetImages(IEnumerable<HImage> targetImages)
    {
        foreach (HImage image in targetImages)
        {
            _runtimeTargetImages.Add(image);
            TargetImages.Add(image);
        }
    }

    /// <summary>
    /// 将本次裁剪出的多张片材图像拼接为一个 HObject 输出。
    /// </summary>
    private static HObject BuildCroppedImagesObject(IEnumerable<HImage> targetImages)
    {
        HOperatorSet.GenEmptyObj(out HObject croppedImages);
        foreach (HImage image in targetImages)
        {
            HOperatorSet.ConcatObj(croppedImages, image, out HObject nextCroppedImages);
            HalconImageOwnership.DisposeOwned(croppedImages);
            croppedImages = nextCroppedImages;
        }

        return croppedImages;
    }

    #endregion

    #region 图像保存

    /// <summary>
    /// 按独立开关保存本次片材图、拼接图和残留图，便于现场追溯。
    /// </summary>
    private void SaveRuntimeImagesIfEnabled(int previousTotalCount)
    {
        if ((!SaveSheetImages && !SaveConcatImage && !SaveRemainImage) ||
            string.IsNullOrWhiteSpace(SaveDirectory))
        {
            return;
        }

        Directory.CreateDirectory(SaveDirectory);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        if (SaveSheetImages)
        {
            for (int index = 0; index < _runtimeTargetImages.Count; index++)
            {
                string filePath = Path.Combine(SaveDirectory, $"{ModuleName}_{timestamp}_target_{previousTotalCount + index + 1:D6}.png");
                HOperatorSet.WriteImage(_runtimeTargetImages[index], "png", 0, filePath);
            }
        }

        int currentCount = previousTotalCount + IncrementCount;
        if (SaveConcatImage)
        {
            WriteImageIfInitialized(LastConcatImage, Path.Combine(SaveDirectory, $"{ModuleName}_{timestamp}_concat_{currentCount:D6}.png"));
        }

        if (SaveRemainImage)
        {
            WriteImageIfInitialized(RemainImage, Path.Combine(SaveDirectory, $"{ModuleName}_{timestamp}_remain_{currentCount:D6}.png"));
        }
    }

    /// <summary>
    /// 仅在图像有效时写入磁盘，避免空对象导致 HALCON 报错。
    /// </summary>
    private static void WriteImageIfInitialized(HImage? image, string filePath)
    {
        if (image == null || !image.IsInitialized())
        {
            return;
        }

        HOperatorSet.WriteImage(image, "png", 0, filePath);
    }

    #endregion

    #region 实时预览

    /// <summary>
    /// 设置会影响 MeasurePos 的参数，并在滑动调整时刷新预览边缘检测结果。
    /// </summary>
    private void SetMeasurementParameter(ref double field, double value, string propertyName)
    {
        if (Math.Abs(field - value) < 0.001)
        {
            return;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        if (!IsFastModeEnabled && !IsSchemeFlowExecution())
        {
            RefreshMeasurementPreview();
        }
    }

    /// <summary>
    /// 根据当前输入图像尺寸更新卡尺滑动条范围和默认值。
    /// </summary>
    private void UpdateMeasurementSliderRanges(HObject image)
    {
        if (!TryGetImageSize(image, out double imageWidth, out _))
        {
            return;
        }

        double width = Math.Max(1.0, imageWidth);
        PreviewImageWidth = Math.Max(1.0, width - 1.0);
        PreviewRoiWidthMaximum = Math.Max(1.0, width * 0.5);

        bool changed = false;
        if (_measureCenterColumn <= 0 || _measureCenterColumn > PreviewImageWidth)
        {
            _measureCenterColumn = PreviewImageWidth * 0.5;
            changed = true;
            RaisePropertyChanged(nameof(MeasureCenterColumn));
        }

        if (_measureRoiWidth <= 0 || _measureRoiWidth > PreviewRoiWidthMaximum)
        {
            _measureRoiWidth = Math.Max(1.0, width / 20.0);
            changed = true;
            RaisePropertyChanged(nameof(MeasureRoiWidth));
        }

        if (changed)
        {
            RaisePropertyChanged(nameof(PreviewImageWidth));
            RaisePropertyChanged(nameof(PreviewRoiWidthMaximum));
        }
    }

    private static bool TryGetImageSize(HObject image, out double width, out double height)
    {
        return TryGetImageSize(image, out width, out height, out _);
    }

    private static bool TryGetImageSize(HObject image, out double width, out double height, out string error)
    {
        return HalconImageOwnership.TryGetImageSize(image, out width, out height, out error);
    }

    /// <summary>
    /// 使用当前滑动参数重新检测输入图像，并只刷新预览窗口，不更新计数和输出。
    /// </summary>
    private void RefreshMeasurementPreview()
    {
        if (_isRefreshingPreview)
        {
            return;
        }

        try
        {
            _isRefreshingPreview = true;
            ResolveAndApplyInputImage();
            ResolveAndApplyMaskImage();
            if (_inputImage?.Value is not HObject inputObject ||
                !HalconImageOwnership.IsInitializedSafe(inputObject))
            {
                return;
            }

            using HImage input = TryCloneInitializedImage(inputObject, out HImage? inputCopy)
                ? inputCopy!
                : throw new InvalidOperationException("线扫图像复制失败。");
            using HImage? mask = CreateCurrentMaskImage();
            using LineScanSheetCounterResult result = new LineScanSheetCounterAlgorithm()
                .Process(_runtimeRemainImage, input, _runtimeRemainMaskImage, mask, BuildParameters());
            EdgeRows = result.EdgeRows.ToArray();

            UpdatePreviewDisplay(result);

            StatusText = $"预览边缘数量 {EdgeRows.Length}，卡尺列 {MeasureCenterColumn:F1}，ROI宽 {MeasureRoiWidth:F1}。";
            RaisePropertyChanged(nameof(EdgeRows));
            RaisePropertyChanged(nameof(StatusText));
        }
        catch (Exception ex)
        {
            StatusText = $"实时预览失败：{ex.Message}";
            RaisePropertyChanged(nameof(StatusText));
        }
        finally
        {
            _isRefreshingPreview = false;
        }
    }

    #endregion

    #region 预览绘制

    /// <summary>
    /// 从算法结果中取出当前帧预览图，缺图时清空预览窗口。
    /// </summary>
    private void UpdatePreviewDisplay(LineScanSheetCounterResult result)
    {
        HImage? preview = result.PreviewImage;
        if (preview != null && preview.IsInitialized())
        {
            UpdatePreviewDisplay(preview, result, previewUsesMeasureCoordinates: true);
        }
        else
        {
            ClearPreviewDisplay();
        }
    }

    /// <summary>
    /// 同一批次刷新预览底图和叠加线，避免卡尺线停留在上一帧。
    /// </summary>
    private void UpdatePreviewDisplay(HImage previewImage, LineScanSheetCounterResult result, bool previewUsesMeasureCoordinates)
    {
        if (!ShouldDrawRuntimePreview() || previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return;
        }

        try
        {
            long updateVersion = NextPreviewUpdateVersion();
            double[] edgeRows = result.EdgeRows.ToArray();
            double[] edgeAmplitudes = result.EdgeAmplitudes.ToArray();
            List<CropRange> cropRanges = result.CropRanges
                .Select(range => new CropRange(range.StartRow, range.EndRow))
                .ToList();
            double cropRow = result.CropRow;
            double previewMeasureCenterColumn = result.PreviewMeasureCenterColumn;
            double previewMeasureRoiWidth = result.PreviewMeasureRoiWidth;

            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                UpdatePreviewDisplayCore(
                    previewImage,
                    edgeRows,
                    edgeAmplitudes,
                    cropRanges,
                    cropRow,
                    previewMeasureCenterColumn,
                    previewMeasureRoiWidth,
                    previewUsesMeasureCoordinates);
                return;
            }

            HImage? previewCopy = CopyPreviewImageOrNull(previewImage);
            if (previewCopy == null)
            {
                return;
            }

            dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (IsPreviewUpdateCurrent(updateVersion))
                    {
                        UpdatePreviewDisplayCore(
                            previewCopy,
                            edgeRows,
                            edgeAmplitudes,
                            cropRanges,
                            cropRow,
                            previewMeasureCenterColumn,
                            previewMeasureRoiWidth,
                            previewUsesMeasureCoordinates);
                    }
                }
                finally
                {
                    HalconImageOwnership.DisposeOwned(previewCopy);
                }
            });
        }
        catch (Exception ex)
        {
            LogPreviewSkip(ex);
        }
    }

    private void UpdatePreviewDisplayCore(
        HImage previewImage,
        double[] edgeRows,
        double[] edgeAmplitudes,
        IReadOnlyList<CropRange> cropRanges,
        double cropRow,
        double previewMeasureCenterColumn,
        double previewMeasureRoiWidth,
        bool previewUsesMeasureCoordinates)
    {
        UpdatePreviewImageObject(previewImage);
        DrawResultOverlays(
            previewImage,
            edgeRows,
            edgeAmplitudes,
            cropRanges,
            cropRow,
            previewMeasureCenterColumn,
            previewMeasureRoiWidth,
            previewUsesMeasureCoordinates);
    }
    private void DrawResultOverlays(
        HImage previewImage,
        double[] edgeRows,
        double[] edgeAmplitudes,
        IReadOnlyList<CropRange> cropRanges,
        double cropRow,
        double previewMeasureCenterColumn,
        double previewMeasureRoiWidth,
        bool previewUsesMeasureCoordinates)
    {
        ClearPreviewDrawObjects();
        if (previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return;
        }

        DrawMeasureRegion(previewImage, previewMeasureCenterColumn, previewMeasureRoiWidth, previewUsesMeasureCoordinates);
        DrawEdgePositions(previewImage, edgeRows, edgeAmplitudes, previewUsesMeasureCoordinates);
        DrawCropPositions(previewImage, cropRanges, cropRow, previewUsesMeasureCoordinates);
    }

    /// <summary>
    /// 在预览图上绘制卡尺中心列和 ROI 宽度范围。
    /// </summary>
    private void DrawMeasureRegion(
        HImage previewImage,
        double previewMeasureCenterColumn,
        double previewMeasureRoiWidth,
        bool previewUsesMeasureCoordinates)
    {
        if (previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return;
        }

        if (!TryGetImageSize(previewImage, out double width, out double height))
        {
            return;
        }

        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);
        double centerColumn = previewUsesMeasureCoordinates && previewMeasureCenterColumn > 0
            ? previewMeasureCenterColumn
            : MeasureCenterColumn;
        double roiWidth = previewUsesMeasureCoordinates && previewMeasureRoiWidth > 0
            ? previewMeasureRoiWidth
            : MeasureRoiWidth;
        centerColumn = Math.Clamp(centerColumn > 0 ? centerColumn : width * 0.5, 0.0, width - 1.0);
        roiWidth = Math.Clamp(roiWidth > 0 ? roiWidth : width / 20.0, 1.0, width * 0.5);
        double leftColumn = Math.Clamp(centerColumn - roiWidth, 0.0, width - 1.0);
        double rightColumn = Math.Clamp(centerColumn + roiWidth, 0.0, width - 1.0);

        AddVerticalOverlay(leftColumn, height, "yellow");
        AddVerticalOverlay(centerColumn, height, "red");
        AddVerticalOverlay(rightColumn, height, "yellow");
    }

    /// <summary>
    /// 在预览图上绘制 MeasurePos 获取到的 Y 方向边缘位置。
    /// </summary>
    private void DrawEdgePositions(
        HImage previewImage,
        double[] edgeRows,
        double[] edgeAmplitudes,
        bool previewUsesMeasureCoordinates)
    {
        if (edgeRows.Length == 0 || previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return;
        }

        if (!TryGetImageSize(previewImage, out double width, out double height))
        {
            return;
        }

        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);
        double rowScale = previewUsesMeasureCoordinates
            ? 1.0
            : 1.0 / Math.Clamp(ScaleFactor, 0.05, 1.0);

        for (int index = 0; index < edgeRows.Length; index++)
        {
            double displayRow = MapPreviewRow(Math.Clamp(edgeRows[index] * rowScale, 0.0, height - 1.0), height);
            AddHorizontalOverlay(displayRow, width, GetEdgeColor(edgeAmplitudes, index));
        }
    }

    /// <summary>
    /// 在预览图上绘制片材裁剪行和残留起始行。
    /// </summary>
    private void DrawCropPositions(
        HImage previewImage,
        IReadOnlyList<CropRange> cropRanges,
        double remainCropRow,
        bool previewUsesMeasureCoordinates)
    {
        if (previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return;
        }

        if (!TryGetImageSize(previewImage, out double width, out double height))
        {
            return;
        }

        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);

        double scale = Math.Clamp(ScaleFactor, 0.05, 1.0);
        double cropRangeScale = previewUsesMeasureCoordinates ? scale : 1.0;
        double remainCropRowScale = previewUsesMeasureCoordinates ? 1.0 : 1.0 / scale;

        for (int index = 0; index < cropRanges.Count; index++)
        {
            double startRow = MapPreviewRow(Math.Clamp(cropRanges[index].StartRow * cropRangeScale, 0.0, height - 1.0), height);
            double endRow = MapPreviewRow(Math.Clamp(cropRanges[index].EndRow * cropRangeScale, 0.0, height - 1.0), height);
            DrawCropLine(startRow, width, "blue");
            DrawCropLine(endRow, width, "magenta");
        }

        if (remainCropRow > 0)
        {
            double displayCropRow = MapPreviewRow(Math.Clamp(remainCropRow * remainCropRowScale, 0.0, height - 1.0), height);
            DrawCropLine(displayCropRow, width, "yellow");
        }
    }

    /// <summary>
    /// 绘制一条水平辅助线。
    /// </summary>
    private void DrawCropLine(double row, double width, string color)
    {
        AddHorizontalOverlay(row, width, color);
    }

    /// <summary>
    /// 添加预览坐标系中的水平结果线。
    /// </summary>
    private void AddHorizontalOverlay(double row, double width, string color)
    {
        AddLineOverlay(row, 0.0, row, Math.Max(0.0, width - 1.0), color);
    }

    /// <summary>
    /// 添加预览坐标系中的垂直卡尺线。
    /// </summary>
    private void AddVerticalOverlay(double column, double height, string color)
    {
        AddLineOverlay(0.0, column, Math.Max(0.0, height - 1.0), column, color);
    }

    /// <summary>
    /// 将线段转成 XLD 对象后加入预览叠加层。
    /// </summary>
    private void AddLineOverlay(double row1, double column1, double row2, double column2, string color)
    {
        HObject? contour = null;
        try
        {
            HOperatorSet.GenContourPolygonXld(out contour, new HTuple(row1, row2), new HTuple(column1, column2));
            AddXldPreviewOverlay(contour, color);
        }
        catch
        {
        }
        finally
        {
            SafeDisposeHObject(contour);
        }
    }

    private void AddXldPreviewOverlay(HObject? contourObject, string color)
    {
        if (contourObject == null || !HalconImageOwnership.IsInitializedSafe(contourObject))
        {
            return;
        }

        try
        {
            PreviewDrawObjects.Add(new HalconDrawingObject
            {
                ShapeType = HalconShapeType.Region,
                Hobject = contourObject.Clone(),
                Color = color,
                IsFillDisplay = false
            });
        }
        catch
        {
        }
    }

    private void UpdatePreviewImageObject(HObject previewImage)
    {
        HImage? image = CopyPreviewImageOrNull(previewImage);
        if (image == null)
        {
            return;
        }

        try
        {
            SetPreviewImageObject(image);
            image = null;
        }
        finally
        {
            HalconImageOwnership.DisposeOwned(image);
        }
    }

    private static HImage? CopyPreviewImageOrNull(HObject? previewImage)
    {
        if (previewImage == null || !HalconImageOwnership.IsInitializedSafe(previewImage))
        {
            return null;
        }

        if (previewImage is HImage hImage)
        {
            return HalconImageOwnership.CopyOwnedOrNull(hImage);
        }

        return HalconImageOwnership.TryCopyBorrowed(previewImage, 1, out HImage imageCopy)
            ? imageCopy
            : null;
    }

    private void SetPreviewImageObject(HObject? image)
    {
        HObject? oldImage = _previewImageObject;
        PreviewImageObject = image;
        SafeDisposeHObject(oldImage);
    }

    public void ClearPreviewDrawObjects()
    {
        foreach (HalconDrawingObject drawObject in PreviewDrawObjects.ToList())
        {
            SafeDisposeHObject(drawObject?.Hobject);
        }

        PreviewDrawObjects.Clear();
    }

    private void ClearPreviewDisplay()
    {
        NextPreviewUpdateVersion();
        var dispatcher = PrismProvider.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            ClearPreviewDisplayCore();
            return;
        }

        dispatcher.BeginInvoke(ClearPreviewDisplayCore);
    }

    private void ClearPreviewDisplayCore()
    {
        ClearPreviewDrawObjects();
        SetPreviewImageObject(null);
    }

    private long NextPreviewUpdateVersion()
    {
        return System.Threading.Interlocked.Increment(ref _previewUpdateVersion);
    }

    private long CurrentPreviewUpdateVersion()
    {
        return System.Threading.Volatile.Read(ref _previewUpdateVersion);
    }

    private bool IsPreviewUpdateCurrent(long updateVersion)
    {
        return updateVersion == CurrentPreviewUpdateVersion();
    }

    private static double MapPreviewRow(double sourceRow, double imageHeight)
    {
        double maxRow = Math.Max(0.0, imageHeight - 1.0);
        double clampedRow = Math.Clamp(sourceRow, 0.0, maxRow);
        return clampedRow;
    }

    private bool ShouldDrawRuntimePreview()
    {
        if (IsFastModeEnabled || !_runtimePreviewEnabled)
        {
            return false;
        }

        return true;
    }

    private static bool IsSchemeFlowExecution()
    {
        return PrismProvider.ProjectManager?.SltCurSolutionItem?.IsManual == false;
    }

    /// <summary>
    /// 根据边缘幅值区分上边缘和下边缘颜色。
    /// </summary>
    private static string GetEdgeColor(double[] edgeAmplitudes, int index)
    {
        if (index >= edgeAmplitudes.Length)
        {
            return "cyan";
        }

        return edgeAmplitudes[index] >= 0 ? "green" : "orange";
    }

    private void LogPreviewSkip(Exception _)
    {
        LogTrace($"预览刷新跳过: {_}");
    }

    #endregion

    #region 日志

    private void LogTrace(string message)
    {
        WriteLog("TRACE", message);
    }

    private void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    private void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    private void LogError(string message)
    {
        WriteLog("ERROR", message);
    }

    private void LogMemoryCheckpoint(long runId, string stage)
    {
        try
        {
            using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            long managedBytes = GC.GetTotalMemory(false);
            LogTrace(
                $"内存状态: RunId={runId}, Stage={stage}, " +
                $"ManagedMB={managedBytes / 1024.0 / 1024.0:0.00}, " +
                $"WorkingSetMB={process.WorkingSet64 / 1024.0 / 1024.0:0.00}, " +
                $"PrivateMB={process.PrivateMemorySize64 / 1024.0 / 1024.0:0.00}, " +
                $"Remain={DescribeImage(_runtimeRemainImage)}, RemainMask={DescribeImage(_runtimeRemainMaskImage)}, " +
                $"Targets={_runtimeTargetImages.Count}, Cropped={DescribeImage(_runtimeCroppedImages)}");
        }
        catch
        {
            // Diagnostics must never affect module execution.
        }
    }

    private void WriteLog(string level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string formatted = $"{LogPrefix} 节点{Serial:D3} {message}";
        try
        {
            switch ((level ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "WARN":
                case "WARNING":
                    Logs.LogWarning(formatted);
                    break;
                case "ERROR":
                    Logs.LogError(formatted);
                    break;
                case "FATAL":
                    Logs.LogFatal(formatted);
                    break;
                case "TRACE":
                case "DEBUG":
                    Logs.LogTrace(formatted);
                    break;
                default:
                    Logs.LogInfo(formatted);
                    break;
            }
        }
        catch
        {
            // 日志失败不能影响线扫计数主流程。
        }
    }

    private static string DescribeImage(HObject? image)
    {
        if (image == null)
        {
            return "null";
        }

        try
        {
            if (!image.IsInitialized())
            {
                return "未初始化";
            }
        }
        catch (Exception ex)
        {
            return $"读取失败:{ex.Message}";
        }

        if (HalconImageOwnership.TryGetImageSize(image, out int width, out int height, out string sizeError))
        {
            return $"{width}x{height}";
        }

        if (sizeError.Contains("no size", StringComparison.OrdinalIgnoreCase))
        {
            return "空对象";
        }

        return $"读取失败:{sizeError}";
    }

    private static string DescribeTransmitParam(TransmitParam? param)
    {
        if (param == null)
        {
            return "null";
        }

        string valueType = param.Value?.GetType().Name ?? "null";
        return $"Guid={param.Guid}, LinkGuid={param.LinkGuid}, Serial={param.Serial}, Name={param.Name ?? "-"}, " +
            $"ParamName={param.ParamName ?? "-"}, ResourcePath={param.ResourcePath ?? "-"}, Resource={param.Resourece}, ValueType={valueType}";
    }

    private void LogInputSourceDiagnostics(long runId)
    {
        TransmitParam? inputHit = FindInputParamHit(_inputImage);
        TransmitParam? maskHit = FindInputParamHit(_maskImage);
        LogTrace(
            $"输入来源: RunId={runId}, " +
            $"InputImage={DescribeTransmitParam(_inputImage)}, " +
            $"InputHit={DescribeTransmitParam(inputHit)}, " +
            $"InputHitSize={DescribeImage(inputHit?.Value as HObject)}, " +
            $"InputValueSize={DescribeImage(_inputImage?.Value as HObject)}, " +
            $"MaskImage={DescribeTransmitParam(_maskImage)}, " +
            $"MaskHit={DescribeTransmitParam(maskHit)}, " +
            $"MaskHitSize={DescribeImage(maskHit?.Value as HObject)}, " +
            $"MaskValueSize={DescribeImage(_maskImage?.Value as HObject)}");
    }

    private TransmitParam? FindInputParamHit(TransmitParam? param)
    {
        if (param == null || InputParams == null)
        {
            return null;
        }

        return InputParams.FirstOrDefault(item => item.Guid == param.Guid);
    }

    #endregion

    #region 资源清理

    /// <summary>
    /// 释放运行时图像对象，避免 HALCON 句柄泄漏。
    /// </summary>
    private void ClearRuntimeRemainImages()
    {
        HImage? runtimeRemainImage = _runtimeRemainImage;
        HImage? runtimeRemainMaskImage = _runtimeRemainMaskImage;
        HImage? displayRemainImage = RemainImage;

        _runtimeRemainImage = null;
        _runtimeRemainMaskImage = null;
        RemainImage = null;

        SafeDisposeRuntimeImage(runtimeRemainImage, "_runtimeRemainImage-manual-clear");
        SafeDisposeRuntimeImage(runtimeRemainMaskImage, "_runtimeRemainMaskImage-manual-clear");
        if (!ReferenceEquals(displayRemainImage, runtimeRemainImage))
        {
            SafeDisposeRuntimeImage(displayRemainImage, "RemainImage-manual-clear");
        }

        RaisePropertyChanged(nameof(RemainImage));
    }

    private void ClearRuntimeResultImages()
    {
        HImage? lastConcatImage = LastConcatImage;
        HObject? runtimeCroppedImages = _runtimeCroppedImages;
        List<HImage> runtimeTargetImages = _runtimeTargetImages.ToList();
        if (lastConcatImage == null && runtimeCroppedImages == null && runtimeTargetImages.Count == 0)
        {
            return;
        }

        LogTrace(
            $"Clear previous line-scan outputs: LastConcat={DescribeImage(lastConcatImage)}, " +
            $"Cropped={DescribeImage(runtimeCroppedImages)}, TargetCount={runtimeTargetImages.Count}");

        LastConcatImage = null;
        _runtimeCroppedImages = null;
        CroppedImages = null;
        _runtimeTargetImages.Clear();
        TargetImages = [];

        SafeDisposeRuntimeImage(lastConcatImage, "LastConcatImage-previous-result");
        SafeDisposeRuntimeImage(runtimeCroppedImages, "_runtimeCroppedImages-previous-result");
        for (int index = 0; index < runtimeTargetImages.Count; index++)
        {
            SafeDisposeRuntimeImage(runtimeTargetImages[index], $"_runtimeTargetImages-previous-result[{index}]");
        }
    }

    private void ClearRuntimeImages()
    {
        HImage? runtimeRemainImage = _runtimeRemainImage;
        HImage? runtimeRemainMaskImage = _runtimeRemainMaskImage;
        HImage? displayRemainImage = RemainImage;
        HImage? lastConcatImage = LastConcatImage;
        HObject? runtimeCroppedImages = _runtimeCroppedImages;
        List<HImage> runtimeTargetImages = _runtimeTargetImages.ToList();
        LogTrace(
            $"清理运行态图像: Remain={DescribeImage(runtimeRemainImage)}, RemainMask={DescribeImage(runtimeRemainMaskImage)}, " +
            $"DisplayRemain={DescribeImage(displayRemainImage)}, LastConcat={DescribeImage(lastConcatImage)}, " +
            $"Cropped={DescribeImage(runtimeCroppedImages)}, TargetCount={runtimeTargetImages.Count}");

        _runtimeRemainImage = null;
        _runtimeRemainMaskImage = null;
        RemainImage = null;
        LastConcatImage = null;
        _runtimeCroppedImages = null;
        CroppedImages = null;
        _runtimeTargetImages.Clear();
        TargetImages = [];

        SafeDisposeRuntimeImage(runtimeRemainImage, "_runtimeRemainImage");
        SafeDisposeRuntimeImage(runtimeRemainMaskImage, "_runtimeRemainMaskImage");
        if (!ReferenceEquals(displayRemainImage, runtimeRemainImage))
        {
            SafeDisposeRuntimeImage(displayRemainImage, "RemainImage");
        }

        SafeDisposeRuntimeImage(lastConcatImage, "LastConcatImage");
        SafeDisposeRuntimeImage(runtimeCroppedImages, "_runtimeCroppedImages");
        for (int index = 0; index < runtimeTargetImages.Count; index++)
        {
            HImage image = runtimeTargetImages[index];
            SafeDisposeRuntimeImage(image, $"_runtimeTargetImages[{index}]");
        }
        LogTrace("运行态图像清理完成");
    }

    private void SafeDisposeRuntimeImage(HObject? image, string label)
    {
        if (image == null)
        {
            return;
        }

        try
        {
            image.Dispose();
        }
        catch (HOperatorException ex) when (IsDeletedObjectCleanup(ex))
        {
            LogWarning($"忽略运行态已删除对象释放异常: Label={label}, State={DescribeImage(image)}, Error={ex.Message}");
        }
        catch (Exception ex)
        {
            LogError($"运行态图像释放失败: Label={label}, State={DescribeImage(image)}, Error={ex}");
        }
    }

    private static bool IsDeletedObjectCleanup(HOperatorException ex)
    {
        return HalconImageOwnership.IsDeletedObjectError(ex);
    }

    #endregion

    #region 属性与输出参数刷新

    /// <summary>
    /// 通知界面刷新模块结果属性。
    /// </summary>
    private void RefreshOutputProperties()
    {
        RaisePropertyChanged(nameof(IncrementCount));
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(TargetImages));
        RaisePropertyChanged(nameof(CroppedImages));
        RaisePropertyChanged(nameof(RemainImage));
        RaisePropertyChanged(nameof(EdgeRows));
        RaisePropertyChanged(nameof(LastConcatImage));
        RaisePropertyChanged(nameof(StatusText));
        RaisePropertyChanged(nameof(ScaleFactor));
        RaisePropertyChanged(nameof(SmoothSigma));
        RaisePropertyChanged(nameof(EdgeThreshold));
        RaisePropertyChanged(nameof(MeasureCenterColumn));
        RaisePropertyChanged(nameof(MeasureRoiWidth));
        RaisePropertyChanged(nameof(CropRatio));
        RaisePropertyChanged(nameof(PreviewImageWidth));
        RaisePropertyChanged(nameof(PreviewRoiWidthMaximum));
        RaisePropertyChanged(nameof(PreviewImageObject));
        RaisePropertyChanged(nameof(MaskImage));
        RaisePropertyChanged(nameof(IncrementCount));
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(TargetImages));
        RaisePropertyChanged(nameof(CroppedImages));
    }

    private void RefreshFastOutputProperties()
    {
        RaisePropertyChanged(nameof(IncrementCount));
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(TargetImages));
        RaisePropertyChanged(nameof(CroppedImages));
    }

    /// <summary>
    /// 将当前输出属性同步到模块输出参数集合。
    /// </summary>
    private void RefreshOutputParams()
    {
        if (OutputParams == null || OutputParams.Count == 0)
        {
            SyncModuleOutputParams();
            return;
        }

        Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
        foreach (TransmitParam item in OutputParams)
        {
            if (!string.IsNullOrWhiteSpace(item.ParamName) && values.TryGetValue(item.ParamName, out object? value))
            {
                SetOutputParamValue(item, value);
            }
        }

        SyncModuleOutputParams();
    }

    private void RefreshOutputParamsFast()
    {
        if (OutputParams == null || OutputParams.Count == 0)
        {
            SyncModuleOutputParams();
            return;
        }

        foreach (TransmitParam item in OutputParams)
        {
            if (item.ParamName == nameof(IncrementCount))
            {
                item.Value = IncrementCount;
            }
            else if (item.ParamName == nameof(CroppedImages))
            {
                SetOutputParamValue(item, CroppedImages);
            }
        }

        SyncModuleOutputParams();
    }

    private static void SetOutputParamValue(TransmitParam item, object? value)
    {
        object? outputValue = CloneOutputValueForBoundary(value);
        DisposePreviousOutputValue(item.Value, value, outputValue);
        item.Value = outputValue;
    }

    private static object? CloneOutputValueForBoundary(object? value)
    {
        return value is HObject hObject
            ? SafeCloneHObject(hObject, "线扫计数克隆输出 HObject 失败")
            : value;
    }

    private static void DisposePreviousOutputValue(object? previousValue, object? sourceValue, object? nextValue)
    {
        if (previousValue is not HObject previousHObject ||
            ReferenceEquals(previousValue, sourceValue) ||
            ReferenceEquals(previousValue, nextValue))
        {
            return;
        }

        SafeDisposeHObject(previousHObject);
    }

    private static HObject? SafeCloneHObject(HObject? hObject, string logMessage)
    {
        if (hObject == null)
        {
            return null;
        }

        try
        {
            if (!hObject.IsInitialized())
            {
                return null;
            }

            HOperatorSet.CopyObj(hObject, out HObject copied, 1, -1);
            return copied;
        }
        catch (Exception ex)
        {
            try
            {
                return hObject.IsInitialized() ? hObject.Clone() : null;
            }
            catch
            {
                Logs.LogWarning($"{LogPrefix} {logMessage}: {ex.Message}");
                return null;
            }
        }
    }

    private static void SafeDisposeHObject(HObject? hObject)
    {
        HalconImageOwnership.DisposeOwned(hObject);
    }

    private void SyncModuleOutputParams()
    {
        moduleOutputParam ??= new ModuleParam();
        moduleOutputParam.TransmitParams = OutputParams?
            .Where(item => item != null)
            .ToDictionary(item => item.Guid.ToString(), item => (object)item)
            ?? [];
    }

    #endregion

    #region 输入参数元数据
    /// <summary>
    /// 创建默认输入图像参数。
    /// </summary>
    private static TransmitParam CreateInputImageParam()
    {
        return new TransmitParam
        {
            Name = "输入图像",
            ParamName = "InputImage",
            Resourece = ResoureceType.Inupt,
            Describe = "输入线扫图像"
        };
    }

    /// <summary>
    /// 修正旧项目中可能遗留的输入参数元数据，避免链接校验失败。
    /// </summary>
    private static void EnsureInputImageParamMetadata(TransmitParam inputImage)
    {
        if (inputImage.Resourece == ResoureceType.None)
        {
            inputImage.Resourece = ResoureceType.Inupt;
        }
        if (inputImage.Value == null && inputImage.Type == DataType.HObject)
        {
            inputImage.Type = DataType.None;
        }

        if (string.IsNullOrWhiteSpace(inputImage.Name))
        {
            inputImage.Name = "输入图像";
        }

        if (string.IsNullOrWhiteSpace(inputImage.ParamName))
        {
            inputImage.ParamName = "InputImage";
        }

        if (string.IsNullOrWhiteSpace(inputImage.Describe))
        {
            inputImage.Describe = "输入线扫图像";
        }
    }

    private static TransmitParam CreateMaskImageParam()
    {
        return new TransmitParam
        {
            Name = "掩膜图像",
            ParamName = "MaskImage",
            Resourece = ResoureceType.Inupt,
            Describe = "可选掩膜图像，用于边缘卡尺检测"
        };
    }

    private static void EnsureMaskImageParamMetadata(TransmitParam maskImage)
    {
        if (maskImage.Resourece == ResoureceType.None)
        {
            maskImage.Resourece = ResoureceType.Inupt;
        }
        if (maskImage.Value == null && maskImage.Type == DataType.HObject)
        {
            maskImage.Type = DataType.None;
        }

        if (string.IsNullOrWhiteSpace(maskImage.Name))
        {
            maskImage.Name = "掩膜图像";
        }

        if (string.IsNullOrWhiteSpace(maskImage.ParamName))
        {
            maskImage.ParamName = "MaskImage";
        }

        if (string.IsNullOrWhiteSpace(maskImage.Describe))
        {
            maskImage.Describe = "可选掩膜图像，用于边缘卡尺检测";
        }
    }

    #endregion
}

internal sealed class LineScanSheetCounterDialogState
{
    public LineScanSheetCounterDialogState(
        TransmitParam? inputImage,
        TransmitParam? maskImage,
        List<TransmitParam> outputParams,
        double scaleFactor,
        double smoothSigma,
        double edgeThreshold,
        double measureCenterColumn,
        double measureRoiWidth,
        double cropRatio,
        bool resetBeforeNextRun,
        bool isFastModeEnabled,
        bool saveSheetImages,
        bool saveConcatImage,
        bool saveRemainImage,
        string saveDirectory)
    {
        InputImage = inputImage;
        MaskImage = maskImage;
        OutputParams = outputParams ?? [];
        ScaleFactor = scaleFactor;
        SmoothSigma = smoothSigma;
        EdgeThreshold = edgeThreshold;
        MeasureCenterColumn = measureCenterColumn;
        MeasureRoiWidth = measureRoiWidth;
        CropRatio = cropRatio;
        ResetBeforeNextRun = resetBeforeNextRun;
        IsFastModeEnabled = isFastModeEnabled;
        SaveSheetImages = saveSheetImages;
        SaveConcatImage = saveConcatImage;
        SaveRemainImage = saveRemainImage;
        SaveDirectory = saveDirectory;
    }

    public TransmitParam? InputImage { get; }
    public TransmitParam? MaskImage { get; }
    public List<TransmitParam> OutputParams { get; }
    public double ScaleFactor { get; }
    public double SmoothSigma { get; }
    public double EdgeThreshold { get; }
    public double MeasureCenterColumn { get; }
    public double MeasureRoiWidth { get; }
    public double CropRatio { get; }
    public bool ResetBeforeNextRun { get; }
    public bool IsFastModeEnabled { get; }
    public bool SaveSheetImages { get; }
    public bool SaveConcatImage { get; }
    public bool SaveRemainImage { get; }
    public string SaveDirectory { get; }
}

using ALGO.DeepLearning.Models;
using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ALGO.DeepLearning.Models.DeepLearningSdk;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.DeepLearning
{
    [Serializable]
    public class DeepLearningModel : ModelParamBase
    {
        #region 字段与运行状态
        [JsonIgnore]
        public ModelConfig ModelConfig { get; set; } = new ModelConfig();

        [JsonIgnore]
        public DeepLearningSdk DLSdk { get; set; }

        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; } = new VMHWindowControl();

        [JsonIgnore]
        public HImage DisposeImage { get; set; }

        [JsonIgnore]
        public List<HImage> DisposeImages { get; set; } = new List<HImage>();

        [JsonIgnore]
        public HImage DisposeDepth { get; set; }

        [JsonIgnore]
        public List<HImage> DisposeDepthes { get; set; } = new List<HImage>();

        private bool _modelInitialized = false;
        [JsonIgnore]
        public bool ModelInitialized 
        {
            get 
            {
                return _modelInitialized;
            }
            set
            {
                _modelInitialized = value;
            } 
        }

        [JsonIgnore]
        private readonly object _executionSyncRoot = new object();

        [JsonIgnore]
        private bool _suppressLoadPreview;

        [JsonIgnore]
        private bool _previewUsesExecutionResults;

        private static long _executeLogSequence;
        #endregion

        #region 参数属性
        private Guid _inputImageGuid;
        public Guid InputImageGuid
        {
            get => _inputImageGuid;
            set { _inputImageGuid = value; RaisePropertyChanged(); }
        }

        private string _inputImageName;
        public string InputImageName
        {
            get => _inputImageName;
            set { _inputImageName = value; RaisePropertyChanged(); }
        }

        private string _inputImagesName;
        public string InputImagesName
        {
            get => _inputImagesName;
            set { _inputImagesName = value; RaisePropertyChanged(); }
        }

        private Guid _inputDepthGuid;
        public Guid InputDepthGuid
        {
            get => _inputDepthGuid;
            set { _inputDepthGuid = value; RaisePropertyChanged(); }
        }

        private string _inputDepthName;
        public string InputDepthName
        {
            get => _inputDepthName;
            set { _inputDepthName = value; RaisePropertyChanged(); }
        }

        private string _inputDepthesName;
        public string InputDepthesName
        {
            get => _inputDepthesName;
            set { _inputDepthesName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get => _inputImage;
            set
            {
                _inputImage = value;
                if (value != null)
                {
                    InputImageGuid = value.Guid;
                    InputImageName = value.Name;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(InputImageName));
                RefreshLinkedInputPreview(value);
            }
        }

        [JsonIgnore]
        private TransmitParam _inputDepth = new TransmitParam();
        /// <summary>
        /// 输入深度图参数
        /// </summary>
        public TransmitParam InputDepth
        {
            get => _inputDepth;
            set
            {
                _inputDepth = value;
                if (value != null)
                {
                    InputDepthGuid = value.Guid;
                    InputDepthName = value.Name;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(InputDepthName));
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        
        [JsonIgnore]
        private string _modelFilePath = null;
        /// <summary>
        /// 模型文件路径
        /// </summary>
        [RecipeParam("模型路径", "模型推理使用的 onnx/kmodel 文件路径")]
        public string ModelFilePath
        {
            get { return _modelFilePath; }
            set 
            { 
                if (string.Equals(_modelFilePath, value, StringComparison.Ordinal))
                {
                    return;
                }

                _modelFilePath = value;
                EnsureModelConfig().ModelPath = _modelFilePath;
                MarkModelConfigChanged();

                RaisePropertyChanged(); 
            }
        }


        [JsonIgnore]
        private int _batchSize = 1;
        /// <summary>
        /// 推理批次数
        /// </summary>
        [RecipeParam("BatchSize", "模型推理批次数")]
        public int BatchSize
        {
            get { return _batchSize; }
            set 
            { 
                if (_batchSize == value)
                {
                    return;
                }

                _batchSize = value;
                EnsureModelConfig().BatchSize = _batchSize;
                MarkModelConfigChanged();

                RaisePropertyChanged(); 
            }
        }


        [JsonIgnore]
        private double _confidenceThreshold = 0.5;
        /// <summary>
        /// 置信度阈值
        /// </summary>
        [RecipeParam("置信度阈值", "检测类模型的置信度阈值")]
        public double ConfidenceThreshold
        {
            get { return _confidenceThreshold; }
            set 
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (Math.Abs(_confidenceThreshold - clamped) > double.Epsilon)
                {
                    _confidenceThreshold = clamped;
                    EnsureModelConfig().ConfidenceThreshold = _confidenceThreshold;
                    MarkModelConfigChanged();

                    RaisePropertyChanged();
                }
            }
        }


        [JsonIgnore]
        private double _IoUThreshold = 0.5;
        /// <summary>
        /// IoU阈值
        /// </summary>
        [RecipeParam("IoU阈值", "检测类模型的交并比阈值")]
        public double IoUThreshold
        {
            get { return _IoUThreshold; }
            set
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (Math.Abs(_IoUThreshold - clamped) > double.Epsilon)
                {
                    _IoUThreshold = clamped;
                    EnsureModelConfig().IoUThreshold = _IoUThreshold;
                    MarkModelConfigChanged();

                    RaisePropertyChanged();
                }
            }
        }


        [JsonIgnore]
        private double _segmentationThreshold = 0.5;
        /// <summary>
        /// 分割阈值
        /// </summary>
        [RecipeParam("分割阈值", "分割类模型的掩膜阈值")]
        public double SegmentationThreshold
        {
            get { return _segmentationThreshold; }
            set
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (Math.Abs(_segmentationThreshold - clamped) > double.Epsilon)
                {
                    _segmentationThreshold = clamped;
                    EnsureModelConfig().SegmentationThreshold = _segmentationThreshold;
                    MarkModelConfigChanged();

                    RaisePropertyChanged();
                }
            }
        }


        [JsonIgnore]
        private double _keypointThreshold = 0.5;
        /// <summary>
        /// 关键点阈值
        /// </summary>
        [RecipeParam("关键点阈值", "关键点模型的点位置信度阈值")]
        public double KeypointThreshold
        {
            get { return _keypointThreshold; }
            set
            {
                double clamped = Math.Clamp(value, 0.0, 1.0);
                if (Math.Abs(_keypointThreshold - clamped) > double.Epsilon)
                {
                    _keypointThreshold = clamped;
                    EnsureModelConfig().KeypointThreshold = _keypointThreshold;
                    MarkModelConfigChanged();

                    RaisePropertyChanged();
                }
            }
        }


        [JsonIgnore]
        private eDeepLearningDeviceType _deviceType = eDeepLearningDeviceType.CPU;
        /// <summary>
        /// 推理设备类型
        /// </summary>
        [RecipeParam("推理设备", "模型推理使用 CPU 或 GPU")]
        public eDeepLearningDeviceType DeviceType
        {
            get { return _deviceType; }
            set 
            { 
                if (_deviceType == value)
                {
                    return;
                }

                _deviceType = value;
                EnsureModelConfig().DeviceType = _deviceType;
                MarkModelConfigChanged();

                RaisePropertyChanged(); 
            }
        }

        [JsonIgnore]
        private eDeepLearningModelType _modelType = eDeepLearningModelType.分类模型;
        /// <summary>
        /// 推理设备类型
        /// </summary>
        [RecipeParam("模型类型", "模型推理结果解析类型")]
        public eDeepLearningModelType ModelType
        {
            get { return _modelType; }
            set 
            { 
                if (_modelType == value)
                {
                    return;
                }

                _modelType = value;
                EnsureModelConfig().ModelType = _modelType;
                MarkModelConfigChanged();

                RaisePropertyChanged(); 
            }
        }


        [JsonIgnore]
        private bool _isFastModeEnabled = false;
        /// <summary>
        /// 急速模式：跳过图像预览和结果绘制，只保留推理计算与输出。
        /// </summary>
        [RecipeParam("急速模式", "开启后关闭图像预览和结果覆盖层绘制")]
        public bool IsFastModeEnabled
        {
            get { return _isFastModeEnabled; }
            set
            {
                if (SetProperty(ref _isFastModeEnabled, value) && value)
                {
                    ClearRuntimePreviewDisplay();
                }
            }
        }

        [JsonIgnore]
        private bool _isImageList = false;
        /// <summary>当前输入是否为多图，执行时由输入 HObject 数量自动刷新。</summary>
        public bool IsImageList
        {
            get { return _isImageList; }
            set { SetProperty(ref _isImageList, value); }
        }

        [JsonIgnore]
        [OutputParam("Results", "模型推理结果")]
        /// <summary>
        /// 按输入图像顺序分组的推理结果，单图也保留一层分组，避免单图/多图输出契约分裂。
        /// </summary>
        public List<List<Result>> Results { get; set; } = new List<List<Result>>();

        [JsonIgnore]
        [OutputParam("SourceImages", "模型实际输入图像")]
        /// <summary>
        /// 本次模型实际推理使用的输入图像，供下游模块与推理结果保持同步。
        /// </summary>
        public HObject SourceImages { get; set; }

        [JsonIgnore]
        private HObject _previewImageObject;
        /// <summary>
        /// 预览窗口显示的 HALCON 图像副本。
        /// </summary>
        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get { return _previewImageObject; }
            private set { SetProperty(ref _previewImageObject, value); }
        }

        /// <summary>
        /// 预览窗口结果覆盖层，图像和结果在同一次刷新里同步替换。
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new ObservableCollection<HalconDrawingObject>();

        /// <summary>
        /// 预览窗口分类结果文字，分类模型没有几何轮廓时用文字叠加显示。
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<string> PreviewResultLabels { get; } = new ObservableCollection<string>();

        [JsonIgnore]
        private int _currentPreviewImageIndex;
        /// <summary>
        /// 当前预览窗口显示的输入图像序号。
        /// </summary>
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
            get { return DisposeImages?.Count ?? 0; }
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
            get { return PreviewImageCount > 0 && CurrentPreviewImageIndex > 0; }
        }

        [JsonIgnore]
        public bool CanMoveNextPreviewImage
        {
            get { return PreviewImageCount > 0 && CurrentPreviewImageIndex < PreviewImageCount - 1; }
        }

        #endregion

        #region 生命周期状态
        private bool _disposed = false;
        #endregion

        #region 重写方法
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns>关键参数是否加载成功。</returns>
        public override bool LoadKeyParam()
        {
            lock (_executionSyncRoot)
            {
                return LoadKeyParamCore();
            }
        }

        /// <summary>
        /// 界面打开、确认或手动执行时尝试刷新输入；模块正在推理时直接跳过，避免 UI 等执行锁卡死。
        /// </summary>
        public bool TryLoadKeyParamForUi()
        {
            if (!Monitor.TryEnter(_executionSyncRoot))
            {
                LogWarning($"模型正在执行，跳过界面参数刷新: Serial={Serial}");
                return false;
            }

            try
            {
                return LoadKeyParamCore();
            }
            finally
            {
                Monitor.Exit(_executionSyncRoot);
            }
        }

        private bool LoadKeyParamCore()
        {
            try
            {
                base.LoadKeyParam();
                ModuleName = Serial.ToString("D3");
                if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(ModuleName))
                {
                    PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(ModuleName, mWindowH);
                }
                else
                {
                    mWindowH = PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[ModuleName] as VMHWindowControl;
                }

                if (!TryResolveInputHObject(_inputImage, "Image", out HObject newImage))
                {
                    LogInvalidInputParam("Image", _inputImage, null);
                    ClearInferenceInputState();
                    return false;
                }

                if (!TryLoadInputImages(newImage, "Image", out List<HImage> imageList))
                {
                    LogInvalidInputParam("Image", _inputImage, newImage);
                    ClearInferenceInputState();
                    return false;
                }

                List<HImage> depthList = new List<HImage>();
                if (HasLinkedParam(_inputDepth))
                {
                    if (!TryResolveInputHObject(_inputDepth, "Depth", out HObject newDepth))
                    {
                        LogInvalidInputParam("Depth", _inputDepth, null);
                        imageList.Dispose();
                        ClearInferenceInputState();
                        return false;
                    }

                    if (!TryLoadInputImages(newDepth, "Depth", out depthList))
                    {
                        LogInvalidInputParam("Depth", _inputDepth, newDepth);
                        imageList.Dispose();
                        ClearInferenceInputState();
                        return false;
                    }

                    if (depthList.Count != imageList.Count)
                    {
                        LogWarning($"深度图数量与输入图像数量不一致: ImageCount={imageList.Count}, DepthCount={depthList.Count}");
                        imageList.Dispose();
                        depthList.Dispose();
                        ClearInferenceInputState();
                        return false;
                    }
                }

                ReplaceSourceImages(CloneHObject(newImage));
                ReplaceRuntimeImages(imageList, depthList);
                IsImageList = DisposeImages.Count > 1;

                if (!_suppressLoadPreview)
                {
                    DisplayLoadedInputPreview();
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"加载参数异常: Error={ex}");
                return false;
            }
        }

        private static bool TryGetInputObjectCount(HObject image, out int count)
        {
            count = 0;
            try
            {
                if (image == null || !image.IsInitialized())
                {
                    return false;
                }

                HOperatorSet.CountObj(image, out HTuple objectCount);
                count = objectCount.I;
                return count > 0;
            }
            catch (Exception ex)
            {
                LogLoadKeyParamImageStageFailure("CountObj", 0, ex);
                return false;
            }
        }

        /// <summary>
        /// 判断输入参数是否已经链接，避免空名称误匹配上游参数。
        /// </summary>
        private static bool HasLinkedParam(TransmitParam param)
        {
            return param != null
                && (TryGetInputHObject(param.Value, out _) || HasInputLinkMetadata(param));
        }

        private static bool HasInputLinkMetadata(TransmitParam param)
        {
            return param != null
                && (param.Resourece == ResoureceType.Inupt
                    || param.Resourece == ResoureceType.LastInput
                    || param.Resourece == ResoureceType.Global
                    || param.Resourece == ResoureceType.CustomGlobal
                    || param.LinkGuid != Guid.Empty
                    || !string.IsNullOrWhiteSpace(param.Name)
                    || !string.IsNullOrWhiteSpace(param.ParamName)
                    || !string.IsNullOrWhiteSpace(param.ResourcePath));
        }

        /// <summary>
        /// 解析输入图像对象：优先使用当前输入快照，缺失时回退全局参数、节点输出缓存和 WxLink 当前值。
        /// </summary>
        private bool TryResolveInputHObject(TransmitParam param, string stage, out HObject inputObject)
        {
            inputObject = null;
            object value = ResolveInputParamValue(param);
            if (TryGetInputHObject(value, out inputObject))
            {
                if (param != null && !ReferenceEquals(param.Value, value))
                {
                    param.Value = value;
                }

                return true;
            }

            LogWarning($"输入图像解析失败: Stage={stage}, Param={DescribeTransmitParam(param)}, Resolved={DescribeInputValue(value)}");
            return false;
        }

        /// <summary>
        /// 从当前选择和上游缓存解析输入值，不依赖 Resourece 单一路径，避免有效链接被判为 NotRun。
        /// </summary>
        private object ResolveInputParamValue(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            object directValue = TryGetInputHObject(param.Value, out _) ? param.Value : null;
            if (!HasInputLinkMetadata(param))
            {
                return directValue;
            }

            object resolvedValue = ResolveTransmitParamValue(InputParams, param, false);
            if (TryGetInputHObject(resolvedValue, out _))
            {
                return resolvedValue;
            }

            TransmitParam linkedParam = FindMatchingTransmitParam(InputParams, param);
            if (TryGetInputHObject(linkedParam?.Value, out _))
            {
                return linkedParam.Value;
            }

            TransmitParam moduleInputParamValue = FindMatchingModuleInputParam(param);
            if (TryGetInputHObject(moduleInputParamValue?.Value, out _))
            {
                return moduleInputParamValue.Value;
            }

            TransmitParam globalParam = FindCurrentGlobalParam(param);
            if (TryGetInputHObject(globalParam?.Value, out _))
            {
                return globalParam.Value;
            }

            TransmitParam cachedParam = FindCachedOutputParam(param);
            if (TryGetInputHObject(cachedParam?.Value, out _))
            {
                return cachedParam.Value;
            }

            return directValue;
        }

        private static bool TryGetInputHObject(object value, out HObject inputObject)
        {
            inputObject = value as HObject;
            if (inputObject == null)
            {
                return false;
            }

            try
            {
                return inputObject.IsInitialized();
            }
            catch
            {
                inputObject = null;
                return false;
            }
        }

        private TransmitParam FindMatchingModuleInputParam(TransmitParam param)
        {
            return FindMatchingTransmitParam(moduleInputParam?.TransmitParams?.Values.OfType<TransmitParam>(), param);
        }

        private TransmitParam FindCurrentGlobalParam(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            IEnumerable<TransmitParam> candidates = param.Resourece switch
            {
                ResoureceType.Global => PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams,
                ResoureceType.CustomGlobal => PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams,
                _ => null
            };

            return FindMatchingTransmitParam(candidates, param);
        }

        private TransmitParam FindCachedOutputParam(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            var outputCache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
            if (outputCache == null)
            {
                return null;
            }

            foreach (string key in EnumerateOutputCacheKeys(param.Serial))
            {
                if (outputCache.TryGetValue(key, out ObservableCollection<TransmitParam> cachedParams))
                {
                    TransmitParam cachedParam = FindMatchingTransmitParam(cachedParams, param);
                    if (cachedParam != null)
                    {
                        return cachedParam;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> EnumerateOutputCacheKeys(int serial)
        {
            yield return serial.ToString();
            yield return serial.ToString("D3");
        }

        private static TransmitParam FindMatchingTransmitParam(IEnumerable<TransmitParam> candidates, TransmitParam target)
        {
            if (candidates == null || target == null)
            {
                return null;
            }

            return candidates.FirstOrDefault(item =>
                    item != null && target.Guid != Guid.Empty && item.Guid == target.Guid)
                ?? candidates.FirstOrDefault(item =>
                    item != null
                    && item.Serial == target.Serial
                    && !string.IsNullOrWhiteSpace(item.Name)
                    && item.Name == target.Name)
                ?? candidates.FirstOrDefault(item =>
                    item != null
                    && !string.IsNullOrWhiteSpace(item.ResourcePath)
                    && item.ResourcePath == target.ResourcePath)
                ?? candidates.FirstOrDefault(item =>
                    item != null
                    && !string.IsNullOrWhiteSpace(item.ParamName)
                    && item.ParamName == target.ParamName)
                ?? candidates.FirstOrDefault(item =>
                    item != null
                    && target.LinkGuid != Guid.Empty
                    && !string.IsNullOrWhiteSpace(item.Name)
                    && item.LinkGuid == target.LinkGuid
                    && item.Name == target.Name);
        }

        /// <summary>
        /// 链接图像后立即刷新预览和调试态输入缓存，执行时仍会重新解析当前帧。
        /// </summary>
        private void RefreshLinkedInputPreview(TransmitParam param)
        {
            if (_suppressLoadPreview || !ShouldDisplayLoadedInputPreview())
            {
                return;
            }

            if (!TryGetInputHObject(param?.Value, out HObject inputObject))
            {
                return;
            }

            if (!TryLoadInputImages(inputObject, "Preview", out List<HImage> imageList))
            {
                return;
            }

            bool ownershipTransferred = false;
            try
            {
                ReplaceSourceImages(CloneHObject(inputObject));
                ReplaceRuntimeImages(imageList, new List<HImage>());
                ownershipTransferred = true;
                IsImageList = DisposeImages.Count > 1;
                DisplayLoadedInputPreview();
            }
            catch (Exception ex)
            {
                LogWarning($"链接图像预览刷新失败: Image={DescribeHObject(inputObject)}, Error={ex}");
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    imageList.Dispose();
                }
            }
        }

        /// <summary>
        /// 将输入 HObject 按对象数量拆成独立 HImage 副本，后续单图和多图统一按列表执行。
        /// </summary>
        private static bool TryLoadInputImages(HObject inputObject, string stage, out List<HImage> images)
        {
            images = new List<HImage>();
            if (!TryGetInputObjectCount(inputObject, out int objectCount))
            {
                return false;
            }

            for (int i = 1; i <= objectCount; i++)
            {
                if (!TryCopySelectedInputImage(inputObject, i, stage, out HImage copiedImage))
                {
                    images.Dispose();
                    images = new List<HImage>();
                    return false;
                }

                images.Add(copiedImage);
            }

            return images.Count > 0;
        }

        private static bool TryCopySelectedInputImage(HObject source, int index, string stage, out HImage copiedImage)
        {
            copiedImage = null;
            HImage selectedImage = null;
            try
            {
                selectedImage = new HImage(source.SelectObj(index));
                copiedImage = selectedImage.CopyImage();
                return copiedImage != null && copiedImage.IsInitialized();
            }
            catch (Exception ex)
            {
                LogLoadKeyParamImageStageFailure(stage, index, ex);
                copiedImage?.Dispose();
                copiedImage = null;
                return false;
            }
            finally
            {
                selectedImage?.Dispose();
            }
        }

        private void LogInvalidInputParam(string stage, TransmitParam transmitParam, object resolvedValue)
        {
            LogWarning(
                $"输入参数无效: Stage={stage}, Param={DescribeTransmitParam(transmitParam)}, " +
                $"MatchedInput={DescribeInputParamMatch(transmitParam)}, InputParamsCount={InputParams?.Count ?? 0}, " +
                $"ResolvedValue={DescribeInputValue(resolvedValue)}");
        }

        private static void LogLoadKeyParamImageStageFailure(string stage, int index, Exception ex)
        {
            LogError($"输入图像加载阶段失败: Stage={stage}, Index={index}, Error={ex}");
        }

        private string DescribeInputParamMatch(TransmitParam param)
        {
            if (param == null || InputParams == null)
            {
                return "null";
            }

            TransmitParam matched = InputParams.FirstOrDefault(item =>
                item != null &&
                ((param.Guid != Guid.Empty && item.Guid == param.Guid) ||
                 (!string.IsNullOrWhiteSpace(param.Name) && item.Name == param.Name) ||
                 (!string.IsNullOrWhiteSpace(param.ParamName) && item.ParamName == param.ParamName)));

            return DescribeTransmitParam(matched);
        }

        private static string DescribeTransmitParam(TransmitParam param)
        {
            if (param == null)
            {
                return "null";
            }

            return $"Guid={param.Guid}, Name={param.Name}, ParamName={param.ParamName}, LinkGuid={param.LinkGuid}, Serial={param.Serial}, Type={param.Type}, Value={DescribeInputValue(param.Value)}";
        }

        private static string DescribeInputValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            return value is HObject hObject ? DescribeHObject(hObject) : value.GetType().FullName;
        }

        private bool ShouldDisplayLoadedInputPreview()
        {
            if (IsFastModeEnabled)
            {
                return false;
            }

            return PrismProvider.ProjectManager?.SltCurSolutionItem?.IsManual != false;
        }

        private void DisplayLoadedInputPreview()
        {
            if (!ShouldDisplayLoadedInputPreview())
            {
                return;
            }

            try
            {
                _previewUsesExecutionResults = false;
                RefreshCurrentPreviewImage(useCurrentResults: false);
            }
            catch (Exception ex)
            {
                LogWarning($"输入预览刷新失败: ImageIndex={CurrentPreviewImageIndex}, Error={ex}");
            }
        }

        /// <summary>
        /// 执行结束后统一刷新预览，保证图像和结果层来自同一帧输入。
        /// </summary>
        private void RefreshExecutionPreview()
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            try
            {
                _previewUsesExecutionResults = true;
                RefreshCurrentPreviewImage(useCurrentResults: true);
            }
            catch (Exception ex)
            {
                LogWarning($"执行预览刷新失败: ImageIndex={CurrentPreviewImageIndex}, ResultCount={GetPreviewResults(CurrentPreviewImageIndex)?.Count ?? 0}, Error={ex}");
            }
        }

        /// <summary>
        /// 按偏移切换预览图像，保持图像和对应结果同步刷新。
        /// </summary>
        public void MovePreviewImage(int offset)
        {
            SelectPreviewImage(CurrentPreviewImageIndex + offset);
        }

        public void SelectPreviewImage(int imageIndex)
        {
            CurrentPreviewImageIndex = NormalizePreviewImageIndex(imageIndex);
            RefreshCurrentPreviewImage(_previewUsesExecutionResults);
        }

        private void RefreshCurrentPreviewImage(bool useCurrentResults)
        {
            if (IsFastModeEnabled)
            {
                ClearRuntimePreviewDisplay();
                return;
            }

            CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
            HImage previewImage = GetPreviewImage(CurrentPreviewImageIndex);
            if (previewImage == null)
            {
                ClearRuntimePreviewDisplay();
                return;
            }

            RefreshPreviewDisplay(previewImage, useCurrentResults ? GetPreviewResults(CurrentPreviewImageIndex) : null);
        }

        private HImage GetPreviewImage(int imageIndex)
        {
            int normalizedIndex = NormalizePreviewImageIndex(imageIndex);
            return DisposeImages != null && normalizedIndex < DisposeImages.Count
                ? DisposeImages[normalizedIndex]
                : DisposeImage;
        }

        private List<Result> GetPreviewResults(int imageIndex)
        {
            int normalizedIndex = NormalizePreviewImageIndex(imageIndex);
            return Results != null && normalizedIndex < Results.Count
                ? Results[normalizedIndex]
                : null;
        }

        private int NormalizePreviewImageIndex(int imageIndex)
        {
            int imageCount = PreviewImageCount;
            if (imageCount <= 0)
            {
                return 0;
            }

            return Math.Clamp(imageIndex, 0, imageCount - 1);
        }

        private void RaisePreviewImageNavigationProperties()
        {
            RaisePropertyChanged(nameof(CurrentPreviewImageIndex));
            RaisePropertyChanged(nameof(PreviewImageCount));
            RaisePropertyChanged(nameof(CurrentPreviewImageDisplayText));
            RaisePropertyChanged(nameof(CanMovePreviousPreviewImage));
            RaisePropertyChanged(nameof(CanMoveNextPreviewImage));
        }

        /// <summary>
        /// 同步刷新预览图像和结果覆盖层，避免先显示图像后显示结果的错帧感。
        /// </summary>
        private void RefreshPreviewDisplay(HImage image, List<Result> results)
        {
            if (IsFastModeEnabled)
            {
                ClearRuntimePreviewDisplay();
                return;
            }

            if (image == null)
            {
                ClearRuntimePreviewDisplay();
                return;
            }

            HObject previewImage = CloneHObject(image);
            List<HalconDrawingObject> previewObjects = CreateResultPreviewObjects(results);
            List<string> previewLabels = CreateResultPreviewLabels(results);

            bool isScheduled = RunOnPreviewDispatcher(() =>
            {
                try
                {
                    SetPreviewImageObject(previewImage);
                    previewImage = null;
                    ClearPreviewDrawObjects();
                    ClearPreviewResultLabels();

                    while (previewObjects.Count > 0)
                    {
                        HalconDrawingObject drawObject = previewObjects[0];
                        previewObjects.RemoveAt(0);
                        PreviewDrawObjects.Add(drawObject);
                    }

                    foreach (string label in previewLabels)
                    {
                        PreviewResultLabels.Add(label);
                    }
                }
                finally
                {
                    SafeDisposeHObject(previewImage, "Preview.Image.Unused");
                    DisposePreviewDrawObjects(previewObjects);
                }
            });

            if (!isScheduled)
            {
                SafeDisposeHObject(previewImage, "Preview.Image.ScheduleFailed");
                DisposePreviewDrawObjects(previewObjects);
            }
        }

        private static bool RunOnPreviewDispatcher(Action action)
        {
            if (action == null)
            {
                return false;
            }

            var dispatcher = PrismProvider.Dispatcher;
            try
            {
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"预览异步刷新失败: Error={ex}");
                        }
                    }));
                    return true;
                }

                action();
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"预览调度失败: Error={ex}");
                return false;
            }
        }

        private void ClearInferenceInputState()
        {
            IsImageList = false;
            ReplaceSourceImages(null);
            ReplaceResults(new List<List<Result>>());
            ReplaceRuntimeImages(new List<HImage>(), new List<HImage>());
            ClearRuntimePreviewDisplay();

            if (_inputImage != null)
            {
                _inputImage.Value = null;
            }

            if (_inputDepth != null)
            {
                _inputDepth.Value = null;
            }
        }

        /// <summary>
        /// 替换本次运行使用的图像缓存，传入列表所有权转移给当前模块。
        /// </summary>
        private void ReplaceRuntimeImages(List<HImage> nextImages, List<HImage> nextDepthImages)
        {
            DisposeImage?.Dispose();
            DisposeImage = nextImages?.FirstOrDefault()?.CopyImage();

            DisposeDepth?.Dispose();
            DisposeDepth = nextDepthImages?.FirstOrDefault()?.CopyImage();

            DisposeImages?.Dispose();
            DisposeImages = nextImages ?? new List<HImage>();

            DisposeDepthes?.Dispose();
            DisposeDepthes = nextDepthImages ?? new List<HImage>();

            CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
            RaisePreviewImageNavigationProperties();
        }

        private void ReplaceSourceImages(HObject nextSourceImages)
        {
            if (!ReferenceEquals(SourceImages, nextSourceImages))
            {
                SafeDisposeHObject(SourceImages, "SourceImages");
            }

            SourceImages = nextSourceImages;
        }

        public override void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // 从 ImgControlPair 中移除当前窗口并释放，避免后续复用已释放的控件
            var project = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (project?.ImgControlPair != null && mWindowH != null)
            {
                var kvp = project.ImgControlPair.FirstOrDefault(p => ReferenceEquals(p.Value, mWindowH));
                if (!kvp.Equals(default(KeyValuePair<string, VMHWindowControl>)))
                {
                    (kvp.Value as VMHWindowControl)?.Dispose();
                    project.ImgControlPair.Remove(kvp.Key);
                }
                else
                {
                    mWindowH.Dispose();
                }
            }
            else
            {
                mWindowH?.Dispose();
            }

            ReplaceSourceImages(null);
            ReplaceRuntimeImages(new List<HImage>(), new List<HImage>());
            ReplaceResults(new List<List<Result>>());
            ClearRuntimePreviewDisplay();
            base.Dispose();
            DLSdk?.Dispose();
            DLSdk = null;

        }


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
        #endregion

        #region 执行与辅助方法
        /// <summary>
        /// 确保模型配置对象存在，并同步当前界面参数。
        /// </summary>
        private ModelConfig EnsureModelConfig()
        {
            ModelConfig ??= new ModelConfig();
            ModelConfig.ModelPath = ModelFilePath ?? string.Empty;
            ModelConfig.BatchSize = BatchSize;
            ModelConfig.DeviceType = DeviceType;
            ModelConfig.ModelType = ModelType;
            ModelConfig.ConfidenceThreshold = ConfidenceThreshold;
            ModelConfig.IoUThreshold = IoUThreshold;
            ModelConfig.SegmentationThreshold = SegmentationThreshold;
            ModelConfig.KeypointThreshold = KeypointThreshold;
            return ModelConfig;
        }

        /// <summary>
        /// 模型配置变化后，下次执行必须重新初始化原生推理运行时。
        /// </summary>
        private void MarkModelConfigChanged()
        {
            ModelInitialized = false;
        }

        /// <summary>
        /// 加载模型
        /// </summary>
        /// <returns>模型是否加载成功。</returns>
        /// <exception cref="Exception">模型文件缺失或原生运行时加载失败。</exception>
        public bool LoadModel(bool forceReload = false)
        {
            lock (_executionSyncRoot)
            {
                return LoadModelCore(forceReload);
            }
        }

        /// <summary>
        /// 界面加载模型时不等待正在运行的推理，避免界面线程卡死；按钮加载可强制重建 Native runtime。
        /// </summary>
        public bool TryLoadModelForUi(bool forceReload = false)
        {
            if (!Monitor.TryEnter(_executionSyncRoot))
            {
                LogWarning($"模型正在执行，跳过界面模型加载: Serial={Serial}");
                return false;
            }

            try
            {
                return LoadModelCore(forceReload);
            }
            finally
            {
                Monitor.Exit(_executionSyncRoot);
            }
        }

        private bool LoadModelCore(bool forceReload = false)
        {
            ModelConfig config = EnsureModelConfig();

            if (!forceReload && ModelInitialized && DLSdk != null)
            {
                return true;
            }

            DLSdk?.Dispose();
            DLSdk = null;

            DLSdk = new DeepLearningSdk(config);
            int state = DLSdk.InitRuntime(config);
            if (state != 0)
            {
                DLSdk.Dispose();
                DLSdk = null;
                ModelInitialized = false;
                return false;
            }

            ModelInitialized = true;
            return true;
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns>执行输出，包含状态和耗时。</returns>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            long runId = Interlocked.Increment(ref _executeLogSequence);
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                lock (_executionSyncRoot)
                {
                try
                {
                    LogInfo($"模型推理开始: RunId={runId}, Serial={Serial}, IsDebug={IsDebug}, IsImageList={IsImageList}, ModelType={ModelType}, ModelInitialized={ModelInitialized}, Image={DescribeHObject(DisposeImage)}, Depth={DescribeHObject(DisposeDepth)}, ImageListCount={DisposeImages?.Count ?? 0}, DepthListCount={DisposeDepthes?.Count ?? 0}");
                    #region 检测参数（每次触发都重新解析输入，避免界面打开后 IsDebug 导致连续触发复用旧图）
                    _suppressLoadPreview = true;
                    try
                    {
                        if (!LoadKeyParamCore())
                        {
                            LogWarning($"模型推理参数加载失败: RunId={runId}, Image={DescribeHObject(DisposeImage)}, Depth={DescribeHObject(DisposeDepth)}, SourceImages={DescribeHObject(SourceImages)}");
                            UpdateOutputParamValues();
                            return NodeStatus.NotRun;
                        }
                    }
                    finally
                    {
                        _suppressLoadPreview = false;
                    }

                    // 检查模型是否已经加载。
                    if (!LoadModelCore(forceReload: false))
                    {
                        LogWarning($"模型推理模型未加载: RunId={runId}, ModelType={ModelType}, ModelInitialized={ModelInitialized}");
                        return NodeStatus.Error;
                    }
                    #endregion

                    if (DisposeImages == null || DisposeImages.Count == 0)
                    {
                        LogWarning($"模型推理没有可用输入图像: RunId={runId}");
                        UpdateOutputParamValues();
                        return NodeStatus.NotRun;
                    }

                    if (!IsFastModeEnabled)
                    {
                        ClearResultRois();
                    }

                    var nextResults = new List<List<Result>>();

                    for (int index = 0; index < DisposeImages.Count; index++)
                    {
                        HImage tempImage = null;
                        HImage tempDepth = null;
                        List<Result> rawResults = null;
                        try
                        {
                            tempImage = DisposeImages.ElementAtOrDefault(index)?.CopyImage();
                            tempDepth = DisposeDepthes != null && DisposeDepthes.Count > 0
                                ? DisposeDepthes.ElementAtOrDefault(index)?.CopyImage()
                                : null;

                            LogInfo($"模型推理输入准备完成: RunId={runId}, Index={index}, Image={DescribeHObject(tempImage)}, Depth={DescribeHObject(tempDepth)}");
                            if (!DeepLearningSdk.IsValidImage(tempImage))
                            {
                                LogError($"模型推理输入无效: RunId={runId}, Index={index}, Image={DescribeHObject(tempImage)}");
                                DisposeResultGroups(nextResults);
                                return NodeStatus.Error;
                            }

                            if (tempDepth != null && !DeepLearningSdk.IsValidImage(tempDepth))
                            {
                                LogError($"模型推理深度图无效: RunId={runId}, Index={index}, Depth={DescribeHObject(tempDepth)}");
                                DisposeResultGroups(nextResults);
                                return NodeStatus.Error;
                            }

                            int ret = DLSdk.Pipeline(tempImage, tempDepth, out rawResults);
                            LogInfo($"Pipeline完成: RunId={runId}, Index={index}, Ret={ret}, ResultCount={rawResults?.Count ?? -1}");
                            if (ret != 0)
                            {
                                LogError($"模型推理Pipeline失败: RunId={runId}, Index={index}, Ret={ret}, Image={DescribeHObject(tempImage)}, Depth={DescribeHObject(tempDepth)}");
                                System.Windows.MessageBox.Show("模型推理失败。");
                                DisposeResultGroups(nextResults);
                                return NodeStatus.Error;
                            }

                            rawResults ??= new List<Result>();
                            nextResults.Add(CloneResults(rawResults, index));
                        }
                        finally
                        {
                            DisposeResultList(rawResults);
                            tempImage?.Dispose();
                            tempDepth?.Dispose();
                        }
                    }

                    ReplaceResults(nextResults);
                    int resultCount = Results.Sum(group => group?.Count ?? 0);
                    LogInfo($"CloneResults完成: RunId={runId}, ResultCount={resultCount}, GroupCount={Results.Count}");

                    if (!IsFastModeEnabled)
                    {
                        RefreshExecutionPreview();
                    }

                }
                catch (Exception ex)
                {
                    LogError($"模型推理执行异常: RunId={runId}, IsImageList={IsImageList}, ModelType={ModelType}, Image={DescribeHObject(DisposeImage)}, Depth={DescribeHObject(DisposeDepth)}, ImageListCount={DisposeImages?.Count ?? 0}, DepthListCount={DisposeDepthes?.Count ?? 0}, Error={ex}");
                    return NodeStatus.Error;
                }


                // 执行结束后刷新输出参数。
                UpdateOutputParamValues();

                #region 输出同步
                var start = DateTime.Now;

                if (!UpdateParam())
                {
                    LogWarning($"模块_{Serial}更新参数失败");
                }
                LogInfo($"模块_{Serial}更新参数耗时: {DateTime.Now.Subtract(start).TotalMilliseconds}ms");
                #endregion

                return NodeStatus.Success;
                }
            });

            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        private void UpdateOutputParamValues()
        {
            var dataPointValues = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (!dataPointValues.TryGetValue(item.ParamName, out object value))
                {
                    continue;
                }

                object outputValue = CloneOutputValueForBoundary(value);
                DisposePreviousOutputValue(item.Value, value, outputValue);
                item.Value = outputValue;
            }
        }

        /// <summary>
        /// 替换模型推理结果分组，释放上一轮结果中的 HALCON 对象。
        /// </summary>
        private void ReplaceResults(List<List<Result>> nextResults)
        {
            if (!ReferenceEquals(Results, nextResults))
            {
                DisposeResultGroups(Results);
            }

            Results = nextResults ?? new List<List<Result>>();
            RaisePropertyChanged(nameof(Results));
            CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
            RaisePreviewImageNavigationProperties();
        }

        private static object CloneOutputValueForBoundary(object value)
        {
            return value switch
            {
                HObject hObject => CloneHObject(hObject),
                List<List<Result>> resultGroups => CloneResultGroups(resultGroups),
                _ => value
            };
        }

        private static void DisposePreviousOutputValue(object previousValue, object sourceValue, object nextValue)
        {
            if (ReferenceEquals(previousValue, sourceValue) ||
                ReferenceEquals(previousValue, nextValue))
            {
                return;
            }

            DisposeOutputValue(previousValue);
        }

        /// <summary>
        /// 释放输出边界旧值，只处理当前模块拥有的 HALCON 对象。
        /// </summary>
        private static void DisposeOutputValue(object value)
        {
            switch (value)
            {
                case HObject hObject:
                    SafeDisposeHObject(hObject, "OutputParam.Value");
                    break;
                case List<Result> results:
                    DisposeResultList(results);
                    break;
                case List<List<Result>> groups:
                    DisposeResultGroups(groups);
                    break;
            }
        }

        /// <summary>
        /// 释放一组推理结果中的分割区域、附加 HALCON 对象。
        /// </summary>
        private static void DisposeResultList(IEnumerable<Result> results)
        {
            if (results == null)
            {
                return;
            }

            foreach (Result result in results)
            {
                DisposeResult(result);
            }
        }

        /// <summary>
        /// 释放多图推理结果中的所有 HALCON 对象。
        /// </summary>
        private static void DisposeResultGroups(IEnumerable<List<Result>> resultGroups)
        {
            if (resultGroups == null)
            {
                return;
            }

            foreach (List<Result> group in resultGroups)
            {
                DisposeResultList(group);
            }
        }

        /// <summary>
        /// 释放单个推理结果中归当前模块持有的 HALCON 对象。
        /// </summary>
        private static void DisposeResult(Result result)
        {
            if (result == null)
            {
                return;
            }

            SafeDisposeHObject(result.Seg, "Result.Seg");
            result.Seg = new HObject();

            if (result.Others == null)
            {
                return;
            }

            foreach (object value in result.Others.Values)
            {
                if (value is HObject hObject)
                {
                    SafeDisposeHObject(hObject, "Result.Others");
                }
            }
        }

        private static void SafeDisposeHObject(HObject hObject, string label)
        {
            if (hObject == null)
            {
                return;
            }

            try
            {
                hObject.Dispose();
            }
            catch (HOperatorException ex) when (IsDeletedHalconObjectCleanup(ex))
            {
                LogWarning($"忽略已删除HALCON对象释放异常: Label={label}, State={DescribeHObject(hObject)}, Error={ex.Message}");
            }
            catch (Exception ex)
            {
                LogWarning($"HALCON对象释放失败: Label={label}, State={DescribeHObject(hObject)}, Error={ex}");
            }
        }

        private static bool IsDeletedHalconObjectCleanup(HOperatorException ex)
        {
            string message = ex.Message ?? string.Empty;
            return message.Contains("#4051", StringComparison.Ordinal)
                || message.Contains("object has been deleted already", StringComparison.OrdinalIgnoreCase);
        }

        private static HObject CloneHObject(HObject source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                if (!source.IsInitialized())
                {
                    return null;
                }

                try
                {
                    HOperatorSet.CopyImage(source, out HObject imageCopy);
                    return imageCopy;
                }
                catch
                {
                    HOperatorSet.CopyObj(source, out HObject objectCopy, 1, -1);
                    return objectCopy;
                }
            }
            catch
            {
                try
                {
                    return source.IsInitialized() ? source.Clone() : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static HObject CloneHObjectWithLog(HObject source, string fieldName, int? resultIndex, int? groupIndex, Result result)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                if (!source.IsInitialized())
                {
                    LogWarning($"模型推理结果HObject未初始化: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={resultIndex?.ToString() ?? "n/a"}, Field={fieldName}, Source={DescribeResult(result)}");
                    return null;
                }

                HObject cloned = CloneHObject(source);
                if (cloned == null)
                {
                    LogWarning($"模型推理结果HObject克隆失败: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={resultIndex?.ToString() ?? "n/a"}, Field={fieldName}, SourceObject={DescribeHObject(source)}, Source={DescribeResult(result)}");
                }

                return cloned;
            }
            catch (Exception ex)
            {
                LogError($"模型推理结果HObject克隆异常: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={resultIndex?.ToString() ?? "n/a"}, Field={fieldName}, SourceObject={DescribeHObject(source)}, Source={DescribeResult(result)}, Error={ex}");
                return null;
            }
        }

        private static string DescribeResult(Result result)
        {
            if (result == null)
            {
                return "null";
            }

            return $"ClassId={result.ClassId}, ClassName={result.ClassName}, ModelType={result.ModelType}, Confidence={result.Confidence:F4}, Seg={DescribeHObject(result.Seg)}, KptPoints={result.Kpt?.Points?.Count ?? 0}, KptSkeletons={result.Kpt?.Skeletons?.Count ?? 0}, Others={result.Others?.Count ?? 0}";
        }

        private static string DescribeHObject(HObject image)
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
                return $"初始化状态读取失败:{ex.GetType().Name}:{ex.Message}";
            }

            try
            {
                HOperatorSet.CountObj(image, out HTuple count);
                return $"已初始化, Count={count.I}";
            }
            catch (Exception ex)
            {
                return $"对象状态读取失败:{ex.GetType().Name}:{ex.Message}";
            }
        }

        private static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        private static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        private static void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        private static void WriteLog(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string formatted = $"[模型推理] {message}";
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
                    default:
                        Logs.LogInfo(formatted);
                        break;
                }
            }
            catch
            {
                // 日志失败不能影响模型推理主流程。
            }
        }

        private static List<List<Result>> CloneResultGroups(IEnumerable<List<Result>> source)
        {
            if (source == null)
            {
                return new List<List<Result>>();
            }

            List<List<Result>> clonedGroups = new List<List<Result>>();
            int groupIndex = 0;
            foreach (List<Result> group in source)
            {
                try
                {
                    clonedGroups.Add(CloneResults(group, groupIndex));
                }
                catch (Exception ex)
                {
                    LogError($"模型推理结果组克隆失败: GroupIndex={groupIndex}, Count={group?.Count ?? -1}, Error={ex}");
                    throw;
                }

                groupIndex++;
            }

            return clonedGroups;
        }

        private static List<Result> CloneResults(IEnumerable<Result> source, int? groupIndex)
        {
            if (source == null)
            {
                return new List<Result>();
            }

            List<Result> clonedResults = new List<Result>();
            int index = 0;
            foreach (Result item in source)
            {
                if (item == null)
                {
                    LogWarning($"模型推理结果克隆跳过: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={index}, Source=null");
                    index++;
                    continue;
                }

                try
                {
                    clonedResults.Add(CloneResult(item, index, groupIndex));
                }
                catch (Exception ex)
                {
                    LogError($"模型推理结果克隆失败: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={index}, Source={DescribeResult(item)}, Error={ex}");
                    throw;
                }

                index++;
            }

            return clonedResults;
        }

        private static Result CloneResult(Result source, int? index, int? groupIndex)
        {
            Result target = new Result
            {
                Cx = source.Cx,
                Cy = source.Cy,
                Width = source.Width,
                Height = source.Height,
                Angle = source.Angle,
                Confidence = source.Confidence,
                ClassId = source.ClassId,
                ClassName = source.ClassName,
                ModelType = source.ModelType,
                Seg = CloneHObjectWithLog(source.Seg, "Seg", index, groupIndex, source) ?? new HObject()
            };

            target.Kpt = new Keypoints
            {
                Thresh = source.Kpt?.Thresh ?? 0
            };

            if (source.Kpt?.Points != null)
            {
                target.Kpt.Points = source.Kpt.Points
                    .Select(item => new ReeYin_V.Core.DeepLearning.Point
                    {
                        X = item.X,
                        Y = item.Y,
                        Confidence = item.Confidence
                    })
                    .ToList();
            }

            if (source.Kpt?.Skeletons != null)
            {
                target.Kpt.Skeletons = source.Kpt.Skeletons
                    .Select(item => new Skeleton
                    {
                        StartKptId = item.StartKptId,
                        EndKptId = item.EndKptId
                    })
                    .ToList();
            }

            if (source.Others != null)
            {
                target.Others = new Dictionary<string, object>();
                foreach (var item in source.Others)
                {
                    if (item.Value is HObject hObject)
                    {
                        HObject clonedObject = CloneHObjectWithLog(hObject, $"Others[{item.Key}]", index, groupIndex, source);
                        if (clonedObject == null)
                        {
                            LogWarning($"模型推理结果Others克隆失败: GroupIndex={groupIndex?.ToString() ?? "n/a"}, Index={index?.ToString() ?? "n/a"}, Key={item.Key}, Value={DescribeHObject(hObject)}, Source={DescribeResult(source)}");
                            clonedObject = new HObject();
                        }

                        target.Others[item.Key] = clonedObject;
                    }
                    else
                    {
                        target.Others[item.Key] = item.Value;
                    }
                }
            }

            return target;
        }


        private void GetDetectionObj(ReeYin_V.Core.DeepLearning.Result result, bool isObb, out HObject hoBboxObj)
        {
            double Row = result.Cy;
            double Column = result.Cx;
            double Phi;
            if (isObb)
            {
                Phi = -result.Angle;
            }
            else
            {
                Phi = 0;
            }
            double Length1 = result.Width / 2.0;
            double Length2 = result.Height / 2.0;

            HOperatorSet.GenRectangle2(out hoBboxObj, Row, Column, Phi, Length1, Length2);
        }


        private void GetKeypointObj(ReeYin_V.Core.DeepLearning.Result result, out HObject hoKeypoint, out HObject hoSkeleton)
        {
            hoKeypoint = new HObject();

            HTuple RowList = new HTuple();
            HTuple ColList = new HTuple();

            int KptNum = result.Kpt.Points.Count;
            for (int i = 0; i < KptNum; i++)
            {
                ReeYin_V.Core.DeepLearning.Point p = result.Kpt.Points[i];

                if(p.Confidence > result.Kpt.Thresh)
                {
                    RowList = RowList.TupleConcat(p.Y);
                    ColList = ColList.TupleConcat(p.X);
                }
            }

            HOperatorSet.GenCrossContourXld(out hoKeypoint, RowList, ColList, 5, new HTuple(45).TupleRad());

            hoSkeleton = new HObject();
            HObject ResultXLD = new HObject();
            HOperatorSet.GenEmptyObj(out hoSkeleton);
            int connectionNum = result.Kpt.Skeletons.Count;
            for (int i = 0; i < connectionNum; i++)
            {
                ReeYin_V.Core.DeepLearning.Skeleton skeleton = result.Kpt.Skeletons[i];

                ReeYin_V.Core.DeepLearning.Point pStart = result.Kpt.Points[skeleton.StartKptId];
                ReeYin_V.Core.DeepLearning.Point pEnd = result.Kpt.Points[skeleton.EndKptId];

                if(pStart.Confidence > result.Kpt.Thresh && pEnd.Confidence > result.Kpt.Thresh)
                {
                    HOperatorSet.GenContourPolygonXld(out ResultXLD, new HTuple(pStart.Y, pEnd.Y), new HTuple(pStart.X, pEnd.X));
                    HOperatorSet.ConcatObj(hoSkeleton, ResultXLD, out hoSkeleton);
                }
            }
            ResultXLD.Dispose();
        }


        private void GetSegmentationMask(ReeYin_V.Core.DeepLearning.Result result, out HObject hoMask)
        {
            hoMask = result.Seg;
        }

        /// <summary>
        /// 添加本次推理结果覆盖层，所有对象先克隆再交给预览集合持有。
        /// </summary>
        private List<HalconDrawingObject> CreateResultPreviewObjects(List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            List<HalconDrawingObject> drawObjects = new List<HalconDrawingObject>();
            if (IsFastModeEnabled || results == null || results.Count == 0)
            {
                return drawObjects;
            }

            string[] segColors = { "cyan", "magenta", "green", "yellow", "orange", "red" };
            foreach (ReeYin_V.Core.DeepLearning.Result result in results.Where(item => item != null))
            {
                switch (result.ModelType)
                {
                    case eDeepLearningModelType.分类模型:
                        // 分类结果没有几何对象，文字叠加层由 CreateResultPreviewLabels 统一刷新。
                        break;
                    case eDeepLearningModelType.目标检测:
                        AddDetectionPreviewObject(drawObjects, result, false, "blue");
                        break;
                    case eDeepLearningModelType.实例分割:
                        AddDetectionPreviewObject(drawObjects, result, false, "blue");
                        AddSegmentationPreviewObject(drawObjects, result, segColors[Math.Abs(result.ClassId) % segColors.Length]);
                        break;
                    case eDeepLearningModelType.旋转框检测:
                        AddDetectionPreviewObject(drawObjects, result, true, "blue");
                        break;
                    case eDeepLearningModelType.关键点检测:
                        AddDetectionPreviewObject(drawObjects, result, false, "blue");
                        AddKeypointPreviewObjects(drawObjects, result);
                        break;
                    case eDeepLearningModelType.语义分割:
                    case eDeepLearningModelType.异常检测:
                        AddSegmentationPreviewObject(drawObjects, result, "cyan");
                        break;
                }
            }

            return drawObjects;
        }

        private List<string> CreateResultPreviewLabels(List<ReeYin_V.Core.DeepLearning.Result> results)
        {
            List<string> labels = new List<string>();
            if (IsFastModeEnabled || results == null || results.Count == 0)
            {
                return labels;
            }

            int labelIndex = 1;
            foreach (ReeYin_V.Core.DeepLearning.Result result in results.Where(item => item != null))
            {
                if (result.ModelType == eDeepLearningModelType.分类模型)
                {
                    AddClassificationPreviewLabel(labels, result, labelIndex);
                    labelIndex++;
                }
            }

            return labels;
        }

        private static void AddClassificationPreviewLabel(List<string> labels, ReeYin_V.Core.DeepLearning.Result result, int index)
        {
            if (labels == null)
            {
                return;
            }

            string label = BuildClassificationPreviewLabel(result, index);
            if (!string.IsNullOrWhiteSpace(label))
            {
                labels.Add(label);
            }
        }

        private static string BuildClassificationPreviewLabel(ReeYin_V.Core.DeepLearning.Result result, int index)
        {
            if (result == null)
            {
                return string.Empty;
            }

            string className = string.IsNullOrWhiteSpace(result.ClassName)
                ? $"ID {result.ClassId}"
                : result.ClassName.Trim();
            string title = index > 1 ? $"分类{index}" : "分类";
            return $"{title}: {className}  置信度:{result.Confidence:P2}";
        }

        private void AddDetectionPreviewObject(List<HalconDrawingObject> drawObjects, ReeYin_V.Core.DeepLearning.Result result, bool isObb, string color)
        {
            HObject bbox = null;
            try
            {
                GetDetectionObj(result, isObb, out bbox);
                AddPreviewObject(drawObjects, color, bbox);
            }
            finally
            {
                SafeDisposeHObject(bbox, "Preview.Bbox.Temp");
            }
        }

        private void AddSegmentationPreviewObject(List<HalconDrawingObject> drawObjects, ReeYin_V.Core.DeepLearning.Result result, string color)
        {
            GetSegmentationMask(result, out HObject mask);
            AddPreviewObject(drawObjects, color, mask);
        }

        private void AddKeypointPreviewObjects(List<HalconDrawingObject> drawObjects, ReeYin_V.Core.DeepLearning.Result result)
        {
            HObject keypoint = null;
            HObject skeleton = null;
            try
            {
                GetKeypointObj(result, out keypoint, out skeleton);
                AddPreviewObject(drawObjects, "yellow", keypoint);
                AddPreviewObject(drawObjects, "cyan", skeleton);
            }
            finally
            {
                SafeDisposeHObject(keypoint, "Preview.Keypoint.Temp");
                SafeDisposeHObject(skeleton, "Preview.Skeleton.Temp");
            }
        }

        private void AddPreviewObject(List<HalconDrawingObject> drawObjects, string color, HObject source, bool isFillDisplay = false)
        {
            if (IsFastModeEnabled || drawObjects == null)
            {
                return;
            }

            HObject displayObject = CloneHObject(source);
            if (displayObject == null)
            {
                return;
            }

            try
            {
                drawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = displayObject,
                    Color = color,
                    IsFillDisplay = isFillDisplay
                });
                displayObject = null;
            }
            finally
            {
                SafeDisposeHObject(displayObject, "Preview.DrawObject.Unused");
            }
        }

        /// <summary>
        /// 清理当前预览覆盖层。
        /// </summary>
        private void ClearResultRois()
        {
            RunOnPreviewDispatcher(() =>
            {
                ClearPreviewDrawObjects();
                ClearPreviewResultLabels();
            });
        }

        /// <summary>
        /// 清空运行时预览图像和结果覆盖层。
        /// </summary>
        private void ClearRuntimePreviewDisplay()
        {
            RunOnPreviewDispatcher(() =>
            {
                ClearPreviewDrawObjects();
                ClearPreviewResultLabels();
                SetPreviewImageObject(null);
            });
        }

        private void ClearPreviewDrawObjects()
        {
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects.ToList())
            {
                SafeDisposeHObject(drawObject?.Hobject, "Preview.DrawObject");
            }

            PreviewDrawObjects.Clear();
        }

        private void ClearPreviewResultLabels()
        {
            PreviewResultLabels.Clear();
        }

        private static void DisposePreviewDrawObjects(List<HalconDrawingObject> drawObjects)
        {
            if (drawObjects == null)
            {
                return;
            }

            foreach (HalconDrawingObject drawObject in drawObjects)
            {
                SafeDisposeHObject(drawObject?.Hobject, "Preview.DrawObject.Unused");
            }

            drawObjects.Clear();
        }

        private void SetPreviewImageObject(HObject image)
        {
            HObject oldImage = _previewImageObject;
            PreviewImageObject = image;
            SafeDisposeHObject(oldImage, "Preview.Image");
        }
        #endregion

    }

    public static class HImageExtensions
    {

        public static void Dispose(this IEnumerable<HImage> images)
        {
            if (images == null)
                return;

            foreach (var img in images)
            {
                if (img != null)
                {
                    try
                    {
                        img.Dispose();
                    }
                    catch
                    {
                        // HALCON 对象已释放或重复释放时直接忽略。
                    }
                }
            }
        }
    }
}

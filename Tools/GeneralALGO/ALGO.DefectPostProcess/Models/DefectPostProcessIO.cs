using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ALGO.DefectPostProcess.Models
{
    public partial class DefectPostProcessModel
    {
        #region 输入链接与参数同步
        #region 输入参数定义
        [JsonIgnore]
        public HImage DisposeImage { get; set; }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _inputLinkParams = new ObservableCollection<TransmitParam>();

        [JsonIgnore]
        public ObservableCollection<TransmitParam> InputLinkParams
        {
            get { return _inputLinkParams; }
            set { SetProperty(ref _inputLinkParams, value); }
        }

        private Guid _inputImageGuid;
        public Guid InputImageGuid
        {
            get { return _inputImageGuid; }
            set { SetProperty(ref _inputImageGuid, value); }
        }

        private string _inputImageName;
        public string InputImageName
        {
            get { return _inputImageName; }
            set { SetProperty(ref _inputImageName, value); }
        }

        private Guid _inputResultsGuid;
        public Guid InputResultsGuid
        {
            get { return _inputResultsGuid; }
            set { SetProperty(ref _inputResultsGuid, value); }
        }

        private string _inputResultsName;
        public string InputResultsName
        {
            get { return _inputResultsName; }
            set { SetProperty(ref _inputResultsName, value); }
        }

        private Guid _inputPixelEquivalentGuid;
        public Guid InputPixelEquivalentGuid
        {
            get { return _inputPixelEquivalentGuid; }
            set { SetProperty(ref _inputPixelEquivalentGuid, value); }
        }

        private string _inputPixelEquivalentName;
        public string InputPixelEquivalentName
        {
            get { return _inputPixelEquivalentName; }
            set
            {
                if (SetProperty(ref _inputPixelEquivalentName, value))
                {
                    RaisePropertyChanged(nameof(InputPixelEquivalentDisplayText));
                }
            }
        }

        private Guid _inputPixelEquivalentXGuid;
        public Guid InputPixelEquivalentXGuid
        {
            get { return _inputPixelEquivalentXGuid; }
            set { SetProperty(ref _inputPixelEquivalentXGuid, value); }
        }

        private string _inputPixelEquivalentXName;
        public string InputPixelEquivalentXName
        {
            get { return _inputPixelEquivalentXName; }
            set
            {
                if (SetProperty(ref _inputPixelEquivalentXName, value))
                {
                    RaisePropertyChanged(nameof(InputPixelEquivalentDisplayText));
                }
            }
        }

        private Guid _inputPixelEquivalentYGuid;
        public Guid InputPixelEquivalentYGuid
        {
            get { return _inputPixelEquivalentYGuid; }
            set { SetProperty(ref _inputPixelEquivalentYGuid, value); }
        }

        private string _inputPixelEquivalentYName;
        public string InputPixelEquivalentYName
        {
            get { return _inputPixelEquivalentYName; }
            set
            {
                if (SetProperty(ref _inputPixelEquivalentYName, value))
                {
                    RaisePropertyChanged(nameof(InputPixelEquivalentDisplayText));
                }
            }
        }

        private Guid _inputEdgeCalibrationXGuid;
        [JsonProperty("InputReferenceX2Guid")]
        public Guid InputEdgeCalibrationXGuid
        {
            get { return _inputEdgeCalibrationXGuid; }
            set { SetProperty(ref _inputEdgeCalibrationXGuid, value); }
        }

        private string _inputEdgeCalibrationXName;
        [JsonProperty("InputReferenceX2Name")]
        public string InputEdgeCalibrationXName
        {
            get { return _inputEdgeCalibrationXName; }
            set
            {
                if (SetProperty(ref _inputEdgeCalibrationXName, value))
                {
                    RaisePropertyChanged(nameof(InputEdgeCalibrationXDisplayText));
                }
            }
        }

        private DefectPostProcessInputBinding _inputImageBinding = new DefectPostProcessInputBinding();
        public DefectPostProcessInputBinding InputImageBinding
        {
            get { return _inputImageBinding; }
            set { SetProperty(ref _inputImageBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private DefectPostProcessInputBinding _inputResultsBinding = new DefectPostProcessInputBinding();
        public DefectPostProcessInputBinding InputResultsBinding
        {
            get { return _inputResultsBinding; }
            set { SetProperty(ref _inputResultsBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private DefectPostProcessInputBinding _inputPixelEquivalentBinding = new DefectPostProcessInputBinding();
        public DefectPostProcessInputBinding InputPixelEquivalentBinding
        {
            get { return _inputPixelEquivalentBinding; }
            set { SetProperty(ref _inputPixelEquivalentBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private DefectPostProcessInputBinding _inputPixelEquivalentXBinding = new DefectPostProcessInputBinding();
        public DefectPostProcessInputBinding InputPixelEquivalentXBinding
        {
            get { return _inputPixelEquivalentXBinding; }
            set { SetProperty(ref _inputPixelEquivalentXBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private DefectPostProcessInputBinding _inputPixelEquivalentYBinding = new DefectPostProcessInputBinding();
        public DefectPostProcessInputBinding InputPixelEquivalentYBinding
        {
            get { return _inputPixelEquivalentYBinding; }
            set { SetProperty(ref _inputPixelEquivalentYBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private DefectPostProcessInputBinding _inputEdgeCalibrationXBinding = new DefectPostProcessInputBinding();
        [JsonProperty("InputReferenceX2Binding")]
        public DefectPostProcessInputBinding InputEdgeCalibrationXBinding
        {
            get { return _inputEdgeCalibrationXBinding; }
            set { SetProperty(ref _inputEdgeCalibrationXBinding, value ?? new DefectPostProcessInputBinding()); }
        }

        private string _calibrationFilePath = string.Empty;
        public string CalibrationFilePath
        {
            get { return _calibrationFilePath; }
            set
            {
                if (SetProperty(ref _calibrationFilePath, NormalizeCalibrationFilePath(value)))
                {
                    MarkSchemeDirty();
                }
            }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set { SetInputImage(value); }
        }

        [JsonIgnore]
        private TransmitParam _inputResults = new TransmitParam();
        public TransmitParam InputResults
        {
            get { RefreshSourceResults(); return _inputResults; }
            set { SetInputResults(value); }
        }

        [JsonIgnore]
        private TransmitParam _inputPixelEquivalent = new TransmitParam();
        public TransmitParam InputPixelEquivalent
        {
            get { return _inputPixelEquivalent; }
            set { SetInputPixelEquivalent(value); }
        }

        [JsonIgnore]
        private TransmitParam _inputPixelEquivalentX = new TransmitParam();
        public TransmitParam InputPixelEquivalentX
        {
            get { return _inputPixelEquivalentX; }
            set { SetInputPixelEquivalentX(value); }
        }

        [JsonIgnore]
        private TransmitParam _inputPixelEquivalentY = new TransmitParam();
        public TransmitParam InputPixelEquivalentY
        {
            get { return _inputPixelEquivalentY; }
            set { SetInputPixelEquivalentY(value); }
        }

        [JsonIgnore]
        private TransmitParam _inputEdgeCalibrationX = new TransmitParam { Type = DataType.Double, Value = DefaultEdgeCalibrationX };
        [JsonProperty("InputReferenceX2")]
        public TransmitParam InputEdgeCalibrationX
        {
            get { return _inputEdgeCalibrationX; }
            set { SetInputEdgeCalibrationX(value); }
        }

        [JsonIgnore]
        public List<Result> SourceResults { get; private set; } = new List<Result>();

        [JsonIgnore]
        public List<List<Result>> SourceResultsByImage { get; private set; } = new List<List<Result>>();

        [JsonIgnore]
        public double PixelEquivalentX { get; private set; }

        [JsonIgnore]
        public double PixelEquivalentY { get; private set; }

        [JsonIgnore]
        private double _edgeCalibrationX = DefaultEdgeCalibrationX;
        [JsonIgnore]
        public double EdgeCalibrationX
        {
            get { return _edgeCalibrationX; }
            private set
            {
                if (SetProperty(ref _edgeCalibrationX, value))
                {
                    RaisePropertyChanged(nameof(InputEdgeCalibrationXDisplayText));
                }
            }
        }

        [JsonIgnore]
        public string InputEdgeCalibrationXDisplayText
        {
            get
            {
                string valueText = EdgeCalibrationX.ToString("0.###");
                return string.IsNullOrWhiteSpace(InputEdgeCalibrationXName)
                    ? valueText
                    : $"{InputEdgeCalibrationXName} ({valueText})";
            }
        }

        [JsonIgnore]
        public string InputPixelEquivalentDisplayText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(InputPixelEquivalentName))
                {
                    return InputPixelEquivalentName;
                }

                if (!string.IsNullOrWhiteSpace(InputPixelEquivalentXName)
                    && !string.IsNullOrWhiteSpace(InputPixelEquivalentYName))
                {
                    return $"{InputPixelEquivalentXName} / {InputPixelEquivalentYName}";
                }

                return !string.IsNullOrWhiteSpace(InputPixelEquivalentXName)
                    ? InputPixelEquivalentXName
                    : InputPixelEquivalentYName ?? string.Empty;
            }
        }
        #endregion

        #region 输入加载与同步
        /// <summary>
        /// 加载输入链接、标定文件和缺陷结果，供执行和界面刷新共用。
        /// </summary>
        public override bool LoadKeyParam()
        {
            return LoadKeyParam(flushDeferredStateRefresh: true);
        }

        /// <summary>
        /// 加载输入链接、标定文件和缺陷结果，并按需推迟界面刷新。
        /// </summary>
        private bool LoadKeyParam(bool flushDeferredStateRefresh)
        {
            BeginDeferredStateRefresh();
            object previousResultsValue = _inputResults?.Value;
            try
            {
                EnsurePreviewWindowControl();
                RestorePersistedInputBindings();

                LoadInputImageValue();
                UpdateInputImage();

                ReloadCalibrationFileContext();
                UpdateCalibrationScaleInputs();

                LoadEdgeCalibrationXInputValue();
                UpdateEdgeCalibrationXInputs();

                LoadInputResultsValue(previousResultsValue);
                UpdateInputResults();

                return true;
            }
            catch (Exception ex)
            {
                LogError($"输入参数加载失败: {ex.Message}");
                return false;
            }
            finally
            {
                EndDeferredStateRefresh(flushDeferredStateRefresh);
            }
        }

        /// <summary>
        /// 刷新当前模块输入参数快照。
        /// </summary>
        public void RefreshInputParamSnapshot()
        {
            Dictionary<string, object> moduleInputs = moduleInputParam?.TransmitParams ?? new Dictionary<string, object>();
            List<TransmitParam> snapshots = moduleInputs.Values
                .OfType<TransmitParam>()
                .Select(CloneInputParamSnapshot)
                .ToList();
            List<TransmitParam> linkSnapshots = moduleInputs.Values
                .OfType<TransmitParam>()
                .Select(CloneInputLinkParam)
                .ToList();

            RunOnDispatcher(() =>
            {
                if (_ownsInputParamSnapshots)
                {
                    _ownsInputParamSnapshots = false;
                }

                InputParams = new ObservableCollection<TransmitParam>(snapshots);
                InputLinkParams = new ObservableCollection<TransmitParam>(linkSnapshots);
                OutputParamNames = OutputParamResource.Select(item => item.Key).ToList();
                moduleOutputParam.TransmitParams ??= new Dictionary<string, object>();
                _ownsInputParamSnapshots = true;
            });
        }

        /// <summary>
        /// 清空当前模块输入参数快照。
        /// </summary>
        public void ClearInputParamSnapshot()
        {
            RunOnDispatcher(() =>
            {
                if (_ownsInputParamSnapshots)
                {
                    _ownsInputParamSnapshots = false;
                }

                InputParams = new ObservableCollection<TransmitParam>();
                InputLinkParams = new ObservableCollection<TransmitParam>();
            });
        }

        /// <summary>
        /// 重新加载标定文件并刷新坐标换算缓存。
        /// </summary>
        public void RefreshCalibrationFile()
        {
            ReloadCalibrationFileContext();
            UpdateCalibrationScaleInputs();
        }

        private void EnsurePreviewWindowControl()
        {
            base.LoadKeyParam();
            ModuleName = Serial.ToString("D3");
        }

        private void RestorePersistedInputBindings()
        {
            ClearCachedInputValues();
            RefreshInputParamSnapshot();

            RestoreInputLinkMetadata(_inputImage, InputImageBinding, InputImageGuid, InputImageName, DataType.HObject);
            RestoreInputLinkMetadata(_inputResults, InputResultsBinding, InputResultsGuid, InputResultsName, DataType.List);
            RestoreInputLinkMetadata(_inputPixelEquivalent, InputPixelEquivalentBinding, InputPixelEquivalentGuid, InputPixelEquivalentName, DataType.Array);
            RestoreInputLinkMetadata(_inputPixelEquivalentX, InputPixelEquivalentXBinding, InputPixelEquivalentXGuid, InputPixelEquivalentXName, DataType.Double);
            RestoreInputLinkMetadata(_inputPixelEquivalentY, InputPixelEquivalentYBinding, InputPixelEquivalentYGuid, InputPixelEquivalentYName, DataType.Double);
            RestoreInputLinkMetadata(_inputEdgeCalibrationX, InputEdgeCalibrationXBinding, InputEdgeCalibrationXGuid, InputEdgeCalibrationXName, DataType.Double);

            NormalizeFlexibleInputLinkTypes();
        }

        private void LoadInputImageValue()
        {
            if (_inputImage == null)
            {
                return;
            }

            try
            {
                HObject newImage = ResolveInputImageValue(_inputImage) as HObject;
                if (!IsReadableImageSafely(newImage))
                {
                    List<HImage> oldInputImages = _inputImages;
                    IEnumerable<HImage> oldValueImages = _inputImage.Value as IEnumerable<HImage>;
                    bool oldValueSharesRuntimeImages = ReferenceEquals(oldValueImages, oldInputImages);
                    _inputImages = new List<HImage>();
                    _inputImage.Value = null;
                    DisposeImageList(oldInputImages);
                    if (!oldValueSharesRuntimeImages)
                    {
                        DisposeImageList(oldValueImages);
                    }

                    return;
                }

                List<HImage> newInputImages = ExtractInputImages(newImage);
                List<HImage> oldImages = _inputImages;
                IEnumerable<HImage> previousValueImages = _inputImage.Value as IEnumerable<HImage>;
                bool previousValueSharesRuntimeImages = ReferenceEquals(previousValueImages, oldImages);
                _inputImages = newInputImages;
                _inputImage.Value = newInputImages;
                DisposeImageList(oldImages);
                if (!previousValueSharesRuntimeImages)
                {
                    DisposeImageList(previousValueImages);
                }

                CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
                RaisePreviewImageNavigationProperties();
            }
            catch (Exception ex)
            {
                LogInputImageLoadFailure("load", _inputImage, _inputImage?.Value, ex);
                throw;
            }
        }

        /// <summary>
        /// 按缓存、实时链接和输入快照顺序解析输入图像值。
        /// </summary>
        private object ResolveInputImageValue(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            TransmitParam cachedParam = GetCachedOutputParam(param);
            object cachedValue = cachedParam == null ? null : CloneInputValue(cachedParam.Value);
            LogInputImageCandidate("cache", cachedParam ?? param, cachedValue);
            if (IsReadableImageSafely(cachedValue as HObject))
            {
                return cachedValue;
            }

            object liveValue = GetLiveLinkedValue(param);
            LogInputImageCandidate("live", param, liveValue);
            if (IsReadableImageSafely(liveValue as HObject))
            {
                return liveValue;
            }

            object snapshotValue = GetTransmitParam(InputParams, param, false);
            LogInputImageCandidate("snapshot", param, snapshotValue);
            if (IsReadableImageSafely(snapshotValue as HObject))
            {
                return snapshotValue;
            }

            return null;
        }

        private void LoadEdgeCalibrationXInputValue()
        {
            if (_inputEdgeCalibrationX == null)
            {
                return;
            }

            _inputEdgeCalibrationX.Value = GetTransmitParam(InputParams, _inputEdgeCalibrationX, false)
                ?? ResolvePreferredEdgeCalibrationXInputValue();
        }

        private void LoadInputResultsValue(object fallbackValue = null)
        {
            if (_inputResults == null)
            {
                return;
            }

            object newResults = GetTransmitParam(InputParams, _inputResults, false);
            if (newResults == null)
            {
                object preferredResults = ResolvePreferredResultsInputValue();
                if (HasSelectableResultPayload(preferredResults))
                {
                    newResults = preferredResults;
                }
            }

            if (newResults == null && ShouldKeepResultsFallback(fallbackValue))
            {
                _inputResults.Value = ExtractLinkedResultGroups(fallbackValue);
                return;
            }

            _inputResults.Value = ExtractLinkedResultGroups(newResults);
        }
        #endregion

        #region 输入图像预览
        /// <summary>
        /// 刷新输入图像列表和当前预览帧缓存。
        /// </summary>
        private void RefreshInputImagePreview()
        {
            List<HImage> oldInputImages = _inputImages;
            IEnumerable<HImage> oldValueImages = _inputImage?.Value as IEnumerable<HImage>;
            bool oldValueSharesRuntimeImages = ReferenceEquals(oldValueImages, oldInputImages);
            List<HImage> newInputImages = null;
            HImage newPreviewImage = null;
            bool inputImagesAssigned = false;

            try
            {
                newInputImages = ExtractInputImages(_inputImage?.Value);
                newPreviewImage = CreatePreviewImage(newInputImages, CurrentPreviewImageIndex);
                _inputImages = newInputImages;
                if (_inputImage != null)
                {
                    _inputImage.Value = newInputImages;
                }

                inputImagesAssigned = true;
                newInputImages = null;
                CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
                ReplaceCurrentPreviewImage(newPreviewImage);
                newPreviewImage = null;

                // 只更新当前帧缓存，真正显示由 RequestPreviewRefresh 统一刷新图像和结果层。
            }
            catch (Exception ex)
            {
                LogWarning($"加载输入图像预览失败: {ex.Message}");
                DisposeImageList(newInputImages);
                newInputImages = null;
                if (inputImagesAssigned && !ReferenceEquals(_inputImages, oldInputImages))
                {
                    DisposeImageList(_inputImages);
                }

                ClearCurrentPreviewImage();
                _inputImages = new List<HImage>();
                if (_inputImage != null)
                {
                    _inputImage.Value = null;
                }
            }
            finally
            {
                HalconImageOwnership.DisposeOwned(newPreviewImage);
                DisposeImageList(newInputImages);
                DisposeImageList(oldInputImages);
                if (!oldValueSharesRuntimeImages)
                {
                    DisposeImageList(oldValueImages);
                }
            }
        }

        /// <summary>
        /// 从已接管的输入图像列表复制当前预览帧，不重新拆分整组图像。
        /// </summary>
        private void RefreshCurrentPreviewImageFromRuntimeImages()
        {
            HImage newPreviewImage = null;
            try
            {
                newPreviewImage = CreatePreviewImage(_inputImages, CurrentPreviewImageIndex);
                CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
                ReplaceCurrentPreviewImage(newPreviewImage);
                newPreviewImage = null;
            }
            finally
            {
                HalconImageOwnership.DisposeOwned(newPreviewImage);
            }
        }

        private void LogInputImageCandidate(string source, TransmitParam param, object value)
        {
            LogTrace(
                $"输入图像候选: Source={source}, Param={DescribeTransmitParamForDiagnostics(param)}, " +
                $"Value={DescribeInputValueForDiagnostics(value)}");
        }

        private void LogInputImageLoadFailure(
            string stage,
            TransmitParam param,
            object value,
            Exception ex,
            int imageIndex = -1)
        {
            string error = ex == null ? "<none>" : ex.ToString();
            LogWarning(
                $"输入图像加载诊断: Stage={stage}, ImageIndex={imageIndex}, " +
                $"Param={DescribeTransmitParamForDiagnostics(param)}, " +
                $"Value={DescribeInputValueForDiagnostics(value)}, Error={error}");
        }

        private static string DescribeTransmitParamForDiagnostics(TransmitParam param)
        {
            if (param == null)
            {
                return "<null>";
            }

            return
                $"Guid={param.Guid}, LinkGuid={param.LinkGuid}, Serial={param.Serial}, " +
                $"Name={param.Name ?? "<null>"}, ParamName={param.ParamName ?? "<null>"}, " +
                $"Type={param.Type}, Source={param.Resourece}, ResourcePath={param.ResourcePath ?? "<null>"}, " +
                $"ValueType={param.Value?.GetType().FullName ?? "null"}";
        }

        private static string DescribeInputValueForDiagnostics(object value)
        {
            if (value == null)
            {
                return "ValueType=null";
            }

            if (value is HObject hObject)
            {
                return $"ValueType={value.GetType().FullName}, {DescribeHObjectForDiagnostics(hObject)}";
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                int count = 0;
                foreach (object _ in enumerable)
                {
                    count++;
                }

                return $"ValueType={value.GetType().FullName}, Count={count}";
            }

            return $"ValueType={value.GetType().FullName}, Value={value}";
        }

        private static string DescribeHObjectForDiagnostics(HObject hObject)
        {
            if (hObject == null)
            {
                return "HObject=null";
            }

            if (!IsInitializedSafely(hObject))
            {
                return "HObject=not-initialized";
            }

            int count = 0;
            string size = "unknown";
            HObject selected = null;
            try
            {
                TryGetInputObjectCount(hObject, out count);
                HOperatorSet.SelectObj(hObject, out selected, 1);
                HOperatorSet.GetImageSize(selected, out HTuple width, out HTuple height);
                size = $"{width}x{height}";
            }
            catch
            {
                size = "unreadable";
            }
            finally
            {
                SafeDisposeHObject(selected);
            }

            return $"HObject=initialized, Count={count}, Size={size}";
        }
        private static List<HImage> ExtractInputImages(object inputValue)
        {
            if (inputValue is HObject hObject && IsInitializedSafely(hObject) && TryGetInputObjectCount(hObject, out int objectCount))
            {
                List<HImage> images = new List<HImage>();
                for (int i = 1; i <= objectCount; i++)
                {
                    if (TryCopySelectedInputImage(hObject, i, out HImage image))
                    {
                        images.Add(image);
                    }
                }

                return images;
            }

            if (inputValue is IEnumerable<HImage> imageEnumerable)
            {
                List<HImage> images = new List<HImage>();
                foreach (HImage image in imageEnumerable)
                {
                    if (TryCopyInputImage(image, out HImage imageCopy))
                    {
                        images.Add(imageCopy);
                    }
                }

                return images;
            }

            return new List<HImage>();
        }

        /// <summary>
        /// 按当前索引从输入 HObject 或图像列表复制预览图像。
        /// </summary>
        private static HImage CreatePreviewImage(object inputValue, int imageIndex)
        {
            if (inputValue is HObject hObject && IsInitializedSafely(hObject) && TryGetInputObjectCount(hObject, out int objectCount))
            {
                int normalizedIndex = Math.Clamp(imageIndex, 0, objectCount - 1);
                return TryCopySelectedInputImage(hObject, normalizedIndex + 1, out HImage image)
                    ? image
                    : null;
            }

            if (inputValue is IEnumerable<HImage> imageEnumerable)
            {
                List<HImage> images = imageEnumerable
                    .Where(item => item != null)
                    .ToList();
                int normalizedIndex = images.Count <= 0 ? 0 : Math.Clamp(imageIndex, 0, images.Count - 1);
                return images.Count > 0 && TryCopyInputImage(images[normalizedIndex], out HImage imageCopy)
                    ? imageCopy
                    : null;
            }

            return null;
        }

        private static bool TryGetInputObjectCount(HObject hObject, out int count)
        {
            count = 0;
            try
            {
                count = hObject.CountObj();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCopySelectedInputImage(HObject hObject, int index, out HImage imageCopy)
        {
            imageCopy = null;
            try
            {
                return HalconImageOwnership.TryCopyBorrowed(hObject, index, out imageCopy);
            }
            catch
            {
                HalconImageOwnership.DisposeOwned(imageCopy);
                imageCopy = null;
                return false;
            }
        }

        private static bool TryCopyInputImage(HImage image, out HImage imageCopy)
        {
            imageCopy = null;

            try
            {
                return HalconImageOwnership.TryCopyBorrowed(image, out imageCopy);
            }
            catch
            {
                HalconImageOwnership.DisposeOwned(imageCopy);
                imageCopy = null;
                return false;
            }
        }

        /// <summary>
        /// 刷新源缺陷结果，并按当前方案类别表同步显示名称。
        /// </summary>
        private void RefreshSourceResults()
        {
            object inputValue = _inputResults?.Value;
            SourceResultsByImage = ExtractLinkedResultGroups(inputValue);
            SourceResults = SourceResultsByImage.SelectMany(group => group).ToList();
            ApplyConfiguredClassNames(SourceResults);
            if (_inputResults != null)
            {
                _inputResults.Value = SourceResultsByImage;
            }

            UpdateResultPhysicalCoordinates(SourceResults);
            if (IsDebug)
            {
                LogInputResultsSummary(inputValue, SourceResults);
            }
            BuildDefectItems();

            RaisePreviewImageNavigationProperties();
        }

        private void SetInputImage(TransmitParam value)
        {
            SetFlexibleInputParam(ref _inputImage, value, InputImageBinding, binding =>
            {
                InputImageBinding = binding;
                InputImageGuid = binding.Guid;
                InputImageName = binding.Name ?? string.Empty;
            });
            RaisePropertyChanged(nameof(InputImage));
        }

        private void SetInputResults(TransmitParam value)
        {
            SetFlexibleInputParam(ref _inputResults, value, InputResultsBinding, binding =>
            {
                InputResultsBinding = binding;
                InputResultsGuid = binding.Guid;
                InputResultsName = binding.Name ?? string.Empty;
            });
            RaisePropertyChanged(nameof(InputResults));
        }

        private void SetInputPixelEquivalentX(TransmitParam value)
        {
            SetInputParam(ref _inputPixelEquivalentX, value, InputPixelEquivalentXBinding, binding =>
            {
                InputPixelEquivalentXBinding = binding;
                InputPixelEquivalentXGuid = binding.Guid;
                InputPixelEquivalentXName = binding.Name ?? string.Empty;
            });
            UpdateCalibrationScaleInputs();
        }

        private void SetInputPixelEquivalentY(TransmitParam value)
        {
            SetInputParam(ref _inputPixelEquivalentY, value, InputPixelEquivalentYBinding, binding =>
            {
                InputPixelEquivalentYBinding = binding;
                InputPixelEquivalentYGuid = binding.Guid;
                InputPixelEquivalentYName = binding.Name ?? string.Empty;
            });
            UpdateCalibrationScaleInputs();
        }

        private void SetInputPixelEquivalent(TransmitParam value)
        {
            SetFlexibleInputParam(ref _inputPixelEquivalent, value, InputPixelEquivalentBinding, binding =>
            {
                InputPixelEquivalentBinding = binding;
                InputPixelEquivalentGuid = binding.Guid;
                InputPixelEquivalentName = binding.Name ?? string.Empty;
            });
            UpdateCalibrationScaleInputs();
        }

        private void SetInputEdgeCalibrationX(TransmitParam value)
        {
            SetInputParam(ref _inputEdgeCalibrationX, value, InputEdgeCalibrationXBinding, binding =>
            {
                InputEdgeCalibrationXBinding = binding;
                InputEdgeCalibrationXGuid = binding.Guid;
                InputEdgeCalibrationXName = binding.Name ?? string.Empty;
            },
            overrideBindingType: true,
            overriddenType: DataType.Double,
            defaultFactory: () => new TransmitParam { Type = DataType.Double, Value = DefaultEdgeCalibrationX });
            UpdateEdgeCalibrationXInputs();
        }

        private void SetFlexibleInputParam(
            ref TransmitParam field,
            TransmitParam value,
            DefectPostProcessInputBinding currentBinding,
            Action<DefectPostProcessInputBinding> applyBinding)
        {
            SetInputParam(
                ref field,
                value,
                currentBinding,
                applyBinding,
                overrideBindingType: true,
                overriddenType: DataType.None);
        }

        /// <summary>
        /// 设置输入参数并同步持久化绑定信息。
        /// </summary>
        private void SetInputParam(
            ref TransmitParam field,
            TransmitParam value,
            DefectPostProcessInputBinding currentBinding,
            Action<DefectPostProcessInputBinding> applyBinding,
            bool overrideBindingType = false,
            DataType overriddenType = DataType.None,
            Func<TransmitParam> defaultFactory = null)
        {
            field = value ?? defaultFactory?.Invoke() ?? new TransmitParam();
            if (overrideBindingType)
            {
                field.Type = overriddenType;
            }

            DefectPostProcessInputBinding binding = CreateInputBindingSnapshot(field, overrideBindingType, overriddenType);
            bool bindingChanged = !AreEquivalentInputBindings(currentBinding, binding);

            applyBinding?.Invoke(binding);
            if (bindingChanged)
            {
                MarkSchemeDirty();
            }

            RaisePropertyChanged();
        }

        /// <summary>
        /// 刷新输入图像预览并请求界面同步显示。
        /// </summary>
        private void UpdateInputImage()
        {
            RefreshCurrentPreviewImageFromRuntimeImages();
            RequestPreviewRefresh();
        }

        /// <summary>
        /// 刷新输入结果、类别定义和规则编辑状态。
        /// </summary>
        private void UpdateInputResults()
        {
            RefreshSourceResults();
            RequestEditingStateRefresh();
        }

        /// <summary>
        /// 同步标定文件 interval 到内部物理尺寸换算缓存。
        /// </summary>
        private void UpdateCalibrationScaleInputs()
        {
            if (!TryGetCalibrationIntervalPixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY))
            {
                pixelEquivalentX = 0d;
                pixelEquivalentY = 0d;
            }

            PixelEquivalentX = pixelEquivalentX;
            PixelEquivalentY = pixelEquivalentY;

            RefreshFeatureThresholdUnits();
            UpdateResultPhysicalCoordinates(SourceResults);
            RequestEditingStateRefresh();
        }

        /// <summary>
        /// 刷新边缘基准 X 输入并重算物理坐标。
        /// </summary>
        private void UpdateEdgeCalibrationXInputs()
        {
            EdgeCalibrationX = GetEdgeCalibrationXValue(_inputEdgeCalibrationX?.Value);

            UpdateResultPhysicalCoordinates(SourceResults);
            RequestEditingStateRefresh();
        }

        /// <summary>
        /// 克隆输入参数快照，保留当前值用于本模块读取。
        /// </summary>
        private static TransmitParam CloneInputParamSnapshot(TransmitParam source)
        {
            if (source == null)
            {
                return new TransmitParam { Resourece = ResoureceType.Inupt };
            }

            return new TransmitParam
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode,
                Guid = source.Guid,
                Resourece = ResoureceType.Inupt,
                Name = source.Name,
                ParamName = source.ParamName,
                Type = ResolveParamType(source.Type, source.Value, source.ResourcePath),
                Value = CloneInputValue(source.Value),
                Describe = source.Describe,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath
            };
        }

        /// <summary>
        /// 克隆输入链接参数，忽略值对象避免持有上游资源。
        /// </summary>
        private static TransmitParam CloneInputLinkParam(TransmitParam source)
        {
            if (source == null)
            {
                return new TransmitParam { Resourece = ResoureceType.Inupt };
            }

            return new TransmitParam
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode,
                Guid = source.Guid,
                Resourece = ResoureceType.Inupt,
                Name = source.Name,
                ParamName = source.ParamName,
                Type = ResolveParamType(source.Type, source.Value, source.ResourcePath),
                Value = null,
                Describe = source.Describe,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath
            };
        }

        /// <summary>
        /// 根据输入参数生成可持久化的链接绑定快照。
        /// </summary>
        private static DefectPostProcessInputBinding CreateInputBindingSnapshot(
            TransmitParam source,
            bool overrideBindingType = false,
            DataType overriddenType = DataType.None)
        {
            if (source == null)
            {
                return new DefectPostProcessInputBinding();
            }

            return new DefectPostProcessInputBinding
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode ?? string.Empty,
                Guid = source.Guid,
                Resourece = source.Resourece == ResoureceType.None ? ResoureceType.Inupt : source.Resourece,
                Name = source.Name ?? string.Empty,
                ParamName = source.ParamName ?? string.Empty,
                Type = overrideBindingType
                    ? overriddenType
                    : ResolveParamType(source.Type, source.Value, source.ResourcePath),
                Describe = source.Describe ?? string.Empty,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath ?? string.Empty
            };
        }

        /// <summary>
        /// 克隆输入链接绑定配置。
        /// </summary>
        private static DefectPostProcessInputBinding CloneInputBinding(DefectPostProcessInputBinding source)
        {
            if (source == null)
            {
                return new DefectPostProcessInputBinding();
            }

            return new DefectPostProcessInputBinding
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode ?? string.Empty,
                Guid = source.Guid,
                Resourece = source.Resourece,
                Name = source.Name ?? string.Empty,
                ParamName = source.ParamName ?? string.Empty,
                Type = source.Type,
                Describe = source.Describe ?? string.Empty,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath ?? string.Empty
            };
        }

        /// <summary>
        /// 判断两个输入链接绑定是否等价。
        /// </summary>
        private static bool AreEquivalentInputBindings(DefectPostProcessInputBinding left, DefectPostProcessInputBinding right)
        {
            left ??= new DefectPostProcessInputBinding();
            right ??= new DefectPostProcessInputBinding();

            return left.IsLink == right.IsLink
                && left.LinkGuid == right.LinkGuid
                && left.Serial == right.Serial
                && string.Equals(left.ParentNode ?? string.Empty, right.ParentNode ?? string.Empty, StringComparison.Ordinal)
                && left.Guid == right.Guid
                && left.Resourece == right.Resourece
                && string.Equals(left.Name ?? string.Empty, right.Name ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(left.ParamName ?? string.Empty, right.ParamName ?? string.Empty, StringComparison.Ordinal)
                && left.Type == right.Type
                && string.Equals(left.Describe ?? string.Empty, right.Describe ?? string.Empty, StringComparison.Ordinal)
                && left.IsGlobal == right.IsGlobal
                && string.Equals(left.ResourcePath ?? string.Empty, right.ResourcePath ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断输入链接绑定是否包含有效元数据。
        /// </summary>
        private static bool HasInputBindingMetadata(DefectPostProcessInputBinding binding)
        {
            if (binding == null)
            {
                return false;
            }

            return binding.IsLink
                || binding.LinkGuid != Guid.Empty
                || binding.Serial >= 0
                || !string.IsNullOrWhiteSpace(binding.ParentNode)
                || binding.Guid != Guid.Empty
                || binding.Resourece != ResoureceType.None
                || !string.IsNullOrWhiteSpace(binding.Name)
                || !string.IsNullOrWhiteSpace(binding.ParamName)
                || binding.Type != DataType.None
                || !string.IsNullOrWhiteSpace(binding.Describe)
                || binding.IsGlobal
                || !string.IsNullOrWhiteSpace(binding.ResourcePath);
        }

        /// <summary>
        /// 解析输入参数类型，优先使用值和资源路径推断结果。
        /// </summary>
        private static DataType ResolveParamType(DataType declaredType, object value, string resourcePath, DataType fallbackType = DataType.None)
        {
            DataType inferredType = InferParamType(value);
            if (inferredType != DataType.None)
            {
                return inferredType;
            }

            inferredType = InferParamTypeFromResourcePath(resourcePath);
            if (inferredType != DataType.None)
            {
                return inferredType;
            }

            if (declaredType != DataType.None && declaredType != DataType._object && declaredType != DataType.Object)
            {
                return declaredType;
            }

            return fallbackType != DataType.None ? fallbackType : declaredType;
        }

        /// <summary>
        /// 根据当前输入值推断参数类型。
        /// </summary>
        private static DataType InferParamType(object value)
        {
            if (value == null)
            {
                return DataType.None;
            }

            if (value is HObject)
            {
                return DataType.HObject;
            }

            if (value is string)
            {
                return DataType.String;
            }

            if (value is bool)
            {
                return DataType.Bool;
            }

            if (value is DateTime)
            {
                return DataType.Datetime;
            }

            if (value is Enum)
            {
                return DataType.Enum;
            }

            if (value is byte or short or int or long)
            {
                return DataType.Int;
            }

            if (value is float or double or decimal)
            {
                return DataType.Double;
            }

            if (value is IDictionary)
            {
                return DataType.Dict;
            }

            if (value is IEnumerable<Result>)
            {
                return DataType.List;
            }

            if (value is Array)
            {
                return DataType.Array;
            }

            if (value is IList)
            {
                return DataType.List;
            }

            return DataType.None;
        }

        /// <summary>
        /// 根据资源路径中的成员类型推断参数类型。
        /// </summary>
        private static DataType InferParamTypeFromResourcePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return DataType.None;
            }

            int lastDotIndex = resourcePath.LastIndexOf('.');
            if (lastDotIndex <= 0 || lastDotIndex >= resourcePath.Length - 1)
            {
                return DataType.None;
            }

            string typeName = resourcePath.Substring(0, lastDotIndex);
            string memberName = resourcePath.Substring(lastDotIndex + 1);
            Type declaringType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(type => type != null);
            if (declaringType == null)
            {
                return DataType.None;
            }

            MemberInfo member = declaringType
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(info => info is PropertyInfo || info is FieldInfo);
            if (member == null)
            {
                return DataType.None;
            }

            Type memberType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };

            return InferParamTypeFromType(memberType);
        }

        /// <summary>
        /// 根据 .NET 类型推断模块参数类型。
        /// </summary>
        private static DataType InferParamTypeFromType(Type type)
        {
            if (type == null)
            {
                return DataType.None;
            }

            if (typeof(HObject).IsAssignableFrom(type))
            {
                return DataType.HObject;
            }

            if (type == typeof(string))
            {
                return DataType.String;
            }

            if (type == typeof(bool))
            {
                return DataType.Bool;
            }

            if (type == typeof(DateTime))
            {
                return DataType.Datetime;
            }

            if (type.IsEnum)
            {
                return DataType.Enum;
            }

            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
            {
                return DataType.Int;
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                return DataType.Double;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return DataType.Dict;
            }

            if (type.IsArray)
            {
                return DataType.Array;
            }

            if (typeof(IEnumerable<Result>).IsAssignableFrom(type) || typeof(IList).IsAssignableFrom(type))
            {
                return DataType.List;
            }

            return DataType.None;
        }

        /// <summary>
        /// 按持久化绑定恢复输入链接元数据。
        /// </summary>
        private static void RestoreInputLinkMetadata(TransmitParam param, DefectPostProcessInputBinding binding, Guid guid, string name, DataType type = DataType.None)
        {
            if (param == null)
            {
                return;
            }

            if (!HasInputBindingMetadata(binding))
            {
                RestoreInputLinkMetadata(param, guid, name, type);
                return;
            }

            param.IsLink = binding.IsLink;
            param.LinkGuid = binding.LinkGuid;
            param.Serial = binding.Serial;
            param.ParentNode = binding.ParentNode ?? string.Empty;
            param.Guid = binding.Guid != Guid.Empty ? binding.Guid : guid;
            param.Resourece = binding.Resourece == ResoureceType.None ? ResoureceType.Inupt : binding.Resourece;
            param.Name = !string.IsNullOrWhiteSpace(binding.Name) ? binding.Name : name;
            param.ParamName = binding.ParamName ?? string.Empty;
            param.Type = binding.Type != DataType.None ? binding.Type : type;
            param.Describe = binding.Describe ?? string.Empty;
            param.IsGlobal = binding.IsGlobal;
            param.ResourcePath = binding.ResourcePath ?? string.Empty;
        }

        /// <summary>
        /// 在缺少绑定快照时补全输入链接的默认元数据。
        /// </summary>
        private static void RestoreInputLinkMetadata(TransmitParam param, Guid guid, string name, DataType type = DataType.None)
        {
            if (param == null)
            {
                return;
            }

            if (param.Guid == Guid.Empty && guid != Guid.Empty)
            {
                param.Guid = guid;
            }

            if (string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(name))
            {
                param.Name = name;
            }

            if (param.Resourece == ResoureceType.None)
            {
                param.Resourece = ResoureceType.Inupt;
            }

            if (param.Type == DataType.None && type != DataType.None)
            {
                param.Type = type;
            }
        }

        /// <summary>
        /// 将可变输入类型统一为不限类型，避免 WxLink 和界面筛选遗漏对象结果。
        /// </summary>
        private void NormalizeFlexibleInputLinkTypes()
        {
            _inputImage.Type = DataType.None;
            _inputResults.Type = DataType.None;
            _inputPixelEquivalent.Type = DataType.None;

            InputImageBinding ??= new DefectPostProcessInputBinding();
            InputResultsBinding ??= new DefectPostProcessInputBinding();
            InputPixelEquivalentBinding ??= new DefectPostProcessInputBinding();
            InputImageBinding.Type = DataType.None;
            InputResultsBinding.Type = DataType.None;
            InputPixelEquivalentBinding.Type = DataType.None;
        }

        /// <summary>
        /// 从当前标定文件上下文读取 interval 像素当量。
        /// </summary>
        private bool TryGetCalibrationIntervalPixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY)
        {
            pixelEquivalentX = _calibrationIntervalX > 0 ? _calibrationIntervalX : 0d;
            pixelEquivalentY = _calibrationIntervalY > 0 ? _calibrationIntervalY : 0d;
            return pixelEquivalentX > 0 && pixelEquivalentY > 0;
        }

        /// <summary>
        /// 读取边缘基准 X，无法转换时返回默认值。
        /// </summary>
        private double GetEdgeCalibrationXValue(object value)
        {
            return TryConvertToDouble(value, out double edgeCalibrationX) ? edgeCalibrationX : DefaultEdgeCalibrationX;
        }

        /// <summary>
        /// 按实时链接、输入快照和当前缓存顺序解析边缘基准 X。
        /// </summary>
        private object ResolvePreferredEdgeCalibrationXInputValue()
        {
            object snapshotValue = GetTransmitParam(InputParams, _inputEdgeCalibrationX);
            object liveValue = GetLiveLinkedValue(_inputEdgeCalibrationX);
            object currentValue = _inputEdgeCalibrationX?.Value;

            if (TryConvertToDouble(liveValue, out _))
            {
                return liveValue;
            }

            if (TryConvertToDouble(snapshotValue, out _))
            {
                return snapshotValue;
            }

            if (TryConvertToDouble(currentValue, out _))
            {
                return currentValue;
            }

            return DefaultEdgeCalibrationX;
        }

        /// <summary>
        /// 重新加载标定文件上下文，完整文件优先使用 SDK，不完整文件使用 interval 参数。
        /// </summary>
        private void ReloadCalibrationFileContext()
        {
            string normalizedPath = NormalizeCalibrationFilePath(CalibrationFilePath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                ResetCalibrationFileContext();
                return;
            }

            if (string.Equals(_loadedCalibrationFilePath, normalizedPath, StringComparison.OrdinalIgnoreCase)
                && HasLoadedCalibrationFileContext())
            {
                return;
            }

            ResetCalibrationFileContext();

            if (!File.Exists(normalizedPath))
            {
                LogWarning($"标定文件不存在: {normalizedPath}");
                return;
            }

            bool triedJson = ShouldTryLoadCalibrationJson(normalizedPath);
            string jsonError = string.Empty;
            if (triedJson && TryLoadCalibrationJsonContext(normalizedPath, out CalibrationCoordinateContext calibrationContext, out jsonError))
            {
                _loadedCalibrationFilePath = normalizedPath;
                _calibrationCameraId = calibrationContext.CameraId;
                _calibrationIntervalX = calibrationContext.IntervalX;
                _calibrationIntervalY = calibrationContext.IntervalY;
                _calibrationCoordinateContext = calibrationContext;
                return;
            }

            if (TryLoadCalibrationTextContext(normalizedPath, out CalibrationCoordinateContext textContext, out string textError))
            {
                _loadedCalibrationFilePath = normalizedPath;
                _calibrationCameraId = textContext.CameraId;
                _calibrationIntervalX = textContext.IntervalX;
                _calibrationIntervalY = textContext.IntervalY;
                _calibrationCoordinateContext = textContext;
                return;
            }

            if (TryLoadCalibrationFileWithSdk(normalizedPath, out CameraCalibrationSdk cameraCalibrationSdk,
                out CameraCalibrationSdk.CameraParams cameraParams, out string sdkError))
            {
                CalibrationCoordinateContext sdkCalibrationContext = CreateCalibrationCoordinateContext(cameraParams);
                _cameraCalibrationSdk = cameraCalibrationSdk;
                _loadedCalibrationFilePath = normalizedPath;
                _calibrationCameraId = sdkCalibrationContext.CameraId;
                _calibrationIntervalX = sdkCalibrationContext.IntervalX;
                _calibrationIntervalY = sdkCalibrationContext.IntervalY;
                _calibrationCoordinateContext = sdkCalibrationContext;
                return;
            }

            ResetCalibrationFileContext();
            LogWarning(
                $"标定文件无法加载: Path={normalizedPath}, " +
                $"SdkError={sdkError}{(triedJson ? $", JsonError={jsonError}" : string.Empty)}, TextError={textError}");
        }

        /// <summary>
        /// 清空标定文件上下文。
        /// </summary>
        private void ResetCalibrationFileContext()
        {
            _cameraCalibrationSdk?.Dispose();
            _cameraCalibrationSdk = null;
            _loadedCalibrationFilePath = string.Empty;
            _calibrationCameraId = string.Empty;
            _calibrationIntervalX = 0d;
            _calibrationIntervalY = 0d;
            _calibrationCoordinateContext = null;
        }

        /// <summary>
        /// 判断当前是否已加载可用的标定上下文。
        /// </summary>
        private bool HasLoadedCalibrationFileContext()
        {
            return _cameraCalibrationSdk != null
                || _calibrationCoordinateContext != null
                || _calibrationIntervalX > 0
                || _calibrationIntervalY > 0;
        }

        /// <summary>
        /// 清空输入缓存值，避免复用上一轮链接数据。
        /// </summary>
        private void ClearCachedInputValues()
        {
            if (_inputImage != null)
            {
                _inputImage.Value = null;
            }

            if (_inputResults != null)
            {
                if (_inputResults.IsLink)
                {
                    _inputResults.Value = null;
                }
            }

            if (_inputPixelEquivalent != null)
            {
                _inputPixelEquivalent.Value = null;
            }

            if (_inputPixelEquivalentX != null)
            {
                _inputPixelEquivalentX.Value = null;
            }

            if (_inputPixelEquivalentY != null)
            {
                _inputPixelEquivalentY.Value = null;
            }
        }
        #endregion

        #endregion

        #region 输入输出运行时辅助


        private void UpdateOutputParamValues()
        {
            foreach (TransmitParam item in OutputParams ?? Enumerable.Empty<TransmitParam>())
            {
                if (string.IsNullOrWhiteSpace(item.ParamName)
                    || !TryGetOutputParamValue(item.ParamName, out object value))
                {
                    continue;
                }

                object outputValue = CloneOutputValueForBoundary(value);
                DisposePreviousOutputValue(item.Value, value, outputValue);
                item.Value = outputValue;
            }
        }

        /// <summary>
        /// 按当前已配置输出名称读取值，避免每次反射收集所有输出。
        /// </summary>
        private bool TryGetOutputParamValue(string paramName, out object value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(paramName)
                || !OutputParamInfoMap.Value.TryGetValue(paramName, out OutputParamInfo info)
                || info?.MemberInfo == null)
            {
                return false;
            }

            value = info.IsField
                ? ((FieldInfo)info.MemberInfo).GetValue(this)
                : ((PropertyInfo)info.MemberInfo).GetValue(this);
            return true;
        }

        private sealed class RuntimeEditingState
        {
            public List<DefectRuleConfig> DefectRuleConfigs { get; set; } = new List<DefectRuleConfig>();

            public SheetSizeJudgeConfig SheetSizeJudge { get; set; } = new SheetSizeJudgeConfig();

            public bool IsFastModeEnabled { get; set; }

            public string PreferredRuleKey { get; set; } = string.Empty;

            public bool IsSchemeDirty { get; set; }
        }

        /// <summary>
        /// 解析优先使用的结果输入值。
        /// </summary>
        private object ResolvePreferredResultsInputValue()
        {
            object snapshotValue = GetSnapshotLinkedValue(_inputResults);
            object liveValue = GetLiveLinkedValue(_inputResults);
            object modelValue = GetUpstreamNodeResultsValue(_inputResults);

            int snapshotSegReady = GetSegReadyCount(snapshotValue);
            int liveSegReady = GetSegReadyCount(liveValue);
            int modelSegReady = GetSegReadyCount(modelValue);

            object selectedValue = null;
            string selectedSource = "none";

            (string Source, object Value, int SegReadyCount)[] candidates =
            {
                ("live", liveValue, liveSegReady),
                ("model", modelValue, modelSegReady),
                ("snapshot", snapshotValue, snapshotSegReady)
            };

            foreach ((string source, object value, int segReadyCount) in candidates)
            {
                if (value == null || segReadyCount <= 0)
                {
                    continue;
                }

                selectedValue = value;
                selectedSource = source;
                break;
            }

            if (selectedValue == null)
            {
                foreach ((string source, object value, int _) in candidates)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    selectedValue = value;
                    selectedSource = source;
                    break;
                }
            }

            LogTrace(
                $"输入结果候选: SnapshotSegReady={snapshotSegReady}, " +
                $"LiveSegReady={liveSegReady}, ModelSegReady={modelSegReady}, " +
                $"Selected={selectedSource}");

            return selectedValue;
        }

        /// <summary>
        /// 判断结果输入是否具备可优先选择的真实分割结果。
        /// </summary>
        private static bool HasSelectableResultPayload(object value)
        {
            return GetSegReadyCount(value) > 0;
        }

        /// <summary>
        /// 上游暂时取不到结果时，仅保留真实结果，避免普通空值恢复成旧脏数据。
        /// </summary>
        private static bool ShouldKeepResultsFallback(object value)
        {
            return HasSelectableResultPayload(value);
        }

        /// <summary>
        /// 获取输入快照中的当前链接值，避免框架通用 DeepClone 破坏 Result.Seg。
        /// </summary>
        private object GetSnapshotLinkedValue(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            TransmitParam source = InputParams?.FirstOrDefault(item => IsSameLinkedParam(item, param))
                ?? InputParams?.FirstOrDefault(item => item.Guid == param.Guid);

            return source == null ? null : CloneInputValue(source.Value);
        }

        /// <summary>
        /// 获取已准备好的分割区域数量。
        /// </summary>
        private static int GetSegReadyCount(object value)
        {
            int count = 0;
            foreach (Result result in EnumerateLinkedResultsWithoutClone(value))
            {
                if (IsInitializedSafely(result?.Seg))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 只读遍历链接结果，避免为了诊断计数克隆 Result.Seg。
        /// </summary>
        private static IEnumerable<Result> EnumerateLinkedResultsWithoutClone(object value)
        {
            switch (value)
            {
                case null:
                    yield break;
                case Result result:
                    yield return result;
                    yield break;
                case IEnumerable<IEnumerable<Result>> resultGroups:
                    foreach (IEnumerable<Result> group in resultGroups)
                    {
                        foreach (Result item in group ?? Enumerable.Empty<Result>())
                        {
                            if (item != null)
                            {
                                yield return item;
                            }
                        }
                    }

                    yield break;
                case IEnumerable enumerable when value is not string:
                    foreach (object item in enumerable)
                    {
                        if (item is Result flatResult)
                        {
                            yield return flatResult;
                            continue;
                        }

                        if (item is IEnumerable<Result> group)
                        {
                            foreach (Result groupResult in group)
                            {
                                if (groupResult != null)
                                {
                                    yield return groupResult;
                                }
                            }
                        }
                    }

                    yield break;
            }
        }

        /// <summary>
        /// 获取链接参数的实时值。
        /// </summary>
        private object GetLiveLinkedValue(TransmitParam param)
        {
            if (param == null)
            {
                return null;
            }

            TransmitParam source = null;
            switch (param.Resourece)
            {
                case ResoureceType.CustomGlobal:
                    source = PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams?.FirstOrDefault(item => item.Guid == param.Guid);
                    break;
                case ResoureceType.Global:
                    source = PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams?.FirstOrDefault(item => item.Guid == param.Guid);
                    break;
                case ResoureceType.Inupt:
                case ResoureceType.LastInput:
                case ResoureceType.None:
                    source = GetCachedOutputParam(param)
                        ?? moduleInputParam?.TransmitParams?.Values?.OfType<TransmitParam>()?.FirstOrDefault(item => IsSameLinkedParam(item, param));
                    break;
            }

            return source == null ? null : CloneInputValue(source.Value);
        }

        /// <summary>
        /// 获取上游节点的结果值。
        /// </summary>
        private object GetUpstreamNodeResultsValue(TransmitParam param)
        {
            if (param == null || param.Serial < 0)
            {
                return null;
            }

            Dictionary<string, object> nodeParamCaches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
            if (nodeParamCaches == null || nodeParamCaches.Count == 0)
            {
                return null;
            }

            string[] candidateKeys =
            {
                param.Serial.ToString("D3"),
                param.Serial.ToString()
            };

            object model = candidateKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => nodeParamCaches.TryGetValue(key, out object cachedModel) ? cachedModel : null)
                .FirstOrDefault(item => item != null);

            if (model == null)
            {
                LogWarning($"上游节点缓存不存在: Serial={param.Serial}");
                return null;
            }

            try
            {
                string propertyName = string.IsNullOrWhiteSpace(param.ParamName) ? "Results" : param.ParamName;
                PropertyInfo propertyInfo = model.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                    ?? model.GetType().GetProperty("Results", BindingFlags.Public | BindingFlags.Instance);

                object value = propertyInfo?.GetValue(model);
                if (IsVerboseFlowLogEnabled())
                {
                    LogTrace(
                        $"上游节点结果: Serial={param.Serial}, Property={propertyInfo?.Name ?? "<null>"}, " +
                        $"SegReady={GetSegReadyCount(value)}");
                }

                return CloneInputValue(value);
            }
            catch (Exception ex)
            {
                LogWarning($"读取上游节点结果失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取缓存的输出参数。
        /// </summary>
        private TransmitParam GetCachedOutputParam(TransmitParam param)
        {
            var cache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
            if (cache == null || cache.Count == 0)
            {
                return null;
            }

            IEnumerable<ObservableCollection<TransmitParam>> candidates;
            if (param.Serial >= 0 && cache.TryGetValue(param.Serial.ToString(), out ObservableCollection<TransmitParam> directMatch))
            {
                candidates = new[] { directMatch };
            }
            else
            {
                candidates = cache.Values;
            }

            foreach (ObservableCollection<TransmitParam> outputParams in candidates.Where(item => item != null))
            {
                TransmitParam matched = outputParams.FirstOrDefault(item => IsSameLinkedParam(item, param));
                if (matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断是否为同一个链接参数。
        /// </summary>
        private static bool IsSameLinkedParam(TransmitParam source, TransmitParam target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            if (source.Guid != Guid.Empty && target.Guid != Guid.Empty && source.Guid == target.Guid)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(source.ParamName) && !string.IsNullOrWhiteSpace(target.ParamName)
                && string.Equals(source.ParamName, target.ParamName, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(source.Name) && !string.IsNullOrWhiteSpace(target.Name)
                && string.Equals(source.Name, target.Name, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(source.ResourcePath) && !string.IsNullOrWhiteSpace(target.ResourcePath)
                && string.Equals(source.ResourcePath, target.ResourcePath, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region 输出测量与结果同步
        /// <summary>
        /// 定义缺陷测量值的计算来源。
        /// </summary>
        private enum DefectMeasurementMode
        {
            None,
            CalibrationFile
        }

        [JsonIgnore]
        private ObservableCollection<DefectMeasurementOutputItem> _defectMeasurements = new ObservableCollection<DefectMeasurementOutputItem>();

        [JsonIgnore]
        public ObservableCollection<DefectMeasurementOutputItem> DefectMeasurements
        {
            get { return _defectMeasurements; }
            private set { SetProperty(ref _defectMeasurements, value); }
        }

        /// <summary>
        /// 刷新模块输出中的缺陷测量结果集合。
        /// </summary>
        private void UpdateDefectMeasurementOutputs()
        {
            List<DefectMeasurementOutputItem> measurements = BuildDefectMeasurementOutputs();

            RunOnDispatcher(() =>
            {
                DefectMeasurements = new ObservableCollection<DefectMeasurementOutputItem>(measurements);
            });
        }

        /// <summary>
        /// 构建完整的缺陷测量结果集合。
        /// </summary>
        private List<DefectMeasurementOutputItem> BuildDefectMeasurementOutputs()
        {
            List<DefectMeasurementOutputItem> measurements = new List<DefectMeasurementOutputItem>();
            if (Results == null || Results.Count == 0)
            {
                return measurements;
            }

            DefectMeasurementMode measurementMode = GetDefectMeasurementMode();
            Dictionary<string, int> instanceIndexMap = new Dictionary<string, int>();
            foreach (Result result in Results)
            {
                if (result == null)
                {
                    continue;
                }

                string defectName = ResolveClassName(result.ClassId, result.ClassName);
                string ruleKey = GetDefectRuleKey(result.ClassId, defectName);
                if (!instanceIndexMap.ContainsKey(ruleKey))
                {
                    instanceIndexMap[ruleKey] = 0;
                }

                instanceIndexMap[ruleKey]++;

                DefectMeasurementOutputItem output = CreateDefectMeasurementOutput(result, instanceIndexMap[ruleKey], measurementMode);
                UpdateResultMeasurementCache(result, output);
                measurements.Add(output);
            }

            return measurements;
        }

        /// <summary>
        /// 构建单个缺陷实例的测量输出项。
        /// </summary>
        private DefectMeasurementOutputItem CreateDefectMeasurementOutput(Result result, int instanceIndex, DefectMeasurementMode measurementMode)
        {
            DefectMeasurementOutputItem output = new DefectMeasurementOutputItem
            {
                InstanceIndex = instanceIndex,
                ClassId = result.ClassId,
                ClassName = ResolveClassName(result.ClassId, result.ClassName),
                Confidence = result.Confidence,
                MeasurementSource = string.IsNullOrWhiteSpace(GetResultCoordinateSource(result))
                    ? GetDefectMeasurementSource(measurementMode)
                    : GetResultCoordinateSource(result)
            };

            output.PhysicalX = GetResultPhysicalCoordinate(result, DefectPostProcessResultKeys.WorldX);
            output.PhysicalY = GetResultPhysicalCoordinate(result, DefectPostProcessResultKeys.WorldY);
            output.PhysicalZ = GetResultPhysicalCoordinate(result, DefectPostProcessResultKeys.WorldZ);

            if (output.PhysicalX == null
                && output.PhysicalY == null
                && TryConvertResultCenterToWorld(result, out double worldX, out double worldY, out double worldZ, out string coordinateSource))
            {
                output.PhysicalX = worldX;
                output.PhysicalY = worldY;
                output.PhysicalZ = worldZ;
                output.MeasurementSource = coordinateSource;
            }

            switch (measurementMode)
            {
                case DefectMeasurementMode.CalibrationFile:
                    if (TryMeasureResultSizeByCalibrationFile(result, out double length, out double width))
                    {
                        if (length > 0)
                        {
                            output.Length = length;
                        }

                        if (width > 0)
                        {
                            output.Width = width;
                        }
                    }

                    if (TryGetCachedActualArea(result, out double actualArea)
                        || TryCalculateActualAreaByCalibrationInterval(result, out actualArea))
                    {
                        output.Area = actualArea;
                    }

                    break;
            }

            return output;
        }

        /// <summary>
        /// 获取当前缺陷测量模式。
        /// </summary>
        private DefectMeasurementMode GetDefectMeasurementMode()
        {
            if (HasCalibrationMeasurementSupport())
            {
                return DefectMeasurementMode.CalibrationFile;
            }

            return DefectMeasurementMode.None;
        }

        /// <summary>
        /// 获取缺陷测量来源标记。
        /// </summary>
        private static string GetDefectMeasurementSource(DefectMeasurementMode measurementMode)
        {
            return measurementMode switch
            {
                DefectMeasurementMode.CalibrationFile => CalibrationCoordinateSource,
                _ => string.Empty
            };
        }

        /// <summary>
        /// 更新单个缺陷结果缓存中的实际面积。
        /// </summary>
        private void UpdateResultActualAreaCache(Result result)
        {
            if (result == null)
            {
                return;
            }

            if (TryCalculateActualAreaByCalibrationInterval(result, out double actualArea))
            {
                result.Others ??= new Dictionary<string, object>();
                SetResultMeasurementValue(result, DefectPostProcessResultKeys.ActualArea, actualArea);
                return;
            }

            SetResultMeasurementValue(result, DefectPostProcessResultKeys.ActualArea, null);
        }

        /// <summary>
        /// 使用标定文件 interval 计算实际面积。
        /// </summary>
        private bool TryCalculateActualAreaByCalibrationInterval(Result result, out double actualArea)
        {
            actualArea = 0d;

            double pixelArea = GetMaskArea(result);
            if (pixelArea <= 0)
            {
                return false;
            }

            if (!TryGetCalibrationAreaPixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY))
            {
                return false;
            }

            double scaleFactor = pixelEquivalentX * pixelEquivalentY;
            if (scaleFactor <= 0 || double.IsNaN(scaleFactor) || double.IsInfinity(scaleFactor))
            {
                return false;
            }

            actualArea = pixelArea * scaleFactor;
            return actualArea > 0 && !double.IsNaN(actualArea) && !double.IsInfinity(actualArea);
        }

        /// <summary>
        /// 读取本次执行已写入 Result.Others 的实际面积缓存。
        /// </summary>
        private static bool TryGetCachedActualArea(Result result, out double actualArea)
        {
            actualArea = 0d;
            double? cachedArea = GetResultPhysicalCoordinate(result, DefectPostProcessResultKeys.ActualArea);
            if (cachedArea == null || cachedArea <= 0)
            {
                return false;
            }

            actualArea = cachedArea.Value;
            return !double.IsNaN(actualArea) && !double.IsInfinity(actualArea);
        }

        /// <summary>
        /// 获取标定文件中的 interval 像素当量。
        /// </summary>
        private bool TryGetCalibrationAreaPixelEquivalent(out double pixelEquivalentX, out double pixelEquivalentY)
        {
            return TryGetCalibrationIntervalPixelEquivalent(out pixelEquivalentX, out pixelEquivalentY);
        }

        /// <summary>
        /// 判断当前是否具备标定文件测量能力。
        /// </summary>
        private bool HasCalibrationMeasurementSupport()
        {
            if (HasCalibrationTransformSupport())
            {
                return true;
            }

            return TryGetCalibrationIntervalPixelEquivalent(out _, out _);
        }

        /// <summary>
        /// 判断标定文件是否具备完整像素到物理坐标转换能力。
        /// </summary>
        private bool HasCalibrationTransformSupport()
        {
            if (_cameraCalibrationSdk != null
                && !string.IsNullOrWhiteSpace(_calibrationCameraId)
                && _calibrationCoordinateContext != null
                && CanConvertWithCalibrationContext(_calibrationCoordinateContext))
            {
                return true;
            }

            return _calibrationCoordinateContext != null
                && CanConvertWithCalibrationContext(_calibrationCoordinateContext);
        }

        /// <summary>
        /// 尝试通过标定文件将像素坐标转换为物理坐标。
        /// </summary>
        private bool TryConvertPixelToCalibrationWorld(double pixelX, double pixelY, out double worldX, out double worldY, out double worldZ)
        {
            worldX = 0d;
            worldY = 0d;
            worldZ = 0d;

            if (HasCalibrationTransformSupport()
                && _cameraCalibrationSdk != null
                && !string.IsNullOrWhiteSpace(_calibrationCameraId))
            {
                try
                {
                    _cameraCalibrationSdk.pixelToWorld(_calibrationCameraId, pixelX, pixelY, out worldX, out worldY, out worldZ);
                    if (IsValidWorldCoordinate(worldX, worldY, worldZ))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"标定像素转世界坐标失败: {ex.Message}");
                }
            }

            return TryConvertPixelToWorldWithCalibrationContext(pixelX, pixelY, out worldX, out worldY, out worldZ);
        }

        /// <summary>
        /// 按缺陷外接矩形测量实际长宽。
        /// </summary>
        private bool TryMeasureResultSizeByCalibrationFile(Result result, out double length, out double width)
        {
            length = 0d;
            width = 0d;

            if (!TryGetResultRectangleParameters(result, out double centerX, out double centerY, out double widthPixel, out double heightPixel, out double angle))
            {
                return false;
            }

            double halfWidth = widthPixel / 2d;
            double halfHeight = heightPixel / 2d;

            double axis1X = System.Math.Cos(angle) * halfWidth;
            double axis1Y = System.Math.Sin(angle) * halfWidth;
            double axis2X = -System.Math.Sin(angle) * halfHeight;
            double axis2Y = System.Math.Cos(angle) * halfHeight;

            var corners = new (double X, double Y)[]
            {
                (centerX - axis1X - axis2X, centerY - axis1Y - axis2Y),
                (centerX + axis1X - axis2X, centerY + axis1Y - axis2Y),
                (centerX + axis1X + axis2X, centerY + axis1Y + axis2Y),
                (centerX - axis1X + axis2X, centerY - axis1Y + axis2Y)
            };

            var worldCorners = new (double X, double Y)[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                if (!TryConvertPixelToCalibrationWorld(corners[i].X, corners[i].Y, out double worldX, out double worldY, out _))
                {
                    return false;
                }

                worldCorners[i] = (worldX, worldY);
            }

            double side1 = GetPointDistance(worldCorners[0], worldCorners[1]);
            double side2 = GetPointDistance(worldCorners[1], worldCorners[2]);
            if (side1 <= 0 || side2 <= 0)
            {
                return false;
            }

            length = System.Math.Max(side1, side2);
            width = System.Math.Min(side1, side2);
            return true;
        }

        /// <summary>
        /// 尝试获取缺陷外接矩形参数。
        /// </summary>
        private bool TryGetResultRectangleParameters(Result result, out double centerX, out double centerY, out double widthPixel, out double heightPixel, out double angle)
        {
            centerX = result?.Cx ?? 0d;
            centerY = result?.Cy ?? 0d;
            widthPixel = result?.Width ?? 0d;
            heightPixel = result?.Height ?? 0d;
            angle = result?.Angle ?? 0d;

            if (result != null
                && !float.IsNaN(result.Cx)
                && !float.IsNaN(result.Cy)
                && !float.IsInfinity(result.Cx)
                && !float.IsInfinity(result.Cy)
                && widthPixel > 0
                && heightPixel > 0)
            {
                return true;
            }

            if (!IsInitializedSafely(result?.Seg))
            {
                return false;
            }

            try
            {
                HOperatorSet.SmallestRectangle2(result.Seg, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                centerX = column.D;
                centerY = row.D;
                widthPixel = length1.D * 2d;
                heightPixel = length2.D * 2d;
                angle = phi.D;
                return widthPixel > 0 && heightPixel > 0;
            }
            catch (Exception ex)
            {
                LogWarning($"获取缺陷矩形参数失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将测量结果缓存回筛选后的缺陷结果，便于后续模块直接读取。
        /// </summary>
        private static void UpdateResultMeasurementCache(Result result, DefectMeasurementOutputItem output)
        {
            if (result == null)
            {
                return;
            }

            result.Others ??= new Dictionary<string, object>();
            SetResultMeasurementValue(result, DefectPostProcessResultKeys.InstanceIndex, output?.InstanceIndex);
            SetResultMeasurementValue(result, DefectPostProcessResultKeys.ActualLength, output?.Length);
            SetResultMeasurementValue(result, DefectPostProcessResultKeys.ActualWidth, output?.Width);
            SetResultMeasurementValue(result, DefectPostProcessResultKeys.ActualArea, output?.Area);
        }

        /// <summary>
        /// 设置单个测量缓存值；若没有有效值则移除对应键。
        /// </summary>
        private static void SetResultMeasurementValue(Result result, string key, object value)
        {
            if (result?.Others == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            result.Others[key] = value;
        }

        /// <summary>
        /// 计算两点间距离。
        /// </summary>
        private static double GetPointDistance((double X, double Y) point1, (double X, double Y) point2)
        {
            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;
            return System.Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }


        #endregion
    }
}

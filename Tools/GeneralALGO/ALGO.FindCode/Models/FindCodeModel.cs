using System;
using ALGO.FindCode.ViewModels;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using static ALGO.FindCode.ViewModels.FindCodeViewModel;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.FindCode
{
    [Serializable]
    public class FindCodeModel : ModelParamBase
    {
        #region 字段与状态
        [JsonIgnore]
        public override VMHWindowControl mWindowH { get; set; } = null!;

        /// <summary>
        /// 从上游输入复制出的自有图像，模块执行、预览和释放都只操作该副本。
        /// </summary>
        [JsonIgnore]
        private readonly List<HImage> _ownedInputImages = new();

        /// <summary>
        /// 按输入图像顺序保存识别区域副本，预览切图时只显示当前图像对应的区域。
        /// </summary>
        [JsonIgnore]
        private readonly List<List<HRoi>> _previewRoisByImage = new();

        /// <summary>
        /// 当前预览是否使用最近一次执行结果，输入刷新时关闭以避免显示旧结果。
        /// </summary>
        [JsonIgnore]
        private bool _previewUsesExecutionResults;

        /// <summary>
        /// 当前读码句柄类型：true 表示一维码模型，false 表示二维码模型。
        /// </summary>
        [JsonIgnore]
        private bool _is1DCodeModel;

        /// <summary>
        /// 最近一次读码失败是否来自 HALCON 异常，用于区分“未识别到码”和“执行错误”。
        /// </summary>
        [JsonIgnore]
        private bool _lastFindCodeFailedByException;
        #endregion

        #region 界面绑定属性

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        [InputParam]
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 显示的 ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        [JsonIgnore]
        private List<string> _outputStrings = new();
        /// <summary>
        /// 输出识别结果；每张输入图像对应一个元素。
        /// </summary>
        [OutputParam("OutputStrings", "识别信息列表")]
        [JsonIgnore]
        public List<string> OutputStrings
        {
            get { return _outputStrings; }
            set { SetProperty(ref _outputStrings, value ?? new List<string>()); }
        }

        [JsonIgnore]
        private ObservableCollection<ParamDefinition> _codeParamDefinitions_1D = new();
        /// <summary>
        /// 一维码 HALCON 参数集合，反序列化后会按当前版本参数表补齐。
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<ParamDefinition> CodeParamDefinitions_1D
        {
            get { return _codeParamDefinitions_1D; }
            set { SetProperty(ref _codeParamDefinitions_1D, value); }
        }

        [JsonIgnore]
        private ObservableCollection<ParamDefinition> _codeParamDefinitions_2D = new();
        /// <summary>
        /// 二维码 HALCON 参数集合，保留旧项目已保存的用户值。
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<ParamDefinition> CodeParamDefinitions_2D
        {
            get { return _codeParamDefinitions_2D; }
            set { SetProperty(ref _codeParamDefinitions_2D, value); }
        }

        [JsonIgnore]
        private CodeType _codeType = CodeType.Aztec_Code;
        /// <summary>
        /// 当前选择的扫码类型，前 4 项按一维码模型处理，其余按二维码模型处理。
        /// </summary>
        public CodeType CodeType
        {
            get { return _codeType; }
            set { SetProperty(ref _codeType, value); }
        }

        [JsonIgnore]
        private HTuple _deCodeHandle = new HTuple();
        /// <summary>
        /// HALCON 读码模型句柄，由当前模块创建并在切换码型或释放模块时清理。
        /// </summary>
        public HTuple DeCodeHandle
        {
            get { return _deCodeHandle; }
            set { _deCodeHandle = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HObject? _previewImageObject;
        /// <summary>预览图像对象</summary>
        [JsonIgnore]
        public HObject? PreviewImageObject
        {
            get => _previewImageObject;
            private set { SetProperty(ref _previewImageObject, value); }
        }

        /// <summary>预览覆盖层绘制对象集合</summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new();

        /// <summary>预览窗口左上角显示的当前图像识别内容。</summary>
        [JsonIgnore]
        public ObservableCollection<string> PreviewResultLabels { get; } = new();

        [JsonIgnore]
        private int _currentPreviewImageIndex;
        /// <summary>
        /// 当前预览窗口显示的输入图像序号，范围始终限制在已有图像数量内。
        /// </summary>
        [JsonIgnore]
        public int CurrentPreviewImageIndex
        {
            get { return _currentPreviewImageIndex; }
            private set
            {
                int normalizedIndex = NormalizePreviewImageIndex(value);
                if (SetProperty(ref _currentPreviewImageIndex, normalizedIndex))
                    RaisePreviewImageNavigationProperties();
            }
        }

        [JsonIgnore]
        public int PreviewImageCount
        {
            get { return _ownedInputImages.Count; }
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

        #region 构造函数
        /// <summary>
        /// 创建扫码识别模型并补齐默认读码参数。
        /// </summary>
        public FindCodeModel()
        {
            EnsureParamDefinitions();
        }
        #endregion

        #region 生命周期
        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit) return true;
                if (!base.OnceInit()) return false;
                EnsureParamDefinitions();
                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch { return false; }
        }

        public override bool LoadKeyParam()
        {
            try
            {
                EnsureParamDefinitions();
                if (!base.LoadKeyParam()) return false;

                _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
                ReplaceOwnedInputImages(_inputImage?.Value);

                _previewUsesExecutionResults = false;
                RefreshPreviewDisplay(includeRoi: false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫码参数加载失败: {ex.Message}");
                return false;
            }
        }

        public override void Dispose()
        {
            ClearDeCodeHandle();
            DisposeOwnedRuntimeObjects();
            ClearMHRoi();
            base.Dispose();
        }

        /// <summary>清理读码模型句柄</summary>
        private void ClearDeCodeHandle()
        {
            if (DeCodeHandle != null && DeCodeHandle.Length > 0)
            {
                try
                {
                    if (_is1DCodeModel)
                        HOperatorSet.ClearBarCodeModel(DeCodeHandle);
                    else
                        HOperatorSet.ClearDataCode2dModel(DeCodeHandle);
                }
                catch { }
                DeCodeHandle = new HTuple();
            }
        }
        #endregion

        #region 识别流程

        /// <summary>
        /// 模块执行
        /// </summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    EnsureParamDefinitions();
                    if (!LoadKeyParam())
                    {
                        ClearMHRoi();
                        return CompleteWithoutResult(NodeStatus.Error);
                    }

                    ClearMHRoi();

                    if (_ownedInputImages.Count == 0)
                        return CompleteWithoutResult(NodeStatus.None);

                    OutputStrings.Clear();
                    ClearPreviewRoisByImage();
                    bool hasDecodedResult = false;
                    bool hasDecodeException = false;
                    foreach (var inputImage in _ownedInputImages)
                    {
                        ClearMHRoi();
                        bool status = FindCode(inputImage, out HTuple foundString);
                        if (status)
                        {
                            hasDecodedResult = true;
                            OutputStrings.Add(FormatDecodedStrings(foundString));
                        }
                        else
                        {
                            hasDecodeException |= _lastFindCodeFailedByException;
                            OutputStrings.Add(string.Empty);
                        }

                        _previewRoisByImage.Add(CopyCurrentRois());
                    }

                    ClearMHRoi();
                    _previewUsesExecutionResults = true;
                    RaisePropertyChanged(nameof(OutputStrings));
                    RaisePreviewImageNavigationProperties();
                    RefreshPreviewDisplay();
                    RefreshOutputParams();

                    if (hasDecodeException)
                        return NodeStatus.Error;

                    return hasDecodedResult ? NodeStatus.Success : NodeStatus.None;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫码执行失败: {ex.Message}");
                    return CompleteWithoutResult(NodeStatus.Error);
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: 扫码识别执行耗时 {time} ms");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public void InitializeParamDefinitions()
        {
            CodeParamDefinitions_1D = CreateDefaultCodeParamDefinitions1D();
            CodeParamDefinitions_2D = CreateDefaultCodeParamDefinitions2D();

            RaisePropertyChanged(nameof(CodeParamDefinitions_1D));
            RaisePropertyChanged(nameof(CodeParamDefinitions_2D));
        }

        /// <summary>
        /// 反序列化后补齐扫码参数，并保留旧项目中用户已保存的参数值。
        /// </summary>
        public void EnsureParamDefinitions()
        {
            CodeParamDefinitions_1D = MergeParamDefinitionsWithCanonical(
                CodeParamDefinitions_1D,
                CreateDefaultCodeParamDefinitions1D());
            CodeParamDefinitions_2D = MergeParamDefinitionsWithCanonical(
                CodeParamDefinitions_2D,
                CreateDefaultCodeParamDefinitions2D());
        }

        private static ObservableCollection<ParamDefinition> CreateDefaultCodeParamDefinitions1D()
        {
            return
            [
                new() { Name="条码最小尺寸:", UIType=ParamUIType.Number, Value=1, MinValue=1, MaxValue=10, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="条码最大尺寸:", UIType=ParamUIType.Number, Value=11, MinValue=11, MaxValue=30, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="模型持久化等级:", UIType=ParamUIType.Number, Value=0, MinValue=0, MaxValue=1, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="校验位验证:", UIType=ParamUIType.ComboBox, Value="present", Options=["present", "absent"], ValueType = ParamValueType.String },
                new() { Name="增加扫描线数:", UIType=ParamUIType.Number, Value=10, MinValue=0, MaxValue=100, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="容许误差值:", UIType=ParamUIType.ComboBox, Value="low", Options=["low", "high"], ValueType = ParamValueType.String },
                new() { Name="条码最小高度:", UIType=ParamUIType.Number, Value=100, MinValue=0, MaxValue=200, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="条码方向:", UIType=ParamUIType.Number, Value=0, MinValue=0, MaxValue=360, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="条码方向容差:", UIType=ParamUIType.Number, Value=20, MinValue=0, MaxValue=360, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="解码个数:", UIType=ParamUIType.Number, Value=0, MinValue=0, MaxValue=10, ValueType = ParamValueType.Int, SmallChange=1 }
            ];
        }

        private static ObservableCollection<ParamDefinition> CreateDefaultCodeParamDefinitions2D()
        {
            return
            [
                new() { Name="最小对比度阈值:", UIType=ParamUIType.Number, Value=30, MinValue=1, MaxValue=100, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="小模块抗噪能力:", UIType=ParamUIType.ComboBox, Value="low", Options=["low", "high"], ValueType = ParamValueType.String },
                new() { Name="码与背景的极性:", UIType=ParamUIType.ComboBox, Value="any", Options=["light_on_dark", "dark_on_light", "any"], ValueType = ParamValueType.String },
            ];
        }

        private static ObservableCollection<ParamDefinition> MergeParamDefinitionsWithCanonical(
            ObservableCollection<ParamDefinition> current,
            ObservableCollection<ParamDefinition> canonical)
        {
            var existingByName = current?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                .GroupBy(item => item.Name)
                .ToDictionary(group => group.Key, group => group.Last());

            foreach (var param in canonical)
            {
                if (existingByName?.TryGetValue(param.Name, out var existing) == true)
                {
                    param.Value = existing.Value;
                }
            }

            return canonical;
        }

        /// <summary>
        /// 使用当前码型和参数执行 HALCON 读码。
        /// </summary>
        /// <param name="ho_Image">待识别的自有输入图像，使用图像坐标生成识别区域。</param>
        /// <param name="resultStrings">识别到的码内容；未识别到时为空 HTuple。</param>
        /// <returns>true 表示至少识别到一个码；false 表示无结果或执行异常。</returns>
        public bool FindCode(HObject ho_Image, out HTuple resultStrings)
        {
            try
            {
                _lastFindCodeFailedByException = false;
                EnsureParamDefinitions();

                if (ho_Image == null || !ho_Image.IsInitialized())
                {
                    Console.WriteLine($"{ModuleName}_{Serial}: 输入图像未加载");
                    resultStrings = new HTuple();
                    return false;
                }

                if (CodeType.IsInFirstN(4))
                {
                    // 一维码
                    ClearDeCodeHandle();
                    HTuple genParamNames, genParamValues;
                    CodeParamDefinitions_1D.ToHalconCodeParams(out genParamNames, out genParamValues);
                    HOperatorSet.CreateBarCodeModel(new HTuple(), new HTuple(), out HTuple handle);
                    DeCodeHandle = handle;
                    _is1DCodeModel = true;

                    HOperatorSet.SetBarCodeParam(handle, genParamNames, genParamValues);
                    HOperatorSet.FindBarCode(ho_Image, out HObject symbolRegions, handle, CodeType.GetDescription(), out HTuple foundDataStrings);

                    if (foundDataStrings.Length > 0)
                    {
                        ShowHRoi(new HRoi(Serial, ModuleName, foundDataStrings, HRoiType.检测范围, "green", symbolRegions, true, false));
                        resultStrings = foundDataStrings;
                        return true;
                    }
                    else
                    {
                        try { symbolRegions?.Dispose(); } catch { }
                        Console.WriteLine($"{ModuleName}_{Serial}: 未识别到一维码");
                        resultStrings = new HTuple();
                        return false;
                    }
                }
                else
                {
                    ClearDeCodeHandle();
                    HTuple genParamNames, genParamValues;
                    CodeParamDefinitions_2D.ToHalconCodeParams(CodeType, out genParamNames, out genParamValues);

                    HOperatorSet.CreateDataCode2dModel(CodeType.GetDescription(), new HTuple(), new HTuple(), out HTuple handle);
                    DeCodeHandle = handle;
                    _is1DCodeModel = false;

                    if (genParamNames.Length > 0)
                        HOperatorSet.SetDataCode2dParam(handle, genParamNames, genParamValues);
                    HOperatorSet.FindDataCode2d(ho_Image, out HObject symbolRegions, handle, new HTuple(), new HTuple(),
                        out HTuple foundHandles, out HTuple foundDataStrings);

                    if (foundDataStrings.Length > 0)
                    {
                        ShowHRoi(new HRoi(Serial, ModuleName, foundDataStrings, HRoiType.检测范围, "green", symbolRegions, true, false));
                        resultStrings = foundDataStrings;
                        return true;
                    }
                    else
                    {
                        try { symbolRegions?.Dispose(); } catch { }
                        Console.WriteLine($"{ModuleName}_{Serial}: 未识别到二维码");
                        resultStrings = new HTuple();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _lastFindCodeFailedByException = true;
                Console.WriteLine($"{ModuleName}_{Serial}: 读码异常 - {ex.Message}");
                resultStrings = new HTuple();
                return false;
            }
        }

        /// <summary>
        /// 将 HALCON 返回的多条码内容转换为多行字符串，保持现有单字符串输出契约。
        /// </summary>
        /// <param name="decodedStrings">HALCON 读码结果 HTuple，可包含多条码内容。</param>
        /// <returns>按换行拼接后的识别内容；无结果时返回空字符串。</returns>
        public static string FormatDecodedStrings(HTuple decodedStrings)
        {
            if (decodedStrings == null || decodedStrings.Length == 0)
                return string.Empty;

            var values = new List<string>();
            for (int i = 0; i < decodedStrings.Length; i++)
            {
                values.Add(decodedStrings.TupleSelect(i).S);
            }

            return string.Join(Environment.NewLine, values);
        }
        public void ShowHRoi()
        {
            if (mWindowH == null)
                return;

            mWindowH.ClearROI();

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(
                        mWindowH.hControl.HalconWindow,
                        roiText.size,
                        "false",
                        "false"
                    );
                    ShowTool.SetMsg(
                        mWindowH.hControl.HalconWindow,
                        roiText.text,
                        "image",
                        roiText.row,
                        roiText.col,
                        roiText.drawColor,
                        "false"
                    );
                }
                else
                {
                    mWindowH.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
                }
            }
        }

        public void ShowHRoi(HRoi ROI)
        {
            try
            {
                int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                if (ROI.fors == true)
                {
                    mHRoi.Add(ROI);
                    return;
                }
                if (index > -1)
                    mHRoi[index] = ROI;
                else
                    mHRoi.Add(ROI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"扫码 ROI 缓存更新失败: {ex.Message}");
            }
        }

        #endregion

        #region 输出参数刷新
        private NodeStatus CompleteWithoutResult(NodeStatus status)
        {
            OutputStrings.Clear();
            ClearPreviewRoisByImage();
            _previewUsesExecutionResults = false;
            RaisePropertyChanged(nameof(OutputStrings));
            ClearMHRoi();
            RefreshPreviewDisplay(includeRoi: false);
            RefreshOutputParams();
            return status;
        }

        private void RefreshOutputParams()
        {
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (item.Resourece == ResoureceType.Inupt)
                    continue;

                var key = !string.IsNullOrWhiteSpace(item.ParamName)
                    ? item.ParamName
                    : item.Name;

                if (!string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out var value))
                {
                    // 克隆 HALCON 对象，避免把内部缓存直接暴露给下游。
                    if (value is HObject hObj && hObj.IsInitialized())
                        item.Value = hObj.Clone();
                    else if (value is List<string> stringList)
                        item.Value = stringList.ToList();
                    else
                        item.Value = value;
                }
            }

            if (!UpdateParam())
                Console.WriteLine($"扫码识别_{Serial} 输出参数刷新失败");
        }
        #endregion

        #region 预览显示
        /// <summary>
        /// 替换模块自有输入图像副本，兼容上游传入的 HImage 和 HObject。
        /// </summary>
        /// <param name="imageValue">上游输入链接中的借用图像对象。</param>
        private void ReplaceOwnedInputImages(object? imageValue)
        {
            ClearOwnedInputImages();
            try
            {
                if (imageValue is HObject hObj && hObj.IsInitialized())
                    CopyInputImagesFromObject(hObj);
            }
            catch
            {
                ClearOwnedInputImages();
            }

            CurrentPreviewImageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
            RaisePreviewImageNavigationProperties();
        }

        private void CopyInputImagesFromObject(HObject imageObject)
        {
            HOperatorSet.CountObj(imageObject, out HTuple imageCountTuple);
            int imageCount = imageCountTuple.I;
            for (int index = 1; index <= imageCount; index++)
            {
                HObject? selectedImage = null;
                try
                {
                    HOperatorSet.SelectObj(imageObject, out selectedImage, index);
                    using (var tempImage = new HImage(selectedImage))
                    {
                        _ownedInputImages.Add(tempImage.CopyImage());
                    }
                }
                finally
                {
                    try { selectedImage?.Dispose(); } catch { }
                }
            }
        }

        private void ClearOwnedInputImages()
        {
            foreach (var image in _ownedInputImages)
            {
                try { image.Dispose(); } catch { }
            }
            _ownedInputImages.Clear();
        }

        /// <summary>
        /// 按偏移切换当前预览图像，并同步刷新对应的识别区域和左上角文本。
        /// </summary>
        /// <param name="offset">相对当前图像的偏移量，-1 表示上一张，1 表示下一张。</param>
        public void MovePreviewImage(int offset)
        {
            SelectPreviewImage(CurrentPreviewImageIndex + offset);
        }

        /// <summary>
        /// 选择指定序号的预览图像，超出范围时自动限制到有效范围内。
        /// </summary>
        /// <param name="imageIndex">目标图像序号，从 0 开始。</param>
        public void SelectPreviewImage(int imageIndex)
        {
            CurrentPreviewImageIndex = NormalizePreviewImageIndex(imageIndex);
            RefreshPreviewDisplay(_previewUsesExecutionResults);
        }

        private HImage? GetPreviewImage(int imageIndex)
        {
            int normalizedIndex = NormalizePreviewImageIndex(imageIndex);
            return normalizedIndex < _ownedInputImages.Count
                ? _ownedInputImages[normalizedIndex]
                : null;
        }

        private string GetPreviewResultLabel(int imageIndex)
        {
            int normalizedIndex = NormalizePreviewImageIndex(imageIndex);
            return OutputStrings != null && normalizedIndex < OutputStrings.Count
                ? OutputStrings[normalizedIndex]
                : string.Empty;
        }

        private int NormalizePreviewImageIndex(int imageIndex)
        {
            int imageCount = PreviewImageCount;
            if (imageCount <= 0)
                return 0;

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
        /// 清理识别区域缓存中的 HALCON 句柄。
        /// </summary>
        private void ClearMHRoi()
        {
            foreach (var roi in mHRoi)
            {
                try { roi.hobject?.Dispose(); } catch { }
            }
            mHRoi.Clear();
        }

        private List<HRoi> CopyCurrentRois()
        {
            var copies = new List<HRoi>();
            foreach (var roi in mHRoi)
            {
                if (roi.hobject == null || !roi.hobject.IsInitialized())
                    continue;

                try
                {
                    copies.Add(new HRoi(
                        roi.ModuleEncode,
                        roi.ModuleName,
                        roi.Remarks,
                        roi.roiType,
                        roi.drawColor,
                        roi.hobject.Clone(),
                        roi.fors,
                        roi.IsFillDisp));
                }
                catch { }
            }

            return copies;
        }

        private void ClearPreviewRoisByImage()
        {
            foreach (var roiList in _previewRoisByImage)
            {
                foreach (var roi in roiList)
                {
                    try { roi.hobject?.Dispose(); } catch { }
                }
            }

            _previewRoisByImage.Clear();
        }

        /// <summary>
        /// 复制当前图像和识别区域后刷新 WPF 预览，跨线程时切回 UI 线程。
        /// </summary>
        /// <param name="includeRoi">是否同步本次识别区域覆盖层；失败路径只刷新图像并清空覆盖层。</param>
        private void RefreshPreviewDisplay(bool includeRoi = true)
        {
            var imageSnapshot = CreatePreviewImageSnapshot();
            var drawObjectSnapshots = includeRoi ? CreatePreviewDrawObjectSnapshots() : new List<HalconDrawingObject>();
            var labelSnapshots = includeRoi ? CreatePreviewResultLabels() : new List<string>();
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                try
                {
                    dispatcher.BeginInvoke(new Action(() => ApplyPreviewDisplay(imageSnapshot, drawObjectSnapshots, labelSnapshots)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫码识别_{Serial} 预览刷新调度失败: {ex.Message}");
                    DisposeHObject(imageSnapshot);
                    DisposeDrawingObjects(drawObjectSnapshots);
                }
                return;
            }

            ApplyPreviewDisplay(imageSnapshot, drawObjectSnapshots, labelSnapshots);
        }

        private HObject? CreatePreviewImageSnapshot()
        {
            try
            {
                var previewImage = GetPreviewImage(CurrentPreviewImageIndex);
                if (previewImage != null && previewImage.IsInitialized())
                    return previewImage.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫码识别_{Serial} 预览图像复制失败: {ex.Message}");
            }

            return null;
        }

        private List<HalconDrawingObject> CreatePreviewDrawObjectSnapshots()
        {
            var snapshots = new List<HalconDrawingObject>();
            int imageIndex = NormalizePreviewImageIndex(CurrentPreviewImageIndex);
            var roiList = imageIndex < _previewRoisByImage.Count
                ? _previewRoisByImage[imageIndex]
                : mHRoi;

            foreach (var roi in roiList.ToList())
            {
                if (roi.hobject == null || !roi.hobject.IsInitialized())
                    continue;

                try
                {
                    snapshots.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = roi.hobject.Clone(),
                        Color = roi.drawColor,
                        IsFillDisplay = roi.IsFillDisp
                    });
                }
                catch { }
            }

            return snapshots;
        }

        private List<string> CreatePreviewResultLabels()
        {
            string label = GetPreviewResultLabel(CurrentPreviewImageIndex);
            return string.IsNullOrWhiteSpace(label)
                ? new List<string>()
                : new List<string> { label };
        }

        private void ApplyPreviewDisplay(HObject? imageSnapshot, List<HalconDrawingObject> drawObjectSnapshots, List<string> labelSnapshots)
        {
            HObject? unappliedImage = imageSnapshot;
            List<HalconDrawingObject>? unappliedDrawObjects = drawObjectSnapshots;

            try
            {
                var oldPreview = _previewImageObject;
                PreviewImageObject = imageSnapshot;
                unappliedImage = null;

                if (oldPreview != null && !ReferenceEquals(oldPreview, _previewImageObject))
                    DisposeHObject(oldPreview);

                ClearPreviewDrawObjects();
                ClearPreviewResultLabels();
                foreach (var drawObject in drawObjectSnapshots)
                {
                    PreviewDrawObjects.Add(drawObject);
                }
                foreach (var label in labelSnapshots)
                {
                    PreviewResultLabels.Add(label);
                }

                unappliedDrawObjects = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫码识别_{Serial} 应用预览刷新失败: {ex.Message}");
                ClearPreviewDrawObjects();
                ClearPreviewResultLabels();
            }
            finally
            {
                DisposeHObject(unappliedImage);
                DisposeDrawingObjects(unappliedDrawObjects);
            }
        }

        /// <summary>清除预览覆盖层对象</summary>
        private void ClearPreviewDrawObjects()
        {
            foreach (var obj in PreviewDrawObjects)
            {
                try { obj.Hobject?.Dispose(); } catch { }
            }
            PreviewDrawObjects.Clear();
        }

        private void ClearPreviewResultLabels()
        {
            PreviewResultLabels.Clear();
        }

        private static void DisposeDrawingObjects(IEnumerable<HalconDrawingObject>? objects)
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                DisposeHObject(obj?.Hobject);
            }
        }

        private static void DisposeHObject(HObject? hObject)
        {
            if (hObject == null)
                return;

            try { hObject.Dispose(); } catch { }
        }

        /// <summary>释放运行时持有的 HALCON 对象</summary>
        private void DisposeOwnedRuntimeObjects()
        {
            ClearPreviewDrawObjects();
            ClearPreviewResultLabels();
            ClearPreviewRoisByImage();
            if (_previewImageObject != null)
            {
                try { _previewImageObject.Dispose(); } catch { }
                _previewImageObject = null;
            }
            ClearOwnedInputImages();
        }
        #endregion
    }

    public static class FindCodeExtensions
    {
        public static void ToHalconCodeParams(this ObservableCollection<ParamDefinition> defs,
                                         CodeType codeType,
                                         out HTuple genParamNames,
                                         out HTuple genParamValues,
                                         IEnumerable<int>? indices = null)
        {
            genParamNames = new HTuple();
            genParamValues = new HTuple();

            var filteredDefs = (indices == null || !indices.Any())
                ? defs.ToList()
                : defs.Where((d, i) => indices.Contains(i)).ToList();

            foreach (var def in filteredDefs)
            {
                string halconName = def.Name switch
                {
                    "条码最小尺寸:" => "element_size_min",
                    "条码最大尺寸:" => "element_size_max",
                    "模型持久化等级:" => "persistence",
                    "校验位验证:" => "check_char",
                    "增加扫描线数:" => "num_scanlines",
                    "容许误差值:" => "start_stop_tolerance",
                    "条码最小高度:" => "element_height_min",
                    "条码方向:" => "orientation",
                    "条码方向容差:" => "orientation_tol",
                    "解码个数:" => "stop_after_result_num",
                    "最小对比度阈值:" => "contrast_min",
                    "码与背景的极性:" => "polarity",
                    "小模块抗噪能力:" => "small_modules_robustness",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(halconName))
                {
                    if (ShouldSkipDataCode2dParam(codeType, halconName))
                        continue;

                    genParamNames.Append(halconName);

                    switch (def.ValueType)
                    {
                        case ParamValueType.Int:
                            genParamValues.Append(Convert.ToInt32(def.Value));
                            break;

                        case ParamValueType.Double:
                            genParamValues.Append(Convert.ToDouble(def.Value));
                            break;

                        case ParamValueType.StringInt:
                            if (int.TryParse(def.Value.ToString(), out int intValue))
                            {
                                genParamValues.Append(Convert.ToInt32(intValue));
                            }
                            else
                            {
                                genParamValues.Append(def.Value.ToString());
                            }
                            break;

                        case ParamValueType.StringDouble:
                            if (double.TryParse(def.Value.ToString(), out double doubleValue))
                            {
                                genParamValues.Append(Convert.ToDouble(doubleValue));
                            }
                            else
                            {
                                genParamValues.Append(def.Value.ToString());
                            }
                            break;

                        case ParamValueType.String:
                            genParamValues.Append(def.Value.ToString());
                            break;
                    }
                }
            }
        }

        public static void ToHalconCodeParams(this ObservableCollection<ParamDefinition> defs,
                                         out HTuple genParamNames,
                                         out HTuple genParamValues,
                                         IEnumerable<int>? indices = null)
        {
            defs.ToHalconCodeParams(CodeType.Auto_1D, out genParamNames, out genParamValues, indices);
        }

        private static bool ShouldSkipDataCode2dParam(CodeType codeType, string halconName)
        {
            if (halconName != "contrast_min")
                return false;

            // Data Matrix 不支持 contrast_min，传入 set_data_code_2d_param 会触发 HALCON 异常。
            switch (codeType)
            {
                case CodeType.Data_Matrix_ECC_200_2D:
                case CodeType.GS1_DataMatrix:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断枚举值是否在前 N 个中
        /// </summary>
        public static bool IsInFirstN<TEnum>(this TEnum value, int n) where TEnum : Enum
        {
            int intValue = Convert.ToInt32(value);
            return intValue >= 0 && intValue < n;
        }

        /// <summary>
        /// 判断枚举值是否在后 N 个中
        /// </summary>
        public static bool IsInLastN<TEnum>(this TEnum value, int n) where TEnum : Enum
        {
            var values = Enum.GetValues(typeof(TEnum));
            int intValue = Convert.ToInt32(value);
            return intValue >= values.Length - n && intValue < values.Length;
        }

        /// <summary>
        /// 获取枚举值的 Description
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var field = type.GetField(value.ToString());
            if (field != null)
            {
                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                return attr != null ? attr.Description : value.ToString();
            }
            return value.ToString();
        }
    }
}

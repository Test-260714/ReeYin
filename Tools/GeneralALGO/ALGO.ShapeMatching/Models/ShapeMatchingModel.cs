#nullable disable
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.ShapeMatching
{
    [Serializable]
    public class ShapeMatchingModel : ModelParamBase
    {
        #region 字段与状态
        [JsonIgnore]
        public override VMHWindowControl mWindowH { get; set; }

        // 可编辑 ROI 生成的模板学习区域，由 CreateAndSaveShapeModel 使用
        [JsonIgnore]
        private HObject _drawnRoiRegion;

        // 当前可编辑 ROI 的图像坐标状态，由预览控件拖拽更新
        [JsonIgnore]
        private ShapeMatchingRoiPreview? _editableRoiPreview;

        // 运行时拥有的输入图像副本
        [JsonIgnore]
        private HImage _ownedInputImage;

        // 当前 ModelID 的模板类型（true=NccModel，false=ShapeModel）
        [JsonIgnore]
        private bool _isNccModel;

        private static readonly RecipeRepository RecipeRepository = new();
        #endregion

        #region 界面绑定属性

        [JsonIgnore]
        private string _currentshapeFilePath = "";
        /// <summary>
        /// 当前模板文件完整路径，用于项目重开或配方切换后重新加载 HALCON 模型。
        /// </summary>
        [RecipeParam("模板文件路径", "形状匹配或 NCC 模板文件的完整路径")]
        public string CurrentShapeFilePath
        {
            get { return _currentshapeFilePath; }
            set { _currentshapeFilePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _currentshapeFile = "";
        /// <summary>
        /// 当前模板文件名，仅用于界面显示和判断模板扩展名。
        /// </summary>
        [RecipeParam("模板文件名", "形状匹配或 NCC 模板文件名")]
        public string CurrentShapeFile
        {
            get { return _currentshapeFile; }
            set { _currentshapeFile = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<ParamDefinition> _shapeParamDefinitions;
        /// <summary>
        /// 形状模板创建和查找使用的 HALCON 参数集合。
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        [RecipeParam("形状匹配参数", "形状模板创建和查找使用的 HALCON 参数集合")]
        public ObservableCollection<ParamDefinition> ShapeParamDefinitions
        {
            get { return _shapeParamDefinitions; }
            set { SetProperty(ref _shapeParamDefinitions, value); }
        }

        [JsonIgnore]
        private ObservableCollection<ParamDefinition> _nccParamDefinitions;
        /// <summary>
        /// NCC 相关性模板创建和查找使用的 HALCON 参数集合。
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        [RecipeParam("相关性匹配参数", "NCC 相关性模板创建和查找使用的 HALCON 参数集合")]
        public ObservableCollection<ParamDefinition> NccParamDefinitions
        {
            get { return _nccParamDefinitions; }
            set { SetProperty(ref _nccParamDefinitions, value); }
        }

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

        [JsonIgnore]
        private TransmitParam _inputRegion = new TransmitParam();
        /// <summary>
        /// 输入区域参数
        /// </summary>
        [InputParam]
        public TransmitParam InputRegion
        {
            get { return _inputRegion; }
            set
            {
                _inputRegion = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private HObject _initRegion = new HObject();
        public HObject InitRegion
        {
            get { return _initRegion; }
            set { SetProperty(ref _initRegion, value); }
        }

        /// <summary> 区域列表 </summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private HRegion[] _outputRegions = null;
        /// <summary>
        /// 输出匹配结果
        /// </summary>
        [OutputParam("OutputRegions", "匹配区域")]
        [JsonIgnore]
        public HRegion[] OutputRegions
        {
            get { return _outputRegions; }
            set { SetProperty(ref _outputRegions, value); }
        }

        [JsonIgnore]
        private HTuple _modelID = "";
        public HTuple ModelID
        {
            get { return _modelID; }
            set { _modelID = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HObject _shapemodelContours = new HObject();
        public HObject ShapeModelContours
        {
            get { return _shapemodelContours; }
            set { _shapemodelContours = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HObject _nccmodelContours = new HObject();
        public HObject NccModelContours
        {
            get { return _nccmodelContours; }
            set { _nccmodelContours = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ShapeMatchingMode _shapeMatchingMode = ShapeMatchingMode.形状匹配;
        /// <summary>
        /// 当前匹配算法类型，决定读取/创建/查找形状模板或 NCC 模板。
        /// </summary>
        [RecipeParam("匹配方式", "形状匹配或相关性匹配")]
        public ShapeMatchingMode ShapeMatchingMode
        {
            get { return _shapeMatchingMode; }
            set { SetProperty(ref _shapeMatchingMode, value, new Action(() => { REnum.EnumToStr(_shapeMatchingMode); })); }
        }

        [JsonIgnore]
        private RegionCreatMode _regionCreatMode = RegionCreatMode.链接输入;
        /// <summary>
        /// 模板学习区域来源，支持上游区域或预览窗口绘制。
        /// </summary>
        [RecipeParam("区域获取方式", "模板学习区域的来源方式")]
        public RegionCreatMode RegionCreatMode
        {
            get { return _regionCreatMode; }
            set
            {
                if (SetProperty(ref _regionCreatMode, value, new Action(() => { REnum.EnumToStr(_regionCreatMode); })))
                {
                    RefreshPreviewForRegionModeChange();
                }
            }
        }

        [JsonIgnore]
        private ObservableCollection<ShapeFileDefinition> _shapeFileDefinitions;
        public ObservableCollection<ShapeFileDefinition> ShapeFileDefinitions
        {
            get { return _shapeFileDefinitions; }
            set { SetProperty(ref _shapeFileDefinitions, value); }
        }

        [JsonIgnore]
        private HObject _previewImageObject;
        /// <summary>预览图像对象</summary>
        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get => _previewImageObject;
            private set { SetProperty(ref _previewImageObject, value); }
        }

        /// <summary>预览覆盖层绘制对象集合</summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new();

        #endregion

        #region 构造函数
        public ShapeMatchingModel()
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
                TriggerModuleRun = () => ExecuteModuleCore(refreshPreview: false);
                EnsureParamDefinitions();

                // ModelID 是运行时 HALCON 句柄，[JsonIgnore] 不会被持久化；
                // 项目重开后根据已恢复的 CurrentShapeFilePath 重新加载模板，避免图流程触发时 ModelID 为空。
                if (!string.IsNullOrEmpty(CurrentShapeFilePath) && File.Exists(CurrentShapeFilePath))
                {
                    try { LoadShapeModel(); }
                    catch (Exception ex)
                    {
                        LogError($"恢复模板失败，Path={CurrentShapeFilePath}", ex);
                    }
                }

                IsOnceInit = true;
                return true;
            }
            catch { return false; }
        }

        public override bool LoadKeyParam()
        {
            return LoadKeyParam(refreshPreview: true);
        }

        private bool LoadKeyParam(bool refreshPreview)
        {
            try
            {
                if (!base.LoadKeyParam()) return false;
                RefreshRuntimeInputObjects(refreshPreview);
                return true;
            }
            catch (Exception ex)
            {
                LogError("加载模板匹配参数异常", ex);
                return false;
            }
        }

        public bool RefreshRuntimeInputsFromCurrentLinks(bool refreshPreview = true)
        {
            try
            {
                if (!TransferParamSync()) return false;
                RefreshRuntimeInputObjects(refreshPreview);
                return true;
            }
            catch (Exception ex)
            {
                LogError("刷新模板匹配输入异常", ex);
                return false;
            }
        }

        private void RefreshRuntimeInputObjects(bool refreshPreview)
        {
            EnsureParamDefinitions();

            _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
            ReplaceOwnedInputImage(_inputImage?.Value);

            _inputRegion.Value = GetTransmitParam(InputParams, _inputRegion);
            ReplaceOwnedInputRegion(_inputRegion?.Value);

            if (refreshPreview)
                RefreshPreviewDisplay();
        }

        public bool PrepareForConfirm()
        {
            EnsureParamDefinitions();
            return SyncRecipeParams();
        }

        protected override bool SyncRecipeParams()
        {
            bool syncResult = base.SyncRecipeParams();
            if (syncResult && IsDebug && Serial >= 0)
            {
                SyncCurrentRecipeValuesFromRuntime();
            }

            return syncResult;
        }

        private void SyncCurrentRecipeValuesFromRuntime()
        {
            try
            {
                ProjectRecipeConfig config = RecipeRepository.Load();
                ProjectRecipeInfo currentRecipe = config.CurrentRecipe;
                if (currentRecipe == null || !currentRecipe.IsEnabled)
                    return;

                List<RecipeParamInfo> currentInfos = BuildCurrentRecipeParamInfos();
                if (currentInfos.Count == 0)
                    return;

                string subjection = ResolveRecipeSubjection(currentInfos);
                ProjectRecipeNodeGroup group = currentRecipe.GetOrCreateGroup(Serial, subjection);

                foreach (RecipeParamInfo info in currentInfos)
                {
                    if (!RecipeParamService.TryGetMarkedParamValue(this, info.Path, out object value))
                        continue;

                    string currentValue = RecipeValueConverter.SerializeValue(value, info.MemberType);
                    RecipeParamInfo existing = FindRecipeParameter(group, info);
                    if (existing == null)
                    {
                        RecipeParamInfo copy = info.CreateCopy();
                        copy.Id = Guid.NewGuid();
                        copy.Value = currentValue;
                        group.Parameters.Add(copy);
                        continue;
                    }

                    string preservedUnit = existing.Unit;
                    bool preservedIsRequired = existing.IsRequired;
                    string preservedOptions = existing.OptionsText;

                    existing.UpdateDefinition(info);
                    existing.Unit = string.IsNullOrWhiteSpace(preservedUnit) ? info.Unit : preservedUnit;
                    existing.IsRequired = preservedIsRequired;
                    existing.OptionsText = string.IsNullOrWhiteSpace(preservedOptions) ? info.OptionsText : preservedOptions;
                    existing.Value = currentValue;
                }

                RecipeRepository.Save(config);
            }
            catch (Exception ex)
            {
                LogError("同步当前配方模板匹配参数失败", ex);
            }
        }

        private List<RecipeParamInfo> BuildCurrentRecipeParamInfos()
        {
            List<RecipeParamInfo> infos = RecipeParamService.GetMarkedParams(this) ?? new List<RecipeParamInfo>();
            string subjection = ResolveRecipeSubjection(infos);

            foreach (RecipeParamInfo info in infos)
            {
                info.Serial = Serial;
                info.Subjection = subjection;
                info.RecipeKey = RecipeKeyHelper.Normalize(info.Serial, info.Path, info.RecipeKey);
            }

            return infos;
        }

        private string ResolveRecipeSubjection(IEnumerable<RecipeParamInfo> infos)
        {
            string subjection = infos?
                .Select(item => item?.Subjection)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

            if (!string.IsNullOrWhiteSpace(subjection))
                return subjection;

            if (!string.IsNullOrWhiteSpace(Name))
                return Name;

            return GetType().Name;
        }

        private static RecipeParamInfo FindRecipeParameter(ProjectRecipeNodeGroup group, RecipeParamInfo info)
        {
            if (group == null || info == null)
                return null;

            RecipeParamInfo existing = group.FindParameter(info.RecipeKey);
            if (existing != null)
                return existing;

            if (string.IsNullOrWhiteSpace(info.Path))
                return null;

            return group.Parameters.FirstOrDefault(item =>
                string.Equals(item.Path ?? string.Empty, info.Path, StringComparison.OrdinalIgnoreCase));
        }

        public override void Dispose()
        {
            ClearModelHandles();
            DisposeOwnedRuntimeObjects();
            ClearMHRoi();
            base.Dispose();
        }

        /// <summary>清理模板模型句柄和轮廓对象</summary>
        private void ClearModelHandles()
        {
            ClearTemplateModelHandle(ModelID, _isNccModel);
            ModelID = "";

            if (_shapemodelContours != null && _shapemodelContours.IsInitialized())
            {
                try { _shapemodelContours.Dispose(); } catch { }
                _shapemodelContours = new HObject();
            }
            if (_nccmodelContours != null && _nccmodelContours.IsInitialized())
            {
                try { _nccmodelContours.Dispose(); } catch { }
                _nccmodelContours = new HObject();
            }
        }

        private static void ClearTemplateModelHandle(HTuple modelID, bool isNccModel)
        {
            if (!HasTemplateModelHandle(modelID))
                return;

            try
            {
                if (isNccModel)
                    HOperatorSet.ClearNccModel(modelID);
                else
                    HOperatorSet.ClearShapeModel(modelID);
            }
            catch
            {
            }
        }

        private static void ClearTemporaryTemplateModel(HTuple modelID, bool isNccModel)
        {
            ClearTemplateModelHandle(modelID, isNccModel);
        }

        private static bool HasTemplateModelHandle(HTuple modelID)
        {
            if (modelID == null || modelID.Length <= 0)
                return false;

            try
            {
                return modelID.Type != HTupleType.STRING || !string.IsNullOrWhiteSpace(modelID.S);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 模块执行
        /// </summary>
        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            return ExecuteModule(refreshPreview: true);
        }

        private Task<ExecuteModuleOutput> ExecuteModule(bool refreshPreview)
        {
            return Task.Run(() => ExecuteModuleCore(refreshPreview));
        }

        private ExecuteModuleOutput ExecuteModuleCore(bool refreshPreview)
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam(refreshPreview))
                        return NodeStatus.Error;

                    ClearMHRoi();

                    if (_ownedInputImage == null || !_ownedInputImage.IsInitialized())
                        return NodeStatus.None;

                    bool status = FindShapeModel(_ownedInputImage, out HRegion[] matchedRegions);

                    if (!status)
                    {
                        return NodeStatus.Error;
                    }

                    bool hasMatchedRegions = HasMatchedRegions(matchedRegions);
                    var oldRegions = _outputRegions;
                    OutputRegions = CloneRegionArray(matchedRegions);
                    DisposeRegionArray(matchedRegions);
                    DisposeRegionArray(oldRegions);

                    if (refreshPreview)
                        RefreshRoiPreviewOverlays();

                    RefreshOutputParams();

                    if (!hasMatchedRegions)
                    {
                        LogWarning("模板匹配执行完成但未找到匹配区域");
                        return NodeStatus.None;
                    }

                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    LogError("模板匹配模块执行异常", ex);
                    return NodeStatus.Error;
                }
            });

            LogTrace($"模板匹配模块执行完成，Status={result}，ElapsedMs={time}");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public void InitializeParamDefinitions()
        {
            ShapeParamDefinitions =
            [
                new() { Name="金字塔层数:", UIType=ParamUIType.Number, Value=0, MinValue=0, MaxValue=10, ValueType = ParamValueType.Int, SmallChange=1, IsVisible=false },
                new() { Name="起始角度:", UIType=ParamUIType.Number, Value=0, MinValue=-3.14, MaxValue=6.28, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="角度范围:", UIType=ParamUIType.Number, Value=0.79, MinValue=0, MaxValue=6.28, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="最小尺度因子:", UIType=ParamUIType.Number, Value=0.8, MinValue=0.1, MaxValue=5, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="最大尺度因子:", UIType=ParamUIType.Number, Value=1.2, MinValue=0.1, MaxValue=5, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="角度步长:", UIType=ParamUIType.ComboBox, Value="auto", Options=["auto", "0.0175", "0.0349", "0.0524", "0.0698", "0.0873"], ValueType = ParamValueType.StringDouble, IsVisible=false },
                new() { Name="优化:", UIType=ParamUIType.ComboBox, Value="auto", Options=["auto", "no_pregeneration", "none", "point_reduction_high", "point_reduction_low", "point_reduction_medium", "pregeneration"], ValueType = ParamValueType.String, IsVisible=false },
                new() { Name="极性:", UIType=ParamUIType.ComboBox, Value="use_polarity", Options=["use_polarity", "ignore_color_polarity", "ignore_global_polarity", "ignore_local_polarity"], ValueType = ParamValueType.String },
                new() { Name="对比度:", UIType=ParamUIType.ComboBox, Value="auto", Options=["auto", "auto_contrast", "auto_contrast_hyst", "auto_min_size", "10", "20", "30", "40", "60", "80", "100", "120", "140", "160"], ValueType = ParamValueType.StringInt, IsVisible=false },
                new() { Name="最小对比度:", UIType=ParamUIType.ComboBox, Value="auto", Options=["auto", "1", "2", "3", "4", "5", "6", "7", "10", "20", "30", "40"], ValueType = ParamValueType.StringInt, IsVisible=false },
                new() { Name="最小分数:", UIType=ParamUIType.Number, Value=0.7, MinValue=0, MaxValue=1, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="匹配个数:", UIType=ParamUIType.Number, Value=1, MinValue=1, MaxValue=500, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="最大重叠度:", UIType=ParamUIType.Number, Value=0.3, MinValue=0, MaxValue=1, ValueType = ParamValueType.Double, SmallChange=0.1, IsVisible=false },
                new() { Name="贪婪数:", UIType=ParamUIType.Number, Value=0.8, MinValue=0, MaxValue=1, ValueType = ParamValueType.Double, SmallChange=0.1, IsVisible=false },
                new() { Name="是否采用亚像素匹配:", UIType=ParamUIType.ComboBox, Value="least_squares", Options=["none", "least_squares", "interpolation", "least_squares_high", "least_squares_very_high"], ValueType = ParamValueType.String, IsVisible=false }
            ];

            NccParamDefinitions =
            [
                new() { Name="金字塔层数:", UIType=ParamUIType.Number, Value=0, MinValue=0, MaxValue=10, ValueType = ParamValueType.Int, SmallChange=1, IsVisible=false },
                new() { Name="起始角度:", UIType=ParamUIType.Number, Value=0, MinValue=-3.14, MaxValue=6.28, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="角度范围:", UIType=ParamUIType.Number, Value=0.79, MinValue=0, MaxValue=6.28, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="角度步长:", UIType=ParamUIType.ComboBox, Value="auto", Options=["auto", "0.0175", "0.0349", "0.0524", "0.0698", "0.0873"], ValueType = ParamValueType.StringDouble, IsVisible=false },
                new() { Name="极性:", UIType=ParamUIType.ComboBox, Value="use_polarity", Options=["ignore_color_polarity", "ignore_global_polarity", "ignore_local_polarity", "use_polarity"], ValueType = ParamValueType.String },
                new() { Name="最小分数:", UIType=ParamUIType.Number, Value=0.7, MinValue=0, MaxValue=1, ValueType = ParamValueType.Double, SmallChange=0.1 },
                new() { Name="匹配个数:", UIType=ParamUIType.Number, Value=1, MinValue=1, MaxValue=500, ValueType = ParamValueType.Int, SmallChange=1 },
                new() { Name="最大重叠度:", UIType=ParamUIType.Number, Value=0.3, MinValue=0, MaxValue=1, ValueType = ParamValueType.Double, SmallChange=0.1, IsVisible=false },
                new() { Name="是否采用亚像素匹配:", UIType=ParamUIType.ComboBox, Value="true", Options=["true", "false"], ValueType = ParamValueType.String, IsVisible=false }
            ];

            RaisePropertyChanged(nameof(ShapeParamDefinitions));
            RaisePropertyChanged(nameof(NccParamDefinitions));
        }

        /// <summary>
        /// 确保模板匹配参数集合在未打开配置页或旧项目反序列化后仍可直接运行。
        /// </summary>
        public void EnsureParamDefinitions()
        {
            if (ShapeParamDefinitions == null || ShapeParamDefinitions.Count == 0 ||
                NccParamDefinitions == null || NccParamDefinitions.Count == 0)
            {
                InitializeParamDefinitions();
            }

            ShapeParamDefinitions = MergeParamDefinitionsWithCanonical(ShapeParamDefinitions, CreateDefaultShapeParamDefinitions());
            NccParamDefinitions = MergeParamDefinitionsWithCanonical(NccParamDefinitions, CreateDefaultNccParamDefinitions());
            NormalizeParamDefinition(ShapeParamDefinitions, "最大重叠度:", ParamValueType.Double, 0.1);
            NormalizeParamDefinition(NccParamDefinitions, "最大重叠度:", ParamValueType.Double, 0.1);
        }

        private static ObservableCollection<ParamDefinition> CreateDefaultShapeParamDefinitions()
        {
            var model = new ShapeMatchingModel(skipParamDefinitionInit: true);
            model.InitializeParamDefinitions();
            return model.ShapeParamDefinitions;
        }

        private static ObservableCollection<ParamDefinition> CreateDefaultNccParamDefinitions()
        {
            var model = new ShapeMatchingModel(skipParamDefinitionInit: true);
            model.InitializeParamDefinitions();
            return model.NccParamDefinitions;
        }

        private ShapeMatchingModel(bool skipParamDefinitionInit)
        {
        }

        private static ObservableCollection<ParamDefinition> MergeParamDefinitionsWithCanonical(
            ObservableCollection<ParamDefinition> current,
            ObservableCollection<ParamDefinition> canonical)
        {
            // Json.NET used to append saved values after constructor defaults; prefer the later item to keep user edits.
            var existingByName = current?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                .GroupBy(item => item.Name)
                .ToDictionary(group => group.Key, group => group.Last());

            bool applyRecommendedDefaults = NeedsParamDefinitionRefresh(current, canonical);
            foreach (var param in canonical)
            {
                if (existingByName?.TryGetValue(param.Name, out var existing) == true)
                {
                    CopyExistingParamValue(existing, param, applyRecommendedDefaults);
                }
            }

            return canonical;
        }

        private static bool NeedsParamDefinitionRefresh(
            ObservableCollection<ParamDefinition> current,
            ObservableCollection<ParamDefinition> canonical)
        {
            if (current == null || current.Count != canonical.Count)
                return true;

            for (int i = 0; i < canonical.Count; i++)
            {
                var currentParam = current[i];
                var canonicalParam = canonical[i];
                if (currentParam == null
                    || currentParam.Name != canonicalParam.Name
                    || currentParam.UIType != canonicalParam.UIType
                    || currentParam.ValueType != canonicalParam.ValueType
                    || Math.Abs(currentParam.MinValue - canonicalParam.MinValue) > double.Epsilon
                    || Math.Abs(currentParam.MaxValue - canonicalParam.MaxValue) > double.Epsilon
                    || Math.Abs(currentParam.SmallChange - canonicalParam.SmallChange) > double.Epsilon
                    || currentParam.IsVisible != canonicalParam.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopyExistingParamValue(
            ParamDefinition existing,
            ParamDefinition target,
            bool applyRecommendedDefaults)
        {
            if (existing == null)
                return;

            if (!applyRecommendedDefaults || !IsLegacyDefaultValue(target.Name, existing.Value))
            {
                target.Value = existing.Value;
            }
        }

        private static bool IsLegacyDefaultValue(string name, object value)
        {
            return name switch
            {
                "角度范围:" => IsNumericValue(value, 3.14),
                "最小尺度因子:" => IsNumericValue(value, 0.5),
                "最大尺度因子:" => IsNumericValue(value, 1.0),
                "极性:" => string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "ignore_color_polarity", StringComparison.Ordinal),
                "最小分数:" => IsNumericValue(value, 0.5),
                "匹配个数:" => IsNumericValue(value, 10) || IsNumericValue(value, 12),
                "最大重叠度:" => IsNumericValue(value, 0.5),
                "贪婪数:" => IsNumericValue(value, 0.9),
                _ => false
            };
        }

        private static bool IsNumericValue(object value, double expected)
        {
            try
            {
                return Math.Abs(Convert.ToDouble(value, CultureInfo.InvariantCulture) - expected) < 0.000001;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 修正旧项目中已经序列化的参数类型和步长，避免界面值与 HALCON 入参不一致。
        /// </summary>
        /// <param name="definitions">需要修正的参数集合。</param>
        /// <param name="name">参数中文名称。</param>
        /// <param name="valueType">写入 HTuple 时使用的目标类型。</param>
        /// <param name="smallChange">界面数值微调步长。</param>
        private static void NormalizeParamDefinition(
            ObservableCollection<ParamDefinition> definitions,
            string name,
            ParamValueType valueType,
            double smallChange)
        {
            var definition = definitions?.FirstOrDefault(item => item?.Name == name);
            if (definition == null)
                return;

            definition.ValueType = valueType;
            definition.SmallChange = smallChange;
        }

        public bool FindShapeModel(HObject ho_Image, out HRegion[] MatchedRegions)
        {
            HObject ho_MatchedRegions = null;
            try
            {
                EnsureParamDefinitions();
                if (!HasLoadedTemplateModel())
                {
                    LogWarning("请先加载有效的模板文件");
                    MatchedRegions = null;
                    return false;
                }

                var templateExtension = GetCurrentTemplateExtension();
                if (string.IsNullOrEmpty(templateExtension))
                {
                    LogWarning("请先加载模板文件");
                    MatchedRegions = null;
                    return false;
                }

                HOperatorSet.GenEmptyObj(out ho_MatchedRegions);
                HRegion[] TmpMatchedRegions = null;

                if (templateExtension.Equals(".shm", StringComparison.CurrentCultureIgnoreCase))
                {
                    HTuple ShapeParams = ShapeParamDefinitions.ToHTuple();

                    HOperatorSet.FindScaledShapeModel(
                        ho_Image,
                        ModelID,
                        ShapeParams[1],
                        ShapeParams[2],
                        ShapeParams[3],
                        ShapeParams[4],
                        ShapeParams[10],
                        ShapeParams[11],
                        ShapeParams[12],
                        ShapeParams[14],
                        ShapeParams[0],
                        ShapeParams[13],
                        out HTuple hv_Row,
                        out HTuple hv_Column,
                        out HTuple hv_Angle,
                        out HTuple hv_Scale,
                        out HTuple hv_Score
                        );

                    TmpMatchedRegions = new HRegion[hv_Row.Length];
                    for (int i = 0; i < hv_Row.Length; i++)
                    {
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, hv_Row[i], hv_Column[i], hv_Angle[i], out HTuple hv_HomMat2D);
                        HOperatorSet.HomMat2dScale(hv_HomMat2D, hv_Scale[i], hv_Scale[i], 0, 0, out hv_HomMat2D);
                        HOperatorSet.AffineTransContourXld(ShapeModelContours, out HObject tempMatchedXLD, hv_HomMat2D);
                        HOperatorSet.GenRegionContourXld(tempMatchedXLD, out HObject tempMatchedRegion, "filled");
                        // 拼接 Region 用于 ShowHRoi 显示
                        var oldMatchedRegions = ho_MatchedRegions;
                        HOperatorSet.ConcatObj(tempMatchedRegion, oldMatchedRegions, out ho_MatchedRegions);
                        try { oldMatchedRegions?.Dispose(); } catch { }
                        try { tempMatchedXLD?.Dispose(); } catch { }
                        // new HRegion(HObject) 与 tempMatchedRegion 共享 HALCON 句柄，不能提前 Dispose
                        TmpMatchedRegions[i] = new HRegion(tempMatchedRegion);
                    }

                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "green", HalconImageOwnership.CopyBorrowedObjectOrNull(ho_MatchedRegions), true, true));
                }
                else if (templateExtension.Equals(".ncm", StringComparison.CurrentCultureIgnoreCase))
                {
                    HTuple NccParams = NccParamDefinitions.ToHTuple();

                    // 彩色图转灰度时新生成的图像是自有对象，需在匹配完成后释放；
                    // 不能用 ho_Image 直接覆盖，否则丢失对该自有句柄的引用
                    HOperatorSet.CountChannels(ho_Image, out HTuple reChannels);
                    HObject grayImage = null;
                    HObject nccImage = ho_Image;
                    if (reChannels == 3)
                    {
                        HOperatorSet.Rgb1ToGray(ho_Image, out grayImage);
                        nccImage = grayImage;
                    }

                    HOperatorSet.FindNccModel(
                        nccImage,
                        ModelID,
                        NccParams[1],
                        NccParams[2],
                        NccParams[5],
                        NccParams[6],
                        NccParams[7],
                        NccParams[8],
                        NccParams[0],
                        out HTuple hv_Row,
                        out HTuple hv_Column,
                        out HTuple hv_Angle,
                        out HTuple hv_Score
                        );

                    TmpMatchedRegions = new HRegion[hv_Row.Length];
                    for (int i = 0; i < hv_Row.Length; i++)
                    {
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, hv_Row[i], hv_Column[i], hv_Angle[i], out HTuple hv_HomMat2D);
                        HOperatorSet.GetNccModelRegion(out HObject tmpModelContours, ModelID);
                        HOperatorSet.AffineTransRegion(tmpModelContours, out HObject tempMatchedRegion, hv_HomMat2D, "false");
                        try { tmpModelContours?.Dispose(); } catch { }
                        var oldMatchedRegions = ho_MatchedRegions;
                        HOperatorSet.ConcatObj(tempMatchedRegion, oldMatchedRegions, out ho_MatchedRegions);
                        try { oldMatchedRegions?.Dispose(); } catch { }
                        // new HRegion(HObject) 与 tempMatchedRegion 共享 HALCON 句柄，不能提前 Dispose
                        TmpMatchedRegions[i] = new HRegion(tempMatchedRegion);
                    }

                    try { grayImage?.Dispose(); } catch { }

                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "green", HalconImageOwnership.CopyBorrowedObjectOrNull(ho_MatchedRegions), true, true));
                }

                MatchedRegions = TmpMatchedRegions;
                return true;
            }
            catch (Exception ex)
            {
                MatchedRegions = null;
                LogError("模板匹配查找失败", ex);
                return false;
            }
            finally
            {
                HalconImageOwnership.DisposeOwned(ho_MatchedRegions);
            }
        }

        /// <summary>
        /// 预览模板文件
        /// </summary>
        public bool LoadShapeModel()
        {
            return LoadShapeModel(CurrentShapeFilePath);
        }

        public bool LoadShapeModel(string shapeFilePath)
        {
            if (string.IsNullOrWhiteSpace(shapeFilePath))
                return false;
            if (!File.Exists(shapeFilePath))
                return WarnTemplateFileUnavailable(shapeFilePath);
            if (!TryReadTemplateModel(shapeFilePath, out HTuple modelID, out HObject modelContours, out bool isNccModel, out ShapeMatchingMode matchingMode))
                return false;

            ReplaceLoadedTemplateModel(shapeFilePath, modelID, modelContours, isNccModel, matchingMode);
            ShowLoadedTemplatePreview();
            return true;
        }

        private bool WarnTemplateFileUnavailable(string shapeFilePath)
        {
            LogWarning($"模板文件不存在或不可访问，Path={shapeFilePath}");
            return false;
        }

        private bool TryReadTemplateModel(
            string shapeFilePath,
            out HTuple modelID,
            out HObject modelContours,
            out bool isNccModel,
            out ShapeMatchingMode matchingMode)
        {
            modelID = null;
            modelContours = null;
            isNccModel = false;
            matchingMode = ShapeMatchingMode.形状匹配;

            string templateExtension = Path.GetExtension(shapeFilePath);
            try
            {
                if (templateExtension.Equals(".shm", StringComparison.CurrentCultureIgnoreCase))
                {
                    HOperatorSet.ReadShapeModel(shapeFilePath, out modelID);
                    HOperatorSet.GetShapeModelContours(out modelContours, modelID, 1);
                    matchingMode = ShapeMatchingMode.形状匹配;
                    return true;
                }

                if (templateExtension.Equals(".ncm", StringComparison.CurrentCultureIgnoreCase))
                {
                    isNccModel = true;
                    HOperatorSet.ReadNccModel(shapeFilePath, out modelID);
                    HOperatorSet.GetNccModelRegion(out modelContours, modelID);
                    matchingMode = ShapeMatchingMode.相关性匹配;
                    return true;
                }

                LogWarning($"不支持的模板文件类型，Path={shapeFilePath}");
                return false;
            }
            catch (Exception ex)
            {
                ClearTemporaryTemplateModel(modelID, isNccModel);
                DisposeHObject(modelContours);
                modelID = null;
                modelContours = null;
                LogError($"加载模板失败，Path={shapeFilePath}", ex);
                return false;
            }
        }

        private void ReplaceLoadedTemplateModel(
            string shapeFilePath,
            HTuple modelID,
            HObject modelContours,
            bool isNccModel,
            ShapeMatchingMode matchingMode)
        {
            ClearModelHandles();
            ModelID = modelID;
            _isNccModel = isNccModel;
            ShapeMatchingMode = matchingMode;
            CurrentShapeFilePath = shapeFilePath;
            CurrentShapeFile = Path.GetFileName(shapeFilePath);

            if (isNccModel)
                NccModelContours = modelContours;
            else
                ShapeModelContours = modelContours;
        }

        private void ShowLoadedTemplatePreview()
        {
            var contours = _isNccModel ? NccModelContours : ShapeModelContours;
            if (contours == null || !contours.IsInitialized())
                return;

            ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "red", HalconImageOwnership.CopyBorrowedObjectOrNull(contours), true, true));
            ShowHRoi();
        }

        /// <summary>
        /// 区域获取方式切换时直接刷新预览窗口中的可编辑 ROI，不再依赖额外按钮触发。
        /// </summary>
        private void RefreshPreviewForRegionModeChange()
        {
            try
            {
                var dispatcher = PrismProvider.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke(new Action(RefreshPreviewForRegionModeChange));
                    return;
                }

                if (RegionCreatMode == RegionCreatMode.链接输入)
                {
                    _editableRoiPreview = null;
                    RefreshRoiPreviewOverlays();
                    return;
                }

                EnsureEditableRoiInitialized(forceModeRefresh: true);
                UpdateDrawnRoiFromPreview();
                RefreshEditableRoiPreview();
                LogTrace($"已切换可编辑 ROI 预览模式，Mode={RegionCreatMode}");
            }
            catch (Exception ex)
            {
                LogError("切换 ROI 预览模式失败", ex);
            }
        }

        /// <summary>
        /// 创建并保存模板文件（WPF 模式：使用预览 ROI 存储的区域，不再依赖 mWindowH）
        /// </summary>
        public void CreateAndSaveShapeModel()
        {
            // 确保 _ownedInputImage/_initRegion 是用户当前选择的最新状态，但不重新套用旧配方值。
            if (!RefreshRuntimeInputsFromCurrentLinks(refreshPreview: true))
                return;

            if (_ownedInputImage == null || !_ownedInputImage.IsInitialized())
            {
                LogWarning("创建模板时图像为空，请先链接输入图像");
                return;
            }

            if (RegionCreatMode != RegionCreatMode.链接输入 && (_drawnRoiRegion == null || !_drawnRoiRegion.IsInitialized()))
            {
                LogWarning("请先在预览窗口确认有效 ROI 区域");
                return;
            }

            if (RegionCreatMode == RegionCreatMode.链接输入 && (_initRegion == null || !_initRegion.IsInitialized()))
            {
                LogWarning("链接输入模式需要先链接输入区域");
                return;
            }

            HObject ShapeRegionHObject = null;
            HObject ImageReduced = null;
            HTuple createdModelID = null;
            bool createdIsNccModel = false;
            string savedFilePath = string.Empty;

            if (RegionCreatMode == RegionCreatMode.绘制旋转矩形
                || RegionCreatMode == RegionCreatMode.绘制矩形
                || RegionCreatMode == RegionCreatMode.绘制圆形)
                ShapeRegionHObject = _drawnRoiRegion.Clone();
            else if (RegionCreatMode == RegionCreatMode.链接输入)
                ShapeRegionHObject = InitRegion.DeepClone();

            try
            {
                HOperatorSet.ReduceDomain(_ownedInputImage, ShapeRegionHObject, out ImageReduced);
                if (ShapeMatchingMode == ShapeMatchingMode.形状匹配)
                {
                    HOperatorSet.CreateScaledShapeModel(
                        ImageReduced,
                        new HTuple(ShapeParamDefinitions[0].Value),
                        new HTuple(ShapeParamDefinitions[1].Value),
                        new HTuple(ShapeParamDefinitions[2].Value),
                        new HTuple(ShapeParamDefinitions[5].Value),
                        new HTuple(ShapeParamDefinitions[3].Value),
                        new HTuple(ShapeParamDefinitions[4].Value),
                        new HTuple("auto"),
                        new HTuple(ShapeParamDefinitions[6].Value),
                        new HTuple(ShapeParamDefinitions[7].Value),
                        new HTuple(ShapeParamDefinitions[8].Value),
                        new HTuple(ShapeParamDefinitions[9].Value),
                        out createdModelID);

                    var saveFileDialog = CreateTemplateSaveFileDialog("保存形状模板", "Shape Model (*.shm)|*.shm", ".shm");
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        HOperatorSet.WriteShapeModel(createdModelID, saveFileDialog.FileName);
                        savedFilePath = saveFileDialog.FileName;
                    }
                }
                else if (ShapeMatchingMode == ShapeMatchingMode.相关性匹配)
                {
                    createdIsNccModel = true;
                    HOperatorSet.CountChannels(ImageReduced, out HTuple reChannels);
                    if (reChannels == 3)
                    {
                        HOperatorSet.Rgb1ToGray(ImageReduced, out HObject grayReduced);
                        ImageReduced.Dispose();
                        ImageReduced = grayReduced;
                    }

                    HOperatorSet.CreateNccModel(
                        ImageReduced,
                        new HTuple(NccParamDefinitions[0].Value),
                        new HTuple(NccParamDefinitions[1].Value),
                        new HTuple(NccParamDefinitions[2].Value),
                        new HTuple(NccParamDefinitions[3].Value),
                        new HTuple(NccParamDefinitions[4].Value),
                        out createdModelID);

                    var saveFileDialog = CreateTemplateSaveFileDialog("保存相似性模板", "Ncc Model (*.ncm)|*.ncm", ".ncm");
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        HOperatorSet.WriteNccModel(createdModelID, saveFileDialog.FileName);
                        savedFilePath = saveFileDialog.FileName;
                    }
                }

                if (!string.IsNullOrEmpty(savedFilePath))
                    LoadShapeModel(savedFilePath);
                else
                    LogTrace("用户取消保存模板，保持当前已加载模板不变");
            }
            finally
            {
                ClearTemporaryTemplateModel(createdModelID, createdIsNccModel);
                try { ImageReduced?.Dispose(); } catch { }
                try { ShapeRegionHObject?.Dispose(); } catch { }
            }
        }

        private Microsoft.Win32.SaveFileDialog CreateTemplateSaveFileDialog(string title, string filter, string extension)
        {
            return new Microsoft.Win32.SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = GetDefaultTemplateFileName(extension),
                InitialDirectory = GetTemplateDialogInitialDirectory()
            };
        }

        private string GetDefaultTemplateFileName(string extension)
        {
            return RegionCreatMode switch
            {
                RegionCreatMode.绘制矩形 => $"Rect1{extension}",
                RegionCreatMode.绘制旋转矩形 => $"Rect2{extension}",
                RegionCreatMode.绘制圆形 => $"Circle{extension}",
                RegionCreatMode.链接输入 => $"LinkInput{extension}",
                _ => $"Model{extension}"
            };
        }

        private string GetTemplateDialogInitialDirectory()
        {
            string directory = string.IsNullOrWhiteSpace(CurrentShapeFilePath)
                ? string.Empty
                : Path.GetDirectoryName(CurrentShapeFilePath);

            return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
                ? directory
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public void ShowHRoi()
        {
            // WPF 模式：通过 PreviewDrawObjects 渲染，由 RefreshPreviewDisplay 驱动
            RefreshPreviewDisplay();
            if (_editableRoiPreview.HasValue)
            {
                RefreshEditableRoiPreview();
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
                {
                    try { mHRoi[index].hobject?.Dispose(); } catch { }
                    mHRoi[index] = ROI;
                }
                else
                {
                    mHRoi.Add(ROI);
                }
            }
            catch (Exception ex)
            {
                LogError("更新 ROI 预览列表失败", ex);
            }
        }

        #endregion

        #region 输出参数刷新
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
                    object newValue;

                    // 跨节点边界输出前克隆 HALCON 对象，使用 HalconImageOwnership 统一入口
                    if (value is HRegion[] regions)
                        newValue = regions
                            .Select(r => r != null ? new HRegion(HalconImageOwnership.CopyBorrowedObjectOrNull(r)) : null)
                            .ToArray();
                    else if (value is HObject hObj && HalconImageOwnership.IsInitializedSafe(hObj))
                        newValue = HalconImageOwnership.CopyBorrowedObjectOrNull(hObj);
                    else
                        newValue = value;

                    if (!ReferenceEquals(item.Value, newValue))
                        DisposeOutputParamValue(item.Value);
                    item.Value = newValue;
                }
            }

            if (!UpdateParam())
                LogWarning("更新输出参数失败");
        }
        #endregion

        #region 预览显示
        /// <summary>
        /// 获取当前可编辑 ROI 的图像坐标状态，供预览控件命中测试使用。
        /// </summary>
        /// <returns>当前 ROI 预览状态。</returns>
        public ShapeMatchingRoiPreview GetPreviewRoi()
        {
            EnsureEditableRoiInitialized();
            return _editableRoiPreview ?? ShapeMatchingRoiPreviewGeometry.CreateDefault(RegionCreatMode, 100, 100);
        }

        /// <summary>
        /// 应用预览控件拖拽后的 ROI，并同步模板学习区域和蓝色编辑覆盖层。
        /// </summary>
        /// <param name="roi">拖拽后的 ROI 图像坐标状态。</param>
        /// <param name="imageUnitsPerScreenPixel">一个屏幕像素对应的图像像素，用于保持手柄显示尺寸稳定。</param>
        public void ApplyPreviewRoi(ShapeMatchingRoiPreview roi, double imageUnitsPerScreenPixel = 1.0)
        {
            if (RegionCreatMode == RegionCreatMode.链接输入)
            {
                return;
            }

            _editableRoiPreview = ShapeMatchingRoiPreviewGeometry.Normalize(roi);
            UpdateDrawnRoiFromPreview();
            RefreshRoiPreviewOverlays(imageUnitsPerScreenPixel);
        }

        /// <summary>
        /// 刷新蓝色可编辑 ROI 覆盖层，保留匹配结果和黄色模板区域。
        /// </summary>
        /// <param name="imageUnitsPerScreenPixel">一个屏幕像素对应的图像像素，用于换算手柄半径。</param>
        public void RefreshEditableRoiPreview(double imageUnitsPerScreenPixel = 1.0)
        {
            RemovePreviewObjectsByColor(ShapeMatchingRoiPreviewStyle.EditableRoiColor);

            if (RegionCreatMode == RegionCreatMode.链接输入)
            {
                return;
            }

            EnsureEditableRoiInitialized();
            if (!_editableRoiPreview.HasValue)
            {
                return;
            }

            HObject roiContour = null;
            HObject handleContours = null;
            HObject rotateGuideContour = null;
            HObject directionArrowContour = null;
            try
            {
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                var roi = _editableRoiPreview.Value;
                if (CreateEditableRoiContour(roi, imageUnitsPerScreenPixel, out roiContour))
                {
                    AddXldPreviewOverlay(roiContour, ShapeMatchingRoiPreviewStyle.EditableRoiColor);
                }

                if (CreatePreviewHandles(roi, imageUnitsPerScreenPixel, out handleContours))
                {
                    AddXldPreviewOverlay(handleContours, ShapeMatchingRoiPreviewStyle.EditableRoiColor);
                }

                if (roi.Mode == RegionCreatMode.绘制旋转矩形)
                {
                    if (CreatePreviewRotateGuide(roi, out rotateGuideContour))
                    {
                        AddXldPreviewOverlay(rotateGuideContour, ShapeMatchingRoiPreviewStyle.EditableRoiColor);
                    }

                    if (CreatePreviewDirectionArrow(roi, out directionArrowContour))
                    {
                        AddXldPreviewOverlay(directionArrowContour, ShapeMatchingRoiPreviewStyle.EditableRoiColor);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("刷新可编辑 ROI 预览失败", ex);
            }
            finally
            {
                DisposeHObject(roiContour);
                DisposeHObject(handleContours);
                DisposeHObject(rotateGuideContour);
                DisposeHObject(directionArrowContour);
            }
        }

        /// <summary>
        /// 按当前图像尺寸初始化可编辑 ROI；切换绘制模式时强制重置为该模式的默认 ROI。
        /// </summary>
        /// <param name="forceModeRefresh">true 表示用户重新点击绘制 ROI，需要同步当前绘制模式。</param>
        private void EnsureEditableRoiInitialized(bool forceModeRefresh = false)
        {
            if (RegionCreatMode == RegionCreatMode.链接输入)
            {
                _editableRoiPreview = null;
                return;
            }

            bool needCreate = !_editableRoiPreview.HasValue
                || forceModeRefresh && _editableRoiPreview.Value.Mode != RegionCreatMode;

            if (!needCreate)
            {
                _editableRoiPreview = ShapeMatchingRoiPreviewGeometry.Normalize(_editableRoiPreview.Value);
                return;
            }

            if (!TryGetInputImageSize(out double width, out double height))
            {
                width = 100;
                height = 100;
            }

            _editableRoiPreview = ShapeMatchingRoiPreviewGeometry.CreateDefault(RegionCreatMode, width, height);
        }

        /// <summary>
        /// 从当前可编辑 ROI 生成模板学习区域，生成的 HALCON 区域由本模块持有并在替换时释放。
        /// </summary>
        /// <returns>true 表示已生成有效区域；false 表示当前 ROI 状态无效。</returns>
        private bool UpdateDrawnRoiFromPreview()
        {
            EnsureEditableRoiInitialized();
            if (!_editableRoiPreview.HasValue)
            {
                return false;
            }

            HObject newRegion = null;
            try
            {
                var roi = _editableRoiPreview.Value;
                switch (roi.Mode)
                {
                    case RegionCreatMode.绘制矩形:
                        HOperatorSet.GenRectangle1(
                            out newRegion,
                            roi.CenterY - roi.Length2,
                            roi.CenterX - roi.Length1,
                            roi.CenterY + roi.Length2,
                            roi.CenterX + roi.Length1);
                        break;
                    case RegionCreatMode.绘制旋转矩形:
                        HOperatorSet.GenRectangle2(
                            out newRegion,
                            roi.CenterY,
                            roi.CenterX,
                            roi.Angle,
                            roi.Length1,
                            roi.Length2);
                        break;
                    case RegionCreatMode.绘制圆形:
                        HOperatorSet.GenCircle(out newRegion, roi.CenterY, roi.CenterX, roi.Radius);
                        break;
                }

                if (newRegion == null || !newRegion.IsInitialized())
                {
                    return false;
                }

                var oldRegion = _drawnRoiRegion;
                _drawnRoiRegion = newRegion;
                DisposeHObject(oldRegion);
                return true;
            }
            catch (Exception ex)
            {
                DisposeHObject(newRegion);
                LogError("生成可编辑 ROI 区域失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取输入图像尺寸，返回宽高均为图像像素。
        /// </summary>
        /// <param name="width">输出图像宽度。</param>
        /// <param name="height">输出图像高度。</param>
        /// <returns>true 表示图像尺寸有效；false 表示当前没有可用图像。</returns>
        private bool TryGetInputImageSize(out double width, out double height)
        {
            width = 0;
            height = 0;
            try
            {
                if (_ownedInputImage == null || !_ownedInputImage.IsInitialized())
                {
                    return false;
                }

                HOperatorSet.GetImageSize(_ownedInputImage, out HTuple widthTuple, out HTuple heightTuple);
                if (widthTuple.Length == 0 || heightTuple.Length == 0 || widthTuple.D <= 0 || heightTuple.D <= 0)
                {
                    return false;
                }

                width = widthTuple.D;
                height = heightTuple.D;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>替换拥有的输入图像副本，兼容 HImage 和 HObject 输入</summary>
        private void ReplaceOwnedInputImage(object imageValue)
        {
            var oldOwned = _ownedInputImage;
            try
            {
                switch (imageValue)
                {
                    case HImage hImage when hImage.IsInitialized():
                        _ownedInputImage = hImage.CopyImage();
                        break;
                    case HObject hObj when hObj.IsInitialized():
                        using (var tempImage = new HImage(hObj))
                        {
                            _ownedInputImage = tempImage.CopyImage();
                        }
                        break;
                    default:
                        _ownedInputImage = null;
                        break;
                }
            }
            catch
            {
                _ownedInputImage = null;
            }
            if (oldOwned != null && !ReferenceEquals(oldOwned, _ownedInputImage))
            {
                try { oldOwned.Dispose(); } catch { }
            }
        }

        /// <summary>替换拥有的输入区域副本</summary>
        private void ReplaceOwnedInputRegion(object regionValue)
        {
            var oldRegion = _initRegion;
            try
            {
                switch (regionValue)
                {
                    case HRegion hRegion when hRegion.IsInitialized():
                        InitRegion = (HObject)hRegion.Clone();
                        break;
                    case HObject hObj when hObj.IsInitialized():
                        InitRegion = hObj.Clone();
                        break;
                    default:
                        InitRegion = new HObject();
                        break;
                }
            }
            catch
            {
                InitRegion = new HObject();
            }
            if (oldRegion != null && !ReferenceEquals(oldRegion, _initRegion) && oldRegion.IsInitialized())
            {
                try { oldRegion.Dispose(); } catch { }
            }
        }

        /// <summary>清理 mHRoi 列表中的 HALCON 句柄</summary>
        private void ClearMHRoi()
        {
            foreach (var roi in mHRoi)
            {
                try { roi.hobject?.Dispose(); } catch { }
            }
            mHRoi.Clear();
        }

        /// <summary>刷新预览图像和覆盖层；会替换预览图像，仅用于输入图像或结果整体变化。</summary>
        private void RefreshPreviewDisplay()
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(RefreshPreviewDisplay));
                return;
            }

            // 更新预览图像
            var oldPreview = _previewImageObject;
            if (_ownedInputImage != null && _ownedInputImage.IsInitialized())
                PreviewImageObject = _ownedInputImage.Clone();
            else
                PreviewImageObject = null;
            if (oldPreview != null && !ReferenceEquals(oldPreview, _previewImageObject))
            {
                try { oldPreview.Dispose(); } catch { }
            }

            RefreshRoiPreviewOverlays();
        }

        /// <summary>
        /// 只刷新 ROI 覆盖层，不替换 PreviewImageObject，避免拖拽时 VMHWindowControl 重新适配图像导致缩放复位。
        /// </summary>
        /// <param name="imageUnitsPerScreenPixel">一个屏幕像素对应的图像像素，用于换算编辑手柄尺寸。</param>
        private void RefreshRoiPreviewOverlays(double imageUnitsPerScreenPixel = 1.0)
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => RefreshRoiPreviewOverlays(imageUnitsPerScreenPixel)));
                return;
            }

            // 更新覆盖层 — 从 mHRoi 克隆区域到 WPF 预览，不触碰当前窗口缩放区域。
            ClearPreviewDrawObjects();
            foreach (var roi in mHRoi)
            {
                // 绘制模式下学习区域只作为内部模板 ROI，不叠加旧的输入区域轮廓。
                if (RegionCreatMode != RegionCreatMode.链接输入 && roi.roiType == HRoiType.输入区域)
                    continue;

                if (roi.hobject == null || !roi.hobject.IsInitialized())
                    continue;
                try
                {
                    PreviewDrawObjects.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = roi.hobject.Clone(),
                        Color = roi.drawColor,
                        IsFillDisplay = roi.IsFillDisp
                    });
                }
                catch { }
            }

            if (_editableRoiPreview.HasValue && RegionCreatMode != RegionCreatMode.链接输入)
            {
                RefreshEditableRoiPreview(imageUnitsPerScreenPixel);
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

        /// <summary>
        /// 移除指定颜色的预览覆盖层，避免刷新可编辑 ROI 时误删匹配结果。
        /// </summary>
        /// <param name="color">需要移除的覆盖层颜色。</param>
        private void RemovePreviewObjectsByColor(string color)
        {
            foreach (var drawObject in PreviewDrawObjects.Where(item => item?.Color == color).ToList())
            {
                try { drawObject.Hobject?.Dispose(); } catch { }
                PreviewDrawObjects.Remove(drawObject);
            }
        }

        /// <summary>
        /// 添加 XLD 轮廓覆盖层；传入对象仍由调用方负责释放。
        /// </summary>
        /// <param name="contourObject">图像坐标系下的 HALCON XLD 对象。</param>
        /// <param name="color">覆盖层显示颜色。</param>
        private void AddXldPreviewOverlay(HObject contourObject, string color)
        {
            if (contourObject == null || !contourObject.IsInitialized())
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

        /// <summary>
        /// 添加区域覆盖层；传入对象仍由调用方负责释放。
        /// </summary>
        /// <param name="regionObject">图像坐标系下的 HALCON 区域对象。</param>
        /// <param name="color">覆盖层显示颜色。</param>
        /// <param name="fill">true 表示按实心区域显示。</param>
        private void AddRegionPreviewOverlay(HObject regionObject, string color, bool fill)
        {
            if (regionObject == null || !regionObject.IsInitialized())
            {
                return;
            }

            try
            {
                PreviewDrawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = regionObject.Clone(),
                    Color = color,
                    IsFillDisplay = fill
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// 创建当前 ROI 的 XLD 轮廓，矩形和圆形分别对齐测量模块的预览绘制方式。
        /// </summary>
        /// <param name="roi">当前 ROI 预览状态。</param>
        /// <param name="imageUnitsPerScreenPixel">一个屏幕像素对应的图像像素。</param>
        /// <param name="roiContour">输出的 ROI 轮廓，由调用方释放。</param>
        /// <returns>true 表示轮廓创建成功。</returns>
        private static bool CreateEditableRoiContour(
            ShapeMatchingRoiPreview roi,
            double imageUnitsPerScreenPixel,
            out HObject roiContour)
        {
            roiContour = null;
            try
            {
                roi = ShapeMatchingRoiPreviewGeometry.Normalize(roi);
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);

                switch (roi.Mode)
                {
                    case RegionCreatMode.绘制矩形:
                    case RegionCreatMode.绘制旋转矩形:
                        HOperatorSet.GenRectangle2ContourXld(
                            out roiContour,
                            roi.CenterY,
                            roi.CenterX,
                            roi.Angle,
                            Math.Max(1.0, roi.Length1),
                            Math.Max(1.0, roi.Length2));
                        break;
                    case RegionCreatMode.绘制圆形:
                        HOperatorSet.GenCircleContourXld(
                            out roiContour,
                            roi.CenterY,
                            roi.CenterX,
                            Math.Max(1.0, roi.Radius),
                            0,
                            Math.PI * 2.0,
                            "positive",
                            Math.Max(1.0, imageUnitsPerScreenPixel));
                        break;
                }

                return roiContour != null && roiContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(roiContour);
                roiContour = null;
                return false;
            }
        }

        /// <summary>
        /// 创建当前 ROI 的 XLD 拖拽手柄轮廓。
        /// </summary>
        /// <param name="roi">当前 ROI 预览状态。</param>
        /// <param name="imageUnitsPerScreenPixel">一个屏幕像素对应的图像像素。</param>
        /// <param name="handleContours">输出的手柄轮廓集合，由调用方释放。</param>
        /// <returns>true 表示至少创建了一个手柄。</returns>
        private static bool CreatePreviewHandles(
            ShapeMatchingRoiPreview roi,
            double imageUnitsPerScreenPixel,
            out HObject handleContours)
        {
            handleContours = null;
            HObject tempContour = null;
            try
            {
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                foreach (var pair in ShapeMatchingRoiPreviewGeometry.GetHandlePoints(roi))
                {
                    double screenSize = pair.Key == ShapeMatchingRoiPreviewHandle.Center
                        ? ShapeMatchingRoiPreviewStyle.CenterHandleScreenSize
                        : ShapeMatchingRoiPreviewStyle.HandleScreenSize;
                    double radius = Math.Max(1.0, screenSize * imageUnitsPerScreenPixel / 2.0);

                    HOperatorSet.GenCircleContourXld(
                        out tempContour,
                        pair.Value.Y,
                        pair.Value.X,
                        radius,
                        0,
                        Math.PI * 2.0,
                        "positive",
                        Math.Max(1.0, imageUnitsPerScreenPixel));

                    ConcatOwnedObject(ref handleContours, tempContour);
                    DisposeHObject(tempContour);
                    tempContour = null;
                }

                return handleContours != null && handleContours.IsInitialized();
            }
            catch
            {
                DisposeHObject(handleContours);
                handleContours = null;
                return false;
            }
            finally
            {
                DisposeHObject(tempContour);
            }
        }

        /// <summary>
        /// 创建旋转矩形右侧边到旋转手柄之间的引导线。
        /// </summary>
        /// <param name="roi">当前旋转矩形 ROI 状态。</param>
        /// <param name="rotateGuideContour">输出的引导线对象，由调用方释放。</param>
        /// <returns>true 表示引导线创建成功。</returns>
        private static bool CreatePreviewRotateGuide(ShapeMatchingRoiPreview roi, out HObject rotateGuideContour)
        {
            rotateGuideContour = null;
            try
            {
                var handles = ShapeMatchingRoiPreviewGeometry.GetHandlePoints(roi);
                if (!handles.TryGetValue(ShapeMatchingRoiPreviewHandle.Right, out var right)
                    || !handles.TryGetValue(ShapeMatchingRoiPreviewHandle.Rotate, out var rotate))
                {
                    return false;
                }

                HOperatorSet.GenContourPolygonXld(
                    out rotateGuideContour,
                    new HTuple(right.Y, rotate.Y),
                    new HTuple(right.X, rotate.X));
                return rotateGuideContour != null && rotateGuideContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(rotateGuideContour);
                rotateGuideContour = null;
                return false;
            }
        }

        /// <summary>
        /// 创建旋转矩形方向提示箭头，帮助用户识别 HALCON 角度方向。
        /// </summary>
        /// <param name="roi">当前旋转矩形 ROI 状态。</param>
        /// <param name="directionArrowContour">输出的方向提示轮廓，由调用方释放。</param>
        /// <returns>true 表示方向提示创建成功。</returns>
        private static bool CreatePreviewDirectionArrow(ShapeMatchingRoiPreview roi, out HObject directionArrowContour)
        {
            directionArrowContour = null;
            try
            {
                roi = ShapeMatchingRoiPreviewGeometry.Normalize(roi);
                double axisX = Math.Cos(roi.Angle);
                double axisY = -Math.Sin(roi.Angle);
                double headSize = Math.Max(8.0, Math.Min(18.0, Math.Min(roi.Length1, roi.Length2) * 0.25));
                double arrowLength = Math.Min(Math.Max(roi.Length1 * 0.65, headSize * 2.0), 80.0);
                double tipDistance = Math.Max(headSize, roi.Length1 - headSize * 0.9);
                double tipCol = roi.CenterX + axisX * tipDistance;
                double tipRow = roi.CenterY + axisY * tipDistance;
                double startCol = tipCol - axisX * arrowLength;
                double startRow = tipRow - axisY * arrowLength;

                directionArrowContour = CreateArrowContour(
                    startRow,
                    startCol,
                    tipRow,
                    tipCol,
                    headSize,
                    ShapeMatchingRoiPreviewStyle.DirectionArrowHeadScreenSize);
                return directionArrowContour != null && directionArrowContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(directionArrowContour);
                directionArrowContour = null;
                return false;
            }
        }

        /// <summary>
        /// 创建单个 XLD 箭头轮廓。
        /// </summary>
        private static HObject CreateArrowContour(
            double row1,
            double column1,
            double row2,
            double column2,
            double headLength,
            double headWidth)
        {
            double deltaRow = row2 - row1;
            double deltaColumn = column2 - column1;
            double length = Math.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn);
            if (length <= double.Epsilon)
            {
                HOperatorSet.GenContourPolygonXld(out HObject pointContour, new HTuple(row1), new HTuple(column1));
                return pointContour;
            }

            double rowDirection = deltaRow / length;
            double columnDirection = deltaColumn / length;
            double effectiveHeadLength = Math.Min(Math.Max(1.0, headLength), length);
            double halfHeadWidth = Math.Max(1.0, headWidth) / 2.0;
            double headBaseRow = row1 + (length - effectiveHeadLength) * rowDirection;
            double headBaseColumn = column1 + (length - effectiveHeadLength) * columnDirection;

            double rowP1 = headBaseRow + halfHeadWidth * columnDirection;
            double columnP1 = headBaseColumn - halfHeadWidth * rowDirection;
            double rowP2 = headBaseRow - halfHeadWidth * columnDirection;
            double columnP2 = headBaseColumn + halfHeadWidth * rowDirection;

            HOperatorSet.GenContourPolygonXld(
                out HObject arrowContour,
                new HTuple(new[] { row1, row2, rowP1, row2, rowP2, row2 }),
                new HTuple(new[] { column1, column2, columnP1, column2, columnP2, column2 }));
            return arrowContour;
        }

        /// <summary>
        /// 将临时 HALCON 对象追加到目标集合，目标对象由调用方统一释放。
        /// </summary>
        /// <param name="target">累计的目标对象集合。</param>
        /// <param name="source">需要追加的临时对象。</param>
        private static void ConcatOwnedObject(ref HObject target, HObject source)
        {
            if (source == null || !source.IsInitialized())
            {
                return;
            }

            if (target == null || !target.IsInitialized())
            {
                target = source.Clone();
                return;
            }

            HObject oldTarget = target;
            target = oldTarget.ConcatObj(source);
            DisposeHObject(oldTarget);
        }

        /// <summary>
        /// 安全释放本模块拥有的 HALCON 对象句柄。
        /// </summary>
        /// <param name="hObject">需要释放的 HALCON 对象，可为空。</param>
        private static void DisposeHObject(HObject hObject)
        {
            if (hObject == null)
            {
                return;
            }

            try { hObject.Dispose(); } catch { }
        }

        /// <summary>释放运行时拥有的 HALCON 对象</summary>
        private void DisposeOwnedRuntimeObjects()
        {
            ClearPreviewDrawObjects();
            if (_previewImageObject != null)
            {
                try { _previewImageObject.Dispose(); } catch { }
                _previewImageObject = null;
            }
            if (_ownedInputImage != null)
            {
                try { _ownedInputImage.Dispose(); } catch { }
                _ownedInputImage = null;
            }
            if (_initRegion != null && _initRegion.IsInitialized())
            {
                try { _initRegion.Dispose(); } catch { }
                _initRegion = new HObject();
            }
            // 释放输出区域数组中的自有 HRegion 句柄
            if (_outputRegions != null)
            {
                foreach (var r in _outputRegions)
                    try { r?.Dispose(); } catch { }
                _outputRegions = null;
            }
            // 释放可编辑 ROI 存储的绘制区域
            if (_drawnRoiRegion != null)
            {
                try { _drawnRoiRegion.Dispose(); } catch { }
                _drawnRoiRegion = null;
            }
        }

        /// <summary>
        /// 克隆匹配区域数组，输出数组由本模块持有并在替换或释放时清理。
        /// </summary>
        /// <param name="regions">HALCON 匹配算子生成的临时区域数组。</param>
        /// <returns>可发布到输出参数的自有区域数组。</returns>
        private static HRegion[] CloneRegionArray(HRegion[] regions)
        {
            if (regions == null || regions.Length == 0)
                return Array.Empty<HRegion>();

            return regions
                .Select(r => r != null ? new HRegion(HalconImageOwnership.CopyBorrowedObjectOrNull(r)) : null)
                .ToArray();
        }

        /// <summary>
        /// 释放本模块拥有的 HALCON 区域数组。
        /// </summary>
        /// <param name="regions">需要释放的区域数组，可为空。</param>
        private static void DisposeRegionArray(HRegion[] regions)
        {
            if (regions == null)
                return;

            foreach (var region in regions)
                HalconImageOwnership.DisposeOwned(region);
        }

        /// <summary>
        /// 判断匹配结果中是否存在已初始化的区域。
        /// </summary>
        /// <param name="regions">匹配结果区域数组。</param>
        /// <returns>true 表示至少找到一个可用区域；false 表示没有匹配结果。</returns>
        private static bool HasMatchedRegions(HRegion[] regions)
        {
            return regions?.Any(region => region != null && region.IsInitialized()) == true;
        }

        /// <summary>
        /// 释放 OutputParams 上一次保存的 HALCON 值，避免刷新输出时泄漏旧句柄。
        /// </summary>
        /// <param name="value">旧输出值，可能是 HObject 或 HRegion 数组。</param>
        private static void DisposeOutputParamValue(object value)
        {
            switch (value)
            {
                case HRegion[] regions:
                    DisposeRegionArray(regions);
                    break;
                case HObject hObj:
                    HalconImageOwnership.DisposeOwned(hObj);
                    break;
            }
        }

        /// <summary>
        /// 判断当前是否已有可用于匹配的 HALCON 模板句柄。
        /// </summary>
        /// <returns>true 表示当前 ModelID 可作为模板句柄使用。</returns>
        private bool HasLoadedTemplateModel()
        {
            return HasTemplateModelHandle(ModelID);
        }

        /// <summary>
        /// 获取当前模板扩展名；已加载句柄优先，未加载时才回退到完整路径。
        /// </summary>
        /// <returns>.shm、.ncm 或空字符串。</returns>
        private string GetCurrentTemplateExtension()
        {
            if (HasLoadedTemplateModel())
                return _isNccModel ? ".ncm" : ".shm";

            if (!string.IsNullOrWhiteSpace(CurrentShapeFilePath))
                return Path.GetExtension(CurrentShapeFilePath);

            return string.Empty;
        }

        /// <summary>
        /// 写入形状匹配模块的 trace 级别运行诊断。
        /// </summary>
        /// <param name="message">诊断内容。</param>
        private void LogTrace(string message)
        {
            Logs.LogTrace($"{ModuleName}_{Serial}：{message}");
        }

        /// <summary>
        /// 写入形状匹配模块的 warning 级别可恢复问题。
        /// </summary>
        /// <param name="message">诊断内容。</param>
        private void LogWarning(string message)
        {
            Logs.LogWarning($"{ModuleName}_{Serial}：{message}");
        }

        /// <summary>
        /// 写入形状匹配模块的 error 级别异常诊断。
        /// </summary>
        /// <param name="message">诊断内容。</param>
        /// <param name="exception">异常对象。</param>
        private void LogError(string message, Exception exception)
        {
            Logs.LogError($"{ModuleName}_{Serial}：{message}，Error={exception}");
        }
        #endregion
    }

    public static class ShapeMatchingExtensions
    {
        public static void UpdateOrAdd(
            this ObservableCollection<ShapeFileDefinition> collection,
            ShapeFileDefinition newItem,
            bool keepOrder = false)
        {
            if (newItem == null) return;

            var existingItem = collection.FirstOrDefault(x => x.Name == newItem.Name && x.Path == newItem.Path);

            if (existingItem != null)
            {
                if (keepOrder)
                {
                    existingItem.IsSelected = newItem.IsSelected;
                }
                else
                {
                    collection.Remove(existingItem);
                    collection.Add(newItem);
                }
            }
            else
            {
                collection.Add(newItem);
            }
        }

        public static HTuple ToHTuple(this ObservableCollection<ParamDefinition> paramDefinitions)
        {
            HTuple tuple = new HTuple();

            if (paramDefinitions == null)
                return tuple;

            foreach (var param in paramDefinitions)
            {
                if (param.Value == null)
                {
                    tuple.Append(new HTuple());
                    continue;
                }

                try
                {
                    switch (param.ValueType)
                    {
                        case ParamValueType.Int:
                            tuple.Append(Convert.ToInt32(param.Value));
                            break;
                        case ParamValueType.Double:
                            tuple.Append(Convert.ToDouble(param.Value));
                            break;
                        case ParamValueType.StringInt:
                            if (int.TryParse(param.Value.ToString(), out int intValue))
                                tuple.Append(Convert.ToInt32(intValue));
                            else
                                tuple.Append(param.Value.ToString());
                            break;
                        case ParamValueType.StringDouble:
                            if (double.TryParse(param.Value.ToString(), out double doubleValue))
                                tuple.Append(Convert.ToDouble(doubleValue));
                            else
                                tuple.Append(param.Value.ToString());
                            break;
                        case ParamValueType.String:
                            tuple.Append(param.Value.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogWarning($"[形状匹配] 参数转换失败: Name={param.Name}, Value={param.Value}, Error={ex.Message}");
                    tuple.Append(new HTuple());
                }
            }

            return tuple;
        }
    }
}

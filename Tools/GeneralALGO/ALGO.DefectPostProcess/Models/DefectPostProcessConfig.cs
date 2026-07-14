using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Mvvm;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ALGO.DefectPostProcess.Models
{
    public partial class DefectPostProcessModel
    {
        #region 方案配置管理
        private ObservableCollection<DefectPostProcessScheme> _schemeConfigs = new ObservableCollection<DefectPostProcessScheme>();
        public ObservableCollection<DefectPostProcessScheme> SchemeConfigs
        {
            get { return _schemeConfigs; }
            set
            {
                if (ReferenceEquals(_schemeConfigs, value))
                {
                    return;
                }

                if (_schemeConfigs != null)
                {
                    _schemeConfigs.CollectionChanged -= OnSchemeConfigsChanged;
                }

                _schemeConfigs = value ?? new ObservableCollection<DefectPostProcessScheme>();
                _schemeConfigs.CollectionChanged += OnSchemeConfigsChanged;
                RaisePropertyChanged();
                NotifySchemeStateChanged();
            }
        }

        private string _schemeName = string.Empty;
        public string SchemeName
        {
            get { return _schemeName; }
            set
            {
                if (SetProperty(ref _schemeName, value))
                {
                    NotifySchemeStateChanged();
                }
            }
        }

        private string _currentSchemeName = string.Empty;
        public string CurrentSchemeName
        {
            get { return _currentSchemeName; }
            set
            {
                if (SetProperty(ref _currentSchemeName, value))
                {
                    NotifySchemeStateChanged();
                }
            }
        }

        [JsonIgnore]
        private DefectPostProcessScheme _selectedScheme;
        [JsonIgnore]
        public DefectPostProcessScheme SelectedScheme
        {
            get { return _selectedScheme; }
            set
            {
                if (SetProperty(ref _selectedScheme, value))
                {
                    if (value != null)
                    {
                        SchemeName = value.Name;
                    }

                    NotifySchemeStateChanged();
                }
            }
        }

        [JsonIgnore]
        private bool _isSchemeDirty;
        [JsonIgnore]
        public bool IsSchemeDirty
        {
            get { return _isSchemeDirty; }
            private set
            {
                if (SetProperty(ref _isSchemeDirty, value))
                {
                    NotifySchemeStateChanged();
                }
            }
        }

        [JsonIgnore]
        public bool HasSchemeConfigs
        {
            get { return SchemeConfigs != null && SchemeConfigs.Count > 0; }
        }

        [JsonIgnore]
        public string SchemeStorageFilePath
        {
            get
            {
                string solutionFilePath = PrismProvider.ProjectManager?.SolutionManager?.DefaultBaseInfo?.FilePath;
                if (!string.IsNullOrWhiteSpace(solutionFilePath))
                {
                    return solutionFilePath;
                }

                var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
                if (solutionItem == null || string.IsNullOrWhiteSpace(solutionItem.FilePath))
                {
                    return string.Empty;
                }

                string schemeName = string.IsNullOrWhiteSpace(solutionItem.Name) ? "Default" : solutionItem.Name;
                return Path.Combine(solutionItem.FilePath, $"{schemeName}.rysl");
            }
        }

        [JsonIgnore]
        public string SchemeStorageDirectory
        {
            get
            {
                string filePath = SchemeStorageFilePath;
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return PrismProvider.ProjectManager?.SltCurSolutionItem?.FilePath ?? string.Empty;
                }

                return Path.GetDirectoryName(filePath) ?? string.Empty;
            }
        }

        [JsonIgnore]
        public string SchemeFileDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SchemeStorageFilePath))
                {
                    return "将随当前 .rysl 方案文件一起保存";
                }

                return SchemeStorageFilePath;
            }
        }

        [JsonIgnore]
        public string SchemeSummary
        {
            get
            {
                if (!HasSchemeConfigs)
                {
                    return "当前还没有保存方案，点击“保存”后会把规则、输入绑定和标定文件写入当前 .rysl 方案文件。";
                }

                string currentName = string.IsNullOrWhiteSpace(CurrentSchemeName) ? "未绑定" : CurrentSchemeName;
                string selectedName = SelectedScheme?.Name ?? "未选择";
                string saveState = IsSchemeDirty ? "当前规则或输入有未保存修改" : "当前规则和输入已与方案同步";
                return $"已保存 {SchemeConfigs.Count} 个方案；当前方案：{currentName}；选中方案：{selectedName}；{saveState}。";
            }
        }

        /// <summary>
        /// 初始化方案数据并同步当前选择状态。
        /// </summary>
        public void InitializeSchemeManagement()
        {
            SchemeConfigs = NormalizeSchemeConfigs(SchemeConfigs);

            if (SchemeConfigs.Count == 0)
            {
                string initialSchemeName = string.IsNullOrWhiteSpace(CurrentSchemeName)
                    ? "默认方案" : CurrentSchemeName.Trim();

                DefectPostProcessScheme initialScheme = CreateSchemeSnapshot(initialSchemeName);
                SchemeConfigs.Add(initialScheme);
                CurrentSchemeName = initialScheme.Name;
                SelectedScheme = initialScheme;
                SchemeName = initialScheme.Name;
                IsSchemeDirty = false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(CurrentSchemeName))
                {
                    SelectedScheme = FindScheme(CurrentSchemeName);
                }

                if (SelectedScheme == null)
                {
                    SelectedScheme = SchemeConfigs.FirstOrDefault();
                }

                if (SelectedScheme != null)
                {
                    CurrentSchemeName = string.IsNullOrWhiteSpace(CurrentSchemeName)
                        ? SelectedScheme.Name
                        : CurrentSchemeName;

                    RestoreSelectedSchemeFilePathsFromPersistedCurrentState();

                    if (string.IsNullOrWhiteSpace(SchemeName))
                    {
                        SchemeName = SelectedScheme.Name;
                    }
                }
                else if (string.IsNullOrWhiteSpace(SchemeName))
                {
                    SchemeName = GenerateNewSchemeName();
                }

                IsSchemeDirty = false;
            }

            NotifySchemeStateChanged();
        }

        /// <summary>
        /// 兼容旧项目：若当前模型已保存了文件路径，但选中方案内还没有路径，则回填到方案，避免打开时被空方案覆盖。
        /// </summary>
        private void RestoreSelectedSchemeFilePathsFromPersistedCurrentState()
        {
            if (SelectedScheme == null)
            {
                return;
            }

            string currentCalibrationPath = NormalizeCalibrationFilePath(CalibrationFilePath);
            if (string.IsNullOrWhiteSpace(SelectedScheme.CalibrationFilePath)
                && !string.IsNullOrWhiteSpace(currentCalibrationPath))
            {
                SelectedScheme.CalibrationFilePath = currentCalibrationPath;
            }

        }

        /// <summary>
        /// 准备新建方案。
        /// </summary>
        public void PrepareNewScheme()
        {
            CurrentSchemeName = string.Empty;
            SelectedScheme = null;
            SchemeName = GenerateNewSchemeName();
            IsSchemeDirty = true;
            NotifySchemeStateChanged();
        }

        /// <summary>
        /// 保存当前方案。
        /// </summary>
        public bool SaveCurrentScheme(out string message)
        {
            message = string.Empty;
            SyncCurrentRuleConfigFromEditingState();

            string targetName = NormalizeSchemeName(string.IsNullOrWhiteSpace(SchemeName) ? CurrentSchemeName : SchemeName);
            if (string.IsNullOrWhiteSpace(targetName))
            {
                targetName = GenerateNewSchemeName();
            }

            DefectPostProcessScheme currentScheme = FindScheme(CurrentSchemeName);
            DefectPostProcessScheme targetScheme = FindScheme(targetName);

            if (currentScheme == null && targetScheme != null)
            {
                message = $"方案“{targetName}”已存在，请改名后另存，或先应用该方案再保存。";
                return false;
            }

            if (currentScheme != null)
            {
                if (!string.Equals(currentScheme.Name, targetName, StringComparison.OrdinalIgnoreCase)
                    && targetScheme != null
                    && !ReferenceEquals(currentScheme, targetScheme))
                {
                    message = $"方案“{targetName}”已存在，请改用其他名称。";
                    return false;
                }

                currentScheme.Name = targetName;
                currentScheme.UpdatedTime = DateTime.Now;
                currentScheme.SelectedRuleKey = GetDefectRuleKey(CurrentDefect);
                currentScheme.DefectRuleConfigs = CloneRuleConfigs(DefectRuleConfigs);
                currentScheme.DefectClassDefinitions = CloneDefectClassDefinitions(_configuredDefectDefinitions);
                currentScheme.SheetSizeJudge = CloneSheetSizeJudgeConfig(SheetSizeJudge);
                currentScheme.CalibrationFilePath = NormalizeCalibrationFilePath(CalibrationFilePath);
                currentScheme.InputImageBinding = CloneInputBinding(InputImageBinding);
                currentScheme.InputResultsBinding = CloneInputBinding(InputResultsBinding);
                currentScheme.InputPixelEquivalentBinding = CloneInputBinding(InputPixelEquivalentBinding);
                currentScheme.InputPixelEquivalentXBinding = CloneInputBinding(InputPixelEquivalentXBinding);
                currentScheme.InputPixelEquivalentYBinding = CloneInputBinding(InputPixelEquivalentYBinding);
                currentScheme.InputEdgeCalibrationXBinding = CloneInputBinding(InputEdgeCalibrationXBinding);
                currentScheme.IsFastModeEnabled = IsFastModeEnabled;
                targetScheme = currentScheme;
            }
            else
            {
                targetScheme = CreateSchemeSnapshot(targetName);
                SchemeConfigs.Add(targetScheme);
            }

            CurrentSchemeName = targetScheme.Name;
            SelectedScheme = targetScheme;
            SchemeName = targetScheme.Name;
            IsSchemeDirty = false;
            NotifySchemeStateChanged();
            if (!UpdateParam())
            {
                LogWarning("保存方案后更新参数失败");
            }

            return true;
        }

        /// <summary>
        /// 尝试创建用于导出的方案对象。
        /// </summary>
        public bool TryCreateExportScheme(out DefectPostProcessScheme exportScheme, out string message)
        {
            exportScheme = null;
            message = string.Empty;
            SyncCurrentRuleConfigFromEditingState();

            if ((DefectRuleConfigs == null || DefectRuleConfigs.Count == 0) && !HasSchemeConfigs)
            {
                message = "当前还没有可导出的方案配置。";
                return false;
            }

            string exportName = NormalizeSchemeName(string.IsNullOrWhiteSpace(SchemeName) ? CurrentSchemeName : SchemeName);
            if (string.IsNullOrWhiteSpace(exportName))
            {
                exportName = SelectedScheme?.Name;
            }

            if (string.IsNullOrWhiteSpace(exportName))
            {
                exportName = GenerateNewSchemeName();
            }

            exportScheme = CreateSchemeSnapshot(exportName);
            return true;
        }

        /// <summary>
        /// 应用选中的方案。
        /// </summary>
        public bool ApplySelectedScheme(out string message)
        {
            message = string.Empty;

            if (SelectedScheme == null)
            {
                message = "请先选择要应用的方案。";
                return false;
            }

            string preferredRuleKey = SelectedScheme.SelectedRuleKey;
            SetConfiguredDefectDefinitions(SelectedScheme.DefectClassDefinitions, refreshUi: false, markDirty: false);
            DefectRuleConfigs = CloneRuleConfigs(SelectedScheme.DefectRuleConfigs);
            SheetSizeJudge = CloneSheetSizeJudgeConfig(SelectedScheme.SheetSizeJudge);
            IsFastModeEnabled = SelectedScheme.IsFastModeEnabled;
            CalibrationFilePath = NormalizeCalibrationFilePath(SelectedScheme.CalibrationFilePath);
            if (!LoadKeyParam(flushDeferredStateRefresh: false))
            {
                message = "应用方案失败，输入参数恢复异常。";
                return false;
            }

            CurrentSchemeName = SelectedScheme.Name;
            SchemeName = SelectedScheme.Name;

            RefreshSchemeRuleBindings(preferredRuleKey);
            RequestEditingStateRefresh();

            SelectedScheme = FindScheme(CurrentSchemeName) ?? SelectedScheme;
            IsSchemeDirty = false;
            NotifySchemeStateChanged();
            return true;
        }

        /// <summary>
        /// 删除选中的方案。
        /// </summary>
        public bool DeleteSelectedScheme(out string message)
        {
            message = string.Empty;

            if (SelectedScheme == null)
            {
                message = "请先选择要删除的方案。";
                return false;
            }

            string deletedName = SelectedScheme.Name;
            SchemeConfigs.Remove(SelectedScheme);
            bool deletedCurrentScheme = string.Equals(CurrentSchemeName, deletedName, StringComparison.OrdinalIgnoreCase);

            if (deletedCurrentScheme)
            {
                CurrentSchemeName = string.Empty;
                IsSchemeDirty = true;
            }

            SelectedScheme = FindScheme(CurrentSchemeName) ?? SchemeConfigs.FirstOrDefault();
            SchemeName = deletedCurrentScheme
                ? GenerateNewSchemeName()
                : SelectedScheme?.Name ?? GenerateNewSchemeName();
            NotifySchemeStateChanged();
            return true;
        }

        /// <summary>
        /// 标记方案存在未保存修改。
        /// </summary>
        internal void MarkSchemeDirty()
        {
            if (_isLoadingCurrentRule)
            {
                return;
            }

            IsSchemeDirty = true;
        }

        /// <summary>
        /// 刷新方案与规则的绑定关系。
        /// </summary>
        private void RefreshSchemeRuleBindings(string preferredRuleKey)
        {
            BuildDefectItems();

            if (DefectItems != null && DefectItems.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(preferredRuleKey))
                {
                    DefectItem targetDefect = FindDefectItemByRuleKey(preferredRuleKey);
                    if (targetDefect != null && !ReferenceEquals(CurrentDefect, targetDefect))
                    {
                        CurrentDefect = targetDefect;
                        return;
                    }
                }
            }

            LoadCurrentDefectRule();
            UpdateCurrentDefectSummary();
        }

        /// <summary>
        /// 克隆规则配置集合。
        /// </summary>
        private List<DefectRuleConfig> CloneRuleConfigs(IEnumerable<DefectRuleConfig> source)
        {
            return source?
                .Where(item => item != null)
                .Select(item => new DefectRuleConfig
                {
                    RuleKey = item.RuleKey ?? string.Empty,
                    ClassId = item.ClassId,
                    ClassName = item.ClassName ?? string.Empty,
                    MinimumConfidence = Math.Clamp(item.MinimumConfidence, 0d, 1d),
                    IsNmsEnabled = item.IsNmsEnabled,
                    NmsIoUThreshold = Math.Clamp(item.NmsIoUThreshold, 0d, 1d),
                    FeatureThresholds = NormalizeFeatureThresholds(item.FeatureThresholds)
                })
                .ToList() ?? new List<DefectRuleConfig>();
        }

        /// <summary>
        /// 克隆片材尺寸判定配置。
        /// </summary>
        private static SheetSizeJudgeConfig CloneSheetSizeJudgeConfig(SheetSizeJudgeConfig source)
        {
            if (source == null)
            {
                return new SheetSizeJudgeConfig();
            }

            return new SheetSizeJudgeConfig
            {
                IsEnabled = source.IsEnabled,
                Threshold = source.Threshold,
                OpeningRadius = source.OpeningRadius,
                RectangularityMinimum = source.RectangularityMinimum,
                StandardLength = source.StandardLength,
                LengthTolerance = source.LengthTolerance,
                StandardWidth = source.StandardWidth,
                WidthTolerance = source.WidthTolerance
            };
        }

        /// <summary>
        /// 克隆并规范化方案内缺陷类别表，同 ClassId 保留第一条有效名称。
        /// </summary>
        private static List<DefectClassDefinition> CloneDefectClassDefinitions(IEnumerable<DefectClassDefinition> source)
        {
            return source?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.ClassName))
                .Select(item => new DefectClassDefinition
                {
                    ClassId = Math.Max(0, item.ClassId),
                    ClassName = item.ClassName.Trim()
                })
                .GroupBy(item => item.ClassId)
                .Select(group => group.First())
                .OrderBy(item => item.ClassId)
                .ToList() ?? new List<DefectClassDefinition>();
        }

        /// <summary>
        /// 规范化方案配置集合。
        /// </summary>
        private ObservableCollection<DefectPostProcessScheme> NormalizeSchemeConfigs(IEnumerable<DefectPostProcessScheme> source)
        {
            ObservableCollection<DefectPostProcessScheme> normalizedSchemes = new ObservableCollection<DefectPostProcessScheme>();
            if (source == null)
            {
                return normalizedSchemes;
            }

            foreach (DefectPostProcessScheme item in source.Where(item => item != null))
            {
                string schemeName = GenerateUniqueSchemeName(NormalizeSchemeName(item.Name), normalizedSchemes.Select(scheme => scheme.Name));
                normalizedSchemes.Add(new DefectPostProcessScheme
                {
                    Name = schemeName,
                    UpdatedTime = item.UpdatedTime == default ? DateTime.Now : item.UpdatedTime,
                    SelectedRuleKey = item.SelectedRuleKey ?? string.Empty,
                    DefectRuleConfigs = CloneRuleConfigs(item.DefectRuleConfigs),
                    DefectClassDefinitions = CloneDefectClassDefinitions(item.DefectClassDefinitions),
                    SheetSizeJudge = CloneSheetSizeJudgeConfig(item.SheetSizeJudge),
                    CalibrationFilePath = NormalizeCalibrationFilePath(item.CalibrationFilePath),
                    InputImageBinding = CloneInputBinding(item.InputImageBinding),
                    InputResultsBinding = CloneInputBinding(item.InputResultsBinding),
                    InputPixelEquivalentBinding = CloneInputBinding(item.InputPixelEquivalentBinding),
                    InputPixelEquivalentXBinding = CloneInputBinding(item.InputPixelEquivalentXBinding),
                    InputPixelEquivalentYBinding = CloneInputBinding(item.InputPixelEquivalentYBinding),
                    InputEdgeCalibrationXBinding = CloneInputBinding(item.InputEdgeCalibrationXBinding),
                    IsFastModeEnabled = item.IsFastModeEnabled
                });
            }

            return normalizedSchemes;
        }

        /// <summary>
        /// 创建方案快照。
        /// </summary>
        private DefectPostProcessScheme CreateSchemeSnapshot(string schemeName)
        {
            SyncCurrentRuleConfigFromEditingState();

            return new DefectPostProcessScheme
            {
                Name = NormalizeSchemeName(schemeName),
                UpdatedTime = DateTime.Now,
                SelectedRuleKey = GetDefectRuleKey(CurrentDefect),
                DefectRuleConfigs = CloneRuleConfigs(DefectRuleConfigs),
                DefectClassDefinitions = CloneDefectClassDefinitions(_configuredDefectDefinitions),
                SheetSizeJudge = CloneSheetSizeJudgeConfig(SheetSizeJudge),
                CalibrationFilePath = NormalizeCalibrationFilePath(CalibrationFilePath),
                InputImageBinding = CloneInputBinding(InputImageBinding),
                InputResultsBinding = CloneInputBinding(InputResultsBinding),
                InputPixelEquivalentBinding = CloneInputBinding(InputPixelEquivalentBinding),
                InputPixelEquivalentXBinding = CloneInputBinding(InputPixelEquivalentXBinding),
                InputPixelEquivalentYBinding = CloneInputBinding(InputPixelEquivalentYBinding),
                InputEdgeCalibrationXBinding = CloneInputBinding(InputEdgeCalibrationXBinding),
                IsFastModeEnabled = IsFastModeEnabled
            };
        }

        /// <summary>
        /// 查找指定方案。
        /// </summary>
        private DefectPostProcessScheme FindScheme(string schemeName)
        {
            string normalizedName = NormalizeSchemeName(schemeName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            return SchemeConfigs?.FirstOrDefault(item =>
                string.Equals(item.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 生成新方案名称。
        /// </summary>
        private string GenerateNewSchemeName()
        {
            return GenerateUniqueSchemeName("方案1");
        }

        /// <summary>
        /// 生成唯一方案名称。
        /// </summary>
        private string GenerateUniqueSchemeName(string baseName, IEnumerable<string> existingNames = null)
        {
            string normalizedBaseName = NormalizeSchemeName(baseName);
            if (string.IsNullOrWhiteSpace(normalizedBaseName))
            {
                normalizedBaseName = "方案";
            }

            HashSet<string> nameSet = new HashSet<string>(
                (existingNames ?? SchemeConfigs?.Select(item => item.Name) ?? Enumerable.Empty<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);

            if (!nameSet.Contains(normalizedBaseName))
            {
                return normalizedBaseName;
            }

            int index = 2;
            string candidateName;
            do
            {
                candidateName = $"{normalizedBaseName}_{index}";
                index++;
            }
            while (nameSet.Contains(candidateName));

            return candidateName;
        }

        /// <summary>
        /// 规范化方案名称。
        /// </summary>
        private static string NormalizeSchemeName(string schemeName)
        {
            return string.IsNullOrWhiteSpace(schemeName) ? string.Empty : schemeName.Trim();
        }

        /// <summary>
        /// 处理方案配置集合变更。
        /// </summary>
        private void OnSchemeConfigsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifySchemeStateChanged();
        }

        /// <summary>
        /// 通知方案状态已变更。
        /// </summary>
        private void NotifySchemeStateChanged()
        {
            RaisePropertyChanged(nameof(HasSchemeConfigs));
            RaisePropertyChanged(nameof(SchemeStorageFilePath));
            RaisePropertyChanged(nameof(SchemeStorageDirectory));
            RaisePropertyChanged(nameof(SchemeFileDisplay));
            RaisePropertyChanged(nameof(SchemeSummary));
        }

        #endregion


        #region 配置状态与缺陷类别


        private static ObservableCollection<DefectCustomAlgorithmItem> CreateDefaultCustomAlgorithmItems()
        {
            return new ObservableCollection<DefectCustomAlgorithmItem>
            {
                new DefectCustomAlgorithmItem(
                    DefectCustomAlgorithmKeys.SheetSizeJudge,
                    "片材尺寸判定",
                    false)
            };
        }

        private static ObservableCollection<DefectCustomAlgorithmItem> NormalizeCustomAlgorithmItems(IEnumerable<DefectCustomAlgorithmItem> source)
        {
            bool isSheetSizeJudgeEnabled = source?
                .Where(item => string.Equals(item?.AlgorithmKey, DefectCustomAlgorithmKeys.SheetSizeJudge, StringComparison.Ordinal))
                .Any(item => item.IsEnabled) == true;

            return new ObservableCollection<DefectCustomAlgorithmItem>
            {
                new DefectCustomAlgorithmItem(
                    DefectCustomAlgorithmKeys.SheetSizeJudge,
                    "片材尺寸判定",
                    isSheetSizeJudgeEnabled)
            };
        }

        private void AttachCustomAlgorithmEvents(ObservableCollection<DefectCustomAlgorithmItem> items)
        {
            if (items == null)
            {
                return;
            }

            items.CollectionChanged += OnCustomAlgorithmItemsCollectionChanged;
            foreach (DefectCustomAlgorithmItem item in items.Where(item => item != null))
            {
                item.PropertyChanged += OnCustomAlgorithmItemPropertyChanged;
            }
        }

        private void DetachCustomAlgorithmEvents(ObservableCollection<DefectCustomAlgorithmItem> items)
        {
            if (items == null)
            {
                return;
            }

            items.CollectionChanged -= OnCustomAlgorithmItemsCollectionChanged;
            foreach (DefectCustomAlgorithmItem item in items.Where(item => item != null))
            {
                item.PropertyChanged -= OnCustomAlgorithmItemPropertyChanged;
            }
        }

        private void AttachSheetSizeJudgeEvents(SheetSizeJudgeConfig config)
        {
            if (config != null)
            {
                config.PropertyChanged += OnSheetSizeJudgePropertyChanged;
            }
        }

        private void DetachSheetSizeJudgeEvents(SheetSizeJudgeConfig config)
        {
            if (config != null)
            {
                config.PropertyChanged -= OnSheetSizeJudgePropertyChanged;
            }
        }

        private void OnCustomAlgorithmItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSynchronizingCustomAlgorithmState)
            {
                return;
            }

            if (e.OldItems != null)
            {
                foreach (DefectCustomAlgorithmItem item in e.OldItems.OfType<DefectCustomAlgorithmItem>())
                {
                    item.PropertyChanged -= OnCustomAlgorithmItemPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (DefectCustomAlgorithmItem item in e.NewItems.OfType<DefectCustomAlgorithmItem>())
                {
                    item.PropertyChanged += OnCustomAlgorithmItemPropertyChanged;
                }
            }

            ObservableCollection<DefectCustomAlgorithmItem> normalizedItems = NormalizeCustomAlgorithmItems(CustomAlgorithmItems);
            if (CustomAlgorithmItems == null
                || CustomAlgorithmItems.Count != normalizedItems.Count
                || CustomAlgorithmItems.Where((item, index) =>
                    !string.Equals(item?.AlgorithmKey, normalizedItems[index].AlgorithmKey, StringComparison.Ordinal)).Any())
            {
                _isSynchronizingCustomAlgorithmState = true;
                try
                {
                    DetachCustomAlgorithmEvents(_customAlgorithmItems);
                    _customAlgorithmItems = normalizedItems;
                    AttachCustomAlgorithmEvents(_customAlgorithmItems);
                    RaisePropertyChanged(nameof(CustomAlgorithmItems));
                }
                finally
                {
                    _isSynchronizingCustomAlgorithmState = false;
                }
            }

            SynchronizeSheetSizeJudgeItemFromConfig(markDirty: false);
            OnCustomAlgorithmStateChanged();
        }

        private void OnCustomAlgorithmItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizingCustomAlgorithmState)
            {
                return;
            }

            DefectCustomAlgorithmItem item = sender as DefectCustomAlgorithmItem;
            if (item == null)
            {
                return;
            }

            if (string.Equals(item.AlgorithmKey, DefectCustomAlgorithmKeys.SheetSizeJudge, StringComparison.Ordinal)
                && string.Equals(e.PropertyName, nameof(DefectCustomAlgorithmItem.IsEnabled), StringComparison.Ordinal))
            {
                SynchronizeSheetSizeJudgeConfigFromItem(item, markDirty: false);
            }

            OnCustomAlgorithmStateChanged();
        }

        private void OnSheetSizeJudgePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizingCustomAlgorithmState)
            {
                return;
            }

            if (string.Equals(e.PropertyName, nameof(SheetSizeJudgeConfig.IsEnabled), StringComparison.Ordinal))
            {
                SynchronizeSheetSizeJudgeItemFromConfig(markDirty: false);
            }

            OnCustomAlgorithmStateChanged();
        }

        private void SynchronizeSheetSizeJudgeConfigFromItem(DefectCustomAlgorithmItem item, bool markDirty)
        {
            if (_isSynchronizingCustomAlgorithmState || item == null || SheetSizeJudge == null)
            {
                return;
            }

            _isSynchronizingCustomAlgorithmState = true;
            try
            {
                SheetSizeJudge.IsEnabled = item.IsEnabled;
            }
            finally
            {
                _isSynchronizingCustomAlgorithmState = false;
            }

            if (markDirty)
            {
                OnCustomAlgorithmStateChanged();
            }
        }

        private void SynchronizeSheetSizeJudgeItemFromConfig(bool markDirty)
        {
            if (_isSynchronizingCustomAlgorithmState)
            {
                return;
            }

            DefectCustomAlgorithmItem item = GetSheetSizeJudgeItem();
            if (item == null || SheetSizeJudge == null)
            {
                return;
            }

            _isSynchronizingCustomAlgorithmState = true;
            try
            {
                item.IsEnabled = SheetSizeJudge.IsEnabled;
            }
            finally
            {
                _isSynchronizingCustomAlgorithmState = false;
            }

            if (markDirty)
            {
                OnCustomAlgorithmStateChanged();
            }
        }

        private DefectCustomAlgorithmItem GetSheetSizeJudgeItem()
        {
            return CustomAlgorithmItems?.FirstOrDefault(item =>
                string.Equals(item?.AlgorithmKey, DefectCustomAlgorithmKeys.SheetSizeJudge, StringComparison.Ordinal));
        }

        public void OpenCustomAlgorithmSubpage(string algorithmKey)
        {
            SelectedCustomAlgorithmKey = string.IsNullOrWhiteSpace(algorithmKey)
                ? DefectCustomAlgorithmKeys.SheetSizeJudge
                : algorithmKey;
            IsCustomAlgorithmPopupOpen = true;
        }

        public void CloseCustomAlgorithmSubpage()
        {
            IsCustomAlgorithmPopupOpen = false;
        }

        private void OnCustomAlgorithmStateChanged()
        {
            MarkSchemeDirty();

            if (SourceResults != null && SourceResults.Count > 0)
            {
                ApplyPostProcessResultsAndRefreshPreview();
            }
        }


        /// <summary>
        /// 从用户选择的模型配置 JSON 中导入缺陷类别，类别表只归属当前后处理方案。
        /// </summary>
        public bool ImportDefectClassDefinitionsFromFile(string filePath, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                message = "请选择有效的模型配置文件。";
                return false;
            }

            if (!TryLoadDefectClassDefinitionsFromFile(filePath, out List<DefectClassDefinition> definitions, out string error))
            {
                message = $"导入缺陷类别失败：{error}";
                return false;
            }

            int changedCount = MergeConfiguredDefectDefinitions(definitions);
            RefreshDefectClassDefinitionsAfterEdit(markDirty: changedCount > 0);
            message = changedCount > 0
                ? $"已导入 {changedCount} 个缺陷类别。"
                : "配置文件中的缺陷类别与当前方案一致。";
            return true;
        }

        /// <summary>
        /// 手动新增或更新一个缺陷类别。
        /// </summary>
        public bool AddManualDefectClass(out string message)
        {
            message = string.Empty;
            string className = ManualDefectClassName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(className))
            {
                message = "请输入缺陷类别名称。";
                return false;
            }

            if (!TryResolveManualDefectClassId(out int classId, out message))
            {
                return false;
            }

            int changedCount = MergeConfiguredDefectDefinitions(new[]
            {
                new DefectClassDefinition
                {
                    ClassId = classId,
                    ClassName = className
                }
            });

            RefreshDefectClassDefinitionsAfterEdit(markDirty: changedCount > 0);
            ManualDefectClassIdText = GetNextDefectClassId().ToString(CultureInfo.InvariantCulture);
            ManualDefectClassName = string.Empty;
            message = changedCount > 0
                ? $"已新增或更新缺陷类别：{classId} - {className}。"
                : "当前缺陷类别已存在，无需重复新增。";
            return true;
        }

        /// <summary>
        /// 从当前方案类别表中删除选中的缺陷类别，不删除已保存的规则配置。
        /// </summary>
        public bool DeleteCurrentDefectClass(out string message)
        {
            message = string.Empty;
            if (CurrentDefect == null)
            {
                message = "请先选择要删除的缺陷类别。";
                return false;
            }

            int deletedClassId = CurrentDefect.ClassId;
            string deletedClassName = CurrentDefect.ClassName;
            _configuredDefectDefinitions ??= new List<DefectClassDefinition>();
            int removedCount = _configuredDefectDefinitions.RemoveAll(item => item != null && item.ClassId == deletedClassId);
            if (removedCount <= 0)
            {
                message = "当前缺陷类别来自推理结果，不在方案类别表中。";
                return false;
            }

            RefreshDefectClassDefinitionsAfterEdit(markDirty: true);
            ManualDefectClassIdText = GetNextDefectClassId().ToString(CultureInfo.InvariantCulture);
            message = $"已删除方案类别：{deletedClassId} - {deletedClassName}。";
            return true;
        }

        /// <summary>
        /// 从方案或导入结果设置当前类别表。
        /// </summary>
        private void SetConfiguredDefectDefinitions(IEnumerable<DefectClassDefinition> definitions, bool refreshUi, bool markDirty)
        {
            _configuredDefectDefinitions = CloneDefectClassDefinitions(definitions);
            RefreshConfiguredClassNameMap();
            ManualDefectClassIdText = GetNextDefectClassId().ToString(CultureInfo.InvariantCulture);
            if (!refreshUi)
            {
                return;
            }

            RefreshDefectClassDefinitionsAfterEdit(markDirty);
        }

        /// <summary>
        /// 合并导入或手动创建的类别，同 ClassId 时更新名称。
        /// </summary>
        private int MergeConfiguredDefectDefinitions(IEnumerable<DefectClassDefinition> definitions)
        {
            int changedCount = 0;
            Dictionary<int, DefectClassDefinition> mergedDefinitions = CloneDefectClassDefinitions(_configuredDefectDefinitions)
                .ToDictionary(item => item.ClassId, item => item);

            foreach (DefectClassDefinition definition in CloneDefectClassDefinitions(definitions))
            {
                if (mergedDefinitions.TryGetValue(definition.ClassId, out DefectClassDefinition existingDefinition))
                {
                    if (!string.Equals(existingDefinition.ClassName, definition.ClassName, StringComparison.Ordinal))
                    {
                        existingDefinition.ClassName = definition.ClassName;
                        changedCount++;
                    }

                    continue;
                }

                mergedDefinitions.Add(definition.ClassId, definition);
                changedCount++;
            }

            _configuredDefectDefinitions = mergedDefinitions.Values
                .OrderBy(item => item.ClassId)
                .ToList();
            return changedCount;
        }

        /// <summary>
        /// 类别表变化后同步名称映射、缺陷列表和预览状态。
        /// </summary>
        private void RefreshDefectClassDefinitionsAfterEdit(bool markDirty)
        {
            RefreshConfiguredClassNameMap();
            ApplyConfiguredClassNames(SourceResults);
            BuildDefectItems();
            RequestEditingStateRefresh();
            RequestPreviewRefresh();
            if (markDirty)
            {
                MarkSchemeDirty();
            }
        }

        private void RefreshConfiguredClassNameMap()
        {
            _configuredDefectDefinitions = CloneDefectClassDefinitions(_configuredDefectDefinitions);
            _configuredClassNameMap = _configuredDefectDefinitions
                .GroupBy(item => item.ClassId)
                .ToDictionary(group => group.Key, group => group.First().ClassName);
        }

        private bool TryResolveManualDefectClassId(out int classId, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(ManualDefectClassIdText))
            {
                classId = GetNextDefectClassId();
                return true;
            }

            if (!int.TryParse(ManualDefectClassIdText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out classId)
                || classId < 0)
            {
                message = "缺陷类别 ID 必须是非负整数。";
                return false;
            }

            return true;
        }

        private int GetNextDefectClassId()
        {
            int maxConfiguredId = _configuredDefectDefinitions?.Count > 0
                ? _configuredDefectDefinitions.Max(item => item.ClassId)
                : -1;
            int maxResultId = SourceResults?.Count > 0
                ? SourceResults.Max(item => item?.ClassId ?? -1)
                : -1;
            return Math.Max(maxConfiguredId, maxResultId) + 1;
        }

        private bool TryGetConfiguredClassName(int classId, out string className)
        {
            className = string.Empty;
            if (_configuredClassNameMap == null
                || !_configuredClassNameMap.TryGetValue(classId, out string configuredClassName)
                || string.IsNullOrWhiteSpace(configuredClassName))
            {
                return false;
            }

            className = configuredClassName;
            return true;
        }

        private void ApplyConfiguredClassNames(IEnumerable<Result> results)
        {
            if (results == null || _configuredClassNameMap == null || _configuredClassNameMap.Count == 0)
            {
                return;
            }

            foreach (Result result in results.Where(item => item != null))
            {
                if (TryGetConfiguredClassName(result.ClassId, out string className))
                {
                    result.ClassName = className;
                }
            }
        }

        private static bool TryLoadDefectClassDefinitionsFromFile(string filePath, out List<DefectClassDefinition> definitions, out string error)
        {
            definitions = new List<DefectClassDefinition>();
            error = string.Empty;

            try
            {
                return TryLoadDefectClassDefinitionsJson(File.ReadAllText(filePath), out definitions, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryLoadDefectClassDefinitionsJson(string definitionsJson, out List<DefectClassDefinition> definitions, out string error)
        {
            definitions = new List<DefectClassDefinition>();
            error = string.Empty;

            try
            {
                JToken rootToken = JToken.Parse(definitionsJson);
                List<DefectClassDefinition> loadedDefinitions = new List<DefectClassDefinition>();

                CollectDefectClassDefinitions(rootToken.SelectTokens("$..categories[*]"), loadedDefinitions);
                CollectDefectClassDefinitions(rootToken.SelectTokens("$..defect_list[*]"), loadedDefinitions);
                if (rootToken is JArray rootArray)
                {
                    CollectDefectClassDefinitions(rootArray.Children(), loadedDefinitions);
                }

                definitions = CloneDefectClassDefinitions(loadedDefinitions);
                if (definitions.Count == 0)
                {
                    error = "未找到有效的 categories 或 defect_list 类别定义。";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                definitions = new List<DefectClassDefinition>();
                error = ex.Message;
                return false;
            }
        }

        private static void CollectDefectClassDefinitions(IEnumerable<JToken> tokens, ICollection<DefectClassDefinition> definitions)
        {
            if (tokens == null || definitions == null)
            {
                return;
            }

            int fallbackClassId = definitions.Count == 0
                ? 0
                : definitions.Max(item => item.ClassId) + 1;
            foreach (JToken token in tokens)
            {
                switch (token)
                {
                    case JObject definitionObject:
                        {
                            string className = GetDefectClassName(definitionObject);
                            if (string.IsNullOrWhiteSpace(className))
                            {
                                continue;
                            }

                            int classId = TryGetDefectClassId(definitionObject, out int resolvedClassId)
                                ? resolvedClassId
                                : fallbackClassId++;

                            definitions.Add(new DefectClassDefinition
                            {
                                ClassId = classId,
                                ClassName = className
                            });
                            break;
                        }
                    case JValue value when value.Type == JTokenType.String:
                        {
                            string className = value.Value<string>()?.Trim();
                            if (string.IsNullOrWhiteSpace(className))
                            {
                                continue;
                            }

                            definitions.Add(new DefectClassDefinition
                            {
                                ClassId = fallbackClassId++,
                                ClassName = className
                            });
                            break;
                        }
                }
            }
        }

        private static bool TryGetDefectClassId(JObject definitionObject, out int classId)
        {
            classId = 0;
            return TryConvertTokenToInt(GetFirstExistingPropertyToken(definitionObject, "id", "class_id", "classId", "label"), out classId);
        }

        private static string GetDefectClassName(JObject definitionObject)
        {
            JToken nameToken = GetFirstExistingPropertyToken(definitionObject, "name", "class_name", "className", "label_name");
            return nameToken?.Type == JTokenType.String
                ? nameToken.Value<string>()?.Trim() ?? string.Empty
                : nameToken?.ToString().Trim() ?? string.Empty;
        }

        private static JToken GetFirstExistingPropertyToken(JObject source, params string[] propertyNames)
        {
            if (source == null || propertyNames == null)
            {
                return null;
            }

            foreach (string propertyName in propertyNames.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                if (TryGetPropertyToken(source, propertyName, out JToken token))
                {
                    return token;
                }
            }

            return null;
        }

        private static bool TryConvertTokenToInt(JToken sourceToken, out int value)
        {
            value = 0;
            if (sourceToken == null)
            {
                return false;
            }

            switch (sourceToken.Type)
            {
                case JTokenType.Integer:
                    value = sourceToken.Value<int>();
                    return true;
                case JTokenType.Float:
                    value = (int)sourceToken.Value<double>();
                    return true;
                case JTokenType.String:
                    return int.TryParse(sourceToken.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }


        /// <summary>
        /// 构建缺陷列表项。
        /// </summary>
        private void BuildDefectItems()
        {
            RunOnDispatcher(() =>
            {
                string currentRuleKey = CurrentDefect == null
                    ? string.Empty
                    : GetDefectRuleKey(CurrentDefect.ClassId, ResolveClassName(CurrentDefect.ClassId, CurrentDefect.ClassName));
                DefectItems.Clear();
                bool hasRuleConfigChanged = false;

                List<Result> processableResults = GetProcessableResults(SourceResults);
                Dictionary<string, DefectItem> defectMap = new Dictionary<string, DefectItem>();

                foreach (DefectClassDefinition definition in GetConfiguredDefectDefinitions())
                {
                    string className = ResolveClassName(definition.ClassId, definition.ClassName);
                    string ruleKey = GetDefectRuleKey(definition.ClassId, className);
                    if (defectMap.ContainsKey(ruleKey))
                    {
                        continue;
                    }

                    DefectItem defectItem = new DefectItem
                    {
                        Index = DefectItems.Count + 1,
                        ClassId = definition.ClassId,
                        ClassName = className,
                        Count = 0,
                        MinimumConfidence = 0
                    };

                    defectMap.Add(ruleKey, defectItem);
                    DefectItems.Add(defectItem);

                    int configCountBefore = DefectRuleConfigs.Count;
                    GetOrCreateRuleConfig(defectItem);
                    hasRuleConfigChanged |= DefectRuleConfigs.Count != configCountBefore;
                }

                foreach (Result item in processableResults)
                {
                    if (HasConfiguredDefectDefinitions() && !IsDefinedBySchemeClassDefinition(item))
                    {
                        continue;
                    }

                    string className = ResolveClassName(item.ClassId, item.ClassName);
                    string ruleKey = GetDefectRuleKey(item.ClassId, className);
                    if (!defectMap.TryGetValue(ruleKey, out DefectItem defectItem))
                    {
                        defectItem = new DefectItem
                        {
                            Index = DefectItems.Count + 1,
                            ClassId = item.ClassId,
                            ClassName = className,
                            Count = 0,
                            MinimumConfidence = item.Confidence
                        };

                        defectMap.Add(ruleKey, defectItem);
                        DefectItems.Add(defectItem);
                    }

                    defectItem.Count++;
                    defectItem.MinimumConfidence = Math.Min(defectItem.MinimumConfidence, item.Confidence);
                    defectItem.ResultItems.Add(item);
                    int configCountBefore = DefectRuleConfigs.Count;
                    GetOrCreateRuleConfig(defectItem);
                    hasRuleConfigChanged |= DefectRuleConfigs.Count != configCountBefore;
                }

                foreach (DefectItem defectItem in DefectItems)
                {
                    DefectRuleConfig ruleConfig = GetOrCreateRuleConfig(defectItem);
                    List<Result> mergedResults = defectItem.ResultItems.Count > 0
                        ? GroupResultsByImageIndex(defectItem.ResultItems)
                            .SelectMany(imageGroup => BuildMergedResultsByNms(imageGroup, ruleConfig))
                            .ToList()
                        : new List<Result>();
                    defectItem.Count = mergedResults.Count;
                    defectItem.MinimumConfidence = mergedResults.Count > 0
                        ? mergedResults.Min(item => item?.Confidence ?? 0f)
                        : 0f;
                }

                CurrentDefect = FindDefectItemByRuleKey(currentRuleKey) ?? DefectItems.FirstOrDefault();
                if (CurrentDefect == null)
                {
                    LoadCurrentDefectRule();
                    ClearCurrentDefectSummary();
                }

                if (hasRuleConfigChanged)
                {
                    MarkSchemeDirty();
                }
            });
        }

        /// <summary>
        /// 规范化类别名称。
        /// </summary>
        private static string NormalizeClassName(string className, int classId)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return $"ClassId:{classId}";
            }

            return className.Trim();
        }

        /// <summary>
        /// 优先使用当前方案维护的缺陷类别名称。
        /// </summary>
        private string ResolveClassName(int classId, string className)
        {
            return TryGetConfiguredClassName(classId, out string configuredClassName)
                ? configuredClassName
                : NormalizeClassName(className, classId);
        }

        private bool HasConfiguredDefectDefinitions()
        {
            return _configuredClassNameMap != null && _configuredClassNameMap.Count > 0;
        }

        private bool IsDefinedBySchemeClassDefinition(Result result)
        {
            return result != null
                && _configuredClassNameMap != null
                && _configuredClassNameMap.ContainsKey(result.ClassId);
        }

        /// <summary>
        /// 更新当前缺陷摘要信息。
        /// </summary>
        private void UpdateCurrentDefectSummary()
        {
            if (CurrentDefect == null)
            {
                ClearCurrentDefectSummary();
                return;
            }

            RuleName = CurrentDefect.ClassName;
            DefectName = CurrentDefect.ClassName;
            SourceClassId = CurrentDefect.ClassId;
            OutputClassId = CurrentDefect.ClassId;
            SourceClassName = CurrentDefect.ClassName;
            OutputClassName = CurrentDefect.ClassName;
            CurrentMinimumConfidence = Math.Round(CurrentDefect.MinimumConfidence, 4);
        }

        /// <summary>
        /// 清空当前缺陷摘要信息。
        /// </summary>
        private void ClearCurrentDefectSummary()
        {
            RuleName = string.Empty;
            DefectName = string.Empty;
            SourceClassId = 0;
            OutputClassId = 0;
            SourceClassName = string.Empty;
            OutputClassName = string.Empty;
            CurrentMinimumConfidence = 0;
            MinimumConfidence = 0;
        }

        /// <summary>
        /// 获取当前方案内维护的缺陷类别定义。
        /// </summary>
        private IReadOnlyList<DefectClassDefinition> GetConfiguredDefectDefinitions()
        {
            return _configuredDefectDefinitions;
        }

        /// <summary>
        /// 根据 RuleKey 或 ClassId 查找缺陷项，兼容方案类别名调整后的规则键变化。
        /// </summary>
        private DefectItem FindDefectItemByRuleKey(string ruleKey)
        {
            if (DefectItems == null || DefectItems.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(ruleKey))
            {
                DefectItem exactMatch = DefectItems.FirstOrDefault(item => GetDefectRuleKey(item) == ruleKey);
                if (exactMatch != null)
                {
                    return exactMatch;
                }

                if (TryParseRuleKeyClassId(ruleKey, out int classId))
                {
                    return DefectItems.FirstOrDefault(item => item.ClassId == classId);
                }
            }

            return null;
        }

        private static bool TryParseRuleKeyClassId(string ruleKey, out int classId)
        {
            classId = 0;
            if (string.IsNullOrWhiteSpace(ruleKey))
            {
                return false;
            }

            string[] parts = ruleKey.Split(new[] { "__" }, 2, StringSplitOptions.None);
            return parts.Length > 0 && int.TryParse(parts[0], out classId);
        }
        #endregion

    }

    #region 支持模型

    /// <summary>
    /// 定义缺陷后处理规则支持的特征键。
    /// </summary>
    public static class DefectFeatureKeys
    {
        public const string Length = "Length";
        public const string Width = "Width";
        public const string Area = "Area";
    }

    /// <summary>
    /// 定义 DefectPostProcess 写入 Result.Others 的扩展字段键名。
    /// </summary>
    public static class DefectPostProcessResultKeys
    {
        public const string WorldX = "DefectPostProcess.WorldX";
        public const string WorldY = "DefectPostProcess.WorldY";
        public const string WorldZ = "DefectPostProcess.WorldZ";
        public const string CoordinateSource = "DefectPostProcess.CoordinateSource";
        public const string ActualLength = "DefectPostProcess.ActualLength";
        public const string ActualWidth = "DefectPostProcess.ActualWidth";
        public const string ActualArea = "DefectPostProcess.ActualArea";
        public const string InstanceIndex = "DefectPostProcess.InstanceIndex";
        public const string ImageIndex = "DefectPostProcess.ImageIndex";
        public const string DefectJudgeIsOk = "DefectJudgeIsOk";
        public const string SheetSizeJudgeIsOk = "SheetSizeJudgeIsOk";
        public const string FinalJudgeIsOk = "FinalJudgeIsOk";
    }

    public static class DefectCustomAlgorithmKeys
    {
        public const string SheetSizeJudge = "SheetSizeJudge";
    }

    [Serializable]
    public class DefectCustomAlgorithmItem : BindableBase
    {
        private string _algorithmKey = string.Empty;
        private string _displayName = string.Empty;
        private bool _isEnabled;

        public string AlgorithmKey
        {
            get { return _algorithmKey; }
            set { SetProperty(ref _algorithmKey, value); }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set { SetProperty(ref _displayName, value); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        /// <summary>
        /// 保留给配置反序列化使用。
        /// </summary>
        public DefectCustomAlgorithmItem()
        {
        }

        public DefectCustomAlgorithmItem(string algorithmKey, string displayName, bool isEnabled)
        {
            AlgorithmKey = algorithmKey;
            DisplayName = displayName;
            IsEnabled = isEnabled;
        }
    }

    [Serializable]
    public class SheetSizeJudgeConfig : BindableBase
    {
        private bool _isEnabled;
        private double _threshold = 128d;
        private double _openingRadius = 3.5d;
        private double _rectangularityMinimum = 0.98d;
        private double _standardLength;
        private double _lengthTolerance = 0.5d;
        private double _standardWidth;
        private double _widthTolerance = 0.5d;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        public double Threshold
        {
            get { return _threshold; }
            set { SetProperty(ref _threshold, value); }
        }

        public double OpeningRadius
        {
            get { return _openingRadius; }
            set { SetProperty(ref _openingRadius, value); }
        }

        public double RectangularityMinimum
        {
            get { return _rectangularityMinimum; }
            set { SetProperty(ref _rectangularityMinimum, value); }
        }

        public double StandardLength
        {
            get { return _standardLength; }
            set { SetProperty(ref _standardLength, value); }
        }

        public double LengthTolerance
        {
            get { return _lengthTolerance; }
            set { SetProperty(ref _lengthTolerance, value); }
        }

        public double StandardWidth
        {
            get { return _standardWidth; }
            set { SetProperty(ref _standardWidth, value); }
        }

        public double WidthTolerance
        {
            get { return _widthTolerance; }
            set { SetProperty(ref _widthTolerance, value); }
        }
    }

    /// <summary>
    /// 表示 DefectPostProcess 模块保存的输入链接元数据快照。
    /// </summary>
    [Serializable]
    public class DefectPostProcessInputBinding
    {
        public bool IsLink { get; set; }

        public Guid LinkGuid { get; set; }

        public int Serial { get; set; } = -1;

        public string ParentNode { get; set; } = string.Empty;

        public Guid Guid { get; set; }

        public ResoureceType Resourece { get; set; } = ResoureceType.None;

        public string Name { get; set; } = string.Empty;

        public string ParamName { get; set; } = string.Empty;

        public DataType Type { get; set; } = DataType.None;

        public string Describe { get; set; } = string.Empty;

        public bool IsGlobal { get; set; }

        public string ResourcePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 表示单个缺陷类别对应的规则配置。
    /// </summary>
    [Serializable]
    public class DefectRuleConfig
    {
        public string RuleKey { get; set; } = string.Empty;

        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public double MinimumConfidence { get; set; }

        public bool IsNmsEnabled { get; set; } = true;

        public double NmsIoUThreshold { get; set; } = 0.5d;

        public ObservableCollection<FeatureThresholdItem> FeatureThresholds { get; set; } = new ObservableCollection<FeatureThresholdItem>();
    }

    /// <summary>
    /// 表示单条特征阈值规则项。
    /// </summary>
    [Serializable]
    public class FeatureThresholdItem : BindableBase
    {
        private static readonly ObservableCollection<string> DefaultRelationOptions = new ObservableCollection<string>
        {
            "与",
            "或"
        };

        private bool _isEnabled;
        private string _featureKey = string.Empty;
        private string _featureName = string.Empty;
        private string _minimumValue = string.Empty;
        private string _maximumValue = string.Empty;
        private string _unit = string.Empty;
        private string _relationOperator = "与";
        private bool _canEditRelation;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        public string FeatureKey
        {
            get { return _featureKey; }
            set { SetProperty(ref _featureKey, value); }
        }

        public string FeatureName
        {
            get { return _featureName; }
            set { SetProperty(ref _featureName, value); }
        }

        public string MinimumValue
        {
            get { return _minimumValue; }
            set { SetProperty(ref _minimumValue, value); }
        }

        public string MaximumValue
        {
            get { return _maximumValue; }
            set { SetProperty(ref _maximumValue, value); }
        }

        public string Unit
        {
            get { return _unit; }
            set { SetProperty(ref _unit, value); }
        }

        public string RelationOperator
        {
            get { return _relationOperator; }
            set { SetProperty(ref _relationOperator, value); }
        }

        [JsonIgnore]
        public bool CanEditRelation
        {
            get { return _canEditRelation; }
            set { SetProperty(ref _canEditRelation, value); }
        }

        [JsonIgnore]
        public ObservableCollection<string> RelationOptions
        {
            get { return DefaultRelationOptions; }
        }

        /// <summary>
        /// 初始化空的特征阈值项。
        /// </summary>
        public FeatureThresholdItem()
        {
        }

        /// <summary>
        /// 使用指定参数初始化特征阈值项。
        /// </summary>
        public FeatureThresholdItem(
            string featureKey,
            string featureName,
            string minimumValue,
            string maximumValue,
            string unit,
            string relationOperator,
            bool canEditRelation)
        {
            FeatureKey = featureKey;
            FeatureName = featureName;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            Unit = unit;
            RelationOperator = relationOperator;
            CanEditRelation = canEditRelation;
        }
    }

    /// <summary>
    /// 表示已保存的缺陷后处理方案。
    /// </summary>
    [Serializable]
    public class DefectPostProcessScheme
    {
        public string Name { get; set; } = string.Empty;

        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        public string SelectedRuleKey { get; set; } = string.Empty;

        public List<DefectRuleConfig> DefectRuleConfigs { get; set; } = new List<DefectRuleConfig>();

        public List<DefectClassDefinition> DefectClassDefinitions { get; set; } = new List<DefectClassDefinition>();

        public SheetSizeJudgeConfig SheetSizeJudge { get; set; } = new SheetSizeJudgeConfig();

        public string CalibrationFilePath { get; set; } = string.Empty;

        public DefectPostProcessInputBinding InputImageBinding { get; set; } = new DefectPostProcessInputBinding();

        public DefectPostProcessInputBinding InputResultsBinding { get; set; } = new DefectPostProcessInputBinding();

        public DefectPostProcessInputBinding InputPixelEquivalentBinding { get; set; } = new DefectPostProcessInputBinding();

        public DefectPostProcessInputBinding InputPixelEquivalentXBinding { get; set; } = new DefectPostProcessInputBinding();

        public DefectPostProcessInputBinding InputPixelEquivalentYBinding { get; set; } = new DefectPostProcessInputBinding();

        public DefectPostProcessInputBinding InputEdgeCalibrationXBinding { get; set; } = new DefectPostProcessInputBinding();

        public bool IsFastModeEnabled { get; set; }
    }

    /// <summary>
    /// 表示方案内维护的缺陷类别定义。
    /// </summary>
    [Serializable]
    public class DefectClassDefinition : BindableBase
    {
        private int _classId;
        private string _className = string.Empty;

        /// <summary>
        /// 模型输出中的类别编号。
        /// </summary>
        public int ClassId
        {
            get { return _classId; }
            set
            {
                if (SetProperty(ref _classId, value))
                {
                    RaisePropertyChanged(nameof(DisplayText));
                }
            }
        }

        /// <summary>
        /// 后处理界面和规则配置中显示的缺陷名称。
        /// </summary>
        public string ClassName
        {
            get { return _className; }
            set
            {
                if (SetProperty(ref _className, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(DisplayText));
                }
            }
        }

        [JsonIgnore]
        public string DisplayText
        {
            get { return $"{ClassId} - {ClassName}"; }
        }
    }

    /// <summary>
    /// 表示主界面中展示的缺陷类别摘要。
    /// </summary>
    [Serializable]
    public class DefectItem
    {
        public int Index { get; set; }

        public int ClassId { get; set; }

        public string ClassName { get; set; }

        public int Count { get; set; }

        public float MinimumConfidence { get; set; }

        public string DisplayName
        {
            get { return ClassName; }
        }

        public string DisplayDetail
        {
            get { return $"ClassId: {ClassId}    Count: {Count}"; }
        }

        [JsonIgnore]
        public List<Result> ResultItems { get; set; } = new List<Result>();
    }

    /// <summary>
    /// 表示单个缺陷实例的测量输出结果。
    /// </summary>
    [Serializable]
    public class DefectMeasurementOutputItem
    {
        /// <summary>
        /// 当前缺陷在同类缺陷中的序号，从 1 开始。
        /// </summary>
        public int InstanceIndex { get; set; }

        /// <summary>
        /// 缺陷类别编号。
        /// </summary>
        public int ClassId { get; set; }

        /// <summary>
        /// 缺陷类别名称。
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// 缺陷置信度。
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 实际坐标和尺寸的计算来源。
        /// </summary>
        public string MeasurementSource { get; set; } = string.Empty;

        /// <summary>
        /// 缺陷中心点的物理 X 坐标，按边缘基准值做横向零点修正。
        /// </summary>
        public double? PhysicalX { get; set; }

        /// <summary>
        /// 缺陷中心点的物理 Y 坐标，从图像上边缘向下递增。
        /// </summary>
        public double? PhysicalY { get; set; }

        /// <summary>
        /// 缺陷中心点的实际高度坐标。
        /// </summary>
        public double? PhysicalZ { get; set; }

        /// <summary>
        /// 实际长度。
        /// </summary>
        public double? Length { get; set; }

        /// <summary>
        /// 实际宽度。
        /// </summary>
        public double? Width { get; set; }

        /// <summary>
        /// 实际面积。
        /// </summary>
        public double? Area { get; set; }
    }

    /// <summary>
    /// 表示缺陷特征值弹窗使用的数据。
    /// </summary>
    [Serializable]
    public class DefectFeatureValueDialogData
    {
        public string CurrentRuleKey { get; set; } = string.Empty;

        public string LengthUnit { get; set; } = "px";

        public string WidthUnit { get; set; } = "px";

        public string AreaUnit { get; set; } = "px^2";

        [JsonIgnore]
        public DefectPostProcessModel SourceModel { get; set; }

        public ObservableCollection<DefectFeatureValueItem> Items { get; set; } = new ObservableCollection<DefectFeatureValueItem>();
    }

    /// <summary>
    /// 表示特征值弹窗中展示的单个缺陷实例。
    /// </summary>
    [Serializable]
    public class DefectFeatureValueItem
    {
        public string RuleKey { get; set; } = string.Empty;

        public string DefectName { get; set; } = string.Empty;

        public int ClassId { get; set; }

        public int InstanceIndex { get; set; }

        public double LengthValue { get; set; }

        public double WidthValue { get; set; }

        public double AreaValue { get; set; }

        /// <summary>
        /// 缺陷中心点的物理 X 坐标。
        /// </summary>
        public double? PhysicalXValue { get; set; }

        /// <summary>
        /// 缺陷中心点的物理 Y 坐标。
        /// </summary>
        public double? PhysicalYValue { get; set; }

        [JsonIgnore]
        public double? ActualXValue
        {
            get { return PhysicalXValue; }
            set { PhysicalXValue = value; }
        }

        [JsonIgnore]
        public double? ActualYValue
        {
            get { return PhysicalYValue; }
            set { PhysicalYValue = value; }
        }

        public double Confidence { get; set; }

        public bool IsMatched { get; set; }

        public string MatchText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 表示特征值弹窗中的缺陷筛选项。
    /// </summary>
    [Serializable]
    public class DefectFeatureFilterOption
    {
        public string RuleKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    }

    #endregion
}

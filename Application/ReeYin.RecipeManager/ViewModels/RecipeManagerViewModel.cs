using Prism.Commands;
using Prism.Dialogs;
using ReeYin.RecipeManager.Models;
using ReeYin.RecipeManager.Services;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using ReeYin_V.Logger;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.RecipeManager.ViewModels
{
    public class RecipeManagerViewModel : DialogViewModelBase
    {
        #region Fields
        private readonly RecipeManagerService _service;
        private readonly RecipeManagerRecipeModel _recipeModel;
        private readonly RecipeManagerNodeModel _nodeModel;
        private readonly RecipeManagerRuntimeSyncModel _runtimeSyncModel;
        private ProjectRecipeConfig _model = new();
        private bool _isInitializing;
        private bool _isUpdatingAudit;
        private bool _isApplyingCurrentState;
        private bool _allowClose;
        public ObservableCollection<ProjectRecipeInfo> Recipes => _model.Recipes;
        #endregion

        #region Properties
        private ProjectRecipeInfo? _selectedRecipe;
        public ProjectRecipeInfo? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    RaisePropertyChanged(nameof(HasSelectedRecipe));
                    RaisePropertyChanged(nameof(DetailVisibility));
                    RaisePropertyChanged(nameof(EmptyStateVisibility));
                    RaisePropertyChanged(nameof(NodeSummary));
                    UpdateParamEditState();
                }
            }
        }

        private ProjectRecipeNodeGroup? _selectedParamGroup;
        public ProjectRecipeNodeGroup? SelectedParamGroup
        {
            get => _selectedParamGroup;
            set
            {
                if (SetProperty(ref _selectedParamGroup, value))
                {
                    RaisePropertyChanged(nameof(CurrentGroupParameters));
                }
            }
        }

        public ObservableCollection<ProjectRecipeNodeGroup> CurrentRecipeGroups
            => SelectedRecipe?.Groups ?? new ObservableCollection<ProjectRecipeNodeGroup>();

        public ObservableCollection<RecipeParamInfo> CurrentGroupParameters
            => _selectedParamGroup?.Parameters ?? new ObservableCollection<RecipeParamInfo>();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    RaisePropertyChanged(nameof(UnsavedChangesText));
                }
            }
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _storageScope = "当前工程 / 内存";
        public string StorageScope
        {
            get => _storageScope;
            set => SetProperty(ref _storageScope, value);
        }

        public bool HasSelectedRecipe => SelectedRecipe != null;
        public string RecipeSummary => Recipes.Count == 0 ? "暂无配方" : $"{Recipes.Count} 个配方";
        public string ActiveRecipeName => _model.CurrentRecipe?.Name ?? "无";
        public string NodeSummary => SelectedRecipe == null
            ? "未选择配方"
            : $"{SelectedRecipe.NodeCount} 个节点组 / {SelectedRecipe.ParameterCount} 个参数";
        public string UnsavedChangesText => HasUnsavedChanges ? "是" : "否";
        public Visibility DetailVisibility => HasSelectedRecipe ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmptyStateVisibility => HasSelectedRecipe ? Visibility.Collapsed : Visibility.Visible;
        #endregion

        #region Constructor
        public RecipeManagerViewModel()
        {
            IRecipeService recipeService = new RecipeService();
            _service = new RecipeManagerService(recipeService);
            _recipeModel = new RecipeManagerRecipeModel();
            _nodeModel = new RecipeManagerNodeModel();
            _runtimeSyncModel = new RecipeManagerRuntimeSyncModel(recipeService, _service, _nodeModel);
            Title = "配方管理";

            LoadCommand = new DelegateCommand(async () => await LoadDataAsync());
            NewRecipeCommand = new DelegateCommand(async () => await CreateRecipeAsync());
            DuplicateRecipeCommand = new DelegateCommand(DuplicateSelectedRecipe, () => HasSelectedRecipe)
                .ObservesProperty(() => HasSelectedRecipe);
            DeleteRecipeCommand = new DelegateCommand(DeleteSelectedRecipe, () => HasSelectedRecipe)
                .ObservesProperty(() => HasSelectedRecipe);
            ActivateRecipeCommand = new DelegateCommand(ActivateSelectedRecipe, () => HasSelectedRecipe)
                .ObservesProperty(() => HasSelectedRecipe);
            EditParamsCommand = new DelegateCommand(EditSelectedRecipeParams, () => HasSelectedRecipe)
                .ObservesProperty(() => HasSelectedRecipe);
            EditPageParameterCommand = new DelegateCommand<RecipeParamInfo>(EditPageParameter);
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            ResetCommand = new DelegateCommand(async () => await ReloadAsync());
            CloseCommand = new DelegateCommand(Close);
        }
        #endregion

        #region Ovverride
        public override bool CanCloseDialog()
        {
            return _allowClose || ConfirmDiscardChanges();
        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand { get; }
        public DelegateCommand NewRecipeCommand { get; }
        public DelegateCommand DuplicateRecipeCommand { get; }
        public DelegateCommand DeleteRecipeCommand { get; }
        public DelegateCommand ActivateRecipeCommand { get; }
        public DelegateCommand EditParamsCommand { get; }
        public DelegateCommand<RecipeParamInfo> EditPageParameterCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ResetCommand { get; }
        public DelegateCommand CloseCommand { get; }
        #endregion

        #region Methods
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载配方...";
                _isInitializing = true;

                await EnsureRecipeNodesInitializedAsync();

                ProjectRecipeConfig loadedModel = await _service.LoadAsync();
                (bool runtimeParamChanged, bool runtimeParamSaved) = await SyncRecipeParametersFromRuntimeAsync(loadedModel);
                BindModel(loadedModel);

                StorageScope = _service.GetStorageScope();
                SelectedRecipe = _model.CurrentRecipe ?? Recipes.FirstOrDefault();
                HasUnsavedChanges = runtimeParamChanged && !runtimeParamSaved;
                StatusMessage = BuildLoadStatusMessage(runtimeParamChanged, runtimeParamSaved);
            }
            catch (Exception ex)
            {
                Logs.LogError($"加载配方失败：{ex.Message}");
                StatusMessage = "加载配方失败。";
                HandyControl.Controls.MessageBox.Show(
                    $"加载配方失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isInitializing = false;
                IsLoading = false;
            }
        }

        private async Task CreateRecipeAsync()
        {
            await EnsureRecipeNodesInitializedAsync();
            ProjectRecipeInfo recipe = _service.CreateRecipe(BuildNextRecipeName("配方"));
            _model.Recipes.Insert(0, recipe);

            if (_model.CurrentRecipeId == Guid.Empty)
            {
                SetCurrentRecipe(recipe, markDirty: false);
            }

            SelectedRecipe = recipe;
            MarkDirty("已新建配方。");
        }

        private void DuplicateSelectedRecipe()
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            ProjectRecipeInfo duplicatedRecipe = SelectedRecipe.CreateCopy(
                _service.GetCurrentOperator(),
                BuildNextRecipeName($"{SelectedRecipe.Name}_副本"));

            _model.Recipes.Insert(0, duplicatedRecipe);
            SelectedRecipe = duplicatedRecipe;
            MarkDirty($"已复制配方“{duplicatedRecipe.Name}”。");
        }

        private void DeleteSelectedRecipe()
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            ProjectRecipeInfo recipeToDelete = SelectedRecipe;
            MessageBoxResult result = HandyControl.Controls.MessageBox.Show(
                $"确定要删除配方“{recipeToDelete.Name}”吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            bool wasCurrent = recipeToDelete.IsCurrent;
            _model.Recipes.Remove(recipeToDelete);

            if (wasCurrent)
            {
                SetCurrentRecipe(_model.Recipes.FirstOrDefault(), markDirty: false);
            }

            SelectedRecipe = _model.CurrentRecipe ?? _model.Recipes.FirstOrDefault();
            MarkDirty($"已删除配方“{recipeToDelete.Name}”。");
        }

        private void ActivateSelectedRecipe()
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            SetCurrentRecipe(SelectedRecipe, markDirty: true);
            StatusMessage = $"当前配方：{SelectedRecipe.Name}";
        }

        private void EditSelectedRecipeParams()
        {
            if (SelectedRecipe == null)
            {
                return;
            }

            PrismProvider.DialogService.ShowDialog("RecipeParamEditView", new DialogParameters
            {
                { "Title", "配方参数编辑" },
                { "Icon", "\ue6b7" },
                { "recipe", SelectedRecipe }
            }, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    TouchRecipe(SelectedRecipe);
                    RaisePropertyChanged(nameof(NodeSummary));
                    MarkDirty($"配方“{SelectedRecipe.Name}”参数已编辑。");
                }
            }, nameof(DialogWindowView));
        }

        private void EditPageParameter(RecipeParamInfo parameter)
        {
            if (parameter == null || !parameter.RequiresPageEditor)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(parameter.EditorPageName))
            {
                HandyControl.Controls.MessageBox.Show("当前参数未配置编辑页面。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ProjectRecipeInfo? ownerRecipe = FindOwnerRecipe(parameter);
            ProjectRecipeNodeGroup? ownerGroup = FindOwnerGroup(parameter);
            if (ownerRecipe == null || ownerGroup == null)
            {
                HandyControl.Controls.MessageBox.Show("未找到当前参数所属的配方节点。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryCreatePageEditorModel(ownerGroup, parameter, out ModelParamBase editorModel, out string errorMessage))
            {
                HandyControl.Controls.MessageBox.Show(errorMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string parameterDisplayName = string.IsNullOrWhiteSpace(parameter.Description) ? parameter.Name : parameter.Description;
            PrismProvider.DialogService.ShowDialog(parameter.EditorPageName, new DialogParameters
            {
                { "Title", $"{ownerGroup.DisplayName} - {parameterDisplayName}" },
                { "Icon", "\ue6b7" },
                { "Param", editorModel }
            }, result =>
            {
                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                ModelParamBase editedModel = result.Parameters?.GetValue<object>("Param") as ModelParamBase ?? editorModel;
                if (!TryUpdateParameterValueFromEditorModel(parameter, editedModel, out string updateError))
                {
                    HandyControl.Controls.MessageBox.Show(updateError, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = $"参数“{parameterDisplayName}”已更新。";
            }, nameof(DialogWindowView));
        }

        private async Task SaveAsync()
        {
            try
            {
                if (!TryPrepareForSave(out string validationMessage))
                {
                    StatusMessage = validationMessage;
                    HandyControl.Controls.MessageBox.Show(validationMessage, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                StatusMessage = "正在保存配方...";

                _model.LastUpdatedAt = DateTime.Now;
                var (success, message, storageScope) = await _service.SaveAsync(_model);
                StorageScope = storageScope;

                if (!success)
                {
                    StatusMessage = message;
                    HandyControl.Controls.MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                HasUnsavedChanges = false;
                StatusMessage = message;
                HandyControl.Controls.MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存配方失败：{ex.Message}");
                StatusMessage = "保存配方失败。";
                HandyControl.Controls.MessageBox.Show(
                    $"保存配方失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReloadAsync()
        {
            if (!ConfirmDiscardChanges())
            {
                return;
            }

            await LoadDataAsync();
        }

        private void Close()
        {
            if (!ConfirmDiscardChanges())
            {
                return;
            }

            _allowClose = true;
            PrismProvider.Dispatcher.Invoke(() =>
            {
                // 加载主界面
                PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                // 导航到主区域
                PrismProvider.RegionManager.RequestNavigate("MainRegion", "MainView");
            });
        }

        private bool ConfirmDiscardChanges()
        {
            if (!HasUnsavedChanges)
            {
                return true;
            }

            MessageBoxResult result = HandyControl.Controls.MessageBox.Show(
                "确定要放弃未保存的配方修改吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private void BindModel(ProjectRecipeConfig model)
        {
            UnbindModel();

            _model = model ?? new ProjectRecipeConfig();
            _model.EnsureValidState();
            RefreshParameterDefinitionsFromRuntime();
            _model.Recipes.CollectionChanged += Recipes_CollectionChanged;

            foreach (ProjectRecipeInfo recipe in _model.Recipes)
            {
                TrackRecipe(recipe);
            }

            RaisePropertyChanged(nameof(Recipes));
            RaisePropertyChanged(nameof(RecipeSummary));
            RaisePropertyChanged(nameof(ActiveRecipeName));
            RaisePropertyChanged(nameof(NodeSummary));
            RaisePropertyChanged(nameof(DetailVisibility));
            RaisePropertyChanged(nameof(EmptyStateVisibility));
        }

        private void UnbindModel()
        {
            if (_model?.Recipes == null)
            {
                return;
            }

            _model.Recipes.CollectionChanged -= Recipes_CollectionChanged;
            foreach (ProjectRecipeInfo recipe in _model.Recipes)
            {
                UntrackRecipe(recipe);
            }
        }

        private void Recipes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ProjectRecipeInfo recipe in e.NewItems.OfType<ProjectRecipeInfo>())
                {
                    TrackRecipe(recipe);
                }
            }

            if (e.OldItems != null)
            {
                foreach (ProjectRecipeInfo recipe in e.OldItems.OfType<ProjectRecipeInfo>())
                {
                    UntrackRecipe(recipe);
                }
            }

            RaisePropertyChanged(nameof(RecipeSummary));
            RaisePropertyChanged(nameof(ActiveRecipeName));
            RaisePropertyChanged(nameof(EmptyStateVisibility));
        }

        private void TrackRecipe(ProjectRecipeInfo recipe)
        {
            recipe.PropertyChanged += Recipe_PropertyChanged;
            recipe.Groups.CollectionChanged += Groups_CollectionChanged;

            foreach (ProjectRecipeNodeGroup group in recipe.Groups)
            {
                TrackGroup(group);
            }
        }

        private void UntrackRecipe(ProjectRecipeInfo recipe)
        {
            recipe.PropertyChanged -= Recipe_PropertyChanged;
            recipe.Groups.CollectionChanged -= Groups_CollectionChanged;

            foreach (ProjectRecipeNodeGroup group in recipe.Groups)
            {
                UntrackGroup(group);
            }
        }

        private void TrackGroup(ProjectRecipeNodeGroup group)
        {
            group.PropertyChanged += Group_PropertyChanged;
            group.Parameters.CollectionChanged += Parameters_CollectionChanged;

            foreach (RecipeParamInfo parameter in group.Parameters)
            {
                TrackParameter(parameter);
            }
        }

        private void UntrackGroup(ProjectRecipeNodeGroup group)
        {
            group.PropertyChanged -= Group_PropertyChanged;
            group.Parameters.CollectionChanged -= Parameters_CollectionChanged;

            foreach (RecipeParamInfo parameter in group.Parameters)
            {
                UntrackParameter(parameter);
            }
        }

        private void TrackParameter(RecipeParamInfo parameter)
        {
            parameter.PropertyChanged += Parameter_PropertyChanged;
        }

        private void UntrackParameter(RecipeParamInfo parameter)
        {
            parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        private void Recipe_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInitializing || _isUpdatingAudit || sender is not ProjectRecipeInfo recipe)
            {
                return;
            }

            if (_isApplyingCurrentState && e.PropertyName == nameof(ProjectRecipeInfo.IsCurrent))
            {
                RaisePropertyChanged(nameof(ActiveRecipeName));
                return;
            }

            if (e.PropertyName is nameof(ProjectRecipeInfo.ModifiedAt) or
                nameof(ProjectRecipeInfo.LastModifiedBy) or
                nameof(ProjectRecipeInfo.NodeCount) or
                nameof(ProjectRecipeInfo.ParameterCount))
            {
                RaisePropertyChanged(nameof(NodeSummary));
                return;
            }

            TouchRecipe(recipe);
            RaisePropertyChanged(nameof(RecipeSummary));
            RaisePropertyChanged(nameof(ActiveRecipeName));
            MarkDirty();
        }

        private void Groups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ProjectRecipeNodeGroup group in e.NewItems.OfType<ProjectRecipeNodeGroup>())
                {
                    TrackGroup(group);
                }
            }

            if (e.OldItems != null)
            {
                foreach (ProjectRecipeNodeGroup group in e.OldItems.OfType<ProjectRecipeNodeGroup>())
                {
                    UntrackGroup(group);
                }
            }

            RaisePropertyChanged(nameof(NodeSummary));
        }

        private void Group_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInitializing)
            {
                return;
            }

            if (e.PropertyName == nameof(ProjectRecipeNodeGroup.ParameterCount))
            {
                RaisePropertyChanged(nameof(NodeSummary));
            }
        }

        private void Parameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (RecipeParamInfo parameter in e.NewItems.OfType<RecipeParamInfo>())
                {
                    TrackParameter(parameter);
                }
            }

            if (e.OldItems != null)
            {
                foreach (RecipeParamInfo parameter in e.OldItems.OfType<RecipeParamInfo>())
                {
                    UntrackParameter(parameter);
                }
            }

            RaisePropertyChanged(nameof(NodeSummary));
        }

        private void Parameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInitializing || _isUpdatingAudit || sender is not RecipeParamInfo parameter)
            {
                return;
            }

            ProjectRecipeInfo? owner = FindOwnerRecipe(parameter);
            if (owner != null)
            {
                TouchRecipe(owner);
            }

            RaisePropertyChanged(nameof(NodeSummary));
            MarkDirty();
        }

        private ProjectRecipeInfo? FindOwnerRecipe(RecipeParamInfo parameter)
        {
            return _model.Recipes.FirstOrDefault(recipe =>
                recipe.Groups.Any(group => group.Parameters.Contains(parameter)));
        }

        private ProjectRecipeNodeGroup? FindOwnerGroup(RecipeParamInfo parameter)
        {
            return _model.Recipes
                .SelectMany(recipe => recipe.Groups)
                .FirstOrDefault(group => group.Parameters.Contains(parameter));
        }

        private void SetCurrentRecipe(ProjectRecipeInfo? recipe, bool markDirty)
        {
            _isApplyingCurrentState = true;
            _model.SetCurrentRecipe(recipe?.Id ?? Guid.Empty);
            _isApplyingCurrentState = false;

            if (recipe != null)
            {
                TouchRecipe(recipe);
            }

            RaisePropertyChanged(nameof(ActiveRecipeName));
            RaisePropertyChanged(nameof(RecipeSummary));

            if (markDirty)
            {
                MarkDirty("当前配方已切换。");
            }
        }

        private void TouchRecipe(ProjectRecipeInfo recipe)
        {
            _isUpdatingAudit = true;
            recipe.ModifiedAt = DateTime.Now;
            recipe.LastModifiedBy = _service.GetCurrentOperator();
            _isUpdatingAudit = false;

            RaisePropertyChanged(nameof(RecipeSummary));
            RaisePropertyChanged(nameof(NodeSummary));
            RaisePropertyChanged(nameof(ActiveRecipeName));
        }

        private void MarkDirty(string message = null)
        {
            HasUnsavedChanges = true;
            StatusMessage = string.IsNullOrWhiteSpace(message) ? "存在未保存的更改。" : message;
        }

        private bool TryPrepareForSave(out string message)
        {
            return _recipeModel.TryPrepareForSave(_model, _service.GetCurrentOperator(), out message);
        }

        private string BuildNextRecipeName(string prefix)
        {
            return _recipeModel.BuildNextRecipeName(_model, prefix);
        }

        private async Task<(bool changed, bool saved)> SyncRecipeParametersFromRuntimeAsync(ProjectRecipeConfig config)
        {
            bool previousUpdatingAudit = _isUpdatingAudit;
            _isUpdatingAudit = true;
            try
            {
                RecipeRuntimeSyncResult result = await _runtimeSyncModel.SyncRecipeParametersFromRuntimeAsync(config);
                if (!string.IsNullOrWhiteSpace(result.StorageScope))
                {
                    StorageScope = result.StorageScope;
                }

                return (result.Changed, result.Saved);
            }
            finally
            {
                _isUpdatingAudit = previousUpdatingAudit;
            }
        }

        private string BuildLoadStatusMessage(bool runtimeParamChanged, bool runtimeParamSaved)
        {
            return RecipeManagerRuntimeSyncModel.BuildLoadStatusMessage(Recipes.Count, runtimeParamChanged, runtimeParamSaved);
        }

        private void RefreshParameterDefinitionsFromRuntime()
        {
            bool previousUpdatingAudit = _isUpdatingAudit;
            _isUpdatingAudit = true;
            try
            {
                _runtimeSyncModel.RefreshParameterDefinitionsFromRuntime(_model);
            }
            finally
            {
                _isUpdatingAudit = previousUpdatingAudit;
            }
        }

        private bool TryCreatePageEditorModel(
            ProjectRecipeNodeGroup group,
            RecipeParamInfo parameter,
            out ModelParamBase editorModel,
            out string errorMessage)
        {
            return _nodeModel.TryCreatePageEditorModel(group, parameter, out editorModel, out errorMessage);
        }

        private static bool TryUpdateParameterValueFromEditorModel(
            RecipeParamInfo parameter,
            ModelParamBase model,
            out string errorMessage)
        {
            return RecipeManagerNodeModel.TryUpdateParameterValueFromEditorModel(parameter, model, out errorMessage);
        }

        private void UpdateParamEditState()
        {
            if (SelectedRecipe != null)
            {
                RefreshParameterDefinitionsFromRuntime();
                SelectedParamGroup = SelectedRecipe.Groups.FirstOrDefault();
                RaisePropertyChanged(nameof(CurrentRecipeGroups));
            }
            else
            {
                SelectedParamGroup = null;
            }
        }

        private async Task EnsureRecipeNodesInitializedAsync()
        {
            await _nodeModel.EnsureRecipeNodesInitializedAsync();
        }
        #endregion
    }
}

using Prism.Commands;
using Prism.Navigation.Regions;
using ReeYin.RecipeManager.Models;
using ReeYin.RecipeManager.Services;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.RecipeManager.ViewModels
{
    public class RecipeSwitcherViewModel : DialogViewModelBase, INavigationAware
    {
        private readonly IRecipeService _recipeService;
        private readonly RecipeManagerService _service;
        private readonly RecipeManagerNodeModel _nodeModel;
        private readonly RecipeManagerRuntimeSyncModel _runtimeSyncModel;

        private ProjectRecipeConfig _model = new();
        private ProjectRecipeInfo? _selectedRecipe;
        private bool _isBusy;
        private string _statusMessage = "就绪";

        public RecipeSwitcherViewModel()
        {
            IRecipeService recipeService = new RecipeService();
            _recipeService = recipeService;
            _service = new RecipeManagerService(recipeService);
            _nodeModel = new RecipeManagerNodeModel();
            _runtimeSyncModel = new RecipeManagerRuntimeSyncModel(recipeService, _service, _nodeModel);
            LoadCommand = new DelegateCommand(async () => await LoadAsync());
            SwitchRecipeCommand = new DelegateCommand(async () => await SwitchSelectedRecipeAsync(), () => CanSwitchSelectedRecipe);
        }

        public ObservableCollection<ProjectRecipeInfo> Recipes => _model.Recipes;

        public ProjectRecipeInfo? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    RaisePropertyChanged(nameof(CanSwitchSelectedRecipe));
                    SwitchRecipeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanSwitchSelectedRecipe
            => !IsBusy && SelectedRecipe != null && SelectedRecipe.Id != _model.CurrentRecipeId;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaisePropertyChanged(nameof(CanSwitchSelectedRecipe));
                    SwitchRecipeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DelegateCommand LoadCommand { get; }

        public DelegateCommand SwitchRecipeCommand { get; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "正在加载配方...";

                ProjectRecipeConfig loadedModel = await _service.LoadAsync();
                RecipeRuntimeSyncResult syncResult = await _runtimeSyncModel.SyncRecipeParametersFromRuntimeAsync(loadedModel);

                _model = loadedModel ?? new ProjectRecipeConfig();
                _model.EnsureValidState();

                RaisePropertyChanged(nameof(Recipes));
                SelectedRecipe = _model.CurrentRecipe ?? Recipes.FirstOrDefault();
                StatusMessage = RecipeManagerRuntimeSyncModel.BuildLoadStatusMessage(
                    Recipes.Count,
                    syncResult.Changed,
                    syncResult.Saved);
            }
            catch (Exception ex)
            {
                Logs.LogError($"加载配方切换页失败：{ex.Message}");
                StatusMessage = $"加载配方失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SwitchSelectedRecipeAsync()
        {
            if (!CanSwitchSelectedRecipe)
            {
                return;
            }

            ProjectRecipeInfo? targetRecipe = SelectedRecipe;
            if (targetRecipe == null)
            {
                return;
            }

            // 切换前先确认，避免在主运行页面误用到其他配方。
            MessageBoxResult confirmResult = HandyControl.Controls.MessageBox.Show(
                $"确定要切换到配方“{targetRecipe.Name}”吗？",
                "确认切换配方",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes)
            {
                StatusMessage = "已取消配方切换。";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"正在切换到配方“{targetRecipe.Name}”...";

                Guid previousRecipeId = _model.CurrentRecipeId;
                _model.SetCurrentRecipe(targetRecipe.Id);
                _model.LastUpdatedAt = DateTime.Now;

                ProjectRecipeInfo? currentRecipe = _model.CurrentRecipe;
                if (currentRecipe != null)
                {
                    currentRecipe.ModifiedAt = DateTime.Now;
                    currentRecipe.LastModifiedBy = _service.GetCurrentOperator();
                }

                var (success, message, _) = await _service.SaveAsync(_model);
                if (!success)
                {
                    _model.SetCurrentRecipe(previousRecipeId);
                    RaisePropertyChanged(nameof(Recipes));
                    StatusMessage = message;
                    return;
                }

                (int appliedCount, int failedCount) = ApplyCurrentRecipeToRuntimeModels();
                PublishRecipeSwitched();

                RaisePropertyChanged(nameof(Recipes));
                RaisePropertyChanged(nameof(CanSwitchSelectedRecipe));
                SwitchRecipeCommand.RaiseCanExecuteChanged();

                StatusMessage = failedCount > 0
                    ? $"已切换到配方“{currentRecipe?.Name}”，{failedCount}/{appliedCount + failedCount} 个运行模型应用失败。"
                    : $"已切换到配方“{currentRecipe?.Name}”。";
            }
            catch (Exception ex)
            {
                Logs.LogError($"切换配方失败：{ex.Message}");
                StatusMessage = $"切换配方失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private (int appliedCount, int failedCount) ApplyCurrentRecipeToRuntimeModels()
        {
            int appliedCount = 0;
            int failedCount = 0;

            foreach (ModelParamBase model in _nodeModel.CollectRuntimeRecipeModels())
            {
                // 切换配方时按当前代码重新收集配方参数，避免使用旧缓存导致新增标记参数不生效。
                List<RecipeParamInfo> recipeParams = _recipeService.GetMarkedParams(model) ?? new List<RecipeParamInfo>();
                if (recipeParams.Count == 0 && model.RecipeParams?.Count > 0)
                    recipeParams = model.RecipeParams.ToList();
                model.RecipeParams = new ObservableCollection<RecipeParamInfo>(recipeParams);

                if (_recipeService.ApplyRecipeParams(model, recipeParams))
                {
                    appliedCount++;
                }
                else
                {
                    failedCount++;
                }
            }

            return (appliedCount, failedCount);
        }

        private void PublishRecipeSwitched()
        {
            PrismProvider.EventAggregator?
                .GetEvent<UpdateMessageEvent>()?
                .Publish("RecipeSwitched");
        }
    }
}

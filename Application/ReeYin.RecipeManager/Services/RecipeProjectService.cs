using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReeYin.RecipeManager.Services
{
    public class RecipeManagerService
    {
        private readonly IRecipeService _recipeService;
        private readonly IRecipeRepository _repository;

        public RecipeManagerService(
            IRecipeService recipeService = null,
            IRecipeRepository repository = null)
        {
            _recipeService = recipeService ?? new RecipeService();
            _repository = repository ?? new RecipeRepository();
        }

        public Task<ProjectRecipeConfig> LoadAsync()
        {
            return _recipeService.LoadRecipeConfigAsync();
        }

        public Task<(bool success, string message, string storageScope)> SaveAsync(ProjectRecipeConfig model)
        {
            return Task.Run(async () =>
            {
                bool success = await _recipeService.SaveRecipeConfigAsync(model);
                string message = success ? "配方配置已保存。" : "保存配方配置失败。";
                return (success, message, GetStorageScope());
            });
        }

        public ProjectRecipeInfo CreateRecipe(string suggestedName)
        {
            return _recipeService.CreateRecipe(suggestedName, GetCurrentOperator());
        }

        public string GetCurrentOperator()
        {
            string userName = PrismProvider.User?.CurUser?.UserName;
            return string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
        }

        public string GetStorageScope()
        {
            var defaultBaseInfo = PrismProvider.ProjectManager?.SolutionManager?.DefaultBaseInfo;
            if (defaultBaseInfo == null)
            {
                return "当前工程 / 内存";
            }

            if (!string.IsNullOrWhiteSpace(defaultBaseInfo.FilePath) && File.Exists(defaultBaseInfo.FilePath))
            {
                string name = string.IsNullOrWhiteSpace(defaultBaseInfo.Name)
                    ? Path.GetFileNameWithoutExtension(defaultBaseInfo.FilePath)
                    : defaultBaseInfo.Name;

                return $"当前工程 / {name}";
            }

            if (!string.IsNullOrWhiteSpace(defaultBaseInfo.Name))
            {
                return $"当前工程 / {defaultBaseInfo.Name}（未保存）";
            }

            return "当前工程 / 内存";
        }
    }
}

using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方参数同步服务 - 负责将模型参数同步到配方
    /// </summary>
    public class RecipeSyncService
    {
        private readonly IRecipeParamCollector _collector;
        private readonly IRecipeRepository _repository;

        public RecipeSyncService(IRecipeParamCollector collector, IRecipeRepository repository)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 将模型当前参数值同步写入所有配方
        /// </summary>
        public bool SyncRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            if (model == null || PrismProvider.ProjectManager?.SolutionManager == null)
                return true;

            if (model.Serial < 0)
                return true;

            // Skip persistence while a solution file is loading.
            if (PrismProvider.ProjectManager.IsLoadingProject)
                return true;

            List<RecipeParamInfo> paramsList = NormalizeRecipeParams(model, recipeParams, includeCurrentValues: true);

            ProjectRecipeConfig config = _repository.Load();
            if (config?.Recipes == null || config.Recipes.Count == 0)
                return _repository.Save(config ?? new ProjectRecipeConfig());

            foreach (ProjectRecipeInfo recipe in config.Recipes)
                SyncRecipeNode(recipe, model, paramsList);

            return _repository.Save(config);
        }

        /// <summary>
        /// 从所有配方中移除指定模型的参数节点
        /// </summary>
        public bool RemoveRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            if (PrismProvider.ProjectManager?.SolutionManager == null)
                return true;

            List<RecipeParamInfo> paramsList = NormalizeRecipeParams(model, recipeParams, includeCurrentValues: false);
            HashSet<string> recipeKeys = paramsList
                .Where(item => !string.IsNullOrWhiteSpace(item.RecipeKey))
                .Select(item => item.RecipeKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> parameterPaths = paramsList
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .Select(item => item.Path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ProjectRecipeConfig config = _repository.Load();
            if (config?.Recipes == null || config.Recipes.Count == 0)
                return true;

            foreach (ProjectRecipeInfo recipe in config.Recipes)
            {
                foreach (ProjectRecipeNodeGroup group in recipe.Groups.ToList())
                {
                    bool sameNode = model != null && group.Serial == model.Serial;
                    if (!sameNode)
                    {
                        continue;
                    }

                    if (recipeKeys.Count == 0)
                    {
                        recipe.Groups.Remove(group);
                        continue;
                    }

                    foreach (RecipeParamInfo parameter in group.Parameters
                        .Where(item =>
                            recipeKeys.Contains(item.RecipeKey ?? string.Empty) ||
                            parameterPaths.Contains(item.Path ?? string.Empty))
                        .ToList())
                    {
                        group.Parameters.Remove(parameter);
                    }

                    if (group.Parameters.Count == 0)
                        recipe.Groups.Remove(group);
                }
            }

            return _repository.Save(config);
        }

        #region Helpers

        private void SyncRecipeNode(ProjectRecipeInfo recipe, ModelParamBase model, IReadOnlyCollection<RecipeParamInfo> paramsList)
        {
            if (recipe == null || model == null)
                return;

            string subjection = ResolveModelSubjection(model, paramsList);
            ProjectRecipeNodeGroup group = recipe.GetOrCreateGroup(model.Serial, subjection);

            HashSet<string> currentKeys = paramsList
                .Select(item => item.RecipeKey)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> currentPaths = paramsList
                .Select(item => item.Path)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 移除已失效的参数
            foreach (RecipeParamInfo stale in group.Parameters
                .Where(item =>
                    !currentKeys.Contains(item.RecipeKey ?? string.Empty) &&
                    !currentPaths.Contains(item.Path ?? string.Empty))
                .ToList())
            {
                group.Parameters.Remove(stale);
            }

            foreach (RecipeParamInfo info in paramsList)
            {
                RecipeParamInfo existing = group.FindParameter(info.RecipeKey);
                if (existing == null && !string.IsNullOrWhiteSpace(info.Path))
                {
                    existing = group.Parameters.FirstOrDefault(item =>
                        string.Equals(item.Path ?? string.Empty, info.Path, StringComparison.OrdinalIgnoreCase));
                }
                if (existing == null)
                {
                    RecipeParamInfo copy = info.CreateCopy();
                    copy.Id = Guid.NewGuid();
                    group.Parameters.Add(copy);
                    continue;
                }

                // 保留用户已编辑的值，仅更新定义元数据
                string preservedUnit = existing.Unit;
                bool preservedIsRequired = existing.IsRequired;
                string preservedOptions = existing.OptionsText;

                existing.UpdateDefinition(info);
                existing.Unit = string.IsNullOrWhiteSpace(preservedUnit) ? info.Unit : preservedUnit;
                existing.IsRequired = preservedIsRequired;
                existing.OptionsText = string.IsNullOrWhiteSpace(preservedOptions) ? info.OptionsText : preservedOptions;
            }

            if (group.Parameters.Count == 0)
                recipe.Groups.Remove(group);
        }

        private List<RecipeParamInfo> NormalizeRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams, bool includeCurrentValues)
        {
            List<RecipeParamInfo> sourceInfos = recipeParams?
                .Where(item => item != null)
                .ToList() ?? new List<RecipeParamInfo>();

            string subjection = ResolveModelSubjection(model, sourceInfos);
            List<RecipeParamInfo> normalizedInfos = new();

            foreach (RecipeParamInfo source in sourceInfos)
            {
                RecipeParamInfo info = source.CreateCopy();

                if (model?.Serial >= 0)
                    info.Serial = model.Serial;

                if (string.IsNullOrWhiteSpace(info.Subjection))
                    info.Subjection = subjection;

                info.RecipeKey = RecipeKeyHelper.Normalize(info.Serial, info.Path, info.RecipeKey);

                if (includeCurrentValues)
                {
                    string currentValue = GetCurrentValueText(model, info);
                    if (string.IsNullOrWhiteSpace(info.Value?.ToString()))
                        info.Value = currentValue;
                }

                normalizedInfos.Add(info);
            }

            return normalizedInfos;
        }

        private string GetCurrentValueText(ModelParamBase model, RecipeParamInfo info)
        {
            if (model == null || info == null)
                return string.Empty;

            return _collector.TryGetMarkedParamValue(model, info.Path, out object value)
                ? RecipeValueConverter.SerializeValue(value, info.MemberType)
                : string.Empty;
        }

        private string ResolveModelSubjection(ModelParamBase model, IEnumerable<RecipeParamInfo> infos)
        {
            string subjection = infos?
                .Select(item => item?.Subjection)
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

            if (!string.IsNullOrWhiteSpace(subjection))
                return subjection;

            if (!string.IsNullOrWhiteSpace(model?.Name))
                return model.Name;

            return model?.GetType().Name ?? string.Empty;
        }

        private string BuildRecipeKey(int serial, string path)
        {
            return RecipeKeyHelper.Build(serial, path);
        }

        #endregion
    }
}


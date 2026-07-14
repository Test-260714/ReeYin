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
    /// 配方参数应用服务 - 负责将配方参数应用到模型
    /// </summary>
    public class RecipeParamApplierService
    {
        private readonly IRecipeParamCollector _collector;
        private readonly IRecipeRepository _repository;

        public RecipeParamApplierService(IRecipeParamCollector collector, IRecipeRepository repository)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 将当前配方参数值应用到模型
        /// </summary>
        public bool ApplyRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            if (model == null)
                return false;

            List<RecipeParamInfo> paramsList = NormalizeRecipeParams(model, recipeParams, includeCurrentValues: false);
            if (paramsList.Count == 0)
                return true;

            ProjectRecipeConfig config = _repository.Load();
            ProjectRecipeInfo currentRecipe = config?.CurrentRecipe;

            if (currentRecipe == null || !currentRecipe.IsEnabled)
                return true;

            Dictionary<string, object> valuesToAssign = new(StringComparer.OrdinalIgnoreCase);
            foreach (RecipeParamInfo info in paramsList)
            {
                RecipeParamInfo parameter = FindRecipeParameter(currentRecipe, info);
                if (parameter == null || !parameter.IsEnable || string.IsNullOrWhiteSpace(info.Path))
                    continue;

                // 如果Value是字符串，尝试反序列化为正确的类型
                object valueToAssign = parameter.Value;
                if (valueToAssign is string valueStr && info.MemberType != typeof(string))
                {
                    valueToAssign = RecipeValueConverter.DeserializeValue(valueStr, info.MemberType);
                }

                valuesToAssign[info.Path] = valueToAssign;
            }

            if (valuesToAssign.Count == 0)
                return true;

            int updatedCount = _collector.SetMarkedParamValues(model, valuesToAssign, throwOnError: true);
            return updatedCount == valuesToAssign.Count;
        }

        #region Helpers

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

        private RecipeParamInfo FindRecipeParameter(ProjectRecipeInfo recipe, RecipeParamInfo info)
        {
            if (recipe == null || info == null)
                return null;

            ProjectRecipeNodeGroup group = recipe.FindGroup(info.Serial, info.Subjection);
            RecipeParamInfo parameter = group?.FindParameter(info.RecipeKey);
            if (parameter != null)
                return parameter;

            // 按路径回退查找
            if (group != null && !string.IsNullOrWhiteSpace(info.Path))
            {
                parameter = group.Parameters.FirstOrDefault(item =>
                    string.Equals(item.Path ?? string.Empty, info.Path, StringComparison.OrdinalIgnoreCase));
                if (parameter != null)
                    return parameter;
            }

            // 全局回退查找
            if (!string.IsNullOrWhiteSpace(info.RecipeKey))
                return recipe.FindParameter(info.RecipeKey);

            return null;
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


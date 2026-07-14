using ReeYin_V.Core.Services.Recipe;
using System;
using System.Linq;

namespace ReeYin.RecipeManager.Models
{
    public sealed class RecipeManagerRecipeModel
    {
        public string BuildNextRecipeName(ProjectRecipeConfig model, string prefix)
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = $"{prefix}_{index:000}";
                index++;
            } while (model?.Recipes?.Any(recipe =>
                string.Equals(recipe.Name, candidate, StringComparison.OrdinalIgnoreCase)) == true);

            return candidate;
        }

        public bool TryPrepareForSave(ProjectRecipeConfig model, string operatorName, out string message)
        {
            message = string.Empty;
            if (model?.Recipes == null)
            {
                return true;
            }

            // 保存前统一补齐基础字段，并集中校验名称和键值。
            foreach (ProjectRecipeInfo recipe in model.Recipes)
            {
                if (recipe.Id == Guid.Empty)
                {
                    recipe.Id = Guid.NewGuid();
                }

                recipe.Name = (recipe.Name ?? string.Empty).Trim();
                recipe.Category = (recipe.Category ?? string.Empty).Trim();
                recipe.Version = string.IsNullOrWhiteSpace(recipe.Version) ? "1.0.0" : recipe.Version.Trim();
                recipe.Author = string.IsNullOrWhiteSpace(recipe.Author) ? operatorName : recipe.Author.Trim();
                recipe.LastModifiedBy = string.IsNullOrWhiteSpace(recipe.LastModifiedBy)
                    ? operatorName
                    : recipe.LastModifiedBy.Trim();

                if (string.IsNullOrWhiteSpace(recipe.Name))
                {
                    message = "配方名称不能为空。";
                    return false;
                }

                foreach (ProjectRecipeNodeGroup group in recipe.Groups)
                {
                    foreach (RecipeParamInfo parameter in group.Parameters)
                    {
                        if (parameter.Id == Guid.Empty)
                        {
                            parameter.Id = Guid.NewGuid();
                        }

                        parameter.Name = (parameter.Name ?? string.Empty).Trim();
                        parameter.Path = (parameter.Path ?? string.Empty).Trim();
                        if (group.Serial >= 0)
                        {
                            parameter.Serial = group.Serial;
                        }
                        parameter.RecipeKey = RecipeKeyHelper.Normalize(parameter.Serial, parameter.Path, parameter.RecipeKey);
                        parameter.Unit = (parameter.Unit ?? string.Empty).Trim();
                        parameter.Value = parameter.Value ?? new object();
                        parameter.OptionsText = (parameter.OptionsText ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(parameter.RecipeKey))
                        {
                            message = $"配方“{recipe.Name}”中存在未设置键值的参数。";
                            return false;
                        }
                    }
                }

                var duplicateParameterKey = recipe.Groups
                    .SelectMany(group => group.Parameters)
                    .GroupBy(parameter => parameter.RecipeKey, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

                if (duplicateParameterKey != null)
                {
                    message = $"配方“{recipe.Name}”中存在重复的参数键“{duplicateParameterKey.Key}”。";
                    return false;
                }
            }

            var duplicateRecipeName = model.Recipes
                .GroupBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            if (duplicateRecipeName != null)
            {
                message = $"发现重复的配方名称“{duplicateRecipeName.Key}”。";
                return false;
            }

            return true;
        }
    }
}

using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Recipe.Interfaces
{
    /// <summary>
    /// 配方核心业务逻辑接口
    /// </summary>
    public interface IRecipeService
    {
        /// <summary>
        /// 加载配方配置
        /// </summary>
        Task<ProjectRecipeConfig> LoadRecipeConfigAsync();

        /// <summary>
        /// 保存配方配置
        /// </summary>
        Task<bool> SaveRecipeConfigAsync(ProjectRecipeConfig config);

        /// <summary>
        /// 创建新配方
        /// </summary>
        ProjectRecipeInfo CreateRecipe(string recipeName = null, string operatorName = null);

        /// <summary>
        /// 应用配方参数到模型
        /// </summary>
        bool ApplyRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams);

        /// <summary>
        /// 同步模型参数到所有配方
        /// </summary>
        bool SyncRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams);

        /// <summary>
        /// 从所有配方中移除模型参数
        /// </summary>
        bool RemoveRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams);

        /// <summary>
        /// 从模型中收集标记的参数
        /// </summary>
        List<RecipeParamInfo> GetMarkedParams(object model);

        /// <summary>
        /// 读取模型参数的当前值
        /// </summary>
        bool TryGetMarkedParamValue(object model, string path, out object value);
    }
}


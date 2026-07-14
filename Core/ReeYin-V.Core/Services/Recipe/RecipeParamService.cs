using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Recipe.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Core.Services.Recipe
{
    /// <summary>
    /// 配方参数服务 - 兼容层，保持向后兼容性
    /// 新代码应使用 IRecipeService 接口
    /// </summary>
    public static class RecipeParamService
    {
        private static IRecipeService _service;

        private static IRecipeService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = new RecipeService();
                }
                return _service;
            }
        }

        #region Public API

        public static ProjectRecipeConfig LoadRecipeConfig()
        {
            try
            {
                var task = Service.LoadRecipeConfigAsync();
                task.Wait();
                return task.Result ?? new ProjectRecipeConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配方配置失败：{ex}");
                return new ProjectRecipeConfig();
            }
        }

        public static bool SaveRecipeConfig(ProjectRecipeConfig config)
        {
            try
            {
                var task = Service.SaveRecipeConfigAsync(config);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配方配置失败：{ex}");
                return false;
            }
        }

        public static ProjectRecipeInfo CreateRecipe(string recipeName = null, string operatorName = null)
        {
            return Service.CreateRecipe(recipeName, operatorName);
        }

        /// <summary>
        /// 通过反射收集模型上带有 ReassignParam 标记的参数列表。
        /// </summary>
        public static List<RecipeParamInfo> GetMarkedParams(object model)
        {
            return Service.GetMarkedParams(model);
        }

        /// <summary>
        /// 通过反射读取模型上指定路径的当前值。
        /// </summary>
        public static bool TryGetMarkedParamValue(object model, string key, out object value)
        {
            return Service.TryGetMarkedParamValue(model, key, out value);
        }

        /// <summary>
        /// 将当前配方参数值应用到模型（通过反射写入）。
        /// 实现依赖 ReeYin_V.Share.ReassignParamCollector，接口保留供调用方使用。
        /// </summary>
        public static bool ApplyRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return Service.ApplyRecipeParams(model, recipeParams);
        }

        /// <summary>
        /// 将模型当前参数值同步写入所有配方（新增/更新参数节点）。
        /// 接口保留供 ModelParamBase 调用；如无需运行时同步可安全忽略返回值。
        /// </summary>
        public static bool SyncRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return Service.SyncRecipeParams(model, recipeParams);
        }

        /// <summary>
        /// 从所有配方中移除指定模型的参数节点。
        /// 接口保留供 ModelParamBase.Dispose 调用。
        /// </summary>
        public static bool RemoveRecipeParams(ModelParamBase model, IEnumerable<RecipeParamInfo> recipeParams)
        {
            return Service.RemoveRecipeParams(model, recipeParams);
        }

        #endregion
    }
}

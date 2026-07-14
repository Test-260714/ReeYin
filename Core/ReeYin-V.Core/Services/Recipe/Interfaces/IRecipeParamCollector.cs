using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Recipe.Interfaces
{
    /// <summary>
    /// 配方参数收集接口 - 负责从模型中收集和应用参数
    /// </summary>
    public interface IRecipeParamCollector
    {
        /// <summary>
        /// 从模型中收集标记的参数列表
        /// </summary>
        List<RecipeParamInfo> GetMarkedParams(object model);

        /// <summary>
        /// 读取模型上指定路径的当前值
        /// </summary>
        bool TryGetMarkedParamValue(object model, string path, out object value);

        /// <summary>
        /// 将参数值应用到模型
        /// </summary>
        int SetMarkedParamValues(object model, IReadOnlyDictionary<string, object> values, bool throwOnError);
    }
}


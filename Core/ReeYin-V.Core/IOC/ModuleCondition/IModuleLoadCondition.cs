using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.IOC
{
    /// <summary>
    /// 模块加载条件类型
    /// </summary>
    public enum ModuleConditionType
    {
        /// <summary>
        /// 始终加载
        /// </summary>
        Always,
        /// <summary>
        /// 仅在调试模式下加载
        /// </summary>
        DebugOnly,
        /// <summary>
        /// 仅在发布模式下加载
        /// </summary>
        ReleaseOnly,
        /// <summary>
        /// 自定义条件（需要实现 IModuleConditionEvaluator）
        /// </summary>
        Custom,
        /// <summary>
        /// 从数据库配置表读取条件（使用 DatabaseModuleConditionEvaluator）
        /// </summary>
        Database
    }

    /// <summary>
    /// 模块加载条件特性，用于在 RegisterTypes 前判断是否加载模块
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModuleLoadConditionAttribute : Attribute
    {
        /// <summary>
        /// 条件类型
        /// </summary>
        public ModuleConditionType ConditionType { get; }

        /// <summary>
        /// 自定义条件评估器类型（当 ConditionType 为 Custom 时使用）
        /// </summary>
        public Type CustomEvaluatorType { get; }

        /// <summary>
        /// 条件不满足时的提示信息
        /// </summary>
        public string SkipMessage { get; set; }

        /// <summary>
        /// 使用数据库配置表进行条件判断
        /// </summary>
        public ModuleLoadConditionAttribute()
        {
            ConditionType = ModuleConditionType.Database;
            CustomEvaluatorType = typeof(DatabaseModuleConditionEvaluator);
        }

        public ModuleLoadConditionAttribute(ModuleConditionType conditionType)
        {
            ConditionType = conditionType;
            if (conditionType == ModuleConditionType.Database)
            {
                CustomEvaluatorType = typeof(DatabaseModuleConditionEvaluator);
            }
        }

        public ModuleLoadConditionAttribute(Type customEvaluatorType)
        {
            ConditionType = ModuleConditionType.Custom;
            CustomEvaluatorType = customEvaluatorType;

            if (customEvaluatorType != null && !typeof(IModuleConditionEvaluator).IsAssignableFrom(customEvaluatorType))
            {
                throw new ArgumentException($"类型 {customEvaluatorType.Name} 必须实现 IModuleConditionEvaluator 接口");
            }
        }
    }

    /// <summary>
    /// 模块条件评估器接口，用于自定义加载条件
    /// </summary>
    public interface IModuleConditionEvaluator
    {
        /// <summary>
        /// 评估模块是否应该被加载
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <returns>true 表示应该加载，false 表示跳过</returns>
        bool ShouldLoad(Type moduleType);

        /// <summary>
        /// 获取跳过加载时的原因说明
        /// </summary>
        string GetSkipReason(Type moduleType);
    }
}

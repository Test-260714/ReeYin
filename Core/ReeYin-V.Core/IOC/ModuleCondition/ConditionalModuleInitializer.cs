using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Services.License;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReeYin_V.Core.IOC
{
    /// <summary>
    /// 带条件判断的模块初始化器
    /// 在执行 RegisterTypes 前检查模块是否满足加载条件
    /// </summary>
    public class ConditionalModuleInitializer : IModuleInitializer
    {
        private readonly IContainerExtension _containerExtension;

        /// <summary>
        /// 被跳过的模块列表
        /// </summary>
        public List<string> SkippedModules { get; } = new();

        /// <summary>
        /// 已加载的模块列表
        /// </summary>
        public List<string> LoadedModules { get; } = new();

        public ConditionalModuleInitializer(IContainerExtension containerExtension)
        {
            _containerExtension = containerExtension;
        }

        /// <summary>
        /// 初始化模块，在 RegisterTypes 前检查条件
        /// </summary>
        public void Initialize(IModuleInfo moduleInfo)
        {
            var moduleType = Type.GetType(moduleInfo.ModuleType);
            if (moduleType == null)
            {
                throw new ModuleInitializeException($"无法加载模块类型: {moduleInfo.ModuleType}");
            }

            // 检查模块加载条件
            if (!ShouldLoadModule(moduleType, moduleInfo.ModuleName))
            {
                SkippedModules.Add(moduleInfo.ModuleName);
                return; // 条件不满足，跳过初始化
            }

            // 创建模块实例
            IModule moduleInstance;
            try
            {
                moduleInstance = (IModule)_containerExtension.Resolve(moduleType);
            }
            catch
            {
                moduleInstance = (IModule)Activator.CreateInstance(moduleType);
            }

            // 执行 RegisterTypes
            moduleInstance.RegisterTypes(_containerExtension);

            // 执行 OnInitialized
            moduleInstance.OnInitialized(_containerExtension);

            // 标记模块已加载（用于互斥组和依赖判断）
            LoadedModules.Add(moduleType.Name);
            DatabaseModuleConditionEvaluator.MarkModuleLoaded(moduleType.Name);
        }

        /// <summary>
        /// 检查模块是否满足加载条件
        /// </summary>
        private bool ShouldLoadModule(Type moduleType, string moduleName)
        {
            try
            {
                if (!LicensePermissionHub.IsModuleAllowed(moduleName))
                {
                    Logs.LogWarning($"模块 '{moduleName}' 未授权，已跳过加载");
                    return false;
                }

                var conditionAttr = moduleType.GetCustomAttribute<ModuleLoadConditionAttribute>();
                if (conditionAttr == null)
                {
                    return true;
                }

                bool shouldLoad = EvaluateCondition(conditionAttr, moduleType);

                if (!shouldLoad)
                {
                    string skipMessage = conditionAttr.SkipMessage ?? GetDefaultSkipMessage(conditionAttr, moduleType);
                    Logs.LogInfo($"模块 '{moduleName}' 跳过加载: {skipMessage}");
                    Console.WriteLine($"模块 '{moduleName}' 跳过加载: {skipMessage}");
                }

                return shouldLoad;
            }
            catch (Exception ex)
            {
                Logs.LogError($"检查模块加载条件失败: {moduleName}, 错误: {ex.Message}");
                return true; // 出错时默认加载
            }
        }

        /// <summary>
        /// 评估加载条件
        /// </summary>
        private bool EvaluateCondition(ModuleLoadConditionAttribute conditionAttr, Type moduleType)
        {
            switch (conditionAttr.ConditionType)
            {
                case ModuleConditionType.Always:
                    return true;

                case ModuleConditionType.DebugOnly:
#if DEBUG
                    return true;
#else
                    return false;
#endif

                case ModuleConditionType.ReleaseOnly:
#if DEBUG
                    return false;
#else
                    return true;
#endif

                case ModuleConditionType.Custom:
                case ModuleConditionType.Database:
                    return EvaluateCustomCondition(conditionAttr.CustomEvaluatorType, moduleType);

                default:
                    return true;
            }
        }

        /// <summary>
        /// 评估自定义条件
        /// </summary>
        private bool EvaluateCustomCondition(Type evaluatorType, Type moduleType)
        {
            if (evaluatorType == null)
            {
                return true;
            }

            try
            {
                var evaluator = Activator.CreateInstance(evaluatorType) as IModuleConditionEvaluator;
                return evaluator?.ShouldLoad(moduleType) ?? true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"自定义条件评估失败: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// 获取默认的跳过消息
        /// </summary>
        private string GetDefaultSkipMessage(ModuleLoadConditionAttribute conditionAttr, Type moduleType)
        {
            switch (conditionAttr.ConditionType)
            {
                case ModuleConditionType.DebugOnly:
                    return "仅在调试模式下加载";
                case ModuleConditionType.ReleaseOnly:
                    return "仅在发布模式下加载";
                case ModuleConditionType.Custom:
                case ModuleConditionType.Database:
                    if (conditionAttr.CustomEvaluatorType != null)
                    {
                        try
                        {
                            var evaluator = Activator.CreateInstance(conditionAttr.CustomEvaluatorType) as IModuleConditionEvaluator;
                            return evaluator?.GetSkipReason(moduleType) ?? "条件不满足";
                        }
                        catch
                        {
                            return "条件不满足";
                        }
                    }
                    return "条件不满足";
                default:
                    return "条件不满足";
            }
        }
    }
}

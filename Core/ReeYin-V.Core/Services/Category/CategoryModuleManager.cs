using Dm.util;
using Prism.Modularity;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReeYin_V.Core.Services
{
    public class CategoryModuleManager : ModuleManager
    {
        public readonly Dictionary<string, List<IModuleInfo>> _categorizedModules = new();

        /// <summary>
        /// 分类后的数据
        /// </summary>
        public readonly Dictionary<string, List<string>> Classified = new();

        public CategoryModuleManager(
            IModuleCatalog moduleCatalog,
            IModuleInitializer moduleInitializer)
            : base(moduleInitializer, moduleCatalog)
        {
            LoadModuleCompleted += OnModuleLoaded;

            // 初始化已注册模块的分类
            InitializeExistingModules();
        }

        // 初始化已注册模块的分类
        private void InitializeExistingModules()
        {
            foreach (var moduleInfo in ModuleCatalog.Modules)
            {
                CategorizeModule(moduleInfo);
            }
        }

        /// <summary>
        /// 模块加载完成进行分类(未被标记的默认为通用组件)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModuleLoaded(object sender, LoadModuleCompletedEventArgs e)
        {
            CategorizeModule(e.ModuleInfo);
        }

        /// <summary>
        /// 对模块进行分类
        /// </summary>
        /// <param name="moduleInfo"></param>
        private void CategorizeModule(IModuleInfo moduleInfo)
        {
            try
            {
                var moduleType = Type.GetType(moduleInfo.ModuleType);
                if (moduleType == null) return;

                // 获取模块分类（通过特性或接口）
                string category = GetModuleCategory(moduleType);

                if (!string.IsNullOrEmpty(category))
                {
                    if (!_categorizedModules.ContainsKey(category))
                    {
                        _categorizedModules[category] = new List<IModuleInfo>();
                    }

                    _categorizedModules[category].Add(moduleInfo);
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"模块分类失败: {moduleInfo.ModuleName}, 错误: {ex.Message}");
            }
        }

        // 获取模块的分类
        private string GetModuleCategory(Type moduleType)
        {
            // 优先从特性获取分类
            var categoryAttribute = moduleType.GetCustomAttribute<ModuleCategoryAttribute>();
            if (categoryAttribute != null)
            {
                if (!Classified.ContainsKey(categoryAttribute.Category))
                {
                    Classified[categoryAttribute.Category] = new List<string>();
                }
                Classified[categoryAttribute.Category].Add(categoryAttribute.Name);
                return categoryAttribute.Category;
            }

            // 尝试从接口获取分类
            if (typeof(IModuleCategory).IsAssignableFrom(moduleType))
            {
                var instance = Activator.CreateInstance(moduleType) as IModuleCategory;
                return instance?.Category;
            }

            return null;
        }

        // 获取指定分类的模块
        public IEnumerable<IModuleInfo> GetModulesByCategory(string category)
        {
            if (_categorizedModules.TryGetValue(category, out var modules))
            {
                return modules;
            }

            return Enumerable.Empty<ModuleInfo>();
        }

        // 获取所有分类
        public IEnumerable<string> GetAllCategories()
        {
            return _categorizedModules.Keys;
        }

        // 初始化指定分类的模块
        public void InitializeModulesByCategory(string category)
        {
            var modules = GetModulesByCategory(category).ToList();
            if (!modules.Any())
            {
                Logs.LogWarning($"未找到分类为 '{category}' 的模块");
                Console.WriteLine($"未找到分类为 '{category}' 的模块");
                return;
            }
            Logs.LogInfo($"开始初始化分类为 '{category}' 的模块...");
            Console.WriteLine($"开始初始化分类为 '{category}' 的模块...");

            foreach (var moduleInfo in modules)
            {
                LoadModule(moduleInfo.ModuleName);
            }
        }
    }

    // 模块分类接口
    public interface IModuleCategory
    {
        string Category { get; }
    }

    // 模块分类特性
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModuleCategoryAttribute : Attribute, IModuleCategory
    {
        public string Category { get; }
        public string Name { get; }

        public ModuleCategoryAttribute(string category, string name)
        {
            Category = category;
            Name = name;
        }
    }
}

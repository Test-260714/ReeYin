using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services.License;
using ReeYin_V.Logger;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.IOC
{
    /// <summary>
    /// 通用模块加载条件评估器
    /// 从数据库配置表读取条件进行判断
    /// </summary>
    public class DatabaseModuleConditionEvaluator : IModuleConditionEvaluator
    {
        private static ISqlSugarClient _db;
        private static string _currentSite;
        private static HashSet<string> _loadedModules = new();
        private static Dictionary<string, ModuleLoadConfig> _configCache;
        private static readonly object _lock = new();

        /// <summary>
        /// 初始化评估器
        /// </summary>
        /// <param name="db">数据库客户端</param>
        /// <param name="currentSite">当前站点/现场名称</param>
        public static void Initialize(ISqlSugarClient db, string currentSite = null)
        {
            _db = db;
            _currentSite = currentSite;
            _configCache = null;
            _loadedModules.Clear();
        }

        /// <summary>
        /// 设置当前站点
        /// </summary>
        public static void SetCurrentSite(string site)
        {
            _currentSite = site;
        }

        /// <summary>
        /// 标记模块已加载（用于互斥组判断）
        /// </summary>
        public static void MarkModuleLoaded(string moduleName)
        {
            lock (_lock)
            {
                _loadedModules.Add(moduleName);
            }
        }

        /// <summary>
        /// 获取所有配置（带缓存）
        /// </summary>
        private Dictionary<string, ModuleLoadConfig> GetAllConfigs()
        {
            if (_configCache != null) return _configCache;

            lock (_lock)
            {
                if (_configCache != null) return _configCache;

                try
                {
                    if (_db == null) return new Dictionary<string, ModuleLoadConfig>();

                    var configs = _db.Queryable<ModuleLoadConfig>().ToList();
                    _configCache = configs.ToDictionary(c => c.ModuleName, c => c);
                }
                catch (Exception ex)
                {
                    Logs.LogError($"加载模块配置失败: {ex.Message}");
                    _configCache = new Dictionary<string, ModuleLoadConfig>();
                }
            }

            return _configCache;
        }

        /// <summary>
        /// 清除配置缓存（配置更新后调用）
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _configCache = null;
            }
        }

        public bool ShouldLoad(Type moduleType)
        {
            string moduleName = moduleType.Name;
            var configs = GetAllConfigs();

            // 如果没有配置，默认加载
            if (!configs.TryGetValue(moduleName, out var config))
            {
                return true;
            }

            // 总开关检查
            if (config.IsEnabled == 0)
            {
                return false;
            }

            // 根据规则类型判断
            return config.RuleType switch
            {
                ModuleLoadRuleType.Always => true,
                ModuleLoadRuleType.Disabled => false,
                ModuleLoadRuleType.BySite => EvaluateSiteRule(config),
                ModuleLoadRuleType.MutualExclusive => EvaluateMutualExclusiveRule(config, moduleName),
                ModuleLoadRuleType.Dependency => EvaluateDependencyRule(config),
                ModuleLoadRuleType.ByLicense => EvaluateLicenseRule(config),
                _ => true
            };
        }

        public string GetSkipReason(Type moduleType)
        {
            string moduleName = moduleType.Name;
            var configs = GetAllConfigs();

            if (!configs.TryGetValue(moduleName, out var config))
            {
                return "未配置";
            }

            if (!string.IsNullOrEmpty(config.SkipReason))
            {
                return config.SkipReason;
            }

            if (config.IsEnabled == 0)
            {
                return "模块已禁用";
            }

            return config.RuleType switch
            {
                ModuleLoadRuleType.Disabled => "模块已禁用",
                ModuleLoadRuleType.BySite => $"当前站点 '{_currentSite}' 不在适用范围内",
                ModuleLoadRuleType.MutualExclusive => $"互斥组 '{config.MutualExclusiveGroup}' 中已有其他模块加载",
                ModuleLoadRuleType.Dependency => $"依赖的模块未加载: {config.DependsOn}",
                ModuleLoadRuleType.ByLicense => "许可证验证失败",
                _ => "条件不满足"
            };
        }

        /// <summary>
        /// 按站点规则判断
        /// </summary>
        private bool EvaluateSiteRule(ModuleLoadConfig config)
        {
            if (string.IsNullOrEmpty(config.ApplicableSites))
            {
                return true; // 未配置站点限制，允许加载
            }

            if (string.IsNullOrEmpty(_currentSite))
            {
                return false; // 未设置当前站点，不加载
            }

            var sites = config.ApplicableSites.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return sites.Contains(_currentSite);
        }

        /// <summary>
        /// 互斥组规则判断
        /// </summary>
        private bool EvaluateMutualExclusiveRule(ModuleLoadConfig config, string moduleName)
        {
            if (string.IsNullOrEmpty(config.MutualExclusiveGroup))
            {
                return true;
            }

            var configs = GetAllConfigs();

            // 获取同组的所有模块，按优先级排序
            var groupModules = configs.Values
                .Where(c => c.MutualExclusiveGroup == config.MutualExclusiveGroup && c.IsEnabled == 1)
                .OrderBy(c => c.MutualExclusivePriority)
                .ToList();

            if (!groupModules.Any())
            {
                return true;
            }

            // 检查是否已有同组模块加载
            lock (_lock)
            {
                foreach (var m in groupModules)
                {
                    if (_loadedModules.Contains(m.ModuleName))
                    {
                        // 同组已有模块加载，当前模块不加载
                        return false;
                    }
                }
            }

            // 当前模块是否是优先级最高的
            var highestPriority = groupModules.First();
            return highestPriority.ModuleName == moduleName;
        }

        /// <summary>
        /// 依赖规则判断
        /// </summary>
        private bool EvaluateDependencyRule(ModuleLoadConfig config)
        {
            if (string.IsNullOrEmpty(config.DependsOn))
            {
                return true;
            }

            var dependencies = config.DependsOn.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            lock (_lock)
            {
                return dependencies.All(dep => _loadedModules.Contains(dep));
            }
        }

        /// <summary>
        /// 许可证规则判断
        /// </summary>
        private bool EvaluateLicenseRule(ModuleLoadConfig config)
        {
            if (string.IsNullOrEmpty(config.LicenseKey))
            {
                return true;
            }

            return LicensePermissionHub.IsModuleAllowed(config.LicenseKey);
        }
    }
}

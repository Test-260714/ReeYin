using ReeYin_V.Core.Models.Database.Tables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.RootManager.Models
{
    /// <summary>
    /// 模块加载配置编辑模型
    /// </summary>
    public class ModuleLoadConfigEditModel : BindableBase
    {
        private int _configId;
        public int ConfigId
        {
            get => _configId;
            set => SetProperty(ref _configId, value);
        }

        private string _moduleName;
        public string ModuleName
        {
            get => _moduleName;
            set => SetProperty(ref _moduleName, value);
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private ModuleLoadRuleType _ruleType = ModuleLoadRuleType.Always;
        public ModuleLoadRuleType RuleType
        {
            get => _ruleType;
            set => SetProperty(ref _ruleType, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _mutualExclusiveGroup;
        public string MutualExclusiveGroup
        {
            get => _mutualExclusiveGroup;
            set => SetProperty(ref _mutualExclusiveGroup, value);
        }

        private int _mutualExclusivePriority = 100;
        public int MutualExclusivePriority
        {
            get => _mutualExclusivePriority;
            set => SetProperty(ref _mutualExclusivePriority, value);
        }

        private string _applicableSites;
        public string ApplicableSites
        {
            get => _applicableSites;
            set => SetProperty(ref _applicableSites, value);
        }

        private string _dependsOn;
        public string DependsOn
        {
            get => _dependsOn;
            set => SetProperty(ref _dependsOn, value);
        }

        private string _licenseKey;
        public string LicenseKey
        {
            get => _licenseKey;
            set => SetProperty(ref _licenseKey, value);
        }

        private string _skipReason;
        public string SkipReason
        {
            get => _skipReason;
            set => SetProperty(ref _skipReason, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _sort;
        public int Sort
        {
            get => _sort;
            set => SetProperty(ref _sort, value);
        }

        /// <summary>
        /// 从数据库实体转换
        /// </summary>
        public static ModuleLoadConfigEditModel FromEntity(ModuleLoadConfig entity)
        {
            return new ModuleLoadConfigEditModel
            {
                ConfigId = entity.ConfigId,
                ModuleName = entity.ModuleName,
                DisplayName = entity.DisplayName,
                RuleType = entity.RuleType,
                IsEnabled = entity.IsEnabled == 1,
                MutualExclusiveGroup = entity.MutualExclusiveGroup,
                MutualExclusivePriority = entity.MutualExclusivePriority,
                ApplicableSites = entity.ApplicableSites,
                DependsOn = entity.DependsOn,
                LicenseKey = entity.LicenseKey,
                SkipReason = entity.SkipReason,
                Description = entity.Description,
                Sort = entity.Sort
            };
        }

        /// <summary>
        /// 转换为数据库实体
        /// </summary>
        public ModuleLoadConfig ToEntity()
        {
            return new ModuleLoadConfig
            {
                ConfigId = ConfigId,
                ModuleName = ModuleName,
                DisplayName = DisplayName,
                RuleType = RuleType,
                IsEnabled = (byte)(IsEnabled ? 1 : 0),
                MutualExclusiveGroup = MutualExclusiveGroup,
                MutualExclusivePriority = MutualExclusivePriority,
                ApplicableSites = ApplicableSites,
                DependsOn = DependsOn,
                LicenseKey = LicenseKey,
                SkipReason = SkipReason,
                Description = Description,
                Sort = Sort
            };
        }
    }

    /// <summary>
    /// 规则类型显示项
    /// </summary>
    public class RuleTypeItem
    {
        public ModuleLoadRuleType Value { get; set; }
        public string DisplayName { get; set; }

        public static ObservableCollection<RuleTypeItem> GetAllItems()
        {
            return new ObservableCollection<RuleTypeItem>
            {
                new RuleTypeItem { Value = ModuleLoadRuleType.Always, DisplayName = "始终加载" },
                new RuleTypeItem { Value = ModuleLoadRuleType.Disabled, DisplayName = "始终禁用" },
                new RuleTypeItem { Value = ModuleLoadRuleType.BySite, DisplayName = "按站点加载" },
                new RuleTypeItem { Value = ModuleLoadRuleType.MutualExclusive, DisplayName = "互斥组" },
                new RuleTypeItem { Value = ModuleLoadRuleType.Dependency, DisplayName = "依赖其他模块" },
                new RuleTypeItem { Value = ModuleLoadRuleType.ByLicense, DisplayName = "按许可证加载" }
            };
        }
    }
}

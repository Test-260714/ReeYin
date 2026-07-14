using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Database.Tables
{
    /// <summary>
    /// 模块加载规则类型
    /// </summary>
    public enum ModuleLoadRuleType
    {
        /// <summary>
        /// 始终加载
        /// </summary>
        Always = 0,
        /// <summary>
        /// 始终禁用
        /// </summary>
        Disabled = 1,
        /// <summary>
        /// 按现场/站点加载
        /// </summary>
        BySite = 2,
        /// <summary>
        /// 互斥组（同组只能加载一个）
        /// </summary>
        MutualExclusive = 3,
        /// <summary>
        /// 依赖其他模块
        /// </summary>
        Dependency = 4,
        /// <summary>
        /// 按许可证加载
        /// </summary>
        ByLicense = 5
    }

    /// <summary>
    /// 模块加载配置表
    /// </summary>
    [SugarTable("module_load_config", TableDescription = "模块加载配置表")]
    public class ModuleLoadConfig : BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "配置ID（主键）")]
        public int ConfigId { get; set; }

        /// <summary>
        /// 模块名称（与 IModule 类名对应）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false, ColumnDescription = "模块名称")]
        public string ModuleName { get; set; }

        /// <summary>
        /// 模块显示名称
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "模块显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 加载规则类型
        /// </summary>
        [SugarColumn(ColumnDescription = "加载规则类型")]
        public ModuleLoadRuleType RuleType { get; set; } = ModuleLoadRuleType.Always;

        /// <summary>
        /// 是否启用（总开关）
        /// </summary>
        [SugarColumn(ColumnDescription = "是否启用（1=启用，0=禁用）", DefaultValue = "1")]
        public byte IsEnabled { get; set; } = 1;

        /// <summary>
        /// 互斥组名称（RuleType=MutualExclusive时使用，同组只能加载一个）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "互斥组名称")]
        public string MutualExclusiveGroup { get; set; }

        /// <summary>
        /// 互斥组优先级（值越小优先级越高，同组中优先级最高且启用的模块被加载）
        /// </summary>
        [SugarColumn(ColumnDescription = "互斥组优先级", DefaultValue = "100")]
        public int MutualExclusivePriority { get; set; } = 100;

        /// <summary>
        /// 适用站点/现场（RuleType=BySite时使用，多个用逗号分隔，空表示所有站点）
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true, ColumnDescription = "适用站点（多个用逗号分隔）")]
        public string ApplicableSites { get; set; }

        /// <summary>
        /// 依赖的模块名称（RuleType=Dependency时使用，多个用逗号分隔）
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true, ColumnDescription = "依赖的模块（多个用逗号分隔）")]
        public string DependsOn { get; set; }

        /// <summary>
        /// 许可证Key（RuleType=ByLicense时使用）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "许可证Key")]
        public string LicenseKey { get; set; }

        /// <summary>
        /// 不加载时的原因说明
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true, ColumnDescription = "不加载原因")]
        public string SkipReason { get; set; }

        /// <summary>
        /// 排序（用于显示顺序）
        /// </summary>
        [SugarColumn(ColumnDescription = "排序", DefaultValue = "0")]
        public int Sort { get; set; } = 0;
    }
}

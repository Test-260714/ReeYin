using ReeYin.CSharpScript.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.CSharpScript
{
    /// <summary>
    /// 使用数据库配置表进行条件判断
    /// 在 module_load_config 表中配置 ModuleName = "CSharpScript" 的记录来控制加载行为
    ///
    /// 示例配置：
    /// 1. 禁用模块：IsEnabled = 0
    /// 2. 按站点加载：RuleType = BySite, ApplicableSites = "站点A,站点B"
    /// 3. 互斥组：RuleType = MutualExclusive, MutualExclusiveGroup = "ScriptGroup", MutualExclusivePriority = 1
    /// </summary>
    [ModuleLoadCondition]  // 使用数据库配置
    public class CSharpScript : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<CSharpScriptView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ScriptModule",
                Icon = "\ue7b3",
                Type = "01.LogicModule",
                Description = "用来进行脚本编辑的模块",
                TargetType = typeof(CSharpScriptView),
            });
        }
    }
}

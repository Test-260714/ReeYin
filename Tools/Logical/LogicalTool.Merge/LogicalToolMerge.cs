using LogicalTool.Merge.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Merge
{
    public class LogicalToolMerge : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<MergerView>(null, new MenuInfo
            {
                NodeType = NodeType.Merge,
                TranslateKey = "",
                Title = "MergeBranches",
                Icon = "\ueb70",
                Type = "01.LogicModule",
                Description = "用来对分支进行合并的组件",
                TargetType = typeof(MergerView),
            });
        }
    }
}

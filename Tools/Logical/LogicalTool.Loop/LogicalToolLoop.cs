using LogicalTool.Loop.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Loop
{
    public class LogicalToolLoop : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<LoopView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "StartTheCycle",
                Icon = "\ue618",
                Type = "01.LogicModule",
                Description = "用来实现循环的组件",
                TargetType = typeof(LoopView),
            });

            containerRegistry.RegisterDialogAndMenu<EndLoopView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "EndTheCycle",
                Icon = "\ue618",
                Type = "01.LogicModule",
                Description = "用来结束循环的组件",
                TargetType = typeof(EndLoopView),
            });
        }
    }
}

using LogicalTool.Monitor.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Monitor
{
    public class LogicalToolMonitor : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<MonitorView>(null, new MenuInfo
            {
                NodeType = NodeType.Monitor,
                TranslateKey = "",
                Title = "Listen/Start",
                Icon = "\ue69a",
                Type = "01.LogicModule",
                Description = "用来进行通信监听的组件",
                TargetType = typeof(MonitorView),
            });
        }
    }
}

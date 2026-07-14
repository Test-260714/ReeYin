using LogicalTool.Finish.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Finish
{
    public class LogicalToolFinish : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<FinishView>(null, new MenuInfo
            {
                NodeType = NodeType.Finish,
                TranslateKey = "",
                Title = "End",
                Icon = "\ue617",
                Type = "01.LogicModule",
                Description = "运行结束",
                TargetType = typeof(FinishView),
            });
        }
    }
}

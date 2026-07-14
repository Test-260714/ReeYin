using LogicalTool.GeneralOutput.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.GeneralOutput
{
    public class LogicalToolGeneralOutput: IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<GeneralOutputView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "GeneralOutput",
                Icon = "\ueabf",
                Type = "01.LogicModule",
                Description = "用来做一些通用的输出",
                TargetType = typeof(GeneralOutputView),
            });
        }
    }
}

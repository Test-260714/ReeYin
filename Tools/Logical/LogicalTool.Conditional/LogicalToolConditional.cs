using LogicalTool.Conditional.ViewModels;
using LogicalTool.Conditional.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Conditional
{
    public class LogicalToolConditional : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<DefaultView>("ConditionRegion");
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<ListConditionView>("ConditionRegion");
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<ObjectConditionView>("ConditionRegion");
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<StringOperationView>("ConditionRegion");
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<IntConditionView>("ConditionRegion");

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterForNavigation<DefaultView>();
            containerRegistry.RegisterForNavigation<ListConditionView>();
            containerRegistry.RegisterForNavigation<ObjectConditionView>();
            containerRegistry.RegisterForNavigation<StringOperationView>();
            containerRegistry.RegisterForNavigation<IntConditionView>();

            containerRegistry.RegisterDialogAndMenu<ConditionView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ConditionalBranching",
                Icon = "\ue6d8",
                Type = "01.LogicModule",
                Description = "用来编辑条件分支的弹窗",
                TargetType = typeof(ConditionView),
            });
        }
    }
}

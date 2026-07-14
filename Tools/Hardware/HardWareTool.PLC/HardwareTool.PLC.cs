using HardWareTool.PLC.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HardWareTool.PLC
{
    public class HardwareToolPLC : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion<ReadSingleAddrView>("PLCOperationRegion");
            regionManager.RegisterViewWithRegion<WriteSingleAddrView>("PLCOperationRegion");
            regionManager.RegisterViewWithRegion<AxisOperationView>("PLCOperationRegion");
            regionManager.RegisterViewWithRegion<DelayOperationView>("PLCOperationRegion");
            regionManager.RegisterViewWithRegion<TriggerEventView>("PLCOperationRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterForNavigation<ReadSingleAddrView>();
            containerRegistry.RegisterForNavigation<WriteSingleAddrView>();
            containerRegistry.RegisterForNavigation<AxisOperationView>();
            containerRegistry.RegisterForNavigation<DelayOperationView>();
            containerRegistry.RegisterForNavigation<TriggerEventView>();

            containerRegistry.RegisterDialog<PLCMonitorView>();
            containerRegistry.RegisterDialog<PLCRecipeManagerView>();
            containerRegistry.RegisterDialog<LineScanGenerateView>();
            containerRegistry.RegisterDialogAndDynamic<SwitchRecipeView>(null,new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "切换制程",
                ViewName = "SwitchRecipeView"
            });


            containerRegistry.RegisterDialogAndMenu<PLCView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "PLCOperation",
                Icon = "\ue63e",
                Type = "04.Hardware",
                Description = "用来下发对应指令给PLC的模块",
                TargetType = typeof(PLCView),
            });
        }
    }
}

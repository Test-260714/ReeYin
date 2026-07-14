using ReeYin.Status.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ReeYin.Status
{
    //[Module(ModuleName = ModuleNames.ApplicationStatusModule, OnDemand = true)]
    public class ApplicationStatusModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndDynamic<HardwareStatusView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "硬件状态显示",
                ViewName = "HardwareStatusView"
            });
        }
    }
}

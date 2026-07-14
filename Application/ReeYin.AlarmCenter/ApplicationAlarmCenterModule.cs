using Prism.Ioc;
using Prism.Modularity;
using ReeYin.AlarmCenter.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ReeYin.AlarmCenter
{
    //[Module(ModuleName = ModuleNames.ApplicationAlarmCenterModule, OnDemand = true)]
    public class ApplicationAlarmCenterModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<AlarmRealtimeView>();
            containerRegistry.RegisterForNavigation<AlarmHistoryView>();
            containerRegistry.RegisterForNavigation<AlarmStatisticsView>();
            containerRegistry.RegisterForNavigation<AlarmDefinitionsView>();

            containerRegistry.RegisterDialogAndDynamic<AlarmWorkbenchShellView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "报警中心",
                ViewName = "AlarmWorkbenchShellView"
            });
        }
    }
}

using Nodify.FlowApp.Views.Subjoin;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    public class ApplicationNotifyFlowAPPModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<AppView>(RegionNames.PrimaryRegion);
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<SubJoinView>("test");
            //containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<FlowStateView>(RegionNames.PrimaryRegion);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<AppView>();
            containerRegistry.RegisterForNavigation<FlowStateView>();
            //containerRegistry.RegisterDialog<ConfigView>();
        }
    }
}

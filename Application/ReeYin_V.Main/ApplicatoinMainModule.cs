using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Main.UC.Views;
using ReeYin_V.Main.Views;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Main
{
    [Module(ModuleName = ModuleNames.ApplicatoinMainModule, OnDemand = true)]
    [ModuleDependency(ModuleNames.ApplicationUserManagerModule)] //登陆主页面依赖于用户管理模块
    [ModuleDependency(ModuleNames.ApplicationPermissionModule)] //登陆主页面依赖于权限管理模块

    public class ApplicatoinMainModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<MainView>(RegionNames.MainRegion);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterForNavigation<MainView>();
            containerRegistry.RegisterForNavigation<RunMainView>();
            containerRegistry.RegisterForNavigation<RegionManagementView>();

            containerRegistry.RegisterDialogAndDynamic<LogView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "日志控件",
                ViewName = "LogView"
            });

            containerRegistry.RegisterDialogAndDynamic<OpBtnView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "操作按钮",
                ViewName = "OpBtnView"
            });
        }
    }
}

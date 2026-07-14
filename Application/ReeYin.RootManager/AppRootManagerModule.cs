using ReeYin.RootManager.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ReeYin.RootManager
{
    /// <summary>
    /// Root管理模块，用来管理模块是否被加载
    /// </summary>
    public class AppRootManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion<ComponentPreviewView>("RootManagerContentRegion");
            regionManager.RegisterViewWithRegion<HardwareManageView>("RootManagerContentRegion");
            regionManager.RegisterViewWithRegion<ModuleLoadConfigView>("RootManagerContentRegion");
            regionManager.RegisterViewWithRegion<UpdateView>("RootManagerContentRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialog<RootManagerView>();

            // 注册导航区域中使用的子视图
            containerRegistry.RegisterForNavigation<ComponentPreviewView>();
            containerRegistry.RegisterForNavigation<HardwareManageView>();
            containerRegistry.RegisterForNavigation<ModuleLoadConfigView>();
            containerRegistry.RegisterForNavigation<UpdateView>();
        }
    }
}

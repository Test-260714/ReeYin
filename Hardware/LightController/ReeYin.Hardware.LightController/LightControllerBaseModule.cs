using ReeYin.Hardware.LightController.Views;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController
{
    /// <summary>
    /// 光源控制器基础模块
    /// </summary>
    public class LightControllerBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<LightControllerSetView>("HardwareRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<LightControllerSetView>();

            PrismProvider.HardwareModuleManager.ConfigItems.Add(new HardwareConfigItem
            {
                Title = "光源控制器",
                Icon = "\ue6a2",
                Description = "光源控制器",
                HardType = HardwareType.LightController,
                Config = ConfigKey.LightControllerConfig,
                Navigation = () =>
                {
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        //加载主界面
                        PrismProvider.ModuleManager.LoadModule("LightControllerBaseModule");
                        //导航到主区域
                        PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "LightControllerSetView");
                    });
                }
            });
        }
    }
}

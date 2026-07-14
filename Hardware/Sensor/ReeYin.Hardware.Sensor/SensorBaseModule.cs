using ReeYin.Hardware.Sensor.Views;
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

namespace ReeYin.Hardware.Sensor
{
    /// <summary>
    /// 传感器基础模块
    /// </summary>
    public class SensorBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<SensorSetView>("HardwareRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<SensorSetView>();

            PrismProvider.HardwareModuleManager.ConfigItems.Add(new HardwareConfigItem
            {
                Title = "传感器",
                Icon = "\ue6a2",
                Description = "传感器",
                HardType = HardwareType.Sensor,
                Config = ConfigKey.SensorConfig,
                Navigation = () =>
                {
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        //加载主界面
                        PrismProvider.ModuleManager.LoadModule("SensorBaseModule");
                        //导航到主区域
                        PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "SensorSetView");
                    });
                }
            });
        }


    }
}

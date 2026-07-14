using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Hardware.PLC
{
    /// <summary>
    /// PLC基础模块
    /// </summary>
    public class PLCBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<PLCSetView>("HardwareRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<PLCSetView>();

            PrismProvider.HardwareModuleManager.ConfigItems.Add(new HardwareConfigItem
            {
                Title = "PLC",
                Icon = "\ue63e",
                Description = "PLC",
                HardType = HardwareType.PLC,
                Config = ConfigKey.PLCConfig,
                Navigation = () =>
                {
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        //加载主界面
                        PrismProvider.ModuleManager.LoadModule("PLCBaseModule");
                        //导航到主区域
                        PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "PLCSetView");
                    });
                }
            });

            PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Subscribe((order) =>
            {
                switch (order)
                {
                    case "PLCAxisGroupMoveView":
                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            WindowManager.OpenSingleInstanceWindow(
                                typeof(PLCAxisGroupMoveView),
                                "PLCAxisGroupMoveView",
                                window =>
                                {
                                    window.Width = 1100;
                                    window.Height = 620;
                                    window.Title = "PLC运动调试面板";
                                    window.Topmost = true;
                                    window.Left = 100;
                                    window.Top = 100;
                                    window.ResizeMode = ResizeMode.NoResize;
                                });
                        });
                        break;
                }
            });
        }
    }
}

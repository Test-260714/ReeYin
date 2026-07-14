using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.ControlCard.ViewModels;
using ReeYin_V.Hardware.ControlCard.Views;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 控制卡基础模块
    /// </summary>
    public class ControlCardBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

            containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<ControlCardConfigView>("HardwareRegion");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<ControlCardConfigView>();
            containerRegistry.RegisterForNavigation<CoordinateCacheView>();
            containerRegistry.RegisterDialog<AxisView, AxisViewModel>();
            containerRegistry.RegisterDialog<IOManagerView, IOManagerViewModel>();

            PrismProvider.HardwareModuleManager.ConfigItems.Add(new HardwareConfigItem
            {
                Title = "控制卡",
                Icon = "\ue626",
                Description = "控制卡",
                HardType = HardwareType.ControlCard,
                Config = ConfigKey.ControlCard,
                Navigation = () =>
                {
                    PrismProvider.Dispatcher.Invoke(() =>
                    {
                        //加载主界面
                        PrismProvider.ModuleManager.LoadModule("ControlCardBaseModule");
                        //导航到主区域
                        PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "ControlCardConfigView");
                    });
                }
            });

            PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Subscribe((order) =>
            {
                switch (order)
                {
                    case "AxisView":
                        {
                            PrismProvider.Dispatcher.BeginInvoke(() =>
                            {
                                // 打开窗口并设置属性
                                WindowManager.OpenSingleInstanceWindow(
                                    typeof(AxisView),
                                    "AxisView",
                                    window =>
                                    {
                                        // 设置窗口大小
                                        window.Width = 800;
                                        window.Height = 350;

                                        // 设置窗口标题
                                        window.Title = "运动轴操作面板";

                                        // 设置置顶
                                        window.Topmost = true;

                                        // 设置窗口位置
                                        window.Left = 100;
                                        window.Top = 100;

                                        // 设置是否可调整大小
                                        window.ResizeMode = ResizeMode.NoResize;
                                    }
                                );
                            });
                        }
                        break;
                    case "IOManagerView":
                        {
                            // 打开窗口并设置属性
                            WindowManager.OpenSingleInstanceWindow(
                                typeof(IOManagerView),
                                "IOManagerView",
                                window =>
                                {
                                    // 设置窗口大小
                                    window.Width = 1100;
                                    window.Height = 600;

                                    // 设置窗口标题
                                    window.Title = "IO操作面板";

                                    // 设置置顶
                                    window.Topmost = true;

                                    // 设置窗口位置
                                    window.Left = 800;
                                    window.Top = 100;

                                    // 设置是否可调整大小
                                    window.ResizeMode = ResizeMode.NoResize;
                                }
                            );
                        }
                        break;
                }

            }, ThreadOption.UIThread);
        }
    }
}

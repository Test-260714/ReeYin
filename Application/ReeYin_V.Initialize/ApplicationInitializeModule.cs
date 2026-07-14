using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Hardware.Camera.Views;
using ReeYin_V.Initialize.Views;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Initialize
{
    /// <summary>
    /// 初始化模块
    /// </summary>
    [Module(ModuleName = ModuleNames.ApplicationInitializeModule, OnDemand = true)]
    [ModuleDependency(ModuleNames.ApplicationConfigModule)] //硬件初始化模块依赖于系统配置模块，即先加载系统配置模块，后加载硬件初始化模块
    public class ApplicationInitializeModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterForNavigation<InitializeView>();
            containerRegistry.RegisterDialog<LoadingView>();
            containerRegistry.RegisterDialog<LoadingWaitView>();
        }
    }
}

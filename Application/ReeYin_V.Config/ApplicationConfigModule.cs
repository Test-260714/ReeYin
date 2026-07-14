using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Config.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Config
{
    [Module(ModuleName = ModuleNames.ApplicationConfigModule, OnDemand = true)]
    public class ApplicationConfigModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<ConfigView>();

            //containerRegistry.RegisterDialog<HardwareConfigView>();
            containerRegistry.RegisterForNavigation<HardwareConfigView>();
            containerRegistry.RegisterForNavigation<LanguageConfigView>();
            containerRegistry.RegisterForNavigation<StyleConfigView>();
        }
    }
}

using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.License.Views;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ReeYin_V.License
{
    [Module(ModuleName = ModuleNames.ApplicationLicenseModule, OnDemand = true)]
    public class ApplicationLicenseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<LicenseActivationView>();
            containerRegistry.RegisterDialog<LicenseActivationView>();
        }
    }
}

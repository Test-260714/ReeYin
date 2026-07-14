using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.Camera.Models;
using System.Reflection;

namespace ReeYin_V.Hardware.Camera.Dalsa
{
    [ModuleCategory("Camera", "DalsaCamera")]
    public class DalsaCameraModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ICamera, DalsaCamera>("DalsaCamera");
        }
    }
}

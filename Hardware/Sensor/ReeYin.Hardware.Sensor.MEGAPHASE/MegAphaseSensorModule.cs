using ReeYin.Hardware.Sensor.MEGAPHASE.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.MEGAPHASE
{
    [ModuleCategory("Sensor", "MEGAPHASE")]
    public class MegAphaseSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, MegAphaseSensor>("MegAphaseSensor");
            containerRegistry.RegisterDialog<MegAphaseSensorView>();
        }
    }
}

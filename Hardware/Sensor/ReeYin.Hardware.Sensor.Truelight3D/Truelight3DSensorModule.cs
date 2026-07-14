using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.Truelight3D.CustomUI.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.Truelight3D
{
    [ModuleCategory("Sensor", "Truelight3D")]
    public class Truelight3DSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, Truelight3DSensor>("Truelight3D");
            containerRegistry.RegisterDialog<Truelight3DSensorView>();
        }
    }
}

using ReeYin.Hardware.Sensor.CDMS.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.CDMS
{
    [ModuleCategory("Sensor", "CDMS")]
    public class CDMSSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, CDMSSensor>("CDMSSensor");
            containerRegistry.RegisterDialog<CDMSSensorView>();
        }
    }
}

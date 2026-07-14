using ReeYin.Hardware.Sensor.ChroCodile.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.ChroCodile
{
    [ModuleCategory("Sensor", "PRECITEC")]
    public class ChroCodileSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<ISensor, ChroCodileSensor>("ChroCodileSensor");

            containerRegistry.RegisterDialog<ChroCodileSensorView>();
        }
    }
}

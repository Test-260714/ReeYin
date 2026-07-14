using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ReeYin.Hardware.Sensor.Hyperson
{
    // 使用特性为模块指定分类
    [ModuleCategory("Sensor", "HPS")]
    public class HypersonSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<ISensor, HypersenSensor>("HypersenSensor");

            containerRegistry.RegisterDialog<HypersonSensorView>();
        }
    }
}

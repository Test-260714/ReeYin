using ReeYin.Hardware.Sensor.JingCe.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.JingCe
{
    // 使用特性为模块指定分类
    [ModuleCategory("Sensor", "JingCe")]
    public class JingCeSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<ISensor, JingCeSensor>("JingCeSensor");

            containerRegistry.RegisterDialog<JingCeSensorView>();
        }
    }
}

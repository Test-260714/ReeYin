﻿using ReeYin.Hardware.Sensor.JueXin.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.JueXin
{
    [ModuleCategory("Sensor", "JueXin_Whitelight")]
    public class JueXinSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, JueXinSensor>("JueXin_Whitelight");
            containerRegistry.RegisterDialog<JueXinSensorView>();
        }
    }
}

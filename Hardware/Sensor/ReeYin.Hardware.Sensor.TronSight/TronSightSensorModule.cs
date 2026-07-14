using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.TronSight
{
    // 使用特性为模块指定分类
    [ModuleCategory("Sensor", "TS")]
    public class TronSightSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<ISensor, TronSightSensor>("TronSight");

            containerRegistry.RegisterDialog<TronSightSensorView>();
            containerRegistry.RegisterDialog<TronSightRawImageWindow>();

        }
    }
}

using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.TronSight2.CustomUI.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.TronSight2
{
    // 使用特性为模块指定分类
    [ModuleCategory("Sensor", "TSInfrared")]
    public class TronSight2SensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<ISensor, TronSight2Sensor>("TronSight2Sensor");

            containerRegistry.RegisterDialog<TronSight2SensorView>();
        }
    }
}

using Prism.Ioc;
using Prism.Modularity;
using ReeYin.Hardware.Sensor.IKapSpectralConfocal.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.IKapSpectralConfocal
{
    /// <summary>
    /// 埃科线光谱共焦传感器模块注册。
    /// </summary>
    [ModuleCategory("Sensor", "IKapSpectralConfocal")]
    public class IKapSpectralConfocalSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, IKapSpectralConfocalSensor>("IKapSpectralConfocal");
            containerRegistry.RegisterDialog<IKapSpectralConfocalSensorView>();
        }
    }
}
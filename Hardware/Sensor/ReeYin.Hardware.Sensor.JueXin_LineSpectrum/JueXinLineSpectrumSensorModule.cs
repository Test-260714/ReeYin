using Prism.Modularity;
using ReeYin.Hardware.Sensor.JueXin_LineSpectrum.CustomUI.Views;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.Sensor.JueXin_LineSpectrum
{
    /// <summary>
    /// 觉芯线光谱传感器模块注册。
    /// </summary>
    [ModuleCategory("Sensor", "JueXin_LineSpectrum")]
    public class JueXinLineSpectrumSensorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ISensor, JueXinLineSpectrumSensor>("JueXin_LineSpectrum");
            containerRegistry.RegisterDialog<JueXinLineSpectrumSensorView>();
        }
    }
}

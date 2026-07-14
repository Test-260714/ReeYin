using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.Camera.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Hardware.Camera
{
    /// <summary>
    /// 相机基础模块
    /// </summary>
    public class CameraBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialog<CameraSetView>();
            containerRegistry.RegisterDialog<CamConfigView>();
        }
    }
}

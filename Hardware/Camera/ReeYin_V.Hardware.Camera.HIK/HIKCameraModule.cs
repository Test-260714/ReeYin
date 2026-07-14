using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ReeYin_V.Hardware.Camera.HIK
{
    // 使用特性为模块指定分类
    [ModuleCategory("Camera", "HIKCamera")]
    public class HIKCameraModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<ICamera, HIKCamera>("HIKCamera");
        }
    }
}

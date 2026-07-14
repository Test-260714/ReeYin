using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.Camera.DaHeng
{
    // 使用特性为模块指定分类
    [ModuleCategory("Camera", "DaHengCamera")]
    public class DaHengCameraModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterSingleton<DaHengCamera>();//注册相机到IOC容器中
            containerRegistry.Register<ICamera, DaHengCamera>("DaHengCamera");
        }
    }
}

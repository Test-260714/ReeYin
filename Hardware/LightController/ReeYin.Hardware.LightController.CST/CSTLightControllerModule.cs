using ReeYin.Hardware.LightController.CST.CustomUI.Views;
using ReeYin.Hardware.LightController.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController.CST
{
    /// <summary>
    /// CST光源控制器模块
    /// 使用特性为模块指定分类
    /// </summary>
    [ModuleCategory("LightController", "CST")]
    public class CSTLightControllerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            // 注册光源控制器实现到IOC容器
            containerRegistry.Register<ILightController, CSTLightController>("CSTLightController");

            // 注册调试对话框
            containerRegistry.RegisterDialog<CSTLightControllerView>();
        }
    }
}

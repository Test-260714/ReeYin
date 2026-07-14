using Custom.LineScan.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace Custom.LineScan
{
    public class CustomLineScanModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }
 
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            // 注册线扫测试平台节点（集成正运动控制卡和埃科相机）
            containerRegistry.RegisterDialogAndMenu<LineScanTestPlatformView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.Start,
                Title = "测试平台",
                Icon = "\ue6a2",
                Type = "11.线扫测试",
                Description = "控制测试平台",
                TargetType = typeof(LineScanTestPlatformView),
            });

            // 注册运动控制节点
           /* containerRegistry.RegisterDialogAndMenu<MotionControlView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.Start,
                Title = "运动控制",
                Icon = "\ue673",
                Type = "11.线扫测试",
                Description = "控制平台连续运动",
                TargetType = typeof(MotionControlView),
            });

            // 注册线扫采集节点
            containerRegistry.RegisterDialogAndMenu<LineScanView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.Start,
                Title = "线扫采集",
                Icon = "\ue673",
                Type = "11.线扫测试",
                Description = "控制相机图像采集",
                TargetType = typeof(LineScanView),
            });*/
        }
    }
}

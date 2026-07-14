using Custom.PlcImageCollect.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using System.Reflection;

namespace Custom.PlcImageCollect
{
    public class CustomPlcImageCollect : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialogAndMenu<PlcImageCollectView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "PLC触发采集",
                Icon = "\ue6a2",
                Type = "00.DataCollection",
                Description = "PLC事件触发传感器开始、停止采集并在停止采集时保存原图",
                TargetType = typeof(PlcImageCollectView),
            });
        }
    }
}

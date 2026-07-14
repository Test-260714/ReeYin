using HardwareTool.ZMotionOutput.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace HardwareTool.ZMotionOutput
{
    public class HardwareToolZMotionOutput : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<ZMotionNgOutputView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "正运动通用输出",
                Icon = "\ue670",
                Type = "04.Hardware",
                Description = "正运动IO输出源与规则配置",
                TargetType = typeof(ZMotionNgOutputView),
            });
        }
    }
}

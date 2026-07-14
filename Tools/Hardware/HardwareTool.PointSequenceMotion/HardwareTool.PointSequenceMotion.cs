using HardwareTool.PointSequenceMotion.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace HardwareTool.PointSequenceMotion
{
    public class HardwareToolPointSequenceMotion : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<PointSequenceMotionView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "PointSequenceMotion",
                Icon = "\ue626",
                Type = "04.Hardware",
                Description = "按执行次数或点位序号逐点触发运动控制卡移动",
                TargetType = typeof(PointSequenceMotionView),
            });
        }
    }
}

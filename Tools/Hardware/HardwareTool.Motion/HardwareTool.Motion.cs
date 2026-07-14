using HardwareTool.Motion.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace HardwareTool.Motion
{
    public class HardwareToolMotion : IModule
    {
        private const string REGION_NAME = "MotionOperationRegion";

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion<PointOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<IoOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<LineOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<ArcOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<PosCompareOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<DelayMotionOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<TriggerEventOperationView>(REGION_NAME);
            regionManager.RegisterViewWithRegion<CustomMotionOperationView>(REGION_NAME);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterForNavigation<PointOperationView>();
            containerRegistry.RegisterForNavigation<IoOperationView>();
            containerRegistry.RegisterForNavigation<LineOperationView>();
            containerRegistry.RegisterForNavigation<ArcOperationView>();
            containerRegistry.RegisterForNavigation<PosCompareOperationView>();
            containerRegistry.RegisterForNavigation<DelayMotionOperationView>();
            containerRegistry.RegisterForNavigation<TriggerEventOperationView>();
            containerRegistry.RegisterForNavigation<CustomMotionOperationView>();

            containerRegistry.RegisterDialogAndMenu<MotionShellView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "MotionModule",
                Icon = "\ue626",
                Type = "04.Hardware",
                Description = "用来操作运动控制卡的组件",
                TargetType = typeof(MotionShellView),
            });
        }
    }
}

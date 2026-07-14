using Custom.DefectOverview;
using Custom.DefectOverview.Services;
using Custom.XYHD.Services;
using Custom.XYHD.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Reflection;

namespace Custom.XYHD
{
    /// <summary>
    /// 医用敷料检测模块 - 线扫相机采集
    /// </summary>
    [ModuleDependency(nameof(DefectOverview))]
    public class CustomXYHDModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<XYHDDefectOverviewAdapterService>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterSingleton<XYHDDefectOverviewAdapterService>();
            containerRegistry.RegisterDialogAndMenu<DetectionPublishConfigView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "敷料检测",
                Icon = "\ue6a2",
                Type = "12.医用敷料",
                Description = "敷料检测",
                TargetType = typeof(DetectionView),
            });
            containerRegistry.RegisterDialog<DetectionPublishConfigView>();
            containerRegistry.RegisterDialog<DetectionPublishConfigView>("DetectionView");
            containerRegistry.RegisterForNavigation<DetectionPublishConfigView>("DetectionView");
            containerRegistry.RegisterDialogAndDynamic<DetectionView>("XYHDDetectionView", new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "敷料检测主页面",
                ViewName = "XYHDDetectionView"
            });
        }
    }
}

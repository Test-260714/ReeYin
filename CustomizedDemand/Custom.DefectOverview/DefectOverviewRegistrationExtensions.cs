using Custom.DefectOverview.Services;
using Custom.DefectOverview.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;

namespace Custom.DefectOverview
{
    public class DefectOverview : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(typeof(BandMapView).Assembly);
            containerRegistry.RegisterSingleton<IBandMapStateService, BandMapStateService>();
            containerRegistry.RegisterSingleton<IDefectOverviewPostProcessService, DefectOverviewPostProcessService>();
            containerRegistry.RegisterSingleton<IDefectOverviewIngestService, DefectOverviewIngestService>();
            containerRegistry.RegisterDialog<DefectDetailView>();
            containerRegistry.RegisterDialogAndDynamic<BandMapView>("BandMapView", new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "缺陷总览发布",
                ViewName = "BandMapView"
            });
            containerRegistry.RegisterDialogAndMenu<DefectOverviewPublishView>(null, new MenuInfo
            {
                NodeType = NodeType.Finish,
                TranslateKey = "",
                Title = "缺陷总览发布",
                Icon = "\xe67c",
                Type = "12.医用敷料",
                Description = "将缺陷结果发布到缺陷总览、缺陷墙和模拟图。",
                TargetType = typeof(DefectOverviewPublishView),
            });
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(typeof(BandMapView).Assembly);
        }
    }
}

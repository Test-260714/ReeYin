using Custom.KBTScraper.Models;
using Custom.KBTScraper.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace Custom.KBTScraper
{
    public class CustomKBTScraper : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            // 注册算法
            containerRegistry.RegisterSingleton<KBTGDJC_Algorithm>();

            // 注册视图
            containerRegistry.RegisterDialogAndDynamic<GrayShowView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "灰度图显示",
                ViewName = "GrayShowView"
            });
            
            // OtherConfigView 只注册 Dialog，动态视图由 SensorDataCollectionModel 在反序列化时添加
            containerRegistry.RegisterDialog<OtherConfigView>();
            
            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "刮刀检测",
                Icon = "\ue6a2",
                Type = "98.科百特",
                Description = "刮刀缺陷检测数据采集组件",
                TargetType = typeof(SensorDataCollectionView),
            });
            containerRegistry.RegisterDialogAndDynamic<AutoProcessView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "自动运行操作页面",
                ViewName = "AutoProcessView"
            });
            containerRegistry.RegisterDialog<ChartView>();
        }
    }
}

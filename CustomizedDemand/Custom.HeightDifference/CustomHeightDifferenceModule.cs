using Prism.Ioc;
using Prism.Modularity;
using ReeYin.Customized.Algo.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace Custom.HeightDifference
{
    public class CustomHeightDifferenceModule : IModule
    {
        /// <summary>
        /// 模块初始化时注册高度差测量弹窗导航入口。
        /// </summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// 向容器注册高度差测量模型和视图映射。
        /// </summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<HeightDifferenceMeasureView>("HeightDifferenceMeasureDialog");

            containerRegistry.RegisterDialogAndMenu<HeightDifferenceMeasureView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "高度差测量",
                Icon = "\xe67b",
                Type = "02.AlgorithmicTool",
                Description = "用于高度图或 PCD 的高度差测量页面",
                TargetType = typeof(HeightDifferenceMeasureView),
            });

            containerRegistry.RegisterDialogAndDynamic<HeightDifferenceMeasureView>("HeightDifferenceMeasureView", new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "高度差测量",
                ViewName = "HeightDifferenceMeasureView"
            });
        }
    }
}

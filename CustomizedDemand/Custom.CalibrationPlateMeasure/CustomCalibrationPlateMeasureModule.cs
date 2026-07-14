using Custom.CalibrationPlateMeasure.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace Custom.CalibrationPlateMeasure
{
    // Prism 模块入口：注册标准片测量视图、运行模式视图项目和菜单项。
    public sealed class CustomCalibrationPlateMeasureModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<CalibrationPlateMeasureView>("CalibrationPlateMeasureDialog");

            containerRegistry.RegisterDialogAndMenu<CalibrationPlateMeasureView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "标准片测量",
                Icon = "\xe679",
                Type = "02.AlgorithmicTool",
                Description = "读取 TIFF 高度图并测量标准片目标区域的长度、宽度和深度。",
                TargetType = typeof(CalibrationPlateMeasureView),
            });

            containerRegistry.RegisterDialogAndDynamic<CalibrationPlateMeasureView>("CalibrationPlateMeasureView", new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "标准片测量",
                ViewName = "CalibrationPlateMeasureView"
            });
        }
    }
}

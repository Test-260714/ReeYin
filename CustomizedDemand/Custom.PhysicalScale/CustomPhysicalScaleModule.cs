using Custom.PhysicalScale.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace Custom.PhysicalScale
{
    public class CustomPhysicalScaleModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<PhysicalScaleView>("PhysicalScaleDialog");
            containerRegistry.RegisterDialog<PhysicalScaleSharpenPreviewView>("PhysicalScaleSharpenPreviewDialog");

            containerRegistry.RegisterDialogAndMenu<PhysicalScaleView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.Start,
                Title = "物理标尺",
                Icon = "\ue6a2",
                Type = "11.线扫测试",
                Description = "离线图像物理量测与微孔判定",
                TargetType = typeof(PhysicalScaleView),
            });

            containerRegistry.RegisterDialogAndDynamic<PhysicalScaleView>("PhysicalScaleView", new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "物理标尺",
                ViewName = "PhysicalScaleView"
            });
        }
    }
}

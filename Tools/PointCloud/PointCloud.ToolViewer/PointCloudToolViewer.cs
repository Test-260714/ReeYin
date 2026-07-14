using PointCloud.ToolViewer.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace PointCloud.ToolViewer;

public class PointCloudToolViewer : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());


        containerRegistry.RegisterDialogAndDynamic<PointCloudViewerView>(null, new DynamicView
        {
            Type = DynamicViewType.General,
            DisplayName = "点云控件",
            ViewName = "LogView"
        });
        //containerRegistry.RegisterDialogAndMenu<PointCloudViewerView>(null, new MenuInfo
        //{
        //    NodeType = NodeType.General,
        //    TranslateKey = "",
        //    Title = "PointCloudViewer",
        //    Icon = "\ue640",
        //    Type = "03.PointCloud",
        //    Description = "点云文件加载、渲染显示与交互浏览模块",
        //    TargetType = typeof(PointCloudViewerView),
        //});
    }
}

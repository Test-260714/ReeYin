using ReeYin.ChartShow.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.UI.UserControls.ImageRedactor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.ChartShow
{
    /// <summary>
    /// 图表控件
    /// </summary>
    public class ReeYinChartShow : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndDynamic<HalconImageView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "2D图显示",
                ViewName = "HalconImageView"
            });
            containerRegistry.RegisterDialog<HalconImageView>();

            containerRegistry.RegisterDialogAndDynamic<LightingChart3DView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "3D图显示(LC)",
                ViewName = "LightingChart3DView"
            });

            containerRegistry.RegisterDialogAndDynamic<PointCloud>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "点云图显示(LC)",
                ViewName = "PointCloud"
            });

            containerRegistry.RegisterDialogAndDynamic<LineSeriesView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "折线极值图(LC)",
                ViewName = "LineSeriesView"
            });

            containerRegistry.RegisterDialogAndDynamic<DefectMapDemoView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "卷材缺陷映射(LC)",
                ViewName = "DefectMapDemoView"
            });

            containerRegistry.RegisterDialogAndDynamic<ImageRedactorView>("ImageRedactorView", new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "图像标注编辑(LC)",
                ViewName = "ImageRedactorView"
            });
        }
    }
}

using Custom.KBTBox.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Custom.KBTBox
{
    public class CustomKBTBox : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterSingleton<KBTDispensing_Algorithm>();
            containerRegistry.Register<ICustomAlgo, KBTDispensing_Algorithm>("KBTDispensing_Algorithm");

            //containerRegistry.RegisterDialogAndDynamic<GrayShowView>(null, new DynamicView
            //{
            //    //Subjection = "Custom.KBTBox",
            //    Type = DynamicViewType.Custom,
            //    DisplayName = "灰度图显示",
            //    ViewName = "GrayShowView"
            //});
            containerRegistry.RegisterForNavigation<GrayShowView>();
            containerRegistry.RegisterForNavigation<AutoProcessView>();
            containerRegistry.RegisterForNavigation<JudgmentConfigView>();
            containerRegistry.RegisterForNavigation<OtherConfigView>();
            //containerRegistry.RegisterDialog<OtherConfigView>();
            //containerRegistry.RegisterDialog<JudgmentConfigView>();
            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "其他采集",
                Icon = "\ue6a2",
                Type = "99.科百特",
                Description = "用来采集传感器数据的组件",
                TargetType = typeof(SensorDataCollectionView),
            });

            containerRegistry.RegisterDialogAndDynamic<HPManualView>(null, new DynamicView
            {
                //Subjection = "Custom.KBTBox",
                Type = DynamicViewType.Custom,
                DisplayName = "传感器手动操作页面",
                ViewName = "HPManualView"
            });

            //containerRegistry.RegisterDialogAndDynamic<AutoProcessView>(null, new DynamicView
            //{
            //    //Subjection = "Custom.KBTBox",
            //    Type = DynamicViewType.Custom,
            //    DisplayName = "自动运行操作页面",
            //    ViewName = "AutoProcessView"
            //});
        }
    }
}

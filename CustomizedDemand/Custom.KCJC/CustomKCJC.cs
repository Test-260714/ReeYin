using Custom.KCJC.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC
{
    public class CustomKCJC : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialog<CalibrationView>();
            containerRegistry.RegisterDialog<OtherConfigView>();
            containerRegistry.RegisterDialog<InfoConfigView>();

            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "OtherCollection",
                Icon = "\ue6a2",
                Type = "99.GrooveDetection",
                Description = "用来采集传感器数据的组件",
                TargetType = typeof(SensorDataCollectionView),
            });

            containerRegistry.RegisterDialogAndDynamic<JudgementChart>(null, new DynamicView
            {
                //Subjection = "Custom.KCJC",
                Type = DynamicViewType.Custom,
                DisplayName = "刻槽检测判定表",
                ViewName = "JudgementChart"
            });

            containerRegistry.RegisterDialogAndDynamic<PartitionChart>(null, new DynamicView
            {
                //Subjection = "Custom.KCJC",
                Type = DynamicViewType.Custom,
                DisplayName = "刻槽检测分区表",
                ViewName = "PartitionChart"
            });

            containerRegistry.RegisterDialogAndDynamic<StatisticsTableChart>(null, new DynamicView
            {
                //Subjection = "Custom.KCJC",
                Type = DynamicViewType.Custom,
                DisplayName = "刻槽检测统计表",
                ViewName = "StatisticsTableChart"
            });

        }
    }
}

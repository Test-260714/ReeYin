
using Custom.EVEMFDJC.Models;
using Custom.EVEMFDJC.ViewModels;
using Custom.EVEMFDJC.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;
using static Custom.EVEMFDJC.Models.EVEMFDJC0_Algorithm;

namespace Custom.EVEMFDJC
{
    public class CustomEVEMFDJC : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<EVEMFDJC0_Algorithm>();
            containerRegistry.Register<ICustomAlgo, EVEMFDJC0_Algorithm>("EVEMFDJC0_Algorithm");
            containerRegistry.RegisterSingleton<MFDJC0_MeasureParam>();
            containerRegistry.RegisterDialogAndMenu<EveSensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "yiwei",
                Icon = "\ue6a2",
                Type = "00.DataCollection",
                Description = "用来采集传感器数据的组件",
                TargetType = typeof(EveSensorDataCollectionView),
            });

            containerRegistry.RegisterForNavigation<OtherConfigView>();

            containerRegistry.RegisterForNavigation<EveAlarmLogView>();

            containerRegistry.RegisterForNavigation<ConfigureaAlarm>();

            containerRegistry.RegisterForNavigation<EvePlcControlView>();

            containerRegistry.RegisterDialogAndDynamic<ChartView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "高/灰度图显示",
                ViewName = "ChartView"
            });
        }
    }
}

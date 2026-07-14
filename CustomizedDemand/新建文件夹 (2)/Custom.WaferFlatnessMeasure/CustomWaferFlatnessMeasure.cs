using Custom.WaferFlatnessMeasure.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace Custom.WaferFlatnessMeasure
{
    public class CustomWaferFlatnessMeasure : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterSingleton<Flatness_Algorithm>();
            containerRegistry.RegisterForNavigation<ResultDisposeView>();
            containerRegistry.RegisterForNavigation<WaferTrajectoryMonitorDemoView>();
            containerRegistry.RegisterForNavigation<WaferFlatnessResultChartView>();
            containerRegistry.RegisterDialogAndMenu<WaferFlatnessConfigView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                Title = "规划配置",
                Icon = "\ue784",
                Type = "97.WaferMeasurement",
                Description = "用来进行晶圆宏观平面几何测量的弹窗",
                TargetType = typeof(WaferFlatnessConfigView),
            });

            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "其他采集",
                Icon = "\ue6a2",
                Type = "99.半导体设备",
                Description = "半导体设备测试与通道标定",
                TargetType = typeof(SensorDataCollectionView),
            });

            containerRegistry.RegisterDialogAndMenu<GripperClampControlView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "夹爪夹紧",
                Icon = "\ue6a2",
                Type = "100.半导体设备",
                Description = "通过PLC地址执行夹爪夹紧、松开和到位状态读取",
                TargetType = typeof(GripperClampControlView),
            });
        }
    }
}

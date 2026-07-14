using Custom.MFDJC.Models;
using Custom.MFDJC.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Base;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
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

namespace Custom.MFDJC
{

    public class CustomMFDJC : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterSingleton<ElectroStaticChuck_Algorithm>();
            containerRegistry.Register<ICustomAlgo, ElectroStaticChuck_Algorithm>("ElectroStaticChuck_Algorithm");

            var module = PrismProvider.Container.Resolve(typeof(ElectroStaticChuck_Algorithm)) as ElectroStaticChuck_Algorithm;

            containerRegistry.RegisterDialogAndDynamic<ChartView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "高/灰度图显示",
                ViewName = "ChartView"
            });

            containerRegistry.RegisterDialogAndDynamic<ElectroStaticChuckPreviewView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "静电卡盘图像预览",
                ViewName = "ElectroStaticChuckPreviewView"
            });

            containerRegistry.RegisterDialog<OtherConfigView>();

            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "OtherCollection",
                Icon = "\ue6a2",
                Type = "00.DataCollection",
                Description = "用来采集传感器数据的组件",
                TargetType = typeof(SensorDataCollectionView),
            });

        }
    }
}

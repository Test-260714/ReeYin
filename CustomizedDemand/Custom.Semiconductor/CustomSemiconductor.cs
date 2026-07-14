using Custom.Semiconductor.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.Semiconductor
{
    public class CustomSemiconductor : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<SensorDataCollectionView>(null, new MenuInfo
            {
                ModuleType = ModuleType.Custom,
                NodeType = NodeType.General,
                Title = "其他采集",
                Icon = "\ue6a2",
                Type = "99.半导体设备",
                Description = "半导体设备测试",
                TargetType = typeof(SensorDataCollectionView),
            });
        }
    }
}

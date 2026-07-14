
using Custom.ElectroStaticChuckMeasure.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.ElectroStaticChuckMeasure
{
    public class CustomElectroStaticChuckMeasure : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<ElectroStaticChuckMeasureView>();

            containerRegistry.RegisterDialogAndMenu<ElectroStaticChuckMeasureView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                Title = "静电吸附盘几何测量",
                Icon = "\ue784",
                Type = "97.晶圆测量",
                Description = "用来进行静电吸附盘几何测量的弹窗",
                TargetType = typeof(ElectroStaticChuckMeasureView),
            });
        }
    }

}

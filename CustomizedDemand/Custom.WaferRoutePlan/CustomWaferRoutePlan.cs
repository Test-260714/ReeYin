
using Custom.WaferRoutePlan.Views;
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

namespace Custom.WaferRoutePlan
{
    public class CustomWaferRoutePlan : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterSingleton<RoutePlan_Algorithm>();

            //containerRegistry.RegisterDialogAndMenu<WaferRoutePlanView>(null, new MenuInfo
            //{
            //    NodeType = NodeType.General,
            //    Title = "WaferPathPlanning",
            //    TranslateKey = "",
            //    Icon = "\xe667",
            //    Type = "97.WaferMeasurement",
            //    Description = "用来进行晶圆路径规划的弹窗",
            //    TargetType = typeof(WaferRoutePlanView),
            //});

            containerRegistry.RegisterDialogAndMenu<PixelToActualCoordinateView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                Title = "PixelToActualCoordinate",
                TranslateKey = "",
                Icon = "\xe667",
                Type = "97.WaferMeasurement",
                Description = "根据拍照实际坐标和圆心像素坐标计算圆心实际坐标的弹窗",
                TargetType = typeof(PixelToActualCoordinateView),
            });
        }
    }
}

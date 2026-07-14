using ALGO.MeasureCircle.Views;
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

namespace ALGO.MeasureCircle
{
    /// <summary>
    /// 圆测量模块
    /// </summary>
    public class MeasureCircle : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<MeasureCircleView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "CircularMeasurement",
                Icon = "\ue61d",
                Type = "02.AlgorithmicTool",
                Description = "用来进行圆形测量的弹窗",
                TargetType = typeof(MeasureCircleView),
            });
        }
    }
}

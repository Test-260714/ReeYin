using ALGO.MeasureRect.Views;
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

namespace ALGO.MeasureRect
{
    public class MeasureRect : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<MeasureRectView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "RectangularMeasurement",
                Icon = "\ue62b",
                Type = "02.AlgorithmicTool",
                Description = "用来进行矩形测量的弹窗",
                TargetType = typeof(MeasureRectView),
            });
        }
    }

}

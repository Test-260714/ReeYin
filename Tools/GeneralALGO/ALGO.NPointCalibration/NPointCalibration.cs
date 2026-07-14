using ALGO.NPointCalibration.Views;
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

namespace ALGO.NPointCalibration
{
    /// <summary>
    /// 多点标定工具
    /// </summary>
    public class NPointCalibration : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<NPointCalibrationView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "N-PointCalibration",
                Icon = "\ue653",
                Type = "02.AlgorithmicTool",
                Description = "多点标定工具",
                TargetType = typeof(NPointCalibrationView),
            });
        }
    }
}

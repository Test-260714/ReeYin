using ALGO.MultiCameraCalibration.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ALGO.MultiCameraCalibration
{
    public class ALGOMultiCameraCalibration : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<MultiCameraCalibrationView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "MultiCamCalib",
                Icon = "\ue69b",
                Type = "02.AlgorithmicTool",
                Description = "多相机联合标定工具",
                TargetType = typeof(MultiCameraCalibrationView),
            });
        }
    }
}

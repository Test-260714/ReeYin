using ALGO.NinePointCalibration.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.NinePointCalibration
{
    /// <summary>
    /// 九点标定工具。
    /// </summary>
    public class NinePointCalibration : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<NinePointCalibrationView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = string.Empty,
                Title = "NinePointCalibration",
                Icon = "\ue653",
                Type = "02.AlgorithmicTool",
                Description = "九点标定工具",
                TargetType = typeof(NinePointCalibrationView),
            });
        }
    }
}

using ALGO.BlobAnalysis.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.BlobAnalysis
{
    /// <summary>
    /// Blob分析模块
    /// </summary>
    public class BlobAnalysis : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<BlobAnalysisView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "BlobAnalysis",
                Icon = "\ue723",
                Type = "02.AlgorithmicTool",
                Description = "用来进行Blob分析的弹窗",
                TargetType = typeof(BlobAnalysisView),
            });
        }
    }
}

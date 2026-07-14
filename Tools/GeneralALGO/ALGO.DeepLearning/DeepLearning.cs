using ALGO.DeepLearning.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.DeepLearning
{
    /// <summary>
    /// 模型推理模块入口。
    /// </summary>
    public class DeepLearning : IModule
    {
        #region 模块初始化
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }
        #endregion

        #region 视图注册
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<DeepLearningView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "DeepLearning",
                Icon = "\ue657",
                Type = "02.AlgorithmicTool",
                Description = "深度学习模型推理工具",
                TargetType = typeof(DeepLearningView),
            });
        }
        #endregion
    }
}

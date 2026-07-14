using ALGO.DefectPostProcess.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.DefectPostProcess
{
    /// <summary>
    /// 作为缺陷后处理模块的入口类。
    /// </summary>
    public class DefectPostProcess : IModule
    {
        /// <summary>
        /// 初始化模块程序集中的相关服务。
        /// </summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// 注册模块使用的视图、弹窗与菜单入口。
        /// </summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<DefectFeatureValuesView>();

            containerRegistry.RegisterDialogAndMenu<DefectPostProcessView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "DefectPostProcess",
                Icon = "\xe67a",
                Type = "02.AlgorithmicTool",
                Description = "缺陷后处理工具",
                TargetType = typeof(DefectPostProcessView),
            });
        }
    }
}

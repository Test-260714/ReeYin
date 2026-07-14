using ALGO.FindCode.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.FindCode
{
    /// <summary>
    /// 扫码识别模块入口，负责注册参数弹窗和节点菜单信息。
    /// </summary>
    public class FindCode : IModule
    {
        #region 模块注册
        /// <summary>
        /// 模块加载后初始化当前程序集内的扩展服务。
        /// </summary>
        /// <param name="containerProvider">Prism 容器提供器。</param>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        /// <summary>
        /// 注册扫码识别参数页和工具箱菜单项。
        /// </summary>
        /// <param name="containerRegistry">Prism 容器注册器。</param>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<FindCodeView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ScanAndRecognize",
                Icon = "\ue643",
                Type = "02.AlgorithmicTool",
                Description = "用于识别一维码和二维码的弹窗",
                TargetType = typeof(FindCodeView),
            });
        }
        #endregion
    }

}

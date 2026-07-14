using ALGO.ShapeMatching.Views;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System.Reflection;

namespace ALGO.ShapeMatching
{
    /// <summary>
    /// 形状匹配算法模块的 Prism 注册入口。
    /// </summary>
    public class ShapeMatching : IModule
    {
        #region 模块初始化

        /// <summary>
        /// 初始化当前程序集中的导出服务和视图注册。
        /// </summary>
        /// <param name="containerProvider">Prism 容器提供器。</param>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// 注册形状匹配参数弹窗和流程节点菜单。
        /// </summary>
        /// <param name="containerRegistry">Prism 容器注册器。</param>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<ShapeMatchingView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ShapeMatching",
                Icon = "\ue654",
                Type = "02.AlgorithmicTool",
                Description = "用来进行形状匹配的弹窗",
                TargetType = typeof(ShapeMatchingView),
            });
        }

        #endregion
    }
}

using LogicalTool.BranchRouter.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace LogicalTool.BranchRouter
{
    public class LogicalToolBranchRouter : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<BranchRouterView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "BranchRouter",
                Icon = "\ue99b",
                Type = "01.LogicModule",
                Description = "根据参数值选择已连接节点执行的分支路由组件",
                TargetType = typeof(BranchRouterView),
            });
        }
    }
}

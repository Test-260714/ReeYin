using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Share.Prism;
using ReeYin_V.UserManager.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin_V.UserManager
{
    [Module(ModuleName = ModuleNames.ApplicationUserManagerModule, OnDemand = true)]
    public class ApplicationUserManagerModule : IModule
    {
        //先注册
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialog<UserManagerView>();

            containerRegistry.RegisterDialog<RoleManageView>();

            containerRegistry.RegisterDialog<RoleModificationView>();

            containerRegistry.RegisterDialog<PermissionManageView>();

            containerRegistry.RegisterDialog<EditUserView>();

            containerRegistry.RegisterDialog<EditPermissionView>();

            containerRegistry.RegisterDialog<UserSwitchView>();
            containerRegistry.RegisterDialogAndDynamic<UserAndStatusView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "用户和状态控件",
                ViewName = "UserAndStatusView"
            });

        }

        //后初始化
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }
    }
}

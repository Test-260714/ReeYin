using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Login.Views;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Login
{
    /// <summary>
    /// 登录模块 - 按需延迟加载
    /// </summary>
    [Module(ModuleName = ModuleNames.ApplicationLoginModule, OnDemand = true)]
    [ModuleDependency(ModuleNames.ApplicationUserManagerModule)] //登陆页面依赖于用户管理模块
    public class ApplicationLoginModule : IModule
    {
        //先注册
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<LoginView>();
            containerRegistry.RegisterDialog<ExitView>();
        }

        //后初始化
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }
    }
}

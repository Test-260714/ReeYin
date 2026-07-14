using LogicalTool.ParamLink.Views;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.ParamLink
{
    public class LogicalToolParamLink : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialog<ParamLinkView>();
            containerRegistry.RegisterDialog<CustomVariableListView>();
        }
    }
}

using ReeYin.RecipeManager.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.RecipeManager
{
    public class ApplicationRecipeManager : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<RecipeManagerView>();
            containerRegistry.RegisterDialogAndDynamic<RecipeSwitcherView>(null, new DynamicView
            {
                Type = DynamicViewType.General,
                DisplayName = "配方切换",
                ViewName = "RecipeSwitcherView"
            });
        }
    }
}

using Custom.Semiconductor.Test.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.Semiconductor.Test
{
    public class CustomSemiconductorTest : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            //containerRegistry.RegisterDialog<ConcentricPointsView>();
            containerRegistry.RegisterDialogAndDynamic<ConcentricPointsView>(null, new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "示意图",
                ViewName = "ConcentricPointsView"
            });

        }
    }
}

using GoogolMotion.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.ControlCard.Googol.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Googol
{
    [ModuleCategory("ControlCard", "Googol")]
    public class GoogolControlCardModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<IControlCard, GoogolControlCard>("GoogolControlCard");


            containerRegistry.RegisterDialog<GoogolCustomView>();
        }
    }
}

using ReeYin.Hardware.ControlCard.Leisai.App;
using ReeYin.Hardware.ControlCard.Leisai.Packging;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.ControlCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.ControlCard.Leisai
{
    [ModuleCategory("ControlCard", "Leisai")]
    public class LeisaiControlCardModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.Register<IControlCard, LeisaiEMCControlCard>("LeisaiEMCControlCard");


            //containerRegistry.RegisterDialog<GoogolCustomView>();
        }
    }
}

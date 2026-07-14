using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.ControlCard.ACS.App;
using System.Reflection;

namespace ReeYin_V.Hardware.ControlCard.ACS;

[ModuleCategory("ControlCard", "ACS")]
public class AcsControlCardModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
        containerRegistry.Register<IControlCard, AcsControlCard>("ACSControlCard");
        containerRegistry.Register<ControlCardBase, AcsControlCard>("ACSControlCard");
    }
}

using ReeYin.Hardware.LightController.Models;
using ReeYin.Hardware.LightController.Rsee.CustomUI.Views;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using System.Reflection;

namespace ReeYin.Hardware.LightController.Rsee;

[ModuleCategory("LightController", "Rsee")]
public class RseeLightControllerModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
        containerRegistry.Register<ILightController, RseeLightController>("RseeLightController");
        containerRegistry.RegisterDialog<RseeLightControllerView>();
    }
}

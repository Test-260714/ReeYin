using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services;
using ReeYin_V.Hardware.ControlCard.ZMotion.App;
using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Views;
using System.Reflection;

namespace ReeYin_V.Hardware.ControlCard.ZMotion
{
    [ModuleCategory("ControlCard", "ZMotion")]
    public class ZMotionControlCardModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.Register<IControlCard, ZMotionControlCard>("ZMotionControlCard");
            containerRegistry.Register<ControlCardBase, ZMotionControlCard>("ZMotionControlCard");
            containerRegistry.RegisterDialog<ZMotionCustomView>();
        }
    }
}

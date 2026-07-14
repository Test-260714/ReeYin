using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using System.Reflection;

namespace ReeYin.Customized.Algo
{

    public class AlgoBaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

        }
    }
}

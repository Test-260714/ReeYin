using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.None
{
    public class NoneControlCardModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            if (!containerRegistry.IsRegistered<IControlCard>())
            {
                 containerRegistry.RegisterSingleton<IControlCard, NoneControlCard>();//注册仿真控制卡到IOC容器中
            }
        }
    }
}

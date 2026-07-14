using ALGO.RegionTrans.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ALGO.RegionTrans
{
    public class RegionTrans : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<RegionTransView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ZoneTransfer",
                Icon = "\ue656",
                Type = "02.AlgorithmicTool",
                Description = "用来进行区域转换的弹窗",
                TargetType = typeof(RegionTransView),
            });
        }
    }

}

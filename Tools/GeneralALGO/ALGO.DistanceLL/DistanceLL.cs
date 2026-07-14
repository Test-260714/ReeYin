using ALGO.DistanceLL.Views;
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

namespace ALGO.DistanceLL
{
    /// <summary>
    /// 取图模块
    /// </summary>
    public class DistanceLL : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<DistanceLLView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                Title = "DistanceBetweenLines",
                TranslateKey = "",
                Icon = "\ue80f",
                Type = "02.AlgorithmicTool",
                Description = "线线距离计算工具",
                TargetType = typeof(DistanceLLView),
            });
        }
    }
}

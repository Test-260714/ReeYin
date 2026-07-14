using ALGO.ImagePerProcessing.Views;
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

namespace ALGO.ImagePerProcessing
{
    /// <summary>
    /// 取图模块
    /// </summary>
    public class ImagePerProcessing : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<ImagePerProcessingView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "PhotoPretreatment",
                Icon = "\ue63c",
                Type = "02.AlgorithmicTool",
                Description = "图片预处理工具",
                TargetType = typeof(ImagePerProcessingView),
            });
        }
    }
}

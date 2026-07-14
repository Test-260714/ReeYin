using ImageTool.GrabImage.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Extension;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReeYin_V.Core;

namespace ImageTool.GrabImage
{
    /// <summary>
    /// 取图模块
    /// </summary>
    public class ImageToolGrab : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterDialogAndMenu<CollectImageView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "TakeAPicture",
                Icon = "\ue673",
                Type = "00.DataCollection",
                Description = "用来进行取图的弹窗",
                TargetType = typeof(CollectImageView),
            });

            containerRegistry.RegisterDialogAndMenu<ContinuousGrabView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "ConsecutiveCollection",
                Icon = "\ue673",
                Type = "00.DataCollection",
                Description = "相机连续采集模块",
                TargetType = typeof(ContinuousGrabView),
            });
        }
    }
}

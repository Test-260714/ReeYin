using ImageTool.SaveImage.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Conditional
{
    public class LogicalToolSaveImage : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

            containerRegistry.RegisterDialogAndMenu<SaveImageView>(null, new MenuInfo
            {
                NodeType = NodeType.General,
                TranslateKey = "",
                Title = "SaveImage",
                Icon = "\ue641",
                Type = "03.FileDandling",
                Description = "用来存图的工具",
                TargetType = typeof(SaveImageView),
            });
        }
    }
}

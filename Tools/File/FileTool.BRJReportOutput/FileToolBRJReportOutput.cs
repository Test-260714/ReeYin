using FileTool.BRJReportOutput.Views;
using FileTool.BRJReportOutput.Services;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using System.Reflection;

namespace FileTool.BRJReportOutput
{
    public class FileToolBRJReportOutput : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeAssembly(Assembly.GetExecutingAssembly());
            BrjReportBatchSyncSubscriber.Subscribe(containerProvider);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<BrjReportOutputView>();

            PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
            {
                Type = DynamicViewType.Custom,
                DisplayName = "BRJ报表输出",
                ViewName = nameof(BrjReportOutputView),
                Subjection = "FileTool.BRJReportOutput",
            });
        }
    }
}

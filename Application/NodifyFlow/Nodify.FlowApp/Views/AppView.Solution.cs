using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Events;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Nodify.FlowApp
{
    /// <summary>
    /// AppView 解决方案操作部分
    /// </summary>
    public partial class AppView : UserControl
    {
        /// <summary>
        /// 操作解决方案
        /// </summary>
        /// <param name="order"></param>
        public void OperateSolution(string order)
        {
            switch (order)
            {
                case "打开":
                    this.DataContext = PrismProvider.ProjectManager.SolutionManager.GetItem("NodifyAppView") ?? new AppViewModel();

                    break;

                case "文件打开":
                    this.DataContext = PrismProvider.ProjectManager.SolutionManager.GetItem("NodifyAppView") ?? new AppViewModel();
                    break;

                case "保存":
                    PrismProvider.ProjectManager.SolutionManager.UpdateItem("NodifyAppView", this.DataContext);
                    break;

                case "释放":
                    {
                        CleanupCurrentSolution();
                    }
                    break;

                case "导入旧项目文件":
                    {
                        var dialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "旧项目文件 (*.ryv)|*.ryv|所有文件 (*.*)|*.*",
                            Title = "导入旧项目文件"
                        };

                        if (dialog.ShowDialog() != true)
                            break;

                        if (!LegacyProjectImporter.TryLoad(dialog.FileName, out var importedAppView, out var message))
                        {
                            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }

                        CleanupCurrentSolution();
                        this.DataContext = importedAppView;
                    }
                    break;
            }
        }

        /// <summary>
        /// 清理当前解决方案
        /// </summary>
        private void CleanupCurrentSolution()
        {
            var currentViewModel = this.DataContext as AppViewModel;
            if (currentViewModel?.GraphViewModel == null)
                return;

            currentViewModel.GraphViewModel.Release();

            if (currentViewModel.GraphViewModel.Nodes.Count > 0)
            {
                currentViewModel.GraphViewModel = null;
                PrismProvider.EventAggregator.GetEvent<ProjectRelatedEvent>().Publish("释放");
            }
        }

    }
}

using Custom.KCJC.Models.ALGO;
using HandyControl.Controls;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC.ViewModels
{
    public partial class SensorDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields


        #endregion



        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> DCGeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "打开图像预览":
                    PrismProvider.DialogService.Show("ChartView", new DialogParameters
                        {
                            { "Title", "高度/灰度图预览" },
                            { "Icon", "\ue673" },
                        }, result =>
                        {

                        }, nameof(DialogWindowView));
                    break;

                case "执行标定":
                    {
                        if (!ModelParam.CalibMethod())
                        {
                            MessageBox.Show("执行标定失败！");
                        }
                    }break;
                case "执行拼接":
                    {
                        if (!ModelParam.Concatenation())
                        {
                            MessageBox.Show("执行拼接失败！");
                        }
                    }break;

                case "获取标定路径":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                            {
                                folderDialog.Description = "请选择文件夹";
                                folderDialog.ShowNewFolderButton = true;

                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    ModelParam.CalibPath = folderDialog.SelectedPath;
                                }
                            }
                        });
                    }break;
                case "获取拼接路径":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                            {
                                folderDialog.Description = "请选择文件夹";
                                folderDialog.ShowNewFolderButton = true;

                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    ModelParam.ConcatenationPath = folderDialog.SelectedPath;
                                }
                            }
                        });
                    }
                    break;
                case "执行":
                    {

                    }break;
                case "确认":
                    {

                    }
                    break;
                default:
                    break;
            }
        });
        #endregion
    }
}

using Custom.WaferFlatnessMeasure.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    [Serializable]
    public class SensorDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Properties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { SetProperty(ref _sltOutputParamName, value); }
        }
        public new SensorDataCollectionModel ModelParam
        {
            get { return base.ModelParam as SensorDataCollectionModel; }
            set { base.ModelParam = value; }
        }


        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { SetProperty(ref _currentOutputParam, value); }
        }

        private DataAnalysisDataSourceOption? _currentDataAnalysisDataSource;
        public DataAnalysisDataSourceOption? CurrentDataAnalysisDataSource
        {
            get { return _currentDataAnalysisDataSource; }
            set { SetProperty(ref _currentDataAnalysisDataSource, value); }
        }

        #endregion

        #region Methods
        public override void InitParam()
        {
            ModelParam = InitModelParam<SensorDataCollectionModel>();
            ModelParam.LoadKeyParam();

        }

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
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
                case "切换模块":
                case "SwitchModule":
                    {
                        if (ModelParam?.SltModel != null)
                        {
                            ModelParam.SltModelName = ModelParam.SltModel.NickName;
                        }
                        else
                        {
                            MessageBox.Show("选中的传感器模块无效。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    break;
                case "StartCollect":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    if (!ModelParam.StartChannelCalibrationCollect(out string startMessage))
                    {
                        MessageBox.Show(startMessage, "开始采集失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "StopCollect":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    if (!ModelParam.StopChannelCalibrationCollect(out string stopMessage))
                    {
                        MessageBox.Show(stopMessage, "停止采集失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "SolveABC":
                case "CalculateC":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    if (!ModelParam.SolveChannelCalibration(out string solveMessage))
                    {
                        MessageBox.Show(solveMessage, "计算失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "ClearRawPoints":
                    ModelParam?.ClearChannelCalibrationRawPoints();
                    break;
                case "AddDataAnalysisDataSource":
                    ModelParam?.AddDataAnalysisDataSource();
                    break;
                case "DeleteDataAnalysisDataSource":
                    if (ModelParam != null)
                    {
                        ModelParam.RemoveDataAnalysisDataSource(CurrentDataAnalysisDataSource);
                        CurrentDataAnalysisDataSource = null;
                    }
                    break;
                case "ResetDataAnalysisDataSource":
                    ModelParam?.ResetDataAnalysisDataSources();
                    break;
                case "SelectCalibrationSourceFile":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    SelectCalibrationSourceFilePath(
                        selectedPath => ModelParam.CalibrationSourceFilePath = selectedPath,
                        ModelParam.CalibrationSourceFilePath);
                    break;
                case "SelectRawPointCloudPlyPath":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    SelectPointCloudOutputFilePath(
                        selectedPath => ModelParam.RawPointCloudPlyPath = selectedPath,
                        ModelParam.RawPointCloudPlyPath,
                        "请选择原始点云 PLY 输出路径",
                        "RawPointCloudPly.ply");
                    break;
                case "SelectResidualPointCloudPlyPath":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    SelectPointCloudOutputFilePath(
                        selectedPath => ModelParam.ResidualPointCloudPlyPath = selectedPath,
                        ModelParam.ResidualPointCloudPlyPath,
                        "请选择残差点云 PLY 输出路径",
                        "ResidualPointCloud.ply");
                    break;
                case "SelectPointCloudOutputDirectory":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    SelectFolderPath(
                        selectedPath => ModelParam.PointCloudOutputDirectory = selectedPath,
                        ModelParam.PointCloudOutputDirectory,
                        "请选择 RunALGO 点云/高度图导出文件夹");
                    break;
                case "SelectPreDatasCsvDirectory":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    SelectFolderPath(
                        selectedPath => ModelParam.PreDatasCsvDirectory = selectedPath,
                        ModelParam.PreDatasCsvDirectory,
                        "请选择 PreDatas CSV 存储文件夹");
                    break;
                case "ResetPointTemperatureAddresses":
                    ModelParam?.ResetPointTemperatureAddresses();
                    break;
                case "CalibFromFile":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    if (!ModelParam.CalibFromFile(out string calibFromFileMessage))
                    {
                        MessageBox.Show(calibFromFileMessage, "文件标定失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "Cancel":
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        ModelParam?.ExecuteModule();
                    });

                    break;
                case "Confirm":
                case "确认":
                    {
                        if (ModelParam == null)
                        {
                            break;
                        }

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                case "ExportConfig":

                    //MessageBox.Show("导出配置成功！");

                    break;

                case "获取路径":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                            {
                                folderDialog.Description = "请选择文件夹";
                                folderDialog.ShowNewFolderButton = true;

                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    ModelParam.FilePath = folderDialog.SelectedPath;
                                }
                            }
                        });
                    }
                    break;

                case "SelectRawDataCsvFile":
                case "打开文件":
                    {
                        if (ModelParam == null)
                        {
                            break;
                        }

                        SelectRawDataCsvFilePath(
                            selectedPath =>
                            {
                                ModelParam.SltFile = selectedPath;
                                ModelParam.FilePath = selectedPath;
                            },
                            ModelParam.FilePath);
                    }
                    break;
                case "链接路径":

                    ModelParam.IsLinkVisibility = Visibility.Visible;

                    break;
                case "选择文件":

                    ModelParam.IsLinkVisibility = Visibility.Hidden;
                    break;
                default:
                    break;
            }

        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam == null)
            {
                return;
            }

            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            RestoreSelectedSensor();
            ModelParam.LoadKeyParam();
            SltOutputParamName ??= ModelParam.OutputParamNames?.FirstOrDefault();

            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            if (ModelParam == null)
            {
                return;
            }

            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                        !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object resourceObject) ||
                        resourceObject is not TransmitParam curSltParam)
                    {
                        return;
                    }

                    if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                    {
                        MessageBox.Show("已包含重名参数，请重新输入！");
                        return;
                    }

                    if (curSltParam.Resourece == ResoureceType.None)
                    {
                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            ParamName = curSltParam.Name,
                            Serial = ModelParam.Serial,
                            Name = Serial + "_" + Name + "_" + SltOutputParamName,
                            Type = DataType._object,
                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                            ResourcePath = curSltParam.ResourcePath,
                        });
                    }
                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                    {
                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                        if (inputParam == null)
                        {
                            return;
                        }

                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            Name = SltOutputParamName,
                            Type = DataType._object,
                            Value = inputParam.Value.DeepClone(),
                            ResourcePath = inputParam.ResourcePath,
                            Serial = inputParam.Serial
                        });
                    }
                    break;

                case "Delete":
                    if (CurrentOutputParam == null)
                    {
                        return;
                    }

                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                    CurrentOutputParam = null;
                    break;
            }
        });

        #endregion

        #region Private Methods
        private void RestoreSelectedSensor()
        {
            if (ModelParam == null ||
                ModelParam.SltModel != null ||
                string.IsNullOrWhiteSpace(ModelParam.SltModelName) ||
                ModelParam.Models == null)
            {
                return;
            }

            ModelParam.SltModel = ModelParam.Models.FirstOrDefault(model => model.NickName == ModelParam.SltModelName);
        }

        private void SelectRawDataCsvFilePath(Action<string> assignPath, string currentPath)
        {
            void OpenDialog()
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "请选择原始数据CSV文件",
                    Filter = "CSV文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                string? existingDirectory = GetExistingDirectory(currentPath);
                if (!string.IsNullOrWhiteSpace(existingDirectory))
                {
                    dialog.InitialDirectory = existingDirectory;
                }

                if (dialog.ShowDialog() == true)
                {
                    assignPath(dialog.FileName);
                }
            }

            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                OpenDialog();
                return;
            }

            PrismProvider.Dispatcher.Invoke(OpenDialog);
        }

        private void SelectCalibrationSourceFilePath(Action<string> assignPath, string currentPath)
        {
            void OpenDialog()
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "请选择标定数据文件",
                    Filter = "CSV文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                string? existingDirectory = GetExistingDirectory(currentPath);
                if (!string.IsNullOrWhiteSpace(existingDirectory))
                {
                    dialog.InitialDirectory = existingDirectory;
                }

                if (dialog.ShowDialog() == true)
                {
                    assignPath(dialog.FileName);
                }
            }

            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                OpenDialog();
                return;
            }

            PrismProvider.Dispatcher.Invoke(OpenDialog);
        }

        private void SelectPointCloudOutputFilePath(
            Action<string> assignPath,
            string currentPath,
            string title,
            string defaultFileName)
        {
            void OpenDialog()
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = title,
                    Filter = "PLY文件 (*.ply)|*.ply|所有文件 (*.*)|*.*",
                    DefaultExt = ".ply",
                    AddExtension = true,
                    CheckPathExists = true,
                    OverwritePrompt = true,
                    FileName = !string.IsNullOrWhiteSpace(currentPath)
                        ? Path.GetFileName(currentPath)
                        : defaultFileName
                };

                string? existingDirectory = GetExistingDirectory(currentPath);
                if (!string.IsNullOrWhiteSpace(existingDirectory))
                {
                    dialog.InitialDirectory = existingDirectory;
                }

                if (dialog.ShowDialog() == true)
                {
                    assignPath(dialog.FileName);
                }
            }

            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                OpenDialog();
                return;
            }

            PrismProvider.Dispatcher.Invoke(OpenDialog);
        }

        private void SelectFolderPath(Action<string> assignPath, string currentPath, string description)
        {
            void OpenDialog()
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = description,
                    ShowNewFolderButton = true
                };

                string? existingDirectory = GetExistingDirectory(currentPath);
                if (!string.IsNullOrWhiteSpace(existingDirectory))
                {
                    dialog.SelectedPath = existingDirectory;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    assignPath(dialog.SelectedPath);
                }
            }

            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                OpenDialog();
                return;
            }

            PrismProvider.Dispatcher.Invoke(OpenDialog);
        }

        private static string? GetExistingDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (Directory.Exists(path))
            {
                return path;
            }

            string? directory = Path.GetDirectoryName(path);
            return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
                ? directory
                : null;
        }
        #endregion
    }
}

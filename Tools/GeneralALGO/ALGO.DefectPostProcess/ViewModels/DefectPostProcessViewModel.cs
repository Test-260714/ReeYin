using ALGO.DefectPostProcess.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.DefectPostProcess.ViewModels
{
    /// <summary>
    /// 实现缺陷后处理主界面的视图模型。
    /// </summary>
    [Serializable]
    public class DefectPostProcessViewModel : DialogViewModelBase, IViewModuleParam
    {
        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { SetProperty(ref _sltOutputParamName, value); }
        }

        public new DefectPostProcessModel ModelParam
        {
            get { return base.ModelParam as DefectPostProcessModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { SetProperty(ref _currentOutputParam, value); }
        }

        private bool _isFeatureValuesDialogOpen;

        /// <summary>
        /// 初始化模块参数与界面状态。
        /// </summary>
        public override void InitParam()
        {
            ModelParam = InitModelParam<DefectPostProcessModel>();
            if (Param is IModuleParam moduleParam)
            {
                ModelParam.moduleInputParam = moduleParam.moduleInputParam;
            }

            ModelParam.LoadKeyParam();
            ModelParam.InitializeSchemeManagement();
            ModelParam.ApplySelectedScheme(out _);
            ModelParam.RefreshPreview();
        }

        /// <summary>
        /// 处理弹窗关闭后的状态收尾。
        /// </summary>
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        /// <summary>
        /// 处理输出参数的新增与删除操作。
        /// </summary>
        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((order) =>
        {
            switch (order?.ToString())
            {
                case "Add":
                    AddSelectedOutputParam();
                    break;
                case "Delete":
                    DeleteSelectedOutputParam();
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    _ = ModelParam.ExecuteModule();
                    break;
                case "执行片材尺寸判定":
                    ModelParam.ExecuteSheetSizeJudgePreview();
                    break;
                case "关闭定制算法子页面":
                    ModelParam.CloseCustomAlgorithmSubpage();
                    break;
                case "上一张图":
                    ModelParam.MovePreviewImage(-1);
                    break;
                case "下一张图":
                    ModelParam.MovePreviewImage(1);
                    break;
                case "刷新标定文件":
                    ModelParam.RefreshCalibrationFile();
                    break;
                case "删除标定文件":
                    ModelParam.CalibrationFilePath = string.Empty;
                    ModelParam.RefreshCalibrationFile();
                    break;
                case "导入缺陷类别":
                    ImportDefectClasses();
                    break;
                case "新增缺陷类别":
                    AddManualDefectClass();
                    break;
                case "删除缺陷类别":
                    DeleteCurrentDefectClass();
                    break;
                case "选择标定文件":
                    {
                        try
                        {
                            OpenFileDialog dialog = new OpenFileDialog
                            {
                                Title = "选择标定文件",
                                Filter = "标定文件 (*.json;*.yaml;*.yml)|*.json;*.yaml;*.yml|JSON 文件 (*.json)|*.json|YAML 文件 (*.yaml;*.yml)|*.yaml;*.yml|所有文件 (*.*)|*.*",
                                CheckFileExists = true,
                                Multiselect = false
                            };

                            if (dialog.ShowDialog() == true)
                            {
                                ModelParam.CalibrationFilePath = dialog.FileName;
                                ModelParam.RefreshCalibrationFile();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"选择标定文件失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;

                case "确认":
                    {
                        if (!ModelParam.SaveCurrentScheme(out string confirmSaveMessage))
                        {
                            MessageBox.Show(confirmSaveMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                        }

                        ModelParam.LoadKeyParam();

                        foreach (var item in ModelParam.OutputParams.Where(item => item.IsGlobal &&
                                 !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
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
                case "方案应用":
                    {
                        if (ModelParam.IsSchemeDirty && ModelParam.SelectedScheme != null)
                        {
                            MessageBoxResult applyResult = MessageBox.Show(
                                "当前规则存在未保存修改，继续应用选中方案会覆盖这些修改，是否继续？",
                                "方案应用确认",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (applyResult != MessageBoxResult.Yes)
                            {
                                break;
                            }
                        }

                        if (!ModelParam.ApplySelectedScheme(out string applyMessage))
                        {
                            MessageBox.Show(applyMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    break;
                case "方案新建":
                    ModelParam.PrepareNewScheme();
                    break;
                case "方案保存":
                    if (!ModelParam.SaveCurrentScheme(out string saveMessage))
                    {
                        MessageBox.Show(saveMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "方案导出":
                    {
                        if (!ModelParam.TryCreateExportScheme(out DefectPostProcessScheme exportScheme, out string exportMessage))
                        {
                            MessageBox.Show(exportMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                        }

                        string defaultFileName = string.IsNullOrWhiteSpace(exportScheme.Name)
                            ? "DefectPostProcessScheme"
                            : exportScheme.Name;

                        foreach (char invalidChar in Path.GetInvalidFileNameChars())
                        {
                            defaultFileName = defaultFileName.Replace(invalidChar, '_');
                        }

                        SaveFileDialog saveFileDialog = new SaveFileDialog
                        {
                            Title = "导出方案文件",
                            Filter = "方案文件 (*.rysl)|*.rysl|所有文件 (*.*)|*.*",
                            DefaultExt = "rysl",
                            AddExtension = true,
                            FileName = defaultFileName,
                            InitialDirectory = ModelParam.SchemeStorageDirectory
                        };

                        if (saveFileDialog.ShowDialog() != true)
                        {
                            break;
                        }

                        try
                        {
                            string json = JsonConvert.SerializeObject(exportScheme, Formatting.Indented);
                            File.WriteAllText(saveFileDialog.FileName, json);
                            MessageBox.Show("方案已导出为 .rysl 文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"导出方案失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;
                case "方案删除":
                    {
                        if (ModelParam.SelectedScheme == null)
                        {
                            MessageBox.Show("请先选择要删除的方案。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                        }

                        MessageBoxResult deleteResult = MessageBox.Show(
                            $"确定删除方案“{ModelParam.SelectedScheme.Name}”吗？",
                            "方案删除确认",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (deleteResult != MessageBoxResult.Yes)
                        {
                            break;
                        }

                        if (!ModelParam.DeleteSelectedScheme(out string deleteMessage))
                        {
                            MessageBox.Show(deleteMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<string> OpenCustomAlgorithmCommand => new DelegateCommand<string>((algorithmKey) =>
        {
            ModelParam.OpenCustomAlgorithmSubpage(algorithmKey);
        });

        public DelegateCommand ShowFeatureValuesCommand => new DelegateCommand(() =>
        {
            if (!TryBeginFeatureValuesDialogOpen())
            {
                return;
            }

            try
            {
                PrismProvider.DialogService.Show(
                    nameof(ALGO.DefectPostProcess.Views.DefectFeatureValuesView),
                    new DialogParameters
                    {
                        { "Title", "实例特征" },
                        { "Param", ModelParam.CreateFeatureValueDialogData() }
                    },
                    result => EndFeatureValuesDialogOpen(),
                    nameof(SingleInstanceDialogWindowView));
            }
            catch
            {
                EndFeatureValuesDialogOpen();
                throw;
            }
        });

        private bool TryBeginFeatureValuesDialogOpen()
        {
            if (_isFeatureValuesDialogOpen)
            {
                return false;
            }

            _isFeatureValuesDialogOpen = true;
            return true;
        }

        private void EndFeatureValuesDialogOpen()
        {
            _isFeatureValuesDialogOpen = false;
        }

        /// <summary>
        /// 从模型配置 JSON 中导入当前方案维护的缺陷类别表。
        /// </summary>
        private void ImportDefectClasses()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "导入缺陷类别",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (!ModelParam.ImportDefectClassDefinitionsFromFile(dialog.FileName, out string message))
                {
                    MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入缺陷类别失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根据界面输入手动新增或更新当前方案的缺陷类别。
        /// </summary>
        private void AddManualDefectClass()
        {
            if (!ModelParam.AddManualDefectClass(out string message))
            {
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 删除当前方案内选中的缺陷类别，保留规则配置以便后续恢复。
        /// </summary>
        private void DeleteCurrentDefectClass()
        {
            if (!ModelParam.DeleteCurrentDefectClass(out string message))
            {
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (Visibility == Visibility.Hidden)
            {
                ModelParam.LoadKeyParam();

                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 将选中的输出参数定义加入模块输出列表。
        /// </summary>
        private void AddSelectedOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName)
                || !ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                MessageBox.Show("已包含重名参数，请重新输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelParam.OutputParamResource[SltOutputParamName] is not TransmitParam selectedParam)
            {
                return;
            }

            if (selectedParam.Resourece == ResoureceType.None)
            {
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    ParamName = selectedParam.Name,
                    Serial = ModelParam.Serial,
                    ParentNode = Name,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[selectedParam.Name],
                    Describe = selectedParam.Describe,
                    ResourcePath = selectedParam.ResourcePath
                });

                return;
            }

            if (selectedParam.Resourece == ResoureceType.Inupt)
            {
                TransmitParam inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == selectedParam.Name);
                if (inputParam == null)
                {
                    return;
                }

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = SltOutputParamName,
                    ParentNode = Name,
                    Type = DataType._object,
                    Value = inputParam.Value,
                    Describe = inputParam.Describe,
                    ResourcePath = inputParam.ResourcePath,
                    Serial = inputParam.Serial
                });
            }
        }

        /// <summary>
        /// 从模块输出列表中删除当前选中的输出参数映射。
        /// </summary>
        private void DeleteSelectedOutputParam()
        {
            if (CurrentOutputParam == null)
            {
                return;
            }

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }
    }
}

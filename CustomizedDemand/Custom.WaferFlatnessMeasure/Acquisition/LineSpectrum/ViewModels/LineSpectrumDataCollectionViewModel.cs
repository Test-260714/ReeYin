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
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    [Serializable]
    public class LineSpectrumDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
    {
        private string _sltOutputParamName = string.Empty;
        private TransmitParam? _currentOutputParam;

        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value);
        }

        public new LineSpectrumDataCollectionModel ModelParam
        {
            get => (base.ModelParam as LineSpectrumDataCollectionModel)!;
            set => base.ModelParam = value;
        }

        public TransmitParam? CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<LineSpectrumDataCollectionModel>();
            ModelParam.LoadKeyParam();
        }

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public DelegateCommand SelectTiffOutputDirectoryCommand => new DelegateCommand(SelectTiffOutputDirectory);

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "SwitchModule":
                case "切换模块":
                    if (ModelParam?.SltModel != null)
                    {
                        ModelParam.SltModelName = ModelParam.SltModel.NickName;
                    }
                    else
                    {
                        MessageBox.Show("选中的线光谱传感器模块无效。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "执行":
                    if (ModelParam != null)
                    {
                        _ = ModelParam.ExecuteModule();
                    }
                    break;
                case "Cancel":
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "Confirm":
                case "确认":
                    if (ModelParam == null)
                    {
                        break;
                    }

                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
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
            {
                ModelParam.Serial = Serial;
            }

            RestoreSelectedSensor();
            ModelParam.LoadKeyParam();
            SltOutputParamName = ModelParam.OutputParamNames?.FirstOrDefault() ?? string.Empty;

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>(obj =>
        {
            if (ModelParam == null)
            {
                return;
            }

            switch (obj?.ToString())
            {
                case "Add":
                    AddOutputParam();
                    break;
                case "Delete":
                    DeleteOutputParam();
                    break;
            }
        });

        private void AddOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? resourceObject) ||
                resourceObject is not TransmitParam curSltParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
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
                TransmitParam? inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
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
        }

        private void DeleteOutputParam()
        {
            if (CurrentOutputParam == null)
            {
                return;
            }

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }

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

        private void SelectTiffOutputDirectory()
        {
            if (ModelParam == null)
            {
                return;
            }

            var dialog = new OpenFolderDialog
            {
                Title = "选择线光谱 TIFF 导出文件夹",
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(ModelParam.TiffOutputDirectory))
            {
                dialog.InitialDirectory = ModelParam.TiffOutputDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                ModelParam.TiffOutputDirectory = dialog.FolderName;
            }
        }
    }
}

using ALGO.NinePointCalibration.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;
using MessageBox = System.Windows.MessageBox;

namespace ALGO.NinePointCalibration.ViewModels
{
    [Serializable]
    public class NinePointCalibrationViewModel : DialogViewModelBase, IViewModuleParam
    {
        private NinePointCalibrationModel _modelParam = new NinePointCalibrationModel();
        private string _sltOutputParamName = string.Empty;
        private TransmitParam? _currentOutputParam;

        public new NinePointCalibrationModel ModelParam
        {
            get { return _modelParam; }
            set
            {
                _modelParam = value;
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { SetProperty(ref _sltOutputParamName, value); }
        }

        public TransmitParam? CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { SetProperty(ref _currentOutputParam, value); }
        }

        public override void InitParam()
        {
            ModelParam = Param is NinePointCalibrationModel model
                ? model
                : new NinePointCalibrationModel();

            ModelParam.OnceInit();
            ModelParam.InitOutputParamResource(Guid);
            ModelParam.TransferParam();
            ModelParam.RefreshControlCardContext();
        }

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.LoadKeyParam();
            ModelParam.RefreshControlCardContext();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "刷新控制卡":
                    ModelParam.RefreshControlCardContext();
                    break;
                case "生成九点":
                    ModelParam.GenerateNinePointTemplate(true);
                    break;
                case "清空像素":
                    ModelParam.ClearPixelInputs();
                    break;
                case "计算标定":
                    if (!ModelParam.TryCalculateCalibration(out string calibrationMessage))
                    {
                        MessageBox.Show(calibrationMessage, "九点标定", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "验证转换":
                    if (!ModelParam.TryTransformPreviewPixel(out string transformMessage))
                    {
                        MessageBox.Show(transformMessage, "九点标定", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
                case "移动到选中点":
                    MoveToSelectedPoint();
                    break;
                case "移动到转换点":
                    MoveToPreviewPoint();
                    break;
                case "执行":
                    _ = ModelParam.ExecuteModule();
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    ConfirmAndClose();
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>(obj =>
        {
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

        private void MoveToSelectedPoint()
        {
            if (ModelParam.SelectedPoint == null)
            {
                MessageBox.Show("请先选择一个标定点。", "九点标定", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"确定移动到 {ModelParam.SelectedPoint.DisplayName}：X={ModelParam.SelectedPoint.MachineX:F6}, Y={ModelParam.SelectedPoint.MachineY:F6}？",
                "九点标定",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (!ModelParam.TryMoveToSelectedPoint(out string message))
            {
                MessageBox.Show(message, "九点标定", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveToPreviewPoint()
        {
            MessageBoxResult result = MessageBox.Show(
                $"确定移动到转换点：X={ModelParam.PreviewMachineX:F6}, Y={ModelParam.PreviewMachineY:F6}？",
                "九点标定",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (!ModelParam.TryMoveToPreviewMachine(out string message))
            {
                MessageBox.Show(message, "九点标定", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ConfirmAndClose()
        {
            foreach (TransmitParam item in ModelParam.OutputParams.Where(item => item.IsGlobal))
            {
                if (!PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(globalParam => globalParam.Guid == item.Guid))
                {
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
                }
            }

            ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                item => item.Guid.ToString(),
                item => (object)item);

            CloseDialog(ButtonResult.OK, new DialogParameters
            {
                { "Param", ModelParam },
            });
        }

        private void AddOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName))
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                MessageBox.Show("已存在同名输出参数。", "九点标定", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelParam.OutputParamResource[SltOutputParamName] is not TransmitParam selectedParam)
            {
                return;
            }

            ModelParam.OutputParams.Add(new TransmitParam
            {
                LinkGuid = Guid,
                ParamName = selectedParam.Name,
                Serial = ModelParam.Serial,
                Name = SltOutputParamName,
                Type = DataType._object,
                ParentNode = Name,
                Value = OutputParamCollector.GetDataPointValues(ModelParam)[selectedParam.Name],
                ResourcePath = selectedParam.ResourcePath,
                Describe = selectedParam.Describe,
            });
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
    }
}

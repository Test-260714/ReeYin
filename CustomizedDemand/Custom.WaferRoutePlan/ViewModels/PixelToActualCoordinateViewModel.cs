using Custom.WaferRoutePlan.Services;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace Custom.WaferRoutePlan.ViewModels
{
    public class PixelToActualCoordinateViewModel : DialogViewModelBase, IViewModuleParam
    {
        private static readonly ControlCardPositionService _positionService = new();

        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value);
        }

        public new PixelToActualCoordinateModel ModelParam
        {
            get { return base.ModelParam as PixelToActualCoordinateModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<PixelToActualCoordinateModel>();
            ModelParam.LoadKeyParam();
            if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                !ModelParam.OutputParamNames.Contains(SltOutputParamName))
            {
                SltOutputParamName = ModelParam.OutputParamNames.FirstOrDefault();
            }
        }

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    ModelParam.ExecuteModule();
                    break;
                case "标定文件":
                    SelectFile(path => ModelParam.CalibrationFile = path);
                    break;
                case "NPointCalibrationFile":
                    SelectFile(path => ModelParam.NPointCalibrationFile = path);
                    break;
                case "GetCurrentPositionToCameraCenter":
                    FillCalibrationPosition((x, y) =>
                    {
                        ModelParam.CameraCenterPosX = x;
                        ModelParam.CameraCenterPosY = y;
                    });
                    break;
                case "GetCurrentPositionToPointSpectrum":
                    FillCalibrationPosition((x, y) =>
                    {
                        ModelParam.PointSpectrumPosX = x;
                        ModelParam.PointSpectrumPosY = y;
                    });
                    break;
                case "CalculateCalibrationOffset":
                    try
                    {
                        ModelParam.CalculateCalibrationOffset();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case "确认":
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam }
                    });
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam }
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                        !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object resourceObject) ||
                        resourceObject is not TransmitParam curSltParam)
                    {
                        break;
                    }

                    if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
                    {
                        MessageBox.Show("已包含重名参数，请重新输入！");
                        break;
                    }

                    ModelParam.OutputParams.Add(new TransmitParam
                    {
                        LinkGuid = Guid,
                        ParamName = curSltParam.Name,
                        Serial = ModelParam.Serial,
                        Name = SltOutputParamName,
                        ParentNode = Name,
                        Type = DataType._object,
                        Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                        ResourcePath = curSltParam.ResourcePath,
                    });
                    break;

                case "Delete":
                    if (CurrentOutputParam == null)
                    {
                        break;
                    }

                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                    CurrentOutputParam = null;
                    break;
            }
        });

        private static void SelectFile(System.Action<string> setPathAction)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult == true)
            {
                setPathAction(dialog.FileName);
            }
        }

        private static void FillCalibrationPosition(Action<double, double> setPositionAction)
        {
            if (!_positionService.TryGetCurrentXY(out double x, out double y, out string message))
            {
                MessageBox.Show(message);
                return;
            }

            setPositionAction(x, y);
        }
    }
}

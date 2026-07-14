using ALGO.MeasureCircle.Views;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.MeasureCircle.ViewModels
{
    [Serializable]
    public class MeasureCircleViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private MeasureCircleModel _modelParam = new MeasureCircleModel();

        public MeasureCircleModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        public Array MeasModes { get; set; } = Enum.GetValues(typeof(eMeasMode));

        public Array MeasSelects { get; set; } = Enum.GetValues(typeof(eMeasSelect));
        #endregion

        #region Constructor
        public MeasureCircleViewModel()
        {

        }
        #endregion

        #region Override
        public override void OnDialogClosed()
        {
            ModelParam.IsDebug = false;
        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            if (Param != null && (Param is MeasureCircleModel))
                ModelParam = Param as MeasureCircleModel;
            else
                ModelParam = new MeasureCircleModel();

            LoadSpecificConfig(ModelParam);
            // 获取数据点定义
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(MeasureCircleModel));

            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = point.Name,
                    Type = DataType._object,
                    Resourece = ResoureceType.None,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }
            ModelParam.TransferParam();
            ModelParam.IsDebug = true;
        }

        private bool ValidateCalibrationBeforeAction()
        {
            if (ModelParam.ValidateCalibrationConfig(out string errorMessage))
            {
                return true;
            }

            System.Windows.MessageBox.Show(errorMessage);
            return false;
        }

        private void SelectCalibrationFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择标定文件",
                Filter = "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ModelParam.CalibrationFilePath = dialog.FileName;
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
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    if (!ValidateCalibrationBeforeAction())
                        break;

                    ModelParam.ExecuteModule();
                    ModelParam.InitCircleMethod();
                    break;
                case "选择标定文件":
                    SelectCalibrationFile();
                    break;
                case "确认":
                    {
                        if (!ValidateCalibrationBeforeAction())
                            break;

                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(MeasureCircleModel.ModuleName))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(MeasureCircleModel.ModuleName, ModelParam.mWindowH);
                        }
                        else
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[MeasureCircleModel.ModuleName] = ModelParam.mWindowH;
                        }

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;

                default:
                    break;
            }

        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand
        {
            get
            {
                return new DelegateCommand<object>
                (
                    (obj) =>
                    {
                        //var tempModelParam = ModelParam.DeepClone();

                        switch (obj?.ToString())
                        {
                            case "Add":
                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                                }
                                else
                                {
                                    if (curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value,
                                            ResourcePath = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.ResourcePath,
                                            Serial = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name).Serial
                                        });
                                    }
                                }

                                break;
                            case "Delete":
                                if (CurrentOutputParam != null)
                                {
                                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                                    CurrentOutputParam = null;
                                }

                                break;
                            default:
                                break;
                        }
                    }
                );
            }
        }

        #endregion
    }
}

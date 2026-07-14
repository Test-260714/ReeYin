using ALGO.MeasureRect;
using ALGO.MeasureRect.Views;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.MeasureRect.ViewModels
{
    [Serializable]
    public class MeasureRectViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 属性

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new MeasureRectModel ModelParam
        {
            get { return base.ModelParam as MeasureRectModel; }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
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
        #endregion 属性

        #region 生命周期
        public override void OnDialogClosed()
        {
            ModelParam.IsDebug = false;
        }
        #endregion 生命周期

        #region 方法
        public override void InitParam()
        {
            ModelParam = InitModelParam<MeasureRectModel>();

            LoadSpecificConfig(ModelParam);

            // 获取数据点定义
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(MeasureRectModel));

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

        #endregion 方法

        #region 命令

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(async (order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    if (!ValidateCalibrationBeforeAction())
                        break;

                    ModelParam.LoadKeyParam();
                    await ModelParam.ExecuteModule();
                    ModelParam.InitRectMethod();
                    break;
                case "选择标定文件":
                    SelectCalibrationFile();
                    break;
                case "确认":
                    {
                        if (!ValidateCalibrationBeforeAction())
                            break;

                        foreach (var outputParam in ModelParam.OutputParams.Where(item => item.IsGlobal &&
                            !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(outputParam);
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

        #endregion 命令
    }
}

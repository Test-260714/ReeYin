using ALGO.MeasureLine.Views;
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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.MeasureLine.ViewModels
{
    [Serializable]
    public class MeasureLineViewModel : DialogViewModelBase, IViewModuleParam,IDisposable
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

        public new MeasureLineModel ModelParam
        {
            get { return base.ModelParam as MeasureLineModel; }
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
        #endregion

        #region Constructor
        public MeasureLineViewModel()
        {

        }

        ~MeasureLineViewModel()
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

        public override void LoadSpecificConfig(ModelParamBase modelParam)
        {
            if (modelParam == null)
            {
                return;
            }

            modelParam.OutputParamResource.Clear();
            if (Serial >= 0)
            {
                modelParam.Serial = Serial;
            }
            else if (modelParam.Serial == -999)
            {
                modelParam.Serial = Serial;
            }

            if (modelParam is not MeasureLineModel)
            {
                base.LoadSpecificConfig(modelParam);
            }
        }

        public override void InitParam()
        {
            IModuleParam upstreamModuleParam = Param as IModuleParam;
            bool shouldCopyUpstreamInputs = upstreamModuleParam != null && Param is not MeasureLineModel;

            ModelParam = InitModelParam<MeasureLineModel>();
            if (shouldCopyUpstreamInputs)
            {
                ModelParam.moduleInputParam = upstreamModuleParam.moduleInputParam;
            }

            LoadSpecificConfig(ModelParam);

            // 获取数据点定义
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(MeasureLineModel));
            var validOutputNames = new HashSet<string>(dataPoints.Select(point => point.Name));
            ModelParam.OutputParamResource.Clear();
            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = point.Name,
                    // 按成员真实类型派生：坐标点(double)输出为 Double，其余对象(如 OutLine)保持 _object
                    Type = ResolveOutputDataType(point.MemberType),
                    Resourece = ResoureceType.None,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }

            CleanupLegacyOutputParams(validOutputNames);
            ModelParam.TransferParam();
            ModelParam.IsDebug = true;
        }

        private void CleanupLegacyOutputParams(HashSet<string> validOutputNames)
        {
            var invalidOutputParams = ModelParam.OutputParams
                .Where(item => !string.IsNullOrWhiteSpace(item.ParamName) && !validOutputNames.Contains(item.ParamName))
                .ToList();

            foreach (var invalidOutputParam in invalidOutputParams)
            {
                ModelParam.OutputParams.Remove(invalidOutputParam);
                PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams?.Remove(invalidOutputParam);
            }

            if (CurrentOutputParam != null && invalidOutputParams.Contains(CurrentOutputParam))
            {
                CurrentOutputParam = null;
            }
        }

        /// <summary>
        /// 根据输出成员的真实类型推断对应的 DataType。
        /// 坐标点等基础数值类型输出为 Double，其余复杂对象保持 _object。
        /// </summary>
        private static DataType ResolveOutputDataType(Type memberType)
        {
            // 可空类型取其基础类型，保证 double? 仍被识别为 Double
            memberType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (memberType == typeof(double) || memberType == typeof(float) || memberType == typeof(decimal))
                return DataType.Double;

            if (memberType == typeof(int) || memberType == typeof(long) || memberType == typeof(short))
                return DataType.Int;

            if (memberType == typeof(bool))
                return DataType.Bool;

            if (memberType == typeof(string))
                return DataType.String;

            return DataType._object;
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


        public void Dispose()
        {
           
        }

        #endregion

        #region Commands

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

                    await ModelParam.ExecuteModule();
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

                        //PrismProvider.ProjectManager.SltCurSolutionItem.NodeCaches.Add(Serial.ToString(), new Dictionary<string, object>() { { "OutputImage", ModelParam.mWindowH.Image.Clone() } });
                        //ModelParam.mWindowH.Dispose();
                        //(ModelParam.InputImage.Value as HImage).Dispose();

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

                        switch (obj?.ToString())
                        {
                            case "Add":
                                if (string.IsNullOrWhiteSpace(SltOutputParamName))
                                    return;

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                                }
                                else
                                {
                                    if(curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            Name = Serial + "_" + Name + "_"+SltOutputParamName,
                                            // 沿用资源项已派生的类型（坐标点为 Double，对象为 _object）
                                            Type = curSltParam.Type,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if(curSltParam.Resourece == ResoureceType.Inupt)
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
                                if(CurrentOutputParam != null)
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

using ALGO.CreatRegion;
using ALGO.CreatRegion.Views;
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
using ReeYin_V.Core.Extension;
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

namespace ALGO.CreatRegion.ViewModels
{
    [Serializable]
    public class CreatRegionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Proerties

        private string _sltOutputParamName = string.Empty;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new CreatRegionModel ModelParam
        {
            get { return (CreatRegionModel)base.ModelParam; }
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        private TransmitParam? _currentOutputParam;

        public TransmitParam? CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        public Array MeasModes { get; set; } = Enum.GetValues(typeof(eMeasMode));

        public Array MeasSelects { get; set; } = Enum.GetValues(typeof(eMeasSelect));

        public enum BinarizationMode
        {
            [Description("固定")]
            固定,

            [Description("自动亮")]
            自动亮,

            [Description("自动暗")]
            自动暗,

            [Description("局部阈值")]
            局部阈值
        }

        public enum LocalType
        {
            dark,
            light,
            equal,
            not_equal
        }

        public Array BinarizationModes { get; set; } = Enum.GetValues(typeof(BinarizationMode));

        public Array LocalTypes { get; set; } = Enum.GetValues(typeof(LocalType));

        #endregion

        #region Constructor
        public CreatRegionViewModel()
        {

        }
        #endregion

        #region Override
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }

            base.OnDialogClosed();
        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            ModelParam = InitModelParam<CreatRegionModel>();
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
                    ModelParam.ExecuteModule();
                    break;
                case "确认":
                    {
                        if (!ModelParam.LoadKeyParam())
                            return;

                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);


                        if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(CreatRegionModel.ModuleName))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(CreatRegionModel.ModuleName, ModelParam.mWindowH);
                        }
                        else
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[CreatRegionModel.ModuleName] = ModelParam.mWindowH;
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
            ModelParam.LoadKeyParam();

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
                                if (string.IsNullOrWhiteSpace(SltOutputParamName)
                                    || !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? selected)
                                    || selected is not TransmitParam curSltParam)
                                {
                                    return;
                                }

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
                                            ParentNode = Name,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            ParentNode = Name,
                                            Type = DataType._object,
                                            Value = inputParam?.Value,
                                            ResourcePath = inputParam?.ResourcePath,
                                            Serial = inputParam?.Serial ?? ModelParam.Serial
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

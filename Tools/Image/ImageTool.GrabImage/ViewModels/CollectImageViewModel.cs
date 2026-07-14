using HalconDotNet;
using ImageTool.GrabImage.Views;
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
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ImageTool.GrabImage.ViewModels
{
    [Serializable]
    public class CollectImageViewModel : DialogViewModelBase, IViewModuleParam
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

        public new CollectImageModel ModelParam
        {
            get { return base.ModelParam as CollectImageModel; }
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public CollectImageViewModel()
        {

        }

        ~CollectImageViewModel()
        {

        }
        #endregion

        #region Methods
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<CollectImageModel>();
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
                case "ImportConfig":
                    MessageBox.Show("导入配置成功！");

                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    ModelParam.ExecuteModule();

                    break;
                case "确认":
                    {
                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        //选中相机
                        if (ModelParam.SltCamName != null && ModelParam.SelectedCameraModel!= null)
                            ModelParam.SltCamName = ModelParam.SelectedCameraModel.CameraNo;

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                case "ExportConfig":

                    MessageBox.Show("导出配置成功！");

                    break;

                case "打开文件":

                    Task.Run(() =>
                    {
                        OpenFileDialog dialog = new OpenFileDialog();
                        var dialogResult = (bool)dialog.ShowDialog();
                        if (dialogResult)
                        {
                            ModelParam.FilePath = dialog.FileName;
                        }
                    });

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
            if(ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam.LoadKeyParam();
            PrismProvider.SoftwareAlarmReporter?.Report(
code: "SW.MYMODULE.NO_RESULT",
source: "MyVisionModule",
location: $"{Serial:D3}",
message: "检测模块未输出有效结果。",
severity: AlarmSeverity.Warning,
extraData: new Dictionary<string, object?>
{
["Serial"] = Serial,
["ModuleName"] = "MyVisionModule",
["Reason"] = "NoResult"
});
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
                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    MessageBox.Show("已包含重名参数，请重新输入！");
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
                                            ParentNode = Name,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
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
                                            ParentNode = Name,
                                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value.DeepClone(),
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

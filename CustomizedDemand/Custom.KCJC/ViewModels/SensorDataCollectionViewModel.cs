using Custom.KCJC.Models;
using Microsoft.Win32;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.KCJC.ViewModels
{
    [Serializable]
    public partial class SensorDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
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

        public SensorDataCollectionModel ModelParam
        {
            get { return base.ModelParam as SensorDataCollectionModel; }
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
        public SensorDataCollectionViewModel()
        {

        }

        ~SensorDataCollectionViewModel()
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

        public async void Init()
        {
            // 获取数据点定义
            ModelParam.OutputParamResource.Clear();
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(SensorDataCollectionModel));
            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    Name = point.Name,
                    Type = DataType._object,
                    Resourece = ResoureceType.None,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }

            #region 获取设置相机的基本参数

            #endregion

        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<SensorDataCollectionModel>();
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
                case "打开标定页面":
                    PrismProvider.DialogService.Show("CalibrationView", new DialogParameters
                        {
                            { "Title", "标定页面" },
                            { "Serial", Serial },
                            { "Icon", "\ue673" },
                        }, result =>
                        {

                        }, nameof(DialogWindowView));
                    break;
                case "切换模块":
                    {
                        if (ModelParam.SltModel != null)
                        {
                            ModelParam.SltModelName = ModelParam.SltModel.NickName;
                        }
                        else
                        {
                            MessageBox.Show("选中的模块名称无效或为空！");
                        }
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        ModelParam.ExecuteModule();
                    });

                    break;
                case "确认":
                    {
                        ModelParam.SyncMeasureParamByLimit();
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

                case "打开文件":
                    {
                        Task.Run(() =>
                        {
                            OpenFileDialog dialog = new OpenFileDialog();
                            var dialogResult = (bool)dialog.ShowDialog();
                            if (dialogResult)
                            {

                                ModelParam.SltFile = dialog.FileName;
                            }
                        });
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
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            //if (!PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Keys.Contains(Serial.ToString("D3")))
            //    PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(Serial.ToString("D3"), ModelParam);
            else
            {
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[Serial.ToString("D3")] = ModelParam;
            }

            if (ModelParam.SltModelName != null && ModelParam.SltModelName != "")
            {
                ModelParam.SltModel = ModelParam.Models.Where(c => c.NickName == ModelParam.SltModelName).FirstOrDefault();
                Logs.LogInfo($"选中的数据源是{ModelParam.SltModelName}");
            }

            ModelParam.LoadKeyParam();
            ModelParam.SyncMeasureParamByLimit();

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
                                    if (curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            Name = SltOutputParamName,
                                            ParentNode = Name,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
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

using Custom.KBTScraper.Models;
using Microsoft.Win32;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.KBTScraper.ViewModels
{
    [Serializable]
    public class SensorDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
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

        private SensorDataCollectionModel _modelParam = new SensorDataCollectionModel();

        public SensorDataCollectionModel ModelParam
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
        public async void Init()
        {
            ModelParam.OutputParamResource.Clear();
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(SensorDataCollectionModel));
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
        }

        public override void InitParam()
        {
            if (Param != null && (Param is SensorDataCollectionModel))
                ModelParam = Param as SensorDataCollectionModel;
            else
                ModelParam = new SensorDataCollectionModel();

            Init();

            ModelParam.TransferParam();

        }

        #endregion

        #region Commands
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

                case "选择文件":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            OpenFileDialog dialog = new OpenFileDialog();
                            dialog.Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|所有文件|*.*";
                            dialog.Title = "选择图像文件";
                            var dialogResult = dialog.ShowDialog();
                            if (dialogResult == true)
                            {
                                ModelParam.SltFile = dialog.FileName;
                            }
                        });
                    }
                    break;
                case "连接路径":
                    {
                        if (string.IsNullOrEmpty(ModelParam.LinkPath))
                        {
                            MessageBox.Show("请先输入链接路径！");
                            return;
                        }
                        // TODO: 实现路径连接逻辑
                        MessageBox.Show($"连接路径: {ModelParam.LinkPath}");
                    }
                    break;
                case "断开路径":
                    {
                        // TODO: 实现路径断开逻辑
                        MessageBox.Show("路径已断开");
                        ModelParam.LinkPath = string.Empty;
                    }
                    break;
                default:
                    break;
            }

        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            if (ModelParam.SltModelName != null && ModelParam.SltModelName != "")
            {
                ModelParam.SltModel = ModelParam.Models.Where(c => c.NickName == ModelParam.SltModelName).FirstOrDefault();
            }

            ModelParam.LoadKeyParam();

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
                                            LinkGuid = Guid,
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
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
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

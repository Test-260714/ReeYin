using Custom.EVEMFDJC.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Custom.EVEMFDJC.ViewModels
{
    [Serializable]
    public class EveSensorDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
    {

        #region Proerties

        private string _sltOutputParamName;
        /// <summary>
        /// 数据输出的类型
        /// </summary>
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        //private EveSensorDataCollectionModel _modelParam;

        public EveSensorDataCollectionModel ModelParam
        {
            get { return base.ModelParam as EveSensorDataCollectionModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;
        /// <summary>
        /// 输出信息选中项
        /// </summary>
        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public EveSensorDataCollectionViewModel()
        {

        }

        ~EveSensorDataCollectionViewModel()
        {

        }
        #endregion
        #region Methods
        public override void InitParam()
        {
            InitModelParam<EveSensorDataCollectionModel>();
        }

        #endregion


        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            if (ModelParam.SltModelName != null && ModelParam.SltModelName != "")
            {
                ModelParam.SltModel = ModelParam.Models.Where(c => c.NickName == ModelParam.SltModelName).FirstOrDefault();
            }

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
                    ModelParam.ExecuteModule();

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

                    //MessageBox.Show("导出配置成功！");

                    break;

                case "获取路径":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new FolderBrowserDialog())
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
                                ModelParam.FilePath = dialog.FileName;
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
                case "加载模型":
                    ModelParam.LoadAlgorithm();
                    break;
                case "创建halcon模板":
                    
                    int status =  ModelParam.CreateHalconTemplate();
                    if(status != 0)
                    {
                        MessageView.Ins.MessageBoxShow("创建halcon模板失败", eMsgType.Error);
                    }
                    break;
                default:
                    break;
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
                                if(SltOutputParamName == null)
                                {
                                    MessageView.Ins.MessageBoxShow("请先在下拉框中选择要输出的数据，再添加。", eMsgType.Info);
                                    return;
                                }

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    MessageView.Ins.MessageBoxShow("已包含重名参数，请重新输入！");
                                }
                                else
                                {
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
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
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

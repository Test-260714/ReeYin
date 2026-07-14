using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.DeepLearning.ViewModels
{
    [Serializable]
    public class DeepLearningViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 字段
        #endregion

        #region 属性
        
        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }


        public new DeepLearningModel ModelParam
        {
            get { return base.ModelParam as DeepLearningModel; }
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }


        public Array DeviceTypes { get; set; } = Enum.GetValues(typeof(eDeepLearningDeviceType));

        public Array ModelTypes { get; set; } = Enum.GetValues(typeof(eDeepLearningModelType));
        #endregion

        #region 对话框生命周期
        public override void OnDialogClosed()
        {
            ModelParam.IsDebug = false;
        }
        #endregion

        #region 初始化方法
        public override void InitParam()
        {
            ModelParam = InitModelParam<DeepLearningModel>();
            if (Param is IModuleParam moduleParam)
            {
                ModelParam.moduleInputParam = moduleParam.moduleInputParam;
            }
        }

        #endregion

        #region 命令

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
                case "预览图片":
                    {
                        PrismProvider.DialogService.Show("HalconImageView", new DialogParameters
                        {
                            { "width", 500 },
                            { "hight", 500 },

                        }, result =>
                        {


                        }, nameof(SingleInstanceDialogWindowView));
                    
                    }break;
                case "执行":
                    if (ModelParam.TryLoadKeyParamForUi())
                    {
                        _ = ModelParam.ExecuteModule();
                    }
                    break;
                case "上一张图":
                    ModelParam?.MovePreviewImage(-1);
                    break;
                case "下一张图":
                    ModelParam?.MovePreviewImage(1);
                    break;
                case "确认":
                    {
                        if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(DeepLearningModel.ModuleName))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(DeepLearningModel.ModuleName, ModelParam.mWindowH);
                        }
                        else
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[DeepLearningModel.ModuleName] = ModelParam.mWindowH;
                        }

                        ModelParam.TryLoadKeyParamForUi();

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });

                    }
                    break;

                case "选择模型文件":
                    {
                        try
                        {
                            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog 
                            {
                                Title = "选择模型文件",
                                Filter = "ONNX 模型文件 (*.onnx,*.kmodel)|*.onnx;*.kmodel|ONNX 模型 (*.onnx)|*.onnx|KModel 文件 (*.kmodel)|*.kmodel|所有文件 (*.*)|*.*",
                                DefaultExt = ".onnx",
                                CheckFileExists = true,
                                Multiselect = false
                            };

                            var dialogResult = (bool)dialog.ShowDialog();

                            if (dialogResult)
                            {
                                ModelParam.ModelFilePath = dialog.FileName;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"选择模型文件失败: {ex.Message}");
                        }
                    }
                    break;

                case "加载模型":
                    {
                        try
                        {
                            if (ModelParam.ModelConfig == null || string.IsNullOrEmpty(ModelParam.ModelConfig.ModelPath))
                            {
                                System.Windows.MessageBox.Show("请先选择模型文件。");
                            }
                            if (!ModelParam.TryLoadModelForUi(forceReload: true))
                            {
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"模型文件加载失败: {ex.Message}");
                        }
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

            ModelParam.TryLoadKeyParamForUi();

            // 隐藏窗口只表示参数加载，直接返回模型参数。
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
                                if(string.IsNullOrEmpty(SltOutputParamName)) 
                                    return;
                                
                                if (!ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
                                    return;

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;
                                if (curSltParam == null)
                                    return;

                                string outputDisplayName = GetOutputDisplayName(curSltParam);
                                if (ModelParam.OutputParams.Any(item =>
                                        string.Equals(item.ParamName, curSltParam.Name, StringComparison.Ordinal)
                                        || string.Equals(item.Name, outputDisplayName, StringComparison.Ordinal)))
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
                                            Name = outputDisplayName,
                                            Type = GetOutputDataType(curSltParam.Name),
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if(curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = outputDisplayName,
                                            Type = DataType._object,
                                            ParentNode = Name,
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

        private static DataType GetOutputDataType(string outputName)
        {
            return outputName == nameof(DeepLearningModel.SourceImages)
                ? DataType.HObject
                : DataType._object;
        }

        private string GetOutputDisplayName(TransmitParam outputParam)
        {
            if (outputParam == null)
            {
                return SltOutputParamName;
            }

            if (string.Equals(outputParam.Name, nameof(DeepLearningModel.SourceImages), StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(ModelParam?.InputImageName)
                    ? SltOutputParamName
                    : ModelParam.InputImageName;
            }

            return SltOutputParamName;
        }

        #endregion
    }
}

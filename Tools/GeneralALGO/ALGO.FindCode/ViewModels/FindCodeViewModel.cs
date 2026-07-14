using ALGO.FindCode;
using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.FindCode.ViewModels
{
    [Serializable]
    public class FindCodeViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 字段与状态

        private ObservableCollection<ParamDefinition> _codeParamDefinitions_1D = new();
        /// <summary>
        /// 参数页当前显示的一维码参数集合，确认或执行时同步回 Model。
        /// </summary>
        public ObservableCollection<ParamDefinition> CodeParamDefinitions_1D
        {
            get { return _codeParamDefinitions_1D; }
            set { SetProperty(ref _codeParamDefinitions_1D, value); }
        }

        private ObservableCollection<ParamDefinition> _codeParamDefinitions_2D = new();
        /// <summary>
        /// 参数页当前显示的二维码参数集合，确认或执行时同步回 Model。
        /// </summary>
        public ObservableCollection<ParamDefinition> CodeParamDefinitions_2D
        {
            get { return _codeParamDefinitions_2D; }
            set { SetProperty(ref _codeParamDefinitions_2D, value); }
        }

        #endregion

        #region 界面绑定属性

        private string _sltOutputParamName = string.Empty;

        /// <summary>
        /// 输出参数下拉框中当前选中的输出名称。
        /// </summary>
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前节点参数模型，承载扫码配置、输入链接和输出参数。
        /// </summary>
        public new FindCodeModel ModelParam
        {
            get { return base.ModelParam as FindCodeModel ?? null!; }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        private TransmitParam? _currentOutputParam;

        /// <summary>
        /// 输出参数表格当前选中的行。
        /// </summary>
        public TransmitParam? CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 界面可选的扫码类型列表。
        /// </summary>
        public enum CodeType
        {
            [Description("auto")]
            Auto_1D,

            [Description("Code 128")]
            Code_128,

            [Description("2/5 Interleaved")]
            Interleaved_25,

            [Description("Code 39")]
            Code_39,

            [Description("auto")]
            Auto_2D,

            [Description("Aztec Code")]
            Aztec_Code,

            [Description("Data Matrix ECC 200")]
            Data_Matrix_ECC_200_2D,

            [Description("GS1 Aztec Code")]
            GS1_Aztec_Code,

            [Description("GS1 DataMatrix")]
            GS1_DataMatrix,

            [Description("GS1 QR_Code")]
            GS1_QR_Code,

            [Description("Micro QR Code")]
            Micro_QR_Code,

            [Description("PDF417")]
            PDF417,

            [Description("QR Code")]
            QR_Code
        }

        /// <summary>
        /// 绑定到扫码类型下拉框的枚举值集合。
        /// </summary>
        public Array CodeTypes { get; set; } = Enum.GetValues(typeof(CodeType));

        #endregion

        #region 初始化
        /// <summary>
        /// 通过框架生命周期初始化 Model，并把扫码参数集合绑定到界面。
        /// </summary>
        public override void InitParam()
        {
            ModelParam = InitModelParam<FindCodeModel>();

            if (ModelParam.CodeParamDefinitions_1D == null || ModelParam.CodeParamDefinitions_1D.Count == 0)
            {
                ModelParam.InitializeParamDefinitions();
            }

            CodeParamDefinitions_1D = ModelParam.CodeParamDefinitions_1D ?? new ObservableCollection<ParamDefinition>();
            CodeParamDefinitions_2D = ModelParam.CodeParamDefinitions_2D ?? new ObservableCollection<ParamDefinition>();
        }
        #endregion

        #region 命令入口

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
                    ModelParam.CodeParamDefinitions_1D = CodeParamDefinitions_1D;
                    ModelParam.CodeParamDefinitions_2D = CodeParamDefinitions_2D;
                    _ = ModelParam.ExecuteModule();
                    break;
                case "上一张图":
                    ModelParam?.MovePreviewImage(-1);
                    break;
                case "下一张图":
                    ModelParam?.MovePreviewImage(1);
                    break;
                case "确认":
                    {
                        ModelParam.LoadKeyParam();
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

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
            // 隐藏模式直接关闭
            if (Visibility == Visibility.Hidden)
            {
                ModelParam.LoadKeyParam();
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
                                if (string.IsNullOrEmpty(SltOutputParamName) ||
                                    ModelParam.OutputParamResource == null ||
                                    !ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
                                    return;

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;
                                if (curSltParam == null) return;

                                if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
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
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                                        if (inputParam == null)
                                            return;

                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = inputParam.Value,
                                            ResourcePath = inputParam.ResourcePath,
                                            Serial = inputParam.Serial
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

    public enum ParamUIType
    {
        Number,
        Text,
        ComboBox
    }

    public enum ParamValueType
    {
        Int,
        Double,
        String,
        StringInt,
        StringDouble
    }

    public class ParamDefinition : BindableBase
    {
        /// <summary>
        /// 界面显示的参数名称，同时用于映射 HALCON 参数名。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数在界面上的输入控件类型。
        /// </summary>
        public ParamUIType UIType { get; set; }

        /// <summary>
        /// 数值参数允许的最小值。
        /// </summary>
        public double MinValue { get; set; } = 0;

        /// <summary>
        /// 数值参数允许的最大值。
        /// </summary>
        public double MaxValue { get; set; } = 99999999;

        /// <summary>
        /// 数值控件每次递增或递减的步长。
        /// </summary>
        public double SmallChange { get; set; } = 1;

        /// <summary>
        /// 转换为 HALCON HTuple 时使用的值类型。
        /// </summary>
        public ParamValueType ValueType { get; set; } = ParamValueType.Double;

        private object _value = 0d;
        /// <summary>
        /// 参数当前值，执行前按 ValueType 转换为 HALCON 参数。
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                SetProperty(ref _value, value);
            }
        }

        /// <summary>
        /// 下拉参数的可选值列表。
        /// </summary>
        public List<string> Options { get; set; } = new();
    }
}

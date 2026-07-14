using ALGO.RegionProcessing;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DataType = ReeYin_V.Core.Services.Project.DataType;
using static ALGO.RegionProcessing.ViewModels.RegionProcessingViewModel;


namespace ALGO.RegionProcessing.ViewModels
{
    public class RegionProcessingViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 字段

        private ObservableCollection<ParamDefinition> _processingParamDefinitions;
        public ObservableCollection<ParamDefinition> ProcessingParamDefinitions
        {
            get { return _processingParamDefinitions; }
            set { SetProperty(ref _processingParamDefinitions, value); RaisePropertyChanged(); }
        }

        #endregion

        #region 属性

        private ObservableCollection<MenuItemModel> _dynamicMenuItems;
        public ObservableCollection<MenuItemModel> DynamicMenuItems
        {
            get { return _dynamicMenuItems; }
            set { SetProperty(ref _dynamicMenuItems, value); }
        }

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new RegionProcessingModel ModelParam
        {
            get { return base.ModelParam as RegionProcessingModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        public enum ProcessingMode
        {
            [Description("阈值创建区域|0|1")]
            CreateThreshold,

            [Description("按特征筛选|1|1")]
            FilterShape,

            [Description("形状转换|1|1")]
            ShapeTrans,

            [Description("最小形状|1|1")]
            SmallestShape,

            [Description("腐蚀|1|1")]
            Erosion,

            [Description("膨胀|1|1")]
            Dilation,

            [Description("开操作|1|1")]
            Opening,

            [Description("闭操作|1|1")]
            Closing,

            [Description("联合1|1|1")]
            Union1,

            [Description("联合2|2|1")]
            Union2,

            [Description("拼接|2|1")]
            Concat,

            [Description("对称|1|1")]
            Mirror,

            [Description("平移|1|1")]
            Move,

            [Description("放缩|1|1")]
            Zoom,

            [Description("补集|1|1")]
            Complement,

            [Description("差集|2|1")]
            Difference,

            [Description("交集|2|1")]
            Intersection,

            [Description("矩形屏蔽|1|1")]
            RectangleMask,

            [Description("矩形等分|1|1")]
            SplitRectangle
        }

        public Array ProcessingModes { get; set; } = Enum.GetValues(typeof(ProcessingMode));

        public ObservableCollection<ProcessingMode> AvailableModes { get; } = new ObservableCollection<ProcessingMode>(Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>());

        #endregion

        #region 构造函数
        public RegionProcessingViewModel()
        {
            InitializeMenuItems();
        }

        #endregion

        #region 方法

        private void InitializeMenuItems()
        {
            // 直接使用 AvailableModes 生成菜单项
            DynamicMenuItems = new ObservableCollection<MenuItemModel>(
                AvailableModes.Select(mode => new MenuItemModel
                {
                    Header = mode.GetInfo().Description,
                    Command = GeneralCommand,
                    CommandParameter = $"add_{mode}"
                })
            );
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<RegionProcessingModel>();
            if (Param is IModuleParam moduleParam)
            {
                ModelParam.moduleInputParam = moduleParam.moduleInputParam;
            }

            RemoveUnavailableOutputParams();
            ModelParam.InitializeParamDefinitions(ModelParam.ProcessingMode);
            ModelParam.RestoreListStepParams();
            ModelParam.InitializeListParamDefinitions();
            if (ModelParam.ProcessingSteps != null && ModelParam.ProcessingSteps.Count > 0)
            {
                if (ModelParam.CurrentStep == null || !ModelParam.ProcessingSteps.Contains(ModelParam.CurrentStep))
                {
                    ModelParam.CurrentStep = ModelParam.ProcessingSteps[0];
                }
                else
                {
                    ModelParam.UpdateListParamDefinitions();
                }
            }
            else
            {
                ModelParam.CurrentStep = null;
                ModelParam.UpdateListParamDefinitions();
            }
            ProcessingParamDefinitions = ModelParam.ProcessingParamDefinitions;
            ModelParam.SetRuntimePreviewEnabled(true);

            ModelParam.LoadKeyParam();
        }

        private void RemoveUnavailableOutputParams()
        {
            if (ModelParam?.OutputParams == null || ModelParam.OutputParamResource == null)
            {
                return;
            }

            var validOutputNames = ModelParam.OutputParamResource.Values.OfType<TransmitParam>()
                .Select(item => item.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var outputParam in ModelParam.OutputParams
                .Where(item => item != null
                    && !string.IsNullOrWhiteSpace(item.ParamName)
                    && !validOutputNames.Contains(item.ParamName))
                .ToList())
            {
                ModelParam.OutputParams.Remove(outputParam);
                PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams?.Remove(outputParam);
            }
        }

        public override void OnDialogClosed()
        {
            ModelParam.SetRuntimePreviewEnabled(false);
            base.OnDialogClosed();
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
                case "执行":                   
                    ModelParam.ExecuteModule();

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

                case "remove":
                    {
                        ModelParam.OldListParamPresets = ModelParam.ListParamPresets.DeepClone();

                        if (ModelParam.ProcessingSteps == null || ModelParam.CurrentStep == null)
                            return;
                        int i = ModelParam.ProcessingSteps.IndexOf(ModelParam.CurrentStep) - 1;
                        ModelParam.ProcessingSteps.Remove(ModelParam.CurrentStep);
                        if (i >= 0)
                        {
                            ModelParam.CurrentStep = ModelParam.ProcessingSteps[i];
                        }
                        else
                        {
                            ModelParam.CurrentStep = null;
                        }
                    }
                    ModelParam.InitializeListParamDefinitions();
                    ModelParam.UpdateListParamDefinitions();
                    break;
                case "up":
                    {
                        ModelParam.OldListParamPresets = ModelParam.ListParamPresets.DeepClone();

                        if (ModelParam.ProcessingSteps == null || ModelParam.CurrentStep == null)
                            return;
                        int i = ModelParam.ProcessingSteps.IndexOf(ModelParam.CurrentStep);
                        if (i > 0)
                        {
                            ModelParam.ProcessingSteps.Move(i, i - 1);
                            ModelParam.CurrentStep = ModelParam.ProcessingSteps[i - 1];
                        }              
                    }
                    ModelParam.InitializeListParamDefinitions();
                    ModelParam.UpdateListParamDefinitions();
                    break;

                case "down":
                    {
                        ModelParam.OldListParamPresets = ModelParam.ListParamPresets.DeepClone();

                        if (ModelParam.ProcessingSteps == null || ModelParam.CurrentStep == null)
                            return;
                        int i = ModelParam.ProcessingSteps.IndexOf(ModelParam.CurrentStep);
                        if (i + 1 < ModelParam.ProcessingSteps.Count)
                        {
                            ModelParam.ProcessingSteps.Move(i, i + 1);
                            ModelParam.CurrentStep = ModelParam.ProcessingSteps[i + 1];
                        }
                    }
                    ModelParam.InitializeListParamDefinitions();
                    ModelParam.UpdateListParamDefinitions();
                    break;

                default:
                    if (order?.StartsWith("add_") == true)
                    {
                        ModelParam.OldListParamPresets = ModelParam.ListParamPresets.DeepClone();

                        var modeName = order.Substring(4);
                        if (Enum.TryParse<ProcessingMode>(modeName, out var mode))
                        {
                            var newStep = new ProcessingStep
                            {
                                StepId = Guid.NewGuid(),
                                Index = ModelParam.ProcessingSteps.Count + 1,
                                Name = mode.GetInfo().Description,
                                Mode = mode,
                                IsEnabled = true,
                                Input = mode.GetInfo().Inputs.ToString(),
                                Output = mode.GetInfo().Outputs.ToString()
                            };

                            ModelParam.ProcessingSteps.Add(newStep);

                            ModelParam.InitializeListParamDefinitions();
                            ModelParam.UpdateListParamDefinitions();
                        }
                    }
                    break;
            }

        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            // 不显示说明，只执行加载
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
                        //var tempModelParam = ModelParam.DeepClone();

                        switch (obj?.ToString())
                        {
                            case "Add":
                                if (string.IsNullOrEmpty(SltOutputParamName)
                                    || !ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
                                {
                                    return;
                                }

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;
                                if (curSltParam == null)
                                {
                                    return;
                                }

                                string outputDisplayName = GetOutputDisplayName(curSltParam);
                                if (ModelParam.OutputParams.Any(item =>
                                        string.Equals(item.ParamName, curSltParam.Name, StringComparison.Ordinal)
                                        || string.Equals(item.Name, outputDisplayName, StringComparison.Ordinal)))
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
                                            Name = outputDisplayName,
                                            Type = GetOutputDataType(curSltParam.Name),
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = outputDisplayName,
                                            Type = GetOutputDataType(curSltParam.Name),
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

        private static DataType GetOutputDataType(string outputName)
        {
            return outputName == nameof(RegionProcessingModel.SourceImage)
                || outputName == nameof(RegionProcessingModel.OutRegion)
                || outputName == nameof(RegionProcessingModel.OutRegionImage)
                || outputName == nameof(RegionProcessingModel.OutRegionImages)
                ? DataType.HObject
                : DataType._object;
        }

        private string GetOutputDisplayName(TransmitParam outputParam)
        {
            if (outputParam == null)
            {
                return SltOutputParamName;
            }

            if (string.Equals(outputParam.Name, nameof(RegionProcessingModel.SourceImage), StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(ModelParam?.InputImage?.Name)
                    ? SltOutputParamName
                    : ModelParam.InputImage?.Name ?? SltOutputParamName;
            }

            return SltOutputParamName;
        }

        #endregion
    }

    public enum ParamUIType
    {
        Number,     // 数值输入
        Text,       // 文本输入
        ComboBox    // 下拉框选择
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
        public string Name { get; set; }
        public ParamUIType UIType { get; set; }
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 99999999;
        public double SmallChange { get; set; } = 1;
        public ParamValueType ValueType { get; set; } = ParamValueType.Double;

        private object _value;
        public object Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    if (Name == "圆/矩形腐蚀:" || Name == "圆/矩形膨胀:" || Name == "开操作形状:" || Name == "闭操作形状:")
                    {
                        OnShapeChanged(value?.ToString());
                    }
                    else if (Name == "阈值方式:")
                    {
                        OnThresholdModeChanged(value?.ToString());
                    }
                    else if (Name == "变换类型:")
                    {
                        OnTransTypeChanged(value?.ToString());
                    }
                }

            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public List<string> Options { get; set; }

        private void OnShapeChanged(string shape)
        {
            if (ProcessingParamDefinitionsRef == null) return;

            foreach (var p in ProcessingParamDefinitionsRef)
            {
                if (p.Name == "圆半径:") p.IsVisible = shape == "circle";
                if (p.Name == "矩形长:" || p.Name == "矩形宽:")
                    p.IsVisible = shape == "rectangle1";
            }
        }

        private void OnThresholdModeChanged(string mode)
        {
            if (ProcessingParamDefinitionsRef == null) return;

            foreach (var p in ProcessingParamDefinitionsRef)
            {
                if (p.Name == "固定最小值:" || p.Name == "固定最大值:")
                    p.IsVisible = mode == "固定";

                if (p.Name == "局部Mask高:" || p.Name == "局部Mask宽:" || p.Name == "局部StdFactor:" || p.Name == "局部Abs阈值:" || p.Name == "局部类型:")
                    p.IsVisible = mode == "局部阈值";
            }
        }

        private void OnTransTypeChanged(string mode)
        {
            if (ProcessingParamDefinitionsRef == null) return;

            foreach (var p in ProcessingParamDefinitionsRef)
            {
                if (p.Name == "形状转换参数:")
                    p.IsVisible = mode == "形状转换";

                if (p.Name == "最小形状参数:")
                    p.IsVisible = mode == "最小形状";
            }
        }
        internal ObservableCollection<ParamDefinition> ProcessingParamDefinitionsRef { get; set; }

        /// <summary>
        /// 深拷贝当前对象
        /// </summary>
        public ParamDefinition Clone()
        {
            return new ParamDefinition
            {
                Name = this.Name,
                UIType = this.UIType,
                MinValue = this.MinValue,
                MaxValue = this.MaxValue,
                SmallChange = this.SmallChange,
                ValueType = this.ValueType,
                Value = this.Value, // ⚠️ 如果 Value 是复杂类型需要再深拷贝
                IsVisible = this.IsVisible,
                Options = this.Options != null ? new List<string>(this.Options) : null,
                ProcessingParamDefinitionsRef = null
            };
        }
    }

    public class ProcessingStep : BindableBase
    {
        public Guid StepId { get; set; } = Guid.NewGuid();

        private bool _isEnabled;
        private string _name;
        private ProcessingMode _mode;
        private string _input;
        private string _output;
        private int _index;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ProcessingMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public string Input
        {
            get => _input;
            set => SetProperty(ref _input, value);
        }

        public string Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

    }

    public class MenuItemModel
    {
        public string Header { get; set; }
        public ICommand Command { get; set; }
        public object CommandParameter { get; set; }
    }
}

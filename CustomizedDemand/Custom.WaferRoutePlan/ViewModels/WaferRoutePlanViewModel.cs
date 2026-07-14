using Custom.WaferRoutePlan;
using Custom.WaferRoutePlan.Views;
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

namespace Custom.WaferRoutePlan.ViewModels
{
    [Serializable]
    public class WaferRoutePlanViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        // 所有 ScanType → 参数定义 的映射
        private Dictionary<ScanTypes, ObservableCollection<ParamDefinition>> _scanTypeParamDefinitions;

        // 当前 UI 实际绑定的参数集合
        private ObservableCollection<ParamDefinition> _currentParamDefinitions;
        public ObservableCollection<ParamDefinition> CurrentParamDefinitions
        {
            get => _currentParamDefinitions;
            set
            {
                if (SetProperty(ref _currentParamDefinitions, value) && ModelParam != null)
                {
                    ModelParam.CurrentParamDefinitions = value;
                }
            }
        }

        #endregion

        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private WaferRoutePlanModel _modelParam = new WaferRoutePlanModel();

        public WaferRoutePlanModel ModelParam
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


        private ScanTypes _currentScanType = ScanTypes.旋转线扫;
        public ScanTypes CurrentScanType
        {
            get => _currentScanType;
            set
            {
                if (SetProperty(ref _currentScanType, value))
                {
                    if (ModelParam != null)
                    {
                        ModelParam.CurrentScanType = value;
                    }

                    UpdateCurrentParamDefinitions();
                }
            }
        }

        public enum ScanTypes
        {
            [Description("旋转线扫")]//十字
            旋转线扫,

            [Description("水平线扫")]
            水平线扫,

            [Description("点扫")]
            点扫,

            [Description("相切圆点扫")]
            相切圆点扫,

            [Description("圆环扫描")]
            圆环扫描,

            [Description("拍摄中心自标定")]
            拍摄中心自标定
        }


        public Array ScanTypesArray { get; set; } = Enum.GetValues(typeof(ScanTypes));

        #endregion

        #region Constructor
        public WaferRoutePlanViewModel()
        {
            InitScanTypeParamDefinitions();
            UpdateCurrentParamDefinitions();
            
        }

        #endregion

        #region Methods
        public override void InitParam()
        {
            if (Param is WaferRoutePlanModel waferRoutePlanModel)
            {
                ModelParam = waferRoutePlanModel;
            }
            else
            {
                ModelParam = new WaferRoutePlanModel();
                if (Param is IModuleParam moduleParam)
                {
                    ModelParam.moduleInputParam = moduleParam.moduleInputParam;
                }
            }

            ModelParam.Serial = Serial;
            //ModelParam.OnceInit();
            //ModelParam.IsDebug = true;
            InitializeScanConfiguration();

            // 获取数据点定义
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(WaferRoutePlanModel));

            ModelParam.OutputParamResource.Clear();
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
            ModelParam.LoadKeyParam();
            SyncModelScanConfiguration();
        }

        public void InitScanTypeParamDefinitions()
        {
            _scanTypeParamDefinitions = new Dictionary<ScanTypes, ObservableCollection<ParamDefinition>>
            {
                [ScanTypes.旋转线扫] = new ObservableCollection<ParamDefinition>
                                            {
                                                new ParamDefinition
                                                {
                                                    Name = "边缘缩进-外扩+(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = -99999,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                },
                                                new ParamDefinition
                                                {
                                                    Name = "旋转格数:",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = 0,
                                                    MaxValue = 360,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                }
                                            },

                [ScanTypes.水平线扫] = new ObservableCollection<ParamDefinition>
                                            {
                                                new ParamDefinition
                                                {
                                                    Name = "边缘缩进-外扩+(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = -99999,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                },
                                                new ParamDefinition
                                                {
                                                    Name = "扫描行数:",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = 0,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 20
                                                }
                                            },

                [ScanTypes.点扫] = new ObservableCollection<ParamDefinition>
                                            {
                                                new ParamDefinition
                                                {
                                                    Name = "边缘缩进-外扩+(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = -99999,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                },
                                                new ParamDefinition
                                                {
                                                    Name = "点扫描间隔(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = 0,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 20
                                                }
                                            },

                [ScanTypes.相切圆点扫] = new ObservableCollection<ParamDefinition>
                                            {
                                                new ParamDefinition
                                                {
                                                    Name = "边缘缩进-外扩+(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = -99999,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                },
                                                new ParamDefinition
                                                {
                                                    Name = "相切圆半径(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = 0,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 20
                                                }
                                            },
                [ScanTypes.圆环扫描] = new ObservableCollection<ParamDefinition>
                                            {
                                                new ParamDefinition
                                                {
                                                    Name = "圆环扫描半径(mm):",
                                                    UIType = ParamUIType.Number,
                                                    MinValue = -99999,
                                                    MaxValue = 99999,
                                                    ValueType = ParamValueType.Double,
                                                    Value = 30.0
                                                }
                                            },
                [ScanTypes.拍摄中心自标定] = new ObservableCollection<ParamDefinition> { }
            };
        }

        private void InitializeScanConfiguration()
        {
            InitScanTypeParamDefinitions();

            if (ModelParam.CurrentParamDefinitions != null
                && (ModelParam.CurrentParamDefinitions.Count > 0 || ModelParam.CurrentScanType == ScanTypes.拍摄中心自标定))
            {
                _scanTypeParamDefinitions[ModelParam.CurrentScanType] = CloneParamDefinitions(ModelParam.CurrentParamDefinitions);
            }

            CurrentScanType = ModelParam.CurrentScanType;
            UpdateCurrentParamDefinitions();
        }

        private static ObservableCollection<ParamDefinition> CloneParamDefinitions(IEnumerable<ParamDefinition> definitions)
        {
            if (definitions == null)
            {
                return new ObservableCollection<ParamDefinition>();
            }

            return new ObservableCollection<ParamDefinition>(definitions.Select(definition => new ParamDefinition
            {
                Name = definition.Name,
                UIType = definition.UIType,
                MinValue = definition.MinValue,
                MaxValue = definition.MaxValue,
                SmallChange = definition.SmallChange,
                ValueType = definition.ValueType,
                Value = definition.Value,
                Options = definition.Options == null ? null : new List<string>(definition.Options)
            }));
        }

        private void UpdateCurrentParamDefinitions()
        {
            if (_scanTypeParamDefinitions.TryGetValue(CurrentScanType, out var defs))
            {
                CurrentParamDefinitions = defs;
            }
        }

        private void SyncModelScanConfiguration()
        {
            ModelParam.CurrentScanType = CurrentScanType;
            ModelParam.CurrentParamDefinitions = CurrentParamDefinitions;
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
                    SyncModelScanConfiguration();
                    ModelParam.ExecuteModule();

                    break;
                case "标定文件":
                    Task.Run(() =>
                    {
                        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                        var dialogResult = (bool)dialog.ShowDialog();
                        if (dialogResult)
                        {
                            ModelParam.CalibrationFile = dialog.FileName;
                        }
                    });
                    break;
                case "N点标定文件":
                    Task.Run(() =>
                    {
                        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                        var dialogResult = (bool)dialog.ShowDialog();
                        if (dialogResult)
                        {
                            ModelParam.NPointCalibrationFile = dialog.FileName;
                        }
                    });
                    break;
                case "确认":
                    {
                        SyncModelScanConfiguration();

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
                                            ParentNode = Name,
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
                                            ParentNode = Name,
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

        #endregion
    }

    public enum ParamUIType
    {
        Number,     // 数值输入
        Text,       // 文本输入
        ComboBox,   // 下拉框选择
    }

    public enum ParamValueType
    {
        Int,
        Double,
        String,
        StringInt,
        StringDouble
    }

    [Serializable]
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
                SetProperty(ref _value, value);
            }
        }

        public List<string> Options { get; set; } // ComboBox 可选项
    }
}

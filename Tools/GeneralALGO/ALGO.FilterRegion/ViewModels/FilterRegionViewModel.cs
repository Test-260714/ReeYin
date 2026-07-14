using ALGO.FilterRegion;
using ALGO.FilterRegion.Views;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
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
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.FilterRegion.ViewModels
{
    public class FilterRegionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Field

        private ObservableCollection<DoublePropertyDefinition> _propertyDefinitions;
        public ObservableCollection<DoublePropertyDefinition> PropertyDefinitions
        {
            get { return _propertyDefinitions; }
            set { SetProperty(ref _propertyDefinitions, value); }
        }

        #endregion

        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new FilterRegionModel ModelParam
        {
            get { return base.ModelParam as FilterRegionModel; }
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
        public enum FilterType
        {
            and,
            or
        }

        public Array FilterTypes { get; set; } = Enum.GetValues(typeof(FilterType));

        #endregion

        #region Constructor
        public FilterRegionViewModel()
        {

        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            ModelParam = InitModelParam<FilterRegionModel>();

            // 仅初始化一次：如果为空，再初始化
            if (ModelParam.PropertyDefinitions == null || ModelParam.PropertyDefinitions.Count == 0)
            {
                ModelParam.InitializePropertyDefinitions();
            }

            // 从ModelParam取数据
            PropertyDefinitions = ModelParam.PropertyDefinitions;
        }
        #endregion

        #region Commands

        // 添加一个命令来处理属性值的应用
        public DelegateCommand ApplyPropertiesCommand => new DelegateCommand(() =>
        {
            foreach (var property in PropertyDefinitions.Where(p => p.IsSelected))
            {
                ApplyPropertyToFilter(property);
            }

            // 执行过滤操作
            ModelParam.ExecuteModule();
        });

        /// <summary>
        /// 将属性值应用到实际的过滤器中
        /// </summary>
        private void ApplyPropertyToFilter(DoublePropertyDefinition property)
        {
            switch (property.Name)
            {
                case "面积":
                    ModelParam.PropertyDefinitions[0] = property;
                    break;
                case "中心行坐标":
                    ModelParam.PropertyDefinitions[1] = property;
                    break;
                case "中心列坐标":
                    ModelParam.PropertyDefinitions[2] = property;
                    break;
                case "区域宽":
                    ModelParam.PropertyDefinitions[3] = property;
                    break;
                case "区域高":
                    ModelParam.PropertyDefinitions[4] = property;
                    break;
                case "区域宽高比":
                    ModelParam.PropertyDefinitions[5] = property;
                    break;
                case "左上角点行坐标":
                    ModelParam.PropertyDefinitions[6] = property;
                    break;
                case "左上角点列坐标":
                    ModelParam.PropertyDefinitions[7] = property;
                    break;
                case "右下角点行坐标":
                    ModelParam.PropertyDefinitions[8] = property;
                    break;
                case "右下角点列坐标":
                    ModelParam.PropertyDefinitions[9] = property;
                    break;
                case "圆度":
                    ModelParam.PropertyDefinitions[10] = property;
                    break;
                case "紧凑":
                    ModelParam.PropertyDefinitions[11] = property;
                    break;
                case "周长":
                    ModelParam.PropertyDefinitions[12] = property;
                    break;
                case "凸性":
                    ModelParam.PropertyDefinitions[13] = property;
                    break;
                case "矩形度":
                    ModelParam.PropertyDefinitions[14] = property;
                    break;
                case "等效椭圆长半径":
                    ModelParam.PropertyDefinitions[15] = property;
                    break;
                case "等效椭圆短半径":
                    ModelParam.PropertyDefinitions[16] = property;
                    break;
                case "等效椭圆方向":
                    ModelParam.PropertyDefinitions[17] = property;
                    break;
                case "偏心率":
                    ModelParam.PropertyDefinitions[18] = property;
                    break;
                case "膨松度":
                    ModelParam.PropertyDefinitions[19] = property;
                    break;
                case "结构因子":
                    ModelParam.PropertyDefinitions[20] = property;
                    break;
                case "外切圆半径":
                    ModelParam.PropertyDefinitions[21] = property;
                    break;
                case "内接圆半径":
                    ModelParam.PropertyDefinitions[22] = property;
                    break;
                case "内接矩形高度":
                    ModelParam.PropertyDefinitions[23] = property;
                    break;
                case "内接矩形宽度":
                    ModelParam.PropertyDefinitions[24] = property;
                    break;
                case "边界与中心平均距离":
                    ModelParam.PropertyDefinitions[25] = property;
                    break;
                case "边界与中心距离偏差":
                    ModelParam.PropertyDefinitions[26] = property;
                    break;
                case "多边形边数":
                    ModelParam.PropertyDefinitions[27] = property;
                    break;
                case "联通数":
                    ModelParam.PropertyDefinitions[28] = property;
                    break;
                case "区域内洞数":
                    ModelParam.PropertyDefinitions[29] = property;
                    break;
                case "所有洞面积":
                    ModelParam.PropertyDefinitions[30] = property;
                    break;
                case "最大直径":
                    ModelParam.PropertyDefinitions[31] = property;
                    break;
                case "区域方向":
                    ModelParam.PropertyDefinitions[32] = property;
                    break;
                case "欧拉数":
                    ModelParam.PropertyDefinitions[33] = property;
                    break;
                case "外接矩形方向":
                    ModelParam.PropertyDefinitions[34] = property;
                    break;
            }
        }

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

    public class DoublePropertyDefinition : BindableBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _hname;
        public string HName
        {
            get { return _hname; }
            set { SetProperty(ref _hname, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        private bool _isOut;
        public bool IsOut
        {
            get { return _isOut; }
            set { SetProperty(ref _isOut, value); }
        }

        private double _minValue = 0;
        public double MinValue
        {
            get { return _minValue; }
            set
            {
                SetProperty(ref _minValue, value);
            }
        }

        private double _maxValue = 100;
        public double MaxValue
        {
            get { return _maxValue; }
            set
            {
                SetProperty(ref _maxValue, value);
            }
        }

        private double _smallChange = 1;
        public double SmallChange
        {
            get { return _smallChange; }
            set { SetProperty(ref _smallChange, value); }
        }

        // 构造函数
        public DoublePropertyDefinition(string name, string hname, double minValue, double maxValue, double smallChange, bool isSelected = false, bool isOut = false)
        {
            Name = name;
            HName = hname;
            MinValue = minValue;
            MaxValue = maxValue;
            SmallChange = smallChange;
            IsSelected = isSelected;
            IsOut = isOut;
        }
    }
}

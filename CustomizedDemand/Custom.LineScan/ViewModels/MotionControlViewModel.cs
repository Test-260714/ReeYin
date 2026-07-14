using Custom.LineScan.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;

namespace Custom.LineScan.ViewModels
{
    /// <summary>
    /// 运动控制节点ViewModel
    /// </summary>
    [Serializable]
    public class MotionControlViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Properties
        private MotionControlModel _modelParam;
        public MotionControlModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public MotionControlViewModel()
        {
        }
        #endregion

        #region Methods
        public void Init()
        {
            // 获取数据点定义
            ModelParam.OutputParamResource.Clear();
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(MotionControlModel));
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
        }

        public override void InitParam()
        {
            if (Param != null && (Param is MotionControlModel))
                ModelParam = Param as MotionControlModel;
            else
                ModelParam = new MotionControlModel();

            Init();
            ModelParam.TransferParam();
        }
        #endregion

        #region Commands
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
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
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
        });
        #endregion
    }
}

using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.DistancePP.ViewModels
{
    [Serializable]
    public class DistancePPViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Properties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new DistancePPModel ModelParam
        {
            get { return base.ModelParam as DistancePPModel; }
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
        public DistancePPViewModel()
        {

        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            IModuleParam upstreamModuleParam = Param as IModuleParam;
            bool shouldCopyUpstream = upstreamModuleParam != null && Param is not DistancePPModel;

            ModelParam = InitModelParam<DistancePPModel>();

            if (shouldCopyUpstream &&
                (ModelParam.moduleInputParam?.TransmitParams == null ||
                 ModelParam.moduleInputParam.TransmitParams.Count == 0))
            {
                ModelParam.moduleInputParam = upstreamModuleParam.moduleInputParam;
            }

            ModelParam.LoadKeyParam();
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
                    _ = ModelParam.ExecuteModule();
                    break;
                case "确认":
                    {
                        ModelParam.LoadKeyParam();
                        foreach (TransmitParam item in ModelParam.OutputParams.Where(item => item.IsGlobal &&
                            !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
                        }

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
            ModelParam.LoadKeyParam();
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        [JsonIgnore]
        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrWhiteSpace(SltOutputParamName)
                        || !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object value)
                        || value is not TransmitParam curSltParam)
                    {
                        return;
                    }

                    if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
                    {
                        System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                        return;
                    }

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
                        TransmitParam inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                        if (inputParam == null)
                            return;

                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            LinkGuid = Guid,
                            Name = SltOutputParamName,
                            Type = DataType._object,
                            ParentNode = Name,
                            Value = inputParam.Value,
                            ResourcePath = inputParam.ResourcePath,
                            Serial = inputParam.Serial
                        });
                    }
                    break;

                case "Delete":
                    if (CurrentOutputParam != null)
                    {
                        ModelParam.OutputParams.Remove(CurrentOutputParam);
                        PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams.Remove(CurrentOutputParam);
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

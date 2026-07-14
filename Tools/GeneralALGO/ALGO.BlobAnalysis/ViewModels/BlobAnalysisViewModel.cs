using ALGO.BlobAnalysis.Models;
using ALGO.BlobAnalysis.Views;
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.BlobAnalysis.ViewModels
{
    [Serializable]
    public class BlobAnalysisViewModel : DialogViewModelBase, IViewModuleParam
    {
        private string _sltOutputParamName = string.Empty;
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new BlobAnalysisModel ModelParam
        {
            get { return base.ModelParam as BlobAnalysisModel; }
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

        [JsonIgnore]
        public Array ThresholdModes { get; } = Enum.GetValues(typeof(BlobThresholdMode));

        [JsonIgnore]
        public Array LocalThresholdTypes { get; } = Enum.GetValues(typeof(BlobLocalThresholdType));

        [JsonIgnore]
        public Array FilterModes { get; } = Enum.GetValues(typeof(BlobFilterMode));

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }

            base.OnDialogClosed();
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<BlobAnalysisModel>();
            ModelParam.EnsurePropertyDefinitions();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                case "Run":
                    ModelParam.ExecuteModule();
                    ModelParam.InitAnalysisRegionMethod();
                    break;
                case "确认":
                case "OK":
                    ModelParam.LoadKeyParam();

                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.LoadKeyParam();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
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
                    if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                        !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object selectedParam))
                    {
                        return;
                    }

                    TransmitParam curSltParam = selectedParam as TransmitParam;
                    if (curSltParam == null)
                    {
                        return;
                    }

                    if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
                    {
                        MessageBox.Show("已包含重名参数，请重新输入！");
                        return;
                    }

                    if (curSltParam.Resourece == ResoureceType.None)
                    {
                        Dictionary<string, object> outputValues = OutputParamCollector.GetDataPointValues(ModelParam);
                        outputValues.TryGetValue(curSltParam.Name, out object outputValue);

                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            LinkGuid = Guid,
                            ParamName = curSltParam.Name,
                            Serial = ModelParam.Serial,
                            Name = SltOutputParamName,
                            Type = DataType._object,
                            Value = outputValue,
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
                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value,
                            ResourcePath = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.ResourcePath,
                            Serial = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Serial ?? 0,
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
            }
        });
    }
}

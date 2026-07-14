using Custom.PlcImageCollect.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System.Windows;

namespace Custom.PlcImageCollect.ViewModels
{
    [Serializable]
    public class PlcImageCollectViewModel : DialogViewModelBase, IViewModuleParam
    {
        public new PlcImageCollectModel ModelParam
        {
            get { return (PlcImageCollectModel)base.ModelParam; }
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        private string _sltOutputParamName = string.Empty;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private TransmitParam? _currentOutputParam;

        public TransmitParam? CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<PlcImageCollectModel>();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "打开图像预览":
                    break;
                case "切换模块":
                    if (ModelParam.SltModel != null)
                    {
                        ModelParam.SltModelName = ModelParam.SltModel.NickName;
                    }
                    else
                    {
                        MessageBox.Show("选中的传感器无效或为空！");
                    }
                    break;
                case "开始采集":
                    ModelParam.TrrigerStartCollect("TrrigerStartCollect");
                    break;
                case "停止采集":
                    ModelParam.TrrigerStopCollect("TrrigerStopCollect");
                    break;
                case "执行":
                    _ = ModelParam.ExecuteModule();
                    break;
                case "确认":
                    ModelParam.LoadKeyParam();
                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.LoadKeyParam();
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    if (string.IsNullOrWhiteSpace(SltOutputParamName))
                    {
                        return;
                    }

                    if (!ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object? value) || value is not TransmitParam curSltParam)
                    {
                        return;
                    }

                    if (ModelParam.OutputParams.Any(item => item.ParamName == curSltParam.Name))
                    {
                        MessageBox.Show("已包含重名参数，请重新输入！");
                        return;
                    }

                    ModelParam.OutputParams.Add(new TransmitParam
                    {
                        ParamName = curSltParam.Name,
                        Serial = ModelParam.Serial,
                        Name = Serial + "_" + Name + "_" + curSltParam.Name,
                        Type = curSltParam.Type,
                        Value = curSltParam.Value,
                        Describe = curSltParam.Describe,
                        ResourcePath = curSltParam.ResourcePath,
                    });
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

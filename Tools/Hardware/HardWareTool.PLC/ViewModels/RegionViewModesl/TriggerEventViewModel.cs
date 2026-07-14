using HardWareTool.PLC.Models;
using Newtonsoft.Json;
using ReeYin_V.Hardware.PLC.Models;

namespace HardWareTool.PLC.ViewModels
{
    public class TriggerEventViewModel : BindableBase, INavigationAware
    {
        [JsonIgnore]
        private PLCModel _modelParam;
        public PLCModel ModelParam
        {
            get => _modelParam;
            set => SetProperty(ref _modelParam, value);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<PLCModel>("ModelParam");
        }
    }
}

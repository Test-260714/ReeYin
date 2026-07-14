using HardWareTool.PLC.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Windows;

namespace HardWareTool.PLC.ViewModels
{
    public class WriteSingleAddrViewModel : BindableBase, INavigationAware
    {
        [JsonIgnore]
        private PLCModel _modelParam;
        public PLCModel ModelParam
        {
            get => _modelParam;
            set => SetProperty(ref _modelParam, value);
        }

        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.SltPLCOrder == null) return;
            if (ModelParam.SingleExecute(ModelParam.SltPLCOrder) != NodeStatus.Success)
            {
                MessageBox.Show("执行失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<PLCModel>("ModelParam");
        }
    }
}

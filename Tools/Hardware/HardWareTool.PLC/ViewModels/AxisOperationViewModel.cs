using HardWareTool.PLC.Models;
using Newtonsoft.Json;
using Prism.Dialogs;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace HardWareTool.PLC.ViewModels
{
    public class AxisOperationViewModel : BindableBase, INavigationAware
    {
        [JsonIgnore]
        private PLCModel _modelParam = null!;
        public PLCModel ModelParam
        {
            get => _modelParam;
            set => SetProperty(ref _modelParam, value);
        }

        private ObservableCollection<PLCBase> _plcDevices = new ObservableCollection<PLCBase>();
        public ObservableCollection<PLCBase> PlcDevices
        {
            get => _plcDevices;
            set => SetProperty(ref _plcDevices, value);
        }

        private PLCBase? _selectedPlc;
        public PLCBase? SelectedPlc
        {
            get => _selectedPlc;
            set
            {
                SetProperty(ref _selectedPlc, value);

                if (ModelParam?.SltPLCOrder != null && value != null)
                    ModelParam.SltPLCOrder.TargetPlcId = value.Config.GetID();

                AxisGroups = value?.AxisGroups ?? new ObservableCollection<PLCAxisGroup>();
                SelectedAxisGroup = AxisGroups.FirstOrDefault(group => string.Equals(group.GroupName, ModelParam?.SltPLCOrder?.AxisGroupName, StringComparison.OrdinalIgnoreCase)) ??
                                    AxisGroups.FirstOrDefault();
            }
        }

        private ObservableCollection<PLCAxisGroup> _axisGroups = new ObservableCollection<PLCAxisGroup>();
        public ObservableCollection<PLCAxisGroup> AxisGroups
        {
            get => _axisGroups;
            set => SetProperty(ref _axisGroups, value);
        }

        private PLCAxisGroup? _selectedAxisGroup;
        public PLCAxisGroup? SelectedAxisGroup
        {
            get => _selectedAxisGroup;
            set
            {
                SetProperty(ref _selectedAxisGroup, value);

                if (ModelParam?.SltPLCOrder != null && value != null)
                    ModelParam.SltPLCOrder.AxisGroupName = value.GroupName;

                SyncAxisMoveItems();
            }
        }

        public DelegateCommand ExecuteCommand => new DelegateCommand(() =>
        {
            if (ModelParam?.SltPLCOrder == null) return;
            if (ModelParam.SingleExecute(ModelParam.SltPLCOrder) != NodeStatus.Success)
            {
                MessageBox.Show("执行失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        public DelegateCommand OpenLineScanGenerateCommand => new DelegateCommand(() =>
        {
            PrismProvider.DialogService.ShowDialog("LineScanGenerateView", new DialogParameters
            {
                { "Title", "线扫轨迹生成" },
                { "Icon", "\ue63e" },
                { "Param", ModelParam },
                { "CurrentPlcId", SelectedPlc?.Config?.GetID() ?? string.Empty },
                { "CurrentAxisGroupName", SelectedAxisGroup?.GroupName ?? string.Empty },
            }, result =>
            {
                if (result.Result != ButtonResult.OK)
                    return;

                var generatedOrders = result.Parameters.GetValue<List<PLCOrder>>("Orders");
                string mode = result.Parameters.GetValue<string>("Mode");
                if (generatedOrders == null || generatedOrders.Count == 0 || ModelParam == null)
                    return;

                ModelParam.PLCOrder ??= new ObservableCollection<PLCOrder>();
                if (string.Equals(mode, "Replace", StringComparison.OrdinalIgnoreCase))
                {
                    ModelParam.PLCOrder.Clear();
                }

                foreach (var order in generatedOrders)
                {
                    ModelParam.PLCOrder.Add(order);
                }

                ModelParam.SltPLCOrder = generatedOrders[0];
            }, nameof(DialogWindowView));
        });

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<PLCModel>("ModelParam");
            LoadPlcDevices();
        }

        private void LoadPlcDevices()
        {
            var plcSet = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
            PlcDevices = plcSet?.Models ?? new ObservableCollection<PLCBase>();
            SelectedPlc = PlcDevices.FirstOrDefault(plc =>
                string.Equals(plc.Config.GetID(), ModelParam?.SltPLCOrder?.TargetPlcId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(plc.Config.DisplayName, ModelParam?.SltPLCOrder?.TargetPlcId, StringComparison.OrdinalIgnoreCase)) ??
                          PlcDevices.FirstOrDefault();
        }

        private void SyncAxisMoveItems()
        {
            if (ModelParam?.SltPLCOrder == null || SelectedAxisGroup?.AxisItems == null)
                return;

            for (int i = ModelParam.SltPLCOrder.AxisMoveItems.Count - 1; i >= 0; i--)
            {
                var moveItem = ModelParam.SltPLCOrder.AxisMoveItems[i];
                if (!SelectedAxisGroup.AxisItems.Any(axis => string.Equals(axis.AxisName, moveItem.AxisName, StringComparison.OrdinalIgnoreCase)))
                    ModelParam.SltPLCOrder.AxisMoveItems.RemoveAt(i);
            }

            foreach (var axis in SelectedAxisGroup.AxisItems)
            {
                var item = ModelParam.SltPLCOrder.AxisMoveItems.FirstOrDefault(moveItem =>
                    string.Equals(moveItem.AxisName, axis.AxisName, StringComparison.OrdinalIgnoreCase));

                if (item == null)
                {
                    ModelParam.SltPLCOrder.AxisMoveItems.Add(new PLCOrderAxisMoveItem
                    {
                        AxisName = axis.AxisName,
                        AxisType = axis.AxisType
                    });
                }
                else
                {
                    item.AxisType = axis.AxisType;
                }
            }
        }
    }
}

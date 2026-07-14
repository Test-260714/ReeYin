using ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.ViewModels
{
    public class TronSight2ThicknessCorrectionViewModel
    {
        public TronSight2SensorViewModel ParentViewModel { get; }

        public ObservableCollection<ThicknessCorrectionItem> ThicknessCorrectionItems { get; } = new ObservableCollection<ThicknessCorrectionItem>();

        public TronSight2ThicknessCorrectionViewModel(TronSight2SensorViewModel parentViewModel)
        {
            ParentViewModel = parentViewModel;
        }

        public void LoadItems()
        {
            ThicknessCorrectionItems.Clear();
            if (ParentViewModel?.ModelParam?.TronSight2Sensor == null)
            {
                return;
            }

            foreach (ThicknessCorrectionItem item in ParentViewModel.ModelParam.TronSight2Sensor.LoadThicknessCorrectionItems(ParentViewModel.RefractiveIndexTableItems))
            {
                ThicknessCorrectionItems.Add(item);
            }
        }

        public void PrepareSelectLayer(int layer)
        {
            if (ParentViewModel == null)
            {
                return;
            }

            ParentViewModel.SelectedRefractiveProbeChannel = 1;
            ParentViewModel.SelectedRefractiveLayer = layer;
            ParentViewModel.IsRefractiveTableEditMode = false;
        }

        public bool ApplySelectedRefractiveToLayer(int layer)
        {
            if (ParentViewModel?.ModelParam?.TronSight2Sensor == null)
            {
                return false;
            }

            RefractiveIndexTableItem selectedItem = ParentViewModel.RefractiveIndexTableItems.FirstOrDefault(item => item != null && item.IsSelected);
            if (selectedItem == null)
            {
                return false;
            }

            bool result = ParentViewModel.ModelParam.TronSight2Sensor.UseSelectedRefractiveIndex(1, layer, selectedItem.Label);
            if (result)
            {
                LoadItems();
            }

            return result;
        }

        public bool SaveCorrectionFactor(ThicknessCorrectionItem item)
        {
            if (ParentViewModel?.ModelParam?.TronSight2Sensor == null || item == null)
            {
                return false;
            }

            return ParentViewModel.ModelParam.TronSight2Sensor.SaveThicknessCorrectionFactor(1, item.LayerIndex, item.CorrectionFactor);
        }
    }
}

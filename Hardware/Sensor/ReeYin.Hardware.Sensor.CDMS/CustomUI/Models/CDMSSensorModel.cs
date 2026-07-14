using Newtonsoft.Json;
using Prism.Mvvm;

namespace ReeYin.Hardware.Sensor.CDMS.CustomUI.Models
{
    public class CDMSSensorModel : BindableBase
    {
        [JsonIgnore]
        private CDMSSensor _sensor = new();

        public CDMSSensor Sensor
        {
            get => _sensor;
            set { _sensor = value; RaisePropertyChanged(); }
        }
    }
}

using Newtonsoft.Json;
using ReeYin_V.Core.Services.Module;
using System;
using ReeYin.Hardware.Sensor.JueXin;

namespace ReeYin.Hardware.Sensor.JueXin.CustomUI.Models
{
    [Serializable]
    public class JueXinSensorModel : BindableBase
    {
        [JsonIgnore]
        private JueXinSensor _jueXinSensor;

        public JueXinSensor JueXinSensor
        {
            get { return _jueXinSensor; }
            set { _jueXinSensor = value; RaisePropertyChanged(); }
        }

        public JueXinSensorModel()
        {
            _jueXinSensor = new JueXinSensor();
        }
    }
}

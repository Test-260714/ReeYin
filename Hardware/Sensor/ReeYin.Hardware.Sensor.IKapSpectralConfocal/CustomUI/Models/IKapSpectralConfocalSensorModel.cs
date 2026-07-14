using Newtonsoft.Json;
using ReeYin_V.Core.Services.Module;
using System;

namespace ReeYin.Hardware.Sensor.IKapSpectralConfocal.CustomUI.Models
{
    [Serializable]
    public class IKapSpectralConfocalSensorModel : BindableBase
    {
        [JsonIgnore]
        private IKapSpectralConfocalSensor _sensor = new IKapSpectralConfocalSensor();

        public IKapSpectralConfocalSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; RaisePropertyChanged(); }
        }
    }
}
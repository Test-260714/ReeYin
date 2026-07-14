using Newtonsoft.Json;
using ReeYin_V.Core.Services.Module;
using System;

namespace ReeYin.Hardware.Sensor.MEGAPHASE.Models
{
    [Serializable]
    public class MegAphaseSensorModel : BindableBase
    {
        #region Fields
        [JsonIgnore]
        private MegAphaseSensor _megAphaseSensor;
        #endregion

        #region Properties
        public MegAphaseSensor MegAphaseSensor
        {
            get { return _megAphaseSensor; }
            set { _megAphaseSensor = value; RaisePropertyChanged(); }
        }
        #endregion
    }
}

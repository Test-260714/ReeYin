using Newtonsoft.Json;
using ReeYin_V.Core.Services.Module;
using System;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models
{
    [Serializable]
    public class TronSight2SensorModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private TronSight2Sensor tronSight2Sensor;

        public TronSight2Sensor TronSight2Sensor
        {
            get { return tronSight2Sensor; }
            set { tronSight2Sensor = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public TronSight2SensorModel()
        {

        }
        #endregion

        #region Methods

        #endregion
    }
}

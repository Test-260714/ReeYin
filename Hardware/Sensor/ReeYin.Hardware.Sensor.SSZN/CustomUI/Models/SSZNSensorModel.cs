using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.SSZN.CustomUI
{
    [Serializable]
    public class SSZNSensorModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private SSZNSensor _sensor;

        public SSZNSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SSZNSensorModel()
        {
            
        }
        #endregion

    }
}

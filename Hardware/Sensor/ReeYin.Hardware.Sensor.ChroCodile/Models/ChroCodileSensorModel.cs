using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.ChroCodile.Models
{
    [Serializable]
    public class ChroCodileSensorModel : BindableBase
    {
        #region Properties
        [JsonIgnore]
        private ChroCodileSensor _chroCodileSensor;

        public ChroCodileSensor ChroCodileSensor
        {
            get { return _chroCodileSensor; }
            set { _chroCodileSensor = value; RaisePropertyChanged(); }
        }

        #endregion
    }
}

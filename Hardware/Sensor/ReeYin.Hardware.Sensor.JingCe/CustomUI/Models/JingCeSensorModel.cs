using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.JingCe.CustomUI.Models
{
    public class JingCeSensorModel : BindableBase
    {
        #region Properties
        [JsonIgnore]
        private JingCeSensor _jingCeSensor;

        public JingCeSensor JingCeSensor
        {
            get { return _jingCeSensor; }
            set { _jingCeSensor = value; RaisePropertyChanged(); }
        }

        #endregion
    }
}

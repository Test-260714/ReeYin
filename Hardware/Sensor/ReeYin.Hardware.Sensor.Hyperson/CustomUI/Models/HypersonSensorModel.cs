using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReeYin.Hardware.Sensor.Hyperson.API;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.Hyperson.CustomUI.Models
{
    [Serializable]
    public class HypersonSensorModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private HypersenSensor hypersenSensor;

        public HypersenSensor HypersenSensor
        {
            get { return hypersenSensor; }
            set { hypersenSensor = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public HypersonSensorModel()
        {


        }
        #endregion

        #region Methods

        #endregion
    }

}

using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTBox.Models
{
    public class HPManualModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        private int _runNum;

        public int RunNum
        {
            get { return _runNum; }
            set { _runNum = value; RaisePropertyChanged(); }
        }

        #endregion

    }
}

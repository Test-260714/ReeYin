using Newtonsoft.Json;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardWareTool.PLC.Models
{
    [Serializable]
    public class PLCMonitorModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private PLCOrder _param = new PLCOrder();
        public PLCOrder Param
        {
            get { return _param; }
            set { _param = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public PLCMonitorModel()
        {
            
        }
        #endregion

        #region Methods

        #endregion

    }
}

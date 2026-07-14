using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTBox.Models
{
    public enum KBTPLCStatus
    {
        待机,
        运行,
        报警,
    }

    public class AutoPorcessModel : BindableBase
    {
        #region Fields
        [JsonIgnore]
        public PLCBase CurPLC { get; set; }

        #endregion

        #region Properties
        private int _plcStatus;

        public int PlcStatus
        {
            get { return _plcStatus; }
            set { _plcStatus = value; RaisePropertyChanged(); }
        }

        private int _runNum;

        public int RunNum
        {
            get { return _runNum; }
            set { _runNum = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public AutoPorcessModel()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();

            if (models.Models.Count > 0)
                CurPLC = models.Models[0];
        }
        #endregion

    }
}

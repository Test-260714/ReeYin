using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using System.Collections.ObjectModel;

namespace Custom.KBTScraper.Models
{
    /// <summary>
    /// 传感器状态枚举
    /// </summary>
    public enum SensorStatusEnum
    {
        未连接,
        已连接,
        采集中,
    }

    public class AutoProcessModel : BindableBase
    {
        #region Fields
        [JsonIgnore]
        public ObservableCollection<SensorBase> Sensors { get; set; }

        [JsonIgnore]
        public SensorBase? CurSensor { get; set; }

        [JsonIgnore]
        public PLCBase? CurPLC { get; set; }
        #endregion

        #region Properties
        private string _sensorStatus = "未连接";
        /// <summary>
        /// 传感器状态
        /// </summary>
        public string SensorStatus
        {
            get { return _sensorStatus; }
            set { _sensorStatus = value; RaisePropertyChanged(); }
        }

        private int _runNum;
        /// <summary>
        /// 采集段数
        /// </summary>
        public int RunNum
        {
            get { return _runNum; }
            set { _runNum = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public AutoProcessModel()
        {
            // 初始化传感器
            var sensorConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
            Sensors = sensorConfig.Models ?? new ObservableCollection<SensorBase>();
            
            if (Sensors.Count > 0)
                CurSensor = Sensors[0];

            // 初始化PLC
            var plcConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
            if (plcConfig.Models.Count > 0)
                CurPLC = plcConfig.Models[0];
        }
        #endregion

        #region Methods
        /// <summary>
        /// 更新传感器状态
        /// </summary>
        public void UpdateSensorStatus()
        {
            if (CurSensor == null)
            {
                SensorStatus = "未配置";
                return;
            }

            SensorStatus = CurSensor.IsConnected ? "已连接" : "未连接";
        }

        /// <summary>
        /// 向PLC写入bool值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">写入值</param>
        /// <returns>是否成功</returns>
        public bool WritePLCBool(string address, bool value)
        {
            if (CurPLC == null)
                return false;

            var param = new PLCParaInfoModel
            {
                PLCAddress = address,
                ParaType = EnumParaInfoModelParaType.Bool,
                ParaValue = value
            };

            return CurPLC.WritePLCPara(param);
        }
        #endregion
    }
}

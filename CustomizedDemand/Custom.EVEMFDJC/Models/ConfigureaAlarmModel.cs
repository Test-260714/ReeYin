using Newtonsoft.Json;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.EVEMFDJC.Models
{
    /// <summary>
    /// 配置PLC地址对应异常信息页面绑定的所有数据
    /// </summary>
    [Serializable]
    public class ConfigureaAlarmModel : BindableBase
    {
        #region Properties
        [JsonIgnore]
        private ObservableCollection<ConfigureAlarm> _configureaAlarm = new ObservableCollection<ConfigureAlarm>();
        /// <summary>
        /// 配置PLC地址对应异常信息集合
        /// </summary>
        public ObservableCollection<ConfigureAlarm> ConfigureaAlarm
        {
            get { return _configureaAlarm; }
            set { _configureaAlarm = value; RaisePropertyChanged(); }
        }


        private ConfigureAlarm _sltConfigureaAlarm;
        /// <summary>
        /// 选中项
        /// </summary>

        public ConfigureAlarm SltConfigureaAlarm
        {
            get { return _sltConfigureaAlarm; }
            set { _sltConfigureaAlarm = value; RaisePropertyChanged(); }
        }
        #endregion

    }

    /// <summary>
    /// 配置PLC地址对应异常信息页面每一行数据对应的对象
    /// </summary>
    [Serializable]
    public class ConfigureAlarm : PLCOrder
    {
        #region Properties
        private string _alarmContent;
        /// <summary>
        /// 对应PLC地址的报警信息
        /// </summary>
        public string AlarmContent
        {
            get { return _alarmContent; }
            set { _alarmContent = value; RaisePropertyChanged(); }
        }
        #endregion
    }
}

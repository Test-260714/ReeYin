using Arction.Wpf.Charting.Maps;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Custom.EVEMFDJC.Models
{
    public class EveAlarmLogModel : BindableBase
    {
        #region Properties
        /// <summary>
        /// plc地址状态类集合
        /// </summary>
        private ObservableCollection<PlcAlarmInformation> _plcAlarmInformation = new ObservableCollection<PlcAlarmInformation>();

        public ObservableCollection<PlcAlarmInformation> PlcAlarmInformation
        {
            get { return _plcAlarmInformation; }
            set { _plcAlarmInformation = value; RaisePropertyChanged(); }
        }


        /// <summary>
        ///PLC点位地址和报警信息对应关系列表
        /// </summary>
        private ObservableCollection<ConfigureAlarm> _alarmInformation = new ObservableCollection<ConfigureAlarm>();

        public ObservableCollection<ConfigureAlarm> PlcConfigureAlarm
        {
            get { return _alarmInformation; }
            set { _alarmInformation = value; RaisePropertyChanged(); }
        }
        #endregion
    }

    /// <summary>
    /// 报警信息类
    /// </summary>
    public class PlcAlarmInformation : BindableBase
    {
        #region Properties
        /// <summary>
        /// 序号
        /// </summary>
        private int _id;
        public int Id
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 信息内容
        /// </summary>
        private string _information;

        public string Information
        {
            get { return _information; }
            set { _information = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 触发时间
        /// </summary>
        private string _triggertime;

        public string Triggertime
        {
            get { return _triggertime; }
            set { _triggertime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 地址
        /// </summary>
        private string _address;

        public string Address
        {
            get { return _address; }
            set { _address = value; RaisePropertyChanged(); }
        }
        #endregion
    }
}

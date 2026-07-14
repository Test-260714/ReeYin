using ComTool.General.Communacation;
using ComTool.General.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LogicalTool.Monitor.Models
{
    /// <summary>
    /// 监听类型
    /// </summary>
    public enum MonitorType
    {
        串口网口,
        PLC,
        自定义,
    }

    [Serializable]
    public class MonitorModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public CommunicationSetModel ComModel = new CommunicationSetModel();

        [JsonIgnore]
        public PLCSetModel PLCModel;

        [JsonIgnore]
        private DispatcherTimer monitorTimer;
        #endregion

        #region Properties
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ObservableCollection<MonitorCondition> monitors = new ObservableCollection<MonitorCondition>();

        public ObservableCollection<MonitorCondition> Monitors
        {
            get { return monitors; }
            set { monitors = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string sltCom ;
        public string SltCom
        {
            get { return sltCom; }
            set { sltCom = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private MonitorType _sltMonitorType;
        public MonitorType SltMonitorType
        {
            get { return _sltMonitorType; }
            set { _sltMonitorType = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public MonitorModel()
        {
            ComModel = PrismProvider.HardwareModuleManager.Modules[ConfigKey.ComConfig] as CommunicationSetModel ?? new CommunicationSetModel();
            PLCModel = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
        }

        ~MonitorModel()
        {
            
        }
        #endregion

        #region Methods

        public override bool OnceInit()
        {
            if (IsOnceInit)
            {
                return true;
            }

            if (!base.OnceInit())
            {
                return false;
            }

            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };

            // 提取所有需要匹配的监控名称
            var monitorNames = Monitors.Select(m => m.Name).ToList();

            monitorTimer = new DispatcherTimer();
            monitorTimer.Interval = TimeSpan.FromMilliseconds(50);
            monitorTimer.Tick += (s, e) => MonitorSpecific();
            monitorTimer.Start();

            IsOnceInit = true;
            return true;
        }
        public override void Dispose()
        {
            monitorTimer.Stop();
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {

                return NodeStatus.Success;

            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：监听模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// 监听指定的内容
        /// </summary>
        private void MonitorSpecific()
        {
            foreach (var monitor in Monitors)
            {
                //PLC
                if(monitor.Type == MonitorType.PLC && monitor.IsUsing )
                {
                    if (SltCom == null)
                        return;
                    var SltObj = PLCModel.Models.FirstOrDefault(p => p.Config.DisplayName == SltCom);

                    if(SltObj == null)
                    {
                        //没有找到对应的PLC设备，移除对应的监听对象
                        Monitors.Remove(monitor);
                        Console.WriteLine($"未找到对应的PLC对象，移除监听对象");
                        return;
                    }

                    if (monitor.Param == null) return;
                    var temp = (PLCOrder)monitor.Param;
                    var rtn = new AddressMappingItem
                    {
                        Address = temp.Addr,
                        DataType = temp.ParamType,
                    };
                    SltObj.ReadPLCPara(rtn);

                    //与结果匹配触发执行
                    if (rtn.Value != null && rtn.Value.ToString() == temp.JudgeValue.ToString())
                    {
                        var Param = new PLCParaInfoModel
                        {
                            PLCAddress = temp.Addr,
                            ParaType = temp.ParamType,
                            ParaValue = false
                        };
                        SltObj.WritePLCPara(Param);
                        Console.WriteLine($"监听PLC地址：{temp.Addr}，读取值：{rtn.Value}，监听值：{monitor.Value}");
                        Task.Delay(2000).Wait();
                        //说明之前流程还没执行完
                        if (PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(Serial) && PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[Serial].Count != 0)
                        {
                            Console.WriteLine("上一个流程还没执行完！");
                            return;
                        }

                        PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Publish((eRunStatus.Running, Serial));
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// 监听条件
    /// </summary>
    [Serializable]
    public class MonitorCondition : BindableBase
    {
        [JsonIgnore]
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private MonitorType _type;
        public MonitorType Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Guid _guid;
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Filtrate _condition;
        /// <summary>
        /// 监听条件
        /// </summary>
        public Filtrate Condition
        {
            get { return _condition; }
            set { _condition = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _value;
        /// <summary>
        /// 监听值
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing;
        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private object _param;
        public object Param
        {
            get { return _param; }
            set { _param = value; RaisePropertyChanged(); }
        }

    }

    /// <summary>
    /// 赛选条件
    /// </summary>
    public enum Filtrate
    {
        包含,
        等于,
        自定义
    }
}

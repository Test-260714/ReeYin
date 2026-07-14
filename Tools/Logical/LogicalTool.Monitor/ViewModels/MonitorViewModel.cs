using ComTool.General.Models;
using DryIoc.ImTools;
using LogicalTool.Monitor.Models;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Share.Events;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Windows;


namespace LogicalTool.Monitor.ViewModels
{
    [Serializable]
    public class MonitorViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties
        private Array _condition = Enum.GetValues(typeof(Filtrate));

        public Array Condition
        {
            get { return _condition; }
            set { _condition = value; RaisePropertyChanged(); }
        }

        private Array _ComLists = null;

        public Array ComLists
        {
            get { return _ComLists; }
            set { _ComLists = value; RaisePropertyChanged(); }
        }

        private MonitorCondition selectedMonitor;

        public MonitorCondition SelectedMonitor
        {
            get { return selectedMonitor; }
            set { selectedMonitor = value; }
        }

        private ObservableCollection<TransmitParam> _globalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get { return _globalParams; }
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _globalParamsNames = new ObservableCollection<string>();

        public ObservableCollection<string> GlobalParamsNames
        {
            get { return _globalParamsNames; }
            set { _globalParamsNames = value; RaisePropertyChanged(); }
        }

        private TransmitParam _sltGlobalParam;

        public TransmitParam SltGlobalParam
        {
            get { return _sltGlobalParam; }
            set { _sltGlobalParam = value; RaisePropertyChanged(); }
        }

        public MonitorModel ModelParam
        {
            get { return base.ModelParam as MonitorModel; }
            set { base.ModelParam = value; }
        }

        #endregion

        #region Constructor

        public MonitorViewModel()
        {
            GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
            //注册重新打开项目事件
            PrismProvider.EventAggregator.GetEvent<ProjectRelatedEvent>().Subscribe(ReleaseResources, ThreadOption.UIThread);

            foreach (var item in GlobalParams)
            {
                GlobalParamsNames.Add(item.Serial.ToString("D3") + "_" + item.Name);
            }
        }

        #endregion

        #region Methods
        public override void InitParam()
        {
            InitModelParam<MonitorModel>();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="order"></param>
        public override void ReleaseResources(string order)
        {
            if (order != "释放")
                return;

            // 提取所有需要匹配的监控名称
            var monitorNames = ModelParam.Monitors.Select(m => m.Name).ToList();

            // 筛选出 Key 在监控名称中的通信模型，并移除事件
            foreach (var com in ModelParam.ComModel.CommunicationModels
                .Where(c => monitorNames.Contains(c.Key)))
            {
                //移除监控事件
                ModelParam.TriggerModuleRun = null;
                com.ReceiveString -= MonitorCom;
            }

            // 筛选出 DisplayName 在监控名称中的PLC模型，并移除事件
            foreach (var item in ModelParam.PLCModel.Models.
                Where(c=> monitorNames.Contains(c.Config.DisplayName)))
            {

            }
        }

        /// <summary>
        /// 监视通信
        /// </summary>
        public void MonitorCom(string str)
        {

            foreach (var item in ModelParam.Monitors)
            {
                if (!item.IsUsing)
                {
                    Console.WriteLine($"{ModelParam.Serial}未启用，不在监听！");
                    return;
                }
   
                switch (item.Condition)
                {
                    case Filtrate.包含:
                        {
                            if (str.Contains(item.Value))
                            {
                                //这样判断也可以，但是就没有移除监听的必要（虽然现在移除也没啥用，还没找到原因）
                                if (ModelParam.TriggerModuleRun == null)
                                    return;
                                //说明之前流程还没执行完
                                if(PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds.ContainsKey(ModelParam.Serial) && PrismProvider.ProjectManager.SltCurSolutionItem.IsProcessEnds[ModelParam.Serial].Count != 0)
                                {
                                    Console.WriteLine("上一个流程还没执行完！");
                                    return;
                                }


                                PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Publish((eRunStatus.Running, ModelParam.Serial));

                            }
                        }
                        break;
                    case Filtrate.等于:
                        {
                            if (str == item.Value)
                            {

                                //MessageBox.Show("监听到关键字等于目标！");
                            }
                        }
                        break;



                    default:
                        {
                            MessageBox.Show("不满足任何要求！");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 项目被加载时就得触发
        /// </summary>
        public void AddMonitor(string order)
        {
            if(order == "加载")
            {
                // 提取所有需要匹配的监控名称
                var monitorNames = ModelParam.Monitors.Select(m => m.Name).ToList();

                // 筛选出 Key 在监控名称中的通信模型，并添加事件
                foreach (var com in ModelParam.ComModel.CommunicationModels
                    .Where(c => monitorNames.Contains(c.Key)))
                {
                    com.ReceiveString += MonitorCom;

                    ModelParam.TriggerModuleRun += () =>
                    {
                        return ModelParam.ExecuteModule().Result;
                    };
                }

                foreach (var plc in ModelParam.PLCModel.Models.
                    Where(c=> monitorNames.Contains(c.Config.DisplayName)))
                {
                    
                }
            }
        }
        
        #endregion

        #region Commands
        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    try
                    {
                        switch (ModelParam.SltMonitorType)
                        {
                            case MonitorType.串口网口:
                                {
                                    if (ModelParam.SltCom == null)
                                        return;
                                    var CurCom = ModelParam.ComModel.CommunicationModels.FirstOrDefault(p => p.Key == ModelParam.SltCom);

                                    //先释放
                                    //if (ModelParam.Monitors.Select(p => p.Name == CurCom.Key).ToList().Count == 0)
                                    {
                                        ReleaseResources("释放");
                                    }

                                    ModelParam.Monitors.Add(new MonitorCondition
                                    {
                                        Name = CurCom.Key,
                                        Guid = CurCom.guid,
                                        Type = ModelParam.SltMonitorType,
                                        Condition = Filtrate.包含,
                                        Value = "",
                                        IsUsing = false
                                    });
                                    //重新监听
                                    AddMonitor("加载");
                                }
                                break;
                            case MonitorType.PLC:
                                {
                                    if (ModelParam.SltCom == null)
                                        return;
                                    var SltObj = ModelParam.PLCModel.Models.FirstOrDefault(p => p.Config.DisplayName == ModelParam.SltCom);

                                    ModelParam.Monitors.Add(new MonitorCondition
                                    {
                                        Name = SltObj.Config.DisplayName,
                                        Type = ModelParam.SltMonitorType,
                                        Condition = Filtrate.自定义,
                                        Value = "",
                                        IsUsing = false
                                    });
                                }
                                break;
                            case MonitorType.自定义:
                                {

                                }break;
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                    break;
                case "Delete":
                    ModelParam.Monitors.Remove(SelectedMonitor);
                    break;
                case "Modify":
                    {

                    }break;

                case "切换监听类型":
                    {
                        if(ModelParam.SltMonitorType == MonitorType.串口网口)
                            ComLists = ModelParam.ComModel.CommunicationModels.Select(p => p.Key).ToArray();

                        if(ModelParam.SltMonitorType == MonitorType.PLC)
                            ComLists = ModelParam.PLCModel.Models.Select(p => p.Config.DisplayName).ToArray();

                        if(ModelParam.SltMonitorType == MonitorType.自定义)
                        {
                            ComLists = new string[] 
                            {
                                "持续触发",
                                "触发指定次数",

                            };
                        }
                    }
                    break;

                default:
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam.LoadKeyParam();

            //等待加载完成赋值
            ComLists = ModelParam.ComModel.CommunicationModels.Select(p=>p.Key).ToArray();
            
            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                AddMonitor("加载");

                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "执行":
                    {

                    }break;
                case "编辑参数":
                    {
                        if (SelectedMonitor != null) 
                            PrismProvider.DialogService.ShowDialog("PLCMonitorView", new DialogParameters
                            {
                                { "Title", "PLC地址设置" },
                                { "Icon",  "\ue63e"},
                                { "Param",  SelectedMonitor.Param},
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    var param = result.Parameters.GetValue<object>("Param") as PLCOrder;
                                    SelectedMonitor.Param = param;
                                }
                            }, nameof(DialogWindowView));
                    }
                    break;
                case "使能":
                    if (SelectedMonitor.IsUsing)
                    {
                        Console.WriteLine("已启用!");
                    }
                    else
                    {
                        Console.WriteLine("取消启用!");
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });
                    break;
                default:
                    break;
            }
        });
        #endregion


    }
}

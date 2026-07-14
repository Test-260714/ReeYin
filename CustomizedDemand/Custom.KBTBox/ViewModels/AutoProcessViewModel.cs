using Custom.KBTBox.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Custom.KBTBox.ViewModels
{
    /// <summary>
    /// 单边流程配置
    /// </summary>
    public class EdgeConfig
    {
        public Func<bool> StartSignal;
        public Action ResetStart;
        public Func<bool> EndSignal;
        public Action ResetEnd;

        public int StartValue;
        public int EndValue;

        public string StartEvent;  // Prism事件
        public string EndEvent;
    }


    public class AutoProcessViewModel : DialogViewModelBase
    {
        #region Fields
        private CancellationTokenSource _autoProcessCTS;

        private DispatcherTimer _timer;

        /// <summary>
        /// 每条边是否开始采集
        /// </summary>
        private bool _firstS,_secondS, _thirdS, _fourthS;
        private bool _firstE,_secondE, _thirdE, _fourthE;

        private bool fromPLCStart,fromPLCEnd = false;
        #endregion

        #region Properties
        private AutoPorcessModel _model = new AutoPorcessModel();

        public AutoPorcessModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public AutoProcessViewModel()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += (s, e) => RefreshView();
            _timer.Start();

            // 检查是否需要自动启动流程
            Task.Run(() =>
            {
                Task.Delay(2000).Wait(); // 延迟2秒等待初始化完成
                try
                {
                    var sensorModel = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches
                        .Values.OfType<SensorDataCollectionModel>().FirstOrDefault();
                    if (sensorModel?.OtherConfig?.IsAutoStart == true)
                    {
                        Logs.LogInfo("检测到自动启动配置，启动自动流程");
                        StartAutoProcess();
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError($"自动启动流程失败: {ex.Message}");
                }
            });
        }
        #endregion

        #region Methods

        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "开始":
                    {
                        StartAutoProcess();
                        Logs.LogInfo($"触发自动检测流程！");
                    }
                    break;

                case "停止":
                    {
                        StopAutoProcess();
                    }
                    break;

                case "重置":
                    {
                        StopAutoProcess();
                    }
                    break;

                case "确认":
                    {

                    }
                    break;

                default:
                    break;
            }
        });

        #endregion

        #region Methods

        /// <summary>
        /// 刷新View，获取PLC的一些地址值
        /// </summary>
        private void RefreshView()
        {
            #region 读取PLC地址数据
            if (!Model.CurPLC.Config.IsConnected)
            {
                Model.PlcStatus = -1;
                return; 
            }

            var D1000 = new PLCParaInfoModel
            {
                PLCAddress = "1000",
                ParaType = EnumParaInfoModelParaType.Ushort
            };

            var D1001 = new PLCParaInfoModel
            {
                PLCAddress = "1001",
                ParaType = EnumParaInfoModelParaType.Ushort
            };

            Model.CurPLC.ReadPLCPara(D1000);

            Model.CurPLC.ReadPLCPara(D1001);

            if (D1000.ParaValue != null)
            {
                _firstS = (((ushort)D1000.ParaValue >> 0) & 1) == 1;
                _firstE = (((ushort)D1000.ParaValue >> 1) & 1) == 1;
                _secondS = (((ushort)D1000.ParaValue >> 2) & 1) == 1;
                _secondE = (((ushort)D1000.ParaValue >> 3) & 1) == 1;

                _thirdS = (((ushort)D1000.ParaValue >> 4) & 1) == 1;
                _thirdE = (((ushort)D1000.ParaValue >> 5) & 1) == 1;
                _fourthS = (((ushort)D1000.ParaValue >> 6) & 1) == 1;
                _fourthE = (((ushort)D1000.ParaValue >> 7) & 1) == 1;
            }

            if (D1001.ParaValue != null)
                Model.PlcStatus = (ushort)D1001.ParaValue;
            #endregion
        }

        /// <summary>
        /// 启动自动流程
        /// </summary>
        public void StartAutoProcess()
        {
            _autoProcessCTS = new CancellationTokenSource();
            _ = AutoProcess(_autoProcessCTS.Token);
        }

        /// <summary>
        /// 停止自动流程
        /// </summary>
        public void StopAutoProcess()
        {
            _autoProcessCTS?.Cancel();
            Logs.LogInfo("取消自动流程！");
        }

        /// <summary>
        /// 自动流程核心逻辑
        /// </summary>
        private async Task AutoProcess(CancellationToken ct)
        {
            try
            {
                var edges = new List<EdgeConfig>
                {
                    new EdgeConfig
                    {
                        StartSignal = () => _firstS,
                        ResetStart  = () => _firstS = false,
                        EndSignal   = () => _firstE,
                        ResetEnd    = () => _firstE = false,
                        StartValue  = 1,
                        EndValue    = 2,
                        StartEvent  = "TrrigerStartCollect",
                        EndEvent    = "TrrigerStopCollect"
                    },
                    new EdgeConfig
                    {
                        StartSignal = () => _secondS,
                        ResetStart  = () => _secondS = false,
                        EndSignal   = () => _secondE,
                        ResetEnd    = () => _secondE = false,
                        StartValue  = 4,
                        EndValue    = 8,
                        StartEvent  = "TrrigerStartCollect",
                        EndEvent    = "TrrigerStopCollect"
                    },
                    new EdgeConfig
                    {
                        StartSignal = () => _thirdS,
                        ResetStart  = () => _thirdS = false,
                        EndSignal   = () => _thirdE,
                        ResetEnd    = () => _thirdE = false,
                        StartValue  = 16,
                        EndValue    = 32,
                        StartEvent  = "TrrigerStartCollect",
                        EndEvent    = "TrrigerStopCollect"
                    },
                    new EdgeConfig
                    {
                        StartSignal = () => _fourthS,
                        ResetStart  = () => _fourthS = false,
                        EndSignal   = () => _fourthE,
                        ResetEnd    = () => _fourthE = false,
                        StartValue  = 64,
                        EndValue    = 128,
                        StartEvent  = "TrrigerStartCollect",
                        EndEvent    = "TrrigerDispose"
                    }
                };

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        for (int i = 0; i < edges.Count; i++)
                        {
                            // 检查软件工作状态，如果是Error，重置为Idle并重新开始
                            if (PrismProvider.WorkStatusManager.CurStatus == WorkStatus.Error)
                            {
                                Logs.LogError("检测到软件Error状态，重置为Idle并重新开始流程");
                                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
                                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                                break;
                            }

                            // 检查PLC状态 D1001 (0=空闲, 1=运行, 2=错误)
                            if (Model.PlcStatus == 2 && PrismProvider.WorkStatusManager.CurStatus == WorkStatus.Running)
                            {
                                Logs.LogError("检测到PLC错误状态(D1001=2)，重置为Idle并重新开始流程");
                                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
                                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                                break;
                            }

                            await ProcessEdgeAsync(i + 1, edges[i], ct);
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        Logs.LogError($"流程超时: {ex.Message}，重置为Idle并重新开始流程");
                        PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                        // 超时后重新开始，回到第一步
                        continue;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logs.LogInfo("自动流程被取消");
            }
        }

        /// <summary>
        /// 单边流程
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cfg"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessEdgeAsync(
    int index,
    EdgeConfig cfg,
    CancellationToken ct)
        {
            // 等待采集开始信号（无超时限制）
            await WaitForSignalAsync(cfg.StartSignal, cfg.ResetStart,
                $"PLC触发开始采集[{index}]...", ct, 0);

            if (index == 1)
            {
                //PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                Task.Delay(1000).Wait();
                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Running);
                //开始执行的时间，作为存图文件夹
                Logs.LogInfo($"流程开始");
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish($"StartTime@{DateTime.Now.ToString("yyyyMMddHHmmssfff")}");
            }

            PrismProvider.EventAggregator .GetEvent<UpdateMessageEvent>() .Publish(cfg.StartEvent);

            Logs.LogInfo($"开始进行第{index}次传感器采集操作...");

            // 写 PLC 开始值
            var param = new PLCParaInfoModel
            {
                PLCAddress = "1002",
                ParaType = EnumParaInfoModelParaType.Ushort,
                ParaValue = (ushort)cfg.StartValue
            };

            if (Model.CurPLC.WritePLCPara(param))
                Logs.LogInfo($"地址1002，写入成功 {cfg.StartValue}");
            else
                Logs.LogInfo($"地址1002，写入失败！！");

            // 等待采集结束信号（4分钟超时）
            await WaitForSignalAsync(cfg.EndSignal, cfg.ResetEnd,
                $"PLC停止采集[{index}]...", ct, 240000);

            PrismProvider.EventAggregator
                .GetEvent<UpdateMessageEvent>()
                .Publish(cfg.EndEvent);

            //延时等算法执行完成
            Task.Delay(4000).Wait();

            // 写 PLC 停止值
            param.ParaValue = (ushort)cfg.EndValue;

            if (Model.CurPLC.WritePLCPara(param))
                Logs.LogInfo($"地址1002，写入成功 {cfg.EndValue}");

            Logs.LogInfo($"结束进行第{index}次传感器采集操作...");
        }

        /// <summary>
        /// 等待PLC信号
        /// </summary>
        /// <param name="check"></param>
        /// <param name="reset"></param>
        /// <param name="log"></param>
        /// <param name="ct"></param>
        /// <param name="timeoutMs">超时时间（毫秒），0表示无限等待</param>
        /// <returns></returns>
        private async Task WaitForSignalAsync(
    Func<bool> check,
    Action reset,
    string log,
    CancellationToken ct,
    int timeoutMs = 0)
        {
            var startTime = DateTime.Now;

            // 每 100ms 检查一次信号
            while (!ct.IsCancellationRequested)
            {
                if (check())
                {
                    reset();
                    Logs.LogInfo(log);
                    return;
                }

                // 检查超时
                if (timeoutMs > 0)
                {
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    if (elapsed >= timeoutMs)
                    {
                        Logs.LogWarning($"等待PLC回复超时4分钟！");
                        throw new TimeoutException($"等待信号超时: {log}");
                    }
                }

                await Task.Delay(100, ct);
            }

            ct.ThrowIfCancellationRequested();
        }

        #endregion
    }
}

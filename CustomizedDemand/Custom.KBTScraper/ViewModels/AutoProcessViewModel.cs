using Custom.KBTScraper.Models;
using HandyControl.Controls;
using HandyControl.Data;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Custom.KBTScraper.ViewModels
{
    public class AutoProcessViewModel : DialogViewModelBase
    {
        #region Fields
        private CancellationTokenSource? _autoProcessCTS;
        
        // 是否正在运行
        private bool _isRunning = false;
        #endregion

        #region Properties
        private AutoProcessModel _model = new AutoProcessModel();

        public AutoProcessModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public AutoProcessViewModel()
        {
            // 初始化时更新传感器状态
            Model.UpdateSensorStatus();
            
            // 订阅段数更新事件（每处理完一段数据，RunNum+1）
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((msg) =>
            {
                if (msg == "SegmentProcessed")
                {
                    Model.RunNum++;
                }
            }, ThreadOption.UIThread);
        }
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
                        if (_isRunning)
                        {
                            Logs.LogInfo("自动流程已在运行中");
                            Growl.WarningGlobal(new GrowlInfo { Message = "自动流程已在运行中", WaitTime = 3, IsCustom = true });
                            return;
                        }
                        
                        // 检查传感器状态
                        Model.UpdateSensorStatus();
                        if (Model.CurSensor == null || !Model.CurSensor.IsConnected)
                        {
                            Logs.LogError("传感器未连接，无法开始采集");
                            Growl.ErrorGlobal(new GrowlInfo { Message = "传感器未连接，无法开始采集", WaitTime = 3, IsCustom = true });
                            return;
                        }
                        
                        // 发布开始时间，用于存图路径
                        string startTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish($"StartTime@{startTime}");
                        
                        Model.SensorStatus = "采集中";
                        StartAutoProcess();
                        Logs.LogInfo("触发自动检测流程 - 刮刀缺陷检测（走带回调模式）");
                        Growl.SuccessGlobal(new GrowlInfo { Message = "开始采集", WaitTime = 3, IsCustom = true });
                    }
                    break;

                case "停止":
                    {
                        // 检查是否在采集中
                        if (!_isRunning)
                        {
                            Logs.LogWarning("当前未在采集中，无需停止");
                            Growl.WarningGlobal(new GrowlInfo { Message = "当前未在采集中，无需停止", WaitTime = 3, IsCustom = true });
                            return;
                        }
                        
                        // 停止采集（StopAutoProcess会触发TrrigerDispose事件）
                        StopAutoProcess();
                        Model.UpdateSensorStatus();
                        Logs.LogInfo($"手动停止采集，共处理 {Model.RunNum} 段数据");
                        Growl.SuccessGlobal(new GrowlInfo { Message = $"停止采集，共处理 {Model.RunNum} 段数据", WaitTime = 3, IsCustom = true });
                    }
                    break;

                case "重置":
                    {
                        StopAutoProcess();
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("ClearGrayShow");
                        Model.RunNum = 0;
                        Model.UpdateSensorStatus();
                        Logs.LogInfo("重置自动流程");
                        Growl.InfoGlobal(new GrowlInfo { Message = "已重置", WaitTime = 3, IsCustom = true });
                    }
                    break;

                case "继续":
                    {
                        // 向PLC 2206地址写入false
                        if (Model.CurPLC == null)
                        {
                            Logs.LogError("PLC未配置，无法发送继续信号");
                            Growl.ErrorGlobal(new GrowlInfo { Message = "PLC未配置", WaitTime = 3, IsCustom = true });
                            return;
                        }

                        bool success = Model.WritePLCBool("2206", false);
                        if (success)
                        {
                            Logs.LogInfo("向PLC地址2206写入false成功");
                            Growl.SuccessGlobal(new GrowlInfo { Message = "继续信号已发送", WaitTime = 3, IsCustom = true });
                        }
                        else
                        {
                            Logs.LogError("向PLC地址2206写入失败");
                            Growl.ErrorGlobal(new GrowlInfo { Message = "继续信号发送失败", WaitTime = 3, IsCustom = true });
                        }
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion

        #region Methods
        /// <summary>
        /// 启动自动流程
        /// </summary>
        public void StartAutoProcess()
        {
            if (_isRunning) return;
            
            _autoProcessCTS = new CancellationTokenSource();
            _isRunning = true;
            _ = AutoProcess(_autoProcessCTS.Token);
        }

        /// <summary>
        /// 停止自动流程
        /// </summary>
        public void StopAutoProcess()
        {
            _autoProcessCTS?.Cancel();
            _isRunning = false;
            Logs.LogInfo("停止自动流程 - 刮刀缺陷检测");
        }

        /// <summary>
        /// 自动流程核心逻辑 - 点击开始直接采集
        /// </summary>
        private async Task AutoProcess(CancellationToken ct)
        {
            try
            {
                Logs.LogInfo("自动流程开始运行");
                
                // 重置采集段数
                Model.RunNum = 0;

                // 启动传感器采集
                Logs.LogInfo("开始传感器采集");
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");

                // 等待取消信号
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                Logs.LogInfo("自动流程被取消");
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerDispose");
                Logs.LogInfo($"走带采集流程完成，共处理 {Model.RunNum} 段数据");
            }
            catch (Exception ex)
            {
                Logs.LogError($"自动流程异常: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
            }
        }
        #endregion
    }
}

using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Share;
using ReeYin_V.Share.Events;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LogicalTool.Loop.Models
{
    [Serializable]
    public class LoopModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        private SubscriptionToken _subscriptionToken { get; set; }

        [JsonIgnore]

        public bool IsLoopFlag;

        /// <summary>
        /// 终止循环标志
        /// </summary>
        [JsonIgnore]
        public bool IsAbortLoop;

        /// <summary>
        /// 传递的循环次数（运行时计数器）
        /// </summary>
        [JsonIgnore]
        [OutputParam("TransmitLoopNum", "当前次数")]
        public int TransmitLoopNum;

        [JsonIgnore]
        private Task _monitorTask;

        [JsonIgnore]
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Properties

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        private int _loopNum = 1;
        /// <summary>
        /// 循环次数（-1表示无限循环）
        /// </summary>
        [OutputParam("LoopNum", "循环次数")]
        public int LoopNum
        {
            get => _loopNum;
            set { _loopNum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isLinkVisibility = Visibility.Hidden;
        /// <summary>
        /// 链接数据可见性
        /// </summary>
        public Visibility IsLinkVisibility
        {
            get => _isLinkVisibility;
            set { _isLinkVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isAssignVisibility = Visibility.Visible;
        /// <summary>
        /// 指定数据可见性
        /// </summary>
        public Visibility IsAssignVisibility
        {
            get => _isAssignVisibility;
            set { _isAssignVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _linkLoopNum;
        /// <summary>
        /// 链接的循环次数参数
        /// </summary>
        public TransmitParam LinkLoopNum
        {
            get => _linkLoopNum;
            set { _linkLoopNum = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public LoopModel()
        {

        }

        #endregion

        #region Override

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }
                _subscriptionToken = PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((obj) =>
                {
                    if (obj == WorkStatus.Error)
                    {
                        IsLoopFlag = false;
                        TransmitLoopNum = 0;
                    }

                }, ThreadOption.UIThread);

                TransmitLoopNum = 0;

                // 启动循环监控任务
                StartLoopMonitor();

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun += () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 加载关键参数
        /// </summary>
        public override bool LoadKeyParam()
        {
            try
            {
                ModuleName = Serial.ToString("D3");

                if (!PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Keys.Contains(Serial.ToString("D3")))
                {
                    TransmitLoopNum = 0;

                    // 启动循环监控任务
                    StartLoopMonitor();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常：{ex.Message}");
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _subscriptionToken.Dispose();
            // 停止监控任务
            StopLoopMonitor();
        }

        #endregion

        #region Methods

        /// <summary>
        /// 启动循环监控任务
        /// </summary>
        private void StartLoopMonitor()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _monitorTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (IsLoopFlag && TransmitLoopNum > 0 && !IsAbortLoop)
                        {
                            IsLoopFlag = false;
                            Console.WriteLine($"循环监控：当前次数/已执行次数 {TransmitLoopNum} 次");

                            // 触发工作状态切换事件
                            PrismProvider.EventAggregator
                                .GetEvent<SwitchWorkStatusEvent>()
                                .Publish((eRunStatus.Running, Serial));
                        }

                        await Task.Delay(10, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        /// <summary>
        /// 停止循环监控任务
        /// </summary>
        private void StopLoopMonitor()
        {
            _cancellationTokenSource?.Cancel();
            _monitorTask?.Wait(1000);
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            try
            {
                var (result, time) = SetTimeHelper.SetTimer(() =>
                {
                    IsLoopFlag = false;
                    //TransmitLoopNum--;
                    TransmitLoopNum++;

                    #region 输出
                    //执行后对输出参数重新赋值
                    foreach (var item in OutputParams)
                    {
                        item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    }

                    var start = DateTime.Now;

                    if (!UpdateParam())
                    {
                        Console.WriteLine($"模块_{Serial}更新参数失败");
                    }
                    Console.WriteLine($"{DateTime.Now.Subtract(start).TotalMilliseconds}");
                    #endregion

                    // 循环开始节点，直接返回成功
                    return NodeStatus.Success;
                });

                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：循环模块执行时间：{time} 毫秒");

                return Output = new ExecuteModuleOutput
                {
                    RunStatus = result,
                    RunTime = time
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"循环模块执行异常：{ex.Message}");
                return Output = new ExecuteModuleOutput
                {
                    RunStatus = NodeStatus.Error,
                    RunTime = 0.0
                };
            }
        }

        #endregion
    }
}

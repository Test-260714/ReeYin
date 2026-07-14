using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.Camera.ViewModels;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ReeYin_V.Initialize.ViewModels
{
    public class InitializeViewModel : DialogViewModelBase
    {
        #region Fields
        private const string ControlCardStartupResetMessage = "控制卡正在复位...";
        private string _message;
        private Guid _controlCardStartupResetOperationId;
        private CancellationTokenSource _controlCardStartupResetTimeoutCts;
        private SubscriptionToken _controlCardResetOverlaySubscriptionToken;

        public string Message
        {
            get { return _message; }
            set { _message = value; RaisePropertyChanged(); }
        }

        #endregion

        public ICommand LoadedCommand { get; }
        public ICommand EnterCommand { get; }
        public InitializeViewModel(/*HardwareLifetimeManager manager*/)
        {
            //Manager = manager;

            LoadedCommand = new DelegateCommand(Init);
            EnterCommand = new DelegateCommand(Enter);
            _controlCardResetOverlaySubscriptionToken = PrismProvider.EventAggregator
                .GetEvent<ControlCardResetOverlayEvent>()
                .Subscribe(OnControlCardResetOverlay, ThreadOption.UIThread);
            //订阅关闭硬件的事件通知
            PrismProvider.EventAggregator.GetEvent<CloseAllHardwareEvent>().Subscribe(() => 
            {
                PrismProvider.HardwareModuleManager.Shutdown();
                //判断是否有需要保存的参数

                //PrismProvider.ProjectManager.TriggerSave();
                //断开所有组件
                //MessageBox.Show("正在关闭所有硬件连接...");
            });
        }

        private void OnControlCardResetOverlay(ControlCardResetOverlayPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            if (payload.IsRunning)
            {
                var timeoutSeconds = payload.TimeoutSeconds > 0 ? payload.TimeoutSeconds : 60;
                _controlCardStartupResetOperationId = payload.OperationId;
                Message = ControlCardStartupResetMessage;
                StartControlCardStartupResetPromptTimeout(payload.OperationId, timeoutSeconds);
                return;
            }

            if (payload.OperationId != _controlCardStartupResetOperationId)
            {
                return;
            }

            _controlCardStartupResetTimeoutCts?.Cancel();
            Message = string.IsNullOrWhiteSpace(payload.Message)
                ? "控制卡复位完成。"
                : payload.Message;
        }

        private void StartControlCardStartupResetPromptTimeout(Guid operationId, int timeoutSeconds)
        {
            _controlCardStartupResetTimeoutCts?.Cancel();
            _controlCardStartupResetTimeoutCts = new CancellationTokenSource();
            _ = HideControlCardStartupResetPromptOnTimeoutAsync(operationId, timeoutSeconds, _controlCardStartupResetTimeoutCts.Token);
        }

        private async Task HideControlCardStartupResetPromptOnTimeoutAsync(Guid operationId, int timeoutSeconds, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            PrismProvider.Dispatcher.Invoke(() =>
            {
                if (operationId != _controlCardStartupResetOperationId)
                {
                    return;
                }

                Message = $"控制卡复位等待超时（{timeoutSeconds}秒）。";
            });
        }

        private void CleanupControlCardStartupResetPrompt()
        {
            _controlCardStartupResetTimeoutCts?.Cancel();
            _controlCardStartupResetTimeoutCts = null;

            if (_controlCardResetOverlaySubscriptionToken == null)
            {
                return;
            }

            PrismProvider.EventAggregator
                .GetEvent<ControlCardResetOverlayEvent>()
                .Unsubscribe(_controlCardResetOverlaySubscriptionToken);
            _controlCardResetOverlaySubscriptionToken = null;
        }

        /// <summary>
        /// 进入主界面
        /// </summary>
        private async void Enter()
        {
            //加载主界面
            await Task.Delay(100).ContinueWith(p =>
            {
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    //加载主界面
                    PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                    //导航到主区域
                    PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "RunMainView");
                });

            });

            CleanupControlCardStartupResetPrompt();
            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 异步方法
        /// </summary>
        private async void Init()
        {
            InitResult result = new InitResult();

            if (!result.Success)
            {
                Message = PrismProvider.LanguageManager.GetStringResource("InitializingAllComponents");
                await Task.Run(() => 
                { 
                    PrismProvider.HardwareModuleManager.Initialize();
                });

                if (PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.ControlCard, out var controlCardModule) &&
                    controlCardModule is IStartupResetHardwareModule controlCardStartupReset)
                {
                    InitResult controlCardResetResult = await controlCardStartupReset.ExecuteStartupResetAsync(message =>
                    {
                        PrismProvider.Dispatcher.Invoke(() => Message = message);
                    });
                    if (!controlCardResetResult.Success)
                    {
                        Message = controlCardResetResult.Message;
                        return;
                    }
                }

                if (PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.PLCConfig, out var module) &&
                    module is PLCSetModel plcSet)
                {
                    InitResult resetResult = await plcSet.ExecuteStartupResetAsync(message =>
                    {
                        PrismProvider.Dispatcher.Invoke(() => Message = message);
                    });
                    if (!resetResult.Success)
                    {
                        Message = resetResult.Message;
                        return;
                    }
                }

                Message = PrismProvider.LanguageManager.GetStringResource("InitializationComponentsCompleted");
            }

            //加载主界面
            await Task.Delay(100).ContinueWith(p =>
            {
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    // 登录成功在加载 ProjectItem；若加载失败（首次运行或文件丢失）则创建默认配置
                    bool loaded = PrismProvider.ProjectManager.SolutionManager.LoadProject(PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.FilePath);
                    if (!loaded && string.IsNullOrWhiteSpace(PrismProvider.ProjectManager.SolutionManager.DefaultBaseInfo?.FilePath))
                        PrismProvider.ProjectManager.EnsureDefaultProject();

                    //加载主界面
                    PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                    //导航到主区域
                    PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "RunMainView");
                });
            });

            CleanupControlCardStartupResetPrompt();
            CloseDialog(ButtonResult.OK);
        }
    }
}

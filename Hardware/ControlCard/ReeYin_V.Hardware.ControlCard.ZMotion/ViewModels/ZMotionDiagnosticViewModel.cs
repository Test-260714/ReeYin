using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.ControlCard.ZMotion.App;
using ReeYin_V.Hardware.ControlCard.ZMotion.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.ViewModels
{
    public sealed class ZMotionDiagnosticViewModel : DialogViewModelBase
    {
        private const int RefreshIntervalMilliseconds = 200;
        private readonly SemaphoreSlim deviceGate = new(1, 1);
        private CancellationTokenSource? refreshCancellation;
        private Task? refreshTask;
        private ZMotionControlCard? controlCard;
        private ZMotionAxisDiagnosticState? selectedAxis;
        private short? joggingAxisId;
        private bool isConnected;
        private bool isBusy;
        private bool isOutputUnlocked;
        private string connectionDescription = "未选择 ZMotion 控制卡";
        private string targetPositionText = "0.000";
        private string statusMessage = "等待诊断页面初始化";
        private string statusTime = string.Empty;
        private string lastRefreshError = string.Empty;

        public ZMotionDiagnosticViewModel()
        {
            for (int port = 0; port < 32; port++)
            {
                Inputs.Add(new ZMotionIoDiagnosticItem(port, false));
                Outputs.Add(new ZMotionIoDiagnosticItem(port, true));
            }
        }

        public ObservableCollection<ZMotionAxisDiagnosticState> Axes { get; } = new();

        public ObservableCollection<ZMotionIoDiagnosticItem> Inputs { get; } = new();

        public ObservableCollection<ZMotionIoDiagnosticItem> Outputs { get; } = new();

        public ZMotionAxisDiagnosticState? SelectedAxis
        {
            get => selectedAxis;
            set
            {
                if (!SetProperty(ref selectedAxis, value))
                {
                    return;
                }

                if (value != null)
                {
                    TargetPositionText = value.Position.ToString("F3", CultureInfo.CurrentCulture);
                }

                RaiseOperationStateChanged();
            }
        }

        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (SetProperty(ref isConnected, value))
                {
                    if (!value)
                    {
                        IsOutputUnlocked = false;
                    }

                    RaisePropertyChanged(nameof(ConnectionStatusText));
                    RaiseOperationStateChanged();
                }
            }
        }

        public string ConnectionStatusText => IsConnected ? "已连接" : "未连接";

        public string ConnectionDescription
        {
            get => connectionDescription;
            private set => SetProperty(ref connectionDescription, value);
        }

        public string TargetPositionText
        {
            get => targetPositionText;
            set => SetProperty(ref targetPositionText, value);
        }

        public bool IsOutputUnlocked
        {
            get => isOutputUnlocked;
            set
            {
                if (SetProperty(ref isOutputUnlocked, value))
                {
                    RaisePropertyChanged(nameof(CanToggleOutputs));
                    ToggleOutputCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RaiseOperationStateChanged();
                }
            }
        }

        public bool CanOperateAxis => controlCard != null && IsConnected && SelectedAxis != null && !IsBusy;

        public bool CanStopAxis => controlCard != null && SelectedAxis != null;

        public bool CanToggleOutputs => controlCard != null && IsConnected && IsOutputUnlocked && !IsBusy;

        public string AxisEnableButtonText => SelectedAxis?.IsEnabled == true ? "去使能" : "使能";

        public string StatusMessage
        {
            get => statusMessage;
            private set => SetProperty(ref statusMessage, value);
        }

        public string StatusTime
        {
            get => statusTime;
            private set => SetProperty(ref statusTime, value);
        }

        private DelegateCommand? toggleAxisEnableCommand;
        public DelegateCommand ToggleAxisEnableCommand => toggleAxisEnableCommand ??=
            new DelegateCommand(ToggleAxisEnable, () => CanOperateAxis);

        private DelegateCommand<string>? startJogCommand;
        public DelegateCommand<string> StartJogCommand => startJogCommand ??=
            new DelegateCommand<string>(StartJog, _ => CanOperateAxis);

        private DelegateCommand? stopJogCommand;
        public DelegateCommand StopJogCommand => stopJogCommand ??= new DelegateCommand(StopPageJog);

        private DelegateCommand? moveAbsoluteCommand;
        public DelegateCommand MoveAbsoluteCommand => moveAbsoluteCommand ??=
            new DelegateCommand(async () => await MoveAbsoluteAsync(), () => CanOperateAxis);

        private DelegateCommand? stopAxisCommand;
        public DelegateCommand StopAxisCommand => stopAxisCommand ??=
            new DelegateCommand(StopSelectedAxis, () => CanStopAxis);

        private DelegateCommand<ZMotionIoDiagnosticItem>? toggleOutputCommand;
        public DelegateCommand<ZMotionIoDiagnosticItem> ToggleOutputCommand => toggleOutputCommand ??=
            new DelegateCommand<ZMotionIoDiagnosticItem>(
                async item => await ToggleOutputAsync(item),
                item => item != null && CanToggleOutputs);

        public override void InitParam()
        {
            controlCard = Param as ZMotionControlCard;
            Axes.Clear();

            if (controlCard == null)
            {
                SetOperationResult("当前控制卡不是 ZMotion 实例");
                RaiseOperationStateChanged();
                return;
            }

            foreach (var axis in controlCard.Config.AllAxis.Where(item => item.AxisNo > 0).OrderBy(item => item.AxisNo))
            {
                Axes.Add(new ZMotionAxisDiagnosticState(axis.AxisNum, axis.AxisNo, axis.NickName));
            }

            SelectedAxis = Axes.FirstOrDefault();
            ConnectionDescription = controlCard.ConnectionType == 0
                ? $"串口 COM{controlCard.ComPort}"
                : $"以太网 {controlCard.IpAddress}";
            IsConnected = controlCard.IsConnected;

            SetOperationResult(Axes.Count == 0 ? "当前控制卡未配置有效轴" : "诊断页面已就绪");
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            if (controlCard == null)
            {
                return;
            }

            refreshCancellation = new CancellationTokenSource();
            refreshTask = Task.Run(() => RefreshLoopAsync(refreshCancellation.Token));
        }

        public override void OnDialogClosed()
        {
            StopPageJog();
            refreshCancellation?.Cancel();
            refreshCancellation?.Dispose();
            refreshCancellation = null;
            base.OnDialogClosed();
        }

        private async Task RefreshLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await RefreshOnceAsync(cancellationToken);
                    await Task.Delay(RefreshIntervalMilliseconds, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                await UpdateRefreshErrorAsync($"诊断刷新已停止：{ex.Message}");
            }
        }

        private async Task RefreshOnceAsync(CancellationToken cancellationToken)
        {
            var card = controlCard;
            if (card == null)
            {
                return;
            }

            bool connected;
            bool axisRead = false;
            float position = 0;
            bool enabled = false;
            bool idle = false;
            bool ioRead = false;
            bool[] inputStates = Array.Empty<bool>();
            bool[] outputStates = Array.Empty<bool>();
            string refreshError = string.Empty;
            var axis = SelectedAxis;

            await deviceGate.WaitAsync(cancellationToken);
            try
            {
                connected = card.IsConnected;
                if (connected && axis != null)
                {
                    axisRead = card.TryGetAxisDiagnostic(
                        axis.ControllerAxisId,
                        out position,
                        out enabled,
                        out idle,
                        out _,
                        out refreshError);
                }

                if (connected)
                {
                    bool inputRead = card.GetAllInput(out inputStates);
                    bool outputRead = card.GetAllOutput(out outputStates);
                    ioRead = inputRead && outputRead;
                    if (!ioRead && string.IsNullOrEmpty(refreshError))
                    {
                        refreshError = "读取数字 IO 状态失败";
                    }
                }
            }
            finally
            {
                deviceGate.Release();
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                return;
            }

            await dispatcher.InvokeAsync(() =>
            {
                IsConnected = connected;
                if (axisRead && axis != null)
                {
                    axis.Update(position, enabled, idle);
                    if (ReferenceEquals(SelectedAxis, axis))
                    {
                        RaisePropertyChanged(nameof(AxisEnableButtonText));
                    }
                }

                if (ioRead)
                {
                    UpdateIoStates(Inputs, inputStates);
                    UpdateIoStates(Outputs, outputStates);
                }

                if (!string.IsNullOrEmpty(refreshError) && refreshError != lastRefreshError)
                {
                    lastRefreshError = refreshError;
                    SetOperationResult(refreshError);
                }
                else if (string.IsNullOrEmpty(refreshError))
                {
                    lastRefreshError = string.Empty;
                }
            });
        }

        private void ToggleAxisEnable()
        {
            var axis = SelectedAxis;
            if (controlCard == null || axis == null)
            {
                return;
            }

            bool targetState = !axis.IsEnabled;
            bool success = controlCard.TrySetAxisEnabled(
                axis.ControllerAxisId,
                targetState,
                out _,
                out string message);

            SetOperationResult(message);
            if (success)
            {
                axis.IsEnabled = targetState;
                RaisePropertyChanged(nameof(AxisEnableButtonText));
            }
        }

        private void StartJog(string? directionText)
        {
            var axis = SelectedAxis;
            if (controlCard == null || axis == null || joggingAxisId.HasValue)
            {
                return;
            }

            MoveDirection direction = string.Equals(directionText, "Reverse", StringComparison.OrdinalIgnoreCase)
                ? MoveDirection.反向
                : MoveDirection.正向;

            bool success = controlCard.TryStartJog(
                axis.ControllerAxisId,
                direction,
                out _,
                out string message);

            if (success)
            {
                joggingAxisId = axis.ControllerAxisId;
            }

            SetOperationResult(message);
        }

        private void StopPageJog()
        {
            if (controlCard == null || !joggingAxisId.HasValue)
            {
                return;
            }

            short axisId = joggingAxisId.Value;
            joggingAxisId = null;
            controlCard.TryStopAxis(axisId, out _, out string message);
            SetOperationResult(message);
        }

        private async Task MoveAbsoluteAsync()
        {
            var axis = SelectedAxis;
            if (controlCard == null || axis == null)
            {
                return;
            }

            if (!TryParseTargetPosition(out double targetPosition))
            {
                SetOperationResult("目标位置格式无效");
                return;
            }

            IsBusy = true;
            await deviceGate.WaitAsync();
            try
            {
                bool success = false;
                string message = string.Empty;
                await Task.Run(() => success = controlCard.TryMoveAbsolute(
                    axis.ControllerAxisId,
                    targetPosition,
                    out _,
                    out message));
                SetOperationResult(message);
            }
            finally
            {
                deviceGate.Release();
                IsBusy = false;
            }
        }

        private void StopSelectedAxis()
        {
            var axis = SelectedAxis;
            if (controlCard == null || axis == null)
            {
                return;
            }

            if (joggingAxisId == axis.ControllerAxisId)
            {
                joggingAxisId = null;
            }

            controlCard.TryStopAxis(axis.ControllerAxisId, out _, out string message);
            SetOperationResult(message);
        }

        private async Task ToggleOutputAsync(ZMotionIoDiagnosticItem? item)
        {
            if (controlCard == null || item == null || !CanToggleOutputs)
            {
                return;
            }

            IsBusy = true;
            await deviceGate.WaitAsync();
            try
            {
                bool requestedState = !item.State;
                bool writeSucceeded = false;
                bool readSucceeded = false;
                bool actualState = item.State;

                await Task.Run(() =>
                {
                    writeSucceeded = controlCard.SetSpecifiedIO(item.Port, requestedState);
                    if (writeSucceeded)
                    {
                        readSucceeded = controlCard.GetSpecifiedIO(false, item.Port, out actualState);
                    }
                });

                if (writeSucceeded && readSucceeded)
                {
                    item.State = actualState;
                    SetOperationResult($"OUT{item.Port} 已切换为 {(actualState ? "ON" : "OFF")}");
                }
                else
                {
                    SetOperationResult($"OUT{item.Port} 切换失败，已保留硬件回读状态");
                }
            }
            finally
            {
                deviceGate.Release();
                IsBusy = false;
            }
        }

        private bool TryParseTargetPosition(out double targetPosition)
        {
            return double.TryParse(TargetPositionText, NumberStyles.Float, CultureInfo.CurrentCulture, out targetPosition)
                || double.TryParse(TargetPositionText, NumberStyles.Float, CultureInfo.InvariantCulture, out targetPosition);
        }

        private static void UpdateIoStates(
            IReadOnlyList<ZMotionIoDiagnosticItem> items,
            IReadOnlyList<bool> states)
        {
            int count = Math.Min(items.Count, states.Count);
            for (int index = 0; index < count; index++)
            {
                items[index].State = states[index];
            }
        }

        private async Task UpdateRefreshErrorAsync(string message)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
            {
                return;
            }

            await dispatcher.InvokeAsync(() => SetOperationResult(message));
        }

        private void SetOperationResult(string message)
        {
            StatusMessage = message;
            StatusTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private void RaiseOperationStateChanged()
        {
            RaisePropertyChanged(nameof(CanOperateAxis));
            RaisePropertyChanged(nameof(CanStopAxis));
            RaisePropertyChanged(nameof(CanToggleOutputs));
            RaisePropertyChanged(nameof(AxisEnableButtonText));
            toggleAxisEnableCommand?.RaiseCanExecuteChanged();
            startJogCommand?.RaiseCanExecuteChanged();
            moveAbsoluteCommand?.RaiseCanExecuteChanged();
            stopAxisCommand?.RaiseCanExecuteChanged();
            toggleOutputCommand?.RaiseCanExecuteChanged();
        }
    }
}

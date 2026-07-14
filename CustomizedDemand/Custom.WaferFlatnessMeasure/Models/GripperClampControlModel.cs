using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Custom.WaferFlatnessMeasure.Models
{
    public enum GripperClampOperationMode
    {
        夹紧,
        松开,
        刷新状态
    }

    [Serializable]
    public class GripperClampChannelModel : BindableBase
    {
        private int _index;
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? $"夹爪{Index}" : value.Trim());
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _clampCommandAddress = string.Empty;
        public string ClampCommandAddress
        {
            get => _clampCommandAddress;
            set => SetProperty(ref _clampCommandAddress, NormalizeAddress(value));
        }

        private string _clampDoneAddress = string.Empty;
        public string ClampDoneAddress
        {
            get => _clampDoneAddress;
            set => SetProperty(ref _clampDoneAddress, NormalizeAddress(value));
        }

        private string _releaseCommandAddress = string.Empty;
        public string ReleaseCommandAddress
        {
            get => _releaseCommandAddress;
            set => SetProperty(ref _releaseCommandAddress, NormalizeAddress(value));
        }

        private string _releaseDoneAddress = string.Empty;
        public string ReleaseDoneAddress
        {
            get => _releaseDoneAddress;
            set => SetProperty(ref _releaseDoneAddress, NormalizeAddress(value));
        }

        private string _alarmAddress = string.Empty;
        public string AlarmAddress
        {
            get => _alarmAddress;
            set => SetProperty(ref _alarmAddress, NormalizeAddress(value));
        }

        private bool _clampCommandValue;
        [JsonIgnore]
        public bool ClampCommandValue
        {
            get => _clampCommandValue;
            set => SetProperty(ref _clampCommandValue, value);
        }

        private bool _releaseCommandValue;
        [JsonIgnore]
        public bool ReleaseCommandValue
        {
            get => _releaseCommandValue;
            set => SetProperty(ref _releaseCommandValue, value);
        }

        private bool _clampDone;
        [JsonIgnore]
        public bool ClampDone
        {
            get => _clampDone;
            set => SetProperty(ref _clampDone, value);
        }

        private bool _releaseDone;
        [JsonIgnore]
        public bool ReleaseDone
        {
            get => _releaseDone;
            set => SetProperty(ref _releaseDone, value);
        }

        private bool _alarmSignal;
        [JsonIgnore]
        public bool AlarmSignal
        {
            get => _alarmSignal;
            set => SetProperty(ref _alarmSignal, value);
        }

        public GripperClampChannelModel()
        {
        }

        public GripperClampChannelModel(int index)
        {
            Index = index;
            Name = $"夹爪{index}";
        }

        public void NormalizeConfiguredAddresses()
        {
            ClampCommandAddress = ClampCommandAddress;
            ClampDoneAddress = ClampDoneAddress;
            ReleaseCommandAddress = ReleaseCommandAddress;
            ReleaseDoneAddress = ReleaseDoneAddress;
            AlarmAddress = AlarmAddress;
        }

        private static string NormalizeAddress(string? address)
        {
            return string.IsNullOrWhiteSpace(address) ? string.Empty : address.Trim();
        }
    }

    [Serializable]
    public class GripperClampControlModel : ModelParamBase
    {
        private const int GripperCount = 4;

        [JsonIgnore]
        private PLCBase? _curPLC;

        private string _plcDisplayName = "未选择PLC";
        [JsonIgnore]
        public string PlcDisplayName
        {
            get => _plcDisplayName;
            private set => SetProperty(ref _plcDisplayName, value);
        }

        private bool _isPlcConnected;
        [JsonIgnore]
        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            private set => SetProperty(ref _isPlcConnected, value);
        }

        private ObservableCollection<GripperClampChannelModel> _grippers = CreateDefaultGrippers();
        public ObservableCollection<GripperClampChannelModel> Grippers
        {
            get => _grippers;
            set
            {
                ObservableCollection<GripperClampChannelModel> next = value ?? CreateDefaultGrippers();
                if (SetProperty(ref _grippers, next))
                {
                    EnsureGripperDefinitions();
                    RaiseStatusPropertiesChanged();
                }
            }
        }

        private bool _isPulseCommand = true;
        public bool IsPulseCommand
        {
            get => _isPulseCommand;
            set => SetProperty(ref _isPulseCommand, value);
        }

        private bool _waitForDoneSignal = true;
        public bool WaitForDoneSignal
        {
            get => _waitForDoneSignal;
            set => SetProperty(ref _waitForDoneSignal, value);
        }

        private bool _resetOppositeCommand = true;
        public bool ResetOppositeCommand
        {
            get => _resetOppositeCommand;
            set => SetProperty(ref _resetOppositeCommand, value);
        }

        private int _commandPulseResetDelayMs = 200;
        public int CommandPulseResetDelayMs
        {
            get => _commandPulseResetDelayMs;
            set => SetProperty(ref _commandPulseResetDelayMs, Math.Clamp(value, 0, 60000));
        }

        private int _operationTimeoutMs = 5000;
        public int OperationTimeoutMs
        {
            get => _operationTimeoutMs;
            set => SetProperty(ref _operationTimeoutMs, Math.Clamp(value, 100, 600000));
        }

        private int _pollIntervalMs = 100;
        public int PollIntervalMs
        {
            get => _pollIntervalMs;
            set => SetProperty(ref _pollIntervalMs, Math.Clamp(value, 20, 10000));
        }

        private GripperClampOperationMode _operationMode = GripperClampOperationMode.夹紧;
        public GripperClampOperationMode OperationMode
        {
            get => _operationMode;
            set => SetProperty(ref _operationMode, value);
        }

        private bool _isOperationInProgress;
        [JsonIgnore]
        public bool IsOperationInProgress
        {
            get => _isOperationInProgress;
            private set
            {
                if (SetProperty(ref _isOperationInProgress, value))
                {
                    RaisePropertyChanged(nameof(IsOperationIdle));
                }
            }
        }

        [JsonIgnore]
        public bool IsOperationIdle => !IsOperationInProgress;

        [JsonIgnore]
        public bool ClampCommandValue => GetActiveGrippers().Any(item => item.ClampCommandValue);

        [JsonIgnore]
        public bool ReleaseCommandValue => GetActiveGrippers().Any(item => item.ReleaseCommandValue);

        [JsonIgnore]
        [OutputParam("ClampDone", "四个夹爪全部夹紧到位")]
        public bool ClampDone => AreAllActiveGrippers(item => item.ClampDone);

        [JsonIgnore]
        [OutputParam("ReleaseDone", "四个夹爪全部松开到位")]
        public bool ReleaseDone => AreAllActiveGrippers(item => item.ReleaseDone);

        [JsonIgnore]
        [OutputParam("AlarmSignal", "任一夹爪报警信号")]
        public bool AlarmSignal => GetActiveGrippers().Any(item => item.AlarmSignal);

        [JsonIgnore]
        [OutputParam("ClampDone1", "夹爪1夹紧到位")]
        public bool ClampDone1 => GetGripperState(1, item => item.ClampDone);

        [JsonIgnore]
        [OutputParam("ClampDone2", "夹爪2夹紧到位")]
        public bool ClampDone2 => GetGripperState(2, item => item.ClampDone);

        [JsonIgnore]
        [OutputParam("ClampDone3", "夹爪3夹紧到位")]
        public bool ClampDone3 => GetGripperState(3, item => item.ClampDone);

        [JsonIgnore]
        [OutputParam("ClampDone4", "夹爪4夹紧到位")]
        public bool ClampDone4 => GetGripperState(4, item => item.ClampDone);

        [JsonIgnore]
        [OutputParam("ReleaseDone1", "夹爪1松开到位")]
        public bool ReleaseDone1 => GetGripperState(1, item => item.ReleaseDone);

        [JsonIgnore]
        [OutputParam("ReleaseDone2", "夹爪2松开到位")]
        public bool ReleaseDone2 => GetGripperState(2, item => item.ReleaseDone);

        [JsonIgnore]
        [OutputParam("ReleaseDone3", "夹爪3松开到位")]
        public bool ReleaseDone3 => GetGripperState(3, item => item.ReleaseDone);

        [JsonIgnore]
        [OutputParam("ReleaseDone4", "夹爪4松开到位")]
        public bool ReleaseDone4 => GetGripperState(4, item => item.ReleaseDone);

        [JsonIgnore]
        [OutputParam("AlarmSignal1", "夹爪1报警信号")]
        public bool AlarmSignal1 => GetGripperState(1, item => item.AlarmSignal);

        [JsonIgnore]
        [OutputParam("AlarmSignal2", "夹爪2报警信号")]
        public bool AlarmSignal2 => GetGripperState(2, item => item.AlarmSignal);

        [JsonIgnore]
        [OutputParam("AlarmSignal3", "夹爪3报警信号")]
        public bool AlarmSignal3 => GetGripperState(3, item => item.AlarmSignal);

        [JsonIgnore]
        [OutputParam("AlarmSignal4", "夹爪4报警信号")]
        public bool AlarmSignal4 => GetGripperState(4, item => item.AlarmSignal);

        private bool _lastOperationSuccess;
        [JsonIgnore]
        [OutputParam("LastOperationSuccess", "最近一次夹爪操作成功")]
        public bool LastOperationSuccess
        {
            get => _lastOperationSuccess;
            private set => SetOutputProperty(ref _lastOperationSuccess, value, nameof(LastOperationSuccess));
        }

        private string _lastOperationMessage = "等待操作";
        [JsonIgnore]
        [OutputParam("LastOperationMessage", "最近一次夹爪操作信息")]
        public string LastOperationMessage
        {
            get => _lastOperationMessage;
            private set => SetOutputProperty(ref _lastOperationMessage, value, nameof(LastOperationMessage));
        }

        private DateTime _lastOperationTime;
        [JsonIgnore]
        public DateTime LastOperationTime
        {
            get => _lastOperationTime;
            private set
            {
                if (SetProperty(ref _lastOperationTime, value))
                {
                    RaisePropertyChanged(nameof(LastOperationTimeText));
                    RefreshOutputParamValue(nameof(LastOperationTimeText), LastOperationTimeText);
                }
            }
        }

        [JsonIgnore]
        [OutputParam("LastOperationTimeText", "最近一次夹爪操作时间")]
        public string LastOperationTimeText =>
            LastOperationTime == default ? "--" : LastOperationTime.ToString("yyyy-MM-dd HH:mm:ss");

        public GripperClampControlModel()
        {
            Name = "夹爪夹紧";
            EnsureGripperDefinitions();
        }

        public override bool LoadKeyParam()
        {
            try
            {
                base.LoadKeyParam();
                EnsureGripperDefinitions();
                CommandPulseResetDelayMs = CommandPulseResetDelayMs;
                OperationTimeoutMs = OperationTimeoutMs;
                PollIntervalMs = PollIntervalMs;
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
        }

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

            TriggerModuleRun ??= () => ExecuteModule().Result;
            RefreshPlcReference();
            IsOnceInit = true;
            return true;
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            DateTime startTime = DateTime.Now;
            NodeStatus status = NodeStatus.Error;

            try
            {
                LoadKeyParam();
                bool success = OperationMode switch
                {
                    GripperClampOperationMode.夹紧 => await ClampAsync(),
                    GripperClampOperationMode.松开 => await ReleaseAsync(),
                    GripperClampOperationMode.刷新状态 => await RefreshStatusAsync(),
                    _ => false
                };

                status = success ? NodeStatus.Success : NodeStatus.Error;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                LastOperationSuccess = false;
                LastOperationMessage = ex.Message;
            }

            return Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = (DateTime.Now - startTime).TotalMilliseconds
            };
        }

        public Task<bool> ClampAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteMotionAsync(
                "夹紧",
                gripper => gripper.ClampCommandAddress,
                gripper => gripper.ClampDoneAddress,
                gripper => gripper.ReleaseCommandAddress,
                (gripper, value) => gripper.ClampCommandValue = value,
                (gripper, value) => gripper.ClampDone = value,
                cancellationToken);
        }

        public Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteMotionAsync(
                "松开",
                gripper => gripper.ReleaseCommandAddress,
                gripper => gripper.ReleaseDoneAddress,
                gripper => gripper.ClampCommandAddress,
                (gripper, value) => gripper.ReleaseCommandValue = value,
                (gripper, value) => gripper.ReleaseDone = value,
                cancellationToken);
        }

        public async Task<bool> RefreshStatusAsync(CancellationToken cancellationToken = default)
        {
            if (!TryResolvePlc(out _))
            {
                return false;
            }

            EnsureGripperDefinitions();

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (GripperClampChannelModel gripper in Grippers)
                {
                    RefreshGripperStatus(gripper);
                }
            }, cancellationToken);

            RaiseStatusPropertiesChanged();
            LastOperationSuccess = true;
            LastOperationMessage = "四个夹爪PLC状态刷新完成";
            LastOperationTime = DateTime.Now;
            RefreshOutputParamValues();
            return true;
        }

        public async Task<bool> ResetCommandsAsync(CancellationToken cancellationToken = default)
        {
            if (!TryResolvePlc(out _))
            {
                return false;
            }

            EnsureGripperDefinitions();

            bool success = true;
            foreach (GripperClampChannelModel gripper in Grippers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(gripper.ClampCommandAddress))
                {
                    success &= WriteBool(gripper.ClampCommandAddress, false);
                    gripper.ClampCommandValue = false;
                }

                if (!string.IsNullOrWhiteSpace(gripper.ReleaseCommandAddress))
                {
                    success &= WriteBool(gripper.ReleaseCommandAddress, false);
                    gripper.ReleaseCommandValue = false;
                }
            }

            await RefreshStatusAsync(cancellationToken);
            LastOperationSuccess = success;
            LastOperationMessage = success ? "四个夹爪命令已复位" : "夹爪命令复位失败，请检查PLC日志";
            LastOperationTime = DateTime.Now;
            RaiseStatusPropertiesChanged();
            RefreshOutputParamValues();
            return success;
        }

        public bool RefreshPlcReference()
        {
            if (!TryGetPlcSetModel(out PLCSetModel? plcSetModel) ||
                plcSetModel == null ||
                plcSetModel.Models == null ||
                plcSetModel.Models.Count == 0)
            {
                _curPLC = null;
                PlcDisplayName = "未配置PLC";
                IsPlcConnected = false;
                return false;
            }

            _curPLC = plcSetModel.CurSlt ?? plcSetModel.Models.FirstOrDefault();
            PlcDisplayName = _curPLC?.Config?.DisplayName ?? "未选择PLC";
            IsPlcConnected = _curPLC?.Config?.IsConnected == true;
            return _curPLC != null;
        }

        private async Task<bool> ExecuteMotionAsync(
            string actionName,
            Func<GripperClampChannelModel, string> commandAddressSelector,
            Func<GripperClampChannelModel, string> doneAddressSelector,
            Func<GripperClampChannelModel, string> oppositeCommandAddressSelector,
            Action<GripperClampChannelModel, bool> commandValueSetter,
            Action<GripperClampChannelModel, bool> doneValueSetter,
            CancellationToken cancellationToken)
        {
            if (IsOperationInProgress)
            {
                LastOperationMessage = "已有夹爪操作正在执行";
                return false;
            }

            if (!TryResolvePlc(out _))
            {
                return false;
            }

            IReadOnlyList<GripperClampChannelModel> activeGrippers = GetActiveGrippers();
            if (activeGrippers.Count == 0)
            {
                LastOperationSuccess = false;
                LastOperationMessage = "未启用夹爪";
                return false;
            }

            if (!ValidateOperationConfig(actionName, activeGrippers, commandAddressSelector, doneAddressSelector))
            {
                return false;
            }

            IsOperationInProgress = true;
            LastOperationSuccess = false;
            LastOperationMessage = $"开始同时执行{activeGrippers.Count}个夹爪{actionName}";

            var writtenCommands = new List<(GripperClampChannelModel Gripper, string Address)>();

            try
            {
                if (ResetOppositeCommand)
                {
                    List<string> resetFailures = ResetOppositeCommands(activeGrippers, oppositeCommandAddressSelector);
                    if (resetFailures.Count > 0)
                    {
                        LastOperationMessage = $"复位反向命令失败：{string.Join("；", resetFailures)}";
                        return false;
                    }
                }

                List<string> writeFailures = WriteMotionCommands(
                    activeGrippers,
                    commandAddressSelector,
                    commandValueSetter,
                    writtenCommands);

                if (writeFailures.Count > 0)
                {
                    ResetWrittenCommands(writtenCommands, commandValueSetter);
                    LastOperationMessage = $"写入{actionName}命令失败：{string.Join("；", writeFailures)}";
                    return false;
                }

                LastOperationMessage = $"已向{activeGrippers.Count}个夹爪写入{actionName}命令";

                if (IsPulseCommand)
                {
                    await Task.Delay(CommandPulseResetDelayMs, cancellationToken);
                    List<string> pulseResetFailures = ResetWrittenCommands(writtenCommands, commandValueSetter);
                    if (pulseResetFailures.Count > 0)
                    {
                        LastOperationMessage = $"复位{actionName}命令失败：{string.Join("；", pulseResetFailures)}";
                        return false;
                    }
                }

                RaiseStatusPropertiesChanged();

                if (WaitForDoneSignal)
                {
                    bool isDone = await WaitForSignalsAsync(
                        actionName,
                        activeGrippers,
                        doneAddressSelector,
                        doneValueSetter,
                        cancellationToken);

                    if (!isDone)
                    {
                        return false;
                    }
                }

                await RefreshStatusAsync(cancellationToken);
                LastOperationSuccess = true;
                LastOperationMessage = $"{activeGrippers.Count}个夹爪{actionName}完成";
                LastOperationTime = DateTime.Now;
                Logs.LogInfo($"{activeGrippers.Count}个夹爪{actionName}完成");
                return true;
            }
            catch (OperationCanceledException)
            {
                LastOperationMessage = $"{activeGrippers.Count}个夹爪{actionName}操作已取消";
                return false;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                LastOperationMessage = $"{activeGrippers.Count}个夹爪{actionName}异常：{ex.Message}";
                return false;
            }
            finally
            {
                IsOperationInProgress = false;
                RaiseStatusPropertiesChanged();
                RefreshOutputParamValues();
            }
        }

        private bool ValidateOperationConfig(
            string actionName,
            IReadOnlyList<GripperClampChannelModel> activeGrippers,
            Func<GripperClampChannelModel, string> commandAddressSelector,
            Func<GripperClampChannelModel, string> doneAddressSelector)
        {
            List<string> missingCommand = activeGrippers
                .Where(gripper => string.IsNullOrWhiteSpace(commandAddressSelector(gripper)))
                .Select(gripper => gripper.Name)
                .ToList();

            if (missingCommand.Count > 0)
            {
                LastOperationSuccess = false;
                LastOperationMessage = $"{string.Join("、", missingCommand)}的{actionName}命令地址未配置";
                return false;
            }

            if (WaitForDoneSignal)
            {
                List<string> missingDone = activeGrippers
                    .Where(gripper => string.IsNullOrWhiteSpace(doneAddressSelector(gripper)))
                    .Select(gripper => gripper.Name)
                    .ToList();

                if (missingDone.Count > 0)
                {
                    LastOperationSuccess = false;
                    LastOperationMessage = $"{string.Join("、", missingDone)}的{actionName}到位反馈地址未配置";
                    return false;
                }
            }

            return true;
        }

        private List<string> ResetOppositeCommands(
            IReadOnlyList<GripperClampChannelModel> activeGrippers,
            Func<GripperClampChannelModel, string> oppositeCommandAddressSelector)
        {
            var failures = new List<string>();
            foreach (GripperClampChannelModel gripper in activeGrippers)
            {
                string address = oppositeCommandAddressSelector(gripper);
                if (string.IsNullOrWhiteSpace(address))
                {
                    continue;
                }

                if (!WriteBool(address, false))
                {
                    failures.Add($"{gripper.Name}:{address}");
                }
            }

            return failures;
        }

        private List<string> WriteMotionCommands(
            IReadOnlyList<GripperClampChannelModel> activeGrippers,
            Func<GripperClampChannelModel, string> commandAddressSelector,
            Action<GripperClampChannelModel, bool> commandValueSetter,
            List<(GripperClampChannelModel Gripper, string Address)> writtenCommands)
        {
            var failures = new List<string>();
            foreach (GripperClampChannelModel gripper in activeGrippers)
            {
                string address = commandAddressSelector(gripper);
                if (WriteBool(address, true))
                {
                    commandValueSetter(gripper, true);
                    writtenCommands.Add((gripper, address));
                }
                else
                {
                    failures.Add($"{gripper.Name}:{address}");
                }
            }

            return failures;
        }

        private List<string> ResetWrittenCommands(
            IReadOnlyList<(GripperClampChannelModel Gripper, string Address)> writtenCommands,
            Action<GripperClampChannelModel, bool> commandValueSetter)
        {
            var failures = new List<string>();
            foreach ((GripperClampChannelModel gripper, string address) in writtenCommands)
            {
                if (WriteBool(address, false))
                {
                    commandValueSetter(gripper, false);
                }
                else
                {
                    failures.Add($"{gripper.Name}:{address}");
                }
            }

            return failures;
        }

        private async Task<bool> WaitForSignalsAsync(
            string actionName,
            IReadOnlyList<GripperClampChannelModel> activeGrippers,
            Func<GripperClampChannelModel, string> doneAddressSelector,
            Action<GripperClampChannelModel, bool> doneValueSetter,
            CancellationToken cancellationToken)
        {
            DateTime startTime = DateTime.Now;
            HashSet<GripperClampChannelModel> pendingGrippers = activeGrippers.ToHashSet();

            while ((DateTime.Now - startTime).TotalMilliseconds <= OperationTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (GripperClampChannelModel gripper in pendingGrippers.ToList())
                {
                    ReadAlarmSignal(gripper);

                    string doneAddress = doneAddressSelector(gripper);
                    if (TryReadBool(doneAddress, out bool isDone))
                    {
                        doneValueSetter(gripper, isDone);
                        if (isDone)
                        {
                            pendingGrippers.Remove(gripper);
                        }
                    }
                }

                RaiseStatusPropertiesChanged();

                if (pendingGrippers.Count == 0)
                {
                    return true;
                }

                await Task.Delay(PollIntervalMs, cancellationToken);
            }

            string pendingNames = string.Join("、", pendingGrippers.Select(gripper => gripper.Name));
            LastOperationMessage = $"等待夹爪{actionName}到位超时，未到位：{pendingNames}";
            Logs.LogWarning(LastOperationMessage);
            return false;
        }

        private void RefreshGripperStatus(GripperClampChannelModel gripper)
        {
            gripper.ClampCommandValue = TryReadBool(gripper.ClampCommandAddress, out bool clampCommand) && clampCommand;
            gripper.ReleaseCommandValue = TryReadBool(gripper.ReleaseCommandAddress, out bool releaseCommand) && releaseCommand;
            gripper.ClampDone = TryReadBool(gripper.ClampDoneAddress, out bool clampDone) && clampDone;
            gripper.ReleaseDone = TryReadBool(gripper.ReleaseDoneAddress, out bool releaseDone) && releaseDone;
            ReadAlarmSignal(gripper);
        }

        private void ReadAlarmSignal(GripperClampChannelModel gripper)
        {
            gripper.AlarmSignal = TryReadBool(gripper.AlarmAddress, out bool alarmSignal) && alarmSignal;
        }

        private bool TryResolvePlc(out PLCBase? plc)
        {
            RefreshPlcReference();
            plc = _curPLC;

            if (plc == null)
            {
                LastOperationMessage = "未找到PLC配置";
                LastOperationSuccess = false;
                return false;
            }

            if (plc.Config?.IsConnected != true)
            {
                LastOperationMessage = $"PLC未连接：{plc.Config?.DisplayName ?? "未知PLC"}";
                LastOperationSuccess = false;
                return false;
            }

            return true;
        }

        private bool WriteBool(string address, bool value)
        {
            if (_curPLC == null || string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            var param = new PLCParaInfoModel
            {
                PLCAddress = NormalizeAddress(address),
                ParaType = EnumParaInfoModelParaType.Bool,
                ParaValue = value
            };

            bool success = _curPLC.WritePLCPara(param);
            if (!success)
            {
                Logs.LogError($"夹爪PLC写入失败，地址：{param.PLCAddress}，值：{value}");
            }

            return success;
        }

        private bool TryReadBool(string address, out bool value)
        {
            value = false;
            if (_curPLC == null || string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            var param = new PLCParaInfoModel
            {
                PLCAddress = NormalizeAddress(address),
                ParaType = EnumParaInfoModelParaType.Bool
            };

            if (!_curPLC.ReadPLCPara(param) || param.ParaValue == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToBoolean(param.ParaValue);
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"夹爪PLC读取值转换失败，地址：{param.PLCAddress}，值：{param.ParaValue}，异常：{ex.Message}");
                return false;
            }
        }

        private static bool TryGetPlcSetModel(out PLCSetModel? plcSetModel)
        {
            plcSetModel = null;
            var modules = PrismProvider.HardwareModuleManager?.Modules;
            if (modules == null || !modules.TryGetValue(ConfigKey.PLCConfig, out IHardwareModule? module))
            {
                return false;
            }

            plcSetModel = module as PLCSetModel;
            return plcSetModel != null;
        }

        private IReadOnlyList<GripperClampChannelModel> GetActiveGrippers()
        {
            EnsureGripperDefinitions();
            return Grippers
                .Where(gripper => gripper.IsEnabled)
                .Take(GripperCount)
                .ToList();
        }

        private bool AreAllActiveGrippers(Func<GripperClampChannelModel, bool> predicate)
        {
            IReadOnlyList<GripperClampChannelModel> activeGrippers = GetActiveGrippers();
            return activeGrippers.Count > 0 && activeGrippers.All(predicate);
        }

        private bool GetGripperState(int index, Func<GripperClampChannelModel, bool> selector)
        {
            EnsureGripperDefinitions();
            GripperClampChannelModel? gripper = Grippers.FirstOrDefault(item => item.Index == index);
            return gripper != null && selector(gripper);
        }

        private void EnsureGripperDefinitions()
        {
            _grippers ??= CreateDefaultGrippers();

            while (_grippers.Count < GripperCount)
            {
                _grippers.Add(new GripperClampChannelModel(_grippers.Count + 1));
            }

            while (_grippers.Count > GripperCount)
            {
                _grippers.RemoveAt(_grippers.Count - 1);
            }

            for (int i = 0; i < _grippers.Count; i++)
            {
                GripperClampChannelModel gripper = _grippers[i];
                gripper.Index = i + 1;
                if (string.IsNullOrWhiteSpace(gripper.Name))
                {
                    gripper.Name = $"夹爪{i + 1}";
                }

                gripper.NormalizeConfiguredAddresses();
            }
        }

        private static ObservableCollection<GripperClampChannelModel> CreateDefaultGrippers()
        {
            return new ObservableCollection<GripperClampChannelModel>(
                Enumerable.Range(1, GripperCount).Select(index => new GripperClampChannelModel(index)));
        }

        private static string NormalizeAddress(string? address)
        {
            return string.IsNullOrWhiteSpace(address) ? string.Empty : address.Trim();
        }

        private void RaiseStatusPropertiesChanged()
        {
            RaisePropertyChanged(nameof(ClampCommandValue));
            RaisePropertyChanged(nameof(ReleaseCommandValue));
            RaisePropertyChanged(nameof(ClampDone));
            RaisePropertyChanged(nameof(ReleaseDone));
            RaisePropertyChanged(nameof(AlarmSignal));
            RaisePropertyChanged(nameof(ClampDone1));
            RaisePropertyChanged(nameof(ClampDone2));
            RaisePropertyChanged(nameof(ClampDone3));
            RaisePropertyChanged(nameof(ClampDone4));
            RaisePropertyChanged(nameof(ReleaseDone1));
            RaisePropertyChanged(nameof(ReleaseDone2));
            RaisePropertyChanged(nameof(ReleaseDone3));
            RaisePropertyChanged(nameof(ReleaseDone4));
            RaisePropertyChanged(nameof(AlarmSignal1));
            RaisePropertyChanged(nameof(AlarmSignal2));
            RaisePropertyChanged(nameof(AlarmSignal3));
            RaisePropertyChanged(nameof(AlarmSignal4));
        }

        private void SetOutputProperty<T>(ref T storage, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return;
            }

            storage = value;
            RaisePropertyChanged(propertyName);
            RefreshOutputParamValue(propertyName, value);
        }

        private void RefreshOutputParamValues()
        {
            if (OutputParams == null || OutputParams.Count == 0)
            {
                return;
            }

            Dictionary<string, object> dataPointValues = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (dataPointValues.TryGetValue(item.ParamName, out object? value))
                {
                    item.Value = value;
                }
            }
        }

        private void RefreshOutputParamValue(string propertyName, object? value)
        {
            if (OutputParams == null || OutputParams.Count == 0)
            {
                return;
            }

            foreach (var item in OutputParams.Where(item => item.ParamName == propertyName))
            {
                item.Value = value;
            }
        }
    }
}

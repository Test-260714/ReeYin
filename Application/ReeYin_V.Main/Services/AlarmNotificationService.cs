#nullable enable
using HandyControl.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events.Alarm;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.User;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace ReeYin_V.Main.Services
{
    /// <summary>
    /// Listens for alarm events globally so notifications are not tied to the Alarm Center page.
    /// </summary>
    [ExposedService(Lifetime.Singleton, 8, AutoInitialize = true)]
    public sealed class AlarmNotificationService : BindableBase, IDisposable
    {
        public const string GlobalGrowlToken = "AlarmGlobalGrowl";

        private static readonly HashSet<string> HardwareSafetyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HardwareAlarmCodes.SafetyError,
            HardwareAlarmCodes.MotionLimitTriggered,
            HardwareAlarmCodes.MotionServoAlarm,
            HardwareAlarmCodes.MotionSafetyError
        };

        private static readonly string[] HardwareSafetyKeywords =
        {
            "急停",
            "安全",
            "联锁",
            "限位",
            "伺服",
            "Safety",
            "EStop",
            "Emergency",
            "Limit",
            "Servo",
            "Interlock"
        };

        private readonly IEventAggregator _eventAggregator;
        private readonly IAlarmService _alarmService;
        private readonly IUserService _userService;
        private readonly Dictionary<string, DateTime> _lastNotificationAtByKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<AlarmRealtimeEntry> _criticalOverlayQueue = new Queue<AlarmRealtimeEntry>();
        private readonly object _gate = new object();
        private SubscriptionToken? _subscriptionToken;
        private AlarmRealtimeEntry? _currentCriticalOverlayEntry;
        private bool _disposed;
        private bool _isCriticalOverlayVisible;
        private bool _criticalOverlayIsSafetyStyle;
        private string _criticalOverlayHeader = string.Empty;
        private string _criticalOverlayTitle = string.Empty;
        private string _criticalOverlayCode = string.Empty;
        private string _criticalOverlayName = string.Empty;
        private string _criticalOverlaySourceLocation = string.Empty;
        private string _criticalOverlayOccurrenceText = string.Empty;
        private string _criticalOverlayMessage = string.Empty;
        private string _criticalOverlayActionText = "确认报警";

        public AlarmNotificationService(
            IEventAggregator eventAggregator,
            IAlarmService alarmService,
            IUserService userService)
        {
            _eventAggregator = eventAggregator;
            _alarmService = alarmService;
            _userService = userService;
            ConfirmCriticalAlarmCommand = new DelegateCommand(ConfirmCriticalAlarm, CanConfirmCriticalAlarm);
            _subscriptionToken = _eventAggregator.GetEvent<AlarmRealtimeEvent>().Subscribe(OnAlarmRealtimeEvent);
        }

        public DelegateCommand ConfirmCriticalAlarmCommand { get; }

        public bool IsCriticalOverlayVisible
        {
            get => _isCriticalOverlayVisible;
            private set
            {
                if (SetProperty(ref _isCriticalOverlayVisible, value))
                {
                    ConfirmCriticalAlarmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CriticalOverlayIsSafetyStyle
        {
            get => _criticalOverlayIsSafetyStyle;
            private set => SetProperty(ref _criticalOverlayIsSafetyStyle, value);
        }

        public string CriticalOverlayHeader
        {
            get => _criticalOverlayHeader;
            private set => SetProperty(ref _criticalOverlayHeader, value);
        }

        public string CriticalOverlayTitle
        {
            get => _criticalOverlayTitle;
            private set => SetProperty(ref _criticalOverlayTitle, value);
        }

        public string CriticalOverlayCode
        {
            get => _criticalOverlayCode;
            private set => SetProperty(ref _criticalOverlayCode, value);
        }

        public string CriticalOverlayName
        {
            get => _criticalOverlayName;
            private set => SetProperty(ref _criticalOverlayName, value);
        }

        public string CriticalOverlaySourceLocation
        {
            get => _criticalOverlaySourceLocation;
            private set => SetProperty(ref _criticalOverlaySourceLocation, value);
        }

        public string CriticalOverlayOccurrenceText
        {
            get => _criticalOverlayOccurrenceText;
            private set => SetProperty(ref _criticalOverlayOccurrenceText, value);
        }

        public string CriticalOverlayMessage
        {
            get => _criticalOverlayMessage;
            private set => SetProperty(ref _criticalOverlayMessage, value);
        }

        public string CriticalOverlayActionText
        {
            get => _criticalOverlayActionText;
            private set => SetProperty(ref _criticalOverlayActionText, value);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_subscriptionToken != null)
            {
                _eventAggregator.GetEvent<AlarmRealtimeEvent>().Unsubscribe(_subscriptionToken);
                _subscriptionToken = null;
            }

            _disposed = true;
        }

        private void OnAlarmRealtimeEvent(AlarmRealtimeEntry entry)
        {
            if (entry == null || _disposed)
            {
                return;
            }

            if (ShouldShowCriticalOverlay(entry))
            {
                if (ShouldThrottleNotification(entry, AlarmPopupMode.Modal))
                {
                    return;
                }

                RunOnUiThread(() => EnqueueCriticalOverlay(entry));
                return;
            }

            AlarmPopupMode popupMode = ResolveEffectivePopupMode(entry);
            if (popupMode == AlarmPopupMode.None || ShouldThrottleNotification(entry, popupMode))
            {
                return;
            }

            RunOnUiThread(() =>
            {
                if (_disposed)
                {
                    return;
                }

                if (popupMode == AlarmPopupMode.Modal)
                {
                    ShowModalNotification(entry);
                    return;
                }

                ShowGrowlNotification(entry);
            });
        }

        private void EnqueueCriticalOverlay(AlarmRealtimeEntry entry)
        {
            if (_disposed)
            {
                return;
            }

            _criticalOverlayQueue.Enqueue(entry);
            if (!IsCriticalOverlayVisible)
            {
                ShowNextCriticalOverlay();
            }
        }

        private void ShowNextCriticalOverlay()
        {
            if (_criticalOverlayQueue.Count == 0)
            {
                ClearCriticalOverlay();
                return;
            }

            AlarmRealtimeEntry entry = _criticalOverlayQueue.Dequeue();
            bool isSafetyStyle = IsHardwareSafetyAlarm(entry);
            _currentCriticalOverlayEntry = entry;

            CriticalOverlayIsSafetyStyle = isSafetyStyle;
            CriticalOverlayHeader = isSafetyStyle ? "CRITICAL STOP" : "FATAL ALARM";
            CriticalOverlayTitle = isSafetyStyle ? "严重报警" : "致命报警";
            CriticalOverlayCode = string.IsNullOrWhiteSpace(entry.Code) ? "UNKNOWN" : entry.Code;
            CriticalOverlayName = string.IsNullOrWhiteSpace(entry.Name) ? CriticalOverlayCode : entry.Name;
            CriticalOverlaySourceLocation = BuildSourceLocation(entry);
            CriticalOverlayOccurrenceText = $"触发次数：{Math.Max(1, entry.OccurrenceCount)}";
            CriticalOverlayMessage = string.IsNullOrWhiteSpace(entry.Message)
                ? "设备已进入保护状态，请立即检查现场并确认报警。"
                : entry.Message;
            CriticalOverlayActionText = entry.NeedAcknowledge ? "确认报警" : "关闭提示";
            IsCriticalOverlayVisible = true;
        }

        private void ClearCriticalOverlay()
        {
            _currentCriticalOverlayEntry = null;
            IsCriticalOverlayVisible = false;
            CriticalOverlayIsSafetyStyle = false;
            CriticalOverlayHeader = string.Empty;
            CriticalOverlayTitle = string.Empty;
            CriticalOverlayCode = string.Empty;
            CriticalOverlayName = string.Empty;
            CriticalOverlaySourceLocation = string.Empty;
            CriticalOverlayOccurrenceText = string.Empty;
            CriticalOverlayMessage = string.Empty;
            CriticalOverlayActionText = "确认报警";
        }

        private bool CanConfirmCriticalAlarm()
        {
            return IsCriticalOverlayVisible;
        }

        private void ConfirmCriticalAlarm()
        {
            AlarmRealtimeEntry? entry = _currentCriticalOverlayEntry;
            if (entry != null &&
                entry.NeedAcknowledge &&
                !entry.IsConfirmed &&
                !string.IsNullOrWhiteSpace(entry.ActiveId))
            {
                try
                {
                    _alarmService.ConfirmAlarm(entry.ActiveId, ResolveCurrentUser(), "Critical overlay confirmed.");
                }
                catch
                {
                    Growl.Warning("报警确认失败，请进入报警中心手动确认。", GlobalGrowlToken);
                }
            }

            if (_criticalOverlayQueue.Count > 0)
            {
                ShowNextCriticalOverlay();
                return;
            }

            ClearCriticalOverlay();
        }

        private void ShowGrowlNotification(AlarmRealtimeEntry entry)
        {
            string message = BuildNotificationMessage(entry);
            switch (entry.Severity)
            {
                case AlarmSeverity.Fatal:
                    Growl.Fatal(message, GlobalGrowlToken);
                    break;
                case AlarmSeverity.Error:
                    Growl.Error(message, GlobalGrowlToken);
                    break;
                case AlarmSeverity.Warning:
                    Growl.Warning(message, GlobalGrowlToken);
                    break;
                default:
                    Growl.Info(message, GlobalGrowlToken);
                    break;
            }
        }

        private void ShowModalNotification(AlarmRealtimeEntry entry)
        {
            MessageBoxImage icon = entry.Severity >= AlarmSeverity.Fatal
                ? MessageBoxImage.Error
                : MessageBoxImage.Warning;
            string message = BuildModalNotificationMessage(entry);

            MessageBoxResult result = HandyControl.Controls.MessageBox.Show(
                message,
                "报警确认",
                MessageBoxButton.OK,
                icon);

            if (result != MessageBoxResult.OK ||
                !entry.NeedAcknowledge ||
                entry.IsConfirmed ||
                string.IsNullOrWhiteSpace(entry.ActiveId))
            {
                return;
            }

            try
            {
                _alarmService.ConfirmAlarm(entry.ActiveId, ResolveCurrentUser(), "Popup confirmed.");
            }
            catch
            {
                Growl.Warning("报警弹窗确认失败，请进入报警中心手动确认。", GlobalGrowlToken);
            }
        }

        private bool ShouldThrottleNotification(AlarmRealtimeEntry entry, AlarmPopupMode popupMode)
        {
            int throttleSeconds = Math.Max(0, entry.PopupThrottleSeconds);
            if (throttleSeconds == 0)
            {
                return false;
            }

            string key = $"{popupMode}|{entry.Source}|{entry.Location}|{entry.Code}";
            DateTime now = DateTime.Now;
            lock (_gate)
            {
                if (_lastNotificationAtByKey.TryGetValue(key, out DateTime last) &&
                    (now - last).TotalSeconds < throttleSeconds)
                {
                    return true;
                }

                _lastNotificationAtByKey[key] = now;
            }

            return false;
        }

        private static bool ShouldShowCriticalOverlay(AlarmRealtimeEntry entry)
        {
            return (entry.EventKind == AlarmEventKind.Raised || entry.EventKind == AlarmEventKind.Repeated) &&
                   entry.Severity >= AlarmSeverity.Fatal;
        }

        private static AlarmPopupMode ResolveEffectivePopupMode(AlarmRealtimeEntry entry)
        {
            if (entry.EventKind != AlarmEventKind.Raised && entry.EventKind != AlarmEventKind.Repeated)
            {
                return AlarmPopupMode.None;
            }

            if (entry.Severity >= AlarmSeverity.Fatal || entry.NeedAcknowledge)
            {
                return AlarmPopupMode.Modal;
            }

            if (entry.PopupMode == AlarmPopupMode.Modal)
            {
                return AlarmPopupMode.Modal;
            }

            if (entry.PopupMode == AlarmPopupMode.Growl || entry.Severity >= AlarmSeverity.Warning)
            {
                return AlarmPopupMode.Growl;
            }

            return AlarmPopupMode.None;
        }

        private static bool IsHardwareSafetyAlarm(AlarmRealtimeEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Code) && HardwareSafetyCodes.Contains(entry.Code.Trim()))
            {
                return true;
            }

            string searchableText = string.Join(
                " ",
                entry.Code,
                entry.Name,
                entry.Message,
                entry.Source,
                entry.Location);

            foreach (string keyword in HardwareSafetyKeywords)
            {
                if (searchableText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildNotificationMessage(AlarmRealtimeEntry entry)
        {
            string title = string.IsNullOrWhiteSpace(entry.Name) ? entry.Code : entry.Name;
            return $"{GetSeverityText(entry.Severity)} - {title}\n{BuildSourceLocation(entry)}\n{entry.Message}";
        }

        private static string BuildModalNotificationMessage(AlarmRealtimeEntry entry)
        {
            return
                $"报警等级：{GetSeverityText(entry.Severity)}\n" +
                $"报警编码：{entry.Code}\n" +
                $"报警名称：{(string.IsNullOrWhiteSpace(entry.Name) ? entry.Code : entry.Name)}\n" +
                $"来源位置：{BuildSourceLocation(entry)}\n" +
                $"触发次数：{entry.OccurrenceCount}\n\n" +
                $"{entry.Message}\n\n" +
                "点击“确定”确认该报警。";
        }

        private static string BuildSourceLocation(AlarmRealtimeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Source))
            {
                return string.IsNullOrWhiteSpace(entry.Location) ? "Unknown" : entry.Location;
            }

            return string.IsNullOrWhiteSpace(entry.Location)
                ? entry.Source
                : $"{entry.Source} / {entry.Location}";
        }

        private static string GetSeverityText(AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Fatal => "致命",
                AlarmSeverity.Error => "错误",
                AlarmSeverity.Warning => "警告",
                _ => "信息"
            };
        }

        private string ResolveCurrentUser()
        {
            return _userService.CurUser?.UserName ?? "System";
        }

        private static void RunOnUiThread(Action action)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }
    }
}

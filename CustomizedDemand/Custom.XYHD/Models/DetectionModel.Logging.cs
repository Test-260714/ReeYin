using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {
        [NonSerialized]
        private System.Collections.ObjectModel.ObservableCollection<LogItem> _logItems;

        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<LogItem> LogItems
        {
            get => _logItems ??= new System.Collections.ObjectModel.ObservableCollection<LogItem>();
            set => SetProperty(ref _logItems, value);
        }

        [NonSerialized]
        private object _logThrottleSync = new();
        [NonSerialized]
        private Dictionary<string, DateTime> _lastThrottledLogUtc;

        private object LogThrottleSync => _logThrottleSync ??= new object();

        public void AddLog(string message, string level = "INFO")
        {
            if (ShouldSuppressThrottledLog(message, level))
                return;

            PublishToSharedLog(message, level);
            if (!ShouldAddLocalLog(level))
            {
                return;
            }

            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                LogItems.Insert(0, new LogItem(level, message));
                if (LogItems.Count > 100)
                    LogItems.RemoveAt(LogItems.Count - 1);
            }));
        }

        private void AddLogThrottled(string key, string message, string level = "INFO", int intervalMilliseconds = 5000)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                AddLog(message, level);
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;
            lock (LogThrottleSync)
            {
                _lastThrottledLogUtc ??= new Dictionary<string, DateTime>(StringComparer.Ordinal);
                if (_lastThrottledLogUtc.TryGetValue(key, out DateTime lastUtc)
                    && (nowUtc - lastUtc).TotalMilliseconds < Math.Max(1, intervalMilliseconds))
                {
                    return;
                }

                _lastThrottledLogUtc[key] = nowUtc;
            }

            AddLog(message, level);
        }

        private bool ShouldSuppressThrottledLog(string message, string level)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string normalizedLevel = (level ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedLevel != "WARN" && normalizedLevel != "WARNING")
                return false;

            if (!message.Contains("[TryGetPathImage]", StringComparison.Ordinal))
                return false;

            DateTime nowUtc = DateTime.UtcNow;
            string key = $"AutoThrottle:{message}";
            lock (LogThrottleSync)
            {
                _lastThrottledLogUtc ??= new Dictionary<string, DateTime>(StringComparer.Ordinal);
                if (_lastThrottledLogUtc.TryGetValue(key, out DateTime lastUtc)
                    && (nowUtc - lastUtc).TotalMilliseconds < 5000)
                {
                    return true;
                }

                _lastThrottledLogUtc[key] = nowUtc;
                return false;
            }
        }

        private static void PublishToSharedLog(string message, string level)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string normalizedLevel = (level ?? string.Empty).Trim().ToUpperInvariant();
            if ((normalizedLevel == "TRACE" || normalizedLevel == "DEBUG" || normalizedLevel == "INFO")
                && !IsVerboseFlowLogEnabled())
            {
                return;
            }

            var formatted = $"[XYHD] {message}";
            switch (normalizedLevel)
            {
                case "WARN":
                case "WARNING":
                    Logs.LogWarning(formatted);
                    break;
                case "ERROR":
                    Logs.LogError(formatted);
                    break;
                case "FATAL":
                    Logs.LogFatal(formatted);
                    break;
                case "TRACE":
                case "DEBUG":
                    Logs.LogTrace(formatted);
                    break;
                default:
                    Logs.LogInfo(formatted);
                    break;
            }
        }

        private static bool IsVerboseFlowLogEnabled()
        {
            string value = Environment.GetEnvironmentVariable("REEYIN_VERBOSE_FLOW_LOG");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldAddLocalLog(string level)
        {
            string normalizedLevel = (level ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedLevel == "WARN"
                || normalizedLevel == "WARNING"
                || normalizedLevel == "ERROR"
                || normalizedLevel == "FATAL")
            {
                return true;
            }

#if DEBUG
            return true;
#else
            return IsVerboseFlowLogEnabled() || IsVerboseXyhdUiLogEnabled();
#endif
        }

        private static bool IsVerboseXyhdUiLogEnabled()
        {
            string value = Environment.GetEnvironmentVariable("REEYIN_VERBOSE_XYHD_UI_LOG");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public void ClearLogs()
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                LogItems.Clear();
            }));
        }
    }
}

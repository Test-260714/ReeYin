#nullable enable
using Prism.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.HardwareRules;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    [ExposedService(Lifetime.Singleton, 7, AutoInitialize = true)]
    public sealed class HardwareAlarmMonitorService : IDisposable
    {
        private const string AlarmReportSourceKey = "AlarmReportSource";
        private const string RuleMatchSourceKey = "RuleMatchSource";
        private const string HardwareSourceNameKey = "HardwareSourceName";

        private readonly IEventAggregator _eventAggregator;
        private readonly IHardwareAlarmReporter _reporter;
        private readonly HardwareAlarmRuleEngine _ruleEngine;
        private readonly Dictionary<string, HardwareAlarmStateSnapshot> _snapshots = new Dictionary<string, HardwareAlarmStateSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _activeRuleIdsByAlarmKey = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> _ruleSnapshotKeysByAlarmKey = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _lastReportedAtByAlarmKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reportInFlightAlarmKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reportedAlarmKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();
        private SubscriptionToken? _subscriptionToken;
        private bool _disposed;

        public HardwareAlarmMonitorService(IEventAggregator eventAggregator, IHardwareAlarmReporter reporter, HardwareAlarmRuleEngine engine)
        {
            _eventAggregator = eventAggregator;
            _reporter = reporter;
            _ruleEngine = engine;
            _subscriptionToken = _eventAggregator.GetEvent<HardwareStatusChangedEvent>().Subscribe(OnHardwareStatusChanged);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_subscriptionToken != null)
            {
                _eventAggregator.GetEvent<HardwareStatusChangedEvent>().Unsubscribe(_subscriptionToken);
                _subscriptionToken = null;
            }

            _disposed = true;
        }

        private void OnHardwareStatusChanged(HardwareStatus status)
        {
            if (status == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(status.Name) ? "Unknown" : status.Name.Trim();
            bool useFallback = false;
            try
            {
                HardwareAlarmRuleContext context = CreateRuleContext(status, name);
                IReadOnlyList<HardwareAlarmRuleAction> ruleActions = _ruleEngine.Evaluate(context);
                if (ruleActions.Count > 0)
                {
                    ApplyRuleActions(ruleActions, status.Status);
                    return;
                }

                useFallback = true;
            }
            catch (Exception)
            {
                useFallback = true;
            }

            if (useFallback)
            {
                SafeApplyFallbackStatePolicy(status, name);
            }
        }

        private void ApplyRuleActions(IReadOnlyList<HardwareAlarmRuleAction> actions, HardwareState currentState)
        {
            foreach (HardwareAlarmRuleAction action in actions)
            {
                if (action == null || string.IsNullOrWhiteSpace(action.DefinitionCode))
                {
                    continue;
                }

                if (action.ShouldRaise)
                {
                    ApplyRuleRaise(action, currentState);
                }

                if (action.ShouldClear)
                {
                    ApplyRuleClear(action, currentState);
                }
            }
        }

        private void ApplyRuleRaise(HardwareAlarmRuleAction action, HardwareState currentState)
        {
            string code = action.DefinitionCode.Trim();
            string source = NormalizeSource(action.Source);
            string sourceType = string.IsNullOrWhiteSpace(action.SourceType) ? source : action.SourceType.Trim();
            string location = NormalizeLocation(action.Location);
            string ruleId = GetRuleId(action, code);
            string alarmKey = CreateAlarmSnapshotKey(code, source, location);
            string ruleKey = CreateRuleSnapshotKey(code, source, location, ruleId);
            int debounceMilliseconds = Math.Max(0, action.DebounceMilliseconds);
            int throttleSeconds = Math.Max(0, action.ThrottleSeconds);
            DateTime now = DateTime.Now;
            bool shouldReport = false;
            bool enteredAlarm = false;
            DateTime pendingSince = default;
            bool scheduleDebouncedRaise = false;
            bool waitForDebounce = false;

            lock (_gate)
            {
                HardwareAlarmStateSnapshot snapshot = GetOrCreateSnapshotNoLock(ruleKey, currentState);
                HashSet<string> activeRuleIds = GetOrCreateActiveRuleIdsNoLock(alarmKey);
                Dictionary<string, string> ruleSnapshotKeys = GetOrCreateRuleSnapshotKeysNoLock(alarmKey);
                if (!snapshot.IsInAlarm || !string.Equals(snapshot.ActiveCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    if (debounceMilliseconds > 0)
                    {
                        if (!string.Equals(snapshot.PendingCode, code, StringComparison.OrdinalIgnoreCase))
                        {
                            snapshot.PendingCode = code;
                            snapshot.PendingSince = now;
                            snapshot.LastState = currentState;
                            snapshot.LastChangedAt = now;
                            pendingSince = now;
                            scheduleDebouncedRaise = true;
                            waitForDebounce = true;
                        }
                        else if ((now - snapshot.PendingSince).TotalMilliseconds < debounceMilliseconds)
                        {
                            snapshot.LastState = currentState;
                            snapshot.LastChangedAt = now;
                            waitForDebounce = true;
                        }
                    }

                    if (!waitForDebounce)
                    {
                        snapshot.IsInAlarm = true;
                        snapshot.ActiveCode = code;
                        snapshot.PendingCode = string.Empty;
                        snapshot.PendingSince = default;
                        snapshot.IsLatched = action.IsLatched;
                        snapshot.LastState = currentState;
                        snapshot.LastChangedAt = now;
                        activeRuleIds.Add(ruleId);
                        ruleSnapshotKeys[ruleId] = ruleKey;
                        enteredAlarm = true;
                        shouldReport = TryMarkRuleReportInFlightNoLock(alarmKey, throttleSeconds, now);
                    }
                }
                else
                {
                    snapshot.LastState = currentState;
                    snapshot.LastChangedAt = now;
                    activeRuleIds.Add(ruleId);
                    ruleSnapshotKeys[ruleId] = ruleKey;
                    shouldReport = TryMarkRuleReportInFlightNoLock(alarmKey, throttleSeconds, now);
                }
            }

            if (scheduleDebouncedRaise)
            {
                ScheduleDebouncedRuleRaise(action, currentState, code, source, sourceType, location, ruleId, alarmKey, ruleKey, pendingSince, debounceMilliseconds);
                return;
            }

            if (!shouldReport)
            {
                return;
            }

            ReportRuleAlarm(action, code, source, sourceType, location, alarmKey, ruleKey, enteredAlarm);
        }

        private void ApplyRuleClear(HardwareAlarmRuleAction action, HardwareState currentState)
        {
            string code = action.DefinitionCode.Trim();
            string source = NormalizeSource(action.Source);
            string location = NormalizeLocation(action.Location);
            string ruleId = GetRuleId(action, code);
            string alarmKey = CreateAlarmSnapshotKey(code, source, location);
            string ruleKey = CreateRuleSnapshotKey(code, source, location, ruleId);

            lock (_gate)
            {
                if (!_snapshots.TryGetValue(ruleKey, out HardwareAlarmStateSnapshot? snapshot) || !snapshot.IsInAlarm)
                {
                    if (snapshot != null && string.Equals(snapshot.PendingCode, code, StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.PendingCode = string.Empty;
                        snapshot.PendingSince = default;
                        snapshot.LastState = currentState;
                        snapshot.LastChangedAt = DateTime.Now;
                    }

                    return;
                }

                if (snapshot.IsLatched || action.IsLatched)
                {
                    snapshot.LastState = currentState;
                    snapshot.LastChangedAt = DateTime.Now;
                    return;
                }

                if (!_activeRuleIdsByAlarmKey.TryGetValue(alarmKey, out HashSet<string>? activeRuleIds))
                {
                    activeRuleIds = GetOrCreateActiveRuleIdsNoLock(alarmKey);
                    activeRuleIds.Add(ruleId);
                }

                if (activeRuleIds.Count > 1)
                {
                    activeRuleIds.Remove(ruleId);
                    RemoveRuleSnapshotKeyNoLock(alarmKey, ruleId);
                    snapshot.IsInAlarm = false;
                    snapshot.IsLatched = false;
                    snapshot.ActiveCode = string.Empty;
                    snapshot.PendingCode = string.Empty;
                    snapshot.PendingSince = default;
                    snapshot.LastReportedAt = default;
                    snapshot.LastState = currentState;
                    snapshot.LastChangedAt = DateTime.Now;
                    return;
                }

                bool clearSucceeded = _reporter.Clear(code, source, location, "System", action.Message);
                if (!clearSucceeded)
                {
                    snapshot.LastState = currentState;
                    snapshot.LastChangedAt = DateTime.Now;
                    activeRuleIds.Add(ruleId);
                    return;
                }

                activeRuleIds.Remove(ruleId);
                if (activeRuleIds.Count == 0)
                {
                    _activeRuleIdsByAlarmKey.Remove(alarmKey);
                    _ruleSnapshotKeysByAlarmKey.Remove(alarmKey);
                    _lastReportedAtByAlarmKey.Remove(alarmKey);
                    _reportedAlarmKeys.Remove(alarmKey);
                    _reportInFlightAlarmKeys.Remove(alarmKey);
                }
                else
                {
                    RemoveRuleSnapshotKeyNoLock(alarmKey, ruleId);
                }

                snapshot.IsInAlarm = false;
                snapshot.IsLatched = false;
                snapshot.ActiveCode = string.Empty;
                snapshot.PendingCode = string.Empty;
                snapshot.PendingSince = default;
                snapshot.LastReportedAt = default;
                snapshot.LastState = currentState;
                snapshot.LastChangedAt = DateTime.Now;
            }
        }

        private void SafeApplyFallbackStatePolicy(HardwareStatus status, string name)
        {
            try
            {
                ApplyFallbackStatePolicy(status, name);
            }
            catch (Exception)
            {
                // Hardware status notifications must not destabilize the Prism publisher path.
            }
        }

        private void ApplyFallbackStatePolicy(HardwareStatus status, string name)
        {
            HardwareAlarmStateAction action;
            bool shouldRaise = false;
            bool shouldClear = false;
            string activeCodeToClear = string.Empty;
            string location = $"Hardware:{name}";

            lock (_gate)
            {
                HardwareAlarmStateSnapshot snapshot = GetOrCreateSnapshotNoLock(name, status.Status);
                action = HardwareAlarmStatePolicy.Resolve(status.Status, snapshot.IsInAlarm);
                shouldRaise = action.ShouldRaise && (!snapshot.IsInAlarm || !string.Equals(snapshot.ActiveCode, action.Code, StringComparison.OrdinalIgnoreCase));
                shouldClear = action.ShouldClear && snapshot.IsInAlarm;
                if (shouldRaise)
                {
                    snapshot.IsInAlarm = true;
                    snapshot.ActiveCode = action.Code;
                }

                if (shouldClear)
                {
                    activeCodeToClear = string.IsNullOrWhiteSpace(snapshot.ActiveCode) ? action.Code : snapshot.ActiveCode;
                    bool clearSucceeded = _reporter.Clear(activeCodeToClear, HardwareAlarmSources.Hardware, location, "System", action.Message);
                    if (clearSucceeded)
                    {
                        snapshot.IsInAlarm = false;
                        snapshot.ActiveCode = string.Empty;
                    }
                    else
                    {
                        shouldClear = false;
                    }
                }

                snapshot.LastState = status.Status;
                snapshot.LastChangedAt = DateTime.Now;
            }

            if (shouldRaise)
            {
                try
                {
                    AlarmInfo alarm = _reporter.Report(new AlarmReportRequest
                    {
                        Code = action.Code,
                        Source = HardwareAlarmSources.Hardware,
                        SourceType = HardwareAlarmSources.Hardware,
                        Location = location,
                        Message = string.IsNullOrWhiteSpace(status.Describe) ? action.Message : status.Describe,
                        ExtraData =
                        {
                            ["HardwareName"] = name,
                            ["HardwareState"] = status.Status.ToString(),
                            ["IsConnect"] = status.IsConnect
                        }
                    });

                    if (!ReportSucceeded(alarm, action.Code))
                    {
                        RollbackFallbackRaise(name, action.Code);
                    }
                }
                catch (Exception)
                {
                    RollbackFallbackRaise(name, action.Code);
                    throw;
                }
            }
        }

        private void ScheduleDebouncedRuleRaise(
            HardwareAlarmRuleAction action,
            HardwareState currentState,
            string code,
            string source,
            string sourceType,
            string location,
            string ruleId,
            string alarmKey,
            string ruleKey,
            DateTime pendingSince,
            int debounceMilliseconds)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(debounceMilliseconds).ConfigureAwait(false);

                    bool shouldReport = false;
                    lock (_gate)
                    {
                        if (!_snapshots.TryGetValue(ruleKey, out HardwareAlarmStateSnapshot? snapshot) ||
                            snapshot.IsInAlarm ||
                            !string.Equals(snapshot.PendingCode, code, StringComparison.OrdinalIgnoreCase) ||
                            snapshot.PendingSince != pendingSince)
                        {
                            return;
                        }

                        DateTime now = DateTime.Now;
                        snapshot.IsInAlarm = true;
                        snapshot.ActiveCode = code;
                        snapshot.PendingCode = string.Empty;
                        snapshot.PendingSince = default;
                        snapshot.IsLatched = action.IsLatched;
                        snapshot.LastState = currentState;
                        snapshot.LastChangedAt = now;
                        GetOrCreateActiveRuleIdsNoLock(alarmKey).Add(ruleId);
                        GetOrCreateRuleSnapshotKeysNoLock(alarmKey)[ruleId] = ruleKey;
                        shouldReport = TryMarkRuleReportInFlightNoLock(alarmKey, Math.Max(0, action.ThrottleSeconds), now);
                    }

                    if (shouldReport)
                    {
                        ReportRuleAlarm(action, code, source, sourceType, location, alarmKey, ruleKey, enteredAlarm: true);
                    }
                }
                catch
                {
                    // Delayed debounce reporting must not destabilize the event publisher path.
                }
            });
        }

        private void ReportRuleAlarm(
            HardwareAlarmRuleAction action,
            string code,
            string source,
            string sourceType,
            string location,
            string alarmKey,
            string ruleKey,
            bool enteredAlarm)
        {
            try
            {
                AlarmInfo alarm = _reporter.Report(new AlarmReportRequest
                {
                    Code = code,
                    Source = source,
                    SourceType = sourceType,
                    Location = location,
                    Message = string.IsNullOrWhiteSpace(action.Message) ? code : action.Message,
                    ExtraData = action.ExtraData == null
                        ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, object?>(action.ExtraData, StringComparer.OrdinalIgnoreCase)
                });

                if (!ReportSucceeded(alarm, code))
                {
                    CompleteRuleReportFailure(alarmKey, enteredAlarm);
                    return;
                }

                CompleteRuleReportSuccess(alarmKey, ruleKey);
            }
            catch (Exception)
            {
                CompleteRuleReportFailure(alarmKey, enteredAlarm);
                throw;
            }
        }

        private bool TryMarkRuleReportInFlightNoLock(string alarmKey, int throttleSeconds, DateTime now)
        {
            if (_reportInFlightAlarmKeys.Contains(alarmKey))
            {
                return false;
            }

            if (throttleSeconds > 0 &&
                _lastReportedAtByAlarmKey.TryGetValue(alarmKey, out DateTime lastReportedAt) &&
                (now - lastReportedAt).TotalSeconds < throttleSeconds)
            {
                return false;
            }

            _reportInFlightAlarmKeys.Add(alarmKey);
            return true;
        }

        private void CompleteRuleReportSuccess(string alarmKey, string ruleKey)
        {
            DateTime now = DateTime.Now;
            lock (_gate)
            {
                _reportInFlightAlarmKeys.Remove(alarmKey);
                _reportedAlarmKeys.Add(alarmKey);
                _lastReportedAtByAlarmKey[alarmKey] = now;
                if (_snapshots.TryGetValue(ruleKey, out HardwareAlarmStateSnapshot? snapshot))
                {
                    snapshot.LastReportedAt = now;
                }
            }
        }

        private void CompleteRuleReportFailure(string alarmKey, bool rollbackEnteredAlarm)
        {
            if (rollbackEnteredAlarm)
            {
                RollbackRuleAlarmRaise(alarmKey);
                return;
            }

            lock (_gate)
            {
                _reportInFlightAlarmKeys.Remove(alarmKey);
            }
        }

        private void RollbackRuleAlarmRaise(string alarmKey)
        {
            lock (_gate)
            {
                if (_ruleSnapshotKeysByAlarmKey.TryGetValue(alarmKey, out Dictionary<string, string>? ruleSnapshotKeys))
                {
                    foreach (string ruleKey in ruleSnapshotKeys.Values)
                    {
                        if (_snapshots.TryGetValue(ruleKey, out HardwareAlarmStateSnapshot? snapshot))
                        {
                            snapshot.IsInAlarm = false;
                            snapshot.ActiveCode = string.Empty;
                            snapshot.LastChangedAt = DateTime.Now;
                        }
                    }
                }

                _activeRuleIdsByAlarmKey.Remove(alarmKey);
                _ruleSnapshotKeysByAlarmKey.Remove(alarmKey);
                _lastReportedAtByAlarmKey.Remove(alarmKey);
                _reportInFlightAlarmKeys.Remove(alarmKey);
                _reportedAlarmKeys.Remove(alarmKey);
            }
        }

        private void RollbackFallbackRaise(string key, string code)
        {
            lock (_gate)
            {
                if (_snapshots.TryGetValue(key, out HardwareAlarmStateSnapshot? snapshot) &&
                    string.Equals(snapshot.ActiveCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.IsInAlarm = false;
                    snapshot.ActiveCode = string.Empty;
                    snapshot.LastChangedAt = DateTime.Now;
                }
            }
        }

        private HardwareAlarmStateSnapshot GetOrCreateSnapshotNoLock(string key, HardwareState currentState)
        {
            if (!_snapshots.TryGetValue(key, out HardwareAlarmStateSnapshot? snapshot))
            {
                snapshot = new HardwareAlarmStateSnapshot { Name = key, LastState = currentState };
                _snapshots[key] = snapshot;
            }

            return snapshot;
        }

        private HashSet<string> GetOrCreateActiveRuleIdsNoLock(string alarmKey)
        {
            if (!_activeRuleIdsByAlarmKey.TryGetValue(alarmKey, out HashSet<string>? activeRuleIds))
            {
                activeRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _activeRuleIdsByAlarmKey[alarmKey] = activeRuleIds;
            }

            return activeRuleIds;
        }

        private Dictionary<string, string> GetOrCreateRuleSnapshotKeysNoLock(string alarmKey)
        {
            if (!_ruleSnapshotKeysByAlarmKey.TryGetValue(alarmKey, out Dictionary<string, string>? ruleSnapshotKeys))
            {
                ruleSnapshotKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _ruleSnapshotKeysByAlarmKey[alarmKey] = ruleSnapshotKeys;
            }

            return ruleSnapshotKeys;
        }

        private void RemoveRuleSnapshotKeyNoLock(string alarmKey, string ruleId)
        {
            if (_ruleSnapshotKeysByAlarmKey.TryGetValue(alarmKey, out Dictionary<string, string>? ruleSnapshotKeys))
            {
                ruleSnapshotKeys.Remove(ruleId);
                if (ruleSnapshotKeys.Count == 0)
                {
                    _ruleSnapshotKeysByAlarmKey.Remove(alarmKey);
                }
            }
        }

        private static HardwareAlarmRuleContext CreateRuleContext(HardwareStatus status, string name)
        {
            string sourceType = string.IsNullOrWhiteSpace(status.SourceType) ? HardwareAlarmSources.Hardware : status.SourceType.Trim();
            string location = string.IsNullOrWhiteSpace(status.Location) ? $"Hardware:{name}" : status.Location.Trim();
            Dictionary<string, object?> extraData = status.ExtraData == null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(status.ExtraData, StringComparer.OrdinalIgnoreCase);

            extraData[HardwareSourceNameKey] = name;
            extraData[RuleMatchSourceKey] = name;
            extraData[AlarmReportSourceKey] = name;

            return new HardwareAlarmRuleContext
            {
                Name = name,
                Source = name,
                SourceType = sourceType,
                Location = location,
                Status = status.Status,
                IsConnect = status.IsConnect,
                Describe = status.Describe ?? string.Empty,
                ErrorCode = status.ErrorCode ?? string.Empty,
                Operation = status.Operation ?? string.Empty,
                ExtraData = extraData,
                Timestamp = status.Timestamp == default ? DateTime.Now : status.Timestamp
            };
        }

        private static bool ReportSucceeded(AlarmInfo? alarm, string code)
        {
            return alarm != null &&
                   !string.IsNullOrWhiteSpace(alarm.Code) &&
                   (string.IsNullOrWhiteSpace(code) || string.Equals(alarm.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetRuleId(HardwareAlarmRuleAction action, string code)
        {
            if (action.ExtraData != null &&
                action.ExtraData.TryGetValue("RuleId", out object? value) &&
                value != null &&
                !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString()!.Trim();
            }

            return code;
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? HardwareAlarmSources.Hardware : source.Trim();
        }

        private static string NormalizeLocation(string location)
        {
            return string.IsNullOrWhiteSpace(location) ? "Hardware:Unknown" : location.Trim();
        }

        private static string CreateAlarmSnapshotKey(string definitionCode, string source, string location)
        {
            return $"Alarm:{definitionCode}|{source}|{location}";
        }

        private static string CreateRuleSnapshotKey(string definitionCode, string source, string location, string ruleId)
        {
            return $"Rule:{ruleId}|{definitionCode}|{source}|{location}";
        }
    }
}

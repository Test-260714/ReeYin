using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using ReeYin_V.Core.Events.Alarm;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Governance;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Logger;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;

namespace ReeYin_V.Core.Services.Alarm
{
    /// <summary>
    /// 线程安全的报警引擎，包含活动报警内存缓存和异步数据库持久化。
    /// </summary>
    [ExposedService(Lifetime.Singleton, 5, typeof(IAlarmService))]
    public sealed class AlarmService : IAlarmService
    {
        private readonly ISqlSugarClient _database;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAlarmDefinitionService _definitionService;
        private readonly IAlarmGovernanceService _governanceService;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private static readonly string[] SimulationCodePrefixes =
        {
            "SIM.",
            "SIM_",
            "SIM-",
            "MOCK.",
            "MOCK_",
            "MOCK-",
            "FAKE.",
            "FAKE_",
            "FAKE-",
            "DEMO.",
            "DEMO_",
            "DEMO-",
            "SAMPLE.",
            "SAMPLE_",
            "SAMPLE-",
            "TEST_ALARM",
            "TEST.ALARM",
            "TEST-ALARM"
        };

        private static readonly string[] SimulationExactValues =
        {
            "SIM",
            "SIMULATED",
            "SIMULATION",
            "SIMULATOR",
            "MOCK",
            "FAKE",
            "DEMO",
            "SAMPLE",
            "TEST_ALARM",
            "TEST ALARM",
            "TEST.ALARM",
            "TEST-ALARM",
            "\u6a21\u62df",
            "\u6a21\u62df\u6570\u636e",
            "\u6a21\u62df\u62a5\u8b66",
            "\u4eff\u771f",
            "\u6f14\u793a",
            "\u793a\u4f8b",
            "\u6837\u4f8b",
            "\u6d4b\u8bd5\u62a5\u8b66"
        };

        private static readonly string[] SimulationBooleanKeys =
        {
            "IsSimulation",
            "IsSimulated",
            "Simulation",
            "Simulated",
            "IsMock",
            "Mock",
            "IsFake",
            "Fake",
            "IsDemo",
            "Demo",
            "IsSample",
            "Sample"
        };

        private static readonly string[] SimulationDescriptorKeys =
        {
            "DataSource",
            "SourceKind",
            "SourceType",
            "Mode",
            "Provider",
            "Environment"
        };

        // 状态变更先更新内存，再由单读者队列异步写库，避免阻塞设备流程。
        private readonly Channel<PersistenceWorkItem> _writeChannel = Channel.CreateUnbounded<PersistenceWorkItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

        // 保护 _activeByKey、_realtimeFeed 和看板计数。
        private readonly object _stateGate = new object();

        // 活动报警按来源、编码和位置去重。
        private readonly Dictionary<string, AlarmInfo> _activeByKey = new Dictionary<string, AlarmInfo>(StringComparer.OrdinalIgnoreCase);

        // 实时事件流保留最近的生命周期事件，超过 MaxCacheCount 后裁剪旧数据。
        private readonly LinkedList<AlarmRealtimeEntry> _realtimeFeed = new LinkedList<AlarmRealtimeEntry>();

        private bool _initialized;
        private long _historyCount;
        private int _todayRaisedCount;
        private DateTime _todayCounterDate = DateTime.Today;

        private sealed class AlarmTimestampProjection
        {
            public DateTime Timestamp { get; set; }
        }

        private sealed class AlarmTypeDistributionProjection
        {
            public string Code { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string Category { get; set; } = string.Empty;

            public int Count { get; set; }

            public int HighestSeverityValue { get; set; }

            public DateTime LatestRaisedAt { get; set; }
        }

        private sealed class AlarmSourceDistributionProjection
        {
            public string Source { get; set; } = string.Empty;

            public int Count { get; set; }

            public int HighestSeverityValue { get; set; }

            public DateTime LatestRaisedAt { get; set; }
        }

        #region 初始化

        public AlarmService(
            ISqlSugarClient database,
            IEventAggregator eventAggregator,
            IAlarmDefinitionService definitionService,
            IAlarmGovernanceService governanceService)
        {
            _database = database;
            _eventAggregator = eventAggregator;
            _definitionService = definitionService;
            _governanceService = governanceService;
            _ = Task.Run(ProcessPersistenceQueueAsync);
        }

        public event EventHandler<AlarmDataChangedEventArgs>? DataChanged;

        public int MaxCacheCount => 1000;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            await _initializeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                List<AlarmRecordEntity> activeRecords = await _database.Queryable<AlarmRecordEntity>()
                    .Where(record => record.IsActive)
                    .OrderBy(record => record.LastRaisedAt, OrderByType.Asc)
                    .ToListAsync()
                    .ConfigureAwait(false);

                await RemoveSimulatedActiveRecordsAsync(activeRecords).ConfigureAwait(false);
                activeRecords = activeRecords
                    .Where(record => !IsSimulatedAlarmRecord(record))
                    .ToList();

                List<AlarmRecordEntity> latestHistory = await _database.Queryable<AlarmRecordEntity>()
                    .OrderBy(record => record.LastRaisedAt, OrderByType.Desc)
                    .Take(MaxCacheCount)
                    .ToListAsync()
                    .ConfigureAwait(false);

                long historyCount = await _database.Queryable<AlarmRecordEntity>()
                    .CountAsync()
                    .ConfigureAwait(false);

                int todayRaisedCount = await _database.Queryable<AlarmRecordEntity>()
                    .Where(record => record.Timestamp >= _todayCounterDate)
                    .CountAsync()
                    .ConfigureAwait(false);

                lock (_stateGate)
                {
                    _activeByKey.Clear();
                    foreach (AlarmRecordEntity entity in activeRecords)
                    {
                        _activeByKey[entity.ActiveKey] = MapToAlarmInfo(entity);
                    }

                    _realtimeFeed.Clear();
                    foreach (AlarmRealtimeEntry entry in latestHistory
                                 .OrderBy(record => ResolveLatestEventTime(record))
                                 .Select(CreateHistoricalRealtimeEntry))
                    {
                        _realtimeFeed.AddLast(entry);
                    }

                    _historyCount = historyCount;
                    _todayRaisedCount = todayRaisedCount;
                    _initialized = true;
                }
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        #endregion

        #region 状态变更

        public AlarmInfo AddAlarm(string code, string message, AlarmSeverity level, string source)
        {
            return AddAlarm(new AlarmRaiseRequest
            {
                Code = code,
                Message = message,
                Level = level,
                Source = source
            });
        }

        public AlarmInfo Report(AlarmSignal signal)
        {
            if (signal == null)
            {
                throw new ArgumentNullException(nameof(signal));
            }

            Dictionary<string, object?> data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (signal.Data != null)
            {
                foreach (KeyValuePair<string, object?> item in signal.Data)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key))
                    {
                        data[item.Key] = item.Value;
                    }
                }
            }

            data["OccurredAt"] = signal.OccurredAt;
            data["SourceKind"] = signal.SourceKind.ToString();

            AlarmReportRequest reportRequest = new AlarmReportRequest
            {
                Code = signal.Code,
                Source = signal.Source,
                SourceType = ToSourceType(signal.SourceKind),
                Location = signal.Location,
                Message = signal.Message,
                Severity = signal.SeverityOverride,
                ExtraData = data
            };

            return AddAlarm(_definitionService.BuildRaiseRequest(reportRequest));
        }

        public AlarmInfo AddAlarm(AlarmRaiseRequest request)
        {
            EnsureInitialized();
            ValidateRaiseRequest(request);

            DateTime now = DateTime.Now;
            if (_governanceService.TryMatchSuppression(request, now, out AlarmSuppressionRuleInfo? suppressionRule))
            {
                AlarmInfo suppressed = CreateGovernedSnapshot(request, false, "Suppressed", suppressionRule?.Reason);
                WriteAudit(CreateAuditInfo(suppressed, "Suppressed", "System", suppressionRule?.Reason ?? "Suppression rule matched.", now));
                return suppressed;
            }

            if (_governanceService.TryMatchShelve(request, now, out AlarmShelveInfo? shelf))
            {
                AlarmInfo shelved = CreateGovernedSnapshot(request, false, "Shelved", shelf?.Reason);
                WriteAudit(CreateAuditInfo(shelved, "Shelved", shelf?.ShelvedBy ?? "System", shelf?.Reason ?? "Alarm is shelved.", now));
                return shelved;
            }

            IReadOnlyList<AlarmNotificationRouteInfo> notificationRoutes = _governanceService.ResolveNotificationRoutes(request, now);

            AlarmInfo snapshot;
            AlarmRecordEntity entity;
            AlarmRealtimeEntry latestEvent;
            AlarmDashboardSnapshot dashboard;
            IReadOnlyList<AlarmActiveRecord> activeSnapshot;
            PersistenceAction action;

            lock (_stateGate)
            {
                RollDateWindowUnsafe(now);

                string activeKey = BuildActiveKey(request.Code, request.Source, request.Location);
                if (_activeByKey.TryGetValue(activeKey, out AlarmInfo? activeAlarm))
                {
                    // 重复触发会更新同一个生命周期，避免产生重复的活动报警。
                    activeAlarm.Message = request.Message;
                    activeAlarm.Name = GetPreferredValue(request.Name, activeAlarm.Name, request.Code);
                    activeAlarm.Category = GetPreferredValue(request.Category, activeAlarm.Category);
                    AlarmSeverity previousLevel = activeAlarm.Level;
                    activeAlarm.Level = request.Level;
                    activeAlarm.LastRaisedAt = now;
                    activeAlarm.OccurrenceCount++;
                    activeAlarm.Location = GetPreferredValue(request.Location, activeAlarm.Location);
                    activeAlarm.NeedAcknowledge = request.NeedAcknowledge;
                    bool resetAcknowledgement = request.AcknowledgeResetMode == AlarmAcknowledgeResetMode.OnEveryRepeat ||
                                                (request.AcknowledgeResetMode == AlarmAcknowledgeResetMode.OnSeverityIncrease && request.Level > previousLevel);
                    if (request.NeedAcknowledge && resetAcknowledgement)
                    {
                        activeAlarm.IsConfirmed = false;
                        activeAlarm.ConfirmUser = string.Empty;
                        activeAlarm.ConfirmTime = null;
                    }
                    else
                    {
                        activeAlarm.IsConfirmed = true;
                    }

                    activeAlarm.PopupMode = request.PopupMode;
                    activeAlarm.PopupThrottleSeconds = Math.Max(0, request.PopupThrottleSeconds);
                    activeAlarm.AllowManualClear = request.AllowManualClear;
                    activeAlarm.AcknowledgeResetMode = request.AcknowledgeResetMode;
                    MergeExtraData(activeAlarm.ExtraData, request.ExtraData);

                    snapshot = activeAlarm.CreateCopy();
                    entity = MapToEntity(activeAlarm, activeKey);
                    latestEvent = CreateRealtimeEntry(activeAlarm, AlarmEventKind.Repeated, now);
                    action = PersistenceAction.Update;
                }
                else
                {
                    AlarmInfo newAlarm = new AlarmInfo
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Code = request.Code.Trim(),
                        Name = GetPreferredValue(request.Name, request.Code),
                        Category = request.Category?.Trim() ?? string.Empty,
                        Message = request.Message.Trim(),
                        Level = request.Level,
                        Source = request.Source.Trim(),
                        Location = request.Location?.Trim() ?? string.Empty,
                        Timestamp = now,
                        LastRaisedAt = now,
                        IsActive = true,
                        IsConfirmed = !request.NeedAcknowledge,
                        NeedAcknowledge = request.NeedAcknowledge,
                        PopupMode = request.PopupMode,
                        PopupThrottleSeconds = Math.Max(0, request.PopupThrottleSeconds),
                        AllowManualClear = request.AllowManualClear,
                        AcknowledgeResetMode = request.AcknowledgeResetMode,
                        OccurrenceCount = 1,
                        ExtraData = new Dictionary<string, object?>(request.ExtraData ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase)
                    };

                    _activeByKey[activeKey] = newAlarm;
                    _historyCount++;
                    _todayRaisedCount++;

                    snapshot = newAlarm.CreateCopy();
                    entity = MapToEntity(newAlarm, activeKey);
                    latestEvent = CreateRealtimeEntry(newAlarm, AlarmEventKind.Raised, now);
                    action = PersistenceAction.Insert;
                }

                AppendRealtimeEntryUnsafe(latestEvent);
                dashboard = BuildDashboardSnapshotUnsafe();
                activeSnapshot = BuildActiveSnapshotUnsafe();
            }

            List<AlarmEventAuditInfo> audits = new List<AlarmEventAuditInfo>
            {
                CreateAuditInfo(snapshot, action == PersistenceAction.Insert ? "Raised" : "Repeated", "System", string.Empty, now)
            };
            if (notificationRoutes.Count > 0)
            {
                audits.Add(CreateAuditInfo(
                    snapshot,
                    "NotificationRouted",
                    "System",
                    string.Join(";", notificationRoutes.Select(item => item.Name)),
                    now,
                    JsonConvert.SerializeObject(notificationRoutes.Select(item => new
                    {
                        item.Name,
                        item.Channels,
                        item.Receivers
                    }))));
            }

            QueuePersistence(action, entity, audits);
            PublishChange(dashboard, activeSnapshot, latestEvent);
            return snapshot;
        }

        public bool ClearAlarm(string code, string? source = null, string? user = null, string? note = null, string? location = null)
        {
            return ClearByKey(code, source, location, user, note, AlarmClearOrigin.Manual).Success;
        }

        public AlarmOperationResult ClearByKey(string code, string? source, string? location, string? user, string? note, AlarmClearOrigin origin)
        {
            EnsureInitialized();
            string normalizedCode = code?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return AlarmOperationResult.From(AlarmOperationStatus.InvalidRequest, message: "Alarm code is required.");
            }

            List<AlarmRecordEntity> updates;
            AlarmRealtimeEntry? latestEvent;
            AlarmDashboardSnapshot dashboard;
            IReadOnlyList<AlarmActiveRecord> activeSnapshot;

            lock (_stateGate)
            {
                DateTime now = DateTime.Now;
                List<string> keys = _activeByKey
                    .Where(item =>
                        item.Value.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrWhiteSpace(source) || item.Value.Source.Equals(source, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(location) || item.Value.Location.Equals(location, StringComparison.OrdinalIgnoreCase)))
                    .Select(item => item.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (origin == AlarmClearOrigin.Manual && keys.Any(key => !_activeByKey[key].AllowManualClear))
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.ManualClearNotAllowed, message: "Manual clear is not allowed for this alarm.");
                }

                updates = keys
                    .Select(key => ClearUnsafe(key, user, note, now))
                    .Where(entity => entity != null)
                    .Cast<AlarmRecordEntity>()
                    .ToList();

                if (updates.Count == 0)
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.NotFound, message: "Active alarm was not found.");
                }

                latestEvent = _realtimeFeed.Last?.Value;
                dashboard = BuildDashboardSnapshotUnsafe();
                activeSnapshot = BuildActiveSnapshotUnsafe();
            }

            foreach (AlarmRecordEntity update in updates)
            {
                QueuePersistence(
                    PersistenceAction.Update,
                    update,
                    CreateAuditInfo(MapToAlarmInfo(update), "Cleared", user ?? "System", note ?? string.Empty, update.ClearTime ?? DateTime.Now));
            }

            PublishChange(dashboard, activeSnapshot, latestEvent);
            return AlarmOperationResult.From(AlarmOperationStatus.Succeeded, updates[0].Id);
        }

        public bool ConfirmAlarm(string id, string user, string? note = null)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(user))
            {
                return false;
            }

            AlarmRecordEntity? updateEntity;
            AlarmRealtimeEntry? latestEvent;
            AlarmDashboardSnapshot dashboard;
            IReadOnlyList<AlarmActiveRecord> activeSnapshot;

            lock (_stateGate)
            {
                AlarmInfo? alarm = _activeByKey.Values.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (alarm == null)
                {
                    return false;
                }

                if (alarm.IsConfirmed)
                {
                    return true;
                }

                alarm.IsConfirmed = true;
                alarm.ConfirmUser = user.Trim();
                alarm.ConfirmTime = DateTime.Now;
                alarm.Note = note?.Trim() ?? alarm.Note;

                updateEntity = MapToEntity(alarm, BuildActiveKey(alarm.Code, alarm.Source, alarm.Location));
                latestEvent = CreateRealtimeEntry(alarm, AlarmEventKind.Confirmed, DateTime.Now);
                AppendRealtimeEntryUnsafe(latestEvent);
                dashboard = BuildDashboardSnapshotUnsafe();
                activeSnapshot = BuildActiveSnapshotUnsafe();
            }

            QueuePersistence(
                PersistenceAction.Update,
                updateEntity,
                CreateAuditInfo(MapToAlarmInfo(updateEntity), "Confirmed", user, note ?? string.Empty, updateEntity.ConfirmTime ?? DateTime.Now));
            PublishChange(dashboard, activeSnapshot, latestEvent);
            return true;
        }

        public async Task AcknowledgeAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default)
        {
            await AcknowledgeOperationAsync(activeId, user, note, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AlarmOperationResult> AcknowledgeOperationAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(activeId) || string.IsNullOrWhiteSpace(user))
            {
                return AlarmOperationResult.From(AlarmOperationStatus.InvalidRequest, activeId, "Alarm id and user are required.");
            }

            lock (_stateGate)
            {
                AlarmInfo? alarm = _activeByKey.Values.FirstOrDefault(item => item.Id.Equals(activeId, StringComparison.OrdinalIgnoreCase));
                if (alarm == null)
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.NotFound, activeId, "Active alarm was not found.");
                }

                if (alarm.IsConfirmed)
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.AlreadyAcknowledged, activeId, "Alarm is already acknowledged.");
                }
            }

            return ConfirmAlarm(activeId, user, note)
                ? AlarmOperationResult.From(AlarmOperationStatus.Succeeded, activeId)
                : AlarmOperationResult.From(AlarmOperationStatus.NotFound, activeId, "Active alarm was not found.");
        }

        public async Task ClearAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default)
        {
            await ClearByIdAsync(activeId, user, note, AlarmClearOrigin.Manual, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AlarmOperationResult> ClearByIdAsync(string activeId, string user, string? note, AlarmClearOrigin origin, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            AlarmRecordEntity? updateEntity;
            AlarmRealtimeEntry? latestEvent;
            AlarmDashboardSnapshot dashboard;
            IReadOnlyList<AlarmActiveRecord> activeSnapshot;

            lock (_stateGate)
            {
                string? activeKey = _activeByKey.FirstOrDefault(item => item.Value.Id.Equals(activeId, StringComparison.OrdinalIgnoreCase)).Key;
                if (string.IsNullOrWhiteSpace(activeKey))
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.NotFound, activeId, "Active alarm was not found.");
                }

                if (origin == AlarmClearOrigin.Manual && !_activeByKey[activeKey].AllowManualClear)
                {
                    return AlarmOperationResult.From(AlarmOperationStatus.ManualClearNotAllowed, activeId, "Manual clear is not allowed for this alarm.");
                }

                updateEntity = ClearUnsafe(activeKey, user, note, DateTime.Now);
                latestEvent = _realtimeFeed.Last?.Value;
                dashboard = BuildDashboardSnapshotUnsafe();
                activeSnapshot = BuildActiveSnapshotUnsafe();
            }

            if (updateEntity == null)
            {
                return AlarmOperationResult.From(AlarmOperationStatus.NotFound, activeId, "Active alarm was not found.");
            }

            QueuePersistence(
                PersistenceAction.Update,
                updateEntity,
                CreateAuditInfo(MapToAlarmInfo(updateEntity), "Cleared", user, note ?? string.Empty, updateEntity.ClearTime ?? DateTime.Now));
            PublishChange(dashboard, activeSnapshot, latestEvent);
            return AlarmOperationResult.From(AlarmOperationStatus.Succeeded, updateEntity.Id);
        }

        #endregion

        #region 查询与导出

        public async Task<AlarmDashboardSnapshot> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            lock (_stateGate)
            {
                return BuildDashboardSnapshotUnsafe();
            }
        }

        public async Task<IReadOnlyList<AlarmActiveRecord>> GetActiveAlarmsAsync(AlarmActiveQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            query ??= new AlarmActiveQuery();
            string keyword = query.Keyword?.Trim() ?? string.Empty;
            string source = query.Source?.Trim() ?? string.Empty;

            lock (_stateGate)
            {
                IEnumerable<AlarmInfo> alarms = _activeByKey.Values
                    .OrderByDescending(item => item.LastRaisedAt)
                    .ThenByDescending(item => item.Timestamp);

                if (query.Severity.HasValue)
                {
                    alarms = alarms.Where(item => item.Level == query.Severity.Value);
                }

                if (!string.IsNullOrWhiteSpace(source))
                {
                    alarms = alarms.Where(item => item.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
                }

                if (query.OnlyUnacknowledged)
                {
                    alarms = alarms.Where(item => item.NeedAcknowledge && !item.IsConfirmed);
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    alarms = alarms.Where(item => ContainsKeyword(item, keyword));
                }

                if (query.MaxCount > 0)
                {
                    alarms = alarms.Take(query.MaxCount);
                }

                return alarms.Select(MapToActiveRecord).ToArray();
            }
        }

        public async Task<AlarmPagedResult<AlarmHistoryEntry>> GetHistoryPageAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmHistoryQuery();

            (DateTime? start, DateTime? end) = NormalizeRange(query.StartTime, query.EndTime);
            int pageIndex = Math.Max(1, query.PageIndex);
            int pageSize = Math.Max(1, query.PageSize);

            ISugarQueryable<AlarmRecordEntity> historyQuery = BuildHistoryQuery(query, start, end);
            int totalCount = await historyQuery.CountAsync().ConfigureAwait(false);

            List<AlarmRecordEntity> entities = await historyQuery
                .OrderBy(record => record.Timestamp, OrderByType.Desc)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);

            return new AlarmPagedResult<AlarmHistoryEntry>
            {
                Items = entities.Select(MapToHistoryEntry).ToArray(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyList<AlarmHistoryEntry>> GetHistoryAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmHistoryQuery();

            (DateTime? start, DateTime? end) = NormalizeRange(query.StartTime, query.EndTime);
            int maxCount = query.MaxCount <= 0 ? 500 : query.MaxCount;

            List<AlarmRecordEntity> entities = await BuildHistoryQuery(query, start, end)
                .OrderBy(record => record.Timestamp, OrderByType.Desc)
                .Take(maxCount)
                .ToListAsync()
                .ConfigureAwait(false);

            return entities.Select(MapToHistoryEntry).ToArray();
        }

        public async Task<AlarmStatisticsResult> GetStatisticsAsync(AlarmStatisticsQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmStatisticsQuery();

            DateTime start = query.StartTime <= query.EndTime ? query.StartTime : query.EndTime;
            DateTime end = query.StartTime <= query.EndTime ? query.EndTime : query.StartTime;

            List<AlarmTimestampProjection> timestampRows = await BuildStatisticsRecordQuery(query, start, end)
                .Select(record => new AlarmTimestampProjection
                {
                    Timestamp = record.Timestamp
                })
                .ToListAsync()
                .ConfigureAwait(false);

            List<AlarmTypeDistributionProjection> typeRows = await BuildStatisticsRecordQuery(query, start, end)
                .GroupBy(record => new { record.Code, record.Name, record.Category })
                .Select(record => new AlarmTypeDistributionProjection
                {
                    Code = record.Code,
                    Name = record.Name,
                    Category = record.Category,
                    Count = SqlFunc.AggregateCount(record.Code),
                    HighestSeverityValue = SqlFunc.AggregateMax(record.LevelValue),
                    LatestRaisedAt = SqlFunc.AggregateMax(record.LastRaisedAt)
                })
                .ToListAsync()
                .ConfigureAwait(false);

            List<AlarmSourceDistributionProjection> sourceRows = await BuildStatisticsRecordQuery(query, start, end)
                .GroupBy(record => record.Source)
                .Select(record => new AlarmSourceDistributionProjection
                {
                    Source = record.Source,
                    Count = SqlFunc.AggregateCount(record.Source),
                    HighestSeverityValue = SqlFunc.AggregateMax(record.LevelValue),
                    LatestRaisedAt = SqlFunc.AggregateMax(record.LastRaisedAt)
                })
                .ToListAsync()
                .ConfigureAwait(false);

            DateTime bucketStart = NormalizeBucketStart(start, query.Bucket);
            DateTime bucketEnd = NormalizeBucketStart(end, query.Bucket);
            Dictionary<DateTime, int> trendMap = timestampRows
                .GroupBy(record => NormalizeBucketStart(record.Timestamp, query.Bucket))
                .ToDictionary(group => group.Key, group => group.Count());

            List<AlarmTrendPoint> trendPoints = new List<AlarmTrendPoint>();
            for (DateTime cursor = bucketStart; cursor <= bucketEnd; cursor = NextBucket(cursor, query.Bucket))
            {
                trendMap.TryGetValue(cursor, out int count);
                trendPoints.Add(new AlarmTrendPoint
                {
                    BucketStart = cursor,
                    Count = count
                });
            }

            IReadOnlyList<AlarmTypeDistributionItem> typeDistribution = typeRows
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Code)
                .Take(Math.Max(1, query.TopCount))
                .Select(item => new AlarmTypeDistributionItem
                {
                    Code = item.Code,
                    Name = item.Name,
                    Category = item.Category,
                    Count = item.Count,
                    HighestSeverity = (AlarmSeverity)item.HighestSeverityValue,
                    LatestRaisedAt = item.LatestRaisedAt
                })
                .ToArray();

            IReadOnlyList<AlarmSourceDistributionItem> sourceDistribution = sourceRows
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Source)
                .Take(Math.Max(1, query.TopCount))
                .Select(item => new AlarmSourceDistributionItem
                {
                    Source = item.Source,
                    Count = item.Count,
                    HighestSeverity = (AlarmSeverity)item.HighestSeverityValue,
                    LatestRaisedAt = item.LatestRaisedAt
                })
                .ToArray();

            return new AlarmStatisticsResult
            {
                RangeStart = start,
                RangeEnd = end,
                Bucket = query.Bucket,
                TrendPoints = trendPoints,
                TypeDistribution = typeDistribution,
                SourceDistribution = sourceDistribution
            };
        }

        public async Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            List<string> persistedSources = await _database.Queryable<AlarmRecordEntity>()
                .Select(record => record.Source)
                .Distinct()
                .ToListAsync()
                .ConfigureAwait(false);

            lock (_stateGate)
            {
                return persistedSources
                    .Concat(_activeByKey.Values.Select(item => item.Source))
                    .Where(source => !string.IsNullOrWhiteSpace(source))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(source => source)
                    .ToArray();
            }
        }

        public async Task<IReadOnlyList<AlarmRealtimeEntry>> GetRealtimeFeedAsync(int maxCount = 200, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            lock (_stateGate)
            {
                return _realtimeFeed
                    .TakeLast(Math.Max(1, Math.Min(maxCount, MaxCacheCount)))
                    .Select(entry => new AlarmRealtimeEntry
                    {
                        ActiveId = entry.ActiveId,
                        Code = entry.Code,
                        Name = entry.Name,
                        Message = entry.Message,
                        Source = entry.Source,
                        Location = entry.Location,
                        Severity = entry.Severity,
                        EventKind = entry.EventKind,
                        EventTime = entry.EventTime,
                        OccurrenceCount = entry.OccurrenceCount
                    })
                    .ToArray();
            }
        }

        public async Task<string> ExportHistoryAsync(AlarmHistoryQuery query, string outputDirectory, AlarmExportFormat format = AlarmExportFormat.Csv, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);

            query ??= new AlarmHistoryQuery();
            (DateTime? start, DateTime? end) = NormalizeRange(query.StartTime, query.EndTime);
            List<AlarmHistoryEntry> history = (await BuildHistoryQuery(query, start, end)
                    .OrderBy(record => record.Timestamp, OrderByType.Desc)
                    .ToListAsync()
                    .ConfigureAwait(false))
                .Select(MapToHistoryEntry)
                .ToList();

            string targetDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(PrismProvider.AppBasePath, "Export", "Alarm")
                : outputDirectory;
            Directory.CreateDirectory(targetDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string extension = format == AlarmExportFormat.ExcelXml ? ".xls" : ".csv";
            string filePath = Path.Combine(targetDirectory, $"alarm_history_{timestamp}{extension}");

            if (format == AlarmExportFormat.ExcelXml)
            {
                await WriteExcelXmlAsync(filePath, history, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteCsvAsync(filePath, history, cancellationToken).ConfigureAwait(false);
            }

            return filePath;
        }

        #endregion

        #region 内部状态辅助方法

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            InitializeAsync().GetAwaiter().GetResult();
        }

        private static string ToSourceType(AlarmSourceKind sourceKind)
        {
            return sourceKind switch
            {
                AlarmSourceKind.Software => "Software",
                AlarmSourceKind.Plc => "PLC",
                AlarmSourceKind.MotionCard => "MotionCard",
                AlarmSourceKind.Sensor => "Sensor",
                AlarmSourceKind.Camera => "Camera",
                AlarmSourceKind.System => "System",
                AlarmSourceKind.Hardware => "Hardware",
                _ => string.Empty
            };
        }

        private static void ValidateRaiseRequest(AlarmRaiseRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new ArgumentException("Alarm code is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Alarm message is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Source))
            {
                throw new ArgumentException("Alarm source is required.", nameof(request));
            }
        }

        private static AlarmInfo CreateGovernedSnapshot(AlarmRaiseRequest request, bool isActive, string state, string? note)
        {
            Dictionary<string, object?> extraData = new Dictionary<string, object?>(
                request.ExtraData ?? new Dictionary<string, object?>(),
                StringComparer.OrdinalIgnoreCase)
            {
                ["GovernanceState"] = state
            };

            if (!string.IsNullOrWhiteSpace(note))
            {
                extraData["GovernanceNote"] = note.Trim();
            }

            DateTime now = DateTime.Now;
            return new AlarmInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                Code = request.Code.Trim(),
                Name = GetPreferredValue(request.Name, request.Code),
                Category = request.Category?.Trim() ?? string.Empty,
                Message = request.Message.Trim(),
                Level = request.Level,
                Source = request.Source.Trim(),
                Location = request.Location?.Trim() ?? string.Empty,
                Timestamp = now,
                LastRaisedAt = now,
                IsActive = isActive,
                IsConfirmed = !request.NeedAcknowledge,
                NeedAcknowledge = request.NeedAcknowledge,
                PopupMode = request.PopupMode,
                PopupThrottleSeconds = Math.Max(0, request.PopupThrottleSeconds),
                AllowManualClear = request.AllowManualClear,
                OccurrenceCount = 1,
                Note = note ?? string.Empty,
                ExtraData = extraData
            };
        }

        private static AlarmEventAuditInfo CreateAuditInfo(
            AlarmInfo alarm,
            string action,
            string operatorName,
            string note,
            DateTime occurredAt,
            string extraDataJson = "")
        {
            return new AlarmEventAuditInfo
            {
                ActiveId = alarm.Id,
                Action = action,
                Code = alarm.Code,
                Source = alarm.Source,
                Location = alarm.Location,
                Severity = alarm.Level,
                Message = alarm.Message,
                OperatorName = operatorName,
                Note = note,
                ExtraDataJson = string.IsNullOrWhiteSpace(extraDataJson)
                    ? JsonConvert.SerializeObject(alarm.ExtraData ?? new Dictionary<string, object?>())
                    : extraDataJson,
                OccurredAt = occurredAt
            };
        }

        private void WriteAudit(AlarmEventAuditInfo audit)
        {
            if (audit == null)
            {
                return;
            }

            _writeChannel.Writer.TryWrite(new PersistenceWorkItem(PersistenceAction.AuditOnly, null, new[] { audit }));
        }

        private static string BuildActiveKey(string code, string source, string? location)
        {
            return $"{source?.Trim()}|{code?.Trim()}|{location?.Trim()}";
        }

        private static AlarmPopupMode NormalizePopupMode(int popupModeValue, AlarmSeverity severity, bool needAcknowledge)
        {
            return Enum.IsDefined(typeof(AlarmPopupMode), popupModeValue)
                ? (AlarmPopupMode)popupModeValue
                : GetDefaultPopupMode(severity, needAcknowledge);
        }

        private static AlarmPopupMode GetDefaultPopupMode(AlarmSeverity severity, bool needAcknowledge)
        {
            if (severity >= AlarmSeverity.Fatal || needAcknowledge)
            {
                return AlarmPopupMode.Modal;
            }

            return severity >= AlarmSeverity.Warning
                ? AlarmPopupMode.Growl
                : AlarmPopupMode.None;
        }

        private static string GetPreferredValue(string? primary, string fallback, string emptyFallback = "")
        {
            return !string.IsNullOrWhiteSpace(primary)
                ? primary.Trim()
                : (!string.IsNullOrWhiteSpace(fallback) ? fallback : emptyFallback);
        }

        private static void MergeExtraData(IDictionary<string, object?> target, IDictionary<string, object?> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object?> pair in source)
            {
                target[pair.Key] = pair.Value;
            }
        }

        private static bool ContainsKeyword(AlarmInfo info, string keyword)
        {
            return Contains(info.Code, keyword) ||
                   Contains(info.Name, keyword) ||
                   Contains(info.Message, keyword) ||
                   Contains(info.Source, keyword) ||
                   Contains(info.Location, keyword);
        }

        private async Task RemoveSimulatedActiveRecordsAsync(IEnumerable<AlarmRecordEntity> activeRecords)
        {
            List<string> simulatedIds = activeRecords
                .Where(IsSimulatedAlarmRecord)
                .Select(record => record.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string id in simulatedIds)
            {
                await _database.Deleteable<AlarmRecordEntity>()
                    .Where(record => record.Id == id)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }
        }

        private static bool IsSimulatedAlarmRecord(AlarmRecordEntity record)
        {
            if (record == null)
            {
                return false;
            }

            return HasSimulationCodePrefix(record.Code) ||
                   HasSimulationCodeSegment(record.ActiveKey) ||
                   IsSimulationExactValue(record.Source) ||
                   IsSimulationExactValue(record.Category) ||
                   IsSimulationExactValue(record.Name) ||
                   IsSimulationExactValue(record.Message) ||
                   HasSimulationExtraData(record.ExtraDataJson);
        }

        private static bool HasSimulationCodePrefix(string? code)
        {
            string value = code?.Trim() ?? string.Empty;
            return SimulationCodePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasSimulationCodeSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            char[] separators = { '|', '/', '\\', ':', ';', ',', ' ' };
            return value
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Any(HasSimulationCodePrefix);
        }

        private static bool IsSimulationExactValue(string? value)
        {
            string normalizedValue = value?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(normalizedValue) &&
                   SimulationExactValues.Any(marker => normalizedValue.Equals(marker, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasSimulationExtraData(string? extraDataJson)
        {
            if (string.IsNullOrWhiteSpace(extraDataJson))
            {
                return false;
            }

            try
            {
                JObject extraData = JObject.Parse(extraDataJson);
                foreach (JProperty property in extraData.Properties())
                {
                    if (IsSimulationExtraDataProperty(property.Name, property.Value))
                    {
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
            }

            return false;
        }

        private static bool IsSimulationExtraDataProperty(string name, JToken value)
        {
            if (SimulationBooleanKeys.Any(key => name.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                return IsTruthySimulationFlag(value);
            }

            return SimulationDescriptorKeys.Any(key => name.Equals(key, StringComparison.OrdinalIgnoreCase)) &&
                   IsSimulationExactValue(value.Type == JTokenType.String ? value.Value<string>() : value.ToString());
        }

        private static bool IsTruthySimulationFlag(JToken value)
        {
            if (value.Type == JTokenType.Boolean)
            {
                return value.Value<bool>();
            }

            if (value.Type == JTokenType.Integer)
            {
                return value.Value<int>() != 0;
            }

            string text = value.Type == JTokenType.String ? value.Value<string>() ?? string.Empty : value.ToString();
            return text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   IsSimulationExactValue(text);
        }

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static DateTime ResolveLatestEventTime(AlarmRecordEntity entity)
        {
            return entity.ClearTime ?? entity.ConfirmTime ?? entity.LastRaisedAt;
        }

        private static AlarmRealtimeEntry CreateHistoricalRealtimeEntry(AlarmRecordEntity entity)
        {
            DateTime eventTime = ResolveLatestEventTime(entity);
            AlarmEventKind eventKind = entity.ClearTime.HasValue
                ? AlarmEventKind.Cleared
                : entity.IsConfirmed
                    ? AlarmEventKind.Confirmed
                    : AlarmEventKind.Raised;

            return new AlarmRealtimeEntry
            {
                ActiveId = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Message = entity.Message,
                Source = entity.Source,
                Location = entity.Location,
                Severity = (AlarmSeverity)entity.LevelValue,
                EventKind = eventKind,
                EventTime = eventTime,
                OccurrenceCount = entity.OccurrenceCount,
                NeedAcknowledge = entity.NeedAcknowledge,
                IsConfirmed = entity.IsConfirmed,
                PopupMode = NormalizePopupMode(entity.PopupModeValue, (AlarmSeverity)entity.LevelValue, entity.NeedAcknowledge),
                PopupThrottleSeconds = Math.Max(0, entity.PopupThrottleSeconds)
            };
        }

        private AlarmRecordEntity? ClearUnsafe(string activeKey, string? user, string? note, DateTime clearTime)
        {
            // 调用方必须持有 _stateGate，因为这里会同时修改活动状态和实时事件流。
            if (!_activeByKey.TryGetValue(activeKey, out AlarmInfo? alarm))
            {
                return null;
            }

            alarm.IsActive = false;
            alarm.ClearTime = clearTime;
            alarm.ClearUser = user?.Trim() ?? string.Empty;
            alarm.Note = note?.Trim() ?? alarm.Note;

            _activeByKey.Remove(activeKey);

            AlarmRealtimeEntry realtimeEntry = CreateRealtimeEntry(alarm, AlarmEventKind.Cleared, clearTime);
            AppendRealtimeEntryUnsafe(realtimeEntry);

            return MapToEntity(alarm, activeKey);
        }

        private void RollDateWindowUnsafe(DateTime now)
        {
            if (_todayCounterDate.Date == now.Date)
            {
                return;
            }

            _todayCounterDate = now.Date;
            _todayRaisedCount = 0;
        }

        private void AppendRealtimeEntryUnsafe(AlarmRealtimeEntry entry)
        {
            _realtimeFeed.AddLast(entry);
            while (_realtimeFeed.Count > MaxCacheCount)
            {
                _realtimeFeed.RemoveFirst();
            }
        }

        private AlarmDashboardSnapshot BuildDashboardSnapshotUnsafe()
        {
            AlarmInfo[] active = _activeByKey.Values.ToArray();
            return new AlarmDashboardSnapshot
            {
                ActiveCount = active.Length,
                UnacknowledgedCount = active.Count(item => item.NeedAcknowledge && !item.IsConfirmed),
                FatalCount = active.Count(item => item.Level >= AlarmSeverity.Fatal),
                TodayRaisedCount = _todayRaisedCount,
                HistoryCount = (int)Math.Min(int.MaxValue, _historyCount),
                LatestRaisedAt = active.Length == 0 ? null : active.Max(item => item.LastRaisedAt)
            };
        }

        private IReadOnlyList<AlarmActiveRecord> BuildActiveSnapshotUnsafe()
        {
            return _activeByKey.Values
                .OrderByDescending(item => item.LastRaisedAt)
                .ThenByDescending(item => item.Timestamp)
                .Select(MapToActiveRecord)
                .ToArray();
        }

        private void PublishChange(
            AlarmDashboardSnapshot dashboard,
            IReadOnlyList<AlarmActiveRecord> activeSnapshot,
            AlarmRealtimeEntry? latestEvent)
        {
            if (latestEvent != null)
            {
                _eventAggregator.GetEvent<AlarmRealtimeEvent>().Publish(latestEvent);
            }

            DataChanged?.Invoke(this, new AlarmDataChangedEventArgs(dashboard, activeSnapshot, latestEvent));
        }

        private void QueuePersistence(PersistenceAction action, AlarmRecordEntity entity, params AlarmEventAuditInfo[] audits)
        {
            QueuePersistence(action, entity, (IReadOnlyList<AlarmEventAuditInfo>)audits);
        }

        private void QueuePersistence(PersistenceAction action, AlarmRecordEntity entity, IReadOnlyList<AlarmEventAuditInfo> audits)
        {
            if (entity == null)
            {
                return;
            }

            _writeChannel.Writer.TryWrite(new PersistenceWorkItem(action, entity, audits));
        }

        private ISugarQueryable<AlarmRecordEntity> BuildHistoryQuery(
            AlarmHistoryQuery query,
            DateTime? start,
            DateTime? end)
        {
            ISugarQueryable<AlarmRecordEntity> dbQuery = _database.Queryable<AlarmRecordEntity>();

            if (!query.IncludeActive)
            {
                dbQuery = dbQuery.Where(record => !record.IsActive);
            }

            if (start.HasValue)
            {
                DateTime startValue = start.Value;
                dbQuery = dbQuery.Where(record => record.Timestamp >= startValue);
            }

            if (end.HasValue)
            {
                DateTime endValue = end.Value;
                dbQuery = dbQuery.Where(record => record.Timestamp <= endValue);
            }

            if (query.Severity.HasValue)
            {
                int severity = (int)query.Severity.Value;
                dbQuery = dbQuery.Where(record => record.LevelValue == severity);
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                string source = query.Source.Trim();
                dbQuery = dbQuery.Where(record => record.Source == source);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                dbQuery = dbQuery.Where(record =>
                    record.Code.Contains(keyword) ||
                    record.Name.Contains(keyword) ||
                    record.Message.Contains(keyword) ||
                    record.Source.Contains(keyword) ||
                    record.Location.Contains(keyword));
            }

            return dbQuery;
        }

        private ISugarQueryable<AlarmRecordEntity> BuildStatisticsRecordQuery(
            AlarmStatisticsQuery query,
            DateTime start,
            DateTime end)
        {
            ISugarQueryable<AlarmRecordEntity> dbQuery = _database.Queryable<AlarmRecordEntity>()
                .Where(record => record.Timestamp >= start && record.Timestamp <= end);

            if (query.Severity.HasValue)
            {
                int severity = (int)query.Severity.Value;
                dbQuery = dbQuery.Where(record => record.LevelValue == severity);
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                string source = query.Source.Trim();
                dbQuery = dbQuery.Where(record => record.Source == source);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                dbQuery = dbQuery.Where(record =>
                    record.Code.Contains(keyword) ||
                    record.Name.Contains(keyword) ||
                    record.Message.Contains(keyword) ||
                    record.Source.Contains(keyword) ||
                    record.Location.Contains(keyword));
            }

            return dbQuery;
        }

        private static (DateTime? Start, DateTime? End) NormalizeRange(DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue && start > end)
            {
                return (end, start);
            }

            return (start, end);
        }

        private static AlarmRealtimeEntry CreateRealtimeEntry(AlarmInfo alarm, AlarmEventKind eventKind, DateTime eventTime)
        {
            return new AlarmRealtimeEntry
            {
                ActiveId = alarm.Id,
                Code = alarm.Code,
                Name = alarm.Name,
                Message = alarm.Message,
                Source = alarm.Source,
                Location = alarm.Location,
                Severity = alarm.Level,
                EventKind = eventKind,
                EventTime = eventTime,
                OccurrenceCount = alarm.OccurrenceCount,
                NeedAcknowledge = alarm.NeedAcknowledge,
                IsConfirmed = alarm.IsConfirmed,
                PopupMode = alarm.PopupMode,
                PopupThrottleSeconds = Math.Max(0, alarm.PopupThrottleSeconds)
            };
        }

        private static AlarmActiveRecord MapToActiveRecord(AlarmInfo info)
        {
            return new AlarmActiveRecord
            {
                ActiveId = info.Id,
                Code = info.Code,
                Name = info.Name,
                Category = info.Category,
                SourceName = info.Source,
                Location = info.Location,
                Severity = info.Level,
                RaisedAt = info.Timestamp,
                LastRaisedAt = info.LastRaisedAt,
                ActiveDuration = DateTime.Now - info.Timestamp,
                OccurrenceCount = info.OccurrenceCount,
                NeedAcknowledge = info.NeedAcknowledge,
                IsAcknowledged = info.IsConfirmed,
                PopupMode = info.PopupMode,
                PopupThrottleSeconds = Math.Max(0, info.PopupThrottleSeconds),
                AllowManualClear = info.AllowManualClear,
                Message = info.Message,
                Detail = JsonConvert.SerializeObject(info.ExtraData ?? new Dictionary<string, object?>())
            };
        }

        private static AlarmHistoryEntry MapToHistoryEntry(AlarmRecordEntity entity)
        {
            DateTime clearedAt = entity.ClearTime ?? entity.LastRaisedAt;
            return new AlarmHistoryEntry
            {
                ActiveId = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Category = entity.Category,
                SourceName = entity.Source,
                Location = entity.Location,
                Severity = (AlarmSeverity)entity.LevelValue,
                RaisedAt = entity.Timestamp,
                ClearedAt = clearedAt,
                Duration = clearedAt - entity.Timestamp,
                OccurrenceCount = entity.OccurrenceCount,
                WasAcknowledged = entity.IsConfirmed,
                Message = entity.Message,
                ConfirmUser = entity.ConfirmUser
            };
        }

        private static AlarmInfo MapToAlarmInfo(AlarmRecordEntity entity)
        {
            Dictionary<string, object?> extraData = string.IsNullOrWhiteSpace(entity.ExtraDataJson)
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : JsonConvert.DeserializeObject<Dictionary<string, object?>>(entity.ExtraDataJson) ??
                  new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            return new AlarmInfo
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Category = entity.Category,
                Message = entity.Message,
                Level = (AlarmSeverity)entity.LevelValue,
                Source = entity.Source,
                Location = entity.Location,
                Timestamp = entity.Timestamp,
                LastRaisedAt = entity.LastRaisedAt,
                ClearTime = entity.ClearTime,
                IsActive = entity.IsActive,
                IsConfirmed = entity.IsConfirmed,
                ConfirmUser = entity.ConfirmUser,
                ConfirmTime = entity.ConfirmTime,
                ClearUser = entity.ClearUser,
                Note = entity.Note,
                NeedAcknowledge = entity.NeedAcknowledge,
                PopupMode = NormalizePopupMode(entity.PopupModeValue, (AlarmSeverity)entity.LevelValue, entity.NeedAcknowledge),
                PopupThrottleSeconds = Math.Max(0, entity.PopupThrottleSeconds),
                AllowManualClear = entity.AllowManualClear,
                AcknowledgeResetMode = (AlarmAcknowledgeResetMode)entity.AcknowledgeResetModeValue,
                OccurrenceCount = entity.OccurrenceCount,
                ExtraData = extraData
            };
        }

        private static AlarmRecordEntity MapToEntity(AlarmInfo info, string activeKey)
        {
            return new AlarmRecordEntity
            {
                Id = info.Id,
                ActiveKey = activeKey,
                Code = info.Code,
                Name = info.Name,
                Category = info.Category,
                Message = info.Message,
                LevelValue = (int)info.Level,
                Source = info.Source,
                Location = info.Location,
                Timestamp = info.Timestamp,
                LastRaisedAt = info.LastRaisedAt,
                ClearTime = info.ClearTime,
                ConfirmTime = info.ConfirmTime,
                IsActive = info.IsActive,
                IsConfirmed = info.IsConfirmed,
                ConfirmUser = info.ConfirmUser,
                ClearUser = info.ClearUser,
                Note = info.Note,
                NeedAcknowledge = info.NeedAcknowledge,
                PopupModeValue = (int)info.PopupMode,
                PopupThrottleSeconds = Math.Max(0, info.PopupThrottleSeconds),
                AllowManualClear = info.AllowManualClear,
                AcknowledgeResetModeValue = (int)info.AcknowledgeResetMode,
                OccurrenceCount = info.OccurrenceCount,
                ExtraDataJson = JsonConvert.SerializeObject(info.ExtraData ?? new Dictionary<string, object?>())
            };
        }

        private static DateTime NormalizeBucketStart(DateTime timestamp, AlarmChartBucket bucket)
        {
            return bucket switch
            {
                AlarmChartBucket.Hour => new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, timestamp.Kind),
                AlarmChartBucket.Week => timestamp.Date.AddDays(-((7 + (int)timestamp.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
                AlarmChartBucket.Month => new DateTime(timestamp.Year, timestamp.Month, 1, 0, 0, 0, timestamp.Kind),
                _ => timestamp.Date
            };
        }

        private static DateTime NextBucket(DateTime current, AlarmChartBucket bucket)
        {
            return bucket switch
            {
                AlarmChartBucket.Hour => current.AddHours(1),
                AlarmChartBucket.Week => current.AddDays(7),
                AlarmChartBucket.Month => current.AddMonths(1),
                _ => current.AddDays(1)
            };
        }

        private static async Task WriteCsvAsync(string filePath, IReadOnlyList<AlarmHistoryEntry> history, CancellationToken cancellationToken)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Id,Code,Name,Level,Source,Location,RaisedAt,ClearedAt,DurationMinutes,Occurrences,Confirmed,ConfirmUser,Message");

            foreach (AlarmHistoryEntry entry in history)
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(string.Join(",",
                    EscapeCsv(entry.ActiveId),
                    EscapeCsv(entry.Code),
                    EscapeCsv(entry.Name),
                    EscapeCsv(entry.Severity.ToString()),
                    EscapeCsv(entry.SourceName),
                    EscapeCsv(entry.Location),
                    EscapeCsv(entry.RaisedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                    EscapeCsv(entry.ClearedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                    EscapeCsv(entry.Duration.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture)),
                    EscapeCsv(entry.OccurrenceCount.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(entry.WasAcknowledged.ToString()),
                    EscapeCsv(entry.ConfirmUser),
                    EscapeCsv(entry.Message)));
            }

            await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        private static async Task WriteExcelXmlAsync(string filePath, IReadOnlyList<AlarmHistoryEntry> history, CancellationToken cancellationToken)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            await using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using XmlWriter writer = XmlWriter.Create(stream, settings);

            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(null, "Workbook", "urn:schemas-microsoft-com:office:spreadsheet").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync("xmlns", "ss", null, "urn:schemas-microsoft-com:office:spreadsheet").ConfigureAwait(false);
            await writer.WriteStartElementAsync(null, "Worksheet", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync("ss", "Name", null, "AlarmHistory").ConfigureAwait(false);
            await writer.WriteStartElementAsync(null, "Table", null).ConfigureAwait(false);

            await WriteExcelRowAsync(writer, new[]
            {
                "Id",
                "Code",
                "Name",
                "Level",
                "Source",
                "Location",
                "RaisedAt",
                "ClearedAt",
                "DurationMinutes",
                "Occurrences",
                "Confirmed",
                "ConfirmUser",
                "Message"
            }).ConfigureAwait(false);

            foreach (AlarmHistoryEntry entry in history)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteExcelRowAsync(writer, new[]
                {
                    entry.ActiveId,
                    entry.Code,
                    entry.Name,
                    entry.Severity.ToString(),
                    entry.SourceName,
                    entry.Location,
                    entry.RaisedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    entry.ClearedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    entry.Duration.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture),
                    entry.OccurrenceCount.ToString(CultureInfo.InvariantCulture),
                    entry.WasAcknowledged.ToString(),
                    entry.ConfirmUser,
                    entry.Message
                }).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        private static async Task WriteExcelRowAsync(XmlWriter writer, IEnumerable<string> values)
        {
            await writer.WriteStartElementAsync(null, "Row", null).ConfigureAwait(false);
            foreach (string value in values)
            {
                await writer.WriteStartElementAsync(null, "Cell", null).ConfigureAwait(false);
                await writer.WriteStartElementAsync(null, "Data", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync("ss", "Type", null, "String").ConfigureAwait(false);
                await writer.WriteStringAsync(value ?? string.Empty).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private static string EscapeCsv(string? value)
        {
            string text = value ?? string.Empty;
            if (text.Contains('"'))
            {
                text = text.Replace("\"", "\"\"");
            }

            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? $"\"{text}\""
                : text;
        }

        #endregion

        #region Persistence queue
        private const int MaxPersistenceRetryCount = 3;

        private async Task ProcessPersistenceQueueAsync()
        {
            await foreach (PersistenceWorkItem workItem in _writeChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    await PersistAlarmRecordAsync(workItem).ConfigureAwait(false);
                    await PersistAuditsAsync(workItem.Audits).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logs.LogError($"Alarm persistence failed. Action={workItem.Action}, RecordId={workItem.Entity?.Id ?? string.Empty}, AuditCount={workItem.Audits.Count}, Attempt={workItem.Attempt}: {ex}");
                    if (workItem.Attempt < MaxPersistenceRetryCount)
                    {
                        await Task.Delay(100 * (workItem.Attempt + 1)).ConfigureAwait(false);
                        _writeChannel.Writer.TryWrite(workItem.NextAttempt());
                    }
                    // Runtime alarm state is already updated; persistence failures must not block equipment flow.
                }
            }
        }

        private async Task PersistAlarmRecordAsync(PersistenceWorkItem workItem)
        {
            if (workItem.Entity == null || workItem.Action == PersistenceAction.AuditOnly)
            {
                return;
            }

            if (workItem.Action == PersistenceAction.Insert)
            {
                try
                {
                    await _database.Insertable(workItem.Entity).ExecuteCommandAsync().ConfigureAwait(false);
                }
                catch
                {
                    int recoveredRows = await UpdateAlarmRecordAsync(workItem.Entity).ConfigureAwait(false);
                    if (recoveredRows == 0)
                    {
                        throw;
                    }
                }

                return;
            }

            int affectedRows = await UpdateAlarmRecordAsync(workItem.Entity).ConfigureAwait(false);

            if (affectedRows == 0)
            {
                await _database.Insertable(workItem.Entity).ExecuteCommandAsync().ConfigureAwait(false);
            }
        }

        private async Task<int> UpdateAlarmRecordAsync(AlarmRecordEntity entity)
        {
            return await _database.Updateable(entity)
                .Where(record => record.Id == entity.Id)
                .ExecuteCommandAsync()
                .ConfigureAwait(false);
        }

        private async Task PersistAuditsAsync(IReadOnlyList<AlarmEventAuditInfo> audits)
        {
            if (audits == null || audits.Count == 0)
            {
                return;
            }

            foreach (AlarmEventAuditInfo audit in audits)
            {
                if (await AuditExistsAsync(audit.Id).ConfigureAwait(false))
                {
                    continue;
                }

                await _governanceService.AppendAuditAsync(audit).ConfigureAwait(false);
            }
        }

        private async Task<bool> AuditExistsAsync(string auditId)
        {
            if (string.IsNullOrWhiteSpace(auditId))
            {
                return false;
            }

            return await _database.Queryable<AlarmEventAuditEntity>()
                .AnyAsync(item => item.Id == auditId)
                .ConfigureAwait(false);
        }

        private enum PersistenceAction
        {
            AuditOnly,
            Insert,
            Update
        }

        private readonly struct PersistenceWorkItem
        {
            public PersistenceWorkItem(
                PersistenceAction action,
                AlarmRecordEntity entity,
                IReadOnlyList<AlarmEventAuditInfo> audits,
                int attempt = 0)
            {
                Action = action;
                Entity = entity;
                Audits = audits ?? Array.Empty<AlarmEventAuditInfo>();
                Attempt = attempt;
            }

            public PersistenceAction Action { get; }

            public AlarmRecordEntity Entity { get; }

            public IReadOnlyList<AlarmEventAuditInfo> Audits { get; }

            public int Attempt { get; }

            public PersistenceWorkItem NextAttempt()
            {
                return new PersistenceWorkItem(Action, Entity, Audits, Attempt + 1);
            }
        }

        #endregion
    }
}

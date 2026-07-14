#nullable enable
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    [ExposedService(Lifetime.Singleton, 6, typeof(IAlarmGovernanceService))]
    public sealed class AlarmGovernanceService : IAlarmGovernanceService
    {
        private readonly ISqlSugarClient _database;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private readonly object _cacheGate = new object();
        private readonly List<AlarmSuppressionRuleInfo> _suppressionRules = new List<AlarmSuppressionRuleInfo>();
        private readonly List<AlarmShelveInfo> _shelves = new List<AlarmShelveInfo>();
        private readonly List<AlarmNotificationRouteInfo> _notificationRoutes = new List<AlarmNotificationRouteInfo>();
        private bool _initialized;

        public AlarmGovernanceService(ISqlSugarClient database)
        {
            _database = database;
        }

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

                List<AlarmSuppressionRuleEntity> suppressionEntities = await _database.Queryable<AlarmSuppressionRuleEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);
                List<AlarmShelveEntity> shelveEntities = await _database.Queryable<AlarmShelveEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);
                List<AlarmNotificationRouteEntity> routeEntities = await _database.Queryable<AlarmNotificationRouteEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);

                lock (_cacheGate)
                {
                    _suppressionRules.Clear();
                    _suppressionRules.AddRange(suppressionEntities.Select(MapToInfo));
                    _shelves.Clear();
                    _shelves.AddRange(shelveEntities.Select(MapToInfo));
                    _notificationRoutes.Clear();
                    _notificationRoutes.AddRange(routeEntities.Select(MapToInfo));
                    _initialized = true;
                }
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        public async Task<IReadOnlyList<AlarmSuppressionRuleInfo>> GetSuppressionRulesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmGovernanceQuery();

            lock (_cacheGate)
            {
                IEnumerable<AlarmSuppressionRuleInfo> rules = _suppressionRules;
                rules = ApplyGovernanceFilter(rules, query, item => item.CodePattern, item => item.SourcePattern, item => item.Enabled);
                return rules
                    .OrderByDescending(item => item.UpdatedAt)
                    .Take(NormalizeMax(query.MaxCount))
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task SaveSuppressionRuleAsync(AlarmSuppressionRuleInfo rule, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            NormalizeSuppressionRule(rule, operatorName);
            AlarmSuppressionRuleEntity entity = MapToEntity(rule);

            bool exists = await _database.Queryable<AlarmSuppressionRuleEntity>()
                .AnyAsync(item => item.Id == entity.Id)
                .ConfigureAwait(false);
            if (exists)
            {
                await _database.Updateable(entity)
                    .Where(item => item.Id == entity.Id)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                await _database.Insertable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }

            lock (_cacheGate)
            {
                UpsertCache(_suppressionRules, rule, item => item.Id);
            }
        }

        public async Task SetSuppressionRuleEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            AlarmSuppressionRuleInfo? rule;
            lock (_cacheGate)
            {
                rule = _suppressionRules.FirstOrDefault(item => item.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase))?.CreateCopy();
            }

            if (rule == null)
            {
                return;
            }

            rule.Enabled = enabled;
            await SaveSuppressionRuleAsync(rule, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AlarmShelveInfo>> GetShelvesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmGovernanceQuery();
            DateTime now = DateTime.Now;

            lock (_cacheGate)
            {
                IEnumerable<AlarmShelveInfo> shelves = _shelves;
                if (!query.IncludeInactive)
                {
                    shelves = shelves.Where(item => item.IsActive && item.ShelvedUntil >= now);
                }

                shelves = ApplyGovernanceFilter(shelves, query, item => item.CodePattern, item => item.SourcePattern, item => item.IsActive);
                return shelves
                    .OrderByDescending(item => item.ShelvedAt)
                    .Take(NormalizeMax(query.MaxCount))
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task SaveShelveAsync(AlarmShelveInfo shelf, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            NormalizeShelve(shelf, operatorName);
            AlarmShelveEntity entity = MapToEntity(shelf);

            bool exists = await _database.Queryable<AlarmShelveEntity>()
                .AnyAsync(item => item.Id == entity.Id)
                .ConfigureAwait(false);
            if (exists)
            {
                await _database.Updateable(entity)
                    .Where(item => item.Id == entity.Id)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                await _database.Insertable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }

            lock (_cacheGate)
            {
                UpsertCache(_shelves, shelf, item => item.Id);
            }
        }

        public async Task ReleaseShelveAsync(string id, string operatorName, string? note = null, CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            AlarmShelveInfo? shelf;
            lock (_cacheGate)
            {
                shelf = _shelves.FirstOrDefault(item => item.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase))?.CreateCopy();
            }

            if (shelf == null)
            {
                return;
            }

            shelf.IsActive = false;
            shelf.ReleasedAt = DateTime.Now;
            shelf.ReleasedBy = operatorName?.Trim() ?? string.Empty;
            shelf.ReleaseNote = note?.Trim() ?? string.Empty;
            await SaveShelveAsync(shelf, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AlarmNotificationRouteInfo>> GetNotificationRoutesAsync(AlarmGovernanceQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmGovernanceQuery();

            lock (_cacheGate)
            {
                IEnumerable<AlarmNotificationRouteInfo> routes = _notificationRoutes;
                routes = ApplyGovernanceFilter(routes, query, item => item.CodePattern, item => item.SourcePattern, item => item.Enabled);
                return routes
                    .OrderBy(item => item.Name)
                    .Take(NormalizeMax(query.MaxCount))
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task SaveNotificationRouteAsync(AlarmNotificationRouteInfo route, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            NormalizeRoute(route);
            AlarmNotificationRouteEntity entity = MapToEntity(route);

            bool exists = await _database.Queryable<AlarmNotificationRouteEntity>()
                .AnyAsync(item => item.Id == entity.Id)
                .ConfigureAwait(false);
            if (exists)
            {
                await _database.Updateable(entity)
                    .Where(item => item.Id == entity.Id)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                await _database.Insertable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);
            }

            lock (_cacheGate)
            {
                UpsertCache(_notificationRoutes, route, item => item.Id);
            }
        }

        public async Task SetNotificationRouteEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            AlarmNotificationRouteInfo? route;
            lock (_cacheGate)
            {
                route = _notificationRoutes.FirstOrDefault(item => item.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase))?.CreateCopy();
            }

            if (route == null)
            {
                return;
            }

            route.Enabled = enabled;
            await SaveNotificationRouteAsync(route, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AlarmEventAuditInfo>> GetAuditsAsync(AlarmAuditQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            query ??= new AlarmAuditQuery();
            ISugarQueryable<AlarmEventAuditEntity> dbQuery = _database.Queryable<AlarmEventAuditEntity>();

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                string action = query.Action.Trim();
                dbQuery = dbQuery.Where(item => item.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(query.Code))
            {
                string code = query.Code.Trim();
                dbQuery = dbQuery.Where(item => item.Code.Contains(code));
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                string source = query.Source.Trim();
                dbQuery = dbQuery.Where(item => item.Source.Contains(source));
            }

            if (query.StartTime.HasValue)
            {
                DateTime start = query.StartTime.Value;
                dbQuery = dbQuery.Where(item => item.OccurredAt >= start);
            }

            if (query.EndTime.HasValue)
            {
                DateTime end = query.EndTime.Value;
                dbQuery = dbQuery.Where(item => item.OccurredAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                dbQuery = dbQuery.Where(item =>
                    item.Action.Contains(keyword) ||
                    item.Code.Contains(keyword) ||
                    item.Source.Contains(keyword) ||
                    item.Location.Contains(keyword) ||
                    item.Message.Contains(keyword) ||
                    item.OperatorName.Contains(keyword) ||
                    item.Note.Contains(keyword));
            }

            List<AlarmEventAuditEntity> entities = await dbQuery
                .OrderBy(item => item.OccurredAt, OrderByType.Desc)
                .Take(NormalizeMax(query.MaxCount))
                .ToListAsync()
                .ConfigureAwait(false);

            return entities.Select(MapToInfo).ToArray();
        }

        public async Task AppendAuditAsync(AlarmEventAuditInfo audit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (audit == null)
            {
                return;
            }

            NormalizeAudit(audit);
            await _database.Insertable(MapToEntity(audit))
                .ExecuteCommandAsync()
                .ConfigureAwait(false);
        }

        public bool TryMatchSuppression(AlarmRaiseRequest request, DateTime now, out AlarmSuppressionRuleInfo? rule)
        {
            EnsureInitialized();
            lock (_cacheGate)
            {
                rule = _suppressionRules
                    .Where(item => item.Enabled && IsWithinWindow(item.EffectiveFrom, item.EffectiveTo, now))
                    .FirstOrDefault(item =>
                        Matches(item.CodePattern, request.Code) &&
                        Matches(item.SourcePattern, request.Source) &&
                        Matches(item.LocationPattern, request.Location))
                    ?.CreateCopy();
            }

            return rule != null;
        }

        public bool TryMatchShelve(AlarmRaiseRequest request, DateTime now, out AlarmShelveInfo? shelf)
        {
            EnsureInitialized();
            lock (_cacheGate)
            {
                shelf = _shelves
                    .Where(item => item.IsActive && item.ShelvedUntil >= now)
                    .FirstOrDefault(item =>
                        Matches(item.CodePattern, request.Code) &&
                        Matches(item.SourcePattern, request.Source) &&
                        Matches(item.LocationPattern, request.Location))
                    ?.CreateCopy();
            }

            return shelf != null;
        }

        public IReadOnlyList<AlarmNotificationRouteInfo> ResolveNotificationRoutes(AlarmRaiseRequest request, DateTime now)
        {
            EnsureInitialized();
            lock (_cacheGate)
            {
                return _notificationRoutes
                    .Where(item => item.Enabled &&
                                   request.Level >= item.MinSeverity &&
                                   !IsInQuietPeriod(item.QuietStart, item.QuietEnd, now) &&
                                   Matches(item.CodePattern, request.Code) &&
                                   Matches(item.SourcePattern, request.Source))
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            InitializeAsync().GetAwaiter().GetResult();
        }

        private static IEnumerable<T> ApplyGovernanceFilter<T>(
            IEnumerable<T> items,
            AlarmGovernanceQuery query,
            Func<T, string> codeSelector,
            Func<T, string> sourceSelector,
            Func<T, bool> enabledSelector)
        {
            if (query.Enabled.HasValue)
            {
                items = items.Where(item => enabledSelector(item) == query.Enabled.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Code))
            {
                string code = query.Code.Trim();
                items = items.Where(item => Contains(codeSelector(item), code));
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                string source = query.Source.Trim();
                items = items.Where(item => Contains(sourceSelector(item), source));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string keyword = query.Keyword.Trim();
                items = items.Where(item => Contains(codeSelector(item), keyword) || Contains(sourceSelector(item), keyword) || Contains(item?.ToString(), keyword));
            }

            return items;
        }

        private static void UpsertCache<T>(List<T> cache, T value, Func<T, string> keySelector)
        {
            string key = keySelector(value);
            int index = cache.FindIndex(item => keySelector(item).Equals(key, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                cache[index] = value;
            }
            else
            {
                cache.Add(value);
            }
        }

        private static void NormalizeSuppressionRule(AlarmSuppressionRuleInfo rule, string operatorName)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            DateTime now = DateTime.Now;
            rule.Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id.Trim();
            rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? BuildRuleName(rule.CodePattern, rule.SourcePattern) : rule.Name.Trim();
            rule.CodePattern = rule.CodePattern?.Trim() ?? string.Empty;
            rule.SourcePattern = rule.SourcePattern?.Trim() ?? string.Empty;
            rule.LocationPattern = rule.LocationPattern?.Trim() ?? string.Empty;
            rule.Reason = rule.Reason?.Trim() ?? string.Empty;
            rule.CreatedBy = string.IsNullOrWhiteSpace(rule.CreatedBy) ? operatorName?.Trim() ?? string.Empty : rule.CreatedBy.Trim();
            rule.CreatedAt = rule.CreatedAt == default ? now : rule.CreatedAt;
            rule.UpdatedAt = now;
        }

        private static void NormalizeShelve(AlarmShelveInfo shelf, string operatorName)
        {
            if (shelf == null)
            {
                throw new ArgumentNullException(nameof(shelf));
            }

            DateTime now = DateTime.Now;
            shelf.Id = string.IsNullOrWhiteSpace(shelf.Id) ? Guid.NewGuid().ToString("N") : shelf.Id.Trim();
            shelf.ActiveId = shelf.ActiveId?.Trim() ?? string.Empty;
            shelf.CodePattern = shelf.CodePattern?.Trim() ?? string.Empty;
            shelf.SourcePattern = shelf.SourcePattern?.Trim() ?? string.Empty;
            shelf.LocationPattern = shelf.LocationPattern?.Trim() ?? string.Empty;
            shelf.Reason = shelf.Reason?.Trim() ?? string.Empty;
            shelf.ShelvedBy = string.IsNullOrWhiteSpace(shelf.ShelvedBy) ? operatorName?.Trim() ?? string.Empty : shelf.ShelvedBy.Trim();
            shelf.ShelvedAt = shelf.ShelvedAt == default ? now : shelf.ShelvedAt;
            if (shelf.ShelvedUntil <= now)
            {
                shelf.ShelvedUntil = now.AddHours(1);
            }
        }

        private static void NormalizeRoute(AlarmNotificationRouteInfo route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            DateTime now = DateTime.Now;
            route.Id = string.IsNullOrWhiteSpace(route.Id) ? Guid.NewGuid().ToString("N") : route.Id.Trim();
            route.Name = string.IsNullOrWhiteSpace(route.Name) ? BuildRuleName(route.CodePattern, route.SourcePattern) : route.Name.Trim();
            route.CodePattern = route.CodePattern?.Trim() ?? string.Empty;
            route.SourcePattern = route.SourcePattern?.Trim() ?? string.Empty;
            route.Channels = route.Channels?.Trim() ?? string.Empty;
            route.Receivers = route.Receivers?.Trim() ?? string.Empty;
            route.QuietStart = route.QuietStart?.Trim() ?? string.Empty;
            route.QuietEnd = route.QuietEnd?.Trim() ?? string.Empty;
            route.CreatedAt = route.CreatedAt == default ? now : route.CreatedAt;
            route.UpdatedAt = now;
        }

        private static void NormalizeAudit(AlarmEventAuditInfo audit)
        {
            DateTime now = DateTime.Now;
            audit.Id = string.IsNullOrWhiteSpace(audit.Id) ? Guid.NewGuid().ToString("N") : audit.Id.Trim();
            audit.Action = string.IsNullOrWhiteSpace(audit.Action) ? "Unknown" : audit.Action.Trim();
            audit.Code = audit.Code?.Trim() ?? string.Empty;
            audit.Source = audit.Source?.Trim() ?? string.Empty;
            audit.Location = audit.Location?.Trim() ?? string.Empty;
            audit.Message = audit.Message?.Trim() ?? string.Empty;
            audit.OperatorName = audit.OperatorName?.Trim() ?? string.Empty;
            audit.Note = audit.Note?.Trim() ?? string.Empty;
            audit.ExtraDataJson = audit.ExtraDataJson?.Trim() ?? string.Empty;
            audit.OccurredAt = audit.OccurredAt == default ? now : audit.OccurredAt;
        }

        private static string BuildRuleName(string? codePattern, string? sourcePattern)
        {
            string code = string.IsNullOrWhiteSpace(codePattern) ? "*" : codePattern.Trim();
            string source = string.IsNullOrWhiteSpace(sourcePattern) ? "*" : sourcePattern.Trim();
            return $"{source}/{code}";
        }

        private static bool IsWithinWindow(DateTime? start, DateTime? end, DateTime now)
        {
            return (!start.HasValue || now >= start.Value) && (!end.HasValue || now <= end.Value);
        }

        private static bool IsInQuietPeriod(string? startText, string? endText, DateTime now)
        {
            if (!TimeSpan.TryParse(startText, CultureInfo.InvariantCulture, out TimeSpan start) ||
                !TimeSpan.TryParse(endText, CultureInfo.InvariantCulture, out TimeSpan end))
            {
                return false;
            }

            TimeSpan current = now.TimeOfDay;
            return start <= end
                ? current >= start && current <= end
                : current >= start || current <= end;
        }

        private static bool Matches(string? pattern, string? value)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return true;
            }

            string normalizedPattern = pattern.Trim();
            string normalizedValue = value?.Trim() ?? string.Empty;
            if (normalizedPattern == "*")
            {
                return true;
            }

            if (normalizedPattern.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                string regex = "^" + Regex.Escape(normalizedPattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                return Regex.IsMatch(normalizedValue, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return normalizedPattern.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int NormalizeMax(int maxCount)
        {
            return maxCount <= 0 ? 500 : Math.Min(maxCount, 1000);
        }

        private static AlarmSuppressionRuleInfo MapToInfo(AlarmSuppressionRuleEntity entity)
        {
            return new AlarmSuppressionRuleInfo
            {
                Id = entity.Id,
                Name = entity.Name,
                CodePattern = entity.CodePattern,
                SourcePattern = entity.SourcePattern,
                LocationPattern = entity.LocationPattern,
                Reason = entity.Reason,
                Enabled = entity.Enabled,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static AlarmSuppressionRuleEntity MapToEntity(AlarmSuppressionRuleInfo info)
        {
            return new AlarmSuppressionRuleEntity
            {
                Id = info.Id,
                Name = info.Name,
                CodePattern = info.CodePattern,
                SourcePattern = info.SourcePattern,
                LocationPattern = info.LocationPattern,
                Reason = info.Reason,
                Enabled = info.Enabled,
                EffectiveFrom = info.EffectiveFrom,
                EffectiveTo = info.EffectiveTo,
                CreatedBy = info.CreatedBy,
                CreatedAt = info.CreatedAt,
                UpdatedAt = info.UpdatedAt
            };
        }

        private static AlarmShelveInfo MapToInfo(AlarmShelveEntity entity)
        {
            return new AlarmShelveInfo
            {
                Id = entity.Id,
                ActiveId = entity.ActiveId,
                CodePattern = entity.CodePattern,
                SourcePattern = entity.SourcePattern,
                LocationPattern = entity.LocationPattern,
                Reason = entity.Reason,
                ShelvedBy = entity.ShelvedBy,
                ShelvedAt = entity.ShelvedAt,
                ShelvedUntil = entity.ShelvedUntil,
                IsActive = entity.IsActive,
                ReleasedBy = entity.ReleasedBy,
                ReleasedAt = entity.ReleasedAt,
                ReleaseNote = entity.ReleaseNote
            };
        }

        private static AlarmShelveEntity MapToEntity(AlarmShelveInfo info)
        {
            return new AlarmShelveEntity
            {
                Id = info.Id,
                ActiveId = info.ActiveId,
                CodePattern = info.CodePattern,
                SourcePattern = info.SourcePattern,
                LocationPattern = info.LocationPattern,
                Reason = info.Reason,
                ShelvedBy = info.ShelvedBy,
                ShelvedAt = info.ShelvedAt,
                ShelvedUntil = info.ShelvedUntil,
                IsActive = info.IsActive,
                ReleasedBy = info.ReleasedBy,
                ReleasedAt = info.ReleasedAt,
                ReleaseNote = info.ReleaseNote
            };
        }

        private static AlarmNotificationRouteInfo MapToInfo(AlarmNotificationRouteEntity entity)
        {
            return new AlarmNotificationRouteInfo
            {
                Id = entity.Id,
                Name = entity.Name,
                CodePattern = entity.CodePattern,
                SourcePattern = entity.SourcePattern,
                MinSeverity = (AlarmSeverity)entity.MinSeverityValue,
                Channels = entity.Channels,
                Receivers = entity.Receivers,
                QuietStart = entity.QuietStart,
                QuietEnd = entity.QuietEnd,
                Enabled = entity.Enabled,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static AlarmNotificationRouteEntity MapToEntity(AlarmNotificationRouteInfo info)
        {
            return new AlarmNotificationRouteEntity
            {
                Id = info.Id,
                Name = info.Name,
                CodePattern = info.CodePattern,
                SourcePattern = info.SourcePattern,
                MinSeverityValue = (int)info.MinSeverity,
                Channels = info.Channels,
                Receivers = info.Receivers,
                QuietStart = info.QuietStart,
                QuietEnd = info.QuietEnd,
                Enabled = info.Enabled,
                CreatedAt = info.CreatedAt,
                UpdatedAt = info.UpdatedAt
            };
        }

        private static AlarmEventAuditInfo MapToInfo(AlarmEventAuditEntity entity)
        {
            return new AlarmEventAuditInfo
            {
                Id = entity.Id,
                ActiveId = entity.ActiveId,
                Action = entity.Action,
                Code = entity.Code,
                Source = entity.Source,
                Location = entity.Location,
                Severity = (AlarmSeverity)entity.SeverityValue,
                Message = entity.Message,
                OperatorName = entity.OperatorName,
                Note = entity.Note,
                ExtraDataJson = entity.ExtraDataJson,
                OccurredAt = entity.OccurredAt
            };
        }

        private static AlarmEventAuditEntity MapToEntity(AlarmEventAuditInfo info)
        {
            return new AlarmEventAuditEntity
            {
                Id = info.Id,
                ActiveId = info.ActiveId,
                Action = info.Action,
                Code = info.Code,
                Source = info.Source,
                Location = info.Location,
                SeverityValue = (int)info.Severity,
                Message = info.Message,
                OperatorName = info.OperatorName,
                Note = info.Note,
                ExtraDataJson = info.ExtraDataJson,
                OccurredAt = info.OccurredAt
            };
        }
    }
}

#nullable enable
using Newtonsoft.Json;
using ReeYin_V.Core.IOC;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    [ExposedService(Lifetime.Singleton, 6, typeof(IHardwareAlarmRuleService))]
    public sealed class HardwareAlarmRuleService : IHardwareAlarmRuleService
    {
        private readonly ISqlSugarClient _database;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, HardwareAlarmRuleInfo> _cache = new Dictionary<string, HardwareAlarmRuleInfo>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        public HardwareAlarmRuleService(ISqlSugarClient database)
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

                await SeedDefaultsAsync().ConfigureAwait(false);
                List<HardwareAlarmRuleEntity> entities = await _database.Queryable<HardwareAlarmRuleEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);

                lock (_cache)
                {
                    _cache.Clear();
                    foreach (HardwareAlarmRuleEntity entity in entities)
                    {
                        HardwareAlarmRuleInfo info = MapToInfo(entity);
                        _cache[info.Id] = info;
                    }

                    _initialized = true;
                }
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        public async Task<IReadOnlyList<HardwareAlarmRuleInfo>> GetRulesAsync(HardwareAlarmRuleQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new HardwareAlarmRuleQuery();
            string keyword = query.Keyword?.Trim() ?? string.Empty;
            string definitionCode = query.DefinitionCode?.Trim() ?? string.Empty;
            string sourceType = query.SourceType?.Trim() ?? string.Empty;
            string source = query.Source?.Trim() ?? string.Empty;

            lock (_cache)
            {
                IEnumerable<HardwareAlarmRuleInfo> rules = _cache.Values;
                if (!query.IncludeSystem)
                {
                    rules = rules.Where(item => !item.IsSystem);
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    rules = rules.Where(item =>
                        Contains(item.Id, keyword) ||
                        Contains(item.DefinitionCode, keyword) ||
                        Contains(item.Name, keyword) ||
                        Contains(item.SourceType, keyword) ||
                        Contains(item.TriggerField, keyword));
                }

                if (!string.IsNullOrWhiteSpace(definitionCode))
                {
                    rules = rules.Where(item => item.DefinitionCode.Equals(definitionCode, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(sourceType))
                {
                    rules = rules.Where(item => item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(source))
                {
                    rules = rules.Where(item => PatternMatches(item.SourcePattern, source));
                }

                if (query.Enabled.HasValue)
                {
                    rules = rules.Where(item => item.Enabled == query.Enabled.Value);
                }

                return rules
                    .OrderBy(item => item.Priority)
                    .ThenBy(item => item.Name)
                    .Take(query.MaxCount <= 0 ? 500 : query.MaxCount)
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task<HardwareAlarmRuleInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            lock (_cache)
            {
                return _cache.TryGetValue(id.Trim(), out HardwareAlarmRuleInfo? rule)
                    ? rule.CreateCopy()
                    : null;
            }
        }

        public async Task SaveAsync(HardwareAlarmRuleInfo rule, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            NormalizeRule(rule);
            HardwareAlarmRuleEntity entity = MapToEntity(rule);

            bool exists = await _database.Queryable<HardwareAlarmRuleEntity>()
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

            lock (_cache)
            {
                _cache[rule.Id] = rule.CreateCopy();
            }
        }

        public async Task SetEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            HardwareAlarmRuleInfo? rule = await FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (rule == null)
            {
                return;
            }

            rule.Enabled = enabled;
            await SaveAsync(rule, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public IReadOnlyList<HardwareAlarmRuleInfo> GetEnabledRulesSnapshot()
        {
            EnsureInitialized();
            lock (_cache)
            {
                return _cache.Values
                    .Where(item => item.Enabled)
                    .OrderBy(item => item.Priority)
                    .ThenBy(item => item.Name)
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeAsync().GetAwaiter().GetResult();
            }
        }

        private async Task SeedDefaultsAsync()
        {
            foreach (HardwareAlarmRuleInfo rule in HardwareAlarmRuleDefaults.CreateDefaults())
            {
                bool exists = await _database.Queryable<HardwareAlarmRuleEntity>()
                    .AnyAsync(item => item.Id == rule.Id || item.DefinitionCode == rule.DefinitionCode)
                    .ConfigureAwait(false);
                if (!exists)
                {
                    await _database.Insertable(MapToEntity(rule))
                        .ExecuteCommandAsync()
                        .ConfigureAwait(false);
                }
            }
        }

        private static HardwareAlarmRuleInfo MapToInfo(HardwareAlarmRuleEntity entity)
        {
            return new HardwareAlarmRuleInfo
            {
                Id = entity.Id,
                DefinitionCode = entity.DefinitionCode,
                Name = entity.Name,
                SourceType = entity.SourceType,
                SourcePattern = entity.SourcePattern,
                LocationPattern = entity.LocationPattern,
                TriggerKind = ParseEnum(entity.TriggerKind, HardwareAlarmTriggerKind.State),
                TriggerField = entity.TriggerField,
                Operator = ParseEnum(entity.Operator, HardwareAlarmOperator.Equals),
                TriggerValue = entity.TriggerValue,
                ClearKind = ParseEnum(entity.ClearKind, HardwareAlarmClearKind.StateRecovery),
                ClearValue = entity.ClearValue,
                DebounceMilliseconds = entity.DebounceMilliseconds,
                ThrottleSeconds = entity.ThrottleSeconds,
                LatchMode = entity.LatchMode,
                Enabled = entity.Enabled,
                IsSystem = entity.IsSystem,
                Priority = entity.Priority,
                ExtraTemplate = DeserializeExtraTemplate(entity.ExtraTemplateJson),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static HardwareAlarmRuleEntity MapToEntity(HardwareAlarmRuleInfo info)
        {
            return new HardwareAlarmRuleEntity
            {
                Id = info.Id.Trim(),
                DefinitionCode = info.DefinitionCode.Trim(),
                Name = info.Name?.Trim() ?? info.DefinitionCode.Trim(),
                SourceType = info.SourceType?.Trim() ?? string.Empty,
                SourcePattern = string.IsNullOrWhiteSpace(info.SourcePattern) ? "*" : info.SourcePattern.Trim(),
                LocationPattern = string.IsNullOrWhiteSpace(info.LocationPattern) ? "*" : info.LocationPattern.Trim(),
                TriggerKind = info.TriggerKind.ToString(),
                TriggerField = info.TriggerField?.Trim() ?? string.Empty,
                Operator = info.Operator.ToString(),
                TriggerValue = info.TriggerValue?.Trim() ?? string.Empty,
                ClearKind = info.ClearKind.ToString(),
                ClearValue = info.ClearValue?.Trim() ?? string.Empty,
                DebounceMilliseconds = Math.Max(0, info.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, info.ThrottleSeconds),
                LatchMode = info.LatchMode,
                Enabled = info.Enabled,
                IsSystem = info.IsSystem,
                Priority = info.Priority,
                ExtraTemplateJson = JsonConvert.SerializeObject(info.ExtraTemplate ?? new Dictionary<string, object?>()),
                CreatedAt = info.CreatedAt == default ? DateTime.Now : info.CreatedAt,
                UpdatedAt = info.UpdatedAt == default ? DateTime.Now : info.UpdatedAt
            };
        }

        private static void NormalizeRule(HardwareAlarmRuleInfo rule)
        {
            if (string.IsNullOrWhiteSpace(rule.DefinitionCode))
            {
                throw new ArgumentException("Hardware alarm rule definition code is required.", nameof(rule));
            }

            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString("N");
            }

            rule.Id = rule.Id.Trim();
            rule.DefinitionCode = rule.DefinitionCode.Trim();
            rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? rule.DefinitionCode : rule.Name.Trim();
            rule.SourceType = rule.SourceType?.Trim() ?? string.Empty;
            rule.SourcePattern = string.IsNullOrWhiteSpace(rule.SourcePattern) ? "*" : rule.SourcePattern.Trim();
            rule.LocationPattern = string.IsNullOrWhiteSpace(rule.LocationPattern) ? "*" : rule.LocationPattern.Trim();
            rule.TriggerField = rule.TriggerField?.Trim() ?? string.Empty;
            rule.TriggerValue = rule.TriggerValue?.Trim() ?? string.Empty;
            rule.ClearValue = rule.ClearValue?.Trim() ?? string.Empty;
            rule.DebounceMilliseconds = Math.Max(0, rule.DebounceMilliseconds);
            rule.ThrottleSeconds = Math.Max(0, rule.ThrottleSeconds);
            rule.ExtraTemplate ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (rule.CreatedAt == default)
            {
                rule.CreatedAt = DateTime.Now;
            }

            rule.UpdatedAt = DateTime.Now;
        }

        private static IDictionary<string, object?> DeserializeExtraTemplate(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ??
                       new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue)
            where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum result) && Enum.IsDefined(typeof(TEnum), result)
                ? result
                : defaultValue;
        }

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool PatternMatches(string? pattern, string value)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
            {
                return true;
            }

            string normalizedPattern = pattern.Trim();
            if (!normalizedPattern.Contains("*"))
            {
                return normalizedPattern.Equals(value, StringComparison.OrdinalIgnoreCase);
            }

            bool startsWithWildcard = normalizedPattern.StartsWith("*", StringComparison.Ordinal);
            bool endsWithWildcard = normalizedPattern.EndsWith("*", StringComparison.Ordinal);
            string[] parts = normalizedPattern.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return true;
            }

            int position = 0;
            int firstMiddlePart = 0;
            int lastMiddlePart = parts.Length - 1;
            int searchLimit = value.Length;

            if (!startsWithWildcard)
            {
                if (!value.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                position = parts[0].Length;
                firstMiddlePart = 1;
            }

            if (!endsWithWildcard && lastMiddlePart >= firstMiddlePart)
            {
                string lastPart = parts[lastMiddlePart];
                if (!value.EndsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                searchLimit = value.Length - lastPart.Length;
                lastMiddlePart--;
            }

            if (position > searchLimit)
            {
                return false;
            }

            for (int index = firstMiddlePart; index <= lastMiddlePart; index++)
            {
                string part = parts[index];
                int found = value.IndexOf(part, position, StringComparison.OrdinalIgnoreCase);
                if (found < 0 || found + part.Length > searchLimit)
                {
                    return false;
                }

                position = found + part.Length;
            }

            return true;
        }
    }
}

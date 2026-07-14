#nullable enable
using Newtonsoft.Json;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    [ExposedService(Lifetime.Singleton, 5, typeof(IAlarmDefinitionService))]
    public sealed class AlarmDefinitionService : IAlarmDefinitionService
    {
        private const string SoftwareSourceType = "Software";

        private readonly ISqlSugarClient _database;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, AlarmDefinitionInfo> _cache = new Dictionary<string, AlarmDefinitionInfo>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        public AlarmDefinitionService(ISqlSugarClient database)
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
                List<AlarmDefinitionEntity> entities = await _database.Queryable<AlarmDefinitionEntity>().ToListAsync().ConfigureAwait(false);
                lock (_cache)
                {
                    _cache.Clear();
                    foreach (AlarmDefinitionEntity entity in entities)
                    {
                        AlarmDefinitionInfo info = MapToInfo(entity);
                        _cache[info.Code] = info;
                    }

                    _initialized = true;
                }
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        public async Task<IReadOnlyList<AlarmDefinitionInfo>> GetDefinitionsAsync(AlarmDefinitionQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmDefinitionQuery();
            string keyword = query.Keyword?.Trim() ?? string.Empty;
            string sourceType = query.SourceType?.Trim() ?? string.Empty;
            string category = query.Category?.Trim() ?? string.Empty;

            lock (_cache)
            {
                IEnumerable<AlarmDefinitionInfo> definitions = _cache.Values;
                if (!query.IncludeSystem)
                {
                    definitions = definitions.Where(item => !item.IsSystem);
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    definitions = definitions.Where(item =>
                        Contains(item.Code, keyword) ||
                        Contains(item.Name, keyword) ||
                        Contains(item.Category, keyword) ||
                        Contains(item.SourceType, keyword));
                }

                if (!string.IsNullOrWhiteSpace(sourceType))
                {
                    definitions = definitions.Where(item => item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    definitions = definitions.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                if (query.Severity.HasValue)
                {
                    definitions = definitions.Where(item => item.Severity == query.Severity.Value);
                }

                if (query.Enabled.HasValue)
                {
                    definitions = definitions.Where(item => item.Enabled == query.Enabled.Value);
                }

                return definitions
                    .OrderBy(item => item.Code)
                    .Take(query.MaxCount <= 0 ? 500 : query.MaxCount)
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task<AlarmDefinitionInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            lock (_cache)
            {
                return _cache.TryGetValue(code.Trim(), out AlarmDefinitionInfo? definition)
                    ? definition.CreateCopy()
                    : null;
            }
        }

        public async Task SaveAsync(AlarmDefinitionInfo definition, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(definition.Code))
            {
                throw new ArgumentException("Alarm definition code is required.", nameof(definition));
            }

            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            definition.UpdatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                definition.Id = Guid.NewGuid().ToString("N");
            }

            AlarmDefinitionEntity entity = MapToEntity(definition);
            bool exists = await _database.Queryable<AlarmDefinitionEntity>().AnyAsync(item => item.Code == definition.Code).ConfigureAwait(false);
            if (exists)
            {
                await _database.Updateable(entity).Where(item => item.Code == entity.Code).ExecuteCommandAsync().ConfigureAwait(false);
            }
            else
            {
                await _database.Insertable(entity).ExecuteCommandAsync().ConfigureAwait(false);
            }

            lock (_cache)
            {
                _cache[definition.Code] = definition.CreateCopy();
            }
        }

        public async Task SetEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            AlarmDefinitionInfo? definition = await FindByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (definition == null)
            {
                return;
            }

            definition.Enabled = enabled;
            await SaveAsync(definition, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public AlarmRaiseRequest BuildRaiseRequest(AlarmReportRequest request)
        {
            EnsureInitialized();
            AlarmDefinitionInfo? definition = null;
            if (!string.IsNullOrWhiteSpace(request?.Code))
            {
                lock (_cache)
                {
                    _cache.TryGetValue(request.Code.Trim(), out definition);
                }
            }

            if (!AlarmDefinitionResolver.TryBuildRaiseRequest(request ?? new AlarmReportRequest(), definition, out AlarmRaiseRequest raiseRequest))
            {
                throw new InvalidOperationException($"Alarm definition is disabled: {request?.Code}");
            }

            return raiseRequest;
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
            foreach (AlarmDefinitionInfo definition in DefaultAlarmDefinitions.CreateDefaults())
            {
                bool exists = await _database.Queryable<AlarmDefinitionEntity>().AnyAsync(item => item.Code == definition.Code).ConfigureAwait(false);
                if (!exists)
                {
                    await _database.Insertable(MapToEntity(definition)).ExecuteCommandAsync().ConfigureAwait(false);
                }
            }

            foreach (AlarmDefinitionInfo definition in CreateSoftwareDefaults())
            {
                bool exists = await _database.Queryable<AlarmDefinitionEntity>().AnyAsync(item => item.Code == definition.Code).ConfigureAwait(false);
                if (!exists)
                {
                    await _database.Insertable(MapToEntity(definition)).ExecuteCommandAsync().ConfigureAwait(false);
                }
            }
        }

        private static IReadOnlyList<AlarmDefinitionInfo> CreateSoftwareDefaults()
        {
            return new[]
            {
                CreateSoftwareDefinition(
                    "SW.MODULE.EXECUTE_FAILED",
                    "软件模块执行失败",
                    "软件模块",
                    "Module",
                    AlarmSeverity.Error,
                    "检查模块参数、输入数据和执行异常信息。"),
                CreateSoftwareDefinition(
                    "SW.RECIPE.INVALID_PARAM",
                    "配方参数无效",
                    "配方",
                    "Recipe",
                    AlarmSeverity.Warning,
                    "检查配方参数范围、必填项和当前产品配置。"),
                CreateSoftwareDefinition(
                    "SW.ALGORITHM.FAILED",
                    "算法执行失败",
                    "算法",
                    "Algorithm",
                    AlarmSeverity.Error,
                    "检查算法输入、模型文件和异常堆栈信息。"),
                CreateSoftwareDefinition(
                    "SW.DATA.NO_RESULT",
                    "软件数据无结果",
                    "数据",
                    "Data",
                    AlarmSeverity.Warning,
                    "检查上游模块输出、数据筛选条件和采集结果。"),
                CreateSoftwareDefinition(
                    "SW.SYSTEM.UNHANDLED_EXCEPTION",
                    "系统未处理异常",
                    "系统",
                    "System",
                    AlarmSeverity.Fatal,
                    "保存现场日志，停止自动流程并联系维护人员。")
            };
        }

        private static AlarmDefinitionInfo CreateSoftwareDefinition(
            string code,
            string name,
            string category,
            string defaultSource,
            AlarmSeverity severity,
            string suggestedAction)
        {
            DateTime now = DateTime.Now;
            return new AlarmDefinitionInfo
            {
                Id = code.Replace(".", "_"),
                Code = code,
                Name = name,
                Category = category,
                SourceType = SoftwareSourceType,
                DefaultSource = defaultSource,
                DefaultLocation = "Unknown",
                Severity = severity,
                NeedAcknowledge = severity >= AlarmSeverity.Fatal,
                PopupMode = GetDefaultPopupMode(severity, severity >= AlarmSeverity.Fatal),
                PopupThrottleSeconds = 3,
                AllowManualClear = true,
                AutoClearOnRecovery = false,
                DebounceMilliseconds = severity >= AlarmSeverity.Fatal ? 0 : 500,
                ThrottleSeconds = 1,
                Enabled = true,
                IsSystem = true,
                SuggestedAction = suggestedAction,
                ExtraTemplate = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SourceType"] = SoftwareSourceType
                },
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private static AlarmDefinitionInfo MapToInfo(AlarmDefinitionEntity entity)
        {
            return new AlarmDefinitionInfo
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Category = entity.Category,
                SourceType = entity.SourceType,
                DefaultSource = entity.DefaultSource,
                DefaultLocation = entity.DefaultLocation,
                Severity = (AlarmSeverity)entity.SeverityValue,
                NeedAcknowledge = entity.NeedAcknowledge,
                PopupMode = NormalizePopupMode(entity.PopupModeValue, (AlarmSeverity)entity.SeverityValue, entity.NeedAcknowledge),
                PopupThrottleSeconds = Math.Max(0, entity.PopupThrottleSeconds),
                AllowManualClear = entity.AllowManualClear,
                AcknowledgeResetMode = (AlarmAcknowledgeResetMode)entity.AcknowledgeResetModeValue,
                AutoClearOnRecovery = entity.AutoClearOnRecovery,
                DebounceMilliseconds = entity.DebounceMilliseconds,
                ThrottleSeconds = entity.ThrottleSeconds,
                Enabled = entity.Enabled,
                IsSystem = entity.IsSystem,
                SuggestedAction = entity.SuggestedAction,
                ExtraTemplate = string.IsNullOrWhiteSpace(entity.ExtraTemplateJson)
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    : JsonConvert.DeserializeObject<Dictionary<string, object?>>(entity.ExtraTemplateJson) ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static AlarmDefinitionEntity MapToEntity(AlarmDefinitionInfo info)
        {
            return new AlarmDefinitionEntity
            {
                Id = info.Id,
                Code = info.Code.Trim(),
                Name = info.Name?.Trim() ?? info.Code.Trim(),
                Category = info.Category?.Trim() ?? string.Empty,
                SourceType = info.SourceType?.Trim() ?? string.Empty,
                DefaultSource = info.DefaultSource?.Trim() ?? string.Empty,
                DefaultLocation = info.DefaultLocation?.Trim() ?? string.Empty,
                SeverityValue = (int)info.Severity,
                NeedAcknowledge = info.NeedAcknowledge,
                PopupModeValue = (int)info.PopupMode,
                PopupThrottleSeconds = Math.Max(0, info.PopupThrottleSeconds),
                AllowManualClear = info.AllowManualClear,
                AcknowledgeResetModeValue = (int)info.AcknowledgeResetMode,
                AutoClearOnRecovery = info.AutoClearOnRecovery,
                DebounceMilliseconds = Math.Max(0, info.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, info.ThrottleSeconds),
                Enabled = info.Enabled,
                IsSystem = info.IsSystem,
                SuggestedAction = info.SuggestedAction?.Trim() ?? string.Empty,
                ExtraTemplateJson = JsonConvert.SerializeObject(info.ExtraTemplate ?? new Dictionary<string, object?>()),
                CreatedAt = info.CreatedAt == default ? DateTime.Now : info.CreatedAt,
                UpdatedAt = info.UpdatedAt == default ? DateTime.Now : info.UpdatedAt
            };
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

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}


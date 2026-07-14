#nullable enable
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.HardwareRules;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Config
{
    [ExposedService(Lifetime.Singleton, 6, typeof(IAlarmConfigService))]
    public sealed class AlarmConfigService : IAlarmConfigService
    {
        private const string SoftwareSourceType = "Software";

        private readonly IAlarmDefinitionService _definitionService;
        private readonly IHardwareAlarmRuleService _triggerRuleService;

        public AlarmConfigService(IAlarmDefinitionService definitionService, IHardwareAlarmRuleService triggerRuleService)
        {
            _definitionService = definitionService ?? throw new ArgumentNullException(nameof(definitionService));
            _triggerRuleService = triggerRuleService ?? throw new ArgumentNullException(nameof(triggerRuleService));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _definitionService.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await _triggerRuleService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<AlarmConfigSnapshot> LoadAsync(string keyword = "", CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            string normalizedKeyword = keyword?.Trim() ?? string.Empty;

            IReadOnlyList<AlarmDefinitionInfo> definitionInfos = await _definitionService.GetDefinitionsAsync(new AlarmDefinitionQuery
            {
                Keyword = normalizedKeyword,
                IncludeSystem = true,
                MaxCount = 500
            }, cancellationToken).ConfigureAwait(false);

            IReadOnlyList<HardwareAlarmRuleInfo> ruleInfos = await _triggerRuleService.GetRulesAsync(new HardwareAlarmRuleQuery
            {
                Keyword = normalizedKeyword,
                IncludeSystem = true,
                MaxCount = 500
            }, cancellationToken).ConfigureAwait(false);

            return new AlarmConfigSnapshot
            {
                Definitions = definitionInfos.Select(ToModel).ToArray(),
                TriggerRules = ruleInfos.Select(ToModel).ToArray()
            };
        }

        public Task SaveDefinitionAsync(AlarmDefinition definition, string operatorName, CancellationToken cancellationToken = default)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return _definitionService.SaveAsync(ToInfo(definition), operatorName, cancellationToken);
        }

        public Task SetDefinitionEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            return _definitionService.SetEnabledAsync(code, enabled, operatorName, cancellationToken);
        }

        public Task SaveTriggerRuleAsync(AlarmTriggerRule rule, string operatorName, CancellationToken cancellationToken = default)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            return _triggerRuleService.SaveAsync(ToInfo(rule), operatorName, cancellationToken);
        }

        public Task SetTriggerRuleEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            return _triggerRuleService.SetEnabledAsync(id, enabled, operatorName, cancellationToken);
        }

        internal static AlarmDefinition ToModel(AlarmDefinitionInfo info)
        {
            return new AlarmDefinition
            {
                Id = info.Id,
                Code = info.Code,
                Name = info.Name,
                Category = info.Category,
                SourceKind = ParseSourceKind(info.SourceType),
                DefaultSource = info.DefaultSource,
                DefaultLocation = info.DefaultLocation,
                Severity = info.Severity,
                NeedAcknowledge = info.NeedAcknowledge,
                AllowManualClear = info.AllowManualClear,
                AutoClearOnRecovery = info.AutoClearOnRecovery,
                DebounceMilliseconds = info.DebounceMilliseconds,
                ThrottleSeconds = info.ThrottleSeconds,
                AcknowledgeResetMode = info.AcknowledgeResetMode,
                PopupMode = info.PopupMode,
                PopupThrottleSeconds = info.PopupThrottleSeconds,
                SuggestedAction = info.SuggestedAction,
                Enabled = info.Enabled,
                IsSystem = info.IsSystem,
                ExtraTemplate = new Dictionary<string, object?>(info.ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = info.CreatedAt,
                UpdatedAt = info.UpdatedAt
            };
        }

        internal static AlarmDefinitionInfo ToInfo(AlarmDefinition model)
        {
            string sourceType = ToSourceType(model.SourceKind);
            DateTime now = DateTime.Now;
            return new AlarmDefinitionInfo
            {
                Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id.Trim(),
                Code = model.Code?.Trim() ?? string.Empty,
                Name = string.IsNullOrWhiteSpace(model.Name) ? model.Code?.Trim() ?? string.Empty : model.Name.Trim(),
                Category = model.Category?.Trim() ?? string.Empty,
                SourceType = sourceType,
                DefaultSource = string.IsNullOrWhiteSpace(model.DefaultSource) ? sourceType : model.DefaultSource.Trim(),
                DefaultLocation = string.IsNullOrWhiteSpace(model.DefaultLocation) ? "Unknown" : model.DefaultLocation.Trim(),
                Severity = model.Severity,
                NeedAcknowledge = model.NeedAcknowledge,
                PopupMode = model.PopupMode,
                PopupThrottleSeconds = Math.Max(0, model.PopupThrottleSeconds),
                AllowManualClear = model.AllowManualClear,
                AutoClearOnRecovery = model.AutoClearOnRecovery,
                DebounceMilliseconds = Math.Max(0, model.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, model.ThrottleSeconds),
                AcknowledgeResetMode = model.AcknowledgeResetMode,
                Enabled = model.Enabled,
                IsSystem = model.IsSystem,
                SuggestedAction = model.SuggestedAction?.Trim() ?? string.Empty,
                ExtraTemplate = new Dictionary<string, object?>(model.ExtraTemplate ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase),
                CreatedAt = model.CreatedAt == default ? now : model.CreatedAt,
                UpdatedAt = now
            };
        }

        internal static AlarmTriggerRule ToModel(HardwareAlarmRuleInfo info)
        {
            return new AlarmTriggerRule
            {
                Id = info.Id,
                DefinitionCode = info.DefinitionCode,
                Name = info.Name,
                SourceKind = ParseSourceKind(info.SourceType),
                SourcePattern = info.SourcePattern,
                LocationPattern = info.LocationPattern,
                TriggerKind = ToTriggerKind(info.TriggerKind),
                TriggerField = info.TriggerField,
                Operator = (AlarmRuleOperator)(int)info.Operator,
                TriggerValue = info.TriggerValue,
                ClearMode = (AlarmClearMode)(int)info.ClearKind,
                ClearValue = info.ClearValue,
                DebounceMilliseconds = info.DebounceMilliseconds,
                ThrottleSeconds = info.ThrottleSeconds,
                LatchMode = info.LatchMode,
                Enabled = info.Enabled,
                IsSystem = info.IsSystem,
                Priority = info.Priority
            };
        }

        internal static HardwareAlarmRuleInfo ToInfo(AlarmTriggerRule model)
        {
            DateTime now = DateTime.Now;
            return new HardwareAlarmRuleInfo
            {
                Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id,
                DefinitionCode = model.DefinitionCode?.Trim() ?? string.Empty,
                Name = string.IsNullOrWhiteSpace(model.Name) ? model.DefinitionCode?.Trim() ?? string.Empty : model.Name.Trim(),
                SourceType = ToSourceType(model.SourceKind),
                SourcePattern = model.SourcePattern?.Trim() ?? string.Empty,
                LocationPattern = model.LocationPattern?.Trim() ?? string.Empty,
                TriggerKind = ToHardwareTriggerKind(model.TriggerKind),
                TriggerField = model.TriggerField?.Trim() ?? string.Empty,
                Operator = (HardwareAlarmOperator)(int)model.Operator,
                TriggerValue = model.TriggerValue?.Trim() ?? string.Empty,
                ClearKind = (HardwareAlarmClearKind)(int)model.ClearMode,
                ClearValue = model.ClearValue?.Trim() ?? string.Empty,
                DebounceMilliseconds = Math.Max(0, model.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, model.ThrottleSeconds),
                LatchMode = model.LatchMode,
                Enabled = model.Enabled,
                IsSystem = model.IsSystem,
                Priority = model.Priority,
                ExtraTemplate = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        internal static AlarmSourceKind ParseSourceKind(string? sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType))
            {
                return AlarmSourceKind.Unknown;
            }

            string value = sourceType.Trim();
            if (value.Equals(SoftwareSourceType, StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Software;
            }

            if (value.Equals(HardwareAlarmSources.Plc, StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Plc;
            }

            if (value.Equals(HardwareAlarmSources.MotionCard, StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.MotionCard;
            }

            if (value.Equals(HardwareAlarmSources.Sensor, StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Sensor;
            }

            if (value.Equals(HardwareAlarmSources.Camera, StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Camera;
            }

            return value.Equals("System", StringComparison.OrdinalIgnoreCase)
                ? AlarmSourceKind.System
                : AlarmSourceKind.Hardware;
        }

        internal static string ToSourceType(AlarmSourceKind sourceKind)
        {
            return sourceKind switch
            {
                AlarmSourceKind.Software => SoftwareSourceType,
                AlarmSourceKind.Plc => HardwareAlarmSources.Plc,
                AlarmSourceKind.MotionCard => HardwareAlarmSources.MotionCard,
                AlarmSourceKind.Sensor => HardwareAlarmSources.Sensor,
                AlarmSourceKind.Camera => HardwareAlarmSources.Camera,
                AlarmSourceKind.System => "System",
                AlarmSourceKind.Hardware => HardwareAlarmSources.Hardware,
                _ => HardwareAlarmSources.Hardware
            };
        }

        private static AlarmTriggerKind ToTriggerKind(HardwareAlarmTriggerKind kind)
        {
            return kind == HardwareAlarmTriggerKind.ExtraData
                ? AlarmTriggerKind.Data
                : (AlarmTriggerKind)(int)kind;
        }

        private static HardwareAlarmTriggerKind ToHardwareTriggerKind(AlarmTriggerKind kind)
        {
            return kind == AlarmTriggerKind.Data
                ? HardwareAlarmTriggerKind.ExtraData
                : (HardwareAlarmTriggerKind)(int)kind;
        }
    }
}

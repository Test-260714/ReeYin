#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string DefinitionCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string SourcePattern { get; set; } = string.Empty;
        public string LocationPattern { get; set; } = string.Empty;
        public HardwareAlarmTriggerKind TriggerKind { get; set; } = HardwareAlarmTriggerKind.State;
        public string TriggerField { get; set; } = string.Empty;
        public HardwareAlarmOperator Operator { get; set; } = HardwareAlarmOperator.Equals;
        public string TriggerValue { get; set; } = string.Empty;
        public HardwareAlarmClearKind ClearKind { get; set; } = HardwareAlarmClearKind.StateRecovery;
        public string ClearValue { get; set; } = string.Empty;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool LatchMode { get; set; }
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public int Priority { get; set; } = 100;
        public IDictionary<string, object?> ExtraTemplate { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public HardwareAlarmRuleInfo CreateCopy()
        {
            return new HardwareAlarmRuleInfo
            {
                Id = Id,
                DefinitionCode = DefinitionCode,
                Name = Name,
                SourceType = SourceType,
                SourcePattern = SourcePattern,
                LocationPattern = LocationPattern,
                TriggerKind = TriggerKind,
                TriggerField = TriggerField,
                Operator = Operator,
                TriggerValue = TriggerValue,
                ClearKind = ClearKind,
                ClearValue = ClearValue,
                DebounceMilliseconds = DebounceMilliseconds,
                ThrottleSeconds = ThrottleSeconds,
                LatchMode = LatchMode,
                Enabled = Enabled,
                IsSystem = IsSystem,
                Priority = Priority,
                ExtraTemplate = ExtraTemplate == null
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, object?>(ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}

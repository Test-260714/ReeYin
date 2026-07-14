#nullable enable
using System;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public sealed class AlarmTriggerRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string DefinitionCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public AlarmSourceKind SourceKind { get; set; } = AlarmSourceKind.Hardware;

        public string SourcePattern { get; set; } = string.Empty;

        public string LocationPattern { get; set; } = string.Empty;

        public AlarmTriggerKind TriggerKind { get; set; } = AlarmTriggerKind.State;

        public string TriggerField { get; set; } = string.Empty;

        public AlarmRuleOperator Operator { get; set; } = AlarmRuleOperator.Equals;

        public string TriggerValue { get; set; } = string.Empty;

        public AlarmClearMode ClearMode { get; set; } = AlarmClearMode.StateRecovery;

        public string ClearValue { get; set; } = string.Empty;

        public int DebounceMilliseconds { get; set; }

        public int ThrottleSeconds { get; set; } = 1;

        public bool LatchMode { get; set; }

        public bool Enabled { get; set; } = true;

        public bool IsSystem { get; set; }

        public int Priority { get; set; } = 100;
    }
}

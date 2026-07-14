#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleAction
    {
        public string DefinitionCode { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ShouldRaise { get; set; }
        public bool ShouldClear { get; set; }
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool IsLatched { get; set; }
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime OccurredAt { get; set; } = DateTime.Now;
    }
}

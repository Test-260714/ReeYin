#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public sealed class AlarmDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public AlarmSourceKind SourceKind { get; set; } = AlarmSourceKind.Unknown;
        public string DefaultSource { get; set; } = string.Empty;
        public string DefaultLocation { get; set; } = string.Empty;

        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;

        public bool NeedAcknowledge { get; set; } = true;

        public bool AllowManualClear { get; set; } = true;
        public bool AutoClearOnRecovery { get; set; } = true;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public AlarmAcknowledgeResetMode AcknowledgeResetMode { get; set; } = AlarmAcknowledgeResetMode.OnSeverityIncrease;

        public AlarmPopupMode PopupMode { get; set; } = AlarmPopupMode.Growl;

        public int PopupThrottleSeconds { get; set; } = 3;

        public string SuggestedAction { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public bool IsSystem { get; set; }
        public IDictionary<string, object?> ExtraTemplate { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

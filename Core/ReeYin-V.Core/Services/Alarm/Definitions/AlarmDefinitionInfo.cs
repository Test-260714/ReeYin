#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public sealed class AlarmDefinitionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string DefaultSource { get; set; } = string.Empty;
        public string DefaultLocation { get; set; } = string.Empty;
        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
        public bool NeedAcknowledge { get; set; } = true;
        public AlarmPopupMode PopupMode { get; set; } = AlarmPopupMode.Growl;
        public int PopupThrottleSeconds { get; set; } = 3;
        public bool AllowManualClear { get; set; } = true;
        public AlarmAcknowledgeResetMode AcknowledgeResetMode { get; set; } = AlarmAcknowledgeResetMode.OnSeverityIncrease;
        public bool AutoClearOnRecovery { get; set; } = true;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
        public IDictionary<string, object?> ExtraTemplate { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public AlarmDefinitionInfo CreateCopy()
        {
            return new AlarmDefinitionInfo
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Category = Category,
                SourceType = SourceType,
                DefaultSource = DefaultSource,
                DefaultLocation = DefaultLocation,
                Severity = Severity,
                NeedAcknowledge = NeedAcknowledge,
                PopupMode = PopupMode,
                PopupThrottleSeconds = PopupThrottleSeconds,
                AllowManualClear = AllowManualClear,
                AcknowledgeResetMode = AcknowledgeResetMode,
                AutoClearOnRecovery = AutoClearOnRecovery,
                DebounceMilliseconds = DebounceMilliseconds,
                ThrottleSeconds = ThrottleSeconds,
                Enabled = Enabled,
                IsSystem = IsSystem,
                SuggestedAction = SuggestedAction,
                ExtraTemplate = new Dictionary<string, object?>(ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}


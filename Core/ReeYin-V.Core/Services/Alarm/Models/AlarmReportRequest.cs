#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// Common alarm report input used by both software and hardware reporters.
    /// </summary>
    public class AlarmReportRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string SourceType { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public AlarmSeverity? Severity { get; set; }

        public string Operation { get; set; } = string.Empty;

        public string ErrorCode { get; set; } = string.Empty;

        public bool? NeedAcknowledge { get; set; }

        public bool? AllowManualClear { get; set; }

        public string? SuggestedAction { get; set; }

        public AlarmPopupMode? PopupMode { get; set; }

        public int? PopupThrottleSeconds { get; set; }

        public Exception? Exception { get; set; }

        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}

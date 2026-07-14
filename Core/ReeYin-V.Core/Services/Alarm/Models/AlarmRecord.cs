#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public sealed class AlarmRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;

        public string Source { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsAcknowledged { get; set; }

        public bool NeedAcknowledge { get; set; } = true;

        public bool AllowManualClear { get; set; } = true;

        public DateTime FirstTriggeredAt { get; set; } = DateTime.Now;

        public DateTime LastTriggeredAt { get; set; } = DateTime.Now;

        public DateTime? AcknowledgedAt { get; set; }

        public DateTime? ClearedAt { get; set; }

        public int OccurrenceCount { get; set; } = 1;

        public IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}

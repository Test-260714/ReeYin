#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public sealed class AlarmSignal
    {
        public string Code { get; set; } = string.Empty;

        public AlarmSourceKind SourceKind { get; set; } = AlarmSourceKind.Unknown;

        public string Source { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public AlarmSeverity? SeverityOverride { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.Now;

        public IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}

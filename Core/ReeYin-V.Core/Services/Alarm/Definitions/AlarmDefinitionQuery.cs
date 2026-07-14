#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public sealed class AlarmDefinitionQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public AlarmSeverity? Severity { get; set; }
        public bool? Enabled { get; set; }
        public bool IncludeSystem { get; set; } = true;
        public int MaxCount { get; set; } = 500;
    }
}


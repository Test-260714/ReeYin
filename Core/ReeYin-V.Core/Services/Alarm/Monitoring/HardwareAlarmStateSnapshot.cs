#nullable enable
using System;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    public sealed class HardwareAlarmStateSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public HardwareState LastState { get; set; }
        public DateTime LastChangedAt { get; set; } = DateTime.Now;
        public DateTime PendingSince { get; set; }
        public DateTime LastReportedAt { get; set; }
        public bool IsInAlarm { get; set; }
        public bool IsLatched { get; set; }
        public string ActiveCode { get; set; } = string.Empty;
        public string PendingCode { get; set; } = string.Empty;
    }
}


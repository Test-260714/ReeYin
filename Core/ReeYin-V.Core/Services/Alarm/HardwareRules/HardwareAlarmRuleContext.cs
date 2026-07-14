#nullable enable
using ReeYin_V.Core;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleContext
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public HardwareState Status { get; set; }
        public bool IsConnect { get; set; }
        public string Describe { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

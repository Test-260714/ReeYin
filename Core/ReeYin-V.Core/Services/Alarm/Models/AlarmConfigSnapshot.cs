#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public sealed class AlarmConfigSnapshot
    {
        public IReadOnlyList<AlarmDefinition> Definitions { get; set; } = Array.Empty<AlarmDefinition>();

        public IReadOnlyList<AlarmTriggerRule> TriggerRules { get; set; } = Array.Empty<AlarmTriggerRule>();
    }
}

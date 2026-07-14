#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    [SugarTable("hardware_alarm_rule", TableDescription = "硬件报警触发和恢复规则")]
    public sealed class HardwareAlarmRuleEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = false)]
        public string DefinitionCode { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = false)]
        public string Name { get; set; } = string.Empty;
        [SugarColumn(Length = 64, IsNullable = false)]
        public string SourceType { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = true)]
        public string SourcePattern { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = true)]
        public string LocationPattern { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string TriggerKind { get; set; } = HardwareAlarmTriggerKind.State.ToString();
        [SugarColumn(Length = 128, IsNullable = false)]
        public string TriggerField { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string Operator { get; set; } = HardwareAlarmOperator.Equals.ToString();
        [SugarColumn(Length = 256, IsNullable = true)]
        public string TriggerValue { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string ClearKind { get; set; } = HardwareAlarmClearKind.StateRecovery.ToString();
        [SugarColumn(Length = 256, IsNullable = true)]
        public string ClearValue { get; set; } = string.Empty;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool LatchMode { get; set; }
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public int Priority { get; set; } = 100;
        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string ExtraTemplateJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

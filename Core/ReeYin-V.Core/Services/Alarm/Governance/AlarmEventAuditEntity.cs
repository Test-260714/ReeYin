#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    [SugarTable("alarm_event_audit", TableDescription = "Alarm event audit trail")]
    public sealed class AlarmEventAuditEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string ActiveId { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = false)]
        public string Action { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = false)]
        public string Source { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string Location { get; set; } = string.Empty;

        public int SeverityValue { get; set; }

        [SugarColumn(Length = 512, IsNullable = true)]
        public string Message { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string OperatorName { get; set; } = string.Empty;

        [SugarColumn(Length = 512, IsNullable = true)]
        public string Note { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string ExtraDataJson { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime OccurredAt { get; set; } = DateTime.Now;
    }
}

#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    [SugarTable("alarm_suppression_rule", TableDescription = "Alarm suppression rules")]
    public sealed class AlarmSuppressionRuleEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string CodePattern { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string SourcePattern { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string LocationPattern { get; set; } = string.Empty;

        [SugarColumn(Length = 512, IsNullable = true)]
        public string Reason { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        [SugarColumn(ColumnDataType = "datetime", IsNullable = true)]
        public DateTime? EffectiveFrom { get; set; }

        [SugarColumn(ColumnDataType = "datetime", IsNullable = true)]
        public DateTime? EffectiveTo { get; set; }

        [SugarColumn(Length = 64, IsNullable = true)]
        public string CreatedBy { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

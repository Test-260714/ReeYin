#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    [SugarTable("alarm_notification_route", TableDescription = "Alarm notification routes")]
    public sealed class AlarmNotificationRouteEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string CodePattern { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string SourcePattern { get; set; } = string.Empty;

        public int MinSeverityValue { get; set; }

        [SugarColumn(Length = 256, IsNullable = true)]
        public string Channels { get; set; } = string.Empty;

        [SugarColumn(Length = 512, IsNullable = true)]
        public string Receivers { get; set; } = string.Empty;

        [SugarColumn(Length = 16, IsNullable = true)]
        public string QuietStart { get; set; } = string.Empty;

        [SugarColumn(Length = 16, IsNullable = true)]
        public string QuietEnd { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

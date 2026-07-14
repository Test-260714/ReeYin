#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    [SugarTable("alarm_shelve", TableDescription = "Shelved alarm rules")]
    public sealed class AlarmShelveEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string ActiveId { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string CodePattern { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string SourcePattern { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string LocationPattern { get; set; } = string.Empty;

        [SugarColumn(Length = 512, IsNullable = true)]
        public string Reason { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string ShelvedBy { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime ShelvedAt { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDataType = "datetime")]
        public DateTime ShelvedUntil { get; set; } = DateTime.Now.AddHours(1);

        public bool IsActive { get; set; } = true;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string ReleasedBy { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "datetime", IsNullable = true)]
        public DateTime? ReleasedAt { get; set; }

        [SugarColumn(Length = 512, IsNullable = true)]
        public string ReleaseNote { get; set; } = string.Empty;
    }
}

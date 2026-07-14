using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm
{
    /// <summary>
    /// SqlSugar 报警记录实体，同时承载活动报警和已闭环报警。
    /// </summary>
    [SugarTable("alarm_record", TableDescription = "报警历史和活动记录")]
    public sealed class AlarmRecordEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64, ColumnDescription = "报警生命周期 Id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 活动报警去重键，由来源、编码和位置生成。
        /// </summary>
        [SugarColumn(Length = 256, IsNullable = false, ColumnDescription = "去重键")]
        public string ActiveKey { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = false, ColumnDescription = "报警编码")]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true, ColumnDescription = "报警名称")]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true, ColumnDescription = "报警分类")]
        public string Category { get; set; } = string.Empty;

        [SugarColumn(Length = 512, IsNullable = false, ColumnDescription = "报警消息")]
        public string Message { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "报警严重等级")]
        public int LevelValue { get; set; }

        [SugarColumn(Length = 128, IsNullable = false, ColumnDescription = "报警来源")]
        public string Source { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true, ColumnDescription = "报警位置")]
        public string Location { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "datetime", IsNullable = false, ColumnDescription = "首次触发时间")]
        public DateTime Timestamp { get; set; }

        [SugarColumn(ColumnDataType = "datetime", IsNullable = false, ColumnDescription = "最近触发时间")]
        public DateTime LastRaisedAt { get; set; }

        [SugarColumn(ColumnDataType = "datetime", IsNullable = true, ColumnDescription = "清除时间")]
        public DateTime? ClearTime { get; set; }

        [SugarColumn(ColumnDataType = "datetime", IsNullable = true, ColumnDescription = "确认时间")]
        public DateTime? ConfirmTime { get; set; }

        [SugarColumn(ColumnDescription = "是否活动报警", DefaultValue = "1")]
        public bool IsActive { get; set; } = true;

        [SugarColumn(ColumnDescription = "是否已确认", DefaultValue = "0")]
        public bool IsConfirmed { get; set; }

        [SugarColumn(Length = 64, IsNullable = true, ColumnDescription = "确认用户")]
        public string ConfirmUser { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true, ColumnDescription = "清除用户")]
        public string ClearUser { get; set; } = string.Empty;

        [SugarColumn(Length = 256, IsNullable = true, ColumnDescription = "操作备注")]
        public string Note { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "是否需要确认", DefaultValue = "1")]
        public bool NeedAcknowledge { get; set; } = true;

        public int PopupModeValue { get; set; } = 1;

        public int PopupThrottleSeconds { get; set; } = 3;

        [SugarColumn(ColumnDescription = "是否允许手动清除", DefaultValue = "1")]
        public bool AllowManualClear { get; set; } = true;
        public int AcknowledgeResetModeValue { get; set; } = (int)Models.AlarmAcknowledgeResetMode.OnSeverityIncrease;

        [SugarColumn(ColumnDescription = "触发次数", DefaultValue = "1")]
        public int OccurrenceCount { get; set; } = 1;

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "扩展数据 JSON")]
        public string ExtraDataJson { get; set; } = string.Empty;
    }
}

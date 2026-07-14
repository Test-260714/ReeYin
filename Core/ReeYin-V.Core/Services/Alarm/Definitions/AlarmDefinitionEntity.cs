#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    [SugarTable("alarm_definition", TableDescription = "报警定义和硬件报警规则")]
    public sealed class AlarmDefinitionEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string Category { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string SourceType { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string DefaultSource { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string DefaultLocation { get; set; } = string.Empty;

        public int SeverityValue { get; set; }

        public bool NeedAcknowledge { get; set; } = true;

        public int PopupModeValue { get; set; } = (int)AlarmPopupMode.Growl;

        public int PopupThrottleSeconds { get; set; } = 3;

        public bool AllowManualClear { get; set; } = true;
        public int AcknowledgeResetModeValue { get; set; } = (int)AlarmAcknowledgeResetMode.OnSeverityIncrease;

        public bool AutoClearOnRecovery { get; set; } = true;

        public int DebounceMilliseconds { get; set; }

        public int ThrottleSeconds { get; set; } = 1;

        public bool Enabled { get; set; } = true;

        public bool IsSystem { get; set; }

        [SugarColumn(Length = 512, IsNullable = true)]
        public string SuggestedAction { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string ExtraTemplateJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

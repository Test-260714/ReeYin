using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 报警新增或重复触发时使用的入参模型。
    /// Code、Source 和 Location 共同组成活动报警的去重键。
    /// </summary>
    public sealed class AlarmRaiseRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public AlarmSeverity Level { get; set; } = AlarmSeverity.Warning;

        public string Source { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 报警是否需要确认后才视为已处理。
        /// </summary>
        public bool NeedAcknowledge { get; set; } = true;

        public AlarmPopupMode PopupMode { get; set; } = AlarmPopupMode.Growl;

        public int PopupThrottleSeconds { get; set; } = 3;

        /// <summary>
        /// 是否允许操作员手动清除活动报警。
        /// </summary>
        public bool AllowManualClear { get; set; } = true;
        public AlarmAcknowledgeResetMode AcknowledgeResetMode { get; set; } = AlarmAcknowledgeResetMode.OnSeverityIncrease;

        /// <summary>
        /// 可选结构化扩展数据，用于界面详情展示或后续诊断。
        /// </summary>
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}

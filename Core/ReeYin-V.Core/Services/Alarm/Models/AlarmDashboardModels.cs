using System;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 报警看板顶部使用的聚合计数快照。
    /// </summary>
    public sealed class AlarmDashboardSnapshot
    {
        public int ActiveCount { get; set; }

        public int UnacknowledgedCount { get; set; }

        public int FatalCount { get; set; }

        public int CriticalCount => FatalCount;

        public int TodayRaisedCount { get; set; }

        public int HistoryCount { get; set; }

        public DateTime? LatestRaisedAt { get; set; }
    }

    /// <summary>
    /// 实时报警流中显示的轻量事件项。
    /// </summary>
    public sealed class AlarmRealtimeEntry
    {
        public string ActiveId { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public AlarmSeverity Severity { get; set; }

        public AlarmEventKind EventKind { get; set; }

        public DateTime EventTime { get; set; }

        public int OccurrenceCount { get; set; }

        public bool NeedAcknowledge { get; set; }

        public bool IsConfirmed { get; set; }

        public AlarmPopupMode PopupMode { get; set; }

        public int PopupThrottleSeconds { get; set; }
    }

    /// <summary>
    /// 活动报警列表行使用的读取模型。
    /// </summary>
    public sealed class AlarmActiveRecord
    {
        public string ActiveId { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public AlarmSeverity Severity { get; set; }

        public DateTime RaisedAt { get; set; }

        public DateTime LastRaisedAt { get; set; }

        public TimeSpan ActiveDuration { get; set; }

        public int OccurrenceCount { get; set; }

        public bool NeedAcknowledge { get; set; }

        public bool IsAcknowledged { get; set; }

        public AlarmPopupMode PopupMode { get; set; }

        public int PopupThrottleSeconds { get; set; }

        public bool AllowManualClear { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 用于界面详情展示的序列化扩展数据。
        /// </summary>
        public string Detail { get; set; } = string.Empty;
    }

    /// <summary>
    /// 已清除报警历史列表行使用的读取模型。
    /// </summary>
    public sealed class AlarmHistoryEntry
    {
        public string ActiveId { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public AlarmSeverity Severity { get; set; }

        public DateTime RaisedAt { get; set; }

        public DateTime ClearedAt { get; set; }

        public TimeSpan Duration { get; set; }

        public int OccurrenceCount { get; set; }

        public bool WasAcknowledged { get; set; }

        public string Message { get; set; } = string.Empty;

        public string ConfirmUser { get; set; } = string.Empty;
    }
}

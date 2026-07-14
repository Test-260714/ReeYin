using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 单个报警生命周期的可变运行态数据。
    /// 服务会将活动实例保存在内存中，并映射为 AlarmRecordEntity 持久化。
    /// </summary>
    public sealed class AlarmInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public AlarmSeverity Level { get; set; } = AlarmSeverity.Warning;

        public string Source { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 当前生命周期首次触发的时间。
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 同一活动键最近一次重复触发的时间。
        /// </summary>
        public DateTime LastRaisedAt { get; set; } = DateTime.Now;

        public DateTime? ClearTime { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsConfirmed { get; set; }

        public string ConfirmUser { get; set; } = string.Empty;

        public DateTime? ConfirmTime { get; set; }

        public string ClearUser { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public bool NeedAcknowledge { get; set; } = true;

        public AlarmPopupMode PopupMode { get; set; } = AlarmPopupMode.Growl;

        public int PopupThrottleSeconds { get; set; } = 3;

        public bool AllowManualClear { get; set; } = true;
        public AlarmAcknowledgeResetMode AcknowledgeResetMode { get; set; } = AlarmAcknowledgeResetMode.OnSeverityIncrease;

        /// <summary>
        /// 同一活动报警在清除前的累计触发次数。
        /// </summary>
        public int OccurrenceCount { get; set; } = 1;

        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public TimeSpan ActiveDuration => (IsActive ? DateTime.Now : ClearTime ?? DateTime.Now) - Timestamp;

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static AlarmInfo? FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonConvert.DeserializeObject<AlarmInfo>(json);
        }

        /// <summary>
        /// 返回独立快照，避免调用方直接修改服务内部缓存。
        /// </summary>
        public AlarmInfo CreateCopy()
        {
            return new AlarmInfo
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Category = Category,
                Message = Message,
                Level = Level,
                Source = Source,
                Location = Location,
                Timestamp = Timestamp,
                LastRaisedAt = LastRaisedAt,
                ClearTime = ClearTime,
                IsActive = IsActive,
                IsConfirmed = IsConfirmed,
                ConfirmUser = ConfirmUser,
                ConfirmTime = ConfirmTime,
                ClearUser = ClearUser,
                Note = Note,
                NeedAcknowledge = NeedAcknowledge,
                PopupMode = PopupMode,
                PopupThrottleSeconds = PopupThrottleSeconds,
                AllowManualClear = AllowManualClear,
                AcknowledgeResetMode = AcknowledgeResetMode,
                OccurrenceCount = OccurrenceCount,
                ExtraData = new Dictionary<string, object?>(ExtraData ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}

#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;

namespace ReeYin_V.Core.Services.Alarm.Governance
{
    public sealed class AlarmGovernanceQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool? Enabled { get; set; }
        public bool IncludeInactive { get; set; }
        public int MaxCount { get; set; } = 500;
    }

    public sealed class AlarmAuditQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int MaxCount { get; set; } = 300;
    }

    public sealed class AlarmSuppressionRuleInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string CodePattern { get; set; } = string.Empty;
        public string SourcePattern { get; set; } = string.Empty;
        public string LocationPattern { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public AlarmSuppressionRuleInfo CreateCopy()
        {
            return (AlarmSuppressionRuleInfo)MemberwiseClone();
        }
    }

    public sealed class AlarmShelveInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ActiveId { get; set; } = string.Empty;
        public string CodePattern { get; set; } = string.Empty;
        public string SourcePattern { get; set; } = string.Empty;
        public string LocationPattern { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ShelvedBy { get; set; } = string.Empty;
        public DateTime ShelvedAt { get; set; } = DateTime.Now;
        public DateTime ShelvedUntil { get; set; } = DateTime.Now.AddHours(1);
        public bool IsActive { get; set; } = true;
        public string ReleasedBy { get; set; } = string.Empty;
        public DateTime? ReleasedAt { get; set; }
        public string ReleaseNote { get; set; } = string.Empty;

        public AlarmShelveInfo CreateCopy()
        {
            return (AlarmShelveInfo)MemberwiseClone();
        }
    }

    public sealed class AlarmNotificationRouteInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string CodePattern { get; set; } = string.Empty;
        public string SourcePattern { get; set; } = string.Empty;
        public AlarmSeverity MinSeverity { get; set; } = AlarmSeverity.Warning;
        public string Channels { get; set; } = string.Empty;
        public string Receivers { get; set; } = string.Empty;
        public string QuietStart { get; set; } = string.Empty;
        public string QuietEnd { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public AlarmNotificationRouteInfo CreateCopy()
        {
            return (AlarmNotificationRouteInfo)MemberwiseClone();
        }
    }

    public sealed class AlarmEventAuditInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ActiveId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
        public string Message { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string ExtraDataJson { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.Now;

        public AlarmEventAuditInfo CreateCopy()
        {
            return (AlarmEventAuditInfo)MemberwiseClone();
        }
    }
}

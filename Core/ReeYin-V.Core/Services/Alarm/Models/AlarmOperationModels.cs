namespace ReeYin_V.Core.Services.Alarm.Models
{
    public enum AlarmClearOrigin { Manual = 0, Recovery = 1, System = 2 }

    public enum AlarmOperationStatus
    {
        Succeeded = 0,
        NotFound = 1,
        ManualClearNotAllowed = 2,
        AlreadyAcknowledged = 3,
        InvalidRequest = 4
    }

    public sealed class AlarmOperationResult
    {
        public bool Success => Status == AlarmOperationStatus.Succeeded;
        public AlarmOperationStatus Status { get; init; }
        public string Message { get; init; } = string.Empty;
        public string AlarmId { get; init; } = string.Empty;

        public static AlarmOperationResult From(AlarmOperationStatus status, string alarmId = "", string message = "") =>
            new AlarmOperationResult { Status = status, AlarmId = alarmId, Message = message };
    }
}

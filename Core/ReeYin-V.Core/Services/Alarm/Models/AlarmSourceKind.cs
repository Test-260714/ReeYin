#nullable enable

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public enum AlarmSourceKind
    {
        Unknown = 0,
        Software = 1,
        Hardware = 2,
        Plc = 3,
        MotionCard = 4,
        Sensor = 5,
        Camera = 6,
        System = 7
    }

    public enum AlarmTriggerKind
    {
        State = 0,
        ErrorCode = 1,
        Data = 2,
        Heartbeat = 3
    }

    public enum AlarmRuleOperator
    {
        Equals = 0,
        NotEquals = 1,
        GreaterThan = 2,
        GreaterThanOrEqual = 3,
        LessThan = 4,
        LessThanOrEqual = 5,
        Contains = 6,
        BitHasFlag = 7
    }

    public enum AlarmClearMode
    {
        StateRecovery = 0,
        FieldRecovery = 1,
        ManualOnly = 2
    }
}

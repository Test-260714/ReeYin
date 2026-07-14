#nullable enable
namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public enum HardwareAlarmTriggerKind
    {
        State = 0,
        ErrorCode = 1,
        ExtraData = 2,
        Heartbeat = 3
    }

    public enum HardwareAlarmOperator
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

    public enum HardwareAlarmClearKind
    {
        StateRecovery = 0,
        FieldRecovery = 1,
        ManualOnly = 2
    }
}

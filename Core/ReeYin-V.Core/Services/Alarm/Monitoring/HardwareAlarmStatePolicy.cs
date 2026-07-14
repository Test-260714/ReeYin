#nullable enable
using ReeYin_V.Core.Services.Alarm.Hardware;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    public sealed class HardwareAlarmStateAction
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ShouldRaise { get; set; }
        public bool ShouldClear { get; set; }
    }

    public static class HardwareAlarmStatePolicy
    {
        public static HardwareAlarmStateAction Resolve(HardwareState state, bool wasInAlarm)
        {
            switch (state)
            {
                case HardwareState.NotConnected:
                    return Raise(HardwareAlarmCodes.Disconnected, "硬件未连接");

                case HardwareState.Error:
                    return Raise(HardwareAlarmCodes.OperationFailed, "硬件状态异常");

                case HardwareState.Connected:
                case HardwareState.Ready:
                case HardwareState.Idle:
                    return ClearIfNeeded(wasInAlarm, HardwareAlarmCodes.Disconnected, "硬件连接恢复");

                case HardwareState.Complete:
                    return ClearIfNeeded(wasInAlarm, HardwareAlarmCodes.OperationFailed, "硬件操作恢复");

                default:
                    return new HardwareAlarmStateAction();
            }
        }

        private static HardwareAlarmStateAction Raise(string code, string message)
        {
            return new HardwareAlarmStateAction
            {
                Code = code,
                Message = message,
                ShouldRaise = true
            };
        }

        private static HardwareAlarmStateAction ClearIfNeeded(bool wasInAlarm, string code, string message)
        {
            return new HardwareAlarmStateAction
            {
                Code = code,
                Message = message,
                ShouldClear = wasInAlarm
            };
        }
    }
}


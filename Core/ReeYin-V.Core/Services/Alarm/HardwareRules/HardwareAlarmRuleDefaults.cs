#nullable enable
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Alarm.Hardware;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public static class HardwareAlarmRuleDefaults
    {
        public static IReadOnlyList<HardwareAlarmRuleInfo> CreateDefaults()
        {
            return new[]
            {
                Create(
                    HardwareAlarmCodes.Disconnected,
                    "硬件断线",
                    HardwareAlarmSources.Hardware,
                    HardwareAlarmTriggerKind.State,
                    "State",
                    HardwareAlarmOperator.Equals,
                    nameof(HardwareState.NotConnected),
                    HardwareAlarmClearKind.StateRecovery,
                    "Connected,Ready,Idle",
                    500,
                    1,
                    100),
                Create(
                    HardwareAlarmCodes.OperationFailed,
                    "硬件操作失败",
                    HardwareAlarmSources.Hardware,
                    HardwareAlarmTriggerKind.State,
                    "State",
                    HardwareAlarmOperator.Equals,
                    nameof(HardwareState.Error),
                    HardwareAlarmClearKind.StateRecovery,
                    "Ready,Complete,Idle",
                    500,
                    1,
                    100),
                Create(
                    HardwareAlarmCodes.PlcHeartbeatTimeout,
                    "PLC 心跳超时",
                    HardwareAlarmSources.Plc,
                    HardwareAlarmTriggerKind.ExtraData,
                    "HeartbeatAlive",
                    HardwareAlarmOperator.Equals,
                    "false",
                    HardwareAlarmClearKind.FieldRecovery,
                    "true",
                    3000,
                    3,
                    100),
                Create(
                    HardwareAlarmCodes.MotionLimitTriggered,
                    "运动轴限位触发",
                    HardwareAlarmSources.MotionCard,
                    HardwareAlarmTriggerKind.ExtraData,
                    "AxisStatus",
                    HardwareAlarmOperator.BitHasFlag,
                    "PositiveLimit,NegativeLimit",
                    HardwareAlarmClearKind.FieldRecovery,
                    "PositiveLimit,NegativeLimit",
                    0,
                    1,
                    10),
                Create(
                    HardwareAlarmCodes.MotionServoAlarm,
                    "运动轴伺服报警",
                    HardwareAlarmSources.MotionCard,
                    HardwareAlarmTriggerKind.ExtraData,
                    "AxisStatus",
                    HardwareAlarmOperator.BitHasFlag,
                    "ServoAlarm",
                    HardwareAlarmClearKind.FieldRecovery,
                    "ServoAlarm",
                    0,
                    1,
                    10),
                Create(
                    HardwareAlarmCodes.SensorNoData,
                    "传感器无有效数据",
                    HardwareAlarmSources.Sensor,
                    HardwareAlarmTriggerKind.ExtraData,
                    "ValidDataCount",
                    HardwareAlarmOperator.Equals,
                    "0",
                    HardwareAlarmClearKind.FieldRecovery,
                    ">0",
                    1000,
                    3,
                    100)
            };
        }

        private static HardwareAlarmRuleInfo Create(
            string definitionCode,
            string name,
            string sourceType,
            HardwareAlarmTriggerKind triggerKind,
            string triggerField,
            HardwareAlarmOperator triggerOperator,
            string triggerValue,
            HardwareAlarmClearKind clearKind,
            string clearValue,
            int debounceMilliseconds,
            int throttleSeconds,
            int priority)
        {
            DateTime now = DateTime.Now;
            return new HardwareAlarmRuleInfo
            {
                Id = definitionCode.Replace(".", "_"),
                DefinitionCode = definitionCode,
                Name = name,
                SourceType = sourceType,
                SourcePattern = "*",
                LocationPattern = "*",
                TriggerKind = triggerKind,
                TriggerField = triggerField,
                Operator = triggerOperator,
                TriggerValue = triggerValue,
                ClearKind = clearKind,
                ClearValue = clearValue,
                DebounceMilliseconds = debounceMilliseconds,
                ThrottleSeconds = throttleSeconds,
                Enabled = true,
                IsSystem = true,
                Priority = priority,
                ExtraTemplate = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }
}

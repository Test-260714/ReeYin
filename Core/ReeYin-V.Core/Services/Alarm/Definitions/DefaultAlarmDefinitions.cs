#nullable enable
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public static class DefaultAlarmDefinitions
    {
        public static IReadOnlyList<AlarmDefinitionInfo> CreateDefaults()
        {
            return new[]
            {
                Create(HardwareAlarmCodes.ConnectionFailed, "硬件连接失败", HardwareAlarmCategories.Communication, HardwareAlarmSources.Hardware, AlarmSeverity.Error, false, "检查网线、IP、端口和设备电源。"),
                Create(HardwareAlarmCodes.Disconnected, "硬件断线", HardwareAlarmCategories.Communication, HardwareAlarmSources.Hardware, AlarmSeverity.Error, false, "检查设备连接状态并尝试重新连接。"),
                Create(HardwareAlarmCodes.InitializationFailed, "硬件初始化失败", HardwareAlarmCategories.Initialization, HardwareAlarmSources.Hardware, AlarmSeverity.Error, true, "检查配置参数和驱动初始化日志。"),
                Create(HardwareAlarmCodes.OperationFailed, "硬件操作失败", HardwareAlarmCategories.Operation, HardwareAlarmSources.Hardware, AlarmSeverity.Error, true, "检查操作参数、设备状态和异常信息。"),
                Create(HardwareAlarmCodes.SafetyError, "硬件安全异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.Hardware, AlarmSeverity.Fatal, false, "停止自动流程，确认安全条件并执行硬件复位。"),
                Create(HardwareAlarmCodes.PlcReadWriteFailed, "PLC 读写失败", HardwareAlarmCategories.Communication, HardwareAlarmSources.Plc, AlarmSeverity.Error, true, "检查 PLC 地址、通讯连接和数据类型。"),
                Create(HardwareAlarmCodes.PlcCommandTimeout, "PLC 指令超时", HardwareAlarmCategories.Operation, HardwareAlarmSources.Plc, AlarmSeverity.Warning, true, "检查 PLC 目标值、等待时间和外部设备动作。"),
                Create(HardwareAlarmCodes.PlcHeartbeatTimeout, "PLC 心跳超时", HardwareAlarmCategories.Communication, HardwareAlarmSources.Plc, AlarmSeverity.Error, true, "检查 PLC 心跳地址、通讯连接和心跳刷新周期。"),
                Create(HardwareAlarmCodes.MotionControllerError, "运动控制卡异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Error, false, "检查控制卡状态、驱动器和运动轴报警。"),
                Create(HardwareAlarmCodes.MotionLimitTriggered, "运动轴限位触发", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Fatal, false, "停止运动流程，检查限位信号和轴当前位置。"),
                Create(HardwareAlarmCodes.MotionServoAlarm, "运动轴伺服报警", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Fatal, false, "检查伺服驱动器报警，确认安全条件后复位。"),
                Create(HardwareAlarmCodes.MotionSafetyError, "运动安全异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Fatal, false, "停止运动流程，确认限位、急停和伺服状态。"),
                Create(HardwareAlarmCodes.SensorAcquireFailed, "传感器采集失败", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Error, true, "检查传感器连接、曝光、触发和采集参数。"),
                Create(HardwareAlarmCodes.SensorReadResultFailed, "传感器结果读取失败", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Error, true, "检查结果缓存、SDK 返回码和采集流程。"),
                Create(HardwareAlarmCodes.SensorNoData, "传感器无有效数据", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Warning, true, "检查被测物、触发时序和数据输出配置。")
            };
        }

        private static AlarmDefinitionInfo Create(string code, string name, string category, string sourceType, AlarmSeverity severity, bool allowManualClear, string suggestedAction)
        {
            DateTime now = DateTime.Now;
            return new AlarmDefinitionInfo
            {
                Id = code.Replace(".", "_"),
                Code = code,
                Name = name,
                Category = category,
                SourceType = sourceType,
                Severity = severity,
                NeedAcknowledge = severity >= AlarmSeverity.Fatal,
                PopupMode = GetDefaultPopupMode(severity, severity >= AlarmSeverity.Fatal),
                PopupThrottleSeconds = 3,
                AllowManualClear = allowManualClear,
                AutoClearOnRecovery = true,
                DebounceMilliseconds = severity >= AlarmSeverity.Fatal ? 0 : 500,
                ThrottleSeconds = 1,
                Enabled = true,
                IsSystem = true,
                SuggestedAction = suggestedAction,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private static AlarmPopupMode GetDefaultPopupMode(AlarmSeverity severity, bool needAcknowledge)
        {
            if (severity >= AlarmSeverity.Fatal || needAcknowledge)
            {
                return AlarmPopupMode.Modal;
            }

            return severity >= AlarmSeverity.Warning
                ? AlarmPopupMode.Growl
                : AlarmPopupMode.None;
        }
    }
}


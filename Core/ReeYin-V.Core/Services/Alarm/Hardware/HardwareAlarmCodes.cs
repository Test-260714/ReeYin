#nullable enable
namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public static class HardwareAlarmCodes
    {
        public const string ConnectionFailed = "HW.COMM.CONNECTION_FAILED";
        public const string Disconnected = "HW.COMM.DISCONNECTED";
        public const string InitializationFailed = "HW.INITIALIZATION_FAILED";
        public const string OperationFailed = "HW.OPERATION_FAILED";
        public const string SafetyError = "HW.SAFETY_ERROR";
        public const string ConfigurationInvalid = "HW.CONFIG.INVALID";
        public const string PlcHeartbeatTimeout = "HW.PLC.HEARTBEAT_TIMEOUT";
        public const string PlcReadWriteFailed = "HW.PLC.READ_WRITE_FAILED";
        public const string PlcCommandTimeout = "HW.PLC.COMMAND_TIMEOUT";
        public const string MotionControllerError = "HW.MOTION.CONTROLLER_ERROR";
        public const string MotionServoAlarm = "HW.MOTION.SERVO_ALARM";
        public const string MotionLimitTriggered = "HW.MOTION.LIMIT_TRIGGERED";
        public const string MotionSafetyError = "HW.MOTION.SAFETY_ERROR";
        public const string SensorAcquireFailed = "HW.SENSOR.ACQUIRE_FAILED";
        public const string SensorReadResultFailed = "HW.SENSOR.READ_RESULT_FAILED";
        public const string SensorNoData = "HW.SENSOR.NO_DATA";
        public const string SensorZAxisFailed = "HW.SENSOR.Z_AXIS_FAILED";
        public const string CameraCaptureFailed = "HW.CAMERA.CAPTURE_FAILED";
        public const string LightControlFailed = "HW.LIGHT.CONTROL_FAILED";
    }
}


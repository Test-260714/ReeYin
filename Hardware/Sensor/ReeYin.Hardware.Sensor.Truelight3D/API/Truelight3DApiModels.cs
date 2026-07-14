using System;

namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    public enum Truelight3DStatus
    {
        STATUS_OK = 0,
        STATUS_ERROR,
        STATUS_IP_ERROR,
        STATUS_Z_MOTOR_ERROR,
        STATUS_XY_MOTOR_ERROR,
        STATUS_XYZ_MOTOR_ERROR,
        STATUS_CAMERA_ERROR,
        STATUS_SLM_ERROR,
        STATUS_SLM_CONFIG_ERROR,
        STATUS_OCULAR_LENS_ERROR,
        STATUS_CAMERA_PARAMETER_ERROR,
        STATUS_TURRET_ERROR,
        STATUS_PZT_ERROR,
        STATUS_TIME_OUT,
        STATUS_NOT_IMPLEMENTATION,
        STATUS_NOT_ENABLED,
        STATUS_BAD_PARAMETER,
        STATUS_NOT_CONNECTED,
        STATUS_NOT_INITIALIZED,
        STATUS_NOT_SUPPORTED,
        STATUS_OUT_OF_FLOW,
        STATUS_OUT_OF_LAYER_MAX,
        STATUS_OUT_OF_RANGE,
        STATUS_ALREADY_INITIALIZED,
    }

    public enum Truelight3DPixelFormat
    {
        Gray = 0,
        RGB,
        BGR,
    }

    public enum Truelight3DScanType
    {
        Confocal = 0,
        Variation = 1,
    }

    public enum Truelight3DObjectiveMagnification
    {
        Magnification2 = 2,
        Magnification5 = 5,
        Magnification10 = 10,
        Magnification20 = 20,
        Magnification50 = 50,
        Magnification100 = 100,
        Magnification150 = 150,
    }

    public enum Truelight3DMotionDirection
    {
        Positive = 0,
        Negative = 1,
    }

    public sealed class Truelight3DApiResult
    {
        public bool Success { get; init; }

        public Truelight3DStatus Status { get; init; }

        public string Message { get; init; } = string.Empty;

        public static Truelight3DApiResult SuccessResult(Truelight3DStatus status, string message)
        {
            return new Truelight3DApiResult
            {
                Success = true,
                Status = status,
                Message = message,
            };
        }

        public static Truelight3DApiResult FailureResult(Truelight3DStatus status, string message)
        {
            return new Truelight3DApiResult
            {
                Success = false,
                Status = status,
                Message = message,
            };
        }
    }

    public sealed class Truelight3DApiResult<T>
    {
        public bool Success { get; init; }

        public Truelight3DStatus Status { get; init; }

        public string Message { get; init; } = string.Empty;

        public T? Data { get; init; }

        public static Truelight3DApiResult<T> SuccessResult(T data, Truelight3DStatus status, string message)
        {
            return new Truelight3DApiResult<T>
            {
                Success = true,
                Status = status,
                Message = message,
                Data = data,
            };
        }

        public static Truelight3DApiResult<T> FailureResult(Truelight3DStatus status, string message)
        {
            return new Truelight3DApiResult<T>
            {
                Success = false,
                Status = status,
                Message = message,
                Data = default,
            };
        }
    }

    public sealed class Truelight3DScanConfiguration
    {
        public Truelight3DScanType ScanType { get; set; } = Truelight3DScanType.Variation;

        public Truelight3DObjectiveMagnification ObjectiveMagnification { get; set; } = Truelight3DObjectiveMagnification.Magnification20;

        public uint ExposureTimeUs { get; set; } = 701;

        public uint WindowSize { get; set; } = 15;

        public float ZFilter { get; set; } = 0.8f;

        public bool UseScanRange { get; set; }

        public float ScanRangeMm { get; set; } = 20f;

        public float ScanStartMm { get; set; }

        public float ScanEndMm { get; set; }

        public float ScanStepMm { get; set; } = 1f;

        public byte LightRed { get; set; }

        public byte LightGreen { get; set; }

        public byte LightBlue { get; set; }

        public uint? CircleLightValue { get; set; }

        public float? ZSpeedMmPerSec { get; set; } = 2f;
    }

    public sealed class Truelight3DScanResult
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public float[] DepthData { get; set; } = [];

        public byte[] TextureData { get; set; } = [];

        public int TextureChannel { get; set; }

        public Truelight3DPixelFormat TextureFormat { get; set; } = Truelight3DPixelFormat.RGB;

        public float XScale { get; set; }

        public float YScale { get; set; }

        public Truelight3DPointCloud? PointCloud { get; set; }
    }

    public sealed class Truelight3DPointCloud
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsDense { get; set; }

        public Truelight3DPoint[] Points { get; set; } = [];
    }

    public readonly struct Truelight3DPoint
    {
        public Truelight3DPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }
    }

    internal static class Truelight3DStatusExtensions
    {
        public static string ToDisplayString(this Truelight3DStatus status)
        {
            return status switch
            {
                Truelight3DStatus.STATUS_OK => "状态正常",
                Truelight3DStatus.STATUS_ERROR => "连接错误",
                Truelight3DStatus.STATUS_IP_ERROR => "IP错误",
                Truelight3DStatus.STATUS_Z_MOTOR_ERROR => "Z轴连接失败",
                Truelight3DStatus.STATUS_XY_MOTOR_ERROR => "XY轴连接失败",
                Truelight3DStatus.STATUS_XYZ_MOTOR_ERROR => "XYZ轴连接失败",
                Truelight3DStatus.STATUS_CAMERA_ERROR => "相机连接错误",
                Truelight3DStatus.STATUS_SLM_ERROR => "SLM连接失败",
                Truelight3DStatus.STATUS_SLM_CONFIG_ERROR => "SLM配置错误",
                Truelight3DStatus.STATUS_OCULAR_LENS_ERROR => "目镜连接错误",
                Truelight3DStatus.STATUS_CAMERA_PARAMETER_ERROR => "相机参数错误",
                Truelight3DStatus.STATUS_TURRET_ERROR => "转塔连接错误",
                Truelight3DStatus.STATUS_PZT_ERROR => "压电陶瓷连接错误",
                Truelight3DStatus.STATUS_TIME_OUT => "超时",
                Truelight3DStatus.STATUS_NOT_IMPLEMENTATION => "未实现",
                Truelight3DStatus.STATUS_NOT_ENABLED => "未使能或不可用",
                Truelight3DStatus.STATUS_BAD_PARAMETER => "错误参数",
                Truelight3DStatus.STATUS_NOT_CONNECTED => "未连接",
                Truelight3DStatus.STATUS_NOT_INITIALIZED => "未初始化",
                Truelight3DStatus.STATUS_NOT_SUPPORTED => "不支持",
                Truelight3DStatus.STATUS_OUT_OF_FLOW => "内存溢出",
                Truelight3DStatus.STATUS_OUT_OF_LAYER_MAX => "超过最大层数",
                Truelight3DStatus.STATUS_OUT_OF_RANGE => "超过范围",
                Truelight3DStatus.STATUS_ALREADY_INITIALIZED => "已初始化",
                _ => status.ToString(),
            };
        }
    }
}

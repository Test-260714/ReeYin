using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    public enum CoordinatedMotionKind
    {
        Line,
        XsegLine,
        Arc,
        Polyline,
        Custom
    }

    public sealed class CoordinatedMotionRequest
    {
        public CoordinatedMotionKind Kind { get; set; } = CoordinatedMotionKind.Line;
        public List<En_AxisNum> Axes { get; set; } = new();
        public Dictionary<En_AxisNum, double> TargetPositions { get; set; } = new();
        public List<Point> Points { get; set; } = new();
        public EN_SpeedType SpeedType { get; set; } = EN_SpeedType.Work;
        public bool WaitForEnd { get; set; } = true;
        public int Timeout { get; set; } = 60000;
        public int? BufferNo { get; set; }
        public LineInterPoParam? LineParam { get; set; }
        public ArcInterPoParam? ArcParam { get; set; }
        public CustomInterPoParam? CustomParam { get; set; }
        public Func<string>? CustomCommand { get; set; }
    }

    public enum SynchronizedTriggerMode
    {
        BufferedIo,
        PositionCompare,
        FixedDistancePulse,
        CoordinateArrayPulse,
        DataCollection
    }

    [Flags]
    public enum SynchronizedTriggerCapabilities
    {
        None = 0,
        BufferedIo = 1,
        PositionCompare = 2,
        FixedDistancePulse = 4,
        CoordinateArrayPulse = 8,
        DataCollection = 16
    }

    public sealed class SynchronizedTriggerRequest
    {
        public SynchronizedTriggerMode Mode { get; set; }
        public List<En_AxisNum> Axes { get; set; } = new();
        public List<Point> Points { get; set; } = new();
        public int BufferNo { get; set; } = 10;
        public double PulseWidth { get; set; } = 0.01d;
        public double Interval { get; set; } = 1d;
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
        public double TriggerWindow { get; set; } = 0.001d;
        public double Velocity { get; set; }
        public bool RouteConfigOutput { get; set; } = true;
        public int ConfigOutputIndex { get; set; }
        public int ConfigOutputCode { get; set; } = 7;
        public ushort DoMask { get; set; }
        public ushort DoValue { get; set; }
        public ushort DelayMilliseconds { get; set; }
        public bool WaitForEnd { get; set; } = true;
        public int Timeout { get; set; } = 60000;
        public PosComparisonOutputParam? PositionCompareParam { get; set; }
        public PosCompareData? PositionCompareDataTemplate { get; set; }
        public object? VendorRequest { get; set; }
    }

    public sealed class SynchronizedTriggerResult
    {
        public bool Success { get; set; }
        public int PulseCount { get; set; }
        public int PointCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object?> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

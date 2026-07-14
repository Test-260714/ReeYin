using ACS.SPiiPlusNET;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public enum AcsPeg2DPathKind
{
    Line,
    ArcCenter,
    Polyline
}

public enum AcsPegReferenceAxis
{
    Auto,
    X,
    Y
}

// Lightweight value type used by the path sampler and PEG requests; units follow the caller's motion units.
[Serializable]
public readonly struct AcsPoint2D
{
    public AcsPoint2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }
}

[Serializable]
public class AcsPegOutputConfig : BindableBase
{
    private En_AxisNum _axis = En_AxisNum.X;
    private int _engineToEncoderBitCode = 1;
    private int _generalOutputBitCode = 1;
    private int _outputIndex;
    private int _outputBitCode = 1;
    private double _pulseWidth = 10d;
    private int _timeout = 30000;
    private MotionFlags _flags;
    private bool _isEnabled;

    public En_AxisNum Axis
    {
        get => _axis;
        set
        {
            _axis = value;
            RaisePropertyChanged();
        }
    }

    public int EngineToEncoderBitCode
    {
        get => _engineToEncoderBitCode;
        set
        {
            _engineToEncoderBitCode = value;
            RaisePropertyChanged();
        }
    }

    public int GeneralOutputBitCode
    {
        get => _generalOutputBitCode;
        set
        {
            _generalOutputBitCode = value;
            RaisePropertyChanged();
        }
    }

    public int OutputIndex
    {
        get => _outputIndex;
        set
        {
            _outputIndex = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public int OutputBitCode
    {
        get => _outputBitCode;
        set
        {
            _outputBitCode = value;
            RaisePropertyChanged();
        }
    }

    public double PulseWidth
    {
        get => _pulseWidth;
        set
        {
            _pulseWidth = value;
            RaisePropertyChanged();
        }
    }

    public int Timeout
    {
        get => _timeout;
        set
        {
            _timeout = Math.Max(1000, value);
            RaisePropertyChanged();
        }
    }

    public MotionFlags Flags
    {
        get => _flags;
        set
        {
            _flags = value;
            RaisePropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            RaisePropertyChanged();
        }
    }
}

[Serializable]
public class AcsPegIncrementalRequest
{
    public AcsPegOutputConfig Output { get; set; } = new();

    public double FirstPoint { get; set; }

    public double Interval { get; set; }

    public double LastPoint { get; set; }

    public bool StartImmediately { get; set; } = true;

    public bool WaitReadyBeforeStart { get; set; } = true;
}

[Serializable]
public class AcsPegRandomRequest
{
    public AcsPegOutputConfig Output { get; set; } = new();

    public IList<double> Points { get; set; } = new List<double>();

    public IList<int> States { get; set; } = new List<int>();

    public string PointArrayName { get; set; } = "RY_PEG_POINTS";

    public string StateArrayName { get; set; } = "RY_PEG_STATES";

    public int Mode { get; set; }

    public int FirstIndex { get; set; }

    public int? LastIndex { get; set; }

    public bool StartImmediately { get; set; } = true;

    public bool WaitReadyBeforeStart { get; set; } = true;
}

[Serializable]
public class AcsPeg2DPathRequest
{
    public AcsPegOutputConfig Output { get; set; } = new();

    public En_AxisNum XAxis { get; set; } = En_AxisNum.X;

    public En_AxisNum YAxis { get; set; } = En_AxisNum.Y;

    public AcsPeg2DPathKind PathKind { get; set; } = AcsPeg2DPathKind.Line;

    // Auto chooses the first monotonic axis that can represent the 2D path in a single PEG point table.
    public AcsPegReferenceAxis ReferenceAxis { get; set; } = AcsPegReferenceAxis.Auto;

    public AcsPoint2D Start { get; set; }

    public AcsPoint2D End { get; set; }

    public AcsPoint2D Center { get; set; }

    public DirOfRotation ArcDirection { get; set; } = DirOfRotation.逆时针;

    public IList<AcsPoint2D> Points { get; set; } = new List<AcsPoint2D>();

    public double Interval { get; set; }

    public double PulseWidth { get; set; } = 10d;

    public string PointArrayName { get; set; } = "RY_PEG_POINTS";

    public string StateArrayName { get; set; } = "RY_PEG_STATES";

    public int StateValue { get; set; } = 1;

    public bool StartImmediately { get; set; } = true;
}

[Serializable]
public class AcsDataCollectionRequest
{
    public En_AxisNum Axis { get; set; } = En_AxisNum.X;

    public string ArrayName { get; set; } = "RY_DC_DATA";

    public int SampleCount { get; set; } = 1000;

    public double Period { get; set; } = 1d;

    public string Variables { get; set; } = string.Empty;

    public DataCollectionFlags Flags { get; set; }

    public int Timeout { get; set; } = 30000;
}

[Serializable]
public class AcsDataCollectionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ArrayName { get; set; } = string.Empty;

    public int SampleCount { get; set; }

    public double[] Values { get; set; } = Array.Empty<double>();

    public double[] MatrixValues { get; set; } = Array.Empty<double>();
}

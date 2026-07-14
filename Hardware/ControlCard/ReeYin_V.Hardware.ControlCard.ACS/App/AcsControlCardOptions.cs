using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

[Serializable]
public class AcsControlCardOptions : BindableBase
{
    private AcsConnectionMode _connectionMode = AcsConnectionMode.Simulator;
    private string _remoteAddress = "10.0.0.183";
    private bool _useTcp = true;
    private int _ethernetPort = (int)global::ACS.SPiiPlusNET.EthernetCommOption.ACSC_SOCKET_STREAM_PORT;
    private int _serialPort = 1;
    private int _serialBaudRate = -1;
    private int _pciSlotNumber;
    private int _internalTimeout = 30000;
    private int _interpolationBufferNo = 9;
    private int _digitalInputCount = 32;
    private int _digitalOutputCount = 32;
    private AxisStopMode _homeBeforeRunStopMode = AxisStopMode.减速停止;
    private AcsXyHomeBufferConfig _xyHomeBuffer = new();
    private AcsLciFixedDistancePulseConfig _lciFixedDistancePulse = new();
    private AcsLciSegmentCircleConfig _lciSegmentCircle = new();
    private ObservableCollection<AcsAxisHomeBufferConfig> _homeBuffers = new();
    private ObservableCollection<AcsPegOutputConfig> _pegOutputs = new();
    private string _defaultPegPointArrayName = "RY_PEG_POINTS";
    private string _defaultPegStateArrayName = "RY_PEG_STATES";
    private string _defaultDataCollectionArrayName = "RY_DC_DATA";
    private int _defaultDataCollectionSampleCount = 1000;
    private double _defaultDataCollectionPeriod = 1d;

    public AcsConnectionMode ConnectionMode
    {
        get => _connectionMode;
        set
        {
            _connectionMode = value;
            RaisePropertyChanged();
        }
    }

    public string RemoteAddress
    {
        get => _remoteAddress;
        set
        {
            _remoteAddress = value;
            RaisePropertyChanged();
        }
    }

    public bool UseTcp
    {
        get => _useTcp;
        set
        {
            _useTcp = value;
            RaisePropertyChanged();
        }
    }

    public int EthernetPort
    {
        get => _ethernetPort;
        set
        {
            _ethernetPort = value;
            RaisePropertyChanged();
        }
    }

    public int SerialPort
    {
        get => _serialPort;
        set
        {
            _serialPort = value;
            RaisePropertyChanged();
        }
    }

    public int SerialBaudRate
    {
        get => _serialBaudRate;
        set
        {
            _serialBaudRate = value;
            RaisePropertyChanged();
        }
    }

    public int PciSlotNumber
    {
        get => _pciSlotNumber;
        set
        {
            _pciSlotNumber = value;
            RaisePropertyChanged();
        }
    }

    public int InternalTimeout
    {
        get => _internalTimeout;
        set
        {
            _internalTimeout = Math.Max(1000, value);
            RaisePropertyChanged();
        }
    }

    public int InterpolationBufferNo
    {
        get => _interpolationBufferNo;
        set
        {
            _interpolationBufferNo = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int DigitalInputCount
    {
        get => _digitalInputCount;
        set
        {
            _digitalInputCount = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public int DigitalOutputCount
    {
        get => _digitalOutputCount;
        set
        {
            _digitalOutputCount = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public AxisStopMode HomeBeforeRunStopMode
    {
        get => _homeBeforeRunStopMode;
        set
        {
            _homeBeforeRunStopMode = value;
            RaisePropertyChanged();
        }
    }

    public AcsXyHomeBufferConfig XyHomeBuffer
    {
        get => _xyHomeBuffer;
        set
        {
            _xyHomeBuffer = value ?? new AcsXyHomeBufferConfig();
            RaisePropertyChanged();
        }
    }

    public AcsLciFixedDistancePulseConfig LciFixedDistancePulse
    {
        get => _lciFixedDistancePulse;
        set
        {
            _lciFixedDistancePulse = value ?? new AcsLciFixedDistancePulseConfig();
            RaisePropertyChanged();
        }
    }

    public AcsLciSegmentCircleConfig LciSegmentCircle
    {
        get => _lciSegmentCircle;
        set
        {
            _lciSegmentCircle = value ?? new AcsLciSegmentCircleConfig();
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<AcsAxisHomeBufferConfig> HomeBuffers
    {
        get => _homeBuffers;
        set
        {
            _homeBuffers = value ?? new ObservableCollection<AcsAxisHomeBufferConfig>();
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<AcsPegOutputConfig> PegOutputs
    {
        get => _pegOutputs;
        set
        {
            _pegOutputs = value ?? new ObservableCollection<AcsPegOutputConfig>();
            RaisePropertyChanged();
        }
    }

    public string DefaultPegPointArrayName
    {
        get => _defaultPegPointArrayName;
        set
        {
            _defaultPegPointArrayName = string.IsNullOrWhiteSpace(value) ? "RY_PEG_POINTS" : value.Trim();
            RaisePropertyChanged();
        }
    }

    public string DefaultPegStateArrayName
    {
        get => _defaultPegStateArrayName;
        set
        {
            _defaultPegStateArrayName = string.IsNullOrWhiteSpace(value) ? "RY_PEG_STATES" : value.Trim();
            RaisePropertyChanged();
        }
    }

    public string DefaultDataCollectionArrayName
    {
        get => _defaultDataCollectionArrayName;
        set
        {
            _defaultDataCollectionArrayName = string.IsNullOrWhiteSpace(value) ? "RY_DC_DATA" : value.Trim();
            RaisePropertyChanged();
        }
    }

    public int DefaultDataCollectionSampleCount
    {
        get => _defaultDataCollectionSampleCount;
        set
        {
            _defaultDataCollectionSampleCount = Math.Max(1, value);
            RaisePropertyChanged();
        }
    }

    public double DefaultDataCollectionPeriod
    {
        get => _defaultDataCollectionPeriod;
        set
        {
            _defaultDataCollectionPeriod = Math.Max(double.Epsilon, value);
            RaisePropertyChanged();
        }
    }

    [JsonIgnore]
    public IReadOnlyList<AcsConnectionMode> ConnectionModes =>
        Enum.GetValues(typeof(AcsConnectionMode)).Cast<AcsConnectionMode>().ToList();

    private static int GetNextAvailableBufferNo(IEnumerable<AcsAxisHomeBufferConfig> homeBuffers)
    {
        var used = homeBuffers
            .Where(buffer => buffer != null)
            .Select(buffer => buffer.BufferNo)
            .ToHashSet();

        for (var bufferNo = 1; bufferNo <= 64; bufferNo++)
        {
            if (!used.Contains(bufferNo))
            {
                return bufferNo;
            }
        }

        return used.Contains(0) ? 64 : 0;
    }

    public void EnsureHomeBuffers(IEnumerable<En_AxisNum>? axes)
    {
        HomeBuffers ??= new ObservableCollection<AcsAxisHomeBufferConfig>();

        var targetAxes = axes?.Distinct().ToList();
        if (targetAxes == null || targetAxes.Count == 0)
        {
            targetAxes = new List<En_AxisNum>
            {
                En_AxisNum.X,
                En_AxisNum.Y,
                En_AxisNum.Z,
                En_AxisNum.R
            };
        }

        var existingAxes = HomeBuffers
            .Where(buffer => buffer != null)
            .Select(buffer => buffer.Axis)
            .ToHashSet();

        foreach (var axis in targetAxes)
        {
            if (!existingAxes.Add(axis))
            {
                continue;
            }

            var bufferNo = GetNextAvailableBufferNo(HomeBuffers);

            HomeBuffers.Add(new AcsAxisHomeBufferConfig
            {
                Axis = axis,
                BufferNo = bufferNo,
                IsEnabled = false,
                Timeout = InternalTimeout,
                StopAxisBeforeRun = true,
                ResetFeedbackAfterSuccess = false,
                ResetPosition = 0
            });
        }
    }

    public void EnsurePegOutputs(IEnumerable<En_AxisNum>? axes)
    {
        PegOutputs ??= new ObservableCollection<AcsPegOutputConfig>();

        var targetAxes = axes?.Distinct().ToList();
        if (targetAxes == null || targetAxes.Count == 0)
        {
            targetAxes = new List<En_AxisNum>
            {
                En_AxisNum.X,
                En_AxisNum.Y,
                En_AxisNum.Z,
                En_AxisNum.R
            };
        }

        var existingAxes = PegOutputs
            .Where(output => output != null)
            .Select(output => output.Axis)
            .ToHashSet();

        foreach (var axis in targetAxes)
        {
            if (!existingAxes.Add(axis))
            {
                continue;
            }

            PegOutputs.Add(new AcsPegOutputConfig
            {
                Axis = axis,
                IsEnabled = false,
                PulseWidth = 10d,
                Timeout = InternalTimeout
            });
        }
    }
}

[Serializable]
public class AcsLciFixedDistancePulseConfig : BindableBase
{
    private int _bufferNo = 10;
    private int _axisX;
    private int _axisY = 1;
    private double _pulseWidth = 0.01d;
    private double _interval = 1d;
    private double _startDistance;
    private double _endDistance;
    private double _velocity;
    private double _acceleration;
    private double _deceleration;
    private double _killDeceleration;
    private double _jerk;
    private bool _routeConfigOutput = true;
    private int _configOutputIndex;
    private int _configOutputCode = 7;
    private int _timeout = 60000;
    private string _pointsText = string.Join(Environment.NewLine, "0,0", "100,0", "100,100");

    public int BufferNo
    {
        get => _bufferNo;
        set
        {
            _bufferNo = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int AxisX
    {
        get => _axisX;
        set
        {
            _axisX = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int AxisY
    {
        get => _axisY;
        set
        {
            _axisY = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public double PulseWidth
    {
        get => _pulseWidth;
        set
        {
            _pulseWidth = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Interval
    {
        get => _interval;
        set
        {
            _interval = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double StartDistance
    {
        get => _startDistance;
        set
        {
            _startDistance = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double EndDistance
    {
        get => _endDistance;
        set
        {
            _endDistance = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Velocity
    {
        get => _velocity;
        set
        {
            _velocity = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Deceleration
    {
        get => _deceleration;
        set
        {
            _deceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double KillDeceleration
    {
        get => _killDeceleration;
        set
        {
            _killDeceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Jerk
    {
        get => _jerk;
        set
        {
            _jerk = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public bool RouteConfigOutput
    {
        get => _routeConfigOutput;
        set
        {
            _routeConfigOutput = value;
            RaisePropertyChanged();
        }
    }

    public int ConfigOutputIndex
    {
        get => _configOutputIndex;
        set
        {
            _configOutputIndex = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public int ConfigOutputCode
    {
        get => _configOutputCode;
        set
        {
            _configOutputCode = Math.Max(0, value);
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

    public string PointsText
    {
        get => _pointsText;
        set
        {
            _pointsText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public SpeedSetting ToMotionProfile()
    {
        return new SpeedSetting
        {
            MaxSpeed = Velocity,
            AccSpeed = Acceleration,
            DecSpeed = Deceleration,
            KillDecSpeed = KillDeceleration,
            Jerk = Jerk
        };
    }
}

[Serializable]
public class AcsLciSegmentCircleConfig : BindableBase
{
    private int _bufferNo = 10;
    private int _axisX;
    private int _axisY = 1;
    private double _velocity;
    private double _acceleration;
    private double _deceleration;
    private double _killDeceleration;
    private double _jerk;
    private double _startX;
    private double _startY;
    private double _centerX = 10d;
    private double _centerY = 5d;
    private double _radius = 5d;
    private int _gateActiveState = 1;
    private int _timeout = 60000;

    public int BufferNo
    {
        get => _bufferNo;
        set
        {
            _bufferNo = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int AxisX
    {
        get => _axisX;
        set
        {
            _axisX = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int AxisY
    {
        get => _axisY;
        set
        {
            _axisY = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public double Velocity
    {
        get => _velocity;
        set
        {
            _velocity = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Deceleration
    {
        get => _deceleration;
        set
        {
            _deceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double KillDeceleration
    {
        get => _killDeceleration;
        set
        {
            _killDeceleration = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double Jerk
    {
        get => _jerk;
        set
        {
            _jerk = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public double StartX
    {
        get => _startX;
        set
        {
            _startX = value;
            RaisePropertyChanged();
        }
    }

    public double StartY
    {
        get => _startY;
        set
        {
            _startY = value;
            RaisePropertyChanged();
        }
    }

    public double CenterX
    {
        get => _centerX;
        set
        {
            _centerX = value;
            RaisePropertyChanged();
        }
    }

    public double CenterY
    {
        get => _centerY;
        set
        {
            _centerY = value;
            RaisePropertyChanged();
        }
    }

    public double Radius
    {
        get => _radius;
        set
        {
            _radius = Math.Max(0d, value);
            RaisePropertyChanged();
        }
    }

    public int GateActiveState
    {
        get => _gateActiveState;
        set
        {
            _gateActiveState = value;
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

    public SpeedSetting ToMotionProfile()
    {
        return new SpeedSetting
        {
            MaxSpeed = Velocity,
            AccSpeed = Acceleration,
            DecSpeed = Deceleration,
            KillDecSpeed = KillDeceleration,
            Jerk = Jerk
        };
    }
}

[Serializable]
public class AcsAxisHomeBufferConfig : BindableBase
{
    private En_AxisNum _axis = En_AxisNum.X;
    private int _bufferNo = 1;
    private bool _isEnabled;
    private int _timeout = 30000;
    private bool _stopAxisBeforeRun = true;
    private bool _resetFeedbackAfterSuccess;
    private double _resetPosition;

    public En_AxisNum Axis
    {
        get => _axis;
        set
        {
            _axis = value;
            RaisePropertyChanged();
        }
    }

    public int BufferNo
    {
        get => _bufferNo;
        set
        {
            _bufferNo = Math.Clamp(value, 0, 64);
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

    public int Timeout
    {
        get => _timeout;
        set
        {
            _timeout = Math.Max(1000, value);
            RaisePropertyChanged();
        }
    }

    public bool StopAxisBeforeRun
    {
        get => _stopAxisBeforeRun;
        set
        {
            _stopAxisBeforeRun = value;
            RaisePropertyChanged();
        }
    }

    public bool ResetFeedbackAfterSuccess
    {
        get => _resetFeedbackAfterSuccess;
        set
        {
            _resetFeedbackAfterSuccess = value;
            RaisePropertyChanged();
        }
    }

    public double ResetPosition
    {
        get => _resetPosition;
        set
        {
            _resetPosition = value;
            RaisePropertyChanged();
        }
    }
}

[Serializable]
public class AcsXyHomeBufferConfig : BindableBase
{
    private int _xBufferNo = 1;
    private int _yBufferNo = 2;
    private bool _isEnabled = true;
    private int _timeout = 60000;
    private bool _stopAxesBeforeRun = true;
    private int _xPhysicalAxis = 2;
    private int _yPhysicalAxis;

    public int BufferNo
    {
        get => XBufferNo;
        set
        {
            XBufferNo = value;
            RaisePropertyChanged();
        }
    }

    public int XBufferNo
    {
        get => _xBufferNo;
        set
        {
            _xBufferNo = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int YBufferNo
    {
        get => _yBufferNo;
        set
        {
            _yBufferNo = Math.Clamp(value, 0, 64);
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

    public int Timeout
    {
        get => _timeout;
        set
        {
            _timeout = Math.Max(1000, value);
            RaisePropertyChanged();
        }
    }

    public bool StopAxesBeforeRun
    {
        get => _stopAxesBeforeRun;
        set
        {
            _stopAxesBeforeRun = value;
            RaisePropertyChanged();
        }
    }

    public int XPhysicalAxis
    {
        get => _xPhysicalAxis;
        set
        {
            _xPhysicalAxis = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }

    public int YPhysicalAxis
    {
        get => _yPhysicalAxis;
        set
        {
            _yPhysicalAxis = Math.Clamp(value, 0, 64);
            RaisePropertyChanged();
        }
    }
}

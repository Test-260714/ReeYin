using ACS.SPiiPlusNET;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard : ControlCardBase
{
    private readonly object _syncRoot = new();
    private Api _api = new();
    private AcsControlCardOptions? _options;

    public AcsControlCard()
    {
        VenderName = "ACS";
        CardType = "SPiiPlus";
        NickName = "ACS SPiiPlus Control Card";
        EnsureOptions();
    }

    protected override int MotionTimeoutMs => InternalTimeout;

    protected override void OnConfigChanged()
    {
        EnsureOptions();
    }

    public AcsControlCardOptions Options
    {
        get
        {
            EnsureOptions();
            return _options!;
        }
        set
        {
            _options = value ?? new AcsControlCardOptions();
            EnsureOptions();
            RaisePropertyChanged();
        }
    }

    public AcsConnectionMode ConnectionMode
    {
        get => Options.ConnectionMode;
        set
        {
            Options.ConnectionMode = value;
            RaisePropertyChanged();
        }
    }

    public string RemoteAddress
    {
        get => Options.RemoteAddress;
        set
        {
            Options.RemoteAddress = value;
            RaisePropertyChanged();
        }
    }

    public bool UseTcp
    {
        get => Options.UseTcp;
        set
        {
            Options.UseTcp = value;
            RaisePropertyChanged();
        }
    }

    public int EthernetPort
    {
        get => Options.EthernetPort;
        set
        {
            Options.EthernetPort = value;
            RaisePropertyChanged();
        }
    }

    public int SerialPort
    {
        get => Options.SerialPort;
        set
        {
            Options.SerialPort = value;
            RaisePropertyChanged();
        }
    }

    public int SerialBaudRate
    {
        get => Options.SerialBaudRate;
        set
        {
            Options.SerialBaudRate = value;
            RaisePropertyChanged();
        }
    }

    public int PciSlotNumber
    {
        get => Options.PciSlotNumber;
        set
        {
            Options.PciSlotNumber = value;
            RaisePropertyChanged();
        }
    }

    public int InternalTimeout
    {
        get => Options.InternalTimeout;
        set
        {
            Options.InternalTimeout = value;
            RaisePropertyChanged();
        }
    }

    public int InterpolationBufferNo
    {
        get => Options.InterpolationBufferNo;
        set
        {
            Options.InterpolationBufferNo = value;
            RaisePropertyChanged();
        }
    }

    public int DigitalInputCount
    {
        get => Options.DigitalInputCount;
        set
        {
            Options.DigitalInputCount = value;
            RaisePropertyChanged();
        }
    }

    public int DigitalOutputCount
    {
        get => Options.DigitalOutputCount;
        set
        {
            Options.DigitalOutputCount = value;
            RaisePropertyChanged();
        }
    }

    public void EnsureOptions()
    {
        _options ??= new AcsControlCardOptions();
        var axes = Config?.AllAxis?.Select(axis => axis.AxisNum).ToArray();
        _options.EnsureHomeBuffers(axes);
        _options.EnsurePegOutputs(axes);
    }
}

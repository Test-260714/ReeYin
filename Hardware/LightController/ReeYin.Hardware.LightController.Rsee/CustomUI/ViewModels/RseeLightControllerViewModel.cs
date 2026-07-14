using Prism.Commands;
using Prism.Dialogs;
using ReeYin.Hardware.LightController.Rsee.CustomUI.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI;
using System.Collections.ObjectModel;

namespace ReeYin.Hardware.LightController.Rsee.CustomUI.ViewModels;

public class RseeLightControllerViewModel : DialogViewModelBase
{
    private RseeLightController? _controller;
    private bool _isConnected;
    private string _ip = "192.168.1.252";
    private int _port = 8234;
    private int _globalPulseWidthUs = 100;
    private int _internalTriggerCycleMs = 100;
    private string _statusMessage = "Ready";
    private ObservableCollection<RseeChannelInfo> _channelList = new();

    public bool IsConnected
    {
        get { return _isConnected; }
        set { _isConnected = value; RaisePropertyChanged(); }
    }

    public string IP
    {
        get { return _ip; }
        set { _ip = value; RaisePropertyChanged(); }
    }

    public int Port
    {
        get { return _port; }
        set { _port = value; RaisePropertyChanged(); }
    }

    public int GlobalPulseWidthUs
    {
        get { return _globalPulseWidthUs; }
        set { _globalPulseWidthUs = value; RaisePropertyChanged(); }
    }

    public int InternalTriggerCycleMs
    {
        get { return _internalTriggerCycleMs; }
        set { _internalTriggerCycleMs = value; RaisePropertyChanged(); }
    }

    public string StatusMessage
    {
        get { return _statusMessage; }
        set { _statusMessage = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<RseeChannelInfo> ChannelList
    {
        get { return _channelList; }
        set { _channelList = value; RaisePropertyChanged(); }
    }

    public override void OnDialogOpened(IDialogParameters parameters)
    {
        base.OnDialogOpened(parameters);

        if (parameters.ContainsKey("Param"))
        {
            _controller = parameters.GetValue<object>("Param") as RseeLightController;
        }

        _controller ??= new RseeLightController();
        IP = _controller.IP;
        Port = _controller.Port;
        IsConnected = _controller.IsConnected;
        InitChannelList();
    }

    public override void InitParam()
    {
    }

    private DelegateCommand? _loadCommand;
    public DelegateCommand LoadCommand => _loadCommand ??= new DelegateCommand(RefreshStatus);

    private DelegateCommand<string>? _generalCommand;
    public DelegateCommand<string> GeneralCommand => _generalCommand ??= new DelegateCommand<string>(ExecuteGeneralCommand);

    private DelegateCommand<RseeChannelInfo>? _setChannelCommand;
    public DelegateCommand<RseeChannelInfo> SetChannelCommand => _setChannelCommand ??= new DelegateCommand<RseeChannelInfo>(SetChannelPulseWidth);

    private DelegateCommand<RseeChannelInfo>? _getChannelCommand;
    public DelegateCommand<RseeChannelInfo> GetChannelCommand => _getChannelCommand ??= new DelegateCommand<RseeChannelInfo>(ReadChannelPulseWidth);

    private void ExecuteGeneralCommand(string? command)
    {
        switch (command)
        {
            case "Connect":
                Connect();
                break;
            case "Disconnect":
                Disconnect();
                break;
            case "Refresh":
                RefreshStatus();
                break;
            case "ApplyAllPulseWidth":
                ApplyAllPulseWidth();
                break;
            case "AllOff":
                SetAllChannels(0);
                break;
            case "ExternalTrigger":
                SetTriggerMode(1);
                break;
            case "InternalTrigger":
                SetTriggerMode(2);
                break;
            case "SetCycle":
                SetInternalTriggerCycle();
                break;
            case "ReadCycle":
                ReadInternalTriggerCycle();
                break;
            case "OK":
                if (_controller != null)
                {
                    _controller.IP = IP;
                    _controller.Port = Port;
                }
                CloseDialog(ButtonResult.OK, new DialogParameters { { "Param", _controller! } });
                break;
            case "Cancel":
                CloseDialog(ButtonResult.No);
                break;
        }
    }

    private void Connect()
    {
        if (_controller == null)
        {
            _controller = new RseeLightController();
        }

        _controller.IP = IP;
        _controller.Port = Port;
        _controller.ConnectionType = 0;
        IsConnected = _controller.Init();
        StatusMessage = IsConnected ? "Connected" : "Connect failed";
        if (IsConnected)
        {
            RefreshStatus();
        }
    }

    private void Disconnect()
    {
        _controller?.Close();
        IsConnected = false;
        StatusMessage = "Disconnected";
    }

    private void RefreshStatus()
    {
        if (_controller == null)
        {
            return;
        }

        IsConnected = _controller.IsConnected;
        if (!IsConnected)
        {
            StatusMessage = "Not connected";
            return;
        }

        foreach (var channel in ChannelList)
        {
            ReadChannelPulseWidth(channel);
        }
    }

    private void ApplyAllPulseWidth()
    {
        SetAllChannels(GlobalPulseWidthUs);
    }

    private void SetAllChannels(int pulseWidthUs)
    {
        if (!CheckConnected())
        {
            return;
        }

        var success = true;
        foreach (var channel in ChannelList)
        {
            channel.PulseWidthUs = pulseWidthUs;
            success &= _controller!.SetStrobeTime(channel.ChannelIndex, pulseWidthUs);
            channel.IsOn = pulseWidthUs > 0;
        }

        StatusMessage = success ? "Pulse width applied" : "Apply pulse width failed";
    }

    private void SetChannelPulseWidth(RseeChannelInfo channel)
    {
        if (!CheckConnected() || channel == null)
        {
            return;
        }

        bool success = _controller!.SetStrobeTime(channel.ChannelIndex, channel.PulseWidthUs);
        channel.IsOn = success && channel.PulseWidthUs > 0;
        StatusMessage = success ? $"CH{channel.ChannelIndex} applied" : $"CH{channel.ChannelIndex} failed";
    }

    private void ReadChannelPulseWidth(RseeChannelInfo channel)
    {
        if (!CheckConnected() || channel == null)
        {
            return;
        }

        int value = _controller!.GetStrobeTime(channel.ChannelIndex);
        if (value >= 0)
        {
            channel.PulseWidthUs = value;
            channel.IsOn = value > 0;
            StatusMessage = $"CH{channel.ChannelIndex} read";
        }
        else
        {
            StatusMessage = $"CH{channel.ChannelIndex} read failed";
        }
    }

    private void SetTriggerMode(int mode)
    {
        if (!CheckConnected())
        {
            return;
        }

        bool success = _controller!.SetTriggerMode(mode);
        StatusMessage = success ? (mode == 2 ? "Internal trigger" : "External trigger") : "Set trigger failed";
    }

    private void SetInternalTriggerCycle()
    {
        if (!CheckConnected())
        {
            return;
        }

        bool success = _controller!.SetInternalTriggerCycle(InternalTriggerCycleMs);
        StatusMessage = success ? "Cycle applied" : "Set cycle failed";
    }

    private void ReadInternalTriggerCycle()
    {
        if (!CheckConnected())
        {
            return;
        }

        int cycle = _controller!.GetInternalTriggerCycle();
        if (cycle >= 0)
        {
            InternalTriggerCycleMs = cycle;
            StatusMessage = "Cycle read";
        }
        else
        {
            StatusMessage = "Read cycle failed";
        }
    }

    private bool CheckConnected(bool showWarning = true)
    {
        if (_controller?.IsConnected == true)
        {
            return true;
        }

        IsConnected = false;
        StatusMessage = "Not connected";
        if (showWarning)
        {
            MessageView.Ins.MessageBoxShow("请先连接锐视光源控制器", eMsgType.Warn);
        }
        return false;
    }

    private void InitChannelList()
    {
        ChannelList.Clear();
        var channelCount = _controller?.ChannelCount > 0 ? _controller.ChannelCount : 4;
        for (int i = 1; i <= channelCount; i++)
        {
            ChannelList.Add(new RseeChannelInfo
            {
                ChannelIndex = i,
                PulseWidthUs = 0,
                IsOn = false
            });
        }
    }
}

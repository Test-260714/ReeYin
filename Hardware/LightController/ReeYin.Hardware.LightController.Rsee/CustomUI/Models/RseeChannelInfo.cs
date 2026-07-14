namespace ReeYin.Hardware.LightController.Rsee.CustomUI.Models;

public class RseeChannelInfo : BindableBase
{
    private int _channelIndex;
    public int ChannelIndex
    {
        get { return _channelIndex; }
        set { _channelIndex = value; RaisePropertyChanged(); }
    }

    private int _pulseWidthUs;
    public int PulseWidthUs
    {
        get { return _pulseWidthUs; }
        set { _pulseWidthUs = value; RaisePropertyChanged(); }
    }

    private bool _isOn;
    public bool IsOn
    {
        get { return _isOn; }
        set { _isOn = value; RaisePropertyChanged(); }
    }
}
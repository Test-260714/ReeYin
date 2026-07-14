using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public sealed class AcsAxisTestSnapshot
{
    public En_AxisNum Axis { get; init; }

    public bool IsEnabled { get; init; }

    public bool IsMoving { get; init; }

    public bool IsPositiveLimit { get; init; }

    public bool IsNegativeLimit { get; init; }

    public bool IsResetCompleted { get; init; }

    public double CurrentPosition { get; init; }

    public int AxisStatus { get; init; }

    public string Summary =>
        $"轴={Axis}; 位置={CurrentPosition:F3}; 使能={FormatBool(IsEnabled)}; 运动中={FormatBool(IsMoving)}; 正限位={FormatBool(IsPositiveLimit)}; 负限位={FormatBool(IsNegativeLimit)}; 已回零={FormatBool(IsResetCompleted)}; 状态=0x{AxisStatus:X}";

    private static string FormatBool(bool value)
    {
        return value ? "是" : "否";
    }
}

public sealed class AcsAxisMotionProfile : BindableBase
{
    private En_AxisNum _axis;
    private double _velocity;
    private double _acceleration;
    private double _deceleration;
    private double _killDeceleration;
    private double _jerk;
    private string _message = string.Empty;

    public En_AxisNum Axis
    {
        get => _axis;
        set
        {
            _axis = value;
            RaisePropertyChanged();
        }
    }

    public double Velocity
    {
        get => _velocity;
        set
        {
            _velocity = value;
            RaisePropertyChanged();
        }
    }

    public double Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = value;
            RaisePropertyChanged();
        }
    }

    public double Deceleration
    {
        get => _deceleration;
        set
        {
            _deceleration = value;
            RaisePropertyChanged();
        }
    }

    public double KillDeceleration
    {
        get => _killDeceleration;
        set
        {
            _killDeceleration = value;
            RaisePropertyChanged();
        }
    }

    public double Jerk
    {
        get => _jerk;
        set
        {
            _jerk = value;
            RaisePropertyChanged();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }
}

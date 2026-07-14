using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using System;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public sealed class AcsAxisMonitorRow : BindableBase
{
    private readonly En_AxisNum _axis;
    private bool _hasSample;
    private double _lastPosition;
    private DateTime _lastSampleTime = DateTime.MinValue;
    private double _currentPosition;
    private double _speed;
    private bool _isOnline;
    private bool _isEnabled;
    private bool _isMoving;
    private bool _isPositiveLimit;
    private bool _isNegativeLimit;
    private bool _isResetCompleted;
    private int _axisStatus;
    private string _message = "未刷新。";
    private DateTime _lastUpdated = DateTime.MinValue;

    public AcsAxisMonitorRow(En_AxisNum axis)
    {
        _axis = axis;
    }

    public En_AxisNum Axis => _axis;

    public string AxisName => Axis.ToString();

    public double CurrentPosition
    {
        get => _currentPosition;
        private set
        {
            _currentPosition = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPositionText));
        }
    }

    public string CurrentPositionText => CurrentPosition.ToString("F3");

    public double Speed
    {
        get => _speed;
        private set
        {
            _speed = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SpeedText));
        }
    }

    public string SpeedText => Speed.ToString("F3");

    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            _isOnline = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(StateText));
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            _isEnabled = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsEnabledText));
            RaisePropertyChanged(nameof(StateText));
        }
    }

    public string IsEnabledText => FormatBool(IsEnabled);

    public bool IsMoving
    {
        get => _isMoving;
        private set
        {
            _isMoving = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsMovingText));
            RaisePropertyChanged(nameof(StateText));
        }
    }

    public string IsMovingText => FormatBool(IsMoving);

    public bool IsPositiveLimit
    {
        get => _isPositiveLimit;
        private set
        {
            _isPositiveLimit = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsPositiveLimitText));
            RaisePropertyChanged(nameof(StateText));
            RaisePropertyChanged(nameof(HasAlarm));
        }
    }

    public string IsPositiveLimitText => FormatBool(IsPositiveLimit);

    public bool IsNegativeLimit
    {
        get => _isNegativeLimit;
        private set
        {
            _isNegativeLimit = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsNegativeLimitText));
            RaisePropertyChanged(nameof(StateText));
            RaisePropertyChanged(nameof(HasAlarm));
        }
    }

    public string IsNegativeLimitText => FormatBool(IsNegativeLimit);

    public bool IsResetCompleted
    {
        get => _isResetCompleted;
        private set
        {
            _isResetCompleted = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsResetCompletedText));
        }
    }

    public string IsResetCompletedText => FormatBool(IsResetCompleted);

    public int AxisStatus
    {
        get => _axisStatus;
        private set
        {
            _axisStatus = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(AxisStatusText));
        }
    }

    public string AxisStatusText => $"0x{AxisStatus:X}";

    public bool HasAlarm => IsOnline && (IsPositiveLimit || IsNegativeLimit);

    public string StateText
    {
        get
        {
            if (!IsOnline)
            {
                return "离线";
            }

            if (IsPositiveLimit || IsNegativeLimit)
            {
                return "限位触发";
            }

            if (IsMoving)
            {
                return "运动中";
            }

            return IsEnabled ? "已使能" : "未使能";
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            _message = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        private set
        {
            _lastUpdated = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(LastUpdatedText));
        }
    }

    public string LastUpdatedText => LastUpdated == DateTime.MinValue
        ? "-"
        : LastUpdated.ToString("HH:mm:ss.fff");

    public void ApplySnapshot(AcsAxisTestSnapshot snapshot, DateTime sampleTime, string message)
    {
        IsOnline = true;
        IsEnabled = snapshot.IsEnabled;
        IsMoving = snapshot.IsMoving;
        IsPositiveLimit = snapshot.IsPositiveLimit;
        IsNegativeLimit = snapshot.IsNegativeLimit;
        IsResetCompleted = snapshot.IsResetCompleted;
        AxisStatus = snapshot.AxisStatus;

        Speed = CalculateSpeed(snapshot.CurrentPosition, sampleTime);
        CurrentPosition = snapshot.CurrentPosition;
        Message = string.IsNullOrWhiteSpace(message) ? "读取成功。" : message;
        LastUpdated = sampleTime;
    }

    public void ApplyError(string message, DateTime sampleTime)
    {
        IsOnline = false;
        IsEnabled = false;
        IsMoving = false;
        IsPositiveLimit = false;
        IsNegativeLimit = false;
        IsResetCompleted = false;
        Speed = 0d;
        Message = string.IsNullOrWhiteSpace(message) ? "读取失败。" : message;
        LastUpdated = sampleTime;
    }

    private double CalculateSpeed(double currentPosition, DateTime sampleTime)
    {
        if (!_hasSample)
        {
            _hasSample = true;
            _lastPosition = currentPosition;
            _lastSampleTime = sampleTime;
            return 0d;
        }

        var seconds = (sampleTime - _lastSampleTime).TotalSeconds;
        if (seconds <= 0.001d)
        {
            _lastPosition = currentPosition;
            _lastSampleTime = sampleTime;
            return 0d;
        }

        var speed = Math.Abs((currentPosition - _lastPosition) / seconds);
        _lastPosition = currentPosition;
        _lastSampleTime = sampleTime;
        return Math.Round(speed, 3);
    }

    private static string FormatBool(bool value)
    {
        return value ? "是" : "否";
    }
}

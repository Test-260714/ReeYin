using Prism.Commands;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.ACS.App;
using ReeYin_V.Hardware.ControlCard.ACS.Views;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.ACS.ViewModels;

public sealed class AcsAxisControlContext : BindableBase
{
    public AcsAxisControlContext(
        En_AxisNum axis,
        SingleAxisParam? axisConfig,
        AcsAxisHomeBufferConfig? homeBuffer)
    {
        Axis = axis;
        AxisConfig = axisConfig;
        HomeBuffer = homeBuffer;
    }

    public En_AxisNum Axis { get; }

    public SingleAxisParam? AxisConfig { get; }

    public AcsAxisHomeBufferConfig? HomeBuffer { get; }
}

public sealed class AcsAxisSpeedParameterRow : BindableBase
{
    private readonly SingleAxisParam _axisConfig;
    private readonly SpeedSetting _speedSetting;

    public AcsAxisSpeedParameterRow(SingleAxisParam axisConfig, SpeedSetting speedSetting)
    {
        _axisConfig = axisConfig ?? throw new ArgumentNullException(nameof(axisConfig));
        _speedSetting = speedSetting ?? throw new ArgumentNullException(nameof(speedSetting));
    }

    public En_AxisNum Axis => _axisConfig.AxisNum;

    public short AxisNo
    {
        get => _axisConfig.AxisNo;
        set
        {
            _axisConfig.AxisNo = value;
            RaisePropertyChanged();
        }
    }

    public string AxisName
    {
        get => _axisConfig.NickName ?? string.Empty;
        set
        {
            _axisConfig.NickName = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public bool IsUsing
    {
        get => _axisConfig.IsUsing;
        set
        {
            _axisConfig.IsUsing = value;
            RaisePropertyChanged();
        }
    }

    public EN_SpeedType SpeedType
    {
        get => _speedSetting.SpeedType;
        set
        {
            _speedSetting.SpeedType = value;
            RaisePropertyChanged();
        }
    }

    public string SpeedDescribe
    {
        get => _speedSetting.SpeedDescribe ?? string.Empty;
        set
        {
            _speedSetting.SpeedDescribe = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public double StartSpeed
    {
        get => _speedSetting.StartSpeed;
        set
        {
            _speedSetting.StartSpeed = value;
            RaisePropertyChanged();
        }
    }

    public double MaxSpeed
    {
        get => _speedSetting.MaxSpeed;
        set
        {
            _speedSetting.MaxSpeed = value;
            RaisePropertyChanged();
        }
    }

    public double AccSpeed
    {
        get => _speedSetting.AccSpeed;
        set
        {
            _speedSetting.AccSpeed = value;
            RaisePropertyChanged();
        }
    }
}

public class AcsControlCardConfigViewModel : BindableBase, IDisposable
{
    private const int MaxSelectedTabIndex = 3;

    private AcsControlCard? _card;
    private IReadOnlyList<En_AxisNum> _axisOptions = new[] { En_AxisNum.X, En_AxisNum.Y, En_AxisNum.Z, En_AxisNum.R };
    private En_AxisNum _selectedAxis = En_AxisNum.X;
    private SingleAxisParam? _selectedAxisConfig;
    private AcsAxisHomeBufferConfig? _selectedHomeBuffer;
    private AcsAxisControlContext _selectedAxisContext = new(En_AxisNum.X, null, null);
    private int _selectedTabIndex;
    private string _statusText = "就绪。";
    private string _operationLog = string.Empty;
    private bool _lastOperationSucceeded = true;
    private int _selectedBufferNo = 1;
    private string _runLabel = string.Empty;
    private string _bufferScript = string.Empty;
    private string _bufferStatus = string.Empty;
    private string _bufferStateText = "状态: 未读取";
    private string _bufferCompiledText = "编译: 未读取";
    private bool _bufferIsRunning;
    private bool _bufferIsCompiled;
    private int _selectedIoIndex;
    private bool _selectedIoOutputValue;
    private bool _selectedIoIsInput = true;
    private string _inputStatusText = string.Empty;
    private string _outputStatusText = string.Empty;
    private string _ioStatusText = string.Empty;
    private double _relativeDistance = 1000d;
    private double _absoluteTarget;
    private float _jogStep = 100f;
    private EN_SpeedType _continuousMoveSpeedType = EN_SpeedType.Low;
    private bool _continuousMoveActive;
    private En_AxisNum _continuousMoveAxis = En_AxisNum.X;
    private MoveDirection _continuousMoveDirection = MoveDirection.正向;
    private double _resetPosition;
    private string _axisStatusText = string.Empty;
    private AcsAxisMotionProfile? _selectedMotionProfile;
    private readonly AcsLciFixedDistancePulseConfig _fallbackLciFixedDistancePulse = new();
    private readonly AcsLciSegmentCircleConfig _fallbackLciSegmentCircle = new();
    private string _lciFixedDistancePulseStatusText = string.Empty;
    private string _lciFixedDistancePulseScript = string.Empty;

    public AcsControlCardConfigViewModel()
    {
        InitializeCommand = new DelegateCommand(InitializeCard);
        CloseCommand = new DelegateCommand(CloseCard);
        RefreshStateCommand = new DelegateCommand(RefreshState);
        OpenBufferScriptCommand = new DelegateCommand(OpenBufferScript);
        OpenDBufferCommand = new DelegateCommand(OpenDBuffer);
        UploadBufferCommand = new DelegateCommand(UploadBuffer);
        DownloadBufferCommand = new DelegateCommand(DownloadBuffer);
        CompileBufferCommand = new DelegateCommand(CompileBuffer);
        RunBufferCommand = new DelegateCommand(RunBuffer);
        StopBufferCommand = new DelegateCommand(StopBuffer);
        ClearBufferCommand = new DelegateCommand(ClearBuffer);
        RefreshBufferStatusCommand = new DelegateCommand(RefreshBufferStatus);
        RefreshInputsCommand = new DelegateCommand(RefreshInputs);
        RefreshOutputsCommand = new DelegateCommand(RefreshOutputs);
        SetOutputCommand = new DelegateCommand(SetOutput);
        ReadSelectedIoCommand = new DelegateCommand(ReadSelectedIo);
        EnableAxisCommand = new DelegateCommand(() => SetAxisEnabled(true));
        DisableAxisCommand = new DelegateCommand(() => SetAxisEnabled(false));
        MoveRelativeCommand = new DelegateCommand(MoveRelative);
        MoveAbsoluteCommand = new DelegateCommand(MoveAbsolute);
        JogPositiveCommand = new DelegateCommand(() => JogAxis((MoveDirection)0));
        JogNegativeCommand = new DelegateCommand(() => JogAxis((MoveDirection)1));
        StartPositiveContinuousMoveCommand = new DelegateCommand(() => StartContinuousMove(MoveDirection.正向));
        StartNegativeContinuousMoveCommand = new DelegateCommand(() => StartContinuousMove(MoveDirection.反向));
        StopContinuousMoveCommand = new DelegateCommand(StopContinuousMove);
        StopAxisCommand = new DelegateCommand(StopAxis);
        ResetFeedbackCommand = new DelegateCommand(ResetFeedback);
        RefreshAxisStatusCommand = new DelegateCommand(RefreshAxisStatus);
        RefreshMotionProfilesCommand = new DelegateCommand(RefreshMotionProfiles);
        ApplySelectedMotionProfileCommand = new DelegateCommand(ApplySelectedMotionProfile);
        ApplyAllMotionProfilesCommand = new DelegateCommand(ApplyAllMotionProfiles);
        GoHomeCommand = new DelegateCommand(GoHome);
        RunLciFixedDistancePulseXsegCommand = new DelegateCommand(RunLciFixedDistancePulseXseg);
        RunLciSegmentCircleCommand = new DelegateCommand(RunLciSegmentCircle);

        AxisMotionProfiles = new ObservableCollection<AcsAxisMotionProfile>();
        AxisSpeedRows = new ObservableCollection<AcsAxisSpeedParameterRow>();
        SelectedAxisSpeedRows = new ObservableCollection<AcsAxisSpeedParameterRow>();
        Monitor = new AcsControlCardMonitorPanelViewModel();
    }

    public AcsControlCardConfigViewModel(AcsControlCard card)
        : this()
    {
        SetCard(card);
    }

    public AcsControlCard? Card
    {
        get => _card;
        private set
        {
            _card = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Options));
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(IsConnected));
            RaisePropertyChanged(nameof(ConnectionStateText));
        }
    }

    public AcsControlCardOptions? Options => Card?.Options;

    private AcsLciFixedDistancePulseConfig LciFixedDistancePulseConfig =>
        Options?.LciFixedDistancePulse ?? _fallbackLciFixedDistancePulse;

    private AcsLciSegmentCircleConfig LciSegmentCircleConfig =>
        Options?.LciSegmentCircle ?? _fallbackLciSegmentCircle;

    public string DisplayName => string.IsNullOrWhiteSpace(Card?.NickName)
        ? "ACS SPiiPlus"
        : Card!.NickName;

    public bool IsConnected => Card?.IsConnected == true;

    public string ConnectionStateText => IsConnected ? "已连接" : "未连接";

    public AcsControlCardMonitorPanelViewModel Monitor { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            _selectedTabIndex = Math.Clamp(value, 0, MaxSelectedTabIndex);
            RaisePropertyChanged();
        }
    }

    public IReadOnlyList<En_AxisNum> AxisOptions
    {
        get => _axisOptions;
        private set
        {
            _axisOptions = value;
            RaisePropertyChanged();
        }
    }

    public En_AxisNum SelectedAxis
    {
        get => _selectedAxis;
        set
        {
            if (_selectedAxis == value)
            {
                return;
            }

            _selectedAxis = value;
            RaisePropertyChanged();
            SyncSelectedAxisContext();
        }
    }

    public SingleAxisParam? SelectedAxisConfig
    {
        get => _selectedAxisConfig;
        private set
        {
            _selectedAxisConfig = value;
            RaisePropertyChanged();
        }
    }

    public AcsAxisHomeBufferConfig? SelectedHomeBuffer
    {
        get => _selectedHomeBuffer;
        private set
        {
            _selectedHomeBuffer = value;
            RaisePropertyChanged();
            RaiseSelectedHomeBufferProperties();
        }
    }

    public AcsAxisControlContext SelectedAxisContext
    {
        get => _selectedAxisContext;
        private set
        {
            _selectedAxisContext = value ?? new AcsAxisControlContext(SelectedAxis, SelectedAxisConfig, SelectedHomeBuffer);
            RaisePropertyChanged();
        }
    }

    public int SelectedAxisHomeBufferNo
    {
        get => SelectedHomeBuffer?.BufferNo ?? SelectedBufferNo;
        set
        {
            if (SelectedHomeBuffer != null)
            {
                SelectedHomeBuffer.BufferNo = value;
            }

            SelectedBufferNo = value;
            RaisePropertyChanged();
        }
    }

    public bool SelectedAxisHomeBufferEnabled
    {
        get => SelectedHomeBuffer?.IsEnabled == true;
        set
        {
            if (SelectedHomeBuffer == null)
            {
                return;
            }

            SelectedHomeBuffer.IsEnabled = value;
            RaisePropertyChanged();
        }
    }

    public int SelectedAxisHomeBufferTimeout
    {
        get => SelectedHomeBuffer?.Timeout ?? Options?.InternalTimeout ?? 30000;
        set
        {
            if (SelectedHomeBuffer == null)
            {
                return;
            }

            SelectedHomeBuffer.Timeout = value;
            RaisePropertyChanged();
        }
    }

    public bool SelectedAxisStopBeforeHomeBuffer
    {
        get => SelectedHomeBuffer?.StopAxisBeforeRun == true;
        set
        {
            if (SelectedHomeBuffer == null)
            {
                return;
            }

            SelectedHomeBuffer.StopAxisBeforeRun = value;
            RaisePropertyChanged();
        }
    }

    public bool SelectedAxisResetFeedbackAfterHome
    {
        get => SelectedHomeBuffer?.ResetFeedbackAfterSuccess == true;
        set
        {
            if (SelectedHomeBuffer == null)
            {
                return;
            }

            SelectedHomeBuffer.ResetFeedbackAfterSuccess = value;
            RaisePropertyChanged();
        }
    }

    public double SelectedAxisHomeResetPosition
    {
        get => SelectedHomeBuffer?.ResetPosition ?? 0d;
        set
        {
            if (SelectedHomeBuffer == null)
            {
                return;
            }

            SelectedHomeBuffer.ResetPosition = value;
            RaisePropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string OperationLog
    {
        get => _operationLog;
        private set
        {
            _operationLog = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public bool LastOperationSucceeded
    {
        get => _lastOperationSucceeded;
        private set
        {
            _lastOperationSucceeded = value;
            RaisePropertyChanged();
        }
    }

    public int SelectedBufferNo
    {
        get => _selectedBufferNo;
        set
        {
            var bufferNo = Math.Clamp(value, 0, 64);
            if (!BufferNoOptions.Contains(bufferNo))
            {
                BufferNoOptions.Add(bufferNo);
            }

            _selectedBufferNo = bufferNo;
            RaisePropertyChanged();
        }
    }

    public string RunLabel
    {
        get => _runLabel;
        set
        {
            _runLabel = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string BufferScript
    {
        get => _bufferScript;
        set
        {
            _bufferScript = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string BufferStatus
    {
        get => _bufferStatus;
        private set
        {
            _bufferStatus = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public IReadOnlyList<EN_SpeedType> SpeedTypeOptions { get; } = Enum.GetValues<EN_SpeedType>();

    public ObservableCollection<int> BufferNoOptions { get; } = new(Enumerable.Range(0, 10));

    public string BufferStateText
    {
        get => _bufferStateText;
        private set
        {
            _bufferStateText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string BufferCompiledText
    {
        get => _bufferCompiledText;
        private set
        {
            _bufferCompiledText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public bool BufferIsRunning
    {
        get => _bufferIsRunning;
        private set
        {
            _bufferIsRunning = value;
            RaisePropertyChanged();
        }
    }

    public bool BufferIsCompiled
    {
        get => _bufferIsCompiled;
        private set
        {
            _bufferIsCompiled = value;
            RaisePropertyChanged();
        }
    }

    public int SelectedIoIndex
    {
        get => _selectedIoIndex;
        set
        {
            _selectedIoIndex = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public bool SelectedIoOutputValue
    {
        get => _selectedIoOutputValue;
        set
        {
            _selectedIoOutputValue = value;
            RaisePropertyChanged();
        }
    }

    public bool SelectedIoIsInput
    {
        get => _selectedIoIsInput;
        set
        {
            _selectedIoIsInput = value;
            RaisePropertyChanged();
        }
    }

    public string InputStatusText
    {
        get => _inputStatusText;
        private set
        {
            _inputStatusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string OutputStatusText
    {
        get => _outputStatusText;
        private set
        {
            _outputStatusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string IoStatusText
    {
        get => _ioStatusText;
        private set
        {
            _ioStatusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public double RelativeDistance
    {
        get => _relativeDistance;
        set
        {
            _relativeDistance = value;
            RaisePropertyChanged();
        }
    }

    public double AbsoluteTarget
    {
        get => _absoluteTarget;
        set
        {
            _absoluteTarget = value;
            RaisePropertyChanged();
        }
    }

    public float JogStep
    {
        get => _jogStep;
        set
        {
            _jogStep = Math.Max(0, value);
            RaisePropertyChanged();
        }
    }

    public EN_SpeedType ContinuousMoveSpeedType
    {
        get => _continuousMoveSpeedType;
        set
        {
            _continuousMoveSpeedType = value;
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

    public string AxisStatusText
    {
        get => _axisStatusText;
        private set
        {
            _axisStatusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<AcsAxisMotionProfile> AxisMotionProfiles { get; }

    public ObservableCollection<AcsAxisSpeedParameterRow> AxisSpeedRows { get; }

    public ObservableCollection<AcsAxisSpeedParameterRow> SelectedAxisSpeedRows { get; }

    public AcsAxisMotionProfile? SelectedMotionProfile
    {
        get => _selectedMotionProfile;
        set
        {
            _selectedMotionProfile = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseBufferNo
    {
        get => LciFixedDistancePulseConfig.BufferNo;
        set
        {
            LciFixedDistancePulseConfig.BufferNo = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseAxisX
    {
        get => LciFixedDistancePulseConfig.AxisX;
        set
        {
            LciFixedDistancePulseConfig.AxisX = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseAxisY
    {
        get => LciFixedDistancePulseConfig.AxisY;
        set
        {
            LciFixedDistancePulseConfig.AxisY = value;
            RaisePropertyChanged();
        }
    }

    public double LciFixedDistancePulseWidth
    {
        get => LciFixedDistancePulseConfig.PulseWidth;
        set
        {
            LciFixedDistancePulseConfig.PulseWidth = value;
            RaisePropertyChanged();
        }
    }

    public double LciFixedDistancePulseInterval
    {
        get => LciFixedDistancePulseConfig.Interval;
        set
        {
            LciFixedDistancePulseConfig.Interval = value;
            RaisePropertyChanged();
        }
    }

    public double LciFixedDistancePulseStartDistance
    {
        get => LciFixedDistancePulseConfig.StartDistance;
        set
        {
            LciFixedDistancePulseConfig.StartDistance = value;
            RaisePropertyChanged();
        }
    }

    public double LciFixedDistancePulseEndDistance
    {
        get => LciFixedDistancePulseConfig.EndDistance;
        set
        {
            LciFixedDistancePulseConfig.EndDistance = value;
            RaisePropertyChanged();
        }
    }

    public bool LciFixedDistancePulseRouteConfigOutput
    {
        get => LciFixedDistancePulseConfig.RouteConfigOutput;
        set
        {
            LciFixedDistancePulseConfig.RouteConfigOutput = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseConfigOutputIndex
    {
        get => LciFixedDistancePulseConfig.ConfigOutputIndex;
        set
        {
            LciFixedDistancePulseConfig.ConfigOutputIndex = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseConfigOutputCode
    {
        get => LciFixedDistancePulseConfig.ConfigOutputCode;
        set
        {
            LciFixedDistancePulseConfig.ConfigOutputCode = value;
            RaisePropertyChanged();
        }
    }

    public int LciFixedDistancePulseTimeout
    {
        get => LciFixedDistancePulseConfig.Timeout;
        set
        {
            LciFixedDistancePulseConfig.Timeout = value;
            RaisePropertyChanged();
        }
    }

    public string LciFixedDistancePulsePointsText
    {
        get => LciFixedDistancePulseConfig.PointsText;
        set
        {
            LciFixedDistancePulseConfig.PointsText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string LciFixedDistancePulseStatusText
    {
        get => _lciFixedDistancePulseStatusText;
        private set
        {
            _lciFixedDistancePulseStatusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string LciFixedDistancePulseScript
    {
        get => _lciFixedDistancePulseScript;
        private set
        {
            _lciFixedDistancePulseScript = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public int LciSegmentCircleBufferNo
    {
        get => LciSegmentCircleConfig.BufferNo;
        set
        {
            LciSegmentCircleConfig.BufferNo = value;
            RaisePropertyChanged();
        }
    }

    public int LciSegmentCircleAxisX
    {
        get => LciSegmentCircleConfig.AxisX;
        set
        {
            LciSegmentCircleConfig.AxisX = value;
            RaisePropertyChanged();
        }
    }

    public int LciSegmentCircleAxisY
    {
        get => LciSegmentCircleConfig.AxisY;
        set
        {
            LciSegmentCircleConfig.AxisY = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleVelocity
    {
        get => LciSegmentCircleConfig.Velocity;
        set
        {
            LciSegmentCircleConfig.Velocity = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleStartX
    {
        get => LciSegmentCircleConfig.StartX;
        set
        {
            LciSegmentCircleConfig.StartX = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleStartY
    {
        get => LciSegmentCircleConfig.StartY;
        set
        {
            LciSegmentCircleConfig.StartY = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleCenterX
    {
        get => LciSegmentCircleConfig.CenterX;
        set
        {
            LciSegmentCircleConfig.CenterX = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleCenterY
    {
        get => LciSegmentCircleConfig.CenterY;
        set
        {
            LciSegmentCircleConfig.CenterY = value;
            RaisePropertyChanged();
        }
    }

    public double LciSegmentCircleRadius
    {
        get => LciSegmentCircleConfig.Radius;
        set
        {
            LciSegmentCircleConfig.Radius = value;
            RaisePropertyChanged();
        }
    }

    public int LciSegmentCircleGateActiveState
    {
        get => LciSegmentCircleConfig.GateActiveState;
        set
        {
            LciSegmentCircleConfig.GateActiveState = value;
            RaisePropertyChanged();
        }
    }

    public int LciSegmentCircleTimeout
    {
        get => LciSegmentCircleConfig.Timeout;
        set
        {
            LciSegmentCircleConfig.Timeout = value;
            RaisePropertyChanged();
        }
    }

    public DelegateCommand InitializeCommand { get; }

    public DelegateCommand ConnectCommand => InitializeCommand;

    public DelegateCommand CloseCommand { get; }

    public DelegateCommand RefreshStateCommand { get; }

    public DelegateCommand OpenBufferScriptCommand { get; }

    public DelegateCommand OpenDBufferCommand { get; }

    public DelegateCommand UploadBufferCommand { get; }

    public DelegateCommand DownloadBufferCommand { get; }

    public DelegateCommand CompileBufferCommand { get; }

    public DelegateCommand RunBufferCommand { get; }

    public DelegateCommand StopBufferCommand { get; }

    public DelegateCommand ClearBufferCommand { get; }

    public DelegateCommand RefreshBufferStatusCommand { get; }

    public DelegateCommand RefreshInputsCommand { get; }

    public DelegateCommand RefreshOutputsCommand { get; }

    public DelegateCommand SetOutputCommand { get; }

    public DelegateCommand ReadSelectedIoCommand { get; }

    public DelegateCommand EnableAxisCommand { get; }

    public DelegateCommand DisableAxisCommand { get; }

    public DelegateCommand MoveRelativeCommand { get; }

    public DelegateCommand MoveAbsoluteCommand { get; }

    public DelegateCommand JogPositiveCommand { get; }

    public DelegateCommand JogNegativeCommand { get; }

    public DelegateCommand StartPositiveContinuousMoveCommand { get; }

    public DelegateCommand StartNegativeContinuousMoveCommand { get; }

    public DelegateCommand StopContinuousMoveCommand { get; }

    public DelegateCommand StopAxisCommand { get; }

    public DelegateCommand ResetFeedbackCommand { get; }

    public DelegateCommand RefreshAxisStatusCommand { get; }

    public DelegateCommand RefreshMotionProfilesCommand { get; }

    public DelegateCommand ApplySelectedMotionProfileCommand { get; }

    public DelegateCommand ApplyAllMotionProfilesCommand { get; }

    public DelegateCommand GoHomeCommand { get; }

    public DelegateCommand RunLciFixedDistancePulseXsegCommand { get; }

    public DelegateCommand RunLciSegmentCircleCommand { get; }

    public bool SetCard(object? card)
    {
        if (card is not AcsControlCard acsControlCard)
        {
            SetStatus(false, "当前对象不是 ACS 控制卡。");
            return false;
        }

        acsControlCard.EnsureOptions();
        Card = acsControlCard;
        Monitor.SetCard(acsControlCard);
        RefreshAxisOptions();
        RefreshAxisSpeedRows();
        RefreshMotionProfiles();
        RefreshState();
        SyncSelectedAxisContext();
        RaiseLciTestConfigPropertiesChanged();
        return true;
    }

    private void RaiseLciTestConfigPropertiesChanged()
    {
        RaisePropertyChanged(nameof(LciFixedDistancePulseBufferNo));
        RaisePropertyChanged(nameof(LciFixedDistancePulseAxisX));
        RaisePropertyChanged(nameof(LciFixedDistancePulseAxisY));
        RaisePropertyChanged(nameof(LciFixedDistancePulseWidth));
        RaisePropertyChanged(nameof(LciFixedDistancePulseInterval));
        RaisePropertyChanged(nameof(LciFixedDistancePulseStartDistance));
        RaisePropertyChanged(nameof(LciFixedDistancePulseEndDistance));
        RaisePropertyChanged(nameof(LciFixedDistancePulseRouteConfigOutput));
        RaisePropertyChanged(nameof(LciFixedDistancePulseConfigOutputIndex));
        RaisePropertyChanged(nameof(LciFixedDistancePulseConfigOutputCode));
        RaisePropertyChanged(nameof(LciFixedDistancePulseTimeout));
        RaisePropertyChanged(nameof(LciFixedDistancePulsePointsText));
        RaisePropertyChanged(nameof(LciSegmentCircleBufferNo));
        RaisePropertyChanged(nameof(LciSegmentCircleAxisX));
        RaisePropertyChanged(nameof(LciSegmentCircleAxisY));
        RaisePropertyChanged(nameof(LciSegmentCircleVelocity));
        RaisePropertyChanged(nameof(LciSegmentCircleStartX));
        RaisePropertyChanged(nameof(LciSegmentCircleStartY));
        RaisePropertyChanged(nameof(LciSegmentCircleCenterX));
        RaisePropertyChanged(nameof(LciSegmentCircleCenterY));
        RaisePropertyChanged(nameof(LciSegmentCircleRadius));
        RaisePropertyChanged(nameof(LciSegmentCircleGateActiveState));
        RaisePropertyChanged(nameof(LciSegmentCircleTimeout));
    }

    public void Dispose()
    {
        Monitor.Dispose();
    }

    private void InitializeCard()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        try
        {
            var ok = card.Initialized || card.Init();
            SetStatus(ok, ok ? "ACS 控制卡初始化成功。" : "ACS 控制卡初始化失败。");
        }
        catch (Exception ex)
        {
            SetStatus(false, $"ACS 控制卡初始化失败: {ex.Message}");
        }
    }

    private void CloseCard()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        try
        {
            card.Close();
            SetStatus(true, "ACS 控制卡已关闭。");
        }
        catch (Exception ex)
        {
            SetStatus(false, $"ACS 控制卡关闭失败: {ex.Message}");
        }
    }

    private void RefreshState()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        RaisePropertyChanged(nameof(IsConnected));
        SetStatus(true, $"状态={card.State}; 已连接={FormatBool(card.IsConnected)}; 已初始化={FormatBool(card.Initialized)}; 连接方式={card.ConnectionMode}");
    }

    private void OpenBufferScript()
    {
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);

        var dialog = new AcsBufferScriptView
        {
            Owner = owner,
            DataContext = this
        };

        dialog.ShowDialog();
    }

    private void OpenDBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (!card.TryGetDBufferNo(out var dBufferNo, out var lookupMessage))
        {
            BufferStatus = lookupMessage;
            SetStatus(false, lookupMessage);
            return;
        }

        SelectedBufferNo = dBufferNo;
        var ok = card.TryUploadProgramBuffer(SelectedBufferNo, out var script, out var uploadMessage);
        if (ok)
        {
            BufferScript = script;
        }

        SetBufferOperationStatus(card, ok, $"{lookupMessage}{Environment.NewLine}{uploadMessage}");
    }

    private void UploadBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryUploadProgramBuffer(SelectedBufferNo, out var script, out var message);
        if (ok)
        {
            BufferScript = script;
        }

        SetBufferOperationStatus(card, ok, message);
    }

    private void DownloadBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryLoadProgramBuffer(SelectedBufferNo, BufferScript, out var message);
        SetBufferOperationStatus(card, ok, message);
    }

    private void CompileBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryCompileProgramBuffer(SelectedBufferNo, out var message);
        SetBufferOperationStatus(card, ok, message);
    }

    private void RunBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryRunProgramBuffer(SelectedBufferNo, RunLabel, out var message);
        SetBufferOperationStatus(card, ok, message);
    }

    private void StopBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryStopProgramBuffer(SelectedBufferNo, out var message);
        SetBufferOperationStatus(card, ok, message);
    }

    private void ClearBuffer()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryClearProgramBuffer(SelectedBufferNo, out var message);
        if (ok)
        {
            BufferScript = string.Empty;
        }

        SetBufferOperationStatus(card, ok, message);
    }

    private void RefreshBufferStatus()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        BufferStatus = RefreshBufferStatusSnapshot(card);
        SetStatus(true, BufferStatus);
    }

    private void RefreshInputs()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.GetAllInput(out var values);
        InputStatusText = FormatBits(values);
        SetIoStatus(ok, ok ? "输入状态已刷新。" : "刷新输入失败。");
    }

    private void RefreshOutputs()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.GetAllOutput(out var values);
        OutputStatusText = FormatBits(values);
        SetIoStatus(ok, ok ? "输出状态已刷新。" : "刷新输出失败。");
    }

    private void SetOutput()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.SetSpecifiedIO(SelectedIoIndex, SelectedIoOutputValue);
        SetIoStatus(ok, ok ? $"DO{SelectedIoIndex}已设置为{(SelectedIoOutputValue ? "ON" : "OFF")}。" : $"设置 DO{SelectedIoIndex} 失败。");
        if (ok)
        {
            RefreshOutputs();
        }
    }

    private void ReadSelectedIo()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.GetSpecifiedIO(SelectedIoIsInput, SelectedIoIndex, out var value);
        SetIoStatus(ok, ok ? $"{(SelectedIoIsInput ? "DI" : "DO")}{SelectedIoIndex}={FormatOnOff(value)}" : "读取选中 IO 失败。");
    }

    private void SetAxisEnabled(bool enabled)
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TrySetAxisEnabled(SelectedAxis, enabled, out var message);
        SetAxisStatus(ok, message);
        if (ok)
        {
            RefreshAxisStatus();
        }
    }

    private void MoveRelative()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (!IsFinite(RelativeDistance))
        {
            SetAxisStatus(false, "相对距离无效。");
            return;
        }

        var direction = RelativeDistance < 0 ? (MoveDirection)1 : (MoveDirection)0;
        var ok = card.Move(SelectedAxis, direction, Math.Abs(RelativeDistance), out var message);
        SetAxisStatus(ok, ok
            ? $"轴 {SelectedAxis} 已相对移动 {RelativeDistance:F3}。"
            : $"轴 {SelectedAxis} 相对移动失败: {message}");
    }

    private void MoveAbsolute()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (!IsFinite(AbsoluteTarget))
        {
            SetAxisStatus(false, "绝对目标位置无效。");
            return;
        }

        var ok = card.MoveAbsoluteAxis(SelectedAxis, AbsoluteTarget, true);
        SetAxisStatus(ok, ok ? $"轴 {SelectedAxis} 已移动到 {AbsoluteTarget:F3}。" : $"轴 {SelectedAxis} 绝对移动失败。");
    }

    private void JogAxis(MoveDirection direction)
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (JogStep <= 0)
        {
            SetAxisStatus(false, "点动步长必须大于0。");
            return;
        }

        var ok = card.JogAxis(SelectedAxis, direction, JogStep);
        SetAxisStatus(ok, ok ? $"轴 {SelectedAxis} 点动 {FormatDirection(direction)}，步长={JogStep}。" : $"轴 {SelectedAxis} 点动失败。");
    }

    private void StartContinuousMove(MoveDirection direction)
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (_continuousMoveActive)
        {
            StopContinuousMove();
        }

        if (!card.ValidateJogLimitCondition(SelectedAxis, direction, out var validationMessage))
        {
            SetAxisStatus(false, validationMessage);
            return;
        }

        try
        {
            var ok = card.JogAxis(SelectedAxis, direction, ContinuousMoveSpeedType, true);
            if (ok)
            {
                _continuousMoveActive = true;
                _continuousMoveAxis = SelectedAxis;
                _continuousMoveDirection = direction;
            }

            SetAxisStatus(ok, ok
                ? $"轴 {SelectedAxis} 开始{FormatDirection(direction)}连续移动，松开按钮停止。"
                : $"轴 {SelectedAxis} {FormatDirection(direction)}连续移动启动失败。");
        }
        catch (Exception ex)
        {
            SetAxisStatus(false, $"轴 {SelectedAxis} {FormatDirection(direction)}连续移动启动失败: {ex.Message}");
        }
    }

    private void StopContinuousMove()
    {
        if (!_continuousMoveActive)
        {
            return;
        }

        if (!TryGetCard(out var card))
        {
            _continuousMoveActive = false;
            return;
        }

        try
        {
            var axis = _continuousMoveAxis;
            var ok = card.JogAxis(axis, _continuousMoveDirection, ContinuousMoveSpeedType, false);
            if (ok)
            {
                _continuousMoveActive = false;
            }

            SetAxisStatus(ok, ok ? $"轴 {axis} 连续移动已停止。" : $"轴 {axis} 连续移动停止失败。");
        }
        catch (Exception ex)
        {
            SetAxisStatus(false, $"轴 {_continuousMoveAxis} 连续移动停止失败: {ex.Message}");
        }
    }

    private void StopAxis()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        try
        {
            card.Stop(SelectedAxis);
            SetAxisStatus(true, $"轴 {SelectedAxis} 已请求停止。");
        }
        catch (Exception ex)
        {
            SetAxisStatus(false, $"轴 {SelectedAxis} 停止失败: {ex.Message}");
        }
    }

    private void ResetFeedback()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.ResetFeedbackPosition(SelectedAxis, ResetPosition);
        SetAxisStatus(ok, ok ? $"轴 {SelectedAxis} 反馈位置已设置为 {ResetPosition:F3}。" : $"轴 {SelectedAxis} 设置反馈失败。");
    }

    private void RefreshAxisStatus()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.TryGetAxisTestSnapshot(SelectedAxis, out var snapshot, out var message);
        AxisStatusText = ok ? snapshot.Summary : message;
        SetAxisStatus(ok, AxisStatusText);
    }

    private void RefreshMotionProfiles()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var previousAxis = SelectedMotionProfile?.Axis;
        AxisMotionProfiles.Clear();

        foreach (var axis in AxisOptions)
        {
            if (card.TryGetAxisMotionProfile(axis, out var profile, out var message))
            {
                profile.Message = message;
            }
            else
            {
                profile = new AcsAxisMotionProfile
                {
                    Axis = axis,
                    Message = message
                };
            }

            AxisMotionProfiles.Add(profile);
        }

        SelectedMotionProfile = AxisMotionProfiles.FirstOrDefault(item => item.Axis.Equals(previousAxis))
                                ?? AxisMotionProfiles.FirstOrDefault();
    }

    private void ApplySelectedMotionProfile()
    {
        if (SelectedMotionProfile == null)
        {
            SetAxisStatus(false, "请先选择需要下发的轴运动参数。");
            return;
        }

        ApplyMotionProfile(SelectedMotionProfile, refreshAfterApply: true);
    }

    private void ApplyAllMotionProfiles()
    {
        if (AxisMotionProfiles.Count == 0)
        {
            SetAxisStatus(false, "没有可下发的轴运动参数。");
            return;
        }

        var successCount = 0;
        var failedMessages = new List<string>();
        foreach (var profile in AxisMotionProfiles)
        {
            if (ApplyMotionProfile(profile, refreshAfterApply: false))
            {
                successCount++;
                continue;
            }

            failedMessages.Add($"{profile.Axis}:{profile.Message}");
        }

        if (failedMessages.Count == 0)
        {
            RefreshMotionProfiles();
            SetAxisStatus(true, $"已下发 {successCount} 个轴的运动参数。");
            return;
        }

        SetAxisStatus(false, $"运动参数下发完成，成功 {successCount} 个，失败 {failedMessages.Count} 个：{string.Join("；", failedMessages.Take(3))}");
    }

    private bool ApplyMotionProfile(AcsAxisMotionProfile profile, bool refreshAfterApply)
    {
        if (!ValidateMotionProfile(profile, out var validationMessage))
        {
            profile.Message = validationMessage;
            SetAxisStatus(false, validationMessage);
            return false;
        }

        if (!TryGetCard(out var card))
        {
            return false;
        }

        var ok = card.TrySetAxisMotionProfile(profile, out var message);
        SetAxisStatus(ok, message);
        if (ok && refreshAfterApply)
        {
            RefreshMotionProfiles();
        }

        return ok;
    }

    private void GoHome()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var ok = card.GoHome(out var message);
        SetAxisStatus(ok, message);
    }

    private void RunLciFixedDistancePulseXseg()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        if (!TryParseLciFixedDistancePulsePoints(LciFixedDistancePulsePointsText, out var points, out var validationMessage))
        {
            LciFixedDistancePulseScript = string.Empty;
            LciFixedDistancePulseStatusText = validationMessage;
            SetStatus(false, validationMessage);
            return;
        }

        var param = new AcsLciFixedDistancePulseXsegParam
        {
            BufferNo = LciFixedDistancePulseBufferNo,
            AxisX = LciFixedDistancePulseAxisX,
            AxisY = LciFixedDistancePulseAxisY,
            PulseWidth = LciFixedDistancePulseWidth,
            Interval = LciFixedDistancePulseInterval,
            StartDistance = LciFixedDistancePulseStartDistance,
            EndDistance = LciFixedDistancePulseEndDistance,
            MotionProfile = LciFixedDistancePulseConfig.ToMotionProfile(),
            RouteConfigOutput = LciFixedDistancePulseRouteConfigOutput,
            ConfigOutputIndex = LciFixedDistancePulseConfigOutputIndex,
            ConfigOutputCode = LciFixedDistancePulseConfigOutputCode,
            Timeout = LciFixedDistancePulseTimeout,
            Points = points
        };

        var ok = card.TryRunLciFixedDistancePulseXseg(param, out var result, out var message);
        LciFixedDistancePulseScript = result.Script;
        LciFixedDistancePulseStatusText = ok
            ? $"{message}{Environment.NewLine}Channel={result.Channel}, PulseCount={result.PulseCount}"
            : message;
        SetStatus(ok, LciFixedDistancePulseStatusText);
    }

    private void RunLciSegmentCircle()
    {
        if (!TryGetCard(out var card))
        {
            return;
        }

        var param = new AcsLciSegmentCircleParam
        {
            BufferNo = LciSegmentCircleBufferNo,
            AxisX = LciSegmentCircleAxisX,
            AxisY = LciSegmentCircleAxisY,
            MotionProfile = LciSegmentCircleConfig.ToMotionProfile(),
            Velocity = LciSegmentCircleVelocity,
            StartX = LciSegmentCircleStartX,
            StartY = LciSegmentCircleStartY,
            CenterX = LciSegmentCircleCenterX,
            CenterY = LciSegmentCircleCenterY,
            Radius = LciSegmentCircleRadius,
            GateActiveState = LciSegmentCircleGateActiveState,
            Timeout = LciSegmentCircleTimeout
        };

        var ok = card.TryRunLciSegmentCircle(param, out var result, out var message);
        LciFixedDistancePulseScript = result.Script;
        LciFixedDistancePulseStatusText = ok
            ? $"{message}{Environment.NewLine}Channel={result.Channel}"
            : message;
        SetStatus(ok, LciFixedDistancePulseStatusText);
    }

    private static bool TryParseLciFixedDistancePulsePoints(
        string pointsText,
        out List<AcsPoint2D> points,
        out string message)
    {
        points = new List<AcsPoint2D>();
        var lines = (pointsText ?? string.Empty).Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                message = $"LCI 点位第 {index + 1} 行格式错误，请输入 X,Y。";
                return false;
            }

            if (!TryParseDouble(parts[0], out var x) || !TryParseDouble(parts[1], out var y))
            {
                message = $"LCI 点位第 {index + 1} 行不是有效数字。";
                return false;
            }

            points.Add(new AcsPoint2D(x, y));
        }

        if (points.Count < 2)
        {
            message = "LCI 固定距离脉冲至少需要两个 XSEG 点位。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool TryParseDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    private void RefreshAxisOptions()
    {
        var axes = Card?.Config?.AllAxis?
            .Where(axis => axis != null)
            .Select(axis => axis.AxisNum)
            .Distinct()
            .ToList();

        if (axes == null || axes.Count == 0)
        {
            axes = new List<En_AxisNum> { En_AxisNum.X, En_AxisNum.Y, En_AxisNum.Z, En_AxisNum.R };
        }

        AxisOptions = axes;
        if (!AxisOptions.Contains(SelectedAxis))
        {
            SelectedAxis = AxisOptions[0];
        }

    }

    private void RefreshAxisSpeedRows()
    {
        AxisSpeedRows.Clear();

        var axes = Card?.Config?.AllAxis?
            .Where(axis => axis != null)
            .ToList();

        if (axes == null || axes.Count == 0)
        {
            return;
        }

        foreach (var axis in axes)
        {
            EnsureSpeedSettings(axis);
            foreach (var speed in axis.SpeedDict1.Where(speed => speed != null))
            {
                AxisSpeedRows.Add(new AcsAxisSpeedParameterRow(axis, speed));
            }
        }
    }

    private void RefreshSelectedAxisSpeedRows()
    {
        SelectedAxisSpeedRows.Clear();

        if (SelectedAxisConfig == null)
        {
            return;
        }

        EnsureSpeedSettings(SelectedAxisConfig);
        foreach (var speed in SelectedAxisConfig.SpeedDict1.Where(speed => speed != null))
        {
            SelectedAxisSpeedRows.Add(new AcsAxisSpeedParameterRow(SelectedAxisConfig, speed));
        }
    }

    private static void EnsureSpeedSettings(SingleAxisParam axis)
    {
        axis.EnsureDefaultSpeedSettings();
    }

    private void SyncSelectedAxisContext()
    {
        SelectedAxisConfig = Card?.Config?.AllAxis?
            .FirstOrDefault(axis => axis != null && axis.AxisNum == SelectedAxis);

        SelectedHomeBuffer = Options?.HomeBuffers?
            .FirstOrDefault(buffer => buffer != null && buffer.Axis == SelectedAxis);

        if (SelectedHomeBuffer == null && Options != null)
        {
            Options.EnsureHomeBuffers(new[] { SelectedAxis });
            SelectedHomeBuffer = Options.HomeBuffers
                .FirstOrDefault(buffer => buffer != null && buffer.Axis == SelectedAxis);
        }

        if (SelectedHomeBuffer != null)
        {
            SelectedBufferNo = SelectedHomeBuffer.BufferNo;
        }

        SelectedAxisContext = new AcsAxisControlContext(SelectedAxis, SelectedAxisConfig, SelectedHomeBuffer);
        RefreshSelectedAxisSpeedRows();
        RaisePropertyChanged(nameof(SelectedAxisSpeedRows));
    }

    private void RaiseSelectedHomeBufferProperties()
    {
        RaisePropertyChanged(nameof(SelectedAxisHomeBufferNo));
        RaisePropertyChanged(nameof(SelectedAxisHomeBufferEnabled));
        RaisePropertyChanged(nameof(SelectedAxisHomeBufferTimeout));
        RaisePropertyChanged(nameof(SelectedAxisStopBeforeHomeBuffer));
        RaisePropertyChanged(nameof(SelectedAxisResetFeedbackAfterHome));
        RaisePropertyChanged(nameof(SelectedAxisHomeResetPosition));
    }

    private bool TryGetCard(out AcsControlCard card)
    {
        if (Card == null)
        {
            card = null!;
            SetStatus(false, "ACS 控制卡未绑定。");
            return false;
        }

        card = Card;
        return true;
    }

    private void SetStatus(bool success, string message)
    {
        LastOperationSucceeded = success;
        StatusText = $"[{DateTime.Now:HH:mm:ss}] {(success ? "成功" : "失败")}: {message}";
        OperationLog = $"{StatusText}{Environment.NewLine}{OperationLog}";
        RaisePropertyChanged(nameof(IsConnected));
        RaisePropertyChanged(nameof(ConnectionStateText));
    }

    private void SetBufferOperationStatus(AcsControlCard card, bool success, string message)
    {
        var snapshot = RefreshBufferStatusSnapshot(card);
        BufferStatus = $"{message}{Environment.NewLine}{snapshot}";
        SetStatus(success, BufferStatus);
    }

    private string RefreshBufferStatusSnapshot(AcsControlCard card)
    {
        if (card.TryGetProgramBufferStatus(SelectedBufferNo, out var status, out var message))
        {
            BufferIsRunning = status.IsRunning;
            BufferIsCompiled = status.IsCompiled;
            BufferStateText = status.IsRunning ? "状态: 运行中" : $"状态: {status.StateText}";
            BufferCompiledText = status.IsCompiled ? "编译: 已编译" : "编译: 未编译";
            return status.Summary;
        }

        BufferIsRunning = false;
        BufferIsCompiled = false;
        BufferStateText = $"状态: {message}";
        BufferCompiledText = "编译: 未知";
        return message;
    }

    private void SetIoStatus(bool success, string message)
    {
        IoStatusText = message;
        SetStatus(success, message);
    }

    private void SetAxisStatus(bool success, string message)
    {
        AxisStatusText = message;
        SetStatus(success, message);
    }


    private static string FormatBits(IReadOnlyList<bool> values)
    {
        if (values == null || values.Count == 0)
        {
            return "（无数据）";
        }

        return string.Join(Environment.NewLine, values.Select((value, index) => $"{index:D2}: {(value ? "ON" : "OFF")}"));
    }

    private static string FormatDirection(MoveDirection direction)
    {
        return (int)direction == 0 ? "正向" : "反向";
    }

    private static string FormatBool(bool value)
    {
        return value ? "是" : "否";
    }

    private static string FormatOnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool ValidateMotionProfile(AcsAxisMotionProfile profile, out string message)
    {
        if (!IsFinite(profile.Velocity)
            || !IsFinite(profile.Acceleration)
            || !IsFinite(profile.Deceleration)
            || !IsFinite(profile.KillDeceleration)
            || !IsFinite(profile.Jerk))
        {
            message = $"轴 {profile.Axis} 运动参数必须为有限数。";
            return false;
        }

        if (profile.Velocity < 0d
            || profile.Acceleration < 0d
            || profile.Deceleration < 0d
            || profile.KillDeceleration < 0d
            || profile.Jerk < 0d)
        {
            message = $"轴 {profile.Axis} 运动参数不能为负数。";
            return false;
        }

        message = string.Empty;
        return true;
    }
}

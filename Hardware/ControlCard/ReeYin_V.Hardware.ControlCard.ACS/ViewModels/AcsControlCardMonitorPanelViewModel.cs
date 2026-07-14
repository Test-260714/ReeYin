using Prism.Commands;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.ACS.App;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace ReeYin_V.Hardware.ControlCard.ACS.ViewModels;

public sealed class AcsControlCardMonitorPanelViewModel : BindableBase, IDisposable
{
    private readonly DispatcherTimer _refreshTimer;
    private AcsControlCard? _card;
    private string _statusText = "未绑定 ACS 控制卡。";
    private string _lastRefreshText = "-";
    private string _ioStatusText = "未刷新。";
    private bool _isAutoRefreshEnabled;
    private int _refreshIntervalMs = 500;
    private int _axisCount;
    private int _enabledAxisCount;
    private int _movingAxisCount;
    private int _alarmAxisCount;
    private int _inputCount;
    private int _inputOnCount;
    private int _outputCount;
    private int _outputOnCount;

    public AcsControlCardMonitorPanelViewModel()
    {
        AxisRows = new ObservableCollection<AcsAxisMonitorRow>();
        RefreshMonitorCommand = new DelegateCommand(RefreshMonitor);
        StartAutoRefreshCommand = new DelegateCommand(StartAutoRefresh);
        StopAutoRefreshCommand = new DelegateCommand(StopAutoRefresh);

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_refreshIntervalMs)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    public AcsControlCardMonitorPanelViewModel(AcsControlCard card)
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
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(ConnectionStateText));
            RaisePropertyChanged(nameof(ControllerStateText));
            RaisePropertyChanged(nameof(ControllerDetailText));
        }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Card?.NickName)
        ? "ACS SPiiPlus"
        : Card!.NickName;

    public string ConnectionStateText => Card == null
        ? "未绑定"
        : Card.IsConnected ? "已连接" : "未连接";

    public string ControllerStateText => Card?.State.ToString() ?? "未知";

    public string ControllerDetailText => Card == null
        ? "未绑定控制卡"
        : $"初始化={FormatBool(Card.Initialized)}；连接方式={Card.ConnectionMode}";

    public ObservableCollection<AcsAxisMonitorRow> AxisRows { get; }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value ?? string.Empty;
            RaisePropertyChanged();
        }
    }

    public string LastRefreshText
    {
        get => _lastRefreshText;
        private set
        {
            _lastRefreshText = value ?? "-";
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

    public bool IsAutoRefreshEnabled
    {
        get => _isAutoRefreshEnabled;
        set
        {
            if (_isAutoRefreshEnabled == value)
            {
                return;
            }

            _isAutoRefreshEnabled = value;
            RaisePropertyChanged();
            ApplyAutoRefreshState();
        }
    }

    public int RefreshIntervalMs
    {
        get => _refreshIntervalMs;
        set
        {
            var clamped = Math.Clamp(value, 100, 10000);
            if (_refreshIntervalMs == clamped)
            {
                return;
            }

            _refreshIntervalMs = clamped;
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(_refreshIntervalMs);
            RaisePropertyChanged();
        }
    }

    public int AxisCount
    {
        get => _axisCount;
        private set
        {
            _axisCount = value;
            RaisePropertyChanged();
        }
    }

    public int EnabledAxisCount
    {
        get => _enabledAxisCount;
        private set
        {
            _enabledAxisCount = value;
            RaisePropertyChanged();
        }
    }

    public int MovingAxisCount
    {
        get => _movingAxisCount;
        private set
        {
            _movingAxisCount = value;
            RaisePropertyChanged();
        }
    }

    public int AlarmAxisCount
    {
        get => _alarmAxisCount;
        private set
        {
            _alarmAxisCount = value;
            RaisePropertyChanged();
        }
    }

    public int InputCount
    {
        get => _inputCount;
        private set
        {
            _inputCount = value;
            RaisePropertyChanged();
        }
    }

    public int InputOnCount
    {
        get => _inputOnCount;
        private set
        {
            _inputOnCount = value;
            RaisePropertyChanged();
        }
    }

    public int OutputCount
    {
        get => _outputCount;
        private set
        {
            _outputCount = value;
            RaisePropertyChanged();
        }
    }

    public int OutputOnCount
    {
        get => _outputOnCount;
        private set
        {
            _outputOnCount = value;
            RaisePropertyChanged();
        }
    }

    public DelegateCommand RefreshMonitorCommand { get; }

    public DelegateCommand StartAutoRefreshCommand { get; }

    public DelegateCommand StopAutoRefreshCommand { get; }

    public bool SetCard(object? card)
    {
        if (card is not AcsControlCard acsControlCard)
        {
            StatusText = "当前对象不是 ACS 控制卡。";
            return false;
        }

        acsControlCard.EnsureOptions();
        Card = acsControlCard;
        SyncAxisRows();
        RefreshMonitor();
        return true;
    }

    public void RefreshMonitor()
    {
        var sampleTime = DateTime.Now;
        if (Card == null)
        {
            AxisRows.Clear();
            UpdateAxisCounters();
            StatusText = $"[{sampleTime:HH:mm:ss}] ACS 控制卡未绑定。";
            LastRefreshText = sampleTime.ToString("HH:mm:ss");
            return;
        }

        SyncAxisRows();
        RaiseControllerProperties();

        var failedAxes = new List<string>();
        foreach (var row in AxisRows)
        {
            if (Card.TryGetAxisTestSnapshot(row.Axis, out var snapshot, out var message))
            {
                row.ApplySnapshot(snapshot, sampleTime, message);
            }
            else
            {
                row.ApplyError(message, sampleTime);
                failedAxes.Add($"{row.Axis}:{message}");
            }
        }

        RefreshIoStatus(Card);
        UpdateAxisCounters();
        LastRefreshText = sampleTime.ToString("HH:mm:ss");
        StatusText = FormatRefreshStatus(sampleTime, failedAxes);
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshTimer_Tick;
    }

    private void StartAutoRefresh()
    {
        IsAutoRefreshEnabled = true;
    }

    private void StopAutoRefresh()
    {
        IsAutoRefreshEnabled = false;
    }

    private void ApplyAutoRefreshState()
    {
        if (IsAutoRefreshEnabled)
        {
            _refreshTimer.Start();
            RefreshMonitor();
            return;
        }

        _refreshTimer.Stop();
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        RefreshMonitor();
    }

    private void SyncAxisRows()
    {
        var axes = GetTargetAxes();
        var existingRows = AxisRows.ToDictionary(row => row.Axis);

        for (var i = AxisRows.Count - 1; i >= 0; i--)
        {
            if (!axes.Contains(AxisRows[i].Axis))
            {
                AxisRows.RemoveAt(i);
            }
        }

        for (var index = 0; index < axes.Count; index++)
        {
            var axis = axes[index];
            if (!existingRows.TryGetValue(axis, out var row))
            {
                AxisRows.Insert(Math.Min(index, AxisRows.Count), new AcsAxisMonitorRow(axis));
                continue;
            }

            var currentIndex = AxisRows.IndexOf(row);
            if (currentIndex >= 0 && currentIndex != index)
            {
                AxisRows.Move(currentIndex, index);
            }
        }
    }

    private List<En_AxisNum> GetTargetAxes()
    {
        var axes = Card?.Config?.AllAxis?
            .Where(axis => axis != null)
            .Select(axis => axis.AxisNum)
            .Distinct()
            .ToList();

        if (axes is { Count: > 0 })
        {
            return axes;
        }

        return new List<En_AxisNum>
        {
            En_AxisNum.X,
            En_AxisNum.Y,
            En_AxisNum.Z,
            En_AxisNum.R
        };
    }

    private void RefreshIoStatus(AcsControlCard card)
    {
        var inputOk = card.GetAllInput(out var inputs);
        var outputOk = card.GetAllOutput(out var outputs);

        InputCount = inputs?.Length ?? card.Options.DigitalInputCount;
        InputOnCount = inputs?.Count(value => value) ?? 0;
        OutputCount = outputs?.Length ?? card.Options.DigitalOutputCount;
        OutputOnCount = outputs?.Count(value => value) ?? 0;

        IoStatusText = inputOk || outputOk
            ? $"DI：{InputOnCount}/{InputCount} ON；DO：{OutputOnCount}/{OutputCount} ON"
            : "IO 状态未读取（控制卡未连接或读取失败）。";
    }

    private void UpdateAxisCounters()
    {
        AxisCount = AxisRows.Count;
        EnabledAxisCount = AxisRows.Count(row => row.IsOnline && row.IsEnabled);
        MovingAxisCount = AxisRows.Count(row => row.IsOnline && row.IsMoving);
        AlarmAxisCount = AxisRows.Count(row => row.HasAlarm);
    }

    private void RaiseControllerProperties()
    {
        RaisePropertyChanged(nameof(ConnectionStateText));
        RaisePropertyChanged(nameof(ControllerStateText));
        RaisePropertyChanged(nameof(ControllerDetailText));
    }

    private static string FormatRefreshStatus(DateTime sampleTime, IReadOnlyList<string> failedAxes)
    {
        if (failedAxes.Count == 0)
        {
            return $"[{sampleTime:HH:mm:ss}] 状态刷新完成。";
        }

        var preview = string.Join("；", failedAxes.Take(3));
        return $"[{sampleTime:HH:mm:ss}] 刷新完成，{failedAxes.Count} 个轴读取失败：{preview}";
    }

    private static string FormatBool(bool value)
    {
        return value ? "是" : "否";
    }
}

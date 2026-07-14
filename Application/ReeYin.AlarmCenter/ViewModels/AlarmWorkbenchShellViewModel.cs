using Prism.Commands;
using Prism.Navigation.Regions;
using ReeYin.AlarmCenter.Models;
using ReeYin.AlarmCenter.Views;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DisplayAlarmTrendPoint = ReeYin.AlarmCenter.Models.AlarmTrendPoint;
using DelegateCommand = Prism.Commands.DelegateCommand;

namespace ReeYin.AlarmCenter.ViewModels
{
    // 中文文案请直接使用 UTF-8 文本，避免写成 \uXXXX 转义。
    public class AlarmWorkbenchShellViewModel : DialogViewModelBase, INavigationAware
    {
        private const int HistoryPageSize = 18;
        private const int RealtimeFeedCapacity = 24;
        private const int ActiveAlarmMaxCount = 200;
        private const string ContentRegionName = "AlarmWorkbenchContentRegion";
        private const string ActiveAlarmViewPending = "Pending";
        private const string ActiveAlarmViewAcknowledged = "Acknowledged";
        private const string ActiveAlarmViewAll = "All";
        private const int StatisticsBarTabIndex = 0;
        private const int StatisticsTrendTabIndex = 1;
        private const int StatisticsDistributionTabIndex = 2;

        private readonly IAlarmService _alarmService;
        private readonly IRegionManager _regionManager;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private readonly DispatcherTimer _clockTimer;
        private readonly List<AlarmActiveRecord> _latestActiveAlarmRecords = new List<AlarmActiveRecord>();
        private IRegionManager? _contentRegionManager;
        private CancellationTokenSource? _deferredRefreshCts;
        private AlarmActiveItem? _selectedActiveAlarm;
        private string _selectedActiveAlarmView = ActiveAlarmViewPending;
        private AlarmWorkbenchPageItem? _selectedPage;
        private DateTime? _filterStartDate = DateTime.Today.AddDays(-6);
        private DateTime? _filterEndDate = DateTime.Today;
        private string _filterStartTimeText = "00:00:00";
        private string _filterEndTimeText = "23:59:59";
        private DateTime? _barFilterStartDate = DateTime.Today;
        private string _barFilterStartTimeText = "00:00:00";
        private DateTime? _trendFilterStartDate = DateTime.Today.AddDays(-6);
        private DateTime? _trendFilterEndDate = DateTime.Today;
        private bool _isBarStatisticsEmpty = true;
        private bool _isTypePieStatisticsEmpty = true;
        private string _selectedHistoryLevel = string.Empty;
        private string _selectedHistorySource = string.Empty;
        private string _historyKeyword = string.Empty;
        private bool _isRealtimePaused;
        private string _statusText = "正在等待报警数据...";
        private string _statisticsSummaryText = "设置起止时间后点击查询，统计图将按当前条件刷新。";
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalHistoryCount;
        private int _pendingActiveCount;
        private int _acknowledgedActiveCount;
        private int _allActiveCount;
        private int _pendingFeedCount;
        private bool _isBusy;
        private bool _isInitialized;
        private bool _isSubscribed;
        private bool _isContentRegionAttached;
        private bool _hasNavigatedInitialPage;
        private bool _hasLoadedStatistics;
        private bool _isStatisticsCacheInvalidated = true;
        private int _selectedStatisticsTabIndex;
        private string _lastExportFilePath = string.Empty;
        private string _lastNavigatedViewName = string.Empty;
        private bool _isAlarmDefinitionsPopupOpen;
        private string _alarmDemoLastActionText = "选择一个场景触发 Demo 报警。";
        private int _alarmDemoSequence;
#if DEBUG
        private AlarmDemoWindow? _alarmDemoWindow;
#endif
        public AlarmWorkbenchShellViewModel(IRegionManager regionManager)
        {
            Title = "报警中心";
            _alarmService = PrismProvider.AlarmService;
            _regionManager = regionManager;
            Title = "报警中心";

            SummaryCards = new ObservableCollection<AlarmSummaryCard>();
            StatisticCards = new ObservableCollection<AlarmSummaryCard>();
            ActiveAlarms = new ObservableCollection<AlarmActiveItem>();
            ActiveAlarmViewOptions = new ObservableCollection<AlarmOptionItem>
            {
                new AlarmOptionItem { Label = "待处理", Value = ActiveAlarmViewPending },
                new AlarmOptionItem { Label = "监视中", Value = ActiveAlarmViewAcknowledged },
                new AlarmOptionItem { Label = "全部活动", Value = ActiveAlarmViewAll }
            };
            RealtimeFeed = new ObservableCollection<AlarmFeedItem>();
            HistoryAlarms = new ObservableCollection<AlarmHistoryItem>();
            AlarmDemoScenarios = new ObservableCollection<AlarmDemoScenarioItem>(AlarmDemoScenarioFactory.CreateDefaults());
            TypeStatistics = new ObservableCollection<AlarmStatisticBarItem>();
            SourceStatistics = new ObservableCollection<AlarmStatisticBarItem>();
            BarTrendPoints = new ObservableCollection<DisplayAlarmTrendPoint>();
            TrendPoints = new ObservableCollection<DisplayAlarmTrendPoint>();
            TypePieSlices = new ObservableCollection<AlarmPieSliceItem>();
            LevelOptions = new ObservableCollection<AlarmOptionItem>();
            SourceOptions = new ObservableCollection<AlarmOptionItem>();
            PageItems = new ObservableCollection<AlarmWorkbenchPageItem>
            {
                new AlarmWorkbenchPageItem
                {
                    Header = "实时报警",
                    NavigationCode = "01",
                    ViewName = nameof(AlarmRealtimeView),
                    Description = "查看当前活动报警、报警详情与实时事件流。"
                },
                new AlarmWorkbenchPageItem
                {
                    Header = "历史记录",
                    NavigationCode = "02",
                    ViewName = nameof(AlarmHistoryView),
                    Description = "按时间、等级、来源和关键字查询报警闭环记录。"
                },
                new AlarmWorkbenchPageItem
                {
                    Header = "统计分析",
                    NavigationCode = "03",
                    ViewName = nameof(AlarmStatisticsView),
                    Description = "查看报警趋势、类型分布和设备来源分布。"
                }
            };
            StatusText = "正在等待报警数据...";

            ToggleRealtimeCommand = new DelegateCommand(async () => await ToggleRealtimeAsync());
            ConfirmSelectedCommand = new DelegateCommand(async () => await ConfirmSelectedAsync(), CanConfirmSelected);
            ClearSelectedCommand = new DelegateCommand(async () => await ClearSelectedAsync(), CanClearSelected);
            ApplyHistoryFilterCommand = new DelegateCommand(async () => await ApplyHistoryFilterAsync(), CanRunCommand);
            ResetHistoryFilterCommand = new DelegateCommand(async () => await ResetHistoryFilterAsync(), CanRunCommand);
            ShiftTimeRangeBackwardCommand = new DelegateCommand(async () => await ShiftTimeRangeAsync(-1), CanRunCommand);
            ShiftTimeRangeForwardCommand = new DelegateCommand(async () => await ShiftTimeRangeAsync(1), CanRunCommand);
            ApplyBarFilterCommand = new DelegateCommand(async () => await ApplyBarFilterAsync(), CanRunCommand);
            ShiftBarTimeRangeBackwardCommand = new DelegateCommand(async () => await ShiftBarTimeRangeAsync(-1), CanRunCommand);
            ShiftBarTimeRangeForwardCommand = new DelegateCommand(async () => await ShiftBarTimeRangeAsync(1), CanRunCommand);
            ApplyTrendFilterCommand = new DelegateCommand(async () => await ApplyTrendFilterAsync(), CanRunCommand);
            ShiftTrendTimeRangeBackwardCommand = new DelegateCommand(async () => await ShiftTrendTimeRangeAsync(-1), CanRunCommand);
            ShiftTrendTimeRangeForwardCommand = new DelegateCommand(async () => await ShiftTrendTimeRangeAsync(1), CanRunCommand);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanPreviousPage);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanNextPage);
            ExportCsvCommand = new DelegateCommand(async () => await ExportAsync(AlarmExportFormat.Csv), CanRunCommand);
            ExportExcelCommand = new DelegateCommand(async () => await ExportAsync(AlarmExportFormat.ExcelXml), CanRunCommand);
            OpenLastExportLocationCommand = new DelegateCommand(OpenLastExportLocation, CanOpenLastExportLocation);
            OpenAlarmDefinitionsPopupCommand = new DelegateCommand(OpenAlarmDefinitionsPopup, CanRunCommand);
            CloseAlarmDefinitionsPopupCommand = new DelegateCommand(CloseAlarmDefinitionsPopup);
            OpenAlarmDemoPopupCommand = new DelegateCommand(OpenAlarmDemoPopup, CanRunAlarmDemoCommand);
            RaiseAlarmDemoScenarioCommand = new Prism.Commands.DelegateCommand<AlarmDemoScenarioItem>(async scenario => await RaiseAlarmDemoScenarioAsync(scenario), CanRaiseAlarmDemoScenario);
            RaiseAllAlarmDemoScenariosCommand = new DelegateCommand(async () => await RaiseAllAlarmDemoScenariosAsync(), CanRunAlarmDemoCommand);
            RaiseRepeatedAlarmDemoCommand = new DelegateCommand(async () => await RaiseRepeatedAlarmDemoAsync(), CanRunAlarmDemoCommand);
            ClearAlarmDemoCommand = new DelegateCommand(async () => await ClearAlarmDemoAsync(), CanRunAlarmDemoCommand);
            NavigatePageCommand = new Prism.Commands.DelegateCommand<AlarmWorkbenchPageItem>(NavigatePage);

            InitializeOptions();
            SelectedPage = PageItems.FirstOrDefault();
            ApplyDashboard(new AlarmDashboardSnapshot());

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (_, __) =>
            {
                foreach (AlarmActiveItem item in ActiveAlarms)
                {
                    item.RefreshClock();
                }
            };
        }

        public ObservableCollection<AlarmSummaryCard> SummaryCards { get; }

        public ObservableCollection<AlarmSummaryCard> StatisticCards { get; }

        public ObservableCollection<AlarmActiveItem> ActiveAlarms { get; }

        public ObservableCollection<AlarmOptionItem> ActiveAlarmViewOptions { get; }

        public ObservableCollection<AlarmFeedItem> RealtimeFeed { get; }

        public ObservableCollection<AlarmHistoryItem> HistoryAlarms { get; }

        public ObservableCollection<AlarmDemoScenarioItem> AlarmDemoScenarios { get; }

        public ObservableCollection<AlarmStatisticBarItem> TypeStatistics { get; }

        public ObservableCollection<AlarmStatisticBarItem> SourceStatistics { get; }

        public ObservableCollection<DisplayAlarmTrendPoint> BarTrendPoints { get; }

        public ObservableCollection<DisplayAlarmTrendPoint> TrendPoints { get; }

        public ObservableCollection<AlarmPieSliceItem> TypePieSlices { get; }

        public ObservableCollection<AlarmOptionItem> LevelOptions { get; }

        public ObservableCollection<AlarmOptionItem> SourceOptions { get; }

        public ObservableCollection<AlarmWorkbenchPageItem> PageItems { get; }

        public DelegateCommand ToggleRealtimeCommand { get; }

        public DelegateCommand ConfirmSelectedCommand { get; }

        public DelegateCommand ClearSelectedCommand { get; }

        public DelegateCommand ApplyHistoryFilterCommand { get; }

        public DelegateCommand ResetHistoryFilterCommand { get; }

        public DelegateCommand ShiftTimeRangeBackwardCommand { get; }

        public DelegateCommand ShiftTimeRangeForwardCommand { get; }

        public DelegateCommand ApplyBarFilterCommand { get; }

        public DelegateCommand ShiftBarTimeRangeBackwardCommand { get; }

        public DelegateCommand ShiftBarTimeRangeForwardCommand { get; }

        public DelegateCommand ApplyTrendFilterCommand { get; }

        public DelegateCommand ShiftTrendTimeRangeBackwardCommand { get; }

        public DelegateCommand ShiftTrendTimeRangeForwardCommand { get; }

        public DelegateCommand PreviousPageCommand { get; }

        public DelegateCommand NextPageCommand { get; }

        public DelegateCommand ExportCsvCommand { get; }

        public DelegateCommand ExportExcelCommand { get; }

        public DelegateCommand OpenLastExportLocationCommand { get; }

        public DelegateCommand OpenAlarmDefinitionsPopupCommand { get; }

        public DelegateCommand CloseAlarmDefinitionsPopupCommand { get; }

        public DelegateCommand OpenAlarmDemoPopupCommand { get; }

        public Prism.Commands.DelegateCommand<AlarmDemoScenarioItem> RaiseAlarmDemoScenarioCommand { get; }

        public DelegateCommand RaiseAllAlarmDemoScenariosCommand { get; }

        public DelegateCommand RaiseRepeatedAlarmDemoCommand { get; }

        public DelegateCommand ClearAlarmDemoCommand { get; }

        public Prism.Commands.DelegateCommand<AlarmWorkbenchPageItem> NavigatePageCommand { get; }

        public bool IsAlarmDemoAvailable
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public string AlarmDemoLastActionText
        {
            get => _alarmDemoLastActionText;
            set => SetProperty(ref _alarmDemoLastActionText, value ?? string.Empty);
        }

        public bool IsAlarmDefinitionsPopupOpen
        {
            get => _isAlarmDefinitionsPopupOpen;
            set => SetProperty(ref _isAlarmDefinitionsPopupOpen, value);
        }

        public bool CanEditAlarmDefinitions => IsDesignRelatedUser();

        public AlarmWorkbenchPageItem? SelectedPage
        {
            get => _selectedPage;
            private set
            {
                if (SetProperty(ref _selectedPage, value))
                {
                    foreach (AlarmWorkbenchPageItem item in PageItems)
                    {
                        item.IsSelected = ReferenceEquals(item, value);
                    }
                }
            }
        }

        public AlarmActiveItem? SelectedActiveAlarm
        {
            get => _selectedActiveAlarm;
            set
            {
                if (SetProperty(ref _selectedActiveAlarm, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string SelectedActiveAlarmView
        {
            get => _selectedActiveAlarmView;
            set
            {
                string normalized = NormalizeActiveAlarmView(value);
                if (SetProperty(ref _selectedActiveAlarmView, normalized))
                {
                    RefreshActiveAlarmView();
                    RaiseActiveAlarmViewTextChanged();
                }
            }
        }

        public int PendingActiveCount
        {
            get => _pendingActiveCount;
            private set
            {
                if (SetProperty(ref _pendingActiveCount, value))
                {
                    RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
                }
            }
        }

        public int AcknowledgedActiveCount
        {
            get => _acknowledgedActiveCount;
            private set
            {
                if (SetProperty(ref _acknowledgedActiveCount, value))
                {
                    RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
                }
            }
        }

        public int AllActiveCount
        {
            get => _allActiveCount;
            private set
            {
                if (SetProperty(ref _allActiveCount, value))
                {
                    RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
                }
            }
        }

        public string ActiveAlarmListTitle => SelectedActiveAlarmView switch
        {
            ActiveAlarmViewAcknowledged => "监视中",
            ActiveAlarmViewAll => "全部活动报警",
            _ => "待处理报警"
        };

        public string ActiveAlarmListDescription => SelectedActiveAlarmView switch
        {
            ActiveAlarmViewAcknowledged => "这些报警已被确认当前提示，系统继续监视报警源；如果需要确认的报警再次触发，会自动回到“待处理”。",
            ActiveAlarmViewAll => "显示全部仍处于活动生命周期的报警，包含待处理、监视中和免确认报警。",
            _ => "默认只显示需要操作员确认且尚未确认的报警；确认后进入“监视中”，再次触发会自动回到这里。"
        };

        public string ActiveAlarmViewCountText => SelectedActiveAlarmView switch
        {
            ActiveAlarmViewAcknowledged => $"监视中 {AcknowledgedActiveCount} / 活动 {AllActiveCount}",
            ActiveAlarmViewAll => $"全部活动 {AllActiveCount}",
            _ => $"待处理 {PendingActiveCount} / 活动 {AllActiveCount}"
        };

        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set => SetProperty(ref _filterStartDate, value);
        }

        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set => SetProperty(ref _filterEndDate, value);
        }

        public string FilterStartTimeText
        {
            get => _filterStartTimeText;
            set => SetProperty(ref _filterStartTimeText, value ?? string.Empty);
        }

        public string FilterEndTimeText
        {
            get => _filterEndTimeText;
            set => SetProperty(ref _filterEndTimeText, value ?? string.Empty);
        }

        public DateTime? BarFilterStartDate
        {
            get => _barFilterStartDate;
            set => SetProperty(ref _barFilterStartDate, value);
        }

        public string BarFilterStartTimeText
        {
            get => _barFilterStartTimeText;
            set => SetProperty(ref _barFilterStartTimeText, value ?? string.Empty);
        }

        public DateTime? TrendFilterStartDate
        {
            get => _trendFilterStartDate;
            set => SetProperty(ref _trendFilterStartDate, value);
        }

        public DateTime? TrendFilterEndDate
        {
            get => _trendFilterEndDate;
            set => SetProperty(ref _trendFilterEndDate, value);
        }

        public string SelectedHistoryLevel
        {
            get => _selectedHistoryLevel;
            set => SetProperty(ref _selectedHistoryLevel, value ?? string.Empty);
        }

        public string SelectedHistorySource
        {
            get => _selectedHistorySource;
            set => SetProperty(ref _selectedHistorySource, value ?? string.Empty);
        }

        public string HistoryKeyword
        {
            get => _historyKeyword;
            set => SetProperty(ref _historyKeyword, value ?? string.Empty);
        }

        public bool IsRealtimePaused
        {
            get => _isRealtimePaused;
            set
            {
                if (SetProperty(ref _isRealtimePaused, value))
                {
                    RaisePropertyChanged(nameof(RealtimeToggleText));
                    RaisePropertyChanged(nameof(FeedStateText));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value ?? string.Empty);
        }

        public string LastExportFilePath
        {
            get => _lastExportFilePath;
            private set
            {
                if (SetProperty(ref _lastExportFilePath, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(HasExportedFileLocation));
                    OpenLastExportLocationCommand.RaiseCanExecuteChanged();
            OpenAlarmDefinitionsPopupCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(CanEditAlarmDefinitions));
                }
            }
        }

        public bool HasExportedFileLocation => !string.IsNullOrWhiteSpace(LastExportFilePath);

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, Math.Max(1, value)))
                {
                    RaisePropertyChanged(nameof(HistoryPagerText));
                    RaiseCommandStates();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, Math.Max(1, value)))
                {
                    RaisePropertyChanged(nameof(HistoryPagerText));
                    RaiseCommandStates();
                }
            }
        }

        public int TotalHistoryCount
        {
            get => _totalHistoryCount;
            set
            {
                if (SetProperty(ref _totalHistoryCount, Math.Max(0, value)))
                {
                    RaisePropertyChanged(nameof(HistoryResultText));
                    RaiseCommandStates();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string RealtimeToggleText => IsRealtimePaused ? "恢复刷新" : "暂停刷新";

        public string FeedStateText => IsRealtimePaused
            ? $"实时播报已暂停，待同步事件 {PendingFeedCount} 条，列表仍仅保留最近 {RealtimeFeedCapacity} 条。"
            : $"实时播报仅保留最近 {RealtimeFeedCapacity} 条事件，高频报警会自动滚动保留最新记录。";

        public string HistoryPagerText => $"第 {CurrentPage} / {TotalPages} 页";

        public string HistoryResultText => $"共 {TotalHistoryCount} 条历史记录";

        public string StatisticsSummaryText
        {
            get => _statisticsSummaryText;
            private set => SetProperty(ref _statisticsSummaryText, value ?? string.Empty);
        }

        public bool IsBarStatisticsEmpty
        {
            get => _isBarStatisticsEmpty;
            private set => SetProperty(ref _isBarStatisticsEmpty, value);
        }

        public bool IsTypePieStatisticsEmpty
        {
            get => _isTypePieStatisticsEmpty;
            private set => SetProperty(ref _isTypePieStatisticsEmpty, value);
        }

        public int SelectedStatisticsTabIndex
        {
            get => _selectedStatisticsTabIndex;
            set
            {
                int normalized = Math.Max(StatisticsBarTabIndex, Math.Min(StatisticsDistributionTabIndex, value));
                SetProperty(ref _selectedStatisticsTabIndex, normalized);
            }
        }

        private int PendingFeedCount
        {
            get => _pendingFeedCount;
            set
            {
                if (SetProperty(ref _pendingFeedCount, Math.Max(0, value)))
                {
                    RaisePropertyChanged(nameof(FeedStateText));
                }
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = EnsureInitializedAsync();
            EnsureInitialContentNavigation();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (_isSubscribed)
            {
                _alarmService.DataChanged -= OnAlarmDataChanged;
                _isSubscribed = false;
            }

            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();
            _deferredRefreshCts = null;

            if (_clockTimer.IsEnabled)
            {
                _clockTimer.Stop();
            }

        }

        public void NavigateToSelectedPage()
        {
            EnsureInitialContentNavigation();
        }

        public void AttachContentRegionManager(IRegionManager regionManager)
        {
            _contentRegionManager = regionManager;
            _isContentRegionAttached = true;
            EnsureInitialContentNavigation();
        }

        public void EnsureInitialContentNavigation()
        {
            if (_hasNavigatedInitialPage)
            {
                return;
            }

            if (TryNavigateContentRegion(SelectedPage ?? PageItems.FirstOrDefault()))
            {
                _hasNavigatedInitialPage = true;
            }
        }

        private void OpenAlarmDefinitionsPopup()
        {
            if (!CanEditAlarmDefinitions)
            {
                HandyControl.Controls.MessageBox.Show(
                    "\u53ea\u6709\u8bbe\u8ba1\u6216\u5de5\u7a0b\u76f8\u5173\u4eba\u5458\u624d\u80fd\u4fee\u6539\u62a5\u8b66\u5b9a\u4e49\u3002",
                    "\u6743\u9650\u4e0d\u8db3",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsAlarmDefinitionsPopupOpen = true;
            StatusText = "\u5df2\u6253\u5f00\u62a5\u8b66\u5b9a\u4e49\u914d\u7f6e\u7a97\u53e3\u3002";
        }

        private void CloseAlarmDefinitionsPopup()
        {
            IsAlarmDefinitionsPopupOpen = false;
            StatusText = "\u5df2\u5173\u95ed\u62a5\u8b66\u5b9a\u4e49\u914d\u7f6e\u7a97\u53e3\u3002";
        }

        private void OpenAlarmDemoPopup()
        {
            if (!IsAlarmDemoAvailable)
            {
                return;
            }

#if DEBUG
            RunOnUiThread(() =>
            {
                if (_alarmDemoWindow != null)
                {
                    if (_alarmDemoWindow.WindowState == WindowState.Minimized)
                    {
                        _alarmDemoWindow.WindowState = WindowState.Normal;
                    }

                    _alarmDemoWindow.Activate();
                    return;
                }

                AlarmDemoWindow window = new AlarmDemoWindow
                {
                    DataContext = this
                };

                System.Windows.Window? owner = Application.Current?.MainWindow;
                if (owner != null && owner.IsLoaded)
                {
                    window.Owner = owner;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowInTaskbar = false;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                window.Closed += (_, __) => _alarmDemoWindow = null;
                _alarmDemoWindow = window;
                AlarmDemoLastActionText = "选择一个场景触发 Demo 报警。";
                StatusText = "已打开报警测试窗口。";
                window.Show();
            });
#endif
        }

#if DEBUG
        private void CloseAlarmDemoWindow()
        {
            AlarmDemoWindow? window = _alarmDemoWindow;
            if (window == null)
            {
                return;
            }

            _alarmDemoWindow = null;
            window.Close();
        }
#endif

        private async Task RaiseAlarmDemoScenarioAsync(AlarmDemoScenarioItem? scenario)
        {
            if (!CanRaiseAlarmDemoScenario(scenario) || scenario == null)
            {
                return;
            }

            try
            {
                await EnsureAlarmDemoServiceReadyAsync().ConfigureAwait(false);
                AlarmInfo alarm = _alarmService.AddAlarm(scenario.CreateRaiseRequest(NextAlarmDemoSequence()));
                string message = $"已触发 Demo 报警：{scenario.DisplayName} / {alarm.Code}";
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = message;
                    StatusText = message;
                });
                await RefreshForPageAsync(SelectedPage, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = $"触发 Demo 报警失败：{ex.Message}";
                    StatusText = AlarmDemoLastActionText;
                });
            }
        }

        private async Task RaiseAllAlarmDemoScenariosAsync()
        {
            if (!CanRunAlarmDemoCommand())
            {
                return;
            }

            try
            {
                await EnsureAlarmDemoServiceReadyAsync().ConfigureAwait(false);
                foreach (AlarmDemoScenarioItem scenario in AlarmDemoScenarios)
                {
                    _alarmService.AddAlarm(scenario.CreateRaiseRequest(NextAlarmDemoSequence()));
                }

                string message = $"已生成 {AlarmDemoScenarios.Count} 条 Demo 报警。";
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = message;
                    StatusText = message;
                });
                await RefreshForPageAsync(SelectedPage, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = $"生成 Demo 报警失败：{ex.Message}";
                    StatusText = AlarmDemoLastActionText;
                });
            }
        }

        private async Task RaiseRepeatedAlarmDemoAsync()
        {
            if (!CanRunAlarmDemoCommand())
            {
                return;
            }

            try
            {
                await EnsureAlarmDemoServiceReadyAsync().ConfigureAwait(false);
                AlarmDemoScenarioItem scenario = AlarmDemoScenarioFactory.CreateRepeatScenario();
                _alarmService.AddAlarm(scenario.CreateRaiseRequest(NextAlarmDemoSequence()));
                AlarmInfo alarm = _alarmService.AddAlarm(scenario.CreateRaiseRequest(NextAlarmDemoSequence()));
                string message = $"已重复触发 Demo 报警：{alarm.Code}，当前次数 {alarm.OccurrenceCount}。";
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = message;
                    StatusText = message;
                });
                await RefreshForPageAsync(SelectedPage, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = $"重复触发 Demo 报警失败：{ex.Message}";
                    StatusText = AlarmDemoLastActionText;
                });
            }
        }

        private async Task ClearAlarmDemoAsync()
        {
            if (!CanRunAlarmDemoCommand())
            {
                return;
            }

            try
            {
                await EnsureAlarmDemoServiceReadyAsync().ConfigureAwait(false);
                int clearedCount = 0;
                foreach (string code in AlarmDemoScenarioFactory.GetDemoCodes())
                {
                    if (_alarmService.ClearAlarm(
                        code,
                        AlarmDemoScenarioFactory.DemoSource,
                        ResolveCurrentUser(),
                        "AlarmCenter Demo cleanup.",
                        AlarmDemoScenarioFactory.DemoLocation))
                    {
                        clearedCount++;
                    }
                }

                string message = $"已清除 {clearedCount} 类 Demo 活动报警。";
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = message;
                    StatusText = message;
                });
                await RefreshForPageAsync(SelectedPage, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    AlarmDemoLastActionText = $"清除 Demo 报警失败：{ex.Message}";
                    StatusText = AlarmDemoLastActionText;
                });
            }
        }

        private async Task EnsureAlarmDemoServiceReadyAsync()
        {
            if (_isInitialized)
            {
                EnsureLiveTracking();
                return;
            }

            await EnsureInitializedAsync().ConfigureAwait(false);
        }

        private int NextAlarmDemoSequence()
        {
            return Interlocked.Increment(ref _alarmDemoSequence);
        }

        private void NavigatePage(AlarmWorkbenchPageItem? page)
        {
            if (page == null)
            {
                return;
            }

            SelectedPage = page;
            TryNavigateContentRegion(page);
            _ = RefreshForPageAsync(page, string.Empty);
        }

        private static bool IsStatisticsPage(AlarmWorkbenchPageItem? page)
        {
            return string.Equals(page?.ViewName, nameof(AlarmStatisticsView), StringComparison.OrdinalIgnoreCase);
        }

        private bool CanReuseStatisticsNavigation(string successMessage)
        {
            return _hasLoadedStatistics &&
                !_isStatisticsCacheInvalidated &&
                string.IsNullOrWhiteSpace(successMessage);
        }

        private bool TryNavigateContentRegion(AlarmWorkbenchPageItem? page)
        {
            if (page == null)
            {
                return false;
            }

            IRegionManager regionManager = _isContentRegionAttached && _contentRegionManager != null
                ? _contentRegionManager
                : _regionManager;
            if (!regionManager.Regions.ContainsRegionWithName(ContentRegionName))
            {
                return false;
            }

            if (string.Equals(_lastNavigatedViewName, page.ViewName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            _lastNavigatedViewName = page.ViewName;
            regionManager.RequestNavigate(ContentRegionName, page.ViewName);
            return true;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
            {
                EnsureLiveTracking();
                await RefreshForPageAsync(SelectedPage, "报警数据已刷新。").ConfigureAwait(false);
                return;
            }

            await _initializeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                RunOnUiThread(() => StatusText = "正在初始化报警工作台...");
                await _alarmService.InitializeAsync().ConfigureAwait(false);
                EnsureLiveTracking();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"报警工作台初始化失败：{ex.Message}");
            }
            finally
            {
                _initializeLock.Release();
            }

            if (_isInitialized)
            {
                await RefreshForPageAsync(SelectedPage, "报警工作台已就绪。").ConfigureAwait(false);
            }
        }

        private void EnsureLiveTracking()
        {
            if (!_isSubscribed)
            {
                _alarmService.DataChanged += OnAlarmDataChanged;
                _isSubscribed = true;
            }

            RunOnUiThread(() =>
            {
                if (!_clockTimer.IsEnabled)
                {
                    _clockTimer.Start();
                }
            });
        }

        private async Task ToggleRealtimeAsync()
        {
            IsRealtimePaused = !IsRealtimePaused;
            if (IsRealtimePaused)
            {
                StatusText = "实时播报已暂停，列表会保持当前状态。";
                return;
            }

            await RefreshRealtimeFeedAsync("实时播报已恢复。").ConfigureAwait(false);
        }

        private async Task ConfirmSelectedAsync()
        {
            if (!CanConfirmSelected() || SelectedActiveAlarm == null)
            {
                return;
            }

            AlarmActiveItem selected = SelectedActiveAlarm;

            await _alarmService.AcknowledgeAsync(
                selected.ActiveId,
                ResolveCurrentUser(),
                "报警工作台确认").ConfigureAwait(false);

            RunOnUiThread(() => StatusText = $"报警 {selected.Code} 已确认。");
            ScheduleDeferredRefresh();
        }

        private async Task ClearSelectedAsync()
        {
            if (!CanClearSelected() || SelectedActiveAlarm == null)
            {
                return;
            }

            AlarmActiveItem selected = SelectedActiveAlarm;

            AlarmOperationResult result = await _alarmService.ClearByIdAsync(
                selected.ActiveId,
                ResolveCurrentUser(),
                "Alarm workbench manual clear.",
                AlarmClearOrigin.Manual).ConfigureAwait(false);

            RunOnUiThread(() => StatusText = result.Success
                ? $"报警 {selected.Code} 已清除；若源头仍异常将再次触发。"
                : $"报警 {selected.Code} 清除失败：{result.Message}");
            if (result.Success)
            {
                ScheduleDeferredRefresh();
            }
        }

        private async Task ApplyHistoryFilterAsync()
        {
            CurrentPage = 1;
            await RefreshHistoryScopeAsync("历史查询条件已应用。").ConfigureAwait(false);
        }

        private async Task ResetHistoryFilterAsync()
        {
            FilterStartDate = DateTime.Today.AddDays(-6);
            FilterEndDate = DateTime.Today;
            FilterStartTimeText = "00:00:00";
            FilterEndTimeText = "23:59:59";
            BarFilterStartDate = DateTime.Today;
            BarFilterStartTimeText = "00:00:00";
            TrendFilterStartDate = DateTime.Today.AddDays(-6);
            TrendFilterEndDate = DateTime.Today;
            SelectedHistoryLevel = string.Empty;
            SelectedHistorySource = string.Empty;
            HistoryKeyword = string.Empty;
            CurrentPage = 1;
            await RefreshHistoryScopeAsync("历史查询条件已重置。").ConfigureAwait(false);
        }

        private async Task ApplyBarFilterAsync()
        {
            await RefreshBarStatisticsAsync("柱状分类开始时间已应用。").ConfigureAwait(false);
        }

        private async Task ApplyTrendFilterAsync()
        {
            await RefreshTrendStatisticsAsync("报警趋势时间范围已应用。").ConfigureAwait(false);
        }

        private async Task ShiftTimeRangeAsync(int direction)
        {
            (DateTime? startValue, DateTime? endValue) = BuildFilterDateTimeRange(useDefaultDates: true);
            DateTime start = startValue ?? DateTime.Today.AddDays(-1);
            DateTime end = endValue ?? DateTime.Today.AddHours(12);
            TimeSpan span = end - start;
            if (span <= TimeSpan.Zero)
            {
                span = TimeSpan.FromHours(12);
            }

            DateTime shiftedStart = start.AddTicks(span.Ticks * Math.Sign(direction));
            DateTime shiftedEnd = end.AddTicks(span.Ticks * Math.Sign(direction));
            (shiftedStart, shiftedEnd) = ClampShiftedDateTimeRangeToNow(shiftedStart, shiftedEnd, span);
            SetFilterDateTimeRange(shiftedStart, shiftedEnd);

            CurrentPage = 1;
            await RefreshHistoryScopeAsync(direction < 0 ? "已切换到上一时间段。" : "已切换到下一时间段。").ConfigureAwait(false);
        }

        private async Task ShiftBarTimeRangeAsync(int direction)
        {
            (DateTime? startValue, _) = BuildBarFilterDateTimeRange(useDefaultDates: true);
            DateTime start = startValue ?? DateTime.Today;
            TimeSpan span = TimeSpan.FromHours(1);
            DateTime shiftedStart = start.AddTicks(span.Ticks * Math.Sign(direction));
            DateTime maxStart = GetCurrentHourStart(DateTime.Now);
            if (shiftedStart > maxStart)
            {
                shiftedStart = maxStart;
            }

            SetBarFilterDateTimeRange(shiftedStart);

            await RefreshBarStatisticsAsync(direction < 0 ? "柱状分类已切换到上一小时。" : "柱状分类已切换到下一小时。").ConfigureAwait(false);
        }

        private async Task ShiftTrendTimeRangeAsync(int direction)
        {
            (DateTime? startValue, DateTime? endValue) = BuildTrendFilterDateTimeRange(useDefaultDates: true);
            DateTime start = startValue ?? DateTime.Today.AddDays(-1);
            DateTime end = endValue ?? DateTime.Today.AddHours(12);
            TimeSpan span = end - start;
            if (span <= TimeSpan.Zero)
            {
                span = TimeSpan.FromHours(12);
            }

            DateTime shiftedStart = start.AddTicks(span.Ticks * Math.Sign(direction));
            DateTime shiftedEnd = end.AddTicks(span.Ticks * Math.Sign(direction));
            (shiftedStart, shiftedEnd) = ClampShiftedDateTimeRangeToNow(shiftedStart, shiftedEnd, span);
            SetTrendFilterDateTimeRange(shiftedStart, shiftedEnd);

            await RefreshTrendStatisticsAsync(direction < 0 ? "报警趋势已切换到上一时间段。" : "报警趋势已切换到下一时间段。").ConfigureAwait(false);
        }

        private async Task PreviousPageAsync()
        {
            if (!CanPreviousPage())
            {
                return;
            }

            CurrentPage--;
            await RefreshHistoryScopeAsync("已切换到上一页。").ConfigureAwait(false);
        }

        private async Task NextPageAsync()
        {
            if (!CanNextPage())
            {
                return;
            }

            CurrentPage++;
            await RefreshHistoryScopeAsync("已切换到下一页。").ConfigureAwait(false);
        }

        private async Task ExportAsync(AlarmExportFormat format)
        {
            await _alarmService.InitializeAsync().ConfigureAwait(false);
            string basePath = string.IsNullOrWhiteSpace(PrismProvider.AppBasePath)
                ? AppContext.BaseDirectory
                : PrismProvider.AppBasePath;
            string outputDirectory = Path.Combine(basePath, "Export", "Alarm");

            try
            {
                RunOnUiThread(() =>
                {
                    IsBusy = true;
                    LastExportFilePath = string.Empty;
                });
                string path = await _alarmService.ExportHistoryAsync(BuildHistoryQuery(), outputDirectory, format).ConfigureAwait(false);
                RunOnUiThread(() =>
                {
                    LastExportFilePath = path;
                    StatusText = $"导出完成：{path}";
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    LastExportFilePath = string.Empty;
                    StatusText = $"导出失败：{ex.Message}";
                });
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
            }
        }

        private void OpenLastExportLocation()
        {
            string targetPath = LastExportFilePath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            try
            {
                string? directory = Path.GetDirectoryName(targetPath);
                if (File.Exists(targetPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{targetPath}\"",
                        UseShellExecute = true
                    });
                    StatusText = $"已打开导出位置：{targetPath}";
                    return;
                }

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                    StatusText = $"导出文件不存在，已打开目录：{directory}";
                    return;
                }

                StatusText = $"导出文件位置不存在：{targetPath}";
            }
            catch (Exception ex)
            {
                StatusText = $"打开导出位置失败：{ex.Message}";
            }
        }

        private async Task RefreshForPageAsync(AlarmWorkbenchPageItem? page, string successMessage)
        {
            string viewName = page?.ViewName ?? nameof(AlarmRealtimeView);

            if (string.Equals(viewName, nameof(AlarmHistoryView), StringComparison.OrdinalIgnoreCase))
            {
                await RefreshHistoryAsync(successMessage).ConfigureAwait(false);
                return;
            }

            if (string.Equals(viewName, nameof(AlarmStatisticsView), StringComparison.OrdinalIgnoreCase))
            {
                if (CanReuseStatisticsNavigation(successMessage))
                {
                    RunOnUiThread(() => StatusText = "????????");
                    return;
                }

                _hasLoadedStatistics = true;
                await RefreshStatisticsAsync(successMessage).ConfigureAwait(false);
                return;
            }

            await RefreshAllAsync(successMessage, includeHistoryAndStatistics: false).ConfigureAwait(false);
        }

        private async Task RefreshAllAsync(string successMessage, bool includeHistoryAndStatistics = true)
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                RunOnUiThread(() => IsBusy = true);

                Task<AlarmDashboardSnapshot> dashboardTask = _alarmService.GetDashboardAsync();
                Task<IReadOnlyList<AlarmActiveRecord>> activeTask = _alarmService.GetActiveAlarmsAsync(BuildActiveQuery());
                Task<AlarmPagedResult<AlarmHistoryEntry>>? historyTask = includeHistoryAndStatistics ? LoadHistoryPageAsync() : null;
                Task<AlarmStatisticsResult>? statisticsTask = includeHistoryAndStatistics ? _alarmService.GetStatisticsAsync(BuildStatisticsQuery()) : null;
                Task<AlarmStatisticsResult>? trendStatisticsTask = includeHistoryAndStatistics ? _alarmService.GetStatisticsAsync(BuildTrendStatisticsQuery()) : null;
                Task<IReadOnlyList<string>> sourceTask = _alarmService.GetSourcesAsync();
                Task<IReadOnlyList<AlarmRealtimeEntry>> realtimeTask = _alarmService.GetRealtimeFeedAsync(RealtimeFeedCapacity);

                List<Task> tasks = new List<Task> { dashboardTask, activeTask, sourceTask, realtimeTask };
                if (historyTask != null)
                {
                    tasks.Add(historyTask);
                }

                if (statisticsTask != null)
                {
                    tasks.Add(statisticsTask);
                }

                if (trendStatisticsTask != null)
                {
                    tasks.Add(trendStatisticsTask);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ApplyDashboard(dashboardTask.Result);
                    ApplyActiveAlarms(activeTask.Result);
                    if (historyTask != null)
                    {
                        ApplyHistoryPage(historyTask.Result);
                    }

                    if (statisticsTask != null)
                    {
                        ApplyStatistics(statisticsTask.Result, trendStatisticsTask?.Result ?? statisticsTask.Result);
                        _isStatisticsCacheInvalidated = false;
                    }

                    ApplySources(sourceTask.Result);
                    ApplyRealtimeFeed(realtimeTask.Result);
                    PendingFeedCount = 0;
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"报警数据刷新失败：{ex.Message}");
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
                _refreshLock.Release();
            }
        }

        private async Task RefreshHistoryScopeAsync(string successMessage)
        {
            if (string.Equals(SelectedPage?.ViewName, nameof(AlarmStatisticsView), StringComparison.OrdinalIgnoreCase))
            {
                _hasLoadedStatistics = true;
                await RefreshStatisticsAsync(successMessage).ConfigureAwait(false);
                return;
            }

            await RefreshHistoryAsync(successMessage).ConfigureAwait(false);
        }

        private async Task RefreshHistoryAsync(string successMessage)
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                RunOnUiThread(() => IsBusy = true);

                Task<AlarmPagedResult<AlarmHistoryEntry>> historyTask = LoadHistoryPageAsync();
                Task<IReadOnlyList<string>> sourceTask = _alarmService.GetSourcesAsync();

                await Task.WhenAll(historyTask, sourceTask).ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ApplyHistoryPage(historyTask.Result);
                    ApplySources(sourceTask.Result);
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"历史记录刷新失败：{ex.Message}");
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
                _refreshLock.Release();
            }
        }

        private async Task RefreshStatisticsAsync(string successMessage)
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                RunOnUiThread(() => IsBusy = true);

                Task<AlarmStatisticsResult> statisticsTask = _alarmService.GetStatisticsAsync(BuildStatisticsQuery());
                Task<AlarmStatisticsResult> trendStatisticsTask = _alarmService.GetStatisticsAsync(BuildTrendStatisticsQuery());
                Task<IReadOnlyList<string>> sourceTask = _alarmService.GetSourcesAsync();

                await Task.WhenAll(statisticsTask, trendStatisticsTask, sourceTask).ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ApplyStatistics(statisticsTask.Result, trendStatisticsTask.Result);
                    _isStatisticsCacheInvalidated = false;

                    ApplySources(sourceTask.Result);
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"历史与统计刷新失败：{ex.Message}");
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
                _refreshLock.Release();
            }
        }

        private async Task RefreshBarStatisticsAsync(string successMessage)
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                RunOnUiThread(() => IsBusy = true);

                AlarmStatisticsResult statistics = await _alarmService
                    .GetStatisticsAsync(BuildStatisticsQuery())
                    .ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ApplyStatistics(statistics);
                    _isStatisticsCacheInvalidated = false;
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"柱状分类刷新失败：{ex.Message}");
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
                _refreshLock.Release();
            }
        }

        private async Task RefreshTrendStatisticsAsync(string successMessage)
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                RunOnUiThread(() => IsBusy = true);

                AlarmStatisticsResult trendStatistics = await _alarmService
                    .GetStatisticsAsync(BuildTrendStatisticsQuery())
                    .ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ApplyTrendStatistics(trendStatistics);
                    _isStatisticsCacheInvalidated = false;
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"报警趋势刷新失败：{ex.Message}");
            }
            finally
            {
                RunOnUiThread(() => IsBusy = false);
                _refreshLock.Release();
            }
        }

        private async Task RefreshRealtimeFeedAsync(string successMessage)
        {
            try
            {
                IReadOnlyList<AlarmRealtimeEntry> realtime = await _alarmService.GetRealtimeFeedAsync(RealtimeFeedCapacity).ConfigureAwait(false);
                RunOnUiThread(() =>
                {
                    ApplyRealtimeFeed(realtime);
                    PendingFeedCount = 0;
                    if (!string.IsNullOrWhiteSpace(successMessage))
                    {
                        StatusText = successMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"实时列表刷新失败：{ex.Message}");
            }
        }

        private async Task<AlarmPagedResult<AlarmHistoryEntry>> LoadHistoryPageAsync()
        {
            AlarmHistoryQuery query = BuildHistoryQuery();
            AlarmPagedResult<AlarmHistoryEntry> result = await _alarmService.GetHistoryPageAsync(query).ConfigureAwait(false);

            if (result.TotalPages > 0 && result.PageIndex > result.TotalPages)
            {
                query.PageIndex = result.TotalPages;
                result = await _alarmService.GetHistoryPageAsync(query).ConfigureAwait(false);
            }

            return result;
        }

        private void OnAlarmDataChanged(object? sender, AlarmDataChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _isStatisticsCacheInvalidated = true;
                ApplyDashboard(e.Dashboard);
                ApplyActiveAlarms(e.ActiveAlarms);

                if (e.LatestEvent != null)
                {
                    if (IsRealtimePaused)
                    {
                        PendingFeedCount++;
                        StatusText = $"新事件已缓存：{e.LatestEvent.Source} / {e.LatestEvent.Code}";
                    }
                    else
                    {
                        AppendRealtimeEvent(e.LatestEvent);
                        StatusText = BuildRealtimeStatusText(e.LatestEvent);
                    }
                }
                else
                {
                    StatusText = $"报警列表已更新 {DateTime.Now:HH:mm:ss}";
                }

                if (!IsStatisticsPage(SelectedPage))
                {
                    ScheduleDeferredRefresh();
                }
            });
        }

        private void ScheduleDeferredRefresh()
        {
            _deferredRefreshCts?.Cancel();
            _deferredRefreshCts?.Dispose();
            _deferredRefreshCts = new CancellationTokenSource();
            CancellationToken token = _deferredRefreshCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    await RefreshForPageAsync(SelectedPage, "报警数据已自动同步。").ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }, token);
        }

        private void ApplyDashboard(AlarmDashboardSnapshot dashboard)
        {
            ReplaceCollection(SummaryCards, new[]
            {
                new AlarmSummaryCard
                {
                    Title = "当前活动",
                    Value = dashboard.ActiveCount.ToString(),
                    Caption = dashboard.LatestRaisedAt.HasValue
                        ? $"最近触发 {dashboard.LatestRaisedAt:HH:mm:ss}"
                        : "当前没有活动报警",
                    AccentBrush = "#DC2626",
                    BackgroundBrush = "#FEF2F2"
                },
                new AlarmSummaryCard
                {
                    Title = "待确认",
                    Value = dashboard.UnacknowledgedCount.ToString(),
                    Caption = "需要操作员尽快处理的报警",
                    AccentBrush = "#D97706",
                    BackgroundBrush = "#FFFBEB"
                },
                new AlarmSummaryCard
                {
                    Title = "严重报警",
                    Value = dashboard.FatalCount.ToString(),
                    Caption = "致命等级报警建议优先联动",
                    AccentBrush = "#7C3AED",
                    BackgroundBrush = "#F5F3FF"
                },
                new AlarmSummaryCard
                {
                    Title = "今日触发",
                    Value = dashboard.TodayRaisedCount.ToString(),
                    Caption = "当天新增报警与重复触发统计",
                    AccentBrush = "#2563EB",
                    BackgroundBrush = "#EFF6FF"
                },
                new AlarmSummaryCard
                {
                    Title = "历史总量",
                    Value = dashboard.HistoryCount.ToString(),
                    Caption = "已归档的报警记录总数",
                    AccentBrush = "#059669",
                    BackgroundBrush = "#ECFDF3"
                }
            });
        }

        private void ApplyActiveAlarms(IEnumerable<AlarmActiveRecord> records)
        {
            string? selectedId = SelectedActiveAlarm?.ActiveId;
            _latestActiveAlarmRecords.Clear();
            _latestActiveAlarmRecords.AddRange(records ?? Enumerable.Empty<AlarmActiveRecord>());
            RefreshActiveAlarmCounts();
            ApplyActiveAlarmView(selectedId);
        }

        private void RefreshActiveAlarmView()
        {
            ApplyActiveAlarmView(SelectedActiveAlarm?.ActiveId);
        }

        private void ApplyActiveAlarmView(string? preferredActiveId)
        {
            List<AlarmActiveItem> items = FilterActiveRecords(_latestActiveAlarmRecords, SelectedActiveAlarmView)
                .OrderByDescending(item => item.LastRaisedAt)
                .ThenByDescending(item => item.RaisedAt)
                .Select(AlarmActiveItem.FromCore)
                .ToList();

            ReplaceCollection(ActiveAlarms, items);
            SelectedActiveAlarm = items.FirstOrDefault(item => string.Equals(item.ActiveId, preferredActiveId, StringComparison.OrdinalIgnoreCase))
                ?? items.FirstOrDefault();
        }

        private void RefreshActiveAlarmCounts()
        {
            PendingActiveCount = _latestActiveAlarmRecords.Count(IsPendingActiveAlarm);
            AcknowledgedActiveCount = _latestActiveAlarmRecords.Count(IsAcknowledgedActiveAlarm);
            AllActiveCount = _latestActiveAlarmRecords.Count;
            RaiseActiveAlarmViewTextChanged();
        }

        private static IEnumerable<AlarmActiveRecord> FilterActiveRecords(IEnumerable<AlarmActiveRecord> records, string selectedView)
        {
            string normalized = NormalizeActiveAlarmView(selectedView);
            return normalized switch
            {
                ActiveAlarmViewAcknowledged => records.Where(IsAcknowledgedActiveAlarm),
                ActiveAlarmViewAll => records,
                _ => records.Where(IsPendingActiveAlarm)
            };
        }

        private static bool IsPendingActiveAlarm(AlarmActiveRecord record)
        {
            return record.NeedAcknowledge && !record.IsAcknowledged;
        }

        private static bool IsAcknowledgedActiveAlarm(AlarmActiveRecord record)
        {
            return record.IsAcknowledged;
        }

        private static string NormalizeActiveAlarmView(string? value)
        {
            return string.Equals(value, ActiveAlarmViewAcknowledged, StringComparison.OrdinalIgnoreCase)
                ? ActiveAlarmViewAcknowledged
                : string.Equals(value, ActiveAlarmViewAll, StringComparison.OrdinalIgnoreCase)
                    ? ActiveAlarmViewAll
                    : ActiveAlarmViewPending;
        }

        private void RaiseActiveAlarmViewTextChanged()
        {
            RaisePropertyChanged(nameof(ActiveAlarmListTitle));
            RaisePropertyChanged(nameof(ActiveAlarmListDescription));
            RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
        }

        private void ApplyHistoryPage(AlarmPagedResult<AlarmHistoryEntry> page)
        {
            ReplaceCollection(HistoryAlarms, page.Items.Select(AlarmHistoryItem.FromCore));
            CurrentPage = Math.Max(1, page.PageIndex);
            TotalPages = Math.Max(1, page.TotalPages);
            TotalHistoryCount = page.TotalCount;
        }

        private void ApplyStatistics(AlarmStatisticsResult statistics, AlarmStatisticsResult? trendStatistics = null)
        {
            List<AlarmTypeDistributionItem> allTypeItems = statistics.TypeDistribution
                .OrderByDescending(item => item.Count)
                .ThenBy(item => string.IsNullOrWhiteSpace(item.Name) ? item.Code : item.Name)
                .ToList();
            List<AlarmTypeDistributionItem> typeItems = allTypeItems
                .Take(6)
                .ToList();
            List<AlarmSourceDistributionItem> sourceItems = statistics.SourceDistribution
                .OrderByDescending(item => item.Count)
                .Take(6)
                .ToList();

            int typeMax = Math.Max(1, typeItems.Count == 0 ? 1 : typeItems.Max(item => item.Count));
            int sourceMax = Math.Max(1, sourceItems.Count == 0 ? 1 : sourceItems.Max(item => item.Count));
            int totalCount = statistics.TrendPoints.Sum(item => item.Count);
            int barMax = Math.Max(1, statistics.TrendPoints.Count == 0 ? 1 : statistics.TrendPoints.Max(item => item.Count));
            IsBarStatisticsEmpty = totalCount <= 0;
            int averageCount = statistics.TrendPoints.Count == 0
                ? 0
                : (int)Math.Round(totalCount / (double)statistics.TrendPoints.Count, MidpointRounding.AwayFromZero);
            AlarmTypeDistributionItem? topType = typeItems.FirstOrDefault();
            AlarmSourceDistributionItem? topSource = sourceItems.FirstOrDefault();
            string topTypeText = topType == null
                ? "暂无"
                : $"{(string.IsNullOrWhiteSpace(topType.Name) ? topType.Code : topType.Name)}({topType.Count})";
            string topSourceText = topSource == null
                ? "暂无"
                : $"{topSource.Source}({topSource.Count})";

            StatisticsSummaryText = $"范围 {statistics.RangeStart:yyyy-MM-dd HH:mm:ss} ~ {statistics.RangeEnd:yyyy-MM-dd HH:mm:ss} / 总量 {totalCount} / 平均 {averageCount} / 高频类型 {topTypeText} / 高频来源 {topSourceText}";

            ReplaceCollection(BarTrendPoints, statistics.TrendPoints.Select(item => new DisplayAlarmTrendPoint
            {
                Label = FormatBarBucketLabel(item.BucketStart, statistics.Bucket),
                Count = item.Count,
                Maximum = barMax,
                AccentBrush = item.BucketStart.Date == DateTime.Today ? "#DC2626" : "#2563EB",
                Description = BuildTrendDescription(statistics.Bucket, item.BucketStart)
            }));

            ReplaceCollection(TypeStatistics, typeItems.Select(item => new AlarmStatisticBarItem
            {
                Label = string.IsNullOrWhiteSpace(item.Name) ? item.Code : item.Name,
                Count = item.Count,
                Maximum = typeMax,
                AccentBrush = AlarmWorkbenchPalette.GetBadgeBrush(item.HighestSeverity),
                Description = item.LatestRaisedAt.HasValue
                    ? (string.IsNullOrWhiteSpace(item.Code)
                        ? $"最近触发 {item.LatestRaisedAt:MM-dd HH:mm}"
                        : $"{item.Code} / 最近 {item.LatestRaisedAt:MM-dd HH:mm}")
                    : "暂无最新时间"
            }));

            ReplaceCollection(SourceStatistics, sourceItems.Select(item => new AlarmStatisticBarItem
            {
                Label = item.Source,
                Count = item.Count,
                Maximum = sourceMax,
                AccentBrush = AlarmWorkbenchPalette.GetBadgeBrush(item.HighestSeverity),
                Description = $"最高等级 {AlarmWorkbenchPalette.GetSeverityText(item.HighestSeverity)}"
            }));

            if (trendStatistics != null)
            {
                ApplyTrendStatistics(trendStatistics);
            }

            BuildTypePieSlices(allTypeItems);

            ReplaceCollection(StatisticCards, new[]
            {
                new AlarmSummaryCard
                {
                    Title = "查询总量",
                    Value = totalCount.ToString(),
                    Caption = $"{statistics.RangeStart:MM-dd} ~ {statistics.RangeEnd:MM-dd}",
                    AccentBrush = "#2563EB",
                    BackgroundBrush = "#EFF6FF"
                },
                new AlarmSummaryCard
                {
                    Title = "平均波动",
                    Value = averageCount.ToString(),
                    Caption = "按当前统计桶计算的平均次数",
                    AccentBrush = "#059669",
                    BackgroundBrush = "#ECFDF3"
                },
                new AlarmSummaryCard
                {
                    Title = "高频类型",
                    Value = (typeItems.FirstOrDefault()?.Count ?? 0).ToString(),
                    Caption = typeItems.FirstOrDefault() == null
                        ? "暂无统计数据"
                        : $"排名第一：{(string.IsNullOrWhiteSpace(typeItems[0].Name) ? typeItems[0].Code : typeItems[0].Name)}",
                    AccentBrush = "#D97706",
                    BackgroundBrush = "#FFFBEB"
                },
                new AlarmSummaryCard
                {
                    Title = "高频来源",
                    Value = (sourceItems.FirstOrDefault()?.Count ?? 0).ToString(),
                    Caption = sourceItems.FirstOrDefault() == null
                        ? "暂无统计数据"
                        : $"排名第一：{sourceItems[0].Source}",
                    AccentBrush = "#7C3AED",
                    BackgroundBrush = "#F5F3FF"
                }
            });
        }

        private void ApplySources(IEnumerable<string> sources)
        {
            string currentValue = SelectedHistorySource;
            List<AlarmOptionItem> items = new List<AlarmOptionItem>
            {
                new AlarmOptionItem
                {
                    Label = "全部来源",
                    Value = string.Empty
                }
            };

            items.AddRange(sources
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .Select(item => new AlarmOptionItem
                {
                    Label = item,
                    Value = item
                }));

            ReplaceCollection(SourceOptions, items);
            if (items.All(item => !string.Equals(item.Value, currentValue, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedHistorySource = string.Empty;
            }
        }

        private void ApplyTrendStatistics(AlarmStatisticsResult statistics)
        {
            int trendMax = Math.Max(1, statistics.TrendPoints.Count == 0 ? 1 : statistics.TrendPoints.Max(item => item.Count));
            ReplaceCollection(TrendPoints, statistics.TrendPoints.Select(item => new DisplayAlarmTrendPoint
            {
                Label = FormatBucketLabel(item.BucketStart, statistics.Bucket),
                Count = item.Count,
                Maximum = trendMax,
                AccentBrush = item.BucketStart.Date == DateTime.Today ? "#DC2626" : "#2563EB",
                Description = BuildTrendDescription(statistics.Bucket, item.BucketStart)
            }));
        }

        private void BuildTypePieSlices(IReadOnlyList<AlarmTypeDistributionItem> typeItems)
        {
            const int visibleSliceCount = 5;
            string[] colors =
            {
                "#2563EB",
                "#DC2626",
                "#D97706",
                "#7C3AED",
                "#059669",
                "#64748B"
            };

            List<(string Label, int Count)> slices = typeItems
                .Where(item => item.Count > 0)
                .Select(item => (string.IsNullOrWhiteSpace(item.Name) ? item.Code : item.Name, item.Count))
                .Take(visibleSliceCount)
                .ToList();

            int otherCount = typeItems
                .Where(item => item.Count > 0)
                .Skip(visibleSliceCount)
                .Sum(item => item.Count);
            if (otherCount > 0)
            {
                slices.Add(("其他", otherCount));
            }

            int total = slices.Sum(item => item.Count);
            if (total <= 0)
            {
                IsTypePieStatisticsEmpty = true;
                ReplaceCollection(TypePieSlices, Array.Empty<AlarmPieSliceItem>());
                return;
            }

            IsTypePieStatisticsEmpty = false;

            List<AlarmPieSliceItem> items = new List<AlarmPieSliceItem>();
            for (int index = 0; index < slices.Count; index++)
            {
                (string label, int count) = slices[index];
                double percentage = count / (double)total;
                Brush fillBrush = new SolidColorBrush(ParseColor(colors[Math.Min(index, colors.Length - 1)]));
                if (fillBrush.CanFreeze)
                {
                    fillBrush.Freeze();
                }

                items.Add(new AlarmPieSliceItem
                {
                    Label = label,
                    Count = count,
                    Percentage = percentage,
                    FillBrush = fillBrush
                });
            }

            ReplaceCollection(TypePieSlices, items);
        }

        private static Color ParseColor(string hex)
        {
            object? value = ColorConverter.ConvertFromString(hex);
            return value is Color color ? color : Colors.SteelBlue;
        }

        private void ApplyRealtimeFeed(IEnumerable<AlarmRealtimeEntry> entries)
        {
            List<AlarmFeedItem> items = entries
                .Reverse()
                .Select((entry, index) => AlarmFeedItem.FromCore(entry, index == 0))
                .ToList();

            ReplaceCollection(RealtimeFeed, items);
        }

        private void AppendRealtimeEvent(AlarmRealtimeEntry entry)
        {
            if (RealtimeFeed.Count > 0)
            {
                RealtimeFeed[0].IsLatest = false;
            }

            RealtimeFeed.Insert(0, AlarmFeedItem.FromCore(entry, true));
            while (RealtimeFeed.Count > RealtimeFeedCapacity)
            {
                RealtimeFeed.RemoveAt(RealtimeFeed.Count - 1);
            }
        }

        private AlarmActiveQuery BuildActiveQuery()
        {
            return new AlarmActiveQuery
            {
                MaxCount = ActiveAlarmMaxCount
            };
        }

        private AlarmHistoryQuery BuildHistoryQuery()
        {
            (DateTime? start, DateTime? end) = BuildFilterDateTimeRange(useDefaultDates: false);

            return new AlarmHistoryQuery
            {
                StartTime = start,
                EndTime = end,
                Severity = ParseSeverity(SelectedHistoryLevel),
                Source = SelectedHistorySource,
                Keyword = HistoryKeyword?.Trim() ?? string.Empty,
                PageIndex = CurrentPage,
                PageSize = HistoryPageSize,
                MaxCount = HistoryPageSize,
                IncludeActive = false
            };
        }

        private AlarmStatisticsQuery BuildStatisticsQuery()
        {
            (DateTime? startValue, DateTime? endValue) = BuildBarFilterDateTimeRange(useDefaultDates: true);
            DateTime start = startValue ?? DateTime.Today;
            DateTime end = endValue ?? start.AddHours(12).AddTicks(-1);

            return new AlarmStatisticsQuery
            {
                StartTime = start,
                EndTime = end,
                Severity = ParseSeverity(SelectedHistoryLevel),
                Source = SelectedHistorySource,
                Keyword = HistoryKeyword?.Trim() ?? string.Empty,
                Bucket = AlarmChartBucket.Hour,
                TopCount = 20
            };
        }

        private AlarmStatisticsQuery BuildTrendStatisticsQuery()
        {
            (DateTime? startValue, DateTime? endValue) = BuildTrendFilterDateTimeRange(useDefaultDates: true);
            DateTime start = startValue ?? DateTime.Today.AddDays(-6);
            DateTime end = endValue ?? DateTime.Today.AddDays(1).AddTicks(-1);

            return new AlarmStatisticsQuery
            {
                StartTime = start,
                EndTime = end,
                Severity = ParseSeverity(SelectedHistoryLevel),
                Source = SelectedHistorySource,
                Keyword = HistoryKeyword?.Trim() ?? string.Empty,
                Bucket = AlarmChartBucket.Day,
                TopCount = 20
            };
        }

        private void SetFilterDateTimeRange(DateTime start, DateTime end)
        {
            FilterStartDate = start.Date;
            FilterStartTimeText = start.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            FilterEndDate = end.Date;
            FilterEndTimeText = end.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void SetBarFilterDateTimeRange(DateTime start)
        {
            BarFilterStartDate = start.Date;
            BarFilterStartTimeText = start.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void SetTrendFilterDateTimeRange(DateTime start, DateTime end)
        {
            TrendFilterStartDate = start.Date;
            TrendFilterEndDate = end.Date;
        }

        private (DateTime? Start, DateTime? End) BuildFilterDateTimeRange(bool useDefaultDates)
        {
            DateTime? startDate = FilterStartDate?.Date;
            DateTime? endDate = FilterEndDate?.Date;

            if (useDefaultDates)
            {
                startDate ??= DateTime.Today.AddDays(-6);
                endDate ??= DateTime.Today;
            }

            DateTime? start = startDate.HasValue
                ? startDate.Value.Add(ParseFilterTime(FilterStartTimeText, TimeSpan.Zero))
                : null;
            DateTime? end = endDate.HasValue
                ? endDate.Value.Add(ParseFilterTime(FilterEndTimeText, new TimeSpan(23, 59, 59)))
                : null;

            if (start.HasValue && end.HasValue && start > end)
            {
                (start, end) = (end, start);
            }

            (start, end) = ClampDateTimeRangeToNow(start, end);
            return (start, end);
        }

        private (DateTime? Start, DateTime? End) BuildBarFilterDateTimeRange(bool useDefaultDates)
        {
            DateTime? startDate = BarFilterStartDate?.Date;
            if (useDefaultDates)
            {
                startDate ??= DateTime.Today;
            }

            if (!startDate.HasValue)
            {
                return (null, null);
            }

            DateTime now = DateTime.Now;
            DateTime start = startDate.Value.Add(ParseFilterTime(BarFilterStartTimeText, TimeSpan.Zero));
            start = ClampBarStartToNow(start, now);
            DateTime end = start.AddHours(12).AddTicks(-1);
            if (start <= now && end > now)
            {
                end = now;
            }

            return (start, end);
        }

        private (DateTime? Start, DateTime? End) BuildTrendFilterDateTimeRange(bool useDefaultDates)
        {
            DateTime? startDate = TrendFilterStartDate?.Date;
            DateTime? endDate = TrendFilterEndDate?.Date;

            if (useDefaultDates)
            {
                startDate ??= DateTime.Today.AddDays(-6);
                endDate ??= DateTime.Today;
            }

            DateTime? start = startDate;
            DateTime? end = endDate.HasValue
                ? endDate.Value.Add(new TimeSpan(23, 59, 59))
                : null;

            if (start.HasValue && end.HasValue && start > end)
            {
                (start, end) = (end, start);
            }

            (start, end) = ClampDateTimeRangeToNow(start, end);
            return (start, end);
        }

        private static (DateTime? Start, DateTime? End) ClampDateTimeRangeToNow(DateTime? start, DateTime? end)
        {
            DateTime now = DateTime.Now;
            if (start.HasValue && start.Value > now)
            {
                start = now;
            }

            if (end.HasValue && end.Value > now)
            {
                end = now;
            }

            if (start.HasValue && end.HasValue && start.Value > end.Value)
            {
                start = end;
            }

            return (start, end);
        }

        private static (DateTime Start, DateTime End) ClampShiftedDateTimeRangeToNow(DateTime start, DateTime end, TimeSpan span)
        {
            DateTime now = DateTime.Now;
            if (end <= now)
            {
                return (start, end);
            }

            TimeSpan safeSpan = span > TimeSpan.Zero ? span : TimeSpan.FromHours(1);
            DateTime clampedEnd = now;
            DateTime clampedStart = clampedEnd - safeSpan;
            if (clampedStart > clampedEnd)
            {
                clampedStart = clampedEnd;
            }

            return (clampedStart, clampedEnd);
        }

        private static DateTime ClampBarStartToNow(DateTime start, DateTime now)
        {
            DateTime maxStart = GetCurrentHourStart(now);
            return start > maxStart ? maxStart : start;
        }

        private static DateTime GetCurrentHourStart(DateTime now)
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Kind);
        }

        private static TimeSpan ParseFilterTime(string? value, TimeSpan fallback)
        {
            string text = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            string[] formats =
            {
                @"hh\:mm\:ss",
                @"h\:mm\:ss",
                @"hh\:mm",
                @"h\:mm"
            };

            if (TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out TimeSpan parsed)
                && parsed >= TimeSpan.Zero
                && parsed < TimeSpan.FromDays(1))
            {
                return parsed;
            }

            return fallback;
        }

        private void InitializeOptions()
        {
            ReplaceCollection(LevelOptions, new[]
            {
                new AlarmOptionItem { Label = "全部等级", Value = string.Empty },
                new AlarmOptionItem { Label = "致命", Value = nameof(AlarmSeverity.Fatal) },
                new AlarmOptionItem { Label = "错误", Value = nameof(AlarmSeverity.Error) },
                new AlarmOptionItem { Label = "预警", Value = nameof(AlarmSeverity.Warning) },
                new AlarmOptionItem { Label = "信息", Value = nameof(AlarmSeverity.Info) }
            });

            ReplaceCollection(SourceOptions, new[]
            {
                new AlarmOptionItem { Label = "全部来源", Value = string.Empty }
            });
        }

        private static AlarmSeverity? ParseSeverity(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Enum.TryParse(value, true, out AlarmSeverity severity)
                ? severity
                : null;
        }

        private bool CanRunCommand()
        {
            return !IsBusy;
        }

        private bool CanRunAlarmDemoCommand()
        {
            return IsAlarmDemoAvailable && !IsBusy;
        }

        private bool CanRaiseAlarmDemoScenario(AlarmDemoScenarioItem? scenario)
        {
            return scenario != null && CanRunAlarmDemoCommand();
        }

        private bool CanConfirmSelected()
        {
            return !IsBusy &&
                SelectedActiveAlarm != null &&
                SelectedActiveAlarm.NeedAcknowledge &&
                !SelectedActiveAlarm.IsConfirmed;
        }

        private bool CanClearSelected()
        {
            return !IsBusy &&
                SelectedActiveAlarm != null;
        }

        private bool CanPreviousPage()
        {
            return !IsBusy && CurrentPage > 1;
        }

        private bool CanNextPage()
        {
            return !IsBusy && CurrentPage < TotalPages;
        }

        private bool CanOpenLastExportLocation()
        {
            return !IsBusy && HasExportedFileLocation;
        }

        private static string BuildRealtimeStatusText(AlarmRealtimeEntry entry)
        {
            return $"实时事件：{entry.Source} / {entry.Code} / {AlarmWorkbenchPalette.GetRealtimeActionText(entry.EventKind)}";
        }

        private static string FormatBucketLabel(DateTime bucketStart, AlarmChartBucket bucket)
        {
            return bucket switch
            {
                AlarmChartBucket.Hour => bucketStart.ToString("MM-dd HH点"),
                AlarmChartBucket.Week => bucketStart.ToString("MM-dd"),
                AlarmChartBucket.Month => bucketStart.ToString("yyyy-MM"),
                _ => bucketStart.ToString("MM-dd")
            };
        }

        private static string FormatBarBucketLabel(DateTime bucketStart, AlarmChartBucket bucket)
        {
            return bucket switch
            {
                AlarmChartBucket.Hour => bucketStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                AlarmChartBucket.Week => bucketStart.ToString("MM-dd", CultureInfo.InvariantCulture),
                AlarmChartBucket.Month => bucketStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                _ => bucketStart.ToString("HH:mm", CultureInfo.InvariantCulture)
            };
        }

        private static string BuildTrendDescription(AlarmChartBucket bucket, DateTime bucketStart)
        {
            return bucket switch
            {
                AlarmChartBucket.Hour => $"{bucketStart:MM-dd HH}点时段",
                AlarmChartBucket.Week => $"{bucketStart:MM-dd} 起始周",
                AlarmChartBucket.Month => $"{bucketStart:yyyy-MM} 统计",
                _ => $"{bucketStart:MM-dd} 日统计"
            };
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (T item in items)
            {
                target.Add(item);
            }
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            Dispatcher? dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }

        private static bool IsDesignRelatedUser()
        {
            IUserService? userService = PrismProvider.User;
            ReeYin_V.Core.Models.CurrentUser? user = userService?.CurUser;
            if (user == null)
            {
                return false;
            }

            if (user.PermissionID > 0 && user.PermissionID <= (int)UserPermission.Admin)
            {
                return true;
            }

            string roleName = userService?.AllRole?.FirstOrDefault(item => item.RoleId == user.RoleID)?.RoleName ?? string.Empty;
            return ContainsDesignKeyword(user.UserName) || ContainsDesignKeyword(roleName);
        }

        private static bool ContainsDesignKeyword(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.IndexOf("\u8bbe\u8ba1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("\u5de5\u7a0b", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("\u5de5\u827a", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("\u7ba1\u7406\u5458", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Design", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Designer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Engineer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string ResolveCurrentUser()
        {
            return PrismProvider.User?.CurUser?.UserName ?? "System";
        }

        private void RaiseCommandStates()
        {
            ConfirmSelectedCommand.RaiseCanExecuteChanged();
            ClearSelectedCommand.RaiseCanExecuteChanged();
            ApplyHistoryFilterCommand.RaiseCanExecuteChanged();
            ResetHistoryFilterCommand.RaiseCanExecuteChanged();
            ShiftTimeRangeBackwardCommand.RaiseCanExecuteChanged();
            ShiftTimeRangeForwardCommand.RaiseCanExecuteChanged();
            ApplyBarFilterCommand.RaiseCanExecuteChanged();
            ShiftBarTimeRangeBackwardCommand.RaiseCanExecuteChanged();
            ShiftBarTimeRangeForwardCommand.RaiseCanExecuteChanged();
            ApplyTrendFilterCommand.RaiseCanExecuteChanged();
            ShiftTrendTimeRangeBackwardCommand.RaiseCanExecuteChanged();
            ShiftTrendTimeRangeForwardCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportExcelCommand.RaiseCanExecuteChanged();
            OpenLastExportLocationCommand.RaiseCanExecuteChanged();
            OpenAlarmDefinitionsPopupCommand.RaiseCanExecuteChanged();
            OpenAlarmDemoPopupCommand.RaiseCanExecuteChanged();
            RaiseAlarmDemoScenarioCommand.RaiseCanExecuteChanged();
            RaiseAllAlarmDemoScenariosCommand.RaiseCanExecuteChanged();
            RaiseRepeatedAlarmDemoCommand.RaiseCanExecuteChanged();
            ClearAlarmDemoCommand.RaiseCanExecuteChanged();
        }

    }
}

using Prism.Commands;
using Prism.Navigation.Regions;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReeYin.Status.ViewModels
{
    /// <summary>
    /// 报警中心页面 ViewModel。
    /// 当前主要承担展示骨架和调试入口，等待具体报警服务实现接入后即可承载实时数据。
    /// </summary>
    public class AlarmCenterViewModel : DialogViewModelBase, INavigationAware
    {
        private readonly IAlarmService _alarmService;
        private bool _isSubscribed;

        public AlarmCenterViewModel()
        {
            Title = "报警中心";
            Overview = new AlarmOverviewSnapshot();
            _alarmService = TryResolveAlarmService();
            IsServiceConnected = _alarmService != null;
            StatusMessage = IsServiceConnected
                ? "报警中心已连接到报警服务。"
                : "报警模块骨架已加载，等待具体报警服务实现接入。";
        }

        public ObservableCollection<AlarmSnapshot> ActiveAlarms { get; } = new ObservableCollection<AlarmSnapshot>();

        public ObservableCollection<AlarmHistoryEntry> RecentHistory { get; } = new ObservableCollection<AlarmHistoryEntry>();

        public ObservableCollection<AlarmStatisticsSnapshot> Statistics { get; } = new ObservableCollection<AlarmStatisticsSnapshot>();

        public ObservableCollection<AlarmDefinition> Definitions { get; } = new ObservableCollection<AlarmDefinition>();

        private AlarmOverviewSnapshot _overview;
        public AlarmOverviewSnapshot Overview
        {
            get => _overview;
            set => SetProperty(ref _overview, value ?? new AlarmOverviewSnapshot());
        }

        private AlarmSnapshot _selectedActiveAlarm;
        public AlarmSnapshot SelectedActiveAlarm
        {
            get => _selectedActiveAlarm;
            set => SetProperty(ref _selectedActiveAlarm, value);
        }

        private AlarmDefinition _selectedDefinition;
        public AlarmDefinition SelectedDefinition
        {
            get => _selectedDefinition;
            set => SetProperty(ref _selectedDefinition, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isServiceConnected;
        public bool IsServiceConnected
        {
            get => _isServiceConnected;
            set => SetProperty(ref _isServiceConnected, value);
        }

        public DelegateCommand LoadCommand => new DelegateCommand(async () => await LoadAsync());

        public DelegateCommand UnLoadedCommand => new DelegateCommand(UnsubscribeEvents);

        public DelegateCommand RefreshCommand => new DelegateCommand(async () => await RefreshAsync());

        public DelegateCommand AckSelectedCommand => new DelegateCommand(async () => await AcknowledgeSelectedAsync(), CanOperateSelectedAlarm)
            .ObservesProperty(() => SelectedActiveAlarm)
            .ObservesProperty(() => IsServiceConnected);

        public DelegateCommand ClearSelectedCommand => new DelegateCommand(async () => await ClearSelectedAsync(), CanOperateSelectedAlarm)
            .ObservesProperty(() => SelectedActiveAlarm)
            .ObservesProperty(() => IsServiceConnected);

        public DelegateCommand ShelveSelectedCommand => new DelegateCommand(async () => await ShelveSelectedAsync(), CanOperateSelectedAlarm)
            .ObservesProperty(() => SelectedActiveAlarm)
            .ObservesProperty(() => IsServiceConnected);

        public DelegateCommand SaveConfigCommand => new DelegateCommand(async () => await SaveDefinitionsAsync(), () => IsServiceConnected)
            .ObservesProperty(() => IsServiceConnected);

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        private async Task LoadAsync()
        {
            if (!IsServiceConnected)
            {
                StatusMessage = "当前仅提供页面与服务基类骨架，后续接入具体报警实现后会显示实时数据。";
                return;
            }

            // 页面首次加载时再订阅事件，避免 ViewModel 构造阶段就持有外部服务回调。
            if (!_isSubscribed)
            {
                _alarmService.AlarmChanged += AlarmService_AlarmChanged;
                _alarmService.ActiveAlarmsChanged += AlarmService_ActiveAlarmsChanged;
                _isSubscribed = true;
            }

            await _alarmService.InitializeAsync();
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (!IsServiceConnected)
            {
                return;
            }

            try
            {
                IsLoading = true;
                // 页面只消费快照副本，避免 UI 层误改服务内部状态。
                ReplaceCollection(ActiveAlarms, _alarmService.ActiveAlarms.Select(item => item.CreateCopy()));
                Overview = _alarmService.Overview?.CreateCopy() ?? new AlarmOverviewSnapshot();

                IReadOnlyList<AlarmHistoryEntry> history = await _alarmService.QueryHistoryAsync(new AlarmHistoryQuery { MaxCount = 100 });
                ReplaceCollection(RecentHistory, history.Select(item => item.CreateCopy()));

                IReadOnlyList<AlarmStatisticsSnapshot> statistics = await _alarmService.QueryStatisticsAsync(new AlarmStatisticsQuery { MaxCount = 50 });
                ReplaceCollection(Statistics, statistics.Select(item => item.CreateCopy()));

                IReadOnlyList<AlarmDefinition> definitions = await _alarmService.GetDefinitionsAsync();
                ReplaceCollection(Definitions, definitions.Select(item => item.CreateCopy()));

                StatusMessage = $"已刷新报警总览，当前活动报警 {Overview.ActiveCount} 条。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新报警中心失败：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AcknowledgeSelectedAsync()
        {
            if (!CanOperateSelectedAlarm())
            {
                return;
            }

            await _alarmService.AcknowledgeAsync(SelectedActiveAlarm.AlarmId, ResolveCurrentOperator(), "UI Ack");
            await RefreshAsync();
        }

        private async Task ClearSelectedAsync()
        {
            if (!CanOperateSelectedAlarm())
            {
                return;
            }

            await _alarmService.ClearAsync(SelectedActiveAlarm.AlarmId, ResolveCurrentOperator(), "UI Clear");
            await RefreshAsync();
        }

        private async Task ShelveSelectedAsync()
        {
            if (!CanOperateSelectedAlarm())
            {
                return;
            }

            await _alarmService.ShelveAsync(SelectedActiveAlarm.AlarmId, ResolveCurrentOperator(), TimeSpan.FromMinutes(10), "UI Shelve");
            await RefreshAsync();
        }

        private async Task SaveDefinitionsAsync()
        {
            if (!IsServiceConnected)
            {
                return;
            }

            try
            {
                await _alarmService.SaveDefinitionsAsync(Definitions.Select(item => item.CreateCopy()).ToArray());
                StatusMessage = $"已保存 {Definitions.Count} 条报警配置。";
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存报警配置失败：{ex.Message}";
            }
        }

        private void AlarmService_AlarmChanged(object sender, AlarmChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                string target = e?.ActiveAlarm?.Name
                    ?? e?.HistoryEntry?.Name
                    ?? e?.ActiveAlarm?.Code
                    ?? e?.HistoryEntry?.Code
                    ?? "报警";
                StatusMessage = $"{e?.Action}: {target}";
                _ = RefreshAsync();
            });
        }

        private void AlarmService_ActiveAlarmsChanged(object sender, AlarmCollectionChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                ReplaceCollection(ActiveAlarms, e.ActiveAlarms.Select(item => item.CreateCopy()));
                Overview = e.Overview?.CreateCopy() ?? new AlarmOverviewSnapshot();
            });
        }

        private void UnsubscribeEvents()
        {
            if (_alarmService == null || !_isSubscribed)
            {
                return;
            }

            _alarmService.AlarmChanged -= AlarmService_AlarmChanged;
            _alarmService.ActiveAlarmsChanged -= AlarmService_ActiveAlarmsChanged;
            _isSubscribed = false;
        }

        private bool CanOperateSelectedAlarm()
        {
            return IsServiceConnected && SelectedActiveAlarm != null && !string.IsNullOrWhiteSpace(SelectedActiveAlarm.AlarmId);
        }

        private IAlarmService TryResolveAlarmService()
        {
            try
            {
                // 这里先尝试从容器拿服务；如果当前还没注册具体实现，页面仍然能以骨架模式打开。
                return PrismProvider.Container?.Resolve(typeof(IAlarmService)) as IAlarmService;
            }
            catch
            {
                return null;
            }
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (T item in items ?? Enumerable.Empty<T>())
            {
                collection.Add(item);
            }
        }

        private string ResolveCurrentOperator()
        {
            return PrismProvider.User?.CurUser?.UserName ?? "System";
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            // 报警事件可能来自轮询线程或硬件线程，统一切回 UI 线程更新界面。
            if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            PrismProvider.Dispatcher.BeginInvoke(action);
        }
    }
}

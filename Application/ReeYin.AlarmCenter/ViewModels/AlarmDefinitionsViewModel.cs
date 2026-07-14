#nullable enable
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin.AlarmCenter.Models;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Alarm.Config;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.AlarmCenter.ViewModels
{
    public sealed class AlarmDefinitionsViewModel : BindableBase, INavigationAware
    {
        private readonly IAlarmConfigService _configService;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private AlarmDefinitionItem? _selectedDefinition;
        private AlarmDefinitionItem? _editingDefinition;
        private AlarmHardwareRuleItem? _selectedHardwareRule;
        private AlarmHardwareRuleItem? _editingHardwareRule;
        private string _definitionKeyword = string.Empty;
        private bool _isBusy;
        private bool _initialized;
        private bool _isDefinitionEditorOpen;
        private bool _isHardwareRuleEditorOpen;
        private string _statusText = "Waiting for alarm configuration.";

        public AlarmDefinitionsViewModel()
            : this(PrismProvider.AlarmConfigService)
        {
        }

        public AlarmDefinitionsViewModel(IAlarmConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            Definitions = new ObservableCollection<AlarmDefinitionItem>();
            HardwareRules = new ObservableCollection<AlarmHardwareRuleItem>();
            SeverityOptions = new ObservableCollection<AlarmSeverityOption>
            {
                new AlarmSeverityOption { Value = AlarmSeverity.Fatal, Label = "Fatal" },
                new AlarmSeverityOption { Value = AlarmSeverity.Error, Label = "Error" },
                new AlarmSeverityOption { Value = AlarmSeverity.Warning, Label = "Warning" },
                new AlarmSeverityOption { Value = AlarmSeverity.Info, Label = "Info" }
            };
            PopupModeOptions = new ObservableCollection<AlarmPopupModeOption>
            {
                new AlarmPopupModeOption { Value = AlarmPopupMode.Growl, Label = AlarmDefinitionItem.GetPopupModeText(AlarmPopupMode.Growl) },
                new AlarmPopupModeOption { Value = AlarmPopupMode.Modal, Label = AlarmDefinitionItem.GetPopupModeText(AlarmPopupMode.Modal) },
                new AlarmPopupModeOption { Value = AlarmPopupMode.None, Label = AlarmDefinitionItem.GetPopupModeText(AlarmPopupMode.None) }
            };

            RefreshCommand = new DelegateCommand(async () => await SafeRunAsync(RefreshAsync, "Failed to load alarm configuration."), CanRunCommand);
            NewDefinitionCommand = new DelegateCommand(NewDefinition, CanEditAlarmConfigurationCommand);
            EditDefinitionCommand = new DelegateCommand(EditDefinition, CanEditDefinition);
            SaveDefinitionCommand = new DelegateCommand(async () => await SafeRunAsync(SaveDefinitionAsync, "Failed to save alarm definition."), CanSaveDefinition);
            CancelDefinitionEditCommand = new DelegateCommand(CancelDefinitionEdit, CanCloseDefinitionEditor);
            ToggleDefinitionCommand = new DelegateCommand(async () => await SafeRunAsync(ToggleDefinitionAsync, "Failed to toggle alarm definition."), CanToggleDefinition);

            NewHardwareRuleCommand = new DelegateCommand(NewHardwareRule, CanEditAlarmConfigurationCommand);
            EditHardwareRuleCommand = new DelegateCommand(EditHardwareRule, CanEditHardwareRule);
            SaveHardwareRuleCommand = new DelegateCommand(async () => await SafeRunAsync(SaveHardwareRuleAsync, "Failed to save trigger rule."), CanSaveHardwareRule);
            CancelHardwareRuleEditCommand = new DelegateCommand(CancelHardwareRuleEdit, CanCloseHardwareRuleEditor);
            ToggleHardwareRuleCommand = new DelegateCommand(async () => await SafeRunAsync(ToggleHardwareRuleAsync, "Failed to toggle trigger rule."), CanToggleHardwareRule);
        }

        public ObservableCollection<AlarmDefinitionItem> Definitions { get; }
        public ObservableCollection<AlarmHardwareRuleItem> HardwareRules { get; }
        public ObservableCollection<AlarmSeverityOption> SeverityOptions { get; }
        public ObservableCollection<AlarmPopupModeOption> PopupModeOptions { get; }

        public IReadOnlyList<AlarmHardwareRuleTriggerKindOption> HardwareTriggerKindOptions { get; } =
            Enum.GetValues(typeof(AlarmTriggerKind))
                .Cast<AlarmTriggerKind>()
                .Select(value => new AlarmHardwareRuleTriggerKindOption
                {
                    Value = value,
                    Label = AlarmHardwareRuleItem.GetTriggerKindText(value)
                })
                .ToArray();

        public IReadOnlyList<AlarmHardwareRuleOperatorOption> HardwareOperatorOptions { get; } =
            Enum.GetValues(typeof(AlarmRuleOperator))
                .Cast<AlarmRuleOperator>()
                .Select(value => new AlarmHardwareRuleOperatorOption
                {
                    Value = value,
                    Label = AlarmHardwareRuleItem.GetOperatorText(value)
                })
                .ToArray();

        public IReadOnlyList<AlarmHardwareRuleClearKindOption> HardwareClearKindOptions { get; } =
            Enum.GetValues(typeof(AlarmClearMode))
                .Cast<AlarmClearMode>()
                .Select(value => new AlarmHardwareRuleClearKindOption
                {
                    Value = value,
                    Label = AlarmHardwareRuleItem.GetClearKindText(value)
                })
                .ToArray();

        public AlarmDefinitionItem? SelectedDefinition
        {
            get => _selectedDefinition;
            set
            {
                if (SetProperty(ref _selectedDefinition, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public AlarmDefinitionItem? EditingDefinition
        {
            get => _editingDefinition;
            set
            {
                if (SetProperty(ref _editingDefinition, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public AlarmHardwareRuleItem? SelectedHardwareRule
        {
            get => _selectedHardwareRule;
            set
            {
                if (SetProperty(ref _selectedHardwareRule, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public AlarmHardwareRuleItem? EditingHardwareRule
        {
            get => _editingHardwareRule;
            set
            {
                if (SetProperty(ref _editingHardwareRule, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string DefinitionKeyword
        {
            get => _definitionKeyword;
            set => SetProperty(ref _definitionKeyword, value ?? string.Empty);
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

        public bool IsDefinitionEditorOpen
        {
            get => _isDefinitionEditorOpen;
            set
            {
                if (SetProperty(ref _isDefinitionEditorOpen, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public bool IsHardwareRuleEditorOpen
        {
            get => _isHardwareRuleEditorOpen;
            set
            {
                if (SetProperty(ref _isHardwareRuleEditorOpen, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value ?? string.Empty);
        }

        public bool CanEditAlarmConfiguration => IsDesignRelatedUser();

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewDefinitionCommand { get; }
        public DelegateCommand EditDefinitionCommand { get; }
        public DelegateCommand SaveDefinitionCommand { get; }
        public DelegateCommand CancelDefinitionEditCommand { get; }
        public DelegateCommand ToggleDefinitionCommand { get; }
        public DelegateCommand NewHardwareRuleCommand { get; }
        public DelegateCommand EditHardwareRuleCommand { get; }
        public DelegateCommand SaveHardwareRuleCommand { get; }
        public DelegateCommand CancelHardwareRuleEditCommand { get; }
        public DelegateCommand ToggleHardwareRuleCommand { get; }

        public async Task EnsureLoadedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await SafeRunAsync(RefreshAsync, "Failed to load alarm configuration.").ConfigureAwait(true);
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await EnsureLoadedAsync().ConfigureAwait(true);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private async Task RefreshAsync()
        {
            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                IsBusy = true;
                StatusText = "Loading alarm configuration...";

                string selectedCode = SelectedDefinition?.Code ?? string.Empty;
                string selectedRuleId = SelectedHardwareRule?.Id ?? string.Empty;
                AlarmConfigSnapshot snapshot = await _configService.LoadAsync(DefinitionKeyword.Trim()).ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    ReplaceCollection(Definitions, snapshot.Definitions.Select(AlarmDefinitionItem.FromModel));
                    ReplaceCollection(HardwareRules, snapshot.TriggerRules.Select(AlarmHardwareRuleItem.FromModel));
                    SelectedDefinition = Definitions.FirstOrDefault(item => item.Code.Equals(selectedCode, StringComparison.OrdinalIgnoreCase)) ?? Definitions.FirstOrDefault();
                    SelectedHardwareRule = HardwareRules.FirstOrDefault(item => item.Id.Equals(selectedRuleId, StringComparison.OrdinalIgnoreCase)) ?? HardwareRules.FirstOrDefault();
                    StatusText = $"Loaded {Definitions.Count} definitions and {HardwareRules.Count} trigger rules.";
                    _initialized = true;
                });
            }
            finally
            {
                IsBusy = false;
                _refreshLock.Release();
            }
        }

        private void NewDefinition()
        {
            EditingDefinition = AlarmDefinitionItem.NewCustom();
            IsDefinitionEditorOpen = true;
            StatusText = "Creating alarm definition.";
        }

        private void EditDefinition()
        {
            if (SelectedDefinition == null)
            {
                return;
            }

            EditingDefinition = AlarmDefinitionItem.FromModel(SelectedDefinition.ToModel());
            IsDefinitionEditorOpen = true;
            StatusText = $"Editing definition {EditingDefinition.Code}.";
        }

        private async Task SaveDefinitionAsync()
        {
            if (EditingDefinition == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingDefinition.Code))
            {
                StatusText = "Alarm code is required.";
                return;
            }

            IsBusy = true;
            try
            {
                string savedCode = EditingDefinition.Code.Trim();
                await _configService.SaveDefinitionAsync(EditingDefinition.ToModel(), ResolveCurrentUser()).ConfigureAwait(false);
                IsDefinitionEditorOpen = false;
                EditingDefinition = null;
                StatusText = $"Saved alarm definition {savedCode}.";
                await RefreshAsync().ConfigureAwait(false);
                RunOnUiThread(() => SelectedDefinition = Definitions.FirstOrDefault(item => item.Code.Equals(savedCode, StringComparison.OrdinalIgnoreCase)) ?? SelectedDefinition);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CancelDefinitionEdit()
        {
            IsDefinitionEditorOpen = false;
            EditingDefinition = null;
            StatusText = "Canceled definition editing.";
        }

        private async Task ToggleDefinitionAsync()
        {
            if (SelectedDefinition == null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                bool next = !SelectedDefinition.Enabled;
                string code = SelectedDefinition.Code;
                await _configService.SetDefinitionEnabledAsync(code, next, ResolveCurrentUser()).ConfigureAwait(false);
                StatusText = $"{code} {(next ? "enabled" : "disabled")}.";
                await RefreshAsync().ConfigureAwait(false);
                RunOnUiThread(() => SelectedDefinition = Definitions.FirstOrDefault(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? SelectedDefinition);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void NewHardwareRule()
        {
            EditingHardwareRule = AlarmHardwareRuleItem.NewRule();
            if (SelectedDefinition != null)
            {
                EditingHardwareRule.DefinitionCode = SelectedDefinition.Code;
                EditingHardwareRule.Name = SelectedDefinition.Name;
                EditingHardwareRule.SourceType = SelectedDefinition.SourceType;
            }

            IsHardwareRuleEditorOpen = true;
            StatusText = "Creating trigger rule.";
        }

        private void EditHardwareRule()
        {
            if (SelectedHardwareRule == null)
            {
                return;
            }

            EditingHardwareRule = AlarmHardwareRuleItem.FromModel(SelectedHardwareRule.ToModel());
            IsHardwareRuleEditorOpen = true;
            StatusText = "Editing trigger rule.";
        }

        private async Task SaveHardwareRuleAsync()
        {
            if (EditingHardwareRule == null)
            {
                return;
            }

            if (EditingHardwareRule.IsSystem)
            {
                StatusText = "System trigger rules cannot be modified.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingHardwareRule.DefinitionCode))
            {
                StatusText = "Trigger rule definition code is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingHardwareRule.Name))
            {
                StatusText = "Trigger rule name is required.";
                return;
            }

            if (RequiresTriggerField(EditingHardwareRule.TriggerKind) && string.IsNullOrWhiteSpace(EditingHardwareRule.TriggerField))
            {
                StatusText = "Trigger field is required.";
                return;
            }

            IsBusy = true;
            try
            {
                string savedId = EditingHardwareRule.Id;
                await _configService.SaveTriggerRuleAsync(EditingHardwareRule.ToModel(), ResolveCurrentUser()).ConfigureAwait(false);
                IsHardwareRuleEditorOpen = false;
                EditingHardwareRule = null;
                StatusText = "Saved trigger rule.";
                await RefreshAsync().ConfigureAwait(false);
                RunOnUiThread(() => SelectedHardwareRule = HardwareRules.FirstOrDefault(item => item.Id.Equals(savedId, StringComparison.OrdinalIgnoreCase)) ?? SelectedHardwareRule);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CancelHardwareRuleEdit()
        {
            IsHardwareRuleEditorOpen = false;
            EditingHardwareRule = null;
            StatusText = "Canceled trigger rule editing.";
        }

        private async Task ToggleHardwareRuleAsync()
        {
            if (SelectedHardwareRule == null)
            {
                return;
            }

            if (SelectedHardwareRule.IsSystem)
            {
                StatusText = "System trigger rules cannot be enabled or disabled.";
                return;
            }

            IsBusy = true;
            try
            {
                bool next = !SelectedHardwareRule.Enabled;
                string id = SelectedHardwareRule.Id;
                await _configService.SetTriggerRuleEnabledAsync(id, next, ResolveCurrentUser()).ConfigureAwait(false);
                StatusText = $"Trigger rule {(next ? "enabled" : "disabled")}.";
                await RefreshAsync().ConfigureAwait(false);
                RunOnUiThread(() => SelectedHardwareRule = HardwareRules.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? SelectedHardwareRule);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRunCommand()
        {
            return !IsBusy;
        }

        private bool CanEditAlarmConfigurationCommand()
        {
            return !IsBusy && CanEditAlarmConfiguration;
        }

        private bool CanEditDefinition()
        {
            return CanEditAlarmConfigurationCommand() && SelectedDefinition != null;
        }

        private bool CanSaveDefinition()
        {
            return CanEditAlarmConfigurationCommand() && EditingDefinition != null && !EditingDefinition.IsSystem;
        }

        private bool CanCloseDefinitionEditor()
        {
            return IsDefinitionEditorOpen && !IsBusy;
        }

        private bool CanToggleDefinition()
        {
            return CanEditAlarmConfigurationCommand() && SelectedDefinition != null && !string.IsNullOrWhiteSpace(SelectedDefinition.Code);
        }

        private bool CanEditHardwareRule()
        {
            return CanEditAlarmConfigurationCommand() && SelectedHardwareRule != null && !SelectedHardwareRule.IsSystem;
        }

        private bool CanSaveHardwareRule()
        {
            return CanEditAlarmConfigurationCommand() && EditingHardwareRule != null && !EditingHardwareRule.IsSystem;
        }

        private bool CanCloseHardwareRuleEditor()
        {
            return IsHardwareRuleEditorOpen && !IsBusy;
        }

        private bool CanToggleHardwareRule()
        {
            return CanEditAlarmConfigurationCommand() && SelectedHardwareRule != null && !SelectedHardwareRule.IsSystem && !string.IsNullOrWhiteSpace(SelectedHardwareRule.Id);
        }

        private void RaiseCommandStates()
        {
            RefreshCommand.RaiseCanExecuteChanged();
            NewDefinitionCommand.RaiseCanExecuteChanged();
            EditDefinitionCommand.RaiseCanExecuteChanged();
            SaveDefinitionCommand.RaiseCanExecuteChanged();
            CancelDefinitionEditCommand.RaiseCanExecuteChanged();
            ToggleDefinitionCommand.RaiseCanExecuteChanged();
            NewHardwareRuleCommand.RaiseCanExecuteChanged();
            EditHardwareRuleCommand.RaiseCanExecuteChanged();
            SaveHardwareRuleCommand.RaiseCanExecuteChanged();
            CancelHardwareRuleEditCommand.RaiseCanExecuteChanged();
            ToggleHardwareRuleCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(CanEditAlarmConfiguration));
        }

        private async Task SafeRunAsync(Func<Task> action, string failureMessage)
        {
            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => StatusText = $"{failureMessage} {ex.Message}");
            }
        }

        private static bool IsDesignRelatedUser()
        {
            CurrentUser? user = PrismProvider.User?.CurUser;
            if (user == null)
            {
                return true;
            }

            if (user.PermissionID > 0 && user.PermissionID <= (int)UserPermission.Admin)
            {
                return true;
            }

            string name = user.UserName ?? string.Empty;
            string[] designTokens = { "设计", "工程", "工艺", "管理员", "Design", "Designer", "Engineer", "Admin" };
            return designTokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string ResolveCurrentUser()
        {
            return string.IsNullOrWhiteSpace(PrismProvider.User?.CurUser?.UserName)
                ? "System"
                : PrismProvider.User.CurUser.UserName.Trim();
        }

        private static bool RequiresTriggerField(AlarmTriggerKind triggerKind)
        {
            return triggerKind == AlarmTriggerKind.ErrorCode ||
                   triggerKind == AlarmTriggerKind.Data;
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> values)
        {
            target.Clear();
            foreach (T value in values)
            {
                target.Add(value);
            }
        }

        private static void RunOnUiThread(Action action)
        {
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            Application.Current.Dispatcher.Invoke(action);
        }
    }
}

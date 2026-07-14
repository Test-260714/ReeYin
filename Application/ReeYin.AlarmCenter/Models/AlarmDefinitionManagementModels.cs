#nullable enable
using Prism.Mvvm;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin.AlarmCenter.Models
{
    public sealed class AlarmSeverityOption
    {
        public AlarmSeverity Value { get; init; }

        public string Label { get; init; } = string.Empty;
    }


    public sealed class AlarmPopupModeOption
    {
        public AlarmPopupMode Value { get; init; }

        public string Label { get; init; } = string.Empty;
    }

    public sealed class AlarmHardwareRuleTriggerKindOption
    {
        public AlarmTriggerKind Value { get; init; }

        public string Label { get; init; } = string.Empty;
    }

    public sealed class AlarmHardwareRuleOperatorOption
    {
        public AlarmRuleOperator Value { get; init; }

        public string Label { get; init; } = string.Empty;
    }

    public sealed class AlarmHardwareRuleClearKindOption
    {
        public AlarmClearMode Value { get; init; }

        public string Label { get; init; } = string.Empty;
    }

    public sealed class AlarmDefinitionItem : BindableBase
    {
        private string _id = string.Empty;
        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _sourceType = string.Empty;
        private string _defaultSource = string.Empty;
        private string _defaultLocation = string.Empty;
        private AlarmSeverity _severity = AlarmSeverity.Warning;
        private bool _needAcknowledge = true;
        private AlarmPopupMode _popupMode = AlarmPopupMode.Growl;
        private int _popupThrottleSeconds = 3;
        private bool _allowManualClear = true;
        private bool _autoClearOnRecovery = true;
        private int _debounceMilliseconds;
        private int _throttleSeconds = 1;
        private AlarmAcknowledgeResetMode _acknowledgeResetMode = AlarmAcknowledgeResetMode.OnSeverityIncrease;
        private IDictionary<string, object?> _extraTemplate = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private bool _enabled = true;
        private bool _isSystem;
        private string _suggestedAction = string.Empty;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        public string Id { get => _id; set => SetProperty(ref _id, value ?? string.Empty); }
        public string Code { get => _code; set => SetProperty(ref _code, value ?? string.Empty); }
        public string Name { get => _name; set => SetProperty(ref _name, value ?? string.Empty); }
        public string Category { get => _category; set => SetProperty(ref _category, value ?? string.Empty); }
        public string SourceType { get => _sourceType; set => SetProperty(ref _sourceType, value ?? string.Empty); }
        public string DefaultSource { get => _defaultSource; set => SetProperty(ref _defaultSource, value ?? string.Empty); }
        public string DefaultLocation { get => _defaultLocation; set => SetProperty(ref _defaultLocation, value ?? string.Empty); }
        public AlarmSeverity Severity { get => _severity; set => SetProperty(ref _severity, value); }
        public bool NeedAcknowledge { get => _needAcknowledge; set => SetProperty(ref _needAcknowledge, value); }
        public AlarmPopupMode PopupMode
        {
            get => _popupMode;
            set
            {
                if (SetProperty(ref _popupMode, value))
                {
                    RaisePropertyChanged(nameof(PopupModeText));
                }
            }
        }
        public int PopupThrottleSeconds { get => _popupThrottleSeconds; set => SetProperty(ref _popupThrottleSeconds, Math.Max(0, value)); }
        public bool AllowManualClear { get => _allowManualClear; set => SetProperty(ref _allowManualClear, value); }
        public bool AutoClearOnRecovery { get => _autoClearOnRecovery; set => SetProperty(ref _autoClearOnRecovery, value); }
        public int DebounceMilliseconds { get => _debounceMilliseconds; set => SetProperty(ref _debounceMilliseconds, Math.Max(0, value)); }
        public int ThrottleSeconds { get => _throttleSeconds; set => SetProperty(ref _throttleSeconds, Math.Max(0, value)); }
        public AlarmAcknowledgeResetMode AcknowledgeResetMode { get => _acknowledgeResetMode; set => SetProperty(ref _acknowledgeResetMode, value); }
        public IDictionary<string, object?> ExtraTemplate { get => _extraTemplate; set => SetProperty(ref _extraTemplate, new Dictionary<string, object?>(value ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase)); }
        public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
        public bool IsSystem { get => _isSystem; set => SetProperty(ref _isSystem, value); }
        public string SuggestedAction { get => _suggestedAction; set => SetProperty(ref _suggestedAction, value ?? string.Empty); }
        public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
        public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

        public string SeverityText => AlarmWorkbenchPalette.GetSeverityText(Severity);
        public string PopupModeText => GetPopupModeText(PopupMode);
        public string StatusText => Enabled ? "启用" : "停用";
        public string SystemText => IsSystem ? "系统" : "自定义";

        public static AlarmDefinitionItem NewCustom()
        {
            return new AlarmDefinitionItem
            {
                Id = Guid.NewGuid().ToString("N"),
                SourceType = "Software",
                Category = "Software",
                Severity = AlarmSeverity.Warning,
                NeedAcknowledge = false,
                PopupMode = AlarmPopupMode.Growl,
                PopupThrottleSeconds = 3,
                AllowManualClear = true,
                AutoClearOnRecovery = true,
                ThrottleSeconds = 1,
                Enabled = true
            };
        }

        public static AlarmDefinitionItem FromModel(AlarmDefinition model)
        {
            return new AlarmDefinitionItem
            {
                Id = string.IsNullOrWhiteSpace(model.Id) ? (string.IsNullOrWhiteSpace(model.Code) ? Guid.NewGuid().ToString("N") : model.Code.Trim().Replace(".", "_")) : model.Id,
                Code = model.Code,
                Name = model.Name,
                Category = model.Category,
                SourceType = ToSourceType(model.SourceKind),
                DefaultSource = model.DefaultSource,
                DefaultLocation = model.DefaultLocation,
                Severity = model.Severity,
                NeedAcknowledge = model.NeedAcknowledge,
                PopupMode = model.PopupMode,
                PopupThrottleSeconds = model.PopupThrottleSeconds,
                AllowManualClear = model.AllowManualClear,
                AutoClearOnRecovery = model.AutoClearOnRecovery,
                DebounceMilliseconds = model.DebounceMilliseconds,
                ThrottleSeconds = model.ThrottleSeconds,
                AcknowledgeResetMode = model.AcknowledgeResetMode,
                ExtraTemplate = model.ExtraTemplate,
                Enabled = model.Enabled,
                IsSystem = model.IsSystem,
                SuggestedAction = model.SuggestedAction,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        public AlarmDefinition ToModel()
        {
            return new AlarmDefinition
            {
                Id = Id,
                Code = Code.Trim(),
                Name = string.IsNullOrWhiteSpace(Name) ? Code.Trim() : Name.Trim(),
                Category = Category.Trim(),
                SourceKind = ParseSourceKind(SourceType),
                DefaultSource = DefaultSource.Trim(),
                DefaultLocation = DefaultLocation.Trim(),
                Severity = Severity,
                NeedAcknowledge = NeedAcknowledge,
                AllowManualClear = AllowManualClear,
                AutoClearOnRecovery = AutoClearOnRecovery,
                DebounceMilliseconds = DebounceMilliseconds,
                ThrottleSeconds = ThrottleSeconds,
                AcknowledgeResetMode = AcknowledgeResetMode,
                PopupMode = PopupMode,
                PopupThrottleSeconds = Math.Max(0, PopupThrottleSeconds),
                SuggestedAction = SuggestedAction.Trim(),
                Enabled = Enabled,
                IsSystem = IsSystem,
                ExtraTemplate = new Dictionary<string, object?>(ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }

        public static string GetPopupModeText(AlarmPopupMode popupMode)
        {
            return popupMode switch
            {
                AlarmPopupMode.Modal => "\u786e\u8ba4\u5f39\u7a97",
                AlarmPopupMode.Growl => "Growl \u63d0\u793a",
                _ => "\u4e0d\u63d0\u793a"
            };
        }

        internal static AlarmSourceKind ParseSourceKind(string? sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType))
            {
                return AlarmSourceKind.Unknown;
            }

            string value = sourceType.Trim();
            if (value.Equals("Software", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Software;
            }

            if (value.Equals("PLC", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Plc;
            }

            if (value.Equals("MotionCard", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.MotionCard;
            }

            if (value.Equals("Sensor", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Sensor;
            }

            if (value.Equals("Camera", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.Camera;
            }

            if (value.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return AlarmSourceKind.System;
            }

            return AlarmSourceKind.Hardware;
        }

        internal static string ToSourceType(AlarmSourceKind sourceKind)
        {
            return sourceKind switch
            {
                AlarmSourceKind.Software => "Software",
                AlarmSourceKind.Plc => "PLC",
                AlarmSourceKind.MotionCard => "MotionCard",
                AlarmSourceKind.Sensor => "Sensor",
                AlarmSourceKind.Camera => "Camera",
                AlarmSourceKind.System => "System",
                AlarmSourceKind.Hardware => "Hardware",
                _ => string.Empty
            };
        }
    }

    public sealed class AlarmHardwareRuleItem : BindableBase
    {
        private string _id = string.Empty;
        private string _definitionCode = string.Empty;
        private string _name = string.Empty;
        private string _sourceType = string.Empty;
        private string _sourcePattern = string.Empty;
        private string _locationPattern = string.Empty;
        private AlarmTriggerKind _triggerKind = AlarmTriggerKind.State;
        private string _triggerField = string.Empty;
        private AlarmRuleOperator _operator = AlarmRuleOperator.Equals;
        private string _triggerValue = string.Empty;
        private AlarmClearMode _clearKind = AlarmClearMode.StateRecovery;
        private string _clearValue = string.Empty;
        private int _debounceMilliseconds;
        private int _throttleSeconds = 1;
        private bool _latchMode;
        private bool _enabled = true;
        private bool _isSystem;
        private int _priority = 100;
        private IDictionary<string, object?> _extraTemplate = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        public string Id { get => _id; set => SetProperty(ref _id, value ?? string.Empty); }
        public string DefinitionCode { get => _definitionCode; set => SetProperty(ref _definitionCode, value ?? string.Empty); }
        public string Name { get => _name; set => SetProperty(ref _name, value ?? string.Empty); }
        public string SourceType { get => _sourceType; set => SetProperty(ref _sourceType, value ?? string.Empty); }
        public string SourcePattern { get => _sourcePattern; set => SetProperty(ref _sourcePattern, value ?? string.Empty); }
        public string LocationPattern { get => _locationPattern; set => SetProperty(ref _locationPattern, value ?? string.Empty); }
        public AlarmTriggerKind TriggerKind
        {
            get => _triggerKind;
            set
            {
                if (SetProperty(ref _triggerKind, value))
                {
                    RaisePropertyChanged(nameof(TriggerSummary));
                }
            }
        }

        public string TriggerField
        {
            get => _triggerField;
            set
            {
                if (SetProperty(ref _triggerField, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(TriggerSummary));
                }
            }
        }

        public AlarmRuleOperator Operator
        {
            get => _operator;
            set
            {
                if (SetProperty(ref _operator, value))
                {
                    RaisePropertyChanged(nameof(TriggerSummary));
                }
            }
        }

        public string TriggerValue
        {
            get => _triggerValue;
            set
            {
                if (SetProperty(ref _triggerValue, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(TriggerSummary));
                }
            }
        }
        public AlarmClearMode ClearKind { get => _clearKind; set => SetProperty(ref _clearKind, value); }
        public string ClearValue { get => _clearValue; set => SetProperty(ref _clearValue, value ?? string.Empty); }
        public int DebounceMilliseconds { get => _debounceMilliseconds; set => SetProperty(ref _debounceMilliseconds, Math.Max(0, value)); }
        public int ThrottleSeconds { get => _throttleSeconds; set => SetProperty(ref _throttleSeconds, Math.Max(0, value)); }
        public bool LatchMode { get => _latchMode; set => SetProperty(ref _latchMode, value); }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (SetProperty(ref _enabled, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsSystem
        {
            get => _isSystem;
            set
            {
                if (SetProperty(ref _isSystem, value))
                {
                    RaisePropertyChanged(nameof(SystemText));
                }
            }
        }
        public int Priority { get => _priority; set => SetProperty(ref _priority, value); }
        public IDictionary<string, object?> ExtraTemplate { get => _extraTemplate; set => SetProperty(ref _extraTemplate, CopyExtraTemplate(value)); }
        public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
        public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

        public string StatusText => Enabled ? "启用" : "停用";
        public string SystemText => IsSystem ? "系统" : "自定义";
        public string TriggerSummary
        {
            get
            {
                string field = string.IsNullOrWhiteSpace(TriggerField) ? GetTriggerKindText(TriggerKind) : TriggerField.Trim();
                return $"{GetTriggerKindText(TriggerKind)}：{field} {GetOperatorText(Operator)} {TriggerValue}".Trim();
            }
        }

        public static string GetTriggerKindText(AlarmTriggerKind triggerKind)
        {
            return triggerKind switch
            {
                AlarmTriggerKind.State => "状态",
                AlarmTriggerKind.ErrorCode => "错误码",
                AlarmTriggerKind.Data => "扩展数据",
                AlarmTriggerKind.Heartbeat => "心跳",
                _ => triggerKind.ToString()
            };
        }

        public static string GetOperatorText(AlarmRuleOperator ruleOperator)
        {
            return ruleOperator switch
            {
                AlarmRuleOperator.Equals => "等于",
                AlarmRuleOperator.NotEquals => "不等于",
                AlarmRuleOperator.GreaterThan => "大于",
                AlarmRuleOperator.GreaterThanOrEqual => "大于等于",
                AlarmRuleOperator.LessThan => "小于",
                AlarmRuleOperator.LessThanOrEqual => "小于等于",
                AlarmRuleOperator.Contains => "包含",
                AlarmRuleOperator.BitHasFlag => "位包含",
                _ => ruleOperator.ToString()
            };
        }

        public static string GetClearKindText(AlarmClearMode clearKind)
        {
            return clearKind switch
            {
                AlarmClearMode.StateRecovery => "状态恢复",
                AlarmClearMode.FieldRecovery => "字段恢复",
                AlarmClearMode.ManualOnly => "仅手动",
                _ => clearKind.ToString()
            };
        }

        public static AlarmHardwareRuleItem NewRule()
        {
            return new AlarmHardwareRuleItem
            {
                Id = Guid.NewGuid().ToString("N"),
                SourceType = "Hardware",
                TriggerKind = AlarmTriggerKind.State,
                TriggerField = "State",
                Operator = AlarmRuleOperator.Equals,
                ClearKind = AlarmClearMode.StateRecovery,
                ThrottleSeconds = 1,
                Enabled = true,
                Priority = 100
            };
        }

        public static AlarmHardwareRuleItem FromModel(AlarmTriggerRule model)
        {
            return new AlarmHardwareRuleItem
            {
                Id = model.Id,
                DefinitionCode = model.DefinitionCode,
                Name = model.Name,
                SourceType = AlarmDefinitionItem.ToSourceType(model.SourceKind),
                SourcePattern = model.SourcePattern,
                LocationPattern = model.LocationPattern,
                TriggerKind = model.TriggerKind,
                TriggerField = model.TriggerField,
                Operator = model.Operator,
                TriggerValue = model.TriggerValue,
                ClearKind = model.ClearMode,
                ClearValue = model.ClearValue,
                DebounceMilliseconds = model.DebounceMilliseconds,
                ThrottleSeconds = model.ThrottleSeconds,
                LatchMode = model.LatchMode,
                Enabled = model.Enabled,
                IsSystem = model.IsSystem,
                Priority = model.Priority,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public AlarmTriggerRule ToModel()
        {
            return new AlarmTriggerRule
            {
                Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("N") : Id.Trim(),
                DefinitionCode = DefinitionCode.Trim(),
                Name = string.IsNullOrWhiteSpace(Name) ? DefinitionCode.Trim() : Name.Trim(),
                SourceKind = AlarmDefinitionItem.ParseSourceKind(SourceType),
                SourcePattern = SourcePattern.Trim(),
                LocationPattern = LocationPattern.Trim(),
                TriggerKind = TriggerKind,
                TriggerField = TriggerField.Trim(),
                Operator = Operator,
                TriggerValue = TriggerValue.Trim(),
                ClearMode = ClearKind,
                ClearValue = ClearValue.Trim(),
                DebounceMilliseconds = Math.Max(0, DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, ThrottleSeconds),
                LatchMode = LatchMode,
                Enabled = Enabled,
                IsSystem = IsSystem,
                Priority = Priority
            };
        }

        private static IDictionary<string, object?> CopyExtraTemplate(IDictionary<string, object?>? source)
        {
            return source == null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);
        }
    }

}

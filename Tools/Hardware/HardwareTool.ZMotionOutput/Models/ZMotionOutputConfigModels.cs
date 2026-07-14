using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Services.Project;
using System;

namespace HardwareTool.ZMotionOutput.Models
{
    [Serializable]
    public class ZMotionOutputPointConfig : BindableBase
    {
        public ZMotionOutputPointConfig()
        {
        }

        public ZMotionOutputPointConfig(string id, string name, int port)
            : this(id, name, port, "None")
        {
        }

        public ZMotionOutputPointConfig(string id, string name, int port, string roleKey)
            : this(id, name, port, roleKey, true, true, "AutoReset")
        {
        }

        public ZMotionOutputPointConfig(string id, string name, int port, string roleKey, bool isEnabled, bool activeLevel, string resetPolicyKey)
        {
            Id = id;
            Name = name;
            Port = port;
            RoleKey = roleKey;
            IsEnabled = isEnabled;
            ActiveLevel = activeLevel;
            ResetPolicyKey = resetPolicyKey;
        }

        private string _id = Guid.NewGuid().ToString("N");
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
        }

        private string _name = "OUT点";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, Math.Max(0, value));
        }

        private string _roleKey = "None";
        public string RoleKey
        {
            get => _roleKey;
            set => SetProperty(ref _roleKey, string.IsNullOrWhiteSpace(value) ? "None" : value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private bool _activeLevel = true;
        public bool ActiveLevel
        {
            get => _activeLevel;
            set => SetProperty(ref _activeLevel, value);
        }

        private string _resetPolicyKey = "AutoReset";
        public string ResetPolicyKey
        {
            get => _resetPolicyKey;
            set => SetProperty(ref _resetPolicyKey, string.IsNullOrWhiteSpace(value) ? "AutoReset" : value);
        }

        [JsonIgnore]
        private bool _currentState;

        [JsonIgnore]
        public bool CurrentState
        {
            get => _currentState;
            set => SetProperty(ref _currentState, value);
        }
    }

    [Serializable]
    public class ZMotionOutputSourceConfig : BindableBase
    {
        public ZMotionOutputSourceConfig()
        {
        }

        public ZMotionOutputSourceConfig(string id, string name, string roleKey, string resolverKey)
        {
            Id = id;
            Name = name;
            RoleKey = roleKey;
            ResolverKey = resolverKey;
        }

        private string _id = Guid.NewGuid().ToString("N");
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
        }

        private string _name = "输入源";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _roleKey = "Custom";
        public string RoleKey
        {
            get => _roleKey;
            set => SetProperty(ref _roleKey, string.IsNullOrWhiteSpace(value) ? "Custom" : value);
        }

        private string _resolverKey = "Auto";
        public string ResolverKey
        {
            get => _resolverKey;
            set => SetProperty(ref _resolverKey, string.IsNullOrWhiteSpace(value) ? "Auto" : value);
        }

        private TransmitParam _bindingParam = new();
        public TransmitParam BindingParam
        {
            get => _bindingParam;
            set => SetProperty(ref _bindingParam, value ?? new TransmitParam());
        }

        private bool _manualValue;
        public bool ManualValue
        {
            get => _manualValue;
            set => SetProperty(ref _manualValue, value);
        }
    }

    [Serializable]
    public class ZMotionOutputRuleConfig : BindableBase
    {
        public ZMotionOutputRuleConfig()
        {
        }

        public ZMotionOutputRuleConfig(string id, string name, string conditionKey, string sourceId, string targetPointId, string actionKey, int priority)
        {
            Id = id;
            Name = name;
            ConditionKey = conditionKey;
            SourceId = sourceId;
            TargetPointId = targetPointId;
            ActionKey = actionKey;
            Priority = priority;
        }

        private string _id = Guid.NewGuid().ToString("N");
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
        }

        private string _name = "输出规则";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _conditionKey = "SourceNg";
        public string ConditionKey
        {
            get => _conditionKey;
            set => SetProperty(ref _conditionKey, string.IsNullOrWhiteSpace(value) ? "SourceNg" : value);
        }

        private string _sourceId = string.Empty;
        public string SourceId
        {
            get => _sourceId;
            set => SetProperty(ref _sourceId, value ?? string.Empty);
        }

        private TransmitParam _bindingParam = new();
        public TransmitParam BindingParam
        {
            get => _bindingParam;
            set => SetProperty(ref _bindingParam, value ?? new TransmitParam());
        }

        private string _resolverKey = "Auto";
        public string ResolverKey
        {
            get => _resolverKey;
            set => SetProperty(ref _resolverKey, string.IsNullOrWhiteSpace(value) ? "Auto" : value);
        }

        private bool _manualValue;
        public bool ManualValue
        {
            get => _manualValue;
            set => SetProperty(ref _manualValue, value);
        }

        private string _targetPointId = string.Empty;
        public string TargetPointId
        {
            get => _targetPointId;
            set => SetProperty(ref _targetPointId, value ?? string.Empty);
        }

        private bool _useDirectPort;
        public bool UseDirectPort
        {
            get => _useDirectPort;
            set => SetProperty(ref _useDirectPort, value);
        }

        private int _directPort;
        public int DirectPort
        {
            get => _directPort;
            set => SetProperty(ref _directPort, Math.Max(0, value));
        }

        private bool _activeLevel = true;
        public bool ActiveLevel
        {
            get => _activeLevel;
            set => SetProperty(ref _activeLevel, value);
        }

        private string _resetPolicyKey = "AutoReset";
        public string ResetPolicyKey
        {
            get => _resetPolicyKey;
            set => SetProperty(ref _resetPolicyKey, string.IsNullOrWhiteSpace(value) ? "AutoReset" : value);
        }

        private string _actionKey = "SetActive";
        public string ActionKey
        {
            get => _actionKey;
            set => SetProperty(ref _actionKey, string.IsNullOrWhiteSpace(value) ? "SetActive" : value);
        }

        private int _priority = 100;
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }
    }

    [Serializable]
    public class ZMotionIoStatusItem : BindableBase
    {
        public ZMotionIoStatusItem()
        {
        }

        public ZMotionIoStatusItem(bool isInput, int port, string name)
        {
            IsInput = isInput;
            Port = port;
            Name = name;
        }

        private bool _isInput;
        public bool IsInput
        {
            get => _isInput;
            set
            {
                if (SetProperty(ref _isInput, value))
                    RaisePropertyChanged(nameof(DirectionName));
            }
        }

        [JsonIgnore]
        public string DirectionName => IsInput ? "IN" : "OUT";

        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, Math.Max(0, value));
        }

        private string _name = "IO";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        private bool _state;
        public bool State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        private bool _isReadOk;
        public bool IsReadOk
        {
            get => _isReadOk;
            set => SetProperty(ref _isReadOk, value);
        }

        private string _lastRefreshText = string.Empty;
        public string LastRefreshText
        {
            get => _lastRefreshText;
            set => SetProperty(ref _lastRefreshText, value ?? string.Empty);
        }
    }

}
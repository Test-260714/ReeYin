using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HardwareTool.ZMotionOutput.Models
{
    [Serializable]
    public partial class ZMotionNgOutputModel : ModelParamBase
    {
        private const string DefaultLeftNgOutputId = "default-left-ng";
        private const string DefaultRightNgOutputId = "default-right-ng";
        private const string DefaultGreenOutputId = "default-green";
        private const string DefaultYellowOutputId = "default-yellow";
        private const string DefaultRedOutputId = "default-red";
        private const string DefaultLeftSourceId = "default-source-left-ng";
        private const string DefaultRightSourceId = "default-source-right-ng";
        private const string DefaultTotalSourceId = "default-source-total-ng";
        private const string DefaultManualYellowSourceId = "default-source-manual-yellow";
        private const string OutputRoleNone = "None";
        private const string OutputRoleLeftNg = "LeftNg";
        private const string OutputRoleRightNg = "RightNg";
        private const string OutputRoleTotalNg = "TotalNg";
        private const string OutputRoleGreen = "Green";
        private const string OutputRoleYellow = "Yellow";
        private const string OutputRoleRed = "Red";
        private const string OutputRoleBuzzer = "Buzzer";
        private const string OutputRoleCustom = "Custom";
        private const string ResetPolicyAutoReset = "AutoReset";
        private const string ResetPolicyHold = "Hold";
        private const string ResetPolicyManual = "Manual";
        private const string SourceRoleNone = "None";
        private const string SourceRoleLeftNg = "LeftNg";
        private const string SourceRoleRightNg = "RightNg";
        private const string SourceRoleTotalNg = "TotalNg";
        private const string SourceRoleManualYellow = "ManualYellow";
        private const string SourceRoleCustom = "Custom";
        private const string SourceResolverAuto = "Auto";
        private const string SourceResolverBool = "Bool";
        private const string SourceResolverOkNgText = "OkNgText";
        private const string SourceResolverDefectList = "DefectList";
        private const string SourceResolverNumberThreshold = "NumberThreshold";
        private const string RuleConditionSourceNg = "SourceNg";
        private const string RuleConditionAnyNg = "AnyNg";
        private const string RuleConditionAllOk = "AllOk";
        private const string RuleConditionManualYellow = "ManualYellow";
        private const string RuleActionSetActive = "SetActive";
        private const string RuleActionSetInactive = "SetInactive";

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        [JsonIgnore]
        private ObservableCollection<ControlCardBase> _models = new();

        [JsonIgnore]
        public ObservableCollection<ControlCardBase> Models
        {
            get => _models;
            set => SetProperty(ref _models, value ?? new ObservableCollection<ControlCardBase>());
        }

        [JsonIgnore]
        private ControlCardBase? _controlCard;

        [JsonIgnore]
        public ControlCardBase? ControlCard
        {
            get => _controlCard;
            set => SetProperty(ref _controlCard, value);
        }

        [JsonIgnore]
        public ObservableCollection<int> AvailableOutputPorts { get; } = new(Enumerable.Range(0, 32));

        [JsonIgnore]
        public ObservableCollection<ZMotionOutputRoleOption> OutputRoleOptions { get; } = new()
        {
            new ZMotionOutputRoleOption(OutputRoleNone, "备用"),
            new ZMotionOutputRoleOption(OutputRoleLeftNg, "左路NG"),
            new ZMotionOutputRoleOption(OutputRoleRightNg, "右路NG"),
            new ZMotionOutputRoleOption(OutputRoleTotalNg, "总NG"),
            new ZMotionOutputRoleOption(OutputRoleGreen, "绿灯"),
            new ZMotionOutputRoleOption(OutputRoleYellow, "黄灯"),
            new ZMotionOutputRoleOption(OutputRoleRed, "红灯"),
            new ZMotionOutputRoleOption(OutputRoleBuzzer, "蜂鸣器"),
            new ZMotionOutputRoleOption(OutputRoleCustom, "自定义"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionBoolOption> ActiveLevelOptions { get; } = new()
        {
            new ZMotionBoolOption(true, "高电平"),
            new ZMotionBoolOption(false, "低电平"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionResetPolicyOption> ResetPolicyOptions { get; } = new()
        {
            new ZMotionResetPolicyOption(ResetPolicyAutoReset, "OK复位"),
            new ZMotionResetPolicyOption(ResetPolicyHold, "保持"),
            new ZMotionResetPolicyOption(ResetPolicyManual, "手动"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionSourceRoleOption> SourceRoleOptions { get; } = new()
        {
            new ZMotionSourceRoleOption(SourceRoleNone, "不参与"),
            new ZMotionSourceRoleOption(SourceRoleLeftNg, "左路NG"),
            new ZMotionSourceRoleOption(SourceRoleRightNg, "右路NG"),
            new ZMotionSourceRoleOption(SourceRoleTotalNg, "总NG"),
            new ZMotionSourceRoleOption(SourceRoleManualYellow, "手动黄灯"),
            new ZMotionSourceRoleOption(SourceRoleCustom, "自定义"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionSourceResolverOption> SourceResolverOptions { get; } = new()
        {
            new ZMotionSourceResolverOption(SourceResolverAuto, "自动"),
            new ZMotionSourceResolverOption(SourceResolverBool, "布尔/开关"),
            new ZMotionSourceResolverOption(SourceResolverOkNgText, "OK/NG文本"),
            new ZMotionSourceResolverOption(SourceResolverDefectList, "缺陷列表"),
            new ZMotionSourceResolverOption(SourceResolverNumberThreshold, "数值>0"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionRuleConditionOption> RuleConditionOptions { get; } = new()
        {
            new ZMotionRuleConditionOption(RuleConditionSourceNg, "输入源NG"),
            new ZMotionRuleConditionOption(RuleConditionAnyNg, "任一路NG"),
            new ZMotionRuleConditionOption(RuleConditionAllOk, "全部OK"),
            new ZMotionRuleConditionOption(RuleConditionManualYellow, "手动黄灯"),
        };

        [JsonIgnore]
        public ObservableCollection<ZMotionRuleActionOption> RuleActionOptions { get; } = new()
        {
            new ZMotionRuleActionOption(RuleActionSetActive, "置有效"),
            new ZMotionRuleActionOption(RuleActionSetInactive, "置无效"),
        };

        private ObservableCollection<ZMotionOutputPointConfig> _outputPointConfigs = new();
        public ObservableCollection<ZMotionOutputPointConfig> OutputPointConfigs
        {
            get => _outputPointConfigs;
            set => SetProperty(ref _outputPointConfigs, value ?? new ObservableCollection<ZMotionOutputPointConfig>());
        }

        public int OutputPointConfigVersion { get; set; }

        private ObservableCollection<ZMotionOutputSourceConfig> _inputSourceConfigs = new();
        public ObservableCollection<ZMotionOutputSourceConfig> InputSourceConfigs
        {
            get => _inputSourceConfigs;
            set => SetProperty(ref _inputSourceConfigs, value ?? new ObservableCollection<ZMotionOutputSourceConfig>());
        }

        private ObservableCollection<ZMotionOutputRuleConfig> _outputRuleConfigs = new();
        public ObservableCollection<ZMotionOutputRuleConfig> OutputRuleConfigs
        {
            get => _outputRuleConfigs;
            set => SetProperty(ref _outputRuleConfigs, value ?? new ObservableCollection<ZMotionOutputRuleConfig>());
        }

        public int GenericOutputConfigVersion { get; set; }

        [JsonIgnore]
        private ObservableCollection<ZMotionIoStatusItem> _ioStatusItems = new();

        [JsonIgnore]
        public ObservableCollection<ZMotionIoStatusItem> IoStatusItems
        {
            get => _ioStatusItems;
            set => SetProperty(ref _ioStatusItems, value ?? new ObservableCollection<ZMotionIoStatusItem>());
        }

        [JsonIgnore]
        private ZMotionOutputPointConfig? _selectedOutputPoint;

        [JsonIgnore]
        public ZMotionOutputPointConfig? SelectedOutputPoint
        {
            get => _selectedOutputPoint;
            set => SetProperty(ref _selectedOutputPoint, value);
        }

        [JsonIgnore]
        private ZMotionOutputSourceConfig? _selectedInputSource;

        [JsonIgnore]
        public ZMotionOutputSourceConfig? SelectedInputSource
        {
            get => _selectedInputSource;
            set => SetProperty(ref _selectedInputSource, value);
        }

        [JsonIgnore]
        private ZMotionOutputRuleConfig? _selectedOutputRule;

        [JsonIgnore]
        public ZMotionOutputRuleConfig? SelectedOutputRule
        {
            get => _selectedOutputRule;
            set => SetProperty(ref _selectedOutputRule, value);
        }

        [JsonIgnore]
        private ZMotionIoStatusItem? _selectedIoStatusItem;

        [JsonIgnore]
        public ZMotionIoStatusItem? SelectedIoStatusItem
        {
            get => _selectedIoStatusItem;
            set => SetProperty(ref _selectedIoStatusItem, value);
        }

        public string SltModelName { get; set; } = string.Empty;

        private string _leftNgOutputPointId = DefaultLeftNgOutputId;
        public string LeftNgOutputPointId
        {
            get => _leftNgOutputPointId;
            set => SetProperty(ref _leftNgOutputPointId, value ?? string.Empty);
        }

        private string _rightNgOutputPointId = DefaultRightNgOutputId;
        public string RightNgOutputPointId
        {
            get => _rightNgOutputPointId;
            set => SetProperty(ref _rightNgOutputPointId, value ?? string.Empty);
        }

        private string _greenOutputPointId = DefaultGreenOutputId;
        public string GreenOutputPointId
        {
            get => _greenOutputPointId;
            set => SetProperty(ref _greenOutputPointId, value ?? string.Empty);
        }

        private string _yellowOutputPointId = DefaultYellowOutputId;
        public string YellowOutputPointId
        {
            get => _yellowOutputPointId;
            set => SetProperty(ref _yellowOutputPointId, value ?? string.Empty);
        }

        private string _redOutputPointId = DefaultRedOutputId;
        public string RedOutputPointId
        {
            get => _redOutputPointId;
            set => SetProperty(ref _redOutputPointId, value ?? string.Empty);
        }

        private TransmitParam _xyhdPacketInput = new();

        [InputParam("检测数据包", "推荐绑定DefectOverviewPacket；兼容旧接法，可留空", false)]
        public TransmitParam XYHDPacketInput
        {
            get => _xyhdPacketInput;
            set => SetProperty(ref _xyhdPacketInput, value ?? new TransmitParam());
        }

        private TransmitParam _leftNgInput = new();

        [InputParam("左路NG", "推荐连接XYHD_LeftNg", false)]
        public TransmitParam LeftNgInput
        {
            get => _leftNgInput;
            set => SetProperty(ref _leftNgInput, value ?? new TransmitParam());
        }

        private TransmitParam _rightNgInput = new();

        [InputParam("右路NG", "推荐连接XYHD_RightNg", false)]
        public TransmitParam RightNgInput
        {
            get => _rightNgInput;
            set => SetProperty(ref _rightNgInput, value ?? new TransmitParam());
        }

        private bool _enableOutput = true;
        public bool EnableOutput
        {
            get => _enableOutput;
            set => SetProperty(ref _enableOutput, value);
        }

        private bool _enableNgOutput = true;
        public bool EnableNgOutput
        {
            get => _enableNgOutput;
            set => SetProperty(ref _enableNgOutput, value);
        }

        private bool _enableTowerLight = true;
        public bool EnableTowerLight
        {
            get => _enableTowerLight;
            set => SetProperty(ref _enableTowerLight, value);
        }

        private bool _autoResetOnOk = true;
        public bool AutoResetOnOk
        {
            get => _autoResetOnOk;
            set => SetProperty(ref _autoResetOnOk, value);
        }

        private int _leftOutputPort = 5;
        public int LeftOutputPort
        {
            get => _leftOutputPort;
            set => SetProperty(ref _leftOutputPort, value);
        }

        private int _rightOutputPort = 6;
        public int RightOutputPort
        {
            get => _rightOutputPort;
            set => SetProperty(ref _rightOutputPort, value);
        }

        private int _greenOutputPort = 1;
        public int GreenOutputPort
        {
            get => _greenOutputPort;
            set => SetProperty(ref _greenOutputPort, value);
        }

        private int _yellowOutputPort = 2;
        public int YellowOutputPort
        {
            get => _yellowOutputPort;
            set => SetProperty(ref _yellowOutputPort, value);
        }

        private int _redOutputPort = 3;
        public int RedOutputPort
        {
            get => _redOutputPort;
            set => SetProperty(ref _redOutputPort, value);
        }

        private bool _ngActiveLevel = true;
        public bool NgActiveLevel
        {
            get => _ngActiveLevel;
            set => SetProperty(ref _ngActiveLevel, value);
        }

        private bool _towerActiveLevel = true;
        public bool TowerActiveLevel
        {
            get => _towerActiveLevel;
            set => SetProperty(ref _towerActiveLevel, value);
        }

        private bool _manualLeftNg;
        public bool ManualLeftNg
        {
            get => _manualLeftNg;
            set => SetProperty(ref _manualLeftNg, value);
        }

        private bool _manualRightNg;
        public bool ManualRightNg
        {
            get => _manualRightNg;
            set => SetProperty(ref _manualRightNg, value);
        }

        private bool _manualYellowOn;
        public bool ManualYellowOn
        {
            get => _manualYellowOn;
            set => SetProperty(ref _manualYellowOn, value);
        }

        private bool _leftOutputState;

        [OutputParam("LeftOutputState", "左路OUT状态")]
        public bool LeftOutputState
        {
            get => _leftOutputState;
            set => SetProperty(ref _leftOutputState, value);
        }

        private bool _rightOutputState;

        [OutputParam("RightOutputState", "右路OUT状态")]
        public bool RightOutputState
        {
            get => _rightOutputState;
            set => SetProperty(ref _rightOutputState, value);
        }

        private bool _greenOutputState;

        [OutputParam("GreenOutputState", "绿灯OUT状态")]
        public bool GreenOutputState
        {
            get => _greenOutputState;
            set => SetProperty(ref _greenOutputState, value);
        }

        private bool _yellowOutputState;

        [OutputParam("YellowOutputState", "黄灯OUT状态")]
        public bool YellowOutputState
        {
            get => _yellowOutputState;
            set => SetProperty(ref _yellowOutputState, value);
        }

        private bool _redOutputState;

        [OutputParam("RedOutputState", "红灯OUT状态")]
        public bool RedOutputState
        {
            get => _redOutputState;
            set => SetProperty(ref _redOutputState, value);
        }

        private string _lastMessage = "";

        [OutputParam("LastMessage", "最近输出信息")]
        public string LastMessage
        {
            get => _lastMessage;
            set
            {
                if (SetProperty(ref _lastMessage, value))
                    AddLog(value);
            }
        }

        [JsonIgnore]
        public ObservableCollection<string> LogMessages { get; } = new();

        [JsonIgnore]
        public string LogText => string.Join(Environment.NewLine, LogMessages);

        public ZMotionNgOutputModel()
        {
            RefreshControlCards();
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                    return true;

                if (!base.OnceInit())
                    return false;

                TriggerModuleRun ??= () => ExecuteModule().Result;
                EnsureOutputPointConfig();
                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                LastMessage = $"正运动输出初始化异常：{ex.Message}";
                return false;
            }
        }

        public void EnsureOutputPointConfig()
        {
            OutputPointConfigs ??= new ObservableCollection<ZMotionOutputPointConfig>();

            if (OutputPointConfigs.Count == 0)
            {
                OutputPointConfigs.Add(new ZMotionOutputPointConfig(DefaultLeftNgOutputId, "左路NG", LeftOutputPort, OutputRoleLeftNg, true, NgActiveLevel, ResetPolicyAutoReset));
                OutputPointConfigs.Add(new ZMotionOutputPointConfig(DefaultRightNgOutputId, "右路NG", RightOutputPort, OutputRoleRightNg, true, NgActiveLevel, ResetPolicyAutoReset));
                OutputPointConfigs.Add(new ZMotionOutputPointConfig(DefaultGreenOutputId, "绿灯", GreenOutputPort, OutputRoleGreen, true, TowerActiveLevel, ResetPolicyAutoReset));
                OutputPointConfigs.Add(new ZMotionOutputPointConfig(DefaultYellowOutputId, "黄灯", YellowOutputPort, OutputRoleYellow, true, TowerActiveLevel, ResetPolicyManual));
                OutputPointConfigs.Add(new ZMotionOutputPointConfig(DefaultRedOutputId, "红灯", RedOutputPort, OutputRoleRed, true, TowerActiveLevel, ResetPolicyAutoReset));
            }

            if (OutputPointConfigVersion <= 0)
            {
                ApplyLegacyRole(_leftNgOutputPointId, OutputRoleLeftNg);
                ApplyLegacyRole(_rightNgOutputPointId, OutputRoleRightNg);
                ApplyLegacyRole(_greenOutputPointId, OutputRoleGreen);
                ApplyLegacyRole(_yellowOutputPointId, OutputRoleYellow);
                ApplyLegacyRole(_redOutputPointId, OutputRoleRed);
            }

            if (OutputPointConfigVersion < 2)
            {
                ApplyLegacyPointOptions(OutputRoleLeftNg, EnableNgOutput, NgActiveLevel, AutoResetOnOk ? ResetPolicyAutoReset : ResetPolicyHold);
                ApplyLegacyPointOptions(OutputRoleRightNg, EnableNgOutput, NgActiveLevel, AutoResetOnOk ? ResetPolicyAutoReset : ResetPolicyHold);
                ApplyLegacyPointOptions(OutputRoleGreen, EnableTowerLight, TowerActiveLevel, ResetPolicyAutoReset);
                ApplyLegacyPointOptions(OutputRoleYellow, EnableTowerLight, TowerActiveLevel, ResetPolicyManual);
                ApplyLegacyPointOptions(OutputRoleRed, EnableTowerLight, TowerActiveLevel, ResetPolicyAutoReset);
            }

            OutputPointConfigVersion = 2;
            EnsureGenericOutputConfig();

            SelectedOutputPoint ??= OutputPointConfigs.FirstOrDefault();
            RaisePropertyChanged(nameof(OutputPointConfigs));
        }

        private void EnsureGenericOutputConfig()
        {
            InputSourceConfigs ??= new ObservableCollection<ZMotionOutputSourceConfig>();
            OutputRuleConfigs ??= new ObservableCollection<ZMotionOutputRuleConfig>();

            EnsureDefaultInputSource(DefaultLeftSourceId, "左路NG", SourceRoleLeftNg, LeftNgInput, SourceResolverAuto, ManualLeftNg);
            EnsureDefaultInputSource(DefaultRightSourceId, "右路NG", SourceRoleRightNg, RightNgInput, SourceResolverAuto, ManualRightNg);
            EnsureDefaultInputSource(DefaultTotalSourceId, "检测包/总NG", SourceRoleTotalNg, XYHDPacketInput, SourceResolverAuto, false);
            EnsureDefaultInputSource(DefaultManualYellowSourceId, "手动黄灯", SourceRoleManualYellow, new TransmitParam(), SourceResolverBool, ManualYellowOn);

            SyncLegacyInputsFromDefaultSources();
            NormalizeGenericOutputRules();
            GenericOutputConfigVersion = Math.Max(GenericOutputConfigVersion, 1);
            EnsureIoStatusItems();

            SelectedInputSource ??= InputSourceConfigs.FirstOrDefault();
            SelectedOutputRule ??= OutputRuleConfigs.FirstOrDefault();
            RaisePropertyChanged(nameof(InputSourceConfigs));
            RaisePropertyChanged(nameof(OutputRuleConfigs));
        }

        private void EnsureDefaultInputSource(string id, string name, string roleKey, TransmitParam legacyParam, string resolverKey, bool manualValue)
        {
            var source = InputSourceConfigs.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
            if (source == null)
            {
                source = new ZMotionOutputSourceConfig(id, name, roleKey, resolverKey)
                {
                    BindingParam = HasTransmitBinding(legacyParam) ? legacyParam : new TransmitParam(),
                    ManualValue = manualValue,
                };
                InputSourceConfigs.Add(source);
                return;
            }

            if (string.IsNullOrWhiteSpace(source.Name))
                source.Name = name;

            if (string.IsNullOrWhiteSpace(source.RoleKey) || source.RoleKey == SourceRoleNone)
                source.RoleKey = roleKey;

            if (string.IsNullOrWhiteSpace(source.ResolverKey))
                source.ResolverKey = resolverKey;

            if (!HasTransmitBinding(source.BindingParam) && HasTransmitBinding(legacyParam))
                source.BindingParam = legacyParam;

            if (string.Equals(id, DefaultManualYellowSourceId, StringComparison.Ordinal))
                source.ManualValue = manualValue;
        }

        private void SyncLegacyInputsFromDefaultSources()
        {
            SyncLegacyInputFromSource(DefaultLeftSourceId, value => LeftNgInput = value);
            SyncLegacyInputFromSource(DefaultRightSourceId, value => RightNgInput = value);
            SyncLegacyInputFromSource(DefaultTotalSourceId, value => XYHDPacketInput = value);
        }

        private void SyncLegacyInputFromSource(string sourceId, Action<TransmitParam> apply)
        {
            var source = InputSourceConfigs.FirstOrDefault(item => string.Equals(item.Id, sourceId, StringComparison.Ordinal));
            if (source?.BindingParam == null || !HasTransmitBinding(source.BindingParam))
                return;

            apply(source.BindingParam);
        }

        private static bool IsDefaultInputSource(string? sourceId)
        {
            return string.Equals(sourceId, DefaultLeftSourceId, StringComparison.Ordinal)
                || string.Equals(sourceId, DefaultRightSourceId, StringComparison.Ordinal)
                || string.Equals(sourceId, DefaultTotalSourceId, StringComparison.Ordinal)
                || string.Equals(sourceId, DefaultManualYellowSourceId, StringComparison.Ordinal);
        }

        private static bool HasTransmitBinding(TransmitParam? param)
        {
            if (param == null)
                return false;

            return param.IsLink
                || param.LinkGuid != Guid.Empty
                || param.Resourece != ResoureceType.None
                || !string.IsNullOrWhiteSpace(param.Name)
                || !string.IsNullOrWhiteSpace(param.ParamName);
        }

        private void EnsureIoStatusItems()
        {
            IoStatusItems ??= new ObservableCollection<ZMotionIoStatusItem>();
            EnsureIoStatusItems(false, "OUT", 32);
            EnsureIoStatusItems(true, "IN", 32);
            SelectedIoStatusItem ??= IoStatusItems.FirstOrDefault(item => !item.IsInput);
        }

        private void NormalizeGenericOutputRules()
        {
            foreach (var rule in OutputRuleConfigs ?? Enumerable.Empty<ZMotionOutputRuleConfig>())
            {
                if (rule == null)
                    continue;

                if (string.IsNullOrWhiteSpace(rule.ConditionKey))
                    rule.ConditionKey = RuleConditionSourceNg;

                if (string.IsNullOrWhiteSpace(rule.ActionKey))
                    rule.ActionKey = RuleActionSetActive;

                var source = InputSourceConfigs.FirstOrDefault(item => string.Equals(item.Id, rule.SourceId, StringComparison.Ordinal));
                if (!HasTransmitBinding(rule.BindingParam) && source?.BindingParam != null)
                    rule.BindingParam = source.BindingParam;

                if (string.IsNullOrWhiteSpace(rule.ResolverKey) && source != null)
                    rule.ResolverKey = source.ResolverKey;

                var targetPoint = OutputPointConfigs.FirstOrDefault(point => string.Equals(point.Id, rule.TargetPointId, StringComparison.Ordinal));
                if (!rule.UseDirectPort && targetPoint != null)
                {
                    rule.UseDirectPort = true;
                    rule.DirectPort = NormalizePort(targetPoint.Port);
                    rule.ActiveLevel = targetPoint.ActiveLevel;
                    rule.ResetPolicyKey = targetPoint.ResetPolicyKey;
                }

                if (rule.UseDirectPort)
                    rule.ActionKey = RuleActionSetActive;
            }
        }

        private void EnsureIoStatusItems(bool isInput, string prefix, int count)
        {
            for (var port = 0; port < count; port++)
            {
                var item = IoStatusItems.FirstOrDefault(status => status.IsInput == isInput && status.Port == port);
                if (item != null)
                    continue;

                IoStatusItems.Add(new ZMotionIoStatusItem(isInput, port, $"{prefix}{port}"));
            }
        }

        public void AddOutputPoint()
        {
            EnsureOutputPointConfig();
            var port = AvailableOutputPorts.FirstOrDefault(item => OutputPointConfigs.All(point => point.Port != item));
            var index = OutputPointConfigs.Count + 1;
            var point = new ZMotionOutputPointConfig(Guid.NewGuid().ToString("N"), $"OUT点{index}", port, OutputRoleNone, true, true, ResetPolicyAutoReset);
            OutputPointConfigs.Add(point);
            SelectedOutputPoint = point;
            AddLog($"新增OUT点：{point.Name}->OUT{point.Port}");
        }

        public void RemoveSelectedOutputPoint()
        {
            EnsureOutputPointConfig();
            var point = SelectedOutputPoint;
            if (point == null)
                return;

            OutputPointConfigs.Remove(point);
            AddLog($"删除OUT点：{point.Name}");
            SelectedOutputPoint = OutputPointConfigs.FirstOrDefault();
        }

        public void AddInputSource()
        {
            EnsureOutputPointConfig();
            var index = InputSourceConfigs.Count + 1;
            var source = new ZMotionOutputSourceConfig(Guid.NewGuid().ToString("N"), $"输入源{index}", SourceRoleCustom, SourceResolverAuto);
            InputSourceConfigs.Add(source);
            SelectedInputSource = source;
            AddLog($"新增输入源：{source.Name}");
        }

        public void RemoveSelectedInputSource()
        {
            EnsureOutputPointConfig();
            var source = SelectedInputSource;
            if (source == null)
                return;

            if (IsDefaultInputSource(source.Id))
            {
                AddLog("默认输入源用于兼容旧流程，不建议删除；如暂不使用可取消启用");
                return;
            }

            InputSourceConfigs.Remove(source);
            foreach (var rule in OutputRuleConfigs.Where(rule => string.Equals(rule.SourceId, source.Id, StringComparison.Ordinal)).ToList())
                rule.SourceId = string.Empty;

            AddLog($"删除输入源：{source.Name}");
            SelectedInputSource = InputSourceConfigs.FirstOrDefault();
        }

        public void AddOutputRule()
        {
            EnsureOutputPointConfig();
            var index = OutputRuleConfigs.Count + 1;
            var usedPorts = GetEnabledDirectOutputRules().Select(rule => NormalizePort(rule.DirectPort)).ToHashSet();
            var port = AvailableOutputPorts.FirstOrDefault(item => !usedPorts.Contains(item));
            var rule = new ZMotionOutputRuleConfig(Guid.NewGuid().ToString("N"), $"IO输出{index}", RuleConditionSourceNg, string.Empty, string.Empty, RuleActionSetActive, index * 10)
            {
                BindingParam = new TransmitParam(),
                ResolverKey = SourceResolverAuto,
                UseDirectPort = true,
                DirectPort = NormalizePort(port),
                ActiveLevel = true,
                ResetPolicyKey = ResetPolicyAutoReset,
            };
            OutputRuleConfigs.Add(rule);
            SelectedOutputRule = rule;
            AddLog($"新增输出规则：{rule.Name}");
        }

        public void RemoveSelectedOutputRule()
        {
            EnsureOutputPointConfig();
            var rule = SelectedOutputRule;
            if (rule == null)
                return;

            OutputRuleConfigs.Remove(rule);
            AddLog($"删除输出规则：{rule.Name}");
            SelectedOutputRule = OutputRuleConfigs.FirstOrDefault();
        }

        public void ClearLogs()
        {
            LogMessages.Clear();
            RaisePropertyChanged(nameof(LogText));
        }

        public void RefreshControlCards()
        {
            try
            {
                var config = PrismProvider.HardwareModuleManager?.Modules?[ConfigKey.ControlCard] as ControlCardConfigModel;
                Models = new ObservableCollection<ControlCardBase>(GetZMotionCards(config, Models));
                ControlCard = ResolveControlCardFromConfig(config, ControlCard);
            }
            catch
            {
                Models = new ObservableCollection<ControlCardBase>();
                ControlCard = null;
            }
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var stopwatch = Stopwatch.StartNew();
            var status = NodeStatus.Success;

            try
            {
                EnsureOutputPointConfigSync();
                TransferParamSync();
                ApplyRecipeParamValues();

                if (!EnableOutput)
                {
                    SetLastMessageSync("NG OUT未启用");
                    PublishOutputsSync();
                    return Task.FromResult(Output = new ExecuteModuleOutput { RunStatus = NodeStatus.Success, RunTime = stopwatch.Elapsed.TotalMilliseconds });
                }

                if (!ValidateConfiguredPorts(out var portMessage))
                {
                    SetLastMessageSync(portMessage);
                    PublishOutputsSync();
                    return Task.FromResult(Output = new ExecuteModuleOutput { RunStatus = NodeStatus.Error, RunTime = stopwatch.Elapsed.TotalMilliseconds });
                }

                var signals = ResolveOutputSignals();
                var outputs = BuildOutputWrites(signals);
                var controlCard = ResolveControlCardForExecution();
                if (controlCard == null)
                {
                    SetLastMessageSync("未找到正运动控制卡");
                    PublishOutputsSync();
                    return Task.FromResult(Output = new ExecuteModuleOutput { RunStatus = NodeStatus.Success, RunTime = stopwatch.Elapsed.TotalMilliseconds });
                }

                if (!controlCard.IsConnected)
                {
                    SetLastMessageSync("控制卡未连接，已跳过OUT输出");
                    PublishOutputsSync();
                    return Task.FromResult(Output = new ExecuteModuleOutput { RunStatus = NodeStatus.Success, RunTime = stopwatch.Elapsed.TotalMilliseconds });
                }

                var failedOutputs = ApplyOutputWrites(controlCard, outputs);
                RefreshOutputPointStates(controlCard);

                if (failedOutputs.Count > 0)
                {
                    status = NodeStatus.Error;
                    SetLastMessageSync($"输出失败：{string.Join("，", failedOutputs)}");
                }
                else
                {
                    SetLastMessageSync(BuildSuccessMessage(outputs, signals));
                }

                PublishOutputsSync();
            }
            catch (Exception ex)
            {
                status = NodeStatus.Error;
                SetLastMessageSync($"正运动NG输出异常：{ex.Message}");
                PublishOutputsSync();
            }

            stopwatch.Stop();
            return Task.FromResult(Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = stopwatch.Elapsed.TotalMilliseconds,
            });
        }

        public bool ResetOutputs(out string message)
        {
            try
            {
                EnsureOutputPointConfigSync();
                var controlCard = ResolveControlCardForExecution();
                var resetOutputs = BuildResetWrites();

                if (controlCard == null)
                {
                    message = "未找到正运动控制卡";
                    return false;
                }

                if (!controlCard.IsConnected)
                {
                    message = "控制卡未连接";
                    return false;
                }

                var failedOutputs = ApplyOutputWrites(controlCard, resetOutputs);
                RefreshOutputPointStates(controlCard);
                InvokeOnDispatcher(() =>
                {
                    ManualLeftNg = false;
                    ManualRightNg = false;
                    ManualYellowOn = false;
                });
                SetLastMessageSync(failedOutputs.Count == 0
                    ? "复位完成：已关闭启用OUT点"
                    : $"复位失败：{string.Join("，", failedOutputs)}");
                PublishOutputsSync();

                message = LastMessage;
                return failedOutputs.Count == 0;
            }
            catch (Exception ex)
            {
                message = $"复位异常：{ex.Message}";
                SetLastMessageSync(message);
                return false;
            }
        }

        public bool TestAllEnabledOutputs(out string message)
        {
            try
            {
                EnsureOutputPointConfigSync();
                if (!ValidateConfiguredPorts(out var portMessage))
                {
                    message = portMessage;
                    SetLastMessageSync(message);
                    return false;
                }

                var controlCard = ResolveControlCardForExecution();
                if (controlCard == null)
                {
                    message = "未找到正运动控制卡";
                    SetLastMessageSync(message);
                    return false;
                }

                if (!controlCard.IsConnected)
                {
                    message = "控制卡未连接";
                    SetLastMessageSync(message);
                    return false;
                }

                var writes = BuildTestWrites();
                var failedOutputs = ApplyOutputWrites(controlCard, writes);
                RefreshOutputPointStates(controlCard);
                SetLastMessageSync(failedOutputs.Count == 0
                    ? $"测试输出完成：{writes.Count} 个启用点已置为有效电平"
                    : $"测试输出失败：{string.Join("，", failedOutputs)}");
                PublishOutputsSync();

                message = LastMessage;
                return failedOutputs.Count == 0;
            }
            catch (Exception ex)
            {
                message = $"测试输出异常：{ex.Message}";
                SetLastMessageSync(message);
                return false;
            }
        }

        private void PublishOutputsSync()
        {
            if (CheckDispatcherAccess())
            {
                PublishOutputs();
                return;
            }

            InvokeOnDispatcher(PublishOutputs);
        }

        private void PublishOutputs()
        {
            foreach (var item in OutputParams)
            {
                var values = OutputParamCollector.GetDataPointValues(this);
                if (values.TryGetValue(item.ParamName, out var value))
                    item.Value = value;
            }

            UpdateParam();
        }

        private OutputSignalSnapshot ResolveOutputSignals()
        {
            SyncLegacyInputsFromDefaultSources();

            var snapshot = new OutputSignalSnapshot
            {
                LeftNg = ManualLeftNg,
                RightNg = ManualRightNg,
                ManualYellow = ManualYellowOn,
            };

            var leftResolved = false;
            var rightResolved = false;

            foreach (var source in GetEnabledInputSources())
            {
                if (!TryResolveSourceState(source, out var sourceNg))
                    continue;

                snapshot.SetSourceState(source.Id, sourceNg);

                switch (NormalizeSourceRoleKey(source.RoleKey))
                {
                    case SourceRoleLeftNg:
                        snapshot.LeftNg = sourceNg;
                        leftResolved = true;
                        break;
                    case SourceRoleRightNg:
                        snapshot.RightNg = sourceNg;
                        rightResolved = true;
                        break;
                    case SourceRoleTotalNg:
                        snapshot.TotalNg = sourceNg;
                        break;
                    case SourceRoleManualYellow:
                        snapshot.ManualYellow = sourceNg;
                        break;
                }
            }

            if (!leftResolved && TryResolveNgValue(GetTransmitParam(InputParams, LeftNgInput, false), out var leftNg))
            {
                snapshot.LeftNg = leftNg;
                leftResolved = true;
            }

            if (!rightResolved && TryResolveNgValue(GetTransmitParam(InputParams, RightNgInput, false), out var rightNg))
            {
                snapshot.RightNg = rightNg;
                rightResolved = true;
            }

            var packetValue = GetTransmitParam(InputParams, XYHDPacketInput, false);
            var packetLeftNg = snapshot.LeftNg;
            var packetRightNg = snapshot.RightNg;
            ResolvePacketValue(packetValue, ref packetLeftNg, ref packetRightNg, ref leftResolved, ref rightResolved);
            snapshot.LeftNg = packetLeftNg;
            snapshot.RightNg = packetRightNg;

            snapshot.SetSourceState(DefaultLeftSourceId, snapshot.LeftNg);
            snapshot.SetSourceState(DefaultRightSourceId, snapshot.RightNg);
            snapshot.SetSourceState(DefaultTotalSourceId, snapshot.AnyNg);
            snapshot.SetSourceState(DefaultManualYellowSourceId, snapshot.ManualYellow);
            snapshot.SetRoleState(SourceRoleLeftNg, snapshot.LeftNg);
            snapshot.SetRoleState(SourceRoleRightNg, snapshot.RightNg);
            snapshot.SetRoleState(SourceRoleTotalNg, snapshot.AnyNg);
            snapshot.SetRoleState(SourceRoleManualYellow, snapshot.ManualYellow);
            return snapshot;
        }

        private IEnumerable<ZMotionOutputSourceConfig> GetEnabledInputSources()
        {
            return (InputSourceConfigs ?? new ObservableCollection<ZMotionOutputSourceConfig>())
                .Where(source => source != null && source.IsEnabled);
        }

        private bool TryResolveSourceState(ZMotionOutputSourceConfig source, out bool isNg)
        {
            isNg = false;
            if (source == null)
                return false;

            if (NormalizeSourceRoleKey(source.RoleKey) == SourceRoleManualYellow)
            {
                isNg = ManualYellowOn || source.ManualValue;
                return true;
            }

            if (HasTransmitBinding(source.BindingParam))
            {
                var value = GetTransmitParam(InputParams, source.BindingParam, false);
                if (TryResolveSourceValue(source.ResolverKey, value, out isNg))
                    return true;
            }

            isNg = source.ManualValue;
            return source.ManualValue;
        }

        private static bool TryResolveSourceValue(string? resolverKey, object? value, out bool isNg)
        {
            isNg = false;
            switch (NormalizeSourceResolverKey(resolverKey))
            {
                case SourceResolverBool:
                    return TryResolveBoolValue(value, out isNg);
                case SourceResolverOkNgText:
                    return TryResolveString(value?.ToString() ?? string.Empty, out isNg);
                case SourceResolverDefectList:
                    return TryResolveDefectResults(value, out isNg);
                case SourceResolverNumberThreshold:
                    return TryResolveNumberThreshold(value, out isNg);
                default:
                    return TryResolveNgValue(value, out isNg);
            }
        }

        private static bool TryResolveBoolValue(object? value, out bool isNg)
        {
            isNg = false;
            if (value == null)
                return false;

            if (value is bool boolValue)
            {
                isNg = boolValue;
                return true;
            }

            if (value is string text)
            {
                var trimmed = text.Trim();
                if (trimmed.Equals("ON", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("NG", StringComparison.OrdinalIgnoreCase))
                {
                    isNg = true;
                    return true;
                }

                if (trimmed.Equals("OFF", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("0", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    isNg = false;
                    return true;
                }
            }

            return TryResolveNumberThreshold(value, out isNg);
        }

        private static bool TryResolveNumberThreshold(object? value, out bool isNg)
        {
            isNg = false;
            if (value == null)
                return false;

            if (value is IConvertible convertible)
            {
                try
                {
                    isNg = Math.Abs(convertible.ToDouble(null)) > double.Epsilon;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private List<OutputWrite> BuildOutputWrites(OutputSignalSnapshot signals)
        {
            var writes = new List<OutputWrite>();
            if (GetEnabledDirectOutputRules().Any())
                return BuildDirectRuleWrites(signals);

            foreach (var point in GetEnabledOutputPoints())
            {
                if (!TryEvaluateOutputRulePoint(point, signals, out var isActive)
                    && !TryEvaluatePoint(point, signals, out isActive))
                {
                    continue;
                }

                if (isActive)
                {
                    writes.Add(CreateOutputWrite(point, point.ActiveLevel));
                    continue;
                }

                if (ShouldWriteInactive(point))
                    writes.Add(CreateOutputWrite(point, !point.ActiveLevel));
            }

            writes.AddRange(BuildDirectRuleWrites(signals));
            return writes;
        }

        private List<OutputWrite> BuildDirectRuleWrites(OutputSignalSnapshot signals)
        {
            var writes = new List<OutputWrite>();
            foreach (var rule in GetEnabledDirectOutputRules())
            {
                var isActive = EvaluateRuleCondition(rule, signals);
                if (isActive)
                {
                    writes.Add(CreateOutputWrite(rule, rule.ActiveLevel));
                    continue;
                }

                if (NormalizeResetPolicyKey(rule.ResetPolicyKey) == ResetPolicyAutoReset)
                    writes.Add(CreateOutputWrite(rule, !rule.ActiveLevel));
            }

            return writes;
        }

        private List<OutputWrite> BuildResetWrites()
        {
            if (GetEnabledDirectOutputRules().Any())
            {
                return GetEnabledDirectOutputRules()
                    .Select(rule => CreateOutputWrite(rule, !rule.ActiveLevel))
                    .ToList();
            }

            var writes = GetEnabledOutputPoints()
                .Select(point => CreateOutputWrite(point, !point.ActiveLevel))
                .ToList();

            return writes;
        }

        private List<OutputWrite> BuildTestWrites()
        {
            if (GetEnabledDirectOutputRules().Any())
            {
                return GetEnabledDirectOutputRules()
                    .Select(rule => CreateOutputWrite(rule, rule.ActiveLevel))
                    .ToList();
            }

            var writes = GetEnabledOutputPoints()
                .Select(point => CreateOutputWrite(point, point.ActiveLevel))
                .ToList();

            return writes;
        }

        private OutputWrite CreateOutputWrite(ZMotionOutputPointConfig point, bool level)
        {
            var name = string.IsNullOrWhiteSpace(point.Name) ? $"OUT{point.Port}" : point.Name.Trim();
            return new OutputWrite(name, NormalizePort(point.Port), level, value => ApplyPointState(point, value));
        }

        private OutputWrite CreateOutputWrite(ZMotionOutputRuleConfig rule, bool level)
        {
            var port = NormalizePort(rule.DirectPort);
            var name = string.IsNullOrWhiteSpace(rule.Name) ? $"OUT{port}" : rule.Name.Trim();
            return new OutputWrite(name, port, level, value => ApplyIoStatusState(false, port, value));
        }

        private static bool ShouldWriteInactive(ZMotionOutputPointConfig point)
        {
            return NormalizeResetPolicyKey(point.ResetPolicyKey) == ResetPolicyAutoReset;
        }

        private bool TryEvaluateOutputRulePoint(ZMotionOutputPointConfig point, OutputSignalSnapshot signals, out bool isActive)
        {
            isActive = false;
            var rules = (OutputRuleConfigs ?? new ObservableCollection<ZMotionOutputRuleConfig>())
                .Where(rule => rule != null
                    && rule.IsEnabled
                    && !rule.UseDirectPort
                    && string.Equals(rule.TargetPointId, point.Id, StringComparison.Ordinal))
                .OrderBy(rule => rule.Priority)
                .ToList();

            if (rules.Count == 0)
                return false;

            foreach (var rule in rules)
            {
                if (!EvaluateRuleCondition(rule, signals))
                    continue;

                isActive = NormalizeRuleActionKey(rule.ActionKey) != RuleActionSetInactive;
            }

            return true;
        }

        private bool EvaluateRuleCondition(ZMotionOutputRuleConfig rule, OutputSignalSnapshot signals)
        {
            switch (NormalizeRuleConditionKey(rule.ConditionKey))
            {
                case RuleConditionAnyNg:
                    return signals.AnyNg;
                case RuleConditionAllOk:
                    return signals.AllOk;
                case RuleConditionManualYellow:
                    return signals.ManualYellow;
                default:
                    if (TryResolveRuleBinding(rule, out var ruleValue))
                        return ruleValue;

                    return signals.TryGetSourceState(rule.SourceId, out var sourceNg) && sourceNg;
            }
        }

        private bool TryResolveRuleBinding(ZMotionOutputRuleConfig rule, out bool isActive)
        {
            isActive = false;
            if (rule == null || !HasTransmitBinding(rule.BindingParam))
                return false;

            var value = GetTransmitParam(InputParams, rule.BindingParam, false);
            return TryResolveSourceValue(rule.ResolverKey, value, out isActive);
        }

        private bool TryEvaluatePoint(ZMotionOutputPointConfig point, OutputSignalSnapshot signals, out bool isActive)
        {
            isActive = false;

            switch (NormalizeRoleKey(point.RoleKey))
            {
                case OutputRoleLeftNg:
                    isActive = signals.LeftNg;
                    return true;
                case OutputRoleRightNg:
                    isActive = signals.RightNg;
                    return true;
                case OutputRoleTotalNg:
                    isActive = signals.AnyNg;
                    return true;
                case OutputRoleGreen:
                    isActive = signals.AllOk;
                    return true;
                case OutputRoleYellow:
                    isActive = signals.ManualYellow;
                    return true;
                case OutputRoleRed:
                case OutputRoleBuzzer:
                    isActive = signals.AnyNg;
                    return true;
                default:
                    return false;
            }
        }

        private ControlCardBase? ResolveControlCardForExecution()
        {
            var config = PrismProvider.HardwareModuleManager?.Modules?[ConfigKey.ControlCard] as ControlCardConfigModel;
            Models = new ObservableCollection<ControlCardBase>(GetZMotionCards(config, Models));

            var controlCard = ResolveControlCardFromConfig(config, ControlCard);
            ControlCard = controlCard;

            if (controlCard != null && !string.IsNullOrWhiteSpace(GetCardIdentity(controlCard)))
                SltModelName = GetCardIdentity(controlCard);

            return controlCard;
        }

        private ControlCardBase? ResolveControlCardFromConfig(ControlCardConfigModel? config, ControlCardBase? preferredCard)
        {
            var models = GetZMotionCards(config, Models);
            if (models.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(SltModelName))
            {
                var namedCard = models.FirstOrDefault(card => string.Equals(card?.NickName, SltModelName, StringComparison.Ordinal));
                if (namedCard != null)
                    return namedCard;

                namedCard = models.FirstOrDefault(card => string.Equals(GetCardIdentity(card), SltModelName, StringComparison.Ordinal));
                if (namedCard != null)
                    return namedCard;
            }

            if (preferredCard != null && models.Any(card => ReferenceEquals(card, preferredCard)))
                return preferredCard;

            if (config?.CurSltCard != null && models.Any(card => ReferenceEquals(card, config.CurSltCard)))
                return config.CurSltCard;

            return models.FirstOrDefault(card => card.IsConnected) ?? models.FirstOrDefault();
        }

        private static List<ControlCardBase> GetZMotionCards(ControlCardConfigModel? config, IEnumerable<ControlCardBase>? fallbackCards)
        {
            return EnumerateControlCards(config, fallbackCards)
                .Where(IsZMotionCard)
                .ToList();
        }

        private static IEnumerable<ControlCardBase> EnumerateControlCards(ControlCardConfigModel? config, IEnumerable<ControlCardBase>? fallbackCards)
        {
            var seenCards = new HashSet<ControlCardBase>();

            foreach (var card in config?.CardModels ?? Enumerable.Empty<ControlCardBase>())
            {
                if (card != null && seenCards.Add(card))
                    yield return card;
            }

            if (config?.CurSltCard != null && seenCards.Add(config.CurSltCard))
                yield return config.CurSltCard;

            foreach (var card in fallbackCards ?? Enumerable.Empty<ControlCardBase>())
            {
                if (card != null && seenCards.Add(card))
                    yield return card;
            }
        }

        private static bool IsZMotionCard(ControlCardBase? card)
        {
            if (card == null)
                return false;

            return string.Equals(card.VenderName, "ZMotion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(card.VenderName, "正运动", StringComparison.OrdinalIgnoreCase)
                || string.Equals(card.CardType, "ZMotion", StringComparison.OrdinalIgnoreCase)
                || card.GetType().Name.Contains("ZMotion", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCardIdentity(ControlCardBase? card)
        {
            if (card == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(card.DisplayName))
                return card.DisplayName;

            if (!string.IsNullOrWhiteSpace(card.NickName))
                return card.NickName;

            if (!string.IsNullOrWhiteSpace(card.VenderName) || !string.IsNullOrWhiteSpace(card.CardType))
                return $"{card.VenderName}-{card.CardType}".Trim('-');

            return card.GetType().Name;
        }

        private List<string> ApplyOutputWrites(ControlCardBase? controlCard, IEnumerable<OutputWrite> writes)
        {
            var failures = new List<string>();
            foreach (var write in writes)
            {
                var ok = controlCard?.SetSpecifiedIO(write.Port, write.Level) == true;
                if (ok)
                {
                    InvokeOnDispatcher(() => write.ApplyState(write.Level));
                    continue;
                }

                failures.Add($"{write.Name}->OUT{write.Port}={(write.Level ? "ON" : "OFF")}");
            }

            return failures;
        }

        private bool ValidateConfiguredPorts(out string message)
        {
            message = string.Empty;
            var ports = GetEnabledOutputPoints()
                .Select(point => (Name: string.IsNullOrWhiteSpace(point.Name) ? $"OUT{point.Port}" : point.Name.Trim(), Port: NormalizePort(point.Port)))
                .ToList();

            var duplicate = ports
                .GroupBy(item => item.Port)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate != null)
            {
                message = $"OUT点位重复：OUT{duplicate.Key} 被 {string.Join("/", duplicate.Select(item => item.Name))} 同时使用";
                return false;
            }

            var directDuplicate = GetEnabledDirectOutputRules()
                .Select(rule => (Name: string.IsNullOrWhiteSpace(rule.Name) ? $"规则OUT{rule.DirectPort}" : rule.Name.Trim(), Port: NormalizePort(rule.DirectPort)))
                .GroupBy(item => item.Port)
                .FirstOrDefault(group => group.Count() > 1);

            if (directDuplicate == null)
                return true;

            message = $"输出规则直配IO重复：OUT{directDuplicate.Key} 被 {string.Join("/", directDuplicate.Select(item => item.Name))} 同时使用";
            return false;
        }

        private string BuildSuccessMessage(IReadOnlyCollection<OutputWrite> outputs, OutputSignalSnapshot signals)
        {
            var actionText = outputs.Count == 0
                ? "无自动输出点动作"
                : string.Join("，", outputs.Select(output => $"{output.Name}/OUT{output.Port}={(output.Level ? "ON" : "OFF")}"));
            return $"输出完成：左NG={signals.LeftNg}，右NG={signals.RightNg}，总NG={signals.AnyNg}；{actionText}";
        }

        private static int NormalizePort(int port) => Math.Max(0, port);

        private OutputEndpoint ResolveOutputEndpoint(string roleKey, int fallbackPort, string fallbackName)
        {
            EnsureOutputPointConfigSync();
            var point = GetRolePoints(roleKey).FirstOrDefault();
            if (point == null)
                return new OutputEndpoint(fallbackName, NormalizePort(fallbackPort));

            var name = string.IsNullOrWhiteSpace(point.Name) ? fallbackName : point.Name.Trim();
            return new OutputEndpoint(name, NormalizePort(point.Port));
        }

        private void EnsureRequiredRolePoint(string roleKey, string defaultId, string defaultName, int defaultPort)
        {
            if (GetRolePoints(roleKey).Any())
                return;

            var defaultPoint = OutputPointConfigs.FirstOrDefault(point => point.Id == defaultId);
            if (defaultPoint == null)
            {
                defaultPoint = new ZMotionOutputPointConfig(defaultId, defaultName, defaultPort, roleKey);
                OutputPointConfigs.Add(defaultPoint);
            }

            defaultPoint.RoleKey = roleKey;
        }

        private void ApplyLegacyRole(string pointId, string roleKey)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            var point = OutputPointConfigs.FirstOrDefault(item => item.Id == pointId);
            if (point == null)
                return;

            if (string.IsNullOrWhiteSpace(point.RoleKey) || point.RoleKey == OutputRoleNone)
                point.RoleKey = roleKey;
        }

        private void ApplyLegacyPointOptions(string roleKey, bool isEnabled, bool activeLevel, string resetPolicyKey)
        {
            foreach (var point in GetRolePoints(roleKey))
            {
                point.IsEnabled = isEnabled;
                point.ActiveLevel = activeLevel;
                point.ResetPolicyKey = resetPolicyKey;
            }
        }

        private IEnumerable<ZMotionOutputPointConfig> GetEnabledOutputPoints()
        {
            return (OutputPointConfigs ?? new ObservableCollection<ZMotionOutputPointConfig>())
                .Where(point => point != null && point.IsEnabled);
        }

        private IEnumerable<ZMotionOutputRuleConfig> GetEnabledDirectOutputRules()
        {
            return (OutputRuleConfigs ?? new ObservableCollection<ZMotionOutputRuleConfig>())
                .Where(rule => rule != null && rule.IsEnabled && rule.UseDirectPort);
        }

        private HashSet<int> GetEnabledDirectRulePorts()
        {
            return GetEnabledDirectOutputRules()
                .Select(rule => NormalizePort(rule.DirectPort))
                .ToHashSet();
        }

        private IEnumerable<ZMotionOutputPointConfig> GetRolePoints(string roleKey)
        {
            return OutputPointConfigs
                .Where(point => point != null && NormalizeRoleKey(point.RoleKey) == roleKey);
        }

        private static string NormalizeRoleKey(string? roleKey)
        {
            if (string.IsNullOrWhiteSpace(roleKey))
                return OutputRoleNone;

            return roleKey.Trim();
        }

        private static string NormalizeSourceRoleKey(string? roleKey)
        {
            if (string.IsNullOrWhiteSpace(roleKey))
                return SourceRoleNone;

            return roleKey.Trim();
        }

        private static string NormalizeSourceResolverKey(string? resolverKey)
        {
            if (string.IsNullOrWhiteSpace(resolverKey))
                return SourceResolverAuto;

            return resolverKey.Trim();
        }

        private static string NormalizeRuleConditionKey(string? conditionKey)
        {
            if (string.IsNullOrWhiteSpace(conditionKey))
                return RuleConditionSourceNg;

            return conditionKey.Trim();
        }

        private static string NormalizeRuleActionKey(string? actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
                return RuleActionSetActive;

            return actionKey.Trim();
        }

        private static string NormalizeResetPolicyKey(string? resetPolicyKey)
        {
            if (string.IsNullOrWhiteSpace(resetPolicyKey))
                return ResetPolicyAutoReset;

            return resetPolicyKey.Trim();
        }

        private string GetRoleDisplayName(string roleKey)
        {
            return OutputRoleOptions.FirstOrDefault(item => item.RoleKey == roleKey)?.RoleName ?? roleKey;
        }

        private void ApplyPointState(ZMotionOutputPointConfig point, bool value)
        {
            point.CurrentState = value;
            ApplyIoStatusState(false, NormalizePort(point.Port), value);

            switch (NormalizeRoleKey(point.RoleKey))
            {
                case OutputRoleLeftNg:
                    LeftOutputState = value;
                    break;
                case OutputRoleRightNg:
                    RightOutputState = value;
                    break;
                case OutputRoleGreen:
                    GreenOutputState = value;
                    break;
                case OutputRoleYellow:
                    YellowOutputState = value;
                    break;
                case OutputRoleRed:
                    RedOutputState = value;
                    break;
            }
        }

        private void ApplyIoStatusState(bool isInput, int port, bool value)
        {
            EnsureIoStatusItems();
            var item = IoStatusItems.FirstOrDefault(status => status.IsInput == isInput && status.Port == port);
            if (item == null)
                return;

            item.State = value;
            item.IsReadOk = true;
            item.LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
        }

        public void RefreshOutputPointStates()
        {
            if (!CheckDispatcherAccess())
            {
                InvokeOnDispatcher(RefreshOutputPointStates);
                return;
            }

            EnsureOutputPointConfig();
            var controlCard = ResolveControlCardForExecution();
            RefreshOutputPointStates(controlCard);
        }

        private void RefreshOutputPointStates(ControlCardBase? controlCard)
        {
            if (controlCard == null || !controlCard.IsConnected)
                return;

            foreach (var point in OutputPointConfigs ?? Enumerable.Empty<ZMotionOutputPointConfig>())
            {
                if (point == null)
                    continue;

                var port = NormalizePort(point.Port);
                if (!controlCard.GetSpecifiedIO(false, port, out var state))
                    continue;

                if (CheckDispatcherAccess())
                {
                    ApplyPointState(point, state);
                }
                else
                {
                    InvokeOnDispatcher(() => ApplyPointState(point, state));
                }
            }
        }

        public void RefreshIoStatusItems()
        {
            if (!CheckDispatcherAccess())
            {
                InvokeOnDispatcher(RefreshIoStatusItems);
                return;
            }

            EnsureOutputPointConfig();
            var controlCard = ResolveControlCardForExecution();
            RefreshIoStatusItems(controlCard);
        }

        private void RefreshIoStatusItems(ControlCardBase? controlCard)
        {
            EnsureIoStatusItems();
            if (controlCard == null)
            {
                SetIoStatusReadFailed("未找到控制卡");
                return;
            }

            if (!controlCard.IsConnected)
            {
                SetIoStatusReadFailed("未连接");
                return;
            }

            foreach (var item in IoStatusItems)
            {
                if (item == null)
                    continue;

                var ok = controlCard.GetSpecifiedIO(item.IsInput, NormalizePort(item.Port), out var state);
                item.IsReadOk = ok;
                item.LastRefreshText = DateTime.Now.ToString("HH:mm:ss");
                if (ok)
                    item.State = state;
            }

            SetLastMessageSync("IO状态刷新完成");
        }

        private void SetIoStatusReadFailed(string reason)
        {
            foreach (var item in IoStatusItems)
            {
                item.IsReadOk = false;
                item.LastRefreshText = reason;
            }

            SetLastMessageSync($"IO状态刷新失败：{reason}");
        }

        public bool SetSelectedOutputIo(bool level, out string message)
        {
            try
            {
                EnsureOutputPointConfigSync();
                var item = SelectedIoStatusItem;
                if (item == null || item.IsInput)
                {
                    message = "请先在IO状态表选择一个OUT点";
                    SetLastMessageSync(message);
                    return false;
                }

                var controlCard = ResolveControlCardForExecution();
                if (controlCard == null)
                {
                    message = "未找到正运动控制卡";
                    SetLastMessageSync(message);
                    return false;
                }

                if (!controlCard.IsConnected)
                {
                    message = "控制卡未连接";
                    SetLastMessageSync(message);
                    return false;
                }

                var port = NormalizePort(item.Port);
                var ok = controlCard.SetSpecifiedIO(port, level);
                if (ok)
                {
                    ApplyIoStatusState(false, port, level);
                    RefreshOutputPointStates(controlCard);
                    message = $"手动设置OUT{port}={(level ? "ON" : "OFF")}完成";
                }
                else
                {
                    message = $"手动设置OUT{port}={(level ? "ON" : "OFF")}失败";
                }

                SetLastMessageSync(message);
                return ok;
            }
            catch (Exception ex)
            {
                message = $"手动设置IO异常：{ex.Message}";
                SetLastMessageSync(message);
                return false;
            }
        }

        private void AddLog(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!CheckDispatcherAccess())
            {
                InvokeOnDispatcher(() => AddLog(message));
                return;
            }

            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            while (LogMessages.Count > 200)
                LogMessages.RemoveAt(LogMessages.Count - 1);

            RaisePropertyChanged(nameof(LogText));
        }

        private void SetLastMessageSync(string message)
        {
            if (CheckDispatcherAccess())
            {
                LastMessage = message;
                return;
            }

            InvokeOnDispatcher(() => LastMessage = message);
        }

        private void EnsureOutputPointConfigSync()
        {
            if (CheckDispatcherAccess())
            {
                EnsureOutputPointConfig();
                return;
            }

            InvokeOnDispatcher(EnsureOutputPointConfig);
        }

        private static bool CheckDispatcherAccess()
        {
            return PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess();
        }

        private static void InvokeOnDispatcher(Action action)
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        private static void ResolvePacketValue(object? value, ref bool leftNg, ref bool rightNg, ref bool leftResolved, ref bool rightResolved)
        {
            if (value == null)
                return;

            if (value is IEnumerable items && value is not string)
            {
                var unknownIndex = 0;
                foreach (var item in items)
                {
                    ApplyPacketItem(item, ref leftNg, ref rightNg, ref leftResolved, ref rightResolved, ref unknownIndex);
                }
                return;
            }

            var singleUnknownIndex = 0;
            ApplyPacketItem(value, ref leftNg, ref rightNg, ref leftResolved, ref rightResolved, ref singleUnknownIndex);
        }

        private static void ApplyPacketItem(object? item, ref bool leftNg, ref bool rightNg, ref bool leftResolved, ref bool rightResolved, ref int unknownIndex)
        {
            if (item == null || !TryResolveNgValue(item, out var isNg))
                return;

            var role = ResolvePathRole(item);
            if (role == PathRole.Left)
            {
                leftNg = isNg;
                leftResolved = true;
                return;
            }

            if (role == PathRole.Right)
            {
                rightNg = isNg;
                rightResolved = true;
                return;
            }

            if (unknownIndex == 0 && !leftResolved)
            {
                leftNg = isNg;
                leftResolved = true;
            }
            else if (!rightResolved)
            {
                rightNg = isNg;
                rightResolved = true;
            }

            unknownIndex++;
        }

        private static bool TryResolveNgValue(object? value, out bool isNg)
        {
            isNg = false;
            if (value == null)
                return false;

            if (value is bool boolValue)
            {
                isNg = boolValue;
                return true;
            }

            if (value is string text)
                return TryResolveString(text, out isNg);

            if (value is int intValue)
            {
                isNg = intValue != 0;
                return true;
            }

            if (value is double doubleValue)
            {
                isNg = Math.Abs(doubleValue) > double.Epsilon;
                return true;
            }

            if (value is IDictionary dictionary && TryResolveDictionaryNg(dictionary, out isNg))
                return true;

            if (TryResolveObjectMemberNg(value, out isNg))
                return true;

            if (value is IEnumerable enumerable)
                return TryResolveEnumerableNg(enumerable, out isNg);

            return false;
        }

        private static bool TryResolveDictionaryNg(IDictionary values, out bool isNg)
        {
            isNg = false;
            if (values == null)
                return false;

            foreach (var key in new[] { "IsNg", "IsNG", "NG", "TotalNg", "AnyNg" })
            {
                if (TryGetDictionaryValue(values, key, out var value))
                    return TryResolveNgValue(value, out isNg);
            }

            foreach (var key in new[] { "IsOk", "IsOK", "OK" })
            {
                if (TryGetDictionaryValue(values, key, out var value))
                    return TryResolveIsOkValue(value, out isNg);
            }

            foreach (var key in new[] { "Results", "DefectResults", "Defects" })
            {
                if (TryGetDictionaryValue(values, key, out var value))
                    return TryResolveDefectResults(value, out isNg);
            }

            return TryGetDictionaryValue(values, "Count", out var count)
                && TryResolveNumberThreshold(count, out isNg);
        }

        private static bool TryGetDictionaryValue(IDictionary values, string key, out object? value)
        {
            value = null;
            foreach (DictionaryEntry item in values)
            {
                if (string.Equals(item.Key?.ToString(), key, StringComparison.OrdinalIgnoreCase))
                {
                    value = item.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveString(string text, out bool isNg)
        {
            isNg = false;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var value = text.Trim();
            if (value.Equals("NG", StringComparison.OrdinalIgnoreCase)
                || value.Equals("NOK", StringComparison.OrdinalIgnoreCase)
                || value.Equals("FAIL", StringComparison.OrdinalIgnoreCase)
                || value.Equals("FAILED", StringComparison.OrdinalIgnoreCase)
                || value.Equals("不良", StringComparison.OrdinalIgnoreCase)
                || value.Equals("异常", StringComparison.OrdinalIgnoreCase)
                || value.Equals("失败", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                isNg = true;
                return true;
            }

            if (value.Equals("OK", StringComparison.OrdinalIgnoreCase)
                || value.Equals("PASS", StringComparison.OrdinalIgnoreCase)
                || value.Equals("正常", StringComparison.OrdinalIgnoreCase)
                || value.Equals("良品", StringComparison.OrdinalIgnoreCase)
                || value.Equals("合格", StringComparison.OrdinalIgnoreCase)
                || value.Equals("通过", StringComparison.OrdinalIgnoreCase)
                || value.Equals("0", StringComparison.OrdinalIgnoreCase)
                || value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                isNg = false;
                return true;
            }

            return false;
        }

        private static bool TryResolveObjectMemberNg(object value, out bool isNg)
        {
            isNg = false;
            var type = value.GetType();

            if (TryGetMemberValue(value, type, "IsNG", out var directNg)
                || TryGetMemberValue(value, type, "IsNg", out directNg)
                || TryGetMemberValue(value, type, "NG", out directNg))
            {
                return TryResolveNgValue(directNg, out isNg);
            }

            if (TryGetMemberValue(value, type, "IsOks", out var isOks)
                || TryGetMemberValue(value, type, "IsOKs", out isOks)
                || TryGetMemberValue(value, type, "IsOk", out isOks)
                || TryGetMemberValue(value, type, "IsOK", out isOks))
            {
                return TryResolveIsOkValue(isOks, out isNg);
            }

            if (TryGetMemberValue(value, type, "DefectResults", out var defects)
                || TryGetMemberValue(value, type, "Results", out defects))
            {
                return TryResolveDefectResults(defects, out isNg);
            }

            return false;
        }

        private static bool TryResolveEnumerableNg(IEnumerable values, out bool isNg)
        {
            isNg = false;
            var hasAny = false;

            foreach (var item in values)
            {
                hasAny = true;
                if (item is bool okValue)
                {
                    if (!okValue)
                        isNg = true;
                }
                else if (TryResolveNgValue(item, out var itemNg) && itemNg)
                {
                    isNg = true;
                }
            }

            return hasAny;
        }

        private static bool TryResolveIsOkValue(object? value, out bool isNg)
        {
            isNg = false;
            if (value == null)
                return false;

            if (value is bool ok)
            {
                isNg = !ok;
                return true;
            }

            if (value is string text && TryResolveString(text, out var textNg))
            {
                isNg = textNg;
                return true;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var hasAny = false;
                foreach (var item in enumerable)
                {
                    if (item is bool itemOk)
                    {
                        hasAny = true;
                        if (!itemOk)
                            isNg = true;
                    }
                    else if (TryResolveIsOkValue(item, out var itemNg))
                    {
                        hasAny = true;
                        if (itemNg)
                            isNg = true;
                    }
                }
                return hasAny;
            }

            return false;
        }

        private static bool TryResolveDefectResults(object? value, out bool isNg)
        {
            isNg = false;
            if (value == null)
                return false;

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var _ in enumerable)
                {
                    isNg = true;
                    return true;
                }

                return true;
            }

            isNg = true;
            return true;
        }

        private static PathRole ResolvePathRole(object? item)
        {
            if (item == null)
                return PathRole.Unknown;

            if (!TryGetMemberValue(item, item.GetType(), "PathName", out var pathObj)
                && !TryGetMemberValue(item, item.GetType(), "Name", out pathObj))
            {
                return PathRole.Unknown;
            }

            var path = pathObj?.ToString() ?? string.Empty;
            if (path.Contains("左", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Left", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Path1", StringComparison.OrdinalIgnoreCase)
                || path.Contains("路1", StringComparison.OrdinalIgnoreCase))
            {
                return PathRole.Left;
            }

            if (path.Contains("右", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Right", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Path2", StringComparison.OrdinalIgnoreCase)
                || path.Contains("路2", StringComparison.OrdinalIgnoreCase))
            {
                return PathRole.Right;
            }

            return PathRole.Unknown;
        }

        private static bool TryGetMemberValue(object? instance, Type? type, string memberName, out object? value)
        {
            value = null;
            if (instance == null || type == null || string.IsNullOrWhiteSpace(memberName))
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            var property = type.GetProperty(memberName, flags);
            if (property != null)
            {
                value = property.GetValue(instance);
                return true;
            }

            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                value = field.GetValue(instance);
                return true;
            }

            return false;
        }

        private enum PathRole
        {
            Unknown,
            Left,
            Right
        }

        private sealed class OutputSignalSnapshot
        {
            private readonly Dictionary<string, bool> _sourceStates = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, bool> _roleStates = new(StringComparer.OrdinalIgnoreCase);

            public bool LeftNg { get; set; }

            public bool RightNg { get; set; }

            public bool TotalNg { get; set; }

            public bool ManualYellow { get; set; }

            public bool AnyNg => LeftNg || RightNg || TotalNg;

            public bool AllOk => !AnyNg;

            public void SetSourceState(string? sourceId, bool value)
            {
                if (string.IsNullOrWhiteSpace(sourceId))
                    return;

                _sourceStates[sourceId] = value;
            }

            public void SetRoleState(string? roleKey, bool value)
            {
                if (string.IsNullOrWhiteSpace(roleKey))
                    return;

                _roleStates[roleKey] = value;
            }

            public bool TryGetSourceState(string? sourceId, out bool value)
            {
                value = false;
                if (string.IsNullOrWhiteSpace(sourceId))
                    return false;

                return _sourceStates.TryGetValue(sourceId, out value)
                    || _roleStates.TryGetValue(sourceId, out value);
            }
        }

        private sealed class OutputWrite
        {
            public OutputWrite(string name, int port, bool level, Action<bool> applyState)
            {
                Name = name;
                Port = port;
                Level = level;
                ApplyState = applyState;
            }

            public string Name { get; }

            public int Port { get; }

            public bool Level { get; }

            public Action<bool> ApplyState { get; }
        }

        private readonly struct OutputEndpoint
        {
            public OutputEndpoint(string name, int port)
            {
                Name = name;
                Port = port;
            }

            public string Name { get; }

            public int Port { get; }
        }
    }

}

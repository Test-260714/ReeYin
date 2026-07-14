using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LogicalTool.BranchRouter.Models
{
    public enum BranchParamSource
    {
        LastInput,
        Global,
        CustomGlobal
    }

    public enum BranchCompareType
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        NotContains,
        IsTrue,
        IsFalse
    }

    public enum BranchMatchMode
    {
        FirstMatch,
        AllMatch
    }

    [Serializable]
    public class BranchRouteRule : BindableBase
    {
        private TransmitParam _variable = new TransmitParam();
        /// <summary>
        /// 判断使用的参数，复用参数链接弹窗选择上一节点输入、全局或自定义全局。
        /// </summary>
        public TransmitParam Variable
        {
            get => _variable;
            set
            {
                _variable = value;
                if (_variable != null)
                {
                    DataType = _variable.Type;
                    ParamName = _variable.Name;
                    Source = _variable.Resourece switch
                    {
                        ResoureceType.Global => BranchParamSource.Global,
                        ResoureceType.CustomGlobal => BranchParamSource.CustomGlobal,
                        _ => BranchParamSource.LastInput
                    };
                }
                RaisePropertyChanged();
            }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private BranchParamSource _source = BranchParamSource.LastInput;
        public BranchParamSource Source
        {
            get => _source;
            set { _source = value; RaisePropertyChanged(); }
        }

        private string _paramName = string.Empty;
        public string ParamName
        {
            get => _paramName;
            set { _paramName = value; RaisePropertyChanged(); }
        }

        private DataType _dataType = DataType.String;
        public DataType DataType
        {
            get => _dataType;
            set { _dataType = value; RaisePropertyChanged(); }
        }

        private BranchCompareType _compareType = BranchCompareType.Equal;
        public BranchCompareType CompareType
        {
            get => _compareType;
            set { _compareType = value; RaisePropertyChanged(); }
        }

        private string _targetValue = string.Empty;
        public string TargetValue
        {
            get => _targetValue;
            set { _targetValue = value; RaisePropertyChanged(); }
        }

        private int _targetNodeSerial;
        public int TargetNodeSerial
        {
            get => _targetNodeSerial;
            set { _targetNodeSerial = value; RaisePropertyChanged(); }
        }
    }

    [Serializable]
    public class BranchRouterModel : ModelParamBase, IBranchRouter
    {
        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        private ObservableCollection<BranchRouteRule> _rules = new ObservableCollection<BranchRouteRule>();
        public ObservableCollection<BranchRouteRule> Rules
        {
            get => _rules;
            set { _rules = value ?? new ObservableCollection<BranchRouteRule>(); RaisePropertyChanged(); }
        }

        private BranchMatchMode _matchMode = BranchMatchMode.FirstMatch;
        public BranchMatchMode MatchMode
        {
            get => _matchMode;
            set { _matchMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private readonly List<int> _selectedNextSerials = new List<int>();

        [JsonIgnore]
        public IReadOnlyCollection<int> SelectedNextSerials => _selectedNextSerials;

        [OutputParam("BranchMatched", "分支是否命中")]
        public bool BranchMatched { get; set; }

        [OutputParam("SelectedNodeSerial", "命中节点序号")]
        public int SelectedNodeSerial { get; set; }

        public BranchRouterModel()
        {
            TriggerModuleRun = () => ExecuteModule().Result;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            TriggerModuleRun = () => ExecuteModule().Result;

            var key = Serial.ToString("D3");
            if (PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches != null
                && !PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(key))
            {
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(key, this);
            }
        }

        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                LoadKeyParam();

                _selectedNextSerials.Clear();
                foreach (var rule in Rules.Where(item => item.IsEnabled))
                {
                    if (!TryResolveValue(rule, out var value))
                    {
                        Console.WriteLine($"分支路由_{Serial}：未找到参数 {rule.ParamName}");
                        continue;
                    }

                    if (!CompareValue(value, rule))
                    {
                        continue;
                    }

                    if (rule.TargetNodeSerial > 0 && !_selectedNextSerials.Contains(rule.TargetNodeSerial))
                    {
                        _selectedNextSerials.Add(rule.TargetNodeSerial);
                    }

                    if (MatchMode == BranchMatchMode.FirstMatch)
                    {
                        break;
                    }
                }

                BranchMatched = _selectedNextSerials.Count > 0;
                SelectedNodeSerial = _selectedNextSerials.FirstOrDefault();
                BuildOutputParams();

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }

                return BranchMatched ? NodeStatus.Success : NodeStatus.NotRun;
            });

            return Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };
        }

        private void BuildOutputParams()
        {
            OutputParams = new ObservableCollection<TransmitParam>(
                InputParams.Select(CloneInputParam));

            OutputParams.Add(new TransmitParam
            {
                Serial = Serial,
                Name = "BranchMatched",
                ParamName = "BranchMatched",
                Type = DataType.Bool,
                Value = BranchMatched,
                Describe = "分支是否命中",
                Resourece = ResoureceType.Output
            });

            OutputParams.Add(new TransmitParam
            {
                Serial = Serial,
                Name = "SelectedNodeSerial",
                ParamName = "SelectedNodeSerial",
                Type = DataType.Int,
                Value = SelectedNodeSerial,
                Describe = "命中的后续节点序号",
                Resourece = ResoureceType.Output
            });
        }

        private TransmitParam CloneInputParam(TransmitParam param)
        {
            return new TransmitParam
            {
                Guid = param.Guid,
                Serial = Serial,
                ParentNode = param.ParentNode,
                LinkGuid = param.LinkGuid,
                IsLink = param.IsLink,
                Name = param.Name,
                ParamName = param.ParamName,
                Type = param.Type,
                Value = param.Value,
                Describe = param.Describe,
                IsGlobal = param.IsGlobal,
                ResourcePath = param.ResourcePath,
                Resourece = ResoureceType.Output
            };
        }

        private bool TryResolveValue(BranchRouteRule rule, out object value)
        {
            value = null;
            if (rule.Variable != null && rule.Variable.IsLink)
            {
                // 新页面通过 WxLink 选择参数，运行时按链接来源重新取实时值。
                value = GetTransmitParam(InputParams, rule.Variable, false);
                return true;
            }

            IEnumerable<TransmitParam> sourceParams = rule.Source switch
            {
                BranchParamSource.Global => PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams ?? Enumerable.Empty<TransmitParam>(),
                BranchParamSource.CustomGlobal => PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams ?? Enumerable.Empty<TransmitParam>(),
                _ => InputParams ?? Enumerable.Empty<TransmitParam>()
            };

            var param = sourceParams.FirstOrDefault(item =>
                string.Equals(item.Name, rule.ParamName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ParamName, rule.ParamName, StringComparison.OrdinalIgnoreCase));

            if (param == null)
            {
                return false;
            }

            value = param.Value;
            return true;
        }

        private bool CompareValue(object value, BranchRouteRule rule)
        {
            return rule.CompareType switch
            {
                BranchCompareType.Equal => CompareEqual(value, rule, true),
                BranchCompareType.NotEqual => CompareEqual(value, rule, false),
                BranchCompareType.GreaterThan => CompareNumber(value, rule.TargetValue, (actual, target) => actual > target),
                BranchCompareType.GreaterThanOrEqual => CompareNumber(value, rule.TargetValue, (actual, target) => actual >= target),
                BranchCompareType.LessThan => CompareNumber(value, rule.TargetValue, (actual, target) => actual < target),
                BranchCompareType.LessThanOrEqual => CompareNumber(value, rule.TargetValue, (actual, target) => actual <= target),
                BranchCompareType.Contains => (value?.ToString() ?? string.Empty).Contains(rule.TargetValue ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                BranchCompareType.NotContains => !(value?.ToString() ?? string.Empty).Contains(rule.TargetValue ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                BranchCompareType.IsTrue => TryParseBool(value, out var boolValue) && boolValue,
                BranchCompareType.IsFalse => TryParseBool(value, out var boolValue) && !boolValue,
                _ => false
            };
        }

        private bool CompareEqual(object value, BranchRouteRule rule, bool expected)
        {
            bool actual;
            if (rule.DataType == DataType.Bool && TryParseBool(value, out var actualBool) && TryParseBool(rule.TargetValue, out var targetBool))
            {
                actual = actualBool == targetBool;
            }
            else if (IsNumberType(rule.DataType) && TryParseDouble(value, out var actualNumber) && TryParseDouble(rule.TargetValue, out var targetNumber))
            {
                actual = Math.Abs(actualNumber - targetNumber) < 0.0000001;
            }
            else
            {
                actual = string.Equals(value?.ToString() ?? string.Empty, rule.TargetValue ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return expected ? actual : !actual;
        }

        private bool CompareNumber(object value, string targetValue, Func<double, double, bool> compare)
        {
            return TryParseDouble(value, out var actual) && TryParseDouble(targetValue, out var target) && compare(actual, target);
        }

        private bool TryParseDouble(object value, out double result)
        {
            return double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                || double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        private bool TryParseBool(object value, out bool result)
        {
            string text = value?.ToString()?.Trim() ?? string.Empty;
            if (bool.TryParse(text, out result))
            {
                return true;
            }

            if (text == "1" || string.Equals(text, "是", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (text == "0" || string.Equals(text, "否", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            return false;
        }

        private bool IsNumberType(DataType dataType)
        {
            return dataType == DataType.Int || dataType == DataType.Double;
        }
    }
}

#nullable disable

using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LogicalTool.Loop.Models
{
    [Serializable]
    public class EndLoopModel : ModelParamBase
    {
        #region Fields

        [JsonIgnore]
        private SubscriptionToken _subscriptionToken { get; set; }

        [JsonIgnore]
        public bool IsLoopFlag { get; set; }

        [JsonIgnore]
        private readonly object _loopOutputLock = new object();

        [JsonIgnore]
        private readonly Dictionary<Guid, List<object>> _loopOutputValues = new Dictionary<Guid, List<object>>();

        [JsonIgnore]
        private readonly Dictionary<Guid, TransmitParam> _loopOutputTemplates = new Dictionary<Guid, TransmitParam>();

        [JsonIgnore]
        private int _lastCollectedLoopIndex = 0;

        #endregion

        #region Properties

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        private int _endNodifyNum = 1;
        /// <summary>
        /// 结束节点号（指定要结束的循环节点序号）
        /// </summary>
        public int EndNodifyNum
        {
            get => _endNodifyNum;
            set { _endNodifyNum = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public EndLoopModel()
        {

        }

        #endregion

        #region Override

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun += () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _subscriptionToken?.Dispose();

        }
        #endregion

        #region Methods

        /// <summary>
        /// 模块执行
        /// </summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            try
            {
                var (result, time) = SetTimeHelper.SetTimer(() =>
                {
                    // 获取对应的循环节点
                    var nodeKey = EndNodifyNum.ToString("D3");
                    if (!PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(nodeKey))
                    {
                        Console.WriteLine($"错误：未找到节点 {nodeKey}");
                        return NodeStatus.Error;
                    }

                    if (PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[nodeKey] is not LoopModel loopModel)
                    {
                        Console.WriteLine($"错误：节点 {nodeKey} 不是循环节点");
                        return NodeStatus.Error;
                    }

                    CollectLoopInputParams(loopModel);

                    // 检查循环是否应该结束
                    if (loopModel.TransmitLoopNum == loopModel.LoopNum || loopModel.IsAbortLoop)
                    {
                        FlushMergedOutputParams();

                        // 循环结束，重置状态
                        loopModel.IsAbortLoop = false;
                        //loopModel.TransmitLoopNum = loopModel.LoopNum;
                        loopModel.TransmitLoopNum = 0;
                        Console.WriteLine($"循环结束，节点 {nodeKey} 已完成所有循环");
                        return NodeStatus.Success;
                    }
                    else
                    {
                        // 继续循环，设置标志回到循环起点
                        loopModel.IsLoopFlag = true;
                        Console.WriteLine($"继续循环，当前次数/已执行次数：{loopModel.TransmitLoopNum}");
                        return NodeStatus.Circle;
                    }
                });

                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：结束循环模块执行时间：{time} 毫秒");

                return Output = new ExecuteModuleOutput
                {
                    RunStatus = result,
                    RunTime = time
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"结束循环模块执行异常：{ex.Message}");
                return Output = new ExecuteModuleOutput
                {
                    RunStatus = NodeStatus.Error,
                    RunTime = 0.0
                };
            }
        }

        /// <summary>
        /// 收集本轮 EndLoop 上一节点传入的输出参数。
        /// </summary>
        private void CollectLoopInputParams(LoopModel loopModel)
        {
            var currentInputs = RefreshInputParamsFromModuleInput();

            lock (_loopOutputLock)
            {
                int loopIndex = loopModel.TransmitLoopNum;

                if (!loopModel.IsAbortLoop && loopIndex <= 1 && _lastCollectedLoopIndex != 1)
                {
                    ClearMergedOutputCache();
                }

                if (loopIndex > 0 && loopIndex == _lastCollectedLoopIndex)
                {
                    return;
                }

                foreach (var input in currentInputs)
                {
                    if (input == null)
                    {
                        continue;
                    }

                    if (!_loopOutputValues.TryGetValue(input.Guid, out var values))
                    {
                        values = new List<object>();
                        _loopOutputValues[input.Guid] = values;
                    }

                    values.Add(CloneValue(input.Value));
                    _loopOutputTemplates[input.Guid] = CloneTransmitParam(input);
                }

                _lastCollectedLoopIndex = loopIndex > 0 ? loopIndex : _lastCollectedLoopIndex + 1;
            }
        }

        private ObservableCollection<TransmitParam> RefreshInputParamsFromModuleInput()
        {
            var inputs = new ObservableCollection<TransmitParam>();
            var transmitParams = moduleInputParam?.TransmitParams;
            if (transmitParams == null)
            {
                InputParams = inputs;
                return inputs;
            }

            foreach (var item in transmitParams)
            {
                if (item.Value is TransmitParam transmitParam)
                {
                    var cloned = CloneTransmitParam(transmitParam);
                    cloned.Resourece = ResoureceType.Inupt;
                    inputs.Add(cloned);
                    continue;
                }

                inputs.Add(new TransmitParam
                {
                    Guid = Guid.TryParse(item.Key, out var guid) ? guid : Guid.NewGuid(),
                    Serial = Serial,
                    ParentNode = Name,
                    Name = item.Key,
                    ParamName = item.Key,
                    Type = InferDataType(item.Value),
                    Value = CloneValue(item.Value),
                    Describe = "上一节点输出",
                    Resourece = ResoureceType.Inupt,
                    ResourcePath = item.Key
                });
            }

            InputParams = inputs;
            return inputs;
        }

        private void FlushMergedOutputParams()
        {
            ObservableCollection<TransmitParam> finalOutputs;

            lock (_loopOutputLock)
            {
                finalOutputs = new ObservableCollection<TransmitParam>();
                var outputGuids = _loopOutputValues.Keys.ToList();
                var configuredOutputs = OutputParams?.ToList() ?? new List<TransmitParam>();

                foreach (var guid in outputGuids)
                {
                    var configured = configuredOutputs.FirstOrDefault(item => item.Guid == guid);
                    _loopOutputTemplates.TryGetValue(guid, out var template);
                    _loopOutputValues.TryGetValue(guid, out var values);

                    if (template == null)
                    {
                        continue;
                    }

                    var source = configured ?? template;

                    finalOutputs.Add(new TransmitParam
                    {
                        Guid = template.Guid,
                        LinkGuid = template.LinkGuid,
                        Serial = Serial,
                        ParentNode = Name,
                        Name = string.IsNullOrWhiteSpace(source.Name) ? template.Name : source.Name,
                        ParamName = string.IsNullOrWhiteSpace(template.ParamName) ? template.Name : template.ParamName,
                        Type = DataType.List,
                        Value = values?.Select(CloneValue).ToList() ?? new List<object>(),
                        Describe = string.IsNullOrWhiteSpace(source.Describe)
                            ? $"循环合并输出：{template.Name}"
                            : source.Describe,
                        IsGlobal = source.IsGlobal,
                        ResourcePath = template.ResourcePath,
                        Resourece = ResoureceType.Output
                    });
                }

                ClearMergedOutputCache();
            }

            OutputParams = finalOutputs;
            if (!UpdateParam())
            {
                Console.WriteLine($"模块_{Serial}更新循环合并输出失败");
            }
        }

        private void ClearMergedOutputCache()
        {
            _loopOutputValues.Clear();
            _loopOutputTemplates.Clear();
            _lastCollectedLoopIndex = 0;
        }

        private static TransmitParam CloneTransmitParam(TransmitParam source)
        {
            if (source == null)
            {
                return null;
            }

            return new TransmitParam
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode,
                Guid = source.Guid,
                Resourece = source.Resourece,
                Name = source.Name,
                ParamName = source.ParamName,
                Type = source.Type,
                Value = CloneValue(source.Value),
                Describe = source.Describe,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath
            };
        }

        private static object CloneValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return value.DeepCopy();
            }
            catch
            {
                return value;
            }
        }

        private static DataType InferDataType(object value)
        {
            if (value == null)
            {
                return DataType.None;
            }

            return value switch
            {
                int => DataType.Int,
                string => DataType.String,
                bool => DataType.Bool,
                double => DataType.Double,
                float => DataType.Double,
                decimal => DataType.Double,
                DateTime => DataType.Datetime,
                IDictionary => DataType.Dict,
                Array => DataType.Array,
                IEnumerable when value is not string => DataType.List,
                _ => DataType.Object
            };
        }

        #endregion
    }
}

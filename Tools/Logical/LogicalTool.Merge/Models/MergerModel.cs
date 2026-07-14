using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogicalTool.Merge.Models
{
    /// <summary>
    /// 合并触发模式
    /// </summary>
    public enum MergeTriggerMode
    {
        /// <summary>
        /// 任意一个输入节点触发即执行
        /// </summary>
        AnyOne,
        /// <summary>
        /// 所有输入节点都触发后才执行
        /// </summary>
        All,
    }

    [Serializable]
    public class MergerModel : ModelParamBase
    {
        #region Fields

        [JsonIgnore]
        public bool IsLoopFlag { get; set; }

        #endregion

        #region Properties

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private MergeTriggerMode _triggerMode = MergeTriggerMode.All;
        /// <summary>
        /// 合并触发模式：AnyOne-任意一个触发即执行，All-所有节点都触发后执行
        /// </summary>
        public MergeTriggerMode TriggerMode
        {
            get => _triggerMode;
            set { _triggerMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _waitTimeoutSeconds = 60;
        /// <summary>
        /// 等待超时时间（秒），默认60秒。
        /// 在All模式下，等待所有输入节点完成的最大时间。
        /// </summary>
        public int WaitTimeoutSeconds
        {
            get => _waitTimeoutSeconds;
            set { _waitTimeoutSeconds = value; RaisePropertyChanged(); }
        }

        [OutputParam("ListResults", "合并输出结果")]
        public ObservableCollection<TransmitParam> MergerResult { get; set; }
        #endregion

        #region Constructor

        public MergerModel()
        {
            TriggerModuleRun = () => ExecuteModule().GetAwaiter().GetResult();
        }

        #endregion

        #region Override

        [OnDeserialized]
        internal void OnDeserializedMethod1(StreamingContext context)
        {
            var key = Serial.ToString("D3");

            if (PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(key) == false) 
            {
                PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(key, this);
            }
            
        }
        #endregion

        #region Methods

        /// <summary>
        /// 模块执行
        /// 触发时机：由NodeViewModel保证所有前驱节点都已完成后才触发本方法。
        /// InputNodeStatus 在 ExecuteMulti 中已从 LastNodes 收集完毕，此处直接判断即可。
        /// </summary>
        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            try
            {
                var (result, time) = SetTimeHelper.SetTimer(() =>
                {
                    if (InputNodeStatus == null || InputNodeStatus.Count == 0)
                    {
                        LogMergeWarning($"Merge_{Serial}: no input node status, pass directly.");
                        return NodeStatus.Success;
                    }
                    // 打印所有前驱节点状态
                    foreach (var s in InputNodeStatus)
                    {
                        LogMergeInfo($"Merge_{Serial}: previous node {s.Item1}, status={s.Item2}");
                    }

                    // 判断是否有失败的前驱节点
                    bool anyFailed = InputNodeStatus.Any(s =>
                        s.Item2 == NodeStatus.Failed
                        || s.Item2 == NodeStatus.Error
                        || s.Item2 == NodeStatus.Timeout
                        || s.Item2 == NodeStatus.Cancelled
                        || s.Item2 == NodeStatus.Stopped
                        || s.Item2 == NodeStatus.Aborted
                        || s.Item2 == NodeStatus.NoParam);

                    if (anyFailed)
                    {
                        LogMergeWarning($"Merge_{Serial}: failed previous node exists.");
                        return NodeStatus.Failed;
                    }

                    LogMergeInfo($"Merge_{Serial}: all previous nodes are ready.");
                    return NodeStatus.Success;
                });

                LogMergeInfo($"Merge_{Serial}: status calculation cost={time}ms.");


                #region 输出

                Dictionary<string, object> outputSnapshot = BuildRuntimeOutputSnapshot();
                OutputParams = BuildOutputParams(outputSnapshot);
                PublishOutputSnapshot(outputSnapshot);

                if (OutputParams.Count == 0)
                {
                    LogMergeWarning($"Merge_{Serial}: runtime input is empty, output is cleared.");
                    return Task.FromResult(Output = new ExecuteModuleOutput
                    {
                        RunStatus = result,
                        RunTime = time
                    });
                }

                LogMergeInfo($"Merge_{Serial}: pass-through output count={OutputParams.Count}.");
                #endregion

                return Task.FromResult(Output = new ExecuteModuleOutput
                {
                    RunStatus = result,
                    RunTime = time
                });
            }
            catch (Exception ex)
            {
                LogMergeError($"Merge_{Serial}: execute failed: {ex}");
                return Task.FromResult(Output = new ExecuteModuleOutput
                {
                    RunStatus = NodeStatus.Error,
                    RunTime = 0.0
                });
            }
        }

        private Dictionary<string, object> BuildRuntimeOutputSnapshot()
        {
            Dictionary<string, object>? source = moduleInputParam?.TransmitParams;
            var snapshot = source == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(source.Count, source.Comparer);

            if (source != null)
            {
                try
                {
                    foreach (var pair in source.ToArray())
                    {
                        if (pair.Value == null)
                            continue;

                        string key = EnsureUniqueOutputKey(snapshot, pair.Key, snapshot.Count);
                        snapshot[key] = pair.Value;
                    }
                }
                catch (Exception ex)
                {
                    LogMergeWarning($"Merge_{Serial}: snapshot runtime input failed: {ex.Message}");
                }
            }

            if (snapshot.Count == 0 && InputParams != null)
            {
                foreach (var param in InputParams.Where(item => item != null))
                {
                    string key = EnsureUniqueOutputKey(snapshot, param.Guid.ToString(), snapshot.Count);
                    snapshot[key] = param;
                }
            }

            return snapshot;
        }

        private static ObservableCollection<TransmitParam> BuildOutputParams(Dictionary<string, object> outputSnapshot)
        {
            var outputParams = outputSnapshot?.Values?
                .OfType<TransmitParam>()
                .Where(item => item != null)
                .ToList() ?? new List<TransmitParam>();

            return new ObservableCollection<TransmitParam>(outputParams);
        }

        private void PublishOutputSnapshot(Dictionary<string, object> outputSnapshot)
        {
            moduleOutputParam ??= new ModuleParam();
            moduleOutputParam.TransmitParams = outputSnapshot ?? new Dictionary<string, object>();

            try
            {
                var outputCache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
                if (outputCache != null)
                {
                    outputCache[Serial.ToString()] = OutputParams ?? new ObservableCollection<TransmitParam>();
                }
            }
            catch (Exception ex)
            {
                LogMergeWarning($"Merge_{Serial}: update output cache failed: {ex.Message}");
            }
        }

        private static string EnsureUniqueOutputKey(Dictionary<string, object> snapshot, string key, int index)
        {
            string normalized = string.IsNullOrWhiteSpace(key) ? $"merge-output-{index}" : key;
            if (!snapshot.ContainsKey(normalized))
                return normalized;

            int suffix = 1;
            string candidate;
            do
            {
                candidate = $"{normalized}_{suffix++}";
            }
            while (snapshot.ContainsKey(candidate));

            return candidate;
        }

        private void SyncRuntimeInputParams()
        {
            try
            {
                var runtimeParams = moduleInputParam?.TransmitParams?.Values?
                    .OfType<TransmitParam>()
                    .Where(item => item != null)
                    .ToList();

                if (runtimeParams != null && runtimeParams.Count > 0)
                {
                    InputParams = new ObservableCollection<TransmitParam>(runtimeParams);
                }
            }
            catch (Exception ex)
            {
                LogMergeWarning($"Merge_{Serial}: sync runtime input failed: {ex.Message}");
            }
        }




        public void SupplementDefectPostProcessResultsByImage()
        {
            try
            {
                InputParams ??= new ObservableCollection<TransmitParam>();
                OutputParams ??= new ObservableCollection<TransmitParam>();

                var sourceResultsParams = InputParams
                    .Concat(OutputParams)
                    .Where(IsDefectPostProcessResultsParam)
                    .GroupBy(BuildSourceIdentity, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

                foreach (var resultParam in sourceResultsParams)
                {
                    var resultsByImageParam = ResolveResultsByImageParam(resultParam);
                    if (resultsByImageParam == null)
                        continue;

                    AddParamIfMissing(InputParams, resultsByImageParam);

                    bool sourceIsPublished = OutputParams.Any(item => IsSameSource(item, resultParam) && IsDefectPostProcessResultsParam(item));
                    if (sourceIsPublished)
                        AddParamIfMissing(OutputParams, CloneTransmitParam(resultsByImageParam));
                }
            }
            catch (Exception ex)
            {
                LogMergeWarning($"Merge_{Serial}: supplement ResultsByImage failed: {ex.Message}");
            }
        }

        private static void LogMergeInfo(string message)
        {
            try
            {
                Logs.LogInfo(message);
            }
            catch
            {
            }
        }

        private static void LogMergeWarning(string message)
        {
            try
            {
                Logs.LogWarning(message);
            }
            catch
            {
            }
        }

        private static void LogMergeError(string message)
        {
            try
            {
                Logs.LogError(message);
            }
            catch
            {
            }
        }

        private TransmitParam ResolveResultsByImageParam(TransmitParam resultParam)
        {
            var existing = InputParams
                .Concat(OutputParams)
                .FirstOrDefault(item => IsSameSource(item, resultParam) && IsResultsByImageParam(item));
            if (existing != null)
                return CloneTransmitParam(existing);

            foreach (var param in EnumerateSourceTransmitParams(resultParam.Serial))
            {
                if (IsSameSource(param, resultParam) && IsResultsByImageParam(param))
                    return CloneTransmitParam(param);
            }

            object sourceModel = FindSourceModel(resultParam.Serial);
            if (sourceModel == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = sourceModel.GetType().GetProperty("ResultsByImage", flags);
            if (property == null)
                return null;

            object value = property.GetValue(sourceModel);
            var resource = FindOutputResourceParam(sourceModel, "ResultsByImage");
            TransmitParam resourceParam = resource.Param;

            return new TransmitParam
            {
                Guid = resourceParam != null && resourceParam.Guid != Guid.Empty ? resourceParam.Guid : Guid.NewGuid(),
                LinkGuid = resultParam.LinkGuid != Guid.Empty ? resultParam.LinkGuid : resourceParam?.LinkGuid ?? Guid.Empty,
                Serial = resultParam.Serial,
                ParentNode = string.IsNullOrWhiteSpace(resultParam.ParentNode) ? resourceParam?.ParentNode : resultParam.ParentNode,
                Name = string.IsNullOrWhiteSpace(resource.Key) ? "ResultsByImage" : resource.Key,
                ParamName = "ResultsByImage",
                Type = resourceParam?.Type ?? DataType._object,
                Value = value ?? resourceParam?.Value,
                Describe = string.IsNullOrWhiteSpace(resourceParam?.Describe) ? "Defect results grouped by image." : resourceParam.Describe,
                IsGlobal = false,
                Resourece = resultParam.Resourece == ResoureceType.None ? ResoureceType.Output : resultParam.Resourece,
                ResourcePath = string.IsNullOrWhiteSpace(resourceParam?.ResourcePath)
                    ? $"{sourceModel.GetType().FullName}.ResultsByImage"
                    : resourceParam.ResourcePath
            };
        }

        private IEnumerable<TransmitParam> EnumerateSourceTransmitParams(int sourceSerial)
        {
            if (sourceSerial < 0)
                yield break;

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem == null)
                yield break;

            foreach (string key in EnumerateSourceKeys(sourceSerial))
            {
                if (solutionItem.NodesOutputCache != null
                    && solutionItem.NodesOutputCache.TryGetValue(key, out ObservableCollection<TransmitParam> outputParams))
                {
                    foreach (var param in outputParams ?? Enumerable.Empty<TransmitParam>())
                    {
                        if (param != null)
                            yield return param;
                    }
                }

                if (solutionItem.NodeParamCaches != null
                    && solutionItem.NodeParamCaches.TryGetValue(key, out object model)
                    && model is ModelParamBase modelParam)
                {
                    foreach (var param in modelParam.OutputParams ?? Enumerable.Empty<TransmitParam>())
                    {
                        if (param != null)
                            yield return param;
                    }

                    foreach (var param in modelParam.moduleOutputParam?.TransmitParams?.Values?.OfType<TransmitParam>() ?? Enumerable.Empty<TransmitParam>())
                    {
                        if (param != null)
                            yield return param;
                    }
                }
            }
        }

        private static object FindSourceModel(int sourceSerial)
        {
            if (sourceSerial < 0)
                return null;

            var caches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
            if (caches == null || caches.Count == 0)
                return null;

            foreach (string key in EnumerateSourceKeys(sourceSerial))
            {
                if (caches.TryGetValue(key, out object model) && model != null)
                    return model;
            }

            return null;
        }

        private static IEnumerable<string> EnumerateSourceKeys(int sourceSerial)
        {
            yield return sourceSerial.ToString();
            yield return sourceSerial.ToString("D3");
        }

        private static (string Key, TransmitParam Param) FindOutputResourceParam(object sourceModel, string paramName)
        {
            if (sourceModel == null || string.IsNullOrWhiteSpace(paramName))
                return default;

            PropertyInfo property = sourceModel.GetType().GetProperty(
                "OutputParamResource",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(sourceModel) is not IDictionary dictionary)
                return default;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is not TransmitParam param)
                    continue;

                if (!IsNamedParam(param, paramName))
                    continue;

                return (entry.Key?.ToString(), param);
            }

            return default;
        }

        private static void AddParamIfMissing(ObservableCollection<TransmitParam> target, TransmitParam param)
        {
            if (target == null || param == null)
                return;

            if (target.Any(item => IsSameSource(item, param) && IsResultsByImageParam(item)))
                return;

            target.Add(CloneTransmitParam(param));
        }

        private static bool IsDefectPostProcessResultsParam(TransmitParam param)
        {
            if (param == null || !IsNamedParam(param, "Results"))
                return false;

            string sourceText = $"{param.ParentNode} {param.ResourcePath}";
            return sourceText.Contains("DefectPostProcess", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsResultsByImageParam(TransmitParam param)
        {
            return IsNamedParam(param, "ResultsByImage");
        }

        private static bool IsNamedParam(TransmitParam param, string paramName)
        {
            if (param == null || string.IsNullOrWhiteSpace(paramName))
                return false;

            return string.Equals(param.ParamName, paramName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(param.Name, paramName, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(param.Name)
                    && param.Name.StartsWith($"{paramName}[", StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(param.ResourcePath)
                    && param.ResourcePath.EndsWith($".{paramName}", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSameSource(TransmitParam left, TransmitParam right)
        {
            return string.Equals(BuildSourceIdentity(left), BuildSourceIdentity(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSourceIdentity(TransmitParam param)
        {
            if (param == null)
                return string.Empty;

            if (param.Serial >= 0)
                return param.Serial.ToString();

            if (!string.IsNullOrWhiteSpace(param.ParentNode))
                return param.ParentNode;

            return param.LinkGuid == Guid.Empty ? string.Empty : param.LinkGuid.ToString();
        }

        private static TransmitParam CloneTransmitParam(TransmitParam source)
        {
            if (source == null)
                return null;

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
                Value = source.Value,
                Describe = source.Describe,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath
            };
        }

        #endregion
    }
}

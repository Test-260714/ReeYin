using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Core.Interfaces
{
    /// <summary>
    /// 模块内部参数接口
    /// </summary>
    public interface IModuleParam : IDisposable
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Serial { get; set; }

        /// <summary>
        /// 模块输入参数
        /// </summary>
        ModuleParam moduleInputParam { get; set; }

        /// <summary>
        /// 模块输出参数
        /// </summary>
        ModuleParam moduleOutputParam { get; set; }

        /// <summary>
        /// 输入们节点状态
        /// 
        /// </summary>
        List<(int,NodeStatus)> InputNodeStatus { get; set; }

        /// <summary>
        /// 触发模块运行
        /// </summary>
        [JsonIgnore]
        public Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<ExecuteModuleOutput> ExecuteModule();

        /// <summary>
        /// 执行模块输出
        /// </summary>
        public ExecuteModuleOutput Output { get; set; }


        public void Dispose();
    }

    /// <summary>
    /// 视图模块参数接口
    /// </summary>
    public interface IViewModuleParam
    {
        //public virtual void Init();

        //bool TransferParam(CollectImageModel ModelParam);
    }

    /// <summary>
    /// Model实体用来传递参数
    /// </summary>
    public class ModuleParamBase :BindableBase, IModuleParam
    {
        public virtual int Serial { get; set; }

        public virtual ModuleParam moduleInputParam { get; set; } = new ModuleParam();
        public virtual ModuleParam moduleOutputParam { get ; set ; } = new ModuleParam();

        [JsonIgnore]
        public virtual Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = new Func<ExecuteModuleOutput>(() =>
        {
            MessageBox.Show("请双击模块进行参数配置！！！");
            return new ExecuteModuleOutput() 
            {
                RunStatus = NodeStatus.NoParam,

            };
        });

        [JsonIgnore]
        public ExecuteModuleOutput Output { get; set ; }

        public Guid Guid { get; set; } = Guid.NewGuid();
        public List<(int, NodeStatus)> InputNodeStatus { get; set; } = new List<(int, NodeStatus)>();

        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            return new Task<ExecuteModuleOutput>(() =>
            {
                return TriggerModuleRun();
            });

        }

        /// <summary>
        /// 更新参数
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public bool UpdateParam(ObservableCollection<TransmitParam> OutputParams)
        {
            try
            {
                #region 输出
                PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()] = OutputParams.DeepClone();

                foreach (var item in OutputParams)
                {
                    if (item.IsGlobal)
                    {
                        // 一次性查询：获取Guid匹配的对象（不存在则为null）
                        var existingParam = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams
                            .FirstOrDefault(p => p.Guid == item.Guid && p.Name == item.Name);

                        if (existingParam == null)
                        {
                            // 不存在则添加
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
                        }
                        else
                        {
                            // 存在则更新值
                            existingParam.Value = item.Value;
                        }
                    }
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新参数失败: {ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public virtual bool LoadKeyParam() { return true; }

        public virtual void DrogInit()
        {
            
        }

        public void Dispose()
        {

        }
    }

    /// <summary>
    /// Model基类
    /// </summary>
    [Serializable]
    public abstract class ModelParamBase : BindableBase, IModuleParam
    {
        private const string OutputParamAttributeFullName = "ReeYin_V.Share.OutputParamAttribute";

        #region Fields
        [JsonIgnore]
        public bool IsDebug { get; set; } = false;

        public object _Lockobj = new object();

        [JsonIgnore]
        public static string ModuleName = "";

        [JsonIgnore]
        public virtual VMHWindowControl mWindowH {  get; set; }

        #endregion

        #region Properties
        [JsonIgnore]
        private List<string> _outputParamNames = new List<string>();

        [JsonIgnore]
        public List<string> OutputParamNames
        {
            get { return _outputParamNames; }
            set { _outputParamNames = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _OutputParams = new ObservableCollection<TransmitParam>();

        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _OutputParams; }
            set { _OutputParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _InputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 上一节点输入
        /// </summary>
        public ObservableCollection<TransmitParam> InputParams
        {
            get { return _InputParams; }
            set { _InputParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<RecipeParamInfo> _RecipeParams = new ObservableCollection<RecipeParamInfo>();
        /// <summary>
        /// 配方参数
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RecipeParamInfo> RecipeParams
        {
            get { return _RecipeParams; }
            set { _RecipeParams = value ?? new ObservableCollection<RecipeParamInfo>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 输出参数资源
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> OutputParamResource = new Dictionary<string, object>();

        [JsonIgnore]
        public virtual Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public int Serial { get; set; } = -999;

        public virtual string Name { get; set; }

        public virtual ModuleParam moduleInputParam { get; set; } = new ModuleParam();

        public virtual ModuleParam moduleOutputParam { get; set; } = new ModuleParam();


        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        //用来显示模块输出状态
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }

        public Guid Guid { get; set; } = Guid.NewGuid();

        public List<(int, NodeStatus)> InputNodeStatus { get; set; } = new List<(int, NodeStatus)>();

        private SubscriptionToken _NodifyRemoveNodeToken;
        private readonly object _disposeSyncRoot = new object();
        private bool _isDisposed;
        #endregion

        #region Constructor
        protected ModelParamBase()
        {

        }
        #endregion

        #region Methods
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            ModelParamCompatibilityDefaults.Normalize(this);
            OnceInit();

        }

        [JsonIgnore]
        public bool IsOnceInit = false;

        /// <summary>
        /// 整个程序运行周期内只做一次
        /// </summary>
        /// <returns></returns>
        public virtual bool OnceInit()
        {
            EnsureNodifyRemoveNodeSubscription();

            if (Serial >= 0)
            {
                PrismProvider.ProjectManager?.AddNodeParamCache(this);
            }

            SyncRecipeParams();

            return true;
        }

        private void EnsureNodifyRemoveNodeSubscription()
        {
            if (_NodifyRemoveNodeToken != null || PrismProvider.EventAggregator == null)
            {
                return;
            }

            _NodifyRemoveNodeToken = PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>()
                .Subscribe(OnNodifyRemoveNode, ThreadOption.UIThread);
        }

        private void OnNodifyRemoveNode(string order)
        {
            if (order != Serial.ToString())
            {
                return;
            }

            Dispose();
        }

        private void UnsubscribeNodifyRemoveNode()
        {
            if (_NodifyRemoveNodeToken == null)
            {
                return;
            }

            if (PrismProvider.EventAggregator != null)
            {
                PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Unsubscribe(_NodifyRemoveNodeToken);
            }

            _NodifyRemoveNodeToken = null;
        }

        /// <summary>
        /// 根据 OutputParam 特性初始化输出参数资源。
        /// </summary>
        /// <param name="linkGuid"></param>
        public virtual void InitOutputParamResource(Guid linkGuid)
        {
            OutputParamResource.Clear();

            foreach (var point in GetOutputParamDefinitions())
            {
                OutputParamResource[GetOutputParamResourceKey(point.Name, point.Description)] = new TransmitParam
                {
                    LinkGuid = linkGuid,
                    Name = point.Name,
                    Type = DataType._object,
                    Resourece = ResoureceType.None,
                    Value = point.GetValue(this),
                    Describe = point.Description,
                    ResourcePath = GetOutputParamResourcePath(point.DeclaringType, point.Name)
                };
            }
        }

        protected virtual string GetOutputParamResourceKey(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return name ?? string.Empty;
            }

            return $"{name}[{description}]";
        }

        protected virtual string GetOutputParamResourcePath(Type declaringType, string name)
        {
            if (declaringType == null)
            {
                return name ?? string.Empty;
            }

            return $"{declaringType.FullName}.{name}";
        }

        private IEnumerable<OutputParamDefinition> GetOutputParamDefinitions()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (FieldInfo field in GetType().GetFields(flags))
            {
                OutputParamDefinition definition = CreateOutputParamDefinition(field);
                if (definition != null)
                {
                    yield return definition;
                }
            }

            foreach (PropertyInfo property in GetType().GetProperties(flags))
            {
                if (!property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                OutputParamDefinition definition = CreateOutputParamDefinition(property);
                if (definition != null)
                {
                    yield return definition;
                }
            }
        }

        private OutputParamDefinition CreateOutputParamDefinition(MemberInfo member)
        {
            object attribute = member.GetCustomAttributes(true)
                .FirstOrDefault(item => item.GetType().FullName == OutputParamAttributeFullName);
            if (attribute == null)
            {
                return null;
            }

            Type attributeType = attribute.GetType();
            string name = attributeType.GetProperty("Name")?.GetValue(attribute) as string;
            string description = attributeType.GetProperty("Description")?.GetValue(attribute) as string;

            return new OutputParamDefinition
            {
                Name = string.IsNullOrWhiteSpace(name) ? member.Name : name,
                Description = description,
                DeclaringType = member.DeclaringType,
                GetValue = member switch
                {
                    FieldInfo field => model => field.GetValue(model),
                    PropertyInfo property => model => property.GetValue(model),
                    _ => _ => null
                }
            };
        }

        private sealed class OutputParamDefinition
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public Type DeclaringType { get; set; }

            public Func<ModelParamBase, object> GetValue { get; set; }
        }

        /// <summary>
        /// 收集并同步本模型的配方参数至配方存储。
        /// RecipeParamService.NormalizeRecipeParams 内部已负责填充 Serial/Subjection/RecipeKey，无需在此重复处理。
        /// </summary>
        protected virtual bool SyncRecipeParams()
        {
            try
            {
                List<RecipeParamInfo> infos = RecipeParamService.GetMarkedParams(this);
                foreach (var item in infos)
                {
                    item.Serial = Serial;
                    item.Subjection = Name ?? "未命名";
                }
                RecipeParams = new ObservableCollection<RecipeParamInfo>(infos);

                // 负数 Serial 表示模型尚未挂接到真实节点，不能写入配方分组。
                if (Serial < 0)
                {
                    return true;
                }

                return RecipeParamService.SyncRecipeParams(this, infos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步配方参数失败: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 从当前配方中读取参数值并应用到本模型。
        /// </summary>
        protected virtual bool ApplyRecipeParamValues()
        {
            try
            {
                if (RecipeParams == null || RecipeParams.Count == 0)
                    RecipeParams = new ObservableCollection<RecipeParamInfo>(RecipeParamService.GetMarkedParams(this));

                return RecipeParamService.ApplyRecipeParams(this, RecipeParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配方参数失败: {ex.StackTrace}");
                return false;
            }
        }

        protected virtual object GetMarkedInputParamValue(string name, bool? isDC = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            InputParamDefinition definition = GetInputParamDefinitions().FirstOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                return null;
            }

            TransmitParam param = definition.GetValue(this);
            if (param == null)
            {
                return null;
            }

            bool needDeepCopy = isDC ?? definition.NeedDeepCopy;
            object value = ResolveTransmitParamValue(InputParams, param, needDeepCopy);
            param.Value = value;
            return value;
        }

        protected virtual bool SyncMarkedInputParamValues(bool? isDC = null)
        {
            try
            {
                foreach (InputParamDefinition definition in GetInputParamDefinitions())
                {
                    TransmitParam param = definition.GetValue(this);
                    if (param == null)
                    {
                        continue;
                    }

                    bool needDeepCopy = isDC ?? definition.NeedDeepCopy;
                    param.Value = ResolveTransmitParamValue(InputParams, param, needDeepCopy);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步输入参数失败: {ex.StackTrace}");
                return false;
            }
        }

        private IEnumerable<InputParamDefinition> GetInputParamDefinitions()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (FieldInfo field in GetType().GetFields(flags))
            {
                InputParamDefinition definition = CreateInputParamDefinition(field);
                if (definition != null)
                {
                    yield return definition;
                }
            }

            foreach (PropertyInfo property in GetType().GetProperties(flags))
            {
                if (!typeof(TransmitParam).IsAssignableFrom(property.PropertyType) || !property.CanRead)
                {
                    continue;
                }

                InputParamDefinition definition = CreateInputParamDefinition(property);
                if (definition != null)
                {
                    yield return definition;
                }
            }
        }

        private InputParamDefinition CreateInputParamDefinition(MemberInfo member)
        {
            if (member == null)
            {
                return null;
            }

            if (member is FieldInfo field && !typeof(TransmitParam).IsAssignableFrom(field.FieldType))
            {
                return null;
            }

            if (member is PropertyInfo property && !typeof(TransmitParam).IsAssignableFrom(property.PropertyType))
            {
                return null;
            }

            InputParamAttribute attribute = member.GetCustomAttribute<InputParamAttribute>();
            if (attribute == null)
            {
                return null;
            }

            return new InputParamDefinition
            {
                Name = string.IsNullOrWhiteSpace(attribute.Name) ? member.Name : attribute.Name,
                Description = attribute.Description,
                NeedDeepCopy = attribute.NeedDeepCopy,
                GetValue = member switch
                {
                    FieldInfo fieldInfo => model => fieldInfo.GetValue(model) as TransmitParam,
                    PropertyInfo propertyInfo => model => propertyInfo.GetValue(model) as TransmitParam,
                    _ => _ => null
                }
            };
        }

        private sealed class InputParamDefinition
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool NeedDeepCopy { get; set; }

            public Func<ModelParamBase, TransmitParam> GetValue { get; set; }
        }

        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            return new Task<ExecuteModuleOutput>(() =>
            {
                ApplyRecipeParamValues();
                return TriggerModuleRun();
            });

        }

        /// <summary>
        /// 更新参数
        /// 模块/全局/缓存
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public virtual bool UpdateParam()
        {
            try
            {
                var temp = OutputParams;

                //更新模块输出参数
                moduleOutputParam.TransmitParams.Clear();
                moduleOutputParam.TransmitParams = OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item); ;

                #region 输出
                PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()] = temp;

                foreach (var item in temp)
                {
                    if (item.IsGlobal)
                    {
                        var existingParam = FindExistingGlobalParam(
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams,
                            item);

                        if (existingParam == null)
                        {
                            PrismProvider.Dispatcher.BeginInvoke(() =>
                            {
                                // 不存在则添加
                                PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
                            });

                        }
                        else
                        {
                            if (item.Value is HObject)
                            {
                                if ((item.Value as HObject) != null && (item.Value as HObject).IsInitialized())
                                    existingParam.Value = (item.Value as HObject).Clone();
                            }
                            else
                                // 存在则更新值
                                existingParam.Value = item.Value;
                        }
                    }
                }

                //移除为空的项目
                var list = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Where(p => p.Serial == Serial && p.Value == null).ToList();

                foreach (var item in list)
                {
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(item);
                }
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新参数失败: {ex.StackTrace}");
                return false;
            }
        }

        private static TransmitParam FindExistingGlobalParam(
            ObservableCollection<TransmitParam> globalParams,
            TransmitParam item)
        {
            if (globalParams == null || item == null)
            {
                return null;
            }

            return globalParams.FirstOrDefault(p => p.Guid == item.Guid)
                ?? globalParams.FirstOrDefault(p => p.Serial == item.Serial && p.Name == item.Name);
        }

        /// <summary>
        /// 获取转换的参数
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public object GetTransmitParam(ObservableCollection<TransmitParam> InputParams, TransmitParam param,bool IsDC = true)
        {
            try
            {
                object value = ResolveTransmitParamValue(InputParams, param, IsDC);
                if (param != null)
                {
                    param.Value = value;
                }

                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取参数失败: {ex.StackTrace}");
                return null;
            }
        }

        protected virtual object ResolveTransmitParamValue(
            ObservableCollection<TransmitParam> InputParams,
            TransmitParam param,
            bool IsDC = true
        )
        {
            try
            {
                lock (_Lockobj)
                {
                    if (param == null)
                        return null;

                    switch (param.Resourece)
                    {
                        case ResoureceType.CustomGlobal:
                        {
                            return PrismProvider.ProjectManager.SltCurSolutionItem.CustomGlobalParams
                                .FirstOrDefault(p => p.Guid == param.Guid)?.Value.DeepClone();
                        }
                        case ResoureceType.Global:
                        {
                            var temp = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams
                                .FirstOrDefault(p => p.Guid == param.Guid);
                            if (temp == null) return null;
                            var tempvalue = temp.Value;
                            if (IsDC)
                                return tempvalue is HObject ho ? ho.Clone() : tempvalue.DeepClone();
                            else
                                return tempvalue;
                        }
                        case ResoureceType.Inupt:
                        case ResoureceType.LastInput:
                        {
                            // Inupt 和 LastInput 的取值逻辑完全相同，合并处理
                            var temp = InputParams?.FirstOrDefault(p => p.Guid == param.Guid);
                            if (temp == null) return null;
                            var tempvalue = temp.Value;
                            if (IsDC)
                                return tempvalue is HObject ho ? ho : tempvalue.DeepClone();
                            else
                                return tempvalue;
                        }
                        case ResoureceType.None:
                        default:
                            return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取参数失败: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 转化参数（初始化输入输出参数）
        /// </summary>
        /// <returns></returns>
        protected virtual bool TransferParamCore()
        {
            try
            {
                #region 获取上一节点输入
                moduleInputParam ??= new ModuleParam();
                var before = moduleInputParam.TransmitParams ??= new Dictionary<string, object>();

                InputParams.Clear();
                foreach (var item in before.Values.OfType<TransmitParam>())
                {
                    item.Resourece = ResoureceType.Inupt;
                    InputParams.Add(item);
                }
                #endregion

                #region 当前节点的输出
                moduleOutputParam ??= new ModuleParam();
                moduleOutputParam.TransmitParams ??= new Dictionary<string, object>();

                OutputParamNames = OutputParamResource.Select(item => item.Key).ToList();
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.StackTrace);
                return false;
            }
        }

        protected virtual bool TransferParamSync()
        {
            try
            {
                if (PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess())
                {
                    return TransferParamCore();
                }

                bool result = false;
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    result = TransferParamCore();
                });
                return result;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.StackTrace);
                return false;
            }
        }

        public bool TransferParam()
        {
            if (PrismProvider.Dispatcher == null)
            {
                return TransferParamCore();
            }

            PrismProvider.Dispatcher.BeginInvoke(() =>
            {
                TransferParamCore();
            });
            return true;
        }

        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public virtual bool LoadKeyParam() 
        {
            static double ConvertToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;

            var totalStopwatch = Stopwatch.StartNew();
            double transferParamElapsedUs = 0;
            double applyRecipeElapsedUs = 0;
            double syncMarkedInputElapsedUs = 0;
            bool result = false;

            try
            {
                var stageStopwatch = Stopwatch.StartNew();
                if (!TransferParamSync())
                {
                    transferParamElapsedUs = ConvertToMicroseconds(stageStopwatch.ElapsedTicks);
                    return false;
                }
                transferParamElapsedUs = ConvertToMicroseconds(stageStopwatch.ElapsedTicks);

                stageStopwatch.Restart();
                if (!ApplyRecipeParamValues())
                {
                    applyRecipeElapsedUs = ConvertToMicroseconds(stageStopwatch.ElapsedTicks);
                    return false;
                }
                applyRecipeElapsedUs = ConvertToMicroseconds(stageStopwatch.ElapsedTicks);

                stageStopwatch.Restart();
                result = SyncMarkedInputParamValues();
                syncMarkedInputElapsedUs = ConvertToMicroseconds(stageStopwatch.ElapsedTicks);
                return result;
            }
            finally
            {
                totalStopwatch.Stop();
                Console.WriteLine($"节点{Serial} {GetType().Name}.LoadKeyParam耗时统计：总计{ConvertToMicroseconds(totalStopwatch.ElapsedTicks):F3}us，TransferParamSync={transferParamElapsedUs:F3}us，ApplyRecipeParamValues={applyRecipeElapsedUs:F3}us，SyncMarkedInputParamValues={syncMarkedInputElapsedUs:F3}us，结果={result}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            lock (_disposeSyncRoot)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                UnsubscribeNodifyRemoveNode();
            }

            RecipeParamService.RemoveRecipeParams(this, RecipeParams);
            RecipeParams.Clear();

            PrismProvider.ProjectManager?.RemoveNodeParamCache(this);
            PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Remove(this.Serial.ToString());

            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams
                    .Where(p => p.Serial != Serial)
                    .ToList().ToObservableCollection();

            if (Serial >= 0)
            {
                PrismProvider.DynamicViewManager?.RemoveDynamic(DynamicViewType.NodeMap, nodeSerial: Serial);
                Logs.LogInfo($"节点{Serial}，相关视图已被释放");


            }

        }
        #endregion
    }
 

    public class ExecuteModuleOutput : BindableBase
    {
        private NodeStatus runStatus;
        /// <summary>
        /// 执行状态
        /// </summary>
        public NodeStatus RunStatus
        {
            get { return runStatus; }
            set { runStatus = value; RaisePropertyChanged(); }
        }

        private double runTime = 0.0;
        /// <summary>
        /// 执行时间
        /// </summary>
        public double RunTime
        {
            get { return runTime; }
            set { runTime = value; RaisePropertyChanged(); }
        }

    }

    /// <summary>
    /// 模块传递参数接口
    /// </summary>
    public interface IModuleTransmitParam
    {
        /// <summary>
        /// 参数
        /// </summary>
        Dictionary<string, object> TransmitParams { get; set; }

    }

    /// <summary>
    /// 模块输入参数
    /// </summary>
    public class ModuleInputParam : BindableBase, IModuleTransmitParam
    {
        private Dictionary<string, object> transmitParams;

        public Dictionary<string, object> TransmitParams
        {
            get { return transmitParams; }
            set { transmitParams = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 模块输出参数
    /// </summary>
    public class ModuleOutputParam : BindableBase, IModuleTransmitParam
    {
        private Dictionary<string, object> transmitParams;

        public Dictionary<string, object> TransmitParams
        {
            get { return transmitParams; }
            set { transmitParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _OutputParams = new ObservableCollection<TransmitParam>();

        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _OutputParams; }
            set { _OutputParams = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 模块参数
    /// </summary>
    public class ModuleParam : BindableBase, IModuleTransmitParam
    {
        private Dictionary<string, object> transmitParams = new Dictionary<string, object>();

        public Dictionary<string, object> TransmitParams
        {
            get { return transmitParams; }
            set { transmitParams = value; RaisePropertyChanged(); }
        }
    }
}

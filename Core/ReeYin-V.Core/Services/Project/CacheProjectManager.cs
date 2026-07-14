using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Core.Services.Project
{
    public partial class ProjectManager
    {
        /// <summary>
        /// 添加或更新节点参数缓存，避免重复 Add 导致异常。
        /// </summary>
        public bool AddNodeParamCache(string cacheKey, object cacheValue)
        {
            if (string.IsNullOrWhiteSpace(cacheKey) || cacheValue == null)
            {
                return false;
            }

            var runtimeData = SltCurSolutionRuntimeData;
            runtimeData.NodeParamCaches ??= new Dictionary<string, object>();

            if (runtimeData.NodeParamCaches.TryGetValue(cacheKey, out object existValue) &&
                ReferenceEquals(existValue, cacheValue))
            {
                return true;
            }

            runtimeData.NodeParamCaches[cacheKey] = cacheValue;
            return true;
        }

        /// <summary>
        /// 按 ModelParamBase 默认规则添加或更新节点参数缓存。
        /// </summary>
        public bool AddNodeParamCache(ModelParamBase modelParam, string cacheKey = null)
        {
            if (modelParam == null)
            {
                return false;
            }

            string finalCacheKey = ResolveNodeParamCacheKey(modelParam, cacheKey);
            if (string.IsNullOrWhiteSpace(finalCacheKey))
            {
                return false;
            }

            return AddNodeParamCache(finalCacheKey, modelParam);
        }

        /// <summary>
        /// 获取节点参数缓存，获取失败时返回 null。
        /// </summary>
        public object GetNodeParamCacheValue(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return null;
            }

            var caches = SltCurSolutionRuntimeData?.NodeParamCaches;
            if (caches == null || caches.Count == 0)
            {
                return null;
            }

            if (!caches.TryGetValue(cacheKey, out object value) || value == null)
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// 按指定类型获取节点参数缓存，获取失败或类型不匹配时返回 default。
        /// </summary>
        public T GetNodeParamCacheValue<T>(string cacheKey)
        {
            object value = GetNodeParamCacheValue(cacheKey);
            if (value is not T typedValue)
            {
                return default;
            }

            return typedValue;
        }

        /// <summary>
        /// 根据缓存键移除节点参数缓存。
        /// </summary>
        public bool RemoveNodeParamCache(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return false;
            }

            var caches = SltCurSolutionRuntimeData?.NodeParamCaches;
            if (caches == null || caches.Count == 0)
            {
                return false;
            }

            return caches.Remove(cacheKey);
        }

        /// <summary>
        /// 根据 ModelParamBase 移除节点参数缓存，并清理同实例的残留键。
        /// </summary>
        public bool RemoveNodeParamCache(ModelParamBase modelParam, string cacheKey = null)
        {
            if (modelParam == null && string.IsNullOrWhiteSpace(cacheKey))
            {
                return false;
            }

            var caches = SltCurSolutionRuntimeData?.NodeParamCaches;
            if (caches == null || caches.Count == 0)
            {
                return false;
            }

            bool isRemoved = false;
            string finalCacheKey = ResolveNodeParamCacheKey(modelParam, cacheKey);
            if (!string.IsNullOrWhiteSpace(finalCacheKey))
            {
                isRemoved = caches.Remove(finalCacheKey);
            }

            if (modelParam == null)
            {
                return isRemoved;
            }

            List<string> duplicateKeys = caches
                .Where(item => ReferenceEquals(item.Value, modelParam))
                .Select(item => item.Key)
                .ToList();

            foreach (string duplicateKey in duplicateKeys)
            {
                isRemoved = caches.Remove(duplicateKey) || isRemoved;
            }

            return isRemoved;
        }

        private static string ResolveNodeParamCacheKey(ModelParamBase modelParam, string cacheKey = null)
        {
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                return cacheKey;
            }

            if (modelParam == null || modelParam.Serial < 0)
            {
                return string.Empty;
            }

            return modelParam.Serial.ToString("D3");
        }
    }

    /// <summary>
    /// Nodify解决方案管理器
    /// </summary>
    [Serializable]
    public class NodifySolutionManagerModel : BindableBase
    {
        #region Fields
        /// <summary>
        /// 启动项目
        /// </summary>
        public NodifySolutionItem InitiateSolution = null;
        #endregion

        #region Properties

        [JsonIgnore]
        private ObservableCollection<NodifySolutionItem> _solutionItems = new ObservableCollection<NodifySolutionItem>();

        public ObservableCollection<NodifySolutionItem> SolutionItems
        {
            get { return _solutionItems; }
            set { _solutionItems = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public NodifySolutionManagerModel()
        {

        }
        #endregion

        #region Methods



        #endregion


    }

    /// <summary>
    /// 解决方案项目
    /// </summary>
    [Serializable]
    public class NodifySolutionItem : BindableBase
    {
        [JsonIgnore]
        private readonly NodifySolutionRuntimeData _fallbackRuntimeData = new NodifySolutionRuntimeData();

        #region Properties
        [JsonIgnore]
        private NodifySolutionRuntimeData RuntimeData =>
            PrismProvider.ProjectManager?.SltCurSolutionRuntimeData ?? _fallbackRuntimeData;

        /// <summary>
        /// 图像显示控件对应（让它们只访问到一个对象）。
        /// 运行时数据，不参与持久化。
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> ImgControlPair
        {
            get { return RuntimeData.ImgControlPair; }
            set { RuntimeData.ImgControlPair = value ?? new Dictionary<string, object>(); }
        }

        /// <summary>
        /// 在手动调试
        /// </summary>
        public bool IsManual { get; set; } = false;

        /// <summary>
        /// 急速模式
        /// </summary>
        public bool IsRapidMode { get; set; } = false;

        /// <summary>
        /// 流程执行结束标记
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, Dictionary<int, bool>> IsProcessEnds
        {
            get { return RuntimeData.IsProcessEnds; }
            set { RuntimeData.IsProcessEnds = value ?? new Dictionary<int, Dictionary<int, bool>>(); }
        }

        [JsonIgnore]
        public object NodeCaches
        {
            get { return RuntimeData.NodeCaches; }
            set { RuntimeData.NodeCaches = value; }
        }

        /// <summary>
        /// 用来获取周期内加载节点的参数
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> NodeParamCaches
        {
            get { return RuntimeData.NodeParamCaches; }
            set { RuntimeData.NodeParamCaches = value ?? new Dictionary<string, object>(); }
        }

        [JsonIgnore]
        private Dictionary<string, ObservableCollection<TransmitParam>> _nodesOutputCache = new Dictionary<string, ObservableCollection<TransmitParam>>();
        /// <summary>
        /// 节点的临时缓存
        /// </summary>
        public Dictionary<string, ObservableCollection<TransmitParam>> NodesOutputCache
        {
            get { return _nodesOutputCache; }
            set { _nodesOutputCache = value; }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _customGlobalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 自定义全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> CustomGlobalParams
        {
            get { return _customGlobalParams; }
            set { _customGlobalParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _globalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get { return _globalParams; }
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _id;
        /// <summary>
        /// 0为默认项目ID
        /// </summary>
        public int ID
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Guid _guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private DateTime _modifyTime = DateTime.Now;
        public DateTime ModifyTime
        {
            get { return _modifyTime; }
            set { _modifyTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing;

        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public NodifySolutionItem()
        {

        }
        #endregion

    }
}

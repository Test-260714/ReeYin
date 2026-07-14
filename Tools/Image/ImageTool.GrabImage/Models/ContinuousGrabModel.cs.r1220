using HalconDotNet;
using Newtonsoft.Json;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.Share.Events;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace ImageTool.GrabImage.Models
{
    [Serializable]
    public class ContinuousGrabModel : ModelParamBase
    {
        #region Fields
        private const int MaxRetainedOutputSnapshotCount = 4;

        [NonSerialized]
        private System.Timers.Timer _watchdogTimer;
        [NonSerialized]
        private DateTime _lastFrameTime = DateTime.MinValue;
        [NonSerialized]
        private int _frameCount = 0;

        // ── 帧队列 & 流程触发 ──
        [NonSerialized]
        private ConcurrentQueue<HImage> _frameQueue = new();
        [NonSerialized]
        private ConcurrentQueue<MultiCameraFramePacket> _multiFrameQueue = new();
        [NonSerialized]
        private volatile bool _isProcessingQueue;
        [NonSerialized]
        private volatile bool _suppressQueueProcessing;   // 首帧级联期间阻止队列处理
        [NonSerialized]
        private object _processLock = new();
        [NonSerialized]
        private object _outputSnapshotLock = new();
        [NonSerialized]
        private Dictionary<string, object> _ownedOutputSnapshot;
        [NonSerialized]
        private Queue<Dictionary<string, object>> _retiredOutputSnapshots = new();
        [NonSerialized]
        private SubscriptionToken _xyhdFlowModeToken;
        [NonSerialized]
        private volatile bool _xyhdEventOnlyMode;
        [NonSerialized]
        private CancellationTokenSource _pseudoGrabCts;
        [NonSerialized]
        private Task _pseudoGrabTask;
        [NonSerialized]
        private string[] _pseudoImageFiles = Array.Empty<string>();
        [NonSerialized]
        private int _pseudoImageIndex;
        [NonSerialized]
        private int _pseudoLoopIndex;
        [NonSerialized]
        private PseudoImageReadPlanner _pseudoReadPlanner;
        [NonSerialized]
        private int _pendingPseudoSingleShotRuns;
        [NonSerialized]
        private DateTime _pseudoSingleShotBypassUntil = DateTime.MinValue;
        [NonSerialized]
        private volatile bool _isPseudoGrabActive;
        [NonSerialized]
        private volatile bool _isMultiCameraGrabActive;
        [NonSerialized]
        private object _multiCameraSync = new();
        [NonSerialized]
        private Dictionary<CameraBase, ImageGrabcallback> _multiCameraHandlers = new();
        [NonSerialized]
        private Dictionary<string, HImage> _multiPendingImages = new(StringComparer.OrdinalIgnoreCase);
        [NonSerialized]
        private DateTime _multiPendingStartedUtc = DateTime.MinValue;
        [NonSerialized]
        private ObservableCollection<ContinuousGrabCameraStatusItem> _multiCameraItems;
        [NonSerialized]
        private bool _isSyncingMultiCameraNames;
        private ObservableCollection<ContinuousGrabCameraStatusItem> _pseudoCameraItems = new();
        private string _debugRawImageSaveRootDirectory = string.Empty;
        private const int MaxQueueSize = 100;
        private const int MaxMultiCameraCount = 6;
        private const int MaxDebugRawImageSaveQueueSize = 10;
        private const string DebugRawImageSaveFormat = "png";
        private const string DebugRawImageSaveExtension = ".png";
        private const int MultiCameraPacketTimeoutMs = 500;
        private const string XyhdFlowModeEventSource = "XYHD_ContinuousGrabFlowMode";
        [NonSerialized]
        private object _debugRawImageSaveLock = new();
        [NonSerialized]
        private BlockingCollection<DebugRawImageSavePacket> _debugRawImageSaveQueue;
        [NonSerialized]
        private bool _isDebugRawImageSaving;
        [NonSerialized]
        private string _debugRawImageSaveDirectory = string.Empty;
        [NonSerialized]
        private long _debugRawImageSaveSessionId;
        [NonSerialized]
        private long _debugRawImageSavedFrameCount;
        [NonSerialized]
        private long _debugRawImageSavedImageCount;
        [NonSerialized]
        private long _debugRawImageDroppedFrameCount;
        #endregion

        private sealed class MultiCameraFramePacket
        {
            public long FrameNumber { get; set; }
            public List<HImage> Images { get; set; } = new();
        }

        private sealed class DebugRawImageSavePacket
        {
            public long SessionId { get; set; }
            public long FrameNumber { get; set; }
            public string RootDirectory { get; set; }
            public List<HImage> Images { get; set; } = new();
        }

        #region Properties
        [JsonIgnore]
        public ObservableCollection<CameraBase> CameraModels
        {
            get
            {
                var camParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.CamConfig] as CameraSetModel;
                return camParam?.CameraModels ?? new ObservableCollection<CameraBase>();
            }
        }

        [JsonIgnore]
        private HImage _image = new HImage();
        [OutputParam("Image", "被处理的图像")]
        public HImage Image
        {
            get => _image;
            set { _image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image1 = new HImage();
        [OutputParam("Image1", "Camera image 1")]
        public HImage Image1
        {
            get => _image1;
            set { _image1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image2 = new HImage();
        [OutputParam("Image2", "Camera image 2")]
        public HImage Image2
        {
            get => _image2;
            set { _image2 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image3 = new HImage();
        [OutputParam("Image3", "Camera image 3")]
        public HImage Image3
        {
            get => _image3;
            set { _image3 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image4 = new HImage();
        [OutputParam("Image4", "Camera image 4")]
        public HImage Image4
        {
            get => _image4;
            set { _image4 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image5 = new HImage();
        [OutputParam("Image5", "Camera image 5")]
        public HImage Image5
        {
            get => _image5;
            set { _image5 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _image6 = new HImage();
        [OutputParam("Image6", "Camera image 6")]
        public HImage Image6
        {
            get => _image6;
            set { _image6 = value; RaisePropertyChanged(); }
        }

        private string _sltCamName = "";
        public string SltCamName
        {
            get => _sltCamName;
            set { _sltCamName = value; RaisePropertyChanged(); }
        }

        private bool _useMultiCameraGrab = true;
        public bool UseMultiCameraGrab
        {
            get => _useMultiCameraGrab;
            set
            {
                _useMultiCameraGrab = true;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MultiCameraSummary));
                RaisePropertyChanged(nameof(GrabSourceSummary));
                RaisePropertyChanged(nameof(PseudoModeSummary));
            }
        }

        private string _multiCameraNames = "";
        public string MultiCameraNames
        {
            get => _multiCameraNames;
            set
            {
                _multiCameraNames = value ?? string.Empty;
                if (!_isSyncingMultiCameraNames)
                    RefreshMultiCameraItemsFromNames();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MultiCameraSummary));
            }
        }

        [JsonIgnore]
        public ObservableCollection<ContinuousGrabCameraStatusItem> MultiCameraItems
        {
            get
            {
                _multiCameraItems ??= new ObservableCollection<ContinuousGrabCameraStatusItem>();
                return _multiCameraItems;
            }
        }

        [JsonProperty]
        public ObservableCollection<ContinuousGrabCameraStatusItem> PseudoCameraItems
        {
            get
            {
                _pseudoCameraItems ??= new ObservableCollection<ContinuousGrabCameraStatusItem>();
                return _pseudoCameraItems;
            }
            set
            {
                _pseudoCameraItems = value ?? new ObservableCollection<ContinuousGrabCameraStatusItem>();
                ReindexPseudoCameraItems();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PseudoImageSummary));
            }
        }

        [JsonProperty]
        public List<ContinuousGrabPseudoCameraConfig> PseudoCameraFolderConfigs { get; set; } = new();

        private int _grabInterval = 1000;
        public int GrabInterval
        {
            get => _grabInterval;
            set
            {
                var normalized = value < 10 ? 10 : value;
                _grabInterval = normalized;
                RaisePropertyChanged();
            }
        }

        private int _pseudoLoopCount;
        public int PseudoLoopCount
        {
            get => _pseudoLoopCount;
            set
            {
                _pseudoLoopCount = value < 0 ? 0 : value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PseudoModeSummary));
            }
        }

        private bool _usePseudoGrab;
        public bool UsePseudoGrab
        {
            get => _usePseudoGrab;
            set
            {
                _usePseudoGrab = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrabSourceSummary));
                RaisePropertyChanged(nameof(MultiCameraSummary));
                RaisePropertyChanged(nameof(PseudoModeSummary));
            }
        }

        private string _pseudoImageFolder = "";
        public string PseudoImageFolder
        {
            get => _pseudoImageFolder;
            set
            {
                _pseudoImageFolder = value ?? string.Empty;
                RaisePropertyChanged();
                RefreshPseudoImageFolderState();
                ResetPseudoGrabOrderState();
            }
        }

        private bool _pseudoLoopEnabled = true;
        public bool PseudoLoopEnabled
        {
            get => _pseudoLoopEnabled;
            set
            {
                _pseudoLoopEnabled = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PseudoModeSummary));
            }
        }

        private bool _pseudoWaitForFlowCompletionBeforeRead = true;
        public bool PseudoWaitForFlowCompletionBeforeRead
        {
            get => _pseudoWaitForFlowCompletionBeforeRead;
            set
            {
                _pseudoWaitForFlowCompletionBeforeRead = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PseudoModeSummary));
            }
        }

        [JsonIgnore]
        private CameraBase _selectedCamera;
        [JsonIgnore]
        public CameraBase SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                _selectedCamera = value;
                if (value != null)
                {
                    SltCamName = value.CameraNo;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrabStatusSummary));
            }
        }

        [JsonIgnore]
        private bool _isGrabbing;
        [JsonIgnore]
        public bool IsGrabbing
        {
            get => _isGrabbing;
            set
            {
                _isGrabbing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrabStatusSummary));
            }
        }

        [JsonIgnore]
        public bool IsPseudoGrabActive
        {
            get => _isPseudoGrabActive;
            private set
            {
                _isPseudoGrabActive = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrabStatusSummary));
            }
        }

        [JsonIgnore]
        public bool IsMultiCameraGrabActive
        {
            get => _isMultiCameraGrabActive;
            private set
            {
                _isMultiCameraGrabActive = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GrabStatusSummary));
            }
        }

        [JsonIgnore]
        public bool IsDebugRawImageSaving => _isDebugRawImageSaving;

        [JsonIgnore]
        public string DebugRawImageSaveDirectory => _debugRawImageSaveDirectory ?? string.Empty;

        public string DebugRawImageSaveRootDirectory
        {
            get => _debugRawImageSaveRootDirectory;
            set
            {
                _debugRawImageSaveRootDirectory = value ?? string.Empty;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DebugRawImageSaveRootDirectoryDisplay));
            }
        }

        [JsonIgnore]
        public string DebugRawImageSaveRootDirectoryDisplay => GetDebugRawImageSaveRootDirectory();

        [JsonIgnore]
        public long DebugRawImageSavedFrameCount => Interlocked.Read(ref _debugRawImageSavedFrameCount);

        [JsonIgnore]
        public long DebugRawImageSavedImageCount => Interlocked.Read(ref _debugRawImageSavedImageCount);

        [JsonIgnore]
        public long DebugRawImageDroppedFrameCount => Interlocked.Read(ref _debugRawImageDroppedFrameCount);

        [JsonIgnore]
        public string DebugRawImageSaveSummary
        {
            get
            {
                var savedFrames = DebugRawImageSavedFrameCount;
                var savedImages = DebugRawImageSavedImageCount;
                var droppedFrames = DebugRawImageDroppedFrameCount;
                if (IsDebugRawImageSaving)
                {
                    return $"原图保存中：{savedFrames} 帧 / {savedImages} 张，丢弃 {droppedFrames} 帧";
                }

                if (!string.IsNullOrWhiteSpace(DebugRawImageSaveDirectory))
                {
                    return $"原图保存已停止：{savedFrames} 帧 / {savedImages} 张，丢弃 {droppedFrames} 帧";
                }

                return "原图调试保存未开启";
            }
        }

        [JsonIgnore]
        public int PseudoImageCount => _pseudoImageFiles?.Length ?? 0;

        [JsonIgnore]
        public ObservableCollection<PseudoImageOrderItem> PseudoImageOrderItems { get; } = new();

        [JsonIgnore]
        public string PseudoCurrentImageSummary
        {
            get
            {
                if (PseudoCameraItems.Count == 0)
                    return "未添加伪相机";

                var currentCameraItems = PseudoCameraItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.CurrentPseudoFileName))
                    .Select(item => $"{item.OutputName}:{item.CurrentPseudoFileName}")
                    .ToList();
                if (currentCameraItems.Count == 0)
                    return "未开始";

                return string.Join("  |  ", currentCameraItems);
            }
        }

        [JsonIgnore]
        public string PseudoImageSummary
        {
            get
            {
                var readyCount = PseudoCameraItems.Count(item => item.PseudoConnected);
                return PseudoCameraItems.Count == 0
                    ? "未添加伪相机"
                    : $"伪相机 {readyCount}/{PseudoCameraItems.Count} 路已配置目录";
            }
        }

        [JsonIgnore]
        public string GrabSourceSummary => UsePseudoGrab
            ? "伪相机采集"
            : "真机采集";

        [JsonIgnore]
        public string MultiCameraSummary
        {
            get
            {
                var cameras = ResolveMultiCameraModels();
                if (cameras.Count == 0)
                    return "未配置真机";

                return $"已配置 {cameras.Count} 路：{string.Join(", ", cameras.Select(c => c.CameraNo))}";
            }
        }

        [JsonIgnore]
        public string PseudoModeSummary
        {
            get
            {
                if (!PseudoLoopEnabled && PseudoLoopCount <= 0)
                    return "单次发送";

                var loopText = PseudoLoopCount <= 0
                    ? "循环发送（不限次数）"
                    : $"循环发送（{PseudoLoopCount} 次）";

                var pseudoCameraCount = GetPseudoMultiCameraOutputCount();
                var packetText = pseudoCameraCount > 0
                    ? $"，每包 {pseudoCameraCount} 张"
                    : string.Empty;

                return PseudoWaitForFlowCompletionBeforeRead
                    ? $"{loopText}{packetText}，等待流程完成"
                    : $"{loopText}{packetText}";
            }
        }

        [JsonIgnore]
        public string GrabStatusSummary
        {
            get
            {
                if (!IsGrabbing)
                    return "已停止";

                if (UsePseudoGrab || IsPseudoGrabActive)
                    return "伪采集中...";

                return "相机列表采集中...";
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get => _output;
            set { _output = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public ContinuousGrabModel()
        {
            EnsureRuntimeState();
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            EnsureRuntimeState();
            RestorePseudoCameraItemsFromFolderConfigs();
        }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            SyncPseudoCameraFolderConfigsFromItems(allowEmpty: false);
        }
        #endregion

        #region Override
        public override bool OnceInit()
        {
            EnsureRuntimeState();

            if (IsOnceInit)
            {
                return true;
            }

            if (!base.OnceInit())
            {
                return false;
            }

            TriggerModuleRun ??= () => ExecuteModule().Result;

            if (_xyhdFlowModeToken == null)
            {
                _xyhdFlowModeToken = PrismProvider.EventAggregator.GetEvent<OutputResultEvent>()
                    .Subscribe(OnOutputEventReceived, ThreadOption.PublisherThread);
            }

            IsOnceInit = true;
            return true;
        }

        public override bool LoadKeyParam()
        {
            try
            {
                UseMultiCameraGrab = true;
                RaisePropertyChanged(nameof(CameraModels));

                if (string.IsNullOrWhiteSpace(MultiCameraNames) && !string.IsNullOrWhiteSpace(SltCamName))
                {
                    MultiCameraNames = SltCamName;
                }

                if (!string.IsNullOrEmpty(SltCamName))
                {
                    SelectedCamera = CameraModels.FirstOrDefault(c => c.CameraNo == SltCamName);
                }

                RefreshMultiCameraItemsFromNames();
                RestorePseudoCameraItemsFromFolderConfigs();
                RefreshPseudoCameraItemsState();
                RaisePropertyChanged(nameof(MultiCameraSummary));
                RaisePropertyChanged(nameof(PseudoImageSummary));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool UpdateParam()
        {
            Dictionary<string, object> nextSnapshot = null;
            Dictionary<string, object> previousSnapshot = null;
            bool snapshotPublished = false;

            try
            {
                EnsureRuntimeState();

                lock (_outputSnapshotLock)
                {
                    moduleOutputParam ??= new ModuleParam();

                    nextSnapshot = BuildOwnedOutputSnapshot();
                    previousSnapshot = _ownedOutputSnapshot;
                    moduleOutputParam.TransmitParams = nextSnapshot;
                    _ownedOutputSnapshot = nextSnapshot;
                    snapshotPublished = true;

                    var snapshotOutputParams = new ObservableCollection<TransmitParam>(
                        nextSnapshot.Values.OfType<TransmitParam>());

                    PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()] = snapshotOutputParams;
                    UpdateGlobalParamsFromSnapshot(snapshotOutputParams);

                    if (!ReferenceEquals(previousSnapshot, nextSnapshot))
                    {
                        RetireOwnedOutputSnapshot(previousSnapshot);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!snapshotPublished)
                {
                    DisposeOwnedOutputSnapshot(nextSnapshot);
                }

                Logs.LogError($"[连续采集] 更新输出快照失败: Serial={Serial}, Error={ex}");
                return false;
            }
        }

        public override void Dispose()
        {
            StopGrab();
            StopDebugRawImageSave();
            ClearFrameQueue();
            ClearMultiFrameQueue();
            ClearMultiPendingImages();
            SafeDisposeImage(ref _image);
            SafeDisposeImage(ref _image1);
            SafeDisposeImage(ref _image2);
            SafeDisposeImage(ref _image3);
            SafeDisposeImage(ref _image4);
            SafeDisposeImage(ref _image5);
            SafeDisposeImage(ref _image6);
            if (_xyhdFlowModeToken != null)
            {
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Unsubscribe(_xyhdFlowModeToken);
                _xyhdFlowModeToken = null;
            }
            DisposeAllOwnedOutputSnapshots();
            base.Dispose();
        }
        #endregion

        #region Methods
        private void EnsureRuntimeState()
        {
            _frameQueue ??= new ConcurrentQueue<HImage>();
            _multiFrameQueue ??= new ConcurrentQueue<MultiCameraFramePacket>();
            _processLock ??= new object();
            _outputSnapshotLock ??= new object();
            _retiredOutputSnapshots ??= new Queue<Dictionary<string, object>>();
            _multiCameraSync ??= new object();
            _multiCameraHandlers ??= new Dictionary<CameraBase, ImageGrabcallback>();
            _multiPendingImages ??= new Dictionary<string, HImage>(StringComparer.OrdinalIgnoreCase);
            _multiCameraItems ??= new ObservableCollection<ContinuousGrabCameraStatusItem>();
            _pseudoCameraItems ??= new ObservableCollection<ContinuousGrabCameraStatusItem>();
            _pseudoImageFiles ??= Array.Empty<string>();
            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            _debugRawImageSaveLock ??= new object();
            _debugRawImageSaveDirectory ??= string.Empty;
            _debugRawImageSaveRootDirectory ??= string.Empty;
            SanitizeTransmitParamCollections("EnsureRuntimeState");
        }

        private void SanitizeTransmitParamCollections(string source)
        {
            if (OutputParams == null)
            {
                OutputParams = new ObservableCollection<TransmitParam>();
            }

            if (InputParams == null)
            {
                InputParams = new ObservableCollection<TransmitParam>();
            }

            var outputNulls = RemoveNullTransmitParams(OutputParams);
            var inputNulls = RemoveNullTransmitParams(InputParams);

            if (outputNulls > 0 || inputNulls > 0)
            {
                Logs.LogWarning($"[连续采集配置诊断] 清理空参数: Source={source}, Serial={Serial}, OutputNulls={outputNulls}, InputNulls={inputNulls}");
            }
        }

        private static int RemoveNullTransmitParams(ObservableCollection<TransmitParam> transmitParams)
        {
            if (transmitParams == null)
            {
                return 0;
            }

            var removed = 0;
            for (var i = transmitParams.Count - 1; i >= 0; i--)
            {
                if (transmitParams[i] != null)
                {
                    continue;
                }

                transmitParams.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        private bool TryConsumePseudoSingleShotRun()
        {
            if (Interlocked.CompareExchange(ref _pendingPseudoSingleShotRuns, 0, 0) <= 0)
                return false;

            if (DateTime.Now > _pseudoSingleShotBypassUntil)
            {
                Interlocked.Exchange(ref _pendingPseudoSingleShotRuns, 0);
                return false;
            }

            Interlocked.Decrement(ref _pendingPseudoSingleShotRuns);
            return true;
        }

        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    // ★ 关键修复：重置 IsManual 标志
                    // 手动点击"运行"会设置 IsManual=true，但 TriggerWork 从不重置它。
                    // 导致末端节点完成后不执行 IsProcessEnds.Remove()，WaitForFlowIdle 永远等待。
                    var sln = PrismProvider.ProjectManager?.SltCurSolutionItem;
                    if (sln != null) sln.IsManual = false;

                    LoadKeyParam();

                    if (UsePseudoGrab && TryConsumePseudoSingleShotRun())
                    {
                        if (HasReadyFrameForCurrentMode())
                        {
                            Console.WriteLine("[连续采集] 执行一次触发回流，使用已读取图像，不启动连续伪采集");
                            return NodeStatus.Success;
                        }

                        Console.WriteLine("[连续采集] 执行一次触发回流，但图像未就绪");
                        return NodeStatus.Error;
                    }

                    if (UsePseudoGrab)
                    {
                        if (!TryEnsurePseudoImagesLoaded(out var pseudoError))
                        {
                            Console.WriteLine($"[连续采集] 伪采集源不可用: {pseudoError}");
                            return NodeStatus.Error;
                        }
                    }
                    else
                    {
                        if (!TryEnsureCameraListConfigured(out var cameraListError))
                        {
                            Console.WriteLine($"[ContinuousGrab] Camera list invalid: {cameraListError}");
                            return NodeStatus.Error;
                        }

                        if (!TryResolveMultiCameraModels(out var cameras, out var multiCameraError))
                        {
                            Console.WriteLine($"[ContinuousGrab] Camera list config invalid: {multiCameraError}");
                            return NodeStatus.Error;
                        }

                        var disconnected = cameras.FirstOrDefault(c => !c.Connected);
                        if (disconnected != null)
                        {
                            Console.WriteLine($"[ContinuousGrab] Camera disconnected: {disconnected.CameraNo}");
                            return NodeStatus.Error;
                        }
                    }

                    // ★ 已在连续采集中：短路返回，图像已由 ProcessFrameQueue 准备好
                    if (IsGrabbing)
                    {
                        if (HasReadyFrameForCurrentMode())
                        {
                            Console.WriteLine("[连续采集] ExecuteModule 短路 — 使用已有图像");
                            return NodeStatus.Success;
                        }

                        // 可能刚启动还没收到帧，等一下
                        Console.WriteLine("[连续采集] 等待图像就绪...");
                        var waitDeadline = DateTime.Now.AddSeconds(5);
                        while (DateTime.Now < waitDeadline)
                        {
                            if (HasReadyFrameForCurrentMode())
                            {
                                Console.WriteLine("[连续采集] ExecuteModule 短路 — 图像已就绪");
                                return NodeStatus.Success;
                            }
                            Thread.Sleep(50);
                        }
                        Console.WriteLine("[连续采集] 等待图像超时");
                        return NodeStatus.Error;
                    }

                    // ★ 首次执行：启动连续采集，让相机持续出帧
                    Console.WriteLine("[连续采集] ExecuteModule — 首次执行，启动连续采集模式");

                    // 阻止 ProcessFrameQueue 启动，避免首帧被重复处理
                    _suppressQueueProcessing = true;

                    if (!StartGrabInternal())
                    {
                        _suppressQueueProcessing = false;
                        Console.WriteLine("[连续采集] 启动失败");
                        return NodeStatus.Error;
                    }

                    // 等待第一帧到达
                    var deadline = DateTime.Now.AddSeconds(10);
                    while (DateTime.Now < deadline)
                    {
                        if (HasReadyFrameForCurrentMode())
                            break;
                        Thread.Sleep(50);
                    }

                    if (!HasReadyFrameForCurrentMode())
                    {
                        Console.WriteLine("[连续采集] 首帧等待超时(10s)，停止采集");
                        _suppressQueueProcessing = false;
                        StopGrab();
                        return NodeStatus.Error;
                    }

                    // 设置 OutputParams 供本次 ExecuteMulti 级联的下游节点使用
                    var dataPointValues = OutputParamCollector.GetDataPointValues(this);
                    if (!TryReplaceOutputImagesFromValues(dataPointValues, "FirstFrame"))
                    {
                        Console.WriteLine("[连续采集] 首帧输出图像准备失败，停止采集");
                        Logs.LogWarning($"[连续采集] 首帧输出图像准备失败: Serial={Serial}, Frame={_frameCount}");
                        _suppressQueueProcessing = false;
                        StopGrab();
                        return NodeStatus.Error;
                    }
                    UpdateParam();

                    // 清空启动期间积累的帧（它们由本次级联处理，不需要重复）
                    ClearFrameQueue();
                    ClearMultiFrameQueue();

                    // 开放队列处理 → 后续帧由 ProcessFrameQueue 自动触发流程
                    _suppressQueueProcessing = false;

                    // 如果清空后又有新帧入队了，立即启动处理
                    if (HasPendingFrameQueue())
                        TryStartFlowProcessing();

                    Console.WriteLine("[连续采集] 首帧就绪，连续采集已启动，后续帧将自动触发流程");
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    _suppressQueueProcessing = false;
                    Console.WriteLine($"[连续采集] 异常: {ex.Message}");
                    return NodeStatus.Error;
                }
            });

            return Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// UI 按钮调用的启动方法（带 MessageBox 提示）
        /// </summary>
        public void StartRealGrab()
        {
            if (IsGrabbing)
            {
                StartGrab();
                return;
            }

            UsePseudoGrab = false;
            StartGrab();
        }

        public void StartPseudoGrab()
        {
            if (IsGrabbing)
            {
                StartGrab();
                return;
            }

            UsePseudoGrab = true;
            StartGrab();
        }

        public void StartGrab()
        {
            if (IsGrabbing)
            {
                var runningMode = IsPseudoGrabActive ? "伪采集" : "真机采集";
                System.Windows.MessageBox.Show($"{runningMode}正在运行，请先停止当前采集");
                return;
            }

            if (UsePseudoGrab)
            {
                if (!TryEnsurePseudoImagesLoaded(out var pseudoError))
                {
                    System.Windows.MessageBox.Show(pseudoError);
                    return;
                }
            }
            else
            {
                if (!TryEnsureCameraListConfigured(out var cameraListError))
                {
                    System.Windows.MessageBox.Show(cameraListError);
                    return;
                }

                if (!TryResolveMultiCameraModels(out var cameras, out var multiCameraError))
                {
                    System.Windows.MessageBox.Show(multiCameraError);
                    return;
                }

                var disconnected = cameras.FirstOrDefault(c => !c.Connected);
                if (disconnected != null)
                {
                    System.Windows.MessageBox.Show($"相机未连接：{disconnected.CameraNo}");
                    return;
                }
            }

            if (!StartGrabInternal())
            {
                var error = UsePseudoGrab ? "伪采集启动失败" : "连续采集启动失败";
                System.Windows.MessageBox.Show(error);
                return;
            }
        }

        /// <summary>
        /// 内部启动连续采集（无 UI 弹窗，可从 ExecuteModule 调用）
        /// </summary>
        private bool StartGrabInternal()
        {
            if (IsGrabbing) return true;

            if (UsePseudoGrab)
                return StartPseudoGrabInternal();

            return StartMultiCameraGrabInternal();
        }

        private bool StartCameraGrabInternal()
        {
            if (SelectedCamera == null || !SelectedCamera.Connected)
                return false;

            // 订阅相机图像回调
            SelectedCamera.ImageGrab += OnImageGrabbed;

            // 启动看门狗定时器，检测回调是否停止
            _watchdogTimer = new System.Timers.Timer { Interval = 2000, AutoReset = true };
            _watchdogTimer.Elapsed += OnWatchdogTick;
            _watchdogTimer.Enabled = true;

            _lastFrameTime = DateTime.Now;
            _frameCount = 0;

            SelectedCamera.StartCollect();
            IsGrabbing = true;
            Console.WriteLine("[连续采集] StartGrabInternal 完成");
            return true;
        }

        private bool StartMultiCameraGrabInternal()
        {
            if (!TryResolveMultiCameraModels(out var cameras, out var error))
            {
                Console.WriteLine($"[ContinuousGrab] Start multi-camera failed: {error}");
                return false;
            }

            var disconnected = cameras.FirstOrDefault(c => !c.Connected);
            if (disconnected != null)
            {
                Console.WriteLine($"[ContinuousGrab] Start multi-camera failed, disconnected: {disconnected.CameraNo}");
                return false;
            }

            ClearFrameQueue();
            ClearMultiFrameQueue();
            ClearMultiPendingImages();

            try
            {
                lock (_multiCameraSync)
                {
                    _multiCameraHandlers.Clear();
                    foreach (var camera in cameras)
                    {
                        var currentCamera = camera;
                        ImageGrabcallback handler = img => OnMultiCameraImageGrabbed(currentCamera, img);
                        currentCamera.ImageGrab += handler;
                        _multiCameraHandlers[currentCamera] = handler;
                    }
                }

                _watchdogTimer = new System.Timers.Timer { Interval = 2000, AutoReset = true };
                _watchdogTimer.Elapsed += OnWatchdogTick;
                _watchdogTimer.Enabled = true;

                _lastFrameTime = DateTime.Now;
                _frameCount = 0;
                IsMultiCameraGrabActive = true;
                IsGrabbing = true;
                MarkMultiCameraWaiting();

                foreach (var camera in cameras)
                    camera.StartCollect();

                Console.WriteLine($"[ContinuousGrab] Multi-camera grab started: {string.Join(", ", cameras.Select(c => c.CameraNo))}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Start multi-camera exception: {ex.Message}");
                StopMultiCameraGrab();
                IsMultiCameraGrabActive = false;
                IsGrabbing = false;
                StopWatchdog();
                ClearMultiFrameQueue();
                ClearMultiPendingImages();
                return false;
            }
        }

        public void StopRealGrab()
        {
            if (!IsGrabbing)
                return;

            if (IsPseudoGrabActive)
            {
                System.Windows.MessageBox.Show("当前运行的是伪采集，请使用“停止伪采集”");
                return;
            }

            StopGrab();
        }

        public void StopPseudoGrab()
        {
            if (!IsGrabbing)
                return;

            if (!IsPseudoGrabActive)
            {
                System.Windows.MessageBox.Show("当前运行的是真机采集，请使用“停止真机”");
                return;
            }

            StopGrab();
        }

        private bool StartPseudoGrabInternal()
        {
            if (!TryEnsurePseudoImagesLoaded(out var error))
            {
                Console.WriteLine($"[连续采集] 伪采集启动失败: {error}");
                return false;
            }

            _pseudoGrabCts?.Cancel();
            _pseudoGrabCts?.Dispose();
            var pseudoGrabCts = new CancellationTokenSource();
            _pseudoGrabCts = pseudoGrabCts;
            ResetPseudoGrabOrderState();
            ClearFrameQueue();
            ClearMultiFrameQueue();
            ClearMultiPendingImages();
            _lastFrameTime = DateTime.Now;
            _frameCount = 0;
            IsPseudoGrabActive = true;
            IsGrabbing = true;
            MarkPseudoCameraWaiting();

            _pseudoGrabTask = Task.Run(() => RunPseudoGrabLoopAsync(pseudoGrabCts));
            Console.WriteLine($"[连续采集] 伪采集已启动, 伪相机数={PseudoCameraItems.Count}, 间隔={GrabInterval}ms, 模式={PseudoModeSummary}");
            return true;
        }

        public void GrabPseudoOnce()
        {
            if (IsGrabbing)
            {
                System.Windows.MessageBox.Show("当前正在连续采集，请先停止采集");
                return;
            }

            if (_isProcessingQueue)
            {
                System.Windows.MessageBox.Show("上一张图像流程正在触发，请稍后再执行");
                return;
            }

            UsePseudoGrab = true;

            if (!TryEnsurePseudoImagesLoaded(out var error))
            {
                System.Windows.MessageBox.Show(error);
                return;
            }

            try
            {
                ClearFrameQueue();
                ClearMultiFrameQueue();

                if (!TryGetNextPseudoImagePathsByCamera(out var imagePaths))
                {
                    System.Windows.MessageBox.Show("伪相机目录图片不足，无法组成完整伪采集图包");
                    return;
                }

                AcceptPseudoImageFilesAsMultiCameraPacket(imagePaths, forceFlowTrigger: true, requireGrabbingForFlow: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[连续采集] 执行一次读取图片失败: {ex.Message}");
                System.Windows.MessageBox.Show($"读取图片失败：{ex.Message}");
            }
        }

        public void StopGrab()
        {
            if (!IsGrabbing) return;

            // ★ 先置标志，让处理循环尽快退出
            IsGrabbing = false;
            StopWatchdog();

            if (IsPseudoGrabActive)
            {
                _pseudoGrabCts?.Cancel();
                IsPseudoGrabActive = false;
            }
            else if (IsMultiCameraGrabActive)
            {
                StopMultiCameraGrab();
                IsMultiCameraGrabActive = false;
            }
            else
            {
                // 取消订阅
                if (SelectedCamera != null)
                {
                    SelectedCamera.ImageGrab -= OnImageGrabbed;
                }

                SelectedCamera?.EndCollect();
            }

            // 清空帧队列
            ClearFrameQueue();
            ClearMultiFrameQueue();
            ClearMultiPendingImages();
            Console.WriteLine("[连续采集] 已停止，帧队列已清空");
        }

        private void StopMultiCameraGrab()
        {
            List<KeyValuePair<CameraBase, ImageGrabcallback>> handlers;
            lock (_multiCameraSync)
            {
                handlers = _multiCameraHandlers.ToList();
                _multiCameraHandlers.Clear();
            }

            foreach (var pair in handlers)
            {
                try
                {
                    pair.Key.ImageGrab -= pair.Value;
                    pair.Key.EndCollect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ContinuousGrab] Stop multi-camera failed: Camera={pair.Key?.CameraNo}, Error={ex.Message}");
                }
            }
        }

        /// <summary>
        /// 相机图像回调 - 每当相机采集到新帧时触发
        /// </summary>
        private void OnImageGrabbed(HImage img)
        {
            if (!IsGrabbing || img == null) return;

            AcceptGrabbedImage(img, forceFlowTrigger: false, requireGrabbingForFlow: true);
        }

        private void OnMultiCameraImageGrabbed(CameraBase camera, HImage img)
        {
            if (!IsGrabbing || !IsMultiCameraGrabActive || camera == null || img == null)
                return;

            var ownedImage = HalconImageOwnership.CopyBorrowedOrNull(img);
            if (!HalconImageOwnership.IsInitializedSafe(ownedImage))
            {
                SafeDisposeImage(ownedImage);
                return;
            }

            MultiCameraFramePacket packet = null;
            try
            {
                var cameras = ResolveMultiCameraModels();
                if (!cameras.Any(c => ReferenceEquals(c, camera)))
                {
                    SafeDisposeImage(ownedImage);
                    return;
                }

                lock (_multiCameraSync)
                {
                    var now = DateTime.UtcNow;
                    if (_multiPendingStartedUtc != DateTime.MinValue
                        && (now - _multiPendingStartedUtc).TotalMilliseconds > MultiCameraPacketTimeoutMs)
                    {
                        MarkMultiCameraPacketTimeout();
                        ClearMultiPendingImagesLocked();
                    }

                    if (_multiPendingImages.Count == 0)
                        _multiPendingStartedUtc = now;

                    var cameraKey = camera.CameraNo ?? string.Empty;
                    if (_multiPendingImages.TryGetValue(cameraKey, out var oldImage))
                        SafeDisposeImage(oldImage);

                    _multiPendingImages[cameraKey] = ownedImage;
                    ownedImage = null;
                    MarkMultiCameraReceived(cameraKey);

                    if (cameras.All(c => _multiPendingImages.ContainsKey(c.CameraNo ?? string.Empty)))
                    {
                        packet = new MultiCameraFramePacket
                        {
                            FrameNumber = _frameCount + 1,
                            Images = cameras
                                .Select(c => _multiPendingImages[c.CameraNo ?? string.Empty])
                                .ToList()
                        };

                        _multiPendingImages.Clear();
                        _multiPendingStartedUtc = DateTime.MinValue;
                    }
                }

                if (packet != null)
                    AcceptOwnedMultiCameraPacket(packet, forceFlowTrigger: false, requireGrabbingForFlow: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Multi-camera callback exception: Camera={camera.CameraNo}, Error={ex.Message}");
                DisposeMultiCameraPacket(packet);
            }
            finally
            {
                SafeDisposeImage(ownedImage);
            }
        }

        private void AcceptGrabbedImage(HImage img, bool forceFlowTrigger, bool requireGrabbingForFlow)
        {
            if (img == null) return;

            try
            {

                // 1. 更新显示用图像（最新帧，用于 UI 预览）
                var latestImage = HalconImageOwnership.CopyBorrowedOrNull(img);
                var queueImage = HalconImageOwnership.CopyBorrowedOrNull(img);
                AcceptOwnedGrabbedImages(latestImage, queueImage, forceFlowTrigger, requireGrabbingForFlow);
                return;
/*
                var oldImage = _image;
                _image = latestImage;
                SafeDisposeImage(oldImage);
                RaisePropertyChanged(nameof(Image));

                if (forceFlowTrigger || !_xyhdEventOnlyMode)
                {
                    // 2. 入队：为流程处理保存一份独立拷贝
                    _frameQueue.Enqueue(HalconImageOwnership.CopyForOutput(img));

                    // 3. 安全上限：若队列超限，丢弃最早帧并告警
                    while (_frameQueue.Count > MaxQueueSize)
                    {
                        if (_frameQueue.TryDequeue(out var dropped))
                        {
                            Console.WriteLine($"[连续采集] ⚠ 队列超限({MaxQueueSize})，丢弃最早帧");
                            SafeDisposeImage(dropped);
                        }
                    }

                    // 4. 每帧输出日志（方便排查是否有帧到达）
                WriteVerboseContinuousGrabLog($"[连续采集] ◆ 帧#{_frameCount} 到达, 队列深度={_frameQueue.Count}, suppress={_suppressQueueProcessing}, processing={_isProcessingQueue}");

                    // 5. 尝试启动流程处理循环
                    TryStartFlowProcessing(requireGrabbingForFlow);
                }
                else
                {
                    if (!_frameQueue.IsEmpty)
                        ClearFrameQueue();

                WriteVerboseContinuousGrabLog($"[连续采集] ◆ 帧#{_frameCount} 到达, 事件直发模式启用");
                }

                // 6. Only publish full-frame image events in event-only mode. In flow mode
                // the frame is already passed through OutputParams; an extra HImage copy here
                // has no deterministic owner and can accumulate HALCON native memory.
                if (_xyhdEventOnlyMode)
                {
                    var publishImage = HalconImageOwnership.CopyForOutput(_image);
                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ContinuousGrab", publishImage));
                }
*/
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[连续采集] AcceptGrabbedImage 异常: {ex.Message}");
            }
        }

        private void AcceptPseudoImageFile(string imagePath, bool forceFlowTrigger, bool requireGrabbingForFlow)
        {
            HImage latestImage = null;
            HImage queueImage = null;
            try
            {
                latestImage = new HImage();
                latestImage.ReadImage(imagePath);
                queueImage = new HImage();
                queueImage.ReadImage(imagePath);

                AcceptOwnedGrabbedImages(latestImage, queueImage, forceFlowTrigger, requireGrabbingForFlow);
                latestImage = null;
                queueImage = null;
            }
            finally
            {
                SafeDisposeImage(latestImage);
                SafeDisposeImage(queueImage);
            }
        }

        private void AcceptPseudoImageFilesAsMultiCameraPacket(IReadOnlyList<string> imagePaths, bool forceFlowTrigger, bool requireGrabbingForFlow)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                return;

            MultiCameraFramePacket packet = null;
            var images = new List<HImage>();
            try
            {
                foreach (var imagePath in imagePaths)
                {
                    var image = new HImage();
                    image.ReadImage(imagePath);
                    if (!HalconImageOwnership.IsInitializedSafe(image))
                    {
                        SafeDisposeImage(image);
                        throw new InvalidOperationException($"图片未初始化：{Path.GetFileName(imagePath)}");
                    }

                    images.Add(image);
                }

                packet = new MultiCameraFramePacket
                {
                    FrameNumber = _frameCount + 1,
                    Images = images
                };
                images = null;

                MarkPseudoMultiCameraReceived(packet.Images.Count);
                AcceptOwnedMultiCameraPacket(packet, forceFlowTrigger, requireGrabbingForFlow);
                packet = null;
            }
            finally
            {
                if (images != null)
                {
                    foreach (var image in images)
                        SafeDisposeImage(image);
                }

                DisposeMultiCameraPacket(packet);
            }
        }

        private void AcceptOwnedGrabbedImages(HImage latestImage, HImage queueImage, bool forceFlowTrigger, bool requireGrabbingForFlow)
        {
            if (!HalconImageOwnership.IsInitializedSafe(latestImage))
            {
                SafeDisposeImage(latestImage);
                SafeDisposeImage(queueImage);
                return;
            }

            _lastFrameTime = DateTime.Now;
            _frameCount++;

            var oldImage = _image;
            _image = latestImage;
            SafeDisposeImage(oldImage);
            RaisePropertyChanged(nameof(Image));
            QueueDebugRawImageSave(_frameCount, new[] { _image });

            if (forceFlowTrigger || !_xyhdEventOnlyMode)
            {
                if (HalconImageOwnership.IsInitializedSafe(queueImage))
                {
                    _frameQueue.Enqueue(queueImage);
                    queueImage = null;
                }

                while (_frameQueue.Count > MaxQueueSize)
                {
                    if (_frameQueue.TryDequeue(out var dropped))
                    {
                        Console.WriteLine($"[杩炵画閲囬泦] 鈿?闃熷垪瓒呴檺({MaxQueueSize})锛屼涪寮冩渶鏃╁抚");
                        SafeDisposeImage(dropped);
                    }
                }

                Console.WriteLine($"[杩炵画閲囬泦] 鈼?甯?{_frameCount} 鍒拌揪, 闃熷垪娣卞害={_frameQueue.Count}, suppress={_suppressQueueProcessing}, processing={_isProcessingQueue}");
                TryStartFlowProcessing(requireGrabbingForFlow);
            }
            else
            {
                SafeDisposeImage(queueImage);
                if (!_frameQueue.IsEmpty)
                    ClearFrameQueue();

                Console.WriteLine($"[杩炵画閲囬泦] 鈼?甯?{_frameCount} 鍒拌揪, 浜嬩欢鐩村彂妯″紡鍚敤");
            }

            if (_xyhdEventOnlyMode)
            {
                var publishImage = HalconImageOwnership.CopyForOutput(_image);
                if (HalconImageOwnership.IsInitializedSafe(publishImage))
                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ContinuousGrab", publishImage));
                else
                    SafeDisposeImage(publishImage);
            }
        }

        private void AcceptOwnedMultiCameraPacket(MultiCameraFramePacket packet, bool forceFlowTrigger, bool requireGrabbingForFlow)
        {
            if (!IsValidMultiCameraPacket(packet))
            {
                DisposeMultiCameraPacket(packet);
                return;
            }

            try
            {
                _lastFrameTime = DateTime.Now;
                _frameCount++;
                packet.FrameNumber = _frameCount;
                if (UsePseudoGrab)
                    MarkPseudoCameraPacketSent(packet.FrameNumber);
                else
                    MarkMultiCameraPacketSent(packet.FrameNumber);

                if (!TryReplacePreviewImagesFromPacket(packet))
                {
                    DisposeMultiCameraPacket(packet);
                    return;
                }
                QueueDebugRawImageSave(packet.FrameNumber, packet.Images);

                if (forceFlowTrigger || !_xyhdEventOnlyMode)
                {
                    _multiFrameQueue.Enqueue(packet);
                    packet = null;

                    while (_multiFrameQueue.Count > MaxQueueSize)
                    {
                        if (_multiFrameQueue.TryDequeue(out var dropped))
                        {
                            Console.WriteLine($"[ContinuousGrab] Multi-camera queue overflow({MaxQueueSize}), drop oldest packet.");
                            DisposeMultiCameraPacket(dropped);
                        }
                    }

                    Console.WriteLine($"[ContinuousGrab] Multi-camera packet #{_frameCount} ready, Queue={_multiFrameQueue.Count}, suppress={_suppressQueueProcessing}, processing={_isProcessingQueue}");
                    TryStartFlowProcessing(requireGrabbingForFlow);
                }
                else
                {
                    DisposeMultiCameraPacket(packet);
                    packet = null;
                    ClearMultiFrameQueue();
                }

                if (_xyhdEventOnlyMode)
                {
                    var publishImage = HalconImageOwnership.CopyForOutput(_image);
                    if (HalconImageOwnership.IsInitializedSafe(publishImage))
                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ContinuousGrab", publishImage));
                    else
                        SafeDisposeImage(publishImage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Accept multi-camera packet exception: {ex.Message}");
                DisposeMultiCameraPacket(packet);
            }
        }

        #region Frame Queue Processing
        /// <summary>
        /// 非阻塞地尝试启动帧处理循环
        /// </summary>
        private void TryStartFlowProcessing(bool requireGrabbing = true)
        {
            // 首帧级联期间不启动队列处理，避免重复执行
            if (_isProcessingQueue || _suppressQueueProcessing) return;

            lock (_processLock)
            {
                if (_isProcessingQueue || _suppressQueueProcessing) return;
                _isProcessingQueue = true;
            }

            Task.Run(() => ProcessFrameQueue(requireGrabbing));
        }

        /// <summary>
        /// 帧处理循环：逐帧出队 → 写入 OutputParams/Cache → 触发流程 → 等完成 → 下一帧
        /// 保证同一时刻只有一条流程在执行，杜绝并发竞态
        /// </summary>
        private void ProcessFrameQueue(bool requireGrabbing)
        {
            WriteVerboseContinuousGrabLog("[连续采集] ── ProcessFrameQueue 启动 ──");
            int processed = 0;
            try
            {
                while ((!requireGrabbing || IsGrabbing)
                    && (!requireGrabbing || !_xyhdEventOnlyMode)
                    && TryDequeueNextFrame(out var frameImage, out var framePacket))
                {
                    processed++;
                    var frameProcessSw = Stopwatch.StartNew();
                    try
                    {
                        // 1. 等待 **所有** 流程执行完毕（包括首次 Listen/Start 触发的级联）
                        WriteVerboseContinuousGrabLog($"[连续采集] ProcessFrameQueue: 等待流程空闲 (第{processed}帧)...");
                        var waitSw = Stopwatch.StartNew();
                        if (!WaitForDownstreamLeafNodesCompleted(TimeSpan.FromSeconds(30), requireGrabbing))
                        {
                            if (requireGrabbing)
                            {
                                if (HasActiveDownstreamFlowNode())
                                {
                                    Console.WriteLine("[ContinuousGrab] Wait flow idle timeout, but downstream still has Running/Waiting node. Skip this frame to avoid cross-cycle state pollution.");
                                    DisposeDequeuedFrame(frameImage, framePacket);
                                    continue;
                                }

                                Console.WriteLine("[连续采集] ⚠ 等待末端节点状态超时(30s)，重置流程结束标记并继续触发");
                                TryResetStuckProcessEnds();
                            }
                            else
                            {
                                Console.WriteLine("[连续采集] ⚠ 等待末端节点状态超时(30s)，跳过当前帧");
                                DisposeDequeuedFrame(frameImage, framePacket);
                                continue;
                            }
                        }

                        waitSw.Stop();
                        WriteVerboseContinuousGrabLog($"[连续采集] ProcessFrameQueue: 流程空闲，准备触发，WaitIdle={waitSw.Elapsed.TotalMilliseconds:F1}ms");

                        // 2. 将本帧图像写入 OutputParams（供 ExecuteMulti 下游节点读取）
                        var updateFrameSw = Stopwatch.StartNew();
                        var outputReady = framePacket != null
                            ? TryReplaceOutputImagesFromPacket(framePacket, "QueuePacket")
                            : TryReplaceOutputImagesFromFrame(frameImage, "QueueFrame");
                        if (!outputReady)
                        {
                            Logs.LogWarning(
                                $"[连续采集] 本帧输出图像准备失败，跳过触发: Serial={Serial}, Frame={_frameCount}, Queue={GetActiveFrameQueueCount()}");
                            DisposeDequeuedFrame(frameImage, framePacket);
                            continue;
                        }

                        // 3. 刷新 NodesOutputCache（下游节点通过 Cache 获取图像）
                        if (OutputParams.Count > 0)
                            UpdateParam();
                        updateFrameSw.Stop();

                        // 4. 确保 IsManual=false，否则末端节点不会清理 IsProcessEnds
                        var sln = PrismProvider.ProjectManager?.SltCurSolutionItem;
                        if (sln != null) sln.IsManual = false;

                        // 5. 发布 SwitchWorkStatusEvent → TriggerWork → ExecuteMulti
                        var triggerSw = Stopwatch.StartNew();
                        long cycleId = FlowCycleContext.CreateAndRegister(Serial);
                        WriteVerboseContinuousGrabLog($"[连续采集] ▶ 触发流程, CycleId={cycleId}, 帧#{_frameCount}, 剩余队列={GetActiveFrameQueueCount()}");
                        if (!requireGrabbing)
                        {
                            _pseudoSingleShotBypassUntil = DateTime.Now.AddSeconds(5);
                            Interlocked.Increment(ref _pendingPseudoSingleShotRuns);
                        }

                        PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>()
                            .Publish((eRunStatus.Running, Serial));

                        // 6. 短轮询等待 TriggerWork 建立 IsProcessEnds 标记，替代固定 300ms 阻塞
                        var flowRegistered = WaitForFlowDispatch(TimeSpan.FromMilliseconds(500), requireGrabbing);
                        triggerSw.Stop();
                        frameProcessSw.Stop();
                        WriteVerboseContinuousGrabLog($"[连续采集] 帧#{_frameCount} 流程触发完成: UpdateParam={updateFrameSw.Elapsed.TotalMilliseconds:F1}ms, TriggerDispatch={triggerSw.Elapsed.TotalMilliseconds:F1}ms, Registered={flowRegistered}, Total={frameProcessSw.Elapsed.TotalMilliseconds:F1}ms");

                        // 7. 释放出队帧（副本已存入 OutputParams）
                        DisposeDequeuedFrame(frameImage, framePacket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[连续采集] 处理帧异常: {ex.Message}\n{ex.StackTrace}");
                        DisposeDequeuedFrame(frameImage, framePacket);
                    }
                }

                WriteVerboseContinuousGrabLog($"[连续采集] ── ProcessFrameQueue 队列已空, 本轮处理了 {processed} 帧 ──");
            }
            finally
            {
                lock (_processLock)
                {
                    _isProcessingQueue = false;
                }

                // 退出循环期间可能有新帧入队，递归检查
                if (requireGrabbing && _xyhdEventOnlyMode)
                {
                    ClearFrameQueue();
                    ClearMultiFrameQueue();
                }
                else if ((!requireGrabbing || IsGrabbing) && HasPendingFrameQueue())
                {
                    WriteVerboseContinuousGrabLog("[连续采集] ProcessFrameQueue 退出后发现新帧，重新启动");
                    TryStartFlowProcessing(requireGrabbing);
                }
            }
        }

        private void OnOutputEventReceived((string source, object data) result)
        {
            if (result.data is not ValueTuple<int, bool> mode)
                return;

            var targetSerial = mode.Item1;
            if (targetSerial != -1 && targetSerial != Serial)
                return;

            if (result.source == XyhdFlowModeEventSource)
            {
                _xyhdEventOnlyMode = mode.Item2;
                if (_xyhdEventOnlyMode)
                    ClearFrameQueue();

                Console.WriteLine($"[连续采集] XYHD事件直发模式{(_xyhdEventOnlyMode ? "启用" : "关闭")}, Serial={Serial}, Target={targetSerial}");
                return;
            }

        }

        /// <summary>
        /// 轮询等待 **所有** 流程执行完毕（IsProcessEnds 中不再有未完成的末端节点）。
        /// ── 关键修复 ──
        /// 首次级联由 Listen/Start(serial=4) 触发，跟踪在 IsProcessEnds[4]。
        /// 如果只检查 IsProcessEnds[本节点Serial]，会在第一级联还在跑时就误判为空闲，
        /// 导致并行级联共享下游节点，DL 模型死锁。
        /// 改为检查 **所有** key：只要有任一分支还在运行就等待。
        /// </summary>
        private bool WaitForFlowIdle(TimeSpan timeout, bool requireGrabbing = true)
        {
            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem == null) return true;

            var sw = Stopwatch.StartNew();
            bool logged = false;
            while (sw.Elapsed < timeout)
            {
                if (requireGrabbing && !IsGrabbing) return false;   // 采集已停止，不再等待

                try
                {
                    // 取快照避免遍历期间字典被修改抛异常
                    var snapshot = solutionItem.IsProcessEnds.ToList();

                    bool anyRunning = false;
                    foreach (var kvp in snapshot)
                    {
                        if (kvp.Value != null && kvp.Value.Count > 0)
                        {
                            anyRunning = true;
                            if (!logged)
                            {
                WriteVerboseContinuousGrabLog($"[连续采集] WaitForFlowIdle: 等待 serial={kvp.Key} 完成 (剩余末端节点={kvp.Value.Count})");
                                logged = true;
                            }
                            break;
                        }
                    }

                    if (!anyRunning)
                        return true;
                }
                catch
                {
                    // 字典并发修改导致枚举失败，稍后重试
                    Thread.Sleep(50);
                    continue;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine($"[连续采集] ⚠ WaitForFlowIdle 超时({timeout.TotalSeconds}s)");
            return false;
        }

        /// <summary>
        /// 等待 TriggerWork 完成流程注册（IsProcessEnds 出现未完成末端），
        /// 避免固定 300ms 阻塞，同时防止下一帧过早抢跑。
        /// </summary>
        private bool WaitForFlowDispatch(TimeSpan timeout, bool requireGrabbing = true)
        {
            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem == null) return true;

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (requireGrabbing && !IsGrabbing) return false;

                try
                {
                    var snapshot = solutionItem.IsProcessEnds.ToList();
                    if (snapshot.Any(kvp => kvp.Value != null && kvp.Value.Count > 0))
                        return true;
                }
                catch
                {
                    // 并发修改时短暂重试
                }

                Thread.Sleep(10);
            }

            var status = PrismProvider.WorkStatusManager?.CurStatus ?? WorkStatus.None;
            Console.WriteLine($"[连续采集] ⚠ WaitForFlowDispatch 超时({timeout.TotalMilliseconds:F0}ms)，当前状态={status}");
            return false;
        }

        /// <summary>
        /// 清空帧队列并释放所有 HImage
        /// </summary>
        private void ClearFrameQueue()
        {
            if (_frameQueue == null) return;
            while (_frameQueue.TryDequeue(out var img))
                SafeDisposeImage(img);
        }

        private void ClearMultiFrameQueue()
        {
            if (_multiFrameQueue == null) return;
            while (_multiFrameQueue.TryDequeue(out var packet))
                DisposeMultiCameraPacket(packet);
        }

        private void ClearMultiPendingImages()
        {
            lock (_multiCameraSync)
            {
                ClearMultiPendingImagesLocked();
            }
        }

        private void ClearMultiPendingImagesLocked()
        {
            if (_multiPendingImages == null)
                return;

            foreach (var image in _multiPendingImages.Values)
                SafeDisposeImage(image);

            _multiPendingImages.Clear();
            _multiPendingStartedUtc = DateTime.MinValue;
        }

        private bool TryDequeueNextFrame(out HImage frameImage, out MultiCameraFramePacket framePacket)
        {
            frameImage = null;
            framePacket = null;

            if (UseMultiCameraGrab)
                return _multiFrameQueue.TryDequeue(out framePacket);

            return _frameQueue.TryDequeue(out frameImage);
        }

        private bool HasPendingFrameQueue()
        {
            return UseMultiCameraGrab
                ? !_multiFrameQueue.IsEmpty
                : !_frameQueue.IsEmpty;
        }

        private int GetActiveFrameQueueCount()
        {
            return UseMultiCameraGrab ? _multiFrameQueue.Count : _frameQueue.Count;
        }

        private void DisposeDequeuedFrame(HImage frameImage, MultiCameraFramePacket framePacket)
        {
            SafeDisposeImage(frameImage);
            DisposeMultiCameraPacket(framePacket);
        }

        public bool StartDebugRawImageSave(out string error)
        {
            EnsureRuntimeState();
            error = null;

            lock (_debugRawImageSaveLock)
            {
                if (_isDebugRawImageSaving)
                {
                    error = $"原图保存已开启，当前目录：{DebugRawImageSaveDirectory}";
                    return false;
                }
            }

            BlockingCollection<DebugRawImageSavePacket> queue;
            long sessionId;
            string saveDirectory;
            try
            {
                saveDirectory = Path.Combine(
                    GetDebugRawImageSaveRootDirectory(),
                    $"Node{Serial:D3}",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
                Directory.CreateDirectory(saveDirectory);
            }
            catch (Exception ex)
            {
                error = $"创建原图保存目录失败：{ex.Message}";
                return false;
            }

            lock (_debugRawImageSaveLock)
            {
                if (_isDebugRawImageSaving)
                {
                    error = $"原图保存已开启，当前目录：{DebugRawImageSaveDirectory}";
                    return false;
                }

                _debugRawImageSaveQueue?.CompleteAdding();
                queue = new BlockingCollection<DebugRawImageSavePacket>(MaxDebugRawImageSaveQueueSize);
                sessionId = Interlocked.Increment(ref _debugRawImageSaveSessionId);
                _debugRawImageSaveQueue = queue;
                _debugRawImageSaveDirectory = saveDirectory;
                Interlocked.Exchange(ref _debugRawImageSavedFrameCount, 0);
                Interlocked.Exchange(ref _debugRawImageSavedImageCount, 0);
                Interlocked.Exchange(ref _debugRawImageDroppedFrameCount, 0);
                _isDebugRawImageSaving = true;
                _ = Task.Run(() => SaveDebugRawImagesWorker(queue, sessionId));
            }

            RaiseDebugRawImageSavePropertiesChanged();
            Console.WriteLine($"[ContinuousGrab] Debug raw image save started: {saveDirectory}");
            return true;
        }

        public void StopDebugRawImageSave()
        {
            BlockingCollection<DebugRawImageSavePacket> queue = null;

            lock (_debugRawImageSaveLock)
            {
                if (!_isDebugRawImageSaving)
                    return;

                _isDebugRawImageSaving = false;
                queue = _debugRawImageSaveQueue;
                _debugRawImageSaveQueue = null;
            }

            try
            {
                queue?.CompleteAdding();
            }
            catch
            {
                // The worker may have already completed the queue.
            }

            RaiseDebugRawImageSavePropertiesChanged();
            Console.WriteLine($"[ContinuousGrab] Debug raw image save stopped: {DebugRawImageSaveDirectory}");
        }

        private string GetDebugRawImageSaveRootDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DebugRawImageSaveRootDirectory))
                return DebugRawImageSaveRootDirectory.Trim();

            var basePath = string.IsNullOrWhiteSpace(PrismProvider.AppBasePath)
                ? AppContext.BaseDirectory
                : PrismProvider.AppBasePath;
            return Path.Combine(basePath, "DebugRawImages");
        }

        private void QueueDebugRawImageSave(long frameNumber, IReadOnlyList<HImage> sourceImages)
        {
            if (sourceImages == null || sourceImages.Count == 0)
                return;

            BlockingCollection<DebugRawImageSavePacket> queue;
            string rootDirectory;
            long sessionId;
            lock (_debugRawImageSaveLock)
            {
                if (!_isDebugRawImageSaving
                    || _debugRawImageSaveQueue == null
                    || _debugRawImageSaveQueue.IsAddingCompleted)
                {
                    return;
                }

                queue = _debugRawImageSaveQueue;
                rootDirectory = _debugRawImageSaveDirectory;
                sessionId = _debugRawImageSaveSessionId;
            }

            List<HImage> copiedImages = null;
            DebugRawImageSavePacket packet = null;
            try
            {
                if (queue.Count >= MaxDebugRawImageSaveQueueSize)
                {
                    MarkDebugRawImageFrameDropped();
                    return;
                }

                copiedImages = new List<HImage>(Math.Min(sourceImages.Count, MaxMultiCameraCount));
                var count = Math.Min(sourceImages.Count, MaxMultiCameraCount);
                for (var i = 0; i < count; i++)
                {
                    var copiedImage = HalconImageOwnership.CopyForOutput(sourceImages[i]);
                    if (!HalconImageOwnership.IsInitializedSafe(copiedImage))
                    {
                        SafeDisposeImage(copiedImage);
                        continue;
                    }

                    copiedImages.Add(copiedImage);
                }

                if (copiedImages.Count == 0)
                    return;

                packet = new DebugRawImageSavePacket
                {
                    SessionId = sessionId,
                    FrameNumber = frameNumber,
                    RootDirectory = rootDirectory,
                    Images = copiedImages
                };
                copiedImages = null;

                if (!queue.TryAdd(packet))
                {
                    MarkDebugRawImageFrameDropped();
                    DisposeDebugRawImageSavePacket(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Queue debug raw image save failed: Frame={frameNumber}, Error={ex.Message}");
                DisposeDebugRawImageSavePacket(packet);
            }
            finally
            {
                if (copiedImages != null)
                {
                    foreach (var image in copiedImages)
                        SafeDisposeImage(image);
                }
            }
        }

        private void SaveDebugRawImagesWorker(BlockingCollection<DebugRawImageSavePacket> queue, long sessionId)
        {
            try
            {
                foreach (var packet in queue.GetConsumingEnumerable())
                {
                    try
                    {
                        SaveDebugRawImagePacket(packet);
                        if (sessionId == Interlocked.Read(ref _debugRawImageSaveSessionId))
                        {
                            Interlocked.Increment(ref _debugRawImageSavedFrameCount);
                            Interlocked.Add(ref _debugRawImageSavedImageCount, packet.Images?.Count ?? 0);
                            RaiseDebugRawImageSavePropertiesChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ContinuousGrab] Save debug raw image failed: Frame={packet?.FrameNumber}, Error={ex.Message}");
                    }
                    finally
                    {
                        DisposeDebugRawImageSavePacket(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Debug raw image save worker stopped with error: {ex.Message}");
            }
            finally
            {
                queue.Dispose();
            }
        }

        private static void SaveDebugRawImagePacket(DebugRawImageSavePacket packet)
        {
            if (packet?.Images == null || packet.Images.Count == 0 || string.IsNullOrWhiteSpace(packet.RootDirectory))
                return;

            var frameFileName = $"F{packet.FrameNumber:D6}{DebugRawImageSaveExtension}";
            for (var i = 0; i < packet.Images.Count; i++)
            {
                var image = packet.Images[i];
                if (!HalconImageOwnership.IsInitializedSafe(image))
                    continue;

                var cameraDirectory = Path.Combine(packet.RootDirectory, $"Cam{i + 1:D2}");
                Directory.CreateDirectory(cameraDirectory);
                var filePath = Path.Combine(cameraDirectory, frameFileName);
                image.WriteImage(DebugRawImageSaveFormat, 0, filePath);
            }
        }

        private void MarkDebugRawImageFrameDropped()
        {
            Interlocked.Increment(ref _debugRawImageDroppedFrameCount);
            RaiseDebugRawImageSavePropertiesChanged();
        }

        private void RaiseDebugRawImageSavePropertiesChanged()
        {
            void Raise()
            {
                RaisePropertyChanged(nameof(IsDebugRawImageSaving));
                RaisePropertyChanged(nameof(DebugRawImageSaveDirectory));
                RaisePropertyChanged(nameof(DebugRawImageSaveRootDirectoryDisplay));
                RaisePropertyChanged(nameof(DebugRawImageSavedFrameCount));
                RaisePropertyChanged(nameof(DebugRawImageSavedImageCount));
                RaisePropertyChanged(nameof(DebugRawImageDroppedFrameCount));
                RaisePropertyChanged(nameof(DebugRawImageSaveSummary));
            }

            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke((Action)Raise);
            else
                Raise();
        }

        private static void DisposeDebugRawImageSavePacket(DebugRawImageSavePacket packet)
        {
            if (packet?.Images == null)
                return;

            foreach (var image in packet.Images)
                SafeDisposeImage(image);

            packet.Images.Clear();
        }

        private bool HasReadyFrameForCurrentMode()
        {
            if (!UseMultiCameraGrab)
                return HalconImageOwnership.IsInitializedSafe(_image);

            var imageCount = UsePseudoGrab
                ? GetPseudoMultiCameraOutputCount()
                : ResolveMultiCameraModels().Count;
            if (imageCount <= 0)
                return false;

            for (var i = 0; i < imageCount; i++)
            {
                if (!HalconImageOwnership.IsInitializedSafe(GetPreviewImageByIndex(i)))
                    return false;
            }

            return true;
        }

        private List<CameraBase> ResolveMultiCameraModels()
        {
            return TryResolveMultiCameraModels(out var cameras, out _)
                ? cameras
                : new List<CameraBase>();
        }

        private bool TryResolveMultiCameraModels(out List<CameraBase> cameras, out string error)
        {
            cameras = new List<CameraBase>();
            var names = GetMultiCameraNameTokens();
            if (names.Count == 0)
            {
                error = "请填写多相机编号，例如：Cam1,Cam2,Cam3,Cam4,Cam5,Cam6";
                return false;
            }

            if (names.Count > MaxMultiCameraCount)
            {
                error = $"多相机模式最多支持 {MaxMultiCameraCount} 路，当前配置 {names.Count} 路";
                return false;
            }

            var duplicates = names
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                error = $"多相机编号重复：{string.Join(", ", duplicates)}";
                return false;
            }

            var allCameras = CameraModels?.ToList() ?? new List<CameraBase>();
            var missingNames = new List<string>();
            foreach (var name in names)
            {
                var camera = allCameras.FirstOrDefault(c =>
                    string.Equals(c.CameraNo, name, StringComparison.OrdinalIgnoreCase));
                if (camera == null)
                {
                    missingNames.Add(name);
                    continue;
                }

                cameras.Add(camera);
            }

            if (missingNames.Count > 0)
            {
                error = $"未找到相机：{string.Join(", ", missingNames)}";
                return false;
            }

            error = null;
            return cameras.Count > 0;
        }

        private bool TryEnsureCameraListConfigured(out string error)
        {
            EnsureRuntimeState();
            if (MultiCameraItems.Count == 0 && !string.IsNullOrWhiteSpace(MultiCameraNames))
                RefreshMultiCameraItemsFromNames();

            if (MultiCameraItems.Count == 0)
            {
                error = "请先在相机列表添加至少 1 路相机";
                return false;
            }

            if (MultiCameraItems.Count > MaxMultiCameraCount)
            {
                error = $"当前节点最多支持 {MaxMultiCameraCount} 路相机";
                return false;
            }

            var duplicates = MultiCameraItems
                .Where(item => item != null)
                .GroupBy(item => item.CameraNo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                error = $"相机列表重复：{string.Join(", ", duplicates)}";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryEnsurePseudoCameraListConfigured(out string error)
        {
            EnsureRuntimeState();
            if (PseudoCameraItems.Count == 0)
                RestorePseudoCameraItemsFromFolderConfigs();

            if (PseudoCameraItems.Count == 0)
            {
                error = "请先添加至少 1 路伪相机";
                return false;
            }

            if (PseudoCameraItems.Count > MaxMultiCameraCount)
            {
                error = $"当前节点最多支持 {MaxMultiCameraCount} 路伪相机";
                return false;
            }

            var duplicates = PseudoCameraItems
                .Where(item => item != null)
                .GroupBy(item => item.CameraNo, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                error = $"伪相机编号重复：{string.Join(", ", duplicates)}";
                return false;
            }

            error = null;
            return true;
        }

        private List<string> GetMultiCameraNameTokens()
        {
            if (string.IsNullOrWhiteSpace(MultiCameraNames))
                return new List<string>();

            return MultiCameraNames
                .Split(new[] { ',', ';', '，', '；', '|', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }

        public IReadOnlyList<string> GetMultiCameraOutputNamesForCurrentMode()
        {
            return UsePseudoGrab ? GetPseudoCameraOutputNames() : GetRealCameraOutputNames();
        }

        public IReadOnlyList<string> GetRealCameraOutputNames()
        {
            return GetCameraOutputNames(MultiCameraItems.Count);
        }

        public IReadOnlyList<string> GetPseudoCameraOutputNames()
        {
            return GetCameraOutputNames(PseudoCameraItems.Count);
        }

        private static IReadOnlyList<string> GetCameraOutputNames(int itemCount)
        {
            var count = Math.Min(itemCount, MaxMultiCameraCount);
            if (count <= 0)
                return Array.Empty<string>();

            return Enumerable.Range(1, count)
                .Select(index => $"Image{index}")
                .ToArray();
        }

        private int GetPseudoMultiCameraOutputCount()
        {
            return Math.Clamp(PseudoCameraItems.Count, 0, MaxMultiCameraCount);
        }

        public bool AddMultiCamera(CameraBase camera, out string error)
        {
            EnsureRuntimeState();
            if (camera == null)
            {
                error = "请先选择要加入当前节点的相机";
                return false;
            }

            if (MultiCameraItems.Count >= MaxMultiCameraCount)
            {
                error = $"当前节点最多添加 {MaxMultiCameraCount} 路相机";
                return false;
            }

            if (MultiCameraItems.Any(item => string.Equals(item.CameraNo, camera.CameraNo, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"相机 {camera.CameraNo} 已在当前节点列表中";
                return false;
            }

            MultiCameraItems.Add(CreateMultiCameraItem(camera.CameraNo, MultiCameraItems.Count));
            SyncMultiCameraNamesFromItems();
            RaisePropertyChanged(nameof(MultiCameraSummary));
            error = null;
            return true;
        }

        public bool RemoveMultiCamera(ContinuousGrabCameraStatusItem item, out string error)
        {
            EnsureRuntimeState();
            if (item == null)
            {
                error = "请先选择要移除的相机";
                return false;
            }

            MultiCameraItems.Remove(item);
            ReindexMultiCameraItems();
            SyncMultiCameraNamesFromItems();
            RaisePropertyChanged(nameof(MultiCameraSummary));
            error = null;
            return true;
        }

        public bool AddPseudoCamera(out string error)
        {
            EnsureRuntimeState();
            if (PseudoCameraItems.Count >= MaxMultiCameraCount)
            {
                error = $"当前节点最多添加 {MaxMultiCameraCount} 路伪相机";
                return false;
            }

            var index = PseudoCameraItems.Count;
            PseudoCameraItems.Add(new ContinuousGrabCameraStatusItem
            {
                Index = index + 1,
                CameraNo = GetNextPseudoCameraName(),
                OutputName = $"Image{index + 1}",
                Connected = false,
                ReceiveStatus = "未收图",
                SendStatus = "待发图"
            });

            RaisePropertyChanged(nameof(PseudoImageSummary));
            RaisePropertyChanged(nameof(PseudoModeSummary));
            SyncPseudoCameraFolderConfigsFromItems(allowEmpty: true);
            QueueCurrentSolutionSave();
            error = null;
            return true;
        }

        public bool RemovePseudoCamera(ContinuousGrabCameraStatusItem item, out string error)
        {
            EnsureRuntimeState();
            if (item == null)
            {
                error = "请先选择要移除的伪相机";
                return false;
            }

            PseudoCameraItems.Remove(item);
            ReindexPseudoCameraItems();
            RaisePropertyChanged(nameof(PseudoImageSummary));
            RaisePropertyChanged(nameof(PseudoModeSummary));
            SyncPseudoCameraFolderConfigsFromItems(allowEmpty: true);
            QueueCurrentSolutionSave();
            error = null;
            return true;
        }

        public void SetPseudoCameraFolder(ContinuousGrabCameraStatusItem item, string folder)
        {
            EnsureRuntimeState();
            if (item == null)
                return;

            item.SetPseudoImageFolder(folder);
            RaisePropertyChanged(nameof(PseudoImageSummary));
            RaisePropertyChanged(nameof(PseudoModeSummary));
            SyncPseudoCameraFolderConfigsFromItems(allowEmpty: true);
            QueueCurrentSolutionSave();
        }

        private void RefreshMultiCameraItemsFromNames()
        {
            EnsureRuntimeState();
            var names = GetMultiCameraNameTokens();
            InvokeOnUiThread(() =>
            {
                MultiCameraItems.Clear();
                for (var i = 0; i < Math.Min(names.Count, MaxMultiCameraCount); i++)
                    MultiCameraItems.Add(CreateMultiCameraItem(names[i], i));

                RaisePropertyChanged(nameof(MultiCameraItems));
            });
        }

        private ContinuousGrabCameraStatusItem CreateMultiCameraItem(string cameraNo, int index)
        {
            var camera = CameraModels.FirstOrDefault(c => string.Equals(c.CameraNo, cameraNo, StringComparison.OrdinalIgnoreCase));
            var item = new ContinuousGrabCameraStatusItem
            {
                Index = index + 1,
                CameraNo = cameraNo,
                OutputName = $"Image{index + 1}",
                Connected = camera?.Connected == true,
                ReceiveStatus = "未收图",
                SendStatus = "待发图"
            };

            return item;
        }

        private void ReindexMultiCameraItems()
        {
            for (var i = 0; i < MultiCameraItems.Count; i++)
            {
                MultiCameraItems[i].Index = i + 1;
                MultiCameraItems[i].OutputName = $"Image{i + 1}";
            }
        }

        private void ReindexPseudoCameraItems()
        {
            for (var i = 0; i < PseudoCameraItems.Count; i++)
            {
                PseudoCameraItems[i].Index = i + 1;
                PseudoCameraItems[i].OutputName = $"Image{i + 1}";
            }
        }

        private string GetNextPseudoCameraName()
        {
            for (var i = 1; i <= MaxMultiCameraCount; i++)
            {
                var name = $"PseudoCam{i}";
                if (!PseudoCameraItems.Any(item => string.Equals(item.CameraNo, name, StringComparison.OrdinalIgnoreCase)))
                    return name;
            }

            return $"PseudoCam{PseudoCameraItems.Count + 1}";
        }

        private void SyncMultiCameraNamesFromItems()
        {
            _isSyncingMultiCameraNames = true;
            try
            {
                MultiCameraNames = string.Join(",", MultiCameraItems.Select(item => item.CameraNo));
            }
            finally
            {
                _isSyncingMultiCameraNames = false;
            }
        }

        private void MarkMultiCameraWaiting()
        {
            BeginInvokeOnUiThread(() =>
            {
                foreach (var item in MultiCameraItems)
                {
                    var camera = CameraModels.FirstOrDefault(c => string.Equals(c.CameraNo, item.CameraNo, StringComparison.OrdinalIgnoreCase));
                    item.Connected = camera?.Connected == true;
                    item.ReceiveStatus = "等待帧";
                    item.SendStatus = "待组包";
                }
            });
        }

        private void MarkMultiCameraReceived(string cameraNo)
        {
            BeginInvokeOnUiThread(() =>
            {
                var item = MultiCameraItems.FirstOrDefault(x => string.Equals(x.CameraNo, cameraNo, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                    return;

                item.ReceiveCount++;
                item.LastReceiveTime = DateTime.Now;
                item.ReceiveStatus = "已收图";
                item.SendStatus = "等待同包";
            });
        }

        private void MarkPseudoMultiCameraReceived(int imageCount)
        {
            BeginInvokeOnUiThread(() =>
            {
                for (var i = 0; i < PseudoCameraItems.Count && i < imageCount; i++)
                {
                    var item = PseudoCameraItems[i];
                    item.ReceiveCount++;
                    item.LastReceiveTime = DateTime.Now;
                    item.ReceiveStatus = "伪收图";
                    item.SendStatus = "等待同包";
                }
            });
        }

        private void MarkPseudoCameraWaiting()
        {
            BeginInvokeOnUiThread(() =>
            {
                foreach (var item in PseudoCameraItems)
                {
                    item.ReceiveStatus = item.PseudoConnected ? "等待伪图" : "目录未就绪";
                    item.SendStatus = "待组包";
                }
            });
        }

        private void MarkMultiCameraPacketTimeout()
        {
            BeginInvokeOnUiThread(() =>
            {
                foreach (var item in MultiCameraItems)
                {
                    if (item.ReceiveStatus != "已收图")
                        item.ReceiveStatus = "超时未到";

                    item.SendStatus = "组包超时";
                }
            });
        }

        private void MarkMultiCameraPacketSent(long frameNumber)
        {
            BeginInvokeOnUiThread(() =>
            {
                foreach (var item in MultiCameraItems)
                {
                    item.SendCount++;
                    item.LastSendTime = DateTime.Now;
                    item.SendStatus = $"已发图 #{frameNumber}";
                }
            });
        }

        private void MarkPseudoCameraPacketSent(long frameNumber)
        {
            BeginInvokeOnUiThread(() =>
            {
                foreach (var item in PseudoCameraItems)
                {
                    item.SendCount++;
                    item.LastSendTime = DateTime.Now;
                    item.SendStatus = $"已发图 #{frameNumber}";
                }
            });
        }

        private bool TryReplaceOutputImagesFromValues(Dictionary<string, object> dataPointValues, string context)
        {
            var preparedImages = new List<(TransmitParam Param, HImage Image)>();
            try
            {
                foreach (var item in OutputParams)
                {
                    object value = null;
                    if (item != null && !string.IsNullOrWhiteSpace(item.ParamName))
                    {
                        dataPointValues?.TryGetValue(item.ParamName, out value);
                    }

                    HImage nextImage = CopyImageForOutput(value, item?.ParamName);
                    if (!HalconImageOwnership.IsInitializedSafe(nextImage))
                    {
                        LogInvalidOutputImage(context, item, value);
                        SafeDisposeImage(nextImage);
                        return false;
                    }

                    preparedImages.Add((item, nextImage));
                }

                ApplyPreparedOutputImages(preparedImages);
                preparedImages.Clear();
                return true;
            }
            finally
            {
                DisposePreparedOutputImages(preparedImages);
            }
        }

        private bool TryReplaceOutputImagesFromFrame(HImage frameImage, string context)
        {
            if (!HalconImageOwnership.IsInitializedSafe(frameImage))
            {
                LogInvalidOutputImage(context, null, frameImage);
                return false;
            }

            var preparedImages = new List<(TransmitParam Param, HImage Image)>();
            try
            {
                foreach (var item in OutputParams)
                {
                    HImage nextImage = HalconImageOwnership.CopyForOutput(frameImage);
                    if (!HalconImageOwnership.IsInitializedSafe(nextImage))
                    {
                        LogInvalidOutputImage(context, item, frameImage);
                        SafeDisposeImage(nextImage);
                        return false;
                    }

                    preparedImages.Add((item, nextImage));
                }

                ApplyPreparedOutputImages(preparedImages);
                preparedImages.Clear();
                return true;
            }
            finally
            {
                DisposePreparedOutputImages(preparedImages);
            }
        }

        private bool TryReplaceOutputImagesFromPacket(MultiCameraFramePacket packet, string context)
        {
            if (!IsValidMultiCameraPacket(packet))
            {
                LogInvalidOutputImage(context, null, packet);
                return false;
            }

            var preparedImages = new List<(TransmitParam Param, HImage Image)>();
            try
            {
                foreach (var item in OutputParams)
                {
                    var sourceImage = GetPacketImageForParam(packet, item?.ParamName);
                    if (!HalconImageOwnership.IsInitializedSafe(sourceImage))
                    {
                        LogInvalidOutputImage(context, item, sourceImage);
                        return false;
                    }

                    var nextImage = HalconImageOwnership.CopyForOutput(sourceImage);
                    if (!HalconImageOwnership.IsInitializedSafe(nextImage))
                    {
                        LogInvalidOutputImage(context, item, sourceImage);
                        SafeDisposeImage(nextImage);
                        return false;
                    }

                    preparedImages.Add((item, nextImage));
                }

                ApplyPreparedOutputImages(preparedImages);
                preparedImages.Clear();
                return true;
            }
            finally
            {
                DisposePreparedOutputImages(preparedImages);
            }
        }

        private bool TryReplacePreviewImagesFromPacket(MultiCameraFramePacket packet)
        {
            if (!IsValidMultiCameraPacket(packet))
                return false;

            HImage imageCopy = null;
            var imageCopies = new HImage[MaxMultiCameraCount];
            try
            {
                imageCopy = HalconImageOwnership.CopyForOutput(packet.Images[0]);
                if (!HalconImageOwnership.IsInitializedSafe(imageCopy))
                    return false;

                for (var i = 0; i < MaxMultiCameraCount; i++)
                {
                    if (i >= packet.Images.Count)
                        continue;

                    imageCopies[i] = HalconImageOwnership.CopyForOutput(packet.Images[i]);
                    if (!HalconImageOwnership.IsInitializedSafe(imageCopies[i]))
                        return false;
                }

                ReplacePreviewImage(ref _image, imageCopy, nameof(Image));
                imageCopy = null;
                ReplacePreviewImage(ref _image1, imageCopies[0], nameof(Image1));
                imageCopies[0] = null;
                ReplacePreviewImage(ref _image2, imageCopies[1], nameof(Image2));
                imageCopies[1] = null;
                ReplacePreviewImage(ref _image3, imageCopies[2], nameof(Image3));
                imageCopies[2] = null;
                ReplacePreviewImage(ref _image4, imageCopies[3], nameof(Image4));
                imageCopies[3] = null;
                ReplacePreviewImage(ref _image5, imageCopies[4], nameof(Image5));
                imageCopies[4] = null;
                ReplacePreviewImage(ref _image6, imageCopies[5], nameof(Image6));
                imageCopies[5] = null;
                return true;
            }
            finally
            {
                SafeDisposeImage(imageCopy);
                foreach (var image in imageCopies)
                    SafeDisposeImage(image);
            }
        }

        private void ApplyPreparedOutputImages(List<(TransmitParam Param, HImage Image)> preparedImages)
        {
            EnsureRuntimeState();

            lock (_outputSnapshotLock)
            {
                foreach (var prepared in preparedImages)
                {
                    ReplaceOutputImageValue(prepared.Param, prepared.Image);
                }
            }
        }

        private static void DisposePreparedOutputImages(List<(TransmitParam Param, HImage Image)> preparedImages)
        {
            if (preparedImages == null)
                return;

            foreach (var prepared in preparedImages)
            {
                SafeDisposeImage(prepared.Image);
            }
        }

        private Dictionary<string, object> BuildOwnedOutputSnapshot()
        {
            var source = (OutputParams ?? new ObservableCollection<TransmitParam>())
                .Where(item => item != null)
                .ToDictionary(item => item.Guid.ToString(), item => (object)item);

            return HalconTransmitParamOwnership.CloneTransmitParams(source);
        }

        private void UpdateGlobalParamsFromSnapshot(IEnumerable<TransmitParam> snapshotOutputParams)
        {
            var globalParams = PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams;
            if (globalParams == null)
                return;

            foreach (TransmitParam item in snapshotOutputParams?.Where(item => item?.IsGlobal == true)
                         ?? Enumerable.Empty<TransmitParam>())
            {
                TransmitParam existingParam = globalParams.FirstOrDefault(p => p.Guid == item.Guid)
                    ?? globalParams.FirstOrDefault(p => p.Serial == item.Serial && p.Name == item.Name);

                if (existingParam == null)
                {
                    TransmitParam globalCopy = CloneTransmitParamForGlobal(item);
                    if (globalCopy == null)
                        continue;

                    if (PrismProvider.Dispatcher != null)
                    {
                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(globalCopy);
                        });
                    }
                    else
                    {
                        globalParams.Add(globalCopy);
                    }
                }
                else
                {
                    existingParam.Value = HalconTransmitParamOwnership.CloneValueForParameterIsolation(item.Value);
                }
            }

            var emptyGlobals = globalParams
                .Where(p => p.Serial == Serial && p.Value == null)
                .ToList();

            foreach (TransmitParam item in emptyGlobals)
            {
                globalParams.Remove(item);
            }
        }

        private static TransmitParam CloneTransmitParamForGlobal(TransmitParam source)
        {
            return HalconTransmitParamOwnership.CloneTransmitParamValue(source) as TransmitParam;
        }

        private void RetireOwnedOutputSnapshot(Dictionary<string, object> snapshot)
        {
            if (snapshot == null)
                return;

            _retiredOutputSnapshots ??= new Queue<Dictionary<string, object>>();
            _retiredOutputSnapshots.Enqueue(snapshot);

            while (_retiredOutputSnapshots.Count > MaxRetainedOutputSnapshotCount)
            {
                DisposeOwnedOutputSnapshot(_retiredOutputSnapshots.Dequeue());
            }
        }

        private void DisposeAllOwnedOutputSnapshots()
        {
            EnsureRuntimeState();

            lock (_outputSnapshotLock)
            {
                DisposeOwnedOutputSnapshot(_ownedOutputSnapshot);
                _ownedOutputSnapshot = null;

                while (_retiredOutputSnapshots.Count > 0)
                {
                    DisposeOwnedOutputSnapshot(_retiredOutputSnapshots.Dequeue());
                }
            }
        }

        private static void DisposeOwnedOutputSnapshot(Dictionary<string, object> snapshot)
        {
            if (snapshot == null)
                return;

            foreach (object value in snapshot.Values)
            {
                HalconTransmitParamOwnership.DisposeOwnedTransmitValue(value);
            }
        }

        private HImage GetPacketImageForParam(MultiCameraFramePacket packet, string paramName)
        {
            if (packet?.Images == null || !TryGetImageIndexFromParamName(paramName, out var index))
                return null;

            return index >= 0 && index < packet.Images.Count
                ? packet.Images[index]
                : null;
        }

        private static bool TryGetImageIndexFromParamName(string paramName, out int index)
        {
            index = -1;
            if (string.Equals(paramName, "Image", StringComparison.OrdinalIgnoreCase))
            {
                index = 0;
                return true;
            }

            const string prefix = "Image";
            if (string.IsNullOrWhiteSpace(paramName)
                || !paramName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(paramName.Substring(prefix.Length), out var imageNo))
                return false;

            index = imageNo - 1;
            return index >= 0 && index < MaxMultiCameraCount;
        }

        private HImage GetPreviewImageByIndex(int index)
        {
            return index switch
            {
                0 => _image1,
                1 => _image2,
                2 => _image3,
                3 => _image4,
                4 => _image5,
                5 => _image6,
                _ => null
            };
        }

        private static bool IsValidMultiCameraPacket(MultiCameraFramePacket packet)
        {
            return packet?.Images != null
                && packet.Images.Count > 0
                && packet.Images.All(HalconImageOwnership.IsInitializedSafe);
        }

        private static void DisposeMultiCameraPacket(MultiCameraFramePacket packet)
        {
            if (packet?.Images == null)
                return;

            foreach (var image in packet.Images)
                SafeDisposeImage(image);

            packet.Images.Clear();
        }

        private void ReplacePreviewImage(ref HImage target, HImage nextImage, string propertyName)
        {
            var oldImage = target;
            target = nextImage;

            if (oldImage != null && !ReferenceEquals(oldImage, nextImage))
                SafeDisposeImage(oldImage);

            RaisePropertyChanged(propertyName);
        }

        private bool IsModelPreviewImage(HImage image)
        {
            return ReferenceEquals(image, _image)
                || ReferenceEquals(image, _image1)
                || ReferenceEquals(image, _image2)
                || ReferenceEquals(image, _image3)
                || ReferenceEquals(image, _image4)
                || ReferenceEquals(image, _image5)
                || ReferenceEquals(image, _image6);
        }

        private void LogInvalidOutputImage(string context, TransmitParam item, object sourceValue)
        {
            Logs.LogWarning(
                $"[连续采集] 输出图像无效: Context={context}, Serial={Serial}, Frame={_frameCount}, " +
                $"Param={DescribeOutputParamForLog(item)}, Source={DescribeOutputSourceForLog(sourceValue)}");
        }

        private static string DescribeOutputParamForLog(TransmitParam item)
        {
            if (item == null)
                return "null";

            return $"Guid={item.Guid}, Name={item.Name}, ParamName={item.ParamName}, Type={item.Type}, Value={DescribeOutputSourceForLog(item.Value)}";
        }

        private static string DescribeOutputSourceForLog(object value)
        {
            if (value == null)
                return "null";

            if (value is HObject hObject)
            {
                if (HalconImageOwnership.TryGetImageSize(hObject, out int width, out int height, out string error))
                    return $"HObject({width}x{height})";

                return $"HObject(invalid:{error})";
            }

            return value.GetType().Name;
        }

        private HImage CopyImageForOutput(object value, string paramName)
        {
            if (value is not HImage image)
                return null;

            try
            {
                return HalconImageOwnership.CopyForOutput(image);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContinuousGrab] Copy output image failed: Param={paramName}, Error={ex.Message}");
                return null;
            }
        }

        private void ReplaceOutputImageValue(TransmitParam item, HImage nextImage)
        {
            if (item == null)
            {
                SafeDisposeImage(nextImage);
                return;
            }

            var oldImage = item.Value as HImage;
            item.Value = nextImage;

            // Do not let OutputParams own the preview handle; downstream receives copies only.
            if (oldImage != null &&
                !ReferenceEquals(oldImage, nextImage) &&
                !IsModelPreviewImage(oldImage))
            {
                SafeDisposeImage(oldImage);
            }
        }

        private static void SafeDisposeImage(HImage image)
        {
            HalconImageOwnership.DisposeOwned(image);
        }

        private static void SafeDisposeImage(ref HImage image)
        {
            HalconImageOwnership.DisposeOwned(ref image);
        }
        #endregion

        /// <summary>
        /// 看门狗定时器 - 检测回调是否停止，如果停止则重启采集
        /// </summary>
        private void OnWatchdogTick(object sender, ElapsedEventArgs e)
        {
            if (!IsGrabbing || IsPseudoGrabActive) return;

            if (IsMultiCameraGrabActive)
            {
                var packetElapsed = DateTime.UtcNow - _multiPendingStartedUtc;
                var elapsed = DateTime.Now - _lastFrameTime;
                if (elapsed.TotalSeconds <= 3
                    && (_multiPendingStartedUtc == DateTime.MinValue || packetElapsed.TotalSeconds <= 3))
                {
                    return;
                }

                try
                {
                    ClearMultiPendingImages();
                    List<CameraBase> cameras;
                    lock (_multiCameraSync)
                    {
                        cameras = _multiCameraHandlers.Keys.ToList();
                    }

                    foreach (var camera in cameras)
                        camera.EndCollect();

                    System.Threading.Thread.Sleep(100);

                    foreach (var camera in cameras)
                        camera.StartCollect();

                    _lastFrameTime = DateTime.Now;
                }
                catch
                {
                    // 重启采集失败
                }

                return;
            }

            if (SelectedCamera == null) return;

            var singleElapsed = DateTime.Now - _lastFrameTime;
            if (singleElapsed.TotalSeconds > 3)
            {
                try
                {
                    // 停止当前采集
                    SelectedCamera.EndCollect();
                    System.Threading.Thread.Sleep(100);

                    // 重新启动采集
                    SelectedCamera.StartCollect();
                    _lastFrameTime = DateTime.Now;
                }
                catch
                {
                    // 重启采集失败
                }
            }
        }

        private void StopWatchdog()
        {
            if (_watchdogTimer == null)
                return;

            _watchdogTimer.Enabled = false;
            _watchdogTimer.Dispose();
            _watchdogTimer = null;
        }

        private void RefreshPseudoImageFolderState()
        {
            LoadPseudoImageFiles();
            RaisePropertyChanged(nameof(PseudoImageCount));
            RaisePropertyChanged(nameof(PseudoImageSummary));
        }

        private void RefreshPseudoCameraItemsState()
        {
            ReindexPseudoCameraItems();
            foreach (var item in PseudoCameraItems)
                item.TryEnsurePseudoImagesLoaded(out _);

            RaisePropertyChanged(nameof(PseudoCameraItems));
            RaisePropertyChanged(nameof(PseudoImageSummary));
            RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
        }

        private void RestorePseudoCameraItemsFromFolderConfigs()
        {
            EnsureRuntimeState();
            if (PseudoCameraItems.Count > 0)
            {
                ReindexPseudoCameraItems();
                return;
            }

            if (PseudoCameraFolderConfigs?.Count > 0)
            {
                PseudoCameraItems.Clear();
                foreach (var config in PseudoCameraFolderConfigs.Take(MaxMultiCameraCount))
                {
                    var index = PseudoCameraItems.Count + 1;
                    var item = new ContinuousGrabCameraStatusItem
                    {
                        Index = index,
                        CameraNo = string.IsNullOrWhiteSpace(config.CameraNo) ? $"PseudoCam{index}" : config.CameraNo,
                        OutputName = string.IsNullOrWhiteSpace(config.OutputName) ? $"Image{index}" : config.OutputName,
                        Connected = false
                    };

                    item.SetPseudoImageFolder(config.PseudoImageFolder);
                    PseudoCameraItems.Add(item);
                }

                ReindexPseudoCameraItems();
                return;
            }

            if (PseudoCameraItems.Count != 0 || string.IsNullOrWhiteSpace(PseudoImageFolder))
                return;

            var legacyItem = new ContinuousGrabCameraStatusItem
            {
                Index = 1,
                CameraNo = "PseudoCam1",
                OutputName = "Image1",
                Connected = false
            };
            legacyItem.SetPseudoImageFolder(PseudoImageFolder);
            PseudoCameraItems.Add(legacyItem);
            SyncPseudoCameraFolderConfigsFromItems(allowEmpty: true);
        }

        private void SyncPseudoCameraFolderConfigsFromItems(bool allowEmpty)
        {
            _pseudoCameraItems ??= new ObservableCollection<ContinuousGrabCameraStatusItem>();
            if (PseudoCameraItems.Count == 0 && !allowEmpty && PseudoCameraFolderConfigs?.Count > 0)
                return;

            PseudoCameraFolderConfigs = PseudoCameraItems
                .Take(MaxMultiCameraCount)
                .Select((item, index) => new ContinuousGrabPseudoCameraConfig
                {
                    Index = index + 1,
                    CameraNo = string.IsNullOrWhiteSpace(item.CameraNo) ? $"PseudoCam{index + 1}" : item.CameraNo,
                    OutputName = string.IsNullOrWhiteSpace(item.OutputName) ? $"Image{index + 1}" : item.OutputName,
                    PseudoImageFolder = item.PseudoImageFolder ?? string.Empty
                })
                .ToList();
        }

        private void QueueCurrentSolutionSave()
        {
            try
            {
                var solutionManager = PrismProvider.ProjectManager?.SolutionManager;
                var nodifyAppView = solutionManager?.GetItem("NodifyAppView");
                if (nodifyAppView != null)
                    solutionManager.UpdateItem("NodifyAppView", nodifyAppView);
            }
            catch (Exception ex)
            {
                Logs.LogError($"[ContinuousGrab] Queue save pseudo camera folder config failed: {ex.Message}");
            }
        }

        private bool TryEnsurePseudoImagesLoaded(out string error)
        {
            if (!TryEnsurePseudoCameraListConfigured(out error))
                return false;

            foreach (var item in PseudoCameraItems)
            {
                if (!item.TryEnsurePseudoImagesLoaded(out var itemError))
                {
                    error = $"{item.OutputName}/{item.CameraNo}: {itemError}";
                    return false;
                }
            }

            error = null;
            RaisePropertyChanged(nameof(PseudoImageSummary));
            return true;
        }

        private void LoadPseudoImageFiles()
        {
            _pseudoImageFiles = PseudoImageFileOrder.LoadSupportedImageFiles(PseudoImageFolder).ToArray();
            ReplacePseudoImageOrderItems();
        }

        private async Task RunPseudoGrabLoopAsync(CancellationTokenSource pseudoGrabCts)
        {
            var cancellationToken = pseudoGrabCts.Token;
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsGrabbing && IsPseudoGrabActive)
                {
                    try
                    {
                        if (!TryGetNextPseudoImagePathsByCamera(out var imagePaths))
                        {
                            Console.WriteLine("[连续采集] 伪相机目录剩余图片不足，无法组成完整图包");
                            break;
                        }

                        AcceptPseudoImageFilesAsMultiCameraPacket(imagePaths, forceFlowTrigger: false, requireGrabbingForFlow: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[连续采集] 伪采集读取图片失败: {ex.Message}");
                    }

                    if (PseudoWaitForFlowCompletionBeforeRead && !WaitForPseudoFlowCompletion(cancellationToken))
                    {
                        break;
                    }

                    try
                    {
                        await Task.Delay(GrabInterval, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                if (ReferenceEquals(_pseudoGrabCts, pseudoGrabCts))
                {
                    IsPseudoGrabActive = false;
                    IsGrabbing = false;
                    _pseudoGrabCts.Dispose();
                    _pseudoGrabCts = null;
                    _pseudoGrabTask = null;
                }
            }
        }

        private bool WaitForPseudoFlowCompletion(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || !IsGrabbing)
                return false;

            var flowRegistered = WaitForFlowDispatch(TimeSpan.FromMilliseconds(500), requireGrabbing: true);
            if (cancellationToken.IsCancellationRequested || !IsGrabbing)
                return false;

            if (!flowRegistered)
            {
                Console.WriteLine("[连续采集] 伪采集未检测到流程注册，按空闲处理");
                return true;
            }

            WriteVerboseContinuousGrabLog("[连续采集] 伪采集等待本帧流程完成后再读取下一张");
            while (!cancellationToken.IsCancellationRequested && IsGrabbing)
            {
                if (WaitForDownstreamLeafNodesCompleted(TimeSpan.FromSeconds(30), requireGrabbing: true))
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested || !IsGrabbing)
                    return false;

                if (!HasActiveDownstreamFlowNode())
                {
                    Console.WriteLine("[连续采集] 伪采集等待末端节点状态超时，但后续链路无运行中节点，重置流程结束标记并继续读取下一张");
                    TryResetStuckProcessEnds();
                    return true;
                }

                WriteVerboseContinuousGrabLog("[连续采集] 伪采集仍在等待流程完成，后续链路仍有运行中节点");
            }

            return false;
        }

        private bool StopGrabIfDownstreamFlowFailed(string context)
        {
            if (!TryFindDownstreamFailedNode(out int serial, out string title, out NodeStatus status))
                return false;

            Console.WriteLine($"[连续采集] {context}检测到后续节点异常，Serial={serial}, Title={title}, Status={status}，停止取图并不再触发后续流程");
            StopGrab();
            return true;
        }

        private bool TryFindDownstreamFailedNode(out int serial, out string title, out NodeStatus status)
        {
            serial = -1;
            title = string.Empty;
            status = NodeStatus.None;

            object nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches;
            if (nodeCaches == null)
                return false;

            object currentNode = EnumerateObjects(nodeCaches)
                .FirstOrDefault(node => TryGetNodeSerial(node, out int nodeSerial) && nodeSerial == Serial);
            if (currentNode == null)
                return false;

            var visited = new HashSet<object>();
            return TryFindFailedNodeInDownstream(currentNode, visited, out serial, out title, out status);
        }

        private bool TryFindFailedNodeInDownstream(
            object node,
            HashSet<object> visited,
            out int serial,
            out string title,
            out NodeStatus status)
        {
            serial = -1;
            title = string.Empty;
            status = NodeStatus.None;

            if (node == null || !visited.Add(node))
                return false;

            foreach (object nextNode in EnumerateNodeCollection(node, "NextNodes"))
            {
                if (TryGetNodeStatus(nextNode, out NodeStatus nextStatus)
                    && IsDownstreamStopStatus(nextStatus))
                {
                    TryGetNodeSerial(nextNode, out serial);
                    title = TryGetNodeTitle(nextNode);
                    status = nextStatus;
                    return true;
                }

                if (TryFindFailedNodeInDownstream(nextNode, visited, out serial, out title, out status))
                    return true;
            }

            return false;
        }

        private static bool IsDownstreamStopStatus(NodeStatus status)
        {
            return status == NodeStatus.NoParam;
        }

        private void TryResetStuckProcessEnds()
        {
            var processEnds = PrismProvider.ProjectManager?.SltCurSolutionItem?.IsProcessEnds;
            if (processEnds == null)
                return;

            try
            {
                foreach (var kv in processEnds.ToList())
                {
                    kv.Value?.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[连续采集] 重置流程结束标记失败: {ex.Message}");
            }
        }

        private bool HasActiveDownstreamFlowNode()
        {
            object nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches;
            if (nodeCaches == null)
                return false;

            object currentNode = EnumerateObjects(nodeCaches)
                .FirstOrDefault(node => TryGetNodeSerial(node, out int nodeSerial) && nodeSerial == Serial);
            if (currentNode == null)
                return false;

            var visited = new HashSet<object>();
            return HasActiveNodeInDownstream(currentNode, visited);
        }

        private bool WaitForDownstreamLeafNodesCompleted(TimeSpan timeout, bool requireGrabbing = true)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (requireGrabbing && !IsGrabbing)
                    return false;

                if (AreRegisteredProcessEndsCleared())
                {
                    if (!HasActiveDownstreamFlowNode())
                    {
                        return true;
                    }
                }

                if (PrismProvider.WorkStatusManager?.CurStatus == WorkStatus.Running)
                {
                    Thread.Sleep(50);
                    continue;
                }

                if (AreDownstreamLeafNodesCompleted())
                {
                    if (!HasActiveDownstreamFlowNode())
                    {
                        return true;
                    }
                }

                Thread.Sleep(50);
            }

            Console.WriteLine($"[连续采集] ⚠ WaitForDownstreamLeafNodesCompleted 超时({timeout.TotalSeconds}s)");
            return false;
        }

        private bool AreRegisteredProcessEndsCleared()
        {
            var processEnds = PrismProvider.ProjectManager?.SltCurSolutionItem?.IsProcessEnds;
            if (processEnds == null)
                return false;

            return processEnds.TryGetValue(Serial, out var ends)
                && (ends == null || ends.Count == 0);
        }

        private bool AreDownstreamLeafNodesCompleted()
        {
            var leafNodes = GetDownstreamLeafNodes();
            if (leafNodes.Count == 0)
                return true;

            foreach (var leafNode in leafNodes)
            {
                if (!TryGetNodeStatus(leafNode, out NodeStatus status))
                    return false;

                if (status == NodeStatus.None
                    || status == NodeStatus.Waiting
                    || status == NodeStatus.Running)
                {
                    return false;
                }
            }

            return true;
        }

        private List<object> GetDownstreamLeafNodes()
        {
            var result = new List<object>();
            object nodeCaches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeCaches;
            if (nodeCaches == null)
                return result;

            object currentNode = EnumerateObjects(nodeCaches)
                .FirstOrDefault(node => TryGetNodeSerial(node, out int nodeSerial) && nodeSerial == Serial);
            if (currentNode == null)
                return result;

            var visited = new HashSet<object>();
            CollectDownstreamLeafNodes(currentNode, visited, result);
            return result;
        }

        private void CollectDownstreamLeafNodes(object node, HashSet<object> visited, List<object> leafNodes)
        {
            if (node == null || !visited.Add(node))
                return;

            var nextNodes = EnumerateNodeCollection(node, "NextNodes").ToList();
            if (nextNodes.Count == 0)
            {
                leafNodes.Add(node);
                return;
            }

            foreach (var nextNode in nextNodes)
            {
                CollectDownstreamLeafNodes(nextNode, visited, leafNodes);
            }
        }

        private bool HasActiveNodeInDownstream(object node, HashSet<object> visited)
        {
            if (node == null || !visited.Add(node))
                return false;

            foreach (object nextNode in EnumerateNodeCollection(node, "NextNodes"))
            {
                if (TryGetNodeStatus(nextNode, out NodeStatus nextStatus)
                    && (nextStatus == NodeStatus.Running || nextStatus == NodeStatus.Waiting))
                {
                    return true;
                }

                if (HasActiveNodeInDownstream(nextNode, visited))
                    return true;
            }

            return false;
        }

        private static IEnumerable<object> EnumerateNodeCollection(object node, string propertyName)
        {
            object value = node.GetType().GetProperty(propertyName)?.GetValue(node);
            return EnumerateObjects(value);
        }

        private static IEnumerable<object> EnumerateObjects(object value)
        {
            if (value is not IEnumerable enumerable)
                yield break;

            foreach (object item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        private static bool TryGetNodeStatus(object node, out NodeStatus status)
        {
            status = NodeStatus.None;
            object value = node.GetType().GetProperty("CurStatus")?.GetValue(node);
            if (value is not NodeStatus nodeStatus)
                return false;

            status = nodeStatus;
            return true;
        }

        private static bool TryGetNodeSerial(object node, out int serial)
        {
            serial = -1;
            object menuInfo = node.GetType().GetProperty("MenuInfo")?.GetValue(node);
            object value = menuInfo?.GetType().GetProperty("Serial")?.GetValue(menuInfo);
            if (value is not int nodeSerial)
                return false;

            serial = nodeSerial;
            return true;
        }

        private static string TryGetNodeTitle(object node)
        {
            object value = node.GetType().GetProperty("Title")?.GetValue(node);
            if (value is string title && !string.IsNullOrWhiteSpace(title))
                return title;

            object menuInfo = node.GetType().GetProperty("MenuInfo")?.GetValue(node);
            value = menuInfo?.GetType().GetProperty("Title")?.GetValue(menuInfo);
            return value as string ?? string.Empty;
        }

        private bool TryGetNextPseudoImagePath(out string imagePath)
        {
            imagePath = null;
            if (_pseudoImageFiles == null || _pseudoImageFiles.Length == 0)
                return false;

            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            if (!_pseudoReadPlanner.TryGetNext(
                _pseudoImageFiles.Length,
                PseudoLoopEnabled,
                PseudoLoopCount,
                out _pseudoImageIndex,
                out _pseudoLoopIndex))
            {
                return false;
            }

            imagePath = _pseudoImageFiles[_pseudoImageIndex];
            MarkCurrentPseudoImage(imagePath);
            return !string.IsNullOrWhiteSpace(imagePath);
        }

        private bool TryGetNextPseudoImagePaths(int count, out IReadOnlyList<string> imagePaths)
        {
            imagePaths = Array.Empty<string>();
            if (count <= 0 || _pseudoImageFiles == null || _pseudoImageFiles.Length == 0)
                return false;

            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            if (!_pseudoReadPlanner.TryGetNextBatch(
                    _pseudoImageFiles.Length,
                    count,
                    PseudoLoopEnabled,
                    PseudoLoopCount,
                    out var indexes,
                    out _pseudoLoopIndex))
            {
                return false;
            }

            var paths = indexes
                .Select(index => _pseudoImageFiles[index])
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            if (paths.Length != count)
                return false;

            _pseudoImageIndex = indexes.Last();
            imagePaths = paths;
            MarkCurrentPseudoImages(paths);
            return true;
        }

        private bool TryGetNextPseudoImagePathsByCamera(out IReadOnlyList<string> imagePaths)
        {
            imagePaths = Array.Empty<string>();
            if (PseudoCameraItems.Count == 0)
                return false;

            var paths = new List<string>(PseudoCameraItems.Count);
            foreach (var item in PseudoCameraItems)
            {
                if (!item.TryGetNextPseudoImagePath(PseudoLoopEnabled, PseudoLoopCount, out var imagePath))
                    return false;

                paths.Add(imagePath);
            }

            imagePaths = paths;
            RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
            return true;
        }

        private void ReplacePseudoImageOrderItems()
        {
            var items = (_pseudoImageFiles ?? Array.Empty<string>())
                .Select((path, index) => new PseudoImageOrderItem
                {
                    Index = index + 1,
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                })
                .ToArray();

            InvokeOnUiThread(() =>
            {
                PseudoImageOrderItems.Clear();
                foreach (var item in items)
                    PseudoImageOrderItems.Add(item);

                RaisePropertyChanged(nameof(PseudoImageOrderItems));
                RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
            });
        }

        private void ResetPseudoGrabDisplayState()
        {
            InvokeOnUiThread(() =>
            {
                foreach (var item in PseudoImageOrderItems)
                    item.Reset();

                RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
            });
        }

        private void ResetPseudoGrabOrderState()
        {
            _pseudoImageIndex = 0;
            _pseudoLoopIndex = 0;
            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            _pseudoReadPlanner.Reset();
            foreach (var item in PseudoCameraItems)
                item.ResetPseudoOrder();
            ResetPseudoGrabDisplayState();
        }

        private void MarkCurrentPseudoImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            InvokeOnUiThread(() =>
            {
                foreach (var item in PseudoImageOrderItems)
                {
                    if (string.Equals(item.FullPath, imagePath, StringComparison.OrdinalIgnoreCase))
                        item.MarkCurrent();
                    else
                        item.MarkNotCurrent();
                }

                RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
            });
        }

        private void MarkCurrentPseudoImages(IReadOnlyCollection<string> imagePaths)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                return;

            var pathSet = new HashSet<string>(imagePaths.Where(path => !string.IsNullOrWhiteSpace(path)), StringComparer.OrdinalIgnoreCase);
            InvokeOnUiThread(() =>
            {
                foreach (var item in PseudoImageOrderItems)
                {
                    if (pathSet.Contains(item.FullPath))
                        item.MarkCurrent();
                    else
                        item.MarkNotCurrent();
                }

                RaisePropertyChanged(nameof(PseudoCurrentImageSummary));
            });
        }

        private static void WriteVerboseContinuousGrabLog(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#else
            if (IsVerboseContinuousGrabLogEnabled())
            {
                Console.WriteLine(message);
            }
#endif
        }

        private static bool IsVerboseContinuousGrabLogEnabled()
        {
            string value = Environment.GetEnvironmentVariable("REEYIN_VERBOSE_CONTINUOUS_GRAB_LOG");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void InvokeOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.Invoke(action);
        }

        private static void BeginInvokeOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action);
        }
        #endregion
    }

    public class ContinuousGrabPseudoCameraConfig
    {
        public int Index { get; set; }

        public string CameraNo { get; set; } = string.Empty;

        public string OutputName { get; set; } = string.Empty;

        public string PseudoImageFolder { get; set; } = string.Empty;
    }

    public class ContinuousGrabCameraStatusItem : BindableBase
    {
        private string[] _pseudoImageFiles = Array.Empty<string>();
        private PseudoImageReadPlanner _pseudoReadPlanner = new();

        private int _index;
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        private string _cameraNo = string.Empty;
        public string CameraNo
        {
            get => _cameraNo;
            set => SetProperty(ref _cameraNo, value ?? string.Empty);
        }

        private string _outputName = string.Empty;
        public string OutputName
        {
            get => _outputName;
            set => SetProperty(ref _outputName, value ?? string.Empty);
        }

        private bool _connected;
        public bool Connected
        {
            get => _connected;
            set
            {
                if (SetProperty(ref _connected, value))
                {
                    RaisePropertyChanged(nameof(ConnectedText));
                    RaisePropertyChanged(nameof(ConnectionText));
                }
            }
        }

        public string ConnectedText => Connected ? "已连接" : "未连接";

        public string ConnectionText => Connected ? "真机" : PseudoConnected ? "伪相机" : "未连接";

        private string _pseudoImageFolder = string.Empty;
        [JsonProperty]
        public string PseudoImageFolder
        {
            get => _pseudoImageFolder;
            private set
            {
                if (SetProperty(ref _pseudoImageFolder, value ?? string.Empty))
                {
                    RaisePropertyChanged(nameof(PseudoImageFolderDisplay));
                    RaisePropertyChanged(nameof(PseudoConnected));
                    RaisePropertyChanged(nameof(ConnectionText));
                }
            }
        }

        private int _pseudoImageCount;
        public int PseudoImageCount
        {
            get => _pseudoImageCount;
            private set
            {
                if (SetProperty(ref _pseudoImageCount, value))
                {
                    RaisePropertyChanged(nameof(PseudoImageFolderDisplay));
                    RaisePropertyChanged(nameof(PseudoConnected));
                    RaisePropertyChanged(nameof(ConnectionText));
                }
            }
        }

        private string _currentPseudoFileName = string.Empty;
        public string CurrentPseudoFileName
        {
            get => _currentPseudoFileName;
            private set => SetProperty(ref _currentPseudoFileName, value ?? string.Empty);
        }

        public bool PseudoConnected => !string.IsNullOrWhiteSpace(PseudoImageFolder) && PseudoImageCount > 0;

        public string PseudoImageFolderDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PseudoImageFolder))
                    return "未选择";

                var folderName = Path.GetFileName(PseudoImageFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrWhiteSpace(folderName))
                    folderName = PseudoImageFolder;

                return $"{folderName} ({PseudoImageCount})";
            }
        }

        private string _receiveStatus = "未收图";
        public string ReceiveStatus
        {
            get => _receiveStatus;
            set => SetProperty(ref _receiveStatus, value ?? string.Empty);
        }

        private string _sendStatus = "待发图";
        public string SendStatus
        {
            get => _sendStatus;
            set => SetProperty(ref _sendStatus, value ?? string.Empty);
        }

        private int _receiveCount;
        public int ReceiveCount
        {
            get => _receiveCount;
            set => SetProperty(ref _receiveCount, value);
        }

        private int _sendCount;
        public int SendCount
        {
            get => _sendCount;
            set => SetProperty(ref _sendCount, value);
        }

        private DateTime? _lastReceiveTime;
        public DateTime? LastReceiveTime
        {
            get => _lastReceiveTime;
            set
            {
                if (SetProperty(ref _lastReceiveTime, value))
                    RaisePropertyChanged(nameof(LastReceiveText));
            }
        }

        public string LastReceiveText => LastReceiveTime.HasValue
            ? LastReceiveTime.Value.ToString("HH:mm:ss.fff")
            : "-";

        private DateTime? _lastSendTime;
        public DateTime? LastSendTime
        {
            get => _lastSendTime;
            set => SetProperty(ref _lastSendTime, value);
        }

        public void SetPseudoImageFolder(string folder)
        {
            PseudoImageFolder = folder ?? string.Empty;
            RefreshPseudoImages();
            ResetPseudoOrder();
        }

        public bool TryEnsurePseudoImagesLoaded(out string error)
        {
            RefreshPseudoImages();
            if (string.IsNullOrWhiteSpace(PseudoImageFolder))
            {
                error = "未选择伪采集目录";
                return false;
            }

            if (!Directory.Exists(PseudoImageFolder))
            {
                error = "伪采集目录不存在";
                return false;
            }

            if (PseudoImageCount <= 0)
            {
                error = "伪采集目录中未找到可用图片";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryGetNextPseudoImagePath(bool loopEnabled, int loopCount, out string imagePath)
        {
            imagePath = null;
            if (!TryEnsurePseudoImagesLoaded(out _))
                return false;

            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            if (!_pseudoReadPlanner.TryGetNext(
                    _pseudoImageFiles.Length,
                    loopEnabled,
                    loopCount,
                    out var imageIndex,
                    out _))
            {
                return false;
            }

            imagePath = _pseudoImageFiles[imageIndex];
            CurrentPseudoFileName = Path.GetFileName(imagePath);
            return true;
        }

        private void RefreshPseudoImages()
        {
            _pseudoImageFiles = PseudoImageFileOrder.LoadSupportedImageFiles(PseudoImageFolder).ToArray();
            PseudoImageCount = _pseudoImageFiles.Length;
        }

        public void ResetPseudoOrder()
        {
            _pseudoReadPlanner ??= new PseudoImageReadPlanner();
            _pseudoReadPlanner.Reset();
            CurrentPseudoFileName = string.Empty;
        }
    }
}

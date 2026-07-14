using Custom.DefectOverview.Models.GroupedDualCamera;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace Custom.DefectOverview.Models
{
    public enum DefectOverviewResultMode
    {
        FilteredResults,
        RawResultsNeedPostProcess
    }

    [Serializable]
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
        private const string LegacyDisplayTargetBitmapKey = "XYHD_DisplayTargetBitmap";
        private const string LegacyDisplayTargetSourceWidthKey = "XYHD_DisplayTargetSourceWidth";
        private const string LegacyDisplayTargetSourceHeightKey = "XYHD_DisplayTargetSourceHeight";
        private const string LegacyDisplayPixelWidthKey = "XYHD_DisplayPixelWidth";
        private const string LegacyDisplayPixelHeightKey = "XYHD_DisplayPixelHeight";

        [JsonIgnore]
        private TransmitParam _inputImage = new();
        [InputParam("Image", "输入图像", needDeepCopy: false)]
        public TransmitParam InputImage
        {
            get => _inputImage;
            set => SetProperty(ref _inputImage, value);
        }

        [JsonIgnore]
        private TransmitParam _inputResults = new();
        [InputParam("Results", "输入缺陷结果", needDeepCopy: false)]
        public TransmitParam InputResults
        {
            get => _inputResults;
            set => SetProperty(ref _inputResults, value);
        }

        [JsonIgnore]
        private TransmitParam _leftInputImage = new();
        [InputParam("LeftImage", "左路图像", needDeepCopy: false)]
        public TransmitParam LeftInputImage
        {
            get => _leftInputImage;
            set => SetProperty(ref _leftInputImage, value);
        }

        [JsonIgnore]
        private TransmitParam _leftInputResults = new();
        [InputParam("LeftResults", "左路缺陷结果", needDeepCopy: false)]
        public TransmitParam LeftInputResults
        {
            get => _leftInputResults;
            set => SetProperty(ref _leftInputResults, value);
        }

        [JsonIgnore]
        private TransmitParam _rightInputImage = new();
        [InputParam("RightImage", "右路图像", needDeepCopy: false)]
        public TransmitParam RightInputImage
        {
            get => _rightInputImage;
            set => SetProperty(ref _rightInputImage, value);
        }

        [JsonIgnore]
        private TransmitParam _rightInputResults = new();
        [InputParam("RightResults", "右路缺陷结果", needDeepCopy: false)]
        public TransmitParam RightInputResults
        {
            get => _rightInputResults;
            set => SetProperty(ref _rightInputResults, value);
        }

        [JsonIgnore]
        private TransmitParam _inputFrameKey = new();
        [InputParam("FrameKey", "帧分组键")]
        public TransmitParam InputFrameKey
        {
            get => _inputFrameKey;
            set => SetProperty(ref _inputFrameKey, value);
        }

        [JsonIgnore]
        private TransmitParam _inputFrameIdText = new();
        [InputParam("FrameIdText", "帧显示编号")]
        public TransmitParam InputFrameIdText
        {
            get => _inputFrameIdText;
            set => SetProperty(ref _inputFrameIdText, value);
        }

        [JsonIgnore]
        private TransmitParam _inputLaneWidth = new();
        [InputParam("LaneWidth", "幅宽")]
        public TransmitParam InputLaneWidth
        {
            get => _inputLaneWidth;
            set => SetProperty(ref _inputLaneWidth, value);
        }

        [JsonIgnore]
        private TransmitParam _inputPixelEquivalentX = new();
        [InputParam("PixelEquivalentX", "像素当量X")]
        public TransmitParam InputPixelEquivalentX
        {
            get => _inputPixelEquivalentX;
            set => SetProperty(ref _inputPixelEquivalentX, value);
        }

        [JsonIgnore]
        private TransmitParam _inputPixelEquivalentY = new();
        [InputParam("PixelEquivalentY", "像素当量Y")]
        public TransmitParam InputPixelEquivalentY
        {
            get => _inputPixelEquivalentY;
            set => SetProperty(ref _inputPixelEquivalentY, value);
        }

        [JsonIgnore]
        private TransmitParam _inputEdgeCalibrationX = new();
        [InputParam("EdgeCalibrationX", "边缘标定X")]
        public TransmitParam InputEdgeCalibrationX
        {
            get => _inputEdgeCalibrationX;
            set => SetProperty(ref _inputEdgeCalibrationX, value);
        }

        [JsonIgnore]
        private TransmitParam _inputSchemeFilePath = new();
        [InputParam("SchemeFilePath", "方案文件路径")]
        public TransmitParam InputSchemeFilePath
        {
            get => _inputSchemeFilePath;
            set => SetProperty(ref _inputSchemeFilePath, value);
        }

        public string SourceName { get; set; } = "DefectOverview";

        public string PathName { get; set; } = string.Empty;

        public DefectOverviewFrameLayout FrameLayout { get; set; } = DefectOverviewFrameLayout.SinglePath;

        public DefectOverviewPathRole PathRole { get; set; } = DefectOverviewPathRole.Unknown;

        public DefectOverviewResultMode ResultMode { get; set; } = DefectOverviewResultMode.FilteredResults;

        private bool _useGroupedDualCameraInputs;
        public bool UseGroupedDualCameraInputs
        {
            get => _useGroupedDualCameraInputs;
            set
            {
                if (!SetProperty(ref _useGroupedDualCameraInputs, value))
                    return;

                if (value && UseDualPathInputs)
                    UseDualPathInputs = false;

                FrameLayout = value || UseDualPathInputs
                    ? DefectOverviewFrameLayout.DualPath
                    : DefectOverviewFrameLayout.SinglePath;
                RaisePropertyChanged(nameof(FrameLayout));
            }
        }

        private ObservableCollection<GroupedDualCameraBinding> _groupedDualCameraBindings = new();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<GroupedDualCameraBinding> GroupedDualCameraBindings
        {
            get
            {
                _groupedDualCameraBindings ??= new ObservableCollection<GroupedDualCameraBinding>();
                return _groupedDualCameraBindings;
            }
            set => SetProperty(ref _groupedDualCameraBindings, value ?? new ObservableCollection<GroupedDualCameraBinding>());
        }

        private bool _useDualPathInputs;
        public bool UseDualPathInputs
        {
            get => _useDualPathInputs;
            set
            {
                if (!SetProperty(ref _useDualPathInputs, value))
                    return;

                if (value && UseGroupedDualCameraInputs)
                    UseGroupedDualCameraInputs = false;

                FrameLayout = value || UseGroupedDualCameraInputs
                    ? DefectOverviewFrameLayout.DualPath
                    : DefectOverviewFrameLayout.SinglePath;
                RaisePropertyChanged(nameof(FrameLayout));
            }
        }

        public string LeftPathName { get; set; } = "左路";

        public string RightPathName { get; set; } = "右路";

        public double LaneWidth { get; set; } = 0;

        public double PixelEquivalentX { get; set; } = 1;

        public double PixelEquivalentY { get; set; } = 1;

        public double EdgeCalibrationX { get; set; } = 100;

        public string SchemeFilePath { get; set; } = string.Empty;

        private bool _saveLocalDefectImages;
        public bool SaveLocalDefectImages
        {
            get => _saveLocalDefectImages;
            set => SetProperty(ref _saveLocalDefectImages, value);
        }

        [JsonIgnore]
        private List<Result> _publishedResults = new();
        [OutputParam("Results", "发布到缺陷总览的结果")]
        public List<Result> PublishedResults
        {
            get => _publishedResults;
            set => SetProperty(ref _publishedResults, value);
        }

        [JsonIgnore]
        private int _publishedCount;
        [OutputParam("Count", "发布结果数量")]
        public int PublishedCount
        {
            get => _publishedCount;
            set => SetProperty(ref _publishedCount, value);
        }

        [JsonIgnore]
        private string _publishedFrameKey = string.Empty;
        [OutputParam("FrameKey", "实际发布帧键")]
        public string PublishedFrameKey
        {
            get => _publishedFrameKey;
            set => SetProperty(ref _publishedFrameKey, value);
        }

        [JsonIgnore]
        private string _publishStatusText = string.Empty;
        [OutputParam("StatusText", "发布状态")]
        public string PublishStatusText
        {
            get => _publishStatusText;
            set => SetProperty(ref _publishStatusText, value);
        }

        [JsonIgnore]
        private DateTime _lastPublishTime = DateTime.MinValue;
        [OutputParam("PublishTime", "最近发布时间")]
        public DateTime LastPublishTime
        {
            get => _lastPublishTime;
            set => SetProperty(ref _lastPublishTime, value);
        }

        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public new ExecuteModuleOutput Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        [JsonIgnore]
        private IDefectOverviewIngestService _ingestService;

        [JsonIgnore]
        private IDefectOverviewPostProcessService _postProcessService;

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                    return true;

                if (!base.OnceInit())
                    return false;

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                EnsureDefaultOutputParams(Guid, Name);

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] 初始化异常：{ex.Message}");
                return false;
            }
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            EnsureDefaultOutputParams(Guid, Name);

            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!IsDebug && !LoadKeyParam())
                    {
                        PublishStatusText = "加载输入参数失败。";
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] Error: {PublishStatusText}");
                        return NodeStatus.Error;
                    }

                    if (ShouldUseGroupedDualCameraInputs())
                        return PublishGroupedDualCameraInputs();

                    if (ShouldUseDualPathInputs())
                        return PublishDualPathInputs();

                    object rawResults = ResolveSelectedInputValue(InputResults, false, out string resultsSource);
                    List<Result> inputResults = ExtractResults(rawResults);
                    (HImage Image, string Source) resolvedImage = ResolveSelectedImageInput(InputImage);
                    using HImage image = resolvedImage.Image;

                    string frameKey = ResolveString(ResolveSelectedInputValue(InputFrameKey), string.Empty);
                    string frameIdText = ResolveString(ResolveSelectedInputValue(InputFrameIdText), frameKey);
                    double laneWidth = ResolveDouble(ResolveSelectedInputValue(InputLaneWidth), LaneWidth);
                    double pixelEquivalentX = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentX), PixelEquivalentX);
                    double pixelEquivalentY = ResolveDouble(ResolveSelectedInputValue(InputPixelEquivalentY), PixelEquivalentY);
                    double edgeCalibrationX = ResolveDouble(ResolveSelectedInputValue(InputEdgeCalibrationX), EdgeCalibrationX);
                    string schemeFilePath = ResolveString(ResolveSelectedInputValue(InputSchemeFilePath), SchemeFilePath);

                    if (string.IsNullOrWhiteSpace(frameKey))
                        frameKey = BuildDefaultFrameKey();

                    if (string.IsNullOrWhiteSpace(frameIdText))
                        frameIdText = frameKey;

                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                            $"[DefectOverviewPublish] Bindings image={DescribeTransmitParam(InputImage)}, results={DescribeTransmitParam(InputResults)}, frameKey={DescribeTransmitParam(InputFrameKey)}, frameIdText={DescribeTransmitParam(InputFrameIdText)}");
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine(
                            $"[DefectOverviewPublish] InputSummary rawType={rawResults?.GetType().FullName ?? "null"}, resultsSource={resultsSource ?? "none"}, inputCount={inputResults.Count}, frameKey={frameKey}, frameIdText={frameIdText}, laneWidth={laneWidth:F2}, pixelEq=({pixelEquivalentX:F4},{pixelEquivalentY:F4}), edgeCalX={edgeCalibrationX:F2}");
                    }

                    List<Result> publishedResults = BuildPublishedResults(
                        image,
                        inputResults,
                        frameKey,
                        frameIdText,
                        laneWidth,
                        pixelEquivalentX,
                        pixelEquivalentY,
                        edgeCalibrationX,
                        schemeFilePath);
                    AttachPreviewMetadata(image, publishedResults);

                    ResolveIngestService().PublishPath(new DefectOverviewPathPacket
                    {
                        SourceName = string.IsNullOrWhiteSpace(SourceName) ? "DefectOverview" : SourceName,
                        FrameKey = frameKey,
                        FrameIdText = frameIdText,
                        CreatedUtc = DateTime.UtcNow,
                        FrameLayout = FrameLayout,
                        PathRole = PathRole,
                        PathName = ResolvePathName(),
                        PathImage = image,
                        OriginalImage = image,
                        ApplyPostProcess = false,
                        SaveLocalDefectImages = SaveLocalDefectImages,
                        IsNg = publishedResults.Count > 0,
                        Results = publishedResults,
                        LaneWidth = laneWidth > 0 ? laneWidth : null,
                        PixelEquivalentX = pixelEquivalentX > 0 ? pixelEquivalentX : null,
                        PixelEquivalentY = pixelEquivalentY > 0 ? pixelEquivalentY : null,
                        EdgeCalibrationX = edgeCalibrationX,
                        SchemeFilePath = schemeFilePath ?? string.Empty
                    });

                    PublishedResults = publishedResults;
                    PublishedCount = publishedResults.Count;
                    PublishedFrameKey = frameKey;
                    if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
                    {
                        string imageState = DescribeImageState(image);
                        string resultState = DescribeResultImageState(publishedResults);
                        PublishStatusText = $"Published {publishedResults.Count} results | Image={imageState} | ResultImage={resultState}";
                        Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] ImageSource={resolvedImage.Source}, FrameKey={frameKey}, Count={publishedResults.Count}, Image={imageState}, ResultImage={resultState}");
                    }
                    else
                    {
                        PublishStatusText = $"Published {publishedResults.Count} results";
                    }
                    LastPublishTime = DateTime.Now;
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    PublishStatusText = ex.Message;
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] Error: {ex}");
                    return NodeStatus.Error;
                }
            });

            RefreshBrjReportOutputValues();
            RefreshPublishedPacket();

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };

            if (!UpdatePublishedOutputParams())
            {
                Output.RunStatus = NodeStatus.Error;
            }

            return await Task.FromResult(Output);
        }

    }
}


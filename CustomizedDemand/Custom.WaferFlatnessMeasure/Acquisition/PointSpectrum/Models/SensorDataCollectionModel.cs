using NetTaste;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Custom.WaferFlatnessMeasure.Models
{
    [Serializable]
    public partial class SensorDataCollectionModel : ModelParamBase
    {
        public const string UpSurfaceResultOption = "上表面";
        public const string DownSurfaceResultOption = "下表面";

        private static readonly IReadOnlyList<string> _surfaceResultOptions = new[]
        {
            UpSurfaceResultOption,
            DownSurfaceResultOption
        };

        #region Fields
        public string SltModelName;

        /// <summary>
        /// 打点的数据
        /// </summary>
        [JsonIgnore]
        public bool IsPoint = false;

        [JsonIgnore]
        private List<PreprocessDatasetModel> _preDatas = new List<PreprocessDatasetModel>();

        [JsonIgnore]
        private List<PreprocessDatasetModel> _unfilteredPreDatas = new List<PreprocessDatasetModel>();

        [JsonIgnore]
        public List<double[]> ValidCollect = new List<double[]>();


        [JsonIgnore]
        public List<double[]> DownValidCollect = new List<double[]>();



        [JsonIgnore]
        public Flatness_Algorithm ALGO {  get; set; }

        [JsonIgnore]
        private readonly object _allDatasLock = new object();

        [JsonIgnore]
        private CancellationTokenSource? _allDatasPollingCts;

        [JsonIgnore]
        private Task? _allDatasPollingTask;

        [JsonIgnore]
        private bool _isSyncingLegacySurfaceDataSources;

        [JsonIgnore]
        private bool _isNormalizingDataAnalysisDataSources;

        [JsonIgnore]
        private bool _isNormalizingDataAnalysisAlgorithms;

        [JsonIgnore]
        private bool _hasAppliedLegacyFilterSettingsToDataSources;

        [JsonIgnore]
        private List<PointCollectionStepInfo> _pointCollectionSteps = new List<PointCollectionStepInfo>();

        [JsonIgnore]
        private readonly object _pointTemperatureRecordsLock = new object();

        [JsonIgnore]
        private string _pointTemperatureCaptureSessionId = string.Empty;

        [JsonIgnore]
        private List<PointTemperatureRecord> _pointTemperatureRecords = new List<PointTemperatureRecord>();

        [JsonIgnore]
        private ObservableCollection<PointTemperatureAddressModel>? _subscribedPointTemperatureAddresses;

        [JsonIgnore]
        private readonly object _lineSegmentCsvSessionLock = new object();

        [JsonIgnore]
        private string _lineSegmentCsvSessionDirectory = string.Empty;

        [JsonIgnore]
        private int _lineSegmentCsvIndex;

        [JsonIgnore]
        private int _lineSegmentCsvExpectedCount;

        [JsonIgnore]
        private readonly List<PreprocessDatasetModel> _lineSegmentSummaryPreDatas = new List<PreprocessDatasetModel>();

        [JsonIgnore]
        private readonly List<PreprocessDatasetModel> _lineSegmentSummaryFilteredPreDatas = new List<PreprocessDatasetModel>();
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<SensorBase> _models;
        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath;
        /// <summary>
        /// 原始数据CSV路径
        /// </summary>
        [OutputParam("FilePath", "原始数据CSV路径")]
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        [OutputParam("PreDatas", "最近一次预处理数据")]
        public List<PreprocessDatasetModel> PreDatas
        {
            get { return _preDatas; }
            set
            {
                _preDatas = value ?? new List<PreprocessDatasetModel>();
                PreDataCount = _preDatas.Count;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PreDataCount));
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        [OutputParam("PreDataCount", "最近一次预处理数据点数")]
        public int PreDataCount { set; get; }/* => PreDatas?.Count ?? 0;*/

        [JsonIgnore]
        private string _sltFile;
        /// <summary>
        /// 选中文件
        /// </summary>
        public string SltFile
        {
            get { return _sltFile; }
            set { _sltFile = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _linkPath;
        /// <summary>
        /// 链接路径
        /// </summary>
        public string LinkPath
        {
            get { return _linkPath; }
            set { _linkPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isLinkVisibility = Visibility.Hidden;
        /// <summary>
        /// 链接路径可见性
        /// </summary>
        public Visibility IsLinkVisibility
        {
            get { return _isLinkVisibility; }
            set { _isLinkVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前采集模式
        /// </summary>
        [JsonIgnore]
        private eTrigMode _AcquisitionMode = eTrigMode.软触发;
        public eTrigMode AcquisitionMode
        {
            get { return _AcquisitionMode; }
            set { _AcquisitionMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTriggerPicIndex = 1;
        /// <summary>
        /// 选择采集方式（0：原始数据CSV，1：传感器采集）
        /// </summary>
        public int SltTriggerPicIndex
        {
            get { return _sltTriggerPicIndex; }
            set { _sltTriggerPicIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SensorBase _sltModel;
        /// <summary>
        /// 选中的
        /// </summary>
        [JsonIgnore]
        public SensorBase SltModel
        {
            get { return _sltModel; }
            set { _sltModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _camSltRotate = "Null";
        /// <summary>
        /// 选择相机旋转
        /// </summary>
        public string CamSltRotate
        {
            get { return _camSltRotate; }
            set { _camSltRotate = value; }
        }

        [JsonIgnore]
        private string _sltTriggerMode = "Null";
        /// <summary>
        /// 采集模式
        /// </summary>
        public string SltTriggerMode
        {
            get { return _sltTriggerMode; }
            set { _sltTriggerMode = value; }
        }

        [JsonIgnore]
        private bool _startCollect;
        /// <summary>
        /// 开始采集；
        /// </summary>
        public bool StartCollect
        {
            get { return _startCollect; }
            set { _startCollect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _stopCollect;
        /// <summary>
        /// 停止采集；
        /// </summary>
        public bool StopCollect
        {
            get { return _stopCollect; }
            set { _stopCollect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _stopAndDispose;
        /// <summary>
        /// 停止采集并处理数据；
        /// </summary>
        public bool StopAndDispose
        {
            get { return _stopAndDispose; }
            set { _stopAndDispose = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _startEventName = "TrrigerStartCollect";
        /// <summary>
        /// 开始采集事件名称
        /// </summary>
        public string StartEventName
        {
            get { return _startEventName; }
            set { _startEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _stopEventName = "TrrigerStopCollect";
        /// <summary>
        /// 停止采集事件名称
        /// </summary>
        public string StopEventName
        {
            get { return _stopEventName; }
            set { _stopEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _stopAndDisposeEventName = "TrrigerDispose";
        /// <summary>
        /// 停止采集并处理数据事件名称
        /// </summary>
        public string StopAndDisposeEventName
        {
            get { return _stopAndDisposeEventName; }
            set { _stopAndDisposeEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isCollectionProgressVisible;
        [JsonIgnore]
        public bool IsCollectionProgressVisible
        {
            get { return _isCollectionProgressVisible; }
            private set { SetProperty(ref _isCollectionProgressVisible, value); }
        }

        [JsonIgnore]
        private int _expectedAllDatasCount;
        [JsonIgnore]
        public int ExpectedAllDatasCount
        {
            get { return _expectedAllDatasCount; }
            private set
            {
                if (SetProperty(ref _expectedAllDatasCount, value))
                {
                    RaiseCollectionProgressChanged();
                }
            }
        }

        [JsonIgnore]
        private int _currentAllDatasCount;
        [JsonIgnore]
        public int CurrentAllDatasCount
        {
            get { return _currentAllDatasCount; }
            private set
            {
                if (SetProperty(ref _currentAllDatasCount, value))
                {
                    RaiseCollectionProgressChanged();
                }
            }
        }

        [JsonIgnore]
        private string _collectionProgressStatus = "等待采集开始...";
        [JsonIgnore]
        public string CollectionProgressStatus
        {
            get { return _collectionProgressStatus; }
            private set { SetProperty(ref _collectionProgressStatus, value); }
        }

        [JsonIgnore]
        public double CollectionProgressMaximum => ExpectedAllDatasCount > 0 ? ExpectedAllDatasCount : 1d;

        [JsonIgnore]
        public double CollectionProgressValue => Math.Min(CurrentAllDatasCount, CollectionProgressMaximum);

        [JsonIgnore]
        public double CollectionProgressPercent =>
            ExpectedAllDatasCount > 0
                ? Math.Min(100d, CurrentAllDatasCount * 100d / ExpectedAllDatasCount)
                : 0d;

        [JsonIgnore]
        public string CollectionProgressSummary =>
            ExpectedAllDatasCount > 0
                ? $"{CurrentAllDatasCount}/{ExpectedAllDatasCount} ({CollectionProgressPercent:F1}%)"
                : "0/0";

        [JsonIgnore]
        public IReadOnlyList<string> OriginalDataValueNames => PreprocessDatasetModel.OriginalDataValueNames;

        private string _upSurfaceOriginalDataValueName = PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;
        /// <summary>
        /// UpSurface 对应的 MeasureData.OriginalDatas 数据键，格式为 Channel.Type
        /// </summary>
        public string UpSurfaceOriginalDataValueName
        {
            get { return _upSurfaceOriginalDataValueName; }
            set
            {
                _upSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                    value,
                    PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SurfaceChannelSummary));
                SyncDataAnalysisSourcesFromLegacySurfaceFields();
            }
        }

        private string _downSurfaceOriginalDataValueName = PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;
        /// <summary>
        /// DownSurface 对应的 MeasureData.OriginalDatas 数据键，格式为 Channel.Type
        /// </summary>
        public string DownSurfaceOriginalDataValueName
        {
            get { return _downSurfaceOriginalDataValueName; }
            set
            {
                _downSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                    value,
                    PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SurfaceChannelSummary));
                SyncDataAnalysisSourcesFromLegacySurfaceFields();
            }
        }

        [JsonIgnore]
        public string SurfaceChannelSummary =>
            string.Join(
                ", ",
                GetEnabledDataAnalysisSources()
                    .Select(source =>
                        $"{source.Name}={source.OriginalDataValueName}{(source.IsFilterEnabled ? (source.IsRawDataFilter ? "[原始过滤]" : "[平均后过滤]") : string.Empty)}")) + " (OriginalDatas)";

        private ObservableCollection<DataAnalysisDataSourceOption> _dataAnalysisDataSources = CreateDefaultDataAnalysisDataSources();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<DataAnalysisDataSourceOption> DataAnalysisDataSources
        {
            get { return _dataAnalysisDataSources; }
            set
            {
                ExecuteOnUiThreadSync(() => SetDataAnalysisDataSources(value));
            }
        }

        private void SetDataAnalysisDataSources(ObservableCollection<DataAnalysisDataSourceOption>? value)
        {
            DetachDataAnalysisDataSourceListeners(_dataAnalysisDataSources);
            _dataAnalysisDataSources = value ?? CreateDefaultDataAnalysisDataSources();
            EnsureDataAnalysisDataSourceIds();
            AttachDataAnalysisDataSourceListeners(_dataAnalysisDataSources);
            if (!_isNormalizingDataAnalysisDataSources && NormalizeDataAnalysisDataSources())
            {
                return;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SurfaceChannelSummary));
            SyncLegacySurfaceFieldsFromDataAnalysisSources();
            SyncDataAnalysisAlgorithmSourceSelections();
        }

        private ObservableCollection<DataAnalysisAlgorithmOption> _dataAnalysisAlgorithms = CreateDefaultDataAnalysisAlgorithms();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<DataAnalysisAlgorithmOption> DataAnalysisAlgorithms
        {
            get { return _dataAnalysisAlgorithms; }
            set
            {
                ExecuteOnUiThreadSync(() => SetDataAnalysisAlgorithms(value));
            }
        }

        private void SetDataAnalysisAlgorithms(ObservableCollection<DataAnalysisAlgorithmOption>? value)
        {
            _dataAnalysisAlgorithms = value ?? CreateDefaultDataAnalysisAlgorithms();
            if (!_isNormalizingDataAnalysisAlgorithms && NormalizeDataAnalysisAlgorithms())
            {
                return;
            }

            RaisePropertyChanged();
            SyncDataAnalysisAlgorithmSourceSelections();
        }

        [JsonIgnore]
        private ObservableCollection<DataAnalysisResult> _dataAnalysisResults = new ObservableCollection<DataAnalysisResult>();
        [JsonIgnore]
        [OutputParam("DataAnalysisResults", "最近一次数据分析结果")]
        public ObservableCollection<DataAnalysisResult> DataAnalysisResults
        {
            get { return _dataAnalysisResults; }
            set
            {
                _dataAnalysisResults = value ?? new ObservableCollection<DataAnalysisResult>();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DataAnalysisResultCount));
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        [OutputParam("DataAnalysisResultCount", "最近一次数据分析结果数量")]
        public int DataAnalysisResultCount => DataAnalysisResults?.Count ?? 0;

        private bool _isUsingChannel4RangeFilter;
        /// <summary>
        /// 是否启用数据源范围过滤（兼容旧配置开关）
        /// </summary>
        public bool IsUsingChannel4RangeFilter
        {
            get { return _isUsingChannel4RangeFilter; }
            set { _isUsingChannel4RangeFilter = value; RaisePropertyChanged(); }
        }

        private double _channel4FilterMin = -99999999d;
        /// <summary>
        /// UpSurface 允许下限
        /// </summary>
        public double Channel4FilterMin
        {
            get { return _channel4FilterMin; }
            set { _channel4FilterMin = value; RaisePropertyChanged(); }
        }

        private double _channel4FilterMax = 99999999d;
        /// <summary>
        /// UpSurface 允许上限
        /// </summary>
        public double Channel4FilterMax
        {
            get { return _channel4FilterMax; }
            set { _channel4FilterMax = value; RaisePropertyChanged(); }
        }

        private double _downSurfaceFilterMin = -99999999d;
        /// <summary>
        /// DownSurface 允许下限
        /// </summary>
        public double DownSurfaceFilterMin
        {
            get { return _downSurfaceFilterMin; }
            set { _downSurfaceFilterMin = value; RaisePropertyChanged(); }
        }

        private double _downSurfaceFilterMax = 99999999d;
        /// <summary>
        /// DownSurface 允许上限
        /// </summary>
        public double DownSurfaceFilterMax
        {
            get { return _downSurfaceFilterMax; }
            set { _downSurfaceFilterMax = value; RaisePropertyChanged(); }
        }

        private bool _isMedianFilterEnabled;
        public bool IsMedianFilterEnabled
        {
            get { return _isMedianFilterEnabled; }
            set { _isMedianFilterEnabled = value; RaisePropertyChanged(); }
        }

        private int _medianFilterWindowSize = 3;
        public int MedianFilterWindowSize
        {
            get { return _medianFilterWindowSize; }
            set { _medianFilterWindowSize = Math.Max(1, value); RaisePropertyChanged(); }
        }

        private bool _isSmoothFilterEnabled;
        public bool IsSmoothFilterEnabled
        {
            get { return _isSmoothFilterEnabled; }
            set { _isSmoothFilterEnabled = value; RaisePropertyChanged(); }
        }

        private int _smoothFilterWindowSize = 3;
        public int SmoothFilterWindowSize
        {
            get { return _smoothFilterWindowSize; }
            set { _smoothFilterWindowSize = Math.Max(1, value); RaisePropertyChanged(); }
        }

        private bool _isFilterOriginalDataValues = true;
        public bool IsFilterOriginalDataValues
        {
            get { return _isFilterOriginalDataValues; }
            set { _isFilterOriginalDataValues = value; RaisePropertyChanged(); }
        }

        private bool _isSavePreDatasToCsv = true;
        /// <summary>
        /// 是否导出预处理数据 CSV
        /// </summary>
        public bool IsSavePreDatasToCsv
        {
            get { return _isSavePreDatasToCsv; }
            set { _isSavePreDatasToCsv = value; RaisePropertyChanged(); }
        }

        private string _preDatasCsvDirectory = string.Empty;
        /// <summary>
        /// 预处理数据 CSV 存储目录
        /// </summary>
        public string PreDatasCsvDirectory
        {
            get { return _preDatasCsvDirectory; }
            set { _preDatasCsvDirectory = value?.Trim() ?? string.Empty; RaisePropertyChanged(); }
        }

        private string _pointCloudOutputDirectory = string.Empty;
        /// <summary>
        /// RunALGO 点云导出目录
        /// </summary>
        public string PointCloudOutputDirectory
        {
            get { return _pointCloudOutputDirectory; }
            set { _pointCloudOutputDirectory = value; RaisePropertyChanged(); }
        }

        private bool _isSaveRunAlgoPointCloudImages = true;
        /// <summary>
        /// 是否将 RunALGO 点云导出为 GrayAndHeightChart 可加载的高度图
        /// </summary>
        public bool IsSaveRunAlgoPointCloudImages
        {
            get { return _isSaveRunAlgoPointCloudImages; }
            set { _isSaveRunAlgoPointCloudImages = value; RaisePropertyChanged(); }
        }

        private int _runAlgoPointCloudImageResolution = 600;
        /// <summary>
        /// RunALGO 点云高度图长边分辨率
        /// </summary>
        public int RunAlgoPointCloudImageResolution
        {
            get { return _runAlgoPointCloudImageResolution; }
            set
            {
                _runAlgoPointCloudImageResolution = Math.Clamp(value, 16, 5480);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _lastPreDatasCsvPath = string.Empty;
        [JsonIgnore]
        [OutputParam("LastPreDatasCsvPath", "最近一次预处理数据CSV路径")]
        public string LastPreDatasCsvPath
        {
            get { return _lastPreDatasCsvPath; }
            set { _lastPreDatasCsvPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _lastFilteredPreDatasCsvPath = string.Empty;
        [JsonIgnore]
        [OutputParam("LastFilteredPreDatasCsvPath", "最近一次滤波预处理数据CSV路径")]
        public string LastFilteredPreDatasCsvPath
        {
            get { return _lastFilteredPreDatasCsvPath; }
            set { _lastFilteredPreDatasCsvPath = value ?? string.Empty; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _lastPointTemperatureCsvPath = string.Empty;
        [JsonIgnore]
        [OutputParam("LastPointTemperatureCsvPath", "最近一次打点温度CSV路径")]
        public string LastPointTemperatureCsvPath
        {
            get { return _lastPointTemperatureCsvPath; }
            set { _lastPointTemperatureCsvPath = value ?? string.Empty; RaisePropertyChanged(); }
        }

        private ObservableCollection<PointTemperatureAddressModel> _pointTemperatureAddresses =
            new ObservableCollection<PointTemperatureAddressModel>(
                PointTemperatureStorageService.CreateDefaultTemperatureAddresses());
        /// <summary>
        /// 打点到位时读取的 PLC 温度地址。
        /// </summary>
        public ObservableCollection<PointTemperatureAddressModel> PointTemperatureAddresses
        {
            get
            {
                EnsurePointTemperatureAddresses();
                return _pointTemperatureAddresses;
            }
            set
            {
                SetPointTemperatureAddressCollection(value);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _lastCalibrationWaferDataCsvPath = string.Empty;
        [JsonIgnore]
        [OutputParam("LastCalibrationWaferDataCsvPath", "最近一次标准片数据CSV路径")]
        public string LastCalibrationWaferDataCsvPath
        {
            get { return _lastCalibrationWaferDataCsvPath; }
            set { _lastCalibrationWaferDataCsvPath = value ?? string.Empty; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _lastPointCloudImageDirectory = string.Empty;
        [JsonIgnore]
        [OutputParam("LastPointCloudImageDirectory", "最近一次点云高度图目录")]
        public string LastPointCloudImageDirectory
        {
            get { return _lastPointCloudImageDirectory; }
            set { _lastPointCloudImageDirectory = value ?? string.Empty; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _lastParallelismValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastParallelismValue", "最近一次平行度")]
        public double LastParallelismValue
        {
            get { return _lastParallelismValue; }
            set
            {
                _lastParallelismValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastUpSurfaceFlatnessValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastUpSurfaceFlatnessValue", "最近一次上表面平面度")]
        public double LastUpSurfaceFlatnessValue
        {
            get { return _lastUpSurfaceFlatnessValue; }
            set
            {
                _lastUpSurfaceFlatnessValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastDownSurfaceFlatnessValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastDownSurfaceFlatnessValue", "最近一次下表面平面度")]
        public double LastDownSurfaceFlatnessValue
        {
            get { return _lastDownSurfaceFlatnessValue; }
            set
            {
                _lastDownSurfaceFlatnessValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastTtvValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastTtvValue", "最近一次TTV")]
        public double LastTtvValue
        {
            get { return _lastTtvValue; }
            set
            {
                _lastTtvValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastThicknessMinValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastThicknessMinValue", "最近一次厚度最小值")]
        public double LastThicknessMinValue
        {
            get { return _lastThicknessMinValue; }
            set
            {
                _lastThicknessMinValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastThicknessMaxValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastThicknessMaxValue", "最近一次厚度最大值")]
        public double LastThicknessMaxValue
        {
            get { return _lastThicknessMaxValue; }
            set
            {
                _lastThicknessMaxValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastThicknessValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastThicknessValue", "最近一次厚度(THK)")]
        public double LastThicknessValue
        {
            get { return _lastThicknessValue; }
            set
            {
                _lastThicknessValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastTirValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastTirValue", "最近一次TIR")]
        public double LastTirValue
        {
            get { return _lastTirValue; }
            set
            {
                _lastTirValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastWarp1Value = double.NaN;
        [JsonIgnore]
        [OutputParam("LastWarp1Value", "最近一次Warp1")]
        public double LastWarp1Value
        {
            get { return _lastWarp1Value; }
            set
            {
                _lastWarp1Value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private double _lastWarp2Value = double.NaN;
        [JsonIgnore]
        [OutputParam("LastWarp2Value", "最近一次Warp2")]
        public double LastWarp2Value
        {
            get { return _lastWarp2Value; }
            set
            {
                _lastWarp2Value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastMeasurementSummary));
            }
        }

        [JsonIgnore]
        private string _resultPointCloudTitle = "暂无点云";
        [JsonIgnore]
        public string ResultPointCloudTitle
        {
            get { return _resultPointCloudTitle; }
            set
            {
                _resultPointCloudTitle = string.IsNullOrWhiteSpace(value) ? "暂无点云" : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private List<double[]> _resultPointCloud = new List<double[]>();
        [JsonIgnore]
        public List<double[]> ResultPointCloud
        {
            get { return _resultPointCloud; }
            set
            {
                _resultPointCloud = value ?? new List<double[]>();
                ResultPointCloudCount = _resultPointCloud.Count;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ResultPointCloudCount));
                RaisePropertyChanged(nameof(HasResultPointCloud));
            }
        }

        [JsonIgnore]
        public int ResultPointCloudCount { set; get; } =  0;

        [JsonIgnore]
        public bool HasResultPointCloud => ResultPointCloudCount > 0;

        private string _selectedSurfaceResultOption = UpSurfaceResultOption;
        [JsonIgnore]
        public IReadOnlyList<string> SurfaceResultOptions =>
            GetEnabledDataAnalysisSources()
                .Select(source => source.Name)
                .DefaultIfEmpty(UpSurfaceResultOption)
                .ToList();

        /// <summary>
        /// ResultDisposeView 中当前查看的单表面结果
        /// </summary>
        public string SelectedSurfaceResultOption
        {
            get { return _selectedSurfaceResultOption; }
            set
            {
                _selectedSurfaceResultOption = NormalizeSurfaceResultOption(value);
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private string _selectedSurfaceResultTitle = UpSurfaceResultOption;
        [JsonIgnore]
        public string SelectedSurfaceResultTitle
        {
            get { return _selectedSurfaceResultTitle; }
            set
            {
                _selectedSurfaceResultTitle = string.IsNullOrWhiteSpace(value)
                    ? UpSurfaceResultOption
                    : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _selectedSurfaceTtvTitle = $"{UpSurfaceResultOption}TTV";
        [JsonIgnore]
        public string SelectedSurfaceTtvTitle
        {
            get { return _selectedSurfaceTtvTitle; }
            set
            {
                _selectedSurfaceTtvTitle = string.IsNullOrWhiteSpace(value)
                    ? $"{UpSurfaceResultOption}TTV"
                    : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _selectedSurfaceMinTitle = $"{UpSurfaceResultOption}最小值";
        [JsonIgnore]
        public string SelectedSurfaceMinTitle
        {
            get { return _selectedSurfaceMinTitle; }
            set
            {
                _selectedSurfaceMinTitle = string.IsNullOrWhiteSpace(value)
                    ? $"{UpSurfaceResultOption}最小值"
                    : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _selectedSurfaceMaxTitle = $"{UpSurfaceResultOption}最大值";
        [JsonIgnore]
        public string SelectedSurfaceMaxTitle
        {
            get { return _selectedSurfaceMaxTitle; }
            set
            {
                _selectedSurfaceMaxTitle = string.IsNullOrWhiteSpace(value)
                    ? $"{UpSurfaceResultOption}最大值"
                    : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _selectedSurfaceTtvValue = double.NaN;
        [JsonIgnore]
        public double SelectedSurfaceTtvValue
        {
            get { return _selectedSurfaceTtvValue; }
            set
            {
                _selectedSurfaceTtvValue = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _selectedSurfaceMinValue = double.NaN;
        [JsonIgnore]
        public double SelectedSurfaceMinValue
        {
            get { return _selectedSurfaceMinValue; }
            set
            {
                _selectedSurfaceMinValue = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _selectedSurfaceMaxValue = double.NaN;
        [JsonIgnore]
        public double SelectedSurfaceMaxValue
        {
            get { return _selectedSurfaceMaxValue; }
            set
            {
                _selectedSurfaceMaxValue = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private bool IsDownSurfaceResultSelected =>
            string.Equals(_selectedSurfaceResultOption, DownSurfaceResultOption, StringComparison.Ordinal);

        [JsonIgnore]
        private double _lastUpSurfaceTtvValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastUpSurfaceTtvValue", "最近一次上表面TTV")]
        public double LastUpSurfaceTtvValue
        {
            get { return _lastUpSurfaceTtvValue; }
            set
            {
                _lastUpSurfaceTtvValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private double _lastUpSurfaceMinValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastUpSurfaceMinValue", "最近一次上表面最小值")]
        public double LastUpSurfaceMinValue
        {
            get { return _lastUpSurfaceMinValue; }
            set
            {
                _lastUpSurfaceMinValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private double _lastUpSurfaceMaxValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastUpSurfaceMaxValue", "最近一次上表面最大值")]
        public double LastUpSurfaceMaxValue
        {
            get { return _lastUpSurfaceMaxValue; }
            set
            {
                _lastUpSurfaceMaxValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private double _lastDownSurfaceTtvValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastDownSurfaceTtvValue", "最近一次下表面TTV")]
        public double LastDownSurfaceTtvValue
        {
            get { return _lastDownSurfaceTtvValue; }
            set
            {
                _lastDownSurfaceTtvValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private double _lastDownSurfaceMinValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastDownSurfaceMinValue", "最近一次下表面最小值")]
        public double LastDownSurfaceMinValue
        {
            get { return _lastDownSurfaceMinValue; }
            set
            {
                _lastDownSurfaceMinValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private double _lastDownSurfaceMaxValue = double.NaN;
        [JsonIgnore]
        [OutputParam("LastDownSurfaceMaxValue", "最近一次下表面最大值")]
        public double LastDownSurfaceMaxValue
        {
            get { return _lastDownSurfaceMaxValue; }
            set
            {
                _lastDownSurfaceMaxValue = value;
                RaisePropertyChanged();
                RaiseSelectedSurfaceResultChanged();
            }
        }

        [JsonIgnore]
        private List<double[]> _lastThicknessRawPointCloud = new List<double[]>();
        [JsonIgnore]
        public List<double[]> LastThicknessRawPointCloud
        {
            get { return _lastThicknessRawPointCloud; }
            set
            {
                _lastThicknessRawPointCloud = value ?? new List<double[]>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private List<double[]> _lastThicknessPointCloud = new List<double[]>();
        [JsonIgnore]
        public List<double[]> LastThicknessPointCloud
        {
            get { return _lastThicknessPointCloud; }
            set
            {
                _lastThicknessPointCloud = value ?? new List<double[]>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private List<double[]> _lastThicknessNormalPointCloud = new List<double[]>();
        [JsonIgnore]
        public List<double[]> LastThicknessNormalPointCloud
        {
            get { return _lastThicknessNormalPointCloud; }
            set
            {
                _lastThicknessNormalPointCloud = value ?? new List<double[]>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public string LastMeasurementSummary =>
            $"点数={PreDataCount}，结果数={DataAnalysisResultCount}，上表面平面度={FormatResultValue(LastUpSurfaceFlatnessValue)}，下表面平面度={FormatResultValue(LastDownSurfaceFlatnessValue)}，平行度={FormatResultValue(LastParallelismValue)}，TTV={FormatResultValue(LastTtvValue)}，THK={FormatResultValue(LastThicknessValue)}，TIR={FormatResultValue(LastTirValue)}，Warp1={FormatResultValue(LastWarp1Value)}，Warp2={FormatResultValue(LastWarp2Value)}，厚度范围=[{FormatResultValue(LastThicknessMinValue)}, {FormatResultValue(LastThicknessMaxValue)}]";

        [JsonIgnore]
        private Flatness_MeasureParam _measureParam = new Flatness_MeasureParam();

        public Flatness_MeasureParam MeasureParam
        {
            get { return _measureParam; }
            set
            {
                _measureParam = value ?? new Flatness_MeasureParam();
                ALGO?.SetMeasureParam(_measureParam);
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }
        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
            var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();

            Models = Param.Models;
            EnsureDataAnalysisConfiguration();
            EnsurePointTemperatureAddresses();
        }

        [OnDeserializing]
        private void OnSensorDataCollectionModelDeserializing(StreamingContext context)
        {
            DetachDataAnalysisDataSourceListeners(_dataAnalysisDataSources);
            _dataAnalysisDataSources = new ObservableCollection<DataAnalysisDataSourceOption>();
            _dataAnalysisAlgorithms = new ObservableCollection<DataAnalysisAlgorithmOption>();
            _isSyncingLegacySurfaceDataSources = false;
            _isNormalizingDataAnalysisDataSources = false;
            _isNormalizingDataAnalysisAlgorithms = false;
            _hasAppliedLegacyFilterSettingsToDataSources = false;
            _subscribedPointTemperatureAddresses = null;
        }

        [OnDeserialized]
        private void OnSensorDataCollectionModelDeserialized(StreamingContext context)
        {
            EnsureDataAnalysisConfiguration();
            EnsurePointTemperatureAddresses();
        }

        #endregion

        #region Override
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                base.LoadKeyParam();
                EnsureDataAnalysisConfiguration();
                if (SltTriggerPicIndex < 0 || SltTriggerPicIndex > 1)
                {
                    SltTriggerPicIndex = 1;
                }

                UpSurfaceOriginalDataValueName = UpSurfaceOriginalDataValueName;
                DownSurfaceOriginalDataValueName = DownSurfaceOriginalDataValueName;
                SelectedSurfaceResultOption = SelectedSurfaceResultOption;
                IsUsingChannelCalibration = IsUsingChannelCalibration;
                RunAlgoPointCloudImageResolution = RunAlgoPointCloudImageResolution;
                MeasureParam ??= new Flatness_MeasureParam();
                ALGO?.SetMeasureParam(MeasureParam);
                EnsureDataAnalysisConfiguration();
                EnsurePointTemperatureAddresses();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

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

                Task.Run(() =>
                {
                    EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);

                });

                _calibALGO = new FlatCalib_Algorithm(CalibParam);
                ALGO = new Flatness_Algorithm(MeasureParam);
                LoadCalibModel();
                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-结果展示",
                    ViewName = "ResultDisposeView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-轨迹预览",
                    ViewName = "WaferTrajectoryMonitorDemoView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-结果曲线",
                    ViewName = "WaferFlatnessResultChartView"
                });

                #region 拿到移动的坐标位置
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != "CollectPoints") return;
                    if (obj.Item2 is IEnumerable<(double, double)> collectPoints)
                    {
                        PreDatas = PreprocessDatasetModel.CreateFromCollectPositions(collectPoints);
                    }

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != PointCollectionStepInfo.EventName) return;
                    if (obj.Item2 is IEnumerable<PointCollectionStepInfo> collectionSteps)
                    {
                        _pointCollectionSteps = collectionSteps
                            .Where(step => step != null)
                            .Select(step => step.Clone())
                            .ToList();
                    }

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != PointTemperatureStorageService.PointTemperatureCaptureStartedEventName) return;
                    if (obj.Item2 is string sessionId)
                    {
                        BeginPointTemperatureCaptureSession(sessionId);
                    }

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != PointTemperatureStorageService.PointTemperatureRecordCapturedEventName) return;
                    if (obj.Item2 is PointTemperatureRecord record)
                    {
                        AddPointTemperatureRecord(record);
                    }

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != LineSegmentCsvSessionInfo.EventName) return;
                    if (obj.Item2 is LineSegmentCsvSessionInfo sessionInfo)
                    {
                        BeginLineSegmentCsvSession(sessionInfo.ExpectedSegmentCount);
                    }

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != "IsPoint") return;
                    IsPoint = (bool)obj.Item2;
                    if (IsPoint)
                    {
                        ClearLineSegmentCsvSession();
                    }
                    else
                    {
                        ClearPointTemperatureRecords();
                    }

                }, ThreadOption.BackgroundThread);
                #endregion

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 释放相关资源
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                #region 检查条件

                #endregion

                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        // 原始数据CSV
                        case 0:
                            {
                                ExecuteCsvModule();
                            }

                            break;
                        // 传感器采集
                        case 1:
                        case 2:
                            {
                                if (StartCollect)
                                {
                                    TrrigerStartCollect(StartEventName);
                                }
                                else if (StopCollect)
                                {
                                    TrrigerStopCollect(StopEventName);
                                }
                                else if (StopAndDispose)
                                {
                                    TrrigerDispose(StopAndDisposeEventName);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError(ex);
                    return NodeStatus.Error;

                }

                #region 输出
                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                #endregion

                return NodeStatus.Success;
            });

            Logs.LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }
        #endregion

        #region ExternalMethods
        /// <summary>
        /// 触发采集
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始采集", ThreadOption.BackgroundThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != StartEventName) return;

            if (SltModel == null && !string.IsNullOrWhiteSpace(SltModelName))
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }

            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的传感器模块，无法开始采集。");
                return;
            }

            StopAllDatasPolling();
            ClearAllDatas();
            LastCalibrationWaferDataCsvPath = string.Empty;
            LastPointTemperatureCsvPath = string.Empty;
            SltModel.StopCollect();

            //开始执行有效移动前开启编码器触发
            //SltModel.SettingParam("Encoder1_Enable", true);


            Logs.LogInfo("传感器开始采集");
            SltModel.StartCollect();
            BeginCollectionProgress();
            StartAllDatasPolling();

        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集", ThreadOption.BackgroundThread)]
        public void TrrigerStopCollect(string order)
        {
            if (order != StopEventName) return;

            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的传感器模块，无法停止采集。");
                return;
            }

            try
            {
                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：触发传感器停止采集");
                UpdateCollectionProgressStatus("正在停止采集并整理数据...");
                StopAllDatasPolling();
                SltModel.StopCollect();
                ReceiveRemainingAllDatas();
                //移动结束后停止编码器触发
                //SltModel.SettingParam("Encoder1_Enable", false);
                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：开始获取数据...");

                List<MeasureData> allDatas = SnapshotAllDatas(clearAfterRead: true);
                UpdateCollectionProgressCount(allDatas.Count);
                UpdateCollectionProgressStatus("采集完成，正在处理数据...");

                (string lineSegmentCsvSessionDirectory, int lineSegmentCsvIndex) = IsPoint
                    ? (string.Empty, 0)
                    : BeginCurrentLineSegmentCsvCollect();

                List<MeasureData> DataCollect = new List<MeasureData>();
                if (allDatas.Count > 0)
                {
                    string rawCsvPath = string.Empty;
                    if (!IsPoint &&
                        lineSegmentCsvIndex > 0 &&
                        !string.IsNullOrWhiteSpace(lineSegmentCsvSessionDirectory))
                    {
                        rawCsvPath = SensorDataStorageService.GetLineSegmentRawCsvPath(
                            lineSegmentCsvSessionDirectory,
                            lineSegmentCsvIndex);
                    }
                    else if (IsPoint)
                    {
                        rawCsvPath = SensorDataStorageService.GetPointRawCsvPath(PreDatasCsvDirectory);
                    }

                    if (!string.IsNullOrWhiteSpace(rawCsvPath))
                    {
                        SensorDataStorageService.ExportOriginalDatasToCsv(allDatas, rawCsvPath);
                    }
                    else
                    {
                        Logs.LogWarning("PreDatas CSV directory is not configured; original raw CSV export skipped.");
                    }
                }

                if (IsPoint)
                {
                    DataCollect = ProcessCollectedData(allDatas, applyRawDataFilters: true);
                }
                else
                {
                    DataCollect = AlignCollectedDataWithPositions(allDatas);
                }

                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：结束获取数据...");

                UpdatePreDatas(BuildPreprocessDatas(DataCollect));
                if (IsPoint)
                {
                    SavePointTemperaturesCsvIfNeeded();
                    SavePreDatasToCsvIfNeeded();
                }
                else
                {
                    LastPointTemperatureCsvPath = string.Empty;
                    ClearPointTemperatureRecords();
                    SaveLineSegmentPreDatasToCsvIfNeeded(
                        lineSegmentCsvSessionDirectory,
                        lineSegmentCsvIndex);
                }

                RunALGO();
                ClearAllDatas();
            }
            finally
            {
                ResetCollectionProgress();
            }
        }

        /// <summary>
        /// 触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始处理数据出图", ThreadOption.BackgroundThread)]
        public void TrrigerDispose(string order)
        {
            if (order != StopAndDisposeEventName) return;

            Logs.LogInfo("传感器停止采集并处理数据");
            StopAllDatasPolling();
            SltModel?.StopCollect();
            ResetCollectionProgress();
            Logs.LogInfo("开始处理数据...");
        }

        /// <summary>
        /// 触发重置
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发重置采集数据", ThreadOption.BackgroundThread)]
        public void Reset(string order)
        {
            if (order != "Reset") return;

            Logs.LogInfo("开始停止采集并释放之前的资源");
            StopAllDatasPolling();
            SltModel?.StopCollect();
            PreDatas = new List<PreprocessDatasetModel>();
            ValidCollect.Clear();
            DownValidCollect.Clear();
            LastPreDatasCsvPath = string.Empty;
            LastCalibrationWaferDataCsvPath = string.Empty;
            LastPointTemperatureCsvPath = string.Empty;
            ClearPointTemperatureRecords();
            ClearLineSegmentCsvSession();
            ResetMeasurementResults();
            ClearChannelCalibrationRawPoints();
            RefreshOutputParamValues();
            ClearAllDatas();
            ResetCollectionProgress();

            Logs.LogInfo("完成...");
        }

        [JsonIgnore]
        public List<MeasureData> AllDatas = new List<MeasureData>();

        /// <summary>
        /// 出采集数据
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发获取数据", ThreadOption.BackgroundThread)]
        public void GetAllDatas(string order)
        {
            if (order != "GetAllDatas") return;

            if (SltModel == null && !string.IsNullOrWhiteSpace(SltModelName))
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }

            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的传感器模块，无法启动数据轮询。");
                return;
            }

            BeginCollectionProgress();
            StartAllDatasPolling();

        }

        #endregion

        #region Methods

        private void StartAllDatasPolling()
        {
            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的传感器模块，无法启动数据轮询。");
                return;
            }

            if (_allDatasPollingTask is { IsCompleted: false })
            {
                Logs.LogInfo("后台数据轮询线程已在运行，跳过重复启动。");
                return;
            }

            ClearAllDatas();
            _allDatasPollingCts?.Dispose();
            _allDatasPollingCts = new CancellationTokenSource();
            CancellationToken token = _allDatasPollingCts.Token;
            _allDatasPollingTask = Task.Run(() => CollectAllDatasLoop(token), token);
            Logs.LogInfo("后台数据轮询线程已启动。");
        }

        private void StopAllDatasPolling()
        {
            CancellationTokenSource? pollingCts = _allDatasPollingCts;
            Task? pollingTask = _allDatasPollingTask;

            _allDatasPollingCts = null;
            _allDatasPollingTask = null;

            pollingCts?.Cancel();
            if (pollingTask != null)
            {
                try
                {
                    pollingTask.Wait(500);
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(inner =>
                    inner is OperationCanceledException || inner is TaskCanceledException))
                {
                }
                catch (OperationCanceledException)
                {
                }
                //catch (TaskCanceledException)
                //{
                //}
            }

            pollingCts?.Dispose();
        }

        private async Task CollectAllDatasLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Logs.LogInfo("获取一下数据");
                    AppendAllDatas(SltModel?.ReceiveSensorData());
                    Logs.LogInfo("完成...");
                    await Task.Delay(100, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
            finally
            {
                Logs.LogInfo("后台数据轮询线程已停止。");
            }
        }

        private void ReceiveRemainingAllDatas()
        {
            if (SltModel == null)
            {
                return;
            }

            try
            {
                AppendAllDatas(SltModel.ReceiveSensorData());
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
        }

        private void AppendAllDatas(IEnumerable<MeasureData>? datas)
        {
            if (datas == null)
            {
                return;
            }

            List<MeasureData> dataList = datas.ToList();
            if (dataList.Count == 0)
            {
                return;
            }

            int currentCount;
            lock (_allDatasLock)
            {
                AllDatas.AddRange(dataList);
                currentCount = AllDatas.Count;
            }

            UpdateCollectionProgressCount(currentCount);
        }

        private List<MeasureData> SnapshotAllDatas(bool clearAfterRead = false)
        {
            lock (_allDatasLock)
            {
                List<MeasureData> snapshot = new List<MeasureData>(AllDatas);
                if (clearAfterRead)
                {
                    AllDatas.Clear();
                }

                return snapshot;
            }
        }

        private void ClearAllDatas()
        {
            lock (_allDatasLock)
            {
                AllDatas.Clear();
            }
        }

        public void ConfigureCollectionProgress(int expectedCount)
        {
            ExecuteOnUiThread(() =>
            {
                ExpectedAllDatasCount = Math.Max(expectedCount, 0);
                CurrentAllDatasCount = 0;
                IsCollectionProgressVisible = false;
                CollectionProgressStatus = ExpectedAllDatasCount > 0
                    ? "等待轮询采集开始..."
                    : "等待采集开始...";
            });
        }

        public void ResetCollectionProgress()
        {
            ExecuteOnUiThread(() =>
            {
                IsCollectionProgressVisible = false;
                CurrentAllDatasCount = 0;
                ExpectedAllDatasCount = 0;
                CollectionProgressStatus = "等待采集开始...";
            });
        }

        private void BeginCollectionProgress()
        {
            ExecuteOnUiThread(() =>
            {
                if (ExpectedAllDatasCount <= 0)
                {
                    return;
                }

                CurrentAllDatasCount = 0;
                CollectionProgressStatus = "正在轮询采集数据...";
                IsCollectionProgressVisible = true;
            });
        }

        private void UpdateCollectionProgressCount(int currentCount)
        {
            ExecuteOnUiThread(() =>
            {
                CurrentAllDatasCount = Math.Max(currentCount, 0);
                if (ExpectedAllDatasCount > 0 && CurrentAllDatasCount >= ExpectedAllDatasCount)
                {
                    CollectionProgressStatus = "已达到预估采集量，等待停止采集...";
                }
            });
        }

        private void UpdateCollectionProgressStatus(string status)
        {
            ExecuteOnUiThread(() =>
            {
                if (ExpectedAllDatasCount > 0)
                {
                    IsCollectionProgressVisible = true;
                }

                CollectionProgressStatus = string.IsNullOrWhiteSpace(status)
                    ? "正在处理..."
                    : status;
            });
        }

        private void RaiseCollectionProgressChanged()
        {
            RaisePropertyChanged(nameof(CollectionProgressMaximum));
            RaisePropertyChanged(nameof(CollectionProgressValue));
            RaisePropertyChanged(nameof(CollectionProgressPercent));
            RaisePropertyChanged(nameof(CollectionProgressSummary));
        }

        private bool IsOnUiThread()
        {
            return PrismProvider.Dispatcher == null || PrismProvider.Dispatcher.CheckAccess();
        }

        private void ExecuteOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (IsOnUiThread())
            {
                action();
                return;
            }

            PrismProvider.Dispatcher.BeginInvoke(action);
        }

        private void ExecuteOnUiThreadSync(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (IsOnUiThread())
            {
                action();
                return;
            }

            PrismProvider.Dispatcher.Invoke(action);
        }

        private void ExecuteCsvModule()
        {
            string csvFilePath = ResolveCsvFilePath();
            List<PreprocessDatasetModel> csvPreDatas = PreprocessDatasetModel.LoadFromCsv(csvFilePath);
            if (csvPreDatas.Count == 0)
            {
                throw new InvalidDataException($"CSV文件中未解析到有效数据：{csvFilePath}");
            }

            Logs.LogInfo($"开始从CSV读取预处理数据：{csvFilePath}");
            UpdatePreDatas(csvPreDatas);
            LastPreDatasCsvPath = csvFilePath;
            RefreshOutputParamValues();

            bool originalIsPoint = IsPoint;
            try
            {
                IsPoint = true;
                RunALGO();
            }
            finally
            {
                IsPoint = originalIsPoint;
            }
        }

        private string ResolveCsvFilePath()
        {
            string csvFilePath = !string.IsNullOrWhiteSpace(FilePath) ? FilePath : SltFile;
            if (string.IsNullOrWhiteSpace(csvFilePath) || !File.Exists(csvFilePath))
            {
                MessageBox.Show("原始数据CSV文件不存在，请重新选择文件！");
                throw new FileNotFoundException($"CSV文件不存在：{csvFilePath}");
            }

            return csvFilePath;
        }

        private List<PreprocessDatasetModel> BuildPreprocessDatas(IEnumerable<MeasureData>? dataCollect)
        {
            _unfilteredPreDatas = SensorPointDataProcessor.BuildPreprocessDatas(
                dataCollect,
                UpSurfaceOriginalDataValueName,
                DownSurfaceOriginalDataValueName);

            return IsPreprocessFilterEnabled()
                ? PreprocessDatasetFilter.Apply(_unfilteredPreDatas, CreateMeasurementDataFilterOptions())
                : PreprocessDatasetModel.Clone(_unfilteredPreDatas);
        }

        private bool IsPreprocessFilterEnabled()
        {
            return IsMedianFilterEnabled || IsSmoothFilterEnabled;
        }

        private MeasurementDataFilterOptions CreateMeasurementDataFilterOptions()
        {
            return new MeasurementDataFilterOptions
            {
                MedianFilterEnabled = IsMedianFilterEnabled,
                MedianFilterWindowSize = MedianFilterWindowSize,
                SmoothFilterEnabled = IsSmoothFilterEnabled,
                SmoothFilterWindowSize = SmoothFilterWindowSize,
                FilterOriginalDataValues = IsFilterOriginalDataValues
            };
        }

        private void UpdatePreDatas(IEnumerable<PreprocessDatasetModel>? dataCollect)
        {
            PreDatas = dataCollect?.Where(data => data != null).ToList() ?? new List<PreprocessDatasetModel>();
            ValidCollect = PreprocessDatasetModel.ToUpSurfacePointCloud(PreDatas);
            DownValidCollect = PreprocessDatasetModel.ToDownSurfacePointCloud(PreDatas);
        }

        private List<PreprocessDatasetModel> FilterFinalSurfacePreDatas(IEnumerable<PreprocessDatasetModel>? dataCollect)
        {
            return SensorPointDataProcessor.FilterFinalSurfacePreDatas(
                dataCollect,
                GetActiveDataAnalysisSourceFilters(isRawDataFilter: false),
                GetDataAnalysisSourceValue);
        }

        private List<DataAnalysisSourceFilter> GetActiveDataAnalysisSourceFilters(bool isRawDataFilter)
        {
            List<DataAnalysisDataSourceOption> enabledSources = GetEnabledDataAnalysisSources();
            if (!IsUsingChannel4RangeFilter &&
                enabledSources.All(source => !source.IsFilterEnabled))
            {
                return new List<DataAnalysisSourceFilter>();
            }

            return enabledSources
                .Where(source => source.IsFilterEnabled && source.IsRawDataFilter == isRawDataFilter)
                .Select(source =>
                {
                    string sourceName = string.IsNullOrWhiteSpace(source.Name)
                        ? "DataSource"
                        : source.Name.Trim();
                    (double minValue, double maxValue) = NormalizeFilterRange(
                        source.FilterMin,
                        source.FilterMax,
                        sourceName);
                    return new DataAnalysisSourceFilter(
                        source,
                        sourceName,
                        minValue,
                        maxValue);
                })
                .ToList();
        }

        private void SavePreDatasToCsvIfNeeded()
        {
            List<PreprocessDatasetModel> primaryPreDatas = IsPreprocessFilterEnabled() && _unfilteredPreDatas.Count > 0
                ? _unfilteredPreDatas
                : PreDatas;
            LastPreDatasCsvPath = SensorDataStorageService.SavePreDatasToCsvIfNeeded(
                IsSavePreDatasToCsv,
                primaryPreDatas,
                PreDatasCsvDirectory,
                ChannelCalibrationC);
            LastFilteredPreDatasCsvPath = IsPreprocessFilterEnabled()
                ? SensorDataStorageService.SaveFilteredPreDatasToCsvIfNeeded(
                    IsSavePreDatasToCsv,
                    PreDatas,
                    PreDatasCsvDirectory,
                    ChannelCalibrationC)
                : string.Empty;
        }

        private void BeginPointTemperatureCaptureSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            PublishPointTemperatureAddressConfig();

            lock (_pointTemperatureRecordsLock)
            {
                _pointTemperatureCaptureSessionId = sessionId;
                _pointTemperatureRecords = _pointTemperatureRecords
                    .Where(record => string.Equals(
                        record.CaptureSessionId,
                        sessionId,
                        StringComparison.Ordinal))
                    .Select(PointTemperatureStorageService.CloneRecord)
                    .ToList();
            }
        }

        private void AddPointTemperatureRecord(PointTemperatureRecord record)
        {
            PointTemperatureRecord recordCopy = PointTemperatureStorageService.CloneRecord(record);
            if (string.IsNullOrWhiteSpace(recordCopy.CaptureSessionId))
            {
                return;
            }

            lock (_pointTemperatureRecordsLock)
            {
                if (string.IsNullOrWhiteSpace(_pointTemperatureCaptureSessionId) ||
                    !string.Equals(
                        _pointTemperatureCaptureSessionId,
                        recordCopy.CaptureSessionId,
                        StringComparison.Ordinal))
                {
                    _pointTemperatureCaptureSessionId = recordCopy.CaptureSessionId;
                    _pointTemperatureRecords.RemoveAll(existing =>
                        !string.Equals(
                            existing.CaptureSessionId,
                            recordCopy.CaptureSessionId,
                            StringComparison.Ordinal));
                }

                int existingIndex = _pointTemperatureRecords.FindIndex(existing =>
                    existing.PointIndex == recordCopy.PointIndex &&
                    string.Equals(
                        existing.CaptureSessionId,
                        recordCopy.CaptureSessionId,
                        StringComparison.Ordinal));
                if (existingIndex >= 0)
                {
                    _pointTemperatureRecords[existingIndex] = recordCopy;
                }
                else
                {
                    _pointTemperatureRecords.Add(recordCopy);
                }
            }
        }

        private List<PointTemperatureRecord> SnapshotPointTemperatureRecords()
        {
            lock (_pointTemperatureRecordsLock)
            {
                return _pointTemperatureRecords
                    .Where(record => string.IsNullOrWhiteSpace(_pointTemperatureCaptureSessionId) ||
                        string.Equals(
                            record.CaptureSessionId,
                            _pointTemperatureCaptureSessionId,
                            StringComparison.Ordinal))
                    .Select(PointTemperatureStorageService.CloneRecord)
                    .ToList();
            }
        }

        private void ClearPointTemperatureRecords()
        {
            lock (_pointTemperatureRecordsLock)
            {
                _pointTemperatureCaptureSessionId = string.Empty;
                _pointTemperatureRecords.Clear();
            }
        }

        public void ResetPointTemperatureAddresses()
        {
            PointTemperatureAddresses = new ObservableCollection<PointTemperatureAddressModel>(
                PointTemperatureStorageService.CreateDefaultTemperatureAddresses());
        }

        private void EnsurePointTemperatureAddresses()
        {
            if (_pointTemperatureAddresses == null || _pointTemperatureAddresses.Count == 0)
            {
                SetPointTemperatureAddressCollection(new ObservableCollection<PointTemperatureAddressModel>(
                    PointTemperatureStorageService.CreateDefaultTemperatureAddresses()));
                return;
            }

            AttachPointTemperatureAddressListeners(_pointTemperatureAddresses);
            PublishPointTemperatureAddressConfig();
        }

        private void SetPointTemperatureAddressCollection(ObservableCollection<PointTemperatureAddressModel>? addresses)
        {
            ObservableCollection<PointTemperatureAddressModel> next =
                new ObservableCollection<PointTemperatureAddressModel>(
                    PointTemperatureStorageService.CloneTemperatureAddresses(addresses));

            DetachPointTemperatureAddressListeners();
            _pointTemperatureAddresses = next;
            AttachPointTemperatureAddressListeners(_pointTemperatureAddresses);
            PublishPointTemperatureAddressConfig();
        }

        private void AttachPointTemperatureAddressListeners(ObservableCollection<PointTemperatureAddressModel> addresses)
        {
            if (ReferenceEquals(_subscribedPointTemperatureAddresses, addresses))
            {
                return;
            }

            DetachPointTemperatureAddressListeners();
            _subscribedPointTemperatureAddresses = addresses;
            _subscribedPointTemperatureAddresses.CollectionChanged += OnPointTemperatureAddressesChanged;
            foreach (PointTemperatureAddressModel address in _subscribedPointTemperatureAddresses)
            {
                address.PropertyChanged += OnPointTemperatureAddressChanged;
            }
        }

        private void DetachPointTemperatureAddressListeners()
        {
            if (_subscribedPointTemperatureAddresses == null)
            {
                return;
            }

            _subscribedPointTemperatureAddresses.CollectionChanged -= OnPointTemperatureAddressesChanged;
            foreach (PointTemperatureAddressModel address in _subscribedPointTemperatureAddresses)
            {
                address.PropertyChanged -= OnPointTemperatureAddressChanged;
            }

            _subscribedPointTemperatureAddresses = null;
        }

        private void OnPointTemperatureAddressesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PointTemperatureAddressModel address in e.OldItems)
                {
                    address.PropertyChanged -= OnPointTemperatureAddressChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PointTemperatureAddressModel address in e.NewItems)
                {
                    address.PropertyChanged += OnPointTemperatureAddressChanged;
                }
            }

            PublishPointTemperatureAddressConfig();
        }

        private void OnPointTemperatureAddressChanged(object? sender, PropertyChangedEventArgs e)
        {
            PublishPointTemperatureAddressConfig();
        }

        private void PublishPointTemperatureAddressConfig()
        {
            PointTemperatureStorageService.SetActiveTemperatureAddresses(_pointTemperatureAddresses);
            PrismProvider.EventAggregator?.GetEvent<OutputResultEvent>().Publish((
                PointTemperatureStorageService.PointTemperatureAddressConfigUpdatedEventName,
                PointTemperatureStorageService.CloneTemperatureAddresses(_pointTemperatureAddresses)));
        }

        private List<PreprocessDatasetModel> BuildPointTemperatureCsvPoints()
        {
            List<PointCollectionStepInfo> normalSteps = (_pointCollectionSteps ?? new List<PointCollectionStepInfo>())
                .Where(step => step != null && !step.IsCalibrationReference && step.NormalPointIndex >= 0)
                .OrderBy(step => step.NormalPointIndex)
                .ToList();
            if (normalSteps.Count > 0)
            {
                return normalSteps
                    .Select(step => new PreprocessDatasetModel
                    {
                        PosX = step.X,
                        PosY = step.Y
                    })
                    .ToList();
            }

            return PreprocessDatasetModel.Clone(PreDatas);
        }

        private void SavePointTemperaturesCsvIfNeeded()
        {
            LastPointTemperatureCsvPath = string.Empty;

            List<PreprocessDatasetModel> temperaturePoints = BuildPointTemperatureCsvPoints();
            if (temperaturePoints.Count == 0)
            {
                Logs.LogWarning("打点坐标为空，本次未导出温度 CSV。");
                return;
            }

            try
            {
                List<PointTemperatureRecord> capturedRecords = SnapshotPointTemperatureRecords();
                if (capturedRecords.Count < temperaturePoints.Count)
                {
                    Logs.LogWarning(
                        $"本次打点温度记录 {capturedRecords.Count} 条，少于坐标点 {temperaturePoints.Count} 个，缺失点位温度将留空。");
                }

                List<PointTemperatureRecord> records = PointTemperatureStorageService.MergeCapturedRecords(
                    temperaturePoints,
                    capturedRecords);
                LastPointTemperatureCsvPath = PointTemperatureStorageService.SavePointTemperaturesCsv(
                    records,
                    PreDatasCsvDirectory,
                    temperatureAddresses: PointTemperatureAddresses);

                if (!string.IsNullOrWhiteSpace(LastPointTemperatureCsvPath))
                {
                    Logs.LogInfo($"打点温度数据已导出到 CSV：{LastPointTemperatureCsvPath}");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                LastPointTemperatureCsvPath = string.Empty;
            }
        }

        private void BeginLineSegmentCsvSession(int expectedSegmentCount)
        {
            try
            {
                lock (_lineSegmentCsvSessionLock)
                {
                    _lineSegmentCsvSessionDirectory =
                        SensorDataStorageService.CreateLineSegmentCsvSessionDirectory(PreDatasCsvDirectory);
                    _lineSegmentCsvIndex = 0;
                    _lineSegmentCsvExpectedCount = Math.Max(1, expectedSegmentCount);
                    _lineSegmentSummaryPreDatas.Clear();
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                ClearLineSegmentCsvSession();
            }
        }

        private (string SessionDirectory, int SegmentIndex) BeginCurrentLineSegmentCsvCollect()
        {
            try
            {
                lock (_lineSegmentCsvSessionLock)
                {
                    if (string.IsNullOrWhiteSpace(_lineSegmentCsvSessionDirectory))
                    {
                        _lineSegmentCsvSessionDirectory =
                            SensorDataStorageService.CreateLineSegmentCsvSessionDirectory(PreDatasCsvDirectory);
                        if (string.IsNullOrWhiteSpace(_lineSegmentCsvSessionDirectory))
                        {
                            return (string.Empty, 0);
                        }

                        _lineSegmentCsvIndex = 0;
                        _lineSegmentCsvExpectedCount = 1;
                        _lineSegmentSummaryPreDatas.Clear();
                    }

                    _lineSegmentCsvIndex++;
                    return (_lineSegmentCsvSessionDirectory, _lineSegmentCsvIndex);
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                ClearLineSegmentCsvSession();
                return (string.Empty, 0);
            }
        }

        private void SaveLineSegmentPreDatasToCsvIfNeeded(
            string lineSegmentCsvSessionDirectory,
            int lineSegmentCsvIndex)
        {
            if (lineSegmentCsvIndex <= 0 ||
                string.IsNullOrWhiteSpace(lineSegmentCsvSessionDirectory))
            {
                SavePreDatasToCsvIfNeeded();
                return;
            }

            bool isSessionComplete;
            lock (_lineSegmentCsvSessionLock)
            {
                isSessionComplete =
                    _lineSegmentCsvExpectedCount > 0 &&
                    lineSegmentCsvIndex >= _lineSegmentCsvExpectedCount;
            }

            if (!IsSavePreDatasToCsv)
            {
                LastPreDatasCsvPath = string.Empty;
                LastFilteredPreDatasCsvPath = string.Empty;
                if (isSessionComplete)
                {
                    ClearLineSegmentCsvSession();
                }

                return;
            }

            List<PreprocessDatasetModel> primaryPreDatas = IsPreprocessFilterEnabled() && _unfilteredPreDatas.Count > 0
                ? _unfilteredPreDatas
                : PreDatas;
            LastPreDatasCsvPath = SensorDataStorageService.SaveLineSegmentPreDatasCsv(
                lineSegmentCsvSessionDirectory,
                lineSegmentCsvIndex,
                primaryPreDatas,
                ChannelCalibrationC);
            LastFilteredPreDatasCsvPath = IsPreprocessFilterEnabled()
                ? SensorDataStorageService.SaveFilteredLineSegmentPreDatasCsv(
                    lineSegmentCsvSessionDirectory,
                    lineSegmentCsvIndex,
                    PreDatas,
                    ChannelCalibrationC)
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(LastPreDatasCsvPath))
            {
                lock (_lineSegmentCsvSessionLock)
                {
                    _lineSegmentSummaryPreDatas.AddRange(PreprocessDatasetModel.Clone(primaryPreDatas));
                    if (IsPreprocessFilterEnabled())
                    {
                        _lineSegmentSummaryFilteredPreDatas.AddRange(PreprocessDatasetModel.Clone(PreDatas));
                    }
                }
            }

            if (!isSessionComplete)
            {
                return;
            }

            List<PreprocessDatasetModel> summaryPreDatas;
            lock (_lineSegmentCsvSessionLock)
            {
                summaryPreDatas = PreprocessDatasetModel.Clone(_lineSegmentSummaryPreDatas);
            }

            string summaryCsvPath = SensorDataStorageService.SaveLineSegmentSummaryCsv(
                lineSegmentCsvSessionDirectory,
                summaryPreDatas,
                ChannelCalibrationC);

            if (IsPreprocessFilterEnabled())
            {
                List<PreprocessDatasetModel> filteredSummaryPreDatas;
                lock (_lineSegmentCsvSessionLock)
                {
                    filteredSummaryPreDatas = PreprocessDatasetModel.Clone(_lineSegmentSummaryFilteredPreDatas);
                }

                LastFilteredPreDatasCsvPath = SensorDataStorageService.SaveFilteredLineSegmentSummaryCsv(
                    lineSegmentCsvSessionDirectory,
                    filteredSummaryPreDatas,
                    ChannelCalibrationC);
            }
            SensorDataStorageService.SaveLineSegmentSummaryPly(
                lineSegmentCsvSessionDirectory,
                summaryPreDatas,
                ChannelCalibrationC);

            if (!string.IsNullOrWhiteSpace(summaryCsvPath))
            {
                LastPreDatasCsvPath = summaryCsvPath;
            }

            ClearLineSegmentCsvSession();
        }

        private void ClearLineSegmentCsvSession()
        {
            lock (_lineSegmentCsvSessionLock)
            {
                _lineSegmentCsvSessionDirectory = string.Empty;
                _lineSegmentCsvIndex = 0;
            _lineSegmentCsvExpectedCount = 0;
            _lineSegmentSummaryPreDatas.Clear();
            _lineSegmentSummaryFilteredPreDatas.Clear();
            }
        }

        private static string SanitizePointCloudFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "PointCloud";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName
                .Select(character => invalidChars.Contains(character) ? '_' : character)
                .ToArray());
        }

        private void ResetMeasurementResults()
        {
            LastParallelismValue = double.NaN;
            LastUpSurfaceFlatnessValue = double.NaN;
            LastDownSurfaceFlatnessValue = double.NaN;
            LastTtvValue = double.NaN;
            LastThicknessMinValue = double.NaN;
            LastThicknessMaxValue = double.NaN;
            LastThicknessValue = double.NaN;
            LastTirValue = double.NaN;
            LastWarp1Value = double.NaN;
            LastWarp2Value = double.NaN;
            LastUpSurfaceTtvValue = double.NaN;
            LastUpSurfaceMinValue = double.NaN;
            LastUpSurfaceMaxValue = double.NaN;
            LastDownSurfaceTtvValue = double.NaN;
            LastDownSurfaceMinValue = double.NaN;
            LastDownSurfaceMaxValue = double.NaN;
            LastThicknessRawPointCloud = new List<double[]>();
            LastThicknessPointCloud = new List<double[]>();
            LastThicknessNormalPointCloud = new List<double[]>();
            ResultPointCloud = new List<double[]>();
            ResultPointCloudTitle = "暂无点云";
            LastPointCloudImageDirectory = string.Empty;
            ExecuteOnUiThreadSync(() =>
            {
                DataAnalysisResults = new ObservableCollection<DataAnalysisResult>();
            });
        }

        private static string NormalizeSurfaceResultOption(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return UpSurfaceResultOption;
            }

            return value.Trim();
        }

        private void RaiseSelectedSurfaceResultChanged()
        {
            string surfaceTitle = IsDownSurfaceResultSelected
                ? DownSurfaceResultOption
                : (!string.IsNullOrWhiteSpace(_selectedSurfaceResultOption)
                    ? _selectedSurfaceResultOption
                    : UpSurfaceResultOption);

            SelectedSurfaceResultTitle = surfaceTitle;
            SelectedSurfaceTtvTitle = $"{surfaceTitle}TTV";
            SelectedSurfaceMinTitle = $"{surfaceTitle}最小值";
            SelectedSurfaceMaxTitle = $"{surfaceTitle}最大值";

            DataAnalysisResult? statisticsResult = DataAnalysisResults?
                .FirstOrDefault(result =>
                    string.Equals(result.AlgorithmName, DataAnalysisAlgorithmOption.GetDisplayName(DataAnalysisAlgorithmKind.SurfaceStatistics), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(result.DataSourceName, surfaceTitle, StringComparison.OrdinalIgnoreCase));

            if (statisticsResult != null)
            {
                SelectedSurfaceTtvValue = statisticsResult.Value;
                SelectedSurfaceMinValue = statisticsResult.MinValue;
                SelectedSurfaceMaxValue = statisticsResult.MaxValue;
                return;
            }

            SelectedSurfaceTtvValue = IsDownSurfaceResultSelected
                ? LastDownSurfaceTtvValue
                : LastUpSurfaceTtvValue;
            SelectedSurfaceMinValue = IsDownSurfaceResultSelected
                ? LastDownSurfaceMinValue
                : LastUpSurfaceMinValue;
            SelectedSurfaceMaxValue = IsDownSurfaceResultSelected
                ? LastDownSurfaceMaxValue
                : LastUpSurfaceMaxValue;
        }

        private void RefreshOutputParamValues()
        {
            if (OutputParams == null || OutputParams.Count == 0)
            {
                return;
            }

            Dictionary<string, object> dataPointValues = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (dataPointValues.TryGetValue(item.ParamName, out object value))
                {
                    item.Value = value;
                }
            }
        }

        private void EnsureDataAnalysisConfiguration()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(EnsureDataAnalysisConfiguration);
                return;
            }

            if (DataAnalysisDataSources == null || DataAnalysisDataSources.Count == 0)
            {
                DataAnalysisDataSources = CreateDefaultDataAnalysisDataSources();
            }
            else
            {
                NormalizeDataAnalysisDataSources();
            }

            ApplyLegacyFilterSettingsToDataSources();

            if (DataAnalysisAlgorithms == null || DataAnalysisAlgorithms.Count == 0)
            {
                DataAnalysisAlgorithms = CreateDefaultDataAnalysisAlgorithms();
            }
            else
            {
                NormalizeDataAnalysisAlgorithms();
                foreach (DataAnalysisAlgorithmKind algorithm in Enum.GetValues<DataAnalysisAlgorithmKind>())
                {
                    if (DataAnalysisAlgorithms.All(item => item.Algorithm != algorithm))
                    {
                        bool enabled = algorithm is DataAnalysisAlgorithmKind.Flatness
                            or DataAnalysisAlgorithmKind.SurfaceStatistics
                            or DataAnalysisAlgorithmKind.Parallelism
                            or DataAnalysisAlgorithmKind.TTV;
                        DataAnalysisAlgorithms.Add(new DataAnalysisAlgorithmOption(algorithm, enabled));
                    }
                }
            }

            SyncDataAnalysisAlgorithmSourceSelections();
            SyncLegacySurfaceFieldsFromDataAnalysisSources();
        }

        private bool NormalizeDataAnalysisDataSources(bool allowCollectionReplacement = true)
        {
            if (!IsOnUiThread())
            {
                bool normalized = false;
                ExecuteOnUiThreadSync(() =>
                {
                    normalized = NormalizeDataAnalysisDataSources(allowCollectionReplacement);
                });
                return normalized;
            }

            if (_isNormalizingDataAnalysisDataSources || DataAnalysisDataSources == null)
            {
                return false;
            }

            _isNormalizingDataAnalysisDataSources = true;
            try
            {
                ObservableCollection<DataAnalysisDataSourceOption> currentSources = DataAnalysisDataSources;
                List<DataAnalysisDataSourceOption> normalizedSources = new List<DataAnalysisDataSourceOption>();
                Dictionary<string, int> sourceIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (DataAnalysisDataSourceOption source in currentSources.Where(source => source != null))
                {
                    source.Name = source.Name;
                    source.OriginalDataValueName = source.OriginalDataValueName;

                    string key = $"{source.Name.Trim()}|{source.OriginalDataValueName.Trim()}";
                    if (sourceIndexes.TryGetValue(key, out int existingIndex))
                    {
                        normalizedSources[existingIndex] = source;
                    }
                    else
                    {
                        sourceIndexes[key] = normalizedSources.Count;
                        normalizedSources.Add(source);
                    }
                }

                if (normalizedSources.Count == 0)
                {
                    if (allowCollectionReplacement)
                    {
                        DataAnalysisDataSources = CreateDefaultDataAnalysisDataSources();
                        return true;
                    }

                    return false;
                }

                if (normalizedSources.Count != currentSources.Count)
                {
                    if (allowCollectionReplacement)
                    {
                        DataAnalysisDataSources = new ObservableCollection<DataAnalysisDataSourceOption>(normalizedSources);
                        return true;
                    }

                    EnsureDataAnalysisDataSourceIds();
                    AttachDataAnalysisDataSourceListeners(DataAnalysisDataSources);
                    return false;
                }

                EnsureDataAnalysisDataSourceIds();
                AttachDataAnalysisDataSourceListeners(DataAnalysisDataSources);
                return false;
            }
            finally
            {
                _isNormalizingDataAnalysisDataSources = false;
            }
        }

        private bool NormalizeDataAnalysisAlgorithms()
        {
            if (!IsOnUiThread())
            {
                bool normalized = false;
                ExecuteOnUiThreadSync(() =>
                {
                    normalized = NormalizeDataAnalysisAlgorithms();
                });
                return normalized;
            }

            if (_isNormalizingDataAnalysisAlgorithms || DataAnalysisAlgorithms == null || DataAnalysisAlgorithms.Count == 0)
            {
                return false;
            }

            _isNormalizingDataAnalysisAlgorithms = true;
            try
            {
                ObservableCollection<DataAnalysisAlgorithmOption> currentAlgorithms = DataAnalysisAlgorithms;
                List<DataAnalysisAlgorithmOption> normalizedAlgorithms = new List<DataAnalysisAlgorithmOption>();
                Dictionary<DataAnalysisAlgorithmKind, int> algorithmIndexes = new Dictionary<DataAnalysisAlgorithmKind, int>();

                foreach (DataAnalysisAlgorithmOption algorithm in currentAlgorithms.Where(algorithm => algorithm != null))
                {
                    if (algorithmIndexes.TryGetValue(algorithm.Algorithm, out int existingIndex))
                    {
                        normalizedAlgorithms[existingIndex] = algorithm;
                    }
                    else
                    {
                        algorithmIndexes[algorithm.Algorithm] = normalizedAlgorithms.Count;
                        normalizedAlgorithms.Add(algorithm);
                    }
                }

                if (normalizedAlgorithms.Count == 0)
                {
                    DataAnalysisAlgorithms = CreateDefaultDataAnalysisAlgorithms();
                    return true;
                }

                if (normalizedAlgorithms.Count != currentAlgorithms.Count)
                {
                    DataAnalysisAlgorithms = new ObservableCollection<DataAnalysisAlgorithmOption>(normalizedAlgorithms);
                    return true;
                }

                return false;
            }
            finally
            {
                _isNormalizingDataAnalysisAlgorithms = false;
            }
        }

        private void EnsureDataAnalysisDataSourceIds()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(EnsureDataAnalysisDataSourceIds);
                return;
            }

            if (DataAnalysisDataSources == null)
            {
                return;
            }

            HashSet<Guid> usedIds = new HashSet<Guid>();
            foreach (DataAnalysisDataSourceOption source in DataAnalysisDataSources.Where(source => source != null))
            {
                if (source.DataSourceId == Guid.Empty || usedIds.Contains(source.DataSourceId))
                {
                    Guid newId;
                    do
                    {
                        newId = Guid.NewGuid();
                    }
                    while (usedIds.Contains(newId));

                    source.DataSourceId = newId;
                }

                usedIds.Add(source.DataSourceId);
            }
        }

        private void SyncDataAnalysisAlgorithmSourceSelections()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(SyncDataAnalysisAlgorithmSourceSelections);
                return;
            }

            if (DataAnalysisAlgorithms == null)
            {
                return;
            }

            List<DataAnalysisDataSourceOption> enabledSources = GetEnabledDataAnalysisSources();
            foreach (DataAnalysisAlgorithmOption algorithm in DataAnalysisAlgorithms.Where(algorithm => algorithm != null))
            {
                if (!DataAnalysisAlgorithmOption.RequiresPairSources(algorithm.Algorithm))
                {
                    continue;
                }

                DataAnalysisDataSourceOption? sourceA = FindDataSourceById(enabledSources, algorithm.SourceADataSourceId);
                DataAnalysisDataSourceOption? sourceB = FindDataSourceById(enabledSources, algorithm.SourceBDataSourceId);

                if (sourceA == null && enabledSources.Count > 0)
                {
                    sourceA = enabledSources[0];
                }

                if ((sourceB == null || ReferenceEquals(sourceA, sourceB)) && enabledSources.Count > 1)
                {
                    sourceB = enabledSources.FirstOrDefault(source => !ReferenceEquals(source, sourceA));
                }

                algorithm.SourceADataSourceId = sourceA?.DataSourceId;
                algorithm.SourceBDataSourceId = sourceB?.DataSourceId;
            }
        }

        private static DataAnalysisDataSourceOption? FindDataSourceById(
            IEnumerable<DataAnalysisDataSourceOption>? sources,
            Guid? dataSourceId)
        {
            if (dataSourceId == null || dataSourceId == Guid.Empty || sources == null)
            {
                return null;
            }

            return sources.FirstOrDefault(source => source != null && source.DataSourceId == dataSourceId.Value);
        }

        private void ApplyLegacyFilterSettingsToDataSources()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(ApplyLegacyFilterSettingsToDataSources);
                return;
            }

            if (_hasAppliedLegacyFilterSettingsToDataSources ||
                !IsUsingChannel4RangeFilter ||
                DataAnalysisDataSources == null ||
                DataAnalysisDataSources.Count == 0)
            {
                return;
            }

            if (DataAnalysisDataSources.All(source => source == null || !source.IsFilterEnabled))
            {
                if (DataAnalysisDataSources.Count > 0 && DataAnalysisDataSources[0] != null)
                {
                    DataAnalysisDataSources[0].IsFilterEnabled = true;
                    DataAnalysisDataSources[0].FilterMin = Channel4FilterMin;
                    DataAnalysisDataSources[0].FilterMax = Channel4FilterMax;
                }

                if (DataAnalysisDataSources.Count > 1 && DataAnalysisDataSources[1] != null)
                {
                    DataAnalysisDataSources[1].IsFilterEnabled = true;
                    DataAnalysisDataSources[1].FilterMin = DownSurfaceFilterMin;
                    DataAnalysisDataSources[1].FilterMax = DownSurfaceFilterMax;
                }
            }

            _hasAppliedLegacyFilterSettingsToDataSources = true;
        }

        private static ObservableCollection<DataAnalysisDataSourceOption> CreateDefaultDataAnalysisDataSources()
        {
            return new ObservableCollection<DataAnalysisDataSourceOption>
            {
                new DataAnalysisDataSourceOption(
                    UpSurfaceResultOption,
                    PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName,
                    true,
                    false,
                    false,
                    -99999999d,
                    99999999d),
                new DataAnalysisDataSourceOption(
                    DownSurfaceResultOption,
                    PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName,
                    true,
                    false,
                    false,
                    -99999999d,
                    99999999d)
            };
        }

        private static ObservableCollection<DataAnalysisAlgorithmOption> CreateDefaultDataAnalysisAlgorithms()
        {
            return new ObservableCollection<DataAnalysisAlgorithmOption>
            {
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Flatness, true),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.SurfaceStatistics, true),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Parallelism, true),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.TTV, true),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.THK, true),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.TIR, false),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Warp1, false),
                new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Warp2, false)
            };
        }

        private void AttachDataAnalysisDataSourceListeners(ObservableCollection<DataAnalysisDataSourceOption>? sources)
        {
            if (sources == null)
            {
                return;
            }

            sources.CollectionChanged -= OnDataAnalysisDataSourcesChanged;
            sources.CollectionChanged += OnDataAnalysisDataSourcesChanged;
            foreach (DataAnalysisDataSourceOption source in sources.Where(source => source != null))
            {
                source.PropertyChanged -= OnDataAnalysisDataSourcePropertyChanged;
                source.PropertyChanged += OnDataAnalysisDataSourcePropertyChanged;
            }
        }

        private void DetachDataAnalysisDataSourceListeners(ObservableCollection<DataAnalysisDataSourceOption>? sources)
        {
            if (sources == null)
            {
                return;
            }

            sources.CollectionChanged -= OnDataAnalysisDataSourcesChanged;
            foreach (DataAnalysisDataSourceOption source in sources.Where(source => source != null))
            {
                source.PropertyChanged -= OnDataAnalysisDataSourcePropertyChanged;
            }
        }

        private void OnDataAnalysisDataSourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(() => OnDataAnalysisDataSourcesChanged(sender, e));
                return;
            }

            if (e.OldItems != null)
            {
                foreach (DataAnalysisDataSourceOption source in e.OldItems.OfType<DataAnalysisDataSourceOption>())
                {
                    source.PropertyChanged -= OnDataAnalysisDataSourcePropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (DataAnalysisDataSourceOption source in e.NewItems.OfType<DataAnalysisDataSourceOption>())
                {
                    source.PropertyChanged -= OnDataAnalysisDataSourcePropertyChanged;
                    source.PropertyChanged += OnDataAnalysisDataSourcePropertyChanged;
                }
            }

            if (NormalizeDataAnalysisDataSources(allowCollectionReplacement: false))
            {
                return;
            }

            EnsureDataAnalysisDataSourceIds();
            SyncDataAnalysisAlgorithmSourceSelections();
            SyncLegacySurfaceFieldsFromDataAnalysisSources();
            RaisePropertyChanged(nameof(SurfaceResultOptions));
            RaisePropertyChanged(nameof(SurfaceChannelSummary));
        }

        private void OnDataAnalysisDataSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(() => OnDataAnalysisDataSourcePropertyChanged(sender, e));
                return;
            }

            if (e.PropertyName == nameof(DataAnalysisDataSourceOption.IsEnabled) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.Name) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.OriginalDataValueName) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.IsFilterEnabled) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.IsRawDataFilter) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.FilterMin) ||
                e.PropertyName == nameof(DataAnalysisDataSourceOption.FilterMax))
            {
                if (!_isSyncingLegacySurfaceDataSources &&
                    (e.PropertyName == nameof(DataAnalysisDataSourceOption.Name) ||
                     e.PropertyName == nameof(DataAnalysisDataSourceOption.OriginalDataValueName)) &&
                    NormalizeDataAnalysisDataSources(allowCollectionReplacement: false))
                {
                    return;
                }

                SyncDataAnalysisAlgorithmSourceSelections();
                SyncLegacySurfaceFieldsFromDataAnalysisSources();
                RaisePropertyChanged(nameof(SurfaceResultOptions));
                RaisePropertyChanged(nameof(SurfaceChannelSummary));
            }
        }

        private List<DataAnalysisDataSourceOption> GetEnabledDataAnalysisSources()
        {
            if (!IsOnUiThread())
            {
                List<DataAnalysisDataSourceOption> enabledSources = new List<DataAnalysisDataSourceOption>();
                ExecuteOnUiThreadSync(() =>
                {
                    enabledSources = GetEnabledDataAnalysisSources();
                });
                return enabledSources;
            }

            EnsureDataAnalysisDataSourcesOnly();
            return DataAnalysisDataSources
                .Where(source =>
                    source != null &&
                    source.IsEnabled &&
                    !string.IsNullOrWhiteSpace(source.OriginalDataValueName))
                .ToList();
        }

        private void EnsureDataAnalysisDataSourcesOnly()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(EnsureDataAnalysisDataSourcesOnly);
                return;
            }

            if (DataAnalysisDataSources == null || DataAnalysisDataSources.Count == 0)
            {
                DataAnalysisDataSources = CreateDefaultDataAnalysisDataSources();
            }
        }

        private void SyncLegacySurfaceFieldsFromDataAnalysisSources()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(SyncLegacySurfaceFieldsFromDataAnalysisSources);
                return;
            }

            if (_isSyncingLegacySurfaceDataSources)
            {
                return;
            }

            _isSyncingLegacySurfaceDataSources = true;
            try
            {
                List<DataAnalysisDataSourceOption> enabledSources = DataAnalysisDataSources?
                    .Where(source => source != null && source.IsEnabled)
                    .ToList() ?? new List<DataAnalysisDataSourceOption>();

                if (enabledSources.Count > 0)
                {
                    _upSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                        enabledSources[0].OriginalDataValueName,
                        PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName);
                    RaisePropertyChanged(nameof(UpSurfaceOriginalDataValueName));
                }

                if (enabledSources.Count > 1)
                {
                    _downSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                        enabledSources[1].OriginalDataValueName,
                        PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName);
                    RaisePropertyChanged(nameof(DownSurfaceOriginalDataValueName));
                }
            }
            finally
            {
                _isSyncingLegacySurfaceDataSources = false;
            }
        }

        private void SyncDataAnalysisSourcesFromLegacySurfaceFields()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(SyncDataAnalysisSourcesFromLegacySurfaceFields);
                return;
            }

            if (_isSyncingLegacySurfaceDataSources)
            {
                return;
            }

            _isSyncingLegacySurfaceDataSources = true;
            try
            {
                EnsureDataAnalysisDataSourcesOnly();
                if (DataAnalysisDataSources.Count == 0)
                {
                    DataAnalysisDataSources.Add(new DataAnalysisDataSourceOption(
                        UpSurfaceResultOption,
                        UpSurfaceOriginalDataValueName));
                }
                else
                {
                    DataAnalysisDataSources[0].Name = string.IsNullOrWhiteSpace(DataAnalysisDataSources[0].Name)
                        ? UpSurfaceResultOption
                        : DataAnalysisDataSources[0].Name;
                    DataAnalysisDataSources[0].OriginalDataValueName = UpSurfaceOriginalDataValueName;
                }

                if (DataAnalysisDataSources.Count == 1)
                {
                    DataAnalysisDataSources.Add(new DataAnalysisDataSourceOption(
                        DownSurfaceResultOption,
                        DownSurfaceOriginalDataValueName));
                }
                else
                {
                    DataAnalysisDataSources[1].Name = string.IsNullOrWhiteSpace(DataAnalysisDataSources[1].Name)
                        ? DownSurfaceResultOption
                        : DataAnalysisDataSources[1].Name;
                    DataAnalysisDataSources[1].OriginalDataValueName = DownSurfaceOriginalDataValueName;
                }
            }
            finally
            {
                _isSyncingLegacySurfaceDataSources = false;
            }

            NormalizeDataAnalysisDataSources();
            SyncDataAnalysisAlgorithmSourceSelections();
            RaisePropertyChanged(nameof(SurfaceChannelSummary));
        }

        public void AddDataAnalysisDataSource()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(AddDataAnalysisDataSource);
                return;
            }

            EnsureDataAnalysisDataSourcesOnly();
            int index = DataAnalysisDataSources.Count + 1;
            DataAnalysisDataSources.Add(new DataAnalysisDataSourceOption(
                $"DataSource{index}",
                PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName,
                true,
                false,
                false,
                -99999999d,
                99999999d));
        }

        public void RemoveDataAnalysisDataSource(DataAnalysisDataSourceOption? source)
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(() => RemoveDataAnalysisDataSource(source));
                return;
            }

            if (source == null || DataAnalysisDataSources == null)
            {
                return;
            }

            DataAnalysisDataSources.Remove(source);
            if (DataAnalysisDataSources.Count == 0)
            {
                ResetDataAnalysisDataSourcesInPlace();
            }
        }

        public void ResetDataAnalysisDataSources()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(ResetDataAnalysisDataSources);
                return;
            }

            ResetDataAnalysisDataSourcesInPlace();
            UpSurfaceOriginalDataValueName = PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;
            DownSurfaceOriginalDataValueName = PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;
        }

        private void ResetDataAnalysisDataSourcesInPlace()
        {
            if (!IsOnUiThread())
            {
                ExecuteOnUiThreadSync(ResetDataAnalysisDataSourcesInPlace);
                return;
            }

            ObservableCollection<DataAnalysisDataSourceOption> defaultSources = CreateDefaultDataAnalysisDataSources();
            if (DataAnalysisDataSources == null)
            {
                DataAnalysisDataSources = defaultSources;
                return;
            }

            DataAnalysisDataSources.Clear();
            foreach (DataAnalysisDataSourceOption source in defaultSources)
            {
                DataAnalysisDataSources.Add(source);
            }
        }

        private static (double minValue, double maxValue) NormalizeFilterRange(
            double configuredMinValue,
            double configuredMaxValue,
            string surfaceName)
        {
            if (configuredMinValue <= configuredMaxValue)
            {
                return (configuredMinValue, configuredMaxValue);
            }

            Logs.LogWarning(
                $"{surfaceName} 过滤范围配置异常，下限 {configuredMinValue:F3} 大于上限 {configuredMaxValue:F3}，已自动按 [{configuredMaxValue:F3}, {configuredMinValue:F3}] 处理。");
            return (configuredMaxValue, configuredMinValue);
        }

        private List<MeasureData> ProcessCollectedData(List<MeasureData>? sensorDatas, bool applyRawDataFilters = false)
        {
            List<DataAnalysisSourceFilter> rawDataFilters = applyRawDataFilters
                ? GetActiveDataAnalysisSourceFilters(isRawDataFilter: true)
                : new List<DataAnalysisSourceFilter>();

            SensorPointDataProcessingResult result = SensorPointDataProcessor.ProcessCollectedData(
                new SensorPointDataProcessingOptions
                {
                    SensorDatas = sensorDatas,
                    PreDatas = PreDatas,
                    PointCollectionSteps = _pointCollectionSteps,
                    RawDataFilters = rawDataFilters,
                    UpSurfaceOriginalDataValueName = UpSurfaceOriginalDataValueName,
                    DownSurfaceOriginalDataValueName = DownSurfaceOriginalDataValueName
                });

            LastCalibrationWaferDataCsvPath = result.HasCalibrationWaferReference
                ? SensorDataStorageService.SaveCalibrationWaferDataCsv(
                    result.CalibrationWaferDatas,
                    PreDatasCsvDirectory)
                : string.Empty;

            return result.DataCollect;
        }

        /// <summary>
        /// Align collected data with trajectory positions.
        /// </summary>
        /// <param name="dataCollect"></param>
        /// <returns></returns>
        private List<MeasureData> AlignCollectedDataWithPositions(List<MeasureData>? dataCollect)
        {
            return SensorPointDataProcessor.AlignCollectedDataWithPositions(dataCollect, PreDatas);
        }
        #endregion

    }
}

using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Defines;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.Models
{
    public class TronSightSensorModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private TronSightSensor _sensor;
        public TronSightSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private BasicSettingsModel _basicSettings = new BasicSettingsModel();
        public BasicSettingsModel BasicSettings
        {
            get { return _basicSettings; }
            set { _basicSettings = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TriggerConfigModel _triggerConfig = new TriggerConfigModel();
        public TriggerConfigModel TriggerConfig
        {
            get { return _triggerConfig; }
            set { _triggerConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private MultiMathConfigModel _multiMathConfig = new MultiMathConfigModel();
        public MultiMathConfigModel MultiMathConfig
        {
            get { return _multiMathConfig; }
            set { _multiMathConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private DeviceConfigModel _deviceConfig = new DeviceConfigModel();
        public DeviceConfigModel DeviceConfig
        {
            get { return _deviceConfig; }
            set { _deviceConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CalibrationConfigModel _calibrationConfig = new CalibrationConfigModel();
        public CalibrationConfigModel CalibrationConfig
        {
            get { return _calibrationConfig; }
            set { _calibrationConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private RefractiveConfigModel _refractiveConfig = new RefractiveConfigModel();
        public RefractiveConfigModel RefractiveConfig
        {
            get { return _refractiveConfig; }
            set { _refractiveConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TronSightRawImageConfigModel _rawImageConfig = new TronSightRawImageConfigModel();
        public TronSightRawImageConfigModel RawImageConfig
        {
            get { return _rawImageConfig; }
            set { _rawImageConfig = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public TronSightSensorModel()
        {
            
        }
        #endregion

        #region Methods

        #endregion

        #region Commands

        #endregion
    }

    /// <summary>
    /// 基础设置模型
    /// </summary>
    public class BasicSettingsModel : BindableBase
    {
        [JsonIgnore]
        private MeasureMode _selectedMeasureMode;
        public MeasureMode SelectedMeasureMode
        {
            get { return _selectedMeasureMode; }
            set { _selectedMeasureMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<MeasureMode> _measureModes;
        public ObservableCollection<MeasureMode> MeasureModes
        {
            get
            {
                if (_measureModes == null)
                {
                    _measureModes = new ObservableCollection<MeasureMode>();
                    foreach (MeasureMode mode in Enum.GetValues(typeof(MeasureMode)))
                    {
                        _measureModes.Add(mode);
                    }
                }
                return _measureModes;
            }
        }

        [JsonIgnore]
        private int _invalidDataHold;
        public int InvalidDataHold
        {
            get { return _invalidDataHold; }
            set { _invalidDataHold = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<SAMPLING_INTERVAL> _samplingIntervals;
        public ObservableCollection<SAMPLING_INTERVAL> SamplingIntervals
        {
            get
            {
                if (_samplingIntervals == null)
                {
                    _samplingIntervals = new ObservableCollection<SAMPLING_INTERVAL>();
                    foreach (SAMPLING_INTERVAL interval in Enum.GetValues(typeof(SAMPLING_INTERVAL)))
                    {
                        _samplingIntervals.Add(interval);
                    }
                }
                return _samplingIntervals;
            }
        }

        [JsonIgnore]
        private SAMPLING_INTERVAL _selectedSamplingInterval;
        public SAMPLING_INTERVAL SelectedSamplingInterval
        {
            get { return _selectedSamplingInterval; }
            set { _selectedSamplingInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<FILTER_WINDOW_WIDTH> _filterWindowWidths;
        public ObservableCollection<FILTER_WINDOW_WIDTH> FilterWindowWidths
        {
            get
            {
                if (_filterWindowWidths == null)
                {
                    _filterWindowWidths = new ObservableCollection<FILTER_WINDOW_WIDTH>();
                    foreach (FILTER_WINDOW_WIDTH width in Enum.GetValues(typeof(FILTER_WINDOW_WIDTH)))
                    {
                        _filterWindowWidths.Add(width);
                    }
                }
                return _filterWindowWidths;
            }
        }

        [JsonIgnore]
        private FILTER_WINDOW_WIDTH _movingAverageWindow;
        public FILTER_WINDOW_WIDTH MovingAverageWindow
        {
            get { return _movingAverageWindow; }
            set { _movingAverageWindow = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FILTER_WINDOW_WIDTH _medianWindow;
        public FILTER_WINDOW_WIDTH MedianWindow
        {
            get { return _medianWindow; }
            set { _medianWindow = value; RaisePropertyChanged(); }
        }

        #region 通道使能
        [JsonIgnore]
        private bool _channel1Enable = true;
        public bool Channel1Enable
        {
            get { return _channel1Enable; }
            set { _channel1Enable = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _channel2Enable = false;
        public bool Channel2Enable
        {
            get { return _channel2Enable; }
            set { _channel2Enable = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _channel3Enable = false;
        public bool Channel3Enable
        {
            get { return _channel3Enable; }
            set { _channel3Enable = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _channel4Enable = false;
        public bool Channel4Enable
        {
            get { return _channel4Enable; }
            set { _channel4Enable = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 最大通道数（从设备读取）
        /// </summary>
        [JsonIgnore]
        private int _maxChannelCount = 4;
        public int MaxChannelCount
        {
            get { return _maxChannelCount; }
            set { _maxChannelCount = value; RaisePropertyChanged(); }
        }
        #endregion
    }

    public class TriggerConfigModel : BindableBase
    {
        private TriggerSamplingMode _triggerSamplingMode;
        public TriggerSamplingMode TriggerSamplingMode
        {
            get { return _triggerSamplingMode; }
            set { _triggerSamplingMode = value; RaisePropertyChanged(); }
        }

        private SamplingEnableLevel _samplingEnableLevel;
        public SamplingEnableLevel SamplingEnableLevel
        {
            get { return _samplingEnableLevel; }
            set { _samplingEnableLevel = value; RaisePropertyChanged(); }
        }

        private int _singlePulseSamplingCount = 1;
        public int SinglePulseSamplingCount
        {
            get { return _singlePulseSamplingCount; }
            set { _singlePulseSamplingCount = value; RaisePropertyChanged(); }
        }

        private TriggerFilterWidth _triggerFilterWidth = TriggerFilterWidth._0_1us;
        public TriggerFilterWidth TriggerFilterWidth
        {
            get { return _triggerFilterWidth; }
            set { _triggerFilterWidth = value; RaisePropertyChanged(); }
        }

        private TriggerChannel _triggerChannel;
        public TriggerChannel TriggerChannel
        {
            get { return _triggerChannel; }
            set { _triggerChannel = value; RaisePropertyChanged(); }
        }

        private TriggerMode _triggerMode;
        public TriggerMode TriggerMode
        {
            get { return _triggerMode; }
            set { _triggerMode = value; RaisePropertyChanged(); }
        }

        private TriggerDirection _triggerDirection;
        public TriggerDirection TriggerDirection
        {
            get { return _triggerDirection; }
            set { _triggerDirection = value; RaisePropertyChanged(); }
        }

        private TrackingMode _trackingMode;
        public TrackingMode TrackingMode
        {
            get { return _trackingMode; }
            set { _trackingMode = value; RaisePropertyChanged(); }
        }

        private int _triggerInterval = 1;
        public int TriggerInterval
        {
            get { return _triggerInterval; }
            set { _triggerInterval = value; RaisePropertyChanged(); }
        }

        private EncoderConfigModel _encoder1 = new EncoderConfigModel();
        public EncoderConfigModel Encoder1
        {
            get { return _encoder1; }
            set { _encoder1 = value; RaisePropertyChanged(); }
        }

        private EncoderConfigModel _encoder2 = new EncoderConfigModel();
        public EncoderConfigModel Encoder2
        {
            get { return _encoder2; }
            set { _encoder2 = value; RaisePropertyChanged(); }
        }
    }

    public class MultiMathConfigModel : BindableBase
    {
        [JsonIgnore]
        private ObservableCollection<TronSightMultiMathChannelConfig> _channels =
            TronSightSensor.CreateDefaultMultiMathChannelConfigs();
        public ObservableCollection<TronSightMultiMathChannelConfig> Channels
        {
            get { return _channels; }
            set { _channels = value; RaisePropertyChanged(); }
        }

        public void LoadFrom(IEnumerable<TronSightMultiMathChannelConfig>? channels)
        {
            Channels = channels == null
                ? TronSightSensor.CreateDefaultMultiMathChannelConfigs()
                : new ObservableCollection<TronSightMultiMathChannelConfig>(channels.Select(item => item.Clone()));
        }
    }

    public class EncoderConfigModel : BindableBase
    {
        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private EncoderInputMode _inputMode;
        public EncoderInputMode InputMode
        {
            get { return _inputMode; }
            set { _inputMode = value; RaisePropertyChanged(); }
        }

        private EncoderDecodeMode _decodeMode;
        public EncoderDecodeMode DecodeMode
        {
            get { return _decodeMode; }
            set { _decodeMode = value; RaisePropertyChanged(); }
        }

        private bool _zPhaseEnable;
        public bool ZPhaseEnable
        {
            get { return _zPhaseEnable; }
            set { _zPhaseEnable = value; RaisePropertyChanged(); }
        }

        private double _pulseRatio = 0.000000;
        public double PulseRatio
        {
            get { return _pulseRatio; }
            set { _pulseRatio = value; RaisePropertyChanged(); }
        }

        private double _manualResetValue = 0.000000;
        public double ManualResetValue
        {
            get { return _manualResetValue; }
            set { _manualResetValue = value; RaisePropertyChanged(); }
        }

        private double _zSignalResetValue = 0.000000;
        public double ZSignalResetValue
        {
            get { return _zSignalResetValue; }
            set { _zSignalResetValue = value; RaisePropertyChanged(); }
        }
    }

    public class DeviceConfigModel : BindableBase
    {
        private bool _isSolidifyParams;
        public bool IsSolidifyParams
        {
            get { return _isSolidifyParams; }
            set { _isSolidifyParams = value; RaisePropertyChanged(); }
        }
    }

    public class CalibrationConfigModel : BindableBase
    {
        private ThicknessCalibrationMode _thicknessCalibrationMode = ThicknessCalibrationMode.对射测厚;
        public ThicknessCalibrationMode ThicknessCalibrationMode
        {
            get { return _thicknessCalibrationMode; }
            set { _thicknessCalibrationMode = value; RaisePropertyChanged(); }
        }

        private CalibrationMethod _calibrationMethod;
        public CalibrationMethod CalibrationMethod
        {
            get { return _calibrationMethod; }
            set { _calibrationMethod = value; RaisePropertyChanged(); }
        }

        private int _averageCount = 1;
        public int AverageCount
        {
            get { return _averageCount; }
            set { _averageCount = value; RaisePropertyChanged(); }
        }

        private double _standardValue = 0.000000;
        public double StandardValue
        {
            get { return _standardValue; }
            set { _standardValue = value; RaisePropertyChanged(); }
        }

        private double _actualValue = 0.000000;
        public double ActualValue
        {
            get { return _actualValue; }
            set { _actualValue = value; RaisePropertyChanged(); }
        }

        private double _measuredValue = 0.000000;
        public double MeasuredValue
        {
            get { return _measuredValue; }
            set { _measuredValue = value; RaisePropertyChanged(); }
        }

        // 两点标定 - 标定值2
        private double _standardValue2 = 0.000000;
        public double StandardValue2
        {
            get { return _standardValue2; }
            set { _standardValue2 = value; RaisePropertyChanged(); }
        }

        // 两点标定 - 测量值2
        private double _measuredValue2 = 0.000000;
        public double MeasuredValue2
        {
            get { return _measuredValue2; }
            set { _measuredValue2 = value; RaisePropertyChanged(); }
        }

        private double _correctionFactor = 1.000;
        public double CorrectionFactor
        {
            get { return _correctionFactor; }
            set { _correctionFactor = value; RaisePropertyChanged(); }
        }

        // 标定结果 - 系数k
        private double _calibrationK = 1.000;
        public double CalibrationK
        {
            get { return _calibrationK; }
            set { _calibrationK = value; RaisePropertyChanged(); }
        }

        // 标定结果 - 偏移b
        private double _calibrationB = 0.000;
        public double CalibrationB
        {
            get { return _calibrationB; }
            set { _calibrationB = value; RaisePropertyChanged(); }
        }
    }

    public class TronSightRawImageConfigModel : BindableBase
    {
        private int _sensorChannelIndex;
        public int SensorChannelIndex
        {
            get { return _sensorChannelIndex; }
            set { _sensorChannelIndex = value; RaisePropertyChanged(); }
        }

        private FRAME_DATA_SRC _frameDataSource = FRAME_DATA_SRC.ORIGIN;
        public FRAME_DATA_SRC FrameDataSource
        {
            get { return _frameDataSource; }
            set { _frameDataSource = value; RaisePropertyChanged(); }
        }

        private bool _isLightSourceEnabled;
        public bool IsLightSourceEnabled
        {
            get { return _isLightSourceEnabled; }
            set { _isLightSourceEnabled = value; RaisePropertyChanged(); }
        }

        private double _lightIntensity = 20;
        public double LightIntensity
        {
            get { return _lightIntensity; }
            set { _lightIntensity = value; RaisePropertyChanged(); }
        }

        private bool _isAutoExposureEnabled;
        public bool IsAutoExposureEnabled
        {
            get { return _isAutoExposureEnabled; }
            set { _isAutoExposureEnabled = value; RaisePropertyChanged(); }
        }

        private int _manualExposure = 500;
        public int ManualExposure
        {
            get { return _manualExposure; }
            set { _manualExposure = value; RaisePropertyChanged(); }
        }

        private int _targetExposure = 500;
        public int TargetExposure
        {
            get { return _targetExposure; }
            set { _targetExposure = value; RaisePropertyChanged(); }
        }

        private int _minExposure = 1;
        public int MinExposure
        {
            get { return _minExposure; }
            set { _minExposure = value; RaisePropertyChanged(); }
        }

        private int _maxExposure = 5000;
        public int MaxExposure
        {
            get { return _maxExposure; }
            set { _maxExposure = value; RaisePropertyChanged(); }
        }

        private PEAK_SELECTION_MODE _peakSelectionMode = PEAK_SELECTION_MODE.MAX;
        public PEAK_SELECTION_MODE PeakSelectionMode
        {
            get { return _peakSelectionMode; }
            set { _peakSelectionMode = value; RaisePropertyChanged(); }
        }

        private PEAK_SORT_MODE _peakSortMode = PEAK_SORT_MODE.LEFT_AND_RIGHT;
        public PEAK_SORT_MODE PeakSortMode
        {
            get { return _peakSortMode; }
            set { _peakSortMode = value; RaisePropertyChanged(); }
        }

        private int _peakHeightThreshold = 10;
        public int PeakHeightThreshold
        {
            get { return _peakHeightThreshold; }
            set { _peakHeightThreshold = value; RaisePropertyChanged(); }
        }

        private int _peakSharpnessThreshold = 10;
        public int PeakSharpnessThreshold
        {
            get { return _peakSharpnessThreshold; }
            set { _peakSharpnessThreshold = value; RaisePropertyChanged(); }
        }

        private int _peakMinSpacing = 2;
        public int PeakMinSpacing
        {
            get { return _peakMinSpacing; }
            set { _peakMinSpacing = value; RaisePropertyChanged(); }
        }

        private int _peak1Index = 1;
        public int Peak1Index
        {
            get { return _peak1Index; }
            set { _peak1Index = value; RaisePropertyChanged(); }
        }

        private int _peak1Start;
        public int Peak1Start
        {
            get { return _peak1Start; }
            set { _peak1Start = value; RaisePropertyChanged(); }
        }

        private int _peak1End = 1023;
        public int Peak1End
        {
            get { return _peak1End; }
            set { _peak1End = value; RaisePropertyChanged(); }
        }

        private int _peak2Index = 2;
        public int Peak2Index
        {
            get { return _peak2Index; }
            set { _peak2Index = value; RaisePropertyChanged(); }
        }

        private int _peak2Start;
        public int Peak2Start
        {
            get { return _peak2Start; }
            set { _peak2Start = value; RaisePropertyChanged(); }
        }

        private int _peak2End = 1023;
        public int Peak2End
        {
            get { return _peak2End; }
            set { _peak2End = value; RaisePropertyChanged(); }
        }

        private bool _isAutoImageFilterEnabled = true;
        public bool IsAutoImageFilterEnabled
        {
            get { return _isAutoImageFilterEnabled; }
            set { _isAutoImageFilterEnabled = value; RaisePropertyChanged(); }
        }

        private IMAGE_FILTER_WIDTH _imageFilterWidth = IMAGE_FILTER_WIDTH._1;
        public IMAGE_FILTER_WIDTH ImageFilterWidth
        {
            get { return _imageFilterWidth; }
            set { _imageFilterWidth = value; RaisePropertyChanged(); }
        }

        private bool _isYAxisAuto = true;
        public bool IsYAxisAuto
        {
            get { return _isYAxisAuto; }
            set { _isYAxisAuto = value; RaisePropertyChanged(); }
        }

        private bool _isXAxisAuto = true;
        public bool IsXAxisAuto
        {
            get { return _isXAxisAuto; }
            set { _isXAxisAuto = value; RaisePropertyChanged(); }
        }

        private bool _isMeasureRangeVisible = true;
        public bool IsMeasureRangeVisible
        {
            get { return _isMeasureRangeVisible; }
            set { _isMeasureRangeVisible = value; RaisePropertyChanged(); }
        }

        private bool _isThresholdVisible = true;
        public bool IsThresholdVisible
        {
            get { return _isThresholdVisible; }
            set { _isThresholdVisible = value; RaisePropertyChanged(); }
        }

        private bool _isAutoRefreshEnabled;
        public bool IsAutoRefreshEnabled
        {
            get { return _isAutoRefreshEnabled; }
            set { _isAutoRefreshEnabled = value; RaisePropertyChanged(); }
        }

        private double _maxPeakValue;
        public double MaxPeakValue
        {
            get { return _maxPeakValue; }
            set { _maxPeakValue = value; RaisePropertyChanged(); }
        }

        private int _maxPeakPosition;
        public int MaxPeakPosition
        {
            get { return _maxPeakPosition; }
            set { _maxPeakPosition = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 折射率配置模型
    /// </summary>
    public class RefractiveConfigModel : BindableBase
    {
        // 静态变量：保存当前会话状态（关闭页面后保留，重启程序恢复默认）
        private static string _sessionFilePath = "";
        private static List<RefractiveTableItem>? _sessionTableData = null;

        /// <summary>
        /// 操作模式：true=折射率表修改，false=折射率表选择
        /// </summary>
        private bool _isEditMode = true;
        public bool IsEditMode
        {
            get { return _isEditMode; }
            set { _isEditMode = value;  RaisePropertyChanged(); }
        }

        /// <summary>
        /// 文件路径（用于修改模式）
        /// </summary>
        private string _filePath = "";
        public string FilePath
        {
            get { return _filePath; }
            set 
            { 
                _filePath = value; 
                _sessionFilePath = value; // 同步保存到会话变量
                RaisePropertyChanged(); 
            }
        }

        /// <summary>
        /// 获取当前会话路径（如果有），否则返回默认路径
        /// </summary>
        public static string GetSessionOrDefaultPath()
        {
            if (!string.IsNullOrEmpty(_sessionFilePath))
                return _sessionFilePath;
            return GetDefaultFilePath();
        }

        /// <summary>
        /// 保存表格数据到会话
        /// </summary>
        public static void SaveSessionTableData(ObservableCollection<RefractiveTableItem> items)
        {
            _sessionTableData = items.Select(x => new RefractiveTableItem
            {
                Index = x.Index,
                Material = x.Material,
                C486nm = x.C486nm,
                C587nm = x.C587nm,
                C656nm = x.C656nm,
                IsSelected = x.IsSelected
            }).ToList();
        }

        /// <summary>
        /// 获取会话表格数据（如果有）
        /// </summary>
        public static List<RefractiveTableItem>? GetSessionTableData()
        {
            return _sessionTableData;
        }

        /// <summary>
        /// 是否有会话表格数据
        /// </summary>
        public static bool HasSessionTableData => _sessionTableData != null && _sessionTableData.Count > 0;

        /// <summary>
        /// 获取默认折射率文件路径
        /// </summary>
        public static string GetDefaultFilePath()
        {
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(exePath, "ts-data", "refractive.txt");
        }

        /// <summary>
        /// 确保默认文件存在，不存在则创建空文件
        /// </summary>
        public static void EnsureDefaultFileExists()
        {
            string filePath = GetDefaultFilePath();
            string? dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            if (!System.IO.File.Exists(filePath))
            {
                // 创建空文件（16行空数据）
                var lines = new System.Collections.Generic.List<string>();
                for (int i = 0; i < 16; i++)
                {
                    lines.Add(",--,--,--");
                }
                System.IO.File.WriteAllLines(filePath, lines);
            }
        }

        /// <summary>
        /// 折射率表列表
        /// </summary>
        private ObservableCollection<RefractiveTableItem> _refractiveTableItems = new ObservableCollection<RefractiveTableItem>();
        public ObservableCollection<RefractiveTableItem> RefractiveTableItems
        {
            get { return _refractiveTableItems; }
            set { _refractiveTableItems = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的折射率表项
        /// </summary>
        private RefractiveTableItem _selectedItem;
        public RefractiveTableItem SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前探头通道
        /// </summary>
        private string _currentChannel = "CH1";
        public string CurrentChannel
        {
            get { return _currentChannel; }
            set { _currentChannel = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前使用的折射率表序号
        /// </summary>
        private int _currentLabel = 1;
        public int CurrentLabel
        {
            get { return _currentLabel; }
            set { _currentLabel = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 可用的通道列表 (CH1, CH2, CH3, CH4)
        /// </summary>
        private ObservableCollection<string> _channels = new ObservableCollection<string> { "CH1", "CH2", "CH3", "CH4" };
        public ObservableCollection<string> Channels
        {
            get { return _channels; }
            set { _channels = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 可用的层序号列表 (1-5)
        /// </summary>
        private ObservableCollection<int> _labels = new ObservableCollection<int> { 1, 2, 3, 4, 5 };
        public ObservableCollection<int> Labels
        {
            get { return _labels; }
            set { _labels = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 折射率表项
    /// </summary>
    public class RefractiveTableItem : BindableBase
    {
        /// <summary>
        /// 父级配置模型引用（用于单选逻辑）
        /// </summary>
        public RefractiveConfigModel? Parent { get; set; }

        private int _index;
        public int Index
        {
            get { return _index; }
            set { _index = value; RaisePropertyChanged(); }
        }

        private string _material = "--";
        public string Material
        {
            get { return _material; }
            set { _material = value; RaisePropertyChanged(); }
        }

        private string _c486nm = "--";
        public string C486nm
        {
            get { return _c486nm; }
            set { _c486nm = value; RaisePropertyChanged(); }
        }

        private string _c587nm = "--";
        public string C587nm
        {
            get { return _c587nm; }
            set { _c587nm = value; RaisePropertyChanged(); }
        }

        private string _c656nm = "--";
        public string C656nm
        {
            get { return _c656nm; }
            set { _c656nm = value; RaisePropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set 
            { 
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChanged();
                    
                    // 单选逻辑：选中时清除其他项的选中状态
                    if (value && Parent != null)
                    {
                        foreach (var item in Parent.RefractiveTableItems)
                        {
                            if (item != this && item.IsSelected)
                            {
                                item._isSelected = false;
                                item.RaisePropertyChanged(nameof(IsSelected));
                            }
                        }
                    }
                }
            }
        }
    }
}

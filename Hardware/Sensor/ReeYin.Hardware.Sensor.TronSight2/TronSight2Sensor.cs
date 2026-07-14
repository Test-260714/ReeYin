using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models;
using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.TronSight2.API;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using InstanceHandle = System.UInt64;

namespace ReeYin.Hardware.Sensor.TronSight2
{
    /// <summary>
    /// TronSight2 传感器
    /// </summary>
    public class TronSight2Sensor : SensorBase
    {
        #region Fields
        private InstanceHandle _instance;
        private const int ControllerIndex = 0;
        private const int SDK_RING_BUFFER_SIZE = 2000000000;
        private const int InvalidDataHoldPointsMin = 0;
        private const int InvalidDataHoldPointsMax = 1000;
        private const int DefaultSensorChannel = 1;
        private const int RefractiveTableMaxCount = 32;
        private const int RefractiveLayerMin = 1;
        private const int RefractiveLayerMax = 5;
        private readonly object _dataLock = new object();
        #endregion

        #region Properties
        [JsonIgnore]
        public InstanceHandle Instance
        {
            get { return _instance; }
            set { _instance = value; RaisePropertyChanged(); }
        }

        private API.MEASUREMODE _selectedMeasureMode = API.MEASUREMODE.INTERF_THICKNESS_SINGLE_LAYER;
        public API.MEASUREMODE SelectedMeasureMode
        {
            get { return _selectedMeasureMode; }
            // 模式由UI配置页下拉框双向绑定，采集时读取该值决定输出配置和解析逻辑。
            set { _selectedMeasureMode = value; RaisePropertyChanged(); }
        }

        private int _invalidDataHoldPoints;
        public int InvalidDataHoldPoints
        {
            get { return _invalidDataHoldPoints; }
            set
            {
                int normalizedValue = Math.Clamp(value, InvalidDataHoldPointsMin, InvalidDataHoldPointsMax);
                if (_invalidDataHoldPoints == normalizedValue)
                {
                    return;
                }

                _invalidDataHoldPoints = normalizedValue;
                RaisePropertyChanged();
            }
        }

        private API.SAMPLING_INTERVAL _selectedSamplingInterval = API.SAMPLING_INTERVAL._100US;
        public API.SAMPLING_INTERVAL SelectedSamplingInterval
        {
            get { return _selectedSamplingInterval; }
            set { _selectedSamplingInterval = value; RaisePropertyChanged(); }
        }

        private API.FILTER_WINDOW_WIDTH _selectedMoveAverageWindow = API.FILTER_WINDOW_WIDTH._1;
        public API.FILTER_WINDOW_WIDTH SelectedMoveAverageWindow
        {
            get { return _selectedMoveAverageWindow; }
            set { _selectedMoveAverageWindow = value; RaisePropertyChanged(); }
        }

        private API.MEDIAN_FILTER_WIDTH _selectedMedianFilterWidth = API.MEDIAN_FILTER_WIDTH._1;
        public API.MEDIAN_FILTER_WIDTH SelectedMedianFilterWidth
        {
            get { return _selectedMedianFilterWidth; }
            set { _selectedMedianFilterWidth = value; RaisePropertyChanged(); }
        }

        private TriggerSampleMode _selectedTriggerSampleMode = TriggerSampleMode.FixedInterval;
        public TriggerSampleMode SelectedTriggerSampleMode
        {
            get { return _selectedTriggerSampleMode; }
            set
            {
                if (_selectedTriggerSampleMode == value)
                {
                    return;
                }

                _selectedTriggerSampleMode = value;
                RaisePropertyChanged();
                RaiseTriggerUiPropertiesChanged();
            }
        }

        private API.SYNC_VALID_LEVEL _selectedSyncValidLevel = API.SYNC_VALID_LEVEL.HIGH;
        public API.SYNC_VALID_LEVEL SelectedSyncValidLevel
        {
            get { return _selectedSyncValidLevel; }
            set { _selectedSyncValidLevel = value; RaisePropertyChanged(); }
        }

        private int _singlePulseSampleCount = 1;
        public int SinglePulseSampleCount
        {
            get { return _singlePulseSampleCount; }
            set
            {
                int normalizedValue = Math.Clamp(value, 1, ushort.MaxValue);
                if (_singlePulseSampleCount == normalizedValue)
                {
                    return;
                }

                _singlePulseSampleCount = normalizedValue;
                RaisePropertyChanged();
            }
        }

        private API.SYNC_FILTER_WIDTH _selectedSyncFilterWidth = API.SYNC_FILTER_WIDTH._0_1_US;
        public API.SYNC_FILTER_WIDTH SelectedSyncFilterWidth
        {
            get { return _selectedSyncFilterWidth; }
            set { _selectedSyncFilterWidth = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsSyncValidLevelEnabled
        {
            get
            {
                return SelectedTriggerSampleMode == TriggerSampleMode.FixedIntervalWithSyncOutput
                    || SelectedTriggerSampleMode == TriggerSampleMode.EncoderTriggerWithSyncOutput
                    || SelectedTriggerSampleMode == TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints;
            }
        }

        [JsonIgnore]
        public bool IsSinglePulseSampleCountEnabled
        {
            get { return SelectedTriggerSampleMode == TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints; }
        }

        [JsonIgnore]
        public bool IsSyncFilterWidthEnabled
        {
            get
            {
                return SelectedTriggerSampleMode == TriggerSampleMode.FixedIntervalWithSyncOutput
                    || SelectedTriggerSampleMode == TriggerSampleMode.EncoderTriggerWithSyncOutput
                    || SelectedTriggerSampleMode == TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints;
            }
        }

        private bool _persistConfigurationAfterImportOrRestore;
        public bool PersistConfigurationAfterImportOrRestore
        {
            get { return _persistConfigurationAfterImportOrRestore; }
            set { _persistConfigurationAfterImportOrRestore = value; RaisePropertyChanged(); }
        }

        private bool _isZSignalMultiplexedToEncoder3;
        public bool IsZSignalMultiplexedToEncoder3
        {
            get { return _isZSignalMultiplexedToEncoder3; }
            set
            {
                if (_isZSignalMultiplexedToEncoder3 == value)
                {
                    return;
                }

                _isZSignalMultiplexedToEncoder3 = value;
                if (value)
                {
                    Encoder1ZPhaseEnabled = false;
                    Encoder2ZPhaseEnabled = false;
                }
                else if (SelectedTriggerEncoderChannel == EncoderTriggerChannel.Encoder3)
                {
                    SelectedTriggerEncoderChannel = EncoderTriggerChannel.Encoder1;
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsEncoder1ZPhaseAvailable));
                RaisePropertyChanged(nameof(IsEncoder2ZPhaseAvailable));
            }
        }

        private EncoderTriggerChannel _selectedTriggerEncoderChannel = EncoderTriggerChannel.Encoder1;
        public EncoderTriggerChannel SelectedTriggerEncoderChannel
        {
            get { return _selectedTriggerEncoderChannel; }
            set
            {
                if (_selectedTriggerEncoderChannel == value)
                {
                    return;
                }

                _selectedTriggerEncoderChannel = value;
                if (value == EncoderTriggerChannel.Encoder3 && !IsZSignalMultiplexedToEncoder3)
                {
                    _isZSignalMultiplexedToEncoder3 = true;
                    RaisePropertyChanged(nameof(IsZSignalMultiplexedToEncoder3));
                    RaisePropertyChanged(nameof(IsEncoder1ZPhaseAvailable));
                    RaisePropertyChanged(nameof(IsEncoder2ZPhaseAvailable));
                    Encoder1ZPhaseEnabled = false;
                    Encoder2ZPhaseEnabled = false;
                }

                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public bool IsEncoder1ZPhaseAvailable
        {
            get { return !IsZSignalMultiplexedToEncoder3; }
        }

        [JsonIgnore]
        public bool IsEncoder2ZPhaseAvailable
        {
            get { return !IsZSignalMultiplexedToEncoder3; }
        }

        private API.TRIG_MODE _selectedTriggerMode = API.TRIG_MODE.COUNTER;
        public API.TRIG_MODE SelectedTriggerMode
        {
            get { return _selectedTriggerMode; }
            set { _selectedTriggerMode = value; RaisePropertyChanged(); }
        }

        private API.TRIG_DIRECTION _selectedTriggerDirection = API.TRIG_DIRECTION.POS;
        public API.TRIG_DIRECTION SelectedTriggerDirection
        {
            get { return _selectedTriggerDirection; }
            set { _selectedTriggerDirection = value; RaisePropertyChanged(); }
        }

        private API.TRIG_TRACK_MODE _selectedTriggerTrackMode = API.TRIG_TRACK_MODE.OFF;
        public API.TRIG_TRACK_MODE SelectedTriggerTrackMode
        {
            get { return _selectedTriggerTrackMode; }
            set { _selectedTriggerTrackMode = value; RaisePropertyChanged(); }
        }

        private int _triggerInterval = 1;
        public int TriggerInterval
        {
            get { return _triggerInterval; }
            set
            {
                int normalizedValue = Math.Max(1, value);
                if (_triggerInterval == normalizedValue)
                {
                    return;
                }

                _triggerInterval = normalizedValue;
                RaisePropertyChanged();
            }
        }

        private bool _encoder1Enabled;
        public bool Encoder1Enabled
        {
            get { return _encoder1Enabled; }
            set { _encoder1Enabled = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_FILTER_WIDTH _encoder1FilterWidth = API.ENCODER_FILTER_WIDTH._16;
        public API.ENCODER_FILTER_WIDTH Encoder1FilterWidth
        {
            get { return _encoder1FilterWidth; }
            set { _encoder1FilterWidth = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_INPUT_MODE _encoder1InputMode = API.ENCODER_INPUT_MODE.A;
        public API.ENCODER_INPUT_MODE Encoder1InputMode
        {
            get { return _encoder1InputMode; }
            set { _encoder1InputMode = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_OUTPUT_MODE _encoder1OutputMode = API.ENCODER_OUTPUT_MODE.X1;
        public API.ENCODER_OUTPUT_MODE Encoder1OutputMode
        {
            get { return _encoder1OutputMode; }
            set { _encoder1OutputMode = value; RaisePropertyChanged(); }
        }

        private bool _encoder1ZPhaseEnabled;
        public bool Encoder1ZPhaseEnabled
        {
            get { return _encoder1ZPhaseEnabled; }
            set
            {
                bool normalizedValue = IsZSignalMultiplexedToEncoder3 ? false : value;
                if (_encoder1ZPhaseEnabled == normalizedValue)
                {
                    return;
                }

                _encoder1ZPhaseEnabled = normalizedValue;
                RaisePropertyChanged();
            }
        }

        private double _encoder1Resolution = 0.000001d;
        public double Encoder1Resolution
        {
            get { return _encoder1Resolution; }
            set { _encoder1Resolution = value; RaisePropertyChanged(); }
        }

        private double _encoder1ManualPosition = 10d;
        public double Encoder1ManualPosition
        {
            get { return _encoder1ManualPosition; }
            set { _encoder1ManualPosition = value; RaisePropertyChanged(); }
        }

        private double _encoder1ZPhasePosition = 1d;
        public double Encoder1ZPhasePosition
        {
            get { return _encoder1ZPhasePosition; }
            set { _encoder1ZPhasePosition = value; RaisePropertyChanged(); }
        }

        private bool _encoder2Enabled;
        public bool Encoder2Enabled
        {
            get { return _encoder2Enabled; }
            set { _encoder2Enabled = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_FILTER_WIDTH _encoder2FilterWidth = API.ENCODER_FILTER_WIDTH._16;
        public API.ENCODER_FILTER_WIDTH Encoder2FilterWidth
        {
            get { return _encoder2FilterWidth; }
            set { _encoder2FilterWidth = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_INPUT_MODE _encoder2InputMode = API.ENCODER_INPUT_MODE.A;
        public API.ENCODER_INPUT_MODE Encoder2InputMode
        {
            get { return _encoder2InputMode; }
            set { _encoder2InputMode = value; RaisePropertyChanged(); }
        }

        private API.ENCODER_OUTPUT_MODE _encoder2OutputMode = API.ENCODER_OUTPUT_MODE.X1;
        public API.ENCODER_OUTPUT_MODE Encoder2OutputMode
        {
            get { return _encoder2OutputMode; }
            set { _encoder2OutputMode = value; RaisePropertyChanged(); }
        }

        private bool _encoder2ZPhaseEnabled;
        public bool Encoder2ZPhaseEnabled
        {
            get { return _encoder2ZPhaseEnabled; }
            set
            {
                bool normalizedValue = IsZSignalMultiplexedToEncoder3 ? false : value;
                if (_encoder2ZPhaseEnabled == normalizedValue)
                {
                    return;
                }

                _encoder2ZPhaseEnabled = normalizedValue;
                RaisePropertyChanged();
            }
        }

        private double _encoder2Resolution = 0.000001d;
        public double Encoder2Resolution
        {
            get { return _encoder2Resolution; }
            set { _encoder2Resolution = value; RaisePropertyChanged(); }
        }

        private double _encoder2ManualPosition = 10d;
        public double Encoder2ManualPosition
        {
            get { return _encoder2ManualPosition; }
            set { _encoder2ManualPosition = value; RaisePropertyChanged(); }
        }

        private double _encoder2ZPhasePosition = 1d;
        public double Encoder2ZPhasePosition
        {
            get { return _encoder2ZPhasePosition; }
            set { _encoder2ZPhasePosition = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public TronSight2Sensor()
        {
        }
        #endregion

        #region Override Methods
        public override bool Init()
        {
            try
            {
                // 创建实例
                _instance = TSCMCAPICS.CreateInstance();
                if (_instance == 0)
                {
                    Logs.LogError("TronSight2: 创建实例失败");
                    State = HardwareState.NotConnected;
                    return false;
                }

                State = HardwareState.Connecting;
                EnsureRingBufferSize();

                // 配置IP地址
                API.IPAddr deviceAddr = new API.IPAddr
                {
                    c1 = byte.Parse(IP.Split('.')[0]),
                    c2 = byte.Parse(IP.Split('.')[1]),
                    c3 = byte.Parse(IP.Split('.')[2]),
                    c4 = byte.Parse(IP.Split('.')[3])
                };

                // 打开以太网连接
                var ret = TSCMCAPICS.OpenConnectionEthernet(_instance, deviceAddr, Port);
                if (ret != API.ERRCODE.OK)
                {
                    Logs.LogError($"TronSight2: 打开连接失败，错误码: {ret}");
                    State = HardwareState.NotConnected;
                    IsConnected = false;
                    return false;
                }

                // 设置连接开启
                ret = TSCMCAPICS.SetConnectionOn(_instance, 0);
                if (ret == API.ERRCODE.FIRMWARE_NOT_SUPPORTED)
                {
                    Logs.LogWarning("TronSight2: 固件不支持 SetConnectionOn，按兼容模式继续");
                }
                else if (ret != API.ERRCODE.OK)
                {
                    Logs.LogError($"TronSight2: 设置连接开启失败，错误码: {ret}");
                    TSCMCAPICS.CloseConnectionPort(_instance);
                    State = HardwareState.NotConnected;
                    IsConnected = false;
                    return false;
                }

                State = HardwareState.Connected;
                IsConnected = true;
                Logs.LogInfo("TronSight2: 连接成功");
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 初始化异常 - {ex.Message}");
                State = HardwareState.NotConnected;
                IsConnected = false;
                return false;
            }
        }

        public override void Close()
        {
            try
            {
                State = HardwareState.Disconnecting;

                if (_instance != 0)
                {
                    // 停止数据采集
                    if (TSCMCAPICS.isAcquireData(_instance))
                    {
                        TSCMCAPICS.SetDataOutputOff(_instance, 0);
                    }

                    // 断开连接
                    TSCMCAPICS.SetConnectionOff(_instance, 0);   //向控制器发送断开连接命令
                    TSCMCAPICS.CloseConnectionPort(_instance);
                    TSCMCAPICS.ReleaseInstance(_instance);
                    _instance = 0;
                }

                State = HardwareState.Closed;
                IsConnected = false;
                Logs.LogInfo("TronSight2: 连接已关闭");
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 关闭连接异常 - {ex.Message}");
            }
        }

        public override void StartCollect()
        {
            try
            {
                lock (_dataLock)
                {
                    if (_instance == 0 || !TSCMCAPICS.isConnected(_instance))
                    {
                        Logs.LogError("TronSight2: 设备未连接");
                        State = HardwareState.NotConnected;
                        return;
                    }

                    // 先设置测量模式：干涉单层/干涉多层
                    var modeRet = TSCMCAPICS.SetConfigMeasurementMode(_instance, 0, GetApiMeasureMode());
                    if (modeRet != API.ERRCODE.OK)
                    {
                        Logs.LogWarning($"TronSight2: 设置测量模式失败，错误码: {modeRet}，将继续按当前设备模式采集");
                    }

                    // 根据模式动态选择输出项，后续ReceiveSensorData按同一套输出顺序组帧。
                    int[] dataSel = GetOutputSignals();
                    int dataCnt = dataSel.Length;
                    var connectionType = TSCMCAPICS.GetConnectionType(_instance);

                    // 配置输出信号
                    bool ch1ConfigOk = false;
                    for (int i = 0; i <= TSCMCAPICS.MaxSensorChannels(_instance); ++i)//判断当前控制器通道支持的最大通道数
                    {
                        int ntypes = i == 1 ? dataCnt : 0; // 仅选择输出探头
                        var configRet = TSCMCAPICS.SetConfigOutputSignals(_instance, 0, i, connectionType, ref dataSel[0], ntypes);  //设置连续输出数据的类型
                        if (i == 1 && configRet == API.ERRCODE.OK)
                        {
                            ch1ConfigOk = true;
                        }
                        if (configRet != API.ERRCODE.OK)
                        {
                            Logs.LogWarning($"TronSight2: 配置输出信号失败，通道{i}，错误码: {configRet}");
                        }
                    }

                    if (!ch1ConfigOk)
                    {
                        Logs.LogError("TronSight2: CH1输出信号配置失败，已取消开始采集");
                        return;
                    }

                    // 开始数据输出
                    var ret = TSCMCAPICS.SetDataOutputOn(_instance, 0);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 开始采集失败，错误码: {ret}");
                        return;
                    }

                    if (!TSCMCAPICS.isAcquireData(_instance))
                    {
                        Logs.LogWarning("TronSight2: 已开启数据输出但当前未处于采集态，可能是触发模式为外部触发或未满足触发条件");
                    }

                    State = HardwareState.Running;
                    Logs.LogInfo($"TronSight2: 开始采集数据，模式:{SelectedMeasureMode}，输出类型数:{dataCnt}");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 开始采集异常 - {ex.Message}");
            }
        }

        public override void StopCollect()
        {
            try
            {
                lock (_dataLock)
                {
                    if (_instance == 0)
                    {
                        return;
                    }

                    if (TSCMCAPICS.isAcquireData(_instance))
                    {
                        var ret = TSCMCAPICS.SetDataOutputOff(_instance, 0);
                        if (ret != API.ERRCODE.OK)
                        {
                            Logs.LogError($"TronSight2: 停止采集失败，错误码: {ret}");
                            return;
                        }
                    }

                    State = HardwareState.Complete;
                    Logs.LogInfo("TronSight2: 停止采集数据");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 停止采集异常 - {ex.Message}");
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            var result = new List<MeasureData>();

            try
            {
                lock (_dataLock)
                {
                    if (_instance == 0 || !TSCMCAPICS.isConnected(_instance))
                    {
                        return result;
                    }

                    bool isAcquiring = TSCMCAPICS.isAcquireData(_instance);
                    if (!isAcquiring)
                    {
                        // 停采后仍可能有缓冲区残留数据，继续按官方方式一次性取完。
                        Logs.LogWarning("TronSight2: 当前非采集态，尝试读取缓冲区残留数据");
                    }

                    int availableRecordCount = GetAvailableRecordCount();
                    if (availableRecordCount <= 0)
                    {
                        return result;
                    }

                    int[] outputSignals = GetOutputSignals();
                    int expectedNodeCount = availableRecordCount * outputSignals.Length;
                    var dataNodes = new API.DataNode[expectedNodeCount];
                    int nread = 0;

                    // 按官方 demo 的一次性读取方式，把当前缓冲区里的数据一次取完。
                    var ret = TSCMCAPICS.TransferAllDataNode(_instance, ref dataNodes[0], ref nread, expectedNodeCount);
                    if (ret == API.ERRCODE.NO_DATA_IN_BUFFER)
                    {
                        return result;
                    }

                    if (ret != API.ERRCODE.OK || nread <= 0)
                    {
                        Logs.LogWarning($"TronSight2: 读取缓冲区数据失败，错误码: {ret}");
                        return result;
                    }

                    BuildMeasureData(result, dataNodes, nread);
                    ApplySoftwareRecordLimit(result, availableRecordCount);

                    if (result.Count > 0)
                    {
                        LogReadResult(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 接收数据异常 - {ex.Message}");
            }

            return result;
        }

        public bool SaveConfigurationToSensor()
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("写入配置到传感器"))
                    {
                        return false;
                    }

                    if (!ApplyConfigurationParameters())
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.SetConfigControllerSettings(_instance, ControllerIndex);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 写入配置到传感器失败，错误码: {ret}");
                        return false;
                    }

                    Logs.LogInfo("TronSight2: 当前参数已写入传感器 Flash");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 写入配置到传感器异常 - {ex.Message}");
                return false;
            }
        }

        public bool LoadConfigurationFromSensor()
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("从传感器读取配置"))
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.GetConfigControllerSettings(_instance, ControllerIndex);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 读取控制器配置失败，错误码: {ret}");
                        return false;
                    }

                    if (!ReadConfigurationParameters())
                    {
                        return false;
                    }

                    Logs.LogInfo("TronSight2: 已从传感器读取配置并更新页面参数");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 从传感器读取配置异常 - {ex.Message}");
                return false;
            }
        }

        public bool ResetTriggerCounter()
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("触发复位"))
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.ResetTriggerCounter(_instance, ControllerIndex);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 触发复位失败，错误码: {ret}");
                        return false;
                    }

                    Logs.LogInfo("TronSight2: 已执行触发复位");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 触发复位异常 - {ex.Message}");
                return false;
            }
        }

        public bool SaveConfigurationToFile(string filePath)
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("保存配置到文件"))
                    {
                        return false;
                    }

                    if (!ApplyConfigurationParameters())
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.SaveControllerConfig(_instance, filePath);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 保存配置到文件失败，路径:{filePath}，错误码: {ret}");
                        return false;
                    }

                    Logs.LogInfo($"TronSight2: 配置已保存到文件 {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 保存配置到文件异常 - {ex.Message}");
                return false;
            }
        }

        public bool LoadConfigurationFromFile(string filePath)
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("从文件读取配置"))
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.ReadControllerConfig(_instance, filePath);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 从文件读取配置失败，路径:{filePath}，错误码: {ret}");
                        return false;
                    }

                    if (PersistConfigurationAfterImportOrRestore && !PersistControllerConfiguration())
                    {
                        return false;
                    }

                    if (!RefreshConfigurationParameters())
                    {
                        return false;
                    }

                    Logs.LogInfo($"TronSight2: 已从文件读取配置 {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 从文件读取配置异常 - {ex.Message}");
                return false;
            }
        }

        public bool RestoreFactoryConfiguration()
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("恢复出厂配置"))
                    {
                        return false;
                    }

                    var ret = TSCMCAPICS.RestoreFactorySetting(_instance);
                    if (ret != API.ERRCODE.OK)
                    {
                        Logs.LogError($"TronSight2: 恢复出厂配置失败，错误码: {ret}");
                        return false;
                    }

                    if (PersistConfigurationAfterImportOrRestore && !PersistControllerConfiguration())
                    {
                        return false;
                    }

                    if (!RefreshConfigurationParameters())
                    {
                        return false;
                    }

                    Logs.LogInfo("TronSight2: 已恢复出厂配置");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 恢复出厂配置异常 - {ex.Message}");
                return false;
            }
        }

        public string GetSuggestedConfigFileName()
        {
            try
            {
                lock (_dataLock)
                {
                    string baseName = string.Empty;
                    if (_instance != 0 && TSCMCAPICS.isConnected(_instance))
                    {
                        var serialNumber = new API.SerialNumber();
                        var ret = TSCMCAPICS.GetSensorSerialNumber(_instance, ControllerIndex, 1, ref serialNumber);
                        if (ret == API.ERRCODE.OK)
                        {
                            baseName = serialNumber.serial ?? string.Empty;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(baseName))
                    {
                        baseName = string.IsNullOrWhiteSpace(NickName) ? "TronSight2" : NickName;
                    }

                    return $"{SanitizeFileName(baseName)}-config.dat";
                }
            }
            catch
            {
                return "TronSight2-config.dat";
            }
        }

        // 折射率表最多 32 条，先生成固定容量空表，后续文件/控制器读写都基于这套索引。
        public static List<RefractiveIndexTableItem> CreateEmptyRefractiveIndexTableItems()
        {
            var items = new List<RefractiveIndexTableItem>(RefractiveTableMaxCount);
            for (int i = 1; i <= RefractiveTableMaxCount; i++)
            {
                items.Add(new RefractiveIndexTableItem(i));
            }

            return items;
        }

        // 文本表格式固定为: 材料名,1285nm,1310nm,1335nm；不足 32 条时自动补空行。
        public static List<RefractiveIndexTableItem> LoadRefractiveIndexTableFromFile(string filePath)
        {
            var items = CreateEmptyRefractiveIndexTableItems();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return items;
            }

            if (!File.Exists(filePath))
            {
                Logs.LogWarning($"TronSight2: 折射率表文件不存在 - {filePath}");
                return items;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                for (int i = 0; i < Math.Min(lines.Length, RefractiveTableMaxCount); i++)
                {
                    ApplyRefractiveLine(items[i], lines[i]);
                }

                Logs.LogInfo($"TronSight2: 已从硬盘读取折射率表 - {filePath}");
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 读取折射率表文件异常 - {ex.Message}");
            }

            return items;
        }

        public static bool SaveRefractiveIndexTableToFile(string filePath, IEnumerable<RefractiveIndexTableItem> tableItems)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Logs.LogError("TronSight2: 下载折射率表到硬盘失败，文件路径为空");
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = NormalizeRefractiveTableItems(tableItems)
                    .Select(BuildRefractiveLine)
                    .ToArray();

                File.WriteAllLines(filePath, lines);
                Logs.LogInfo($"TronSight2: 已将折射率表下载到硬盘 - {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 下载折射率表到硬盘异常 - {ex.Message}");
                return false;
            }
        }

        public bool LoadRefractiveIndexTableFromController(IList<RefractiveIndexTableItem> targetItems)
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("由控制器读取折射率表"))
                    {
                        return false;
                    }

                    var normalizedItems = NormalizeRefractiveTableItems(targetItems);
                    bool isSuccess = true;
                    for (int label = 1; label <= RefractiveTableMaxCount; label++)
                    {
                        var table = new API.RefractiveTable();
                        var ret = TSCMCAPICS.DownloadRefractiveTable(_instance, ControllerIndex, label, ref table);
                        if (ret == API.ERRCODE.OK)
                        {
                            ApplyRefractiveTable(normalizedItems[label - 1], table);
                        }
                        else
                        {
                            Logs.LogWarning($"TronSight2: 读取折射率表第{label}条失败，错误码: {ret}");
                            isSuccess = false;
                        }
                    }

                    int selectedLabel = GetControllerSelectedRefractiveLabel(DefaultSensorChannel);
                    ApplySelectedRefractiveLabel(normalizedItems, selectedLabel);
                    ReplaceRefractiveItems(targetItems, normalizedItems);
                    Logs.LogInfo("TronSight2: 已由控制器读取折射率表");
                    return isSuccess;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 由控制器读取折射率表异常 - {ex.Message}");
                return false;
            }
        }

        // 由上位机当前表格内容覆盖控制器 1-32 号折射率槽位。
        public bool UploadRefractiveIndexTableToController(IEnumerable<RefractiveIndexTableItem> tableItems)
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("上传折射率表到控制器"))
                    {
                        return false;
                    }

                    bool isSuccess = true;
                    foreach (RefractiveIndexTableItem item in NormalizeRefractiveTableItems(tableItems))
                    {
                        var table = new API.RefractiveTable
                        {
                            object_name = item.MaterialName ?? string.Empty,
                            refractive_data = new API.RefractiveCoeff
                            {
                                c486 = item.RefractiveIndex1285,
                                c587 = item.RefractiveIndex1310,
                                c656 = item.RefractiveIndex1335
                            }
                        };

                        var ret = TSCMCAPICS.UploadRefractiveTable(_instance, ControllerIndex, item.Label, table);
                        if (ret != API.ERRCODE.OK)
                        {
                            Logs.LogError($"TronSight2: 上传折射率表第{item.Label}条失败，错误码: {ret}");
                            isSuccess = false;
                        }
                    }

                    if (isSuccess)
                    {
                        Logs.LogInfo("TronSight2: 已上传折射率表到控制器");
                    }

                    return isSuccess;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 上传折射率表到控制器异常 - {ex.Message}");
                return false;
            }
        }

        // “使用选中折射率”需要同时切换当前表标签，并把指定层绑定到同一个折射率 tag。
        public bool UseSelectedRefractiveIndex(int sensorChannel, int layer, int label)
        {
            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("使用选中折射率"))
                    {
                        return false;
                    }

                    int normalizedSensorChannel = Math.Max(DefaultSensorChannel, sensorChannel);
                    int normalizedLayer = Math.Clamp(layer, RefractiveLayerMin, RefractiveLayerMax);
                    int normalizedLabel = Math.Clamp(label, RefractiveLayerMin, RefractiveTableMaxCount);

                    // 新固件把“当前折射率表”接口标成 deprecated，层级标签才是实际生效入口。
                    bool isSuccess = SetCurrentRefractiveLabelCompat(normalizedSensorChannel, normalizedLabel);

                    isSuccess &= ExecuteConfigCall(
                        TSCMCAPICS.SetThicknessRefractionTagByLayer(_instance, ControllerIndex, normalizedSensorChannel, normalizedLayer, normalizedLabel),
                        $"设置第{normalizedLayer}层折射率标签失败，目标值:{normalizedLabel}");

                    if (isSuccess)
                    {
                        Logs.LogInfo($"TronSight2: 已应用选中折射率 - 通道:CH{normalizedSensorChannel}, 层号:{normalizedLayer}, 标签:{normalizedLabel}");
                    }

                    return isSuccess;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 使用选中折射率异常 - {ex.Message}");
                return false;
            }
        }

        // 厚度修正页只需要当前层的材料和系数，别整一堆没必要的花活。
        public List<ThicknessCorrectionItem> LoadThicknessCorrectionItems(IEnumerable<RefractiveIndexTableItem> refractiveTableItems)
        {
            var result = new List<ThicknessCorrectionItem>();

            try
            {
                lock (_dataLock)
                {
                    if (!EnsureConnectedForConfiguration("读取厚度修正"))
                    {
                        return result;
                    }

                    var normalizedRefractiveItems = NormalizeRefractiveTableItems(refractiveTableItems);
                    int layerCount = IsMultiLayerMode() ? RefractiveLayerMax : RefractiveLayerMin;

                    for (int layer = RefractiveLayerMin; layer <= layerCount; layer++)
                    {
                        int label = GetLayerRefractiveLabel(DefaultSensorChannel, layer);
                        double factor = 1d;
                        TryGetThicknessCorrectionFactor(DefaultSensorChannel, layer, ref factor);

                        var item = new ThicknessCorrectionItem(layer)
                        {
                            RefractionTag = label,
                            CorrectionFactor = factor
                        };

                        ApplyThicknessRefractiveInfo(item, normalizedRefractiveItems, label);
                        result.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 读取厚度修正异常 - {ex.Message}");
            }

            return result;
        }

        public bool SaveThicknessCorrectionFactor(int sensorChannel, int layer, double factor)
        {
            try
            {
                lock (_dataLock)
                {



                    if (!EnsureConnectedForConfiguration("写入厚度修正"))
                    {
                        return false;
                    }

                    int normalizedSensorChannel = Math.Max(DefaultSensorChannel, sensorChannel);
                    int normalizedLayer = Math.Clamp(layer, RefractiveLayerMin, RefractiveLayerMax);
                    var ret = IsInterferenceThicknessMode()
                        ? TSCMCAPICS.SetInterfThicknessCorrectionByLayer(_instance, ControllerIndex, normalizedSensorChannel, normalizedLayer, factor)
                        : TSCMCAPICS.SetThicknessCorrectionByLayer(_instance, ControllerIndex, normalizedSensorChannel, normalizedLayer, factor);

                    return ExecuteConfigCall(ret, $"设置第{normalizedLayer}层厚度修正系数失败，目标值:{factor}");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"TronSight2: 写入厚度修正异常 - {ex.Message}");
                return false;
            }
        }

        private API.MEASUREMODE GetApiMeasureMode()
        {
            if (SelectedMeasureMode == API.MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER)
            {
                return API.MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER;
            }

            return API.MEASUREMODE.INTERF_THICKNESS_SINGLE_LAYER;
        }

        private bool IsMultiLayerMode()
        {
            return SelectedMeasureMode == API.MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER;
        }

        private bool IsInterferenceThicknessMode()
        {
            return SelectedMeasureMode == API.MEASUREMODE.INTERF_THICKNESS_SINGLE_LAYER
                || SelectedMeasureMode == API.MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER;
        }

        private int[] GetOutputSignals()
        {
            return IsMultiLayerMode() ? MultiLayerOutputSignals : SingleLayerOutputSignals;
        }

        // 让 SDK 环形缓冲区和官方软件的“存储点数”保持同一语义。
        private void EnsureRingBufferSize()
        {
            int currentSize = 0;
            var ret = TSCMCAPICS.RingBufferDataSize(_instance, ref currentSize);
            if (ret != API.ERRCODE.OK)
            {
                Logs.LogWarning($"TronSight2: 读取SDK存储点数失败，错误码: {ret}");
                return;
            }

            if (currentSize == SDK_RING_BUFFER_SIZE)
            {
                return;
            }

            ret = TSCMCAPICS.ResizeRingBuffer(_instance, SDK_RING_BUFFER_SIZE);
            if (ret != API.ERRCODE.OK)
            {
                Logs.LogWarning($"TronSight2: 设置SDK存储点数失败，当前:{currentSize}，目标:{SDK_RING_BUFFER_SIZE}，错误码:{ret}");
                return;
            }

            Logs.LogInfo($"TronSight2: SDK存储点数已调整为 {SDK_RING_BUFFER_SIZE}");
        }

        private int GetAvailableRecordCount()
        {
            int availableRecordCount = 0;
            var ret = TSCMCAPICS.RingBufferDataSize(_instance, ref availableRecordCount);
            if (ret != API.ERRCODE.OK)
            {
                Logs.LogWarning($"TronSight2: 读取缓冲区可用数据数量失败，错误码: {ret}");
                return 0;
            }

            Logs.LogInfo($"TronSight2: 当前缓冲区可用数据数量:{availableRecordCount}");
            return availableRecordCount;
        }

        // 按 TronSight 的方式保留 SDK 原始通道/类型，不再把输出强行映射成业务列。
        private void BuildMeasureData(List<MeasureData> result, API.DataNode[] dataNodes, int nread)
        {
            int dataPerGroup = ResolveDataPerGroup(dataNodes, nread);
            if (dataPerGroup <= 0)
            {
                return;
            }

            int completeNodeCount = nread - nread % dataPerGroup;
            if (completeNodeCount == 0)
            {
                completeNodeCount = nread;
            }

            for (int startIndex = 0; startIndex < completeNodeCount; startIndex += dataPerGroup)
            {
                MeasureData? measureData = BuildMeasureData(
                    dataNodes,
                    startIndex,
                    Math.Min(dataPerGroup, nread - startIndex));

                if (measureData != null)
                {
                    result.Add(measureData);
                }
            }
        }

        private static MeasureData? BuildMeasureData(API.DataNode[] dataBuffer, int startIndex, int length)
        {
            Dictionary<string, Dictionary<string, object>> originalDatas = new Dictionary<string, Dictionary<string, object>>();

            for (int i = startIndex; i < startIndex + length; i++)
            {
                API.DataNode node = dataBuffer[i];
                AddOriginalData(originalDatas, node);
            }

            if (originalDatas.Count == 0)
            {
                return null;
            }

            return new MeasureData
            {
                OriginalDatas = originalDatas
            };
        }

        private static void AddOriginalData(Dictionary<string, Dictionary<string, object>> originalDatas, API.DataNode node)
        {
            string channelName = GetOriginalDataChannelName(node.cfg.channel);
            if (!originalDatas.TryGetValue(channelName, out Dictionary<string, object>? typeValues))
            {
                typeValues = new Dictionary<string, object>();
                originalDatas[channelName] = typeValues;
            }

            typeValues[GetOriginalDataTypeName(node.cfg.channel, node.cfg.type)] = node.data;
        }

        private static string GetOriginalDataChannelName(int channel)
        {
            return channel == 0 ? "Controller" : $"Channel{channel}";
        }

        private static string GetOriginalDataTypeName(int channel, int type)
        {
            if (channel == 0 && Enum.IsDefined(typeof(API.CONTROLLER_OUTPUT_DATA), type))
            {
                return ((API.CONTROLLER_OUTPUT_DATA)type).ToString();
            }

            if (channel != 0 && Enum.IsDefined(typeof(API.SENSOR_OUTPUT_DATA), type))
            {
                return ((API.SENSOR_OUTPUT_DATA)type).ToString();
            }

            return type.ToString();
        }

        private static int ResolveDataPerGroup(API.DataNode[] dataBuffer, int nread)
        {
            if (nread <= 0)
            {
                return 0;
            }

            var firstCfg = dataBuffer[0].cfg;
            for (int i = 1; i < nread; i++)
            {
                var currentCfg = dataBuffer[i].cfg;
                if (currentCfg.channel == firstCfg.channel && currentCfg.type == firstCfg.type)
                {
                    return i;
                }
            }

            return nread;
        }

        // 某些情况下 SDK 侧的存储点数限制不会直接体现在返回条数上，这里软件侧再兜一次底。
        private void ApplySoftwareRecordLimit(List<MeasureData> result, int availableRecordCount)
        {
            if (SDK_RING_BUFFER_SIZE <= 0 || result.Count <= SDK_RING_BUFFER_SIZE)
            {
                return;
            }

            int removeCount = result.Count - SDK_RING_BUFFER_SIZE;
            result.RemoveRange(0, removeCount);
            Logs.LogWarning($"TronSight2: 缓冲区可用数据数量:{availableRecordCount}，软件侧按存储点数限制保留最近 {SDK_RING_BUFFER_SIZE} 组数据");
        }

        private void LogReadResult(List<MeasureData> result)
        {
            Logs.LogInfo($"TronSight2: 接收到{result.Count}组原始数据");
        }

        // 单层输出列顺序与官方软件单层导出一致：峰1高度/光强度/曝光时间/厚度
        private static readonly int[] SingleLayerOutputSignals = new int[]
        {
            (int)API.SENSOR_OUTPUT_DATA.PEAK1_HEIGHT,
            (int)API.SENSOR_OUTPUT_DATA.INTENSITY,
            (int)API.SENSOR_OUTPUT_DATA.EXPTIME,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS
        };
        // 多层输出列顺序按当前业务导出表头组织：峰1/峰2/光强/曝光/厚度1~5/峰3~5
        private static readonly int[] MultiLayerOutputSignals = new int[]
        {
            (int)API.SENSOR_OUTPUT_DATA.PEAK1_HEIGHT,
            (int)API.SENSOR_OUTPUT_DATA.PEAK2_HEIGHT,
            (int)API.SENSOR_OUTPUT_DATA.INTENSITY,
            (int)API.SENSOR_OUTPUT_DATA.EXPTIME,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS2,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS3,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS4,
            (int)API.SENSOR_OUTPUT_DATA.THICKNESS5,
            (int)API.SENSOR_OUTPUT_DATA.PEAK3_HEIGHT,
            (int)API.SENSOR_OUTPUT_DATA.PEAK4_HEIGHT,
            (int)API.SENSOR_OUTPUT_DATA.PEAK5_HEIGHT
        };

        private bool EnsureConnectedForConfiguration(string action)
        {
            if (_instance == 0 || !TSCMCAPICS.isConnected(_instance))
            {
                Logs.LogError($"TronSight2: {action}失败，设备未连接");
                State = HardwareState.NotConnected;
                return false;
            }

            return true;
        }

        private bool ApplyConfigurationParameters()
        {
            bool isSuccess = true;

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigMeasurementMode(_instance, ControllerIndex, GetApiMeasureMode()),
                $"设置测量模式失败，目标值:{SelectedMeasureMode}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetWarningHoldPoints(_instance, ControllerIndex, InvalidDataHoldPoints),
                $"设置无效数据保持失败，目标值:{InvalidDataHoldPoints}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigSamplingInterval(_instance, ControllerIndex, SelectedSamplingInterval),
                $"设置采样间隔失败，目标值:{SelectedSamplingInterval}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigMoveAvarage(_instance, ControllerIndex, SelectedMoveAverageWindow),
                $"设置滑动平均滤波窗口宽度失败，目标值:{SelectedMoveAverageWindow}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetMedianFilterWidth(_instance, ControllerIndex, SelectedMedianFilterWidth),
                $"设置中值滤波窗口宽度失败，目标值:{SelectedMedianFilterWidth}");

            isSuccess &= ApplyTriggerConfiguration();

            if (isSuccess)
            {
                Logs.LogInfo($"TronSight2: 参数已下发到设备 - 测量模式:{SelectedMeasureMode}, 无效数据保持:{InvalidDataHoldPoints}, 采样间隔:{SelectedSamplingInterval}, 滑动平均:{SelectedMoveAverageWindow}, 中值滤波:{SelectedMedianFilterWidth}, 触发模式:{SelectedTriggerSampleMode}, 采样使能电平:{SelectedSyncValidLevel}, 单次采样个数:{SinglePulseSampleCount}, 滤波宽度:{SelectedSyncFilterWidth}, 触发通道:{SelectedTriggerEncoderChannel}, 触发模式:{SelectedTriggerMode}, 触发方向:{SelectedTriggerDirection}, 追踪模式:{SelectedTriggerTrackMode}, 触发间隔:{TriggerInterval}");
            }

            return isSuccess;
        }

        private bool ReadConfigurationParameters()
        {
            var measureMode = SelectedMeasureMode;
            int invalidDataHoldPoints = InvalidDataHoldPoints;
            var samplingInterval = SelectedSamplingInterval;
            var moveAverageWindow = SelectedMoveAverageWindow;
            var medianFilterWidth = SelectedMedianFilterWidth;
            var externalTrigger = BuildExternalTriggerConfig();
            var triggerSetting = BuildTriggerSettingConfig();

            bool isSuccess = true;

            var measureModeRet = TSCMCAPICS.GetConfigMeasurementMode(_instance, ControllerIndex, ref measureMode);
            if (measureModeRet == API.ERRCODE.OK)
            {
                SelectedMeasureMode = measureMode;
            }
            else
            {
                Logs.LogError($"TronSight2: 读取测量模式失败，错误码: {measureModeRet}");
                isSuccess = false;
            }

            var invalidDataRet = TSCMCAPICS.GetWarningHoldPoints(_instance, ControllerIndex, ref invalidDataHoldPoints);
            if (invalidDataRet == API.ERRCODE.OK)
            {
                InvalidDataHoldPoints = invalidDataHoldPoints;
            }
            else
            {
                Logs.LogError($"TronSight2: 读取无效数据保持失败，错误码: {invalidDataRet}");
                isSuccess = false;
            }

            var samplingRet = TSCMCAPICS.GetConfigSamplingInterval(_instance, ControllerIndex, ref samplingInterval);
            if (samplingRet == API.ERRCODE.OK)
            {
                SelectedSamplingInterval = samplingInterval;
            }
            else
            {
                Logs.LogError($"TronSight2: 读取采样间隔失败，错误码: {samplingRet}");
                isSuccess = false;
            }

            var moveAverageRet = TSCMCAPICS.GetConfigMoveAvarage(_instance, ControllerIndex, ref moveAverageWindow);
            if (moveAverageRet == API.ERRCODE.OK)
            {
                SelectedMoveAverageWindow = moveAverageWindow;
            }
            else
            {
                Logs.LogError($"TronSight2: 读取滑动平均滤波窗口宽度失败，错误码: {moveAverageRet}");
                isSuccess = false;
            }

            var medianRet = TSCMCAPICS.GetMedianFilterWidth(_instance, ControllerIndex, ref medianFilterWidth);
            if (medianRet == API.ERRCODE.OK)
            {
                SelectedMedianFilterWidth = medianFilterWidth;
            }
            else
            {
                Logs.LogError($"TronSight2: 读取中值滤波窗口宽度失败，错误码: {medianRet}");
                isSuccess = false;
            }

            var externalTriggerRet = TSCMCAPICS.GetConfigExternalTrigger(_instance, ControllerIndex, ref externalTrigger);
            if (externalTriggerRet == API.ERRCODE.OK)
            {
                ApplyExternalTriggerConfig(externalTrigger);
            }
            else
            {
                Logs.LogError($"TronSight2: 读取触发配置失败，错误码: {externalTriggerRet}");
                isSuccess = false;
            }

            var triggerSettingRet = TSCMCAPICS.GetConfigTriggerSetting(_instance, ControllerIndex, ref triggerSetting);
            if (triggerSettingRet == API.ERRCODE.OK)
            {
                ApplyTriggerSettingConfig(triggerSetting);
            }
            else
            {
                Logs.LogWarning($"TronSight2: 读取编码器触发参数失败，错误码: {triggerSettingRet}，将保留当前默认值");
            }

            isSuccess &= ReadEncoderConfiguration();
            return isSuccess;
        }

        private bool RefreshConfigurationParameters()
        {
            var ret = TSCMCAPICS.GetConfigControllerSettings(_instance, ControllerIndex);
            if (ret != API.ERRCODE.OK)
            {
                Logs.LogError($"TronSight2: 刷新控制器配置失败，错误码: {ret}");
                return false;
            }

            return ReadConfigurationParameters();
        }

        private bool PersistControllerConfiguration()
        {
            var ret = TSCMCAPICS.SetConfigControllerSettings(_instance, ControllerIndex);
            if (ret != API.ERRCODE.OK)
            {
                Logs.LogError($"TronSight2: 固化参数失败，错误码: {ret}");
                return false;
            }

            Logs.LogInfo("TronSight2: 已将当前配置写入传感器 Flash");
            return true;
        }

        private bool ApplyTriggerConfiguration()
        {
            var externalTrigger = BuildExternalTriggerConfig();
            bool isSuccess = ExecuteConfigCall(
                TSCMCAPICS.SetConfigExternalTrigger(_instance, ControllerIndex, externalTrigger),
                $"设置触发采样模式失败，目标值:{SelectedTriggerSampleMode}");

            if (!isSuccess)
            {
                return false;
            }

            if (externalTrigger.trig_method != API.TRIG_METHOD.ENCODER)
            {
                return true;
            }

            return ApplyEncoderConfiguration();
        }

        private bool ApplyEncoderConfiguration()
        {
            var triggerSetting = BuildTriggerSettingConfig();
            bool isSuccess = ExecuteConfigCall(
                TSCMCAPICS.SetConfigTriggerSetting(_instance, ControllerIndex, triggerSetting),
                $"设置编码器触发参数失败，触发通道:{triggerSetting.channel}, 触发模式:{triggerSetting.mode}, 触发方向:{triggerSetting.direction}, 抽样系数:{triggerSetting.downsample_factor}");

            isSuccess &= ApplyEncoderChannelConfiguration(
                API.ENCODER_CHANNEL.CH1,
                Encoder1Enabled,
                BuildEncoderSetting(Encoder1FilterWidth, Encoder1InputMode, Encoder1OutputMode, Encoder1ZPhaseEnabled),
                Encoder1Resolution,
                Encoder1ManualPosition,
                Encoder1ZPhasePosition,
                "编码器1");

            isSuccess &= ApplyEncoderChannelConfiguration(
                API.ENCODER_CHANNEL.CH2,
                Encoder2Enabled,
                BuildEncoderSetting(Encoder2FilterWidth, Encoder2InputMode, Encoder2OutputMode, Encoder2ZPhaseEnabled),
                Encoder2Resolution,
                Encoder2ManualPosition,
                Encoder2ZPhasePosition,
                "编码器2");

            return isSuccess;
        }

        private API.ExternalTrigger BuildExternalTriggerConfig()
        {
            var externalTrigger = new API.ExternalTrigger
            {
                trig_method = API.TRIG_METHOD.NONE,
                sync_setting = new API.SyncSetting
                {
                    state = API.STATE.OFF,
                    input_mode = API.SYNC_INPUT_MODE.LEVEL,
                    valid_level = SelectedSyncValidLevel,
                    sample_per_trigger = (ushort)SinglePulseSampleCount,
                    filter_width = SelectedSyncFilterWidth
                }
            };

            switch (SelectedTriggerSampleMode)
            {
                case TriggerSampleMode.FixedInterval:
                    externalTrigger.trig_method = API.TRIG_METHOD.NONE;
                    externalTrigger.sync_setting.state = API.STATE.OFF;
                    break;

                case TriggerSampleMode.FixedIntervalWithSyncOutput:
                    externalTrigger.trig_method = API.TRIG_METHOD.NONE;
                    externalTrigger.sync_setting.state = API.STATE.ON;
                    externalTrigger.sync_setting.input_mode = API.SYNC_INPUT_MODE.LEVEL;
                    break;

                case TriggerSampleMode.EncoderTrigger:
                    externalTrigger.trig_method = API.TRIG_METHOD.ENCODER;
                    externalTrigger.sync_setting.state = API.STATE.OFF;
                    break;

                case TriggerSampleMode.EncoderTriggerWithSyncOutput:
                    externalTrigger.trig_method = API.TRIG_METHOD.ENCODER;
                    externalTrigger.sync_setting.state = API.STATE.ON;
                    externalTrigger.sync_setting.input_mode = API.SYNC_INPUT_MODE.LEVEL;
                    break;

                case TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints:
                    externalTrigger.trig_method = API.TRIG_METHOD.SYNCIN;
                    externalTrigger.sync_setting.state = API.STATE.ON;
                    externalTrigger.sync_setting.input_mode = API.SYNC_INPUT_MODE.EDGE;
                    break;
            }

            return externalTrigger;
        }

        private API.TriggerSetting BuildTriggerSettingConfig()
        {
            return new API.TriggerSetting
            {
                channel = (API.ENCODER_CHANNEL)(int)SelectedTriggerEncoderChannel,
                mode = SelectedTriggerMode,
                direction = SelectedTriggerDirection,
                track_mode = SelectedTriggerTrackMode,
                downsample_factor = Math.Max(1, TriggerInterval)
            };
        }

        private static API.EncoderSetting BuildEncoderSetting(
            API.ENCODER_FILTER_WIDTH filterWidth,
            API.ENCODER_INPUT_MODE inputMode,
            API.ENCODER_OUTPUT_MODE outputMode,
            bool zPhaseEnabled)
        {
            return new API.EncoderSetting
            {
                filter_width = filterWidth,
                input_mode = inputMode,
                output_mode = outputMode,
                z_phase = zPhaseEnabled
            };
        }

        private void ApplyExternalTriggerConfig(API.ExternalTrigger externalTrigger)
        {
            SelectedTriggerSampleMode = GetTriggerSampleMode(externalTrigger);
            SelectedSyncValidLevel = externalTrigger.sync_setting.valid_level;
            SelectedSyncFilterWidth = externalTrigger.sync_setting.filter_width;
            SinglePulseSampleCount = Math.Max(1, (int)externalTrigger.sync_setting.sample_per_trigger);
        }

        private void ApplyTriggerSettingConfig(API.TriggerSetting triggerSetting)
        {
            int channelValue = (int)triggerSetting.channel;
            if (channelValue < (int)EncoderTriggerChannel.Encoder1 || channelValue > (int)EncoderTriggerChannel.Encoder3)
            {
                channelValue = (int)EncoderTriggerChannel.Encoder1;
            }

            if (channelValue == (int)EncoderTriggerChannel.Encoder3)
            {
                IsZSignalMultiplexedToEncoder3 = true;
            }
            else
            {
                _isZSignalMultiplexedToEncoder3 = false;
                RaisePropertyChanged(nameof(IsZSignalMultiplexedToEncoder3));
                RaisePropertyChanged(nameof(IsEncoder1ZPhaseAvailable));
                RaisePropertyChanged(nameof(IsEncoder2ZPhaseAvailable));
            }

            SelectedTriggerEncoderChannel = (EncoderTriggerChannel)channelValue;
            SelectedTriggerMode = triggerSetting.mode;
            SelectedTriggerDirection = triggerSetting.direction;
            SelectedTriggerTrackMode = triggerSetting.track_mode;
            TriggerInterval = Math.Max(1, triggerSetting.downsample_factor);
        }

        private bool ApplyEncoderChannelConfiguration(
            API.ENCODER_CHANNEL channel,
            bool enabled,
            API.EncoderSetting encoderSetting,
            double resolution,
            double manualPosition,
            double zPhasePosition,
            string channelName)
        {
            bool isSuccess = ExecuteConfigCall(
                TSCMCAPICS.SetConfigEncoderCounterEnable(_instance, ControllerIndex, channel, enabled ? API.STATE.ON : API.STATE.OFF),
                $"设置{channelName}使能失败，目标值:{enabled}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigEncoderSetting(_instance, ControllerIndex, channel, encoderSetting),
                $"设置{channelName}运行参数失败，滤波:{encoderSetting.filter_width}, 输入模式:{encoderSetting.input_mode}, 解码模式:{encoderSetting.output_mode}, Z相使能:{encoderSetting.z_phase}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigEncoderResolution(_instance, ControllerIndex, channel, resolution),
                $"设置{channelName}脉冲比例系数失败，目标值:{resolution}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigEncoderPosition(_instance, ControllerIndex, channel, manualPosition),
                $"设置{channelName}手动置位失败，目标值:{manualPosition}");

            isSuccess &= ExecuteConfigCall(
                TSCMCAPICS.SetConfigZPhasePosition(_instance, ControllerIndex, channel, zPhasePosition),
                $"设置{channelName}Z相置位失败，目标值:{zPhasePosition}");

            return isSuccess;
        }

        private bool ReadEncoderConfiguration()
        {
            bool isSuccess = true;
            isSuccess &= ReadEncoderChannelConfiguration(API.ENCODER_CHANNEL.CH1, true, "编码器1");
            isSuccess &= ReadEncoderChannelConfiguration(API.ENCODER_CHANNEL.CH2, false, "编码器2");
            return isSuccess;
        }

        private bool ReadEncoderChannelConfiguration(API.ENCODER_CHANNEL channel, bool isEncoder1, string channelName)
        {
            var counterEnable = API.STATE.OFF;
            var encoderSetting = BuildEncoderSetting(
                isEncoder1 ? Encoder1FilterWidth : Encoder2FilterWidth,
                isEncoder1 ? Encoder1InputMode : Encoder2InputMode,
                isEncoder1 ? Encoder1OutputMode : Encoder2OutputMode,
                isEncoder1 ? Encoder1ZPhaseEnabled : Encoder2ZPhaseEnabled);
            double resolution = isEncoder1 ? Encoder1Resolution : Encoder2Resolution;
            double manualPosition = isEncoder1 ? Encoder1ManualPosition : Encoder2ManualPosition;
            double zPhasePosition = isEncoder1 ? Encoder1ZPhasePosition : Encoder2ZPhasePosition;
            bool isSuccess = true;

            var enableRet = TSCMCAPICS.GetConfigEncoderCounterEnable(_instance, ControllerIndex, channel, ref counterEnable);
            if (enableRet == API.ERRCODE.OK)
            {
                if (isEncoder1)
                {
                    Encoder1Enabled = counterEnable == API.STATE.ON;
                }
                else
                {
                    Encoder2Enabled = counterEnable == API.STATE.ON;
                }
            }
            else
            {
                Logs.LogError($"TronSight2: 读取{channelName}使能失败，错误码: {enableRet}");
                isSuccess = false;
            }

            var settingRet = TSCMCAPICS.GetConfigEncoderSetting(_instance, ControllerIndex, channel, ref encoderSetting);
            if (settingRet == API.ERRCODE.OK)
            {
                if (isEncoder1)
                {
                    Encoder1FilterWidth = encoderSetting.filter_width;
                    Encoder1InputMode = encoderSetting.input_mode;
                    Encoder1OutputMode = encoderSetting.output_mode;
                    Encoder1ZPhaseEnabled = encoderSetting.z_phase;
                }
                else
                {
                    Encoder2FilterWidth = encoderSetting.filter_width;
                    Encoder2InputMode = encoderSetting.input_mode;
                    Encoder2OutputMode = encoderSetting.output_mode;
                    Encoder2ZPhaseEnabled = encoderSetting.z_phase;
                }
            }
            else
            {
                Logs.LogError($"TronSight2: 读取{channelName}运行参数失败，错误码: {settingRet}");
                isSuccess = false;
            }

            var resolutionRet = TSCMCAPICS.GetConfigEncoderResolution(_instance, ControllerIndex, channel, ref resolution);
            if (resolutionRet == API.ERRCODE.OK)
            {
                if (isEncoder1)
                {
                    Encoder1Resolution = resolution;
                }
                else
                {
                    Encoder2Resolution = resolution;
                }
            }
            else
            {
                Logs.LogError($"TronSight2: 读取{channelName}脉冲比例系数失败，错误码: {resolutionRet}");
                isSuccess = false;
            }

            var positionRet = TSCMCAPICS.GetConfigEncoderPosition(_instance, ControllerIndex, channel, ref manualPosition);
            if (positionRet == API.ERRCODE.OK)
            {
                if (isEncoder1)
                {
                    Encoder1ManualPosition = manualPosition;
                }
                else
                {
                    Encoder2ManualPosition = manualPosition;
                }
            }
            else
            {
                Logs.LogError($"TronSight2: 读取{channelName}手动置位失败，错误码: {positionRet}");
                isSuccess = false;
            }

            var zPhaseRet = TSCMCAPICS.GetConfigZPhasePosition(_instance, ControllerIndex, channel, ref zPhasePosition);
            if (zPhaseRet == API.ERRCODE.OK)
            {
                if (isEncoder1)
                {
                    Encoder1ZPhasePosition = zPhasePosition;
                }
                else
                {
                    Encoder2ZPhasePosition = zPhasePosition;
                }
            }
            else
            {
                Logs.LogError($"TronSight2: 读取{channelName}Z相置位失败，错误码: {zPhaseRet}");
                isSuccess = false;
            }

            return isSuccess;
        }

        private static TriggerSampleMode GetTriggerSampleMode(API.ExternalTrigger externalTrigger)
        {
            if (externalTrigger.trig_method == API.TRIG_METHOD.SYNCIN)
            {
                return TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints;
            }

            if (externalTrigger.trig_method == API.TRIG_METHOD.ENCODER)
            {
                return externalTrigger.sync_setting.state == API.STATE.ON
                    ? TriggerSampleMode.EncoderTriggerWithSyncOutput
                    : TriggerSampleMode.EncoderTrigger;
            }

            return externalTrigger.sync_setting.state == API.STATE.ON
                ? TriggerSampleMode.FixedIntervalWithSyncOutput
                : TriggerSampleMode.FixedInterval;
        }

        private void RaiseTriggerUiPropertiesChanged()
        {
            RaisePropertyChanged(nameof(IsSyncValidLevelEnabled));
            RaisePropertyChanged(nameof(IsSinglePulseSampleCountEnabled));
            RaisePropertyChanged(nameof(IsSyncFilterWidthEnabled));
        }

        private static bool ExecuteConfigCall(API.ERRCODE ret, string message)
        {
            if (ret == API.ERRCODE.OK)
            {
                return true;
            }

            Logs.LogError($"TronSight2: {message}，错误码: {ret}");
            return false;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "TronSight2";
            }

            return string.Concat(fileName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        }

        private bool SetCurrentRefractiveLabelCompat(int sensorChannel, int label)
        {
            var ret = TSCMCAPICS.SetCurrentRefractiveTableLabel(_instance, ControllerIndex, sensorChannel, label);
            if (ret == API.ERRCODE.OK)
            {
                return true;
            }

            if (ret == API.ERRCODE.FUNCTION_DEPRECATED)
            {
                Logs.LogInfo($"TronSight2: 当前固件不再支持设置当前折射率标签，将仅写入层标签，目标值:{label}");
                return true;
            }

            Logs.LogError($"TronSight2: 设置当前折射率表失败，目标值:{label}，错误码: {ret}");
            return false;
        }

        private int GetControllerSelectedRefractiveLabel(int sensorChannel)
        {
            int label = 1;
            var ret = TSCMCAPICS.GetCurrentRefractiveTableLabel(_instance, ControllerIndex, Math.Max(DefaultSensorChannel, sensorChannel), ref label);
            if (ret != API.ERRCODE.OK)
            {
                // 新固件会把这个接口标成 deprecated，优先退到读取第 1 层标签。
                if (ret == API.ERRCODE.FUNCTION_DEPRECATED)
                {
                    int layerLabel = 1;
                    var layerRet = TSCMCAPICS.GetThicknessRefractionTagByLayer(_instance, ControllerIndex, Math.Max(DefaultSensorChannel, sensorChannel), RefractiveLayerMin, ref layerLabel);
                    if (layerRet == API.ERRCODE.OK)
                    {
                        Logs.LogInfo($"TronSight2: 当前固件不再支持读取当前折射率标签，已改为读取第{RefractiveLayerMin}层标签");
                        return Math.Clamp(layerLabel, RefractiveLayerMin, RefractiveTableMaxCount);
                    }

                    Logs.LogInfo("TronSight2: 当前固件不再支持读取当前折射率标签，且未读到层标签，回退使用第 1 条");
                    return 1;
                }

                Logs.LogWarning($"TronSight2: 读取当前折射率标签失败，错误码: {ret}");
                return 1;
            }

            return Math.Clamp(label, RefractiveLayerMin, RefractiveTableMaxCount);
        }

        private int GetLayerRefractiveLabel(int sensorChannel, int layer)
        {
            int label = 1;
            var ret = TSCMCAPICS.GetThicknessRefractionTagByLayer(_instance, ControllerIndex, Math.Max(DefaultSensorChannel, sensorChannel), Math.Clamp(layer, RefractiveLayerMin, RefractiveLayerMax), ref label);
            if (ret == API.ERRCODE.OK)
            {
                return Math.Clamp(label, RefractiveLayerMin, RefractiveTableMaxCount);
            }

            Logs.LogWarning($"TronSight2: 读取第{layer}层折射率标签失败，错误码: {ret}，回退使用当前选中折射率");
            return GetControllerSelectedRefractiveLabel(sensorChannel);
        }

        private bool TryGetThicknessCorrectionFactor(int sensorChannel, int layer, ref double factor)
        {
            int normalizedSensorChannel = Math.Max(DefaultSensorChannel, sensorChannel);
            int normalizedLayer = Math.Clamp(layer, RefractiveLayerMin, RefractiveLayerMax);
            var ret = IsInterferenceThicknessMode()
                ? TSCMCAPICS.GetInterfThicknessCorrectionByLayer(_instance, ControllerIndex, normalizedSensorChannel, normalizedLayer, ref factor)
                : TSCMCAPICS.GetThicknessCorrectionByLayer(_instance, ControllerIndex, normalizedSensorChannel, normalizedLayer, ref factor);

            if (ret == API.ERRCODE.OK)
            {
                return true;
            }

            Logs.LogWarning($"TronSight2: 读取第{normalizedLayer}层厚度修正系数失败，错误码: {ret}");
            return false;
        }

        private static void ApplyThicknessRefractiveInfo(ThicknessCorrectionItem targetItem, IList<RefractiveIndexTableItem> refractiveItems, int label)
        {
            RefractiveIndexTableItem sourceItem = refractiveItems.FirstOrDefault(item => item != null && item.Label == Math.Clamp(label, RefractiveLayerMin, RefractiveTableMaxCount));
            if (sourceItem == null)
            {
                return;
            }

            targetItem.MaterialName = sourceItem.MaterialName;
            targetItem.RefractiveIndex1285 = sourceItem.RefractiveIndex1285;
            targetItem.RefractiveIndex1310 = sourceItem.RefractiveIndex1310;
            targetItem.RefractiveIndex1335 = sourceItem.RefractiveIndex1335;
        }

        private static void ApplySelectedRefractiveLabel(IList<RefractiveIndexTableItem> items, int selectedLabel)
        {
            foreach (RefractiveIndexTableItem item in items)
            {
                item.IsSelected = item.Label == selectedLabel;
            }
        }

        private static void ReplaceRefractiveItems(IList<RefractiveIndexTableItem> targetItems, IList<RefractiveIndexTableItem> sourceItems)
        {
            int count = Math.Min(targetItems.Count, sourceItems.Count);
            for (int i = 0; i < count; i++)
            {
                targetItems[i].CopyFrom(sourceItems[i]);
            }
        }

        private static List<RefractiveIndexTableItem> NormalizeRefractiveTableItems(IEnumerable<RefractiveIndexTableItem> tableItems)
        {
            var normalizedItems = CreateEmptyRefractiveIndexTableItems();
            if (tableItems == null)
            {
                return normalizedItems;
            }

            foreach (RefractiveIndexTableItem item in tableItems.Take(RefractiveTableMaxCount))
            {
                if (item == null)
                {
                    continue;
                }

                int index = Math.Clamp(item.Label, RefractiveLayerMin, RefractiveTableMaxCount) - 1;
                normalizedItems[index].CopyFrom(item);
            }

            return normalizedItems;
        }

        private static string BuildRefractiveLine(RefractiveIndexTableItem item)
        {
            return string.Join(",",
                item.MaterialName ?? string.Empty,
                item.RefractiveIndex1285.ToString("0.00000", CultureInfo.InvariantCulture),
                item.RefractiveIndex1310.ToString("0.00000", CultureInfo.InvariantCulture),
                item.RefractiveIndex1335.ToString("0.00000", CultureInfo.InvariantCulture));
        }

        private static void ApplyRefractiveLine(RefractiveIndexTableItem item, string line)
        {
            if (item == null)
            {
                return;
            }

            string[] columns = (line ?? string.Empty).Split(',');
            item.MaterialName = columns.Length > 0 ? columns[0].Trim() : string.Empty;
            item.RefractiveIndex1285 = ParseRefractiveDouble(columns, 1);
            item.RefractiveIndex1310 = ParseRefractiveDouble(columns, 2);
            item.RefractiveIndex1335 = ParseRefractiveDouble(columns, 3);
            item.IsSelected = false;
        }

        private static void ApplyRefractiveTable(RefractiveIndexTableItem item, API.RefractiveTable table)
        {
            item.MaterialName = table.object_name ?? string.Empty;
            item.RefractiveIndex1285 = table.refractive_data.c486;
            item.RefractiveIndex1310 = table.refractive_data.c587;
            item.RefractiveIndex1335 = table.refractive_data.c656;
            item.IsSelected = false;
        }

        private static double ParseRefractiveDouble(IReadOnlyList<string> columns, int index)
        {
            if (columns.Count <= index)
            {
                return 0d;
            }

            return double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0d;
        }
        #endregion
    }

    public enum TriggerSampleMode
    {
        FixedInterval,
        FixedIntervalWithSyncOutput,
        EncoderTrigger,
        EncoderTriggerWithSyncOutput,
        SyncEdgeTriggerFixedIntervalNPoints
    }

    public enum EncoderTriggerChannel
    {
        Encoder1 = 1,
        Encoder2 = 2,
        Encoder3 = 3
    }

    [Serializable]
    public class RefractiveIndexTableItem : BindableBase
    {
        private int _label;
        private string _materialName = string.Empty;
        private double _refractiveIndex1285;
        private double _refractiveIndex1310;
        private double _refractiveIndex1335;
        private bool _isSelected;

        [JsonIgnore]
        public Action<RefractiveIndexTableItem> SelectedCallback { get; set; }

        public int Label
        {
            get { return _label; }
            set { _label = value; RaisePropertyChanged(); }
        }

        public string MaterialName
        {
            get { return _materialName; }
            set { _materialName = value ?? string.Empty; RaisePropertyChanged(); }
        }

        public double RefractiveIndex1285
        {
            get { return _refractiveIndex1285; }
            set { _refractiveIndex1285 = value; RaisePropertyChanged(); }
        }

        public double RefractiveIndex1310
        {
            get { return _refractiveIndex1310; }
            set { _refractiveIndex1310 = value; RaisePropertyChanged(); }
        }

        public double RefractiveIndex1335
        {
            get { return _refractiveIndex1335; }
            set { _refractiveIndex1335 = value; RaisePropertyChanged(); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                RaisePropertyChanged();
                if (value)
                {
                    SelectedCallback?.Invoke(this);
                }
            }
        }

        public RefractiveIndexTableItem()
        {
        }

        public RefractiveIndexTableItem(int label)
        {
            Label = label;
        }

        public void CopyFrom(RefractiveIndexTableItem source)
        {
            if (source == null)
            {
                return;
            }

            Label = source.Label;
            MaterialName = source.MaterialName;
            RefractiveIndex1285 = source.RefractiveIndex1285;
            RefractiveIndex1310 = source.RefractiveIndex1310;
            RefractiveIndex1335 = source.RefractiveIndex1335;
            IsSelected = source.IsSelected;
        }
    }
}

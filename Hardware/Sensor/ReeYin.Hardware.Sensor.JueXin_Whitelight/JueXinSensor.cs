﻿﻿using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.JueXin.API;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReeYin.Hardware.Sensor.JueXin
{
    public enum JueXinCollectMode
    {
        Spectrum,
        FFT,
        SpectrumFFT,
        Thickness,
        ThickEncoder
    }

    public class JueXinThicknessChartPoint
    {
        public JueXinThicknessChartPoint(double x, double thickness)
        {
            X = x;
            Thickness = thickness;
        }

        public double X { get; }

        public double Thickness { get; }
    }

    public class JueXinEncoderThicknessChartPoint
    {
        public JueXinEncoderThicknessChartPoint(double index, double thickness, double encoder1, double encoder2, double encoder3)
        {
            Index = index;
            Thickness = thickness;
            Encoder1 = encoder1;
            Encoder2 = encoder2;
            Encoder3 = encoder3;
        }

        public double Index { get; }

        public double Thickness { get; }

        public double Encoder1 { get; }

        public double Encoder2 { get; }

        public double Encoder3 { get; }
    }

    /// <summary>
    /// 觉芯白光干涉传感器实现类
    /// </summary>
    public class JueXinSensor : SensorBase
    {
        #region Fields
        private IntPtr _sdkHandle = IntPtr.Zero;
        private WliSdkCAPI.WliEventCallbackC _eventCallback;
        private ConcurrentQueue<MeasureData> _dataQueue = new ConcurrentQueue<MeasureData>();
        private readonly object _lockObj = new object();
        private readonly object _displayDataLock = new object();
        private const int DisplayPointCapacity = 5000;
        private ushort[] _latestSpectrum = Array.Empty<ushort>();
        private float[] _latestFft = Array.Empty<float>();
        private readonly List<JueXinThicknessChartPoint> _thicknessChartPoints = new List<JueXinThicknessChartPoint>();
        private readonly List<JueXinEncoderThicknessChartPoint> _encoderThicknessChartPoints = new List<JueXinEncoderThicknessChartPoint>();
        private readonly List<uint> _spectrumPeakHistory = new List<uint>();
        private int _chartPointIndex;
        private double _spectrumPeakTotal;
        private JueXinCollectMode _activeCollectMode = JueXinCollectMode.Thickness;
        #endregion

        #region Properties
        private int _sampleInterval = 10000;
        [JsonIgnore]
        public int SampleInterval
        {
            get { return _sampleInterval; }
            set { _sampleInterval = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(MeasurementRate)); }
        }

        private int _integrationTime = 20;
        [JsonIgnore]
        public int IntegrationTime
        {
            get { return _integrationTime; }
            set { _integrationTime = value; RaisePropertyChanged(); }
        }

        private int _timeout = 3000;
        [JsonIgnore]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public double MeasurementRate
        {
            get { return SampleInterval > 0 ? 1000000.0 / SampleInterval : 0; }
        }

        private int _exposureMode;
        [JsonIgnore]
        public int ExposureMode
        {
            get { return _exposureMode; }
            set { _exposureMode = value; RaisePropertyChanged(); }
        }

        private int _targetValue = 25000;
        [JsonIgnore]
        public int TargetValue
        {
            get { return _targetValue; }
            set { _targetValue = value; RaisePropertyChanged(); }
        }

        private int _spectrumType;
        [JsonIgnore]
        public int SpectrumType
        {
            get { return _spectrumType; }
            set { _spectrumType = value; RaisePropertyChanged(); }
        }

        private int _specWeakThresh = 9800;
        [JsonIgnore]
        public int SpecWeakThresh
        {
            get { return _specWeakThresh; }
            set { _specWeakThresh = value; RaisePropertyChanged(); }
        }

        private int _specSatThresh = 39000;
        [JsonIgnore]
        public int SpecSatThresh
        {
            get { return _specSatThresh; }
            set { _specSatThresh = value; RaisePropertyChanged(); }
        }

        private float _fftRangeLow = 10;
        [JsonIgnore]
        public float FFTRangeLow
        {
            get { return _fftRangeLow; }
            set { _fftRangeLow = value; RaisePropertyChanged(); }
        }

        private float _fftRangeHigh = 50;
        [JsonIgnore]
        public float FFTRangeHigh
        {
            get { return _fftRangeHigh; }
            set { _fftRangeHigh = value; RaisePropertyChanged(); }
        }

        private float _fftWeakThresh = 1;
        [JsonIgnore]
        public float FFTWeakThresh
        {
            get { return _fftWeakThresh; }
            set { _fftWeakThresh = value; RaisePropertyChanged(); }
        }

        private int _fftProcOption;
        [JsonIgnore]
        public int FFTProcOption
        {
            get { return _fftProcOption; }
            set { _fftProcOption = value; RaisePropertyChanged(); }
        }

        private int _fftAvrWin = 0;
        [JsonIgnore]
        public int FFTAvrWin
        {
            get { return _fftAvrWin; }
            set { _fftAvrWin = value; RaisePropertyChanged(); }
        }

        private int _fftAccWin = 0;
        [JsonIgnore]
        public int FFTAccWin
        {
            get { return _fftAccWin; }
            set { _fftAccWin = value; RaisePropertyChanged(); }
        }

        private int _filterType;
        [JsonIgnore]
        public int FilterType
        {
            get { return _filterType; }
            set { _filterType = value; RaisePropertyChanged(); }
        }

        private int _medianFilterWidth = 3;
        [JsonIgnore]
        public int MedianFilterWidth
        {
            get { return _medianFilterWidth; }
            set { _medianFilterWidth = value; RaisePropertyChanged(); }
        }

        private int _averageFilterWidth = 2;
        [JsonIgnore]
        public int AverageFilterWidth
        {
            get { return _averageFilterWidth; }
            set { _averageFilterWidth = value; RaisePropertyChanged(); }
        }

        private float _filterCoeff = 1;
        [JsonIgnore]
        public float FilterCoeff
        {
            get { return _filterCoeff; }
            set { _filterCoeff = value; RaisePropertyChanged(); }
        }

        private bool _errorDataHold = true;
        [JsonIgnore]
        public bool ErrorDataHold
        {
            get { return _errorDataHold; }
            set { _errorDataHold = value; RaisePropertyChanged(); }
        }

        private int _errorDataCount;
        [JsonIgnore]
        public int ErrorDataCount
        {
            get { return _errorDataCount; }
            set { _errorDataCount = value; RaisePropertyChanged(); }
        }

        private string _errorDataDisplayValue = "NaN";
        [JsonIgnore]
        public string ErrorDataDisplayValue
        {
            get { return _errorDataDisplayValue; }
            set { _errorDataDisplayValue = value; RaisePropertyChanged(); }
        }

        private bool _triggerEnabled;
        [JsonIgnore]
        public bool TriggerEnabled
        {
            get { return _triggerEnabled; }
            set { _triggerEnabled = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigSource _triggerSource = WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER1;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigSource TriggerSource
        {
            get { return _triggerSource; }
            set { _triggerSource = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigMode _triggerMode = WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_LOCATION;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigMode TriggerMode
        {
            get { return _triggerMode; }
            set { _triggerMode = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigLevel _triggerLevel = WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_RISE_EDGE;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigLevel TriggerLevel
        {
            get { return _triggerLevel; }
            set { _triggerLevel = value; RaisePropertyChanged(); }
        }

        private int _triggerNumber = 1;
        [JsonIgnore]
        public int TriggerNumber
        {
            get { return _triggerNumber; }
            set { _triggerNumber = value; RaisePropertyChanged(); }
        }

        private float _triggerDelayTime = 10;
        [JsonIgnore]
        public float TriggerDelayTime
        {
            get { return _triggerDelayTime; }
            set { _triggerDelayTime = value; RaisePropertyChanged(); }
        }

        private bool _syncEnabled;
        [JsonIgnore]
        public bool SyncEnabled
        {
            get { return _syncEnabled; }
            set { _syncEnabled = value; RaisePropertyChanged(); }
        }

        private int _syncSlaveMode;
        [JsonIgnore]
        public int SyncSlaveMode
        {
            get { return _syncSlaveMode; }
            set { _syncSlaveMode = value; RaisePropertyChanged(); }
        }

        private int _syncResistor;
        [JsonIgnore]
        public int SyncResistor
        {
            get { return _syncResistor; }
            set { _syncResistor = value; RaisePropertyChanged(); }
        }

        private bool _encoder1Enabled;
        [JsonIgnore]
        public bool Encoder1Enabled
        {
            get { return _encoder1Enabled; }
            set { _encoder1Enabled = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliEncoderInputMode _encoderInputMode = WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_TWO_PHASE;
        [JsonIgnore]
        public WliSdkCAPI.WliEncoderInputMode EncoderInputMode
        {
            get { return _encoderInputMode; }
            set { _encoderInputMode = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigDirection _encoderTrigDirection = WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_BIDIRECTION;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigDirection EncoderTrigDirection
        {
            get { return _encoderTrigDirection; }
            set { _encoderTrigDirection = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliDecodeMode _encoderDecodeMode = WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X4;
        [JsonIgnore]
        public WliSdkCAPI.WliDecodeMode EncoderDecodeMode
        {
            get { return _encoderDecodeMode; }
            set { _encoderDecodeMode = value; RaisePropertyChanged(); }
        }

        private int _encoderTrigCount = 1;
        [JsonIgnore]
        public int EncoderTrigCount
        {
            get { return _encoderTrigCount; }
            set { _encoderTrigCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder1TrigDistance)); }
        }

        private float _encoderLenPulseRatio = 0.01f;
        [JsonIgnore]
        public float EncoderLenPulseRatio
        {
            get { return _encoderLenPulseRatio; }
            set { _encoderLenPulseRatio = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder1TrigDistance)); }
        }

        [JsonIgnore]
        public float Encoder1TrigDistance
        {
            get { return EncoderTrigCount * EncoderLenPulseRatio; }
        }

        private bool _encoderZPhaseEnable;
        [JsonIgnore]
        public bool EncoderZPhaseEnable
        {
            get { return _encoderZPhaseEnable; }
            set { _encoderZPhaseEnable = value; RaisePropertyChanged(); }
        }

        private float _encoderZPhaseResetPosition;
        [JsonIgnore]
        public float EncoderZPhaseResetPosition
        {
            get { return _encoderZPhaseResetPosition; }
            set { _encoderZPhaseResetPosition = value; RaisePropertyChanged(); }
        }

        private float _encoderSetPosition;
        [JsonIgnore]
        public float EncoderSetPosition
        {
            get { return _encoderSetPosition; }
            set { _encoderSetPosition = value; RaisePropertyChanged(); }
        }

        private bool _encoder2Enabled;
        [JsonIgnore]
        public bool Encoder2Enabled
        {
            get { return _encoder2Enabled; }
            set { _encoder2Enabled = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliEncoderInputMode _encoder2InputMode = WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_TWO_PHASE;
        [JsonIgnore]
        public WliSdkCAPI.WliEncoderInputMode Encoder2InputMode
        {
            get { return _encoder2InputMode; }
            set { _encoder2InputMode = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigDirection _encoder2TrigDirection = WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_BIDIRECTION;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigDirection Encoder2TrigDirection
        {
            get { return _encoder2TrigDirection; }
            set { _encoder2TrigDirection = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliDecodeMode _encoder2DecodeMode = WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X4;
        [JsonIgnore]
        public WliSdkCAPI.WliDecodeMode Encoder2DecodeMode
        {
            get { return _encoder2DecodeMode; }
            set { _encoder2DecodeMode = value; RaisePropertyChanged(); }
        }

        private int _encoder2TrigCount = 100;
        [JsonIgnore]
        public int Encoder2TrigCount
        {
            get { return _encoder2TrigCount; }
            set { _encoder2TrigCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder2TrigDistance)); }
        }

        private float _encoder2LenPulseRatio = 0.0001f;
        [JsonIgnore]
        public float Encoder2LenPulseRatio
        {
            get { return _encoder2LenPulseRatio; }
            set { _encoder2LenPulseRatio = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder2TrigDistance)); }
        }

        [JsonIgnore]
        public float Encoder2TrigDistance
        {
            get { return Encoder2TrigCount * Encoder2LenPulseRatio; }
        }

        private bool _encoder2ZPhaseEnable;
        [JsonIgnore]
        public bool Encoder2ZPhaseEnable
        {
            get { return _encoder2ZPhaseEnable; }
            set { _encoder2ZPhaseEnable = value; RaisePropertyChanged(); }
        }

        private float _encoder2ZPhaseResetPosition;
        [JsonIgnore]
        public float Encoder2ZPhaseResetPosition
        {
            get { return _encoder2ZPhaseResetPosition; }
            set { _encoder2ZPhaseResetPosition = value; RaisePropertyChanged(); }
        }

        private float _encoder2SetPosition;
        [JsonIgnore]
        public float Encoder2SetPosition
        {
            get { return _encoder2SetPosition; }
            set { _encoder2SetPosition = value; RaisePropertyChanged(); }
        }

        private bool _encoder3Enabled;
        [JsonIgnore]
        public bool Encoder3Enabled
        {
            get { return _encoder3Enabled; }
            set { _encoder3Enabled = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliEncoderInputMode _encoder3InputMode = WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_TWO_PHASE;
        [JsonIgnore]
        public WliSdkCAPI.WliEncoderInputMode Encoder3InputMode
        {
            get { return _encoder3InputMode; }
            set { _encoder3InputMode = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliTrigDirection _encoder3TrigDirection = WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_BIDIRECTION;
        [JsonIgnore]
        public WliSdkCAPI.WliTrigDirection Encoder3TrigDirection
        {
            get { return _encoder3TrigDirection; }
            set { _encoder3TrigDirection = value; RaisePropertyChanged(); }
        }

        private WliSdkCAPI.WliDecodeMode _encoder3DecodeMode = WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X4;
        [JsonIgnore]
        public WliSdkCAPI.WliDecodeMode Encoder3DecodeMode
        {
            get { return _encoder3DecodeMode; }
            set { _encoder3DecodeMode = value; RaisePropertyChanged(); }
        }

        private int _encoder3TrigCount = 100;
        [JsonIgnore]
        public int Encoder3TrigCount
        {
            get { return _encoder3TrigCount; }
            set { _encoder3TrigCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder3TrigDistance)); }
        }

        private float _encoder3LenPulseRatio = 0.0001f;
        [JsonIgnore]
        public float Encoder3LenPulseRatio
        {
            get { return _encoder3LenPulseRatio; }
            set { _encoder3LenPulseRatio = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Encoder3TrigDistance)); }
        }

        [JsonIgnore]
        public float Encoder3TrigDistance
        {
            get { return Encoder3TrigCount * Encoder3LenPulseRatio; }
        }

        private bool _encoder3ZPhaseEnable;
        [JsonIgnore]
        public bool Encoder3ZPhaseEnable
        {
            get { return _encoder3ZPhaseEnable; }
            set { _encoder3ZPhaseEnable = value; RaisePropertyChanged(); }
        }

        private float _encoder3ZPhaseResetPosition;
        [JsonIgnore]
        public float Encoder3ZPhaseResetPosition
        {
            get { return _encoder3ZPhaseResetPosition; }
            set { _encoder3ZPhaseResetPosition = value; RaisePropertyChanged(); }
        }

        private float _encoder3SetPosition;
        [JsonIgnore]
        public float Encoder3SetPosition
        {
            get { return _encoder3SetPosition; }
            set { _encoder3SetPosition = value; RaisePropertyChanged(); }
        }

        private JueXinCollectMode _collectMode = JueXinCollectMode.Thickness;
        [JsonIgnore]
        public JueXinCollectMode CollectMode
        {
            get { return _collectMode; }
            set { _collectMode = value; RaisePropertyChanged(); }
        }

        private int _cachedDataCount;
        [JsonIgnore]
        public int CachedDataCount
        {
            get { return _cachedDataCount; }
            set { _cachedDataCount = value; RaisePropertyChanged(); }
        }

        private float _lastThickness;
        [JsonIgnore]
        public float LastThickness
        {
            get { return _lastThickness; }
            set { _lastThickness = value; RaisePropertyChanged(); }
        }

        private float _thicknessMin;
        [JsonIgnore]
        public float ThicknessMin
        {
            get { return _thicknessMin; }
            set { _thicknessMin = value; RaisePropertyChanged(); }
        }

        private float _thicknessMax;
        [JsonIgnore]
        public float ThicknessMax
        {
            get { return _thicknessMax; }
            set { _thicknessMax = value; RaisePropertyChanged(); }
        }

        private float _thicknessMean;
        [JsonIgnore]
        public float ThicknessMean
        {
            get { return _thicknessMean; }
            set { _thicknessMean = value; RaisePropertyChanged(); }
        }

        private float _lastEncoder1;
        [JsonIgnore]
        public float LastEncoder1
        {
            get { return _lastEncoder1; }
            set { _lastEncoder1 = value; RaisePropertyChanged(); }
        }

        private float _lastEncoder2;
        [JsonIgnore]
        public float LastEncoder2
        {
            get { return _lastEncoder2; }
            set { _lastEncoder2 = value; RaisePropertyChanged(); }
        }

        private float _lastEncoder3;
        [JsonIgnore]
        public float LastEncoder3
        {
            get { return _lastEncoder3; }
            set { _lastEncoder3 = value; RaisePropertyChanged(); }
        }

        private int _spectrumFrameCount;
        [JsonIgnore]
        public int SpectrumFrameCount
        {
            get { return _spectrumFrameCount; }
            set { _spectrumFrameCount = value; RaisePropertyChanged(); }
        }

        private int _lastSpectrumPeak;
        [JsonIgnore]
        public int LastSpectrumPeak
        {
            get { return _lastSpectrumPeak; }
            set { _lastSpectrumPeak = value; RaisePropertyChanged(); }
        }

        private double _averageSpectrumPeak;
        [JsonIgnore]
        public double AverageSpectrumPeak
        {
            get { return _averageSpectrumPeak; }
            set { _averageSpectrumPeak = value; RaisePropertyChanged(); }
        }

        private float _lastFftPeak;
        [JsonIgnore]
        public float LastFftPeak
        {
            get { return _lastFftPeak; }
            set { _lastFftPeak = value; RaisePropertyChanged(); }
        }

        private float _lastFftThickness;
        [JsonIgnore]
        public float LastFftThickness
        {
            get { return _lastFftThickness; }
            set { _lastFftThickness = value; RaisePropertyChanged(); }
        }

        private float _lastThickCoeff;
        [JsonIgnore]
        public float LastThickCoeff
        {
            get { return _lastThickCoeff; }
            set { _lastThickCoeff = value; RaisePropertyChanged(); }
        }

        private string _lastSdkMessage = string.Empty;
        [JsonIgnore]
        public string LastSdkMessage
        {
            get { return _lastSdkMessage; }
            set { _lastSdkMessage = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public JueXinSensor()
        {
            VenderName = "JueXin_Whitelight";
            VenderType = "WLI";
            IP = "192.168.1.130";
            Port = 8001;

            // 创建回调委托并保持引用防止被GC回收
            _eventCallback = OnEventCallback;
        }
        #endregion

        #region Override Methods
        public override bool Init()
        {
            bool deviceOpened = false;
            State = HardwareState.Initializing;

            try
            {
                if (_sdkHandle != IntPtr.Zero)
                {
                    ReleaseSdkHandle(IsConnected);
                    IsConnected = false;
                }

                // 创建SDK实例
                _sdkHandle = WliSdkCAPI.WliSdk_Create();
                if (_sdkHandle == IntPtr.Zero)
                {
                    Console.WriteLine("创建SDK实例失败");
                    State = HardwareState.Error;
                    return false;
                }

                // 设置以太网连接
                WliSdkCAPI.WliSdk_SetEthernet(_sdkHandle, IP, Port);

                // 设置事件回调
                WliSdkCAPI.WliSdk_SetEventCallback(_sdkHandle, _eventCallback);

                // WliSdk_OpenDevice 同步完成连接；WliSdk_SetTimeout 在打开后才生效，不能控制连接阶段等待。
                int ret = WliSdkCAPI.WliSdk_OpenDevice(_sdkHandle);
                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    Console.WriteLine($"打开设备失败，错误码: {ret}");
                    ReleaseSdkHandle(false);
                    State = HardwareState.Error;
                    return false;
                }
                deviceOpened = true;

                // 官方示例不把 SetTimeout 作为打开设备成功条件；失败只记录，连接继续。
                ret = WliSdkCAPI.WliSdk_SetTimeout(_sdkHandle, Timeout);
                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    Console.WriteLine($"设置超时失败，错误码: {ret}");
                }

                // 设置采样间隔
                ret = WliSdkCAPI.WliSdk_SetAttributeUInt32(_sdkHandle, "SampleInterval", (uint)SampleInterval);
                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    Console.WriteLine($"设置采样间隔失败，错误码: {ret}");
                    ReleaseSdkHandle(deviceOpened);
                    State = HardwareState.Error;
                    return false;
                }

                // 设置积分时间
                ret = WliSdkCAPI.WliSdk_SetAttributeUInt32(_sdkHandle, "IntegrationTime", (uint)IntegrationTime);
                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    Console.WriteLine($"设置积分时间失败，错误码: {ret}");
                    ReleaseSdkHandle(deviceOpened);
                    State = HardwareState.Error;
                    return false;
                }

                IsConnected = true;
                State = HardwareState.Ready;
                Console.WriteLine("觉芯传感器初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化异常: {ex.Message}");
                ReleaseSdkHandle(deviceOpened);
                IsConnected = false;
                State = HardwareState.Error;
                return false;
            }
        }

        public override void Close()
        {
            try
            {
                if (_sdkHandle == IntPtr.Zero)
                {
                    IsConnected = false;
                    State = HardwareState.Closed;
                    return;
                }

                // 停止数据传输
                StopCollect();

                ReleaseSdkHandle(true);

                IsConnected = false;
                State = HardwareState.Closed;
                Console.WriteLine("觉芯传感器已关闭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭异常: {ex.Message}");
                IsConnected = false;
                State = HardwareState.Error;
            }
        }

        public override void StartCollect()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法开始采集";
                Console.WriteLine(LastSdkMessage);
                return;
            }

            try
            {
                ClearDataQueue();

                int ret = StartTransfer(CollectMode);
                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    LastSdkMessage = $"开始采集失败，错误码: {ret}";
                    Console.WriteLine(LastSdkMessage);
                    State = HardwareState.Error;
                    return;
                }

                _activeCollectMode = CollectMode;
                State = HardwareState.Running;
                LastSdkMessage = $"开始{CollectMode}采集";
                Console.WriteLine(LastSdkMessage);
            }
            catch (Exception ex)
            {
                LastSdkMessage = $"开始采集异常: {ex.Message}";
                Console.WriteLine(LastSdkMessage);
                State = HardwareState.Error;
            }
        }

        public override void StopCollect()
        {
            if (!CheckConnection())
            {
                return;
            }

            try
            {
                int ret = StopTransfer(_activeCollectMode);
                if (ret == (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    State = HardwareState.Complete;
                    LastSdkMessage = "停止采集数据";
                    Console.WriteLine(LastSdkMessage);
                }
                else
                {
                    LastSdkMessage = $"停止采集失败，错误码: {ret}";
                    Console.WriteLine(LastSdkMessage);
                    State = HardwareState.Error;
                }
            }
            catch (Exception ex)
            {
                LastSdkMessage = $"停止采集异常: {ex.Message}";
                Console.WriteLine(LastSdkMessage);
                State = HardwareState.Error;
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            List<MeasureData> result = new List<MeasureData>();

            try
            {
                // 从队列中取出所有数据
                while (_dataQueue.TryDequeue(out MeasureData? data))
                {
                    result.Add(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收数据异常: {ex.Message}");
            }

            return result;
        }

        public override bool SettingParam(string key, object value)
        {
            if (!CheckConnection())
            {
                Console.WriteLine("设备未连接，无法设置参数");
                return false;
            }

            try
            {
                int ret = (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE;

                switch (key)
                {
                    case "SampleInterval":
                        ret = WliSdkCAPI.WliSdk_SetAttributeUInt32(_sdkHandle, "SampleInterval", Convert.ToUInt32(value));
                        if (ret == (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                            SampleInterval = Convert.ToInt32(value);
                        break;

                    case "IntegrationTime":
                        ret = WliSdkCAPI.WliSdk_SetAttributeUInt32(_sdkHandle, "IntegrationTime", Convert.ToUInt32(value));
                        if (ret == (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                            IntegrationTime = Convert.ToInt32(value);
                        break;

                    case "Timeout":
                        // 运行中只更新 SDK 通信超时；连接超时不走这里。
                        ret = WliSdkCAPI.WliSdk_SetTimeout(_sdkHandle, Convert.ToInt32(value));
                        if (ret == (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                            Timeout = Convert.ToInt32(value);
                        break;

                    default:
                        Console.WriteLine($"未知参数: {key}");
                        return false;
                }

                if (ret != (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
                {
                    Console.WriteLine($"设置参数 {key} 失败，错误码: {ret}");
                    return false;
                }

                LastSdkMessage = $"设置参数 {key} = {value} 成功";
                Console.WriteLine(LastSdkMessage);
                return true;
            }
            catch (Exception ex)
            {
                LastSdkMessage = $"设置参数异常: {ex.Message}";
                Console.WriteLine(LastSdkMessage);
                return false;
            }
        }

        public bool ApplyBasicParams()
        {
            return SettingParam("SampleInterval", SampleInterval)
                && SettingParam("IntegrationTime", IntegrationTime)
                && SettingParam("Timeout", Timeout);
        }

        public bool ApplySpectrumParams()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置光谱频谱参数";
                return false;
            }

            if (!WriteUInt32Attribute("ExpoMode", ExposureMode)) return false;
            if (!WriteUInt32Attribute("IntegrationTime", IntegrationTime)) return false;
            if (!WriteUInt32Attribute("TargetValue", TargetValue)) return false;
            if (!WriteUInt32Attribute("SpectrumType", SpectrumType)) return false;
            if (!WriteUInt32Attribute("SpecWeakThresh", SpecWeakThresh)) return false;
            if (!WriteUInt32Attribute("SpecSatThresh", SpecSatThresh)) return false;

            int ret = WliSdkCAPI.WliSdk_SetFFTRange(_sdkHandle, FFTRangeLow, FFTRangeHigh);
            if (!CheckSdkResult(ret, "设置频谱寻峰范围")) return false;

            if (!WriteFloatAttribute("FFTWeakThresh", FFTWeakThresh)) return false;
            if (!WriteUInt32Attribute("FFTProcOption", FFTProcOption)) return false;
            if (!WriteUInt32Attribute("FFTAvrWin", FFTAvrWin)) return false;
            if (!WriteUInt32Attribute("FFTAccWin", FFTAccWin)) return false;

            LastSdkMessage = "设置光谱频谱参数成功";
            return true;
        }

        public bool ApplyThicknessParams()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置厚度参数";
                return false;
            }

            if (!WriteUInt32Attribute("SampleInterval", SampleInterval)) return false;

            int filterWidth = FilterType == 2 ? AverageFilterWidth : MedianFilterWidth;
            int ret = WliSdkCAPI.WliSdk_SetThicknessFilter(_sdkHandle, FilterType, filterWidth);
            if (!CheckSdkResult(ret, "设置厚度滤波")) return false;

            if (!WriteFloatAttribute("FilterCoeff", FilterCoeff)) return false;

            ret = WliSdkCAPI.WliSdk_SetErrorData(_sdkHandle, ErrorDataHold ? 1 : 0, ErrorDataCount);
            if (!CheckSdkResult(ret, "设置异常数据保持")) return false;

            LastSdkMessage = "设置厚度参数成功";
            return true;
        }

        public bool LoadDeviceParams()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法读取参数";
                return false;
            }

            if (!ReadUInt32Attribute("SampleInterval", out uint sampleInterval)) return false;
            SampleInterval = Convert.ToInt32(sampleInterval);

            if (!ReadUInt32Attribute("IntegrationTime", out uint integrationTime)) return false;
            IntegrationTime = Convert.ToInt32(integrationTime);

            if (!ReadUInt32Attribute("ExpoMode", out uint exposureMode)) return false;
            ExposureMode = Convert.ToInt32(exposureMode);

            if (!ReadUInt32Attribute("TargetValue", out uint targetValue)) return false;
            TargetValue = Convert.ToInt32(targetValue);

            if (!ReadUInt32Attribute("SpectrumType", out uint spectrumType)) return false;
            SpectrumType = Convert.ToInt32(spectrumType);

            if (!ReadUInt32Attribute("SpecWeakThresh", out uint specWeakThresh)) return false;
            SpecWeakThresh = Convert.ToInt32(specWeakThresh);

            if (!ReadUInt32Attribute("SpecSatThresh", out uint specSatThresh)) return false;
            SpecSatThresh = Convert.ToInt32(specSatThresh);

            int ret = WliSdkCAPI.WliSdk_GetFFTRange(_sdkHandle, out float fftLow, out float fftHigh);
            if (!CheckSdkResult(ret, "读取频谱寻峰范围")) return false;
            FFTRangeLow = fftLow;
            FFTRangeHigh = fftHigh;

            if (!ReadFloatAttribute("FFTWeakThresh", out float fftWeakThresh)) return false;
            FFTWeakThresh = fftWeakThresh;

            if (!ReadUInt32Attribute("FFTProcOption", out uint fftProcOption)) return false;
            FFTProcOption = Convert.ToInt32(fftProcOption);

            if (!ReadUInt32Attribute("FFTAvrWin", out uint fftAvrWin)) return false;
            FFTAvrWin = Convert.ToInt32(fftAvrWin);

            if (!ReadUInt32Attribute("FFTAccWin", out uint fftAccWin)) return false;
            FFTAccWin = Convert.ToInt32(fftAccWin);

            ret = WliSdkCAPI.WliSdk_GetThicknessFilter(_sdkHandle, out int filterType, out int filterWidth);
            if (!CheckSdkResult(ret, "读取厚度滤波")) return false;
            FilterType = filterType;
            if (filterType == 2)
                AverageFilterWidth = filterWidth;
            else
                MedianFilterWidth = filterWidth;

            if (!ReadFloatAttribute("FilterCoeff", out float filterCoeff)) return false;
            FilterCoeff = filterCoeff;

            ret = WliSdkCAPI.WliSdk_GetErrorData(_sdkHandle, out int errorOn, out int errorCount);
            if (!CheckSdkResult(ret, "读取异常数据保持")) return false;
            ErrorDataHold = errorOn != 0;
            ErrorDataCount = errorCount;

            ret = WliSdkCAPI.WliSdk_GetTrigEnabled(_sdkHandle, out int triggerOn);
            if (!CheckSdkResult(ret, "读取触发使能")) return false;
            TriggerEnabled = triggerOn != 0;

            WliSdkCAPI.WliTrigConfigC triggerConfig = new WliSdkCAPI.WliTrigConfigC();
            ret = WliSdkCAPI.WliSdk_GetTrigConfig(_sdkHandle, ref triggerConfig);
            if (!CheckSdkResult(ret, "读取触发参数")) return false;
            TriggerSource = triggerConfig.trigSource;
            TriggerMode = triggerConfig.trigMode;
            TriggerLevel = triggerConfig.trigLevel;
            TriggerNumber = triggerConfig.trigNumber;
            TriggerDelayTime = triggerConfig.delayTime;

            ret = WliSdkCAPI.WliSdk_GetSyncEnabled(_sdkHandle, out int syncOn);
            if (!CheckSdkResult(ret, "读取同步使能")) return false;
            SyncEnabled = syncOn != 0;

            ret = WliSdkCAPI.WliSdk_GetSyncConfig(_sdkHandle, out int syncSlave, out int syncRes);
            if (!CheckSdkResult(ret, "读取同步参数")) return false;
            SyncSlaveMode = syncSlave;
            SyncResistor = syncRes;

            if (!LoadEncoderConfig(1)) return false;
            if (!LoadEncoderConfig(2)) return false;
            if (!LoadEncoderConfig(3)) return false;

            LastSdkMessage = "读取设备参数成功";
            return true;
        }

        public bool ApplyTriggerConfig()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置触发参数";
                return false;
            }

            int ret = WliSdkCAPI.WliSdk_SetTrigEnabled(_sdkHandle, TriggerEnabled ? 1 : 0);
            if (!CheckSdkResult(ret, "设置触发使能")) return false;

            WliSdkCAPI.WliTrigConfigC config = new WliSdkCAPI.WliTrigConfigC
            {
                trigSource = TriggerSource,
                trigMode = TriggerMode,
                trigLevel = TriggerLevel,
                trigNumber = TriggerNumber,
                delayTime = TriggerDelayTime
            };
            ret = WliSdkCAPI.WliSdk_SetTrigConfig(_sdkHandle, ref config);
            if (!CheckSdkResult(ret, "设置触发参数")) return false;

            LastSdkMessage = "设置触发参数成功";
            return true;
        }

        public bool ApplySyncConfig()
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置同步参数";
                return false;
            }

            int ret = WliSdkCAPI.WliSdk_SetSyncEnabled(_sdkHandle, SyncEnabled ? 1 : 0);
            if (!CheckSdkResult(ret, "设置同步使能")) return false;

            ret = WliSdkCAPI.WliSdk_SetSyncConfig(_sdkHandle, SyncSlaveMode, SyncResistor);
            if (!CheckSdkResult(ret, "设置同步参数")) return false;

            LastSdkMessage = "设置同步参数成功";
            return true;
        }

        public bool ApplyEncoder1Config()
        {
            return ApplyEncoderConfig(1);
        }

        public bool ApplyEncoderConfig(int encoderId)
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置编码器参数";
                return false;
            }

            int ret = WliSdkCAPI.WliSdk_SetEncoderEnabled(_sdkHandle, encoderId, GetEncoderEnabled(encoderId) ? 1 : 0);
            if (!CheckSdkResult(ret, $"设置编码器{encoderId}使能")) return false;

            WliSdkCAPI.WliEncoderConfigC config = BuildEncoderConfig(encoderId);
            ret = WliSdkCAPI.WliSdk_SetEncoderConfig(_sdkHandle, encoderId, ref config);
            if (!CheckSdkResult(ret, $"设置编码器{encoderId}参数")) return false;

            LastSdkMessage = $"设置编码器{encoderId}参数成功";
            return true;
        }

        public bool SetEncoder1Position()
        {
            return SetEncoderPosition(1);
        }

        public bool SetEncoderPosition(int encoderId)
        {
            if (!CheckConnection())
            {
                LastSdkMessage = "设备未连接，无法设置编码器位置";
                return false;
            }

            int ret = WliSdkCAPI.WliSdk_SetEncoderPosition(_sdkHandle, encoderId, GetEncoderSetPosition(encoderId));
            if (!CheckSdkResult(ret, $"设置编码器{encoderId}位置")) return false;

            LastSdkMessage = $"设置编码器{encoderId}位置成功";
            return true;
        }

        public bool ExecuteSdkCommand(string command, string action)
        {
            if (!CheckConnection())
            {
                LastSdkMessage = $"设备未连接，无法{action}";
                return false;
            }

            int ret = WliSdkCAPI.WliSdk_Execute(_sdkHandle, command);
            if (!CheckSdkResult(ret, action)) return false;

            LastSdkMessage = $"{action}成功";
            return true;
        }

        public void ClearDataQueue()
        {
            while (_dataQueue.TryDequeue(out _)) { }
            CachedDataCount = 0;
            LastThickness = 0;
            ThicknessMin = 0;
            ThicknessMax = 0;
            ThicknessMean = 0;
            LastEncoder1 = 0;
            LastEncoder2 = 0;
            LastEncoder3 = 0;
            LastSpectrumPeak = 0;
            AverageSpectrumPeak = 0;
            SpectrumFrameCount = 0;
            LastFftPeak = 0;
            LastFftThickness = 0;
            LastThickCoeff = 0;
            _spectrumPeakTotal = 0;

            lock (_displayDataLock)
            {
                _latestSpectrum = Array.Empty<ushort>();
                _latestFft = Array.Empty<float>();
                _thicknessChartPoints.Clear();
                _encoderThicknessChartPoints.Clear();
                _spectrumPeakHistory.Clear();
                _chartPointIndex = 0;
            }
        }

        public double GetAverageSpectrumPeak(int frameCount)
        {
            lock (_displayDataLock)
            {
                if (_spectrumPeakHistory.Count == 0)
                {
                    return 0;
                }

                int count = Math.Max(1, Math.Min(frameCount, _spectrumPeakHistory.Count));
                double total = 0;
                for (int i = _spectrumPeakHistory.Count - count; i < _spectrumPeakHistory.Count; i++)
                {
                    total += _spectrumPeakHistory[i];
                }

                return total / count;
            }
        }

        public ushort[] GetLatestSpectrumSnapshot()
        {
            lock (_displayDataLock)
            {
                return (ushort[])_latestSpectrum.Clone();
            }
        }

        public float[] GetLatestFftSnapshot()
        {
            lock (_displayDataLock)
            {
                return (float[])_latestFft.Clone();
            }
        }

        public List<JueXinThicknessChartPoint> GetThicknessChartPointsSnapshot()
        {
            lock (_displayDataLock)
            {
                return new List<JueXinThicknessChartPoint>(_thicknessChartPoints);
            }
        }

        public List<JueXinEncoderThicknessChartPoint> GetEncoderThicknessChartPointsSnapshot()
        {
            lock (_displayDataLock)
            {
                return new List<JueXinEncoderThicknessChartPoint>(_encoderThicknessChartPoints);
            }
        }

        #endregion

        #region Private Methods
        private bool CheckConnection()
        {
            return _sdkHandle != IntPtr.Zero && IsConnected;
        }

        private int StartTransfer(JueXinCollectMode mode)
        {
            switch (mode)
            {
                case JueXinCollectMode.Spectrum:
                    return WliSdkCAPI.WliSdk_StartSpectrumTransfer(_sdkHandle);
                case JueXinCollectMode.FFT:
                    return WliSdkCAPI.WliSdk_StartFFTTransfer(_sdkHandle);
                case JueXinCollectMode.SpectrumFFT:
                    return WliSdkCAPI.WliSdk_StartSpectrumFFTTransfer(_sdkHandle);
                case JueXinCollectMode.ThickEncoder:
                    return WliSdkCAPI.WliSdk_StartThickEncoderTransfer(_sdkHandle);
                default:
                    return WliSdkCAPI.WliSdk_StartThicknessTransfer(_sdkHandle);
            }
        }

        private int StopTransfer(JueXinCollectMode mode)
        {
            switch (mode)
            {
                case JueXinCollectMode.Spectrum:
                    return WliSdkCAPI.WliSdk_StopSpectrumTransfer(_sdkHandle);
                case JueXinCollectMode.FFT:
                    return WliSdkCAPI.WliSdk_StopFFTTransfer(_sdkHandle);
                case JueXinCollectMode.SpectrumFFT:
                    return WliSdkCAPI.WliSdk_StopSpectrumFFTTransfer(_sdkHandle);
                case JueXinCollectMode.ThickEncoder:
                    return WliSdkCAPI.WliSdk_StopThickEncoderTransfer(_sdkHandle);
                default:
                    return WliSdkCAPI.WliSdk_StopThicknessTransfer(_sdkHandle);
            }
        }

        private bool ReadUInt32Attribute(string name, out uint value)
        {
            int ret = WliSdkCAPI.WliSdk_GetAttributeUInt32(_sdkHandle, name, out value);
            return CheckSdkResult(ret, $"读取{name}");
        }

        private bool ReadFloatAttribute(string name, out float value)
        {
            int ret = WliSdkCAPI.WliSdk_GetAttributeFloat32(_sdkHandle, name, out value);
            return CheckSdkResult(ret, $"读取{name}");
        }

        private bool WriteUInt32Attribute(string name, int value)
        {
            int ret = WliSdkCAPI.WliSdk_SetAttributeUInt32(_sdkHandle, name, Convert.ToUInt32(value));
            return CheckSdkResult(ret, $"设置{name}");
        }

        private bool WriteFloatAttribute(string name, float value)
        {
            int ret = WliSdkCAPI.WliSdk_SetAttributeFloat32(_sdkHandle, name, value);
            return CheckSdkResult(ret, $"设置{name}");
        }

        private bool LoadEncoderConfig(int encoderId)
        {
            int ret = WliSdkCAPI.WliSdk_GetEncoderEnabled(_sdkHandle, encoderId, out int encoderOn);
            if (!CheckSdkResult(ret, $"读取编码器{encoderId}使能")) return false;

            WliSdkCAPI.WliEncoderConfigC config = new WliSdkCAPI.WliEncoderConfigC();
            ret = WliSdkCAPI.WliSdk_GetEncoderConfig(_sdkHandle, encoderId, ref config);
            if (!CheckSdkResult(ret, $"读取编码器{encoderId}参数")) return false;

            SetEncoderProperties(encoderId, encoderOn != 0, config);
            return true;
        }

        private bool GetEncoderEnabled(int encoderId)
        {
            return encoderId switch
            {
                2 => Encoder2Enabled,
                3 => Encoder3Enabled,
                _ => Encoder1Enabled
            };
        }

        private float GetEncoderSetPosition(int encoderId)
        {
            return encoderId switch
            {
                2 => Encoder2SetPosition,
                3 => Encoder3SetPosition,
                _ => EncoderSetPosition
            };
        }

        private WliSdkCAPI.WliEncoderConfigC BuildEncoderConfig(int encoderId)
        {
            return encoderId switch
            {
                2 => new WliSdkCAPI.WliEncoderConfigC
                {
                    inputMode = Encoder2InputMode,
                    trigDirection = Encoder2TrigDirection,
                    decodeMode = Encoder2DecodeMode,
                    trigCount = Encoder2TrigCount,
                    lenPulseRatio = Encoder2LenPulseRatio,
                    zPhaseEnable = Encoder2ZPhaseEnable ? 1 : 0,
                    zPhaseResetPosition = Encoder2ZPhaseResetPosition
                },
                3 => new WliSdkCAPI.WliEncoderConfigC
                {
                    inputMode = Encoder3InputMode,
                    trigDirection = Encoder3TrigDirection,
                    decodeMode = Encoder3DecodeMode,
                    trigCount = Encoder3TrigCount,
                    lenPulseRatio = Encoder3LenPulseRatio,
                    zPhaseEnable = Encoder3ZPhaseEnable ? 1 : 0,
                    zPhaseResetPosition = Encoder3ZPhaseResetPosition
                },
                _ => new WliSdkCAPI.WliEncoderConfigC
                {
                    inputMode = EncoderInputMode,
                    trigDirection = EncoderTrigDirection,
                    decodeMode = EncoderDecodeMode,
                    trigCount = EncoderTrigCount,
                    lenPulseRatio = EncoderLenPulseRatio,
                    zPhaseEnable = EncoderZPhaseEnable ? 1 : 0,
                    zPhaseResetPosition = EncoderZPhaseResetPosition
                }
            };
        }

        private void SetEncoderProperties(int encoderId, bool enabled, WliSdkCAPI.WliEncoderConfigC config)
        {
            switch (encoderId)
            {
                case 2:
                    Encoder2Enabled = enabled;
                    Encoder2InputMode = config.inputMode;
                    Encoder2TrigDirection = config.trigDirection;
                    Encoder2DecodeMode = config.decodeMode;
                    Encoder2TrigCount = config.trigCount;
                    Encoder2LenPulseRatio = config.lenPulseRatio;
                    Encoder2ZPhaseEnable = config.zPhaseEnable != 0;
                    Encoder2ZPhaseResetPosition = config.zPhaseResetPosition;
                    break;

                case 3:
                    Encoder3Enabled = enabled;
                    Encoder3InputMode = config.inputMode;
                    Encoder3TrigDirection = config.trigDirection;
                    Encoder3DecodeMode = config.decodeMode;
                    Encoder3TrigCount = config.trigCount;
                    Encoder3LenPulseRatio = config.lenPulseRatio;
                    Encoder3ZPhaseEnable = config.zPhaseEnable != 0;
                    Encoder3ZPhaseResetPosition = config.zPhaseResetPosition;
                    break;

                default:
                    Encoder1Enabled = enabled;
                    EncoderInputMode = config.inputMode;
                    EncoderTrigDirection = config.trigDirection;
                    EncoderDecodeMode = config.decodeMode;
                    EncoderTrigCount = config.trigCount;
                    EncoderLenPulseRatio = config.lenPulseRatio;
                    EncoderZPhaseEnable = config.zPhaseEnable != 0;
                    EncoderZPhaseResetPosition = config.zPhaseResetPosition;
                    break;
            }
        }

        private bool CheckSdkResult(int ret, string action)
        {
            if (ret == (int)WliSdkCAPI.WliErrorCode.WLI_ERR_NONE)
            {
                return true;
            }

            LastSdkMessage = $"{action}失败，错误码: {ret}";
            Console.WriteLine(LastSdkMessage);
            State = HardwareState.Error;
            return false;
        }

        private void ReleaseSdkHandle(bool closeDevice)
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return;
            }

            IntPtr sdkHandle = _sdkHandle;
            _sdkHandle = IntPtr.Zero;

            if (closeDevice)
            {
                try
                {
                    WliSdkCAPI.WliSdk_CloseDevice(sdkHandle);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭设备异常: {ex.Message}");
                }
            }

            try
            {
                WliSdkCAPI.WliSdk_Destroy(sdkHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"销毁SDK实例异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 事件回调处理函数
        /// </summary>
        private void OnEventCallback(IntPtr eventPtr)
        {
            try
            {
                if (eventPtr == IntPtr.Zero)
                    return;

                // 读取事件类型
                var baseEvent = Marshal.PtrToStructure<WliSdkCAPI.WliEventC>(eventPtr);

                switch ((WliSdkCAPI.WliEventType)baseEvent.type)
                {
                    case WliSdkCAPI.WliEventType.WLI_SPECTRUM_EVENT_TYPE:
                        HandleSpectrumEvent(eventPtr);
                        break;

                    case WliSdkCAPI.WliEventType.WLI_FFT_EVENT_TYPE:
                        HandleFftEvent(eventPtr);
                        break;

                    case WliSdkCAPI.WliEventType.WLI_SPECTRUM_FFT_EVENT_TYPE:
                        HandleSpectrumFftEvent(eventPtr);
                        break;

                    case WliSdkCAPI.WliEventType.WLI_THICKNESS_EVENT_TYPE:
                        HandleThicknessEvent(eventPtr);
                        break;

                    case WliSdkCAPI.WliEventType.WLI_THICK_ENCODER_EVENT_TYPE:
                        HandleThickEncoderEvent(eventPtr);
                        break;

                    case WliSdkCAPI.WliEventType.WLI_ERROR_EVENT_TYPE:
                        HandleErrorEvent(eventPtr);
                        break;

                    default:
                        // 其他事件类型暂不处理
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"事件回调异常: {ex.Message}");
            }
        }

        private void HandleSpectrumEvent(IntPtr eventPtr)
        {
            var spectrumEvent = Marshal.PtrToStructure<WliSdkCAPI.WliSpectrumEventC>(eventPtr);

            if (spectrumEvent.count > 0 && spectrumEvent.spectrum != IntPtr.Zero)
            {
                ushort[] spectrum = CopyUInt16Array(spectrumEvent.spectrum, (int)spectrumEvent.count);
                UpdateSpectrumDisplayData(spectrum, spectrumEvent.peak);
            }
        }

        private void HandleFftEvent(IntPtr eventPtr)
        {
            var fftEvent = Marshal.PtrToStructure<WliSdkCAPI.WliFFTEventC>(eventPtr);

            if (fftEvent.count > 0 && fftEvent.fft != IntPtr.Zero)
            {
                float[] fft = new float[fftEvent.count];
                Marshal.Copy(fftEvent.fft, fft, 0, (int)fftEvent.count);
                UpdateFftDisplayData(fft, fftEvent.peak, fftEvent.thickness, fftEvent.thickCoeff);
            }
        }

        private void HandleSpectrumFftEvent(IntPtr eventPtr)
        {
            var spectrumFftEvent = Marshal.PtrToStructure<WliSdkCAPI.WliSpectrumFFTEventC>(eventPtr);

            if (spectrumFftEvent.spectrumCount > 0 && spectrumFftEvent.spectrum != IntPtr.Zero)
            {
                ushort[] spectrum = CopyUInt16Array(spectrumFftEvent.spectrum, (int)spectrumFftEvent.spectrumCount);
                UpdateSpectrumDisplayData(spectrum, spectrumFftEvent.spectrumPeak);
            }

            if (spectrumFftEvent.fftCount > 0 && spectrumFftEvent.fft != IntPtr.Zero)
            {
                float[] fft = new float[spectrumFftEvent.fftCount];
                Marshal.Copy(spectrumFftEvent.fft, fft, 0, (int)spectrumFftEvent.fftCount);
                UpdateFftDisplayData(fft, spectrumFftEvent.fftPeak, spectrumFftEvent.thickness, spectrumFftEvent.thickCoeff);
            }

            AppendThicknessDisplayData(new[] { spectrumFftEvent.thickness });
        }

        /// <summary>
        /// 处理厚度事件
        /// </summary>
        private void HandleThicknessEvent(IntPtr eventPtr)
        {
            var thickEvent = Marshal.PtrToStructure<WliSdkCAPI.WliThicknessEventC>(eventPtr);

            if (thickEvent.count > 0 && thickEvent.thicknesses != IntPtr.Zero)
            {
                // 读取厚度数组
                float[] thicknesses = new float[thickEvent.count];
                Marshal.Copy(thickEvent.thicknesses, thicknesses, 0, (int)thickEvent.count);
                AppendThicknessDisplayData(thicknesses);

                // 将厚度数据按原始通道结构写入MeasureData，保持与TronSight一致。
                for (int i = 0; i < thicknesses.Length; i++)
                {
                    MeasureData data = BuildOriginalMeasureData("Channel1", new Dictionary<string, object>
                    {
                        { "THICKNESS", thicknesses[i] }
                    });

                    _dataQueue.Enqueue(data);
                }

                CachedDataCount = _dataQueue.Count;
            }
        }

        /// <summary>
        /// 处理厚度+编码器事件
        /// </summary>
        private void HandleThickEncoderEvent(IntPtr eventPtr)
        {
            var thickEncEvent = Marshal.PtrToStructure<WliSdkCAPI.WliThickEncoderEventC>(eventPtr);

            if (thickEncEvent.count > 0 && thickEncEvent.thicknesses != IntPtr.Zero)
            {
                // 读取厚度数组
                float[] thicknesses = new float[thickEncEvent.count];
                Marshal.Copy(thickEncEvent.thicknesses, thicknesses, 0, (int)thickEncEvent.count);

                // 读取编码器数据
                float[] encVals1 = new float[thickEncEvent.count];
                float[] encVals2 = new float[thickEncEvent.count];
                float[] encVals3 = new float[thickEncEvent.count];

                if (thickEncEvent.encVals1 != IntPtr.Zero)
                    Marshal.Copy(thickEncEvent.encVals1, encVals1, 0, (int)thickEncEvent.count);
                if (thickEncEvent.encVals2 != IntPtr.Zero)
                    Marshal.Copy(thickEncEvent.encVals2, encVals2, 0, (int)thickEncEvent.count);
                if (thickEncEvent.encVals3 != IntPtr.Zero)
                    Marshal.Copy(thickEncEvent.encVals3, encVals3, 0, (int)thickEncEvent.count);
                AppendEncoderThicknessDisplayData(thicknesses, encVals1, encVals2, encVals3);

                // 将厚度和编码器数据按原始通道结构写入MeasureData，保持与TronSight一致。
                for (int i = 0; i < thicknesses.Length; i++)
                {
                    MeasureData data = BuildOriginalMeasureData("Channel1", new Dictionary<string, object>
                    {
                        { "THICKNESS", thicknesses[i] },
                        { "ENCODER1", encVals1[i] },
                        { "ENCODER2", encVals2[i] },
                        { "ENCODER3", encVals3[i] }
                    });

                    _dataQueue.Enqueue(data);
                }

                CachedDataCount = _dataQueue.Count;
            }
        }

        private static ushort[] CopyUInt16Array(IntPtr source, int count)
        {
            byte[] bytes = new byte[count * sizeof(ushort)];
            ushort[] result = new ushort[count];
            Marshal.Copy(source, bytes, 0, bytes.Length);
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        private void UpdateSpectrumDisplayData(ushort[] spectrum, uint peak)
        {
            lock (_displayDataLock)
            {
                _latestSpectrum = spectrum;
                _spectrumPeakHistory.Add(peak);
                TrimDisplayPoints(_spectrumPeakHistory);
            }

            SpectrumFrameCount++;
            LastSpectrumPeak = Convert.ToInt32(peak);
            _spectrumPeakTotal += peak;
            AverageSpectrumPeak = SpectrumFrameCount > 0 ? _spectrumPeakTotal / SpectrumFrameCount : 0;
        }

        private void UpdateFftDisplayData(float[] fft, float peak, float thickness, float thickCoeff)
        {
            lock (_displayDataLock)
            {
                _latestFft = fft;
            }

            LastFftPeak = peak;
            LastFftThickness = thickness;
            LastThickCoeff = thickCoeff;
            LastThickness = thickness;
        }

        private void AppendThicknessDisplayData(float[] thicknesses)
        {
            if (thicknesses.Length == 0)
            {
                return;
            }

            lock (_displayDataLock)
            {
                for (int i = 0; i < thicknesses.Length; i++)
                {
                    _thicknessChartPoints.Add(new JueXinThicknessChartPoint(_chartPointIndex++, thicknesses[i]));
                }

                TrimDisplayPoints(_thicknessChartPoints);
                UpdateThicknessStatistics(_thicknessChartPoints);
            }
        }

        private void AppendEncoderThicknessDisplayData(float[] thicknesses, float[] encVals1, float[] encVals2, float[] encVals3)
        {
            if (thicknesses.Length == 0)
            {
                return;
            }

            lock (_displayDataLock)
            {
                for (int i = 0; i < thicknesses.Length; i++)
                {
                    _thicknessChartPoints.Add(new JueXinThicknessChartPoint(_chartPointIndex, thicknesses[i]));
                    _encoderThicknessChartPoints.Add(new JueXinEncoderThicknessChartPoint(_chartPointIndex, thicknesses[i], encVals1[i], encVals2[i], encVals3[i]));
                    _chartPointIndex++;
                }

                TrimDisplayPoints(_thicknessChartPoints);
                TrimDisplayPoints(_encoderThicknessChartPoints);
                UpdateThicknessStatistics(_thicknessChartPoints);
            }

            LastEncoder1 = encVals1[thicknesses.Length - 1];
            LastEncoder2 = encVals2[thicknesses.Length - 1];
            LastEncoder3 = encVals3[thicknesses.Length - 1];
        }

        private void UpdateThicknessStatistics(List<JueXinThicknessChartPoint> points)
        {
            if (points.Count == 0)
            {
                LastThickness = 0;
                ThicknessMin = 0;
                ThicknessMax = 0;
                ThicknessMean = 0;
                return;
            }

            double min = points[0].Thickness;
            double max = points[0].Thickness;
            double total = 0;

            for (int i = 0; i < points.Count; i++)
            {
                double value = points[i].Thickness;
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                total += value;
            }

            LastThickness = Convert.ToSingle(points[points.Count - 1].Thickness);
            ThicknessMin = Convert.ToSingle(min);
            ThicknessMax = Convert.ToSingle(max);
            ThicknessMean = Convert.ToSingle(total / points.Count);
        }

        private static void TrimDisplayPoints<T>(List<T> points)
        {
            if (points.Count > DisplayPointCapacity)
            {
                points.RemoveRange(0, points.Count - DisplayPointCapacity);
            }
        }

        private static MeasureData BuildOriginalMeasureData(string channelName, Dictionary<string, object> values)
        {
            return new MeasureData
            {
                OriginalDatas = new Dictionary<string, Dictionary<string, object>>
                {
                    { channelName, values }
                }
            };
        }

        /// <summary>
        /// 处理错误事件
        /// </summary>
        private void HandleErrorEvent(IntPtr eventPtr)
        {
            var errorEvent = Marshal.PtrToStructure<WliSdkCAPI.WliErrorEventC>(eventPtr);
            Console.WriteLine($"SDK错误事件: {errorEvent.code}");
        }
        #endregion
    }
}

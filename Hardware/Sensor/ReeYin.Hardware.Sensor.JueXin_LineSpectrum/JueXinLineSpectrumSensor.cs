﻿﻿using LineScanCcs;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ReeYin.Hardware.Sensor.JueXin_LineSpectrum
{
    /// <summary>
    /// 觉芯线光谱传感器实体类。
    /// </summary>
    public class JueXinLineSpectrumSensor : SensorBase
    {
        private LineScanCcsDevice? _device;
        private readonly ConcurrentQueue<MeasureData> _dataQueue = new ConcurrentQueue<MeasureData>();
        private bool _collectWithEncoder;

        private int _sampleInterval = 10000;
        /// <summary>
        /// 采样间隔，控制传感器两次采样之间的时间间隔。
        /// </summary>
        public int SampleInterval
        {
            get { return _sampleInterval; }
            set
            {
                if (_sampleInterval == value) return;
                if (value <= 0)
                {
                    RaisePropertyChanged();
                    return;
                }
                if (!TrySetSdkIntValue("SampleInterval", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _sampleInterval = value;
                RaisePropertyChanged();
            }
        }

        private int _integrationTime = 20;
        /// <summary>
        /// 积分时间，控制单次采样的曝光/积分时长。
        /// </summary>
        public int IntegrationTime
        {
            get { return _integrationTime; }
            set
            {
                if (_integrationTime == value) return;
                if (!TrySetSdkIntValue("IntegrationTime", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _integrationTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _lightEnabled = true;
        /// <summary>
        /// 是否启用传感器光源。
        /// </summary>
        public bool LightEnabled
        {
            get { return _lightEnabled; }
            set
            {
                if (_lightEnabled == value) return;
                if (!TrySetSdkIntValue("LightEnabled", value ? 1 : 0))
                {
                    RaisePropertyChanged();
                    return;
                }
                _lightEnabled = value;
                RaisePropertyChanged();
            }
        }

        private int _highSpeedMode;
        /// <summary>
        /// 高速模式倍率，0-3 分别对应 x1、x1.5、x2、x4。
        /// </summary>
        public int HighSpeedMode
        {
            get { return _highSpeedMode; }
            set
            {
                if (_highSpeedMode == value) return;
                if (!TrySetSdkIntValue("HighSpeedMode", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _highSpeedMode = value;
                RaisePropertyChanged();
            }
        }

        private int _timeout = 3000;
        /// <summary>
        /// 通信超时时间，单位为 ms。
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                if (_timeout == value) return;
                if (!TrySetSdkTimeout(value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _timeout = value;
                RaisePropertyChanged();
            }
        }

        private int _weakSignalThresh;
        /// <summary>
        /// 弱信号阈值，用于过滤信号强度不足的数据。
        /// </summary>
        public int WeakSignalThresh
        {
            get { return _weakSignalThresh; }
            set
            {
                if (_weakSignalThresh == value) return;
                if (!TrySetSdkIntValue("WeakSignalThresh", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _weakSignalThresh = value;
                RaisePropertyChanged();
            }
        }

        private bool _distanceFilterEnabled;
        /// <summary>
        /// 是否启用位移滤波。
        /// </summary>
        public bool DistanceFilterEnabled
        {
            get { return _distanceFilterEnabled; }
            set
            {
                if (_distanceFilterEnabled == value) return;
                if (!TrySetSdkDistanceFilter(value, DistanceFilterWidth))
                {
                    RaisePropertyChanged();
                    return;
                }
                _distanceFilterEnabled = value;
                RaisePropertyChanged();
            }
        }

        private int _distanceFilterWidth = 3;
        /// <summary>
        /// 位移滤波窗口宽度。
        /// </summary>
        public int DistanceFilterWidth
        {
            get { return _distanceFilterWidth; }
            set
            {
                if (_distanceFilterWidth == value) return;
                if (!TrySetSdkDistanceFilter(DistanceFilterEnabled, value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _distanceFilterWidth = value;
                RaisePropertyChanged();
            }
        }

        private int _peakCount = 1;
        /// <summary>
        /// 参与测量的峰值数量。
        /// </summary>
        public int PeakCount
        {
            get { return _peakCount; }
            set
            {
                if (_peakCount == value) return;
                if (!TrySetSdkIntValue("PeakCount", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _peakCount = value;
                RaisePropertyChanged();
            }
        }

        private int _multiPeakThresh = 1;
        /// <summary>
        /// 多峰寻峰阈值。
        /// </summary>
        public int MultiPeakThresh
        {
            get { return _multiPeakThresh; }
            set
            {
                if (_multiPeakThresh == value) return;
                if (!TrySetSdkIntValue("MultiPeakThresh", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _multiPeakThresh = value;
                RaisePropertyChanged();
            }
        }

        private int _multiPeakInterval;
        /// <summary>
        /// 多峰峰间隔。
        /// </summary>
        public int MultiPeakInterval
        {
            get { return _multiPeakInterval; }
            set
            {
                if (_multiPeakInterval == value) return;
                if (!TrySetSdkIntValue("MultiPeakInterval", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _multiPeakInterval = value;
                RaisePropertyChanged();
            }
        }

        private int _multiPeakIndex;
        /// <summary>
        /// 多峰输出峰索引。
        /// </summary>
        public int MultiPeakIndex
        {
            get { return _multiPeakIndex; }
            set
            {
                if (_multiPeakIndex == value) return;
                if (!TrySetSdkIntValue("MultiPeakIndex", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _multiPeakIndex = value;
                RaisePropertyChanged();
            }
        }

        private int _multiPeakWidthMode = 3;
        /// <summary>
        /// 多峰峰宽选择模式。
        /// </summary>
        public int MultiPeakWidthMode
        {
            get { return _multiPeakWidthMode; }
            set
            {
                if (_multiPeakWidthMode == value) return;
                if (!TrySetSdkIntValue("MultiPeakWidthMode", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _multiPeakWidthMode = value;
                RaisePropertyChanged();
            }
        }

        private int _multiPeakWidth;
        /// <summary>
        /// 多峰峰宽。
        /// </summary>
        public int MultiPeakWidth
        {
            get { return _multiPeakWidth; }
            set
            {
                if (_multiPeakWidth == value) return;
                if (!TrySetSdkIntValue("MultiPeakWidth", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _multiPeakWidth = value;
                RaisePropertyChanged();
            }
        }

        private int _singlePeakMode;
        /// <summary>
        /// 单峰寻峰计算方式。
        /// </summary>
        public int SinglePeakMode
        {
            get { return _singlePeakMode; }
            set
            {
                if (_singlePeakMode == value) return;
                if (!TrySetSdkIntValue("SinglePeakMode", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _singlePeakMode = value;
                RaisePropertyChanged();
            }
        }

        private int _singlePeakHalfWidth = 1;
        /// <summary>
        /// 单峰寻峰单侧点数。
        /// </summary>
        public int SinglePeakHalfWidth
        {
            get { return _singlePeakHalfWidth; }
            set
            {
                if (_singlePeakHalfWidth == value) return;
                if (!TrySetSdkIntValue("SinglePeakHalfWidth", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _singlePeakHalfWidth = value;
                RaisePropertyChanged();
            }
        }

        private double _singlePeakThresh;
        /// <summary>
        /// 单峰识别阈值。
        /// </summary>
        public double SinglePeakThresh
        {
            get { return _singlePeakThresh; }
            set
            {
                if (Math.Abs(_singlePeakThresh - value) < double.Epsilon) return;
                if (!TrySetSdkFloatValue("SinglePeakThresh", value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _singlePeakThresh = value;
                RaisePropertyChanged();
            }
        }

        private int _spectrumChannel;
        /// <summary>
        /// 光谱通道编号。
        /// </summary>
        public int SpectrumChannel
        {
            get { return _spectrumChannel; }
            set
            {
                if (_spectrumChannel == value) return;
                if (!TrySetSdkSpectrumChannel(value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _spectrumChannel = value;
                RaisePropertyChanged();
            }
        }

        private bool _triggerEnabled;
        /// <summary>
        /// 是否启用外部触发。
        /// </summary>
        public bool TriggerEnabled
        {
            get { return _triggerEnabled; }
            set
            {
                if (_triggerEnabled == value) return;
                if (!TrySetSdkTriggerEnabled(value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _triggerEnabled = value;
                RaisePropertyChanged();
            }
        }

        private string _triggerSource = "Input";
        /// <summary>
        /// 触发源。
        /// </summary>
        public string TriggerSource
        {
            get { return _triggerSource; }
            set
            {
                if (_triggerSource == value) return;
                if (!TrySetSdkTriggerConfig(value, TriggerMode, TriggerLevel, TriggerCount, TriggerDebounceTimeUs))
                {
                    RaisePropertyChanged();
                    return;
                }
                _triggerSource = value;
                RaisePropertyChanged();
            }
        }

        private string _triggerMode = "Edge";
        /// <summary>
        /// 触发模式。
        /// </summary>
        public string TriggerMode
        {
            get { return _triggerMode; }
            set
            {
                if (_triggerMode == value) return;
                //if (!TrySetSdkTriggerConfig(TriggerSource, value, TriggerLevel, TriggerCount, TriggerDebounceTimeUs))
                //{
                //    RaisePropertyChanged();
                //    return;
                //}
                _triggerMode = value;
                RaisePropertyChanged();
            }
        }

        private string _triggerLevel = "RisingEdge";
        /// <summary>
        /// 触发电平或边沿类型。
        /// </summary>
        public string TriggerLevel
        {
            get { return _triggerLevel; }
            set
            {
                if (_triggerLevel == value) return;
                if (!TrySetSdkTriggerConfig(TriggerSource, TriggerMode, value, TriggerCount, TriggerDebounceTimeUs))
                {
                    RaisePropertyChanged();
                    return;
                }
                _triggerLevel = value;
                RaisePropertyChanged();
            }
        }

        private int _triggerCount = 1;
        /// <summary>
        /// 触发次数。
        /// </summary>
        public int TriggerCount
        {
            get { return _triggerCount; }
            set
            {
                if (_triggerCount == value) return;
                if (!TrySetSdkTriggerConfig(TriggerSource, TriggerMode, TriggerLevel, value, TriggerDebounceTimeUs))
                {
                    RaisePropertyChanged();
                    return;
                }
                _triggerCount = value;
                RaisePropertyChanged();
            }
        }

        private double _triggerDebounceTimeUs;
        /// <summary>
        /// 触发去抖时间，单位为 us。
        /// </summary>
        public double TriggerDebounceTimeUs
        {
            get { return _triggerDebounceTimeUs; }
            set
            {
                if (!TrySetSdkTriggerConfig(TriggerSource, TriggerMode, TriggerLevel, TriggerCount, value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _triggerDebounceTimeUs = value;
                RaisePropertyChanged();
            }
        }

        private bool _syncEnabled;
        /// <summary>
        /// 是否启用同步功能。
        /// </summary>
        public bool SyncEnabled
        {
            get { return _syncEnabled; }
            set
            {
                if (_syncEnabled == value) return;
                if (!TrySetSdkSyncEnabled(value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _syncEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _syncIsSlave;
        /// <summary>
        /// 同步模式下是否作为从机。
        /// </summary>
        public bool SyncIsSlave
        {
            get { return _syncIsSlave; }
            set
            {
                if (_syncIsSlave == value) return;
                if (!TrySetSdkSyncConfig(value, SyncTermResEnabled))
                {
                    RaisePropertyChanged();
                    return;
                }
                _syncIsSlave = value;
                RaisePropertyChanged();
            }
        }

        private bool _syncTermResEnabled;
        /// <summary>
        /// 同步链路是否启用终端电阻。
        /// </summary>
        public bool SyncTermResEnabled
        {
            get { return _syncTermResEnabled; }
            set
            {
                if (_syncTermResEnabled == value) return;
                if (!TrySetSdkSyncConfig(SyncIsSlave, value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _syncTermResEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _encoderEnabled;
        /// <summary>
        /// 是否启用编码器触发。
        /// </summary>
        public bool EncoderEnabled
        {
            get { return _encoderEnabled; }
            set
            {
                if (_encoderEnabled == value) return;
                if (!TrySetSdkEncoderEnabled(EncoderId, value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderEnabled = value;
                RaisePropertyChanged();
            }
        }

        private int _encoderId = 1;
        /// <summary>
        /// 编码器编号。
        /// </summary>
        public int EncoderId
        {
            get { return _encoderId; }
            set
            {
                if (_encoderId == value) return;
                _encoderId = value;
                RaisePropertyChanged();
            }
        }

        private string _encoderInputMode = "SinglePhase";
        /// <summary>
        /// 编码器输入模式。
        /// </summary>
        public string EncoderInputMode
        {
            get { return _encoderInputMode; }
            set
            {
                if (_encoderInputMode == value) return;
                if (!TrySetSdkEncoderConfig(EncoderId, value, EncoderTriggerDirection, EncoderDecodeMode, EncoderPulsePerTrigger, EncoderLengthPerPulse, EncoderZPhaseEnabled, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderInputMode = value;
                RaisePropertyChanged();
            }
        }

        private string _encoderTriggerDirection = "Forward";
        /// <summary>
        /// 编码器触发方向。
        /// </summary>
        public string EncoderTriggerDirection
        {
            get { return _encoderTriggerDirection; }
            set
            {
                if (_encoderTriggerDirection == value) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, value, EncoderDecodeMode, EncoderPulsePerTrigger, EncoderLengthPerPulse, EncoderZPhaseEnabled, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderTriggerDirection = value;
                RaisePropertyChanged();
            }
        }

        private string _encoderDecodeMode = "X1";
        /// <summary>
        /// 编码器解码模式。
        /// </summary>
        public string EncoderDecodeMode
        {
            get { return _encoderDecodeMode; }
            set
            {
                if (_encoderDecodeMode == value) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, EncoderTriggerDirection, value, EncoderPulsePerTrigger, EncoderLengthPerPulse, EncoderZPhaseEnabled, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderDecodeMode = value;
                RaisePropertyChanged();
            }
        }

        private int _encoderPulsePerTrigger = 1;
        /// <summary>
        /// 每次触发对应的编码器脉冲数。
        /// </summary>
        public int EncoderPulsePerTrigger
        {
            get { return _encoderPulsePerTrigger; }
            set
            {
                if (_encoderPulsePerTrigger == value) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, EncoderTriggerDirection, EncoderDecodeMode, value, EncoderLengthPerPulse, EncoderZPhaseEnabled, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderPulsePerTrigger = value;
                RaisePropertyChanged();
            }
        }

        private double _encoderLengthPerPulse = 0.001;
        /// <summary>
        /// 单个编码器脉冲对应的位移长度。
        /// </summary>
        public double EncoderLengthPerPulse
        {
            get { return _encoderLengthPerPulse; }
            set
            {
                if (Math.Abs(_encoderLengthPerPulse - value) < double.Epsilon) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, EncoderTriggerDirection, EncoderDecodeMode, EncoderPulsePerTrigger, value, EncoderZPhaseEnabled, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderLengthPerPulse = value;
                RaisePropertyChanged();
            }
        }

        private bool _encoderZPhaseEnabled;
        /// <summary>
        /// 是否启用编码器 Z 相复位。
        /// </summary>
        public bool EncoderZPhaseEnabled
        {
            get { return _encoderZPhaseEnabled; }
            set
            {
                if (_encoderZPhaseEnabled == value) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, EncoderTriggerDirection, EncoderDecodeMode, EncoderPulsePerTrigger, EncoderLengthPerPulse, value, EncoderZPhaseResetPosition))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderZPhaseEnabled = value;
                RaisePropertyChanged();
            }
        }

        private double _encoderZPhaseResetPosition;
        /// <summary>
        /// 编码器 Z 相复位位置。
        /// </summary>
        public double EncoderZPhaseResetPosition
        {
            get { return _encoderZPhaseResetPosition; }
            set
            {
                if (Math.Abs(_encoderZPhaseResetPosition - value) < double.Epsilon) return;
                if (!TrySetSdkEncoderConfig(EncoderId, EncoderInputMode, EncoderTriggerDirection, EncoderDecodeMode, EncoderPulsePerTrigger, EncoderLengthPerPulse, EncoderZPhaseEnabled, value))
                {
                    RaisePropertyChanged();
                    return;
                }
                _encoderZPhaseResetPosition = value;
                RaisePropertyChanged();
            }
        }

        public JueXinLineSpectrumSensor()
        {
            VenderName = "JueXin_LineSpectrum";
            VenderType = "LineSpectrum";
            IP = "192.168.1.120";
            Port = 8001;
        }

        /// <summary>
        /// 打开觉芯设备连接，并将当前缓存的参数一次性下发到设备。
        /// </summary>
        public override bool Init()
        {
            State = HardwareState.Initializing;

            LineScanCcsDevice? device = null;
            try
            {
                device = LineScanCcsDevice.Open(IP, Port);
                string serialNumber = device.GetSerialNumber();
                device.DistanceReceived += OnDistanceReceived;
                device.EncoderDistanceReceived += OnEncoderDistanceReceived;
                _device = device;
                IsConnected = true;
                State = HardwareState.Ready;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"觉芯传感器连接失败：{ex.Message}");
                device?.Dispose();
                _device = null;
                IsConnected = false;
                State = HardwareState.Error;
                return false;
            }
        }

        /// <summary>
        /// 停止采集并释放设备连接。
        /// </summary>
        public override void Close()
        {
            StopCollect();
            if (_device != null)
            {
                _device.DistanceReceived -= OnDistanceReceived;
                _device.EncoderDistanceReceived -= OnEncoderDistanceReceived;
                _device.Dispose();
            }
            _device = null;
            IsConnected = false;
            State = HardwareState.Closed;
        }

        /// <summary>
        /// 根据当前是否启用编码器，启动普通位移流或编码器位移流。
        /// </summary>
        public override void StartCollect()
        {
            if (!IsDeviceReady()) return;

            ClearCachedData();
            _collectWithEncoder = EncoderEnabled;

            if (_collectWithEncoder)
            {
                _device!.StartEncoderDistanceStream();
            }
            else
            {
                _device!.StartDistanceStream();
            }

            State = HardwareState.Running;
        }

        /// <summary>
        /// 停止本次采集启动时打开的数据流。
        /// </summary>
        public override void StopCollect()
        {
            if (!IsDeviceReady()) return;

            if (_collectWithEncoder)
            {
                _device!.StopEncoderDistanceStream();
            }
            else
            {
                _device!.StopDistanceStream();
            }
            State = HardwareState.Complete;
        }

        /// <summary>
        /// 预留数据接收入口，当前尚未接入实时回调缓存。
        /// </summary>
        public override List<MeasureData> ReceiveSensorData()
        {
            List<MeasureData> measureDataList = new List<MeasureData>();
            while (_dataQueue.TryDequeue(out MeasureData? measureData))
            {
                measureDataList.Add(measureData);
            }

            return measureDataList;
        }

        /// <summary>
        /// 页面打开时从设备回读当前参数，避免界面继续显示本地旧缓存。
        /// </summary>
        public bool RefreshParametersFromDevice()
        {
            if (!IsDeviceReady()) return false;

            try
            {
                int encoderId = _encoderId == 2 ? 2 : 1;
                int timeout = _device!.GetTimeout();
                int sampleInterval = _device.GetIntValue("SampleInterval");
                int integrationTime = _device.GetIntValue("IntegrationTime");
                bool lightEnabled = _device.GetIntValue("LightEnabled") != 0;
                int highSpeedMode = _device.GetIntValue("HighSpeedMode");
                int weakSignalThresh = _device.GetIntValue("WeakSignalThresh");
                _device.GetDistanceFilter(out bool distanceFilterEnabled, out int distanceFilterWidth);
                int peakCount = _device.GetIntValue("PeakCount");
                int multiPeakThresh = _device.GetIntValue("MultiPeakThresh");
                int multiPeakInterval = _device.GetIntValue("MultiPeakInterval");
                int multiPeakIndex = _device.GetIntValue("MultiPeakIndex");
                int multiPeakWidthMode = _device.GetIntValue("MultiPeakWidthMode");
                int multiPeakWidth = _device.GetIntValue("MultiPeakWidth");
                int singlePeakMode = _device.GetIntValue("SinglePeakMode");
                int singlePeakHalfWidth = _device.GetIntValue("SinglePeakHalfWidth");
                double singlePeakThresh = _device.GetFloatValue("SinglePeakThresh");
                int spectrumChannel = _device.GetSpectrumChannel();
                bool triggerEnabled = _device.GetTriggerEnabled();
                TriggerConfig triggerConfig = _device.GetTriggerConfig();
                bool syncEnabled = _device.GetSyncEnabled();
                _device.GetSyncConfig(out bool syncIsSlave, out bool syncTermResEnabled);
                bool encoderEnabled = _device.GetEncoderEnabled(encoderId);
                EncoderConfig encoderConfig = _device.GetEncoderConfig(encoderId);

                UpdateCachedValue(ref _timeout, timeout, nameof(Timeout));
                UpdateCachedValue(ref _sampleInterval, sampleInterval, nameof(SampleInterval));
                UpdateCachedValue(ref _integrationTime, integrationTime, nameof(IntegrationTime));
                UpdateCachedValue(ref _lightEnabled, lightEnabled, nameof(LightEnabled));
                UpdateCachedValue(ref _highSpeedMode, highSpeedMode, nameof(HighSpeedMode));
                UpdateCachedValue(ref _weakSignalThresh, weakSignalThresh, nameof(WeakSignalThresh));
                UpdateCachedValue(ref _distanceFilterEnabled, distanceFilterEnabled, nameof(DistanceFilterEnabled));
                UpdateCachedValue(ref _distanceFilterWidth, distanceFilterWidth, nameof(DistanceFilterWidth));
                UpdateCachedValue(ref _peakCount, peakCount, nameof(PeakCount));
                UpdateCachedValue(ref _multiPeakThresh, multiPeakThresh, nameof(MultiPeakThresh));
                UpdateCachedValue(ref _multiPeakInterval, multiPeakInterval, nameof(MultiPeakInterval));
                UpdateCachedValue(ref _multiPeakIndex, multiPeakIndex, nameof(MultiPeakIndex));
                UpdateCachedValue(ref _multiPeakWidthMode, multiPeakWidthMode, nameof(MultiPeakWidthMode));
                UpdateCachedValue(ref _multiPeakWidth, multiPeakWidth, nameof(MultiPeakWidth));
                UpdateCachedValue(ref _singlePeakMode, singlePeakMode, nameof(SinglePeakMode));
                UpdateCachedValue(ref _singlePeakHalfWidth, singlePeakHalfWidth, nameof(SinglePeakHalfWidth));
                UpdateCachedValue(ref _singlePeakThresh, singlePeakThresh, nameof(SinglePeakThresh));
                UpdateCachedValue(ref _spectrumChannel, spectrumChannel, nameof(SpectrumChannel));
                UpdateCachedValue(ref _triggerEnabled, triggerEnabled, nameof(TriggerEnabled));
                UpdateCachedValue(ref _triggerSource, triggerConfig.TriggerSource.ToString(), nameof(TriggerSource));
                UpdateCachedValue(ref _triggerMode, triggerConfig.TriggerMode.ToString(), nameof(TriggerMode));
                UpdateCachedValue(ref _triggerLevel, triggerConfig.TriggerLevel.ToString(), nameof(TriggerLevel));
                UpdateCachedValue(ref _triggerCount, triggerConfig.TriggerCount, nameof(TriggerCount));
                UpdateCachedValue(ref _triggerDebounceTimeUs, triggerConfig.DebounceTimeUs, nameof(TriggerDebounceTimeUs));
                UpdateCachedValue(ref _syncEnabled, syncEnabled, nameof(SyncEnabled));
                UpdateCachedValue(ref _syncIsSlave, syncIsSlave, nameof(SyncIsSlave));
                UpdateCachedValue(ref _syncTermResEnabled, syncTermResEnabled, nameof(SyncTermResEnabled));
                UpdateCachedValue(ref _encoderId, encoderId, nameof(EncoderId));
                UpdateCachedValue(ref _encoderEnabled, encoderEnabled, nameof(EncoderEnabled));
                UpdateCachedValue(ref _encoderInputMode, encoderConfig.InputMode.ToString(), nameof(EncoderInputMode));
                UpdateCachedValue(ref _encoderTriggerDirection, encoderConfig.TriggerDirection.ToString(), nameof(EncoderTriggerDirection));
                UpdateCachedValue(ref _encoderDecodeMode, encoderConfig.DecodeMode.ToString(), nameof(EncoderDecodeMode));
                UpdateCachedValue(ref _encoderPulsePerTrigger, encoderConfig.PulsePerTrigger, nameof(EncoderPulsePerTrigger));
                UpdateCachedValue(ref _encoderLengthPerPulse, encoderConfig.LengthPerPulse, nameof(EncoderLengthPerPulse));
                UpdateCachedValue(ref _encoderZPhaseEnabled, encoderConfig.ZPhaseEnabled != 0, nameof(EncoderZPhaseEnabled));
                UpdateCachedValue(ref _encoderZPhaseResetPosition, encoderConfig.ZPhaseResetPosition, nameof(EncoderZPhaseResetPosition));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取觉芯设备参数失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 统一的参数设置入口，外部通过参数名写值时最终仍走属性 setter。
        /// </summary>
        public override bool SettingParam(string key, object value)
        {
            try
            {
                switch (key)
                {
                    case nameof(IP):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        IP = targetValue;
                        return IP == targetValue;
                    }

                    case nameof(Port):
                    {
                        ushort targetValue = Convert.ToUInt16(value);
                        Port = targetValue;
                        return Port == targetValue;
                    }

                    case nameof(SampleInterval):
                    {
                        int targetValue = Convert.ToInt32(value);
                        SampleInterval = targetValue;
                        return SampleInterval == targetValue;
                    }

                    case nameof(IntegrationTime):
                    {
                        int targetValue = Convert.ToInt32(value);
                        IntegrationTime = targetValue;
                        return IntegrationTime == targetValue;
                    }

                    case nameof(LightEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        LightEnabled = targetValue;
                        return LightEnabled == targetValue;
                    }

                    case nameof(HighSpeedMode):
                    {
                        int targetValue = Convert.ToInt32(value);
                        HighSpeedMode = targetValue;
                        return HighSpeedMode == targetValue;
                    }

                    case nameof(Timeout):
                    {
                        int targetValue = Convert.ToInt32(value);
                        Timeout = targetValue;
                        return Timeout == targetValue;
                    }

                    case nameof(WeakSignalThresh):
                    {
                        int targetValue = Convert.ToInt32(value);
                        WeakSignalThresh = targetValue;
                        return WeakSignalThresh == targetValue;
                    }

                    case nameof(DistanceFilterEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        DistanceFilterEnabled = targetValue;
                        return DistanceFilterEnabled == targetValue;
                    }

                    case nameof(DistanceFilterWidth):
                    {
                        int targetValue = Convert.ToInt32(value);
                        DistanceFilterWidth = targetValue;
                        return DistanceFilterWidth == targetValue;
                    }

                    case nameof(PeakCount):
                    {
                        int targetValue = Convert.ToInt32(value);
                        PeakCount = targetValue;
                        return PeakCount == targetValue;
                    }

                    case nameof(MultiPeakThresh):
                    {
                        int targetValue = Convert.ToInt32(value);
                        MultiPeakThresh = targetValue;
                        return MultiPeakThresh == targetValue;
                    }

                    case nameof(MultiPeakInterval):
                    {
                        int targetValue = Convert.ToInt32(value);
                        MultiPeakInterval = targetValue;
                        return MultiPeakInterval == targetValue;
                    }

                    case nameof(MultiPeakIndex):
                    {
                        int targetValue = Convert.ToInt32(value);
                        MultiPeakIndex = targetValue;
                        return MultiPeakIndex == targetValue;
                    }

                    case nameof(MultiPeakWidthMode):
                    {
                        int targetValue = Convert.ToInt32(value);
                        MultiPeakWidthMode = targetValue;
                        return MultiPeakWidthMode == targetValue;
                    }

                    case nameof(MultiPeakWidth):
                    {
                        int targetValue = Convert.ToInt32(value);
                        MultiPeakWidth = targetValue;
                        return MultiPeakWidth == targetValue;
                    }

                    case nameof(SinglePeakMode):
                    {
                        int targetValue = Convert.ToInt32(value);
                        SinglePeakMode = targetValue;
                        return SinglePeakMode == targetValue;
                    }

                    case nameof(SinglePeakHalfWidth):
                    {
                        int targetValue = Convert.ToInt32(value);
                        SinglePeakHalfWidth = targetValue;
                        return SinglePeakHalfWidth == targetValue;
                    }

                    case nameof(SinglePeakThresh):
                    {
                        double targetValue = Convert.ToDouble(value);
                        SinglePeakThresh = targetValue;
                        return Math.Abs(SinglePeakThresh - targetValue) < double.Epsilon;
                    }

                    case nameof(SpectrumChannel):
                    {
                        int targetValue = Convert.ToInt32(value);
                        SpectrumChannel = targetValue;
                        return SpectrumChannel == targetValue;
                    }

                    case nameof(TriggerEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        TriggerEnabled = targetValue;
                        return TriggerEnabled == targetValue;
                    }

                    case nameof(TriggerSource):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        TriggerSource = targetValue;
                        return TriggerSource == targetValue;
                    }

                    case nameof(TriggerMode):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        TriggerMode = targetValue;
                        return TriggerMode == targetValue;
                    }

                    case nameof(TriggerLevel):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        TriggerLevel = targetValue;
                        return TriggerLevel == targetValue;
                    }

                    case nameof(TriggerCount):
                    {
                        int targetValue = Convert.ToInt32(value);
                        TriggerCount = targetValue;
                        return TriggerCount == targetValue;
                    }

                    case nameof(TriggerDebounceTimeUs):
                    {
                        double targetValue = Convert.ToDouble(value);
                        TriggerDebounceTimeUs = targetValue;
                        return Math.Abs(TriggerDebounceTimeUs - targetValue) < double.Epsilon;
                    }

                    case nameof(SyncEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        SyncEnabled = targetValue;
                        return SyncEnabled == targetValue;
                    }

                    case nameof(SyncIsSlave):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        SyncIsSlave = targetValue;
                        return SyncIsSlave == targetValue;
                    }

                    case nameof(SyncTermResEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        SyncTermResEnabled = targetValue;
                        return SyncTermResEnabled == targetValue;
                    }

                    case nameof(EncoderEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        EncoderEnabled = targetValue;
                        return EncoderEnabled == targetValue;
                    }

                    case nameof(EncoderId):
                    {
                        int targetValue = Convert.ToInt32(value);
                        EncoderId = targetValue;
                        return EncoderId == targetValue;
                    }

                    case nameof(EncoderInputMode):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        EncoderInputMode = targetValue;
                        return EncoderInputMode == targetValue;
                    }

                    case nameof(EncoderTriggerDirection):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        EncoderTriggerDirection = targetValue;
                        return EncoderTriggerDirection == targetValue;
                    }

                    case nameof(EncoderDecodeMode):
                    {
                        string targetValue = Convert.ToString(value) ?? string.Empty;
                        EncoderDecodeMode = targetValue;
                        return EncoderDecodeMode == targetValue;
                    }

                    case nameof(EncoderPulsePerTrigger):
                    {
                        int targetValue = Convert.ToInt32(value);
                        EncoderPulsePerTrigger = targetValue;
                        return EncoderPulsePerTrigger == targetValue;
                    }

                    case nameof(EncoderLengthPerPulse):
                    {
                        double targetValue = Convert.ToDouble(value);
                        EncoderLengthPerPulse = targetValue;
                        return Math.Abs(EncoderLengthPerPulse - targetValue) < double.Epsilon;
                    }

                    case nameof(EncoderZPhaseEnabled):
                    {
                        bool targetValue = Convert.ToBoolean(value);
                        EncoderZPhaseEnabled = targetValue;
                        return EncoderZPhaseEnabled == targetValue;
                    }

                    case nameof(EncoderZPhaseResetPosition):
                    {
                        double targetValue = Convert.ToDouble(value);
                        EncoderZPhaseResetPosition = targetValue;
                        return Math.Abs(EncoderZPhaseResetPosition - targetValue) < double.Epsilon;
                    }

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置觉芯线光谱参数 {key} 失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断 SDK 设备对象是否已经初始化完成且当前连接有效。
        /// </summary>
        private bool IsDeviceReady()
        {
            return _device != null && IsConnected;
        }

        /// <summary>
        /// 切换采集模式前先清空旧缓存，避免上一轮残留数据混进当前结果。
        /// </summary>
        private void ClearCachedData()
        {
            while (_dataQueue.TryDequeue(out _))
            {
            }
        }

        /// <summary>
        /// 普通位移流回调，将当前一帧距离数据转换为通用测量结果后缓存。
        /// </summary>
        private void OnDistanceReceived(DistanceFrame frame)
        {
            try
            {
                MeasureData? measureData = BuildMeasureData(frame);
                if (measureData != null)
                {
                    _dataQueue.Enqueue(measureData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"觉芯普通位移回调处理失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 编码器位移流回调，将编码器位置和位移数组一起缓存，供上层统一读取。
        /// </summary>
        private void OnEncoderDistanceReceived(EncoderDistanceFrame frame)
        {
            try
            {
                MeasureData? measureData = BuildMeasureData(frame);
                if (measureData != null)
                {
                    _dataQueue.Enqueue(measureData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"觉芯编码器位移回调处理失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 将普通位移帧映射为项目内部的 MeasureData，只保留高度和灰度显示所需数据。
        /// </summary>
        private MeasureData? BuildMeasureData(DistanceFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            float[] heights = ConvertToFloatArray(frame.Distances);
            float[] peakValues = ConvertToFloatArray(frame.Peaks);
            if (heights.Length == 0 && peakValues.Length == 0)
            {
                return null;
            }

            List<float[]> areaData = new List<float[]>
            {
                heights
            };
            if (peakValues.Length > 0)
            {
                areaData.Add(peakValues);
            }

            return new MeasureData
            {
                RTime = DateTime.Now,
                IsValid = heights.Length > 0,
                AreaData = areaData
            };
        }

        /// <summary>
        /// 将编码器位移帧映射为项目内部的 MeasureData，只保留高度和灰度显示所需数据。
        /// </summary>
        private MeasureData? BuildMeasureData(EncoderDistanceFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            float[] heights = ConvertToFloatArray(frame.Distances);
            float[] peakValues = ConvertToFloatArray(frame.Peaks);
            if (heights.Length == 0 && peakValues.Length == 0)
            {
                return null;
            }

            List<float[]> areaData = new List<float[]>
            {
                heights
            };
            if (peakValues.Length > 0)
            {
                areaData.Add(peakValues);
            }

            return new MeasureData
            {
                RTime = DateTime.Now,
                IsValid = heights.Length > 0,
                AreaData = areaData
            };
        }

        /// <summary>
        /// 将 SDK 返回的数值数组统一转成 float 数组，减少上层处理分支。
        /// </summary>
        private static float[] ConvertToFloatArray(Array? source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<float>();
            }

            float[] values = new float[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                values[i] = Convert.ToSingle(source.GetValue(i));
            }

            return values;
        }

        /// <summary>
        /// 设备回读参数时直接刷新本地缓存，避免再次触发 SDK 下发。
        /// </summary>
        private void UpdateCachedValue<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            RaisePropertyChanged(propertyName);
        }


        /// <summary>
        /// 严格设备模式下，只有 SDK 接受成功才允许界面保留新超时值。
        /// </summary>
        private bool TrySetSdkTimeout(int value)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetTimeout(value);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯超时参数失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 严格设备模式下，整型参数必须设备侧写入成功才更新本地缓存。
        /// </summary>
        private bool TrySetSdkIntValue(string name, int value)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetIntValue(name, value);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯整型参数 {name} 失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 严格设备模式下，浮点参数必须设备侧写入成功才更新本地缓存。
        /// </summary>
        private bool TrySetSdkFloatValue(string name, double value)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetFloatValue(name, Convert.ToSingle(value));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯浮点参数 {name} 失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 位移滤波是成组参数，严格模式下任一项下发失败都不接受本地改值。
        /// </summary>
        private bool TrySetSdkDistanceFilter(bool enabled, int width)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetDistanceFilter(enabled, width);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯位移滤波参数失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 光谱通道切换成功后，界面才保留新通道号。
        /// </summary>
        private bool TrySetSdkSpectrumChannel(int channel)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetSpectrumChannel(channel);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"切换觉芯光谱通道失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 触发总开关直接决定采样方式，严格模式下必须以下发结果为准。
        /// </summary>
        private bool TrySetSdkTriggerEnabled(bool enabled)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetTriggerEnabled(enabled);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯触发开关失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步使能状态单独下发，避免同步配置修改时误带设备开关状态。
        /// </summary>
        private bool TrySetSdkSyncEnabled(bool enabled)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetSyncEnabled(enabled);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯同步开关失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步配置是一组联动参数，严格模式下整组成功才接受界面改值。
        /// </summary>
        private bool TrySetSdkSyncConfig(bool isSlave, bool termResEnabled)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetSyncConfig(isSlave, termResEnabled);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯同步配置失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 触发配置需要整包提交，严格模式下只认整包成功。
        /// </summary>
        private bool TrySetSdkTriggerConfig(string sourceText, string modeText, string levelText, int count, double debounceTimeUs)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                TriggerConfig triggerConfig = new TriggerConfig()
                {
                    TriggerSource = ParseEnum(sourceText, LineScanCcs.TriggerSource.Input),
                    TriggerMode = ParseEnum(modeText, LineScanCcs.TriggerMode.Edge),
                    TriggerLevel = ParseEnum(levelText, LineScanCcs.TriggerLevel.RisingEdge),
                    TriggerCount = count,
                    DebounceTimeUs = Convert.ToSingle(debounceTimeUs)
                };
                _device!.SetTriggerConfig(triggerConfig);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯触发配置失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 编码器开关属于设备实时状态，严格模式下以下发结果回写界面。
        /// </summary>
        private bool TrySetSdkEncoderEnabled(int encoderId, bool enabled)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetEncoderEnabled(encoderId, enabled);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯编码器启用状态失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 编码器配置是组合参数，严格模式下只要设备拒绝其中一项，界面就回退。
        /// </summary>
        private bool TrySetSdkEncoderConfig(int encoderId, string inputModeText, string triggerDirectionText, string decodeModeText, int pulsePerTrigger, double lengthPerPulse, bool zPhaseEnabled, double zPhaseResetPosition)
        {
            if (!IsDeviceReady()) return false;

            try
            {
                _device!.SetEncoderConfig(encoderId, new EncoderConfig
                {
                    InputMode = ParseEnum(inputModeText, LineScanCcs.EncoderInputMode.SinglePhase),
                    TriggerDirection = ParseEnum(triggerDirectionText, LineScanCcs.EncoderTriggerDirection.Forward),
                    DecodeMode = ParseEnum(decodeModeText, LineScanCcs.EncoderDecodeMode.X1),
                    PulsePerTrigger = pulsePerTrigger,
                    LengthPerPulse = Convert.ToSingle(lengthPerPulse),
                    ZPhaseEnabled = zPhaseEnabled ? 1 : 0,
                    ZPhaseResetPosition = Convert.ToSingle(zPhaseResetPosition)
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下发觉芯编码器配置失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将字符串安全转换为枚举，转换失败时回退到默认值。
        /// </summary>
        private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct
        {
            return Enum.TryParse(value, true, out TEnum result) ? result : defaultValue;
        }
    }
}

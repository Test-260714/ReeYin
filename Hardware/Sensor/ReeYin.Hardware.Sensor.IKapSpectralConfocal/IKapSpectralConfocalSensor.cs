using IKapCDotNet;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.IKapSpectralConfocal.API;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReeYin.Hardware.Sensor.IKapSpectralConfocal
{
    /// <summary>
    /// 埃科线光谱共焦传感器实体，负责 IKapC 初始化、参数下发和同步采集。
    /// </summary>
    public class IKapSpectralConfocalSensor : SensorBase
    {
        private const int Int16Size = 2;
        private const int ByteSize = 1;

        [JsonIgnore]
        private ITKDEVICE? _device;
        [JsonIgnore]
        private ITKSTREAM? _stream;
        [JsonIgnore]
        private ITKBUFFER? _currentBuffer;
        [JsonIgnore]
        private ITKBUFFER? _splitBuffer;
        [JsonIgnore]
        private ITK_BUFFER_INFO? _currentBufferInfo;
        [JsonIgnore]
        private readonly object _syncRoot = new object();
        [JsonIgnore]
        private bool _sdkInitialized;
        [JsonIgnore]
        private bool _isStreamStarted;

        private string _lastSdkMessage = string.Empty;
        private uint _deviceIndex = IKapSpectralConfocalSdkNames.DefaultDeviceIndex;
        private string _serialNumber = string.Empty;
        private string _deviceFullName = string.Empty;
        private string _deviceClass = string.Empty;
        private string _pixelFormat = IKapSpectralConfocalSdkNames.PixelFormatCoord3DAC32;
        private string _layerSelector = IKapSpectralConfocalSdkNames.LayerSelectorManual;
        private long _layerNumber = IKapSpectralConfocalSdkNames.DefaultLayerNumber;
        private double _ySpacing = IKapSpectralConfocalSdkNames.DefaultYSpacing;
        private bool _acquisitionLineRateEnabled = true;
        private double _acquisitionLineRate = IKapSpectralConfocalSdkNames.DefaultAcquisitionLineRate;
        private uint _acquisitionLineCount = IKapSpectralConfocalSdkNames.DefaultAcquisitionLineCount;
        private double _exposureTime = IKapSpectralConfocalSdkNames.DefaultExposureTime;
        private long _grabTimeoutMs = IKapSpectralConfocalSdkNames.DefaultGrabTimeoutMs;

        public IKapSpectralConfocalSensor()
        {
            VenderName = "IKapSpectralConfocal";
            VenderType = "SpectralConfocal";
        }

        public string LastSdkMessage
        {
            get { return _lastSdkMessage; }
            private set { _lastSdkMessage = value; RaisePropertyChanged(); }
        }

        public uint DeviceIndex
        {
            get { return _deviceIndex; }
            set { _deviceIndex = value; RaisePropertyChanged(); }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value ?? string.Empty; RaisePropertyChanged(); }
        }

        public string DeviceFullName
        {
            get { return _deviceFullName; }
            private set { _deviceFullName = value; RaisePropertyChanged(); }
        }

        public string DeviceClass
        {
            get { return _deviceClass; }
            private set { _deviceClass = value; RaisePropertyChanged(); }
        }

        public string PixelFormat
        {
            get { return _pixelFormat; }
            set { _pixelFormat = value; RaisePropertyChanged(); }
        }

        public string LayerSelector
        {
            get { return _layerSelector; }
            set { _layerSelector = value; RaisePropertyChanged(); }
        }

        public long LayerNumber
        {
            get { return _layerNumber; }
            set { _layerNumber = value; RaisePropertyChanged(); }
        }

        public double YSpacing
        {
            get { return _ySpacing; }
            set { _ySpacing = value; RaisePropertyChanged(); }
        }

        public bool AcquisitionLineRateEnabled
        {
            get { return _acquisitionLineRateEnabled; }
            set { _acquisitionLineRateEnabled = value; RaisePropertyChanged(); }
        }

        public double AcquisitionLineRate
        {
            get { return _acquisitionLineRate; }
            set { _acquisitionLineRate = value; RaisePropertyChanged(); }
        }

        public uint AcquisitionLineCount
        {
            get { return _acquisitionLineCount; }
            set { _acquisitionLineCount = value; RaisePropertyChanged(); }
        }

        public double ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        public long GrabTimeoutMs
        {
            get { return _grabTimeoutMs; }
            set { _grabTimeoutMs = value; RaisePropertyChanged(); }
        }

        public override bool Init()
        {
            lock (_syncRoot)
            {
                State = HardwareState.Initializing;
                ReleaseSdkHandles(false);

                uint res = IKapC.ItkManInitialize();
                if (!CheckStatus(res, "ItkManInitialize"))
                {
                    State = HardwareState.Error;
                    return false;
                }
                _sdkInitialized = true;

                if (!TryResolveDeviceIndex(out uint openIndex))
                {
                    ReleaseSdkHandles(false);
                    IsConnected = false;
                    State = HardwareState.NotConnected;
                    return false;
                }

                ITKDEVICE device = new ITKDEVICE();
                res = IKapC.ItkDevOpen(openIndex, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref device);
                if (!CheckStatus(res, "ItkDevOpen"))
                {
                    ReleaseSdkHandles(false);
                    IsConnected = false;
                    State = HardwareState.NotConnected;
                    return false;
                }

                _device = device;
                DeviceIndex = openIndex;
                if (!ApplyCameraParameters() || !CreateStream())
                {
                    ReleaseSdkHandles(false);
                    IsConnected = false;
                    State = HardwareState.Error;
                    return false;
                }

                IsConnected = true;
                State = HardwareState.Connected;
                LastSdkMessage = "IKapSpectralConfocal 初始化完成。";
                return true;
            }
        }

        public override void Close()
        {
            lock (_syncRoot)
            {
                ReleaseSdkHandles(true);
                LastSdkMessage = "IKapSpectralConfocal 已关闭。";
            }
        }

        public override void StartCollect()
        {
            lock (_syncRoot)
            {
                if (_stream == null)
                {
                    LastSdkMessage = "IKapSpectralConfocal 采集失败：数据流未初始化。";
                    State = HardwareState.Error;
                    return;
                }

                uint res = IKapC.ItkStreamStart(_stream, IKapC.ITKSTREAM_CONTINUOUS);
                if (!CheckStatus(res, "ItkStreamStart"))
                {
                    State = HardwareState.Error;
                    return;
                }

                _isStreamStarted = true;
                State = HardwareState.Running;
                LastSdkMessage = "IKapSpectralConfocal 已开始采集。";
            }
        }

        public override void StopCollect()
        {
            lock (_syncRoot)
            {
                StopStream();
                State = HardwareState.Complete;
                LastSdkMessage = "IKapSpectralConfocal 已停止采集。";
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            lock (_syncRoot)
            {
                List<MeasureData> result = new List<MeasureData>();
                if (_stream == null)
                {
                    LastSdkMessage = "IKapSpectralConfocal 接收失败：数据流未初始化。";
                    return result;
                }

                ITKBUFFER buffer = new ITKBUFFER();
                uint res = IKapC.ItkStreamWaitOneFrameReady(_stream, ref buffer, GrabTimeoutMs);
                if (!CheckStatus(res, "ItkStreamWaitOneFrameReady"))
                {
                    return result;
                }

                ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                res = IKapC.ItkBufferGetInfo(buffer, bufferInfo);
                if (!CheckStatus(res, "ItkBufferGetInfo"))
                {
                    return result;
                }

                _currentBuffer = buffer;
                _currentBufferInfo = bufferInfo;
                result = ConvertBufferToMeasureData(buffer, bufferInfo);
                LastSdkMessage = $"IKapSpectralConfocal 已接收一帧，行数：{result.Count}。";
                return result;
            }
        }

        public override bool SettingParam(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            lock (_syncRoot)
            {
                try
                {
                    switch (key)
                    {
                        case IKapSpectralConfocalSdkNames.PixelFormat:
                            PixelFormat = Convert.ToString(value) ?? PixelFormat;
                            return SetEnumFeature(key, PixelFormat);
                        case IKapSpectralConfocalSdkNames.LayerSelector:
                            LayerSelector = Convert.ToString(value) ?? LayerSelector;
                            return SetEnumFeature(key, LayerSelector);
                        case IKapSpectralConfocalSdkNames.LayerNumber:
                            LayerNumber = Convert.ToInt64(value);
                            return SetInt64Feature(key, LayerNumber);
                        case IKapSpectralConfocalSdkNames.YSpacing:
                            YSpacing = Convert.ToDouble(value);
                            return SetDoubleFeature(key, YSpacing);
                        case IKapSpectralConfocalSdkNames.AcquisitionLineRateEnable:
                            AcquisitionLineRateEnabled = Convert.ToBoolean(value);
                            return SetBoolFeature(key, AcquisitionLineRateEnabled);
                        case IKapSpectralConfocalSdkNames.AcquisitionLineRate:
                            AcquisitionLineRate = Convert.ToDouble(value);
                            return SetDoubleFeature(key, AcquisitionLineRate);
                        case IKapSpectralConfocalSdkNames.AcquisitionLineCount:
                            AcquisitionLineCount = Convert.ToUInt32(value);
                            return SetInt64Feature(key, AcquisitionLineCount);
                        case IKapSpectralConfocalSdkNames.ExposureTime:
                            ExposureTime = Convert.ToDouble(value);
                            return SetDoubleFeature(key, ExposureTime);
                        case IKapSpectralConfocalSdkNames.TriggerMode:
                        case IKapSpectralConfocalSdkNames.TriggerSelector:
                            return SetEnumFeature(key, Convert.ToString(value) ?? string.Empty);
                        default:
                            LastSdkMessage = $"IKapSpectralConfocal 不支持参数：{key}";
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    LastSdkMessage = $"IKapSpectralConfocal 参数设置异常：{ex.Message}";
                    return false;
                }
            }
        }

        private bool TryResolveDeviceIndex(out uint openIndex)
        {
            openIndex = DeviceIndex;
            uint deviceCount = 0;
            uint res = IKapC.ItkManGetDeviceCount(ref deviceCount);
            if (!CheckStatus(res, "ItkManGetDeviceCount"))
            {
                return false;
            }

            if (deviceCount == 0)
            {
                LastSdkMessage = "IKapSpectralConfocal 未发现设备。";
                return false;
            }

            for (uint index = 0; index < deviceCount; index++)
            {
                ITKDEV_INFO deviceInfo = new ITKDEV_INFO();
                res = IKapC.ItkManGetDeviceInfo(index, deviceInfo);
                if (!CheckStatus(res, "ItkManGetDeviceInfo"))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(SerialNumber) || string.Equals(deviceInfo.SerialNumber, SerialNumber, StringComparison.OrdinalIgnoreCase))
                {
                    openIndex = index;
                    SerialNumber = deviceInfo.SerialNumber;
                    DeviceFullName = deviceInfo.FullName;
                    DeviceClass = deviceInfo.DeviceClass;
                    return true;
                }
            }

            LastSdkMessage = $"IKapSpectralConfocal 未找到序列号：{SerialNumber}";
            return false;
        }

        private bool ApplyCameraParameters()
        {
            if (_device == null)
            {
                return false;
            }

            if (!SetEnumFeature(IKapSpectralConfocalSdkNames.LayerSelector, LayerSelector)) return false;
            if (LayerSelector == IKapSpectralConfocalSdkNames.LayerSelectorManual && !SetInt64Feature(IKapSpectralConfocalSdkNames.LayerNumber, LayerNumber)) return false;
            if (!SetEnumFeature(IKapSpectralConfocalSdkNames.PixelFormat, PixelFormat)) return false;
            if (!ApplyYSpacing()) return false;
            if (!SetBoolFeature(IKapSpectralConfocalSdkNames.AcquisitionLineRateEnable, AcquisitionLineRateEnabled)) return false;
            if (!SetDoubleFeature(IKapSpectralConfocalSdkNames.AcquisitionLineRate, AcquisitionLineRate)) return false;
            if (!SetInt64Feature(IKapSpectralConfocalSdkNames.AcquisitionLineCount, AcquisitionLineCount)) return false;
            if (!SetDoubleFeature(IKapSpectralConfocalSdkNames.ExposureTime, ExposureTime)) return false;
            return ApplyFreeRunTriggerMode();
        }

        private bool ApplyYSpacing()
        {
            if (YSpacing >= 0)
            {
                return SetDoubleFeature(IKapSpectralConfocalSdkNames.YSpacing, YSpacing);
            }

            if (_device == null)
            {
                return false;
            }

            double profileXUnit = 0.0;
            long xStep = 0L;
            uint res = IKapC.ItkDevGetDouble(_device, IKapSpectralConfocalSdkNames.ProfileXUnit, ref profileXUnit);
            if (!CheckStatus(res, "ItkDevGetDouble ProfileXUnit")) return false;
            res = IKapC.ItkDevGetInt64(_device, IKapSpectralConfocalSdkNames.XStep, ref xStep);
            if (!CheckStatus(res, "ItkDevGetInt64 XStep")) return false;
            return SetDoubleFeature(IKapSpectralConfocalSdkNames.YSpacing, profileXUnit * xStep);
        }

        private bool ApplyFreeRunTriggerMode()
        {
            if (!SetEnumFeature(IKapSpectralConfocalSdkNames.TriggerSelector, IKapSpectralConfocalSdkNames.TriggerSelectorFrameStart)) return false;
            if (!SetEnumFeature(IKapSpectralConfocalSdkNames.TriggerMode, IKapSpectralConfocalSdkNames.TriggerModeOff)) return false;
            if (!SetEnumFeature(IKapSpectralConfocalSdkNames.TriggerSelector, IKapSpectralConfocalSdkNames.TriggerSelectorLineStart)) return false;
            return SetEnumFeature(IKapSpectralConfocalSdkNames.TriggerMode, IKapSpectralConfocalSdkNames.TriggerModeOff);
        }

        private bool CreateStream()
        {
            if (_device == null)
            {
                return false;
            }

            uint streamCount = 0;
            uint res = IKapC.ItkDevGetStreamCount(_device, ref streamCount);
            if (!CheckStatus(res, "ItkDevGetStreamCount") || streamCount == 0)
            {
                LastSdkMessage = streamCount == 0 ? "IKapSpectralConfocal 设备没有图像流通道。" : LastSdkMessage;
                return false;
            }

            ITKSTREAM stream = new ITKSTREAM();
            res = IKapC.ItkDevAllocStreamEx(_device, 0, IKapSpectralConfocalSdkNames.DefaultBufferCount, ref stream);
            if (!CheckStatus(res, "ItkDevAllocStreamEx"))
            {
                return false;
            }

            _stream = stream;
            if (!SetStreamUInt32Prm(IKapC.ITKSTREAM_PRM_TRANSFER_MODE, IKapC.ITKSTREAM_VAL_TRANSFER_MODE_SYNCHRONOUS_WITH_PROTECT)) return false;
            if (!SetStreamUInt32Prm(IKapC.ITKSTREAM_PRM_TIME_OUT, unchecked((uint)-1))) return false;
            return SetStreamUInt32Prm(IKapC.ITKSTREAM_PRM_SELF_ADAPTION, 1U);
        }

        private List<MeasureData> ConvertBufferToMeasureData(ITKBUFFER buffer, ITK_BUFFER_INFO bufferInfo)
        {
            List<MeasureData> result = new List<MeasureData>();
            if (!TryGetFrameSize(bufferInfo, out long width, out long height) || width <= 0 || height <= 0)
            {
                return result;
            }

            ReleaseSplitBuffer();
            uint splitFormat = bufferInfo.PixelFormat == IKapC.ITKBUFFER_VAL_FORMAT_CALIBRATED_COORD3D_AC32
                ? IKapC.ITKBUFFER_VAL_FORMAT_DATA_32_WITH_8U
                : IKapC.ITKBUFFER_VAL_FORMAT_DATA_16_WITH_8U;

            ITKBUFFER splitBuffer = new ITKBUFFER();
            uint res = IKapC.ItkBufferNew(width, height, splitFormat, ref splitBuffer);
            if (!CheckStatus(res, "ItkBufferNew"))
            {
                return result;
            }

            _splitBuffer = splitBuffer;
            res = IKapC.ItkBuffer3DSplit(buffer, splitBuffer, IKapC.ITKBUFFER_VAL_3D_SPLIT_INTO_BLOCK_AND_STRATIFIED, AcquisitionLineCount);
            if (!CheckStatus(res, "ItkBuffer3DSplit"))
            {
                return result;
            }

            ITK_BUFFER_INFO splitInfo = new ITK_BUFFER_INFO();
            res = IKapC.ItkBufferGetInfo(splitBuffer, splitInfo);
            if (!CheckStatus(res, "ItkBufferGetInfo Split"))
            {
                return result;
            }

            long pixelCount = width * height;
            long dataALen = bufferInfo.PixelFormat == IKapC.ITKBUFFER_VAL_FORMAT_CALIBRATED_COORD3D_AC32 ? pixelCount * Int16Size : 0L;
            long dataCLen = pixelCount * Int16Size;
            IntPtr heightPtr = IntPtr.Add(splitInfo.BaseAddress, checked((int)dataALen));
            IntPtr grayPtr = IntPtr.Add(heightPtr, checked((int)dataCLen));

            for (int row = 0; row < height; row++)
            {
                float[] heightRow = new float[width];
                float[] grayRow = new float[width];
                long rowOffset = row * width;
                for (int column = 0; column < width; column++)
                {
                    long index = rowOffset + column;
                    heightRow[column] = Marshal.ReadInt16(heightPtr, checked((int)(index * Int16Size)));
                    grayRow[column] = Marshal.ReadByte(grayPtr, checked((int)(index * ByteSize)));
                }

                result.Add(new MeasureData
                {
                    AreaData = new List<float[]> { heightRow, grayRow },
                    RTime = DateTime.Now,
                    IsValid = true
                });
            }

            return result;
        }

        private bool TryGetFrameSize(ITK_BUFFER_INFO bufferInfo, out long width, out long height)
        {
            width = 0L;
            height = 0L;
            if (_device != null)
            {
                IKapC.ItkDevGetInt64(_device, IKapSpectralConfocalSdkNames.Width, ref width);
                IKapC.ItkDevGetInt64(_device, IKapSpectralConfocalSdkNames.Height, ref height);
            }

            if (width <= 0 && bufferInfo.ImageWidth <= long.MaxValue)
            {
                width = (long)bufferInfo.ImageWidth;
            }
            if (height <= 0 && bufferInfo.ImageHeight <= long.MaxValue)
            {
                height = (long)bufferInfo.ImageHeight;
            }

            return width > 0 && height > 0;
        }

        private bool SetEnumFeature(string name, string value)
        {
            if (_device == null)
            {
                return true;
            }

            uint res = IKapC.ItkDevFromString(_device, name, value);
            return CheckStatus(res, $"ItkDevFromString {name}");
        }

        private bool SetDoubleFeature(string name, double value)
        {
            if (_device == null)
            {
                return true;
            }

            uint res = IKapC.ItkDevSetDouble(_device, name, value);
            return CheckStatus(res, $"ItkDevSetDouble {name}");
        }

        private bool SetInt64Feature(string name, long value)
        {
            if (_device == null)
            {
                return true;
            }

            uint res = IKapC.ItkDevSetInt64(_device, name, value);
            return CheckStatus(res, $"ItkDevSetInt64 {name}");
        }

        private bool SetBoolFeature(string name, bool value)
        {
            if (_device == null)
            {
                return true;
            }

            uint res = IKapC.ItkDevSetBool(_device, name, value);
            return CheckStatus(res, $"ItkDevSetBool {name}");
        }

        private bool SetStreamUInt32Prm(uint prm, uint value)
        {
            if (_stream == null)
            {
                return false;
            }

            IntPtr valuePtr = Marshal.AllocHGlobal(sizeof(uint));
            try
            {
                Marshal.WriteInt32(valuePtr, unchecked((int)value));
                uint res = IKapC.ItkStreamSetPrm(_stream, prm, valuePtr);
                return CheckStatus(res, $"ItkStreamSetPrm {prm}");
            }
            finally
            {
                Marshal.FreeHGlobal(valuePtr);
            }
        }

        private void StopStream()
        {
            if (_stream == null)
            {
                return;
            }

            if (!_isStreamStarted)
            {
                return;
            }

            uint res = IKapC.ItkStreamStop(_stream);
            CheckStatus(res, "ItkStreamStop");
            _isStreamStarted = false;
        }

        private void ReleaseSdkHandles(bool updateState)
        {
            StopStream();
            if (_stream != null)
            {
                IKapC.ItkDevFreeStream(_stream);
                _stream = null;
            }

            ReleaseSplitBuffer();
            if (_device != null)
            {
                IKapC.ItkDevClose(_device);
                _device = null;
            }

            if (_sdkInitialized)
            {
                IKapC.ItkManTerminate();
                _sdkInitialized = false;
            }

            _currentBuffer = null;
            _currentBufferInfo = null;
            _isStreamStarted = false;
            IsConnected = false;
            if (updateState)
            {
                State = HardwareState.Closed;
            }
        }

        private void ReleaseSplitBuffer()
        {
            if (_splitBuffer == null)
            {
                return;
            }

            IKapC.ItkBufferFree(_splitBuffer);
            _splitBuffer = null;
        }

        private bool CheckStatus(uint status, string action)
        {
            if (status == IKapC.ITKSTATUS_OK)
            {
                return true;
            }

            LastSdkMessage = $"IKapSpectralConfocal {action} 失败，Status=0x{status:X8}";
            return false;
        }
    }
}

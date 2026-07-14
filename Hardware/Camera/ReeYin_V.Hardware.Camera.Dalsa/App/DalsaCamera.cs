using DALSA.SaperaLT.SapClassBasic;
using HalconDotNet;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Models;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace ReeYin_V.Hardware.Camera.Dalsa
{
    [Category("相机")]
    [DisplayName("Dalsa相机")]
    [Serializable]
    public partial class DalsaCamera : CameraBase
    {
        #region Fields

        [NonSerialized]
        private SapLocation _location;

        [NonSerialized]
        private SapAcqDevice _acqDevice;

        [NonSerialized]
        private SapBuffer _buffers;

        [NonSerialized]
        private SapTransfer _xfer;

        [NonSerialized]
        private object _syncRoot = new object();

        public string ServerName { get; set; } = string.Empty;

        public int ResourceIndex { get; set; }

        public string ConfigFileName { get; set; } = string.Empty;

        public int BufferCount { get; set; } = 2;

        public string HardwareTriggerSource { get; set; } = "Line1";

        #endregion

        #region Constructor

        public DalsaCamera() : base()
        {
        }

        #endregion

        #region Event

        private void OnXferNotify(object sender, SapXferNotifyEventArgs args)
        {
            try
            {
                if (args.Trash || _buffers == null || !_buffers.Initialized)
                    return;

                HImage hImage = CreateHalconImageFromCurrentBuffer();
                if (hImage == null || !hImage.IsInitialized())
                    return;

                Image = hImage;
                Width = _buffers.Width;
                Height = _buffers.Height;
                ImageGrab?.Invoke(Image);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa OnXferNotify failed: {0}", ex.Message);
            }
            finally
            {
                EventWait?.Set();
                GetSignalWait?.Set();
            }
        }

        #endregion

        #region Methods

        private HImage CreateHalconImageFromCurrentBuffer()
        {
            _buffers.GetAddress(out IntPtr imagePtr);
            if (imagePtr == IntPtr.Zero)
                return null;

            string formatName = _buffers.Format.ToString();
            var hImage = new HImage();

            if (formatName.IndexOf("Mono16", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hImage.GenImage1("uint2", _buffers.Width, _buffers.Height, imagePtr);
            }
            else if (formatName.IndexOf("RGB", StringComparison.OrdinalIgnoreCase) >= 0
                || formatName.IndexOf("BGR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string colorOrder = formatName.IndexOf("BGR", StringComparison.OrdinalIgnoreCase) >= 0 ? "bgr" : "rgb";
                hImage.GenImageInterleaved(imagePtr, colorOrder, _buffers.Width, _buffers.Height, -1, "byte", 0, 0, 0, 0, -1, 0);
            }
            else
            {
                hImage.GenImage1("byte", _buffers.Width, _buffers.Height, imagePtr);
            }

            return hImage;
        }

        private DalsaCameraInfo ResolveCameraInfo()
        {
            if (ExtInfo is DalsaCameraInfo extInfo)
                return extInfo;

            if (!string.IsNullOrWhiteSpace(ServerName))
            {
                return new DalsaCameraInfo
                {
                    ServerName = ServerName,
                    ResourceIndex = ResourceIndex,
                    SerialNumber = SerialNo ?? string.Empty,
                    ConfigFileName = ConfigFileName ?? string.Empty
                };
            }

            if (!string.IsNullOrWhiteSpace(SerialNo))
                return FindCameraInfoBySerialNo(SerialNo);

            return null;
        }

        private DalsaCameraInfo FindCameraInfoBySerialNo(string serialNo)
        {
            foreach (CameraInfoModel cameraInfo in SearchCameras())
            {
                if (string.Equals(cameraInfo.SerialNO, serialNo, StringComparison.OrdinalIgnoreCase)
                    && cameraInfo.ExtInfo is DalsaCameraInfo dalsaInfo)
                {
                    return dalsaInfo;
                }
            }

            return null;
        }

        private DalsaCameraInfo CreateCameraInfo(string serverName, int resourceIndex)
        {
            SapLocation location = null;
            SapAcqDevice device = null;
            bool created = false;

            try
            {
                location = new SapLocation(serverName, resourceIndex);
                device = new SapAcqDevice(location);
                created = device.Create();
                if (!created)
                    return null;

                string serial = GetStringFeature(device, "DeviceSerialNumber");
                string userDefinedName = GetStringFeature(device, "DeviceUserID");
                string vendor = GetStringFeature(device, "DeviceVendorName");
                string model = GetStringFeature(device, "DeviceModelName");

                return new DalsaCameraInfo
                {
                    ServerName = serverName,
                    ResourceIndex = resourceIndex,
                    SerialNumber = serial,
                    UserDefinedName = userDefinedName,
                    VendorName = vendor,
                    ModelName = model,
                    ConfigFileName = string.Empty
                };
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa CreateCameraInfo failed: {0}", ex.Message);
                return null;
            }
            finally
            {
                if (device != null)
                {
                    if (created && device.Initialized)
                        device.Destroy();
                    device.Dispose();
                }

                location?.Dispose();
            }
        }

        private void ApplyCameraInfo(DalsaCameraInfo info)
        {
            ServerName = info.ServerName;
            ResourceIndex = info.ResourceIndex;
            ConfigFileName = string.IsNullOrWhiteSpace(info.ConfigFileName) ? ConfigFileName ?? string.Empty : info.ConfigFileName;

            if (!string.IsNullOrWhiteSpace(info.SerialNumber))
                SerialNo = info.SerialNumber;

            CameraType = string.IsNullOrWhiteSpace(info.ModelName) ? "Dalsa" : info.ModelName;
            ExtInfo = info;
        }

        private void RefreshCameraProperties()
        {
            string serial = GetStringFeature(_acqDevice, "DeviceSerialNumber");
            if (!string.IsNullOrWhiteSpace(serial))
                SerialNo = serial;

            string model = GetStringFeature(_acqDevice, "DeviceModelName");
            if (!string.IsNullOrWhiteSpace(model))
                CameraType = model;

            int width = GetIntFeature(_acqDevice, "Width");
            int height = GetIntFeature(_acqDevice, "Height");
            int sensorWidth = GetIntFeature(_acqDevice, "SensorWidth");
            int sensorHeight = GetIntFeature(_acqDevice, "SensorHeight");

            if (sensorWidth > 0)
                WidthMax = sensorWidth;
            if (sensorHeight > 0)
                HeightMax = sensorHeight;
            if (width > 0)
                Width = width;
            if (height > 0)
                Height = height;

            double frameRate = GetDoubleFeature(_acqDevice, "AcquisitionFrameRate", "ResultingFrameRate");
            if (frameRate > 0)
                Framerate = frameRate.ToString("0.###", CultureInfo.InvariantCulture);

            string scanType = GetEnumFeatureText("DeviceScanType");
            IsLineScan = scanType.IndexOf("Line", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetStringFeature(SapAcqDevice device, params string[] featureNames)
        {
            if (device == null || !device.Initialized)
                return string.Empty;

            foreach (string featureName in featureNames)
            {
                try
                {
                    if (device.IsFeatureAvailable(featureName) && device.GetFeatureValue(featureName, out string value))
                        return value ?? string.Empty;
                }
                catch
                {
                    // Some features are not string typed; try the next candidate.
                }
            }

            return string.Empty;
        }

        private static int GetIntFeature(SapAcqDevice device, params string[] featureNames)
        {
            if (device == null || !device.Initialized)
                return 0;

            foreach (string featureName in featureNames)
            {
                try
                {
                    if (device.IsFeatureAvailable(featureName) && device.GetFeatureValue(featureName, out int value))
                        return value;
                }
                catch
                {
                    // Try the next candidate.
                }
            }

            return 0;
        }

        private static double GetDoubleFeature(SapAcqDevice device, params string[] featureNames)
        {
            if (device == null || !device.Initialized)
                return 0d;

            foreach (string featureName in featureNames)
            {
                try
                {
                    if (device.IsFeatureAvailable(featureName) && device.GetFeatureValue(featureName, out double value))
                        return value;
                }
                catch
                {
                    // Try the next candidate.
                }
            }

            return 0d;
        }

        private string GetEnumFeatureText(string featureName)
        {
            if (_acqDevice == null || !_acqDevice.Initialized || !_acqDevice.IsFeatureAvailable(featureName))
                return string.Empty;

            SapFeature feature = null;

            try
            {
                feature = new SapFeature(_location);
                if (!feature.Create())
                    return string.Empty;

                if (!_acqDevice.GetFeatureInfo(featureName, feature))
                    return string.Empty;

                if (!_acqDevice.GetFeatureValue(featureName, out int enumValue))
                    return string.Empty;

                return feature.GetEnumTextFromValue(enumValue, out string enumText) ? enumText : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (feature != null)
                {
                    if (feature.Initialized)
                        feature.Destroy();
                    feature.Dispose();
                }
            }
        }

        private bool TrySetFeatureValue(string featureName, int value)
        {
            return IsWritableFeature(featureName) && _acqDevice.SetFeatureValue(featureName, value);
        }

        private bool TrySetFeatureValue(string featureName, long value)
        {
            return IsWritableFeature(featureName) && _acqDevice.SetFeatureValue(featureName, value);
        }

        private bool TrySetFeatureValue(string featureName, double value)
        {
            return IsWritableFeature(featureName) && _acqDevice.SetFeatureValue(featureName, value);
        }

        private bool TrySetFeatureValue(string featureName, string value)
        {
            return IsWritableFeature(featureName) && _acqDevice.SetFeatureValue(featureName, value ?? string.Empty);
        }

        private bool TrySetOptionalFeatureValue(string featureName, string value)
        {
            if (_acqDevice == null || !_acqDevice.Initialized || !_acqDevice.IsFeatureAvailable(featureName))
                return true;

            return _acqDevice.SetFeatureValue(featureName, value ?? string.Empty);
        }

        private bool ExecuteFeatureCommand(string featureName)
        {
            if (_acqDevice == null || !_acqDevice.Initialized || !_acqDevice.IsFeatureAvailable(featureName))
                return false;

            return _acqDevice.SetFeatureValue(featureName, 1);
        }

        private bool IsWritableFeature(string featureName)
        {
            return _acqDevice != null
                && _acqDevice.Initialized
                && !string.IsNullOrWhiteSpace(featureName)
                && _acqDevice.IsFeatureAvailable(featureName);
        }

        private void DisConnectSaperaObjects()
        {
            try
            {
                if (_xfer != null)
                {
                    _xfer.XferNotify -= OnXferNotify;

                    if (_xfer.Initialized && _xfer.Grabbing)
                    {
                        _xfer.Freeze();
                        _xfer.Wait(1000);
                    }

                    if (_xfer.Initialized)
                        _xfer.Destroy();

                    _xfer.Dispose();
                    _xfer = null;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa transfer release failed: {0}", ex.Message);
            }

            try
            {
                if (_buffers != null)
                {
                    if (_buffers.Initialized)
                        _buffers.Destroy();

                    _buffers.Dispose();
                    _buffers = null;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa buffer release failed: {0}", ex.Message);
            }

            try
            {
                if (_acqDevice != null)
                {
                    if (_acqDevice.Initialized)
                        _acqDevice.Destroy();

                    _acqDevice.Dispose();
                    _acqDevice = null;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa device release failed: {0}", ex.Message);
            }

            try
            {
                _location?.Dispose();
                _location = null;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa location release failed: {0}", ex.Message);
            }
        }

        [OnDeserializing]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            _syncRoot = new object();
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            _syncRoot = new object();

            try
            {
                if (string.IsNullOrWhiteSpace(SerialNo))
                    return;

                DalsaCameraInfo info = FindCameraInfoBySerialNo(SerialNo);
                if (info == null)
                    return;

                ExtInfo = info;
                ApplyCameraInfo(info);
                ConnectDev();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa deserialization reconnect failed: {0}", ex.Message);
            }
        }
        #endregion
    }
}

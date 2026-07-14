using DALSA.SaperaLT.SapClassBasic;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Models;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace ReeYin_V.Hardware.Camera.Dalsa
{
    public partial class DalsaCamera : CameraBase
    {
        #region Overriden Methods
        public override List<CameraInfoModel> SearchCameras()
        {
            var cameras = new List<CameraInfoModel>();

            try
            {
                SapManager.DisplayStatusMode = SapManager.StatusMode.Log;

                int serverCount = SapManager.GetServerCount();
                for (int serverIndex = 0; serverIndex < serverCount; serverIndex++)
                {
                    if (SapManager.GetResourceCount(serverIndex, SapManager.ResourceType.AcqDevice) <= 0)
                        continue;

                    string serverName = SapManager.GetServerName(serverIndex);
                    int cameraCount = SapManager.GetResourceCount(serverName, SapManager.ResourceType.AcqDevice);
                    for (int resourceIndex = 0; resourceIndex < cameraCount; resourceIndex++)
                    {
                        DalsaCameraInfo info = CreateCameraInfo(serverName, resourceIndex);
                        if (info == null)
                            continue;

                        string serial = string.IsNullOrWhiteSpace(info.SerialNumber)
                            ? $"{info.ServerName}:{info.ResourceIndex}"
                            : info.SerialNumber;

                        cameras.Add(new CameraInfoModel
                        {
                            CamName = $"Dalsa: {info}",
                            SerialNO = serial,
                            MaskName = serial,
                            CameraIP = string.Empty,
                            Connected = false,
                            ExtInfo = info
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa SearchCameras failed: {0}", ex.Message);
            }

            return cameras;
        }

        public override void ConnectDev()
        {
            lock (_syncRoot)
            {
                try
                {
                    DisConnectSaperaObjects();

                    DalsaCameraInfo info = ResolveCameraInfo();
                    if (info == null)
                    {
                        Connected = false;
                        return;
                    }

                    ApplyCameraInfo(info);
                    _location = new SapLocation(ServerName, ResourceIndex);
                    _acqDevice = new SapAcqDevice(_location, ConfigFileName ?? string.Empty);

                    if (!_acqDevice.Create())
                        throw new InvalidOperationException("SapAcqDevice.Create failed.");

                    RefreshCameraProperties();
                    SetSetting();

                    SapBuffer.MemoryType memoryType = SapBuffer.IsBufferTypeSupported(_location, SapBuffer.MemoryType.ScatterGather)
                        ? SapBuffer.MemoryType.ScatterGather
                        : SapBuffer.MemoryType.ScatterGatherPhysical;

                    int bufferCount = Math.Max(2, BufferCount);
                    _buffers = new SapBufferWithTrash(bufferCount, _acqDevice, memoryType);
                    _xfer = new SapAcqDeviceToBuf(_acqDevice, _buffers);
                    _xfer.Pairs[0].EventType = SapXferPair.XferEventType.EndOfFrame;
                    _xfer.Pairs[0].Cycle = SapXferPair.CycleMode.NextWithTrash;
                    _xfer.XferNotify += OnXferNotify;
                    _xfer.XferNotifyContext = this;

                    if (!_buffers.Create())
                        throw new InvalidOperationException("SapBuffer.Create failed.");

                    _buffers.Clear();

                    if (!_xfer.Create())
                        throw new InvalidOperationException("SapTransfer.Create failed.");

                    if (!_xfer.Grab())
                        throw new InvalidOperationException("SapTransfer.Grab failed.");

                    Connected = true;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Dalsa ConnectDev failed: {0}", ex.Message);
                    Connected = false;
                    DisConnectSaperaObjects();
                }
                finally
                {
                    base.ConnectDev();
                }
            }
        }

        public override void DisConnectDev()
        {
            lock (_syncRoot)
            {
                DisConnectSaperaObjects();
                Connected = false;
            }

            base.DisConnectDev();
        }

        public override bool CaptureImage(bool byHand)
        {
            try
            {
                if (!Connected)
                    ConnectDev();

                if (!Connected || _xfer == null)
                    return false;

                if (byHand)
                {
                    eTrigMode oldMode = TrigMode;
                    SetTriggerMode(eTrigMode.软触发);
                    bool result = ExecuteFeatureCommand("TriggerSoftware");
                    SetTriggerMode(oldMode);
                    return result;
                }

                if (TrigMode == eTrigMode.软触发)
                    return ExecuteFeatureCommand("TriggerSoftware");

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa CaptureImage failed: {0}", ex.Message);
                EventWait?.Set();
                return false;
            }
        }

        public override bool StartCollect()
        {
            try
            {
                if (!Connected)
                    ConnectDev();

                if (_xfer == null)
                    return false;

                return _xfer.Grabbing || _xfer.Grab();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa StartCollect failed: {0}", ex.Message);
                return false;
            }
        }

        public override bool EndCollect()
        {
            try
            {
                if (_xfer == null || !_xfer.Grabbing)
                    return true;

                bool result = _xfer.Freeze();
                _xfer.Wait(1000);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa EndCollect failed: {0}", ex.Message);
                return false;
            }
        }

        public override bool SoftTriggerOnce()
        {
            return ExecuteFeatureCommand("TriggerSoftware");
        }

        public override void SetSetting()
        {
            if (_acqDevice == null || !_acqDevice.Initialized)
                return;

            if (Width > 0)
                TrySetFeatureValue("Width", Width);

            if (Height > 0)
                TrySetFeatureValue("Height", Height);

            if (Config.ExposeTime > 0)
                TrySetFeatureValue("ExposureTime", Convert.ToDouble(Config.ExposeTime, CultureInfo.InvariantCulture));

            if (Config.Gain > 0)
                TrySetFeatureValue("Gain", Convert.ToDouble(Config.Gain, CultureInfo.InvariantCulture));

            if (Config.LineRate > 0)
                TrySetFeatureValue("AcquisitionLineRate", Convert.ToDouble(Config.LineRate, CultureInfo.InvariantCulture));

            SetTriggerMode(TrigMode);
        }

        public override void CameraChanged(ChangType changTyp)
        {
            try
            {
                switch (changTyp)
                {
                    case ChangType.增益:
                        TrySetFeatureValue("Gain", Convert.ToDouble(Config.Gain, CultureInfo.InvariantCulture));
                        break;
                    case ChangType.曝光:
                        TrySetFeatureValue("ExposureTime", Convert.ToDouble(Config.ExposeTime, CultureInfo.InvariantCulture));
                        break;
                    case ChangType.宽度:
                        TrySetFeatureValue("Width", Width);
                        break;
                    case ChangType.高度:
                        TrySetFeatureValue("Height", Height);
                        break;
                    case ChangType.触发:
                        SetTriggerMode(TrigMode);
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa CameraChanged failed: {0}", ex.Message);
            }
        }

        public override bool SetSpecifiedParam(string type, string key, object value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                string normalizedType = type?.Trim() ?? string.Empty;
                switch (normalizedType)
                {
                    case "Int":
                        return TrySetFeatureValue(key, Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    case "Int64":
                        return TrySetFeatureValue(key, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    case "Float":
                    case "Double":
                        return TrySetFeatureValue(key, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    case "Enum":
                    case "String":
                        return TrySetFeatureValue(key, Convert.ToString(value, CultureInfo.InvariantCulture));
                    case "Bool":
                        return TrySetFeatureValue(key, Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? 1 : 0);
                    case "Command":
                        return ExecuteFeatureCommand(key);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa SetSpecifiedParam failed: {0}", ex.Message);
                return false;
            }
        }

        public override bool GetSpecifiedParam(string type, string key, ref object value)
        {
            try
            {
                if (_acqDevice == null || !_acqDevice.Initialized || string.IsNullOrWhiteSpace(key))
                    return false;

                if (!_acqDevice.IsFeatureAvailable(key))
                    return false;

                string normalizedType = type?.Trim() ?? string.Empty;
                switch (normalizedType)
                {
                    case "Int":
                        int intValue;
                        if (!_acqDevice.GetFeatureValue(key, out intValue))
                            return false;
                        value = intValue;
                        return true;
                    case "Int64":
                        long longValue;
                        if (!_acqDevice.GetFeatureValue(key, out longValue))
                            return false;
                        value = longValue;
                        return true;
                    case "Float":
                    case "Double":
                        double doubleValue;
                        if (!_acqDevice.GetFeatureValue(key, out doubleValue))
                            return false;
                        value = doubleValue;
                        return true;
                    case "Enum":
                        value = GetEnumFeatureText(key);
                        return !string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture));
                    case "String":
                        string stringValue;
                        if (!_acqDevice.GetFeatureValue(key, out stringValue))
                            return false;
                        value = stringValue;
                        return true;
                    case "Bool":
                        int boolValue;
                        if (!_acqDevice.GetFeatureValue(key, out boolValue))
                            return false;
                        value = boolValue != 0;
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa GetSpecifiedParam failed: {0}", ex.Message);
                return false;
            }
        }

        public override bool SetTriggerMode(eTrigMode mode)
        {
            if (_acqDevice == null || !_acqDevice.Initialized)
                return false;

            try
            {
                TrySetOptionalFeatureValue("TriggerSelector", "FrameStart");

                switch (mode)
                {
                    case eTrigMode.内触发:
                        return TrySetOptionalFeatureValue("TriggerMode", "Off");
                    case eTrigMode.软触发:
                        return TrySetOptionalFeatureValue("TriggerMode", "On")
                            && TrySetOptionalFeatureValue("TriggerSource", "Software");
                    case eTrigMode.上升沿:
                        return TrySetOptionalFeatureValue("TriggerMode", "On")
                            && TrySetOptionalFeatureValue("TriggerSource", HardwareTriggerSource)
                            && TrySetOptionalFeatureValue("TriggerActivation", "RisingEdge");
                    case eTrigMode.下降沿:
                        return TrySetOptionalFeatureValue("TriggerMode", "On")
                            && TrySetOptionalFeatureValue("TriggerSource", HardwareTriggerSource)
                            && TrySetOptionalFeatureValue("TriggerActivation", "FallingEdge");
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dalsa SetTriggerMode failed: {0}", ex.Message);
                return false;
            }
        }

        public override bool SetLineRate(float lineRate)
        {
            return TrySetFeatureValue("AcquisitionLineRate", Convert.ToDouble(lineRate, CultureInfo.InvariantCulture));
        }

        public override float GetLineRate()
        {
            object value = 0d;
            if (GetSpecifiedParam("Double", "AcquisitionLineRate", ref value))
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);

            return 0f;
        }

        #endregion
    }
}
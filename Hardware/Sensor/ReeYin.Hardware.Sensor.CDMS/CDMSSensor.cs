using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using SM_CDMS;
using SM_CDMS3010;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin.Hardware.Sensor.CDMS
{
    public class CDMSSensor : SensorBase
    {
        private const int MaxChannelCount = 8;
        private readonly object _syncRoot = new();
        private readonly CDMS3010_V1_1 _device = new();
        private byte[] _remainData = new byte[512];
        private int _remainDataLength;
        private bool _isCollecting;

        public CDMSSensor()
        {
            IP = "192.168.1.111";
            Port = 8011;
            NickName = "CDMS";
            VenderName = "SmartMeasurement";
            VenderType = "CDMS";
        }

        public CDMSSensorConfig Config { get; set; } = new();

        [JsonIgnore]
        public float[] LastData
        {
            get => Config.LastData;
            private set
            {
                Config.LastData = value;
                RaisePropertyChanged();
            }
        }

        public override bool Init()
        {
            lock (_syncRoot)
            {
                if (IsConnected)
                {
                    State = HardwareState.Connected;
                    return true;
                }

                State = HardwareState.Connecting;
                string localAddress = ResolveLocalAddress();
                if (string.IsNullOrWhiteSpace(localAddress))
                {
                    SetMessage("未找到可用于连接CDMS的本地IP。");
                    State = HardwareState.NotConnected;
                    IsConnected = false;
                    return false;
                }

                int result = _device.SM_CDMS30xx_ETH_Open(localAddress, IP, checked((short)Port));
                if (result != SM_CDMS3010_API_Result.Success)
                {
                    SetMessage($"CDMS连接失败：{result}");
                    State = HardwareState.NotConnected;
                    IsConnected = false;
                    return false;
                }

                Config.LocalAddress = localAddress;
                State = HardwareState.Connected;
                IsConnected = true;
                SetMessage("CDMS连接成功。");
                RefreshDeviceParameters();
                return true;
            }
        }

        public override void Close()
        {
            lock (_syncRoot)
            {
                if (_isCollecting)
                {
                    StopCollect();
                }

                _device.SM_CDMS30xx_ETH_Close();
                IsConnected = false;
                State = HardwareState.Closed;
                SetMessage("CDMS已断开。");
            }
        }

        public override void StartCollect()
        {
            lock (_syncRoot)
            {
                if (!EnsureConnected())
                {
                    return;
                }

                int result = _device.SM_CDMS30xx_ETH_AcqStart_BackImmediately();
                if (result != SM_CDMS3010_API_Result.Success)
                {
                    SetMessage($"CDMS开始采集失败：{result}");
                    State = HardwareState.Error;
                    return;
                }

                _isCollecting = true;
                _remainDataLength = 0;
                State = HardwareState.Running;
                SetMessage("CDMS连续采集已开始。");
            }
        }

        public override void StopCollect()
        {
            lock (_syncRoot)
            {
                if (!_isCollecting)
                {
                    State = IsConnected ? HardwareState.Complete : HardwareState.Closed;
                    return;
                }

                int result = _device.SM_CDMS30xx_ETH_AcqStop();
                _isCollecting = false;
                State = result == SM_CDMS3010_API_Result.Success ? HardwareState.Complete : HardwareState.Error;
                SetMessage(result == SM_CDMS3010_API_Result.Success
                    ? "CDMS连续采集已停止。"
                    : $"CDMS停止采集失败：{result}");
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            lock (_syncRoot)
            {
                if (!EnsureConnected())
                {
                    return new List<MeasureData>();
                }

                if (_isCollecting)
                {
                    return ReceiveContinuousData();
                }

                return TryReadSingleData(out float[] values)
                    ? new List<MeasureData> { BuildMeasureData(values) }
                    : new List<MeasureData>();
            }
        }

        public override bool SettingParam(string key, object value)
        {
            return key switch
            {
                nameof(Config.FreqBW) => SetFreqBW((CDMS30xx_FreqBW)value),
                nameof(Config.OutDataMode) => SetOutDataMode((CDMS30xx_OutDataMode)value),
                nameof(Config.TriggerMode) => SetTriggerMode((CDMS30xx_TriggerMode)value),
                nameof(Config.ChannelEnabled) => SetChannelEnabled(Convert.ToByte(value)),
                nameof(Config.FSO) => SetFSO(Convert.ToDouble(value)),
                nameof(Config.IniDistance) => SetIniDistance(Convert.ToDouble(value)),
                nameof(Config.Zero) => SetZero(Convert.ToInt32(value)),
                nameof(Config.One) => SetOne(Convert.ToInt32(value)),
                nameof(Config.SSPN) => SetSSPN(Convert.ToString(value) ?? string.Empty),
                nameof(Config.SSPNBackup) => SetSSPNBackup(Convert.ToString(value) ?? string.Empty),
                _ => false,
            };
        }

        public bool RefreshDeviceParameters()
        {
            if (!EnsureConnected())
            {
                return false;
            }

            bool ok = true;
            byte deviceId = Config.DeviceID;
            ok &= IsSuccess(_device.SM_CDMS30xx_ETH_GetMAID(ref deviceId));
            Config.DeviceID = deviceId;

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetMAPN(out string devicePN)))
            {
                Config.DevicePN = devicePN;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetChEnable(out byte channelEnabled)))
            {
                Config.ChannelEnabled = channelEnabled;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetFreqBW(out CDMS30xx_FreqBW freqBW)))
            {
                Config.FreqBW = freqBW;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetOutDataMode(out CDMS30xx_OutDataMode outDataMode)))
            {
                Config.OutDataMode = outDataMode;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetTriggerMode(out CDMS30xx_TriggerMode triggerMode)))
            {
                Config.TriggerMode = triggerMode;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetSoftVersion(out string softVersion)))
            {
                Config.SoftVersion = softVersion;
            }
            else
            {
                ok = false;
            }

            ok &= RefreshChannelParameters();
            SetMessage(ok ? "CDMS参数刷新完成。" : "CDMS参数部分刷新完成。");
            return ok;
        }

        public bool RefreshChannelParameters()
        {
            if (!EnsureConnected())
            {
                return false;
            }

            byte channel = Config.SelectedChannel;
            bool ok = true;

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetSLPN(channel, out string slpn)))
            {
                Config.SLPN = slpn;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetSSPN(channel, out string sspn)))
            {
                Config.SSPN = sspn;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetSSPN_Backup(channel, out string sspnBackup)))
            {
                Config.SSPNBackup = sspnBackup;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetFSO(channel, out double fso)))
            {
                Config.FSO = fso;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetIniDistance(channel, out double iniDistance)))
            {
                Config.IniDistance = iniDistance;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetZero(channel, out int zero)))
            {
                Config.Zero = zero;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetOne(channel, out int one)))
            {
                Config.One = one;
            }
            else
            {
                ok = false;
            }

            if (IsSuccess(_device.SM_CDMS30xx_ETH_GetSelfCheck(channel, out ushort selfCheck)))
            {
                Config.SelfCheck = selfCheck;
            }
            else
            {
                ok = false;
            }

            return ok;
        }

        public bool SetDeviceID(byte deviceId)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetMAID(deviceId));
            if (ok) Config.DeviceID = deviceId;
            return ReportWriteResult(ok, "设备ID");
        }

        public bool SetDevicePN(string devicePN)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetMAPN(devicePN));
            if (ok) Config.DevicePN = devicePN;
            return ReportWriteResult(ok, "设备PN");
        }

        public bool SetChannelEnabled(byte channelEnabled)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetChEnable(channelEnabled));
            if (ok) Config.ChannelEnabled = channelEnabled;
            return ReportWriteResult(ok, "通道使能");
        }

        public bool SetFreqBW(CDMS30xx_FreqBW freqBW)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetFreqBW(freqBW));
            if (ok) Config.FreqBW = freqBW;
            return ReportWriteResult(ok, "IIR带宽");
        }

        public bool SetOutDataMode(CDMS30xx_OutDataMode outDataMode)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetOutDataMode(outDataMode));
            if (ok) Config.OutDataMode = outDataMode;
            return ReportWriteResult(ok, "输出模式");
        }

        public bool SetTriggerMode(CDMS30xx_TriggerMode triggerMode)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetTriggerMode(triggerMode));
            if (ok) Config.TriggerMode = triggerMode;
            return ReportWriteResult(ok, "触发模式");
        }

        public bool SetFSO(double fso)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetFSO(Config.SelectedChannel, fso));
            if (ok) Config.FSO = fso;
            return ReportWriteResult(ok, "量程");
        }

        public bool SetIniDistance(double iniDistance)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetIniDistance(Config.SelectedChannel, iniDistance));
            if (ok) Config.IniDistance = iniDistance;
            return ReportWriteResult(ok, "初距");
        }

        public bool SetZero(int zero)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetZero(Config.SelectedChannel, zero));
            if (ok) Config.Zero = zero;
            return ReportWriteResult(ok, "归一化0点");
        }

        public bool SetOne(int one)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetOne(Config.SelectedChannel, one));
            if (ok) Config.One = one;
            return ReportWriteResult(ok, "归一化1点");
        }

        public bool SetSSPN(string sspn)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetSSPN(Config.SelectedChannel, sspn));
            if (ok) Config.SSPN = sspn;
            return ReportWriteResult(ok, "SSPN");
        }

        public bool SetSSPNBackup(string sspnBackup)
        {
            if (!EnsureConnected()) return false;
            bool ok = IsSuccess(_device.SM_CDMS30xx_ETH_SetSSPN_Backup(Config.SelectedChannel, sspnBackup));
            if (ok) Config.SSPNBackup = sspnBackup;
            return ReportWriteResult(ok, "SSPN备份");
        }

        private unsafe bool TryReadSingleData(out float[] values)
        {
            values = Array.Empty<float>();
            float* buffer = stackalloc float[MaxChannelCount];
            int result = _device.SM_CDMS30xx_ETH_GetSingleData(buffer);
            if (!IsSuccess(result))
            {
                SetMessage($"CDMS单次读取失败：{result}");
                return false;
            }

            values = CopyEnabledValues(buffer);
            LastData = values;
            State = HardwareState.Complete;
            SetMessage($"CDMS读取到 {values.Length} 个数据。");
            return true;
        }

        private List<MeasureData> ReceiveContinuousData()
        {
            List<SM_CMD> cmdList = new();
            int readDataSize = 2000;
            float[] readData = new float[readDataSize];
            int dataFrameNum = 0;

            int result = _device.SM_CDMS30xx_ETH_ReadData(
                ref _remainData,
                ref _remainDataLength,
                ref cmdList,
                ref readData,
                ref readDataSize,
                ref dataFrameNum);

            if (!IsSuccess(result))
            {
                SetMessage($"CDMS连续数据读取失败：{result}");
                return new List<MeasureData>();
            }

            if (cmdList.Any(IsAcqStopCommand))
            {
                _device.SM_CDMS30xx_SetHandlePara_AcqState(CDMS30xx_AcqState.CDMS30xx_AcqStop);
                _isCollecting = false;
                State = HardwareState.Complete;
            }

            int valueCount = Math.Min(Math.Max(dataFrameNum, 0), readData.Length);
            if (valueCount <= 0)
            {
                return new List<MeasureData>();
            }

            float[] values = readData.Take(valueCount).ToArray();
            LastData = values;
            return new List<MeasureData> { BuildMeasureData(values) };
        }

        private bool EnsureConnected()
        {
            return IsConnected || Init();
        }

        private string ResolveLocalAddress()
        {
            if (!string.IsNullOrWhiteSpace(Config.LocalAddress))
            {
                return Config.LocalAddress;
            }

            if (CDMS_ETH.SM_CDMS30xx_ETH_GetLocalAddress(out string[] allLocalIp) == SM_CDMS3010_API_Result.Success
                && allLocalIp.Length > 0)
            {
                return allLocalIp[0];
            }

            return string.Empty;
        }

        private unsafe float[] CopyEnabledValues(float* buffer)
        {
            byte[] channels = GetEnabledChannels();
            float[] values = new float[channels.Length];
            for (int i = 0; i < channels.Length; i++)
            {
                values[i] = buffer[channels[i]];
            }

            return values;
        }

        private byte[] GetEnabledChannels()
        {
            byte mask = Config.ChannelEnabled;
            if (mask == 0)
            {
                return new byte[] { Config.SelectedChannel };
            }

            List<byte> channels = new();
            for (byte i = 0; i < MaxChannelCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    channels.Add(i);
                }
            }

            return channels.Count == 0 ? new byte[] { 0 } : channels.ToArray();
        }

        private MeasureData BuildMeasureData(float[] values)
        {
            byte[] channels = GetEnabledChannels();
            Dictionary<string, Dictionary<string, object>> originalDatas = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < values.Length; i++)
            {
                int channelIndex = i < channels.Length ? channels[i] : i;
                originalDatas[$"Channel{channelIndex + 1}"] = new Dictionary<string, object>
                {
                    ["Measurement"] = values[i],
                    ["Mode"] = Config.OutDataMode.ToString(),
                };
            }

            return new MeasureData
            {
                RTime = DateTime.Now,
                Z = values.Length > 0 ? values[0] : 0,
                AreaData = new List<float[]> { values },
                OriginalDatas = originalDatas,
            };
        }

        private bool IsAcqStopCommand(SM_CMD cmd)
        {
            return cmd.CMDCode == CDMS3010_V1_1_CMDCode.CDMS3010_CMDCODE_ContinueData
                && cmd.MainDeviceID == Config.DeviceID;
        }

        private bool ReportWriteResult(bool ok, string name)
        {
            SetMessage(ok ? $"CDMS写入{name}成功。" : $"CDMS写入{name}失败。");
            return ok;
        }

        private void SetMessage(string message)
        {
            Config.LastMessage = message;
            Logs.LogInfo(message);
        }

        private static bool IsSuccess(int result)
        {
            return result == SM_CDMS3010_API_Result.Success;
        }
    }

    [Serializable]
    public class CDMSSensorConfig : BindableBase
    {
        private string _localAddress = string.Empty;
        public string LocalAddress
        {
            get => _localAddress;
            set { _localAddress = value; RaisePropertyChanged(); }
        }

        private byte _deviceID;
        public byte DeviceID
        {
            get => _deviceID;
            set { _deviceID = value; RaisePropertyChanged(); }
        }

        private string _devicePN = string.Empty;
        public string DevicePN
        {
            get => _devicePN;
            set { _devicePN = value; RaisePropertyChanged(); }
        }

        private byte _selectedChannel;
        public byte SelectedChannel
        {
            get => _selectedChannel;
            set { _selectedChannel = (byte)Math.Min(value, (byte)7); RaisePropertyChanged(); }
        }

        private byte _channelEnabled = 1;
        public byte ChannelEnabled
        {
            get => _channelEnabled;
            set { _channelEnabled = value; RaisePropertyChanged(); }
        }

        private CDMS30xx_FreqBW _freqBW = CDMS30xx_FreqBW.CDMS30xx_FreqBW_NoFilter;
        public CDMS30xx_FreqBW FreqBW
        {
            get => _freqBW;
            set { _freqBW = value; RaisePropertyChanged(); }
        }

        private CDMS30xx_OutDataMode _outDataMode = CDMS30xx_OutDataMode.CDMS30xx_Distance;
        public CDMS30xx_OutDataMode OutDataMode
        {
            get => _outDataMode;
            set { _outDataMode = value; RaisePropertyChanged(); }
        }

        private CDMS30xx_TriggerMode _triggerMode = CDMS30xx_TriggerMode.CDMS30xx_Trig_Immediately;
        public CDMS30xx_TriggerMode TriggerMode
        {
            get => _triggerMode;
            set { _triggerMode = value; RaisePropertyChanged(); }
        }

        private double _fso;
        public double FSO
        {
            get => _fso;
            set { _fso = value; RaisePropertyChanged(); }
        }

        private double _iniDistance;
        public double IniDistance
        {
            get => _iniDistance;
            set { _iniDistance = value; RaisePropertyChanged(); }
        }

        private int _zero;
        public int Zero
        {
            get => _zero;
            set { _zero = value; RaisePropertyChanged(); }
        }

        private int _one;
        public int One
        {
            get => _one;
            set { _one = value; RaisePropertyChanged(); }
        }

        private ushort _selfCheck;
        public ushort SelfCheck
        {
            get => _selfCheck;
            set { _selfCheck = value; RaisePropertyChanged(); }
        }

        private string _slpn = string.Empty;
        public string SLPN
        {
            get => _slpn;
            set { _slpn = value; RaisePropertyChanged(); }
        }

        private string _sspn = string.Empty;
        public string SSPN
        {
            get => _sspn;
            set { _sspn = value; RaisePropertyChanged(); }
        }

        private string _sspnBackup = string.Empty;
        public string SSPNBackup
        {
            get => _sspnBackup;
            set { _sspnBackup = value; RaisePropertyChanged(); }
        }

        private string _softVersion = string.Empty;
        public string SoftVersion
        {
            get => _softVersion;
            set { _softVersion = value; RaisePropertyChanged(); }
        }

        private string _lastMessage = string.Empty;
        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float[] _lastData = Array.Empty<float>();
        [JsonIgnore]
        public float[] LastData
        {
            get => _lastData;
            set { _lastData = value ?? Array.Empty<float>(); RaisePropertyChanged(); }
        }
    }
}

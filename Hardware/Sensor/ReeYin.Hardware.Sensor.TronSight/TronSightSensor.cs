using Dm.util;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Defines;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.TronSight
{
    public class TronSightSensor : SensorBase
    {
        #region Fields
        public UInt64 ControlerHandle;

        private int kDefaultBufferSize = 10000;
        private static readonly int[] DefaultSensorOutputSignals = new int[]
        {
            (int)SENSOR_OUTPUT_DATA.DIST1,
            (int)SENSOR_OUTPUT_DATA.DIST2,
            (int)SENSOR_OUTPUT_DATA.THICKNESS
        };
        #endregion

        #region Properties
        [JsonIgnore]
        private BasicConfig _basicConfig = new BasicConfig();

        public BasicConfig BasicConfig
        {
            get { return _basicConfig; }
            set { _basicConfig = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TronSightMultiMathChannelConfig> _multiMathChannels;
        public ObservableCollection<TronSightMultiMathChannelConfig> MultiMathChannels
        {
            get { return _multiMathChannels; }
            set
            {
                _multiMathChannels = CloneMultiMathChannelConfigs(value);
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Constructor
        public TronSightSensor()
        {
            VenderName = "TS";
            VenderType = "TronSight";
            MultiMathChannels = CreateDefaultMultiMathChannelConfigs();
        }
        #endregion

        #region Methods
        public override bool SettingParam(string key, object value)
        {
            try
            {
                if(SetSingleParam(key, value))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }


        public override bool Init()
        {
            try
            {
                ControlerHandle = TSCMCAPICS.CreateInstance();
                ERRCODE rtn;
                var IPPorts = IP.Split('.');

                IPAddr deviceAddr = new IPAddr()
                {
                    c1 = byte.Parse(IPPorts[0]),
                    c2 = byte.Parse(IPPorts[1]),
                    c3 = byte.Parse(IPPorts[2]),
                    c4 = byte.Parse(IPPorts[3]),
                };
                int localPort = 8001;
                rtn = TSCMCAPICS.OpenConnectionEthernet(ControlerHandle, deviceAddr, localPort);

                if (rtn != ERRCODE.OK)
                {
                    TSCMCAPICS.CloseConnectionPort(ControlerHandle);
                    Console.WriteLine("关闭连接通道");
                    return false;
                }
                Console.Write("建立连接");
                rtn = TSCMCAPICS.SetConnectionOn(ControlerHandle, 0);
                if (rtn != ERRCODE.OK)
                {
                    TSCMCAPICS.CloseConnectionPort(ControlerHandle);
                    Console.WriteLine("关闭连接通道");
                    IsConnected = false;
                    return false;
                }

                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }

        public override void Close()
        {
            try
            {
                // 先检查句柄是否有效
                if (ControlerHandle == 0)
                {
                    IsConnected = false;
                    return;
                }

                // 安全检查连接状态
                bool isRunning = false;
                bool isConnected = false;
                try
                {
                    isRunning = TSCMCAPICS.isRunning(ControlerHandle);
                    isConnected = TSCMCAPICS.isConnected(ControlerHandle);
                }
                catch
                {
                    // 如果检查失败，直接返回
                    IsConnected = false;
                    return;
                }

                if (!isRunning || !isConnected)
                {
                    IsConnected = false;
                    return;
                }

                Console.Write("断开连接");
                TSCMCAPICS.SetConnectionOff(ControlerHandle, 0);
                TSCMCAPICS.CloseConnectionPort(ControlerHandle);
                Console.WriteLine("关闭连接通道");
                IsConnected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close异常: {ex.Message}");
                IsConnected = false;
            }
        }

        public override void StartCollect()
        {
            if (!CheckConnection())
            {
                return;
            }

            if (!ApplyConfiguredMultiMathSettings())
            {
                return;
            }

            Console.WriteLine("选择输出数据");
            CONNECTION_TYPE connectionType = TSCMCAPICS.GetConnectionType(ControlerHandle);
            int[] controllerSignals = GetEnabledControllerOutputSignals();

            //if (!ConfigureOutputSignals(connectionType, 0, controllerSignals))
            //{
            //    return;
            //}

            //int maxSensorChannels = TSCMCAPICS.MaxSensorChannels(ControlerHandle);
            //for (int i = 1; i <= maxSensorChannels; ++i)
            //{
            //    int[] sensorSignals = i == 1 ? DefaultSensorOutputSignals : Array.Empty<int>();
            //    if (!ConfigureOutputSignals(connectionType, i, DefaultSensorOutputSignals))
            //    {
            //        return;
            //    }
            //}

            Console.WriteLine("开始连续输出测量值");
            ERRCODE rtn = TSCMCAPICS.SetDataOutputOn(ControlerHandle, 0);

            if (rtn != ERRCODE.OK)
            {
                Console.WriteLine($"触发开始采集失败，请检查硬件连接状态！");
                return;
            }

            base.StartCollect();
        }

        public override void StopCollect()
        {
            if (!CheckConnection())
            {
                return;
            }
            var status = TSCMCAPICS.SetDataOutputOff(ControlerHandle, 0);
            if (status == ERRCODE.OK)
            {
                base.StopCollect();
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            List<MeasureData> ListMeasureData = new List<MeasureData>();
            
            if (!CheckConnection())
            {
                return ListMeasureData;
            }

            try
            {
                DataNode[] dataBuffer = new DataNode[kDefaultBufferSize];
                int nread = 0;
                
                // 非阻塞读取缓冲区中的所有数据
                ERRCODE rtn = TSCMCAPICS.TransferAllDataNode(ControlerHandle, ref dataBuffer[0], ref nread, kDefaultBufferSize);

                if (rtn == ERRCODE.NO_DATA_IN_BUFFER || nread == 0)
                {
                    return ListMeasureData;
                }
                
                if (rtn != ERRCODE.OK)
                {
                    Console.WriteLine($"读取数据失败，错误码: {rtn}");
                    return ListMeasureData;
                }

                int dataPerGroup = ResolveDataPerGroup(dataBuffer, nread);
                if (dataPerGroup <= 0)
                {
                    return ListMeasureData;
                }

                int completeNodeCount = nread - nread % dataPerGroup;
                if (completeNodeCount == 0)
                {
                    completeNodeCount = nread;
                }

                for (int startIndex = 0; startIndex < completeNodeCount; startIndex += dataPerGroup)
                {
                    MeasureData? measureData = BuildMeasureData(
                        dataBuffer,
                        startIndex,
                        Math.Min(dataPerGroup, nread - startIndex));

                    if (measureData != null)
                    {
                        ListMeasureData.Add(measureData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取传感器数据异常: {ex.Message}");
            }

            return ListMeasureData;
        }

        /// <summary>
        /// 执行自定义指令
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="Custom"></param>
        /// <returns></returns>
        public bool ExecuteCustomCommand(Func<object> Custom)
        {
            try
            {
                //检查是否连接设备
                if (!CheckConnection())
                {
                    return false;
                }

                //检查是否有报错
                if (TSCMCAPICS.getAllWarning(ControlerHandle)!=0)
                {
                    Console.WriteLine("设备存在报警！");
                    return false;
                }

                #region 执行自定义指令
                var ret = Custom();
                if (ret == null)
                {
                    Console.WriteLine("执行自定义指令失败! 错误码:" + ret.ToString());
                    return false;
                }
                else
                {
                    Console.WriteLine("执行自定义指令成功\r\n");
                    return true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }
        /// <summary>
        /// 检查传感器是否已连接
        /// </summary>
        private bool CheckConnection()
        {
            if (ControlerHandle == 0)
            {
                Console.WriteLine("传感器句柄无效！");
                return false;
            }
            try
            {
                if (!TSCMCAPICS.isConnected(ControlerHandle))
                {
                    Console.WriteLine("传感器未连接！");
                    return false;
                }
                return true;
            }
            catch
            {
                Console.WriteLine("检查连接状态失败！");
                return false;
            }
        }

        /// <summary>
        /// 设置单个参数
        /// </summary>
        public bool SetSingleParam(string paramName, object value)
        {
            if (!CheckConnection())
            {
                return false;
            }

            ERRCODE rtn = ERRCODE.OK;
            try
            {
                switch (paramName)
                {
                    case "WarningHoldPoints":
                        rtn = TSCMCAPICS.SetWarningHoldPoints(ControlerHandle, 0, (int)value);
                        break;
                    case "SamplingInterval":
                        rtn = TSCMCAPICS.SetConfigSamplingInterval(ControlerHandle, 0, (SAMPLING_INTERVAL)value);
                        break;
                    case "MoveAverage":
                        rtn = TSCMCAPICS.SetConfigMoveAvarage(ControlerHandle, 0, (FILTER_WINDOW_WIDTH)value);
                        break;
                    case "MultiMathSetting":
                        if (value is TronSightMultiMathChannelConfig multiMathConfig)
                        {
                            UpdateStoredMultiMathSetting(multiMathConfig);
                            rtn = TSCMCAPICS.SetConfigMultiMathSetting(ControlerHandle, 0, multiMathConfig.Channel, multiMathConfig.ToApiSetting());
                        }
                        else if (value is IEnumerable<TronSightMultiMathChannelConfig> multiMathConfigs)
                        {
                            UpdateMultiMathChannels(multiMathConfigs);
                            if (!ApplyConfiguredMultiMathSettings())
                            {
                                return false;
                            }
                            rtn = ERRCODE.OK;
                        }
                        break;

                    // ===================== 通道使能 =====================
                    case "ChannelEnable":
                        if (value is ChannelEnable ce)
                        {
                            rtn = TSCMCAPICS.SetContorllerChannelEnable(ControlerHandle, 0, ce);
                        }
                        break;

                    case "MeasureMode":
                        // 测量模式设置 - 使用SetConfigMeasurementMode API
                        MeasureMode mode = (MeasureMode)value;
                        MEASUREMODE apiMode = MEASUREMODE.CONFOCAL_DISTANCE;
                        switch (mode)
                        {
                            case MeasureMode.测距模式:
                                apiMode = MEASUREMODE.CONFOCAL_DISTANCE;
                                break;
                            case MeasureMode.单层测厚模式:
                                apiMode = MEASUREMODE.CONFOCAL_THICKNESS_SINGLE_LAYER;
                                break;
                            case MeasureMode.多层测厚模式:
                                apiMode = MEASUREMODE.CONFOCAL_THICKNESS_MULTI_LAYER;
                                break;
                        }
                        rtn = TSCMCAPICS.SetConfigMeasurementMode(ControlerHandle, 0, apiMode);
                        break;

                    // ===================== 外部触发配置（触发采样模式、采样使能电平、单脉冲采样个数、滤波宽度）=====================
                    case "ExternalTriggerConfig":
                        // 一次性设置整个ExternalTrigger结构体
                        if (value is ExternalTrigger etConfig)
                        {
                            rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etConfig);
                        }
                        break;

                    case "TriggerMethod":
                        // 触发方式（无触发/编码器触发/同步触发）
                        ExternalTrigger etMethod = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etMethod);
                        etMethod.trig_method = (TRIG_METHOD)value;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etMethod);
                        break;

                    case "TriggerSamplingMode":
                        // 触发采样模式（边沿触发/电平触发）- 对应 sync_setting.input_mode
                        ExternalTrigger etSamplingMode = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etSamplingMode);
                        etSamplingMode.sync_setting.input_mode = (SYNC_INPUT_MODE)value;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etSamplingMode);
                        break;

                    case "SamplingEnableLevel":
                        // 采样使能电平（低电平/高电平）- 对应 sync_setting.valid_level
                        ExternalTrigger etLevel = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etLevel);
                        etLevel.sync_setting.valid_level = (SYNC_VALID_LEVEL)value;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etLevel);
                        break;

                    case "SinglePulseSamplingCount":
                        // 单脉冲采样个数 - 对应 sync_setting.sample_per_trigger
                        ExternalTrigger etCount = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etCount);
                        etCount.sync_setting.sample_per_trigger = (ushort)(int)value;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etCount);
                        break;

                    case "TriggerFilterWidth":
                        // 滤波宽度（触发配置中的）- 对应 sync_setting.filter_width
                        ExternalTrigger etFilter = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etFilter);
                        etFilter.sync_setting.filter_width = (SYNC_FILTER_WIDTH)value;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etFilter);
                        break;

                    case "SyncEnable":
                        // SYNC使能 - 对应 sync_setting.state
                        ExternalTrigger etSync = new ExternalTrigger();
                        TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etSync);
                        etSync.sync_setting.state = (bool)value ? STATE.ON : STATE.OFF;
                        rtn = TSCMCAPICS.SetConfigExternalTrigger(ControlerHandle, 0, etSync);
                        break;

                    // ===================== 折射率表层序号（1-5层）=====================
                    case "RefractiveTableLabel":
                        // 设置当前使用的折射率表层序号
                        if (value is Tuple<int, int> labelData)
                        {
                            int sensor = labelData.Item1;
                            int label = labelData.Item2; // 层序号 1-5
                            rtn = TSCMCAPICS.SetCurrentRefractiveTableLabel(ControlerHandle, 0, sensor, label);
                        }
                        else if (value is int labelOnly)
                        {
                            // 默认sensor=0
                            rtn = TSCMCAPICS.SetCurrentRefractiveTableLabel(ControlerHandle, 0, 0, labelOnly);
                        }
                        break;

                    case "TriggerChannel": // 触发通道 -> TriggerSetting.channel
                        TriggerSetting tsChannel = new TriggerSetting();
                        TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref tsChannel);
                        tsChannel.channel = (TriggerChannel)value == TriggerChannel.编码器1 ? ENCODER_CHANNEL.CH1 : ENCODER_CHANNEL.CH2;
                        rtn = TSCMCAPICS.SetConfigTriggerSetting(ControlerHandle, 0, tsChannel);
                        break;

                     case "TriggerMode": // 触发模式 -> TriggerSetting.mode
                        TriggerSetting tsMode = new TriggerSetting();
                        TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref tsMode);
                        tsMode.mode = (TriggerMode)value == TriggerMode.位置触发 ? TRIG_MODE.POSITION : TRIG_MODE.COUNTER;
                        rtn = TSCMCAPICS.SetConfigTriggerSetting(ControlerHandle, 0, tsMode);
                        break;

                    case "TriggerDirection": // 触发方向 -> TriggerSetting.direction
                        TriggerSetting tsDir = new TriggerSetting();
                        TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref tsDir);
                        TriggerDirection dir = (TriggerDirection)value;
                        if (dir == TriggerDirection.正向) tsDir.direction = TRIG_DIRECTION.POS;
                        else if (dir == TriggerDirection.反向) tsDir.direction = TRIG_DIRECTION.NEG;
                        else tsDir.direction = TRIG_DIRECTION.BOTH;
                        rtn = TSCMCAPICS.SetConfigTriggerSetting(ControlerHandle, 0, tsDir);
                        break;

                    case "TrackingMode": // 追踪模式 -> TriggerSetting.track_mode
                        TriggerSetting tsTrack = new TriggerSetting();
                        TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref tsTrack);
                        tsTrack.track_mode = (TrackingMode)value == TrackingMode.开 ? TRIG_TRACK_MODE.ON : TRIG_TRACK_MODE.OFF;
                        rtn = TSCMCAPICS.SetConfigTriggerSetting(ControlerHandle, 0, tsTrack);
                        break;

                    case "TriggerInterval": // 触发间隔 -> TriggerSetting.downsample_factor
                        TriggerSetting tsInterval = new TriggerSetting();
                        TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref tsInterval);
                        tsInterval.downsample_factor = (int)value;
                        rtn = TSCMCAPICS.SetConfigTriggerSetting(ControlerHandle, 0, tsInterval);
                        break;
                        
                    // 编码器1设置
                    case "Encoder1_Enable":
                         rtn = TSCMCAPICS.SetConfigEncoderCounterEnable(ControlerHandle, 0, ENCODER_CHANNEL.CH1, (bool)value ? STATE.ON : STATE.OFF);
                         break;
                    case "Encoder1_InputMode":
                    case "Encoder1_DecodeMode":
                    case "Encoder1_ZPhaseEnable":
                        EncoderSetting es1 = new EncoderSetting();
                        TSCMCAPICS.GetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref es1);
                        if (paramName == "Encoder1_InputMode") es1.input_mode = (EncoderInputMode)value == EncoderInputMode.单路 ? ENCODER_INPUT_MODE.A : ENCODER_INPUT_MODE.AB;
                        else if (paramName == "Encoder1_DecodeMode")
                        {
                             EncoderDecodeMode dmode = (EncoderDecodeMode)value;
                             if(dmode == EncoderDecodeMode.X1) es1.output_mode = ENCODER_OUTPUT_MODE.X1;
                             else if(dmode == EncoderDecodeMode.X2) es1.output_mode = ENCODER_OUTPUT_MODE.X2;
                             else es1.output_mode = ENCODER_OUTPUT_MODE.X4;
                        }
                        else if (paramName == "Encoder1_ZPhaseEnable") es1.z_phase = (bool)value;
                        rtn = TSCMCAPICS.SetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH1, es1);
                        break;
                    case "Encoder1_PulseRatio":
                        rtn = TSCMCAPICS.SetConfigEncoderResolution(ControlerHandle, 0, ENCODER_CHANNEL.CH1, (double)value);
                        break;
                    case "Encoder1_ManualResetValue":
                         rtn = TSCMCAPICS.SetConfigEncoderPosition(ControlerHandle, 0, ENCODER_CHANNEL.CH1, (double)value);
                         break;
                    case "Encoder1_ZSignalResetValue":
                         rtn = TSCMCAPICS.SetConfigZPhasePosition(ControlerHandle, 0, ENCODER_CHANNEL.CH1, (double)value);
                         break;

                    // 编码器2设置
                    case "Encoder2_Enable":
                         rtn = TSCMCAPICS.SetConfigEncoderCounterEnable(ControlerHandle, 0, ENCODER_CHANNEL.CH2, (bool)value ? STATE.ON : STATE.OFF);
                         break;
                    case "Encoder2_InputMode":
                    case "Encoder2_DecodeMode":
                    case "Encoder2_ZPhaseEnable":
                        EncoderSetting es2 = new EncoderSetting();
                        TSCMCAPICS.GetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref es2);
                        if (paramName == "Encoder2_InputMode") es2.input_mode = (EncoderInputMode)value == EncoderInputMode.单路 ? ENCODER_INPUT_MODE.A : ENCODER_INPUT_MODE.AB;
                        else if (paramName == "Encoder2_DecodeMode")
                        {
                             EncoderDecodeMode dmode = (EncoderDecodeMode)value;
                             if(dmode == EncoderDecodeMode.X1) es2.output_mode = ENCODER_OUTPUT_MODE.X1;
                             else if(dmode == EncoderDecodeMode.X2) es2.output_mode = ENCODER_OUTPUT_MODE.X2;
                             else es2.output_mode = ENCODER_OUTPUT_MODE.X4;
                        }
                        else if (paramName == "Encoder2_ZPhaseEnable") es2.z_phase = (bool)value;
                        rtn = TSCMCAPICS.SetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH2, es2);
                        break;
                    case "Encoder2_PulseRatio":
                        rtn = TSCMCAPICS.SetConfigEncoderResolution(ControlerHandle, 0, ENCODER_CHANNEL.CH2, (double)value);
                        break;
                    case "Encoder2_ManualResetValue":
                         rtn = TSCMCAPICS.SetConfigEncoderPosition(ControlerHandle, 0, ENCODER_CHANNEL.CH2, (double)value);
                         break;
                    case "Encoder2_ZSignalResetValue":
                         rtn = TSCMCAPICS.SetConfigZPhasePosition(ControlerHandle, 0, ENCODER_CHANNEL.CH2, (double)value);
                         break;
                    
                    // 标定相关
                    case "CorrectionFactor":
                        rtn = TSCMCAPICS.SetThickCorrectionFactor(ControlerHandle, 0, 0, (double)value);
                        break;

                    default:
                        Console.WriteLine($"未知参数名: {paramName}");
                        return false;
                }

                if (rtn != ERRCODE.OK)
                {
                    Console.WriteLine($"设置参数({paramName})失败! 错误码: {rtn}");
                    return false;
                }
                else
                {
                    if (paramName == "MeasureMode" && value is MeasureMode selectedMeasureMode)
                    {
                        BasicConfig.MeasureMode = selectedMeasureMode;
                    }

                    Console.WriteLine($"设置参数({paramName})成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置参数({paramName})异常: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 获取单个参数
        /// </summary>
        public bool GetSingleParam(string paramName, out object value)
        {
            value = null;
            if (!CheckConnection())
            {
                return false;
            }

            ERRCODE rtn = ERRCODE.OK;
            try
            {
                switch (paramName)
                {
                    case "WarningHoldPoints":
                        int points = 0;
                        rtn = TSCMCAPICS.GetWarningHoldPoints(ControlerHandle, 0, ref points);
                        value = points;
                        break;
                    case "SamplingInterval":
                        SAMPLING_INTERVAL interval = SAMPLING_INTERVAL._100US;
                        rtn = TSCMCAPICS.GetConfigSamplingInterval(ControlerHandle, 0, ref interval);
                        value = interval;
                        break;
                    case "MoveAverage":
                        FILTER_WINDOW_WIDTH window = FILTER_WINDOW_WIDTH._1;
                        rtn = TSCMCAPICS.GetConfigMoveAvarage(ControlerHandle, 0, ref window);
                        value = window;
                        break;
                    case "MultiMathSetting":
                        value = CloneMultiMathChannelConfigs(MultiMathChannels);
                        break;

                    // ===================== 通道使能获取 =====================
                    case "ChannelEnable":
                        ChannelEnable channelEnable = new ChannelEnable();
                        rtn = TSCMCAPICS.GetContorllerChannelEnable(ControlerHandle, 0, ref channelEnable);
                        value = channelEnable;
                        break;

                    case "MeasureMode":
                        // 获取测量模式 - 使用GetConfigMeasurementMode API
                        MEASUREMODE currentMode = MEASUREMODE.CONFOCAL_DISTANCE;
                        rtn = TSCMCAPICS.GetConfigMeasurementMode(ControlerHandle, 0, ref currentMode);
                        switch (currentMode)
                        {
                            case MEASUREMODE.CONFOCAL_DISTANCE:
                                value = MeasureMode.测距模式;
                                break;
                            case MEASUREMODE.CONFOCAL_THICKNESS_SINGLE_LAYER:
                            case MEASUREMODE.INTERF_THICKNESS_SINGLE_LAYER:
                                value = MeasureMode.单层测厚模式;
                                break;
                            case MEASUREMODE.CONFOCAL_THICKNESS_MULTI_LAYER:
                            case MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER:
                                value = MeasureMode.多层测厚模式;
                                break;
                            default:
                                value = MeasureMode.测距模式;
                                break;
                        }
                        break;

                    // ===================== 外部触发配置获取 =====================
                    case "ExternalTriggerConfig":
                        // 一次性获取所有外部触发配置
                        ExternalTrigger etConfig = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etConfig);
                        value = etConfig;
                        break;

                    case "TriggerMethod":
                        // 获取触发方式
                        ExternalTrigger etMethodGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etMethodGet);
                        value = etMethodGet.trig_method;
                        break;

                    case "TriggerSamplingMode":
                        // 获取触发采样模式
                        ExternalTrigger etSamplingModeGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etSamplingModeGet);
                        value = etSamplingModeGet.sync_setting.input_mode;
                        break;

                    case "SamplingEnableLevel":
                        // 获取采样使能电平
                        ExternalTrigger etLevelGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etLevelGet);
                        value = etLevelGet.sync_setting.valid_level;
                        break;

                    case "SinglePulseSamplingCount":
                        // 获取单脉冲采样个数
                        ExternalTrigger etCountGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etCountGet);
                        value = (int)etCountGet.sync_setting.sample_per_trigger;
                        break;

                    case "TriggerFilterWidth":
                        // 获取滤波宽度（触发配置中的）
                        ExternalTrigger etFilterGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etFilterGet);
                        value = etFilterGet.sync_setting.filter_width;
                        break;

                    case "SyncEnable":
                        // 获取SYNC使能状态
                        ExternalTrigger etSyncGet = new ExternalTrigger();
                        rtn = TSCMCAPICS.GetConfigExternalTrigger(ControlerHandle, 0, ref etSyncGet);
                        value = etSyncGet.sync_setting.state == STATE.ON;
                        break;

                    // ===================== 折射率表层序号获取 =====================
                    case "RefractiveTableLabel":
                        // 获取当前使用的折射率表层序号（默认sensor=0）
                        int currentLabel = 0;
                        rtn = TSCMCAPICS.GetCurrentRefractiveTableLabel(ControlerHandle, 0, 0, ref currentLabel);
                        value = currentLabel;
                        break;
                    
                    // 触发配置相关（TriggerSetting）
                    case "TriggerConfig": // 一次性获取所有触发配置，减少IO交互，或者按字段获取
                         TriggerSetting ts = new TriggerSetting();
                         rtn = TSCMCAPICS.GetConfigTriggerSetting(ControlerHandle, 0, ref ts);
                         value = ts;
                         break;
                         
                    // 编码器
                    case "Encoder1_Config":
                         EncoderSetting es1 = new EncoderSetting();
                         rtn = TSCMCAPICS.GetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref es1);
                         value = es1;
                         break;
                    case "Encoder1_Enable":
                         STATE st1 = STATE.OFF;
                         rtn = TSCMCAPICS.GetConfigEncoderCounterEnable(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref st1);
                         value = st1 == STATE.ON;
                         break;
                    case "Encoder1_PulseRatio":
                         double res1 = 0;
                         rtn = TSCMCAPICS.GetConfigEncoderResolution(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref res1);
                         value = res1;
                         break;
                    case "Encoder1_ManualResetValue":
                         double pos1 = 0;
                         rtn = TSCMCAPICS.GetConfigEncoderPosition(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref pos1);
                         value = pos1;
                         break;
                    case "Encoder1_ZSignalResetValue":
                         double zpos1 = 0;
                         rtn = TSCMCAPICS.GetConfigZPhasePosition(ControlerHandle, 0, ENCODER_CHANNEL.CH1, ref zpos1);
                         value = zpos1;
                         break;

                    case "Encoder2_Config":
                         EncoderSetting es2 = new EncoderSetting();
                         rtn = TSCMCAPICS.GetConfigEncoderSetting(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref es2);
                         value = es2;
                         break;
                    case "Encoder2_Enable":
                         STATE st2 = STATE.OFF;
                         rtn = TSCMCAPICS.GetConfigEncoderCounterEnable(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref st2);
                         value = st2 == STATE.ON;
                         break;
                    case "Encoder2_PulseRatio":
                         double res2 = 0;
                         rtn = TSCMCAPICS.GetConfigEncoderResolution(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref res2);
                         value = res2;
                         break;
                    case "Encoder2_ManualResetValue":
                         double pos2 = 0;
                         rtn = TSCMCAPICS.GetConfigEncoderPosition(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref pos2);
                         value = pos2;
                         break;
                    case "Encoder2_ZSignalResetValue":
                         double zpos2 = 0;
                         rtn = TSCMCAPICS.GetConfigZPhasePosition(ControlerHandle, 0, ENCODER_CHANNEL.CH2, ref zpos2);
                         value = zpos2;
                         break;

                    // 标定
                    case "CorrectionFactor":
                         double factor = 0;
                         rtn = TSCMCAPICS.GetThickCorrectionFactor(ControlerHandle, 0, 0, ref factor);
                         value = factor;
                         break;

                    default:
                        Console.WriteLine($"未知参数名: {paramName}");
                        return false;
                }

                if (rtn != ERRCODE.OK)
                {
                    Console.WriteLine($"获取参数({paramName})失败! 错误码: {rtn}");
                    return false;
                }
                else
                {
                    if (paramName == "MeasureMode" && value is MeasureMode currentMeasureMode)
                    {
                        BasicConfig.MeasureMode = currentMeasureMode;
                    }

                    Console.WriteLine($"获取参数({paramName})成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取参数({paramName})异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <param name="param">可选参数</param>
        /// <returns>执行结果，失败返回null</returns>
        public object? ExecuteCommand(string command, object? param = null)
        {
            if (!CheckConnection())
            {
                return null;
            }

            ERRCODE rtn = ERRCODE.OK;
            try
            {
                switch (command)
                {
                    #region 配置管理
                    case "RestoreFactorySetting":
                        // 恢复出厂设置
                        rtn = TSCMCAPICS.RestoreFactorySetting(ControlerHandle);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine("恢复出厂设置成功");
                            return true;
                        }
                        break;

                    case "SaveConfigToFile":
                        // 保存配置到文件
                        string saveFilePath = param as string;
                        if (string.IsNullOrEmpty(saveFilePath))
                        {
                            Console.WriteLine("保存路径不能为空");
                            return null;
                        }
                        rtn = TSCMCAPICS.SaveControllerConfig(ControlerHandle, saveFilePath);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"配置已保存到: {saveFilePath}");
                            return true;
                        }
                        break;

                    case "ReadConfigFromFile":
                        // 从文件读取配置
                        string readFilePath = param as string;
                        if (string.IsNullOrEmpty(readFilePath))
                        {
                            Console.WriteLine("读取路径不能为空");
                            return null;
                        }
                        rtn = TSCMCAPICS.ReadControllerConfig(ControlerHandle, readFilePath);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"已从文件读取配置: {readFilePath}");
                            return true;
                        }
                        break;

                    case "WriteConfigToFlash":
                        // 写入配置到Flash (固化参数)
                        rtn = TSCMCAPICS.SetConfigControllerSettings(ControlerHandle, 0);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine("配置已写入Flash");
                            return true;
                        }
                        break;

                    case "ReadConfigFromSensor":
                        // 从传感器读取配置
                        rtn = TSCMCAPICS.GetConfigControllerSettings(ControlerHandle, 0);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine("已从传感器读取配置");
                            return true;
                        }
                        break;
                    #endregion

                    #region 触发复位
                    case "ResetTriggerCounter":
                        // 触发计数器复位
                        rtn = TSCMCAPICS.ResetTriggerCounter(ControlerHandle, 0);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine("触发计数器已复位");
                            return true;
                        }
                        break;
                    #endregion

                    #region 标定功能
                    case "GetSingleMeasurement":
                        // 获取单次测量数据用于标定
                        DataNode[] dataBuffer = new DataNode[64];
                        int nread = 0;
                        rtn = TSCMCAPICS.GetSingleDataNode(ControlerHandle, 0, ref dataBuffer[0], ref nread, 64);
                        if (rtn == ERRCODE.OK && nread > 0)
                        {
                            // 返回第一个距离值
                            Console.WriteLine($"获取单次测量数据成功，读取 {nread} 个数据点");
                            return dataBuffer[0].data;
                        }
                        break;

                    case "SetZeroSetting":
                        // 设置零点
                        bool zeroEnable = param is bool b ? b : false;
                        rtn = TSCMCAPICS.SetConfigZeroSetting(ControlerHandle, 0, 0, zeroEnable);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"零点设置: {(zeroEnable ? "启用" : "禁用")}");
                            return true;
                        }
                        break;

                    case "SetZeroOffset":
                        // 设置零点偏移
                        double offset = param is double d ? d : 0;
                        rtn = TSCMCAPICS.SetConfigZeroOffset(ControlerHandle, 0, 0, offset);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"零点偏移设置为: {offset}");
                            return true;
                        }
                        break;

                    case "GetZeroOffset":
                        // 获取零点偏移
                        double currentOffset = 0;
                        rtn = TSCMCAPICS.GetConfigZeroOffset(ControlerHandle, 0, 0, ref currentOffset);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"当前零点偏移: {currentOffset}");
                            return currentOffset;
                        }
                        break;

                    case "SetMappingFactor":
                        // 设置映射系数 (标定系数)
                        double mappingFactor = param is double mf ? mf : 1.0;
                        rtn = TSCMCAPICS.SetConfigMapping(ControlerHandle, 0, 0, mappingFactor);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"映射系数设置为: {mappingFactor}");
                            return true;
                        }
                        break;

                    case "GetMappingFactor":
                        // 获取映射系数
                        double currentMapping = 0;
                        rtn = TSCMCAPICS.GetConfigMapping(ControlerHandle, 0, 0, ref currentMapping);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine($"当前映射系数: {currentMapping}");
                            return currentMapping;
                        }
                        break;

                    case "DarkCalibration":
                        // 暗校准
                        DarkReferenceTable darkTable = new DarkReferenceTable();
                        rtn = TSCMCAPICS.DarkCalibration(ControlerHandle, 0, 0, ref darkTable);
                        if (rtn == ERRCODE.OK)
                        {
                            Console.WriteLine("暗校准完成");
                            return darkTable;
                        }
                        break;
                    #endregion

                    #region 折射率表
                    case "GetRefractiveTableLabels":
                        // 获取折射率表标签列表
                        int[] labels = new int[16];
                        int nLabels = 0;
                        rtn = TSCMCAPICS.GetRefractiveTableLabel(ControlerHandle, 0, ref labels[0], ref nLabels, 16);
                        if (rtn == ERRCODE.OK)
                        {
                            int[] result = new int[nLabels];
                            Array.Copy(labels, result, nLabels);
                            Console.WriteLine($"获取折射率表标签成功，共 {nLabels} 个");
                            return result;
                        }
                        break;

                    case "DownloadRefractiveTable":
                        // 从控制器下载折射率表
                        if (param is int downloadLabel)
                        {
                            RefractiveTable refTable = new RefractiveTable();
                            rtn = TSCMCAPICS.DownloadRefractiveTable(ControlerHandle, 0, downloadLabel, ref refTable);
                            if (rtn == ERRCODE.OK)
                            {
                                Console.WriteLine($"下载折射率表 {downloadLabel} 成功");
                                return refTable;
                            }
                        }
                        break;

                    case "UploadRefractiveTable":
                        // 上传折射率表到控制器
                        if (param is Tuple<int, RefractiveTable> uploadData)
                        {
                            rtn = TSCMCAPICS.UploadRefractiveTable(ControlerHandle, 0, uploadData.Item1, uploadData.Item2);
                            if (rtn == ERRCODE.OK)
                            {
                                Console.WriteLine($"上传折射率表 {uploadData.Item1} 成功");
                                return true;
                            }
                        }
                        break;

                    case "DeleteRefractiveTableLabel":
                        // 删除折射率表标签
                        if (param is int deleteLabel)
                        {
                            int labelToDelete = deleteLabel;
                            rtn = TSCMCAPICS.DeleteRefractiveTableLabel(ControlerHandle, 0, ref labelToDelete, 1);
                            if (rtn == ERRCODE.OK)
                            {
                                Console.WriteLine($"删除折射率表 {deleteLabel} 成功");
                                return true;
                            }
                        }
                        break;

                    case "SetCurrentRefractiveTableLabel":
                        // 设置当前使用的折射率表
                        if (param is Tuple<int, int> setLabelData)
                        {
                            int sensor = setLabelData.Item1;
                            int label = setLabelData.Item2;
                            rtn = TSCMCAPICS.SetCurrentRefractiveTableLabel(ControlerHandle, 0, sensor, label);
                            if (rtn == ERRCODE.OK)
                            {
                                Console.WriteLine($"设置传感器 {sensor} 使用折射率表 {label} 成功");
                                return true;
                            }
                        }
                        break;

                    case "GetCurrentRefractiveTableLabel":
                        // 获取当前使用的折射率表
                        if (param is int sensorChannel)
                        {
                            int currentLabel = 0;
                            rtn = TSCMCAPICS.GetCurrentRefractiveTableLabel(ControlerHandle, 0, sensorChannel, ref currentLabel);
                            if (rtn == ERRCODE.OK)
                            {
                                Console.WriteLine($"传感器 {sensorChannel} 当前使用折射率表 {currentLabel}");
                                return currentLabel;
                            }
                        }
                        break;
                    #endregion

                    default:
                        Console.WriteLine($"未知命令: {command}");
                        return null;
                }

                if (rtn != ERRCODE.OK)
                {
                    Console.WriteLine($"执行命令({command})失败! 错误码: {rtn}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令({command})异常: {ex.Message}");
                return null;
            }

            return null;
        }

        #endregion

        #region MultiMath Helpers

        public static ObservableCollection<TronSightMultiMathChannelConfig> CreateDefaultMultiMathChannelConfigs()
        {
            return new ObservableCollection<TronSightMultiMathChannelConfig>(
                Enum.GetValues(typeof(MULTI_MATH_CHANNEL))
                    .Cast<MULTI_MATH_CHANNEL>()
                    .Select(channel => new TronSightMultiMathChannelConfig
                    {
                        Channel = channel,
                        IsEnabled = false,
                        SelectedSource = MULTI_MATH_DATA_SRC.DISTANCE1,
                        SelectedMode = MULTI_MATH_CALC_MODE.MEAN
                    }));
        }

        public void UpdateMultiMathChannels(IEnumerable<TronSightMultiMathChannelConfig> configs)
        {
            MultiMathChannels = CloneMultiMathChannelConfigs(configs);
        }

        public bool ApplyConfiguredMultiMathSettings()
        {
            if (!CheckConnection())
            {
                return false;
            }

            foreach (var config in MultiMathChannels.OrderBy(item => item.Channel))
            {
                ERRCODE rtn = TSCMCAPICS.SetConfigMultiMathSetting(ControlerHandle, 0, config.Channel, config.ToApiSetting());
                if (rtn != ERRCODE.OK)
                {
                    Console.WriteLine($"设置 {config.DisplayName} 失败，错误码: {rtn}");
                    //return false;
                }
            }

            return true;
        }

        private static ObservableCollection<TronSightMultiMathChannelConfig> CloneMultiMathChannelConfigs(IEnumerable<TronSightMultiMathChannelConfig>? configs)
        {
            IEnumerable<TronSightMultiMathChannelConfig> source = configs;
            if (source == null || !source.Any())
            {
                return CreateDefaultMultiMathChannelConfigs();
            }

            return new ObservableCollection<TronSightMultiMathChannelConfig>(
                source
                    .GroupBy(item => item.Channel)
                    .OrderBy(group => group.Key)
                    .Select(group => group.Last().Clone()));
        }

        private void UpdateStoredMultiMathSetting(TronSightMultiMathChannelConfig config)
        {
            var current = MultiMathChannels.FirstOrDefault(item => item.Channel == config.Channel);
            if (current == null)
            {
                MultiMathChannels.Add(config.Clone());
                return;
            }

            current.IsEnabled = config.IsEnabled;
            current.SelectedSource = config.SelectedSource;
            current.SelectedMode = config.SelectedMode;
        }

        private int[] GetEnabledControllerOutputSignals()
        {
            return MultiMathChannels
                .Where(item => item.IsEnabled)
                .OrderBy(item => item.Channel)
                .Select(item => (int)GetControllerOutputData(item.Channel))
                .ToArray();
        }

        private bool ConfigureOutputSignals(CONNECTION_TYPE connectionType, int channelIndex, int[] outputSignals)
        {
            int[] signalBuffer = outputSignals != null && outputSignals.Length > 0 ? outputSignals : new int[] { 0 };
            ERRCODE rtn = TSCMCAPICS.SetConfigOutputSignals(
                ControlerHandle,
                0,
                channelIndex,
                connectionType,
                ref signalBuffer[0],
                outputSignals?.Length ?? 0);

            if (rtn != ERRCODE.OK && rtn != ERRCODE.NO_DATA_IN_BUFFER)
            {
                Console.WriteLine($"配置通道{channelIndex}输出失败，错误码: {rtn}");
                return false;
            }

            return true;
        }

        private MeasureData? BuildMeasureData(
            DataNode[] dataBuffer,
            int startIndex,
            int length)
        {
            Dictionary<string, Dictionary<string, object>> originalDatas = new Dictionary<string, Dictionary<string, object>>();

            for (int i = startIndex; i < startIndex + length; i++)
            {
                DataNode node = dataBuffer[i];
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

        private static void AddOriginalData(Dictionary<string, Dictionary<string, object>> originalDatas, DataNode node)
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
            if (channel == 0 && Enum.IsDefined(typeof(CONTROLLER_OUTPUT_DATA), type))
            {
                return ((CONTROLLER_OUTPUT_DATA)type).ToString();
            }

            if (channel != 0 && Enum.IsDefined(typeof(SENSOR_OUTPUT_DATA), type))
            {
                return ((SENSOR_OUTPUT_DATA)type).ToString();
            }

            return type.ToString();
        }

        private static int ResolveDataPerGroup(DataNode[] dataBuffer, int nread)
        {
            if (nread <= 0)
            {
                return 0;
            }

            DataCfg firstCfg = dataBuffer[0].cfg;
            for (int i = 1; i < nread; i++)
            {
                DataCfg currentCfg = dataBuffer[i].cfg;
                if (currentCfg.channel == firstCfg.channel && currentCfg.type == firstCfg.type)
                {
                    return i;
                }
            }

            return nread;
        }

        private static CONTROLLER_OUTPUT_DATA GetControllerOutputData(MULTI_MATH_CHANNEL channel)
        {
            switch (channel)
            {
                case MULTI_MATH_CHANNEL._1:
                    return CONTROLLER_OUTPUT_DATA.MULTI_MATH1;
                case MULTI_MATH_CHANNEL._2:
                    return CONTROLLER_OUTPUT_DATA.MULTI_MATH2;
                case MULTI_MATH_CHANNEL._3:
                    return CONTROLLER_OUTPUT_DATA.MULTI_MATH3;
                case MULTI_MATH_CHANNEL._4:
                    return CONTROLLER_OUTPUT_DATA.MULTI_MATH4;
                default:
                    return CONTROLLER_OUTPUT_DATA.MULTI_MATH1;
            }
        }

        #endregion
    }

    #region Config Classes

    /// <summary>
    /// 基础配置参数
    /// </summary>
    public class BasicConfig : BindableBase
    {
        [JsonIgnore]
        private MeasureMode _measureMode = MeasureMode.测距模式;

        public MeasureMode MeasureMode
        {
            get { return _measureMode; }
            set { _measureMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _invalidDataHold;

        public int InvalidDataHold
        {
            get { return _invalidDataHold; }
            set { _invalidDataHold = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SAMPLING_INTERVAL _samplingInterval;

        public SAMPLING_INTERVAL SamplingInterval
        {
            get { return _samplingInterval; }
            set { _samplingInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _enableMovingAverage;

        public bool EnableMovingAverage
        {
            get { return _enableMovingAverage; }
            set { _enableMovingAverage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FILTER_WINDOW_WIDTH _movingAverageWindow;

        public FILTER_WINDOW_WIDTH MovingAverageWindow
        {
            get { return _movingAverageWindow; }
            set { _movingAverageWindow = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _enableMedianFilter;

        public bool EnableMedianFilter
        {
            get { return _enableMedianFilter; }
            set { _enableMedianFilter = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FILTER_WINDOW_WIDTH _medianWindow;

        public FILTER_WINDOW_WIDTH MedianWindow
        {
            get { return _medianWindow; }
            set { _medianWindow = value; RaisePropertyChanged(); }
        }
    }

    public class TronSightMultiMathChannelConfig : BindableBase
    {
        private MULTI_MATH_CHANNEL _channel;
        public MULTI_MATH_CHANNEL Channel
        {
            get { return _channel; }
            set
            {
                _channel = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        [JsonIgnore]
        public string DisplayName => $"Multi-Math {(int)Channel + 1}";

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private MULTI_MATH_DATA_SRC _selectedSource = MULTI_MATH_DATA_SRC.DISTANCE1;
        public MULTI_MATH_DATA_SRC SelectedSource
        {
            get { return _selectedSource; }
            set { _selectedSource = value; RaisePropertyChanged(); }
        }

        private MULTI_MATH_CALC_MODE _selectedMode = MULTI_MATH_CALC_MODE.MEAN;
        public MULTI_MATH_CALC_MODE SelectedMode
        {
            get { return _selectedMode; }
            set { _selectedMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<MULTI_MATH_DATA_SRC>? _dataSources;
        public ObservableCollection<MULTI_MATH_DATA_SRC> DataSources
        {
            get
            {
                if (_dataSources == null)
                {
                    _dataSources = new ObservableCollection<MULTI_MATH_DATA_SRC>(
                        Enum.GetValues(typeof(MULTI_MATH_DATA_SRC)).Cast<MULTI_MATH_DATA_SRC>());
                }
                return _dataSources;
            }
        }

        [JsonIgnore]
        private ObservableCollection<MULTI_MATH_CALC_MODE>? _calcModes;
        public ObservableCollection<MULTI_MATH_CALC_MODE> CalcModes
        {
            get
            {
                if (_calcModes == null)
                {
                    _calcModes = new ObservableCollection<MULTI_MATH_CALC_MODE>(
                        Enum.GetValues(typeof(MULTI_MATH_CALC_MODE)).Cast<MULTI_MATH_CALC_MODE>());
                }
                return _calcModes;
            }
        }

        public MultiMathSetting ToApiSetting()
        {
            return new MultiMathSetting
            {
                src = SelectedSource,
                mode = SelectedMode
            };
        }

        public TronSightMultiMathChannelConfig Clone()
        {
            return new TronSightMultiMathChannelConfig
            {
                Channel = Channel,
                IsEnabled = IsEnabled,
                SelectedSource = SelectedSource,
                SelectedMode = SelectedMode
            };
        }
    }

    #endregion
}

using ReeYin.Hardware.Sensor.TronSight;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Defines;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using ReeYin_V.UI;
using ReeYin_V.Core.Enums;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Views;
using ReeYin_V.UI.Style.Dialogs;

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.ViewModels
{
    public class TronSightSensorViewModel : DialogViewModelBase
    {
        #region Fields
        private DispatcherTimer _RefreshChartTimer;
        private bool _isInitializingMultiMathConfig;

        #endregion

        #region Properties
        private Grid _singlePointChartGrid = new Grid();
        public Grid SinglePointChartGrid
        {
            get { return _singlePointChartGrid; }
            set { _singlePointChartGrid = value; RaisePropertyChanged(); }
        }

        private TronSightSensorModel _modelParam = new TronSightSensorModel();

        public TronSightSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public TronSightSensorViewModel()
        {

        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            if (Param != null && (Param is TronSightSensor))
                ModelParam.Sensor = Param as TronSightSensor ?? new TronSightSensor();
            else
                ModelParam.Sensor = new TronSightSensor();

            InitMultiMathConfig();
            InitChartStyle();

            InitRefreshChartTimer();
            
            // 初始化折射率表（只设置默认路径，不加载数据）
            InitRefractiveConfig();

            Init();
        }

        private void InitMultiMathConfig()
        {
            _isInitializingMultiMathConfig = true;
            try
            {
                if (ModelParam.Sensor == null)
                {
                    ModelParam.MultiMathConfig.LoadFrom(null);
                    return;
                }

                ModelParam.MultiMathConfig.LoadFrom(ModelParam.Sensor.MultiMathChannels);
            }
            finally
            {
                _isInitializingMultiMathConfig = false;
            }
        }

        private void Init()
        {
            if (ModelParam.Sensor != null && ModelParam.Sensor.IsConnected)
            {
                // 从设备读取所有参数
                LoadBasicSettings();
                LoadTriggerSettings();
                LoadEncoderSettings();
                LoadCalibrationSettings();
            }
        }

        /// <summary>
        /// 从设备加载基础设置
        /// </summary>
        private void LoadBasicSettings()
        {
            if (ModelParam.Sensor == null) return;

            object value;
            // 读取无效数据保持
            if (ModelParam.Sensor.GetSingleParam("WarningHoldPoints", out value))
            {
                ModelParam.BasicSettings.InvalidDataHold = (int)value;
            }

            // 读取采样间隔
            if (ModelParam.Sensor.GetSingleParam("SamplingInterval", out value))
            {
                ModelParam.BasicSettings.SelectedSamplingInterval = (SAMPLING_INTERVAL)value;
            }

            // 读取滑动平均滤波
            if (ModelParam.Sensor.GetSingleParam("MoveAverage", out value))
            {
                ModelParam.BasicSettings.MovingAverageWindow = (FILTER_WINDOW_WIDTH)value;
            }

            // 读取测量模式
            if (ModelParam.Sensor.GetSingleParam("MeasureMode", out value))
            {
                ModelParam.BasicSettings.SelectedMeasureMode = (MeasureMode)value;
            }

            // 读取通道使能
            if (ModelParam.Sensor.GetSingleParam("ChannelEnable", out value))
            {
                ChannelEnable ce = (ChannelEnable)value;
                ModelParam.BasicSettings.MaxChannelCount = ce.channelCnt;
                ModelParam.BasicSettings.Channel1Enable = (ce.channelState & (short)CHANNEL_ENABLE_MODE.CH1) != 0;
                ModelParam.BasicSettings.Channel2Enable = (ce.channelState & (short)CHANNEL_ENABLE_MODE.CH2) != 0;
                ModelParam.BasicSettings.Channel3Enable = (ce.channelState & (short)CHANNEL_ENABLE_MODE.CH3) != 0;
                ModelParam.BasicSettings.Channel4Enable = (ce.channelState & (short)CHANNEL_ENABLE_MODE.CH4) != 0;
            }
        }

        /// <summary>
        /// 从设备加载触发配置
        /// </summary>
        private void LoadTriggerSettings()
        {
            if (ModelParam.Sensor == null) return;

            object value;
            
            // ========== 加载外部触发配置（ExternalTrigger）==========
            if (ModelParam.Sensor.GetSingleParam("ExternalTriggerConfig", out value))
            {
                ExternalTrigger et = (ExternalTrigger)value;
                
                // 映射触发采样模式（根据TRIG_METHOD和SyncSetting组合判断）
                ModelParam.TriggerConfig.TriggerSamplingMode = GetTriggerSamplingModeFromApi(et);
                
                // 映射采样使能电平
                ModelParam.TriggerConfig.SamplingEnableLevel = et.sync_setting.valid_level == SYNC_VALID_LEVEL.LOW 
                    ? SamplingEnableLevel.低电平_下降沿 
                    : SamplingEnableLevel.高电平_上升沿;
                
                // 单脉冲采样个数
                ModelParam.TriggerConfig.SinglePulseSamplingCount = et.sync_setting.sample_per_trigger;
                
                // 映射滤波宽度
                ModelParam.TriggerConfig.TriggerFilterWidth = (TriggerFilterWidth)et.sync_setting.filter_width;
            }
            
            // ========== 加载触发配置（TriggerSetting）==========
            if (ModelParam.Sensor.GetSingleParam("TriggerConfig", out value))
            {
                TriggerSetting ts = (TriggerSetting)value;
                
                // 映射触发通道
                ModelParam.TriggerConfig.TriggerChannel = ts.channel == ENCODER_CHANNEL.CH1 
                    ? TriggerChannel.编码器1 
                    : TriggerChannel.编码器2;
                
                // 映射触发模式
                ModelParam.TriggerConfig.TriggerMode = ts.mode == TRIG_MODE.POSITION 
                    ? TriggerMode.位置触发 
                    : TriggerMode.计数触发;
                
                // 映射触发方向
                switch (ts.direction)
                {
                    case TRIG_DIRECTION.POS:
                        ModelParam.TriggerConfig.TriggerDirection = TriggerDirection.正向;
                        break;
                    case TRIG_DIRECTION.NEG:
                        ModelParam.TriggerConfig.TriggerDirection = TriggerDirection.反向;
                        break;
                    default:
                        ModelParam.TriggerConfig.TriggerDirection = TriggerDirection.双向;
                        break;
                }
                
                // 映射追踪模式
                ModelParam.TriggerConfig.TrackingMode = ts.track_mode == TRIG_TRACK_MODE.ON 
                    ? TrackingMode.开 
                    : TrackingMode.关;
                
                // 触发间隔
                ModelParam.TriggerConfig.TriggerInterval = ts.downsample_factor;
            }
        }

        /// <summary>
        /// 从设备加载编码器配置
        /// </summary>
        private void LoadEncoderSettings()
        {
            if (ModelParam.Sensor == null) return;

            object value;

            // ========== 编码器1 ==========
            // 读取使能状态
            if (ModelParam.Sensor.GetSingleParam("Encoder1_Enable", out value))
            {
                ModelParam.TriggerConfig.Encoder1.IsEnabled = (bool)value;
            }

            // 读取编码器配置
            if (ModelParam.Sensor.GetSingleParam("Encoder1_Config", out value))
            {
                EncoderSetting es = (EncoderSetting)value;
                
                // 映射输入模式
                ModelParam.TriggerConfig.Encoder1.InputMode = es.input_mode == ENCODER_INPUT_MODE.A 
                    ? EncoderInputMode.单路 
                    : EncoderInputMode.双路;
                
                // 映射解码模式
                switch (es.output_mode)
                {
                    case ENCODER_OUTPUT_MODE.X1:
                        ModelParam.TriggerConfig.Encoder1.DecodeMode = EncoderDecodeMode.X1;
                        break;
                    case ENCODER_OUTPUT_MODE.X2:
                        ModelParam.TriggerConfig.Encoder1.DecodeMode = EncoderDecodeMode.X2;
                        break;
                    default:
                        ModelParam.TriggerConfig.Encoder1.DecodeMode = EncoderDecodeMode.X4;
                        break;
                }
                
                // Z相使能
                ModelParam.TriggerConfig.Encoder1.ZPhaseEnable = es.z_phase;
            }

            // 读取脉冲比例系数
            if (ModelParam.Sensor.GetSingleParam("Encoder1_PulseRatio", out value))
            {
                ModelParam.TriggerConfig.Encoder1.PulseRatio = (double)value;
            }

            // 读取手动置位值
            if (ModelParam.Sensor.GetSingleParam("Encoder1_ManualResetValue", out value))
            {
                ModelParam.TriggerConfig.Encoder1.ManualResetValue = (double)value;
            }

            // 读取Z信号置位值
            if (ModelParam.Sensor.GetSingleParam("Encoder1_ZSignalResetValue", out value))
            {
                ModelParam.TriggerConfig.Encoder1.ZSignalResetValue = (double)value;
            }

            // ========== 编码器2 ==========
            // 读取使能状态
            if (ModelParam.Sensor.GetSingleParam("Encoder2_Enable", out value))
            {
                ModelParam.TriggerConfig.Encoder2.IsEnabled = (bool)value;
            }

            // 读取编码器配置
            if (ModelParam.Sensor.GetSingleParam("Encoder2_Config", out value))
            {
                EncoderSetting es = (EncoderSetting)value;
                
                // 映射输入模式
                ModelParam.TriggerConfig.Encoder2.InputMode = es.input_mode == ENCODER_INPUT_MODE.A 
                    ? EncoderInputMode.单路 
                    : EncoderInputMode.双路;
                
                // 映射解码模式
                switch (es.output_mode)
                {
                    case ENCODER_OUTPUT_MODE.X1:
                        ModelParam.TriggerConfig.Encoder2.DecodeMode = EncoderDecodeMode.X1;
                        break;
                    case ENCODER_OUTPUT_MODE.X2:
                        ModelParam.TriggerConfig.Encoder2.DecodeMode = EncoderDecodeMode.X2;
                        break;
                    default:
                        ModelParam.TriggerConfig.Encoder2.DecodeMode = EncoderDecodeMode.X4;
                        break;
                }
                
                // Z相使能
                ModelParam.TriggerConfig.Encoder2.ZPhaseEnable = es.z_phase;
            }

            // 读取脉冲比例系数
            if (ModelParam.Sensor.GetSingleParam("Encoder2_PulseRatio", out value))
            {
                ModelParam.TriggerConfig.Encoder2.PulseRatio = (double)value;
            }

            // 读取手动置位值
            if (ModelParam.Sensor.GetSingleParam("Encoder2_ManualResetValue", out value))
            {
                ModelParam.TriggerConfig.Encoder2.ManualResetValue = (double)value;
            }

            // 读取Z信号置位值
            if (ModelParam.Sensor.GetSingleParam("Encoder2_ZSignalResetValue", out value))
            {
                ModelParam.TriggerConfig.Encoder2.ZSignalResetValue = (double)value;
            }
        }

        /// <summary>
        /// 根据API返回的ExternalTrigger结构体判断触发采样模式
        /// </summary>
        private TriggerSamplingMode GetTriggerSamplingModeFromApi(ExternalTrigger et)
        {
            // 根据TRIG_METHOD和SyncSetting组合判断5种模式
            switch (et.trig_method)
            {
                case TRIG_METHOD.NONE:
                    // 无触发 = 固定时间间隔采样
                    if (et.sync_setting.state == STATE.ON && et.sync_setting.input_mode == SYNC_INPUT_MODE.LEVEL)
                        return TriggerSamplingMode.固定时间间隔采样_SYNC_IN控制数据输出;
                    return TriggerSamplingMode.固定时间间隔采样;
                    
                case TRIG_METHOD.ENCODER:
                    // 编码器触发
                    if (et.sync_setting.state == STATE.ON && et.sync_setting.input_mode == SYNC_INPUT_MODE.LEVEL)
                        return TriggerSamplingMode.编码器触发采样_SYNC_IN控制数据输出;
                    return TriggerSamplingMode.编码器触发采样;
                    
                case TRIG_METHOD.SYNCIN:
                    // 同步触发 = SYNC IN边沿触发
                    return TriggerSamplingMode.SYNC_IN边沿触发以固定时间间隔采样N点;
                    
                default:
                    return TriggerSamplingMode.固定时间间隔采样;
            }
        }

        /// <summary>
        /// 设置触发采样模式（将UI的5种模式映射到API参数）
        /// </summary>
        private void SetTriggerSamplingMode(TriggerSamplingMode mode)
        {
            if (ModelParam.Sensor == null) return;
            
            // 先获取当前配置
            object value;
            ExternalTrigger et = new ExternalTrigger();
            if (ModelParam.Sensor.GetSingleParam("ExternalTriggerConfig", out value))
            {
                et = (ExternalTrigger)value;
            }
            
            // 根据UI模式设置API参数
            switch (mode)
            {
                case TriggerSamplingMode.固定时间间隔采样:
                    et.trig_method = TRIG_METHOD.NONE;
                    et.sync_setting.state = STATE.OFF;
                    break;
                    
                case TriggerSamplingMode.固定时间间隔采样_SYNC_IN控制数据输出:
                    et.trig_method = TRIG_METHOD.NONE;
                    et.sync_setting.state = STATE.ON;
                    et.sync_setting.input_mode = SYNC_INPUT_MODE.LEVEL;
                    break;
                    
                case TriggerSamplingMode.编码器触发采样:
                    et.trig_method = TRIG_METHOD.ENCODER;
                    et.sync_setting.state = STATE.OFF;
                    break;
                    
                case TriggerSamplingMode.编码器触发采样_SYNC_IN控制数据输出:
                    et.trig_method = TRIG_METHOD.ENCODER;
                    et.sync_setting.state = STATE.ON;
                    et.sync_setting.input_mode = SYNC_INPUT_MODE.LEVEL;
                    break;
                    
                case TriggerSamplingMode.SYNC_IN边沿触发以固定时间间隔采样N点:
                    et.trig_method = TRIG_METHOD.SYNCIN;
                    et.sync_setting.state = STATE.ON;
                    et.sync_setting.input_mode = SYNC_INPUT_MODE.EDGE;
                    break;
            }
            
            // 一次性设置整个ExternalTrigger结构体到设备
            ModelParam.Sensor.SetSingleParam("ExternalTriggerConfig", et);
        }

        /// <summary>
        /// 设置通道使能
        /// </summary>
        private void SetChannelEnable()
        {
            if (ModelParam.Sensor == null) return;
            
            // 至少保证一个通道使能
            if (!ModelParam.BasicSettings.Channel1Enable && 
                !ModelParam.BasicSettings.Channel2Enable && 
                !ModelParam.BasicSettings.Channel3Enable && 
                !ModelParam.BasicSettings.Channel4Enable)
            {
                MessageView.Ins.MessageBoxShow("至少需要使能一个通道！", eMsgType.Warn);
                ModelParam.BasicSettings.Channel1Enable = true;
                return;
            }
            
            // 构建通道状态位
            short channelState = 0;
            if (ModelParam.BasicSettings.Channel1Enable) channelState |= (short)CHANNEL_ENABLE_MODE.CH1;
            if (ModelParam.BasicSettings.Channel2Enable) channelState |= (short)CHANNEL_ENABLE_MODE.CH2;
            if (ModelParam.BasicSettings.Channel3Enable) channelState |= (short)CHANNEL_ENABLE_MODE.CH3;
            if (ModelParam.BasicSettings.Channel4Enable) channelState |= (short)CHANNEL_ENABLE_MODE.CH4;
            
            ChannelEnable ce = new ChannelEnable
            {
                channelCnt = ModelParam.BasicSettings.MaxChannelCount,
                channelState = channelState
            };
            
            ModelParam.Sensor.SetSingleParam("ChannelEnable", ce);
        }

        private void SyncMultiMathConfigToSensor(bool applyToDevice)
        {
            if (_isInitializingMultiMathConfig || ModelParam.Sensor == null) return;

            ModelParam.Sensor.UpdateMultiMathChannels(ModelParam.MultiMathConfig.Channels);
            if (applyToDevice && ModelParam.Sensor.IsConnected)
            {
                ModelParam.Sensor.ApplyConfiguredMultiMathSettings();
            }
        }

        /// <summary>
        /// 从设备加载标定配置
        /// </summary>
        private void LoadCalibrationSettings()
        {
            if (ModelParam.Sensor == null) return;

            object value;
            // 读取修正系数
            if (ModelParam.Sensor.GetSingleParam("CorrectionFactor", out value))
            {
                ModelParam.CalibrationConfig.CorrectionFactor = (double)value;
            }
        }

        /// <summary>
        /// 采集标定数据（多次采集取平均）
        /// </summary>
        /// <returns>平均测量值，失败返回null</returns>
        private double? CollectCalibrationData()
        {
            if (ModelParam.Sensor == null) return null;
            
            int averageCount = ModelParam.CalibrationConfig.AverageCount;
            if (averageCount < 1) averageCount = 1;
            
            double sum = 0;
            int successCount = 0;
            
            // 多次采集取平均
            for (int i = 0; i < averageCount; i++)
            {
                var result = ModelParam.Sensor.ExecuteCommand("GetSingleMeasurement");
                if (result != null && result is double value)
                {
                    sum += value;
                    successCount++;
                }
                // 短暂延时，避免采集过快
                if (i < averageCount - 1)
                    System.Threading.Thread.Sleep(50);
            }
            
            if (successCount > 0)
            {
                return sum / successCount;
            }
            return null;
        }

        /// <summary>
        /// 初始化折射率配置（设置路径，恢复会话状态）
        /// </summary>
        private void InitRefractiveConfig()
        {
            // 确保默认文件存在
            RefractiveConfigModel.EnsureDefaultFileExists();
            
            // 使用会话路径（如果有），否则使用默认路径
            ModelParam.RefractiveConfig.FilePath = RefractiveConfigModel.GetSessionOrDefaultPath();
            
            // 恢复会话表格数据（如果有），否则初始化空表格
            if (RefractiveConfigModel.HasSessionTableData)
            {
                RestoreSessionTableData();
            }
            else
            {
                InitEmptyRefractiveTable();
            }
        }

        /// <summary>
        /// 从会话恢复表格数据
        /// </summary>
        private void RestoreSessionTableData()
        {
            var sessionData = RefractiveConfigModel.GetSessionTableData();
            if (sessionData == null) return;
            
            ModelParam.RefractiveConfig.RefractiveTableItems.Clear();
            foreach (var item in sessionData)
            {
                ModelParam.RefractiveConfig.RefractiveTableItems.Add(new Models.RefractiveTableItem
                {
                    Parent = ModelParam.RefractiveConfig,
                    Index = item.Index,
                    Material = item.Material,
                    C486nm = item.C486nm,
                    C587nm = item.C587nm,
                    C656nm = item.C656nm,
                    IsSelected = item.IsSelected
                });
            }
        }

        /// <summary>
        /// 初始化空的折射率表（16行，API限制最大16个）
        /// </summary>
        private void InitEmptyRefractiveTable()
        {
            const int MAX_REFRACTIVE_TABLE_SIZE = 16;
            
            ModelParam.RefractiveConfig.RefractiveTableItems.Clear();
            for (int i = 1; i <= MAX_REFRACTIVE_TABLE_SIZE; i++)
            {
                ModelParam.RefractiveConfig.RefractiveTableItems.Add(new Models.RefractiveTableItem
                {
                    Parent = ModelParam.RefractiveConfig,
                    Index = i,
                    Material = "--",
                    C486nm = "--",
                    C587nm = "--",
                    C656nm = "--",
                    IsSelected = false
                });
            }
            
            // 通道和层序号已在Model中初始化 (CH1-CH4, 1-5)
        }

        private void InitRefreshChartTimer()
        {
            _RefreshChartTimer = new DispatcherTimer();
            _RefreshChartTimer.Interval = TimeSpan.FromMilliseconds(50);
            _RefreshChartTimer.Tick += (s, e) => RefreshChart();
            _RefreshChartTimer.Start();
        }

        /// <summary>
        /// 初始化图表样式
        /// </summary>
        private void InitChartStyle()
        {
            //_singlePointChart.ChartName = "PointLineSeries chart";
            //_singlePointChart.ViewXY.LegendBoxes[0].Visible = false;
            //_singlePointChart.ViewXY.XAxes[0].SetRange(0, 2048);
            //_singlePointChart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
            //_singlePointChart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;

            //_outlineChart.ChartName = "PointLineSeries chart";
            //_outlineChart.ViewXY.LegendBoxes[0].Visible = false;
            //_outlineChart.ViewXY.XAxes[0].SetRange(0, 2048);
            //_outlineChart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;
            //_outlineChart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
        }

        private void RefreshChart()
        {
            // 刷新图表逻辑


        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "执行":
                    break;

                case "原始图像":
                    {
                        if (ModelParam.Sensor == null) return;

                        PrismProvider.DialogService.Show(nameof(TronSightRawImageWindow), new DialogParameters
                        {
                            { "Guid", Guid },
                            { "Title", "原始图像" },
                            { "Icon", "\ue640" },
                            { "Param", ModelParam.Sensor },
                            { "Serial", Serial },
                            { "Visibility", Visibility.Visible },
                            { TronSightRawImageWindowViewModel.RawImageConfigParameterName, ModelParam.RawImageConfig },
                        }, _ => { }, nameof(SingleInstanceDialogWindowView));
                    }
                    break;

                case "取消":
                    _RefreshChartTimer?.Stop();
                    CloseDialog(ButtonResult.No);
                    break;

                case "确认":
                    _RefreshChartTimer?.Stop();
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.Sensor },
                    });
                    break;

                #region 配置管理
                case "恢复出厂配置":
                    {
                        if (ModelParam.Sensor == null) return;
                        var result = ModelParam.Sensor.ExecuteCommand("RestoreFactorySetting");
                        if (result != null)
                        {
                            MessageView.Ins.MessageBoxShow("恢复出厂配置成功！", eMsgType.Info);
                            // 重新加载参数到UI
                            Init();
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("恢复出厂配置失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "保存配置到文件":
                    {
                        if (ModelParam.Sensor == null) return;
                        var saveDialog = new Microsoft.Win32.SaveFileDialog
                        {
                            Filter = "配置文件 (*.cfg)|*.cfg|所有文件 (*.*)|*.*",
                            DefaultExt = ".cfg",
                            FileName = "TronSight_Config"
                        };
                        if (saveDialog.ShowDialog() == true)
                        {
                            var result = ModelParam.Sensor.ExecuteCommand("SaveConfigToFile", saveDialog.FileName);
                            if (result != null)
                            {
                                MessageView.Ins.MessageBoxShow($"配置已保存到: {saveDialog.FileName}", eMsgType.Info);
                            }
                            else
                            {
                                MessageView.Ins.MessageBoxShow("保存配置失败！", eMsgType.Error);
                            }
                        }
                    }
                    break;

                case "从文件读取配置":
                    {
                        if (ModelParam.Sensor == null) return;
                        var openDialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "配置文件 (*.cfg)|*.cfg|所有文件 (*.*)|*.*",
                            DefaultExt = ".cfg"
                        };
                        if (openDialog.ShowDialog() == true)
                        {
                            var result = ModelParam.Sensor.ExecuteCommand("ReadConfigFromFile", openDialog.FileName);
                            if (result != null)
                            {
                                MessageView.Ins.MessageBoxShow($"已从文件读取配置: {openDialog.FileName}", eMsgType.Info);
                                // 重新加载参数到UI
                                Init();
                                
                                // 如果勾选了固化参数，则写入Flash
                                if (ModelParam.DeviceConfig.IsSolidifyParams)
                                {
                                    ModelParam.Sensor.ExecuteCommand("WriteConfigToFlash");
                                }
                            }
                            else
                            {
                                MessageView.Ins.MessageBoxShow("读取配置失败！", eMsgType.Error);
                            }
                        }
                    }
                    break;

                case "写入配置到Flash":
                    {
                        if (ModelParam.Sensor == null) return;
                        var result = ModelParam.Sensor.ExecuteCommand("WriteConfigToFlash");
                        if (result != null)
                        {
                            MessageView.Ins.MessageBoxShow("配置已写入Flash，断电后仍然生效！", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("写入Flash失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "从传感器读取配置":
                    {
                        if (ModelParam.Sensor == null) return;
                        var result = ModelParam.Sensor.ExecuteCommand("ReadConfigFromSensor");
                        if (result != null)
                        {
                            MessageView.Ins.MessageBoxShow("已从传感器读取配置！", eMsgType.Info);
                            // 重新加载参数到UI
                            Init();
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("从传感器读取配置失败！", eMsgType.Error);
                        }
                    }
                    break;
                #endregion

                #region 触发复位
                case "触发复位":
                    {
                        if (ModelParam.Sensor == null) return;
                        var result = ModelParam.Sensor.ExecuteCommand("ResetTriggerCounter");
                        if (result != null)
                        {
                            MessageView.Ins.MessageBoxShow("触发计数器已复位！", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("触发复位失败！", eMsgType.Error);
                        }
                    }
                    break;
                #endregion

                #region 标定功能（MATH标定）
                case "采集标定数据1":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        double? measuredValue = CollectCalibrationData();
                        if (measuredValue.HasValue)
                        {
                            ModelParam.CalibrationConfig.MeasuredValue = measuredValue.Value;
                            MessageView.Ins.MessageBoxShow($"采集成功！ 平均测量值: {measuredValue.Value:F6}", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("采集标定数据失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "采集标定数据2":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        double? measuredValue = CollectCalibrationData();
                        if (measuredValue.HasValue)
                        {
                            ModelParam.CalibrationConfig.MeasuredValue2 = measuredValue.Value;
                            MessageView.Ins.MessageBoxShow($"采集成功！ 平均测量值: {measuredValue.Value:F6}", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("采集标定数据失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "执行标定":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        bool isTwoPointCalibration = ModelParam.CalibrationConfig.CalibrationMethod == CalibrationMethod.两点标定;
                        
                        double s1 = ModelParam.CalibrationConfig.StandardValue;  // 实际值1
                        double m1 = ModelParam.CalibrationConfig.MeasuredValue;  // 测量值1
                        
                        if (Math.Abs(m1) < 0.000001)
                        {
                            MessageView.Ins.MessageBoxShow("请先采集标定数据1！", eMsgType.Warn);
                            return;
                        }
                        
                        double k = 1.0;  // 系数
                        double b = 0.0;  // 偏移
                        
                        if (isTwoPointCalibration)
                        {
                            // 两点标定：T = k * M + b
                            // s1 = k * m1 + b
                            // s2 = k * m2 + b
                            // 解方程：k = (s1 - s2) / (m1 - m2), b = s1 - k * m1
                            double s2 = ModelParam.CalibrationConfig.StandardValue2;
                            double m2 = ModelParam.CalibrationConfig.MeasuredValue2;
                            
                            if (Math.Abs(m2) < 0.000001)
                            {
                                MessageView.Ins.MessageBoxShow("请先采集标定数据2！", eMsgType.Warn);
                                return;
                            }
                            
                            if (Math.Abs(m1 - m2) < 0.000001)
                            {
                                MessageView.Ins.MessageBoxShow("两组测量值相同，无法进行两点标定！", eMsgType.Error);
                                return;
                            }
                            
                            k = (s1 - s2) / (m1 - m2);
                            b = s1 - k * m1;
                        }
                        else
                        {
                            // 单点标定：T = M + b（假设k=1，只标定偏移）
                            // s1 = m1 + b => b = s1 - m1
                            k = 1.0;
                            b = s1 - m1;
                        }
                        
                        // 设置映射系数k和偏移b
                        var resultK = ModelParam.Sensor.ExecuteCommand("SetMappingFactor", k);
                        var resultB = ModelParam.Sensor.ExecuteCommand("SetZeroOffset", b);
                        
                        if (resultK != null && resultB != null)
                        {
                            ModelParam.CalibrationConfig.CalibrationK = k;
                            ModelParam.CalibrationConfig.CalibrationB = b;
                            
                            string msg = isTwoPointCalibration 
                                ? $"两点标定成功！\n标定值1: {s1:F6}, 测量值1: {m1:F6}\n标定值2: {ModelParam.CalibrationConfig.StandardValue2:F6}, 测量值2: {ModelParam.CalibrationConfig.MeasuredValue2:F6}\n系数k: {k:F6}\n偏移b: {b:F6}"
                                : $"单点标定成功！\n标定值: {s1:F6}\n测量值: {m1:F6}\n偏移b: {b:F6}";
                            MessageView.Ins.MessageBoxShow(msg, eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("执行标定失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "取消标定":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        // 重置映射系数为1.0，偏移为0
                        var resultK = ModelParam.Sensor.ExecuteCommand("SetMappingFactor", 1.0);
                        var resultB = ModelParam.Sensor.ExecuteCommand("SetZeroOffset", 0.0);
                        
                        if (resultK != null && resultB != null)
                        {
                            ModelParam.CalibrationConfig.MeasuredValue = 0;
                            ModelParam.CalibrationConfig.MeasuredValue2 = 0;
                            ModelParam.CalibrationConfig.CalibrationK = 1.0;
                            ModelParam.CalibrationConfig.CalibrationB = 0.0;
                            MessageView.Ins.MessageBoxShow("已取消标定，系数已重置为k=1.0, b=0", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("取消标定失败！", eMsgType.Error);
                        }
                    }
                    break;
                #endregion

                #region 折射率功能
                case "由控制器读取折射率":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        // 获取折射率表标签列表
                        var labelsResult = ModelParam.Sensor.ExecuteCommand("GetRefractiveTableLabels");
                        if (labelsResult is int[] labels && labels.Length > 0)
                        {
                            ModelParam.RefractiveConfig.RefractiveTableItems.Clear();
                            
                            int index = 1;
                            foreach (var label in labels)
                            {
                                // 下载每个折射率表
                                var tableResult = ModelParam.Sensor.ExecuteCommand("DownloadRefractiveTable", label);
                                if (tableResult is RefractiveTable table)
                                {
                                    ModelParam.RefractiveConfig.RefractiveTableItems.Add(new Models.RefractiveTableItem
                                    {
                                        Parent = ModelParam.RefractiveConfig,
                                        Index = index++,
                                        Material = table.object_name ?? "--",
                                        C486nm = table.refractive_data.c486.ToString("F5"),
                                        C587nm = table.refractive_data.c587.ToString("F5"),
                                        C656nm = table.refractive_data.c656.ToString("F5"),
                                        IsSelected = false
                                    });
                                }
                            }
                            
                            // 补齐到16行
                            while (ModelParam.RefractiveConfig.RefractiveTableItems.Count < 16)
                            {
                                ModelParam.RefractiveConfig.RefractiveTableItems.Add(new Models.RefractiveTableItem
                                {
                                    Parent = ModelParam.RefractiveConfig,
                                    Index = ModelParam.RefractiveConfig.RefractiveTableItems.Count + 1,
                                    Material = "--",
                                    C486nm = "--",
                                    C587nm = "--",
                                    C656nm = "--",
                                    IsSelected = false
                                });
                            }
                            
                            // 保存表格数据到会话
                            RefractiveConfigModel.SaveSessionTableData(ModelParam.RefractiveConfig.RefractiveTableItems);
                            MessageView.Ins.MessageBoxShow($"成功读取 {labels.Length} 个折射率表", eMsgType.Info);
                        }
                        else
                        {
                            // 初始化空表格
                            InitEmptyRefractiveTable();
                            MessageView.Ins.MessageBoxShow("控制器中无折射率表数据", eMsgType.Info);
                        }
                    }
                    break;

                case "上传折射率到控制器":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        var selectedItems = ModelParam.RefractiveConfig.RefractiveTableItems.Where(x => x.IsSelected).ToList();
                        if (selectedItems.Count == 0)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择要上传的折射率表项", eMsgType.Warn);
                            return;
                        }
                        
                        int successCount = 0;
                        foreach (var item in selectedItems)
                        {
                            var table = new RefractiveTable
                            {
                                object_name = item.Material,
                                refractive_data = new RefractiveCoeff
                                {
                                    c486 = double.TryParse(item.C486nm, out var c486) ? c486 : 0,
                                    c587 = double.TryParse(item.C587nm, out var c587) ? c587 : 0,
                                    c656 = double.TryParse(item.C656nm, out var c656) ? c656 : 0
                                }
                            };
                            
                            var result = ModelParam.Sensor.ExecuteCommand("UploadRefractiveTable", Tuple.Create(item.Index, table));
                            if (result != null) successCount++;
                        }
                        
                        MessageView.Ins.MessageBoxShow($"成功上传 {successCount}/{selectedItems.Count} 个折射率表", eMsgType.Info);
                    }
                    break;

                case "使用选中折射率":
                    {
                        if (ModelParam.Sensor == null) return;
                        
                        var selectedItem = ModelParam.RefractiveConfig.RefractiveTableItems.FirstOrDefault(x => x.IsSelected);
                        if (selectedItem == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个折射率表项", eMsgType.Warn);
                            return;
                        }
                        
                        // 从 "CH1" 提取数字 1
                        string channelStr = ModelParam.RefractiveConfig.CurrentChannel;
                        int channel = int.Parse(channelStr.Replace("CH", ""));
                        int label = selectedItem.Index;
                        
                        var result = ModelParam.Sensor.ExecuteCommand("SetCurrentRefractiveTableLabel", Tuple.Create(channel, label));
                        if (result != null)
                        {
                            MessageView.Ins.MessageBoxShow($"通道 {channelStr} 已设置使用折射率表 {label}", eMsgType.Info);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("设置折射率表失败！", eMsgType.Error);
                        }
                    }
                    break;

                case "清空折射率表":
                    {
                        ModelParam.RefractiveConfig.RefractiveTableItems.Clear();
                        InitEmptyRefractiveTable();
                    }
                    break;

                case "选择折射率文件路径":
                    {
                        var dialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "文本文件|*.txt|所有文件|*.*",
                            Title = "选择折射率文件"
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            ModelParam.RefractiveConfig.FilePath = dialog.FileName;
                        }
                    }
                    break;

                case "下载折射率到硬盘":
                    {
                        // 使用上方路径框中的路径
                        string filePath = ModelParam.RefractiveConfig.FilePath;
                        if (string.IsNullOrWhiteSpace(filePath))
                        {
                            MessageView.Ins.MessageBoxShow("请先选择文件路径", eMsgType.Warn);
                            break;
                        }
                        
                        try
                        {
                            // 保存格式与官方一致: 材料,486nm,587nm,656nm (无序号，逗号分隔)
                            var lines = new System.Collections.Generic.List<string>();
                            foreach (var item in ModelParam.RefractiveConfig.RefractiveTableItems)
                            {
                                string material = item.Material == "--" ? "" : item.Material;
                                lines.Add($"{material},{item.C486nm},{item.C587nm},{item.C656nm}");
                            }
                            System.IO.File.WriteAllLines(filePath, lines);
                            MessageView.Ins.MessageBoxShow("折射率表已保存到文件", eMsgType.Info);
                        }
                        catch (Exception ex)
                        {
                            MessageView.Ins.MessageBoxShow($"保存失败：{ex.Message}", eMsgType.Error);
                        }
                    }
                    break;

                case "由硬盘读取折射率":
                    {
                        // 使用上方路径框中的路径
                        string filePath = ModelParam.RefractiveConfig.FilePath;
                        if (string.IsNullOrWhiteSpace(filePath))
                        {
                            MessageView.Ins.MessageBoxShow("请先选择文件路径", eMsgType.Warn);
                            break;
                        }
                        
                        if (!System.IO.File.Exists(filePath))
                        {
                            MessageView.Ins.MessageBoxShow("文件不存在，请检查路径", eMsgType.Warn);
                            break;
                        }
                        
                        try
                        {
                            var lines = System.IO.File.ReadAllLines(filePath);
                            var tempItems = new System.Collections.Generic.List<Models.RefractiveTableItem>();
                            
                            int index = 1;
                            int invalidLineCount = 0;
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                if (index > 16) break; // 最多16行
                                
                                // 支持逗号或制表符分隔
                                char separator = line.Contains(',') ? ',' : '\t';
                                var parts = line.Split(separator);
                                
                                // 官方格式: 材料,486nm,587nm,656nm (4列，无序号)
                                if (parts.Length == 4)
                                {
                                    tempItems.Add(new Models.RefractiveTableItem
                                    {
                                        Parent = ModelParam.RefractiveConfig,
                                        Index = index++,
                                        Material = string.IsNullOrWhiteSpace(parts[0]) ? "--" : parts[0].Trim(),
                                        C486nm = parts[1].Trim(),
                                        C587nm = parts[2].Trim(),
                                        C656nm = parts[3].Trim(),
                                        IsSelected = false
                                    });
                                }
                                else
                                {
                                    invalidLineCount++;
                                }
                            }
                            
                            // 检查是否有有效数据
                            if (tempItems.Count == 0)
                            {
                                MessageView.Ins.MessageBoxShow("数据长度不一致，读取失败！\n文件格式应为：材料,486nm,587nm,656nm（4列）", eMsgType.Error);
                                break;
                            }
                            
                            // 如果有无效行，提示警告但继续读取
                            if (invalidLineCount > 0)
                            {
                                MessageView.Ins.MessageBoxShow($"数据长度不一致，{invalidLineCount}行格式错误已跳过！\n文件格式应为：材料,486nm,587nm,656nm（4列）", eMsgType.Warn);
                            }
                            
                            // 读取成功，更新数据
                            ModelParam.RefractiveConfig.RefractiveTableItems.Clear();
                            foreach (var item in tempItems)
                            {
                                ModelParam.RefractiveConfig.RefractiveTableItems.Add(item);
                            }
                            
                            // 补齐到16行
                            while (ModelParam.RefractiveConfig.RefractiveTableItems.Count < 16)
                            {
                                ModelParam.RefractiveConfig.RefractiveTableItems.Add(new Models.RefractiveTableItem
                                {
                                    Parent = ModelParam.RefractiveConfig,
                                    Index = ModelParam.RefractiveConfig.RefractiveTableItems.Count + 1,
                                    Material = "--",
                                    C486nm = "--",
                                    C587nm = "--",
                                    C656nm = "--",
                                    IsSelected = false
                                });
                            }
                            
                            // 保存表格数据到会话
                            RefractiveConfigModel.SaveSessionTableData(ModelParam.RefractiveConfig.RefractiveTableItems);
                            
                            // 读取成功提示
                            MessageView.Ins.MessageBoxShow($"成功读取 {tempItems.Count} 条折射率数据", eMsgType.Info);
                        }
                        catch (Exception ex)
                        {
                            MessageView.Ins.MessageBoxShow($"读取失败：{ex.Message}", eMsgType.Error);
                        }
                    }
                    break;
                #endregion

                default:
                    break;
            }

        });

        /// <summary>
        /// 参数变更命令
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((paramName) =>
        {
            if (ModelParam.Sensor == null) return;

            switch (paramName)
            {
                case "无效数据保持":
                    ModelParam.Sensor.SetSingleParam("WarningHoldPoints", ModelParam.BasicSettings.InvalidDataHold);
                    break;

                case "采样间隔":
                    ModelParam.Sensor.SetSingleParam("SamplingInterval", ModelParam.BasicSettings.SelectedSamplingInterval);
                    break;

                case "滑动平均窗口宽度":
                    ModelParam.Sensor.SetSingleParam("MoveAverage", ModelParam.BasicSettings.MovingAverageWindow);
                    break;

                case "中值窗口宽度":
                    // 假设 API 支持中值滤波
                    // ModelParam.Sensor.SetSingleParam("MedianFilter", ModelParam.BasicSettings.MedianWindow);
                    break;

                case "测量模式":
                    ModelParam.Sensor.SetSingleParam("MeasureMode", ModelParam.BasicSettings.SelectedMeasureMode);
                    break;

                // 通道使能
                case "通道使能":
                    SetChannelEnable();
                    break;

                case "MultiMath配置":
                    SyncMultiMathConfigToSensor(true);
                    break;

                // 触发配置相关（ExternalTrigger）
                case "触发采样模式":
                    // 5种触发采样模式通过TRIG_METHOD和SyncSetting组合实现
                    SetTriggerSamplingMode(ModelParam.TriggerConfig.TriggerSamplingMode);
                    break;
                case "采样使能电平":
                    // 映射UI枚举到API枚举
                    SYNC_VALID_LEVEL apiValidLevel = ModelParam.TriggerConfig.SamplingEnableLevel == SamplingEnableLevel.低电平_下降沿 
                        ? SYNC_VALID_LEVEL.LOW 
                        : SYNC_VALID_LEVEL.HIGH;
                    ModelParam.Sensor.SetSingleParam("SamplingEnableLevel", apiValidLevel);
                    break;
                case "单脉冲采样个数":
                    ModelParam.Sensor.SetSingleParam("SinglePulseSamplingCount", ModelParam.TriggerConfig.SinglePulseSamplingCount);
                    break;
                case "触发滤波宽度":
                    // 映射UI枚举到API枚举
                    SYNC_FILTER_WIDTH apiFilterWidth = (SYNC_FILTER_WIDTH)ModelParam.TriggerConfig.TriggerFilterWidth;
                    ModelParam.Sensor.SetSingleParam("TriggerFilterWidth", apiFilterWidth);
                    break;
                case "触发通道":
                    ModelParam.Sensor.SetSingleParam("TriggerChannel", ModelParam.TriggerConfig.TriggerChannel);
                    break;
                case "触发模式":
                    ModelParam.Sensor.SetSingleParam("TriggerMode", ModelParam.TriggerConfig.TriggerMode);
                    break;
                case "触发方向":
                    ModelParam.Sensor.SetSingleParam("TriggerDirection", ModelParam.TriggerConfig.TriggerDirection);
                    break;
                case "追踪模式":
                    ModelParam.Sensor.SetSingleParam("TrackingMode", ModelParam.TriggerConfig.TrackingMode);
                    break;
                case "触发间隔":
                    ModelParam.Sensor.SetSingleParam("TriggerInterval", ModelParam.TriggerConfig.TriggerInterval);
                    break;

                // 编码器1
                case "Encoder1_Enable":
                    ModelParam.Sensor.SetSingleParam("Encoder1_Enable", ModelParam.TriggerConfig.Encoder1.IsEnabled);
                    break;
                case "Encoder1_InputMode":
                    ModelParam.Sensor.SetSingleParam("Encoder1_InputMode", ModelParam.TriggerConfig.Encoder1.InputMode);
                    break;
                case "Encoder1_DecodeMode":
                    ModelParam.Sensor.SetSingleParam("Encoder1_DecodeMode", ModelParam.TriggerConfig.Encoder1.DecodeMode);
                    break;
                case "Encoder1_ZPhaseEnable":
                    ModelParam.Sensor.SetSingleParam("Encoder1_ZPhaseEnable", ModelParam.TriggerConfig.Encoder1.ZPhaseEnable);
                    break;
                case "Encoder1_PulseRatio":
                    ModelParam.Sensor.SetSingleParam("Encoder1_PulseRatio", ModelParam.TriggerConfig.Encoder1.PulseRatio);
                    break;
                case "Encoder1_ManualResetValue":
                    ModelParam.Sensor.SetSingleParam("Encoder1_ManualResetValue", ModelParam.TriggerConfig.Encoder1.ManualResetValue);
                    break;
                case "Encoder1_ZSignalResetValue":
                    ModelParam.Sensor.SetSingleParam("Encoder1_ZSignalResetValue", ModelParam.TriggerConfig.Encoder1.ZSignalResetValue);
                    break;

                // 编码器2
                case "Encoder2_Enable":
                    ModelParam.Sensor.SetSingleParam("Encoder2_Enable", ModelParam.TriggerConfig.Encoder2.IsEnabled);
                    break;
                case "Encoder2_InputMode":
                    ModelParam.Sensor.SetSingleParam("Encoder2_InputMode", ModelParam.TriggerConfig.Encoder2.InputMode);
                    break;
                case "Encoder2_DecodeMode":
                    ModelParam.Sensor.SetSingleParam("Encoder2_DecodeMode", ModelParam.TriggerConfig.Encoder2.DecodeMode);
                    break;
                case "Encoder2_ZPhaseEnable":
                    ModelParam.Sensor.SetSingleParam("Encoder2_ZPhaseEnable", ModelParam.TriggerConfig.Encoder2.ZPhaseEnable);
                    break;
                case "Encoder2_PulseRatio":
                    ModelParam.Sensor.SetSingleParam("Encoder2_PulseRatio", ModelParam.TriggerConfig.Encoder2.PulseRatio);
                    break;
                case "Encoder2_ManualResetValue":
                    ModelParam.Sensor.SetSingleParam("Encoder2_ManualResetValue", ModelParam.TriggerConfig.Encoder2.ManualResetValue);
                    break;
                case "Encoder2_ZSignalResetValue":
                    ModelParam.Sensor.SetSingleParam("Encoder2_ZSignalResetValue", ModelParam.TriggerConfig.Encoder2.ZSignalResetValue);
                    break;
                
                // 标定
                case "CorrectionFactor":
                    ModelParam.Sensor.SetSingleParam("CorrectionFactor", ModelParam.CalibrationConfig.CorrectionFactor);
                    break;

                default:
                    break;
            }
        });

        #endregion

    }
}

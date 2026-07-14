using ReeYin.Hardware.Sensor.TronSight2.CustomUI.Models;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;
using API = ReeYin.Hardware.Sensor.TronSight2.API;

namespace ReeYin.Hardware.Sensor.TronSight2.CustomUI.ViewModels
{
    public class TronSight2SensorViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties
        private Grid _singlePointChartGrid = new Grid();
        public Grid SinglePointChartGrid
        {
            get { return _singlePointChartGrid; }
            set { _singlePointChartGrid = value; RaisePropertyChanged(); }
        }

        private Grid _outlineChartGrid = new Grid();
        public Grid OutlineChartGrid
        {
            get { return _outlineChartGrid; }
            set { _outlineChartGrid = value; RaisePropertyChanged(); }
        }

        private TronSight2SensorModel _modelParam = new TronSight2SensorModel();
        public TronSight2SensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private List<MeasureModeOption> _measureModeOptions = new List<MeasureModeOption>
        {
            new MeasureModeOption("干涉单层测厚", API.MEASUREMODE.INTERF_THICKNESS_SINGLE_LAYER),
            new MeasureModeOption("干涉多层测厚", API.MEASUREMODE.INTERF_THICKNESS_MULTI_LAYER)
        };
        public List<MeasureModeOption> MeasureModeOptions
        {
            get { return _measureModeOptions; }
            set { _measureModeOptions = value; RaisePropertyChanged(); }
        }

        private List<SamplingIntervalOption> _samplingIntervalOptions = new List<SamplingIntervalOption>
        {
            new SamplingIntervalOption("31.25 us", API.SAMPLING_INTERVAL._31_25US),
            new SamplingIntervalOption("40 us", API.SAMPLING_INTERVAL._40US),
            new SamplingIntervalOption("50 us", API.SAMPLING_INTERVAL._50US),
            new SamplingIntervalOption("62.5 us", API.SAMPLING_INTERVAL._62_5US),
            new SamplingIntervalOption("80 us", API.SAMPLING_INTERVAL._80US),
            new SamplingIntervalOption("100 us", API.SAMPLING_INTERVAL._100US),
            new SamplingIntervalOption("125 us", API.SAMPLING_INTERVAL._125US),
            new SamplingIntervalOption("160 us", API.SAMPLING_INTERVAL._160US),
            new SamplingIntervalOption("200 us", API.SAMPLING_INTERVAL._200US),
            new SamplingIntervalOption("250 us", API.SAMPLING_INTERVAL._250US),
            new SamplingIntervalOption("500 us", API.SAMPLING_INTERVAL._500US),
            new SamplingIntervalOption("1 ms", API.SAMPLING_INTERVAL._1MS),
            new SamplingIntervalOption("2 ms", API.SAMPLING_INTERVAL._2MS),
            new SamplingIntervalOption("5 ms", API.SAMPLING_INTERVAL._5MS),
            new SamplingIntervalOption("10 ms", API.SAMPLING_INTERVAL._10MS)
        };
        public List<SamplingIntervalOption> SamplingIntervalOptions
        {
            get { return _samplingIntervalOptions; }
            set { _samplingIntervalOptions = value; RaisePropertyChanged(); }
        }

        private List<FilterWindowWidthOption> _filterWindowWidthOptions = new List<FilterWindowWidthOption>
        {
            new FilterWindowWidthOption("1", API.FILTER_WINDOW_WIDTH._1),
            new FilterWindowWidthOption("2", API.FILTER_WINDOW_WIDTH._2),
            new FilterWindowWidthOption("4", API.FILTER_WINDOW_WIDTH._4),
            new FilterWindowWidthOption("16", API.FILTER_WINDOW_WIDTH._16),
            new FilterWindowWidthOption("64", API.FILTER_WINDOW_WIDTH._64),
            new FilterWindowWidthOption("256", API.FILTER_WINDOW_WIDTH._256),
            new FilterWindowWidthOption("1024", API.FILTER_WINDOW_WIDTH._1024),
            new FilterWindowWidthOption("4096", API.FILTER_WINDOW_WIDTH._4096)
        };
        public List<FilterWindowWidthOption> FilterWindowWidthOptions
        {
            get { return _filterWindowWidthOptions; }
            set { _filterWindowWidthOptions = value; RaisePropertyChanged(); }
        }

        private List<MedianFilterWidthOption> _medianFilterWidthOptions = new List<MedianFilterWidthOption>
        {
            new MedianFilterWidthOption("1", API.MEDIAN_FILTER_WIDTH._1),
            new MedianFilterWidthOption("3", API.MEDIAN_FILTER_WIDTH._3),
            new MedianFilterWidthOption("5", API.MEDIAN_FILTER_WIDTH._5),
            new MedianFilterWidthOption("9", API.MEDIAN_FILTER_WIDTH._9),
            new MedianFilterWidthOption("15", API.MEDIAN_FILTER_WIDTH._15),
            new MedianFilterWidthOption("31", API.MEDIAN_FILTER_WIDTH._31),
            new MedianFilterWidthOption("63", API.MEDIAN_FILTER_WIDTH._63)
        };
        public List<MedianFilterWidthOption> MedianFilterWidthOptions
        {
            get { return _medianFilterWidthOptions; }
            set { _medianFilterWidthOptions = value; RaisePropertyChanged(); }
        }

        private List<TriggerSampleModeOption> _triggerSampleModeOptions = new List<TriggerSampleModeOption>
        {
            new TriggerSampleModeOption("固定时间间隔采样", TriggerSampleMode.FixedInterval),
            new TriggerSampleModeOption("固定时间间隔采样+SYNC IN控制数据输出", TriggerSampleMode.FixedIntervalWithSyncOutput),
            new TriggerSampleModeOption("编码器触发采样", TriggerSampleMode.EncoderTrigger),
            new TriggerSampleModeOption("编码器触发采样+SYNC IN控制数据输出", TriggerSampleMode.EncoderTriggerWithSyncOutput),
            new TriggerSampleModeOption("SYNC IN边沿触发以固定时间间隔采样N点", TriggerSampleMode.SyncEdgeTriggerFixedIntervalNPoints)
        };
        public List<TriggerSampleModeOption> TriggerSampleModeOptions
        {
            get { return _triggerSampleModeOptions; }
            set { _triggerSampleModeOptions = value; RaisePropertyChanged(); }
        }

        private List<SyncValidLevelOption> _syncValidLevelOptions = new List<SyncValidLevelOption>
        {
            new SyncValidLevelOption("高电平", API.SYNC_VALID_LEVEL.HIGH),
            new SyncValidLevelOption("低电平", API.SYNC_VALID_LEVEL.LOW)
        };
        public List<SyncValidLevelOption> SyncValidLevelOptions
        {
            get { return _syncValidLevelOptions; }
            set { _syncValidLevelOptions = value; RaisePropertyChanged(); }
        }

        private List<SyncFilterWidthOption> _syncFilterWidthOptions = new List<SyncFilterWidthOption>
        {
            new SyncFilterWidthOption("0.1us", API.SYNC_FILTER_WIDTH._0_1_US),
            new SyncFilterWidthOption("0.4us", API.SYNC_FILTER_WIDTH._0_4_US),
            new SyncFilterWidthOption("1.6us", API.SYNC_FILTER_WIDTH._1_6_US),
            new SyncFilterWidthOption("6.4us", API.SYNC_FILTER_WIDTH._6_4_US),
            new SyncFilterWidthOption("25.6us", API.SYNC_FILTER_WIDTH._25_6_US),
            new SyncFilterWidthOption("102.4us", API.SYNC_FILTER_WIDTH._102_4_US),
            new SyncFilterWidthOption("409.6us", API.SYNC_FILTER_WIDTH._409_6_US),
            new SyncFilterWidthOption("1638.4us", API.SYNC_FILTER_WIDTH._1638_4US)
        };
        public List<SyncFilterWidthOption> SyncFilterWidthOptions
        {
            get { return _syncFilterWidthOptions; }
            set { _syncFilterWidthOptions = value; RaisePropertyChanged(); }
        }

        private List<EncoderTriggerChannelOption> _encoderTriggerChannelOptions = new List<EncoderTriggerChannelOption>
        {
            new EncoderTriggerChannelOption("编码器1", EncoderTriggerChannel.Encoder1),
            new EncoderTriggerChannelOption("编码器2", EncoderTriggerChannel.Encoder2),
            new EncoderTriggerChannelOption("编码器3", EncoderTriggerChannel.Encoder3)
        };
        public List<EncoderTriggerChannelOption> EncoderTriggerChannelOptions
        {
            get { return _encoderTriggerChannelOptions; }
            set { _encoderTriggerChannelOptions = value; RaisePropertyChanged(); }
        }

        private List<TriggerModeOption> _triggerModeOptions = new List<TriggerModeOption>
        {
            new TriggerModeOption("计数触发", API.TRIG_MODE.COUNTER),
            new TriggerModeOption("位置触发", API.TRIG_MODE.POSITION)
        };
        public List<TriggerModeOption> TriggerModeOptions
        {
            get { return _triggerModeOptions; }
            set { _triggerModeOptions = value; RaisePropertyChanged(); }
        }

        private List<TriggerDirectionOption> _triggerDirectionOptions = new List<TriggerDirectionOption>
        {
            new TriggerDirectionOption("正向", API.TRIG_DIRECTION.POS),
            new TriggerDirectionOption("负向", API.TRIG_DIRECTION.NEG),
            new TriggerDirectionOption("双向", API.TRIG_DIRECTION.BOTH)
        };
        public List<TriggerDirectionOption> TriggerDirectionOptions
        {
            get { return _triggerDirectionOptions; }
            set { _triggerDirectionOptions = value; RaisePropertyChanged(); }
        }

        private List<TriggerTrackModeOption> _triggerTrackModeOptions = new List<TriggerTrackModeOption>
        {
            new TriggerTrackModeOption("关", API.TRIG_TRACK_MODE.OFF),
            new TriggerTrackModeOption("开", API.TRIG_TRACK_MODE.ON)
        };
        public List<TriggerTrackModeOption> TriggerTrackModeOptions
        {
            get { return _triggerTrackModeOptions; }
            set { _triggerTrackModeOptions = value; RaisePropertyChanged(); }
        }

        private List<EncoderFilterWidthOption> _encoderFilterWidthOptions = new List<EncoderFilterWidthOption>
        {
            new EncoderFilterWidthOption("关闭", API.ENCODER_FILTER_WIDTH.NONE),
            new EncoderFilterWidthOption("4", API.ENCODER_FILTER_WIDTH._4),
            new EncoderFilterWidthOption("16", API.ENCODER_FILTER_WIDTH._16)
        };
        public List<EncoderFilterWidthOption> EncoderFilterWidthOptions
        {
            get { return _encoderFilterWidthOptions; }
            set { _encoderFilterWidthOptions = value; RaisePropertyChanged(); }
        }

        private List<EncoderInputModeOption> _encoderInputModeOptions = new List<EncoderInputModeOption>
        {
            new EncoderInputModeOption("单路", API.ENCODER_INPUT_MODE.A),
            new EncoderInputModeOption("正交", API.ENCODER_INPUT_MODE.AB)
        };
        public List<EncoderInputModeOption> EncoderInputModeOptions
        {
            get { return _encoderInputModeOptions; }
            set { _encoderInputModeOptions = value; RaisePropertyChanged(); }
        }

        private List<EncoderOutputModeOption> _encoderOutputModeOptions = new List<EncoderOutputModeOption>
        {
            new EncoderOutputModeOption("X1", API.ENCODER_OUTPUT_MODE.X1),
            new EncoderOutputModeOption("X2", API.ENCODER_OUTPUT_MODE.X2),
            new EncoderOutputModeOption("X4", API.ENCODER_OUTPUT_MODE.X4)
        };
        public List<EncoderOutputModeOption> EncoderOutputModeOptions
        {
            get { return _encoderOutputModeOptions; }
            set { _encoderOutputModeOptions = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<RefractiveIndexTableItem> _refractiveIndexTableItems = new ObservableCollection<RefractiveIndexTableItem>();
        public ObservableCollection<RefractiveIndexTableItem> RefractiveIndexTableItems
        {
            get { return _refractiveIndexTableItems; }
            set { _refractiveIndexTableItems = value; RaisePropertyChanged(); }
        }

        private bool _isRefractiveTableEditMode = true;
        public bool IsRefractiveTableEditMode
        {
            get { return _isRefractiveTableEditMode; }
            set
            {
                if (_isRefractiveTableEditMode == value)
                {
                    return;
                }

                _isRefractiveTableEditMode = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsRefractiveTableSelectMode));
            }
        }

        public bool IsRefractiveTableSelectMode
        {
            get { return !IsRefractiveTableEditMode; }
            set
            {
                if (value == !IsRefractiveTableEditMode)
                {
                    return;
                }

                IsRefractiveTableEditMode = !value;
            }
        }

        private string _refractiveIndexTableFilePath = string.Empty;
        public string RefractiveIndexTableFilePath
        {
            get { return _refractiveIndexTableFilePath; }
            set { _refractiveIndexTableFilePath = value ?? string.Empty; RaisePropertyChanged(); }
        }

        private int _selectedRefractiveProbeChannel = 1;
        public int SelectedRefractiveProbeChannel
        {
            get { return _selectedRefractiveProbeChannel; }
            set { _selectedRefractiveProbeChannel = value; RaisePropertyChanged(); }
        }

        private int _selectedRefractiveLayer = 1;
        public int SelectedRefractiveLayer
        {
            get { return _selectedRefractiveLayer; }
            set { _selectedRefractiveLayer = value; RaisePropertyChanged(); }
        }

        private List<ProbeChannelOption> _refractiveProbeChannelOptions = new List<ProbeChannelOption>
        {
            new ProbeChannelOption("CH1", 1)
        };
        public List<ProbeChannelOption> RefractiveProbeChannelOptions
        {
            get { return _refractiveProbeChannelOptions; }
            set { _refractiveProbeChannelOptions = value; RaisePropertyChanged(); }
        }

        private List<LayerNumberOption> _refractiveLayerOptions = new List<LayerNumberOption>
        {
            new LayerNumberOption("1", 1),
            new LayerNumberOption("2", 2),
            new LayerNumberOption("3", 3),
            new LayerNumberOption("4", 4),
            new LayerNumberOption("5", 5)
        };
        public List<LayerNumberOption> RefractiveLayerOptions
        {
            get { return _refractiveLayerOptions; }
            set { _refractiveLayerOptions = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public TronSight2SensorViewModel()
        {

        }
        #endregion

        #region Methods
        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            if (Param != null && (Param is TronSight2Sensor))
                ModelParam.TronSight2Sensor = Param as TronSight2Sensor ?? new TronSight2Sensor();
            else
                ModelParam.TronSight2Sensor = new TronSight2Sensor();

            InitializeRefractiveIndexTable();

            //打开页面时刷新加载参数
            if (ModelParam.TronSight2Sensor != null && ModelParam.TronSight2Sensor.IsConnected)
            {
                ModelParam.TronSight2Sensor.LoadConfigurationFromSensor();
            }
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
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;

                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.TronSight2Sensor },
                    });
                    break;

                default:
                    break;
            }
        });

        public DelegateCommand<string> ConfigCommand => new DelegateCommand<string>((order) =>
        {
            if (ModelParam.TronSight2Sensor == null)
            {
                return;
            }

            switch (order)
            {
                case "写入配置到传感器":
                    ModelParam.TronSight2Sensor.SaveConfigurationToSensor();
                    break;

                case "从传感器读取配置":
                    ModelParam.TronSight2Sensor.LoadConfigurationFromSensor();
                    break;

                case "触发复位":
                    ModelParam.TronSight2Sensor.ResetTriggerCounter();
                    break;

                default:
                    break;
            }
        });

        public DelegateCommand<string> DeviceConfigCommand => new DelegateCommand<string>((order) =>
        {
            if (ModelParam.TronSight2Sensor == null)
            {
                return;
            }

            switch (order)
            {
                case "恢复出厂配置":
                    ModelParam.TronSight2Sensor.RestoreFactoryConfiguration();
                    break;

                case "保存配置到文件":
                    ShowSaveConfigDialog();
                    break;

                case "从文件读取配置":
                    ShowOpenConfigDialog();
                    break;

                default:
                    break;
            }
        });

        public DelegateCommand<string> RefractiveTableCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "选择路径":
                    ShowSelectRefractiveTablePathDialog();
                    break;

                case "清空":
                    ClearRefractiveIndexTable();
                    break;

                case "由硬盘读取":
                    LoadRefractiveIndexTableFromDisk();
                    break;

                case "下载到硬盘":
                    TronSight2Sensor.SaveRefractiveIndexTableToFile(RefractiveIndexTableFilePath, RefractiveIndexTableItems);
                    break;

                case "由控制器读取":
                    ModelParam.TronSight2Sensor?.LoadRefractiveIndexTableFromController(RefractiveIndexTableItems);
                    EnsureSingleRefractiveSelection();
                    break;

                case "上传到控制器":
                    ModelParam.TronSight2Sensor?.UploadRefractiveIndexTableToController(RefractiveIndexTableItems);
                    break;

                case "使用选中折射率":
                    ApplySelectedRefractiveIndex();
                    break;

                case "折射率标定":
                    Logs.LogInfo("TronSight2: 折射率标定按钮已点击，当前版本暂不实现标定功能");
                    break;

                default:
                    break;
            }
        });

        #endregion

        private void ShowSaveConfigDialog()
        {
            var sensor = ModelParam.TronSight2Sensor;
            var dialog = new SaveFileDialog
            {
                Title = "保存配置",
                Filter = "配置文件 (*.dat)|*.dat",
                DefaultExt = "dat",
                AddExtension = true,
                FileName = sensor.GetSuggestedConfigFileName()
            };

            if (dialog.ShowDialog() == true)
            {
                sensor.SaveConfigurationToFile(dialog.FileName);
            }
        }

        private void ShowOpenConfigDialog()
        {
            var sensor = ModelParam.TronSight2Sensor;
            var dialog = new OpenFileDialog
            {
                Title = "打开配置",
                Filter = "配置文件 (*.dat)|*.dat",
                DefaultExt = "dat",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                sensor.LoadConfigurationFromFile(dialog.FileName);
            }
        }

        private void InitializeRefractiveIndexTable()
        {
            // 每次打开页面默认回到“折射率表修改”，避免上次界面状态把默认入口带偏。
            IsRefractiveTableEditMode = true;
            RefractiveIndexTableFilePath = GetDefaultRefractiveIndexTablePath();

            if (RefractiveIndexTableItems.Count == 0)
            {
                ReplaceRefractiveIndexTableItems(TronSight2Sensor.LoadRefractiveIndexTableFromFile(RefractiveIndexTableFilePath));
            }
            else
            {
                AttachRefractiveSelectionCallbacks();
                EnsureSingleRefractiveSelection();
            }
        }

        private void ShowSelectRefractiveTablePathDialog()
        {
            var dialog = new SaveFileDialog
            {
                Title = "选择折射率表路径",
                Filter = "文本文件 (*.txt)|*.txt",
                DefaultExt = "txt",
                AddExtension = true,
                FileName = Path.GetFileName(RefractiveIndexTableFilePath),
                InitialDirectory = GetInitialRefractiveDirectory()
            };

            if (dialog.ShowDialog() == true)
            {
                RefractiveIndexTableFilePath = dialog.FileName;
            }
        }

        private void LoadRefractiveIndexTableFromDisk()
        {
            ReplaceRefractiveIndexTableItems(TronSight2Sensor.LoadRefractiveIndexTableFromFile(RefractiveIndexTableFilePath));
        }

        private void ClearRefractiveIndexTable()
        {
            ReplaceRefractiveIndexTableItems(TronSight2Sensor.CreateEmptyRefractiveIndexTableItems());
        }

        private void ApplySelectedRefractiveIndex()
        {
            if (ModelParam.TronSight2Sensor == null)
            {
                return;
            }

            RefractiveIndexTableItem selectedItem = GetSelectedRefractiveItem();
            if (selectedItem == null)
            {
                Logs.LogWarning("TronSight2: 使用选中折射率失败，当前未选中任何折射率表项");
                return;
            }

            ModelParam.TronSight2Sensor.UseSelectedRefractiveIndex(SelectedRefractiveProbeChannel, SelectedRefractiveLayer, selectedItem.Label);
        }

        private RefractiveIndexTableItem GetSelectedRefractiveItem()
        {
            foreach (RefractiveIndexTableItem item in RefractiveIndexTableItems)
            {
                if (item.IsSelected)
                {
                    return item;
                }
            }

            return null;
        }

        private void ReplaceRefractiveIndexTableItems(IEnumerable<RefractiveIndexTableItem> items)
        {
            var collection = new ObservableCollection<RefractiveIndexTableItem>();
            foreach (RefractiveIndexTableItem item in items)
            {
                collection.Add(item);
            }

            RefractiveIndexTableItems = collection;
            AttachRefractiveSelectionCallbacks();
            EnsureSingleRefractiveSelection();
        }

        private void AttachRefractiveSelectionCallbacks()
        {
            foreach (RefractiveIndexTableItem item in RefractiveIndexTableItems)
            {
                item.SelectedCallback = OnRefractiveItemSelected;
            }
        }

        private void OnRefractiveItemSelected(RefractiveIndexTableItem selectedItem)
        {
            foreach (RefractiveIndexTableItem item in RefractiveIndexTableItems)
            {
                if (!ReferenceEquals(item, selectedItem) && item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
        }

        private void EnsureSingleRefractiveSelection()
        {
            RefractiveIndexTableItem selectedItem = null;
            foreach (RefractiveIndexTableItem item in RefractiveIndexTableItems)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                if (selectedItem == null)
                {
                    selectedItem = item;
                }
                else
                {
                    item.IsSelected = false;
                }
            }

            // 文件本身不带“当前选中项”状态时，默认选中第一条，避免选择模式下没有可用项。
            if (selectedItem == null && RefractiveIndexTableItems.Count > 0)
            {
                RefractiveIndexTableItems[0].IsSelected = true;
            }
        }

        private static string GetDefaultRefractiveIndexTablePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ts-data", "Ts-Infrared", "refractive_index_table.txt");
        }

        private string GetInitialRefractiveDirectory()
        {
            if (!string.IsNullOrWhiteSpace(RefractiveIndexTableFilePath))
            {
                string directory = Path.GetDirectoryName(RefractiveIndexTableFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return directory;
                }
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }

    public class MeasureModeOption
    {
        public string DisplayName { get; set; }

        public API.MEASUREMODE Value { get; set; }

        public MeasureModeOption(string displayName, API.MEASUREMODE value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class SamplingIntervalOption
    {
        public string DisplayName { get; set; }

        public API.SAMPLING_INTERVAL Value { get; set; }

        public SamplingIntervalOption(string displayName, API.SAMPLING_INTERVAL value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class FilterWindowWidthOption
    {
        public string DisplayName { get; set; }

        public API.FILTER_WINDOW_WIDTH Value { get; set; }

        public FilterWindowWidthOption(string displayName, API.FILTER_WINDOW_WIDTH value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class MedianFilterWidthOption
    {
        public string DisplayName { get; set; }

        public API.MEDIAN_FILTER_WIDTH Value { get; set; }

        public MedianFilterWidthOption(string displayName, API.MEDIAN_FILTER_WIDTH value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class TriggerSampleModeOption
    {
        public string DisplayName { get; set; }

        public TriggerSampleMode Value { get; set; }

        public TriggerSampleModeOption(string displayName, TriggerSampleMode value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }


    public class SyncValidLevelOption
    {
        public string DisplayName { get; set; }

        public API.SYNC_VALID_LEVEL Value { get; set; }

        public SyncValidLevelOption(string displayName, API.SYNC_VALID_LEVEL value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class SyncFilterWidthOption
    {
        public string DisplayName { get; set; }

        public API.SYNC_FILTER_WIDTH Value { get; set; }

        public SyncFilterWidthOption(string displayName, API.SYNC_FILTER_WIDTH value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class EncoderTriggerChannelOption
    {
        public string DisplayName { get; set; }

        public EncoderTriggerChannel Value { get; set; }

        public EncoderTriggerChannelOption(string displayName, EncoderTriggerChannel value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class TriggerModeOption
    {
        public string DisplayName { get; set; }

        public API.TRIG_MODE Value { get; set; }

        public TriggerModeOption(string displayName, API.TRIG_MODE value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class TriggerDirectionOption
    {
        public string DisplayName { get; set; }

        public API.TRIG_DIRECTION Value { get; set; }

        public TriggerDirectionOption(string displayName, API.TRIG_DIRECTION value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class TriggerTrackModeOption
    {
        public string DisplayName { get; set; }

        public API.TRIG_TRACK_MODE Value { get; set; }

        public TriggerTrackModeOption(string displayName, API.TRIG_TRACK_MODE value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class EncoderFilterWidthOption
    {
        public string DisplayName { get; set; }

        public API.ENCODER_FILTER_WIDTH Value { get; set; }

        public EncoderFilterWidthOption(string displayName, API.ENCODER_FILTER_WIDTH value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class EncoderInputModeOption
    {
        public string DisplayName { get; set; }

        public API.ENCODER_INPUT_MODE Value { get; set; }

        public EncoderInputModeOption(string displayName, API.ENCODER_INPUT_MODE value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class EncoderOutputModeOption
    {
        public string DisplayName { get; set; }

        public API.ENCODER_OUTPUT_MODE Value { get; set; }

        public EncoderOutputModeOption(string displayName, API.ENCODER_OUTPUT_MODE value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class ProbeChannelOption
    {
        public string DisplayName { get; set; }

        public int Value { get; set; }

        public ProbeChannelOption(string displayName, int value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }

    public class LayerNumberOption
    {
        public string DisplayName { get; set; }

        public int Value { get; set; }

        public LayerNumberOption(string displayName, int value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}

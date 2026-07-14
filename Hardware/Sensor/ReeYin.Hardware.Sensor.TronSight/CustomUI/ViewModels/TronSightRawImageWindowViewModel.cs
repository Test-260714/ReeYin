using Prism.Dialogs;
using ReeYin.Hardware.Sensor.TronSight.CustomUI.Models;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.ViewModels
{
    public class TronSightRawImageWindowViewModel : DialogViewModelBase
    {
        public const string RawImageConfigParameterName = "RawImageConfig";

        public ObservableCollection<OptionItem<int>> SensorChannelOptions { get; } = new ObservableCollection<OptionItem<int>>();
        public ObservableCollection<OptionItem<FRAME_DATA_SRC>> FrameDataSourceOptions { get; } = new ObservableCollection<OptionItem<FRAME_DATA_SRC>>();
        public ObservableCollection<OptionItem<PEAK_SELECTION_MODE>> PeakSelectionModeOptions { get; } = new ObservableCollection<OptionItem<PEAK_SELECTION_MODE>>();
        public ObservableCollection<OptionItem<PEAK_SORT_MODE>> PeakSortModeOptions { get; } = new ObservableCollection<OptionItem<PEAK_SORT_MODE>>();
        public ObservableCollection<OptionItem<IMAGE_FILTER_WIDTH>> ImageFilterWidthOptions { get; } = new ObservableCollection<OptionItem<IMAGE_FILTER_WIDTH>>();

        private TronSightSensor? _sensor;
        public TronSightSensor? Sensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value;
                RaisePropertyChanged();
                RefreshSensorChannelOptions();
            }
        }

        private TronSightRawImageConfigModel _rawImageConfig = new TronSightRawImageConfigModel();
        public TronSightRawImageConfigModel RawImageConfig
        {
            get { return _rawImageConfig; }
            set
            {
                if (ReferenceEquals(_rawImageConfig, value))
                {
                    return;
                }

                if (_rawImageConfig != null)
                {
                    _rawImageConfig.PropertyChanged -= RawImageConfig_PropertyChanged;
                }

                _rawImageConfig = value ?? new TronSightRawImageConfigModel();
                _rawImageConfig.PropertyChanged += RawImageConfig_PropertyChanged;
                RaisePropertyChanged();
                RaiseExposureStateProperties();
                RaisePropertyChanged(nameof(RefreshButtonText));
            }
        }

        public bool IsManualExposureEnabled => !RawImageConfig.IsAutoExposureEnabled;
        public bool IsAutoExposureSettingEnabled => RawImageConfig.IsAutoExposureEnabled;
        public string RefreshButtonText => RawImageConfig.IsAutoRefreshEnabled ? "停止刷新" : "刷新图像";

        public TronSightRawImageWindowViewModel()
        {
            _rawImageConfig.PropertyChanged += RawImageConfig_PropertyChanged;
            InitializeOptions();
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            if (parameters.TryGetValue<TronSightRawImageConfigModel>(RawImageConfigParameterName, out var config)
                && config != null)
            {
                RawImageConfig = config;
            }

            RefreshSensorChannelOptions();
        }

        public override void InitParam()
        {
            Sensor = Param as TronSightSensor;
        }

        public void RefreshSensorChannelOptions()
        {
            int sensorCount = 1;
            if (Sensor != null && Sensor.ControlerHandle != 0 && Sensor.IsConnected)
            {
                sensorCount = Math.Max(1, TSCMCAPICS.MaxSensorChannels(Sensor.ControlerHandle));
            }

            int selectedIndex = RawImageConfig.SensorChannelIndex;
            bool shouldRebuildOptions = SensorChannelOptions.Count != sensorCount;
            if (!shouldRebuildOptions)
            {
                for (int index = 0; index < SensorChannelOptions.Count; index++)
                {
                    if (SensorChannelOptions[index].Value != index)
                    {
                        shouldRebuildOptions = true;
                        break;
                    }
                }
            }

            if (shouldRebuildOptions)
            {
                SensorChannelOptions.Clear();
                for (int index = 0; index < sensorCount; index++)
                {
                    SensorChannelOptions.Add(new OptionItem<int>(index, $"通道 {index + 1}"));
                }
            }

            RawImageConfig.SensorChannelIndex = selectedIndex < 0 || selectedIndex >= sensorCount
                ? 0
                : selectedIndex;
        }

        private void InitializeOptions()
        {
            RefreshSensorChannelOptions();

            FrameDataSourceOptions.Add(new OptionItem<FRAME_DATA_SRC>(FRAME_DATA_SRC.ORIGIN, "原始图像"));
            FrameDataSourceOptions.Add(new OptionItem<FRAME_DATA_SRC>(FRAME_DATA_SRC.CALIB, "校准图像"));
            FrameDataSourceOptions.Add(new OptionItem<FRAME_DATA_SRC>(FRAME_DATA_SRC.SHARPNESS, "锐度图像"));

            PeakSelectionModeOptions.Add(new OptionItem<PEAK_SELECTION_MODE>(PEAK_SELECTION_MODE.MAX, "最大值"));
            PeakSelectionModeOptions.Add(new OptionItem<PEAK_SELECTION_MODE>(PEAK_SELECTION_MODE.NUMBER, "序号"));
            PeakSelectionModeOptions.Add(new OptionItem<PEAK_SELECTION_MODE>(PEAK_SELECTION_MODE.WINDOW, "窗口"));
            PeakSelectionModeOptions.Add(new OptionItem<PEAK_SELECTION_MODE>(PEAK_SELECTION_MODE.LAST, "最后一个峰"));

            PeakSortModeOptions.Add(new OptionItem<PEAK_SORT_MODE>(PEAK_SORT_MODE.LEFT_AND_RIGHT, "左右排序"));
            PeakSortModeOptions.Add(new OptionItem<PEAK_SORT_MODE>(PEAK_SORT_MODE.HEIGHT, "高度排序"));

            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._1, "1"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._2, "2"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._3, "3"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._5, "5"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._7, "7"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._11, "11"));
            ImageFilterWidthOptions.Add(new OptionItem<IMAGE_FILTER_WIDTH>(IMAGE_FILTER_WIDTH._15, "15"));
        }

        private void RawImageConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TronSightRawImageConfigModel.IsAutoExposureEnabled))
            {
                RaiseExposureStateProperties();
            }

            if (e.PropertyName == nameof(TronSightRawImageConfigModel.IsAutoRefreshEnabled))
            {
                RaisePropertyChanged(nameof(RefreshButtonText));
            }
        }

        private void RaiseExposureStateProperties()
        {
            RaisePropertyChanged(nameof(IsManualExposureEnabled));
            RaisePropertyChanged(nameof(IsAutoExposureSettingEnabled));
        }
    }

    public class OptionItem<T>
    {
        public OptionItem(T value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public T Value { get; }
        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

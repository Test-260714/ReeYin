using HalconDotNet;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Custom.PlcImageCollect.Models
{
    [Serializable]
    public class PlcImageCollectModel : ModelParamBase
    {
        [JsonIgnore]
        private ObservableCollection<SensorBase> _models = new ObservableCollection<SensorBase>();

        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SensorBase? _sltModel;

        [JsonIgnore]
        public SensorBase? SltModel
        {
            get { return _sltModel; }
            set { _sltModel = value; RaisePropertyChanged(); }
        }

        private string _sltModelName = string.Empty;

        public string SltModelName
        {
            get { return _sltModelName; }
            set { _sltModelName = value; RaisePropertyChanged(); }
        }


        private int _sltTriggerPicIndex = 0;

        public int SltTriggerPicIndex
        {
            get { return _sltTriggerPicIndex; }
            set { _sltTriggerPicIndex = value; RaisePropertyChanged(); }
        }
        private double _pixelEquivalentX = 2.9;

        public double PixelEquivalentX
        {
            get { return _pixelEquivalentX; }
            set { _pixelEquivalentX = value; RaisePropertyChanged(); }
        }

        private double _pixelEquivalentY = 5.0;

        public double PixelEquivalentY
        {
            get { return _pixelEquivalentY; }
            set { _pixelEquivalentY = value; RaisePropertyChanged(); }
        }

        private double _pixelEquivalentZ = 0.1;

        public double PixelEquivalentZ
        {
            get { return _pixelEquivalentZ; }
            set { _pixelEquivalentZ = value; RaisePropertyChanged(); }
        }

        private bool _startCollect = true;

        public bool StartCollect
        {
            get { return _startCollect; }
            set { _startCollect = value; RaisePropertyChanged(); }
        }

        private bool _stopCollect;

        public bool StopCollect
        {
            get { return _stopCollect; }
            set { _stopCollect = value; RaisePropertyChanged(); }
        }

        private bool _isSaveImage = true;

        public bool IsSaveImage
        {
            get { return _isSaveImage; }
            set { _isSaveImage = value; RaisePropertyChanged(); }
        }

        private string _savePath = @"D:\PlcImageCollect";

        public string SavePath
        {
            get { return _savePath; }
            set { _savePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _collectStartTime = string.Empty;

        [JsonIgnore]
        private readonly object _collectSyncRoot = new object();

        [JsonIgnore]
        private PLCLineScanSegmentInfo? _currentLineScanSegment;

        [JsonIgnore]
        private PLCLineScanSegmentInfo? _collectLineScanSegment;

        private string _lastSaveDirectory = string.Empty;

        [OutputParam("LastSaveDirectory", "最近一次存图目录")]
        public string LastSaveDirectory
        {
            get { return _lastSaveDirectory; }
            set { _lastSaveDirectory = value; RaisePropertyChanged(); }
        }

        private int _lastDataCount;

        [OutputParam("LastDataCount", "最近一次采集数据行数")]
        public int LastDataCount
        {
            get { return _lastDataCount; }
            set { _lastDataCount = value; RaisePropertyChanged(); }
        }

        public PlcImageCollectModel()
        {
            RefreshSensorModels();
        }

        public override bool LoadKeyParam()
        {
            if (!base.LoadKeyParam())
            {
                return false;
            }

            RefreshSensorModels();
            ResolveSelectedSensor();
            return true;
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);
                PrismProvider.EventAggregator.GetEvent<PLCLineScanSegmentEvent>().Subscribe(OnLineScanSegment, ThreadOption.PublisherThread);

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () => ExecuteModule().Result;
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError($"PLC触发采集模块初始化失败：{ex.Message}");
                return false;
            }
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            NodeStatus status = NodeStatus.Success;

            try
            {
                LoadKeyParam();

                if (StartCollect)
                {
                    TrrigerStartCollect("TrrigerStartCollect");
                }
                else if (StopCollect)
                {
                    TrrigerStopCollect("TrrigerStopCollect");
                }

                RefreshOutputParams();
                UpdateParam();
            }
            catch (Exception ex)
            {
                status = NodeStatus.Error;
                Logs.LogError($"PLC触发采集模块执行失败：{ex.Message}");
            }

            stopwatch.Stop();
            Output = new ExecuteModuleOutput
            {
                RunStatus = status,
                RunTime = stopwatch.ElapsedMilliseconds,
            };
            return await Task.FromResult(Output);
        }

        [EventSubscription(typeof(UpdateMessageEvent), "触发开始采集", ThreadOption.PublisherThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != "TrrigerStartCollect") return;

            lock (_collectSyncRoot)
            {
                LoadKeyParam();
                if (SltModel == null)
                {
                    Logs.LogWarning("PLC触发采集：未选择传感器，无法开始采集。");
                    return;
                }

                _collectStartTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                _collectLineScanSegment = CloneLineScanSegmentInfo(_currentLineScanSegment);
                Logs.LogInfo($"PLC触发采集：{SltModel.NickName} 开始采集，批次={_collectStartTime}，行={_collectLineScanSegment?.RowIndex}");
                SltModel.StartCollect();
            }
        }

        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集", ThreadOption.PublisherThread)]
        public void TrrigerStopCollect(string order)
        {
            if (order != "TrrigerStopCollect") return;

            List<List<float[]>> channels = new List<List<float[]>>();
            string rootPath = string.Empty;
            bool shouldSaveImage = false;

            lock (_collectSyncRoot)
            {
                LoadKeyParam();
                if (SltModel == null)
                {
                    Logs.LogWarning("PLC触发采集：未选择传感器，无法停止采集。");
                    return;
                }

                Logs.LogInfo($"PLC触发采集：{SltModel.NickName} 停止采集，批次={_collectStartTime}");
                SltModel.StopCollect();
                List<MeasureData> data = SltModel.ReceiveSensorData();
                LastDataCount = data?.Count ?? 0;
                Logs.LogInfo($"PLC触发采集：读取到 {LastDataCount} 行传感器数据，批次={_collectStartTime}");

                if (IsSaveImage)
                {
                    channels = ConvertToChannels(data ?? new List<MeasureData>());
                    if (NeedMirrorLineScanRows(_collectLineScanSegment))
                    {
                        MirrorScanRows(channels);
                        Logs.LogInfo($"PLC触发采集：第{_collectLineScanSegment?.RowIndex}行单数行，保存前已按扫描方向镜像。");
                    }
                    else if (_collectLineScanSegment != null)
                    {
                        Logs.LogInfo($"PLC触发采集：第{_collectLineScanSegment.RowIndex}行非镜像保存。");
                    }

                    string batch = string.IsNullOrWhiteSpace(_collectStartTime) ? DateTime.Now.ToString("yyyyMMddHHmmssfff") : _collectStartTime;
                    rootPath = Path.Combine(SavePath, batch);
                    LastSaveDirectory = rootPath;
                    _collectStartTime = string.Empty;
                    _collectLineScanSegment = null;
                    shouldSaveImage = true;
                }

                RefreshOutputParams();
                UpdateParam();
            }

            if (shouldSaveImage)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        SaveSensorImages(channels, rootPath);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"PLC触发采集：后台存图失败，{ex.Message}");
                    }
                });
                Logs.LogInfo($"PLC触发采集：已加入后台存图队列 {rootPath}");
            }
        }

        private void RefreshSensorModels()
        {
            var param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
            Models = param.Models ?? new ObservableCollection<SensorBase>();
        }

        private void ResolveSelectedSensor()
        {
            if (SltModel != null)
            {
                SltModelName = SltModel.NickName;
                return;
            }

            if (!string.IsNullOrWhiteSpace(SltModelName))
            {
                SltModel = Models.FirstOrDefault(item => item.NickName == SltModelName);
            }
        }

        private void OnLineScanSegment(PLCLineScanSegmentInfo segmentInfo)
        {
            lock (_collectSyncRoot)
            {
                _currentLineScanSegment = CloneLineScanSegmentInfo(segmentInfo);
            }
        }

        private static bool NeedMirrorLineScanRows(PLCLineScanSegmentInfo? segmentInfo)
        {
            return segmentInfo != null && segmentInfo.RowIndex % 2 == 1;
        }

        private static void MirrorScanRows(List<List<float[]>> channels)
        {
            foreach (var channel in channels)
            {
                channel.Reverse();
            }
        }

        private static PLCLineScanSegmentInfo? CloneLineScanSegmentInfo(PLCLineScanSegmentInfo? source)
        {
            if (source == null)
            {
                return null;
            }

            return new PLCLineScanSegmentInfo
            {
                RowIndex = source.RowIndex,
                Stage = source.Stage,
                SourceEventName = source.SourceEventName,
                StartX = source.StartX,
                StartY = source.StartY,
                StartZ = source.StartZ,
                EndX = source.EndX,
                EndY = source.EndY,
                EndZ = source.EndZ,
                Direction = source.Direction,
                SourceSerial = source.SourceSerial,
                OrderDescribe = source.OrderDescribe,
                EventTime = source.EventTime,
                PositionReadSuccess = source.PositionReadSuccess,
            };
        }

        private void SaveSensorImages(List<List<float[]>> channels, string rootPath)
        {
            if (channels.Count < 2)
            {
                Logs.LogWarning("PLC触发采集：传感器数据不足灰度和高度两路，跳过存图。");
                return;
            }

            Directory.CreateDirectory(rootPath);
            string depthDir = Path.Combine(rootPath, "depth");
            string grayDir = Path.Combine(rootPath, "gray");
            Directory.CreateDirectory(depthDir);
            Directory.CreateDirectory(grayDir);

            Common_Algorithm converter = new Common_Algorithm();
            string heightFilePath = Path.Combine(depthDir, "1_tif_0.tif");
            string grayFilePath = Path.Combine(grayDir, "1_tif.tif");
            HObject heightImg = new HObject();
            HObject grayImg = new HObject();
            HObject heightRotateImg = new HObject();
            HObject grayRotateImg = new HObject();
            try
            {
                converter.ConvertListToHObject(channels[0], ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out heightImg);
                converter.ConvertListToHObject(channels[1], ReeYin_V.Core.Helper.ImageOP.ImageType.Gray, out grayImg);
                HOperatorSet.RotateImage(heightImg, out heightRotateImg, 270, "constant");
                HOperatorSet.RotateImage(grayImg, out grayRotateImg, 270, "constant");
                HOperatorSet.WriteImage(heightRotateImg, "tiff", 0, heightFilePath);
                HOperatorSet.WriteImage(grayRotateImg, "tiff", 0, grayFilePath);
                Logs.LogInfo($"PLC触发采集：高度原图已顺时针旋转90度保存 {heightFilePath}");
                Logs.LogInfo($"PLC触发采集：灰度原图已顺时针旋转90度保存 {grayFilePath}");
            }
            finally
            {
                heightImg.Dispose();
                grayImg.Dispose();
                heightRotateImg.Dispose();
                grayRotateImg.Dispose();
            }
        }

        private static List<List<float[]>> ConvertToChannels(List<MeasureData> sensorData)
        {
            List<List<float[]>> channels = new List<List<float[]>>();
            foreach (MeasureData rowData in sensorData)
            {
                if (rowData?.AreaData == null)
                {
                    continue;
                }

                for (int i = 0; i < rowData.AreaData.Count; i++)
                {
                    while (channels.Count <= i)
                    {
                        channels.Add(new List<float[]>());
                    }

                    if (rowData.AreaData[i] != null)
                    {
                        channels[i].Add((float[])rowData.AreaData[i].Clone());
                    }
                }
            }

            return channels;
        }

        private void RefreshOutputParams()
        {
            Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (values.TryGetValue(item.ParamName, out object? value))
                {
                    item.Value = value;
                }
            }
        }
    }
}

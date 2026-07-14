using Custom.WaferFlatnessMeasure;
using Newtonsoft.Json;
using Prism.Events;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Custom.WaferFlatnessMeasure.Models
{
    public sealed record LineSpectrumMeasureRows(List<float[]> HeightRows, List<float[]> GrayRows);

    [Serializable]
    public class LineSpectrumDataCollectionModel : ModelParamBase
    {
        private ObservableCollection<SensorBase> _models = new ObservableCollection<SensorBase>();
        private SensorBase? _sltModel;
        private int _sltTriggerPicIndex;
        private bool _startCollect;
        private bool _stopCollect;
        private string _startEventName = "TrrigerLineSpectrumStartCollect";
        private string _stopEventName = "TrrigerLineSpectrumStopCollect";
        private double _heightScale = 10d;
        private string _tiffOutputDirectory = string.Empty;
        private string _lastTiffExportDirectory = string.Empty;
        private List<float[]> _heightRows = new List<float[]>();
        private List<float[]> _grayRows = new List<float[]>();
        private int _heightRowCount;
        private int _grayRowCount;
        private string _lastCollectSummary = "暂无线光谱数据";

        [JsonIgnore]
        private readonly object _linePositionLock = new object();

        [JsonIgnore]
        private readonly Dictionary<int, LineSegmentStartPositionInfo> _linePositionsBySegment =
            new Dictionary<int, LineSegmentStartPositionInfo>();

        [JsonIgnore]
        private int _nextTiffBatchIndex;

        public string SltModelName { get; set; } = string.Empty;

        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get => _models;
            set
            {
                _models = value ?? new ObservableCollection<SensorBase>();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public SensorBase? SltModel
        {
            get => _sltModel;
            set
            {
                _sltModel = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 0: sensor collection command mode.
        /// </summary>
        public int SltTriggerPicIndex
        {
            get => _sltTriggerPicIndex;
            set
            {
                _sltTriggerPicIndex = value;
                RaisePropertyChanged();
            }
        }

        public bool StartCollect
        {
            get => _startCollect;
            set
            {
                _startCollect = value;
                RaisePropertyChanged();
            }
        }

        public bool StopCollect
        {
            get => _stopCollect;
            set
            {
                _stopCollect = value;
                RaisePropertyChanged();
            }
        }

        public string StartEventName
        {
            get => _startEventName;
            set
            {
                _startEventName = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        public string StopEventName
        {
            get => _stopEventName;
            set
            {
                _stopEventName = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        public double HeightScale
        {
            get => _heightScale;
            set
            {
                _heightScale = double.IsFinite(value) && value > 0 ? value : 1d;
                RaisePropertyChanged();
            }
        }

        public string TiffOutputDirectory
        {
            get => _tiffOutputDirectory;
            set
            {
                _tiffOutputDirectory = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        [OutputParam("LineSpectrumLastTiffExportDirectory", "Line spectrum last TIFF export directory")]
        [JsonIgnore]
        public string LastTiffExportDirectory
        {
            get => _lastTiffExportDirectory;
            set
            {
                _lastTiffExportDirectory = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        [OutputParam("LineSpectrumHeightRows", "线光谱高度数据")]
        [JsonIgnore]
        public List<float[]> HeightRows
        {
            get => _heightRows;
            set
            {
                _heightRows = value ?? new List<float[]>();
                HeightRowCount = _heightRows.Count;
                RaisePropertyChanged();
            }
        }

        [OutputParam("LineSpectrumGrayRows", "线光谱灰度数据")]
        [JsonIgnore]
        public List<float[]> GrayRows
        {
            get => _grayRows;
            set
            {
                _grayRows = value ?? new List<float[]>();
                GrayRowCount = _grayRows.Count;
                RaisePropertyChanged();
            }
        }

        [OutputParam("LineSpectrumHeightRowCount", "线光谱高度行数")]
        [JsonIgnore]
        public int HeightRowCount
        {
            get => _heightRowCount;
            set
            {
                _heightRowCount = Math.Max(0, value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastCollectSummary));
            }
        }

        [OutputParam("LineSpectrumGrayRowCount", "线光谱灰度行数")]
        [JsonIgnore]
        public int GrayRowCount
        {
            get => _grayRowCount;
            set
            {
                _grayRowCount = Math.Max(0, value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LastCollectSummary));
            }
        }

        [OutputParam("LineSpectrumLastCollectSummary", "线光谱最近采集摘要")]
        [JsonIgnore]
        public string LastCollectSummary
        {
            get => _lastCollectSummary;
            set
            {
                _lastCollectSummary = string.IsNullOrWhiteSpace(value) ? "暂无线光谱数据" : value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        public LineSpectrumDataCollectionModel()
        {
            RefreshSensorModels();
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            RefreshSensorModels();
        }

        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam())
                {
                    return false;
                }

                RefreshSensorModels();
                ResolveSelectedSensor();
                HeightScale = HeightScale;
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
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
                RefreshSensorModels();
                ResolveSelectedSensor();
                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer<NodeStatus>(() =>
            {
                try
                {
                    if (StartCollect)
                    {
                        TrrigerStartCollect(StartEventName);
                    }
                    else if (StopCollect)
                    {
                        TrrigerStopCollect(StopEventName);
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError(ex);
                    return NodeStatus.Error;
                }

                RefreshOutputParams();
                if (!UpdateParam())
                {
                    Logs.LogWarning($"模块_{Serial}更新线光谱输出参数失败");
                }

                return NodeStatus.Success;
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };
            return Task.FromResult(Output);
        }

        [EventSubscription(typeof(UpdateMessageEvent), "触发线光谱开始采集", ThreadOption.BackgroundThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != StartEventName)
            {
                return;
            }

            ResolveSelectedSensor();
            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的线光谱传感器模块，无法开始采集。");
                return;
            }

            Logs.LogInfo("线光谱传感器开始采集");
            SltModel.StartCollect();
        }

        [EventSubscription(typeof(UpdateMessageEvent), "触发线光谱停止采集", ThreadOption.BackgroundThread)]
        public void TrrigerStopCollect(string order)
        {
            if (order != StopEventName)
            {
                return;
            }

            ResolveSelectedSensor();
            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的线光谱传感器模块，无法停止采集。");
                return;
            }

            Logs.LogInfo("线光谱传感器停止采集");
            SltModel.StopCollect();
            List<MeasureData> sensorDatas = SltModel.ReceiveSensorData();
            LineSpectrumMeasureRows convertedRows = ConvertToMeasureRows(sensorDatas, HeightScale);
            ApplyMeasureRows(convertedRows);
            ExportTiffRows(convertedRows);
            RefreshOutputParams();
            UpdateParam();
        }

        [EventSubscription(typeof(OutputResultEvent), "记录线光谱线段坐标", ThreadOption.BackgroundThread)]
        public void ReceiveLineSegmentPosition((string, object) obj)
        {
            if (obj.Item1 == LineSegmentCsvSessionInfo.EventName)
            {
                ResetLinePositionSession();
                return;
            }

            if (obj.Item1 == "IsPoint" &&
                obj.Item2 is bool isPoint &&
                isPoint)
            {
                ResetLinePositionSession();
                return;
            }

            if (obj.Item1 != LineSegmentStartPositionInfo.EventName ||
                obj.Item2 is not LineSegmentStartPositionInfo linePosition ||
                linePosition.SegmentIndex <= 0)
            {
                return;
            }

            lock (_linePositionLock)
            {
                _linePositionsBySegment[linePosition.SegmentIndex] = linePosition.Clone();
            }
        }

        public static LineSpectrumMeasureRows ConvertToMeasureRows(
            IEnumerable<MeasureData>? measureDatas,
            double heightScale)
        {
            double safeHeightScale = double.IsFinite(heightScale) && heightScale > 0 ? heightScale : 1d;
            var heightRows = new List<float[]>();
            var grayRows = new List<float[]>();

            foreach (MeasureData data in measureDatas ?? Enumerable.Empty<MeasureData>())
            {
                if (data?.AreaData == null ||
                    data.AreaData.Count == 0 ||
                    data.AreaData[0] == null ||
                    data.AreaData[0].Length == 0)
                {
                    continue;
                }

                heightRows.Add(data.AreaData[0]
                    .Select(value => (float)(value * safeHeightScale))
                    .ToArray());

                if (data.AreaData.Count > 1 &&
                    data.AreaData[1] != null &&
                    data.AreaData[1].Length > 0)
                {
                    grayRows.Add(data.AreaData[1].ToArray());
                }
            }

            return new LineSpectrumMeasureRows(heightRows, grayRows);
        }

        private void ApplyMeasureRows(LineSpectrumMeasureRows rows)
        {
            HeightRows = rows.HeightRows;
            GrayRows = rows.GrayRows;
            LastCollectSummary = $"高度行数：{HeightRowCount}，灰度行数：{GrayRowCount}";
        }

        private void ExportTiffRows(LineSpectrumMeasureRows rows)
        {
            if (string.IsNullOrWhiteSpace(TiffOutputDirectory))
            {
                Logs.LogInfo("Line spectrum TIFF output directory is empty; TIFF export is skipped.");
                return;
            }

            int rowCount = Math.Max(rows.HeightRows?.Count ?? 0, rows.GrayRows?.Count ?? 0);
            if (rowCount == 0)
            {
                return;
            }

            try
            {
                int startBatchIndex;
                Dictionary<int, LineSegmentStartPositionInfo> linePositions;
                lock (_linePositionLock)
                {
                    startBatchIndex = _nextTiffBatchIndex;
                    _nextTiffBatchIndex++;
                    linePositions = _linePositionsBySegment.ToDictionary(
                        item => item.Key,
                        item => item.Value.Clone());
                }

                IReadOnlyList<LineSpectrumTiffExportPlanItem> exportItems =
                    LineSpectrumTiffExportService.BuildExportPlan(
                        TiffOutputDirectory,
                        rows,
                        linePositions,
                        startBatchIndex);
                int exportedCount = LineSpectrumTiffExportService.Export(exportItems);
                LastTiffExportDirectory = TiffOutputDirectory;
                Logs.LogInfo(
                    $"Line spectrum TIFF export finished. Directory: {TiffOutputDirectory}, file count: {exportedCount}.");
            }
            catch (Exception ex)
            {
                Logs.LogError($"Line spectrum TIFF export failed: {ex.Message}{Environment.NewLine}{ex}");
            }
        }

        private void ResetLinePositionSession()
        {
            lock (_linePositionLock)
            {
                _linePositionsBySegment.Clear();
                _nextTiffBatchIndex = 0;
            }
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

        private void RefreshSensorModels()
        {
            try
            {
                if (PrismProvider.HardwareModuleManager?.Modules != null &&
                    PrismProvider.HardwareModuleManager.Modules.TryGetValue(ConfigKey.SensorConfig, out var module) &&
                    module is SensorSetModel sensorSetModel)
                {
                    Models = sensorSetModel.Models ?? new ObservableCollection<SensorBase>();
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }

            Models ??= new ObservableCollection<SensorBase>();
        }

        private void ResolveSelectedSensor()
        {
            if (SltModel != null)
            {
                SltModelName = SltModel.NickName;
                return;
            }

            if (string.IsNullOrWhiteSpace(SltModelName) || Models == null)
            {
                return;
            }

            SltModel = Models.FirstOrDefault(model => model.NickName == SltModelName);
        }
    }
}

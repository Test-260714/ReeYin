using HslCommunication.Core.Net;
using Newtonsoft.Json;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.Models
{
    public class ChannelCalibrationRawPointModel : PreprocessDatasetModel
    {
        public double StandardValue { get; set; }
    }

    public sealed class ChannelCalibrationRawPointDisplay : ChannelCalibrationRawPointModel
    {
        public int Index { get; set; }
    }

    public partial class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        private FlatCalib_Algorithm _calibALGO;

        [JsonIgnore]
        private readonly List<ChannelCalibrationRawPointModel> _channelCalibrationRawPoints = new();

        [JsonIgnore]
        private int _channelCalibrationRawPointCount;

        [JsonIgnore]
        private List<ChannelCalibrationRawPointDisplay> _channelCalibrationRawPointDisplays = new();

        [JsonIgnore]
        private string _channelCalibrationFormulaDisplay =
            "厚度 = Abs(UpSurface - DownSurface)";

        private string _calibrationSourceFilePath = string.Empty;
        #endregion

        #region Properties
        [JsonIgnore]
        private bool _isUsingCalib;
        /// <summary>
        /// 是否启用标定
        /// </summary>
        public bool IsUsingCalib
        {
            get { return _isUsingCalib; }
            set { _isUsingCalib = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FlatCalib_MeasureParam _calibParam = new FlatCalib_MeasureParam();
        [JsonIgnore]
        public FlatCalib_MeasureParam CalibParam
        {
            get { return _calibParam; }
            set
            {
                _calibParam = value ?? new FlatCalib_MeasureParam();
                _calibALGO = new FlatCalib_Algorithm(_calibParam);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private string _rawPointCloudPlyPath = string.Empty;

        public string RawPointCloudPlyPath
        {
            get { return _rawPointCloudPlyPath; }
            set { _rawPointCloudPlyPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _residualPointCloudPlyPath = string.Empty;

        public string ResidualPointCloudPlyPath
        {
            get { return _residualPointCloudPlyPath; }
            set { _residualPointCloudPlyPath = value; RaisePropertyChanged(); }
        }

        public string CalibrationSourceFilePath
        {
            get { return _calibrationSourceFilePath; }
            set { _calibrationSourceFilePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _currentCalibrationStandardValue;
        /// <summary>
        /// 当前采集批次对应的标准值
        /// </summary>
        public double CurrentCalibrationStandardValue
        {
            get { return _currentCalibrationStandardValue; }
            set { _currentCalibrationStandardValue = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isChannelCalibrationCollecting;
        /// <summary>
        /// 是否正在进行通道标定采集
        /// </summary>
        public bool IsChannelCalibrationCollecting
        {
            get { return _isChannelCalibrationCollecting; }
            set { _isChannelCalibrationCollecting = value; RaisePropertyChanged(); }
        }

        private bool _isUsingChannelCalibration;
        /// <summary>
        /// 是否启用通道线性标定计算厚度
        /// </summary>
        public bool IsUsingChannelCalibration
        {
            get { return _isUsingChannelCalibration; }
            set
            {
                _isUsingChannelCalibration = value;
                RaisePropertyChanged();
                UpdateChannelCalibrationFormulaDisplay();
            }
        }

        [JsonIgnore]
        public int ChannelCalibrationRawPointCount
        {
            get { return _channelCalibrationRawPointCount; }
            set
            {
                _channelCalibrationRawPointCount = Math.Max(0, value);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public List<ChannelCalibrationRawPointDisplay> ChannelCalibrationRawPoints
        {
            get { return _channelCalibrationRawPointDisplays; }
            set
            {
                _channelCalibrationRawPointDisplays = value ?? new List<ChannelCalibrationRawPointDisplay>();
                ChannelCalibrationRawPointCount = _channelCalibrationRawPointDisplays.Count;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private double _channelCalibrationA;
        /// <summary>
        /// 通道标定系数 A，手动输入
        /// </summary>
        [OutputParam("ChannelCalibrationA", "通道线性标定系数A")]
        public double  ChannelCalibrationA
        {
            get { return _channelCalibrationA; }
            set
            {
                _channelCalibrationA = value;
                RaisePropertyChanged();
                UpdateChannelCalibrationFormulaDisplay();
            }
        }

        [JsonIgnore]
        private double _channelCalibrationB;
        /// <summary>
        /// 通道标定系数 B，手动输入
        /// </summary>
        [OutputParam("ChannelCalibrationB", "通道线性标定系数B")]
        public double ChannelCalibrationB
        {
            get { return _channelCalibrationB; }
            set
            {
                _channelCalibrationB = value;
                RaisePropertyChanged();
                UpdateChannelCalibrationFormulaDisplay();
            }
        }

        [JsonIgnore]
        private double _channelCalibrationC;
        /// <summary>
        /// 通道标定系数 C，基于手动输入的 A/B 和采集数据计算
        /// </summary>
        [OutputParam("ChannelCalibrationC", "通道线性标定系数C")]
        public double ChannelCalibrationC
        {
            get { return _channelCalibrationC; }
            set
            {
                _channelCalibrationC = value;
                RaisePropertyChanged();
                UpdateChannelCalibrationFormulaDisplay();
            }
        }

        [JsonIgnore]
        private double _channelCalibrationRmse;
        /// <summary>
        /// 通道标定拟合均方根误差
        /// </summary>
        [OutputParam("ChannelCalibrationRmse", "通道标定拟合RMSE")]
        public double ChannelCalibrationRmse
        {
            get { return _channelCalibrationRmse; }
            set { _channelCalibrationRmse = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public string ChannelCalibrationFormulaDisplay
        {
            get { return _channelCalibrationFormulaDisplay; }
            set
            {
                _channelCalibrationFormulaDisplay = value ?? string.Empty;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region ExternalMethods
        /// <summary>
        /// 触发重置
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发标定", ThreadOption.BackgroundThread)]
        public void Calib(string order)
        {
            if (order != "Calib") return;

            if (SltModel == null)
            {
                Logs.LogWarning("未找到可用的传感器模块，无法停止采集。");
                return;
            }

            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：触发传感器停止采集");
            SltModel.StopCollect();
            //移动结束后停止编码器触发
            SltModel.SettingParam("Encoder1_Enable", false);
            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：开始获取数据...");

            var sensorDatas = SltModel.ReceiveSensorData();
            List<MeasureData> DataCollect = new List<MeasureData>();
            if (IsPoint)
            {
                DataCollect = ProcessCollectedData(sensorDatas);
            }
            else
            {
                DataCollect = AlignCollectedDataWithPositions(sensorDatas);
            }


            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：结束获取数据...");

            if (!TryCreateCalibrationModel(
                    BuildPreprocessDatas(DataCollect),
                    "传感器采集标定",
                    savePreDatasToCsv: true,
                    out string message))
            {
                Logs.LogWarning(message);
            }
        }
        #endregion

        #region Methods
        public void LoadCalibModel()
        {
            var result = _calibALGO.LoadCalibrationModel(RawPointCloudPlyPath, ResidualPointCloudPlyPath);
            if (result != 0)
                Logs.LogError("算法标定结果无效");
        }

        public bool CalibFromFile(out string message)
        {
            message = string.Empty;

            try
            {
                string calibrationFilePath = ResolveCalibrationSourceFilePath();
                Logs.LogInfo($"{DateTime.Now:HH-mm-ss}：开始从文件读取标定数据：{calibrationFilePath}");

                List<PreprocessDatasetModel> filePreDatas = PreprocessDatasetModel.LoadFromCsv(calibrationFilePath);
                if (!TryCreateCalibrationModel(
                        filePreDatas,
                        $"文件标定[{Path.GetFileName(calibrationFilePath)}]",
                        savePreDatasToCsv: false,
                        out message))
                {
                    return false;
                }

                CalibrationSourceFilePath = calibrationFilePath;
                return true;
            }
            catch (Exception ex)
            {
                message = $"从文件执行标定失败：{ex.Message}";
                Logs.LogError($"{message}{Environment.NewLine}{ex}");
                return false;
            }
        }

        public bool StartChannelCalibrationCollect(out string message)
        {
            message = string.Empty;
            if (!TryResolveCalibrationSensor(out message))
            {
                return false;
            }

            try
            {
                SltModel.StopCollect();
                Logs.LogInfo($"{DateTime.Now:HH-mm-ss}：通道标定开始采集，{SurfaceChannelSummary}。");
                SltModel.StartCollect();
                IsChannelCalibrationCollecting = true;
                message = "已开始采集通道标定数据。";
                return true;
            }
            catch (Exception ex)
            {
                IsChannelCalibrationCollecting = false;
                message = $"开始通道标定采集失败：{ex.Message}";
                Logs.LogError($"{message}{Environment.NewLine}{ex}");
                return false;
            }
        }

        public bool StopChannelCalibrationCollect(out string message)
        {
            message = string.Empty;
            if (!TryResolveCalibrationSensor(out message))
            {
                IsChannelCalibrationCollecting = false;
                return false;
            }

            try
            {
                Logs.LogInfo($"{DateTime.Now:HH-mm-ss}：通道标定停止采集，{SurfaceChannelSummary}。");
                SltModel.StopCollect();
                List<MeasureData> sensorDatas = SltModel.ReceiveSensorData() ?? new List<MeasureData>();
                IsChannelCalibrationCollecting = false;

                if (sensorDatas.Count == 0)
                {
                    message = "未接收到可用于标定的传感器数据。";
                    Logs.LogWarning(message);
                    return false;
                }

                if (!IsFiniteDouble(CurrentCalibrationStandardValue))
                {
                    message = "当前标准值无效，无法写入标定原始数据。";
                    Logs.LogWarning(message);
                    return false;
                }

                List<PreprocessDatasetModel> validRawDatas = BuildPreprocessDatas(sensorDatas)
                    .Where(data => IsFiniteChannelValue(data.UpSurface) &&
                                   IsFiniteChannelValue(data.DownSurface))
                    .ToList();

                if (validRawDatas.Count == 0)
                {
                    message = "原始采集数据中不存在有效的 UpSurface/DownSurface 数据。";
                    Logs.LogWarning(message);
                    return false;
                }

                foreach (PreprocessDatasetModel data in validRawDatas)
                {
                    _channelCalibrationRawPoints.Add(new ChannelCalibrationRawPointModel
                    {
                        StandardValue = CurrentCalibrationStandardValue,
                        PosX = data.PosX,
                        PosY = data.PosY,
                        UpSurface = data.UpSurface,
                        DownSurface = data.DownSurface
                    });
                }

                RefreshChannelCalibrationRawPointDisplays();

                Logs.LogInfo(
                    $"通道标定采集完成，{SurfaceChannelSummary}，标准值={CurrentCalibrationStandardValue:F6}，本次追加 {validRawDatas.Count} 条原始点，累计 {_channelCalibrationRawPoints.Count} 条。");

                message = $"标定数据采集完成，已追加 {validRawDatas.Count} 条原始数据。";
                return true;
            }
            catch (Exception ex)
            {
                IsChannelCalibrationCollecting = false;
                message = $"停止通道标定采集失败：{ex.Message}";
                Logs.LogError($"{message}{Environment.NewLine}{ex}");
                return false;
            }
        }

        public void ClearChannelCalibrationRawPoints()
        {
            _channelCalibrationRawPoints.Clear();
            RefreshChannelCalibrationRawPointDisplays();
        }

        public bool SolveChannelCalibration(out string message)
        {
            message = string.Empty;
            var effectivePoints = _channelCalibrationRawPoints
                .Where(point => IsFiniteDouble(point.StandardValue) &&
                                IsFiniteDouble(point.UpSurface) &&
                                IsFiniteDouble(point.DownSurface))
                .ToList();

            if (effectivePoints.Count < 1)
            {
                message = "至少需要 1 条数值有效的原始数据才能计算 C。";
                Logs.LogWarning(message);
                return false;
            }

            if (!IsFiniteDouble(ChannelCalibrationA) || !IsFiniteDouble(ChannelCalibrationB))
            {
                message = "手动输入的 A 或 B 无效，无法计算 C。";
                Logs.LogWarning(message);
                return false;
            }

            ChannelCalibrationC = effectivePoints.Average(point =>
                point.StandardValue - ChannelCalibrationA * point.UpSurface - ChannelCalibrationB * point.DownSurface);

            ChannelCalibrationRmse = Math.Sqrt(
                effectivePoints.Average(point =>
                {
                    double predictedValue =
                        ChannelCalibrationA * point.UpSurface +
                        ChannelCalibrationB * point.DownSurface +
                        ChannelCalibrationC;
                    double residual = predictedValue - point.StandardValue;
                    return residual * residual;
                }));

            message = $"C 计算完成，共使用 {effectivePoints.Count} 条原始数据。";
            Logs.LogInfo(
                $"通道标定 C 计算完成，A={ChannelCalibrationA:F8}，B={ChannelCalibrationB:F8}，C={ChannelCalibrationC:F8}，RMSE={ChannelCalibrationRmse:F8}，原始点数={effectivePoints.Count}。");
            return true;
        }

        private bool TryResolveCalibrationSensor(out string message)
        {
            message = string.Empty;

            if (SltModel == null &&
                Models != null &&
                !string.IsNullOrWhiteSpace(SltModelName))
            {
                SltModel = Models.FirstOrDefault(model => model.NickName == SltModelName);
            }

            if (SltModel != null)
            {
                return true;
            }

            message = "未找到可用的传感器模块，无法执行标定采集。";
            Logs.LogWarning(message);
            return false;
        }

        private static bool IsFiniteChannelValue(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFiniteDouble(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private bool TryCreateCalibrationModel(
            IEnumerable<PreprocessDatasetModel>? calibrationPreDatas,
            string sourceDescription,
            bool savePreDatasToCsv,
            out string message)
        {
            message = string.Empty;
            _calibALGO ??= new FlatCalib_Algorithm(CalibParam);

            List<PreprocessDatasetModel> effectivePreDatas = PreprocessDatasetModel.Clone(calibrationPreDatas);
            if (effectivePreDatas.Count == 0)
            {
                message = $"{sourceDescription}未解析到有效的标定数据。";
                return false;
            }
            ValidCollect.Clear();
            UpdatePreDatas(effectivePreDatas);
            if (savePreDatasToCsv)
            {
                SavePreDatasToCsvIfNeeded();
            }

            if (ValidCollect == null || ValidCollect.Count < 3)
            {
                message = $"{sourceDescription}有效点数量不足，至少需要 3 个点才能执行标定。";
                return false;
            }
            int result = _calibALGO.CreateCalibrationModel(ValidCollect, RawPointCloudPlyPath, ResidualPointCloudPlyPath);
            RefreshOutputParamValues();

            if (result != 0)
            {
                message = $"{sourceDescription}执行标定失败。";
                return false;
            }

            message = $"{sourceDescription}执行完成，共使用 {ValidCollect.Count} 个有效点。";
            Logs.LogInfo(message);
            return true;
        }

        private void RefreshChannelCalibrationRawPointDisplays()
        {
            ChannelCalibrationRawPoints = _channelCalibrationRawPoints
                .Select((point, index) => new ChannelCalibrationRawPointDisplay
                {
                    Index = index + 1,
                    StandardValue = point.StandardValue,
                    PosX = point.PosX,
                    PosY = point.PosY,
                    UpSurface = point.UpSurface,
                    DownSurface = point.DownSurface
                })
                .ToList();
        }

        private void UpdateChannelCalibrationFormulaDisplay()
        {
            ChannelCalibrationFormulaDisplay = IsUsingChannelCalibration
                ? $"厚度 = {ChannelCalibrationA:F6} * UpSurface + {ChannelCalibrationB:F6} * DownSurface + {ChannelCalibrationC:F6}"
                : "厚度 = Abs(UpSurface - DownSurface)";
        }

        private string ResolveCalibrationSourceFilePath()
        {
            string calibrationFilePath = !string.IsNullOrWhiteSpace(CalibrationSourceFilePath)
                ? CalibrationSourceFilePath
                : FilePath;

            if (string.IsNullOrWhiteSpace(calibrationFilePath) || !File.Exists(calibrationFilePath))
            {
                throw new FileNotFoundException($"指定的标定数据文件不存在：{calibrationFilePath}", calibrationFilePath);
            }

            return calibrationFilePath;
        }
        #endregion
    }

}

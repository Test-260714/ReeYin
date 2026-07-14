using HalconDotNet;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI.Controls.Loading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.Semiconductor.Models
{
    [Serializable]
    public class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        public string SltModelName;

        //[JsonIgnore]
        //Dictionary<Guid,List<MeasureData>> MeasureDatas = new Dictionary<Guid,List<MeasureData>>();

        ProcessedData processedData = new ProcessedData();

        [JsonIgnore]
        public List<(double, double)> CollectPos = new List<(double, double)>();

        [JsonIgnore]
        public  List<double[]> ValidCollect = new List<double[]>();
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<SensorBase> _models;
        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath;
        /// <summary>
        /// 文件取图路径
        /// </summary>
        [OutputParam("FilePath", "文件取图路径")]
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _sltFile;
        /// <summary>
        /// 选中文件
        /// </summary>
        public string SltFile
        {
            get { return _sltFile; }
            set { _sltFile = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _linkPath;
        /// <summary>
        /// 链接路径
        /// </summary>
        public string LinkPath
        {
            get { return _linkPath; }
            set { _linkPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isLinkVisibility = Visibility.Hidden;
        /// <summary>
        /// 链接路径可见性
        /// </summary>
        public Visibility IsLinkVisibility
        {
            get { return _isLinkVisibility; }
            set { _isLinkVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前采集模式
        /// </summary>
        [JsonIgnore]
        private eTrigMode _AcquisitionMode = eTrigMode.软触发;
        public eTrigMode AcquisitionMode
        {
            get { return _AcquisitionMode; }
            set { _AcquisitionMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _sltTriggerPicIndex = 2;
        /// <summary>
        /// 选择取图方式（0：指定图像，1：文件获取，2：传感器采集 ）
        /// </summary>
        public int SltTriggerPicIndex
        {
            get { return _sltTriggerPicIndex; }
            set { _sltTriggerPicIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SensorBase _sltModel;
        /// <summary>
        /// 选中的
        /// </summary>
        [JsonIgnore]
        public SensorBase SltModel
        {
            get { return _sltModel; }
            set { _sltModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _camSltRotate = "Null";
        /// <summary>
        /// 选择相机旋转
        /// </summary>
        public string CamSltRotate
        {
            get { return _camSltRotate; }
            set { _camSltRotate = value; }
        }

        [JsonIgnore]
        private string _sltTriggerMode = "Null";
        /// <summary>
        /// 采集模式
        /// </summary>
        public string SltTriggerMode
        {
            get { return _sltTriggerMode; }
            set { _sltTriggerMode = value; }
        }

        [JsonIgnore]
        private bool _startCollect;
        /// <summary>
        /// 开始采集；
        /// </summary>
        public bool StartCollect
        {
            get { return _startCollect; }
            set { _startCollect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _stopCollect;
        /// <summary>
        /// 停止采集；
        /// </summary>
        public bool StopCollect
        {
            get { return _stopCollect; }
            set { _stopCollect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _stopAndDispose;
        /// <summary>
        /// 停止采集并处理数据；
        /// </summary>
        public bool StopAndDispose
        {
            get { return _stopAndDispose; }
            set { _stopAndDispose = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _startEventName = "TrrigerStartCollect";
        /// <summary>
        /// 开始采集事件名称
        /// </summary>
        public string StartEventName
        {
            get { return _startEventName; }
            set { _startEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _stopEventName = "TrrigerStopCollect";
        /// <summary>
        /// 停止采集事件名称
        /// </summary>
        public string StopEventName
        {
            get { return _stopEventName; }
            set { _stopEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _stopAndDisposeEventName = "TrrigerDispose";
        /// <summary>
        /// 停止采集并处理数据事件名称
        /// </summary>
        public string StopAndDisposeEventName
        {
            get { return _stopAndDisposeEventName; }
            set { _stopAndDisposeEventName = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }


        //[JsonIgnore]
        //private OtherConfigModel _otherConfig = new OtherConfigModel();
        ///// <summary>
        ///// 可以显示在页面的参数
        ///// </summary>
        //public OtherConfigModel OtherConfig
        //{
        //    get { return _otherConfig; }
        //    set { _otherConfig = value; RaisePropertyChanged(); }
        //}

        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
            //PrismProvider.ProjectManager.SltCurSolutionItem.NodeCaches

        }

        #endregion

        #region Override
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                base.LoadKeyParam();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
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

                Task.Run(() =>
                {
                    EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);

                });

                var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();

                Models = Param.Models;

                TriggerModuleRun += () =>
                {
                    return ExecuteModule().Result;
                };

                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((obj) =>
                {
                    if (obj.Item1 != "CollectPoints") return;
                    CollectPos = (List<(double, double)>)obj.Item2;

                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Subscribe((order) =>
                {
                    if (order != Serial.ToString()) return;
                    Dispose();
                }, ThreadOption.UIThread);

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 释放相关资源
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(Serial.ToString(), this);

            PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
            {
                Type = DynamicViewType.NodeMap,
                NodeSerial = Serial,
                DisplayName = $"{Serial.ToString("D3")}-传感器设置",
                ViewName = "OtherConfigView"
            });
        }
        #endregion

        #region Methods

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                #region 检查条件

                #endregion

                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        //指定图像
                        case 0:
                            if (File.Exists(FilePath))
                            {

                                break;
                            }
                            else
                            {
                                MessageBox.Show("指定图像不存在，请重新选择文件！");
                                FilePath = "";
                            }

                            break;
                        //文件取图
                        case 1:
                            {
                                //从文件中加载图片执行算法流程
                                var loading = new LoadingWindow()
                                {
                                    Topmost = true
                                };

                            }
                            break;
                        //传感器采集
                        case 2:
                            {
                                if (StartCollect)
                                {
                                    TrrigerStartCollect(StartEventName);
                                }
                                else if (StopCollect)
                                {
                                    TrrigerStopCollect(StopEventName);
                                }
                                else if (StopAndDispose)
                                {
                                    TrrigerDispose(StopAndDisposeEventName);
                                }
                            }
                            break;
                        //其他传感器采集
                        case 3:
                            {

                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;

                }

                #region 输出
                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                #endregion

                return NodeStatus.Success;
            });

            Logs.LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// 触发采集
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始采集", ThreadOption.BackgroundThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != StartEventName) return;

            if (SltModel == null && SltModelName != null && SltModelName != "")
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }
            SltModel.StopCollect();

            //开始执行有效移动前开启编码器触发
            SltModel.SettingParam("Encoder1_Enable", true);


            Logs.LogInfo("传感器开始采集");
            SltModel.StartCollect();

        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集", ThreadOption.BackgroundThread)]
        public void TrrigerStopCollect(string order)
        {
            if (order != StopEventName) return;

            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：触发传感器停止采集");
            SltModel.StopCollect();
            //移动结束后停止编码器触发
            SltModel.SettingParam("Encoder1_Enable", false);
            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：开始获取数据...");
            var DataCollect = AlignCollectedDataWithPositions(SltModel.ReceiveSensorData());
            Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：结束获取数据...");

            foreach (var data in DataCollect)
            {
                ValidCollect.Add([data.X, data.Y, GetMeasureChannelValue(data, 4)]);
            }

            //存到CSV
            ExportChannelsToCsv(DataCollect, FilePath + $"\\{DateTime.Now.ToString("yymmddHHmmss")}.csv"/*, ["通道1", "通道2", "通道3", "通道4"]*/);
            List<List<float[]>> allDates = ConvertToMeasurePara(DataCollect);

        }

        /// <summary>
        /// 触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始处理数据出图", ThreadOption.BackgroundThread)]
        public void TrrigerDispose(string order)
        {
            if (order != StopAndDisposeEventName) return;

            Logs.LogInfo("传感器停止采集并处理数据");
            SltModel.StopCollect();
            Logs.LogInfo("开始处理数据...");
            //if (OtherConfig.IsSaveImage)
            //{
            //    List<List<float[]>> allDates = ConvertToMeasurePara(SltModel.ReceiveSensorData());

            //    //通知数据更新
            //    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
            //}
            //else
            //{
            //    List<List<float[]>> allDates = ConvertToMeasurePara(SltModel.ReceiveSensorData());

            //    //通知数据更新
            //    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
            //}

        }

        /// <summary>
        /// 触发重置
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发重置采集数据", ThreadOption.BackgroundThread)]
        public void Reset(string order)
        {
            if (order != "Reset") return;

            Logs.LogInfo("开始停止采集并释放之前的资源");
            SltModel.StopCollect();

            Logs.LogInfo("完成...");
        }

        public List<List<float[]>> ConvertToMeasurePara(List<MeasureData> listMeasureData)
        {
            List<float[]> grayDataList = new List<float[]>();
            List<float[]> height1DataList = new List<float[]>();
            List<float[]> height2DataList = new List<float[]>();

            if (listMeasureData != null)
            {
                foreach (var measureData in listMeasureData)
                {
                    if (measureData.AreaData != null && measureData.AreaData.Count >= 3)
                    {
                        //float[] heightArray = measureData.AreaData[0];
                        float[] height1Array = measureData.AreaData[0].Select(x => x * 10).ToArray();
                        height1DataList.Add(height1Array);

                        float[] height2Array = measureData.AreaData[1].Select(x => x * 10).ToArray();
                        height2DataList.Add(height2Array);

                        float[] grayArray = measureData.AreaData[2];
                        grayDataList.Add(grayArray);
                    }
                }
            }

            return [grayDataList, height1DataList, height2DataList];
        }

        private List<MeasureData> AlignCollectedDataWithPositions(List<MeasureData>? dataCollect)
        {
            dataCollect ??= new List<MeasureData>();

            if (CollectPos == null || CollectPos.Count == 0)
            {
                if (dataCollect.Count > 0)
                {
                    Logs.LogWarning("轨迹坐标为空，已跳过采集数据坐标回填。");
                }

                return dataCollect;
            }

            if (dataCollect.Count > CollectPos.Count)
            {
                Logs.LogWarning($"采集数据数量 {dataCollect.Count} 多于轨迹点数量 {CollectPos.Count}，已截断多余数据。");
                dataCollect = dataCollect.Take(CollectPos.Count).ToList();
            }
            else if (dataCollect.Count < CollectPos.Count)
            {
                Logs.LogWarning($"采集数据数量 {dataCollect.Count} 少于轨迹点数量 {CollectPos.Count}，已补齐缺失数据。");
                while (dataCollect.Count < CollectPos.Count)
                {
                    dataCollect.Add(new MeasureData());
                }
            }

            for (int i = 0; i < dataCollect.Count; i++)
            {
                dataCollect[i].X = CollectPos[i].Item1;
                dataCollect[i].Y = CollectPos[i].Item2;
            }

            return dataCollect;
        }

        /// <summary>
        /// 存高度/灰度图
        /// </summary>
        public void SaveImage()
        {

        }

        /// <summary>
        /// 将 List<MeasureData> 中的 Channel1, Channel2, Channel3, Channel4 导出到 CSV 文件
        /// </summary>
        /// <param name="measureDataList">包含测量数据的列表</param>
        /// <param name="filePath">要写入的 CSV 文件路径</param>
        public static void ExportChannelsToCsv(List<MeasureData> measureDataList, string filePath)
        {
            if (measureDataList == null)
            {
                throw new ArgumentNullException(nameof(measureDataList), "输入的 MeasureData 列表不能为 null。");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空或仅包含空白字符。", nameof(filePath));
            }

            // 使用 StreamWriter 写入文件，指定 UTF-8 编码以支持中文等字符
            using (var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8))
            {
                writer.WriteLine("X,Y," + string.Join(",", Enumerable.Range(1, 18).Select(index => $"Channel{index}")));

                // 遍历列表，写入每一行数据
                foreach (var data in measureDataList)
                {
                    List<string> fields = new List<string>
                    {
                        data.X.ToString("G17", CultureInfo.InvariantCulture),
                        data.Y.ToString("G17", CultureInfo.InvariantCulture)
                    };

                    fields.AddRange(Enumerable.Range(1, 18).Select(index => EscapeCsvField(GetMeasureChannelText(data, index))));
                    writer.WriteLine(string.Join(",", fields));
                }
            }

            Console.WriteLine($"数据已成功导出到 CSV 文件: {filePath}");
        }

        private static double GetMeasureChannelValue(MeasureData? data, int channelIndex)
        {
            return TryGetMeasureChannelValue(data, channelIndex, out double value) ? value : double.NaN;
        }

        private static string GetMeasureChannelText(MeasureData? data, int channelIndex)
        {
            if (TryGetMeasureChannelValue(data, channelIndex, out double value))
            {
                return value.ToString("G17", CultureInfo.InvariantCulture);
            }

            if (TryGetOriginalDataValue(data, channelIndex, out object? originalValue))
            {
                return Convert.ToString(originalValue, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool TryGetMeasureChannelValue(MeasureData? data, int channelIndex, out double value)
        {
            value = 0d;

            if (data?.AreaData != null && channelIndex > 0 && channelIndex <= data.AreaData.Count)
            {
                float[] channelData = data.AreaData[channelIndex - 1];
                if (channelData != null && channelData.Length > 0)
                {
                    value = channelData[0];
                    return true;
                }
            }

            if (TryGetOriginalDataValue(data, channelIndex, out object? originalValue) &&
                TryConvertToDouble(originalValue, out value))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetOriginalDataValue(MeasureData? data, int channelIndex, out object? value)
        {
            value = null;

            if (data?.OriginalDatas == null || channelIndex <= 0)
            {
                return false;
            }

            string channelName = $"Channel{channelIndex}";
            if (!data.OriginalDatas.TryGetValue(channelName, out Dictionary<string, object>? channelValues) ||
                channelValues == null ||
                channelValues.Count == 0)
            {
                return false;
            }

            value = channelValues.Values.FirstOrDefault();
            return value != null;
        }

        private static bool TryConvertToDouble(object? value, out double number)
        {
            number = 0d;

            if (value == null)
            {
                return false;
            }

            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return !double.IsNaN(number) && !double.IsInfinity(number);
            }
            catch
            {
                return false;
            }
        }

        // 处理 CSV 特殊字符（如逗号、引号、换行符）
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // 如果字段包含逗号、引号或换行符，则用双引号包裹，并转义内部双引号
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        #endregion

    }
}

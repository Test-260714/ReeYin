using Dm;
using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Custom.MFDJC.Models
{
    [Serializable]
    public class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        public string SltModelName = string.Empty;

        [JsonIgnore]
        ElectroStaticChuck_Algorithm MFDCustomAlgo;
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<SensorBase> _models = new ObservableCollection<SensorBase>();
        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath = string.Empty;
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
        private string _sltFile = string.Empty;
        /// <summary>
        /// 选中文件
        /// </summary>
        public string SltFile
        {
            get { return _sltFile; }
            set { _sltFile = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _linkPath = string.Empty;
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

        [JsonIgnore]
        private Visibility _isSltVisibility = Visibility.Visible;
        /// <summary>
        /// 选中文件可见性
        /// </summary>
        public Visibility IsSltVisibility
        {
            get { return _isSltVisibility; }
            set { _isSltVisibility = value; RaisePropertyChanged(); }
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
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ElectroStaticChuck_MeasureParam _algorithm = new ElectroStaticChuck_MeasureParam();

        public ElectroStaticChuck_MeasureParam Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private OtherConfigModel _otherConfig = new OtherConfigModel();
        /// <summary>
        /// 可以显示在页面的参数
        /// </summary>
        public OtherConfigModel OtherConfig
        {
            get { return _otherConfig; }
            set { _otherConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _selectFlow;
        /// <summary>
        /// 流程参数
        /// </summary>
        [OutputParam("SelectFlow", "触发流程选择")]
        public int SelectFlow
        {
            get { return _selectFlow; }
            set { _selectFlow = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
        }

        ~SensorDataCollectionModel()
        {
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
                if (!base.LoadKeyParam())
                {
                    return false;
                }

                //等待加载完成赋值
                //SltModel = Models.SingleOrDefault(c => c.CameraNo == SltCamName);

                //if (PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Keys.Contains(Serial.ToString()) && PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()].Count != 0)
                //{
                //    ////OutputParams = PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()].DeepCopy();
                //    //var temp = new Dictionary<string, object>();
                //    //foreach (var item in PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache[Serial.ToString()])
                //    //{
                //    //    if (item.ParamName == "Image" && item.Value != null)
                //    //    {
                //    //        Image = ((HImage)item.Value).CopyImage();
                //    //    }
                //    //}
                //}

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
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
                var param = PrismProvider.HardwareModuleManager?.Modules[ConfigKey.SensorConfig] as SensorSetModel
                            ?? new SensorSetModel();
                MFDCustomAlgo = PrismProvider.Container.Resolve(typeof(ElectroStaticChuck_Algorithm)) as ElectroStaticChuck_Algorithm
                                ?? new ElectroStaticChuck_Algorithm();
                Models = param.Models ?? new ObservableCollection<SensorBase>();
                TriggerModuleRun = () => ExecuteModule().Result;

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-传感器设置",
                    ViewName = "OtherConfigView"
                });

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.Message);
                return false;
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            OnceInit();
        }
        #endregion

        #region Methods

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer<NodeStatus>(() =>
            {
                #region 检查条件

                #endregion

                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        //指定图像
                        case 0:
                            if (File.Exists(SltFile))
                            {
                                using Mat heightImage = Cv2.ImRead(SltFile, ImreadModes.Unchanged);

                                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ElectroStaticChuck_MeasureResult", new ImageResultsDisplay
                                {
                                    HeightImage = heightImage.Clone(),

                                }));
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
                                ElectroStaticChuckAlgorithmRunResult? lastRunResult = null;
                                try
                                {
                                    foreach (ElectroStaticChuckImageSet imageSet in ElectroStaticChuckImagePipeline.ResolveImageSets(FilePath))
                                    {
                                        lastRunResult?.Dispose();
                                        lastRunResult = ElectroStaticChuckImagePipeline.RunAlgorithm(MFDCustomAlgo, imageSet, Algorithm);
                                        Algorithm = lastRunResult.MeasureParam;
                                    }

                                    if (lastRunResult == null)
                                    {
                                        return NodeStatus.Error;
                                    }

                                    PublishAlgorithmResult(lastRunResult);
                                }
                                finally
                                {
                                    lastRunResult?.Dispose();
                                }
                            }
                            break;
                        //传感器采集
                        case 2:
                            {
                                if (StartCollect)
                                {
                                    TrrigerStartCollect("TrrigerStartCollect");
                                }
                                else if (StopCollect)
                                {
                                    TrrigerStopCollect("TrrigerStopCollect");
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
                RefreshOutputParams();

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                #endregion

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            Logs.LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        private void PublishAlgorithmResult(ElectroStaticChuckAlgorithmRunResult runResult)
        {
            ProcessedData processedData = new ProcessedData();
            processedData.SetMemoryPara("ElectroStaticChuck_MeasureResult", runResult.MeasureResult);

            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ElectroStaticChuck_MeasureResult", runResult.CreateImageResultsDisplay()));
        }

        private void PublishAlgorithmResult(ElectroStaticChuck_MeasureResult measureResult)
        {
            ProcessedData processedData = new ProcessedData();
            processedData.SetMemoryPara("ElectroStaticChuck_MeasureResult", measureResult);
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

            using Mat displayHeightImage = ElectroStaticChuckImagePipeline.HObjectToMat(measureResult.HeightImage);
            using Mat displayGrayImage = MFDCustomAlgo.CvDrawResult(measureResult);
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("ElectroStaticChuck_MeasureResult", new ImageResultsDisplay
            {
                GrayImage = displayGrayImage.Clone(),
                HeightImage = displayHeightImage.Clone(),
            }));
        }

        private void RefreshOutputParams()
        {
            Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (values.TryGetValue(item.ParamName, out object value))
                {
                    item.Value = value;
                }
            }
        }

        /// <summary>
        /// 触发采集
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始采集", ThreadOption.BackgroundThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != "TrrigerStartCollect") return;

            if (SltModel == null && SltModelName != null && SltModelName != "")
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }
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
            if (order != "TrrigerStopCollect") return;

            Logs.LogInfo("传感器停止采集");
            SltModel.StopCollect();
            Logs.LogInfo("开始处理数据...");
            var DataCollect = SltModel.ReceiveSensorData();


            (List<float[]> grayDate, List<float[]> heightData) = ConvertToMeasurePara(DataCollect);

            Algorithm.IsFlip = false;
            ElectroStaticChuck_MeasureResult measureResult = MFDCustomAlgo.Process(grayDate, heightData, Algorithm);
            PublishAlgorithmResult(measureResult);
        }



        public Tuple<List<float[]>, List<float[]>> ConvertToMeasurePara(List<MeasureData> listMeasureData)
        {
            List<float[]> grayDataList = new List<float[]>();
            List<float[]> heightDataList = new List<float[]>();

            if (listMeasureData != null)
            {
                foreach (var measureData in listMeasureData)
                {
                    if (measureData.AreaData != null && measureData.AreaData.Count >= 2)
                    {
                        //float[] heightArray = measureData.AreaData[0];
                        float[] heightArray = measureData.AreaData[0].Select(x => x * 10).ToArray();
                        heightDataList.Add(heightArray);

                        float[] grayArray = measureData.AreaData[1];
                        grayDataList.Add(grayArray);
                    }
                }
            }

            return Tuple.Create(grayDataList, heightDataList);
        }


        #endregion

    }
}

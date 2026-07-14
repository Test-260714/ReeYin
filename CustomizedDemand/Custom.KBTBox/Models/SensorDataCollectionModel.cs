using Custom.KBTBox.Helper;
using Custom.KBTBox.Views;
using Dm.util;
using DryIoc.ImTools;
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using static Custom.KBTBox.KBTDispensing_Algorithm;

namespace Custom.KBTBox.Models
{
    [Serializable]
    public class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        Drawing drawing = new Drawing();

        public string SltModelName;

        [JsonIgnore]
        KBTDispensing_Algorithm CustomAlgo;

        //[JsonIgnore]
        //Dictionary<Guid,List<MeasureData>> MeasureDatas = new Dictionary<Guid,List<MeasureData>>();

        ProcessedData processedData = new ProcessedData();

        private string StartTime;

        /// <summary>
        /// 转换点云图的方法
        /// </summary>
        private DepthMapToCloudUm depthMapToCloudUm = null;
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
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private KBTDispensing_MeasureParam _measureParam = new KBTDispensing_MeasureParam();

        public KBTDispensing_MeasureParam MeasureParam
        {
            get { return _measureParam; }
            set { _measureParam = value; RaisePropertyChanged(); }
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
        private JudgmentConfigModel _judgmentConfig = new JudgmentConfigModel();
        /// <summary>
        /// 判定参数配置
        /// </summary>
        public JudgmentConfigModel JudgmentConfig
        {
            get { return _judgmentConfig; }
            set { _judgmentConfig = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
            var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
            CustomAlgo = PrismProvider.Container.Resolve(typeof(KBTDispensing_Algorithm)) as KBTDispensing_Algorithm;

            Models = Param.Models;


        }

        ~SensorDataCollectionModel()
        {
            TriggerModuleRun -= () =>
            {
                return ExecuteModule().Result;
            };
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

                Task.Run(() =>
                {
                    EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);

                });

                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((order) =>
                {
                    if (!order.contains("StartTime")) return;


                    StartTime = order.Split('@')[1];
                    Logs.LogInfo($"获取到开始时间为：{StartTime}");
                }, ThreadOption.BackgroundThread);

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-传感器设置",
                    ViewName = "OtherConfigView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-判定配置",
                    ViewName = "JudgmentConfigView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    NodeSerial = Serial,
                    Type = DynamicViewType.NodeMap,
                    DisplayName = $"{Serial.ToString("D3")}-自动运行操作页面",
                    ViewName = "AutoProcessView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    NodeSerial = Serial,
                    Type = DynamicViewType.NodeMap,
                    DisplayName = $"{Serial.ToString("D3")}-灰度图显示",
                    ViewName = "GrayShowView"
                });

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun ??= () =>
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

                                Task.Run(() =>
                                {
                                    //PrismProvider.Dispatcher.BeginInvoke(() =>
                                    //{
                                    //    loading.Show();
                                    //});
                                    //SltModel.StopCollect();
                                    string[] folders = Directory.GetDirectories(FilePath);

                                    for (int i = 0; i < folders.Length; i++)
                                    {
                                        string folder = folders[i];

                                        string grayImageDir = folder + "/gray";
                                        string heightImageDir = folder + "/depth";

                                        string grayImagePath = grayImageDir + $"/1_tif.tif";
                                        string heightImageL0Path = heightImageDir + $"/1_tif_0.tif";
                                        string heightImageL1Path = heightImageDir + $"/1_tif_1.tif";

                                        Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                                        Mat heightImage = Cv2.ImRead(heightImageL0Path, ImreadModes.Unchanged);
                                        Mat height1Image = Cv2.ImRead(heightImageL1Path, ImreadModes.Unchanged);
                                        List<float[]> grayDate = CustomAlgo.ConvertMatToList(grayImage);
                                        List<float[]> heightDate = CustomAlgo.ConvertMatToList(heightImage);
                                        List<float[]> height1Date = CustomAlgo.ConvertMatToList(height1Image);

                                        Stopwatch stopwatch0 = Stopwatch.StartNew();

                                        CustomAlgo.Process(grayDate, heightDate, height1Date, ConvertMeasureParam(MeasureParam));

                                        stopwatch0.Stop();
                                        Console.WriteLine($"Process运行时间：{stopwatch0.Elapsed.TotalMilliseconds} 毫秒");
                                    }

                                    Stopwatch stopwatch1 = Stopwatch.StartNew();
                                    KBTDispensing_MeasureResult _measureResult = CustomAlgo.GetMeasureResult();
                                    Mat gray , depth;


                                    DateTime dateTime = DateTime.Now;
                                    drawing.CvDrawResult(_measureResult, out gray, out depth);

                                    Console.WriteLine("绘制图形耗时：" + DateTime.Now.Subtract(dateTime).TotalMilliseconds);
                                    //推送检测结果
                                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTDispensing_MeasureResult", _measureResult));
           
                                    stopwatch1.Stop();
                                    Console.WriteLine($"GetMeasureResult运行时间：{stopwatch1.Elapsed.TotalMilliseconds} 毫秒");
                                    processedData = new ProcessedData();
                                    processedData.SetMemoryPara("MFDJC0_MeasureResult", _measureResult);
                                    Stopwatch stopwatch2 = Stopwatch.StartNew();
                                    processedData.Gray = gray.Clone();
                                    stopwatch2.Stop();
                                    Console.WriteLine($"CvDrawResult运行时间：{stopwatch2.Elapsed.TotalMilliseconds} 毫秒");

                                    //通知数据更新
                                    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                                    PrismProvider.Dispatcher.BeginInvoke(() =>
                                    {
                                        loading.Close();
                                    });
                                });
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
                                else if (StopAndDispose)
                                {
                                    TrrigerDispose("TrrigerDispose");
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
            if (order != "TrrigerStartCollect") return;


            if (SltModel == null && SltModelName != null && SltModelName != "")
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }
            if (SltModel == null)
                Logs.LogInfo($"{SltModel}为空");
            Logs.LogInfo($"节点{Serial}开始触发，{SltModel.NickName}触发传感器开始采集...");
            SltModel.StartCollect();
            Logs.LogInfo("完成传感器开始采集...");
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集", ThreadOption.BackgroundThread)]
        public void TrrigerStopCollect(string order)
        {
            try
            {
                if (order != "TrrigerStopCollect") return;
                if (SltModel == null && SltModelName != null && SltModelName != "")
                {
                    SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
                }

                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：触发传感器停止采集");
                SltModel.StopCollect();
                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：开始获取数据...");
                var DataCollect = SltModel.ReceiveSensorData();
                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：结束获取数据...");
                List<List<float[]>> allDates = ConvertToMeasurePara(DataCollect);
                //是否需要存图（异步）
                if (OtherConfig.IsSaveImage)
                {
                    //DateTime tmpTime = DateTime.Now;
                    depthMapToCloudUm = new DepthMapToCloudUm(MeasureParam.IntervalX, MeasureParam.IntervalY, MeasureParam.IntervalZ);
                    Logs.LogInfo("开始存图单边图片！");
                    var rootPath = OtherConfig.SavePath + $"\\{StartTime}";

                    if (!Directory.Exists(rootPath))
                    {
                        Directory.CreateDirectory(rootPath);
                    }
                    // 获取所有直接子目录（不包含子目录的子目录）
                    string[] directories = Directory.GetDirectories(rootPath);

                    HObject grayImg = new HObject();
                    HObject depthImg = new HObject();
                    HObject depth1Img = new HObject();
                    CustomAlgo.ConvertListToHObject(allDates[0], KBTDispensing_Algorithm.ImageType.Gray, out grayImg);
                    CustomAlgo.ConvertListToHObject(allDates[1], KBTDispensing_Algorithm.ImageType.Depth, out depthImg);
                    CustomAlgo.ConvertListToHObject(allDates[2], KBTDispensing_Algorithm.ImageType.Depth, out depth1Img);

                    string grayfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\gray\\1_tif.tif";
                    string depthfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_0.tif";
                    string depth1filepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_1.tif";
                    string outputPly1path = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_2.ply";
                    string outputPly2path = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_3.ply";

                    // 获取父目录
                    string? dir = Path.GetDirectoryName(grayfilepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(depthfilepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(depth1filepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(outputPly1path);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(outputPly2path);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    HOperatorSet.WriteImage(grayImg, "tiff", 0, grayfilepath);
                    HOperatorSet.WriteImage(depthImg, "tiff", 0, depthfilepath);
                    HOperatorSet.WriteImage(depth1Img, "tiff", 0, depth1filepath);

                    depthMapToCloudUm.SavePlyAsciiUm(outputPly1path, allDates[1], centerOrigin: false, stride: 2);
                    depthMapToCloudUm.SavePlyAsciiUm(outputPly2path, allDates[2], centerOrigin: false, stride: 2);

                    CustomAlgo.Process(allDates[0], allDates[1], allDates[2], ConvertMeasureParam(MeasureParam));
                }
                else
                {
                    CustomAlgo.Process(allDates[0], allDates[1], allDates[2], ConvertMeasureParam(MeasureParam));
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.StackTrace.ToString());
                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Error);
            }
        }

        /// <summary>
        /// 触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始处理数据出图", ThreadOption.BackgroundThread)]
        public void TrrigerDispose(string order)
        {
            try
            {
                if (order != "TrrigerDispose") return;

                Logs.LogInfo("传感器停止采集并处理数据");
                SltModel.StopCollect();
                Logs.LogInfo("开始处理数据...");
                if (OtherConfig.IsSaveImage)
                {
                    List<List<float[]>> allDates = ConvertToMeasurePara(SltModel.ReceiveSensorData());
                    //DateTime tmpTime = DateTime.Now;
                    depthMapToCloudUm = new DepthMapToCloudUm(MeasureParam.IntervalX, MeasureParam.IntervalY, MeasureParam.IntervalZ);
                    Logs.LogInfo("开始存图单边图片！");
                    var rootPath = OtherConfig.SavePath + $"\\{StartTime}";

                    if (!Directory.Exists(rootPath))
                    {
                        Directory.CreateDirectory(rootPath);
                    }
                    // 获取所有直接子目录（不包含子目录的子目录）
                    string[] directories = Directory.GetDirectories(rootPath);

                    HObject grayImg = new HObject();
                    HObject depthImg = new HObject();
                    HObject depth1Img = new HObject();
                    CustomAlgo.ConvertListToHObject(allDates[0], KBTDispensing_Algorithm.ImageType.Gray, out grayImg);
                    CustomAlgo.ConvertListToHObject(allDates[1], KBTDispensing_Algorithm.ImageType.Depth, out depthImg);
                    CustomAlgo.ConvertListToHObject(allDates[2], KBTDispensing_Algorithm.ImageType.Depth, out depth1Img);

                    string grayfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\gray\\1_tif.tif";
                    string depthfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_0.tif";
                    string depth1filepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_1.tif";
                    string outputPly1path = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_2.ply";
                    string outputPly2path = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_3.ply";

                    // 获取父目录
                    string? dir = Path.GetDirectoryName(grayfilepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(depthfilepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(depth1filepath);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(outputPly1path);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 获取父目录
                    dir = Path.GetDirectoryName(outputPly2path);
                    // 如果目录不存在，自动创建
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    HOperatorSet.WriteImage(grayImg, "tiff", 0, grayfilepath);
                    HOperatorSet.WriteImage(depthImg, "tiff", 0, depthfilepath);
                    HOperatorSet.WriteImage(depth1Img, "tiff", 0, depth1filepath);

                    depthMapToCloudUm.SavePlyAsciiUm(outputPly1path, allDates[1], centerOrigin: false, stride: 2);
                    depthMapToCloudUm.SavePlyAsciiUm(outputPly2path, allDates[2], centerOrigin: false, stride: 2);

                    CustomAlgo.Process(allDates[0], allDates[1], allDates[2], ConvertMeasureParam(MeasureParam));

                    KBTDispensing_MeasureResult measureResult = CustomAlgo.GetMeasureResult();
                    processedData = new ProcessedData();
                    processedData.SetMemoryPara("KBTDispensing_MeasureResult", measureResult);

                    //推送检测结果
                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTDispensing_MeasureResult", measureResult));

                    CSVHelper.RootPath = OtherConfig.SaveCSVPath;
                    //CSVHelper.SaveAllData(measureResult);

                    //先绘制结果，在通知更新
                    Mat gray, depth;
                    drawing.CvDrawResult(measureResult, out gray, out depth);
                    processedData.Gray = gray.Clone();
                    depth.Dispose();

                    //通知数据更新
                    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                }
                else
                {
                    List<List<float[]>> allDates = ConvertToMeasurePara(SltModel.ReceiveSensorData());
                    CustomAlgo.Process(allDates[0], allDates[1], allDates[2], ConvertMeasureParam(MeasureParam));

                    KBTDispensing_MeasureResult measureResult = CustomAlgo.GetMeasureResult();
                    processedData = new ProcessedData();
                    processedData.SetMemoryPara("KBTDispensing_MeasureResult", measureResult);

                    //先绘制结果，在通知更新
                    Mat gray, depth;
                    drawing.CvDrawResult(measureResult, out gray, out depth);
                    depth.Dispose();
                    processedData.Gray = gray.Clone();
                    //通知数据更新
                    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                }

                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
            }
            catch (Exception ex)
            {
                Logs.LogError(ex.StackTrace.ToString());
                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Error);
            }
        }

        /// <summary>
        /// 触发重置
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent),"触发重置采集数据", ThreadOption.BackgroundThread)]
        public void Reset(string order)
        {
            if (order != "Reset") return;

            Logs.LogInfo("开始停止采集并释放之前的资源");
            SltModel?.StopCollect();
            CustomAlgo?.Dispose();
            Logs.LogInfo("完成...");
        }

        /// <summary>
        /// 对一些参数*10
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private KBTDispensing_MeasureParam ConvertMeasureParam(KBTDispensing_MeasureParam param)
        {
            return new KBTDispensing_MeasureParam
            {
                ModelPath = MeasureParam.ModelPath,
                IntervalX = MeasureParam.IntervalX,
                IntervalY = MeasureParam.IntervalY,
                IntervalZ = MeasureParam.IntervalZ,
                MinDepth = MeasureParam.MinDepth * 10,
                MaxDepth = MeasureParam.MaxDepth * 10,
                GlueLowThresh = MeasureParam.GlueLowThresh * 10,
                GlueUpThresh = MeasureParam.GlueUpThresh * 10,
                FrameLowThresh = MeasureParam.FrameLowThresh * 10,
                FrameUpThresh = MeasureParam.FrameUpThresh * 10,
                SamplingInterval = MeasureParam.SamplingInterval,
                SamplingViewInterval = MeasureParam.SamplingViewInterval,
                RefractiveIndex = MeasureParam.RefractiveIndex,
                IsSaveImage = MeasureParam.IsSaveImage,
                SaveImagePath = MeasureParam.SaveImagePath,
            };
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
                    if (measureData.AreaData != null && measureData.AreaData.Count >= 2)
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

            return [grayDataList, height1DataList,height2DataList];
        }

        /// <summary>
        /// 存高度/灰度图
        /// </summary>
        public void SaveImage()
        {
            //DateTime tmpTime = DateTime.Now;
            //var imageDatas = CustomAlgo.GetImageData();
            //Console.WriteLine($"图片数量为：{imageDatas.Count}");

            //var rootPath = OtherConfig.SavePath + $"\\{tmpTime.ToString("yyyyMMddHHmmssfff")}";
            //for (int i = 0; i < imageDatas.Count; i++)
            //{
            //    ImageData imageData = imageDatas[i];

            //    string imageName = $"{Algorithm.IntervalX.ToString("F3")}_{Algorithm.IntervalY.ToString("F3")}_" +
            //                       $"{Algorithm.IntervalZ.ToString("F3")}_{Algorithm.MinDepth}_{Algorithm.MaxDepth}_" +
            //                       $"{Algorithm.OffsetX}_{Algorithm.OffsetY}";

            //    if (i == 0)
            //        imageName = $"{Algorithm.IntervalX.ToString("F3")}_{Algorithm.IntervalY.ToString("F3")}_" +
            //                       $"{Algorithm.IntervalZ.ToString("F3")}_{Algorithm.MinDepth}_{Algorithm.MaxDepth}_" +
            //                       $"{0}_{0}";
            //    string GrayImageFilePath = rootPath + $"\\{i}" + "\\gray" + $"\\{imageName}" + ".tif";
            //    DirectoryInfo directoryInfo = Directory.GetParent(GrayImageFilePath);
            //    if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);

            //    string HeightImageFilePath = rootPath + $"\\{i}" + "\\depth" + $"\\{imageName}" + ".tif";
            //    directoryInfo = Directory.GetParent(HeightImageFilePath);
            //    if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);

            //    HObject hoGrayImage = imageData.hoGrayImage;
            //    HObject hoHeightImage = imageData.hoHeightImage;

            //    HOperatorSet.WriteImage(hoGrayImage, "tiff", 0, GrayImageFilePath);
            //    HOperatorSet.WriteImage(hoHeightImage, "tiff", 0, HeightImageFilePath);
            //}

            //CustomAlgo.Dispose();
        }
        #endregion

    }
}

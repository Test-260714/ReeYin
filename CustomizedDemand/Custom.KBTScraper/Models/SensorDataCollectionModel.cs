using HalconDotNet;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.Hyperson;
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
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using static Custom.KBTScraper.Models.KBTGDJC_Algorithm;
using Custom.KBTScraper.Helper;

namespace Custom.KBTScraper.Models
{
    [Serializable]
    public class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        public string SltModelName = string.Empty;

        [JsonIgnore]
        KBTGDJC_Algorithm? CustomAlgo;

        ProcessedData processedData = new ProcessedData();

        private string StartTime = string.Empty;

        // 走带分段缓冲
        private readonly List<MeasureData> _distanceBuffer = new List<MeasureData>();

        // 走带采集循环控制
        private CancellationTokenSource? _collectCts;
        private Task? _collectTask;
        
        // 数据处理队列和控制
        private readonly Queue<List<MeasureData>> _processingQueue = new Queue<List<MeasureData>>();
        private readonly object _queueLock = new object();
        private Task? _processingTask;
        private CancellationTokenSource? _processingCts;
        
        // PLC对象（用于写入缺陷信号）
        [JsonIgnore]
        private ReeYin_V.Hardware.PLC.Models.PLCBase? _curPLC;
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<SensorBase>? _models = new ObservableCollection<SensorBase>();
        [JsonIgnore]
        public ObservableCollection<SensorBase> Models
        {
            get { return _models ?? new ObservableCollection<SensorBase>(); }
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _filePath = string.Empty;
        [OutputParam("FilePath", "文件取图路径")]
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _sltFile = string.Empty;
        public string SltFile
        {
            get { return _sltFile; }
            set { _sltFile = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _linkPath = string.Empty;
        public string LinkPath
        {
            get { return _linkPath; }
            set { _linkPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isLinkVisibility = Visibility.Hidden;
        public Visibility IsLinkVisibility
        {
            get { return _isLinkVisibility; }
            set { _isLinkVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _isFileVisibility = Visibility.Visible;
        public Visibility IsFileVisibility
        {
            get { return _isFileVisibility; }
            set { _isFileVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isLinkMode = false;
        public bool IsLinkMode
        {
            get { return _isLinkMode; }
            set
            {
                _isLinkMode = value;
                RaisePropertyChanged();
                // 切换显示模式
                if (value)
                {
                    IsFileVisibility = Visibility.Hidden;
                    IsLinkVisibility = Visibility.Visible;
                }
                else
                {
                    IsFileVisibility = Visibility.Visible;
                    IsLinkVisibility = Visibility.Hidden;
                }
            }
        }

        [JsonIgnore]
        private int _sltTriggerPicIndex = 2; // 默认为指定采集模式
        public int SltTriggerPicIndex
        {
            get { return _sltTriggerPicIndex; }
            set { _sltTriggerPicIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SensorBase? _sltModel;
        [JsonIgnore]
        public SensorBase? SltModel
        {
            get { return _sltModel; }
            set { _sltModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _startCollect = true;  // 默认选中"开始采集"
        public bool StartCollect
        {
            get { return _startCollect; }
            set { _startCollect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _stopAndDispose;
        public bool StopAndDispose
        {
            get { return _stopAndDispose; }
            set { _stopAndDispose = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 算法参数（直接引用算法单例的参数，实现多页面共享）
        /// </summary>
        public KBTGDJC_MeasureParam MeasureParam
        {
            get { return CustomAlgo?.MParam ?? new KBTGDJC_MeasureParam(); }
        }

        private OtherConfigModel _otherConfig = new OtherConfigModel();
        /// <summary>
        /// 其他配置（会被序列化保存）
        /// </summary>
        public OtherConfigModel OtherConfig
        {
            get { return _otherConfig; }
            set { _otherConfig = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 走带距离(mm)，用于计算帧数阈值
        /// </summary>
        private double _travelDistanceMm = 10.0;
        public double TravelDistanceMm
        {
            get { return _travelDistanceMm; }
            set { _travelDistanceMm = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// X方向像素当量(μm)，手动配置
        /// </summary>
        private double _sensorIntervalX = 2.9;
        public double SensorIntervalX
        {
            get { return _sensorIntervalX; }
            set { _sensorIntervalX = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Y方向像素当量(μm)，手动配置
        /// </summary>
        private double _sensorIntervalY = 5.0;
        public double SensorIntervalY
        {
            get { return _sensorIntervalY; }
            set { _sensorIntervalY = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Z方向像素当量(μm)，手动配置
        /// </summary>
        private double _sensorIntervalZ = 0.1;
        public double SensorIntervalZ
        {
            get { return _sensorIntervalZ; }
            set { _sensorIntervalZ = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _enablePlcDefectSignal = true;
        public bool EnablePlcDefectSignal
        {
            get { return _enablePlcDefectSignal; }
            set { _enablePlcDefectSignal = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 实际帧数阈值（根据走带距离和Y像素当量计算）
        /// </summary>
        [JsonIgnore]
        private int _frameThreshold = 1250;
        [JsonIgnore]
        public int FrameThreshold
        {
            get { return _frameThreshold; }
            private set { _frameThreshold = value; }
        }

        // 已累计的帧数，用于达到阈值后再处理
        [JsonIgnore]
        private int _accumulatedFrameCount = 0;
        
        // 数据统计（用于检测丢数据）
        [JsonIgnore]
        private int _totalReceivedFrames = 0;  // 总接收帧数
        [JsonIgnore]
        private int _totalProcessedFrames = 0;  // 总处理帧数
        [JsonIgnore]
        private DateTime _collectStartTime;     // 采集开始时间

        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
            Task.Run(() =>
            {
                EventSubscriptionHelper.AutoSubscribe(this, PrismProvider.EventAggregator);
            });

            var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
            CustomAlgo = PrismProvider.Container.Resolve(typeof(KBTGDJC_Algorithm)) as KBTGDJC_Algorithm ?? new KBTGDJC_Algorithm(MeasureParam);

            Models = Param.Models ?? new ObservableCollection<SensorBase>();

            // 初始化PLC
            var plcConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as ReeYin_V.Hardware.PLC.Models.PLCSetModel;
            if (plcConfig != null && plcConfig.Models.Count > 0)
            {
                _curPLC = plcConfig.Models[0];
                Logs.LogInfo($"PLC已初始化: {_curPLC.Config.DisplayName}");
            }
            else
            {
                Logs.LogWarning("未找到PLC配置");
            }

            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };

            PrismProvider.EventAggregator.GetEvent<NodifyRemoveNodeEvent>().Subscribe((order) =>
            {
                if (order != Serial.ToString()) return;
                Dispose();
            }, ThreadOption.UIThread);

            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((order) =>
            {
                if (!order.Contains("StartTime")) return;
                StartTime = order.Split('@')[1];
                Logs.LogInfo($"获取到开始时间为：{StartTime}");
            }, ThreadOption.BackgroundThread);
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
        public override bool LoadKeyParam()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

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
                DisplayName = $"{Serial.ToString("D3")}-其他配置",
                ViewName = "OtherConfigView"
            });
        }
        #endregion

        #region Methods

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            Logs.LogInfo($"ExecuteModule开始执行，模式: {SltTriggerPicIndex}, 路径: {FilePath}");
            
            NodeStatus result;
            double time;
            (result, time) = await Task.Run(() => SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        // 指定图像：选择单个文件夹，处理一张图片
                        case 0:
                            {
                                Console.WriteLine("[KBTScraper] 进入指定图像模式");
                                Logs.LogInfo("进入指定图像模式 (case 0)");
                                
                                // 检查文件夹路径（FilePath 指向包含 gray/depth 的文件夹）
                                if (string.IsNullOrEmpty(FilePath) || !Directory.Exists(FilePath))
                                {
                                    Console.WriteLine($"[KBTScraper] 文件夹路径无效: {FilePath}");
                                    MessageBox.Show("指定的文件夹不存在，请重新选择文件夹！");
                                    FilePath = "";
                                    return NodeStatus.Error;
                                }
                                
                                string grayImageDir = Path.Combine(FilePath, "gray");
                                string heightImageDir = Path.Combine(FilePath, "depth");
                                
                                // 检查 gray 和 depth 目录
                                if (!Directory.Exists(grayImageDir) || !Directory.Exists(heightImageDir))
                                {
                                    Console.WriteLine($"[KBTScraper] 缺少 gray 或 depth 目录");
                                    MessageBox.Show("指定的文件夹中缺少 gray 或 depth 子目录！");
                                    return NodeStatus.Error;
                                }
                                
                                // 获取图像文件
                                string[] grayImagePaths = Directory.GetFiles(grayImageDir, "*.tif");
                                string[] heightImagePaths = Directory.GetFiles(heightImageDir, "*.tif");
                                
                                if (grayImagePaths.Length == 0 || heightImagePaths.Length == 0)
                                {
                                    Console.WriteLine($"[KBTScraper] 缺少图像文件");
                                    MessageBox.Show("指定的文件夹中缺少图像文件！");
                                    return NodeStatus.Error;
                                }
                                
                                // 读取图像
                                using Mat grayImage = Cv2.ImRead(grayImagePaths[0], ImreadModes.Grayscale);
                                using Mat heightImage = Cv2.ImRead(heightImagePaths[0], ImreadModes.Unchanged);
                                
                                if (grayImage.Empty() || heightImage.Empty())
                                {
                                    Console.WriteLine($"[KBTScraper] 无法读取图像文件");
                                    MessageBox.Show("无法读取图像文件！");
                                    return NodeStatus.Error;
                                }
                                
                                Console.WriteLine($"[KBTScraper] 图像读取成功，尺寸: {grayImage.Width}x{grayImage.Height}");
                                Logs.LogInfo($"图像读取成功，灰度图: {grayImagePaths[0]}, 高度图: {heightImagePaths[0]}");
                                
                                // 处理图像
                                Stopwatch processWatch = Stopwatch.StartNew();
                                KBTGDJC_MeasureResult measureResult = ProcessSingleImage(grayImage, heightImage, 0, 0);
                                processWatch.Stop();
                                
                                Console.WriteLine($"[KBTScraper] Process耗时: {processWatch.Elapsed.TotalMilliseconds:F2}ms");
                                Logs.LogInfo($"Process耗时: {processWatch.Elapsed.TotalMilliseconds:F2}ms, 缺陷数量: {measureResult.Defects?.Count ?? 0}");
                                
                                // 发布结果
                                PublishResult(measureResult);
                                
                                // 异步保存
                                SaveResultImageAsync(measureResult.GrayImage, $"result_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                                SaveDefectDataAsync(measureResult.Defects);
                                
                                Console.WriteLine("[KBTScraper] 指定图像处理完成");
                            }
                            break;

                        case 1:
                            {
                                Console.WriteLine("[KBTScraper] 进入文件取图模式");
                                Logs.LogInfo("进入文件取图模式 (case 1)");

                                LoadingWindow? loading = null;
                                //PrismProvider.Dispatcher.Invoke(() =>
                                //{
                                //    loading = new LoadingWindow() { Topmost = true };
                                //    loading.Show();
                                //});
                                
                                Task.Run(() =>
                                {
                                    Stopwatch totalStopwatch = Stopwatch.StartNew();
                                    NodeStatus taskResult = NodeStatus.Success;
                                    
                                    try
                                    {
                                        // 检查文件夹路径
                                        if (string.IsNullOrEmpty(FilePath) || !Directory.Exists(FilePath))
                                        {
                                            Console.WriteLine($"[KBTScraper] 文件夹路径无效: {FilePath}");
                                            Logs.LogInfo($"文件夹路径无效: {FilePath}");
                                            taskResult = NodeStatus.Error;
                                            PrismProvider.Dispatcher.BeginInvoke(() =>
                                            {
                                                MessageBox.Show("指定的文件夹不存在，请重新选择文件夹！");
                                                loading?.Close();
                                            });
                                            return;
                                        }

                                        string[] folders = Directory.GetDirectories(FilePath);
                                        Console.WriteLine($"[KBTScraper] 找到 {folders.Length} 个子文件夹");
                                        Logs.LogInfo($"找到 {folders.Length} 个子文件夹");
                                        
                                        if (folders.Length == 0)
                                        {
                                            taskResult = NodeStatus.Error;
                                            PrismProvider.Dispatcher.BeginInvoke(() =>
                                            {
                                                MessageBox.Show("指定的文件夹中没有子文件夹！");
                                                loading?.Close();
                                            });
                                            return;
                                        }

                                        Console.WriteLine($"[KBTScraper] 算法参数: MinDefectDepth={MeasureParam.MinDefectDepth}, MinDefectRadius={MeasureParam.MinDefectDiameter}");
                                        Logs.LogInfo($"算法参数: IntervalX={MeasureParam.IntervalX}, IntervalY={MeasureParam.IntervalY}, MinDefectDepth={MeasureParam.MinDefectDepth}");

                                        int imageCounter = 1;
                                        int processedCount = 0;
                                        
                                        // 收集所有缺陷数据，最后统一保存一个CSV
                                        List<DefectResult> allDefects = new List<DefectResult>();
                                        
                                        for (int i = 0; i < folders.Length; i++)
                                        {
                                            Stopwatch itemWatch = Stopwatch.StartNew();  // 单张完整处理计时开始
                                            
                                            string folder = folders[i];
                                            string grayImageDir = Path.Combine(folder, "gray");
                                            string heightImageDir = Path.Combine(folder, "depth");

                                            // 检查目录是否存在
                                            if (!Directory.Exists(grayImageDir) || !Directory.Exists(heightImageDir))
                                            {
                                                Console.WriteLine($"[KBTScraper] 跳过 {folder}：缺少 gray 或 depth 目录");
                                                Logs.LogInfo($"跳过文件夹 {folder}：缺少 gray 或 depth 目录");
                                                continue;
                                            }

                                            // 获取图像文件
                                            string[] grayImagePaths = Directory.GetFiles(grayImageDir, "*.tif");
                                            string[] heightImagePaths = Directory.GetFiles(heightImageDir, "*.tif");

                                            if (grayImagePaths.Length == 0 || heightImagePaths.Length == 0)
                                            {
                                                Console.WriteLine($"[KBTScraper] 跳过 {folder}：缺少图像文件");
                                                Logs.LogInfo($"跳过文件夹 {folder}：缺少图像文件");
                                                continue;
                                            }

                                            // 读取图像
                                            using Mat grayImage = Cv2.ImRead(grayImagePaths[0], ImreadModes.Grayscale);
                                            using Mat heightImage = Cv2.ImRead(heightImagePaths[0], ImreadModes.Unchanged);

                                            if (grayImage.Empty() || heightImage.Empty())
                                            {
                                                Console.WriteLine($"[KBTScraper] 跳过：无法读取图像文件");
                                                Logs.LogInfo($"无法读取图像文件：{grayImagePaths[0]} 或 {heightImagePaths[0]}");
                                                continue;
                                            }

                                            // 调用算法处理
                                            Stopwatch processWatch = Stopwatch.StartNew();
                                            KBTGDJC_MeasureResult measureResult = ProcessSingleImage(grayImage, heightImage, 0, 0);
                                            processWatch.Stop();
                                            double algoTime = processWatch.Elapsed.TotalMilliseconds;

                                            // 收集缺陷数据（最后统一保存）
                                            if (measureResult.Defects != null && measureResult.Defects.Count > 0)
                                            {
                                                allDefects.AddRange(measureResult.Defects);
                                            }
                                            
                                            processedCount++;

                                            // 先展示结果（优先响应）
                                            Stopwatch showWatch = Stopwatch.StartNew();
                                            PublishResult(measureResult);
                                            showWatch.Stop();
                                            double showTime = showWatch.Elapsed.TotalMilliseconds;
                                            
                                            // 单张完整处理计时结束
                                            itemWatch.Stop();
                                            int defectCount = measureResult.Defects?.Count ?? 0;
                                            Console.WriteLine($"[KBTScraper] 处理 {i + 1}/{folders.Length}, 算法: {algoTime:F0}ms, 展示: {showTime:F0}ms, 完整: {itemWatch.Elapsed.TotalMilliseconds:F0}ms, 缺陷: {defectCount}");
                                            Logs.LogInfo($"文件夹 {i + 1}/{folders.Length} 算法: {algoTime:F2}ms, 展示: {showTime:F2}ms, 完整: {itemWatch.Elapsed.TotalMilliseconds:F2}ms, 缺陷: {defectCount}");

                                            // 异步保存结果图片
                                            SaveResultImageAsync(measureResult.GrayImage, $"images{imageCounter}.png");
                                            imageCounter++;
                                        }
                                        
                                        // 统一保存所有缺陷数据到一个CSV文件（异步）
                                        SaveDefectDataAsync(allDefects);
                                        
                                        Logs.LogInfo($"处理完成，共处理 {processedCount}/{folders.Length} 个文件夹");
                                        Console.WriteLine($"[KBTScraper] 处理完成，共处理 {processedCount}/{folders.Length} 个文件夹");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[KBTScraper] 异常: {ex.Message}");
                                        Logs.LogInfo($"文件取图处理异常：{ex.Message}\n{ex.StackTrace}");
                                        taskResult = NodeStatus.Error;
                                        PrismProvider.Dispatcher.BeginInvoke(() =>
                                        {
                                            MessageBox.Show($"处理过程中发生错误：{ex.Message}");
                                        });
                                    }
                                    finally
                                    {
                                        totalStopwatch.Stop();
                                        double totalTime = totalStopwatch.Elapsed.TotalMilliseconds;
                                        Console.WriteLine($"[KBTScraper] 总耗时: {totalTime:F2}ms");
                                        Logs.LogInfo($"文件目录模式总耗时：{totalTime:F2} 毫秒");
                                        
                                        Output = new ExecuteModuleOutput()
                                        {
                                            RunStatus = taskResult,
                                            RunTime = totalTime,
                                        };
                                        
                                        PrismProvider.Dispatcher.BeginInvoke(() => loading?.Close());
                                    }
                                });
                            }
                            break;

                        case 2:
                            {
                                Logs.LogInfo("进入传感器采集模式 (case 2)");
                                Logs.LogInfo($"StartCollect: {StartCollect}, StopAndDispose: {StopAndDispose}");
                                if (StartCollect)
                                {
                                    Logs.LogInfo("触发开始采集");
                                    TrrigerStartCollect("TrrigerStartCollect");
                                }
                                else if (StopAndDispose)
                                {
                                    Logs.LogInfo("触发停止采集并处理数据");
                                    TrrigerDispose("TrrigerDispose");
                                }
                                else
                                {
                                    Logs.LogInfo("传感器采集模式：没有匹配的触发条件");
                                }

                                // 执行后不重置按钮状态，保持用户选择
                                // StartCollect = false;
                                // StopAndDispose = false;
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception)
                {
                    return NodeStatus.Error;
                }

                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }

                return NodeStatus.Success;
            }));

            Logs.LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// 触发开始采集（走带回调模式）
        /// 传感器使用回调方式采集数据，数据会自动缓存到传感器内部
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始采集", ThreadOption.BackgroundThread)]
        public void TrrigerStartCollect(string order)
        {
            if (order != "TrrigerStartCollect") return;

            Logs.LogInfo("开始触发传感器采集");

            // 初始化传感器
            if (SltModel == null && !string.IsNullOrEmpty(SltModelName))
            {
                SltModel = Models?.Where(c => c.NickName == SltModelName).FirstOrDefault() ?? Models?.FirstOrDefault();
                Logs.LogInfo($"根据名称查找传感器: {SltModelName}，找到: {SltModel?.NickName ?? "null"}");
            }

            if (SltModel == null)
            {
                Logs.LogError("未找到传感器，请检查配置");
                return;
            }

            // 检查传感器连接状态
            if (!SltModel.IsConnected)
            {
                Logs.LogError($"传感器 {SltModel.NickName} 未连接，请先连接传感器");
                return;
            }

            Logs.LogInfo($"传感器信息: {SltModel.NickName}, 类型: {SltModel.GetType().Name}, 已连接: {SltModel.IsConnected}");

            // 将手动配置的XYZ参数传给算法
            MeasureParam.IntervalX = SensorIntervalX;
            MeasureParam.IntervalY = SensorIntervalY;
            MeasureParam.IntervalZ = SensorIntervalZ;
            Logs.LogInfo($"算法使用参数: IntervalX={MeasureParam.IntervalX}, IntervalY={MeasureParam.IntervalY}, IntervalZ={MeasureParam.IntervalZ}");
            
            // 根据走带距离计算帧数阈值：帧数 = 走带距离(mm) * 1000 / Y像素当量(um)
            if (MeasureParam.IntervalY > 0)
            {
                _frameThreshold = (int)(TravelDistanceMm * 1000 / MeasureParam.IntervalY);
                if (_frameThreshold < 1) _frameThreshold = 1;
            }
            Logs.LogInfo($"走带距离: {TravelDistanceMm}mm, 计算帧数阈值: {FrameThreshold}");
            Logs.LogInfo("传感器开始采集（回调模式）");
            
            try
            {
                SltModel.StartCollect();
                Logs.LogInfo("传感器StartCollect()调用成功");
            }
            catch (Exception ex)
            {
                Logs.LogError($"传感器StartCollect()调用失败: {ex.Message}");
                return;
            }

            // 启动后台循环，持续取回调缓存做帧数分段处理
            _collectCts?.Cancel();
            _collectCts = new CancellationTokenSource();
            
            // 重置统计数据
            _totalReceivedFrames = 0;
            _totalProcessedFrames = 0;
            _collectStartTime = DateTime.Now;
            
            _collectTask = Task.Run(() => CollectLoop(_collectCts.Token));
            Logs.LogInfo("后台采集循环已启动");
            
            // 启动数据处理循环
            _processingCts?.Cancel();
            _processingCts = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessingLoop(_processingCts.Token));
            Logs.LogInfo("后台处理循环已启动");
        }

        private async Task CollectLoop(CancellationToken ct)
        {
            Logs.LogInfo("走带采集后台循环启动");
            int emptyCount = 0;
            
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (SltModel == null)
                    {
                        Logs.LogError("传感器对象为空，退出采集循环");
                        break;
                    }

                    var dataCollect = SltModel.ReceiveSensorData();
                    if (dataCollect != null && dataCollect.Count > 0)
                    {
                        emptyCount = 0; // 重置空计数
                        _distanceBuffer.AddRange(dataCollect);
                        _accumulatedFrameCount += dataCollect.Count;
                        _totalReceivedFrames += dataCollect.Count;
                        
                        // 达到帧数阈值后，将数据放入处理队列（非阻塞）
                        while (_accumulatedFrameCount >= FrameThreshold && _distanceBuffer.Count >= FrameThreshold)
                        {
                            // 取出指定帧数的数据
                            var segmentData = _distanceBuffer.Take(FrameThreshold).ToList();
                            _distanceBuffer.RemoveRange(0, FrameThreshold);
                            _accumulatedFrameCount -= FrameThreshold;
                            
                            // 放入处理队列，不阻塞采集
                            lock (_queueLock)
                            {
                                _processingQueue.Enqueue(segmentData);
                            }
                            
                            // 定期输出统计信息
                            if (_totalReceivedFrames % (FrameThreshold * 5) == 0)
                            {
                                var elapsed = (DateTime.Now - _collectStartTime).TotalSeconds;
                                var fps = _totalReceivedFrames / elapsed;
                                Logs.LogInfo($"[统计] 已接收: {_totalReceivedFrames} 帧, 已处理: {_totalProcessedFrames} 帧, " +
                                           $"队列长度: {_processingQueue.Count}, 采集速率: {fps:F2} 帧/秒");
                            }
                        }
                        
                        // 检查队列积压情况
                        int queueCount;
                        lock (_queueLock)
                        {
                            queueCount = _processingQueue.Count;
                        }
                        
                        if (queueCount > 5)
                        {
                            Logs.LogWarning($"⚠️ 处理队列积压严重！当前队列长度: {queueCount}，可能存在丢数据风险");
                        }
                    }
                    else
                    {
                        emptyCount++;
                        if (emptyCount % 100 == 0) // 每100次空数据记录一次日志
                        {
                            Logs.LogInfo($"传感器暂无数据，已等待 {emptyCount * 10} ms");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logs.LogError($"走带采集循环异常: {ex.Message}");
                }

                try
                {
                    await Task.Delay(10, ct);
                }
                catch (TaskCanceledException) { }
            }
            Logs.LogInfo("走带采集后台循环结束");
        }

        /// <summary>
        /// 数据处理循环（独立线程，不阻塞采集）
        /// </summary>
        private async Task ProcessingLoop(CancellationToken ct)
        {
            Logs.LogInfo("数据处理循环启动");
            
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    List<MeasureData>? segmentData = null;
                    
                    // 从队列取出数据
                    lock (_queueLock)
                    {
                        if (_processingQueue.Count > 0)
                        {
                            segmentData = _processingQueue.Dequeue();
                        }
                    }
                    
                    if (segmentData != null)
                    {
                        // 处理数据（同步调用，但在独立线程中）
                        ProcessSegmentData(segmentData, isFinal: false);
                        _totalProcessedFrames += segmentData.Count;
                        
                        int remainingCount;
                        lock (_queueLock)
                        {
                            remainingCount = _processingQueue.Count;
                        }
                        
                        if (remainingCount > 0)
                        {
                            Logs.LogInfo($"数据段处理完成（{segmentData.Count} 帧），队列剩余: {remainingCount}");
                        }
                    }
                    else
                    {
                        // 队列为空，等待一下
                        try
                        {
                            await Task.Delay(50, ct);
                        }
                        catch (TaskCanceledException) { }
                    }
                }
                catch (TaskCanceledException)
                {
                    // 正常取消，不记录错误
                }
                catch (Exception ex)
                {
                    Logs.LogError($"数据处理循环异常: {ex.Message}\n{ex.StackTrace}");
                }
            }
            
            Logs.LogInfo("数据处理循环结束");
        }

        /// <summary>
        /// 停止采集并处理最后一段数据
        /// 走带结束时调用，停止传感器并处理剩余缓存数据
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集并处理最后一段数据", ThreadOption.BackgroundThread)]
        public void TrrigerDispose(string order)
        {
            if (order != "TrrigerDispose") return;

            Logs.LogInfo("传感器停止采集并处理最后一段数据");

            if (SltModel == null)
            {
                Logs.LogError("传感器未初始化");
                PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
                return;
            }

            // 先停止传感器采集
            SltModel.StopCollect();
            Logs.LogInfo("传感器已停止采集");

            // 再停止后台采集循环，避免同时操作数据
            _collectCts?.Cancel();
            try
            {
                _collectTask?.Wait(1000);  // 等待循环结束，最多 1 秒
            }
            catch (Exception) { }
            Logs.LogInfo("后台采集循环已停止");

            // 取出最后的回调缓存数据（只有真正有数据才处理）
            var DataCollect = SltModel.ReceiveSensorData();
            if (DataCollect != null && DataCollect.Count > 0)
            {
                _distanceBuffer.AddRange(DataCollect);
                _totalReceivedFrames += DataCollect.Count;
                Logs.LogInfo($"取出剩余回调缓存数据 {DataCollect.Count} 帧");
            }
            else
            {
                Logs.LogInfo("传感器缓存无剩余数据");
            }

            // 将剩余的缓冲数据加入处理队列（只有有数据才处理）
            if (_distanceBuffer.Count > 0)
            {
                Logs.LogInfo($"将剩余缓冲数据加入处理队列，共 {_distanceBuffer.Count} 帧");
                lock (_queueLock)
                {
                    _processingQueue.Enqueue(_distanceBuffer.ToList());
                }
                _distanceBuffer.Clear();
                _accumulatedFrameCount = 0;
            }
            else
            {
                Logs.LogInfo("缓冲区无剩余数据，无需额外处理");
            }

            // 等待处理队列清空
            int waitCount = 0;
            int maxWaitSeconds = 300; // 最多等待5分钟
            while (waitCount < maxWaitSeconds)
            {
                int queueCount;
                lock (_queueLock)
                {
                    queueCount = _processingQueue.Count;
                }
                
                if (queueCount == 0)
                {
                    Logs.LogInfo("处理队列已清空");
                    break;
                }
                
                if (waitCount % 10 == 0) // 每10秒记录一次
                {
                    Logs.LogInfo($"等待处理队列清空，剩余 {queueCount} 段数据...");
                }
                
                System.Threading.Thread.Sleep(1000);
                waitCount++;
            }
            
            // 停止处理循环
            _processingCts?.Cancel();
            try
            {
                _processingTask?.Wait(2000);
            }
            catch (Exception) { }
            Logs.LogInfo("后台处理循环已停止");

            // 输出最终统计信息
            var totalElapsed = (DateTime.Now - _collectStartTime).TotalSeconds;
            Logs.LogInfo($"========== 采集完成统计 ==========");
            Logs.LogInfo($"总接收帧数: {_totalReceivedFrames}");
            Logs.LogInfo($"总处理帧数: {_totalProcessedFrames}");
            Logs.LogInfo($"丢失帧数: {_totalReceivedFrames - _totalProcessedFrames}");
            Logs.LogInfo($"采集时长: {totalElapsed:F2} 秒");
            Logs.LogInfo($"平均采集速率: {_totalReceivedFrames / totalElapsed:F2} 帧/秒");
            Logs.LogInfo($"================================");
            
            if (_totalReceivedFrames != _totalProcessedFrames)
            {
                Logs.LogWarning($"⚠️ 检测到数据丢失！接收 {_totalReceivedFrames} 帧，处理 {_totalProcessedFrames} 帧");
            }

            // 释放算法资源
            CustomAlgo?.Dispose();

            // 流程结束时清除PLC缺陷标志位
            WritePLCDefectSignal(false);
            Logs.LogInfo("流程结束，已清除PLC地址2206缺陷标志位");

            PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
            Logs.LogInfo("走带采集全部完成");
        }

        /// <summary>
        /// 处理指定的数据段
        /// </summary>
        private void ProcessSegmentData(List<MeasureData> segmentData, bool isFinal)
        {
            if (segmentData.Count == 0)
            {
                Logs.LogInfo("分段处理：数据为空，无需处理");
                return;
            }

            Stopwatch totalWatch = Stopwatch.StartNew();  // 完整处理计时开始

            Logs.LogInfo($"分段处理：包含 {segmentData.Count} 帧数据，IsFinal = {isFinal}");

            // 转换数据格式
            List<List<float[]>> allDates = ConvertToMeasurePara(segmentData);
            if (allDates[0].Count == 0)
            {
                Logs.LogInfo("分段处理：数据转换为空，跳过");
                return;
            }

            // 异步保存原始图（不阻塞主流程）
            if (OtherConfig.IsSaveImage)
            {
                var dataToSave = allDates.Select(list => list.ToList()).ToList();
                Task.Run(() =>
                {
                    try
                    {
                        SaveSensorImage(dataToSave);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"异步保存原始图失败: {ex.Message}");
                    }
                });
            }

            // 调用算法处理
            Logs.LogInfo($"分段处理：开始算法处理，共 {allDates[0].Count} 行");
            Stopwatch sw = Stopwatch.StartNew();

            MeasureParam.ProductNum = OtherConfig.ProductNum;
            MeasureParam.BatchNum = OtherConfig.BatchNum;
            MeasureParam.IsScanEnd = isFinal;
            CustomAlgo?.Process(allDates[0], allDates[1], MeasureParam);

            sw.Stop();
            double algoTime = sw.Elapsed.TotalMilliseconds;

            // 获取结果
            KBTGDJC_MeasureResult measureResult = CustomAlgo?.GetDefectsResult() ?? new KBTGDJC_MeasureResult();

            // 检测到缺陷时立即向PLC写入信号（优先响应）
            int defectCount = measureResult.Defects?.Count ?? 0;
            if (EnablePlcDefectSignal && defectCount > 0)
            {
                WritePLCDefectSignal(true);
                Logs.LogInfo($"检测到 {defectCount} 个缺陷，已向PLC地址2206写入true");
            }

            // 计算真实坐标(mm)：根据实际处理的帧数和Y像素当量计算
            int framesBeforeThisSegment = _totalProcessedFrames;
            int framesAfterThisSegment = _totalProcessedFrames + segmentData.Count;
            double startY = framesBeforeThisSegment * SensorIntervalY / 1000.0;
            double endY = framesAfterThisSegment * SensorIntervalY / 1000.0;

            // 调用算法绘制结果（包含坐标信息）
            Stopwatch drawWatch = Stopwatch.StartNew();
            CustomAlgo?.CvDrawResult(measureResult, startY, endY);
            drawWatch.Stop();
            double drawTime = drawWatch.Elapsed.TotalMilliseconds;

            // 先展示结果（优先响应）
            Stopwatch showWatch = Stopwatch.StartNew();
            PublishResult(measureResult);
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("SegmentProcessed");
            showWatch.Stop();
            double showTime = showWatch.Elapsed.TotalMilliseconds;

            totalWatch.Stop();  // 完整处理计时结束
            Console.WriteLine($"[KBTScraper] 分段 {segmentData.Count}帧, 算法: {algoTime:F0}ms, 绘制: {drawTime:F0}ms, 展示: {showTime:F0}ms, 完整: {totalWatch.Elapsed.TotalMilliseconds:F0}ms, 缺陷: {defectCount}");
            Logs.LogInfo($"分段处理完成，帧数: {segmentData.Count}, 算法: {algoTime:F2}ms, 绘制: {drawTime:F2}ms, 展示: {showTime:F2}ms, 完整: {totalWatch.Elapsed.TotalMilliseconds:F2}ms, 缺陷: {defectCount}");

            // 异步保存（不阻塞主流程）
            SaveResultImageAsync(measureResult.GrayImage, $"result_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            SaveDefectDataAsync(measureResult.Defects);
        }

        /// <summary>
        /// 保存传感器图像
        /// </summary>
        private void SaveSensorImage(List<List<float[]>> allDates)
        {
            try
            {
                var rootPath = string.IsNullOrEmpty(OtherConfig.SavePath) ? @"D:\KBTScraper_results" : OtherConfig.SavePath;
                rootPath = Path.Combine(rootPath, string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToString("yyyyMMdd_HHmmss") : StartTime);

                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                string[] directories = Directory.GetDirectories(rootPath);
                string folderIndex = directories.Length.ToString("D2");

                HObject grayImg = new HObject();
                HObject depthImg = new HObject();
                Common_Algorithm algo = new Common_Algorithm();
                algo.ConvertListToHObject(allDates[0], ImageType.Gray, out grayImg);
                algo.ConvertListToHObject(allDates[1], ImageType.Depth, out depthImg);

                string grayfilepath = Path.Combine(rootPath, folderIndex, "gray", "1_tif.tif");
                string depthfilepath = Path.Combine(rootPath, folderIndex, "depth", "1_tif.tif");

                string? dir = Path.GetDirectoryName(grayfilepath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                dir = Path.GetDirectoryName(depthfilepath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                HOperatorSet.WriteImage(grayImg, "tiff", 0, grayfilepath);
                HOperatorSet.WriteImage(depthImg, "tiff", 0, depthfilepath);
                
                Logs.LogInfo($"原始图像已保存: {grayfilepath}");
            }
            catch (Exception ex)
            {
                Logs.LogInfo($"保存图像失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置采集
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), "触发重置采集数据", ThreadOption.BackgroundThread)]
        public void Reset(string order)
        {
            if (order != "Reset") return;

            Logs.LogInfo("重置：停止采集并释放资源");

            // 停止后台采集循环
            _collectCts?.Cancel();
            try
            {
                _collectTask?.Wait(1000);
            }
            catch (Exception) { }

            // 停止后台处理循环
            _processingCts?.Cancel();
            try
            {
                _processingTask?.Wait(1000);
            }
            catch (Exception) { }

            // 清空缓冲和队列
            _distanceBuffer.Clear();
            _accumulatedFrameCount = 0;
            
            lock (_queueLock)
            {
                _processingQueue.Clear();
            }

            // 停止传感器
            SltModel?.StopCollect();
            CustomAlgo?.Dispose();
            Logs.LogInfo("重置完成");
        }

        /// <summary>
        /// 转换传感器数据为算法输入格式
        /// </summary>
        public List<List<float[]>> ConvertToMeasurePara(List<MeasureData> listMeasureData)
        {
            List<float[]> grayDataList = new List<float[]>();
            List<float[]> heightDataList = new List<float[]>();

            if (listMeasureData != null)
            {
                foreach (var measureData in listMeasureData)
                {
                    // 添加完整  的数据验证
                    if (measureData?.AreaData == null || measureData.AreaData.Count < 2)
                    {
                        Logs.LogWarning($"跳过无效数据：AreaData为空或数量不足");
                        continue;
                    }

                    // 验证数据数组不为空
                    if (measureData.AreaData[0] == null || measureData.AreaData[1] == null)
                    {
                        Logs.LogWarning($"跳过空数据数组");
                        continue;
                    }

                    try
                    {
                        // AreaData[0] = 高度数据，AreaData[1] = 灰度数据
                        float[] heightArray = measureData.AreaData[0].Select(x => x * 10).ToArray();
                        heightDataList.Add(heightArray);

                        float[] grayArray = measureData.AreaData[1];
                        grayDataList.Add(grayArray);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"数据转换异常：{ex.Message}");
                        continue;
                    }
                }
            }

            Logs.LogInfo($"数据转换完成：灰度数据 {grayDataList.Count} 行，高度数据 {heightDataList.Count} 行");
            return new List<List<float[]>> { grayDataList, heightDataList };
        }

        /// <summary>
        /// 处理单张图像的公共方法
        /// </summary>
        private KBTGDJC_MeasureResult ProcessSingleImage(Mat grayImage, Mat heightImage, double startY = 0, double endY = 0)
        {
            // 图像转换
            List<float[]> grayData = Common_Algorithm.ConvertMatToList(grayImage);
            List<float[]> heightData = Common_Algorithm.ConvertMatToList(heightImage);

            // 调用算法
            MeasureParam.ProductNum = OtherConfig.ProductNum;
            MeasureParam.BatchNum = OtherConfig.BatchNum;
            MeasureParam.IsScanEnd = false;
            CustomAlgo?.Process(grayData, heightData, MeasureParam);

            // 获取并绘制结果
            KBTGDJC_MeasureResult measureResult = CustomAlgo?.GetDefectsResult() ?? new KBTGDJC_MeasureResult();
            CustomAlgo?.CvDrawResult(measureResult, startY, endY);

            return measureResult;
        }

        /// <summary>
        /// 发布结果到UI的公共方法
        /// </summary>
        private void PublishResult(KBTGDJC_MeasureResult measureResult)
        {
            // 发布结果事件
            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTGDJC_MeasureResult", measureResult));

            // 更新显示
            processedData = new ProcessedData();
            processedData.SetMemoryPara("KBTGDJC_MeasureResult", measureResult);

            if (measureResult.GrayImage != null && !measureResult.GrayImage.Empty())
            {
                processedData.Gray = measureResult.GrayImage.Clone();
                PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
            }
        }

        /// <summary>
        /// 异步保存结果图片的公共方法
        /// </summary>
        private void SaveResultImageAsync(Mat resultImage, string fileName)
        {
            if (!OtherConfig.IsSaveImage || resultImage == null || resultImage.Empty())
                return;

            Mat imageToSave = resultImage.Clone();
            Task.Run(() =>
            {
                try
                {
                    string dstDir = string.IsNullOrEmpty(OtherConfig.SavePath) ? @"D:\KBTScraper_results" : OtherConfig.SavePath;
                    Directory.CreateDirectory(dstDir);
                    string outputPath = Path.Combine(dstDir, fileName);
                    Cv2.ImWrite(outputPath, imageToSave);
                    Logs.LogInfo($"异步保存：结果图片已保存 {outputPath}");
                    imageToSave.Dispose();
                }
                catch (Exception ex)
                {
                    Logs.LogError($"异步保存结果图片失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 异步保存缺陷数据的公共方法
        /// </summary>
        private void SaveDefectDataAsync(List<DefectResult> defects)
        {
            if (!OtherConfig.IsSaveCSV || defects == null || defects.Count == 0)
                return;

            var defectsToSave = defects.ToList();
            Task.Run(() =>
            {
                try
                {
                    string csvPath = string.IsNullOrEmpty(OtherConfig.SaveCSVPath) ? @"D:\KBTScraper_data" : OtherConfig.SaveCSVPath;
                    bool success = DefectDataHelper.SaveDefectData(defectsToSave, csvPath, productNum: OtherConfig.ProductNum, batchNum: OtherConfig.BatchNum);
                    if (success)
                        Logs.LogInfo($"异步保存：缺陷数据已保存，数量 {defectsToSave.Count}");
                    else
                        Logs.LogError("异步保存：缺陷数据保存失败");
                }
                catch (Exception ex)
                {
                    Logs.LogError($"异步保存CSV失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 向PLC写入缺陷信号
        /// </summary>
        /// <param name="hasDefect">是否有缺陷</param>
        private void WritePLCDefectSignal(bool hasDefect)
        {
            if (_curPLC == null)
            {
                Logs.LogWarning("PLC未初始化，无法写入缺陷信号");
                return;
            }

            try
            {
                var param = new ReeYin_V.Hardware.PLC.Models.PLCParaInfoModel
                {
                    PLCAddress = "2206",
                    ParaType = ReeYin_V.Hardware.PLC.Models.EnumParaInfoModelParaType.Bool,
                    ParaValue = hasDefect
                };

                bool success = _curPLC.WritePLCPara(param);
                if (success)
                {
                    Logs.LogInfo($"向PLC地址2206写入{hasDefect}成功");
                }
                else
                {
                    Logs.LogError($"向PLC地址2206写入{hasDefect}失败");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"写入PLC缺陷信号异常: {ex.Message}");
            }
        }
        #endregion
    }
}

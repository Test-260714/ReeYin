using Custom.KCJC.Models.ALGO;
using Custom.KCJC.Views;
using Custom.KCJC.Models.StandardPlate;
using Dm.util;
using HalconDotNet;
using LogicalTool.Loop.Models;
using Newtonsoft.Json;
using OpenCvSharp;
using Prism.Events;
using ReeYin.Hardware.Sensor.Hyperson;
using ReeYin.Hardware.Sensor.Hyperson.API;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Recipe;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI.Controls.Loading;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.Models
{
    [Serializable]
    public partial class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        private int myVar1;
        private int myVar2;
        private int myVar3;

        [JsonIgnore]
        private int myVar;

        public int MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }



        public string SltModelName;

        [JsonIgnore]
        KCJC0_Algorithm CustomAlgo;

        ProcessedData processedData = new ProcessedData();

        private string StartTime;

        // 刻点标准片每次 StandardFilmTrrigerDispose 跑一次算法，满 3 次后合并显示。
        private readonly List<KCJC0_StandardPlateAlgorithm.KCJC0_StandardPlateMeasureResult> _standardFilmPointResults = new List<KCJC0_StandardPlateAlgorithm.KCJC0_StandardPlateMeasureResult>();
        private readonly List<Mat> _standardFilmPointImages = new List<Mat>();
        private readonly object _standardFilmTrrigerDisposeLock = new object();
        private int _standardFilmPointStartCollectCount = 0;
        private int _standardFilmPointDisposeCollectCount = 0;
        private int _currentConvexActualSegmentIndex = 0;
        private int _currentConvexExpectedLoopCount = 1;

        private void ClearStandardFilmPointCache()
        {
            foreach (var image in _standardFilmPointImages)
            {
                image?.Dispose();
            }
            _standardFilmPointImages.Clear();
            _standardFilmPointResults.Clear();
            _standardFilmPointStartCollectCount = 0;
            _standardFilmPointDisposeCollectCount = 0;
        }

        /// <summary>
        /// 转换点云图的方法
        /// </summary>
        private DepthMapToCloudUm depthMapToCloudUm = null;

        #region 传感器状态实时监控（温度 + 帧率）
        private System.Timers.Timer _sensorStatusTimer;

        private void StartSensorStatusMonitor()
        {
            if (_sensorStatusTimer != null) return;
            _sensorStatusTimer = new System.Timers.Timer(120000); // 120 秒刷新一次
            _sensorStatusTimer.Elapsed += OnSensorStatusTimerElapsed;
            _sensorStatusTimer.AutoReset = true;
            _sensorStatusTimer.Start();
        }

        private void StopSensorStatusMonitor()
        {
            if (_sensorStatusTimer == null) return;
            _sensorStatusTimer.Stop();
            _sensorStatusTimer.Elapsed -= OnSensorStatusTimerElapsed;
            _sensorStatusTimer.Dispose();
            _sensorStatusTimer = null;
        }

        private void OnSensorStatusTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (SltModel is not HypersenSensor hyperson) return;
            if (!LCFDevice.LCF_IsConnect(hyperson.ControlerHandle)) return;

            var ret = LCFDevice.LCF_GetDeviceStatisticInfo(hyperson.ControlerHandle, out LCF_DeviceStatisticInfo_t info);
            if (ret == LCF_StatusTypeDef.LCF_Status_Succeed)
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] 传感器状态 => " +
                    $"采样帧率: {info.calculateFrameRate}fps  " +
                    $"相机帧率: {info.cameraFrameRate}fps  " +
                    $"传输帧率: {info.transmitFrameRate}fps  " +
                    $"| CPU: {info.cpu_temp}°C  " +
                    $"GPU: {info.gpu_temp}°C  " +
                    $"相机(CMOS): {info.camera_temp}°C  " +
                    $"采集卡: {info.grabber_card_temp}°C  " +
                    $"光源: {info.lightTemp}°C");
            }
        }
        #endregion

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
        /// 选择取图方式（0：指定图像，1：文件获取，2：传感器采集，3：PLC写入）
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
        private KCJC0_MeasureParam _measureParam = new KCJC0_MeasureParam();

        public KCJC0_MeasureParam MeasureParam
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
 
        #endregion

        #region Constructor
        public SensorDataCollectionModel()
        {
            //PrismProvider.ProjectManager.SltCurSolutionItem.NodeCaches

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
                // 重新收集当前代码标记的配方参数，避免旧缓存导致切换配方后新增参数不生效。
                RecipeParams = new ObservableCollection<RecipeParamInfo>(RecipeParamService.GetMarkedParams(this));
                base.LoadKeyParam();
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
            ReleaseCustomAlgo();
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

                var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();

                Models = Param.Models;
                Logs.LogInfo($"获取到数据为{Models.Count}个");
                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((order) =>
                {
                    if (!order.contains("StartTime")) return;


                    StartTime = order.Split('@')[1];
                    Logs.LogInfo($"获取到开始时间为：{StartTime}");
                }, ThreadOption.BackgroundThread);

                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Subscribe((order) =>
                {
                    if (order != $"ClearStandardFilmPointCache@{OtherConfig?.CalibrationNodeSerial}") return;

                    lock (_standardFilmTrrigerDisposeLock)
                    {
                        ClearStandardFilmPointCache();
                    }
                    Logs.LogInfo("开始新的标准片流程，已清空刻点三段缓存。");
                }, ThreadOption.PublisherThread);

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial.ToString("D3")}-传感器设置",
                    ViewName = "OtherConfigView"
                });

                //plc启动复位流程：写M904=True触发，等待M101=True完成，期间弹窗提示，超时则提示但继续初始化。
                var plcConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
                var curPLC = plcConfig?.Models?.FirstOrDefault();
                if (curPLC?.Config?.IsConnected != true)
                {
                    Logs.LogWarning("启动复位失败：PLC未连接，地址：M904");
                    IsOnceInit = true;
                    return true;
                }

                bool startupResetFinished = false;
                PrismProvider.Dispatcher.Invoke(() =>
                {
                    bool allowClose = false;
                    var statusText = new System.Windows.Controls.TextBlock
                    {
                        Text = "PLC启动复位中，请稍候...",
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    var resetContent = new System.Windows.Controls.StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    resetContent.Children.Add(statusText);

                    var resetWindow = new System.Windows.Window
                    {
                        Title = "启动复位",
                        Width = 360,
                        Height = 140,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        ShowInTaskbar = false,
                        WindowStyle = WindowStyle.None,
                        Content = new System.Windows.Controls.Border
                        {
                            Padding = new Thickness(30),
                            Child = resetContent
                        },
                        Owner = Application.Current?.MainWindow
                    };

                    resetWindow.Closing += (sender, args) =>
                    {
                        if (!allowClose)
                        {
                            args.Cancel = true;
                        }
                    };

                    resetWindow.Loaded += async (sender, args) =>
                    {
                        await Task.Run(() =>
                        {
                            Logs.LogInfo("启动复位开始：写入M904=True，等待M101=True");
                            if (!WritePlcValue("M904", EnumParaInfoModelParaType.Bool, true, "启动复位"))
                            {
                                PrismProvider.Dispatcher.Invoke(() => statusText.Text = "PLC启动复位写入失败");
                                return true;
                            }

                            DateTime startTime = DateTime.Now;
                            while ((DateTime.Now - startTime).TotalSeconds < 120)
                            {
                                var resetDoneParam = new PLCParaInfoModel
                                {
                                    PLCAddress = "M101",
                                    ParaType = EnumParaInfoModelParaType.Bool,
                                    ParaDescription = "启动复位完成状态"
                                };

                                if (curPLC.ReadPLCPara(resetDoneParam))
                                {
                                    bool isDone = resetDoneParam.ParaValue is bool boolValue && boolValue;
                                    if (!isDone && bool.TryParse(resetDoneParam.ParaValue?.ToString(), out bool parsedValue))
                                    {
                                        isDone = parsedValue;
                                    }

                                    if (isDone)
                                    {
                                        Logs.LogInfo("启动复位完成，地址：M101");
                                        startupResetFinished = true;
                                        return true;
                                    }
                                }

                                System.Threading.Thread.Sleep(200);
                            }

                            Logs.LogWarning("启动复位等待超时，地址：M101");
                            PrismProvider.Dispatcher.Invoke(() => statusText.Text = "PLC启动复位等待超时");
                            return true;
                        });

                        allowClose = true;
                        if (startupResetFinished)
                        {
                            statusText.Text = "PLC启动复位完成";
                        }
                        await Task.Delay(800);
                        resetWindow.Close();
                    };

                    resetWindow.ShowDialog();
                });

                if (!startupResetFinished)
                {
                    Logs.LogWarning("启动复位未完成，用户关闭提示后继续初始化。");
                }
                ////以上plc复位流程完成
                // 启动后按当前配方下发一次参数，保证未切换配方直接运行时硬件参数也是当前配方。
                Task.Run(() =>
                {
                    LoadKeyParam();
                    ApplyRecipeParams();
                });

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

        private void ReleaseCustomAlgo()
        {
            if (CustomAlgo == null)
            {
                return;
            }

            try
            {
                CustomAlgo.Dispose();
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
            finally
            {
                CustomAlgo = null;
            }
        }

        private KCJC0_Algorithm RecreateCustomAlgo()
        {
            SyncMeasureParamByLimit();
            ReleaseCustomAlgo();
            CustomAlgo = KCJC0_AlgorithmService.CreateAlgorithm(MeasureParam);
            return CustomAlgo;
        }

        private bool IsCustomAlgoMatched()
        {
            if (CustomAlgo == null || MeasureParam == null)
            {
                return false;
            }

            return MeasureParam.AlgorithmType switch
            {
                0 => CustomAlgo is KCJC0_AlgorithmMeasureLine,
                1 => CustomAlgo is KCJC0_AlgorithmMeasurePoint,
                2 => CustomAlgo is KCJC0_AlgorithmMeasureLinePoint,
                _ => CustomAlgo is KCJC0_AlgorithmMeasurePoint
            };
        }

        private KCJC0_Algorithm EnsureCustomAlgo()
        {
            SyncMeasureParamByLimit();
            if (!IsCustomAlgoMatched())
            {
                // 检测方式切换后必须重建算法实例，否则会继续跑上一次缓存的算法。
                return RecreateCustomAlgo();
            }

            return CustomAlgo;
        }

        // 规格限录入在“其他”页，算法仍然只使用现有标准值字段。
        private static double GetAverageValue(double lowerLimit, double upperLimit)
        {
            return (lowerLimit + upperLimit) / 2.0;
        }

        private static void NormalizeLimitPair(ref double lowerLimit, ref double upperLimit, double fallbackValue)
        {
            if (lowerLimit <= 0 && upperLimit <= 0)
            {
                lowerLimit = fallbackValue;
                upperLimit = fallbackValue;
            }
            else if (lowerLimit <= 0)
            {
                lowerLimit = upperLimit > 0 ? upperLimit : fallbackValue;
            }
            else if (upperLimit <= 0)
            {
                upperLimit = lowerLimit > 0 ? lowerLimit : fallbackValue;
            }

            if (upperLimit < lowerLimit)
            {
                (lowerLimit, upperLimit) = (upperLimit, lowerLimit);
            }
        }

        public void SyncMeasureParamByLimit()
        {
            if (MeasureParam == null || OtherConfig == null)
                return;

            double lineDepthLower = OtherConfig.EtchingLineDepthLowerLimit;
            double lineDepthUpper = OtherConfig.EtchingLineDepthUpperLimit;
            NormalizeLimitPair(ref lineDepthLower, ref lineDepthUpper, MeasureParam.StandardEtchingLineDepthReal);
            OtherConfig.EtchingLineDepthLowerLimit = lineDepthLower;
            OtherConfig.EtchingLineDepthUpperLimit = lineDepthUpper;
            MeasureParam.StandardEtchingLineDepthReal = GetAverageValue(lineDepthLower, lineDepthUpper);

            double lineWidthLower = OtherConfig.EtchingLineWidthLowerLimit;
            double lineWidthUpper = OtherConfig.EtchingLineWidthUpperLimit;
            NormalizeLimitPair(ref lineWidthLower, ref lineWidthUpper, MeasureParam.StandardEtchingLineWidthReal);
            OtherConfig.EtchingLineWidthLowerLimit = lineWidthLower;
            OtherConfig.EtchingLineWidthUpperLimit = lineWidthUpper;
            MeasureParam.StandardEtchingLineWidthReal = GetAverageValue(lineWidthLower, lineWidthUpper);

            double pointDistLower = OtherConfig.EtchingPointDistLowerLimit;
            double pointDistUpper = OtherConfig.EtchingPointDistUpperLimit;
            NormalizeLimitPair(ref pointDistLower, ref pointDistUpper, MeasureParam.StandardEtchingPointDistReal);
            OtherConfig.EtchingPointDistLowerLimit = pointDistLower;
            OtherConfig.EtchingPointDistUpperLimit = pointDistUpper;
            MeasureParam.StandardEtchingPointDistReal = GetAverageValue(pointDistLower, pointDistUpper);

            double pointHeightLower = OtherConfig.PointHeightLowerLimit;
            double pointHeightUpper = OtherConfig.PointHeightUpperLimit;
            NormalizeLimitPair(ref pointHeightLower, ref pointHeightUpper, MeasureParam.StandardPointHeight);
            OtherConfig.PointHeightLowerLimit = pointHeightLower;
            OtherConfig.PointHeightUpperLimit = pointHeightUpper;
            MeasureParam.StandardPointHeight = GetAverageValue(pointHeightLower, pointHeightUpper);

            double pointDiameterLower = OtherConfig.PointDiameterLowerLimit;
            double pointDiameterUpper = OtherConfig.PointDiameterUpperLimit;
            NormalizeLimitPair(ref pointDiameterLower, ref pointDiameterUpper, MeasureParam.StandardPointRadius * 2.0);
            OtherConfig.PointDiameterLowerLimit = pointDiameterLower;
            OtherConfig.PointDiameterUpperLimit = pointDiameterUpper;
            MeasureParam.StandardPointRadius = GetAverageValue(pointDiameterLower, pointDiameterUpper) / 2.0;
        }

        private OtherConfigModel CreateJudgeConfigSnapshot()
        {
            return new OtherConfigModel
            {
                ProductModelName = OtherConfig.ProductModelName,
                WorkpieceName = OtherConfig.WorkpieceName,
                BatchNo = OtherConfig.BatchNo,
                Workshop = OtherConfig.Workshop,
                ProcessName = OtherConfig.ProcessName,
                ReportType = OtherConfig.ReportType,
                ShiftName = OtherConfig.ShiftName,
                MachineNo = OtherConfig.MachineNo,
                Tester = OtherConfig.Tester,
                CalibrationNodeSerial = OtherConfig.CalibrationNodeSerial,
                EtchingLineDepthLowerLimit = OtherConfig.EtchingLineDepthLowerLimit,
                EtchingLineDepthUpperLimit = OtherConfig.EtchingLineDepthUpperLimit,
                EtchingLineWidthLowerLimit = OtherConfig.EtchingLineWidthLowerLimit,
                EtchingLineWidthUpperLimit = OtherConfig.EtchingLineWidthUpperLimit,
                EtchingPointDistLowerLimit = OtherConfig.EtchingPointDistLowerLimit,
                EtchingPointDistUpperLimit = OtherConfig.EtchingPointDistUpperLimit,
                PointHeightLowerLimit = OtherConfig.PointHeightLowerLimit,
                PointHeightUpperLimit = OtherConfig.PointHeightUpperLimit,
                PointDiameterLowerLimit = OtherConfig.PointDiameterLowerLimit,
                PointDiameterUpperLimit = OtherConfig.PointDiameterUpperLimit
            };
        }

        private void ResetConvexRunTracking()
        {
            _currentConvexActualSegmentIndex = 0;
            _currentConvexExpectedLoopCount = 1;
        }

        private int ResolveConfiguredLoopCount()
        {
            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.NodeParamCaches == null)
                return 1;

            LoopModel loopModel = solutionItem.NodeParamCaches.Values.OfType<LoopModel>().FirstOrDefault();
            if (loopModel == null || loopModel.LoopNum <= 0)
                return 1;

            return loopModel.LoopNum;
        }

        private void AttachCommonResultMetadata(ProcessedData data)
        {
            data.SetMemoryPara("SaveDatasPath", OtherConfig.SaveDatasPath);
            data.SetMemoryPara("IsSaveDatas", OtherConfig.IsSaveDatas);
            data.SetMemoryPara("IsUploadPublicDisk", OtherConfig.IsUploadPublicDisk);
            data.SetMemoryPara("PublicDiskPath", OtherConfig.PublicDiskPath);
            data.SetMemoryPara("IsUploadSummaryPublicDisk", OtherConfig.IsUploadSummaryPublicDisk);
            data.SetMemoryPara("SummaryPublicDiskPath", OtherConfig.SummaryPublicDiskPath);
            data.SetMemoryPara("ProductModelName", OtherConfig.ProductModelName);
            data.SetMemoryPara("WorkpieceName", OtherConfig.WorkpieceName);
            data.SetMemoryPara("BatchNo", OtherConfig.BatchNo);
            data.SetMemoryPara("Workshop", OtherConfig.Workshop);
            data.SetMemoryPara("ProcessName", OtherConfig.ProcessName);
            data.SetMemoryPara("ReportType", OtherConfig.ReportType);
            data.SetMemoryPara("ShiftName", OtherConfig.ShiftName);
            data.SetMemoryPara("MachineNo", OtherConfig.MachineNo);
            data.SetMemoryPara("Tester", OtherConfig.Tester);
            data.SetMemoryPara("SummaryTestItems", OtherConfig.SummaryTestItems);
        }

        private void AttachConvexRunMetadata(ProcessedData data)
        {
            if (_currentConvexActualSegmentIndex == 0)
            {
                _currentConvexExpectedLoopCount = ResolveConfiguredLoopCount();
            }

            _currentConvexActualSegmentIndex++;

            int actualLoopCount = Math.Max(_currentConvexExpectedLoopCount, 1);
            int displaySegmentCount = Math.Min(actualLoopCount, 3);
            bool runCompleted = _currentConvexActualSegmentIndex >= actualLoopCount;

            data.SetMemoryPara("KCJC0_ConvexActualSegmentIndex", _currentConvexActualSegmentIndex);
            data.SetMemoryPara("KCJC0_ConvexDisplaySegmentCount", displaySegmentCount);
            data.SetMemoryPara("KCJC0_ConvexActualLoopCount", actualLoopCount);
            data.SetMemoryPara("KCJC0_ConvexRunCompleted", runCompleted);

            if (runCompleted)
            {
                ResetConvexRunTracking();
            }
        }

        /// <summary>
        /// 统一写PLC，避免三个地址各写一套重复代码
        /// </summary>
        private bool WritePlcValue(string plcAddress, EnumParaInfoModelParaType paraType, object paraValue, string describe)
        {
            try
            {
                var plcConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
                var curPLC = plcConfig?.Models?.FirstOrDefault();

                if (curPLC == null)
                {
                    Logs.LogWarning($"未找到PLC设备，无法写入{describe}");
                    return false;
                }

                var param = new PLCParaInfoModel
                {
                    PLCAddress = plcAddress,
                    ParaType = paraType,
                    ParaValue = paraValue,
                    ParaDescription = describe
                };

                bool result = curPLC.WritePLCPara(param);
                if (result)
                {
                    Logs.LogInfo($"{describe}写入成功，地址：{plcAddress}，值：{paraValue}");
                }
                else
                {
                    Logs.LogWarning($"{describe}写入失败，地址：{plcAddress}，值：{paraValue}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
        }

        /// <summary>
        /// 将总次数写入PLC的HD31，类型为Short
        /// </summary>
        public bool WriteTotalCountToPlc()
        {
            return WritePlcValue("HD31", EnumParaInfoModelParaType.Short, OtherConfig.PlcTotalCount, "总次数");
        }

        /// <summary>
        /// 将复位次数写入PLC的HD30，类型为Short
        /// </summary>
        public bool WriteResetCountToPlc()
        {
            return WritePlcValue("HD30", EnumParaInfoModelParaType.Short, OtherConfig.PlcResetCount, "复位次数");
        }

        /// <summary>
        /// 将材料长度写入PLC的HD32，类型为Float
        /// </summary>
        public bool WriteMaterialLengthToPlc()
        {
            return WritePlcValue("HD32", EnumParaInfoModelParaType.Float, OtherConfig.PlcMaterialLength, "材料长度");
        }

        /// <summary>
        /// 应用当前配方参数：传感器、PLC、算法和规格限。
        /// </summary>
        private bool ApplyRecipeParams()
        {
            var currentRecipe = RecipeParamService.LoadRecipeConfig()?.CurrentRecipe;
            bool applyResult = true;
            Logs.LogInfo($"开始应用配方参数：节点={Serial:D3}，配方={currentRecipe?.Name ?? "空"}，配方启用={currentRecipe?.IsEnabled == true}，传感器={SltModelName}");

            bool IsRecipeParamEnabled(string path)
            {
                if (currentRecipe == null || !currentRecipe.IsEnabled || string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                string recipeKey = Serial >= 0 ? $"{Serial:D3}:{path}" : path;
                var parameter = currentRecipe.FindParameter(recipeKey);
                if (parameter != null)
                {
                    return parameter.IsEnable;
                }

                parameter = currentRecipe.Groups?
                    .Where(group => group.Serial == Serial)
                    .SelectMany(group => group.Parameters)
                    .FirstOrDefault(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));

                return parameter?.IsEnable == true;
            }

            void LogRecipeParam(string describe, bool enabled, object value)
            {
                Logs.LogInfo($"配方参数检查：{describe}，启用={enabled}，当前值={value}");
            }

            var hypersenSensor = Models?.FirstOrDefault(c => c.NickName == SltModelName) as HypersenSensor;
            bool exposureTimeEnabled = IsRecipeParamEnabled("OtherConfig.ExposureTime");
            bool intensityEnabled = IsRecipeParamEnabled("OtherConfig.Intensity");
            bool gainEnabled = IsRecipeParamEnabled("OtherConfig.GainIndex");
            bool encoderDivisionEnabled = IsRecipeParamEnabled("OtherConfig.EncoderDivision");
            bool detectPosition1Enabled = IsRecipeParamEnabled("OtherConfig.DetectPosition1");
            bool detectPosition2Enabled = IsRecipeParamEnabled("OtherConfig.DetectPosition2");
            bool runSpeedEnabled = IsRecipeParamEnabled("OtherConfig.RunSpeed");
            bool algorithmTypeEnabled = IsRecipeParamEnabled("MeasureParam.AlgorithmType");
            bool collectIntervalYEnabled = IsRecipeParamEnabled("MeasureParam.CollectIntervalY");
            bool calibIntervalYEnabled = IsRecipeParamEnabled("CalibMeasureParam.CalibIntervalY");
            bool limitEnabled = IsRecipeParamEnabled("OtherConfig.EtchingLineDepthLowerLimit")
                || IsRecipeParamEnabled("OtherConfig.EtchingLineDepthUpperLimit")
                || IsRecipeParamEnabled("OtherConfig.EtchingLineWidthLowerLimit")
                || IsRecipeParamEnabled("OtherConfig.EtchingLineWidthUpperLimit")
                || IsRecipeParamEnabled("OtherConfig.EtchingPointDistLowerLimit")
                || IsRecipeParamEnabled("OtherConfig.EtchingPointDistUpperLimit")
                || IsRecipeParamEnabled("OtherConfig.PointHeightLowerLimit")
                || IsRecipeParamEnabled("OtherConfig.PointHeightUpperLimit")
                || IsRecipeParamEnabled("OtherConfig.PointDiameterLowerLimit")
                || IsRecipeParamEnabled("OtherConfig.PointDiameterUpperLimit");

            LogRecipeParam("曝光时间", exposureTimeEnabled, OtherConfig.ExposureTime);
            LogRecipeParam("光强", intensityEnabled, OtherConfig.Intensity);
            LogRecipeParam("增益", gainEnabled, OtherConfig.GainIndex);
            LogRecipeParam("固定预分频值", encoderDivisionEnabled, OtherConfig.EncoderDivision);
            LogRecipeParam("检测位1", detectPosition1Enabled, OtherConfig.DetectPosition1);
            LogRecipeParam("检测位2", detectPosition2Enabled, OtherConfig.DetectPosition2);
            LogRecipeParam("运行速度", runSpeedEnabled, OtherConfig.RunSpeed);
            LogRecipeParam("检测方式", algorithmTypeEnabled, MeasureParam.AlgorithmType);
            LogRecipeParam("采集Y方向像素当量", collectIntervalYEnabled, MeasureParam.IntervalY);
            LogRecipeParam("标定Y方向像素当量", calibIntervalYEnabled, CalibMeasureParam.IntervalY);
            Logs.LogInfo($"配方规格限检查：启用={limitEnabled}，槽深={OtherConfig.EtchingLineDepthLowerLimit}-{OtherConfig.EtchingLineDepthUpperLimit}，槽宽={OtherConfig.EtchingLineWidthLowerLimit}-{OtherConfig.EtchingLineWidthUpperLimit}，槽间距={OtherConfig.EtchingPointDistLowerLimit}-{OtherConfig.EtchingPointDistUpperLimit}，凸点高度={OtherConfig.PointHeightLowerLimit}-{OtherConfig.PointHeightUpperLimit}，凸点直径={OtherConfig.PointDiameterLowerLimit}-{OtherConfig.PointDiameterUpperLimit}");

            if (hypersenSensor == null && (exposureTimeEnabled || intensityEnabled || gainEnabled || encoderDivisionEnabled))
            {
                Logs.LogWarning($"应用配方传感器参数失败：未找到海伯森传感器，传感器名称={SltModelName}");
            }

            // 配方传感器参数：曝光时间。
            if (hypersenSensor != null && exposureTimeEnabled && OtherConfig.ExposureTime > 0)
            {
                bool exposureRet = hypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_EXPOSURE_TIME, OtherConfig.ExposureTime);
                if (exposureRet)
                {
                    Logs.LogInfo($"海伯森参数写入成功：曝光时间={OtherConfig.ExposureTime}");
                }
                else
                {
                    Logs.LogWarning($"海伯森参数写入失败：曝光时间={OtherConfig.ExposureTime}");
                }
                hypersenSensor.CmosConfig.ExposureTime = OtherConfig.ExposureTime;
                hypersenSensor.Config.ExposureTime = OtherConfig.ExposureTime;
            }
            else if (exposureTimeEnabled)
            {
                Logs.LogInfo($"跳过曝光时间写入：值无效或传感器不存在，值={OtherConfig.ExposureTime}");
            }

            // 配方传感器参数：光强。
            if (hypersenSensor != null && intensityEnabled && OtherConfig.Intensity > 0)
            {
                // 光强按传感器调试页面的写法用 float 下发，数值仍使用配方中的百分比。
                float intensity = OtherConfig.Intensity;
                bool intensityRet = hypersenSensor.SetSingleParam("float", LCF_ParameterDefine.PARAM_LIGHT_INTENSITY, intensity);
                if (intensityRet)
                {
                    Logs.LogInfo($"海伯森参数写入成功：光强={intensity.ToString("f1")}%");
                }
                else
                {
                    Logs.LogWarning($"海伯森参数写入失败：光强={intensity.ToString("f1")}%");
                }
                hypersenSensor.CmosConfig.Intensity = OtherConfig.Intensity;
                hypersenSensor.CmosConfig.IntensityPercentage = intensity.ToString("f1") + "%";
            }
            else if (intensityEnabled)
            {
                Logs.LogInfo($"跳过光强写入：值无效或传感器不存在，值={OtherConfig.Intensity}");
            }

            // 配方传感器参数：增益。
            if (hypersenSensor != null && gainEnabled)
            {
                // 增益枚举值不是设备值，必须转换为 1/2/4 后再写入。
                int gainValue = OtherConfig.GainIndex switch
                {
                    Gain.GAIN_1 => (int)LCF_CameraGain_t.LCF_2K_Gain_1,
                    Gain.GAIN_2 => (int)LCF_CameraGain_t.LCF_2K_Gain_2,
                    Gain.GAIN_4 => (int)LCF_CameraGain_t.LCF_2K_Gain_4,
                    _ => (int)LCF_CameraGain_t.LCF_2K_Gain_1
                };
                bool gainRet = hypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_GAIN, gainValue);
                if (gainRet)
                {
                    Logs.LogInfo($"海伯森参数写入成功：增益={OtherConfig.GainIndex}");
                }
                else
                {
                    Logs.LogWarning($"海伯森参数写入失败：增益={OtherConfig.GainIndex}");
                }
                hypersenSensor.CmosConfig.GainIndex = OtherConfig.GainIndex;
            }
            else if (gainEnabled)
            {
                Logs.LogInfo($"跳过增益写入：传感器不存在，值={OtherConfig.GainIndex}");
            }

            // 配方传感器参数：固定预分频值。
            if (hypersenSensor != null && encoderDivisionEnabled && OtherConfig.EncoderDivision > 0)
            {
                // 固定预分频值按传感器调试页面的写法用 PARAM_ENCODER_DIV 下发。
                bool encoderRet = hypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_ENCODER_DIV, OtherConfig.EncoderDivision);
                if (encoderRet)
                {
                    Logs.LogInfo($"海伯森参数写入成功：固定预分频值={OtherConfig.EncoderDivision}");
                }
                else
                {
                    Logs.LogWarning($"海伯森参数写入失败：固定预分频值={OtherConfig.EncoderDivision}");
                }
                hypersenSensor.TriggerConfig.NUDEncoderDivision = OtherConfig.EncoderDivision;
                hypersenSensor.Config.EncoderDivision = OtherConfig.EncoderDivision;
            }
            else if (encoderDivisionEnabled)
            {
                Logs.LogInfo($"跳过固定预分频值写入：值无效或传感器不存在，值={OtherConfig.EncoderDivision}");
            }

            // 配方PLC参数：检测位1。
            if (detectPosition1Enabled)
            {
                applyResult &= WritePlcValue("HD20204", EnumParaInfoModelParaType.Float, OtherConfig.DetectPosition1, "检测位1");
            }

            // 配方PLC参数：检测位2。
            if (detectPosition2Enabled)
            {
                applyResult &= WritePlcValue("HD20206", EnumParaInfoModelParaType.Float, OtherConfig.DetectPosition2, "检测位2");
            }

            // 配方PLC参数：运行速度。
            if (runSpeedEnabled)
            {
                applyResult &= WritePlcValue("HD1218", EnumParaInfoModelParaType.Float, OtherConfig.RunSpeed, "运行速度");
            }

            // 配方采集算法参数：检测方式和采集Y方向像素当量。
            if (algorithmTypeEnabled || collectIntervalYEnabled)
            {
                EnsureCustomAlgo();
                Logs.LogInfo($"采集算法参数已应用：检测方式={MeasureParam.AlgorithmType}，采集Y方向像素当量={MeasureParam.IntervalY}");
            }

            // 配方标定算法参数：标定Y方向像素当量。
            if (calibIntervalYEnabled)
            {
                // 标准片处理线程会自行按最新 CalibMeasureParam 重建 CalibALGO；这里不能释放，避免采集下一段时把正在处理的算法对象 Dispose 掉。
                Logs.LogInfo($"标定算法参数已应用：标定Y方向像素当量={CalibMeasureParam.IntervalY}");
            }

            // 配方规格限参数：上下限同步到算法判定参数。
            if (limitEnabled)
            {
                SyncMeasureParamByLimit();
                Logs.LogInfo($"规格限已同步到算法参数：槽深标准={MeasureParam.StandardEtchingLineDepthReal}，槽宽标准={MeasureParam.StandardEtchingLineWidthReal}，槽间距标准={MeasureParam.StandardEtchingPointDistReal}，凸点高度标准={MeasureParam.StandardPointHeight}，凸点半径标准={MeasureParam.StandardPointRadius}");
            }

            Logs.LogInfo($"结束应用配方参数：结果={applyResult}");
            return applyResult;
        }

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
                LoadKeyParam();
                try
                {
                    // 开始采集会在 TrrigerStartCollect 后台线程里应用配方，避免同一次采集前重复写传感器和PLC。
                    // 文件取图是离线算法验证，不能写PLC和传感器，避免未连接硬件时卡住界面。
                    bool recipeParamApplyResult = SltTriggerPicIndex == 1 || (SltTriggerPicIndex == 2 && StartCollect) ? true : ApplyRecipeParams();
                    if (SltTriggerPicIndex == 1)
                    {
                        SyncMeasureParamByLimit();
                    }
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
                                    KCJC0_Algorithm customAlgo = null;
                                    Mat grayImage = null;
                                    Mat heightImage = null;

                                    try
                                    {
                                        //KCJC0_MeasureParam measureParam = new KCJC0_MeasureParam();
                                        KCJC0_MeasureResult measureResult = new KCJC0_MeasureResult();
                                        customAlgo = KCJC0_AlgorithmService.CreateAlgorithm(MeasureParam);

                                        string grayImagePath = FilePath + "\\gray\\1_tif.tif";
                                        string heightImagePath = FilePath + "\\depth\\1_tif_0.tif";

                                        grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                                        heightImage = Cv2.ImRead(heightImagePath, ImreadModes.Unchanged);

                                        List<float[]> grayDate = customAlgo.ConvertMatToList(grayImage);
                                        List<float[]> heightDate = customAlgo.ConvertMatToList(heightImage);


                                        for (int i = 0; i < 1; i++)
                                        {
                                            ////measureParam = new KCJC0_MeasureParam();
                                            measureResult = new KCJC0_MeasureResult();

                                            DateTime startTime = DateTime.Now;

                                            measureResult = customAlgo.Process(grayDate, heightDate, MeasureParam);

                                            DateTime endTime = DateTime.Now;
                                            TimeSpan elapsed = endTime - startTime;
                                            Console.WriteLine($"Elapsed time: {elapsed.TotalMilliseconds} ms");
                                            //AppendConvexResultsAsSingleRow("C:\\Users\\19765\\Desktop\\新建文件夹\\Test2.csv", measureResult.ConvexResultsList);

                                            //绘制结果
                                            Mat mat = null;
                                            try
                                            {
                                                mat = customAlgo.CvDrawResult(measureResult, true);

                                                processedData = new ProcessedData();
                                                processedData.SetMemoryPara("KCJC0_MeasureResult", measureResult);
                                                processedData.SetMemoryPara("KCJC0_MeasureParam", MeasureParam);
                                                processedData.SetMemoryPara("KCJC0_JudgeOtherConfig", CreateJudgeConfigSnapshot());
                                                AttachCommonResultMetadata(processedData);
                                                processedData.Gray = mat.Clone();
                                                //通知数据更新
                                                PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                                                //推送检测结果
                                                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KCJC0_MeasureResult", measureResult));
                                                Common_Algorithm.ConvertListToMat(measureResult.DepthMap.ToList(), ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage1);
                                                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("LC3DShow", new ImageResultsDisplay { HeightImage = heightImage1 }));

                                                
                                                //Cv2.ImWrite("D:\\workspace\\2_ReechiImageAlgorithm\\CPlusPlus\\01_Development\\01_Products\\09_Custom.KCJC\\Custom.KCJC\\Custom.KCJC\\bin\\Debug\\result\\00.png", mat);
                                            }
                                            finally
                                            {
                                                mat?.Dispose();
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logs.LogError(ex);
                                    }
                                    finally
                                    {
                                        grayImage?.Dispose();
                                        heightImage?.Dispose();
                                        customAlgo?.Dispose();

                                        PrismProvider.Dispatcher.BeginInvoke(() =>
                                        {
                                            loading.Close();
                                        });
                                    }
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
                        //PLC写入
                        case 3:
                            {
                                // 配方PLC参数已在 ApplyRecipeParams 中写入，这里只写非配方PLC参数。
                                if (!WriteTotalCountToPlc())
                                {
                                    return NodeStatus.Error;
                                }

                                if (!WriteResetCountToPlc())
                                {
                                    return NodeStatus.Error;
                                }

                                if (!WriteMaterialLengthToPlc())
                                {
                                    return NodeStatus.Error;
                                }

                                if (!recipeParamApplyResult)
                                {
                                    return NodeStatus.Error;
                                }
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


        private static string EscapeCsvField(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
                return "\"" + input.Replace("\"", "\"\"") + "\"";

            return input;
        }

        public void AppendConvexResultsAsSingleRow(string csvFilePath, List<KCJC0_ConvexConcaveResult> results)
        {
            if (results == null || results.Count == 0)
                return;

            bool writeHeader = !File.Exists(csvFilePath);
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // 构建数据行
            var fields = new List<string> { EscapeCsvField(now) };

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];

                // 处理 Radius
                string radiusStr = item.Radius.ToString("R", CultureInfo.InvariantCulture);

                // 处理 HeightDiff（支持 Infinity / NaN）
                string heightStr = double.IsInfinity(item.HeightDiff)
                    ? (item.HeightDiff > 0 ? "Infinity" : "-Infinity")
                    : double.IsNaN(item.HeightDiff)
                        ? "NaN"
                        : item.HeightDiff.ToString("R", CultureInfo.InvariantCulture);

                fields.Add(EscapeCsvField(radiusStr));
                fields.Add(EscapeCsvField(heightStr));
            }

            // 写入文件
            using var writer = new StreamWriter(csvFilePath, append: true, Encoding.UTF8);

            // 写表头（仅首次）
            if (writeHeader)
            {
                var headerFields = new List<string> { "写入时间" };
                for (int i = 1; i <= results.Count; i++)
                {
                    headerFields.Add($"{i}-Radius");
                    headerFields.Add($"{i}-HeightDiff");
                }
                writer.WriteLine(string.Join(",", headerFields));
            }

            // 写数据行
            writer.WriteLine(string.Join(",", fields));
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

            LoadKeyParam();
            //ApplyRecipeParams();//开始采集事件是后台线程，采集前应用配方参数，避免标定按钮在UI线程卡死。(这加了plc模块开始不用调用RecipeParamTrrigerApply)
            EnsureCustomAlgo();//保证用的是当前检测方式，缓存会导致改了没用
            SltModel.StartCollect();
            if (OtherConfig?.CalibrationNodeSerial == 8)
            {
                _standardFilmPointStartCollectCount++;
                Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：刻点标准片开始第{_standardFilmPointStartCollectCount}次采集");
            }
            StartSensorStatusMonitor();
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发停止采集", ThreadOption.BackgroundThread)]
        public void TrrigerStopCollect(string order)
        {
            if (order != "TrrigerStopCollect") return;

            EnsureCustomAlgo();
            StopSensorStatusMonitor();
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
                try
                {
                    CustomAlgo.ConvertListToHObject(allDates[0], KCJC0_Algorithm.ImageType.Gray, out grayImg);
                    CustomAlgo.ConvertListToHObject(allDates[1], KCJC0_Algorithm.ImageType.Depth, out depthImg);
                    CustomAlgo.ConvertListToHObject(allDates[2], KCJC0_Algorithm.ImageType.Depth, out depth1Img);

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

                    CustomAlgo.Process(allDates[0], allDates[1],MeasureParam);
                }
                finally
                {
                    grayImg.Dispose();
                    depthImg.Dispose();
                    depth1Img.Dispose();
                }
            }
            else
            {
                CustomAlgo.Process(allDates[0], allDates[1], MeasureParam);
            }
        }

        /// <summary>
        /// 标准片流程触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始处理数据出图", ThreadOption.BackgroundThread)]
        public void StandardFilmTrrigerDispose(string order)
        {
            if (order != "StandardFilmTrrigerDispose") return;

            lock (_standardFilmTrrigerDisposeLock)
            {
                    StopSensorStatusMonitor();
                    CalibMeasureParam.GrooveStandardRefs = GrooveStandardRefs != null
                        ? new List<GrooveStandardRef>(GrooveStandardRefs)
                        : new List<GrooveStandardRef>();
                    CalibMeasureParam.BumpStandardRefs = BumpStandardRefs != null
                        ? new List<BumpStandardRef>(BumpStandardRefs)
                        : new List<BumpStandardRef>();
                    CalibMeasureParam.AlgorithmType = OtherConfig.CalibrationNodeSerial switch
                    {
                        5 => 0,
                        8 => 1,
                        11 => 2,
                        _ => CalibMeasureParam.AlgorithmType
                    };
                    CalibALGO?.Dispose();
                    CalibALGO = KCJC0_StandardPlateAlgorithmService.CreateAlgorithm(CalibMeasureParam);

                    bool isPointStandardFilm = CalibMeasureParam.AlgorithmType == 1;
                    if (isPointStandardFilm && _standardFilmPointResults.Count >= 3)
                    {
                        ClearStandardFilmPointCache();
                    }

                    if (isPointStandardFilm)
                    {
                        // 刻点标准片一段结果必须对应一次新的开始采集，避免重复 Dispose 被当成下一段。
                        if (_standardFilmPointDisposeCollectCount >= _standardFilmPointStartCollectCount)
                        {
                            Logs.LogInfo("刻点标准片没有新的开始采集，本次重复处理触发已忽略。");
                            return;
                        }
                        _standardFilmPointDisposeCollectCount++;
                    }

                    Logs.LogInfo(isPointStandardFilm
                        ? $"{DateTime.Now.ToString("HH-mm-ss")}：开始获取刻点标准片第{_standardFilmPointResults.Count + 1}段数据..."
                        : "标准片流程停止采集并直接获取传感器数据...");
                SltModel.StopCollect();
                List<MeasureData> standardFilmMeasureDatas = SltModel.ReceiveSensorData();
                if (isPointStandardFilm)
                {
                    Logs.LogInfo($"{DateTime.Now.ToString("HH-mm-ss")}：结束获取刻点标准片数据...");
                    Logs.LogInfo($"刻点标准片第{_standardFilmPointResults.Count + 1}段读取数据行数：{standardFilmMeasureDatas?.Count ?? 0}");
                }
                List<List<float[]>> allDates = ConvertToMeasurePara(standardFilmMeasureDatas);

                if (OtherConfig.IsSaveImage)
                {
                    depthMapToCloudUm = new DepthMapToCloudUm(CalibMeasureParam.IntervalX, CalibMeasureParam.IntervalY, CalibMeasureParam.IntervalZ);
                    Logs.LogInfo("开始存图单边图片！");
                    var rootPath = OtherConfig.SavePath + $"\\{StartTime}";

                    if (!Directory.Exists(rootPath))
                    {
                        Directory.CreateDirectory(rootPath);
                    }
                    string[] directories = Directory.GetDirectories(rootPath);

                    HObject grayImg = new HObject();
                    HObject depthImg = new HObject();
                    try
                    {
                        CalibALGO.ConvertListToHObject(allDates[0], KCJC0_StandardPlateAlgorithm.ImageType.Gray, out grayImg);
                        CalibALGO.ConvertListToHObject(allDates[1], KCJC0_StandardPlateAlgorithm.ImageType.Depth, out depthImg);

                        string grayfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\gray\\1_tif.tif";
                        string depthfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_0.tif";
                        string? dir = Path.GetDirectoryName(grayfilepath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        dir = Path.GetDirectoryName(depthfilepath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        HOperatorSet.WriteImage(grayImg, "tiff", 0, grayfilepath);
                        HOperatorSet.WriteImage(depthImg, "tiff", 0, depthfilepath);
                    }
                    finally
                    {
                        grayImg.Dispose();
                        depthImg.Dispose();
                    }
                }

                var measureResult = CalibALGO.Process(allDates[0], allDates[1], CalibMeasureParam);
                Mat mat = null;
                try
                {
                    mat = CalibALGO.CvDrawResult(measureResult, true);

                    if (isPointStandardFilm)
                    {
                        _standardFilmPointResults.Add(measureResult);
                        if (mat != null && !mat.Empty())
                        {
                            _standardFilmPointImages.Add(mat.Clone());
                        }

                        Logs.LogInfo($"刻点标准片第{_standardFilmPointResults.Count}段处理完成，点数：{measureResult?.BumpResults?.Count ?? 0}");

                        if (_standardFilmPointResults.Count < 3)
                        {
                            return;
                        }

                        var resultAll = new KCJC0_StandardPlateAlgorithm.KCJC0_StandardPlateMeasureResult();
                        var lastResult = _standardFilmPointResults.LastOrDefault();
                        if (lastResult != null)
                        {
                            resultAll.DepthMap = lastResult.DepthMap;
                            resultAll.DepthMapMinValue = lastResult.DepthMapMinValue;
                            resultAll.DepthMapMaxValue = lastResult.DepthMapMaxValue;
                            resultAll.ImageScaleW = lastResult.ImageScaleW;
                            resultAll.ImageScaleH = lastResult.ImageScaleH;
                            resultAll.FinalHeightThreshold = lastResult.FinalHeightThreshold;
                        }

                        resultAll.IsOK = _standardFilmPointResults.All(x => x?.IsOK == true);
                        resultAll.BumpResults = _standardFilmPointResults
                            .Where(x => x?.BumpResults != null)
                            .SelectMany(x => x.BumpResults)
                            .ToList();
                        resultAll.BumpHeightPhysicalList = _standardFilmPointResults
                            .Where(x => x?.BumpHeightPhysicalList != null)
                            .SelectMany(x => x.BumpHeightPhysicalList)
                            .ToArray();
                        resultAll.BumpDiameterPhysicalList = _standardFilmPointResults
                            .Where(x => x?.BumpDiameterPhysicalList != null)
                            .SelectMany(x => x.BumpDiameterPhysicalList)
                            .ToArray();

                        var heights = resultAll.BumpHeightPhysicalList.Where(x => x >= 0).ToArray();
                        var diameters = resultAll.BumpDiameterPhysicalList.Where(x => x >= 0).ToArray();
                        resultAll.BumpHeightPhysicalMean = heights.Length > 0 ? heights.Average() : -1;
                        resultAll.BumpHeightPhysicalMax = heights.Length > 0 ? heights.Max() : -1;
                        resultAll.BumpHeightPhysicalMin = heights.Length > 0 ? heights.Min() : -1;
                        resultAll.BumpDiameterPhysicalMean = diameters.Length > 0 ? diameters.Average() : -1;
                        resultAll.BumpDiameterPhysicalMax = diameters.Length > 0 ? diameters.Max() : -1;
                        resultAll.BumpDiameterPhysicalMin = diameters.Length > 0 ? diameters.Min() : -1;

                        measureResult = resultAll;
                    }

                    processedData = new ProcessedData();
                    processedData.SetMemoryPara("KCJC0_StandardPlateMeasureResult", measureResult);
                    processedData.SetMemoryPara("KCJC0_StandardPlateMeasureParam", CalibMeasureParam);
                    if (isPointStandardFilm)
                    {
                        // 刻点标准片三段结果已合并成一个结果，这里额外记录每段点数，界面用它还原“区1点1、区2点1...”。
                        processedData.SetMemoryPara("KCJC0_StandardPlateBumpSegmentCounts", _standardFilmPointResults
                            .Select(x => Math.Min(x?.BumpHeightPhysicalList?.Length ?? 0, x?.BumpDiameterPhysicalList?.Length ?? 0))
                            .ToArray());
                    }
                    processedData.SetMemoryPara("KCJC0_JudgeOtherConfig", CreateJudgeConfigSnapshot());
                    AttachCommonResultMetadata(processedData);

                    Mat showMat = null;
                    try
                    {
                        if (isPointStandardFilm)
                        {
                            if (_standardFilmPointImages.Count > 0)
                            {
                                showMat = new Mat();
                                Cv2.VConcat(_standardFilmPointImages.ToArray(), showMat);
                                processedData.Gray = showMat.Clone();
                            }
                        }
                        else if (mat != null && !mat.Empty())
                        {
                            processedData.Gray = mat.Clone();
                        }
                    }
                    finally
                    {
                        showMat?.Dispose();
                    }

                    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KCJC0_StandardPlateMeasureResult", measureResult));
                    if (measureResult.DepthMap != null && measureResult.DepthMap.Length > 0)
                    {
                        Common_Algorithm.ConvertListToMat(measureResult.DepthMap.ToList(), ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage1);
                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("LC3DShow", new ImageResultsDisplay { HeightImage = heightImage1 }));
                    }

                    if (isPointStandardFilm)
                    {
                        ClearStandardFilmPointCache();
                    }
                }
                finally
                {
                    mat?.Dispose();
                }

            }
        }

        /// <summary>
        /// PLC触发应用当前配方和传感器参数（plc不用调，暂时没用）
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发应用配方参数", ThreadOption.BackgroundThread)]
        public void RecipeParamTrrigerApply(string order)
        {
            if (order != "RecipeParamTrrigerApply") return;

            LoadKeyParam();
            ApplyRecipeParams();//应用当前配方参数
        }

        /// <summary>
        /// 配方切换后重新加载当前配方参数。
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "配方切换后重新加载关键参数", ThreadOption.BackgroundThread)]
        public void RecipeSwitchedLoadKeyParam(string order)
        {
            if (order != "RecipeSwitched") return;

            // 解决方案切换后旧节点订阅仍可能残留，这里只允许当前运行缓存里的本节点响应。
            string cacheKey = Serial.ToString("D3");
            if (PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches?.TryGetValue(cacheKey, out object currentParam) != true ||
                !ReferenceEquals(currentParam, this))
            {
                return;
            }

            LoadKeyParam();
            ApplyRecipeParams();
        }


        /// <summary>
        /// 触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发开始处理数据出图", ThreadOption.BackgroundThread)]
        public void TrrigerDispose(string order)
        {
            if (order != "TrrigerDispose") return;

            try
            {
                EnsureCustomAlgo();
                StopSensorStatusMonitor();
                Logs.LogInfo("传感器停止采集并处理数据");
                SltModel.StopCollect();
                Logs.LogInfo("开始处理数据...");

                List<MeasureData> sensorDatas = SltModel.ReceiveSensorData();
                Logs.LogInfo($"正常流程读取数据行数：{sensorDatas?.Count ?? 0}");
                List<List<float[]>> allDates = ConvertToMeasurePara(sensorDatas);
                int grayRowCount = allDates.Count > 0 ? allDates[0]?.Count ?? 0 : 0;
                int heightRowCount = allDates.Count > 1 ? allDates[1]?.Count ?? 0 : 0;
                Logs.LogInfo($"正常流程转换数据行数：灰度={grayRowCount}，高度={heightRowCount}");
                if (grayRowCount == 0 || heightRowCount == 0)
                {
                    Logs.LogError($"正常流程传感器数据为空，已停止处理和发布结果。原始行数={sensorDatas?.Count ?? 0}，灰度行数={grayRowCount}，高度行数={heightRowCount}");
                    return;
                }

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
                    try
                    {
                        CustomAlgo.ConvertListToHObject(allDates[0], KCJC0_Algorithm.ImageType.Gray, out grayImg);
                        CustomAlgo.ConvertListToHObject(allDates[1], KCJC0_Algorithm.ImageType.Depth, out depthImg);


                        string grayfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\gray\\1_tif.tif";
                        string depthfilepath = $"{rootPath}\\{directories.Length.ToString("D2")}\\depth\\1_tif_0.tif";
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

                        HOperatorSet.WriteImage(grayImg, "tiff", 0, grayfilepath);
                        HOperatorSet.WriteImage(depthImg, "tiff", 0, depthfilepath);

                        var measureResult = CustomAlgo.Process(allDates[0], allDates[1], MeasureParam);

                        processedData = new ProcessedData();
                        processedData.SetMemoryPara("KCJC0_MeasureResult", measureResult);
                        processedData.SetMemoryPara("KCJC0_MeasureParam", MeasureParam);
                        processedData.SetMemoryPara("KCJC0_JudgeOtherConfig", CreateJudgeConfigSnapshot());
                        AttachCommonResultMetadata(processedData);
                        AttachConvexRunMetadata(processedData);
                        //推送检测结果
                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KCJC0_MeasureResult", measureResult));

                        //先绘制结果，在通知更新
                        Mat mat = null;
                        try
                        {
                            mat = CustomAlgo.CvDrawResult(measureResult, true);
                            processedData.Gray = mat.Clone();
                        }
                        finally
                        {
                            mat?.Dispose();
                        }
                        //通知数据更新
                        PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KCJC0_MeasureResult", measureResult));
                        if (measureResult.DepthMap != null && measureResult.DepthMap.Length > 0)
                        {
                            Common_Algorithm.ConvertListToMat(measureResult.DepthMap.ToList(), ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage1);
                            PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("LC3DShow", new ImageResultsDisplay { HeightImage = heightImage1 }));
                        }
                        else
                        {
                            Logs.LogWarning("正常流程深度图为空，跳过3D显示。");
                        }
                    }
                    finally
                    {
                        grayImg.Dispose();
                        depthImg.Dispose();
                    }
                }
                else
                {
                    RecreateCustomAlgo();
                    var measureResult = CustomAlgo.Process(allDates[0], allDates[1], MeasureParam);

                    //KCJC0_MeasureResult measureResult = CustomAlgo.GetMeasureResult();
                    processedData = new ProcessedData();
                    processedData.SetMemoryPara("KCJC0_MeasureResult", measureResult);
                    processedData.SetMemoryPara("KCJC0_MeasureParam", MeasureParam);
                    processedData.SetMemoryPara("KCJC0_JudgeOtherConfig", CreateJudgeConfigSnapshot());
                    AttachCommonResultMetadata(processedData);
                    AttachConvexRunMetadata(processedData);
                    Mat mat = null;
                    try
                    {
                        mat = CustomAlgo.CvDrawResult(measureResult, true);

                        //depth.Dispose();
                        processedData.Gray = mat.Clone();
                    }
                    finally
                    {
                        mat?.Dispose();
                    }
                    //通知数据更新
                    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KCJC0_MeasureResult", measureResult));
                    if (measureResult.DepthMap != null && measureResult.DepthMap.Length > 0)
                    {
                        Common_Algorithm.ConvertListToMat(measureResult.DepthMap.ToList(), ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage1);
                        PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("LC3DShow", new ImageResultsDisplay { HeightImage = heightImage1 }));
                    }
                    else
                    {
                        Logs.LogWarning("正常流程深度图为空，跳过3D显示。");
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }

            //PrismProvider.WorkStatusManager.SwitchWorkStatus(WorkStatus.Idle);
        }

        /// <summary>
        /// 触发重置
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), "触发重置采集数据", ThreadOption.BackgroundThread)]
        public void Reset(string order)
        {
            if (order != "Reset") return;

            StopSensorStatusMonitor();
            Logs.LogInfo("开始停止采集并释放之前的资源");
            SltModel.StopCollect();
            ReleaseCustomAlgo();
            ResetConvexRunTracking();
            ClearStandardFilmPointCache();
            Logs.LogInfo("完成...");
        }

        public List<List<float[]>> ConvertToMeasurePara(List<MeasureData> listMeasureData)
        {
            List<float[]> grayDataList = new List<float[]>();
            List<float[]> height1DataList = new List<float[]>();
            //List<float[]> height2DataList = new List<float[]>();

            if (listMeasureData != null)
            {
                foreach (var measureData in listMeasureData)
                {
                    if (measureData.AreaData != null && measureData.AreaData.Count >= 2)
                    {
                        //float[] heightArray = measureData.AreaData[0];
                        float[] height1Array = measureData.AreaData[0].Select(x => x).ToArray();
                        height1DataList.Add(height1Array);

                        //float[] height2Array = measureData.AreaData[1].Select(x => x * 10).ToArray();
                        //height2DataList.Add(height2Array);

                        float[] grayArray = measureData.AreaData[1];
                        grayDataList.Add(grayArray);
                    }
                }
            }

            return [grayDataList, height1DataList/*, height2DataList*/];
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

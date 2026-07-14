﻿using Custom.EVEMFDJC.ViewModels;
using HalconDotNet;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Custom.EVEMFDJC.Models.EVEMFDJC0_Algorithm;
using ReeYin.Hardware.Sensor.ChroCodile;

namespace Custom.EVEMFDJC.Models
{
    [Serializable]
    public class EveSensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        public string SltModelName;

        [JsonIgnore]
        EVEMFDJC0_Algorithm MFDCustomAlgo;

        /// <summary>
        /// 当前指定图像的缓存，用于拖动蓝色圆ROI后继续同步坐标。
        /// </summary>
        [JsonIgnore]
        private HImage _specifiedCircleImage;

        /// <summary>
        /// 内部批量更新圆参数时使用，避免属性 setter 重复触发 ROI 刷新。
        /// </summary>
        [JsonIgnore]
        private bool _isUpdatingCircleParam;

        /// <summary>
        /// 上一次初始化蓝色圆 ROI 的图像路径，用于判断是否换图。
        /// </summary>
        [JsonIgnore]
        private string _lastCircleImagePath = string.Empty;

        /// <summary>
        /// 上一次初始化蓝色圆 ROI 的图像宽度，用于判断图像尺寸是否变化。
        /// </summary>
        [JsonIgnore]
        private int _lastCircleImageWidth;

        /// <summary>
        /// 上一次初始化蓝色圆 ROI 的图像高度，用于判断图像尺寸是否变化。
        /// </summary>
        [JsonIgnore]
        private int _lastCircleImageHeight;

        /// <summary>
        /// 当前节点的蓝色圆ROI名称，用 Serial 区分不同节点，避免 ROI 名称冲突。
        /// </summary>
        private string SpecifiedCircleRoiName => $"{Serial}_SpecifiedCircle";

        [JsonIgnore]
        private int _triggerCount;
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
        private List<ImageData> image = new List<ImageData>();
        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public List<ImageData> Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
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
        private MFDJC0_MeasureParam _algorithm = new MFDJC0_MeasureParam();

        public MFDJC0_MeasureParam Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 图像窗口中的 ROI 集合，用来维护当前蓝色圆 ROI。
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        /// <summary>
        /// 当前蓝色圆 ROI 参数；页面圆心和半径会同步到这里。
        /// </summary>
        [JsonIgnore]
        public ROICircle InitCircle { get; set; } = new ROICircle();


        /// <summary>
        /// 页面配置的蓝色圆 ROI 圆心 X。
        /// </summary>
        [JsonIgnore]
        private double _initCircleCenterX = 50.0;

        /// <summary>
        /// 页面配置的蓝色圆 ROI 圆心 X。
        /// </summary>
        public double InitCircleCenterX
        {
            get { return _initCircleCenterX; }
            set
            {
                _initCircleCenterX = value;
                RaisePropertyChanged();
                if (!_isUpdatingCircleParam) InitCircleChanged();
            }
        }

        /// <summary>
        /// 页面配置的蓝色圆 ROI 圆心 Y。
        /// </summary>
        [JsonIgnore]
        private double _initCircleCenterY = 50.0;

        /// <summary>
        /// 页面配置的蓝色圆 ROI 圆心 Y。
        /// </summary>
        public double InitCircleCenterY
        {
            get { return _initCircleCenterY; }
            set
            {
                _initCircleCenterY = value;
                RaisePropertyChanged();
                if (!_isUpdatingCircleParam) InitCircleChanged();
            }
        }

        /// <summary>
        /// 页面配置的蓝色圆 ROI 半径。
        /// </summary>
        [JsonIgnore]
        private double _initCircleRadius = 40.0;

        /// <summary>
        /// 页面配置的蓝色圆 ROI 半径。
        /// </summary>
        public double InitCircleRadius
        {
            get { return _initCircleRadius; }
            set
            {
                _initCircleRadius = value;
                RaisePropertyChanged();
                if (!_isUpdatingCircleParam) InitCircleChanged();
            }
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

        private string _sensorNote;
        /// <summary>
        /// 传感器备注
        /// </summary>
        public string SensorNote
        {
            get { return _sensorNote; }
            set { _sensorNote = value; RaisePropertyChanged(); }
        }

        private int _exposureTime = 5;
        /// <summary>
        /// 曝光时间
        /// </summary>
        public int ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }


        private EveAlarmLogModel _eveAlarmLogModel = new EveAlarmLogModel();
        /// <summary>
        /// 报警信息页面页面内容
        /// </summary>
        public EveAlarmLogModel EveAlarmLogModel
        {
            get { return _eveAlarmLogModel; }
            set { _eveAlarmLogModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private FlowList _selectFlow;
        /// <summary>
        /// 流程参数
        /// </summary>
        [OutputParam("SelectFlow", "触发流程选择")]
        public FlowList SelectFlow
        {
            get { return _selectFlow; }
            set { _selectFlow = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public EveSensorDataCollectionModel()
        {

        }

        ~EveSensorDataCollectionModel()
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
                InitOutputParamResource(Guid);
                return base.LoadKeyParam();
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
            if (mWindowH?.hControl != null)
                mWindowH.hControl.MouseUp -= HControl_MouseUp;
            _specifiedCircleImage?.Dispose();
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
                var Param = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
                
                LoadAlgorithm();

                Models = Param.Models;

                TriggerModuleRun += () =>
                {
                    return ExecuteModule().Result;
                };


                //订阅组件间消息
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Subscribe((order) =>
                {
                    if (order.Item1 == "PLC")
                    {
                        MFDCustomAlgo.tempx.Add(Convert.ToSingle(order.Item2));
                    }
                }, ThreadOption.UIThread);


                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"{Serial}-其他配置",
                    ViewName = "OtherConfigView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"报警信息",
                    ViewName = "EveAlarmLogView"
                });

                PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
                {
                    Type = DynamicViewType.NodeMap,
                    NodeSerial = Serial,
                    DisplayName = $"PLC操作",
                    ViewName = "EvePlcControlView"
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

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            LoadKeyParam();
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                NodeStatus nodeStatus = NodeStatus.Success;
                try
                {
                    switch (SltTriggerPicIndex)
                    {
                        //指定图像
                        case 0:
                            {
                                nodeStatus =  ExecuteSpecifiedImageMeasureCircle();
                            }
                            break;
                        //文件取图
                        case 1:
                            {
                                string[] subdirectories = Directory.GetDirectories(FilePath);

                                foreach (string subdirectory in subdirectories)
                                {
                                    string grayImageDir = subdirectory + "/gray";
                                    string heightImageDir = subdirectory + "/depth";

                                    string grayImagePath = FileHelper.GetImagePaths(grayImageDir)[0];
                                    string heightImagePath = FileHelper.GetImagePaths(heightImageDir)[0];

                                    Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                                    Mat heightImage = Cv2.ImRead(heightImagePath, ImreadModes.Unchanged);
                                    List<float[]> grayData = MFDCustomAlgo.ConvertMatToList(grayImage);
                                    List<float[]> heightData = MFDCustomAlgo.ConvertMatToList(heightImage);

                                    string imageName = Path.GetFileNameWithoutExtension(grayImagePath);
                                    string[] parts = imageName.Split('_');
                                    double[] values = parts.Select(part => double.Parse(part)).ToArray();
                                    if (values.Count() == 6)
                                    {
                                        Algorithm.IntervalX = values[0];
                                        Algorithm.IntervalY = values[1];
                                        Algorithm.IntervalZ = 0.1;
                                        Algorithm.MinDepth = values[2];
                                        Algorithm.MaxDepth = values[3];
                                        Algorithm.IsFlip = false;
                                        Algorithm.IsScanEnd = false;
                                        Algorithm.OffsetX = values[4];
                                        Algorithm.OffsetY = values[5];
                                    }
                                    else if (values.Count() == 7)
                                    {
                                        Algorithm.IntervalX = values[0];
                                        Algorithm.IntervalY = values[1];
                                        Algorithm.IntervalZ = values[2];
                                        Algorithm.MinDepth = values[3];
                                        Algorithm.MaxDepth = values[4];
                                        Algorithm.IsFlip = false;
                                        Algorithm.IsScanEnd = false;
                                        Algorithm.OffsetX = values[5];
                                        Algorithm.OffsetY = values[6];
                                    }

                                    MFDCustomAlgo.Process(grayData, heightData, Algorithm);

                                    if (grayImage.Data != IntPtr.Zero)
                                        grayImage.Dispose();
                                    if (heightImage.Data != IntPtr.Zero)
                                        heightImage.Dispose();
                                }

                                MFDCustomAlgo.GetFeature();

                                MFDJC0_MeasureResult measureResult = MFDCustomAlgo.GetMeasureResult();
                                MFDCustomAlgo.CvDrawResult(measureResult, true);

                                ProcessedData processedData = new ProcessedData();
                                processedData.SetMemoryPara("MFDJC0_MeasureResult", CreateMeasureResultSnapshot(measureResult));

                                //通知数据更新
                                PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                                Task.Delay(400).Wait();
                                MFDCustomAlgo.Dispose();
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
                    Logs.LogError(ex.Message);
                    nodeStatus = NodeStatus.Error;
                    return nodeStatus;
                }

                #region 输出
                //执行后对输出参数重新赋值
                RefreshOutputParams();

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                #endregion

                return nodeStatus;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            Logs.LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：取图模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// 指定图像模式执行入口：读取图像、显示图像、初始化蓝色圆 ROI，并同步 ROI 输出值。
        /// </summary>
        private NodeStatus ExecuteSpecifiedImageMeasureCircle()
        {
            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
            {
                Console.WriteLine($"指定图像路径无效：{FilePath}");
                return NodeStatus.Error;
            }

            HImage? tempImage = null;
            try
            {
                tempImage = new HImage(FilePath);
                if (tempImage == null || !tempImage.IsInitialized())
                {
                    Console.WriteLine($"指定图像读取失败：{FilePath}");
                    return NodeStatus.Error;
                }

                _specifiedCircleImage?.Dispose();
                _specifiedCircleImage = tempImage.Clone();

                if (mWindowH != null)
                {
                    mWindowH.HobjectToHimage(tempImage);
                    InitSpecifiedCircleRoi(tempImage);
                }

                return NodeStatus.Success;
            }
            catch (Exception ex)
            {
                Logs.LogError($"指定图像圆ROI处理失败：{ex.Message},调用堆栈:{ex.StackTrace}");
                return NodeStatus.Error;
            }
            finally
            {
                tempImage?.Dispose();
            }
        }

        /// <summary>
        /// 根据当前图像初始化或重绘蓝色圆 ROI；首次执行时按图像尺寸给默认圆。
        /// </summary>
        private void InitSpecifiedCircleRoi(HImage image)
        {
            if (image == null || !image.IsInitialized() || mWindowH == null)
                return;

            image.GetImageSize(out int imageWidth, out int imageHeight);
            bool isDefaultCircle = InitCircleCenterX == 50.0 && InitCircleCenterY == 50.0 && InitCircleRadius == 40.0;
            bool isInvalidCircle = InitCircleRadius <= 0;
            bool isImageChanged = !string.Equals(_lastCircleImagePath, FilePath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                                  || _lastCircleImageWidth != imageWidth
                                  || _lastCircleImageHeight != imageHeight;
            if (isDefaultCircle || isInvalidCircle || isImageChanged)
            {
                _isUpdatingCircleParam = true;
                InitCircleCenterX = imageWidth / 2.0;
                InitCircleCenterY = imageHeight / 2.0;
                InitCircleRadius = Math.Min(imageWidth, imageHeight) / 8.0;
                _isUpdatingCircleParam = false;

                _lastCircleImagePath = FilePath ?? string.Empty;
                _lastCircleImageWidth = imageWidth;
                _lastCircleImageHeight = imageHeight;
            }

            InitCircle.CenterX = InitCircleCenterX;
            InitCircle.CenterY = InitCircleCenterY;
            InitCircle.Radius = InitCircleRadius;
            mWindowH.WindowH.genCircle(SpecifiedCircleRoiName, InitCircle.CenterY, InitCircle.CenterX, InitCircle.Radius, ref RoiList);
            mWindowH.hControl.MouseUp -= HControl_MouseUp;
            mWindowH.hControl.MouseUp += HControl_MouseUp;
        }

        /// <summary>
        /// 鼠标松开时读取当前被拖动的蓝色圆 ROI，并同步页面参数。
        /// </summary>
        private void HControl_MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                if (mWindowH == null || _specifiedCircleImage == null || !_specifiedCircleImage.IsInitialized())
                    return;

                ROI roi = mWindowH.WindowH.smallestActiveROI(out string info, out string index);
                if (index != SpecifiedCircleRoiName || roi is not ROICircle circle)
                    return;

                _isUpdatingCircleParam = true;
                InitCircleCenterX = Math.Round(circle.CenterX, 3);
                InitCircleCenterY = Math.Round(circle.CenterY, 3);
                InitCircleRadius = Math.Round(circle.Radius, 3);
                _isUpdatingCircleParam = false;

                InitCircle.CenterX = InitCircleCenterX;
                InitCircle.CenterY = InitCircleCenterY;
                InitCircle.Radius = InitCircleRadius;
            }
            catch (Exception ex)
            {
                Logs.LogError($"拖动指定图像圆ROI失败：{ex.Message},调用堆栈:{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 页面手动修改圆心或半径时，重绘蓝色圆 ROI。
        /// </summary>
        private void InitCircleChanged()
        {
            InitCircle.CenterX = InitCircleCenterX;
            InitCircle.CenterY = InitCircleCenterY;
            InitCircle.Radius = InitCircleRadius;

            if (mWindowH == null || _specifiedCircleImage == null || !_specifiedCircleImage.IsInitialized())
                return;

            mWindowH.WindowH.genCircle(SpecifiedCircleRoiName, InitCircle.CenterY, InitCircle.CenterX, InitCircle.Radius, ref RoiList);

        }


        /// <summary>
        /// 将已添加到右侧输出信息表的输出参数刷新为当前模型值。
        /// </summary>
        private void RefreshOutputParams()
        {
            var outputValues = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (outputValues.TryGetValue(item.ParamName, out object value))
                    item.Value = value;
            }
        }

        /// <summary>
        /// 创建测量结果快照，避免图表线程继续引用算法内部的 Mat 等 native 资源。
        /// </summary>
        private static MFDJC0_MeasureResult CreateMeasureResultSnapshot(MFDJC0_MeasureResult source)
        {
            MFDJC0_MeasureResult snapshot = new MFDJC0_MeasureResult();
            if (source == null)
                return snapshot;

            snapshot.GrayImage?.Dispose();
            snapshot.GrayImage = CloneMat(source.GrayImage);
            snapshot.HeightImage?.Dispose();
            snapshot.HeightImage = CloneMat(source.HeightImage);
            snapshot.MinDepth = source.MinDepth;
            snapshot.MaxDepth = source.MaxDepth;
            snapshot.Warp = CloneWarpResult(source.Warp);
            snapshot.Orbit = CloneOrbitResult(source.Orbit);
            snapshot.Defects = source.Defects?.Select(CloneDefectResult).ToList() ?? new List<DefectResult>();

            return snapshot;
        }

        /// <summary>
        /// 克隆 OpenCV 图像数据，确保快照持有独立的 native 内存。
        /// </summary>
        private static Mat CloneMat(Mat source)
        {
            return source != null && source.Data != IntPtr.Zero ? source.Clone() : new Mat();
        }

        /// <summary>
        /// 复制翘钉测量结果。
        /// </summary>
        private static WarpResult CloneWarpResult(WarpResult source)
        {
            if (source == null)
                return new WarpResult();

            return new WarpResult
            {
                IsOk = source.IsOk,
                Height = source.Height,
                HighestPointRow = source.HighestPointRow,
                HighestPointCol = source.HighestPointCol,
                Polygons = source.Polygons?.ToList() ?? new List<Polygon>()
            };
        }

        /// <summary>
        /// 复制轨迹偏移测量结果。
        /// </summary>
        private static OrbitResult CloneOrbitResult(OrbitResult source)
        {
            if (source == null)
                return new OrbitResult();

            return new OrbitResult
            {
                IsOk = source.IsOk,
                NailCenterCol = source.NailCenterCol,
                NailCenterRow = source.NailCenterRow,
                NailRadius = source.NailRadius,
                OrbitCenterCol = source.OrbitCenterCol,
                OrbitCenterRow = source.OrbitCenterRow,
                OrbitRadius = source.OrbitRadius,
                Offset = source.Offset
            };
        }

        /// <summary>
        /// 复制单个瑕疵检测结果。
        /// </summary>
        private static DefectResult CloneDefectResult(DefectResult source)
        {
            if (source == null)
                return new DefectResult();

            return new DefectResult
            {
                IsOk = source.IsOk,
                InstanceId = source.InstanceId,
                Left = source.Left,
                Top = source.Top,
                Right = source.Right,
                Bottom = source.Bottom,
                DefectPolygons = source.DefectPolygons?.ToList() ?? new List<Polygon>(),
                AreaFeature = source.AreaFeature,
                LengthFeature = source.LengthFeature,
                WidthFeature = source.WidthFeature,
                DepthFeature = source.DepthFeature,
                CenterRowFeature = source.CenterRowFeature,
                CenterColFeature = source.CenterColFeature,
                Diameter = source.Diameter,
                Confidence = source.Confidence,
                ClassId = source.ClassId,
                Categories = source.Categories != null ? new Dictionary<int, string>(source.Categories) : new Dictionary<int, string>()
            };
        }
        private void ApplyChroCodileCollectDirection()
        {
            if (SltModel is not ChroCodileSensor chrocodilesensor)
                return;

            int startPosition = Math.Abs(chrocodilesensor.CurrentConfig.StartPosition);
            int endPosition = Math.Abs(chrocodilesensor.CurrentConfig.EndPosition);
            float interval = Math.Abs(chrocodilesensor.CurrentConfig.Interval);

            if (_triggerCount % 2 == 1)
            {
                chrocodilesensor.CurrentConfig.StartPosition = startPosition;
                chrocodilesensor.CurrentConfig.EndPosition = endPosition;
                chrocodilesensor.CurrentConfig.Interval = interval;
                Console.WriteLine($"ChroCodile第1段扫描参数: start={startPosition}, stop={endPosition}, interval={interval}");
                return;
            }

            chrocodilesensor.CurrentConfig.StartPosition = -startPosition;
            chrocodilesensor.CurrentConfig.EndPosition = -endPosition;
            chrocodilesensor.CurrentConfig.Interval = -interval;
            Console.WriteLine($"ChroCodile第2段扫描参数: start={-startPosition}, stop={-endPosition}, interval={-interval}");
        }

        private void ResetChroCodileCollectDirection()
        {
            if (SltModel is not ChroCodileSensor chrocodilesensor)
                return;

            chrocodilesensor.CurrentConfig.StartPosition = Math.Abs(chrocodilesensor.CurrentConfig.StartPosition);
            chrocodilesensor.CurrentConfig.EndPosition = Math.Abs(chrocodilesensor.CurrentConfig.EndPosition);
            chrocodilesensor.CurrentConfig.Interval = Math.Abs(chrocodilesensor.CurrentConfig.Interval);
            Console.WriteLine($"ChroCodile扫描参数已恢复正向: start={chrocodilesensor.CurrentConfig.StartPosition}, stop={chrocodilesensor.CurrentConfig.EndPosition}, interval={chrocodilesensor.CurrentConfig.Interval}");
        }

        /// <summary>
        /// 触发采集
        /// </summary>
        [EventSubscription(typeof(UpdateMessageEvent), Description = "触发开始采集")]
        public void TrrigerStartCollect(string order)
        {
            if (order != "TrrigerStartCollect") return;

            if (SltModel == null && SltModelName != null && SltModelName != "")
            {
                SltModel = Models.Where(c => c.NickName == SltModelName).FirstOrDefault();
            }
            _triggerCount++;
            ApplyChroCodileCollectDirection();
            Logs.LogInfo("传感器开始采集");

            SltModel.StartCollect();
            
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), Description = "触发停止采集")]
        public void TrrigerStopCollect(string order)
        {
            try
            {
                if (order != "TrrigerStopCollect") return;

                Logs.LogInfo("传感器停止采集");
                SltModel.StopCollect();
                Logs.LogInfo("开始处理数据...");
                var DataCollect = SltModel.ReceiveSensorData();


                (List<float[]> grayDate, List<float[]> heightData) = ConvertToMeasurePara(DataCollect);
                Algorithm.OffsetX = 0.0;
                Algorithm.OffsetY = 0.0;
                Algorithm.IsFlip = false;

                MFDCustomAlgo.Process(grayDate, heightData, Algorithm);
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now}:报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发处理
        /// </summary>
        /// <param name="order"></param>
        [EventSubscription(typeof(UpdateMessageEvent), Description = "触发开始处理数据出图")]
        public void TrrigerDispose(string order)
        {
            try
            {
                if (order != "TrrigerDispose") return;
                Logs.LogInfo("传感器停止采集并处理数据");
                SltModel.StopCollect();
                Logs.LogInfo("开始处理数据...");
                Logs.LogInfo("进入SltModel.ReceiveSensorData()");
                var DataCollect = SltModel.ReceiveSensorData();
                _triggerCount = 0;
                ResetChroCodileCollectDirection();
                Logs.LogInfo("结束SltModel.ReceiveSensorData()");
                Logs.LogInfo("进入ConvertToMeasurePara(DataCollect)");
                (List<float[]> grayDate, List<float[]> heightData) = ConvertToMeasurePara(DataCollect);
                Logs.LogInfo("结束ConvertToMeasurePara(DataCollect)");
                Algorithm.IsFlip = true;
                Algorithm.OffsetY = MFDCustomAlgo.tempx.Count == 4 ? (MFDCustomAlgo.tempx[0] - MFDCustomAlgo.tempx[2]) * 1000 + OtherConfig.ManualOffsetY : OtherConfig.ManualOffsetY;
                Algorithm.OffsetX = MFDCustomAlgo.tempx.Count == 4 ? (MFDCustomAlgo.tempx[1] - MFDCustomAlgo.tempx[3]) * 1000 + OtherConfig.ManualOffsetX : OtherConfig.ManualOffsetX;
                Logs.LogInfo($"进入MFDCustomAlgo.Process()");
                MFDCustomAlgo.Process(grayDate, heightData, Algorithm);
                Logs.LogInfo("结束MFDCustomAlgo.Process()");
                Logs.LogInfo($"X方向偏移：{Algorithm.OffsetX},Y方向偏移：{Algorithm.OffsetY}");
                if (OtherConfig.IsSaveImage)
                {
                    Console.WriteLine("开始存图...");
                    Logs.LogInfo("开始存图...");
                    SaveImage();
                    Logs.LogInfo("存图结束...");
                }
                Logs.LogInfo($"进入MFDCustomAlgo.GetFeature()");
                MFDCustomAlgo.GetFeature();
                Logs.LogInfo($"结束MFDCustomAlgo.GetFeature()");
                MFDJC0_MeasureResult measureResult = MFDCustomAlgo.GetMeasureResult();
                ProcessedData processedData = new ProcessedData();
                //先绘制结果，在通知更新
                Logs.LogInfo($"进入MFDCustomAlgo.CvDrawResult");
                MFDCustomAlgo.CvDrawResult(measureResult, true);
                Logs.LogInfo($"结束MFDCustomAlgo.CvDrawResult");
                Logs.LogInfo($"进入processedData.SetMemoryPara");
                processedData.SetMemoryPara("MFDJC0_MeasureResult", CreateMeasureResultSnapshot(measureResult));
                Logs.LogInfo($"结束processedData.SetMemoryPara");
                //通知数据更新
                Logs.LogInfo($"进入PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData)");
                PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);
                Logs.LogInfo($"结束PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData)");
                Logs.LogInfo($"进入MFDCustomAlgo.Dispose()");
                Task.Delay(400).Wait();
                MFDCustomAlgo.Dispose();
                Logs.LogInfo($"结束MFDCustomAlgo.Dispose()");
                Logs.LogInfo($"进入MFDCustomAlgo.tempx.Clear()");
                MFDCustomAlgo.tempx.Clear();
                Logs.LogInfo($"结束MFDCustomAlgo.tempx.Clear()");
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now}:报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
            }
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

        /// <summary>
        /// 存高度/灰度图
        /// </summary>
        public void SaveImage()
        {
            try
            {
                DateTime tmpTime = DateTime.Now;
                var imageDatas = MFDCustomAlgo.GetImageData();
                Console.WriteLine($"图片数量为：{imageDatas.Count}");

                string currentdate = tmpTime.ToString("yyyy-MM-dd");

                string groupfolder = tmpTime.ToString("yyyyMMddHHmmssfff");

                string rootPath = Path.Combine(OtherConfig.SavePath, currentdate, groupfolder);

                for (int i = 0; i < imageDatas.Count; i++)
                {
                    ImageData imageData = imageDatas[i];

                    string imageName = "";
                    if (i == 0)
                    {
                        imageName = $"{Algorithm.IntervalX.ToString("F3")}_{Algorithm.IntervalY.ToString("F3")}_" +
                        $"{Algorithm.IntervalZ.ToString("F3")}_{Algorithm.MinDepth}_{Algorithm.MaxDepth}_" +
                        $"{0}_{0}";
                    }
                    else
                    {
                        imageName = $"{Algorithm.IntervalX.ToString("F3")}_{Algorithm.IntervalY.ToString("F3")}_" +
                                           $"{Algorithm.IntervalZ.ToString("F3")}_{Algorithm.MinDepth}_{Algorithm.MaxDepth}_" +
                                           $"{(MFDCustomAlgo.tempx[1] - MFDCustomAlgo.tempx[3]) * 1000 + OtherConfig.ManualOffsetX}_{(MFDCustomAlgo.tempx[0] - MFDCustomAlgo.tempx[2]) * 1000 + OtherConfig.ManualOffsetY}";
                    }


                    string GrayImageFilePath = rootPath + $"\\{i}" + "\\gray" + $"\\{imageName}" + ".tiff";
                    DirectoryInfo directoryInfo = Directory.GetParent(GrayImageFilePath);
                    if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);

                    string HeightImageFilePath = rootPath + $"\\{i}" + "\\depth" + $"\\{imageName}" + ".tiff";
                    directoryInfo = Directory.GetParent(HeightImageFilePath);
                    if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);

                    HObject hoGrayImage = new HObject();
                    HObject hoHeightImage = new HObject();

                    HOperatorSet.MirrorImage(imageData.hoGrayImage, out hoGrayImage, "column");
                    HOperatorSet.MirrorImage(imageData.hoHeightImage, out hoHeightImage, "column");

                    HOperatorSet.WriteImage(hoGrayImage, "tiff", 0, GrayImageFilePath);
                    HOperatorSet.WriteImage(hoHeightImage, "tiff", 0, HeightImageFilePath);

                    hoGrayImage.Dispose();
                    hoHeightImage.Dispose();
                }
            }
            catch(Exception ex)
            {
                Logs.LogError($"保存图像：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据检测的密封钉有无清洗面，加载对应的算法模型
        /// </summary>
        public void LoadAlgorithm()
        {
            try
            {
                MFDJC0_MeasureParam measureparam = PrismProvider.Container.Resolve(typeof(MFDJC0_MeasureParam)) as MFDJC0_MeasureParam;

                if (Algorithm.IsWithMilling) //密封钉有清洗面
                {
                    measureparam.ModelConfigPath = "./SealingNailsSDK/models_WidthCleanSurface/model.json";
                    measureparam.ModelPath = "./SealingNailsSDK/models_WidthCleanSurface/model.kmodel";
                    measureparam.FeatureConfigPath = "./SealingNailsSDK/models_WidthCleanSurface/FeatureConfig.json";
                    measureparam.TemplateModelPath = "./SealingNailsSDK/models_WidthCleanSurface/NailCenterNCCModel";
                    measureparam.DeviceType = Algorithm.DeviceType;
                    measureparam.ConfidenceThreshold = Algorithm.ConfidenceThreshold;
                    measureparam.IoUThreshold = Algorithm.IoUThreshold;
                    measureparam.SegmentationThreshold = Algorithm.SegmentationThreshold;
                    MFDCustomAlgo = PrismProvider.Container.Resolve(typeof(EVEMFDJC0_Algorithm)) as EVEMFDJC0_Algorithm;
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}加载有清洗面算法模型成功");
                }
                else
                {
                    measureparam.ModelConfigPath = "./SealingNailsSDK/models_WidthoutCleanSurface/model.json";
                    measureparam.ModelPath = "./SealingNailsSDK/models_WidthoutCleanSurface/model.kmodel";
                    measureparam.FeatureConfigPath = "./SealingNailsSDK/models_WidthoutCleanSurface/FeatureConfig.json";
                    measureparam.TemplateModelPath = "./SealingNailsSDK/models_WidthoutCleanSurface/NailCenterNCCModel";
                    measureparam.DeviceType = Algorithm.DeviceType;
                    measureparam.ConfidenceThreshold = Algorithm.ConfidenceThreshold;
                    measureparam.IoUThreshold = Algorithm.IoUThreshold;
                    measureparam.SegmentationThreshold = Algorithm.SegmentationThreshold;
                    MFDCustomAlgo = PrismProvider.Container.Resolve(typeof(EVEMFDJC0_Algorithm)) as EVEMFDJC0_Algorithm;
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff")}加载无清洗面算法模型成功");
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"亿纬模块LoadAlgorithm()方法报错：{ex.Message}");
            }
        }

        /// <summary>
        /// 创建halcon模板
        /// </summary>
        /// <returns></returns>
        public int CreateHalconTemplate()
        {
            if (MFDCustomAlgo == null)
                LoadAlgorithm();

            return MFDCustomAlgo.CreateHalconTemplate(
                InitCircleCenterX,
                InitCircleCenterY,
                InitCircleRadius);
        }
        #endregion
    }
}

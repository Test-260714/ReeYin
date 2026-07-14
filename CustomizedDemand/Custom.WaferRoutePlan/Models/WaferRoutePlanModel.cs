using Custom.WaferRoutePlan.ViewModels;
using Dm;
using DryIoc;
using HalconDotNet;
using HandyControl.Controls;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using MathNet.Numerics.Statistics;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using ReeYin_V.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using static Custom.WaferRoutePlan.ViewModels.WaferRoutePlanViewModel;

namespace Custom.WaferRoutePlan
{
    public class WaferRoutePlanModel : ModelParamBase
    {
        #region Fields
        public List<Geometry2D> StEdPointXY = null;

        public int[] InputImageSize { get; set; }

        [OutputParam("OutputStEdPointXY", "运动轨迹")]
        public List<LocusInfo> OutputStEdPointXY = null;
        #endregion

        #region Properties
        private double _xPos;

        public double XPos
        {
            get { return _xPos; }
            set { _xPos = value; RaisePropertyChanged(); }
        }
        private double _yPos;

        public double YPos
        {
            get { return _yPos; }
            set { _yPos = value; RaisePropertyChanged(); }
        }
        private double _zPos;

        public double ZPos
        {
            get { return _zPos; }
            set { _zPos = value; RaisePropertyChanged(); }
        }

        private string _calibrationFile;
        /// <summary>
        /// 标定文件路径
        /// </summary>
        public string CalibrationFile
        {
            get { return _calibrationFile; }
            set { _calibrationFile = value; RaisePropertyChanged(); }
        }

        private string _nPointcalibrationFile;
        /// <summary>
        /// N点标定文件路径
        /// </summary>
        public string NPointCalibrationFile
        {
            get { return _nPointcalibrationFile; }
            set { _nPointcalibrationFile = value; RaisePropertyChanged(); }
        }

        private double _laserIPos;
        /// <summary>
        /// 图像激光中心点的列坐标
        /// </summary>
        public double LaserIPos
        {
            get { return _laserIPos; }
            set { _laserIPos = value; RaisePropertyChanged(); }
        }

        private double _laserJPos;
        /// <summary>
        /// 图像激光中心点的行坐标
        /// </summary>
        public double LaserJPos
        {
            get { return _laserJPos; }
            set { _laserJPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HObject _initCircle = new HObject();
        /// <summary>
        /// 输入区域1信息
        /// </summary>
        public HObject InitCircle
        {
            get { return _initCircle; }
            set { SetProperty(ref _initCircle, value); }
        }

        [JsonIgnore]
        private double _sensorInterval = 1;

        public double SensorInterval
        {
            get { return _sensorInterval; }
            set { _sensorInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HImage _previewImage = new HImage();
        [JsonIgnore]
        public HImage PreviewImage
        {
            get { return _previewImage; }
            private set
            {
                if (ReferenceEquals(_previewImage, value))
                {
                    return;
                }

                HImage previousImage = _previewImage;
                _previewImage = value;
                RaisePropertyChanged();
                previousImage?.Dispose();
            }
        }

        [JsonIgnore]
        private readonly ObservableCollection<DrawingObjectInfo> _previewDrawObjectList = new ObservableCollection<DrawingObjectInfo>();
        [JsonIgnore]
        public ObservableCollection<DrawingObjectInfo> PreviewDrawObjectList => _previewDrawObjectList;

        [JsonIgnore]
        private HObject _resultPreviewObject = new HObject();



        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value;
                RaisePropertyChanged();
                RefreshPreview();
            }
        }

        [JsonIgnore]
        private TransmitParam _inputCircle = new TransmitParam();
        /// <summary>
        /// 输入圆轮廓
        /// </summary>
        public TransmitParam InputCircle
        {
            get { return _inputCircle; }
            set
            {
                _inputCircle = value;
                RaisePropertyChanged();
                RefreshInitCircle();
            }
        }

        [JsonIgnore]
        private double[] _outIdealGetPicPos = [0, 0];
        [JsonIgnore]
        [OutputParam("outIdealGetPicPos", "理想拍照位置")]
        /// <summary>
        /// 理想拍照位置
        /// </summary>
        public double[] OutIdealGetPicPos
        {
            get { return _outIdealGetPicPos; }
            set { SetProperty(ref _outIdealGetPicPos, value); }
        }

        [JsonIgnore]
        private double[] _outLaserCenterPos = [0, 0];
        [JsonIgnore]
        [OutputParam("outLaserCenterPos", "晶圆激光中心坐标")]
        /// <summary>
        /// 晶圆激光中心坐标
        /// </summary>
        public double[] OutLaserCenterPos
        {
            get { return _outLaserCenterPos; }
            set { SetProperty(ref _outLaserCenterPos, value); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }


        private ScanTypes _currentScanType = ScanTypes.旋转线扫;
        public ScanTypes CurrentScanType
        {
            get { return _currentScanType; }
            set { SetProperty(ref _currentScanType, value); }
        }

        private ObservableCollection<ParamDefinition> _currentParamDefinitions = new ObservableCollection<ParamDefinition>();
        public ObservableCollection<ParamDefinition> CurrentParamDefinitions
        {
            get { return _currentParamDefinitions; }
            set { SetProperty(ref _currentParamDefinitions, value); }
        }


        #endregion

        #region Constructor
        public WaferRoutePlanModel()
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
                ModuleName = Serial.ToString("D3");
                if (_inputImage.Value != null)
                    (_inputImage.Value as HObject)?.Dispose();
                var temp = GetTransmitParam(InputParams, _inputImage);
                if (temp != null)
                    _inputImage.Value = new HImage((HObject)temp);

                //if (_inputCircle.Value != null)
                //    (_inputCircle.Value as HObject)?.Dispose();
                temp = GetTransmitParam(InputParams, _inputCircle);
                if (temp != null)
                    _inputCircle.Value = temp;

                RefreshInitCircle();
                RefreshPreview();
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
            ClearPreviewDrawObjects();
            PreviewImage?.Dispose();
            InitCircle?.Dispose();
            _resultPreviewObject?.Dispose();
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
                try
                {
                    #region 检测参数（对链接参数重新赋值）
                    Console.WriteLine($"开始加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    //if (!IsDebug)
                    LoadKeyParam();
                    Console.WriteLine($"结束加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    #endregion


                    if (_inputCircle.Value == null)
                        return NodeStatus.None;
                    HObject tempCiecle = (_inputCircle.Value as HObject).Clone();
                    if (tempCiecle == null || !tempCiecle.IsInitialized())
                        return NodeStatus.Error;

                    HOperatorSet.GetContourXld(tempCiecle, out HTuple CircleRows, out HTuple CircleCols);
                    double[] CircleCenterIJ = [CircleCols.DArr.Mean(), CircleRows.DArr.Mean()];

                    //执行的方法
                    RoutePlanningParam param = new RoutePlanningParam()
                    {
                        ImgGetPLCXY = [XPos, YPos],
                        //需要设计一个标定页面来计算此值
                        LaserCtIJ = [LaserIPos, LaserJPos],
                        CircleCenterIJ = CircleCenterIJ,
                        CircleEdgeCols = CircleCols.DArr,
                        CircleEdgeRows = CircleRows.DArr,
                        ScanType = CurrentScanType,

                        NPointCalibFilePath = NPointCalibrationFile,
                        CalibFilePath = CalibrationFile,
                        IndentWidth = CurrentParamDefinitions.GetValueOrDefault("边缘缩进-外扩+(mm):", 10),
                        CircleScanRadius = CurrentParamDefinitions.GetValueOrDefault("圆环扫描半径(mm):", 10),
                        PointScanInterval = CurrentParamDefinitions.GetValueOrDefault("点扫描间隔(mm):", 10),
                        PointScanTangentRadius = CurrentParamDefinitions.GetValueOrDefault("相切圆半径(mm):", 10),
                        LineScannLineNum = CurrentParamDefinitions.GetValueOrDefault("扫描行数:", 10),
                        LineScanSectorNum = CurrentParamDefinitions.GetValueOrDefault("旋转格数:", 10),
                        ImageSize = InputImageSize
                    };

                    RoutePlan_Algorithm routePlan_Algorithm = new RoutePlan_Algorithm(param);

                    bool status = routePlan_Algorithm.PlanRoute(out StEdPointXY, out HObject ResultRegion, out double[] IdealGetPicPos, out double[] LaserCenterPos);

                    if (status)
                    {
                        OutIdealGetPicPos = IdealGetPicPos;
                        OutputStEdPointXY = ConvertToLocusInfos(StEdPointXY);
                        UpdateResultPreviewObject(ResultRegion);
                        ResultRegion?.Dispose();
                    }
                    else if (!status)
                    {
                        ResultRegion?.Dispose();
                        return NodeStatus.Error;
                    }

                    #region 执行运动轨迹
                    switch (CurrentScanType)
                    {
                        case ScanTypes.旋转线扫:
                            {
                                if (StEdPointXY != null)
                                {

                                }


                            }break;
                        default:
                            {

                            }break;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;
                }

                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：区域处理模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        private void RefreshInitCircle()
        {
            HObject previousCircle = InitCircle;

            if (_inputCircle?.Value is not HObject circle || !circle.IsInitialized())
            {
                InitCircle = new HObject();
                previousCircle?.Dispose();
                RebuildPreviewDrawObjects();
                return;
            }

            InitCircle = circle.Clone();
            previousCircle?.Dispose();
            RebuildPreviewDrawObjects();
        }

        private void RefreshPreview()
        {
            if (_inputImage?.Value is not HObject inputImage || !inputImage.IsInitialized())
            {
                PreviewImage = new HImage();
                InputImageSize = null;
                return;
            }

            HImage sourceImage = new HImage(inputImage);
            sourceImage.GetImageSize(out int width, out int height);
            InputImageSize = [width, height];
            PreviewImage = sourceImage.CopyImage();
            sourceImage.Dispose();
        }

        private void UpdateResultPreviewObject(HObject resultPreviewObject)
        {
            HObject previousResultPreviewObject = _resultPreviewObject;

            if (resultPreviewObject == null || !resultPreviewObject.IsInitialized())
            {
                _resultPreviewObject = new HObject();
            }
            else
            {
                _resultPreviewObject = resultPreviewObject.Clone();
            }

            previousResultPreviewObject?.Dispose();
            RebuildPreviewDrawObjects();
        }

        private void RebuildPreviewDrawObjects()
        {
            ClearPreviewDrawObjects();
            AddPreviewDrawObject(InitCircle, "red");
            AddPreviewDrawObject(_resultPreviewObject, "green");
        }
        private static List<LocusInfo> ConvertToLocusInfos(IEnumerable<Geometry2D> geometry2Ds)
        {
            if (geometry2Ds == null)
            {
                return new List<LocusInfo>();
            }

            return geometry2Ds.Select(item => new LocusInfo
            {
                Type = item.IsPoint ? "IsPoint" : item.IsCircle ? "IsCircle" : "IsLine",
                OriginX = item.X1,
                OriginY = item.Y1,
                TargetX = item.IsCircle ? item.Radius : item.X2,
                TargetY = item.IsCircle ? 0 : item.Y2
            }).ToList();
        }
        private void AddPreviewDrawObject(HObject sourceObject, string color)
        {
            if (sourceObject == null || !sourceObject.IsInitialized())
            {
                return;
            }

            PreviewDrawObjectList.Add(new DrawingObjectInfo
            {
                ShapeType = ShapeType.Region,
                Hobject = sourceObject.Clone(),
                Color = color,
                IsFillDisplay = false
            });
        }

        private void ClearPreviewDrawObjects()
        {
            foreach (DrawingObjectInfo drawObject in PreviewDrawObjectList)
            {
                try
                {
                    drawObject.Hobject?.Dispose();
                }
                catch
                {
                }
            }

            PreviewDrawObjectList.Clear();
        }

        #endregion

    }
    [Serializable]
    public class LocusInfo : BindableBase
    {
        private string _type = "IsLine";
        private double _originX;
        private double _originY;
        private double _targetX;
        private double _targetY;

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                RaisePropertyChanged();
            }
        }

        public double OriginX
        {
            get => _originX;
            set
            {
                _originX = value;
                RaisePropertyChanged();
            }
        }

        public double OriginY
        {
            get => _originY;
            set
            {
                _originY = value;
                RaisePropertyChanged();
            }
        }

        public double TargetX
        {
            get => _targetX;
            set
            {
                _targetX = value;
                RaisePropertyChanged();
            }
        }

        public double TargetY
        {
            get => _targetY;
            set
            {
                _targetY = value;
                RaisePropertyChanged();
            }
        }
    }
    public static class ParamDefinitionExtensions
    {
        public static T GetValueOrDefault<T>(
            this IEnumerable<ParamDefinition> defs,
            string name,
            T defaultValue = default)
        {
            if (defs == null) return defaultValue;

            var param = defs.FirstOrDefault(p => p.Name == name);
            if (param?.Value == null)
                return defaultValue;

            try
            {
                if (param.Value is T t)
                    return t;

                return (T)Convert.ChangeType(param.Value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

using Dm;
using ALGO.MeasureCircle.Controls;
using DryIoc;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
using NetTaste;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
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
using System.Windows.Media;
using System.Windows.Shapes;

namespace ALGO.MeasureCircle
{
    [Serializable]
    public partial class MeasureCircleModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public bool IsDebug { get; set; } = false;

        [JsonIgnore]
        private readonly object _roiSyncRoot = new object();

        [JsonIgnore]
        private bool _isRefreshingPreview;

        [JsonIgnore]
        private MeasureCircleHalconControl _imageControl;

        [JsonIgnore]
        public MeasureCircleHalconControl ImageControl
        {
            get { return _imageControl; }
            set { SetProperty(ref _imageControl, value); }
        }

        //public HImage DisposeImage { get; set; }
        #endregion

        #region Properties
        [JsonIgnore]
        [InputParam(nameof(InputImage), "输入图像参数")]
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
                RefreshLinkedImage();
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private bool _disenableAffine2d = false;
        /// <summary>
        /// DisenableAffine2d
        /// </summary>
        public bool DisenableAffine2d
        {
            get { return _disenableAffine2d; }
            set { _disenableAffine2d = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _initCircleChanged_Flag = false;
        /// <summary>
        /// InitCircleChanged_Flag
        /// </summary>
        public bool InitCircleChanged_Flag
        {
            get { return _initCircleChanged_Flag; }
            set { _initCircleChanged_Flag = value; 
                  RaisePropertyChanged(); }
        }

        private bool _showResultPoint = true;
        /// <summary>显示结果点</summary>
        [JsonIgnore]
        public bool ShowResultPoint
        {
            get { return _showResultPoint; }
            set { SetProperty(ref _showResultPoint, value, RefreshMeasurementPreview); }
        }
        
        private bool _showMeasContour = true;
        /// <summary>显示测量轮廓 </summary>
        [JsonIgnore]
        public bool ShowMeasContour
        {
            get { return _showMeasContour; }
            set { SetProperty(ref _showMeasContour, value, RefreshMeasurementPreview); }
        }
        
        private bool _showResultCircle = true;
        /// <summary>显示结果圆 </summary>
        [JsonIgnore]
        public bool ShowResultCircle
        {
            get { return _showResultCircle; }
            set { SetProperty(ref _showResultCircle, value, RefreshMeasurementPreview); }
        }

        private bool _showCircleCenter;
        /// <summary>显示圆心</summary>
        [JsonIgnore]
        public bool ShowCircleCenter
        {
            get { return _showCircleCenter; }
            set { SetProperty(ref _showCircleCenter, value, RefreshMeasurementPreview); }
        }

        private bool _showCircleInfo;
        /// <summary>显示圆心和半径信息</summary>
        [JsonIgnore]
        public bool ShowCircleInfo
        {
            get { return _showCircleInfo; }
            set { SetProperty(ref _showCircleInfo, value, RefreshMeasurementPreview); }
        }


        /// <summary> 区域列表 </summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        [JsonIgnore]
        private ROICircle _outCircle = new ROICircle();
        /// <summary>
        /// 输出圆信息
        /// </summary>
        public ROICircle OutCircle
        {
            get { return _outCircle; }
            set { SetProperty(ref _outCircle, value); }
        }

        [JsonIgnore]
        private HObject _outCircleRegion = new HObject();
        [JsonIgnore]
        [OutputParam("OutCircleRegion", "圆轮廓对象")]
        /// <summary>
        /// 检测到的圆轮廓对象
        /// </summary>
        public HObject OutCircleRegion
        {
            get { return _outCircleRegion; }
            set { SetProperty(ref _outCircleRegion, value); }
        }

        [JsonIgnore]
        private double _outCircleCenterX = -1;
        [JsonIgnore]
        [OutputParam("OutCircleCenterX", "圆心X")]
        /// <summary>
        /// 检测到的圆心坐标X
        /// </summary>
        public double OutCircleCenterX
        {
            get { return _outCircleCenterX; }
            set { SetProperty(ref _outCircleCenterX, value); }
        }

        [JsonIgnore]
        private double _outCircleCenterY = -1;
        [JsonIgnore]
        [OutputParam("OutCircleCenterY", "圆心Y")]
        /// <summary>
        /// 检测到的圆心坐标Y
        /// </summary>
        public double OutCircleCenterY
        {
            get { return _outCircleCenterY; }
            set { SetProperty(ref _outCircleCenterY, value); }
        }

        [JsonIgnore]
        private double _outCircleRadius = -1;
        [JsonIgnore]
        [OutputParam("OutCircleRadius", "半径")]
        /// <summary>
        /// 检测到的圆半径
        /// </summary>
        public double OutCircleRadius
        {
            get { return _outCircleRadius; }
            set { SetProperty(ref _outCircleRadius, value); }
        }

        [JsonIgnore]
        private double _initCircleCenterX = 50.0;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double InitCircleCenterX
        {
            get { return _initCircleCenterX; }
            set { _initCircleCenterX = value; RaisePropertyChanged(); InitCircleChanged(); }
        }

        [JsonIgnore]
        private double _initCircleCenterY = 50.0;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double InitCircleCenterY
        {
            get { return _initCircleCenterY; }
            //set { _initCircleCenterY = value; RaisePropertyChanged(); InitCircleChanged(); }
            set { _initCircleCenterY = value; RaisePropertyChanged(); InitCircleChanged(); }
        }

        [JsonIgnore]
        private double _initCircleRadius = 40.0;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double InitCircleRadius
        {
            get { return _initCircleRadius; }
            set { _initCircleRadius = value; RaisePropertyChanged(); InitCircleChanged(); }
        }

        /// <summary>
        /// 变换矩阵
        /// </summary>
        [JsonIgnore]
        public HTuple HomMat2D { get; set; } = new HTuple();

        /// <summary>
        /// 逆变换矩阵
        /// </summary>
        [JsonIgnore]
        public HTuple HomMat2D_Inverse { get; set; } = new HTuple();


        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        [JsonIgnore]
        public ROICircle InitCircle { get; set; } = new ROICircle();

        /// <summary>
        /// 圆信息
        /// </summary>
        [JsonIgnore]
        public ROICircle TempCircle { get; set; } = new ROICircle();

        /// <summary>
        /// 变换后-圆信息
        /// </summary>
        [JsonIgnore]
        public ROICircle TranCircle { get; set; } = new ROICircle();

        /// <summary>
        /// 圆卡尺ROI信息
        /// </summary>
        [JsonIgnore]
        public ROICircle roiCircle { get; set; } = new ROICircle();

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        private double _length1 = 40;
        /// <summary>
        /// 长/2
        /// </summary>
        public double Length1
        {
            get { return _length1; }
            set { SetProperty(ref _length1, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _length2 = 10;
        /// <summary>
        /// 宽/2
        /// </summary>
        public double Length2
        {
            get { return _length2; }
            set { SetProperty(ref _length2, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _threshold = 30;
        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold
        {
            get { return _threshold; }
            set { SetProperty(ref _threshold, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _measDis = 10;
        /// <summary>
        /// 间隔
        /// </summary>
        public double MeasDis
        {
            get { return _measDis; }
            set
            {
                SetProperty(ref _measDis, value, () =>
                {
                    RebuildMeasureParamValue();
                    RefreshMeasurementPreview();
                });
            }
        }

        [JsonIgnore]
        private double _measureSigma = 1;
        /// <summary>
        /// 边缘平滑系数
        /// </summary>
        public double MeasureSigma
        {
            get { return _measureSigma; }
            set { SetProperty(ref _measureSigma, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _minScore = 0;
        /// <summary>
        /// 最低匹配得分，0表示不限制
        /// </summary>
        public double MinScore
        {
            get { return _minScore; }
            set { SetProperty(ref _minScore, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private int _minMeasurePointCount = 3;
        /// <summary>
        /// 最少有效测量点数量
        /// </summary>
        public int MinMeasurePointCount
        {
            get { return _minMeasurePointCount; }
            set { SetProperty(ref _minMeasurePointCount, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _pointDistanceTolerance = 0;
        /// <summary>
        /// 点到拟合圆的距离容差，0表示不启用
        /// </summary>
        public double PointDistanceTolerance
        {
            get { return _pointDistanceTolerance; }
            set { SetProperty(ref _pointDistanceTolerance, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _minRadius = 0;
        /// <summary>
        /// 最小半径，0表示不限制
        /// </summary>
        public double MinRadius
        {
            get { return _minRadius; }
            set { SetProperty(ref _minRadius, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private double _maxRadius = 0;
        /// <summary>
        /// 最大半径，0表示不限制
        /// </summary>
        public double MaxRadius
        {
            get { return _maxRadius; }
            set { SetProperty(ref _maxRadius, value, RefreshMeasurementPreview); }
        }

        [JsonIgnore]
        private HTuple _paramName;
        /// <summary>
        /// 参数名
        /// </summary>
        public HTuple ParamName
        {
            get { return _paramName; }
            set { SetProperty(ref _paramName, value); }
        }

        [JsonIgnore]
        private HTuple _paramValue;
        /// <summary>
        /// 参数值
        /// </summary>
        public HTuple ParamValue
        {
            get { return _paramValue; }
            set { SetProperty(ref _paramValue, value); }
        }

        private int _PointsOrder;
        /// <summary>
        /// 点顺序 0位默认,1顺时针,2逆时针
        /// </summary>
        public int PointsOrder
        {
            get { return _PointsOrder; }
            set { SetProperty(ref _PointsOrder, value); }
        }

        [JsonIgnore]
        private eMeasMode _measMode = eMeasMode.由白到黑;
        /// <summary>
        /// 测量模式
        /// </summary>
        public eMeasMode MeasMode
        {
            get { return _measMode; }
            set
            {
                SetProperty(ref _measMode, value, () =>
                {
                    RebuildMeasureParamValue();
                    RefreshMeasurementPreview();
                });
            }
        }

        [JsonIgnore]
        private eMeasSelect _measSelect = eMeasSelect.第一点;
        /// <summary>
        /// 测量点筛选
        /// </summary>
        public eMeasSelect MeasSelect
        {
            get { return _measSelect; }
            set
            {
                SetProperty(ref _measSelect, value, () =>
                {
                    RebuildMeasureParamValue();
                    RefreshMeasurementPreview();
                });
            }
        }
        #endregion

        #region Constructor
        public MeasureCircleModel()
        {
            EnsureMeasureParametersInitialized(true);
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

                //ModuleName = Serial.ToString("D3");
                //object inputImageValue = GetMarkedInputParamValue(nameof(InputImage));
                //RefreshLinkedImageParam(_inputImage, inputImageValue);
                //RefreshLinkedImage();
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

        public override void Dispose()
        {
            ReleaseCalibrationRuntime();
            DetachImageControl();
            base.Dispose();
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
                #region 检测参数（对链接参数重新赋值）
                Console.WriteLine($"开始加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                if (!IsDebug)
                    LoadKeyParam();
                Console.WriteLine($"结束加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                #endregion
                try
                {


                    if (_inputImage.Value == null)
                        return NodeStatus.None;
                    var tempImage = new HImage((HObject)_inputImage.Value).Clone();
                    ClearDisplayRois();
                    if (tempImage == null && !tempImage.IsInitialized())
                        return NodeStatus.Error;

                    //执行的方法
                    if (DisenableAffine2d && HomMat2D_Inverse != null && HomMat2D_Inverse.Length > 0)
                    {
                        DisenableAffine2d = false;
                        Affine2d(HomMat2D_Inverse, TempCircle, InitCircle);
                        if (InitCircleChanged_Flag)
                        {
                            InitCircleCenterX = InitCircle.CenterX;
                            InitCircleCenterY = InitCircle.CenterY;
                            InitCircleRadius = InitCircle.Radius;
                        }
                    }
                    if (HomMat2D != null && HomMat2D.Length > 0)
                    {
                        InitCircle.CenterX = InitCircleCenterX;
                        InitCircle.CenterY = InitCircleCenterY;
                        InitCircle.Radius = InitCircleRadius;
                        Affine2d(HomMat2D, InitCircle, TranCircle);
                    }
                    else
                    {
                        InitCircle.CenterX = TranCircle.CenterX = TempCircle.CenterX;
                        InitCircle.CenterY = TranCircle.CenterY = TempCircle.CenterY;
                        InitCircle.Radius = TranCircle.Radius = TempCircle.Radius;
                    }

                    bool status = MeasCircle(tempImage, TranCircle, OutCircle, out HTuple RowList, out HTuple ColList, out HXLDCont m_MeasXLD, null);
                    GenCircle(out HObject m_ResultXLD, OutCircle.CenterX, OutCircle.CenterY, OutCircle.Radius);
                    UpdateMeasureResultDisplay(status, RowList, ColList, m_MeasXLD, m_ResultXLD);

                    if(status)
                    {
                        UpdateOutputCircleCoordinates(OutCircle, RowList, ColList);
                        OutCircleRegion = m_ResultXLD.Clone();
                        Console.WriteLine($"找圆测量结果：{JsonHelper.Serialize(OutCircleCenterX)}");
                    }

                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;
                }
                //var result = Run();
                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：找圆模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public NodeStatus Run()
        {
            try
            {
                if (_inputImage.Value == null)
                    return NodeStatus.None;
                var tempImage = new HImage((HObject)_inputImage.Value).Clone();
                ClearDisplayRois();
                if (tempImage == null && !tempImage.IsInitialized())
                    return NodeStatus.Error;

                //执行的方法
                if (DisenableAffine2d && HomMat2D_Inverse != null && HomMat2D_Inverse.Length > 0)
                {
                    DisenableAffine2d = false;
                    Affine2d(HomMat2D_Inverse, TempCircle, InitCircle);
                    if (InitCircleChanged_Flag)
                    {
                        InitCircleCenterX = InitCircle.CenterX;
                        InitCircleCenterY = InitCircle.CenterY;
                        InitCircleRadius = InitCircle.Radius;
                    }
                }
                if (HomMat2D != null && HomMat2D.Length > 0)
                {
                    InitCircle.CenterX = InitCircleCenterX;
                    InitCircle.CenterY = InitCircleCenterY;
                    InitCircle.Radius = InitCircleRadius;
                    Affine2d(HomMat2D, InitCircle, TranCircle);
                }
                else
                {
                    InitCircle.CenterX = TranCircle.CenterX = TempCircle.CenterX;
                    InitCircle.CenterY = TranCircle.CenterY = TempCircle.CenterY;
                    InitCircle.Radius = TranCircle.Radius = TempCircle.Radius;
                }

                bool status = MeasCircle(tempImage, TranCircle, OutCircle, out HTuple RowList, out HTuple ColList, out HXLDCont m_MeasXLD, null);
                GenCircle(out HObject m_ResultXLD, OutCircle.CenterX, OutCircle.CenterY, OutCircle.Radius);
                UpdateMeasureResultDisplay(status, RowList, ColList, m_MeasXLD, m_ResultXLD);

                if (status)
                {
                    UpdateOutputCircleCoordinates(OutCircle, RowList, ColList);
                    OutCircleRegion = m_ResultXLD.Clone();
                    Console.WriteLine($"找圆测量结果：{JsonHelper.Serialize(OutCircleCenterX)}");
                }
                return NodeStatus.Success;
            }
            catch (Exception ex)
            {
                return NodeStatus.Error;
            }
        }

        /// <summary>
        /// 检测圆
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="circel">输入圆</param>
        /// <param name="meas">输入形态学</param>
        /// <param name="outCircle">输出圆</param>
        /// <param name="outR">输出行坐标</param>
        /// <param name="outC">输出列坐标</param>
        /// <param name="outXld">输出检测轮廓</param>
        public bool MeasCircle(
            HImage image,
            ROICircle circel,
            ROICircle outCircle,
            out HTuple outR,
            out HTuple outC,
            out HXLDCont outXld,
            HRegion? disableRegion = null
        )
        {
            HMetrologyModel MetroModel = new HMetrologyModel();
            try
            {
                EnsureMeasureParametersInitialized();

                HTuple Circle_Info = new HTuple(new double[] { circel.CenterY, circel.CenterX, circel.Radius });

                MetroModel.AddMetrologyObjectGeneric(
                new HTuple("circle"),
                Circle_Info,
                    new HTuple(Length1),
                    new HTuple(Length2),
                    new HTuple(Math.Max(0.4, MeasureSigma)),
                    new HTuple(Threshold),
                    ParamName,
                    ParamValue
                );
                MetroModel.SetMetrologyObjectParam(0, "measure_distance", Math.Max(0, MeasDis));
                if (MinScore > 0)
                {
                    MetroModel.SetMetrologyObjectParam(0, "min_score", MinScore);
                }

                MetroModel.ApplyMetrologyModel(image);
                outXld = MetroModel.GetMetrologyObjectMeasures("all", "all", out outR, out outC);
                bool pointsFiltered = FilterMeasurePointsByDisableRegion(disableRegion, ref outR, ref outC);

                bool hasCircle = TryGetMetrologyCircleResult(MetroModel, out ROICircle candidateCircle);
                if (!hasCircle && outR.TupleLength() >= 3)
                {
                    hasCircle = FitCircle(outR.ToDArr(), outC.ToDArr(), out candidateCircle);
                }

                if (PointDistanceTolerance > 0 && hasCircle && outR.TupleLength() >= 3)
                {
                    pointsFiltered |= FilterMeasurePointsByDistance(candidateCircle, ref outR, ref outC);
                }

                if (pointsFiltered)
                {
                    hasCircle = outR.TupleLength() >= 3 && FitCircle(outR.ToDArr(), outC.ToDArr(), out candidateCircle);
                }

                if (hasCircle)
                {
                    outCircle.CenterY = candidateCircle.CenterY;
                    outCircle.CenterX = candidateCircle.CenterX;
                    outCircle.Radius = candidateCircle.Radius;
                }
                MetroModel.Dispose();

                return hasCircle && ValidateMeasuredCircle(outCircle, outR.TupleLength());
            }
            catch (Exception ex)
            {
                outCircle = circel;
                outR = new HTuple();
                outC = new HTuple();
                outXld = new HXLDCont();
                MetroModel.Dispose();
                Debug.Write(ex.Message);

                return false;
            }
        }

        /// <summary>
        /// 最小二乘法圆拟合
        /// </summary>
        /// <param name="rows">点云 行坐标</param>
        /// <param name="cols">点云 列坐标</param>
        /// <param name="circle">返回圆</param>
        /// <returns>是否拟合成功</returns>
        public static bool FitCircle(double[] rows, double[] cols, out ROICircle circle)
        {
            circle = new ROICircle();
            if (cols.Length < 3)
            {
                return false;
            }
            //本地代码验证通过------20180827 yoga
            ////原始托管代码
            double sum_x = 0.0f,
                sum_y = 0.0f;
            double sum_x2 = 0.0f,
                sum_y2 = 0.0f;
            double sum_x3 = 0.0f,
                sum_y3 = 0.0f;
            double sum_xy = 0.0f,
                sum_x1y2 = 0.0f,
                sum_x2y1 = 0.0f;
            int N = cols.Length;
            for (int i = 0; i < N; i++)
            {
                double x = rows[i];
                double y = cols[i];
                double x2 = x * x;
                double y2 = y * y;
                sum_x += x;
                sum_y += y;
                sum_x2 += x2;
                sum_y2 += y2;
                sum_x3 += x2 * x;
                sum_y3 += y2 * y;
                sum_xy += x * y;
                sum_x1y2 += x * y2;
                sum_x2y1 += x2 * y;
            }
            double C,
                D,
                E,
                G,
                H;
            double a,
                b,
                c;
            C = N * sum_x2 - sum_x * sum_x;
            D = N * sum_xy - sum_x * sum_y;
            E = N * sum_x3 + N * sum_x1y2 - (sum_x2 + sum_y2) * sum_x;
            G = N * sum_y2 - sum_y * sum_y;
            H = N * sum_x2y1 + N * sum_y3 - (sum_x2 + sum_y2) * sum_y;
            a = (H * D - E * G) / (C * G - D * D);
            b = (H * C - E * D) / (D * D - G * C);
            c = -(a * sum_x + b * sum_y + sum_x2 + sum_y2) / N;
            circle.CenterY = Math.Round(a / (-2), 4);
            circle.CenterX = Math.Round(b / (-2), 4);
            circle.Radius = Math.Round(Math.Sqrt(a * a + b * b - 4 * c) / 2, 4);
            return true;
        }

        /// <summary>
        /// 最小二乘法圆拟合
        /// </summary>
        /// <param name="rows">点云 行坐标</param>
        /// <param name="cols">点云 列坐标</param>
        /// <param name="circle">返回圆</param>
        /// <returns>是否拟合成功</returns>
        public static bool FitCircle1(List<double> rows, List<double> cols, ROICircle circle)
        {
            if (cols.Count < 3)
            {
                circle.Status = false;
                return false;
            }
            //本地代码验证通过------20180827 yoga
            ////原始托管代码
            double sum_x = 0.0f,
                sum_y = 0.0f;
            double sum_x2 = 0.0f,
                sum_y2 = 0.0f;
            double sum_x3 = 0.0f,
                sum_y3 = 0.0f;
            double sum_xy = 0.0f,
                sum_x1y2 = 0.0f,
                sum_x2y1 = 0.0f;
            int N = cols.Count;
            for (int i = 0; i < N; i++)
            {
                double x = rows[i];
                double y = cols[i];
                double x2 = x * x;
                double y2 = y * y;
                sum_x += x;
                sum_y += y;
                sum_x2 += x2;
                sum_y2 += y2;
                sum_x3 += x2 * x;
                sum_y3 += y2 * y;
                sum_xy += x * y;
                sum_x1y2 += x * y2;
                sum_x2y1 += x2 * y;
            }
            double C,
                D,
                E,
                G,
                H;
            double a,
                b,
                c;
            C = N * sum_x2 - sum_x * sum_x;
            D = N * sum_xy - sum_x * sum_y;
            E = N * sum_x3 + N * sum_x1y2 - (sum_x2 + sum_y2) * sum_x;
            G = N * sum_y2 - sum_y * sum_y;
            H = N * sum_x2y1 + N * sum_y3 - (sum_x2 + sum_y2) * sum_y;
            a = (H * D - E * G) / (C * G - D * D);
            b = (H * C - E * D) / (D * D - G * C);
            c = -(a * sum_x + b * sum_y + sum_x2 + sum_y2) / N;
            circle.CenterY = Math.Round(a / (-2), 4);
            circle.CenterX = Math.Round(b / (-2), 4);
            circle.Radius = Math.Round(Math.Sqrt(a * a + b * b - 4 * c) / 2, 4);
            return true;
        }


        /// <summary>
        /// 点排序
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        public static void SortPairs(ref List<double> rows, ref List<double> cols)
        {
            HTuple hv_T1 = new HTuple(rows.ToArray());
            HTuple hv_T2 = new HTuple(cols.ToArray());
            //相同的方法 直接使用htuple返回结果
            SortPairs(ref hv_T1, ref hv_T2);
            rows = hv_T1.ToDArr().ToList();
            cols = hv_T2.ToDArr().ToList();
            return;
        }


        /// <summary>
        /// 点排序
        /// </summary>
        /// <param name="hv_T1"></param>
        /// <param name="hv_T2"></param>
        public static void SortPairs(ref HTuple hv_T1, ref HTuple hv_T2)
        {
            HTuple hv_Sorted1 = new HTuple();
            HTuple hv_Sorted2 = new HTuple();
            HTuple hv_SortMode = new HTuple();
            HTuple hv_Indices1 = new HTuple(),
                hv_Indices2 = new HTuple();
            if (
                (hv_T1.TupleMax().D - hv_T1.TupleMin().D)
                > (hv_T2.TupleMax().D - hv_T2.TupleMin().D)
            )
                hv_SortMode = new HTuple("1");
            else
                hv_SortMode = new HTuple("2");
            if (
                (int)(
                    (new HTuple(hv_SortMode.TupleEqual("1"))).TupleOr(
                        new HTuple(hv_SortMode.TupleEqual(1))
                    )
                ) != 0
            )
            {
                HOperatorSet.TupleSortIndex(hv_T1, out hv_Indices1);
                hv_Sorted1 = hv_T1.TupleSelect(hv_Indices1);
                hv_Sorted2 = hv_T2.TupleSelect(hv_Indices1);
            }
            else if (
                (int)(
                    (
                        new HTuple(
                            (new HTuple(hv_SortMode.TupleEqual("column"))).TupleOr(
                                new HTuple(hv_SortMode.TupleEqual("2"))
                            )
                        )
                    ).TupleOr(new HTuple(hv_SortMode.TupleEqual(2)))
                ) != 0
            )
            {
                HOperatorSet.TupleSortIndex(hv_T2, out hv_Indices2);
                hv_Sorted1 = hv_T1.TupleSelect(hv_Indices2);
                hv_Sorted2 = hv_T2.TupleSelect(hv_Indices2);
            }
            hv_T1 = hv_Sorted1;
            hv_T2 = hv_Sorted2;
        }


        /// <summary>创建点xld</summary>
        /// <param name="MeasCross"></param>
        /// <param name="RowList"></param>
        /// <param name="ColList"></param>
        /// <param name="size"></param>
        /// <param name="angle"></param>
        public static void GenCross(
            out HObject MeasCross,
            HTuple RowList,
            HTuple ColList,
            HTuple size,
            HTuple angle
        )
        {
            HOperatorSet.GenCrossContourXld(out MeasCross, RowList, ColList, size, angle);
        }


        /// <summary>创建结果xld-线</summary>
        /// <param name="ResultXLD"></param>
        /// <param name="StartX"></param>
        /// <param name="StartY"></param>
        /// <param name="EndX"></param>
        /// <param name="EndY"></param>
        public static void GenContour(
            out HObject ResultXLD,
            double StartX,
            double StartY,
            double EndX,
            double EndY
        )
        {
            HOperatorSet.GenContourPolygonXld(
                out ResultXLD,
                new HTuple(StartX, StartY),
                new HTuple(EndX, EndY)
            );
        }

        /// <summary>创建结果xld-圆 </summary>
        /// <param name="ResultXLD"></param>
        /// <param name="CenterX"></param>
        /// <param name="CenterY"></param>
        /// <param name="Radius"></param>
        public static void GenCircle(
            out HObject ResultXLD,
            double CenterX,
            double CenterY,
            double Radius
        )
        {
            HOperatorSet.GenCircleContourXld(
                out ResultXLD,
                CenterY,
                CenterX,
                Radius,
                0,
                6.28318,
                "positive",
                1
            );
        }

        private void EnsureMeasureParametersInitialized(bool forceRebuild = false)
        {
            if (forceRebuild || ParamName == null || ParamName.TupleLength() == 0)
            {
                ParamName = new HTuple();
                ParamName.Append("measure_transition");
                ParamName.Append("measure_select");
                ParamName.Append("measure_distance");
            }

            if (forceRebuild || ParamValue == null || ParamValue.TupleLength() == 0)
            {
                RebuildMeasureParamValue();
            }
        }

        private void RebuildMeasureParamValue()
        {
            ParamValue = new HTuple();
            ParamValue.Append(REnum.EnumToStr(_measMode));
            ParamValue.Append(REnum.EnumToStr(_measSelect));
            ParamValue.Append(_measDis);
        }

        private void RefreshMeasurementPreview()
        {
            if (_isRefreshingPreview || ImageControl == null || _inputImage?.Value == null)
            {
                return;
            }

            try
            {
                _isRefreshingPreview = true;
                Run();
            }
            catch
            {
            }
            finally
            {
                _isRefreshingPreview = false;
            }
        }

        private void UpdateMeasureResultDisplay(bool status, HTuple rowList, HTuple colList, HXLDCont measXld, HObject resultXld)
        {
            int pointCount = rowList == null ? 0 : rowList.TupleLength();

            if (ShowResultPoint && pointCount > 0)
            {
                GenCross(out HObject measCross, rowList, colList, Length2 / 2, new HTuple(45).TupleRad());
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测点, "red", new HObject(measCross)));
            }

            if (ShowResultCircle && pointCount > 0 && status)
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "green", new HObject(resultXld)));
            }

            if (ShowCircleCenter && status)
            {
                double crossSize = Math.Max(10, Math.Min(OutCircle.Radius * 0.15, 40));
                GenCross(
                    out HObject centerCross,
                    new HTuple(OutCircle.CenterY),
                    new HTuple(OutCircle.CenterX),
                    crossSize,
                    new HTuple(45).TupleRad()
                );
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测中心, "yellow", new HObject(centerCross)));
            }

            if (ShowCircleInfo && status)
            {
                ShowHRoi(
                    new HText(
                        Serial,
                        ModuleName,
                        "",
                        HRoiType.文字显示,
                        "yellow",
                        BuildCircleInfoText(pointCount),
                        ResolveCircleInfoRow(),
                        ResolveCircleInfoCol(),
                        16
                    )
                );
            }

            if (ShowMeasContour && measXld != null && measXld.IsInitialized())
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测范围, "blue", new HObject(measXld)));
            }

            ShowHRoi();
        }

        private string BuildCircleInfoText(int pointCount)
        {
            return $"X:{OutCircle.CenterX:F4}  Y:{OutCircle.CenterY:F4}  R:{OutCircle.Radius:F4}  Pts:{pointCount}";
        }

        private double ResolveCircleInfoRow()
        {
            return Math.Max(20, OutCircle.CenterY - OutCircle.Radius - 24);
        }

        private double ResolveCircleInfoCol()
        {
            return Math.Max(20, OutCircle.CenterX - OutCircle.Radius);
        }

        private bool FilterMeasurePointsByDisableRegion(HRegion disableRegion, ref HTuple rows, ref HTuple cols)
        {
            if (disableRegion == null || !disableRegion.IsInitialized() || disableRegion.Area <= 0 || rows == null || rows.TupleLength() == 0)
            {
                return false;
            }

            List<double> tempRows = new List<double>();
            List<double> tempCols = new List<double>();
            int originalCount = rows.TupleLength();

            for (int i = 0; i < originalCount; i++)
            {
                if (disableRegion.TestRegionPoint(rows[i].D, cols[i].D) == 0)
                {
                    tempRows.Add(rows[i].D);
                    tempCols.Add(cols[i].D);
                }
            }

            if (tempRows.Count == originalCount)
            {
                return false;
            }

            rows = new HTuple(tempRows.ToArray());
            cols = new HTuple(tempCols.ToArray());
            return true;
        }

        private bool FilterMeasurePointsByDistance(ROICircle circle, ref HTuple rows, ref HTuple cols)
        {
            if (rows == null || rows.TupleLength() < 3 || PointDistanceTolerance <= 0)
            {
                return false;
            }

            List<double> tempRows = new List<double>();
            List<double> tempCols = new List<double>();
            int originalCount = rows.TupleLength();

            for (int i = 0; i < originalCount; i++)
            {
                double distance = Math.Sqrt(
                    Math.Pow(rows[i].D - circle.CenterY, 2) +
                    Math.Pow(cols[i].D - circle.CenterX, 2)
                );

                if (Math.Abs(distance - circle.Radius) <= PointDistanceTolerance)
                {
                    tempRows.Add(rows[i].D);
                    tempCols.Add(cols[i].D);
                }
            }

            if (tempRows.Count == originalCount || tempRows.Count < 3)
            {
                return false;
            }

            rows = new HTuple(tempRows.ToArray());
            cols = new HTuple(tempCols.ToArray());
            return true;
        }

        private static bool TryGetMetrologyCircleResult(HMetrologyModel metroModel, out ROICircle circle)
        {
            circle = new ROICircle();
            HTuple circleResult = metroModel.GetMetrologyObjectResult(
                new HTuple("all"),
                new HTuple("all"),
                new HTuple("result_type"),
                new HTuple("all_param")
            );

            if (circleResult.TupleLength() < 3)
            {
                return false;
            }

            circle.CenterY = Math.Round(circleResult[0].D, 4);
            circle.CenterX = Math.Round(circleResult[1].D, 4);
            circle.Radius = Math.Round(circleResult[2].D, 4);
            return true;
        }

        private bool ValidateMeasuredCircle(ROICircle circle, int pointCount)
        {
            if (circle == null || circle.Radius <= 0 || double.IsNaN(circle.CenterX) || double.IsNaN(circle.CenterY) || double.IsNaN(circle.Radius))
            {
                return false;
            }

            if (pointCount < Math.Max(3, MinMeasurePointCount))
            {
                return false;
            }

            if (MinRadius > 0 && circle.Radius < MinRadius)
            {
                return false;
            }

            if (MaxRadius > 0 && circle.Radius > MaxRadius)
            {
                return false;
            }

            return true;
        }
        #endregion

    }





}

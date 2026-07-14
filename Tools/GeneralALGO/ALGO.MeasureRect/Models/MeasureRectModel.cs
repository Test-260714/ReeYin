using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.MeasureRect
{
    [Serializable]
    public class MeasureRectModel : ModelParamBase
    {
        #region 字段
        private const string EditableRectColor = "blue";

        [JsonIgnore]
        private bool _ownsInputImage;

        [JsonIgnore]
        private bool _previewRefreshPending = true;

        [JsonIgnore]
        private long _previewUpdateVersion;

        [JsonIgnore]
        private HObject _previewImageObject;

        [JsonIgnore]
        private double _previewImageWidth = 1.0;

        [JsonIgnore]
        private double _previewImageHeight = 1.0;

        [JsonIgnore]
        private CameraCalibrationSdk _cameraCalib;

        [JsonIgnore]
        private string _calibrationCameraId = string.Empty;

        [JsonIgnore]
        public new bool IsDebug { get; set; } = false;
        #endregion 字段

        #region 属性

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get
            {
                RefreshInputImagePreview();
                return _inputImage;
            }
            set
            {
                SetInputImage(value);
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get { return _previewImageObject; }
            private set { SetProperty(ref _previewImageObject, value); }
        }

        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new();

        [JsonIgnore]
        public double PreviewImageWidth
        {
            get { return _previewImageWidth; }
            private set { SetProperty(ref _previewImageWidth, Math.Max(1.0, value)); }
        }

        [JsonIgnore]
        public double PreviewImageHeight
        {
            get { return _previewImageHeight; }
            private set { SetProperty(ref _previewImageHeight, Math.Max(1.0, value)); }
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
        private bool _initRectChanged_Flag = false;
        /// <summary>
        /// InitRectChanged_Flag
        /// </summary>
        public bool InitRectChanged_Flag
        {
            get { return _initRectChanged_Flag; }
            set
            {
                _initRectChanged_Flag = value;
                RaisePropertyChanged();
            }
        }

        private bool _showResultPoint = true;
        /// <summary>显示结果点</summary>
        [JsonIgnore]
        public bool ShowResultPoint
        {
            get { return _showResultPoint; }
            set { SetProperty(ref _showResultPoint, value); }
        }

        private bool _showMeasContour = true;
        /// <summary>显示测量轮廓 </summary>
        [JsonIgnore]
        public bool ShowMeasContour
        {
            get { return _showMeasContour; }
            set { SetProperty(ref _showMeasContour, value); }
        }

        private bool _showResultRect = true;
        /// <summary>显示结果矩形 </summary>
        [JsonIgnore]
        public bool ShowResultRect
        {
            get { return _showResultRect; }
            set { SetProperty(ref _showResultRect, value); }
        }

        [JsonIgnore]
        private ROIRectangle2 _outRect = new ROIRectangle2();
        /// <summary>
        /// 输出矩形信息
        /// </summary>
        public ROIRectangle2 OutRect
        {
            get { return _outRect; }
            set { SetProperty(ref _outRect, value); }
        }

        [JsonIgnore]
        private HRegion _outRectRegion = new HRegion();
        /// <summary>
        /// 根据测量结果生成的矩形区域。
        /// </summary>
        [JsonIgnore]
        [OutputParam("OutRectRegion", "矩形区域")]
        public HRegion OutRectRegion
        {
            get { return _outRectRegion; }
            set { SetProperty(ref _outRectRegion, value ?? new HRegion()); }
        }

        private double _rectDeg;

        public double RectDeg
        {
            get { return _rectDeg; }
            set { SetProperty(ref _rectDeg, value); }
        }

        [JsonIgnore]
        private double _outRectLen1 = -1;
        [JsonIgnore]
        [OutputParam("OutRectLen1", "长边L1")]
        /// <summary>
        /// 检测到的矩形长边
        /// </summary>
        public double OutRectLen1
        {
            get { return _outRectLen1; }
            set { SetProperty(ref _outRectLen1, value); }
        }

        [JsonIgnore]
        private double _outRectLen2 = -1;
        [JsonIgnore]
        [OutputParam("OutRectLen2", "短边L2")]
        /// <summary>
        /// 检测到的矩形短边
        /// </summary>
        public double OutRectLen2
        {
            get { return _outRectLen2; }
            set { SetProperty(ref _outRectLen2, value); }
        }

        [JsonIgnore]
        private double _outRectPX = -1;
        [JsonIgnore]
        [OutputParam("OuttRectPX", "中心X")]
        /// <summary>
        /// 检测到的矩形中心X坐标
        /// </summary>
        public double OutRectPX
        {
            get { return _outRectPX; }
            set { SetProperty(ref _outRectPX, value); }
        }

        [JsonIgnore]
        private double _outRectPY = -1;
        [JsonIgnore]
        [OutputParam("OuttRectPY", "中心Y")]
        /// <summary>
        /// 检测到的矩形中心Y坐标
        /// </summary>
        public double OutRectPY
        {
            get { return _outRectPY; }
            set { SetProperty(ref _outRectPY, value); }
        }

        [JsonIgnore]
        private double _outRectAngle = -1;
        [JsonIgnore]
        [OutputParam("OuttRectAngle", "角度")]
        /// <summary>
        /// 检测到的矩形角度
        /// </summary>
        public double OutRectAngle
        {
            get { return _outRectAngle; }
            set { SetProperty(ref _outRectAngle, value); }
        }

        private bool _enableCalibration;
        /// <summary>
        /// 是否启用标定
        /// </summary>
        public bool EnableCalibration
        {
            get { return _enableCalibration; }
            set
            {
                if (SetProperty(ref _enableCalibration, value))
                {
                    if (_enableCalibration)
                    {
                        InvalidateCalibrationRuntime();
                    }
                    else
                    {
                        DisableCalibration();
                    }
                }
            }
        }

        private string _calibrationFilePath = string.Empty;
        /// <summary>
        /// 标定文件路径
        /// </summary>
        public string CalibrationFilePath
        {
            get { return _calibrationFilePath; }
            set
            {
                if (SetProperty(ref _calibrationFilePath, value))
                {
                    InvalidateCalibrationRuntime();
                }
            }
        }

        [JsonIgnore]
        private bool _isCalibrationEffective;
        /// <summary>
        /// 标定是否生效
        /// </summary>
        [JsonIgnore]
        public bool IsCalibrationEffective
        {
            get { return _isCalibrationEffective; }
        }

        /// <summary>
        /// 标定状态文本
        /// </summary>
        [JsonIgnore]
        public string CalibrationStatusText
        {
            get
            {
                if (!EnableCalibration)
                {
                    return "未启用标定";
                }

                if (IsCalibrationEffective)
                {
                    return string.IsNullOrWhiteSpace(_calibrationCameraId)
                        ? "标定已生效"
                        : $"标定已生效：{_calibrationCameraId}";
                }

                if (string.IsNullOrWhiteSpace(CalibrationFilePath))
                {
                    return "未选择标定文件，输出像素坐标";
                }

                return "标定未生效，输出像素坐标";
            }
        }

        [JsonIgnore]
        private double _initRectLen1 = 50.0;
        /// <summary>
        /// 变换前-矩形信息
        /// </summary>
        public double InitRectLen1
        {
            get { return _initRectLen1; }
            set { _initRectLen1 = value; RaisePropertyChanged(); InitRectChanged(); }
        }

        [JsonIgnore]
        private double _initRectLen2 = 50.0;
        /// <summary>
        /// 变换前-矩形信息
        /// </summary>
        public double InitRectLen2
        {
            get { return _initRectLen2; }
            set { _initRectLen2 = value; RaisePropertyChanged(); InitRectChanged(); }
        }

        [JsonIgnore]
        private double _initRectPX = 40.0;
        /// <summary>
        /// 变换前-矩形信息
        /// </summary>
        public double InitRectPX
        {
            get { return _initRectPX; }
            set { _initRectPX = value; RaisePropertyChanged(); InitRectChanged(); }
        }

        [JsonIgnore]
        private double _initRectPY = 40.0;
        /// <summary>
        /// 变换前-矩形信息
        /// </summary>
        public double InitRectPY
        {
            get { return _initRectPY; }
            set { _initRectPY = value; RaisePropertyChanged(); InitRectChanged(); }
        }

        [JsonIgnore]
        private double _initRectAngle = 40.0;
        /// <summary>
        /// 变换前-矩形信息
        /// </summary>
        public double InitRectAngle
        {
            get { return _initRectAngle; }
            set { _initRectAngle = value; RaisePropertyChanged(); InitRectChanged(); }
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
        /// 变换前-矩形信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 InitRect { get; set; } = new ROIRectangle2();

        /// <summary>
        /// 矩形信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 TempRect { get; set; } = new ROIRectangle2();

        /// <summary>
        /// 变换后-矩形信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 TranRect { get; set; } = new ROIRectangle2();

        /// <summary>
        /// 矩形卡尺ROI信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 roiRect { get; set; } = new ROIRectangle2();

        [JsonIgnore]
        private double _length1 = 40;
        /// <summary>
        /// 长/2
        /// </summary>
        public double Length1
        {
            get { return _length1; }
            set { SetProperty(ref _length1, value); }
        }

        [JsonIgnore]
        private double _length2 = 10;
        /// <summary>
        /// 宽/2
        /// </summary>
        public double Length2
        {
            get { return _length2; }
            set { SetProperty(ref _length2, value); }
        }

        [JsonIgnore]
        private double _threshold = 30;
        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold
        {
            get { return _threshold; }
            set { SetProperty(ref _threshold, value); }
        }

        [JsonIgnore]
        private double _measDis = 10;
        /// <summary>
        /// 间隔
        /// </summary>
        public double MeasDis
        {
            get { return _measDis; }
            set { SetProperty(ref _measDis, value); }
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
                SetProperty(ref _measMode, value,
                    new Action(() =>
                    {
                        ParamValue = new HTuple();
                        ParamValue.Append(REnum.EnumToStr(_measMode));
                        ParamValue.Append(REnum.EnumToStr(_measSelect));
                        ParamValue.Append(_measDis);
                    }));
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
                SetProperty(ref _measSelect, value,
                    new Action(() =>
                    {
                        ParamValue = new HTuple();
                        ParamValue.Append(REnum.EnumToStr(_measMode));
                        ParamValue.Append(REnum.EnumToStr(_measSelect));
                        ParamValue.Append(_measDis);
                    }));
            }
        }
        #endregion 属性

        #region 构造函数
        public MeasureRectModel()
        {
            ParamName = new HTuple();
            ParamName.Append("measure_transition");
            ParamName.Append("measure_select");
            ParamName.Append("measure_distance");

            ParamValue = new HTuple();
            ParamValue.Append(REnum.EnumToStr(_measMode));
            ParamValue.Append(REnum.EnumToStr(_measSelect));
            ParamValue.Append(_measDis);

            TriggerModuleRun = () =>
            {
                return ExecuteModule().Result;
            };
        }
        #endregion 构造函数

        #region 生命周期
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

                ModuleName = Serial.ToString("D3");
                RefreshResolvedInputImageValue();
                _previewRefreshPending = true;
                RefreshInputImagePreview();
                InitRectMethod();
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
            ReleaseCalibrationRuntime();
            ReleaseOwnedInputImage();
            ClearPreviewDisplayCore();
            base.Dispose();
        }
        #endregion 生命周期

        #region 方法
        /// <summary>
        /// 模块执行。
        /// </summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                Console.WriteLine($"开始加载参数：{DateTime.Now:HH:mm:ss.fff}");
                if (!IsDebug)
                {
                    LoadKeyParam();
                }

                Console.WriteLine($"结束加载参数：{DateTime.Now:HH:mm:ss.fff}");

                NodeStatus status = ExecuteMeasurementCore();
                if (status == NodeStatus.Success)
                {
                    RefreshOutputParamValues();
                    UpdateParam();
                }

                return status;
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：找矩形模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public NodeStatus Run()
        {
            return ExecuteMeasurementCore();
        }

        private NodeStatus ExecuteMeasurementCore()
        {
            try
            {
                if (_inputImage.Value == null)
                {
                    return NodeStatus.None;
                }

                var tempImage = new HImage((HObject)_inputImage.Value).Clone();
                ClearPreviewDrawObjects();
                if (tempImage == null || !tempImage.IsInitialized())
                {
                    return NodeStatus.Error;
                }

                UpdateTransformedMeasureRect();
                bool status = MeasRect2(tempImage, out HXLDCont _, out HObject resultRectContour, out HObject measureContours,
                    TranRect.MidR, TranRect.MidC, TranRect.Phi, TranRect.Length1, TranRect.Length2, MeasDis, Length1, Length2, 1, Threshold,
                    ParamValue[0], ParamValue[1], out HTuple rectRow, out HTuple rectCol, out HTuple rectPhi, out HTuple len1, out HTuple len2,
                    out HTuple rowList, out HTuple colList);

                EnsureMeasurementContour(ref measureContours);

                if (status)
                {
                    UpdateMeasuredRectOutputs(rectRow, rectCol, rectPhi, len1, len2);
                }

                DrawMeasurementResult(status, rowList, colList, resultRectContour, measureContours);
                ShowHRoi();

                if (status)
                {
                    UpdateOutputRectCoordinates(OutRect);
                }

                return status ? NodeStatus.Success : NodeStatus.None;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                ShowHRoi();
                return NodeStatus.Error;
            }
        }

        private void UpdateTransformedMeasureRect()
        {
            if (DisenableAffine2d && HomMat2D_Inverse != null && HomMat2D_Inverse.Length > 0)
            {
                DisenableAffine2d = false;
                Affine2d(HomMat2D_Inverse, TempRect, InitRect);
                if (InitRectChanged_Flag)
                {
                    InitRectLen1 = InitRect.Length1;
                    InitRectLen2 = InitRect.Length2;
                    InitRectPX = InitRect.MidC;
                    InitRectPY = InitRect.MidR;
                    InitRectAngle = InitRect.Phi;
                }
            }

            if (HomMat2D != null && HomMat2D.Length > 0)
            {
                InitRect.Length1 = InitRectLen1;
                InitRect.Length2 = InitRectLen2;
                InitRect.MidC = InitRectPX;
                InitRect.MidR = InitRectPY;
                InitRect.Phi = InitRectAngle;
                Affine2d(HomMat2D, InitRect, TranRect);
                return;
            }

            InitRect.Length1 = TranRect.Length1 = TempRect.Length1;
            InitRect.Length2 = TranRect.Length2 = TempRect.Length2;
            InitRect.MidC = TranRect.MidC = TempRect.MidC;
            InitRect.MidR = TranRect.MidR = TempRect.MidR;
            InitRect.Phi = TranRect.Phi = TempRect.Phi;
        }

        private void UpdateMeasuredRectOutputs(HTuple rectRow, HTuple rectCol, HTuple rectPhi, HTuple len1, HTuple len2)
        {
            double row = RoundHTuple(rectRow);
            double col = RoundHTuple(rectCol);
            double phi = RoundHTuple(rectPhi);
            double length1 = RoundHTuple(len1);
            double length2 = RoundHTuple(len2);

            OutRect.CreateRectangle2(row, col, phi, length1, length2);
            RectDeg = ((HTuple)phi).TupleDeg();
            UpdateOutRectRegion(row, col, phi, length1, length2);
        }

        private void UpdateOutRectRegion(double row, double col, double phi, double length1, double length2)
        {
            HObject rectangleRegion = null;
            try
            {
                HOperatorSet.GenRectangle2(out rectangleRegion, row, col, phi, length1, length2);
                OutRectRegion = new HRegion(rectangleRegion.DeepCopy());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"生成矩形区域输出失败：{ex.Message}");
            }
            finally
            {
                rectangleRegion?.Dispose();
            }
        }

        private static double RoundHTuple(HTuple value)
        {
            return Math.Round(Convert.ToDouble(value.ToString()), 4);
        }

        private void DrawMeasurementResult(bool status, HTuple rowList, HTuple colList, HObject resultRectContour, HObject measureContours)
        {
            if (ShowResultPoint && rowList.ToDArr().Length > 0)
            {
                GenCross(out HObject measureCross, rowList, colList, Length2 / 2, new HTuple(45).TupleRad());
                AddXldPreviewOverlay(measureCross, "red");
            }

            if (ShowMeasContour && measureContours != null && measureContours.IsInitialized())
            {
                AddXldPreviewOverlay(measureContours, "yellow");
            }

            if (ShowResultRect && status)
            {
                AddXldPreviewOverlay(resultRectContour, "green");
            }
        }

        private void EnsureMeasurementContour(ref HObject measureContours)
        {
            if (measureContours != null && measureContours.IsInitialized())
            {
                return;
            }

            DisposeHObject(measureContours);
            measureContours = null;
            HObject fallbackContour = null;
            try
            {
                HOperatorSet.GenRectangle2ContourXld(
                    out fallbackContour,
                    TranRect.MidR,
                    TranRect.MidC,
                    TranRect.Phi,
                    Math.Max(1.0, TranRect.Length1),
                    Math.Max(1.0, TranRect.Length2));
                measureContours = fallbackContour;
                fallbackContour = null;
            }
            catch
            {
            }
            finally
            {
                DisposeHObject(fallbackContour);
            }
        }

        private void RefreshOutputParamValues()
        {
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                item.Value = values[item.ParamName];
            }
        }

        private void SetInputImage(TransmitParam value)
        {
            DisposeOwnedInputImage();
            _inputImage = value ?? new TransmitParam();
            _ownsInputImage = false;
            _previewRefreshPending = true;
            ClearIncomingLinkedInputImageValue();
            EnsureInputImageValueResolved();
            RefreshInputImagePreview();
        }

        private void RefreshInputImagePreview()
        {
            if (!_previewRefreshPending)
            {
                return;
            }

            EnsureInputImageValueResolved();
            HImage previewImage = CloneInputImage();
            if (previewImage == null || !previewImage.IsInitialized())
            {
                if (HasInputImageLink())
                {
                    _previewRefreshPending = true;
                    return;
                }

                ClearPreviewDisplay();
                _previewRefreshPending = false;
                return;
            }

            DisplayPreviewImage(previewImage);
            _previewRefreshPending = false;
            EnsurePreviewRectInitialized();
        }

        private void DisplayPreviewImage(HObject previewImage)
        {
            if (previewImage == null || !previewImage.IsInitialized())
            {
                return;
            }

            try
            {
                long updateVersion = NextPreviewUpdateVersion();
                var dispatcher = PrismProvider.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                {
                    UpdatePreviewImageObject(previewImage);
                    ClearPreviewDrawObjects();
                    RefreshEditableRectPreview();
                    return;
                }

                HImage previewCopy = new HImage(previewImage).CopyImage();
                dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (IsPreviewUpdateCurrent(updateVersion))
                        {
                            UpdatePreviewImageObject(previewCopy);
                            ClearPreviewDrawObjects();
                            RefreshEditableRectPreview();
                        }
                    }
                    finally
                    {
                        previewCopy.Dispose();
                    }
                });
            }
            catch
            {
            }
        }

        private void UpdatePreviewImageObject(HObject previewImage)
        {
            HImage image = previewImage is HImage hImage
                ? hImage.CopyImage()
                : new HImage(previewImage).CopyImage();

            image.GetImageSize(out int width, out int height);
            PreviewImageWidth = width;
            PreviewImageHeight = height;
            SetPreviewImageObject(image);
        }

        private void ClearPreviewDisplay()
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ClearPreviewDisplayCore();
                return;
            }

            dispatcher.BeginInvoke(ClearPreviewDisplayCore);
        }

        private void ClearPreviewDisplayCore()
        {
            ClearPreviewDrawObjects();
            SetPreviewImageObject(null);
            PreviewImageWidth = 1.0;
            PreviewImageHeight = 1.0;
        }

        private void SetPreviewImageObject(HObject image)
        {
            HObject oldImage = _previewImageObject;
            PreviewImageObject = image;
            DisposeHObject(oldImage);
        }

        private void ClearPreviewDrawObjects()
        {
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects.ToList())
            {
                DisposeHObject(drawObject?.Hobject);
            }

            PreviewDrawObjects.Clear();
        }

        private void RemovePreviewObjectsByColor(string color)
        {
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects
                .Where(item => string.Equals(item.Color, color, StringComparison.OrdinalIgnoreCase))
                .ToList())
            {
                DisposeHObject(drawObject.Hobject);
                PreviewDrawObjects.Remove(drawObject);
            }
        }

        private static void DisposeHObject(HObject hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }

        private long NextPreviewUpdateVersion()
        {
            return System.Threading.Interlocked.Increment(ref _previewUpdateVersion);
        }

        private long CurrentPreviewUpdateVersion()
        {
            return System.Threading.Volatile.Read(ref _previewUpdateVersion);
        }

        private bool IsPreviewUpdateCurrent(long updateVersion)
        {
            return updateVersion == CurrentPreviewUpdateVersion();
        }

        private HImage CloneInputImage()
        {
            try
            {
                return CreateOwnedInputImage(_inputImage?.Value);
            }
            catch
            {
                return null;
            }
        }

        private void EnsureInputImageValueResolved()
        {
            if (_inputImage == null)
            {
                return;
            }

            RefreshResolvedInputImageValue();
        }

        private void RefreshResolvedInputImageValue()
        {
            if (_inputImage == null)
            {
                return;
            }

            HImage previousOwnedImage = _ownsInputImage ? _inputImage.Value as HImage : null;
            object imageValue = ResolveLinkedInputImageValue();
            HImage ownedImage = CreateOwnedInputImage(imageValue);
            if (ownedImage == null || !ownedImage.IsInitialized())
            {
                ownedImage?.Dispose();
                if (HasInputImageLink())
                {
                    _inputImage.Value = null;
                }

                previousOwnedImage?.Dispose();
                _ownsInputImage = false;
                return;
            }

            previousOwnedImage?.Dispose();
            _inputImage.Value = ownedImage;
            _ownsInputImage = true;
        }

        private void ClearIncomingLinkedInputImageValue()
        {
            if (_inputImage?.Resourece != ResoureceType.Global
                && _inputImage?.Resourece != ResoureceType.CustomGlobal)
            {
                return;
            }

            _inputImage.Value = null;
        }

        private object ResolveLinkedInputImageValue()
        {
            if (_inputImage == null)
            {
                return null;
            }

            object imageValue = null;
            if (_inputImage.Resourece == ResoureceType.Inupt
                || _inputImage.Resourece == ResoureceType.LastInput)
            {
                imageValue = GetTransmitParam(InputParams, _inputImage);
                if (imageValue != null)
                {
                    return imageValue;
                }

                imageValue = FindLinkedInputImageParam()?.Value;
                if (imageValue != null)
                {
                    return imageValue;
                }
            }

            imageValue = FindCurrentGlobalInputImageParam()?.Value;
            if (imageValue != null)
            {
                return imageValue;
            }

            imageValue = FindCachedInputImageParam()?.Value;
            if (imageValue != null)
            {
                return imageValue;
            }

            return HasInputImageLink() ? null : _inputImage.Value;
        }

        private TransmitParam FindCurrentGlobalInputImageParam()
        {
            if (_inputImage == null)
            {
                return null;
            }

            IEnumerable<TransmitParam> candidates = _inputImage.Resourece switch
            {
                ResoureceType.Global => PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams,
                ResoureceType.CustomGlobal => PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams,
                _ => null
            };

            return FindMatchingTransmitParam(candidates, _inputImage);
        }

        private TransmitParam FindLinkedInputImageParam()
        {
            if (_inputImage == null || InputParams == null)
            {
                return null;
            }

            return FindMatchingTransmitParam(InputParams, _inputImage);
        }

        private TransmitParam FindCachedInputImageParam()
        {
            if (_inputImage == null)
            {
                return null;
            }

            var outputCache = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
            if (outputCache == null)
            {
                return null;
            }

            if (!outputCache.TryGetValue(_inputImage.Serial.ToString(), out ObservableCollection<TransmitParam> cachedParams)
                || cachedParams == null)
            {
                return null;
            }

            return FindMatchingTransmitParam(cachedParams, _inputImage);
        }

        private static TransmitParam FindMatchingTransmitParam(IEnumerable<TransmitParam> candidates, TransmitParam target)
        {
            if (candidates == null || target == null)
            {
                return null;
            }

            return candidates.FirstOrDefault(item => item.Guid == target.Guid)
                ?? candidates.FirstOrDefault(item =>
                    item.Serial == target.Serial
                    && !string.IsNullOrWhiteSpace(item.Name)
                    && item.Name == target.Name)
                ?? candidates.FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(item.ResourcePath)
                    && item.ResourcePath == target.ResourcePath)
                ?? candidates.FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(item.ParamName)
                    && item.ParamName == target.ParamName);
        }

        private bool HasInputImageLink()
        {
            if (_inputImage == null)
            {
                return false;
            }

            return _inputImage.Resourece == ResoureceType.Inupt
                || _inputImage.Resourece == ResoureceType.LastInput
                || _inputImage.Resourece == ResoureceType.Global
                || _inputImage.Resourece == ResoureceType.CustomGlobal;
        }

        private HImage CreateOwnedInputImage(object imageValue)
        {
            try
            {
                switch (imageValue)
                {
                    case HImage inputImage:
                        return TryCopyImage(inputImage, out HImage imageCopy) ? imageCopy : null;
                    case HObject inputObject:
                        using (HImage inputImage = new HImage(inputObject))
                        {
                            return TryCopyImage(inputImage, out HImage objectImageCopy) ? objectImageCopy : null;
                        }
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool TryCopyImage(HImage image, out HImage imageCopy)
        {
            imageCopy = null;
            if (image == null)
            {
                return false;
            }

            try
            {
                if (!image.IsInitialized())
                {
                    return false;
                }

                imageCopy = image.CopyImage();
                if (!TryGetImageSize(imageCopy, out _, out _))
                {
                    imageCopy.Dispose();
                    imageCopy = null;
                    return false;
                }

                return true;
            }
            catch
            {
                imageCopy?.Dispose();
                imageCopy = null;
                return false;
            }
        }

        private static bool TryGetImageSize(HObject image, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (image == null)
            {
                return false;
            }

            try
            {
                HOperatorSet.GetImageSize(image, out HTuple widthTuple, out HTuple heightTuple);
                if (widthTuple.Length == 0 || heightTuple.Length == 0)
                {
                    return false;
                }

                width = widthTuple.I;
                height = heightTuple.I;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DisposeOwnedInputImage()
        {
            ReleaseOwnedInputImage();
        }

        private void ReleaseOwnedInputImage()
        {
            if (_ownsInputImage && _inputImage?.Value is HImage inputImage)
            {
                try
                {
                    inputImage.Dispose();
                }
                catch
                {
                }
            }

            _ownsInputImage = false;
        }
        private void EnsurePreviewRectInitialized()
        {
            if (InitRectLen1 == 50.0
                && InitRectLen2 == 50.0
                && InitRectAngle == 40.0
                && InitRectPX == 40.0
                && InitRectPY == 40.0
                && PreviewImageWidth > 1.0
                && PreviewImageHeight > 1.0)
            {
                InitRectLen1 = PreviewImageWidth / 4.0;
                InitRectLen2 = PreviewImageHeight / 4.0;
                InitRectAngle = 0.0;
                InitRectPX = PreviewImageWidth / 2.0;
                InitRectPY = PreviewImageHeight / 2.0;
            }

            InitRect.Length1 = TempRect.Length1 = TranRect.Length1 = InitRectLen1;
            InitRect.Length2 = TempRect.Length2 = TranRect.Length2 = InitRectLen2;
            InitRect.Phi = TempRect.Phi = TranRect.Phi = InitRectAngle;
            InitRect.MidC = TempRect.MidC = TranRect.MidC = InitRectPX;
            InitRect.MidR = TempRect.MidR = TranRect.MidR = InitRectPY;
        }


        /// <summary>
        /// 对点应用任意加法 2D 变换 矩形2
        /// </summary>
        public static void Affine2d(HTuple HomMat2D, ROIRectangle2 intRect, ROIRectangle2 tranRect)
        {
            HHomMat2D TempHomMat2D = new HHomMat2D(HomMat2D);
            tranRect.Length1 = intRect.Length1;
            tranRect.Length2 = intRect.Length2;
            HTuple X0 = new HTuple();
            double _Phi1, _Phi2, _Phi3, _Phi;
            tranRect.MidR = TempHomMat2D.AffineTransPoint2d(intRect.MidR, intRect.MidC, out X0);
            tranRect.MidC = X0;
            _Phi1 = ((HTuple)TempHomMat2D[0]).TupleAcos().D;
            _Phi2 = ((HTuple)TempHomMat2D[1]).TupleAsin().D;
            _Phi3 = ((HTuple)TempHomMat2D[4]).TupleAcos().D;
            _Phi = _Phi2 <= 0 ? _Phi1 : -_Phi3;
            tranRect._Phi = intRect.Phi - _Phi;
        }


        public void InitRectMethod()
        {
            EnsurePreviewRectInitialized();
            RefreshEditableRectPreview();
        }

        public MeasureRectPreviewRect GetPreviewRect()
        {
            EnsurePreviewRectInitialized();
            return new MeasureRectPreviewRect(
                InitRectPX,
                InitRectPY,
                Math.Max(1.0, InitRectLen1),
                Math.Max(1.0, InitRectLen2),
                InitRectAngle);
        }

        public void ApplyPreviewRect(MeasureRectPreviewRect rect, bool runMeasurement, double imageUnitsPerScreenPixel = 1.0)
        {
            InitRectChanged_Flag = true;
            try
            {
                InitRectLen1 = Math.Round(Math.Max(1.0, rect.Length1), 3);
                InitRectLen2 = Math.Round(Math.Max(1.0, rect.Length2), 3);
                InitRectPX = Math.Round(rect.CenterX, 3);
                InitRectPY = Math.Round(rect.CenterY, 3);
                InitRectAngle = Math.Round(rect.Angle, 6);

                InitRect.Length1 = TempRect.Length1 = TranRect.Length1 = InitRectLen1;
                InitRect.Length2 = TempRect.Length2 = TranRect.Length2 = InitRectLen2;
                InitRect.MidC = TempRect.MidC = TranRect.MidC = InitRectPX;
                InitRect.MidR = TempRect.MidR = TranRect.MidR = InitRectPY;
                InitRect.Phi = TempRect.Phi = TranRect.Phi = InitRectAngle;
            }
            finally
            {
                InitRectChanged_Flag = false;
            }

            RefreshEditableRectPreview(imageUnitsPerScreenPixel);
            if (runMeasurement)
            {
                Run();
            }
        }


        private void InitRectChanged()
        {
            if (InitRectChanged_Flag == true)
                return;

            InitRect.Length1 = InitRectLen1;
            InitRect.Length2 = InitRectLen2;
            InitRect.Phi = InitRectAngle;
            InitRect.MidC = InitRectPX;
            InitRect.MidR = InitRectPY;
            DisenableAffine2d = true;
            if (roiRect != null)
            {
                if (DisenableAffine2d && HomMat2D != null && HomMat2D.Length > 0)
                {
                    Affine2d(HomMat2D, InitRect, TempRect);
                    if (InitRectChanged_Flag)
                    {
                        roiRect.Length1 = TempRect.Length1;
                        roiRect.Length2 = TempRect.Length2;
                        roiRect.Phi = TempRect.Phi;
                        roiRect.MidC = TempRect.MidC;
                        roiRect.MidR = TempRect.MidR;
                    }
                }
                else
                {
                    roiRect.Length1 = InitRect.Length1;
                    roiRect.Length2 = InitRect.Length2;
                    roiRect.Phi = InitRect.Phi;
                    roiRect.MidC = InitRect.MidC;
                    roiRect.MidR = InitRect.MidR;
                    TempRect.Length1 = InitRect.Length1;
                    TempRect.Length2 = InitRect.Length2;
                    TempRect.Phi = InitRect.Phi;
                    TempRect.MidC = InitRect.MidC;
                    TempRect.MidR = InitRect.MidR;
                }
                Run();
                InitRectMethod();
            }
        }

        public bool ValidateCalibrationConfig(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!EnableCalibration)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(CalibrationFilePath))
            {
                errorMessage = "启用标定后必须选择标定文件。";
                return false;
            }

            if (!File.Exists(CalibrationFilePath))
            {
                errorMessage = "标定文件不存在，请重新选择有效文件。";
                return false;
            }

            return true;
        }

        private void UpdateOutputRectCoordinates(ROIRectangle2 rect)
        {
            double centerX = rect.MidC;
            double centerY = rect.MidR;
            double length1 = rect.Length1;
            double length2 = rect.Length2;
            double angle = rect.Phi;

            if (EnableCalibration &&
                EnsureCalibrationRuntimeReady() &&
                TryConvertPointToBoard(rect.MidC, rect.MidR, out double boardCenterX, out double boardCenterY) &&
                TryBuildWorldCorners(rect, out List<(double X, double Y)> worldCorners) &&
                RectangleCalibrationMath.TryCalculateWorldRectangle(worldCorners, out double boardLength1, out double boardLength2, out double boardAngle))
            {
                centerX = boardCenterX;
                centerY = boardCenterY;
                length1 = boardLength1;
                length2 = boardLength2;
                angle = boardAngle;
                SetCalibrationState(true, _calibrationCameraId);
            }
            else if (!EnableCalibration)
            {
                SetCalibrationState(false);
            }

            OutRectLen1 = Math.Round(length1, 4);
            OutRectLen2 = Math.Round(length2, 4);
            OutRectPX = Math.Round(centerX, 4);
            OutRectPY = Math.Round(centerY, 4);
            OutRectAngle = Math.Round(angle, 4);
        }

        private bool TryBuildWorldCorners(ROIRectangle2 rect, out List<(double X, double Y)> worldCorners)
        {
            worldCorners = new List<(double X, double Y)>(4);

            if (rect.Length1 <= 0 || rect.Length2 <= 0)
            {
                return false;
            }

            double axis1X = Math.Cos(rect.Phi) * rect.Length1;
            double axis1Y = Math.Sin(rect.Phi) * rect.Length1;
            double axis2X = -Math.Sin(rect.Phi) * rect.Length2;
            double axis2Y = Math.Cos(rect.Phi) * rect.Length2;

            var pixelCorners = new (double X, double Y)[]
            {
                (rect.MidC - axis1X - axis2X, rect.MidR - axis1Y - axis2Y),
                (rect.MidC + axis1X - axis2X, rect.MidR + axis1Y - axis2Y),
                (rect.MidC + axis1X + axis2X, rect.MidR + axis1Y + axis2Y),
                (rect.MidC - axis1X + axis2X, rect.MidR - axis1Y + axis2Y)
            };

            for (int i = 0; i < pixelCorners.Length; i++)
            {
                if (!TryConvertPointToBoard(pixelCorners[i].X, pixelCorners[i].Y, out double boardX, out double boardY))
                {
                    return false;
                }

                worldCorners.Add((boardX, boardY));
            }

            return worldCorners.Count == 4;
        }

        private bool EnsureCalibrationRuntimeReady()
        {
            if (!EnableCalibration)
            {
                SetCalibrationState(false);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CalibrationFilePath) || !File.Exists(CalibrationFilePath))
            {
                SetCalibrationState(false);
                return false;
            }

            if (_cameraCalib != null && !string.IsNullOrWhiteSpace(_calibrationCameraId))
            {
                SetCalibrationState(true, _calibrationCameraId);
                return true;
            }

            try
            {
                ReleaseCalibrationRuntime();
                _cameraCalib = new CameraCalibrationSdk();
                _cameraCalib.loadCalibrationFile(CalibrationFilePath);

                CameraCalibrationSdk.CameraParams cameraParams = default(CameraCalibrationSdk.CameraParams);
                _cameraCalib.getCameraParams(ref cameraParams);

                if (string.IsNullOrWhiteSpace(cameraParams.cameraId))
                {
                    ReleaseCalibrationRuntime();
                    SetCalibrationState(false);
                    return false;
                }

                SetCalibrationState(true, cameraParams.cameraId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载标定文件失败：{ex.Message}");
                ReleaseCalibrationRuntime();
                SetCalibrationState(false);
                return false;
            }
        }

        private bool TryConvertPointToBoard(double pixelX, double pixelY, out double boardX, out double boardY)
        {
            boardX = pixelX;
            boardY = pixelY;

            if (_cameraCalib == null || string.IsNullOrWhiteSpace(_calibrationCameraId))
            {
                SetCalibrationState(false);
                return false;
            }

            try
            {
                double boardZ;
                _cameraCalib.pixelToWorld(_calibrationCameraId, pixelX, pixelY, out boardX, out boardY, out boardZ);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"标定坐标转换失败：{ex.Message}");
                SetCalibrationState(false);
                return false;
            }
        }

        private void DisableCalibration()
        {
            ReleaseCalibrationRuntime();
            SetCalibrationState(false);

            if (!string.IsNullOrWhiteSpace(_calibrationFilePath))
            {
                _calibrationFilePath = string.Empty;
                RaisePropertyChanged(nameof(CalibrationFilePath));
            }
        }

        private void InvalidateCalibrationRuntime()
        {
            ReleaseCalibrationRuntime();
            SetCalibrationState(false);
        }

        private void ReleaseCalibrationRuntime()
        {
            try
            {
                _cameraCalib?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放标定资源失败：{ex.Message}");
            }
            finally
            {
                _cameraCalib = null;
                _calibrationCameraId = string.Empty;
            }
        }

        private void SetCalibrationState(bool isEffective, string cameraId = "")
        {
            cameraId = cameraId ?? string.Empty;

            bool effectiveChanged = _isCalibrationEffective != isEffective;
            bool cameraChanged = !string.Equals(_calibrationCameraId, cameraId, StringComparison.Ordinal);

            _isCalibrationEffective = isEffective;
            _calibrationCameraId = cameraId;

            if (effectiveChanged)
            {
                RaisePropertyChanged(nameof(IsCalibrationEffective));
            }

            if (effectiveChanged || cameraChanged)
            {
                RaisePropertyChanged(nameof(CalibrationStatusText));
            }
        }

        public static bool MeasRect2(
            HObject ho_Image,
            out HXLDCont ho_Arrow,
            out HObject ho_Rectangle2Contour,
            out HObject ho_ruleContours,
            HTuple hv_Row,
            HTuple hv_Column,
            HTuple hv_Phi,
            HTuple hv_Length1,
            HTuple hv_Length2,
            HTuple hv_MeasureCliperNum,
            HTuple hv_MeasureLength1,
            HTuple hv_MeasureLength2,
            HTuple hv_MeasureSigma,
            HTuple hv_MeasureThreshold,
            HTuple hv_MeasureTransition,
            HTuple hv_MeasureSelect,
            out HTuple hv_RectRow,
            out HTuple hv_RectCol,
            out HTuple hv_RectPhi,
            out HTuple hv_Len1,
            out HTuple hv_Len2,
            out HTuple hv_Rows,
            out HTuple hv_Columns
        )
        {
            try
            {
                HTuple hv_RowEx = null,
                    hv_ColEx = null,
                    hv_beginRow = null;
                HTuple hv_beginCol = null,
                    hv_EndRow = null,
                    hv_EndCol = null;
                HTuple hv_MetrologyHandle = null,
                    hv_Width = null,
                    hv_Height = null;
                HTuple hv_Index = null,
                    hv_Rectangle2Parameter = null;
                // Initialize local and output iconic variables
                //HOperatorSet.GenEmptyObj(out ho_Arrow);
                HOperatorSet.GenEmptyObj(out ho_Rectangle2Contour);
                HOperatorSet.GenEmptyObj(out ho_ruleContours);
                hv_RectRow = new HTuple();
                hv_RectCol = new HTuple();
                hv_RectPhi = new HTuple();
                hv_Len1 = new HTuple();
                hv_Len2 = new HTuple();
                hv_RowEx = hv_Row - ((hv_Phi.TupleSin()) * hv_Length1);
                hv_ColEx = hv_Column + ((hv_Phi.TupleCos()) * hv_Length1);

                hv_beginRow = hv_RowEx + ((hv_Phi.TupleSin()) * hv_MeasureLength1);
                hv_beginCol = hv_ColEx - ((hv_Phi.TupleCos()) * hv_MeasureLength1);
                hv_EndRow = hv_RowEx - ((hv_Phi.TupleSin()) * hv_MeasureLength1);
                hv_EndCol = hv_ColEx + ((hv_Phi.TupleCos()) * hv_MeasureLength1);
                //ho_Arrow.Dispose();
                GenArrow(
                    out ho_Arrow,
                    hv_beginRow,
                    hv_beginCol,
                    hv_EndRow,
                    hv_EndCol,
                    hv_MeasureLength2 * 2,
                    hv_MeasureLength2 * 2
                );
                //创建2维测量
                HOperatorSet.CreateMetrologyModel(out hv_MetrologyHandle);
                HOperatorSet.GetImageSize(ho_Image, out hv_Width, out hv_Height);
                HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, hv_Width, hv_Height);
                //加载方向矩形2维测量
                HOperatorSet.AddMetrologyObjectRectangle2Measure(
                    hv_MetrologyHandle,
                    hv_Row,
                    hv_Column,
                    hv_Phi,
                    hv_Length1,
                    hv_Length2,
                    hv_MeasureLength1,
                    hv_MeasureLength2,
                    hv_MeasureSigma,
                    hv_MeasureThreshold,
                    new HTuple(),
                    new HTuple(),
                    out hv_Index
                );
                //卡尺搜索模式 positive：白到黑   negative：黑到白
                HOperatorSet.SetMetrologyObjectParam(
                    hv_MetrologyHandle,
                    "all",
                    "measure_transition",
                    hv_MeasureTransition
                );
                //卡尺选择边缘点
                HOperatorSet.SetMetrologyObjectParam(
                    hv_MetrologyHandle,
                    "all",
                    "measure_select",
                    hv_MeasureSelect
                );
                //卡尺间隔
                HOperatorSet.SetMetrologyObjectParam(
                    hv_MetrologyHandle,
                    "all",
                    "measure_distance",
                    hv_MeasureCliperNum
                );
                //图像加载到2维测量中
                HOperatorSet.ApplyMetrologyModel(ho_Image, hv_MetrologyHandle);
                //拟合线结果
                HOperatorSet.GetMetrologyObjectResult(
                    hv_MetrologyHandle,
                    "all",
                    "all",
                    "result_type",
                    "all_param",
                    out hv_Rectangle2Parameter
                );

                bool status = false;

                if (hv_Rectangle2Parameter.TupleLength() >= 5)
                {
                    hv_RectRow = hv_Rectangle2Parameter[0];
                    hv_RectCol = hv_Rectangle2Parameter[1];
                    hv_RectPhi = hv_Rectangle2Parameter[2];
                    hv_Len1 = hv_Rectangle2Parameter[3];
                    hv_Len2 = hv_Rectangle2Parameter[4];

                    status = true;
                }
                //拟合方向矩形图形
                ho_Rectangle2Contour.Dispose();
                HOperatorSet.GetMetrologyObjectResultContour(
                    out ho_Rectangle2Contour,
                    hv_MetrologyHandle,
                    "all",
                    "all",
                    1.5
                );
                //卡尺方向矩形图形
                ho_ruleContours.Dispose();
                HOperatorSet.GetMetrologyObjectMeasures(
                    out ho_ruleContours,
                    hv_MetrologyHandle,
                    "all",
                    "all",
                    out hv_Rows,
                    out hv_Columns
                );
                HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);

                return status;
            }
            catch (Exception ex)
            {
                ho_Arrow = new HXLDCont();
                ho_Rectangle2Contour = new HObject();
                ho_ruleContours = new HObject();
                hv_RectRow = 0;
                hv_RectCol = 0;
                hv_RectPhi = 0;
                hv_Len1 = 0;
                hv_Len2 = 0;
                hv_Rows = 0;
                hv_Columns = 0;
                Debug.Write(ex.Message);

                return false;
            }
        }

        /// <summary>创建箭头 XLD。</summary>
        public static void GenArrow(
            out HXLDCont ho_Arrow,
            HTuple hv_Row1,
            HTuple hv_Column1,
            HTuple hv_Row2,
            HTuple hv_Column2,
            HTuple hv_HeadLength,
            HTuple hv_HeadWidth)
        {
            ho_Arrow = new HXLDCont();
            HObject arrowObjects = null;
            HObject tempArrow = null;
            HObject oldArrowObjects = null;
            try
            {
                HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out HTuple lengths);
                HOperatorSet.GenEmptyObj(out arrowObjects);

                int count = Math.Max(1, lengths.TupleLength());
                for (int index = 0; index < count; index++)
                {
                    double row1 = SelectTupleDouble(hv_Row1, index);
                    double col1 = SelectTupleDouble(hv_Column1, index);
                    double row2 = SelectTupleDouble(hv_Row2, index);
                    double col2 = SelectTupleDouble(hv_Column2, index);
                    double length = SelectTupleDouble(lengths, index);

                    DisposeHObject(tempArrow);
                    tempArrow = length <= double.Epsilon
                        ? CreateArrowPoint(row1, col1)
                        : CreateArrowContour(
                            row1,
                            col1,
                            row2,
                            col2,
                            length,
                            SelectTupleDouble(hv_HeadLength, index),
                            SelectTupleDouble(hv_HeadWidth, index));

                    oldArrowObjects = arrowObjects;
                    arrowObjects = oldArrowObjects.ConcatObj(tempArrow);
                    DisposeHObject(oldArrowObjects);
                    oldArrowObjects = null;
                }

                ho_Arrow = new HXLDCont(arrowObjects);
            }
            catch
            {
                ho_Arrow = new HXLDCont();
            }
            finally
            {
                DisposeHObject(tempArrow);
                DisposeHObject(oldArrowObjects);
                DisposeHObject(arrowObjects);
            }
        }

        private static HObject CreateArrowPoint(double row, double column)
        {
            HOperatorSet.GenContourPolygonXld(out HObject pointContour, new HTuple(row), new HTuple(column));
            return pointContour;
        }

        private static HObject CreateArrowContour(
            double row1,
            double column1,
            double row2,
            double column2,
            double length,
            double headLength,
            double headWidth)
        {
            double rowDirection = (row2 - row1) / length;
            double colDirection = (column2 - column1) / length;
            double halfHeadWidth = headWidth / 2.0;
            double headBaseRow = row1 + (length - headLength) * rowDirection;
            double headBaseCol = column1 + (length - headLength) * colDirection;

            double rowP1 = headBaseRow + halfHeadWidth * colDirection;
            double colP1 = headBaseCol - halfHeadWidth * rowDirection;
            double rowP2 = headBaseRow - halfHeadWidth * colDirection;
            double colP2 = headBaseCol + halfHeadWidth * rowDirection;

            HOperatorSet.GenContourPolygonXld(
                out HObject arrowContour,
                new HTuple(new[] { row1, row2, rowP1, row2, rowP2, row2 }),
                new HTuple(new[] { column1, column2, colP1, column2, colP2, column2 }));
            return arrowContour;
        }

        private static double SelectTupleDouble(HTuple tuple, int index)
        {
            if (tuple == null || tuple.Length == 0)
            {
                return 0.0;
            }

            int selectedIndex = tuple.Length == 1 ? 0 : Math.Min(index, tuple.Length - 1);
            return tuple.TupleSelect(selectedIndex).D;
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


        private void AddXldPreviewOverlay(HObject contourObject, string color)
        {
            if (contourObject == null || !contourObject.IsInitialized())
            {
                return;
            }

            try
            {
                PreviewDrawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = contourObject.Clone(),
                    Color = color,
                    IsFillDisplay = false
                });
            }
            catch
            {
            }
        }

        public void RefreshEditableRectPreview(double imageUnitsPerScreenPixel = 1.0)
        {
            RemovePreviewObjectsByColor(EditableRectColor);
            EnsurePreviewRectInitialized();

            HObject rectContour = null;
            HObject rotateGuideContour = null;
            HObject handleContours = null;
            HXLDCont arrowContour = null;
            try
            {
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                HOperatorSet.GenRectangle2ContourXld(
                    out rectContour,
                    InitRectPY,
                    InitRectPX,
                    InitRectAngle,
                    Math.Max(1.0, InitRectLen1),
                    Math.Max(1.0, InitRectLen2));

                AddXldPreviewOverlay(rectContour, EditableRectColor);
                if (CreatePreviewRotateGuide(out rotateGuideContour))
                {
                    AddXldPreviewOverlay(rotateGuideContour, EditableRectColor);
                }

                if (CreatePreviewDirectionArrow(out arrowContour))
                {
                    AddXldPreviewOverlay(arrowContour, EditableRectColor);
                }

                if (CreatePreviewHandles(imageUnitsPerScreenPixel, out handleContours))
                {
                    AddXldPreviewOverlay(handleContours, EditableRectColor);
                }
            }
            catch
            {
            }
            finally
            {
                DisposeHObject(rectContour);
                DisposeHObject(rotateGuideContour);
                DisposeHObject(handleContours);
                DisposeHObject(arrowContour);
            }
        }

        private bool CreatePreviewRotateGuide(out HObject rotateGuideContour)
        {
            rotateGuideContour = null;
            try
            {
                var rect = GetPreviewRect();
                var handles = MeasureRectPreviewGeometry.GetHandlePoints(rect);
                var right = handles[MeasureRectPreviewHandle.Right];
                var rotate = handles[MeasureRectPreviewHandle.Rotate];
                HOperatorSet.GenContourPolygonXld(
                    out rotateGuideContour,
                    new HTuple(right.Y, rotate.Y),
                    new HTuple(right.X, rotate.X));
                return rotateGuideContour != null && rotateGuideContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(rotateGuideContour);
                rotateGuideContour = null;
                return false;
            }
        }

        private bool CreatePreviewHandles(double imageUnitsPerScreenPixel, out HObject handleContours)
        {
            handleContours = null;
            HObject tempContour = null;
            try
            {
                var rect = GetPreviewRect();
                var handles = MeasureRectPreviewGeometry.GetHandlePoints(rect);
                foreach (var pair in handles)
                {
                    double screenSize = pair.Key == MeasureRectPreviewHandle.Center
                        ? MeasureRectPreviewStyle.CenterHandleScreenSize
                        : MeasureRectPreviewStyle.HandleScreenSize;
                    double radius = Math.Max(1.0, screenSize * imageUnitsPerScreenPixel / 2.0);

                    HOperatorSet.GenCircleContourXld(
                        out tempContour,
                        pair.Value.Y,
                        pair.Value.X,
                        radius,
                        0,
                        Math.PI * 2.0,
                        "positive",
                        Math.Max(1.0, imageUnitsPerScreenPixel));

                    if (handleContours == null || !handleContours.IsInitialized())
                    {
                        handleContours = tempContour.Clone();
                    }
                    else
                    {
                        HObject oldContours = handleContours;
                        handleContours = oldContours.ConcatObj(tempContour);
                        DisposeHObject(oldContours);
                    }
                    DisposeHObject(tempContour);
                    tempContour = null;
                }

                return handleContours != null && handleContours.IsInitialized();
            }
            catch
            {
                DisposeHObject(handleContours);
                handleContours = null;
                return false;
            }
            finally
            {
                DisposeHObject(tempContour);
            }
        }

        private bool CreatePreviewDirectionArrow(out HXLDCont arrowContour)
        {
            arrowContour = null;
            try
            {
                // HALCON 角度投影到图像显示时，Row 方向与数学 Y 轴相反。
                double axisX = Math.Cos(InitRectAngle);
                double axisY = -Math.Sin(InitRectAngle);
                double headSize = Math.Max(8.0, Math.Min(18.0, Math.Min(InitRectLen1, InitRectLen2) * 0.25));
                double arrowLength = Math.Min(Math.Max(InitRectLen1 * 0.65, headSize * 2.0), 80.0);
                double tipDistance = Math.Max(headSize, InitRectLen1 - headSize * 0.9);

                double tipCol = InitRectPX + axisX * tipDistance;
                double tipRow = InitRectPY + axisY * tipDistance;
                double startCol = tipCol - axisX * arrowLength;
                double startRow = tipRow - axisY * arrowLength;

                GenArrow(
                    out arrowContour,
                    startRow,
                    startCol,
                    tipRow,
                    tipCol,
                    headSize,
                    headSize);

                return arrowContour != null && arrowContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(arrowContour);
                arrowContour = null;
                return false;
            }
        }


        public void ShowHRoi()
        {
            RefreshEditableRectPreview();
        }

        #endregion 方法

    }
}

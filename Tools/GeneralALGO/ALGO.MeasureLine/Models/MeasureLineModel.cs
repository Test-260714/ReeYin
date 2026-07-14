using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Prism.Events;
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
using ReeYin_V.Core.Calibration;
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
using System.Windows.Forms;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.MeasureLine
{
    [Serializable]
    public class MeasureLineModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        private CameraCalibrationSdk _cameraCalib;

        [JsonIgnore]
        private string _calibrationCameraId = string.Empty;

        [JsonIgnore]
        private bool _isDisposed;

        [JsonIgnore]
        private int _isExecuting;

        [JsonIgnore]
        public bool IsDebug { get; set; } = false;

        private const string EditableLineColor = "blue";

        [JsonIgnore]
        private HObject _previewImageObject;

        [JsonIgnore]
        private double _previewImageWidth = 1.0;

        [JsonIgnore]
        private double _previewImageHeight = 1.0;

        [JsonIgnore]
        private bool _previewRefreshPending = true;

        [JsonIgnore]
        private long _previewUpdateVersion;

        [JsonIgnore]
        private bool _isRefreshingEditableLinePreview;

        #endregion

        #region Properties
        [JsonIgnore]
        private HImage _showImg;
        /// <summary>
        /// 展示的图片
        /// </summary>
        [JsonIgnore]
        public HImage ShowImg
        {
            get { return _showImg; }
            set { _showImg = value; RaisePropertyChanged(); }
        }

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
                _inputImage = value ?? new TransmitParam();
                _previewRefreshPending = true;
                RefreshInputImagePreview();
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
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new ObservableCollection<HalconDrawingObject>();

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
        private bool _initLineChanged_Flag = false;
        /// <summary>
        /// InitLineChanged_Flag
        /// </summary>
        public bool InitLineChanged_Flag
        {
            get { return _initLineChanged_Flag; }
            set { _initLineChanged_Flag = value; 
                  RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _showResultPoint = true;
        /// <summary>显示结果点</summary>
        [JsonIgnore]
        public bool ShowResultPoint
        {
            get { return _showResultPoint; }
            set { SetProperty(ref _showResultPoint, value); }
        }

        [JsonIgnore]
        private bool _showMeasContour = true;
        /// <summary>显示测量轮廓 </summary>
        public bool ShowMeasContour
        {
            get { return _showMeasContour; }
            set { SetProperty(ref _showMeasContour, value); }
        }

        [JsonIgnore]
        private bool _showResultLine = true;
        /// <summary>显示结果直线 </summary>
        public bool ShowResultLine
        {
            get { return _showResultLine; }
            set { SetProperty(ref _showResultLine, value); }
        }

        [JsonIgnore]
        //绘制的区域列表
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        [JsonIgnore]
        private ROILine _line = new ROILine();
        /// <summary>
        /// 测量得到的线段
        /// </summary>
        [JsonIgnore]
        public ROILine Line
        {
            get { return _line; }
            set { SetProperty(ref _line, value); }
        }

        [JsonIgnore]
        private Line _outLine;
        /// <summary>输出直线</summary>
        [OutputParam("OutLine", "输出线段")]
        public Line OutLine
        {
            get { return _outLine; }
            set { SetProperty(ref _outLine, value); }
        }

        [JsonIgnore]
        private double? _outStartX;
        /// <summary>输出线段起点X坐标</summary>
        [OutputParam("OutStartX", "线段起点X坐标")]
        public double? OutStartX
        {
            get { return _outStartX; }
            set { SetProperty(ref _outStartX, value); }
        }

        [JsonIgnore]
        private double? _outStartY;
        /// <summary>输出线段起点Y坐标</summary>
        [OutputParam("OutStartY", "线段起点Y坐标")]
        public double? OutStartY
        {
            get { return _outStartY; }
            set { SetProperty(ref _outStartY, value); }
        }

        [JsonIgnore]
        private double? _outEndX;
        /// <summary>输出线段终点X坐标</summary>
        [OutputParam("OutEndX", "线段终点X坐标")]
        public double? OutEndX
        {
            get { return _outEndX; }
            set { SetProperty(ref _outEndX, value); }
        }

        [JsonIgnore]
        private double? _outEndY;
        /// <summary>输出线段终点Y坐标</summary>
        [OutputParam("OutEndY", "线段终点Y坐标")]
        public double? OutEndY
        {
            get { return _outEndY; }
            set { SetProperty(ref _outEndY, value); }
        }

        [JsonIgnore]
        private double? _outMidX;
        /// <summary>输出线段中点X坐标</summary>
        [OutputParam("OutMidX", "线段中点X坐标")]
        public double? OutMidX
        {
            get { return _outMidX; }
            set { SetProperty(ref _outMidX, value); }
        }

        [JsonIgnore]
        private double? _outMidY;
        /// <summary>输出线段中点Y坐标</summary>
        [OutputParam("OutMidY", "线段中点Y坐标")]
        public double? OutMidY
        {
            get { return _outMidY; }
            set { SetProperty(ref _outMidY, value); }
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
        private double _initLineStartX = 50.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double InitLineStartX
        {
            get { return _initLineStartX; }
            set { _initLineStartX = value; RaisePropertyChanged();
                InitLineChanged();
            }
        }

        [JsonIgnore]
        private double _initLineStartY = 50.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double InitLineStartY
        {
            get { return _initLineStartY; }
            set { _initLineStartY = value; RaisePropertyChanged();
                    InitLineChanged();
            }
        }

        [JsonIgnore]
        private double _initLineEndX = 50.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double InitLineEndX
        {
            get { return _initLineEndX; }
            set { _initLineEndX = value; RaisePropertyChanged();
                    InitLineChanged();
            }
        }

        [JsonIgnore]
        private double _initLineEndY = 50.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double InitLineEndY
        {
            get { return _initLineEndY; }
            set { _initLineEndY = value; RaisePropertyChanged();
                    InitLineChanged();
            }
        }

        [JsonIgnore]
        private double _offsetX = 0.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; RaisePropertyChanged();
                    InitLineChanged();
            }
        }

        [JsonIgnore]
        private double _offsetY = 0.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; RaisePropertyChanged();
                InitLineChanged(); 
            }
        }

        [JsonIgnore]
        private double _compensationAngle = 0.0;
        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        public double CompensationAngle
        {
            get { return _compensationAngle; }
            set { _compensationAngle = value; RaisePropertyChanged(); 
                 InitLineChanged();
            }
        }


        /// <summary>
        /// 变换前-直线信息
        /// </summary>
        [JsonIgnore]
        public ROILine InitLine { get; set; } = new ROILine();

        /// <summary>
        /// 直线信息
        /// </summary>
        [JsonIgnore]
        public ROILine TempLine { get; set; } = new ROILine();

        /// <summary>
        /// 变换后-直线信息
        /// </summary>
        [JsonIgnore]
        public ROILine TranLine { get; set; } = new ROILine();

        /// <summary>
        /// 直线卡尺ROI信息
        /// </summary>
        [JsonIgnore]
        public ROILine roiLine { get; set; } = new ROILine();

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        private string _runtimeModuleKey;

        [JsonIgnore]
        private string _windowControlKey;

        [JsonIgnore]
        public string RuntimeModuleKey
        {
            get
            {
                if (Serial >= 0)
                {
                    return Serial.ToString("D3");
                }

                if (string.IsNullOrWhiteSpace(_runtimeModuleKey))
                {
                    _runtimeModuleKey = $"MeasureLine_{System.Guid.NewGuid():N}";
                }

                return _runtimeModuleKey;
            }
        }

        [JsonIgnore]
        private double _length1 = 40;
        /// <summary>
        /// 长
        /// </summary>
        public double Length1
        {
            get { return _length1; }
            set { SetProperty(ref _length1, value); }
        }

        [JsonIgnore]
        private double _length2 = 10;
        /// <summary>
        /// 宽
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
            set
            {
                SetProperty(ref _measDis, Math.Max(0.001, value), new Action(() =>
                {
                    RebuildMeasureParamValue();
                }));
            }
        }

        [JsonIgnore]
        private HTuple _paramName;
        /// <summary>
        /// 参数名
        /// </summary>
        [JsonIgnore]
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
        [JsonIgnore]
        public HTuple ParamValue
        {
            get { return _paramValue; }
            set { SetProperty(ref _paramValue, value); }
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
                        RebuildMeasureParamValue();
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
                        RebuildMeasureParamValue();
                    }));
            }
        }
        #endregion

        #region Constructor
        public MeasureLineModel()
        {
            EnsureMeasureParametersInitialized(true);
        }

        ~MeasureLineModel()
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
                    return false;

                EnsureMeasureParametersInitialized();

                DisposeCachedInputImage();
                var temp = ResolveTransmitParamValue(InputParams, _inputImage, false);
                _inputImage.Value = CreateOwnedInputImage(temp);
                _previewRefreshPending = true;
                if (IsDebug)
                {
                    RefreshInputImagePreview();
                }
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
            if (_isDisposed)
                return;

            _isDisposed = true;

            string moduleKey = !string.IsNullOrWhiteSpace(_windowControlKey)
                ? _windowControlKey
                : RuntimeModuleKey;
            ReleaseCalibrationRuntime();
            DisposeCachedInputImage();

            try
            {
                mWindowH?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放窗口控件失败：{ex.Message}");
            }
            finally
            {
                mWindowH = null;
            }

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.ImgControlPair != null && !string.IsNullOrWhiteSpace(moduleKey))
            {
                solutionItem.ImgControlPair.Remove(moduleKey);
            }

            _windowControlKey = null;
            _runtimeModuleKey = null;

            base.Dispose();
        }
        #endregion

        int Count = 0;
        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            if (Interlocked.Exchange(ref _isExecuting, 1) == 1)
            {
                return Output = new ExecuteModuleOutput()
                {
                    RunStatus = NodeStatus.None,
                    RunTime = 0,
                };
            }

            try
            {
                var (result, time) = SetTimeHelper.SetTimer(() =>
                {
                    try
                    {
                        ResetOutputs();
                        // 先把本模块输出参数刷成 null，保证任何早退分支都不残留上次结果
                        SyncOutputParams();

                        #region 检测参数（对链接参数重新赋值）
                        Console.WriteLine($"开始加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                        if (!IsDebug && !LoadKeyParam())
                            return NodeStatus.Error;
                        Console.WriteLine($"结束加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                        #endregion

                        EnsureMeasureParametersInitialized();
                        using var tempImage = CloneInputImage();
                        if (tempImage == null)
                            return NodeStatus.None;

                        mHRoi.Clear();
                        ClearPreviewDrawObjects();
                        if (!tempImage.IsInitialized())
                            return NodeStatus.Error;

                        InitLine.StartX = TranLine.StartX = TempLine.StartX;
                        InitLine.StartY = TranLine.StartY = TempLine.StartY;
                        InitLine.EndX = TranLine.EndX = TempLine.EndX;
                        InitLine.EndY = TranLine.EndY = TempLine.EndY;

                        bool status = MeasLine(tempImage, TranLine, Line, out HTuple RowList, out HTuple ColList, out HXLDCont m_MeasXLD, null);
                        if (ShowResultPoint && RowList.ToDArr().Length > 0) //显示结果点
                        {
                            GenCross(out HObject m_MeasCross, RowList, ColList, Length2/2, new HTuple(45).TupleRad());
                            try
                            {
                                AddXldPreviewOverlay(m_MeasCross, "red");
                            }
                            finally
                            {
                                m_MeasCross.Dispose();
                            }
                        }
                        if (ShowResultLine && RowList.ToDArr().Length > 0 && status) //显示结果线
                        {
                            GenContour(out HObject m_ResultXLD, Line.StartY, Line.StartX, Line.EndY, Line.EndX);
                            try
                            {
                                AddXldPreviewOverlay(m_ResultXLD, "green");
                            }
                            finally
                            {
                                m_ResultXLD.Dispose();
                            }
                        }
                        if (ShowMeasContour) //显示检测范围
                        {
                            try
                            {
                                if (m_MeasXLD != null && m_MeasXLD.IsInitialized())
                                {
                                    AddXldPreviewOverlay(m_MeasXLD, "yellow");
                                }
                            }
                            finally
                            {
                                m_MeasXLD?.Dispose();
                            }
                        }
                        else
                        {
                            m_MeasXLD?.Dispose();
                        }

                        RefreshEditableLinePreview();

                        //更新结果
                        if (status)
                        {
                            UpdateOutputCoordinates(Line);

                            //Console.WriteLine($"找线测量结果：{JsonHelper.Serialize(new
                            //{
                            //    OutStartX,
                            //    OutStartY,
                            //    OutEndX,
                            //    OutEndY,
                            //    IsCalibrationEffective
                            //})}");
                        }
                        else
                        {
                            return NodeStatus.None;
                        }
                    }
                    catch (Exception ex)
                    {
                        ResetOutputs();
                        Console.WriteLine(ex.StackTrace.ToString());
                        return NodeStatus.Error;
                    }
                    #region 输出

                    Console.WriteLine($"开始输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");

                    SyncOutputParams();
                    Console.WriteLine($"完成赋值时间：{DateTime.Now.ToString($"HH:mm:ss.fff")}");


                    if (!UpdateParam())
                    {
                        Console.WriteLine($"模块_{Serial}更新参数失败");
                    }
                    Console.WriteLine($"结束输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    #endregion

                    return NodeStatus.Success;
                });

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：找直线模块执行时间：{time} 毫秒");
                Console.WriteLine($"执行了{Count++}次");
                return Output = new ExecuteModuleOutput()
                {
                    RunStatus = result,
                    RunTime = time,
                };
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
            }
        }

        public override bool OnceInit()
        {
            if (IsOnceInit)
                return true;

            bool result = base.OnceInit();
            if (!result)
                return false;

            _isDisposed = false;
            EnsureMeasureParametersInitialized();
            TriggerModuleRun ??= () => ExecuteModule().Result;
            IsOnceInit = true;
            return true;
        }


        public void InitImg()
        {
            if (GetInputImage() == null)
                return;

            if (mWindowH?.hControl == null)
            {
                EnsurePreviewLineInitialized();
                return;
            }

            mWindowH.hControl.MouseUp -= HControl_MouseUp;
            mWindowH.hControl.MouseUp += HControl_MouseUp;

            ShowHRoi();
            InitLineMethod();
        }


        private void HControl_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (mWindowH?.WindowH == null)
                {
                    return;
                }

                ROI roi = mWindowH.WindowH.smallestActiveROI(out string info, out string index);
                if (index.Length > 0)
                {
                    roiLine = roi as ROILine;
                    if (roiLine != null)
                    {
                        TempLine.StartX = Math.Round(roiLine.StartX, 3);
                        TempLine.StartY = Math.Round(roiLine.StartY, 3);
                        TempLine.EndX = Math.Round(roiLine.EndX, 3);
                        TempLine.EndY = Math.Round(roiLine.EndY, 3);
                        InitLineChanged_Flag = true;
                        _ = ExecuteModule();
                        InitLineMethod();
                        InitLineChanged_Flag = false;
                    }
                }

                //OffsetX = 0; 
                //OffsetY = 0;
                //CompensationAngle = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        /// <summary>
        /// 对点应用任意加法 2D 变换 直线
        /// </summary>
        public static void Affine2d(HTuple HomMat2D, ROILine intLine, ROILine tranLine)
        {
            HHomMat2D TempHomMat2D = new HHomMat2D(HomMat2D);
            if (TempHomMat2D.RawData.Length == 0)
            {
                MessageView.Ins.MessageBoxShow("仿射矩阵为空，请检查！", eMsgType.Error);
                return;
            }
            HTuple X0 = new HTuple();
            HTuple X1 = new HTuple();
            tranLine.StartY = TempHomMat2D.AffineTransPoint2d(intLine.StartY, intLine.StartX, out X0);
            tranLine.StartX = X0;
            tranLine.EndY = TempHomMat2D.AffineTransPoint2d(intLine.EndY, intLine.EndX, out X1);
            tranLine.EndX = X1;
        }

        public void InitLineMethod()
        {
            if (mWindowH?.WindowH == null)
            {
                EnsurePreviewLineInitialized();
                return;
            }

            if (TranLine.FlagLineStyle != null)
            {
                mWindowH.WindowH.genLine(Serial.ToString(), TranLine.StartY, TranLine.StartX, TranLine.EndY, TranLine.EndX, ref RoiList);
            }
            else if (!RoiList.ContainsKey(Serial.ToString()))
            {
                using var tempImage = CloneInputImage();
                if (tempImage == null) return;

                if (tempImage != null && tempImage.IsInitialized())
                {
                    //所有参数都为默认值时按图片尺寸绘制直线
                    if (InitLineStartX == 50.0 && InitLineStartY == 50.0 && InitLineEndX == 50.0 && InitLineEndY == 50.0)
                    {
                        //按照图片尺寸绘制直线
                        tempImage.GetImageSize(out int imageWidth, out int imageHeight);
                        mWindowH.WindowH.genLine(Serial.ToString(), imageHeight / 4, imageWidth / 4, imageHeight / 4, imageWidth / 2, ref RoiList);
                        InitLineStartX = TranLine.StartX = imageWidth / 4;
                        InitLineStartY = TranLine.StartY = imageHeight / 4;
                        InitLineEndX = TranLine.EndX = imageWidth / 2;
                        InitLineEndY = TranLine.EndY = imageHeight / 4;
                    }
                    else
                    {
                        mWindowH.WindowH.genLine(Serial.ToString(), InitLineStartY, InitLineStartX, InitLineEndY, InitLineEndX, ref RoiList);
                    }
                }
                else
                {
                    mWindowH.WindowH.genLine(Serial.ToString(), InitLineStartY, InitLineStartX, InitLineEndY, InitLineEndX, ref RoiList);
                    InitLine.StartX = TranLine.StartX = InitLineStartX;
                    InitLine.StartY = TranLine.StartY = InitLineStartY;
                    InitLine.EndX = TranLine.EndX = InitLineEndX;
                    InitLine.EndY = TranLine.EndY = InitLineEndY;
                }
                    
            }
            else if (RoiList.ContainsKey(Serial.ToString()))
            {
                mWindowH.WindowH.genLine(Serial.ToString(), InitLine.StartY, InitLine.StartX, InitLine.EndY, InitLine.EndX, ref RoiList);
                if (InitLineChanged_Flag)
                {
                    InitLineStartX = InitLine.StartX;
                    InitLineStartY = InitLine.StartY;
                    InitLineEndX = InitLine.EndX;
                    InitLineEndY = InitLine.EndY;
                    OffsetX = 0;
                    OffsetY = 0;
                    CompensationAngle = 0;
                }
            }
        }

        private void DisposeCachedInputImage()
        {
            if (_inputImage?.Value is HImage inputImage)
            {
                inputImage.Dispose();
            }

            if (_inputImage != null)
            {
                _inputImage.Value = null;
            }
        }

        private HImage CreateOwnedInputImage(object imageValue)
        {
            switch (imageValue)
            {
                case HImage inputImage:
                    return inputImage.Clone();
                case HObject inputObject:
                    return new HImage(inputObject).Clone();
                default:
                    return null;
            }
        }

        private HImage GetInputImage()
        {
            if (_inputImage?.Value == null)
                return null;

            switch (_inputImage.Value)
            {
                case HImage inputImage:
                    return inputImage.IsInitialized() ? inputImage : null;
                case HObject inputObject:
                    var normalizedImage = new HImage(inputObject).Clone();
                    _inputImage.Value = normalizedImage;
                    return normalizedImage.IsInitialized() ? normalizedImage : null;
                default:
                    return null;
            }
        }

        private HImage CloneInputImage()
        {
            var inputImage = GetInputImage();
            return inputImage?.Clone();
        }

        private void EnsureMeasureParametersInitialized(bool forceRebuild = false)
        {
            if (forceRebuild || ParamName == null)
            {
                ParamName = new HTuple();
                ParamName.Append("measure_transition");
                ParamName.Append("measure_select");
                ParamName.Append("measure_distance");
            }

            if (forceRebuild || ParamValue == null)
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

        private void RefreshInputImagePreview()
        {
            if (!_previewRefreshPending)
            {
                return;
            }

            using HImage previewImage = CloneInputImage();
            if (previewImage == null || !previewImage.IsInitialized())
            {
                ClearPreviewDisplay();
                _previewRefreshPending = false;
                return;
            }

            DisplayPreviewImage(previewImage);
            _previewRefreshPending = false;
            EnsurePreviewLineInitialized();
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
                    RefreshEditableLinePreview();
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
                            RefreshEditableLinePreview();
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

        public void ClearPreviewDrawObjects()
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

        public void RefreshEditableLinePreview(double imageUnitsPerScreenPixel = 1.0)
        {
            if (_isRefreshingEditableLinePreview)
                return;

            _isRefreshingEditableLinePreview = true;
            try
            {
                RemovePreviewObjectsByColor(EditableLineColor);
                EnsurePreviewLineInitialized();

                HObject lineContour = null;
                HObject arrowContour = null;
                HObject handleContours = null;
                try
                {
                    imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                    GenContour(out lineContour, InitLineStartY, InitLineStartX, InitLineEndY, InitLineEndX);
                    AddXldPreviewOverlay(lineContour, EditableLineColor);

                    if (CreatePreviewDirectionArrow(out arrowContour))
                    {
                        AddXldPreviewOverlay(arrowContour, EditableLineColor);
                    }

                    if (CreatePreviewHandles(imageUnitsPerScreenPixel, out handleContours))
                    {
                        AddXldPreviewOverlay(handleContours, EditableLineColor);
                    }
                }
                catch
                {
                }
                finally
                {
                    DisposeHObject(lineContour);
                    DisposeHObject(arrowContour);
                    DisposeHObject(handleContours);
                }
            }
            finally
            {
                _isRefreshingEditableLinePreview = false;
            }
        }

        private bool CreatePreviewDirectionArrow(out HObject arrowContour)
        {
            arrowContour = null;
            HObject shaft = null;
            HObject headLeft = null;
            HObject headRight = null;
            HObject arrowWithLeftHead = null;
            try
            {
                double dx = InitLineEndX - InitLineStartX;
                double dy = InitLineEndY - InitLineStartY;
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length <= double.Epsilon)
                {
                    return false;
                }

                double ux = dx / length;
                double uy = dy / length;
                double headSize = Math.Max(8.0, Math.Min(18.0, length * 0.12));
                double arrowLength = Math.Min(Math.Max(length * 0.35, headSize * 2.0), 80.0);
                double tipX = InitLineStartX + ux * Math.Min(length - headSize, length * 0.72);
                double tipY = InitLineStartY + uy * Math.Min(length - headSize, length * 0.72);
                double startX = tipX - ux * arrowLength;
                double startY = tipY - uy * arrowLength;

                HOperatorSet.GenContourPolygonXld(out shaft, new HTuple(startY, tipY), new HTuple(startX, tipX));

                double wingX = -uy;
                double wingY = ux;
                double baseX = tipX - ux * headSize;
                double baseY = tipY - uy * headSize;
                double halfWidth = headSize * 0.55;
                HOperatorSet.GenContourPolygonXld(out headLeft,
                    new HTuple(tipY, baseY + wingY * halfWidth),
                    new HTuple(tipX, baseX + wingX * halfWidth));
                HOperatorSet.GenContourPolygonXld(out headRight,
                    new HTuple(tipY, baseY - wingY * halfWidth),
                    new HTuple(tipX, baseX - wingX * halfWidth));

                arrowWithLeftHead = shaft.ConcatObj(headLeft);
                arrowContour = arrowWithLeftHead.ConcatObj(headRight);
                return arrowContour != null && arrowContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(arrowContour);
                arrowContour = null;
                return false;
            }
            finally
            {
                DisposeHObject(shaft);
                DisposeHObject(headLeft);
                DisposeHObject(headRight);
                DisposeHObject(arrowWithLeftHead);
            }
        }

        private bool CreatePreviewHandles(double imageUnitsPerScreenPixel, out HObject handleContours)
        {
            handleContours = null;
            HObject startHandle = null;
            HObject endHandle = null;
            try
            {
                double radius = Math.Max(1.0, MeasureLinePreviewStyle.HandleScreenSize * imageUnitsPerScreenPixel / 2.0);
                HOperatorSet.GenCircleContourXld(out startHandle, InitLineStartY, InitLineStartX, radius, 0, Math.PI * 2.0, "positive", Math.Max(1.0, imageUnitsPerScreenPixel));
                HOperatorSet.GenCircleContourXld(out endHandle, InitLineEndY, InitLineEndX, radius, 0, Math.PI * 2.0, "positive", Math.Max(1.0, imageUnitsPerScreenPixel));
                handleContours = startHandle.ConcatObj(endHandle);
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
                DisposeHObject(startHandle);
                DisposeHObject(endHandle);
            }
        }

        public void EnsurePreviewLineInitialized()
        {
            if (InitLineStartX == 50.0
                && InitLineStartY == 50.0
                && InitLineEndX == 50.0
                && InitLineEndY == 50.0
                && PreviewImageWidth > 1.0
                && PreviewImageHeight > 1.0)
            {
                InitLineStartX = PreviewImageWidth / 4.0;
                InitLineStartY = PreviewImageHeight / 2.0;
                InitLineEndX = PreviewImageWidth * 3.0 / 4.0;
                InitLineEndY = PreviewImageHeight / 2.0;
            }

            InitLine.StartX = TempLine.StartX = TranLine.StartX = InitLineStartX;
            InitLine.StartY = TempLine.StartY = TranLine.StartY = InitLineStartY;
            InitLine.EndX = TempLine.EndX = TranLine.EndX = InitLineEndX;
            InitLine.EndY = TempLine.EndY = TranLine.EndY = InitLineEndY;
        }

        public MeasureLinePreviewLine GetPreviewLine()
        {
            EnsurePreviewLineInitialized();
            return new MeasureLinePreviewLine(InitLineStartX, InitLineStartY, InitLineEndX, InitLineEndY);
        }

        public void ApplyPreviewLine(MeasureLinePreviewLine line, bool runMeasurement, double imageUnitsPerScreenPixel = 1.0)
        {
            InitLineChanged_Flag = true;
            try
            {
                InitLineStartX = Math.Round(line.StartX, 3);
                InitLineStartY = Math.Round(line.StartY, 3);
                InitLineEndX = Math.Round(line.EndX, 3);
                InitLineEndY = Math.Round(line.EndY, 3);

                InitLine.StartX = TempLine.StartX = TranLine.StartX = InitLineStartX;
                InitLine.StartY = TempLine.StartY = TranLine.StartY = InitLineStartY;
                InitLine.EndX = TempLine.EndX = TranLine.EndX = InitLineEndX;
                InitLine.EndY = TempLine.EndY = TranLine.EndY = InitLineEndY;
            }
            finally
            {
                InitLineChanged_Flag = false;
            }

            RefreshEditableLinePreview(imageUnitsPerScreenPixel);
            if (runMeasurement)
            {
                _ = ExecuteModule();
            }
        }

        public void EnsureWindowControlInitialized()
        {
            string moduleKey = RuntimeModuleKey;
            if (string.IsNullOrWhiteSpace(moduleKey))
                return;

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.ImgControlPair == null)
            {
                if (mWindowH == null)
                {
                    mWindowH = new VMHWindowControl();
                }
                return;
            }

            if (solutionItem.ImgControlPair.TryGetValue(moduleKey, out object windowControl) &&
                windowControl is VMHWindowControl existWindow)
            {
                mWindowH = existWindow;
                return;
            }

            if (mWindowH == null)
            {
                mWindowH = new VMHWindowControl();
            }

            if (!string.IsNullOrWhiteSpace(_windowControlKey) &&
                _windowControlKey != moduleKey &&
                solutionItem.ImgControlPair.TryGetValue(_windowControlKey, out object oldWindowControl) &&
                ReferenceEquals(oldWindowControl, mWindowH))
            {
                solutionItem.ImgControlPair.Remove(_windowControlKey);
            }

            solutionItem.ImgControlPair[moduleKey] = mWindowH;
            _windowControlKey = moduleKey;
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

        private void UpdateOutputCoordinates(ROILine line)
        {
            double startX = line.StartX;
            double startY = line.StartY;
            double endX = line.EndX;
            double endY = line.EndY;

            if (EnableCalibration &&
                EnsureCalibrationRuntimeReady() &&
                TryConvertPointToBoard(line.StartX, line.StartY, out double boardStartX, out double boardStartY) &&
                TryConvertPointToBoard(line.EndX, line.EndY, out double boardEndX, out double boardEndY))
            {
                startX = boardStartX;
                startY = boardStartY;
                endX = boardEndX;
                endY = boardEndY;
                SetCalibrationState(true, _calibrationCameraId);
            }
            else if (!EnableCalibration)
            {
                SetCalibrationState(false);
            }

            double roundedStartX = Math.Round(startX, 4);
            double roundedStartY = Math.Round(startY, 4);
            double roundedEndX = Math.Round(endX, 4);
            double roundedEndY = Math.Round(endY, 4);

            OutStartX = roundedStartX;
            OutStartY = roundedStartY;
            OutEndX = roundedEndX;
            OutEndY = roundedEndY;
            // 中点取最终坐标（含标定）的均值，保证与起点/终点同一坐标系
            OutMidX = Math.Round((startX + endX) / 2.0, 4);
            OutMidY = Math.Round((startY + endY) / 2.0, 4);

            OutLine = new Line(roundedStartY, roundedStartX, roundedEndY, roundedEndX);
        }

        /// <summary>
        /// 重置所有测量结果输出为 null，表示没测到。
        /// </summary>
        private void ResetOutputs()
        {
            OutLine = null;
            OutStartX = null;
            OutStartY = null;
            OutEndX = null;
            OutEndY = null;
            OutMidX = null;
            OutMidY = null;
        }

        /// <summary>
        /// 仅同步本模块 OutputParams 的值（取自当前模型属性），不调用 UpdateParam，
        /// 因此不触发全局参数的增删策略。
        /// </summary>
        private void SyncOutputParams()
        {
            var outputValues = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (!string.IsNullOrWhiteSpace(item.ParamName) &&
                    outputValues.TryGetValue(item.ParamName, out object value))
                {
                    item.Value = value;
                }
            }
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

        private void InitLineChanged()
        {
            if (InitLineChanged_Flag == true)
                return;

            InitLine.StartX = InitLineStartX;
            InitLine.StartY = InitLineStartY;
            InitLine.EndX = InitLineEndX;
            InitLine.EndY = InitLineEndY;

            HTuple Hommat2d = new HTuple();
            Hommat2d.Dispose();
            HOperatorSet.HomMat2dIdentity(out Hommat2d);
            double CenterX = (InitLine.EndX + InitLine.StartX) * 0.5;
            double CenterY = (InitLine.EndY + InitLine.StartY) * 0.5;
            HOperatorSet.HomMat2dRotate(Hommat2d, new HTuple(CompensationAngle).TupleRad(), CenterY, CenterX, out Hommat2d);
            HOperatorSet.HomMat2dTranslate(Hommat2d, OffsetY, OffsetX, out Hommat2d);

            Affine2d(Hommat2d, InitLine, InitLine);
            TempLine.StartX = TranLine.StartX = InitLine.StartX;
            TempLine.StartY = TranLine.StartY = InitLine.StartY;
            TempLine.EndX = TranLine.EndX = InitLine.EndX;
            TempLine.EndY = TranLine.EndY = InitLine.EndY;

            if (roiLine != null && mWindowH?.WindowH != null)
            {
                roiLine.StartX = InitLine.StartX;
                roiLine.StartY = InitLine.StartY;
                roiLine.EndX = InitLine.EndX;
                roiLine.EndY = InitLine.EndY;

                _ = ExecuteModule();
            }
        }


        /// <summary>
        /// 检测直线 增加屏蔽区域 magical20171028
        /// </summary>
        /// <param name="image">检测图像</param>
        /// <param name="line">输入检测直线区域</param>
        /// <param name="meas">形态参数</param>
        /// <param name="outLine">输出直线</param>
        /// <param name="outR">输出行点</param>
        /// <param name="outC">输出列点</param>
        /// <param name="outXld">输出检测轮廓</param>
        /// <param name="disableRegion">屏蔽区域 可选</param>
        /// <param name="isPaint">对屏蔽区域进行喷绘 可选</param>
        public bool MeasLine(
            HImage image,
            ROILine line,
            ROILine outLine,
            out HTuple outR,
            out HTuple outC,
            out HXLDCont outXld,
            HRegion disableRegion = null
        )
        {
            HMetrologyModel MetroModel = new HMetrologyModel();
            try
            {
                HTuple lineResult = new HTuple();
                HTuple lineInfo = new HTuple(new double[] { line.StartY, line.StartX, line.EndY, line.EndX });

                bool status = true;

                //最强边的计算
                if (ParamValue[1] == "strongest")
                {
                    ROILine tmpOutLine;
                    status = MeasLine1D(image, line, out tmpOutLine, out outR, out outC, out outXld, disableRegion);

                    outLine.StartY = tmpOutLine.StartY;
                    outLine.StartX = tmpOutLine.StartX;
                    outLine.EndY = tmpOutLine.EndY;
                    outLine.EndX = tmpOutLine.EndX;
                    outLine.MidX = tmpOutLine.MidX;
                    outLine.MidY = tmpOutLine.MidY;
                    outLine.Phi = tmpOutLine.Phi;
                    outLine.Dist = tmpOutLine.Dist;
                    outLine.Y = outR;
                    outLine.X = outC;
                    outLine.Nx = tmpOutLine.Nx;
                    outLine.Ny = tmpOutLine.Ny;

                    return status;
                }

                //降低直线拟合的最低得分
                MetroModel.AddMetrologyObjectGeneric(
                    new HTuple("line"),
                    lineInfo,
                    new HTuple(Length1 / 2.0f),
                    new HTuple(Length2 / 2.0f),
                    new HTuple(1), //滤波
                    new HTuple(Threshold),
                    ParamName,
                    ParamValue
                );
                MetroModel.SetMetrologyObjectParam(0, "min_score", 0.1);
                MetroModel.SetMetrologyObjectParam(0, "measure_distance", MeasDis);
                /// 分数阈值
                if (disableRegion != null)
                {
                    MetroModel.ApplyMetrologyModel(image);
                    //单个测量区域 刚好 有一大半在屏蔽区域,一小部分在有效区域,这时候也会测出一个点这个点在屏蔽区域内,导致精度损失约为1个像素左右.需要喷绘之后,再进行点是否在屏蔽区域判断
                    outXld = MetroModel.GetMetrologyObjectMeasures(
                        "all",
                        "all",
                        out outR,
                        out outC
                    );
                    List<double> tempOutR = new List<double>(), tempOutC = new List<double>();
                    for (int i = 0; i < outR.DArr.Length; i++)
                    {
                        //0 表示没有包含
                        if (disableRegion.TestRegionPoint(outR[i].D, outC[i].D) == 0)
                        {
                            tempOutR.Add(outR[i].D);
                            tempOutC.Add(outC[i].D);
                        }
                    }
                    outR = new HTuple(tempOutR.ToArray());
                    outC = new HTuple(tempOutC.ToArray());
                }
                else
                {
                    MetroModel.ApplyMetrologyModel(image);
                    outXld = MetroModel.GetMetrologyObjectMeasures(
                        "all",
                        "all",
                        out outR,
                        out outC
                    );
                }
                lineResult = MetroModel.GetMetrologyObjectResult(
                    new HTuple("all"),
                    new HTuple("all"),
                    new HTuple("result_type"),
                    new HTuple("all_param")
                );

                if (lineResult.TupleLength() >= 4)
                {
                    outLine.StartY = Math.Round(lineResult[0].D, 4);
                    outLine.StartX = Math.Round(lineResult[1].D, 4);
                    outLine.EndY = Math.Round(lineResult[2].D, 4);
                    outLine.EndX = Math.Round(lineResult[3].D, 4);
                    outLine.MidX = (outLine.StartX + outLine.EndX) / 2;
                    outLine.MidY = (outLine.StartY + outLine.EndY) / 2;
                    outLine.Phi = Math.Round(
                        HMisc.AngleLx(outLine.StartY, outLine.StartX, outLine.EndY, outLine.EndX),
                        4
                    );
                    outLine.Dist = Math.Round(
                        HMisc.DistancePp(
                            outLine.StartX,
                            outLine.StartY,
                            outLine.EndX,
                            outLine.EndY
                        ),
                        4
                    );
                    outLine.Y = outR;
                    outLine.X = outC;
                    outLine.Nx = outLine.EndX - outLine.StartX;
                    outLine.Ny = outLine.StartY - outLine.EndY;

                    status = true;
                }
                else
                {
                    ROILine tmpOutLine;
                    status = FitLine(outR.ToDArr().ToList(), outC.ToDArr().ToList(), out tmpOutLine);
                    if (status)
                    {
                        outLine.StartY = tmpOutLine.StartY;
                        outLine.StartX = tmpOutLine.StartX;
                        outLine.EndY = tmpOutLine.EndY;
                        outLine.EndX = tmpOutLine.EndX;
                        outLine.MidX = tmpOutLine.MidX;
                        outLine.MidY = tmpOutLine.MidY;
                        outLine.Phi = tmpOutLine.Phi;
                        outLine.Dist = tmpOutLine.Dist;
                        outLine.Y = outR;
                        outLine.X = outC;
                        outLine.Nx = tmpOutLine.Nx;
                        outLine.Ny = tmpOutLine.Ny;
                    }    
                }
                MetroModel.Dispose();

                return status;
            }
            catch (Exception ex)
            {
                outLine = line;
                outR = new HTuple();
                outC = new HTuple();
                outXld = new HXLDCont();
                MetroModel.Dispose();
                Debug.Write(ex.Message);

                return false;
            }
        }


        /// <summary>
        /// 一维测量算子,检测直线.再利用halcon的拟合直线算法拟合直线 主要用于最强边缘的测量
        /// </summary>
        /// <param name="image"></param>
        /// <param name="line"></param>
        /// <param name="outLine"></param>
        /// <param name="outR"></param>
        /// <param name="outC"></param>
        /// <param name="outXld"></param>
        /// <param name="disableRegion"></param>
        /// <param name="isPaint"></param>
        public bool MeasLine1D(
            HImage image,
            ROILine line,
            out ROILine outLine,
            out HTuple outR,
            out HTuple outC,
            out HXLDCont outXld,
            HRegion disableRegion = null,
            bool isPaint = true
        )
        {
            double halfLength1 = Length1 / 2;
            double halfLength2 = Length2 / 2;

            outLine = line;
            outR = new HTuple();
            outC = new HTuple();
            outXld = new HXLDCont();
            List<double> outRList = new List<double>();
            List<double> outCList = new List<double>();
            HImage tempImage = disableRegion != null ? disableRegion.PaintRegion(image, 0d, "fill") : image; //将屏蔽区域喷绘为0
            double angle = HMisc.AngleLx(line.StartY, line.StartX, line.EndY, line.EndX); //注意下这里的角度
            double effectiveLength = HMisc.DistancePp(line.StartY, line.StartX, line.EndY, line.EndX) - 2 * halfLength2;
            if (effectiveLength <= 0 || MeasDis <= 0)
            {
                if (!ReferenceEquals(tempImage, image))
                {
                    tempImage?.Dispose();
                }
                return false;
            }

            double points = Math.Ceiling(effectiveLength / MeasDis);
            if (points <= 0)
            {
                if (!ReferenceEquals(tempImage, image))
                {
                    tempImage?.Dispose();
                }
                return false;
            }

            double measDis = effectiveLength / points;

            HObject hoOutXld = new HObject();
            HOperatorSet.GenEmptyObj(out hoOutXld);
            HObject hoTmpOutXld = new HObject();
            for (int i = 0; i <= points; i++)
            {
                double rectRowC = line.StartY - (halfLength2 + i * measDis) * Math.Sin(angle);
                double rectColC = line.StartX + (halfLength2 + i * measDis) * Math.Cos(angle);

                HOperatorSet.GenRectangle2ContourXld(out hoTmpOutXld, rectRowC, rectColC, angle - Math.PI / 2, halfLength1, halfLength2);
                HOperatorSet.ConcatObj(hoOutXld, hoTmpOutXld, out hoOutXld);

                image.GetImageSize(out int width, out int height);
                using HMeasure mea = new HMeasure();
                mea.GenMeasureRectangle2(
                    rectRowC,
                    rectColC,
                    angle - Math.PI / 2,
                    halfLength1,
                    halfLength2,
                    width,
                    height,
                    "nearest_neighbor"
                );
                mea.MeasurePos(
                    tempImage,
                    1,
                    Threshold,
                    ParamValue[0],
                    "all",
                    out HTuple rowEdge,
                    out HTuple columnEdge,
                    out HTuple amplitude,
                    out HTuple distance
                );
                if (amplitude != null & amplitude.Length > 0)
                {
                    // amplitude.TupleSort();
                    HTuple HIndex = amplitude.TupleAbs().TupleSortIndex();
                    outRList.Add(rowEdge[HIndex[HIndex.Length - 1].I]);
                    outCList.Add(columnEdge[HIndex[HIndex.Length - 1].I]);
                }
            }
            outXld = new HXLDCont(hoOutXld);

            bool status = true;
            outR = new HTuple(outRList.ToArray());
            outC = new HTuple(outCList.ToArray());
            if (disableRegion != null)
            {
                List<double> tempOutR = new List<double>(),
                    tempOutC = new List<double>();
                for (int i = 0; i < outR.DArr.Length; i++)
                {
                    if (disableRegion.TestRegionPoint(outR[i].D, outC[i].D) == 0) //0 表示没有包含
                    {
                        tempOutR.Add(outR[i].D);
                        tempOutC.Add(outC[i].D);
                    }
                }
                outR = new HTuple(tempOutR.ToArray());
                outC = new HTuple(tempOutC.ToArray());
            }
            if (outR.Length > 0)
            {
                status = FitLine(outR.ToDArr().ToList(), outC.ToDArr().ToList(), out outLine);
            }
            else
            {
                outLine = line;
                status = false;
            }

            hoOutXld.Dispose();
            hoTmpOutXld.Dispose();
            if (!ReferenceEquals(tempImage, image))
            {
                tempImage?.Dispose();
            }

            return status;
        }


        public struct Point
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class PointSetWithScore
        {
            public List<Point> Points { get; set; }
            public double Score { get; set; }
        }

        public static List<PointSetWithScore> SegmentPoints(List<double> xList, List<double> yList)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; i < xList.Count; i++)
            {
                points.Add(new Point { X = xList[i], Y = yList[i] });
            }

            double minDist = CalculateMinDistance(points);
            double threshold = minDist;
            int minInliers = 1;
            int iterations = 50;

            List<Point> remainingPoints = new List<Point>(points);
            List<List<Point>> pointSets = new List<List<Point>>();
            // RANSAC
            while (remainingPoints.Count >= minInliers)
            {
                List<Point> inliers = RANSAC(remainingPoints, threshold, iterations, minInliers);
                if (inliers.Count < minInliers)
                    break;

                pointSets.Add(inliers);

                remainingPoints = remainingPoints.Except(inliers).ToList();
            }

            // 计算每个点集的得分
            List<PointSetWithScore> result = new List<PointSetWithScore>();
            foreach (var pointSet in pointSets)
            {
                double score = CalculateScore(pointSet);
                result.Add(new PointSetWithScore { Points = pointSet, Score = score });
            }

            result.Sort((a, b) => b.Score.CompareTo(a.Score));
            return result;
        }

        // 计算点集中每个点的最近邻距离的平均值
        private static double CalculateMinDistance(List<Point> points)
        {
            if (points.Count < 2)
                return 0;

            double minDist = double.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    if (i == j)
                        continue;

                    double dist = Distance(points[i], points[j]);

                    if (dist < minDist)
                        minDist = dist;
                }
            }
            return minDist;
        }

        // 计算两点之间的欧氏距离
        private static double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        // RANSAC算法提取内点
        private static List<Point> RANSAC(List<Point> points, double threshold, int iterations, int minInliers)
        {
            Random random = new Random();
            List<Point> bestInliers = new List<Point>();
            for (int i = 0; i < iterations; i++)
            {
                // 随机选择两个点
                int index1 = random.Next(points.Count);
                int index2 = random.Next(points.Count);
                if (index1 == index2)
                    continue;
                Point p1 = points[index1];
                Point p2 = points[index2];

                // 如果两点相同，跳过
                if (p1.X == p2.X && p1.Y == p2.Y)
                    continue;

                List<Point> inliers = new List<Point>();
                foreach (Point p in points)
                {
                    double dist = DistancePointToLine(p, p1, p2);
                    if (dist < threshold)
                        inliers.Add(p);
                }

                if (inliers.Count > bestInliers.Count)
                    bestInliers = inliers;
            }
            return bestInliers;
        }

        // 计算点到直线的距离
        private static double DistancePointToLine(Point p, Point a, Point b)
        {
            double numerator = Math.Abs((b.X - a.X) * (a.Y - p.Y) - (a.X - p.X) * (b.Y - a.Y));
            double denominator = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            if (denominator == 0)
                return Distance(p, a);
            return numerator / denominator;
        }

        // 计算点集的得分
        private static double CalculateScore(List<Point> points)
        {
            int pointCount = points.Count;
            if (pointCount < 2)
                return pointCount;

            // PCA：求主方向向量
            double xMean = points.Average(p => p.X);
            double yMean = points.Average(p => p.Y);

            double ssxx = points.Sum(p => (p.X - xMean) * (p.X - xMean));
            double ssyy = points.Sum(p => (p.Y - yMean) * (p.Y - yMean));
            double ssxy = points.Sum(p => (p.X - xMean) * (p.Y - yMean));

            // 特征向量（主方向）
            double theta = 0.5 * Math.Atan2(2 * ssxy, ssxx - ssyy);
            double dx = Math.Cos(theta);
            double dy = Math.Sin(theta);

            // 计算点到直线的投影残差
            var distances = points.Select(p =>
            {
                double px = p.X - xMean;
                double py = p.Y - yMean;
                // 点到方向向量的垂直距离
                return Math.Abs(-dy * px + dx * py);
            }).ToList();

            // 用中位数距离来减少异常点影响
            //distances.Sort();
            //double medianDist = distances[distances.Count / 2];
            double meanDist = distances.Sum() / distances.Count;

            // 得分 = 点数 * e^(-λ*残差)
            double lambda = 1.0;
            double score = pointCount * Math.Exp(-lambda * meanDist);
            return score;
        }


        /// <summary>
        /// /使用halcon的拟合直线算法,比fitLine更准确,因为有其自己的剔除异常点算法
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="line"></param>
        /// <returns>结果直线</returns>
        public static bool FitLine(List<double> rows, List<double> cols, out ROILine line)
        {
            line = new ROILine();
            try
            {
                // 剔除离群点
                List<PointSetWithScore> pointSets = SegmentPoints(cols, rows);

                if(pointSets.Count > 0)
                {
                    PointSetWithScore pointSet = pointSets[0];

                    rows.Clear(); 
                    cols.Clear();
                    for (int i = 0; i < pointSet.Points.Count; i++)
                    {
                        rows.Add(pointSet.Points[i].Y);
                        cols.Add(pointSet.Points[i].X);
                    }

                    SortPairs(ref rows, ref cols);

                    HXLDCont lineXLD = new HXLDCont(new HTuple(rows.ToArray()), new HTuple(cols.ToArray()));

                    lineXLD.FitLineContourXld(
                        "tukey",
                        -1,
                        0,
                        5,
                        2,
                        out double rowBegin,
                        out double colBegin,
                        out double rowEnd,
                        out double colEnd,
                        out double nr,
                        out double nc,
                        out double dist
                    ); //tukey剔除算法为halcon推荐算法

                    line = new ROILine(Math.Round(rowBegin, 4), Math.Round(colBegin, 4),
                                       Math.Round(rowEnd, 4), Math.Round(colEnd, 4));

                    return true;
                }
                else
                {
                    return false;
                }
                
            }
            catch (Exception)
            {
                line.Status = false;
                return false;
            }
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
            HTuple hv_Indices1 = new HTuple(), hv_Indices2 = new HTuple();
            
            if ((hv_T1.TupleMax().D - hv_T1.TupleMin().D) > (hv_T2.TupleMax().D - hv_T2.TupleMin().D))
                hv_SortMode = new HTuple("1");
            else
                hv_SortMode = new HTuple("2");
            if ((int)((new HTuple(hv_SortMode.TupleEqual("1"))).TupleOr(new HTuple(hv_SortMode.TupleEqual(1)))) != 0)
            {
                HOperatorSet.TupleSortIndex(hv_T1, out hv_Indices1);
                hv_Sorted1 = hv_T1.TupleSelect(hv_Indices1);
                hv_Sorted2 = hv_T2.TupleSelect(hv_Indices1);
            }
            else if ((int)((new HTuple((new HTuple(hv_SortMode.TupleEqual("column"))).TupleOr(new HTuple(hv_SortMode.TupleEqual("2"))))
                           ).TupleOr(new HTuple(hv_SortMode.TupleEqual(2)))) != 0)
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
            double startRow,
            double startCol,
            double endRow,
            double endCol
        )
        {
            HOperatorSet.GenContourPolygonXld(
                out ResultXLD,
                new HTuple(startRow, endRow),
                new HTuple(startCol, endCol)
            );
        }


        public void ShowHRoi()
        {
            if (mWindowH?.WindowH == null || mWindowH?.hControl == null)
            {
                return;
            }

            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == RuntimeModuleKey).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(
                        mWindowH.hControl.HalconWindow,
                        roiText.size,
                        "false",
                        "false"
                    );
                    ShowTool.SetMsg(
                        mWindowH.hControl.HalconWindow,
                        roiText.text,
                        "image",
                        roiText.row,
                        roiText.col,
                        roiText.drawColor,
                        "false"
                    );
                }
                else
                {
                    mWindowH.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
                }
            }
        }


        public void ShowHRoi(HRoi ROI)
        {
            try
            {
                int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                if (ROI.fors == true)
                {
                    mHRoi.Add(ROI);
                    return;
                }
                if (index > -1)
                    mHRoi[index] = ROI;
                else
                    mHRoi.Add(ROI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

    }





}

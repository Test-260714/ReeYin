using Dm;
using DryIoc;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
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
using System.Windows.Forms;
using System.Windows.Media;
using static ALGO.CreatRegion.ViewModels.CreatRegionViewModel;
using ALGO.CreatRegion.ViewModels;

namespace ALGO.CreatRegion
{
    [Serializable]
    public class CreatRegionModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; } = null!;

        [JsonIgnore]
        private bool _ownsInputImage;

        [JsonIgnore]
        private bool _previewRefreshPending = true;

        #endregion

        #region Properties

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get { RefreshInputImageDisplay(); return _inputImage; }
            set { SetInputImage(value); }
        }

        private string _inputImageName = string.Empty;
        /// <summary>
        /// 输入图像名称
        /// </summary>
        public string InputImageName
        {
            get { return _inputImageName; }
            set { SetProperty(ref _inputImageName, value); }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

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
        private bool _initRegionChanged_Flag = false;
        /// <summary>
        /// InitRegionChanged_Flag
        /// </summary>
        public bool InitRegionChanged_Flag
        {
            get { return _initRegionChanged_Flag; }
            set
            {
                _initRegionChanged_Flag = value;
                RaisePropertyChanged();
            }
        }

        /// <summary> 区域列表 </summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        [JsonIgnore]
        private HRegion _outRegion = new HRegion();
        [JsonIgnore]
        [OutputParam("OutRegion", "输出区域")]
        /// <summary>
        /// 输出区域信息
        /// </summary>
        public HRegion OutRegion
        {
            get { return _outRegion; }
            set { SetProperty(ref _outRegion, value); }
        }

        [JsonIgnore]
        private ROIRectangle2 _initMeasRegion = new ROIRectangle2()
        { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };
        /// <summary>
        /// 变换前-区域信息
        /// </summary>
        public ROIRectangle2 InitMeasRegion
        {
            get { return _initMeasRegion; }
            set { _initMeasRegion = value; RaisePropertyChanged(); InitRegionChanged(); }
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
        /// 变换前-区域信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 InitRegion { get; set; } = new ROIRectangle2() { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };

        /// <summary>
        /// 区域信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 TempRegion { get; set; } = new ROIRectangle2() { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };

        /// <summary>
        /// 变换后-区域信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 TranRegion { get; set; } = new ROIRectangle2() { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };

        /// <summary>
        /// 区域ROI信息
        /// </summary>
        [JsonIgnore]
        public ROIRectangle2 roiRegion { get; set; } = new ROIRectangle2() { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        private BinarizationMode _binarizationMode = BinarizationMode.固定;
        [RecipeParam("二值方式", "创建区域时使用的阈值分割方式")]
        public BinarizationMode BinarizationMode
        {
            get { return _binarizationMode; }
            set { SetProperty(ref _binarizationMode, value, new Action(() => { REnum.EnumToStr(_binarizationMode); })); }
        }

        private LocalType _localType = LocalType.dark;
        [RecipeParam("局部阈值类型", "局部阈值模式下的明暗类型")]
        public LocalType LocalType
        {
            get { return _localType; }
            set { SetProperty(ref _localType, value); }
        }

        private double _fixedMinValue = 0;
        [RecipeParam("固定最小值", "固定阈值模式下的最小灰度")]
        public double FixedMinValue
        {
            get { return _fixedMinValue; }
            set { SetProperty(ref _fixedMinValue, value); }
        }

        private double _fixedMaxValue = 255;
        [RecipeParam("固定最大值", "固定阈值模式下的最大灰度")]
        public double FixedMaxValue
        {
            get { return _fixedMaxValue; }
            set { SetProperty(ref _fixedMaxValue, value); }
        }

        private int _localMaskH = 5;
        [RecipeParam("局部掩膜高", "局部阈值模式下的掩膜高度")]
        public int LocalMaskH
        {
            get { return _localMaskH; }
            set { SetProperty(ref _localMaskH, value); }
        }

        private int _localMaskW = 5;
        [RecipeParam("局部掩膜宽", "局部阈值模式下的掩膜宽度")]
        public int LocalMaskW
        {
            get { return _localMaskW; }
            set { SetProperty(ref _localMaskW, value); }
        }

        private int _localAbsTh = 5;
        [RecipeParam("局部绝对阈值", "局部阈值模式下的绝对阈值")]
        public int LocalAbsTh
        {
            get { return _localAbsTh; }
            set { SetProperty(ref _localAbsTh, value); }
        }

        private double _localStdFactor = 0.25;
        [RecipeParam("局部标准差因子", "局部阈值模式下的标准差乘数因子")]
        public double LocalStdFactor
        {
            get { return _localStdFactor; }
            set { SetProperty(ref _localStdFactor, value); }
        }

        #endregion

        #region Constructor
        public CreatRegionModel()
        {
        }
        #endregion

        #region Methods
        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                    return true;

                if (!base.OnceInit())
                    return false;

                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建区域模块一次初始化异常：{ex.StackTrace}");
                return false;
            }
        }

        public override void Dispose()
        {
            if (mWindowH?.hControl != null)
            {
                mWindowH.hControl.MouseUp -= HControl_MouseUp;
            }

            DisposeOwnedInputImage();
            base.Dispose();
        }

        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            return LoadKeyParam(true);
        }

        private bool LoadKeyParam(bool refreshPreview)
        {
            try
            {
                if (!base.LoadKeyParam())
                    return false;

                EnsureWindowControlInitialized();
                DisposeOwnedInputImage();

                object imageValue = GetTransmitParam(InputParams, _inputImage);
                _inputImage.Value = CreateOwnedInputImage(imageValue);
                _ownsInputImage = _inputImage.Value is HImage;
                InputImageName = _inputImage?.Name ?? string.Empty;

                if (refreshPreview)
                {
                    _previewRefreshPending = true;
                    RefreshInputImageDisplay();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载参数异常{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam(false))
                        return NodeStatus.Error;

                    HImage tempImage = CloneInputImage();
                    if (tempImage == null || !tempImage.IsInitialized())
                        return NodeStatus.None;

                    mHRoi.Clear();

                    //执行的方法
                    if (DisenableAffine2d && HomMat2D_Inverse != null && HomMat2D_Inverse.Length > 0)
                    {
                        DisenableAffine2d = false;
                        Affine2d(HomMat2D_Inverse, TempRegion, InitRegion);
                        if (InitRegionChanged_Flag)
                        {
                            SetInitMeasRegionSilently(InitRegion);
                        }
                    }
                    if (HasAffineTransform(HomMat2D))
                    {
                        InitRegion = CloneRegion(InitMeasRegion);
                    }
                    else
                    {
                        InitRegion = CloneRegion(TempRegion);
                        TranRegion = CloneRegion(TempRegion);
                    }

                    // 阈值分割流程
                    HOperatorSet.GenEmptyObj(out HObject TmpOutRegion);
                    bool status = ThresholdInRectangle2(tempImage, BinarizationMode, out TmpOutRegion, TranRegion, 
                        FixedMinValue, FixedMaxValue, LocalMaskH, LocalMaskW, LocalStdFactor, LocalAbsTh, LocalType);
                    if (!status || TmpOutRegion == null || !TmpOutRegion.IsInitialized())
                        return NodeStatus.Error;

                    OutRegion = new HRegion(TmpOutRegion.DeepCopy());
                    
                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "red", new HObject(TmpOutRegion), true, true));

                    ShowHRoi();
                    RedisplayInteractiveRegion();

                }
                catch
                {
                    return NodeStatus.Error;
                }

                //执行后对输出参数重新赋值
                var outputValues = OutputParamCollector.GetDataPointValues(this);
                foreach (var item in OutputParams)
                {
                    if (outputValues.TryGetValue(item.ParamName, out object? value))
                    {
                        item.Value = value;
                    }
                }

                UpdateParam();
                return NodeStatus.Success;
            });

            //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：获取区域模块执行时间：{time} 毫秒");
            Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };

            return Task.FromResult(Output);
        }

        public void InitImg()
        {
            if (mWindowH?.hControl == null)
                return;

            mWindowH.hControl.MouseUp -= HControl_MouseUp;
            mWindowH.hControl.MouseUp += HControl_MouseUp;

            ShowHRoi();
            InitRegionMethod();
        }


        private void HControl_MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                ROI roi = mWindowH.WindowH.smallestActiveROI(out string info, out string index);
                if (index.Length > 0)
                {
                    roiRegion = CloneRegion(roi as ROIRectangle2);
                    if (roiRegion != null)
                    {
                        TempRegion.Length1 = Math.Round(roiRegion.Length1, 3);
                        TempRegion.Length2 = Math.Round(roiRegion.Length2, 3);
                        TempRegion.Phi = Math.Round(roiRegion.Phi, 3);
                        TempRegion.MidR = Math.Round(roiRegion.MidR, 3);
                        TempRegion.MidC = Math.Round(roiRegion.MidC, 3);
                        TranRegion = CloneRegion(TempRegion);
                        DisenableAffine2d = true;
                        InitRegionChanged_Flag = true;
                        _ = ExecuteModule();
                        InitRegionChanged_Flag = false;
                    }
                }
            }
            catch
            {
            }
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
            double _Phi1,
                _Phi2,
                _Phi3,
                _Phi;
            tranRect.MidR = TempHomMat2D.AffineTransPoint2d(intRect.MidC, intRect.MidR, out X0);
            tranRect.MidC = X0;
            _Phi1 = ((HTuple)TempHomMat2D[0]).TupleAcos().D;
            _Phi2 = ((HTuple)TempHomMat2D[1]).TupleAsin().D;
            _Phi3 = ((HTuple)TempHomMat2D[4]).TupleAcos().D;
            _Phi = _Phi2 <= 0 ? _Phi1 : -_Phi3;
            tranRect._Phi = intRect.Phi - _Phi;
        }


        public void InitRegionMethod()
        {
            if (TranRegion.FlagLineStyle != null)
            {
                mWindowH.WindowH.genRect2(Serial.ToString(), TranRegion.MidR, TranRegion.MidC, TranRegion.Phi, TranRegion.Length1, TranRegion.Length2, ref RoiList);
            }
            else if (!RoiList.ContainsKey(Serial.ToString()))
            {
                if (TryResolveDisplayRegion(out ROIRectangle2 displayRegion))
                {
                    mWindowH.WindowH.genRect2(Serial.ToString(), displayRegion.MidR, displayRegion.MidC, displayRegion.Phi, displayRegion.Length1, displayRegion.Length2, ref RoiList);
                    TempRegion = CloneRegion(displayRegion);
                    TranRegion = CloneRegion(displayRegion);

                    if (HasValidRegion(InitMeasRegion))
                    {
                        InitRegion = CloneRegion(InitMeasRegion);
                    }
                    else
                    {
                        InitRegion = CloneRegion(displayRegion);
                        SetInitMeasRegionSilently(displayRegion);
                    }
                }
                else
                {
                    mWindowH.WindowH.genRect2(Serial.ToString(), InitRegion.MidR, InitRegion.MidC, InitRegion.Phi, InitRegion.Length1, InitRegion.Length2, ref RoiList);
                    InitRegion = CloneRegion(InitMeasRegion);
                    TranRegion = CloneRegion(InitMeasRegion);
                }

            }
            else if (RoiList.ContainsKey(Serial.ToString()))
            {
                if (HasAffineTransform(HomMat2D_Inverse))
                {
                    mWindowH.WindowH.genRect2(Serial.ToString(), TranRegion.MidR, TranRegion.MidC, TranRegion.Phi, TranRegion.Length1, TranRegion.Length2, ref RoiList);
                    Affine2d(HomMat2D_Inverse, TranRegion, InitRegion);
                    InitRegion.Length1 = Math.Round(InitRegion.Length1, 3);
                    InitRegion.Length2 = Math.Round(InitRegion.Length2, 3);
                    InitRegion.MidR = Math.Round(InitRegion.MidR, 3);
                    InitRegion.MidC = Math.Round(InitRegion.MidC, 3);
                    InitRegion.Phi = Math.Round(InitRegion.Phi, 3);
                    if (InitRegionChanged_Flag)
                    {
                        SetInitMeasRegionSilently(InitRegion);
                    }
                }
                else
                {
                    mWindowH.WindowH.genRect2(Serial.ToString(), InitRegion.MidR, InitRegion.MidC, InitRegion.Phi, InitRegion.Length1, InitRegion.Length2, ref RoiList);
                    if (InitRegionChanged_Flag)
                    {
                        SetInitMeasRegionSilently(InitRegion);
                    }
                }
            }
        }


        private void InitRegionChanged()
        {
            if (InitRegionChanged_Flag == true)
                return;

            InitRegion = CloneRegion(InitMeasRegion);
            DisenableAffine2d = true;
            if (HasValidRegion(InitRegion))
            {
                if (DisenableAffine2d && HasAffineTransform(HomMat2D))
                {
                    Affine2d(HomMat2D, InitRegion, TempRegion);
                    TranRegion = CloneRegion(TempRegion);
                    roiRegion = CloneRegion(TempRegion);
                }
                else
                {
                    roiRegion = CloneRegion(InitRegion);
                    TempRegion = CloneRegion(InitRegion);
                    TranRegion = CloneRegion(InitRegion);
                }
                _ = ExecuteModule();
                InitRegionMethod();
            }
        }


        public void ShowHRoi()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
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

        public void Rectangle22Contour(ROIRectangle2 ho_Rect2, out HObject ho_Contour)
        {
            HOperatorSet.GenEmptyObj(out HObject ho_Rectanle2);

            // 生成矩形2区域
            HOperatorSet.GenRectangle2(out ho_Rectanle2, ho_Rect2.MidR, ho_Rect2.MidC, ho_Rect2.Phi, ho_Rect2.Length1, ho_Rect2.Length2);

            // 将Region转换为XLD轮廓
            HOperatorSet.GenContourRegionXld(ho_Rectanle2, out ho_Contour, "border");
        }

        /// <summary>
        /// 在矩形2区域内进行阈值分割
        /// </summary>
        /// <param name="image"></param>
        /// <param name="result"></param>
        /// <param name="rectangle2"></param>
        /// <param name="minGray"></param>
        /// <param name="maxGray"></param>
        public static bool ThresholdInRectangle2(
            HObject ho_image,
            BinarizationMode thresholdMethod,
            out HObject ho_result,
            ROIRectangle2 rectangle2,
            double minGray, 
            double maxGray,
            int maskH,
            int maskW,
            double stdFactor,
            double absTh,
            LocalType localType)
        {
            try
            {

                HOperatorSet.GenEmptyObj(out ho_result);
                HOperatorSet.GenEmptyObj(out HObject rectangle);
                HOperatorSet.GenEmptyObj(out HObject region);

                HOperatorSet.GenRectangle2(out rectangle, rectangle2.MidR, rectangle2.MidC, -rectangle2.Phi, rectangle2.Length1, rectangle2.Length2);

                HOperatorSet.ReduceDomain(ho_image, rectangle, out HObject imageReduced);

                if (thresholdMethod == BinarizationMode.固定)
                {
                    HOperatorSet.Threshold(imageReduced, out region, minGray, maxGray);
                }
                else if (thresholdMethod == BinarizationMode.局部阈值)
                {
                    HOperatorSet.VarThreshold(imageReduced, out region, maskH, maskW, stdFactor, absTh, localType.ToString());
                }
                else if (thresholdMethod == BinarizationMode.自动暗)
                {
                    HOperatorSet.BinaryThreshold(imageReduced, out region, "max_separability", "dark", out HTuple usedThreshold);
                }
                else if (thresholdMethod == BinarizationMode.自动亮)
                {
                    HOperatorSet.BinaryThreshold(imageReduced, out region, "max_separability", "light", out HTuple usedThreshold);
                }


                HOperatorSet.Intersection(rectangle, region, out ho_result);

                // 释放临时变量
                imageReduced.Dispose();
                region.Dispose();
                rectangle.Dispose();


                return true;
            }

            catch (Exception ex)
            {
                ho_result = null;
                return false;
            }
        }

        private void RefreshInputImageDisplay()
        {
            EnsureWindowControlInitialized();
            if (mWindowH == null)
                return;

            if (!_previewRefreshPending)
                return;

            HImage tempImage = CloneInputImage();
            if (tempImage == null || !tempImage.IsInitialized())
            {
                mWindowH.ClearWindow();
                _previewRefreshPending = false;
                return;
            }

            mWindowH.HobjectToHimage(tempImage);

            if (ShouldInitializeDefaultRegion() && TryCreateFullImageRegion(out ROIRectangle2 fullImageRegion))
            {
                InitRegion = CloneRegion(fullImageRegion);
                TempRegion = CloneRegion(fullImageRegion);
                TranRegion = CloneRegion(fullImageRegion);
                roiRegion = CloneRegion(fullImageRegion);
                SetInitMeasRegionSilently(fullImageRegion);
            }

            _previewRefreshPending = false;
            InitImg();
        }

        private void SetInputImage(TransmitParam value)
        {
            DisposeOwnedInputImage();
            _inputImage = value ?? new TransmitParam();
            _ownsInputImage = false;
            _previewRefreshPending = true;
            InputImageName = _inputImage.Name ?? string.Empty;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(InputImageName));
        }

        private HImage CreateOwnedInputImage(object imageValue)
        {
            try
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
            catch
            {
                return null;
            }
        }

        private HImage CloneInputImage()
        {
            return CreateOwnedInputImage(_inputImage?.Value);
        }

        private bool ShouldInitializeDefaultRegion()
        {
            if (InitRegionChanged_Flag)
                return false;

            if (RoiList.ContainsKey(Serial.ToString()))
                return false;

            return !HasValidRegion(TempRegion)
                && !HasValidRegion(TranRegion)
                && !HasValidRegion(InitRegion)
                && !HasValidRegion(InitMeasRegion);
        }

        private bool TryGetInputImageSize(out int imageWidth, out int imageHeight)
        {
            imageWidth = 0;
            imageHeight = 0;

            try
            {
                switch (_inputImage?.Value)
                {
                    case HImage inputImage when inputImage.IsInitialized():
                        inputImage.GetImageSize(out imageWidth, out imageHeight);
                        return true;
                    case HObject inputObject when inputObject != null && inputObject.IsInitialized():
                        HOperatorSet.GetImageSize(inputObject, out HTuple width, out HTuple height);
                        imageWidth = width.I;
                        imageHeight = height.I;
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool TryCreateFullImageRegion(out ROIRectangle2 fullImageRegion)
        {
            fullImageRegion = null;
            if (!TryGetInputImageSize(out int imageWidth, out int imageHeight))
                return false;

            fullImageRegion = new ROIRectangle2()
            {
                MidR = (imageHeight - 1) / 2.0,
                MidC = (imageWidth - 1) / 2.0,
                Length1 = Math.Max((imageWidth - 1) / 2.0, 0.5),
                Length2 = Math.Max((imageHeight - 1) / 2.0, 0.5),
                Phi = 0
            };
            return true;
        }

        private bool TryResolveDisplayRegion(out ROIRectangle2 displayRegion)
        {
            displayRegion = null;

            if (HasValidRegion(TranRegion))
            {
                displayRegion = CloneRegion(TranRegion);
                return true;
            }

            if (HasValidRegion(TempRegion))
            {
                displayRegion = CloneRegion(TempRegion);
                return true;
            }

            if (HasValidRegion(InitRegion))
            {
                displayRegion = CloneRegion(InitRegion);
                return true;
            }

            if (HasValidRegion(InitMeasRegion))
            {
                displayRegion = CloneRegion(InitMeasRegion);
                if (HasAffineTransform(HomMat2D))
                {
                    Affine2d(HomMat2D, InitMeasRegion, displayRegion);
                }
                return true;
            }

            return TryCreateFullImageRegion(out displayRegion);
        }

        private bool HasAffineTransform(HTuple homMat2D)
        {
            return homMat2D != null && homMat2D.Length > 0;
        }

        private bool HasValidRegion(ROIRectangle2 region)
        {
            return region != null && region.Length1 > 0 && region.Length2 > 0;
        }

        private void RedisplayInteractiveRegion()
        {
            if (mWindowH?.WindowH == null)
                return;

            if (RoiList.ContainsKey(Serial.ToString()))
            {
                mWindowH.WindowH.DispROI(RoiList);
                return;
            }

            InitRegionMethod();
        }

        private ROIRectangle2 CloneRegion(ROIRectangle2 region)
        {
            if (region == null)
                return null;

            return new ROIRectangle2()
            {
                MidR = region.MidR,
                MidC = region.MidC,
                Phi = region.Phi,
                Length1 = region.Length1,
                Length2 = region.Length2
            };
        }

        private void SetInitMeasRegionSilently(ROIRectangle2 region)
        {
            _initMeasRegion = CloneRegion(region) ?? new ROIRectangle2() { Length1 = 0, Length2 = 0, Phi = 0, MidR = 0, MidC = 0 };
            RaisePropertyChanged(nameof(InitMeasRegion));
        }

        private void DisposeOwnedInputImage()
        {
            if (_ownsInputImage && _inputImage?.Value is HImage inputImage)
            {
                inputImage.Dispose();
            }

            _ownsInputImage = false;
        }

        private void EnsureWindowControlInitialized()
        {
            string moduleKey = ModuleName;
            if (string.IsNullOrWhiteSpace(moduleKey))
            {
                if (mWindowH == null)
                {
                    mWindowH = new VMHWindowControl();
                }

                return;
            }

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

            solutionItem.ImgControlPair[moduleKey] = mWindowH;
        }

        #endregion
    }
}

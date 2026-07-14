<<<<<<< .mine
﻿using ALGO.ImagePerProcessing.Models;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ALGO.ImagePerProcessing
{
    [Serializable]
    public class ImagePerProcessingModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        public HImage globelImage { get; set; }
        #endregion

        #region Properties

        [JsonIgnore]
        private HImage image = null;
        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get {
                if (_inputImage != null && _inputImage.Value != null)
                {
                    //拿到克隆对象，不对源数据操作
                    var temp = ((HImage)_inputImage.Value);

                    globelImage = temp.Clone();
                    image = temp.Clone();
                    mWindowH.HobjectToHimage(image);
                    InitImg();
                }

                return _inputImage; }
            set
            {
                _inputImage = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private HRegion roi = null;
        [OutputParam("ROI", "被处理的图像的ROI")]
        [JsonIgnore]
        public HRegion ROI
        {
            get { return roi; }
            set { roi = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputROI = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputROI
        {
            get
            {
                if (_inputROI != null && _inputROI.Value != null)
                {
                    //拿到克隆对象，不对源数据操作
                    var temp = ((HRegion)_inputImage.Value).Clone();
                    ROI = temp.Clone();

                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "blue", new HObject(ROI)));
                    ShowHRoi();
                }

                return _inputROI;
            }
            set
            {
                _inputROI = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private eSelectRoiType _selectedROIType = eSelectRoiType.全图;
        /// <summary>
        /// 处理图像区域
        /// </summary>
        [JsonIgnore]
        public eSelectRoiType SelectedROIType
        {
            get { return _selectedROIType; }
            set
            {
                eSelectRoiType oldValue = _selectedROIType;

                _selectedROIType = value;
                RaisePropertyChanged();

                if (oldValue != value)
                    OnSelectedROITypeChanged(value);
            }
        }

        [JsonIgnore]
        private eCreateRoiType _createROIType = eCreateRoiType.矩形;
        /// <summary>
        /// 创建ROI的类型
        /// </summary>
        [JsonIgnore]
        public eCreateRoiType CreateROIType
        {
            get { return _createROIType; }
            set
            {
                eCreateRoiType oldValue = _createROIType;

                _createROIType = value;
                RaisePropertyChanged();

                if (oldValue != value)
                    OnCreateROITypeChanged();

            }
        }

        [JsonIgnore]
        private bool _IsOutImageReduced = false;
        /// <summary>
        /// 是否输出ROI裁切的图像
        /// </summary>
        public bool IsOutImageReduced
        {
            get { return _IsOutImageReduced; }
            set { SetProperty(ref _IsOutImageReduced, value); }
        }

        [JsonIgnore]
        private double _rect2Len1 = 60;
        /// <summary>
        /// 变换前-绘制矩形ROI Len1的长度
        /// </summary>
        public double Rect2Len1
        {
            get { return _rect2Len1; }
            set
            {
                _rect2Len1 = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2Len2 = 60;
        /// <summary>
        /// 变换前-绘制矩形ROI Len2的长度
        /// </summary>
        public double Rect2Len2
        {
            get { return _rect2Len2; }
            set
            {
                _rect2Len2 = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2CenterX = 300;
        /// <summary>
        /// 变换前-绘制矩形ROI中心点X坐标
        /// </summary>
        public double Rect2CenterX
        {
            get { return _rect2CenterX; }
            set
            {
                _rect2CenterX = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2CenterY = 300;
        /// <summary>
        /// 变换前-绘制矩形ROI中心点Y坐标
        /// </summary>
        public double Rect2CenterY
        {
            get { return _rect2CenterY; }
            set
            {
                _rect2CenterY = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2Deg = 0;
        /// <summary>
        /// 变换前-绘制矩形ROI旋转角度
        /// </summary>
        public double Rect2Deg
        {
            get { return _rect2Deg; }
            set
            {
                _rect2Deg = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleX = 300;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleX
        {
            get { return _circleX; }
            set 
            { 
                _circleX = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleY = 300;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleY
        {
            get { return _circleY; }
            set 
            { 
                _circleY = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleRadius = 60;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleRadius
        {
            get { return _circleRadius; }
            set 
            { 
                _circleRadius = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }


        /// <summary>
        /// 算法模型列表
        /// </summary>
        public ObservableCollection<ModelData> ModelToolList { get; set; } = new ObservableCollection<ModelData>();


        private ModelData _selectedModel = new ModelData();
        /// <summary>
        /// 选中的模型
        /// </summary>
        public ModelData SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        private int _selectedModelIndex;
        /// <summary>
        /// 选中的模型序号
        /// </summary>
        public int SelectedModelIndex
        {
            get { return _selectedModelIndex; }
            set { SetProperty(ref _selectedModelIndex, value); }
        }

        [JsonIgnore]
        /// <summary>
        /// 算法模型
        /// </summary>
        public ModelMethod Model = new ModelMethod();


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [NonSerialized]
        private bool RoiChanged_Flag = false;
        /// <summary> 区域列表 </summary>
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        #endregion

        #region Constructor
        public ImagePerProcessingModel()
        {
            

            TriggerModuleRun += () =>
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
                if (_inputImage.Value != null)
                    (_inputImage.Value as HImage).Dispose();
                _inputImage.Value = (GetTransmitParam(InputParams, _inputImage) as HImage)?.CopyImage();
                image = ((HImage)_inputImage.Value)?.CopyImage();
                mWindowH.HobjectToHimage(image);

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
            PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Remove(Serial.ToString());

            mWindowH.Dispose();

            base.Dispose();
        }
        #endregion


        #region Methods
        private bool IsLoaded_Flag = false;


        private void RoiChanged()
        {
            if (RoiChanged_Flag == true) 
                return;
            RoiChanged_Flag = true;
            IsLoaded_Flag = true;
            ShowHRoi();
            RoiChanged_Flag = false;
            IsLoaded_Flag = false;
        }


        private void OnSelectedROITypeChanged(eSelectRoiType newValue)
        {
            switch (newValue)
            {
                case eSelectRoiType.全图:
                    ClearAllROI();
                    break;

                case eSelectRoiType.链接ROI:
                    ClearAllROI();
                    if (InputROI != null && InputROI.Value != null)
                    {
                        var temp = ((HRegion)InputROI.Value).Clone();
                        ROI = temp.Clone();
                        ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "blue", new HObject(ROI)));
                        ShowHRoi();
                    }
                    break;

                case eSelectRoiType.绘制ROI:
                    ClearAllROI();
                    InitImg();
                    break;
            }
        }

        private void ClearAllROI()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            ROI = null;

            if (globelImage != null && globelImage.IsInitialized())
            {
                mWindowH.HobjectToHimage(globelImage);
                mWindowH.DispImageFitImage();
            }
            else if (Image != null && Image.IsInitialized())
            {
                mWindowH.HobjectToHimage(Image);
                mWindowH.DispImageFitImage();
            }
        }


        private void OnCreateROITypeChanged()
        {
            InitImg();
        }


        private void HControl_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                RoiChanged_Flag = true;
                ROI roi = mWindowH.WindowH.smallestActiveROI(out string info, out string index);
                if (index.Length < 1) 
                    return;
                RoiList[index] = roi;
                switch (CreateROIType)
                {
                    case eCreateRoiType.矩形:
                        ROIRectangle2 rectangle2 = (ROIRectangle2)roi;

                        Rect2Len1 = Math.Round(rectangle2.Length1, 3);
                        Rect2Len2 = Math.Round(rectangle2.Length2, 3);
                        Rect2CenterX = Math.Round(rectangle2.MidC, 3);
                        Rect2CenterY = Math.Round(rectangle2.MidR, 3);
                        Rect2Deg = (double)(new HTuple(-Math.Round(rectangle2.Phi, 3)).TupleDeg());

                        mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, rectangle2.MidR, rectangle2.MidC, rectangle2.Phi, rectangle2.Length1, rectangle2.Length2, ref RoiList);
                        break;

                    case eCreateRoiType.圆形:
                        ROICircle circle = (ROICircle)roi;
                        CircleX = Math.Round(circle.CenterX, 3);
                        CircleY = Math.Round(circle.CenterY, 3);
                        CircleRadius = Math.Round(circle.Radius, 3);
                        
                        mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, circle.CenterY, circle.CenterX, circle.Radius, ref RoiList);
                        break;
                    default:
                        break;
                }
                
                ShowHRoi();
                
            }
            catch (Exception ex)
            {
            }
            finally
            {
                RoiChanged_Flag = false;
            }
        }



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
                    mHRoi.Clear();
                    if (globelImage == null || !globelImage.IsInitialized())
                        return NodeStatus.Error;

                    if(Model == null)
                    {
                        Model = new ModelMethod();
                    }

                    HImage tmpHImage = new HImage(globelImage);
                    foreach (var item in ModelToolList)
                    {
                        switch (item.m_name)
                        {
                            case eOperatorType.RGB转BGR:
                                if (item.m_enable)
                                    Model.RGB2BGR(tmpHImage, out tmpHImage);    
                                break;
                            case eOperatorType.彩色转灰:
                                if (item.m_enable)
                                    Model.TransImage(tmpHImage, out tmpHImage, item.m_TransImageType, item.m_TransImageChannel);// m_MirrorImage);     
                                break;
                            case eOperatorType.图像镜像:
                                if (item.m_enable)
                                    Model.MirrorImage(tmpHImage, out tmpHImage, item.m_MirrorImageType);// m_MirrorImage);             
                                break;
                            case eOperatorType.图像旋转:
                                if (item.m_enable)
                                    Model.RotateImage(tmpHImage, out tmpHImage, item.m_RotateImageAngle);//m_RotateImageAngle);
                                break;
                            case eOperatorType.修改图像尺寸:
                                if (item.m_enable)
                                    Model.ChangeFormat(tmpHImage, out tmpHImage, item.m_ChangeImageWidth, item.m_ChangeImageHeight);//m_ChangeFormatWidth, m_ChangeFormatHeight);
                                break;
                            //TODO：滤波 - Obj
                            case eOperatorType.均值滤波:
                                if (item.m_enable)
                                    Model.MeanImage(tmpHImage, out tmpHImage, item.m_MeanImageWidth, item.m_MeanImageHeight);
                                break;
                            case eOperatorType.中值滤波:
                                if (item.m_enable)
                                    Model.MedianImage(tmpHImage, out tmpHImage, item.m_MedianImageWidth, item.m_MedianImageHeight);
                                break;
                            case eOperatorType.高斯滤波:
                                if (item.m_enable)
                                    Model.GaussImage(tmpHImage, out tmpHImage, item.m_GaussImageSize);
                                break;
                            //TODO：形态学运算 - Obj
                            case eOperatorType.灰度腐蚀:
                                if (item.m_enable)
                                    Model.GrayDilation(tmpHImage, out tmpHImage, item.m_GrayErosionWidth, item.m_GrayErosionHeight);
                                break;
                            case eOperatorType.灰度膨胀:
                                if (item.m_enable)
                                    Model.GrayErosion(tmpHImage, out tmpHImage, item.m_GrayDilationWidth, item.m_GrayDilationHeight);
                                break;
                            //TODO：图像增强 - Obj
                            case eOperatorType.锐化:
                                if (item.m_enable)
                                    Model.EmphaSize(tmpHImage, out tmpHImage, item.m_EmphaSizeWidth, item.m_EmphaSizeHeight, item.m_EmphaSizeFactor);
                                break;
                            case eOperatorType.对比度:
                                if (item.m_enable)
                                    Model.Illuminate(tmpHImage, out tmpHImage, item.m_IlluminateWidth, item.m_IlluminateHeight, item.m_IlluminateFactor);
                                break;
                            case eOperatorType.亮度调节:
                                if (item.m_enable)
                                    Model.ScaleImage(tmpHImage, out tmpHImage, item.m_ScaleImageMult, item.m_ScaleImageAdd);
                                break;
                            case eOperatorType.灰度开运算:
                                if (item.m_enable)
                                    Model.Opening(tmpHImage, out tmpHImage, item.m_OpeningWidth, item.m_OpeningHeight);
                                break;
                            case eOperatorType.灰度闭运算:
                                if (item.m_enable)
                                    Model.Closing(tmpHImage, out tmpHImage, item.m_ClosingWidth, item.m_ClosingHeight);
                                break;
                            case eOperatorType.反色:
                                if (item.m_enable)
                                    Model.InvertImage(tmpHImage, out tmpHImage, item.m_InvertImageLogic);
                                break;
                            //TODO：二值化 - Obj
                            case eOperatorType.二值化:
                                if (item.m_enable)
                                    Model.Threshold(tmpHImage, out tmpHImage, item.m_ThresholdLow, item.m_ThresholdHight, item.m_ThresholdReverse);
                                break;
                            case eOperatorType.均值二值化:
                                if (item.m_enable)
                                    Model.VarThreshold(tmpHImage, out tmpHImage, item.m_VarThresholdWidth, item.m_VarThresholdHeight, item.m_VarThresholdSkew, item.m_VarThresholdType);
                                break;
                        }
                    }

                    Image = new HImage(tmpHImage);
                    if (SelectedROIType != eSelectRoiType.全图)
                    {
                        Image = Image.ReduceDomain(ROI);
                        if (IsOutImageReduced)
                        {
                            Image = Image.CropDomain();
                        }
                    }
                    mWindowH.HobjectToHimage(Image);
                    InitImg();

                    //mWindowH.WindowH._hWndControl.ZoomImage(0,0,1);
                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;
                }

                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    Console.WriteLine(JsonHelper.Serialize(item.Value));
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：图片预处理模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }



        public void InitImg()
        {
            if(SelectedROIType == eSelectRoiType.绘制ROI)
            {
                mWindowH.hControl.MouseUp += HControl_MouseUp;
            }

            ShowHRoi();

            mWindowH.DispImageFitImage();
        }


        public void ShowHRoi()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            if(SelectedROIType == eSelectRoiType.绘制ROI)
            {
                switch (CreateROIType)
                {
                    case eCreateRoiType.矩形:
                        if (RoiList.ContainsKey(ModuleName + ROIDefine.Rectangle2))
                        {
                            ROIRectangle2 ROIRect2 = (ROIRectangle2)RoiList[ModuleName + ROIDefine.Rectangle2];

                            if (IsLoaded_Flag)
                            {
                                mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, Rect2CenterY, Rect2CenterX, (double)(new HTuple(Rect2Deg).TupleRad()), Rect2Len1, Rect2Len2, ref RoiList);
                            }
                            else
                            {
                                mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, ROIRect2.MidR, ROIRect2.MidC, ROIRect2.Phi, ROIRect2.Length1, ROIRect2.Length2, ref RoiList);
                            }
                        }
                        else
                        {
                            mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, Rect2CenterY, Rect2CenterX, (double)(new HTuple(Rect2Deg).TupleRad()), Rect2Len1, Rect2Len2, ref RoiList);
                        }

                        ROI = RoiList[ModuleName + ROIDefine.Rectangle2].GetRegion();

                        break;

                    case eCreateRoiType.圆形:
                        if (RoiList.ContainsKey(ModuleName + ROIDefine.Circle))
                        {
                            ROICircle ROICircle = (ROICircle)RoiList[ModuleName + ROIDefine.Circle];
                            if (IsLoaded_Flag)
                            {
                                mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, CircleY, CircleX, CircleRadius, ref RoiList);
                            }
                            else
                            {
                                mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, ROICircle.CenterY, ROICircle.CenterX, ROICircle.Radius, ref RoiList);
                            }
                        }
                        else
                        {
                            mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, CircleY, CircleX, CircleRadius, ref RoiList);
                        }
                        ROI = RoiList[ModuleName + ROIDefine.Circle].GetRegion();
                        break;
                    default:
                        break;
                }
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(mWindowH.hControl.HalconWindow, roiText.size, "false", "false");
                    ShowTool.SetMsg(mWindowH.hControl.HalconWindow, roiText.text, "image", roiText.row, roiText.col, roiText.drawColor,"false");
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

    
    public enum eSelectRoiType
    {
        全图,
        链接ROI,
        绘制ROI,
    }

    public enum eCreateRoiType
    {
        矩形,
        圆形,
    }



}

||||||| .r1186
﻿using ALGO.ImagePerProcessing.Models;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ALGO.ImagePerProcessing
{
    [Serializable]
    public class ImagePerProcessingModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        public HImage globelImage { get; set; }
        #endregion

        #region Properties

        [JsonIgnore]
        private HImage image = null;
        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get { return image; }
            set { image = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputImage
        {
            get {
                if (_inputImage != null && _inputImage.Value != null)
                {
                    //拿到克隆对象，不对源数据操作
                    var temp = ((HImage)_inputImage.Value);

                    globelImage = temp.Clone();
                    image = temp.Clone();
                    mWindowH.HobjectToHimage(image);
                    InitImg();
                }

                return _inputImage; }
            set
            {
                _inputImage = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private HRegion roi = null;
        [OutputParam("ROI", "被处理的图像的ROI")]
        [JsonIgnore]
        public HRegion ROI
        {
            get { return roi; }
            set { roi = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _inputROI = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        public TransmitParam InputROI
        {
            get
            {
                if (_inputROI != null && _inputROI.Value != null)
                {
                    //拿到克隆对象，不对源数据操作
                    var temp = ((HRegion)_inputImage.Value).Clone();
                    ROI = temp.Clone();

                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "blue", new HObject(ROI)));
                    ShowHRoi();
                }

                return _inputROI;
            }
            set
            {
                _inputROI = value;
                RaisePropertyChanged();
            }
        }


        [JsonIgnore]
        private eSelectRoiType _selectedROIType = eSelectRoiType.全图;
        /// <summary>
        /// 处理图像区域
        /// </summary>
        [JsonIgnore]
        public eSelectRoiType SelectedROIType
        {
            get { return _selectedROIType; }
            set
            {
                eSelectRoiType oldValue = _selectedROIType;

                _selectedROIType = value;
                RaisePropertyChanged();

                if (oldValue != value)
                    OnSelectedROITypeChanged(value);
            }
        }

        [JsonIgnore]
        private eCreateRoiType _createROIType = eCreateRoiType.矩形;
        /// <summary>
        /// 创建ROI的类型
        /// </summary>
        [JsonIgnore]
        public eCreateRoiType CreateROIType
        {
            get { return _createROIType; }
            set
            {
                eCreateRoiType oldValue = _createROIType;

                _createROIType = value;
                RaisePropertyChanged();

                if (oldValue != value)
                    OnCreateROITypeChanged();

            }
        }

        [JsonIgnore]
        private bool _IsOutImageReduced = false;
        /// <summary>
        /// 是否输出ROI裁切的图像
        /// </summary>
        public bool IsOutImageReduced
        {
            get { return _IsOutImageReduced; }
            set { SetProperty(ref _IsOutImageReduced, value); }
        }

        [JsonIgnore]
        private double _rect2Len1 = 60;
        /// <summary>
        /// 变换前-绘制矩形ROI Len1的长度
        /// </summary>
        public double Rect2Len1
        {
            get { return _rect2Len1; }
            set
            {
                _rect2Len1 = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2Len2 = 60;
        /// <summary>
        /// 变换前-绘制矩形ROI Len2的长度
        /// </summary>
        public double Rect2Len2
        {
            get { return _rect2Len2; }
            set
            {
                _rect2Len2 = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2CenterX = 300;
        /// <summary>
        /// 变换前-绘制矩形ROI中心点X坐标
        /// </summary>
        public double Rect2CenterX
        {
            get { return _rect2CenterX; }
            set
            {
                _rect2CenterX = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2CenterY = 300;
        /// <summary>
        /// 变换前-绘制矩形ROI中心点Y坐标
        /// </summary>
        public double Rect2CenterY
        {
            get { return _rect2CenterY; }
            set
            {
                _rect2CenterY = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _rect2Deg = 0;
        /// <summary>
        /// 变换前-绘制矩形ROI旋转角度
        /// </summary>
        public double Rect2Deg
        {
            get { return _rect2Deg; }
            set
            {
                _rect2Deg = value;
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleX = 300;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleX
        {
            get { return _circleX; }
            set 
            { 
                _circleX = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleY = 300;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleY
        {
            get { return _circleY; }
            set 
            { 
                _circleY = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }

        [JsonIgnore]
        private double _circleRadius = 60;
        /// <summary>
        /// 变换前-圆信息
        /// </summary>
        public double CircleRadius
        {
            get { return _circleRadius; }
            set 
            { 
                _circleRadius = value; 
                RaisePropertyChanged();
                RoiChanged();
            }
        }


        /// <summary>
        /// 算法模型列表
        /// </summary>
        public ObservableCollection<ModelData> ModelToolList { get; set; } = new ObservableCollection<ModelData>();


        private ModelData _selectedModel = new ModelData();
        /// <summary>
        /// 选中的模型
        /// </summary>
        public ModelData SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        private int _selectedModelIndex;
        /// <summary>
        /// 选中的模型序号
        /// </summary>
        public int SelectedModelIndex
        {
            get { return _selectedModelIndex; }
            set { SetProperty(ref _selectedModelIndex, value); }
        }

        [JsonIgnore]
        /// <summary>
        /// 算法模型
        /// </summary>
        public ModelMethod Model = new ModelMethod();


        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [NonSerialized]
        private bool RoiChanged_Flag = false;
        /// <summary> 区域列表 </summary>
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        #endregion

        #region Constructor
        public ImagePerProcessingModel()
        {
            

            TriggerModuleRun += () =>
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
                if (_inputImage.Value != null)
                    (_inputImage.Value as HImage).Dispose();
                _inputImage.Value = (GetTransmitParam(InputParams, _inputImage) as HImage)?.CopyImage();
                image = ((HImage)_inputImage.Value)?.CopyImage();
                mWindowH.HobjectToHimage(image);

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
            PrismProvider.ProjectManager.SltCurSolutionItem.NodesOutputCache.Remove(Serial.ToString());

            mWindowH.Dispose();

            base.Dispose();
        }
        #endregion


        #region Methods
        private bool IsLoaded_Flag = false;


        private void RoiChanged()
        {
            if (RoiChanged_Flag == true) 
                return;
            RoiChanged_Flag = true;
            IsLoaded_Flag = true;
            ShowHRoi();
            RoiChanged_Flag = false;
            IsLoaded_Flag = false;
        }


        private void OnSelectedROITypeChanged(eSelectRoiType newValue)
        {
            switch (newValue)
            {
                case eSelectRoiType.全图:
                    ClearAllROI();
                    break;

                case eSelectRoiType.链接ROI:
                    ClearAllROI();
                    if (InputROI != null && InputROI.Value != null)
                    {
                        var temp = ((HRegion)InputROI.Value).Clone();
                        ROI = temp.Clone();
                        ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "blue", new HObject(ROI)));
                        ShowHRoi();
                    }
                    break;

                case eSelectRoiType.绘制ROI:
                    ClearAllROI();
                    InitImg();
                    break;
            }
        }

        private void ClearAllROI()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            ROI = null;

            if (globelImage != null && globelImage.IsInitialized())
            {
                mWindowH.HobjectToHimage(globelImage);
                mWindowH.DispImageFitImage();
            }
            else if (Image != null && Image.IsInitialized())
            {
                mWindowH.HobjectToHimage(Image);
                mWindowH.DispImageFitImage();
            }
        }


        private void OnCreateROITypeChanged()
        {
            InitImg();
        }


        private void HControl_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                RoiChanged_Flag = true;
                ROI roi = mWindowH.WindowH.smallestActiveROI(out string info, out string index);
                if (index.Length < 1) 
                    return;
                RoiList[index] = roi;
                switch (CreateROIType)
                {
                    case eCreateRoiType.矩形:
                        ROIRectangle2 rectangle2 = (ROIRectangle2)roi;

                        Rect2Len1 = Math.Round(rectangle2.Length1, 3);
                        Rect2Len2 = Math.Round(rectangle2.Length2, 3);
                        Rect2CenterX = Math.Round(rectangle2.MidC, 3);
                        Rect2CenterY = Math.Round(rectangle2.MidR, 3);
                        Rect2Deg = (double)(new HTuple(-Math.Round(rectangle2.Phi, 3)).TupleDeg());

                        mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, rectangle2.MidR, rectangle2.MidC, rectangle2.Phi, rectangle2.Length1, rectangle2.Length2, ref RoiList);
                        break;

                    case eCreateRoiType.圆形:
                        ROICircle circle = (ROICircle)roi;
                        CircleX = Math.Round(circle.CenterX, 3);
                        CircleY = Math.Round(circle.CenterY, 3);
                        CircleRadius = Math.Round(circle.Radius, 3);
                        
                        mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, circle.CenterY, circle.CenterX, circle.Radius, ref RoiList);
                        break;
                    default:
                        break;
                }
                
                ShowHRoi();
                
            }
            catch (Exception ex)
            {
            }
            finally
            {
                RoiChanged_Flag = false;
            }
        }



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
                    mHRoi.Clear();
                    if (globelImage == null || !globelImage.IsInitialized())
                        return NodeStatus.Error;

                    if(Model == null)
                    {
                        Model = new ModelMethod();
                    }

                    HImage tmpHImage = new HImage(globelImage);
                    foreach (var item in ModelToolList)
                    {
                        switch (item.m_name)
                        {
                            case eOperatorType.RGB转BGR:
                                if (item.m_enable)
                                    Model.RGB2BGR(tmpHImage, out tmpHImage);    
                                break;
                            case eOperatorType.彩色转灰:
                                if (item.m_enable)
                                    Model.TransImage(tmpHImage, out tmpHImage, item.m_TransImageType, item.m_TransImageChannel);// m_MirrorImage);     
                                break;
                            case eOperatorType.图像镜像:
                                if (item.m_enable)
                                    Model.MirrorImage(tmpHImage, out tmpHImage, item.m_MirrorImageType);// m_MirrorImage);             
                                break;
                            case eOperatorType.图像旋转:
                                if (item.m_enable)
                                    Model.RotateImage(tmpHImage, out tmpHImage, item.m_RotateImageAngle);//m_RotateImageAngle);
                                break;
                            case eOperatorType.修改图像尺寸:
                                if (item.m_enable)
                                    Model.ChangeFormat(tmpHImage, out tmpHImage, item.m_ChangeImageWidth, item.m_ChangeImageHeight);//m_ChangeFormatWidth, m_ChangeFormatHeight);
                                break;
                            //TODO：滤波 - Obj
                            case eOperatorType.均值滤波:
                                if (item.m_enable)
                                    Model.MeanImage(tmpHImage, out tmpHImage, item.m_MeanImageWidth, item.m_MeanImageHeight);
                                break;
                            case eOperatorType.中值滤波:
                                if (item.m_enable)
                                    Model.MedianImage(tmpHImage, out tmpHImage, item.m_MedianImageWidth, item.m_MedianImageHeight);
                                break;
                            case eOperatorType.高斯滤波:
                                if (item.m_enable)
                                    Model.GaussImage(tmpHImage, out tmpHImage, item.m_GaussImageSize);
                                break;
                            //TODO：形态学运算 - Obj
                            case eOperatorType.灰度腐蚀:
                                if (item.m_enable)
                                    Model.GrayDilation(tmpHImage, out tmpHImage, item.m_GrayErosionWidth, item.m_GrayErosionHeight);
                                break;
                            case eOperatorType.灰度膨胀:
                                if (item.m_enable)
                                    Model.GrayErosion(tmpHImage, out tmpHImage, item.m_GrayDilationWidth, item.m_GrayDilationHeight);
                                break;
                            //TODO：图像增强 - Obj
                            case eOperatorType.锐化:
                                if (item.m_enable)
                                    Model.EmphaSize(tmpHImage, out tmpHImage, item.m_EmphaSizeWidth, item.m_EmphaSizeHeight, item.m_EmphaSizeFactor);
                                break;
                            case eOperatorType.对比度:
                                if (item.m_enable)
                                    Model.Illuminate(tmpHImage, out tmpHImage, item.m_IlluminateWidth, item.m_IlluminateHeight, item.m_IlluminateFactor);
                                break;
                            case eOperatorType.亮度调节:
                                if (item.m_enable)
                                    Model.ScaleImage(tmpHImage, out tmpHImage, item.m_ScaleImageMult, item.m_ScaleImageAdd);
                                break;
                            case eOperatorType.灰度开运算:
                                if (item.m_enable)
                                    Model.Opening(tmpHImage, out tmpHImage, item.m_OpeningWidth, item.m_OpeningHeight);
                                break;
                            case eOperatorType.灰度闭运算:
                                if (item.m_enable)
                                    Model.Closing(tmpHImage, out tmpHImage, item.m_ClosingWidth, item.m_ClosingHeight);
                                break;
                            case eOperatorType.反色:
                                if (item.m_enable)
                                    Model.InvertImage(tmpHImage, out tmpHImage, item.m_InvertImageLogic);
                                break;
                            //TODO：二值化 - Obj
                            case eOperatorType.二值化:
                                if (item.m_enable)
                                    Model.Threshold(tmpHImage, out tmpHImage, item.m_ThresholdLow, item.m_ThresholdHight, item.m_ThresholdReverse);
                                break;
                            case eOperatorType.均值二值化:
                                if (item.m_enable)
                                    Model.VarThreshold(tmpHImage, out tmpHImage, item.m_VarThresholdWidth, item.m_VarThresholdHeight, item.m_VarThresholdSkew, item.m_VarThresholdType);
                                break;
                        }
                    }

                    Image = new HImage(tmpHImage);
                    if (SelectedROIType != eSelectRoiType.全图)
                    {
                        Image = Image.ReduceDomain(ROI);
                        if (IsOutImageReduced)
                        {
                            Image = Image.CropDomain();
                        }
                    }
                    mWindowH.HobjectToHimage(Image);
                    InitImg();

                    //mWindowH.WindowH._hWndControl.ZoomImage(0,0,1);
                }
                catch (Exception ex)
                {
                    return NodeStatus.Error;
                }

                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    Console.WriteLine(JsonHelper.Serialize(item.Value));
                }

                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：图片预处理模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }



        public void InitImg()
        {
            if(SelectedROIType == eSelectRoiType.绘制ROI)
            {
                mWindowH.hControl.MouseUp += HControl_MouseUp;
            }

            ShowHRoi();

            mWindowH.DispImageFitImage();
        }


        public void ShowHRoi()
        {
            if (mWindowH != null)
            {
                mWindowH.ClearROI();
            }

            if(SelectedROIType == eSelectRoiType.绘制ROI)
            {
                switch (CreateROIType)
                {
                    case eCreateRoiType.矩形:
                        if (RoiList.ContainsKey(ModuleName + ROIDefine.Rectangle2))
                        {
                            ROIRectangle2 ROIRect2 = (ROIRectangle2)RoiList[ModuleName + ROIDefine.Rectangle2];

                            if (IsLoaded_Flag)
                            {
                                mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, Rect2CenterY, Rect2CenterX, (double)(new HTuple(Rect2Deg).TupleRad()), Rect2Len1, Rect2Len2, ref RoiList);
                            }
                            else
                            {
                                mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, ROIRect2.MidR, ROIRect2.MidC, ROIRect2.Phi, ROIRect2.Length1, ROIRect2.Length2, ref RoiList);
                            }
                        }
                        else
                        {
                            mWindowH.WindowH.genRect2(ModuleName + ROIDefine.Rectangle2, Rect2CenterY, Rect2CenterX, (double)(new HTuple(Rect2Deg).TupleRad()), Rect2Len1, Rect2Len2, ref RoiList);
                        }

                        ROI = RoiList[ModuleName + ROIDefine.Rectangle2].GetRegion();

                        break;

                    case eCreateRoiType.圆形:
                        if (RoiList.ContainsKey(ModuleName + ROIDefine.Circle))
                        {
                            ROICircle ROICircle = (ROICircle)RoiList[ModuleName + ROIDefine.Circle];
                            if (IsLoaded_Flag)
                            {
                                mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, CircleY, CircleX, CircleRadius, ref RoiList);
                            }
                            else
                            {
                                mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, ROICircle.CenterY, ROICircle.CenterX, ROICircle.Radius, ref RoiList);
                            }
                        }
                        else
                        {
                            mWindowH.WindowH.genCircle(ModuleName + ROIDefine.Circle, CircleY, CircleX, CircleRadius, ref RoiList);
                        }
                        ROI = RoiList[ModuleName + ROIDefine.Circle].GetRegion();
                        break;
                    default:
                        break;
                }
            }

            List<HRoi> roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            foreach (HRoi roi in roiList)
            {
                if (roi.roiType == HRoiType.文字显示)
                {
                    HText roiText = (HText)roi;
                    ShowTool.SetFont(mWindowH.hControl.HalconWindow, roiText.size, "false", "false");
                    ShowTool.SetMsg(mWindowH.hControl.HalconWindow, roiText.text, "image", roiText.row, roiText.col, roiText.drawColor,"false");
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

    
    public enum eSelectRoiType
    {
        全图,
        链接ROI,
        绘制ROI,
    }

    public enum eCreateRoiType
    {
        矩形,
        圆形,
    }



}
=======
using ALGO.ImagePerProcessing.Models;
using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.ImagePerProcessing
{
    [Serializable]
    public class ImagePerProcessingModel : ModelParamBase
    {
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        [JsonIgnore]
        public HImage globelImage { get; set; }

        [JsonIgnore]
        private HImage image;

        [OutputParam("Image", "被处理的图像")]
        [JsonIgnore]
        public HImage Image
        {
            get => image;
            set => ReplaceOutputImage(value);
        }

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();

        [InputParam("Image", "输入图像")]
        public TransmitParam InputImage
        {
            get => _inputImage;
            set
            {
                _inputImage = value ?? new TransmitParam();
                RaisePropertyChanged();
                RefreshRuntimeInputObjects(refreshPreview: true, resolveLinks: false);
            }
        }

        [JsonIgnore]
        private HRegion roi;

        [OutputParam("ROI", "被处理的图像的ROI")]
        [JsonIgnore]
        public HRegion ROI
        {
            get => roi;
            set => ReplaceOutputRegion(value);
        }

        [JsonIgnore]
        private TransmitParam _inputROI = new TransmitParam();

        [InputParam("ROI", "输入ROI")]
        public TransmitParam InputROI
        {
            get => _inputROI;
            set
            {
                _inputROI = value ?? new TransmitParam();
                RaisePropertyChanged();
                RefreshRuntimeInputObjects(refreshPreview: true, resolveLinks: false);
            }
        }

        private eSelectRoiType _selectedROIType = eSelectRoiType.全图;

        public eSelectRoiType SelectedROIType
        {
            get => _selectedROIType;
            set
            {
                if (SetProperty(ref _selectedROIType, value))
                {
                    OnSelectedROITypeChanged();
                }
            }
        }

        private eCreateRoiType _createROIType = eCreateRoiType.矩形;

        public eCreateRoiType CreateROIType
        {
            get => _createROIType;
            set
            {
                if (SetProperty(ref _createROIType, value))
                {
                    EnsureEditableRoiInitialized(forceModeRefresh: true);
                    RefreshRoiPreviewOverlays();
                }
            }
        }

        private bool _isOutImageReduced;

        public bool IsOutImageReduced
        {
            get => _isOutImageReduced;
            set => SetProperty(ref _isOutImageReduced, value);
        }

        private double _rect2Len1 = 60;

        public double Rect2Len1
        {
            get => _rect2Len1;
            set
            {
                if (SetProperty(ref _rect2Len1, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _rect2Len2 = 60;

        public double Rect2Len2
        {
            get => _rect2Len2;
            set
            {
                if (SetProperty(ref _rect2Len2, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _rect2CenterX = 300;

        public double Rect2CenterX
        {
            get => _rect2CenterX;
            set
            {
                if (SetProperty(ref _rect2CenterX, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _rect2CenterY = 300;

        public double Rect2CenterY
        {
            get => _rect2CenterY;
            set
            {
                if (SetProperty(ref _rect2CenterY, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _rect2Deg;

        public double Rect2Deg
        {
            get => _rect2Deg;
            set
            {
                if (SetProperty(ref _rect2Deg, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _circleX = 300;

        public double CircleX
        {
            get => _circleX;
            set
            {
                if (SetProperty(ref _circleX, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _circleY = 300;

        public double CircleY
        {
            get => _circleY;
            set
            {
                if (SetProperty(ref _circleY, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        private double _circleRadius = 60;

        public double CircleRadius
        {
            get => _circleRadius;
            set
            {
                if (SetProperty(ref _circleRadius, value))
                {
                    SyncEditableRoiFromProperties();
                }
            }
        }

        public ObservableCollection<ModelData> ModelToolList { get; set; } = new ObservableCollection<ModelData>();

        private ModelData _selectedModel = new ModelData();

        public ModelData SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

        private int _selectedModelIndex;

        public int SelectedModelIndex
        {
            get => _selectedModelIndex;
            set => SetProperty(ref _selectedModelIndex, value);
        }

        [JsonIgnore]
        public ModelMethod Model = new ModelMethod();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private HImage _ownedInputImage;

        [JsonIgnore]
        private HRegion _ownedInputRegion;

        [JsonIgnore]
        private HObject _previewImageObject;

        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get => _previewImageObject;
            private set => SetPreviewImageObject(value);
        }

        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new ObservableCollection<HalconDrawingObject>();

        [JsonIgnore]
        private ImagePerProcessingRoiPreview? _editableRoiPreview;

        [JsonIgnore]
        private bool _suppressRoiPropertySync;

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

                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam())
                {
                    return false;
                }

                RefreshRuntimeInputObjects(refreshPreview: true, resolveLinks: false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载图片预处理参数异常: {ex}");
                return false;
            }
        }

        public override void Dispose()
        {
            DisposeOwnedRuntimeObjects();
            base.Dispose();
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (globelImage == null || !globelImage.IsInitialized())
                    {
                        RefreshRuntimeInputObjects(refreshPreview: false, resolveLinks: true);
                    }

                    if (globelImage == null || !globelImage.IsInitialized())
                    {
                        return NodeStatus.None;
                    }

                    if (Model == null)
                    {
                        Model = new ModelMethod();
                    }

                    HImage workingImage = globelImage.CopyImage();
                    try
                    {
                        foreach (var item in ModelToolList.Where(item => item?.m_enable == true))
                        {
                            ApplyOperator(item, ref workingImage);
                        }

                        HRegion activeRoi = ResolveActiveRoi();
                        HImage outputImage = workingImage;
                        workingImage = null;

                        if (SelectedROIType != eSelectRoiType.全图)
                        {
                            if (activeRoi == null || !activeRoi.IsInitialized())
                            {
                                DisposeHObject(outputImage);
                                return NodeStatus.None;
                            }

                            HImage reducedImage = outputImage.ReduceDomain(activeRoi);
                            DisposeHObject(outputImage);
                            outputImage = reducedImage;

                            if (IsOutImageReduced)
                            {
                                HImage croppedImage = outputImage.CropDomain();
                                DisposeHObject(outputImage);
                                outputImage = croppedImage;
                            }
                        }

                        Image = outputImage;
                        outputImage = null;
                        RefreshPreviewDisplay(useOutputImage: true);
                        RefreshOutputParams();
                    }
                    finally
                    {
                        DisposeHObject(workingImage);
                    }

                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：图片预处理模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public void RefreshPreviewDisplay(bool useOutputImage = false)
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => RefreshPreviewDisplay(useOutputImage)));
                return;
            }

            HImage previewSource = useOutputImage && Image != null && Image.IsInitialized()
                ? Image
                : globelImage;

            SetPreviewImageObject(CreateOwnedImageCopy(previewSource));
            RefreshRoiPreviewOverlays();
        }

        public ImagePerProcessingRoiPreview GetPreviewRoi()
        {
            EnsureEditableRoiInitialized();
            return _editableRoiPreview ?? ImagePerProcessingRoiPreviewGeometry.CreateDefault(CreateROIType, 100, 100);
        }

        public void ApplyPreviewRoi(ImagePerProcessingRoiPreview roiPreview, double imageUnitsPerScreenPixel = 1.0)
        {
            if (SelectedROIType != eSelectRoiType.绘制ROI)
            {
                return;
            }

            _editableRoiPreview = ImagePerProcessingRoiPreviewGeometry.Normalize(roiPreview);
            SyncRoiPropertiesFromPreview(_editableRoiPreview.Value);
            UpdateDrawnRoiFromPreview();
            RefreshRoiPreviewOverlays(imageUnitsPerScreenPixel);
        }

        public void RefreshEditableRoiPreview(double imageUnitsPerScreenPixel = 1.0)
        {
            RemovePreviewObjectsByColor(ImagePerProcessingRoiPreviewStyle.EditableRoiColor);
            if (SelectedROIType != eSelectRoiType.绘制ROI)
            {
                return;
            }

            EnsureEditableRoiInitialized();
            if (!_editableRoiPreview.HasValue)
            {
                return;
            }

            HObject roiContour = null;
            HObject handleContours = null;
            HObject rotateGuideContour = null;
            HObject directionArrowContour = null;
            try
            {
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                var preview = _editableRoiPreview.Value;
                if (CreateEditableRoiContour(preview, imageUnitsPerScreenPixel, out roiContour))
                {
                    AddXldPreviewOverlay(roiContour, ImagePerProcessingRoiPreviewStyle.EditableRoiColor);
                }

                if (CreatePreviewHandles(preview, imageUnitsPerScreenPixel, out handleContours))
                {
                    AddXldPreviewOverlay(handleContours, ImagePerProcessingRoiPreviewStyle.EditableRoiColor);
                }

                if (preview.Mode == eCreateRoiType.矩形)
                {
                    if (CreatePreviewRotateGuide(preview, out rotateGuideContour))
                    {
                        AddXldPreviewOverlay(rotateGuideContour, ImagePerProcessingRoiPreviewStyle.EditableRoiColor);
                    }

                    if (CreatePreviewDirectionArrow(preview, out directionArrowContour))
                    {
                        AddXldPreviewOverlay(directionArrowContour, ImagePerProcessingRoiPreviewStyle.EditableRoiColor);
                    }
                }
            }
            finally
            {
                DisposeHObject(roiContour);
                DisposeHObject(handleContours);
                DisposeHObject(rotateGuideContour);
                DisposeHObject(directionArrowContour);
            }
        }

        private void ApplyOperator(ModelData item, ref HImage workingImage)
        {
            HImage nextImage = null;
            switch (item.m_name)
            {
                case eOperatorType.RGB转BGR:
                    Model.RGB2BGR(workingImage, out nextImage);
                    break;
                case eOperatorType.彩色转灰:
                    Model.TransImage(workingImage, out nextImage, item.m_TransImageType, item.m_TransImageChannel);
                    break;
                case eOperatorType.图像镜像:
                    Model.MirrorImage(workingImage, out nextImage, item.m_MirrorImageType);
                    break;
                case eOperatorType.图像旋转:
                    Model.RotateImage(workingImage, out nextImage, item.m_RotateImageAngle);
                    break;
                case eOperatorType.修改图像尺寸:
                    Model.ChangeFormat(workingImage, out nextImage, item.m_ChangeImageWidth, item.m_ChangeImageHeight);
                    break;
                case eOperatorType.均值滤波:
                    Model.MeanImage(workingImage, out nextImage, item.m_MeanImageWidth, item.m_MeanImageHeight);
                    break;
                case eOperatorType.中值滤波:
                    Model.MedianImage(workingImage, out nextImage, item.m_MedianImageWidth, item.m_MedianImageHeight);
                    break;
                case eOperatorType.高斯滤波:
                    Model.GaussImage(workingImage, out nextImage, item.m_GaussImageSize);
                    break;
                case eOperatorType.灰度腐蚀:
                    Model.GrayErosion(workingImage, out nextImage, item.m_GrayErosionWidth, item.m_GrayErosionHeight);
                    break;
                case eOperatorType.灰度膨胀:
                    Model.GrayDilation(workingImage, out nextImage, item.m_GrayDilationWidth, item.m_GrayDilationHeight);
                    break;
                case eOperatorType.锐化:
                    Model.EmphaSize(workingImage, out nextImage, item.m_EmphaSizeWidth, item.m_EmphaSizeHeight, item.m_EmphaSizeFactor);
                    break;
                case eOperatorType.对比度:
                    Model.Illuminate(workingImage, out nextImage, item.m_IlluminateWidth, item.m_IlluminateHeight, item.m_IlluminateFactor);
                    break;
                case eOperatorType.亮度调节:
                    Model.ScaleImage(workingImage, out nextImage, item.m_ScaleImageMult, item.m_ScaleImageAdd);
                    break;
                case eOperatorType.灰度开运算:
                    Model.Opening(workingImage, out nextImage, item.m_OpeningWidth, item.m_OpeningHeight);
                    break;
                case eOperatorType.灰度闭运算:
                    Model.Closing(workingImage, out nextImage, item.m_ClosingWidth, item.m_ClosingHeight);
                    break;
                case eOperatorType.反色:
                    Model.InvertImage(workingImage, out nextImage, item.m_InvertImageLogic);
                    break;
                case eOperatorType.二值化:
                    Model.Threshold(workingImage, out nextImage, item.m_ThresholdLow, item.m_ThresholdHight, item.m_ThresholdReverse);
                    break;
                case eOperatorType.均值二值化:
                    Model.VarThreshold(workingImage, out nextImage, item.m_VarThresholdWidth, item.m_VarThresholdHeight, item.m_VarThresholdSkew, item.m_VarThresholdType);
                    break;
                default:
                    return;
            }

            ReplaceWorkingImage(ref workingImage, nextImage);
        }

        private void RefreshRuntimeInputObjects(bool refreshPreview, bool resolveLinks)
        {
            try
            {
                if (resolveLinks)
                {
                    _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
                    _inputROI.Value = GetTransmitParam(InputParams, _inputROI);
                }

                HImage newImage = CreateOwnedImageCopy(_inputImage.Value);
                ReplaceOwnedInputImage(newImage);
                HRegion newRegion = CreateOwnedRegionCopy(_inputROI.Value);
                ReplaceOwnedInputRegion(newRegion);

                if (SelectedROIType == eSelectRoiType.链接ROI && _ownedInputRegion != null && _ownedInputRegion.IsInitialized())
                {
                    ROI = CreateOwnedRegionCopy(_ownedInputRegion);
                }
                else if (SelectedROIType == eSelectRoiType.绘制ROI)
                {
                    UpdateDrawnRoiFromPreview();
                }
                else
                {
                    ROI = null;
                }

                if (refreshPreview)
                {
                    RefreshPreviewDisplay();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnSelectedROITypeChanged()
        {
            if (SelectedROIType == eSelectRoiType.链接ROI)
            {
                if (_ownedInputRegion == null || !_ownedInputRegion.IsInitialized())
                {
                    RefreshRuntimeInputObjects(refreshPreview: false, resolveLinks: true);
                }

                ROI = CreateOwnedRegionCopy(_ownedInputRegion);
            }
            else if (SelectedROIType == eSelectRoiType.绘制ROI)
            {
                EnsureEditableRoiInitialized();
                UpdateDrawnRoiFromPreview();
            }
            else
            {
                ROI = null;
            }

            RefreshPreviewDisplay();
        }

        private HRegion ResolveActiveRoi()
        {
            switch (SelectedROIType)
            {
                case eSelectRoiType.全图:
                    return null;
                case eSelectRoiType.链接ROI:
                    if (_ownedInputRegion == null || !_ownedInputRegion.IsInitialized())
                    {
                        RefreshRuntimeInputObjects(refreshPreview: false, resolveLinks: true);
                    }
                    return _ownedInputRegion;
                case eSelectRoiType.绘制ROI:
                    UpdateDrawnRoiFromPreview();
                    return ROI;
                default:
                    return null;
            }
        }

        private void EnsureEditableRoiInitialized(bool forceModeRefresh = false)
        {
            if (SelectedROIType != eSelectRoiType.绘制ROI)
            {
                return;
            }

            bool needCreate = !_editableRoiPreview.HasValue
                || forceModeRefresh && _editableRoiPreview.Value.Mode != CreateROIType;

            if (!needCreate)
            {
                _editableRoiPreview = ImagePerProcessingRoiPreviewGeometry.Normalize(_editableRoiPreview.Value);
                return;
            }

            if (!TryGetPreviewImageSize(out double width, out double height))
            {
                width = 100;
                height = 100;
            }

            _editableRoiPreview = ImagePerProcessingRoiPreviewGeometry.CreateDefault(CreateROIType, width, height);
            SyncRoiPropertiesFromPreview(_editableRoiPreview.Value);
        }

        private void SyncEditableRoiFromProperties()
        {
            if (_suppressRoiPropertySync || SelectedROIType != eSelectRoiType.绘制ROI)
            {
                return;
            }

            double angle = new HTuple(Rect2Deg).TupleRad().D;
            _editableRoiPreview = ImagePerProcessingRoiPreviewGeometry.Normalize(new ImagePerProcessingRoiPreview(
                CreateROIType,
                CreateROIType == eCreateRoiType.圆形 ? CircleX : Rect2CenterX,
                CreateROIType == eCreateRoiType.圆形 ? CircleY : Rect2CenterY,
                Rect2Len1,
                Rect2Len2,
                angle,
                CircleRadius));

            UpdateDrawnRoiFromPreview();
            RefreshRoiPreviewOverlays();
        }

        private void SyncRoiPropertiesFromPreview(ImagePerProcessingRoiPreview preview)
        {
            try
            {
                _suppressRoiPropertySync = true;
                if (preview.Mode == eCreateRoiType.圆形)
                {
                    CircleX = Math.Round(preview.CenterX, 3);
                    CircleY = Math.Round(preview.CenterY, 3);
                    CircleRadius = Math.Round(preview.Radius, 3);
                }
                else
                {
                    Rect2CenterX = Math.Round(preview.CenterX, 3);
                    Rect2CenterY = Math.Round(preview.CenterY, 3);
                    Rect2Len1 = Math.Round(preview.Length1, 3);
                    Rect2Len2 = Math.Round(preview.Length2, 3);
                    Rect2Deg = Math.Round(new HTuple(preview.Angle).TupleDeg().D, 3);
                }
            }
            finally
            {
                _suppressRoiPropertySync = false;
            }
        }

        private bool UpdateDrawnRoiFromPreview()
        {
            EnsureEditableRoiInitialized();
            if (!_editableRoiPreview.HasValue)
            {
                return false;
            }

            HObject newRegionObject = null;
            try
            {
                var preview = _editableRoiPreview.Value;
                if (preview.Mode == eCreateRoiType.圆形)
                {
                    HOperatorSet.GenCircle(out newRegionObject, preview.CenterY, preview.CenterX, preview.Radius);
                }
                else
                {
                    HOperatorSet.GenRectangle2(out newRegionObject, preview.CenterY, preview.CenterX, preview.Angle, preview.Length1, preview.Length2);
                }

                if (newRegionObject == null || !newRegionObject.IsInitialized())
                {
                    return false;
                }

                ROI = new HRegion(newRegionObject);
                newRegionObject = null;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                DisposeHObject(newRegionObject);
            }
        }

        private void RefreshRoiPreviewOverlays(double imageUnitsPerScreenPixel = 1.0)
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => RefreshRoiPreviewOverlays(imageUnitsPerScreenPixel)));
                return;
            }

            ClearPreviewDrawObjects();
            if (SelectedROIType == eSelectRoiType.链接ROI)
            {
                AddRegionPreviewOverlay(_ownedInputRegion, "blue", false);
            }
            else if (SelectedROIType == eSelectRoiType.绘制ROI)
            {
                RefreshEditableRoiPreview(imageUnitsPerScreenPixel);
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

        private void AddRegionPreviewOverlay(HObject regionObject, string color, bool fill)
        {
            if (regionObject == null || !regionObject.IsInitialized())
            {
                return;
            }

            try
            {
                PreviewDrawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = regionObject.Clone(),
                    Color = color,
                    IsFillDisplay = fill
                });
            }
            catch
            {
            }
        }

        private static bool CreateEditableRoiContour(
            ImagePerProcessingRoiPreview preview,
            double imageUnitsPerScreenPixel,
            out HObject roiContour)
        {
            roiContour = null;
            try
            {
                preview = ImagePerProcessingRoiPreviewGeometry.Normalize(preview);
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);

                if (preview.Mode == eCreateRoiType.圆形)
                {
                    HOperatorSet.GenCircleContourXld(
                        out roiContour,
                        preview.CenterY,
                        preview.CenterX,
                        Math.Max(1.0, preview.Radius),
                        0,
                        Math.PI * 2.0,
                        "positive",
                        Math.Max(1.0, imageUnitsPerScreenPixel));
                }
                else
                {
                    HOperatorSet.GenRectangle2ContourXld(
                        out roiContour,
                        preview.CenterY,
                        preview.CenterX,
                        preview.Angle,
                        Math.Max(1.0, preview.Length1),
                        Math.Max(1.0, preview.Length2));
                }

                return roiContour != null && roiContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(roiContour);
                roiContour = null;
                return false;
            }
        }

        private static bool CreatePreviewHandles(
            ImagePerProcessingRoiPreview preview,
            double imageUnitsPerScreenPixel,
            out HObject handleContours)
        {
            handleContours = null;
            HObject tempContour = null;
            try
            {
                imageUnitsPerScreenPixel = Math.Max(0.1, imageUnitsPerScreenPixel);
                foreach (var pair in ImagePerProcessingRoiPreviewGeometry.GetHandlePoints(preview))
                {
                    double screenSize = pair.Key == ImagePerProcessingRoiPreviewHandle.Center
                        ? ImagePerProcessingRoiPreviewStyle.CenterHandleScreenSize
                        : ImagePerProcessingRoiPreviewStyle.HandleScreenSize;
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

                    ConcatOwnedObject(ref handleContours, tempContour);
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

        private static bool CreatePreviewRotateGuide(ImagePerProcessingRoiPreview preview, out HObject rotateGuideContour)
        {
            rotateGuideContour = null;
            try
            {
                var handles = ImagePerProcessingRoiPreviewGeometry.GetHandlePoints(preview);
                if (!handles.TryGetValue(ImagePerProcessingRoiPreviewHandle.Right, out var right)
                    || !handles.TryGetValue(ImagePerProcessingRoiPreviewHandle.Rotate, out var rotate))
                {
                    return false;
                }

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

        private static bool CreatePreviewDirectionArrow(ImagePerProcessingRoiPreview preview, out HObject directionArrowContour)
        {
            directionArrowContour = null;
            try
            {
                preview = ImagePerProcessingRoiPreviewGeometry.Normalize(preview);
                double axisX = Math.Cos(preview.Angle);
                double axisY = -Math.Sin(preview.Angle);
                double headSize = Math.Max(8.0, Math.Min(18.0, Math.Min(preview.Length1, preview.Length2) * 0.25));
                double arrowLength = Math.Min(Math.Max(preview.Length1 * 0.65, headSize * 2.0), 80.0);
                double tipDistance = Math.Max(headSize, preview.Length1 - headSize * 0.9);
                double tipCol = preview.CenterX + axisX * tipDistance;
                double tipRow = preview.CenterY + axisY * tipDistance;
                double startCol = tipCol - axisX * arrowLength;
                double startRow = tipRow - axisY * arrowLength;

                directionArrowContour = CreateArrowContour(
                    startRow,
                    startCol,
                    tipRow,
                    tipCol,
                    headSize,
                    ImagePerProcessingRoiPreviewStyle.DirectionArrowHeadScreenSize);
                return directionArrowContour != null && directionArrowContour.IsInitialized();
            }
            catch
            {
                DisposeHObject(directionArrowContour);
                directionArrowContour = null;
                return false;
            }
        }

        private static HObject CreateArrowContour(
            double row1,
            double column1,
            double row2,
            double column2,
            double headLength,
            double headWidth)
        {
            double deltaRow = row2 - row1;
            double deltaColumn = column2 - column1;
            double length = Math.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn);
            if (length <= double.Epsilon)
            {
                HOperatorSet.GenContourPolygonXld(out HObject pointContour, new HTuple(row1), new HTuple(column1));
                return pointContour;
            }

            double rowDirection = deltaRow / length;
            double columnDirection = deltaColumn / length;
            double effectiveHeadLength = Math.Min(Math.Max(1.0, headLength), length);
            double halfHeadWidth = Math.Max(1.0, headWidth) / 2.0;
            double headBaseRow = row1 + (length - effectiveHeadLength) * rowDirection;
            double headBaseColumn = column1 + (length - effectiveHeadLength) * columnDirection;

            double rowP1 = headBaseRow + halfHeadWidth * columnDirection;
            double columnP1 = headBaseColumn - halfHeadWidth * rowDirection;
            double rowP2 = headBaseRow - halfHeadWidth * columnDirection;
            double columnP2 = headBaseColumn + halfHeadWidth * rowDirection;

            HOperatorSet.GenContourPolygonXld(
                out HObject arrowContour,
                new HTuple(new[] { row1, row2, rowP1, row2, rowP2, row2 }),
                new HTuple(new[] { column1, column2, columnP1, column2, columnP2, column2 }));
            return arrowContour;
        }

        private void RefreshOutputParams()
        {
            Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (item == null || string.IsNullOrEmpty(item.ParamName))
                {
                    continue;
                }

                if (!values.TryGetValue(item.ParamName, out object newValue))
                {
                    continue;
                }

                object clonedValue = CloneOutputValue(newValue);
                DisposeOutputParamValue(item.Value);
                item.Value = clonedValue;
            }

            if (!UpdateParam())
            {
                Console.WriteLine($"模块_{Serial}更新参数失败");
            }
        }

        private object CloneOutputValue(object value)
        {
            return value switch
            {
                HImage hImage when hImage.IsInitialized() => hImage.CopyImage(),
                HRegion hRegion when hRegion.IsInitialized() => new HRegion(hRegion.Clone()),
                HObject hObject when hObject.IsInitialized() => hObject.Clone(),
                _ => value
            };
        }

        private static HImage CreateOwnedImageCopy(object value)
        {
            try
            {
                switch (value)
                {
                    case HImage hImage when hImage.IsInitialized():
                        return hImage.CopyImage();
                    case HObject hObject when hObject.IsInitialized():
                        using (var temp = new HImage(hObject))
                        {
                            return temp.CopyImage();
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

        private static HRegion CreateOwnedRegionCopy(object value)
        {
            try
            {
                switch (value)
                {
                    case HRegion hRegion when hRegion.IsInitialized():
                        return new HRegion(hRegion.Clone());
                    case HObject hObject when hObject.IsInitialized():
                        return new HRegion(hObject.Clone());
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private void ReplaceOwnedInputImage(HImage newImage)
        {
            HImage oldImage = _ownedInputImage;
            _ownedInputImage = newImage;
            globelImage = _ownedInputImage;
            if (!ReferenceEquals(oldImage, newImage))
            {
                DisposeHObject(oldImage);
            }
        }

        private void ReplaceOwnedInputRegion(HRegion newRegion)
        {
            HRegion oldRegion = _ownedInputRegion;
            _ownedInputRegion = newRegion;
            if (!ReferenceEquals(oldRegion, newRegion))
            {
                DisposeHObject(oldRegion);
            }
        }

        private void ReplaceOutputImage(HImage newImage)
        {
            HImage oldImage = image;
            image = newImage;
            RaisePropertyChanged(nameof(Image));
            if (!ReferenceEquals(oldImage, newImage))
            {
                DisposeHObject(oldImage);
            }
        }

        private void ReplaceOutputRegion(HRegion newRegion)
        {
            HRegion oldRegion = roi;
            roi = newRegion;
            RaisePropertyChanged(nameof(ROI));
            if (!ReferenceEquals(oldRegion, newRegion))
            {
                DisposeHObject(oldRegion);
            }
        }

        private void SetPreviewImageObject(HObject newImage)
        {
            HObject oldImage = _previewImageObject;
            _previewImageObject = newImage;
            RaisePropertyChanged(nameof(PreviewImageObject));
            if (!ReferenceEquals(oldImage, newImage))
            {
                DisposeHObject(oldImage);
            }
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
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects.Where(item => item?.Color == color).ToList())
            {
                DisposeHObject(drawObject.Hobject);
                PreviewDrawObjects.Remove(drawObject);
            }
        }

        private bool TryGetPreviewImageSize(out double width, out double height)
        {
            width = 0;
            height = 0;
            try
            {
                HImage source = globelImage ?? Image;
                if (source == null || !source.IsInitialized())
                {
                    return false;
                }

                HOperatorSet.GetImageSize(source, out HTuple widthTuple, out HTuple heightTuple);
                if (widthTuple.Length == 0 || heightTuple.Length == 0 || widthTuple.D <= 0 || heightTuple.D <= 0)
                {
                    return false;
                }

                width = widthTuple.D;
                height = heightTuple.D;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceWorkingImage(ref HImage current, HImage next)
        {
            if (next == null || !next.IsInitialized())
            {
                DisposeHObject(next);
                return;
            }

            HImage old = current;
            current = next;
            if (!ReferenceEquals(old, next))
            {
                DisposeHObject(old);
            }
        }

        private static void ConcatOwnedObject(ref HObject target, HObject source)
        {
            if (source == null || !source.IsInitialized())
            {
                return;
            }

            if (target == null || !target.IsInitialized())
            {
                target = source.Clone();
                return;
            }

            HObject oldTarget = target;
            target = oldTarget.ConcatObj(source);
            DisposeHObject(oldTarget);
        }

        private static void DisposeOutputParamValue(object value)
        {
            if (value is HObject hObject)
            {
                DisposeHObject(hObject);
            }
        }

        private static void DisposeHObject(HObject hObject)
        {
            if (hObject == null)
            {
                return;
            }

            try
            {
                hObject.Dispose();
            }
            catch
            {
            }
        }

        private void DisposeOwnedRuntimeObjects()
        {
            ClearPreviewDrawObjects();
            SetPreviewImageObject(null);
            ReplaceOwnedInputImage(null);
            ReplaceOwnedInputRegion(null);
            Image = null;
            ROI = null;
        }
    }

    public enum eSelectRoiType
    {
        全图,
        链接ROI,
        绘制ROI,
    }

    public enum eCreateRoiType
    {
        矩形,
        圆形,
    }
}
>>>>>>> .r1220

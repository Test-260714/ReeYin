using ALGO.RegionTrans.ViewModels;
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
using Prism.Navigation.Regions;
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static ALGO.RegionTrans.ViewModels.RegionTransViewModel;

namespace ALGO.RegionTrans
{
    [Serializable]
    public class RegionTransModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        [JsonIgnore]
        private bool _ownsInputImage;

        [JsonIgnore]
        private bool _previewRefreshPending = true;

        #endregion

        #region Properties

        [JsonIgnore]
        private HObject _initRegion = new HObject();
        /// <summary>
        /// 输入区域1信息
        /// </summary>
        public HObject InitRegion
        {
            get { return _initRegion; }
            set { SetProperty(ref _initRegion, value); }
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
                SetInputImage(value);
            }
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
        private TransmitParam _inputRegion = new TransmitParam();
        /// <summary>
        /// 输入区域1参数
        /// </summary>
        public TransmitParam InputRegion
        {
            get
            {
                RefreshInputRegionPreview();
                return _inputRegion;
            }
            set
            {
                _inputRegion = value ?? new TransmitParam();
                RefreshInputRegionPreview();
                RaisePropertyChanged();
            }
        }

        /// <summary> 区域列表 </summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;


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


        private RegionTransMode _regionTransMode = RegionTransMode.形状转换;
        [RecipeParam("转换方式", "区域转换模式")]
        public RegionTransMode RegionTransMode
        {
            get => _regionTransMode;
            set
            {
                if (SetProperty(ref _regionTransMode, value))
                {
                    UpdateTransTypeList();
                    RaisePropertyChanged(nameof(TransType));
                }
            }
        }

        private RegionTransType _regionTransType = RegionTransType.凸包性;
        [RecipeParam("形状转换类型", "形状转换模式下使用的转换类型")]
        public RegionTransType RegionTransType
        {
            get { return _regionTransType; }
            set
            {
                if (SetProperty(ref _regionTransType, value))
                    RaisePropertyChanged(nameof(TransType));
            }
        }

        private SmallRegionType _smallRegionType = SmallRegionType.圆形;
        [RecipeParam("最小形状类型", "最小形状模式下使用的输出形状")]
        public SmallRegionType SmallRegionType
        {
            get { return _smallRegionType; }
            set
            {
                if (SetProperty(ref _smallRegionType, value))
                    RaisePropertyChanged(nameof(TransType));
            }
        }

        [JsonIgnore]
        private ObservableCollection<Enum> _transTypes = new ObservableCollection<Enum>();
        [JsonIgnore]
        public ObservableCollection<Enum> TransTypes
        {
            get => _transTypes;
            set => SetProperty(ref _transTypes, value);
        }

        [JsonIgnore]
        public Enum TransType
        {
            get => RegionTransMode == RegionTransMode.形状转换
                ? RegionTransType
                : SmallRegionType;
            set
            {
                switch (value)
                {
                    case RegionTransType regionTransType:
                        RegionTransType = regionTransType;
                        break;
                    case SmallRegionType smallRegionType:
                        SmallRegionType = smallRegionType;
                        break;
                }

                RaisePropertyChanged();
            }
        }

        #endregion

        #region Constructor

        public RegionTransModel()
        {
            UpdateTransTypeList();
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
                UpdateTransTypeList();

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"区域转换模块一次初始化异常：{ex.StackTrace}");
                return false;
            }
        }

        public override void Dispose()
        {
            DisposeOwnedInputImage();
            base.Dispose();
        }

        /// <summary>
        /// 加载并解析当前节点的链接输入参数。
        /// </summary>
        /// <returns>参数加载是否成功。</returns>
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

                object imageValue = ResolveLinkedInputImageValue();
                _inputImage.Value = CreateOwnedInputImage(imageValue);
                _ownsInputImage = _inputImage.Value is HImage;
                InputImageName = _inputImage?.Name ?? string.Empty;

                object regionValue = GetTransmitParam(InputParams, _inputRegion);
                if (regionValue != null)
                {
                    _inputRegion.Value = regionValue;
                    HObject loadedRegion = CloneInputRegion(regionValue);
                    if (loadedRegion != null && loadedRegion.IsInitialized())
                    {
                        InitRegion = loadedRegion;
                    }
                }

                if (refreshPreview)
                {
                    _previewRefreshPending = true;
                    RefreshInputImagePreview();
                    RefreshInputRegionPreview();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"区域转换模块加载参数异常：{ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {

                    if (!LoadKeyParam(false))
                        return NodeStatus.Error;

                    if (_inputRegion.Value == null)
                        return NodeStatus.None;
                    HObject tempRegion = InitRegion.DeepClone();
                    mHRoi.Clear();
                    if (tempRegion == null && !tempRegion.IsInitialized())
                        return NodeStatus.Error;

                    //执行的方法
                    HOperatorSet.GenEmptyObj(out HObject TmpOutRegion);
                    bool status = TransRegion(tempRegion, out TmpOutRegion);

                    if (status)
                    {
                        //输出区域
                        OutRegion = new HRegion(TmpOutRegion.DeepCopy());

                        ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "green", OutRegion, true, true));
                        ShowHRoi();
                    }

                    else if (!status)
                        return NodeStatus.Error;


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

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：区域转换模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }


        public bool TransRegion(HObject region, out HObject resultRegion)
        {
            if (region == null)
            {
                resultRegion = null;
                return false;
            }

            try 
            {
                HOperatorSet.GenEmptyObj(out HObject tmpResultRegion);

                if (RegionTransMode == RegionTransMode.形状转换)
                {
                    HOperatorSet.ShapeTrans(region, out tmpResultRegion, TransType.GetDescription());
                }
                else if (RegionTransMode == RegionTransMode.最小形状)
                {
                    if (TransType is SmallRegionType small)
                    {
                        if (small == SmallRegionType.圆形)
                        {
                            HOperatorSet.SmallestCircle(region, out HTuple row, out HTuple col, out HTuple radius);
                            HOperatorSet.GenCircle(out tmpResultRegion, row, col, radius);
                        }
                        else if (small == SmallRegionType.矩形1)
                        {
                            HOperatorSet.SmallestRectangle1(region, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                            HOperatorSet.GenRectangle1(out tmpResultRegion, row1, col1, row2, col2);
                        }
                        else if (small == SmallRegionType.矩形2)
                        {
                            HOperatorSet.SmallestRectangle2(region, out HTuple row, out HTuple col, out HTuple phi, out HTuple length1, out HTuple length2);
                            HOperatorSet.GenRectangle2(out tmpResultRegion, row, col, phi, length1, length2);
                        }
                    }              
                }

                resultRegion = tmpResultRegion;
                return true;
            }
            catch (Exception ex)
            {
                resultRegion = null;
                return false;
            }

        }

        public void UpdateTransTypeList()
        {
            Array list;

            if (RegionTransMode == RegionTransMode.形状转换)
                list = Enum.GetValues(typeof(RegionTransType));
            else
                list = Enum.GetValues(typeof(SmallRegionType));

            TransTypes = new ObservableCollection<Enum>(list.Cast<Enum>());
            RaisePropertyChanged(nameof(TransType));
        }

        public void RefreshInputImagePreview()
        {
            EnsureWindowControlInitialized();
            if (mWindowH == null)
            {
                return;
            }

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

                mWindowH.ClearWindow();
                _previewRefreshPending = false;
                return;
            }

            mWindowH.HobjectToHimage(previewImage);
            _previewRefreshPending = false;
            InitImg();
        }

        public void RequestInputImagePreviewRefresh()
        {
            _previewRefreshPending = true;
            RefreshInputImagePreview();
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

        private void SetInputImage(TransmitParam value)
        {
            DisposeOwnedInputImage();
            _inputImage = value ?? new TransmitParam();
            _ownsInputImage = false;
            _previewRefreshPending = true;
            InputImageName = _inputImage.Name ?? string.Empty;
            ClearIncomingLinkedInputImageValue();
            EnsureInputImageValueResolved();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(InputImageName));
            RefreshInputImagePreview();
        }

        private void EnsureInputImageValueResolved()
        {
            if (_inputImage == null || _inputImage.Value != null)
            {
                return;
            }

            object imageValue = ResolveLinkedInputImageValue();
            HImage ownedImage = CreateOwnedInputImage(imageValue);
            if (ownedImage == null || !ownedImage.IsInitialized())
            {
                ownedImage?.Dispose();
                return;
            }

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
            object imageValue = GetTransmitParam(InputParams, _inputImage);
            if (imageValue != null)
            {
                return imageValue;
            }

            imageValue = FindLinkedInputImageParam()?.Value;
            if (imageValue != null)
            {
                return imageValue;
            }

            imageValue = FindCurrentGlobalInputImageParam()?.Value;
            if (imageValue != null)
            {
                return imageValue;
            }

            return FindCachedInputImageParam()?.Value;
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

        private static TransmitParam FindMatchingTransmitParam(
            IEnumerable<TransmitParam> candidates,
            TransmitParam target)
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

        private void DisposeOwnedInputImage()
        {
            if (_ownsInputImage && _inputImage?.Value is HImage inputImage)
            {
                inputImage.Dispose();
            }

            _ownsInputImage = false;
        }

        private void RefreshInputRegionPreview()
        {
            HObject previewRegion = CloneInputRegion(_inputRegion?.Value);
            if (previewRegion == null || !previewRegion.IsInitialized())
            {
                return;
            }

            InitRegion = previewRegion;
            EnsureWindowControlInitialized();
            EnsureInputImageValueResolved();
            RefreshInputImagePreview();
            if (CloneInputImage() == null && !HasInputImageLink())
            {
                CreateBlankPreviewForRegion(previewRegion);
            }

            ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "red", InitRegion, true, true));
            ShowHRoi();
        }

        private HObject CloneInputRegion(object regionValue)
        {
            try
            {
                switch (regionValue)
                {
                    case HRegion region:
                        return region.Clone();
                    case HObject hObject when hObject.IsInitialized():
                        return hObject.Clone();
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private void CreateBlankPreviewForRegion(HObject region)
        {
            try
            {
                HOperatorSet.SmallestRectangle1(region, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                int width = Math.Max(1, (int)Math.Ceiling(column2.D + Math.Max(10, column1.D)));
                int height = Math.Max(1, (int)Math.Ceiling(row2.D + Math.Max(10, row1.D)));
                HImage hImage = new();
                hImage.GenImageConst("byte", width, height);
                hImage = hImage + 255;
                mWindowH?.HobjectToHimage(hImage);
                InitImg();
            }
            catch
            {
            }
        }

        public void InitImg()
        {
            mWindowH?.DispImageFitImage();
        }

        private void EnsureWindowControlInitialized()
        {
            string moduleKey = ModuleName;
            if (string.IsNullOrWhiteSpace(moduleKey))
            {
                mWindowH ??= new VMHWindowControl();
                return;
            }

            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem?.ImgControlPair == null)
            {
                mWindowH ??= new VMHWindowControl();
                return;
            }

            if (solutionItem.ImgControlPair.TryGetValue(moduleKey, out object windowControl) &&
                windowControl is VMHWindowControl existWindow)
            {
                mWindowH = existWindow;
                return;
            }

            mWindowH ??= new VMHWindowControl();
            solutionItem.ImgControlPair[moduleKey] = mWindowH;
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

        #endregion
    }


    public static class RegionTransExtensions
    {
        /// <summary>
        /// 获取枚举值对应的 Description 特性描述。
        /// 如果没有定义 Description，则返回枚举名称。
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>描述文本</returns>
        public static string GetDescription(this Enum value)
        {
            if (value == null)
                return string.Empty;

            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null)
                return string.Empty;

            var field = type.GetField(name);
            if (field == null)
                return name;

            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? name;
        }
    }

}

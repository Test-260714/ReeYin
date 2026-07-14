using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
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
using ALGO.FilterRegion.ViewModels;
using static ALGO.FilterRegion.ViewModels.FilterRegionViewModel;
using ReeYin_V.Core.Extension;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.FilterRegion
{
    [Serializable]
    public class FilterRegionModel : ModelParamBase
    {
        #region Fields
        [JsonIgnore]
        public override VMHWindowControl mWindowH { set; get; }

        #endregion

        #region Properties

        [JsonIgnore]
        private TransmitParam _inputImage = new TransmitParam();
        /// <summary>
        /// 输入图像参数
        /// </summary>
        [InputParam]
        public TransmitParam InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private TransmitParam _inputRegion = new TransmitParam();
        /// <summary>
        /// 输入区域参数
        /// </summary>
        [InputParam]
        public TransmitParam InputRegion
        {
            get { return _inputRegion; }
            set
            {
                _inputRegion = value ?? new TransmitParam();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        /// <summary>区域列表</summary>
        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        [JsonIgnore]
        private HRegion _outFilterRegion;
        [OutputParam("OutFilterRegion", "筛选区域")]
        [JsonIgnore]
        public HRegion OutFilterRegion
        {
            get { return _outFilterRegion; }
            set
            {
                var old = _outFilterRegion;
                SetProperty(ref _outFilterRegion, value);
                if (old != null && !ReferenceEquals(old, _outFilterRegion))
                    HalconImageOwnership.DisposeOwned(old);
            }
        }

        [JsonIgnore]
        private double[] _outFeatureValues = [];
        [OutputParam("OutFeatureValues", "输出特征值")]
        [JsonIgnore]
        public double[] OutFeatureValues
        {
            get { return _outFeatureValues; }
            set { SetProperty(ref _outFeatureValues, value); }
        }

        [JsonIgnore]
        private HObject _initRegion;
        /// <summary>
        /// 输入区域的自有副本
        /// </summary>
        [JsonIgnore]
        public HObject InitRegion
        {
            get { return _initRegion; }
            set { SetProperty(ref _initRegion, value); }
        }

        /// <summary>
        /// 显示的ROI
        /// </summary>
        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        [JsonIgnore]
        private FilterType _filterType = FilterType.and;
        public FilterType FilterType
        {
            get { return _filterType; }
            set { SetProperty(ref _filterType, value); }
        }

        private ObservableCollection<DoublePropertyDefinition> _propertyDefinitions;
        public ObservableCollection<DoublePropertyDefinition> PropertyDefinitions
        {
            get { return _propertyDefinitions; }
            set { SetProperty(ref _propertyDefinitions, value); }
        }

        // 运行时拥有的输入图像副本
        [JsonIgnore]
        private HImage _ownedInputImage;

        [JsonIgnore]
        private HObject _previewImageObject;
        /// <summary>预览图像对象</summary>
        [JsonIgnore]
        public HObject PreviewImageObject
        {
            get => _previewImageObject;
            private set { SetProperty(ref _previewImageObject, value); }
        }

        /// <summary>预览覆盖层绘制对象集合</summary>
        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> PreviewDrawObjects { get; } = new();

        #endregion

        #region Constructor
        public FilterRegionModel()
        {
        }
        #endregion

        #region 生命周期
        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit) return true;
                if (!base.OnceInit()) return false;
                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch { return false; }
        }

        public override bool LoadKeyParam()
        {
            try
            {
                if (!base.LoadKeyParam()) return false;

                _inputImage.Value = GetTransmitParam(InputParams, _inputImage);
                ReplaceOwnedInputImage(_inputImage?.Value);

                _inputRegion.Value = GetTransmitParam(InputParams, _inputRegion);
                ReplaceOwnedInputRegion(_inputRegion?.Value);

                RefreshPreviewDisplay();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载筛选区域参数异常：{ex.Message}");
                return false;
            }
        }

        public override void Dispose()
        {
            DisposeOwnedRuntimeObjects();
            ClearMHRoi();
            base.Dispose();
        }
        #endregion

        #region Methods

        public void InitializePropertyDefinitions()
        {
            PropertyDefinitions = new ObservableCollection<DoublePropertyDefinition>();

            PropertyDefinitions.Add(new DoublePropertyDefinition("面积", "area", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("中心行坐标", "row", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("中心列坐标", "column", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("区域宽", "width", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("区域高", "height", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("区域宽高比", "ratio", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("左上角点行坐标", "row1", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("左上角点列坐标", "column1", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("右下角点行坐标", "row2", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("右下角点列坐标", "column2", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("圆度", "circularity", 0, 1, 0.1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("紧凑", "compactness", 0, 1, 0.1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("周长", "contlength", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("凸性", "convexity", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("矩形度", "rectangularity", 0, 1, 0.1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("等效椭圆长半径", "ra", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("等效椭圆短半径", "rb", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("等效椭圆方向", "phi", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("偏心率", "anisometry", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("膨松度", "bulkiness", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("结构因子", "struct_factor", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("外切圆半径", "outer_radius", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("内接圆半径", "inner_radius", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("内接矩形高度", "inner_height", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("内接矩形宽度", "inner_width", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("边界与中心平均距离", "dist_mean", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("边界与中心距离偏差", "dist_deviation", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("多边形边数", "num_sides", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("联通数", "connect_num", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("区域内洞数", "holes_num", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("所有洞面积", "area_holes", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("最大直径", "max_diameter", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("区域方向", "orientation", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("欧拉数", "euler_number", 0, 100, 1));
            PropertyDefinitions.Add(new DoublePropertyDefinition("外接矩形方向", "rect2_phi", 0, 100, 1));
        }

        /// <summary>
        /// 模块执行
        /// </summary>
        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam())
                        return NodeStatus.Error;

                    ClearMHRoi();

                    if (_ownedInputImage == null || !_ownedInputImage.IsInitialized())
                        return NodeStatus.None;

                    if (InitRegion == null || !InitRegion.IsInitialized())
                        return NodeStatus.None;

                    bool status = true;
                    HOperatorSet.GenEmptyObj(out HObject TmpOutRegion);
                    double[] TmpOutFeatureValues = [];
                    status = FilterRegions(InitRegion, PropertyDefinitions, FilterType, out TmpOutRegion, out TmpOutFeatureValues);

                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "green", InitRegion.Clone(), true, true));
                    ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "red", TmpOutRegion.Clone(), true, true));

                    if (status)
                    {
                        OutFilterRegion = new HRegion(HalconImageOwnership.CopyOwnedObjectAndDisposeOrNull(TmpOutRegion));
                        OutFeatureValues = TmpOutFeatureValues;
                    }
                    else
                    {
                        HalconImageOwnership.DisposeOwned(TmpOutRegion);
                    }

                    RefreshPreviewDisplay();
                    RefreshOutputParams();
                    return status ? NodeStatus.Success : NodeStatus.Error;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"筛选区域模块执行异常：{ex.Message}");
                    return NodeStatus.Error;
                }
            });

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：筛选区域模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        public void InitImg()
        {
            ShowHRoi();
        }

        public void InitRgn()
        {
            ShowHRoi();
            InitInputRegionMethod();
        }

        public void InitInputRegionMethod()
        {
            if (InitRegion != null && InitRegion.IsInitialized())
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.输入区域, "green", InitRegion.Clone(), true, true));
            ShowHRoi();
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

        public static bool FilterRegions(
            HObject region,
            ObservableCollection<DoublePropertyDefinition> features,
            FilterType filterType,
            out HObject result,
            out double[] featureValues
            )
        {
            try
            {
                HObject connectedRegions;
                HOperatorSet.GenEmptyObj(out result);

                // 先连通域分解
                HOperatorSet.Connection(region, out connectedRegions);

                // 没有选中的特征，直接返回
                var selectedFeatures = features.Where(f => f.IsSelected).ToList();
                var outputFeatures = features.Where(f => f.IsOut).ToList();

                featureValues = [];
                if (selectedFeatures.Count == 0)
                {
                    HOperatorSet.CopyObj(connectedRegions, out result, 1, -1);
                    connectedRegions.Dispose();
                    return true;
                }

                if (filterType == FilterType.and)
                {
                    // AND 模式：逐步收窄
                    HOperatorSet.CopyObj(connectedRegions, out result, 1, -1);

                    foreach (var f in selectedFeatures)
                    {
                        HObject tmp;
                        HOperatorSet.SelectShape(result, out tmp, f.HName, "and", f.MinValue, f.MaxValue);
                        result.Dispose();
                        result = tmp;
                    }
                }
                else if (filterType == FilterType.or)
                {
                    // OR 模式：结果并集
                    foreach (var f in selectedFeatures)
                    {
                        HObject tmp;
                        HOperatorSet.SelectShape(connectedRegions, out tmp, f.HName, "and", f.MinValue, f.MaxValue);

                        HObject concat;
                        HOperatorSet.ConcatObj(result, tmp, out concat);

                        result.Dispose();
                        tmp.Dispose();
                        result = concat;
                    }
                }

                // 结果区域存在时提取特征值
                if (outputFeatures.Count > 0)
                {
                    HOperatorSet.Union1(result, out HObject unionResult);

                    List<double> values = new List<double>();
                    foreach (var f in outputFeatures)
                    {
                        HOperatorSet.RegionFeatures(unionResult, f.HName, out HTuple val);
                        values.Add(val.D);
                    }
                    featureValues = values.ToArray();

                    try { unionResult?.Dispose(); } catch { }
                }

                connectedRegions.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                featureValues = [];
                result = null;
                return false;
            }
        }

        #endregion

        #region 输出参数刷新
        private void RefreshOutputParams()
        {
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (item.Resourece == ResoureceType.Inupt)
                    continue;

                var key = !string.IsNullOrWhiteSpace(item.ParamName)
                    ? item.ParamName
                    : item.Name;

                if (!string.IsNullOrWhiteSpace(key) && values.TryGetValue(key, out var value))
                {
                    // 克隆 HALCON 对象，避免暴露内部缓存给下游
                    if (value is HObject hObj && hObj.IsInitialized())
                        item.Value = hObj.Clone();
                    else
                        item.Value = value;
                }
            }

            if (!UpdateParam())
                Console.WriteLine($"筛选区域模块_{Serial}更新参数失败");
        }
        #endregion

        #region 预览显示
        /// <summary>替换拥有的输入图像副本，兼容 HImage 和 HObject 输入</summary>
        private void ReplaceOwnedInputImage(object imageValue)
        {
            var oldOwned = _ownedInputImage;
            try
            {
                switch (imageValue)
                {
                    case HImage hImage when hImage.IsInitialized():
                        _ownedInputImage = hImage.CopyImage();
                        break;
                    case HObject hObj when hObj.IsInitialized():
                        using (var tempImage = new HImage(hObj))
                        {
                            _ownedInputImage = tempImage.CopyImage();
                        }
                        break;
                    default:
                        _ownedInputImage = null;
                        break;
                }
            }
            catch
            {
                _ownedInputImage = null;
            }
            if (oldOwned != null && !ReferenceEquals(oldOwned, _ownedInputImage))
            {
                try { oldOwned.Dispose(); } catch { }
            }
        }

        /// <summary>替换拥有的输入区域副本</summary>
        private void ReplaceOwnedInputRegion(object regionValue)
        {
            var oldRegion = _initRegion;
            try
            {
                switch (regionValue)
                {
                    case HRegion hRegion when hRegion.IsInitialized():
                        InitRegion = hRegion.Clone();
                        break;
                    case HObject hObj when hObj.IsInitialized():
                        InitRegion = hObj.Clone();
                        break;
                    default:
                        InitRegion = null;
                        break;
                }
            }
            catch
            {
                InitRegion = null;
            }
            if (oldRegion != null && !ReferenceEquals(oldRegion, _initRegion))
            {
                try { oldRegion.Dispose(); } catch { }
            }
        }

        /// <summary>清理 mHRoi 列表中的 HALCON 句柄</summary>
        private void ClearMHRoi()
        {
            foreach (var roi in mHRoi)
            {
                try { roi.hobject?.Dispose(); } catch { }
            }
            mHRoi.Clear();
        }

        /// <summary>刷新预览图像和覆盖层</summary>
        private void RefreshPreviewDisplay()
        {
            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(RefreshPreviewDisplay));
                return;
            }

            // 更新预览图像
            var oldPreview = _previewImageObject;
            if (_ownedInputImage != null && _ownedInputImage.IsInitialized())
                PreviewImageObject = _ownedInputImage.Clone();
            else
                PreviewImageObject = null;
            if (oldPreview != null && !ReferenceEquals(oldPreview, _previewImageObject))
            {
                try { oldPreview.Dispose(); } catch { }
            }

            // 更新覆盖层
            ClearPreviewDrawObjects();
            // 显示输入区域
            if (InitRegion != null && InitRegion.IsInitialized())
            {
                try
                {
                    PreviewDrawObjects.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = InitRegion.Clone(),
                        Color = "green",
                        IsFillDisplay = true
                    });
                }
                catch { }
            }
            // 显示筛选结果区域
            if (OutFilterRegion != null && OutFilterRegion.IsInitialized())
            {
                try
                {
                    PreviewDrawObjects.Add(new HalconDrawingObject
                    {
                        ShapeType = HalconShapeType.Region,
                        Hobject = OutFilterRegion.Clone(),
                        Color = "red",
                        IsFillDisplay = true
                    });
                }
                catch { }
            }
        }

        /// <summary>清除预览覆盖层对象</summary>
        private void ClearPreviewDrawObjects()
        {
            foreach (var obj in PreviewDrawObjects)
            {
                try { obj.Hobject?.Dispose(); } catch { }
            }
            PreviewDrawObjects.Clear();
        }

        /// <summary>释放运行时拥有的 HALCON 对象</summary>
        private void DisposeOwnedRuntimeObjects()
        {
            ClearPreviewDrawObjects();
            if (_previewImageObject != null)
            {
                try { _previewImageObject.Dispose(); } catch { }
                _previewImageObject = null;
            }
            if (_ownedInputImage != null)
            {
                try { _ownedInputImage.Dispose(); } catch { }
                _ownedInputImage = null;
            }
            if (_initRegion != null)
            {
                try { _initRegion.Dispose(); } catch { }
                _initRegion = null;
            }
            if (_outFilterRegion != null)
            {
                try { _outFilterRegion.Dispose(); } catch { }
                _outFilterRegion = null;
            }
        }
        #endregion
    }
}

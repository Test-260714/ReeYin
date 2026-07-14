using HalconDotNet;
using Newtonsoft.Json;
using ReeYin.Customized.Algo.Algorithms;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Share;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ReeYin.Customized.Algo.Models
{
    public enum HeightDifferenceMeasureKind
    {
        Automatic,
        LineProfile,
        Rectangle1,
        Rectangle2
    }

    public sealed class HeightDifferenceMeasureItem : BindableBase
    {
        // 结果列表中高度差的格式化显示文本。
        private string _heightDiffText = "--";
        // 结果列表中第一段平均高度的格式化显示文本。
        private string _segment1MeanText = "--";
        // 结果列表中第二段平均高度的格式化显示文本。
        private string _segment2MeanText = "--";
        // 结果列表中画线长度的格式化显示文本。
        private string _profileLengthText = "--";
        // 当前测量项的执行状态说明。
        private string _executionStatus = string.Empty;
        // 当前测量项在结果列表中的序号。
        private int _index;

        /// <summary>
        /// 当前测量结果的来源类型，用于区分自动、画线、矩形1和矩形2结果。
        /// </summary>
        public HeightDifferenceMeasureKind MeasureKind { get; set; } = HeightDifferenceMeasureKind.Automatic;

        /// <summary>
        /// 当前测量结果类型的中文显示名称。
        /// </summary>
        public string MeasureKindText => MeasureKind switch
        {
            HeightDifferenceMeasureKind.LineProfile => "画线测量",
            HeightDifferenceMeasureKind.Rectangle1 => "矩形1测量",
            HeightDifferenceMeasureKind.Rectangle2 => "矩形2测量",
            _ => "自动测量"
        };

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        /// <summary>
        /// 画线测量起点的图像列坐标（像素）。
        /// </summary>
        public double StartPixelX { get; set; }

        /// <summary>
        /// 画线测量起点的图像行坐标（像素）。
        /// </summary>
        public double StartPixelY { get; set; }

        /// <summary>
        /// 画线测量终点的图像列坐标（像素）。
        /// </summary>
        public double EndPixelX { get; set; }

        /// <summary>
        /// 画线测量终点的图像行坐标（像素）。
        /// </summary>
        public double EndPixelY { get; set; }

        /// <summary>
        /// 指示该结果是否包含可回显的画线测量主线。
        /// </summary>
        public bool HasSelectionLine { get; set; }

        /// <summary>
        /// 指示该结果是否包含两块矩形测量区域。
        /// </summary>
        public bool HasRectanglePair { get; set; }

        /// <summary>
        /// 指示高度曲线中是否已保存第一段取样区间。
        /// </summary>
        public bool HasProfileSegment1 { get; set; }

        /// <summary>
        /// 指示高度曲线中是否已保存第二段取样区间。
        /// </summary>
        public bool HasProfileSegment2 { get; set; }

        /// <summary>
        /// 第一段高度区间起点对应的图像列坐标（像素）。
        /// </summary>
        public double Segment1StartPixelX { get; set; }

        /// <summary>
        /// 第一段高度区间起点对应的图像行坐标（像素）。
        /// </summary>
        public double Segment1StartPixelY { get; set; }

        /// <summary>
        /// 第一段高度区间终点对应的图像列坐标（像素）。
        /// </summary>
        public double Segment1EndPixelX { get; set; }

        /// <summary>
        /// 第一段高度区间终点对应的图像行坐标（像素）。
        /// </summary>
        public double Segment1EndPixelY { get; set; }

        /// <summary>
        /// 第二段高度区间起点对应的图像列坐标（像素）。
        /// </summary>
        public double Segment2StartPixelX { get; set; }

        /// <summary>
        /// 第二段高度区间起点对应的图像行坐标（像素）。
        /// </summary>
        public double Segment2StartPixelY { get; set; }

        /// <summary>
        /// 第二段高度区间终点对应的图像列坐标（像素）。
        /// </summary>
        public double Segment2EndPixelX { get; set; }

        /// <summary>
        /// 第二段高度区间终点对应的图像行坐标（像素）。
        /// </summary>
        public double Segment2EndPixelY { get; set; }

        /// <summary>
        /// 指示矩形测量结果是否采用可旋转的矩形2区域。
        /// </summary>
        public bool IsRectangle2Pair { get; set; }

        /// <summary>
        /// 矩形1第一块区域拖拽起点的图像列坐标（像素）。
        /// </summary>
        public double Rect1StartPixelX { get; set; }

        /// <summary>
        /// 矩形1第一块区域拖拽起点的图像行坐标（像素）。
        /// </summary>
        public double Rect1StartPixelY { get; set; }

        /// <summary>
        /// 矩形1第一块区域拖拽终点的图像列坐标（像素）。
        /// </summary>
        public double Rect1EndPixelX { get; set; }

        /// <summary>
        /// 矩形1第一块区域拖拽终点的图像行坐标（像素）。
        /// </summary>
        public double Rect1EndPixelY { get; set; }

        /// <summary>
        /// 矩形1第二块区域拖拽起点的图像列坐标（像素）。
        /// </summary>
        public double Rect2StartPixelX { get; set; }

        /// <summary>
        /// 矩形1第二块区域拖拽起点的图像行坐标（像素）。
        /// </summary>
        public double Rect2StartPixelY { get; set; }

        /// <summary>
        /// 矩形1第二块区域拖拽终点的图像列坐标（像素）。
        /// </summary>
        public double Rect2EndPixelX { get; set; }

        /// <summary>
        /// 矩形1第二块区域拖拽终点的图像行坐标（像素）。
        /// </summary>
        public double Rect2EndPixelY { get; set; }

        /// <summary>
        /// 矩形2第一块旋转区域中心的图像列坐标（像素）。
        /// </summary>
        public double Rect1CenterPixelX { get; set; }

        /// <summary>
        /// 矩形2第一块旋转区域中心的图像行坐标（像素）。
        /// </summary>
        public double Rect1CenterPixelY { get; set; }

        /// <summary>
        /// 矩形2第一块旋转区域的角度（弧度）。
        /// </summary>
        public double Rect1Phi { get; set; }

        /// <summary>
        /// 矩形2第一块旋转区域的长轴半长（像素）。
        /// </summary>
        public double Rect1Length1 { get; set; }

        /// <summary>
        /// 矩形2第一块旋转区域的短轴半长（像素）。
        /// </summary>
        public double Rect1Length2 { get; set; }

        /// <summary>
        /// 矩形2第二块旋转区域中心的图像列坐标（像素）。
        /// </summary>
        public double Rect2CenterPixelX { get; set; }

        /// <summary>
        /// 矩形2第二块旋转区域中心的图像行坐标（像素）。
        /// </summary>
        public double Rect2CenterPixelY { get; set; }

        /// <summary>
        /// 矩形2第二块旋转区域的角度（弧度）。
        /// </summary>
        public double Rect2Phi { get; set; }

        /// <summary>
        /// 矩形2第二块旋转区域的长轴半长（像素）。
        /// </summary>
        public double Rect2Length1 { get; set; }

        /// <summary>
        /// 矩形2第二块旋转区域的短轴半长（像素）。
        /// </summary>
        public double Rect2Length2 { get; set; }

        /// <summary>
        /// 两段曲线或两块区域平均高度的差值，按当前显示精度格式化输出。
        /// </summary>
        public double HeightDiff { get; set; }

        /// <summary>
        /// 第一段曲线或第一块区域的平均高度。
        /// </summary>
        public double Segment1Mean { get; set; }

        /// <summary>
        /// 第二段曲线或第二块区域的平均高度。
        /// </summary>
        public double Segment2Mean { get; set; }

        /// <summary>
        /// 画线测量主线的物理长度，单位为毫米。
        /// </summary>
        public double ProfileLength { get; set; }

        /// <summary>
        /// 当前测量参与统计的总采样点数量。
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// 当前测量排除无效高度后的有效采样点数量。
        /// </summary>
        public int ValidSampleCount { get; set; }

        public string HeightDiffText
        {
            get => _heightDiffText;
            private set => SetProperty(ref _heightDiffText, value);
        }

        public string Segment1MeanText
        {
            get => _segment1MeanText;
            private set => SetProperty(ref _segment1MeanText, value);
        }

        public string Segment2MeanText
        {
            get => _segment2MeanText;
            private set => SetProperty(ref _segment2MeanText, value);
        }

        public string ProfileLengthText
        {
            get => _profileLengthText;
            private set => SetProperty(ref _profileLengthText, value);
        }

        /// <summary>
        /// 有效采样点和总采样点的比例文本。
        /// </summary>
        public string SampleSummary => $"{ValidSampleCount}/{SampleCount}";

        public string ExecutionStatus
        {
            get => _executionStatus;
            set => SetProperty(ref _executionStatus, value);
        }

        /// <summary>
        /// 按当前精度刷新测量项在结果列表中的显示文本。
        /// </summary>
        public void RefreshDisplayText(Func<double, string> formatMeasurement, Func<double, string> formatPlainNumber)
        {
            HeightDiffText = double.IsNaN(HeightDiff) ? "--" : formatMeasurement(Math.Abs(HeightDiff));
            Segment1MeanText = double.IsNaN(Segment1Mean) ? "--" : formatMeasurement(Segment1Mean);
            Segment2MeanText = double.IsNaN(Segment2Mean) ? "--" : formatMeasurement(Segment2Mean);
            ProfileLengthText = double.IsNaN(ProfileLength) ? "--" : $"{formatPlainNumber(ProfileLength)} mm";
        }
    }

    public class HeightDifferenceMeasureModel : ModelParamBase
    {
        /// <summary>
        /// 生成热力图预览时保留的边距像素。
        /// </summary>
        private const int HeatmapPreviewMargin = 20;
        /// <summary>
        /// 热力图预览右侧色带和文字所需的额外宽度。
        /// </summary>
        private const int HeatmapPreviewExtraWidth = 170;
        /// <summary>
        /// 热力图预览底部标注所需的额外高度。
        /// </summary>
        private const int HeatmapPreviewExtraHeight = 40;

        // 用户手动选择的高度图或 PCD 文件路径。
        private string _manualImagePath = string.Empty;
        public string ManualImagePath
        {
            get => _manualImagePath;
            set => SetProperty(ref _manualImagePath, value);
        }

        // 配方保存的 X 方向像素间距（mm），用于图像列坐标到物理长度换算。
        private double _intervalX = 0.000117;
        [RecipeParam("IntervalX", "X方向像素间距 (mm)")]
        public double IntervalX
        {
            get => _intervalX;
            set => SetProperty(ref _intervalX, value);
        }

        // 配方保存的 Y 方向像素间距（mm），用于图像行坐标到物理长度换算。
        private double _intervalY = 0.000117;
        [RecipeParam("IntervalY", "Y方向像素间距 (mm)")]
        public double IntervalY
        {
            get => _intervalY;
            set => SetProperty(ref _intervalY, value);
        }

        // 配方保存的 Z 向数值系数，用于表示一个原始 Z 值对应多少个指定单位。
        private double _zValueScale = 1.0;
        [RecipeParam("ZValueScale", "Z向数值系数")]
        public double ZValueScale
        {
            get => _zValueScale;
            set
            {
                if (SetProperty(ref _zValueScale, NormalizeZValueScale(value)))
                {
                    RaisePropertyChanged(nameof(ZValueToMillimeterFactor));
                }
            }
        }

        // 配方保存的 Z 向数值单位，用于把读取到的高度值统一换算为毫米。
        private HeightDifferenceZValueUnit _zValueUnit = HeightDifferenceZValueUnit.mm;
        [RecipeParam("ZValueUnit", "Z向数值单位")]
        public HeightDifferenceZValueUnit ZValueUnit
        {
            get => _zValueUnit;
            set
            {
                HeightDifferenceZValueUnit normalized = Enum.IsDefined(value)
                    ? value
                    : HeightDifferenceZValueUnit.mm;
                if (SetProperty(ref _zValueUnit, normalized))
                {
                    RaisePropertyChanged(nameof(ZValueToMillimeterFactor));
                }
            }
        }

        [JsonIgnore]
        public double ZValueToMillimeterFactor => ZValueUnit.ToMillimeterFactor(ZValueScale);

        private static double NormalizeZValueScale(double value)
        {
            return double.IsFinite(value) && value > 0
                ? value
                : 1.0;
        }

        // 无效高度灰度的中心值，统计均值时会按该值过滤无数据区域。
        private double _invalidGrayCenter = 888888.0;
        [RecipeParam("InvalidGrayCenter", "无效灰度中心值")]
        public double InvalidGrayCenter
        {
            get => _invalidGrayCenter;
            set => SetProperty(ref _invalidGrayCenter, value);
        }

        // 无效高度灰度的容差范围，配合中心值判断哪些采样点需要剔除。
        private double _invalidGrayTolerance = 1.0;
        [RecipeParam("InvalidGrayTolerance", "无效灰度容差")]
        public double InvalidGrayTolerance
        {
            get => _invalidGrayTolerance;
            set => SetProperty(ref _invalidGrayTolerance, value);
        }

        // 自动测量区域在高度方向的内缩比例，用于避开边缘异常高度。
        private double _shrinkHeightRatio = 0.2;
        [RecipeParam("ShrinkHeightRatio", "测量框高度保留比例")]
        public double ShrinkHeightRatio
        {
            get => _shrinkHeightRatio;
            set => SetProperty(ref _shrinkHeightRatio, value);
        }

        // 自动测量区域在宽度方向的内缩比例，用于聚焦左右主体区域。
        private double _shrinkWidthRatio = 0.6;
        [RecipeParam("ShrinkWidthRatio", "测量框宽度保留比例")]
        public double ShrinkWidthRatio
        {
            get => _shrinkWidthRatio;
            set => SetProperty(ref _shrinkWidthRatio, value);
        }

        // 左侧自动测量区域向内平移的宽度比例，用于避开外侧边缘。
        private double _leftRegionMoveRatio = 0.05;
        [RecipeParam("LeftRegionMoveRatio", "左区域移动比例")]
        public double LeftRegionMoveRatio
        {
            get => _leftRegionMoveRatio;
            set => SetProperty(ref _leftRegionMoveRatio, value);
        }

        // 右侧自动测量区域向内平移的宽度比例，用于避开外侧边缘。
        private double _rightRegionMoveRatio = 0.05;
        [RecipeParam("RightRegionMoveRatio", "右区域移动比例")]
        public double RightRegionMoveRatio
        {
            get => _rightRegionMoveRatio;
            set => SetProperty(ref _rightRegionMoveRatio, value);
        }

        // 计算区域均值时裁掉两端异常高度的比例。
        private double _trimRatio = 0.05;
        [RecipeParam("TrimRatio", "截尾均值比例")]
        public double TrimRatio
        {
            get => _trimRatio;
            set => SetProperty(ref _trimRatio, value);
        }

        // 高度差、区域均值和曲线长度显示时保留的小数位数。
        private int _measurementPrecision = 3;
        [RecipeParam("MeasurementPrecision", "测量精度(小数位数)")]
        public int MeasurementPrecision
        {
            get => _measurementPrecision;
            set
            {
                int normalized = Math.Clamp(value, 0, 6);
                if (SetProperty(ref _measurementPrecision, normalized))
                {
                    RefreshFormattedResults();
                }
            }
        }

        [JsonIgnore]
        private string _resultMessage = "等待执行";
        [JsonIgnore]
        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        [JsonIgnore]
        private string _inputSourceText = "未选择高度图或 PCD 文件";
        [JsonIgnore]
        public string InputSourceText
        {
            get => _inputSourceText;
            set => SetProperty(ref _inputSourceText, value);
        }

        [JsonIgnore]
        private BitmapSource? _heatmapPreviewImage;
        [JsonIgnore]
        public BitmapSource? HeatmapPreviewImage
        {
            get => _heatmapPreviewImage;
            set => SetProperty(ref _heatmapPreviewImage, value);
        }

        [JsonIgnore]
        private HObject? _heatmapPreviewObject;
        [JsonIgnore]
        public HObject? HeatmapPreviewObject
        {
            get => _heatmapPreviewObject;
            private set => SetProperty(ref _heatmapPreviewObject, value);
        }

        [JsonIgnore]
        public ObservableCollection<HalconDrawingObject> HeatmapPreviewDrawObjects { get; } = [];

        [JsonIgnore]
        private BitmapSource? _heatmapColorBarImage;
        [JsonIgnore]
        public BitmapSource? HeatmapColorBarImage
        {
            get => _heatmapColorBarImage;
            set => SetProperty(ref _heatmapColorBarImage, value);
        }

        [JsonIgnore]
        public bool IsReplacingHeatmapPreviewWithoutLayoutReset { get; private set; }

        [JsonIgnore]
        private float[]? _depthRawValues;
        [JsonIgnore]
        public float[]? DepthRawValues
        {
            get => _depthRawValues;
            set => SetProperty(ref _depthRawValues, value);
        }

        [JsonIgnore]
        private int _depthImageWidth;
        [JsonIgnore]
        public int DepthImageWidth
        {
            get => _depthImageWidth;
            set => SetProperty(ref _depthImageWidth, value);
        }

        [JsonIgnore]
        private int _depthImageHeight;
        [JsonIgnore]
        public int DepthImageHeight
        {
            get => _depthImageHeight;
            set => SetProperty(ref _depthImageHeight, value);
        }

        [JsonIgnore]
        private string _depthPixelType = string.Empty;
        [JsonIgnore]
        public string DepthPixelType
        {
            get => _depthPixelType;
            set => SetProperty(ref _depthPixelType, value);
        }

        [JsonIgnore]
        private string _profileStatusText = "请先生成热力图，再在图上按住鼠标左键拖动提取高度曲线。";
        [JsonIgnore]
        public string ProfileStatusText
        {
            get => _profileStatusText;
            set => SetProperty(ref _profileStatusText, value);
        }

        [JsonIgnore]
        private double _profileHeightDiff = double.NaN;
        [JsonIgnore]
        public double ProfileHeightDiff
        {
            get => _profileHeightDiff;
            set => SetProperty(ref _profileHeightDiff, value);
        }

        [JsonIgnore]
        private double _profileSegment1Mean = double.NaN;
        [JsonIgnore]
        public double ProfileSegment1Mean
        {
            get => _profileSegment1Mean;
            set => SetProperty(ref _profileSegment1Mean, value);
        }

        [JsonIgnore]
        private double _profileSegment2Mean = double.NaN;
        [JsonIgnore]
        public double ProfileSegment2Mean
        {
            get => _profileSegment2Mean;
            set => SetProperty(ref _profileSegment2Mean, value);
        }

        [JsonIgnore]
        private double _profileLength = double.NaN;
        [JsonIgnore]
        public double ProfileLength
        {
            get => _profileLength;
            set => SetProperty(ref _profileLength, value);
        }

        [JsonIgnore]
        private int _profileSampleCount;
        [JsonIgnore]
        public int ProfileSampleCount
        {
            get => _profileSampleCount;
            set => SetProperty(ref _profileSampleCount, value);
        }

        [JsonIgnore]
        private int _profileValidSampleCount;
        [JsonIgnore]
        public int ProfileValidSampleCount
        {
            get => _profileValidSampleCount;
            set => SetProperty(ref _profileValidSampleCount, value);
        }

        [JsonIgnore]
        public ObservableCollection<HeightDifferenceMeasureItem> MeasureItems { get; } = [];

        [JsonIgnore]
        private double _zoom = 1.0;
        [JsonIgnore]
        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        [JsonIgnore]
        private double _heightDiff = double.NaN;
        [JsonIgnore]
        [OutputParam("HeightDiff", "高度差")]
        public double HeightDiff
        {
            get => _heightDiff;
            set => SetProperty(ref _heightDiff, value);
        }

        [JsonIgnore]
        private double _areaHeightDiff = double.NaN;
        [JsonIgnore]
        public double AreaHeightDiff
        {
            get => _areaHeightDiff;
            set => SetProperty(ref _areaHeightDiff, value);
        }

        [JsonIgnore]
        private string _areaHeightDiffDisplayText = "--";
        [JsonIgnore]
        public string AreaHeightDiffDisplayText
        {
            get => _areaHeightDiffDisplayText;
            set => SetProperty(ref _areaHeightDiffDisplayText, value);
        }

        [JsonIgnore]
        private double _heatmapRangeMin = double.NaN;
        [JsonIgnore]
        [OutputParam("HeatmapRangeMin", "热力图区间下限")]
        public double HeatmapRangeMin
        {
            get => _heatmapRangeMin;
            set => SetProperty(ref _heatmapRangeMin, value);
        }

        [JsonIgnore]
        private double _heatmapRangeMax = double.NaN;
        [JsonIgnore]
        [OutputParam("HeatmapRangeMax", "热力图区间上限")]
        public double HeatmapRangeMax
        {
            get => _heatmapRangeMax;
            set => SetProperty(ref _heatmapRangeMax, value);
        }

        [JsonIgnore]
        private double _heightRangeMin = double.NaN;
        [JsonIgnore]
        [OutputParam("HeightRangeMin", "高度区间下限")]
        public double HeightRangeMin
        {
            get => _heightRangeMin;
            set => SetProperty(ref _heightRangeMin, value);
        }

        [JsonIgnore]
        private double _heightRangeMax = double.NaN;
        [JsonIgnore]
        [OutputParam("HeightRangeMax", "高度区间上限")]
        public double HeightRangeMax
        {
            get => _heightRangeMax;
            set => SetProperty(ref _heightRangeMax, value);
        }

        [JsonIgnore]
        private string _usedImagePath = string.Empty;
        [JsonIgnore]
        [OutputParam("UsedImagePath", "实际使用的图像路径")]
        public string UsedImagePath
        {
            get => _usedImagePath;
            set => SetProperty(ref _usedImagePath, value);
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        /// <summary>
        /// 初始化模块输出、运行时集合和默认显示状态。
        /// </summary>
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
                    TriggerModuleRun = () => ExecuteModule().Result;
                }

                IsOnceInit = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 确保热力图和手动测量可用的深度缓存已加载。
        /// </summary>
        public bool TryEnsureDepthDataForVisualization(out string message)
        {
            message = string.Empty;

            if (DepthRawValues != null
                && DepthImageWidth > 0
                && DepthImageHeight > 0
                && DepthRawValues.Length >= DepthImageWidth * DepthImageHeight)
            {
                return true;
            }

            try
            {
                using HObject inputImage = ResolveInputImage(out _, out _);
                CacheDepthImageData(inputImage);
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 清空本次运行产生的自动测量、手动测量和预览缓存。
        /// </summary>
        public void ResetRuntimeState(string resultMessage = "等待执行")
        {
            ResetRuntimeResult();
            ResultMessage = resultMessage;
            Output = new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.NotRun,
                RunTime = 0
            };
        }

        /// <summary>
        /// 模块运行入口，按当前参数执行自动高度差测量。
        /// </summary>
        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            return ExecuteMeasurement(automaticMeasurement: true, loadKeyParameters: true);
        }

        /// <summary>
        /// 使用界面当前参数触发一次自动测量。
        /// </summary>
        public Task<ExecuteModuleOutput> ExecuteMeasurementWithCurrentParameters()
        {
            return ExecuteMeasurement(automaticMeasurement: true, loadKeyParameters: false);
        }

        /// <summary>
        /// 为画线或矩形手动测量准备热力图和深度数据。
        /// </summary>
        public Task<ExecuteModuleOutput> PrepareManualMeasurement()
        {
            return ExecuteMeasurement(automaticMeasurement: false, loadKeyParameters: false);
        }

        /// <summary>
        /// 按测量精度格式化带单位的高度值。
        /// </summary>
        public string FormatMeasurement(double value)
        {
            return $"{value.ToString($"F{MeasurementPrecision}")} mm";
        }

        /// <summary>
        /// 按测量精度格式化不带单位的数值。
        /// </summary>
        public string FormatPlainNumber(double value)
        {
            return value.ToString($"F{MeasurementPrecision}");
        }

        /// <summary>
        /// 刷新自动结果和结果列表中所有格式化文本。
        /// </summary>
        private void RefreshFormattedResults()
        {
            if (!double.IsNaN(AreaHeightDiff))
            {
                AreaHeightDiffDisplayText = FormatMeasurement(Math.Abs(AreaHeightDiff));
            }

            foreach (HeightDifferenceMeasureItem item in MeasureItems)
            {
                item.RefreshDisplayText(FormatMeasurement, FormatPlainNumber);
            }
        }

        /// <summary>
        /// 清空自动测量的高度差和热力图范围结果。
        /// </summary>
        public void ClearAutomaticMeasurementResult()
        {
            HeightDiff = double.NaN;
            AreaHeightDiff = double.NaN;
            AreaHeightDiffDisplayText = "--";
        }

        /// <summary>
        /// 删除全部测量结果并重置结果列表。
        /// </summary>
        public void ClearManualMeasureItems()
        {
            if (MeasureItems.Count > 0)
            {
                MeasureItems.Clear();
            }
        }

        /// <summary>
        /// 从结果列表删除指定测量项并重新编号。
        /// </summary>
        public bool DeleteMeasureItem(HeightDifferenceMeasureItem? item)
        {
            if (item == null || MeasureItems.Count == 0)
            {
                return false;
            }

            List<HeightDifferenceMeasureItem> remainingItems = [];
            bool removed = false;
            foreach (HeightDifferenceMeasureItem candidate in MeasureItems)
            {
                if (!removed && ReferenceEquals(candidate, item))
                {
                    removed = true;
                    continue;
                }

                remainingItems.Add(candidate);
            }

            if (!removed)
            {
                remainingItems.Clear();
                foreach (HeightDifferenceMeasureItem candidate in MeasureItems)
                {
                    if (!removed && candidate.Index == item.Index)
                    {
                        removed = true;
                        continue;
                    }

                    remainingItems.Add(candidate);
                }
            }

            if (!removed)
            {
                return false;
            }

            MeasureItems.Clear();
            for (int i = 0; i < remainingItems.Count; i++)
            {
                remainingItems[i].Index = i + 1;
                MeasureItems.Add(remainingItems[i]);
            }

            return true;
        }

        /// <summary>
        /// 把自动测量结果写入测量结果列表。
        /// </summary>
        public HeightDifferenceMeasureItem AddAutomaticMeasureItem(
            double heightDiff,
            HeightDifferenceMeasureRectangle? leftRectangle,
            HeightDifferenceMeasureRectangle? rightRectangle,
            string executionStatus)
        {
            HeightDifferenceMeasureItem item = new()
            {
                Index = MeasureItems.Count + 1,
                MeasureKind = HeightDifferenceMeasureKind.Automatic,
                HasSelectionLine = false,
                HasRectanglePair = leftRectangle != null && rightRectangle != null,
                IsRectangle2Pair = false,
                HeightDiff = heightDiff,
                Segment1Mean = double.NaN,
                Segment2Mean = double.NaN,
                ProfileLength = double.NaN,
                ExecutionStatus = executionStatus
            };

            if (leftRectangle != null && rightRectangle != null)
            {
                item.Rect1StartPixelX = leftRectangle.Col1;
                item.Rect1StartPixelY = leftRectangle.Row1;
                item.Rect1EndPixelX = leftRectangle.Col2;
                item.Rect1EndPixelY = leftRectangle.Row2;
                item.Rect2StartPixelX = rightRectangle.Col1;
                item.Rect2StartPixelY = rightRectangle.Row1;
                item.Rect2EndPixelX = rightRectangle.Col2;
                item.Rect2EndPixelY = rightRectangle.Row2;
            }

            item.RefreshDisplayText(FormatMeasurement, FormatPlainNumber);
            MeasureItems.Add(item);
            return item;
        }

        /// <summary>
        /// 保存画线测量的两段区间、均值和高度差。
        /// </summary>
        public HeightDifferenceMeasureItem AddManualMeasureItem(
            System.Windows.Point startPixel,
            System.Windows.Point endPixel,
            System.Windows.Point segment1StartPixel,
            System.Windows.Point segment1EndPixel,
            System.Windows.Point segment2StartPixel,
            System.Windows.Point segment2EndPixel,
            double segment1Mean,
            double segment2Mean,
            double heightDiff,
            double profileLength,
            int sampleCount,
            int validSampleCount)
        {
            HeightDifferenceMeasureItem item = new()
            {
                Index = MeasureItems.Count + 1,
                MeasureKind = HeightDifferenceMeasureKind.LineProfile,
                StartPixelX = startPixel.X,
                StartPixelY = startPixel.Y,
                EndPixelX = endPixel.X,
                EndPixelY = endPixel.Y,
                HasSelectionLine = true,
                HasRectanglePair = false,
                HasProfileSegment1 = true,
                HasProfileSegment2 = true,
                Segment1StartPixelX = segment1StartPixel.X,
                Segment1StartPixelY = segment1StartPixel.Y,
                Segment1EndPixelX = segment1EndPixel.X,
                Segment1EndPixelY = segment1EndPixel.Y,
                Segment2StartPixelX = segment2StartPixel.X,
                Segment2StartPixelY = segment2StartPixel.Y,
                Segment2EndPixelX = segment2EndPixel.X,
                Segment2EndPixelY = segment2EndPixel.Y,
                Segment1Mean = segment1Mean,
                Segment2Mean = segment2Mean,
                HeightDiff = heightDiff,
                ProfileLength = profileLength,
                SampleCount = sampleCount,
                ValidSampleCount = validSampleCount,
                ExecutionStatus = "手动测量完成"
            };

            item.RefreshDisplayText(FormatMeasurement, FormatPlainNumber);
            MeasureItems.Add(item);
            return item;
        }

        /// <summary>
        /// 保存矩形1测量的两块水平矩形及高度差。
        /// </summary>
        public HeightDifferenceMeasureItem AddRectangleMeasureItem(
            HeightDifferenceMeasureKind measureKind,
            System.Windows.Rect rect1,
            System.Windows.Rect rect2,
            double rect1Mean,
            double rect2Mean,
            double heightDiff,
            int sampleCount,
            int validSampleCount)
        {
            HeightDifferenceMeasureItem item = new()
            {
                Index = MeasureItems.Count + 1,
                MeasureKind = measureKind,
                HasSelectionLine = false,
                HasRectanglePair = true,
                IsRectangle2Pair = false,
                Rect1StartPixelX = rect1.Left,
                Rect1StartPixelY = rect1.Top,
                Rect1EndPixelX = rect1.Right,
                Rect1EndPixelY = rect1.Bottom,
                Rect2StartPixelX = rect2.Left,
                Rect2StartPixelY = rect2.Top,
                Rect2EndPixelX = rect2.Right,
                Rect2EndPixelY = rect2.Bottom,
                Segment1Mean = rect1Mean,
                Segment2Mean = rect2Mean,
                HeightDiff = heightDiff,
                ProfileLength = double.NaN,
                SampleCount = sampleCount,
                ValidSampleCount = validSampleCount,
                ExecutionStatus = measureKind == HeightDifferenceMeasureKind.Rectangle2
                    ? "矩形2测量完成"
                    : "矩形1测量完成"
            };

            AreaHeightDiff = Math.Abs(heightDiff);
            AreaHeightDiffDisplayText = FormatMeasurement(Math.Abs(heightDiff));
            item.RefreshDisplayText(FormatMeasurement, FormatPlainNumber);
            MeasureItems.Add(item);
            return item;
        }

        /// <summary>
        /// 保存矩形2测量的两块旋转矩形及高度差。
        /// </summary>
        public HeightDifferenceMeasureItem AddRectangle2MeasureItem(
            double rect1CenterX,
            double rect1CenterY,
            double rect1Phi,
            double rect1Length1,
            double rect1Length2,
            double rect2CenterX,
            double rect2CenterY,
            double rect2Phi,
            double rect2Length1,
            double rect2Length2,
            double rect1Mean,
            double rect2Mean,
            double heightDiff,
            int sampleCount,
            int validSampleCount)
        {
            HeightDifferenceMeasureItem item = new()
            {
                Index = MeasureItems.Count + 1,
                MeasureKind = HeightDifferenceMeasureKind.Rectangle2,
                HasSelectionLine = false,
                HasRectanglePair = true,
                IsRectangle2Pair = true,
                Rect1CenterPixelX = rect1CenterX,
                Rect1CenterPixelY = rect1CenterY,
                Rect1Phi = rect1Phi,
                Rect1Length1 = rect1Length1,
                Rect1Length2 = rect1Length2,
                Rect2CenterPixelX = rect2CenterX,
                Rect2CenterPixelY = rect2CenterY,
                Rect2Phi = rect2Phi,
                Rect2Length1 = rect2Length1,
                Rect2Length2 = rect2Length2,
                Segment1Mean = rect1Mean,
                Segment2Mean = rect2Mean,
                HeightDiff = heightDiff,
                ProfileLength = double.NaN,
                SampleCount = sampleCount,
                ValidSampleCount = validSampleCount,
                ExecutionStatus = "矩形2测量完成"
            };

            AreaHeightDiff = Math.Abs(heightDiff);
            AreaHeightDiffDisplayText = FormatMeasurement(Math.Abs(heightDiff));
            item.RefreshDisplayText(FormatMeasurement, FormatPlainNumber);
            MeasureItems.Add(item);
            return item;
        }

        /// <summary>
        /// 清空画线剖面曲线、区间统计和曲线汇总值。
        /// </summary>
        public void ClearProfileAnalysis()
        {
            ProfileHeightDiff = double.NaN;
            ProfileSegment1Mean = double.NaN;
            ProfileSegment2Mean = double.NaN;
            ProfileLength = double.NaN;
            ProfileSampleCount = 0;
            ProfileValidSampleCount = 0;
            ProfileStatusText = "请先生成热力图，再在图上按住鼠标左键拖动提取高度曲线。";
        }

        /// <summary>
        /// 把画线剖面统计结果同步到模型汇总字段。
        /// </summary>
        public void UpdateProfileSummary(
            double profileLength,
            int sampleCount,
            int validSampleCount,
            double? segment1Mean,
            double? segment2Mean,
            double? heightDiff,
            string statusText)
        {
            ProfileLength = profileLength;
            ProfileSampleCount = sampleCount;
            ProfileValidSampleCount = validSampleCount;
            ProfileSegment1Mean = segment1Mean ?? double.NaN;
            ProfileSegment2Mean = segment2Mean ?? double.NaN;
            ProfileHeightDiff = heightDiff ?? double.NaN;
            if (heightDiff.HasValue && !double.IsNaN(heightDiff.Value))
            {
                AreaHeightDiff = Math.Abs(heightDiff.Value);
                AreaHeightDiffDisplayText = FormatMeasurement(Math.Abs(heightDiff.Value));
            }
            ProfileStatusText = statusText;
        }

        /// <summary>
        /// 解析输入图像并执行自动测量算法。
        /// </summary>
        private Task<ExecuteModuleOutput> ExecuteMeasurement(bool automaticMeasurement, bool loadKeyParameters)
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (loadKeyParameters)
                    {
                        LoadKeyParam();
                    }

                    ResetRuntimeResult(clearVisualization: false);

                    using HObject inputImage = ResolveInputImage(out string sourceText, out string resolvedInputPath);
                    string resolvedHeatmapPath = ResolveHeatmapPreviewPath(resolvedInputPath);

                    HeightDifferenceMeasureAlgorithm algorithm = new();
                    HeightDifferenceMeasureRequest request = CreateMeasureRequest(inputImage, resolvedHeatmapPath);
                    HeightDifferenceMeasureResult measureResult = automaticMeasurement
                        ? algorithm.Measure(request)
                        : HeightDifferenceHeatmapRenderer.GenerateHeatmap(request);

                    ApplyExecutionResult(measureResult, inputImage, sourceText, resolvedInputPath, automaticMeasurement);
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    ResetRuntimeResult(clearVisualization: false);
                    ResultMessage = ex.Message;
                    InputSourceText = "执行失败";
                    return NodeStatus.Error;
                }
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time
            };

            return Task.FromResult(Output);
        }

        /// <summary>
        /// 把模型参数组装成自动测量算法请求。
        /// </summary>
        private HeightDifferenceMeasureRequest CreateMeasureRequest(HObject inputImage, string resolvedHeatmapPath)
        {
            return new HeightDifferenceMeasureRequest
            {
                InputImage = inputImage,
                HeatmapPreviewPath = resolvedHeatmapPath,
                IntervalX = IntervalX,
                IntervalY = IntervalY,
                IntervalZ = ZValueToMillimeterFactor,
                InvalidGrayCenter = InvalidGrayCenter,
                InvalidGrayTolerance = InvalidGrayTolerance,
                ShrinkHeightRatio = ShrinkHeightRatio,
                ShrinkWidthRatio = ShrinkWidthRatio,
                LeftRegionMoveRatio = LeftRegionMoveRatio,
                RightRegionMoveRatio = RightRegionMoveRatio,
                TrimRatio = TrimRatio
            };
        }

        /// <summary>
        /// 把算法输出的高度差、热力图范围和预览对象写回模型。
        /// </summary>
        private void ApplyExecutionResult(
            HeightDifferenceMeasureResult measureResult,
            HObject inputImage,
            string sourceText,
            string resolvedInputPath,
            bool automaticMeasurement)
        {
            HeightDiff = automaticMeasurement ? Math.Abs(measureResult.HeightDiff) : double.NaN;
            AreaHeightDiff = automaticMeasurement ? Math.Abs(measureResult.HeightDiff) : double.NaN;
            AreaHeightDiffDisplayText = automaticMeasurement && !double.IsNaN(measureResult.HeightDiff)
                ? FormatMeasurement(Math.Abs(measureResult.HeightDiff))
                : "--";
            HeatmapRangeMin = measureResult.HeatmapRangeMin;
            HeatmapRangeMax = measureResult.HeatmapRangeMax;
            HeightRangeMin = measureResult.HeightRangeMin;
            HeightRangeMax = measureResult.HeightRangeMax;
            UsedImagePath = resolvedInputPath;
            InputSourceText = string.IsNullOrWhiteSpace(resolvedInputPath)
                ? sourceText
                : $"{sourceText}: {resolvedInputPath}";

            CacheDepthImageData(inputImage);
            LoadHeatmapPreview(measureResult.HeatmapPreviewPath);

            if (automaticMeasurement)
            {
                AddAutomaticMeasureItem(
                    Math.Abs(measureResult.HeightDiff),
                    measureResult.LeftMeasureRectangle,
                    measureResult.RightMeasureRectangle,
                    "自动测量完成");
                ResultMessage = $"自动算法测量完成，高度差 = {FormatMeasurement(Math.Abs(measureResult.HeightDiff))}。";
                ProfileStatusText = "自动算法测量已完成。可继续在热力图上画线，手动提取曲线并计算两段高度差。";
                return;
            }

            ResultMessage = "手动测量模式已准备完成。当前仅生成热力图，不执行自动区域算法。";
            ProfileStatusText = "手动测量模式已就绪。请在热力图上画线提取高度曲线，再在下方曲线上选择两段。";
        }

        /// <summary>
        /// 重置输出状态、热力图对象和自动测量数值。
        /// </summary>
        private void ResetRuntimeResult(bool clearVisualization = true)
        {
            HeightDiff = double.NaN;
            AreaHeightDiff = double.NaN;
            AreaHeightDiffDisplayText = "--";
            HeatmapRangeMin = double.NaN;
            HeatmapRangeMax = double.NaN;
            HeightRangeMin = double.NaN;
            HeightRangeMax = double.NaN;
            UsedImagePath = string.Empty;
            if (clearVisualization)
            {
                HeatmapPreviewImage = null;
                HeatmapColorBarImage = null;
                ClearHeatmapPreviewDrawObjects();
                SetHeatmapPreviewObject(null);
                DepthRawValues = null;
                DepthImageWidth = 0;
                DepthImageHeight = 0;
                DepthPixelType = string.Empty;
                Zoom = 1.0;
            }

            ClearProfileAnalysis();
        }

        /// <summary>
        /// 从上游输入、手动文件或 PCD 转换结果解析 HALCON 高度图。
        /// </summary>
        private HObject ResolveInputImage(out string sourceText, out string resolvedInputPath)
        {
            if (PcdDepthInputHelper.IsPcdFile(ManualImagePath))
            {
                PcdDepthImageLoadResult pcdLoadResult = PcdDepthInputHelper.LoadPcdAsHeightImage(
                    ManualImagePath,
                    InvalidGrayCenter);
                sourceText = $"使用本地 PCD 文件（原始点云：{ManualImagePath}）";
                resolvedInputPath = pcdLoadResult.TempImagePath;
                return pcdLoadResult.Image;
            }

            if (TryReadImageFromFile(ManualImagePath, out HObject localImage))
            {
                sourceText = "使用本地高度图文件";
                resolvedInputPath = ManualImagePath;
                return localImage;
            }

            throw new InvalidOperationException("请先选择本地高度图或 PCD 文件。");
        }

        /// <summary>
        /// 尝试从磁盘读取高度图文件为 HALCON 图像。
        /// </summary>
        private static bool TryReadImageFromFile(string imagePath, out HObject image)
        {
            image = null!;

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            image = HalconImageConverter.ReadImage(imagePath);
            if (image != null && image.IsInitialized())
            {
                return true;
            }

            image?.Dispose();
            image = null!;
            return false;
        }

        /// <summary>
        /// 生成热力图预览图片在临时目录中的路径。
        /// </summary>
        private string ResolveHeatmapPreviewPath(string resolvedInputPath)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "ReeYin", "HeightDifferenceMeasure");
            Directory.CreateDirectory(tempDirectory);
            string sourceName = Path.GetFileNameWithoutExtension(resolvedInputPath);
            if (string.IsNullOrWhiteSpace(sourceName) && !string.IsNullOrWhiteSpace(ManualImagePath))
            {
                sourceName = Path.GetFileNameWithoutExtension(ManualImagePath);
            }

            string safeSourceName = string.IsNullOrWhiteSpace(sourceName) ? "height_diff" : sourceName;
            return Path.Combine(tempDirectory, $"{safeSourceName}_heatmap_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        }

        /// <summary>
        /// 读取热力图预览图片并刷新 WPF 显示对象。
        /// </summary>
        private void LoadHeatmapPreview(string previewPath)
        {
            if (string.IsNullOrWhiteSpace(previewPath) || !File.Exists(previewPath))
            {
                HeatmapPreviewImage = null;
                HeatmapColorBarImage = null;
                ClearHeatmapPreviewDrawObjects();
                SetHeatmapPreviewObject(null);
                Zoom = 1.0;
                return;
            }

            BitmapImage bitmap = new();
            using FileStream stream = File.Open(previewPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            BitmapSource previewImage = CreatePreviewBitmapSource(bitmap);
            BitmapSource? colorBarImage = CreateColorBarBitmapSource(bitmap);
            BitmapSource? currentImage = HeatmapPreviewImage;
            bool preserveLayout = currentImage != null
                && currentImage.PixelWidth == previewImage.PixelWidth
                && currentImage.PixelHeight == previewImage.PixelHeight;

            IsReplacingHeatmapPreviewWithoutLayoutReset = preserveLayout;
            try
            {
                HeatmapPreviewImage = previewImage;
                HeatmapColorBarImage = colorBarImage;
                LoadHalconHeatmapPreviewObject(previewPath);
                if (!preserveLayout)
                {
                    Zoom = 1.0;
                }
            }
            finally
            {
                IsReplacingHeatmapPreviewWithoutLayoutReset = false;
            }
        }

        /// <summary>
        /// 把热力图上的测量线或矩形同步到预览绘制集合。
        /// </summary>
        public void AddHeatmapPreviewDrawObject(HObject hObject, string color, bool isFillDisplay = false)
        {
            if (hObject == null || !hObject.IsInitialized())
            {
                hObject?.Dispose();
                return;
            }

            HeatmapPreviewDrawObjects.Add(new HalconDrawingObject
            {
                ShapeType = HalconShapeType.Region,
                Hobject = hObject,
                Color = color,
                IsFillDisplay = isFillDisplay
            });
        }

        /// <summary>
        /// 清空热力图预览中的全部测量叠加图形。
        /// </summary>
        public void ClearHeatmapPreviewDrawObjects()
        {
            foreach (HalconDrawingObject drawObject in HeatmapPreviewDrawObjects)
            {
                try
                {
                    drawObject?.Hobject?.Dispose();
                }
                catch
                {
                }
            }

            HeatmapPreviewDrawObjects.Clear();
        }

        /// <summary>
        /// 把热力图预览图片加载为 HALCON 图像对象供窗口显示。
        /// </summary>
        private void LoadHalconHeatmapPreviewObject(string previewPath)
        {
            HObject? fullImage = null;
            HObject? previewImage = null;
            try
            {
                fullImage = HalconImageConverter.ReadImage(previewPath);
                if (fullImage == null || !fullImage.IsInitialized())
                {
                    SetHeatmapPreviewObject(null);
                    return;
                }

                if (TryCreatePreviewCropRectFromSize(out Int32Rect cropRect))
                {
                    HOperatorSet.CropPart(
                        fullImage,
                        out previewImage,
                        cropRect.Y,
                        cropRect.X,
                        cropRect.Width,
                        cropRect.Height);
                }
                else
                {
                    previewImage = fullImage.Clone();
                }

                SetHeatmapPreviewObject(previewImage);
                previewImage = null;
            }
            catch
            {
                previewImage?.Dispose();
                SetHeatmapPreviewObject(null);
            }
            finally
            {
                fullImage?.Dispose();
            }
        }

        /// <summary>
        /// 根据预览图尺寸计算裁掉色带后的热力图区域。
        /// </summary>
        private bool TryCreatePreviewCropRectFromSize(out Int32Rect cropRect)
        {
            cropRect = default;
            if (DepthImageWidth <= 0 || DepthImageHeight <= 0)
            {
                return false;
            }

            cropRect = new Int32Rect(HeatmapPreviewMargin, HeatmapPreviewMargin, DepthImageWidth, DepthImageHeight);
            return true;
        }

        /// <summary>
        /// 替换热力图 HALCON 预览对象并释放旧对象。
        /// </summary>
        private void SetHeatmapPreviewObject(HObject? image)
        {
            HObject? oldImage = _heatmapPreviewObject;
            HeatmapPreviewObject = image;
            if (!ReferenceEquals(oldImage, image))
            {
                DisposeHObject(oldImage);
            }
        }

        /// <summary>
        /// 安全释放 HALCON 对象并忽略重复释放异常。
        /// </summary>
        private static void DisposeHObject(HObject? hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 释放模型持有的 HALCON 图像和预览资源。
        /// </summary>
        public override void Dispose()
        {
            ClearHeatmapPreviewDrawObjects();
            SetHeatmapPreviewObject(null);
            base.Dispose();
        }

        /// <summary>
        /// 生成 WPF 色带图像供界面显示热力范围。
        /// </summary>
        private BitmapSource? CreateColorBarBitmapSource(BitmapSource fullBitmap)
        {
            if (DepthImageWidth <= 0
                || DepthImageHeight <= 0
                || fullBitmap.PixelWidth <= 0
                || fullBitmap.PixelHeight <= 0)
            {
                return null;
            }

            int colorBarX = HeatmapPreviewMargin + DepthImageWidth + 30;
            int colorBarY = HeatmapPreviewMargin;
            int colorBarWidth = 28;
            int colorBarHeight = DepthImageHeight;
            if (colorBarX < 0
                || colorBarY < 0
                || colorBarWidth <= 0
                || colorBarHeight <= 0
                || colorBarX + colorBarWidth > fullBitmap.PixelWidth
                || colorBarY + colorBarHeight > fullBitmap.PixelHeight)
            {
                return null;
            }

            CroppedBitmap colorBarBitmap = new(fullBitmap, new Int32Rect(colorBarX, colorBarY, colorBarWidth, colorBarHeight));
            colorBarBitmap.Freeze();
            return colorBarBitmap;
        }

        /// <summary>
        /// 缓存当前热力图对应的深度数组，供手动测量复用。
        /// </summary>
        private void CacheDepthImageData(HObject inputImage)
        {
            DepthImageData depthImageData = DepthProfileAnalysisHelper.LoadDepthImage(inputImage);
            DepthRawValues = depthImageData.RawValues;
            DepthImageWidth = depthImageData.Width;
            DepthImageHeight = depthImageData.Height;
            DepthPixelType = depthImageData.PixelType;
            ClearProfileAnalysis();
        }

        /// <summary>
        /// 从图像文件创建冻结的 WPF 预览位图。
        /// </summary>
        private BitmapSource CreatePreviewBitmapSource(BitmapSource fullBitmap)
        {
            if (!TryCreatePreviewCropRect(fullBitmap, out Int32Rect cropRect))
            {
                return fullBitmap;
            }

            CroppedBitmap croppedBitmap = new(fullBitmap, cropRect);
            croppedBitmap.Freeze();
            return croppedBitmap;
        }

        /// <summary>
        /// 根据热力图预览文件尺寸计算可显示的裁剪区域。
        /// </summary>
        private bool TryCreatePreviewCropRect(BitmapSource bitmap, out Int32Rect cropRect)
        {
            cropRect = default;

            if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            {
                return false;
            }

            int cropX = HeatmapPreviewMargin;
            int cropY = HeatmapPreviewMargin;
            int cropWidth = DepthImageWidth;
            int cropHeight = DepthImageHeight;

            if (cropWidth <= 0 || cropHeight <= 0)
            {
                cropWidth = bitmap.PixelWidth - HeatmapPreviewExtraWidth;
                cropHeight = bitmap.PixelHeight - HeatmapPreviewExtraHeight;
            }

            if (cropWidth <= 0
                || cropHeight <= 0
                || cropX + cropWidth > bitmap.PixelWidth
                || cropY + cropHeight > bitmap.PixelHeight)
            {
                return false;
            }

            cropRect = new Int32Rect(cropX, cropY, cropWidth, cropHeight);
            return true;
        }
    }
}

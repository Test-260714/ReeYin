using ALGO.BlobAnalysis.Controls;
using ALGO.BlobAnalysis.Models;
using HalconDotNet;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ALGO.BlobAnalysis
{
    [Serializable]
    public partial class BlobAnalysisModel : ModelParamBase
    {
        private const double DefaultInitRegionCenterX = 50.0;
        private const double DefaultInitRegionCenterY = 50.0;
        private const double LegacyDefaultInitRegionCenterX = 500.0;
        private const double LegacyDefaultInitRegionCenterY = 500.0;
        private const double DefaultInitRegionLength1 = 80.0;
        private const double DefaultInitRegionLength2 = 60.0;

        [JsonIgnore]
        private readonly object _roiSyncRoot = new object();

        [JsonIgnore]
        [InputParam(nameof(InputImage), "输入图像参数")]
        private TransmitParam _inputImage = new TransmitParam();
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
        private BlobAnalysisHalconControl _imageControl;
        [JsonIgnore]
        public BlobAnalysisHalconControl ImageControl
        {
            get { return _imageControl; }
            set { SetProperty(ref _imageControl, value); }
        }

        [JsonIgnore]
        private bool _disenableAffine2d;
        [JsonIgnore]
        public bool DisenableAffine2d
        {
            get { return _disenableAffine2d; }
            set { SetProperty(ref _disenableAffine2d, value); }
        }

        [JsonIgnore]
        private bool _initRegionChangedFlag;
        [JsonIgnore]
        public bool InitRegionChangedFlag
        {
            get { return _initRegionChangedFlag; }
            set { SetProperty(ref _initRegionChangedFlag, value); }
        }

        [JsonIgnore]
        public Dictionary<string, ROI> RoiList = new Dictionary<string, ROI>();

        [JsonIgnore]
        private double _initRegionCenterX = DefaultInitRegionCenterX;
        public double InitRegionCenterX
        {
            get { return _initRegionCenterX; }
            set
            {
                _initRegionCenterX = value;
                RaisePropertyChanged();
                InitRegionChanged();
            }
        }

        [JsonIgnore]
        private double _initRegionCenterY = DefaultInitRegionCenterY;
        public double InitRegionCenterY
        {
            get { return _initRegionCenterY; }
            set
            {
                _initRegionCenterY = value;
                RaisePropertyChanged();
                InitRegionChanged();
            }
        }

        [JsonIgnore]
        private double _initRegionAngleDeg;
        public double InitRegionAngleDeg
        {
            get { return _initRegionAngleDeg; }
            set
            {
                _initRegionAngleDeg = value;
                RaisePropertyChanged();
                InitRegionChanged();
            }
        }

        [JsonIgnore]
        private double _initRegionLength1 = DefaultInitRegionLength1;
        public double InitRegionLength1
        {
            get { return _initRegionLength1; }
            set
            {
                _initRegionLength1 = value;
                RaisePropertyChanged();
                InitRegionChanged();
            }
        }

        [JsonIgnore]
        private double _initRegionLength2 = DefaultInitRegionLength2;
        public double InitRegionLength2
        {
            get { return _initRegionLength2; }
            set
            {
                _initRegionLength2 = value;
                RaisePropertyChanged();
                InitRegionChanged();
            }
        }

        [JsonIgnore]
        public HTuple HomMat2D { get; set; } = new HTuple();

        [JsonIgnore]
        public HTuple HomMat2D_Inverse { get; set; } = new HTuple();

        [JsonIgnore]
        public ROIRectangle2 InitRegion { get; set; } = new ROIRectangle2();

        [JsonIgnore]
        public ROIRectangle2 TempRegion { get; set; } = new ROIRectangle2();

        [JsonIgnore]
        public ROIRectangle2 TranRegion { get; set; } = new ROIRectangle2();

        [JsonIgnore]
        public ROIRectangle2 roiRegion { get; set; } = new ROIRectangle2();

        [JsonIgnore]
        public List<HRoi> mHRoi { get; set; } = new List<HRoi>();

        private BlobThresholdMode _thresholdMode = BlobThresholdMode.固定阈值;
        public BlobThresholdMode ThresholdMode
        {
            get { return _thresholdMode; }
            set { SetProperty(ref _thresholdMode, value); }
        }

        private BlobLocalThresholdType _localThresholdType = BlobLocalThresholdType.暗;
        public BlobLocalThresholdType LocalThresholdType
        {
            get { return _localThresholdType; }
            set { SetProperty(ref _localThresholdType, value); }
        }

        private double _fixedMinGray;
        public double FixedMinGray
        {
            get { return _fixedMinGray; }
            set { SetProperty(ref _fixedMinGray, value); }
        }

        private double _fixedMaxGray = 255;
        public double FixedMaxGray
        {
            get { return _fixedMaxGray; }
            set { SetProperty(ref _fixedMaxGray, value); }
        }

        private int _localMaskH = 15;
        public int LocalMaskH
        {
            get { return _localMaskH; }
            set { SetProperty(ref _localMaskH, value); }
        }

        private int _localMaskW = 15;
        public int LocalMaskW
        {
            get { return _localMaskW; }
            set { SetProperty(ref _localMaskW, value); }
        }

        private double _localStdFactor = 0.2;
        public double LocalStdFactor
        {
            get { return _localStdFactor; }
            set { SetProperty(ref _localStdFactor, value); }
        }

        private double _localAbsThreshold = 5;
        public double LocalAbsThreshold
        {
            get { return _localAbsThreshold; }
            set { SetProperty(ref _localAbsThreshold, value); }
        }

        private bool _fillUp = true;
        public bool FillUp
        {
            get { return _fillUp; }
            set { SetProperty(ref _fillUp, value); }
        }

        private BlobFilterMode _filterMode = BlobFilterMode.与;
        public BlobFilterMode FilterMode
        {
            get { return _filterMode; }
            set { SetProperty(ref _filterMode, value); }
        }

        private string _sortFeature = "area";
        public string SortFeature
        {
            get { return _sortFeature; }
            set { SetProperty(ref _sortFeature, string.IsNullOrWhiteSpace(value) ? "area" : value); }
        }

        private bool _sortDescending = true;
        public bool SortDescending
        {
            get { return _sortDescending; }
            set { SetProperty(ref _sortDescending, value); }
        }

        private int _selectedBlobIndex = 1;
        public int SelectedBlobIndex
        {
            get { return _selectedBlobIndex; }
            set { SetProperty(ref _selectedBlobIndex, Math.Max(1, value)); }
        }

        private ObservableCollection<BlobFeatureDefinition> _propertyDefinitions = BlobFeatureDefinition.CreateDefaultDefinitions();
        public ObservableCollection<BlobFeatureDefinition> PropertyDefinitions
        {
            get { return _propertyDefinitions; }
            set { SetProperty(ref _propertyDefinitions, value); }
        }

        [JsonIgnore]
        private bool _showFilteredRegion = true;
        [JsonIgnore]
        public bool ShowFilteredRegion
        {
            get { return _showFilteredRegion; }
            set { SetProperty(ref _showFilteredRegion, value); }
        }

        [JsonIgnore]
        private bool _showSelectedRegion = true;
        [JsonIgnore]
        public bool ShowSelectedRegion
        {
            get { return _showSelectedRegion; }
            set { SetProperty(ref _showSelectedRegion, value); }
        }

        [JsonIgnore]
        private bool _showCenterPoint = true;
        [JsonIgnore]
        public bool ShowCenterPoint
        {
            get { return _showCenterPoint; }
            set { SetProperty(ref _showCenterPoint, value); }
        }

        [JsonIgnore]
        private bool _showCentroidPoint = true;
        [JsonIgnore]
        public bool ShowCentroidPoint
        {
            get { return _showCentroidPoint; }
            set { SetProperty(ref _showCentroidPoint, value); }
        }

        [JsonIgnore]
        private HObject _outRegion = CreateEmptyObject();
        [JsonIgnore]
        [OutputParam("OutRegion", "筛选后的Blob区域")]
        public HObject OutRegion
        {
            get { return _outRegion; }
            set { SetProperty(ref _outRegion, value); }
        }

        [JsonIgnore]
        private HObject _outSelectedRegion = CreateEmptyObject();
        [JsonIgnore]
        [OutputParam("OutSelectedRegion", "选中的Blob区域")]
        public HObject OutSelectedRegion
        {
            get { return _outSelectedRegion; }
            set { SetProperty(ref _outSelectedRegion, value); }
        }

        [JsonIgnore]
        private HObject _outContour = CreateEmptyObject();
        [JsonIgnore]
        [OutputParam("OutContour", "选中Blob轮廓")]
        public HObject OutContour
        {
            get { return _outContour; }
            set { SetProperty(ref _outContour, value); }
        }

        [JsonIgnore]
        private int _outBlobCount;
        [JsonIgnore]
        [OutputParam("OutBlobCount", "筛选后Blob数量")]
        public int OutBlobCount
        {
            get { return _outBlobCount; }
            set { SetProperty(ref _outBlobCount, value); }
        }

        [JsonIgnore]
        private double _outArea = -1;
        [JsonIgnore]
        [OutputParam("OutArea", "选中Blob面积")]
        public double OutArea
        {
            get { return _outArea; }
            set { SetProperty(ref _outArea, value); }
        }

        [JsonIgnore]
        private double _outCenterX = -1;
        [JsonIgnore]
        [OutputParam("OutCenterX", "选中Blob中心X")]
        public double OutCenterX
        {
            get { return _outCenterX; }
            set { SetProperty(ref _outCenterX, value); }
        }

        [JsonIgnore]
        private double _outCenterY = -1;
        [JsonIgnore]
        [OutputParam("OutCenterY", "选中Blob中心Y")]
        public double OutCenterY
        {
            get { return _outCenterY; }
            set { SetProperty(ref _outCenterY, value); }
        }

        [JsonIgnore]
        private double _outCentroidX = -1;
        [JsonIgnore]
        [OutputParam("OutCentroidX", "选中Blob质心X")]
        public double OutCentroidX
        {
            get { return _outCentroidX; }
            set { SetProperty(ref _outCentroidX, value); }
        }

        [JsonIgnore]
        private double _outCentroidY = -1;
        [JsonIgnore]
        [OutputParam("OutCentroidY", "选中Blob质心Y")]
        public double OutCentroidY
        {
            get { return _outCentroidY; }
            set { SetProperty(ref _outCentroidY, value); }
        }

        [JsonIgnore]
        private double _outRectAngle;
        [JsonIgnore]
        [OutputParam("OutRectAngle", "外接矩形角度")]
        public double OutRectAngle
        {
            get { return _outRectAngle; }
            set { SetProperty(ref _outRectAngle, value); }
        }

        [JsonIgnore]
        private double _outRectLength1 = -1;
        [JsonIgnore]
        [OutputParam("OutRectLength1", "外接矩形半长")]
        public double OutRectLength1
        {
            get { return _outRectLength1; }
            set { SetProperty(ref _outRectLength1, value); }
        }

        [JsonIgnore]
        private double _outRectLength2 = -1;
        [JsonIgnore]
        [OutputParam("OutRectLength2", "外接矩形半宽")]
        public double OutRectLength2
        {
            get { return _outRectLength2; }
            set { SetProperty(ref _outRectLength2, value); }
        }

        [JsonIgnore]
        private double[] _outFeatureValues = Array.Empty<double>();
        [JsonIgnore]
        [OutputParam("OutFeatureValues", "输出特征值")]
        public double[] OutFeatureValues
        {
            get { return _outFeatureValues; }
            set { SetProperty(ref _outFeatureValues, value); }
        }

        public BlobAnalysisModel()
        {
            EnsurePropertyDefinitions();
        }

        public void EnsurePropertyDefinitions()
        {
            PropertyDefinitions ??= BlobFeatureDefinition.CreateDefaultDefinitions();
            if (PropertyDefinitions.Count == 0)
            {
                PropertyDefinitions = BlobFeatureDefinition.CreateDefaultDefinitions();
            }
        }

        public override bool LoadKeyParam()
        {
            try
            {
                return base.LoadKeyParam();
            }
            catch
            {
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

                TriggerModuleRun = () => ExecuteModule().Result;
                IsOnceInit = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                NodeStatus status = ExecuteCore();
                return RefreshOutputParams(status);
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };

            return Task.FromResult(Output);
        }

        public override void Dispose()
        {
            try
            {
                DetachImageControl();
                SafeDisposeHObject(_outRegion);
                SafeDisposeHObject(_outSelectedRegion);
                SafeDisposeHObject(_outContour);
            }
            finally
            {
                base.Dispose();
            }
        }

        public NodeStatus Run()
        {
            return ExecuteCore();
        }

        private NodeStatus ExecuteCore()
        {
            if (!IsDebug)
            {
                LoadKeyParam();
            }

            try
            {
                if (_inputImage.Value == null)
                {
                    ClearDisplayRois();
                    ResetOutputs();
                    ShowHRoi();
                    return NodeStatus.None;
                }

                using HImage tempImage = new HImage((HObject)_inputImage.Value).Clone();
                ClearDisplayRois();
                if (tempImage == null || !tempImage.IsInitialized())
                {
                    ResetOutputs();
                    return NodeStatus.Error;
                }

                PrepareAnalysisRegion();
                using BlobComputationResult result = AnalyzeBlob(tempImage, TranRegion);
                ApplyComputationResult(result);
                RenderComputationResult(result);
                ShowHRoi();
                return NodeStatus.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                ResetOutputs();
                ShowHRoi();
                return NodeStatus.Error;
            }
        }

        private void PrepareAnalysisRegion()
        {
            if (DisenableAffine2d)
            {
                DisenableAffine2d = false;
                if (TryGetInverseHomMat2D(out HTuple inverseHomMat2D))
                {
                    Affine2d(inverseHomMat2D, TempRegion, InitRegion);
                    NormalizeRectangle(InitRegion);
                }
                else
                {
                    CopyRectangle(TempRegion, InitRegion);
                    NormalizeRectangle(InitRegion);
                }

                if (InitRegionChangedFlag)
                {
                    UpdateInitRegionProperties(InitRegion);
                }
            }

            if (HasAffineTransform(HomMat2D))
            {
                InitRegion.MidC = InitRegionCenterX;
                InitRegion.MidR = InitRegionCenterY;
                InitRegion.Length1 = InitRegionLength1;
                InitRegion.Length2 = InitRegionLength2;
                InitRegion.Phi = DegreeToRadian(InitRegionAngleDeg);
                Affine2d(HomMat2D, InitRegion, TranRegion);
                NormalizeRectangle(TranRegion);
            }
            else
            {
                InitRegion.MidC = TempRegion.MidC;
                InitRegion.MidR = TempRegion.MidR;
                InitRegion.Length1 = TempRegion.Length1;
                InitRegion.Length2 = TempRegion.Length2;
                InitRegion.Phi = TempRegion.Phi;
                CopyRectangle(TempRegion, TranRegion);
            }
        }

        private BlobComputationResult AnalyzeBlob(HImage image, ROIRectangle2 analysisRegion)
        {
            BlobComputationResult result = new BlobComputationResult();
            HObject rectangle = null;
            HObject reducedImage = null;
            HObject thresholdRegion = null;
            HObject roiRegion = null;
            HObject connectedRegions = null;
            HObject filteredRegions = null;
            HObject unionRegion = null;
            HObject selectedRegion = null;

            try
            {
                HOperatorSet.GenRectangle2(
                    out rectangle,
                    analysisRegion.MidR,
                    analysisRegion.MidC,
                    -analysisRegion.Phi,
                    analysisRegion.Length1,
                    analysisRegion.Length2);
                HOperatorSet.ReduceDomain(image, rectangle, out reducedImage);

                thresholdRegion = CreateThresholdRegion(reducedImage);
                HOperatorSet.Intersection(rectangle, thresholdRegion, out roiRegion);

                if (FillUp)
                {
                    HOperatorSet.FillUp(roiRegion, out HObject fillRegion);
                    roiRegion.Dispose();
                    roiRegion = fillRegion;
                }

                HOperatorSet.Connection(roiRegion, out connectedRegions);
                filteredRegions = FilterBlobRegions(connectedRegions);
                HOperatorSet.CountObj(filteredRegions, out HTuple countTuple);

                result.BlobCount = countTuple.I;
                if (result.BlobCount <= 0)
                {
                    result.FilteredRegion = CreateEmptyObject();
                    result.FilteredContour = CreateEmptyObject();
                    return result;
                }

                HOperatorSet.Union1(filteredRegions, out unionRegion);
                result.FilteredRegion = CloneObject(unionRegion);
                HOperatorSet.GenContourRegionXld(unionRegion, out HObject filteredContour, "border");
                result.FilteredContour = filteredContour;

                int selectedIndex = ResolveSelectedIndex(filteredRegions, result.BlobCount);
                HOperatorSet.SelectObj(filteredRegions, out selectedRegion, selectedIndex);
                result.SelectedRegion = CloneObject(selectedRegion);
                HOperatorSet.GenContourRegionXld(selectedRegion, out HObject selectedContour, "border");
                result.SelectedContour = selectedContour;

                HOperatorSet.AreaCenter(selectedRegion, out HTuple area, out HTuple centroidRow, out HTuple centroidCol);
                result.Area = Round4(area.D);
                result.CentroidY = Round4(centroidRow.D);
                result.CentroidX = Round4(centroidCol.D);

                HOperatorSet.SmallestRectangle2(selectedRegion, out HTuple rectRow, out HTuple rectCol, out HTuple rectPhi, out HTuple rectLength1, out HTuple rectLength2);
                result.CenterY = Round4(rectRow.D);
                result.CenterX = Round4(rectCol.D);
                result.RectAngle = Round4(((HTuple)rectPhi.D).TupleDeg().D);
                result.RectLength1 = Round4(rectLength1.D);
                result.RectLength2 = Round4(rectLength2.D);

                HOperatorSet.GenCrossContourXld(out HObject centerCross, rectRow, rectCol, 20, new HTuple(45).TupleRad());
                result.CenterCross = centerCross;

                HOperatorSet.GenCrossContourXld(out HObject centroidCross, centroidRow, centroidCol, 26, new HTuple(0).TupleRad());
                result.CentroidCross = centroidCross;

                result.FeatureValues = ExtractOutputFeatureValues(selectedRegion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                rectangle?.Dispose();
                reducedImage?.Dispose();
                thresholdRegion?.Dispose();
                roiRegion?.Dispose();
                connectedRegions?.Dispose();
                filteredRegions?.Dispose();
                unionRegion?.Dispose();
                selectedRegion?.Dispose();
            }

            return result;
        }

        private HObject CreateThresholdRegion(HObject reducedImage)
        {
            switch (ThresholdMode)
            {
                case BlobThresholdMode.局部阈值:
                    HOperatorSet.VarThreshold(
                        reducedImage,
                        out HObject localRegion,
                        LocalMaskH,
                        LocalMaskW,
                        LocalStdFactor,
                        LocalAbsThreshold,
                        LocalThresholdType == BlobLocalThresholdType.暗 ? "dark" : "light");
                    return localRegion;
                case BlobThresholdMode.自动暗:
                    HOperatorSet.BinaryThreshold(reducedImage, out HObject autoDarkRegion, "max_separability", "dark", out HTuple _);
                    return autoDarkRegion;
                case BlobThresholdMode.自动亮:
                    HOperatorSet.BinaryThreshold(reducedImage, out HObject autoLightRegion, "max_separability", "light", out HTuple _);
                    return autoLightRegion;
                default:
                    HOperatorSet.Threshold(reducedImage, out HObject fixedRegion, FixedMinGray, FixedMaxGray);
                    return fixedRegion;
            }
        }

        private HObject FilterBlobRegions(HObject connectedRegions)
        {
            List<int> indexes = ResolveFilterIndexes(connectedRegions);
            HObject result = CreateEmptyObject();

            foreach (int index in indexes)
            {
                HOperatorSet.SelectObj(connectedRegions, out HObject currentRegion, index);
                HOperatorSet.ConcatObj(result, currentRegion, out HObject concatRegion);
                result.Dispose();
                currentRegion.Dispose();
                result = concatRegion;
            }

            return result;
        }

        private List<int> ResolveFilterIndexes(HObject connectedRegions)
        {
            HOperatorSet.CountObj(connectedRegions, out HTuple countTuple);
            int count = countTuple.I;
            List<int> allIndexes = Enumerable.Range(1, count).ToList();
            List<BlobFeatureDefinition> selectedFeatures = PropertyDefinitions?
                .Where(item => item.IsSelected)
                .ToList() ?? new List<BlobFeatureDefinition>();

            if (selectedFeatures.Count == 0)
            {
                return allIndexes;
            }

            bool[] matches = FilterMode == BlobFilterMode.与
                ? Enumerable.Repeat(true, count).ToArray()
                : new bool[count];

            foreach (BlobFeatureDefinition feature in selectedFeatures)
            {
                if (!TryGetFeatureValues(connectedRegions, feature.HName, out double[] values))
                {
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    bool current = i < values.Length &&
                        values[i] >= feature.MinValue &&
                        values[i] <= feature.MaxValue;

                    if (FilterMode == BlobFilterMode.与)
                    {
                        matches[i] &= current;
                    }
                    else
                    {
                        matches[i] |= current;
                    }
                }
            }

            List<int> result = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (matches[i])
                {
                    result.Add(i + 1);
                }
            }

            return result;
        }

        private int ResolveSelectedIndex(HObject filteredRegions, int count)
        {
            int rank = Math.Clamp(SelectedBlobIndex, 1, Math.Max(1, count));
            if (string.IsNullOrWhiteSpace(SortFeature) ||
                !TryGetFeatureValues(filteredRegions, SortFeature, out double[] values) ||
                values.Length != count)
            {
                return rank;
            }

            IOrderedEnumerable<(int Index, double Value)> ordered = SortDescending
                ? Enumerable.Range(1, count).Select(index => (Index: index, Value: values[index - 1])).OrderByDescending(item => item.Value)
                : Enumerable.Range(1, count).Select(index => (Index: index, Value: values[index - 1])).OrderBy(item => item.Value);

            return ordered.ElementAt(rank - 1).Index;
        }

        private bool TryGetFeatureValues(HObject regions, string featureName, out double[] values)
        {
            try
            {
                HOperatorSet.RegionFeatures(regions, featureName, out HTuple featureTuple);
                values = featureTuple.ToDArr();
                return true;
            }
            catch
            {
                values = Array.Empty<double>();
                return false;
            }
        }

        private double[] ExtractOutputFeatureValues(HObject selectedRegion)
        {
            List<BlobFeatureDefinition> outputFeatures = PropertyDefinitions?
                .Where(item => item.IsOut)
                .ToList() ?? new List<BlobFeatureDefinition>();

            if (outputFeatures.Count == 0)
            {
                return Array.Empty<double>();
            }

            List<double> values = new List<double>();
            foreach (BlobFeatureDefinition feature in outputFeatures)
            {
                try
                {
                    HOperatorSet.RegionFeatures(selectedRegion, feature.HName, out HTuple valueTuple);
                    values.Add(valueTuple.Length > 0 ? Round4(valueTuple[0].D) : double.NaN);
                }
                catch
                {
                    values.Add(double.NaN);
                }
            }

            return values.ToArray();
        }

        private void ApplyComputationResult(BlobComputationResult result)
        {
            OutBlobCount = result.BlobCount;
            OutArea = result.Area;
            OutCenterX = result.CenterX;
            OutCenterY = result.CenterY;
            OutCentroidX = result.CentroidX;
            OutCentroidY = result.CentroidY;
            OutRectAngle = result.RectAngle;
            OutRectLength1 = result.RectLength1;
            OutRectLength2 = result.RectLength2;
            OutFeatureValues = result.FeatureValues?.DeepCopy() ?? Array.Empty<double>();
            OutRegion = CloneObject(result.FilteredRegion);
            OutSelectedRegion = CloneObject(result.SelectedRegion);
            OutContour = CloneObject(result.SelectedContour);
        }

        private void ResetOutputs()
        {
            OutBlobCount = 0;
            OutArea = -1;
            OutCenterX = -1;
            OutCenterY = -1;
            OutCentroidX = -1;
            OutCentroidY = -1;
            OutRectAngle = 0;
            OutRectLength1 = -1;
            OutRectLength2 = -1;
            OutFeatureValues = Array.Empty<double>();
            OutRegion = CreateEmptyObject();
            OutSelectedRegion = CreateEmptyObject();
            OutContour = CreateEmptyObject();
        }

        private void RenderComputationResult(BlobComputationResult result)
        {
            if (ShowFilteredRegion && IsValidHObject(result.FilteredContour))
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测范围, "yellow", CloneObject(result.FilteredContour)));
            }

            if (ShowSelectedRegion && IsValidHObject(result.SelectedContour))
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测结果, "lime green", CloneObject(result.SelectedContour)));
            }

            if (ShowCenterPoint && IsValidHObject(result.CenterCross))
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测中心, "orange", CloneObject(result.CenterCross)));
            }

            if (ShowCentroidPoint && IsValidHObject(result.CentroidCross))
            {
                ShowHRoi(new HRoi(Serial, ModuleName, "", HRoiType.检测点, "cyan", CloneObject(result.CentroidCross)));
            }
        }

        private static HObject CreateEmptyObject()
        {
            HOperatorSet.GenEmptyObj(out HObject emptyObject);
            return emptyObject;
        }

        private static HObject CloneObject(HObject source)
        {
            return IsValidHObject(source) ? source.Clone() : CreateEmptyObject();
        }

        private NodeStatus RefreshOutputParams(NodeStatus status)
        {
            Dictionary<string, object> outputValues = OutputParamCollector.GetDataPointValues(this);

            foreach (TransmitParam item in OutputParams ?? Enumerable.Empty<TransmitParam>())
            {
                if (item == null)
                {
                    continue;
                }

                string paramName = string.IsNullOrWhiteSpace(item.ParamName) ? item.Name : item.ParamName;
                if (string.IsNullOrWhiteSpace(paramName) ||
                    !outputValues.TryGetValue(paramName, out object value))
                {
                    continue;
                }

                object outputValue = CloneOutputValueForBoundary(value);
                DisposePreviousOutputValue(item.Value, value, outputValue);
                item.Value = outputValue;
            }

            UpdateParam();
            return status;
        }

        private static object CloneOutputValueForBoundary(object value)
        {
            if (value is HObject hObject)
            {
                return CloneObject(hObject);
            }

            if (value is double[] values)
            {
                return values.DeepCopy();
            }

            return value;
        }

        private static void DisposePreviousOutputValue(object previousValue, object sourceValue, object nextValue)
        {
            if (previousValue is HObject previousHObject &&
                !ReferenceEquals(previousValue, sourceValue) &&
                !ReferenceEquals(previousValue, nextValue))
            {
                SafeDisposeHObject(previousHObject);
            }
        }

        private static void SafeDisposeHObject(HObject hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
                // Cleanup must not change module execution results.
            }
        }

        private static double Round4(double value)
        {
            return Math.Round(value, 4);
        }

        private static double DegreeToRadian(double degree)
        {
            return ((HTuple)degree).TupleRad().D;
        }

        private sealed class BlobComputationResult : IDisposable
        {
            public HObject FilteredRegion { get; set; } = CreateEmptyObject();
            public HObject FilteredContour { get; set; } = CreateEmptyObject();
            public HObject SelectedRegion { get; set; } = CreateEmptyObject();
            public HObject SelectedContour { get; set; } = CreateEmptyObject();
            public HObject CenterCross { get; set; } = CreateEmptyObject();
            public HObject CentroidCross { get; set; } = CreateEmptyObject();
            public int BlobCount { get; set; }
            public double Area { get; set; } = -1;
            public double CenterX { get; set; } = -1;
            public double CenterY { get; set; } = -1;
            public double CentroidX { get; set; } = -1;
            public double CentroidY { get; set; } = -1;
            public double RectAngle { get; set; }
            public double RectLength1 { get; set; } = -1;
            public double RectLength2 { get; set; } = -1;
            public double[] FeatureValues { get; set; } = Array.Empty<double>();

            public void Dispose()
            {
                SafeDisposeHObject(FilteredRegion);
                SafeDisposeHObject(FilteredContour);
                SafeDisposeHObject(SelectedRegion);
                SafeDisposeHObject(SelectedContour);
                SafeDisposeHObject(CenterCross);
                SafeDisposeHObject(CentroidCross);
            }
        }
    }
}

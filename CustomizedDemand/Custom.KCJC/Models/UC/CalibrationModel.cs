using Custom.KCJC.Models.ALGO;
using Custom.KCJC.Models.StandardPlate;
using HalconDotNet;
using Newtonsoft.Json;
using OpenCvSharp;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using static Custom.KCJC.Models.KCJC0_Algorithm;
using static Custom.KCJC.Models.StandardPlate.KCJC0_StandardPlateAlgorithm;
using static System.Net.Mime.MediaTypeNames;

namespace Custom.KCJC.Models
{
    /// <summary>槽明细展示项</summary>
    public class GrooveDisplayItem
    {
        public string Label { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public double WidthStandard { get; set; }
        public double DepthStandard { get; set; }
        /// <summary>槽宽允许标准差（0 = 不判断 OK/NG）</summary>
        public double WidthStdDev { get; set; }
        /// <summary>槽深允许标准差（0 = 不判断 OK/NG）</summary>
        public double DepthStdDev { get; set; }

        public bool WidthIsOK => WidthStdDev <= 0 || Math.Abs(Width - WidthStandard) <= WidthStdDev;
        public bool DepthIsOK => DepthStdDev <= 0 || Math.Abs(Depth - DepthStandard) <= DepthStdDev;

        public string WidthDeviationText
        {
            get
            {
                double d = Width - WidthStandard;
                string devStr = d >= 0 ? $"+{d:F4}" : $"{d:F4}";
                if (WidthStdDev > 0) devStr += WidthIsOK ? "  ✓OK" : "  ✗NG";
                return devStr;
            }
        }
        public string DepthDeviationText
        {
            get
            {
                double d = Depth - DepthStandard;
                string devStr = d >= 0 ? $"+{d:F4}" : $"{d:F4}";
                if (DepthStdDev > 0) devStr += DepthIsOK ? "  ✓OK" : "  ✗NG";
                return devStr;
            }
        }

        public System.Windows.Media.SolidColorBrush WidthDeviationBrush =>
            WidthStdDev <= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0x51, 0x00))
                : WidthIsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));

        public System.Windows.Media.SolidColorBrush DepthDeviationBrush =>
            DepthStdDev <= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0x51, 0x00))
                : DepthIsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
    }

    /// <summary>点明细展示项</summary>
    public class BumpDisplayItem
    {
        public string Label { get; set; }
        public double Height { get; set; }
        public double Diameter { get; set; }
        public double HeightStandard { get; set; }
        public double DiameterStandard { get; set; }
        /// <summary>凸点高度允许标准差（0 = 不判断 OK/NG）</summary>
        public double HeightStdDev { get; set; }
        /// <summary>凸点直径允许标准差（0 = 不判断 OK/NG）</summary>
        public double DiameterStdDev { get; set; }

        public bool HeightIsOK => HeightStdDev <= 0 || Math.Abs(Height - HeightStandard) <= HeightStdDev;
        public bool DiameterIsOK => DiameterStdDev <= 0 || Math.Abs(Diameter - DiameterStandard) <= DiameterStdDev;

        public string HeightDeviationText
        {
            get
            {
                double d = Height - HeightStandard;
                string devStr = d >= 0 ? $"+{d:F4}" : $"{d:F4}";
                if (HeightStdDev > 0) devStr += HeightIsOK ? "  ✓OK" : "  ✗NG";
                return devStr;
            }
        }
        public string DiameterDeviationText
        {
            get
            {
                double d = Diameter - DiameterStandard;
                string devStr = d >= 0 ? $"+{d:F4}" : $"{d:F4}";
                if (DiameterStdDev > 0) devStr += DiameterIsOK ? "  ✓OK" : "  ✗NG";
                return devStr;
            }
        }

        public System.Windows.Media.SolidColorBrush HeightDeviationBrush =>
            HeightStdDev <= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0x51, 0x00))
                : HeightIsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));

        public System.Windows.Media.SolidColorBrush DiameterDeviationBrush =>
            DiameterStdDev <= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0x51, 0x00))
                : DiameterIsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
    }

    /// <summary>台阶明细展示项</summary>
    public class StepDisplayItem
    {
        public string Label { get; set; }
        public double Height { get; set; }
        public double HeightStandard { get; set; }
        /// <summary>台阶高度允许偏差（0 = 不判断 OK/NG）</summary>
        public double HeightStdDev { get; set; }

        public bool HeightIsOK => HeightStdDev <= 0 || Math.Abs(Height - HeightStandard) <= HeightStdDev;

        public string HeightDeviationText
        {
            get
            {
                double d = Height - HeightStandard;
                string devStr = d >= 0 ? $"+{d:F4}" : $"{d:F4}";
                if (HeightStdDev > 0) devStr += HeightIsOK ? "  ✓OK" : "  ✗NG";
                return devStr;
            }
        }

        public System.Windows.Media.SolidColorBrush HeightDeviationBrush =>
            HeightStdDev <= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0x51, 0x00))
                : HeightIsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
    }

    /// <summary>槽标准参考值（每槽独立配置，按下标与测量结果对应）</summary>
    public class GrooveStandardRef
    {
        public string Label { get; set; } = "";
        /// <summary>标准槽宽 (um)</summary>
        public double WidthStandard { get; set; } = 1050;
        /// <summary>槽宽允许偏差 (um)，0 = 不判断 OK/NG</summary>
        public double WidthStdDev { get; set; } = 0;
        /// <summary>标准槽深 (um)</summary>
        public double DepthStandard { get; set; } = 15;
        /// <summary>槽深允许偏差 (um)，0 = 不判断 OK/NG</summary>
        public double DepthStdDev { get; set; } = 0;
    }

    /// <summary>凸点标准参考值（每点独立配置，按下标与测量结果对应）</summary>
    public class BumpStandardRef
    {
        public string Label { get; set; } = "";
        /// <summary>标准高度 (um)</summary>
        public double HeightStandard { get; set; } = 80;
        /// <summary>高度允许偏差 (um)，0 = 不判断 OK/NG</summary>
        public double HeightStdDev { get; set; } = 0;
    }

    /// <summary>台阶标准参考值（每台阶独立配置，按下标与测量结果对应）</summary>
    public class StepStandardRef
    {
        public string Label { get; set; } = "";
        /// <summary>标准高度 (um)</summary>
        public double HeightStandard { get; set; } = 0;
        /// <summary>高度允许偏差 (um)，0 = 不判断 OK/NG</summary>
        public double HeightStdDev { get; set; } = 0;
    }

    public partial class SensorDataCollectionModel : ModelParamBase
    {
        #region Fields
        /// <summary>
        /// 标定算法
        /// </summary>
        [JsonIgnore]
        KCJC0_StandardPlateAlgorithm CalibALGO;

        [JsonIgnore]
        private KCJC0_StandardPlateMeasureParam _calibMeasureParam = new KCJC0_StandardPlateMeasureParam();
        /// <summary>
        /// 标定参数
        /// </summary>
        public KCJC0_StandardPlateMeasureParam CalibMeasureParam
        {
            get { return _calibMeasureParam; }
            set { _calibMeasureParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<GrooveStandardRef> _grooveStandardRefs = new ObservableCollection<GrooveStandardRef>();
        /// <summary>各槽独立标准参考值（序号与测量结果 GrooveWidthRealListV2/GrooveDepthRealListV2 对应）</summary>
        public ObservableCollection<GrooveStandardRef> GrooveStandardRefs
        {
            get { return _grooveStandardRefs; }
            set { _grooveStandardRefs = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private ObservableCollection<BumpStandardRef> _bumpStandardRefs = new ObservableCollection<BumpStandardRef>();
        /// <summary>各点独立标准参考值（序号与测量结果 BumpHeightPhysicalList 对应）</summary>
        public ObservableCollection<BumpStandardRef> BumpStandardRefs
        {
            get { return _bumpStandardRefs; }
            set { _bumpStandardRefs = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private ObservableCollection<StepStandardRef> _stepStandardRefs = new ObservableCollection<StepStandardRef>();
        /// <summary>各台阶独立标准参考值（序号与测量结果 StepHeightPhysicalList 对应）</summary>
        public ObservableCollection<StepStandardRef> StepStandardRefs
        {
            get { return _stepStandardRefs; }
            set { _stepStandardRefs = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Methods

        #endregion
    }

    /// <summary>
    /// 标定
    /// </summary>
    public class CalibrationModel : BindableBase
    {
        #region Fields
        /// <summary>
        /// 标定算法
        /// </summary>
        [JsonIgnore]
        KCJC0_StandardPlateAlgorithm CalibALGO;

        /// <summary>记住上一次从文件加载时选择的文件夹路径</summary>
        private static string _lastImportFolder;
        #endregion


        #region Properties
        private HObject _disposeImage = new HObject();
        public HObject DisposeImage
        {
            get => _disposeImage;
            set { _disposeImage = value; RaisePropertyChanged(); }
        }

        private KCJC0_StandardPlateMeasureParam _calibMeasureParam = new KCJC0_StandardPlateMeasureParam();
        public KCJC0_StandardPlateMeasureParam CalibMeasureParam
        {
            get => _calibMeasureParam;
            set { _calibMeasureParam = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<GrooveStandardRef> _grooveStandardRefs = new ObservableCollection<GrooveStandardRef>();
        /// <summary>各槽独立标准参考值（绑定自 SensorDataCollectionModel，序号与测量结果对应）</summary>
        public ObservableCollection<GrooveStandardRef> GrooveStandardRefs
        {
            get => _grooveStandardRefs;
            set { _grooveStandardRefs = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<BumpStandardRef> _bumpStandardRefs = new ObservableCollection<BumpStandardRef>();
        /// <summary>各点独立标准参考值（绑定自 SensorDataCollectionModel，序号与测量结果对应）</summary>
        public ObservableCollection<BumpStandardRef> BumpStandardRefs
        {
            get => _bumpStandardRefs;
            set { _bumpStandardRefs = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<StepStandardRef> _stepStandardRefs = new ObservableCollection<StepStandardRef>();
        /// <summary>各台阶独立标准参考值（绑定自 SensorDataCollectionModel，序号与测量结果对应）</summary>
        public ObservableCollection<StepStandardRef> StepStandardRefs
        {
            get => _stepStandardRefs;
            set { _stepStandardRefs = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private GrooveStandardRef _selectedGrooveStandardRef;
        /// <summary>当前选中的槽标准参考值（用于删除操作）</summary>
        public GrooveStandardRef SelectedGrooveStandardRef
        {
            get => _selectedGrooveStandardRef;
            set { _selectedGrooveStandardRef = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private BumpStandardRef _selectedBumpStandardRef;
        /// <summary>当前选中的点标准参考值（用于删除操作）</summary>
        public BumpStandardRef SelectedBumpStandardRef
        {
            get => _selectedBumpStandardRef;
            set { _selectedBumpStandardRef = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private StepStandardRef _selectedStepStandardRef;
        /// <summary>当前选中的台阶标准参考值（用于删除操作）</summary>
        public StepStandardRef SelectedStepStandardRef
        {
            get => _selectedStepStandardRef;
            set { _selectedStepStandardRef = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 标定结果

        [JsonIgnore]
        private KCJC0_StandardPlateMeasureResult _calibResult = new KCJC0_StandardPlateMeasureResult();
        public KCJC0_StandardPlateMeasureResult CalibResult
        {
            get => _calibResult;
            set { _calibResult = value; RaisePropertyChanged(); RefreshDisplayItems(value); }
        }

        [JsonIgnore]
        private ObservableCollection<GrooveDisplayItem> _grooveDisplayItems = new ObservableCollection<GrooveDisplayItem>();
        /// <summary>各槽明细（槽1, 槽2…），绑定到列表展示</summary>
        public ObservableCollection<GrooveDisplayItem> GrooveDisplayItems
        {
            get => _grooveDisplayItems;
            set { _grooveDisplayItems = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<BumpDisplayItem> _bumpDisplayItems = new ObservableCollection<BumpDisplayItem>();
        /// <summary>各点明细（点1, 点2…），绑定到列表展示</summary>
        public ObservableCollection<BumpDisplayItem> BumpDisplayItems
        {
            get => _bumpDisplayItems;
            set { _bumpDisplayItems = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int[] _bumpSegmentCounts = Array.Empty<int>();
        /// <summary>刻点标准片三段合并结果中每段的点数，用于还原“区X点Y”显示名。</summary>
        public int[] BumpSegmentCounts
        {
            get => _bumpSegmentCounts;
            set { _bumpSegmentCounts = value ?? Array.Empty<int>(); RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<StepDisplayItem> _stepDisplayItems = new ObservableCollection<StepDisplayItem>();
        /// <summary>各台阶明细（台阶1, 台阶2…），绑定到列表展示</summary>
        public ObservableCollection<StepDisplayItem> StepDisplayItems
        {
            get => _stepDisplayItems;
            set { _stepDisplayItems = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _grooveSectionVisibility = Visibility.Collapsed;
        /// <summary>槽明细区块可见性</summary>
        public Visibility GrooveSectionVisibility
        {
            get => _grooveSectionVisibility;
            set { _grooveSectionVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _bumpSectionVisibility = Visibility.Collapsed;
        /// <summary>点明细区块可见性</summary>
        public Visibility BumpSectionVisibility
        {
            get => _bumpSectionVisibility;
            set { _bumpSectionVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Visibility _stepSectionVisibility = Visibility.Collapsed;
        /// <summary>台阶明细区块可见性</summary>
        public Visibility StepSectionVisibility
        {
            get => _stepSectionVisibility;
            set { _stepSectionVisibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _computedIntervalX = 0;
        /// <summary>计算得到的 X 方向像素当量</summary>
        public double ComputedIntervalX
        {
            get => _computedIntervalX;
            set { _computedIntervalX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private double _computedIntervalZ = 0;
        /// <summary>计算得到的 Z 方向像素当量</summary>
        public double ComputedIntervalZ
        {
            get => _computedIntervalZ;
            set { _computedIntervalZ = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Methods
        /// <summary>
        /// 根据标定结果刷新槽/点/台阶明细展示集合。
        /// 各槽/点的标准参考值优先取 GrooveStandardRefs/BumpStandardRefs 中对应下标的条目，
        /// 若列表为空或下标越界则回退到 CalibMeasureParam 全局值。
        /// </summary>
        private void RefreshDisplayItems(KCJC0_StandardPlateMeasureResult result)
        {
            // 未配置独立标准参考值时，仅显示测量值，不做全局标准判定。
            double fallbackWidth      = 0;
            double fallbackDepth      = 0;
            double fallbackWidthDev   = 0;
            double fallbackDepthDev   = 0;
            double fallbackHeight     = 0;
            double fallbackDiameter   = 0;
            double fallbackHeightDev  = 0;
            double fallbackDiamDev    = 0;

            GrooveDisplayItems.Clear();
            if (result?.GrooveWidthRealListV2 != null && result.GrooveDepthRealListV2 != null)
            {
                int resultCount = Math.Min(result.GrooveWidthRealListV2.Length, result.GrooveDepthRealListV2.Length);
                // 已配置独立标准参考值时，校验条数是否匹配
                if (GrooveStandardRefs != null && GrooveStandardRefs.Count > 0 && GrooveStandardRefs.Count < resultCount)
                {
                    System.Windows.MessageBox.Show(
                        $"槽标准参考值条数（{GrooveStandardRefs.Count}）少于测量结果条数（{resultCount}），无法显示槽明细。",
                        "数量不匹配",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    for (int i = 0; i < resultCount; i++)
                    {
                        // 按下标取对应槽的独立标准参考值，越界时回退到全局值
                        GrooveStandardRef stdRef = (GrooveStandardRefs != null && i < GrooveStandardRefs.Count)
                            ? GrooveStandardRefs[i] : null;
                        GrooveDisplayItems.Add(new GrooveDisplayItem
                        {
                            Label         = $"槽{i + 1}",
                            Width         = result.GrooveWidthRealListV2[i],
                            Depth         = result.GrooveDepthRealListV2[i],
                            WidthStandard = stdRef?.WidthStandard ?? fallbackWidth,
                            DepthStandard = stdRef?.DepthStandard ?? fallbackDepth,
                            WidthStdDev   = stdRef?.WidthStdDev   ?? fallbackWidthDev,
                            DepthStdDev   = stdRef?.DepthStdDev   ?? fallbackDepthDev,
                        });
                    }
                }
            }

            BumpDisplayItems.Clear();
            if (result?.BumpHeightPhysicalList != null && result.BumpDiameterPhysicalList != null)
            {
                int resultCount = Math.Min(result.BumpHeightPhysicalList.Length, result.BumpDiameterPhysicalList.Length);
                // BumpSegmentCounts 保存三段各自点数，例如 [2,2,2] 表示区1/区2/区3各2个点。
                int[] bumpSegmentCounts = BumpSegmentCounts ?? Array.Empty<int>();
                int segmentTotal = 0;
                int maxSegmentCount = 0;
                foreach (int count in bumpSegmentCounts)
                {
                    if (count <= 0)
                        continue;

                    segmentTotal += count;
                    if (count > maxSegmentCount)
                        maxSegmentCount = count;
                }

                bool useSegmentLabel = CalibMeasureParam?.AlgorithmType == 1
                    && maxSegmentCount > 0
                    && segmentTotal == resultCount;
                // 按“区内点序号”复用点标准值，所以校验数量只需要覆盖单区最大点数。
                int requiredStandardRefCount = useSegmentLabel ? maxSegmentCount : resultCount;
                Logs.LogInfo($"标定页面刷新点明细：算法类型={CalibMeasureParam?.AlgorithmType}，总点数={resultCount}，分段点数=[{string.Join(",", bumpSegmentCounts)}]，分段总数={segmentTotal}，单段最大点数={maxSegmentCount}，使用分段显示={useSegmentLabel}，需要标准点数={requiredStandardRefCount}，当前标准点数={BumpStandardRefs?.Count ?? 0}");
                // 已配置独立标准参考值时，校验条数是否匹配
                if (BumpStandardRefs != null && BumpStandardRefs.Count > 0 && BumpStandardRefs.Count < requiredStandardRefCount)
                {
                    Logs.LogWarning($"点明细未显示：点标准参考值条数（{BumpStandardRefs.Count}）少于测量结果条数（{requiredStandardRefCount}）。");
                    System.Windows.MessageBox.Show(
                        $"点标准参考值条数（{BumpStandardRefs.Count}）少于测量结果条数（{requiredStandardRefCount}），无法显示点明细。",
                        "数量不匹配",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    for (int i = 0; i < resultCount; i++)
                    {
                        string label = $"点{i + 1}";
                        int standardIndex = i;
                        if (useSegmentLabel)
                        {
                            // 将合并后的总下标 i 还原成“区号 + 区内点号”。
                            int startIndex = 0;
                            for (int segmentIndex = 0; segmentIndex < bumpSegmentCounts.Length; segmentIndex++)
                            {
                                int count = bumpSegmentCounts[segmentIndex];
                                if (count <= 0)
                                    continue;

                                if (i < startIndex + count)
                                {
                                    standardIndex = i - startIndex;
                                    label = $"区{segmentIndex + 1}点{standardIndex + 1}";
                                    break;
                                }

                                startIndex += count;
                            }
                        }

                        // 按下标取对应点的独立标准参考值，越界时回退到全局值
                        BumpStandardRef stdRef = (BumpStandardRefs != null && standardIndex < BumpStandardRefs.Count)
                            ? BumpStandardRefs[standardIndex] : null;
                        BumpDisplayItems.Add(new BumpDisplayItem
                        {
                            Label            = label,
                            Height           = result.BumpHeightPhysicalList[i],
                            Diameter         = result.BumpDiameterPhysicalList[i],
                            HeightStandard   = stdRef?.HeightStandard ?? fallbackHeight,
                            DiameterStandard = fallbackDiameter,
                            HeightStdDev     = stdRef?.HeightStdDev   ?? fallbackHeightDev,
                            DiameterStdDev   = fallbackDiamDev,
                        });
                    }
                }
            }

            StepDisplayItems.Clear();
            if (result?.StepHeightPhysicalList != null)
            {
                for (int i = 0; i < result.StepHeightPhysicalList.Length; i++)
                {
                    StepStandardRef stdRef = (StepStandardRefs != null && i < StepStandardRefs.Count)
                        ? StepStandardRefs[i] : null;
                    StepDisplayItems.Add(new StepDisplayItem
                    {
                        Label          = $"台阶{i + 1}",
                        Height         = result.StepHeightPhysicalList[i],
                        HeightStandard = stdRef?.HeightStandard ?? 0,
                        HeightStdDev   = stdRef?.HeightStdDev   ?? 0,
                    });
                }
            }

            GrooveSectionVisibility = GrooveDisplayItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            BumpSectionVisibility   = BumpDisplayItems.Count  > 0 ? Visibility.Visible : Visibility.Collapsed;
            StepSectionVisibility   = StepDisplayItems.Count  > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 执行标定（保留原接口以供传感器采集模式调用）
        /// </summary>
        public bool ExecuteCalib(bool IsFilePath = false)
        {
            try
            {
                CalibMeasureParam.GrooveStandardRefs = GrooveStandardRefs != null
                    ? new List<GrooveStandardRef>(GrooveStandardRefs)
                    : new List<GrooveStandardRef>();
                CalibMeasureParam.BumpStandardRefs = BumpStandardRefs != null
                    ? new List<BumpStandardRef>(BumpStandardRefs)
                    : new List<BumpStandardRef>();

                CalibALGO?.Dispose();

                //if (CalibALGO == null)
                    CalibALGO = KCJC0_StandardPlateAlgorithmService.CreateAlgorithm(CalibMeasureParam);

                #region 从文件中加载
                if (IsFilePath)
                {
                    // 在 UI 线程弹出文件夹选择对话框
                    using var folderDialog = new FolderBrowserDialog();
                    folderDialog.Description = "请选择文件夹路径";
                    folderDialog.SelectedPath = !string.IsNullOrEmpty(_lastImportFolder) && Directory.Exists(_lastImportFolder)
                        ? _lastImportFolder
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return false;

                    string folderPath = folderDialog.SelectedPath;
                    _lastImportFolder = folderPath;
                    string folderName = Path.GetFileName(folderPath); // 获取选中文件夹名称

                    string grayImagePath = Path.Combine(folderPath, "gray", "1_tif.tif");
                    string heightImagePath = Path.Combine(folderPath, "depth", "1_tif_0.tif");

                    // 判断指定文件是否存在
                    if (!File.Exists(grayImagePath))
                    {
                        Logs.LogError(new FileNotFoundException($"灰度图像文件不存在: {grayImagePath}"));
                        return false;
                    }
                    if (!File.Exists(heightImagePath))
                    {
                        Logs.LogError(new FileNotFoundException($"高度图像文件不存在: {heightImagePath}"));
                        return false;
                    }

                    Mat grayImage = null;
                    Mat heightImage = null;
                    try
                    {
                        grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                        heightImage = Cv2.ImRead(heightImagePath, ImreadModes.Unchanged);

                        if (grayImage.Empty() || heightImage.Empty())
                        {
                            Logs.LogError(new InvalidOperationException($"图像读取失败，文件夹：{folderName}"));
                            return false;
                        }

                        List<float[]> grayData = ConvertMatToList(grayImage);
                        List<float[]> heightData = ConvertMatToList(heightImage);

                        CalibResult = CalibALGO.Process(grayData, heightData, CalibMeasureParam);
                        var imageTemp = CalibALGO.CvDrawResult(CalibResult);
                        DisposeImage = Common_Algorithm.MatToHObject(imageTemp);
                        return true;
                    }
                    finally
                    {
                        grayImage?.Dispose();
                        heightImage?.Dispose();
                        CalibALGO?.Dispose();
                    }
                }
                //流程执行（从传感器采集）
                else
                {
                    List<float[]> grayDatas = new List<float[]>(), heightDatas = new List<float[]>();
                    CalibResult = CalibALGO.Process(grayDatas, heightDatas, CalibMeasureParam);
                    return true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
                return false;
            }
        }


        /// <summary>
        /// OpenCVSharp Mat转List<float[]>
        /// </summary>
        public List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat == null || mat.Empty())
                return data;

            Mat work = mat;
            bool needDispose = false;

            if (!mat.IsContinuous())
            {
                work = mat.Clone();
                needDispose = true;
            }

            int channels = mat.Channels();
            if (channels != 1)
                throw new InvalidOperationException("Only single-channel matrices are supported");

            try
            {
                int rows = work.Rows;
                int cols = work.Cols;
                MatType type = work.Type();

                if (type == MatType.CV_8UC1)
                {
                    ProcessByteMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32FC1)
                {
                    ProcessFloatMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32SC1)
                {
                    ProcessIntMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_16SC1)
                {
                    ProcessShortMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                if (needDispose)
                    work.Dispose();
            }

            return data;
        }


        private static void ProcessByteMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            byte[] buffer = new byte[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                int offset = i * cols;
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[offset + j];
                }
                data.Add(row);
            }
        }

        private static void ProcessFloatMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                Array.Copy(buffer, i * cols, row, 0, cols);
                data.Add(row);
            }
        }

        private static void ProcessIntMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            int[] buffer = new int[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转为 float 存入结果
                }
                data.Add(row);
            }
        }

        private static void ProcessShortMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            short[] buffer = new short[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转 float 存储
                }
                data.Add(row);
            }
        }
        #endregion
    }
}

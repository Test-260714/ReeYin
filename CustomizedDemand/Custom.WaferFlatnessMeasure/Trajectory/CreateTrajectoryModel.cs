using System;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Share;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using ReeYin_V.Core.Services.Project;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// 晶圆圆形轨迹生成参数模型，负责把界面配置转换为可执行的线段或点位轨迹。
    /// </summary>
    public class CreateTrajectoryModel : BindableBase
    {
        #region Fields
        private TransmitParam _circleCenterActualXYLinkParam = new TransmitParam();
        private double _circleCenterX;
        private double _circleCenterY;
        private double _circleRadius = 50;

        private double _inscribedCircleRadius = 10;

        private double _circleLineCount = 5;

        private double _pointSpacing = 10;
        private double _customRingAngleSpacing = 10;
        private double _customRingRadiusInput = 10;
        private ObservableCollection<CustomRingDefinition> _customRingDefinitions = new ObservableCollection<CustomRingDefinition>();
        private CircleTrajectoryGenerateMode _circleGenerateMode = CircleTrajectoryGenerateMode.水平线;
        private bool _isOptimalPathEnabled;
        private int _decimalPlaces = 2;
        #endregion

        #region Properties
        public CreateTrajectoryModel()
        {
            _customRingDefinitions.CollectionChanged += OnCustomRingDefinitionsChanged;
        }

        public TransmitParam CircleCenterActualXYLinkParam
        {
            get => _circleCenterActualXYLinkParam;
            set => SetProperty(ref _circleCenterActualXYLinkParam, value ?? new TransmitParam());
        }

        public double CircleCenterX
        {
            get => _circleCenterX;
            set
            {
                if (SetProperty(ref _circleCenterX, value))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public double CircleCenterY
        {
            get => _circleCenterY;
            set
            {
                if (SetProperty(ref _circleCenterY, value))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public double CircleRadius
        {
            get => _circleRadius;
            set
            {
                if (SetProperty(ref _circleRadius, value))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public double InscribedCircleRadius
        {
            get => _inscribedCircleRadius;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Max(value, 0.001) : 0.001;
                if (SetProperty(ref _inscribedCircleRadius, normalizedValue))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        //[ReassignParam("CircleLineCount", "线条数")]
        public double CircleLineCount
        {
            get => _circleLineCount;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Max(1, value) : 1;
                if (SetProperty(ref _circleLineCount, normalizedValue))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        //[ReassignParam("PointSpacing", "点间距")]
        public double PointSpacing
        {
            get => _pointSpacing;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Max(value, 0.001) : 0.001;
                if (SetProperty(ref _pointSpacing, normalizedValue))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public double CustomRingAngleSpacing
        {
            get => _customRingAngleSpacing;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Clamp(value, 0.001, 360) : 0.001;
                if (SetProperty(ref _customRingAngleSpacing, normalizedValue))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public double CustomRingRadiusInput
        {
            get => _customRingRadiusInput;
            set
            {
                double normalizedValue = double.IsFinite(value) ? Math.Max(value, 0.001) : 0.001;
                if (SetProperty(ref _customRingRadiusInput, normalizedValue))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                }
            }
        }

        public ObservableCollection<CustomRingDefinition> CustomRingDefinitions
        {
            get => _customRingDefinitions;
            set
            {
                if (ReferenceEquals(_customRingDefinitions, value))
                {
                    return;
                }

                if (_customRingDefinitions != null)
                {
                    _customRingDefinitions.CollectionChanged -= OnCustomRingDefinitionsChanged;
                }

                _customRingDefinitions = value ?? new ObservableCollection<CustomRingDefinition>();
                _customRingDefinitions.CollectionChanged += OnCustomRingDefinitionsChanged;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
            }
        }

        public CircleTrajectoryGenerateMode CircleGenerateMode
        {
            get => _circleGenerateMode;
            set
            {
                if (SetProperty(ref _circleGenerateMode, value))
                {
                    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
                    RaisePropertyChanged(nameof(IsCircleLineCountEditable));
                    RaisePropertyChanged(nameof(IsInscribedCircleRadiusEditable));
                    RaisePropertyChanged(nameof(InscribedCircleRadiusTitle));
                    RaisePropertyChanged(nameof(IsPointSpacingEditable));
                    RaisePropertyChanged(nameof(IsCustomRingAngleSpacingEditable));
                    RaisePropertyChanged(nameof(IsPointGenerationMode));
                    RaisePropertyChanged(nameof(SelectedCircleGenerateModeTabIndex));
                }
            }
        }

        public bool IsOptimalPathEnabled
        {
            get => _isOptimalPathEnabled;
            set => SetProperty(ref _isOptimalPathEnabled, value);
        }

        [JsonIgnore]
        public int SelectedCircleGenerateModeTabIndex
        {
            get => (int)CircleGenerateMode;
            set
            {
                if (!Enum.IsDefined(typeof(CircleTrajectoryGenerateMode), value))
                {
                    value = (int)CircleTrajectoryGenerateMode.水平线;
                }

                CircleGenerateMode = (CircleTrajectoryGenerateMode)value;
            }
        }

        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set => SetProperty(ref _decimalPlaces, Math.Max(-1, value));
        }

        [JsonIgnore]
        public bool IsCircleLineCountEditable =>
            CircleGenerateMode == CircleTrajectoryGenerateMode.水平线 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.垂直线 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.过圆心交叉线;

        [JsonIgnore]
        public bool IsInscribedCircleRadiusEditable =>
            CircleGenerateMode == CircleTrajectoryGenerateMode.内切圆心点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点;

        [JsonIgnore]
        public string InscribedCircleRadiusTitle => $"{GetInscribedRadiusDisplayName()}：";

        [JsonIgnore]
        public bool IsPointSpacingEditable =>
            CircleGenerateMode == CircleTrajectoryGenerateMode.等间距点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.内切正方形点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.内接同心圆点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点;

        [JsonIgnore]
        public bool IsCustomRingAngleSpacingEditable =>
            CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环;

        [JsonIgnore]
        public bool IsPointGenerationMode =>
            CircleGenerateMode == CircleTrajectoryGenerateMode.内切圆心点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.等间距点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.内切正方形点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.内接同心圆点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点 ||
            CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环;

        [JsonIgnore]
        public string CircleGeneratorSummaryText
        {
            get
            {
                if (!double.IsFinite(CircleCenterX) || !double.IsFinite(CircleCenterY))
                {
                    return "请输入有效的圆心坐标。";
                }

                if (!double.IsFinite(CircleRadius) || CircleRadius <= 0)
                {
                    return "请输入大于 0 的圆半径。";
                }

                if (IsInscribedCircleRadiusEditable &&
                    (!double.IsFinite(InscribedCircleRadius) || InscribedCircleRadius <= 0))
                {
                    return $"请输入大于 0 的{GetInscribedRadiusDisplayName()}。";
                }

                if (IsInscribedCircleRadiusEditable &&
                    InscribedCircleRadius > CircleRadius)
                {
                    return $"{GetInscribedRadiusDisplayName()}不能大于外圆半径。";
                }

                if (IsPointSpacingEditable &&
                    (!double.IsFinite(PointSpacing) || PointSpacing <= 0))
                {
                    return "请输入大于 0 的点间距。";
                }

                if (IsCustomRingAngleSpacingEditable &&
                    (!double.IsFinite(CustomRingAngleSpacing) || CustomRingAngleSpacing <= 0 || CustomRingAngleSpacing > 360))
                {
                    return "请输入大于 0 且不超过 360 的角度间距。";
                }

                if (CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环)
                {
                    var customRingRadii = GetCustomRingRadii();
                    if (customRingRadii.Count == 0)
                    {
                        return "请至少添加一个自定义圆环。";
                    }

                    if (customRingRadii.Any(radius => !double.IsFinite(radius) || radius <= 0))
                    {
                        return "自定义圆环半径必须大于 0。";
                    }

                    if (customRingRadii.Any(radius => radius > CircleRadius))
                    {
                        return "自定义圆环半径不能大于外圆半径。";
                    }
                }

                return CircleGenerateMode switch
                {
                    CircleTrajectoryGenerateMode.水平线 =>
                        $"将在圆内均匀生成 {NormalizeCircleLineCount()} 条水平弦线。",
                    CircleTrajectoryGenerateMode.垂直线 =>
                        $"将在圆内均匀生成 {NormalizeCircleLineCount()} 条垂直弦线。",
                    CircleTrajectoryGenerateMode.过圆心交叉线 =>
                        $"将生成 {NormalizeCircleLineCount()} 条过圆心的直径线。",
                    CircleTrajectoryGenerateMode.内切圆心点 =>
                        $"将按内切圆半径 {RoundCoordinate(InscribedCircleRadius)} 对称生成 {CalculateInscribedCirclePointCount()} 个圆心点。",
                    CircleTrajectoryGenerateMode.等间距点 =>
                        $"将以圆心为起点，按间距 {RoundCoordinate(PointSpacing)} 在圆内生成等间距点。",
                    CircleTrajectoryGenerateMode.内切正方形点 =>
                        $"将在边长 {RoundCoordinate(GetInscribedSquareSideLength())} 的内切正方形内按间距 {RoundCoordinate(PointSpacing)} 生成 {CalculateInscribedSquarePointCount()} 个点。",
                    CircleTrajectoryGenerateMode.内接同心圆点 =>
                        $"将按间距 {RoundCoordinate(PointSpacing)} 生成 {CalculateInscribedConcentricCircleRingCount()} 圈内接同心圆点，共 {CalculateInscribedConcentricCirclePointCount()} 个点。",
                    CircleTrajectoryGenerateMode.圆环点 =>
                        $"将在内圆半径 {RoundCoordinate(InscribedCircleRadius)} 与外圆半径 {RoundCoordinate(CircleRadius)} 之间按间距 {RoundCoordinate(PointSpacing)} 生成 {CalculateAnnularRingCount()} 圈圆环点，共 {CalculateAnnularPointCount()} 个点。",
                    CircleTrajectoryGenerateMode.自定义圆环 =>
                        $"已配置 {GetCustomRingRadii().Count} 个自定义圆环，将按角度间距 {RoundCoordinate(CustomRingAngleSpacing)} 度生成 {CalculateCustomRingPointCount()} 个点。",
                    _ => "准备生成圆形轨迹。"
                };
            }
        }
        #endregion

        #region Methods
        public List<LocusInfo> GenerateCircleLocusInfos(Func<TransmitParam, object?>? inputValueResolver = null, double sensorInterval = 0)
        {
            SyncCircleCenterFromInput(inputValueResolver);
            return BuildCircleLocusInfos(sensorInterval);
        }

        public CustomRingDefinition AddCustomRing(double radius)
        {
            double normalizedRadius = RoundCoordinate(radius);
            if (!double.IsFinite(normalizedRadius) || normalizedRadius <= 0)
            {
                throw new InvalidOperationException("圆环半径必须大于 0。");
            }

            if (!double.IsFinite(CircleRadius) || CircleRadius <= 0)
            {
                throw new InvalidOperationException("请先输入有效的外圆半径。");
            }

            if (normalizedRadius > CircleRadius)
            {
                throw new InvalidOperationException("圆环半径不能大于外圆半径。");
            }

            var customRing = new CustomRingDefinition
            {
                Radius = normalizedRadius
            };

            CustomRingDefinitions.Add(customRing);
            CustomRingRadiusInput = normalizedRadius;
            return customRing;
        }

        public bool RemoveCustomRing(CustomRingDefinition? customRing)
        {
            if (customRing == null)
            {
                return false;
            }

            return CustomRingDefinitions.Remove(customRing);
        }

        public void SyncCircleCenterFromInput(Func<TransmitParam, object?>? inputValueResolver)
        {
            // 检查是否有有效的链接参数和解析器
            if (inputValueResolver == null)
            {
                return;
            }

            // 检查链接参数是否配置且有效
            bool hasValidLink = CircleCenterActualXYLinkParam != null &&
                               !string.IsNullOrEmpty(CircleCenterActualXYLinkParam.ParamName);

            if (!hasValidLink)
            {
                // 没有链接时，使用手动输入的圆心坐标（保持现有值）
                return;
            }

            // 有链接时，尝试从链接获取坐标
            object? linkedValue = inputValueResolver(CircleCenterActualXYLinkParam);
            if (!TryReadCoordinatePair(linkedValue, out double linkedX, out double linkedY))
            {
                // 链接无效时，保持手动输入的坐标
                return;
            }

            // 链接有效，使用链接的坐标
            CircleCenterX = linkedX;
            CircleCenterY = linkedY;
        }

        private List<LocusInfo> BuildCircleLocusInfos(double sensorInterval = 0)
        {
            ValidateCircleGenerationParams();

            return CircleGenerateMode switch
            {
                CircleTrajectoryGenerateMode.水平线 => BuildParallelCircleLocusInfos(isHorizontal: true, sensorInterval),
                CircleTrajectoryGenerateMode.垂直线 => BuildParallelCircleLocusInfos(isHorizontal: false, sensorInterval),
                CircleTrajectoryGenerateMode.过圆心交叉线 => BuildCenterCrossCircleLocusInfos(sensorInterval),
                CircleTrajectoryGenerateMode.内切圆心点 => BuildInscribedCircleCenterPointLocusInfos(),
                CircleTrajectoryGenerateMode.等间距点 => BuildEquidistantPointLocusInfos(),
                CircleTrajectoryGenerateMode.内切正方形点 => BuildInscribedSquarePointLocusInfos(),
                CircleTrajectoryGenerateMode.内接同心圆点 => BuildInscribedConcentricCirclePointLocusInfos(),
                CircleTrajectoryGenerateMode.圆环点 => BuildAnnularPointLocusInfos(),
                CircleTrajectoryGenerateMode.自定义圆环 => BuildCustomRingPointLocusInfos(),
                _ => throw new InvalidOperationException("不支持的圆形轨迹生成模式。")
            };
        }

        /// <summary>
        /// 垂直/水平线
        /// </summary>
        /// <param name="isHorizontal"></param>
        /// <param name="sensorInterval"></param>
        /// <returns></returns>
        private List<LocusInfo> BuildParallelCircleLocusInfos(bool isHorizontal, double sensorInterval = 0)
        {
            int lineCount = NormalizeCircleLineCount();
            var locusInfos = new List<LocusInfo>(lineCount);

            foreach (double offset in GenerateCircleLineOffsets(lineCount, CircleRadius))
            {
                double halfSpan = Math.Sqrt(Math.Max(CircleRadius * CircleRadius - offset * offset, 0));

                double originX, originY, targetX, targetY;
                
                if (isHorizontal)
                {
                    originX = RoundCoordinate(CircleCenterX - halfSpan);
                    originY = RoundCoordinate(CircleCenterY + offset);
                    targetX = RoundCoordinate(CircleCenterX + halfSpan);
                    targetY = RoundCoordinate(CircleCenterY + offset);
                }
                else
                {
                    originX = RoundCoordinate(CircleCenterX + offset);
                    originY = RoundCoordinate(CircleCenterY - halfSpan);
                    targetX = RoundCoordinate(CircleCenterX + offset);
                    targetY = RoundCoordinate(CircleCenterY + halfSpan);
                }

                // 调整起点和终点
                (originX, originY, targetX, targetY) = AdjustTrajectoryEndpoints(
                    originX, originY, targetX, targetY, sensorInterval);

                locusInfos.Add(new LocusInfo
                {
                    Type = LocusInfo.LineType,
                    OriginX = originX,
                    OriginY = originY,
                    TargetX = targetX,
                    TargetY = targetY
                });
            }

            return locusInfos;
        }

        /// <summary>
        /// 过圆心的交叉线
        /// </summary>
        /// <param name="sensorInterval"></param>
        /// <returns></returns>
        private List<LocusInfo> BuildCenterCrossCircleLocusInfos(double sensorInterval = 0)
        {
            int lineCount = NormalizeCircleLineCount();
            var locusInfos = new List<LocusInfo>(lineCount);

            // 每条过圆心的直径线占用 180 度，所以总共 180 度范围内均匀分布
            double angleStep = Math.PI / lineCount;

            for (int i = 0; i < lineCount; i++)
            {
                double angle = angleStep * i;
                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                double originX = RoundCoordinate(CircleCenterX - CircleRadius * cosAngle);
                double originY = RoundCoordinate(CircleCenterY - CircleRadius * sinAngle);
                double targetX = RoundCoordinate(CircleCenterX + CircleRadius * cosAngle);
                double targetY = RoundCoordinate(CircleCenterY + CircleRadius * sinAngle);

                // 调整起点和终点
                (originX, originY, targetX, targetY) = AdjustTrajectoryEndpoints(
                    originX, originY, targetX, targetY, sensorInterval);

                locusInfos.Add(new LocusInfo
                {
                    Type = LocusInfo.LineType,
                    OriginX = originX,
                    OriginY = originY,
                    TargetX = targetX,
                    TargetY = targetY
                });
            }

            return locusInfos;
        }

        /// <summary>
        /// 沿圆心竖直方向生成等分内切圆的圆心点。
        /// </summary>
        private List<LocusInfo> BuildInscribedCircleCenterPointLocusInfos()
        {
            int pointCount = CalculateInscribedCirclePointCount();
            var locusInfos = new List<LocusInfo>(pointCount);
            double spacing = InscribedCircleRadius * 2;
            double startOffset = -spacing * (pointCount - 1) / 2.0;

            for (int i = 0; i < pointCount; i++)
            {
                double offset = startOffset + i * spacing;
                double pointX = RoundCoordinate(CircleCenterX);
                double pointY = RoundCoordinate(CircleCenterY + offset);

                locusInfos.Add(new LocusInfo
                {
                    Type = LocusInfo.PointType,
                    OriginX = pointX,
                    OriginY = pointY,
                    TargetX = pointX,
                    TargetY = pointY
                });
            }

            return locusInfos;
        }

        private int CalculateInscribedCirclePointCount()
        {
            ValidateInscribedCircleRadius();
            return Math.Max((int)Math.Floor(CircleRadius / InscribedCircleRadius), 1);
        }

        /// <summary>
        /// 以圆心为原点，按设定间距向四周扩展生成位于圆内的点阵。
        /// </summary>
        private List<LocusInfo> BuildEquidistantPointLocusInfos()
        {
            double spacing = NormalizePointSpacing();
            var gridOffsets = GenerateEquidistantPointOffsets(spacing);
            var locusInfos = new List<LocusInfo>(gridOffsets.Count);

            foreach (var (gridX, gridY) in gridOffsets)
            {
                double pointX = RoundCoordinate(CircleCenterX + gridX * spacing);
                double pointY = RoundCoordinate(CircleCenterY + gridY * spacing);

                locusInfos.Add(new LocusInfo
                {
                    Type = LocusInfo.PointType,
                    OriginX = pointX,
                    OriginY = pointY,
                    TargetX = pointX,
                    TargetY = pointY
                });
            }

            return locusInfos;
        }

        /// <summary>
        /// 在圆的内切正方形区域内生成等间距点阵。
        /// </summary>
        private List<LocusInfo> BuildInscribedSquarePointLocusInfos()
        {
            double spacing = NormalizePointSpacing();
            var gridOffsets = GenerateInscribedSquarePointOffsets(spacing);
            var locusInfos = new List<LocusInfo>(gridOffsets.Count);

            foreach (var (gridX, gridY) in gridOffsets)
            {
                double pointX = RoundCoordinate(CircleCenterX + gridX * spacing);
                double pointY = RoundCoordinate(CircleCenterY + gridY * spacing);

                locusInfos.Add(new LocusInfo
                {
                    Type = LocusInfo.PointType,
                    OriginX = pointX,
                    OriginY = pointY,
                    TargetX = pointX,
                    TargetY = pointY
                });
            }

            return locusInfos;
        }

        private int CalculateInscribedSquarePointCount()
        {
            double spacing = NormalizePointSpacing();
            double halfSide = GetInscribedSquareHalfSideLength();
            int gridLimit = Math.Max((int)Math.Floor((halfSide / spacing) + 1e-9), 0);
            int pointsPerAxis = gridLimit * 2 + 1;
            return pointsPerAxis * pointsPerAxis;
        }

        /// <summary>
        /// 按同心圆层间距生成点，每一圈内部按等角度均匀分布。
        /// </summary>
        private List<LocusInfo> BuildInscribedConcentricCirclePointLocusInfos()
        {
            double spacing = NormalizePointSpacing();
            var ringRadii = GenerateConcentricCircleRadii(spacing);
            var locusInfos = new List<LocusInfo>(CalculateInscribedConcentricCirclePointCount());

            AddPointLocusInfo(locusInfos, CircleCenterX, CircleCenterY);

            foreach (double radius in ringRadii)
            {
                AddConcentricRingPoints(locusInfos, radius, spacing);
            }

            return locusInfos;
        }

        private int CalculateInscribedConcentricCircleRingCount()
        {
            double spacing = NormalizePointSpacing();
            return GenerateConcentricCircleRadii(spacing).Count;
        }

        private int CalculateInscribedConcentricCirclePointCount()
        {
            double spacing = NormalizePointSpacing();
            int pointCount = 1;

            foreach (double radius in GenerateConcentricCircleRadii(spacing))
            {
                pointCount += CalculateConcentricRingPointCount(radius, spacing);
            }

            return pointCount;
        }

        /// <summary>
        /// 在内圆与外圆之间按等间距生成圆环点。
        /// </summary>
        private List<LocusInfo> BuildAnnularPointLocusInfos()
        {
            double spacing = NormalizePointSpacing();
            var ringRadii = GenerateAnnularRingRadii(spacing);
            var locusInfos = new List<LocusInfo>(CalculateAnnularPointCount());

            foreach (double radius in ringRadii)
            {
                AddConcentricRingPoints(locusInfos, radius, spacing);
            }

            return locusInfos;
        }

        private int CalculateAnnularRingCount()
        {
            double spacing = NormalizePointSpacing();
            return GenerateAnnularRingRadii(spacing).Count;
        }

        private int CalculateAnnularPointCount()
        {
            double spacing = NormalizePointSpacing();
            int pointCount = 0;

            foreach (double radius in GenerateAnnularRingRadii(spacing))
            {
                pointCount += CalculateConcentricRingPointCount(radius, spacing);
            }

            return pointCount;
        }

        /// <summary>
        /// 按用户手动添加的圆环半径和角度间距生成点。
        /// </summary>
        private List<LocusInfo> BuildCustomRingPointLocusInfos()
        {
            double angleSpacing = NormalizeCustomRingAngleSpacing();
            var ringRadii = GetValidatedCustomRingRadii();
            var locusInfos = new List<LocusInfo>(CalculateCustomRingPointCount(ringRadii, angleSpacing));

            foreach (double radius in ringRadii)
            {
                AddCustomRingPoints(locusInfos, radius, angleSpacing);
            }

            return locusInfos;
        }

        private int CalculateCustomRingPointCount()
        {
            double angleSpacing = NormalizeCustomRingAngleSpacing();
            return CalculateCustomRingPointCount(GetValidatedCustomRingRadii(), angleSpacing);
        }

        private static int CalculateCustomRingPointCount(IReadOnlyCollection<double> ringRadii, double angleSpacing)
        {
            return ringRadii.Count * CalculateCustomRingPointCountPerRing(angleSpacing);
        }

        private List<(int GridX, int GridY)> GenerateEquidistantPointOffsets(double spacing)
        {
            var offsets = new List<(int GridX, int GridY)>();
            var queue = new Queue<(int GridX, int GridY)>();
            var visited = new HashSet<(int GridX, int GridY)>();
            var directions = new (int DeltaX, int DeltaY)[]
            {
                (0, 1),
                (0, -1),
                (-1, 0),
                (1, 0)
            };

            double radiusSquared = CircleRadius * CircleRadius;
            queue.Enqueue((0, 0));
            visited.Add((0, 0));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                offsets.Add(current);

                foreach (var (deltaX, deltaY) in directions)
                {
                    var next = (GridX: current.GridX + deltaX, GridY: current.GridY + deltaY);
                    if (visited.Contains(next) || !IsGridPointInsideCircle(next.GridX, next.GridY, spacing, radiusSquared))
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return offsets;
        }

        private List<(int GridX, int GridY)> GenerateInscribedSquarePointOffsets(double spacing)
        {
            double halfSide = GetInscribedSquareHalfSideLength();
            int gridLimit = Math.Max((int)Math.Floor((halfSide / spacing) + 1e-9), 0);
            int pointsPerAxis = gridLimit * 2 + 1;
            var offsets = new List<(int GridX, int GridY)>(pointsPerAxis * pointsPerAxis);

            for (int gridY = gridLimit; gridY >= -gridLimit; gridY--)
            {
                for (int gridX = -gridLimit; gridX <= gridLimit; gridX++)
                {
                    offsets.Add((gridX, gridY));
                }
            }

            return offsets;
        }

        private List<double> GenerateConcentricCircleRadii(double spacing)
        {
            var radii = new List<double>();
            for (double radius = spacing; radius <= CircleRadius + 1e-9; radius += spacing)
            {
                radii.Add(radius);
            }

            return radii;
        }

        private List<double> GenerateAnnularRingRadii(double spacing)
        {
            ValidateInscribedCircleRadius();

            var radii = new List<double>();
            for (double radius = InscribedCircleRadius; radius <= CircleRadius + 1e-9; radius += spacing)
            {
                radii.Add(radius);
            }

            return radii;
        }

        private void AddConcentricRingPoints(ICollection<LocusInfo> locusInfos, double radius, double spacing)
        {
            int pointCount = CalculateConcentricRingPointCount(radius, spacing);
            double angleStep = 2 * Math.PI / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                double angle = angleStep * i;
                double pointX = CircleCenterX + radius * Math.Cos(angle);
                double pointY = CircleCenterY + radius * Math.Sin(angle);
                AddPointLocusInfo(locusInfos, pointX, pointY);
            }
        }

        private void AddCustomRingPoints(ICollection<LocusInfo> locusInfos, double radius, double angleSpacing)
        {
            int pointCount = CalculateCustomRingPointCountPerRing(angleSpacing);
            double angleStep = 2 * Math.PI / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                double angle = angleStep * i;
                double pointX = CircleCenterX + radius * Math.Cos(angle);
                double pointY = CircleCenterY + radius * Math.Sin(angle);
                AddPointLocusInfo(locusInfos, pointX, pointY);
            }
        }

        private static int CalculateConcentricRingPointCount(double radius, double spacing)
        {
            if (radius <= 0)
            {
                return 1;
            }

            double ratio = Math.Min(spacing / (2 * radius), 1);
            double angle = Math.Asin(ratio);
            if (angle <= 0)
            {
                return 6;
            }

            return Math.Max((int)Math.Round(Math.PI / angle, MidpointRounding.AwayFromZero), 6);
        }

        private static int CalculateCustomRingPointCountPerRing(double angleSpacing)
        {
            if (angleSpacing <= 0)
            {
                return 1;
            }

            return Math.Max((int)Math.Round(360d / angleSpacing, MidpointRounding.AwayFromZero), 1);
        }

        private static bool IsGridPointInsideCircle(int gridX, int gridY, double spacing, double radiusSquared)
        {
            double offsetX = gridX * spacing;
            double offsetY = gridY * spacing;
            double distanceSquared = offsetX * offsetX + offsetY * offsetY;
            return distanceSquared <= radiusSquared + 1e-9;
        }

        private void AddPointLocusInfo(ICollection<LocusInfo> locusInfos, double pointX, double pointY)
        {
            double roundedX = RoundCoordinate(pointX);
            double roundedY = RoundCoordinate(pointY);

            locusInfos.Add(new LocusInfo
            {
                Type = LocusInfo.PointType,
                OriginX = roundedX,
                OriginY = roundedY,
                TargetX = roundedX,
                TargetY = roundedY
            });
        }

        private double GetInscribedSquareHalfSideLength()
        {
            return CircleRadius / Math.Sqrt(2);
        }

        private double GetInscribedSquareSideLength()
        {
            return GetInscribedSquareHalfSideLength() * 2;
        }


        private static IEnumerable<double> GenerateCircleLineOffsets(int lineCount, double radius)
        {
            if (lineCount <= 1)
            {
                yield return 0;
                yield break;
            }

            double step = radius * 2 / lineCount;
            for (int index = 0; index < lineCount; index++)
            {
                yield return -radius + step * (index + 0.5);
            }
        }

        /// <summary>
        /// 调整轨迹的起点和终点
        /// 起点往后按照 SensorInterval 多走一步，终点往前多走一步
        /// </summary>
        /// <param name="originX">起点 X</param>
        /// <param name="originY">起点 Y</param>
        /// <param name="targetX">终点 X</param>
        /// <param name="targetY">终点 Y</param>
        /// <param name="sensorInterval">传感器间隔</param>
        /// <returns>调整后的起点和终点坐标</returns>
        private static (double originX, double originY, double targetX, double targetY) AdjustTrajectoryEndpoints(
            double originX, double originY, double targetX, double targetY, double sensorInterval)
        {
            if (sensorInterval <= 0)
            {
                return (originX, originY, targetX, targetY);
            }

            double dx = targetX - originX;
            double dy = targetY - originY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 1e-10)
            {
                return (originX, originY, targetX, targetY);
            }

            // 单位方向向量
            double ux = dx / distance;
            double uy = dy / distance;

            // 起点往后多走一步
            double newOriginX = originX - ux * sensorInterval;
            double newOriginY = originY - uy * sensorInterval;

            // 终点往前多走一步
            double newTargetX = targetX + ux * sensorInterval;
            double newTargetY = targetY + uy * sensorInterval;

            return (newOriginX, newOriginY, newTargetX, newTargetY);
        }

        private void ValidateCircleGenerationParams()
        {
            if (!double.IsFinite(CircleCenterX) || !double.IsFinite(CircleCenterY))
            {
                throw new InvalidOperationException("圆心坐标必须是有效数值。");
            }

            if (!double.IsFinite(CircleRadius) || CircleRadius <= 0)
            {
                throw new InvalidOperationException("圆半径必须大于 0。");
            }

            if (IsInscribedCircleRadiusEditable)
            {
                ValidateInscribedCircleRadius();
            }

            if (IsPointSpacingEditable)
            {
                ValidatePointSpacing();
            }

            if (IsCustomRingAngleSpacingEditable)
            {
                ValidateCustomRingAngleSpacing();
            }

            if (CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环)
            {
                ValidateCustomRingDefinitions();
            }
        }

        private void ValidateInscribedCircleRadius()
        {
            if (!double.IsFinite(InscribedCircleRadius) || InscribedCircleRadius <= 0)
            {
                throw new InvalidOperationException($"{GetInscribedRadiusDisplayName()}必须大于 0。");
            }

            if (InscribedCircleRadius > CircleRadius)
            {
                throw new InvalidOperationException($"{GetInscribedRadiusDisplayName()}不能大于外圆半径。");
            }
        }

        private void ValidatePointSpacing()
        {
            if (!double.IsFinite(PointSpacing) || PointSpacing <= 0)
            {
                throw new InvalidOperationException("点间距必须大于 0。");
            }
        }

        private void ValidateCustomRingAngleSpacing()
        {
            if (!double.IsFinite(CustomRingAngleSpacing) || CustomRingAngleSpacing <= 0 || CustomRingAngleSpacing > 360)
            {
                throw new InvalidOperationException("角度间距必须大于 0 且不超过 360。");
            }
        }

        private void ValidateCustomRingDefinitions()
        {
            if (CustomRingDefinitions == null || CustomRingDefinitions.Count == 0)
            {
                throw new InvalidOperationException("请至少添加一个自定义圆环。");
            }

            foreach (double radius in GetCustomRingRadii())
            {
                if (!double.IsFinite(radius) || radius <= 0)
                {
                    throw new InvalidOperationException("自定义圆环半径必须大于 0。");
                }

                if (radius > CircleRadius)
                {
                    throw new InvalidOperationException("自定义圆环半径不能大于外圆半径。");
                }
            }
        }

        private int NormalizeCircleLineCount()
        {
            if (!double.IsFinite(CircleLineCount))
            {
                return 1;
            }

            return Math.Max((int)Math.Round(CircleLineCount, MidpointRounding.AwayFromZero), 1);
        }

        private double NormalizePointSpacing()
        {
            ValidatePointSpacing();
            return PointSpacing;
        }

        private double NormalizeCustomRingAngleSpacing()
        {
            ValidateCustomRingAngleSpacing();
            return CustomRingAngleSpacing;
        }

        private List<double> GetCustomRingRadii()
        {
            return CustomRingDefinitions?
                .Where(item => item != null)
                .Select(item => item.Radius)
                .ToList() ?? new List<double>();
        }

        private List<double> GetValidatedCustomRingRadii()
        {
            ValidateCustomRingDefinitions();
            return GetCustomRingRadii();
        }

        private double RoundCoordinate(double value)
        {
            if (DecimalPlaces < 0)
            {
                return value;
            }

            return Math.Round(value, DecimalPlaces);
        }

        private string GetInscribedRadiusDisplayName()
        {
            return CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点 ? "内圆半径" : "内切圆半径";
        }

        private void OnCustomRingDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
        }

        private static bool TryReadCoordinatePair(object? source, out double x, out double y)
        {
            x = 0;
            y = 0;

            if (source == null || source is string)
            {
                return false;
            }

            if (source is IEnumerable enumerable)
            {
                List<double> values = new List<double>(2);
                foreach (object item in enumerable)
                {
                    if (!TryConvertToDouble(item, out double currentValue))
                    {
                        continue;
                    }

                    values.Add(currentValue);
                    if (values.Count >= 2)
                    {
                        x = values[0];
                        y = values[1];
                        return true;
                    }
                }
            }

            return TryReadDoubleMember(source, "X", out x) && TryReadDoubleMember(source, "Y", out y);
        }

        private static bool TryReadDoubleMember(object source, string memberName, out double value)
        {
            value = 0;
            object? memberValue = GetMemberValue(source, memberName);
            if (memberValue == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToDouble(memberValue, CultureInfo.InvariantCulture);
                return double.IsFinite(value);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertToDouble(object source, out double value)
        {
            value = 0;
            if (source == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToDouble(source, CultureInfo.InvariantCulture);
                return double.IsFinite(value);
            }
            catch
            {
                return false;
            }
        }

        private static object? GetMemberValue(object source, string memberName)
        {
            var sourceType = source.GetType();
            var property = sourceType.GetProperty(memberName);
            if (property != null)
            {
                return property.GetValue(source);
            }

            var field = sourceType.GetField(memberName);
            return field?.GetValue(source);
        }
        #endregion
    }

    /// <summary>
    /// 圆形轨迹的生成模式；线段模式用于连续扫描，点位模式用于逐点采集。
    /// </summary>
    [Serializable]
    public enum CircleTrajectoryGenerateMode
    {
        水平线,
        垂直线,
        过圆心交叉线,
        内切圆心点,
        等间距点,
        内切正方形点,
        内接同心圆点,
        圆环点,
        自定义圆环
    }
}

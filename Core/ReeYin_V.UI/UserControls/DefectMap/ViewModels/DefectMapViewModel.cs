#nullable enable

using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Titles;
using Arction.Wpf.ChartingMVVM.Views.ViewXY;
using Prism.Mvvm;
using PrismDelegateCommand = Prism.Commands.DelegateCommand;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed record DefectMapPointVisual(
        DefectMapItem Item,
        double ChartX,
        double ChartY,
        double DisplaySize,
        Color DisplayColor,
        DefectMapMarkerShape MarkerShape,
        string DisplayTypeName);

    public sealed class DefectMapViewModel : BindableBase
    {
        private const string DefaultMapTitle = "Defect Map";
        private const double HitToleranceRatio = 0.015d;

        private readonly record struct DefectMapResolvedStyle(
            Color Color,
            double Size,
            DefectMapMarkerShape Shape,
            string DisplayName);

        private readonly record struct DefectMapVisualStyleKey(
            Color Color,
            double Size,
            DefectMapMarkerShape Shape);

        private readonly List<DefectMapItem> _defects = new();
        private readonly HashSet<DefectMapItem> _subscribedDefects = new();
        private readonly ObservableCollection<DefectMapTypeStyle> _ownedTypeStyleSource = new(
            DefectMapTypeStyle.CreateDefaultStyles().Select(style => style.Clone()));
        private readonly HashSet<DefectMapTypeStyle> _subscribedTypeStyles = new();
        private IEnumerable<DefectMapItem>? _defectSource;
        private INotifyCollectionChanged? _notifyingDefectSource;
        private IEnumerable<DefectMapTypeStyle>? _typeStyleSource;
        private INotifyCollectionChanged? _notifyingTypeStyleSource;
        private IList<DefectMapTypeStyle>? _typeStyleListSource;
        private IReadOnlyList<DefectMapPointVisual> _visiblePoints = Array.Empty<DefectMapPointVisual>();
        private DefectMapAppearance _appearance = DefectMapAppearance.Default;
        private AxisXCollection _chartXAxes = new();
        private AxisYCollection _chartYAxes = new();
        private FreeformPointLineSeriesCollection _defectSeries = new();
        private string _mapTitle = DefaultMapTitle;
        private string _summaryText = string.Empty;
        private string _selectedDefectText = "No defect selected.";
        private string _tooltipText = string.Empty;
        private Thickness _tooltipMargin;
        private Visibility _tooltipVisibility = Visibility.Collapsed;
        private DefectMapItem? _selectedDefect;
        private double _materialWidth = 1d;
        private double _materialLength = 1d;
        private DefectMapLengthOrigin _lengthOrigin = DefectMapLengthOrigin.Top;
        private int _outOfRangeCount;
        private int _newTypeIndex;
        private DefectMapTypeStyle? _selectedTypeStyle;

        public DefectMapViewModel()
        {
            AddTypeStyleCommand = new PrismDelegateCommand(AddTypeStyle);
            RemoveTypeStyleCommand = new PrismDelegateCommand(RemoveTypeStyle, CanRemoveTypeStyle);
            ResetTypeStylesCommand = new PrismDelegateCommand(ResetTypeStyles);
            SetDefectTypeStyles(null);
            RebuildChart();
        }

        public AxisXCollection ChartXAxes
        {
            get => _chartXAxes;
            private set => SetProperty(ref _chartXAxes, value);
        }

        public AxisYCollection ChartYAxes
        {
            get => _chartYAxes;
            private set => SetProperty(ref _chartYAxes, value);
        }

        public FreeformPointLineSeriesCollection DefectSeries
        {
            get => _defectSeries;
            private set => SetProperty(ref _defectSeries, value);
        }

        public IReadOnlyList<DefectMapPointVisual> VisiblePoints => _visiblePoints;

        public ObservableCollection<DefectMapTypeStyle> DefectTypeStyles { get; } = new();

        public DefectMapMarkerShape[] MarkerShapeOptions { get; } =
        {
            DefectMapMarkerShape.Circle,
            DefectMapMarkerShape.Rectangle,
            DefectMapMarkerShape.Triangle,
            DefectMapMarkerShape.Cross,
            DefectMapMarkerShape.Flag,
            DefectMapMarkerShape.FlagLightning
        };

        public DefectMapSeverity[] SeverityOptions { get; } =
        {
            DefectMapSeverity.Minor,
            DefectMapSeverity.Warning,
            DefectMapSeverity.Critical
        };

        public string MapTitle
        {
            get => _mapTitle;
            private set => SetProperty(ref _mapTitle, value);
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        public string SelectedDefectText
        {
            get => _selectedDefectText;
            private set => SetProperty(ref _selectedDefectText, value);
        }

        public string TooltipText
        {
            get => _tooltipText;
            private set => SetProperty(ref _tooltipText, value);
        }

        public Thickness TooltipMargin
        {
            get => _tooltipMargin;
            private set => SetProperty(ref _tooltipMargin, value);
        }

        public Visibility TooltipVisibility
        {
            get => _tooltipVisibility;
            private set => SetProperty(ref _tooltipVisibility, value);
        }

        public DefectMapItem? SelectedDefect
        {
            get => _selectedDefect;
            private set => SetProperty(ref _selectedDefect, value);
        }

        public DefectMapTypeStyle? SelectedTypeStyle
        {
            get => _selectedTypeStyle;
            set
            {
                if (SetProperty(ref _selectedTypeStyle, value))
                {
                    RemoveTypeStyleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int TotalCount => _defects.Count;

        public int VisibleCount => _visiblePoints.Count;

        public int OutOfRangeCount => _outOfRangeCount;

        public double MaterialWidth => _materialWidth;

        public double MaterialLength => _materialLength;

        public DefectMapLengthOrigin LengthOrigin => _lengthOrigin;

        public PrismDelegateCommand AddTypeStyleCommand { get; }

        public PrismDelegateCommand RemoveTypeStyleCommand { get; }

        public PrismDelegateCommand ResetTypeStylesCommand { get; }

        public void SetMapTitle(string? mapTitle)
        {
            MapTitle = string.IsNullOrWhiteSpace(mapTitle)
                ? DefaultMapTitle
                : mapTitle.Trim();
        }

        public void SetMaterialSize(double materialWidth, double materialLength)
        {
            double normalizedWidth = NormalizeDimension(materialWidth);
            double normalizedLength = NormalizeDimension(materialLength);
            bool widthChanged = Math.Abs(_materialWidth - normalizedWidth) > 1e-9d;
            bool lengthChanged = Math.Abs(_materialLength - normalizedLength) > 1e-9d;

            if (!widthChanged && !lengthChanged)
            {
                return;
            }

            _materialWidth = normalizedWidth;
            _materialLength = normalizedLength;
            RaisePropertyChanged(nameof(MaterialWidth));
            RaisePropertyChanged(nameof(MaterialLength));
            RebuildChart();
        }

        public void SetLengthOrigin(DefectMapLengthOrigin lengthOrigin)
        {
            if (_lengthOrigin == lengthOrigin)
            {
                return;
            }

            _lengthOrigin = lengthOrigin;
            RaisePropertyChanged(nameof(LengthOrigin));
            RebuildChart();
        }

        public void SetDefects(IEnumerable<DefectMapItem>? defects)
        {
            DetachDefectSource();
            _defectSource = defects;
            _notifyingDefectSource = defects as INotifyCollectionChanged;
            if (_notifyingDefectSource != null)
            {
                _notifyingDefectSource.CollectionChanged += OnDefectCollectionChanged;
            }

            RefreshTrackedDefects();
            ClearInvalidSelection();
            RebuildChart();
        }

        public void SetDefectTypeStyles(IEnumerable<DefectMapTypeStyle>? typeStyles)
        {
            DetachTypeStyleSource();

            _typeStyleSource = typeStyles ?? _ownedTypeStyleSource;
            _typeStyleListSource = _typeStyleSource as IList<DefectMapTypeStyle>;
            _notifyingTypeStyleSource = _typeStyleSource as INotifyCollectionChanged;
            if (_notifyingTypeStyleSource != null)
            {
                _notifyingTypeStyleSource.CollectionChanged += OnTypeStyleCollectionChanged;
            }

            RefreshTrackedTypeStyles();
            if (SelectedTypeStyle == null || !DefectTypeStyles.Contains(SelectedTypeStyle))
            {
                SelectedTypeStyle = DefectTypeStyles.FirstOrDefault();
            }

            RebuildChart();
        }

        public void SetSelectedDefect(DefectMapItem? defect)
        {
            DefectMapItem? knownDefect = defect == null ? null : ResolveKnownDefect(defect);
            bool isSameSelection = (_selectedDefect == null && knownDefect == null) ||
                                   (_selectedDefect != null &&
                                    knownDefect != null &&
                                    IsSameDefect(_selectedDefect, knownDefect));

            if (isSameSelection)
            {
                if (!ReferenceEquals(_selectedDefect, knownDefect))
                {
                    SelectedDefect = knownDefect;
                }

                UpdateSelectedText();
                return;
            }

            SelectedDefect = knownDefect;
            UpdateSelectedText();
            RebuildSeries();
        }

        public void RebuildChart()
        {
            RebuildVisiblePoints();
            RebuildAxes();
            RebuildSeries();
            UpdateSelectedText();
            UpdateSummary();
        }

        public static double NormalizeDimension(double value)
        {
            return double.IsFinite(value) && value > 0d ? value : 1d;
        }

        private void OnDefectCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTrackedDefects();
            ClearInvalidSelection();
            RebuildChart();
        }

        private void OnDefectItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DefectMapItem)
            {
                return;
            }

            RebuildChart();
        }

        private void OnTypeStyleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTrackedTypeStyles();
            if (SelectedTypeStyle == null || !DefectTypeStyles.Contains(SelectedTypeStyle))
            {
                SelectedTypeStyle = DefectTypeStyles.FirstOrDefault();
            }

            RebuildChart();
        }

        private void OnTypeStylePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DefectMapTypeStyle)
            {
                return;
            }

            RebuildChart();
        }

        private void AddTypeStyle()
        {
            DefectMapTypeStyle newStyle = CreateNextTypeStyle();
            bool sourceNotifies = _notifyingTypeStyleSource != null;

            if (_typeStyleListSource != null)
            {
                _typeStyleListSource.Add(newStyle);
            }
            else
            {
                DefectTypeStyles.Add(newStyle);
            }

            if (!sourceNotifies)
            {
                RefreshTrackedTypeStyles();
                RebuildChart();
            }

            SelectedTypeStyle = newStyle;
        }

        private bool CanRemoveTypeStyle()
        {
            return SelectedTypeStyle != null && DefectTypeStyles.Count > 1;
        }

        private void RemoveTypeStyle()
        {
            DefectMapTypeStyle? style = SelectedTypeStyle;
            if (style == null || DefectTypeStyles.Count <= 1)
            {
                return;
            }

            bool sourceNotifies = _notifyingTypeStyleSource != null;
            if (_typeStyleListSource != null)
            {
                _typeStyleListSource.Remove(style);
            }
            else
            {
                DefectTypeStyles.Remove(style);
            }

            if (!sourceNotifies)
            {
                RefreshTrackedTypeStyles();
                RebuildChart();
            }

            SelectedTypeStyle = DefectTypeStyles.FirstOrDefault();
        }

        private void ResetTypeStyles()
        {
            bool sourceNotifies = _notifyingTypeStyleSource != null;
            IList<DefectMapTypeStyle> target = _typeStyleListSource ?? DefectTypeStyles;
            target.Clear();
            foreach (DefectMapTypeStyle style in DefectMapTypeStyle.CreateDefaultStyles().Select(style => style.Clone()))
            {
                target.Add(style);
            }

            if (!sourceNotifies)
            {
                RefreshTrackedTypeStyles();
                RebuildChart();
            }

            SelectedTypeStyle = DefectTypeStyles.FirstOrDefault();
        }

        private void RefreshTrackedDefects()
        {
            foreach (DefectMapItem defect in _subscribedDefects)
            {
                defect.PropertyChanged -= OnDefectItemPropertyChanged;
            }

            _subscribedDefects.Clear();
            _defects.Clear();

            if (_defectSource != null)
            {
                foreach (DefectMapItem defect in _defectSource)
                {
                    if (defect == null)
                    {
                        continue;
                    }

                    _defects.Add(defect);
                    if (_subscribedDefects.Add(defect))
                    {
                        defect.PropertyChanged += OnDefectItemPropertyChanged;
                    }
                }
            }

            RaisePropertyChanged(nameof(TotalCount));
        }

        private void DetachDefectSource()
        {
            if (_notifyingDefectSource != null)
            {
                _notifyingDefectSource.CollectionChanged -= OnDefectCollectionChanged;
                _notifyingDefectSource = null;
            }

            foreach (DefectMapItem defect in _subscribedDefects)
            {
                defect.PropertyChanged -= OnDefectItemPropertyChanged;
            }

            _subscribedDefects.Clear();
            _defects.Clear();
            _defectSource = null;
        }

        private void RefreshTrackedTypeStyles()
        {
            foreach (DefectMapTypeStyle style in _subscribedTypeStyles)
            {
                style.PropertyChanged -= OnTypeStylePropertyChanged;
            }

            _subscribedTypeStyles.Clear();
            DefectTypeStyles.Clear();

            if (_typeStyleSource != null)
            {
                foreach (DefectMapTypeStyle style in _typeStyleSource)
                {
                    if (style == null)
                    {
                        continue;
                    }

                    DefectTypeStyles.Add(style);
                    if (_subscribedTypeStyles.Add(style))
                    {
                        style.PropertyChanged += OnTypeStylePropertyChanged;
                    }
                }
            }

            RemoveTypeStyleCommand.RaiseCanExecuteChanged();
        }

        private void DetachTypeStyleSource()
        {
            if (_notifyingTypeStyleSource != null)
            {
                _notifyingTypeStyleSource.CollectionChanged -= OnTypeStyleCollectionChanged;
                _notifyingTypeStyleSource = null;
            }

            foreach (DefectMapTypeStyle style in _subscribedTypeStyles)
            {
                style.PropertyChanged -= OnTypeStylePropertyChanged;
            }

            _subscribedTypeStyles.Clear();
            DefectTypeStyles.Clear();
            _typeStyleSource = null;
            _typeStyleListSource = null;
        }

        private DefectMapTypeStyle CreateNextTypeStyle()
        {
            _newTypeIndex++;
            string typeKey = $"Custom{_newTypeIndex:D2}";
            while (DefectTypeStyles.Any(style => DefectMapTypeStyle.IsSameTypeKey(style.TypeKey, typeKey)))
            {
                _newTypeIndex++;
                typeKey = $"Custom{_newTypeIndex:D2}";
            }

            return DefectMapTypeStyle.CreateDefault(
                typeKey,
                $"Custom Defect {_newTypeIndex:D2}",
                "#0EA5E9",
                10d,
                DefectMapMarkerShape.Circle,
                DefectMapSeverity.Minor,
                "User defined defect type");
        }

        private void ClearInvalidSelection()
        {
            if (_selectedDefect == null)
            {
                return;
            }

            DefectMapItem? knownDefect = ResolveKnownDefect(_selectedDefect);
            if (!ReferenceEquals(_selectedDefect, knownDefect))
            {
                SelectedDefect = knownDefect;
            }
        }

        private void RebuildVisiblePoints()
        {
            List<DefectMapPointVisual> visiblePoints = new();
            int outOfRangeCount = 0;

            foreach (DefectMapItem item in _defects)
            {
                if (!IsInMaterial(item))
                {
                    outOfRangeCount++;
                    continue;
                }

                double chartX = item.WidthPosition;
                double chartY = ToChartLength(item);
                DefectMapResolvedStyle visualStyle = ResolveVisualStyle(item);
                double displaySize = double.IsFinite(item.DisplaySize.GetValueOrDefault()) && item.DisplaySize.GetValueOrDefault() > 0d
                    ? item.DisplaySize.GetValueOrDefault()
                    : visualStyle.Size;

                visiblePoints.Add(new DefectMapPointVisual(
                    item,
                    chartX,
                    chartY,
                    displaySize,
                    visualStyle.Color,
                    visualStyle.Shape,
                    visualStyle.DisplayName));
            }

            _visiblePoints = visiblePoints.ToArray();
            _outOfRangeCount = outOfRangeCount;
            RaisePropertyChanged(nameof(VisiblePoints));
            RaisePropertyChanged(nameof(VisibleCount));
            RaisePropertyChanged(nameof(OutOfRangeCount));
        }

        private bool IsInMaterial(DefectMapItem item)
        {
            return double.IsFinite(item.WidthPosition) &&
                   double.IsFinite(item.LengthPosition) &&
                   item.WidthPosition >= 0d &&
                   item.WidthPosition <= MaterialWidth &&
                   item.LengthPosition >= 0d &&
                   item.LengthPosition <= MaterialLength;
        }

        private double ToChartLength(DefectMapItem item)
        {
            return _lengthOrigin switch
            {
                DefectMapLengthOrigin.Top => MaterialLength - item.LengthPosition,
                DefectMapLengthOrigin.Bottom => item.LengthPosition,
                _ => item.LengthPosition
            };
        }

        private DefectMapResolvedStyle ResolveVisualStyle(DefectMapItem item)
        {
            DefectMapTypeStyle? typeStyle = DefectTypeStyles.FirstOrDefault(style =>
                style.IsEnabled &&
                DefectMapTypeStyle.IsSameTypeKey(style.TypeKey, item.DefectType));

            if (typeStyle != null)
            {
                return new DefectMapResolvedStyle(
                    typeStyle.MarkerColor,
                    typeStyle.MarkerSize,
                    typeStyle.MarkerShape,
                    typeStyle.DisplayName);
            }

            return new DefectMapResolvedStyle(
                _appearance.GetSeverityColor(item.Severity),
                _appearance.GetSeveritySize(item.Severity),
                DefectMapMarkerShape.Circle,
                item.DefectType);
        }

        private void UpdateSummary()
        {
            if (TotalCount == 0)
            {
                SummaryText = $"No defects. Material {MaterialWidth:F2} x {MaterialLength:F2}.";
                return;
            }

            int minorCount = _visiblePoints.Count(point => point.Item.Severity == DefectMapSeverity.Minor);
            int warningCount = _visiblePoints.Count(point => point.Item.Severity == DefectMapSeverity.Warning);
            int criticalCount = _visiblePoints.Count(point => point.Item.Severity == DefectMapSeverity.Critical);

            SummaryText =
                $"{VisibleCount}/{TotalCount} visible, {OutOfRangeCount} out of range. " +
                $"Minor {minorCount}, Warning {warningCount}, Critical {criticalCount}. " +
                $"Material {MaterialWidth:F2} x {MaterialLength:F2}.";
        }

        private string FormatSelectedText(DefectMapItem item)
        {
            string displayTypeName = GetDisplayTypeName(item);
            return
                $"{item.Name} | Type: {displayTypeName} | Severity: {item.Severity} | " +
                $"Width: {item.WidthPosition:F2} | Length: {item.LengthPosition:F2}";
        }

        private string GetDisplayTypeName(DefectMapItem item)
        {
            DefectMapPointVisual? point = _visiblePoints.FirstOrDefault(point => IsSameDefect(point.Item, item));
            return string.IsNullOrWhiteSpace(point?.DisplayTypeName)
                ? ResolveVisualStyle(item).DisplayName
                : point.DisplayTypeName;
        }

        private void RebuildAxes()
        {
            AxisX xAxis = new()
            {
                Minimum = 0d,
                Maximum = MaterialWidth,
                ValueType = AxisValueType.Number,
                AutoFormatLabels = false,
                LabelsVisible = true,
                EndPointLabelsVisible = true,
                AllowScaling = false,
                AllowScrolling = false,
                AxisColor = _appearance.AxisLineColor,
                LabelsColor = _appearance.AxisLabelColor,
                Title = new AxisXTitle
                {
                    Text = "Width",
                    Color = _appearance.AxisLabelColor
                }
            };

            xAxis.MajorGrid.Visible = true;
            xAxis.MinorGrid.Visible = false;
            xAxis.CustomTicks = CreateCustomTicks(xAxis, 0d, MaterialWidth, false);
            xAxis.CustomTicksEnabled = true;
            xAxis.SetRange(0d, MaterialWidth);

            AxisY yAxis = new()
            {
                Minimum = 0d,
                Maximum = MaterialLength,
                AutoFormatLabels = false,
                LabelsVisible = true,
                EndPointLabelsVisible = true,
                AllowScaling = false,
                AllowScrolling = false,
                AxisColor = _appearance.AxisLineColor,
                LabelsColor = _appearance.AxisLabelColor,
                Title = new AxisYTitle
                {
                    Text = "Length",
                    Color = _appearance.AxisLabelColor
                }
            };

            yAxis.MajorGrid.Visible = true;
            yAxis.MinorGrid.Visible = false;
            yAxis.CustomTicks = CreateCustomTicks(yAxis, 0d, MaterialLength, true);
            yAxis.CustomTicksEnabled = true;
            yAxis.SetRange(0d, MaterialLength);

            ChartXAxes = new AxisXCollection
            {
                xAxis
            };
            ChartYAxes = new AxisYCollection
            {
                yAxis
            };
        }

        private CustomAxisTickCollection CreateCustomTicks(AxisBase axis, double minimum, double maximum, bool isLengthAxis)
        {
            CustomAxisTickCollection ticks = new();
            foreach (double tickValue in CreateTicks(minimum, maximum))
            {
                double labelValue = isLengthAxis && _lengthOrigin == DefectMapLengthOrigin.Top
                    ? MaterialLength - tickValue
                    : tickValue;

                ticks.Add(new CustomAxisTick(
                    axis,
                    tickValue,
                    FormatTick(labelValue),
                    6,
                    true,
                    _appearance.AxisGridColor,
                    CustomTickStyle.TickAndGrid));
            }

            return ticks;
        }

        private double[] CreateTicks(double minimum, double maximum)
        {
            if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
            {
                return Array.Empty<double>();
            }

            if (maximum <= minimum)
            {
                return new[] { minimum };
            }

            const int segmentCount = 5;
            double span = maximum - minimum;
            double[] ticks = new double[segmentCount + 1];

            for (int index = 0; index <= segmentCount; index++)
            {
                double tickValue = minimum + ((span / segmentCount) * index);
                ticks[index] = Math.Round(tickValue, 6, MidpointRounding.AwayFromZero);
            }

            return ticks;
        }

        private string FormatTick(double value)
        {
            if (!double.IsFinite(value))
            {
                return string.Empty;
            }

            double absoluteValue = Math.Abs(value);
            if (absoluteValue >= 1000d)
            {
                return Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("F0");
            }

            if (absoluteValue > 0d && absoluteValue < 0.01d)
            {
                return value.ToString("G3");
            }

            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2");
        }

        private void RebuildSeries()
        {
            FreeformPointLineSeriesCollection series = new();

            foreach (IGrouping<DefectMapVisualStyleKey, DefectMapPointVisual> styleGroup in _visiblePoints
                         .Where(point => !IsSelected(point.Item))
                         .GroupBy(point => new DefectMapVisualStyleKey(point.DisplayColor, point.DisplaySize, point.MarkerShape)))
            {
                FreeformPointLineSeries styleSeries = CreateVisualStyleSeries(styleGroup.Key, styleGroup);
                if (styleSeries.Points != null && styleSeries.Points.Length > 0)
                {
                    series.Add(styleSeries);
                }
            }

            FreeformPointLineSeries selectedSeries = CreateSelectedSeries();
            if (selectedSeries.Points != null && selectedSeries.Points.Length > 0)
            {
                series.Add(selectedSeries);
            }

            DefectSeries = series;
        }

        private FreeformPointLineSeries CreateVisualStyleSeries(
            DefectMapVisualStyleKey styleKey,
            IEnumerable<DefectMapPointVisual> points)
        {
            return CreatePointSeries(
                points.Select(point => new SeriesPoint(point.ChartX, point.ChartY)).ToArray(),
                styleKey.Color,
                styleKey.Size,
                styleKey.Shape);
        }

        private FreeformPointLineSeries CreateSelectedSeries()
        {
            DefectMapPointVisual? selectedPoint = _selectedDefect == null
                ? null
                : _visiblePoints.FirstOrDefault(point => IsSameDefect(point.Item, _selectedDefect));

            if (selectedPoint == null)
            {
                return CreatePointSeries(
                    Array.Empty<SeriesPoint>(),
                    _appearance.SelectedColor,
                    _appearance.SelectedSize,
                    DefectMapMarkerShape.Circle);
            }

            double selectedSize = Math.Max(_appearance.SelectedSize, selectedPoint.DisplaySize);
            return CreatePointSeries(
                new[]
                {
                    new SeriesPoint(selectedPoint.ChartX, selectedPoint.ChartY)
                },
                _appearance.SelectedColor,
                selectedSize,
                selectedPoint.MarkerShape);
        }

        private FreeformPointLineSeries CreatePointSeries(
            SeriesPoint[] points,
            Color color,
            double size,
            DefectMapMarkerShape markerShape)
        {
            FreeformPointLineSeries series = new()
            {
                AllowUserInteraction = false,
                IncludeInAutoFit = false,
                LineVisible = false,
                PointsVisible = true,
                Points = points
            };

            series.PointStyle.Shape = ToChartShape(markerShape);
            series.PointStyle.Width = size;
            series.PointStyle.Height = size;
            series.PointStyle.Color1 = color;
            series.PointStyle.GradientFill = GradientFillPoint.Solid;

            return series;
        }

        private static Shape ToChartShape(DefectMapMarkerShape markerShape)
        {
            return markerShape switch
            {
                DefectMapMarkerShape.Rectangle => Shape.Rectangle,
                DefectMapMarkerShape.Triangle => Shape.Triangle,
                DefectMapMarkerShape.Cross => Shape.Cross,
                DefectMapMarkerShape.Flag => Shape.Flag,
                DefectMapMarkerShape.FlagLightning => Shape.FlagLightning,
                _ => Shape.Circle
            };
        }

        public void ShowTooltip(DefectMapItem item, Point mousePosition)
        {
            TooltipText = FormatTooltip(item);
            TooltipMargin = new Thickness(mousePosition.X + 12d, mousePosition.Y + 12d, 0d, 0d);
            TooltipVisibility = Visibility.Visible;
        }

        public void HideTooltip()
        {
            TooltipVisibility = Visibility.Collapsed;
        }

        public bool TrySelectNearest(double xValue, double yValue, out DefectMapItem? item)
        {
            item = FindNearestByAxisValues(xValue, yValue);
            if (item == null)
            {
                return false;
            }

            SetSelectedDefect(item);
            return true;
        }

        public DefectMapItem? FindNearestByAxisValues(double xValue, double yValue)
        {
            double toleranceX = Math.Max(MaterialWidth * HitToleranceRatio, 1e-9d);
            double toleranceY = Math.Max(MaterialLength * HitToleranceRatio, 1e-9d);
            return FindNearestByAxisValues(xValue, yValue, toleranceX, toleranceY);
        }

        public DefectMapItem? FindNearestByAxisValues(double xValue, double yValue, double toleranceX, double toleranceY)
        {
            if (_visiblePoints.Count == 0 ||
                !double.IsFinite(xValue) ||
                !double.IsFinite(yValue) ||
                !double.IsFinite(toleranceX) ||
                !double.IsFinite(toleranceY))
            {
                return null;
            }

            double normalizedToleranceX = Math.Max(toleranceX, 1e-9d);
            double normalizedToleranceY = Math.Max(toleranceY, 1e-9d);
            double bestDistance = double.PositiveInfinity;
            DefectMapItem? nearest = null;

            foreach (DefectMapPointVisual point in _visiblePoints)
            {
                double normalizedX = (point.ChartX - xValue) / normalizedToleranceX;
                double normalizedY = (point.ChartY - yValue) / normalizedToleranceY;
                double distance = Math.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = point.Item;
                }
            }

            return bestDistance <= 1d ? nearest : null;
        }

        private string FormatTooltip(DefectMapItem item)
        {
            string displayTypeName = GetDisplayTypeName(item);
            string text =
                $"{item.Name}\n" +
                $"Type: {displayTypeName}\n" +
                $"Severity: {item.Severity}\n" +
                $"Width: {item.WidthPosition:F2}\n" +
                $"Length: {item.LengthPosition:F2}";

            return string.IsNullOrWhiteSpace(item.Description)
                ? text
                : $"{text}\n{item.Description.Trim()}";
        }

        private void UpdateSelectedText()
        {
            SelectedDefectText = _selectedDefect == null
                ? "No defect selected."
                : FormatSelectedText(_selectedDefect);
        }

        private DefectMapItem? ResolveKnownDefect(DefectMapItem defect)
        {
            return _defects.FirstOrDefault(candidate => IsSameDefect(candidate, defect));
        }

        private bool IsSelected(DefectMapItem item)
        {
            return _selectedDefect != null && IsSameDefect(item, _selectedDefect);
        }

        private static bool IsSameDefect(DefectMapItem left, DefectMapItem right)
        {
            return ReferenceEquals(left, right) ||
                   (!string.IsNullOrWhiteSpace(left.Id) &&
                    string.Equals(left.Id, right.Id, StringComparison.Ordinal));
        }
    }
}

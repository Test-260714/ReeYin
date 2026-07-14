# Defect Map UserControl Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable LightningChart-based WPF UserControl that maps roll-material defects with width on X, length on Y, severity point markers, selectable length origin, hover details, and click selection.

**Architecture:** Add a focused `DefectMap` UserControl family under `Core/ReeYin_V.UI/UserControls`, with small model files, one ViewModel that owns chart axes/series/summary/selection state, and one View that exposes dependency properties to business pages. Code-behind remains a thin bridge for dependency properties, chart mouse coordinates, and command execution.

**Tech Stack:** C# WPF, .NET 8, Prism `BindableBase`, LightningChart MVVM `ViewXY`, `AxisXCollection`, `AxisYCollection`, and `FreeformPointLineSeriesCollection`.

---

## File Structure

- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapEnums.cs`
  - Owns severity and length-origin enum values.
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapItem.cs`
  - Owns the bindable generic defect item model used by external pages.
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapAppearance.cs`
  - Owns marker colors, marker sizes, chart colors, and severity style helpers.
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs`
  - Owns material dimensions, origin mapping, valid/out-of-range splitting, axes, point series, selected highlight, tooltip state, and nearest-point hit testing.
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml`
  - Owns the visual layout, LightningChart binding, overlay tooltip, and summary footer.
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs`
  - Owns dependency properties, view lifecycle, chart mouse event bridge, and selected command invocation.
- Create: `.codex_tmp/defect_map_static_tests.py`
  - Static regression checks that the expected files, APIs, and binding hooks exist.

Current workspace note: `git rev-parse --show-toplevel` fails at the workspace root, so this plan omits commit steps.

---

### Task 1: Static Regression Test

**Files:**
- Create: `.codex_tmp/defect_map_static_tests.py`

- [ ] **Step 1: Add a static test that describes the finished control**

Create `.codex_tmp/defect_map_static_tests.py` with this content:

```python
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative_path: str) -> str:
    path = ROOT / relative_path
    assert path.exists(), f"Missing file: {relative_path}"
    return path.read_text(encoding="utf-8-sig")


def assert_contains(text: str, needle: str, source: str) -> None:
    assert needle in text, f"{source} must contain {needle!r}"


def test_defect_map_files_exist() -> None:
    for relative_path in [
        "Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapEnums.cs",
        "Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapItem.cs",
        "Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapAppearance.cs",
        "Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs",
        "Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml",
        "Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs",
    ]:
        assert (ROOT / relative_path).exists(), f"Missing file: {relative_path}"


def test_public_models_and_enums_are_present() -> None:
    enums = read("Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapEnums.cs")
    item = read("Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapItem.cs")
    appearance = read("Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapAppearance.cs")

    assert_contains(enums, "enum DefectMapSeverity", "DefectMapEnums.cs")
    assert_contains(enums, "Minor", "DefectMapEnums.cs")
    assert_contains(enums, "Warning", "DefectMapEnums.cs")
    assert_contains(enums, "Critical", "DefectMapEnums.cs")
    assert_contains(enums, "enum DefectMapLengthOrigin", "DefectMapEnums.cs")
    assert_contains(enums, "Top", "DefectMapEnums.cs")
    assert_contains(enums, "Bottom", "DefectMapEnums.cs")

    for property_name in [
        "Id",
        "Name",
        "DefectType",
        "Severity",
        "WidthPosition",
        "LengthPosition",
        "DisplaySize",
        "Description",
        "Tag",
    ]:
        assert_contains(item, f"public", "DefectMapItem.cs")
        assert_contains(item, property_name, "DefectMapItem.cs")

    assert_contains(appearance, "DefectMapAppearance", "DefectMapAppearance.cs")
    assert_contains(appearance, "GetSeverityColor", "DefectMapAppearance.cs")
    assert_contains(appearance, "GetSeveritySize", "DefectMapAppearance.cs")


def test_view_model_exposes_mapping_series_selection_and_summary() -> None:
    vm = read("Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs")

    for member_name in [
        "ChartXAxes",
        "ChartYAxes",
        "DefectSeries",
        "SelectedDefect",
        "TooltipText",
        "SummaryText",
        "SetDefects",
        "SetMaterialSize",
        "SetLengthOrigin",
        "TrySelectNearest",
        "ShowTooltip",
        "HideTooltip",
        "MaterialLength - item.LengthPosition",
        "CreateSeveritySeries",
        "CreateSelectedSeries",
    ]:
        assert_contains(vm, member_name, "DefectMapViewModel.cs")

    assert_contains(vm, "FreeformPointLineSeriesCollection", "DefectMapViewModel.cs")
    assert_contains(vm, "CustomAxisTick", "DefectMapViewModel.cs")
    assert_contains(vm, "VisibleCount", "DefectMapViewModel.cs")
    assert_contains(vm, "OutOfRangeCount", "DefectMapViewModel.cs")


def test_view_and_code_behind_expose_bindable_control_api() -> None:
    xaml = read("Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml")
    code = read("Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs")

    assert_contains(xaml, "lcusb:LightningChart", "DefectMapView.xaml")
    assert_contains(xaml, "FreeformPointLineSeries=\"{Binding DefectSeries}\"", "DefectMapView.xaml")
    assert_contains(xaml, "XAxes=\"{Binding ChartXAxes}\"", "DefectMapView.xaml")
    assert_contains(xaml, "YAxes=\"{Binding ChartYAxes}\"", "DefectMapView.xaml")
    assert_contains(xaml, "PreviewMouseMove=\"Chart_PreviewMouseMove\"", "DefectMapView.xaml")
    assert_contains(xaml, "PreviewMouseLeftButtonDown=\"Chart_PreviewMouseLeftButtonDown\"", "DefectMapView.xaml")

    for dependency_property in [
        "DefectsProperty",
        "MaterialWidthProperty",
        "MaterialLengthProperty",
        "LengthOriginProperty",
        "SelectedDefectProperty",
        "DefectSelectedCommandProperty",
        "MapTitleProperty",
    ]:
        assert_contains(code, dependency_property, "DefectMapView.xaml.cs")

    assert_contains(code, "BindsTwoWayByDefault", "DefectMapView.xaml.cs")
    assert_contains(code, "DefectSelectedCommand.Execute", "DefectMapView.xaml.cs")
    assert_contains(code, "CoordToValue", "DefectMapView.xaml.cs")


def main() -> int:
    tests = [
        value
        for name, value in sorted(globals().items())
        if name.startswith("test_") and callable(value)
    ]

    for test in tests:
        try:
            test()
        except AssertionError as exc:
            print(exc)
            return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 2: Run the static test and confirm it fails before implementation**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: non-zero exit code with at least one `Missing file` assertion because the `DefectMap` control files do not exist yet.

---

### Task 2: Add Public Models

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapEnums.cs`
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapItem.cs`
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapAppearance.cs`

- [ ] **Step 1: Add enum definitions**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapEnums.cs`:

```csharp
namespace ReeYin_V.UI.UserControls.DefectMap
{
    public enum DefectMapSeverity
    {
        Minor = 0,
        Warning = 1,
        Critical = 2
    }

    public enum DefectMapLengthOrigin
    {
        Top = 0,
        Bottom = 1
    }
}
```

- [ ] **Step 2: Add the bindable defect item**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapItem.cs`:

```csharp
using Prism.Mvvm;
using System;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed class DefectMapItem : BindableBase
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Defect";
        private string _defectType = "Unknown";
        private DefectMapSeverity _severity = DefectMapSeverity.Minor;
        private double _widthPosition;
        private double _lengthPosition;
        private double? _displaySize;
        private string _description = string.Empty;
        private object? _tag;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim());
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Defect" : value.Trim());
        }

        public string DefectType
        {
            get => _defectType;
            set => SetProperty(ref _defectType, string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim());
        }

        public DefectMapSeverity Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        public double WidthPosition
        {
            get => _widthPosition;
            set => SetProperty(ref _widthPosition, value);
        }

        public double LengthPosition
        {
            get => _lengthPosition;
            set => SetProperty(ref _lengthPosition, value);
        }

        public double? DisplaySize
        {
            get => _displaySize;
            set => SetProperty(ref _displaySize, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        public object? Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }
    }
}
```

- [ ] **Step 3: Add appearance defaults and helpers**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/Models/DefectMapAppearance.cs`:

```csharp
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed record DefectMapAppearance(
        Color MinorColor,
        Color WarningColor,
        Color CriticalColor,
        Color SelectedColor,
        Color AxisLineColor,
        Color AxisLabelColor,
        Color AxisGridColor,
        Color ChartBackgroundColor,
        double MinorSize,
        double WarningSize,
        double CriticalSize,
        double SelectedSize)
    {
        public static DefectMapAppearance Default { get; } = new(
            Color.FromRgb(0x22, 0xC5, 0x5E),
            Color.FromRgb(0xF5, 0x9E, 0x0B),
            Color.FromRgb(0xEF, 0x44, 0x44),
            Color.FromRgb(0x25, 0x63, 0xEB),
            Color.FromRgb(0x2E, 0x40, 0x50),
            Color.FromRgb(0x3F, 0x52, 0x63),
            Color.FromArgb(0x70, 0x8E, 0xA8, 0xBA),
            Color.FromRgb(0xF7, 0xFA, 0xFD),
            8d,
            11d,
            15d,
            22d);

        public Color GetSeverityColor(DefectMapSeverity severity)
        {
            return severity switch
            {
                DefectMapSeverity.Critical => CriticalColor,
                DefectMapSeverity.Warning => WarningColor,
                _ => MinorColor
            };
        }

        public double GetSeveritySize(DefectMapSeverity severity)
        {
            return severity switch
            {
                DefectMapSeverity.Critical => CriticalSize,
                DefectMapSeverity.Warning => WarningSize,
                _ => MinorSize
            };
        }
    }
}
```

- [ ] **Step 4: Run the static test**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: model file checks pass; ViewModel/View checks still fail.

---

### Task 3: Add Defect Map ViewModel

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs`

- [ ] **Step 1: Add ViewModel skeleton, public binding properties, and normalized state**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs` with these class members before filling the rebuild methods in later steps:

```csharp
using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Titles;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public sealed record DefectMapPointVisual(
        DefectMapItem Item,
        double ChartX,
        double ChartY,
        double DisplaySize);

    public sealed class DefectMapViewModel : BindableBase
    {
        private readonly List<DefectMapItem> _defects = new();
        private readonly List<DefectMapPointVisual> _visiblePoints = new();
        private readonly DefectMapAppearance _appearance = DefectMapAppearance.Default;
        private AxisXCollection _chartXAxes = new();
        private AxisYCollection _chartYAxes = new();
        private FreeformPointLineSeriesCollection _defectSeries = new();
        private string _mapTitle = "Defect Map";
        private string _summaryText = "Total 0, visible 0, out of range 0";
        private string _selectedDefectText = "No defect selected";
        private string _tooltipText = string.Empty;
        private Thickness _tooltipMargin = new(0);
        private Visibility _tooltipVisibility = Visibility.Collapsed;
        private DefectMapItem? _selectedDefect;
        private double _materialWidth = 1d;
        private double _materialLength = 1d;
        private DefectMapLengthOrigin _lengthOrigin = DefectMapLengthOrigin.Top;
        private int _outOfRangeCount;

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
            private set
            {
                if (SetProperty(ref _selectedDefect, value))
                {
                    SelectedDefectText = value == null ? "No defect selected" : FormatSelectedText(value);
                    RebuildSeries();
                }
            }
        }

        public int TotalCount => _defects.Count;

        public int VisibleCount => _visiblePoints.Count;

        public int OutOfRangeCount => _outOfRangeCount;
    }
}
```

- [ ] **Step 2: Add public update methods and rebuild orchestration**

Add these methods inside `DefectMapViewModel`:

```csharp
public void SetMapTitle(string? mapTitle)
{
    MapTitle = string.IsNullOrWhiteSpace(mapTitle) ? "Defect Map" : mapTitle.Trim();
}

public void SetMaterialSize(double materialWidth, double materialLength)
{
    _materialWidth = NormalizeDimension(materialWidth);
    _materialLength = NormalizeDimension(materialLength);
    RebuildChart();
}

public void SetLengthOrigin(DefectMapLengthOrigin lengthOrigin)
{
    if (_lengthOrigin == lengthOrigin)
    {
        return;
    }

    _lengthOrigin = lengthOrigin;
    RebuildChart();
}

public void SetDefects(IEnumerable<DefectMapItem>? defects)
{
    _defects.Clear();
    if (defects != null)
    {
        _defects.AddRange(defects.Where(item => item != null));
    }

    if (_selectedDefect != null && !_defects.Contains(_selectedDefect))
    {
        _selectedDefect = null;
        RaisePropertyChanged(nameof(SelectedDefect));
        SelectedDefectText = "No defect selected";
    }

    RebuildChart();
}

public void SetSelectedDefect(DefectMapItem? defect)
{
    SelectedDefect = defect != null && _defects.Contains(defect) ? defect : null;
}

private void RebuildChart()
{
    RebuildAxes();
    RebuildVisiblePoints();
    RebuildSeries();
    UpdateSummary();
}

private static double NormalizeDimension(double value)
{
    return double.IsFinite(value) && value > 0d ? value : 1d;
}
```

- [ ] **Step 3: Add coordinate mapping, visible point splitting, and summary**

Add these methods inside `DefectMapViewModel`:

```csharp
private void RebuildVisiblePoints()
{
    _visiblePoints.Clear();
    _outOfRangeCount = 0;

    foreach (DefectMapItem item in _defects)
    {
        if (!IsInMaterial(item))
        {
            _outOfRangeCount++;
            continue;
        }

        _visiblePoints.Add(new DefectMapPointVisual(
            item,
            item.WidthPosition,
            ToChartLength(item),
            item.DisplaySize.HasValue && item.DisplaySize.Value > 0d
                ? item.DisplaySize.Value
                : _appearance.GetSeveritySize(item.Severity)));
    }

    RaisePropertyChanged(nameof(VisiblePoints));
    RaisePropertyChanged(nameof(TotalCount));
    RaisePropertyChanged(nameof(VisibleCount));
    RaisePropertyChanged(nameof(OutOfRangeCount));
}

private bool IsInMaterial(DefectMapItem item)
{
    return double.IsFinite(item.WidthPosition) &&
           double.IsFinite(item.LengthPosition) &&
           item.WidthPosition >= 0d &&
           item.WidthPosition <= _materialWidth &&
           item.LengthPosition >= 0d &&
           item.LengthPosition <= _materialLength;
}

private double ToChartLength(DefectMapItem item)
{
    return _lengthOrigin == DefectMapLengthOrigin.Top
        ? _materialLength - item.LengthPosition
        : item.LengthPosition;
}

private void UpdateSummary()
{
    SummaryText = $"Total {TotalCount}, visible {VisibleCount}, out of range {OutOfRangeCount}    Width {_materialWidth:F2}, length {_materialLength:F2}";
}

private static string FormatSelectedText(DefectMapItem item)
{
    return $"{item.Name} | {item.DefectType} | {item.Severity} | W {item.WidthPosition:F2}, L {item.LengthPosition:F2}";
}
```

- [ ] **Step 4: Add axis creation with top-origin label conversion**

Add these methods inside `DefectMapViewModel`:

```csharp
private void RebuildAxes()
{
    AxisX xAxis = new()
    {
        Minimum = 0d,
        Maximum = _materialWidth,
        ValueType = AxisValueType.Number,
        AutoFormatLabels = false,
        LabelsVisible = true,
        EndPointLabelsVisible = true,
        AllowScaling = true,
        AllowScrolling = true,
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
    xAxis.CustomTicks = CreateCustomTicks(xAxis, 0d, _materialWidth, false);
    xAxis.CustomTicksEnabled = true;
    xAxis.SetRange(0d, _materialWidth);

    AxisY yAxis = new()
    {
        Minimum = 0d,
        Maximum = _materialLength,
        AutoFormatLabels = false,
        LabelsVisible = true,
        EndPointLabelsVisible = true,
        AllowScaling = true,
        AllowScrolling = true,
        AxisColor = _appearance.AxisLineColor,
        LabelsColor = _appearance.AxisLabelColor,
        Title = new AxisYTitle
        {
            Text = _lengthOrigin == DefectMapLengthOrigin.Top ? "Length (0 at top)" : "Length",
            Color = _appearance.AxisLabelColor
        }
    };
    yAxis.MajorGrid.Visible = true;
    yAxis.MinorGrid.Visible = false;
    yAxis.CustomTicks = CreateCustomTicks(yAxis, 0d, _materialLength, true);
    yAxis.CustomTicksEnabled = true;
    yAxis.SetRange(0d, _materialLength);

    ChartXAxes = new AxisXCollection { xAxis };
    ChartYAxes = new AxisYCollection { yAxis };
}

private CustomAxisTickCollection CreateCustomTicks(AxisBase axis, double minimum, double maximum, bool isLengthAxis)
{
    CustomAxisTickCollection ticks = new();
    foreach (double tickValue in CreateTicks(minimum, maximum))
    {
        string label = isLengthAxis && _lengthOrigin == DefectMapLengthOrigin.Top
            ? FormatTick(_materialLength - tickValue)
            : FormatTick(tickValue);

        ticks.Add(new CustomAxisTick(
            axis,
            tickValue,
            label,
            6,
            true,
            _appearance.AxisGridColor,
            CustomTickStyle.TickAndGrid));
    }

    return ticks;
}

private static IReadOnlyList<double> CreateTicks(double minimum, double maximum)
{
    if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum <= minimum)
    {
        return new[] { minimum, maximum };
    }

    double span = maximum - minimum;
    double rawStep = span / 8d;
    double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(rawStep)));
    double normalized = rawStep / magnitude;
    double step = normalized <= 2d ? 2d * magnitude : normalized <= 5d ? 5d * magnitude : 10d * magnitude;

    List<double> ticks = new() { minimum };
    for (double value = Math.Ceiling(minimum / step) * step; value < maximum; value += step)
    {
        if (value > minimum)
        {
            ticks.Add(Math.Round(value, 6, MidpointRounding.AwayFromZero));
        }
    }

    ticks.Add(maximum);
    return ticks;
}

private static string FormatTick(double value)
{
    return Math.Abs(value) >= 1000d ? value.ToString("F0") : value.ToString("F2");
}
```

- [ ] **Step 5: Add severity series, selected series, and point style helpers**

Add these methods inside `DefectMapViewModel`:

```csharp
private void RebuildSeries()
{
    FreeformPointLineSeriesCollection seriesCollection = new();

    seriesCollection.Add(CreateSeveritySeries(DefectMapSeverity.Minor));
    seriesCollection.Add(CreateSeveritySeries(DefectMapSeverity.Warning));
    seriesCollection.Add(CreateSeveritySeries(DefectMapSeverity.Critical));

    FreeformPointLineSeries? selectedSeries = CreateSelectedSeries();
    if (selectedSeries != null)
    {
        seriesCollection.Add(selectedSeries);
    }

    DefectSeries = seriesCollection;
}

private FreeformPointLineSeries CreateSeveritySeries(DefectMapSeverity severity)
{
    DefectMapPointVisual[] points = _visiblePoints
        .Where(point => point.Item.Severity == severity && point.Item != _selectedDefect)
        .ToArray();

    FreeformPointLineSeries series = CreatePointSeries(
        points.Select(point => new SeriesPoint(point.ChartX, point.ChartY)).ToArray(),
        _appearance.GetSeverityColor(severity),
        _appearance.GetSeveritySize(severity));

    series.Title.Text = severity.ToString();
    return series;
}

private FreeformPointLineSeries? CreateSelectedSeries()
{
    if (_selectedDefect == null)
    {
        return null;
    }

    DefectMapPointVisual? point = _visiblePoints.FirstOrDefault(value => value.Item == _selectedDefect);
    if (point == null)
    {
        return null;
    }

    FreeformPointLineSeries series = CreatePointSeries(
        new[] { new SeriesPoint(point.ChartX, point.ChartY) },
        _appearance.SelectedColor,
        _appearance.SelectedSize);

    series.Title.Text = "Selected";
    return series;
}

private static FreeformPointLineSeries CreatePointSeries(SeriesPoint[] points, Color color, double size)
{
    FreeformPointLineSeries series = new()
    {
        AllowUserInteraction = false,
        IncludeInAutoFit = false,
        LineVisible = false,
        PointsVisible = true,
        Points = points
    };

    series.PointStyle.Shape = Shape.Circle;
    series.PointStyle.Width = size;
    series.PointStyle.Height = size;
    series.PointStyle.Color1 = color;
    series.PointStyle.GradientFill = GradientFillPoint.Solid;

    return series;
}
```

- [ ] **Step 6: Add tooltip formatting and nearest-point hit testing**

Add these methods inside `DefectMapViewModel`:

```csharp
public void ShowTooltip(DefectMapItem item, Point mousePosition)
{
    TooltipText = FormatTooltip(item);
    TooltipMargin = new Thickness(mousePosition.X + 18d, mousePosition.Y + 18d, 0d, 0d);
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

    SelectedDefect = item;
    return true;
}

public DefectMapItem? FindNearestByAxisValues(double xValue, double yValue)
{
    if (_visiblePoints.Count == 0 || !double.IsFinite(xValue) || !double.IsFinite(yValue))
    {
        return null;
    }

    double xTolerance = Math.Max(_materialWidth * 0.015d, 1e-6d);
    double yTolerance = Math.Max(_materialLength * 0.015d, 1e-6d);
    double bestScore = double.MaxValue;
    DefectMapItem? bestItem = null;

    foreach (DefectMapPointVisual point in _visiblePoints)
    {
        double normalizedX = (point.ChartX - xValue) / xTolerance;
        double normalizedY = (point.ChartY - yValue) / yTolerance;
        double score = (normalizedX * normalizedX) + (normalizedY * normalizedY);
        if (score <= 1d && score < bestScore)
        {
            bestScore = score;
            bestItem = point.Item;
        }
    }

    return bestItem;
}

private static string FormatTooltip(DefectMapItem item)
{
    string description = string.IsNullOrWhiteSpace(item.Description)
        ? "No description"
        : item.Description;

    return $"{item.Name}\nType: {item.DefectType}\nSeverity: {item.Severity}\nWidth: {item.WidthPosition:F2}\nLength: {item.LengthPosition:F2}\n{description}";
}
```

- [ ] **Step 7: Run the static test**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: ViewModel checks pass; View and code-behind checks still fail.

---

### Task 4: Add DefectMapView XAML

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml`

- [ ] **Step 1: Add the XAML view**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml`:

```xml
<UserControl x:Class="ReeYin_V.UI.UserControls.DefectMap.DefectMapView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:lcusb="http://schemas.arction.com/ChartingMVVM/ultimate/"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             Loaded="UserControl_Loaded"
             Unloaded="UserControl_Unloaded"
             ClipToBounds="True"
             d:DesignWidth="720"
             d:DesignHeight="900">
    <UserControl.Resources>
        <SolidColorBrush x:Key="DefectMapPanelBrush" Color="#F7FAFD" />
        <SolidColorBrush x:Key="DefectMapBorderBrush" Color="#CCD8E4EF" />
        <SolidColorBrush x:Key="DefectMapPrimaryTextBrush" Color="#243547" />
        <SolidColorBrush x:Key="DefectMapSecondaryTextBrush" Color="#586D81" />

        <Style x:Key="DefectMapMetricTitleStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="{StaticResource DefectMapSecondaryTextBrush}" />
            <Setter Property="FontSize" Value="12" />
        </Style>

        <Style x:Key="DefectMapMetricValueStyle" TargetType="TextBlock">
            <Setter Property="Foreground" Value="{StaticResource DefectMapPrimaryTextBrush}" />
            <Setter Property="FontSize" Value="13" />
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="TextTrimming" Value="CharacterEllipsis" />
        </Style>
    </UserControl.Resources>

    <Grid DataContext="{Binding ViewModel, RelativeSource={RelativeSource AncestorType=UserControl}}"
          Background="Transparent">
        <Border Margin="6"
                Background="{StaticResource DefectMapPanelBrush}"
                BorderBrush="{StaticResource DefectMapBorderBrush}"
                BorderThickness="1"
                CornerRadius="10">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0"
                           Margin="14,10,14,6"
                           Foreground="{StaticResource DefectMapPrimaryTextBrush}"
                           FontSize="15"
                           FontWeight="SemiBold"
                           Text="{Binding MapTitle}" />

                <Grid Grid.Row="1"
                      Margin="10,0,10,0"
                      ClipToBounds="True"
                      Background="#F7FAFD">
                    <lcusb:LightningChart x:Name="PART_Chart"
                                          ActiveView="ViewXY"
                                          PreviewMouseMove="Chart_PreviewMouseMove"
                                          PreviewMouseLeftButtonDown="Chart_PreviewMouseLeftButtonDown"
                                          MouseLeave="Chart_MouseLeave">
                        <lcusb:LightningChart.ViewXY>
                            <lcusb:ViewXY XAxes="{Binding ChartXAxes}"
                                          YAxes="{Binding ChartYAxes}"
                                          FreeformPointLineSeries="{Binding DefectSeries}"
                                          Margins="10" />
                        </lcusb:LightningChart.ViewXY>
                        <lcusb:LightningChart.ChartBackground>
                            <lcusb:Fill Color="#FFF7FAFD"
                                        GradientColor="#FFF7FAFD"
                                        GradientDirection="-45"
                                        GradientFill="Solid" />
                        </lcusb:LightningChart.ChartBackground>
                    </lcusb:LightningChart>

                    <Border HorizontalAlignment="Left"
                            VerticalAlignment="Top"
                            MaxWidth="280"
                            Margin="{Binding TooltipMargin}"
                            Padding="10,8"
                            Background="#F21F2D3A"
                            BorderBrush="#88243547"
                            BorderThickness="1"
                            CornerRadius="6"
                            Visibility="{Binding TooltipVisibility}">
                        <TextBlock Foreground="White"
                                   FontSize="12"
                                   Text="{Binding TooltipText}"
                                   TextWrapping="Wrap" />
                    </Border>
                </Grid>

                <Border Grid.Row="2"
                        Margin="10,8,10,10"
                        Padding="12,8"
                        Background="#F4FFFFFF"
                        BorderBrush="#CCD8E4EF"
                        BorderThickness="1"
                        CornerRadius="8">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="1.2*" />
                        </Grid.ColumnDefinitions>

                        <StackPanel Grid.Column="0" Margin="0,0,14,0">
                            <TextBlock Style="{StaticResource DefectMapMetricTitleStyle}" Text="Summary" />
                            <TextBlock Margin="0,4,0,0"
                                       Style="{StaticResource DefectMapMetricValueStyle}"
                                       Text="{Binding SummaryText}" />
                        </StackPanel>

                        <StackPanel Grid.Column="1">
                            <TextBlock Style="{StaticResource DefectMapMetricTitleStyle}" Text="Selected Defect" />
                            <TextBlock Margin="0,4,0,0"
                                       Style="{StaticResource DefectMapMetricValueStyle}"
                                       Text="{Binding SelectedDefectText}" />
                        </StackPanel>
                    </Grid>
                </Border>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Run the static test**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: XAML checks pass; code-behind checks still fail.

---

### Task 5: Add DefectMapView Code-Behind Bridge

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs`

- [ ] **Step 1: Add dependency properties and constructor**

Create `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs` with this content:

```csharp
using Arction.Wpf.ChartingMVVM;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public partial class DefectMapView : UserControl, IDisposable
    {
        public static readonly DependencyProperty DefectsProperty =
            DependencyProperty.Register(
                nameof(Defects),
                typeof(IEnumerable<DefectMapItem>),
                typeof(DefectMapView),
                new PropertyMetadata(null, OnDefectsChanged));

        public static readonly DependencyProperty MaterialWidthProperty =
            DependencyProperty.Register(
                nameof(MaterialWidth),
                typeof(double),
                typeof(DefectMapView),
                new PropertyMetadata(1d, OnMaterialSizeChanged));

        public static readonly DependencyProperty MaterialLengthProperty =
            DependencyProperty.Register(
                nameof(MaterialLength),
                typeof(double),
                typeof(DefectMapView),
                new PropertyMetadata(1d, OnMaterialSizeChanged));

        public static readonly DependencyProperty LengthOriginProperty =
            DependencyProperty.Register(
                nameof(LengthOrigin),
                typeof(DefectMapLengthOrigin),
                typeof(DefectMapView),
                new PropertyMetadata(DefectMapLengthOrigin.Top, OnLengthOriginChanged));

        public static readonly DependencyProperty SelectedDefectProperty =
            DependencyProperty.Register(
                nameof(SelectedDefect),
                typeof(DefectMapItem),
                typeof(DefectMapView),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDefectChanged));

        public static readonly DependencyProperty DefectSelectedCommandProperty =
            DependencyProperty.Register(
                nameof(DefectSelectedCommand),
                typeof(ICommand),
                typeof(DefectMapView),
                new PropertyMetadata(null));

        public static readonly DependencyProperty MapTitleProperty =
            DependencyProperty.Register(
                nameof(MapTitle),
                typeof(string),
                typeof(DefectMapView),
                new PropertyMetadata("Defect Map", OnMapTitleChanged));

        private readonly DefectMapViewModel _viewModel = new();
        private bool _disposed;
        private bool _isSyncingSelectedDefect;

        public DefectMapView()
        {
            InitializeComponent();
        }

        public IEnumerable<DefectMapItem>? Defects
        {
            get => (IEnumerable<DefectMapItem>?)GetValue(DefectsProperty);
            set => SetValue(DefectsProperty, value);
        }

        public double MaterialWidth
        {
            get => (double)GetValue(MaterialWidthProperty);
            set => SetValue(MaterialWidthProperty, value);
        }

        public double MaterialLength
        {
            get => (double)GetValue(MaterialLengthProperty);
            set => SetValue(MaterialLengthProperty, value);
        }

        public DefectMapLengthOrigin LengthOrigin
        {
            get => (DefectMapLengthOrigin)GetValue(LengthOriginProperty);
            set => SetValue(LengthOriginProperty, value);
        }

        public DefectMapItem? SelectedDefect
        {
            get => (DefectMapItem?)GetValue(SelectedDefectProperty);
            set => SetValue(SelectedDefectProperty, value);
        }

        public ICommand? DefectSelectedCommand
        {
            get => (ICommand?)GetValue(DefectSelectedCommandProperty);
            set => SetValue(DefectSelectedCommandProperty, value);
        }

        public string MapTitle
        {
            get => (string)GetValue(MapTitleProperty);
            set => SetValue(MapTitleProperty, value);
        }

        public DefectMapViewModel ViewModel => _viewModel;
    }
}
```

- [ ] **Step 2: Add dependency property change handlers**

Add these methods inside `DefectMapView`:

```csharp
private static void OnDefectsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
{
    if (dependencyObject is DefectMapView view)
    {
        view._viewModel.SetDefects(e.NewValue as IEnumerable<DefectMapItem>);
    }
}

private static void OnMaterialSizeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
{
    if (dependencyObject is DefectMapView view)
    {
        view._viewModel.SetMaterialSize(view.MaterialWidth, view.MaterialLength);
    }
}

private static void OnLengthOriginChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
{
    if (dependencyObject is DefectMapView view && e.NewValue is DefectMapLengthOrigin origin)
    {
        view._viewModel.SetLengthOrigin(origin);
    }
}

private static void OnSelectedDefectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
{
    if (dependencyObject is not DefectMapView view || view._isSyncingSelectedDefect)
    {
        return;
    }

    view._viewModel.SetSelectedDefect(e.NewValue as DefectMapItem);
}

private static void OnMapTitleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
{
    if (dependencyObject is DefectMapView view)
    {
        view._viewModel.SetMapTitle(e.NewValue as string);
    }
}
```

- [ ] **Step 3: Add lifecycle and chart configuration**

Add these methods inside `DefectMapView`:

```csharp
private void UserControl_Loaded(object sender, RoutedEventArgs e)
{
    _disposed = false;
    ConfigureChart();
    _viewModel.SetMapTitle(MapTitle);
    _viewModel.SetMaterialSize(MaterialWidth, MaterialLength);
    _viewModel.SetLengthOrigin(LengthOrigin);
    _viewModel.SetDefects(Defects);
    _viewModel.SetSelectedDefect(SelectedDefect);
}

private void UserControl_Unloaded(object sender, RoutedEventArgs e)
{
    Dispose();
}

public void Dispose()
{
    if (_disposed)
    {
        return;
    }

    _disposed = true;
    _viewModel.HideTooltip();
}

private void ConfigureChart()
{
    if (PART_Chart?.ViewXY == null)
    {
        return;
    }

    PART_Chart.ViewXY.ZoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
    PART_Chart.ViewXY.ZoomPanOptions.DeviceSecondaryButtonAction = UserInteractiveDeviceButtonAction.None;
    PART_Chart.ViewXY.ZoomPanOptions.DeviceTertiaryButtonAction = UserInteractiveDeviceButtonAction.None;
    PART_Chart.ViewXY.ZoomPanOptions.WheelZooming = WheelZooming.Off;
    PART_Chart.ViewXY.ZoomPanOptions.AxisWheelAction = AxisWheelAction.None;
    PART_Chart.ViewXY.ZoomPanOptions.MultiTouchZoomEnabled = false;
    PART_Chart.ViewXY.ZoomPanOptions.MultiTouchPanEnabled = false;

    if (PART_Chart.ViewXY.LegendBoxes.Count > 0)
    {
        PART_Chart.ViewXY.LegendBoxes[0].Visible = false;
    }

    PART_Chart.ViewXY.DataCursor.Visible = false;
}
```

- [ ] **Step 4: Add mouse coordinate conversion, hover tooltip, and selection command**

Add these methods inside `DefectMapView`:

```csharp
private void Chart_PreviewMouseMove(object sender, MouseEventArgs e)
{
    if (!TryGetAxisValues(e, out double xValue, out double yValue))
    {
        _viewModel.HideTooltip();
        return;
    }

    DefectMapItem? item = _viewModel.FindNearestByAxisValues(xValue, yValue);
    if (item == null)
    {
        _viewModel.HideTooltip();
        return;
    }

    _viewModel.ShowTooltip(item, e.GetPosition(PART_Chart));
}

private void Chart_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!TryGetAxisValues(e, out double xValue, out double yValue))
    {
        return;
    }

    if (!_viewModel.TrySelectNearest(xValue, yValue, out DefectMapItem? selectedDefect) || selectedDefect == null)
    {
        return;
    }

    _isSyncingSelectedDefect = true;
    try
    {
        SetCurrentValue(SelectedDefectProperty, selectedDefect);
    }
    finally
    {
        _isSyncingSelectedDefect = false;
    }

    if (DefectSelectedCommand?.CanExecute(selectedDefect) == true)
    {
        DefectSelectedCommand.Execute(selectedDefect);
    }

    e.Handled = true;
}

private void Chart_MouseLeave(object sender, MouseEventArgs e)
{
    _viewModel.HideTooltip();
}

private bool TryGetAxisValues(MouseEventArgs e, out double xValue, out double yValue)
{
    xValue = 0d;
    yValue = 0d;

    if (PART_Chart?.ViewXY == null || _viewModel.ChartXAxes.Count == 0 || _viewModel.ChartYAxes.Count == 0)
    {
        return false;
    }

    Point position = e.GetPosition(PART_Chart);
    _viewModel.ChartXAxes[0].CoordToValue((int)position.X, out xValue, false);
    _viewModel.ChartYAxes[0].CoordToValue((int)position.Y, out yValue);

    return double.IsFinite(xValue) && double.IsFinite(yValue);
}
```

- [ ] **Step 5: Run the static test**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: pass.

---

### Task 6: Build And API Adjustments

**Files:**
- Modify only files created in Tasks 2-5 if compilation reveals exact LightningChart API differences.

- [ ] **Step 1: Build the UI project without restore**

Run:

```powershell
dotnet build Core\ReeYin_V.UI\ReeYin_V.UI.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false
```

Expected: the project compiles or reports exact API errors in the new `DefectMap` files.

- [ ] **Step 2: Fix exact compile errors in the new files**

Use the existing LightningChart controls as the source of truth:

- `Core/ReeYin_V.UI/UserControls/AxisMotionTrajectoryMonitor/Models/AxisMotionTrajectoryMonitorModel.cs`
- `Core/ReeYin_V.UI/UserControls/AxisMotionTrajectoryMonitor/Views/AxisMotionTrajectoryMonitorView.xaml.cs`
- `Core/ReeYin_V.UI/UserControls/ImageRedactor/Views/ImageRedactorView.xaml.cs`

Expected fixes are limited to method signatures or property names such as `WheelZooming`, `AxisWheelAction`, `CustomAxisTick`, `PointStyle`, `CoordToValue`, or `ValueToCoord`.

- [ ] **Step 3: Re-run static test**

Run:

```powershell
python .codex_tmp\defect_map_static_tests.py
```

Expected: pass.

- [ ] **Step 4: Re-run build**

Run:

```powershell
dotnet build Core\ReeYin_V.UI\ReeYin_V.UI.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false
```

Expected: exit code 0. Existing warnings outside the new `DefectMap` files can remain.

---

### Task 7: Manual Usage Probe

**Files:**
- No committed source changes required.

- [ ] **Step 1: Verify sample XAML usage compiles mentally against the dependency properties**

Use this sample from a downstream page:

```xml
<defectMap:DefectMapView
    Defects="{Binding Defects}"
    MaterialWidth="{Binding MaterialWidth}"
    MaterialLength="{Binding MaterialLength}"
    LengthOrigin="{Binding LengthOrigin}"
    SelectedDefect="{Binding SelectedDefect, Mode=TwoWay}"
    DefectSelectedCommand="{Binding DefectSelectedCommand}" />
```

Confirm the page also declares:

```xml
xmlns:defectMap="clr-namespace:ReeYin_V.UI.UserControls.DefectMap;assembly=ReeYin_V.UI"
```

- [ ] **Step 2: Verify top-origin and bottom-origin mapping with sample values**

Use a material size of width `1000` and length `5000`.

Expected mapping:

| Real width | Real length | Origin | Chart X | Chart Y | Visual location |
| --- | --- | --- | --- | --- | --- |
| 500 | 0 | Top | 500 | 5000 | top center |
| 500 | 5000 | Top | 500 | 0 | bottom center |
| 500 | 0 | Bottom | 500 | 0 | bottom center |
| 500 | 5000 | Bottom | 500 | 5000 | top center |

- [ ] **Step 3: Verify interaction behavior by code inspection after build**

Check these behaviors in `Core/ReeYin_V.UI/UserControls/DefectMap/Views/DefectMapView.xaml.cs` and `Core/ReeYin_V.UI/UserControls/DefectMap/ViewModels/DefectMapViewModel.cs`:

- Hover calls `FindNearestByAxisValues`, then `ShowTooltip`.
- Mouse leave calls `HideTooltip`.
- Click calls `TrySelectNearest`.
- Click writes `SelectedDefectProperty` with `SetCurrentValue`.
- Click executes `DefectSelectedCommand.Execute(selectedDefect)` only when `CanExecute` returns `true`.

---

## Self-Review Checklist

- Spec coverage:
  - Location under `Core/ReeYin_V.UI/UserControls/DefectMap`: Tasks 2-5.
  - LightningChart `ViewXY`: Task 4.
  - MVVM data preparation: Task 3.
  - Public dependency properties: Task 5.
  - Width on X and length on Y: Tasks 3-4.
  - `LengthOrigin.Top` and `LengthOrigin.Bottom`: Tasks 3 and 7.
  - Severity point markers: Task 3.
  - Hover tooltip: Tasks 3 and 5.
  - Click selection and command: Task 5.
  - Out-of-range count: Task 3.
  - Build verification: Task 6.
- Placeholder scan:
  - This plan does not use open-ended implementation placeholders.
  - Each created file has concrete content or concrete edits.
- Type consistency:
  - `DefectMapItem`, `DefectMapSeverity`, and `DefectMapLengthOrigin` are defined before the ViewModel and View use them.
  - Dependency property names match the expected usage snippet.
  - View bindings match `DefectMapViewModel` property names.

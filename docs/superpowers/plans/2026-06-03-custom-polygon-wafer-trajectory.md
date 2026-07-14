# Custom Polygon Wafer Trajectory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `自定义多边形` trajectory mode that fills a user-defined polygon with point loci at the configured spacing.

**Architecture:** Extend the existing `CreateTrajectoryModel` generator so polygon points use the same generate command, `AllLocusInfo`, optional shortest-path sorting, preview, and execution flow as current point modes. Add one serializable vertex model, mode-specific XAML controls, and focused console tests.

**Tech Stack:** C#/.NET 8 WPF, Prism `BindableBase` and `DelegateCommand`, HandyControl `NumericUpDown`, existing ReeYin-V MVVM patterns, console regression tests via `dotnet run`.

---

## File Structure

- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/CustomPolygonPoint.cs` - bindable absolute XY polygon vertex.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj` - focused console test project.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs` - custom polygon generation tests.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/CreateTrajectoryModel.cs` - polygon data, validation, mode, summary, and fill algorithm.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessConfigViewModel.cs` - add/remove vertex commands and preview outline.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml` - polygon vertex input/list UI.

Current workspace note: `git status` fails with `fatal: not a git repository`, so this plan uses verification checkpoints instead of commit steps.

---

### Task 1: Add Failing Polygon Generation Tests

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] **Step 1: Write the test project**

Create `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <SolutionDir Condition="'$(SolutionDir)' == '' Or '$(SolutionDir)' == '*Undefined*'">$(MSBuildThisFileDirectory)..\..\</SolutionDir>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write tests before production code**

Create `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`:

```csharp
using Custom.WaferFlatnessMeasure;

Run("square polygon includes boundary grid points", TestSquare);
Run("triangle polygon includes boundary and excludes outside points", TestTriangle);
Run("polygon requires at least three vertices", TestRequiresThreeVertices);
Run("polygon rejects collinear vertices", TestRejectsCollinearVertices);
Run("polygon mode enables point spacing", TestModeFlags);

Console.WriteLine("Custom wafer flatness polygon trajectory tests passed.");

void TestSquare()
{
    var model = CreatePolygonModel(1);
    Add(model, 0, 0);
    Add(model, 2, 0);
    Add(model, 2, 2);
    Add(model, 0, 2);

    var points = model.GenerateCircleLocusInfos();

    AssertEqual(9, points.Count, "square point count");
    AssertPointSet(points, new[] { (0d, 0d), (1d, 0d), (2d, 0d), (0d, 1d), (1d, 1d), (2d, 1d), (0d, 2d), (1d, 2d), (2d, 2d) });
}

void TestTriangle()
{
    var model = CreatePolygonModel(1);
    Add(model, 0, 0);
    Add(model, 2, 0);
    Add(model, 0, 2);

    var points = model.GenerateCircleLocusInfos();

    AssertEqual(6, points.Count, "triangle point count");
    AssertPointSet(points, new[] { (0d, 0d), (1d, 0d), (2d, 0d), (0d, 1d), (1d, 1d), (0d, 2d) });
    AssertFalse(ContainsPoint(points, 2, 1), "triangle excludes 2,1");
    AssertFalse(ContainsPoint(points, 1, 2), "triangle excludes 1,2");
}

void TestRequiresThreeVertices()
{
    var model = CreatePolygonModel(1);
    Add(model, 0, 0);
    Add(model, 1, 0);

    var exception = AssertThrows<InvalidOperationException>(() => model.GenerateCircleLocusInfos(), "two vertices");
    AssertTrue(exception.Message.Contains("至少", StringComparison.Ordinal), "message mentions at least");
}

void TestRejectsCollinearVertices()
{
    var model = CreatePolygonModel(1);
    Add(model, 0, 0);
    Add(model, 1, 1);
    Add(model, 2, 2);

    var exception = AssertThrows<InvalidOperationException>(() => model.GenerateCircleLocusInfos(), "collinear vertices");
    AssertTrue(exception.Message.Contains("面积", StringComparison.Ordinal), "message mentions area");
}

void TestModeFlags()
{
    var model = CreatePolygonModel(0.1);
    AssertTrue(model.IsPointGenerationMode, "point generation mode");
    AssertTrue(model.IsPointSpacingEditable, "point spacing visible");
    AssertTrue(model.IsCustomPolygonEditable, "polygon panel visible");
}

CreateTrajectoryModel CreatePolygonModel(double spacing) => new()
{
    CircleGenerateMode = CircleTrajectoryGenerateMode.自定义多边形,
    PointSpacing = spacing,
    DecimalPlaces = 3
};

void Add(CreateTrajectoryModel model, double x, double y) =>
    model.CustomPolygonPoints.Add(new CustomPolygonPoint { X = x, Y = y });

void AssertPointSet(IReadOnlyCollection<LocusInfo> actual, IReadOnlyCollection<(double X, double Y)> expected)
{
    foreach (var point in actual)
    {
        AssertEqual(LocusInfo.PointType, point.Type, "locus type");
        AssertClose(point.OriginX, point.TargetX, "target X");
        AssertClose(point.OriginY, point.TargetY, "target Y");
    }

    AssertEqual(expected.Count, actual.Count, "point set count");
    foreach (var (x, y) in expected)
        AssertTrue(ContainsPoint(actual, x, y), $"expected point ({x},{y})");
}

bool ContainsPoint(IEnumerable<LocusInfo> points, double x, double y) =>
    points.Any(point => Math.Abs(point.OriginX - x) < 0.000001 && Math.Abs(point.OriginY - y) < 0.000001);

TException AssertThrows<TException>(Action action, string label) where TException : Exception
{
    try { action(); }
    catch (TException ex) { return ex; }
    catch (Exception ex) { throw new InvalidOperationException($"{label}: expected {typeof(TException).Name}, got {ex.GetType().Name}"); }
    throw new InvalidOperationException($"{label}: expected {typeof(TException).Name}, no exception was thrown");
}

void Run(string name, Action test)
{
    try { test(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); Environment.Exit(1); }
}

void AssertClose(double expected, double actual, string label, double tolerance = 0.000001)
{
    if (Math.Abs(expected - actual) > tolerance)
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
}

void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
}

void AssertTrue(bool value, string label)
{
    if (!value) throw new InvalidOperationException(label);
}

void AssertFalse(bool value, string label)
{
    if (value) throw new InvalidOperationException(label);
}
```

- [ ] **Step 3: Verify RED**

Run:

```powershell
dotnet run --project CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj
```

Expected: build fails because `CustomPolygonPoint`, `CircleTrajectoryGenerateMode.自定义多边形`, and `IsCustomPolygonEditable` are missing.

---

### Task 2: Implement Polygon Model And Generator

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/CustomPolygonPoint.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/CreateTrajectoryModel.cs`
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] **Step 1: Add the vertex model**

Create `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/CustomPolygonPoint.cs`:

```csharp
using System;
using Prism.Mvvm;

namespace Custom.WaferFlatnessMeasure
{
    /// <summary>
    /// User-defined polygon vertex in absolute machine XY coordinates.
    /// </summary>
    [Serializable]
    public class CustomPolygonPoint : BindableBase
    {
        private double _x;
        private double _y;

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, double.IsFinite(value) ? value : 0);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, double.IsFinite(value) ? value : 0);
        }
    }
}
```

- [ ] **Step 2: Add polygon state to `CreateTrajectoryModel`**

Add fields near existing custom-ring fields:

```csharp
private double _customPolygonPointXInput;
private double _customPolygonPointYInput;
private ObservableCollection<CustomPolygonPoint> _customPolygonPoints = new ObservableCollection<CustomPolygonPoint>();
private readonly HashSet<CustomPolygonPoint> _subscribedCustomPolygonPoints = new HashSet<CustomPolygonPoint>();
```

Change the constructor to:

```csharp
public CreateTrajectoryModel()
{
    _customRingDefinitions.CollectionChanged += OnCustomRingDefinitionsChanged;
    _customPolygonPoints.CollectionChanged += OnCustomPolygonPointsChanged;
}
```

Add properties after `CustomRingDefinitions`:

```csharp
public double CustomPolygonPointXInput
{
    get => _customPolygonPointXInput;
    set => SetProperty(ref _customPolygonPointXInput, double.IsFinite(value) ? value : 0);
}

public double CustomPolygonPointYInput
{
    get => _customPolygonPointYInput;
    set => SetProperty(ref _customPolygonPointYInput, double.IsFinite(value) ? value : 0);
}

public ObservableCollection<CustomPolygonPoint> CustomPolygonPoints
{
    get => _customPolygonPoints;
    set
    {
        if (ReferenceEquals(_customPolygonPoints, value))
            return;

        DetachCustomPolygonPointListeners();
        if (_customPolygonPoints != null)
            _customPolygonPoints.CollectionChanged -= OnCustomPolygonPointsChanged;

        _customPolygonPoints = value ?? new ObservableCollection<CustomPolygonPoint>();
        _customPolygonPoints.CollectionChanged += OnCustomPolygonPointsChanged;
        RefreshCustomPolygonPointSubscriptions();

        RaisePropertyChanged();
        RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
    }
}
```

- [ ] **Step 3: Add mode flags and enum value**

In the `CircleGenerateMode` setter, raise:

```csharp
RaisePropertyChanged(nameof(IsCustomPolygonEditable));
RaisePropertyChanged(nameof(IsCircleGeometryEditable));
```

Update helper properties:

```csharp
[JsonIgnore]
public bool IsPointSpacingEditable =>
    CircleGenerateMode == CircleTrajectoryGenerateMode.等间距点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.内切正方形点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.内接同心圆点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形;

[JsonIgnore]
public bool IsPointGenerationMode =>
    CircleGenerateMode == CircleTrajectoryGenerateMode.内切圆心点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.等间距点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.内切正方形点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.内接同心圆点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.圆环点 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.自定义圆环 ||
    CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形;

[JsonIgnore]
public bool IsCustomPolygonEditable =>
    CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形;

[JsonIgnore]
public bool IsCircleGeometryEditable =>
    CircleGenerateMode != CircleTrajectoryGenerateMode.自定义多边形;
```

Add enum member:

```csharp
自定义圆环,
自定义多边形
```

- [ ] **Step 4: Add summary and validation routing**

At the start of `CircleGeneratorSummaryText`, before circle center/radius checks, add:

```csharp
if (CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形)
{
    if (!double.IsFinite(PointSpacing) || PointSpacing <= 0)
        return "请输入大于 0 的点间距。";

    var polygonPoints = GetCustomPolygonPoints();
    if (polygonPoints.Count < 3)
        return "请至少添加三个自定义多边形顶点。";

    if (Math.Abs(CalculatePolygonSignedArea(polygonPoints)) < 1e-9)
        return "自定义多边形面积过小，请检查顶点是否共线。";

    return $"已配置 {polygonPoints.Count} 个自定义多边形顶点，将按间距 {RoundCoordinate(PointSpacing)} 生成多边形内部点。";
}
```

In `BuildCircleLocusInfos`, add:

```csharp
CircleTrajectoryGenerateMode.自定义多边形 => BuildCustomPolygonPointLocusInfos(),
```

In `ValidateCircleGenerationParams`, wrap the circle center/radius checks in:

```csharp
if (IsCircleGeometryEditable)
{
    if (!double.IsFinite(CircleCenterX) || !double.IsFinite(CircleCenterY))
        throw new InvalidOperationException("圆心坐标必须是有效数值。");

    if (!double.IsFinite(CircleRadius) || CircleRadius <= 0)
        throw new InvalidOperationException("圆半径必须大于 0。");
}
```

After custom-ring validation, add:

```csharp
if (CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形)
    ValidateCustomPolygonPoints();
```

- [ ] **Step 5: Add add/remove and algorithm helpers**

Add public add/remove methods after `RemoveCustomRing`:

```csharp
public CustomPolygonPoint AddCustomPolygonPoint(double x, double y)
{
    var point = new CustomPolygonPoint { X = RoundCoordinate(x), Y = RoundCoordinate(y) };
    CustomPolygonPoints.Add(point);
    CustomPolygonPointXInput = point.X;
    CustomPolygonPointYInput = point.Y;
    return point;
}

public bool RemoveCustomPolygonPoint(CustomPolygonPoint? point)
{
    return point != null && CustomPolygonPoints.Remove(point);
}
```

Add generation and geometry helpers before `GenerateEquidistantPointOffsets`:

```csharp
private List<LocusInfo> BuildCustomPolygonPointLocusInfos()
{
    double spacing = NormalizePointSpacing();
    var polygonPoints = GetValidatedCustomPolygonPoints();
    var (minX, maxX, minY, maxY) = GetPolygonBounds(polygonPoints);
    var locusInfos = new List<LocusInfo>();

    for (double y = minY; y <= maxY + 1e-9; y += spacing)
    {
        for (double x = minX; x <= maxX + 1e-9; x += spacing)
        {
            if (IsPointInsideOrOnPolygon(x, y, polygonPoints))
                AddPointLocusInfo(locusInfos, x, y);
        }
    }

    if (locusInfos.Count == 0)
        throw new InvalidOperationException("自定义多边形内部未生成任何点，请检查点间距或顶点坐标。");

    return locusInfos;
}

private static (double MinX, double MaxX, double MinY, double MaxY) GetPolygonBounds(IReadOnlyCollection<(double X, double Y)> points)
{
    return (points.Min(p => p.X), points.Max(p => p.X), points.Min(p => p.Y), points.Max(p => p.Y));
}

private static bool IsPointInsideOrOnPolygon(double x, double y, IReadOnlyList<(double X, double Y)> points)
{
    bool inside = false;
    int previousIndex = points.Count - 1;

    for (int currentIndex = 0; currentIndex < points.Count; currentIndex++)
    {
        var current = points[currentIndex];
        var previous = points[previousIndex];

        if (IsPointOnSegment(x, y, previous.X, previous.Y, current.X, current.Y))
            return true;

        if ((current.Y > y) != (previous.Y > y))
        {
            double intersectionX = (previous.X - current.X) * (y - current.Y) / (previous.Y - current.Y) + current.X;
            if (x < intersectionX)
                inside = !inside;
        }

        previousIndex = currentIndex;
    }

    return inside;
}

private static bool IsPointOnSegment(double pointX, double pointY, double startX, double startY, double endX, double endY)
{
    const double tolerance = 1e-9;
    double cross = (pointX - startX) * (endY - startY) - (pointY - startY) * (endX - startX);
    if (Math.Abs(cross) > tolerance)
        return false;

    double dot = (pointX - startX) * (pointX - endX) + (pointY - startY) * (pointY - endY);
    return dot <= tolerance;
}
```

Add validation helpers near `GetValidatedCustomRingRadii`:

```csharp
private void ValidateCustomPolygonPoints()
{
    var points = GetCustomPolygonPoints();
    if (points.Count < 3)
        throw new InvalidOperationException("请至少添加三个自定义多边形顶点。");

    if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
        throw new InvalidOperationException("自定义多边形顶点坐标必须是有效数值。");

    if (Math.Abs(CalculatePolygonSignedArea(points)) < 1e-9)
        throw new InvalidOperationException("自定义多边形面积过小，请检查顶点是否共线。");
}

private List<(double X, double Y)> GetCustomPolygonPoints()
{
    return CustomPolygonPoints?
        .Where(point => point != null)
        .Select(point => (point.X, point.Y))
        .ToList() ?? new List<(double X, double Y)>();
}

private List<(double X, double Y)> GetValidatedCustomPolygonPoints()
{
    ValidateCustomPolygonPoints();
    return GetCustomPolygonPoints();
}

private static double CalculatePolygonSignedArea(IReadOnlyList<(double X, double Y)> points)
{
    double area = 0;
    for (int index = 0; index < points.Count; index++)
    {
        var current = points[index];
        var next = points[(index + 1) % points.Count];
        area += current.X * next.Y - next.X * current.Y;
    }

    return area / 2.0;
}
```

Add collection handlers near `OnCustomRingDefinitionsChanged`:

```csharp
private void OnCustomPolygonPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    RefreshCustomPolygonPointSubscriptions();
    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
}

private void RefreshCustomPolygonPointSubscriptions()
{
    DetachCustomPolygonPointListeners();
    foreach (var point in _customPolygonPoints.Where(point => point != null))
    {
        point.PropertyChanged -= OnCustomPolygonPointPropertyChanged;
        point.PropertyChanged += OnCustomPolygonPointPropertyChanged;
        _subscribedCustomPolygonPoints.Add(point);
    }
}

private void DetachCustomPolygonPointListeners()
{
    foreach (var point in _subscribedCustomPolygonPoints)
        point.PropertyChanged -= OnCustomPolygonPointPropertyChanged;

    _subscribedCustomPolygonPoints.Clear();
}

private void OnCustomPolygonPointPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
{
    RaisePropertyChanged(nameof(CircleGeneratorSummaryText));
}
```

- [ ] **Step 6: Verify GREEN**

Run:

```powershell
dotnet run --project CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj
```

Expected: all five tests print `PASS`, then `Custom wafer flatness polygon trajectory tests passed.`

---

### Task 3: Add ViewModel Commands And Preview Outline

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessConfigViewModel.cs`
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] **Step 1: Add selection state**

Add field:

```csharp
private ObservableCollection<CustomPolygonPoint>? _observedCustomPolygonPoints;
```

Add property after `SelectedCustomRing`:

```csharp
private CustomPolygonPoint? _selectedCustomPolygonPoint;
public CustomPolygonPoint? SelectedCustomPolygonPoint
{
    get => _selectedCustomPolygonPoint;
    set
    {
        if (SetProperty(ref _selectedCustomPolygonPoint, value))
            RefreshTrajectoryShapes();
    }
}
```

In `InitParam`, after `SelectedCustomRing`, add:

```csharp
SelectedCustomPolygonPoint ??= ModelParam.CreateTrajectoryModel.CustomPolygonPoints.FirstOrDefault();
```

- [ ] **Step 2: Add add/remove commands**

Add commands after `RemoveCustomRingCommand`:

```csharp
public DelegateCommand AddCustomPolygonPointCommand => new DelegateCommand(() =>
{
    try
    {
        var trajectoryModel = ModelParam?.CreateTrajectoryModel;
        if (trajectoryModel == null)
            return;

        SelectedCustomPolygonPoint = trajectoryModel.AddCustomPolygonPoint(
            trajectoryModel.CustomPolygonPointXInput,
            trajectoryModel.CustomPolygonPointYInput);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "添加多边形顶点失败", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
});

public DelegateCommand RemoveCustomPolygonPointCommand => new DelegateCommand(() =>
{
    var trajectoryModel = ModelParam?.CreateTrajectoryModel;
    var polygonPoints = trajectoryModel?.CustomPolygonPoints;
    if (polygonPoints == null || polygonPoints.Count == 0)
        return;

    if (SelectedCustomPolygonPoint == null)
    {
        MessageBox.Show("请先选中要删除的多边形顶点。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    int selectedIndex = polygonPoints.IndexOf(SelectedCustomPolygonPoint);
    if (selectedIndex < 0 || trajectoryModel == null || !trajectoryModel.RemoveCustomPolygonPoint(SelectedCustomPolygonPoint))
        return;

    SelectedCustomPolygonPoint = polygonPoints.Count == 0
        ? null
        : polygonPoints[Math.Min(selectedIndex, polygonPoints.Count - 1)];
});
```

- [ ] **Step 3: Attach polygon collection**

In `AttachCreateTrajectoryModelListeners`, add:

```csharp
AttachCustomPolygonPointListeners(_observedCreateTrajectoryModel?.CustomPolygonPoints);
```

Add methods:

```csharp
private void AttachCustomPolygonPointListeners(ObservableCollection<CustomPolygonPoint>? polygonPoints)
{
    if (!ReferenceEquals(_observedCustomPolygonPoints, polygonPoints) && _observedCustomPolygonPoints != null)
        _observedCustomPolygonPoints.CollectionChanged -= OnCustomPolygonPointCollectionChanged;

    _observedCustomPolygonPoints = polygonPoints;
    if (_observedCustomPolygonPoints != null)
    {
        _observedCustomPolygonPoints.CollectionChanged -= OnCustomPolygonPointCollectionChanged;
        _observedCustomPolygonPoints.CollectionChanged += OnCustomPolygonPointCollectionChanged;
    }
}

private void OnCustomPolygonPointCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    if (SelectedCustomPolygonPoint != null && !(_observedCustomPolygonPoints?.Contains(SelectedCustomPolygonPoint) ?? false))
    {
        SelectedCustomPolygonPoint = _observedCustomPolygonPoints?.FirstOrDefault();
        return;
    }

    RefreshTrajectoryShapes();
}
```

In `OnCreateTrajectoryModelPropertyChanged`, add:

```csharp
if (e.PropertyName == nameof(CreateTrajectoryModel.CustomPolygonPoints))
{
    AttachCustomPolygonPointListeners(_observedCreateTrajectoryModel?.CustomPolygonPoints);
    if (SelectedCustomPolygonPoint != null && !(_observedCreateTrajectoryModel?.CustomPolygonPoints?.Contains(SelectedCustomPolygonPoint) ?? false))
    {
        SelectedCustomPolygonPoint = _observedCreateTrajectoryModel?.CustomPolygonPoints?.FirstOrDefault();
        return;
    }
}
```

- [ ] **Step 4: Draw polygon outline in preview**

In `RefreshTrajectoryShapes`, include polygon vertices in bounds:

```csharp
if (model.CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形 && model.CustomPolygonPoints != null)
{
    foreach (var point in model.CustomPolygonPoints)
    {
        if (point == null || !double.IsFinite(point.X) || !double.IsFinite(point.Y))
            continue;

        minX = Math.Min(minX, point.X);
        maxX = Math.Max(maxX, point.X);
        minY = Math.Min(minY, point.Y);
        maxY = Math.Max(maxY, point.Y);
        hasBounds = true;
    }
}
```

Change circle boundary drawing so it is skipped for polygon mode:

```csharp
if (model.CircleGenerateMode != CircleTrajectoryGenerateMode.自定义多边形 &&
    double.IsFinite(model.CircleRadius) &&
    model.CircleRadius > 0)
```

Before locus drawing, add:

```csharp
if (model.CircleGenerateMode == CircleTrajectoryGenerateMode.自定义多边形 &&
    model.CustomPolygonPoints != null &&
    model.CustomPolygonPoints.Count >= 3)
{
    var polygon = new Polygon
    {
        Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x88, 0x66)),
        StrokeThickness = 2,
        StrokeDashArray = new DoubleCollection { 6, 3 },
        Fill = new SolidColorBrush(Color.FromArgb(0x18, 0x00, 0x88, 0x66))
    };

    foreach (var point in model.CustomPolygonPoints)
    {
        if (point != null && double.IsFinite(point.X) && double.IsFinite(point.Y))
            polygon.Points.Add(new Point(MapX(point.X), MapY(point.Y)));
    }

    if (polygon.Points.Count >= 3)
        TrajectoryShapes.Add(polygon);
}
```

- [ ] **Step 5: Verify tests still pass**

Run:

```powershell
dotnet run --project CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj
```

Expected: all tests still pass.

---

### Task 4: Add XAML Polygon Controls

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml`

- [ ] **Step 1: Hide circle radius for polygon mode**

Add visibility to the existing outer-radius `StackPanel`:

```xml
<StackPanel Orientation="Horizontal"
            HorizontalAlignment="Center"
            Visibility="{Binding ModelParam.CreateTrajectoryModel.IsCircleGeometryEditable,Converter={StaticResource BoolToVisibilityConverter}}">
```

- [ ] **Step 2: Add polygon editor panel**

Insert after the custom-ring parameter panel:

```xml
<Border Margin="8,10,8,0" Padding="10" CornerRadius="6" BorderBrush="#D8E2EE" BorderThickness="1"
        Visibility="{Binding ModelParam.CreateTrajectoryModel.IsCustomPolygonEditable,Converter={StaticResource BoolToVisibilityConverter}}">
    <StackPanel>
        <TextBlock Margin="0,0,0,8" FontWeight="SemiBold" Foreground="#2E5573" Text="自定义多边形参数"/>
        <TextBlock Margin="8,0,8,8" Foreground="#6A7480" TextWrapping="Wrap"
                   Text="顶点使用绝对 XY 坐标，请按多边形边界顺序输入；至少需要 3 个顶点，建议不要输入自交多边形。"/>
        <StackPanel Orientation="Horizontal" Margin="0,8,0,0" HorizontalAlignment="Center">
            <hc:NumericUpDown hc:InfoElement.TitlePlacement="Left" hc:InfoElement.Title="顶点 X/Y：" hc:InfoElement.TitleWidth="100"
                              Width="230" DecimalPlaces="3" Minimum="-100000" Maximum="100000" BorderThickness="0 0 0 1" Style="{StaticResource NumericUpDownExtend}"
                              HorizontalContentAlignment="Center"
                              Value="{Binding ModelParam.CreateTrajectoryModel.CustomPolygonPointXInput,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
            <hc:NumericUpDown Width="120" DecimalPlaces="3" Minimum="-100000" Maximum="100000" BorderThickness="0 0 0 1" Style="{StaticResource NumericUpDownExtend}"
                              HorizontalContentAlignment="Center"
                              Value="{Binding ModelParam.CreateTrajectoryModel.CustomPolygonPointYInput,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
            <Button Width="80" Height="28" Margin="8,0,0,0" Command="{Binding AddCustomPolygonPointCommand}" Style="{StaticResource GeneralButtonStyle}" Content="添加"/>
            <Button Width="80" Height="28" Margin="8,0,0,0" Command="{Binding RemoveCustomPolygonPointCommand}" Style="{StaticResource GeneralButtonStyle}" Content="删除"/>
        </StackPanel>
        <ListBox Height="150" Margin="8,10,8,0"
                 ItemsSource="{Binding ModelParam.CreateTrajectoryModel.CustomPolygonPoints}"
                 SelectedItem="{Binding SelectedCustomPolygonPoint,Mode=TwoWay}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Margin="4">
                        <TextBlock Width="60" VerticalAlignment="Center" Text="顶点"/>
                        <hc:NumericUpDown Width="170" Height="28" DecimalPlaces="3" Minimum="-100000" Maximum="100000" BorderThickness="0 0 0 1"
                                          Value="{Binding X,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
                        <hc:NumericUpDown Width="170" Height="28" Margin="8,0,0,0" DecimalPlaces="3" Minimum="-100000" Maximum="100000" BorderThickness="0 0 0 1"
                                          Value="{Binding Y,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </StackPanel>
</Border>
```

- [ ] **Step 3: Build production module**

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj
```

Expected: build succeeds with `0 Error(s)`.

---

### Task 5: Final Verification

**Files:**
- Verify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`
- Verify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [ ] **Step 1: Run focused tests**

Run:

```powershell
dotnet run --project CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj
```

Expected output contains all five `PASS` lines and `Custom wafer flatness polygon trajectory tests passed.`

- [ ] **Step 2: Build module**

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 3: Manual UI check**

Open Wafer Flatness config, select `轨迹生成`, choose `自定义多边形`, add `(0,0)`, `(2,0)`, `(2,2)`, `(0,2)`, set `点间距` to `1`, click `生成轨迹`, and confirm the preview shows a closed polygon plus 9 point loci.

- [ ] **Step 4: Record git limitation**

Run:

```powershell
git status --short
```

Expected here: `fatal: not a git repository (or any of the parent directories): .git`.

# AlarmCenter LightningChart Navigation Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce `ReeYin.AlarmCenter` statistics page-switch stalls by keeping LightningChart but suppressing unchanged redraws after the first chart render.

**Architecture:** Keep chart ownership in the two existing UserControls and keep the ViewModel free of LightningChart types. Add per-control data signatures, dirty flags, and coalesced dispatcher rendering so `Loaded` and collection notifications only rebuild LightningChart content when bound data changed.

**Tech Stack:** .NET 8 WPF, Prism, LightningChart WPF MVVM, source-level functional checks in `Scratch/AlarmCenterFunctionalTests`, `dotnet run`, `dotnet build`.

---

## Working Notes

- Spec: `docs/superpowers/specs/2026-06-18-alarmcenter-lightningchart-navigation-performance-design.md`
- Workspace root: current Codex working directory.
- Git note: `git status --short` at the workspace root returned `fatal: not a git repository`, so this plan uses validation checkpoints instead of commit steps in this workspace.

## File Structure

- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`
  - Owns executable source-level regression checks for AlarmCenter behavior.
  - The relevant test should fail first by requiring data-signature render suppression in both LightningChart wrapper controls.

- Modify: `Application/ReeYin.AlarmCenter/Controls/AlarmTrendBarLightningChart.cs`
  - Owns the statistics bar LightningChart instance.
  - Add dirty/signature state and render only when `AlarmTrendPoint` data changes.

- Modify: `Application/ReeYin.AlarmCenter/Controls/AlarmTypePieLightningChart.cs`
  - Owns the type-share pie LightningChart instance.
  - Add dirty/signature state and render only when `AlarmPieSliceItem` data changes.

---

### Task 1: Add Failing Regression Checks

**Files:**
- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] **Step 1: Rename the chart lifecycle test label**

In the top-level `tests` array, replace:

```csharp
("AlarmStatistics LightningChart controls own chart lifecycle", TestStatisticsChartControlsOwnLifecycleAsync),
```

with:

```csharp
("AlarmStatistics LightningChart controls suppress unchanged redraws", TestStatisticsChartControlsOwnLifecycleAsync),
```

- [ ] **Step 2: Replace `TestStatisticsChartControlsOwnLifecycleAsync`**

Replace the existing method with this complete method:

```csharp
static Task TestStatisticsChartControlsOwnLifecycleAsync()
{
    string workspaceRoot = FindWorkspaceRoot();
    string barChartPath = Path.Combine(workspaceRoot, "Application", "ReeYin.AlarmCenter", "Controls", "AlarmTrendBarLightningChart.cs");
    string pieChartPath = Path.Combine(workspaceRoot, "Application", "ReeYin.AlarmCenter", "Controls", "AlarmTypePieLightningChart.cs");
    string viewModelPath = Path.Combine(workspaceRoot, "Application", "ReeYin.AlarmCenter", "ViewModels", "AlarmWorkbenchShellViewModel.cs");
    string barChartSource = File.Exists(barChartPath) ? File.ReadAllText(barChartPath) : string.Empty;
    string pieChartSource = File.Exists(pieChartPath) ? File.ReadAllText(pieChartPath) : string.Empty;
    string viewModelSource = File.ReadAllText(viewModelPath);
    string barLoadedBody = ExtractMethodBlock(barChartSource, "private void OnLoaded");
    string pieLoadedBody = ExtractMethodBlock(pieChartSource, "private void OnLoaded");

    Assert(barChartSource.Contains("new LightningChart", StringComparison.Ordinal), "bar chart control should own the LightningChart instance");
    Assert(barChartSource.Contains("new ViewXY(chart)", StringComparison.Ordinal), "bar chart control should create ViewXY with the real chart owner after load");
    Assert(barChartSource.Contains("DispatcherPriority.ContextIdle", StringComparison.Ordinal), "bar chart rendering should defer heavy LightningChart updates to UI idle");
    Assert(barChartSource.Contains("IsHitTestVisible = false", StringComparison.Ordinal), "bar chart should not capture mouse input while switching pages");
    Assert(pieChartSource.Contains("new LightningChart", StringComparison.Ordinal), "pie chart control should own the LightningChart instance");
    Assert(pieChartSource.Contains("new Pie3DView(chart)", StringComparison.Ordinal), "pie chart control should create ViewPie3D with the real chart owner after load");
    Assert(pieChartSource.Contains("DispatcherPriority.ContextIdle", StringComparison.Ordinal), "pie chart rendering should defer heavy LightningChart updates to UI idle");
    Assert(pieChartSource.Contains("IsHitTestVisible = false", StringComparison.Ordinal), "pie chart should not capture mouse input while switching pages");

    Assert(barChartSource.Contains("private string? _lastRenderedSignature", StringComparison.Ordinal), "bar chart should cache the last rendered data signature");
    Assert(barChartSource.Contains("private bool _isRenderDirty = true", StringComparison.Ordinal), "bar chart should start dirty for the first render");
    Assert(barChartSource.Contains("private void MarkRenderDirty()", StringComparison.Ordinal), "bar chart collection updates should mark data dirty before scheduling");
    Assert(barChartSource.Contains("string currentSignature = BuildDataSignature(items, maximum)", StringComparison.Ordinal), "bar chart should calculate a data signature before rendering");
    Assert(barChartSource.Contains("string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal)", StringComparison.Ordinal), "bar chart should skip rendering when the data signature is unchanged");
    Assert(barChartSource.Contains("RenderChart(items, maximum)", StringComparison.Ordinal), "bar chart render should reuse the signature input snapshot");
    Assert(barChartSource.Contains("_lastRenderedSignature = currentSignature", StringComparison.Ordinal), "bar chart should store the signature only after a successful render");
    Assert(barChartSource.Contains("if (!IsLoaded)", StringComparison.Ordinal) && barChartSource.Contains("_isRenderDirty = true;", StringComparison.Ordinal), "bar chart should keep dirty state when render is requested while hidden");
    Assert(!barLoadedBody.Contains("MarkRenderDirty(", StringComparison.Ordinal), "bar chart Loaded should not mark unchanged data dirty");

    Assert(pieChartSource.Contains("private string? _lastRenderedSignature", StringComparison.Ordinal), "pie chart should cache the last rendered data signature");
    Assert(pieChartSource.Contains("private bool _isRenderDirty = true", StringComparison.Ordinal), "pie chart should start dirty for the first render");
    Assert(pieChartSource.Contains("private void MarkRenderDirty()", StringComparison.Ordinal), "pie chart collection updates should mark data dirty before scheduling");
    Assert(pieChartSource.Contains("string currentSignature = BuildDataSignature(items)", StringComparison.Ordinal), "pie chart should calculate a data signature before rendering");
    Assert(pieChartSource.Contains("string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal)", StringComparison.Ordinal), "pie chart should skip rendering when the data signature is unchanged");
    Assert(pieChartSource.Contains("RenderChart(items)", StringComparison.Ordinal), "pie chart render should reuse the signature input snapshot");
    Assert(pieChartSource.Contains("_lastRenderedSignature = currentSignature", StringComparison.Ordinal), "pie chart should store the signature only after a successful render");
    Assert(pieChartSource.Contains("if (!IsLoaded)", StringComparison.Ordinal) && pieChartSource.Contains("_isRenderDirty = true;", StringComparison.Ordinal), "pie chart should keep dirty state when render is requested while hidden");
    Assert(!pieLoadedBody.Contains("MarkRenderDirty(", StringComparison.Ordinal), "pie chart Loaded should not mark unchanged data dirty");

    Assert(!viewModelSource.Contains("BuildVisibleStatisticsCharts", StringComparison.Ordinal), "view model should not build LightningChart visuals");
    Assert(!viewModelSource.Contains("CreateTrendBarChartView", StringComparison.Ordinal), "view model should not create LightningChart ViewXY objects");
    Assert(!viewModelSource.Contains("CreateTypePieChartView", StringComparison.Ordinal), "view model should not create LightningChart pie views");
    return Task.CompletedTask;
}
```

- [ ] **Step 3: Run the failing test suite**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
```

Expected: the suite fails at `AlarmStatistics LightningChart controls suppress unchanged redraws` because `_lastRenderedSignature`, `_isRenderDirty`, `MarkRenderDirty`, and `BuildDataSignature` are not present yet.

---

### Task 2: Add Signature-Gated Rendering To The Bar Chart

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Controls/AlarmTrendBarLightningChart.cs`

- [ ] **Step 1: Add `System.Text`**

Add this using with the other `System.*` usings:

```csharp
using System.Text;
```

- [ ] **Step 2: Add render state fields**

Replace the current field block:

```csharp
private LightningChart? _chart;
private ViewXY? _view;
private INotifyCollectionChanged? _collectionChanged;
private bool _renderScheduled;
```

with:

```csharp
private LightningChart? _chart;
private ViewXY? _view;
private INotifyCollectionChanged? _collectionChanged;
private string? _lastRenderedSignature;
private bool _isRenderDirty = true;
private bool _renderScheduled;
```

- [ ] **Step 3: Mark dirty from data changes only**

In `OnItemsSourceChanged`, replace:

```csharp
chart.ScheduleRender();
```

with:

```csharp
chart.MarkRenderDirty();
```

Replace the whole `OnCollectionChanged` method with:

```csharp
private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    MarkRenderDirty();
}
```

Keep `OnLoaded` as:

```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    EnsureChart();
    ScheduleRender();
}
```

- [ ] **Step 4: Replace render scheduling and safe render methods**

Replace the current `ScheduleRender()` and `RenderSafely()` methods with:

```csharp
private void MarkRenderDirty()
{
    _isRenderDirty = true;
    ScheduleRender();
}

private void ScheduleRender()
{
    if (_renderScheduled)
    {
        return;
    }

    if (!IsLoaded)
    {
        _isRenderDirty = true;
        return;
    }

    _renderScheduled = true;
    Dispatcher.BeginInvoke(new Action(() =>
    {
        _renderScheduled = false;
        RenderSafely();
    }), DispatcherPriority.ContextIdle);
}

private void RenderSafely()
{
    if (!IsLoaded)
    {
        _isRenderDirty = true;
        return;
    }

    try
    {
        EnsureChart();
        List<AlarmTrendPoint> items = GetTrendItems();
        int maximum = GetMaximum(items);
        string currentSignature = BuildDataSignature(items, maximum);
        if (!_isRenderDirty && string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal))
        {
            _fallbackText.Visibility = Visibility.Collapsed;
            return;
        }

        RenderChart(items, maximum);
        _lastRenderedSignature = currentSignature;
        _isRenderDirty = false;
        _fallbackText.Visibility = Visibility.Collapsed;
    }
    catch (Exception ex)
    {
        _isRenderDirty = true;
        _fallbackText.Text = $"LightningChart 初始化失败：{ex.Message}";
        _fallbackText.Visibility = Visibility.Visible;
    }
}
```

- [ ] **Step 5: Replace `RenderChart` with a snapshot-based overload**

Replace the current `RenderChart()` method with:

```csharp
private void RenderChart(IReadOnlyList<AlarmTrendPoint> items, int maximum)
{
    if (_chart == null || _view == null)
    {
        return;
    }

    AxisXCollection xAxes = CreateCategoryAxisCollection(_view, items);
    AxisYCollection yAxes = CreateValueAxisCollection(_view, maximum);

    BarSeries series = new BarSeries
    {
        AllowUserInteraction = false,
        AssignXAxisIndex = 0,
        AssignYAxisIndex = 0,
        BarThickness = 18,
        BorderColor = Color.FromRgb(30, 64, 175),
        BorderWidth = 0,
        CursorTrackEnabled = false,
        Fill = CreateSolidFill(Color.FromRgb(37, 99, 235)),
        IncludeInAutoFit = true,
        LabelStyle = new BarLabelsStyle
        {
            AllowUserInteraction = false,
            Color = Color.FromRgb(30, 41, 59),
            Distance = 4,
            Font = new WpfFont("Microsoft YaHei UI", 11, true, false),
            HorizAlign = BarsTitleHorizontalAlign.BarCenter,
            Outside = true,
            VerticalAlign = BarsTitleVerticalAlign.BarTop,
            Visible = true
        },
        ShowInLegendBox = false,
        Values = items.Select((item, index) => new BarSeriesValue(index, item.Count, item.Count.ToString(CultureInfo.InvariantCulture))).ToArray()
    };

    _chart.BeginUpdate();
    try
    {
        _view.XAxes = xAxes;
        _view.YAxes = yAxes;
        _view.BarSeries = new BarSeriesCollection { series };
    }
    finally
    {
        _chart.EndUpdate();
    }
}
```

- [ ] **Step 6: Add bar data helper methods**

Add these helper methods between `RenderChart(...)` and `CreateView(...)`:

```csharp
private List<AlarmTrendPoint> GetTrendItems()
{
    return ItemsSource?.OfType<AlarmTrendPoint>().ToList() ?? new List<AlarmTrendPoint>();
}

private static int GetMaximum(IReadOnlyCollection<AlarmTrendPoint> items)
{
    return Math.Max(1, items.Count == 0 ? 1 : items.Max(item => item.Count));
}

private static string BuildDataSignature(IReadOnlyList<AlarmTrendPoint> items, int maximum)
{
    StringBuilder builder = new StringBuilder();
    builder.Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('|');
    builder.Append(maximum.ToString(CultureInfo.InvariantCulture)).Append('|');
    foreach (AlarmTrendPoint item in items)
    {
        AppendSegment(builder, item.Label);
        builder.Append(item.Count.ToString(CultureInfo.InvariantCulture)).Append(';');
    }

    return builder.ToString();
}

private static void AppendSegment(StringBuilder builder, string? value)
{
    value ??= string.Empty;
    builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
}
```

- [ ] **Step 7: Run the tests and expect the pie assertions to remain red**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
```

Expected: bar-specific assertions now pass, but the same lifecycle test still fails for the pie chart because it does not have signature-gated rendering yet.

---

### Task 3: Add Signature-Gated Rendering To The Pie Chart

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Controls/AlarmTypePieLightningChart.cs`

- [ ] **Step 1: Add required usings**

Add these usings with the existing `System.*` usings:

```csharp
using System.Globalization;
using System.Text;
```

- [ ] **Step 2: Add render state fields**

Replace the current field block:

```csharp
private LightningChart? _chart;
private Pie3DView? _view;
private INotifyCollectionChanged? _collectionChanged;
private bool _renderScheduled;
```

with:

```csharp
private LightningChart? _chart;
private Pie3DView? _view;
private INotifyCollectionChanged? _collectionChanged;
private string? _lastRenderedSignature;
private bool _isRenderDirty = true;
private bool _renderScheduled;
```

- [ ] **Step 3: Mark dirty from data changes only**

In `OnItemsSourceChanged`, replace:

```csharp
chart.ScheduleRender();
```

with:

```csharp
chart.MarkRenderDirty();
```

Replace the whole `OnCollectionChanged` method with:

```csharp
private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    MarkRenderDirty();
}
```

Keep `OnLoaded` as:

```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
    EnsureChart();
    ScheduleRender();
}
```

- [ ] **Step 4: Replace render scheduling and safe render methods**

Replace the current `ScheduleRender()` and `RenderSafely()` methods with:

```csharp
private void MarkRenderDirty()
{
    _isRenderDirty = true;
    ScheduleRender();
}

private void ScheduleRender()
{
    if (_renderScheduled)
    {
        return;
    }

    if (!IsLoaded)
    {
        _isRenderDirty = true;
        return;
    }

    _renderScheduled = true;
    Dispatcher.BeginInvoke(new Action(() =>
    {
        _renderScheduled = false;
        RenderSafely();
    }), DispatcherPriority.ContextIdle);
}

private void RenderSafely()
{
    if (!IsLoaded)
    {
        _isRenderDirty = true;
        return;
    }

    try
    {
        EnsureChart();
        List<AlarmPieSliceItem> items = GetPieItems();
        string currentSignature = BuildDataSignature(items);
        if (!_isRenderDirty && string.Equals(currentSignature, _lastRenderedSignature, StringComparison.Ordinal))
        {
            _fallbackText.Visibility = Visibility.Collapsed;
            return;
        }

        RenderChart(items);
        _lastRenderedSignature = currentSignature;
        _isRenderDirty = false;
        _fallbackText.Visibility = Visibility.Collapsed;
    }
    catch (Exception ex)
    {
        _isRenderDirty = true;
        _fallbackText.Text = $"LightningChart 初始化失败：{ex.Message}";
        _fallbackText.Visibility = Visibility.Visible;
    }
}
```

- [ ] **Step 5: Replace `RenderChart` with a snapshot-based overload**

Replace the current `RenderChart()` method with:

```csharp
private void RenderChart(IReadOnlyList<AlarmPieSliceItem> items)
{
    if (_chart == null || _view == null)
    {
        return;
    }

    PieSliceCollection values = new PieSliceCollection();

    foreach (AlarmPieSliceItem item in items)
    {
        PieSlice slice = new PieSlice
        {
            AllowUserInteraction = false,
            BlinkOnOver = false,
            Color = ResolveSliceColor(item),
            ShowInLegendBox = false,
            TitleAlignment = AlignmentPie3DTitle.Outside,
            Value = item.Count
        };
        values.Add(slice);
    }

    _chart.BeginUpdate();
    try
    {
        _view.Values = values;
    }
    finally
    {
        _chart.EndUpdate();
    }
}
```

- [ ] **Step 6: Add pie data helper methods**

Add these helper methods between `RenderChart(...)` and `CreateView(...)`:

```csharp
private List<AlarmPieSliceItem> GetPieItems()
{
    return ItemsSource?.OfType<AlarmPieSliceItem>()
        .Where(item => item.Count > 0)
        .ToList() ?? new List<AlarmPieSliceItem>();
}

private static string BuildDataSignature(IReadOnlyList<AlarmPieSliceItem> items)
{
    StringBuilder builder = new StringBuilder();
    builder.Append(items.Count.ToString(CultureInfo.InvariantCulture)).Append('|');
    foreach (AlarmPieSliceItem item in items)
    {
        Color color = ResolveSliceColor(item);
        AppendSegment(builder, item.Label);
        builder.Append(item.Count.ToString(CultureInfo.InvariantCulture)).Append(';');
        builder.Append(item.Percentage.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        builder.Append(color.A.ToString(CultureInfo.InvariantCulture)).Append(',');
        builder.Append(color.R.ToString(CultureInfo.InvariantCulture)).Append(',');
        builder.Append(color.G.ToString(CultureInfo.InvariantCulture)).Append(',');
        builder.Append(color.B.ToString(CultureInfo.InvariantCulture)).Append(';');
    }

    return builder.ToString();
}

private static Color ResolveSliceColor(AlarmPieSliceItem item)
{
    return item.FillBrush is SolidColorBrush brush ? brush.Color : Colors.SteelBlue;
}

private static void AppendSegment(StringBuilder builder, string? value)
{
    value ??= string.Empty;
    builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
}
```

- [ ] **Step 7: Run the test suite and expect green**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
```

Expected: all AlarmCenter functional tests pass with `RESULT Passed=21; Failed=0`.

---

### Task 4: Project Build And Final Verification

**Files:**
- Read: `Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj`
- Read: `Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj`

- [ ] **Step 1: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj
```

Expected: `Build succeeded` with zero errors.

- [ ] **Step 2: Re-run functional tests after the build**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
```

Expected: `RESULT Passed=21; Failed=0`.

- [ ] **Step 3: Source scan for protected architecture**

Run:

```powershell
rg -n "BuildVisibleStatisticsCharts|CreateTrendBarChartView|CreateTypePieChartView|ReleaseStatisticsChartViews|Arction.Wpf.ChartingMVVM" Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs
```

Expected: no matches. This confirms the ViewModel still does not own LightningChart objects or release chart views during normal navigation.

- [ ] **Step 4: Source scan for new render gates**

Run:

```powershell
rg -n "_lastRenderedSignature|_isRenderDirty|BuildDataSignature|MarkRenderDirty|RenderChart\\(items" Application/ReeYin.AlarmCenter/Controls/AlarmTrendBarLightningChart.cs Application/ReeYin.AlarmCenter/Controls/AlarmTypePieLightningChart.cs
```

Expected: matches in both chart control files for signature caching, dirty tracking, and snapshot-based rendering.

---

## Plan Self-Review

- Spec coverage: Tasks 2 and 3 implement data signatures, dirty tracking, loaded/unloaded behavior, and control-owned rendering. Task 1 protects the behavior with failing tests. Task 4 verifies tests and build.
- Completion-marker scan: no unfinished markers remain.
- Type consistency: field names, method names, and test assertions consistently use `_lastRenderedSignature`, `_isRenderDirty`, `MarkRenderDirty`, `BuildDataSignature`, and snapshot-based `RenderChart(...)`.
- Scope check: the plan keeps LightningChart and does not remove Arction references, redesign XAML, or add new chart interactions.

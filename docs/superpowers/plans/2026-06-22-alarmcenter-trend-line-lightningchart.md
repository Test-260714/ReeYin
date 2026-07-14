# AlarmCenter Trend Line LightningChart Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the old daily trend card/list body with an owned LightingChart line chart in the AlarmCenter statistics trend tab.

**Architecture:** Add a focused WPF `UserControl` that owns `LightningChart` lifecycle and consumes the existing lightweight `AlarmTrendPoint` collection. Keep `AlarmWorkbenchShellViewModel` chart-lifecycle free and update only XAML composition plus structural functional tests.

**Tech Stack:** WPF, Prism bindings, Arction/LightingChart MVVM API, .NET 8, local functional console tests.

---

## File Structure

- Create `Application/ReeYin.AlarmCenter/Controls/AlarmTrendLineLightningChart.cs`: owned LightingChart wrapper for trend line rendering.
- Modify `Application/ReeYin.AlarmCenter/Views/AlarmStatisticsView.xaml`: replace trend tab `ScrollViewer`/`ItemsControl` body with the new chart and empty overlay.
- Modify `Scratch/AlarmCenterFunctionalTests/Program.cs`: add structural tests for the line chart replacement and lifecycle ownership.

### Task 1: Failing Functional Test

**Files:**
- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] **Step 1: Write the failing test**

Add assertions that `AlarmStatisticsView.xaml` contains `<charts:AlarmTrendLineLightningChart ItemsSource="{Binding TrendPoints}"`, does not contain `<ItemsControl ItemsSource="{Binding TrendPoints}"`, and that `AlarmTrendLineLightningChart.cs` contains `new LightningChart`, `new ViewXY(chart)`, `PointLineSeries`, `DispatcherPriority.ContextIdle`, `BuildDataSignature`, and `ShowFallback`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore`

Expected: FAIL because `AlarmTrendLineLightningChart.cs` and the XAML usage do not exist yet.

### Task 2: Line Chart Wrapper

**Files:**
- Create: `Application/ReeYin.AlarmCenter/Controls/AlarmTrendLineLightningChart.cs`

- [ ] **Step 1: Implement minimal chart wrapper**

Create a sealed `UserControl` with an `ItemsSource` dependency property, delayed `LightningChart` creation, collection change subscription, dirty signature caching, and render fallback behavior copied from the existing bar chart lifecycle pattern.

- [ ] **Step 2: Render trend data as a line**

Use `PointLineSeries` with `SeriesPoint[]` values where X is the item index and Y is `AlarmTrendPoint.Count`. Create custom X ticks from `AlarmTrendPoint.Label`, numeric Y axis range from the maximum count, and a passive legend-free `ViewXY`.

### Task 3: Trend Tab Replacement

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmStatisticsView.xaml`

- [ ] **Step 1: Replace old daily statistics body**

Replace the trend tab `ScrollViewer` containing `ItemsControl ItemsSource="{Binding TrendPoints}"` with a `Grid` containing `<charts:AlarmTrendLineLightningChart ItemsSource="{Binding TrendPoints}" />`.

- [ ] **Step 2: Add empty overlay**

Use the existing `IsBarStatisticsEmpty` style pattern locally adapted for trend data if an existing trend empty property is present; otherwise leave the chart fallback to show empty data without adding ViewModel state.

### Task 4: Verification

**Files:**
- Test: `Scratch/AlarmCenterFunctionalTests/Program.cs`
- Build: `Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj`

- [ ] **Step 1: Run functional tests**

Run: `dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore`

Expected: RESULT reports zero failures.

- [ ] **Step 2: Build project**

Run: `dotnet build Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj --no-restore`

Expected: Build succeeds with exit code 0.

## Self-Review

- Spec coverage: Tasks cover test, wrapper, XAML replacement, and verification.
- Placeholder scan: No deferred implementation placeholders are used.
- Type consistency: The plan uses the existing `AlarmTrendPoint`, `TrendPoints`, and `charts` namespace names.

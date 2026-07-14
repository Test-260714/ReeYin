# Alarm Workbench Navigation And Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix UI freezes when navigating to `AlarmWorkbenchShellView` and make AlarmCenter pages more stable and readable.

**Architecture:** Keep the existing Prism shell and child views, but make child navigation idempotent and deferred until the nested region is loaded. Move expensive chart initialization out of the shell constructor and only build chart data when the statistics page is selected. Use source-level regression checks plus the project build to verify the fix without adding external test packages.

**Tech Stack:** .NET 8 WPF, Prism regions, existing ReeYin_V.UI styles, PowerShell source checks.

---

## File Structure

- Create: `Scratch\AlarmCenterChecks\Check-AlarmCenter.ps1` - regression checks for navigation and layout source patterns.
- Modify: `Application\ReeYin.AlarmCenter\ViewModels\AlarmWorkbenchShellViewModel.cs` - idempotent navigation, async UI dispatch, lazy refresh/chart handling.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml.cs` - call the view model once after `Loaded`.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml` - shell layout.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmRealtimeView.xaml` - realtime page layout.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmHistoryView.xaml` - history filter/table layout.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmStatisticsView.xaml` - statistics card/grid layout.
- Modify: `Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml` - shared spacing/card/button styles.

## Task 1: Add Red Regression Check

- [ ] Create `Scratch\AlarmCenterChecks\Check-AlarmCenter.ps1`.
- [ ] Assert current source still contains blocking or eager patterns:
  - `RunOnUiThread` uses `Dispatcher.Invoke`.
  - `OnNavigatedTo` directly calls `NavigateToSelectedPage`.
  - chart `ViewXY` fields are initialized with `CreateChartView()`.
- [ ] Run the script and verify it fails before implementation.

## Task 2: Fix Navigation And Refresh Lifecycle

- [ ] Add `_isContentRegionAttached`, `_hasNavigatedInitialPage`, and `_hasLoadedStatistics` fields.
- [ ] Replace duplicate child navigation with `EnsureInitialContentNavigation()`.
- [ ] Change `RunOnUiThread` to use `BeginInvoke` rather than synchronous `Invoke`.
- [ ] Add `RefreshForPageAsync(...)` and only load history/statistics when their tab is selected.
- [ ] Make chart fields nullable or initialize them when statistics loads.
- [ ] Run the source check and project build.

## Task 3: Adjust Layouts

- [ ] Update shell to `Auto | Auto | *` rows and keep nested content stretched.
- [ ] Use a card-like left navigation and tighter status/header area.
- [ ] Rework realtime view into toolbar plus `3* / 2*` content columns.
- [ ] Rework history view into compact filter rows, scrollable data grid, and fixed pager.
- [ ] Rework statistics view into summary cards plus responsive distribution/trend sections.
- [ ] Run source check and `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore`.

## Self-Review

- Scope covers the reported navigation freeze and layout cleanup only.
- The plan avoids broad changes to Core alarm behavior.
- Verification is source-level regression checks plus WPF project compilation.

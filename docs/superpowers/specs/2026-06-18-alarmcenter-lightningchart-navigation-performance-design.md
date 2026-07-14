# AlarmCenter LightningChart Navigation Performance Design

Date: 2026-06-18
Status: Design approved by user; awaiting written-spec review before implementation planning.

## Problem

`ReeYin.AlarmCenter` currently uses LightningChart for the statistics bar chart and type-share pie chart. The first chart load and later page switches feel slow because chart controls still initialize and schedule rendering from WPF lifecycle events. Previous work already moved chart ownership out of the ViewModel and added lazy tab templates, but page navigation can still trigger chart `Loaded` events, collection change notifications, and redundant chart redraws.

## Evidence From Current Code

- `Application/ReeYin.AlarmCenter/Controls/AlarmTrendBarLightningChart.cs` creates a `LightningChart` and `ViewXY` when the control is loaded.
- `Application/ReeYin.AlarmCenter/Controls/AlarmTypePieLightningChart.cs` creates a `LightningChart` and `ViewPie3D` when the control is loaded.
- Both controls call `ScheduleRender()` from `Loaded` and from `INotifyCollectionChanged.CollectionChanged`.
- `Application/ReeYin.AlarmCenter/Views/AlarmStatisticsView.xaml.cs` keeps the statistics view alive and does not release chart resources on `Unloaded`.
- `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs` already avoids some statistics refreshes through `_hasLoadedStatistics`, `_isStatisticsCacheInvalidated`, and `CanReuseStatisticsNavigation`.
- `Scratch/AlarmCenterFunctionalTests/Program.cs` already protects the current chart lifecycle pattern with source-level regression checks.

## Goals

- Keep LightningChart as the chart technology for this task.
- Reduce page-switch stalls after the first statistics chart load.
- Avoid rebuilding chart axes, bar series, and pie slices when bound data has not changed.
- Keep chart instances owned by their UserControls, not by `AlarmWorkbenchShellViewModel`.
- Preserve the existing bindings:
  - `BarTrendPoints` for the hourly/statistics bar chart.
  - `TypePieSlices` for the type-share pie chart.
- Preserve the existing no-interaction behavior so charts do not capture mouse input during navigation.

## Non-Goals

- Do not replace LightningChart with native WPF drawing in this task.
- Do not remove `Arction.*` references from `ReeYin.AlarmCenter.csproj`.
- Do not add new chart interactions, zooming, panning, or tooltip behavior.
- Do not redesign the statistics page layout.
- Do not move chart construction back into the ViewModel.

## Proposed Approach

### Data Signatures

Each LightningChart wrapper control will cache a stable signature of its current chart data:

- Bar chart signature should include each `AlarmTrendPoint` label and count, plus enough numeric state to detect visual changes such as maximum/count-derived scaling.
- Pie chart signature should include each visible `AlarmPieSliceItem` label, count, percentage, and fill color.

The controls will compare the new signature to the last rendered signature before doing LightningChart work. If the signature is unchanged, the control skips `RenderChart()`.

### Render Scheduling

`ScheduleRender()` will become a coalescing gate instead of a blind render request:

- If the control is not loaded, mark data dirty and return.
- If a render operation is already scheduled, do not schedule another one.
- On the queued dispatcher callback, compute the current signature.
- Only call `RenderChart()` when the chart has no rendered signature or the current signature differs from the previous one.
- Store the rendered signature only after a successful render.

This keeps collection updates cheap during navigation and prevents duplicate redraws from `Loaded` plus `CollectionChanged`.

### Loaded And Unloaded Behavior

`Loaded` will ensure the chart object exists, then schedule rendering only when needed:

- First load: create LightningChart and render the first data snapshot.
- Later loads with unchanged data: ensure the chart exists and skip redraw.
- Later loads after data changes while hidden: render once.

`Unloaded` will keep the existing behavior of releasing only mouse capture. It will not dispose or recreate LightningChart resources during normal Prism page switching.

### ViewModel Refresh Behavior

The ViewModel already caches statistics page data at navigation level. This design keeps that architecture:

- `AlarmWorkbenchShellViewModel` remains free of LightningChart types.
- `CanReuseStatisticsNavigation()` continues to avoid unnecessary statistics queries when the statistics page is revisited without invalidation.
- If real alarm data invalidates statistics while another page is selected, the ViewModel can refresh lightweight collections; the chart controls decide whether redraw is required when visible.

### User Experience

Expected behavior:

- The first time the statistics page is opened, LightningChart may still pay its normal initialization cost.
- Switching away and back should not rebuild chart visuals when data has not changed.
- Applying statistics filters or changing the time range should update charts normally.
- Empty overlays should continue to work through `IsBarStatisticsEmpty` and `IsTypePieStatisticsEmpty`.

## Files To Modify

- `Application/ReeYin.AlarmCenter/Controls/AlarmTrendBarLightningChart.cs`
  - Add bar data signature calculation.
  - Add last-rendered signature caching.
  - Gate `ScheduleRender()` and `RenderSafely()` through dirty/signature checks.

- `Application/ReeYin.AlarmCenter/Controls/AlarmTypePieLightningChart.cs`
  - Add pie data signature calculation.
  - Add last-rendered signature caching.
  - Gate `ScheduleRender()` and `RenderSafely()` through dirty/signature checks.

- `Scratch/AlarmCenterFunctionalTests/Program.cs`
  - Update existing LightningChart lifecycle tests to require data-signature based redraw suppression.
  - Keep assertions that the ViewModel does not own chart objects.
  - Keep assertions that `Unloaded` does not release cached chart resources.

## Testing Strategy

Implementation should follow TDD:

1. Update `Scratch/AlarmCenterFunctionalTests/Program.cs` so the relevant chart lifecycle tests fail against the current code.
2. Run:

   ```powershell
   dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
   ```

   Expected before implementation: failure indicating the controls do not yet have data-signature redraw suppression.

3. Implement the minimal control changes.
4. Re-run:

   ```powershell
   dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj
   ```

   Expected after implementation: all AlarmCenter functional tests pass.

5. Build the project:

   ```powershell
   dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj
   ```

   Expected: build succeeds.

## Risks And Mitigations

- Risk: LightningChart first-use initialization still costs time.
  - Mitigation: This design targets repeated page-switch redraws. If first-use cost remains unacceptable, a later task can preload the chart after the shell becomes idle.

- Risk: A signature misses a visual input and skips a required redraw.
  - Mitigation: Include all fields used by each render path, including labels, counts, percentages, and pie colors.

- Risk: A failed render stores a signature and suppresses later retries.
  - Mitigation: Store `_lastRenderedSignature` only after `RenderChart()` succeeds.

- Risk: Scheduled dispatcher work runs after unload.
  - Mitigation: The callback should check `IsLoaded` and leave the dirty flag intact if the control is hidden.

## Success Criteria

- Re-entering the statistics page with unchanged statistics data does not rebuild the LightningChart bar or pie content.
- Statistics filters still update the chart after data changes.
- Page switching does not dispose chart resources or recapture mouse input.
- `AlarmWorkbenchShellViewModel` remains free of LightningChart/Arction dependencies.
- AlarmCenter functional tests pass.
- `ReeYin.AlarmCenter` builds successfully.

## Self-Review

- Placeholder scan: no unfinished placeholder markers remain.
- Scope check: this spec is limited to repeated LightningChart rendering during AlarmCenter navigation.
- Consistency check: the design keeps LightningChart, keeps existing bindings, and does not remove Arction references.
- Ambiguity check: the redraw gate is based on explicit per-control data signatures and stores the signature only after a successful render.

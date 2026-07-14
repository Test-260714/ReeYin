# AlarmCenter Trend Line LightningChart Design

## Goal

Replace the daily trend card/list area in `ReeYin.AlarmCenter` statistics trend analysis with an owned LightingChart line chart while keeping the existing trend date filters and data flow.

## Current Context

- `Application/ReeYin.AlarmCenter/Views/AlarmStatisticsView.xaml` has three statistics tabs.
- The bar and pie tabs already use owned chart wrapper controls under `Application/ReeYin.AlarmCenter/Controls`.
- The trend analysis tab keeps filters in the header, but its body currently renders `TrendPoints` through an `ItemsControl` inside a `ScrollViewer`.
- `AlarmWorkbenchShellViewModel.ApplyTrendStatistics()` already produces lightweight `TrendPoints`; it must remain free of Arction/LightingChart types.

## Selected Approach

Create a new `AlarmTrendLineLightningChart` wrapper control and bind it to `TrendPoints` from the trend tab. The wrapper owns the `LightningChart` and `ViewXY`, follows the existing bar chart lifecycle pattern, and renders the trend counts as a line series.

## Behavior

- Keep the trend filter controls: start date, end date, previous range, next range, and query trend.
- Remove the previous daily statistics `ScrollViewer`/`ItemsControl` body.
- Render a line chart using the existing trend point label and count values.
- Show a page-level empty-state overlay when `TrendPoints` has no count data.
- Keep the chart passive by disabling chart hit testing and user interaction, matching the existing statistics chart behavior.

## Data Flow

1. The user changes the trend date filters.
2. `ApplyTrendFilterCommand` or shift commands refresh trend statistics.
3. `ApplyTrendStatistics()` replaces `TrendPoints`.
4. `AlarmTrendLineLightningChart.ItemsSource` receives the updated collection.
5. The chart wrapper schedules a deferred render on the UI dispatcher and skips redraws when the data signature is unchanged.

## Testing

- Update `Scratch/AlarmCenterFunctionalTests/Program.cs` to assert that the trend tab uses `AlarmTrendLineLightningChart`.
- Assert that the old `ItemsControl ItemsSource="{Binding TrendPoints}"` daily card/list body is gone.
- Assert that the line chart wrapper owns `LightningChart`, creates `ViewXY` after load, uses `PointLineSeries`, defers renders, caches signatures, and hides stale chart content on fallback.
- Build `Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj`.

## Constraints

- Do not move chart ownership into `AlarmWorkbenchShellViewModel`.
- Do not add direct `<lc:LightningChart>` usage to `AlarmStatisticsView.xaml`.
- Do not alter the statistics query or trend bucket behavior.

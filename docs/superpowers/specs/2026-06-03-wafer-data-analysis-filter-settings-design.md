# Wafer Data Analysis Filter Settings Design

## Context

`Custom.WaferFlatnessMeasure` currently lets users edit data-source filtering directly in the `SensorDataCollectionView` data analysis tab. The data-source `DataGrid` includes inline columns for filter enablement, raw-data filtering, minimum value, and maximum value.

Point collection aggregation currently removes a fixed two samples from the beginning and two samples from the end of each grouped point sample set before averaging. This fixed value is implemented in `SensorPointDataProcessor.AggregateMeasureDataGroup`.

## Goals

- Move per-data-source filter configuration from inline table columns into a dialog opened from the data-source row.
- Keep the data-source table focused on row identity and source selection.
- Add a configurable point-collection trim count so users can choose how many samples to discard from the start and end of each point group.
- Preserve existing behavior by default: filtering semantics stay the same, and point collection still trims two samples per side unless the user changes the setting.

## Non-Goals

- Do not change data-analysis algorithm math.
- Do not redesign the whole data analysis tab.
- Do not change how data sources are persisted beyond adding the trim-count setting.
- Do not alter line-scan collection alignment behavior.

## Recommended Approach

Use a small per-row filter-settings dialog. The existing `DataAnalysisDataSourceOption` remains the source of truth for filter fields:

- `IsFilterEnabled`
- `IsRawDataFilter`
- `FilterMin`
- `FilterMax`

The table opens the dialog for the currently selected row or the row whose button was clicked. The dialog edits those same properties, so existing runtime filtering code can remain unchanged.

Add `PointCollectionTrimCountPerSide` to `SensorDataCollectionModel`, defaulting to `2`. Pass it through `SensorPointDataProcessingOptions` into `SensorPointDataProcessor`, then use it in aggregation instead of the fixed `2`.

## UI Design

In `SensorDataCollectionView`:

- Remove the inline data-source columns named `过滤`, `原始过滤`, `下限`, and `上限`.
- Add a `过滤设置` button column.
- Add a numeric setting in the data-source area named `点采集每组去除首尾数量`.

The filter settings dialog should show:

- Data-source name and OriginalData key as read-only context.
- Checkbox: `启用过滤`.
- Checkbox: `原始过滤`.
- Numeric/text inputs: `下限`, `上限`.
- `确定` and `取消` actions.

The dialog should feel consistent with existing ReeYin-V pages by reusing shared styles such as `GeneralButtonStyle`, `DefaultDataGridStyle` where applicable, and existing text/input styles.

## Data Flow

1. User clicks a data-source row's `过滤设置` button.
2. `SensorDataCollectionViewModel` opens the filter settings dialog with the selected `DataAnalysisDataSourceOption`.
3. The dialog edits a working copy first.
4. On `确定`, the dialog copies values back to the original data-source object.
5. Existing property-change listeners in `SensorDataCollectionModel` update summaries and downstream selections.
6. During point collection, `SensorDataCollectionModel.ProcessCollectedData` passes `PointCollectionTrimCountPerSide` to `SensorPointDataProcessor`.
7. `SensorPointDataProcessor.AggregateMeasureDataGroup` trims that many samples per side before averaging.

## Processing Rules

- `PointCollectionTrimCountPerSide` is an integer and must be normalized to zero or greater.
- Default value is `2`.
- If a group count is less than or equal to `trimCount * 2`, keep the full group. This preserves current safety behavior and avoids empty aggregation groups.
- Raw-data filtering remains before aggregation.
- Average-after filtering remains during final `PreDatas` filtering before algorithms.
- Standard calibration reference points use the same aggregation trim count when they are aggregated for calibration wafer CSV output.

## Error Handling

- If no data source is selected when opening settings, show a warning message and do not open the dialog.
- If filter minimum is greater than maximum, keep the existing `NormalizeFilterRange` behavior, which swaps the values at runtime and logs a warning.
- If the trim-count setting is negative due to deserialization or bad input, normalize it to `0`.
- If a data-source option is missing or null, ignore the action instead of throwing.

## Testing

Automated tests should cover `SensorPointDataProcessor` behavior because this is the critical processing change:

- Default trim count of `2` preserves existing aggregation behavior.
- A custom trim count changes which samples are averaged.
- A trim count of `0` averages all samples in the group.
- A trim count large enough to remove all samples falls back to using the full group.
- Raw filtering still happens before aggregation.

Build verification should include `dotnet build` for the touched project or solution path that includes `Custom.WaferFlatnessMeasure`.

## Self-Review

- No placeholder requirements remain.
- The design keeps filtering fields on `DataAnalysisDataSourceOption`, so existing filtering code and persisted data remain compatible.
- The trim setting is scoped to point collection aggregation and does not affect line-scan alignment.
- The dialog behavior is specified as copy-on-confirm to avoid accidental changes when the user cancels.

# Defect Map UserControl Design

Date: 2026-06-04
Project: ReeYin_V.UI
Target path: `Core/ReeYin_V.UI/UserControls/DefectMap`

## Summary

Add a reusable WPF MVVM UserControl that uses LightningChart to render a long roll-material defect map. The control shows material width on the horizontal axis and material length on the vertical axis. Defects are rendered as severity-colored point markers. The length origin is configurable so callers can choose whether length `0` starts at the top or bottom of the map.

The control is intended for approximate defect-location overview, not pixel-perfect image annotation. It must be easy for downstream business pages to bind defect data, selected defect state, and selected-defect commands.

## Goals

- Create the control under `Core/ReeYin_V.UI/UserControls/DefectMap`.
- Use LightningChart `ViewXY` for high-performance point rendering.
- Follow the existing `ReeYin_V.UI` UserControl pattern with `Models`, `ViewModels`, and `Views`.
- Use MVVM for data preparation, chart axis setup, summary text, and selected state.
- Expose dependency properties so business pages can bind data without referencing internal ViewModel details.
- Support hover tooltip and click selection for defect review workflows.

## Non-Goals

- Do not create a node module or modify the node execution lifecycle.
- Do not add recipe parameters, output parameters, or dynamic node pages.
- Do not build heatmap rendering in the first version.
- Do not render defect rectangles in the first version; the first version uses severity point markers.
- Do not implement data paging or sampling until real data volume shows it is needed.

## Existing Project Context

- `Core/ReeYin_V.UI/ReeYin_V.UI.csproj` targets `net8.0-windows` and already references:
  - `Arction.Wpf.Charting.LightningChart`
  - `Arction.Wpf.ChartingMVVM.LightningChart`
- Existing LightningChart MVVM controls include:
  - `Core/ReeYin_V.UI/UserControls/AxisMotionTrajectoryMonitor`
  - `Core/ReeYin_V.UI/UserControls/PolarLineSeries`
  - `Core/ReeYin_V.UI/UserControls/PointCloudDisplay`
- The new control should use the MVVM charting API where possible and keep code-behind limited to dependency-property bridging, chart setup, lifecycle cleanup, and minimal interaction bridging when LightningChart requires view-level events.

## File Structure

```text
Core/ReeYin_V.UI/UserControls/DefectMap/
  Models/
    DefectMapAppearance.cs
    DefectMapEnums.cs
    DefectMapItem.cs
  ViewModels/
    DefectMapViewModel.cs
  Views/
    DefectMapView.xaml
    DefectMapView.xaml.cs
```

## Public Control API

`DefectMapView` exposes dependency properties:

- `IEnumerable<DefectMapItem>? Defects`
- `double MaterialWidth`
- `double MaterialLength`
- `DefectMapLengthOrigin LengthOrigin`
- `DefectMapItem? SelectedDefect`
- `ICommand? DefectSelectedCommand`
- `string MapTitle`

Expected XAML usage:

```xml
<defectMap:DefectMapView
    Defects="{Binding Defects}"
    MaterialWidth="{Binding MaterialWidth}"
    MaterialLength="{Binding MaterialLength}"
    LengthOrigin="{Binding LengthOrigin}"
    SelectedDefect="{Binding SelectedDefect, Mode=TwoWay}"
    DefectSelectedCommand="{Binding DefectSelectedCommand}" />
```

## Data Model

`DefectMapItem` is a generic UI data model:

- `string Id`
- `string Name`
- `string DefectType`
- `DefectMapSeverity Severity`
- `double WidthPosition`
- `double LengthPosition`
- `double? DisplaySize`
- `string Description`
- `object? Tag`

`DefectMapSeverity` values:

- `Minor`
- `Warning`
- `Critical`

`DefectMapLengthOrigin` values:

- `Top`: length `0` is at the top and increases downward.
- `Bottom`: length `0` is at the bottom and increases upward.

Model rules:

- `WidthPosition` and `LengthPosition` always store real material coordinates.
- Display coordinate conversion happens only in the ViewModel/chart layer.
- Invalid coordinates (`NaN`, infinity) are treated as out of range.
- Out-of-range defects are not drawn in the first version.

## Visual Design

- Horizontal axis: material width.
- Vertical axis: material length.
- Background: light chart background with grid lines.
- Defect marker style:
  - `Minor`: green, small point.
  - `Warning`: orange, medium point.
  - `Critical`: red, large point.
- Selected defect:
  - Use a separate highlight series or selected marker overlay.
  - The highlight should be visually distinct, such as a larger ring or brighter marker.
- Summary area:
  - Show total count, visible count, out-of-range count, selected defect text, and current material size.

## Data Flow

1. External page updates `Defects`, `MaterialWidth`, `MaterialLength`, or `LengthOrigin`.
2. `DefectMapView.xaml.cs` forwards the new values to `DefectMapViewModel`.
3. `DefectMapViewModel` validates material size and defects.
4. ViewModel rebuilds:
   - X axis collection.
   - Y axis collection.
   - severity point series.
   - selected highlight series.
   - summary text.
5. `DefectMapView.xaml` updates LightningChart through bindings.

Coordinate mapping:

- X coordinate is always `WidthPosition`.
- If `LengthOrigin == Bottom`, chart Y coordinate is `LengthPosition`.
- If `LengthOrigin == Top`, chart Y coordinate is `MaterialLength - LengthPosition`.
- Tooltip text still reports the original real `LengthPosition`.

## Interaction Design

Hover:

- Hovering a defect point shows a tooltip with:
  - name
  - defect type
  - severity
  - width position
  - length position
  - description

Click:

- Clicking a defect point updates `SelectedDefect`.
- Clicking a defect point executes `DefectSelectedCommand` with the selected `DefectMapItem`.
- Selected state is reflected visually on the chart.

Implementation note:

- Prefer LightningChart MVVM interaction hooks if available.
- If point-hit information is not available through binding-only APIs, code-behind may do minimal chart hit testing and call a ViewModel method with the selected item.
- Code-behind must not own business state.

## Error Handling

- If `MaterialWidth <= 0`, normalize display width to `1` and show an empty valid chart area.
- If `MaterialLength <= 0`, normalize display length to `1` and show an empty valid chart area.
- Null `Defects` is treated as an empty collection.
- Out-of-range defects are counted but not drawn.
- Invalid defect coordinates are counted as out of range.
- Null or empty names/types are rendered with safe default text.

## Performance

- Defects are grouped into three severity series instead of one UI element per defect.
- Updates rebuild series in batches.
- The first version targets normal review-size defect lists.
- If future datasets contain very large point counts, add a separate enhancement for:
  - `MaxVisibleDefectCount`
  - sampling or clustering
  - viewport-based filtering

## Testing And Verification

Manual data cases:

- Empty defect list.
- One defect in each severity level.
- Multiple defects at different material-width and material-length positions.
- `LengthOrigin.Top`.
- `LengthOrigin.Bottom`.
- Negative coordinates.
- Coordinates beyond material width or length.
- Null/empty defect names and descriptions.
- Click selection updates `SelectedDefect`.
- Click selection executes `DefectSelectedCommand`.
- Hover tooltip shows original real coordinates.

Build verification:

- Build `Core/ReeYin_V.UI/ReeYin_V.UI.csproj`.
- Confirm XAML compiles.
- Confirm LightningChart references resolve.
- Confirm dependency-property bindings compile.

## Acceptance Criteria

- The new control exists under `Core/ReeYin_V.UI/UserControls/DefectMap`.
- External pages can bind `Defects`, `MaterialWidth`, `MaterialLength`, `LengthOrigin`, and `SelectedDefect`.
- The chart displays width on the horizontal axis and length on the vertical axis.
- `LengthOrigin.Top` and `LengthOrigin.Bottom` both render correctly.
- Defects render as severity-colored point markers.
- Hover shows defect details.
- Click selects a defect and notifies external code through `DefectSelectedCommand`.
- Out-of-range defects are not rendered and are included in summary counts.
- `Core/ReeYin_V.UI/ReeYin_V.UI.csproj` builds successfully.

## Implementation Sequence

1. Add enums and `DefectMapItem`.
2. Add `DefectMapAppearance` defaults.
3. Add `DefectMapViewModel` with axis/series rebuild methods.
4. Add `DefectMapView.xaml` with LightningChart bindings and summary UI.
5. Add `DefectMapView.xaml.cs` dependency properties and event bridge.
6. Build the UI project and adjust compile-time API usage.
7. Manually review top-origin and bottom-origin behavior with sample data.

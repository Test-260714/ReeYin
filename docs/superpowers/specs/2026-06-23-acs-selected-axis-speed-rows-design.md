# ACS Selected Axis Speed Rows Design

Date: 2026-06-23

## Goal

Update the `AcsControlCardConfigView` axis-control tab so the speed parameter grid follows the currently selected axis. When the operator selects X, Y, Z, or R, the speed grid must show only that axis' speed settings. Existing ACS-specific axis controls must remain available.

## Current Behavior

- The axis selector already drives `SelectedAxis`, `SelectedAxisConfig`, and selected-axis home Buffer properties.
- The speed grid binds to `AxisSpeedRows`, which flattens all axes and all `SpeedDict1` entries into one table.
- The all-axis grid includes axis metadata columns such as axis, physical axis number, axis name, and enabled state.

## Required Behavior

- Keep the current axis selector as the single source of the selected axis.
- Show only the selected axis' `SpeedDict1` entries in the speed grid.
- Refresh the selected-axis speed rows when:
  - a card is assigned to the view model,
  - `SelectedAxis` changes,
  - speed settings are initialized for an axis.
- Keep existing ACS-specific controls in the axis tab:
  - axis parameters,
  - single-axis enable/disable and movement commands,
  - per-axis home Buffer settings,
  - X/Y combined home Buffer settings,
  - hold-to-move controls,
  - feedback reset and axis status display.

## Proposed Implementation

Add a selected-axis speed row collection to `AcsControlCardConfigViewModel`, for example `SelectedAxisSpeedRows`, reusing the existing `AcsAxisSpeedParameterRow` row wrapper so edits still write through to the underlying `SpeedSetting`.

`SyncSelectedAxisContext()` should update:

- `SelectedAxisConfig`
- `SelectedHomeBuffer`
- `SelectedAxisContext`
- selected-axis home Buffer properties
- `SelectedAxisSpeedRows`

The existing `AxisSpeedRows` can remain for compatibility and existing tests, but the `AcsControlCardConfigView` axis-control tab should bind its speed grid to `SelectedAxisSpeedRows`.

## UI Shape

Change the speed panel title from all-axis editing to selected-axis editing, for example:

- `轴速度参数（当前轴）`
- helper text: `选择上方轴后，仅编辑当前轴 SpeedDict1`

Because the upper axis-parameter panel already edits axis number, name, and enabled state, the selected-axis speed grid should keep only speed-specific columns:

- speed type
- description
- start speed
- max speed
- acceleration/deceleration

## Tests

Extend the ACS console test harness to cover:

- the view model exposes selected-axis speed rows,
- X initially shows only X speed settings,
- switching to Y shows only Y speed settings,
- edits to a selected-axis row write through to that axis' `SpeedDict1`,
- the XAML binds the speed grid to `SelectedAxisSpeedRows`,
- the axis-control tab still contains ACS-specific controls such as home Buffer, X/Y combined home, hold-to-move, and axis commands.

## Out Of Scope

- Changing speed-setting persistence format.
- Changing how ACS motion code consumes `SpeedDict1`.
- Removing the compatibility `AxisSpeedRows` property.
- Reworking the whole ACS configuration layout.

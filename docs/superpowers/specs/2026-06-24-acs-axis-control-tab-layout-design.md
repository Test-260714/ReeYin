# ACS Axis Control Tab Layout Redesign

## Goal

Redesign the `AcsControlCardConfigView` axis-control tab into a balanced configuration/debugging workspace. The page should make the selected axis context clear, show the selected axis speed parameters without overlap, keep ACS-specific parameters available, and keep critical motion controls visible during debugging.

## Background

The current ACS configuration window already contains the required bindings and behavior for:

- Selecting an axis through `AxisOptions` and `SelectedAxis`.
- Editing current-axis basic parameters through `SelectedAxisConfig`.
- Showing current-axis speed rows through `SelectedAxisSpeedRows`.
- Editing per-axis home-buffer settings through `SelectedAxisHomeBuffer*`.
- Keeping ACS-specific XY combined home settings through `Options.XyHomeBuffer`.
- Running motion/debug commands through `EnableAxisCommand`, `MoveRelativeCommand`, jog/continuous move commands, `StopAxisCommand`, `GoHomeCommand`, and `RefreshAxisStatusCommand`.

The problem is layout density: the axis tab uses a vertical stack of large cards inside a broad scroll viewer. On smaller window sizes, controls can wrap unpredictably, speed rows can be squeezed, and critical controls such as stop/status may move out of view.

## Selected Approach

Use a three-column axis workspace:

1. **Left column: current-axis basic configuration**
2. **Middle column: speed, home, and ACS-specific axis settings**
3. **Right column: fixed debug operations and status**

This balances configuration and operation, keeps the selected-axis context visible, and avoids mixing global ACS settings with ordinary axis fields.

Rejected alternatives:

- **Debug-first layout:** good for commissioning, but hides configuration and speed settings too much.
- **Parameter-first layout:** good for offline configuration, but makes runtime stop/jog/status controls less accessible.

## Layout Structure

### Top Axis Context Bar

Place a compact bar at the top of the axis tab, above the three columns.

Content:

- Axis selector: `AxisOptions` -> `SelectedAxis`
- Current axis summary:
  - `SelectedAxisConfig.AxisNo`
  - `SelectedAxisConfig.NickName`
  - `SelectedAxisConfig.IsUsing`
  - selected home buffer number
- Quick actions:
  - `RefreshAxisStatusCommand`
  - `OpenBufferScriptCommand`

Purpose:

- Make it explicit that all visible configuration is for the selected axis.
- Provide a stable entry point for switching axes.

### Left Column: Current-Axis Basic Configuration

Use one card with a two-column form. Avoid `WrapPanel` for these core fields.

Preserve bindings:

- `SelectedAxisConfig.AxisNo`
- `SelectedAxisConfig.IsUsing`
- `SelectedAxisConfig.NickName`
- `SelectedAxisConfig.PulseEquivalent`
- `SelectedAxisConfig.OriginOffset`
- `SelectedAxisConfig.SoftLimitPositive`
- `SelectedAxisConfig.SoftLimitNegative`
- `SelectedAxisConfig.SafetyDis`
- `SelectedAxisConfig.DecelerateDis`

Behavior:

- Editing these values updates the selected `SingleAxisParam`.
- Switching `SelectedAxis` refreshes the displayed `SelectedAxisConfig`.
- If `SelectedAxisConfig` is null, the area remains structurally stable and shows disabled/empty fields rather than collapsing.

### Middle Column: Speed, Home, and ACS-Specific Settings

Use a vertical configuration stack with its own scroll area if needed.

Sections:

1. **Current-axis speed parameters**
   - Data source: `SelectedAxisSpeedRows`
   - Use `DefaultDataGridStyle`.
   - Minimum height: about 180 to 220 pixels.
   - Columns:
     - `SpeedType`
     - `SpeedDescribe`
     - `StartSpeed`
     - `MaxSpeed`
     - `AccSpeed`
   - Do not show all axes at once in this grid.

2. **Single-axis home buffer**
   - Preserve:
     - `SelectedAxisHomeBufferNo`
     - `SelectedAxisHomeBufferEnabled`
     - `SelectedAxisHomeBufferTimeout`
     - `SelectedAxisStopBeforeHomeBuffer`
     - `SelectedAxisResetFeedbackAfterHome`
     - `SelectedAxisHomeResetPosition`
     - `GoHomeCommand`

3. **ACS motion profile operations**
   - Preserve existing ViewModel surface:
     - `AxisMotionProfiles`
     - `SelectedMotionProfile`
     - `RefreshMotionProfilesCommand`
     - `ApplySelectedMotionProfileCommand`
     - `ApplyAllMotionProfilesCommand`
   - The profile section may use a compact grid or selector plus action buttons, depending on existing available columns.

4. **XY combined home buffer**
   - Preserve `Options.XyHomeBuffer`.
   - Put it in an `Expander` labeled as global ACS XY home settings.
   - Keep it visually separate from selected-axis fields because switching the selected axis should not imply this is per-axis state.

### Right Column: Debug Operations and Status

The right column should not scroll away during normal use.

Sections:

1. **Enable and stop**
   - `EnableAxisCommand`
   - `DisableAxisCommand`
   - `StopAxisCommand`
   - The stop button should be visually easy to find and placed near jog/continuous move controls.

2. **Position moves**
   - `RelativeDistance`
   - `MoveRelativeCommand`
   - `AbsoluteTarget`
   - `MoveAbsoluteCommand`

3. **Jog and continuous movement**
   - `JogStep`
   - `JogPositiveCommand`
   - `JogNegativeCommand`
   - `ContinuousMoveSpeedType`
   - `StartPositiveContinuousMoveCommand`
   - `StartNegativeContinuousMoveCommand`
   - `StopContinuousMoveCommand`

4. **Feedback and status**
   - `ResetPosition`
   - `ResetFeedbackCommand`
   - `AxisStatusText`

Status field:

- Read-only `TextBox`
- `TextWrapping="Wrap"`
- vertical scroll enabled
- monospace font is acceptable for compact status snapshots

## Sizing And Responsiveness

Target normal window: existing `1360 x 820`.

Recommended column widths:

- Left: `330` to `360`
- Middle: `*`, minimum about `430`
- Right: `310` to `340`

Rules:

- The axis tab outer grid should avoid one large horizontal scroll region in normal window size.
- Configuration columns may scroll vertically.
- The debug/status column stays visible.
- Keep input controls at consistent heights, about `28` to `30`.
- Keep action buttons at consistent heights, about `30` to `32`.
- Use fixed label widths in form grids to prevent label/input overlap.
- Avoid `WrapPanel` for critical controls where wrapping can hide or reorder safety-related actions.

## Visual And Style Rules

Follow existing ReeYin-V style resources:

- Keep `AcsCardStyle` or a compatible local card style.
- Use `GeneralButtonStyle` for action buttons.
- Use `DefaultDataGridStyle` for speed/profile grids.
- Use `ExpanderStyle` for collapsible ACS-specific sections.
- Prefer theme resources such as `RegionBackgroundBrush` and `SplitBrush`.
- Do not introduce a new visual theme for this one tab.

## ViewModel Scope

The redesign should be primarily XAML layout work.

Expected ViewModel changes are limited to:

- Adding small read-only display properties if needed for the top context bar.
- Adding missing `RaisePropertyChanged` calls only if an existing binding does not refresh after `SelectedAxis` changes.

Do not redesign motion command behavior in this layout task.

## Compatibility Requirements

The redesign must preserve:

- Selected-axis speed parameter behavior.
- ACS-specific home-buffer parameters.
- XY combined home-buffer parameters.
- Existing LCI tab behavior.
- Existing connection, IO, monitor, and buffer-script behavior.
- Existing command names and public ViewModel properties unless a test explicitly covers a safe compatibility alias.

## Test Strategy

Use the ACS console-style source tests in:

`Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

Add or update source tests to verify:

- The axis tab has a three-column workspace structure.
- `SelectedAxisSpeedRows` remains bound to the current-axis speed grid.
- The right debug/status panel contains `StopAxisCommand` and `AxisStatusText`.
- `Options.XyHomeBuffer` remains present in the axis tab.
- `RefreshMotionProfilesCommand`, `ApplySelectedMotionProfileCommand`, and `ApplyAllMotionProfilesCommand` remain reachable if the profile section is kept.
- The old broad `MinWidth=900` axis-tab scroll layout is removed or replaced with a non-overlapping structure.

Build verification:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Known caveat:

- The ACS test executable may still stop later on unrelated source tests outside this layout scope. The new axis-layout tests should run and pass before any known unrelated failure.

## Risks

- WPF layout changes can compile but still be visually cramped. The implementation should keep the XAML simple and avoid deep nested grids where possible.
- Hard-coded colors already exist in the page; this work should not expand them. Prefer dynamic resources for any new separators or subtle labels.
- If `SelectedMotionProfile` is not currently displayed in XAML, adding it may expose gaps in profile row shape. Keep this section compact and source-tested.

## Success Criteria

- Switching axes clearly updates the visible axis configuration and speed table.
- Current-axis speed parameters are readable and editable without being hidden by other controls.
- ACS-specific home settings remain available.
- Stop/status/debug controls remain visible while editing axis parameters.
- The ACS project builds successfully.
- The ACS test project builds successfully, and new layout source tests pass.

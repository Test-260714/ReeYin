# Axis Speed Monitor Design

## Context

AxisView currently polls axis positions every 100 ms through `ControlCard.GetAllPosInfos()`.
The card implementation writes each configured axis position to `SingleAxisParam.CurPos`,
and AxisView maps the configured axes to the fixed display order `X, Y, Z, Z1, Z2`
through `AxisViewAxisMatcher`.

There is no unified API or runtime cache for current axis speed. The new feature adds
actual feedback speed monitoring for AxisView and implements the speed read path for ACS
and Googol control cards.

## Selected Approach

Use one unified actual-speed API on the common control card contract. AxisView should not
branch on vendor-specific card types.

The displayed speed is actual feedback speed:

- ACS reads feedback velocity from the ACS API with `GetFVelocity`.
- Googol reads encoder velocity from the GTN API path with `GTN_GetEncVel`.
- Values shown in AxisView are in user units per second, matching the current position unit
  convention, so the UI displays `mm/s`.

## Interface And Runtime State

Add to the common control-card layer:

- `IControlCard.GetAllSpeedInfos(short core = 2)`
- `IControlCard.GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)`
- `ControlCardBase.CurSpeed`
- `ControlCardBase.EnsureSpeedBuffers(int requiredLength = 0)`

Add runtime speed state to axis models:

- `SingleAxisParam.CurSpeed`
- `AxisModel.CurSpeedInfos`

The default base implementations return `false` so unsupported cards do not silently claim
speed monitoring support.

## Speed Mode Persistence Bug Fix

AxisView currently persists `AxisModel` only when the AxisView window closes. If the operator
clicks "切换速度" and then restarts the software without closing AxisView cleanly, the selected
`CurSpeedType` can revert to the previously saved value.

The speed-switch command should write `AxisModel` through `ConfigManager.Write(ConfigKey.AxisModel, ModelParam)`
immediately after updating `ModelParam.CurSpeedType`. The existing close-time save remains as a
backup for other AxisView settings.

## Axis Mapping

Extend `AxisViewAxisMatcher` with `BuildSpeedSnapshot(IEnumerable<SingleAxisParam>? axes)`.
It returns a fixed-length array in the same order as the existing position and enable snapshots:

1. X
2. Y
3. Z
4. Z1
5. Z2

Missing or unconfigured axes remain `0`.

## AxisView Flow

AxisViewModel continues to use the existing `DispatcherTimer` interval of 100 ms.

On each tick:

1. Refresh positions with `GetAllPosInfos()`.
2. Update `ModelParam.CurPosInfos`.
3. Refresh axis enable LEDs.
4. Refresh actual speeds with `GetAllSpeedInfos()`.
5. Update `ModelParam.CurSpeedInfos` from `AxisViewAxisMatcher.BuildSpeedSnapshot(...)`.
6. Continue the existing core-1 position refresh.

If speed refresh fails, AxisView keeps the page usable and does not block position refresh.
The implementation logs the failure and keeps the previous speed snapshot.

## ACS Implementation

ACS overrides the unified speed methods.

For each configured axis:

1. Resolve the ACS axis from the existing configured axis mapping.
2. Read feedback velocity with `_api.GetFVelocity(acsAxis)`.
3. Round to a practical display precision.
4. Store the result in `axisConfig.CurSpeed`.
5. Store the result in `CurSpeed[axisConfig.AxisNo - 1]` when the index is valid.

The method returns `false` when the card is not connected or when an ACS API call fails.

## Googol Implementation

Googol adds a low-level helper on `GoogolGTMotion` to read encoder velocity for a range
of axes from the selected core.

GoogolControlCard overrides the unified speed methods:

1. Read raw encoder speed in pulse units from the motion helper.
2. Convert each configured axis to user units with `PulseEquivalent`.
3. Store the value in `axisConfig.CurSpeed`.
4. Store the value in `CurSpeed[axisConfig.AxisNo - 1]` when the index is valid.

The method returns `false` when the card is disconnected or when the GTN call fails.

## UI Changes

AxisView displays speed beside the existing current-position text. The page keeps its current
layout and shared styles. It does not introduce local styles.

Bindings use:

- `ModelParam.CurSpeedInfos[0]` for X
- `ModelParam.CurSpeedInfos[1]` for Y
- `ModelParam.CurSpeedInfos[2]` for Z
- `ModelParam.CurSpeedInfos[3]` for Z1
- `ModelParam.CurSpeedInfos[4]` for Z2

Each value is formatted with `F2` and labeled `mm/s`.

## Testing And Verification

Add source-level regression tests to the existing ACS test project because hardware-specific
runtime tests are not reliable without connected cards.

Tests should cover:

- Common interface exposes both `GetAllSpeedInfos` overloads.
- `ControlCardBase` has `CurSpeed` and speed-buffer support.
- `SingleAxisParam` has `CurSpeed`.
- `AxisModel` has `CurSpeedInfos`.
- `AxisViewAxisMatcher.BuildSpeedSnapshot` returns values in the `X, Y, Z, Z1, Z2` order.
- AxisView binds to `ModelParam.CurSpeedInfos`.
- AxisView persists `CurSpeedType` when the speed-switch command runs.
- ACS overrides speed reads and uses feedback velocity.
- Googol overrides speed reads and uses encoder velocity.

Build verification should include:

- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ReeYin_V.Hardware.ControlCard.csproj`
- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj`
- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/ReeYin_V.Hardware.ControlCard.Googol.csproj`
- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

## Out Of Scope

- Planning or command speed display.
- Combined vector speed display.
- Vendor-specific UI branches in AxisView.
- New configuration options for update interval or speed source.

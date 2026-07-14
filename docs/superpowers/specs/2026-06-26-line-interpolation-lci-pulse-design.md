# ACS LineInterpolation LCI Pulse Output Design

Date: 2026-06-26

## Goal

Optimize `AcsControlCard.BuildLinePtpLciInitBufferScript` so the existing PTP/e line interpolation script is preserved by default, while a per-move pulse-output option can enable ACS LCI fixed-distance pulse output based on the provided ACSPL+ reference script.

## Scope

- Add a common pulse-output configuration object to `LineInterPoParam` so callers can enable pulse output for an individual line interpolation move.
- Keep ACS-specific script generation in `ReeYin_V.Hardware.ControlCard.ACS`.
- Preserve current behavior when pulse output is not configured or disabled.
- Support both `LineInterpoMoving` and `LineInterpoMovingXseg`, since both currently route through the same PTP/e LCI initialization script builder.

## Public Model

Add a new public model under `ReeYin_V.Hardware.ControlCard.Models`, for example `LineInterpolationPulseOutputParam`, and add `LineInterPoParam.PulseOutput`.

The initial fields should be:

- `IsEnabled`: default `false`.
- `PulseWidth`: pulse width in milliseconds.
- `Interval`: fixed-distance interval in user units.
- `StartDistance`: distance from motion start where pulse generation begins.
- `EndDistance`: distance where pulse generation ends; if not greater than `StartDistance`, ACS implementation can default it to the move length.
- `RouteConfigOutput`, `ConfigOutputIndex`, `ConfigOutputCode`: optional output routing compatible with existing ACS LCI helpers.

## ACS Script Behavior

When `PulseOutput` is null or disabled, `BuildLinePtpLciInitBufferScript` keeps the existing script flow:

1. Declare `AxX`, `AxY`, start and stop positions.
2. Enable axes and apply motion tuning.
3. Move to `XStartPos/YStartPos`.
4. Run `lc.Init()`.
5. Assign stop position and move to `XStopPos/YStopPos`.
6. `STOP`.

When `PulseOutput.IsEnabled` is true, the script follows the reference ACSPL+ LCI flow:

1. Declare pulse variables and `ch`, plus global result variables for diagnostics.
2. Call `lc.SetSafetyMasks(1, 1)` before axis enable.
3. Move to the start point first.
4. Call `lc.Init()`.
5. Assign `PulseWidth`, `Interval`, `PulseStartPos`, and `PulseEndPos`.
6. Call `lc.SetMotionAxes(AxX, AxY)`.
7. Call `ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)`.
8. Optionally call `lc.SetConfigOut(...)` when output routing is enabled.
9. Call `lc.LaserEnable()` immediately before the stop-position `PTP/e` motion.
10. After the motion, call `lc.LaserDisable()`, store channel and pulse count, then call `lc.Stop(ch)` and `STOP`.

## Validation

- Reject non-finite or non-positive `PulseWidth` and `Interval` when pulse output is enabled.
- Reject non-finite `StartDistance` and `EndDistance`.
- Reject invalid ACS ConfigOut indices when routing is enabled.
- Reuse existing move-length calculation for default `EndDistance` behavior.

## Testing

- Keep the existing test that verifies non-pulse line scripts do not contain `FixedDistPulse`, `LaserEnable`, or `GetPulseCounts`.
- Add a test that builds a line interpolation script with `PulseOutput.IsEnabled=true` and asserts:
  - `real PulseWidth`, `real Interval`, `real PulseStartPos, PulseEndPos`, and `int ch` are declared.
  - `lc.SetSafetyMasks(1, 1)` is present.
  - `lc.Init()` occurs after the start `PTP/e` move.
  - `lc.FixedDistPulse(...)` occurs before `lc.LaserEnable()`.
  - `lc.LaserEnable()` occurs before the stop `PTP/e` move.
  - Cleanup and diagnostic lines are present after the stop move.
- Add source-level assertions that `LineInterpoMoving` and `LineInterpoMovingXseg` pass `param.PulseOutput` to the script builder.

## Out Of Scope

- UI changes for configuring the new line-interpolation pulse output.
- Changing existing dedicated `TryRunLciFixedDistancePulseXseg` behavior.
- Adding pulse output for arc interpolation.

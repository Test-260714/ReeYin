# HardwareTool.Motion ACS Compatibility Design

Date: 2026-06-24

## Background

`HardwareTool.Motion` is a general motion module. It currently references the shared
control-card project:

- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ReeYin_V.Hardware.ControlCard.csproj`

It does not reference `ReeYin_V.Hardware.ControlCard.ACS`, and that separation should
remain intact. ACS already inherits `ControlCardBase` and implements the public motion
operations used by the Motion module:

- `LineInterpoMoving(LineInterPoParam param)`
- `ArcInterpoMoving(ArcInterPoParam param)`
- `CustomInterpolationMoving(CustomInterPoParam param, Func<string> customOrder, bool waitend = false)`
- `SetSpecifiedIO(int part, bool onOrOff)`
- `ControlPosComparison(bool onOff, PosComparisonOutputParam param)`
- `GetAllPosInfos(...)`

The compatibility issue is mostly in the caller. `MotionModel` builds several target
position dictionaries with hard-coded `X/Y/Z/Z1/Z2` indexes from `AssignPosInfo.TargetPos`.
That is brittle for cards whose configured axes differ from those five logical axes, and
it can also produce limit-validation or indexing failures before ACS gets a valid command.

## Goals

- Make `HardwareTool.Motion` run common point, line, arc, IO, and position comparison
  operations through an ACS control card.
- Keep the dependency direction unchanged: Motion depends only on the shared
  `ControlCardBase` abstraction, not on the ACS project.
- Preserve existing behavior for Googol, ZMotion, and other cards that already work
  through `ControlCardBase`.
- Remove repeated hard-coded five-axis target construction from `MotionModel`.
- Provide tests that guard the compatibility boundary and the most important command
  construction behavior.
- Keep UI behavior unchanged unless needed for correctness.

## Non-Goals

- Do not add a project reference from `HardwareTool.Motion` to
  `ReeYin_V.Hardware.ControlCard.ACS`.
- Do not add ACS-specific type checks in the Motion module.
- Do not change `IControlCard` or `ControlCardBase` method signatures.
- Do not redesign the Motion UI or add ACS-only controls in this change.
- Do not rework ACS connection, homing, LCI, PEG, or buffer script behavior.
- Do not remove Googol-style custom interpolation support.

## Recommended Approach

Update `MotionModel` so it constructs motion requests from the selected card's configured
axes instead of assuming a fixed `X/Y/Z/Z1/Z2` shape.

Add small private helpers in `MotionModel`:

```csharp
private bool TryBuildTargetPositionMap(
    MovementLocus movementLocus,
    out Dictionary<En_AxisNum, double> targetPositions,
    out string errorMessage)

private bool TryBuildTargetPositionMap(
    CoordinatePos coordinatePos,
    out Dictionary<En_AxisNum, double> targetPositions,
    out string errorMessage)

private CustomInterPoParam CreateCustomInterpolationParam(
    Dictionary<En_AxisNum, double> finalPositions)

private LineInterPoParam CreateLineInterpolationParam(
    MovementLocus movementLocus,
    Dictionary<En_AxisNum, double> finalPositions)

private bool ExecuteLineInterpolation(
    MovementLocus movementLocus,
    Dictionary<En_AxisNum, double> finalPositions)

private bool ExecutePointMovement(MovementLocus movementLocus)
private bool ExecuteLineSegmentMovement(MovementLocus movementLocus)
private bool ExecuteArcSegmentMovement(MovementLocus movementLocus)
```

The helpers will use `ControlCard.Config.AllAxis` and each axis `AxisNo` to map
`CoordinatePos.TargetPos` values to configured `En_AxisNum` keys. Missing configured axes
or short target arrays return `false` with a clear message instead of throwing.

## Execution Behavior

### Point

Point movement builds a final target dictionary from `AssignPosInfo.TargetPos`, then runs
the existing custom interpolation wrapper. The custom order calls `LineInterpoMoving` for
the configured interpolation axes. For common XY ACS cards, this becomes a standard ACS
line interpolation through the shared abstraction.

### Line Segment

Line segment movement keeps the existing sequence:

1. Move to the configured start point.
2. Enable position comparison when requested.
3. Move to the line destination.
4. Disable position comparison when requested.

The difference is that destination dictionaries are generated from the selected card's
configured axes. XY destination values come from `OriginX/OriginY` or
`DestinationX/DestinationY`; non-interpolation axes keep their target values from
`AssignPosInfo.TargetPos` when present.

### Arc Segment

Arc movement keeps the current geometry validation and nearest-point-on-circle logic. The
ACS compatibility improvement is to pass complete arc metadata:

- `InterPoAxiss = [X, Y]`, matching the current Motion UI and ACS interpolation default.
- `FinalPosDic` containing the final XY destination and any non-interpolation axis targets.
- `Origin`, `Destination`, `Center`, `Radius`, `DrawArcMethod`, and `Dir` as today.

This lets ACS use its existing limit validation and axis mapping in `ArcInterpoMoving`.

### IO

IO remains a direct `SetSpecifiedIO` call through `ControlCardBase`. The delay behavior is
preserved, but failures should be logged consistently rather than silently ignored.

### Position Comparison

Position comparison remains a direct `ControlPosComparison` call through
`ControlCardBase`. ACS already overrides this method, so Motion should not special-case it.

## Error Handling

- If no card is selected, return `NodeStatus.Error` during module execution and show the
  existing UI prompt during manual movement.
- If a target position cannot be mapped to configured axes, log the message and stop the
  current movement.
- If a card operation returns `false`, propagate that failure to the calling movement
  method.
- Avoid `NullReferenceException` and `IndexOutOfRangeException` from incomplete
  `AssignPosInfo.TargetPos` values.
- Keep hardware SDK exceptions contained in the control-card implementations.

## Testing Strategy

Use the existing lightweight test-project style where hardware cannot be exercised safely.
Add source-level and small behavior tests that run without a physical controller.

Candidate tests:

- `HardwareTool.Motion.csproj` must not reference
  `ReeYin_V.Hardware.ControlCard.ACS.csproj`.
- `MotionModel` must not contain repeated hard-coded five-axis target dictionary blocks.
- `MotionModel` must define a helper that maps target positions through
  `ControlCard.Config.AllAxis` and `AxisNo`.
- Arc execution must set `ArcInterPoParam.InterPoAxiss` and `FinalPosDic`.
- Point and line execution must call focused helper methods instead of duplicating
  `CustomInterpolationMoving` request construction.

Add the source-level assertions to the existing ACS test harness because it already
validates cross-project source contracts and avoids introducing a new test runner for this
small compatibility change.

## Verification

Build the touched projects with an explicit `SolutionDir` to avoid the current
`*Undefined*` post-build copy failure:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet build "Tools\Hardware\HardwareTool.Motion\HardwareTool.Motion.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

If tests are added to the existing ACS test project, also run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Acceptable result: builds succeed and the test executable reports all compatibility tests
as passing. Existing unrelated external dependency warnings are acceptable if there are no
new errors.

## Risks

- Existing `MotionModel` has duplicated execution branches; refactoring must preserve the
  current sequence order for working cards.
- Some historical recipes may contain incomplete `AssignPosInfo.TargetPos` arrays. The new
  validation will fail safer, but old silent behavior may become a visible error.
- ACS arc behavior depends on accurate current-position feedback for nearest-circle
  correction. If the card is not initialized or positions are stale, arc validation should
  fail instead of issuing a risky move.
- Source-level tests guard structure but cannot prove real hardware motion behavior.

## Acceptance Criteria

- `HardwareTool.Motion` still references only the shared control-card project.
- Motion point, line, arc, IO, and position comparison flows call only `ControlCardBase`
  members.
- Target position dictionaries are built from configured axes instead of hard-coded five
  axis blocks.
- ACS receives complete line and arc parameters through the existing shared abstractions.
- The touched project builds with explicit `SolutionDir`.
- Added compatibility tests pass.

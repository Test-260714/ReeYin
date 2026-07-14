# ACS XSEG Interpolation Extension Design

Date: 2026-06-15

## Background

`ReeYin_V.Hardware.ControlCard.ACS` already implements basic interpolation in:

- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`
- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CustomInterpolation.cs`

The existing line interpolation path uses `Api.Line(...)` or `Api.ExtLine(...)`. This behavior must remain available because existing modules call `LineInterpoMoving(LineInterPoParam param)` through the shared control-card abstraction.

ACS also provides the extended segmented motion flow corresponding to ACSPL+ `XSEG...ENDS`:

1. `ExtendedSegmentedMotionV2(...)` starts the XSEG trajectory queue.
2. `SegmentLineV2(...)` appends line segments.
3. `SegmentArc1V2(...)` or `SegmentArc2V2(...)` appends arc segments.
4. `EndSequenceM(...)` closes the segment sequence.

XSEG is a better fit for multi-segment contouring, LCI laser synchronization, segment-state control, and future equal-distance pulse integration.

## Goals

- Keep the existing basic interpolation behavior intact.
- Add ACS-specific XSEG line interpolation without changing `IControlCard` or `ControlCardBase`.
- Add ACS-specific XSEG arc interpolation as a matching extension point.
- Reuse existing axis preparation, limit validation, speed configuration, wait, and state update helpers.
- Make the new XSEG flow explicit and testable in the ACS project.
- Leave room for future LCI segment-state support without forcing it into the first change.

## Non-Goals

- Do not remove or replace `LineInterpoMoving(LineInterPoParam param)`.
- Do not change public shared interfaces used by other control-card vendors.
- Do not alter Googol or other control-card implementations.
- Do not implement full LCI configuration or equal-distance pulse setup in this change.
- Do not add UI controls unless a later request explicitly asks for UI switching.
- Do not change existing connection, homing, IO, PEG, or data collection behavior.

## Public API Design

Add ACS-only public methods on `AcsControlCard`:

```csharp
public bool LineInterpoMovingXseg(LineInterPoParam param)
public bool ArcInterpoMovingXseg(ArcInterPoParam param)
```

These methods are intentionally not added to `IControlCard` because XSEG is ACS-specific and existing shared modules should keep working without knowing about ACS internals.

Existing methods remain unchanged:

```csharp
public override bool LineInterpoMoving(LineInterPoParam param)
public override bool ArcInterpoMoving(ArcInterPoParam param)
```

## Basic Line Behavior

`LineInterpoMoving(LineInterPoParam param)` continues to:

- Validate connection and parameter presence.
- Build axis and target arrays through `TryBuildInterpolationMove`.
- Validate limits through existing helpers.
- Prepare and configure axes.
- Run `_api.ExtLine(axes, target, velocity)` when a positive vector velocity is available.
- Otherwise run `_api.Line(axes, target)`.
- Wait through `WaitUntilAllAxesStopped` when `param.waitforend` is true.
- Update axis states.

This preserves the current behavior and risk profile for existing callers.

## XSEG Line Behavior

`LineInterpoMovingXseg(LineInterPoParam param)` will use the same validation and preparation path as the basic line method, then run:

1. Read the current position for each requested axis.
2. Start XSEG with `ExtendedSegmentedMotionV2(...)` using the current position as the XSEG start point.
3. Append one line segment with `SegmentLineV2(...)` using the target point.
4. End the sequence with `EndSequenceM(axes)`.
5. Wait for all axes to stop if `param.waitforend` is true.
6. Update axis states.

The XSEG start point must match the current feedback position to avoid a discontinuity between the controller's actual path start and the commanded XSEG start.

The first implementation uses `MotionFlags.ACSC_AMF_VELOCITY` only when the computed interpolation velocity is positive. Unused XSEG and segment arguments use `Api.ACSC_NONE` or `null`, matching ACS.SPiiPlusNET conventions.

## XSEG Arc Behavior

`ArcInterpoMovingXseg(ArcInterPoParam param)` will mirror the existing arc validation:

- Resolve the interpolation axes, defaulting to X/Y when absent.
- Build and validate target positions.
- Resolve the center point through `TryResolveArcCenter`.
- Prepare and configure axes.

It then runs:

1. Read the current positions for the requested axes.
2. Start XSEG with `ExtendedSegmentedMotionV2(...)`.
3. Append one arc segment.
4. End the sequence with `EndSequenceM(axes)`.
5. Wait and update states like the basic arc method.

For the first implementation, use `SegmentArc1V2(...)` because the current `ArcInterPoParam` model already resolves arcs as center point plus final point plus direction. This keeps the new XSEG method aligned with the existing `ArcInterpoMoving` semantics.

## Internal Helper Design

Add focused helpers in the ACS partial class:

```csharp
private bool TryGetCurrentInterpolationPoint(En_AxisNum[] axisIds, out double[] point)
private void StartXseg(Axis[] axes, double[] startPoint, double velocity)
private void AddXsegLine(Axis[] axes, double[] target, double velocity)
private void AddXsegArc1(Axis[] axes, double[] center, double[] finalPoint, RotationDirection rotation, double velocity)
private static MotionFlags GetVelocityFlag(double velocity)
private static double GetOptionalVelocity(double velocity)
```

The helpers keep `LineInterpoMovingXseg` and `ArcInterpoMovingXseg` small and make method-level tests easier.

## LCI Extension Path

The first change does not enable LCI state control. The method structure should still make future LCI support straightforward by adding an overload or an ACS-specific request model later:

```csharp
public bool LineInterpoMovingXseg(AcsXsegLineInterPoParam param)
```

Future LCI support can set `MotionFlags.ACSC_AMF_LCI_STATE` on `SegmentLineV2` or `SegmentArc1V2` and pass the desired `lciState` argument per segment.

## Error Handling

New methods follow the existing ACS style:

- Return `false` for validation failures.
- Catch SDK exceptions, log a clear `Console.WriteLine(...)` message, and return `false`.
- Use existing helpers for limit and axis readiness messages where possible.
- Never call `EndSequenceM` if XSEG start or segment append throws before a sequence is valid.
- If waiting times out, return `false` after the ACS command sequence has been sent.

## Testing Strategy

Add tests in `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs` before implementation:

- Verify `LineInterpoMoving` still contains the basic `_api.Line` / `_api.ExtLine` path and does not call `LineInterpoMovingXseg`.
- Verify `LineInterpoMovingXseg` exists and uses `ExtendedSegmentedMotionV2`, `SegmentLineV2`, and `EndSequenceM`.
- Verify `ArcInterpoMovingXseg` exists and uses `ExtendedSegmentedMotionV2`, `SegmentArc1V2`, and `EndSequenceM`.
- Verify no shared interface file references `LineInterpoMovingXseg`, keeping the extension ACS-specific.

These tests match the current project style, which uses source-level assertions for hardware-dependent behavior where real ACS hardware cannot be exercised in CI.

## Verification

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Then run the test executable if the build succeeds:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Acceptable result: tests pass. Existing unrelated warnings from external dependencies are acceptable if there are no build errors.

## Risks

- XSEG start position must match current controller feedback position; using a stale or configured origin can cause an unexpected path.
- `SegmentArc1V2` signature has many optional arguments; incorrect `Api.ACSC_NONE` placement can compile but behave incorrectly.
- Some controllers or firmware versions may support `Line/ExtLine` but not the V2 XSEG APIs; method exceptions must remain contained.
- Existing tests are mostly source-level because hardware API calls cannot be executed safely without a controller.

## Acceptance Criteria

- Existing `LineInterpoMoving` behavior remains available and source tests confirm the basic line path.
- `LineInterpoMovingXseg` compiles and uses the XSEG line queue flow.
- `ArcInterpoMovingXseg` compiles and uses the XSEG arc queue flow.
- No shared `IControlCard` or `ControlCardBase` signature changes are required.
- ACS tests build and pass.

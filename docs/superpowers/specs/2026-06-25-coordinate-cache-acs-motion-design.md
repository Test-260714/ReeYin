# CoordinateCache ACS Motion Adaptation Design

## Context

`CoordinateCacheViewModel` handles the "move to this position" command in the base control-card project. The current flow always wraps an X/Y line move in `CustomInterpolationMoving`, then starts extra configured axes with single-axis absolute moves. That path is compatible with the existing non-ACS workflow, but ACS already exposes the neutral `ICoordinatedMotionCard` capability and maps `MoveCoordinated(Line)` to its own ACS line interpolation implementation.

The goal is to make coordinate-cache target movement use the ACS-capable coordinated-motion path when the active card supports it, without adding a direct dependency from the base control-card project to the ACS project.

## Approved Approach

Use capability detection instead of vendor or type-name checks.

- If the active card implements `ICoordinatedMotionCard` and `SupportsCoordinatedMotion` is true, call `MoveCoordinated` with `CoordinatedMotionKind.Line`.
- If the active card does not support that capability, keep the existing `CustomInterpolationMoving + LineInterpoMoving` fallback.
- Keep extra-axis movement after the X/Y move, preserving current behavior for Z/Z1/Z2 and other configured axes.
- Do not reference ACS namespaces, ACS concrete types, or ACS assembly names from `CoordinateCacheViewModel`.

## Components

- `CoordinateCacheViewModel`
  - Add a small helper for X/Y target movement.
  - Build target-position dictionaries from the configured axes.
  - Route through `ICoordinatedMotionCard` when available.
  - Preserve the current fallback path when the capability is unavailable.
- `ICoordinatedMotionCard`
  - Existing capability interface in the base control-card project.
  - No contract changes are required.
- ACS card
  - Already implements `ICoordinatedMotionCard`.
  - No ACS project changes are expected for this adaptation.

## Data Flow

1. User chooses "move to this position" from CoordinateCache.
2. The view model validates selection, axis configuration, X/Y presence, confirmation, and in-progress task state.
3. The view model builds:
   - target dictionary keyed by `En_AxisNum`;
   - target array aligned with the current saved coordinate list;
   - X/Y axis list for the planar move.
4. The background task runs the planar move:
   - capability path: `MoveCoordinated(Line, axes: X/Y, targetPositions, waitForEnd: true)`;
   - fallback path: current `CustomInterpolationMoving` wrapper with `LineInterpoMoving`.
5. After planar movement succeeds or completes according to the selected path, extra axes are moved as before.

## Error Handling

- If the coordinated capability reports failure, write the returned message to the console and skip extra-axis movement.
- If the fallback path fails, keep the existing failure console message and skip extra-axis movement.
- Existing UI validation messages remain unchanged for missing selection, missing control card, missing axes, and duplicate move task.
- The helper returns a boolean so the command task can avoid follow-up axis moves when X/Y movement fails.

## Testing

Add tests in the existing lightweight ACS test harness:

- CoordinateCache source uses `ICoordinatedMotionCard` / `MoveCoordinated`.
- CoordinateCache source does not hard-code ACS concrete type names or ACS namespaces.
- CoordinateCache fallback path still references `CustomInterpolationMoving` and `LineInterpoMoving`.

Then run the ACS test harness and build the base control-card project with `dotnet build`.

## Out Of Scope

- Refactoring `AxisViewModel` target-position movement.
- Changing ACS interpolation internals.
- Changing the coordinate-cache XAML columns or saved coordinate model.
- Reworking extra-axis sequencing or adding new safety rules.

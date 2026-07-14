# Trajectory Designer Shape Center And Inner Trajectory Design

Date: 2026-07-08

## Goal

Enhance `SkiaTrajectoryDesignerControl` so circle and rectangle shapes are created at a caller-provided fixed center by default, can be restored to that center, and can display/generated internal trajectory primitives that move, rotate, and scale with the shape.

## Scope

- Add reusable control properties `DefaultCenterX` and `DefaultCenterY`.
- Circle and rectangle creation uses the fixed center as the initial shape center.
- Existing drag behavior remains: users can move a placed shape away from the fixed center.
- Add a restore action that resets the selected shape center to the fixed center.
- Add internal trajectory options for circle and rectangle shapes:
  - None
  - Equidistant points
  - Horizontal lines
  - Vertical lines
  - Cross lines
- Internal trajectory uses relative geometry and is rendered through the shape transform, so it follows movement, rotation, and scale.

## Shape Model

`EditableTrajectoryShape` gains:

- `InnerPattern`: selected internal trajectory generation mode.
- `InnerSpacing`: spacing for equidistant point and spacing-based line generation.
- `InnerLineCount`: count for circle horizontal/vertical/cross lines and optional rectangle cross expansion.

The existing `X`, `Y`, `RotationAngle`, and `Scale` remain the source of truth for shape transforms.

## Generation Rules

Circle:

- Equidistant points: start at local `(0,0)`, expand by grid spacing, keep points inside radius.
- Horizontal lines: generate evenly distributed chord lines inside the circle, matching the existing `CreateTrajectoryModel` circle line offset strategy.
- Vertical lines: same offset strategy, swapping axes.
- Cross lines: generate diameter lines through the center over the 180 degree range.

Rectangle:

- Equidistant points: generate grid points inside the rectangle bounds using spacing.
- Horizontal lines: generate lines across the rectangle at spacing intervals, including top and bottom boundaries.
- Vertical lines: generate lines across the rectangle at spacing intervals, including left and right boundaries.
- Cross lines: generate both diagonals; if `InnerLineCount` is greater than 2, add additional center-crossing lines distributed over 180 degrees and clipped to the rectangle.

## Rendering

- Outer shapes keep the current stroke/fill style.
- Internal points render as small dots.
- Internal lines render as thinner strokes.
- The control translates to `shape.X/Y`, rotates by `shape.RotationAngle`, and draws scaled geometry, ensuring internal trajectories follow all transforms.

## Test Page

`TrajectoryDesignerTestView` adds:

- Fixed center X/Y editors bound to the view model.
- A restore selected shape button.
- Internal trajectory pattern selector.
- Internal spacing editor.
- Internal line count editor.

The test page binds `DefaultCenterX` and `DefaultCenterY` into `SkiaTrajectoryDesignerControl`.

## Validation

- Build `Custom.WaferFlatnessMeasure.csproj` with module copy skipped when the host app locks the module DLL.
- Manually verify in the test page:
  - New circle/rectangle appear at fixed center.
  - Dragging moves the shape and internal trajectory together.
  - Restore returns selected shape to fixed center.
  - Changing inner pattern/spacing/count updates the drawn internals.
  - Rotation and scale affect internal trajectories with the outer shape.

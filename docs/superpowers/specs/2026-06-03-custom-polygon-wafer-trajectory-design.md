# Custom Polygon Wafer Trajectory Design

Date: 2026-06-03

## Context

`Custom.WaferFlatnessMeasure` already exposes a trajectory generation tab in `WaferFlatnessConfigView.xaml`. The existing flow lets the user choose a `CircleTrajectoryGenerateMode`, configure mode-specific parameters, click the existing generate button, and store generated points or lines in `SensorMotionControlModel.AllLocusInfo` for preview and execution.

The requested feature adds a new trajectory type, `自定义多边形`, where the user enters at least three polygon vertices in absolute machine XY coordinates, sets a point spacing such as `0.1 mm`, and generates point loci inside that polygon.

## Goals

- Add `自定义多边形` as a selectable trajectory generation mode.
- Let the user edit polygon vertices as absolute XY coordinates in the same coordinate system as existing locus data.
- Reuse the existing point spacing parameter for polygon grid spacing.
- Generate `LocusInfo.PointType` records for points inside or on the boundary of the polygon.
- Show the polygon boundary in the existing trajectory preview so the user can verify the shape and vertex order.
- Validate invalid input before generation with clear messages.

## Non-Goals

- No freehand drawing or mouse-based polygon editing.
- No separate execution pipeline; generated points use the existing `AllLocusInfo` and execution flow.
- No changes to line trajectory generation.
- No new coordinate-link source for polygon vertices.

## Recommended Approach

Extend the current `CreateTrajectoryModel` and `WaferFlatnessConfigView` path instead of adding a separate generator. This keeps generation, preview, optimal-path sorting, and command handling consistent with existing modes.

## Model Changes

Add a serializable `CustomPolygonPoint` model with `X` and `Y` properties. Add an observable `CustomPolygonPoints` collection to `CreateTrajectoryModel` and subscribe to collection changes so `CircleGeneratorSummaryText` refreshes when points are added or removed.

Add helper methods:

- `AddCustomPolygonPoint(double x, double y)`
- `RemoveCustomPolygonPoint(CustomPolygonPoint? point)`
- `ValidateCustomPolygonPoints()`
- `BuildCustomPolygonPointLocusInfos()`

The polygon point generation will:

1. Validate at least three vertices.
2. Validate the polygon area is not near zero.
3. Validate `PointSpacing > 0`.
4. Compute the polygon bounding box.
5. Iterate grid coordinates from `minX` to `maxX` and from `minY` to `maxY` in `PointSpacing` increments.
6. Include each point that is inside the polygon or on a polygon edge.
7. Round coordinates using the existing `RoundCoordinate` method.
8. Emit point loci where origin and target are identical.

Boundary points count as valid interior points. This avoids missing edge samples when the desired wafer area is defined exactly by the polygon.

## UI Changes

In `WaferFlatnessConfigView.xaml`, keep the existing trajectory generation section and add a mode-specific polygon parameter panel visible only when `CircleGenerateMode == 自定义多边形`.

The panel will contain:

- Existing `PointSpacing` field, reused through `IsPointSpacingEditable`.
- Numeric inputs for a new vertex X and Y.
- `添加` and `删除` buttons bound to new ViewModel commands.
- A list of configured vertices, editable inline through numeric fields.
- Help text stating that vertices are absolute XY coordinates and must be entered in boundary order.

The existing generate button remains the only action that creates `AllLocusInfo`.

## ViewModel Changes

Add selection state and commands to `WaferFlatnessConfigViewModel`:

- `SelectedCustomPolygonPoint`
- `AddCustomPolygonPointCommand`
- `RemoveCustomPolygonPointCommand`

Attach collection listeners for `CustomPolygonPoints` in the same style as custom rings so preview refreshes when vertices change. When the current mode is `自定义多边形`, preview will draw the polygon boundary in addition to generated point loci.

## Preview Behavior

The preview bounds should include polygon vertices in addition to generated loci and the existing circular boundary. For `自定义多边形`, draw a closed polygon outline with a dashed or highlighted stroke. Generated interior points continue using existing point rendering.

If no generated loci exist yet, the preview still shows the polygon boundary as soon as at least three vertices are entered.

## Validation And Errors

Generation should fail with a message box when:

- Fewer than three polygon vertices are configured.
- `PointSpacing` is missing or not greater than zero.
- Polygon area is too close to zero.
- Generated point count is zero.

Self-intersecting polygons are not explicitly rejected in the first implementation; the standard ray-casting inside test will define the result. The UI help text should recommend entering a simple, non-self-intersecting polygon in boundary order.

## Testing Plan

Use TDD for the generation algorithm before production changes:

- A square polygon with spacing `1` generates the expected 3x3 grid when boundary points are included.
- A triangle polygon includes expected interior and boundary points and excludes outside points.
- Fewer than three vertices throws a clear validation exception.
- Collinear vertices throw a clear validation exception.
- `自定义多边形` appears as a point generation mode and makes point spacing editable.

If adding a full test project is too costly for this repository, create focused tests around `CreateTrajectoryModel` in the nearest existing or new test project and keep UI verification manual through build plus preview inspection.

## Build Verification

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj
```

Then manually open the Wafer Flatness config page, select `自定义多边形`, add at least three vertices, set spacing, generate the trajectory, and verify preview plus generated point rows.

## Open Constraints

The current working directory did not report as a git repository when `git status` was run. The design document can be written, but committing may not be possible unless the repository root or git metadata is restored.

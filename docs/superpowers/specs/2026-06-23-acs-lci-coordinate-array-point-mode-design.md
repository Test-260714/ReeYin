# ACS LCI Coordinate Array Point Mode Design

Date: 2026-06-23

## Goal
Add an ACS-compatible point trajectory path for `Custom.WaferFlatnessMeasure` when point generation produces many input points. The ACS path must preserve the input point order and use the LCI Coordinate Array Pulse flow from section 5 of the local `Laser Control Interface(LCI)` application examples instead of the current Googol-style buffered IO toggling path.

## Confirmed Behavior
- For ACS cards, point execution must keep the generated point collection order exactly as provided.
- `CreateTrajectoryModel.IsOptimalPathEnabled` must not reorder ACS Coordinate Array point execution.
- The existing Googol point execution path must remain available and unchanged for non-ACS cards.
- The existing ACS fixed-distance line LCI path remains separate from the new coordinate-array point path.

## Approach
Implement a new ACS LCI runner in `AcsLciFixedDistancePulse.cs` because that file already owns the ACS LCI script-building and buffer-runner helpers. The new runner will follow the manual's Coordinate Array Pulse flow:

1. Declare X/Y coordinate arrays and point count.
2. Initialize LCI and enable the X/Y axes.
3. Move to the first point.
4. Set the motion axes and `MultiAxWinSize`.
5. Start `CoordinateArrPulse`.
6. Enable laser output.
7. Execute an XSEG path that visits all points in order.
8. Disable laser, read pulse count, stop LCI, and stop the buffer.

To support many points without generating thousands of coordinate-assignment lines, the runner will compile a compact script with global coordinate arrays and write the point data through the ACS .NET API before running the buffer. The script still contains the same LCI mode setup as the manual example, while the data-loading responsibility stays in C#.

## ACS Control-Card API
Add these public types and methods to `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs`:

- `AcsLciCoordinateArrayPulseParam`
  - `BufferNo`, `AxisX`, `AxisY`
  - `PulseWidth`
  - `MultiAxWinSize`
  - `RouteConfigOutput`, `ConfigOutputIndex`, `ConfigOutputCode`
  - `Velocity`, defaulting to `10`
  - `Timeout`
  - `List<AcsPoint2D> Points`
- `AcsLciCoordinateArrayPulseResult`
  - `Channel`
  - `PulseCount`
  - `Script`
  - `PointCount`
- `TryRunLciCoordinateArrayPulse(AcsLciCoordinateArrayPulseParam? param, out AcsLciCoordinateArrayPulseResult result, out string message)`
- `BuildLciCoordinateArrayPulseScript(AcsLciCoordinateArrayPulseParam param)`

The runner will reuse the existing buffer pipeline: prepare buffer, stop old buffer execution, load script, compile script, write X/Y arrays, run buffer, wait for end, read globals, and cleanup on failure.

## ACSPL Script Shape
The generated script will declare global arrays sized to the point count:

```acspl
global LCI lc
global int RY_LCI_CHANNEL
global int RY_LCI_PULSE_COUNT
global real RY_LCI_XCOORD(<PointCount>)
global real RY_LCI_YCOORD(<PointCount>)
```

It will use a loop for XSEG line generation so the script stays compact:

```acspl
PTP/e (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)
lc.SetMotionAxes(AxX, AxY)
lc.MultiAxWinSize = <window>
ch = lc.CoordinateArrPulse(PointsNum, PulseWidth, RY_LCI_XCOORD, RY_LCI_YCOORD)
lc.LaserEnable()
XSEG (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)
while PointIndex < PointsNum
    LINE (AxX, AxY), RY_LCI_XCOORD(PointIndex), RY_LCI_YCOORD(PointIndex)
    PointIndex = PointIndex + 1
end
ENDS (AxX, AxY)
till GSEG(AxX) = -1
lc.LaserDisable()
RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)
lc.Stop(ch)
STOP
```

The first point is the move/start point. Each subsequent point is appended to the XSEG path in ascending index order.

## Wafer Integration
`SensorMotionControlModel.ExecutePointMoving` will gain an ACS-specific branch:

- Detect ACS support by reflection, matching the current fixed-distance runner pattern, so `Custom.WaferFlatnessMeasure` does not need a direct project reference to the ACS hardware project.
- Build the point execution plan with input-order preservation when the card supports the ACS coordinate-array runner.
- Convert `orderedLocusInfos` to the new ACS parameter object's `Points` collection using each `LocusInfo.TargetX` and `LocusInfo.TargetY`.
- Map existing ACS LCI config values where possible:
  - buffer number, axes, pulse width, config output routing, timeout
  - add a coordinate-array window property to the wafer ACS LCI config, defaulting to `0.001`
  - add a coordinate-array velocity property to the wafer ACS LCI config, defaulting to `10`
- Keep the existing event flow:
  - publish `IsPoint`, `PointCollectionStepInfo`, and `CollectPoints`
  - publish `TrrigerStartCollect` before the ACS runner starts
  - publish `TrrigerStopCollect` after the ACS runner completes for normal point runs
  - keep the existing calibration event behavior
- If the ACS runner fails, log the ACS diagnostic message and mark trajectory tracking as incomplete.

The existing Googol path continues to run for non-ACS cards. For ACS cards, runner discovery failure is treated as an unsupported ACS configuration and must not fall back to the Googol buffered IO path, because the base `BufIO`, `BufDelay`, and `QuerySpace` implementations do not provide ACS queue semantics.

## Validation And Error Handling
- Reject null parameters.
- Require at least two finite points.
- Require positive pulse width, timeout, and `MultiAxWinSize`.
- Validate ConfigOut indexes with the same accepted range as the fixed-distance runner.
- Report compile/run/write-array failures with buffer diagnostics.
- Always attempt `LaserDisable`, `lc.Stop(...)`, and buffer stop during cleanup.

## Tests
Add or extend the console ACS tests to cover:

- `BuildLciCoordinateArrayPulseScript` includes `CoordinateArrPulse`, `MultiAxWinSize`, X/Y global arrays, XSEG/LINE loop, `ENDS`, `GSEG`, pulse count read, and channel stop.
- The runner writes the coordinate arrays with `_api.WriteVariable` between compile and run.
- The runner uses the same load/compile/run/wait/cleanup buffer pipeline as the existing LCI methods.
- Wafer point execution can find `TryRunLciCoordinateArrayPulse` by reflection.
- Wafer point execution preserves input order for ACS even when shortest-path optimization is enabled.
- Existing Googol/legacy point execution code remains present.

## Out Of Scope
- Replacing the existing ACS fixed-distance line pulse mode.
- Adding 3-axis coordinate-array support.
- Changing sensor data-processing algorithms or output result payload formats.
- Changing hardware wiring or LCI option licensing checks beyond reporting ACS runtime errors.

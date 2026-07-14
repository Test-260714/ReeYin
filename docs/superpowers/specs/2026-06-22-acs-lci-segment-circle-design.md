# ACS LCI Segment Circle Design

Date: 2026-06-22

## Goal
Add an ACS LCI segmented-gate circle helper that draws a full circle from a center point and radius, and expose it on the ACS control-card configuration page for manual testing.

## Behavior
- The caller provides buffer number, ACS X/Y axis numbers, velocity, XSEG start point, circle center, radius, gate active state, and timeout.
- The generated ACSPL+ script explicitly defines `pi = ACOS(-1)`.
- The script moves to the XSEG start point, initializes LCI, enables segment gating, moves with `LINE/p` to a point on the circle, draws a full circle with `ARC2/p`, waits for `GSEG(AxX) = -1`, disables the laser, stops LCI, and returns to the XSEG start point.
- The circle start point is computed as `(CenterX, CenterY - Radius)`, matching the provided reference where center `(10,5)` and radius `5` move first to `(10,0)`.

## UI
Extend the existing `LCI脉冲` tab in `AcsControlCardConfigView` with a circle test section. The section binds to new ViewModel properties and invokes a new command that calls the circle runner.

## Testing
Add console tests in `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs` that verify script content and call ordering, runner pipeline usage, and ViewModel/XAML surface bindings.

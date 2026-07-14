# ACS LCI Motion Profile Config Design

Date: 2026-06-29

## Goal
Remove hard-coded ACSPL+ motion tuning formulas from `AcsLciFixedDistancePulse.cs` and generate all LCI movement scripts from explicit motion profile configuration.

## Scope
The change applies to all LCI script builders in the ACS control-card project:

- Fixed-distance pulse PTP flow.
- Segment circle XSEG flow.
- Coordinate-array pulse XSEG flow.

## Design
Add a small reusable motion profile model with five explicit values: velocity, acceleration, deceleration, kill deceleration, and jerk. Each LCI parameter object carries a profile instead of relying on helper defaults. Script generation writes:

- `VEL(axis) = <configured velocity>`
- `ACC(axis) = <configured acceleration>`
- `DEC(axis) = <configured deceleration>`
- `KDEC(axis) = <configured kill deceleration>`
- `JERK(axis) = <configured jerk>`

The script builder will not emit formulas such as `10 * VEL(...)` or `10 * ACC(...)`.

## Persistence And UI
Persist the fixed-distance pulse motion profile under `AcsControlCardOptions.LciFixedDistancePulse`, and persist the circle profile under `AcsControlCardOptions.LciSegmentCircle`. The ACS configuration page exposes the profile fields near each LCI motion section so operators can tune them with the same saved control-card configuration file.

## Defaults
Use conservative defaults aligned with the previous generated behavior:

- Fixed-distance pulse: velocity `10`, acceleration `100`, deceleration `100`, kill deceleration `1000`, jerk `1000`.
- Coordinate-array pulse: velocity `10`, acceleration `100`, deceleration `100`, kill deceleration `1000`, jerk `1000`.
- Segment circle: velocity `50`, acceleration `500`, deceleration `500`, kill deceleration `5000`, jerk `5000`.

These values are safe starting points, not machine limits. Final values should be tuned for the actual axes, load, and process quality.

## Testing
Add console tests that verify generated ACSPL+ scripts contain explicit profile assignments and do not contain multiplier-based default formulas.

# SpeedSetting LCI Profile Unification Design

Date: 2026-06-29

## Goal
Unify ACS LCI motion tuning with the base control-card speed-level model so the project does not maintain a separate `AcsLciMotionProfile` type.

## Design
Use `SpeedSetting` from `ReeYin_V.Hardware.ControlCard` as the common speed-level model. Keep existing fields for compatibility:

- `StartSpeed`
- `MaxSpeed`
- `AccSpeed`

Add explicit ACS-compatible fields:

- `DecSpeed`
- `KillDecSpeed`
- `Jerk`

ACSPL+ mapping:

- `MaxSpeed` -> `VEL`
- `AccSpeed` -> `ACC`
- `DecSpeed` -> `DEC`
- `KillDecSpeed` -> `KDEC`
- `Jerk` -> `JERK`

For old serialized configs, non-positive new fields resolve from existing values at runtime:

- `DecSpeed` falls back to `AccSpeed`
- `KillDecSpeed` falls back to `AccSpeed * 10`
- `Jerk` falls back to `AccSpeed * 10`

## ACS LCI Integration
Remove `AcsLciMotionProfile`. LCI parameter classes use `SpeedSetting` directly. LCI option models continue exposing simple numeric fields for the UI, but their `ToMotionProfile()` helpers return `SpeedSetting`.

The ACS script builder only emits explicit numeric values. It must not emit expressions such as `10 * VEL(...)` or `10 * ACC(...)`.

## Compatibility
Low/Mid/High/Work/Reset speed levels remain represented by `SpeedSetting.SpeedType`. Future LCI configuration can select a speed level by `EN_SpeedType`; this refactor first removes the duplicate LCI-only class while preserving current numeric UI behavior.

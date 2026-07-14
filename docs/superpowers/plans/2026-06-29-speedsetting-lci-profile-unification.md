# SpeedSetting LCI Profile Unification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove ACS LCI's separate motion-profile class and reuse the base `SpeedSetting` model for velocity, acceleration, deceleration, kill deceleration, and jerk.

**Architecture:** Extend `SpeedSetting` with missing ACS-compatible fields, replace `AcsLciMotionProfile` usages with `SpeedSetting`, and keep ACS option/UI fields as simple persisted values that convert to `SpeedSetting`.

**Tech Stack:** C# .NET 8, WPF/Prism BindableBase, ACSPL+ script generation, existing console-style tests.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/SingleAxisParam.cs`: extend `SpeedSetting`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs`: remove `AcsLciMotionProfile` and use `SpeedSetting`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs`: return `SpeedSetting` from LCI option helpers.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`: replace `AcsLciMotionProfile` fallback with `SpeedSetting`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`: update tests to require `SpeedSetting` reuse.

---

### Task 1: RED Tests

- [ ] Add tests that instantiate `SpeedSetting` with `MaxSpeed/AccSpeed/DecSpeed/KillDecSpeed/Jerk` and verify ACS LCI scripts use those values.
- [ ] Add source assertions that `AcsLciFixedDistancePulse.cs` no longer contains `AcsLciMotionProfile`.
- [ ] Run ACS tests build and confirm it fails because `SpeedSetting` lacks new fields and ACS still references the old type.

### Task 2: Extend Base SpeedSetting

- [ ] Add `DecSpeed`, `KillDecSpeed`, and `Jerk` properties to `SpeedSetting`.
- [ ] Add runtime fallback helper methods that resolve non-positive values from `AccSpeed`.
- [ ] Keep existing serialized config compatibility by not renaming existing fields.

### Task 3: Replace ACS LCI Type Usage

- [ ] Delete `AcsLciMotionProfile` from ACS LCI code.
- [ ] Change LCI parameter `MotionProfile` properties to `SpeedSetting`.
- [ ] Change script generation and validation to use `SpeedSetting`.
- [ ] Keep fallback overloads for interpolation buffer callers by creating a `SpeedSetting` internally.

### Task 4: Verify

- [ ] Build ACS project.
- [ ] Build ACS tests project.
- [ ] Run ACS console tests and report any unrelated pre-existing failure separately.
- [ ] Search source for `AcsLciMotionProfile` to confirm it is removed from production code.

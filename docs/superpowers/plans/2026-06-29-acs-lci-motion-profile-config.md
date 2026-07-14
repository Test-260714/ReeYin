# ACS LCI Motion Profile Config Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate ACS LCI scripts from explicit velocity, acceleration, deceleration, kill-deceleration, and jerk configuration instead of hard-coded formulas.

**Architecture:** Introduce a reusable `AcsLciMotionProfile` value object in `AcsLciFixedDistancePulse.cs`, persist matching values in ACS option models, and pass the configured profile into each LCI runner parameter. Replace `AppendAxisMotionTuning` formula output with explicit numeric assignments.

**Tech Stack:** C# .NET 8, ACS.SPiiPlusNET ACSPL+ script generation, WPF XAML, existing console-style ACS tests.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs` to add script-generation assertions.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs` to add motion profile parameters, validation, and script output.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs` to persist profile fields and provide reasonable defaults.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs` to copy profile values into runner parameters.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml` to expose profile inputs.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs` if synchronized-trigger callers need to pass a profile.

---

### Task 1: Tests

- [ ] Add tests that create each LCI parameter with explicit profile values.
- [ ] Assert generated scripts include `VEL/ACC/DEC/KDEC/JERK` numeric assignments.
- [ ] Assert generated scripts do not include `10 * VEL(` or `10 * ACC(`.
- [ ] Run the ACS test project and confirm the new tests fail because the production code still emits formulas.

### Task 2: Motion Profile Model

- [ ] Add `AcsLciMotionProfile` with defaults `10/100/100/1000/1000`.
- [ ] Add `AcsLciMotionProfile.CreateSegmentCircleDefault()` returning `50/500/500/5000/5000`.
- [ ] Add `MotionProfile` properties to fixed-distance, segment-circle, and coordinate-array LCI parameter classes.
- [ ] Validate that all five profile values are finite and positive.

### Task 3: Script Generation

- [ ] Replace helper overloads that accept only velocity with a helper that accepts `AcsLciMotionProfile`.
- [ ] Emit explicit ACSPL+ assignments for all profile fields.
- [ ] Preserve existing point, LCI, ConfigOut, and timeout behavior.

### Task 4: Persisted Config And UI

- [ ] Add profile fields to `AcsLciFixedDistancePulseConfig` and `AcsLciSegmentCircleConfig`.
- [ ] Bind these fields in the ACS config page near the fixed-distance and circle forms.
- [ ] Use defaults that match the documented recommendations.

### Task 5: Caller Wiring And Verification

- [ ] Pass persisted fixed-distance profile values from the config ViewModel to `AcsLciFixedDistancePulseXsegParam`.
- [ ] Pass persisted circle profile values from the config ViewModel to `AcsLciSegmentCircleParam`.
- [ ] Pass explicit profiles from synchronized-trigger callers.
- [ ] Build the ACS project and ACS test project.
- [ ] Run the ACS console tests and confirm the new tests pass.

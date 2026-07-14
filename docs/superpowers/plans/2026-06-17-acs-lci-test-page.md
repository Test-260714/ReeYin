# ACS LCI Test Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a dedicated AcsControlCardTestView tab that manually invokes `TryRunLciFixedDistancePulseXseg`.

**Architecture:** Extend the existing ACS test view model with editable LCI parameter properties, a command that creates `AcsLciFixedDistancePulseXsegParam`, parses point rows, invokes the ACS card, and surfaces the result/message/script. Add a dedicated XAML tab with simple text inputs and one-way result display, following existing WPF/Prism binding patterns.

**Tech Stack:** C# net8.0-windows, WPF XAML, Prism `DelegateCommand`, existing console-style ACS tests.

---

### Task 1: Test Surface

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] Add a test that `AcsControlCardTestViewModel` exposes editable LCI defaults and `RunLciFixedDistancePulseXsegCommand`.
- [ ] Add a test that `AcsControlCardTestView.xaml` contains a dedicated LCI tab and binds all parameter/result fields.
- [ ] Run `dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`; expected result: FAIL because properties/command/bindings do not exist yet.

### Task 2: ViewModel Implementation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardTestViewModel.cs`

- [ ] Add private backing fields for buffer, axes, pulse width, interval, start/end distance, route output, config output index/code, timeout, points text, status, result script.
- [ ] Add public bindable properties with existing `RaisePropertyChanged()` pattern.
- [ ] Add `RunLciFixedDistancePulseXsegCommand = new DelegateCommand(RunLciFixedDistancePulseXseg)`.
- [ ] Implement point parsing from newline rows in `X,Y` form and return validation errors through `SetInterpolationStatus`.
- [ ] Implement `RunLciFixedDistancePulseXseg()` to call `card.TryRunLciFixedDistancePulseXseg(param, out result, out message)` and update `LciFixedDistancePulseStatusText` and `LciFixedDistancePulseScript`.

### Task 3: XAML Tab

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardTestView.xaml`

- [ ] Insert a new `TabItem Header="LCI脉冲"` before the monitor tab.
- [ ] Bind text inputs to the LCI properties with `Mode=TwoWay` and `UpdateSourceTrigger=PropertyChanged`.
- [ ] Bind execution button to `RunLciFixedDistancePulseXsegCommand`.
- [ ] Bind status/script outputs as one-way read-only text boxes.
- [ ] Update `MonitorTabIndex` from 6 to 7.

### Task 4: Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj`

- [ ] Run ACS tests and confirm all pass.
- [ ] Run `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj` and confirm build succeeds.

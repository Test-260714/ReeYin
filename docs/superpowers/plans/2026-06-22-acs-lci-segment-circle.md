# ACS LCI Segment Circle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ACS LCI segmented-gate full-circle helper and a configuration-page test surface.

**Architecture:** Keep the feature in the existing ACS-specific LCI script builder/runner file. The ViewModel gathers UI inputs into a parameter object and reuses the same buffer load/compile/run/wait pattern as the fixed-distance pulse runner.

**Tech Stack:** C#/.NET 8 WPF, Prism `DelegateCommand`, ACSPL+ generated scripts, existing console-style ACS tests.

---

### Task 1: Circle Script Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] Add a test named `TestAcsLciSegmentCircleScriptMatchesReferenceXsegFlow` that builds `AcsLciSegmentCircleParam` with axes `2,0`, center `(10,5)`, radius `5`, and asserts `LINE/p (AxX, AxY), XCircleStartPos, YCircleStartPos, 0` appears before `ARC2/p (AxX, AxY), XCenterPos, YCenterPos, 2*pi, GateActiveState`.
- [ ] Add a test named `TestAcsLciSegmentCircleRunnerUsesBufferPipeline` that reads `AcsLciFixedDistancePulse.cs` and asserts the runner calls build, load, compile, run, wait, and cleanup methods.
- [ ] Run `dotnet run --project .\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore -p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\` and verify the new tests fail because the types/methods do not exist.

### Task 2: Circle Script Implementation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs`

- [ ] Add `AcsLciSegmentCircleParam` and `AcsLciSegmentCircleResult`.
- [ ] Add `TryRunLciSegmentCircle` following the existing buffer pipeline.
- [ ] Add `BuildLciSegmentCircleScript`, validation, and helper logic for the circle start point.
- [ ] Run the test project and verify the circle script and runner tests pass or reveal only UI-surface failures.

### Task 3: Configuration UI Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] Add a ViewModel-surface test for circle properties and `RunLciSegmentCircleCommand`.
- [ ] Add a XAML-surface test for the circle input labels and command binding.
- [ ] Run the test project and verify the UI tests fail before implementation.

### Task 4: Configuration UI Implementation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml`

- [ ] Add backing fields, public bindable properties, and `RunLciSegmentCircleCommand`.
- [ ] Implement `RunLciSegmentCircle` to build `AcsLciSegmentCircleParam`, call `TryRunLciSegmentCircle`, and update the existing LCI status/script output fields.
- [ ] Add a `LCI分段画圆测试` section to the existing LCI tab using existing shared button/input style patterns.
- [ ] Run the test project and verify all added tests pass.

### Task 5: Final Verification

**Files:**
- No source edits expected.

- [ ] Run `dotnet build .\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj --no-restore -p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\`.
- [ ] Run the ACS test project with the same `SolutionDir` property.
- [ ] Report exact verification results and any existing build warnings or blockers.

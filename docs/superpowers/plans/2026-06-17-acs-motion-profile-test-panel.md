# ACS Motion Profile Test Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-axis runtime motion profile editing to the ACS test panel for Velocity, Acceleration, Deceleration, Kill Deceleration, and Jerk.

**Architecture:** Keep this as test/diagnostic functionality. `AcsControlCard` exposes safe read/write wrappers over the ACS API, `AcsControlCardTestViewModel` manages an editable collection of per-axis profiles, and `AcsControlCardTestView` shows the collection in the existing single-axis motion tab.

**Tech Stack:** .NET 8 WPF, Prism `DelegateCommand`, ACS.SPiiPlusNET8 API, ReeYin_V shared XAML styles.

---

### Task 1: Failing Structure Verification

**Files:**
- Create: `.codex_tmp/verify_acs_motion_profile.py`

- [ ] Write a script that checks for `AcsAxisMotionProfile`, `TryGetAxisMotionProfile`, `TrySetAxisMotionProfile`, `AxisMotionProfiles`, `RefreshMotionProfilesCommand`, `ApplySelectedMotionProfileCommand`, `ApplyAllMotionProfilesCommand`, and the XAML DataGrid columns for the five parameters.
- [ ] Run `python .codex_tmp/verify_acs_motion_profile.py`; expected result is failure before implementation.

### Task 2: ACS API Wrappers

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsTestModels.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/TestDiagnostics.cs`

- [ ] Add `AcsAxisMotionProfile` with `Axis`, `Velocity`, `Acceleration`, `Deceleration`, `KillDeceleration`, `Jerk`, and `Message`.
- [ ] Add `TryGetAxisMotionProfile(En_AxisNum axisId, out AcsAxisMotionProfile profile, out string message)`.
- [ ] Add `TrySetAxisMotionProfile(AcsAxisMotionProfile profile, out string message)`.

### Task 3: Test ViewModel

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardTestViewModel.cs`

- [ ] Add `ObservableCollection<AcsAxisMotionProfile> AxisMotionProfiles` and `SelectedMotionProfile`.
- [ ] Add refresh/apply commands and call profile refresh after card binding.
- [ ] Implement selected/all apply methods with finite and non-negative validation.

### Task 4: XAML UI

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardTestView.xaml`

- [ ] Expand the single-axis motion tab right-side panel to include a motion profile DataGrid.
- [ ] Bind columns to `Velocity`, `Acceleration`, `Deceleration`, `KillDeceleration`, and `Jerk`.
- [ ] Add command buttons for refresh, apply selected axis, and apply all axes.

### Task 5: Verification

**Files:**
- No production files.

- [ ] Run `python .codex_tmp/verify_acs_motion_profile.py`; expect pass.
- [ ] Run `dotnet build Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj --no-restore -p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\`; expect exit code 0.

# ACS Control Card Page Merge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Merge ACS status monitoring into the ACS test window so both configuration-page actions open one combined debug panel.

**Architecture:** Keep `AcsControlCardMonitorPanelViewModel` as the owner of monitor refresh state and expose it from `AcsControlCardTestViewModel` as `Monitor`. Add a monitor tab to `AcsControlCardTestView`, bind monitor controls through `Monitor.*`, and make the config view model open `AcsControlCardTestView` with an optional initial tab.

**Tech Stack:** .NET 8 WPF, Prism `DelegateCommand`, ReeYin_V shared XAML styles.

---

### Task 1: Add Failing Structure Test

**Files:**
- Create temporary: `.codex_tmp/verify_acs_merge.py`

- [ ] Write a script that asserts `AcsControlCardTestView.xaml` contains a monitor tab, `AcsControlCardTestViewModel.cs` exposes `Monitor`, and `AcsControlCardConfigViewModel.cs` opens `AcsControlCardTestView` for the monitor entry.
- [ ] Run `python .codex_tmp/verify_acs_merge.py` and confirm it fails before production changes.

### Task 2: Merge Monitor UI Into Test Window

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardTestView.xaml`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardTestView.xaml.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardTestViewModel.cs`

- [ ] Add `SelectedIndex="{Binding SelectedTabIndex, Mode=TwoWay}"` to the existing `TabControl`.
- [ ] Add a “状态监控” `TabItem` that binds all monitor controls to `Monitor.*`.
- [ ] Add `Monitor`, `SelectedTabIndex`, and `MonitorTabIndex` to `AcsControlCardTestViewModel`.
- [ ] Initialize and dispose the monitor ViewModel from `AcsControlCardTestViewModel`.
- [ ] Add an optional `initialTabIndex` constructor argument to `AcsControlCardTestView`.

### Task 3: Unify Config Entrypoints

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`

- [ ] Change `OpenMonitorPage` to open `AcsControlCardTestView(card, AcsControlCardTestViewModel.MonitorTabIndex)`.
- [ ] Keep `OpenTestPage` opening `AcsControlCardTestView(card)`.
- [ ] Reuse a private helper to set the owner and show the window.

### Task 4: Verify

**Files:**
- No production files.

- [ ] Run `python .codex_tmp/verify_acs_merge.py`; expect pass.
- [ ] Run `python .codex_tmp/verify_acs_monitor_page_removed.py`; expect pass.
- [ ] Run `dotnet build Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj --no-restore`; expect exit code 0.

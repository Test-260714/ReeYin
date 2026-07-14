# Alarm Notification Modes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make alarm definitions control whether new/repeated alarms show no popup, a HandyControl Growl, or a blocking manual-confirm modal.

**Architecture:** Persist popup policy on `AlarmDefinitionInfo`, carry it through raise requests, active alarm snapshots, and realtime entries, then let `AlarmWorkbenchShellViewModel` render UI notifications from `IAlarmService.DataChanged`. Fatal/critical or acknowledgement-required alarms are escalated to modal confirmation even if the stored popup mode is lower.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, SqlSugar CodeFirst, HandyControl Growl/MessageBox.

---

### Task 1: Define Popup Policy Data

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmEnums.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmRaiseRequest.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmInfo.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmDashboardModels.cs`

- [ ] Add `AlarmPopupMode` with `None`, `Growl`, and `Modal`.
- [ ] Add `PopupMode` and `PopupThrottleSeconds` to raise request, runtime alarm, active records, and realtime entries.
- [ ] Keep defaults safe: warnings/errors use Growl, fatal or acknowledgement-required uses Modal, info uses None.

### Task 2: Persist And Resolve Definition Policy

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionEntity.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionInfo.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionResolver.cs`

- [ ] Persist popup mode and throttle seconds in alarm definitions.
- [ ] Seed software definitions with severity-based defaults.
- [ ] Resolve hardware/software requests from definition policy.

### Task 3: Surface Policy In Alarm Definition UI

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Models/AlarmDefinitionManagementModels.cs`
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmDefinitionsViewModel.cs`
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmDefinitionsView.xaml`

- [ ] Add selectable popup mode options.
- [ ] Save and load popup mode/throttle values through definition editor.
- [ ] Keep the UI simple: notification policy lives beside level and confirmation.

### Task 4: Notify From Realtime Alarm Changes

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`

- [ ] On `Raised` and `Repeated`, decide the effective notification mode.
- [ ] Use HandyControl `Growl` for warning/error non-blocking notifications.
- [ ] Use HandyControl `MessageBox` for modal alarms and confirm the alarm only when the operator clicks OK.
- [ ] Throttle notifications by source, location, code, and popup mode.

### Task 5: Verify

**Files:**
- Create: `.codex_tmp/alarm_notification_static_tests.py`

- [ ] Run the static test once before implementation and confirm it fails on missing fields.
- [ ] Run the static test after implementation and confirm it passes.
- [ ] Build `Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj` with `--no-restore`.

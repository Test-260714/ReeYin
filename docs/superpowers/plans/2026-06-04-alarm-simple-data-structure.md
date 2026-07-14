# Alarm Simple Data Structure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a smaller alarm domain model and route AlarmCenter configuration through one config service while keeping existing persistence compatible.

**Architecture:** Add simplified domain models in `Core/ReeYin-V.Core/Services/Alarm/Models`, add `IAlarmConfigService` as a facade over existing definition and hardware-rule services, and add `IAlarmService.Report(AlarmSignal)` as the common runtime entry. The AlarmCenter definition popup keeps two tabs only: alarm definitions and custom trigger rules.

**Tech Stack:** C# WPF, Prism, SqlSugar, ReeYin_V.Core alarm services, ReeYin.AlarmCenter MVVM.

---

### Task 1: Static Regression Test

**Files:**
- Create: `.codex_tmp/alarm_simple_structure_static_tests.py`

- [ ] **Step 1: Create a static test that expresses the simplified design**

The test checks for new models, unified config service, no direct governance service dependency in `AlarmDefinitionsViewModel`, and removal of advanced governance sections from `AlarmDefinitionsView.xaml`.

- [ ] **Step 2: Run the test and confirm it fails before implementation**

Run: `python .codex_tmp\alarm_simple_structure_static_tests.py`

Expected: failure because `AlarmSignal`, `AlarmTriggerRule`, `IAlarmConfigService`, and simplified UI calls do not exist yet.

### Task 2: Add Simplified Core Models

**Files:**
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmSourceKind.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmDefinition.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmSignal.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmTriggerRule.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmRecord.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmConfigSnapshot.cs`

- [ ] **Step 1: Add source/trigger enums and simple DTOs**

The models should contain only the fields needed by runtime reporting and configuration editing. Avoid copying legacy governance fields.

- [ ] **Step 2: Run the static test**

Expected: model checks pass; service and UI checks still fail.

### Task 3: Add Alarm Config Facade

**Files:**
- Create: `Core/ReeYin-V.Core/Services/Alarm/Config/IAlarmConfigService.cs`
- Create: `Core/ReeYin-V.Core/Services/Alarm/Config/AlarmConfigService.cs`
- Modify: `Core/ReeYin-V.Core/IOC/PrismProvider.cs`

- [ ] **Step 1: Add facade service**

`IAlarmConfigService` exposes `LoadAsync`, `SaveDefinitionAsync`, `SetDefinitionEnabledAsync`, `SaveTriggerRuleAsync`, and `SetTriggerRuleEnabledAsync`.

- [ ] **Step 2: Add mapping between simplified and legacy models**

`AlarmConfigService` maps `AlarmDefinitionInfo` to `AlarmDefinition` and `HardwareAlarmRuleInfo` to `AlarmTriggerRule`, preserving existing tables.

- [ ] **Step 3: Expose service through `PrismProvider`**

Add a static `AlarmConfigService` property so existing AlarmCenter ViewModels can resolve it without direct container calls.

### Task 4: Add Runtime Signal Entry

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/IAlarmService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`

- [ ] **Step 1: Add `Report(AlarmSignal signal)`**

The method converts `AlarmSignal` to the existing definition resolution flow and then calls `AddAlarm`.

- [ ] **Step 2: Keep `AddAlarm(AlarmRaiseRequest)`**

Existing callers keep working while new callers can use the simpler model.

### Task 5: Simplify AlarmCenter Definition Popup Calls

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Models/AlarmDefinitionManagementModels.cs`
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmDefinitionsViewModel.cs`
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmDefinitionsView.xaml`

- [ ] **Step 1: Add simplified model conversions**

Add `FromModel`/`ToModel` methods for `AlarmDefinitionItem` and `AlarmHardwareRuleItem`.

- [ ] **Step 2: Rewrite `AlarmDefinitionsViewModel` to depend on `IAlarmConfigService`**

Keep the same public property names needed by existing definition/rule XAML, but remove governance service fields, collections, and commands.

- [ ] **Step 3: Remove governance tabs and editor overlays from XAML**

Keep only the definition tab, custom trigger rule tab, definition editor overlay, and trigger rule editor overlay.

### Task 6: Verify

**Files:**
- No source modifications expected.

- [ ] **Step 1: Run static regression**

Run: `python .codex_tmp\alarm_simple_structure_static_tests.py`

Expected: pass.

- [ ] **Step 2: Build AlarmCenter**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false`

Expected: exit code 0. Existing nullable and `halcondotnet` warnings can remain.


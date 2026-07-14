# Fatal Critical Fullscreen Alarm Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show Fatal/Critical alarms as a full-screen overlay, using red default style and black-yellow safety style for hardware safety alarms.

**Architecture:** Extend the existing global `AlarmNotificationService` so Fatal/Critical alarms become overlay state instead of HandyControl modal dialogs. Bind `MainView` to that service through `MainViewModel`, and add one top-level overlay in `MainView.xaml` that switches visual style from service properties.

**Tech Stack:** WPF, Prism `BindableBase`/`DelegateCommand`, Prism EventAggregator, HandyControl Growl for non-critical alarms, existing ReeYin-V `ExposedService` auto-initialization.

---

### Task 1: Verification First

**Files:**
- Check: `Application/ReeYin_V.Main/Services/AlarmNotificationService.cs`
- Check: `Application/ReeYin_V.Main/ViewModels/MainViewModel.cs`
- Check: `Application/ReeYin_V.Main/Views/MainView.xaml`

- [ ] Run a structural test that fails before the overlay feature exists.
- [ ] Expected missing markers before implementation: `IsCriticalOverlayVisible`, `AlarmNotifications`, `CriticalAlarmOverlay`.

### Task 2: Notification Service State

**Files:**
- Modify: `Application/ReeYin_V.Main/Services/AlarmNotificationService.cs`

- [ ] Make the service inherit `BindableBase`.
- [ ] Add overlay properties: visibility, style key, title, message, code, location, source, occurrence count, confirm command.
- [ ] Add a queue for multiple Fatal/Critical entries.
- [ ] Add hardware safety detection using code and keyword matching.

### Task 3: MainViewModel Binding

**Files:**
- Modify: `Application/ReeYin_V.Main/ViewModels/MainViewModel.cs`

- [ ] Expose `AlarmNotificationService AlarmNotifications` for XAML binding.

### Task 4: MainView Overlay UI

**Files:**
- Modify: `Application/ReeYin_V.Main/Views/MainView.xaml`

- [ ] Add `BooleanToVisibilityConverter` resource.
- [ ] Add full-screen overlay above all content and Growl host.
- [ ] Style A uses red full-screen compression layer.
- [ ] Style B uses black-yellow safety stripes.
- [ ] Bind the confirm button to `ConfirmCriticalAlarmCommand`.

### Task 5: Verify Build

**Commands:**
- `dotnet build Application\ReeYin_V.Main\ReeYin_V.Main.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"`
- `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"`

**Expected:** both builds complete with 0 errors.

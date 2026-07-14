# Global Alarm Notification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show alarm notifications on any active page instead of only when Alarm Center is open.

**Architecture:** Add an auto-initialized singleton notification service in `ReeYin_V.Main` that subscribes to the existing `AlarmRealtimeEvent`. Add a root Growl host to `MainView.xaml`, and remove the page-scoped popup call from `AlarmWorkbenchShellViewModel` so Alarm Center keeps list/dashboard refresh only.

**Tech Stack:** WPF, Prism EventAggregator, HandyControl Growl/MessageBox, ReeYin-V ExposedService auto-initialization.

---

### Task 1: Add Global Notification Service

**Files:**
- Create: `Application/ReeYin_V.Main/Services/AlarmNotificationService.cs`

- [ ] Subscribe to `AlarmRealtimeEvent` in an auto-initialized singleton.
- [ ] Reuse current popup rules: raised/repeated only; fatal or need-ack => modal; warning/error => growl; info => growl only when configured.
- [ ] Dispatch UI work through `Application.Current.Dispatcher`.
- [ ] Confirm need-ack alarms through `IAlarmService.ConfirmAlarm` after modal OK.

### Task 2: Add Root Growl Host

**Files:**
- Modify: `Application/ReeYin_V.Main/Views/MainView.xaml`

- [ ] Add namespace for `ReeYin_V.Main.Services`.
- [ ] Add a top-right `StackPanel` with `hc:Growl.Token` bound to `AlarmNotificationService.GlobalGrowlToken`.
- [ ] Keep it in the root grid with high `Panel.ZIndex` so it overlays all PrimaryRegion pages.

### Task 3: Remove Page-Scoped Duplicate Popup

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`

- [ ] Remove `HandleRealtimeNotification(e.LatestEvent)` from `OnAlarmDataChanged`.
- [ ] Keep dashboard, active alarm list, realtime feed, and deferred refresh behavior unchanged.

### Task 4: Verify

**Commands:**
- `dotnet build Application\ReeYin_V.Main\ReeYin_V.Main.csproj`
- `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj`

**Expected:** both builds complete without errors.

# Alarm Realtime Tabs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Put current active alarms and the realtime event feed under one TabControl so the main realtime page is easier to understand.

**Architecture:** Keep the existing AlarmWorkbenchShellViewModel bindings unchanged. Only rearrange AlarmRealtimeView.xaml: the active alarm DataGrid and details stay in the first tab, while the realtime feed moves to the second tab with its pause/resume controls.

**Tech Stack:** WPF XAML, Prism MVVM, ReeYin.AlarmCenter shared alarm styles.

---

### Task 1: Rearrange Realtime Alarm Page

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmRealtimeView.xaml`

- [ ] **Step 1: Remove duplicate global feed controls from command bar**

Keep the page title and description, but remove the top-right `FeedStateText` and `ToggleRealtimeCommand` controls so pause/resume is scoped to the event stream tab.

- [ ] **Step 2: Add a two-tab layout**

Replace the old two-column content grid with a `TabControl` containing:
- `TabItem Header="当前活动报警"`: existing active alarm DataGrid plus existing alarm detail card.
- `TabItem Header="实时事件流"`: existing realtime feed card and pause/resume controls.

- [ ] **Step 3: Preserve existing bindings**

Do not rename or change these bindings: `ActiveAlarms`, `SelectedActiveAlarm`, `RealtimeFeed`, `FeedStateText`, `ToggleRealtimeCommand`, `RealtimeToggleText`, `ConfirmSelectedCommand`, `ClearSelectedCommand`.

- [ ] **Step 4: Build AlarmCenter**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false`
Expected: 0 errors. Existing warnings may remain.

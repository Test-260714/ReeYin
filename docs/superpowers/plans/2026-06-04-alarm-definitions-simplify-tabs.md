# Alarm Definitions Simplify Tabs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Simplify the alarm definition page so the main configuration matches software alarms and customizable hardware alarms.

**Architecture:** Keep `AlarmDefinitionsViewModel` and all existing services/bindings unchanged. Reorganize only `AlarmDefinitionsView.xaml`: promote alarm definitions to `软件报警`, promote hardware rules to `硬件报警`, and move suppression/shelve/notification/audit into a collapsed advanced settings area.

**Tech Stack:** WPF XAML, Prism MVVM, ReeYin.AlarmCenter shared styles.

---

### Task 1: Reorganize AlarmDefinitionsView Tabs

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmDefinitionsView.xaml`

- [ ] **Step 1: Update page copy**

Change the command-bar title and description to focus on software alarms and hardware alarm rules.

- [ ] **Step 2: Rebuild the main content shell**

Wrap the existing top-level `TabControl` in a two-row grid. Row 0 contains the main two-tab `TabControl`; row 1 contains a collapsed `Expander` for advanced settings.

- [ ] **Step 3: Promote core tabs**

Rename the existing alarm definition tab to `软件报警`; keep all definition DataGrid/edit bindings. Move the existing hardware rules tab next to it and rename it to `硬件报警`.

- [ ] **Step 4: Move advanced tabs**

Move existing suppression, shelve, notification route, and event audit tabs into the advanced `Expander` as a nested `TabControl`; keep all bindings unchanged.

- [ ] **Step 5: Build AlarmCenter**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false`
Expected: 0 errors. Existing warnings may remain.

# ACS Config View Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the ACS control card configuration window so connection, axis control, Buffer script editing, and LCI testing are separated clearly and all visible Chinese text is readable.

**Architecture:** Keep the existing WPF window and shared `AcsControlCardConfigViewModel` as the main state owner. Add one focused Buffer script dialog that shares the main ViewModel DataContext, and add one small axis context model to group selected-axis config and home-buffer data.

**Tech Stack:** .NET 8 WPF, Prism `DelegateCommand`, existing `ReeYin_V.UI` resources, source-level ACS tests in `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs`.

---

### Task 1: Update Source-Level Tests

**Files:**
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs`

- [ ] Add tests that assert the main ACS config tab list no longer contains `ACSPL命令`, `TransactionCommandText`, `TransactionResponse`, or a `Buffer脚本` tab.
- [ ] Add tests that assert `轴控制` contains bindings from both the previous axis/home tab and the previous single-axis tab.
- [ ] Add tests that assert `AcsBufferScriptView.xaml` exists and contains Buffer script commands and editor bindings.
- [ ] Add tests that assert LCI headers and labels are readable Chinese and do not contain `??` placeholders.
- [ ] Run the ACS test project and verify the new tests fail before implementation.

### Task 2: Introduce Axis Context Data Structure

**Files:**
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardConfigViewModel.cs`

- [ ] Add a small `AcsAxisControlContext` class with `Axis`, `AxisConfig`, and `HomeBuffer` properties.
- [ ] Add `SelectedAxisContext` to the ViewModel.
- [ ] Update `SyncSelectedAxisContext()` so it refreshes `SelectedAxisContext` together with existing compatibility properties.

### Task 3: Separate Buffer Script Dialog

**Files:**
- Create: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml`
- Create: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml.cs`
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardConfigViewModel.cs`
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml`

- [ ] Add `OpenBufferScriptCommand` to the main ViewModel.
- [ ] Implement `OpenBufferScript()` to show `AcsBufferScriptView` with the same ViewModel as `DataContext`.
- [ ] Move the full Buffer editor UI into the new dialog.
- [ ] Add a main-page button `打开Buffer脚本` that invokes `OpenBufferScriptCommand`.

### Task 4: Refactor Main Tabs And LCI Text

**Files:**
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml`
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardConfigViewModel.cs`

- [ ] Rename `连接/命令` to `连接` and remove ACSPL command controls.
- [ ] Merge `轴参数/回零` and `单轴运动` into one `轴控制` tab.
- [ ] Remove the main `Buffer脚本` tab.
- [ ] Fix the LCI tab headers and labels to readable Chinese.
- [ ] Reduce `MaxSelectedTabIndex` to match the new main tab count.
- [ ] Remove transaction command properties and methods that are no longer used.

### Task 5: Verify

**Files:**
- Build: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj`
- Test: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs`

- [ ] Run the updated ACS tests and note any unrelated existing failures.
- [ ] Run `dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-restore -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal`.
- [ ] Summarize changed files and verification results.

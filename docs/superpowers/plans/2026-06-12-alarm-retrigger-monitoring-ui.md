# Alarm Retrigger Monitoring UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把实时报警筛选从 ComboBox 改为直观的分段切换，并让已确认报警在同一源头重复触发时自动回到待处理，而不是要求操作员手动清除。

**Architecture:** Core 报警生命周期仍保留 active/history 边界：Clear 后才进入历史。重复触发同一 active 报警时，如果该报警需要确认，则重置 `IsConfirmed/ConfirmUser/ConfirmTime`，使它重新进入待处理并触发通知。AlarmCenter UI 用横向 ListBox 分段控件替代 ComboBox，并将“已确认未清除”改名为“监视中”。

**Tech Stack:** C#/.NET 8, WPF, Prism MVVM, existing `Scratch/AlarmCenterFunctionalTests` console functional tests.

---

## File Structure

- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`
  - Add regression test for acknowledged active alarm retriggering back to pending.
  - Add source-level UI test that `AlarmRealtimeView.xaml` no longer uses ComboBox and exposes a segmented ListBox with “监视中”.
- Modify: `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`
  - In repeated active alarm branch, reset acknowledgement when the new request requires acknowledgement.
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`
  - Rename visible acknowledged-active option/text to “监视中”.
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmRealtimeView.xaml`
  - Replace ComboBox with horizontal ListBox segmented selector.

### Task 1: Tests First

- [ ] Add test names in `Scratch/AlarmCenterFunctionalTests/Program.cs`:

```csharp
("AlarmService repeated active alarm resets acknowledgement", TestAlarmServiceRepeatedActiveAlarmResetsAcknowledgementAsync),
("AlarmRealtimeView uses segmented active view switch", TestAlarmRealtimeViewSegmentedSwitchAsync),
```

- [ ] Add `TestAlarmServiceRepeatedActiveAlarmResetsAcknowledgementAsync`:

```csharp
static async Task TestAlarmServiceRepeatedActiveAlarmResetsAcknowledgementAsync()
{
    string dbPath = Path.Combine(Path.GetTempPath(), $"reeyin_alarm_retrigger_{Guid.NewGuid():N}.db");
    ISqlSugarClient db = CreateAlarmTestDatabase(dbPath);

    try
    {
        var governance = new AlarmGovernanceService(db);
        var service = new AlarmService(db, new EventAggregator(), new NullAlarmDefinitionService(), governance);
        var request = new AlarmRaiseRequest
        {
            Code = "TDD.ALARM.RETRIGGER",
            Name = "Retrigger",
            Category = "Test",
            Message = "Initial raise",
            Level = AlarmSeverity.Warning,
            Source = "TDD",
            Location = "L1",
            NeedAcknowledge = true,
            PopupMode = AlarmPopupMode.Growl,
            AllowManualClear = true
        };

        AlarmInfo first = service.AddAlarm(request);
        Assert(service.ConfirmAlarm(first.Id, "tester"), "test setup should confirm the active alarm");

        AlarmActiveRecord confirmed = (await service.GetActiveAlarmsAsync(new AlarmActiveQuery()).ConfigureAwait(false)).Single();
        AssertEqual(true, confirmed.IsAcknowledged, "test setup should leave the alarm acknowledged");

        request.Message = "Same active alarm raised again";
        AlarmInfo repeated = service.AddAlarm(request);
        AssertEqual(first.Id, repeated.Id, "same code/source/location should reuse the active lifecycle");

        AlarmActiveRecord active = (await service.GetActiveAlarmsAsync(new AlarmActiveQuery()).ConfigureAwait(false)).Single();
        AssertEqual(false, active.IsAcknowledged, "retriggered acknowledgement-required active alarm should return to pending");
        AssertEqual(2, active.OccurrenceCount, "retrigger should increment occurrence count");

        IReadOnlyList<AlarmActiveRecord> pending = await service.GetActiveAlarmsAsync(new AlarmActiveQuery { OnlyUnacknowledged = true }).ConfigureAwait(false);
        AssertEqual(1, pending.Count, "retriggered alarm should be visible in pending active query");
    }
    finally
    {
        db.Close();
        try
        {
            File.Delete(dbPath);
        }
        catch
        {
        }
    }
}
```

- [ ] Add `TestAlarmRealtimeViewSegmentedSwitchAsync`:

```csharp
static Task TestAlarmRealtimeViewSegmentedSwitchAsync()
{
    string workspaceRoot = FindWorkspaceRoot();
    string viewPath = Path.Combine(workspaceRoot, "Application", "ReeYin.AlarmCenter", "Views", "AlarmRealtimeView.xaml");
    string viewModelPath = Path.Combine(workspaceRoot, "Application", "ReeYin.AlarmCenter", "ViewModels", "AlarmWorkbenchShellViewModel.cs");

    string viewSource = File.ReadAllText(viewPath);
    string viewModelSource = File.ReadAllText(viewModelPath);

    Assert(!viewSource.Contains("<ComboBox", StringComparison.Ordinal), "active alarm view switch should not use ComboBox");
    Assert(viewSource.Contains("<ListBox", StringComparison.Ordinal), "active alarm view switch should use a visible segmented ListBox");
    Assert(viewSource.Contains("SelectedValue=\"{Binding SelectedActiveAlarmView", StringComparison.Ordinal), "segmented selector should bind SelectedActiveAlarmView");
    Assert(viewModelSource.Contains("Label = \"监视中\"", StringComparison.Ordinal), "acknowledged active option should be named monitoring");
    Assert(!viewModelSource.Contains("已确认未清除", StringComparison.Ordinal), "user-facing acknowledged active text should not say confirmed but uncleared");
    return Task.CompletedTask;
}
```

- [ ] Run red verification:

```powershell
dotnet build Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -p:UseAppHost=false -v:minimal
dotnet Scratch\AlarmCenterFunctionalTests\bin\Debug\net8.0-windows\AlarmCenterFunctionalTests.dll
```

Expected: build succeeds; the two new tests fail before implementation.

### Task 2: Core Retrigger Behavior

- [ ] Modify repeated branch in `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`:

```csharp
activeAlarm.NeedAcknowledge = request.NeedAcknowledge;
if (request.NeedAcknowledge)
{
    activeAlarm.IsConfirmed = false;
    activeAlarm.ConfirmUser = string.Empty;
    activeAlarm.ConfirmTime = null;
}
else
{
    activeAlarm.IsConfirmed = true;
}
```

- [ ] Run the functional tests and confirm the retrigger test now passes.

### Task 3: Segmented UI And Monitoring Copy

- [ ] Modify `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`:

```csharp
new AlarmOptionItem { Label = "监视中", Value = ActiveAlarmViewAcknowledged },
```

Update dynamic copy:

```csharp
ActiveAlarmViewAcknowledged => "监视中",
ActiveAlarmViewAcknowledged => "这些报警已被确认当前提示，系统继续监视报警源；如果报警再次触发，会自动回到“待处理”。",
ActiveAlarmViewAcknowledged => $"监视中 {AcknowledgedActiveCount} / 活动 {AllActiveCount}",
```

- [ ] Modify `Application/ReeYin.AlarmCenter/Views/AlarmRealtimeView.xaml`:

Replace the active-view ComboBox with:

```xml
<ListBox Margin="0,6,0,0"
         MinWidth="244"
         BorderThickness="0"
         DisplayMemberPath="Label"
         ItemsSource="{Binding ActiveAlarmViewOptions}"
         SelectedValue="{Binding SelectedActiveAlarmView, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
         SelectedValuePath="Value">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

Add an item style so selected items look like buttons.

- [ ] Run build and tests.

### Task 4: Final Verification

- [ ] Run:

```powershell
dotnet Scratch\AlarmCenterFunctionalTests\bin\Debug\net8.0-windows\AlarmCenterFunctionalTests.dll
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected:
- Functional tests: all pass.
- AlarmCenter build: exit code 0; existing `halcondotnet` warning may remain.

# Active Alarm View Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让报警中心“实时报警”默认只显示待处理报警，并把已确认但未清除的活动报警切到独立视图，避免用户误以为“确认报警”没有生效。

**Architecture:** 不改 Core 的活动报警生命周期：确认只改变确认状态，清除才进入历史。AlarmCenter ViewModel 缓存服务返回的全部活动报警，并按“待处理 / 已确认未清除 / 全部活动”在本地过滤，XAML 提供切换控件和动态说明。

**Tech Stack:** WPF, Prism `BindableBase`/`DelegateCommand`, `ObservableCollection`, `AlarmActiveRecord`, existing Scratch functional console tests.

---

## File Structure

- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`
  - 增加一个功能测试，证明默认活动列表不再包含已确认报警，切到“已确认未清除”和“全部活动”后列表内容正确。
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`
  - 增加活动报警视图状态、计数、说明文本和本地过滤逻辑。
  - `ApplyActiveAlarms` 保存未过滤快照，再按当前视图刷新 `ActiveAlarms`。
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmRealtimeView.xaml`
  - 在当前活动报警表格上方增加视图切换下拉框。
  - 标题和描述绑定到 ViewModel，提示“已确认未清除”仍属于 active，不进入历史。

### Task 1: Add Failing Functional Test

**Files:**
- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] **Step 1: Add the failing test to the test list**

Add this entry after `AlarmWorkbenchShellViewModel time range and notification policy`:

```csharp
("AlarmWorkbench active alarm view separates acknowledged records", TestWorkbenchActiveAlarmViewFilteringAsync),
```

- [ ] **Step 2: Add the test body**

Add this method near the other `TestWorkbench...` methods:

```csharp
static Task TestWorkbenchActiveAlarmViewFilteringAsync()
{
    var viewModel = new AlarmWorkbenchShellViewModel(null!);
    AlarmActiveRecord[] records =
    {
        CreateActiveRecord("pending-1", needAcknowledge: true, isAcknowledged: false),
        CreateActiveRecord("ack-1", needAcknowledge: true, isAcknowledged: true),
        CreateActiveRecord("auto-1", needAcknowledge: false, isAcknowledged: true)
    };

    InvokePrivate<object?>(viewModel, "ApplyActiveAlarms", new object?[] { records });
    AssertEqual(1, viewModel.ActiveAlarms.Count, "default active alarm view should only show pending acknowledgement alarms");
    AssertEqual("pending-1", viewModel.ActiveAlarms.Single().ActiveId, "default active alarm view should keep the pending alarm selected");

    PropertyInfo selectedViewProperty = viewModel.GetType().GetProperty("SelectedActiveAlarmView")
        ?? throw new MissingMemberException(nameof(AlarmWorkbenchShellViewModel), "SelectedActiveAlarmView");

    selectedViewProperty.SetValue(viewModel, "Acknowledged");
    AssertEqual(2, viewModel.ActiveAlarms.Count, "acknowledged active view should show confirmed and no-ack active alarms");
    Assert(viewModel.ActiveAlarms.All(item => item.IsConfirmed), "acknowledged active view should not show pending alarms");

    selectedViewProperty.SetValue(viewModel, "All");
    AssertEqual(3, viewModel.ActiveAlarms.Count, "all active view should show the full active alarm cache");
    return Task.CompletedTask;
}
```

- [ ] **Step 3: Add the helper**

Add this helper before `InvokePrivateTask`:

```csharp
static AlarmActiveRecord CreateActiveRecord(string id, bool needAcknowledge, bool isAcknowledged)
{
    DateTime now = DateTime.Now;
    return new AlarmActiveRecord
    {
        ActiveId = id,
        Code = id.ToUpperInvariant(),
        Name = id,
        Category = "TDD",
        SourceName = "FunctionalTest",
        Location = "L1",
        Severity = AlarmSeverity.Warning,
        RaisedAt = now.AddMinutes(-5),
        LastRaisedAt = now,
        NeedAcknowledge = needAcknowledge,
        IsAcknowledged = isAcknowledged,
        AllowManualClear = true,
        Message = id
    };
}
```

- [ ] **Step 4: Run red verification**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: FAIL, because the current default `ApplyActiveAlarms` shows all 3 active records instead of only the pending one.

### Task 2: Implement ViewModel Filtering

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`

- [ ] **Step 1: Add constants and fields**

Add constants near the existing `ContentRegionName` and fields near `_selectedActiveAlarm`:

```csharp
private const string ActiveAlarmViewPending = "Pending";
private const string ActiveAlarmViewAcknowledged = "Acknowledged";
private const string ActiveAlarmViewAll = "All";

private readonly List<AlarmActiveRecord> _latestActiveAlarmRecords = new List<AlarmActiveRecord>();
private string _selectedActiveAlarmView = ActiveAlarmViewPending;
private int _pendingActiveCount;
private int _acknowledgedActiveCount;
private int _allActiveCount;
```

- [ ] **Step 2: Add view options collection**

Initialize options in the constructor after `ActiveAlarms`:

```csharp
ActiveAlarmViewOptions = new ObservableCollection<AlarmOptionItem>
{
    new AlarmOptionItem { Label = "待处理", Value = ActiveAlarmViewPending },
    new AlarmOptionItem { Label = "已确认未清除", Value = ActiveAlarmViewAcknowledged },
    new AlarmOptionItem { Label = "全部活动", Value = ActiveAlarmViewAll }
};
```

Add the property beside the other collections:

```csharp
public ObservableCollection<AlarmOptionItem> ActiveAlarmViewOptions { get; }
```

- [ ] **Step 3: Add selected view and display properties**

Add these public properties near `SelectedActiveAlarm`:

```csharp
public string SelectedActiveAlarmView
{
    get => _selectedActiveAlarmView;
    set
    {
        string normalized = NormalizeActiveAlarmView(value);
        if (SetProperty(ref _selectedActiveAlarmView, normalized))
        {
            RefreshActiveAlarmView();
            RaiseActiveAlarmViewTextChanged();
        }
    }
}

public int PendingActiveCount
{
    get => _pendingActiveCount;
    private set
    {
        if (SetProperty(ref _pendingActiveCount, value))
        {
            RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
        }
    }
}

public int AcknowledgedActiveCount
{
    get => _acknowledgedActiveCount;
    private set
    {
        if (SetProperty(ref _acknowledgedActiveCount, value))
        {
            RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
        }
    }
}

public int AllActiveCount
{
    get => _allActiveCount;
    private set
    {
        if (SetProperty(ref _allActiveCount, value))
        {
            RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
        }
    }
}
```

Add dynamic text properties:

```csharp
public string ActiveAlarmListTitle => SelectedActiveAlarmView switch
{
    ActiveAlarmViewAcknowledged => "已确认未清除",
    ActiveAlarmViewAll => "全部活动报警",
    _ => "待处理报警"
};

public string ActiveAlarmListDescription => SelectedActiveAlarmView switch
{
    ActiveAlarmViewAcknowledged => "这些报警已经确认或免确认，但源头仍未清除，所以持续时长仍会增长；清除后才进入历史记录。",
    ActiveAlarmViewAll => "显示全部仍处于活动生命周期的报警，包含待处理、已确认未清除和免确认报警。",
    _ => "默认只显示需要操作员确认且尚未确认的报警；确认后会移到“已确认未清除”。"
};

public string ActiveAlarmViewCountText => SelectedActiveAlarmView switch
{
    ActiveAlarmViewAcknowledged => $"已确认未清除 {AcknowledgedActiveCount} / 活动 {AllActiveCount}",
    ActiveAlarmViewAll => $"全部活动 {AllActiveCount}",
    _ => $"待处理 {PendingActiveCount} / 活动 {AllActiveCount}"
};
```

- [ ] **Step 4: Replace `ApplyActiveAlarms` body**

Use the full active cache and filter locally:

```csharp
private void ApplyActiveAlarms(IEnumerable<AlarmActiveRecord> records)
{
    string? selectedId = SelectedActiveAlarm?.ActiveId;
    _latestActiveAlarmRecords.Clear();
    _latestActiveAlarmRecords.AddRange(records ?? Enumerable.Empty<AlarmActiveRecord>());
    RefreshActiveAlarmCounts();
    ApplyActiveAlarmView(selectedId);
}
```

- [ ] **Step 5: Add filtering helpers**

Add near `ApplyActiveAlarms`:

```csharp
private void RefreshActiveAlarmView()
{
    ApplyActiveAlarmView(SelectedActiveAlarm?.ActiveId);
}

private void ApplyActiveAlarmView(string? preferredActiveId)
{
    List<AlarmActiveItem> items = FilterActiveRecords(_latestActiveAlarmRecords, SelectedActiveAlarmView)
        .OrderByDescending(item => item.LastRaisedAt)
        .ThenByDescending(item => item.RaisedAt)
        .Select(AlarmActiveItem.FromCore)
        .ToList();

    ReplaceCollection(ActiveAlarms, items);
    SelectedActiveAlarm = items.FirstOrDefault(item => string.Equals(item.ActiveId, preferredActiveId, StringComparison.OrdinalIgnoreCase))
        ?? items.FirstOrDefault();
}

private void RefreshActiveAlarmCounts()
{
    PendingActiveCount = _latestActiveAlarmRecords.Count(IsPendingActiveAlarm);
    AcknowledgedActiveCount = _latestActiveAlarmRecords.Count(IsAcknowledgedActiveAlarm);
    AllActiveCount = _latestActiveAlarmRecords.Count;
    RaiseActiveAlarmViewTextChanged();
}

private static IEnumerable<AlarmActiveRecord> FilterActiveRecords(IEnumerable<AlarmActiveRecord> records, string selectedView)
{
    string normalized = NormalizeActiveAlarmView(selectedView);
    return normalized switch
    {
        ActiveAlarmViewAcknowledged => records.Where(IsAcknowledgedActiveAlarm),
        ActiveAlarmViewAll => records,
        _ => records.Where(IsPendingActiveAlarm)
    };
}

private static bool IsPendingActiveAlarm(AlarmActiveRecord record)
{
    return record.NeedAcknowledge && !record.IsAcknowledged;
}

private static bool IsAcknowledgedActiveAlarm(AlarmActiveRecord record)
{
    return record.IsAcknowledged;
}

private static string NormalizeActiveAlarmView(string? value)
{
    return string.Equals(value, ActiveAlarmViewAcknowledged, StringComparison.OrdinalIgnoreCase)
        ? ActiveAlarmViewAcknowledged
        : string.Equals(value, ActiveAlarmViewAll, StringComparison.OrdinalIgnoreCase)
            ? ActiveAlarmViewAll
            : ActiveAlarmViewPending;
}

private void RaiseActiveAlarmViewTextChanged()
{
    RaisePropertyChanged(nameof(ActiveAlarmListTitle));
    RaisePropertyChanged(nameof(ActiveAlarmListDescription));
    RaisePropertyChanged(nameof(ActiveAlarmViewCountText));
}
```

- [ ] **Step 6: Run green verification**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: PASS, including the new active view split test.

### Task 3: Add Realtime UI Switch

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmRealtimeView.xaml`

- [ ] **Step 1: Replace the active list header text bindings**

In the current active alarm card header:

```xml
<TextBlock Style="{StaticResource SectionTitleStyle}" Text="{Binding ActiveAlarmListTitle}" />
<TextBlock Margin="0,4,0,0"
           Style="{StaticResource MutedTextStyle}"
           Text="{Binding ActiveAlarmListDescription}" />
```

- [ ] **Step 2: Add the view selector**

Replace the right-side static “最多展示 200 条...” text with:

```xml
<StackPanel DockPanel.Dock="Right"
            MinWidth="180"
            VerticalAlignment="Center">
    <TextBlock HorizontalAlignment="Right"
               Style="{StaticResource MutedTextStyle}"
               Text="{Binding ActiveAlarmViewCountText}" />
    <ComboBox Margin="0,6,0,0"
              MinWidth="156"
              DisplayMemberPath="Label"
              ItemsSource="{Binding ActiveAlarmViewOptions}"
              SelectedValue="{Binding SelectedActiveAlarmView, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
              SelectedValuePath="Value" />
</StackPanel>
```

- [ ] **Step 3: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: build exits 0. Existing repository warnings may remain.

### Task 4: Final Verification

**Files:**
- Read-only verification across touched files.

- [ ] **Step 1: Run functional test**

Run:

```powershell
dotnet run --project Scratch\AlarmCenterFunctionalTests\AlarmCenterFunctionalTests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: `RESULT Passed=8; Failed=0`.

- [ ] **Step 2: Run project build**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: build exits 0. Report any pre-existing warnings separately from errors.

- [ ] **Step 3: Manual behavior checklist**

Verify by reading the implementation and, if the app is run manually:

```text
1. 默认“实时报警 / 当前活动报警”只列出 NeedAcknowledge=true 且 IsAcknowledged=false 的报警。
2. 点击“确认报警”后，报警从默认列表移出。
3. 切换到“已确认未清除”可以看到该报警，持续时长继续增长，因为报警尚未 Clear。
4. 切换到“全部活动”可以看到所有未清除报警。
5. 历史记录仍只显示已清除的闭环报警。
```

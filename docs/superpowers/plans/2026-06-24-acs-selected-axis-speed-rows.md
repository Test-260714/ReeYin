# ACS Selected Axis Speed Rows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the ACS config axis-control speed grid show only the selected axis' speed settings while preserving ACS-specific axis controls.

**Architecture:** Keep `SelectedAxis` as the selection source in `AcsControlCardConfigViewModel`. Add a selected-axis speed row collection that reuses `AcsAxisSpeedParameterRow`, refresh it from `SyncSelectedAxisContext()`, and bind the XAML speed grid to that collection. Keep existing ACS home Buffer, X/Y home, movement, and status controls intact.

**Tech Stack:** C# .NET 8 WPF, Prism `BindableBase`, existing ACS console test harness.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`: update tests from all-axis speed rows to selected-axis speed rows.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`: add `SelectedAxisSpeedRows` and refresh it when the selected axis changes.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml`: bind the speed table to `SelectedAxisSpeedRows` and keep only speed columns.

## Task 1: Selected-Axis Speed Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Update test registration names**

Change:

```csharp
Run("ACS config view model exposes unified axis speed rows", TestAcsConfigViewModelExposesUnifiedAxisSpeedRows);
Run("ACS config view uses unified axis speed table", TestAcsConfigViewUsesUnifiedAxisSpeedTable);
```

to:

```csharp
Run("ACS config view model exposes selected-axis speed rows", TestAcsConfigViewModelExposesSelectedAxisSpeedRows);
Run("ACS config view uses selected-axis speed table", TestAcsConfigViewUsesSelectedAxisSpeedTable);
```

- [ ] **Step 2: Replace the view model test**

Replace `TestAcsConfigViewModelExposesUnifiedAxisSpeedRows()` with a test that:

```csharp
var selectedRows = ((System.Collections.IEnumerable?)viewModelType!
    .GetProperty("SelectedAxisSpeedRows")?.GetValue(viewModel))?
    .Cast<object>()
    .ToList();

AssertEqual(2, selectedRows!.Count, "SelectedAxisSpeedRows should initially show only X speed settings");
viewModelType.GetProperty("SelectedAxis")?.SetValue(viewModel, En_AxisNum.Y);
selectedRows = ((System.Collections.IEnumerable?)viewModelType
    .GetProperty("SelectedAxisSpeedRows")?.GetValue(viewModel))?
    .Cast<object>()
    .ToList();
AssertEqual(1, selectedRows!.Count, "SelectedAxisSpeedRows should update after selecting Y");
```

and verify row edits write through to the selected axis' `SpeedDict1`.

- [ ] **Step 3: Replace the XAML test**

Replace `TestAcsConfigViewUsesUnifiedAxisSpeedTable()` with a test that checks:

```csharp
AssertContains(xaml, "ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", "axis control tab should bind selected-axis speed rows");
AssertFalse(xaml.Contains("ItemsSource=\"{Binding AxisSpeedRows}\"", StringComparison.Ordinal), "axis control tab should not bind all-axis speed rows");
AssertContains(xaml, "SelectedAxisHomeBufferNo", "axis tab should keep selected-axis home Buffer settings");
AssertContains(xaml, "Options.XyHomeBuffer", "axis tab should keep ACS X/Y combined home Buffer settings");
AssertContains(xaml, "StartPositiveContinuousMoveCommand", "axis tab should keep hold-to-move controls");
```

- [ ] **Step 4: Run red verification**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll
```

Expected: build succeeds, console test fails because `SelectedAxisSpeedRows` does not exist yet.

## Task 2: ViewModel Selected-Axis Speed Rows

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`

- [ ] **Step 1: Add collection initialization**

In the constructor, after `AxisSpeedRows = new ObservableCollection<AcsAxisSpeedParameterRow>();`, add:

```csharp
SelectedAxisSpeedRows = new ObservableCollection<AcsAxisSpeedParameterRow>();
```

- [ ] **Step 2: Add the public property**

After `AxisSpeedRows`, add:

```csharp
public ObservableCollection<AcsAxisSpeedParameterRow> SelectedAxisSpeedRows { get; }
```

- [ ] **Step 3: Add refresh helper**

Add:

```csharp
private void RefreshSelectedAxisSpeedRows()
{
    SelectedAxisSpeedRows.Clear();

    if (SelectedAxisConfig == null)
    {
        return;
    }

    EnsureSpeedSettings(SelectedAxisConfig);
    foreach (var speed in SelectedAxisConfig.SpeedDict1.Where(speed => speed != null))
    {
        SelectedAxisSpeedRows.Add(new AcsAxisSpeedParameterRow(SelectedAxisConfig, speed));
    }
}
```

- [ ] **Step 4: Refresh on selected axis changes**

At the end of `SyncSelectedAxisContext()`, after assigning `SelectedAxisContext`, call:

```csharp
RefreshSelectedAxisSpeedRows();
RaisePropertyChanged(nameof(SelectedAxisSpeedRows));
```

- [ ] **Step 5: Run green verification for view model**

Run the ACS tests again. Expected: selected-axis view model test passes; XAML test still fails until Task 3.

## Task 3: XAML Selected-Axis Speed Table

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml`

- [ ] **Step 1: Update the speed panel title and helper text**

Change the title/helper from all-axis wording to:

```xml
<TextBlock Text="轴速度参数（当前轴）" FontWeight="SemiBold" VerticalAlignment="Center"/>
<TextBlock DockPanel.Dock="Right" Text="选择上方轴后，仅编辑当前轴 SpeedDict1" Foreground="#777777" VerticalAlignment="Center"/>
```

- [ ] **Step 2: Bind to selected-axis rows**

Change:

```xml
ItemsSource="{Binding AxisSpeedRows}"
```

to:

```xml
ItemsSource="{Binding SelectedAxisSpeedRows}"
```

- [ ] **Step 3: Remove all-axis metadata columns**

Remove the columns with headers:

```xml
Header="轴"
Header="物理轴号"
Header="轴名称"
Header="启用"
```

Keep these speed columns:

```xml
Header="速度类型"
Header="描述"
Header="起始速度"
Header="最大速度"
Header="加减速"
```

- [ ] **Step 4: Run final verification**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll
```

Expected: builds exit 0 and console tests pass. Existing warnings may remain.

## Notes

- Git commands are unavailable in this workspace because `.git` is not a valid repository.
- Use `-m:1` for WPF/MSBuild verification to avoid intermittent generated-file access conflicts.

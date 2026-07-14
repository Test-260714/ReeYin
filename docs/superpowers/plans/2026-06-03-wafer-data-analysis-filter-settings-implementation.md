# Wafer Data Analysis Filter Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move data-source filter editing into a per-row dialog and make point-collection edge trimming configurable.

**Architecture:** Keep `DataAnalysisDataSourceOption` as the persisted source of truth for filtering. Add a small dialog/view-model for copy-on-confirm editing, and pass a new `PointCollectionTrimCountPerSide` model setting into `SensorPointDataProcessor` so aggregation uses a configured trim count instead of a hard-coded value.

**Tech Stack:** C# 12 / .NET 8 WPF, Prism dialogs, ReeYin-V MVVM (`DialogViewModelBase`, `ModelParamBase`), XAML shared styles.

---

### File Structure

- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorPointDataProcessor.cs`
  - Add `PointCollectionTrimCountPerSide` to processing options.
  - Normalize trim count.
  - Use the configured trim count in point aggregation.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorDataCollectionModel.cs`
  - Add persisted `PointCollectionTrimCountPerSide` property with default `2`.
  - Pass it to `SensorPointDataProcessor.ProcessCollectedData`.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/DataAnalysisSourceFilterSettingViewModel.cs`
  - Prism dialog view-model that edits a working copy and copies values back only on OK.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/DataAnalysisSourceFilterSettingView.xaml`
  - Dialog UI for one data-source filter configuration.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/DataAnalysisSourceFilterSettingView.xaml.cs`
  - Standard view code-behind.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/SensorDataCollectionView.xaml`
  - Remove inline filter columns.
  - Add per-row filter settings button.
  - Add numeric trim-count setting in the data-source area.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/SensorDataCollectionViewModel.cs`
  - Add command handling for opening the filter dialog.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`
  - Register the new dialog view.

---

### Task 1: Add Failing Processing Test Harness

**Files:**
- Create: `.codex_tmp/wafer_trim_tests/Program.cs`

- [ ] **Step 1: Write the failing test**

Create a temporary console test harness that compiles the processor inputs directly and calls `SensorPointDataProcessor.ProcessCollectedData` after production code exposes the trim option. The test should expect trim count `1` to average only samples `2,3,4` from `[1,2,3,4,5]`.

```csharp
using System;
using System.Collections.Generic;
using Custom.WaferFlatnessMeasure.Models;
using ReeYin_V.Core.Services.DataCollectRelated;

static MeasureData Sample(double value)
{
    return new MeasureData
    {
        Z = value,
        OriginalDatas = new Dictionary<string, Dictionary<string, object>>
        {
            ["Channel4"] = new Dictionary<string, object>
            {
                ["THICKNESS"] = value
            }
        }
    };
}

var sensorDatas = new List<MeasureData>
{
    Sample(1), Sample(2), Sample(3), Sample(4), Sample(5)
};

var preDatas = new List<PreprocessDatasetModel>
{
    new PreprocessDatasetModel { PosX = 10, PosY = 20 }
};

var result = SensorPointDataProcessor.ProcessCollectedData(new SensorPointDataProcessingOptions
{
    SensorDatas = sensorDatas,
    PreDatas = preDatas,
    PointCollectionTrimCountPerSide = 1
});

if (result.DataCollect.Count != 1)
{
    throw new Exception($"Expected one point, got {result.DataCollect.Count}.");
}

double z = result.DataCollect[0].Z;
if (Math.Abs(z - 3d) > 1e-9)
{
    throw new Exception($"Expected Z average 3 after trimming one sample per side, got {z}.");
}

Console.WriteLine("trim-count test passed");
```

- [ ] **Step 2: Run test to verify it fails**

Run a compile/test command against the current production code. Expected: FAIL because `SensorPointDataProcessingOptions.PointCollectionTrimCountPerSide` does not exist.

- [ ] **Step 3: Keep the harness temporary**

Do not include `.codex_tmp/wafer_trim_tests` in the final delivered changes.

---

### Task 2: Implement Configurable Trim Count

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorPointDataProcessor.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorDataCollectionModel.cs`

- [ ] **Step 1: Add option property**

Add this property to `SensorPointDataProcessingOptions`:

```csharp
public int PointCollectionTrimCountPerSide { get; set; } = 2;
```

- [ ] **Step 2: Pass option into aggregation**

Replace calls like:

```csharp
MeasureData aggregatedData = AggregateMeasureDataGroup(group);
```

with:

```csharp
MeasureData aggregatedData = AggregateMeasureDataGroup(group, options.PointCollectionTrimCountPerSide);
```

- [ ] **Step 3: Use normalized trim count**

Change the aggregation method shape to:

```csharp
private static MeasureData AggregateMeasureDataGroup(List<MeasureData>? measureDatas, int trimCountPerSide)
{
    measureDatas ??= new List<MeasureData>();
    if (measureDatas.Count == 0)
    {
        return new MeasureData();
    }

    int normalizedTrimCount = Math.Max(0, trimCountPerSide);
    var trimmedDatas = TrimEdgeSamples(measureDatas, normalizedTrimCount);
    var effectiveDatas = trimmedDatas.Count > 0 ? trimmedDatas : measureDatas;
    var representativeData = effectiveDatas[effectiveDatas.Count / 2];

    return new MeasureData
    {
        OriginalDatas = AverageOriginalDatas(effectiveDatas),
        AreaData = representativeData.AreaData,
        RTime = representativeData.RTime,
        Z = effectiveDatas.Average(data => data.Z),
        IsValid = effectiveDatas.Any(data => data.IsValid),
    };
}
```

- [ ] **Step 4: Add model property**

Add this property to `SensorDataCollectionModel`, near other data-analysis settings:

```csharp
private int _pointCollectionTrimCountPerSide = 2;
public int PointCollectionTrimCountPerSide
{
    get { return _pointCollectionTrimCountPerSide; }
    set
    {
        int normalizedValue = Math.Max(0, value);
        SetProperty(ref _pointCollectionTrimCountPerSide, normalizedValue);
    }
}
```

- [ ] **Step 5: Pass model property to processing options**

In `ProcessCollectedData`, add:

```csharp
PointCollectionTrimCountPerSide = PointCollectionTrimCountPerSide,
```

- [ ] **Step 6: Run test to verify it passes**

Run the temporary trim-count harness. Expected: PASS with `trim-count test passed`.

---

### Task 3: Add Filter Settings Dialog

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/DataAnalysisSourceFilterSettingViewModel.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/DataAnalysisSourceFilterSettingView.xaml`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/DataAnalysisSourceFilterSettingView.xaml.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`

- [ ] **Step 1: Create dialog view-model**

Create `DataAnalysisSourceFilterSettingViewModel` inheriting `DialogViewModelBase`. It should read a `DataSource` parameter, copy its fields into editable properties, and copy back only on OK.

- [ ] **Step 2: Create dialog XAML**

Use shared WPF styles and simple controls for name, OriginalData key, enable filter, raw filter, min, and max.

- [ ] **Step 3: Register dialog**

Add:

```csharp
containerRegistry.RegisterDialog<DataAnalysisSourceFilterSettingView>();
```

to `CustomWaferFlatnessMeasure.RegisterTypes`.

---

### Task 4: Wire SensorDataCollectionView UI

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/SensorDataCollectionView.xaml`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/SensorDataCollectionViewModel.cs`

- [ ] **Step 1: Remove inline filter columns**

Remove columns bound to:

```text
IsFilterEnabled
IsRawDataFilter
FilterMin
FilterMax
```

- [ ] **Step 2: Add button column**

Add a `DataGridTemplateColumn` with a `Button` command parameter of the current row:

```xml
<Button Content="设置"
        Command="{Binding DataContext.OpenDataAnalysisSourceFilterSettingCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}"
        Style="{StaticResource GeneralButtonStyle}"/>
```

- [ ] **Step 3: Add trim-count input**

Add a numeric input bound to:

```xml
Value="{Binding ModelParam.PointCollectionTrimCountPerSide,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"
```

- [ ] **Step 4: Add view-model command**

Add a command:

```csharp
public DelegateCommand<DataAnalysisDataSourceOption?> OpenDataAnalysisSourceFilterSettingCommand =>
    new DelegateCommand<DataAnalysisDataSourceOption?>(OpenDataAnalysisSourceFilterSetting);
```

Open the dialog with `PrismProvider.DialogService.ShowDialog(...)`.

---

### Task 5: Verify Build and Cleanup

**Files:**
- Delete: `.codex_tmp/wafer_trim_tests`

- [ ] **Step 1: Build touched project**

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj --no-restore
```

Expected: Build succeeds.

- [ ] **Step 2: Remove temporary harness**

Remove `.codex_tmp/wafer_trim_tests`.

- [ ] **Step 3: Review changed files**

Check changed files manually because this workspace is not a Git repository in the current directory.

---

## Self-Review

- Spec coverage: dialog-based filter editing is covered by Tasks 3 and 4; configurable point trim is covered by Tasks 1 and 2; build verification is covered by Task 5.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: `PointCollectionTrimCountPerSide` is used consistently on the model and processor options.

# Wafer Spectrum Data Collection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the existing point-spectrum node compatible while adding a separate line-spectrum data collection node.

**Architecture:** Existing `SensorDataCollectionModel` remains the point-spectrum node and moves into `Acquisition/PointSpectrum` without changing namespace or class name. A new `LineSpectrumDataCollectionModel` owns line-spectrum collection and exposes height/gray row output parameters through its own View and ViewModel.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, ReeYin-V `ModelParamBase`, `SensorBase`, `MeasureData`, `OutputParamCollector`.

---

## File Structure

- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/SensorDataCollectionModel.cs` -> `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/SensorDataCollectionModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/Storage/*` -> `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Storage/*`
- Modify imports that reference point temperature storage after the folder move.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/LineSpectrumDataCollectionViewModel.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

### Task 1: Add Failing Line-Spectrum Conversion Test

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`
- Create later: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs`

- [ ] **Step 1: Write the failing test**

Add this invocation near the existing `Run(...)` calls:

```csharp
Run("line spectrum converter scales height and skips invalid rows", TestLineSpectrumConverterScalesHeightAndSkipsInvalidRows);
```

Add this test method before `CreateCapturedTemperatureRecord(...)`:

```csharp
void TestLineSpectrumConverterScalesHeightAndSkipsInvalidRows()
{
    var rows = new List<ReeYin_V.Core.Services.DataCollectRelated.MeasureData>
    {
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>
            {
                new[] { 1f, 2f },
                new[] { 10f, 20f }
            }
        },
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>
            {
                new[] { 3f, 4f }
            }
        },
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>()
        }
    };

    var converted = LineSpectrumDataCollectionModel.ConvertToMeasureRows(rows, 10d);

    AssertEqual(2, converted.HeightRows.Count, "height row count");
    AssertEqual(1, converted.GrayRows.Count, "gray row count");
    AssertClose(10, converted.HeightRows[0][0], "height scale first value");
    AssertClose(20, converted.HeightRows[0][1], "height scale second value");
    AssertClose(30, converted.HeightRows[1][0], "height-only row first value");
    AssertClose(10, converted.GrayRows[0][0], "gray first value");
    AssertClose(20, converted.GrayRows[0][1], "gray second value");
}
```

- [ ] **Step 2: Run the test to verify RED**

Run: `dotnet run --project CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`

Expected: build fails because `LineSpectrumDataCollectionModel` does not exist.

### Task 2: Create Line-Spectrum Model

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs`

- [ ] **Step 1: Implement the model**

Create `LineSpectrumDataCollectionModel` in namespace `Custom.WaferFlatnessMeasure.Models`. Include:

```csharp
[Serializable]
public class LineSpectrumDataCollectionModel : ModelParamBase
{
    [JsonIgnore]
    public ObservableCollection<SensorBase> Models { get; set; } = new();

    public string SltModelName { get; set; } = string.Empty;
    public int SltTriggerPicIndex { get; set; } = 0;
    public string StartEventName { get; set; } = "TrrigerLineSpectrumStartCollect";
    public string StopEventName { get; set; } = "TrrigerLineSpectrumStopCollect";
    public double HeightScale { get; set; } = 10d;

    [JsonIgnore]
    public SensorBase? SltModel { get; set; }

    [OutputParam("LineSpectrumHeightRows", "线光谱高度数据")]
    [JsonIgnore]
    public List<float[]> HeightRows { get; set; } = new();

    [OutputParam("LineSpectrumGrayRows", "线光谱灰度数据")]
    [JsonIgnore]
    public List<float[]> GrayRows { get; set; } = new();

    [OutputParam("LineSpectrumHeightRowCount", "线光谱高度行数")]
    [JsonIgnore]
    public int HeightRowCount { get; set; }

    [OutputParam("LineSpectrumGrayRowCount", "线光谱灰度行数")]
    [JsonIgnore]
    public int GrayRowCount { get; set; }

    public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

    public static LineSpectrumMeasureRows ConvertToMeasureRows(IEnumerable<MeasureData>? measureDatas, double heightScale)
    {
        var heightRows = new List<float[]>();
        var grayRows = new List<float[]>();
        foreach (MeasureData data in measureDatas ?? Enumerable.Empty<MeasureData>())
        {
            if (data?.AreaData == null || data.AreaData.Count == 0 || data.AreaData[0] == null)
            {
                continue;
            }

            heightRows.Add(data.AreaData[0].Select(value => (float)(value * heightScale)).ToArray());
            if (data.AreaData.Count > 1 && data.AreaData[1] != null)
            {
                grayRows.Add(data.AreaData[1].ToArray());
            }
        }

        return new LineSpectrumMeasureRows(heightRows, grayRows);
    }
}

public sealed record LineSpectrumMeasureRows(List<float[]> HeightRows, List<float[]> GrayRows);
```

Add the lifecycle and execution methods around this skeleton: `LoadKeyParam`, `OnceInit`, `ExecuteModule`, `TrrigerStartCollect`, `TrrigerStopCollect`, `RefreshOutputParams`, and `ResolveSelectedSensor`.

- [ ] **Step 2: Run test to verify GREEN for conversion**

Run: `dotnet run --project CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`

Expected: conversion test passes unless unrelated build issues exist.

### Task 3: Add Line-Spectrum ViewModel and View

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/LineSpectrumDataCollectionViewModel.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml.cs`

- [ ] **Step 1: Create the ViewModel**

Use the same `DialogViewModelBase, IViewModuleParam` pattern as the existing point-spectrum ViewModel:

```csharp
public class LineSpectrumDataCollectionViewModel : DialogViewModelBase, IViewModuleParam
{
    public new LineSpectrumDataCollectionModel ModelParam
    {
        get => base.ModelParam as LineSpectrumDataCollectionModel;
        set => base.ModelParam = value;
    }

    public override void InitParam()
    {
        ModelParam = InitModelParam<LineSpectrumDataCollectionModel>();
        ModelParam.LoadKeyParam();
    }
}
```

Add `LoadCommand`, `GeneralCommand`, and `DataOperateCommand` so the node can load, execute, confirm, and manage output params.

- [ ] **Step 2: Create the XAML view**

Create a simple WPF UserControl with `prism:ViewModelLocator.AutoWireViewModel="True"`, a sensor ComboBox bound to `ModelParam.Models`, start/stop event text boxes, `HeightScale`, latest row counts, an output parameter DataGrid, and bottom `执行`/`取消`/`确认` buttons.

- [ ] **Step 3: Create code-behind**

```csharp
namespace Custom.WaferFlatnessMeasure.Views
{
    public partial class LineSpectrumDataCollectionView : UserControl
    {
        public LineSpectrumDataCollectionView()
        {
            InitializeComponent();
        }
    }
}
```

### Task 4: Register New Node

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`

- [ ] **Step 1: Add using and registration**

Add the new view through existing assembly registration and explicit node registration:

```csharp
containerRegistry.RegisterDialogAndMenu<LineSpectrumDataCollectionView>(null, new MenuInfo
{
    ModuleType = ModuleType.Custom,
    NodeType = NodeType.General,
    Title = "线光谱采集",
    Icon = "\ue6a2",
    Type = "99.半导体设备",
    Description = "线光谱传感器采集与高度/灰度数据输出",
    TargetType = typeof(LineSpectrumDataCollectionView),
});
```

### Task 5: Move Point-Spectrum Code Into Folder

**Files:**
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/SensorDataCollectionModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/Storage/PointTemperatureStorageService.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/Storage/SensorDataStorageService.cs`

- [ ] **Step 1: Move files**

Move the point-spectrum main model to `Acquisition/PointSpectrum/SensorDataCollectionModel.cs` and the storage helpers to `Acquisition/PointSpectrum/Storage/`.

- [ ] **Step 2: Verify namespaces**

Keep all moved files in namespace `Custom.WaferFlatnessMeasure.Models`; update no class names.

- [ ] **Step 3: Build**

Run: `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

Expected: build succeeds.

### Task 6: Final Verification

**Files:**
- All touched files.

- [ ] **Step 1: Run tests**

Run: `dotnet run --project CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`

Expected: all tests pass and output includes `Wafer flatness data analysis tests passed.`

- [ ] **Step 2: Run project build**

Run: `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

Expected: build succeeds with no new compile errors.

- [ ] **Step 3: Inspect changed files**

Run: `git diff -- CustomizedDemand/Custom.WaferFlatnessMeasure CustomizedDemand/Custom.WaferFlatnessMeasure.Tests docs/superpowers`

Expected: diff only contains the planned point-spectrum move, line-spectrum node, tests, and docs.

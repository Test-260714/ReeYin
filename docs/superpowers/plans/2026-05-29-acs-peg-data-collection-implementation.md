# ACS PEG And Data Collection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add ACS one-dimensional and two-dimensional PEG pulse output plus Data Collection readback support, including equidistant 2D path sampling for line, arc, and polyline paths.

**Architecture:** Keep all new behavior inside the ACS plugin assembly. Add focused process models, a testable 2D path sampler, PEG SDK wrappers, and Data Collection SDK wrappers on `AcsControlCard`; leave `IControlCard` and `ControlCardBase` public shapes stable. Existing `ControlPosComparison(...)` becomes a compatibility entry that delegates to ACS PEG methods where the existing model contains enough information.

**Tech Stack:** C# / .NET 8 WPF, ACS.SPiiPlusNET8 4.10, existing ReeYin-V `ControlCardBase`, no external test packages; use a small console test harness for deterministic geometry/model tests.

---

## File Structure

- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsProcessModels.cs` - serializable ACS PEG/Data Collection models and request/result types.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsPegPathSampler.cs` - deterministic line/arc/polyline sampling and reference-axis projection logic; no ACS hardware calls.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsPeg.cs` - PEG output configuration, incremental PEG, random PEG, 2D equidistant PEG, `ControlPosComparison(...)` compatibility.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsDataCollection.cs` - Data Collection start/stop/wait/read/run wrappers.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs` - add PEG defaults, output mappings, Data Collection defaults, and ensure helpers.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs` - call new ensure helpers from `EnsureOptions()`.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj` - no external-package console test harness.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs` - deterministic tests for sampler/model behavior.

## Baseline Commands

Run before and after implementation:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`. Existing `halcondotnet` warning is acceptable.

Run deterministic tests after the test project exists:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: process exits `0` and prints `ACS PEG/DataCollection tests passed.`

> Note: this workspace is not a git repository, so plan checkpoints use build/test verification instead of commits.

---

### Task 1: Add Failing Geometry And Model Tests

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Create the no-package console test project**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write failing tests for line, arc, polyline, projection, and option defaults**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`:

```csharp
using ReeYin_V.Core;
using ReeYin_V.Hardware.ControlCard.ACS.App;

Run("line sampling keeps equal path spacing", TestLineSampling);
Run("arc sampling keeps equal path spacing", TestArcSampling);
Run("polyline sampling crosses segment boundaries correctly", TestPolylineSampling);
Run("reference projection rejects non-monotonic paths", TestReferenceProjectionRejectsNonMonotonicPath);
Run("ACS options add PEG and data collection defaults", TestOptionsDefaults);

Console.WriteLine("ACS PEG/DataCollection tests passed.");

static void TestLineSampling()
{
    var points = AcsPegPathSampler.SampleLine(new AcsPoint2D(0, 0), new AcsPoint2D(10, 0), 2.5);

    AssertEqual(5, points.Count, "line sample count");
    AssertPoint(points[0], 0, 0, "line point 0");
    AssertPoint(points[1], 2.5, 0, "line point 1");
    AssertPoint(points[2], 5, 0, "line point 2");
    AssertPoint(points[3], 7.5, 0, "line point 3");
    AssertPoint(points[4], 10, 0, "line point 4");
}

static void TestArcSampling()
{
    var interval = Math.PI * 10d / 4d;
    var points = AcsPegPathSampler.SampleArcCenter(
        new AcsPoint2D(10, 0),
        new AcsPoint2D(0, 10),
        new AcsPoint2D(0, 0),
        DirOfRotation.閫嗗悜,
        interval);

    AssertEqual(3, points.Count, "arc sample count");
    AssertPoint(points[0], 10, 0, "arc point 0");
    AssertPoint(points[1], 10 / Math.Sqrt(2), 10 / Math.Sqrt(2), "arc point 1", 0.001);
    AssertPoint(points[2], 0, 10, "arc point 2", 0.001);
}

static void TestPolylineSampling()
{
    var points = AcsPegPathSampler.SamplePolyline(
        new[]
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(3, 0),
            new AcsPoint2D(3, 4)
        },
        2.5);

    AssertEqual(4, points.Count, "polyline sample count");
    AssertPoint(points[0], 0, 0, "polyline point 0");
    AssertPoint(points[1], 2.5, 0, "polyline point 1");
    AssertPoint(points[2], 3, 2, "polyline point 2");
    AssertPoint(points[3], 3, 4, "polyline point 3");
}

static void TestReferenceProjectionRejectsNonMonotonicPath()
{
    var points = new[]
    {
        new AcsPoint2D(0, 0),
        new AcsPoint2D(2, 1),
        new AcsPoint2D(1, 2)
    };

    var ok = AcsPegPathSampler.TryProjectReferencePositions(points, AcsPegReferenceAxis.X, out _, out var message);

    AssertFalse(ok, "non-monotonic projection should fail");
    AssertTrue(message.Contains("monotonic", StringComparison.OrdinalIgnoreCase), "projection message mentions monotonicity");
}

static void TestOptionsDefaults()
{
    var options = new AcsControlCardOptions();
    options.EnsurePegOutputs(new[] { En_AxisNum.X, En_AxisNum.Y });

    AssertEqual(2, options.PegOutputs.Count, "peg output count");
    AssertEqual(En_AxisNum.X, options.PegOutputs[0].Axis, "peg output X axis");
    AssertEqual("RY_PEG_POINTS", options.DefaultPegPointArrayName, "default point array");
    AssertEqual("RY_PEG_STATES", options.DefaultPegStateArrayName, "default state array");
    AssertEqual("RY_DC_DATA", options.DefaultDataCollectionArrayName, "default data collection array");
    AssertEqual(1000, options.DefaultDataCollectionSampleCount, "default sample count");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.Exit(1);
    }
}

static void AssertPoint(AcsPoint2D actual, double expectedX, double expectedY, string label, double tolerance = 0.000001)
{
    AssertClose(expectedX, actual.X, label + " X", tolerance);
    AssertClose(expectedY, actual.Y, label + " Y", tolerance);
}

static void AssertClose(double expected, double actual, string label, double tolerance = 0.000001)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

static void AssertTrue(bool value, string label)
{
    if (!value)
    {
        throw new InvalidOperationException(label);
    }
}

static void AssertFalse(bool value, string label)
{
    if (value)
    {
        throw new InvalidOperationException(label);
    }
}
```

- [ ] **Step 3: Run tests and verify RED**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj'
```

Expected: build fails because `AcsPegPathSampler`, `AcsPoint2D`, `AcsPegReferenceAxis`, and `EnsurePegOutputs` do not exist yet.

---

### Task 2: Add ACS Process Models And Options Defaults

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsProcessModels.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`

- [ ] **Step 1: Add ACS process model types**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsProcessModels.cs` with model types for `AcsPeg2DPathKind`, `AcsPegReferenceAxis`, `AcsPoint2D`, `AcsPegOutputConfig`, `AcsPegIncrementalRequest`, `AcsPegRandomRequest`, `AcsPeg2DPathRequest`, `AcsDataCollectionRequest`, and `AcsDataCollectionResult`.

Required shape:

```csharp
public enum AcsPeg2DPathKind { Line, ArcCenter, Polyline }
public enum AcsPegReferenceAxis { Auto, X, Y }
```

`AcsPegOutputConfig` must include `Axis`, `EngineToEncoderBitCode`, `GeneralOutputBitCode`, `OutputIndex`, `OutputBitCode`, `PulseWidth`, `Timeout`, `MotionFlags Flags`, and `IsEnabled`.

`AcsPeg2DPathRequest` must include `XAxis`, `YAxis`, `PathKind`, `ReferenceAxis`, `Start`, `End`, `Center`, `ArcDirection`, `Points`, `Interval`, `PulseWidth`, `PointArrayName`, `StateArrayName`, `StateValue`, and `StartImmediately`.

- [ ] **Step 2: Extend ACS options with PEG and Data Collection defaults**

In `AcsControlCardOptions.cs`, add:

```csharp
private ObservableCollection<AcsPegOutputConfig> _pegOutputs = new();
private string _defaultPegPointArrayName = "RY_PEG_POINTS";
private string _defaultPegStateArrayName = "RY_PEG_STATES";
private string _defaultDataCollectionArrayName = "RY_DC_DATA";
private int _defaultDataCollectionSampleCount = 1000;
private double _defaultDataCollectionPeriod = 1d;
```

Add corresponding public properties and `EnsurePegOutputs(IEnumerable<En_AxisNum>? axes)`. `EnsurePegOutputs` should mirror `EnsureHomeBuffers`: use configured axes, fall back to X/Y/Z/R, add missing `AcsPegOutputConfig` entries, default `IsEnabled=false`, `PulseWidth=10`, `Timeout=InternalTimeout`.

- [ ] **Step 3: Ensure PEG outputs in `AcsControlCard.EnsureOptions()`**

Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`:

```csharp
public void EnsureOptions()
{
    _options ??= new AcsControlCardOptions();
    var axes = Config?.AllAxis?.Select(axis => axis.AxisNum).ToArray();
    _options.EnsureHomeBuffers(axes);
    _options.EnsurePegOutputs(axes);
}
```

- [ ] **Step 4: Run tests and verify sampler still fails**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: build still fails only because `AcsPegPathSampler` does not exist.

---

### Task 3: Implement Deterministic 2D Path Sampler

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsPegPathSampler.cs`

- [ ] **Step 1: Add the path sampler implementation**

Create `AcsPegPathSampler` with these public methods:

```csharp
public static IReadOnlyList<AcsPoint2D> SampleLine(AcsPoint2D start, AcsPoint2D end, double interval)
public static IReadOnlyList<AcsPoint2D> SampleArcCenter(AcsPoint2D start, AcsPoint2D end, AcsPoint2D center, DirOfRotation direction, double interval)
public static IReadOnlyList<AcsPoint2D> SamplePolyline(IReadOnlyList<AcsPoint2D> points, double interval)
public static bool TrySamplePath(AcsPeg2DPathRequest request, out IReadOnlyList<AcsPoint2D> points, out string message)
public static bool TryProjectReferencePositions(IReadOnlyList<AcsPoint2D> points, AcsPegReferenceAxis referenceAxis, out double[] positions, out string message)
```

Implementation requirements:

- Validate finite coordinates and `interval > 0`.
- Include start and end points.
- For line length 10 and interval 2.5, return 0/2.5/5/7.5/10.
- For arc, require start and end lie on the same non-zero radius from center.
- For polyline, sample by cumulative path length across segment boundaries.
- `TryProjectReferencePositions` must reject non-monotonic X or Y positions; `Auto` should try X, then Y.

- [ ] **Step 2: Run tests and verify GREEN for sampler/options**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: exits `0`, prints all `PASS` lines and `ACS PEG/DataCollection tests passed.`

- [ ] **Step 3: Build ACS project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

---

### Task 4: Add PEG SDK Wrappers And Compatibility Entry

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsPeg.cs`

- [ ] **Step 1: Add PEG wrappers**

Create `AcsPeg.cs` with public methods:

```csharp
public bool ConfigurePegOutput(AcsPegOutputConfig config, out string message)
public bool StartIncrementalPeg(AcsPegIncrementalRequest request, out string message)
public bool StartRandomPeg(AcsPegRandomRequest request, out string message)
public bool StartEquidistantPeg2D(AcsPeg2DPathRequest request, out string message)
public bool StopPeg(En_AxisNum axisId, out string message)
public bool WaitPegReady(En_AxisNum axisId, int timeout, out string message)
public override bool ControlPosComparison(bool On_Off, PosComparisonOutputParam param)
```

SDK call requirements:

- `ConfigurePegOutput` calls `_api.AssignPegNT(axis, config.EngineToEncoderBitCode, config.GeneralOutputBitCode)` and `_api.AssignPegOutputsNT(axis, config.OutputIndex, config.OutputBitCode)`.
- `StartIncrementalPeg` calls `_api.PegIncNTV2(output.Flags, axis, output.PulseWidth, request.FirstPoint, request.Interval, request.LastPoint, -1, 0, -1, 0, 0, 0, 0, 0)`.
- `StartRandomPeg` writes point/state arrays with `_api.WriteVariable(...)`, then calls `_api.PegRandomNTV2(...)`.
- `StartEquidistantPeg2D` calls `AcsPegPathSampler.TrySamplePath(...)`, projects to reference positions, builds state array, then delegates to `StartRandomPeg`.
- On SDK exceptions, return `false`, include axis/array/action in `message`, and best-effort stop the PEG axis.
- `ControlPosComparison(...)` should stop PEG when `On_Off=false`; when point table exists, convert `PosCompareDatas` to random PEG; when `compareDimension=1` and `syncPos>0`, configure a minimal incremental PEG; when `compareDimension=2` has no point table, return `false` with a console message telling callers to use `StartEquidistantPeg2D`.

- [ ] **Step 2: Build ACS project and fix SDK enum/signature compile mismatches if any**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`. If `ProgramBuffer.ACSC_NONE` is rejected in `WriteVariable`, replace it with `(ProgramBuffer)(-1)` and rerun.

- [ ] **Step 3: Run deterministic tests**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: exits `0`.

---

### Task 5: Add Data Collection Wrappers

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsDataCollection.cs`

- [ ] **Step 1: Add Data Collection implementation**

Create `AcsDataCollection.cs` with public methods:

```csharp
public bool StartDataCollection(AcsDataCollectionRequest request, out string message)
public bool StopDataCollection(out string message)
public bool WaitDataCollectionEnd(En_AxisNum axisId, int timeout, out string message)
public bool ReadDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
public bool RunDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
```

SDK call requirements:

- `StartDataCollection` validates `IsConnected`, array name, sample count, period, and variables; then calls `_api.DataCollectionExt(request.Flags, axis, request.ArrayName, request.SampleCount, request.Period, request.Variables)`.
- `StopDataCollection` calls `_api.StopCollect()`.
- `WaitDataCollectionEnd` calls `_api.WaitCollectEndExt(Math.Max(1000, timeout), ToAcsAxis(axisId))`.
- `ReadDataCollection` first tries `_api.ReadRealVector(request.ArrayName, 0, request.SampleCount - 1, ProgramBuffer.ACSC_NONE)`; if that fails, try `_api.ReadRealMatrix(request.ArrayName, 0, request.SampleCount - 1, 0, 0, ProgramBuffer.ACSC_NONE)`.
- `RunDataCollection` starts, waits, then reads.
- On start/wait failures, best-effort call `_api.StopCollect()`.

- [ ] **Step 2: Build ACS project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

- [ ] **Step 3: Run deterministic tests**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: exits `0`.

---

### Task 6: Add Final Guards And Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/*.cs`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Verify SDK calls are isolated to ACS PEG/Data files**

Run:

```powershell
rg -n "AssignPegNT|AssignPegOutputsNT|PegIncNTV2|PegRandomNTV2|StartPegNT|StopPegNT|DataCollectionExt|StopCollect|WaitCollectEndExt|ReadRealVector|ReadRealMatrix|WriteVariable" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App'
```

Expected: PEG calls appear in `AcsPeg.cs`; Data Collection calls appear in `AcsDataCollection.cs`; no calls appear in base control-card project.

- [ ] **Step 2: Verify public ACS process APIs exist**

Run:

```powershell
rg -n "StartIncrementalPeg|StartRandomPeg|StartEquidistantPeg2D|StartDataCollection|RunDataCollection|ControlPosComparison" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App'
```

Expected: output includes the new ACS-specific methods and the compatibility override.

- [ ] **Step 3: Run deterministic tests**

Run:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore
```

Expected: exits `0` and prints `ACS PEG/DataCollection tests passed.`

- [ ] **Step 4: Build ACS project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`; existing `halcondotnet` warning is acceptable.

- [ ] **Step 5: Build base control card project to confirm no circular reference or XAML regression**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj' --no-restore -p:SolutionDir='E:\Company\宸ヤ綔鐩綍\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`; existing warnings are acceptable.

---

## Self-Review

- Spec coverage: tasks cover one-dimensional incremental PEG, one-dimensional random PEG, two-dimensional equidistant line/arc/polyline sampling, two-dimensional random point tables, compatibility `ControlPosComparison`, and Data Collection start/stop/wait/readback.
- Completeness scan: no unfinished implementation markers or undefined task markers remain.
- Type consistency: `AcsPegOutputConfig`, `AcsPegIncrementalRequest`, `AcsPegRandomRequest`, `AcsPeg2DPathRequest`, `AcsDataCollectionRequest`, `AcsDataCollectionResult`, and `AcsPegPathSampler` are defined before use.
- Dependency check: no base project reference to ACS is introduced; new public methods live only on `AcsControlCard`.
- Verification: final task includes console tests plus ACS and base project builds.


# ACS Buffer 插补运动替换 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 ACS 项目的普通直线/圆弧和 XSEG 直线/圆弧插补从 SDK 直连运动 API 替换为 ACSPL+ Program Buffer 执行，并让基础控制卡项目优先通过 `ICoordinatedMotionCard` 调用。

**Architecture:** ACS 项目新增统一插补 Buffer 执行器，负责构造 ACSPL+ 脚本并复用 `TryPrepareProgramBuffer -> LoadBuffer -> CompileBuffer -> RunBuffer -> WaitProgramEnd` 流程。基础项目只扩展厂商无关的请求/参数模型和能力接口调用路径，不引用 ACS 命名空间或具体类型。

**Tech Stack:** C#/.NET 8 WPF, ACS.SPiiPlusNET, Prism, existing console-style ACS test harness.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Replace old SDK-path interpolation assertions with Buffer-script assertions.
  - Add `AxisViewModel` coordinated-motion routing assertions.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardSyncModels.cs`
  - Add optional `BufferNo` to `CoordinatedMotionRequest`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
  - Add optional `BufferNo` to `LineInterPoParam` and `ArcInterPoParam`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs`
  - Add configurable `InterpolationBufferNo`, default 9.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`
  - Expose `InterpolationBufferNo` pass-through property.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InterpolationBufferMotion.cs`
  - Add script builders and Buffer execution helper for line/XSEG line/arc/XSEG arc.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
  - Route `LineInterpoMoving` and `LineInterpoMovingXseg` through Buffer scripts.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`
  - Route `ArcInterpoMoving` and `ArcInterpoMovingXseg` through Buffer scripts.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/XsegInterpolation.cs`
  - Keep only non-command helpers such as current-position reading; remove SDK queue command helpers.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`
  - Propagate `BufferNo`, `WaitForEnd`, and `Timeout` from `CoordinatedMotionRequest`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
  - Use `ICoordinatedMotionCard` before legacy fallback for “移动至指定位置”.

Git is currently not usable in this workspace (`fatal: not a git repository...`). If it remains unavailable, replace each commit step with a note in the final report.

---

### Task 1: RED Tests For Buffer-Based Interpolation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Replace old interpolation test registrations**

Change the current registrations:

```csharp
Run("ACS basic line interpolation remains basic path", TestAcsBasicLineInterpolationRemainsBasicPath);
Run("ACS XSEG line interpolation uses extended segmented queue", TestAcsXsegLineInterpolationUsesQueue);
Run("ACS XSEG arc interpolation uses extended segmented queue", TestAcsXsegArcInterpolationUsesQueue);
```

to:

```csharp
Run("ACS options expose interpolation Buffer defaults", TestAcsOptionsExposeInterpolationBufferDefaults);
Run("ACS interpolation uses Buffer scripts instead of SDK interpolation APIs", TestAcsInterpolationUsesBufferScripts);
Run("Axis view model uses coordinated motion capability before fallback", TestAxisViewModelUsesCoordinatedMotionCapability);
```

- [ ] **Step 2: Add failing tests**

Replace the bodies of `TestAcsBasicLineInterpolationRemainsBasicPath`, `TestAcsXsegLineInterpolationUsesQueue`, and `TestAcsXsegArcInterpolationUsesQueue` with these new tests:

```csharp
void TestAcsOptionsExposeInterpolationBufferDefaults()
{
    var options = new AcsControlCardOptions();
    AssertEqual(9, options.InterpolationBufferNo, "default interpolation Buffer should avoid home and LCI defaults");

    options.InterpolationBufferNo = -1;
    AssertEqual(0, options.InterpolationBufferNo, "interpolation Buffer should clamp low values");

    options.InterpolationBufferNo = 100;
    AssertEqual(64, options.InterpolationBufferNo, "interpolation Buffer should clamp high values");

    var requestSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardSyncModels.cs");
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    AssertContains(requestSource, "public int? BufferNo", "coordinated motion request should carry an optional Buffer number");
    AssertContains(axisModelSource, "public int? BufferNo", "line and arc interpolation params should carry an optional Buffer number");
}

void TestAcsInterpolationUsesBufferScripts()
{
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var arcSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\CircularInterpolation.cs");
    var xsegSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\XsegInterpolation.cs");
    var bufferSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs");

    AssertContains(bufferSource, "RunInterpolationBufferScript", "ACS interpolation should share a Buffer execution helper");
    AssertContains(bufferSource, "_api.LoadBuffer(buffer, script)", "interpolation Buffer helper should load script");
    AssertContains(bufferSource, "_api.CompileBuffer(buffer)", "interpolation Buffer helper should compile script");
    AssertContains(bufferSource, "_api.RunBuffer(buffer, null)", "interpolation Buffer helper should run script");
    AssertContains(bufferSource, "_api.WaitProgramEnd(buffer", "interpolation Buffer helper should wait when requested");

    AssertContains(bufferSource, "BuildLineInterpolationBufferScript", "line interpolation should have a script builder");
    AssertContains(bufferSource, "BuildXsegLineInterpolationBufferScript", "XSEG line interpolation should have a script builder");
    AssertContains(bufferSource, "BuildArcInterpolationBufferScript", "arc interpolation should have a script builder");
    AssertContains(bufferSource, "BuildXsegArcInterpolationBufferScript", "XSEG arc interpolation should have a script builder");

    AssertContains(ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)"),
        "RunInterpolationBufferScript", "basic line interpolation should run via Buffer");
    AssertContains(ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)"),
        "RunInterpolationBufferScript", "XSEG line interpolation should run via Buffer");
    AssertContains(ReadMethodBody(arcSource, "public override bool ArcInterpoMoving(ArcInterPoParam param)"),
        "RunInterpolationBufferScript", "basic arc interpolation should run via Buffer");
    AssertContains(ReadMethodBody(arcSource, "public bool ArcInterpoMovingXseg(ArcInterPoParam param)"),
        "RunInterpolationBufferScript", "XSEG arc interpolation should run via Buffer");

    foreach (var source in new[] { lineSource, arcSource, xsegSource, bufferSource })
    {
        AssertFalse(source.Contains("_api.Line(", StringComparison.Ordinal), "ACS interpolation must not call SDK Line");
        AssertFalse(source.Contains("_api.ExtLine(", StringComparison.Ordinal), "ACS interpolation must not call SDK ExtLine");
        AssertFalse(source.Contains("_api.Arc1(", StringComparison.Ordinal), "ACS interpolation must not call SDK Arc1");
        AssertFalse(source.Contains("_api.ExtArc1(", StringComparison.Ordinal), "ACS interpolation must not call SDK ExtArc1");
        AssertFalse(source.Contains("ExtendedSegmentedMotionV2", StringComparison.Ordinal), "ACS interpolation must not call SDK XSEG start");
        AssertFalse(source.Contains("SegmentLineV2", StringComparison.Ordinal), "ACS interpolation must not call SDK segment line");
        AssertFalse(source.Contains("SegmentArc1V2", StringComparison.Ordinal), "ACS interpolation must not call SDK segment arc");
        AssertFalse(source.Contains("EndSequenceM", StringComparison.Ordinal), "ACS interpolation must not call SDK XSEG end");
    }
}

void TestAxisViewModelUsesCoordinatedMotionCapability()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");

    AssertFalse(source.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
        "AxisViewModel should not reference ACS namespace");
    AssertFalse(source.Contains("AcsControlCard", StringComparison.Ordinal),
        "AxisViewModel should not hard-code ACS control-card type");

    var moveBody = ReadSourceBetween(source, "case \"移动至指定位置\":", "case \"Z轴使能\":");
    AssertContains(moveBody, "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "AxisViewModel should route planar move through a helper");
    AssertContainsBefore(moveBody,
        "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "MoveAdditionalAxes(targetPositions)",
        "AxisViewModel should move extra axes after planar motion succeeds");

    var guard = ReadBlockStartingAt(moveBody, "if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))");
    AssertContains(guard, "return;", "AxisViewModel should stop when coordinated planar motion fails");

    var methodBody = ReadMethodBody(source, "private bool TryMovePlanarAxesToTarget(");
    AssertContains(methodBody, "ICoordinatedMotionCard", "AxisViewModel should use neutral coordinated motion capability");
    AssertContains(methodBody, "SupportsCoordinatedMotion", "AxisViewModel should check coordinated motion support");
    AssertContains(methodBody, "new CoordinatedMotionRequest", "AxisViewModel should build a coordinated motion request");
    AssertContains(methodBody, "MoveCoordinated", "AxisViewModel should call MoveCoordinated");
    AssertContains(methodBody, "CustomInterpolationMoving", "AxisViewModel should keep legacy fallback");
    AssertContainsBefore(methodBody, "MoveCoordinated", "CustomInterpolationMoving",
        "AxisViewModel should prefer coordinated motion before fallback");
}
```

- [ ] **Step 3: Run RED test**

Run:

```powershell
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-build
```

Expected: FAIL because `AcsControlCardOptions.InterpolationBufferNo`, `InterpolationBufferMotion.cs`, `BufferNo` model fields, and `AxisViewModel` coordinated helper do not exist yet.

- [ ] **Step 4: Commit or record git blockage**

Run:

```powershell
git status --short
```

Expected in current workspace: `fatal: not a git repository...`; record this in the task notes instead of committing.

---

### Task 2: Add Buffer Number To Neutral Models And ACS Options

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardSyncModels.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`

- [ ] **Step 1: Add neutral request field**

In `CoordinatedMotionRequest`, add after `Timeout`:

```csharp
public int? BufferNo { get; set; }
```

- [ ] **Step 2: Add line and arc parameter fields**

In `LineInterPoParam`, add after `waitforend`:

```csharp
/// <summary>
/// 可选 Program Buffer 编号，支持 ACS 等需要 Buffer 执行插补的板卡。
/// </summary>
public int? BufferNo { get; set; }
```

In `ArcInterPoParam`, add after `waitforend`:

```csharp
/// <summary>
/// 可选 Program Buffer 编号，支持 ACS 等需要 Buffer 执行插补的板卡。
/// </summary>
public int? BufferNo { get; set; }
```

- [ ] **Step 3: Add ACS default interpolation Buffer option**

In `AcsControlCardOptions`, add a field near `_internalTimeout`:

```csharp
private int _interpolationBufferNo = 9;
```

Add a property after `InternalTimeout`:

```csharp
public int InterpolationBufferNo
{
    get => _interpolationBufferNo;
    set
    {
        _interpolationBufferNo = Math.Clamp(value, 0, 64);
        RaisePropertyChanged();
    }
}
```

- [ ] **Step 4: Expose pass-through property on ACS card**

In `AcsControlCard.cs`, add after `InternalTimeout`:

```csharp
public int InterpolationBufferNo
{
    get => Options.InterpolationBufferNo;
    set
    {
        Options.InterpolationBufferNo = value;
        RaisePropertyChanged();
    }
}
```

- [ ] **Step 5: Run focused build**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

Expected: build passes or only proceeds to later RED runtime failures. Existing warnings about nullable and `halcondotnet` may remain.

---

### Task 3: Add ACS Interpolation Buffer Script Runner

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InterpolationBufferMotion.cs`

- [ ] **Step 1: Create script runner and formatting helpers**

Create `InterpolationBufferMotion.cs` with this structure:

```csharp
using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    private bool RunInterpolationBufferScript(
        int? requestedBufferNo,
        string script,
        bool waitForEnd,
        int timeout,
        IEnumerable<En_AxisNum> axisIds,
        out string message)
    {
        var bufferNo = ResolveInterpolationBufferNo(requestedBufferNo);
        if (!TryPrepareProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            message = "ACS interpolation Buffer script cannot be empty.";
            return false;
        }

        try
        {
            TryStopProgramBuffer(bufferNo, out _);
            _api.LoadBuffer(buffer, script);
            _api.CompileBuffer(buffer);
            _api.RunBuffer(buffer, null);

            if (waitForEnd)
            {
                _api.WaitProgramEnd(buffer, Math.Max(1000, timeout));
                UpdateAxisStates(axisIds);
            }

            message = $"ACS interpolation Buffer {bufferNo} started.";
            return true;
        }
        catch (Exception ex)
        {
            TryStopProgramBuffer(bufferNo, out _);
            message = $"ACS interpolation Buffer {bufferNo} failed: {ex.Message}; {FormatProgramDiagnostics(bufferNo)}";
            Console.WriteLine(message);
            return false;
        }
    }

    private int ResolveInterpolationBufferNo(int? requestedBufferNo)
    {
        return Math.Clamp(requestedBufferNo ?? Options.InterpolationBufferNo, 0, 64);
    }

    private static string FormatAxisTuple(IReadOnlyList<string> axisNames)
    {
        return $"({string.Join(", ", axisNames)})";
    }

    private static string FormatPointList(IEnumerable<double> points)
    {
        return string.Join(", ", points.Select(FormatAcsNumber));
    }

    private static string FormatRotation(DirOfRotation direction)
    {
        return (int)direction == 0 ? "CW" : "CCW";
    }
}
```

- [ ] **Step 2: Add line script builders**

Append these methods in the same partial class:

```csharp
private string BuildLineInterpolationBufferScript(Axis[] axes, double[] target, double velocity)
{
    var axisNames = axes.Select((_, index) => $"Ax{index}").ToList();
    var builder = new StringBuilder();
    AppendAxisDeclarations(builder, axes, axisNames);
    AppendEnableAndMotionTuning(builder, axisNames, velocity);
    builder.AppendLine($"LINE {FormatAxisTuple(axisNames)}, {FormatPointList(target)}");
    builder.AppendLine("STOP");
    return builder.ToString();
}

private string BuildXsegLineInterpolationBufferScript(Axis[] axes, double[] startPoint, double[] target, double velocity)
{
    var axisNames = axes.Select((_, index) => $"Ax{index}").ToList();
    var tuple = FormatAxisTuple(axisNames);
    var builder = new StringBuilder();
    AppendAxisDeclarations(builder, axes, axisNames);
    AppendEnableAndMotionTuning(builder, axisNames, velocity);
    builder.AppendLine($"XSEG {tuple}, {FormatPointList(startPoint)}");
    builder.AppendLine($"LINE {tuple}, {FormatPointList(target)}");
    builder.AppendLine($"ENDS {tuple}");
    builder.AppendLine($"TILL GSEG({axisNames[0]}) = -1");
    builder.AppendLine("STOP");
    return builder.ToString();
}
```

- [ ] **Step 3: Add arc script builders**

Append these methods:

```csharp
private string BuildArcInterpolationBufferScript(
    Axis[] axes,
    double[] center,
    double[] finalPoint,
    DirOfRotation direction,
    double velocity)
{
    var axisNames = axes.Take(2).Select((_, index) => $"Ax{index}").ToList();
    var tuple = FormatAxisTuple(axisNames);
    var builder = new StringBuilder();
    AppendAxisDeclarations(builder, axes.Take(2).ToArray(), axisNames);
    AppendEnableAndMotionTuning(builder, axisNames, velocity);
    builder.AppendLine($"ARC1 {tuple}, {FormatPointList(center)}, {FormatPointList(finalPoint)}, {FormatRotation(direction)}");
    builder.AppendLine("STOP");
    return builder.ToString();
}

private string BuildXsegArcInterpolationBufferScript(
    Axis[] axes,
    double[] startPoint,
    double[] center,
    double[] finalPoint,
    DirOfRotation direction,
    double velocity)
{
    var axisNames = axes.Take(2).Select((_, index) => $"Ax{index}").ToList();
    var tuple = FormatAxisTuple(axisNames);
    var builder = new StringBuilder();
    AppendAxisDeclarations(builder, axes.Take(2).ToArray(), axisNames);
    AppendEnableAndMotionTuning(builder, axisNames, velocity);
    builder.AppendLine($"XSEG {tuple}, {FormatPointList(startPoint)}");
    builder.AppendLine($"ARC1 {tuple}, {FormatPointList(center)}, {FormatPointList(finalPoint)}, {FormatRotation(direction)}");
    builder.AppendLine($"ENDS {tuple}");
    builder.AppendLine($"TILL GSEG({axisNames[0]}) = -1");
    builder.AppendLine("STOP");
    return builder.ToString();
}
```

- [ ] **Step 4: Add shared ACSPL block helpers**

Append these methods:

```csharp
private static void AppendAxisDeclarations(StringBuilder builder, IReadOnlyList<Axis> axes, IReadOnlyList<string> axisNames)
{
    for (var index = 0; index < axes.Count; index++)
    {
        builder.AppendLine($"int {axisNames[index]} = {(int)axes[index]}");
    }

    builder.AppendLine();
}

private static void AppendEnableAndMotionTuning(StringBuilder builder, IReadOnlyList<string> axisNames, double velocity)
{
    var tuple = FormatAxisTuple(axisNames);
    builder.AppendLine($"ENABLE {tuple}");
    builder.Append("TILL ");
    builder.AppendLine(string.Join(" & ", axisNames.Select(axis => $"MST({axis}).#ENABLED")));
    builder.AppendLine();

    foreach (var axisName in axisNames)
    {
        AppendAxisMotionTuning(builder, axisName, velocity > 0d ? velocity : 10d);
        builder.AppendLine();
    }
}
```

- [ ] **Step 5: Run RED test again**

Run:

```powershell
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-build
```

Expected: tests still fail because production interpolation methods have not routed to `RunInterpolationBufferScript` yet.

---

### Task 4: Route ACS Line Interpolation Through Buffer

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/XsegInterpolation.cs`

- [ ] **Step 1: Update `LineInterpoMoving`**

Replace the SDK command section in `LineInterpoMoving` with Buffer execution:

```csharp
var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
var script = BuildLineInterpolationBufferScript(axes, target, velocity);
if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, InternalTimeout, axisIds, out var message))
{
    Console.WriteLine(message);
    return false;
}

return true;
```

The method should no longer contain `_api.Line(` or `_api.ExtLine(`.

- [ ] **Step 2: Update `LineInterpoMovingXseg`**

Replace the SDK queue section with:

```csharp
var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
var script = BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity);
if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, InternalTimeout, axisIds, out var message))
{
    Console.WriteLine(message);
    return false;
}

return true;
```

- [ ] **Step 3: Remove SDK queue helpers**

In `XsegInterpolation.cs`, remove these methods completely:

```csharp
private void StartXseg(...)
private void AddXsegLine(...)
private void AddXsegArc1(...)
private void EndXseg(...)
private static Axis[] EnsureAcsAxisTerminator(...)
private static MotionFlags GetVelocityFlag(...)
private static double GetOptionalVelocity(...)
```

Keep `TryGetCurrentInterpolationPoint` because it only reads current feedback position and is still needed for XSEG start points.

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

Expected: build passes or only exposes arc/AxisView tests still failing.

---

### Task 5: Route ACS Arc Interpolation Through Buffer

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`

- [ ] **Step 1: Update `ArcInterpoMoving`**

Replace the SDK command section with:

```csharp
var finalPoint = new[] { param.Destination.X, param.Destination.Y };
var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
var script = BuildArcInterpolationBufferScript(axes, center, finalPoint, param.Dir, velocity);
if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, InternalTimeout, axisIds, out var message))
{
    Console.WriteLine(message);
    return false;
}

return true;
```

The method should no longer contain `_api.Arc1(` or `_api.ExtArc1(`.

- [ ] **Step 2: Update `ArcInterpoMovingXseg`**

Replace the SDK queue section with:

```csharp
var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
var script = BuildXsegArcInterpolationBufferScript(axes, startPoint, center, finalPoint, param.Dir, velocity);
if (!RunInterpolationBufferScript(param.BufferNo, script, param.waitforend, InternalTimeout, axisIds, out var message))
{
    Console.WriteLine(message);
    return false;
}

return true;
```

- [ ] **Step 3: Verify no banned SDK interpolation APIs remain**

Run:

```powershell
rg -n "_api\.(Line|ExtLine|Arc1|ExtArc1)\(|ExtendedSegmentedMotionV2|SegmentLineV2|SegmentArc1V2|EndSequenceM" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App"
```

Expected: no matches in ACS interpolation files. Matches in unrelated generated files or non-interpolation features must be reviewed before continuing.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-build
```

Expected: ACS interpolation Buffer test passes; AxisViewModel coordinated test may still fail.

---

### Task 6: Propagate Buffer Requests Through Coordinated Motion

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`

- [ ] **Step 1: Update `MoveCoordinated` arc path**

Change the arc branch from:

```csharp
CoordinatedMotionKind.Arc when request.ArcParam != null => ArcInterpoMoving(request.ArcParam),
```

to:

```csharp
CoordinatedMotionKind.Arc when request.ArcParam != null => MoveCoordinatedArc(request),
```

Add helper:

```csharp
private bool MoveCoordinatedArc(CoordinatedMotionRequest request)
{
    request.ArcParam!.BufferNo ??= request.BufferNo;
    request.ArcParam.waitforend = request.WaitForEnd;
    return ArcInterpoMoving(request.ArcParam);
}
```

- [ ] **Step 2: Update `BuildLineParam`**

Replace `BuildLineParam` with:

```csharp
private LineInterPoParam BuildLineParam(CoordinatedMotionRequest request)
{
    var lineParam = request.LineParam ?? new LineInterPoParam
    {
        InterPoAxiss = request.Axes.ToList(),
        TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
        TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
        DefaultSpeed = request.SpeedType,
    };

    lineParam.waitforend = request.WaitForEnd;
    lineParam.BufferNo ??= request.BufferNo;
    return lineParam;
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

Expected: build passes.

---

### Task 7: Update AxisViewModel To Prefer Coordinated Motion

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`

- [ ] **Step 1: Add planar-axis field and helper parameter builders**

Add near fields:

```csharp
private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];
```

Add methods before `#endregion` for Methods:

```csharp
private static LineInterPoParam CreateLineInterpolationParam(
    Dictionary<En_AxisNum, double> targetPositions,
    double[] targetPosArray)
{
    return new LineInterPoParam
    {
        InterPoAxiss = PlanarMoveAxes.ToList(),
        TargetPos = targetPosArray.ToArray(),
        TargetPosDic = new Dictionary<En_AxisNum, double>(targetPositions),
        decZSpeed = [5, 10, 50],
        upZSpeed = [5, 10, 50],
        waitforend = true,
    };
}

private static CustomInterPoParam CreateCustomInterpolationParam(
    Dictionary<En_AxisNum, double> targetPositions,
    double[] targetPosArray)
{
    return new CustomInterPoParam
    {
        InterPoAxiss = PlanarMoveAxes.ToList(),
        TargetPos = targetPosArray.ToArray(),
        TargetPosDic = new Dictionary<En_AxisNum, double>(targetPositions),
        waitforend = true,
    };
}
```

- [ ] **Step 2: Add coordinated planar move helper**

Add:

```csharp
private bool TryMovePlanarAxesToTarget(
    ControlCardBase controlCard,
    Dictionary<En_AxisNum, double> targetPositions,
    double[] targetPosArray)
{
    var lineParam = CreateLineInterpolationParam(targetPositions, targetPosArray);
    if (controlCard is ICoordinatedMotionCard coordinatedMotionCard &&
        coordinatedMotionCard.SupportsCoordinatedMotion)
    {
        var request = new CoordinatedMotionRequest
        {
            Kind = CoordinatedMotionKind.Line,
            Axes = PlanarMoveAxes.ToList(),
            TargetPositions = new Dictionary<En_AxisNum, double>(targetPositions),
            WaitForEnd = true,
            LineParam = lineParam,
        };

        if (!coordinatedMotionCard.MoveCoordinated(request, out var message))
        {
            Console.WriteLine($"AxisView coordinated move failed: {message}");
            return false;
        }

        return true;
    }

    if (!controlCard.CustomInterpolationMoving(
        CreateCustomInterpolationParam(targetPositions, targetPosArray),
        () => controlCard.LineInterpoMoving(lineParam) ? "OK" : "NG",
        true))
    {
        Console.WriteLine("AxisView custom interpolation move failed.");
        return false;
    }

    return true;
}
```

- [ ] **Step 3: Add additional-axis helper**

Add:

```csharp
private void MoveAdditionalAxes(IReadOnlyDictionary<En_AxisNum, double> targetPositions)
{
    if (targetPositions.TryGetValue(En_AxisNum.Z, out var zTargetPosition))
    {
        Task.Run(() =>
        {
            if (!ControlCard.MoveAbsoluteAxis(En_AxisNum.Z, zTargetPosition))
            {
                Console.WriteLine("执行Z轴移动失败！！！");
            }
        });
    }

    if (targetPositions.TryGetValue(En_AxisNum.Z1, out var z1TargetPosition))
    {
        Task.Run(() =>
        {
            ControlCard.GetAllPosInfos();
            if (!AxisViewAxisMatcher.TryGetAxis(ControlCard.Config.AllAxis, En_AxisNum.Y, out var yAxis) || yAxis.CurPos < 150)
            {
                MessageBox.Show("不在安全位置，无法移动！");
                return;
            }

            if (!ControlCard.MoveAbsoluteAxis(En_AxisNum.Z1, z1TargetPosition))
            {
                Console.WriteLine("执行Z1轴移动失败！！！");
            }
        });
    }
}
```

- [ ] **Step 4: Replace “移动至指定位置” task body**

Inside the existing `_localTask = Task.Factory.StartNew(() => { ... })`, replace the old `CustomInterpolationMoving` block and duplicated Z/Z1 movement with:

```csharp
var controlCard = ControlCard;
_localTask = Task.Factory.StartNew(() =>
{
    if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))
    {
        return;
    }

    MoveAdditionalAxes(targetPositions);
});
```

- [ ] **Step 5: Build and run focused test**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-build
```

Expected: new AxisViewModel coordinated test passes. The full harness may still stop on existing task-unrelated `WaferFlatness point mode uses sync trigger capabilities`.

---

### Task 8: Final Verification And Review

**Files:**
- Review all modified files.

- [ ] **Step 1: Verify no ACS hard-coding leaked into base project**

Run:

```powershell
rg -n "ReeYin_V\.Hardware\.ControlCard\.ACS|AcsControlCard" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard" -g "*.cs"
```

Expected: no matches in base project source.

- [ ] **Step 2: Verify banned ACS interpolation APIs are gone from interpolation paths**

Run:

```powershell
rg -n "_api\.(Line|ExtLine|Arc1|ExtArc1)\(|ExtendedSegmentedMotionV2|SegmentLineV2|SegmentArc1V2|EndSequenceM" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\CircularInterpolation.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\XsegInterpolation.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs"
```

Expected: no matches.

- [ ] **Step 3: Build touched projects**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

Expected: both builds complete with 0 errors. Existing nullable and `halcondotnet` warnings may remain.

- [ ] **Step 4: Run ACS test harness**

Run:

```powershell
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-build
```

Expected:

- New ACS Buffer interpolation tests pass.
- New AxisViewModel coordinated-routing test passes.
- If harness stops at existing task-unrelated WaferFlatness failure, record exact failure line in final report.

- [ ] **Step 5: Request code review**

Use `superpowers:requesting-code-review` or a read-only reviewer subagent. Ask it to check:

- No banned SDK interpolation APIs remain in ACS interpolation paths.
- Buffer scripts are built from validated axis/target/arc data.
- Base project remains ACS-independent.
- Failure paths return `false` before extra axes move.

- [ ] **Step 6: Final report**

Report in Chinese:

- Files changed.
- Behavior changes.
- Commands run and results.
- Any existing warnings or unrelated test failures.
- Git status limitation if `git status` still reports not a repository.


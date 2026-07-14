# ACS XSEG Interpolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve the existing ACS `Line/ExtLine` and `Arc1/ExtArc1` interpolation behavior while adding ACS-specific XSEG line and arc interpolation methods.

**Architecture:** Keep the existing shared `IControlCard` and `ControlCardBase` APIs unchanged. Add ACS-only public methods on the `AcsControlCard` partial class and place shared XSEG queue helpers in a new `XsegInterpolation.cs` partial file.

**Tech Stack:** C# / .NET 8 WPF library, `ACS.SPiiPlusNET8.dll`, source-level hardware tests in `ReeYin_V.Hardware.ControlCard.ACS.Tests`, `dotnet build` and `dotnet run` verification.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs` — add source-level tests for basic and XSEG interpolation.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/XsegInterpolation.cs` — shared XSEG start, segment, end, current-position, and optional-argument helpers.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs` — keep `LineInterpoMoving` unchanged and add `LineInterpoMovingXseg`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs` — keep `ArcInterpoMoving` unchanged and add `ArcInterpoMovingXseg`.

## Commands

Build tests:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Run tests after a successful build:

```powershell
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

This workspace currently has no `.git` directory, so commit steps are not included.

---

### Task 1: Add Failing Source-Level Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add test registrations**

Add these `Run(...)` calls after the existing ACS relative move tests and before the monitor/test view-model checks:

```csharp
Run("ACS basic line interpolation remains basic path", TestAcsBasicLineInterpolationRemainsBasicPath);
Run("ACS XSEG line interpolation uses extended segmented queue", TestAcsXsegLineInterpolationUsesQueue);
Run("ACS XSEG arc interpolation uses extended segmented queue", TestAcsXsegArcInterpolationUsesQueue);
Run("ACS XSEG methods stay ACS-specific", TestAcsXsegMethodsStayAcsSpecific);
```

- [ ] **Step 2: Add test methods**

Add these methods near the existing `TestAcsRelativeMoveAppliesDirectionToSignedDistance` test:

```csharp
void TestAcsBasicLineInterpolationRemainsBasicPath()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var methodBody = ReadMethodBody(
        source,
        "public override bool LineInterpoMoving(LineInterPoParam param)",
        "private bool TryBuildInterpolationMove");

    AssertTrue(methodBody.Contains("_api.ExtLine(", StringComparison.Ordinal), "basic line interpolation should keep ExtLine");
    AssertTrue(methodBody.Contains("_api.Line(", StringComparison.Ordinal), "basic line interpolation should keep Line");
    AssertFalse(methodBody.Contains("LineInterpoMovingXseg", StringComparison.Ordinal), "basic line interpolation should not delegate to XSEG");
}

void TestAcsXsegLineInterpolationUsesQueue()
{
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var xsegSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\XsegInterpolation.cs");

    AssertTrue(lineSource.Contains("public bool LineInterpoMovingXseg(LineInterPoParam param)", StringComparison.Ordinal), "LineInterpoMovingXseg should exist");
    AssertTrue(lineSource.Contains("TryGetCurrentInterpolationPoint", StringComparison.Ordinal), "XSEG line should start from current feedback position");
    AssertTrue(lineSource.Contains("StartXseg(", StringComparison.Ordinal), "XSEG line should start an extended segmented queue");
    AssertTrue(lineSource.Contains("AddXsegLine(", StringComparison.Ordinal), "XSEG line should append a line segment");
    AssertTrue(lineSource.Contains("EndXseg(", StringComparison.Ordinal), "XSEG line should close the segment sequence");
    AssertTrue(xsegSource.Contains("_api.ExtendedSegmentedMotionV2(", StringComparison.Ordinal), "XSEG helper should call ExtendedSegmentedMotionV2");
    AssertTrue(xsegSource.Contains("_api.SegmentLineV2(", StringComparison.Ordinal), "XSEG helper should call SegmentLineV2");
    AssertTrue(xsegSource.Contains("_api.EndSequenceM(", StringComparison.Ordinal), "XSEG helper should call EndSequenceM");
}

void TestAcsXsegArcInterpolationUsesQueue()
{
    var arcSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\CircularInterpolation.cs");
    var xsegSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\XsegInterpolation.cs");

    AssertTrue(arcSource.Contains("public bool ArcInterpoMovingXseg(ArcInterPoParam param)", StringComparison.Ordinal), "ArcInterpoMovingXseg should exist");
    AssertTrue(arcSource.Contains("TryGetCurrentInterpolationPoint", StringComparison.Ordinal), "XSEG arc should start from current feedback position");
    AssertTrue(arcSource.Contains("StartXseg(", StringComparison.Ordinal), "XSEG arc should start an extended segmented queue");
    AssertTrue(arcSource.Contains("AddXsegArc1(", StringComparison.Ordinal), "XSEG arc should append an Arc1 segment");
    AssertTrue(arcSource.Contains("EndXseg(", StringComparison.Ordinal), "XSEG arc should close the segment sequence");
    AssertTrue(xsegSource.Contains("_api.SegmentArc1V2(", StringComparison.Ordinal), "XSEG helper should call SegmentArc1V2");
}

void TestAcsXsegMethodsStayAcsSpecific()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");
    var baseSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs");

    AssertFalse(interfaceSource.Contains("LineInterpoMovingXseg", StringComparison.Ordinal), "IControlCard should not expose ACS-specific XSEG line interpolation");
    AssertFalse(interfaceSource.Contains("ArcInterpoMovingXseg", StringComparison.Ordinal), "IControlCard should not expose ACS-specific XSEG arc interpolation");
    AssertFalse(baseSource.Contains("LineInterpoMovingXseg", StringComparison.Ordinal), "ControlCardBase should not expose ACS-specific XSEG line interpolation");
    AssertFalse(baseSource.Contains("ArcInterpoMovingXseg", StringComparison.Ordinal), "ControlCardBase should not expose ACS-specific XSEG arc interpolation");
}
```

- [ ] **Step 3: Add a reusable method-body helper**

Add this helper after `ReadAcsRelativeMoveMethodBody()`:

```csharp
string ReadMethodBody(string source, string methodSignature, string nextMemberSignature)
{
    var methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
    AssertTrue(methodStart >= 0, $"{methodSignature} should exist");

    var nextMemberStart = source.IndexOf(nextMemberSignature, methodStart, StringComparison.Ordinal);
    AssertTrue(nextMemberStart > methodStart, $"{methodSignature} should be followed by {nextMemberSignature}");

    return source[methodStart..nextMemberStart];
}
```

- [ ] **Step 4: Verify RED**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

Expected: build succeeds, test run fails with a missing `XsegInterpolation.cs` or missing `LineInterpoMovingXseg` assertion.

---

### Task 2: Add Shared XSEG Queue Helpers

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/XsegInterpolation.cs`

- [ ] **Step 1: Create the helper partial file**

Create `XsegInterpolation.cs` with this content:

```csharp
using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using System;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    private bool TryGetCurrentInterpolationPoint(En_AxisNum[] axisIds, out double[] point)
    {
        point = Array.Empty<double>();
        if (axisIds == null || axisIds.Length == 0)
        {
            Console.WriteLine("ACS XSEG interpolation axes are missing.");
            return false;
        }

        point = new double[axisIds.Length];
        for (var i = 0; i < axisIds.Length; i++)
        {
            point[i] = _api.GetFPosition(ToAcsAxis(axisIds[i]));
            if (!IsFinite(point[i]))
            {
                Console.WriteLine($"ACS XSEG start position for axis {axisIds[i]} is invalid.");
                return false;
            }
        }

        return true;
    }

    private void StartXseg(Axis[] axes, double[] startPoint, double velocity)
    {
        _api.ExtendedSegmentedMotionV2(
            GetVelocityFlag(velocity),
            EnsureAcsAxisTerminator(axes),
            startPoint,
            GetOptionalVelocity(velocity),
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            null,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE);
    }

    private void AddXsegLine(Axis[] axes, double[] target, double velocity)
    {
        _api.SegmentLineV2(
            GetVelocityFlag(velocity),
            EnsureAcsAxisTerminator(axes),
            target,
            GetOptionalVelocity(velocity),
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            null,
            null,
            Api.ACSC_NONE,
            null,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE);
    }

    private void AddXsegArc1(Axis[] axes, double[] center, double[] finalPoint, RotationDirection rotation, double velocity)
    {
        _api.SegmentArc1V2(
            GetVelocityFlag(velocity),
            EnsureAcsAxisTerminator(axes),
            center,
            finalPoint,
            rotation,
            GetOptionalVelocity(velocity),
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            null,
            null,
            Api.ACSC_NONE,
            null,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE,
            Api.ACSC_NONE);
    }

    private void EndXseg(Axis[] axes)
    {
        _api.EndSequenceM(EnsureAcsAxisTerminator(axes));
    }

    private static Axis[] EnsureAcsAxisTerminator(Axis[] axes)
    {
        if (axes.Length > 0 && axes[^1] == Axis.ACSC_NONE)
        {
            return axes;
        }

        return axes.Concat(new[] { Axis.ACSC_NONE }).ToArray();
    }

    private static MotionFlags GetVelocityFlag(double velocity)
    {
        return velocity > 0d ? MotionFlags.ACSC_AMF_VELOCITY : MotionFlags.ACSC_NONE;
    }

    private static double GetOptionalVelocity(double velocity)
    {
        return velocity > 0d ? Math.Abs(velocity) : Api.ACSC_NONE;
    }
}
```

- [ ] **Step 2: Verify RED still points to missing public methods**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

Expected: build succeeds, test run still fails because `LineInterpoMovingXseg` and `ArcInterpoMovingXseg` are not implemented.

---

### Task 3: Add XSEG Line Interpolation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`

- [ ] **Step 1: Add `LineInterpoMovingXseg`**

Insert this method after the existing `LineInterpoMoving(LineInterPoParam param)` method and before `TryBuildInterpolationMove(...)`:

```csharp
public bool LineInterpoMovingXseg(LineInterPoParam param)
{
    if (!IsConnected || param == null)
    {
        return false;
    }

    try
    {
        if (!TryBuildInterpolationMove(param.InterPoAxiss, param.TargetPosDic, param.TargetPos, out var axisIds, out var axes, out var target))
        {
            return false;
        }

        if (!PrepareInterpolationAxes(axisIds, axes))
        {
            return false;
        }

        ConfigureInterpolationAxes(axisIds, param.DefaultSpeed);
        if (!TryGetCurrentInterpolationPoint(axisIds, out var startPoint))
        {
            return false;
        }

        var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
        StartXseg(axes, startPoint, velocity);
        AddXsegLine(axes, target, velocity);
        EndXseg(axes);

        if (param.waitforend && !WaitUntilAllAxesStopped(axisIds, InternalTimeout))
        {
            return false;
        }

        UpdateAxisStates(axisIds);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ACS LineInterpoMovingXseg failed: {ex.Message}");
        return false;
    }
}
```

- [ ] **Step 2: Verify line test passes and arc test still fails**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

Expected: build succeeds, the test run advances past the XSEG line checks and fails on missing `ArcInterpoMovingXseg`.

---

### Task 4: Add XSEG Arc Interpolation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`

- [ ] **Step 1: Add `ArcInterpoMovingXseg`**

Insert this method after the existing `ArcInterpoMoving(ArcInterPoParam param)` method and before `TryResolveArcCenter(...)`:

```csharp
public bool ArcInterpoMovingXseg(ArcInterPoParam param)
{
    if (!IsConnected || param == null)
    {
        return false;
    }

    try
    {
        var axisIds = (param.InterPoAxiss == null || param.InterPoAxiss.Count == 0)
            ? new[] { En_AxisNum.X, En_AxisNum.Y }
            : param.InterPoAxiss.Take(2).ToArray();

        if (axisIds.Length != 2 || !TryBuildAxes(axisIds, out var axes))
        {
            return false;
        }

        var targets = new Dictionary<En_AxisNum, double>
        {
            [axisIds[0]] = param.Destination.X,
            [axisIds[1]] = param.Destination.Y
        };

        if (param.FinalPosDic != null)
        {
            foreach (var targetPosition in param.FinalPosDic)
            {
                targets[targetPosition.Key] = targetPosition.Value;
            }
        }

        if (!ValidateLimitPosition(targets, out var limitMessage))
        {
            Console.WriteLine($"ACS ArcInterpoMovingXseg limit validation failed: {limitMessage}");
            return false;
        }

        if (!TryResolveArcCenter(param, out var center))
        {
            return false;
        }

        if (!PrepareInterpolationAxes(axisIds, axes))
        {
            return false;
        }

        ConfigureInterpolationAxes(axisIds, param.DefaultSpeed);
        if (!TryGetCurrentInterpolationPoint(axisIds, out var startPoint))
        {
            return false;
        }

        var finalPoint = new[] { param.Destination.X, param.Destination.Y };
        var rotation = ToAcsRotation(param.Dir);
        var velocity = GetInterpolationVelocity(axisIds, param.DefaultSpeed);
        StartXseg(axes, startPoint, velocity);
        AddXsegArc1(axes, center, finalPoint, rotation, velocity);
        EndXseg(axes);

        if (param.waitforend && !WaitUntilAllAxesStopped(axisIds, InternalTimeout))
        {
            return false;
        }

        UpdateAxisStates(axisIds);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ACS ArcInterpoMovingXseg failed: {ex.Message}");
        return false;
    }
}
```

- [ ] **Step 2: Verify GREEN**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

Expected: all ACS tests pass. Existing external dependency warnings are acceptable if there are no build errors.

---

### Task 5: Final Verification

**Files:**
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/XsegInterpolation.cs`
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Confirm old and new line methods coexist**

Run:

```powershell
rg -n "LineInterpoMoving\\(|LineInterpoMovingXseg|_api\\.ExtLine\\(|_api\\.Line\\(|SegmentLineV2" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App'
```

Expected: `LineInterpoMoving` still contains `ExtLine` and `Line`; `LineInterpoMovingXseg` exists; `XsegInterpolation.cs` contains `SegmentLineV2`.

- [ ] **Step 2: Confirm ACS-specific APIs did not leak into shared interfaces**

Run:

```powershell
rg -n "LineInterpoMovingXseg|ArcInterpoMovingXseg" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs' 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs'
```

Expected: no output.

- [ ] **Step 3: Run final tests**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
dotnet run --project 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj' --no-build
```

Expected: build succeeds and tests pass.

---

## Self-Review

- Spec coverage: Tasks 1-5 cover preserving basic interpolation, adding XSEG line, adding XSEG arc, keeping shared interfaces unchanged, and verifying behavior.
- Placeholder scan: no unfinished markers or omitted implementation steps.
- Type consistency: method names match the approved design: `LineInterpoMovingXseg`, `ArcInterpoMovingXseg`, `StartXseg`, `AddXsegLine`, `AddXsegArc1`, and `EndXseg`.
- Scope check: plan only modifies ACS interpolation code and ACS tests; it does not add LCI configuration, UI controls, or shared interface changes.

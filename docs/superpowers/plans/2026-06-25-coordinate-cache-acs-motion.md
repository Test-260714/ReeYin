# CoordinateCache ACS Motion Adaptation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make CoordinateCache "move to this position" route through the neutral coordinated-motion capability for supported cards, including ACS, while preserving the legacy fallback path for cards without that capability.

**Architecture:** `CoordinateCacheViewModel` keeps the UI command flow and validation, then delegates X/Y movement to a focused helper. The helper prefers `ICoordinatedMotionCard.MoveCoordinated(Line)` when the active card advertises coordinated motion, otherwise it keeps the existing `CustomInterpolationMoving + LineInterpoMoving` sequence.

**Tech Stack:** C# 12, .NET 8 WPF, Prism `DelegateCommand`, existing ReeYin-V control-card abstractions, lightweight console test harness in `ReeYin_V.Hardware.ControlCard.ACS.Tests`.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Adds a source-level regression test that fails until `CoordinateCacheViewModel` uses the coordinated-motion capability.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/CoordinateCacheViewModel.cs`
  - Adds small helper methods for line/custom movement parameter construction and target movement routing.
  - Replaces the inline X/Y move body in the target-move command with the helper call.
- No changes: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/*`
  - ACS already implements `ICoordinatedMotionCard`; this task must not add ACS project dependencies to the base control-card project.

## Task 1: Add Failing Regression Test

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add the failing test registration**

Insert this run registration after the existing `CoordinateCache` registrations:

```csharp
Run("CoordinateCache view model uses coordinated motion capability before fallback", TestCoordinateCacheViewModelUsesCoordinatedMotionCapability);
```

The nearby block should read:

```csharp
Run("CoordinateCache view model derives coordinate values from configured axes", TestCoordinateCacheViewModelDerivesValuesFromConfiguredAxes);
Run("CoordinateCache view model guards selection and caches commands", TestCoordinateCacheViewModelGuardsSelectionAndCachesCommands);
Run("CoordinateCache view model uses coordinated motion capability before fallback", TestCoordinateCacheViewModelUsesCoordinatedMotionCapability);
```

- [ ] **Step 2: Add the failing test method**

Insert this method after `TestCoordinateCacheViewModelGuardsSelectionAndCachesCommands()`:

```csharp
void TestCoordinateCacheViewModelUsesCoordinatedMotionCapability()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\CoordinateCacheViewModel.cs");

    AssertContains(source, "private bool TryMovePlanarAxesToTarget(",
        "CoordinateCache should isolate target movement routing in a helper");
    AssertContains(source, "controlCard is ICoordinatedMotionCard coordinatedMotionCard",
        "CoordinateCache should use neutral coordinated motion capability detection");
    AssertContains(source, "coordinatedMotionCard.SupportsCoordinatedMotion",
        "CoordinateCache should only use coordinated motion when the card advertises support");
    AssertContains(source, "coordinatedMotionCard.MoveCoordinated(new CoordinatedMotionRequest",
        "CoordinateCache should move coordinated-capable cards through MoveCoordinated");
    AssertContains(source, "Kind = CoordinatedMotionKind.Line",
        "CoordinateCache coordinated move should request a line move");
    AssertContains(source, "LineParam = lineParam",
        "CoordinateCache should pass the same line interpolation parameters to capable cards");
    AssertContains(source, "CustomInterpolationMoving(CreateCustomInterpolationParam",
        "CoordinateCache should keep the legacy custom-interpolation fallback");
    AssertContains(source, "LineInterpoMoving(lineParam)",
        "CoordinateCache fallback should still execute the line interpolation command");
    AssertFalse(source.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
        "CoordinateCache should not reference the ACS project namespace");
    AssertFalse(source.Contains("AcsControlCard", StringComparison.Ordinal),
        "CoordinateCache should not hard-code the ACS concrete control-card type");
}
```

- [ ] **Step 3: Run the test harness and verify RED**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected result:

```text
FAIL: CoordinateCache view model uses coordinated motion capability before fallback
CoordinateCache should isolate target movement routing in a helper
```

If the run fails earlier because assets are not restored, run this instead and still expect the same CoordinateCache assertion failure after restore/build completes:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

## Task 2: Implement CoordinateCache Movement Routing

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/CoordinateCacheViewModel.cs`

- [ ] **Step 1: Add the planar-axis field**

Add this field in the `#region Fields` block after `_generalCommand`:

```csharp
private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];
```

- [ ] **Step 2: Add shared movement parameter helpers**

Add these methods after `BuildTargetPositionDictionary(...)`:

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

- [ ] **Step 3: Add the coordinated/fallback routing helper**

Add this method after the helpers from Step 2:

```csharp
private bool TryMovePlanarAxesToTarget(
    IControlCard controlCard,
    Dictionary<En_AxisNum, double> targetPositions,
    double[] targetPosArray)
{
    var lineParam = CreateLineInterpolationParam(targetPositions, targetPosArray);
    if (controlCard is ICoordinatedMotionCard coordinatedMotionCard &&
        coordinatedMotionCard.SupportsCoordinatedMotion)
    {
        if (!coordinatedMotionCard.MoveCoordinated(new CoordinatedMotionRequest
        {
            Kind = CoordinatedMotionKind.Line,
            Axes = PlanarMoveAxes.ToList(),
            TargetPositions = new Dictionary<En_AxisNum, double>(targetPositions),
            WaitForEnd = true,
            LineParam = lineParam,
        }, out var message))
        {
            Console.WriteLine($"CoordinateCache coordinated move failed: {message}");
            return false;
        }

        return true;
    }

    if (!controlCard.CustomInterpolationMoving(
        CreateCustomInterpolationParam(targetPositions, targetPosArray),
        () => controlCard.LineInterpoMoving(lineParam) ? "OK" : "NG",
        true))
    {
        Console.WriteLine("CoordinateCache custom interpolation move failed.");
        return false;
    }

    return true;
}
```

- [ ] **Step 4: Replace the inline X/Y move block**

Inside the target-move command task body, replace the existing `CustomInterpolationMoving(...)` block and the following `MoveAdditionalAxes(position, axes);` call with:

```csharp
if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))
{
    return;
}

MoveAdditionalAxes(position, axes);
```

The full task body should become:

```csharp
_localTask = Task.Run(() =>
{
    if (controlCard == null)
    {
        return;
    }

    if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))
    {
        return;
    }

    MoveAdditionalAxes(position, axes);
});
```

- [ ] **Step 5: Run the test harness and verify GREEN**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected result:

```text
ACS PEG/DataCollection tests passed.
```

## Task 3: Build And Final Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ReeYin_V.Hardware.ControlCard.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Build the base control-card project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj --no-restore
```

Expected result:

```text
Build succeeded.
```

- [ ] **Step 2: Build the ACS test harness**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected result:

```text
Build succeeded.
```

- [ ] **Step 3: Run the ACS test harness once more**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected result:

```text
ACS PEG/DataCollection tests passed.
```

- [ ] **Step 4: Check repository status or record the git limitation**

Run:

```powershell
git status --short
```

Expected in a healthy checkout: a short list containing only the plan, spec, test, and `CoordinateCacheViewModel` changes.

Expected in the current observed checkout: `fatal: not a git repository (or any of the parent directories): .git`. If that fatal error persists, leave the changes uncommitted and report that Git metadata is not usable from this workspace.

- [ ] **Step 5: Commit only if Git is usable**

Run this only when `git status --short` succeeds:

```powershell
git add docs/superpowers/specs/2026-06-25-coordinate-cache-acs-motion-design.md docs/superpowers/plans/2026-06-25-coordinate-cache-acs-motion.md Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/CoordinateCacheViewModel.cs
git commit -m "fix: route coordinate cache moves through coordinated cards"
```

Expected result:

```text
[branch commit] fix: route coordinate cache moves through coordinated cards
```

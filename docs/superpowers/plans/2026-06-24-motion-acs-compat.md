# HardwareTool.Motion ACS Compatibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `HardwareTool.Motion` run common point, line, arc, IO, and position-comparison operations through ACS cards via `ControlCardBase` only.

**Architecture:** Keep `HardwareTool.Motion` independent from `ReeYin_V.Hardware.ControlCard.ACS`. Refactor `MotionModel` so it builds target-position maps from the selected card's configured axes, then dispatches focused point, line, arc, IO, delay, and event helpers through the existing shared control-card API.

**Tech Stack:** C#/.NET 8 WPF module, Prism MVVM, `ControlCardBase`, existing ACS source-level console tests.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Add source-level tests that guard the Motion/ACS compatibility contract without needing real hardware.
- Modify: `Tools/Hardware/HardwareTool.Motion/Models/MotionModel.cs`
  - Add axis-aware target-map helpers.
  - Add focused movement execution helpers.
  - Update arc parameter construction to include interpolation axes and final target positions.
  - Replace repeated inline execution blocks in `ExecuteModule()` and `CustomMoving()`.
- No change: `Tools/Hardware/HardwareTool.Motion/HardwareTool.Motion.csproj`
  - Must continue to reference only the shared control-card project.
- No change: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj`
  - ACS already implements the shared methods Motion needs.

## Scope Check

The spec targets one subsystem: the Motion module's shared control-card execution path. ACS internals, homing, LCI, PEG, buffer scripts, and UI redesign are out of scope.

### Task 1: Add Failing Compatibility Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add test registrations**

Add these `Run(...)` calls after the existing ACS interpolation registrations:

```csharp
Run("Motion tool keeps ACS dependency out of project references", TestMotionToolKeepsAcsDependencyOut);
Run("Motion model exposes axis-aware target mapping helpers", TestMotionModelAxisAwareTargetMappingHelpers);
Run("Motion arc execution passes complete ACS-compatible parameters", TestMotionArcExecutionPassesCompleteParameters);
Run("Motion execution uses focused movement dispatch helpers", TestMotionExecutionUsesFocusedMovementDispatch);
```

- [ ] **Step 2: Add source-level test functions**

Add these functions near the other source-level tests:

```csharp
void TestMotionToolKeepsAcsDependencyOut()
{
    var csproj = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\HardwareTool.Motion.csproj");

    AssertContains(
        csproj,
        @"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj",
        "Motion should reference the shared control-card abstraction");
    AssertFalse(
        csproj.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
        "Motion should not reference the ACS project directly");
}

void TestMotionModelAxisAwareTargetMappingHelpers()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");

    AssertContains(source, "private bool TryBuildTargetPositionMap(", "MotionModel should expose target map helper");
    AssertContains(source, "ControlCard.Config.AllAxis", "target map helper should use selected card axis config");
    AssertContains(source, "axis.AxisNo - 1", "target map helper should map TargetPos by configured physical axis number");
    AssertContains(source, "private List<En_AxisNum> GetInterpolationAxes()", "MotionModel should resolve interpolation axes centrally");
    AssertFalse(
        source.Contains("MovementLocus.AssignPosInfo.TargetPos[3]", StringComparison.Ordinal),
        "MotionModel should not hard-code Z1 target position index");
    AssertFalse(
        source.Contains("MovementLocus.AssignPosInfo.TargetPos[4]", StringComparison.Ordinal),
        "MotionModel should not hard-code Z2 target position index");
}

void TestMotionArcExecutionPassesCompleteParameters()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");
    var createArcBody = ReadMethodBody(source, "private ArcInterPoParam CreateArcInterPoParam(");
    var executeArcBody = ReadMethodBody(source, "private bool ExecuteArcInterpolationSequence(");

    AssertContains(createArcBody, "InterPoAxiss = GetInterpolationAxes()", "arc params should pass selected interpolation axes");
    AssertContains(createArcBody, "FinalPosDic = finalPositions", "arc params should pass final positions for ACS limit checks");
    AssertContains(executeArcBody, "TryBuildPlanarTargetPositionMap", "arc execution should build final target positions centrally");
    AssertContains(executeArcBody, "CreateMoveToCircleParam", "arc move-to-circle step should share line parameter construction");
}

void TestMotionExecutionUsesFocusedMovementDispatch()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");
    var executeBody = ReadMethodBody(source, "public async Task<ExecuteModuleOutput> ExecuteModule()");
    var customMovingBody = ReadMethodBody(source, "public void CustomMoving(MovementLocus movementLocus)");

    AssertContains(source, "private bool ExecuteMovementLocus(MovementLocus movementLocus)", "MotionModel should dispatch each movement through one helper");
    AssertContains(source, "private bool ExecutePointMovement(MovementLocus movementLocus)", "MotionModel should have focused point helper");
    AssertContains(source, "private bool ExecuteLineSegmentMovement(MovementLocus movementLocus)", "MotionModel should have focused line helper");
    AssertContains(source, "private bool ExecuteArcSegmentMovement(MovementLocus movementLocus)", "MotionModel should have focused arc helper");
    AssertContains(executeBody, "ExecuteMovementLocus(MovementLocus)", "module execution should use focused movement dispatch");
    AssertContains(customMovingBody, "ExecuteMovementLocus(movementLocus)", "manual execution should use the same movement dispatch");
    AssertFalse(executeBody.Contains("new CustomInterPoParam", StringComparison.Ordinal), "module execution should not duplicate custom interpolation construction");
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Expected: command builds, then fails on at least `MotionModel should expose target map helper`.

- [ ] **Step 4: Commit test changes**

Run:

```powershell
git status --short
git add "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs"
git commit -m "test: cover motion acs compatibility contract"
```

Expected when Git metadata is valid: one commit is created. Current workspace may return `fatal: not a git repository`; if so, continue implementation and report that commits could not be created.

### Task 2: Add Axis-Aware Target and Request Helpers

**Files:**
- Modify: `Tools/Hardware/HardwareTool.Motion/Models/MotionModel.cs`

- [ ] **Step 1: Add helper methods after `RefreshControlCardContext()`**

Insert this code after `RefreshControlCardContext()`:

```csharp
        private List<En_AxisNum> GetInterpolationAxes()
        {
            var configuredAxes = ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .Select(axis => axis.AxisNum)
                .Distinct()
                .ToHashSet() ?? new HashSet<En_AxisNum>();

            if (configuredAxes.Count == 0)
            {
                return [En_AxisNum.X, En_AxisNum.Y];
            }

            var defaultAxes = ControlCard?.Config?.DefaultInterpCS?.InterPoAxiss?
                .Where(configuredAxes.Contains)
                .Distinct()
                .Take(2)
                .ToList();

            if (defaultAxes != null && defaultAxes.Count >= 2)
            {
                return defaultAxes;
            }

            var xyAxes = new[] { En_AxisNum.X, En_AxisNum.Y }
                .Where(configuredAxes.Contains)
                .ToList();

            if (xyAxes.Count >= 2)
            {
                return xyAxes;
            }

            return configuredAxes.Take(2).ToList();
        }

        private bool TryBuildTargetPositionMap(
            MovementLocus movementLocus,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            if (movementLocus == null)
            {
                targetPositions = new Dictionary<En_AxisNum, double>();
                errorMessage = "运动轨迹不能为空。";
                return false;
            }

            return TryBuildTargetPositionMap(movementLocus.AssignPosInfo, out targetPositions, out errorMessage);
        }

        private bool TryBuildTargetPositionMap(
            CoordinatePos coordinatePos,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            targetPositions = new Dictionary<En_AxisNum, double>();
            errorMessage = string.Empty;

            var axes = ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .OrderBy(axis => axis.AxisNo)
                .ToList();

            if (axes == null || axes.Count == 0)
            {
                errorMessage = "控制卡未配置运动轴。";
                return false;
            }

            if (coordinatePos?.TargetPos == null)
            {
                errorMessage = "目标坐标不能为空。";
                return false;
            }

            foreach (var axis in axes)
            {
                var targetIndex = axis.AxisNo - 1;
                if (targetIndex < 0)
                {
                    errorMessage = $"{axis.AxisNum}轴物理轴号无效。";
                    return false;
                }

                if (targetIndex >= coordinatePos.TargetPos.Count)
                {
                    errorMessage = $"{axis.AxisNum}轴缺少目标坐标。";
                    return false;
                }

                var targetPosition = coordinatePos.TargetPos[targetIndex];
                if (!IsFinite(targetPosition))
                {
                    errorMessage = $"{axis.AxisNum}轴目标坐标存在非法数值。";
                    return false;
                }

                targetPositions[axis.AxisNum] = targetPosition;
            }

            if (targetPositions.Count == 0)
            {
                errorMessage = "目标坐标映射为空。";
                return false;
            }

            return true;
        }

        private bool TryBuildPlanarTargetPositionMap(
            MovementLocus movementLocus,
            double firstAxisPosition,
            double secondAxisPosition,
            out Dictionary<En_AxisNum, double> targetPositions,
            out string errorMessage)
        {
            if (!TryBuildTargetPositionMap(movementLocus, out targetPositions, out errorMessage))
            {
                return false;
            }

            return TryApplyPlanarTargetPositions(targetPositions, firstAxisPosition, secondAxisPosition, out errorMessage);
        }

        private bool TryApplyPlanarTargetPositions(
            Dictionary<En_AxisNum, double> targetPositions,
            double firstAxisPosition,
            double secondAxisPosition,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            var interpolationAxes = GetInterpolationAxes();
            if (interpolationAxes.Count < 2)
            {
                errorMessage = "控制卡至少需要配置两个插补轴。";
                return false;
            }

            if (!IsFinite(firstAxisPosition) || !IsFinite(secondAxisPosition))
            {
                errorMessage = "插补目标坐标存在非法数值。";
                return false;
            }

            targetPositions[interpolationAxes[0]] = firstAxisPosition;
            targetPositions[interpolationAxes[1]] = secondAxisPosition;
            return true;
        }

        private double[] BuildTargetPositionArray(IReadOnlyDictionary<En_AxisNum, double> targetPositions)
        {
            return ControlCard?.Config?.AllAxis?
                .Where(axis => axis != null)
                .OrderBy(axis => axis.AxisNo)
                .Select(axis => targetPositions.TryGetValue(axis.AxisNum, out var position) ? position : axis.CurPos)
                .ToArray() ?? Array.Empty<double>();
        }

        private CustomInterPoParam CreateCustomInterpolationParam(
            Dictionary<En_AxisNum, double> finalPositions,
            bool waitForEnd = true)
        {
            return new CustomInterPoParam
            {
                InterPoAxiss = GetInterpolationAxes(),
                TargetPos = BuildTargetPositionArray(finalPositions),
                TargetPosDic = finalPositions,
                waitforend = waitForEnd
            };
        }

        private LineInterPoParam CreateLineInterpolationParam(
            Dictionary<En_AxisNum, double> targetPositions,
            bool waitForEnd = true)
        {
            return new LineInterPoParam
            {
                InterPoAxiss = GetInterpolationAxes(),
                TargetPos = BuildTargetPositionArray(targetPositions),
                TargetPosDic = targetPositions,
                decZSpeed = [5, 10, 50],
                upZSpeed = [5, 10, 50],
                waitforend = waitForEnd
            };
        }
```

- [ ] **Step 2: Run targeted tests and verify the first helper test advances**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Expected: the helper-name assertions pass, but tests still fail because arc and dispatch helpers are not implemented.

- [ ] **Step 3: Commit helper changes**

Run:

```powershell
git status --short
git add "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs"
git commit -m "feat: map motion targets from configured axes"
```

Expected when Git metadata is valid: one commit is created. If Git returns `fatal: not a git repository`, record that in final verification.

### Task 3: Make Arc Parameters ACS-Compatible

**Files:**
- Modify: `Tools/Hardware/HardwareTool.Motion/Models/MotionModel.cs`

- [ ] **Step 1: Replace arc helper signatures and bodies**

Replace `ExecuteArcInterpolationSequence`, `CreateArcInterPoParam`, and `CreateMoveToCircleParam` with this code:

```csharp
        private bool ExecuteArcInterpolationSequence(MovementLocus movementLocus)
        {
            if (ControlCard == null)
            {
                Logs.LogWarning("执行圆弧失败：未找到可用的控制卡。");
                return false;
            }

            if (!TryBuildArcExecutionPlan(movementLocus, out var executionPlan, out string errorMessage))
            {
                Logs.LogWarning($"圆弧参数非法，不符合要求：{errorMessage}");
                return false;
            }

            if (!TryBuildPlanarTargetPositionMap(
                    movementLocus,
                    executionPlan.DestinationPoint.X,
                    executionPlan.DestinationPoint.Y,
                    out var finalPositions,
                    out errorMessage))
            {
                Logs.LogWarning($"圆弧目标坐标非法：{errorMessage}");
                return false;
            }

            if (executionPlan.RequiresMoveToCircle &&
                !ControlCard.LineInterpoMoving(CreateMoveToCircleParam(executionPlan, finalPositions)))
            {
                Logs.LogWarning($"当前位置未在目标圆上，移动到最近圆上点失败：{FormatPoint(executionPlan.ArcStartPoint)}。");
                return false;
            }

            return ControlCard.ArcInterpoMoving(CreateArcInterPoParam(executionPlan, finalPositions));
        }

        private ArcInterPoParam CreateArcInterPoParam(
            ArcExecutionPlan executionPlan,
            Dictionary<En_AxisNum, double> finalPositions)
        {
            return new ArcInterPoParam
            {
                DrawArcMethod = executionPlan.DrawArcMethod,
                InterPoAxiss = GetInterpolationAxes(),
                Origin = executionPlan.ArcStartPoint,
                Destination = executionPlan.DestinationPoint,
                Center = executionPlan.CenterPoint,
                Radius = executionPlan.Radius,
                Dir = executionPlan.Direction,
                FinalPosDic = finalPositions,
                waitforend = true,
            };
        }

        private LineInterPoParam CreateMoveToCircleParam(
            ArcExecutionPlan executionPlan,
            Dictionary<En_AxisNum, double> finalPositions)
        {
            var moveToCirclePositions = new Dictionary<En_AxisNum, double>(finalPositions);
            if (!TryApplyPlanarTargetPositions(
                    moveToCirclePositions,
                    executionPlan.ArcStartPoint.X,
                    executionPlan.ArcStartPoint.Y,
                    out string errorMessage))
            {
                Logs.LogWarning($"圆弧起点修正坐标非法：{errorMessage}");
            }

            return CreateLineInterpolationParam(moveToCirclePositions, true);
        }
```

- [ ] **Step 2: Run targeted tests and verify arc test passes**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Expected: arc parameter assertions pass, but dispatch tests still fail.

- [ ] **Step 3: Commit arc changes**

Run:

```powershell
git status --short
git add "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs"
git commit -m "feat: pass complete arc parameters to control cards"
```

Expected when Git metadata is valid: one commit is created. If Git returns `fatal: not a git repository`, record that in final verification.

### Task 4: Replace Inline Execution With Focused Dispatch Helpers

**Files:**
- Modify: `Tools/Hardware/HardwareTool.Motion/Models/MotionModel.cs`

- [ ] **Step 1: Add focused movement helpers before `ExecuteModule()`**

Insert this code before `ExecuteModule()`:

```csharp
        private bool ExecuteMovementLocus(MovementLocus movementLocus)
        {
            return movementLocus.MovingMode switch
            {
                CardOperaion.点 => ExecutePointMovement(movementLocus),
                CardOperaion.IO => ExecuteIoOperation(movementLocus),
                CardOperaion.直线线段 => ExecuteLineSegmentMovement(movementLocus),
                CardOperaion.圆弧线段 => ExecuteArcSegmentMovement(movementLocus),
                CardOperaion.位置比较 => true,
                CardOperaion.延时 => ExecuteDelayOperation(movementLocus),
                CardOperaion.触发事件 => ExecuteTriggerEventOperation(movementLocus),
                CardOperaion.自定义 => true,
                _ => true
            };
        }

        private bool ExecutePointMovement(MovementLocus movementLocus)
        {
            if (!TryBuildTargetPositionMap(movementLocus, out var targetPositions, out string errorMessage))
            {
                Logs.LogWarning($"点位运动目标坐标非法：{errorMessage}");
                return false;
            }

            return ExecuteLineWithCustomInterpolation(targetPositions, true);
        }

        private bool ExecuteLineSegmentMovement(MovementLocus movementLocus)
        {
            if (!TryBuildPlanarTargetPositionMap(
                    movementLocus,
                    movementLocus.OriginX,
                    movementLocus.OriginY,
                    out var originPositions,
                    out string errorMessage))
            {
                Logs.LogWarning($"直线起点坐标非法：{errorMessage}");
                return false;
            }

            if (!ExecuteLineWithCustomInterpolation(originPositions, true))
            {
                Logs.LogWarning("移动至直线起点失败。");
                return false;
            }

            Task.Delay(5).Wait();
            var posCompareEnabled = false;
            try
            {
                if (movementLocus.IsUsingValid)
                {
                    movementLocus.Switch = true;
                    if (!SwitchPosCompare(movementLocus))
                    {
                        return false;
                    }

                    posCompareEnabled = true;
                }

                if (!TryBuildPlanarTargetPositionMap(
                        movementLocus,
                        movementLocus.DestinationX,
                        movementLocus.DestinationY,
                        out var destinationPositions,
                        out errorMessage))
                {
                    Logs.LogWarning($"直线终点坐标非法：{errorMessage}");
                    return false;
                }

                return ExecuteLineWithCustomInterpolation(destinationPositions, true);
            }
            finally
            {
                if (posCompareEnabled)
                {
                    movementLocus.Switch = false;
                    SwitchPosCompare(movementLocus);
                }
            }
        }

        private bool ExecuteArcSegmentMovement(MovementLocus movementLocus)
        {
            if (!TryValidateArcMovement(movementLocus, out string errorMessage))
            {
                Logs.LogWarning($"输入的圆弧参数非法，不符合要求：{errorMessage}");
                return false;
            }

            if (!TryBuildTargetPositionMap(movementLocus, out var targetPositions, out errorMessage))
            {
                Logs.LogWarning($"圆弧目标坐标非法：{errorMessage}");
                return false;
            }

            return ControlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions),
                () => ExecuteArcInterpolationSequence(movementLocus) ? "OK" : "NG",
                true);
        }

        private bool ExecuteLineWithCustomInterpolation(
            Dictionary<En_AxisNum, double> targetPositions,
            bool waitForEnd)
        {
            return ControlCard.CustomInterpolationMoving(
                CreateCustomInterpolationParam(targetPositions, waitForEnd),
                () => ControlCard.LineInterpoMoving(CreateLineInterpolationParam(targetPositions, waitForEnd)) ? "OK" : "NG",
                waitForEnd);
        }

        private bool ExecuteIoOperation(MovementLocus movementLocus)
        {
            if (movementLocus.OutputIODelay == 0)
            {
                return ControlCard.SetSpecifiedIO(movementLocus.OutputIO, movementLocus.OutputIOStatus);
            }

            Task.Run(() =>
            {
                Task.Delay(movementLocus.OutputIODelay).Wait();
                if (!ControlCard.SetSpecifiedIO(movementLocus.OutputIO, !movementLocus.OutputIOStatus))
                {
                    Logs.LogWarning($"延时设置IO{movementLocus.OutputIO}失败。");
                }
            });

            return true;
        }

        private static bool ExecuteDelayOperation(MovementLocus movementLocus)
        {
            Task.Delay(movementLocus.OutputIODelay * 1000).Wait();
            return true;
        }

        private static bool ExecuteTriggerEventOperation(MovementLocus movementLocus)
        {
            PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish(movementLocus.EventName);
            return true;
        }
```

- [ ] **Step 2: Replace `ExecuteModule()` movement switch**

Inside `ExecuteModule()`, replace the entire nested `switch (MovementLocus.MovingMode)` block with:

```csharp
                                        if (!ExecuteMovementLocus(MovementLocus))
                                        {
                                            return NodeStatus.Error;
                                        }
```

The surrounding loop remains:

```csharp
                                foreach (var MovementLocus in MovementLocuss)
                                {
                                    if (MovementLocus.IsUsing)
                                    {
                                        if (!ExecuteMovementLocus(MovementLocus))
                                        {
                                            return NodeStatus.Error;
                                        }
                                    }
                                }
```

- [ ] **Step 3: Replace `CustomMoving()` switch**

Replace the entire `switch (movementLocus.MovingMode)` block in `CustomMoving()` with:

```csharp
            if (!ExecuteMovementLocus(movementLocus))
            {
                MessageBox.Show("运动执行失败，请检查控制卡状态和运动参数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
```

- [ ] **Step 4: Run tests to verify all source-level compatibility tests pass**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Expected: all ACS test harness tests pass, including the four Motion compatibility tests.

- [ ] **Step 5: Commit dispatch refactor**

Run:

```powershell
git status --short
git add "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs"
git commit -m "refactor: route motion execution through shared helpers"
```

Expected when Git metadata is valid: one commit is created. If Git returns `fatal: not a git repository`, record that in final verification.

### Task 5: Build and Clean Up

**Files:**
- Verify: `Tools/Hardware/HardwareTool.Motion/HardwareTool.Motion.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`
- Inspect: `Tools/Hardware/HardwareTool.Motion/Models/MotionModel.cs`
- Inspect: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Build Motion project**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet build "Tools\Hardware\HardwareTool.Motion\HardwareTool.Motion.csproj" --no-restore -p:SolutionDir="$solutionDir" -v:minimal
```

Expected: build succeeds. Existing `halcondotnet` warnings from `ReeYin_V.Share` are acceptable if there are no errors.

- [ ] **Step 2: Run ACS test harness**

Run:

```powershell
$solutionDir = "$(Get-Location)\"
dotnet run --project "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-restore -p:SolutionDir="$solutionDir"
```

Expected: output ends with `ACS PEG/DataCollection tests passed.` and every new Motion compatibility test reports `PASS`.

- [ ] **Step 3: Scan for old hard-coded five-axis blocks**

Run:

```powershell
rg -n "MovementLocus\.AssignPosInfo\.TargetPos\[[34]\]|new CustomInterPoParam" "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs"
```

Expected: no `MovementLocus.AssignPosInfo.TargetPos[3]`, no `MovementLocus.AssignPosInfo.TargetPos[4]`, and no duplicated inline `new CustomInterPoParam` in `ExecuteModule()`.

- [ ] **Step 4: Review diff**

Run:

```powershell
git diff -- "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs"
```

Expected: diff is limited to source tests and MotionModel helper/dispatch refactor. If Git is unavailable, use:

```powershell
Get-Content "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs" | Select-String -Pattern "TryBuildTargetPositionMap|ExecuteMovementLocus|CreateArcInterPoParam|FinalPosDic"
Get-Content "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs" | Select-String -Pattern "Motion tool keeps ACS dependency|TestMotionModelAxisAwareTargetMappingHelpers"
```

Expected: the new helper and test names are present.

- [ ] **Step 5: Final commit**

Run:

```powershell
git status --short
git add "Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs"
git commit -m "feat: make motion module compatible with acs cards"
```

Expected when Git metadata is valid: one commit is created or Git reports there is nothing new to commit because earlier task commits already captured all changes. If Git returns `fatal: not a git repository`, report that commits were skipped due to invalid repository metadata.

## Self-Review

- Spec coverage: Motion keeps ACS dependency out, builds target maps from configured axes, passes arc `InterPoAxiss` and `FinalPosDic`, refactors point/line/arc/IO/position comparison through `ControlCardBase`, and verifies with tests and builds.
- Red-flag scan: every task contains concrete code and commands.
- Type consistency: method names used by tests match the helper names added in tasks, and all helper signatures are defined before later tasks call them.

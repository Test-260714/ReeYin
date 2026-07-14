# Googol Startup Reset And Speed Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `ReeYin_V.Hardware.ControlCard.Googol` participate reliably in the existing control-card startup reset and AxisView speed-monitoring flows.

**Architecture:** The common control-card project already owns startup reset orchestration and AxisView speed polling. This work keeps those public entry points unchanged, hardens `GoogolControlCard.DoGoHome(out string message)` so startup reset can call it safely, and verifies the existing Googol encoder-speed implementation remains wired to AxisView through `GetAllSpeedInfos()`.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, ReeYin-V control-card abstraction, Googol GTN API wrapper, source-level regression tests in `ReeYin_V.Hardware.ControlCard.ACS.Tests`.

---

## Git State Note

`git -C E:\Company\工作目录\ReeYin-V\ReeYin-V status --short` currently returns `fatal: not a git repository (or any of the parent directories): .git`. Do not run commit commands until repository metadata is restored. Each task still lists the files that would be included in a future commit.

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`: add source-level regression tests for Googol startup reset and reuse the existing Googol speed-monitor assertions.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`: replace the current partial high-priority-only `DoGoHome` flow with an all-enabled-axis startup-safe flow and focused helpers.
- Review `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`: confirm `GetEncVel` still validates buffers and calls `GTN_GetEncVel`.
- Review `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`: confirm no vendor-specific branch is introduced.

### Task 1: Add Googol Startup Reset Regression Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add failing test registration**

Add this `Run` call near the existing Googol control-card tests:

```csharp
Run("Googol DoGoHome supports startup auto reset", TestGoogolDoGoHomeSupportsStartupAutoReset);
```

- [ ] **Step 2: Add failing test body**

Insert this function near `TestGoogolControlCardReadsEncoderSpeed()`:

```csharp
void TestGoogolDoGoHomeSupportsStartupAutoReset()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.cs");
    var doGoHomeBody = ReadMethodBody(source, "protected override bool DoGoHome(out string message)");

    AssertContains(doGoHomeBody, "if (!IsConnected)",
        "Googol startup reset should fail fast when the card is disconnected");
    AssertContains(doGoHomeBody, "var enabledAxes = Config?.AllAxis?",
        "Googol startup reset should only use configured enabled axes");
    AssertContains(doGoHomeBody, "axis.IsResetCompleted = false",
        "Googol startup reset should clear per-axis reset state before homing");
    AssertContains(doGoHomeBody, "IsHighPriorityHomeAxis",
        "Googol startup reset should preserve priority-based homing order");
    AssertContains(doGoHomeBody, "RunGoogolHomeBatch(highPriorityAxes, out message)",
        "Googol startup reset should home high-priority axes first");
    AssertContains(doGoHomeBody, "RunGoogolHomeBatch(normalPriorityAxes, out message)",
        "Googol startup reset should also home normal-priority enabled axes");
    AssertContains(doGoHomeBody, "FinalizeGoogolHome(enabledAxes, out message)",
        "Googol startup reset should rebind planner positions and restore speeds");
    AssertContains(doGoHomeBody, "axis.IsResetCompleted = true",
        "Googol startup reset should mark enabled axes as reset after success");
    AssertContains(doGoHomeBody, "IsNeedReset = false",
        "Googol startup reset should clear the reset-required flag after success");
    AssertContains(doGoHomeBody, "finally",
        "Googol startup reset should clean up reset state even when an exception occurs");
    AssertContains(doGoHomeBody, "IsReseting = false",
        "Googol startup reset should never leave the UI in a permanently resetting state");

    AssertContains(source, "private static bool IsHighPriorityHomeAxis(SingleAxisParam axis)",
        "Googol startup reset should isolate priority classification");
    AssertContains(source, "private bool PrepareGoogolHome(out string message)",
        "Googol startup reset should isolate alarm cleanup, enable, and stop checks");
    AssertContains(source, "private bool RunGoogolHomeBatch(IReadOnlyCollection<SingleAxisParam> axes, out string message)",
        "Googol startup reset should isolate batched homing command and wait logic");
    AssertContains(source, "private bool FinalizeGoogolHome(IReadOnlyCollection<SingleAxisParam> axes, out string message)",
        "Googol startup reset should isolate planner rebinding and speed restoration");
}
```

- [ ] **Step 3: Run the test and verify it fails**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL on missing `enabledAxes`, `RunGoogolHomeBatch`, `FinalizeGoogolHome`, `finally`, or helper methods in `GoogolControlCard.cs`.

- [ ] **Step 4: Review the test insertion**

Run:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs -Pattern "Googol DoGoHome supports startup auto reset|TestGoogolDoGoHomeSupportsStartupAutoReset"
```

Expected: both the `Run` call and test function are present.

### Task 2: Implement Startup-Safe Googol DoGoHome

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Replace `DoGoHome`**

Replace the entire `protected override bool DoGoHome(out string message)` method in `GoogolControlCard.cs` with:

```csharp
        protected override bool DoGoHome(out string message)
        {
            message = string.Empty;
            if (!IsConnected)
            {
                message = "固高控制卡未连接，无法执行复位。";
                return false;
            }

            var enabledAxes = Config?.AllAxis?
                .Where(axis => axis != null && axis.IsUsing)
                .ToArray();
            if (enabledAxes == null || enabledAxes.Length == 0)
            {
                message = "固高复位失败：未配置启用轴。";
                return false;
            }

            if (IsReseting)
            {
                message = "固高控制卡正在复位中。";
                return false;
            }

            IsReseting = true;
            foreach (var axis in enabledAxes)
            {
                axis.IsResetCompleted = false;
            }

            try
            {
                if (!PrepareGoogolHome(out message))
                {
                    return false;
                }

                var highPriorityAxes = enabledAxes.Where(IsHighPriorityHomeAxis).ToArray();
                var normalPriorityAxes = enabledAxes.Where(axis => !IsHighPriorityHomeAxis(axis)).ToArray();

                if (!RunGoogolHomeBatch(highPriorityAxes, out message))
                {
                    return false;
                }

                if (!RunGoogolHomeBatch(normalPriorityAxes, out message))
                {
                    return false;
                }

                if (!FinalizeGoogolHome(enabledAxes, out message))
                {
                    return false;
                }

                foreach (var axis in enabledAxes)
                {
                    axis.IsResetCompleted = true;
                }

                IsNeedReset = false;
                message = "固高控制卡复位完成。";
                return true;
            }
            catch (Exception ex)
            {
                message = $"固高复位异常：{ex.Message}";
                Console.WriteLine($"DoGoHome()_轴回零失败{ex}");
                return false;
            }
            finally
            {
                IsReseting = false;
            }
        }
```

- [ ] **Step 2: Add priority helper**

Add this helper immediately after `DoGoHome`:

```csharp
        private static bool IsHighPriorityHomeAxis(SingleAxisParam axis)
        {
            return axis.Priority == En_Priority.Top || axis.Priority == En_Priority.High;
        }
```

- [ ] **Step 3: Add preparation helper**

Add this helper after `IsHighPriorityHomeAxis`:

```csharp
        private bool PrepareGoogolHome(out string message)
        {
            message = string.Empty;

            if (!CleanAlarm())
            {
                message = "固高复位失败：清除轴报警失败。";
                return false;
            }

            if (!GetAxisClrSts(1, Config.AllAxis.Count, En_GetAxisClrSts.Bit9_MotorEnabled))
            {
                Console.WriteLine("轴无使能，重新上使能！！");
                if (!Motion.SetAxisEnabled(0, true))
                {
                    message = "固高复位失败：轴使能失败。";
                    return false;
                }

                foreach (var axis in Config.AllAxis)
                {
                    axis.IsEnable = true;
                }
            }

            if (!StopAxisMove())
            {
                message = "固高复位失败：停止轴运动失败。";
                return false;
            }

            return true;
        }
```

- [ ] **Step 4: Add batch homing helpers**

Add these helpers after `PrepareGoogolHome`:

```csharp
        private bool RunGoogolHomeBatch(IReadOnlyCollection<SingleAxisParam> axes, out string message)
        {
            message = string.Empty;
            if (axes.Count == 0)
            {
                return true;
            }

            foreach (var axis in axes)
            {
                if (!StartGoogolHomeAxis(axis, out message))
                {
                    return false;
                }
            }

            foreach (var axis in axes)
            {
                if (!WaitGoogolHomeAxis(axis, out message))
                {
                    return false;
                }
            }

            return true;
        }

        private bool StartGoogolHomeAxis(SingleAxisParam axis, out string message)
        {
            var offset = (int)ConvertToPluse(axis.AxisNum, axis.OriginOffset);
            if (!ResetAxis(axis.AxisNum, false, offset))
            {
                axis.IsResetCompleted = false;
                message = $"固高复位失败：{axis.AxisNum}轴回零命令下发失败。";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private bool WaitGoogolHomeAxis(SingleAxisParam axis, out string message)
        {
            if (!AxisWaitResetZero(axis.AxisNum))
            {
                axis.IsResetCompleted = false;
                message = $"固高复位失败：{axis.AxisNum}轴等待回零完成失败。";
                return false;
            }

            message = string.Empty;
            return true;
        }
```

- [ ] **Step 5: Add finalization helper**

Add this helper after `WaitGoogolHomeAxis`:

```csharp
        private bool FinalizeGoogolHome(IReadOnlyCollection<SingleAxisParam> axes, out string message)
        {
            foreach (var axis in axes)
            {
                if (!Motion.SetPrf(axis.AxisNo, 1))
                {
                    message = $"固高复位失败：{axis.AxisNum}轴核1规划器位置绑定失败。";
                    return false;
                }

                if (!Motion.SetPrf(axis.AxisNo))
                {
                    message = $"固高复位失败：{axis.AxisNum}轴规划器位置绑定失败。";
                    return false;
                }
            }

            if (!SetSpeedAll(EN_SpeedType.Mid))
            {
                message = "固高复位失败：恢复默认速度失败。";
                return false;
            }

            message = string.Empty;
            return true;
        }
```

- [ ] **Step 6: Run the startup reset regression test**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: `Googol DoGoHome supports startup auto reset` passes. If the full source-level suite stops later on an unrelated missing file, the new test must appear before that failure and pass.

- [ ] **Step 7: Build Googol project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj
```

Expected: build succeeds.

### Task 3: Verify Googol Speed Monitor Remains Wired

**Files:**
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Run the existing Googol speed source assertions**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: `Googol control card reads encoder speed`, `AxisView refreshes and displays actual speed`, and `AxisView speed monitor common surface` pass.

- [ ] **Step 2: Confirm Googol speed implementation uses encoder velocity**

Run:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.cs -Pattern "GetAllSpeedInfos|Motion.GetEncVel|PulseEquivalent|axisConfig.CurSpeed|CurSpeed\\[axisIndex\\]"
```

Expected: matches show `GetAllSpeedInfos`, `Motion.GetEncVel(1, ref tmpAllSpeedInfos, core)`, `PulseEquivalent`, `axisConfig.CurSpeed = speed`, and `CurSpeed[axisIndex] = speed`.

- [ ] **Step 3: Confirm Googol wrapper uses `GTN_GetEncVel`**

Run:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\Packaging\GoogolGTMotion.cs -Pattern "GetEncVel|GTN_GetEncVel|encVel == null|axisId < 1"
```

Expected: matches show `public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)`, `GTN_GetEncVel(core, axisId, out encVel[0]`, `encVel == null || encVel.Length == 0`, and `axisId < 1 || axisId > _axisCount`.

- [ ] **Step 4: Confirm AxisView has no vendor branch**

Run:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs -Pattern "AcsControlCard|GoogolControlCard|ReeYin_V.Hardware.ControlCard.ACS|ReeYin_V.Hardware.ControlCard.Googol"
```

Expected: no matches. AxisView must keep using the unified `ControlCard.GetAllSpeedInfos()` API.

### Task 4: Final Build And Verification

**Files:**
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Build the common control-card project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj
```

Expected: build succeeds.

- [ ] **Step 2: Build the Googol project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Build the ACS source-level tests**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: build succeeds.

- [ ] **Step 4: Run source-level tests**

Run:

```powershell
dotnet run --no-build --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: all control-card startup reset and speed-monitor tests pass. If the suite stops on an unrelated pre-existing missing-file assertion, record the first failing test name and confirm all newly added Googol startup reset assertions ran before it.

- [ ] **Step 5: Record git limitation**

Run:

```powershell
git -C E:\Company\工作目录\ReeYin-V\ReeYin-V status --short
```

Expected in the current workspace: `fatal: not a git repository (or any of the parent directories): .git`.

## Self-Review

- Spec coverage: Task 2 implements startup-safe Googol reset; Task 3 verifies existing Googol encoder-speed monitoring remains wired through AxisView; Task 4 verifies builds/tests.
- Placeholder scan: no unfinished markers or unspecified implementation steps remain.
- Type consistency: helper signatures use existing `SingleAxisParam`, `En_Priority`, `EN_SpeedType`, `ResetAxis`, `AxisWaitResetZero`, `SetSpeedAll`, and `Motion.SetPrf` members already available to `GoogolControlCard`.

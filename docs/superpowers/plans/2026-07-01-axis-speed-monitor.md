# Axis Speed Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add actual feedback speed monitoring to AxisView through a unified control-card API implemented by ACS and Googol.

**Architecture:** The common control-card project owns the speed contract, runtime speed buffers, axis speed model fields, and AxisView mapping. ACS and Googol override the common speed methods and translate vendor feedback velocity into the existing user-unit display convention. AxisView keeps polling every 100 ms and displays speed from the same fixed `X, Y, Z, Z1, Z2` axis order used for positions.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, ACS.SPiiPlusNET8, Googol GTN API, source-level regression tests in `ReeYin_V.Hardware.ControlCard.ACS.Tests`.

---

## Git State Note

`git -C E:\Company\工作目录\ReeYin-V\ReeYin-V status --short` currently returns `fatal: not a git repository (or any of the parent directories): .git` because the `.git` directory lacks `HEAD`. Commit steps are not executable until the repository metadata is restored. Each task still lists the exact files to review for a future commit.

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCard.cs`: add the unified `GetAllSpeedInfos` API.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ControlCardBase.cs`: add `CurSpeed`, `EnsureSpeedBuffers`, and default speed API implementations.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/SingleAxisParam.cs`: add runtime per-axis `CurSpeed`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`: add `CurSpeedInfos` for AxisView binding.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisViewAxisMatcher.cs`: add `BuildSpeedSnapshot`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`: refresh speed snapshots without blocking position refresh.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`: persist the selected speed mode immediately when `切换速度` runs.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/AxisView.xaml`: display speed values with `mm/s`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`: implement ACS feedback speed reads.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`: add a GTN encoder velocity helper.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`: implement Googol feedback speed reads.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`: add regression coverage.

### Task 1: Common Speed Surface Failing Test

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add a failing regression test registration**

Add this `Run` call next to the existing AxisView tests:

```csharp
Run("AxisView speed monitor common surface", TestAxisViewSpeedMonitorCommonSurface);
```

- [ ] **Step 2: Add the failing test body**

Insert this function near `TestAxisViewMatchesDisplayedAxisStateByAxisType`:

```csharp
void TestAxisViewSpeedMonitorCommonSurface()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");
    var baseSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs");
    var singleAxisSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\SingleAxisParam.cs");
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var matcherSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewAxisMatcher.cs");

    AssertContains(interfaceSource, "bool GetAllSpeedInfos(short core = 2)",
        "IControlCard should expose a unified speed refresh API");
    AssertContains(interfaceSource, "bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "IControlCard should expose a speed array copy overload");
    AssertContains(baseSource, "public double[] CurSpeed { get; set; }",
        "ControlCardBase should cache current speed by physical axis index");
    AssertContains(baseSource, "protected void EnsureSpeedBuffers(int requiredLength = 0)",
        "ControlCardBase should resize speed buffers consistently");
    AssertContains(baseSource, "public virtual bool GetAllSpeedInfos(short core = 2)",
        "ControlCardBase should provide a default speed refresh implementation");
    AssertContains(singleAxisSource, "public double CurSpeed",
        "SingleAxisParam should store runtime current speed");
    AssertContains(axisModelSource, "public double[] CurSpeedInfos",
        "AxisModel should expose AxisView speed snapshots");
    AssertContains(matcherSource, "BuildSpeedSnapshot",
        "AxisViewAxisMatcher should build speed snapshots by axis type");

    var axes = new[]
    {
        new SingleAxisParam { AxisNum = En_AxisNum.Z1, AxisNo = 4, CurSpeed = 41.25 },
        new SingleAxisParam { AxisNum = En_AxisNum.Y, AxisNo = 2, CurSpeed = -12.5 },
        new SingleAxisParam { AxisNum = En_AxisNum.X, AxisNo = 1, CurSpeed = 3.75 }
    };

    var speeds = AxisViewAxisMatcher.BuildSpeedSnapshot(axes);

    AssertEqual(5, speeds.Length, "displayed speed count");
    AssertClose(3.75, speeds[0], "X speed comes from X axis config");
    AssertClose(-12.5, speeds[1], "Y speed comes from Y axis config");
    AssertClose(0d, speeds[2], "missing Z speed falls back to zero");
    AssertClose(41.25, speeds[3], "Z1 speed comes from Z1 axis config");
    AssertClose(0d, speeds[4], "missing Z2 speed falls back to zero");
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL at compile time with missing `CurSpeed` and `BuildSpeedSnapshot`, or FAIL at runtime on one of the new source assertions.

- [ ] **Step 4: Review files for future commit**

Review:

```powershell
Get-Content Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs | Select-String -Pattern "AxisView speed monitor common surface|TestAxisViewSpeedMonitorCommonSurface"
```

Expected: the new test registration and function are present.

### Task 2: Common Speed Contract And Mapping

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCard.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ControlCardBase.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/SingleAxisParam.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisViewAxisMatcher.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add common interface methods**

In `IControlCard.cs`, add the speed methods immediately after `GetAllPosInfos(short core = 2)`:

```csharp
        /// <summary>
        /// 获取所有轴的实际速度信息
        /// </summary>
        bool GetAllSpeedInfos(short core = 2);

        /// <summary>
        /// 获取所有轴的实际速度信息
        /// </summary>
        bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2);
```

- [ ] **Step 2: Add speed cache and buffer helper**

In `ControlCardBase.cs`, add the cache property after `CurPulse`:

```csharp
        /// <summary>
        /// 当前所有轴的实际速度
        /// </summary>
        [JsonIgnore]
        public double[] CurSpeed { get; set; }
```

Add this helper immediately after `EnsurePositionBuffers`:

```csharp
        protected void EnsureSpeedBuffers(int requiredLength = 0)
        {
            var count = Math.Max(requiredLength, Config?.AllAxis?.Count ?? 0);
            count = Math.Max(1, count);

            if (CurSpeed == null || CurSpeed.Length < count)
            {
                CurSpeed = new double[count];
            }
        }
```

- [ ] **Step 3: Add default speed API implementations**

In `ControlCardBase.cs`, add these methods immediately after the existing `GetAllPosInfos` overloads:

```csharp
        public virtual bool GetAllSpeedInfos(short core = 2)
        {
            return false;
        }

        public virtual bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
        {
            return false;
        }
```

- [ ] **Step 4: Add per-axis runtime speed**

In `SingleAxisParam.cs`, add this property immediately after `CurPos`:

```csharp
        /// <summary>
        /// 当前实际速度
        /// </summary>
        [JsonIgnore]
        private double _curSpeed;
        /// <summary>
        /// 当前实际速度
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public double CurSpeed
        {
            get { return _curSpeed; }
            set { _curSpeed = value; RaisePropertyChanged(); }
        }
```

- [ ] **Step 5: Add AxisView speed snapshot model**

In `AxisModel.cs`, add this property immediately after `CurPosInfos`:

```csharp
        [JsonIgnore]
        private double[] _curSpeedInfos;
        /// <summary>
        /// 当前实际速度信息
        /// </summary>
        [JsonIgnore]
        public double[] CurSpeedInfos
        {
            get { return _curSpeedInfos; }
            set { _curSpeedInfos = value; RaisePropertyChanged(); }
        }
```

- [ ] **Step 6: Add speed snapshot mapping**

In `AxisViewAxisMatcher.cs`, add this method immediately after `BuildPositionSnapshot`:

```csharp
        public static double[] BuildSpeedSnapshot(IEnumerable<SingleAxisParam>? axes)
        {
            var result = new double[DisplayAxes.Count];
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                if (TryGetAxis(axes, DisplayAxes[index], out var axis))
                {
                    result[index] = axis.CurSpeed;
                }
            }

            return result;
        }
```

- [ ] **Step 7: Run the common speed test**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: the new common-surface assertions pass. The full test run can still fail on later speed UI/vendor tests that have not been added.

- [ ] **Step 8: Build the common project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj
```

Expected: build succeeds.

### Task 3: AxisView Speed Refresh And Display

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/AxisView.xaml`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add failing AxisView speed test registration**

Add this `Run` call next to the common speed test:

```csharp
Run("AxisView refreshes and displays actual speed", TestAxisViewRefreshesAndDisplaysActualSpeed);
```

- [ ] **Step 2: Add failing AxisView speed test body**

Insert this function near the other AxisView tests:

```csharp
void TestAxisViewRefreshesAndDisplaysActualSpeed()
{
    var viewModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\AxisView.xaml");

    AssertContains(viewModelSource, "RefreshCurrentSpeedSnapshots();",
        "AxisViewModel constructor should initialize displayed speed snapshots");
    AssertContains(viewModelSource, "RefreshCurrentSpeedSnapshotFromCard();",
        "AxisViewModel timer should refresh speed from the control card");
    AssertContains(viewModelSource, "ControlCard.GetAllSpeedInfos()",
        "AxisViewModel should use the unified speed API");
    AssertContains(viewModelSource, "AxisViewAxisMatcher.BuildSpeedSnapshot",
        "AxisViewModel should map speed snapshots by displayed axis type");
    AssertContains(viewModelSource, "获取轴速度数据失败",
        "AxisViewModel should log speed refresh failures without blocking positions");

    AssertContains(xaml, "ModelParam.CurSpeedInfos[0]",
        "AxisView should bind X speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[1]",
        "AxisView should bind Y speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[2]",
        "AxisView should bind Z speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[3]",
        "AxisView should bind Z1 speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[4]",
        "AxisView should bind Z2 speed");
    AssertContains(xaml, "mm/s",
        "AxisView should label displayed speed in mm/s");
}
```

- [ ] **Step 3: Run the AxisView speed test to verify it fails**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL on missing `RefreshCurrentSpeedSnapshots`, `RefreshCurrentSpeedSnapshotFromCard`, or speed XAML bindings.

- [ ] **Step 4: Update AxisViewModel initialization**

In `AxisViewModel.cs`, add this call after `RefreshCurrentPositionSnapshots();` in the constructor:

```csharp
            RefreshCurrentSpeedSnapshots();
```

- [ ] **Step 5: Update AxisViewModel timer**

In the timer tick, add this call immediately after `RefreshDisplayedAxisStatus();`:

```csharp
                RefreshCurrentSpeedSnapshotFromCard();
```

- [ ] **Step 6: Add AxisViewModel speed helper methods**

In `AxisViewModel.cs`, add these methods immediately after `RefreshCurrentPositionSnapshots()`:

```csharp
        private void RefreshCurrentSpeedSnapshots()
        {
            ModelParam.CurSpeedInfos = AxisViewAxisMatcher.BuildSpeedSnapshot(ControlCard?.Config?.AllAxis);
        }

        private void RefreshCurrentSpeedSnapshotFromCard()
        {
            if (!ControlCard.GetAllSpeedInfos())
            {
                Console.WriteLine("获取轴速度数据失败!!!");
                return;
            }

            RefreshCurrentSpeedSnapshots();
        }
```

- [ ] **Step 7: Add X and Y speed bindings**

In `AxisView.xaml`, in the bottom `TextBlock` that already shows `XPos` and `YPos`, add these runs before the closing `</TextBlock>`:

```xml
                            <Run Text=" XVel:"/>
                            <Run Text="{Binding ModelParam.CurSpeedInfos[0],FallbackValue='--', StringFormat=:{0:F2}}"/>
                            <Run Text="mm/s"/>

                            <Run Text=" YVel:"/>
                            <Run Text="{Binding ModelParam.CurSpeedInfos[1],FallbackValue='--', StringFormat=:{0:F2}}"/>
                            <Run Text="mm/s"/>
```

- [ ] **Step 8: Add Z, Z1, and Z2 speed bindings**

In `AxisView.xaml`, add one speed line to each Z-axis position text block.

For the Z position block, add:

```xml
                            <LineBreak/>
                            <Run Text="ZVel:"/>
                            <Run Text="{Binding ModelParam.CurSpeedInfos[2],FallbackValue='--', StringFormat=:{0:F2}}"/>
                            <LineBreak/>
                            <Run Text="mm/s"/>
```

For the Z1 position block, add:

```xml
                            <LineBreak/>
                            <Run Text="Z1Vel:"/>
                            <Run Text="{Binding ModelParam.CurSpeedInfos[3],FallbackValue='--', StringFormat=:{0:F2}}"/>
                            <LineBreak/>
                            <Run Text="mm/s"/>
```

For the Z2 position block, add:

```xml
                            <LineBreak/>
                            <Run Text="Z2Vel:"/>
                            <Run Text="{Binding ModelParam.CurSpeedInfos[4],FallbackValue='--', StringFormat=:{0:F2}}"/>
                            <LineBreak/>
                            <Run Text="mm/s"/>
```

- [ ] **Step 9: Run the AxisView speed test**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: the AxisView speed assertions pass.

- [ ] **Step 10: Build the common project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj
```

Expected: build succeeds.

### Task 3B: AxisView Speed Mode Persistence Bug Fix

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add failing speed-mode persistence test registration**

Add this `Run` call next to the other AxisView tests:

```csharp
Run("AxisView persists selected speed mode immediately", TestAxisViewPersistsSelectedSpeedModeImmediately);
```

- [ ] **Step 2: Add failing speed-mode persistence test body**

Insert this function near the other AxisView tests:

```csharp
void TestAxisViewPersistsSelectedSpeedModeImmediately()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var switchBody = ReadSourceBetween(source, "case \"切换速度\":", "case \"关闭\":");

    AssertContains(switchBody, "ModelParam.CurSpeedType = newSpeed;",
        "speed switch should update the selected speed mode");
    AssertContains(switchBody, "ConfigManager.Write(ConfigKey.AxisModel, ModelParam);",
        "speed switch should persist AxisModel immediately so restart keeps the selected speed mode");
    AssertContainsBefore(switchBody,
        "ModelParam.CurSpeedType = newSpeed;",
        "ConfigManager.Write(ConfigKey.AxisModel, ModelParam);",
        "AxisView should persist after assigning the new speed mode");
}
```

- [ ] **Step 3: Run the speed-mode persistence test to verify it fails**

Run the already-built test executable when the project has been built with `SolutionDir`:

```powershell
dotnet run --no-build --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL on missing `ConfigManager.Write(ConfigKey.AxisModel, ModelParam);` inside the `切换速度` case. If the run reaches the pre-existing WaferFlatness missing-file failure first, build and run after adding this test still proves the new source assertion is present by temporarily inspecting the failing output or by using `Select-String` on `Program.cs`.

- [ ] **Step 4: Persist speed mode in the switch command**

In `AxisViewModel.cs`, update the `case "切换速度":` block so it writes the config immediately after assigning `CurSpeedType`:

```csharp
                        ModelParam.CurSpeedType = newSpeed;
                        ConfigManager.Write(ConfigKey.AxisModel, ModelParam);
```

- [ ] **Step 5: Build tests with the required SolutionDir property**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"
```

Expected: build succeeds. The existing `halcondotnet` warning may remain.

- [ ] **Step 6: Run source-level tests**

Run:

```powershell
dotnet run --no-build --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: new speed-mode persistence assertion passes. The run may still stop later at the pre-existing missing `CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs` failure.

### Task 4: ACS Feedback Speed Implementation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add failing ACS speed implementation test registration**

Add this `Run` call near the other ACS control-card implementation tests:

```csharp
Run("ACS control card reads feedback speed", TestAcsControlCardReadsFeedbackSpeed);
```

- [ ] **Step 2: Add failing ACS speed implementation test body**

Insert this function near the ACS control-card tests:

```csharp
void TestAcsControlCardReadsFeedbackSpeed()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");

    AssertContains(source, "public override bool GetAllSpeedInfos(short core = 2)",
        "ACS should override the unified speed refresh API");
    AssertContains(source, "public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "ACS should override the speed array copy overload");
    AssertContains(source, "_api.GetFVelocity(acsAxis)",
        "ACS should read actual feedback velocity");
    AssertContains(source, "axisConfig.CurSpeed = speed",
        "ACS should update the configured axis speed");
    AssertContains(source, "CurSpeed[axisIndex] = speed",
        "ACS should update the base speed cache");
    AssertFalse(source.Contains("_api.GetVelocity(acsAxis)", StringComparison.Ordinal),
        "AxisView speed monitoring should not use configured command velocity");
}
```

- [ ] **Step 3: Run the ACS speed test to verify it fails**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL on missing ACS `GetAllSpeedInfos` override.

- [ ] **Step 4: Add ACS speed methods**

In `GoHome.cs`, add these methods immediately after the existing `GetAllPosInfos(ref double[] allPosInfos, short core = 2)` method:

```csharp
    public override bool GetAllSpeedInfos(short core = 2)
    {
        if (!IsConnected)
        {
            return false;
        }

        try
        {
            EnsureSpeedBuffers();

            for (var i = 0; i < Config.AllAxis.Count; i++)
            {
                var axisConfig = Config.AllAxis[i];
                var acsAxis = ToConfiguredAcsAxis(axisConfig);
                var speed = Math.Round(_api.GetFVelocity(acsAxis), 3);
                axisConfig.CurSpeed = speed;

                var axisIndex = axisConfig.AxisNo - 1;
                if (axisIndex >= 0 && axisIndex < CurSpeed.Length)
                {
                    CurSpeed[axisIndex] = speed;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACS GetAllSpeedInfos failed: {ex.Message}");
            return false;
        }
    }

    public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
    {
        if (!GetAllSpeedInfos(core))
        {
            return false;
        }

        for (var i = 0; i < allSpeedInfos.Length && i < Config.AllAxis.Count; i++)
        {
            allSpeedInfos[i] = Config.AllAxis[i].CurSpeed;
        }

        return true;
    }
```

- [ ] **Step 5: Run the ACS speed test**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: ACS feedback speed assertions pass.

- [ ] **Step 6: Build the ACS project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj
```

Expected: build succeeds.

### Task 5: Googol Encoder Speed Implementation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Add failing Googol speed implementation test registration**

Add this `Run` call near the existing Googol implementation tests:

```csharp
Run("Googol control card reads encoder speed", TestGoogolControlCardReadsEncoderSpeed);
```

- [ ] **Step 2: Add failing Googol speed implementation test body**

Insert this function near the Googol control-card tests:

```csharp
void TestGoogolControlCardReadsEncoderSpeed()
{
    var motionSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\Packaging\GoogolGTMotion.cs");
    var cardSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.cs");

    AssertContains(motionSource, "public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)",
        "Googol motion wrapper should expose encoder velocity reads");
    AssertContains(motionSource, "GTN_GetEncVel(core, axisId, out encVel[0]",
        "Googol motion wrapper should use GTN encoder velocity");
    AssertContains(cardSource, "public override bool GetAllSpeedInfos(short core = 2)",
        "Googol should override the unified speed refresh API");
    AssertContains(cardSource, "public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "Googol should override the speed array copy overload");
    AssertContains(cardSource, "Motion.GetEncVel(1, ref tmpAllSpeedInfos, core)",
        "Googol should read actual encoder velocity");
    AssertContains(cardSource, "PulseEquivalent",
        "Googol should convert pulse speed to user units");
    AssertContains(cardSource, "axisConfig.CurSpeed = speed",
        "Googol should update the configured axis speed");
    AssertContains(cardSource, "CurSpeed[axisIndex] = speed",
        "Googol should update the base speed cache");
}
```

- [ ] **Step 3: Run the Googol speed test to verify it fails**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL on missing Googol encoder speed helper or card override.

- [ ] **Step 4: Add Googol encoder velocity wrapper**

In `GoogolGTMotion.cs`, add this method immediately after `GetPrfPos`:

```csharp
        /// <summary>
        /// 获取编码器实际速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="encVel">编码器速度</param>
        /// <returns></returns>
        public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)
        {
            short sRtn;
            if (axisId + encVel.Length - 1 > _axisCount)
            {
                ErrMessage($"多轴获取超过了轴的数量:{_axisCount}");
                return false;
            }

            sRtn = mc.GTN_GetEncVel(core, axisId, out encVel[0], (short)encVel.Length, out _uiClock);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }

            return true;
        }
```

- [ ] **Step 5: Add Googol speed methods**

In `GoogolControlCard.cs`, add these methods immediately after `GetAllPosInfos(short core = 2)`:

```csharp
        public override bool GetAllSpeedInfos(short core = 2)
        {
            try
            {
                lock (_stcobj)
                {
                    if (!IsConnected)
                    {
                        return false;
                    }

                    EnsureSpeedBuffers();
                    var maxAxisNo = Config.AllAxis.Count == 0
                        ? 0
                        : Config.AllAxis.Max(axis => Math.Max(1, (int)axis.AxisNo));

                    if (maxAxisNo == 0)
                    {
                        return true;
                    }

                    var tmpAllSpeedInfos = new double[maxAxisNo];
                    var rs = Motion.GetEncVel(1, ref tmpAllSpeedInfos, core);
                    if (!rs)
                    {
                        return false;
                    }

                    for (var i = 0; i < Config.AllAxis.Count; i++)
                    {
                        var axisConfig = Config.AllAxis[i];
                        var sourceIndex = axisConfig.AxisNo - 1;
                        if (sourceIndex < 0 || sourceIndex >= tmpAllSpeedInfos.Length)
                        {
                            continue;
                        }

                        var pulseEquivalent = Math.Abs(axisConfig.PulseEquivalent) > double.Epsilon
                            ? axisConfig.PulseEquivalent
                            : 1d;
                        var speed = Math.Round(tmpAllSpeedInfos[sourceIndex] / pulseEquivalent, 2);
                        axisConfig.CurSpeed = speed;

                        var axisIndex = axisConfig.AxisNo - 1;
                        if (axisIndex >= 0 && axisIndex < CurSpeed.Length)
                        {
                            CurSpeed[axisIndex] = speed;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllSpeedInfos()_获取所有轴的实际速度失败{ex.StackTrace}");
                return false;
            }
        }

        public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
        {
            if (!GetAllSpeedInfos(core))
            {
                return false;
            }

            for (var i = 0; i < allSpeedInfos.Length && i < Config.AllAxis.Count; i++)
            {
                allSpeedInfos[i] = Config.AllAxis[i].CurSpeed;
            }

            return true;
        }
```

- [ ] **Step 6: Run the Googol speed test**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: Googol encoder speed assertions pass.

- [ ] **Step 7: Build the Googol project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj
```

Expected: build succeeds.

### Task 6: Final Verification

**Files:**
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCard.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ControlCardBase.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/SingleAxisParam.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisViewAxisMatcher.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/AxisView.xaml`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/Packaging/GoogolGTMotion.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.cs`
- Review: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Run all ACS source-level tests**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: all source-level tests pass and print `ACS PEG/DataCollection tests passed.`

- [ ] **Step 2: Build the common control-card project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Build the ACS project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj
```

Expected: build succeeds.

- [ ] **Step 4: Build the Googol project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Check for vendor branching in AxisView**

Run:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs -Pattern "AcsControlCard|GoogolControlCard|ReeYin_V.Hardware.ControlCard.ACS|ReeYin_V.Hardware.ControlCard.Googol"
```

Expected: no matches.

- [ ] **Step 6: Record git limitation**

Run:

```powershell
git -C E:\Company\工作目录\ReeYin-V\ReeYin-V status --short
```

Expected in the current workspace: `fatal: not a git repository (or any of the parent directories): .git`. Do not run commit commands until repository metadata is restored.

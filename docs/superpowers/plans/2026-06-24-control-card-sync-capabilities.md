# Control Card Sync Capabilities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Optimize the shared motion-card base layer and add capability-based synchronized motion/trigger APIs so Googol and ACS can be used by business code through common intent instead of vendor checks.

**Architecture:** Keep `IControlCard` as the legacy baseline contract, move only common safety/configuration helpers into `ControlCardBase`, and add optional capability interfaces for coordinated motion, buffered motion, and synchronized trigger. ACS and Googol each implement the new interfaces by mapping common requests to their own XSEG/LCI/Buffer or CRD/FIFO/position-compare pipelines.

**Tech Stack:** C#/.NET 8 WPF projects, existing console-style ACS test project, ReeYin motion-card models, ACS SPiiPlus SDK wrapper, Googol motion wrapper.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ControlCardBase.cs`
  - Add common axis/config/speed/position/wait helpers.
  - Fix relative move direction, move wait timeout, home return value, and config-change hook.
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCardSyncCapabilities.cs`
  - Define optional capability interfaces.
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardSyncModels.cs`
  - Define common request/result models and capability enums.
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`
  - Map common coordinated motion and synchronized trigger requests to ACS implementations.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`
  - Override `OnConfigChanged` and `MotionTimeoutMs`.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs`
  - Remove or rename helper methods that duplicate new base helpers, then call base helpers.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`
  - Use base position-buffer helper.
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.SyncCapabilities.cs`
  - Map common capability requests to CRD/FIFO/position-compare methods.
- Modify `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorMotionControlModel.cs`
  - Replace direct ACS type detection/reflection with capability checks.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Add regression/source tests for base behavior, capability interfaces, ACS/Googol adapters, and wafer business migration.

## Verification Commands

Use these commands from `E:\Company\工作目录\ReeYin-V\ReeYin-V`:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

If a full build fails because running programs lock `OutputExe` or project `obj` files, report the lock and keep the successful project/test build evidence.

### Task 1: Add Base Behavior Regression Tests

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add test registrations near the existing `Run(...)` list**

Add these lines after the existing axis matcher tests or near other ControlCard base tests:

```csharp
Run("ControlCardBase applies relative move direction", TestControlCardBaseAppliesRelativeMoveDirection);
Run("ControlCardBase returns home failure", TestControlCardBaseReturnsHomeFailure);
Run("ControlCardBase move wait times out", TestControlCardBaseMoveWaitTimesOut);
Run("ControlCardBase invokes config change hook", TestControlCardBaseInvokesConfigChangeHook);
```

- [ ] **Step 2: Add the failing tests before `Run(string name, Action test)`**

```csharp
void TestControlCardBaseAppliesRelativeMoveDirection()
{
    var card = CreateBaseTestCard();
    card.AxisStoppedResponses.Enqueue(true);
    card.AxisStoppedResponses.Enqueue(true);

    var ok = card.Move(En_AxisNum.X, MoveDirection.反向, 5d, out var message);

    AssertTrue(ok, $"relative move should succeed: {message}");
    AssertEqual(1, card.RelativeMoveDistances.Count, "relative move command count");
    AssertEqual(-5d, card.RelativeMoveDistances[0], "reverse move should pass a negative relative distance");
}

void TestControlCardBaseReturnsHomeFailure()
{
    var card = CreateBaseTestCard();
    card.HomeResult = false;
    card.HomeMessage = "home failed";

    var ok = card.GoHome(out var message);

    AssertFalse(ok, "GoHome should return the DoGoHome result");
    AssertEqual("home failed", message, "GoHome should preserve the vendor home failure message");
    AssertFalse(card.IsAxisHomed, "failed home should leave IsAxisHomed false");
}

void TestControlCardBaseMoveWaitTimesOut()
{
    var card = CreateBaseTestCard();
    card.MotionTimeoutOverrideMs = 1;
    card.AxisStoppedResponses.Enqueue(true);
    card.AxisStoppedResponses.Enqueue(false);
    card.AxisStoppedResponses.Enqueue(false);

    var ok = card.Move(En_AxisNum.X, MoveDirection.正向, 3d, out var message);

    AssertFalse(ok, "relative move should fail when the axis does not stop before timeout");
    AssertContains(message, "超时", "timeout message should mention timeout");
    AssertEqual(1, card.RelativeMoveDistances.Count, "move should still have been commanded once");
    AssertEqual(3d, card.RelativeMoveDistances[0], "forward move should pass a positive relative distance");
}

void TestControlCardBaseInvokesConfigChangeHook()
{
    var card = new TestControlCard();

    card.Config = new ControlCardConfig();

    AssertEqual(1, card.ConfigChangedCount, "setting Config should invoke OnConfigChanged once");
}

TestControlCard CreateBaseTestCard()
{
    var card = new TestControlCard();
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 1,
        IsUsing = true,
        SpeedDict1 = new()
        {
            new SpeedSetting { SpeedType = EN_SpeedType.Work, SpeedDescribe = "work", StartSpeed = 1, MaxSpeed = 10, AccSpeed = 100 }
        }
    });

    AssertTrue(card.Init(), "test card should initialize");
    card.IsAxisHomed = true;
    return card;
}
```

- [ ] **Step 3: Add the test card type at the end of the file**

Place this after the last helper method. Top-level test functions can reference this type.

```csharp
sealed class TestControlCard : ControlCardBase
{
    public List<double> RelativeMoveDistances { get; } = new();
    public Queue<bool> AxisStoppedResponses { get; } = new();
    public int ConfigChangedCount { get; private set; }
    public int MotionTimeoutOverrideMs { get; set; } = 100;
    public bool AxisEnabled { get; set; } = true;
    public bool AxisStopped { get; set; } = true;
    public bool HomeResult { get; set; } = true;
    public string HomeMessage { get; set; } = "home ok";

    protected override int MotionTimeoutMs => MotionTimeoutOverrideMs;

    protected override void OnConfigChanged()
    {
        ConfigChangedCount++;
    }

    protected override bool DoInit()
    {
        IsConnected = true;
        return true;
    }

    protected override void DoConfigure()
    {
    }

    protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
    {
    }

    protected override void DoClose()
    {
        IsConnected = false;
    }

    protected override bool DoGetAxisEnable(En_AxisNum axisType)
    {
        return AxisEnabled;
    }

    protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
    {
        AxisEnabled = v;
        return true;
    }

    protected override bool DoGetAxisStopped(En_AxisNum axisType)
    {
        return AxisStoppedResponses.Count > 0 ? AxisStoppedResponses.Dequeue() : AxisStopped;
    }

    protected override bool DoMoveAxis(En_AxisNum axisType, double um)
    {
        RelativeMoveDistances.Add(um);
        return true;
    }

    protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
    {
        return true;
    }

    protected override bool DoGoHome(out string message)
    {
        message = HomeMessage;
        return HomeResult;
    }
}
```

- [ ] **Step 4: Run the test project and verify the new tests fail**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: build may fail first because `ControlCardBase.MotionTimeoutMs` and `ControlCardBase.OnConfigChanged` do not exist. If build succeeds, the test DLL should fail on direction, home return, or timeout behavior.

### Task 2: Implement ControlCardBase Safety Helpers

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ControlCardBase.cs`
- Test `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add base hook properties and update `IsReady`**

Replace:

```csharp
[JsonIgnore]
public bool IsReady => Initialized && IsAxisHomed;
```

with:

```csharp
[JsonIgnore]
public bool IsReady => Initialized && (!RequiresHomeBeforeMove || IsAxisHomed);

[JsonIgnore]
protected virtual int MotionTimeoutMs => 60000;

[JsonIgnore]
protected virtual bool RequiresHomeBeforeMove => true;
```

- [ ] **Step 2: Make `Config` null-safe and call `OnConfigChanged`**

Replace the `Config` property with:

```csharp
public ControlCardConfig Config
{
    get { return _config; }
    set
    {
        _config = value ?? new ControlCardConfig();
        OnConfigChanged();
        RaisePropertyChanged();
    }
}
```

Add this method before `Close()`:

```csharp
protected virtual void OnConfigChanged()
{
}
```

- [ ] **Step 3: Route close-all hardware through the public close flow**

Replace the constructor body with:

```csharp
protected ControlCardBase()
{
    PrismProvider.EventAggregator.GetEvent<CloseAllHardwareEvent>().Subscribe(Close);
}
```

- [ ] **Step 4: Fix relative move direction and validation**

Replace `Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message)` with:

```csharp
public virtual bool Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message)
{
    if (!IsReady)
    {
        message = "运动轴未准备好";
        return false;
    }

    if (double.IsNaN(um) || double.IsInfinity(um) || Math.Abs(um) <= double.Epsilon)
    {
        message = "移动距离必须大于 0";
        return false;
    }

    return MoveAxis(axisType, NormalizeRelativeDistance(moveDirection, um), out message);
}
```

- [ ] **Step 5: Add timeout to relative move wait**

Replace `MoveAxis(...)` with:

```csharp
private bool MoveAxis(En_AxisNum axisType, double um, out string v)
{
    if (!DoGetAxisEnable(axisType))
    {
        v = "当前轴未开启使能";
        return false;
    }

    if (!DoGetAxisStopped(axisType))
    {
        v = "当前轴正在运动中";
        return false;
    }

    if (!DoMoveAxis(axisType, um))
    {
        v = "当前轴运动启动失败";
        return false;
    }

    if (!WaitUntilAxisStopped(axisType, MotionTimeoutMs))
    {
        v = $"当前轴等待停止超时，超时时间 {MotionTimeoutMs}ms";
        return false;
    }

    v = "当前轴运动完成";
    return true;
}
```

- [ ] **Step 6: Fix `GoHome` return value**

Replace `GoHome(out string message)` with:

```csharp
public bool GoHome(out string message)
{
    Console.WriteLine($"{DateTime.Now:hh-mm-ss.fff}触发回零！");
    message = string.Empty;

    if (!Initialized)
    {
        message = "控制卡未初始化";
        Console.WriteLine(message);
        return false;
    }

    DoStop(null, AxisStopMode.立即停止);
    Thread.Sleep(100);

    try
    {
        IsAxisHoming = true;
        IsAxisHomed = false;
        IsAxisHomed = DoGoHome(out message);
        return IsAxisHomed;
    }
    catch (Exception ex)
    {
        message = ex.Message;
        IsAxisHomed = false;
        return false;
    }
    finally
    {
        IsAxisHoming = false;
    }
}
```

- [ ] **Step 7: Add reusable helper methods before `ValidateLimitPosition`**

```csharp
protected bool TryGetAxisConfig(En_AxisNum axisNum, out SingleAxisParam axis)
{
    var matchedAxis = Config?.AllAxis?.FirstOrDefault(item => item != null && item.AxisNum == axisNum);
    if (matchedAxis == null)
    {
        axis = null!;
        return false;
    }

    axis = matchedAxis;
    return true;
}

protected IReadOnlyList<SingleAxisParam> GetConfiguredAxes(bool onlyUsing = true)
{
    var axes = Config?.AllAxis?.Where(item => item != null).ToList() ?? new List<SingleAxisParam>();
    return onlyUsing ? axes.Where(item => item.IsUsing).ToList() : axes;
}

protected static short GetOneBasedAxisNo(SingleAxisParam axis)
{
    return axis == null ? (short)1 : (short)Math.Max(1, axis.AxisNo);
}

protected static short GetZeroBasedAxisNo(SingleAxisParam axis)
{
    return axis == null ? (short)0 : (short)Math.Max(0, axis.AxisNo - 1);
}

protected void EnsurePositionBuffers(int requiredLength = 0)
{
    var count = Math.Max(requiredLength, Config?.AllAxis?.Count ?? 0);
    count = Math.Max(1, count);

    if (CurPos == null || CurPos.Length < count)
    {
        CurPos = new double[count];
    }

    if (CurPulse == null || CurPulse.Length < count)
    {
        CurPulse = new int[count];
    }
}

protected SpeedSetting? ResolveSpeedSetting(En_AxisNum axisNum, EN_SpeedType? speedType = null)
{
    if (!TryGetAxisConfig(axisNum, out var axis) || axis.SpeedDict1 == null || axis.SpeedDict1.Count == 0)
    {
        return null;
    }

    var requestedSpeedType = speedType ?? SpeedMode;
    return axis.SpeedDict1.FirstOrDefault(item => item.SpeedType == requestedSpeedType)
           ?? axis.SpeedDict1.FirstOrDefault(item => item.SpeedType == EN_SpeedType.Work)
           ?? axis.SpeedDict1.FirstOrDefault();
}

protected double GetAxisDefaultVelocity(En_AxisNum axisNum, EN_SpeedType? speedType = null)
{
    return Math.Abs(ResolveSpeedSetting(axisNum, speedType)?.MaxSpeed ?? 100d);
}

protected static double NormalizeRelativeDistance(MoveDirection direction, double distance)
{
    return direction == MoveDirection.反向 ? -Math.Abs(distance) : Math.Abs(distance);
}

protected bool WaitUntilAxisStopped(En_AxisNum axisNum, int timeoutMs, int pollingIntervalMs = 20)
{
    var timeout = Math.Max(0, timeoutMs);
    var interval = Math.Max(1, pollingIntervalMs);
    var elapsed = 0;

    while (elapsed <= timeout)
    {
        if (DoGetAxisStopped(axisNum))
        {
            return true;
        }

        if (elapsed == timeout)
        {
            break;
        }

        var sleep = Math.Min(interval, timeout - elapsed);
        Thread.Sleep(sleep);
        elapsed += sleep;
    }

    return false;
}

protected bool WaitUntilAllAxesStopped(IEnumerable<En_AxisNum> axes, int timeoutMs, int pollingIntervalMs = 20)
{
    var axisArray = axes?.Distinct().ToArray() ?? Array.Empty<En_AxisNum>();
    if (axisArray.Length == 0)
    {
        return true;
    }

    var timeout = Math.Max(0, timeoutMs);
    var interval = Math.Max(1, pollingIntervalMs);
    var elapsed = 0;

    while (elapsed <= timeout)
    {
        if (axisArray.All(DoGetAxisStopped))
        {
            return true;
        }

        if (elapsed == timeout)
        {
            break;
        }

        var sleep = Math.Min(interval, timeout - elapsed);
        Thread.Sleep(sleep);
        elapsed += sleep;
    }

    return false;
}

protected bool TryGetCurrentAxisPosition(En_AxisNum axisNum, out double position)
{
    position = default;
    if (!TryGetAxisConfig(axisNum, out var axis))
    {
        return false;
    }

    if (CurPos != null && axis.AxisNo > 0 && axis.AxisNo <= CurPos.Length)
    {
        position = CurPos[axis.AxisNo - 1];
        return true;
    }

    position = axis.CurPos;
    return true;
}
```

- [ ] **Step 8: Add coordinated target validation helper**

Add this method after `TryGetCurrentAxisPosition`:

```csharp
protected bool TryBuildCoordinatedTargets(
    IReadOnlyList<En_AxisNum>? requestedAxes,
    IReadOnlyDictionary<En_AxisNum, double>? targetPositionMap,
    IReadOnlyList<double>? targetPositions,
    out En_AxisNum[] axisIds,
    out double[] targets,
    out Dictionary<En_AxisNum, double> targetForLimitValidation,
    out string message)
{
    axisIds = Array.Empty<En_AxisNum>();
    targets = Array.Empty<double>();
    targetForLimitValidation = new Dictionary<En_AxisNum, double>();
    message = string.Empty;

    if (requestedAxes == null || requestedAxes.Count == 0)
    {
        message = "插补轴不能为空";
        return false;
    }

    axisIds = requestedAxes.ToArray();
    if (axisIds.Distinct().Count() != axisIds.Length)
    {
        message = "插补轴不能重复";
        return false;
    }

    targets = new double[axisIds.Length];
    for (var index = 0; index < axisIds.Length; index++)
    {
        var axisId = axisIds[index];
        if (targetPositionMap != null && targetPositionMap.TryGetValue(axisId, out var mappedPosition))
        {
            targets[index] = mappedPosition;
        }
        else if (targetPositions != null && index < targetPositions.Count)
        {
            targets[index] = targetPositions[index];
        }
        else
        {
            message = $"缺少{axisId}轴目标位置";
            return false;
        }

        if (double.IsNaN(targets[index]) || double.IsInfinity(targets[index]))
        {
            message = $"{axisId}轴目标位置不是有效数值";
            return false;
        }

        targetForLimitValidation[axisId] = targets[index];
    }

    return true;
}
```

- [ ] **Step 9: Run base regression tests**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: the four new `ControlCardBase` tests pass. If existing tests fail because they relied on `GoHome` always returning `true`, update the caller/test to expect the real home result.

### Task 3: Add Optional Sync Capability Models and Interfaces

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCardSyncCapabilities.cs`
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardSyncModels.cs`

- [ ] **Step 1: Add capability source tests**

Add registrations:

```csharp
Run("ControlCard exposes sync capability interfaces without polluting IControlCard", TestControlCardSyncCapabilityInterfaces);
Run("ControlCard exposes sync request models", TestControlCardSyncRequestModels);
```

Add tests:

```csharp
void TestControlCardSyncCapabilityInterfaces()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCardSyncCapabilities.cs");
    var legacyInterfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");

    AssertContains(interfaceSource, "public interface ICoordinatedMotionCard", "coordinated motion capability interface should exist");
    AssertContains(interfaceSource, "public interface IBufferedMotionCard", "buffered motion capability interface should exist");
    AssertContains(interfaceSource, "public interface ISynchronizedTriggerCard", "synchronized trigger capability interface should exist");
    AssertContains(interfaceSource, "MoveCoordinated(CoordinatedMotionRequest request, out string message)", "coordinated motion should use common request model");
    AssertContains(interfaceSource, "RunSynchronizedTrigger(", "synchronized trigger should expose one common entry point");
    AssertFalse(legacyInterfaceSource.Contains("CoordinateArrayPulse", StringComparison.Ordinal), "IControlCard should not expose ACS coordinate-array pulse");
    AssertFalse(legacyInterfaceSource.Contains("Xseg", StringComparison.OrdinalIgnoreCase), "IControlCard should not expose ACS XSEG details");
}

void TestControlCardSyncRequestModels()
{
    var modelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardSyncModels.cs");

    AssertContains(modelSource, "public enum CoordinatedMotionKind", "coordinated motion kind enum should exist");
    AssertContains(modelSource, "public sealed class CoordinatedMotionRequest", "coordinated motion request should exist");
    AssertContains(modelSource, "public enum SynchronizedTriggerMode", "synchronized trigger mode enum should exist");
    AssertContains(modelSource, "public enum SynchronizedTriggerCapabilities", "trigger capability flags should exist");
    AssertContains(modelSource, "CoordinateArrayPulse", "coordinate-array pulse should be represented as a capability");
    AssertContains(modelSource, "PositionCompare", "Googol position compare should be represented as a capability");
    AssertContains(modelSource, "public List<Point> Points", "trigger requests should preserve ordered point inputs");
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: tests fail because the new interface/model files do not exist.

- [ ] **Step 3: Create `ControlCardSyncModels.cs`**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardSyncModels.cs`:

```csharp
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    public enum CoordinatedMotionKind
    {
        Line,
        XsegLine,
        Arc,
        Polyline,
        Custom
    }

    public sealed class CoordinatedMotionRequest
    {
        public CoordinatedMotionKind Kind { get; set; } = CoordinatedMotionKind.Line;
        public List<En_AxisNum> Axes { get; set; } = new();
        public Dictionary<En_AxisNum, double> TargetPositions { get; set; } = new();
        public List<Point> Points { get; set; } = new();
        public EN_SpeedType SpeedType { get; set; } = EN_SpeedType.Work;
        public bool WaitForEnd { get; set; } = true;
        public int Timeout { get; set; } = 60000;
        public LineInterPoParam? LineParam { get; set; }
        public ArcInterPoParam? ArcParam { get; set; }
        public CustomInterPoParam? CustomParam { get; set; }
        public Func<string>? CustomCommand { get; set; }
    }

    public enum SynchronizedTriggerMode
    {
        BufferedIo,
        PositionCompare,
        FixedDistancePulse,
        CoordinateArrayPulse,
        DataCollection
    }

    [Flags]
    public enum SynchronizedTriggerCapabilities
    {
        None = 0,
        BufferedIo = 1,
        PositionCompare = 2,
        FixedDistancePulse = 4,
        CoordinateArrayPulse = 8,
        DataCollection = 16
    }

    public sealed class SynchronizedTriggerRequest
    {
        public SynchronizedTriggerMode Mode { get; set; }
        public List<En_AxisNum> Axes { get; set; } = new();
        public List<Point> Points { get; set; } = new();
        public int BufferNo { get; set; } = 10;
        public double PulseWidth { get; set; } = 0.01d;
        public double Interval { get; set; } = 1d;
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
        public double TriggerWindow { get; set; } = 0.001d;
        public double Velocity { get; set; } = 10d;
        public ushort DoMask { get; set; }
        public ushort DoValue { get; set; }
        public ushort DelayMilliseconds { get; set; }
        public bool WaitForEnd { get; set; } = true;
        public int Timeout { get; set; } = 60000;
        public PosComparisonOutputParam? PositionCompareParam { get; set; }
        public PosCompareData? PositionCompareDataTemplate { get; set; }
        public object? VendorRequest { get; set; }
    }

    public sealed class SynchronizedTriggerResult
    {
        public bool Success { get; set; }
        public int PulseCount { get; set; }
        public int PointCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object?> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Create `IControlCardSyncCapabilities.cs`**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Interface/IControlCardSyncCapabilities.cs`:

```csharp
using ReeYin_V.Hardware.ControlCard.Models;

namespace ReeYin_V.Hardware.ControlCard
{
    public interface ICoordinatedMotionCard
    {
        bool SupportsCoordinatedMotion { get; }
        bool MoveCoordinated(CoordinatedMotionRequest request, out string message);
    }

    public interface IBufferedMotionCard
    {
        bool SupportsBufferedMotion { get; }
        bool ClearMotionBuffer(short coordinateOrBuffer, out string message);
        bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message);
    }

    public interface ISynchronizedTriggerCard
    {
        SynchronizedTriggerCapabilities TriggerCapabilities { get; }
        bool RunSynchronizedTrigger(
            SynchronizedTriggerRequest request,
            out SynchronizedTriggerResult result,
            out string message);
    }
}
```

- [ ] **Step 5: Run capability model tests**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: capability model/interface tests pass. Adapter tests are not added yet.

### Task 4: Adapt ACS to Capability Interfaces

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs`
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`

- [ ] **Step 1: Add ACS adapter tests**

Add registrations:

```csharp
Run("ACS control card implements sync capability interfaces", TestAcsControlCardImplementsSyncCapabilityInterfaces);
Run("ACS sync adapter maps coordinate array pulse", TestAcsSyncAdapterMapsCoordinateArrayPulse);
Run("ACS reuses base control-card helpers", TestAcsReusesBaseControlCardHelpers);
```

Add tests:

```csharp
void TestAcsControlCardImplementsSyncCapabilityInterfaces()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.SyncCapabilities.cs");

    AssertContains(source, "ICoordinatedMotionCard", "ACS should implement coordinated motion capability");
    AssertContains(source, "IBufferedMotionCard", "ACS should implement buffered motion capability");
    AssertContains(source, "ISynchronizedTriggerCard", "ACS should implement synchronized trigger capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.FixedDistancePulse", "ACS should advertise fixed-distance pulse");
    AssertContains(source, "SynchronizedTriggerCapabilities.CoordinateArrayPulse", "ACS should advertise coordinate-array pulse");
    AssertContains(source, "SynchronizedTriggerCapabilities.DataCollection", "ACS should advertise data collection");
}

void TestAcsSyncAdapterMapsCoordinateArrayPulse()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.SyncCapabilities.cs");

    AssertContains(source, "SynchronizedTriggerMode.CoordinateArrayPulse", "ACS adapter should handle coordinate-array pulse requests");
    AssertContains(source, "TryRunLciCoordinateArrayPulse", "ACS coordinate-array trigger should call the existing LCI runner");
    AssertContains(source, "request.Points.Select", "ACS coordinate-array trigger should preserve ordered request points");
    AssertContains(source, "new AcsPoint2D(point.X, point.Y)", "ACS coordinate-array trigger should map common points to ACS points");
}

void TestAcsReusesBaseControlCardHelpers()
{
    var axisSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AxisSetting.cs");
    var goHomeSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");

    AssertFalse(axisSource.Contains("private SpeedSetting? GetSpeedSetting", StringComparison.Ordinal), "ACS should reuse base speed resolution helper");
    AssertFalse(axisSource.Contains("private double GetAxisDefaultVelocity", StringComparison.Ordinal), "ACS should reuse base default velocity helper");
    AssertFalse(axisSource.Contains("private bool TryGetAxisConfig", StringComparison.Ordinal), "ACS should reuse base axis lookup helper");
    AssertContains(goHomeSource, "EnsurePositionBuffers(", "ACS should reuse base position-buffer helper");
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: tests fail because ACS adapter file does not exist and ACS still has duplicate helpers.

- [ ] **Step 3: Add ACS capability adapter**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.SyncCapabilities.cs`:

```csharp
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard : ICoordinatedMotionCard, IBufferedMotionCard, ISynchronizedTriggerCard
{
    public bool SupportsCoordinatedMotion => true;

    public bool SupportsBufferedMotion => true;

    public SynchronizedTriggerCapabilities TriggerCapabilities =>
        SynchronizedTriggerCapabilities.FixedDistancePulse |
        SynchronizedTriggerCapabilities.CoordinateArrayPulse |
        SynchronizedTriggerCapabilities.DataCollection;

    public bool MoveCoordinated(CoordinatedMotionRequest request, out string message)
    {
        message = string.Empty;
        if (request == null)
        {
            message = "同步运动请求不能为空";
            return false;
        }

        try
        {
            return request.Kind switch
            {
                CoordinatedMotionKind.Line => LineInterpoMoving(BuildLineParam(request, useProvidedParam: true)),
                CoordinatedMotionKind.XsegLine or CoordinatedMotionKind.Polyline => LineInterpoMovingXseg(BuildLineParam(request, useProvidedParam: true)),
                CoordinatedMotionKind.Arc when request.ArcParam != null => ArcInterpoMoving(request.ArcParam),
                _ => FailUnsupportedCoordinatedMotion(request.Kind, out message)
            };
        }
        catch (Exception ex)
        {
            message = $"ACS同步运动执行异常：{ex.Message}";
            return false;
        }
    }

    public bool ClearMotionBuffer(short coordinateOrBuffer, out string message)
    {
        return TryClearProgramBuffer(coordinateOrBuffer, out message);
    }

    public bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message)
    {
        if (!TryRunProgramBuffer(coordinateOrBuffer, null, out message))
        {
            return false;
        }

        return !waitForEnd || WaitProgramBufferEnd(coordinateOrBuffer, MotionTimeoutMs, out message);
    }

    public bool RunSynchronizedTrigger(
        SynchronizedTriggerRequest request,
        out SynchronizedTriggerResult result,
        out string message)
    {
        result = new SynchronizedTriggerResult();
        message = string.Empty;
        if (request == null)
        {
            message = "同步触发请求不能为空";
            result.Message = message;
            return false;
        }

        var success = request.Mode switch
        {
            SynchronizedTriggerMode.FixedDistancePulse => RunFixedDistancePulse(request, result, out message),
            SynchronizedTriggerMode.CoordinateArrayPulse => RunCoordinateArrayPulse(request, result, out message),
            SynchronizedTriggerMode.DataCollection => RunDataCollectionTrigger(request, result, out message),
            _ => FailUnsupportedTrigger(request.Mode, result, out message)
        };

        result.Success = success;
        result.Message = message;
        return success;
    }

    private LineInterPoParam BuildLineParam(CoordinatedMotionRequest request, bool useProvidedParam)
    {
        if (useProvidedParam && request.LineParam != null)
        {
            return request.LineParam;
        }

        return new LineInterPoParam
        {
            InterPoAxiss = request.Axes.ToList(),
            TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
            TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
            DefaultSpeed = request.SpeedType,
            waitforend = request.WaitForEnd
        };
    }

    private bool RunFixedDistancePulse(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        var param = new AcsLciFixedDistancePulseXsegParam
        {
            BufferNo = request.BufferNo,
            AxisX = ResolveAcsAxisNumber(request, En_AxisNum.X),
            AxisY = ResolveAcsAxisNumber(request, En_AxisNum.Y),
            PulseWidth = request.PulseWidth,
            Interval = request.Interval,
            StartDistance = request.StartDistance,
            EndDistance = request.EndDistance,
            Timeout = request.Timeout,
            Points = request.Points.Select(point => new AcsPoint2D(point.X, point.Y)).ToList()
        };

        if (!TryRunLciFixedDistancePulseXseg(param, out var pulseResult, out message))
        {
            return false;
        }

        result.PulseCount = pulseResult.PulseCount;
        result.PointCount = param.Points.Count;
        result.Data["Channel"] = pulseResult.Channel;
        result.Data["Script"] = pulseResult.Script;
        return true;
    }

    private bool RunCoordinateArrayPulse(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        var param = new AcsLciCoordinateArrayPulseParam
        {
            BufferNo = request.BufferNo,
            AxisX = ResolveAcsAxisNumber(request, En_AxisNum.X),
            AxisY = ResolveAcsAxisNumber(request, En_AxisNum.Y),
            PulseWidth = request.PulseWidth,
            MultiAxWinSize = request.TriggerWindow,
            Velocity = request.Velocity,
            Timeout = request.Timeout,
            Points = request.Points.Select(point => new AcsPoint2D(point.X, point.Y)).ToList()
        };

        if (!TryRunLciCoordinateArrayPulse(param, out var pulseResult, out message))
        {
            return false;
        }

        result.PulseCount = pulseResult.PulseCount;
        result.PointCount = pulseResult.PointCount;
        result.Data["Channel"] = pulseResult.Channel;
        result.Data["Script"] = pulseResult.Script;
        return true;
    }

    private bool RunDataCollectionTrigger(
        SynchronizedTriggerRequest request,
        SynchronizedTriggerResult result,
        out string message)
    {
        if (request.VendorRequest is not AcsDataCollectionRequest dataCollectionRequest)
        {
            message = "ACS数据采集需要 AcsDataCollectionRequest";
            return false;
        }

        if (!RunDataCollection(dataCollectionRequest, out var dataCollectionResult))
        {
            message = dataCollectionResult.Message;
            return false;
        }

        message = dataCollectionResult.Message;
        result.Data["DataCollection"] = dataCollectionResult;
        return true;
    }

    private int ResolveAcsAxisNumber(SynchronizedTriggerRequest request, En_AxisNum fallbackAxis)
    {
        var axisNum = request.Axes.FirstOrDefault(axis => axis == fallbackAxis);
        if (axisNum == default && request.Axes.Count > 0)
        {
            axisNum = request.Axes[Math.Min(request.Axes.Count - 1, fallbackAxis == En_AxisNum.Y ? 1 : 0)];
        }

        return TryGetAxisConfig(axisNum, out var axis) ? GetZeroBasedAxisNo(axis) : (fallbackAxis == En_AxisNum.Y ? 1 : 0);
    }

    private static bool FailUnsupportedCoordinatedMotion(CoordinatedMotionKind kind, out string message)
    {
        message = $"ACS暂不支持该同步运动类型：{kind}";
        return false;
    }

    private static bool FailUnsupportedTrigger(
        SynchronizedTriggerMode mode,
        SynchronizedTriggerResult result,
        out string message)
    {
        message = $"ACS暂不支持该同步触发类型：{mode}";
        result.Message = message;
        return false;
    }
}
```

- [ ] **Step 4: Make ACS config changes sync options**

In `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`, add inside the class:

```csharp
protected override int MotionTimeoutMs => InternalTimeout;

protected override void OnConfigChanged()
{
    EnsureOptions();
}
```

- [ ] **Step 5: Replace duplicate ACS helpers with base helpers**

In `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs`:

Replace calls to `GetSpeedSetting(axisType)` with:

```csharp
ResolveSpeedSetting(axisType)
```

Replace calls to `GetSpeedSetting(axisType)?.MaxSpeed` with:

```csharp
ResolveSpeedSetting(axisType)?.MaxSpeed
```

Replace calls to `GetAxisDefaultVelocity(axisType)` with:

```csharp
base.GetAxisDefaultVelocity(axisType)
```

Delete these private methods from `AxisSetting.cs`:

```csharp
private SpeedSetting? GetSpeedSetting(En_AxisNum axisType)
private double GetAxisDefaultVelocity(En_AxisNum axisType)
private bool TryGetAxisConfig(En_AxisNum axisType, out SingleAxisParam axis)
private void InitializeAxisBuffers()
private void EnsurePositionBuffers()
```

In `InitCard.cs`, replace:

```csharp
InitializeAxisBuffers();
```

with:

```csharp
EnsurePositionBuffers();
```

In `GoHome.cs`, keep the existing `EnsurePositionBuffers();` calls and let them bind to the base helper after the duplicate private method is removed.

- [ ] **Step 6: Resolve ACS wait helper duplication**

In `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/Stop.cs`, delete private methods:

```csharp
private bool WaitUntilAxisStopped(En_AxisNum axisType, int timeout)
private bool WaitUntilAllAxesStopped(IEnumerable<En_AxisNum> axisTypes, int timeout)
```

Existing ACS calls should bind to the protected base methods.

- [ ] **Step 7: Build ACS and run ACS tests**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: ACS project builds and all ACS tests pass, including the new adapter tests.

### Task 5: Adapt Googol to Capability Interfaces

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.SyncCapabilities.cs`

- [ ] **Step 1: Add Googol adapter tests**

Add registrations:

```csharp
Run("Googol control card implements sync capability interfaces", TestGoogolControlCardImplementsSyncCapabilityInterfaces);
Run("Googol sync adapter keeps FIFO and position compare mapping", TestGoogolSyncAdapterKeepsFifoAndPositionCompareMapping);
```

Add tests:

```csharp
void TestGoogolControlCardImplementsSyncCapabilityInterfaces()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.SyncCapabilities.cs");

    AssertContains(source, "ICoordinatedMotionCard", "Googol should implement coordinated motion capability");
    AssertContains(source, "IBufferedMotionCard", "Googol should implement buffered motion capability");
    AssertContains(source, "ISynchronizedTriggerCard", "Googol should implement synchronized trigger capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.PositionCompare", "Googol should advertise position compare");
    AssertContains(source, "SynchronizedTriggerCapabilities.BufferedIo", "Googol should advertise buffered IO");
}

void TestGoogolSyncAdapterKeepsFifoAndPositionCompareMapping()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.SyncCapabilities.cs");
    var interpolationSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\InterpolationMove.cs");

    AssertContains(source, "CrdBufClear", "Googol buffered adapter should clear CRD FIFO");
    AssertContains(source, "CrdData", "Googol buffered adapter should push CRD data");
    AssertContains(source, "CrdMoveStart", "Googol buffered adapter should start CRD motion");
    AssertContains(source, "ControlPosComparison", "Googol position-compare trigger should reuse existing position compare");
    AssertContains(source, "InsertPosCompareData", "Googol position-compare trigger should insert ordered compare points");
    AssertContains(source, "request.Points", "Googol position-compare trigger should iterate ordered request points");
    AssertContains(interpolationSource, "new Barrier(coreCount)", "Googol multi-core start barrier should stay in the Googol implementation");
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: tests fail because the Googol adapter file does not exist.

- [ ] **Step 3: Add Googol capability adapter**

Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.Googol/App/GoogolControlCard.SyncCapabilities.cs`:

```csharp
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    public partial class GoogolControlCard : ICoordinatedMotionCard, IBufferedMotionCard, ISynchronizedTriggerCard
    {
        public bool SupportsCoordinatedMotion => true;

        public bool SupportsBufferedMotion => true;

        public SynchronizedTriggerCapabilities TriggerCapabilities =>
            SynchronizedTriggerCapabilities.PositionCompare |
            SynchronizedTriggerCapabilities.BufferedIo;

        public bool MoveCoordinated(CoordinatedMotionRequest request, out string message)
        {
            message = string.Empty;
            if (request == null)
            {
                message = "同步运动请求不能为空";
                return false;
            }

            try
            {
                return request.Kind switch
                {
                    CoordinatedMotionKind.Line => LineInterpoMoving(BuildLineParam(request)),
                    CoordinatedMotionKind.Arc when request.ArcParam != null => ArcInterpoMoving(request.ArcParam),
                    CoordinatedMotionKind.Custom when request.CustomCommand != null => CustomInterpolationMoving(BuildCustomParam(request), request.CustomCommand, request.WaitForEnd),
                    _ => FailUnsupportedCoordinatedMotion(request.Kind, out message)
                };
            }
            catch (Exception ex)
            {
                message = $"固高同步运动执行异常：{ex.Message}";
                return false;
            }
        }

        public bool ClearMotionBuffer(short coordinateOrBuffer, out string message)
        {
            var ok = CrdBufClear(coordinateOrBuffer);
            message = ok ? "固高缓存区已清空" : "固高缓存区清空失败";
            return ok;
        }

        public bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message)
        {
            if (!CrdData(coordinateOrBuffer))
            {
                message = "固高缓存区数据压入失败";
                return false;
            }

            if (!CrdMoveStart(coordinateOrBuffer, waitForEnd))
            {
                message = "固高缓存区运动启动失败";
                return false;
            }

            message = "固高缓存区运动已启动";
            return true;
        }

        public bool RunSynchronizedTrigger(
            SynchronizedTriggerRequest request,
            out SynchronizedTriggerResult result,
            out string message)
        {
            result = new SynchronizedTriggerResult();
            message = string.Empty;
            if (request == null)
            {
                message = "同步触发请求不能为空";
                result.Message = message;
                return false;
            }

            var success = request.Mode switch
            {
                SynchronizedTriggerMode.BufferedIo => RunBufferedIo(request, out message),
                SynchronizedTriggerMode.PositionCompare => RunPositionCompare(request, out message),
                _ => FailUnsupportedTrigger(request.Mode, out message)
            };

            result.Success = success;
            result.PointCount = request.Points.Count;
            result.Message = message;
            return success;
        }

        private LineInterPoParam BuildLineParam(CoordinatedMotionRequest request)
        {
            if (request.LineParam != null)
            {
                return request.LineParam;
            }

            return new LineInterPoParam
            {
                InterPoAxiss = request.Axes.ToList(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
                TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
                DefaultSpeed = request.SpeedType,
                waitforend = request.WaitForEnd
            };
        }

        private CustomInterPoParam BuildCustomParam(CoordinatedMotionRequest request)
        {
            if (request.CustomParam != null)
            {
                return request.CustomParam;
            }

            return new CustomInterPoParam
            {
                InterPoAxiss = request.Axes.ToList(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
                TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
                DefaultSpeed = request.SpeedType
            };
        }

        private bool RunBufferedIo(SynchronizedTriggerRequest request, out string message)
        {
            if (request.DelayMilliseconds > 0 && !BufDelay(request.DelayMilliseconds))
            {
                message = "固高缓存延时指令写入失败";
                return false;
            }

            if (!BufIO(request.DoMask, request.DoValue))
            {
                message = "固高缓存IO指令写入失败";
                return false;
            }

            message = "固高缓存IO指令写入完成";
            return true;
        }

        private bool RunPositionCompare(SynchronizedTriggerRequest request, out string message)
        {
            var param = request.PositionCompareParam ?? new PosComparisonOutputParam();
            var template = request.PositionCompareDataTemplate ?? new PosCompareData();

            foreach (var point in request.Points)
            {
                InsertPosCompareData(
                    new[] { point.X, point.Y },
                    new PosCompareData
                    {
                        Hso = template.Hso,
                        Gpo = template.Gpo,
                        SegmentNumber = template.SegmentNumber
                    });
            }

            if (!ControlPosComparison(true, param))
            {
                message = "固高位置比较启动失败";
                return false;
            }

            message = "固高位置比较启动完成";
            return true;
        }

        private static bool FailUnsupportedCoordinatedMotion(CoordinatedMotionKind kind, out string message)
        {
            message = $"固高暂不支持该同步运动类型：{kind}";
            return false;
        }

        private static bool FailUnsupportedTrigger(SynchronizedTriggerMode mode, out string message)
        {
            message = $"固高暂不支持该同步触发类型：{mode}";
            return false;
        }
    }
}
```

- [ ] **Step 4: Build Googol and run tests**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: Googol builds, and the new source tests pass.

### Task 6: Migrate Wafer Point Mode to Capability Checks

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
- Modify `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorMotionControlModel.cs`

- [ ] **Step 1: Add wafer migration tests**

Add registrations:

```csharp
Run("WaferFlatness point mode uses sync trigger capabilities", TestWaferFlatnessPointModeUsesSyncTriggerCapabilities);
Run("WaferFlatness point mode removes ACS reflection path", TestWaferFlatnessPointModeRemovesAcsReflectionPath);
```

Add tests:

```csharp
void TestWaferFlatnessPointModeUsesSyncTriggerCapabilities()
{
    var source = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");

    AssertContains(source, "ISynchronizedTriggerCard", "wafer point mode should detect synchronized trigger capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.CoordinateArrayPulse", "wafer point mode should prefer ACS coordinate-array pulse capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.PositionCompare", "wafer point mode should support Googol position-compare capability");
    AssertContains(source, "RunSynchronizedTrigger", "wafer point mode should invoke the common trigger interface");
}

void TestWaferFlatnessPointModeRemovesAcsReflectionPath()
{
    var source = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");

    AssertFalse(source.Contains("TryGetAcsLciCoordinateArrayPulseMethod", StringComparison.Ordinal), "wafer point mode should not use ACS reflection method lookup");
    AssertFalse(source.Contains("BuildAcsLciCoordinateArrayPulseParam", StringComparison.Ordinal), "wafer point mode should not build ACS-specific parameter by reflection");
    AssertFalse(source.Contains("IsAcsControlCard", StringComparison.Ordinal), "wafer point mode should not detect ACS by vendor name");
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: wafer migration tests fail because the file still uses ACS-specific reflection and vendor checks.

- [ ] **Step 3: Replace ACS-only point-mode decision**

In `SensorMotionControlModel.ExecutePointMoving`, replace:

```csharp
var useAcsCoordinateArrayPulse = UseAcsLciCoordinateArrayPulse(controlCard);
```

with:

```csharp
var triggerCapability = ResolvePointTriggerCapability(controlCard);
var useSynchronizedPointTrigger = triggerCapability != SynchronizedTriggerCapabilities.None;
```

Replace uses of `useAcsCoordinateArrayPulse` in `BuildPointExecutionPlan(...)` and optimal-path checks with `useSynchronizedPointTrigger`.

- [ ] **Step 4: Add capability resolver**

Replace `UseAcsLciCoordinateArrayPulse` and `IsAcsControlCard` with:

```csharp
private static SynchronizedTriggerCapabilities ResolvePointTriggerCapability(ControlCardBase controlCard)
{
    if (controlCard is not ISynchronizedTriggerCard triggerCard)
    {
        return SynchronizedTriggerCapabilities.None;
    }

    if (triggerCard.TriggerCapabilities.HasFlag(SynchronizedTriggerCapabilities.CoordinateArrayPulse))
    {
        return SynchronizedTriggerCapabilities.CoordinateArrayPulse;
    }

    if (triggerCard.TriggerCapabilities.HasFlag(SynchronizedTriggerCapabilities.PositionCompare))
    {
        return SynchronizedTriggerCapabilities.PositionCompare;
    }

    return SynchronizedTriggerCapabilities.None;
}
```

- [ ] **Step 5: Replace ACS-specific execution call**

Replace the block:

```csharp
if (useAcsCoordinateArrayPulse)
{
    try
    {
        trackingCompleted = ExecuteAcsLciCoordinateArrayPulsePoints(
            controlCard,
            orderedLocusInfos,
            trackingRunId);
    }
    finally
    {
        PublishPointCollectionFinishEvent(Calib);
    }

    return;
}
```

with:

```csharp
if (useSynchronizedPointTrigger)
{
    try
    {
        trackingCompleted = ExecuteSynchronizedPointTrigger(
            controlCard,
            triggerCapability,
            orderedLocusInfos,
            trackingRunId);
    }
    finally
    {
        PublishPointCollectionFinishEvent(Calib);
    }

    return;
}
```

- [ ] **Step 6: Add common synchronized point execution method**

Replace `ExecuteAcsLciCoordinateArrayPulsePoints` and `TryRunLciCoordinateArrayPulse` with:

```csharp
private bool ExecuteSynchronizedPointTrigger(
    ControlCardBase controlCard,
    SynchronizedTriggerCapabilities triggerCapability,
    IReadOnlyList<LocusInfo> orderedLocusInfos,
    string trackingRunId)
{
    if (orderedLocusInfos.Count < 2)
    {
        Logs.LogWarning("同步点触发至少需要两个有效点。");
        return false;
    }

    if (controlCard is not ISynchronizedTriggerCard triggerCard)
    {
        Logs.LogWarning("控制卡不支持同步点触发能力。");
        return false;
    }

    var request = BuildPointTriggerRequest(triggerCapability, orderedLocusInfos);
    if (!triggerCard.RunSynchronizedTrigger(request, out var result, out var message))
    {
        Logs.LogWarning($"同步点触发执行失败：{message}");
        return false;
    }

    if (!string.IsNullOrWhiteSpace(result.Message))
    {
        Logs.LogInfo(result.Message);
    }
    else if (!string.IsNullOrWhiteSpace(message))
    {
        Logs.LogInfo(message);
    }

    var lastIndex = orderedLocusInfos.Count - 1;
    var lastPoint = orderedLocusInfos[lastIndex];
    _trajectoryTrackingPublisher.PublishProgress(trackingRunId, orderedLocusInfos.Count, lastIndex);
    _trajectoryTrackingPublisher.PublishTarget(trackingRunId, lastIndex, lastPoint.TargetX, lastPoint.TargetY);
    return true;
}

private SynchronizedTriggerRequest BuildPointTriggerRequest(
    SynchronizedTriggerCapabilities triggerCapability,
    IReadOnlyList<LocusInfo> orderedLocusInfos)
{
    var request = new SynchronizedTriggerRequest
    {
        Mode = triggerCapability == SynchronizedTriggerCapabilities.CoordinateArrayPulse
            ? SynchronizedTriggerMode.CoordinateArrayPulse
            : SynchronizedTriggerMode.PositionCompare,
        Axes = new List<En_AxisNum> { En_AxisNum.X, En_AxisNum.Y },
        Points = orderedLocusInfos.Select(item => new Point(item.TargetX, item.TargetY)).ToList(),
        BufferNo = AcsLciPulseParam.CoordinateArrayBufferNo,
        PulseWidth = AcsLciPulseParam.PulseWidth,
        TriggerWindow = AcsLciPulseParam.CoordinateArrayMultiAxWinSize,
        Velocity = AcsLciPulseParam.CoordinateArrayVelocity,
        Timeout = AcsLciPulseParam.Timeout,
        PositionCompareParam = PosComparisonParam ?? new PosComparisonOutputParam()
    };

    return request;
}
```

- [ ] **Step 7: Remove reflection helper methods**

Delete these methods from `SensorMotionControlModel.cs`:

```csharp
private bool TryRunLciCoordinateArrayPulse(...)
private bool TryGetAcsLciCoordinateArrayPulseMethod(...)
private object BuildAcsLciCoordinateArrayPulseParam(...)
private static bool UseAcsLciCoordinateArrayPulse(ControlCardBase controlCard)
private static bool IsAcsControlCard(ControlCardBase controlCard)
```

Keep line-mode ACS LCI helpers if they are still used by line scanning; only remove the point-mode reflection path.

- [ ] **Step 8: Build tests and related projects**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: wafer migration tests pass and all previous ACS tests pass. If the custom project is available in the solution build graph without locked outputs, also build the `Custom.WaferFlatnessMeasure` project.

### Task 7: Final Verification

**Files:**
- No source edits unless verification exposes a specific compile or test failure.

- [ ] **Step 1: Build base ControlCard**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

- [ ] **Step 2: Build ACS**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

- [ ] **Step 3: Build Googol**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

- [ ] **Step 4: Build and run ACS tests**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected: exit code 0 and every line starts with `PASS` except the final summary line.

- [ ] **Step 5: Review capability pollution boundaries**

Run:

```powershell
rg -n "LineInterpoMovingXseg|ArcInterpoMovingXseg|TryRunLciCoordinateArrayPulse|TryRunLciFixedDistancePulseXseg" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs"
```

Expected: no matches. ACS-specific methods stay in the ACS project.

## Self-Review Notes

- Spec coverage: the plan covers base safety optimization, optional capability interfaces, ACS adapter, Googol adapter, and wafer point-mode migration.
- Scope: implementation is split into stages so base fixes can land before adapter migration if build locks or project dependencies block later stages.
- Type consistency: interface names, request/result names, and capability enum names are consistent across tests and implementation snippets.
- Git note: this workspace currently reports `fatal: not a git repository`, so commit steps are omitted from task checklists.

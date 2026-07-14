# ACS Line Interpolation LCI Pulse Output Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add optional per-move ACS LCI fixed-distance pulse output to line interpolation while preserving the current non-pulse PTP/e script.

**Architecture:** `LineInterPoParam` gains a hardware-neutral `LineInterpolationPulseOutputParam` option. ACS line interpolation passes that option into `InterpolationBufferMotion`, which branches script generation only when the option is enabled. Tests cover the public model, call propagation, disabled script preservation, enabled LCI script flow, output routing, and move-length defaulting.

**Tech Stack:** C#/.NET 8 WPF projects, ACS.SPiiPlusNET, existing console-style ACS test project.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
  - Add common `LineInterpolationPulseOutputParam` model.
  - Add nullable `PulseOutput` property to `LineInterPoParam`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
  - Pass `param.PulseOutput` into ACS line script builders.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InterpolationBufferMotion.cs`
  - Add pulse-aware overloads.
  - Keep old overloads as compatibility wrappers.
  - Implement `ValidateLineInterpolationPulseOutput`, pulse end-distance resolution, and enabled LCI script branch.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Add source-level model and call-propagation tests.
  - Add enabled pulse script tests.
  - Keep disabled script assertions aligned with the current non-pulse script.

---

### Task 1: Add failing model and call-propagation tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add the test registrations**

Insert these `Run` calls immediately after the existing line:

```csharp
Run("ACS line interpolation Buffer matches LCI PTP reference script", TestAcsLineInterpolationBufferMatchesLciPtpReferenceScript);
```

```csharp
Run("ACS line interpolation exposes per-move LCI pulse output", TestAcsLineInterpolationExposesPerMoveLciPulseOutput);
Run("ACS line interpolation passes pulse output into Buffer script builders", TestAcsLineInterpolationPassesPulseOutputToBufferScripts);
```

- [ ] **Step 2: Preserve the current disabled script expectation**

In `AssertLineInterpolationPtpReferenceFlow`, remove this assertion because the current non-pulse script does not declare an LCI channel variable:

```csharp
AssertContains(script, "int ch", $"{label} should keep the LCI channel variable from the reference script");
```

Keep these existing disabled-mode assertions in the same helper:

```csharp
AssertFalse(script.Contains("FixedDistPulse", StringComparison.Ordinal), $"{label} should not add fixed-distance pulse setup beyond the reference script");
AssertFalse(script.Contains("LaserEnable", StringComparison.Ordinal), $"{label} should not add laser enable outside the reference script");
AssertFalse(script.Contains("GetPulseCounts", StringComparison.Ordinal), $"{label} should not read pulse counts outside the reference script");
```

- [ ] **Step 3: Add source-level tests**

Insert these functions immediately after `AssertLineInterpolationPtpReferenceFlow`:

```csharp
void TestAcsLineInterpolationExposesPerMoveLciPulseOutput()
{
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var lineParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterPoParam");
    var pulseParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterpolationPulseOutputParam");

    AssertContains(axisModelSource, "public class LineInterpolationPulseOutputParam", "line interpolation should expose a common pulse output model");
    AssertContains(lineParamSource, "public LineInterpolationPulseOutputParam? PulseOutput", "line interpolation params should carry optional per-move pulse output settings");
    AssertContains(pulseParamSource, "public bool IsEnabled", "pulse output model should have an enable flag");
    AssertContains(pulseParamSource, "public double PulseWidth", "pulse output model should carry LCI pulse width");
    AssertContains(pulseParamSource, "public double Interval", "pulse output model should carry fixed-distance interval");
    AssertContains(pulseParamSource, "public double StartDistance", "pulse output model should carry pulse start distance");
    AssertContains(pulseParamSource, "public double EndDistance", "pulse output model should carry pulse end distance");
    AssertContains(pulseParamSource, "public bool RouteConfigOutput", "pulse output model should carry output routing intent");
    AssertContains(pulseParamSource, "public int ConfigOutputIndex", "pulse output model should carry ACS ConfigOut index");
    AssertContains(pulseParamSource, "public int ConfigOutputCode", "pulse output model should carry ACS ConfigOut code");
}

void TestAcsLineInterpolationPassesPulseOutputToBufferScripts()
{
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var basicLineBody = ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)");
    var xsegLineBody = ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)");

    AssertContains(basicLineBody, "BuildLineInterpolationBufferScript(axes, startPoint, target, velocity, param.PulseOutput)",
        "ordinary line interpolation should pass per-move pulse output settings to the script builder");
    AssertContains(xsegLineBody, "BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity, param.PulseOutput)",
        "XSEG line interpolation should pass per-move pulse output settings to the script builder");
}
```

- [ ] **Step 4: Run the ACS tests to verify the new tests fail**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: FAIL with a message containing `line interpolation should expose a common pulse output model`.

- [ ] **Step 5: Record git state**

Run:

```powershell
git status --short
```

Expected in this workspace: `fatal: not a git repository (or any of the parent directories): .git`. If a worker runs this plan in a valid checkout, commit the test change with:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs
git commit -m "test: cover ACS line interpolation pulse output contract"
```

---

### Task 2: Add the public pulse output model

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisModel.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Insert the pulse output model**

In `AxisModel.cs`, insert this class immediately before `public class LineInterPoParam`:

```csharp
    /// <summary>
    /// 直线插补脉冲输出参数
    /// </summary>
    public class LineInterpolationPulseOutputParam
    {
        /// <summary>
        /// 是否启用脉冲输出
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 脉冲宽度，单位 ms
        /// </summary>
        public double PulseWidth { get; set; } = 0.01d;

        /// <summary>
        /// 固定距离间隔，单位为插补用户单位
        /// </summary>
        public double Interval { get; set; } = 1d;

        /// <summary>
        /// 脉冲起始距离
        /// </summary>
        public double StartDistance { get; set; }

        /// <summary>
        /// 脉冲结束距离，小于等于起始距离时由 ACS 实现按线段长度计算
        /// </summary>
        public double EndDistance { get; set; }

        /// <summary>
        /// 是否将 LCI 通道路由到 ConfigOut
        /// </summary>
        public bool RouteConfigOutput { get; set; } = true;

        /// <summary>
        /// ConfigOut 输出序号
        /// </summary>
        public int ConfigOutputIndex { get; set; }

        /// <summary>
        /// ConfigOut 输出功能码
        /// </summary>
        public int ConfigOutputCode { get; set; } = 7;
    }
```

- [ ] **Step 2: Add the nullable pulse output property**

In `LineInterPoParam`, insert this property after the `Timeout` property:

```csharp
        /// <summary>
        /// 可选脉冲输出参数，启用后由支持的控制卡在插补运动中同步输出脉冲。
        /// </summary>
        public LineInterpolationPulseOutputParam? PulseOutput { get; set; }
```

- [ ] **Step 3: Run tests and verify the next failure**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: FAIL with a message containing `ordinary line interpolation should pass per-move pulse output settings to the script builder`.

- [ ] **Step 4: Record git state**

Run:

```powershell
git status --short
```

Expected in this workspace: `fatal: not a git repository (or any of the parent directories): .git`. If a worker runs this plan in a valid checkout, commit the model change with:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs
git commit -m "feat: add line interpolation pulse output model"
```

---

### Task 3: Add failing enabled-pulse script tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add enabled-pulse test registrations**

Insert these `Run` calls immediately after the registrations added in Task 1:

```csharp
Run("ACS line interpolation pulse output script matches fixed-distance LCI flow", TestAcsLineInterpolationPulseOutputScriptMatchesFixedDistanceLciFlow);
Run("ACS line interpolation pulse output defaults pulse window to move length", TestAcsLineInterpolationPulseOutputDefaultsPulseWindowToMoveLength);
```

- [ ] **Step 2: Add enabled-pulse script tests**

Insert these functions immediately after `TestAcsLineInterpolationPassesPulseOutputToBufferScripts`:

```csharp
void TestAcsLineInterpolationPulseOutputScriptMatchesFixedDistanceLciFlow()
{
    var axes = new[] { (Axis)2, (Axis)0 };
    var startPoint = new[] { 10d, 10d };
    var target = new[] { 100d, 100d };
    var pulseOutput = new LineInterpolationPulseOutputParam
    {
        IsEnabled = true,
        PulseWidth = 5d,
        Interval = 1d,
        StartDistance = 3d,
        EndDistance = 26d,
        RouteConfigOutput = true,
        ConfigOutputIndex = 2,
        ConfigOutputCode = 7
    };

    var script = AcsControlCard.BuildLineInterpolationBufferScript(axes, startPoint, target, 20d, pulseOutput);

    AssertContains(script, "global int RY_LCI_CHANNEL", "pulse line script should expose the LCI channel diagnostic");
    AssertContains(script, "global int RY_LCI_PULSE_COUNT", "pulse line script should expose the LCI pulse count diagnostic");
    AssertContains(script, "int AxX = 2", "pulse line script should map X axis");
    AssertContains(script, "int AxY = 0", "pulse line script should map Y axis");
    AssertContains(script, "real PulseWidth", "pulse line script should declare pulse width");
    AssertContains(script, "real Interval", "pulse line script should declare interval");
    AssertContains(script, "real PulseStartPos, PulseEndPos", "pulse line script should declare pulse window");
    AssertContains(script, "int ch", "pulse line script should declare the LCI channel variable");
    AssertContains(script, "lc.SetSafetyMasks(1, 1)", "pulse line script should ignore LCI safety errors like the reference script");
    AssertContains(script, "PulseWidth = 5", "pulse line script should assign pulse width");
    AssertContains(script, "Interval = 1", "pulse line script should assign interval");
    AssertContains(script, "PulseStartPos = 3", "pulse line script should assign pulse start distance");
    AssertContains(script, "PulseEndPos = 26", "pulse line script should assign pulse end distance");
    AssertContains(script, "lc.SetMotionAxes(AxX, AxY)", "pulse line script should set LCI motion axes");
    AssertContains(script, "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)", "pulse line script should initialize fixed-distance pulse mode");
    AssertContains(script, "lc.SetConfigOut(2, ch, 7)", "pulse line script should route LCI channel to ConfigOut when requested");
    AssertContains(script, "lc.LaserEnable()", "pulse line script should enable the laser before processing motion");
    AssertContains(script, "lc.LaserDisable()", "pulse line script should disable the laser after processing motion");
    AssertContains(script, "RY_LCI_CHANNEL = ch", "pulse line script should store the LCI channel");
    AssertContains(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "pulse line script should store pulse count");
    AssertContains(script, "lc.Stop(ch)", "pulse line script should stop the active LCI channel");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "lc.Init()",
        "pulse line script should move to start before initializing LCI");
    AssertContainsBefore(script, "lc.Init()", "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)",
        "pulse line script should initialize LCI before fixed-distance pulse mode");
    AssertContainsBefore(script, "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)", "lc.LaserEnable()",
        "pulse line script should configure pulse mode before laser enable");
    AssertContainsBefore(script, "lc.LaserEnable()", "PTP/e (AxX, AxY), XStopPos, YStopPos",
        "pulse line script should enable laser before the stop-position motion");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "lc.LaserDisable()",
        "pulse line script should disable laser after the stop-position motion");
}

void TestAcsLineInterpolationPulseOutputDefaultsPulseWindowToMoveLength()
{
    var axes = new[] { (Axis)0, (Axis)1 };
    var startPoint = new[] { 0d, 0d };
    var target = new[] { 3d, 4d };
    var pulseOutput = new LineInterpolationPulseOutputParam
    {
        IsEnabled = true,
        PulseWidth = 0.5d,
        Interval = 1d,
        StartDistance = 0d,
        EndDistance = 0d,
        RouteConfigOutput = false
    };

    var script = AcsControlCard.BuildLineInterpolationBufferScript(axes, startPoint, target, 10d, pulseOutput);

    AssertContains(script, "PulseEndPos = 5", "line interpolation pulse output should default end distance to vector move length");
    AssertFalse(script.Contains("lc.SetConfigOut", StringComparison.Ordinal), "line interpolation pulse output should skip ConfigOut routing when disabled");
}
```

- [ ] **Step 3: Run tests and verify the overload failure**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: FAIL to compile with a message containing `No overload for method 'BuildLineInterpolationBufferScript' takes 5 arguments`.

- [ ] **Step 4: Record git state**

Run:

```powershell
git status --short
```

Expected in this workspace: `fatal: not a git repository (or any of the parent directories): .git`. If a worker runs this plan in a valid checkout, commit the enabled-pulse tests with:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs
git commit -m "test: cover ACS line interpolation LCI pulse script"
```

---

### Task 4: Implement pulse-aware ACS line script generation

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InterpolationBufferMotion.cs`
- Test: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Pass pulse output from ordinary line interpolation**

In `LineInterpolation.cs`, replace this line inside `LineInterpoMoving`:

```csharp
var script = BuildLineInterpolationBufferScript(axes, startPoint, target, velocity);
```

with:

```csharp
var script = BuildLineInterpolationBufferScript(axes, startPoint, target, velocity, param.PulseOutput);
```

- [ ] **Step 2: Pass pulse output from XSEG line interpolation**

In `LineInterpolation.cs`, replace this line inside `LineInterpoMovingXseg`:

```csharp
var script = BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity);
```

with:

```csharp
var script = BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity, param.PulseOutput);
```

- [ ] **Step 3: Replace the line script builder overload block**

In `InterpolationBufferMotion.cs`, replace the existing `BuildLineInterpolationBufferScript` and `BuildXsegLineInterpolationBufferScript` methods with this overload set:

```csharp
    public static string BuildLineInterpolationBufferScript(Axis[] axes, double[] target, double velocity)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint: null, target, velocity, pulseOutput: null);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity)
    {
        return BuildLineInterpolationBufferScript(axes, startPoint, target, velocity, pulseOutput: null);
    }

    public static string BuildLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, velocity, pulseOutput);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity)
    {
        return BuildXsegLineInterpolationBufferScript(axes, startPoint, target, velocity, pulseOutput: null);
    }

    public static string BuildXsegLineInterpolationBufferScript(
        Axis[] axes,
        double[] startPoint,
        double[] target,
        double velocity,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var lineAxes = TakeLineAxes(axes);
        ValidateInterpolationAxesAndPoint(lineAxes, startPoint, nameof(startPoint));
        ValidateInterpolationAxesAndPoint(lineAxes, target, nameof(target));

        return BuildLinePtpLciInitBufferScript(lineAxes, startPoint, target, velocity, pulseOutput);
    }
```

- [ ] **Step 4: Replace `BuildLinePtpLciInitBufferScript`**

In `InterpolationBufferMotion.cs`, replace the existing private `BuildLinePtpLciInitBufferScript` method with:

```csharp
    private static string BuildLinePtpLciInitBufferScript(
        Axis[] axes,
        double[]? startPoint,
        double[] target,
        double velocity,
        LineInterpolationPulseOutputParam? pulseOutput)
    {
        var isPulseOutputEnabled = IsLineInterpolationPulseOutputEnabled(pulseOutput);
        if (isPulseOutputEnabled)
        {
            ValidateLineInterpolationPulseOutput(pulseOutput!);
        }

        var builder = new StringBuilder();
        if (isPulseOutputEnabled)
        {
            builder.AppendLine("global int RY_LCI_CHANNEL");
            builder.AppendLine("global int RY_LCI_PULSE_COUNT");
            builder.AppendLine();
        }

        builder.AppendLine($"int AxX = {(int)axes[0]}");
        builder.AppendLine($"int AxY = {(int)axes[1]}");
        builder.AppendLine();

        if (isPulseOutputEnabled)
        {
            builder.AppendLine("real PulseWidth");
            builder.AppendLine("real Interval");
        }

        builder.AppendLine("real XStartPos, YStartPos");
        builder.AppendLine("real XStopPos, YStopPos");

        if (isPulseOutputEnabled)
        {
            builder.AppendLine("real PulseStartPos, PulseEndPos");
            builder.AppendLine("int ch");
        }

        builder.AppendLine();

        if (isPulseOutputEnabled)
        {
            builder.AppendLine("lc.SetSafetyMasks(1, 1)");
            builder.AppendLine();
        }

        builder.AppendLine("ENABLE (AxX, AxY)");
        builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxX", 10d);
        builder.AppendLine();
        AppendAxisMotionTuning(builder, "AxY", 10d);
        builder.AppendLine();
        AppendLinePtpStartPositionAssignments(builder, startPoint);
        builder.AppendLine();
        builder.AppendLine("PTP/e (AxX, AxY), XStartPos, YStartPos");
        builder.AppendLine();
        builder.AppendLine("lc.Init()");
        builder.AppendLine();

        if (isPulseOutputEnabled)
        {
            AppendLineInterpolationPulseTimingAssignments(builder, pulseOutput!);
            builder.AppendLine();
        }

        AppendLinePtpStopPositionAssignments(builder, target);

        if (isPulseOutputEnabled)
        {
            builder.AppendLine();
            AppendLineInterpolationPulseWindowAndMode(builder, pulseOutput!, startPoint, target);
            builder.AppendLine();
            builder.AppendLine("lc.LaserEnable()");
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine();
        }

        builder.AppendLine("PTP/e (AxX, AxY), XStopPos, YStopPos");

        if (isPulseOutputEnabled)
        {
            builder.AppendLine("lc.LaserDisable()");
            builder.AppendLine();
            builder.AppendLine("RY_LCI_CHANNEL = ch");
            builder.AppendLine("RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)");
            builder.AppendLine();
            builder.AppendLine("DISP \"Pulse count = %d\", RY_LCI_PULSE_COUNT");
            builder.AppendLine();
            builder.AppendLine("lc.Stop(ch)");
        }

        builder.AppendLine("STOP");
        return builder.ToString();
    }
```

- [ ] **Step 5: Add pulse helper methods**

In `InterpolationBufferMotion.cs`, insert these helper methods immediately after `AppendLinePtpStopPositionAssignments`:

```csharp
    private static void AppendLineInterpolationPulseTimingAssignments(
        StringBuilder builder,
        LineInterpolationPulseOutputParam pulseOutput)
    {
        builder.AppendLine($"PulseWidth = {FormatAcsNumber(pulseOutput.PulseWidth)}");
        builder.AppendLine($"Interval = {FormatAcsNumber(pulseOutput.Interval)}");
    }

    private static void AppendLineInterpolationPulseWindowAndMode(
        StringBuilder builder,
        LineInterpolationPulseOutputParam pulseOutput,
        double[]? startPoint,
        double[] target)
    {
        var pulseEndDistance = ResolveLineInterpolationPulseEndDistance(pulseOutput, startPoint, target);
        builder.AppendLine($"PulseStartPos = {FormatAcsNumber(pulseOutput.StartDistance)}");
        builder.AppendLine($"PulseEndPos = {FormatAcsNumber(pulseEndDistance)}");
        builder.AppendLine();
        builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
        builder.AppendLine("ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)");

        if (pulseOutput.RouteConfigOutput)
        {
            builder.AppendLine($"lc.SetConfigOut({pulseOutput.ConfigOutputIndex}, ch, {pulseOutput.ConfigOutputCode})");
        }
    }

    private static double ResolveLineInterpolationPulseEndDistance(
        LineInterpolationPulseOutputParam pulseOutput,
        double[]? startPoint,
        double[] target)
    {
        if (pulseOutput.EndDistance > pulseOutput.StartDistance)
        {
            return pulseOutput.EndDistance;
        }

        if (startPoint == null)
        {
            throw new ArgumentException("A start point is required when pulse end distance defaults to the move length.", nameof(startPoint));
        }

        var deltaX = target[0] - startPoint[0];
        var deltaY = target[1] - startPoint[1];
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static bool IsLineInterpolationPulseOutputEnabled(LineInterpolationPulseOutputParam? pulseOutput)
    {
        return pulseOutput?.IsEnabled == true;
    }

    private static void ValidateLineInterpolationPulseOutput(LineInterpolationPulseOutputParam pulseOutput)
    {
        if (!IsFinite(pulseOutput.PulseWidth) || pulseOutput.PulseWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.PulseWidth), "Pulse width must be positive.");
        }

        if (!IsFinite(pulseOutput.Interval) || pulseOutput.Interval <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.Interval), "Interval must be positive.");
        }

        if (!IsFinite(pulseOutput.StartDistance) || !IsFinite(pulseOutput.EndDistance))
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.EndDistance), "Start and end distances must be finite.");
        }

        if (pulseOutput.RouteConfigOutput && !IsValidLciConfigOutputIndex(pulseOutput.ConfigOutputIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(pulseOutput.ConfigOutputIndex), "Config output index must be 0..7 or 10.");
        }
    }
```

- [ ] **Step 6: Run tests and verify pass**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: PASS with output ending in `ACS PEG/DataCollection tests passed.`

- [ ] **Step 7: Record git state**

Run:

```powershell
git status --short
```

Expected in this workspace: `fatal: not a git repository (or any of the parent directories): .git`. If a worker runs this plan in a valid checkout, commit the implementation with:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs
git commit -m "feat: enable ACS line interpolation LCI pulse output"
```

---

### Task 5: Build verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Build the ACS project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj --no-restore
```

Expected: output contains `Build succeeded.` and `0 Error(s)`.

- [ ] **Step 2: Build the ACS test project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: output contains `Build succeeded.` and `0 Error(s)`.

- [ ] **Step 3: Run the ACS test executable**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: PASS with output ending in `ACS PEG/DataCollection tests passed.`

- [ ] **Step 4: Inspect the generated line scripts manually through tests if a failure appears**

Run this command only when a test assertion shows the script text is wrong:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore
```

Expected: the assertion message names the missing or misordered ACSPL+ fragment; compare that fragment with the script generation in `InterpolationBufferMotion.cs` and keep disabled mode free of `FixedDistPulse`, `LaserEnable`, and `GetPulseCounts`.

---

## Self-Review

- Spec coverage: Task 2 covers the common model and `LineInterPoParam.PulseOutput`; Task 4 covers the ACS script branch, validation, ConfigOut routing, diagnostics, cleanup, and move-length defaulting; Tasks 1 and 3 cover regression tests for disabled and enabled behavior; Task 5 covers build and runtime verification.
- Type consistency: The plan consistently uses `LineInterpolationPulseOutputParam`, `LineInterPoParam.PulseOutput`, `BuildLineInterpolationBufferScript(..., pulseOutput)`, and `BuildXsegLineInterpolationBufferScript(..., pulseOutput)`.
- Current workspace note: `git status` currently fails because this folder is not recognized as a normal Git repository, so commit steps are documented for valid checkouts but are not expected to succeed here.


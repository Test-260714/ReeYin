# ACS LCI Coordinate Array Point Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ACS point-trajectory path that uses LCI Coordinate Array Pulse and preserves generated point order.

**Architecture:** Add a new coordinate-array LCI parameter/result/runner/script builder to the ACS control-card project, using controller-side arrays written through the ACS .NET API before buffer execution. Extend the wafer point execution path to detect ACS cards, preserve input order, convert `LocusInfo` points to ACS points by reflection, and keep the existing Googol path for non-ACS cards. Add small wafer config fields for coordinate-array window and velocity.

**Tech Stack:** C# .NET 8 WPF, ACS.SPiiPlusNET buffer programs, ACSPL+ LCI, Prism `BindableBase`, existing console-style test harness.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs`: define coordinate-array pulse request/result models, script generation, ACS array writes, runner, and validation.
- Modify `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/AcsLciFixedDistancePulseConfig.cs`: add coordinate-array window and velocity settings reused by point mode.
- Modify `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorMotionControlModel.cs`: route ACS point execution through the new runner and keep Googol queue execution for non-ACS cards.
- Modify `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml`: expose coordinate-array point settings in the existing ACS LCI settings group.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`: add script, runner, config, UI binding, and wafer integration assertions.

---

### Task 1: ACS Coordinate Array Pulse Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add test registration lines**

Add these `Run(...)` calls immediately after the existing `ACS LCI segment circle runner uses buffer pipeline` line:

```csharp
Run("ACS LCI coordinate array pulse script matches reference XSEG flow", TestAcsLciCoordinateArrayPulseScriptMatchesReferenceXsegFlow);
Run("ACS LCI coordinate array pulse runner writes arrays before run", TestAcsLciCoordinateArrayPulseRunnerWritesArraysBeforeRun);
```

- [ ] **Step 2: Add the failing script and runner tests**

Paste these methods after `TestAcsLciSegmentCircleRunnerUsesBufferPipeline()`:

```csharp
void TestAcsLciCoordinateArrayPulseScriptMatchesReferenceXsegFlow()
{
    var param = new AcsLciCoordinateArrayPulseParam
    {
        AxisX = 0,
        AxisY = 1,
        PulseWidth = 0.01,
        MultiAxWinSize = 0.001,
        Velocity = 25,
        RouteConfigOutput = true,
        ConfigOutputIndex = 0,
        ConfigOutputCode = 7,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 0),
            new AcsPoint2D(10, 20),
            new AcsPoint2D(25, 20)
        }
    };

    var script = AcsControlCard.BuildLciCoordinateArrayPulseScript(param);

    AssertContains(script, "global LCI lc", "coordinate array script should declare the LCI object");
    AssertContains(script, "global int RY_LCI_CHANNEL", "coordinate array script should expose the selected channel");
    AssertContains(script, "global int RY_LCI_PULSE_COUNT", "coordinate array script should expose the pulse count");
    AssertContains(script, "global real RY_LCI_XCOORD(4)", "coordinate array script should declare X coordinate array sized from points");
    AssertContains(script, "global real RY_LCI_YCOORD(4)", "coordinate array script should declare Y coordinate array sized from points");
    AssertContains(script, "int PointsNum", "coordinate array script should declare the point count variable");
    AssertContains(script, "int PointIndex", "coordinate array script should declare the point loop variable");
    AssertContains(script, "PulseWidth = 0.01", "coordinate array script should set pulse width from parameter");
    AssertContains(script, "PointsNum = 4", "coordinate array script should set point count from parameter");
    AssertContains(script, "lc.SetSafetyMasks(1, 1)", "coordinate array script should ignore safety/fault inputs like existing LCI scripts");
    AssertContains(script, "lc.Init()", "coordinate array script should initialize LCI");
    AssertContains(script, "ENABLE (AxX, AxY)", "coordinate array script should enable the motion axes");
    AssertContains(script, "TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )", "coordinate array script should wait for axes to enable");
    AssertContains(script, "VEL(AxX) = 25", "coordinate array script should set X velocity from parameter");
    AssertContains(script, "VEL(AxY) = 25", "coordinate array script should set Y velocity from parameter");
    AssertContains(script, "PTP/e (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)", "coordinate array script should move to the first point");
    AssertContains(script, "lc.SetMotionAxes(AxX, AxY)", "coordinate array script should set motion axes");
    AssertContains(script, "lc.MultiAxWinSize = 0.001", "coordinate array script should set coordinate trigger window");
    AssertContains(script, "ch = lc.CoordinateArrPulse(PointsNum, PulseWidth, RY_LCI_XCOORD, RY_LCI_YCOORD)", "coordinate array script should initialize coordinate-array pulse");
    AssertContains(script, "lc.SetConfigOut(0, ch, 7)", "coordinate array script should route pulse output");
    AssertContains(script, "lc.LaserEnable()", "coordinate array script should enable laser before XSEG motion");
    AssertContains(script, "XSEG (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)", "coordinate array script should start XSEG at the first point");
    AssertContains(script, "PointIndex = 1", "coordinate array script should start LINE loop from the second point");
    AssertContains(script, "while PointIndex < PointsNum", "coordinate array script should loop through remaining points");
    AssertContains(script, "LINE (AxX, AxY), RY_LCI_XCOORD(PointIndex), RY_LCI_YCOORD(PointIndex)", "coordinate array script should append points in array order");
    AssertContains(script, "PointIndex = PointIndex + 1", "coordinate array script should increment point index");
    AssertContains(script, "ENDS (AxX, AxY)", "coordinate array script should close XSEG");
    AssertContains(script, "till GSEG(AxX) = -1", "coordinate array script should wait for segmented motion end");
    AssertContains(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "coordinate array script should read pulse count before stopping");
    AssertContains(script, "lc.Stop(ch)", "coordinate array script should stop the LCI channel");
    AssertContainsBefore(script, "lc.CoordinateArrPulse", "lc.LaserEnable()", "coordinate array mode should be initialized before laser enable");
    AssertContainsBefore(script, "lc.LaserEnable()", "XSEG (AxX, AxY)", "laser should be enabled before XSEG motion");
    AssertContainsBefore(script, "PointIndex = 1", "LINE (AxX, AxY)", "LINE loop should start after index initialization");
    AssertContainsBefore(script, "ENDS (AxX, AxY)", "till GSEG(AxX) = -1", "script should close XSEG before waiting for completion");
    AssertContainsBefore(script, "lc.GetPulseCounts(ch)", "lc.Stop(ch)", "script should read pulse count before stopping the channel");
}

void TestAcsLciCoordinateArrayPulseRunnerWritesArraysBeforeRun()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    var methodBody = ReadMethodBody(source, "public bool TryRunLciCoordinateArrayPulse(");

    AssertContains(methodBody, "BuildLciCoordinateArrayPulseScript(param)", "coordinate array runner should build the ACSPL+ script from the request");
    AssertContains(methodBody, "_api.LoadBuffer(buffer", "coordinate array runner should download script to the selected buffer");
    AssertContains(methodBody, "_api.CompileBuffer(buffer)", "coordinate array runner should compile the selected buffer");
    AssertContains(methodBody, "WriteLciCoordinateArrayPulseVariables(param)", "coordinate array runner should write point arrays before running");
    AssertContains(methodBody, "_api.RunBuffer(buffer, null)", "coordinate array runner should run the selected buffer");
    AssertContains(methodBody, "_api.WaitProgramEnd(buffer", "coordinate array runner should wait for the buffer to finish");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "coordinate array runner should read the generated channel");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_PULSE_COUNT\"", "coordinate array runner should read the pulse count");
    AssertContains(methodBody, "TryCleanupLciFixedDistancePulse", "coordinate array runner should attempt laser/channel cleanup on failure");
    AssertContains(source, "_api.WriteVariable(xCoordinates", "coordinate array runner should write X coordinate array");
    AssertContains(source, "_api.WriteVariable(yCoordinates", "coordinate array runner should write Y coordinate array");
    AssertContainsBefore(methodBody, "_api.LoadBuffer(buffer", "_api.CompileBuffer(buffer)", "runner should compile after loading");
    AssertContainsBefore(methodBody, "_api.CompileBuffer(buffer)", "WriteLciCoordinateArrayPulseVariables(param)", "runner should write arrays after compile");
    AssertContainsBefore(methodBody, "WriteLciCoordinateArrayPulseVariables(param)", "_api.RunBuffer(buffer, null)", "runner should run after array writes");
    AssertContainsBefore(methodBody, "_api.WaitProgramEnd(buffer", "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "runner should read globals after the buffer ends");
}
```

- [ ] **Step 3: Run red verification**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: build fails because `AcsLciCoordinateArrayPulseParam`, `BuildLciCoordinateArrayPulseScript`, and `TryRunLciCoordinateArrayPulse` do not exist yet.

- [ ] **Step 4: Commit test changes**

Run:

```powershell
git status --short
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs
git commit -m "test: cover ACS LCI coordinate array pulse"
```

Expected in a valid Git checkout: one commit is created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and continue without committing.

---

### Task 2: ACS Coordinate Array Pulse Runner

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsLciFixedDistancePulse.cs`

- [ ] **Step 1: Add coordinate-array parameter and result models**

Add this class after `AcsLciSegmentCircleParam`:

```csharp
public sealed class AcsLciCoordinateArrayPulseParam
{
    public int BufferNo { get; set; } = 10;

    public int AxisX { get; set; }

    public int AxisY { get; set; } = 1;

    public double PulseWidth { get; set; } = 0.01d;

    public double MultiAxWinSize { get; set; } = 0.001d;

    public double Velocity { get; set; } = 10d;

    public bool RouteConfigOutput { get; set; } = true;

    public int ConfigOutputIndex { get; set; }

    public int ConfigOutputCode { get; set; } = 7;

    public int Timeout { get; set; } = 60000;

    public List<AcsPoint2D> Points { get; set; } =
    [
        new(0d, 0d),
        new(10d, 0d),
        new(10d, 10d)
    ];
}
```

Add this result class after `AcsLciSegmentCircleResult`:

```csharp
public sealed class AcsLciCoordinateArrayPulseResult
{
    public AcsLciCoordinateArrayPulseResult(int channel, int pulseCount, int pointCount, string script)
    {
        Channel = channel;
        PulseCount = pulseCount;
        PointCount = pointCount;
        Script = script;
    }

    public int Channel { get; }

    public int PulseCount { get; }

    public int PointCount { get; }

    public string Script { get; }
}
```

- [ ] **Step 2: Add the runner method**

Add this method after `TryRunLciSegmentCircle(...)`:

```csharp
public bool TryRunLciCoordinateArrayPulse(
    AcsLciCoordinateArrayPulseParam? param,
    out AcsLciCoordinateArrayPulseResult result,
    out string message)
{
    result = new AcsLciCoordinateArrayPulseResult(-1, 0, 0, string.Empty);
    if (param == null)
    {
        message = "ACS LCI coordinate-array pulse parameter cannot be null.";
        return false;
    }

    if (!TryPrepareProgramBuffer(param.BufferNo, out var buffer, out message))
    {
        return false;
    }

    string script;
    try
    {
        script = BuildLciCoordinateArrayPulseScript(param);
    }
    catch (Exception ex)
    {
        message = $"ACS LCI coordinate-array pulse script is invalid: {ex.Message}";
        return false;
    }

    try
    {
        TryStopLciFixedDistancePulseBuffer(buffer);
        _api.LoadBuffer(buffer, script);
        _api.CompileBuffer(buffer);
        WriteLciCoordinateArrayPulseVariables(param);
        _api.RunBuffer(buffer, null);
        _api.WaitProgramEnd(buffer, Math.Max(1000, param.Timeout));

        var channel = _api.ReadIntegerScalar("RY_LCI_CHANNEL", ProgramBuffer.ACSC_NONE);
        var pulseCount = _api.ReadIntegerScalar("RY_LCI_PULSE_COUNT", ProgramBuffer.ACSC_NONE);
        var pointCount = param.Points?.Count ?? 0;
        result = new AcsLciCoordinateArrayPulseResult(channel, pulseCount, pointCount, script);
        message = $"ACS LCI coordinate-array pulse completed. Channel={channel}, PulseCount={pulseCount}, PointCount={pointCount}.";
        return true;
    }
    catch (Exception ex)
    {
        TryCleanupLciFixedDistancePulse(buffer);
        message = $"ACS LCI coordinate-array pulse failed: {ex.Message}; {FormatProgramDiagnostics(param.BufferNo)}";
        Console.WriteLine(message);
        return false;
    }
}
```

- [ ] **Step 3: Add the ACSPL+ script builder**

Add this method after `BuildLciSegmentCircleScript(...)`:

```csharp
public static string BuildLciCoordinateArrayPulseScript(AcsLciCoordinateArrayPulseParam param)
{
    ValidateLciCoordinateArrayPulseParam(param);

    var pointCount = param.Points.Count;
    var builder = new StringBuilder();
    builder.AppendLine("global LCI lc");
    builder.AppendLine("global int RY_LCI_CHANNEL");
    builder.AppendLine("global int RY_LCI_PULSE_COUNT");
    builder.AppendLine($"global real RY_LCI_XCOORD({pointCount})");
    builder.AppendLine($"global real RY_LCI_YCOORD({pointCount})");
    builder.AppendLine();
    builder.AppendLine($"int AxX = {param.AxisX}");
    builder.AppendLine($"int AxY = {param.AxisY}");
    builder.AppendLine();
    builder.AppendLine("real PulseWidth");
    builder.AppendLine("int PointsNum");
    builder.AppendLine("int PointIndex");
    builder.AppendLine("int ch");
    builder.AppendLine();
    builder.AppendLine($"PulseWidth = {FormatAcsNumber(param.PulseWidth)}");
    builder.AppendLine($"PointsNum = {pointCount}");
    builder.AppendLine();
    builder.AppendLine("lc.SetSafetyMasks(1, 1)");
    builder.AppendLine("lc.LaserDisable()");
    builder.AppendLine("lc.Stop()");
    builder.AppendLine("lc.Init()");
    builder.AppendLine();
    builder.AppendLine("ENABLE (AxX, AxY)");
    builder.AppendLine("TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )");
    builder.AppendLine();
    AppendAxisMotionTuning(builder, "AxX", param.Velocity);
    builder.AppendLine();
    AppendAxisMotionTuning(builder, "AxY", param.Velocity);
    builder.AppendLine();
    builder.AppendLine("PTP/e (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)");
    builder.AppendLine("lc.SetMotionAxes(AxX, AxY)");
    builder.AppendLine($"lc.MultiAxWinSize = {FormatAcsNumber(param.MultiAxWinSize)}");
    builder.AppendLine("ch = lc.CoordinateArrPulse(PointsNum, PulseWidth, RY_LCI_XCOORD, RY_LCI_YCOORD)");

    if (param.RouteConfigOutput)
    {
        builder.AppendLine($"lc.SetConfigOut({param.ConfigOutputIndex}, ch, {param.ConfigOutputCode})");
    }

    builder.AppendLine("lc.LaserEnable()");
    builder.AppendLine();
    builder.AppendLine("XSEG (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)");
    builder.AppendLine("PointIndex = 1");
    builder.AppendLine("while PointIndex < PointsNum");
    builder.AppendLine("block");
    builder.AppendLine("    LINE (AxX, AxY), RY_LCI_XCOORD(PointIndex), RY_LCI_YCOORD(PointIndex)");
    builder.AppendLine("    PointIndex = PointIndex + 1");
    builder.AppendLine("end");
    builder.AppendLine("ENDS (AxX, AxY)");
    builder.AppendLine("till GSEG(AxX) = -1");
    builder.AppendLine();
    builder.AppendLine("lc.LaserDisable()");
    builder.AppendLine("RY_LCI_CHANNEL = ch");
    builder.AppendLine("RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)");
    builder.AppendLine("DISP \"Coordinate array pulse count = %d\", RY_LCI_PULSE_COUNT");
    builder.AppendLine("lc.Stop(ch)");
    builder.AppendLine("STOP");
    return builder.ToString();
}
```

- [ ] **Step 4: Add coordinate-array helper methods**

Replace the existing `AppendAxisMotionTuning` method with this overload pair:

```csharp
private static void AppendAxisMotionTuning(StringBuilder builder, string axisName)
{
    AppendAxisMotionTuning(builder, axisName, 10d);
}

private static void AppendAxisMotionTuning(StringBuilder builder, string axisName, double velocity)
{
    var formattedVelocity = FormatAcsNumber(velocity);
    builder.AppendLine($"VEL({axisName}) = {formattedVelocity}");
    builder.AppendLine($"ACC({axisName}) = 10 * VEL({axisName})");
    builder.AppendLine($"DEC({axisName}) = 10 * VEL({axisName})");
    builder.AppendLine($"KDEC({axisName}) = 10 * ACC({axisName})");
    builder.AppendLine($"JERK({axisName}) = 10 * ACC({axisName})");
}
```

Add this method near the other private helpers:

```csharp
private void WriteLciCoordinateArrayPulseVariables(AcsLciCoordinateArrayPulseParam param)
{
    var points = param.Points;
    var xCoordinates = new double[points.Count];
    var yCoordinates = new double[points.Count];

    for (var index = 0; index < points.Count; index++)
    {
        xCoordinates[index] = points[index].X;
        yCoordinates[index] = points[index].Y;
    }

    var lastIndex = points.Count - 1;
    _api.WriteVariable(xCoordinates, "RY_LCI_XCOORD", ProgramBuffer.ACSC_NONE, 0, lastIndex, -1, -1);
    _api.WriteVariable(yCoordinates, "RY_LCI_YCOORD", ProgramBuffer.ACSC_NONE, 0, lastIndex, -1, -1);
}
```

Add this validation method before `ValidateLciSegmentCircleParam(...)`:

```csharp
private static void ValidateLciCoordinateArrayPulseParam(AcsLciCoordinateArrayPulseParam param)
{
    ArgumentNullException.ThrowIfNull(param);

    if (param.AxisX == param.AxisY)
    {
        throw new ArgumentException("Coordinate-array axes must be different.");
    }

    if (!IsFinite(param.PulseWidth) || param.PulseWidth <= 0d)
    {
        throw new ArgumentOutOfRangeException(nameof(param.PulseWidth), "Pulse width must be positive.");
    }

    if (!IsFinite(param.MultiAxWinSize) || param.MultiAxWinSize <= 0d)
    {
        throw new ArgumentOutOfRangeException(nameof(param.MultiAxWinSize), "Coordinate-array trigger window must be positive.");
    }

    if (!IsFinite(param.Velocity) || param.Velocity <= 0d)
    {
        throw new ArgumentOutOfRangeException(nameof(param.Velocity), "Velocity must be positive.");
    }

    if (param.RouteConfigOutput && !IsValidLciConfigOutputIndex(param.ConfigOutputIndex))
    {
        throw new ArgumentOutOfRangeException(nameof(param.ConfigOutputIndex), "Config output index must be 0..7 or 10.");
    }

    if (param.Timeout <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(param.Timeout), "Timeout must be positive.");
    }

    if (param.Points == null || param.Points.Count < 2)
    {
        throw new ArgumentException("At least two coordinate-array points are required.", nameof(param.Points));
    }

    foreach (var point in param.Points)
    {
        if (!IsFinite(point.X) || !IsFinite(point.Y))
        {
            throw new ArgumentException("Coordinate-array points must be finite.", nameof(param.Points));
        }
    }
}
```

- [ ] **Step 5: Run green verification for ACS runner**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: the new coordinate-array ACS tests compile and pass; later wafer integration tests may still fail until the wafer tasks are complete.

- [ ] **Step 6: Commit ACS runner changes**

Run:

```powershell
git status --short
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs
git commit -m "feat: add ACS LCI coordinate array pulse runner"
```

Expected in a valid Git checkout: one commit is created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and continue without committing.

---

### Task 3: Wafer Integration Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add wafer test registration lines**

Add these `Run(...)` calls after `WaferFlatness uses ACS LCI fixed-distance pulse runner`:

```csharp
Run("WaferFlatness exposes ACS LCI coordinate-array point config", TestWaferFlatnessAcsLciCoordinateArrayConfigSurface);
Run("WaferFlatness point mode uses ACS LCI coordinate-array pulse runner", TestWaferFlatnessPointModeUsesAcsLciCoordinateArrayPulseRunner);
Run("WaferFlatness ACS point mode preserves input point order", TestWaferFlatnessAcsPointModePreservesInputOrder);
```

Add this `Run(...)` call after `WaferFlatness config view binds ACS LCI pulse parameters`:

```csharp
Run("WaferFlatness config view binds ACS LCI coordinate-array point parameters", TestWaferFlatnessConfigViewBindsAcsLciCoordinateArrayParameters);
```

- [ ] **Step 2: Add failing wafer config and integration tests**

Paste these methods after `TestWaferFlatnessUsesAcsLciFixedDistancePulseRunner()`:

```csharp
void TestWaferFlatnessAcsLciCoordinateArrayConfigSurface()
{
    var configSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs");

    AssertContains(configSource, "CoordinateArrayMultiAxWinSize", "Wafer ACS LCI config should expose coordinate-array trigger window");
    AssertContains(configSource, "CoordinateArrayVelocity", "Wafer ACS LCI config should expose coordinate-array motion velocity");
    AssertContains(configSource, "0.001d", "Coordinate-array trigger window should default from the manual example");
    AssertContains(configSource, "10d", "Coordinate-array velocity should have a safe default");
}

void TestWaferFlatnessPointModeUsesAcsLciCoordinateArrayPulseRunner()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");

    AssertContains(motionSource, "UseAcsLciCoordinateArrayPulse(controlCard)", "Wafer point mode should detect ACS coordinate-array pulse support");
    AssertContains(motionSource, "ExecuteAcsLciCoordinateArrayPulsePoints", "Wafer point mode should have a dedicated ACS coordinate-array execution path");
    AssertContains(motionSource, "TryRunLciCoordinateArrayPulse", "Wafer point mode should invoke the ACS coordinate-array runner");
    AssertContains(motionSource, "BuildAcsLciCoordinateArrayPulseParam", "Wafer point mode should map wafer points and config into ACS coordinate-array parameters");
    AssertContains(motionSource, "SetAcsLciCoordinateArrayPulsePoints", "Wafer point mode should pass all ordered points to ACS");
    AssertContains(motionSource, "CoordinateArrayMultiAxWinSize", "Wafer point mode should pass coordinate-array trigger window");
    AssertContains(motionSource, "CoordinateArrayVelocity", "Wafer point mode should pass coordinate-array velocity");
}

void TestWaferFlatnessAcsPointModePreservesInputOrder()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");
    var buildPlanBody = ReadMethodBody(motionSource, "private PointExecutionPlan BuildPointExecutionPlan(");
    var executePointBody = ReadMethodBody(motionSource, "public void ExecutePointMoving(");

    AssertContains(buildPlanBody, "bool preserveInputOrder = false", "point execution plan should allow callers to preserve input order");
    AssertContains(buildPlanBody, "CreateTrajectoryModel.IsOptimalPathEnabled && !shouldInsertCalibration && !preserveInputOrder", "shortest-path sorting should be disabled when input order is preserved");
    AssertContains(executePointBody, "var useAcsCoordinateArrayPulse = UseAcsLciCoordinateArrayPulse(controlCard);", "point execution should detect ACS before planning order");
    AssertContains(executePointBody, "BuildPointExecutionPlan(Calib, sourceLocusInfos, useAcsCoordinateArrayPulse)", "ACS point execution should request input-order preservation");
    AssertContainsBefore(executePointBody, "UseAcsLciCoordinateArrayPulse(controlCard)", "BuildPointExecutionPlan(Calib, sourceLocusInfos, useAcsCoordinateArrayPulse)", "ACS detection should happen before execution plan creation");
    AssertContainsBefore(executePointBody, "if (useAcsCoordinateArrayPulse)", "residualOrder.Clear()", "ACS point mode should branch before Googol residual queue execution");
}
```

Paste this method after `TestWaferFlatnessConfigViewBindsAcsLciPulseParameters()`:

```csharp
void TestWaferFlatnessConfigViewBindsAcsLciCoordinateArrayParameters()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml");

    AssertContains(xaml, "ModelParam.AcsLciPulseParam.CoordinateArrayMultiAxWinSize", "Wafer config page should bind ACS coordinate-array trigger window");
    AssertContains(xaml, "ModelParam.AcsLciPulseParam.CoordinateArrayVelocity", "Wafer config page should bind ACS coordinate-array velocity");
    AssertContains(xaml, "坐标数组触发窗口", "Wafer config page should label the coordinate-array window");
    AssertContains(xaml, "坐标数组速度", "Wafer config page should label the coordinate-array velocity");
}
```

- [ ] **Step 3: Run red verification**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: build succeeds, and the console test run fails on the new wafer assertions because wafer config, XAML bindings, and point execution branch are not implemented yet.

Then run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build
```

Expected: new wafer tests fail before implementation; any pre-existing unrelated failures should be recorded separately.

- [ ] **Step 4: Commit wafer test changes**

Run:

```powershell
git status --short
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs
git commit -m "test: cover wafer ACS coordinate array point mode"
```

Expected in a valid Git checkout: one commit is created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and continue without committing.

---

### Task 4: Wafer ACS Coordinate Array Config And XAML

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/AcsLciFixedDistancePulseConfig.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml`

- [ ] **Step 1: Add config properties**

Add these fields and properties after `PulseWidth` in `AcsLciFixedDistancePulseConfig`:

```csharp
private double _coordinateArrayMultiAxWinSize = 0.001d;
public double CoordinateArrayMultiAxWinSize
{
    get => _coordinateArrayMultiAxWinSize;
    set => SetProperty(ref _coordinateArrayMultiAxWinSize, Math.Max(double.Epsilon, value));
}

private double _coordinateArrayVelocity = 10d;
public double CoordinateArrayVelocity
{
    get => _coordinateArrayVelocity;
    set => SetProperty(ref _coordinateArrayVelocity, Math.Max(double.Epsilon, value));
}
```

- [ ] **Step 2: Add XAML bindings**

In `WaferFlatnessConfigView.xaml`, inside the existing `ACS LCI固定距离脉冲` group, add this row immediately after the row that binds `ModelParam.AcsLciPulseParam.PulseWidth` and `ModelParam.AcsLciPulseParam.Interval`:

```xml
<StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
    <hc:NumericUpDown hc:InfoElement.TitlePlacement="Left"
                      hc:InfoElement.Title="坐标数组触发窗口："
                      hc:InfoElement.TitleWidth="130"
                      Width="250"
                      DecimalPlaces="4"
                      Minimum="0.0001"
                      Maximum="100"
                      BorderThickness="0 0 0 1"
                      Style="{StaticResource NumericUpDownExtend}"
                      HorizontalContentAlignment="Center"
                      Value="{Binding ModelParam.AcsLciPulseParam.CoordinateArrayMultiAxWinSize,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
    <hc:NumericUpDown hc:InfoElement.TitlePlacement="Left"
                      hc:InfoElement.Title="坐标数组速度："
                      hc:InfoElement.TitleWidth="120"
                      Width="250"
                      DecimalPlaces="3"
                      Minimum="0.001"
                      Maximum="1000"
                      BorderThickness="0 0 0 1"
                      Style="{StaticResource NumericUpDownExtend}"
                      HorizontalContentAlignment="Center"
                      Value="{Binding ModelParam.AcsLciPulseParam.CoordinateArrayVelocity,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}"/>
</StackPanel>
```

- [ ] **Step 3: Run config/UI verification**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build
```

Expected: coordinate-array config and XAML binding tests pass; wafer execution tests still fail until Task 5 is complete.

- [ ] **Step 4: Commit config/UI changes**

Run:

```powershell
git status --short
git add CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml
git commit -m "feat: expose wafer ACS coordinate array settings"
```

Expected in a valid Git checkout: one commit is created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and continue without committing.

---

### Task 5: Wafer ACS Point Execution Path

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorMotionControlModel.cs`

- [ ] **Step 1: Preserve ACS input order during point plan creation**

In `ExecutePointMoving`, add ACS detection immediately after `trackingCompleted` is initialized:

```csharp
var useAcsCoordinateArrayPulse = UseAcsLciCoordinateArrayPulse(controlCard);
```

Inside the dispatcher block, change the plan creation line to:

```csharp
pointPlan = BuildPointExecutionPlan(Calib, sourceLocusInfos, useAcsCoordinateArrayPulse);
```

In the same dispatcher block, change the `ReplaceLocusOrder(...)` guard to include `!useAcsCoordinateArrayPulse`:

```csharp
if (sourceLocusInfos == null &&
    CreateTrajectoryModel.IsOptimalPathEnabled &&
    !CreateTrajectoryModel.IsCalibrationWaferMeasurementActive &&
    !useAcsCoordinateArrayPulse &&
    orderedLocusInfos.Count > 0)
{
    WaferTrajectoryMotionHelper.ReplaceLocusOrder(AllLocusInfo, orderedLocusInfos);
}
```

- [ ] **Step 2: Branch ACS point execution before the Googol queue path**

In `ExecutePointMoving`, after this existing line:

```csharp
_trajectoryTrackingPublisher.PublishTarget(trackingRunId, 0, orderedLocusInfos[0].TargetX, orderedLocusInfos[0].TargetY);
```

add:

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

Replace the existing end-of-method point collection finish block:

```csharp
if (!Calib)
{
    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
}
else
{
    PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Calib");
}
```

with:

```csharp
PublishPointCollectionFinishEvent(Calib);
```

- [ ] **Step 3: Update point plan signature**

Replace the `BuildPointExecutionPlan` signature with:

```csharp
private PointExecutionPlan BuildPointExecutionPlan(
    bool calib,
    IEnumerable<LocusInfo>? sourceLocusInfos,
    bool preserveInputOrder = false)
```

Replace its `orderedLocusInfos` calculation with:

```csharp
var orderedLocusInfos = CreateTrajectoryModel.IsOptimalPathEnabled && !shouldInsertCalibration && !preserveInputOrder
    ? WaferTrajectoryMotionHelper.SortPointLocusInfosByShortestPath(executionLocusInfos)
    : executionLocusInfos;
```

- [ ] **Step 4: Add finish-event helper**

Add this helper near the existing publish helpers:

```csharp
private static void PublishPointCollectionFinishEvent(bool calib)
{
    if (!calib)
    {
        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
    }
    else
    {
        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Calib");
    }
}
```

- [ ] **Step 5: Add ACS point execution helpers**

Add these methods before the existing `ExecuteAcsLciFixedDistancePulseSegment(...)` method:

```csharp
private bool ExecuteAcsLciCoordinateArrayPulsePoints(
    ControlCardBase controlCard,
    IReadOnlyList<LocusInfo> orderedLocusInfos,
    string trackingRunId)
{
    if (orderedLocusInfos.Count < 2)
    {
        Logs.LogWarning("ACS LCI坐标数组脉冲至少需要两个有效点。");
        return false;
    }

    if (!TryRunLciCoordinateArrayPulse(controlCard, orderedLocusInfos, out var message))
    {
        Logs.LogWarning($"ACS LCI坐标数组脉冲执行失败：{message}");
        return false;
    }

    if (!string.IsNullOrWhiteSpace(message))
    {
        Logs.LogInfo(message);
    }

    var lastIndex = orderedLocusInfos.Count - 1;
    var lastPoint = orderedLocusInfos[lastIndex];
    _trajectoryTrackingPublisher.PublishProgress(trackingRunId, orderedLocusInfos.Count, lastIndex);
    _trajectoryTrackingPublisher.PublishTarget(trackingRunId, lastIndex, lastPoint.TargetX, lastPoint.TargetY);
    return true;
}

private bool TryRunLciCoordinateArrayPulse(
    ControlCardBase controlCard,
    IReadOnlyList<LocusInfo> orderedLocusInfos,
    out string message)
{
    message = string.Empty;
    if (!TryGetAcsLciCoordinateArrayPulseMethod(controlCard, out var method, out var parameterType, out message))
    {
        return false;
    }

    object parameter;
    try
    {
        parameter = BuildAcsLciCoordinateArrayPulseParam(parameterType, orderedLocusInfos);
    }
    catch (Exception ex)
    {
        message = $"构建ACS LCI坐标数组脉冲参数失败：{ex.Message}";
        return false;
    }

    try
    {
        var args = new object?[] { parameter, null, null };
        var invokeResult = method.Invoke(controlCard, args);
        message = args.Length > 2 ? args[2]?.ToString() ?? string.Empty : string.Empty;
        return invokeResult is bool success && success;
    }
    catch (Exception ex)
    {
        message = $"调用ACS LCI坐标数组脉冲失败：{ex.InnerException?.Message ?? ex.Message}";
        return false;
    }
}

private object BuildAcsLciCoordinateArrayPulseParam(
    Type parameterType,
    IReadOnlyList<LocusInfo> orderedLocusInfos)
{
    var config = AcsLciPulseParam ?? new AcsLciFixedDistancePulseConfig();
    var parameter = Activator.CreateInstance(parameterType)
        ?? throw new InvalidOperationException($"无法创建ACS LCI坐标数组参数类型：{parameterType.FullName}");

    SetAcsLciProperty(parameter, nameof(config.BufferNo), config.BufferNo);
    SetAcsLciProperty(parameter, nameof(config.AxisX), config.AxisX);
    SetAcsLciProperty(parameter, nameof(config.AxisY), config.AxisY);
    SetAcsLciProperty(parameter, nameof(config.PulseWidth), config.PulseWidth);
    SetAcsLciProperty(parameter, "MultiAxWinSize", config.CoordinateArrayMultiAxWinSize);
    SetAcsLciProperty(parameter, "Velocity", config.CoordinateArrayVelocity);
    SetAcsLciProperty(parameter, nameof(config.RouteConfigOutput), config.RouteConfigOutput);
    SetAcsLciProperty(parameter, nameof(config.ConfigOutputIndex), config.ConfigOutputIndex);
    SetAcsLciProperty(parameter, nameof(config.ConfigOutputCode), config.ConfigOutputCode);
    SetAcsLciProperty(parameter, nameof(config.Timeout), config.Timeout);
    SetAcsLciCoordinateArrayPulsePoints(parameter, orderedLocusInfos);

    return parameter;
}
```

- [ ] **Step 6: Add ACS detection and reflection helpers**

Add these methods near the existing ACS LCI reflection helpers:

```csharp
private static bool UseAcsLciCoordinateArrayPulse(ControlCardBase controlCard)
{
    return IsAcsControlCard(controlCard);
}

private static bool IsAcsControlCard(ControlCardBase controlCard)
{
    var typeName = controlCard.GetType().FullName ?? controlCard.GetType().Name;
    return string.Equals(controlCard.VenderName, "ACS", StringComparison.OrdinalIgnoreCase)
        || typeName.Contains(".ACS.", StringComparison.OrdinalIgnoreCase)
        || typeName.Contains("AcsControlCard", StringComparison.OrdinalIgnoreCase);
}

private static bool TryGetAcsLciCoordinateArrayPulseMethod(
    ControlCardBase controlCard,
    out MethodInfo method,
    out Type parameterType,
    out string message)
{
    method = null!;
    parameterType = null!;
    if (controlCard == null)
    {
        message = "控制卡为空。";
        return false;
    }

    method = controlCard.GetType()
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .FirstOrDefault(item =>
            item.Name == "TryRunLciCoordinateArrayPulse" &&
            item.GetParameters().Length == 3)!;
    if (method == null)
    {
        message = $"当前控制卡 {controlCard.GetType().Name} 不支持ACS LCI坐标数组脉冲。";
        return false;
    }

    var parameters = method.GetParameters();
    parameterType = parameters[0].ParameterType;
    message = string.Empty;
    return true;
}

private static void SetAcsLciCoordinateArrayPulsePoints(
    object parameter,
    IReadOnlyList<LocusInfo> orderedLocusInfos)
{
    var pointsProperty = parameter.GetType().GetProperty("Points")
        ?? throw new InvalidOperationException("ACS LCI坐标数组参数缺少Points属性。");
    var pointType = pointsProperty.PropertyType.GetGenericArguments().FirstOrDefault()
        ?? throw new InvalidOperationException("ACS LCI坐标数组Points属性不是泛型集合。");
    var listType = typeof(List<>).MakeGenericType(pointType);
    var points = (IList)(Activator.CreateInstance(listType)
        ?? throw new InvalidOperationException("无法创建ACS LCI坐标数组点位集合。"));

    foreach (var locusInfo in orderedLocusInfos)
    {
        points.Add(CreateAcsPoint2D(pointType, locusInfo.TargetX, locusInfo.TargetY));
    }

    pointsProperty.SetValue(parameter, points);
}
```

- [ ] **Step 7: Run wafer integration verification**

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build
```

Expected: wafer project builds, ACS test project builds, and all new coordinate-array tests pass. If an existing unrelated test still fails, capture the failing test name and confirm the new tests passed before it.

- [ ] **Step 8: Commit wafer execution changes**

Run:

```powershell
git status --short
git add CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs
git commit -m "feat: run wafer ACS points with LCI coordinate arrays"
```

Expected in a valid Git checkout: one commit is created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and continue without committing.

---

### Task 6: Final Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ReeYin_V.Hardware.ControlCard.ACS.csproj`
- Verify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Build ACS project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0. Existing warnings may remain, but no new compile errors should appear.

- [ ] **Step 2: Build wafer project**

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0. Existing warnings may remain, but no new compile errors should appear.

- [ ] **Step 3: Run ACS console tests**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build
```

Expected: all ACS LCI coordinate-array tests pass. If the suite stops on an unrelated pre-existing failure, report the first failing test and the last passing coordinate-array test.

- [ ] **Step 4: Review generated ACSPL+ script manually**

Run this quick source inspection:

```powershell
Select-String -Path Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs -Pattern "CoordinateArrPulse|MultiAxWinSize|RY_LCI_XCOORD|RY_LCI_YCOORD|WriteVariable"
```

Expected: output shows the Coordinate Array Pulse script builder and the X/Y array writes.

- [ ] **Step 5: Final commit**

Run:

```powershell
git status --short
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml docs\superpowers\specs\2026-06-23-acs-lci-coordinate-array-point-mode-design.md docs\superpowers\plans\2026-06-23-acs-lci-coordinate-array-point-mode.md
git commit -m "feat: support ACS LCI coordinate array point mode"
```

Expected in a valid Git checkout: one final commit is created only if earlier task commits were not created. In this workspace, if `git status` returns `fatal: not a git repository`, record that commits are unavailable and include that in the final handoff.

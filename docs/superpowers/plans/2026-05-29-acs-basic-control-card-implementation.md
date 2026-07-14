# ACS Basic Control Card Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the first-phase ACS basic control-card implementation with ACS-specific configuration, per-axis ACSPL Buffer homing, Program Buffer diagnostics, and configuration UI support.

**Architecture:** Keep `AcsControlCard` as one logical control card split by focused `partial` files. Add ACS-only options under the ACS plugin assembly, expose them through `AcsControlCard.Options`, and let the base configuration UI bind to those runtime properties without adding a project reference back to the ACS plugin. Program Buffer calls are isolated in `BufferProgram.cs` so later ACS advanced features can reuse the same diagnostics.

**Tech Stack:** C# / .NET 8 WPF, Prism, Newtonsoft.Json, ACS.SPiiPlusNET8 4.10, existing ReeYin-V `ControlCardBase` abstractions.

---

## File Structure

- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs` — ACS-only serializable options and per-axis home Buffer mapping.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/BufferProgram.cs` — `ProgramBuffer` conversion, `RunBuffer`, `WaitProgramEnd`, `StopBuffer`, state and error diagnostics.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs` — expose `Options`, initialize defaults, keep old public properties as compatibility proxies.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InitCard.cs` — read connection and timeout values from `Options`.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/IOControl.cs` — read IO counts from `Options` and reject out-of-range indexes.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs` — run configured per-axis ACSPL Buffers and report detailed failures.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs` — map more ACS motor/axis state bits and guard invalid axis config.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/ControlCardConfigViewModel.cs` — add ACS card creation, ensure plugin options by reflection, expose ACS card type.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardConfigModel.cs` — add `IsCurSltCardAcs` for XAML visibility without referencing the ACS plugin.
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/ControlCardConfigView.xaml` — add an ACS configuration tab/section bound to `ModelParam.CurSltCard.Options`.

## Baseline Command

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: build succeeds with `0 errors`. Existing `halcondotnet` and nullable warnings are acceptable.

---

### Task 1: Baseline And SDK Signature Check

**Files:**
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/*.cs`
- Read: `docs/SPiiPlus.NET-Library-Programmers-Guide_中文快速查阅手册.md`
- Read: `packages/A_ThirdParty/HardAPI/ACS/ACS.SPiiPlusNET8.dll`

- [ ] **Step 1: Run the baseline ACS build**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

- [ ] **Step 2: Confirm ACS Program Buffer signatures**

Use the reflected signatures already verified for `ACS.SPiiPlusNET8.dll`:

```csharp
void RunBuffer(ProgramBuffer buffer, string label)
void WaitProgramEnd(ProgramBuffer buffer, int timeout)
void StopBuffer(ProgramBuffer buffer)
ProgramStates GetProgramState(ProgramBuffer buffer)
int GetProgramError(ProgramBuffer buffer)
```

Expected: implementation code calls `RunBuffer(buffer, null)` and `WaitProgramEnd(buffer, timeout)`.

- [ ] **Step 3: Confirm the ProgramBuffer range**

Use the reflected enum values:

```csharp
ProgramBuffer.ACSC_BUFFER_0  == 0
ProgramBuffer.ACSC_BUFFER_64 == 64
ProgramBuffer.ACSC_NONE      == -1
ProgramBuffer.ACSC_BUFFER_ALL == -2
```

Expected: home Buffer config accepts only `0..64`; it rejects `ACSC_NONE` and `ACSC_BUFFER_ALL`.

---

### Task 2: Add ACS Options Model

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCardOptions.cs`

- [ ] **Step 1: Create `AcsControlCardOptions.cs`**

Create the file with this content:

```csharp
using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

[Serializable]
public class AcsControlCardOptions : BindableBase
{
    private AcsConnectionMode _connectionMode = AcsConnectionMode.Simulator;
    private string _remoteAddress = "10.0.0.183";
    private bool _useTcp = true;
    private int _ethernetPort = (int)ACS.SPiiPlusNET.EthernetCommOption.ACSC_SOCKET_STREAM_PORT;
    private int _serialPort = 1;
    private int _serialBaudRate = -1;
    private int _pciSlotNumber;
    private int _internalTimeout = 30000;
    private int _digitalInputCount = 32;
    private int _digitalOutputCount = 32;
    private AxisStopMode _homeBeforeRunStopMode = AxisStopMode.减速停止;
    private ObservableCollection<AcsAxisHomeBufferConfig> _homeBuffers = new();

    public AcsConnectionMode ConnectionMode
    {
        get => _connectionMode;
        set { _connectionMode = value; RaisePropertyChanged(); }
    }

    public string RemoteAddress
    {
        get => _remoteAddress;
        set { _remoteAddress = value; RaisePropertyChanged(); }
    }

    public bool UseTcp
    {
        get => _useTcp;
        set { _useTcp = value; RaisePropertyChanged(); }
    }

    public int EthernetPort
    {
        get => _ethernetPort;
        set { _ethernetPort = value; RaisePropertyChanged(); }
    }

    public int SerialPort
    {
        get => _serialPort;
        set { _serialPort = value; RaisePropertyChanged(); }
    }

    public int SerialBaudRate
    {
        get => _serialBaudRate;
        set { _serialBaudRate = value; RaisePropertyChanged(); }
    }

    public int PciSlotNumber
    {
        get => _pciSlotNumber;
        set { _pciSlotNumber = value; RaisePropertyChanged(); }
    }

    public int InternalTimeout
    {
        get => _internalTimeout;
        set { _internalTimeout = Math.Max(1000, value); RaisePropertyChanged(); }
    }

    public int DigitalInputCount
    {
        get => _digitalInputCount;
        set { _digitalInputCount = Math.Max(0, value); RaisePropertyChanged(); }
    }

    public int DigitalOutputCount
    {
        get => _digitalOutputCount;
        set { _digitalOutputCount = Math.Max(0, value); RaisePropertyChanged(); }
    }

    public AxisStopMode HomeBeforeRunStopMode
    {
        get => _homeBeforeRunStopMode;
        set { _homeBeforeRunStopMode = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<AcsAxisHomeBufferConfig> HomeBuffers
    {
        get => _homeBuffers;
        set { _homeBuffers = value ?? new ObservableCollection<AcsAxisHomeBufferConfig>(); RaisePropertyChanged(); }
    }

    [JsonIgnore]
    public IReadOnlyList<AcsConnectionMode> ConnectionModes { get; } =
        Enum.GetValues(typeof(AcsConnectionMode)).Cast<AcsConnectionMode>().ToArray();

    public void EnsureHomeBuffers(IEnumerable<En_AxisNum>? axes)
    {
        HomeBuffers ??= new ObservableCollection<AcsAxisHomeBufferConfig>();
        var sourceAxes = axes?.Distinct().ToArray();
        if (sourceAxes == null || sourceAxes.Length == 0)
        {
            sourceAxes = new[] { En_AxisNum.X, En_AxisNum.Y, En_AxisNum.Z, En_AxisNum.R };
        }

        var nextBuffer = 1;
        foreach (var axis in sourceAxes)
        {
            if (HomeBuffers.Any(item => item.Axis == axis))
            {
                continue;
            }

            HomeBuffers.Add(new AcsAxisHomeBufferConfig
            {
                Axis = axis,
                BufferNo = nextBuffer,
                Timeout = InternalTimeout,
                IsEnabled = false,
                StopAxisBeforeRun = true,
                ResetFeedbackAfterSuccess = false,
                ResetPosition = 0d
            });

            nextBuffer++;
        }
    }
}

[Serializable]
public class AcsAxisHomeBufferConfig : BindableBase
{
    private En_AxisNum _axis = En_AxisNum.X;
    private int _bufferNo = 1;
    private bool _isEnabled;
    private int _timeout = 30000;
    private bool _stopAxisBeforeRun = true;
    private bool _resetFeedbackAfterSuccess;
    private double _resetPosition;

    public En_AxisNum Axis
    {
        get => _axis;
        set { _axis = value; RaisePropertyChanged(); }
    }

    public int BufferNo
    {
        get => _bufferNo;
        set { _bufferNo = Math.Max(0, Math.Min(64, value)); RaisePropertyChanged(); }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; RaisePropertyChanged(); }
    }

    public int Timeout
    {
        get => _timeout;
        set { _timeout = Math.Max(1000, value); RaisePropertyChanged(); }
    }

    public bool StopAxisBeforeRun
    {
        get => _stopAxisBeforeRun;
        set { _stopAxisBeforeRun = value; RaisePropertyChanged(); }
    }

    public bool ResetFeedbackAfterSuccess
    {
        get => _resetFeedbackAfterSuccess;
        set { _resetFeedbackAfterSuccess = value; RaisePropertyChanged(); }
    }

    public double ResetPosition
    {
        get => _resetPosition;
        set { _resetPosition = value; RaisePropertyChanged(); }
    }
}
```

- [ ] **Step 2: Build after adding options**

Run the baseline build command.

Expected: `0 errors`.

---

### Task 3: Wire Options Into ACS Shell, Connection, And IO

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InitCard.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/IOControl.cs`

- [ ] **Step 1: Add `Options` and compatibility proxies to `AcsControlCard.cs`**

Replace the current constants and public connection/IO property block with this shape:

```csharp
private readonly object _syncRoot = new();
private Api _api = new();
private AcsControlCardOptions _options = new();

public AcsControlCardOptions Options
{
    get
    {
        EnsureOptions();
        return _options;
    }
    set
    {
        _options = value ?? new AcsControlCardOptions();
        EnsureOptions();
        RaisePropertyChanged();
    }
}

public void EnsureOptions()
{
    _options ??= new AcsControlCardOptions();
    _options.EnsureHomeBuffers(Config?.AllAxis?.Select(axis => axis.AxisNum));
}

public AcsConnectionMode ConnectionMode
{
    get => Options.ConnectionMode;
    set => Options.ConnectionMode = value;
}

public string RemoteAddress
{
    get => Options.RemoteAddress;
    set => Options.RemoteAddress = value;
}

public bool UseTcp
{
    get => Options.UseTcp;
    set => Options.UseTcp = value;
}

public int EthernetPort
{
    get => Options.EthernetPort;
    set => Options.EthernetPort = value;
}

public int SerialPort
{
    get => Options.SerialPort;
    set => Options.SerialPort = value;
}

public int SerialBaudRate
{
    get => Options.SerialBaudRate;
    set => Options.SerialBaudRate = value;
}

public int PciSlotNumber
{
    get => Options.PciSlotNumber;
    set => Options.PciSlotNumber = value;
}

public int InternalTimeout
{
    get => Options.InternalTimeout;
    set => Options.InternalTimeout = value;
}

public int DigitalInputCount
{
    get => Options.DigitalInputCount;
    set => Options.DigitalInputCount = value;
}

public int DigitalOutputCount
{
    get => Options.DigitalOutputCount;
    set => Options.DigitalOutputCount = value;
}
```

Keep the constructor unchanged except add this call at the end:

```csharp
EnsureOptions();
```

- [ ] **Step 2: Ensure options at the start of initialization**

In `InitCard.cs`, add this as the first statement inside `DoInit()` before setting `State`:

```csharp
EnsureOptions();
```

In `OpenCommunication()`, keep using the compatibility properties or switch to a local variable:

```csharp
var options = Options;
```

Then read `options.ConnectionMode`, `options.RemoteAddress`, `options.UseTcp`, `options.EthernetPort`, `options.SerialPort`, `options.SerialBaudRate`, and `options.PciSlotNumber`.

- [ ] **Step 3: Make serial helper methods read `Options`**

Update `GetSerialPort()` and `GetSerialBaudRate()`:

```csharp
private int GetSerialPort()
{
    if (!string.IsNullOrWhiteSpace(Config.Com))
    {
        var digits = new string(Config.Com.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var configuredPort) && configuredPort > 0)
        {
            return configuredPort;
        }
    }

    return Math.Max(1, Options.SerialPort);
}

private int GetSerialBaudRate()
{
    if (Config.BaudRate > 0)
    {
        return Config.BaudRate;
    }

    return Options.SerialBaudRate;
}
```

- [ ] **Step 4: Update IO count and index checks**

In `IOControl.cs`, use local counts:

```csharp
var inputCount = Options.DigitalInputCount;
Status = new bool[inputCount];
```

```csharp
var outputCount = Options.DigitalOutputCount;
Status = new bool[outputCount];
```

Update single IO guards:

```csharp
if (!IsConnected || Part < 0 || Part >= Options.DigitalOutputCount)
{
    return false;
}
```

```csharp
var count = InOrOut ? Options.DigitalInputCount : Options.DigitalOutputCount;
if (!IsConnected || Part < 0 || Part >= count)
{
    return false;
}
```

- [ ] **Step 5: Build after wiring options**

Run the baseline build command.

Expected: `0 errors`.

---

### Task 4: Add Program Buffer Diagnostics

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/BufferProgram.cs`

- [ ] **Step 1: Create `BufferProgram.cs`**

Create the file with this content:

```csharp
using ACS.SPiiPlusNET;
using System;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    private static bool TryToProgramBuffer(int bufferNo, out ProgramBuffer buffer, out string message)
    {
        if (bufferNo < 0 || bufferNo > 64)
        {
            buffer = ProgramBuffer.ACSC_NONE;
            message = $"ACS Buffer number {bufferNo} is invalid. Valid home buffers are 0..64.";
            return false;
        }

        buffer = (ProgramBuffer)bufferNo;
        message = string.Empty;
        return true;
    }

    private bool RunProgramBuffer(int bufferNo, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.RunBuffer(buffer, null);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"RunBuffer({bufferNo}) failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool WaitProgramBufferEnd(int bufferNo, int timeout, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.WaitProgramEnd(buffer, timeout);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            var stateText = TryGetProgramState(bufferNo, out var state, out var stateMessage)
                ? state.ToString()
                : stateMessage;
            var errorText = TryGetProgramError(bufferNo, out var error, out var errorMessage)
                ? error.ToString()
                : errorMessage;

            message = $"WaitProgramEnd({bufferNo}) failed or timed out after {timeout} ms. State={stateText}; Error={errorText}; Exception={ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool StopProgramBuffer(int bufferNo, out string message)
    {
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            _api.StopBuffer(buffer);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"StopBuffer({bufferNo}) failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    private bool TryGetProgramState(int bufferNo, out ProgramStates state, out string message)
    {
        state = ProgramStates.ACSC_NONE;
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            state = _api.GetProgramState(buffer);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"GetProgramState({bufferNo}) failed: {ex.Message}";
            return false;
        }
    }

    private bool TryGetProgramError(int bufferNo, out int error, out string message)
    {
        error = 0;
        if (!TryToProgramBuffer(bufferNo, out var buffer, out message))
        {
            return false;
        }

        try
        {
            error = _api.GetProgramError(buffer);
            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"GetProgramError({bufferNo}) failed: {ex.Message}";
            return false;
        }
    }

    private string FormatProgramDiagnostics(int bufferNo)
    {
        var stateText = TryGetProgramState(bufferNo, out var state, out var stateMessage)
            ? state.ToString()
            : stateMessage;
        var errorText = TryGetProgramError(bufferNo, out var error, out var errorMessage)
            ? error.ToString()
            : errorMessage;

        return $"Buffer={bufferNo}; State={stateText}; Error={errorText}";
    }
}
```

- [ ] **Step 2: Build after adding Program Buffer diagnostics**

Run the baseline build command.

Expected: `0 errors`.

---

### Task 5: Replace Feedback-Only Homing With Per-Axis Buffer Homing

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`

- [ ] **Step 1: Add home Buffer lookup helpers to `GoHome.cs`**

Add these helpers inside `AcsControlCard`:

```csharp
private bool TryGetHomeBufferConfig(En_AxisNum axisId, out AcsAxisHomeBufferConfig config, out string message)
{
    EnsureOptions();
    config = Options.HomeBuffers.FirstOrDefault(item => item.Axis == axisId);
    if (config == null)
    {
        message = $"ACS home buffer for axis {axisId} is not configured.";
        return false;
    }

    if (!config.IsEnabled)
    {
        message = $"ACS home buffer for axis {axisId} is configured but disabled.";
        return false;
    }

    if (config.BufferNo < 0 || config.BufferNo > 64)
    {
        message = $"ACS home buffer for axis {axisId} has invalid BufferNo={config.BufferNo}.";
        return false;
    }

    message = string.Empty;
    return true;
}

private bool RunAxisHomeBuffer(SingleAxisParam axisConfig, AcsAxisHomeBufferConfig homeConfig, out string message)
{
    var axisId = axisConfig.AxisNum;
    var acsAxis = ToConfiguredAcsAxis(axisConfig);

    try
    {
        if (homeConfig.StopAxisBeforeRun)
        {
            StopAxis(acsAxis, Options.HomeBeforeRunStopMode);
        }

        if (!RunProgramBuffer(homeConfig.BufferNo, out message))
        {
            message = $"Axis={axisId}; AcsAxis={(int)acsAxis}; {message}";
            return false;
        }

        var timeout = homeConfig.Timeout > 0 ? homeConfig.Timeout : Options.InternalTimeout;
        if (!WaitProgramBufferEnd(homeConfig.BufferNo, timeout, out message))
        {
            message = $"Axis={axisId}; AcsAxis={(int)acsAxis}; {message}";
            return false;
        }

        UpdateAxisState(axisId);
        GetAllPosInfos();

        if (homeConfig.ResetFeedbackAfterSuccess && !ResetFeedbackPosition(axisId, homeConfig.ResetPosition))
        {
            message = $"Axis={axisId}; AcsAxis={(int)acsAxis}; home Buffer completed but feedback reset failed. {FormatProgramDiagnostics(homeConfig.BufferNo)}";
            return false;
        }

        axisConfig.IsHomed = true;
        message = string.Empty;
        return true;
    }
    catch (Exception ex)
    {
        message = $"Axis={axisId}; AcsAxis={(int)acsAxis}; Buffer={homeConfig.BufferNo}; Exception={ex.Message}; {FormatProgramDiagnostics(homeConfig.BufferNo)}";
        Console.WriteLine($"ACS home failed: {message}");
        return false;
    }
}
```

- [ ] **Step 2: Replace `DoGoHome(out message)`**

Replace the body of `DoGoHome(out string message)` with:

```csharp
protected override bool DoGoHome(out string message)
{
    message = string.Empty;
    if (!IsConnected)
    {
        message = "ACS control card is not connected.";
        return false;
    }

    EnsureOptions();
    IsAxisHoming = true;
    IsAxisHomed = false;

    try
    {
        var axes = Config.AllAxis.Where(axis => axis.IsUsing).ToArray();
        if (axes.Length == 0)
        {
            message = "ACS home failed: no enabled axis is configured.";
            return false;
        }

        foreach (var axis in axes)
        {
            axis.IsHomed = false;
            if (!TryGetHomeBufferConfig(axis.AxisNum, out var homeConfig, out message))
            {
                message = $"ACS home failed before axis {axis.AxisNum}: {message}";
                return false;
            }

            if (!RunAxisHomeBuffer(axis, homeConfig, out message))
            {
                axis.IsHomed = false;
                message = $"ACS home failed on axis {axis.AxisNum}: {message}";
                return false;
            }
        }

        IsAxisHomed = true;
        message = "ACS home completed by configured per-axis ACSPL buffers.";
        return true;
    }
    catch (Exception ex)
    {
        message = ex.Message;
        Console.WriteLine($"ACS DoGoHome failed: {ex.Message}");
        return false;
    }
    finally
    {
        IsAxisHoming = false;
    }
}
```

- [ ] **Step 3: Keep `ResetFeedbackPosition()` unchanged**

Confirm the file still has this explicit helper:

```csharp
public bool ResetFeedbackPosition(En_AxisNum axisId, double position = 0d)
```

Expected: it remains available, but `DoGoHome()` no longer uses feedback clearing as the only homing behavior.

- [ ] **Step 4: Build after homing changes**

Run the baseline build command.

Expected: `0 errors`.

---

### Task 6: Improve Axis State Mapping And IO Guards

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/IOControl.cs`

- [ ] **Step 1: Add axis state read in `ApplyAxisState`**

Inside `ApplyAxisState`, after `GetMotorState`-based values are calculated, read `GetAxisState`:

```csharp
AxisStates axisState = 0;
try
{
    axisState = _api.GetAxisState(ToConfiguredAcsAxis(axis));
}
catch
{
    axisState = 0;
}
```

- [ ] **Step 2: Map additional state bits defensively**

Extend the status mapping with flag-name checks so it compiles even if ACS enum values differ across SDK revisions:

```csharp
static bool HasFlagByName<TEnum>(TEnum value, string flagName) where TEnum : struct, Enum
{
    if (!Enum.TryParse<TEnum>(flagName, out var flag))
    {
        return false;
    }

    var rawValue = Convert.ToInt64(value);
    var rawFlag = Convert.ToInt64(flag);
    return rawFlag != 0 && (rawValue & rawFlag) != 0;
}
```

Use it in `ApplyAxisState`:

```csharp
var hasFault = HasFlagByName(state, "ACSC_MST_FAULT")
               || HasFlagByName(state, "ACSC_MST_SAFETY")
               || HasFlagByName(state, "ACSC_MST_ERROR");
var positiveLimit = HasFlagByName(axisState, "ACSC_AST_LPOS")
                    || HasFlagByName(axisState, "ACSC_AST_PE");
var negativeLimit = HasFlagByName(axisState, "ACSC_AST_LNEG")
                    || HasFlagByName(axisState, "ACSC_AST_NE");

axis.IsPositiveLimit = positiveLimit;
axis.IsNegativeLimit = negativeLimit;
if (hasFault)
{
    axis.AxisStatus |= (int)AxisStatusFlags.FollowErrorOverLimit;
}

if (positiveLimit)
{
    axis.AxisStatus |= (int)AxisStatusFlags.PositiveLimitTriggered;
}

if (negativeLimit)
{
    axis.AxisStatus |= (int)AxisStatusFlags.NegativeLimitTriggered;
}
```

- [ ] **Step 3: Guard configured ACS axis conversion**

Update `ToConfiguredAcsAxis` to avoid negative enum values:

```csharp
private static Axis ToConfiguredAcsAxis(SingleAxisParam axis)
{
    return ToZeroBasedAcsAxis((short)Math.Max(0, axis.AxisNo - 1));
}
```

- [ ] **Step 4: Confirm IO out-of-range behavior**

Run:

```powershell
rg -n "Part >= Options\\.Digital|new bool\\[Options\\.Digital|DigitalInputCount|DigitalOutputCount" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\IOControl.cs'
```

Expected: output shows `Options.DigitalInputCount`, `Options.DigitalOutputCount`, and bounds checks for single IO methods.

- [ ] **Step 5: Build after state and IO changes**

Run the baseline build command.

Expected: `0 errors`.

---

### Task 7: Add ACS-Aware Config ViewModel Support Without Circular References

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardConfigModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/ControlCardConfigViewModel.cs`

- [ ] **Step 1: Add `IsCurSltCardAcs` to `ControlCardConfigModel`**

Add this property near `IsCurSltCardIsNotNull`:

```csharp
[JsonIgnore]
private bool _isCurSltCardAcs;

[JsonIgnore]
public bool IsCurSltCardAcs
{
    get => _isCurSltCardAcs;
    private set { _isCurSltCardAcs = value; RaisePropertyChanged(); }
}
```

Add this helper in the methods region:

```csharp
private static bool IsAcsCard(ControlCardBase card)
{
    return card != null &&
           (string.Equals(card.VenderName, "ACS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.GetType().FullName, "ReeYin_V.Hardware.ControlCard.ACS.App.AcsControlCard", StringComparison.Ordinal));
}
```

Update the `CurSltCard` setter:

```csharp
_curSltCard = value;
IsCurSltCardIsNotNull = _curSltCard != null;
IsCurSltCardAcs = IsAcsCard(_curSltCard);
_curSltCard?.Config?.EnsureInterpolationCoordinateSystems();
SltInterPoCoordinateSystem = _curSltCard?.Config?.GetMatchedInterpolationCoordinateSystem(_curSltCard?.Config?.DefaultInterpCS)
    ?? _curSltCard?.Config?.InterpolationCoordinateSystems?.FirstOrDefault();
RaisePropertyChanged();
```

- [ ] **Step 2: Add ACS card type to `CardTypes`**

In `ControlCardConfigViewModel`, change the initial collection:

```csharp
private ObservableCollection<string> _cardTypes = new ObservableCollection<string>()
{
    "None",
    "GXN",
    "SPiiPlus"
};
```

- [ ] **Step 3: Add reflection helper to ensure plugin options**

Add this method in `ControlCardConfigViewModel`:

```csharp
private static void EnsureVendorSpecificOptions(ControlCardBase card)
{
    if (card == null)
    {
        return;
    }

    var method = card.GetType().GetMethod("EnsureOptions", BindingFlags.Public | BindingFlags.Instance);
    method?.Invoke(card, null);
}
```

Call it in `InitParam()`:

```csharp
foreach (var card in ModelParam.CardModels.Where(card => card?.Config != null))
{
    card.Config.EnsureInterpolationCoordinateSystems();
    EnsureVendorSpecificOptions(card);
}
```

- [ ] **Step 4: Add ACS card creation case**

In `DataOperateCommand`, after the Googol branch, add:

```csharp
if (ModelParam.SltVendorType == "ACS")
{
    module = PrismProvider.Container.Resolve<IControlCard>("ACSControlCard") as ControlCardBase;
}
```

After setting `module.CardType`, call:

```csharp
EnsureVendorSpecificOptions(module);
```

- [ ] **Step 5: Preserve newly added card selection**

Replace the block that resets the selection to the first card:

```csharp
ModelParam.CardModels.Add(module);
ModelParam.CurSltCard = module;
if (ModelParam.CardModels.Count > 0)
    ModelParam.CurSltCard = ModelParam.CardModels[0];
```

with:

```csharp
ModelParam.CardModels.Add(module);
ModelParam.CurSltCard = module;
```

Expected: adding an ACS card leaves the ACS card selected so the ACS configuration panel becomes visible.

- [ ] **Step 6: Build the base control card project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

---

### Task 8: Add ACS Configuration UI

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/ControlCardConfigView.xaml`

- [ ] **Step 1: Add a boolean visibility converter resource**

Add this directly under the `UserControl` opening block, before the existing triggers:

```xml
<UserControl.Resources>
    <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
</UserControl.Resources>
```

- [ ] **Step 2: Add an ACS configuration tab**

Inside the right-side `TabControl`, after the existing basic configuration tab, add:

```xml
<TabItem Header="ACS配置"
         Width="100"
         FontSize="20"
         Style="{DynamicResource SingleTabItemStyle}"
         Visibility="{Binding ModelParam.IsCurSltCardAcs, Converter={StaticResource BooleanToVisibilityConverter}}">
    <Grid Margin="5">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="6" Padding="12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="160"/>
                    <ColumnDefinition Width="16"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="160"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Grid.Column="0" Text="连接方式" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <ComboBox Grid.Row="0" Grid.Column="1"
                          ItemsSource="{Binding ModelParam.CurSltCard.Options.ConnectionModes}"
                          SelectedItem="{Binding ModelParam.CurSltCard.Options.ConnectionMode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="0" Grid.Column="3" Text="IP地址" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="0" Grid.Column="4"
                         Text="{Binding ModelParam.CurSltCard.Options.RemoteAddress, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="2" Grid.Column="0" Text="使用TCP" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <CheckBox Grid.Row="2" Grid.Column="1"
                          IsChecked="{Binding ModelParam.CurSltCard.Options.UseTcp, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="2" Grid.Column="3" Text="以太网端口" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="2" Grid.Column="4"
                         Text="{Binding ModelParam.CurSltCard.Options.EthernetPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="4" Grid.Column="0" Text="串口号" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="4" Grid.Column="1"
                         Text="{Binding ModelParam.CurSltCard.Options.SerialPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="4" Grid.Column="3" Text="波特率" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="4" Grid.Column="4"
                         Text="{Binding ModelParam.CurSltCard.Options.SerialBaudRate, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="6" Grid.Column="0" Text="PCI Slot" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="6" Grid.Column="1"
                         Text="{Binding ModelParam.CurSltCard.Options.PciSlotNumber, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                <TextBlock Grid.Row="6" Grid.Column="3" Text="内部超时(ms)" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Grid.Row="6" Grid.Column="4"
                         Text="{Binding ModelParam.CurSltCard.Options.InternalTimeout, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
            </Grid>
        </Border>

        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="6">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <Border Grid.Row="0" Background="{DynamicResource BtnCheckedColor}" CornerRadius="6,6,0,0" Padding="12,8">
                    <TextBlock Text="ACS每轴回零Buffer映射" Foreground="White" FontWeight="SemiBold" FontSize="14" VerticalAlignment="Center"/>
                </Border>

                <DataGrid Grid.Row="1"
                          AutoGenerateColumns="False"
                          Style="{StaticResource DefaultDataGridStyle}"
                          ItemsSource="{Binding ModelParam.CurSltCard.Options.HomeBuffers}"
                          CanUserAddRows="False"
                          IsReadOnly="False">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="轴" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox helper:ItemsControlHelper.EnumValuesToItemsSource="True"
                                              SelectedItem="{Binding Axis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="Buffer编号" Binding="{Binding BufferNo, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                        <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="70"/>
                        <DataGridTextColumn Header="超时(ms)" Binding="{Binding Timeout, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="100"/>
                        <DataGridCheckBoxColumn Header="运行前停止" Binding="{Binding StopAxisBeforeRun, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                        <DataGridCheckBoxColumn Header="成功后清零" Binding="{Binding ResetFeedbackAfterSuccess, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                        <DataGridTextColumn Header="清零位置" Binding="{Binding ResetPosition, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="100"/>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
    </Grid>
</TabItem>
```

- [ ] **Step 3: Build the WPF control card project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

---

### Task 9: Final Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/*.cs`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/ControlCardConfigView.xaml`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/ControlCardConfigViewModel.cs`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardConfigModel.cs`

- [ ] **Step 1: Build ACS plugin**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

- [ ] **Step 2: Build base control card project**

Run:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: `0 errors`.

- [ ] **Step 3: Confirm ACS-specific UI does not create a project reference cycle**

Run:

```powershell
rg -n "ReeYin_V.Hardware.ControlCard.ACS.csproj|ProjectReference.*ACS" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj'
```

Expected: no output.

- [ ] **Step 4: Confirm homing no longer relies only on feedback reset**

Run:

```powershell
rg -n "RunProgramBuffer|WaitProgramBufferEnd|ResetFeedbackPosition|SetFPosition" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs'
```

Expected: `RunProgramBuffer` and `WaitProgramBufferEnd` appear in the homing path; `SetFPosition` appears only inside `ResetFeedbackPosition`.

- [ ] **Step 5: Confirm Program Buffer APIs are isolated**

Run:

```powershell
rg -n "RunBuffer|WaitProgramEnd|StopBuffer|GetProgramState|GetProgramError" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App'
```

Expected: these ACS Program Buffer SDK calls appear in `BufferProgram.cs`; `GoHome.cs` calls the local wrapper methods.

---

## Self-Review

- Spec coverage: the plan covers ACS options, UI, per-axis Buffer homing, Program Buffer diagnostics, basic connection/axis/position/IO hardening, and build verification.
- Placeholder scan: no unfinished markers or vague implementation steps remain.
- Type consistency: `AcsControlCardOptions`, `AcsAxisHomeBufferConfig`, `Options`, `EnsureOptions`, `RunProgramBuffer`, `WaitProgramBufferEnd`, `TryGetProgramState`, and `TryGetProgramError` use the same names throughout.
- Dependency check: the base control card project uses string/reflection/XAML runtime binding and does not reference the ACS plugin project.

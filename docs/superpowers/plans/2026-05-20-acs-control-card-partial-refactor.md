# ACS Control Card Partial Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `AcsControlCard` into focused `partial` files matching the Googol project style while preserving behavior.

**Architecture:** Keep one logical `AcsControlCard` class and split existing members across focused files under `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App`. This is a mechanical refactor: no public API changes, no ACS SDK call changes, and no new ACSPL features.

**Tech Stack:** C# / .NET 8 WPF library, `ACS.SPiiPlusNET8.dll`, existing `ControlCardBase`, `dotnet build` validation.

---

## File Structure

- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs` — class shell, constants, fields, constructor, connection/IO properties.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InitCard.cs` — `DoInit`, `DoConfigure`, `DoClose`, `OpenCommunication`, serial helpers.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs` — enable/state/speed/axis conversion/state update/buffer helpers.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/JogMove.cs` — `Move`, `DoMoveAxis`, `DoMoveContinue`, `JogAxis`, `MoveAbsoluteAxis`.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/Stop.cs` — `DoStop`, stop helpers, wait helpers.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs` — `DoGoHome`, `ResetFeedbackPosition`, position reads.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs` — line interpolation and axis/target preparation.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs` — arc interpolation and center/radius helpers.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CustomInterpolation.cs` — `CustomInterpolationMoving`.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/IOControl.cs` — digital input/output methods and IO bit helpers.
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/Assist.cs` — `IsFinite`, `TryExecute`.

## Baseline Command

Run all build checks with:

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

Expected: build succeeds with 0 errors. Existing `halcondotnet` warning from `ReeYin_V.Share` is acceptable.

---

### Task 1: Baseline Verification

**Files:**
- Read: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`

- [ ] **Step 1: Run baseline build**

Run the baseline command above. Expected: success with 0 errors.

- [ ] **Step 2: Capture method inventory**

Run:

```powershell
rg -n "^\s*(public|protected|private)\s+(override\s+|static\s+)?[\w\?\[\]<>]+\s+\w+\(" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.cs'
```

Expected: inventory includes connection, axis, motion, IO, interpolation, and helper methods.

---

### Task 2: Create Partial Shell and Move Connection Lifecycle

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AcsControlCard.cs`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InitCard.cs`

- [ ] **Step 1: Change `AcsControlCard.cs` to the class shell**

Keep only `using ACS.SPiiPlusNET;`, the namespace, `public partial class AcsControlCard : ControlCardBase`, constants, `_syncRoot`, `_api`, constructor, and properties: `ConnectionMode`, `RemoteAddress`, `UseTcp`, `EthernetPort`, `SerialPort`, `SerialBaudRate`, `PciSlotNumber`, `InternalTimeout`, `DigitalInputCount`, `DigitalOutputCount`.

- [ ] **Step 2: Create `InitCard.cs`**

Move the exact existing implementations of `DoInit`, `DoConfigure`, `DoClose`, `OpenCommunication`, `GetSerialPort`, and `GetSerialBaudRate` from the original file into `InitCard.cs`. Add required usings: `ACS.SPiiPlusNET`, `ReeYin_V.Core`, `System`, `System.Linq`.

- [ ] **Step 3: Build**

Run the baseline command. Expected: temporary missing-member errors are acceptable until later tasks; syntax errors are not acceptable.

---

### Task 3: Move Axis Setup and State Methods

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/AxisSetting.cs`

- [ ] **Step 1: Create `AxisSetting.cs`**

Move the exact existing implementations of `DoGetAxisEnable`, `DoSetAxisEnable`, `SetAxisEnabled`, `DoGetAxisStopped`, `PrepareMotorToMove`, `GetFlag`, `GetSpeedSetting`, both `GetAxisVelocity` overloads, `GetInterpolationVelocity`, `ConfigureInterpolationAxes`, `TryGetAxisConfig`, all `ToAcsAxis` overloads, `ToAcsRotation`, both `UpdateAxisState` overloads, `UpdateAxisStates`, `InitializeAxisBuffers`, and `EnsurePositionBuffers`. Add required usings: `ACS.SPiiPlusNET`, `ReeYin_V.Hardware.ControlCard.Models`, `System`, `System.Collections.Generic`, `System.Linq`.

- [ ] **Step 2: Build**

Run the baseline command. Expected: remaining failures only relate to not-yet-created movement, stop, IO, interpolation, or helper methods.

---

### Task 4: Move Single-Axis Motion and Stop Methods

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/JogMove.cs`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/Stop.cs`

- [ ] **Step 1: Create `JogMove.cs`**

Move the exact existing implementations of `Move`, `DoMoveAxis`, `DoMoveContinue`, both `JogAxis` overloads, and `MoveAbsoluteAxis`. Add required usings: `ACS.SPiiPlusNET`, `System`.

- [ ] **Step 2: Create `Stop.cs`**

Move the exact existing implementations of `DoStop`, `StopConfiguredAxes`, `StopAxis`, and both `WaitUntilStopped` overloads. Add required usings: `ACS.SPiiPlusNET`, `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading`.

- [ ] **Step 3: Build**

Run the baseline command. Expected: remaining failures only relate to not-yet-created homing, IO, interpolation, or helper methods.

---

### Task 5: Move Homing, Position, and IO Methods

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/GoHome.cs`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/IOControl.cs`

- [ ] **Step 1: Create `GoHome.cs`**

Move the exact existing implementations of `DoGoHome`, `ResetFeedbackPosition`, both `GetAllPosInfos` overloads. Add required usings: `System`, `System.Linq`.

- [ ] **Step 2: Create `IOControl.cs`**

Move the exact existing implementations of `GetAllInput`, `GetAllOutput`, `SetSpecifiedIO`, `GetSpecifiedIO`, `GetIoPort`, and `GetIoBit`. Add required using: `System`.

- [ ] **Step 3: Build**

Run the baseline command. Expected: remaining failures only relate to not-yet-created interpolation or helper methods.

---

### Task 6: Move Interpolation Methods

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/LineInterpolation.cs`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CircularInterpolation.cs`
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/CustomInterpolation.cs`

- [ ] **Step 1: Create `LineInterpolation.cs`**

Move the exact existing implementations of `LineInterpoMoving`, `TryBuildInterpolationMove`, `TryBuildAxes`, and `PrepareInterpolationAxes`. Add required usings: `ACS.SPiiPlusNET`, `System`, `System.Collections.Generic`, `System.Linq`.

- [ ] **Step 2: Create `CircularInterpolation.cs`**

Move the exact existing implementations of `ArcInterpoMoving`, `TryResolveArcCenter`, `TryResolveCenterFromRadius`, and `GetDistance`. Add required usings: `System`, `System.Collections.Generic`, `System.Linq`.

- [ ] **Step 3: Create `CustomInterpolation.cs`**

Move the exact existing implementation of `CustomInterpolationMoving`. Add required using: `System`.

- [ ] **Step 4: Build**

Run the baseline command. Expected: remaining failures only relate to `IsFinite` or `TryExecute` until Task 7.

---

### Task 7: Move Shared Helpers and Final Verify

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/Assist.cs`

- [ ] **Step 1: Create `Assist.cs`**

Move the exact existing implementations of `IsFinite` and `TryExecute`. Add required using: `System`.

- [ ] **Step 2: Run final build**

Run the baseline command. Expected: build succeeds with 0 errors.

- [ ] **Step 3: Confirm shell file has no moved method bodies**

Run:

```powershell
rg -n "DoInit|DoMoveAxis|LineInterpoMoving|ArcInterpoMoving|GetAllInput|TryExecute" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.cs'
```

Expected: no output.

- [ ] **Step 4: Confirm partial methods are distributed**

Run:

```powershell
rg -n "partial class AcsControlCard|DoInit|DoMoveAxis|LineInterpoMoving|ArcInterpoMoving|GetAllInput|TryExecute" 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App'
```

Expected: each moved method appears in its category file.

---

## Self-Review

- Spec coverage: all design sections are mapped to Tasks 1-7.
- Placeholder scan: no unfinished-placeholder markers; every task names exact files and exact method groups.
- Type consistency: all names match existing ACS code and base abstractions.
- Scope check: plan only splits existing ACS implementation; it does not add PEG, DC, CONNECT, EtherCAT, CoE, or UI work.
